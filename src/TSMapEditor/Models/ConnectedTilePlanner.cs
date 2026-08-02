using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
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
            List<ConnectedTilePlacement> placements, bool isClosed, bool usesEndPieces)
        {
            Type = type;
            Origin = origin;
            Placements = new ReadOnlyCollection<ConnectedTilePlacement>(placements);
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
        private const int MaxInitialBoundaryStates = 8;
        private const int MaxStackAllocatedPathVertexCount = 256;

        private const float TileSizePenaltyPerFoundationCell = 0.07f;
        private const int RepeatPenaltyAncestorWindow = 5;
        private const float RepeatPenaltyPerWeightedUse = 0.75f;
        private const float RepeatPenaltyPerFoundationCell = 0.12f;
        private const float TurnTilePenalty = 1.0f;
        private const float NodeScoreJitterAmplitude = 0.02f;

        private static readonly ConditionalWeakTable<ConnectedTileType, SearchTileCache> SearchTileCaches = new();

        private enum SegmentGoal
        {
            Point,
            EndingPiece,
            ClosedSeam
        }

        private readonly struct SearchTile
        {
            public SearchTile(ConnectedTile tile)
            {
                Tile = tile;
                FoundationCells = new Point2D[tile.Foundation.Count];
                tile.Foundation.CopyTo(FoundationCells);
            }

            public ConnectedTile Tile { get; }
            public Point2D[] FoundationCells { get; }
        }

        private sealed class SearchTileCache
        {
            public SearchTileCache(ConnectedTileType type)
            {
                Tiles = type.Tiles.ToArray();
                Foundations = new HashSet<Point2D>[Tiles.Length];
                FoundationCounts = new int[Tiles.Length];

                int linearTileCount = 0;
                int endingTileCount = 0;
                for (int i = 0; i < Tiles.Length; i++)
                {
                    ConnectedTile tile = Tiles[i];
                    Foundations[i] = tile.Foundation;
                    FoundationCounts[i] = tile.Foundation?.Count ?? 0;
                    if (!HasUsableFoundation(tile))
                        continue;

                    if (tile.ConnectionPoints.Length == 2)
                        linearTileCount++;
                    else if (tile.ConnectionPoints.Length == 1)
                        endingTileCount++;
                }

                LinearTiles = new SearchTile[linearTileCount];
                EndingTiles = new SearchTile[endingTileCount];

                int linearTileIndex = 0;
                int endingTileIndex = 0;
                for (int i = 0; i < Tiles.Length; i++)
                {
                    ConnectedTile tile = Tiles[i];
                    if (!HasUsableFoundation(tile))
                        continue;

                    if (tile.ConnectionPoints.Length == 2)
                        LinearTiles[linearTileIndex++] = new SearchTile(tile);
                    else if (tile.ConnectionPoints.Length == 1)
                        EndingTiles[endingTileIndex++] = new SearchTile(tile);
                }
            }

            public ConnectedTile[] Tiles { get; }
            public HashSet<Point2D>[] Foundations { get; }
            public int[] FoundationCounts { get; }
            public SearchTile[] LinearTiles { get; }
            public SearchTile[] EndingTiles { get; }

            public bool Matches(ConnectedTileType type)
            {
                if (type.Tiles.Count != Tiles.Length)
                    return false;

                for (int i = 0; i < Tiles.Length; i++)
                {
                    ConnectedTile tile = type.Tiles[i];
                    if (!ReferenceEquals(tile, Tiles[i]) ||
                        !ReferenceEquals(tile.Foundation, Foundations[i]) ||
                        (tile.Foundation?.Count ?? 0) != FoundationCounts[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private sealed class SearchNode
        {
            private SearchNode() { }

            public SearchNode(SearchNode parent, SearchTile searchTile, Point2D location,
                int entryPointIndex, int exitPointIndex, float gScore)
            {
                Parent = parent;
                SearchTile = searchTile;
                Location = location;
                EntryPointIndex = entryPointIndex;
                ExitPointIndex = exitPointIndex;
                GScore = gScore;
                Depth = (parent?.Depth ?? 0) + 1;

                if (parent?.FirstPlacement != null)
                    FirstPlacement = parent.FirstPlacement;
                else if (searchTile.Tile != null)
                    FirstPlacement = this;
            }

            public SearchNode Parent { get; private init; }
            public SearchNode FirstPlacement { get; private set; }
            public SearchTile SearchTile { get; private init; }
            public ConnectedTile Tile => SearchTile.Tile;
            public Point2D Location { get; private init; }
            public int EntryPointIndex { get; private init; }
            public int ExitPointIndex { get; private init; }
            public float GScore { get; private init; }
            public int Depth { get; private init; }
            public ConnectedTileSide SyntheticStartingSide { get; private init; }

            public bool HasExitPoint => Tile == null || ExitPointIndex >= 0;

            public TileConnectionPoint ExitPoint
            {
                get
                {
                    if (Tile != null)
                        return Tile.ConnectionPoints[ExitPointIndex];

                    return new TileConnectionPoint
                    {
                        Index = -1,
                        ConnectionMask = byte.MaxValue,
                        CoordinateOffset = Point2D.Zero,
                        Side = SyntheticStartingSide,
                        RequiredTiles = Array.Empty<int>(),
                        ForbiddenTiles = Array.Empty<int>()
                    };
                }
            }

            public Point2D ExitCoordinates => Tile == null
                ? Location
                : Location + Tile.ConnectionPoints[ExitPointIndex].CoordinateOffset;

            public static SearchNode MakeSyntheticStart(Point2D location, ConnectedTileSide startingSide)
            {
                return new SearchNode
                {
                    Location = location,
                    EntryPointIndex = -1,
                    ExitPointIndex = -1,
                    SyntheticStartingSide = startingSide,
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
                return ConnectedTilePlanResult.Failed(ConnectedTilePlanStatus.InvalidInput, "A connected tile type is required.");

            if (path == null)
                return ConnectedTilePlanResult.Failed(ConnectedTilePlanStatus.InvalidInput, "A connected tile path is required.");

            if (isCellValid == null)
                return ConnectedTilePlanResult.Failed(ConnectedTilePlanStatus.InvalidInput, "A map-cell validator is required.");

            Span<Point2D> vertices = path.Count <= MaxStackAllocatedPathVertexCount
                ? stackalloc Point2D[path.Count]
                : new Point2D[path.Count];
            for (int i = 0; i < path.Count; i++)
                vertices[i] = path[i];

            int vertexCount = vertices.Length;
            if (closed && vertexCount > 1 && vertices[0] == vertices[vertexCount - 1])
                vertexCount--;

            int minimumVertexCount = closed ? 3 : 2;
            if (vertexCount < minimumVertexCount)
            {
                return ConnectedTilePlanResult.Failed(ConnectedTilePlanStatus.InvalidInput,
                    closed
                        ? "A closed connected tile path requires at least three vertices."
                        : "A connected tile path requires at least two vertices.");
            }

            if (closed && !HasThreeDistinctVertices(vertices[..vertexCount]))
            {
                return ConnectedTilePlanResult.Failed(ConnectedTilePlanStatus.InvalidInput,
                    "A closed connected tile path requires at least three distinct vertices.");
            }

            for (int i = 0; i < vertexCount; i++)
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

            SearchTileCache searchTileCache = GetSearchTileCache(type);
            SearchTile[] linearTiles = searchTileCache.LinearTiles;
            SearchTile[] endingTiles = searchTileCache.EndingTiles;

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

            var openSet = new PriorityQueue<SearchNode, (float Score, int ExtraPriority, long Sequence)>();
            bool earlierSearchWasLimited = false;
            int segmentCount = vertexCount - 1 + (closed ? 1 : 0);

            for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                bool isFirstSegment = segmentIndex == 0;
                bool isLastSegment = segmentIndex == segmentCount - 1;
                Point2D destination = closed && isLastSegment
                    ? vertices[0]
                    : vertices[segmentIndex + 1];

                if (!isFirstSegment)
                    frontier = BackUpOnePlacement(frontier);

                SegmentGoal goal = isLastSegment && closed
                    ? SegmentGoal.ClosedSeam
                    : isLastSegment && usesEndPieces
                        ? SegmentGoal.EndingPiece
                        : SegmentGoal.Point;

                // A broad initial frontier can include paths that reach the first vertex by circling
                // most of a closed outline, effectively erasing the requested starting side.
                int exactResultLimit;
                if (!strict || isLastSegment)
                    exactResultLimit = 1;
                else if (isFirstSegment)
                    exactResultLimit = MaxInitialBoundaryStates;
                else
                    exactResultLimit = MaxBoundaryStates;

                SegmentSearchResult searchResult = SearchSegment(
                    openSet,
                    frontier,
                    destination,
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

        private static SegmentSearchResult SearchSegment(
            PriorityQueue<SearchNode, (float Score, int ExtraPriority, long Sequence)> openSet,
            IReadOnlyList<SearchNode> seeds, Point2D destination,
            Point2D origin, SearchTile[] linearTiles, SearchTile[] endingTiles,
            bool allowSideChangingTiles, SegmentGoal goal, int randomSeed, int exactResultLimit, bool strict,
            Func<Point2D, bool> isCellValid)
        {
            var result = new SegmentSearchResult();
            openSet.Clear();
            long sequence = 0;
            int maxExpandedNodes = strict
                ? MaxExpandedNodesPerStrictSegment
                : MaxExpandedNodesPerLegacySegment;
            int maxQueuedNodes = strict
                ? MaxQueuedNodesPerStrictSegment
                : MaxQueuedNodesPerLegacySegment;

            for (int seedIndex = 0; seedIndex < seeds.Count; seedIndex++)
            {
                SearchNode seed = seeds[seedIndex];
                if (seed == null || !seed.HasExitPoint)
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
                    SearchNode terminalNode = GetBestTerminalEndingNode(
                        currentNode, endingTiles, destination, randomSeed, isCellValid);
                    if (terminalNode != null)
                    {
                        result.ExactNodes.Add(terminalNode);
                        break;
                    }
                }

                if (currentNode.Depth >= MaxPlacementCount)
                {
                    hitDepthLimit = true;
                    continue;
                }

                bool queueLimitReached = false;
                for (int tileIndex = 0; tileIndex < linearTiles.Length; tileIndex++)
                {
                    SearchTile searchTile = linearTiles[tileIndex];
                    ConnectedTile tile = searchTile.Tile;
                    if (!allowSideChangingTiles &&
                        tile.ConnectionPoints[0].Side != tile.ConnectionPoints[1].Side)
                        continue;

                    int remainingQueueCapacity = maxQueuedNodes - openSet.Count;
                    if (remainingQueueCapacity <= 0)
                    {
                        queueLimitReached = true;
                        break;
                    }

                    int generatedNodeCount = EnqueueNextLinearNodes(
                        openSet, currentNode, searchTile, destination, randomSeed,
                        remainingQueueCapacity, isCellValid, ref sequence);

                    if (generatedNodeCount >= remainingQueueCapacity)
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

        private static int EnqueueNextLinearNodes(
            PriorityQueue<SearchNode, (float Score, int ExtraPriority, long Sequence)> openSet,
            SearchNode currentNode, SearchTile searchTile, Point2D destination, int randomSeed,
            int maxNodeCount, Func<Point2D, bool> isCellValid, ref long sequence)
        {
            int generatedNodeCount = 0;
            ConnectedTile tile = searchTile.Tile;
            TileConnectionPoint currentExitPoint = currentNode.ExitPoint;
            Point2D currentExitCoordinates = currentNode.ExitCoordinates;

            for (int entryIndex = 0; entryIndex < tile.ConnectionPoints.Length; entryIndex++)
            {
                TileConnectionPoint entryPoint = tile.ConnectionPoints[entryIndex];
                if (!CanEnterTile(currentNode, tile, entryPoint))
                    continue;

                byte directionMask = (byte)(entryPoint.ReversedConnectionMask & currentExitPoint.ConnectionMask);
                for (int directionIndex = 0; directionIndex < (int)Direction.Count; directionIndex++)
                {
                    if ((directionMask & (byte)(0b10000000 >> directionIndex)) == 0)
                        continue;

                    Direction direction = (Direction)directionIndex;
                    Point2D placementOffset = Helpers.VisualDirectionToPoint(direction) - entryPoint.CoordinateOffset;
                    Point2D placementLocation = currentExitCoordinates + placementOffset;

                    if (!CanPlaceFoundation(currentNode, searchTile, placementLocation, isCellValid))
                        continue;

                    TileConnectionPoint exitPoint = tile.ConnectionPoints[entryIndex == 0 ? 1 : 0];
                    Point2D exitCoordinates = placementLocation + exitPoint.CoordinateOffset;
                    float gScore = currentNode.GScore + Distance(currentExitCoordinates, exitCoordinates);

                    generatedNodeCount++;
                    var nextNode = new SearchNode(currentNode, searchTile, placementLocation,
                        entryIndex, entryIndex == 0 ? 1 : 0, gScore);
                    openSet.Enqueue(nextNode, (GetNodePriorityScore(nextNode, destination, randomSeed),
                        tile.ExtraPriority, sequence++));

                    if (generatedNodeCount >= maxNodeCount)
                        return generatedNodeCount;
                }
            }

            return generatedNodeCount;
        }

        private static List<SearchNode> MakeStartingEndingNodes(SearchTile[] endingTiles,
            Point2D origin, ConnectedTileSide startingSide, Func<Point2D, bool> isCellValid)
        {
            var nodes = new List<SearchNode>();

            for (int tileIndex = 0; tileIndex < endingTiles.Length; tileIndex++)
            {
                SearchTile searchTile = endingTiles[tileIndex];
                ConnectedTile endingTile = searchTile.Tile;
                TileConnectionPoint point = endingTile.ConnectionPoints[0];
                if (point.Side != startingSide)
                    continue;

                Point2D location = origin - point.CoordinateOffset;
                if (!CanPlaceFoundation(null, searchTile, location, isCellValid))
                    continue;

                nodes.Add(new SearchNode(null, searchTile, location, -1, 0, 0.0f));
            }

            return nodes;
        }

        private static SearchNode GetBestTerminalEndingNode(SearchNode currentNode,
            SearchTile[] endingTiles, Point2D destination, int randomSeed,
            Func<Point2D, bool> isCellValid)
        {
            SearchNode bestNode = null;
            TileConnectionPoint currentExitPoint = currentNode.ExitPoint;
            Point2D currentExitCoordinates = currentNode.ExitCoordinates;

            for (int tileIndex = 0; tileIndex < endingTiles.Length; tileIndex++)
            {
                SearchTile searchTile = endingTiles[tileIndex];
                ConnectedTile endingTile = searchTile.Tile;
                TileConnectionPoint entryPoint = endingTile.ConnectionPoints[0];
                if (!CanEnterTile(currentNode, endingTile, entryPoint))
                    continue;

                byte directionMask = (byte)(entryPoint.ReversedConnectionMask & currentExitPoint.ConnectionMask);
                for (int directionIndex = 0; directionIndex < (int)Direction.Count; directionIndex++)
                {
                    if ((directionMask & (byte)(0b10000000 >> directionIndex)) == 0)
                        continue;

                    Direction direction = (Direction)directionIndex;
                    Point2D placementOffset = Helpers.VisualDirectionToPoint(direction) - entryPoint.CoordinateOffset;
                    Point2D location = currentExitCoordinates + placementOffset;
                    Point2D entryCoordinates = location + entryPoint.CoordinateOffset;

                    if (entryCoordinates != destination)
                        continue;

                    if (!CanPlaceFoundation(currentNode, searchTile, location, isCellValid))
                        continue;

                    float gScore = currentNode.GScore + Distance(currentExitCoordinates, entryCoordinates);
                    var terminalNode = new SearchNode(currentNode, searchTile, location,
                        0, -1, gScore);
                    if (bestNode == null || IsBetterTerminalNode(terminalNode, bestNode, randomSeed))
                        bestNode = terminalNode;
                }
            }

            return bestNode;
        }

        private static bool IsBetterTerminalNode(SearchNode candidate, SearchNode currentBest, int randomSeed)
        {
            if (candidate.Tile.ExtraPriority != currentBest.Tile.ExtraPriority)
                return candidate.Tile.ExtraPriority < currentBest.Tile.ExtraPriority;

            return GetStableNodeHash(candidate, randomSeed) < GetStableNodeHash(currentBest, randomSeed);
        }

        private static bool CanEnterTile(SearchNode currentNode, ConnectedTile candidateTile,
            TileConnectionPoint candidateEntry)
        {
            if (!currentNode.HasExitPoint)
                return false;

            TileConnectionPoint currentExitPoint = currentNode.ExitPoint;
            if (candidateEntry.Side != currentExitPoint.Side)
                return false;

            if (currentNode.Tile != null && !ConnectionPointAllowsTile(candidateEntry, currentNode.Tile.Index))
                return false;

            // Existing two-point configurations apply RequiredTiles / ForbiddenTiles from the
            // incoming candidate only. Preserve that behavior, while allowing a one-point start
            // cap to constrain the first linear tile through its sole outgoing connection.
            if (currentNode.Tile?.IsEndingPiece == true &&
                !ConnectionPointAllowsTile(currentExitPoint, candidateTile.Index))
            {
                return false;
            }

            return (candidateEntry.ReversedConnectionMask & currentExitPoint.ConnectionMask) != 0;
        }

        private static bool CanCloseAtOrigin(SearchNode finalNode, Point2D origin)
        {
            SearchNode firstNode = finalNode.FirstPlacement;
            if (finalNode.Tile == null || !finalNode.HasExitPoint ||
                firstNode == null || firstNode.EntryPointIndex < 0)
                return false;

            if (finalNode.Depth < 2 || finalNode.ExitCoordinates != origin)
                return false;

            TileConnectionPoint finalExit = finalNode.ExitPoint;
            TileConnectionPoint firstEntry = firstNode.Tile.ConnectionPoints[firstNode.EntryPointIndex];

            if (finalExit.Side != firstEntry.Side || !ConnectionPointAllowsTile(firstEntry, finalNode.Tile.Index))
                return false;

            Point2D firstEntryCoordinates = firstNode.Location + firstEntry.CoordinateOffset;
            Point2D delta = firstEntryCoordinates - finalNode.ExitCoordinates;
            byte compatibleDirections = (byte)(finalExit.ConnectionMask & firstEntry.ReversedConnectionMask);

            for (int directionIndex = 0; directionIndex < (int)Direction.Count; directionIndex++)
            {
                if ((compatibleDirections & (byte)(0b10000000 >> directionIndex)) == 0)
                    continue;

                Direction direction = (Direction)directionIndex;
                if (Helpers.VisualDirectionToPoint(direction) == delta)
                    return true;
            }

            return false;
        }

        private static bool ConnectionPointAllowsTile(TileConnectionPoint point, int tileIndex)
        {
            if (point.RequiredTiles?.Length > 0)
            {
                for (int i = 0; i < point.RequiredTiles.Length; i++)
                {
                    if (point.RequiredTiles[i] == tileIndex)
                        return true;
                }

                return false;
            }

            if (point.ForbiddenTiles == null)
                return true;

            for (int i = 0; i < point.ForbiddenTiles.Length; i++)
            {
                if (point.ForbiddenTiles[i] == tileIndex)
                    return false;
            }

            return true;
        }

        private static bool CanPlaceFoundation(SearchNode parent, SearchTile searchTile,
            Point2D location, Func<Point2D, bool> isCellValid)
        {
            Point2D[] foundationCells = searchTile.FoundationCells;
            for (int foundationIndex = 0; foundationIndex < foundationCells.Length; foundationIndex++)
            {
                Point2D foundationCell = foundationCells[foundationIndex];
                Point2D absoluteCell = foundationCell + location;
                if (!isCellValid(absoluteCell))
                    return false;

                SearchNode ancestor = parent;
                while (ancestor != null)
                {
                    if (ancestor.Tile != null)
                    {
                        Point2D[] occupiedFoundationCells = ancestor.SearchTile.FoundationCells;
                        for (int occupiedIndex = 0; occupiedIndex < occupiedFoundationCells.Length;
                             occupiedIndex++)
                        {
                            Point2D occupiedFoundationCell = occupiedFoundationCells[occupiedIndex];
                            if (occupiedFoundationCell + ancestor.Location == absoluteCell)
                                return false;
                        }
                    }

                    ancestor = ancestor.Parent;
                }
            }

            return true;
        }

        private static SearchTileCache GetSearchTileCache(ConnectedTileType type)
        {
            if (SearchTileCaches.TryGetValue(type, out SearchTileCache cache) && cache.Matches(type))
                return cache;

            SearchTileCaches.Remove(type);
            return SearchTileCaches.GetValue(type, static connectedTileType => new SearchTileCache(connectedTileType));
        }

        private static bool HasUsableFoundation(ConnectedTile tile)
            => tile.Foundation != null && tile.Foundation.Count > 0;

        private static List<SearchNode> BackUpOnePlacement(IReadOnlyList<SearchNode> nodes)
        {
            var backedUpNodes = new List<SearchNode>(nodes.Count);
            for (int nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
            {
                SearchNode node = nodes[nodeIndex];
                SearchNode backedUpNode = node.Parent ?? node;
                if (!backedUpNodes.Contains(backedUpNode))
                    backedUpNodes.Add(backedUpNode);
            }

            return backedUpNodes;
        }

        private static bool HasThreeDistinctVertices(ReadOnlySpan<Point2D> vertices)
        {
            Point2D first = vertices[0];
            int secondIndex = 1;
            while (secondIndex < vertices.Length && vertices[secondIndex] == first)
                secondIndex++;

            if (secondIndex >= vertices.Length)
                return false;

            Point2D second = vertices[secondIndex];
            for (int i = secondIndex + 1; i < vertices.Length; i++)
            {
                if (vertices[i] != first && vertices[i] != second)
                    return true;
            }

            return false;
        }

        private static List<ConnectedTilePlacement> BuildPlacements(SearchNode finalNode)
        {
            var placements = new List<ConnectedTilePlacement>();
            SearchNode node = finalNode;

            while (node != null)
            {
                if (node.Tile != null)
                {
                    TileConnectionPoint? entryPoint = node.EntryPointIndex >= 0
                        ? node.Tile.ConnectionPoints[node.EntryPointIndex]
                        : null;
                    TileConnectionPoint? exitPoint = node.ExitPointIndex >= 0
                        ? node.Tile.ConnectionPoints[node.ExitPointIndex]
                        : null;
                    placements.Add(new ConnectedTilePlacement(node.Tile, node.Location,
                        entryPoint, exitPoint));
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

            int foundationCellCount = Math.Max(node.SearchTile.FoundationCells.Length, 1);
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
            hash = MixHash(hash, unchecked((uint)node.ExitPointIndex));
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
