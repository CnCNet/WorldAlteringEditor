namespace MapEditorMCP.Infos;

internal class MapObjectTypeInfo
{
    public MapObjectTypeInfo(string iniName, string uiName, string editorCategory)
    {
        ININame = iniName;
        UIName = uiName;
        EditorCategory = editorCategory;
    }

    public string ININame { get; }
    public string UIName { get; }
    public string EditorCategory { get; }
}

internal class MapBuildingTypeInfo : MapObjectTypeInfo
{
    public MapBuildingTypeInfo(string iniName, string uiName, string editorCategory, int foundationWidth,
        int foundationHeight, int foundationCellCount)
        : base(iniName, uiName, editorCategory)
    {
        FoundationWidth = foundationWidth;
        FoundationHeight = foundationHeight;
        FoundationCellCount = foundationCellCount;
    }

    public int FoundationWidth { get; }
    public int FoundationHeight { get; }
    public int FoundationCellCount { get; }
}