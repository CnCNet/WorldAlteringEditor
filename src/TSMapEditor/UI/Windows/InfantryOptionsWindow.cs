using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Globalization;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class InfantryOptionsWindow : INItializableWindow
    {
        public InfantryOptionsWindow(WindowManager windowManager, Map map, IMapView mapView) : base(windowManager)
        {
            this.map = map;
            this.mapView = mapView;
        }

        public event EventHandler<TagEventArgs> TagOpened;

        private readonly Map map;
        private readonly IMapView mapView;

        private XNALabel lblSelectedInfantryValue;
        private XNATrackbar trbStrength;
        private XNALabel lblStrengthValue;
        private XNADropDown ddMission;
        private XNADropDown ddVeterancy;
        private XNADropDown ddSubCell;
        private EditorNumberTextBox tbGroup;
        private XNACheckBox chkOnBridge;
        private XNACheckBox chkAutocreateNoRecruitable;
        private XNACheckBox chkAutocreateYesRecruitable;
        private EditorPopUpSelector attachedTagSelector;

        private Infantry infantry;

        private SelectTagWindow selectTagWindow;

        public override void Initialize()
        {
            Name = nameof(InfantryOptionsWindow);
            base.Initialize();

            lblSelectedInfantryValue = FindChild<XNALabel>(nameof(lblSelectedInfantryValue));
            trbStrength = FindChild<XNATrackbar>(nameof(trbStrength));
            lblStrengthValue = FindChild<XNALabel>(nameof(lblStrengthValue));
            ddMission = FindChild<XNADropDown>(nameof(ddMission));
            ddVeterancy = FindChild<XNADropDown>(nameof(ddVeterancy));
            ddSubCell = FindChild<XNADropDown>(nameof(ddSubCell));
            tbGroup = FindChild<EditorNumberTextBox>(nameof(tbGroup));
            chkOnBridge = FindChild<XNACheckBox>(nameof(chkOnBridge));
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
            if (infantry.AttachedTag == null)
                return;

            TagOpened?.Invoke(this, new TagEventArgs(infantry.AttachedTag));
            PutOnBackground();
        }

        private void AttachedTagSelector_LeftClick(object sender, EventArgs e)
        {
            selectTagWindow.Open(infantry.AttachedTag);
        }

        public void Open(Infantry infantry)
        {
            this.infantry = infantry;
            RefreshValues();
            Show();
        }

        private void RefreshValues()
        {
            lblSelectedInfantryValue.Text = infantry.ObjectType.GetEditorDisplayName() + ", sub cell: " + infantry.SubCell;
            trbStrength.Value = infantry.HP;
            ddMission.SelectedIndex = ddMission.Items.FindIndex(item => item.Text == infantry.Mission);
            int veterancyIndex = ddVeterancy.Items.FindIndex(i => (int)i.Tag == infantry.Veterancy);
            ddVeterancy.SelectedIndex = Math.Max(0, veterancyIndex);
            ddSubCell.SelectedIndex = (int)infantry.SubCell;
            tbGroup.Value = infantry.Group;
            chkOnBridge.Checked = infantry.High;
            chkAutocreateNoRecruitable.Checked = infantry.AutocreateNoRecruitable;
            chkAutocreateYesRecruitable.Checked = infantry.AutocreateYesRecruitable;
            attachedTagSelector.Text = infantry.AttachedTag == null ? string.Empty : infantry.AttachedTag.GetDisplayString();
            attachedTagSelector.Tag = infantry.AttachedTag;
        }

        private void BtnOK_LeftClick(object sender, EventArgs e)
        {
            infantry.HP = Math.Min(Constants.ObjectHealthMax, Math.Max(trbStrength.Value, 0));
            infantry.Mission = ddMission.SelectedItem == null ? infantry.Mission : ddMission.SelectedItem.Text;
            infantry.Veterancy = (int)ddVeterancy.SelectedItem.Tag;

            if (ddSubCell.SelectedIndex != (int)infantry.SubCell && ddSubCell.SelectedIndex >= 0 && ddSubCell.SelectedIndex < (int)SubCell.Count)
            {
                var infantryCell = map.GetTile(infantry.Position);
                if (infantryCell != null)
                {
                    infantryCell.MoveInfantryToSubCell(infantry, (SubCell)ddSubCell.SelectedIndex);
                }

                mapView.AddRefreshPoint(infantryCell.CoordsToPoint());
            }

            infantry.Group = tbGroup.Value;
            infantry.High = chkOnBridge.Checked;
            infantry.AutocreateNoRecruitable = chkAutocreateNoRecruitable.Checked;
            infantry.AutocreateYesRecruitable = chkAutocreateYesRecruitable.Checked;
            infantry.AttachedTag = (Tag)attachedTagSelector.Tag;

            Hide();
        }
    }
}

