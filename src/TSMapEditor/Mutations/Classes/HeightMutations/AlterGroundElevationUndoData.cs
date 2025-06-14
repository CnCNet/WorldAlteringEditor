using TSMapEditor.GameMath;

namespace TSMapEditor.Mutations.Classes.HeightMutations
{
    /// <summary>
    /// Struct for the undo data of mutations based on this class.
    /// </summary>
    public struct AlterGroundElevationUndoData
    {
        public Point2D CellCoords;
        public int TileIndex;
        public int SubTileIndex;
        public int HeightLevel;

        public AlterGroundElevationUndoData(Point2D cellCoords, int tileIndex, int subTileIndex, int heightLevel)
        {
            CellCoords = cellCoords;
            TileIndex = tileIndex;
            SubTileIndex = subTileIndex;
            HeightLevel = heightLevel;
        }
    }
}
