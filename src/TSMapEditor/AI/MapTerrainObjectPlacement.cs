using System.ComponentModel;

namespace TSMapEditor.AI;

public sealed class MapTerrainObjectPlacement
{
    [Description("INI name of the terrain object type to place.")]
    public string TerrainTypeName { get; set; }

    [Description("X coordinate of the destination map cell.")]
    public int X { get; set; }

    [Description("Y coordinate of the destination map cell.")]
    public int Y { get; set; }
}
