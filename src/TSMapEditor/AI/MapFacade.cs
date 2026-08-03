using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.CCEngine;
using TSMapEditor.CCEngine.TileData;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.Mutations;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.Mutations.Classes.AIMutations;
using TSMapEditor.Rendering;
using TSMapEditor.UI;

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
    public MapTechnoInfo(string rtti, int objectId, int index, int x, int y, string iniName, string owner, byte facing, int hp, string attachedTag, string attachedTagID) : base(rtti, x, y, iniName)
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

public class MapBuildingInfo : MapTechnoInfo
{
    public MapBuildingInfo(string rtti, int objectId, int index, int x, int y, string iniName, string owner, byte facing, int hp, string attachedTag, string attachedTagID,
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

public class MapFootInfo : MapTechnoInfo
{
    public MapFootInfo(string rtti, int objectId, int index, int x, int y, string iniName, string owner, byte facing, int hp, string attachedTag, string attachedTagID, string mission,
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

public class CellInfo
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

        var terrainObjectInfo = mapTile.TerrainObject == null ? null : new MapObjectInfo(RTTIType.Terrain.ToString(), mapTile.TerrainObject.Position.X, mapTile.TerrainObject.Position.Y, mapTile.TerrainObject.TerrainType.ININame);
        var overlayInfo = mapTile.Overlay == null ? null : new MapOverlayInfo(mapTile.Overlay.Position.X, mapTile.Overlay.Position.Y, mapTile.Overlay.OverlayType.ININame, mapTile.Overlay.FrameIndex);
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
            return new MapBuildingInfo(structure.WhatAmI().ToString(), structure.ObjectId, map.Structures.IndexOf(structure), structure.Position.X, structure.Position.Y,
                structure.ObjectType.ININame, structure.Owner.ININame, structure.Facing, structure.HP, structure.AttachedTag?.Name, structure.AttachedTag?.ID,
                structure.AISellable, structure.AIRebuildable, structure.Powered, structure.AIRepairable, structure.Nominal, (int)structure.Spotlight,
                structure.Upgrades.Select(upgrade => upgrade?.ININame).ToList());
        }

        if (techno is Unit unit)
        {
            return new MapFootInfo(unit.WhatAmI().ToString(), unit.ObjectId, map.Units.IndexOf(unit), unit.Position.X, unit.Position.Y, unit.ObjectType.ININame, unit.Owner.ININame,
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

public class MapInfo
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

public class MapObjectTypeInfo
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

public class MapBuildingTypeInfo : MapObjectTypeInfo
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

public class MapHouseInfo
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

public class MapTileSetInfo
{
    public MapTileSetInfo(int index, string setName, string uiName, int startTileIndex, int tileCount, bool only1x1)
    {
        Index = index;
        SetName = setName;
        UIName = uiName;
        StartTileIndex = startTileIndex;
        TileCount = tileCount;
        Only1x1 = only1x1;
    }

    public int Index { get; }
    public string SetName { get; }
    public string UIName { get; }
    public int StartTileIndex { get; }
    public int TileCount { get; }
    public bool Only1x1 { get; }
}

public class MapEditResult
{
    public MapEditResult(int revision, List<CellInfo> affectedCells)
    {
        Revision = revision;
        AffectedCells = affectedCells;
    }

    public int Revision { get; }
    public List<CellInfo> AffectedCells { get; }
}

public sealed class MapFacadeValidationException : Exception
{
    public MapFacadeValidationException(string message) : base(message)
    {
    }
}

/// <summary>
/// Facade that performs operations on the map for the Model Context Protocol component.
/// </summary>
public class MapFacade
{
    private const int MaxTechnoQueryResults = 1_000;
    private const int MaxObjectDeletionCount = 1_000;
    private const int MaxConnectedTilePathVertexCount = 256;
    private const int MaxMapOperationDimension = 256;
    private const int MaxMapOperationCellCount = 10_000;

    private static readonly string[] ValidMissions = new[]
    {
        "Ambush", "Area Guard", "Attack", "Capture", "Construction", "Enter", "Guard", "Harmless", "Harvest", "Hunt", "Missile", "Move", "Open",
        "Patrol", "QMove", "Repair", "Rescue", "Retreat", "Return", "Sabotage", "Selling", "Sleep", "Sticky", "Stop", "Unload"
    };

    private static readonly int[] ValidVeterancyLevels = new[] { 0, 50, 100, 150, 200 };

    public MapFacade(Map map, MutationManager mutationManager, IMutationTarget mutationTarget)
    {
        this.map = map;
        this.mutationManager = mutationManager;
        this.mutationTarget = mutationTarget;
    }

    private readonly Map map;
    private readonly MutationManager mutationManager;
    private readonly IMutationTarget mutationTarget;

    public MapInfo GetMapInfo()
    {
        return new MapInfo(map.LoadedTheaterName, map.Size.X, map.Size.Y, Constants.IsFlatWorld);
    }

    public int GetMapRevision()
    {
        return mutationManager.Revision;
    }

    public List<MapObjectTypeInfo> GetTerrainTypes(string nameFilter = null)
    {
        string normalizedFilter = nameFilter?.Trim();

        return map.Rules.TerrainTypes
            .Where(terrainType => terrainType.EditorVisible && terrainType.IsValidForTheater(map.LoadedTheaterName))
            .Select(terrainType => new MapObjectTypeInfo(
                terrainType.ININame,
                terrainType.GetEditorDisplayName(),
                terrainType.EditorCategory))
            .Where(typeInfo => string.IsNullOrWhiteSpace(normalizedFilter) ||
                ContainsIgnoringCase(typeInfo.ININame, normalizedFilter) ||
                ContainsIgnoringCase(typeInfo.UIName, normalizedFilter) ||
                ContainsIgnoringCase(typeInfo.EditorCategory, normalizedFilter))
            .OrderBy(typeInfo => typeInfo.EditorCategory)
            .ThenBy(typeInfo => typeInfo.UIName)
            .ThenBy(typeInfo => typeInfo.ININame)
            .ToList();
    }

    public List<MapOverlayTypeInfo> GetOverlayTypes(string nameFilter = null)
    {
        string normalizedFilter = nameFilter?.Trim();

        return map.Rules.OverlayTypes
            .Where(overlayType => overlayType.EditorVisible && overlayType.IsValidForTheater(map.LoadedTheaterName))
            .Select(overlayType => new MapOverlayTypeInfo(
                overlayType.ININame,
                overlayType.GetEditorDisplayName(),
                GetEffectiveEditorCategory(overlayType),
                GetPlaceableOverlayFrameCount(overlayType),
                overlayType.Tiberium,
                overlayType.Wall,
                overlayType.WaterBound,
                overlayType.IsVeins,
                overlayType.IsVeinholeMonster,
                map.EditorConfig.ConnectedOverlays
                    .Where(connectedOverlay => connectedOverlay.Frames.TrueForAll(frame => frame.OverlayType.IsValidForTheater(map.LoadedTheaterName)))
                    .Where(connectedOverlay => connectedOverlay.Frames.Exists(frame => frame.OverlayType == overlayType))
                    .Select(connectedOverlay => connectedOverlay.Name)
                    .ToList()))
            .Where(typeInfo => string.IsNullOrWhiteSpace(normalizedFilter) ||
                ContainsIgnoringCase(typeInfo.ININame, normalizedFilter) ||
                ContainsIgnoringCase(typeInfo.UIName, normalizedFilter) ||
                ContainsIgnoringCase(typeInfo.EditorCategory, normalizedFilter) ||
                typeInfo.ConnectedOverlayNames.Exists(name => ContainsIgnoringCase(name, normalizedFilter)))
            .OrderBy(typeInfo => typeInfo.EditorCategory)
            .ThenBy(typeInfo => typeInfo.UIName)
            .ThenBy(typeInfo => typeInfo.ININame)
            .ToList();
    }

    public List<MapConnectedOverlayTypeInfo> GetConnectedOverlayTypes(string nameFilter = null)
    {
        string normalizedFilter = nameFilter?.Trim();

        return map.EditorConfig.ConnectedOverlays
            .Where(connectedOverlay => connectedOverlay.Frames.TrueForAll(frame => frame.OverlayType.IsValidForTheater(map.LoadedTheaterName)))
            .Select(connectedOverlay => new MapConnectedOverlayTypeInfo(
                connectedOverlay.Name,
                connectedOverlay.UIName,
                connectedOverlay.ConnectionMask,
                connectedOverlay.RelatedOverlays.Select(relatedOverlay => relatedOverlay.Name).ToList(),
                connectedOverlay.Frames
                    .Select(frame => new MapConnectedOverlayFrameInfo(frame.OverlayType.ININame, frame.FrameIndex, frame.ConnectsTo))
                    .ToList()))
            .Where(typeInfo => string.IsNullOrWhiteSpace(normalizedFilter) ||
                ContainsIgnoringCase(typeInfo.Name, normalizedFilter) ||
                ContainsIgnoringCase(typeInfo.UIName, normalizedFilter) ||
                typeInfo.RelatedOverlayNames.Exists(name => ContainsIgnoringCase(name, normalizedFilter)) ||
                typeInfo.Frames.Exists(frame => ContainsIgnoringCase(frame.OverlayININame, normalizedFilter)))
            .OrderBy(typeInfo => typeInfo.UIName)
            .ThenBy(typeInfo => typeInfo.Name)
            .ToList();
    }

    public List<MapConnectedTileTypeInfo> GetConnectedTileTypes(string nameFilter = null)
    {
        string normalizedFilter = nameFilter?.Trim();

        return map.EditorConfig.ConnectedTileTypes
            .Where(IsConnectedTileTypeAvailable)
            .Select(connectedTileType => new MapConnectedTileTypeInfo(
                connectedTileType.IniName,
                connectedTileType.Name,
                connectedTileType.FrontOnly,
                connectedTileType.Tiles.Count,
                connectedTileType.SupportsEndPieces))
            .Where(typeInfo => string.IsNullOrWhiteSpace(normalizedFilter) ||
                ContainsIgnoringCase(typeInfo.ININame, normalizedFilter) ||
                ContainsIgnoringCase(typeInfo.Name, normalizedFilter))
            .OrderBy(typeInfo => typeInfo.Name)
            .ThenBy(typeInfo => typeInfo.ININame)
            .ToList();
    }

    public List<MapBuildingTypeInfo> GetBuildingTypes(string nameFilter = null)
    {
        string normalizedFilter = nameFilter?.Trim();

        return map.Rules.BuildingTypes
            .Where(buildingType => buildingType.EditorVisible && buildingType.IsValidForTheater(map.LoadedTheaterName))
            .Select(buildingType =>
            {
                var foundation = buildingType.ArtConfig.Foundation;
                bool usesOriginOnly = foundation.Width == 0 || foundation.Height == 0;

                return new MapBuildingTypeInfo(
                    buildingType.ININame,
                    buildingType.GetEditorDisplayName(),
                    GetEffectiveEditorCategory(buildingType),
                    usesOriginOnly ? 1 : foundation.Width,
                    usesOriginOnly ? 1 : foundation.Height,
                    usesOriginOnly ? 1 : (foundation.FoundationCells?.Length ?? 0));
            })
            .Where(typeInfo => string.IsNullOrWhiteSpace(normalizedFilter) ||
                ContainsIgnoringCase(typeInfo.ININame, normalizedFilter) ||
                ContainsIgnoringCase(typeInfo.UIName, normalizedFilter) ||
                ContainsIgnoringCase(typeInfo.EditorCategory, normalizedFilter))
            .OrderBy(typeInfo => typeInfo.EditorCategory)
            .ThenBy(typeInfo => typeInfo.UIName)
            .ThenBy(typeInfo => typeInfo.ININame)
            .ToList();
    }

    private List<MapObjectTypeInfo> GetTechnoTypes(IEnumerable<TechnoType> technoTypes, string nameFilter)
    {
        string normalizedFilter = nameFilter?.Trim();

        return technoTypes
            .Where(technoType => technoType.EditorVisible && technoType.IsValidForTheater(map.LoadedTheaterName))
            .Select(technoType => new MapObjectTypeInfo(
                technoType.ININame,
                technoType.GetEditorDisplayName(),
                GetEffectiveEditorCategory(technoType)))
            .Where(typeInfo => string.IsNullOrWhiteSpace(normalizedFilter) ||
                ContainsIgnoringCase(typeInfo.ININame, normalizedFilter) ||
                ContainsIgnoringCase(typeInfo.UIName, normalizedFilter) ||
                ContainsIgnoringCase(typeInfo.EditorCategory, normalizedFilter))
            .OrderBy(typeInfo => typeInfo.EditorCategory)
            .ThenBy(typeInfo => typeInfo.UIName)
            .ThenBy(typeInfo => typeInfo.ININame)
            .ToList();
    }

    public List<MapObjectTypeInfo> GetAircraftTypes(string nameFilter = null)
    {
        return GetTechnoTypes(map.Rules.AircraftTypes, nameFilter);
    }

    public List<MapObjectTypeInfo> GetInfantryTypes(string nameFilter = null)
    {
        return GetTechnoTypes(map.Rules.InfantryTypes, nameFilter);
    }

    public List<MapObjectTypeInfo> GetVehicleTypes(string nameFilter = null)
    {
        return GetTechnoTypes(map.Rules.UnitTypes, nameFilter);
    }

    public MapTechnoQueryResult GetTechnos(string rttiFilter, string typeNameFilter, string ownerNameFilter, Rectangle? area)
    {
        string normalizedRTTI = NormalizeTechnoRTTI(rttiFilter);
        IEnumerable<TechnoBase> technos = map.Structures.Cast<TechnoBase>()
            .Concat(map.Units)
            .Concat(map.Infantry)
            .Concat(map.Aircraft);

        if (normalizedRTTI != null)
            technos = technos.Where(techno => techno.WhatAmI().ToString() == normalizedRTTI);
        if (!string.IsNullOrWhiteSpace(typeNameFilter))
            technos = technos.Where(techno => ContainsIgnoringCase(techno.GetObjectType().ININame, typeNameFilter.Trim()));
        if (!string.IsNullOrWhiteSpace(ownerNameFilter))
            technos = technos.Where(techno => ContainsIgnoringCase(techno.Owner?.ININame, ownerNameFilter.Trim()));
        if (area.HasValue)
            technos = technos.Where(techno => area.Value.Contains(techno.Position.X, techno.Position.Y));

        var result = technos.Select(techno => CellInfo.FromTechno(map, techno)).ToList();
        if (result.Count > MaxTechnoQueryResults)
        {
            throw new MapFacadeValidationException(
                $"The query matched {result.Count} technos, exceeding the limit of {MaxTechnoQueryResults}. Add type, owner, kind, or area filters.");
        }

        return new MapTechnoQueryResult(mutationManager.Revision, result);
    }

    public List<MapHouseInfo> GetHouses(string nameFilter = null)
    {
        string normalizedFilter = nameFilter?.Trim();

        return map.GetHouses()
            .Select(house => new MapHouseInfo(house.ININame, house.HouseType?.ININame, house.Color))
            .Where(houseInfo => string.IsNullOrWhiteSpace(normalizedFilter) ||
                ContainsIgnoringCase(houseInfo.ININame, normalizedFilter) ||
                ContainsIgnoringCase(houseInfo.HouseTypeName, normalizedFilter) ||
                ContainsIgnoringCase(houseInfo.Color, normalizedFilter))
            .OrderBy(houseInfo => houseInfo.ININame)
            .ToList();
    }

    public List<MapWaypointInfo> GetWaypoints(int? identifier = null)
    {
        if (identifier.HasValue && (identifier.Value < 0 || identifier.Value >= Constants.MaxWaypoint))
        {
            throw new MapFacadeValidationException(
                $"Waypoint identifier must be from 0 through {Constants.MaxWaypoint - 1}.");
        }

        return map.Waypoints
            .Where(waypoint => !identifier.HasValue || waypoint.Identifier == identifier.Value)
            .OrderBy(waypoint => waypoint.Identifier)
            .Select(CellInfo.FromWaypoint)
            .ToList();
    }

    public List<MapTileSetInfo> GetTileSets(string nameFilter = null)
    {
        string normalizedFilter = nameFilter?.Trim();

        return map.TheaterInstance.Theater.TileSets
            .Where(IsTileSetPlaceable)
            .Select(tileSet => new MapTileSetInfo(
                tileSet.Index,
                tileSet.SetName,
                tileSet.TranslatedName,
                tileSet.StartTileIndex,
                tileSet.LoadedTileCount,
                tileSet.Only1x1))
            .Where(tileSetInfo => string.IsNullOrWhiteSpace(normalizedFilter) ||
                ContainsIgnoringCase(tileSetInfo.SetName, normalizedFilter) ||
                ContainsIgnoringCase(tileSetInfo.UIName, normalizedFilter))
            .OrderBy(tileSetInfo => tileSetInfo.UIName)
            .ThenBy(tileSetInfo => tileSetInfo.SetName)
            .ToList();
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
                    continue;

                var mapCell = map.GetTile(coords);
                if (mapCell == null)
                    continue;

                returnValue.Add(CellInfo.FromMapCell(map, mapCell));
            }
        }

        return returnValue;
    }

    public MapEditResult ModifyTechnos(List<MapTechnoReference> technoReferences, MapTechnoModificationProperties properties, int? expectedRevision)
    {
        if (expectedRevision.HasValue && expectedRevision.Value != mutationManager.Revision)
        {
            throw new MapFacadeValidationException(
                $"The map revision changed from {expectedRevision.Value} to {mutationManager.Revision}. Query the technos again before modifying them, or omit expectedRevision to allow concurrent edits.");
        }

        if (technoReferences == null || technoReferences.Count == 0)
            throw new MapFacadeValidationException("At least one techno reference must be provided.");
        if (technoReferences.Count > MaxTechnoQueryResults)
            throw new MapFacadeValidationException($"At most {MaxTechnoQueryResults} technos can be modified in one call.");
        if (properties == null || !HasAnyModification(properties))
            throw new MapFacadeValidationException("At least one property modification must be provided.");

        ValidateCommonModificationProperties(properties);

        var technos = technoReferences.Select(ResolveTechnoReference).ToList();
        if (technos.Distinct().Count() != technos.Count)
            throw new MapFacadeValidationException("The techno reference list contains duplicates.");

        var changes = new List<TechnoPropertyChange>();
        foreach (var techno in technos)
        {
            var oldProperties = TechnoPropertiesSnapshot.Capture(techno);
            var newProperties = TechnoPropertiesSnapshot.Capture(techno);
            ApplyModificationProperties(techno, newProperties, properties);

            if (!oldProperties.HasSameValuesAs(newProperties))
                changes.Add(new TechnoPropertyChange(techno, oldProperties, newProperties));
        }

        if (changes.Count == 0)
            throw new MapFacadeValidationException("All selected technos already have the requested property values.");

        mutationManager.PerformMutation(new ModifyTechnosMutation(mutationTarget, changes));

        var affectedCells = technos
            .Select(techno => map.GetTile(techno.Position))
            .Where(mapTile => mapTile != null)
            .Distinct()
            .Select(mapTile => CellInfo.FromMapCell(map, mapTile))
            .ToList();

        return new MapEditResult(mutationManager.Revision, affectedCells);
    }

    public MapEditResult DeleteObjects(List<MapTechnoReference> technoReferences, List<MapTerrainObjectReference> terrainObjectReferences)
    {
        int technoReferenceCount = technoReferences?.Count ?? 0;
        int terrainObjectReferenceCount = terrainObjectReferences?.Count ?? 0;
        int totalReferenceCount = technoReferenceCount + terrainObjectReferenceCount;

        if (totalReferenceCount == 0)
            throw new MapFacadeValidationException("At least one techno or terrain object reference must be provided.");
        if (totalReferenceCount > MaxObjectDeletionCount)
            throw new MapFacadeValidationException($"At most {MaxObjectDeletionCount} objects can be deleted in one call.");

        var objects = new List<GameObject>(totalReferenceCount);
        if (technoReferences != null)
        {
            foreach (var technoReference in technoReferences)
            {
                var techno = ResolveTechnoReference(technoReference);
                if (map.GetTile(techno.Position) == null)
                    throw new MapFacadeValidationException($"Techno objectId {techno.ObjectId} is outside the valid map area and cannot be deleted through the MCP server.");

                objects.Add(techno);
            }
        }

        if (terrainObjectReferences != null)
        {
            foreach (var terrainObjectReference in terrainObjectReferences)
                objects.Add(ResolveTerrainObjectReference(terrainObjectReference));
        }

        if (objects.Distinct().Count() != objects.Count)
            throw new MapFacadeValidationException("The object reference lists contain duplicates.");

        var affectedMapTiles = GetAffectedMapTiles(objects);
        mutationManager.PerformMutation(new DeleteMapObjectsMutation(mutationTarget, objects));

        return new MapEditResult(
            mutationManager.Revision,
            affectedMapTiles.Select(mapTile => CellInfo.FromMapCell(map, mapTile)).ToList());
    }

    public MapEditResult EraseOverlay(int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new MapFacadeValidationException("The overlay erasure width and height must both be greater than zero.");

        if (width > MaxMapOperationDimension || height > MaxMapOperationDimension || (long)width * height > MaxMapOperationCellCount)
        {
            throw new MapFacadeValidationException(
                $"The overlay erasure area is too large. Each dimension may be at most {MaxMapOperationDimension} cells and the total area may be at most {MaxMapOperationCellCount} cells.");
        }

        var overlayTiles = new List<MapTile>();
        for (int yOffset = 0; yOffset < height; yOffset++)
        {
            for (int xOffset = 0; xOffset < width; xOffset++)
            {
                var mapTile = map.GetTile(x + xOffset, y + yOffset);
                if (mapTile?.Overlay != null)
                    overlayTiles.Add(mapTile);
            }
        }

        if (overlayTiles.Count == 0)
            throw new MapFacadeValidationException($"The requested {width}x{height} area at ({x}, {y}) contains no overlay.");

        var affectedMapTiles = overlayTiles
            .SelectMany(mapTile => GetMapTileAndSurroundings(mapTile.CoordsToPoint()))
            .Distinct()
            .ToList();

        var mutation = new PlaceOverlayMutation(mutationTarget, null, null, new Point2D(x, y), new BrushSize(width, height));
        mutationManager.PerformMutation(mutation);

        return new MapEditResult(
            mutationManager.Revision,
            affectedMapTiles.Select(mapTile => CellInfo.FromMapCell(map, mapTile)).ToList());
    }

    public MapEditResult PlaceOverlay(string overlayTypeName, int x, int y, int width, int height, int? frameIndex)
    {
        if (string.IsNullOrWhiteSpace(overlayTypeName))
            throw new MapFacadeValidationException("An overlay type INI name must be provided.");

        var overlayType = map.Rules.OverlayTypes.Find(candidate => string.Equals(candidate.ININame, overlayTypeName, StringComparison.OrdinalIgnoreCase));

        if (overlayType == null)
            throw new MapFacadeValidationException($"Overlay type '{overlayTypeName}' does not exist in the loaded rules.");
        if (!overlayType.EditorVisible)
            throw new MapFacadeValidationException($"Overlay type '{overlayType.ININame}' is not available for placement in the editor.");
        if (!overlayType.IsValidForTheater(map.LoadedTheaterName))
            throw new MapFacadeValidationException($"Overlay type '{overlayType.ININame}' is not valid for theater '{map.LoadedTheaterName}'.");

        ValidateOverlayFrame(overlayType, frameIndex ?? 0);

        var targetMapTiles = GetValidatedMapTilesInArea(x, y, width, height, "overlay placement");

        var affectedMapTiles = targetMapTiles
            .SelectMany(mapTile => GetMapTileAndSurroundings(mapTile.CoordsToPoint()))
            .Distinct()
            .ToList();

        var mutation = new PlaceOverlayMutation(mutationTarget, overlayType, frameIndex, new Point2D(x, y), new BrushSize(width, height));

        if (!mutation.ShouldPerform())
            throw new MapFacadeValidationException($"The requested area already contains overlay '{overlayType.ININame}' with the requested frame settings.");

        mutationManager.PerformMutation(mutation);

        return new MapEditResult(
            mutationManager.Revision,
            affectedMapTiles.Select(mapTile => CellInfo.FromMapCell(map, mapTile)).ToList());
    }

    public MapEditResult PlaceConnectedOverlay(string connectedOverlayName, int x, int y, int width, int height)
    {
        if (string.IsNullOrWhiteSpace(connectedOverlayName))
            throw new MapFacadeValidationException("A connected overlay type name must be provided.");

        var connectedOverlay = map.EditorConfig.ConnectedOverlays.Find(
            candidate => string.Equals(candidate.Name, connectedOverlayName, StringComparison.OrdinalIgnoreCase));
        if (connectedOverlay == null)
            throw new MapFacadeValidationException($"Connected overlay type '{connectedOverlayName}' does not exist in the editor configuration.");
        if (!connectedOverlay.Frames.TrueForAll(frame => frame.OverlayType.IsValidForTheater(map.LoadedTheaterName)))
            throw new MapFacadeValidationException($"Connected overlay type '{connectedOverlay.Name}' is not valid for theater '{map.LoadedTheaterName}'.");

        foreach (var connectedOverlayFrame in connectedOverlay.Frames)
            ValidateOverlayFrame(connectedOverlayFrame.OverlayType, connectedOverlayFrame.FrameIndex);

        var targetMapTiles = GetValidatedMapTilesInArea(x, y, width, height, "connected overlay placement");
        var affectedMapTiles = targetMapTiles
            .SelectMany(mapTile => GetMapTileAndSurroundings(mapTile.CoordsToPoint()))
            .Distinct()
            .ToList();

        mutationManager.PerformMutation(new PlaceConnectedOverlayMutation(
            mutationTarget,
            connectedOverlay,
            new Point2D(x, y),
            new BrushSize(width, height)));

        return new MapEditResult(
            mutationManager.Revision,
            affectedMapTiles.Select(mapTile => CellInfo.FromMapCell(map, mapTile)).ToList());
    }

    public MapEditResult DrawConnectedTiles(string connectedTileTypeName, List<MapConnectedTilePathVertex> path,
        string side, int randomSeed, int extraHeight)
    {
        return DrawConnectedTiles(connectedTileTypeName, path, side, randomSeed, extraHeight,
            useEndPieces: false, closed: false);
    }

    public MapEditResult DrawConnectedTiles(string connectedTileTypeName, List<MapConnectedTilePathVertex> path,
        string side, int randomSeed, int extraHeight, bool useEndPieces = false, bool closed = false)
    {
        if (string.IsNullOrWhiteSpace(connectedTileTypeName))
            throw new MapFacadeValidationException("A connected tile type INI name must be provided.");

        var connectedTileType = map.EditorConfig.ConnectedTileTypes.Find(
            candidate => string.Equals(candidate.IniName, connectedTileTypeName, StringComparison.OrdinalIgnoreCase));
        if (connectedTileType == null)
            throw new MapFacadeValidationException($"Connected tile type '{connectedTileTypeName}' does not exist in the editor configuration.");
        if (!connectedTileType.IsLegal)
            throw new MapFacadeValidationException($"Connected tile type '{connectedTileTypeName}' is not available because it is configured incorrectly.");
        if (!IsConnectedTileTypeAvailableInTheater(connectedTileType))
            throw new MapFacadeValidationException($"Connected tile type '{connectedTileType.IniName}' is not valid for theater '{map.LoadedTheaterName}'.");

        if (Constants.IsFlatWorld && extraHeight != 0)
            throw new MapFacadeValidationException("Extra height must be 0 when the active mod has flat maps.");

        if (path == null)
            throw new MapFacadeValidationException("A connected tile path must be provided.");
        if (path.Count < 2)
            throw new MapFacadeValidationException("A connected tile path must contain at least two vertices.");
        if (path.Count > MaxConnectedTilePathVertexCount)
            throw new MapFacadeValidationException($"A connected tile path may contain at most {MaxConnectedTilePathVertexCount} vertices.");

        var pathCoords = new List<Point2D>(path.Count);
        for (int i = 0; i < path.Count; i++)
        {
            MapConnectedTilePathVertex vertex = path[i];
            if (vertex == null)
                throw new MapFacadeValidationException($"Connected tile path vertex {i} cannot be null.");

            var coords = new Point2D(vertex.X, vertex.Y);
            if (map.GetTile(coords) == null)
                throw new MapFacadeValidationException($"Connected tile path vertex {i} at ({vertex.X}, {vertex.Y}) is outside the map.");
            if (i > 0 && coords == pathCoords[i - 1])
                throw new MapFacadeValidationException($"Connected tile path vertices {i - 1} and {i} cannot have the same coordinates.");

            pathCoords.Add(coords);
        }

        if (closed && pathCoords.Distinct().Count() < 3)
            throw new MapFacadeValidationException("A closed connected tile path must contain at least three distinct vertices.");

        ConnectedTileSide startingSide;
        if (string.Equals(side, nameof(ConnectedTileSide.Front), StringComparison.OrdinalIgnoreCase))
            startingSide = ConnectedTileSide.Front;
        else if (string.Equals(side, nameof(ConnectedTileSide.Back), StringComparison.OrdinalIgnoreCase))
            startingSide = ConnectedTileSide.Back;
        else
            throw new MapFacadeValidationException("Connected tile side must be Front or Back.");

        if (connectedTileType.FrontOnly && startingSide != ConnectedTileSide.Front)
            throw new MapFacadeValidationException($"Connected tile type '{connectedTileType.IniName}' only supports the Front side.");

        if (useEndPieces && !closed && !connectedTileType.SupportsEndPieces)
            throw new MapFacadeValidationException($"Connected tile type '{connectedTileType.IniName}' does not support ending pieces.");

        int originLevel = map.GetTile(pathCoords[0]).Level;
        if (extraHeight < 0 || extraHeight > Constants.MaxMapHeightLevel - originLevel)
        {
            throw new MapFacadeValidationException(
                $"extraHeight must be from 0 through {Constants.MaxMapHeightLevel - originLevel} for the first path vertex's current height.");
        }

        ValidateConnectedTileType(connectedTileType);

        bool effectiveUseEndPieces = useEndPieces && !closed;
        var planResult = ConnectedTilePlanner.Plan(
            connectedTileType,
            pathCoords,
            startingSide,
            randomSeed,
            effectiveUseEndPieces,
            closed,
            coords => map.GetTile(coords) != null);

        if (!planResult.IsSuccess)
        {
            string failureReason = string.IsNullOrWhiteSpace(planResult.Message)
                ? planResult.Status switch
                {
                    ConnectedTilePlanStatus.InvalidInput => "the path is invalid",
                    ConnectedTilePlanStatus.NoSolution => "no exact connection pattern fits the requested path",
                    ConnectedTilePlanStatus.SearchLimit => "the search limit was reached before an exact pattern was found",
                    _ => "the connected tile planner failed"
                }
                : planResult.Message;

            throw new MapFacadeValidationException(
                $"Unable to plan the connected tile formation: {failureReason}");
        }

        var mutation = new DrawConnectedTilesMutation(
            mutationTarget,
            planResult.Plan,
            randomSeed,
            (byte)extraHeight);

        mutationManager.PerformMutation(mutation);

        return new MapEditResult(
            mutationManager.Revision,
            mutation.AffectedCellCoords
                .Select(coords => CellInfo.FromMapCell(map, map.GetTile(coords)))
                .OrderBy(cellInfo => cellInfo.Y)
                .ThenBy(cellInfo => cellInfo.X)
                .ToList());
    }

    public MapEditResult PlaceWaypoint(int identifier, int x, int y, string editorColor)
    {
        if (identifier < 0 || identifier >= Constants.MaxWaypoint)
        {
            throw new MapFacadeValidationException(
                $"Waypoint identifier must be from 0 through {Constants.MaxWaypoint - 1}.");
        }

        if (map.Waypoints.Exists(waypoint => waypoint.Identifier == identifier))
            throw new MapFacadeValidationException($"Waypoint {identifier} already exists on the map.");

        var cellCoords = new Point2D(x, y);
        var mapTile = map.GetTile(cellCoords);
        if (mapTile == null)
            throw new MapFacadeValidationException($"Cell ({x}, {y}) is outside the map.");

        string resolvedEditorColor = ResolveWaypointColor(editorColor);
        mutationManager.PerformMutation(new PlaceWaypointMutation(mutationTarget, cellCoords, identifier, resolvedEditorColor));

        return new MapEditResult(
            mutationManager.Revision,
            new List<CellInfo> { CellInfo.FromMapCell(map, mapTile) });
    }

    public MapEditResult PlaceTerrainObject(string terrainTypeName, int x, int y)
    {
        if (string.IsNullOrWhiteSpace(terrainTypeName))
            throw new MapFacadeValidationException("A terrain object type INI name must be provided.");

        var cellCoords = new Point2D(x, y);
        if (!map.IsCoordWithinMap(cellCoords))
            throw new MapFacadeValidationException($"Cell ({x}, {y}) is outside the map.");

        var mapTile = map.GetTile(cellCoords);
        if (mapTile == null)
            throw new MapFacadeValidationException($"Cell ({x}, {y}) is outside the map.");

        var terrainType = map.Rules.TerrainTypes.Find(tt => string.Equals(tt.ININame, terrainTypeName, StringComparison.OrdinalIgnoreCase));
        if (terrainType == null)
            throw new MapFacadeValidationException($"Terrain object type '{terrainTypeName}' does not exist in the loaded rules.");

        if (!terrainType.EditorVisible)
            throw new MapFacadeValidationException($"Terrain object type '{terrainType.ININame}' is not available for placement in the editor.");

        if (!terrainType.IsValidForTheater(map.LoadedTheaterName))
            throw new MapFacadeValidationException($"Terrain object type '{terrainType.ININame}' is not valid for theater '{map.LoadedTheaterName}'.");

        if (mapTile.TerrainObject != null)
            throw new MapFacadeValidationException($"Cell ({x}, {y}) already contains terrain object '{mapTile.TerrainObject.TerrainType.ININame}'.");

        var mutation = new PlaceTerrainObjectMutation(mutationTarget, terrainType, cellCoords);
        if (!mutation.ShouldPerform())
            throw new MapFacadeValidationException($"Terrain object '{terrainType.ININame}' cannot be placed at ({x}, {y}).");

        mutationManager.PerformMutation(mutation);

        return new MapEditResult(
            mutationManager.Revision,
            new List<CellInfo> { CellInfo.FromMapCell(map, mapTile) });
    }

    public MapEditResult PlaceBuilding(string buildingTypeName, string ownerName, int x, int y, bool allowOverlap, MapBuildingPlacementProperties properties)
    {
        if (string.IsNullOrWhiteSpace(buildingTypeName))
            throw new MapFacadeValidationException("A building type INI name must be provided.");

        if (string.IsNullOrWhiteSpace(ownerName))
            throw new MapFacadeValidationException("An owner house name must be provided.");

        var buildingType = map.Rules.BuildingTypes.Find(bt => string.Equals(bt.ININame, buildingTypeName, StringComparison.OrdinalIgnoreCase));
        if (buildingType == null)
            throw new MapFacadeValidationException($"Building type '{buildingTypeName}' does not exist in the loaded rules.");

        if (!buildingType.EditorVisible)
            throw new MapFacadeValidationException($"Building type '{buildingType.ININame}' is not available for placement in the editor.");

        if (!buildingType.IsValidForTheater(map.LoadedTheaterName))
            throw new MapFacadeValidationException($"Building type '{buildingType.ININame}' is not valid for theater '{map.LoadedTheaterName}'.");

        var owner = map.GetHouses().Find(
            house => string.Equals(house.ININame, ownerName, StringComparison.OrdinalIgnoreCase));
        if (owner == null)
            throw new MapFacadeValidationException($"House '{ownerName}' does not exist on the map.");

        var cellCoords = new Point2D(x, y);
        if (!map.IsCoordWithinMap(cellCoords) || map.GetTile(cellCoords) == null)
            throw new MapFacadeValidationException($"Cell ({x}, {y}) is outside the map.");

        var structure = new Structure(buildingType)
        {
            Owner = owner,
            Position = cellCoords,
            AIRepairable = buildingType.Repairable && owner.DefaultRepairableStructures
        };
        ApplyBuildingPlacementProperties(structure, properties);

        var foundationCells = new List<MapTile>();
        Point2D? invalidFoundationCoords = null;
        buildingType.ArtConfig.DoForFoundationCoordsOrOrigin(offset =>
        {
            var foundationCoords = cellCoords + offset;
            var foundationCell = map.IsCoordWithinMap(foundationCoords) ? map.GetTile(foundationCoords) : null;
            if (foundationCell == null)
            {
                invalidFoundationCoords ??= foundationCoords;
                return;
            }

            foundationCells.Add(foundationCell);
        });

        if (invalidFoundationCoords.HasValue)
        {
            throw new MapFacadeValidationException(
                $"The building foundation extends outside the map at ({invalidFoundationCoords.Value.X}, {invalidFoundationCoords.Value.Y}).");
        }

        if (!map.CanPlaceObjectAt(structure, cellCoords, false, allowOverlap))
        {
            throw new MapFacadeValidationException(
                $"Building '{buildingType.ININame}' cannot be placed at ({x}, {y}) because its foundation overlaps another building.");
        }

        mutationManager.PerformMutation(new PlaceBuildingMutation(mutationTarget, structure));

        return new MapEditResult(
            mutationManager.Revision,
            foundationCells.Select(cell => CellInfo.FromMapCell(map, cell)).ToList());
    }

    public MapEditResult PlaceAircraft(string aircraftTypeName, string ownerName, int x, int y, bool allowOverlap, MapFootPlacementProperties properties)
    {
        if (string.IsNullOrWhiteSpace(aircraftTypeName))
            throw new MapFacadeValidationException("An aircraft type INI name must be provided.");

        if (string.IsNullOrWhiteSpace(ownerName))
            throw new MapFacadeValidationException("An owner house name must be provided.");

        var aircraftType = map.Rules.AircraftTypes.Find(
            at => string.Equals(at.ININame, aircraftTypeName, StringComparison.OrdinalIgnoreCase));
        if (aircraftType == null)
            throw new MapFacadeValidationException($"Aircraft type '{aircraftTypeName}' does not exist in the loaded rules.");

        if (!aircraftType.EditorVisible)
            throw new MapFacadeValidationException($"Aircraft type '{aircraftType.ININame}' is not available for placement in the editor.");

        if (!aircraftType.IsValidForTheater(map.LoadedTheaterName))
            throw new MapFacadeValidationException($"Aircraft type '{aircraftType.ININame}' is not valid for theater '{map.LoadedTheaterName}'.");

        var owner = map.GetHouses().Find(house => string.Equals(house.ININame, ownerName, StringComparison.OrdinalIgnoreCase));
        if (owner == null)
            throw new MapFacadeValidationException($"House '{ownerName}' does not exist on the map.");

        var cellCoords = new Point2D(x, y);
        if (!map.IsCoordWithinMap(cellCoords) || map.GetTile(cellCoords) == null)
            throw new MapFacadeValidationException($"Cell ({x}, {y}) is outside the map.");

        var aircraft = new Aircraft(aircraftType)
        {
            Owner = owner,
            Position = cellCoords
        };
        ApplyFootPlacementProperties(aircraft, properties, false, false);

        if (!map.CanPlaceObjectAt(aircraft, cellCoords, false, allowOverlap))
        {
            throw new MapFacadeValidationException($"Aircraft '{aircraftType.ININame}' cannot be placed at ({x}, {y}) because the cell already contains aircraft.");
        }

        mutationManager.PerformMutation(new PlaceAircraftMutation(mutationTarget, aircraft));

        return new MapEditResult(
            mutationManager.Revision,
            new List<CellInfo> { CellInfo.FromMapCell(map, map.GetTile(cellCoords)) });
    }

    public MapEditResult PlaceInfantry(string infantryTypeName, string ownerName, int x, int y, MapFootPlacementProperties properties)
    {
        if (string.IsNullOrWhiteSpace(infantryTypeName))
            throw new MapFacadeValidationException("An infantry type INI name must be provided.");

        if (string.IsNullOrWhiteSpace(ownerName))
            throw new MapFacadeValidationException("An owner house name must be provided.");

        var infantryType = map.Rules.InfantryTypes.Find(it => string.Equals(it.ININame, infantryTypeName, StringComparison.OrdinalIgnoreCase));
        if (infantryType == null)
            throw new MapFacadeValidationException($"Infantry type '{infantryTypeName}' does not exist in the loaded rules.");

        if (!infantryType.EditorVisible)
            throw new MapFacadeValidationException($"Infantry type '{infantryType.ININame}' is not available for placement in the editor.");

        if (!infantryType.IsValidForTheater(map.LoadedTheaterName))
            throw new MapFacadeValidationException($"Infantry type '{infantryType.ININame}' is not valid for theater '{map.LoadedTheaterName}'.");

        var owner = map.GetHouses().Find(house => string.Equals(house.ININame, ownerName, StringComparison.OrdinalIgnoreCase));
        if (owner == null)
            throw new MapFacadeValidationException($"House '{ownerName}' does not exist on the map.");

        var cellCoords = new Point2D(x, y);
        var mapTile = map.IsCoordWithinMap(cellCoords) ? map.GetTile(cellCoords) : null;
        if (mapTile == null)
            throw new MapFacadeValidationException($"Cell ({x}, {y}) is outside the map.");

        SubCell freeSubCell = GetInfantryPlacementSubCell(mapTile, properties?.SubCell);
        if (freeSubCell == SubCell.None)
        {
            throw new MapFacadeValidationException($"Infantry '{infantryType.ININame}' cannot be placed at ({x}, {y}) because all usable infantry subcells are occupied.");
        }

        var infantry = new Infantry(infantryType)
        {
            Owner = owner,
            Position = cellCoords,
            SubCell = freeSubCell
        };

        ApplyFootPlacementProperties(infantry, properties, true, true);

        mutationManager.PerformMutation(new PlaceInfantryMutation(mutationTarget, infantry));

        return new MapEditResult(
            mutationManager.Revision,
            new List<CellInfo> { CellInfo.FromMapCell(map, mapTile) });
    }

    public MapEditResult PlaceVehicle(string vehicleTypeName, string ownerName, int x, int y, bool allowOverlap, MapFootPlacementProperties properties)
    {
        if (string.IsNullOrWhiteSpace(vehicleTypeName))
            throw new MapFacadeValidationException("A vehicle type INI name must be provided.");

        if (string.IsNullOrWhiteSpace(ownerName))
            throw new MapFacadeValidationException("An owner house name must be provided.");

        var vehicleType = map.Rules.UnitTypes.Find(ut => string.Equals(ut.ININame, vehicleTypeName, StringComparison.OrdinalIgnoreCase));
        if (vehicleType == null)
            throw new MapFacadeValidationException($"Vehicle type '{vehicleTypeName}' does not exist in the loaded rules.");

        if (!vehicleType.EditorVisible)
            throw new MapFacadeValidationException($"Vehicle type '{vehicleType.ININame}' is not available for placement in the editor.");

        if (!vehicleType.IsValidForTheater(map.LoadedTheaterName))
            throw new MapFacadeValidationException($"Vehicle type '{vehicleType.ININame}' is not valid for theater '{map.LoadedTheaterName}'.");

        var owner = map.GetHouses().Find(house => string.Equals(house.ININame, ownerName, StringComparison.OrdinalIgnoreCase));
        if (owner == null)
            throw new MapFacadeValidationException($"House '{ownerName}' does not exist on the map.");

        var cellCoords = new Point2D(x, y);
        if (!map.IsCoordWithinMap(cellCoords) || map.GetTile(cellCoords) == null)
            throw new MapFacadeValidationException($"Cell ({x}, {y}) is outside the map.");

        var vehicle = new Unit(vehicleType)
        {
            Owner = owner,
            Position = cellCoords
        };

        ApplyFootPlacementProperties(vehicle, properties, true, false);

        if (!map.CanPlaceObjectAt(vehicle, cellCoords, false, allowOverlap))
        {
            throw new MapFacadeValidationException($"Vehicle '{vehicleType.ININame}' cannot be placed at ({x}, {y}) because the cell already contains a vehicle.");
        }

        mutationManager.PerformMutation(new PlaceVehicleMutation(mutationTarget, vehicle));

        return new MapEditResult(
            mutationManager.Revision,
            new List<CellInfo> { CellInfo.FromMapCell(map, map.GetTile(cellCoords)) });
    }

    public MapEditResult PlaceTerrainTile(string tileSetName, int tileIndexInTileSet, int x, int y,
        int brushWidth, int brushHeight, bool autoLAT)
    {
        if (string.IsNullOrWhiteSpace(tileSetName))
            throw new MapFacadeValidationException("A tile set name must be provided.");

        var tileSet = map.TheaterInstance.Theater.TileSets.Find(ts => ts.AllowToPlace && string.Equals(ts.SetName, tileSetName, StringComparison.OrdinalIgnoreCase));
        if (tileSet == null)
            throw new MapFacadeValidationException($"Tile set '{tileSetName}' does not exist in the loaded theater.");

        if (!IsTileSetPlaceable(tileSet))
            throw new MapFacadeValidationException($"Tile set '{tileSet.SetName}' is not available for placement in the editor.");

        if (tileIndexInTileSet < 0 || tileIndexInTileSet >= tileSet.LoadedTileCount)
        {
            throw new MapFacadeValidationException(
                $"Tile index {tileIndexInTileSet} is outside tile set '{tileSet.SetName}', which contains {tileSet.LoadedTileCount} tiles.");
        }

        // Use an existing brush size instance if preconfigured. If not, create a new one.
        var brushSize = map.EditorConfig.BrushSizes.Find(bs => bs.Width == brushWidth && bs.Height == brushHeight);
        if (brushSize == null)
            brushSize = new BrushSize(brushWidth, brushHeight);

        if (tileSet.Only1x1 && (brushSize.Width != 1 || brushSize.Height != 1))
            throw new MapFacadeValidationException($"Tile set '{tileSet.SetName}' only supports a 1x1 brush.");

        int tileIndex = tileSet.StartTileIndex + tileIndexInTileSet;
        if (tileIndex < 0 || tileIndex >= mutationTarget.TheaterGraphics.TileCount)
            throw new MapFacadeValidationException($"Absolute tile index {tileIndex} is not loaded.");

        ITileImage tile = map.TheaterInstance.GetTile(tileIndex);
        if (tile == null || tile.Width <= 0 || tile.Height <= 0 || tile.SubTileCount <= 0)
        {
            throw new MapFacadeValidationException($"Tile {tileIndexInTileSet} from tile set '{tileSet.SetName}' has no usable tile graphics.");
        }

        var cellCoords = new Point2D(x, y);
        ValidateTerrainTileFootprint(tile, cellCoords, brushSize);

        var mutation = new PlaceTerrainTileMutation(
            mutationTarget,
            cellCoords,
            tile,
            0,
            brushSize,
            autoLAT,
            false);

        if (!mutation.ShouldPerform())
        {
            throw new MapFacadeValidationException($"Tile {tileIndexInTileSet} from tile set '{tileSet.SetName}' cannot be placed at ({x}, {y}).");
        }

        mutationManager.PerformMutation(mutation);

        int footprintWidth = tile.Width * brushSize.Width;
        int footprintHeight = tile.Height * brushSize.Height;
        var affectedArea = autoLAT
            ? new Rectangle(x - 1, y - 1, footprintWidth + 3, footprintHeight + 3)
            : new Rectangle(x, y, footprintWidth, footprintHeight);

        return new MapEditResult(mutationManager.Revision, InspectRegion(affectedArea));
    }

    public MapEditResult SetCellsTerrain(List<MapCellCoordinate> cells, int tileIndex, int subTileIndex, int? expectedRevision)
    {
        if (expectedRevision.HasValue && expectedRevision.Value != mutationManager.Revision)
        {
            throw new MapFacadeValidationException(
                $"The map revision changed from {expectedRevision.Value} to {mutationManager.Revision}. Query the map again before setting terrain, or omit expectedRevision to allow concurrent edits.");
        }

        if (cells == null || cells.Count == 0)
            throw new MapFacadeValidationException("At least one cell coordinate must be provided.");
        if (cells.Count > MaxMapOperationCellCount)
            throw new MapFacadeValidationException($"At most {MaxMapOperationCellCount} cell coordinates can be set in one call.");

        if (tileIndex < 0 || tileIndex >= mutationTarget.TheaterGraphics.TileCount)
            throw new MapFacadeValidationException($"Absolute tile index {tileIndex} is not loaded.");

        TileImage tile = mutationTarget.TheaterGraphics.GetTileImage(tileIndex);
        if (tile == null || tile.SubTileCount <= 0)
            throw new MapFacadeValidationException($"Absolute tile index {tileIndex} has no usable tile graphics.");

        if (subTileIndex < 0 || subTileIndex >= tile.SubTileCount || subTileIndex > byte.MaxValue || tile.GetSubTile(subTileIndex) == null)
        {
            throw new MapFacadeValidationException(
                $"Sub-tile index {subTileIndex} is not valid for absolute tile index {tileIndex}.");
        }

        var distinctCoords = new HashSet<Point2D>();
        for (int i = 0; i < cells.Count; i++)
        {
            MapCellCoordinate cell = cells[i];
            if (cell == null)
                throw new MapFacadeValidationException($"Cell coordinate {i} cannot be null.");

            var cellCoords = new Point2D(cell.X, cell.Y);
            if (map.GetTile(cellCoords) == null)
                throw new MapFacadeValidationException($"Cell coordinate {i} at ({cell.X}, {cell.Y}) is outside the map.");

            distinctCoords.Add(cellCoords);
        }

        var changedCoords = distinctCoords
            .Where(coords =>
            {
                MapTile mapTile = map.GetTile(coords);
                return mapTile.TileIndex != tileIndex || mapTile.SubTileIndex != subTileIndex;
            })
            .OrderBy(coords => coords.Y)
            .ThenBy(coords => coords.X)
            .ToList();

        if (changedCoords.Count == 0)
            return new MapEditResult(mutationManager.Revision, new List<CellInfo>());

        mutationManager.PerformMutation(new SetCellsTerrainMutation(
            mutationTarget,
            changedCoords,
            tileIndex,
            (byte)subTileIndex));

        return new MapEditResult(
            mutationManager.Revision,
            changedCoords.Select(coords => CellInfo.FromMapCell(map, map.GetTile(coords))).ToList());
    }

    private void ApplyModificationProperties(TechnoBase techno, TechnoPropertiesSnapshot snapshot, MapTechnoModificationProperties properties)
    {
        if (properties.Owner != null)
            snapshot.Owner = ResolveHouse(properties.Owner);
        if (properties.Health.HasValue)
            snapshot.Health = properties.Health.Value;
        if (properties.Facing.HasValue)
            snapshot.Facing = (byte)properties.Facing.Value;
        if (properties.AttachedTag != null)
            snapshot.AttachedTag = ResolveAttachedTag(properties.AttachedTag);
        else if (properties.ClearAttachedTag)
            snapshot.AttachedTag = null;

        if (techno is Structure structure)
        {
            if (HasFootModification(properties))
                throw new MapFacadeValidationException("Mobile techno properties cannot be applied to buildings.");

            ApplyBuildingModificationProperties(structure, snapshot, properties);
            return;
        }

        if (HasBuildingModification(properties))
            throw new MapFacadeValidationException("Building properties cannot be applied to Unit, Infantry, or Aircraft objects.");
        if (techno is Aircraft && properties.OnBridge.HasValue)
            throw new MapFacadeValidationException("The onBridge property is not valid for aircraft.");

        if (properties.Mission != null)
            snapshot.Mission = ResolveMission(properties.Mission);
        if (properties.Veterancy.HasValue)
            snapshot.Veterancy = properties.Veterancy.Value;
        if (properties.Group.HasValue)
            snapshot.Group = properties.Group.Value;
        if (properties.OnBridge.HasValue)
            snapshot.OnBridge = properties.OnBridge.Value;
        if (properties.AutocreateNoRecruitable.HasValue)
            snapshot.AutocreateNoRecruitable = properties.AutocreateNoRecruitable.Value;
        if (properties.AutocreateYesRecruitable.HasValue)
            snapshot.AutocreateYesRecruitable = properties.AutocreateYesRecruitable.Value;
    }

    private void ApplyBuildingModificationProperties(Structure structure, TechnoPropertiesSnapshot snapshot, MapTechnoModificationProperties properties)
    {
        if (properties.AISellable.HasValue)
            snapshot.AISellable = properties.AISellable.Value;
        if (properties.AIRebuildable.HasValue)
            snapshot.AIRebuildable = properties.AIRebuildable.Value;
        if (properties.Powered.HasValue)
            snapshot.Powered = properties.Powered.Value;
        if (properties.AIRepairable.HasValue)
            snapshot.AIRepairable = properties.AIRepairable.Value;
        if (properties.Nominal.HasValue)
            snapshot.Nominal = properties.Nominal.Value;
        if (properties.Spotlight.HasValue)
            snapshot.Spotlight = (SpotlightType)properties.Spotlight.Value;

        ApplyBuildingUpgradeModification(structure, snapshot, properties.Upgrade1, properties.ClearUpgrade1, 0);
        ApplyBuildingUpgradeModification(structure, snapshot, properties.Upgrade2, properties.ClearUpgrade2, 1);
        ApplyBuildingUpgradeModification(structure, snapshot, properties.Upgrade3, properties.ClearUpgrade3, 2);
    }

    private void ApplyBuildingUpgradeModification(Structure structure, TechnoPropertiesSnapshot snapshot, string upgradeName, bool clearUpgrade, int upgradeIndex)
    {
        if (upgradeName == null && !clearUpgrade)
            return;

        if (upgradeIndex >= structure.ObjectType.Upgrades)
        {
            throw new MapFacadeValidationException(
                $"Building type '{structure.ObjectType.ININame}' does not support upgrade slot #{upgradeIndex + 1}.");
        }

        snapshot.Upgrades[upgradeIndex] = clearUpgrade ? null : ResolveBuildingUpgrade(structure.ObjectType, upgradeName, upgradeIndex);
    }

    private void ValidateCommonModificationProperties(MapTechnoModificationProperties properties)
    {
        if (properties.Health.HasValue && (properties.Health.Value < 1 || properties.Health.Value > Constants.ObjectHealthMax))
            throw new MapFacadeValidationException($"Health must be from 1 through {Constants.ObjectHealthMax}.");
        if (properties.Facing.HasValue && (properties.Facing.Value < 0 || properties.Facing.Value > Constants.FacingMax))
            throw new MapFacadeValidationException($"Facing must be from 0 through {Constants.FacingMax}.");
        if (properties.AttachedTag != null && properties.ClearAttachedTag)
            throw new MapFacadeValidationException("attachedTag and clearAttachedTag cannot be used together.");
        if (properties.Veterancy.HasValue && Array.IndexOf(ValidVeterancyLevels, properties.Veterancy.Value) < 0)
            throw new MapFacadeValidationException("Veterancy must be 0, 50, 100, 150, or 200.");
        if (properties.Mission != null)
            ResolveMission(properties.Mission);
        if (properties.Spotlight.HasValue && !Enum.IsDefined(typeof(SpotlightType), properties.Spotlight.Value))
            throw new MapFacadeValidationException("Spotlight must be 0, 1, or 2.");
        if (properties.Upgrade1 != null && properties.ClearUpgrade1)
            throw new MapFacadeValidationException("upgrade1 and clearUpgrade1 cannot be used together.");
        if (properties.Upgrade2 != null && properties.ClearUpgrade2)
            throw new MapFacadeValidationException("upgrade2 and clearUpgrade2 cannot be used together.");
        if (properties.Upgrade3 != null && properties.ClearUpgrade3)
            throw new MapFacadeValidationException("upgrade3 and clearUpgrade3 cannot be used together.");

        if (properties.Owner != null)
            ResolveHouse(properties.Owner);
        if (properties.AttachedTag != null)
            ResolveAttachedTag(properties.AttachedTag);
    }

    private TechnoBase ResolveTechnoReference(MapTechnoReference technoReference)
    {
        if (technoReference == null)
            throw new MapFacadeValidationException("A techno reference cannot be null.");
        if (technoReference.ObjectId <= 0)
            throw new MapFacadeValidationException("A techno reference objectId must be greater than zero.");

        TechnoBase techno = map.Structures.Cast<TechnoBase>()
            .Concat(map.Units)
            .Concat(map.Infantry)
            .Concat(map.Aircraft)
            .FirstOrDefault(candidate => candidate.ObjectId == technoReference.ObjectId);

        return techno ?? throw new MapFacadeValidationException($"Techno objectId {technoReference.ObjectId} does not exist on the current map.");
    }

    private TerrainObject ResolveTerrainObjectReference(MapTerrainObjectReference terrainObjectReference)
    {
        if (terrainObjectReference == null)
            throw new MapFacadeValidationException("A terrain object reference cannot be null.");

        if (string.IsNullOrWhiteSpace(terrainObjectReference.ININame))
            throw new MapFacadeValidationException("A terrain object reference must include an INI name.");

        var mapTile = map.GetTile(terrainObjectReference.X, terrainObjectReference.Y);
        if (mapTile == null)
            throw new MapFacadeValidationException($"Cell ({terrainObjectReference.X}, {terrainObjectReference.Y}) is outside the map.");

        var terrainObject = mapTile.TerrainObject;
        if (terrainObject == null)
            throw new MapFacadeValidationException($"Cell ({terrainObjectReference.X}, {terrainObjectReference.Y}) does not contain a terrain object.");

        if (!string.Equals(terrainObject.TerrainType.ININame, terrainObjectReference.ININame, StringComparison.OrdinalIgnoreCase))
        {
            throw new MapFacadeValidationException(
                $"Cell ({terrainObjectReference.X}, {terrainObjectReference.Y}) contains terrain object '{terrainObject.TerrainType.ININame}', not '{terrainObjectReference.ININame}'.");
        }

        return terrainObject;
    }

    private List<MapTile> GetAffectedMapTiles(List<GameObject> objects)
    {
        var affectedMapTiles = new List<MapTile>();
        foreach (var mapObject in objects)
        {
            if (mapObject is Structure structure)
            {
                structure.ObjectType.ArtConfig.DoForFoundationCoordsOrOrigin(offset =>
                {
                    var mapTile = map.GetTile(structure.Position + offset);
                    if (mapTile != null)
                        affectedMapTiles.Add(mapTile);
                });
            }
            else
            {
                var mapTile = map.GetTile(mapObject.Position);
                if (mapTile != null)
                    affectedMapTiles.Add(mapTile);
            }
        }

        return affectedMapTiles.Distinct().ToList();
    }

    private IEnumerable<MapTile> GetMapTileAndSurroundings(Point2D cellCoords)
    {
        for (int yOffset = -1; yOffset <= 1; yOffset++)
        {
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                var mapTile = map.GetTile(cellCoords + new Point2D(xOffset, yOffset));
                if (mapTile != null)
                    yield return mapTile;
            }
        }
    }

    private List<MapTile> GetValidatedMapTilesInArea(int x, int y, int width, int height, string operationName)
    {
        if (width <= 0 || height <= 0)
            throw new MapFacadeValidationException($"The {operationName} width and height must both be greater than zero.");

        if (width > MaxMapOperationDimension || height > MaxMapOperationDimension || (long)width * height > MaxMapOperationCellCount)
        {
            throw new MapFacadeValidationException(
                $"The {operationName} area is too large. Each dimension may be at most {MaxMapOperationDimension} cells and the total area may be at most {MaxMapOperationCellCount} cells.");
        }

        var mapTiles = new List<MapTile>(width * height);
        for (int yOffset = 0; yOffset < height; yOffset++)
        {
            for (int xOffset = 0; xOffset < width; xOffset++)
            {
                int targetX = x + xOffset;
                int targetY = y + yOffset;
                var mapTile = map.GetTile(targetX, targetY);
                if (mapTile == null)
                    throw new MapFacadeValidationException($"The {operationName} area includes cell ({targetX}, {targetY}), which is outside the map.");

                mapTiles.Add(mapTile);
            }
        }

        return mapTiles;
    }

    private int GetPlaceableOverlayFrameCount(OverlayType overlayType)
    {
        var overlayTextures = mutationTarget.TheaterGraphics.OverlayTextures;
        if (overlayTextures == null || overlayType.Index < 0 || overlayType.Index >= overlayTextures.Length)
            return 0;

        if (overlayTextures[overlayType.Index] == null)
            return 0;

        return mutationTarget.TheaterGraphics.GetOverlayFrameCount(overlayType);
    }

    private bool IsConnectedTileTypeAvailable(ConnectedTileType connectedTileType)
    {
        return IsConnectedTileTypeAvailableInTheater(connectedTileType);
    }

    private bool IsConnectedTileTypeAvailableInTheater(ConnectedTileType connectedTileType)
    {
        return connectedTileType.AllowedTheaters.Exists(
            theaterName => string.Equals(theaterName, map.LoadedTheaterName, StringComparison.OrdinalIgnoreCase));
    }

    private void ValidateConnectedTileType(ConnectedTileType connectedTileType)
    {
        if (connectedTileType.Tiles.Count == 0)
            throw new MapFacadeValidationException($"Connected tile type '{connectedTileType.IniName}' does not contain any tiles.");

        foreach (ConnectedTile connectedTile in connectedTileType.Tiles)
        {
            var tileSet = map.TheaterInstance.Theater.TileSets.Find(
                candidate => candidate.AllowToPlace && string.Equals(candidate.SetName, connectedTile.TileSetName, StringComparison.OrdinalIgnoreCase));
            if (tileSet == null)
            {
                throw new MapFacadeValidationException(
                    $"Connected tile type '{connectedTileType.IniName}' references unavailable tile set '{connectedTile.TileSetName}'.");
            }

            if (connectedTile.Foundation == null || connectedTile.Foundation.Count == 0)
            {
                throw new MapFacadeValidationException(
                    $"Connected tile type '{connectedTileType.IniName}' tile {connectedTile.Index} has no usable foundation.");
            }

            foreach (int tileIndexInSet in connectedTile.IndicesInTileSet)
            {
                if (tileIndexInSet < 0 || tileIndexInSet >= tileSet.LoadedTileCount ||
                    mutationTarget.TheaterGraphics.GetTileGraphics(tileSet.StartTileIndex + tileIndexInSet) == null)
                {
                    throw new MapFacadeValidationException(
                        $"Connected tile type '{connectedTileType.IniName}' tile {connectedTile.Index} references unavailable tile index {tileIndexInSet} in tile set '{tileSet.SetName}'.");
                }
            }
        }
    }

    private void ValidateOverlayFrame(OverlayType overlayType, int frameIndex)
    {
        int placeableFrameCount = GetPlaceableOverlayFrameCount(overlayType);
        if (placeableFrameCount == 0)
            throw new MapFacadeValidationException($"Overlay type '{overlayType.ININame}' has no graphics for the current theater.");

        if (frameIndex < 0 || frameIndex >= placeableFrameCount)
        {
            throw new MapFacadeValidationException(
                $"Overlay type '{overlayType.ININame}' has {placeableFrameCount} placeable frames; frameIndex must be from 0 through {placeableFrameCount - 1}. " +
                "Higher raw SHP frame indexes are reserved for engine-managed shadows and cannot be placed directly.");
        }

        if (mutationTarget.TheaterGraphics.OverlayTextures[overlayType.Index].GetFrame(frameIndex) == null)
            throw new MapFacadeValidationException($"Frame {frameIndex} of overlay type '{overlayType.ININame}' has no valid graphics.");
    }

    private House ResolveHouse(string ownerName)
    {
        if (string.IsNullOrWhiteSpace(ownerName))
            throw new MapFacadeValidationException("An owner house name cannot be empty.");

        return map.GetHouses().Find(house => string.Equals(house.ININame, ownerName, StringComparison.OrdinalIgnoreCase)) ??
            throw new MapFacadeValidationException($"House '{ownerName}' does not exist on the map.");
    }

    private static string ResolveWaypointColor(string editorColor)
    {
        if (editorColor == null)
            return null;

        if (string.IsNullOrWhiteSpace(editorColor))
            throw new MapFacadeValidationException("A waypoint editor color cannot be empty.");

        int colorIndex = Array.FindIndex(
            Waypoint.SupportedColors,
            supportedColor => string.Equals(supportedColor.Name, editorColor.Trim(), StringComparison.OrdinalIgnoreCase));

        if (colorIndex < 0)
        {
            throw new MapFacadeValidationException(
                $"Waypoint editor color '{editorColor}' is not supported. Valid colors are: {string.Join(", ", Waypoint.SupportedColors.Select(color => color.Name))}.");
        }

        return Waypoint.SupportedColors[colorIndex].Name;
    }

    private static string ResolveMission(string missionName)
    {
        string mission = Array.Find(ValidMissions, validMission => string.Equals(validMission, missionName, StringComparison.OrdinalIgnoreCase));
        return mission ?? throw new MapFacadeValidationException($"Mission '{missionName}' is not available in the editor.");
    }

    private static bool HasAnyModification(MapTechnoModificationProperties properties)
    {
        return properties.Owner != null || properties.Health.HasValue || properties.Facing.HasValue || properties.AttachedTag != null || properties.ClearAttachedTag ||
            HasFootModification(properties) || HasBuildingModification(properties);
    }

    private static bool HasFootModification(MapTechnoModificationProperties properties)
    {
        return properties.Mission != null || properties.Veterancy.HasValue || properties.Group.HasValue || properties.OnBridge.HasValue ||
            properties.AutocreateNoRecruitable.HasValue || properties.AutocreateYesRecruitable.HasValue;
    }

    private static bool HasBuildingModification(MapTechnoModificationProperties properties)
    {
        return properties.AISellable.HasValue || properties.AIRebuildable.HasValue || properties.Powered.HasValue || properties.AIRepairable.HasValue ||
            properties.Nominal.HasValue || properties.Spotlight.HasValue || properties.Upgrade1 != null || properties.Upgrade2 != null || properties.Upgrade3 != null ||
            properties.ClearUpgrade1 || properties.ClearUpgrade2 || properties.ClearUpgrade3;
    }

    private void ApplyBuildingPlacementProperties(Structure structure, MapBuildingPlacementProperties properties)
    {
        if (properties == null)
            return;

        ApplyTechnoPlacementProperties(structure, properties.Health, properties.Facing, properties.AttachedTag);

        if (properties.AISellable.HasValue)
            structure.AISellable = properties.AISellable.Value;
        if (properties.AIRebuildable.HasValue)
            structure.AIRebuildable = properties.AIRebuildable.Value;
        if (properties.Powered.HasValue)
            structure.Powered = properties.Powered.Value;
        if (properties.AIRepairable.HasValue)
            structure.AIRepairable = properties.AIRepairable.Value;
        if (properties.Nominal.HasValue)
            structure.Nominal = properties.Nominal.Value;

        if (properties.Spotlight.HasValue)
        {
            if (!Enum.IsDefined(typeof(SpotlightType), properties.Spotlight.Value))
                throw new MapFacadeValidationException("Spotlight must be 0, 1, or 2.");

            structure.Spotlight = (SpotlightType)properties.Spotlight.Value;
        }

        bool upgradesChanged = false;
        upgradesChanged |= ApplyBuildingUpgrade(structure, properties.Upgrade1, 0);
        upgradesChanged |= ApplyBuildingUpgrade(structure, properties.Upgrade2, 1);
        upgradesChanged |= ApplyBuildingUpgrade(structure, properties.Upgrade3, 2);
        if (upgradesChanged)
            structure.UpdatePowerUpAnims();
    }

    private bool ApplyBuildingUpgrade(Structure structure, string upgradeName, int upgradeIndex)
    {
        if (upgradeName == null)
            return false;

        structure.Upgrades[upgradeIndex] = ResolveBuildingUpgrade(structure.ObjectType, upgradeName, upgradeIndex);
        return true;
    }

    private BuildingType ResolveBuildingUpgrade(BuildingType buildingType, string upgradeName, int upgradeIndex)
    {
        if (string.IsNullOrWhiteSpace(upgradeName))
            throw new MapFacadeValidationException($"Building upgrade #{upgradeIndex + 1} cannot be empty.");

        if (upgradeIndex >= buildingType.Upgrades)
        {
            throw new MapFacadeValidationException(
                $"Building type '{buildingType.ININame}' does not support upgrade slot #{upgradeIndex + 1}.");
        }

        var upgrade = map.Rules.BuildingTypes.Find(bt => string.Equals(bt.ININame, upgradeName, StringComparison.OrdinalIgnoreCase));
        if (upgrade == null)
            throw new MapFacadeValidationException($"Building upgrade type '{upgradeName}' does not exist in the loaded rules.");

        if (!string.Equals(upgrade.PowersUpBuilding, buildingType.ININame, StringComparison.OrdinalIgnoreCase))
        {
            throw new MapFacadeValidationException(
                $"Building type '{upgrade.ININame}' is not a valid upgrade for '{buildingType.ININame}'.");
        }

        return upgrade;
    }

    private void ApplyFootPlacementProperties<T>(Foot<T> foot, MapFootPlacementProperties properties, bool supportsOnBridge, bool supportsSubCell)
        where T : TechnoType
    {
        if (properties == null)
            return;

        if (properties.OnBridge.HasValue && !supportsOnBridge)
            throw new MapFacadeValidationException("The onBridge property is not valid for aircraft.");

        if (properties.SubCell.HasValue && !supportsSubCell)
            throw new MapFacadeValidationException("The subCell property is only valid for infantry.");

        ApplyTechnoPlacementProperties(foot, properties.Health, properties.Facing, properties.AttachedTag);

        if (properties.Mission != null)
        {
            string mission = Array.Find(ValidMissions, validMission => string.Equals(validMission, properties.Mission, StringComparison.OrdinalIgnoreCase));
            if (mission == null)
                throw new MapFacadeValidationException($"Mission '{properties.Mission}' is not available in the editor.");

            foot.Mission = mission;
        }

        if (properties.Veterancy.HasValue)
        {
            if (Array.IndexOf(ValidVeterancyLevels, properties.Veterancy.Value) < 0)
                throw new MapFacadeValidationException("Veterancy must be 0, 50, 100, 150, or 200.");

            foot.Veterancy = properties.Veterancy.Value;
        }

        if (properties.Group.HasValue)
            foot.Group = properties.Group.Value;
        if (properties.OnBridge.HasValue)
            foot.High = properties.OnBridge.Value;
        if (properties.AutocreateNoRecruitable.HasValue)
            foot.AutocreateNoRecruitable = properties.AutocreateNoRecruitable.Value;
        if (properties.AutocreateYesRecruitable.HasValue)
            foot.AutocreateYesRecruitable = properties.AutocreateYesRecruitable.Value;
    }

    private void ApplyTechnoPlacementProperties(TechnoBase techno, int? health, int? facing, string attachedTag)
    {
        if (health.HasValue)
        {
            if (health.Value < 1 || health.Value > Constants.ObjectHealthMax)
                throw new MapFacadeValidationException($"Health must be from 1 through {Constants.ObjectHealthMax}.");

            techno.HP = health.Value;
        }

        if (facing.HasValue)
        {
            if (facing.Value < 0 || facing.Value > Constants.FacingMax)
                throw new MapFacadeValidationException($"Facing must be from 0 through {Constants.FacingMax}.");

            techno.Facing = (byte)facing.Value;
        }

        if (attachedTag != null)
            techno.AttachedTag = ResolveAttachedTag(attachedTag);
    }

    private Tag ResolveAttachedTag(string tagNameOrID)
    {
        if (string.IsNullOrWhiteSpace(tagNameOrID))
            throw new MapFacadeValidationException("An attached tag cannot be empty.");

        var tagByID = map.Tags.Find(tag => string.Equals(tag.ID, tagNameOrID, StringComparison.OrdinalIgnoreCase));
        if (tagByID != null)
            return tagByID;

        var tagsByName = map.Tags.FindAll(tag => string.Equals(tag.Name, tagNameOrID, StringComparison.OrdinalIgnoreCase));
        if (tagsByName.Count == 0)
            throw new MapFacadeValidationException($"Tag '{tagNameOrID}' does not exist on the map.");
        if (tagsByName.Count > 1)
            throw new MapFacadeValidationException($"Multiple tags are named '{tagNameOrID}'; use the tag ID instead.");

        return tagsByName[0];
    }

    private static SubCell GetInfantryPlacementSubCell(MapTile mapTile, int? requestedSubCell)
    {
        if (!requestedSubCell.HasValue)
            return mapTile.GetFreeSubCellSpot();

        if (requestedSubCell.Value != (int)SubCell.Right && requestedSubCell.Value != (int)SubCell.Left && requestedSubCell.Value != (int)SubCell.Bottom)
            throw new MapFacadeValidationException("An infantry subcell must be 2 (Right), 3 (Left), or 4 (Bottom).");

        var subCell = (SubCell)requestedSubCell.Value;
        if (mapTile.Infantry[(int)subCell] != null)
        {
            throw new MapFacadeValidationException(
                $"Infantry cannot be placed at ({mapTile.X}, {mapTile.Y}) because subcell {requestedSubCell.Value} ({subCell}) is occupied.");
        }

        return subCell;
    }

    private void ValidateTerrainTileFootprint(ITileImage tile, Point2D cellCoords, BrushSize brushSize)
    {
        for (int brushY = 0; brushY < brushSize.Height; brushY++)
        {
            for (int brushX = 0; brushX < brushSize.Width; brushX++)
            {
                for (int subTileIndex = 0; subTileIndex < tile.SubTileCount; subTileIndex++)
                {
                    Point2D? subTileOffset = tile.GetSubTileCoordOffset(subTileIndex);
                    if (subTileOffset == null)
                        continue;

                    var targetCoords = cellCoords +
                        new Point2D(brushX * tile.Width, brushY * tile.Height) +
                        subTileOffset.Value;

                    if (!map.IsCoordWithinMap(targetCoords) || map.GetTile(targetCoords) == null)
                    {
                        throw new MapFacadeValidationException(
                            $"The terrain placement footprint extends outside the map at ({targetCoords.X}, {targetCoords.Y}).");
                    }
                }
            }
        }
    }

    private static bool IsTileSetPlaceable(TileSet tileSet)
    {
        return tileSet.AllowToPlace && tileSet.LoadedTileCount > 0 && tileSet.NonMarbleMadness < 0;
    }

    private static string GetEffectiveEditorCategory(GameObjectType gameObjectType)
    {
        string editorCategory = gameObjectType.EditorCategory;
        if (string.IsNullOrWhiteSpace(editorCategory) && gameObjectType is TechnoType technoType)
            editorCategory = technoType.Owner;

        return string.IsNullOrWhiteSpace(editorCategory) ? "Uncategorized" : editorCategory;
    }

    private static string NormalizeTechnoRTTI(string rtti)
    {
        if (string.IsNullOrWhiteSpace(rtti))
            return null;

        string normalizedRTTI = rtti.Trim();
        if (string.Equals(normalizedRTTI, nameof(RTTIType.Building), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedRTTI, "Structure", StringComparison.OrdinalIgnoreCase))
            return nameof(RTTIType.Building);
        if (string.Equals(normalizedRTTI, nameof(RTTIType.Unit), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedRTTI, "Vehicle", StringComparison.OrdinalIgnoreCase))
            return nameof(RTTIType.Unit);
        if (string.Equals(normalizedRTTI, nameof(RTTIType.Infantry), StringComparison.OrdinalIgnoreCase))
            return nameof(RTTIType.Infantry);
        if (string.Equals(normalizedRTTI, nameof(RTTIType.Aircraft), StringComparison.OrdinalIgnoreCase))
            return nameof(RTTIType.Aircraft);

        throw new MapFacadeValidationException($"RTTI '{rtti}' is not a techno kind. Use Building, Unit, Infantry, or Aircraft.");
    }

    private static bool ContainsIgnoringCase(string value, string searchValue)
    {
        return value?.IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
