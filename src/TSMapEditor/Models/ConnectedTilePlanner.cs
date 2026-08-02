using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TSMapEditor.GameMath;

namespace TSMapEditor.Models
{
    public enum ConnectedTilePlanStatus
    {
        Success,
        InvalidInput,
        NoSolution,
        SearchLimit
    }

    public sealed class ConnectedTilePlacement
    {
        internal ConnectedTilePlacement(ConnectedTile tile, Point2D location,
            TileConnectionPoint? entryPoint, TileConnectionPoint? exitPoint)
        {
            Tile = tile;
            Location = location;
            EntryPoint = entryPoint;
            ExitPoint = exitPoint;
        }

        public ConnectedTile Tile { get; }
        public Point2D Location { get; }
        public TileConnectionPoint? EntryPoint { get; }
        public TileConnectionPoint? ExitPoint { get; }

        public Point2D? EntryCoordinates => EntryPoint.HasValue
            ? Location + EntryPoint.Value.CoordinateOffset
            : null;

        public Point2D? ExitCoordinates => ExitPoint.HasValue
            ? Location + ExitPoint.Value.CoordinateOffset
            : null;
    }

    public sealed class ConnectedTilePlan
    {
        internal ConnectedTilePlan(ConnectedTileType type, Point2D origin,
            IEnumerable<ConnectedTilePlacement> placements, bool isClosed, bool usesEndPieces)
        {
            Type = type;
            Origin = origin;
            Placements = new ReadOnlyCollection<ConnectedTilePlacement>(placements.ToList());
            IsClosed = isClosed;
            UsesEndPieces = usesEndPieces;
        }

        public ConnectedTileType Type { get; }
        public ConnectedTileType ConnectedTileType => Type;
        public Point2D Origin { get; }
        public Point2D Start => Origin;
        public IReadOnlyList<ConnectedTilePlacement> Placements { get; }
        public bool IsClosed { get; }
        public bool UsesEndPieces { get; }
    }

    public sealed class ConnectedTilePlanResult
    {
        private ConnectedTilePlanResult(ConnectedTilePlanStatus status, ConnectedTilePlan plan, string message)
        {
            Status = status;
            Plan = plan;
            Message = message;
        }

        public ConnectedTilePlanStatus Status { get; }
        public bool Success => Status == ConnectedTilePlanStatus.Success;
        public bool IsSuccess => Success;
        public ConnectedTilePlan Plan { get; }
        public string Message { get; }

        internal static ConnectedTilePlanResult Succeeded(ConnectedTilePlan plan)
            => new ConnectedTilePlanResult(ConnectedTilePlanStatus.Success, plan, null);

        internal static ConnectedTilePlanResult Failed(ConnectedTilePlanStatus status, string message)
            => new ConnectedTilePlanResult(status, null, message);
    }

    /// <summary>
    /// Creates an immutable placement plan for connected terrain. Planning is deliberately separate from
    /// map mutation so a validated preview can be committed without running the bounded search again.
    /// </summary>
    public static class ConnectedTilePlanner
    {
        private const int MaxExpandedNodesPerLegacySegment = 1_000;
        private const int MaxQueuedNodesPerLegacySegment = 8_000;
        private const int MaxExpandedNodesPerStrictSegment = 25_000;
        private const int MaxQueuedNodesPerStrictSegment = 100_000;
        private const int MaxPlacementCount = 2_048;
        private const int MaxBoundaryStates = 24;

        private const float TileSizePenaltyPerFoundationCell = 0.07f;
        private const int RepeatPenaltyAncestorWindow = 5;
        private const float RepeatPenaltyPerWeightedUse = 0.75f;
        private const float RepeatPenaltyPerFoundationCell = 0.12f;
        private const float TurnTilePenalty = 1.0f;
        private const float NodeScoreJitterAmplitude = 0.02f;

        private enum SegmentGoal
        {
            Point,
            EndingPiece,
            ClosedSeam
        }

        private sealed class SearchNode
        {
            private SearchNode() { }

