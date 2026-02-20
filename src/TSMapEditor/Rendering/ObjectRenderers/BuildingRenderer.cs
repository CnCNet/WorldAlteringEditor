using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Models;

namespace TSMapEditor.Rendering.ObjectRenderers
{
    public sealed class BuildingRenderer : ObjectRenderer<Structure>
    {
        public BuildingRenderer(RenderDependencies renderDependencies) : base(renderDependencies)
        {
        }

        protected override Color ReplacementColor => Color.Yellow;

        private List<Animation> animationList = new List<Animation>();

        private DepthRectangle cachedDepth;

        private bool isDrawingBib = false;

        public override void InitDrawForObject(Structure gameObject)
        {
            cachedDepth = new DepthRectangle(-1f, -1f);
        }

        public Point2D GetBuildingCenterPoint(Structure structure)
        {
            Point2D topPoint = CellMath.CellCenterPointFromCellCoords(structure.Position, Map);
            var foundation = structure.ObjectType.ArtConfig.Foundation;
            Point2D bottomPoint = CellMath.CellCenterPointFromCellCoords(structure.Position + new Point2D(foundation.Width - 1, foundation.Height - 1), Map);
            return topPoint + new Point2D((bottomPoint.X - topPoint.X) / 2, (bottomPoint.Y - topPoint.Y) / 2);
        }

        public void DrawFoundationLines(Structure gameObject)
        {
            int foundationX = gameObject.ObjectType.ArtConfig.Foundation.Width;
            int foundationY = gameObject.ObjectType.ArtConfig.Foundation.Height;

            Color foundationLineColor = gameObject.Owner.XNAColor;
            if (gameObject.IsBaseNodeDummy)
                foundationLineColor *= 0.25f;

            if (foundationX == 0 || foundationY == 0)
                return;

            int heightOffset = 0;

            var cell = Map.GetTile(gameObject.Position);
            if (cell != null && !RenderDependencies.EditorState.Is2DMode)
                heightOffset = cell.Level * Constants.CellHeight;

            foundationLineColor = new Color((foundationLineColor.R / 255.0f) * (float)cell.CellLighting.R,
                (foundationLineColor.G / 255.0f) * (float)cell.CellLighting.G,
                (foundationLineColor.B / 255.0f) * (float)cell.CellLighting.B,
                0.5f);

            foreach (var edge in gameObject.ObjectType.ArtConfig.Foundation.Edges)
            {
                // Translate edge vertices from cell coordinate space to world coordinate space.
                var start = CellMath.CellTopLeftPointFromCellCoords(gameObject.Position + edge[0], Map);
                var end = CellMath.CellTopLeftPointFromCellCoords(gameObject.Position + edge[1], Map);

                float depth = GetFoundationLineDepth(gameObject, start, end);
                // Height is an illusion, just move everything up or down.
                // Also offset X to match the top corner of an iso tile.
                start += new Point2D(Constants.CellSizeX / 2, -heightOffset);
                end += new Point2D(Constants.CellSizeX / 2, -heightOffset);
                // Draw edge.
                RenderDependencies.ObjectSpriteRecord.AddLineEntry(new LineEntry(start.ToXNAVector(), end.ToXNAVector(), foundationLineColor, 1, depth));
            }
        }

        private float GetFoundationLineDepth(Structure gameObject, Point2D startPoint, Point2D endPoint)
        {
            Point2D lowerPoint = startPoint.Y > endPoint.Y ? startPoint : endPoint;
            return CellMath.GetDepthForPixel(lowerPoint.Y, gameObject.Position, Map);
        }

        protected override CommonDrawParams GetDrawParams(Structure gameObject)
        {
            string iniName = gameObject.ObjectType.ININame;

            return new CommonDrawParams()
            {
                IniName = iniName,
                ShapeImage = RenderDependencies.TheaterGraphics.BuildingTextures[gameObject.ObjectType.Index],
                TurretVoxel = RenderDependencies.TheaterGraphics.BuildingTurretModels[gameObject.ObjectType.Index],
                BarrelVoxel = RenderDependencies.TheaterGraphics.BuildingBarrelModels[gameObject.ObjectType.Index]
            };
        }

