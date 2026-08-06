namespace MapEditorMCP.Infos;

internal class MapInfo
{
    public MapInfo(string theaterName, int width, int height, bool isFlatWorld)
    {
        TheaterName = theaterName;
        Width = width;
        Height = height;
        IsFlatWorld = isFlatWorld;
    }

    public string TheaterName { get; }
    public int Width { get; }
    public int Height { get; }
    public bool IsFlatWorld { get; }
}
