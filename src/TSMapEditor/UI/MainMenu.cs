using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.IO;
using TSMapEditor.Settings;
using TSMapEditor.UI.Controls;
using TSMapEditor.UI.Windows;
using TSMapEditor.UI.Windows.MainMenuWindows;
using MessageBoxButtons = TSMapEditor.UI.Windows.MessageBoxButtons;

#if WINDOWS
using System.Windows.Forms;
using Microsoft.Win32;
#endif

namespace TSMapEditor.UI
{
    public class MainMenu : EditorPanel
    {
        private const int BrowseButtonWidth = 70;

        public MainMenu(WindowManager windowManager) : base(windowManager)
        {
        }

        private string gameDirectory;

        private EditorTextBox tbGameDirectory;
        private EditorButton btnBrowseGameDirectory;
        private EditorTextBox tbMapPath;
        private EditorButton btnBrowseMapPath;
        private EditorButton btnLoad;
        private FileBrowserListBox lbFileList;

        private SettingsPanel settingsPanel;

        private int loadingStage;

        public override void Initialize()
        {
            bool hasRecentFiles = UserSettings.Instance.RecentFiles.GetEntries().Count > 0;

            Name = nameof(MainMenu);
            Width = 570;
            Height = WindowManager.RenderResolutionY;

            var lblGameDirectory = new XNALabel(WindowManager);
            lblGameDirectory.Name = nameof(lblGameDirectory);
            lblGameDirectory.X = Constants.UIEmptySideSpace;
            lblGameDirectory.Y = Constants.UIEmptyTopSpace;
            lblGameDirectory.Text = "Path to the game directory:";
            AddChild(lblGameDirectory);

            tbGameDirectory = new EditorTextBox(WindowManager);
            tbGameDirectory.Name = nameof(tbGameDirectory);
            tbGameDirectory.AllowSemicolon = true;
            tbGameDirectory.X = Constants.UIEmptySideSpace;
            tbGameDirectory.Y = lblGameDirectory.Bottom + Constants.UIVerticalSpacing;
            tbGameDirectory.Width = Width - Constants.UIEmptySideSpace * 3 - BrowseButtonWidth;
            tbGameDirectory.Text = UserSettings.Instance.GameDirectory;
            if (string.IsNullOrWhiteSpace(tbGameDirectory.Text))
            {
                ReadGameInstallDirectoryFromRegistry();
            }

#if DEBUG
            // When debugging we might often switch between configs - make it a bit more convenient
            if (!VerifyGameDirectory())
            {
                ReadGameInstallDirectoryFromRegistry();
            }
#endif

            tbGameDirectory.TextChanged += TbGameDirectory_TextChanged;
            AddChild(tbGameDirectory);

            btnBrowseGameDirectory = new EditorButton(WindowManager);
            btnBrowseGameDirectory.Name = nameof(btnBrowseGameDirectory);
            btnBrowseGameDirectory.Width = BrowseButtonWidth;
            btnBrowseGameDirectory.Text = "Browse...";
            btnBrowseGameDirectory.Y = tbGameDirectory.Y;
            btnBrowseGameDirectory.X = tbGameDirectory.Right + Constants.UIEmptySideSpace;
            btnBrowseGameDirectory.Height = tbGameDirectory.Height;
            AddChild(btnBrowseGameDirectory);
            btnBrowseGameDirectory.LeftClick += BtnBrowseGameDirectory_LeftClick;

            var lblMapPath = new XNALabel(WindowManager);
            lblMapPath.Name = nameof(lblMapPath);
            lblMapPath.X = Constants.UIEmptySideSpace;
            lblMapPath.Y = tbGameDirectory.Bottom + Constants.UIEmptyTopSpace;
            lblMapPath.Text = "Path of the map file to load (can be relative to game directory):";
            AddChild(lblMapPath);

            tbMapPath = new EditorTextBox(WindowManager);
            tbMapPath.Name = nameof(tbMapPath);
            tbMapPath.AllowSemicolon = true;
            tbMapPath.X = Constants.UIEmptySideSpace;
            tbMapPath.Y = lblMapPath.Bottom + Constants.UIVerticalSpacing;
            tbMapPath.Width = Width - Constants.UIEmptySideSpace * 3 - BrowseButtonWidth;
            tbMapPath.Text = UserSettings.Instance.LastScenarioPath;
            AddChild(tbMapPath);

            btnBrowseMapPath = new EditorButton(WindowManager);
            btnBrowseMapPath.Name = nameof(btnBrowseMapPath);
            btnBrowseMapPath.Width = BrowseButtonWidth;
            btnBrowseMapPath.Text = "Browse...";
            btnBrowseMapPath.Y = tbMapPath.Y;
            btnBrowseMapPath.X = tbMapPath.Right + Constants.UIEmptySideSpace;
            btnBrowseMapPath.Height = tbMapPath.Height;
            AddChild(btnBrowseMapPath);
            btnBrowseMapPath.LeftClick += BtnBrowseMapPath_LeftClick;

            btnLoad = new EditorButton(WindowManager);
            btnLoad.Name = nameof(btnLoad);
            btnLoad.Width = 150;
            btnLoad.Text = "Load";
            btnLoad.Y = Height - btnLoad.Height - Constants.UIEmptyBottomSpace;
            btnLoad.X = Width - btnLoad.Width - Constants.UIEmptySideSpace;
            AddChild(btnLoad);
            btnLoad.LeftClick += BtnLoad_LeftClick;

            var btnCreateNewMap = new EditorButton(WindowManager);
            btnCreateNewMap.Name = nameof(btnCreateNewMap);
            btnCreateNewMap.Width = 150;
            btnCreateNewMap.Text = "New Map...";
            btnCreateNewMap.X = Constants.UIEmptySideSpace;
            btnCreateNewMap.Y = btnLoad.Y;
            AddChild(btnCreateNewMap);
            btnCreateNewMap.LeftClick += BtnCreateNewMap_LeftClick;

            var lblCopyright = new XNALabel(WindowManager);
            lblCopyright.Name = nameof(lblCopyright);
            lblCopyright.Text = "Created by Rampastring";
            lblCopyright.TextColor = UISettings.ActiveSettings.SubtleTextColor;
            AddChild(lblCopyright);
            lblCopyright.CenterOnControlVertically(btnCreateNewMap);
            lblCopyright.X = btnCreateNewMap.Right + ((btnLoad.X - btnCreateNewMap.Right) - lblCopyright.Width) / 2;

            int directoryListingY = tbMapPath.Bottom + Constants.UIVerticalSpacing * 2;

            if (hasRecentFiles)
            {
                const int recentFilesHeight = 150;

                var lblRecentFiles = new XNALabel(WindowManager);
                lblRecentFiles.Name = nameof(lblRecentFiles);
                lblRecentFiles.X = Constants.UIEmptySideSpace;
                lblRecentFiles.Y = directoryListingY;
                lblRecentFiles.Text = "Recent files:";
                AddChild(lblRecentFiles);

                var recentFilesPanel = new RecentFilesPanel(WindowManager);
                recentFilesPanel.X = lblRecentFiles.X;
                recentFilesPanel.Y = lblRecentFiles.Bottom + Constants.UIVerticalSpacing;
                recentFilesPanel.Width = Width - (Constants.UIEmptySideSpace * 2);
                recentFilesPanel.Height = recentFilesHeight - lblRecentFiles.Height - (Constants.UIVerticalSpacing * 2);
                recentFilesPanel.FileSelected += RecentFilesPanel_FileSelected;
                AddChild(recentFilesPanel);

                directoryListingY = recentFilesPanel.Bottom + Constants.UIVerticalSpacing;
            }

            var lblDirectoryListing = new XNALabel(WindowManager);
            lblDirectoryListing.Name = nameof(lblDirectoryListing);
            lblDirectoryListing.X = Constants.UIEmptySideSpace;
            lblDirectoryListing.Y = directoryListingY;
            lblDirectoryListing.Text = "Alternatively, select a map file below:";
            AddChild(lblDirectoryListing);

            lbFileList = new FileBrowserListBox(WindowManager);
            lbFileList.Name = nameof(lbFileList);
            lbFileList.X = Constants.UIEmptySideSpace;
            lbFileList.Y = lblDirectoryListing.Bottom + Constants.UIVerticalSpacing;
            lbFileList.Width = Width - (Constants.UIEmptySideSpace * 2);
            lbFileList.Height = btnLoad.Y - Constants.UIEmptyTopSpace - lbFileList.Y;
            lbFileList.FileSelected += LbFileList_FileSelected;
            lbFileList.FileDoubleLeftClick += LbFileList_FileDoubleLeftClick;
            AddChild(lbFileList);

            settingsPanel = new SettingsPanel(WindowManager);
            settingsPanel.X = Width;
            settingsPanel.Y = Constants.UIEmptyTopSpace;
            AddChild(settingsPanel);
            settingsPanel.Height = lbFileList.Bottom - settingsPanel.Y;
            Width += settingsPanel.Width + Constants.UIEmptySideSpace;

            var lblVersion = new XNALabel(WindowManager);
            lblVersion.Name = nameof(lblVersion);
            lblVersion.Text = "Version " + Constants.ReleaseVersion;
            lblVersion.TextColor = UISettings.ActiveSettings.SubtleTextColor;
            AddChild(lblVersion);
            lblVersion.CenterOnControlVertically(btnLoad);
            lblVersion.X = Width - lblVersion.Width - Constants.UIEmptySideSpace;

            string directoryPath = string.Empty;

            if (!string.IsNullOrWhiteSpace(tbGameDirectory.Text))
            {
                directoryPath = tbGameDirectory.Text;

                if (!string.IsNullOrWhiteSpace(tbMapPath.Text))
                {
                    if (Path.IsPathRooted(tbMapPath.Text))
                    {
                        directoryPath = Path.GetDirectoryName(tbMapPath.Text);
                    }
                    else
                    {
                        directoryPath = Path.GetDirectoryName(tbGameDirectory.Text + tbMapPath.Text);
                    }
                }

                directoryPath = directoryPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            }

            lbFileList.DirectoryPath = directoryPath;

            base.Initialize();

            if (Program.args.Length > 0 && !string.IsNullOrWhiteSpace(Program.args[0]))
            {
                if (CheckGameDirectory())
                {
                    tbMapPath.Text = Program.args[0];
                    loadingStage++;
                }
            }
        }

