using TSMapEditor.CCEngine.TileData;

namespace TSMapEditor.Rendering
{
    /// <summary>
    /// Contains graphics for a single full TMP (all sub-tiles / all cells).
    /// </summary>
    public class MGTileImage : TileImage
    {
        public MGTileImage(int width, int height, int tileSetId, int tileIndex, int tileId, MGSubTileImage[] tmpImages)
            : base(width, height, tileSetId, tileIndex, tileId)
        {
            TMPImages = tmpImages;
        }

        public override ISubTileImage GetSubTile(int index) => TMPImages[index];

        public override int SubTileCount => TMPImages.Length;

        /// <summary>
        /// Array of graphical sub-tiles that make up this tile.
        /// Avoid accessing directly unless you truly need the graphical texture data:
        /// prefer using <see cref="SubTileCount"/> and <see cref="GetSubTile(int)"/> instead.
        /// </summary>
        public MGSubTileImage[] TMPImages { get; set; }
    }
}
