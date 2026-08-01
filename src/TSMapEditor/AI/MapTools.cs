using Microsoft.Xna.Framework;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Rampastring.Tools;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace TSMapEditor.AI;

[McpServerToolType]
public sealed class MapTools
{
    private const int MaxRegionDimension = 256;
    private const int MaxRegionCellCount = 10_000;

    public MapTools(MapFacade mapFacade, GameThreadDispatcher gameThreadDispatcher)
    {
        this.mapFacade = mapFacade;
        this.gameThreadDispatcher = gameThreadDispatcher;
    }

    private readonly MapFacade mapFacade;
    private readonly GameThreadDispatcher gameThreadDispatcher;

    [McpServerTool(Name = "get_map_info", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns basic information about the map currently open in the World-Altering Editor.")]
    public Task<MapInfo> GetMapInfo(CancellationToken cancellationToken)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetMapInfo)}");
        return gameThreadDispatcher.InvokeAsync(mapFacade.GetMapInfo, cancellationToken);
    }

    [McpServerTool(Name = "get_map_revision", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns the revision number of the map currently open in the World-Altering Editor.")]
    public Task<int> GetMapRevision(CancellationToken cancellationToken)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetMapRevision)}");
        return gameThreadDispatcher.InvokeAsync(mapFacade.GetMapRevision, cancellationToken);
    }

    [McpServerTool(Name = "get_terrain_types", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns terrain object types that are visible in the editor and valid for the current map's theater.")]
    public Task<List<MapObjectTypeInfo>> GetTerrainTypes(
        [Description("Optional case-insensitive filter matched against INI name, UI name, and editor category.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetTerrainTypes)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetTerrainTypes(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_overlay_types", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns regular overlay types that are visible in the editor and valid for the current map's theater, including placeable frame counts and connected-overlay memberships. frameCount counts placeable artwork only; higher raw SHP frames, conventionally the upper half, are engine-managed shadow data.")]
    public Task<List<MapOverlayTypeInfo>> GetOverlayTypes(
        [Description("Optional case-insensitive filter matched against INI name, UI name, editor category, and connected-overlay name.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetOverlayTypes)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetOverlayTypes(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_connected_overlay_types", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns WAE connected-overlay configurations valid for the current map's theater, including their connection masks and underlying overlay frames. Use place_connected_overlay for automatic connections, or place_overlay with this frame data for exact manual placement.")]
    public Task<List<MapConnectedOverlayTypeInfo>> GetConnectedOverlayTypes(
        [Description("Optional case-insensitive filter matched against configuration name, UI name, related configuration names, and underlying overlay INI names.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetConnectedOverlayTypes)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetConnectedOverlayTypes(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_building_types", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns building types that are visible in the editor and valid for the current map's theater, including foundation dimensions.")]
    public Task<List<MapBuildingTypeInfo>> GetBuildingTypes(
        [Description("Optional case-insensitive filter matched against INI name, UI name, and editor category.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetBuildingTypes)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetBuildingTypes(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_aircraft_types", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns aircraft types that are visible in the editor and valid for the current map's theater.")]
    public Task<List<MapObjectTypeInfo>> GetAircraftTypes(
        [Description("Optional case-insensitive filter matched against INI name, UI name, and editor category.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetAircraftTypes)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetAircraftTypes(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_infantry_types", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns infantry types that are visible in the editor and valid for the current map's theater.")]
    public Task<List<MapObjectTypeInfo>> GetInfantryTypes(
        [Description("Optional case-insensitive filter matched against INI name, UI name, and editor category.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetInfantryTypes)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetInfantryTypes(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_vehicle_types", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns vehicle types that are visible in the editor and valid for the current map's theater.")]
    public Task<List<MapObjectTypeInfo>> GetVehicleTypes(
        [Description("Optional case-insensitive filter matched against INI name, UI name, and editor category.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetVehicleTypes)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetVehicleTypes(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_houses", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns houses that can own player-controllable objects on the current map.")]
    public Task<List<MapHouseInfo>> GetHouses(
        [Description("Optional case-insensitive filter matched against house name, house type, and color.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetHouses)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetHouses(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_tile_sets", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns tile sets that are available for placement in the current map's theater.")]
    public Task<List<MapTileSetInfo>> GetTileSets(
        [Description("Optional case-insensitive filter matched against tile set name and UI name.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetTileSets)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetTileSets(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_technos", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns unique buildings, vehicles, infantry, and aircraft from the current map with stable object IDs for use with modify_technos.")]
    public async Task<MapTechnoQueryResult> GetTechnos(
        [Description("Optional kind filter: Building, Unit or Vehicle, Infantry, or Aircraft.")] string rtti = null,
        [Description("Optional case-insensitive partial INI type-name filter.")] string typeNameFilter = null,
        [Description("Optional case-insensitive partial owner-name filter.")] string ownerNameFilter = null,
        [Description("Optional X coordinate of a rectangular query area. Must be supplied together with y, width, and height.")] int? x = null,
        [Description("Optional Y coordinate of a rectangular query area. Must be supplied together with x, width, and height.")] int? y = null,
        [Description("Optional query-area width. Must be supplied together with x, y, and height.")] int? width = null,
        [Description("Optional query-area height. Must be supplied together with x, y, and width.")] int? height = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetTechnos)}");

        Rectangle? area = null;
        bool hasAnyAreaValue = x.HasValue || y.HasValue || width.HasValue || height.HasValue;
        if (hasAnyAreaValue)
        {
            if (!x.HasValue || !y.HasValue || !width.HasValue || !height.HasValue)
                throw new McpException("x, y, width, and height must all be supplied when filtering technos by area.");
            if (width.Value <= 0 || height.Value <= 0)
                throw new McpException("The techno query area width and height must both be greater than zero.");
            if (width.Value > MaxRegionDimension || height.Value > MaxRegionDimension || (long)width.Value * height.Value > MaxRegionCellCount)
                throw new McpException($"The techno query area is too large. Each dimension may be at most {MaxRegionDimension} cells and the total area may be at most {MaxRegionCellCount} cells.");

            area = new Rectangle(x.Value, y.Value, width.Value, height.Value);
        }

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.GetTechnos(rtti, typeNameFilter, ownerNameFilter, area),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "inspect_map_region", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns terrain, overlays, and placed map objects from a rectangular region of the open map.")]
    public Task<List<CellInfo>> InspectMapRegion(
        [Description("X coordinate of the region's top-left cell.")] int x,
        [Description("Y coordinate of the region's top-left cell.")] int y,
        [Description("Width of the region in cells.")] int width,
        [Description("Height of the region in cells.")] int height,
        CancellationToken cancellationToken)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(InspectMapRegion)}");

        if (width <= 0 || height <= 0)
            throw new McpException("The region width and height must both be greater than zero.");

        if (width > MaxRegionDimension || height > MaxRegionDimension || (long)width * height > MaxRegionCellCount)
            throw new McpException($"The requested region is too large. Each dimension may be at most {MaxRegionDimension} cells and the total area may be at most {MaxRegionCellCount} cells.");

        return gameThreadDispatcher.InvokeAsync(
            () => mapFacade.InspectRegion(new Rectangle(x, y, width, height)),
            cancellationToken);
    }

    [McpServerTool(Name = "modify_technos", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Atomically modifies properties of explicitly referenced technos. The entire batch is one undo entry and one revision bump.")]
    public async Task<MapEditResult> ModifyTechnos(
        [Description("One or more object ID references returned by get_technos or inspect_map_region.")] List<MapTechnoReference> technos,
        [Description("Properties to apply to every selected techno. Properties that do not apply to every selected kind cause the entire call to fail.")] MapTechnoModificationProperties properties,
        [Description("Optional map revision returned by get_technos. When supplied, modification fails if the map has changed; omit it to allow concurrent human edits.")] int? expectedRevision = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(ModifyTechnos)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.ModifyTechnos(technos, properties, expectedRevision),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "delete_objects", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Atomically deletes explicitly referenced technos and terrain objects. The entire batch is one undo entry and one revision bump.")]
    public async Task<MapEditResult> DeleteObjects(
        [Description("Optional techno object ID references returned by get_technos or inspect_map_region.")] List<MapTechnoReference> technos = null,
        [Description("Optional terrain object coordinate and INI-name references returned by inspect_map_region.")] List<MapTerrainObjectReference> terrainObjects = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(DeleteObjects)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.DeleteObjects(technos, terrainObjects),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "erase_overlay", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Erases all overlay from a rectangular map area. The operation is one undo entry and also updates neighboring Tiberium frames.")]
    public async Task<MapEditResult> EraseOverlay(
        [Description("X coordinate of the area's top-left cell.")] int x,
        [Description("Y coordinate of the area's top-left cell.")] int y,
        [Description("Area width in cells. Defaults to 1.")] int width = 1,
        [Description("Area height in cells. Defaults to 1.")] int height = 1,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(EraseOverlay)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.EraseOverlay(x, y, width, height),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_overlay", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Places a regular overlay in a rectangular map area, replacing existing overlay. Omit frameIndex to use frame 0 and let WAE automatically smooth Tiberium; specify a placeable artwork frame for exact manual placement. Raw SHP frames at or above the frameCount returned by get_overlay_types are engine-managed shadows and are rejected. The operation is one undo entry and one revision bump.")]
    public async Task<MapEditResult> PlaceOverlay(
        [Description("INI name of an overlay type returned by get_overlay_types.")] string overlayTypeName,
        [Description("X coordinate of the area's top-left cell.")] int x,
        [Description("Y coordinate of the area's top-left cell.")] int y,
        [Description("Area width in cells. Defaults to 1.")] int width = 1,
        [Description("Area height in cells. Defaults to 1.")] int height = 1,
        [Description("Optional zero-based placeable artwork frame index, which must be lower than frameCount from get_overlay_types. The upper raw SHP half contains engine-managed shadow frames and cannot be placed. Omit it for WAE's normal placement behavior and automatic Tiberium smoothing.")] int? frameIndex = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceOverlay)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.PlaceOverlay(overlayTypeName, x, y, width, height, frameIndex),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_connected_overlay", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Places a WAE connected-overlay configuration in a rectangular map area, replacing existing overlay and automatically selecting frames and reconnecting neighboring members. The operation is one undo entry and one revision bump.")]
    public async Task<MapEditResult> PlaceConnectedOverlay(
        [Description("Connected-overlay configuration name returned by get_connected_overlay_types.")] string connectedOverlayName,
        [Description("X coordinate of the area's top-left cell.")] int x,
        [Description("Y coordinate of the area's top-left cell.")] int y,
        [Description("Area width in cells. Defaults to 1.")] int width = 1,
        [Description("Area height in cells. Defaults to 1.")] int height = 1,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceConnectedOverlay)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.PlaceConnectedOverlay(connectedOverlayName, x, y, width, height),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_terrain_object", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Places one terrain object, such as a tree, on an empty map cell. The placement is added to the editor's undo history.")]
    public async Task<MapEditResult> PlaceTerrainObject(
        [Description("INI name of the terrain object type to place.")] string terrainTypeName,
        [Description("X coordinate of the destination cell.")] int x,
        [Description("Y coordinate of the destination cell.")] int y,
        CancellationToken cancellationToken)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceTerrainObject)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.PlaceTerrainObject(terrainTypeName, x, y),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_building", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Places a building with an explicit owner and optional initial properties at the given foundation origin. By default, placement fails if its foundation overlaps another building. The edit is added to undo history.")]
    public async Task<MapEditResult> PlaceBuilding(
        [Description("INI name of the building type returned by get_building_types.")] string buildingTypeName,
        [Description("INI name of the owner returned by get_houses.")] string ownerName,
        [Description("X coordinate of the building foundation origin.")] int x,
        [Description("Y coordinate of the building foundation origin.")] int y,
        [Description("Whether to allow the building foundation to overlap other buildings. Defaults to false.")] bool allowOverlap = false,
        [Description("Optional initial building properties. Omitted properties retain the editor's placement defaults.")] MapBuildingPlacementProperties properties = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceBuilding)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.PlaceBuilding(buildingTypeName, ownerName, x, y, allowOverlap, properties),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_aircraft", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Places aircraft with an explicit owner and optional initial properties at the given cell. By default, placement fails if the cell already contains aircraft. The edit is added to undo history.")]
    public async Task<MapEditResult> PlaceAircraft(
        [Description("INI name of the aircraft type returned by get_aircraft_types.")] string aircraftTypeName,
        [Description("INI name of the owner returned by get_houses.")] string ownerName,
        [Description("X coordinate of the destination cell.")] int x,
        [Description("Y coordinate of the destination cell.")] int y,
        [Description("Whether to allow the aircraft to overlap other aircraft. Defaults to false.")] bool allowOverlap = false,
        [Description("Optional initial aircraft properties. onBridge and subCell are not valid for aircraft. Omitted properties retain the editor's placement defaults.")] MapFootPlacementProperties properties = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceAircraft)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.PlaceAircraft(aircraftTypeName, ownerName, x, y, allowOverlap, properties),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_infantry", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Places infantry with an explicit owner and optional initial properties at the given cell. A specific usable subcell can be requested; otherwise the first free one is used. The edit is added to undo history.")]
    public async Task<MapEditResult> PlaceInfantry(
        [Description("INI name of the infantry type returned by get_infantry_types.")] string infantryTypeName,
        [Description("INI name of the owner returned by get_houses.")] string ownerName,
        [Description("X coordinate of the destination cell.")] int x,
        [Description("Y coordinate of the destination cell.")] int y,
        [Description("Optional initial infantry properties. Omitted properties retain the editor's placement defaults.")] MapFootPlacementProperties properties = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceInfantry)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.PlaceInfantry(infantryTypeName, ownerName, x, y, properties),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_vehicle", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Places a vehicle with an explicit owner and optional initial properties at the given cell. By default, placement fails if the cell already contains a vehicle. The edit is added to undo history.")]
    public async Task<MapEditResult> PlaceVehicle(
        [Description("INI name of the vehicle type returned by get_vehicle_types.")] string vehicleTypeName,
        [Description("INI name of the owner returned by get_houses.")] string ownerName,
        [Description("X coordinate of the destination cell.")] int x,
        [Description("Y coordinate of the destination cell.")] int y,
        [Description("Whether to allow the vehicle to overlap other vehicles. Defaults to false.")] bool allowOverlap = false,
        [Description("Optional initial vehicle properties. subCell is not valid for vehicles. Omitted properties retain the editor's placement defaults.")] MapFootPlacementProperties properties = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceVehicle)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.PlaceVehicle(vehicleTypeName, ownerName, x, y, allowOverlap, properties),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_terrain_tile", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Places a full terrain tile by tile set and tile-set-relative index. Supports configured brush sizes and optional AutoLAT. Existing terrain in the footprint may be replaced, and the edit is added to undo history.")]
    public async Task<MapEditResult> PlaceTerrainTile(
        [Description("Internal name of the tile set returned by get_tile_sets.")] string tileSetName,
        [Description("Zero-based tile index relative to the start of the tile set.")] int tileIndexInTileSet,
        [Description("X coordinate of the placement's top-left cell.")] int x,
        [Description("Y coordinate of the placement's top-left cell.")] int y,
        [Description("Width of the configured brush in repeated full tiles. Defaults to 1.")] int brushWidth = 1,
        [Description("Height of the configured brush in repeated full tiles. Defaults to 1.")] int brushHeight = 1,
        [Description("Whether to automatically create LAT transitions around the placed terrain. Defaults to true.")] bool autoLAT = true,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceTerrainTile)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.PlaceTerrainTile(tileSetName, tileIndexInTileSet, x, y, brushWidth, brushHeight, autoLAT),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "set_cell_terrain", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Directly sets the absolute tile index and sub-tile index of one cell. This is a low-level operation that does not apply a brush or AutoLAT, and it is added to undo history.")]
    public async Task<MapEditResult> SetCellTerrain(
        [Description("X coordinate of the cell.")] int x,
        [Description("Y coordinate of the cell.")] int y,
        [Description("Absolute tile index in the loaded theater.")] int tileIndex,
        [Description("Sub-tile index within the selected full tile.")] int subTileIndex,
        CancellationToken cancellationToken)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(SetCellTerrain)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.SetCellTerrain(x, y, tileIndex, subTileIndex),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }
}
