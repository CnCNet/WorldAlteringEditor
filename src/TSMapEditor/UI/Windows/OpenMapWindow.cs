using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.IO;
using TSMapEditor.Settings;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class FileSelectedEventArgs : EventArgs
    {
        public FileSelectedEventArgs(string filePath)
        {
            FilePath = filePath;
        }

        public string FilePath { get; }
    }

    /// <summary>
    /// A window that allows the user to open a map.
    /// </summary>
    public class OpenMapWindow : EditorWindow
    {
        public OpenMapWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        public event EventHandler<FileSelectedEventArgs> OnFileSelected;

        private FileBrowserListBox lbFileList;

        private string filePath;

        public override void Initialize()
        {
            Width = 300;
            Name = nameof(OpenMapWindow);

            var lblHeader = new XNALabel(WindowManager);
            lblHeader.Name = nameof(lblHeader);
            lblHeader.X = Constants.UIEmptySideSpace;
            lblHeader.Y = Constants.UIEmptyTopSpace;
            lblHeader.Text = Translate(this, "SelectMapFile", "Select the map file to load:");
            AddChild(lblHeader);

            lbFileList = new FileBrowserListBox(WindowManager);
            lbFileList.Name = nameof(lbFileList);
            lbFileList.X = Constants.UIEmptySideSpace;
            lbFileList.Y = lblHeader.Bottom + Constants.UIVerticalSpacing;
            lbFileList.Width = Width - Constants.UIEmptySideSpace * 2;
            lbFileList.Height = 300;
            AddChild(lbFileList);
            lbFileList.FileSelected += LbFileList_FileSelected;
            lbFileList.FileDoubleLeftClick += (s, e) => BtnLoad_LeftClick(this, EventArgs.Empty);

            var btnLoad = new EditorButton(WindowManager);
            btnLoad.Name = nameof(btnLoad);
            btnLoad.Width = 100;
            btnLoad.X = Width - Constants.UIEmptySideSpace - btnLoad.Width;
            btnLoad.Y = lbFileList.Bottom + Constants.UIEmptyTopSpace;
            btnLoad.Text = Translate(this, "Open", "Open");
            AddChild(btnLoad);
            btnLoad.LeftClick += BtnLoad_LeftClick;

            var btnCancel = new EditorButton(WindowManager);
            btnCancel.Name = nameof(btnCancel);
            btnCancel.Width = 100;
            btnCancel.X = Constants.UIEmptySideSpace;
            btnCancel.Y = btnLoad.Y;
            btnCancel.Text = Translate(this, "Cancel", "Cancel");
            AddChild(btnCancel);
            btnCancel.LeftClick += BtnCancel_LeftClick;

            Height = btnLoad.Bottom + Constants.UIEmptyBottomSpace;

            base.Initialize();
        }

        public void Open()
        {
            Show();

            if (string.IsNullOrWhiteSpace(UserSettings.Instance.LastScenarioPath))
                lbFileList.DirectoryPath = UserSettings.Instance.GameDirectory;
            else
                lbFileList.DirectoryPath = Path.GetDirectoryName(UserSettings.Instance.LastScenarioPath);
        }

        private void BtnLoad_LeftClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                EditorMessageBox.Show(WindowManager, 
                    Translate(this, "NoFileSelected.Title", "No file selected"),
                    Translate(this, "NoFileSelected.Description", "Please select the map file to open."), 
                    MessageBoxButtons.OK);
                return;
            }

            OnFileSelected?.Invoke(this, new FileSelectedEventArgs(filePath));

            Hide();
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Hide();
        }

        private void LbFileList_FileSelected(object sender, FileSelectionEventArgs e)
        {
            filePath = e.FilePath;
        }
    }
}
