using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.IO;
using TSMapEditor.Models;
using TSMapEditor.Settings;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    /// <summary>
    /// A window that allows the user to save the map with a customized file path.
    /// </summary>
    public class SaveMapAsWindow : EditorWindow
    {
        public SaveMapAsWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private FileBrowserListBox lbFileList;
        private EditorTextBox tbFileName;

        public override void Initialize()
        {
            Name = nameof(SaveMapAsWindow);

            var lblHeader = new XNALabel(WindowManager);
            lblHeader.Name = nameof(lblHeader);
            lblHeader.X = Constants.UIEmptySideSpace;
            lblHeader.Y = Constants.UIEmptyTopSpace;
            lblHeader.Text = Translate(this, "HeaderText", "Select the destination that you want to save the map to.");
            AddChild(lblHeader);
            Width = lblHeader.Right + Constants.UIEmptySideSpace;

            lbFileList = new FileBrowserListBox(WindowManager);
            lbFileList.Name = nameof(lbFileList);
            lbFileList.X = Constants.UIEmptySideSpace;
            lbFileList.Y = lblHeader.Bottom + Constants.UIVerticalSpacing;
            lbFileList.Width = Width - Constants.UIEmptySideSpace * 2;
            lbFileList.Height = 300;
            AddChild(lbFileList);
            lbFileList.FileSelected += LbFileList_FileSelected;

            var lblFileName = new XNALabel(WindowManager);
            lblFileName.Name = nameof(lblFileName);
            lblFileName.X = Constants.UIEmptySideSpace;
            lblFileName.Y = lbFileList.Bottom + Constants.UIVerticalSpacing;
            lblFileName.Text = Translate(this, "FileNameText", "File name:");
            AddChild(lblFileName);

            tbFileName = new EditorTextBox(WindowManager);
            tbFileName.Name = nameof(tbFileName);
            tbFileName.X = Constants.UIEmptySideSpace;
            tbFileName.Y = lblFileName.Bottom + Constants.UIVerticalSpacing;
            tbFileName.Width = lbFileList.Width;
            AddChild(tbFileName);

            var btnSave = new EditorButton(WindowManager);
            btnSave.Name = nameof(btnSave);
            btnSave.X = Constants.UIEmptySideSpace;
            btnSave.Y = tbFileName.Bottom + Constants.UIEmptyTopSpace;
            btnSave.Width = 100;
            btnSave.Text = Translate(this, "SaveText", "Save");
            AddChild(btnSave);
            btnSave.LeftClick += BtnSave_LeftClick;

            var btnCancel = new EditorButton(WindowManager);
            btnCancel.Name = nameof(btnCancel);
            btnCancel.Width = 100;
            btnCancel.X = Width - Constants.UIEmptySideSpace - btnCancel.Width;
            btnCancel.Y = btnSave.Y;
            btnCancel.Text = Translate(this, "CancelText", "Cancel");
            AddChild(btnCancel);
            btnCancel.LeftClick += BtnCancel_LeftClick;

            Height = btnSave.Bottom + Constants.UIEmptyBottomSpace;

            base.Initialize();
        }

        public void Open()
        {
            Show();
            lbFileList.DirectoryPath = UserSettings.Instance.GameDirectory;
            tbFileName.Text = string.Empty;
        }

        private void BtnSave_LeftClick(object sender, EventArgs e)
        {
            if (tbFileName.Text.Length == 0)
            {
                EditorMessageBox.Show(WindowManager, 
                    Translate(this, "NoFileName.Title", "No file name given"),
                    Translate(this, "NoFileName.Description", "Please enter a name for the map file."), 
                    MessageBoxButtons.OK);
                return;
            }

            string filename = tbFileName.Text;

            if (filename.IndexOf('.') == -1)
            {
                filename += ".map";
            }

            string path = Path.Combine(lbFileList.DirectoryPath, filename);
            map.LoadedINI.FileName = path;
            map.Save();

            if (UserSettings.Instance.LastScenarioPath != path)
            {
                UserSettings.Instance.LastScenarioPath.UserDefinedValue = path;
                UserSettings.Instance.RecentFiles.PutEntry(path);
                _ = UserSettings.Instance.SaveSettingsAsync();
            }

            Hide();
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Hide();
        }

        private void LbFileList_FileSelected(object sender, FileSelectionEventArgs e)
        {
            tbFileName.Text = e.FilePath.Substring(lbFileList.DirectoryPath.Length);
        }
    }
}