        private void RecentFilesPanel_FileSelected(object sender, FileSelectedEventArgs e)
        {
            tbMapPath.Text = e.FilePath;
            BtnLoad_LeftClick(this, EventArgs.Empty);
        }

        private void LbFileList_FileSelected(object sender, FileSelectionEventArgs e)
        {
            tbMapPath.Text = e.FilePath;
        }

        private void BtnCreateNewMap_LeftClick(object sender, EventArgs e)
        {
            if (!CheckGameDirectory())
                return;

            ApplySettings();
            WindowManager.RemoveControl(this);
            var createMapWindow = new CreateNewMapWindow(WindowManager, false);
            createMapWindow.OnCreateNewMap += CreateMapWindow_OnCreateNewMap;
            WindowManager.AddAndInitializeControl(createMapWindow);
        }

        private void CreateMapWindow_OnCreateNewMap(object sender, CreateNewMapEventArgs e)
        {
            string error = MapSetup.InitializeMap(gameDirectory, true, null, e, WindowManager);
            if (!string.IsNullOrWhiteSpace(error))
                throw new InvalidOperationException("Failed to create new map! Returned error message: " + error);

            MapSetup.LoadTheaterGraphics(WindowManager, gameDirectory);
            ((CreateNewMapWindow)sender).OnCreateNewMap -= CreateMapWindow_OnCreateNewMap;
        }

