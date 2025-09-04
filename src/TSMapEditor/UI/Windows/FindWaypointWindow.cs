using Rampastring.XNAUI;
using System;
using TSMapEditor.Extensions;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class FindWaypointWindow : INItializableWindow
    {
        public FindWaypointWindow(WindowManager windowManager, Map map, IMapView mapView) : base(windowManager)
        {
            this.map = map;
            this.mapView = mapView;
        }

        private readonly Map map;
        private readonly IMapView mapView;

        private EditorNumberTextBox tbWaypoint;

        public override void Initialize()
        {
            Name = nameof(FindWaypointWindow);
            base.Initialize();

            tbWaypoint = FindChild<EditorNumberTextBox>(nameof(tbWaypoint));
            FindChild<EditorButton>("btnFind").LeftClick += BtnFind_LeftClick;
        }

        public void Open()
        {
            tbWaypoint.Text = string.Empty;
            Show();
        }

        private void BtnFind_LeftClick(object sender, EventArgs e)
        {
            int waypointNumber = tbWaypoint.Value;

            Waypoint waypoint = map.Waypoints.Find(wp => wp.Identifier == waypointNumber);
            if (waypoint == null)
            {
                EditorMessageBox.Show(WindowManager, "Waypoint not found".L10N(),
                    "Waypoint #".L10N() + waypointNumber + " does not exist on the map!".L10N(), MessageBoxButtons.OK);

                return;
            }

            Hide();
            mapView.Camera.CenterOnCell(waypoint.Position);
        }
    }
}
