namespace TSMapEditor.GameMath
{
    /// <summary>
    /// Enumeration for "visual direction", iow. directions in the way
    /// the player perceives them. This is slightly rotated compared to the
    /// internal game compass where "north" actually points visually towards
    /// the northeast.
    /// </summary>
    public enum Direction
    {
        NE = 0,
        E = 1,
        SE = 2,
        S = 3,
        SW = 4,
        W = 5,
        NW = 6,
        N = 7,

        Count = 8
    }
}
