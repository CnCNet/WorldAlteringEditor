using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace TSMapEditor.UI.Controls;

public class EditorListBox : XNAListBox
{
    public EditorListBox(WindowManager windowManager) : base(windowManager)
    {
    }

    public override void Initialize()
    {
        base.Initialize();

        if (BackgroundTexture.IsEmpty)
        {
            var color = ((CustomUISettings)UISettings.ActiveSettings).ListBoxBackgroundColor;
            BackgroundTexture = AssetLoader.GetSolidUITexture(color);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
        }
    }
}
