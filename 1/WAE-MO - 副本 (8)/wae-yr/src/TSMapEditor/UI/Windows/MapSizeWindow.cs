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

            FindChild<EditorButton>("btnChangeMapSize").LeftClick += BtnChangeMapSize_LeftClick;
            FindChild<EditorButton>("btnApplyChanges").LeftClick += BtnApplyChanges_LeftClick;
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

        public void Open()
        {
            tbMapWidth.Value = map.Size.X;
            tbMapHeight.Value = map.Size.Y;
            lblTotalCellsValue.Text = map.GetCellCount().ToString();
            tbX.Value = map.LocalSize.X;
            tbY.Value = map.LocalSize.Y;
            tbWidth.Value = map.LocalSize.Width;
            tbHeight.Value = map.LocalSize.Height;

            Show();
        }
    }
}
