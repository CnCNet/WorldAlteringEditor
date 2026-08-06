using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;

namespace MapEditorLibrary.Mutations.Classes.AIMutations;

/// <summary>
/// Places random entries from a terrain object collection on multiple explicit map cells.
/// </summary>
public sealed class PlaceTerrainObjectCollectionBatchMutation : Mutation
{
    public PlaceTerrainObjectCollectionBatchMutation(
        IMutationTarget mutationTarget,
        TerrainObjectCollection terrainObjectCollection,
        List<Point2D> cellCoords)
        : base(mutationTarget)
    {
        this.terrainObjectCollection = terrainObjectCollection ?? throw new ArgumentNullException(nameof(terrainObjectCollection));
        this.cellCoords = cellCoords ?? throw new ArgumentNullException(nameof(cellCoords));

        if (cellCoords.Count == 0)
            throw new ArgumentException("At least one cell coordinate must be provided.", nameof(cellCoords));
    }

    private readonly TerrainObjectCollection terrainObjectCollection;
    private readonly List<Point2D> cellCoords;
    private readonly List<Point2D> placedCellCoords = new();

    public override string GetDisplayString()
    {
        return $"Place terrain object collection '{terrainObjectCollection.Name}' on {cellCoords.Count} map cell(s)";
    }

    public override void Perform()
    {
        foreach (Point2D coords in cellCoords)
        {
            var mapTile = Map.GetTile(coords);
            if (mapTile == null)
                throw new InvalidOperationException($"Cell {coords} does not exist.");
            if (mapTile.TerrainObject != null)
                throw new InvalidOperationException($"Cell {coords} already contains a terrain object.");
        }

        placedCellCoords.Clear();

        foreach (Point2D coords in cellCoords)
        {
            var collectionEntry = terrainObjectCollection.Entries[
                MutationTarget.Randomizer.GetRandomNumber(0, terrainObjectCollection.Entries.Length - 1)];
            Map.AddTerrainObject(new TerrainObject(collectionEntry.TerrainType, coords));
            placedCellCoords.Add(coords);
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
