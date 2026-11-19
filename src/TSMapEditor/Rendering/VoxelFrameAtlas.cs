using MapEditorLibrary.GameMath;
using MapEditorLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI.Graphics;
using System;

namespace TSMapEditor.Rendering;

/// <summary>
/// Owns the shared pages for the voxel frames of one theater.
/// </summary>
internal sealed class VoxelFrameAtlas(GraphicsDevice graphicsDevice) : IDisposable
{
    private SpriteSheet<int> sheet;

    public PositionedTexture Add(Color[] pixels, int width, int height, Point2D offset)
    {
        sheet ??= new SpriteSheet<int>(graphicsDevice, SurfaceFormat.Color, padding: 1);

        sheet.BeginAppend(preferredPageSize: 2048);
        int key = sheet.Count;
        sheet.AddImage(pixels, width, height, key);
        sheet.Process(graphicsDevice);

        TextureReference region = sheet[key];

        return new PositionedTexture(width, height, offset.X, offset.Y, region.Texture, region.SourceRectangle.Value);
    }

    public void Dispose()
    {
        sheet?.Dispose();
        sheet = null;
    }
}
