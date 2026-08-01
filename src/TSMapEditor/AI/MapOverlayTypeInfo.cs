using System.Collections.Generic;

namespace TSMapEditor.AI;

public class MapOverlayTypeInfo
{
    public MapOverlayTypeInfo(string iniName, string uiName, string editorCategory, int frameCount, bool tiberium, bool wall, bool waterBound,
        bool isVeins, bool isVeinholeMonster, List<string> connectedOverlayNames)
    {
        ININame = iniName;
        UIName = uiName;
        EditorCategory = editorCategory;
        FrameCount = frameCount;
        Tiberium = tiberium;
        Wall = wall;
        WaterBound = waterBound;
        IsVeins = isVeins;
        IsVeinholeMonster = isVeinholeMonster;
        ConnectedOverlayNames = connectedOverlayNames;
    }

    public string ININame { get; }
    public string UIName { get; }
    public string EditorCategory { get; }
    public int FrameCount { get; }
    public bool Tiberium { get; }
    public bool Wall { get; }
    public bool WaterBound { get; }
    public bool IsVeins { get; }
    public bool IsVeinholeMonster { get; }
    public List<string> ConnectedOverlayNames { get; }
}

public class MapConnectedOverlayFrameInfo
{
    public MapConnectedOverlayFrameInfo(string overlayININame, int frameIndex, int connectsTo)
    {
        OverlayININame = overlayININame;
        FrameIndex = frameIndex;
        ConnectsTo = connectsTo;
    }

    public string OverlayININame { get; }
    public int FrameIndex { get; }
    public int ConnectsTo { get; }
}

public class MapConnectedOverlayTypeInfo
{
    public MapConnectedOverlayTypeInfo(string name, string uiName, int connectionMask, List<string> relatedOverlayNames,
        List<MapConnectedOverlayFrameInfo> frames)
    {
        Name = name;
        UIName = uiName;
        ConnectionMask = connectionMask;
        RelatedOverlayNames = relatedOverlayNames;
        Frames = frames;
    }

    public string Name { get; }
    public string UIName { get; }
    public int ConnectionMask { get; }
    public List<string> RelatedOverlayNames { get; }
    public List<MapConnectedOverlayFrameInfo> Frames { get; }
}
