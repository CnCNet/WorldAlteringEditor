using System;
using System.Collections.Generic;
using TSMapEditor.CCEngine.TileData;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    public class PlaceTerrainLineMutation : Mutation
    {
        public PlaceTerrainLineMutation(IMutationTarget mutationTarget, MapTile source, Direction direction, int length, TileImage tile) : base(mutationTarget)
        {
            sourceTile = source;
            this.direction = direction;
            this.length = length;
            this.tile = tile;
        }

        private readonly MapTile sourceTile;
        private readonly Direction direction;
        private readonly int length;
        private readonly TileImage tile;
        private List<OriginalCellTerrainData> undoData = new List<OriginalCellTerrainData>();

        public override string GetDisplayString()
        {
            string directionString = Translate("Direction." + direction, Helpers.DirectionToName(direction));
            return string.Format(Translate(this, "DisplayString", "Place Terrain Line from {0} towards {1} with distance of {2}"), sourceTile.CoordsToPoint(), direction, length);
        }

        int GetSouthernmostSubTileOffset()
        {
            int maxY = 1;

            for (int i = 0; i < tile.SubTileCount; i++)
            {
                Point2D? subTileOffset = tile.GetSubTileCoordOffset(i);

                if (subTileOffset == null)
                    continue;

                int southOffset = (subTileOffset.Value.X + subTileOffset.Value.Y) / 2;
                if (southOffset > maxY)
                    maxY = southOffset;
            }

            return maxY;
        }

        int GetEasternmostSubTileOffset()
        {
            int maxEast = 1;

            for (int i = 0; i < tile.SubTileCount; i++)
            {
                Point2D? subTileOffset = tile.GetSubTileCoordOffset(i);

                if (subTileOffset == null)
                    continue;

                int eastOffset = subTileOffset.Value.X - subTileOffset.Value.Y;

                if (eastOffset > maxEast)
                    maxEast = eastOffset;
            }

            return maxEast;
        }

        int GetWesternmostSubTileOffset()
        {
            int maxWest = 1;

            for (int i = 0; i < tile.SubTileCount; i++)
            {
                Point2D? subTileOffset = tile.GetSubTileCoordOffset(i);

                if (subTileOffset == null)
                    continue;

                int westOffset = subTileOffset.Value.Y - subTileOffset.Value.X;

                if (westOffset > maxWest)
                    maxWest = westOffset;
            }

            return maxWest;
        }

        public override void Perform()
        {
            undoData.Clear();

            int processedLength = 0;
            while (processedLength <= length)
            {
                Point2D coords = sourceTile.CoordsToPoint() + Helpers.VisualDirectionToPoint(direction).ScaleBy(processedLength);

                PlaceTile(coords);

                switch (direction)
                {
                    case Direction.NE:
                    case Direction.SW:
                        processedLength += tile.Height;
                        break;
                    case Direction.NW:
                    case Direction.SE:
                        processedLength += tile.Width;
                        break;
                    case Direction.N:
                    case Direction.S:
                        processedLength += GetSouthernmostSubTileOffset();
                        break;
                    case Direction.E:
                        processedLength += GetEasternmostSubTileOffset();
                        break;
                    case Direction.W:
                        processedLength += GetWesternmostSubTileOffset();
                        break;
                }
            }

            MutationTarget.InvalidateMap();
        }

        private void PlaceTile(Point2D coords)
        {
            var originalData = new List<OriginalCellTerrainData>();

            // Process cells within tile foundation
            for (int i = 0; i < tile.SubTileCount; i++)
            {
                Point2D? subTileOffset = tile.GetSubTileCoordOffset(i);

                if (subTileOffset == null)
                    continue;

                Point2D cellCoords = coords + subTileOffset.Value;
                var cell = Map.GetTile(cellCoords);
                if (cell == null)
                    continue;

                if (undoData.Exists(d => d.CellCoords == cellCoords))
                    continue;

                undoData.Add(new OriginalCellTerrainData(cellCoords, cell.TileIndex, cell.SubTileIndex, cell.Level));
                cell.ChangeTileIndex(tile.TileID, (byte)i);
                cell.Level = (byte)Math.Min(cell.Level + tile.GetSubTile(i).TmpImage.Height, Constants.MaxMapHeightLevel);

                if (MutationTarget.AutoLATEnabled)
                {
                    BrushSize brush1x1 = Map.EditorConfig.BrushSizes.Find(static bs => bs.Width == 1 && bs.Height == 1);
                    if (brush1x1 == null)
                        throw new InvalidOperationException($"{nameof(PlaceTerrainLineMutation)}.{nameof(PlaceTile)}: Unable to find 1x1 brush!");

                    ApplyAutoLATForTilePlacement(tile, brush1x1, cellCoords);
                }
            }
        }

        public override void Undo()
        {
            for (int i = 0; i < undoData.Count; i++)
            {
                OriginalCellTerrainData originalTerrainData = undoData[i];

                var mapCell = MutationTarget.Map.GetTile(originalTerrainData.CellCoords);
                if (mapCell != null)
                {
                    mapCell.ChangeTileIndex(originalTerrainData.TileIndex, originalTerrainData.SubTileIndex);
                    mapCell.Level = originalTerrainData.HeightLevel;
                    RefreshCellLighting(mapCell);
                }
            }

            MutationTarget.InvalidateMap();
        }
    }
}
