using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;

namespace MapEditorLibrary.Mutations.Classes.AIMutations;

/// <summary>
/// Places multiple explicitly selected terrain objects as one mutation.
/// </summary>
public sealed class PlaceTerrainObjectBatchMutation : Mutation
{
    public PlaceTerrainObjectBatchMutation(
        IMutationTarget mutationTarget,
        List<(TerrainType TerrainType, Point2D CellCoords)> placements)
        : base(mutationTarget)
    {
        this.placements = placements ?? throw new ArgumentNullException(nameof(placements));

        if (placements.Count == 0)
            throw new ArgumentException("At least one terrain object placement must be provided.", nameof(placements));
    }

    private readonly List<(TerrainType TerrainType, Point2D CellCoords)> placements;
    private readonly List<Point2D> placedCellCoords = new();

    public override string GetDisplayString()
    {
        return $"Place {placements.Count} terrain object(s)";
    }

    public override void Perform()
    {
        foreach ((TerrainType _, Point2D cellCoords) in placements)
        {
            var mapTile = Map.GetTile(cellCoords);
            if (mapTile == null)
                throw new InvalidOperationException($"Cell {cellCoords} does not exist.");
            if (mapTile.TerrainObject != null)
                throw new InvalidOperationException($"Cell {cellCoords} already contains a terrain object.");
        }

        placedCellCoords.Clear();

        foreach ((TerrainType terrainType, Point2D cellCoords) in placements)
        {
            Map.AddTerrainObject(new TerrainObject(terrainType, cellCoords));
            placedCellCoords.Add(cellCoords);
        }

        MutationTarget.InvalidateMap();
    }

    public override void Undo()
    {
        RemovePlacedTerrainObjects();
        MutationTarget.InvalidateMap();
    }

    private void RemovePlacedTerrainObjects()
    {
        for (int i = placedCellCoords.Count - 1; i >= 0; i--)
            Map.RemoveTerrainObject(placedCellCoords[i]);

        placedCellCoords.Clear();
    }
}
