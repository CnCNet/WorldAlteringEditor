using System;
using System.Collections.Generic;
using TSMapEditor.CCEngine.TileData;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that places a terrain tile on the map.
    /// </summary>
    public class PlaceTerrainTileMutation : Mutation, ICheckableMutation
    {
        public PlaceTerrainTileMutation(IMutationTarget mutationTarget, Point2D targetCellCoords, TileImage tile, int heightOffset) : base(mutationTarget)
        {
            TargetCellCoords = targetCellCoords;
            Tile = tile;
            HeightOffset = heightOffset;
            BrushSize = mutationTarget.BrushSize;
        }

        public Point2D TargetCellCoords { get; }
        public TileImage Tile { get; }
        public int HeightOffset { get; }
        public BrushSize BrushSize { get; }

        private List<OriginalCellTerrainData> undoData;

        public bool ShouldPerform() => true;

        public override string GetDisplayString()
        {
            var tileSet = MutationTarget.TheaterGraphics.Theater.TileSets[Tile.TileSetId];
            return string.Format(Translate(this, "DisplayString", 
                "Place terrain tile of TileSet {0} at {1} with a brush size of {2}"),
                    tileSet.TranslatedName, TargetCellCoords, BrushSize);
        }

        private void AddUndoDataForTile(Point2D brushOffset)
        {
            for (int i = 0; i < Tile.SubTileCount; i++)
            {
                ISubTileImage image = Tile.GetSubTile(i);

                if (image == null)
                    continue;

                int cx = TargetCellCoords.X + (brushOffset.X * Tile.Width) + i % Tile.Width;
                int cy = TargetCellCoords.Y + (brushOffset.Y * Tile.Height) + i / Tile.Width;

                var mapTile = MutationTarget.Map.GetTile(cx, cy);
                if (mapTile != null && (!MutationTarget.OnlyPaintOnClearGround || mapTile.IsClearGround()) &&
                    !undoData.Exists(otd => otd.CellCoords.X == cx && otd.CellCoords.Y == cy))
                {
                    undoData.Add(new OriginalCellTerrainData(mapTile.CoordsToPoint(), mapTile.TileIndex, mapTile.SubTileIndex, mapTile.Level));
                }
            }
        }

        public override void Perform()
        {
            undoData = new List<OriginalCellTerrainData>(Tile.SubTileCount * BrushSize.Width * BrushSize.Height);

            int totalWidth = Tile.Width * BrushSize.Width;
            int totalHeight = Tile.Height * BrushSize.Height;

            // Get un-do data
            DoForArea(AddUndoDataForTile, MutationTarget.AutoLATEnabled);

            MapTile originCell = MutationTarget.Map.GetTile(TargetCellCoords);
            int originLevel = -1;

            // First, look up the lowest point within the tile area for origin level
            // Only use a 1x1 brush size for this (meaning no brush at all)
            // so users can use larger brush sizes to "paint height"
            for (int i = 0; i < Tile.SubTileCount; i++)
            {
                Point2D? subTileOffset = Tile.GetSubTileCoordOffset(i);

                if (subTileOffset == null)
                    continue;

                var mapTile = MutationTarget.Map.GetTile(TargetCellCoords + subTileOffset.Value);

                if (mapTile != null)
                {
                    var existingTile = Map.TheaterInstance.GetTile(mapTile.TileIndex).GetSubTile(mapTile.SubTileIndex);

                    int cellLevel = mapTile.Level;

                    // Allow replacing back cliffs
                    if (existingTile.TmpImage.Height == Tile.GetSubTile(i).TmpImage.Height)
                        cellLevel -= existingTile.TmpImage.Height;

                    if (originLevel < 0 || cellLevel < originLevel)
                        originLevel = cellLevel;
                }
            }

            originLevel += HeightOffset;
            if (originLevel < 0)
                originLevel = 0;

            // Place the terrain
            BrushSize.DoForBrushSize(offset =>
            {
                for (int i = 0; i < Tile.SubTileCount; i++)
                {
                    Point2D? subTileOffset = Tile.GetSubTileCoordOffset(i);

                    if (subTileOffset == null)
                        continue;

                    int cx = TargetCellCoords.X + (offset.X * Tile.Width) + i % Tile.Width;
                    int cy = TargetCellCoords.Y + (offset.Y * Tile.Height) + i / Tile.Width;

                    var mapTile = MutationTarget.Map.GetTile(TargetCellCoords + new Point2D(offset.X * Tile.Width, offset.Y * Tile.Height) + subTileOffset.Value);
                    if (mapTile != null && (!MutationTarget.OnlyPaintOnClearGround || mapTile.IsClearGround()))
                    {
                        mapTile.ChangeTileIndex(Tile.TileID, (byte)i);
                        mapTile.Level = (byte)Math.Min(originLevel + Tile.GetSubTile(i).TmpImage.Height, Constants.MaxMapHeightLevel);
                        RefreshCellLighting(mapTile);
                    }
                }
            });

            // Apply autoLAT if necessary
            if (MutationTarget.AutoLATEnabled)
            {
                ApplyAutoLATForTilePlacement(Tile, BrushSize, TargetCellCoords);
            }

            MutationTarget.AddRefreshPoint(TargetCellCoords, Math.Max(Tile.Width, Tile.Height) * Math.Max(BrushSize.Width, BrushSize.Height));
        }

        private void DoForArea(Action<Point2D> action, bool doForSurroundings)
        {
            int totalWidth = Tile.Width * BrushSize.Width;
            int totalHeight = Tile.Height * BrushSize.Height;

            int initX = doForSurroundings ? -1 : 0;
            int initY = doForSurroundings ? -1 : 0;

            if (doForSurroundings)
            {
                totalWidth++;
                totalHeight++;
            }

            for (int y = initY; y <= totalHeight; y++)
            {
                for (int x = initX; x <= totalWidth; x++)
                {
                    action(new Point2D(x, y));
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

            MutationTarget.AddRefreshPoint(TargetCellCoords);
        }
    }
}
