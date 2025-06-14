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

        public void DoForBrushSize(Action<Point2D> action)
        {
            DoForArea(0, 0, Height, Width, action);
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

        public override string ToString()
        {
            return Width + "x" + Height;
        }
    }
}
