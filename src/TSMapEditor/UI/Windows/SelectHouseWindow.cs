﻿using System;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.Models;

namespace TSMapEditor.UI.Windows
{
    public class SelectHouseWindow : SelectObjectWindow<House>
    {
        public SelectHouseWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public override void Initialize()
        {
            Name = nameof(SelectHouseWindow);
            base.Initialize();
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (House)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();
            foreach (var house in map.GetHouses())
            {
                lbObjectList.AddItem(new XNAListBoxItem() { Text = $"{house.ININame}", TextColor = house.XNAColor, Tag = house });
                if (house == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
