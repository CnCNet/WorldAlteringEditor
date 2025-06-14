using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.Models;

namespace TSMapEditor.UI.Windows
{
    public class SelectConnectedTileWindow : SelectObjectWindow<CliffType>
    {
        public SelectConnectedTileWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
            
            // 添加连接地形名称的中文翻译
            CliffChineseNames = new Dictionary<string, string>
            {
                { "Cliffs", "悬崖" },
                { "Water Cliffs", "水域悬崖" },
                { "Snow Cliffs", "雪地悬崖" },
                { "Snow Water Cliffs", "雪地水域悬崖" },
                { "Ice Cliffs", "冰川悬崖" },
                { "Ice Water Cliffs", "冰川水域悬崖" },
                { "Dirt Roads", "土路" },
                { "Paved Roads", "铺装道路" },
                { "Temperate Shore", "温带海岸" },
                { "Snow Shore", "雪地海岸" },
                { "Desert Cliffs", "沙漠悬崖" },
                { "Desert Water Cliffs", "沙漠水域悬崖" },
                { "Sand Roads", "沙路" },
                { "Stone Water Cliffs", "石质水域悬崖" },
                { "Stone Cliffs", "石质悬崖" },
                { "Pavement Cliffs Concrete Water", "混凝土水域悬崖" },
                { "Pavement Cliffs Concrete", "混凝土悬崖" },
                { "Concrete Cliffs", "混凝土悬崖" },
                { "Small Paved Roads", "小型铺装道路" },
                { "Urban Grass Shore", "城市草地海岸" },
                { "Highway Roads", "高速公路" },
                { "Shore", "海岸" },
                { "Desert Cliff", "沙漠悬崖" },
                { "Interior Wall", "室内墙" },
                { "Temperate Cliff", "温带悬崖" },
                { "Urban Wall", "城市墙" }
            };
        }

        private readonly Map map;
        private readonly Dictionary<string, string> CliffChineseNames;
        private XNAButton btnDraw;
        private CliffType selectedCliff; // 保存当前选中的悬崖类型

        public override void Initialize()
        {
            Name = nameof(SelectConnectedTileWindow);
            base.Initialize();
            
            // 获取确认按钮引用
            btnDraw = FindChild<XNAButton>("btnSelect");
            
            // 刷新语言
            RefreshLanguage();
        }
        
        // 添加无参数版本的RefreshLanguage方法，供UIManager调用
        public void RefreshLanguage()
        {
            RefreshLanguage(TSMapEditor.UI.MainMenu.IsChinese);
        }
        
        public void RefreshLanguage(bool isChinese)
        {
            // 更新窗口标题
            Text = isChinese ? "选择连接地形：" : "Select Connected Tiles:";
            
            // 更新按钮文本
            if (btnDraw != null)
                btnDraw.Text = isChinese ? "绘制" : "Draw";
            
            // 保存当前选中项
            if (lbObjectList != null && lbObjectList.SelectedItem != null)
                selectedCliff = lbObjectList.SelectedItem.Tag as CliffType;
            
            // 重新加载列表
            if (lbObjectList != null && map != null)
            {
                ListObjects();
                
                // 恢复选中项
                if (selectedCliff != null)
                {
                    for (int i = 0; i < lbObjectList.Items.Count; i++)
                    {
                        if (lbObjectList.Items[i].Tag == selectedCliff)
                        {
                            lbObjectList.SelectedIndex = i;
                            break;
                        }
                    }
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

            SelectedObject = (CliffType)lbObjectList.SelectedItem.Tag;
        }

        public void Open()
        {
            Open(null);
            RefreshLanguage();
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            foreach (CliffType cliff in map.EditorConfig.Cliffs.Where(cliff =>
                         cliff.AllowedTheaters.Exists(theaterName => theaterName.Equals(map.TheaterName, StringComparison.OrdinalIgnoreCase))))
            {
                string text = cliff.Name;
                if (TSMapEditor.UI.MainMenu.IsChinese && CliffChineseNames.ContainsKey(text))
                {
                    text = CliffChineseNames[text];
                }
                
                if (cliff.IsLegal)
                    lbObjectList.AddItem(new XNAListBoxItem() { Text = text, Tag = cliff, TextColor = cliff.Color.GetValueOrDefault(lbObjectList.DefaultItemColor) });
            }
        }
    }
}