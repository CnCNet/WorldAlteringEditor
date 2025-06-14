using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Globalization;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    /// <summary>
    /// A window that allows the user to configure basic properties of the map.
    /// </summary>
    public class BasicSectionConfigWindow : INItializableWindow
    {
        public BasicSectionConfigWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private EditorTextBox tbName;
        private EditorTextBox tbAuthor;
        private EditorNumberTextBox tbCarryOverCap;
        private EditorNumberTextBox tbPercent;
        private EditorNumberTextBox tbInitialTime;
        private EditorNumberTextBox tbHomeCell;
        private EditorPopUpSelector selTheme;        
        private XNACheckBox chkEndOfGame;
        private XNACheckBox chkOneTimeOnly;
        private XNACheckBox chkSkipScore;
        private XNACheckBox chkSkipMapSelect;
        private XNACheckBox chkIgnoreGlobalAITriggers;
        private XNACheckBox chkOfficial;
        private XNACheckBox chkTruckCrate;
        private XNACheckBox chkTrainCrate;
        private XNACheckBox chkMultiplayerOnly;
        private XNACheckBox chkGrowingTiberium;
        private XNACheckBox chkGrowingVeins;
        private XNACheckBox chkGrowingIce;
        private XNACheckBox chkTiberiumDeathToVisceroid;
        private XNACheckBox chkFreeRadar;
        private XNACheckBox chkRequiredAddOn;        

        private SelectThemeWindow selectThemeWindow;
        
        // 添加标签控件引用
        private XNALabel lblName;
        private XNALabel lblAuthor;
        private XNALabel lblCarryOverCap;
        private XNALabel lblPercent;
        private XNALabel lblInitialTime;
        private XNALabel lblHomeCell;
        private XNALabel lblTheme;
        private XNALabel lblHeader;
        private EditorButton btnApply;

        public override void Initialize()
        {
            Name = nameof(BasicSectionConfigWindow);
            base.Initialize();

            tbName = FindChild<EditorTextBox>(nameof(tbName));
            tbAuthor = FindChild<EditorTextBox>(nameof(tbAuthor));
            tbCarryOverCap = FindChild<EditorNumberTextBox>(nameof(tbCarryOverCap));
            tbPercent = FindChild<EditorNumberTextBox>(nameof(tbPercent));
            tbInitialTime = FindChild<EditorNumberTextBox>(nameof(tbInitialTime));

            tbHomeCell = FindChild<EditorNumberTextBox>(nameof(tbHomeCell));            
            tbHomeCell.MaximumTextLength = (Constants.MaxWaypoint - 1).ToString(CultureInfo.InvariantCulture).Length;

            selTheme = FindChild<EditorPopUpSelector>(nameof(selTheme));
            chkEndOfGame = FindChild<XNACheckBox>(nameof(chkEndOfGame));
            chkOneTimeOnly = FindChild<XNACheckBox>(nameof(chkOneTimeOnly));
            chkSkipScore = FindChild<XNACheckBox>(nameof(chkSkipScore));
            chkSkipMapSelect = FindChild<XNACheckBox>(nameof(chkSkipMapSelect));
            chkIgnoreGlobalAITriggers = FindChild<XNACheckBox>(nameof(chkIgnoreGlobalAITriggers));
            chkOfficial = FindChild<XNACheckBox>(nameof(chkOfficial));
            chkTruckCrate = FindChild<XNACheckBox>(nameof(chkTruckCrate));
            chkTrainCrate = FindChild<XNACheckBox>(nameof(chkTrainCrate));
            chkMultiplayerOnly = FindChild<XNACheckBox>(nameof(chkMultiplayerOnly));
            chkGrowingTiberium = FindChild<XNACheckBox>(nameof(chkGrowingTiberium));
            chkGrowingVeins = FindChild<XNACheckBox>(nameof(chkGrowingVeins));
            chkGrowingIce = FindChild<XNACheckBox>(nameof(chkGrowingIce));
            chkTiberiumDeathToVisceroid = FindChild<XNACheckBox>(nameof(chkTiberiumDeathToVisceroid));
            chkFreeRadar = FindChild<XNACheckBox>(nameof(chkFreeRadar));
            chkRequiredAddOn = FindChild<XNACheckBox>(nameof(chkRequiredAddOn));
            
            // 查找标签控件
            lblName = FindChild<XNALabel>(nameof(lblName));
            lblAuthor = FindChild<XNALabel>(nameof(lblAuthor));
            lblCarryOverCap = FindChild<XNALabel>(nameof(lblCarryOverCap));
            lblPercent = FindChild<XNALabel>(nameof(lblPercent));
            lblInitialTime = FindChild<XNALabel>(nameof(lblInitialTime));
            lblHomeCell = FindChild<XNALabel>(nameof(lblHomeCell));
            lblTheme = FindChild<XNALabel>(nameof(lblTheme));
            lblHeader = FindChild<XNALabel>(nameof(lblHeader));
            btnApply = FindChild<EditorButton>("btnApply");

            selectThemeWindow = new SelectThemeWindow(WindowManager, map, true);
            var themeDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectThemeWindow);
            themeDarkeningPanel.Hidden += ThemeDarkeningPanel_Hidden;

            selTheme.LeftClick += SelTheme_LeftClick;

            btnApply.LeftClick += BtnApply_LeftClick;
            
            // 初始化时刷新语言
            RefreshLanguage(MainMenu.IsChinese);
        }

        public void Open()
        {
            // 先根据当前语言设置刷新UI
            RefreshLanguage(MainMenu.IsChinese);
            
            Show();

            tbName.Text = map.Basic.Name ?? string.Empty;
            tbAuthor.Text = map.Basic.Author ?? string.Empty;
            tbCarryOverCap.Value = map.Basic.CarryOverCap;
            tbPercent.Value = map.Basic.Percent;
            tbInitialTime.Value = map.Basic.InitTime;
            tbHomeCell.Value = map.Basic.HomeCell;

            selTheme.Tag = map.Rules.Themes.GetByININame(map.Basic.Theme);
            selTheme.Text = selTheme.Tag != null ? selTheme.Tag.ToString() : Constants.NoneValue2;

            chkEndOfGame.Checked = map.Basic.EndOfGame;
            chkOneTimeOnly.Checked = map.Basic.OneTimeOnly;
            chkSkipScore.Checked = map.Basic.SkipScore;
            chkSkipMapSelect.Checked = map.Basic.SkipMapSelect;
            chkIgnoreGlobalAITriggers.Checked = map.Basic.IgnoreGlobalAITriggers;
            chkOfficial.Checked = map.Basic.Official;
            chkTruckCrate.Checked = map.Basic.TruckCrate;
            chkTrainCrate.Checked = map.Basic.TrainCrate;
            chkMultiplayerOnly.Checked = map.Basic.MultiplayerOnly;
            chkGrowingTiberium.Checked = map.Basic.TiberiumGrowthEnabled;
            chkGrowingVeins.Checked = map.Basic.VeinGrowthEnabled;
            chkGrowingIce.Checked = map.Basic.IceGrowthEnabled;
            chkTiberiumDeathToVisceroid.Checked = map.Basic.TiberiumDeathToVisceroid;
            chkFreeRadar.Checked = map.Basic.FreeRadar;
            chkRequiredAddOn.Checked = map.Basic.RequiredAddOn > 0;
        }
        
        /// <summary>
        /// 根据当前语言刷新UI文本
        /// </summary>
        public void RefreshLanguage(bool isChinese)
        {
            // 确保控件已初始化
            if (lblHeader == null || lblName == null)
                return;
                
            // 更新窗口标题和控件文本
            SetWindowTitle(isChinese ? "基本选项" : "Basic Options");
            
            // 标题
            lblHeader.Text = isChinese ? "基本选项" : "Basic Options";
            
            // 左侧标签
            lblName.Text = isChinese ? "名称：" : "Name:";
            lblAuthor.Text = isChinese ? "作者：" : "Author:";
            lblCarryOverCap.Text = isChinese ? "携带上限：" : "Carry Over Cap:";
            lblPercent.Text = isChinese ? "百分比：" : "Percent:";
            lblInitialTime.Text = isChinese ? "初始时间：" : "Initial time:";
            lblHomeCell.Text = isChinese ? "主基地单元格：" : "Home Cell:";
            lblTheme.Text = isChinese ? "主题：" : "Theme:";
            
            // 左侧复选框
            chkEndOfGame.Text = isChinese ? "游戏结束" : "End of Game";
            chkOneTimeOnly.Text = isChinese ? "仅一次（跳过显示制作人员名单）" : "One Time Only (skip displaying credits)";
            chkSkipScore.Text = isChinese ? "跳过分数屏幕" : "Skip Score Screen";
            chkSkipMapSelect.Text = isChinese ? "跳过地图选择" : "Skip Map Select";
            
            // 右侧复选框
            chkIgnoreGlobalAITriggers.Text = isChinese ? "忽略全局AI触发器" : "Ignore Global AI Triggers";
            chkOfficial.Text = isChinese ? "官方" : "Official";
            chkTruckCrate.Text = isChinese ? "卡车被摧毁时产生箱子" : "Crate From Destroyed Trucks";
            chkTrainCrate.Text = isChinese ? "火车被摧毁时产生箱子" : "Crate From Destroyed Trains";
            chkMultiplayerOnly.Text = isChinese ? "仅多人游戏" : "Multiplayer Only";
            chkGrowingTiberium.Text = isChinese ? "泰伯利亚生长" : "Growing Tiberium";
            chkGrowingVeins.Text = isChinese ? "静脉生长" : "Growing Veins";
            chkGrowingIce.Text = isChinese ? "冰生长" : "Growing Ice";
            chkTiberiumDeathToVisceroid.Text = isChinese ? "在泰伯利亚中死亡转化为内脏虫" : "Visceroid From Death In Tiberium";
            chkFreeRadar.Text = isChinese ? "免费雷达" : "Free Radar";
            chkRequiredAddOn.Text = isChinese ? "增强模式" : "Enhanced Mode";
            
            // 按钮
            btnApply.Text = isChinese ? "应用" : "Apply";
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

        private void BtnApply_LeftClick(object sender, System.EventArgs e)
        {
            Hide();

            map.Basic.Name = tbName.Text;
            map.Basic.Author = tbAuthor.Text;
            map.Basic.CarryOverCap = tbCarryOverCap.Value;
            map.Basic.Percent = tbPercent.Value;
            map.Basic.InitTime = tbInitialTime.Value;
            map.Basic.HomeCell = tbHomeCell.Value;
            map.Basic.Theme = selTheme.Tag != null ? ((Theme)selTheme.Tag).ININame : null;
            map.Basic.EndOfGame = chkEndOfGame.Checked;
            map.Basic.OneTimeOnly = chkOneTimeOnly.Checked;
            map.Basic.SkipScore = chkSkipScore.Checked;
            map.Basic.SkipMapSelect = chkSkipMapSelect.Checked;
            map.Basic.IgnoreGlobalAITriggers = chkIgnoreGlobalAITriggers.Checked;
            map.Basic.Official = chkOfficial.Checked;
            map.Basic.TruckCrate = chkTruckCrate.Checked;
            map.Basic.TrainCrate = chkTrainCrate.Checked;
            map.Basic.MultiplayerOnly = chkMultiplayerOnly.Checked;
            map.Basic.TiberiumGrowthEnabled = chkGrowingTiberium.Checked;
            map.Basic.VeinGrowthEnabled = chkGrowingVeins.Checked;
            map.Basic.IceGrowthEnabled = chkGrowingIce.Checked;
            map.Basic.TiberiumDeathToVisceroid = chkTiberiumDeathToVisceroid.Checked;
            map.Basic.FreeRadar = chkFreeRadar.Checked;
            if (!Constants.IsRA2YR)
            {
                map.Basic.RequiredAddOn = chkRequiredAddOn.Checked ? 1 : 0;
            }
        }

        private void ThemeDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            selTheme.Tag = selectThemeWindow.SelectedObject;
            selTheme.Text = selTheme.Tag != null ? selectThemeWindow.SelectedObject.ToString() : Constants.NoneValue2;
        }

        private void SelTheme_LeftClick(object sender, EventArgs e)
        {
            Theme theme = (Theme)selTheme.Tag;
            selectThemeWindow.Open(theme);
        }
    }
}
