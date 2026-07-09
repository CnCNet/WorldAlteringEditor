namespace TSMapEditor.CCEngine.TileData
{
    /// <summary>
    /// A TMP sub-tile that carries metadata without a MonoGame texture.
    /// </summary>
    public class UntexturedSubTileImage : ISubTileImage
    {
        public UntexturedSubTileImage(TmpImage tmpImage)
        {
            TmpImage = tmpImage;
        }

        public TmpImage TmpImage { get; }
    }
}
