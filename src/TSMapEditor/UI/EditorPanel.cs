using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace TSMapEditor.UI;

/// <summary>
/// A XNAPanel derivative that sets its background.
/// </summary>
public class EditorPanel : XNAPanel
{
    public EditorPanel(WindowManager windowManager) : base(windowManager)
    {
    }

    public override void Initialize()
    {
        base.Initialize();

        if (BackgroundTexture.IsEmpty)
        {
            BackgroundTexture = AssetLoader.GetSolidUITexture(Color.White);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
        }

        UIHelpers.AutoAssignTextBoxNextControls(this);
    }

    public override void Draw(GameTime gameTime)
    {
        RemapColor = UISettings.ActiveSettings.PanelBackgroundColor;
        base.Draw(gameTime);
    }
}