        protected override bool ShouldRenderReplacementText(Structure gameObject)
        {
            var bibGraphics = RenderDependencies.TheaterGraphics.BuildingBibTextures[gameObject.ObjectType.Index];

            if (bibGraphics != null)
                return false;

            if (gameObject.TurretAnim != null)
                return false;

            if (gameObject.Anims.Length > 0)
                return false;

            return base.ShouldRenderReplacementText(gameObject);
        }

        private float GetFoundationCenterXPoint(Structure gameObject)
        {
            var foundation = gameObject.ObjectType.ArtConfig.Foundation;
            if (foundation.Width == 0 || foundation.Height == 0)
                return 1.0f;

            return (float)foundation.Width / (foundation.Width + foundation.Height);
        }

        private DepthRectangle GetDepthForAnimation(Structure gameObject, Rectangle drawingBounds, bool isBelowBuilding)
        {
            float foundationCenterXPoint = GetFoundationCenterXPoint(gameObject);
            int distRight = (int)(drawingBounds.Width * (1.0f - foundationCenterXPoint));

            var southernmostFoundationCellCoords = gameObject.GetSouthernmostFoundationCell();
            var heightReferenceCell = Map.GetTile(gameObject.Position);

            // drawingBounds includes effect of height, which is undesirable for depth rendering
            int y = drawingBounds.Y;
            int bottom = drawingBounds.Bottom;

            if (heightReferenceCell != null && !RenderDependencies.EditorState.Is2DMode)
            {
                y += heightReferenceCell.Level * Constants.CellHeight;
                bottom += heightReferenceCell.Level * Constants.CellHeight;
            }

            int yReference;
            if (isBelowBuilding)
                yReference = CellMath.CellTopLeftPointFromCellCoords(gameObject.Position, Map).Y;
            else
                yReference = CellMath.CellBottomPointFromCellCoords(southernmostFoundationCellCoords, Map);

            float topDepth = CellMath.GetDepthForPixelInCube(y, 0, yReference, heightReferenceCell, Map);
            float bottomDepth = CellMath.GetDepthForPixelInCube(bottom, 0, yReference, heightReferenceCell, Map);

            return new DepthRectangle(topDepth, bottomDepth);
        }

        protected override DepthRectangle GetShadowDepthFromPosition(Structure gameObject, Rectangle drawingBounds)
        {
            // The default behaviour for shadows is to call GetDepthFromPosition,
            // but the adjusted behaviour of GetDepthFromPosition intended for the rendering
            // of turrets and other on-top-of-the-building objects results in shadows
            // being too close to the "camera".

            // This implementation fixes the issue by calculating depth from the building's
            // lowest pixel (at the building's base), not from its highest pixel.

            var southernmostFoundationCellCoords = gameObject.GetSouthernmostFoundationCell();
            var heightReferenceCell = Map.GetTile(gameObject.Position);

            // drawingBounds includes effect of height, which is undesirable for depth rendering
            int bottom = drawingBounds.Bottom;

            if (heightReferenceCell != null && !RenderDependencies.EditorState.Is2DMode)
            {
                bottom += heightReferenceCell.Level * Constants.CellHeight;
            }

            int yReference = CellMath.CellBottomPointFromCellCoords(southernmostFoundationCellCoords, Map);

            float depthAtBottom = CellMath.GetDepthForPixelInCube(bottom, 0, yReference, heightReferenceCell, Map);
            return new DepthRectangle(depthAtBottom, depthAtBottom);
        }

