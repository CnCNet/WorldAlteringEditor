using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace TSMapEditor.UI.Controls
{
    public class EditorButton : XNAButton
    {
        public EditorButton(WindowManager windowManager) : base(windowManager)
        {
            FontIndex = Constants.UIBoldFont;
            Height = Constants.UIButtonHeight;
        }

        public Texture2D ExtraTexture { get; set; }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            base.ParseControlINIAttribute(iniFile, key, value);

            if (key == "ExtraTexture")
            {
                ExtraTexture = AssetLoader.LoadTextureUncached(value);
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            var customUISettings = UISettings.ActiveSettings as CustomUISettings;

            IdleTexture = Helpers.CreateUITexture(GraphicsDevice, Width, Height,
                customUISettings.ButtonMainBackgroundColor,
                customUISettings.ButtonSecondaryBackgroundColor,
                customUISettings.ButtonTertiaryBackgroundColor);

            HoverTexture = Helpers.CreateUITexture(GraphicsDevice, Width, Height,
                new Color(128, 128, 128, 196),
                new Color(128, 128, 128, 255), Color.White);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (ExtraTexture != null)
            {
                var rect = new Rectangle((Width - ExtraTexture.Width) / 2,
                    (Height - ExtraTexture.Height) / 2, ExtraTexture.Width, ExtraTexture.Height);

                DrawTexture(ExtraTexture, rect, Color.White);
            }
        }
    }
}
