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

            OnCreateNewMap?.Invoke(this, new CreateNewMapEventArgs(ddTheater.SelectedItem.Text, 
                new Point2D(tbWidth.Value, tbHeight.Value), Constants.IsFlatWorld ? (byte)0 : (byte)ddStartingLevel.SelectedItem.Tag));
            WindowManager.RemoveControl(this);
        }
    }
}
