using Microsoft.Xna.Framework;

namespace MapEditorLibrary.Graphics
{
    /// <summary>
    /// Class for preparing an arbitrary number of sprite sheets.
    /// </summary>
    public class GraphicsPreparationClass
    {
        public GraphicsPreparationClass(int width = RenderingConstants.MaximumDX11TextureSize, int height = RenderingConstants.MaximumDX11TextureSize)
        {
            SheetWidth = width;
            SheetHeight = height;
        }

        public int SheetWidth { get; }
        public int SheetHeight { get; }

        public List<SpriteSheetPreparation> SpriteSheetPreparationObjects { get; } = new List<SpriteSheetPreparation>();

        public SpriteSheetPreparation CurrentSpriteSheetPreparationObject;

        public Action<object, SpriteSheetPreparation> PostProcessAction { get; set; }

        private readonly object locker = new object();

        public SpriteSheetPreparation GenerateNewSpriteSheetWorkingObject()
        {
            var spriteSheetPreparation = new SpriteSheetPreparation(SheetWidth, SheetHeight);

            lock (locker)
            {
                SpriteSheetPreparationObjects.Add(spriteSheetPreparation);
                CurrentSpriteSheetPreparationObject = spriteSheetPreparation;
            }

            return spriteSheetPreparation;
        }

        public SpriteSheetPreparation GetCurrent()
        {
            if (CurrentSpriteSheetPreparationObject == null)
                CurrentSpriteSheetPreparationObject = GenerateNewSpriteSheetWorkingObject();

            return CurrentSpriteSheetPreparationObject;
        }

        public Point AddImage(int width, int height, byte[] data, object meta)
        {
            if (!CanFitTexture(width, height))
                GenerateNewSpriteSheetWorkingObject();

            return GetCurrent().AddImage(width, height, data, meta);
        }

        public bool CanFitTexture(int width, int height) => GetCurrent().CanFitTexture(width, height);
    }
}
