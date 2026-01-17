using System;
using TSMapEditor.GameMath;

namespace TSMapEditor.UI
{
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

        public void DoForBrushSize(Action<Point2D> action)
        {
            DoForArea(0, 0, Height, Width, action);
        }

        public bool CheckForBrushSize(Func<Point2D, bool> checker)
        {
            return CheckForAnyInArea(0, 0, Height, Width, checker);
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

        private bool CheckForAnyInArea(int initY, int initX, int height, int width, Func<Point2D, bool> checker)
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
}
