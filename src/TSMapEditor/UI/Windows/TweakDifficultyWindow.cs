
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.UI.Controls;
using TSMapEditor.Models;
using System;
using System.Collections.Generic;

namespace TSMapEditor.UI.Windows
{
    public class TweakDifficultyWindow : INItializableWindow
    {
        public TweakDifficultyWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private Map map;
        private XNACheckBox chkOverrideTeamDelays;
        private XNALabel lblTeamDelayHard;
        private XNALabel lblTeamDelayMedium;
        private XNALabel lblTeamDelayEasy;
        private EditorNumberTextBox tbTeamDelayHard;
        private EditorNumberTextBox tbTeamDelayMedium;
        private EditorNumberTextBox tbTeamDelayEasy;
        private EditorButton btnApply;

        private List<XNAControl> teamDelayControls = [];

        public override void Initialize()
        {
            Name = nameof(TweakDifficultyWindow);
            base.Initialize();

            chkOverrideTeamDelays = FindChild<XNACheckBox>(nameof(chkOverrideTeamDelays));

            lblTeamDelayHard = FindChild<XNALabel>(nameof(lblTeamDelayHard));
            lblTeamDelayMedium = FindChild<XNALabel>(nameof(lblTeamDelayMedium));
            lblTeamDelayEasy = FindChild<XNALabel>(nameof(lblTeamDelayEasy));

            tbTeamDelayHard = FindChild<EditorNumberTextBox>(nameof(tbTeamDelayHard));
            tbTeamDelayMedium = FindChild<EditorNumberTextBox>(nameof(tbTeamDelayMedium));
            tbTeamDelayEasy = FindChild<EditorNumberTextBox>(nameof(tbTeamDelayEasy));
            btnApply = FindChild<EditorButton>(nameof(btnApply));

            teamDelayControls.AddRange([
                lblTeamDelayHard, lblTeamDelayMedium, lblTeamDelayEasy, 
                tbTeamDelayHard, tbTeamDelayMedium, tbTeamDelayEasy
            ]);            

            chkOverrideTeamDelays.CheckedChanged += ChkOverrideTeamDelays_CheckedChanged;
            btnApply.LeftClick += BtnApply_LeftClick;
        }

        private void ChkOverrideTeamDelays_CheckedChanged(object sender, EventArgs e)
        {
            foreach (var teamDelayControl in teamDelayControls)
            {
                if (chkOverrideTeamDelays.Checked)
                    teamDelayControl.Enable();
                else
                    teamDelayControl.Disable();
            }
        }

        private void BtnApply_LeftClick(object sender, InputEventArgs e)
        {
            if (!chkOverrideTeamDelays.Checked)
            {
                map.WriteTeamDelays = false;
            } 
            else
            {
                if (!IsValidTeamDelay(tbTeamDelayHard) ||
                    !IsValidTeamDelay(tbTeamDelayMedium) ||
                    !IsValidTeamDelay(tbTeamDelayEasy))
                {
                    EditorMessageBox.Show(WindowManager,
                        Translate(this, "InvalidTeamDelay.Title", "Invalid Team Delay"),
                        Translate(this, "InvalidTeamDelay.Description", "One or more of the team delay values are not valid"),
                        MessageBoxButtons.OK);

                    return;
                }

                map.WriteTeamDelays = true;
                map.TeamDelays = [tbTeamDelayHard.Text, tbTeamDelayMedium.Text, tbTeamDelayEasy.Text];
            }

            Hide();
        }

        public void Open()
        {
            LoadTeamDelayValues();
            Show();
        }

        private void LoadTeamDelayValues()
        {
            if (map.WriteTeamDelays)
            {
                chkOverrideTeamDelays.Checked = true;
                foreach (var teamDelayControl in teamDelayControls)
                {
                    teamDelayControl.Enable();
                }
            }
            else
            {
                chkOverrideTeamDelays.Checked = false;
                foreach (var teamDelayControl in teamDelayControls)
                {
                    teamDelayControl.Disable();
                }
            }

            var teamDelays = map.TeamDelays;
            tbTeamDelayHard.Text = teamDelays == null ? string.Empty : teamDelays[0];
            tbTeamDelayMedium.Text = teamDelays == null ? string.Empty : teamDelays[1];
            tbTeamDelayEasy.Text = teamDelays == null ? string.Empty : teamDelays[2];
        }

        private bool IsValidTeamDelay(EditorNumberTextBox teamDelayTextBox)
        {
            int teamDelay = teamDelayTextBox.Value;
            return teamDelay > 0;
        }
    }
}
