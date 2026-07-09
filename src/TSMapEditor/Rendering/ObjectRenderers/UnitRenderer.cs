using System;
using Microsoft.Xna.Framework;
using TSMapEditor.CCEngine;
using TSMapEditor.CCEngine.TileData;
using TSMapEditor.GameMath;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    public sealed class UnitRenderer : ObjectRenderer<Unit>
    {
        public UnitRenderer(RenderDependencies renderDependencies) : base(renderDependencies)
        {
        }

        protected override Color ReplacementColor => Color.Red;

        protected override CommonDrawParams GetDrawParams(Unit gameObject)
        {
            return new CommonDrawParams()
            {
                IniName = gameObject.ObjectType.ININame,
                ShapeImage = TheaterGraphics.UnitTextures[gameObject.ObjectType.Index],
                MainVoxel = TheaterGraphics.UnitModels[gameObject.ObjectType.Index],
                TurretVoxel = TheaterGraphics.UnitTurretModels[gameObject.ObjectType.Index],
                BarrelVoxel = TheaterGraphics.UnitBarrelModels[gameObject.ObjectType.Index]
            };
        }

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
                return ((height / (float)Map.HeightInPixelsWithCellHeight) * Constants.DownwardsDepthRenderSpace) + (4 * Constants.DepthRenderStep) + Constants.DepthEpsilon * ObjectDepthAdjustments.Vehicle;
            }

            return Constants.DepthEpsilon * ObjectDepthAdjustments.Vehicle;
        }

        protected override double GetExtraLight(Unit gameObject) => Map.Rules.ExtraUnitLight;

        protected override void Render(Unit gameObject, Point2D drawPoint, in CommonDrawParams drawParams)
        {
            bool affectedByLighting = RenderDependencies.EditorState.IsLighting;

            if (gameObject.UnitType.ArtConfig.Voxel)
            {
                RenderVoxelModel(gameObject, drawPoint, drawParams.MainVoxel, affectedByLighting, 0);
            }
            else
            {
                RenderMainShape(gameObject, drawPoint, drawParams, 0);
            }

            if (gameObject.UnitType.Turret)
            {
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

                if (gameObject.Facing is > facingStartDrawAbove and <= facingEndDrawAbove)
                {
                    if (gameObject.UnitType.ArtConfig.Voxel)
                        RenderVoxelModel(gameObject, drawPoint + turretOffset, drawParams.TurretVoxel, affectedByLighting, Constants.DepthEpsilon);
                    else
                        RenderTurretShape(gameObject, drawPoint, drawParams, Constants.DepthEpsilon);

                    RenderVoxelModel(gameObject, drawPoint + turretOffset, drawParams.BarrelVoxel, affectedByLighting, Constants.DepthEpsilon * ObjectDepthAdjustments.Turret);
                }
                else
                {
                    RenderVoxelModel(gameObject, drawPoint + turretOffset, drawParams.BarrelVoxel, affectedByLighting, Constants.DepthEpsilon);

                    if (gameObject.UnitType.ArtConfig.Voxel)
                        RenderVoxelModel(gameObject, drawPoint + turretOffset, drawParams.TurretVoxel, affectedByLighting, Constants.DepthEpsilon * ObjectDepthAdjustments.Turret);
                    else
                        RenderTurretShape(gameObject,  drawPoint, drawParams, Constants.DepthEpsilon * ObjectDepthAdjustments.Turret);
                }
            }

            if (gameObject.Veterancy > 0)
            {
                DrawShapeImage(gameObject, TheaterGraphics.PipTextures, TheaterGraphics.GetVeterancyFrame(gameObject.Veterancy),
                    Color.White, false, Color.White, false, false,
                    drawPoint + new Point2D(Constants.CellSizeX / 4, Constants.CellSizeY / 4),
                    100.0f);
            }
        }

        private void RenderMainShape(Unit gameObject, Point2D drawPoint, CommonDrawParams drawParams, float depthAddition)
        {
            if (!gameObject.ObjectType.NoShadow)
                DrawShadow(gameObject);

            DrawShapeImage(gameObject, drawParams.ShapeImage, 
                gameObject.GetFrameIndex(drawParams.ShapeImage.GetFrameCount()),
                Color.White, true, gameObject.GetRemapColor(),
                false, true, drawPoint, depthAddition);
        }

        private void RenderTurretShape(Unit gameObject, Point2D drawPoint,
            CommonDrawParams drawParams, float depthAddition)
        {
            int turretFrameIndex = gameObject.GetTurretFrameIndex();

            if (turretFrameIndex > -1 && turretFrameIndex < drawParams.ShapeImage.GetFrameCount())
            {
                PositionedTexture frame = drawParams.ShapeImage.GetFrame(turretFrameIndex);

                if (frame == null)
                    return;

                DrawShapeImage(gameObject, drawParams.ShapeImage,
                    turretFrameIndex, Color.White, true, gameObject.GetRemapColor(),
                    false, true, drawPoint, depthAddition);
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
}