        protected override DepthRectangle GetDepthFromPosition(Structure gameObject, Rectangle drawingBounds)
        {
            // Because buildings have a customized depth implementation and can layer several
            // sprites on top of each other, the default implementation
            // is not suitable. For example, bodies can be larger than turrets,
            // leading the bodies to have higher depth and overlapping turrets.
            //
            // To fix this, we normalize everything to use the maximum depth.

            var southernmostFoundationCellCoords = gameObject.GetSouthernmostFoundationCell();
            var heightReferenceCell = Map.GetTile(gameObject.Position);

            // drawingBounds includes effect of height, which is undesirable for depth rendering
            int y = drawingBounds.Y;
            int bottom = drawingBounds.Bottom;

            if (heightReferenceCell != null && !RenderDependencies.EditorState.Is2DMode)
            {
                y += heightReferenceCell.Level * Constants.CellHeight;
                bottom += heightReferenceCell.Level * Constants.CellHeight;
            }

            if (isDrawingBib)
            {
                int topYReference = CellMath.CellTopLeftPointFromCellCoords(gameObject.Position, Map).Y;
                float topDepth = CellMath.GetDepthForPixelInCube(y, 0, topYReference, heightReferenceCell, Map);
                float bottomDepth = CellMath.GetDepthForPixelInCube(bottom, 0, topYReference, heightReferenceCell, Map);
                return new DepthRectangle(topDepth, bottomDepth);
            }
            else
            {
                int yReference = CellMath.CellBottomPointFromCellCoords(southernmostFoundationCellCoords, Map);

                // Used for drawing turrets and stuff, just return maximum depth since they must be on top of the building
                float maxDepth = CellMath.GetDepthForPixelInCube(y, 0, yReference, heightReferenceCell, Map);

                if (maxDepth < cachedDepth.TopLeft)
                    return cachedDepth;

                cachedDepth = new DepthRectangle(maxDepth);

                return cachedDepth;
            }
        }

        private (DepthRectangle depthRectangle, Rectangle sourceRect) GetLeftDepthRectangle(Structure gameObject, PositionedTexture texture, Rectangle drawingBounds)
        {
            float foundationCenterXPoint = GetFoundationCenterXPoint(gameObject);
            int distLeft = (int)(drawingBounds.Width * foundationCenterXPoint);

            var southernmostFoundationCellCoords = gameObject.GetSouthernmostFoundationCell();
            var heightReferenceCell = Map.GetTile(gameObject.Position);

            // drawingBounds includes effect of height, which is undesirable for depth rendering
            int y = drawingBounds.Y;
            int bottom = drawingBounds.Bottom;

            if (heightReferenceCell != null && !RenderDependencies.EditorState.Is2DMode)
            {
                y += heightReferenceCell.Level * Constants.CellHeight;
                bottom += heightReferenceCell.Level * Constants.CellHeight;
            }

            int yReference = CellMath.CellBottomPointFromCellCoords(southernmostFoundationCellCoords, Map);

            float depthTopLeft = CellMath.GetDepthForPixelInCube(y, distLeft, yReference, heightReferenceCell, Map);
            float depthTopRight = CellMath.GetDepthForPixelInCube(y, 0, yReference, heightReferenceCell, Map);
            float depthBottomLeft = CellMath.GetDepthForPixelInCube(bottom, distLeft, yReference, heightReferenceCell, Map);
            float depthBottomRight = CellMath.GetDepthForPixelInCube(bottom, 0, yReference, heightReferenceCell, Map);

            // Debug.Assert(depthTopLeft <= depthTopRight);
            // Debug.Assert(depthBottomLeft <= depthBottomRight);
            // Debug.Assert(depthTopLeft >= depthBottomLeft);
            // Debug.Assert(depthTopRight >= depthBottomRight);

            var depthRectangle = new DepthRectangle(depthTopLeft, depthTopRight, depthBottomLeft, depthBottomRight);
            var sourceRect = new Rectangle(texture.SourceRectangle.X, texture.SourceRectangle.Y, distLeft, texture.SourceRectangle.Height);

            return (depthRectangle, sourceRect);
        }

