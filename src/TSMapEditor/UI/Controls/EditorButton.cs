using MapEditorLibrary;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.Graphics;
using Rampastring.XNAUI.XNAControls;

namespace TSMapEditor.UI.Controls;

public class EditorButton : XNAButton
{
    public EditorButton(WindowManager windowManager) : base(windowManager)
    {
        FontIndex = Constants.UIBoldFont;
        Height = Constants.UIButtonHeight;
        AlphaRate = 0.005f; // Override default animation rate in UISettings
    }

    public TextureReference ExtraTexture { get; set; }

    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        base.ParseControlINIAttribute(iniFile, key, value);

        if (key == "ExtraTexture")
        {
            ExtraTexture = AssetLoader.LoadUITexture(value);
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        var customUISettings = UISettings.ActiveSettings as CustomUISettings;

        IdleTexture = EditorThemes.GetButtonTexture(Width, Height, customUISettings.ButtonMainBackgroundColor,
            customUISettings.ButtonSecondaryBackgroundColor, customUISettings.ButtonTertiaryBackgroundColor);

        HoverTexture = EditorThemes.GetButtonTexture(Width, Height, customUISettings.ButtonHoverMainBackgroundColor,
            customUISettings.ButtonHoverSecondaryBackgroundColor, customUISettings.ButtonHoverTertiaryBackgroundColor);
    }

    public override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);

        if (!ExtraTexture.IsEmpty)
        {
            var rect = new Rectangle((Width - ExtraTexture.Width) / 2,
                (Height - ExtraTexture.Height) / 2, ExtraTexture.Width, ExtraTexture.Height);

            DrawTexture(ExtraTexture, rect, Color.White);
        }
    }
}
