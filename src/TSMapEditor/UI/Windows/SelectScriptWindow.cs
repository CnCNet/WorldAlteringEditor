using System;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class SelectScriptWindow : SelectObjectWindow<Script>
    {
        public SelectScriptWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public override void Initialize()
        {
            Name = nameof(SelectScriptWindow);
            base.Initialize();
            var lblDescription = FindChild<XNALabel>("lblDescription");
            if (lblDescription != null)
                lblDescription.Text = MainMenu.IsChinese ? "选择脚本:" : "Select Script:";
            var btnSelect = FindChild<EditorButton>("btnSelect");
            if (btnSelect != null)
                btnSelect.Text = MainMenu.IsChinese ? "选择" : "Select";
        }

        public void RefreshLanguage()
        {
            var lblDescription = FindChild<XNALabel>("lblDescription");
            if (lblDescription != null)
                lblDescription.Text = MainMenu.IsChinese ? "选择脚本:" : "Select Script:";
            var btnSelect = FindChild<EditorButton>("btnSelect");
            if (btnSelect != null)
                btnSelect.Text = MainMenu.IsChinese ? "选择" : "Select";
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (Script)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            foreach (Script script in map.Scripts)
            {
                lbObjectList.AddItem(new XNAListBoxItem() 
                {
                    Text = $"{script.Name} ({script.ININame})",
                    Tag = script,
                    TextColor = script.EditorColor == null ? lbObjectList.DefaultItemColor : script.XNAColor
                });
                if (script == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
