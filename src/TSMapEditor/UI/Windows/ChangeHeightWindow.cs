using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class ChangeHeightWindow : INItializableWindow
    {
        public ChangeHeightWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private XNADropDown ddHeightLevel;
        private XNALabel lblDescription;
        private EditorButton btnApply;

        public override void Initialize()
        {
            Name = nameof(ChangeHeightWindow);
            base.Initialize();

            ddHeightLevel = FindChild<XNADropDown>(nameof(ddHeightLevel));
            lblDescription = FindChild<XNALabel>("lblDescription");
            btnApply = FindChild<EditorButton>("btnApply");
            btnApply.LeftClick += ChangeHeightWindow_LeftClick;
        }

        private void ChangeHeightWindow_LeftClick(object sender, EventArgs e)
        {
            map.ChangeHeight(Conversions.IntFromString(ddHeightLevel.SelectedItem.Text, 0));
            Hide();
        }

        public void Open()
        {
            ddHeightLevel.SelectedIndex = ddHeightLevel.Items.FindIndex(ddi => ddi.Text == "0");
            Show();
        }
        
        public void RefreshLanguage(bool isChinese = false)
        {
            if (isChinese)
            {
                // 窗口标题
                Text = "更改地图高度";
                
                // 描述文本（包含警告）
                lblDescription.Text = "将地图上所有单元格的高度按指定值增加。\n\n此操作无法撤销，请确保先保存地图！";
                
                // 应用按钮
                btnApply.Text = "应用";
            }
            else
            {
                // 恢复英文
                Text = "Change Map Height";
                lblDescription.Text = "Increase the height of all cells on the map by the specified value.\n\nNo un-do is available, make sure you save your map first!";
                btnApply.Text = "Apply";
            }
        }
    }
}
