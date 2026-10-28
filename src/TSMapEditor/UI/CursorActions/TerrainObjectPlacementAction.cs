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

    private PlaceTerrainObjectMutation previewMutation;

    public TerrainType TerrainType { get; set; }

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

        if (!CanDrawLinePreview())
            return;

        previewMutation = new PlaceTerrainObjectMutation(MutationTarget, TerrainType, cellCoords, Mutation.EventIdNone);
        previewMutation.Perform();
    }

    public override void PostMapDraw(Point2D cellCoords)
    {
        if (LineSourceCell.HasValue)
        {
            ClearLinePreview();
            return;
        }

        if (previewMutation != null)
        {
            previewMutation.Undo();
            previewMutation = null;
        }
    }

    protected override bool CanDrawLinePreview() => TerrainType != null;

    protected override ICheckableMutation CreateRegularPlacementMutation(Point2D cellCoords)
    {
        return new PlaceTerrainObjectMutation(CursorActionTarget.MutationTarget, TerrainType, cellCoords, EventID);
    }

    protected override Mutation CreateLinePlacementMutation(Direction direction, int length)
    {
        return new PlaceTerrainObjectLineMutation(MutationTarget, TerrainType, LineSourceCell.Value, direction, length);
    }

    protected override void ApplyLine(Point2D cellCoords)
    {
        if (TerrainType != null)
        {
            (Direction direction, int length) = GetLineInformation(cellCoords);
            var mutation = CreateLinePlacementMutation(direction, length);
            PerformMutation(mutation);
        }
    }
}
