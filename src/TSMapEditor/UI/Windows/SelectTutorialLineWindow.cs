using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.Models;

namespace TSMapEditor.UI.Windows
{
    public class SelectTutorialLineWindow : SelectObjectWindow<TutorialLine>
    {
        public SelectTutorialLineWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public override void Initialize()
        {
            Name = nameof(SelectTutorialLineWindow);
            base.Initialize();
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = new TutorialLine(-1, null);
                return;
            }

            SelectedObject = (TutorialLine)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            bool darkenColor = false;
            Color darkenedColor = lbObjectList.DefaultItemColor * 0.7f;

            foreach (TutorialLine tutorialLine in map.Rules.TutorialLines.GetLines())
            {
                lbObjectList.AddItem(new XNAListBoxItem() 
                { 
                    Text = $"{tutorialLine.ID} {tutorialLine.Text}",
                    Tag = tutorialLine,
                    TextColor = darkenColor ? darkenedColor : lbObjectList.DefaultItemColor
                });

                if (tutorialLine.ID == SelectedObject.ID)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;

                darkenColor = !darkenColor;
            }
        }
    }
}
