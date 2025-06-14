using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;

namespace TSMapEditor.UI.Windows
{
    public class SelectScriptActionWindow : SelectObjectWindow<ScriptAction>
    {
        public SelectScriptActionWindow(WindowManager windowManager, EditorConfig editorConfig) : base(windowManager)
        {
            this.editorConfig = editorConfig;
        }

        private EditorConfig editorConfig;

        public override void Initialize()
        {
            Name = nameof(SelectScriptActionWindow);
            base.Initialize();
        }
        
        // 添加重写OnOpen方法，在打开窗口时刷新语言
        protected override void OnOpen()
        {
            base.OnOpen();
            RefreshLanguage();
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (ScriptAction)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();
            bool isChinese = MainMenu.IsChinese;

            foreach (ScriptAction scriptAction in editorConfig.ScriptActions.Values)
            {
                string displayName = isChinese ? scriptAction.ChineseName : scriptAction.Name;
                lbObjectList.AddItem(new XNAListBoxItem() { Text = $"{scriptAction.ID} {displayName}", Tag = scriptAction });
                if (scriptAction == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
        
        // 添加刷新语言的方法
        public void RefreshLanguage()
        {
            // 刷新窗口标题
            FindChild<XNALabel>("lblDescription").Text = MainMenu.IsChinese ? "选择脚本动作:" : "Select script action:";
            
            // 刷新按钮文本
            FindChild<XNAButton>("btnSelect").Text = MainMenu.IsChinese ? "选择" : "Select";
            
           
            
            // 重新加载列表
            ListObjects();
        }
    }
}