            public SearchNode(SearchNode parent, ConnectedTile tile, Point2D location,
                TileConnectionPoint? entryPoint, TileConnectionPoint? exitPoint, HashSet<Point2D> occupiedCells,
                float gScore)
            {
                Parent = parent;
                Tile = tile;
                Location = location;
                EntryPoint = entryPoint;
                ExitPoint = exitPoint;
                OccupiedCells = occupiedCells;
                GScore = gScore;
                Depth = (parent?.Depth ?? 0) + 1;

                if (parent?.FirstPlacement != null)
                    FirstPlacement = parent.FirstPlacement;
                else if (tile != null)
                    FirstPlacement = this;
            }

            public SearchNode Parent { get; private init; }
            public SearchNode FirstPlacement { get; private set; }
            public ConnectedTile Tile { get; private init; }
            public Point2D Location { get; private init; }
            public TileConnectionPoint? EntryPoint { get; private init; }
            public TileConnectionPoint? ExitPoint { get; private init; }
            public HashSet<Point2D> OccupiedCells { get; private init; }
            public float GScore { get; private init; }
            public int Depth { get; private init; }

            public Point2D ExitCoordinates => Location + ExitPoint.Value.CoordinateOffset;

            public static SearchNode MakeSyntheticStart(Point2D location, ConnectedTileSide startingSide)
            {
                var connectionPoint = new TileConnectionPoint
                {
                    Index = -1,
                    ConnectionMask = byte.MaxValue,
                    CoordinateOffset = Point2D.Zero,
                    Side = startingSide,
                    RequiredTiles = Array.Empty<int>(),
                    ForbiddenTiles = Array.Empty<int>()
                };

                return new SearchNode
                {
                    Location = location,
                    ExitPoint = connectionPoint,
                    OccupiedCells = new HashSet<Point2D>(),
                    GScore = 0.0f,
                    Depth = 0
                };
            }
        }

        private sealed class SegmentSearchResult
        {
            public List<SearchNode> ExactNodes { get; } = new List<SearchNode>();
            public SearchNode BestNode { get; set; }
            public bool SearchWasLimited { get; set; }
        }

