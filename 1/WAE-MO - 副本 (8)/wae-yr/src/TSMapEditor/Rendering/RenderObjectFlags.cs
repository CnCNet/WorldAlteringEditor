using System;

namespace TSMapEditor.Rendering
{
    [Flags]
    public enum RenderObjectFlags
    {
        None = 0,
        Terrain = 1,
        Smudges = 2,
        Overlay = 4,
        Aircraft = 8,
        Infantry = 16,
        Vehicles = 32,
        Structures = 64,
        TerrainObjects = 128,
        CellTags = 256,
        Waypoints = 512,
        BaseNodes = 1024,
        AlphaLights = 2048,
        TunnelTubes = 4096,
        All = Terrain + Smudges + Overlay + Aircraft + Infantry + Vehicles + Structures + TerrainObjects + CellTags + Waypoints + BaseNodes + AlphaLights + TunnelTubes,
    }
}
