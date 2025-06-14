using Rampastring.XNAUI;
using System;
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
        private EditorButton btnFind;

        public override void Initialize()
        {
            Name = nameof(FindWaypointWindow);
            base.Initialize();

            tbWaypoint = FindChild<EditorNumberTextBox>(nameof(tbWaypoint));
            btnFind = FindChild<EditorButton>("btnFind");
            btnFind.LeftClick += BtnFind_LeftClick;
            
            RefreshLanguage();
        }
        
        public void RefreshLanguage()
        {
            bool isChinese = TSMapEditor.UI.MainMenu.IsChinese;
            
            // 设置窗口标题
            Text = isChinese ? "查找航点" : "Find Waypoint";
            
            // 设置按钮文本
            if (btnFind != null)
                btnFind.Text = isChinese ? "查找" : "Find";
        }

        public void Open()
        {
            tbWaypoint.Text = string.Empty;
            RefreshLanguage();
            Show();
        }

        private void BtnFind_LeftClick(object sender, EventArgs e)
        {
            int waypointNumber = tbWaypoint.Value;

            Waypoint waypoint = map.Waypoints.Find(wp => wp.Identifier == waypointNumber);
            if (waypoint == null)
            {
                bool isChinese = TSMapEditor.UI.MainMenu.IsChinese;
                string title = isChinese ? "未找到航点" : "Waypoint not found";
                string message = isChinese ? 
                    "地图上不存在航点 #" + waypointNumber + "！" : 
                    "Waypoint #" + waypointNumber + " does not exist on the map!";
                
                EditorMessageBox.Show(WindowManager, title, message, MessageBoxButtons.OK);

                return;
            }

            Hide();
            mapView.Camera.CenterOnCell(waypoint.Position);
        }
    }
}
