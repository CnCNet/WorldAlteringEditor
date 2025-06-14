using TSMapEditor.GameMath;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// Struct for un-do data of mutations that change smudges of cells.
    /// </summary>
    struct OriginalSmudgeInfo
    {
        public int SmudgeTypeIndex;
        public Point2D CellCoords;

        public OriginalSmudgeInfo(int smudgeTypeIndex, Point2D cellCoords)
        {
            SmudgeTypeIndex = smudgeTypeIndex;
            CellCoords = cellCoords;
        }
    }
}
