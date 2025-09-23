using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.Models;

namespace TSMapEditor.UI.Windows
{
    public class SelectThemeWindow : SelectObjectWindow<Theme>
    {
        public SelectThemeWindow(WindowManager windowManager, Map map, bool includeNone) : base(windowManager)
        {
            this.map = map;
            this.includeNone = includeNone;
        }

        private readonly Map map;
        private readonly bool includeNone;

        public override void Initialize()
        {
            Name = nameof(SelectThemeWindow);
            base.Initialize();
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (Theme)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            if (includeNone)
            {
                lbObjectList.AddItem(new XNAListBoxItem() { Text = Translate(this, "None", "None") });
            }

            foreach (var theme in map.Rules.Themes.List)
            {
                lbObjectList.AddItem(new XNAListBoxItem() { Text = theme.ToString(), Tag = theme });
                if (theme == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
