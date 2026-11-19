using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI.Graphics;

namespace MapEditorLibrary.Graphics;

/// <summary>
/// Collects batches of paletted sprites from the theater graphics and owns the resulting texture atlas.
/// </summary>
public class GraphicsPreparationClass : IDisposable
{
    private readonly List<SpriteSheetPreparation> batches = new();
    private SpriteSheet<int> spriteSheet;

    public int PageCount => spriteSheet?.PageCount ?? 0;

    public SpriteSheetPreparation CreateBatch()
    {
        var batch = new SpriteSheetPreparation();

        lock (batches)
            batches.Add(batch);

        return batch;
    }

    public void Process(GraphicsDevice graphicsDevice)
    {
        if (spriteSheet != null)
            throw new InvalidOperationException("The theater sprite sheet has already been built.");

        var sheet = new SpriteSheet<int>(graphicsDevice, SurfaceFormat.Alpha8);

        try
        {
            foreach (var batch in batches)
                batch.RegisterImages(sheet);

            sheet.Process(graphicsDevice);

            int index = 0;
            foreach (var batch in batches)
                batch.AssignRegions(sheet, ref index);

            spriteSheet = sheet;
            batches.Clear();
        }
        catch
        {
            sheet.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        spriteSheet?.Dispose();
        spriteSheet = null;
        batches.Clear();
    }
}
