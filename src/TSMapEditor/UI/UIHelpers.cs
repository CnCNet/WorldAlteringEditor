using Rampastring.XNAUI.XNAControls;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI
{
    public static class UIHelpers
    {
        public static void AddSearchTipsBoxToControl(XNAControl control)
        {
            var lblSearchTips = new XNALabel(control.WindowManager);
            lblSearchTips.Name = nameof(lblSearchTips);
            lblSearchTips.Text = "?";
            lblSearchTips.X = control.Width - Constants.UIEmptySideSpace - lblSearchTips.Width;
            lblSearchTips.Y = (control.Height - lblSearchTips.Height) / 2;
            control.AddChild(lblSearchTips);
            var tooltip = new ToolTip(control.WindowManager, lblSearchTips);
            tooltip.Text = Translate("UIHelpers.AddSearchTipsBoxToControl.SearchTip", "Search Tips\r\n\r\nWith the text box activated:\r\n- Press ENTER to move to next match in list\r\n- Press ESC to clear search query");
        }
    }
}
