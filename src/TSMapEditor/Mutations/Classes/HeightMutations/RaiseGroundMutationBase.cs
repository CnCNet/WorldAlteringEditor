using System.Collections.Generic;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes.HeightMutations
{
    public abstract class RaiseGroundMutationBase : AlterElevationMutationBase
    {
        protected RaiseGroundMutationBase(IMutationTarget mutationTarget, Point2D originCell, BrushSize brushSize) : base(mutationTarget, originCell, brushSize)
        {
        }

        /// <summary>
        /// Whether this raise operation is allowed to create steep ramps
        /// (a corner spread of two levels across a single cell).
        /// </summary>
        protected abstract bool AllowSteep { get; }

        /// <summary>
        /// Entry point for raising ground.
        /// </summary>
        protected void RaiseGround()
        {
            Clear();

            var targetCell = Map.GetTile(OriginCell);

            if (targetCell == null || targetCell.Level >= Constants.MaxMapHeightLevel || !IsCellMorphable(targetCell))
                return;

            int targetCellHeight = targetCell.Level;

            // Special case for 2x2 brush.
            // Check if we can create a 2x2 "hill". If yes, then do so.
            // Otherwise, process it as 1x1.
            if (BrushSize.Width == 2 && BrushSize.Height == 2)
            {
                if (CanCreateSmallHill(targetCellHeight))
                {
                    CreateSmallHill(OriginCell);
                    return;
                }
            }

            // If the brush size is 1, only process it if the target cell is a ramp.
            // If it is not a ramp, then we'd need to raise the cell's height,
            // which would always result in it affecting more than 1 cell,
            // which wouldn't be logical with the brush size.
            if (BrushSize.Width == 1 || BrushSize.Height == 1)
            {
                if (!RampTileSet.ContainsTile(targetCell.TileIndex))
                    return;
            }

            // The brush size is the footprint of the whole feature including its ramp ring, so
            // the flat top is exactly (Width - 2) x (Height - 2): 3x3 -> 1x1, 4x4 -> 2x2, etc.
            int xSize = BrushSize.Width - 2;
            int ySize = BrushSize.Height - 2;
            if (xSize < 0) xSize = 0; // a 1-wide brush (used to raise a ramp) still targets one cell
            if (ySize < 0) ySize = 0;

            int beginY = OriginCell.Y - (ySize - 1) / 2;
            int endY = OriginCell.Y + ySize / 2;
            int beginX = OriginCell.X - (xSize - 1) / 2;
            int endX = OriginCell.X + xSize / 2;

            // Gather the cells we want to raise. We only raise ground that was on the same
            // level as our original target cell, otherwise things get illogical.
            var targetedCells = new List<Point2D>();
            for (int y = beginY; y <= endY; y++)
            {
                for (int x = beginX; x <= endX; x++)
                {
                    var cellCoords = new Point2D(x, y);
                    var cell = Map.GetTile(cellCoords);
                    if (cell == null || cell.Level >= Constants.MaxMapHeightLevel)
                        continue;

                    if (cell.Level != targetCellHeight)
                        continue;

                    if (!IsCellMorphable(cell))
                        continue;

                    targetedCells.Add(cellCoords);
                }
            }

            if (targetedCells.Count == 0)
                return;

            SmoothFlat(targetedCells, targetCellHeight + 1, HeightFloodMode.Up, AllowSteep);
        }

        private bool CanCreateSmallHill(int height)
        {
            bool canCreateSmallHill = true;

            BrushSize.DoForBrushSize(offset =>
            {
                if (!canCreateSmallHill)
                    return;

                var otherCell = Map.GetTile(OriginCell + offset);
                if (otherCell == null)
                {
                    canCreateSmallHill = false;
                    return;
                }

                var subTile = Map.TheaterInstance.GetTile(otherCell.TileIndex).GetSubTile(otherCell.SubTileIndex);
                if (!IsCellMorphable(otherCell) || otherCell.Level != height || subTile.TmpImage.RampType != RampType.None)
                    canCreateSmallHill = false;
            });

            return canCreateSmallHill;
        }

        private void CreateSmallHill(Point2D originCell)
        {
            // A 2x2 hill is created by raising the single corner shared by all four cells
            // (the bottom-right corner of the origin cell). This turns each of the four cells
            // into a one-corner ramp, exactly matching the old hard-coded corner stamp.
            var field = new CornerHeightField(Map, originCell.X, originCell.Y, originCell.X + 1, originCell.Y + 1);
            field.Build();

            var sharedCorner = new Point2D(originCell.X + 1, originCell.Y + 1);
            if (!field.TrySeedAdjustCorner(sharedCorner, 1))
                return;

            RunSmoothing(field, HeightFloodMode.Up, AllowSteep);
        }
    }
}
