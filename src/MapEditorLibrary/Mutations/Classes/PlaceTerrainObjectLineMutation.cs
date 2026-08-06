using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;

namespace MapEditorLibrary.Mutations.Classes;

/// <summary>
/// A mutation that allows placing terrain objects in a line on the map.
/// </summary>
public class PlaceTerrainObjectLineMutation : Mutation
{
    public PlaceTerrainObjectLineMutation(IMutationTarget mutationTarget, TerrainType terrainType, Point2D sourceCoords, Direction direction, int length) : base(mutationTarget)
    {
        this.terrainType = terrainType;
        this.sourceCoords = sourceCoords;
        this.direction = direction;
        this.length = length;
    }

    private readonly TerrainType terrainType;
    private readonly Point2D sourceCoords;
    private readonly Direction direction;
    private readonly int length;

    private List<Point2D> placedCells;

    public override string GetDisplayString()
    {
        string directionString = Translate("Direction." + direction, Helpers.DirectionToName(direction));
        return string.Format(Translate(this, "DisplayString",
            "Place terrain object '{0}' in a line from {1} towards {2} with length {3}"),
                terrainType.GetEditorDisplayName(), sourceCoords, directionString, length);
    }

    public override void Perform()
    {
        placedCells = new List<Point2D>();
        Point2D step = Helpers.VisualDirectionToPoint(direction);

        for (int i = 0; i <= length; i++)
        {
            Point2D coords = sourceCoords + step.ScaleBy(i);
            var tile = MutationTarget.Map.GetTile(coords);
            if (tile == null)
                continue;

            if (tile.TerrainObject != null)
                continue;

            var terrainObject = new TerrainObject(terrainType, coords);
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
