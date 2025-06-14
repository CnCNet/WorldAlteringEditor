using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using TSMapEditor.GameMath;

namespace TSMapEditor.UI.Windows
{
    class SelectObjectWindowInfoPanel : EditorPanel
    {
        public SelectObjectWindowInfoPanel(WindowManager windowManager) : base(windowManager)
        {
            InputEnabled = false;
        }

        public int HeaderFontIndex { get; set; } = Constants.UIBoldFont;

        public int TextFontIndex { get; set; } = Constants.UIDefaultFont;

        private string headerText;

        private string text;

        private int headerHeight;

        public override void Initialize()
        {
            BackgroundTexture = AssetLoader.CreateTexture(UISettings.ActiveSettings.BackgroundColor, 2, 2);

            base.Initialize();
        }

        public override void Kill()
        {
            BackgroundTexture?.Dispose();
            BackgroundTexture = null;

            base.Kill();
        }

        private void RegenerateSize()
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            int width = 0;
            headerHeight = Constants.UIEmptyTopSpace;

            if (!string.IsNullOrWhiteSpace(headerText))
            {
                var headerSize = Renderer.GetTextDimensions(headerText, HeaderFontIndex);
                width = (int)headerSize.X;

                headerHeight = (int)headerSize.Y + Constants.UIEmptyTopSpace + Constants.UIVerticalSpacing;
            }

            var descriptionSize = Renderer.GetTextDimensions(text, TextFontIndex);
            if (descriptionSize.X > width)
                width = (int)descriptionSize.X;

            Width = width + (Constants.UIEmptySideSpace * 2);
            Height = headerHeight + (int)descriptionSize.Y + Constants.UIEmptyBottomSpace;
        }

        public void Open(string header, string description, Point2D point)
        {
            headerText = header;
            text = description;
            RegenerateSize();

            X = point.X;
            Y = point.Y;

            Enable();

            if (!Detached)
                Detach();
        }

        public void Hide()
        {
            if (Detached)
                Attach();

            Disable();
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (!string.IsNullOrWhiteSpace(headerText))
            {
                DrawString(headerText, HeaderFontIndex,
                    new Vector2(Constants.UIEmptySideSpace, Constants.UIEmptyTopSpace),
                    UISettings.ActiveSettings.TextColor);
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                DrawString(text, TextFontIndex,
                    new Vector2(Constants.UIEmptySideSpace, headerHeight),
                    UISettings.ActiveSettings.TextColor);
            }
        }
    }
}
