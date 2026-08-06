using MapEditorLibrary.GameMath;

namespace MapEditorLibrary.Mutations.Classes;

/// <summary>
/// Struct for un-do data of mutations that change overlay of cells.
/// </summary>
public struct OriginalOverlayInfo
{
    public int OverlayTypeIndex;
    public int FrameIndex;
    public Point2D CellCoords;

    public OriginalOverlayInfo(int overlayTypeIndex, int frameIndex, Point2D cellCoords)
    {
        OverlayTypeIndex = overlayTypeIndex;
        FrameIndex = frameIndex;
        CellCoords = cellCoords;
    }
}
