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

    private bool isStockBackgroundTexture;

    public override void Initialize()
    {
        base.Initialize();

        if (BackgroundTexture == null)
        {
            BackgroundTexture = AssetLoader.CreateTexture(Color.White, 2, 2);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            isStockBackgroundTexture = true;
        }

        UIHelpers.AutoAssignTextBoxNextControls(this);
    }

    public override void Kill()
    {
        if (isStockBackgroundTexture)
            BackgroundTexture?.Dispose();

        base.Kill();
    }

    public override void Draw(GameTime gameTime)
    {
        RemapColor = UISettings.ActiveSettings.PanelBackgroundColor;
        base.Draw(gameTime);
    }
}
