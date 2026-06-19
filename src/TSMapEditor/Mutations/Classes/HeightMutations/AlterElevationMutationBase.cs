using System;
using System.Collections.Generic;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes.HeightMutations
{
    public abstract class AlterElevationMutationBase : Mutation
    {
        protected AlterElevationMutationBase(IMutationTarget mutationTarget, Point2D originCell, BrushSize brushSize) : base(mutationTarget)
        {
            OriginCell = originCell;
            BrushSize = brushSize ?? throw new ArgumentNullException(nameof(brushSize));
            RampTileSet = Map.TheaterInstance.Theater.RampTileSet;
        }

        protected readonly Point2D OriginCell;
        protected readonly BrushSize BrushSize;
        protected readonly TileSet RampTileSet;

        protected List<AlterGroundElevationUndoData> undoData = new List<AlterGroundElevationUndoData>();

        protected static readonly Point2D[] SurroundingTiles = new Point2D[] { new Point2D(-1, 0), new Point2D(1, 0), new Point2D(0, -1), new Point2D(0, 1),
                                                                             new Point2D(-1, -1), new Point2D(-1, 1), new Point2D(1, -1), new Point2D(1, 1) };

        protected bool IsCellMorphable(MapTile cell) => Map.IsCellMorphable(cell);

        protected void Clear()
        {
            undoData.Clear();
        }

        /// <summary>
        /// Adds a cell's data to the undo data structure.
        /// Does nothing if the cell has already been added to the undo data structure.
        /// </summary>
        protected void AddCellToUndoData(Point2D cellCoords)
        {
            var cell = Map.GetTile(cellCoords);
            if (cell == null)
                return;

            if (undoData.Exists(u => u.CellCoords == cellCoords))
                return;

            undoData.Add(new AlterGroundElevationUndoData(cellCoords, cell.TileIndex, cell.SubTileIndex, cell.Level));
        }

        /// <summary>
        /// Runs a corner-field smoothing pass that sets each targeted cell to a uniform
        /// height and then smooths the surrounding terrain, applying ramps where needed.
        /// Returns the list of cells that were changed, or null if the edit was rejected
        /// because it would have had to alter terrain anchored by an immutable cell, in
        /// which case nothing on the map was changed.
        /// </summary>
        protected List<MapTile> SmoothFlat(List<Point2D> targetedCells, int newLevel, HeightFloodMode mode, bool allowSteep)
        {
            GetCellBounds(targetedCells, out int minX, out int minY, out int maxX, out int maxY);

            var field = new CornerHeightField(Map, minX, minY, maxX, maxY);
            field.Build();

            foreach (var cellCoords in targetedCells)
            {
                if (!field.TrySeedFlat(cellCoords, newLevel))
                    return null;
            }

            return RunSmoothing(field, mode, allowSteep);
        }

        /// <summary>
        /// Floods, writes back and refreshes lighting for an already-built and already-seeded
        /// field. Returns the changed cells, or null if the edit was rejected.
        /// </summary>
        protected List<MapTile> RunSmoothing(CornerHeightField field, HeightFloodMode mode, bool allowSteep)
        {
            if (!field.Flood(mode, allowSteep))
                return null;

            var changedCells = field.WriteBack(allowSteep, AddCellToUndoData);

            foreach (var cell in changedCells)
                RefreshCellLighting(cell);

            MutationTarget.InvalidateMap();
            return changedCells;
        }

        private static void GetCellBounds(List<Point2D> cells, out int minX, out int minY, out int maxX, out int maxY)
        {
            minX = int.MaxValue;
            minY = int.MaxValue;
            maxX = int.MinValue;
            maxY = int.MinValue;

            foreach (var cell in cells)
            {
                if (cell.X < minX) minX = cell.X;
                if (cell.Y < minY) minY = cell.Y;
                if (cell.X > maxX) maxX = cell.X;
                if (cell.Y > maxY) maxY = cell.Y;
            }
        }

        public override void Undo()
        {
            foreach (var entry in undoData)
            {
                var cell = Map.GetTile(entry.CellCoords);
                cell.ChangeTileIndex(entry.TileIndex, (byte)entry.SubTileIndex);
                cell.Level = (byte)entry.HeightLevel;
                RefreshCellLighting(cell);
            }

            MutationTarget.InvalidateMap();
        }
    }
}
