using MapEditorLibrary;
using Rampastring.XNAUI;

namespace TSMapEditor.UI.Controls;

public class SortButton : EditorButton
{
    public SortButton(WindowManager windowManager) : base(windowManager)
    {
        Width = Constants.UIButtonHeight;
    }

    public override void Initialize()
    {
        ExtraTexture = AssetLoader.LoadUITexture("sortbutton.png");
        base.Initialize();
    }
}
