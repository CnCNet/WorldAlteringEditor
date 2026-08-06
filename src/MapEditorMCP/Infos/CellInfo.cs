using MapEditorLibrary;
using MapEditorLibrary.Models;
using MapEditorLibrary.Models.Enums;

namespace MapEditorMCP.Infos;

internal class CellInfo
{
    public CellInfo(int x, int y, string tileSetName, int tileIndex, int tileIndexInTileSet, int subTileIndex, string landType,
        bool passableForLandUnits, bool passableForNavalUnits, int height,
        MapObjectInfo terrainObjectInfo, MapOverlayInfo overlayInfo, List<MapBuildingInfo> buildingInfos, List<MapFootInfo> footInfos,
        List<MapWaypointInfo> waypointInfos)
    {
        X = x;
        Y = y;
        TileSetName = tileSetName;
        TileIndex = tileIndex;
        TileIndexInTileSet = tileIndexInTileSet;
        SubTileIndex = subTileIndex;
        LandType = landType;
        PassableForLandUnits = passableForLandUnits;
        PassableForNavalUnits = passableForNavalUnits;
        Height = height;
        TerrainObjectInfo = terrainObjectInfo;
        OverlayInfo = overlayInfo;
        BuildingInfos = buildingInfos;
        FootInfos = footInfos;
        WaypointInfos = waypointInfos;
    }

    public int X { get; }
    public int Y { get; }
    public string TileSetName { get; }
    public int TileIndex { get; }
    public int TileIndexInTileSet { get; }
    public int SubTileIndex { get; }
    public string LandType { get; }
    public bool PassableForLandUnits { get; }
    public bool PassableForNavalUnits { get; }
    public int Height { get; }
    public MapObjectInfo TerrainObjectInfo { get; }
    public MapOverlayInfo OverlayInfo { get; }
    public List<MapBuildingInfo> BuildingInfos { get; }
    public List<MapFootInfo> FootInfos { get; }
    public List<MapWaypointInfo> WaypointInfos { get; }

    public static CellInfo FromMapCell(Map map, MapTile mapTile)
    {
        ITheater theater = map.TheaterInstance;
        int tileSetIndex = theater.GetTileSetId(mapTile.TileIndex);
        var tileSet = theater.Theater.TileSets[tileSetIndex];

        var terrainObjectInfo = mapTile.TerrainObject == null
            ? null
            : new MapObjectInfo(RTTIType.Terrain.ToString(), mapTile.TerrainObject.Position.X, mapTile.TerrainObject.Position.Y,
                mapTile.TerrainObject.TerrainType.ININame);
        var overlayInfo = mapTile.Overlay == null
            ? null
            : new MapOverlayInfo(mapTile.Overlay.Position.X, mapTile.Overlay.Position.Y, mapTile.Overlay.OverlayType.ININame,
                mapTile.Overlay.FrameIndex);
        var buildingInfos = mapTile.Structures.Select(s => (MapBuildingInfo)FromTechno(map, s)).ToList();
        var vehicleInfos = mapTile.Vehicles.Select(v => (MapFootInfo)FromTechno(map, v));
        var infantryInfos = mapTile.Infantry.Where(i => i != null).Select(i => (MapFootInfo)FromTechno(map, i));
        var aircraftInfos = mapTile.Aircraft.Select(a => (MapFootInfo)FromTechno(map, a));
        var waypointInfos = mapTile.Waypoints.OrderBy(waypoint => waypoint.Identifier).Select(FromWaypoint).ToList();

        var tileInfo = theater.GetTile(mapTile.TileIndex);
        var subTileInfo = tileInfo.GetSubTile(mapTile.SubTileIndex);
        LandType landType = (LandType)subTileInfo.TmpImage.TerrainType;

        return new CellInfo(mapTile.X, mapTile.Y, tileSet.SetName, mapTile.TileIndex, mapTile.TileIndex - tileSet.StartTileIndex,
            mapTile.SubTileIndex, landType.ToString(), !Helpers.IsLandTypeImpassable(landType, true), !Helpers.IsLandTypeImpassableForNavalUnits(landType),
            mapTile.Level, terrainObjectInfo, overlayInfo, buildingInfos,
            vehicleInfos.Concat(infantryInfos).Concat(aircraftInfos).ToList(), waypointInfos);
    }

    public static MapWaypointInfo FromWaypoint(Waypoint waypoint)
    {
        return new MapWaypointInfo(
            waypoint.Identifier,
            waypoint.Position.X,
            waypoint.Position.Y,
            waypoint.EditorColor,
            waypoint.Identifier >= 0 && waypoint.Identifier < Constants.MultiplayerMaxPlayers);
    }

    public static MapTechnoInfo FromTechno(Map map, TechnoBase techno)
    {
        if (techno is Structure structure)
        {
            return new MapBuildingInfo(structure.WhatAmI().ToString(), structure.ObjectId, map.Structures.IndexOf(structure),
                structure.Position.X, structure.Position.Y,
                structure.ObjectType.ININame, structure.Owner.ININame, structure.Facing, structure.HP, structure.AttachedTag?.Name, structure.AttachedTag?.ID,
                structure.AISellable, structure.AIRebuildable, structure.Powered, structure.AIRepairable, structure.Nominal, (int)structure.Spotlight,
                structure.Upgrades.Select(upgrade => upgrade?.ININame).ToList());
        }

        if (techno is Unit unit)
        {
            return new MapFootInfo(unit.WhatAmI().ToString(), unit.ObjectId, map.Units.IndexOf(unit), unit.Position.X,
                unit.Position.Y, unit.ObjectType.ININame, unit.Owner.ININame,
                unit.Facing, unit.HP, unit.AttachedTag?.Name, unit.AttachedTag?.ID, unit.Mission, unit.High, unit.Veterancy, unit.Group,
                unit.AutocreateNoRecruitable, unit.AutocreateYesRecruitable, null);
        }

        if (techno is Infantry infantry)
        {
            return new MapFootInfo(infantry.WhatAmI().ToString(), infantry.ObjectId, map.Infantry.IndexOf(infantry), infantry.Position.X, infantry.Position.Y,
                infantry.ObjectType.ININame, infantry.Owner.ININame, infantry.Facing, infantry.HP, infantry.AttachedTag?.Name, infantry.AttachedTag?.ID,
                infantry.Mission, infantry.High, infantry.Veterancy, infantry.Group, infantry.AutocreateNoRecruitable, infantry.AutocreateYesRecruitable,
                (int)infantry.SubCell);
        }

        if (techno is Aircraft aircraft)
        {
            return new MapFootInfo(aircraft.WhatAmI().ToString(), aircraft.ObjectId, map.Aircraft.IndexOf(aircraft), aircraft.Position.X, aircraft.Position.Y,
                aircraft.ObjectType.ININame, aircraft.Owner.ININame, aircraft.Facing, aircraft.HP, aircraft.AttachedTag?.Name, aircraft.AttachedTag?.ID,
                aircraft.Mission, aircraft.High, aircraft.Veterancy, aircraft.Group, aircraft.AutocreateNoRecruitable, aircraft.AutocreateYesRecruitable, null);
        }

        throw new ArgumentException($"Unsupported techno type {techno.WhatAmI()}.", nameof(techno));
    }
}
