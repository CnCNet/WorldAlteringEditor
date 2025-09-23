using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using System;
using TSMapEditor.Models;

namespace TSMapEditor.UI.Windows
{
    public class SelectAnimationWindow : SelectObjectWindow<AnimType>
    {
        public SelectAnimationWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public bool IncludeNone { get; set; } = true;

        public override void Initialize()
        {
            Name = nameof(SelectAnimationWindow);
            base.Initialize();
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (AnimType)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            if (IncludeNone)
                lbObjectList.AddItem(new XNAListBoxItem() { Text = Translate(this, "None", "None") });

            foreach (AnimType animType in map.Rules.AnimTypes)
            {
                lbObjectList.AddItem(new XNAListBoxItem() { Text = $"{animType.Index} {animType.ININame}", Tag = animType });
                if (animType == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
