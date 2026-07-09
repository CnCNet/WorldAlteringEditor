namespace TSMapEditor.CCEngine.TileData
{
    /// <summary>
    /// Interface for a single cell of a tile; sub-tile of a full TMP.
    /// </summary>
    public interface ISubTileImage
    {
        TmpImage TmpImage { get; }
    }
}