        public static ConnectedTilePlanResult Plan(ConnectedTileType type, IReadOnlyList<Point2D> path,
            ConnectedTileSide startingSide, int randomSeed, bool useEndPieces, bool closed,
            Func<Point2D, bool> isCellValid)
        {
            if (type == null)
                return ConnectedTilePlanResult.Failed(ConnectedTilePlanStatus.InvalidInput,
                    "A connected tile type is required.");

            if (path == null)
                return ConnectedTilePlanResult.Failed(ConnectedTilePlanStatus.InvalidInput,
                    "A connected tile path is required.");

            if (isCellValid == null)
                return ConnectedTilePlanResult.Failed(ConnectedTilePlanStatus.InvalidInput,
                    "A map-cell validator is required.");

            var vertices = path.ToList();
            if (closed && vertices.Count > 1 && vertices[0] == vertices[^1])
                vertices.RemoveAt(vertices.Count - 1);

            int minimumVertexCount = closed ? 3 : 2;
            if (vertices.Count < minimumVertexCount)
            {
                return ConnectedTilePlanResult.Failed(ConnectedTilePlanStatus.InvalidInput,
                    closed
                        ? "A closed connected tile path requires at least three vertices."
                        : "A connected tile path requires at least two vertices.");
            }

            if (closed && vertices.Distinct().Count() < 3)
            {
                return ConnectedTilePlanResult.Failed(ConnectedTilePlanStatus.InvalidInput,
                    "A closed connected tile path requires at least three distinct vertices.");
            }

            for (int i = 0; i < vertices.Count; i++)
            {
                if (!isCellValid(vertices[i]))
                {
                    return ConnectedTilePlanResult.Failed(ConnectedTilePlanStatus.InvalidInput,
                        $"Connected tile path vertex {i} is outside the valid map area.");
                }

                if (i > 0 && vertices[i] == vertices[i - 1])
                {
                    return ConnectedTilePlanResult.Failed(ConnectedTilePlanStatus.InvalidInput,
                        $"Connected tile path vertices {i - 1} and {i} cannot be identical.");
                }
            }

            List<ConnectedTile> linearTiles = type.Tiles
                .Where(tile => tile.ConnectionPoints.Length == 2 && HasUsableFoundation(tile))
                .ToList();
            List<ConnectedTile> endingTiles = type.Tiles
                .Where(tile => tile.ConnectionPoints.Length == 1 && HasUsableFoundation(tile))
                .ToList();

            bool usesEndPieces = useEndPieces && !closed;
            bool strict = usesEndPieces || closed;

            List<SearchNode> frontier;
            if (usesEndPieces)
            {
                frontier = MakeStartingEndingNodes(endingTiles, vertices[0], startingSide, isCellValid);
                if (frontier.Count == 0)
                {
                    return ConnectedTilePlanResult.Failed(ConnectedTilePlanStatus.NoSolution,
                        "No compatible starting ending piece can be placed at the first vertex.");
                }
            }
            else
            {
                frontier = new List<SearchNode>
                {
                    SearchNode.MakeSyntheticStart(vertices[0], startingSide)
                };
            }

            var destinations = vertices.Skip(1).ToList();
            if (closed)
                destinations.Add(vertices[0]);

            bool earlierSearchWasLimited = false;

            for (int segmentIndex = 0; segmentIndex < destinations.Count; segmentIndex++)
            {
                bool isFirstSegment = segmentIndex == 0;
                bool isLastSegment = segmentIndex == destinations.Count - 1;

                if (!isFirstSegment)
                    frontier = BackUpOnePlacement(frontier);

                SegmentGoal goal = isLastSegment && closed
                    ? SegmentGoal.ClosedSeam
                    : isLastSegment && usesEndPieces
                        ? SegmentGoal.EndingPiece
                        : SegmentGoal.Point;

                int exactResultLimit = strict && !isLastSegment ? MaxBoundaryStates : 1;
                SegmentSearchResult searchResult = SearchSegment(
                    frontier,
                    destinations[segmentIndex],
                    vertices[0],
                    linearTiles,
                    endingTiles,
                    allowSideChangingTiles: !isFirstSegment || strict,
                    goal,
                    randomSeed,
                    exactResultLimit,
                    strict,
                    isCellValid);

                earlierSearchWasLimited |= searchResult.SearchWasLimited;

                if (searchResult.ExactNodes.Count > 0)
                {
                    frontier = searchResult.ExactNodes;
                    continue;
                }

                if (!strict && searchResult.BestNode != null)
                {
                    frontier = new List<SearchNode> { searchResult.BestNode };
                    continue;
                }

                ConnectedTilePlanStatus failureStatus = earlierSearchWasLimited
                    ? ConnectedTilePlanStatus.SearchLimit
                    : ConnectedTilePlanStatus.NoSolution;

                return ConnectedTilePlanResult.Failed(failureStatus,
                    failureStatus == ConnectedTilePlanStatus.SearchLimit
                        ? "The connected tile search limit was reached before an exact formation was found."
                        : "No exact connected tile formation matches the requested path.");
            }

            SearchNode finalNode = frontier.Count > 0 ? frontier[0] : null;
            if (finalNode == null)
            {
                return ConnectedTilePlanResult.Failed(ConnectedTilePlanStatus.NoSolution,
                    "No connected tile placements were found.");
            }

            var placements = BuildPlacements(finalNode);
            var plan = new ConnectedTilePlan(type, vertices[0], placements, closed, usesEndPieces);
            return ConnectedTilePlanResult.Succeeded(plan);
        }

