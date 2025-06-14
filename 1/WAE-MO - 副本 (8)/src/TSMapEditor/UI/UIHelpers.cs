using Rampastring.XNAUI.XNAControls;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI
{
    public static class UIHelpers
    {
        public static void AddSearchTipsBoxToControl(XNAControl control)
        {
            bool isChinese = MainMenu.IsChinese;
            
            var lblSearchTips = new XNALabel(control.WindowManager);
            lblSearchTips.Name = nameof(lblSearchTips);
            lblSearchTips.Text = "?";
            lblSearchTips.X = control.Width - Constants.UIEmptySideSpace - lblSearchTips.Width;
            lblSearchTips.Y = (control.Height - lblSearchTips.Height) / 2;
            control.AddChild(lblSearchTips);
            var tooltip = new ToolTip(control.WindowManager, lblSearchTips);
            tooltip.Text = isChinese ? 
                "搜索提示\r\n\r\n在文本框激活状态下：\r\n- 按回车键 (ENTER) 移动到列表中的下一个匹配项\r\n- 按ESC键清除搜索内容" :
                "Search Tips\r\n\r\nWith the text box activated:\r\n- Press ENTER to move to next match in list\r\n- Press ESC to clear search query";
        }
    }
}
