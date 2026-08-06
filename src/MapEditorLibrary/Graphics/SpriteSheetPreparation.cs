using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MapEditorLibrary.Graphics;

/// <summary>
/// Class for gathering 8-bit paletted sprites for generating a single sprite sheet texture.
/// </summary>
public class SpriteSheetPreparation
{
    public SpriteSheetPreparation(int width, int height)
    {
        WorkingBufferWidth = width;
        WorkingBufferHeight = height;
        WorkingBuffer = new byte[WorkingBufferWidth * WorkingBufferHeight];
    }

    public int WorkingBufferWidth { get; }
    public int WorkingBufferHeight { get; }

    public readonly byte[] WorkingBuffer;
    public int maxX = 0;                        // Width of the whole mega-texture (width of the widest row of images).
    public int maxY = 0;                        // Height of the whole mega-texture (height of all rows summed).
    public int X { get; private set; } = 0;     // Horizontal start position of the next tile.
    public int Y { get; private set; } = 0;     // Vertical position of the current row.
    int rowHeight = 0;                          // Height of the current row (aka height of the tallest tile in the current row).

    public List<object> objs = new List<object>();

    public Texture2D Texture { get; set; }

    /// <summary>
    /// Sprite sheets get composited into bigger sprite sheets at the end of the graphics-loading phase.
    /// When that happens, the sprite sheet (and, individual images in the sprite sheet) might get shifted
    /// downwards compared to its original location. This records how much the shift was, so texture
    /// source rectangles can be adjusted accordingly.
    /// </summary>
    public int YOffset { get; set; }

    public int Width => Math.Max(maxX, X);

    public int Height => Y + rowHeight;

    public bool CanFitTexture(int width, int height)
    {
        if (width >= WorkingBufferWidth || height >= WorkingBufferHeight)
            throw new ArgumentException("Texture too large: " + width + "x" + height);

        // Check if fits on current row
        if (X + width < WorkingBufferWidth)
        {
            if (Y + height < WorkingBufferHeight)
            {
                return true;
            }
        }

        // If not, check if the texture would fit if placed on a new row
        if (width < WorkingBufferWidth &&
            Y + rowHeight + height < WorkingBufferHeight)
        {
            return true;
        }

        return false;
    }

    public Point AddImage(int width, int height, byte[] imageData, object meta)
    {
        if (imageData.Length != width * height)
            throw new ArgumentException($"{nameof(SpriteSheetPreparation)}: Image data needs to match width x height. Expected size: {width * height}, actual: {imageData}");

        if (X + width > WorkingBufferWidth)
        {
            // Advance to next row
            maxX = Math.Max(maxX, X);
            X = 0;
            Y += rowHeight;
            rowHeight = 0;

            if (Y + height > WorkingBufferHeight)
            {
                throw new InvalidOperationException("Image does not fit on MegaTexture!");
            }
        }

        // Save placement coord
        Point placementCoord = new Point(X, Y);

        // Copy buffer
        for (int h = 0; h < height; h++)
        {
            Buffer.BlockCopy(imageData, h * width, WorkingBuffer, (Y + h) * WorkingBufferWidth + X, width);
        }

        if (height > rowHeight)
            rowHeight = height;

        if (meta != null)
            objs.Add(meta);

        X += width;
        return placementCoord;
    }
}