        private static SegmentSearchResult SearchSegment(IReadOnlyList<SearchNode> seeds, Point2D destination,
            Point2D origin, IReadOnlyList<ConnectedTile> linearTiles, IReadOnlyList<ConnectedTile> endingTiles,
            bool allowSideChangingTiles, SegmentGoal goal, int randomSeed, int exactResultLimit, bool strict,
            Func<Point2D, bool> isCellValid)
        {
            var result = new SegmentSearchResult();
            var openSet = new PriorityQueue<SearchNode, (float Score, int ExtraPriority, long Sequence)>();
            long sequence = 0;
            int maxExpandedNodes = strict
                ? MaxExpandedNodesPerStrictSegment
                : MaxExpandedNodesPerLegacySegment;
            int maxQueuedNodes = strict
                ? MaxQueuedNodesPerStrictSegment
                : MaxQueuedNodesPerLegacySegment;

            foreach (SearchNode seed in seeds)
            {
                if (seed?.ExitPoint == null)
                    continue;

                if (openSet.Count >= maxQueuedNodes)
                {
                    result.SearchWasLimited = true;
                    break;
                }

                openSet.Enqueue(seed, (GetNodePriorityScore(seed, destination, randomSeed),
                    seed.Tile?.ExtraPriority ?? 0, sequence++));
            }

            float bestDistance = float.PositiveInfinity;
            int expandedNodeCount = 0;
            bool hitDepthLimit = false;

            while (openSet.Count > 0)
            {
                if (expandedNodeCount >= maxExpandedNodes || openSet.Count > maxQueuedNodes)
                {
                    result.SearchWasLimited = true;
                    break;
                }

                SearchNode currentNode = openSet.Dequeue();
                expandedNodeCount++;

                float currentDistance = Distance(currentNode.ExitCoordinates, destination);
                if (currentDistance < bestDistance)
                {
                    bestDistance = currentDistance;
                    result.BestNode = currentNode;
                }

                if (goal == SegmentGoal.Point && currentNode.ExitCoordinates == destination)
                {
                    result.ExactNodes.Add(currentNode);
                    if (result.ExactNodes.Count >= exactResultLimit)
                    {
                        if (strict && exactResultLimit > 1 && openSet.Count > 0)
                            result.SearchWasLimited = true;

                        break;
                    }

                    continue;
                }

                if (goal == SegmentGoal.ClosedSeam && CanCloseAtOrigin(currentNode, origin))
                {
                    result.ExactNodes.Add(currentNode);
                    break;
                }

                if (goal == SegmentGoal.EndingPiece)
                {
                    List<SearchNode> terminalNodes = GetTerminalEndingNodes(
                        currentNode, endingTiles, destination, randomSeed, isCellValid);

                    if (terminalNodes.Count > 0)
                    {
                        result.ExactNodes.Add(terminalNodes[0]);
                        break;
                    }
                }

                if (currentNode.Depth >= MaxPlacementCount)
                {
                    hitDepthLimit = true;
                    continue;
                }

                bool queueLimitReached = false;
                foreach (ConnectedTile tile in linearTiles)
                {
                    if (!allowSideChangingTiles && tile.ConnectionPoints[0].Side != tile.ConnectionPoints[1].Side)
                        continue;

                    int remainingQueueCapacity = maxQueuedNodes - openSet.Count;
                    if (remainingQueueCapacity <= 0)
                    {
                        queueLimitReached = true;
                        break;
                    }

                    foreach (SearchNode nextNode in GetNextLinearNodes(
                        currentNode, tile, remainingQueueCapacity, isCellValid))
                    {
                        openSet.Enqueue(nextNode, (GetNodePriorityScore(nextNode, destination, randomSeed),
                            nextNode.Tile.ExtraPriority, sequence++));
                    }

                    if (openSet.Count >= maxQueuedNodes)
                    {
                        queueLimitReached = true;
                        break;
                    }
                }

                if (queueLimitReached)
                {
                    result.SearchWasLimited = true;
                    continue;
                }
            }

            if (hitDepthLimit)
                result.SearchWasLimited = true;

            if (result.BestNode == null && seeds.Count > 0)
                result.BestNode = seeds[0];

            return result;
        }

