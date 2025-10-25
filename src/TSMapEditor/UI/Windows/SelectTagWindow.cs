using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.Models;

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

            lbObjectList.AddItem(new XNAListBoxItem() { Text = Translate(this, "None", "None") });

            foreach (Tag tag in map.Tags)
            {
                Color color = lbObjectList.DefaultItemColor;
                var trigger = tag.Trigger;
                string tagText = $"{tag.Name} ({tag.ID})";

                if (trigger != null)
                {
                    if (!string.IsNullOrWhiteSpace(trigger.EditorColor))
                        color = trigger.XNAColor;

                    string tagName = tag.Name;

                    int index = tag.Name.IndexOf(" (tag)");
                    if (index >= 0)
                    {
                        tagName = tag.Name.Substring(0, index).Trim();
                    }

                    if (!tagName.Equals(trigger.Name.Trim()))
                        tagText += $" [{trigger.Name}]";
                }

                lbObjectList.AddItem(new XNAListBoxItem() { Text = tagText, TextColor = color, Tag = tag });
                if (tag == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
