using MapEditorLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TSMapEditor.Rendering.Batching;

public struct VertexForObject : IVertexType
{
    public Vector3 Position;
    public Color Color;
    public Vector2 TextureCoordinate;
    public Vector4 CustomData;

    public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration
    (
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
        new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(24, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1)
    );

    readonly VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
}

public sealed class GameObjectBatcher : AbstractBatcher<VertexForObject>
{
    public GameObjectBatcher(GraphicsDevice graphicsDevice, Effect effect) : base(graphicsDevice, effect)
    {
    }

    public void Draw(Texture2D texture, Rectangle destinationRect, Rectangle? sourceRect, Color color, DepthRectangle depthRectangle)
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

        TextureBatch<VertexForObject> batch = null;
        for (int i = 0; i < queuedBatches.Count; i++)
        {
            if (queuedBatches[i].Texture == texture && !queuedBatches[i].IsFull())
                batch = queuedBatches[i];
        }

        if (batch == null)
        {
            if (!inactiveBatches.TryDequeue(out batch))
            {
                batch = new TextureBatch<VertexForObject>();
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
            fixed (VertexForObject* p = &batch.Vertices[vertexOffset])
            {
                p[0] = new VertexForObject { Position = new Vector3(x, y, depthRectangle.TopLeft), TextureCoordinate = new Vector2(u1, v1), Color = color };
                p[1] = new VertexForObject { Position = new Vector3(x + w, y, depthRectangle.TopRight), TextureCoordinate = new Vector2(u2, v1), Color = color };
                p[2] = new VertexForObject { Position = new Vector3(x, y + h, depthRectangle.BottomLeft), TextureCoordinate = new Vector2(u1, v2), Color = color };
                p[3] = new VertexForObject { Position = new Vector3(x + w, y + h, depthRectangle.BottomRight), TextureCoordinate = new Vector2(u2, v2), Color = color };
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
