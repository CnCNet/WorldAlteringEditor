using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.CCEngine;
using TSMapEditor.CCEngine.TileData;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
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
    public MapTechnoInfo(string rtti, int x, int y, string iniName, string owner, byte facing, int hp, string attachedTag, string attachedTagID) : base(rtti, x, y, iniName)
    {
        Owner = owner;
        Facing = facing;
        HP = hp;
        AttachedTag = attachedTag;
        AttachedTagID = attachedTagID;
    }

    public int HP { get; }
    public string AttachedTag { get; }
    public string AttachedTagID { get; }
    public string Owner { get; }
    public byte Facing { get; }
}

public class MapBuildingInfo : MapTechnoInfo
{
    public MapBuildingInfo(string rtti, int x, int y, string iniName, string owner, byte facing, int hp, string attachedTag, string attachedTagID,
        bool aiSellable, bool aiRebuildable, bool powered, bool aiRepairable, bool nominal, int spotlight, List<string> upgrades)
        : base(rtti, x, y, iniName, owner, facing, hp, attachedTag, attachedTagID)
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
    public MapFootInfo(string rtti, int x, int y, string iniName, string owner, byte facing, int hp, string attachedTag, string attachedTagID, string mission,
        bool onBridge, int veterancy, int group, bool autocreateNoRecruitable, bool autocreateYesRecruitable, int? subCell)
        : base(rtti, x, y, iniName, owner, facing, hp, attachedTag, attachedTagID)
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
    public CellInfo(int x, int y, string tileSetName, int tileIndex, int tileIndexInTileSet, int subTileIndex, int height,
        MapObjectInfo terrainObjectInfo, MapOverlayInfo overlayInfo, List<MapBuildingInfo> buildingInfos, List<MapFootInfo> footInfos)
    {
        X = x;
        Y = y;
        TileSetName = tileSetName;
        TileIndex = tileIndex;
        TileIndexInTileSet = tileIndexInTileSet;
        SubTileIndex = subTileIndex;
        Height = height;
        TerrainObjectInfo = terrainObjectInfo;
        OverlayInfo = overlayInfo;
        BuildingInfos = buildingInfos;
        FootInfos = footInfos;
    }

    public int X { get; }
    public int Y { get; }
    public string TileSetName { get; }
    public int TileIndex { get; }
    public int TileIndexInTileSet { get; }
    public int SubTileIndex { get; }
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
        var buildingInfos = mapTile.Structures.Select(s => new MapBuildingInfo(s.WhatAmI().ToString(), s.Position.X, s.Position.Y, s.ObjectType.ININame, s.Owner.ININame,
            s.Facing, s.HP, s.AttachedTag?.Name, s.AttachedTag?.ID, s.AISellable, s.AIRebuildable, s.Powered, s.AIRepairable, s.Nominal, (int)s.Spotlight,
            s.Upgrades.Select(upgrade => upgrade?.ININame).ToList())).ToList();
        var vehicleInfos = mapTile.Vehicles.Select(v => new MapFootInfo(v.WhatAmI().ToString(), v.Position.X, v.Position.Y, v.ObjectType.ININame, v.Owner.ININame,
            v.Facing, v.HP, v.AttachedTag?.Name, v.AttachedTag?.ID, v.Mission, v.High, v.Veterancy, v.Group, v.AutocreateNoRecruitable, v.AutocreateYesRecruitable, null));
        var infantryInfos = mapTile.Infantry.Where(i => i != null).Select(i => new MapFootInfo(i.WhatAmI().ToString(), i.Position.X, i.Position.Y, i.ObjectType.ININame, i.Owner.ININame,
            i.Facing, i.HP, i.AttachedTag?.Name, i.AttachedTag?.ID, i.Mission, i.High, i.Veterancy, i.Group, i.AutocreateNoRecruitable, i.AutocreateYesRecruitable, (int)i.SubCell));
        var aircraftInfos = mapTile.Aircraft.Select(a => new MapFootInfo(a.WhatAmI().ToString(), a.Position.X, a.Position.Y, a.ObjectType.ININame, a.Owner.ININame,
            a.Facing, a.HP, a.AttachedTag?.Name, a.AttachedTag?.ID, a.Mission, a.High, a.Veterancy, a.Group, a.AutocreateNoRecruitable, a.AutocreateYesRecruitable, null));

