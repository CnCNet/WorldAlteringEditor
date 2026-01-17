using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;

namespace TSMapEditor.UI.CursorActions
{
    public class PlaceSmudgeCursorAction : CursorAction
    {
        public PlaceSmudgeCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => Translate("Name", "Place Smudge");

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

        public override void PreMapDraw(Point2D cellCoords)
        {
            base.PreMapDraw(cellCoords);

            Point2D centeredBrushSizeCellCoords = CursorActionTarget.BrushSize.CenterWithinBrush(cellCoords);
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
            base.PostMapDraw(cellCoords);

            Point2D centeredBrushSizeCellCoords = CursorActionTarget.BrushSize.CenterWithinBrush(cellCoords);

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

        public override void LeftClick(Point2D cellCoords)
        {
            Point2D centeredBrushSizeCellCoords = CursorActionTarget.BrushSize.CenterWithinBrush(cellCoords);

            if (!CursorActionTarget.BrushSize.CheckForBrushSize(offset =>
            {
                var cell = CursorActionTarget.Map.GetTile(centeredBrushSizeCellCoords + offset);
                if (cell == null)
                    return false;

                if (cell.Smudge != null && cell.Smudge.SmudgeType == SmudgeType)
                {
                    // it's pointless to replace a smudge with another smudge of the same type
                    return false;
                }

                if (cell.Smudge == null && SmudgeType == null)
                    return false; // we're in deletion mode when SmudgeType == null, skip if there's nothing to delete

                return true;
            }))
            {
                return;
            }

            CursorActionTarget.MutationManager.PerformMutation(new PlaceSmudgeMutation(CursorActionTarget.MutationTarget, SmudgeType, centeredBrushSizeCellCoords, CursorActionTarget.BrushSize));
        }

        public override void LeftDown(Point2D cellCoords) => LeftClick(cellCoords);
    }
}
