using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that allows placing smudge collections.
    /// </summary>
    class PlaceSmudgeCollectionMutation : Mutation
    {
        public PlaceSmudgeCollectionMutation(IMutationTarget mutationTarget, SmudgeCollection smudgeCollection, Point2D cellCoords, BrushSize brushSize) : base(mutationTarget)
        {
            this.smudgeCollection = smudgeCollection;
            this.cellCoords = cellCoords;
            this.brushSize = brushSize ?? throw new ArgumentNullException(nameof(brushSize));
        }

        private List<CachedSmudge> oldSmudges = new List<CachedSmudge>(1);
        private List<CachedSmudge> placedSmudges = new List<CachedSmudge>();
        private SmudgeCollection smudgeCollection;
        private Point2D cellCoords;
        private BrushSize brushSize;

        public override string GetDisplayString()
        {
            return string.Format(Translate(this, "DisplayString", "Place smudge collection {0} at {1} with brush size of {2}"),
                smudgeCollection.Name, cellCoords, brushSize);
        }

        public override void Perform()
        {
            oldSmudges.Clear();

            bool placeNew = placedSmudges.Count == 0;
            int i = 0;

            brushSize.DoForBrushSize(offset =>
            {
                var cell = MutationTarget.Map.GetTile(cellCoords + offset);

                if (cell == null)
                    return;

                oldSmudges.Add(new CachedSmudge(cell.CoordsToPoint(), cell.Smudge == null ? null : cell.Smudge.SmudgeType));

                if (placeNew)
                {
                    var collectionEntry = smudgeCollection.Entries[MutationTarget.Randomizer.GetRandomNumber(0, smudgeCollection.Entries.Length - 1)];
                    cell.Smudge = new Smudge() { SmudgeType = collectionEntry.SmudgeType, Position = cell.CoordsToPoint() };
                    placedSmudges.Add(new CachedSmudge(cell.CoordsToPoint(), cell.Smudge.SmudgeType));
                }
                else
                {
                    cell.Smudge = new Smudge() { SmudgeType = placedSmudges[i].SmudgeType, Position = placedSmudges[i].CellCoords };
                    i++;
                }
            });

            MutationTarget.AddRefreshPoint(cellCoords, brushSize.Max);
        }

        public override void Undo()
        {
            foreach (var oldSmudge in oldSmudges)
            {
                var cell = Map.GetTile(oldSmudge.CellCoords);

                if (oldSmudge.SmudgeType == null)
                    cell.Smudge = null;
                else
                    cell.Smudge = new Smudge() { SmudgeType = oldSmudge.SmudgeType, Position = oldSmudge.CellCoords };
            }

            MutationTarget.AddRefreshPoint(cellCoords, brushSize.Max);
        }
    }
}
