using MapEditorLibrary.CCEngine;
using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;
using MapEditorLibrary.Models.Enums;

namespace MapEditorLibrary.Mutations.Classes.HeightMutations;

/// <summary>
/// A mutation for flattening ground. Especially useful when used with cliffs.
/// Adjusts a target cell's height to a desired level and then processes all surrounding
/// tiles for the height level to match.
/// </summary>
public class FlattenGroundMutation : AlterElevationMutationBase
{
    public FlattenGroundMutation(IMutationTarget mutationTarget, Point2D originCell, BrushSize brushSize, int desiredHeightLevel, int eventId)
        : this(mutationTarget, originCell, brushSize, desiredHeightLevel, eventId, cliffInteriorCells: null)
    { }

    private FlattenGroundMutation(IMutationTarget mutationTarget, Point2D originCell, BrushSize brushSize, int desiredHeightLevel,
        int eventId, List<Point2D> cliffInteriorCells) : base(mutationTarget, originCell, brushSize)
    {
        this.desiredHeightLevel = desiredHeightLevel;
        this.cliffInteriorCells = cliffInteriorCells;
        EventID = eventId;
    }

    private readonly int desiredHeightLevel;
    private readonly List<Point2D> cliffInteriorCells;

    public override string GetDisplayString()
    {
        return string.Format(Translate(this, "DisplayString", "Flatten ground at {0} to a level of {1} with a brush size of {2}"),
            OriginCell, desiredHeightLevel, BrushSize);
    }

    public override void Perform() => FlattenGround();

    public static FlattenGroundMutation CreateForStroke(IMutationTarget mutationTarget, Point2D originCell, BrushSize brushSize,
        int desiredHeightLevel, int eventId, bool allowCliffInterior)
    {
        var map = mutationTarget.Map;
        // Keep the activation area for regular strokes.
        var innerBrush = new BrushSize(Math.Max(1, brushSize.Width - 2), Math.Max(1, brushSize.Height - 2));

        if (GetCellsToFlatten(map, originCell, innerBrush, desiredHeightLevel).Any())
            return new FlattenGroundMutation(mutationTarget, originCell, brushSize, desiredHeightLevel, eventId);

        if (!allowCliffInterior || !IsCliffTopAtLevel(map, map.GetTile(originCell), desiredHeightLevel))
            return null;

        var interiorCells = GetCliffInteriorCells(map, originCell, brushSize, desiredHeightLevel);
        if (interiorCells.Count == 0)
            return null;

        return new FlattenGroundMutation(mutationTarget, originCell, brushSize, desiredHeightLevel, eventId, interiorCells);
    }

    private static IEnumerable<MapTile> GetCellsToFlatten(Map map, Point2D originCell, BrushSize brushSize, int desiredHeightLevel)
    {
        foreach (var cellCoords in GetBrushCells(originCell, brushSize))
        {
            var cell = map.GetTile(cellCoords);
            var tmpImage = GetEditableGroundImage(map, cell);
            if (tmpImage != null && (cell.Level != desiredHeightLevel || tmpImage.RampType != RampType.None))
                yield return cell;
        }
    }

    private static IEnumerable<Point2D> GetBrushCells(Point2D originCell, BrushSize brushSize)
        => brushSize.GetCells(originCell - new Point2D((brushSize.Width - 1) / 2, (brushSize.Height - 1) / 2));

    private static TmpImage GetEditableGroundImage(Map map, MapTile cell)
    {
        if (cell == null || !map.IsCellMorphable(cell))
            return null;

        var tmpImage = map.TheaterInstance.GetTile(cell.TileIndex).GetSubTile(cell.SubTileIndex).TmpImage;
        return (LandType)tmpImage.TerrainType is LandType.Rock or LandType.Water ? null : tmpImage;
    }

    private static List<Point2D> GetCliffInteriorCells(Map map, Point2D originCell, BrushSize brushSize, int level)
    {
        var interiorCells = new HashSet<Point2D>();

        foreach (var cellCoords in GetBrushCells(originCell, brushSize))
        {
            var cell = map.GetTile(cellCoords);
            if (cell == null || cell.Level > level || GetEditableGroundImage(map, cell) == null)
                continue;

            AddCliffInteriorSpan(map, cellCoords, new Point2D(1, 0), brushSize.Width, level, interiorCells);
            AddCliffInteriorSpan(map, cellCoords, new Point2D(0, 1), brushSize.Height, level, interiorCells);
        }

        return interiorCells.OrderBy(cell => cell.Y).ThenBy(cell => cell.X).ToList();
    }

    private static void AddCliffInteriorSpan(Map map, Point2D cellCoords, Point2D axis, int maxSpan, int level, HashSet<Point2D> interiorCells)
    {
        int startDistance = FindCliffTop(map, cellCoords, -axis, maxSpan, level);
        if (startDistance == 0)
            return;

        int endDistance = FindCliffTop(map, cellCoords, axis, maxSpan - startDistance + 1, level);
        if (endDistance == 0)
            return;

        // The brush reaches this span from the rim in 3D. Seed its full width together.
        var coords = cellCoords - axis.ScaleBy(startDistance - 1);
        for (int i = 1 - startDistance; i < endDistance; i++, coords += axis)
        {
            if (map.GetTileOrFail(coords).Level < level)
                interiorCells.Add(coords);
        }
    }

    private static int FindCliffTop(Map map, Point2D cellCoords, Point2D direction, int maxDistance, int level)
    {
        var coords = cellCoords + direction;
        for (int distance = 1; distance <= maxDistance; distance++, coords += direction)
        {
            var cell = map.GetTile(coords);
            if (IsCliffTopAtLevel(map, cell, level))
                return distance;

            if (cell == null || cell.Level > level || GetEditableGroundImage(map, cell) == null)
                break;
        }

        return 0;
    }

    private static bool IsCliffTopAtLevel(Map map, MapTile cell, int level)
    {
        if (cell == null || cell.Level != level || map.IsCellMorphable(cell))
            return false;

        return map.TheaterInstance.GetTile(cell.TileIndex).GetSubTile(cell.SubTileIndex).TmpImage.Height > 0;
    }

    private void FlattenGround()
    {
        Clear();

        var changedCells = cliffInteriorCells != null
            ? SmoothFlat(cliffInteriorCells, desiredHeightLevel, HeightFloodMode.Both, allowSteep: false)
            : FlattenBrush();

        if (changedCells != null && AutoLATEnabled)
            ApplyAutoLAT(changedCells);
    }

    private List<MapTile> FlattenBrush()
    {
        // Cells at a different level must be flattened. Ramps already at the desired level
        // (e.g. a ridge) are flattened only where possible.
        var requiredCells = new List<Point2D>();
        var optionalCells = new List<Point2D>();

        foreach (var cell in GetCellsToFlatten(Map, OriginCell, BrushSize, desiredHeightLevel))
        {
            if (cell.Level != desiredHeightLevel)
                requiredCells.Add(cell.CoordsToPoint());
            else
                optionalCells.Add(cell.CoordsToPoint());
        }

        return SmoothFlat(requiredCells, optionalCells, desiredHeightLevel, HeightFloodMode.Both, allowSteep: false);
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

        ApplyGenericAutoLAT(minX - 1, minY - 1, maxX + 1, maxY + 1, AddCellToUndoData);
    }
}
