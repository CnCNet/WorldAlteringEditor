using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.Misc;
using TSMapEditor.Rendering;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class DeletionModeConfigurationWindow : INItializableWindow
    {
        public DeletionModeConfigurationWindow(WindowManager windowManager, EditorState editorState) : base(windowManager)
        {
            this.editorState = editorState;
        }

        private readonly EditorState editorState;

        private XNACheckBox chkCellTags;
        private XNACheckBox chkWaypoints;
        private XNACheckBox chkAircraft;
        private XNACheckBox chkInfantry;
        private XNACheckBox chkVehicles;
        private XNACheckBox chkStructures;
        private XNACheckBox chkTerrainObjects;

        public override void Initialize()
        {
            Name = nameof(DeletionModeConfigurationWindow);
            base.Initialize();

            chkCellTags = FindChild<XNACheckBox>(nameof(chkCellTags));
            chkWaypoints = FindChild<XNACheckBox>(nameof(chkWaypoints));
            chkAircraft = FindChild<XNACheckBox>(nameof(chkAircraft));
            chkInfantry = FindChild<XNACheckBox>(nameof(chkInfantry));
            chkVehicles = FindChild<XNACheckBox>(nameof(chkVehicles));
            chkStructures = FindChild<XNACheckBox>(nameof(chkStructures));
            chkTerrainObjects = FindChild<XNACheckBox>(nameof(chkTerrainObjects));

            FindChild<EditorButton>("btnApply").LeftClick += BtnApply_LeftClick;
        }

        private void BtnApply_LeftClick(object sender, EventArgs e)
        {
            DeletionMode deletionMode = DeletionMode.None;

            if (chkCellTags.Checked)       deletionMode |= DeletionMode.CellTags;
            if (chkWaypoints.Checked)      deletionMode |= DeletionMode.Waypoints;
            if (chkAircraft.Checked)       deletionMode |= DeletionMode.Aircraft;
            if (chkInfantry.Checked)       deletionMode |= DeletionMode.Infantry;
            if (chkVehicles.Checked)       deletionMode |= DeletionMode.Vehicles;
            if (chkStructures.Checked)     deletionMode |= DeletionMode.Structures;
            if (chkTerrainObjects.Checked) deletionMode |= DeletionMode.TerrainObjects;

            editorState.DeletionMode = deletionMode;

            Hide();
        }

        public void Open()
        {
            SetCheckBoxStates();
            Show();
        }

        private void SetCheckBoxStates()
        {
            chkCellTags.Checked = editorState.DeletionMode.HasFlag(DeletionMode.CellTags);
            chkWaypoints.Checked = editorState.DeletionMode.HasFlag(DeletionMode.Waypoints);
            chkAircraft.Checked = editorState.DeletionMode.HasFlag(DeletionMode.Aircraft);
            chkInfantry.Checked = editorState.DeletionMode.HasFlag(DeletionMode.Infantry);
            chkVehicles.Checked = editorState.DeletionMode.HasFlag(DeletionMode.Vehicles);
            chkStructures.Checked = editorState.DeletionMode.HasFlag(DeletionMode.Structures);
            chkTerrainObjects.Checked = editorState.DeletionMode.HasFlag(DeletionMode.TerrainObjects);
        }
    }
}
