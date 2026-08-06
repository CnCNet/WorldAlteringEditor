using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;
using MapEditorLibrary.Mutations;
using MapEditorLibrary.Mutations.Classes;
using System.Collections.Generic;

namespace TSMapEditor.UI.CursorActions;

public class PlaceSmudgeCursorAction : LineAndRegularPaintingAction
{
    public PlaceSmudgeCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
    {
    }

    public override string GetName() => Translate("Name", "Place Smudge");

    protected override bool PreventInputEventsOnPreviousCell => false; // Not needed, PlaceSmudgeMutation.ShouldPerform does the job already

    protected override bool CenterCellCoordsOnBrush => true;

    private SmudgeType _smudgeType;
    public SmudgeType SmudgeType 
    {
        get => _smudgeType;
        set
        {
            if (value != _smudgeType)
            {
                _smudgeType = value;
            }
        }
    }

    private List<Smudge> previewSmudges = new List<Smudge>();
    private List<Smudge> existingSmudges = new List<Smudge>();

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

        Point2D centeredBrushSizeCellCoords = GetCenteredBrushSizeCellCoords(cellCoords);
        existingSmudges.Clear();

        int i = 0;
        CursorActionTarget.BrushSize.DoForBrushSize(offset =>
        {
            var cell = CursorActionTarget.Map.GetTile(centeredBrushSizeCellCoords + offset);
            if (cell == null)
                return;

            if (previewSmudges.Count <= i)
            {
                previewSmudges.Add(new Smudge());
            }

            previewSmudges[i].Position = centeredBrushSizeCellCoords + offset;
            previewSmudges[i].SmudgeType = SmudgeType;
            existingSmudges.Add(cell.Smudge);

            if (SmudgeType != null)
                cell.Smudge = previewSmudges[i];
            else
                cell.Smudge = null;

            i++;
        });

        CursorActionTarget.AddRefreshPoint(centeredBrushSizeCellCoords, CursorActionTarget.BrushSize.Max);
    }
    
    public override void PostMapDraw(Point2D cellCoords)
    {
        if (LineSourceCell.HasValue)
        {
            ClearLinePreview();
            return;
        }

        Point2D centeredBrushSizeCellCoords = GetCenteredBrushSizeCellCoords(cellCoords);

        int i = 0;
        CursorActionTarget.BrushSize.DoForBrushSize(offset =>
        {
            var cell = CursorActionTarget.Map.GetTile(centeredBrushSizeCellCoords + offset);
            if (cell == null)
                return;

            cell.Smudge = existingSmudges[i];
            i++;
        });

        CursorActionTarget.AddRefreshPoint(centeredBrushSizeCellCoords, CursorActionTarget.BrushSize.Max);
    }

    protected override bool CanDrawLinePreview() => true;

    protected override ICheckableMutation CreateRegularPlacementMutation(Point2D cellCoords)
    {
        return new PlaceSmudgeMutation(CursorActionTarget.MutationTarget, SmudgeType, cellCoords, CursorActionTarget.BrushSize);
    }

    protected override Mutation CreateLinePlacementMutation(Direction direction, int length)
    {
        return new PlaceSmudgeLineMutation(MutationTarget, SmudgeType, LineSourceCell.Value, direction, length);
    }

    protected override void ApplyLine(Point2D cellCoords)
    {
        (Direction direction, int length) = GetLineInformation(cellCoords);
        var mutation = CreateLinePlacementMutation(direction, length);
        PerformMutation(mutation);
    }
}