        private (DepthRectangle depthRectangle, Rectangle sourceRect) GetRightDepthRectangle(Structure gameObject, PositionedTexture texture, Rectangle drawingBounds)
        {
            float foundationCenterXPoint = GetFoundationCenterXPoint(gameObject);
            int distRight = (int)(drawingBounds.Width * (1.0f - foundationCenterXPoint));

            var southernmostFoundationCellCoords = gameObject.GetSouthernmostFoundationCell();
            var heightReferenceCell = Map.GetTile(gameObject.Position);

            // drawingBounds includes effect of height, which is undesirable for depth rendering
            int y = drawingBounds.Y;
            int bottom = drawingBounds.Bottom;

            if (heightReferenceCell != null && !RenderDependencies.EditorState.Is2DMode)
            {
                y += heightReferenceCell.Level * Constants.CellHeight;
                bottom += heightReferenceCell.Level * Constants.CellHeight;
            }

            int yReference = CellMath.CellBottomPointFromCellCoords(southernmostFoundationCellCoords, Map);

            float depthTopLeft = CellMath.GetDepthForPixelInCube(y, 0, yReference, heightReferenceCell, Map);
            float depthTopRight = CellMath.GetDepthForPixelInCube(y, distRight, yReference, heightReferenceCell, Map);
            float depthBottomLeft = CellMath.GetDepthForPixelInCube(bottom, 0, yReference, heightReferenceCell, Map);
            float depthBottomRight = CellMath.GetDepthForPixelInCube(bottom, distRight, yReference, heightReferenceCell, Map);

            // Debug.Assert(depthTopLeft >= depthTopRight);
            // Debug.Assert(depthBottomLeft >= depthBottomRight);
            // Debug.Assert(depthTopLeft >= depthBottomLeft);
            // Debug.Assert(depthTopRight >= depthBottomRight);

            var depthRectangle = new DepthRectangle(depthTopLeft, depthTopRight, depthBottomLeft, depthBottomRight);
            var sourceRect = new Rectangle(texture.SourceRectangle.Right - distRight, texture.SourceRectangle.Y, distRight, texture.SourceRectangle.Height);

            if (depthTopLeft > cachedDepth.TopLeft)
            {
                cachedDepth = new DepthRectangle(depthTopLeft);
            }

            return (depthRectangle, sourceRect);
        }

        protected override float GetDepthAddition(Structure gameObject)
        {
            float buildingAdditionalDepth = gameObject.ObjectType.EditorZAdjust / (float)Map.HeightInPixelsWithCellHeight;
            return buildingAdditionalDepth + (Constants.DepthEpsilon * ObjectDepthAdjustments.Building);
        }

        private void DrawBibGraphics(Structure gameObject, ShapeImage bibGraphics, Point2D drawPoint, in CommonDrawParams drawParams, bool affectedByLighting)
        {
            isDrawingBib = true;
            DrawShapeImage(gameObject, bibGraphics, 0, Color.White, true, gameObject.GetRemapColor(),
                affectedByLighting, !drawParams.ShapeImage.SubjectToLighting, drawPoint);
            isDrawingBib = false;
        }

