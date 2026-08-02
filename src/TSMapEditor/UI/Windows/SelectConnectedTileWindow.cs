using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Linq;
using TSMapEditor.Models;

namespace TSMapEditor.UI.Windows
{
    public class SelectConnectedTileWindow : SelectObjectWindow<ConnectedTileType>
    {
        public SelectConnectedTileWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public override void Initialize()
        {
            Name = nameof(SelectConnectedTileWindow);
            base.Initialize();
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (ConnectedTileType)lbObjectList.SelectedItem.Tag;
        }

        public void Open()
        {
            Open(null);
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            foreach (ConnectedTileType cliff in map.EditorConfig.Cliffs.Where(cliff =>
                         cliff.AllowedTheaters.Exists(theaterName => theaterName.Equals(map.TheaterName, StringComparison.OrdinalIgnoreCase))))
            {
                lbObjectList.AddItem(new XNAListBoxItem() { Text = cliff.Name, Tag = cliff, TextColor = cliff.Color.GetValueOrDefault(lbObjectList.DefaultItemColor) });
            }
        }
    }
}