using System.ComponentModel;

namespace MapEditorMCP.Infos;

internal sealed class MapCellCoordinate
{
    [Description("X coordinate of the map cell.")]
    public int X { get; set; }

    [Description("Y coordinate of the map cell.")]
    public int Y { get; set; }
}
