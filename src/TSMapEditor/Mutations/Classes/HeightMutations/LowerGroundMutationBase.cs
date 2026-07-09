using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes.HeightMutations
{
    public abstract class LowerGroundMutationBase : AlterElevationMutationBase
    {
        public LowerGroundMutationBase(IMutationTarget mutationTarget, Point2D originCell, BrushSize brushSize) : base(mutationTarget, originCell, brushSize)
        {
        }

        /// <summary>
        /// Whether this lower operation is allowed to create steep ramps
        /// (a corner spread of two levels across a single cell).
        /// </summary>
        protected abstract bool AllowSteep { get; }

        protected void LowerGround()
        {
            Clear();

            var targetCell = Map.GetTile(OriginCell);

            if (targetCell == null || targetCell.Level < 1 || !IsCellMorphable(targetCell))
                return;

            int targetCellHeight = targetCell.Level;

            // If the brush size is 1, only process it if the target cell is a ramp.
            // If it is not a ramp, then we'd need to lower the cell's height,
            // which would always result in it affecting more than 1 cell,
            // which wouldn't be logical with the brush size.
            if (BrushSize.Width == 1 || BrushSize.Height == 1)
            {
                if (!RampTileSet.ContainsTile(targetCell.TileIndex))
                    return;
            }

            int xSize = BrushSize.Width;
            int ySize = BrushSize.Height;

            int beginY = OriginCell.Y - (ySize - 1) / 2;
            int endY = OriginCell.Y + ySize / 2;
            int beginX = OriginCell.X - (xSize - 1) / 2;
            int endX = OriginCell.X + xSize / 2;

            // Gather the cells we want to lower. We only lower ground that was on the same
            // level as our original target cell, otherwise things get illogical.
            var targetedCells = new List<Point2D>();
            for (int y = beginY; y <= endY; y++)
            {
                for (int x = beginX; x <= endX; x++)
                {
                    var cellCoords = new Point2D(x, y);
                    var cell = Map.GetTile(cellCoords);
                    if (cell == null || cell.Level < 1)
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

            var changedCells = SmoothFlat(targetedCells, targetCellHeight - 1, HeightFloodMode.Down, AllowSteep);

            if (changedCells != null && MutationTarget.AutoLATEnabled)
                ApplyAutoLAT(changedCells);
        }

        private void ApplyAutoLAT(List<MapTile> changedCells)
        {
            if (changedCells.Count == 0)
                return;

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            foreach (var cell in changedCells)
            {
                if (cell.X < minX) minX = cell.X;
                if (cell.Y < minY) minY = cell.Y;
                if (cell.X > maxX) maxX = cell.X;
                if (cell.Y > maxY) maxY = cell.Y;
            }

            ApplyGenericAutoLAT(minX - 1, minY - 1, maxX + 1, maxY + 1);
        }
    }
}
