using System;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class SelectEventWindow : SelectObjectWindow<TriggerEventType>
    {
        public SelectEventWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public bool IsAddingNew { get; set; }

        public override void Initialize()
        {
            Name = nameof(SelectEventWindow);
            base.Initialize();
            
            RefreshLanguage();
        }
        
        public void RefreshLanguage()
        {
            // 更新窗口标题和按钮文本
            var lblDescription = FindChild<XNALabel>("lblDescription");
            if (lblDescription != null)
                lblDescription.Text = MainMenu.IsChinese ? "选择事件:" : "Select Event:";
                
            var tbSearch = FindChild<EditorSuggestionTextBox>("tbSearch");
            if (tbSearch != null)
                tbSearch.Suggestion = MainMenu.IsChinese ? "搜索事件..." : "Search Event...";
                
            var btnSelect = FindChild<EditorButton>("btnSelect");
            if (btnSelect != null)
                btnSelect.Text = MainMenu.IsChinese ? "选择" : "Select";
                
            // 如果列表已加载，重新加载以应用翻译
            if (Visible && lbObjectList != null && lbObjectList.Items.Count > 0)
                ListObjects();
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (TriggerEventType)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            foreach (TriggerEventType triggerEventType in map.EditorConfig.TriggerEventTypes.Values)
            {
                string displayText;
                if (MainMenu.IsChinese && !string.IsNullOrEmpty(triggerEventType.ChineseName))
                {
                    displayText = $"{triggerEventType.ID} {triggerEventType.ChineseName}";
                }
                else
                {
                    displayText = $"{triggerEventType.ID} {triggerEventType.Name}";
                }
                
                lbObjectList.AddItem(new XNAListBoxItem() { Text = displayText, Tag = triggerEventType });
                if (triggerEventType == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
