using Rampastring.XNAUI.Graphics;

namespace MapEditorLibrary.Graphics;

/// <summary>
/// Queues paletted textures from for packing into a sprite sheet.
/// </summary>
public sealed class SpriteSheetPreparation
{
    private readonly List<Image> images = new();

    internal SpriteSheetPreparation()
    {
    }

    public void AddImage(byte[] pixels, int width, int height, Action<TextureReference> assignRegion)
    {
        images.Add(new Image(pixels, width, height, assignRegion));
    }

    internal void RegisterImages(SpriteSheet<int> sheet)
    {
        foreach (var image in images)
            sheet.AddImage(image.Pixels, image.Width, image.Height, sheet.Count);
    }

    internal void AssignRegions(SpriteSheet<int> sheet, ref int index)
    {
        foreach (var image in images)
        {
            image.AssignRegion(sheet.TextureReferenceFromIndex(index));
            index++;
        }
    }

    private readonly record struct Image(byte[] Pixels, int Width, int Height, Action<TextureReference> AssignRegion);
}
