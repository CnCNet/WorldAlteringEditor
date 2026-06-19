using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes.HeightMutations
{
    /// <summary>
    /// A mutation for flattening ground. Especially useful when used with cliffs.
    /// Adjusts a target cell's height to a desired level and then processes all surrounding
    /// tiles for the height level to match.
    /// </summary>
    public class FlattenGroundMutation : AlterElevationMutationBase
    {
        public FlattenGroundMutation(IMutationTarget mutationTarget, Point2D originCell, BrushSize brushSize,
            int desiredHeightLevel, int eventId) : base(mutationTarget, originCell, brushSize)
        {
            this.desiredHeightLevel = desiredHeightLevel;
            EventID = eventId;
        }

        private readonly int desiredHeightLevel;

        public override string GetDisplayString()
        {
            return string.Format(Translate(this, "DisplayString", "Flatten ground at {0} to a level of {1} with a brush size of {2}"),
                OriginCell, desiredHeightLevel, BrushSize);
        }

        public override void Perform() => FlattenGround();

        private void FlattenGround()
        {
            Clear();

            int xSize = BrushSize.Width;
            int ySize = BrushSize.Height;

            int beginY = OriginCell.Y - (ySize - 1) / 2;
            int endY = OriginCell.Y + ySize / 2;
            int beginX = OriginCell.X - (xSize - 1) / 2;
            int endX = OriginCell.X + xSize / 2;

            var targetedCells = new List<Point2D>();
            for (int y = beginY; y <= endY; y++)
            {
                for (int x = beginX; x <= endX; x++)
                {
                    var cellCoords = new Point2D(x, y);
                    var cell = Map.GetTile(cellCoords);
                    if (cell == null || cell.Level == desiredHeightLevel)
                        continue;

                    if (!IsCellMorphable(cell))
                        continue;

                    targetedCells.Add(cellCoords);
                }
            }

            if (targetedCells.Count == 0)
                return;

            // Flattening produces only non-steep ramps, like the game.
            var changedCells = SmoothFlat(targetedCells, desiredHeightLevel, HeightFloodMode.Both, allowSteep: false);

            if (changedCells != null && MutationTarget.AutoLATEnabled)
                ApplyAutoLAT(changedCells);
        }

        private void ApplyAutoLAT(List<MapTile> changedCells)
        {
            if (changedCells.Count == 0)
                return;

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            foreach (var cell in changedCells)
            {
                if (cell.X < minX) minX = cell.X;
                if (cell.Y < minY) minY = cell.Y;
                if (cell.X > maxX) maxX = cell.X;
                if (cell.Y > maxY) maxY = cell.Y;
            }

            ApplyGenericAutoLAT(minX - 1, minY - 1, maxX + 1, maxY + 1);
        }
    }
}
