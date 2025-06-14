using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Globalization;
using TSMapEditor.GameMath;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows.MainMenuWindows
{
    public class CreateNewMapEventArgs : EventArgs
    {
        public CreateNewMapEventArgs(string theater, Point2D mapSize, byte startingLevel)
        {
            Theater = theater;
            MapSize = mapSize;
            StartingLevel = startingLevel;
        }

        public string Theater { get; }
        public Point2D MapSize { get; }
        public byte StartingLevel { get; }
    }

    public class CreateNewMapWindow : INItializableWindow
    {
        private const int MinMapSize = 50;
        private const int MaxMapSize = 512;

        public CreateNewMapWindow(WindowManager windowManager, bool canExit) : base(windowManager)
        {
            this.canExit = canExit;
        }

        public event EventHandler<CreateNewMapEventArgs> OnCreateNewMap;

        private readonly bool canExit;

        private XNADropDown ddTheater;
        private EditorNumberTextBox tbWidth;
        private EditorNumberTextBox tbHeight;
        private XNADropDown ddStartingLevel;


        public override void Initialize()
        {
            HasCloseButton = canExit;

            Name = nameof(CreateNewMapWindow);
            base.Initialize();

            ddTheater = FindChild<XNADropDown>(nameof(ddTheater));
            tbWidth = FindChild<EditorNumberTextBox>(nameof(tbWidth));
            tbHeight = FindChild<EditorNumberTextBox>(nameof(tbHeight));
            ddStartingLevel = FindChild<XNADropDown>(nameof(ddStartingLevel));

            FindChild<EditorButton>("btnCreate").LeftClick += BtnCreate_LeftClick;

            ddTheater.SelectedIndex = 0;

            if (!Constants.IsFlatWorld)
            {
                for (byte i = 0; i <= Constants.MaxMapHeightLevel; i++)
                    ddStartingLevel.AddItem(new XNADropDownItem() { Text = i.ToString(CultureInfo.InvariantCulture), Tag = i });

                ddStartingLevel.SelectedIndex = 0;
            }

            CenterOnParent();

            RefreshLanguage(TSMapEditor.UI.MainMenu.IsChinese);
        }

        public void Open()
        {
            Show();
        }

        private void BtnCreate_LeftClick(object sender, EventArgs e)
        {
            if (tbWidth.Value < MinMapSize)
            {
                EditorMessageBox.Show(WindowManager, "Map too narrow", "Map width must be at least " + MinMapSize + " cells.", MessageBoxButtons.OK);
                return;
            }

            if (tbHeight.Value < MinMapSize)
            {
                EditorMessageBox.Show(WindowManager, "Map too small", "Map height must be at least " + MinMapSize + " cells.", MessageBoxButtons.OK);
                return;
            }

            if (tbWidth.Value > Constants.MaxMapWidth)
            {
                EditorMessageBox.Show(WindowManager, "Map too wide", "Map width cannot exceed " + Constants.MaxMapWidth + " cells.", MessageBoxButtons.OK);
                return;
            }

            if (tbHeight.Value > Constants.MaxMapHeight)
            {
                EditorMessageBox.Show(WindowManager, "Map too long", "Map height cannot exceed " + Constants.MaxMapHeight + " cells.", MessageBoxButtons.OK);
                return;
            }

            if (tbWidth.Value + tbHeight.Value > MaxMapSize)
            {
                EditorMessageBox.Show(WindowManager, "Map too large", "Map width + height cannot exceed " + MaxMapSize + " cells.", MessageBoxButtons.OK);
                return;
            }

            OnCreateNewMap?.Invoke(this, new CreateNewMapEventArgs(
                ddTheater.SelectedItem.Tag.ToString(),
                new Point2D(tbWidth.Value, tbHeight.Value),
                Constants.IsFlatWorld ? (byte)0 : (byte)ddStartingLevel.SelectedItem.Tag));
            WindowManager.RemoveControl(this);
        }

        public void RefreshLanguage(bool isChinese)
        {
            var lblHeader = FindChild<XNALabel>("lblHeader");
            if (lblHeader != null)
                lblHeader.Text = isChinese ? "新建地图" : "Create New Map";
            var lblTheater = FindChild<XNALabel>("lblTheater");
            if (lblTheater != null)
                lblTheater.Text = isChinese ? "剧场环境：" : "Theater:";
            var lblWidth = FindChild<XNALabel>("lblWidth");
            if (lblWidth != null)
                lblWidth.Text = isChinese ? "宽度：" : "Width:";
            var lblHeight = FindChild<XNALabel>("lblHeight");
            if (lblHeight != null)
                lblHeight.Text = isChinese ? "高度：" : "Height:";
            var lblStartingLevel = FindChild<XNALabel>("lblStartingLevel");
            if (lblStartingLevel != null)
                lblStartingLevel.Text = isChinese ? "起始高度：" : "Starting Level:";
            var btnCreate = FindChild<EditorButton>("btnCreate");
            if (btnCreate != null)
                btnCreate.Text = isChinese ? "创建" : "Create";
            if (ddTheater != null)
            {
                ddTheater.Items.Clear();
                if (isChinese)
                {
                    ddTheater.AddItem(new XNADropDownItem() { Text = "温带", Tag = "TEMPERATE" });
                    ddTheater.AddItem(new XNADropDownItem() { Text = "雪地", Tag = "SNOW" });
                    ddTheater.AddItem(new XNADropDownItem() { Text = "城市", Tag = "URBAN" });
                    ddTheater.AddItem(new XNADropDownItem() { Text = "沙漠", Tag = "DESERT" });
                    ddTheater.AddItem(new XNADropDownItem() { Text = "新城市", Tag = "NEWURBAN" });
                    ddTheater.AddItem(new XNADropDownItem() { Text = "月球", Tag = "LUNAR" });
                }
                else
                {
                    ddTheater.AddItem(new XNADropDownItem() { Text = "TEMPERATE", Tag = "TEMPERATE" });
                    ddTheater.AddItem(new XNADropDownItem() { Text = "SNOW", Tag = "SNOW" });
                    ddTheater.AddItem(new XNADropDownItem() { Text = "URBAN", Tag = "URBAN" });
                    ddTheater.AddItem(new XNADropDownItem() { Text = "DESERT", Tag = "DESERT" });
                    ddTheater.AddItem(new XNADropDownItem() { Text = "NEWURBAN", Tag = "NEWURBAN" });
                    ddTheater.AddItem(new XNADropDownItem() { Text = "LUNAR", Tag = "LUNAR" });
                }
                ddTheater.SelectedIndex = 0;
            }
        }
    }
}
