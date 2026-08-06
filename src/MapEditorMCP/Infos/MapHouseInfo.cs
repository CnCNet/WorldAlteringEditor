namespace MapEditorMCP.Infos;

internal class MapHouseInfo
{
    public MapHouseInfo(string iniName, string houseTypeName, string color)
    {
        ININame = iniName;
        HouseTypeName = houseTypeName;
        Color = color;
    }

    public string ININame { get; }
    public string HouseTypeName { get; }
    public string Color { get; }
}
