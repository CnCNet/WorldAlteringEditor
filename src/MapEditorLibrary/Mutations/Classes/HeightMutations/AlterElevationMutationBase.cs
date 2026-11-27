using MapEditorLibrary.CCEngine;
using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;

namespace MapEditorLibrary.Mutations.Classes.HeightMutations;

public abstract class AlterElevationMutationBase : Mutation
{
    protected AlterElevationMutationBase(IMutationTarget mutationTarget, Point2D originCell, BrushSize brushSize) : base(mutationTarget)
    {
        OriginCell = originCell;
        BrushSize = brushSize ?? throw new ArgumentNullException(nameof(brushSize));
        RampTileSet = Map.TheaterInstance.Theater.RampTileSet;
        AutoLATEnabled = mutationTarget.AutoLATEnabled;
    }

    protected readonly Point2D OriginCell;
    protected readonly BrushSize BrushSize;
    protected readonly TileSet RampTileSet;
    protected readonly bool AutoLATEnabled;

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
    /// Sets each targeted cell to a uniform height and smooths the surrounding terrain with ramps.
    /// Returns the changed cells, or null if the edit was rejected.
    /// </summary>
    protected List<MapTile> SmoothFlat(List<Point2D> targetedCells, int newLevel, HeightFloodMode mode, bool allowSteep)
        => SmoothFlat(targetedCells, null, newLevel, mode, allowSteep);

    /// <summary>
    /// As <see cref="SmoothFlat(List{Point2D}, int, HeightFloodMode, bool)"/>, but optional cells
    /// that cannot be seeded are skipped instead of rejecting the edit.
    /// </summary>
    protected List<MapTile> SmoothFlat(List<Point2D> requiredCells, List<Point2D> optionalCells, int newLevel, HeightFloodMode mode, bool allowSteep)
    {
        var allCells = new List<Point2D>(requiredCells);
        if (optionalCells != null)
            allCells.AddRange(optionalCells);

        if (allCells.Count == 0)
            return null;

        GetCellBounds(allCells, out int minX, out int minY, out int maxX, out int maxY);

        var field = new CornerHeightField(Map, minX, minY, maxX, maxY);
        field.Build();

        foreach (var cellCoords in requiredCells)
        {
            if (!field.TrySeedFlat(cellCoords, newLevel))
                return null;
        }

        if (optionalCells != null)
        {
            foreach (var cellCoords in optionalCells)
            {
                if (field.CanSeedFlat(cellCoords, newLevel))
                    field.TrySeedFlat(cellCoords, newLevel);
            }
        }

        return RunSmoothing(field, mode, allowSteep, mode == HeightFloodMode.Both ? newLevel : null);
    }

    /// <summary>
    /// Smooths a seeded field and writes it back to the map.
    /// Returns the changed cells, or null if the edit was rejected.
    /// </summary>
    protected List<MapTile> RunSmoothing(CornerHeightField field, HeightFloodMode mode, bool allowSteep)
        => RunSmoothing(field, mode, allowSteep, null);

    private List<MapTile> RunSmoothing(CornerHeightField field, HeightFloodMode mode, bool allowSteep, int? flattenLevel)
    {
        if (!field.Flood(mode, allowSteep, flattenLevel))
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
