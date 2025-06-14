using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using System;

namespace TSMapEditor.UI.Notifications
{
    public class Notification : EditorPanel
    {
        private const float AppearAlphaRate = 0.75f;
        private const float DisappearAlphaRate = 0.20f;

        public Notification(WindowManager windowManager) : base(windowManager) 
        {
            DrawMode = Rampastring.XNAUI.XNAControls.ControlDrawMode.UNIQUE_RENDER_TARGET;
            Text = string.Empty;
            AlphaRate = AppearAlphaRate;
            Alpha = 0.0f;
            InputEnabled = false;
        }

        public TimeSpan RemainingLifetime = TimeSpan.FromSeconds(10.0);

        public override string Text 
        { 
            get => base.Text; 
            set { base.Text = value; RecalculateSize(); }
        }

        private void RecalculateSize()
        {
            Vector2 textDimensions = Renderer.GetTextDimensions(Text, Constants.UIBoldFont);

            Width = (int)textDimensions.X + Constants.UIEmptySideSpace * 2;
            Height = (int)textDimensions.Y + Constants.UIEmptyTopSpace + Constants.UIEmptyBottomSpace;
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            DrawStringWithShadow(Text, Constants.UIBoldFont,
                new Vector2(Constants.UIEmptySideSpace, Constants.UIEmptyTopSpace),
                UISettings.ActiveSettings.TextColor);
        }

        public override void Update(GameTime gameTime)
        {
            RemainingLifetime -= gameTime.ElapsedGameTime;

            if (RemainingLifetime.TotalMilliseconds <= 0)
            {
                AlphaRate = -DisappearAlphaRate;
            }

            base.Update(gameTime);
        }
    }
}
