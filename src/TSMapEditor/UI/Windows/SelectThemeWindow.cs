using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class SelectThemeWindow : SelectObjectWindow<Theme>
    {
        public SelectThemeWindow(WindowManager windowManager, Map map, bool includeNone) : base(windowManager)
        {
            this.map = map;
            this.includeNone = includeNone;
        }

        private readonly Map map;
        private readonly bool includeNone;
        
        // 添加主题名称的中文翻译字典
        private static readonly Dictionary<string, string> ThemeChineseNames = new Dictionary<string, string>
        {
            { "Morphscape", "变形景观" },
            { "OddFunk", "奇异风格" },
            { "ItHasBegun", "已经开始" },
            { "TheEdge", "边缘" },
            { "GetOnIt", "行动起来" },
            { "Escape", "逃离" },
            { "VirtualControl", "虚拟控制" },
            { "TheBox", "盒子" },
            { "Cybertek", "赛博科技" },
            { "Strange", "奇怪" },
            { "Decible", "分贝" },
            { "InYourFace", "当面对质" },
            { "ActOn", "行动" },
            { "Infiltrator", "渗透者" },
            { "Morpthunk", "变形朋克" },
            { "TheStreets", "街道" },
            { "Mechanical", "机械" },
            { "InTheTunnel", "隧道之中" },
            { "BrainDead", "脑死亡" },
            { "Magnified", "放大" },
            { "Rocktronic", "摇滚电子" },
            { "Defunct", "消亡" },
            { "Construct", "构建" },
            { "Ownage", "征服" },
            { "Banished", "放逐" },
            { "PerishedLives", "逝去的生命" },
            { "Vigilante", "义警" },
            { "Warfare", "战争" },
            { "Reaping", "收割" },
            { "MindPrison", "思维监狱" },
            { "SonicPain", "音波痛苦" },
            { "Fantasy", "幻想" },
            { "Kill", "杀戮" },
            { "HMM", "嗯..." },
            { "Frostburn", "冻伤" },
            { "ChaoticImpulse", "混沌冲动" },
            { "Outbreak", "爆发" },
            { "Echo", "回声" },
            { "Aenigmata", "谜题" },
            { "Diablo", "恶魔" },
            { "Realms", "领域" },
            { "Psychosis", "精神病" },
            { "Solara", "索拉拉" },
            { "Duality", "二元性" },
            { "Inside", "内部" },
            { "VVersusP", "V对P" },
            { "TheGates", "大门" },
            { "Forgotten", "被遗忘" },
            { "ObsidianSands", "黑曜石沙滩" },
            { "Remnant", "残余" },
            { "Stormringer", "风暴使者" },
            { "DestinyCombat", "命运战斗" },
            { "Calibration", "校准" },
            { "ILLOD", "幻觉" },
            { "RightOnTrack", "正轨" },
            { "Heroic", "英雄" },
            { "Beyond", "超越" },
            { "Breakdown", "崩溃" },
            { "Brotherhood", "兄弟会" },
            { "TheRescue", "救援" },
            { "Downfall", "沦陷" },
            { "Destroyer", "毁灭者" },
            { "AlterTheFuture", "改变未来" },
            { "EdgeRazor", "边缘剃刀" },
            { "Shoulders", "肩膀" },
            { "DEAMachina", "机器神" }
        };

        public override void Initialize()
        {
            Name = nameof(SelectThemeWindow);
            base.Initialize();
            
            // 初始化时刷新语言
            RefreshLanguage(MainMenu.IsChinese);
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (Theme)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            if (includeNone)
            {
                lbObjectList.AddItem(new XNAListBoxItem() { Text = MainMenu.IsChinese ? "无" : "None" });
            }

            foreach (var theme in map.Rules.Themes.List)
            {
                string displayText = theme.ToString();
                
                // 如果是中文模式，尝试翻译主题名称
                if (MainMenu.IsChinese)
                {
                    // 处理主题名称 - 格式通常是 "数字 名称"，例如 "32 THEME:Kill"
                    // 提取名称部分并检查是否有对应的中文翻译
                    string name = theme.Name;
                    if (name.StartsWith("THEME:"))
                    {
                        name = name.Substring(6); // 移除"THEME:"前缀
                    }
                    
                    if (ThemeChineseNames.TryGetValue(name, out string chineseName))
                    {
                        displayText = $"{theme.Index} {chineseName}";
                    }
                }
                
                lbObjectList.AddItem(new XNAListBoxItem() { Text = displayText, Tag = theme });
                if (theme == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
        
        /// <summary>
        /// 刷新窗口和列表中的文本语言
        /// </summary>
        public void RefreshLanguage(bool isChinese)
        {
            // 设置窗口标题
            SetWindowTitle(isChinese ? "选择主题" : "Select Theme");
            
            // 查找搜索框和按钮
            var searchBox = FindChild<EditorSuggestionTextBox>("tbSearch");
            if (searchBox != null)
                searchBox.Suggestion = isChinese ? "搜索主题..." : "Search Theme...";
                
            var selectButton = FindChild<EditorButton>("btnSelect");
            if (selectButton != null)
                selectButton.Text = isChinese ? "选择" : "Select";
            
            // 如果列表已经加载过，刷新列表项的文本
            if (lbObjectList != null && lbObjectList.Items.Count > 0)
            {
                ListObjects();
            }
        }
        
        // 通过修改INI配置文件来设置窗口标题
        private void SetWindowTitle(string title)
        {
            // 使用反射来修改窗口标题
            try 
            {
                var configIniField = GetType().BaseType.GetField("ConfigIni", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (configIniField != null)
                {
                    var configIni = configIniField.GetValue(this) as Rampastring.Tools.IniFile;
                    if (configIni != null)
                    {
                        var section = configIni.GetSection(Name);
                        if (section != null)
                        {
                            section.SetStringValue("WindowTitle", title);
                        }
                    }
                }
                
                // 尝试调用刷新布局的方法，使标题生效
                var method = GetType().BaseType.GetMethod("RefreshLayout", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (method != null)
                {
                    method.Invoke(this, null);
                }
            }
            catch
            {
                // 如果反射操作失败，静默忽略
            }
        }
        
        /// <summary>
        /// 重写Open方法，确保打开时使用当前语言
        /// </summary>
        public new void Open(Theme initialSelection)
        {
            // 先刷新语言
            RefreshLanguage(MainMenu.IsChinese);
            // 调用基类方法
            base.Open(initialSelection);
        }
    }
}
