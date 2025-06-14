using TSMapEditor.GameMath;

namespace TSMapEditor.Mutations.Classes
{
    public struct OriginalTerrainData
    {
        public OriginalTerrainData(int tileIndex, byte subTileIndex, byte level, Point2D cellCoords)
        {
            TileIndex = tileIndex;
            SubTileIndex = subTileIndex;
            Level = level;
            CellCoords = cellCoords;
        }

        public int TileIndex;
        public byte SubTileIndex;
        public byte Level;
        public Point2D CellCoords;
    }
}
