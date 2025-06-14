using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.Rendering;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class RenderedObjectsConfigurationWindow : INItializableWindow
    {
        public RenderedObjectsConfigurationWindow(WindowManager windowManager, EditorState editorState) : base(windowManager)
        {
            this.editorState = editorState;
        }

        private readonly EditorState editorState;

        private XNACheckBox chkTerrainTiles;
        private XNACheckBox chkSmudges;
        private XNACheckBox chkOverlay;
        private XNACheckBox chkAircraft;
        private XNACheckBox chkInfantry;
        private XNACheckBox chkVehicles;
        private XNACheckBox chkStructures;
        private XNACheckBox chkTerrainObjects;
        private XNACheckBox chkCellTags;
        private XNACheckBox chkWaypoints;
        private XNACheckBox chkBaseNodes;
        private XNACheckBox chkAlphaLights;
        private XNACheckBox chkTunnelTubes;

        public override void Initialize()
        {
            Name = nameof(RenderedObjectsConfigurationWindow);
            base.Initialize();

            chkTerrainTiles = FindChild<XNACheckBox>(nameof(chkTerrainTiles));
            chkSmudges = FindChild<XNACheckBox>(nameof(chkSmudges));
            chkOverlay = FindChild<XNACheckBox>(nameof(chkOverlay));
            chkAircraft = FindChild<XNACheckBox>(nameof(chkAircraft));
            chkInfantry = FindChild<XNACheckBox>(nameof(chkInfantry));
            chkVehicles = FindChild<XNACheckBox>(nameof(chkVehicles));
            chkStructures = FindChild<XNACheckBox>(nameof(chkStructures));
            chkTerrainObjects = FindChild<XNACheckBox>(nameof(chkTerrainObjects));
            chkCellTags = FindChild<XNACheckBox>(nameof(chkCellTags));
            chkWaypoints = FindChild<XNACheckBox>(nameof(chkWaypoints));
            chkBaseNodes = FindChild<XNACheckBox>(nameof(chkBaseNodes));
            chkAlphaLights = FindChild<XNACheckBox>(nameof(chkAlphaLights));
            chkTunnelTubes = FindChild<XNACheckBox>(nameof(chkTunnelTubes));

            FindChild<EditorButton>("btnApply").LeftClick += BtnApply_LeftClick;
        }

        private void BtnApply_LeftClick(object sender, EventArgs e)
        {
            RenderObjectFlags renderObjectFlags = RenderObjectFlags.None;

            if (chkTerrainTiles.Checked)   renderObjectFlags |= RenderObjectFlags.Terrain;
            if (chkSmudges.Checked)        renderObjectFlags |= RenderObjectFlags.Smudges;
            if (chkOverlay.Checked)        renderObjectFlags |= RenderObjectFlags.Overlay;
            if (chkAircraft.Checked)       renderObjectFlags |= RenderObjectFlags.Aircraft;
            if (chkInfantry.Checked)       renderObjectFlags |= RenderObjectFlags.Infantry;
            if (chkVehicles.Checked)       renderObjectFlags |= RenderObjectFlags.Vehicles;
            if (chkStructures.Checked)     renderObjectFlags |= RenderObjectFlags.Structures;
            if (chkWaypoints.Checked)      renderObjectFlags |= RenderObjectFlags.Waypoints;
            if (chkTerrainObjects.Checked) renderObjectFlags |= RenderObjectFlags.TerrainObjects;
            if (chkCellTags.Checked)       renderObjectFlags |= RenderObjectFlags.CellTags;
            if (chkWaypoints.Checked)      renderObjectFlags |= RenderObjectFlags.Waypoints;
            if (chkBaseNodes.Checked)      renderObjectFlags |= RenderObjectFlags.BaseNodes;
            if (chkAlphaLights.Checked)    renderObjectFlags |= RenderObjectFlags.AlphaLights;
            if (chkTunnelTubes.Checked)    renderObjectFlags |= RenderObjectFlags.TunnelTubes;

            editorState.RenderObjectFlags = renderObjectFlags;

            Hide();
        }

        public void Open()
        {
            SetCheckBoxStates();
            Show();
        }

        private void SetCheckBoxStates()
        {
            chkTerrainTiles.Checked = editorState.RenderObjectFlags.HasFlag(RenderObjectFlags.Terrain);
            chkSmudges.Checked = editorState.RenderObjectFlags.HasFlag(RenderObjectFlags.Smudges);
            chkOverlay.Checked = editorState.RenderObjectFlags.HasFlag(RenderObjectFlags.Overlay);
            chkAircraft.Checked = editorState.RenderObjectFlags.HasFlag(RenderObjectFlags.Aircraft);
            chkInfantry.Checked = editorState.RenderObjectFlags.HasFlag(RenderObjectFlags.Infantry);
            chkVehicles.Checked = editorState.RenderObjectFlags.HasFlag(RenderObjectFlags.Vehicles);
            chkStructures.Checked = editorState.RenderObjectFlags.HasFlag(RenderObjectFlags.Structures);
            chkTerrainObjects.Checked = editorState.RenderObjectFlags.HasFlag(RenderObjectFlags.TerrainObjects);
            chkCellTags.Checked = editorState.RenderObjectFlags.HasFlag(RenderObjectFlags.CellTags);
            chkWaypoints.Checked = editorState.RenderObjectFlags.HasFlag(RenderObjectFlags.Waypoints);
            chkBaseNodes.Checked = editorState.RenderObjectFlags.HasFlag(RenderObjectFlags.BaseNodes);
            chkAlphaLights.Checked = editorState.RenderObjectFlags.HasFlag(RenderObjectFlags.AlphaLights);
            chkTunnelTubes.Checked = editorState.RenderObjectFlags.HasFlag(RenderObjectFlags.TunnelTubes);
        }
    }
}
