using MapEditorLibrary;
using MapEditorLibrary.CCEngine;
using MapEditorLibrary.Graphics;
using MapEditorLibrary.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using System;
using System.IO;
using TSMapEditor.Rendering.Batching;
using TSMapEditor.Rendering.ObjectRenderers;

namespace TSMapEditor.Rendering;

public sealed class UnitPreviewRenderer : IDisposable
{
    private readonly record struct Frame(PositionedTexture Regular, PositionedTexture Remap, Texture2D Palette, Rectangle Bounds);

    // The object shader doubles RGB for unpaletted sprites and for normal palette colors.
    private static readonly Color unlitColor = new Color(128, 128, 128, 255);

    private readonly GraphicsDevice graphicsDevice;
    private readonly TheaterGraphics theaterGraphics;
    private readonly RenderTarget2D thumbnailTarget;
    private readonly Effect effect;
    private readonly GameObjectBatcher batcher;

    private readonly BlendState eraseRemap = new BlendState
    {
        ColorSourceBlend = Blend.Zero,
        ColorDestinationBlend = Blend.InverseSourceAlpha,
        AlphaSourceBlend = Blend.Zero,
        AlphaDestinationBlend = Blend.InverseSourceAlpha
    };

    public UnitPreviewRenderer(GraphicsDevice graphicsDevice, TheaterGraphics theaterGraphics, int width, int height)
    {
        this.graphicsDevice = graphicsDevice;
        this.theaterGraphics = theaterGraphics;
        var objectEffect = AssetLoader.LoadEffect("Shaders/PalettedColorDraw") ?? throw new FileNotFoundException("Shader not found: PalettedColorDraw");
        effect = objectEffect.Clone();
        effect.Parameters["IsShadow"].SetValue(false);
        batcher = new GameObjectBatcher(graphicsDevice, effect);
        thumbnailTarget = new RenderTarget2D(graphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
    }

    public (Texture2D Regular, Texture2D Remap) Render(Unit unit)
    {
        var drawParams = theaterGraphics.GetUnitDrawParams(unit.UnitType);
        var (body, middle, top) = UnitRenderer.GetLayers(unit, drawParams);

        Renderer.BeginDraw();
        var frames = new[] { GetFrame(unit, body), GetFrame(unit, middle), GetFrame(unit, top) };
        Renderer.EndDraw();

        Rectangle bounds = Rectangle.Empty;
        bool hasRemap = false;
        foreach (var frame in frames)
        {
            if (frame.Regular == null)
                continue;

            bounds = bounds.IsEmpty ? frame.Bounds : Rectangle.Union(bounds, frame.Bounds);
            hasRemap |= frame.Remap != null;
        }

        if (bounds.IsEmpty)
            return (null, null);

        using var compositeTarget = new RenderTarget2D(graphicsDevice, bounds.Width, bounds.Height, false, SurfaceFormat.Color, DepthFormat.None);
        Texture2D regular = Composite(frames, bounds, compositeTarget, false);
        Texture2D remap = hasRemap && unit.Remapable() ? Composite(frames, bounds, compositeTarget, true) : null;

        return (regular, remap);
    }

    private static Frame GetFrame(Unit unit, in UnitRenderer.Layer layer)
    {
        var shape = layer.Shape;
        PositionedTexture regular = shape != null
            ? shape.GetFrame(layer.FrameIndex)
            : layer.Voxel?.GetFrame(unit.Facing, RampType.None, false);

        if (regular?.Texture == null)
            return default;

        PositionedTexture remap = shape != null
            ? (shape.HasRemapFrames() ? shape.GetRemapFrame(layer.FrameIndex) : null)
            : layer.Voxel.GetRemapFrame(unit.Facing, RampType.None, false);
        Rectangle bounds = UnitRenderer.GetTextureDrawCoords(unit, regular, layer.Offset);

        return new Frame(regular, remap, shape?.GetPaletteTexture(), bounds);
    }

    private Texture2D Composite(Frame[] frames, Rectangle bounds, RenderTarget2D target, bool remap)
    {
        Renderer.BeginDraw();
        Renderer.PushRenderTarget(target);
        graphicsDevice.Clear(Color.Transparent);
        graphicsDevice.RasterizerState = RasterizerState.CullNone;

        foreach (var frame in frames)
        {
            if (frame.Regular == null)
                continue;

            Rectangle destination = frame.Bounds;
            destination.Offset(-bounds.X, -bounds.Y);

            // Every opaque pixel in the current layer covers any remap pixels underneath it.
            graphicsDevice.BlendState = remap ? eraseRemap : BlendState.AlphaBlend;
            Draw(frame.Regular, destination, frame.Palette, false, unlitColor);

            // SHP frames store the remap pixels separately from the regular pixels.
            if (frame.Remap != null && (remap || frame.Palette != null))
            {
                graphicsDevice.BlendState = BlendState.AlphaBlend;
                bool palettedRemap = remap && frame.Palette != null;
                Draw(frame.Remap, destination, frame.Palette, palettedRemap, palettedRemap ? Color.White : unlitColor);
            }
        }

        Renderer.PopRenderTarget();
        Renderer.EndDraw();

        return Helpers.RenderTextureAsSmaller(target, thumbnailTarget, graphicsDevice);
    }

    private void Draw(PositionedTexture frame, Rectangle destination, Texture2D palette, bool useRemap, Color color)
    {
        effect.Parameters["UsePalette"].SetValue(palette != null);
        effect.Parameters["UseRemap"].SetValue(useRemap);

        if (palette != null)
            effect.Parameters["PaletteTexture"].SetValue(palette);

        batcher.Begin(depthStencilState: DepthStencilState.None);
        batcher.Draw(frame.Texture, destination, frame.SourceRectangle, color, default);
        batcher.End();
    }

    public void Dispose()
    {
        thumbnailTarget.Dispose();
        effect.Dispose();
        eraseRemap.Dispose();
    }
}
