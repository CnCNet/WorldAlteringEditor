using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;
using MapEditorLibrary.Mutations;

namespace MapEditorMCP.MCPMutations;

/// <summary>
/// Directly changes the absolute tile and sub-tile index of one or more map cells.
/// </summary>
public sealed class SetCellsTerrainMutation : Mutation
{
    public SetCellsTerrainMutation(IMutationTarget mutationTarget, List<Point2D> cellCoords, int tileIndex, byte subTileIndex)
        : base(mutationTarget)
    {
        this.cellCoords = cellCoords ?? throw new ArgumentNullException(nameof(cellCoords));
        if (cellCoords.Count == 0)
            throw new ArgumentException("At least one cell coordinate must be provided.", nameof(cellCoords));

        this.tileIndex = tileIndex;
        this.subTileIndex = subTileIndex;
    }

    private readonly struct OriginalTerrain
    {
        public OriginalTerrain(Point2D cellCoords, int tileIndex, byte subTileIndex)
        {
            CellCoords = cellCoords;
            TileIndex = tileIndex;
            SubTileIndex = subTileIndex;
        }

        public Point2D CellCoords { get; }
        public int TileIndex { get; }
        public byte SubTileIndex { get; }
    }

    private readonly List<Point2D> cellCoords;
    private readonly int tileIndex;
    private readonly byte subTileIndex;
    private readonly List<OriginalTerrain> originalTerrain = new();

    public override string GetDisplayString()
    {
        return $"Set terrain on {cellCoords.Count} map cell(s) to tile {tileIndex}, sub-tile {subTileIndex}";
    }

    public override void Perform()
    {
        originalTerrain.Clear();

        foreach (Point2D coords in cellCoords)
        {
            var mapTile = Map.GetTile(coords);
            if (mapTile == null)
                throw new InvalidOperationException($"Cell {coords} does not exist.");

            originalTerrain.Add(new OriginalTerrain(coords, mapTile.TileIndex, mapTile.SubTileIndex));
            SetTerrain(mapTile, coords, tileIndex, subTileIndex);
        }
    }

    public override void Undo()
    {
        for (int i = originalTerrain.Count - 1; i >= 0; i--)
        {
            OriginalTerrain original = originalTerrain[i];
            var mapTile = Map.GetTile(original.CellCoords);
            if (mapTile == null)
                throw new InvalidOperationException($"Cell {original.CellCoords} does not exist.");

            SetTerrain(mapTile, original.CellCoords, original.TileIndex, original.SubTileIndex);
        }
    }

    private void SetTerrain(MapTile mapTile, Point2D coords, int newTileIndex, byte newSubTileIndex)
    {
        mapTile.ChangeTileIndex(newTileIndex, newSubTileIndex);
        RefreshCellLighting(mapTile);
        MutationTarget.AddRefreshPoint(coords);
    }
}
