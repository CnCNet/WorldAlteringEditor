using System;
using System.Globalization;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.UI.Controls;
using TSMapEditor.UI;

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
            RefreshLanguage();

            FindChild<EditorButton>("btnPlace").LeftClick += BtnPlace_LeftClick;

            // Init color dropdown options
            ddWaypointColor = FindChild<XNADropDown>(nameof(ddWaypointColor));
            ddWaypointColor.AddItem(MainMenu.IsChinese ? "无" : "None");
            Array.ForEach(Waypoint.SupportedColors, sc => ddWaypointColor.AddItem(MainMenu.IsChinese ? GetColorTranslation(sc.Name) : sc.Name, sc.Value));
        }

        private void BtnPlace_LeftClick(object sender, EventArgs e)
        {
            // Cancel dialog if the user leaves the text box empty
            if (tbWaypointNumber.Text == string.Empty)
            {
                Hide();
                return;
            }

            if (tbWaypointNumber.Value < 0 || tbWaypointNumber.Value >= Constants.MaxWaypoint)
                return;

            if (map.Waypoints.Exists(w => w.Identifier == tbWaypointNumber.Value))
            {
                EditorMessageBox.Show(WindowManager,
                    MainMenu.IsChinese ? "航点已存在" : "Waypoint already exists",
                    MainMenu.IsChinese ? 
                        $"编号为 {tbWaypointNumber.Value} 的航点已经存在于地图上！" : 
                        $"A waypoint with the given number {tbWaypointNumber.Value} already exists on the map!",
                    MessageBoxButtons.OK);

                return;
            }

            string waypointColor = ddWaypointColor.SelectedItem != null ? ddWaypointColor.SelectedItem.Text : null;

            mutationManager.PerformMutation(new PlaceWaypointMutation(mutationTarget, cellCoords, tbWaypointNumber.Value, waypointColor));

            Hide();
        }

        public void Open(Point2D cellCoords)
        {
            this.cellCoords = cellCoords;

            if (map.Waypoints.Count == Constants.MaxWaypoint)
            {
                EditorMessageBox.Show(WindowManager,
                    MainMenu.IsChinese ? "已达到最大航点数" : "Maximum waypoints reached",
                    MainMenu.IsChinese ? "地图上所有有效的航点已被使用！" : "All valid waypoints on the map are already in use!",
                    MessageBoxButtons.OK);

                return;
            }

            for (int i = 0; i < Constants.MaxWaypoint; i++)
            {
                if (!map.Waypoints.Exists(w => w.Identifier == i) && (Constants.IsRA2YR || i != Constants.TS_WAYPT_SPECIAL))
                {
                    tbWaypointNumber.Value = i;
                    break;
                }
            }

            Show();
        }

        public void RefreshLanguage()
        {
            bool isChinese = MainMenu.IsChinese;
            
            // 更新标签文本
            lblDescription.Text = isChinese ? 
                $"输入航点编号 (0-{Constants.MaxWaypoint - 1}):" : 
                $"Input waypoint number (0-{Constants.MaxWaypoint - 1}):";
            
            var lblWaypointColor = FindChild<XNALabel>("lblWaypointColor");
            if (lblWaypointColor != null)
                lblWaypointColor.Text = isChinese ? "颜色:" : "Color:";
            
            var btnPlace = FindChild<EditorButton>("btnPlace");
            if (btnPlace != null)
                btnPlace.Text = isChinese ? "放置" : "Place";
            
            // 如果下拉框已经初始化，更新其选项
            if (ddWaypointColor != null && ddWaypointColor.Items.Count > 0)
            {
                // 保存当前选中的项
                int selectedIndex = ddWaypointColor.SelectedIndex;
                
                // 清空并重新添加项目
                ddWaypointColor.Items.Clear();
                ddWaypointColor.AddItem(isChinese ? "无" : "None");
                Array.ForEach(Waypoint.SupportedColors, sc => ddWaypointColor.AddItem(isChinese ? GetColorTranslation(sc.Name) : sc.Name, sc.Value));
                
                // 恢复选中项
                if (selectedIndex >= 0 && selectedIndex < ddWaypointColor.Items.Count)
                    ddWaypointColor.SelectedIndex = selectedIndex;
            }
        }
        
        private string GetColorTranslation(string englishColorName)
        {
            switch (englishColorName)
            {
                case "Red": return "红色";
                case "Blue": return "蓝色";
                case "Green": return "绿色";
                case "Yellow": return "黄色";
                case "Orange": return "橙色";
                case "Purple": return "紫色";
                case "Pink": return "粉色";
                case "Brown": return "棕色";
                case "Teal": return "青色";
                case "Dark Green": return "深绿色";
                case "Lime Green": return "亮绿色";
                case "Blood Red": return "血红色";
                case "Cherry": return "樱桃色";
                case "Sky Blue": return "天蓝色";
                case "Metailic": return "金属色";
                default: return englishColorName;
            }
        }
    }
}
