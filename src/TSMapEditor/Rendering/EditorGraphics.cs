using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;

namespace TSMapEditor.Rendering
{
    /// <summary>
    /// Editor-only assets.
    /// </summary>
    public class EditorGraphics
    {
        public Texture2D GenericTileTexture { get; private set; }
        public Texture2D GenericTileWithBorderTexture { get; private set; }
        public Texture2D TileBorderTexture { get; private set; }
        public Texture2D CellTagTexture { get; private set; }
        public Texture2D ImpassableCellHighlightTexture { get; private set; }
        public Texture2D IceGrowthHighlightTexture { get; private set; }
        public Texture2D RangeIndicatorTexture { get; private set; }

        public EditorGraphics()
        {
            GenericTileTexture = AssetLoader.LoadTextureUncached("generictile.png");
            GenericTileWithBorderTexture = AssetLoader.LoadTextureUncached("generictilewithborder.png");
            TileBorderTexture = AssetLoader.LoadTextureUncached("tileborder.png");
            CellTagTexture = AssetLoader.LoadTextureUncached("celltag.png");
            ImpassableCellHighlightTexture = AssetLoader.LoadTextureUncached("impassablehighlight.png");
            IceGrowthHighlightTexture = AssetLoader.LoadTextureUncached("icehighlight.png");
            RangeIndicatorTexture = AssetLoader.LoadTextureUncached("rangeindicator.png");
        }

        public void DisposeAll()
        {
            GenericTileTexture.Dispose();
            GenericTileWithBorderTexture.Dispose();
            TileBorderTexture.Dispose();
            CellTagTexture.Dispose();
            ImpassableCellHighlightTexture.Dispose();
            IceGrowthHighlightTexture.Dispose();
            RangeIndicatorTexture.Dispose();
        }
    }
}
