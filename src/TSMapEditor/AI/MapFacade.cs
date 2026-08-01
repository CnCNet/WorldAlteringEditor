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
        var buildingInfos = mapTile.Structures.Select(s => new MapBuildingInfo(s.WhatAmI().ToString(), s.Position.X, s.Position.Y, s.ObjectType.ININame, s.Owner.ININame, s.Facing, s.HP, s.AttachedTag?.Name, s.Powered, s.AIRepairable)).ToList();
        var vehicleInfos = mapTile.Vehicles.Select(v => new MapFootInfo(v.WhatAmI().ToString(), v.Position.X, v.Position.Y, v.ObjectType.ININame, v.Owner.ININame, v.Facing, v.HP, v.AttachedTag?.Name, v.Mission, v.High, v.Veterancy, v.Group, v.AutocreateNoRecruitable, v.AutocreateYesRecruitable));
        var infantryInfos = mapTile.Infantry.Where(i => i != null).Select(i => new MapFootInfo(i.WhatAmI().ToString(), i.Position.X, i.Position.Y, i.ObjectType.ININame, i.Owner.ININame, i.Facing, i.HP, i.AttachedTag?.Name, i.Mission, i.High, i.Veterancy, i.Group, i.AutocreateNoRecruitable, i.AutocreateYesRecruitable));
        var aircraftInfos = mapTile.Aircraft.Select(a => new MapFootInfo(a.WhatAmI().ToString(), a.Position.X, a.Position.Y, a.ObjectType.ININame, a.Owner.ININame, a.Facing, a.HP, a.AttachedTag?.Name, a.Mission, a.High, a.Veterancy, a.Group, a.AutocreateNoRecruitable, a.AutocreateYesRecruitable));

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

        var terrainType = map.Rules.TerrainTypes.Find(
            tt => string.Equals(tt.ININame, terrainTypeName, StringComparison.OrdinalIgnoreCase));
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
            throw new MapFacadeValidationException(
                $"Tile {tileIndexInTileSet} from tile set '{tileSet.SetName}' has no usable tile graphics.");
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
            throw new MapFacadeValidationException(
                $"Tile {tileIndexInTileSet} from tile set '{tileSet.SetName}' cannot be placed at ({x}, {y}).");
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

    private static bool ContainsIgnoringCase(string value, string searchValue)
    {
        return value?.IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
