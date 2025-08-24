﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace TSMapEditor.Rendering.Batching
{
    /// <summary>
    /// Custom SpriteBatch replacement for more efficient rendering of terrain tiles.
    /// </summary>
    public class TerrainBatcher
    {
        private GraphicsDevice _graphicsDevice;
        private Effect _effect;

        /// <summary>
        /// Pool for created batches.
        /// Pooling these means that these big objects don't need to be reconstructed each time a batch is drawn.
        /// </summary>
        private Queue<TextureBatch<VertexPositionColorTexture>> inactiveBatches = new Queue<TextureBatch<VertexPositionColorTexture>>();

        /// <summary>
        /// Batches that are currently queued for drawing.
        /// </summary>
        private List<TextureBatch<VertexPositionColorTexture>> queuedBatches = new List<TextureBatch<VertexPositionColorTexture>>();

        private EffectParameter mainTextureParameter;
        private EffectParameter worldViewProjParameter;
        private DepthStencilState depthStencilState;

        private bool beginCalled = false;

        public TerrainBatcher(GraphicsDevice graphicsDevice, Effect effect, DepthStencilState depthStencilState)
        {
            _graphicsDevice = graphicsDevice;
            _effect = effect;
            this.depthStencilState = depthStencilState;

            mainTextureParameter = _effect.Parameters["MainTexture"];
            worldViewProjParameter = _effect.Parameters["WorldViewProj"];
        }

        public void Begin()
        {
            if (beginCalled)
                throw new InvalidOperationException("End needs to be called between two successive calls to Begin.");

            beginCalled = true;
        }

        public void Draw(Texture2D texture, Rectangle destinationRect, Rectangle? sourceRect, Color color, float depth)
        {
            Rectangle src = sourceRect ?? new Rectangle(0, 0, texture.Width, texture.Height);

            float u1 = (float)src.X / texture.Width;
            float v1 = (float)src.Y / texture.Height;
            float u2 = (float)(src.X + src.Width) / texture.Width;
            float v2 = (float)(src.Y + src.Height) / texture.Height;

            float x = destinationRect.X;
            float y = destinationRect.Y;
            float w = destinationRect.Width;
            float h = destinationRect.Height;

            TextureBatch<VertexPositionColorTexture> batch = null;
            for (int i = 0; i < queuedBatches.Count; i++)
            {
                if (queuedBatches[i].Texture == texture && !queuedBatches[i].IsFull())
                    batch = queuedBatches[i];
            }

            if (batch == null)
            {
                if (!inactiveBatches.TryDequeue(out batch))
                {
                    batch = new TextureBatch<VertexPositionColorTexture>();
                }

                queuedBatches.Add(batch);
                batch.Texture = texture;
            }

            int vertexOffset = batch.QuadCount * RenderingConstants.VerticesPerQuad;

            if (vertexOffset >= batch.Vertices.Length)
            {
                batch.ExtendArrays();
            }

            int indexOffset = batch.QuadCount * RenderingConstants.IndexesPerQuad;

            unsafe
            {
                // 4 vertices of the quad
                fixed (VertexPositionColorTexture* p = &batch.Vertices[vertexOffset])
                {
                    p[0] = new VertexPositionColorTexture { Position = new Vector3(x, y, depth), TextureCoordinate = new Vector2(u1, v1), Color = color };
                    p[1] = new VertexPositionColorTexture { Position = new Vector3(x + w, y, depth), TextureCoordinate = new Vector2(u2, v1), Color = color };
                    p[2] = new VertexPositionColorTexture { Position = new Vector3(x, y + h, depth), TextureCoordinate = new Vector2(u1, v2), Color = color };
                    p[3] = new VertexPositionColorTexture { Position = new Vector3(x + w, y + h, depth), TextureCoordinate = new Vector2(u2, v2), Color = color };
                }

                // 2 triangles (indices)
                fixed (short* p = &batch.Indexes[indexOffset])
                {
                    p[0] = (short)(vertexOffset + 0);
                    p[1] = (short)(vertexOffset + 1);
                    p[2] = (short)(vertexOffset + 2);

                    p[3] = (short)(vertexOffset + 2);
                    p[4] = (short)(vertexOffset + 1);
                    p[5] = (short)(vertexOffset + 3);
                }
            }

            batch.QuadCount++;
        }

        public void End()
        {
            // Build orthographic projection
            Matrix projection = Matrix.CreateOrthographicOffCenter(
                0, _graphicsDevice.Viewport.Width,
                _graphicsDevice.Viewport.Height, 0,
                0, -1);

            // Upload it to the shader
            worldViewProjParameter.SetValue(projection);

            for (int i = 0; i < queuedBatches.Count; i++)
            {
                var batch = queuedBatches[i];
                mainTextureParameter.SetValue(batch.Texture);

                foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    _graphicsDevice.DrawUserIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        batch.Vertices, 0, batch.QuadCount * RenderingConstants.VerticesPerQuad,
                        batch.Indexes, 0, batch.QuadCount * RenderingConstants.TrianglesPerQuad,
                        VertexPositionColorTexture.VertexDeclaration
                    );
                }
            }

            for (int i = 0; i < queuedBatches.Count; i++)
            {
                queuedBatches[i].Clear();
                inactiveBatches.Enqueue(queuedBatches[i]);
            }

            queuedBatches.Clear();
            beginCalled = false;
        }
    }
}
