using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace TSMapEditor.UI.Controls
{
    public class EditorListBox : XNAListBox
    {
        public EditorListBox(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            if (BackgroundTexture == null)
            {
                var color = ((CustomUISettings)UISettings.ActiveSettings).ListBoxBackgroundColor;
                BackgroundTexture = AssetLoader.CreateTexture(color, 2, 2);
                PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            }
        }
    }
}
