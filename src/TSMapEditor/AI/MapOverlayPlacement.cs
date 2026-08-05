using System.ComponentModel;

namespace TSMapEditor.AI;

public sealed class MapOverlayPlacement
{
    [Description("INI name of the overlay type to place.")]
    public string OverlayTypeName { get; set; }

    [Description("X coordinate of the destination map cell.")]
    public int X { get; set; }

    [Description("Y coordinate of the destination map cell.")]
    public int Y { get; set; }

    [Description("Optional zero-based placeable artwork frame index. Omit it for WAE's normal placement behavior and automatic resource smoothing.")]
    public int? FrameIndex { get; set; }
}
