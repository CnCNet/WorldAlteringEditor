using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    /// <summary>
    /// A window that allows the user to view and edit the map's size and visible area.
    /// </summary>
    public class MapSizeWindow : INItializableWindow
    {
        public MapSizeWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        public event EventHandler OnResizeMapButtonClicked;

        private readonly Map map;

        private EditorNumberTextBox tbMapWidth;
        private EditorNumberTextBox tbMapHeight;
        private XNALabel lblTotalCellsValue;
        private EditorNumberTextBox tbX;
        private EditorNumberTextBox tbY;
        private EditorNumberTextBox tbWidth;
        private EditorNumberTextBox tbHeight;
        private XNALabel lblHeader;
        private XNALabel lblMapWidth;
        private XNALabel lblMapHeight;
        private XNALabel lblTotalCells;
        private XNALabel lblVisibleMapArea;
        private XNALabel lblX;
        private XNALabel lblY;
        private XNALabel lblWidth;
        private XNALabel lblHeight;
        private EditorButton btnChangeMapSize;
        private EditorButton btnApplyChanges;


        public override void Initialize()
        {
            Name = nameof(MapSizeWindow);
            base.Initialize();

            tbMapWidth = FindChild<EditorNumberTextBox>(nameof(tbMapWidth));
            tbMapWidth.Enabled = false;
            tbMapHeight = FindChild<EditorNumberTextBox>(nameof(tbMapHeight));
            tbMapHeight.Enabled = false;

            lblTotalCellsValue = FindChild<XNALabel>(nameof(lblTotalCellsValue));
            tbX = FindChild<EditorNumberTextBox>(nameof(tbX));
            tbY = FindChild<EditorNumberTextBox>(nameof(tbY));
            tbWidth = FindChild<EditorNumberTextBox>(nameof(tbWidth));
            tbHeight = FindChild<EditorNumberTextBox>(nameof(tbHeight));
            
            // 获取所有标签引用
            lblHeader = FindChild<XNALabel>("lblHeader");
            lblMapWidth = FindChild<XNALabel>("lblMapWidth");
            lblMapHeight = FindChild<XNALabel>("lblMapHeight");
            lblTotalCells = FindChild<XNALabel>("lblTotalCells");
            lblVisibleMapArea = FindChild<XNALabel>("lblVisibleMapArea");
            lblX = FindChild<XNALabel>("lblX");
            lblY = FindChild<XNALabel>("lblY");
            lblWidth = FindChild<XNALabel>("lblWidth");
            lblHeight = FindChild<XNALabel>("lblHeight");
            
            // 获取按钮引用
            btnChangeMapSize = FindChild<EditorButton>("btnChangeMapSize");
            btnApplyChanges = FindChild<EditorButton>("btnApplyChanges");

            btnChangeMapSize.LeftClick += BtnChangeMapSize_LeftClick;
            btnApplyChanges.LeftClick += BtnApplyChanges_LeftClick;
            
            // 初始化语言
            RefreshLanguage(MainMenu.IsChinese);
        }

        private void BtnChangeMapSize_LeftClick(object sender, EventArgs e)
        {
            OnResizeMapButtonClicked?.Invoke(this, EventArgs.Empty);
            Hide();
        }

        private void BtnApplyChanges_LeftClick(object sender, System.EventArgs e)
        {
            map.LocalSize = new Rectangle(tbX.Value, tbY.Value, tbWidth.Value, tbHeight.Value);
        }
        
        /// <summary>
        /// 刷新窗口中的语言
        /// </summary>
        public void RefreshLanguage(bool isChinese)
        {
            // 设置窗口标题
            var lblHeader = FindChild<XNALabel>("lblHeader");
            if (lblHeader != null)
                lblHeader.Text = isChinese ? "地图大小" : "MAP SIZE";
            
            // 设置标签文本
            if (lblMapWidth != null)
                lblMapWidth.Text = isChinese ? "地图宽度:" : "Map Width:";
                
            if (lblMapHeight != null)
                lblMapHeight.Text = isChinese ? "地图高度:" : "Map Height:";
                
            if (lblTotalCells != null)
                lblTotalCells.Text = isChinese ? "单元格总数:" : "Total Number of Cells:";
                
            if (lblVisibleMapArea != null)
                lblVisibleMapArea.Text = isChinese ? "可见地图区域" : "VISIBLE MAP AREA";
                
            if (lblX != null)
                lblX.Text = isChinese ? "X坐标:" : "X:";
                
            if (lblY != null)
                lblY.Text = isChinese ? "Y坐标:" : "Y:";
                
            if (lblWidth != null)
                lblWidth.Text = isChinese ? "宽度:" : "Width:";
                
            if (lblHeight != null)
                lblHeight.Text = isChinese ? "高度:" : "Height:";
                
            // 设置按钮文本
            if (btnChangeMapSize != null)
                btnChangeMapSize.Text = isChinese ? "更改地图大小..." : "Change Map Size...";
                
            if (btnApplyChanges != null)
                btnApplyChanges.Text = isChinese ? "应用更改" : "Apply Changes";
        }

        public void Open()
        {
            tbMapWidth.Value = map.Size.X;
            tbMapHeight.Value = map.Size.Y;
            lblTotalCellsValue.Text = map.GetCellCount().ToString();
            tbX.Value = map.LocalSize.X;
            tbY.Value = map.LocalSize.Y;
            tbWidth.Value = map.LocalSize.Width;
            tbHeight.Value = map.LocalSize.Height;
            
            // 确保使用当前语言设置
            RefreshLanguage(MainMenu.IsChinese);

            Show();
        }
    }
}
