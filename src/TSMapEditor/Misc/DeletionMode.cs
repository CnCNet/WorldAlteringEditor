using System;

namespace TSMapEditor.Misc
{
    /// <summary>
    /// Enum for specifying which object types should be erased when an object
    /// is erased from the map with a deletion tool.
    /// </summary>
    [Flags]
    public enum DeletionMode
    {
        None = 0,
        CellTags = 1,
        Waypoints = 2,
        Aircraft = 4,
        Infantry = 8,
        Vehicles = 16,
        Structures = 32,
        TerrainObjects = 64,
        All = CellTags + Waypoints + Aircraft + Infantry + Vehicles + Structures + TerrainObjects
    }
}
