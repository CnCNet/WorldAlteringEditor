namespace MapEditorMCP.Infos;

internal class MapWaypointInfo
{
    public MapWaypointInfo(int identifier, int x, int y, string editorColor, bool isMultiplayerStartingLocation)
    {
        Identifier = identifier;
        X = x;
        Y = y;
        EditorColor = editorColor;
        IsMultiplayerStartingLocation = isMultiplayerStartingLocation;
    }

    public int Identifier { get; }
    public int X { get; }
    public int Y { get; }
    public string EditorColor { get; }
    public bool IsMultiplayerStartingLocation { get; }
}
