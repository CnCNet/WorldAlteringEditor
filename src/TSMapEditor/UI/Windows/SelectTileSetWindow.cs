using System;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;

namespace TSMapEditor.UI.Windows
{
    /// <summary>
    /// A window that allows the user to select a tile set.
    /// </summary>
    public class SelectTileSetWindow : SelectObjectWindow<TileSet>
    {
        public SelectTileSetWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public override void Initialize()
        {
            Name = nameof(SelectTileSetWindow);
            base.Initialize();
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (TileSet)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            lbObjectList.AddItem(Translate(this, "None", "None"));

            for (int i = 0; i < map.TheaterInstance.Theater.TileSets.Count; i++)
            {
                var tileset = map.TheaterInstance.Theater.TileSets[i];
                if (tileset.NonMarbleMadness != -1 || tileset.LoadedTileCount == 0 || !tileset.AllowToPlace)
                    continue;

                lbObjectList.AddItem(new XNAListBoxItem() { Text = tileset.SetName, Tag = tileset });
                if (tileset == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
