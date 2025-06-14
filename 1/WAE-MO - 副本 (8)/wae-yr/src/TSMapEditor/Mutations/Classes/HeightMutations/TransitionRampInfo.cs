using System;
using System.Collections.Generic;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Models;

namespace TSMapEditor.Mutations.Classes.HeightMutations
{
    using HCT = HeightComparisonType;

    /// <summary>
    /// Defines a condition for applying a ramp on a cell based on the 
    /// height level difference between it and its neighbouring cells.
    /// </summary>
    public class TransitionRampInfo
    {
        public TransitionRampInfo(RampType rampType, List<HCT> comparisonTypesForDirections, int heightChange = 0)
        {
            RampType = rampType;
            ComparisonTypesForDirections = comparisonTypesForDirections;
            HeightChange = heightChange;

            if (comparisonTypesForDirections.Count != (int)Direction.Count)
            {
                throw new ArgumentException($"The length of {nameof(comparisonTypesForDirections)} must match " +
                    $"the number of primary directions in the game ({Direction.Count})");
            }
        }

        public readonly RampType RampType;

        public readonly List<HCT> ComparisonTypesForDirections;

        public int HeightChange { get; }

        public bool Matches(Map map, Point2D cellCoords, int cellHeight)
        {
            for (int i = 0; i < (int)Direction.Count; i++)
            {
                var offset = Helpers.VisualDirectionToPoint((Direction)i);

                var otherCellCoords = cellCoords + offset;
                var otherCell = map.GetTile(otherCellCoords);
                if (otherCell == null)
                    continue;

                HCT expected = ComparisonTypesForDirections[i];

                switch (expected)
                {
                    case HCT.Irrelevant:
                        continue;

                    case HCT.Higher:
                        if (otherCell.Level != cellHeight + 1)
                            return false;
                        break;

                    case HCT.MuchHigher:
                        if (otherCell.Level - cellHeight < 2)
                            return false;
                        break;

                    case HCT.HigherOrEqual:
                        if (otherCell.Level < cellHeight /*|| otherCell.Level - cellHeight > 1*/)
                            return false;
                        break;

                    case HCT.Lower:
                        if (otherCell.Level != cellHeight - 1)
                            return false;
                        break;

                    case HCT.MuchLower:
                        if (cellHeight - otherCell.Level < 2)
                            return false;
                        break;

                    case HCT.LowerOrEqual:
                        if (otherCell.Level > cellHeight /*|| cellHeight - otherCell.Level > 1*/)
                            return false;
                        break;

                    case HCT.Equal:
                        if (otherCell.Level != cellHeight)
                            return false;
                        break;
                }
            }

            return true;
        }
    }
}
