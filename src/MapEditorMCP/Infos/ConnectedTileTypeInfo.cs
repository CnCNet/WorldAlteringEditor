using System.ComponentModel;

namespace MapEditorMCP.Infos;

internal class ConnectedTileTypeInfo
{
    public ConnectedTileTypeInfo(string iniName, string name, bool frontOnly, int tileCount)
        : this(iniName, name, frontOnly, tileCount, false)
    {
    }

    public ConnectedTileTypeInfo(string iniName, string name, bool frontOnly, int tileCount, bool supportsEndPieces)
    {
        ININame = iniName;
        Name = name;
        FrontOnly = frontOnly;
        TileCount = tileCount;
        SupportsEndPieces = supportsEndPieces;
    }

    public string ININame { get; }
    public string Name { get; }
    public bool FrontOnly { get; }
    public int TileCount { get; }
    public bool SupportsEndPieces { get; }
}

internal class ConnectedTilePathVertex
{
    [Description("X coordinate of the path vertex.")]
    public int X { get; set; }

    [Description("Y coordinate of the path vertex.")]
    public int Y { get; set; }
}
