using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI.Graphics;

namespace MapEditorLibrary.Graphics;

public class PositionedTexture
{
    public int ShapeWidth;
    public int ShapeHeight;
    public int OffsetX;
    public int OffsetY;
    public Texture2D Texture;
    public Rectangle SourceRectangle;

    private bool spriteSheetOwned;

    public PositionedTexture(int shapeWidth, int shapeHeight, int offsetX, int offsetY, Texture2D texture, Rectangle sourceRectangle)
    {
        ShapeWidth = shapeWidth;
        ShapeHeight = shapeHeight;
        OffsetX = offsetX;
        OffsetY = offsetY;
        Texture = texture;
        SourceRectangle = sourceRectangle;
    }

    internal void SetSpriteSheetRegion(TextureReference region)
    {
        Texture = region.Texture;
        SourceRectangle = region.SourceRectangle.Value;
        spriteSheetOwned = true;
    }

    public void Dispose()
    {
        if (!spriteSheetOwned)
            Texture?.Dispose();
    }
}
