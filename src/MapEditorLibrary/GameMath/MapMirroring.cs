namespace MapEditorLibrary.GameMath;

[Flags]
public enum MirrorAxes
{
    None = 0,
    NorthSouth = 1,
    NorthEastSouthWest = 2,
    EastWest = 4,
    NorthWestSouthEast = 8,
    HalfTurn = 16
}

/// <summary>
/// A square-grid transform applied around a mirroring point. Reflection swaps the
/// two logical axes, and each quarter turn then rotates clockwise.
/// </summary>
public readonly record struct MirrorTransform(int QuarterTurns, bool Reflected)
{
    public Point2D ApplyToOffset(Point2D offset)
    {
        if (Reflected)
            offset = new Point2D(offset.Y, offset.X);

        return QuarterTurns switch
        {
            1 => new Point2D(-offset.Y, offset.X),
            2 => new Point2D(-offset.X, -offset.Y),
            3 => new Point2D(offset.Y, -offset.X),
            _ => offset
        };
    }

    public byte ApplyToFacing(byte facing)
    {
        // Facing zero points visually northeast, which a north-south reflection sends northwest.
        int direction = Reflected ? 6 * Constants.FacingUnitsPerDirection - facing : facing;
        return (byte)((direction + QuarterTurns * 2 * Constants.FacingUnitsPerDirection) & Constants.FacingMax);
    }

    internal MirrorTransform Then(MirrorTransform next)
    {
        int turns = next.QuarterTurns + (next.Reflected ? -QuarterTurns : QuarterTurns);
        return new MirrorTransform(turns & 3, Reflected ^ next.Reflected);
    }
}

public class MapMirroring
{
    public MapMirroring(MirrorAxes axes, Point2D offsetInHalfTiles = default)
    {
        Axes = axes;
        OffsetInHalfTiles = offsetInHalfTiles;

        var generators = new List<MirrorTransform>();
        for (int turns = 0; turns < 4; turns++)
        {
            if ((axes & (MirrorAxes)(1 << turns)) != MirrorAxes.None)
                generators.Add(new MirrorTransform(turns, true));
        }

        if (axes.HasFlag(MirrorAxes.HalfTurn))
            generators.Add(new MirrorTransform(2, false));

        // Take the closure so every placement has the same counterparts
        // regardless of which side of an axis the user paints on.
        var transforms = new List<MirrorTransform> { default };
        for (int i = 0; i < transforms.Count; i++)
        {
            foreach (MirrorTransform generator in generators)
            {
                MirrorTransform next = transforms[i].Then(generator);
                if (!transforms.Contains(next))
                    transforms.Add(next);
            }
        }

        Transforms = transforms.AsReadOnly();
    }

    public MirrorAxes Axes { get; }

    /// <summary>
    /// Offset from the default map center in half-tile steps along the map view's horizontal and vertical axes.
    /// </summary>
    public Point2D OffsetInHalfTiles { get; }

    public IReadOnlyList<MirrorTransform> Transforms { get; }

    public static Point2D GetDoubledCenter(Point2D mapSize)
    {
        // The outer rows are staggered. Snap the center to the northern of
        // the two nearest positions where all four axes preserve the grid.
        int center = mapSize.X + mapSize.Y;
        return new Point2D(center, center);
    }

    /// <summary>
    /// Gets the mirroring point in doubled cell coordinates so it can lie between cells.
    /// </summary>
    public Point2D GetDoubledMirroringPoint(Point2D mapSize)
    {
        // Projected X follows the difference of the cell coordinates, and Y follows their sum.
        var offset = new Point2D(OffsetInHalfTiles.Y + OffsetInHalfTiles.X, OffsetInHalfTiles.Y - OffsetInHalfTiles.X);
        return GetDoubledCenter(mapSize) + offset;
    }

    /// <summary>
    /// Gets the horizontal and vertical half-tile offsets that put the mirroring point at a cell's center.
    /// </summary>
    public static Point2D GetOffsetForCell(Point2D cell, Point2D mapSize)
    {
        Point2D delta = cell.ScaleBy(2) - GetDoubledCenter(mapSize);
        return new Point2D((delta.X - delta.Y) / 2, (delta.X + delta.Y) / 2);
    }

    public static Point2D TransformCell(Point2D cell, Point2D doubledMirroringPoint, MirrorTransform transform)
    {
        Point2D doubledCell = TransformDoubledPosition(cell.ScaleBy(2), doubledMirroringPoint, transform);
        return new Point2D(doubledCell.X / 2, doubledCell.Y / 2);
    }

    /// <summary>
    /// Transforms a position in doubled cell coordinates, such as the center of a building foundation.
    /// </summary>
    public static Point2D TransformDoubledPosition(Point2D doubledPosition, Point2D doubledMirroringPoint, MirrorTransform transform) =>
        doubledMirroringPoint + transform.ApplyToOffset(doubledPosition - doubledMirroringPoint);
}
