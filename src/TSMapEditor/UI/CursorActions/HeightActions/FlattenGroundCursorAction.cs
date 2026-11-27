using MapEditorLibrary.GameMath;
using MapEditorLibrary.Mutations.Classes.HeightMutations;

namespace TSMapEditor.UI.CursorActions.HeightActions;

/// <summary>
/// A cursor action that allows the user to "flatten" ground by holding
/// the left mouse button and moving it over the map.
/// 
/// The cell that the cursor first was on when the user started holding
/// the mouse button tells which height level which gets assigned to
/// all other cells that the cursor passes through.
/// </summary>
public class FlattenGroundCursorAction : CursorAction
{
    public FlattenGroundCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
    {
    }

    private int desiredHeightLevel = -1;

    public override string GetName() => Translate("Name", "Flatten Ground");

    public override void OnActionEnter()
    {
        CursorActionTarget.BrushSize = Map.EditorConfig.BrushSizes.Find(bs => bs.Width == 3 && bs.Height == 3) ?? Map.EditorConfig.BrushSizes[0];
    }

    public override bool DrawCellCursor => true;

    public override bool SeeThrough => false;

    public override void LeftDown(Point2D cellCoords)
    {
        var cell = Map.GetTile(cellCoords);
        if (cell == null)
            return;

        if (desiredHeightLevel == -1)
        {
            // This is the first cell that the user is holding the cursor on,
            // meaning it's the origin cell that determines the height level
            // that the user wants to assign to other cells
            desiredHeightLevel = cell.Level;
            return;
        }

        var mutation = FlattenGroundMutation.CreateForStroke(MutationTarget, cellCoords, CursorActionTarget.BrushSize, desiredHeightLevel,
            EventID, allowCliffInterior: !Is2DMode);

        if (mutation != null)
            PerformMutation(mutation);
    }

    public override void MouseMove(Point2D cellCoords)
    {
        if (!CursorActionTarget.WindowManager.Cursor.LeftDown)
            desiredHeightLevel = -1;
    }
}
