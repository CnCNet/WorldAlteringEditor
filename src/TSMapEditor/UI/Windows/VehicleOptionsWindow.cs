using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Globalization;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI.Controls;
using TSMapEditor.UI.CursorActions;

namespace TSMapEditor.UI.Windows
{
    /// <summary>
    /// A window that allows the user to edit the properties of a vehicle.
    /// </summary>
    public class VehicleOptionsWindow : INItializableWindow
    {
        public VehicleOptionsWindow(WindowManager windowManager, Map map, EditorState editorState, ICursorActionTarget cursorActionTarget) : base(windowManager)
        {
            this.map = map;
            this.editorState = editorState;
            this.setFollowerCursorAction = new SetFollowerCursorAction(cursorActionTarget);
            this.cursorActionTarget = cursorActionTarget;
        }

        public event EventHandler<TagEventArgs> TagOpened;

        private readonly Map map;
        private readonly EditorState editorState;
        private readonly SetFollowerCursorAction setFollowerCursorAction;
        private readonly ICursorActionTarget cursorActionTarget;

        private XNATrackbar trbStrength;
        private XNALabel lblStrengthValue;
        private XNADropDown ddMission;
        private XNADropDown ddVeterancy;
        private EditorNumberTextBox tbGroup;
        private EditorPopUpSelector followerSelector;
        private XNACheckBox chkOnBridge;
        private XNACheckBox chkAutocreateNoRecruitable;
        private XNACheckBox chkAutocreateYesRecruitable;
        private EditorPopUpSelector attachedTagSelector;

        private Unit unit;

        private SelectTagWindow selectTagWindow;

        public override void Initialize()
        {
            Name = nameof(VehicleOptionsWindow);
            base.Initialize();

            trbStrength = FindChild<XNATrackbar>(nameof(trbStrength));
            lblStrengthValue = FindChild<XNALabel>(nameof(lblStrengthValue));
            ddMission = FindChild<XNADropDown>(nameof(ddMission));
            ddVeterancy = FindChild<XNADropDown>(nameof(ddVeterancy));
            tbGroup = FindChild<EditorNumberTextBox>(nameof(tbGroup));
            followerSelector = FindChild<EditorPopUpSelector>(nameof(followerSelector));
            chkOnBridge = FindChild<XNACheckBox>(nameof(chkOnBridge));
            chkAutocreateNoRecruitable = FindChild<XNACheckBox>(nameof(chkAutocreateNoRecruitable));
            chkAutocreateYesRecruitable = FindChild<XNACheckBox>(nameof(chkAutocreateYesRecruitable));
            attachedTagSelector = FindChild<EditorPopUpSelector>(nameof(attachedTagSelector));

            trbStrength.ValueChanged += TrbStrength_ValueChanged;
            attachedTagSelector.LeftClick += AttachedTagSelector_LeftClick;

            FindChild<EditorButton>("btnOpenAttachedTrigger").LeftClick += BtnOpenAttachedTrigger_LeftClick;

            selectTagWindow = new SelectTagWindow(WindowManager, map);
            var tagDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTagWindow);
            tagDarkeningPanel.Hidden += (s, e) => SelectionWindow_ApplyEffect(w => unit.AttachedTag = w.SelectedObject, selectTagWindow);

            try
            {
                ddVeterancy.Items.ForEach(ddItem => ddItem.Tag = int.Parse(ddItem.Text.Substring(0, ddItem.Text.IndexOf(' ')), CultureInfo.InvariantCulture));
            }
            catch (FormatException)
            {
                throw new INIConfigException($"Invalid options specified for {nameof(ddVeterancy)} in {nameof(VehicleOptionsWindow)}. Options must start with a number followed by a space.");
            }

            FindChild<EditorButton>("btnOK").LeftClick += BtnOK_LeftClick;
            followerSelector.LeftClick += FollowerSelector_LeftClick;
            setFollowerCursorAction.ActionExited += SetFollowerCursorAction_ActionExited;
        }

        private void TrbStrength_ValueChanged(object sender, EventArgs e)
        {
            lblStrengthValue.Text = trbStrength.Value.ToString(CultureInfo.InvariantCulture);
        }

        private void BtnOpenAttachedTrigger_LeftClick(object sender, EventArgs e)
        {
            if (unit.AttachedTag == null)
                return;

            TagOpened?.Invoke(this, new TagEventArgs(unit.AttachedTag));
            PutOnBackground();
        }

        private void SetFollowerCursorAction_ActionExited(object sender, EventArgs e)
        {
            Open(unit);
        }

        private void FollowerSelector_LeftClick(object sender, EventArgs e)
        {
            Hide();
            setFollowerCursorAction.UnitToFollow = unit;
            editorState.CursorAction = setFollowerCursorAction;
        }

        private void SelectionWindow_ApplyEffect<T>(Action<T> action, T window)
        {
            action(window);
            RefreshValues();
        }

        private void AttachedTagSelector_LeftClick(object sender, EventArgs e)
        {
            selectTagWindow.Open(unit.AttachedTag);
        }

        public void Open(Unit unit)
        {
            this.unit = unit;
            RefreshValues();
            Show();
        }

        private void RefreshValues()
        {
            trbStrength.Value = unit.HP;
            ddMission.SelectedIndex = ddMission.Items.FindIndex(item => item.Text == unit.Mission);
            int veterancyIndex = ddVeterancy.Items.FindIndex(i => (int)i.Tag == unit.Veterancy);
            ddVeterancy.SelectedIndex = Math.Max(0, veterancyIndex);
            tbGroup.Value = unit.Group;
            followerSelector.Tag = unit.FollowerUnit;
            followerSelector.Text = unit.FollowerUnit == null ? Translate(this, "None", "none") :
                string.Format(Translate(this, "FollowerSelectorText",
                    "{0} at {1}"), 
                    unit.FollowerUnit.UnitType.GetEditorDisplayName(), unit.FollowerUnit.Position);
            chkOnBridge.Checked = unit.High;
            chkAutocreateNoRecruitable.Checked = unit.AutocreateNoRecruitable;
            chkAutocreateYesRecruitable.Checked = unit.AutocreateYesRecruitable;
            attachedTagSelector.Text = unit.AttachedTag == null ? string.Empty : unit.AttachedTag.GetDisplayString();
            attachedTagSelector.Tag = unit.AttachedTag;
        }

        private void BtnOK_LeftClick(object sender, EventArgs e)
        {
            unit.HP = Math.Min(Constants.ObjectHealthMax, Math.Max(trbStrength.Value, 0));
            unit.Mission = ddMission.SelectedItem == null ? unit.Mission : ddMission.SelectedItem.Text;
            unit.Veterancy = (int)ddVeterancy.SelectedItem.Tag;
            unit.Group = tbGroup.Value;
            unit.FollowerUnit = followerSelector.Tag as Unit;

            if (unit.High != chkOnBridge.Checked)
            {
                unit.High = chkOnBridge.Checked;
                cursorActionTarget.AddRefreshPoint(unit.Position);
            }

            unit.AutocreateNoRecruitable = chkAutocreateNoRecruitable.Checked;
            unit.AutocreateYesRecruitable = chkAutocreateYesRecruitable.Checked;
            unit.AttachedTag = (Tag)attachedTagSelector.Tag;

            Hide();
        }
    }
}
