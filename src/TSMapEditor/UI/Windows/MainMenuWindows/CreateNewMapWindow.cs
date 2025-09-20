using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Globalization;
using TSMapEditor.Extensions;
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
                EditorMessageBox.Show(WindowManager, "Map too narrow".L10N(), "Map width must be at least ".L10N() + MinMapSize + " cells.".L10N(), MessageBoxButtons.OK);
                return;
            }

            if (tbHeight.Value < MinMapSize)
            {
                EditorMessageBox.Show(WindowManager, "Map too small".L10N(), "Map height must be at least ".L10N() + MinMapSize + " cells.".L10N(), MessageBoxButtons.OK);
                return;
            }

            if (tbWidth.Value > Constants.MaxMapWidth)
            {
                EditorMessageBox.Show(WindowManager, "Map too wide".L10N(), "Map width cannot exceed ".L10N() + Constants.MaxMapWidth + " cells.".L10N(), MessageBoxButtons.OK);
                return;
            }

            if (tbHeight.Value > Constants.MaxMapHeight)
            {
                EditorMessageBox.Show(WindowManager, "Map too long".L10N(), "Map height cannot exceed ".L10N() + Constants.MaxMapHeight + " cells.".L10N(), MessageBoxButtons.OK);
                return;
            }

            if (tbWidth.Value + tbHeight.Value > MaxMapSize)
            {
                EditorMessageBox.Show(WindowManager, "Map too large".L10N(), "Map width + height cannot exceed ".L10N() + MaxMapSize + " cells.".L10N(), MessageBoxButtons.OK);
                return;
            }

            OnCreateNewMap?.Invoke(this, new CreateNewMapEventArgs(ddTheater.SelectedItem.Text, 
                new Point2D(tbWidth.Value, tbHeight.Value), Constants.IsFlatWorld ? (byte)0 : (byte)ddStartingLevel.SelectedItem.Tag));
            WindowManager.RemoveControl(this);
        }
    }
}
