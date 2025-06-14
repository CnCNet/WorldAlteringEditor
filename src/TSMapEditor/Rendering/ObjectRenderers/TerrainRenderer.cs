using Microsoft.Xna.Framework;
using TSMapEditor.GameMath;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    public sealed class TerrainRenderer : ObjectRenderer<TerrainObject>
    {
        public TerrainRenderer(RenderDependencies renderDependencies) : base(renderDependencies)
        {
        }

        protected override Color ReplacementColor => Color.Green;

        protected override CommonDrawParams GetDrawParams(TerrainObject gameObject)
        {
            return new CommonDrawParams()
            {
                IniName = gameObject.TerrainType.ININame,
                ShapeImage = TheaterGraphics.TerrainObjectTextures[gameObject.TerrainType.Index]
            };
        }

        protected override float GetDepthFromPosition(TerrainObject gameObject, Rectangle drawingBounds)
        {
            // Terrain objects are not meant to graphically leak to cells below themselves,
            // unless specifically configured to (which is handled in GetSouthernmostCell).
            // We override this method to prevent the default behaviour.
            var southernmostCell = GetSouthernmostCell(gameObject);

            int height = 0;
            if (southernmostCell != null)
            {
                height = southernmostCell.Level;
            }

            return ((CellMath.CellTopLeftPointFromCellCoords(southernmostCell.CoordsToPoint(), Map).Y + Constants.CellSizeY) / (float)Map.HeightInPixelsWithCellHeight) * Constants.DownwardsDepthRenderSpace +
                (height * Constants.DepthRenderStep);
        }

        protected override float GetDepthAddition(TerrainObject gameObject)
        {
            return Constants.DepthEpsilon * ObjectDepthAdjustments.Terrain;
        }

        protected override void Render(TerrainObject gameObject, Point2D drawPoint, in CommonDrawParams drawParams)
        {
            bool affectedByLighting = RenderDependencies.EditorState.IsLighting;

            // We need increased depth for very tall trees so they are reliably drawn over
            // objects behind them despite lack of required precision in the depth buffer
            //float depthOverride = -1f;
            //if (drawParams.ShapeImage != null)
            //{
            //    var frame = drawParams.ShapeImage.GetFrame(0);
            //    if (frame != null && frame.Texture != null)
            //    {
            //        var textureDrawCoords = GetTextureDrawCoords(gameObject, frame, drawPoint);
            //        depthOverride = GetDepth(gameObject, textureDrawCoords.Bottom) + ((textureDrawCoords.Height / Constants.CellHeight) * Constants.DepthRenderStep);
            //    }
            //}

            DrawShadow(gameObject);
            DrawShapeImage(gameObject, drawParams.ShapeImage, 0,
                Color.White, false, Color.White, affectedByLighting, !drawParams.ShapeImage.SubjectToLighting, drawPoint);
        }
    }
}
