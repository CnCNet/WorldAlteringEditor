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

            selectThemeWindow = new SelectThemeWindow(WindowManager, map, true);
            var themeDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectThemeWindow);
            themeDarkeningPanel.Hidden += ThemeDarkeningPanel_Hidden;

            selTheme.LeftClick += SelTheme_LeftClick;

            FindChild<EditorButton>("btnApply").LeftClick += BtnApply_LeftClick;
        }

        public void Open()
        {
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
