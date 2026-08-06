using MapEditorLibrary.Graphics;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TSMapEditor.Rendering.Batching;

public class TextureBatch<T> where T : struct, IVertexType
{
    public Texture2D Texture;
    public T[] Vertices = new T[16];
    public short[] Indexes = new short[24];

    public int QuadCount;

    public void Clear()
    {
        Texture = null;
        QuadCount = 0;
    }

    public void ExtendArrays()
    {
        if (IsFull())
            throw new InvalidOperationException("Cannot extend a texture batch that is already full.");

        // Double array sizes
        var newVertexArray = new T[Math.Min(Vertices.Length * 2, RenderingConstants.MaxVertices)];
        Array.Copy(Vertices, newVertexArray, Vertices.Length);
        Vertices = newVertexArray;

        var newIndexArray = new short[Indexes.Length * 2];
        Array.Copy(Indexes, newIndexArray, Indexes.Length);
        Indexes = newIndexArray;
    }

    public bool IsFull() => QuadCount + RenderingConstants.VerticesPerQuad >= RenderingConstants.MaxVertices / RenderingConstants.VerticesPerQuad;
}
