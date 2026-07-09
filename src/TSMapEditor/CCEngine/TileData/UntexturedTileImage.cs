using System;

namespace TSMapEditor.CCEngine.TileData
{
    /// <summary>
    /// Contains metadata for a single full TMP (all sub-tiles / all cells) without texture data.
    /// </summary>
    public class UntexturedTileImage : TileImage
    {
        public UntexturedTileImage(int width, int height, int tileSetId, int tileIndex, int tileId, TmpImage[] tmpImages)
            : base(width, height, tileSetId, tileIndex, tileId)
        {
            tmpImages ??= Array.Empty<TmpImage>();
            TMPImages = Array.ConvertAll(tmpImages, tmpImage => tmpImage == null ? null : new UntexturedSubTileImage(tmpImage));
        }

        public override ISubTileImage GetSubTile(int index) => TMPImages[index];

        public override int SubTileCount => TMPImages.Length;

        public UntexturedSubTileImage[] TMPImages { get; set; }
    }
}