        private void ReadGameInstallDirectoryFromRegistry()
        {
            string[] pathsToLookup = Constants.GameRegistryInstallPath.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (pathsToLookup.Length == 0)
            {
                Logger.Log($"No valid paths specified in {nameof(Constants.GameRegistryInstallPath)}. Unable to read game installation path from Windows registry.");
                return;
            }

            try
            {
                foreach (string registryInstallPath in pathsToLookup)
                {
                    RegistryKey key;

                    const string hklmIdentifier = "HKLM:";

                    // By default, try to find the key from the current user's registry.
                    // Optionally, if the path starts with the HKLM identifier, look for the key in the local machine's registry instead.
                    if (registryInstallPath.StartsWith(hklmIdentifier))
                    {
                        key = Registry.LocalMachine.OpenSubKey(registryInstallPath.Substring(hklmIdentifier.Length));
                    }
                    else
                    {
                        key = Registry.CurrentUser.OpenSubKey(registryInstallPath);
                    }

                    bool isValid = false;

                    object value = key.GetValue("InstallPath", string.Empty);
                    if (!(value is string valueAsString))
                    {
                        tbGameDirectory.Text = string.Empty;
                    }
                    else
                    {
                        if (File.Exists(valueAsString))
                        {
                            tbGameDirectory.Text = Path.GetDirectoryName(valueAsString);
                        }
                        else
                        {
                            tbGameDirectory.Text = valueAsString;
                        }

                        foreach (string expectedExecutableName in Constants.ExpectedClientExecutableNames)
                        {
                            if (File.Exists(Path.Combine(tbGameDirectory.Text, expectedExecutableName)))
                            {
                                isValid = true;
                                break;
                            }
                        }
                    }

                    key.Close();

                    // Break when we find the first valid installation path
                    if (isValid)
                        break;
                }
            }
            catch (Exception ex)
            {
                tbGameDirectory.Text = string.Empty;
                Logger.Log("Failed to read game installation path from the Windows registry! Exception message: " + ex.Message);
            }
        }

        private void TbGameDirectory_TextChanged(object sender, EventArgs e)
        {
            lbFileList.DirectoryPath = tbGameDirectory.Text;
        }

