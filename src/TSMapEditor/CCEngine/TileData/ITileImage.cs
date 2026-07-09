using TSMapEditor.GameMath;

namespace TSMapEditor.CCEngine.TileData
{
    /// <summary>
    /// Interface for a full tile image (containing all sub-tiles).
    /// </summary>
    public interface ITileImage
    {
        /// <summary>
        /// Width of the tile in cells.
        /// </summary>
        int Width { get; }

        /// <summary>
        /// Height of the tile in cells.
        /// </summary>
        int Height { get; }

        /// <summary>
        /// The index of the tile's tileset.
        /// </summary>
        int TileSetId { get; }

        /// <summary>
        /// The index of the tile within its tileset.
        /// </summary>
        int TileIndexInTileSet { get; }

        /// <summary>
        /// The unique ID of this tile within all tiles in the game.
        /// </summary>
        int TileID { get; }

        int SubTileCount { get; }

        ISubTileImage GetSubTile(int index);

        Point2D? GetSubTileCoordOffset(int index);
    }
}
