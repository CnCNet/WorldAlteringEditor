using System;
using System.Globalization;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class PlaceWaypointWindow : INItializableWindow
    {
        public PlaceWaypointWindow(WindowManager windowManager, Map map, MutationManager mutationManager, IMutationTarget mutationTarget) : base(windowManager)
        {
            this.map = map;
            this.mutationManager = mutationManager;
            this.mutationTarget = mutationTarget;
        }

        private readonly Map map;
        private readonly MutationManager mutationManager;
        private readonly IMutationTarget mutationTarget;

        private EditorNumberTextBox tbWaypointNumber;
        private XNALabel lblDescription;
        private XNADropDown ddWaypointColor;

        private Point2D cellCoords;

        public override void Initialize()
        {
            Name = nameof(PlaceWaypointWindow);
            base.Initialize();

            tbWaypointNumber = FindChild<EditorNumberTextBox>(nameof(tbWaypointNumber));
            tbWaypointNumber.MaximumTextLength = (Constants.MaxWaypoint - 1).ToString(CultureInfo.InvariantCulture).Length;

            lblDescription = FindChild<XNALabel>(nameof(lblDescription));
            lblDescription.Text = string.Format(Translate(this, "DescriptionText", "Input waypoint number (0-{0}):"), Constants.MaxWaypoint - 1);

            FindChild<EditorButton>("btnPlace").LeftClick += BtnPlace_LeftClick;

            // Init color dropdown options
            ddWaypointColor = FindChild<XNADropDown>(nameof(ddWaypointColor));
            ddWaypointColor.AddItem(Translate(this, "None", "None"));
            Array.ForEach(Waypoint.SupportedColors, sc => ddWaypointColor.AddItem(Translate("NamedColors." + sc.Name, sc.Name), sc.Value));
        }

        private void BtnPlace_LeftClick(object sender, EventArgs e)
        {
            // Cancel dialog if the user leaves the text box empty
            if (tbWaypointNumber.Text == string.Empty)
            {
                Hide();
                return;
            }

            int waypointNumber = tbWaypointNumber.Value;

            PlaceWaypoint(waypointNumber, cellCoords);
            Hide();
        }

        public void PlaceWaypoint(int waypointNumber, Point2D cellCoords)
        {
            if (waypointNumber < 0 || waypointNumber >= Constants.MaxWaypoint)
                return;

            if (map.Waypoints.Exists(w => w.Identifier == waypointNumber))
            {
                EditorMessageBox.Show(WindowManager,
                    Translate(this, "WaypointExists.Title", "Waypoint already exists"),
                    string.Format(Translate(this, "WaypointExists.Description",
                        "A waypoint with the given number {0} already exists on the map!"), waypointNumber),
                    MessageBoxButtons.OK);

                return;
            }

            string waypointColor = ddWaypointColor.SelectedItem != null ? ddWaypointColor.SelectedItem.Text : null;

            mutationManager.PerformMutation(new PlaceWaypointMutation(mutationTarget, cellCoords, waypointNumber, waypointColor));
        }

        public void Open(Point2D cellCoords)
        {
            this.cellCoords = cellCoords;

            int availableWaypointNumber = GetAvailableWaypointNumber();
            if (availableWaypointNumber < 0)
                return;

            tbWaypointNumber.Value = availableWaypointNumber;

            Show();
        }

        public int GetAvailableWaypointNumber()
        {
            if (map.Waypoints.Count == Constants.MaxWaypoint)
            {
                EditorMessageBox.Show(WindowManager,
                    Translate(this, "MaxWaypoints.Title", "Maximum waypoints reached"),
                    Translate(this, "MaxWaypoints.Description", "All valid waypoints on the map are already in use!"),
                    MessageBoxButtons.OK);

                return -1;
            }

            for (int i = 0; i < Constants.MaxWaypoint; i++)
            {
                if (!map.Waypoints.Exists(w => w.Identifier == i) && (Constants.IsRA2YR || i != Constants.TS_WAYPT_SPECIAL))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
