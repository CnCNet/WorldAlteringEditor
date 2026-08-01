using System.ComponentModel;

namespace TSMapEditor.AI;

public class MapTerrainObjectReference
{
    [Description("X coordinate returned for the terrain object by inspect_map_region.")]
    public int X { get; set; }

    [Description("Y coordinate returned for the terrain object by inspect_map_region.")]
    public int Y { get; set; }

    [Description("INI name returned for the terrain object by inspect_map_region. Prevents deleting a different terrain object type at the same cell.")]
    public string ININame { get; set; }
}
