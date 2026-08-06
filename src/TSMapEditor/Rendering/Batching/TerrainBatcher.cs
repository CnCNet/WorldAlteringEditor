using MapEditorLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TSMapEditor.Rendering.Batching;

/// <summary>
/// Custom SpriteBatch replacement for more efficient rendering of terrain tiles.
/// </summary>
public sealed class TerrainBatcher : AbstractBatcher<VertexPositionColorTexture>
{
    public TerrainBatcher(GraphicsDevice graphicsDevice, Effect effect, DepthStencilState depthStencilState) : base(graphicsDevice, effect)
    {
        this.depthStencilState = depthStencilState;
    }

    public void Draw(Texture2D texture, Rectangle destinationRect, Rectangle? sourceRect, Color color, float depthTop, float depthBottom)
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
                p[0] = new VertexPositionColorTexture { Position = new Vector3(x, y, depthTop), TextureCoordinate = new Vector2(u1, v1), Color = color };
                p[1] = new VertexPositionColorTexture { Position = new Vector3(x + w, y, depthTop), TextureCoordinate = new Vector2(u2, v1), Color = color };
                p[2] = new VertexPositionColorTexture { Position = new Vector3(x, y + h, depthBottom), TextureCoordinate = new Vector2(u1, v2), Color = color };
                p[3] = new VertexPositionColorTexture { Position = new Vector3(x + w, y + h, depthBottom), TextureCoordinate = new Vector2(u2, v2), Color = color };
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
}
