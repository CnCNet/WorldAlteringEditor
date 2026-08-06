using MapEditorLibrary;
using MapEditorMCP.Infos;
using Microsoft.Xna.Framework;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Rampastring.Tools;
using System.ComponentModel;

namespace MapEditorMCP;

[McpServerToolType]
internal sealed class MapTools
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
    [Description("Returns basic information about the current map.")]
    public Task<MapInfo> GetMapInfo(CancellationToken cancellationToken)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetMapInfo)}");
        return gameThreadDispatcher.InvokeAsync(mapFacade.GetMapInfo, cancellationToken);
    }

    [McpServerTool(Name = "get_map_revision", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns the current map revision.")]
    public Task<int> GetMapRevision(CancellationToken cancellationToken)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetMapRevision)}");
        return gameThreadDispatcher.InvokeAsync(mapFacade.GetMapRevision, cancellationToken);
    }

    [McpServerTool(Name = "get_mutation_history", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns undo and redo history in newest-first order with the current map revision. Entries created through MCP include stable IDs, " +
        "tool names, and affected-cell summaries; only those entries can be undone or redone through MCP.")]
    public async Task<MapMutationHistoryInfo> GetMutationHistory(
        [Description("Maximum number of entries returned from each stack. Defaults to 50 and may be at most 1000.")] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetMutationHistory)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(() => mapFacade.GetMutationHistory(limit), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "undo_latest", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Undoes the latest MCP-created mutation when both guards match get_mutation_history. Non-MCP entries cannot be undone through this API.")]
    public async Task<MapMutationOperationResult> UndoLatest(
        [Description("Exact current map revision returned by get_mutation_history.")] int expectedRevision,
        [Description("Mutation ID of the first undoHistory entry returned by get_mutation_history.")] long expectedMutationId,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(UndoLatest)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(() => mapFacade.UndoLatest(expectedRevision, expectedMutationId), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "redo_latest", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Redoes the latest MCP-created mutation when both guards match get_mutation_history. Non-MCP entries cannot be redone through this API.")]
    public async Task<MapMutationOperationResult> RedoLatest(
        [Description("Exact current map revision returned by get_mutation_history.")] int expectedRevision,
        [Description("Mutation ID of the first redoHistory entry returned by get_mutation_history.")] long expectedMutationId,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(RedoLatest)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(() => mapFacade.RedoLatest(expectedRevision, expectedMutationId), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "get_terrain_types", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns terrain object types available in the current map's theater.")]
    public Task<List<MapObjectTypeInfo>> GetTerrainTypes(
        [Description("Optional case-insensitive filter matched against INI name, UI name, and category.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetTerrainTypes)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetTerrainTypes(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_terrain_object_collections", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns non-empty terrain object collections for the current theater. Duplicate entries increase random selection weight.")]
    public Task<List<TerrainObjectCollectionInfo>> GetTerrainObjectCollections(
        [Description("Optional case-insensitive filter matched against collection names and entry INI/UI names.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetTerrainObjectCollections)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetTerrainObjectCollections(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_overlay_types", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns regular overlay types for the current theater, including connected-overlay memberships. frameCount includes only placeable frames.")]
    public Task<List<OverlayTypeInfo>> GetOverlayTypes(
        [Description("Optional case-insensitive filter matched against INI name, UI name, category, and connected-overlay name.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetOverlayTypes)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetOverlayTypes(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_overlay_collections", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns non-empty overlay collections for the current theater. Duplicate entries increase random selection weight.")]
    public Task<List<OverlayCollectionInfo>> GetOverlayCollections(
        [Description("Optional case-insensitive filter matched against collection names and entry INI/UI names.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetOverlayCollections)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetOverlayCollections(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_connected_overlay_types", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns connected-overlay types for the current theater, including connection masks and overlay frames. Use place_connected_overlay " +
        "for automatic connections or place_overlays_batch for exact frame placement.")]
    public Task<List<ConnectedOverlayTypeInfo>> GetConnectedOverlayTypes(
        [Description("Optional filter matched against names, related types, and underlying overlay INI names.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetConnectedOverlayTypes)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetConnectedOverlayTypes(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_connected_tile_types", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns connected-tile types for the current theater, including ending-piece support.")]
    public Task<List<ConnectedTileTypeInfo>> GetConnectedTileTypes(
        [Description("Optional case-insensitive filter matched against INI name and display name.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetConnectedTileTypes)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetConnectedTileTypes(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_building_types", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns building types available in the current map's theater, including foundation dimensions.")]
    public Task<List<MapBuildingTypeInfo>> GetBuildingTypes(
        [Description("Optional case-insensitive filter matched against INI name, UI name, and category.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetBuildingTypes)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetBuildingTypes(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_aircraft_types", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns aircraft types available in the current map's theater.")]
    public Task<List<MapObjectTypeInfo>> GetAircraftTypes(
        [Description("Optional case-insensitive filter matched against INI name, UI name, and category.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetAircraftTypes)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetAircraftTypes(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_infantry_types", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns infantry types available in the current map's theater.")]
    public Task<List<MapObjectTypeInfo>> GetInfantryTypes(
        [Description("Optional case-insensitive filter matched against INI name, UI name, and category.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetInfantryTypes)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetInfantryTypes(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_vehicle_types", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns vehicle types available in the current map's theater.")]
    public Task<List<MapObjectTypeInfo>> GetVehicleTypes(
        [Description("Optional case-insensitive filter matched against INI name, UI name, and category.")] string nameFilter = null,
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
    [Description("Returns waypoints ordered by identifier. Waypoints 0 through 7 are multiplayer starting locations.")]
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
    [Description("Returns tile sets for the current theater with absolute and tile-set-relative indices. tileCount includes unusable entries.")]
    public Task<List<MapTileSetInfo>> GetTileSets(
        [Description("Optional case-insensitive filter matched against tile set name and UI name.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetTileSets)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetTileSets(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "get_tile_details", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns metadata for one absolute terrain tile index, including usability, tile set, footprint, and valid sub-tiles.")]
    public async Task<TerrainTileInfo> GetTileDetails(
        [Description("Absolute tile index returned by get_tile_sets, inspect_map_region, or another map tool.")] int absoluteTileIndex,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetTileDetails)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(() => mapFacade.GetTileDetails(absoluteTileIndex), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "get_terrain_generator_presets", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns built-in and user terrain-generation presets for the current theater. Results include IDs, source, usability, and group counts; " +
        "use get_terrain_generator_preset for full contents.")]
    public async Task<List<TerrainGeneratorPresetSummary>> GetTerrainGeneratorPresets(
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetTerrainGeneratorPresets)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(mapFacade.GetTerrainGeneratorPresets, cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "get_terrain_generator_preset", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns one terrain-generation preset's placement chances and grouped contents, plus validation errors that prevent its use.")]
    public async Task<TerrainGeneratorPresetInfo> GetTerrainGeneratorPreset(
        [Description("Exact stable preset ID returned by get_terrain_generator_presets.")] string presetId,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetTerrainGeneratorPreset)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(() => mapFacade.GetTerrainGeneratorPreset(presetId), cancellationToken);
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
                throw new McpException($"The techno query area is too large. Each dimension may be at most {MaxRegionDimension} cells " +
                    $"and the total area may be at most {MaxRegionCellCount} cells.");

            area = new Rectangle(x.Value, y.Value, width.Value, height.Value);
        }

        try
        {
            return await gameThreadDispatcher.InvokeAsync(() => mapFacade.GetTechnos(rtti, typeNameFilter, ownerNameFilter, area), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "inspect_map_region", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns terrain, overlays, waypoints, and placed objects from a rectangular map region.")]
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
            throw new McpException($"The requested region is too large. Each dimension may be at most {MaxRegionDimension} cells " +
                $"and the total area may be at most {MaxRegionCellCount} cells.");

        return gameThreadDispatcher.InvokeAsync(() => mapFacade.InspectRegion(new Rectangle(x, y, width, height)), cancellationToken);
    }

    [McpServerTool(Name = "calculate_resource_field_value", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Calculates the total credit value, cell count, and bounding rectangle of the contiguous resource field containing a given cell. " +
        "Diagonally touching harvestable resource cells belong to the same field.")]
    public async Task<MapResourceFieldInfo> CalculateResourceFieldValue(
        [Description("X coordinate of a cell containing a harvestable resource.")] int x,
        [Description("Y coordinate of a cell containing a harvestable resource.")] int y,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(CalculateResourceFieldValue)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(() => mapFacade.CalculateResourceFieldValue(x, y), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "validate_map", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Checks for map issues and non-overlapping 10x10 areas containing only clear terrain and no objects, overlays, or smudges.")]
    public Task<MapValidationResult> ValidateMap(CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(ValidateMap)}");
        return gameThreadDispatcher.InvokeAsync(mapFacade.ValidateMap, cancellationToken);
    }

    [McpServerTool(Name = "screenshot_map_region", ReadOnly = true, OpenWorld = false)]
    [Description("Returns a PNG of a rectangular cell region. Because the map is isometric, the cells form a diamond within the image " +
        "and its corners may contain nearby map content. Regions may cross the map boundary but must overlap the map; out-of-bounds pixels are transparent.")]
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
            throw new McpException($"The requested region is too large. Each dimension may be at most {MaxRegionDimension} cells " +
                $"and the total area may be at most {MaxRegionCellCount} cells.");

        long projectedPixelWidth = ((long)width + height - 2L) * (Constants.CellSizeX / 2L) + Constants.CellSizeX;
        long projectedPixelHeight = ((long)width + height - 2L) * (Constants.CellSizeY / 2L) + Constants.CellSizeY + Constants.MapYBaseline;
        if (projectedPixelWidth * projectedPixelHeight > MaxScreenshotPixelCount)
        {
            throw new McpException(
                $"The requested screenshot would be too large ({projectedPixelWidth}x{projectedPixelHeight} pixels). " +
                $"The projected image may contain at most {MaxScreenshotPixelCount} pixels.");
        }

        if (!mapScreenCropper.TryRequestScreenCrop(new Rectangle(x, y, width, height), cancellationToken, out Task<byte[]> screenCropTask))
        {
            throw new McpException("Another screenshot request is already in progress.");
        }

        try
        {
            byte[] pngData = await screenCropTask.ConfigureAwait(false);
            return ImageContentBlock.FromBytes(pngData, "image/png");
        }
        catch (InvalidOperationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "screenshot_whole_map", ReadOnly = true, OpenWorld = false)]
    [Description("Returns an aspect-ratio-preserving PNG preview of the whole map within the requested dimensions. Defaults to a 2,048x2,048 envelope.")]
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
            throw new McpException("Another screenshot request is already in progress.");

        try
        {
            byte[] pngData = await previewTask.ConfigureAwait(false);
            return ImageContentBlock.FromBytes(pngData, "image/png");
        }
        catch (InvalidOperationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "modify_technos", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Atomically modifies properties of explicitly referenced technos. The entire batch is one undo entry and one revision bump.")]
    public async Task<MCPMapEditResult> ModifyTechnos(
        [Description("One or more object ID references returned by get_technos or inspect_map_region.")] List<MapTechnoReference> technos,
        [Description("Properties applied to every selected techno; incompatible properties reject the batch.")] MapTechnoModificationProperties properties,
        [Description("Optional get_technos revision. When supplied, rejects the call if the map changed.")] int? expectedRevision = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(ModifyTechnos)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(() => mapFacade.ModifyTechnos(technos, properties, expectedRevision), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "delete_objects", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Atomically deletes explicitly referenced technos and terrain objects. The entire batch is one undo entry and one revision bump.")]
    public async Task<MCPMapEditResult> DeleteObjects(
        [Description("Optional techno object ID references returned by get_technos or inspect_map_region.")] List<MapTechnoReference> technos = null,
        [Description("Optional terrain object references returned by inspect_map_region.")] List<MapTerrainObjectReference> terrainObjects = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(DeleteObjects)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(() => mapFacade.DeleteObjects(technos, terrainObjects), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "erase_overlay", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Erases all overlay from a rectangular map area. The operation is one undo entry and also updates neighboring Tiberium frames.")]
    public async Task<MCPMapEditResult> EraseOverlay(
        [Description("X coordinate of the area's top-left cell.")] int x,
        [Description("Y coordinate of the area's top-left cell.")] int y,
        [Description("Area width in cells. Defaults to 1.")] int width = 1,
        [Description("Area height in cells. Defaults to 1.")] int height = 1,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(EraseOverlay)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(() => mapFacade.EraseOverlay(x, y, width, height), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_overlays_batch", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Atomically places overlays on individual cells, replacing existing overlay. Omit frameIndex to use frame 0 and smooth " +
        "neighboring resources; specify a placeable frame for exact placement. Explicitly framed target cells remain unchanged by smoothing.")]
    public async Task<MCPMapEditResult> PlaceOverlaysBatch(
        [Description("Overlay types, optional frames, and distinct destination cells; at most 10,000.")] List<MapOverlayPlacement> placements,
        [Description("Optional map revision. When supplied, rejects the call if the map changed.")] int? expectedRevision = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceOverlaysBatch)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(() => mapFacade.PlaceOverlaysBatch(placements, expectedRevision), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_overlay_collection_batch", ReadOnly = false, Destructive = true,
        Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Places weighted random entries from one overlay collection on distinct cells. Resource placement avoids impassable terrain " +
        "and smooths neighboring resources.")]
    public async Task<MCPMapEditResult> PlaceOverlayCollectionBatch(
        [Description("Configuration name of an overlay collection returned by get_overlay_collections.")] string collectionName,
        [Description("One or more distinct destination map-cell coordinates. At most 10,000 entries are supported.")] List<MapCellCoordinate> cells,
        [Description("Optional map revision. When supplied, rejects the call if the map changed.")] int? expectedRevision = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceOverlayCollectionBatch)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(() => mapFacade.PlaceOverlayCollectionBatch(collectionName, cells, expectedRevision),
                cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_connected_overlay", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Places a connected-overlay type in a rectangular area, replacing existing overlay, selecting frames, and reconnecting neighboring members.")]
    public async Task<MCPMapEditResult> PlaceConnectedOverlay(
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
            return await gameThreadDispatcher.InvokeAsync(() => mapFacade.PlaceConnectedOverlay(connectedOverlayName, x, y, width, height), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    public Task<MCPMapEditResult> DrawConnectedTiles(
        string connectedTileTypeName,
        List<ConnectedTilePathVertex> path,
        string side,
        int randomSeed,
        int extraHeight,
        CancellationToken cancellationToken)
    {
        return DrawConnectedTiles(connectedTileTypeName, path, side, randomSeed, extraHeight,
            useEndPieces: false, closed: false, cancellationToken: cancellationToken);
    }

    [McpServerTool(Name = "draw_connected_tiles", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Draws an open or closed connected-terrain formation through map-cell vertices. May overwrite terrain and change cell heights.")]
    public async Task<MCPMapEditResult> DrawConnectedTiles(
        [Description("INI name of a Connected Tiles configuration returned by get_connected_tile_types.")] string connectedTileTypeName,
        [Description("Ordered path vertices; open paths require at least two, closed paths at least three distinct vertices. Maximum 256.")]
        List<ConnectedTilePathVertex> path,
        [Description("Starting side of the connected terrain: Front or Back. Front-only types require Front.")] string side = "Front",
        [Description("Seed used to select tile variants. Change it for a different pattern on the same path.")] int randomSeed = 0,
        [Description("Non-negative starting-height offset. Must be 0 for flat maps.")] int extraHeight = 0,
        [Description("Whether to cap an open path with configured ending pieces. Ignored for closed paths.")] bool useEndPieces = false,
        [Description("Whether to connect the last vertex to the first. Closed paths never use ending pieces.")] bool closed = false,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(DrawConnectedTiles)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.DrawConnectedTiles(connectedTileTypeName, path, side, randomSeed, extraHeight, useEndPieces, closed), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_waypoint", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Places a uniquely numbered waypoint. Waypoints 0 through 7 are multiplayer starting locations; different waypoints may share a cell.")]
    public async Task<MCPMapEditResult> PlaceWaypoint(
        [Description("Unique waypoint identifier. Values 0 through 7 denote multiplayer starting locations.")] int identifier,
        [Description("X coordinate of the destination cell.")] int x,
        [Description("Y coordinate of the destination cell.")] int y,
        [Description("Optional display color; use a value supported by the current configuration.")] string editorColor = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceWaypoint)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(() => mapFacade.PlaceWaypoint(identifier, x, y, editorColor), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_terrain_objects_batch", ReadOnly = false, Destructive = false,
        Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Atomically places selected terrain object types on distinct empty cells. An invalid or occupied cell rejects the whole batch.")]
    public async Task<MCPMapEditResult> PlaceTerrainObjectsBatch(
        [Description("Terrain object types and destination cells; at most 10,000.")] List<MapTerrainObjectPlacement> placements,
        [Description("Optional map revision. When supplied, rejects the call if the map changed.")] int? expectedRevision = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceTerrainObjectsBatch)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(() => mapFacade.PlaceTerrainObjectsBatch(placements, expectedRevision), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_terrain_object_collection_batch", ReadOnly = false, Destructive = false,
        Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Places weighted random entries from one terrain object collection on distinct empty cells. An invalid or occupied cell rejects the batch.")]
    public async Task<MCPMapEditResult> PlaceTerrainObjectCollectionBatch(
        [Description("Configuration name of a terrain object collection returned by get_terrain_object_collections.")] string collectionName,
        [Description("One or more distinct empty map-cell coordinates. At most 10,000 entries are supported.")] List<MapCellCoordinate> cells,
        [Description("Optional map revision. When supplied, rejects the call if the map changed.")] int? expectedRevision = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceTerrainObjectCollectionBatch)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.PlaceTerrainObjectCollectionBatch(collectionName, cells, expectedRevision), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_building", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Places a building at the given foundation origin. By default, its foundation cannot overlap another building.")]
    public async Task<MCPMapEditResult> PlaceBuilding(
        [Description("INI name of the building type returned by get_building_types.")] string buildingTypeName,
        [Description("INI name of the owner returned by get_houses.")] string ownerName,
        [Description("X coordinate of the building foundation origin.")] int x,
        [Description("Y coordinate of the building foundation origin.")] int y,
        [Description("Whether to allow the building foundation to overlap other buildings. Defaults to false.")] bool allowOverlap = false,
        [Description("Optional initial properties; omitted properties use placement defaults.")] MapBuildingPlacementProperties properties = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceBuilding)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.PlaceBuilding(buildingTypeName, ownerName, x, y, allowOverlap, properties), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_aircraft", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Places aircraft at the given cell. By default, the cell cannot already contain aircraft.")]
    public async Task<MCPMapEditResult> PlaceAircraft(
        [Description("INI name of the aircraft type returned by get_aircraft_types.")] string aircraftTypeName,
        [Description("INI name of the owner returned by get_houses.")] string ownerName,
        [Description("X coordinate of the destination cell.")] int x,
        [Description("Y coordinate of the destination cell.")] int y,
        [Description("Whether to allow the aircraft to overlap other aircraft. Defaults to false.")] bool allowOverlap = false,
        [Description("Optional initial properties; onBridge and subCell are invalid for aircraft.")] MapFootPlacementProperties properties = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceAircraft)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.PlaceAircraft(aircraftTypeName, ownerName, x, y, allowOverlap, properties), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_infantry", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Places infantry at the given cell. Uses the requested usable subcell or the first free one.")]
    public async Task<MCPMapEditResult> PlaceInfantry(
        [Description("INI name of the infantry type returned by get_infantry_types.")] string infantryTypeName,
        [Description("INI name of the owner returned by get_houses.")] string ownerName,
        [Description("X coordinate of the destination cell.")] int x,
        [Description("Y coordinate of the destination cell.")] int y,
        [Description("Optional initial properties; omitted properties use placement defaults.")] MapFootPlacementProperties properties = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceInfantry)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.PlaceInfantry(infantryTypeName, ownerName, x, y, properties), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_vehicle", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Places a vehicle at the given cell. By default, the cell cannot already contain a vehicle.")]
    public async Task<MCPMapEditResult> PlaceVehicle(
        [Description("INI name of the vehicle type returned by get_vehicle_types.")] string vehicleTypeName,
        [Description("INI name of the owner returned by get_houses.")] string ownerName,
        [Description("X coordinate of the destination cell.")] int x,
        [Description("Y coordinate of the destination cell.")] int y,
        [Description("Whether to allow the vehicle to overlap other vehicles. Defaults to false.")] bool allowOverlap = false,
        [Description("Optional initial properties; subCell is invalid for vehicles.")] MapFootPlacementProperties properties = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(PlaceVehicle)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.PlaceVehicle(vehicleTypeName, ownerName, x, y, allowOverlap, properties), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "place_terrain_tile", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Places a full terrain tile by tile set and relative index, with brush sizing and optional AutoLAT. May replace existing terrain.")]
    public async Task<MCPMapEditResult> PlaceTerrainTile(
        [Description("Tile-set name returned by get_tile_sets.")] string tileSetName,
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
                () => mapFacade.PlaceTerrainTile(tileSetName, tileIndexInTileSet, x, y, brushWidth, brushHeight, autoLAT), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "generate_terrain", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Generates terrain in a rectangular area using an existing preset and its placement restrictions. Cells outside the map are clipped. " +
        "Returns placement counts and a potential affected-cell summary.")]
    public async Task<TerrainGenerationResult> GenerateTerrain(
        [Description("Exact stable preset ID returned by get_terrain_generator_presets.")] string presetId,
        [Description("X coordinate of the rectangular area's top-left cell.")] int x,
        [Description("Y coordinate of the rectangular area's top-left cell.")] int y,
        [Description("Rectangle width in logical cells. Each dimension may be at most 256 and total rectangular area at most 10,000 cells.")] int width,
        [Description("Rectangle height in logical cells. Each dimension may be at most 256 and total rectangular area at most 10,000 cells.")] int height,
        [Description("Whether to apply automatic LAT transitions after generation.")] bool autoLAT = true,
        [Description("Optional map revision. When supplied, rejects the call if the map changed.")] int? expectedRevision = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GenerateTerrain)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.GenerateTerrain(presetId, x, y, width, height, autoLAT, expectedRevision), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "set_cells_terrain", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Sets one absolute tile and sub-tile index on map cells without a brush or AutoLAT. Duplicate and unchanged cells are ignored.")]
    public async Task<MCPMapEditResult> SetCellsTerrain(
        [Description("One or more map-cell coordinates. At most 10,000 entries are supported.")] List<MapCellCoordinate> cells,
        [Description("Absolute tile index from a get_tile_sets tilesWithUsableGraphics entry or get_tile_details.")] int tileIndex,
        [Description("Sub-tile index from get_tile_details.validSubTiles for the selected full tile.")] int subTileIndex,
        [Description("Optional map revision. When supplied, rejects the call if the map changed.")] int? expectedRevision = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(SetCellsTerrain)}");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(
                () => mapFacade.SetCellsTerrain(cells, tileIndex, subTileIndex, expectedRevision), cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }
}
