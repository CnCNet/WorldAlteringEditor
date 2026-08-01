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

    [McpServerTool(Name = "get_tile_sets", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns tile sets that are available for placement in the current map's theater.")]
    public Task<List<MapTileSetInfo>> GetTileSets(
        [Description("Optional case-insensitive filter matched against tile set name and UI name.")] string nameFilter = null,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(MapTools)}.{nameof(GetTileSets)}");
        return gameThreadDispatcher.InvokeAsync(() => mapFacade.GetTileSets(nameFilter), cancellationToken);
    }

    [McpServerTool(Name = "inspect_map_region", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns terrain, terrain objects, and overlays from a rectangular region of the open map.")]
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