        private static IEnumerable<SearchNode> GetNextLinearNodes(SearchNode currentNode, ConnectedTile tile,
            int maxNodeCount, Func<Point2D, bool> isCellValid)
        {
            int generatedNodeCount = 0;

            for (int entryIndex = 0; entryIndex < tile.ConnectionPoints.Length; entryIndex++)
            {
                TileConnectionPoint entryPoint = tile.ConnectionPoints[entryIndex];
                if (!CanEnterTile(currentNode, tile, entryPoint))
                    continue;

                byte directionMask = (byte)(entryPoint.ReversedConnectionMask & currentNode.ExitPoint.Value.ConnectionMask);
                foreach (Direction direction in Helpers.GetDirectionsInMask(directionMask))
                {
                    Point2D placementOffset = Helpers.VisualDirectionToPoint(direction) - entryPoint.CoordinateOffset;
                    Point2D placementLocation = currentNode.ExitCoordinates + placementOffset;

                    if (!TryAddFoundation(currentNode.OccupiedCells, tile, placementLocation,
                        isCellValid, out HashSet<Point2D> occupiedCells))
                    {
                        continue;
                    }

                    TileConnectionPoint exitPoint = tile.ConnectionPoints[entryIndex == 0 ? 1 : 0];
                    Point2D exitCoordinates = placementLocation + exitPoint.CoordinateOffset;
                    float gScore = currentNode.GScore + Distance(currentNode.ExitCoordinates, exitCoordinates);

                    generatedNodeCount++;
                    yield return new SearchNode(currentNode, tile, placementLocation,
                        entryPoint, exitPoint, occupiedCells, gScore);

                    if (generatedNodeCount >= maxNodeCount)
                        yield break;
                }
            }
        }

        private static List<SearchNode> MakeStartingEndingNodes(IReadOnlyList<ConnectedTile> endingTiles,
            Point2D origin, ConnectedTileSide startingSide, Func<Point2D, bool> isCellValid)
        {
            var nodes = new List<SearchNode>();

            foreach (ConnectedTile endingTile in endingTiles)
            {
                TileConnectionPoint point = endingTile.ConnectionPoints[0];
                if (point.Side != startingSide)
                    continue;

                Point2D location = origin - point.CoordinateOffset;
                if (!TryAddFoundation(new HashSet<Point2D>(), endingTile, location,
                    isCellValid, out HashSet<Point2D> occupiedCells))
                {
                    continue;
                }

                nodes.Add(new SearchNode(null, endingTile, location, null, point, occupiedCells, 0.0f));
            }

            return nodes;
        }

        private static List<SearchNode> GetTerminalEndingNodes(SearchNode currentNode,
            IReadOnlyList<ConnectedTile> endingTiles, Point2D destination, int randomSeed,
            Func<Point2D, bool> isCellValid)
        {
            var terminalNodes = new List<SearchNode>();

            foreach (ConnectedTile endingTile in endingTiles)
            {
                TileConnectionPoint entryPoint = endingTile.ConnectionPoints[0];
                if (!CanEnterTile(currentNode, endingTile, entryPoint))
                    continue;

                byte directionMask = (byte)(entryPoint.ReversedConnectionMask & currentNode.ExitPoint.Value.ConnectionMask);
                foreach (Direction direction in Helpers.GetDirectionsInMask(directionMask))
                {
                    Point2D placementOffset = Helpers.VisualDirectionToPoint(direction) - entryPoint.CoordinateOffset;
                    Point2D location = currentNode.ExitCoordinates + placementOffset;
                    Point2D entryCoordinates = location + entryPoint.CoordinateOffset;

                    if (entryCoordinates != destination)
                        continue;

                    if (!TryAddFoundation(currentNode.OccupiedCells, endingTile, location,
                        isCellValid, out HashSet<Point2D> occupiedCells))
                    {
                        continue;
                    }

                    float gScore = currentNode.GScore + Distance(currentNode.ExitCoordinates, entryCoordinates);
                    terminalNodes.Add(new SearchNode(currentNode, endingTile, location,
                        entryPoint, null, occupiedCells, gScore));
                }
            }

            terminalNodes.Sort((left, right) =>
            {
                int priorityComparison = left.Tile.ExtraPriority.CompareTo(right.Tile.ExtraPriority);
                if (priorityComparison != 0)
                    return priorityComparison;

                return GetStableNodeHash(left, randomSeed).CompareTo(GetStableNodeHash(right, randomSeed));
            });

            return terminalNodes;
        }

