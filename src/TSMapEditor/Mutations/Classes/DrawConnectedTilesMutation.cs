using System;
using System.Collections.Generic;
using System.Diagnostics;
using TSMapEditor.CCEngine.TileData;
using TSMapEditor.GameMath;
using TSMapEditor.Misc;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation for drawing cliffs.
    /// </summary>
    public class DrawConnectedTilesMutation : Mutation
    {
        public DrawConnectedTilesMutation(IMutationTarget mutationTarget, List<Point2D> path, ConnectedTileType connectedTileType, ConnectedTileSide startingSide, int randomSeed, byte extraHeight) : base(mutationTarget)
        {
            if (path.Count < 2)
            {
                throw new ArgumentException(nameof(DrawConnectedTilesMutation) +
                    ": to draw a connected tile at least 2 path vertices are required.");
            }

            this.path = path;
            this.connectedTileType = connectedTileType;
            this.startingSide = startingSide;

            this.originLevel = mutationTarget.Map.GetTile(path[0]).Level + extraHeight;
            this.randomSeed = randomSeed;
            this.random = new Random(randomSeed);
        }

        private struct ConnectedTileUndoData
        {
            public Point2D CellCoords;
            public int TileIndex;
            public byte SubTileIndex;
            public byte Level;
        }

        private const int MaxTimeInMilliseconds = 10;
        private const float TileSizePenaltyPerFoundationCell = 0.07f;
        private const int RepeatPenaltyAncestorWindow = 5;
        private const float RepeatPenaltyPerWeightedUse = 0.75f;
        private const float RepeatPenaltyPerFoundationCell = 0.12f;
        private const float TurnTilePenalty = 1.0f;
        private const float NodeScoreJitterAmplitude = 0.02f;

        private readonly List<ConnectedTileUndoData> undoData = new List<ConnectedTileUndoData>();

        private readonly List<Point2D> path;
        private readonly ConnectedTileType connectedTileType;
        private readonly ConnectedTileSide startingSide;

        private readonly int originLevel;
        private readonly int randomSeed;
        private readonly Random random;

        private ConnectedTileAStarNode lastNode;

        public override string GetDisplayString()
        {
            return string.Format(Translate(this, "DisplayString",
                "Draw Connected Tiles of type {0}"), connectedTileType.Name);
        }

        public override void Perform()
        {
            lastNode = null;

            for (int i = 0; i < path.Count - 1; i++)
            {
                FindConnectedTilePath(path[i], path[i + 1], i != 0);
            }

            PlaceConnectedTiles(lastNode);

            MutationTarget.InvalidateMap();
        }

        private void FindConnectedTilePath(Point2D start, Point2D end, bool allowInstantTurn)
        {
            PriorityQueue<ConnectedTileAStarNode, (float FScore, int ExtraPriority)> openSet = new();
            List<ConnectedTile> candidateTiles = allowInstantTurn
                ? connectedTileType.Tiles
                : connectedTileType.Tiles.FindAll(static tile => tile.ConnectionPoints[0].Side == tile.ConnectionPoints[1].Side);

            ConnectedTileAStarNode bestNode = null;
            float bestDistance = float.PositiveInfinity;

            if (lastNode == null)
            {
                lastNode = ConnectedTileAStarNode.MakeStartNode(start, end, startingSide);
            }
            else
            {
                // Go back one step if we can, since we didn't know we needed to turn yet
                // and it's likely not gonna be very nice
                lastNode = lastNode.Parent ?? lastNode;
                lastNode.Destination = end;
            }

            long timeoutTimestamp = Stopwatch.GetTimestamp() + TimeSpan.FromMilliseconds(MaxTimeInMilliseconds).Ticks;
            openSet.Enqueue(lastNode, (GetNodePriorityScore(lastNode), lastNode.Tile?.ExtraPriority ?? 0));

            while (openSet.Count > 0)
            {
                ConnectedTileAStarNode currentNode = openSet.Dequeue();
                var nextNodes = currentNode.GetNextNodes(candidateTiles, true);
                for (int i = 0; i < nextNodes.Count; i++)
                {
                    ConnectedTileAStarNode node = nextNodes[i];
                    openSet.Enqueue(node, (GetNodePriorityScore(node), node.Tile?.ExtraPriority ?? 0));
                }

                float currentDistance = currentNode.HScore;

                if (currentDistance < bestDistance)
                {
                    bestNode = currentNode;
                    bestDistance = currentDistance;
                    timeoutTimestamp = Stopwatch.GetTimestamp() + TimeSpan.FromMilliseconds(MaxTimeInMilliseconds).Ticks;
                }

                if (bestDistance == 0 || Stopwatch.GetTimestamp() > timeoutTimestamp)
                    break;
            }

            lastNode = bestNode;
        }

        private float GetNodePriorityScore(ConnectedTileAStarNode node)
        {
            if (node.Tile == null)
                return node.FScore;

            int foundationCellCount = Math.Max(node.Tile.Foundation?.Count ?? 1, 1);
            float sizePenalty = (foundationCellCount - 1) * TileSizePenaltyPerFoundationCell;
            float repeatedWeightedUseCount = CountPreviousTileUsesInRecentAncestors(node, RepeatPenaltyAncestorWindow);
            float repeatPenalty = repeatedWeightedUseCount * RepeatPenaltyPerWeightedUse *
                (1.0f + (foundationCellCount - 1) * RepeatPenaltyPerFoundationCell);
            float turnPenalty = IsTurningTile(node.Tile) ? TurnTilePenalty : 0.0f;

            int hash = HashCode.Combine(randomSeed, node.Location.X, node.Location.Y, node.Exit.Index, node.Tile.Index);
            float jitter = ((hash & 1023) / 1023.0f - 0.5f) * 2f * NodeScoreJitterAmplitude;

            return node.FScore + sizePenalty + repeatPenalty + turnPenalty + jitter;
        }

        private static bool IsTurningTile(ConnectedTile tile)
        {
            int oppositeMask = tile.ConnectionPoints[0].ConnectionMask & tile.ConnectionPoints[1].ReversedConnectionMask;
            if (oppositeMask == 0)
                return true;

            // If the tile has connection points that are not straight in line, it is considered a turning tile
            if (!tile.ConnectionPoints[0].CoordinateOffset.IsInStraightLineWith(tile.ConnectionPoints[1].CoordinateOffset))
            {
                return true;
            }

            return false;
        }

        private static float CountPreviousTileUsesInRecentAncestors(ConnectedTileAStarNode node, int ancestorWindow)
        {
            if (node.Tile == null)
                return 0;

            float weightedUses = 0;
            int tileIndex = node.Tile.Index;
            int depth = 1;

            var current = node.Parent;
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

        private void PlaceConnectedTiles(ConnectedTileAStarNode endNode)
        {
            ConnectedTile lastPlacedTile = null;
            int lastPlacedTileIndex = -1;

            var node = endNode;
            while (node != null)
            {
                if (node.Tile != null)
                {
                    var tileSet = MutationTarget.Map.TheaterInstance.Theater.TileSets.Find(ts => ts.SetName == node.Tile.TileSetName && ts.AllowToPlace);
                    if (tileSet != null)
                    {
                        int tileIndex;

                        // To avoid visual repetition, do not place the same tile twice consecutively if it can be avoided
                        if (node.Tile.IndicesInTileSet.Count > 1 && lastPlacedTile == node.Tile)
                        {
                            tileIndex = node.Tile.IndicesInTileSet.GetRandomElementIndex(random, lastPlacedTileIndex);
                        }
                        else
                        {
                            tileIndex = node.Tile.IndicesInTileSet.GetRandomElementIndex(random, -1);
                        }

                        var tileIndexInSet = node.Tile.IndicesInTileSet[tileIndex];
                        var tileImage = MutationTarget.TheaterGraphics.GetTileGraphics(tileSet.StartTileIndex + tileIndexInSet);

                        PlaceTile(tileImage, new Point2D((int)node.Location.X, (int)node.Location.Y));

                        lastPlacedTileIndex = tileIndex;
                        lastPlacedTile = node.Tile;
                    }
                    else
                    {
                        throw new INIConfigException($"Tile Set {node.Tile.TileSetName} not found when placing cliffs!");
                    }
                }

                node = node.Parent;
            }
        }

        private void PlaceTile(TileImage tile, Point2D targetCellCoords)
        {
            if (tile == null)
                return;

            for (int i = 0; i < tile.SubTileCount; i++)
            {
                ISubTileImage image = tile.GetSubTile(i);
                if (image == null)
                    continue;

                int cx = targetCellCoords.X + i % tile.Width;
                int cy = targetCellCoords.Y + i / tile.Width;

                var mapTile = MutationTarget.Map.GetTile(cx, cy);
                if (mapTile != null)
                {
                    undoData.Add(new ConnectedTileUndoData()
                    {
                        CellCoords = new Point2D(cx, cy),
                        TileIndex = mapTile.TileIndex,
                        SubTileIndex = mapTile.SubTileIndex,
                        Level = mapTile.Level
                    });

                    mapTile.ChangeTileIndex(tile.TileID, (byte)i);
                    mapTile.Level = (byte)Math.Min(originLevel + image.TmpImage.Height, Constants.MaxMapHeightLevel);
                    RefreshCellLighting(mapTile);
                }
            }
        }

        public override void Undo()
        {
            for (int i = undoData.Count - 1; i >= 0; i--)
            {
                var data = undoData[i];
                var mapTile = MutationTarget.Map.GetTile(data.CellCoords);

                if (mapTile != null)
                {
                    mapTile.ChangeTileIndex(data.TileIndex, data.SubTileIndex);
                    mapTile.Level = data.Level;
                    RefreshCellLighting(mapTile);
                }
            }

            undoData.Clear();
            MutationTarget.InvalidateMap();
        }
    }
}
