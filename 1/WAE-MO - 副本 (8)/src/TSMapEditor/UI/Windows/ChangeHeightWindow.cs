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

        public override void Initialize()
        {
            Name = nameof(ChangeHeightWindow);
            base.Initialize();

            ddHeightLevel = FindChild<XNADropDown>(nameof(ddHeightLevel));
            FindChild<EditorButton>("btnApply").LeftClick += ChangeHeightWindow_LeftClick;
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
    }
}
