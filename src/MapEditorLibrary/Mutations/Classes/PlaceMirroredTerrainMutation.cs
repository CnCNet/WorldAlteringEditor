using MapEditorLibrary.CCEngine.TileData;
using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;

namespace MapEditorLibrary.Mutations.Classes;

/// <summary>
/// Applies a set of one-cell tiles before recalculating terrain transitions.
/// </summary>
public class PlaceMirroredTerrainMutation : Mutation
{
    public PlaceMirroredTerrainMutation(IMutationTarget mutationTarget, IReadOnlyList<Point2D> cells, TileImage tile,
        int heightOffset, bool line) : base(mutationTarget)
    {
        this.cells = cells;
        this.tile = tile;
        this.heightOffset = heightOffset;
        this.line = line;
        autoLAT = mutationTarget.AutoLATEnabled;
    }

    private readonly IReadOnlyList<Point2D> cells;
    private readonly TileImage tile;
    private readonly int heightOffset;
    private readonly bool line;
    private readonly bool autoLAT;
    private readonly Dictionary<Point2D, OriginalCellTerrainData> original = new Dictionary<Point2D, OriginalCellTerrainData>();

    public override string GetDisplayString() => Translate("MirroredPlacementMutation.DisplayString", "Mirrored placement");

    public override void Perform()
    {
        original.Clear();
        var subTile = tile.GetSubTile(0);

        foreach (Point2D coords in cells)
        {
            var cell = Map.GetTileOrFail(coords);
            RecordOriginal(coords);

            int baseLevel = line ? cell.Level : Math.Max(0, PlaceTerrainTileMutation.GetBaseLevel(Map, cell, subTile) + heightOffset);
            cell.Level = (byte)Math.Min(baseLevel + subTile.TmpImage.Height, Constants.MaxMapHeightLevel);

            cell.ChangeTileIndex(tile.TileID, 0);
            RefreshCellLighting(cell);
        }

        if (autoLAT)
            UpdateTransitions();

        MutationTarget.InvalidateMap();
    }

    private void UpdateTransitions()
    {
        (var baseTileSet, var altBaseTileSet) = GetBaseTileSetsForTileSet(Map.TheaterInstance, tile.TileSetId);
        var affected = new HashSet<Point2D>();
        var brush = new BrushSize(1, 1);

        foreach (Point2D coords in cells)
        {
            foreach (Point2D neighbor in brush.GetCells(coords, 1))
                affected.Add(neighbor);
        }

        // Resolve transitions from the complete stroke before replacing any of its cells.
        var transitions = new List<(Point2D Cell, int TileIndex)>();
        foreach (Point2D coords in affected)
        {
            int index = GetAutoLATTileIndexForCell(Map, coords, baseTileSet, altBaseTileSet, false);
            if (index >= 0)
                transitions.Add((coords, index));
        }

        foreach (var transition in transitions)
        {
            RecordOriginal(transition.Cell);
            Map.GetTileOrFail(transition.Cell).ChangeTileIndex(transition.TileIndex, 0);
        }
    }

    private void RecordOriginal(Point2D coords)
    {
        var cell = Map.GetTileOrFail(coords);
        original.TryAdd(coords, new OriginalCellTerrainData(coords, cell.TileIndex, cell.SubTileIndex, cell.Level));
    }

    public override void Undo()
    {
        foreach (var entry in original.Values)
        {
            var cell = Map.GetTileOrFail(entry.CellCoords);
            cell.ChangeTileIndex(entry.TileIndex, entry.SubTileIndex);
            cell.Level = entry.HeightLevel;
            RefreshCellLighting(cell);
        }

        MutationTarget.InvalidateMap();
    }
}
