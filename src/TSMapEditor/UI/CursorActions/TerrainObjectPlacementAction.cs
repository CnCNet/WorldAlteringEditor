using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;
using MapEditorLibrary.Mutations;
using MapEditorLibrary.Mutations.Classes;

namespace TSMapEditor.UI.CursorActions;

/// <summary>
/// A cursor action that allows placing down terrain objects.
/// </summary>
public class TerrainObjectPlacementAction : LineAndRegularPaintingAction
{
    public TerrainObjectPlacementAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
    {
    }

    public override string GetName() => Translate("Name", "Place Terrain Object");

    protected override bool PreventInputEventsOnPreviousCell => false; // Not needed, PlaceTerrainObjectMutation.ShouldPerform does the job already

    private TerrainObject terrainObject;

    private TerrainType _terrainType;

    public TerrainType TerrainType
    {
        get => _terrainType;
        set
        {
            if (_terrainType != value)
            {
                _terrainType = value;

                if (_terrainType == null)
                {
                    terrainObject = null;
                }
                else
                {
                    terrainObject = new TerrainObject(_terrainType);
                }
            }
        }
    }

    public override void OnActionExit()
    {
        ClearLinePreview();
        base.OnActionExit();
    }

    public override void PreMapDraw(Point2D cellCoords)
    {
        if (LineSourceCell.HasValue)
        {
            ApplyLinePreview(cellCoords);
            return;
        }

        var cell = CursorActionTarget.Map.GetTile(cellCoords);
        if (cell.TerrainObject == null)
        {
            terrainObject.Position = cell.CoordsToPoint();
            cell.TerrainObject = terrainObject;
        }

        CursorActionTarget.AddRefreshPoint(cellCoords);
    }

    public override void PostMapDraw(Point2D cellCoords)
    {
        if (LineSourceCell.HasValue)
        {
            ClearLinePreview();
            return;
        }

        var cell = CursorActionTarget.Map.GetTile(cellCoords);
        if (cell.TerrainObject == terrainObject)
        {
            cell.TerrainObject = null;
        }

        CursorActionTarget.AddRefreshPoint(cellCoords);
    }

    protected override bool CanDrawLinePreview() => _terrainType != null;

    protected override ICheckableMutation CreateRegularPlacementMutation(Point2D cellCoords)
    {
        return new PlaceTerrainObjectMutation(CursorActionTarget.MutationTarget, TerrainType, cellCoords);
    }

    protected override Mutation CreateLinePlacementMutation(Direction direction, int length)
    {
        return new PlaceTerrainObjectLineMutation(MutationTarget, TerrainType, LineSourceCell.Value, direction, length);
    }

    protected override void ApplyLine(Point2D cellCoords)
    {
        if (_terrainType != null)
        {
            (Direction direction, int length) = GetLineInformation(cellCoords);
            var mutation = CreateLinePlacementMutation(direction, length);
            PerformMutation(mutation);
        }
    }
}
