using System.Collections.Generic;

namespace TSMapEditor.AI;

public sealed class MapTerrainObjectCollectionEntryInfo
{
    public MapTerrainObjectCollectionEntryInfo(string iniName, string uiName)
    {
        ININame = iniName;
        UIName = uiName;
    }

    public string ININame { get; }
    public string UIName { get; }
}

public sealed class MapTerrainObjectCollectionInfo
{
    public MapTerrainObjectCollectionInfo(string name, string uiName, List<MapTerrainObjectCollectionEntryInfo> entries)
    {
        Name = name;
        UIName = uiName;
        Entries = entries;
    }

    public string Name { get; }
    public string UIName { get; }
    public List<MapTerrainObjectCollectionEntryInfo> Entries { get; }
}

public sealed class MapOverlayCollectionEntryInfo
{
    public MapOverlayCollectionEntryInfo(string iniName, string uiName, int frameIndex)
    {
        ININame = iniName;
        UIName = uiName;
        FrameIndex = frameIndex;
    }

    public string ININame { get; }
    public string UIName { get; }
    public int FrameIndex { get; }
}

public sealed class MapOverlayCollectionInfo
{
    public MapOverlayCollectionInfo(string name, string uiName, List<MapOverlayCollectionEntryInfo> entries)
    {
        Name = name;
        UIName = uiName;
        Entries = entries;
    }

    public string Name { get; }
    public string UIName { get; }
    public List<MapOverlayCollectionEntryInfo> Entries { get; }
}
