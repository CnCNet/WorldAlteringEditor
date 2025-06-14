using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Globalization;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class AircraftOptionsWindow : INItializableWindow
    {
        public AircraftOptionsWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        public event EventHandler<TagEventArgs> TagOpened;

        private readonly Map map;

        private XNATrackbar trbStrength;
        private XNALabel lblStrengthValue;
        private XNADropDown ddMission;
        private XNADropDown ddVeterancy;
        private EditorNumberTextBox tbGroup;
        private XNACheckBox chkAutocreateNoRecruitable;
        private XNACheckBox chkAutocreateYesRecruitable;
        private EditorPopUpSelector attachedTagSelector;

        private Aircraft aircraft;

        private SelectTagWindow selectTagWindow;

        public override void Initialize()
        {
            Name = nameof(AircraftOptionsWindow);
            base.Initialize();

            trbStrength = FindChild<XNATrackbar>(nameof(trbStrength));
            lblStrengthValue = FindChild<XNALabel>(nameof(lblStrengthValue));
            ddMission = FindChild<XNADropDown>(nameof(ddMission));
            ddVeterancy = FindChild<XNADropDown>(nameof(ddVeterancy));
            tbGroup = FindChild<EditorNumberTextBox>(nameof(tbGroup));
            chkAutocreateNoRecruitable = FindChild<XNACheckBox>(nameof(chkAutocreateNoRecruitable));
            chkAutocreateYesRecruitable = FindChild<XNACheckBox>(nameof(chkAutocreateYesRecruitable));
            attachedTagSelector = FindChild<EditorPopUpSelector>(nameof(attachedTagSelector));

            trbStrength.ValueChanged += TrbStrength_ValueChanged;
            attachedTagSelector.LeftClick += AttachedTagSelector_LeftClick;

            FindChild<EditorButton>("btnOpenAttachedTrigger").LeftClick += BtnOpenAttachedTrigger_LeftClick;

            selectTagWindow = new SelectTagWindow(WindowManager, map);
            var tagDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTagWindow);
            tagDarkeningPanel.Hidden += (s, e) =>
            {
                var tag = selectTagWindow.SelectedObject;
                attachedTagSelector.Tag = tag;
                attachedTagSelector.Text = tag == null ? string.Empty : tag.GetDisplayString();
            };

            try
            {
                ddVeterancy.Items.ForEach(ddItem => ddItem.Tag = int.Parse(ddItem.Text.Substring(0, ddItem.Text.IndexOf(' ')), CultureInfo.InvariantCulture));
            }
            catch (FormatException)
            {
                throw new INIConfigException($"Invalid options specified for {nameof(ddVeterancy)} in {nameof(InfantryOptionsWindow)}. Options must start with a number followed by a space.");
            }

            FindChild<EditorButton>("btnOK").LeftClick += BtnOK_LeftClick;
        }

        private void TrbStrength_ValueChanged(object sender, EventArgs e)
        {
            lblStrengthValue.Text = trbStrength.Value.ToString(CultureInfo.InvariantCulture);
        }

        private void BtnOpenAttachedTrigger_LeftClick(object sender, EventArgs e)
        {
            if (aircraft.AttachedTag == null)
                return;

            TagOpened?.Invoke(this, new TagEventArgs(aircraft.AttachedTag));
            PutOnBackground();
        }

        private void AttachedTagSelector_LeftClick(object sender, EventArgs e)
        {
            selectTagWindow.Open(aircraft.AttachedTag);
        }

        public void Open(Aircraft aircraft)
        {
            this.aircraft = aircraft;
            RefreshValues();
            Show();
        }

        private void RefreshValues()
        {
            trbStrength.Value = aircraft.HP;
            ddMission.SelectedIndex = ddMission.Items.FindIndex(item => item.Text == aircraft.Mission);
            int veterancyIndex = ddVeterancy.Items.FindIndex(i => (int)i.Tag == aircraft.Veterancy);
            ddVeterancy.SelectedIndex = Math.Max(0, veterancyIndex);
            tbGroup.Value = aircraft.Group;
            chkAutocreateNoRecruitable.Checked = aircraft.AutocreateNoRecruitable;
            chkAutocreateYesRecruitable.Checked = aircraft.AutocreateYesRecruitable;
            attachedTagSelector.Text = aircraft.AttachedTag == null ? string.Empty : aircraft.AttachedTag.GetDisplayString();
            attachedTagSelector.Tag = aircraft.AttachedTag;
        }

        private void BtnOK_LeftClick(object sender, EventArgs e)
        {
            aircraft.HP = Math.Min(Constants.ObjectHealthMax, Math.Max(trbStrength.Value, 0));
            aircraft.Mission = ddMission.SelectedItem == null ? aircraft.Mission : ddMission.SelectedItem.Text;
            aircraft.Veterancy = (int)ddVeterancy.SelectedItem.Tag;
            aircraft.Group = tbGroup.Value;
            aircraft.AutocreateNoRecruitable = chkAutocreateNoRecruitable.Checked;
            aircraft.AutocreateYesRecruitable = chkAutocreateYesRecruitable.Checked;
            aircraft.AttachedTag = (Tag)attachedTagSelector.Tag;

            Hide();
        }
    }
}
