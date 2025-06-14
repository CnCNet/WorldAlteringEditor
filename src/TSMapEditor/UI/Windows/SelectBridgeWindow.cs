using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using TSMapEditor.Models;

namespace TSMapEditor.UI.Windows
{
    public class SelectBridgeWindow : SelectObjectWindow<BridgeType>
    {
        public SelectBridgeWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
            
            // 添加桥梁名称的中文翻译
            BridgeChineseNames = new Dictionary<string, string>
            {
                { "Low Concrete Bridge", "低矮混凝土桥" },
                { "Low Wooden Bridge", "低矮木桥" },
                { "High Concrete Bridge", "高架混凝土桥" },
                { "High Wooden Bridge", "高架木桥" }
            };
        }

        private readonly Map map;
        private readonly Dictionary<string, string> BridgeChineseNames;
        private XNAButton btnDraw;

        public override void Initialize()
        {
            Name = nameof(SelectBridgeWindow);
            base.Initialize();
            
            // 获取确认按钮引用
            btnDraw = FindChild<XNAButton>("btnSelect");
            
            // 刷新语言
            RefreshLanguage();
        }
        
        public void RefreshLanguage()
        {
            bool isChinese = TSMapEditor.UI.MainMenu.IsChinese;
            
            // 更新窗口标题
            Text = isChinese ? "选择桥梁：" : "Select Bridge:";
            
            // 更新按钮文本
            if (btnDraw != null)
                btnDraw.Text = isChinese ? "绘制" : "Draw";
                
            // 更新桥梁列表的名称
            RefreshBridgeNames();
        }
        
        private void RefreshBridgeNames()
        {
            bool isChinese = TSMapEditor.UI.MainMenu.IsChinese;
            if (!isChinese)
                return;
                
            foreach (XNAListBoxItem item in lbObjectList.Items)
            {
                BridgeType bridge = item.Tag as BridgeType;
                if (bridge != null && BridgeChineseNames.ContainsKey(bridge.Name))
                {
                    item.Text = BridgeChineseNames[bridge.Name];
                }
            }
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (BridgeType)lbObjectList.SelectedItem.Tag;
        }

        public void Open()
        {
            Open(null);
            RefreshLanguage();
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            foreach (BridgeType bridge in map.EditorConfig.Bridges)
            {
                string text = bridge.Name;
                if (TSMapEditor.UI.MainMenu.IsChinese && BridgeChineseNames.ContainsKey(text))
                {
                    text = BridgeChineseNames[text];
                }
                
                lbObjectList.AddItem(new XNAListBoxItem() { Text = text, Tag = bridge });
            }
        }
    }
}