        return new CellInfo(mapTile.X, mapTile.Y, tileSet.SetName, mapTile.TileIndex, mapTile.TileIndex - tileSet.StartTileIndex,
            mapTile.SubTileIndex, mapTile.Level, terrainObjectInfo, overlayInfo, buildingInfos,
            vehicleInfos.Concat(infantryInfos).Concat(aircraftInfos).ToList());
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
        return new MapInfo(map.LoadedTheaterName, map.Size.X, map.Size.Y);
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

                returnValue.Add(CellInfo.FromMapCell(map.TheaterInstance, mapCell));
            }
        }

        return returnValue;
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
            new List<CellInfo> { CellInfo.FromMapCell(map.TheaterInstance, mapTile) });
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
            foundationCells.Select(cell => CellInfo.FromMapCell(map.TheaterInstance, cell)).ToList());
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
            new List<CellInfo> { CellInfo.FromMapCell(map.TheaterInstance, map.GetTile(cellCoords)) });
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
            new List<CellInfo> { CellInfo.FromMapCell(map.TheaterInstance, mapTile) });
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
            new List<CellInfo> { CellInfo.FromMapCell(map.TheaterInstance, map.GetTile(cellCoords)) });
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

        var brushSize = map.EditorConfig.BrushSizes.Find(bs => bs.Width == brushWidth && bs.Height == brushHeight);
        if (brushSize == null)
            throw new MapFacadeValidationException($"Brush size {brushWidth}x{brushHeight} is not configured in the editor.");

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

    public MapEditResult SetCellTerrain(int x, int y, int tileIndex, int subTileIndex)
    {
        var cellCoords = new Point2D(x, y);
        var mapTile = map.IsCoordWithinMap(cellCoords) ? map.GetTile(cellCoords) : null;
        if (mapTile == null)
            throw new MapFacadeValidationException($"Cell ({x}, {y}) is outside the map.");

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

        var mutation = new SetCellTerrainMutation(mutationTarget, cellCoords, tileIndex, (byte)subTileIndex);
        if (!mutation.ShouldPerform())
        {
            throw new MapFacadeValidationException(
                $"Cell ({x}, {y}) already uses absolute tile index {tileIndex} and sub-tile index {subTileIndex}.");
        }

        mutationManager.PerformMutation(mutation);

        return new MapEditResult(
            mutationManager.Revision,
            new List<CellInfo> { CellInfo.FromMapCell(map.TheaterInstance, mapTile) });
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

        if (string.IsNullOrWhiteSpace(upgradeName))
            throw new MapFacadeValidationException($"Building upgrade #{upgradeIndex + 1} cannot be empty.");

        if (upgradeIndex >= structure.ObjectType.Upgrades)
        {
            throw new MapFacadeValidationException(
                $"Building type '{structure.ObjectType.ININame}' does not support upgrade slot #{upgradeIndex + 1}.");
        }

        var upgrade = map.Rules.BuildingTypes.Find(bt => string.Equals(bt.ININame, upgradeName, StringComparison.OrdinalIgnoreCase));
        if (upgrade == null)
            throw new MapFacadeValidationException($"Building upgrade type '{upgradeName}' does not exist in the loaded rules.");

        if (!string.Equals(upgrade.PowersUpBuilding, structure.ObjectType.ININame, StringComparison.OrdinalIgnoreCase))
        {
            throw new MapFacadeValidationException(
                $"Building type '{upgrade.ININame}' is not a valid upgrade for '{structure.ObjectType.ININame}'.");
        }

        structure.Upgrades[upgradeIndex] = upgrade;
        return true;
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

    private static bool ContainsIgnoringCase(string value, string searchValue)
    {
        return value?.IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
