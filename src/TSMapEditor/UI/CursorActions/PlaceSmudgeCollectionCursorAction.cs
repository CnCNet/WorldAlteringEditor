using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;
using MapEditorLibrary.Mutations;
using MapEditorLibrary.Mutations.Classes;
using System;
using System.Collections.Generic;

namespace TSMapEditor.UI.CursorActions;

public class PlaceSmudgeCollectionCursorAction : LineAndRegularPaintingAction
{
    public PlaceSmudgeCollectionCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
    {
    }

    public override string GetName() => Translate("Name", "Place Smudge Collection");

    protected override bool CenterCellCoordsOnBrush => true;

    protected override bool ClearPreviousCellOnMouseUp => true;

    private SmudgeCollection _smudgeCollection;
    public SmudgeCollection SmudgeCollection
    {
        get => _smudgeCollection;
        set
        {
            if (value.Entries.Length == 0)
            {
                throw new InvalidOperationException($"Smudge collection {value.Name} has no smudge entries!");
            }

            _smudgeCollection = value;
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

            // "Randomize" the smudge image, it makes it clearer that we're placing down one from a collection.
            // If we used actual RNG here we'd need to avoid doing it every frame to avoid a constantly
            // changing smudge even when the cursor is still. Using cell numbers gives the intended
            // effect without pointless flickering.
            int cellnum = cell.X + cell.Y;
            int smudgeNumber = cellnum % SmudgeCollection.Entries.Length;
            previewSmudges[i].SmudgeType = SmudgeCollection.Entries[smudgeNumber].SmudgeType;
            previewSmudges[i].Position = cell.CoordsToPoint();
            existingSmudges.Add(cell.Smudge);

            cell.Smudge = previewSmudges[i];

            i++;
        });

        CursorActionTarget.AddRefreshPoint(centeredBrushSizeCellCoords, MutationTarget.BrushSize.Max);
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

    protected override bool CanDrawLinePreview() => _smudgeCollection != null;

    protected override ICheckableMutation CreateRegularPlacementMutation(Point2D cellCoords)
    {
        return new PlaceSmudgeCollectionMutation(CursorActionTarget.MutationTarget, SmudgeCollection, cellCoords, MutationTarget.BrushSize);
    }

    protected override Mutation CreateLinePlacementMutation(Direction direction, int length)
    {
        return new PlaceSmudgeCollectionLineMutation(MutationTarget, SmudgeCollection, LineSourceCell.Value, direction, length);
    }

    protected override void ApplyLine(Point2D cellCoords)
    {
        if (_smudgeCollection != null)
        {
            (Direction direction, int length) = GetLineInformation(cellCoords);
            var mutation = CreateLinePlacementMutation(direction, length);
            PerformMutation(mutation);
        }
    }
}
