using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MapEditorLibrary.Graphics;

public class PositionedTexture
{
    public int ShapeWidth;
    public int ShapeHeight;
    public int OffsetX;
    public int OffsetY;
    public Texture2D Texture;
    public Rectangle SourceRectangle;

    public PositionedTexture(int shapeWidth, int shapeHeight, int offsetX, int offsetY, Texture2D texture, Rectangle sourceRectangle)
    {
        ShapeWidth = shapeWidth;
        ShapeHeight = shapeHeight;
        OffsetX = offsetX;
        OffsetY = offsetY;
        Texture = texture;
        SourceRectangle = sourceRectangle;
    }

    public void Dispose()
    {
        if (Texture != null)
            Texture.Dispose();
    }
}