        private void LbFileList_FileDoubleLeftClick(object sender, EventArgs e)
        {
            BtnLoad_LeftClick(this, EventArgs.Empty);
        }

        private bool VerifyGameDirectory()
        {
            bool gameDirectoryVerified = false;
            foreach (string expectedExecutableName in Constants.ExpectedClientExecutableNames)
            {
                if (File.Exists(Path.Combine(tbGameDirectory.Text, expectedExecutableName)))
                {
                    gameDirectoryVerified = true;
                    break;
                }
            }

            return gameDirectoryVerified;
        }

        private bool CheckGameDirectory()
        {
            if (!VerifyGameDirectory())
            {
                EditorMessageBox.Show(WindowManager,
                    "Invalid game directory",
                    $"{Constants.ExpectedClientExecutableNames[0]} not found, please check that you typed the correct game directory.",
                    MessageBoxButtons.OK);

                return false;
            }

            gameDirectory = tbGameDirectory.Text;
            if (!gameDirectory.EndsWith("/") && !gameDirectory.EndsWith("\\"))
                gameDirectory += "/";

            return true;
        }

        private void ApplySettings()
        {
            settingsPanel.ApplySettings();

            UserSettings.Instance.GameDirectory.UserDefinedValue = tbGameDirectory.Text;
            UserSettings.Instance.LastScenarioPath.UserDefinedValue = tbMapPath.Text;
            UserSettings.Instance.RecentFiles.PutEntry(tbMapPath.Text);

            bool fullscreenWindowed = UserSettings.Instance.FullscreenWindowed.GetValue();
            bool borderless = UserSettings.Instance.Borderless.GetValue();
            if (fullscreenWindowed && !borderless)
                throw new InvalidOperationException("Borderless= cannot be set to false if FullscreenWindowed= is enabled.");

            WindowManager.CenterControlOnScreen(this);

            _ = UserSettings.Instance.SaveSettingsAsync();
        }

        private void BtnBrowseGameDirectory_LeftClick(object sender, EventArgs e)
        {
#if WINDOWS
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = tbGameDirectory.Text;
                openFileDialog.Filter =
                    $"Game executable|{string.Join(';', Constants.ExpectedClientExecutableNames)}";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    tbGameDirectory.Text = Path.GetDirectoryName(openFileDialog.FileName);
                    InputIgnoreTime = TimeSpan.FromSeconds(Constants.UIAccidentalClickPreventionTime);
                }
            }
#endif
        }

        private void BtnBrowseMapPath_LeftClick(object sender, EventArgs e)
        {
#if WINDOWS
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = tbMapPath.Text;
                openFileDialog.Filter = Constants.OpenFileDialogFilter.Replace(':', ';');
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    tbMapPath.Text = openFileDialog.FileName;
                    InputIgnoreTime = TimeSpan.FromSeconds(Constants.UIAccidentalClickPreventionTime);
                    BtnLoad_LeftClick(this, new EventArgs());
                }
            }
#endif
        }

        private void BtnLoad_LeftClick(object sender, EventArgs e)
        {
            if (!CheckGameDirectory())
                return;

            UserSettings.Instance.GameDirectory.UserDefinedValue = gameDirectory;

            string mapPath = Path.Combine(gameDirectory, tbMapPath.Text);
            if (Path.IsPathRooted(tbMapPath.Text))
                mapPath = tbMapPath.Text;

            if (!File.Exists(mapPath))
            {
                EditorMessageBox.Show(WindowManager,
                    "Invalid map path",
                    "Specified map file not found. Please re-check the path to the map file.",
                    MessageBoxButtons.OK);

                return;
            }

            loadingStage = 1;
        }

        public override void Update(GameTime gameTime)
        {
            if (loadingStage == 3)
                LoadMap(tbMapPath.Text);
            else if (loadingStage == 5)
                LoadTheater();

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (loadingStage > 0)
            {
                loadingStage++;
            }
        }

        private void LoadMap(string mapPath)
        {
            string error = MapSetup.InitializeMap(gameDirectory, false, mapPath, null, WindowManager);

            if (error == null)
            {
                ApplySettings();

                var messageBox = new EditorMessageBox(WindowManager, "Loading", "Please wait, loading map...", MessageBoxButtons.None);
                var dp = new DarkeningPanel(WindowManager);
                AddChild(dp);
                dp.AddChild(messageBox);

                return;
            }

            loadingStage = 0;
            EditorMessageBox.Show(WindowManager, "Error Loading File", error, MessageBoxButtons.OK);
        }

        private void LoadTheater()
        {
            MapSetup.LoadTheaterGraphics(WindowManager, gameDirectory);
            WindowManager.RemoveControl(this);
        }
    }
}