        private static bool CanEnterTile(SearchNode currentNode, ConnectedTile candidateTile,
            TileConnectionPoint candidateEntry)
        {
            if (currentNode.ExitPoint == null || candidateEntry.Side != currentNode.ExitPoint.Value.Side)
                return false;

            if (currentNode.Tile != null && !ConnectionPointAllowsTile(candidateEntry, currentNode.Tile.Index))
                return false;

            // Existing two-point configurations apply RequiredTiles / ForbiddenTiles from the
            // incoming candidate only. Preserve that behavior, while allowing a one-point start
            // cap to constrain the first linear tile through its sole outgoing connection.
            if (currentNode.Tile?.IsEndingPiece == true &&
                !ConnectionPointAllowsTile(currentNode.ExitPoint.Value, candidateTile.Index))
            {
                return false;
            }

            return (candidateEntry.ReversedConnectionMask & currentNode.ExitPoint.Value.ConnectionMask) != 0;
        }

        private static bool CanCloseAtOrigin(SearchNode finalNode, Point2D origin)
        {
            SearchNode firstNode = finalNode.FirstPlacement;
            if (finalNode.Tile == null || finalNode.ExitPoint == null || firstNode?.EntryPoint == null)
                return false;

            if (finalNode.Depth < 2 || finalNode.ExitCoordinates != origin)
                return false;

            TileConnectionPoint finalExit = finalNode.ExitPoint.Value;
            TileConnectionPoint firstEntry = firstNode.EntryPoint.Value;

            if (finalExit.Side != firstEntry.Side || !ConnectionPointAllowsTile(firstEntry, finalNode.Tile.Index))
                return false;

            Point2D firstEntryCoordinates = firstNode.Location + firstEntry.CoordinateOffset;
            Point2D delta = firstEntryCoordinates - finalNode.ExitCoordinates;
            byte compatibleDirections = (byte)(finalExit.ConnectionMask & firstEntry.ReversedConnectionMask);

            foreach (Direction direction in Helpers.GetDirectionsInMask(compatibleDirections))
            {
                if (Helpers.VisualDirectionToPoint(direction) == delta)
                    return true;
            }

            return false;
        }

        private static bool ConnectionPointAllowsTile(TileConnectionPoint point, int tileIndex)
        {
            if (point.RequiredTiles?.Length > 0)
                return point.RequiredTiles.Contains(tileIndex);

            return point.ForbiddenTiles == null || !point.ForbiddenTiles.Contains(tileIndex);
        }

        private static bool TryAddFoundation(HashSet<Point2D> existingOccupiedCells, ConnectedTile tile,
            Point2D location, Func<Point2D, bool> isCellValid, out HashSet<Point2D> occupiedCells)
        {
            occupiedCells = null;
            if (!HasUsableFoundation(tile))
                return false;

            foreach (Point2D foundationCell in tile.Foundation)
            {
                Point2D absoluteCell = foundationCell + location;
                if (!isCellValid(absoluteCell) || existingOccupiedCells.Contains(absoluteCell))
                    return false;
            }

            occupiedCells = new HashSet<Point2D>(existingOccupiedCells);
            foreach (Point2D foundationCell in tile.Foundation)
                occupiedCells.Add(foundationCell + location);

            return true;
        }

        private static bool HasUsableFoundation(ConnectedTile tile)
            => tile.Foundation != null && tile.Foundation.Count > 0;

