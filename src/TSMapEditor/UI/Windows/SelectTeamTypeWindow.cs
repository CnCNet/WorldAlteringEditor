using System;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.Models;

namespace TSMapEditor.UI.Windows
{
    public class SelectTeamTypeWindow : SelectObjectWindow<TeamType>
    {
        public SelectTeamTypeWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public bool IncludeNone { get; set; }

        public bool IsForSecondaryTeam { get; set; }

        public override void Initialize()
        {
            Name = nameof(SelectTeamTypeWindow);
            base.Initialize();
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (TeamType)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            if (IncludeNone)
                lbObjectList.AddItem(Translate(this, "None", "None"));

            foreach (TeamType teamType in map.TeamTypes)
            {
                lbObjectList.AddItem(new XNAListBoxItem() { Text = $"{teamType.ININame} {teamType.Name}", TextColor = teamType.GetXNAColor(), Tag = teamType });
                if (teamType == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
