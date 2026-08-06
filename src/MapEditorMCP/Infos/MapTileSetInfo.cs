using System.ComponentModel;

namespace MapEditorMCP.Infos;

internal class MapTileSetInfo
{
    public MapTileSetInfo(
        int index,
        string setName,
        string uiName,
        int startTileIndex,
        int tileCount,
        bool only1x1,
        List<TileIndexInfo> tilesWithUsableGraphics,
        List<TileIndexInfo> tilesWithoutUsableGraphics)
    {
        Index = index;
        SetName = setName;
        UIName = uiName;
        StartTileIndex = startTileIndex;
        TileCount = tileCount;
        Only1x1 = only1x1;
        TilesWithUsableGraphics = tilesWithUsableGraphics;
        TilesWithoutUsableGraphics = tilesWithoutUsableGraphics;
    }

    [Description("Zero-based tile-set index in the loaded theater.")]
    public int Index { get; }

    [Description("Internal tile-set name used by place_terrain_tile.")]
    public string SetName { get; }

    [Description("Localized tile-set name shown by the editor.")]
    public string UIName { get; }

    [Description("Absolute theater index of the tile set's first tile.")]
    public int StartTileIndex { get; }

    [Description("Total number of tile entries in the set, including entries with missing or unusable graphics.")]
    public int TileCount { get; }

    [Description("Whether the editor restricts this tile set to a 1x1 placement brush.")]
    public bool Only1x1 { get; }

    [Description("Number of tile entries that have at least one valid graphical subtile.")]
    public int UsableTileCount => TilesWithUsableGraphics.Count;

    [Description("Number of tile entries whose graphics are missing or unusable.")]
    public int UnusableTileCount => TilesWithoutUsableGraphics.Count;

    [Description("Tiles that can be placed, identified by both absolute and tile-set-relative index.")]
    public List<TileIndexInfo> TilesWithUsableGraphics { get; }

    [Description("Tiles whose graphics are missing or unusable, identified by both absolute and tile-set-relative index.")]
    public List<TileIndexInfo> TilesWithoutUsableGraphics { get; }
}
