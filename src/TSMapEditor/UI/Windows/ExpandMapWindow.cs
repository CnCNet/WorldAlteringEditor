using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    /// <summary>
    /// A window that allows the user to expand the map.
    /// </summary>
    public class ExpandMapWindow : INItializableWindow
    {
        public ExpandMapWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private XNALabel lblHeader;
        private XNALabel lblInstructions;
        private XNALabel lblCurrentMapSize;
        private XNALabel lblExpandNorth;
        private XNALabel lblExpandSouth;
        private XNALabel lblExpandEast;
        private XNALabel lblExpandWest;
        private EditorNumberTextBox tbExpandNorth;
        private EditorNumberTextBox tbExpandSouth;
        private EditorNumberTextBox tbExpandEast;
        private EditorNumberTextBox tbExpandWest;
        private EditorButton btnApply;

        public override void Initialize()
        {
            Name = nameof(ExpandMapWindow);
            base.Initialize();

            lblHeader = FindChild<XNALabel>("lblHeader");
            lblInstructions = FindChild<XNALabel>("lblInstructions");
            lblCurrentMapSize = FindChild<XNALabel>(nameof(lblCurrentMapSize));
            lblExpandNorth = FindChild<XNALabel>("lblExpandNorth");
            lblExpandSouth = FindChild<XNALabel>("lblExpandSouth");
            lblExpandEast = FindChild<XNALabel>("lblExpandEast");
            lblExpandWest = FindChild<XNALabel>("lblExpandWest");
            tbExpandNorth = FindChild<EditorNumberTextBox>(nameof(tbExpandNorth));
            tbExpandSouth = FindChild<EditorNumberTextBox>(nameof(tbExpandSouth));
            tbExpandEast = FindChild<EditorNumberTextBox>(nameof(tbExpandEast));
            tbExpandWest = FindChild<EditorNumberTextBox>(nameof(tbExpandWest));
            btnApply = FindChild<EditorButton>("btnApply");
            
            btnApply.LeftClick += BtnApply_LeftClick;
            
            // 初始化语言
            RefreshLanguage(MainMenu.IsChinese);
        }

        private void BtnApply_LeftClick(object sender, EventArgs e)
        {
            int newWidth = map.Size.X + tbExpandEast.Value + tbExpandWest.Value;
            if (newWidth <= 0 || newWidth > Constants.MaxMapWidth)
            {
                EditorMessageBox.Show(WindowManager, MainMenu.IsChinese ? "无效的宽度" : "Invalid width",
                    MainMenu.IsChinese ? 
                        $"给定的值将使地图宽度为{newWidth}。\r\n宽度应在1到{Constants.MaxMapWidth}之间。" : 
                        $"The given values would make the map's width {newWidth}.\r\nIt should be between 1 and {Constants.MaxMapWidth}.",
                    MessageBoxButtons.OK);

                return;
            }

            int newHeight = map.Size.Y + tbExpandNorth.Value + tbExpandSouth.Value;
            if (newHeight <= 0 || newHeight > Constants.MaxMapHeight)
            {
                EditorMessageBox.Show(WindowManager, MainMenu.IsChinese ? "无效的高度" : "Invalid height",
                    MainMenu.IsChinese ? 
                        $"给定的值将使地图高度为{newHeight}。\r\n高度应在1到{Constants.MaxMapHeight}之间。" : 
                        $"The given values would make the map's height {newHeight}.\r\nIt should be between 0 and {Constants.MaxMapHeight}.",
                    MessageBoxButtons.OK);

                return;
            }

            int expandNorth = tbExpandNorth.Value;
            int expandEast = tbExpandEast.Value;
            int expandWest = tbExpandWest.Value;

            // Determine shift for expanding map to north.
            // Expanding to the south doesn't require any changes to existing coords.
            // These shifts consider the in-game compass; shifting east means
            // increasing the X coord, while shifting south means increasing the Y coord.
            int eastShift = expandNorth;
            int southShift = expandNorth;

            // Determine shift for expanding map to east
            southShift += expandEast;

            // Determine shift for expanding map to west
            eastShift += expandWest;

            map.Resize(new Point2D(newWidth, newHeight), eastShift, southShift);
            Hide();
        }
        
        /// <summary>
        /// 刷新窗口中的语言
        /// </summary>
        public void RefreshLanguage(bool isChinese)
        {
            // 设置窗口标题
            if (lblHeader != null)
                lblHeader.Text = isChinese ? "调整地图大小" : "Resize Map";
                
            // 设置说明文本
            if (lblInstructions != null)
            {
                lblInstructions.Text = isChinese ? 
                    "定义地图在不同方向上应该扩展的大小。\n\n" +
                    "您可以使用负数来缩小地图的尺寸。\n\n" +
                    "此操作没有撤销功能，请确保先保存您的地图！" : 
                    "Define how much the map should be expanded to in\n" +
                    "different directions.\n\n" +
                    "You can use negative numbers to reduce the map's size.\n\n" +
                    "No un-do is available, so make sure to save your map first!";
            }
            
            // 更新当前地图大小文本格式
            UpdateCurrentMapSizeText();
            
            // 设置方向标签文本
            if (lblExpandNorth != null)
                lblExpandNorth.Text = isChinese ? "向北(上)扩展:" : "Expand to north (up) by:";
                
            if (lblExpandSouth != null)
                lblExpandSouth.Text = isChinese ? "向南(下)扩展:" : "Expand to south (down) by:";
                
            if (lblExpandEast != null)
                lblExpandEast.Text = isChinese ? "向东(右)扩展:" : "Expand to east (right) by:";
                
            if (lblExpandWest != null)
                lblExpandWest.Text = isChinese ? "向西(左)扩展:" : "Expand to west (left) by:";
                
            // 设置按钮文本
            if (btnApply != null)
                btnApply.Text = isChinese ? "应用" : "Apply";
        }
        
        /// <summary>
        /// 更新当前地图大小的文本
        /// </summary>
        private void UpdateCurrentMapSizeText()
        {
            if (lblCurrentMapSize != null)
                lblCurrentMapSize.Text = MainMenu.IsChinese ? 
                    $"当前地图大小: {map.Size.X}x{map.Size.Y}" : 
                    $"Current map size: {map.Size.X}x{map.Size.Y}";
        }

        public void Open()
        {
            // 确保使用当前语言设置
            RefreshLanguage(MainMenu.IsChinese);
            
            // 更新地图大小信息
            UpdateCurrentMapSizeText();
            
            Show();
        }
    }
}