        protected override void Render(Structure gameObject, Point2D drawPoint, in CommonDrawParams drawParams)
        {
            bool affectedByLighting = RenderDependencies.EditorState.IsLighting && (drawParams.ShapeImage != null && drawParams.ShapeImage.SubjectToLighting);

            // Bib is on the ground, gets grawn first
            var bibGraphics = RenderDependencies.TheaterGraphics.BuildingBibTextures[gameObject.ObjectType.Index];
            if (bibGraphics != null)
                DrawBibGraphics(gameObject, bibGraphics, drawPoint, drawParams, affectedByLighting);

            Color nonRemapColor = gameObject.IsBaseNodeDummy ? new Color(150, 150, 255) * 0.5f : Color.White;

            int frameCount = drawParams.ShapeImage == null ? 0 : drawParams.ShapeImage.GetFrameCount();
            int frameIndex = gameObject.GetFrameIndex(frameCount);

            float depthAddition = Constants.DepthEpsilon * ObjectDepthAdjustments.Building;

            // Form the anims list
            animationList.Clear();
            animationList.AddRange(gameObject.Anims);
            animationList.AddRange(gameObject.PowerUpAnims);
            if (gameObject.TurretAnim != null)
                animationList.Add(gameObject.TurretAnim);

            // Sort the anims according to their settings
            animationList.Sort((anim1, anim2) =>
                anim1.BuildingAnimDrawConfig.SortValue(anim1.AnimType.ArtConfig.YSortAdjust).CompareTo(anim2.BuildingAnimDrawConfig.SortValue(anim2.AnimType.ArtConfig.YSortAdjust)));

            bool affectedByAmbient = !affectedByLighting;

            // The building itself has an offset of 0, so first draw all anims with sort values < 0
            for (int i = 0; i < animationList.Count; i++)
            {
                var anim = animationList[i];

                if (anim.BuildingAnimDrawConfig.SortValue(anim.AnimType.ArtConfig.YSortAdjust) < 0)
                {
                    var animShape = TheaterGraphics.AnimTextures[anim.AnimType.Index];
                    if (animShape != null)
                    {
                        DrawAnimationImage(gameObject, anim, animShape, anim.GetFrameIndex(animShape.GetFrameCount()),
                            nonRemapColor, true, gameObject.GetRemapColor(), affectedByLighting, affectedByAmbient,
                            drawPoint, Constants.DepthEpsilon, true);
                    }
                }
            }

            // Then the building itself
            if (!gameObject.ObjectType.NoShadow)
                DrawShadow(gameObject);

            var foundation = gameObject.ObjectType.ArtConfig.Foundation;
            float foundationCenterXPoint = (float)foundation.Width / (foundation.Width + foundation.Height);

            DrawBuildingImage(gameObject, drawParams.ShapeImage,
                gameObject.GetFrameIndex(frameCount),
                nonRemapColor, true, gameObject.GetRemapColor(),
                affectedByLighting, affectedByAmbient, drawPoint, 0);

            // Draw foundation lines - by drawing them this late we are able to reuse the depth parameters in them
            if (RenderDependencies.EditorState.RenderInvisibleInGameObjects)
                DrawFoundationLines(gameObject);

            // Then draw all anims with sort values >= 0
            for (int i = 0; i < animationList.Count; i++)
            {
                var anim = animationList[i];

                if (anim.BuildingAnimDrawConfig.SortValue(anim.AnimType.ArtConfig.YSortAdjust) >= 0)
                {
                    // It gets challenging to handle depth if the anim renderer draws anims that are "above" the building, 
                    // as it does not have proper context of the building it's drawing on.
                    // Draw the anim here instead, like it was a part of the building.
                    var animShape = TheaterGraphics.AnimTextures[anim.AnimType.Index];

                    if (animShape != null)
                    {
                        DrawAnimationImage(gameObject, anim, animShape, anim.GetFrameIndex(animShape.GetFrameCount()),
                            nonRemapColor, true, gameObject.GetRemapColor(), affectedByLighting, affectedByAmbient,
                            drawPoint, 0, false);
                    }
                }
            }

            DrawVoxelTurret(gameObject, drawPoint, drawParams, nonRemapColor, affectedByLighting);

            if (gameObject.ObjectType.HasSpotlight)
            {
                Point2D cellCenter = RenderDependencies.EditorState.Is2DMode ?
                    CellMath.CellTopLeftPointFromCellCoords(gameObject.Position, Map) :
                    CellMath.CellTopLeftPointFromCellCoords_3D(gameObject.Position, Map);

                DrawObjectFacingArrow(gameObject.Facing, cellCenter);
            }
        }

