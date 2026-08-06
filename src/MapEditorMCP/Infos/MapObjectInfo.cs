using MapEditorLibrary.Models.Enums;

namespace MapEditorMCP.Infos;

internal class MapObjectInfo
{
    public string RTTI { get; }
    public int X { get; }
    public int Y { get; }
    public string ININame { get; }

    public MapObjectInfo(string rtti, int x, int y, string iniName)
    {
        RTTI = rtti;
        X = x;
        Y = y;
        ININame = iniName;
    }
}

internal class MapOverlayInfo : MapObjectInfo
{
    public MapOverlayInfo(int x, int y, string iniName, int frameId) : base(RTTIType.Overlay.ToString(), x, y, iniName)
    {
        FrameID = frameId;
    }

    public int FrameID { get; }
}

internal class MapTechnoInfo : MapObjectInfo
{
    public MapTechnoInfo(string rtti, int objectId, int index, int x, int y, string iniName, string owner,
        byte facing, int hp, string attachedTag, string attachedTagID) : base(rtti, x, y, iniName)
    {
        ObjectId = objectId;
        Index = index;
        Owner = owner;
        Facing = facing;
        HP = hp;
        AttachedTag = attachedTag;
        AttachedTagID = attachedTagID;
    }

    public int ObjectId { get; }
    public int Index { get; }
    public int HP { get; }
    public string AttachedTag { get; }
    public string AttachedTagID { get; }
    public string Owner { get; }
    public byte Facing { get; }
}

internal class MapFootInfo : MapTechnoInfo
{
    public MapFootInfo(string rtti, int objectId, int index, int x, int y, string iniName, string owner,
        byte facing, int hp, string attachedTag, string attachedTagID, string mission,
        bool onBridge, int veterancy, int group, bool autocreateNoRecruitable, bool autocreateYesRecruitable, int? subCell)
        : base(rtti, objectId, index, x, y, iniName, owner, facing, hp, attachedTag, attachedTagID)
    {
        Mission = mission;
        OnBridge = onBridge;
        Veterancy = veterancy;
        Group = group;
        AutocreateNoRecruitable = autocreateNoRecruitable;
        AutocreateYesRecruitable = autocreateYesRecruitable;
        SubCell = subCell;
    }

    public string Mission { get; }
    public bool OnBridge { get; }
    public int Veterancy { get; }
    public int Group { get; }
    public bool AutocreateNoRecruitable { get; }
    public bool AutocreateYesRecruitable { get; }
    public int? SubCell { get; }
}

internal class MapBuildingInfo : MapTechnoInfo
{
    public MapBuildingInfo(string rtti, int objectId, int index, int x, int y, string iniName, string owner,
        byte facing, int hp, string attachedTag, string attachedTagID,
        bool aiSellable, bool aiRebuildable, bool powered, bool aiRepairable, bool nominal, int spotlight, List<string> upgrades)
        : base(rtti, objectId, index, x, y, iniName, owner, facing, hp, attachedTag, attachedTagID)
    {
        AISellable = aiSellable;
        AIRebuildable = aiRebuildable;
        Powered = powered;
        AIRepairable = aiRepairable;
        Nominal = nominal;
        Spotlight = spotlight;
        Upgrades = upgrades;
    }

    public bool AISellable { get; }
    public bool AIRebuildable { get; }
    public bool Powered { get; }
    public bool AIRepairable { get; }
    public bool Nominal { get; }
    public int Spotlight { get; }
    public List<string> Upgrades { get; }
}