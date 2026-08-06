using System.ComponentModel;

namespace MapEditorMCP.Infos;

internal sealed class TileIndexInfo
{
    public TileIndexInfo(int absoluteTileIndex, int tileIndexInTileSet)
    {
        AbsoluteTileIndex = absoluteTileIndex;
        TileIndexInTileSet = tileIndexInTileSet;
    }

    [Description("Absolute tile index in the loaded theater.")]
    public int AbsoluteTileIndex { get; }

    [Description("Zero-based tile index relative to the start of its tile set.")]
    public int TileIndexInTileSet { get; }
}

internal sealed class SubTileInfo
{
    public SubTileInfo(int subTileIndex, int x, int y, int height)
    {
        SubTileIndex = subTileIndex;
        X = x;
        Y = y;
        Height = height;
    }

    [Description("Zero-based sub-tile slot index used by set_cells_terrain.")]
    public int SubTileIndex { get; }

    [Description("X coordinate of this sub-tile within the full tile's footprint.")]
    public int X { get; }

    [Description("Y coordinate of this sub-tile within the full tile's footprint.")]
    public int Y { get; }

    [Description("Height level added to the placement origin when this sub-tile is placed as part of the full tile.")]
    public int Height { get; }
}

internal sealed class TerrainTileInfo
{
    public TerrainTileInfo(
        int absoluteTileIndex,
        int tileSetIndex,
        string tileSetName,
        string tileSetUIName,
        int tileIndexInTileSet,
        bool hasUsableGraphics,
        int footprintWidth,
        int footprintHeight,
        int subTileSlotCount,
        List<SubTileInfo> validSubTiles)
    {
        AbsoluteTileIndex = absoluteTileIndex;
        TileSetIndex = tileSetIndex;
        TileSetName = tileSetName;
        TileSetUIName = tileSetUIName;
        TileIndexInTileSet = tileIndexInTileSet;
        HasUsableGraphics = hasUsableGraphics;
        FootprintWidth = footprintWidth;
        FootprintHeight = footprintHeight;
        SubTileSlotCount = subTileSlotCount;
        ValidSubTiles = validSubTiles;
    }

    [Description("Absolute tile index in the loaded theater.")]
    public int AbsoluteTileIndex { get; }

    [Description("Zero-based tile-set index in the loaded theater.")]
    public int TileSetIndex { get; }

    [Description("Internal name of the tile's tile set.")]
    public string TileSetName { get; }

    [Description("Localized name of the tile's tile set shown by the editor.")]
    public string TileSetUIName { get; }

    [Description("Zero-based tile index relative to the start of its tile set.")]
    public int TileIndexInTileSet { get; }

    [Description("Whether the tile has a positive footprint and at least one valid graphical subtile.")]
    public bool HasUsableGraphics { get; }

    [Description("Width of the full tile's map-cell footprint.")]
    public int FootprintWidth { get; }

    [Description("Height of the full tile's map-cell footprint.")]
    public int FootprintHeight { get; }

    [Description("Total number of subtile slots, including empty slots in irregular full tiles.")]
    public int SubTileSlotCount { get; }

    [Description("Number of subtile slots that contain usable graphics.")]
    public int ValidSubTileCount => ValidSubTiles.Count;

    [Description("Usable subtiles with the indices accepted by set_cells_terrain, their footprint coordinates, and their heights.")]
    public List<SubTileInfo> ValidSubTiles { get; }
}
