using System;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class SelectTechnoTypeWindow : SelectObjectWindow<TechnoType>
    {
        public SelectTechnoTypeWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public bool IncludeNone { get; set; }

        public override void Initialize()
        {
            Name = nameof(SelectTechnoTypeWindow);
            base.Initialize();
            RefreshLanguage();
        }

        public void RefreshLanguage()
        {
            FindChild<EditorButton>("btnSelect").Text = TSMapEditor.UI.MainMenu.IsChinese ? "选择" : "Select";
            tbSearch.Suggestion = TSMapEditor.UI.MainMenu.IsChinese ? "搜索..." : "Search...";
            ListObjects(); // Refresh the list with new language
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (TechnoType)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            if (IncludeNone)
                lbObjectList.AddItem(TSMapEditor.UI.MainMenu.IsChinese ? "<无>" : "None");

            var technoTypes = map.GetAllTechnoTypes();

            foreach (var technoType in technoTypes)
            {
                lbObjectList.AddItem(new XNAListBoxItem() { Text = $"{technoType.Index} {technoType.GetEditorDisplayName()} ({technoType.ININame})", Tag = technoType });
                if (technoType == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
