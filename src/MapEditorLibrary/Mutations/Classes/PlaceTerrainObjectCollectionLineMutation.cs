using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;

namespace MapEditorLibrary.Mutations.Classes;

/// <summary>
/// A mutation that allows placing terrain object collections in a line on the map.
/// </summary>
public class PlaceTerrainObjectCollectionLineMutation : Mutation
{
    public PlaceTerrainObjectCollectionLineMutation(IMutationTarget mutationTarget, TerrainObjectCollection terrainObjectCollection, Point2D sourceCoords, Direction direction, int length) : base(mutationTarget)
    {
        this.terrainObjectCollection = terrainObjectCollection;
        this.sourceCoords = sourceCoords;
        this.direction = direction;
        this.length = length;
        brushSize = mutationTarget.BrushSize;
    }

    private readonly TerrainObjectCollection terrainObjectCollection;
    private readonly Point2D sourceCoords;
    private readonly Direction direction;
    private readonly int length;
    private readonly BrushSize brushSize;

    private List<Point2D> placedCells = new List<Point2D>();

    public override string GetDisplayString()
    {
        string directionString = Translate("Direction." + direction, Helpers.DirectionToName(direction));
        return string.Format(Translate(this, "DisplayString",
            "Place terrain object collection '{0}' in a line from {1} towards {2} with length {3}"),
                terrainObjectCollection.Name, sourceCoords, directionString, length);
    }

    public override void Perform()
    {
        placedCells.Clear();

        foreach (Point2D coords in brushSize.GetCellsAlongLine(sourceCoords, direction, length))
        {
            var tile = MutationTarget.Map.GetTile(coords);
            if (tile == null)
                continue;

            if (tile.TerrainObject != null)
                continue;

            var collectionEntry = terrainObjectCollection.Entries[MutationTarget.Randomizer.GetRandomNumber(0, terrainObjectCollection.Entries.Length - 1)];
            var terrainObject = new TerrainObject(collectionEntry.TerrainType, coords);
            MutationTarget.Map.AddTerrainObject(terrainObject);
            placedCells.Add(coords);
        }

        MutationTarget.InvalidateMap();
    }

    public override void Undo()
    {
        foreach (Point2D coords in placedCells)
        {
            MutationTarget.Map.RemoveTerrainObject(coords);
        }

        MutationTarget.InvalidateMap();
    }
}
