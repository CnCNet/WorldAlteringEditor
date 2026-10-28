using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;

namespace MapEditorLibrary.Mutations.Classes;

/// <summary>
/// A mutation that allows placing terrain object collections.
/// </summary>
public class PlaceTerrainObjectCollectionMutation : Mutation, ICheckableMutation
{
    public PlaceTerrainObjectCollectionMutation(IMutationTarget mutationTarget, TerrainObjectCollection terrainObjectCollection, Point2D cellCoords, int eventID) : base(mutationTarget)
    {
        this.terrainObjectCollection = terrainObjectCollection;
        this.cellCoords = cellCoords;
        EventID = eventID;
        brushSize = mutationTarget.BrushSize;
    }

    private readonly TerrainObjectCollection terrainObjectCollection;
    private readonly Point2D cellCoords;
    private readonly BrushSize brushSize;
    private readonly List<Point2D> placedCells = new List<Point2D>();

    public bool ShouldPerform()
    {
        return brushSize.CheckForAnyCellInBrushArea(offset =>
        {
            var tile = Map.GetTile(cellCoords + offset);
            return tile != null && tile.TerrainObject == null;
        });
    }

    public override string GetDisplayString()
    {
        return string.Format(Translate(this, "DisplayString", 
            "Place terrain object collection '{0}' at {1}"),
                terrainObjectCollection.Name, cellCoords);
    }

    public override void Perform()
    {
        placedCells.Clear();

        foreach (Point2D coords in brushSize.GetCells(cellCoords))
        {
            var tile = Map.GetTile(coords);
            if (tile == null || tile.TerrainObject != null)
                continue;

            var collectionEntry = terrainObjectCollection.Entries[MutationTarget.Randomizer.GetRandomNumber(0, terrainObjectCollection.Entries.Length - 1)];
            var terrainObject = new TerrainObject(collectionEntry.TerrainType, coords);
            Map.AddTerrainObject(terrainObject);
            placedCells.Add(coords);
        }

        MutationTarget.InvalidateMap();
    }

    public override void Undo()
    {
        foreach (Point2D coords in placedCells)
        {
            Map.RemoveTerrainObject(coords);
        }

        MutationTarget.InvalidateMap();
    }
}