        private static List<SearchNode> BackUpOnePlacement(IEnumerable<SearchNode> nodes)
        {
            var backedUpNodes = new List<SearchNode>();
            foreach (SearchNode node in nodes)
            {
                SearchNode backedUpNode = node.Parent ?? node;
                if (!backedUpNodes.Contains(backedUpNode))
                    backedUpNodes.Add(backedUpNode);
            }

            return backedUpNodes;
        }

        private static List<ConnectedTilePlacement> BuildPlacements(SearchNode finalNode)
        {
            var placements = new List<ConnectedTilePlacement>();
            SearchNode node = finalNode;

            while (node != null)
            {
                if (node.Tile != null)
                {
                    placements.Add(new ConnectedTilePlacement(node.Tile, node.Location,
                        node.EntryPoint, node.ExitPoint));
                }

                node = node.Parent;
            }

            placements.Reverse();
            return placements;
        }

        private static float GetNodePriorityScore(SearchNode node, Point2D destination, int randomSeed)
        {
            float hScore = Distance(node.ExitCoordinates, destination);
            float fScore = node.GScore * 0.7f + hScore + (node.Tile?.DistanceModifier ?? 0);
            if (node.Tile == null)
                return fScore;

            int foundationCellCount = Math.Max(node.Tile.Foundation?.Count ?? 1, 1);
            float sizePenalty = (foundationCellCount - 1) * TileSizePenaltyPerFoundationCell;
            float repeatedWeightedUseCount = CountPreviousTileUsesInRecentAncestors(
                node, RepeatPenaltyAncestorWindow);
            float repeatPenalty = repeatedWeightedUseCount * RepeatPenaltyPerWeightedUse *
                (1.0f + (foundationCellCount - 1) * RepeatPenaltyPerFoundationCell);
            float turnPenalty = IsTurningTile(node.Tile) ? TurnTilePenalty : 0.0f;

            uint hash = GetStableNodeHash(node, randomSeed);
            float jitter = ((hash & 1023) / 1023.0f - 0.5f) * 2.0f * NodeScoreJitterAmplitude;

            return fScore + sizePenalty + repeatPenalty + turnPenalty + jitter;
        }

        private static uint GetStableNodeHash(SearchNode node, int randomSeed)
        {
            uint hash = 2166136261;
            hash = MixHash(hash, unchecked((uint)randomSeed));
            hash = MixHash(hash, unchecked((uint)node.Location.X));
            hash = MixHash(hash, unchecked((uint)node.Location.Y));
            hash = MixHash(hash, unchecked((uint)(node.ExitPoint?.Index ?? -1)));
            hash = MixHash(hash, unchecked((uint)(node.Tile?.Index ?? -1)));
            return hash;
        }

        private static uint MixHash(uint hash, uint value)
        {
            hash ^= value;
            hash *= 16777619;
            return hash;
        }

        private static bool IsTurningTile(ConnectedTile tile)
        {
            if (tile.ConnectionPoints.Length != 2)
                return false;

            int oppositeMask = tile.ConnectionPoints[0].ConnectionMask &
                               tile.ConnectionPoints[1].ReversedConnectionMask;
            if (oppositeMask == 0)
                return true;

            return !tile.ConnectionPoints[0].CoordinateOffset
                .IsInStraightLineWith(tile.ConnectionPoints[1].CoordinateOffset);
        }

        private static float CountPreviousTileUsesInRecentAncestors(SearchNode node, int ancestorWindow)
        {
            if (node.Tile == null)
                return 0.0f;

            float weightedUses = 0.0f;
            int tileIndex = node.Tile.Index;
            int depth = 1;
            SearchNode current = node.Parent;

            while (current != null && depth <= ancestorWindow)
            {
                if (current.Tile?.Index == tileIndex)
                {
                    float recencyWeight = (ancestorWindow - depth + 1) / (float)ancestorWindow;
                    weightedUses += recencyWeight;
                }

                current = current.Parent;
                depth++;
            }

            return weightedUses;
        }

        private static float Distance(Point2D first, Point2D second)
            => Vector2.Distance(first.ToXNAVector(), second.ToXNAVector());
    }
}
