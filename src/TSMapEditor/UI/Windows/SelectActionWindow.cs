using System;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class SelectActionWindow : SelectObjectWindow<TriggerActionType>
    {
        public SelectActionWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public bool IsAddingNew { get; set; }

        public override void Initialize()
        {
            Name = nameof(SelectActionWindow);
            base.Initialize();
            
            RefreshLanguage();
        }
        
        public void RefreshLanguage()
        {
            // 更新窗口标题、搜索提示和按钮文本
            var lblDescription = FindChild<XNALabel>("lblDescription");
            if (lblDescription != null)
            {
                lblDescription.Text = MainMenu.IsChinese ? "选择操作类型:" : "Select action type:";
            }
            
            // 搜索框在基类中已经定义为tbSearch而不是tbFilter
            if (tbSearch != null)
            {
                tbSearch.Suggestion = MainMenu.IsChinese ? "搜索..." : "Search...";
            }
            
            var btnSelect = FindChild<XNAButton>("btnSelect");
            if (btnSelect != null)
            {
                btnSelect.Text = MainMenu.IsChinese ? "选择" : "Select";
            }
            
            // 刷新列表，显示中文名称
            ListObjects();
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (TriggerActionType)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            foreach (TriggerActionType triggerActionType in map.EditorConfig.TriggerActionTypes.Values)
            {
                // 支持中英文切换显示
                string actionName = MainMenu.IsChinese && !string.IsNullOrEmpty(triggerActionType.ChineseName) 
                    ? triggerActionType.ChineseName 
                    : triggerActionType.Name;
                
                lbObjectList.AddItem(new XNAListBoxItem() { Text = $"{triggerActionType.ID} {actionName}", Tag = triggerActionType });
                if (triggerActionType == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
