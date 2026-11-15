using MapEditorLibrary;
using MapEditorLibrary.CCEngine;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Graphics;
using MapEditorLibrary.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.IO;
using TSMapEditor.Rendering.Batching;
using TSMapEditor.Rendering.ObjectRenderers;

namespace TSMapEditor.Rendering;

public sealed class ObjectPreviewRenderer : IDisposable
{
    private const byte previewFacing = (byte)Direction.SE * Constants.FacingUnitsPerDirection;

    private readonly record struct Frame(PositionedTexture Regular, PositionedTexture Remap, Texture2D Palette, Rectangle Bounds, byte Opacity);

    // The object shader doubles RGB for unpaletted sprites and for normal palette colors.
    private static readonly Color unlitColor = new Color(128, 128, 128, 255);

    private readonly GraphicsDevice graphicsDevice;
    private readonly TheaterGraphics theaterGraphics;
    private readonly RenderTarget2D thumbnailTarget;
    private readonly Effect effect;
    private readonly GameObjectBatcher batcher;
    private readonly List<Animation> animationList = new List<Animation>();

    private readonly BlendState eraseRemap = new BlendState
    {
        ColorSourceBlend = Blend.Zero,
        ColorDestinationBlend = Blend.InverseSourceAlpha,
        AlphaSourceBlend = Blend.Zero,
        AlphaDestinationBlend = Blend.InverseSourceAlpha
    };

    public ObjectPreviewRenderer(GraphicsDevice graphicsDevice, TheaterGraphics theaterGraphics, int width, int height)
    {
        this.graphicsDevice = graphicsDevice;
        this.theaterGraphics = theaterGraphics;
        var objectEffect = AssetLoader.LoadEffect("Shaders/PalettedColorDraw") ?? throw new FileNotFoundException("Shader not found: PalettedColorDraw");
        effect = objectEffect.Clone();
        effect.Parameters["IsShadow"].SetValue(false);
        batcher = new GameObjectBatcher(graphicsDevice, effect);
        thumbnailTarget = new RenderTarget2D(graphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None);
    }

    public (Texture2D Regular, Texture2D Remap) Render(UnitType unitType)
    {
        var unit = new Unit(unitType) { Facing = previewFacing };
        var drawParams = theaterGraphics.GetUnitDrawParams(unitType);
        var (body, middle, top) = UnitRenderer.GetLayers(unit, drawParams);

        Renderer.BeginDraw();
        var frames = new[] { GetUnitFrame(unit, body), GetUnitFrame(unit, middle), GetUnitFrame(unit, top) };
        Renderer.EndDraw();

        return RenderFrames(frames, unit.Remapable());
    }

    public (Texture2D Regular, Texture2D Remap) Render(BuildingType buildingType)
    {
        var building = new Structure(buildingType) { Facing = previewFacing };
        var drawParams = theaterGraphics.GetBuildingDrawParams(buildingType);
        var (lower, upper) = BuildingRenderer.GetVoxelTurretLayers(building, drawParams);
        int aboveBuildingIndex = BuildingRenderer.GetOrderedAnimations(building, animationList);

        var frames = new List<Frame>
        {
            GetShapeFrame(building, theaterGraphics.BuildingBibTextures[buildingType.Index], 0)
        };

        for (int i = 0; i < aboveBuildingIndex; i++)
            frames.Add(GetAnimationFrame(animationList[i]));

        frames.Add(GetBuildingFrame(building, drawParams.ShapeImage));

        for (int i = aboveBuildingIndex; i < animationList.Count; i++)
            frames.Add(GetAnimationFrame(animationList[i]));

        Renderer.BeginDraw();
        frames.Add(GetVoxelFrame(building, lower.Model, building.Facing, lower.Offset));
        frames.Add(GetVoxelFrame(building, upper.Model, building.Facing, upper.Offset));
        Renderer.EndDraw();

        return RenderFrames(frames, building.Remapable());
    }

    private (Texture2D Regular, Texture2D Remap) RenderFrames(IReadOnlyList<Frame> frames, bool remapable)
    {
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
        Texture2D remap = hasRemap && remapable ? Composite(frames, bounds, compositeTarget, true) : null;

        return (regular, remap);
    }

    private static Frame GetUnitFrame(Unit unit, in UnitRenderer.Layer layer)
    {
        return layer.Shape != null
            ? GetShapeFrame(unit, layer.Shape, layer.FrameIndex)
            : GetVoxelFrame(unit, layer.Voxel, unit.Facing, layer.Offset);
    }

    private static Frame GetBuildingFrame(Structure building, ShapeImage shape)
    {
        if (shape == null)
            return default;

        int frameCount = shape.GetFrameCount();
        for (int frameIndex = building.GetFrameIndex(frameCount); frameIndex < frameCount; frameIndex++)
        {
            Frame frame = GetShapeFrame(building, shape, frameIndex);
            if (frame.Regular != null)
                return frame;
        }

        return default;
    }

    private Frame GetAnimationFrame(Animation animation)
    {
        var shape = theaterGraphics.AnimTextures[animation.AnimType.Index];
        if (shape == null)
            return default;

        int frameIndex = animation.GetFrameIndex(shape.GetFrameCount());
        return GetShapeFrame(animation, shape, frameIndex, BuildingRenderer.GetAnimationOpacity(animation));
    }

    private static Frame GetShapeFrame<T>(T gameObject, ShapeImage shape, int frameIndex, byte opacity = byte.MaxValue) where T : GameObject
    {
        PositionedTexture regular = shape?.GetFrame(frameIndex);

        if (regular?.Texture == null)
            return default;

        PositionedTexture remap = shape.HasRemapFrames() ? shape.GetRemapFrame(frameIndex) : null;
        Rectangle bounds = ObjectRenderer<T>.GetTextureDrawCoords(gameObject, regular, Point2D.Zero);

        return new Frame(regular, remap, shape.GetPaletteTexture(), bounds, opacity);
    }

    private static Frame GetVoxelFrame<T>(T gameObject, VoxelModel model, byte facing, Point2D offset) where T : GameObject
    {
        PositionedTexture regular = model?.GetFrame(facing, RampType.None, false);

        if (regular?.Texture == null)
            return default;

        PositionedTexture remap = model.GetRemapFrame(facing, RampType.None, false);
        Rectangle bounds = ObjectRenderer<T>.GetTextureDrawCoords(gameObject, regular, offset);

        return new Frame(regular, remap, null, bounds, byte.MaxValue);
    }

    private Texture2D Composite(IReadOnlyList<Frame> frames, Rectangle bounds, RenderTarget2D target, bool remap)
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

            // The current layer attenuates any remap pixels underneath it.
            graphicsDevice.BlendState = remap ? eraseRemap : BlendState.AlphaBlend;
            Draw(frame.Regular, destination, frame.Palette, false, unlitColor with { A = frame.Opacity });

            // SHP frames store the remap pixels separately from the regular pixels.
            if (frame.Remap != null && (remap || frame.Palette != null))
            {
                graphicsDevice.BlendState = BlendState.AlphaBlend;
                bool palettedRemap = remap && frame.Palette != null;
                Color color = palettedRemap ? Color.White : unlitColor;
                Draw(frame.Remap, destination, frame.Palette, palettedRemap, color with { A = frame.Opacity });
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
