using System;
using MapEditorLibrary;
using MapEditorLibrary.CCEngine;
using MapEditorLibrary.CCEngine.TileData;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Graphics;
using MapEditorLibrary.Models;
using Microsoft.Xna.Framework;

namespace TSMapEditor.Rendering.ObjectRenderers;

public sealed class UnitRenderer : ObjectRenderer<Unit>
{
    public UnitRenderer(RenderDependencies renderDependencies) : base(renderDependencies)
    {
    }

    protected override Color ReplacementColor => Color.Red;

    protected override CommonDrawParams GetDrawParams(Unit gameObject) => TheaterGraphics.GetUnitDrawParams(gameObject.UnitType);

    private DepthRectangle cachedDepth;

    public override void InitDrawForObject(Unit gameObject)
    {
        cachedDepth = new DepthRectangle(-1f, -1f);
    }

    protected override DepthRectangle GetDepthFromPosition(Unit gameObject, Rectangle drawingBounds)
    {
        // Because units layer multiple sprites on top of each other and we want them to have a fixed layering order,
        // we need a custom implementation where the highest depth rendered so far is recorded.

        var cell = Map.GetTile(gameObject.Position);
        int y = drawingBounds.Y;
        int bottom = drawingBounds.Bottom;
        int yReference = CellMath.CellBottomPointFromCellCoords(gameObject.Position, Map);
        if (cell != null && !RenderDependencies.EditorState.Is2DMode)
        {
            y += cell.Level * Constants.CellHeight;
            bottom += cell.Level * Constants.CellHeight;
        }

        float depthTop = Math.Max(CellMath.GetDepthForPixel(y, yReference, cell, Map), cachedDepth.TopLeft);
        float depthBottom = Math.Max(CellMath.GetDepthForPixel(bottom, yReference, cell, Map), cachedDepth.BottomLeft);
        float max = Math.Max(depthTop, depthBottom);

        // The unit's body is always drawn first, and at that point cachedDepth is -1f.
        // For the shadow and body, use the depth values computed here, otherwise use the largest depth recorded so far.
        if (cachedDepth.TopLeft < max)
        {
            cachedDepth = new DepthRectangle(max);
            return new DepthRectangle(depthTop, depthBottom);
        }

        return cachedDepth;
    }

    protected override float GetDepthAddition(Unit gameObject)
    {
        if (gameObject.High)
        {
            // Add extra depth to the unit so it is rendered above the bridge.
            // Why are we adding exactly this much?
            // Because it happened to work - this is at least currently no smart mathematical formula.
            int height = Constants.CellSizeY * 7;
            return ((height / Map.DepthRenderRange) * Constants.DownwardsDepthRenderSpace) + (4 * Constants.DepthRenderStep) + Constants.DepthEpsilon * ObjectDepthAdjustments.Vehicle;
        }

        return Constants.DepthEpsilon * ObjectDepthAdjustments.Vehicle;
    }

    protected override double GetExtraLight(Unit gameObject) => Map.Rules.ExtraUnitLight;

    public readonly record struct Layer(ShapeImage Shape, int FrameIndex, VoxelModel Voxel, Point2D Offset);

    public static (Layer Body, Layer Middle, Layer Top) GetLayers(Unit gameObject, in CommonDrawParams drawParams)
    {
        bool isVoxel = gameObject.UnitType.ArtConfig.Voxel;
        ShapeImage shape = drawParams.ShapeImage;
        var body = isVoxel
            ? new Layer(null, 0, drawParams.MainVoxel, Point2D.Zero)
            : new Layer(shape, shape == null ? 0 : gameObject.GetFrameIndex(shape.GetFrameCount()), null, Point2D.Zero);

        if (!gameObject.UnitType.Turret)
            return (body, default, default);

        const byte facingStartDrawAbove = (byte)Direction.E * 32;
        const byte facingEndDrawAbove = (byte)Direction.W * 32;

        byte facing = Convert.ToByte(Math.Clamp(
            Math.Round((float)gameObject.Facing / 8, MidpointRounding.AwayFromZero) * 8,
            byte.MinValue,
            byte.MaxValue));

        float rotationFromFacing = 2 * (float)Math.PI * ((float)facing / Constants.FacingMax);
        Vector2 leptonTurretOffset = new Vector2(0, -gameObject.UnitType.ArtConfig.TurretOffset);
        leptonTurretOffset = Vector2.Transform(leptonTurretOffset, Matrix.CreateRotationZ(rotationFromFacing));

        Point2D turretOffset = Helpers.ScreenCoordsFromWorldLeptons(leptonTurretOffset);
        var turret = isVoxel || shape == null
            ? new Layer(null, 0, drawParams.TurretVoxel, turretOffset)
            : new Layer(shape, gameObject.GetTurretFrameIndex(), null, Point2D.Zero);
        var barrel = new Layer(null, 0, drawParams.BarrelVoxel, turretOffset);

        return gameObject.Facing is > facingStartDrawAbove and <= facingEndDrawAbove
            ? (body, turret, barrel)
            : (body, barrel, turret);
    }

    protected override void Render(Unit gameObject, Point2D drawPoint, in CommonDrawParams drawParams)
    {
        var (body, middle, top) = GetLayers(gameObject, drawParams);

        if (body.Shape != null && !gameObject.ObjectType.NoShadow)
            DrawShadow(gameObject);

        DrawLayer(gameObject, drawPoint, body, 0);
        DrawLayer(gameObject, drawPoint, middle, Constants.DepthEpsilon);
        DrawLayer(gameObject, drawPoint, top, Constants.DepthEpsilon * ObjectDepthAdjustments.Turret);

        if (gameObject.Veterancy > 0)
        {
            DrawShapeImage(gameObject, TheaterGraphics.PipTextures, TheaterGraphics.GetVeterancyFrame(gameObject.Veterancy),
                Color.White, false, Color.White, false, false,
                drawPoint + new Point2D(Constants.CellSizeX / 4, Constants.CellSizeY / 4),
                100.0f);
        }
    }

    private void DrawLayer(Unit gameObject, Point2D drawPoint, in Layer layer, float depthAddition)
    {
        Point2D layerDrawPoint = drawPoint + layer.Offset;
        if (layer.Shape != null)
        {
            DrawShapeImage(gameObject, layer.Shape, layer.FrameIndex, Color.White, true, gameObject.GetRemapColor(),
                false, true, layerDrawPoint, depthAddition);
        }
        else if (layer.Voxel != null)
        {
            RenderVoxelModel(gameObject, layerDrawPoint, layer.Voxel, RenderDependencies.EditorState.IsLighting, depthAddition);
        }
    }

    private void RenderVoxelModel(Unit gameObject, Point2D drawPoint, 
        VoxelModel model, bool affectedByLighting, float depthAddition)
    {
        var unitTile = Map.GetTile(gameObject.Position.X, gameObject.Position.Y);

        if (unitTile == null)
            return;

        ITileImage tile = Map.TheaterInstance.GetTile(unitTile.TileIndex);
        ISubTileImage subTile = tile.GetSubTile(unitTile.SubTileIndex);
        RampType ramp = subTile.TmpImage.RampType;

        DrawVoxelModel(gameObject, model,
            gameObject.Facing, ramp, Color.White, true, gameObject.GetRemapColor(),
            affectedByLighting, drawPoint, depthAddition);
    }
}
