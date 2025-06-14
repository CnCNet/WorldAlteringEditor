using Microsoft.Xna.Framework;
using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    public sealed class OverlayRenderer : ObjectRenderer<Overlay>
    {
        public OverlayRenderer(RenderDependencies renderDependencies) : base(renderDependencies)
        {
        }

        protected override Color ReplacementColor => new Color(255, 0, 255);

        protected override CommonDrawParams GetDrawParams(Overlay gameObject)
        {
            return new CommonDrawParams()
            {
                IniName = gameObject.OverlayType.ININame,
                ShapeImage = TheaterGraphics.OverlayTextures[gameObject.OverlayType.Index]
            };
        }

        protected override double GetExtraLight(Overlay gameObject)
        {
            if (gameObject.OverlayType.HighBridgeDirection != BridgeDirection.None && RenderDependencies.EditorState.IsLighting)
            {
                double level = 0.0;

                switch (RenderDependencies.EditorState.LightingPreviewState)
                {
                    case LightingPreviewMode.Normal:
                        level = Map.Lighting.Level;
                        break;
                    case LightingPreviewMode.IonStorm:
                        level = Map.Lighting.IonLevel;
                        break;
                    case LightingPreviewMode.Dominator:
                        level = Map.Lighting.DominatorLevel.GetValueOrDefault();
                        break;
                    default:
                        throw new InvalidOperationException($"{nameof(OverlayRenderer)}.{nameof(GetExtraLight)}: Unknown lighting preview state");
                }

                const int highBridgeHeight = 4;
                return level * highBridgeHeight;
            }

            return 0.0;
        }

        public override Point2D GetDrawPoint(Overlay gameObject)
        {
            if (gameObject.OverlayType.HighBridgeDirection == BridgeDirection.None)
                return base.GetDrawPoint(gameObject);

            Point2D drawPointWithoutCellHeight = CellMath.CellTopLeftPointFromCellCoords(gameObject.Position, Map);

            var mapCell = Map.GetTile(gameObject.Position);
            int heightOffset = 0;

            if (!RenderDependencies.EditorState.Is2DMode)
            {
                heightOffset = mapCell.Level * Constants.CellHeight;

                if (gameObject.OverlayType.HighBridgeDirection == BridgeDirection.EastWest)
                {
                    heightOffset += Constants.CellHeight + 1;
                }
                else
                {
                    heightOffset += Constants.CellHeight * 2 + 1;
                }
            }

            Point2D drawPoint = new Point2D(drawPointWithoutCellHeight.X, drawPointWithoutCellHeight.Y - heightOffset);

            return drawPoint;
        }

        protected override float GetDepthAddition(Overlay gameObject)
        {
            if (gameObject.OverlayType.HighBridgeDirection == BridgeDirection.None)
            {
                // Draw overlays above smudges
                return Constants.DepthEpsilon * ObjectDepthAdjustments.Overlay;
            }

            const int bridgeHeight = 4;

            var tile = Map.GetTile(gameObject.Position);
            return (Constants.DepthEpsilon * ObjectDepthAdjustments.Overlay) + ((tile.Level + bridgeHeight) * Constants.CellHeight / (float)Map.HeightInPixelsWithCellHeight);
        }

        protected override void Render(Overlay gameObject, Point2D drawPoint, in CommonDrawParams drawParams)
        {
            Color remapColor = Color.White;
            if (gameObject.OverlayType.TiberiumType != null)
                remapColor = gameObject.OverlayType.TiberiumType.XNAColor;

            bool affectedByLighting = drawParams.ShapeImage.SubjectToLighting;
            bool affectedByAmbient = !gameObject.OverlayType.Tiberium && !affectedByLighting;

            DrawShadow(gameObject);
            DrawShapeImage(gameObject, drawParams.ShapeImage, gameObject.FrameIndex, Color.White,
                true, remapColor, affectedByLighting, affectedByAmbient, drawPoint);
        }
    }
}
