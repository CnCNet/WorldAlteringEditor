namespace MapEditorMCP.Infos;

internal sealed class TerrainObjectCollectionEntryInfo
{
    public TerrainObjectCollectionEntryInfo(string iniName, string uiName)
    {
        ININame = iniName;
        UIName = uiName;
    }

    public string ININame { get; }
    public string UIName { get; }
}

internal sealed class TerrainObjectCollectionInfo
{
    public TerrainObjectCollectionInfo(string name, string uiName, List<TerrainObjectCollectionEntryInfo> entries)
    {
        Name = name;
        UIName = uiName;
        Entries = entries;
    }

    public string Name { get; }
    public string UIName { get; }
    public List<TerrainObjectCollectionEntryInfo> Entries { get; }
}

internal sealed class OverlayCollectionEntryInfo
{
    public OverlayCollectionEntryInfo(string iniName, string uiName, int frameIndex)
    {
        ININame = iniName;
        UIName = uiName;
        FrameIndex = frameIndex;
    }

    public string ININame { get; }
    public string UIName { get; }
    public int FrameIndex { get; }
}

internal sealed class OverlayCollectionInfo
{
    public OverlayCollectionInfo(string name, string uiName, List<OverlayCollectionEntryInfo> entries)
    {
        Name = name;
        UIName = uiName;
        Entries = entries;
    }

    public string Name { get; }
    public string UIName { get; }
    public List<OverlayCollectionEntryInfo> Entries { get; }
}
