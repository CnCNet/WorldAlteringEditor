using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;

namespace MapEditorLibrary.Mutations.Classes;

/// <summary>
/// A mutation that allows placing terrain objects on the map.
/// </summary>
public class PlaceTerrainObjectMutation : Mutation, ICheckableMutation
{
    public PlaceTerrainObjectMutation(IMutationTarget mutationTarget, TerrainType terrainType, Point2D cellCoords, int eventID) : base(mutationTarget)
    {
        this.terrainType = terrainType;
        this.cellCoords = cellCoords;
        EventID = eventID;
        brushSize = mutationTarget.BrushSize;
    }

    private readonly TerrainType terrainType;
    private readonly Point2D cellCoords;
    private readonly BrushSize brushSize;
    private readonly List<Point2D> placedCells = new List<Point2D>();

    public override string GetDisplayString()
    {
        return string.Format(Translate(this, "DisplayString", 
            "Place terrain object '{0}' at {1}"),
                terrainType.GetEditorDisplayName(), cellCoords);
    }

    public bool ShouldPerform()
    {
        return brushSize.CheckForAnyCellInBrushArea(offset =>
        {
            var tile = Map.GetTile(cellCoords + offset);
            return tile != null && tile.TerrainObject == null;
        });
    }

    public override void Perform()
    {
        placedCells.Clear();

        foreach (Point2D coords in brushSize.GetCells(cellCoords))
        {
            var tile = Map.GetTile(coords);
            if (tile == null || tile.TerrainObject != null)
                continue;

            var terrainObject = new TerrainObject(terrainType, coords);
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
