using System;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class SelectTriggerWindow : SelectObjectWindow<Trigger>
    {
        public SelectTriggerWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public override void Initialize()
        {
            Name = nameof(SelectTriggerWindow);
            base.Initialize();
            
            RefreshLanguage();
        }
        
        public void RefreshLanguage()
        {
            FindChild<XNALabel>("lblDescription").Text = MainMenu.IsChinese ? "选择触发器:" : "Select Trigger:";
            FindChild<EditorButton>("btnSelect").Text = MainMenu.IsChinese ? "选择" : "Select";
            tbSearch.Suggestion = MainMenu.IsChinese ? "搜索触发器..." : "Search Trigger...";
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (Trigger)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            lbObjectList.AddItem("None");

            foreach (Trigger trigger in map.Triggers)
            {
                lbObjectList.AddItem(new XNAListBoxItem() 
                { 
                    Text = $"{trigger.ID} {trigger.Name}",
                    Tag = trigger, 
                    TextColor = trigger.EditorColor == null ? lbObjectList.DefaultItemColor : trigger.XNAColor
                });
                
                if (trigger == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
