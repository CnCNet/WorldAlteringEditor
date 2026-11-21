using System.ComponentModel;

namespace MapEditorMCP;

/// <summary>
/// Lets the MCP server read and move the editor's camera. Implemented by the editor UI.
/// All members are called on the game thread.
/// </summary>
public interface IEditorViewController
{
    EditorCameraState GetCameraState();

    /// <summary>
    /// Centers the camera on a cell and optionally changes the zoom level.
    /// Unless <paramref name="force"/> is set, the camera is not moved while the user
    /// is actively working with the map view, so an agent never yanks the view away
    /// from under the user's cursor.
    /// </summary>
    EditorCameraMoveResult MoveCamera(int centerCellX, int centerCellY, double? zoomLevel, bool force);
}

/// <summary>
/// Lets the MCP server save the map. Implemented by the editor UI.
/// All members are called on the game thread.
/// </summary>
public interface IMapSaveHandler
{
    /// <summary>
    /// Saves the map without showing any dialog to the user.
    /// </summary>
    /// <param name="filePath">Path to save to, or null to save to the map's current file.</param>
    /// <param name="overwrite">Whether an existing file other than the map's current file may be replaced.</param>
    EditorMapSaveResult SaveMap(string filePath, bool overwrite);
}

public sealed class EditorCameraState
{
    [Description("Cell at the center of the map view.")]
    public int CenterCellX { get; init; }

    [Description("Cell at the center of the map view.")]
    public int CenterCellY { get; init; }

    [Description("Zoom level; 1.0 is the default, the editor allows 0.2 through 3.0.")]
    public double ZoomLevel { get; init; }

    [Description("Approximate number of cells visible across the view.")]
    public int VisibleCellsAcross { get; init; }

    [Description("Approximate number of cells visible from top to bottom of the view.")]
    public int VisibleCellsDown { get; init; }
}

public sealed class EditorCameraMoveResult
{
    [Description("Whether the camera was moved.")]
    public bool Moved { get; init; }

    [Description("Why the camera was not moved, for example because the user is currently scrolling or painting on the map.")]
    public string NotMovedReason { get; init; }

    public EditorCameraState Camera { get; init; }
}

public sealed class EditorMapSaveResult
{
    public bool Saved { get; init; }

    [Description("Full path of the file that was written.")]
    public string FilePath { get; init; }

    [Description("True if the map was saved to a different file than before; the editor now uses that file for subsequent saves.")]
    public bool SavedToNewPath { get; init; }

    [Description("Why the map was not saved.")]
    public string Error { get; init; }

    [Description("Problems the editor's map checker found after saving. The user normally sees these in a dialog after a manual save; " +
        "for MCP saves no dialog is shown, so report relevant issues to the user yourself.")]
    public List<string> Issues { get; init; } = new List<string>();
}
