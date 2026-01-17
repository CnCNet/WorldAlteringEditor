using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;

namespace TSMapEditor.UI.CursorActions
{
    public class PlaceSmudgeCollectionCursorAction : CursorAction
    {
        public PlaceSmudgeCollectionCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => Translate("Name", "Place Smudge Collection");

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

        public override void PreMapDraw(Point2D cellCoords)
        {
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

        public override void LeftDown(Point2D cellCoords)
        {
            Point2D centeredBrushSizeCellCoords = CursorActionTarget.BrushSize.CenterWithinBrush(cellCoords);
            var mutation = new PlaceSmudgeCollectionMutation(CursorActionTarget.MutationTarget, SmudgeCollection, centeredBrushSizeCellCoords, MutationTarget.BrushSize);
            CursorActionTarget.MutationManager.PerformMutation(mutation);
        }

        public override void LeftClick(Point2D cellCoords) => LeftDown(cellCoords);
    }
}