        private void DrawBuildingImage(Structure gameObject, ShapeImage image, int frameIndex, Color color,
            bool drawRemap, Color remapColor, bool affectedByLighting, bool affectedByAmbient, Point2D drawPoint,
            float depthAddition = 0f)
        {
            if (image == null)
                return;

            PositionedTexture frame = image.GetFrame(frameIndex);
            if (frame == null || frame.Texture == null)
                return;

            PositionedTexture remapFrame = null;
            if (drawRemap && image.HasRemapFrames())
                remapFrame = image.GetRemapFrame(frameIndex);

            Rectangle drawingBounds = GetTextureDrawCoords(gameObject, frame, drawPoint);

            double extraLight = GetExtraLight(gameObject);

            Vector4 lighting = Vector4.One;
            var mapCell = Map.GetTile(gameObject.Position);

            if (RenderDependencies.EditorState.IsLighting && mapCell != null)
            {
                if (affectedByLighting && image.SubjectToLighting)
                {
                    lighting = mapCell.CellLighting.ToXNAVector4(extraLight);
                    remapColor = ScaleColorToAmbient(remapColor, lighting);
                }
                else if (affectedByAmbient)
                {
                    lighting = mapCell.CellLighting.ToXNAVector4Ambient(extraLight);
                    remapColor = ScaleColorToAmbient(remapColor, lighting);
                }
            }

            // Below is modified RenderFrame because we need to split the building manually into two images

            var (depthRectangleLeft, sourceRectangleLeft) = GetLeftDepthRectangle(gameObject, frame, drawingBounds);
            depthRectangleLeft += depthAddition;
            depthRectangleLeft += GetDepthAddition(gameObject);

            var (depthRectangleRight, sourceRectangleRight) = GetRightDepthRectangle(gameObject, frame, drawingBounds);
            depthRectangleRight += depthAddition;
            depthRectangleRight += GetDepthAddition(gameObject);

            // Check for truncation due to integer-based source rectangle math
            // For example, a texture width of 91 pixels will give us two 45-pixel wide rectangles,
            // or an even-width texture like 104 might give us one uneven rectangle due to truncating
            // in source rectangle computation
            if (sourceRectangleLeft.Width + sourceRectangleRight.Width != drawingBounds.Width)
            {
                sourceRectangleRight.X = sourceRectangleRight.X - 1;
                sourceRectangleRight.Width = sourceRectangleRight.Width + 1;
            }

            color = new Color((color.R / 255.0f) * lighting.X / 2f,
                (color.B / 255.0f) * lighting.Y / 2f,
                (color.B / 255.0f) * lighting.Z / 2f, color.A);

            Rectangle drawingBoundsLeft = drawingBounds with { Width = sourceRectangleLeft.Width };
            Rectangle drawingBoundsRight = drawingBounds with { X = drawingBounds.X + sourceRectangleLeft.Width, Width = sourceRectangleRight.Width };

            RenderDependencies.ObjectSpriteRecord.AddGraphicsEntry(new ObjectSpriteEntry(image.GetPaletteTexture(), frame.Texture, sourceRectangleLeft, drawingBoundsLeft, color, false, false, depthRectangleLeft));
            RenderDependencies.ObjectSpriteRecord.AddGraphicsEntry(new ObjectSpriteEntry(image.GetPaletteTexture(), frame.Texture, sourceRectangleRight, drawingBoundsRight, color, false, false, depthRectangleRight));

            if (drawRemap && remapFrame != null)
            {
                Rectangle remapSourceRectangleLeft = sourceRectangleLeft with { X = remapFrame.SourceRectangle.X, Y = remapFrame.SourceRectangle.Y };
                Rectangle remapSourceRectangleRight = sourceRectangleRight with { X = remapFrame.SourceRectangle.X + sourceRectangleLeft.Width, Y = remapFrame.SourceRectangle.Y };

                RenderDependencies.ObjectSpriteRecord.AddGraphicsEntry(new ObjectSpriteEntry(image.GetPaletteTexture(), remapFrame.Texture, remapSourceRectangleLeft, drawingBoundsLeft, remapColor, true, false, depthRectangleLeft + Constants.DepthEpsilon));
                RenderDependencies.ObjectSpriteRecord.AddGraphicsEntry(new ObjectSpriteEntry(image.GetPaletteTexture(), remapFrame.Texture, remapSourceRectangleRight, drawingBoundsRight, remapColor, true, false, depthRectangleRight + Constants.DepthEpsilon));
            }
        }

