using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations;
using TSMapEditor.Rendering;

namespace TSMapEditor.AI;

public class MapObjectInfo
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

public class MapOverlayInfo : MapObjectInfo
{
    public MapOverlayInfo(int x, int y, string iniName, int frameId) : base(RTTIType.Overlay.ToString(), x, y, iniName)
    {
        FrameID = frameId;
    }

    public int FrameID { get; }
}

public class MapTechnoInfo : MapObjectInfo
{
    public MapTechnoInfo(string rtti, int x, int y, string iniName, string owner, byte facing, int hp, string attachedTag) : base(rtti, x, y, iniName)
    {
        Owner = owner;
        Facing = facing;
        HP = hp;
        AttachedTag = attachedTag;
    }

    public int HP { get; }
    public string AttachedTag { get; }
    public string Owner { get; }
    public byte Facing { get; }
}

public class MapBuildingInfo : MapTechnoInfo
{
    public MapBuildingInfo(string rtti, int x, int y, string iniName, string owner, byte facing, int hp, string attachedTag, bool powered, bool aiRepairable)
        : base(rtti, x, y, iniName, owner, facing, hp, attachedTag)
    {
        Powered = powered;
        AIRepairable = aiRepairable;
    }

    public bool Powered { get; }
    public bool AIRepairable { get; }
}

public class MapFootInfo : MapTechnoInfo
{
    public MapFootInfo(string rtti, int x, int y, string iniName, string owner, byte facing, int hp, string attachedTag, string mission,
        bool onBridge, int veterancy, int group, bool autocreateNoRecruitable, bool autocreateYesRecruitable)
        : base(rtti, x, y, iniName, owner, facing, hp, attachedTag)
    {
        Mission = mission;
        OnBridge = onBridge;
        Veterancy = veterancy;
        Group = group;
        AutocreateNoRecruitable = autocreateNoRecruitable;
        AutocreateYesRecruitable = autocreateYesRecruitable;
    }

    public string Mission { get; }
    public bool OnBridge { get; }
    public int Veterancy { get; }
    public int Group { get; }
    public bool AutocreateNoRecruitable { get; }
    public bool AutocreateYesRecruitable { get; }
}

public class CellInfo
{
    public CellInfo(int x, int y, string tileSetName, int tileIndexInTileSet, int height, MapObjectInfo terrainObjectInfo, MapOverlayInfo overlayInfo,
        List<MapBuildingInfo> buildingInfos, List<MapFootInfo> footInfos)
    {
        X = x;
        Y = y;
        TileSetName = tileSetName;
        TileIndexInTileSet = tileIndexInTileSet;
        Height = height;
        TerrainObjectInfo = terrainObjectInfo;
        OverlayInfo = overlayInfo;
        BuildingInfos = buildingInfos;
        FootInfos = footInfos;
    }

    public int X { get; }
    public int Y { get; }
    public string TileSetName { get; }
    public int TileIndexInTileSet { get; }
    public int Height { get; }
    public MapObjectInfo TerrainObjectInfo { get; }
    public MapOverlayInfo OverlayInfo { get; }
    public List<MapBuildingInfo> BuildingInfos { get; }
    public List<MapFootInfo> FootInfos { get; }

    public static CellInfo FromMapCell(ITheater theater, MapTile mapTile)
    {
        int tileSetIndex = theater.GetTileSetId(mapTile.TileIndex);
        var tileSet = theater.Theater.TileSets[tileSetIndex];

        var terrainObjectInfo = mapTile.TerrainObject == null ? null : new MapObjectInfo(RTTIType.Terrain.ToString(), mapTile.TerrainObject.Position.X, mapTile.TerrainObject.Position.Y, mapTile.TerrainObject.TerrainType.ININame);
        var overlayInfo = mapTile.Overlay == null ? null : new MapOverlayInfo(mapTile.Overlay.Position.X, mapTile.Overlay.Position.Y, mapTile.Overlay.OverlayType.ININame, mapTile.Overlay.FrameIndex);
        var buildingInfos = mapTile.Structures.Select(s => new MapBuildingInfo(s.WhatAmI().ToString(), s.Position.X, s.Position.Y, s.ObjectType.ININame, s.Owner.ININame, s.Facing, s.HP, s.AttachedTag?.Name, s.Powered, s.AIRepairable)).ToList();
        var vehicleInfos = mapTile.Vehicles.Select(v => new MapFootInfo(v.WhatAmI().ToString(), v.Position.X, v.Position.Y, v.ObjectType.ININame, v.Owner.ININame, v.Facing, v.HP, v.AttachedTag?.Name, v.Mission, v.High, v.Veterancy, v.Group, v.AutocreateNoRecruitable, v.AutocreateYesRecruitable));
        var infantryInfos = mapTile.Infantry.Where(i => i != null).Select(i => new MapFootInfo(i.WhatAmI().ToString(), i.Position.X, i.Position.Y, i.ObjectType.ININame, i.Owner.ININame, i.Facing, i.HP, i.AttachedTag?.Name, i.Mission, i.High, i.Veterancy, i.Group, i.AutocreateNoRecruitable, i.AutocreateYesRecruitable));
        var aircraftInfos = mapTile.Aircraft.Select(a => new MapFootInfo(a.WhatAmI().ToString(), a.Position.X, a.Position.Y, a.ObjectType.ININame, a.Owner.ININame, a.Facing, a.HP, a.AttachedTag?.Name, a.Mission, a.High, a.Veterancy, a.Group, a.AutocreateNoRecruitable, a.AutocreateYesRecruitable));

        return new CellInfo(mapTile.X, mapTile.Y, tileSet.SetName, mapTile.TileIndex - tileSet.StartTileIndex, mapTile.Level, terrainObjectInfo, overlayInfo, buildingInfos, vehicleInfos.Concat(infantryInfos).Concat(aircraftInfos).ToList());
    }
}

public class MapInfo
{
    public MapInfo(string theaterName, int width, int height)
    {
        TheaterName = theaterName;
        Width = width;
        Height = height;
    }

    public string TheaterName { get; }
    public int Width { get; }
    public int Height { get; }
}

/// <summary>
/// Facade that performs operations on the map for the Model Context Protocol component.
/// </summary>
public class MapFacade
{
    public MapFacade(Map map, MutationManager mutationManager)
    {
        this.map = map;
        this.mutationManager = mutationManager;
    }

    private readonly Map map;
    private readonly MutationManager mutationManager;

    public MapInfo GetMapInfo()
    {
        return new MapInfo(map.LoadedTheaterName, map.Size.X, map.Size.Y);
    }

    public int GetMapRevision()
    {
        return mutationManager.Revision;
    }

    public List<CellInfo> InspectRegion(Rectangle rectangle)
    {
        var returnValue = new List<CellInfo>();

        for (int y = rectangle.Y; y < rectangle.Bottom; y++)
        {
            for (int x = rectangle.X; x < rectangle.Right; x++)
            {
                Point2D coords = new Point2D(x, y);
                if (!map.IsCoordWithinMap(coords))
                {
                    continue;
                }

                var mapCell = map.GetTile(coords);

                returnValue.Add(CellInfo.FromMapCell(map.TheaterInstance, mapCell));
            }
        }

        return returnValue;
    }
}
