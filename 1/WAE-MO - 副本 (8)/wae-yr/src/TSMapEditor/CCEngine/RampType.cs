namespace TSMapEditor.CCEngine
{
    /// <summary>
    /// Defines the types of terrain ramps in the game.
    /// </summary>
    /// Taken from TS++, TIBSUN_DEFINES.H
    /// https://github.com/Vinifera-Developers/TSpp
    /// Originally by CCHyper, adjusted for C# by Rampastring
    public enum RampType
    {
        None = 0,

        // Basic, two adjacent corners raised
        West = 1,
        North = 2,
        East = 3,
        South = 4,

        // Tile outside corners (one corner raised by half a cell)
        CornerNW = 5,
        CornerNE = 6,
        CornerSE = 7,
        CornerSW = 8,

        // Tile inside corners (three corners raised by half a cell)
        MidNW = 9,
        MidNE = 10,
        MidSE = 11,
        MidSW = 12,

        // Full tile sloped (mid corners raised by half cell, far corner by full cell)
        SteepSE = 13,
        SteepSW = 14,
        SteepNW = 15,
        SteepNE = 16,

        // Double ramps (two corners raised, alternating)
        DoubleUpSWNE = 17,
        DoubleDownSWNE = 18,
        DoubleUpNWSE = 19,
        DoubleDownNWSE = 20
    }
}
