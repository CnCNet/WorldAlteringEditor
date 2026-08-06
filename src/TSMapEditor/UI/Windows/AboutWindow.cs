using MapEditorLibrary;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows;

public class AboutWindow : INItializableWindow
{
    public AboutWindow(WindowManager windowManager) : base(windowManager)
    {
    }

    public override void Initialize()
    {
        Name = nameof(AboutWindow);
        base.Initialize();

        var lblVersion = FindChild<XNALabel>("lblVersion");
        lblVersion.Text = string.Format(Translate(this, "Version", "Version {0}"), Constants.Version);
    }

    public void Open() => Show();
}
