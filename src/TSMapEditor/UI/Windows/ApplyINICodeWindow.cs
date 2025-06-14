using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.IO;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class ApplyINICodeWindow : INItializableWindow
    {
        private const string EditorSection = "$Editor";

        public ApplyINICodeWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private EditorListBox lbINIFiles;

        private IniFile stagingINI;

        public override void Initialize()
        {
            Name = nameof(ApplyINICodeWindow);
            base.Initialize();

            lbINIFiles = FindChild<EditorListBox>(nameof(lbINIFiles));
            FindChild<EditorButton>("btnApplyFile").LeftClick += BtnApplyFile_LeftClick;
        }

        private void BtnApplyFile_LeftClick(object sender, EventArgs e)
        {
            if (lbINIFiles.SelectedItem == null)
                return;

            string filePath = (string)lbINIFiles.SelectedItem.Tag;
            if (!File.Exists(filePath))
            {
                EditorMessageBox.Show(WindowManager, "Can't find file",
                    "The selected INI file doesn't exist! Maybe it was deleted?", MessageBoxButtons.OK);

                return;
            }

            stagingINI = new IniFile((string)lbINIFiles.SelectedItem.Tag);
            
            string confirmation = stagingINI.GetStringValue(EditorSection, "Confirmation", null);
            if (!string.IsNullOrWhiteSpace(confirmation))
            {
                confirmation = Renderer.FixText(confirmation, Constants.UIDefaultFont, Width).Text;

                var messageBox = EditorMessageBox.Show(WindowManager, "Are you sure?",
                    confirmation, MessageBoxButtons.YesNo);
                messageBox.YesClickedAction = (_) => ApplyCode();
            }
            else
            {
                ApplyCode();
            }
        }

        private void ApplyCode()
        {
            if (stagingINI == null)
                throw new InvalidOperationException("Staging INI is null!");

            string successMessage = "INI code successfully added to map.";
            successMessage = stagingINI.GetStringValue(EditorSection, "Success", successMessage);
            successMessage = Renderer.FixText(successMessage, Constants.UIDefaultFont, Width).Text;

            stagingINI.RemoveSection(EditorSection);

            IniFile.ConsolidateIniFiles(map.LoadedINI, stagingINI);

            EditorMessageBox.Show(WindowManager, "Code Applied", successMessage, MessageBoxButtons.OK);
        }

        public void Open()
        {
            lbINIFiles.Clear();

            string directoryPath = Environment.CurrentDirectory + "/Config/MapCode";

            if (!Directory.Exists(directoryPath))
            {
                Logger.Log("Map INI code directory not found!");
                EditorMessageBox.Show(WindowManager, "Error", "Map INI code directory not found!\r\n\r\nExpected path: " + directoryPath, MessageBoxButtons.OK);
                return;
            }

            var iniFiles = Directory.GetFiles(directoryPath, "*.INI");

            foreach (string filePath in iniFiles)
            {
                lbINIFiles.AddItem(new XNAListBoxItem(Path.GetFileName(filePath)) { Tag = filePath });
            }

            Show();
        }
    }
}
