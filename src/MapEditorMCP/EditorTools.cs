using ModelContextProtocol;
using ModelContextProtocol.Server;
using Rampastring.Tools;
using System.ComponentModel;

namespace MapEditorMCP;

/// <summary>
/// MCP tools that act on the editor itself rather than on map content:
/// the camera and saving the map.
/// </summary>
[McpServerToolType]
internal sealed class EditorTools
{
    public EditorTools(MapFacade mapFacade, GameThreadDispatcher gameThreadDispatcher, EditorHostServices hostServices)
    {
        this.mapFacade = mapFacade;
        this.gameThreadDispatcher = gameThreadDispatcher;
        viewController = hostServices.ViewController;
        saveHandler = hostServices.SaveHandler;
    }

    private readonly MapFacade mapFacade;
    private readonly GameThreadDispatcher gameThreadDispatcher;
    private readonly IEditorViewController viewController;
    private readonly IMapSaveHandler saveHandler;

    [McpServerTool(Name = "get_camera", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Returns where the user's map view is currently looking: the center cell, zoom level, and approximate visible size in cells. " +
        "Useful to work on the area the user is looking at, or to restore their view after moving the camera.")]
    public Task<EditorCameraState> GetCamera(CancellationToken cancellationToken)
    {
        Logger.Log($"{nameof(EditorTools)}.{nameof(GetCamera)}");
        return gameThreadDispatcher.InvokeAsync(() => GetViewController().GetCameraState(), cancellationToken);
    }

    [McpServerTool(Name = "move_camera", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Moves the user's map view to center on a cell, optionally changing the zoom level, so the user can see something you did. " +
        "Give either a cell (x and y) or a mutationId to frame the cells changed by that action. This changes what the user sees, so only " +
        "move the camera when presenting results or when the user asks; screenshot_map_region does not need the camera. If the user is " +
        "actively working on the map (scrolling, painting, dragging objects, or moved the view in the last two seconds), the camera is " +
        "not moved and notMovedReason explains why; do not retry in a loop.")]
    public async Task<EditorCameraMoveResult> MoveCamera(
        [Description("X coordinate of the cell to center on. Required unless mutationId is given.")] int? x = null,
        [Description("Y coordinate of the cell to center on. Required unless mutationId is given.")] int? y = null,
        [Description("Optional zoom level from 0.2 through 3.0; 1.0 is the default. Omit to keep the current zoom.")] double? zoomLevel = null,
        [Description("Optional MCP mutation ID; the camera centers on the cells that action changed.")] long? mutationId = null,
        [Description("Move even if the user is currently working on the map. Only set this when the user explicitly asked you to move the view.")] bool force = false,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(EditorTools)}.{nameof(MoveCamera)}");

        if (zoomLevel.HasValue && (double.IsNaN(zoomLevel.Value) || zoomLevel.Value < 0.2 || zoomLevel.Value > 3.0))
            throw new McpException("zoomLevel must be from 0.2 through 3.0.");

        if (mutationId.HasValue == (x.HasValue || y.HasValue))
            throw new McpException("Give either both x and y, or mutationId.");

        if (!mutationId.HasValue && (!x.HasValue || !y.HasValue))
            throw new McpException("Both x and y are required when mutationId is not given.");

        try
        {
            return await gameThreadDispatcher.InvokeAsync(() =>
            {
                int targetX;
                int targetY;

                if (mutationId.HasValue)
                {
                    var region = mapFacade.ReviewAction(mutationId.Value, 1).SuggestedViewRegion ??
                        throw new MapFacadeValidationException($"Mutation ID {mutationId.Value} has no known affected cells to center on.");
                    targetX = region.X + region.Width / 2;
                    targetY = region.Y + region.Height / 2;
                }
                else
                {
                    targetX = x.Value;
                    targetY = y.Value;
                }

                if (!mapFacade.IsCellWithinMap(targetX, targetY))
                    throw new MapFacadeValidationException($"Cell ({targetX}, {targetY}) is not within the map.");

                return GetViewController().MoveCamera(targetX, targetY, zoomLevel, force);
            }, cancellationToken);
        }
        catch (MapFacadeValidationException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "save_map", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Saves the map to disk, exactly like File > Save in the editor, without showing any dialog to the user. Omit filePath to " +
        "save to the map's current file. Give filePath to save to a new file (Save As); this is required for a new map that has never been " +
        "saved. Saving does not change the undo history. Only save when the user asked you to or agreed to it, since it overwrites the " +
        "map file. The result lists any problems the editor's map checker found.")]
    public async Task<EditorMapSaveResult> SaveMap(
        [Description("Optional absolute path ending in .map, .mpr, or .yrm. Omit to save to the current file.")] string filePath = null,
        [Description("Allow replacing an existing file at filePath that is not the map's current file.")] bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        Logger.Log($"{nameof(EditorTools)}.{nameof(SaveMap)}");

        if (filePath != null)
        {
            filePath = filePath.Trim();

            if (filePath.Length == 0 || !Path.IsPathFullyQualified(filePath))
                throw new McpException("filePath must be an absolute path.");

            string extension = Path.GetExtension(filePath);
            if (!extension.Equals(".map", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".mpr", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".yrm", StringComparison.OrdinalIgnoreCase))
            {
                throw new McpException("filePath must end in .map, .mpr, or .yrm.");
            }
        }

        EditorMapSaveResult result = await gameThreadDispatcher.InvokeAsync(
            () => GetSaveHandler().SaveMap(filePath, overwrite), cancellationToken);

        if (!result.Saved)
            throw new McpException(result.Error ?? "The map could not be saved.");

        return result;
    }

    private IEditorViewController GetViewController()
        => viewController ?? throw new McpException("Camera control is not available in this editor session.");

    private IMapSaveHandler GetSaveHandler()
        => saveHandler ?? throw new McpException("Saving is not available in this editor session.");
}

/// <summary>
/// Optional editor host services; either may be null when the host does not support them.
/// </summary>
internal sealed class EditorHostServices
{
    public EditorHostServices(IEditorViewController viewController, IMapSaveHandler saveHandler)
    {
        ViewController = viewController;
        SaveHandler = saveHandler;
    }

    public IEditorViewController ViewController { get; }
    public IMapSaveHandler SaveHandler { get; }
}
