using System.ComponentModel;

namespace TSMapEditor.AI;

public class MapConnectedTileTypeInfo
{
    public MapConnectedTileTypeInfo(string iniName, string name, bool frontOnly, int tileCount)
        : this(iniName, name, frontOnly, tileCount, false)
    {
    }

    public MapConnectedTileTypeInfo(string iniName, string name, bool frontOnly, int tileCount, bool supportsEndPieces)
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

public class MapConnectedTilePathVertex
{
    [Description("X coordinate of the path vertex.")]
    public int X { get; set; }

    [Description("Y coordinate of the path vertex.")]
    public int Y { get; set; }
}