        private void DrawAnimationImage(Structure gameObject, Animation animation, ShapeImage image, int frameIndex, Color color,
            bool drawRemap, Color remapColor, bool affectedByLighting, bool affectedByAmbient, Point2D drawPoint,
            float depthAddition, bool isBelowBuilding)
        {
            if (image == null)
                return;

            PositionedTexture frame = image.GetFrame(frameIndex);
            if (frame == null || frame.Texture == null)
                return;

            PositionedTexture remapFrame = null;
            if (drawRemap && image.HasRemapFrames())
                remapFrame = image.GetRemapFrame(frameIndex);

            Rectangle drawingBounds = GetTextureDrawCoords(animation, frame, drawPoint);

            double extraLight = GetExtraLight(gameObject);

            Vector4 lighting = Vector4.One;
            var mapCell = Map.GetTile(gameObject.Position);

            if (RenderDependencies.EditorState.IsLighting && mapCell != null)
            {
                if (affectedByLighting && image.SubjectToLighting)
                {
                    lighting = mapCell.CellLighting.ToXNAVector4(extraLight);
                    remapColor = ScaleColorToAmbient(remapColor, lighting);
                }
                else if (affectedByAmbient)
                {
                    lighting = mapCell.CellLighting.ToXNAVector4Ambient(extraLight);
                    remapColor = ScaleColorToAmbient(remapColor, lighting);
                }
            }

            var depthRectangle = GetDepthForAnimation(gameObject, drawingBounds, isBelowBuilding);
            depthRectangle += depthAddition;

            if (!isBelowBuilding)
                depthRectangle += GetDepthAddition(gameObject);

            switch (animation.AnimType.ArtConfig.Translucency)
            {
                default:
                case 0:
                    break;
                case 25:
                    color = color with { A = 192 };
                    break;
                case 50:
                    color = color with { A = 128 };
                    break;
                case 75:
                    color = color with { A = 64 };
                    break;
            }

            RenderFrame(gameObject, frame, remapFrame, color, drawRemap, remapColor,
                drawingBounds, image.GetPaletteTexture(), lighting, depthRectangle);
        }

        private void DrawVoxelTurret(Structure gameObject, Point2D drawPoint, in CommonDrawParams drawParams, Color nonRemapColor, bool affectedByLighting)
        {
            if (gameObject.ObjectType.Turret && gameObject.ObjectType.TurretAnimIsVoxel)
            {
                var turretOffset = new Point2D(gameObject.ObjectType.TurretAnimX, gameObject.ObjectType.TurretAnimY);
                var turretDrawPoint = drawPoint + turretOffset;

                const byte facingStartDrawAbove = (byte)Direction.E * 32;
                const byte facingEndDrawAbove = (byte)Direction.W * 32;

                if (gameObject.Facing is > facingStartDrawAbove and <= facingEndDrawAbove)
                {
                    DrawVoxelModel(gameObject, drawParams.TurretVoxel,
                        gameObject.Facing, RampType.None, nonRemapColor, true, gameObject.GetRemapColor(),
                        affectedByLighting, turretDrawPoint, Constants.DepthEpsilon);

                    DrawVoxelModel(gameObject, drawParams.BarrelVoxel,
                        gameObject.Facing, RampType.None, nonRemapColor, true, gameObject.GetRemapColor(),
                        affectedByLighting, turretDrawPoint, Constants.DepthEpsilon * ObjectDepthAdjustments.Turret);
                }
                else
                {
                    DrawVoxelModel(gameObject, drawParams.BarrelVoxel,
                        gameObject.Facing, RampType.None, nonRemapColor, true, gameObject.GetRemapColor(),
                        affectedByLighting, turretDrawPoint, -Constants.DepthEpsilon);

                    DrawVoxelModel(gameObject, drawParams.TurretVoxel,
                        gameObject.Facing, RampType.None, nonRemapColor, true, gameObject.GetRemapColor(),
                        affectedByLighting, turretDrawPoint, Constants.DepthEpsilon * ObjectDepthAdjustments.Turret); // Turret is always drawn above building
                }
            }
            else if (gameObject.ObjectType.Turret && !gameObject.ObjectType.TurretAnimIsVoxel &&
                     gameObject.ObjectType.BarrelAnimIsVoxel)
            {
                DrawVoxelModel(gameObject, drawParams.BarrelVoxel,
                    gameObject.Facing, RampType.None, nonRemapColor, true, gameObject.GetRemapColor(),
                    affectedByLighting, drawPoint, Constants.DepthEpsilon * ObjectDepthAdjustments.Turret);
            }
        }

        protected override void DrawObjectReplacementText(Structure gameObject, string text, Point2D drawPoint)
        {
            DrawFoundationLines(gameObject);

            base.DrawObjectReplacementText(gameObject, text, drawPoint);
        }
    }
}
