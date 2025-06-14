using Microsoft.Xna.Framework;
using Rampastring.XNAUI;

namespace TSMapEditor.UI.Controls
{
    class EditorPopUpSelector : EditorPanel
    {
        private const int TEXT_HORIZONTAL_MARGIN = 3;
        private const int TEXT_VERTICAL_MARGIN = 2;

        public EditorPopUpSelector(WindowManager windowManager) : base(windowManager)
        {
            Height = Constants.UITextBoxHeight;
            TextIdleColor = UISettings.ActiveSettings.TextColor;
            TextHoverColor = UISettings.ActiveSettings.AltColor;
            textColor = TextIdleColor;
        }

        public int FontIndex { get; set; } = 1;

        public Color TextIdleColor { get; set; }
        public Color TextHoverColor { get; set; }

        private string oldText;
        private string cachedText;

        private Color textColor;

        public override void OnMouseEnter()
        {
            textColor = TextHoverColor;
            base.OnMouseEnter();
        }

        public override void OnMouseLeave()
        {
            textColor = TextIdleColor;
            base.OnMouseLeave();
        }

        public override void Draw(GameTime gameTime)
        {
            DrawPanel();

            if (!string.IsNullOrWhiteSpace(Text))
            {
                if (!ReferenceEquals(oldText, Text))
                {
                    oldText = Text;
                    cachedText = Renderer.GetStringWithLimitedWidth(Text, FontIndex, Width - TEXT_HORIZONTAL_MARGIN);
                }

                DrawStringWithShadow(cachedText, FontIndex,
                    new Vector2(TEXT_HORIZONTAL_MARGIN, TEXT_VERTICAL_MARGIN),
                    textColor);
            }

            base.Draw(gameTime);
        }
    }
}
