using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.Rendering;
using TSMapEditor.UI;

namespace TSMapEditor.Initialization
{
    /// <summary>
    /// A minimal, non-UI implementation of <see cref="IMutationTarget"/>.
    /// Allows mutation classes (e.g. <see cref="Mutations.Classes.TerrainGenerationMutation"/>)
    /// that were originally designed to be driven by the interactive map editor UI to instead
    /// be driven programmatically, without any WinForms/MonoGame-rendering objects.
    /// </summary>
    public class HeadlessMutationTarget : IMutationTarget
    {
        public HeadlessMutationTarget(Map map)
        {
            Map = map;
            TheaterGraphics = map.TheaterInstance as TheaterGraphics;
            Randomizer = new Randomizer();
        }

        public Map Map { get; }

        public TheaterGraphics TheaterGraphics { get; }

        public House ObjectOwner { get; set; }

        public BrushSize BrushSize { get; set; } = new BrushSize(1, 1);

        public Randomizer Randomizer { get; }

        public bool AutoLATEnabled { get; set; } = true;

        public LightingPreviewMode LightingPreviewState => LightingPreviewMode.NoLighting;

        public bool LightDisabledLightSources => false;

        public bool OnlyPaintOnClearGround => false;

        // No-ops: there is no UI to refresh or invalidate in headless mode.
        public void AddRefreshPoint(Point2D point, int size = 10) { }

        public void InvalidateMap() { }
    }
}
