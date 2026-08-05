using Microsoft.Xna.Framework;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TSMapEditor.Rendering;

namespace TSMapEditor.AI;

[McpServerToolType]
public sealed class MapTools
{
    private const int MaxRegionDimension = 256;
    private const int MaxRegionCellCount = 10_000;
    private const int MaxScreenshotPixelCount = 8_000_000;
    private const int MaxWholeMapPreviewDimension = 4_096;
    private const int DefaultWholeMapPreviewDimension = 2_048;
    private const string MappingInstructionsFileName = "AIMappingInstructions.md";

    public MapTools(MapFacade mapFacade, GameThreadDispatcher gameThreadDispatcher, IMapScreenCropper mapScreenCropper)
    {
        this.mapFacade = mapFacade;
        this.gameThreadDispatcher = gameThreadDispatcher;
        this.mapScreenCropper = mapScreenCropper;
    }

    private readonly MapFacade mapFacade;
    private readonly GameThreadDispatcher gameThreadDispatcher;
    private readonly IMapScreenCropper mapScreenCropper;

    [McpServerTool(Name = "get_mapping_instructions", ReadOnly = true, OpenWorld = false)]
    [Description("Returns the active mod's AI mapping instructions as Markdown.")]
    public async Task<string> GetMappingInstructions(CancellationToken cancellationToken)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetMappingInstructions)}");

        string customPath = Path.Combine(Environment.CurrentDirectory, "Config", MappingInstructionsFileName);
        string defaultPath = Path.Combine(Environment.CurrentDirectory, "Config", "Default", MappingInstructionsFileName);
        string instructionsPath;

        if (File.Exists(customPath))
            instructionsPath = customPath;
        else if (File.Exists(defaultPath))
            instructionsPath = defaultPath;
        else
            throw new McpException("No mapping instruction file exists for the active mod.");

        try
        {
            return await File.ReadAllTextAsync(instructionsPath, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FileNotFoundException)
        {
            throw new McpException("No mapping instruction file exists for the active mod.");
        }
        catch (DirectoryNotFoundException)
        {
            throw new McpException("No mapping instruction file exists for the active mod.");
        }
        catch (Exception ex)
        {
            throw new McpException($"Failed to read the mapping instruction file for the active mod: {ex.Message}");
        }
    }

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

    [McpServerTool(Name = "get_mutation_history", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns the real editor undo and redo stacks in newest-first order, along with the current map revision. MCP mutations include stable IDs, tool names, and affected-cell summaries. Editor-driven mutations remain visible but have no metadata and cannot be undone or redone through MCP.")]
    public async Task<MapMutationHistoryInfo> GetMutationHistory(
        [Description("Maximum number of entries returned from each stack. Defaults to 50 and may be at most 1000.")] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetMutationHistory)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.GetMutationHistory(limit),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "undo_latest", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Undoes exactly the current top MCP mutation on the real editor undo stack. Both guards are required and must match get_mutation_history, preventing an interleaved editor or MCP edit from being undone accidentally. Editor-driven entries cannot be undone through MCP.")]
    public async Task<MapMutationOperationResult> UndoLatest(
        [Description("Exact current map revision returned by get_mutation_history.")] int expectedRevision,
        [Description("Mutation ID of the first undoHistory entry returned by get_mutation_history.")] long expectedMutationId,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(UndoLatest)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.UndoLatest(expectedRevision, expectedMutationId),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "redo_latest", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Redoes exactly the current top MCP mutation on the real editor redo stack. Both guards are required and must match get_mutation_history, preventing an interleaved editor or MCP action from being redone accidentally. Editor-driven entries cannot be redone through MCP.")]
    public async Task<MapMutationOperationResult> RedoLatest(
        [Description("Exact current map revision returned by get_mutation_history.")] int expectedRevision,
        [Description("Mutation ID of the first redoHistory entry returned by get_mutation_history.")] long expectedMutationId,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(RedoLatest)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.RedoLatest(expectedRevision, expectedMutationId),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
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

    [McpServerTool(Name = "get_terrain_object_collections", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns non-empty terrain object collections available in the current map's theater. Entries are returned in configured order and may repeat because duplicate entries increase their random placement weight.")]
    public Task<List<MapTerrainObjectCollectionInfo>> GetTerrainObjectCollections(
        [Description("Optional case-insensitive filter matched against collection names and entry INI/UI names.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetTerrainObjectCollections)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetTerrainObjectCollections(nameFilter), cancellationToken);
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

    [McpServerTool(Name = "get_overlay_collections", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns non-empty overlay collections available in the current map's theater, including every overlay type and configured frame. Entries are returned in configured order and may repeat because duplicate entries increase their random placement weight.")]
    public Task<List<MapOverlayCollectionInfo>> GetOverlayCollections(
        [Description("Optional case-insensitive filter matched against collection names and entry INI/UI names.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetOverlayCollections)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetOverlayCollections(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_connected_overlay_types", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns WAE connected-overlay configurations valid for the current map's theater, including their connection masks and underlying overlay frames. Use place_connected_overlay for automatic connections, or place_overlays_batch with this frame data for exact manual placement.")]
    public Task<List<MapConnectedOverlayTypeInfo>> GetConnectedOverlayTypes(
        [Description("Optional case-insensitive filter matched against configuration name, UI name, related configuration names, and underlying overlay INI names.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetConnectedOverlayTypes)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetConnectedOverlayTypes(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_connected_tile_types", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns WAE Connected Tiles configurations that are enabled and valid for the current map's theater, including whether each type supports ending pieces. Use draw_connected_tiles with an INI name from this result to lay out cliffs, shores, roads, rivers, and other configured connected terrain.")]
    public Task<List<MapConnectedTileTypeInfo>> GetConnectedTileTypes(
        [Description("Optional case-insensitive filter matched against INI name and display name.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetConnectedTileTypes)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetConnectedTileTypes(nameFilter), cancellationToken);
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

    [McpServerTool(Name = "get_waypoints", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns waypoints placed on the current map, ordered by identifier. Waypoints 0 through 7 are multiplayer starting locations. Waypoints are also included in inspect_map_region cell results.")]
    public async Task<List<MapWaypointInfo>> GetWaypoints(
        [Description("Optional exact waypoint identifier. Omit it to return every waypoint on the map.")] int? identifier = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetWaypoints)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(() => mapFacade.GetWaypoints(identifier), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "get_tile_sets", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns tile sets available for placement in the current map's theater. Each result explicitly lists both usable and unusable tiles with their absolute and tile-set-relative indices; tileCount includes entries whose graphics files are missing or unusable.")]
    public Task<List<MapTileSetInfo>> GetTileSets(
        [Description("Optional case-insensitive filter matched against tile set name and UI name.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetTileSets)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetTileSets(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_tile_details", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns deterministic base-graphics metadata for one absolute terrain tile index, including whether it is usable, its tile set and relative index, full-tile footprint, and every valid sub-tile's slot index, coordinate offset, and height. Missing-graphics tiles return hasUsableGraphics=false and an empty validSubTiles list rather than failing.")]
    public async Task<MapTerrainTileInfo> GetTileDetails(
        [Description("Absolute tile index returned by get_tile_sets, inspect_map_region, or another map tool.")] int absoluteTileIndex,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetTileDetails)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.GetTileDetails(absoluteTileIndex),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "get_terrain_generator_presets", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns built-in mod presets and saved user presets available to the Terrain Generator in the current theater. Results include human-readable names, exact stable preset IDs, source, usability, and group counts; use get_terrain_generator_preset for full contents.")]
    public async Task<List<MapTerrainGeneratorPresetSummary>> GetTerrainGeneratorPresets(
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetTerrainGeneratorPresets)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                mapFacade.GetTerrainGeneratorPresets,
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "get_terrain_generator_preset", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns the effective configuration of one Terrain Generator preset, including placement chances and the terrain objects, tile sets and relative indices, overlays and frames, and smudges used by each group. Also reports validation errors that would prevent generation.")]
    public async Task<MapTerrainGeneratorPresetInfo> GetTerrainGeneratorPreset(
        [Description("Exact stable preset ID returned by get_terrain_generator_presets.")] string presetId,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetTerrainGeneratorPreset)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.GetTerrainGeneratorPreset(presetId),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
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
    [Description("Returns terrain, overlays, waypoints, and placed map objects from a rectangular region of the open map. Each dimension may be at most 256 cells and the region may contain at most 10,000 cells.")]
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

    [McpServerTool(Name = "calculate_resource_field_value", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Calculates the total credit value, resource-cell count, and bounding rectangle of the contiguous resource field containing a given cell. Diagonally touching harvestable resource cells are considered part of the same field. Resource values use the same frame-index calculation as WAE's Calculate Credits cursor tool.")]
    public async Task<MapResourceFieldInfo> CalculateResourceFieldValue(
        [Description("X coordinate of a cell containing a harvestable resource.")] int x,
        [Description("Y coordinate of a cell containing a harvestable resource.")] int y,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(CalculateResourceFieldValue)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.CalculateResourceFieldValue(x, y),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "validate_map", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Runs WAE's standard map issue checks and scans for non-overlapping 10x10 underdetailed areas. An underdetailed area contains only clear terrain and has no terrain objects, overlays, smudges, technos, infantry, or aircraft.")]
    public Task<MapValidationResult> ValidateMap(CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(ValidateMap)}");
        return gameThreadDispatcher.InvokeAsync(mapFacade.ValidateMap, cancellationToken);
    }

    [McpServerTool(Name = "screenshot_map_region", ReadOnly = true, OpenWorld = false)]
    [Description("Renders the entire open map and returns a PNG screenshot of the axis-aligned pixel bounds for a rectangular region of cells. Each dimension may be at most 256 cells, the region may contain at most 10,000 cells, and the projected output may contain at most 8,000,000 pixels. In normal 3D mode, the image includes fixed vertical padding above those bounds for terrain at the maximum supported height. Because the map is isometric, the requested cells form a diamond within the returned rectangular image, whose corners can contain map content outside the requested cells. Regions may partially cross the map boundary, with pixels outside the map's render bounds left transparent, but must overlap the map.")]
    public async Task<ImageContentBlock> ScreenshotMapRegion(
        [Description("X coordinate of the region's top-left cell.")] int x,
        [Description("Y coordinate of the region's top-left cell.")] int y,
        [Description("Width of the region in cells. Must be at most 256; width multiplied by height must be at most 10,000.")] int width,
        [Description("Height of the region in cells. Must be at most 256; width multiplied by height must be at most 10,000.")] int height,
        CancellationToken cancellationToken)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(ScreenshotMapRegion)}");

        if (width <= 0 || height <= 0)
            throw new McpException("The region width and height must both be greater than zero.");

        if (width > MaxRegionDimension || height > MaxRegionDimension || (long)width * height > MaxRegionCellCount)
            throw new McpException($"The requested region is too large. Each dimension may be at most {MaxRegionDimension} cells and the total area may be at most {MaxRegionCellCount} cells.");

        long projectedPixelWidth = ((long)width + height - 2L) * (Constants.CellSizeX / 2L) + Constants.CellSizeX;
        long projectedPixelHeight = ((long)width + height - 2L) * (Constants.CellSizeY / 2L) + Constants.CellSizeY + Constants.MapYBaseline;
        if (projectedPixelWidth * projectedPixelHeight > MaxScreenshotPixelCount)
        {
            throw new McpException(
                $"The requested screenshot would be too large ({projectedPixelWidth}x{projectedPixelHeight} pixels). " +
                $"The projected image may contain at most {MaxScreenshotPixelCount} pixels.");
        }

        if (!mapScreenCropper.TryRequestScreenCrop(
            new Rectangle(x, y, width, height),
            cancellationToken,
            out Task<byte[]> screenCropTask))
        {
            throw new McpException("The renderer is already busy with a previous screen-crop request.");
        }

        try
        {
            byte[] pngData = await screenCropTask.ConfigureAwait(false);
            return ImageContentBlock.FromBytes(pngData, "image/png");
        }
        catch (MapScreenCropException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "screenshot_whole_map", ReadOnly = true, OpenWorld = false)]
    [Description("Renders the entire open map, scales the existing full-map render directly into a bounded preview, and returns it as PNG. The aspect ratio is preserved, neither requested dimension may exceed 4,096 pixels, and their product may not exceed 8,000,000 pixels. Defaults to a 2,048x2,048 envelope. This avoids allocating or encoding an additional full-resolution screenshot.")]
    public async Task<ImageContentBlock> ScreenshotWholeMap(
        [Description("Maximum preview width in pixels. Defaults to 2,048 and may be at most 4,096.")] int maxWidth = DefaultWholeMapPreviewDimension,
        [Description("Maximum preview height in pixels. Defaults to 2,048 and may be at most 4,096.")] int maxHeight = DefaultWholeMapPreviewDimension,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(ScreenshotWholeMap)}");

        if (maxWidth <= 0 || maxHeight <= 0)
            throw new McpException("The maximum preview width and height must both be greater than zero.");
        if (maxWidth > MaxWholeMapPreviewDimension || maxHeight > MaxWholeMapPreviewDimension)
        {
            throw new McpException(
                $"The maximum preview width and height may each be at most {MaxWholeMapPreviewDimension} pixels.");
        }
        if ((long)maxWidth * maxHeight > MaxScreenshotPixelCount)
        {
            throw new McpException(
                $"The maximum preview dimensions may contain at most {MaxScreenshotPixelCount} pixels.");
        }

        if (!mapScreenCropper.TryRequestWholeMapPreview(maxWidth, maxHeight, cancellationToken, out Task<byte[]> previewTask))
            throw new McpException("The renderer is already busy with a previous screenshot request.");

        try
        {
            byte[] pngData = await previewTask.ConfigureAwait(false);
            return ImageContentBlock.FromBytes(pngData, "image/png");
        }
        catch (MapScreenCropException ex)
        {
            throw new McpException(ex.Message);
        }
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

    [McpServerTool(Name = "place_overlays_batch", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Atomically places explicitly selected overlays on individual cells using a 1x1 brush per placement, replacing existing overlay. Omit a placement's frameIndex to use frame 0 and automatically smooth neighboring resources; specify a placeable artwork frame for exact manual placement. Manually framed target cells remain exact during this batch. Raw SHP shadow frames are rejected. The operation is one undo entry and one revision bump.")]
    public async Task<MapEditResult> PlaceOverlaysBatch(
        [Description("One or more overlay types, optional frames, and destination cells. Coordinates must be distinct. At most 10,000 placements are supported.")] List<MapOverlayPlacement> placements,
        [Description("Optional map revision returned by get_map_revision or another map tool. When supplied, placement fails if the map has changed; omit it to allow concurrent human edits.")] int? expectedRevision = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceOverlaysBatch)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.PlaceOverlaysBatch(placements, expectedRevision),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_overlay_collection_batch", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Atomically places random entries from one editor overlay collection on multiple individual cells using a 1x1 brush per cell, replacing existing overlay. This preserves WAE's collection randomizer, impassable-resource avoidance, and resource smoothing behavior. Coordinates must be distinct. The operation is one undo entry and one revision bump.")]
    public async Task<MapEditResult> PlaceOverlayCollectionBatch(
        [Description("Configuration name of an overlay collection returned by get_overlay_collections.")] string collectionName,
        [Description("One or more distinct destination map-cell coordinates. At most 10,000 entries are supported.")] List<MapCellCoordinate> cells,
        [Description("Optional map revision returned by get_map_revision or another map tool. When supplied, placement fails if the map has changed; omit it to allow concurrent human edits.")] int? expectedRevision = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceOverlayCollectionBatch)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.PlaceOverlayCollectionBatch(collectionName, cells, expectedRevision),
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

    public Task<MapEditResult> DrawConnectedTiles(
        string connectedTileTypeName,
        List<MapConnectedTilePathVertex> path,
        string side,
        int randomSeed,
        int extraHeight,
        CancellationToken cancellationToken)
    {
        return DrawConnectedTiles(connectedTileTypeName, path, side, randomSeed, extraHeight,
            useEndPieces: false, closed: false, cancellationToken: cancellationToken);
    }

    [McpServerTool(Name = "draw_connected_tiles", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Uses WAE's Connected Tiles pathfinder to draw an open or closed connected terrain formation through map-cell vertices. The operation can optionally cap open paths with configured ending pieces. It can overwrite terrain and update cell heights, and is one undo entry and one revision bump.")]
    public async Task<MapEditResult> DrawConnectedTiles(
        [Description("INI name of a Connected Tiles configuration returned by get_connected_tile_types.")] string connectedTileTypeName,
        [Description("Ordered polyline vertices for the connected terrain path. Each consecutive pair defines one segment. Open paths require at least two vertices; closed paths require at least three distinct vertices. At most 256 vertices are supported.")] List<MapConnectedTilePathVertex> path,
        [Description("Starting side of the connected terrain: Front or Back. Front-only types require Front.")] string side = "Front",
        [Description("Seed used to select and score tile variants. Change it to request a different pattern while keeping the same path. Defaults to 0.")] int randomSeed = 0,
        [Description("Non-negative height offset added to the first vertex's current level before the connected tiles' own height offsets are applied. Defaults to 0. Must be 0 if the active mod has flat maps.")] int extraHeight = 0,
        [Description("Whether to cap both ends of an open path with configured one-connection-point ending pieces. The selected type must support ending pieces. Ignored for closed paths. Defaults to false.")] bool useEndPieces = false,
        [Description("Whether to connect the last vertex back to the first. Closed paths require at least three distinct vertices and never use ending pieces. Defaults to false.")] bool closed = false,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(DrawConnectedTiles)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.DrawConnectedTiles(connectedTileTypeName, path, side, randomSeed, extraHeight, useEndPieces, closed),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_waypoint", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Places a uniquely numbered waypoint on a map cell. Waypoints 0 through 7 are multiplayer starting locations. Multiple differently numbered waypoints may share a cell. The operation is one undo entry and one revision bump.")]
    public async Task<MapEditResult> PlaceWaypoint(
        [Description("Unique waypoint identifier. Valid values are determined by the editor's configured waypoint limit; 0 through 7 denote multiplayer starting locations.")] int identifier,
        [Description("X coordinate of the destination cell.")] int x,
        [Description("Y coordinate of the destination cell.")] int y,
        [Description("Optional editor-only display color. Supported values include Teal, Green, Dark Green, Lime Green, Yellow, Orange, Red, Blood Red, Pink, Cherry, Purple, Sky Blue, Blue, Brown, and Metalic.")] string editorColor = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceWaypoint)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.PlaceWaypoint(identifier, x, y, editorColor),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_terrain_objects_batch", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Atomically places multiple explicitly selected terrain object types on empty cells. Every placement is validated before any object is added; an invalid, duplicate, or occupied cell rejects the whole batch. The operation is one undo entry and one revision bump.")]
    public async Task<MapEditResult> PlaceTerrainObjectsBatch(
        [Description("One or more terrain object types and destination cells. At most 10,000 placements are supported.")] List<MapTerrainObjectPlacement> placements,
        [Description("Optional map revision returned by get_map_revision or another map tool. When supplied, placement fails if the map has changed; omit it to allow concurrent human edits.")] int? expectedRevision = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceTerrainObjectsBatch)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.PlaceTerrainObjectsBatch(placements, expectedRevision),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_terrain_object_collection_batch", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Atomically places random entries from one editor terrain object collection on multiple explicit empty cells. Every coordinate is validated before placement; an invalid, duplicate, or occupied cell rejects the whole batch. The operation is one undo entry and one revision bump.")]
    public async Task<MapEditResult> PlaceTerrainObjectCollectionBatch(
        [Description("Configuration name of a terrain object collection returned by get_terrain_object_collections.")] string collectionName,
        [Description("One or more distinct empty map-cell coordinates. At most 10,000 entries are supported.")] List<MapCellCoordinate> cells,
        [Description("Optional map revision returned by get_map_revision or another map tool. When supplied, placement fails if the map has changed; omit it to allow concurrent human edits.")] int? expectedRevision = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceTerrainObjectCollectionBatch)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.PlaceTerrainObjectCollectionBatch(collectionName, cells, expectedRevision),
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
        [Description("Zero-based relative index from a get_tile_sets tilesWithUsableGraphics entry.")] int tileIndexInTileSet,
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

    [McpServerTool(Name = "generate_terrain", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Runs the editor's Terrain Generator over every valid map cell inside a rectangular coordinate area using an existing preset. The generator randomly attempts the preset's terrain objects, full terrain tiles, overlays, and smudges while preserving its normal placement restrictions. Cells outside the map diamond are clipped from the rectangle. The operation is one undo entry and one revision bump and returns compact placement counts plus a potential affected-cell summary.")]
    public async Task<MapTerrainGenerationResult> GenerateTerrain(
        [Description("Exact stable preset ID returned by get_terrain_generator_presets.")] string presetId,
        [Description("X coordinate of the rectangular area's top-left cell.")] int x,
        [Description("Y coordinate of the rectangular area's top-left cell.")] int y,
        [Description("Rectangle width in logical cells. Each dimension may be at most 256 and total rectangular area at most 10,000 cells.")] int width,
        [Description("Rectangle height in logical cells. Each dimension may be at most 256 and total rectangular area at most 10,000 cells.")] int height,
        [Description("Whether to apply automatic LAT transitions after generation. Defaults to true and is captured for consistent undo and redo behavior.")] bool autoLAT = true,
        [Description("Optional map revision returned by get_map_revision or another map tool. When supplied, generation fails if the map has changed; omit it to allow concurrent human edits.")] int? expectedRevision = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GenerateTerrain)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.GenerateTerrain(presetId, x, y, width, height, autoLAT, expectedRevision),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "set_cells_terrain", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Directly sets the same absolute tile index and sub-tile index on one or more map cells. Duplicate coordinates and cells that already have the requested terrain are ignored. The batch is one atomic undo entry and one revision bump when any cells change. This low-level operation does not apply a brush or AutoLAT.")]
    public async Task<MapEditResult> SetCellsTerrain(
        [Description("One or more map-cell coordinates. At most 10,000 entries are supported.")] List<MapCellCoordinate> cells,
        [Description("Absolute tile index from a get_tile_sets tilesWithUsableGraphics entry or get_tile_details.")] int tileIndex,
        [Description("Sub-tile index from get_tile_details.validSubTiles for the selected full tile.")] int subTileIndex,
        [Description("Optional map revision returned by get_map_revision or another map tool. When supplied, the operation fails if the map has changed; omit it to allow concurrent human edits.")] int? expectedRevision = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(SetCellsTerrain)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.SetCellsTerrain(cells, tileIndex, subTileIndex, expectedRevision),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }
}
