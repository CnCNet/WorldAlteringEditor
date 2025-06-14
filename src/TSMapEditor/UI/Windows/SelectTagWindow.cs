using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class SelectTagWindow : SelectObjectWindow<Tag>
    {
        public SelectTagWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public override void Initialize()
        {
            Name = nameof(SelectTagWindow);
            base.Initialize();
            var lblDescription = FindChild<XNALabel>("lblDescription");
            if (lblDescription != null)
                lblDescription.Text = MainMenu.IsChinese ? "选择标签:" : "Select Tag:";
            var btnSelect = FindChild<EditorButton>("btnSelect");
            if (btnSelect != null)
                btnSelect.Text = MainMenu.IsChinese ? "选择" : "Select";
        }

        public void RefreshLanguage()
        {
            var lblDescription = FindChild<XNALabel>("lblDescription");
            if (lblDescription != null)
                lblDescription.Text = MainMenu.IsChinese ? "选择标签:" : "Select Tag:";
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

            SelectedObject = (Tag)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            lbObjectList.AddItem(new XNAListBoxItem() { Text = "None" });

            foreach (Tag tag in map.Tags)
            {
                Color color = lbObjectList.DefaultItemColor;
                var trigger = tag.Trigger;
                if (trigger != null && !string.IsNullOrWhiteSpace(trigger.EditorColor))
                    color = trigger.XNAColor;

                lbObjectList.AddItem(new XNAListBoxItem() { Text = $"{tag.Name} ({tag.ID})", TextColor = color, Tag = tag });
                if (tag == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
