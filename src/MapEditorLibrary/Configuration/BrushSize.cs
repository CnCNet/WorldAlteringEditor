using MapEditorLibrary.GameMath;

namespace MapEditorLibrary.Configuration;

public class BrushSize
{
    public BrushSize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int Width { get; }
    public int Height { get; }
    public int Max => Math.Max(Width, Height);

    public Point2D CenterWithinBrush(Point2D point) => new Point2D(point.X - (Width / 2), point.Y - (Height / 2));

    /// <summary>
    /// Enumerates the cells covered by a brush anchored at the given coordinates.
    /// Optional padding includes surrounding cells for connection updates and undo.
    /// </summary>
    public IEnumerable<Point2D> GetCells(Point2D origin, int padding = 0)
    {
        for (int y = -padding; y < Height + padding; y++)
        {
            for (int x = -padding; x < Width + padding; x++)
            {
                yield return origin + new Point2D(x, y);
            }
        }
    }

    /// <summary>
    /// Sweeps the brush along an inclusive line, returning each covered cell only once.
    /// Optional padding includes surrounding cells for connection updates and undo.
    /// </summary>
    public IEnumerable<Point2D> GetCellsAlongLine(Point2D sourceCoords, Direction direction, int length, int padding = 0)
    {
        var visitedCells = new HashSet<Point2D>();
        Point2D step = Helpers.VisualDirectionToPoint(direction);

        for (int i = 0; i <= length; i++)
        {
            foreach (Point2D coords in GetCells(sourceCoords + step.ScaleBy(i), padding))
            {
                if (visitedCells.Add(coords))
                    yield return coords;
            }
        }
    }

    public void DoForBrushSize(Action<Point2D> action)
    {
        DoForArea(0, 0, Height, Width, action);
    }

    public bool CheckForAnyCellInBrushArea(Func<Point2D, bool> checker)
    {
        return CheckForAnyCellInArea(0, 0, Height, Width, checker);
    }

    public void DoForBrushSizeAndSurroundings(Action<Point2D> action)
    {
        DoForArea(-1, -1, Height + 1, Width + 1, action);
    }

    private void DoForArea(int initY, int initX, int height, int width, Action<Point2D> action)
    {
        for (int y = initY; y < height; y++)
        {
            for (int x = initX; x < width; x++)
            {
                action(new Point2D(x, y));
            }
        }
    }

    private bool CheckForAnyCellInArea(int initY, int initX, int height, int width, Func<Point2D, bool> checker)
    {
        for (int y = initY; y < height; y++)
        {
            for (int x = initX; x < width; x++)
            {
                if (checker(new Point2D(x, y)))
                    return true;
            }
        }

        return false;
    }

    public override string ToString()
    {
        return Width + "x" + Height;
    }
}
