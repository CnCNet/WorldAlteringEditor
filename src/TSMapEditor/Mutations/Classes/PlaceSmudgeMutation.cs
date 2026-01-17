using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    public struct CachedSmudge
    {
        public Point2D CellCoords;
        public SmudgeType SmudgeType;

        public CachedSmudge(Point2D cellCoords, SmudgeType smudgeType)
        {
            CellCoords = cellCoords;
            SmudgeType = smudgeType;
        }
    }

    /// <summary>
    /// A mutation that places a smudge on the map.
    /// </summary>
    public class PlaceSmudgeMutation : Mutation
    {
        public PlaceSmudgeMutation(IMutationTarget mutationTarget, SmudgeType smudgeType, Point2D cellCoords, BrushSize brushSize) : base(mutationTarget)
        {
            this.smudgeType = smudgeType;
            this.cellCoords = cellCoords;
            this.brushSize = brushSize ?? throw new ArgumentNullException(nameof(brushSize));
        }

        private List<CachedSmudge> oldSmudges = new List<CachedSmudge>(1);
        private SmudgeType smudgeType;
        private Point2D cellCoords;
        private BrushSize brushSize;

        public override string GetDisplayString()
        {
            if (smudgeType == null)
            {
                return string.Format(Translate(this, "DisplayStringErase",
                    "Erase smudges at {0} with brush size of {1}"),
                    cellCoords, brushSize);
            }
            else
            {
                return string.Format(Translate(this, "DisplayString",
                    "Place smudge '{0}' at {1} with brush size of {2}"),
                    smudgeType.GetEditorDisplayName(), cellCoords, brushSize);
            }
        }

        public override void Perform()
        {
            oldSmudges.Clear();

            brushSize.DoForBrushSize(offset =>
            {
                var cell = MutationTarget.Map.GetTile(cellCoords + offset);

                if (cell == null)
                    return;

                if ((smudgeType == null && cell.Smudge != null) || 
                    (smudgeType != null && (cell.Smudge == null || cell.Smudge.SmudgeType != smudgeType)))
                {
                    oldSmudges.Add(new CachedSmudge(cell.CoordsToPoint(), cell.Smudge == null ? null : cell.Smudge.SmudgeType));
                }
                else
                {
                    return;
                }

                if (smudgeType != null)
                    cell.Smudge = new Smudge() { SmudgeType = smudgeType, Position = cell.CoordsToPoint() };
                else
                    cell.Smudge = null; // delete an existing smudge if smudge type is null
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
