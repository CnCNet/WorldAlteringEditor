using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Linq;
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
        private const string DirectoryPrefix = "<DIR> ";
        private const int BrowseButtonWidth = 70;

        public static bool IsChinese = UserSettings.Instance.Language.GetValue() == "zh";

        public MainMenu(WindowManager windowManager) : base(windowManager)
        {
        }

        private string gameDirectory;

        internal EditorTextBox tbGameDirectory;
        private EditorButton btnBrowseGameDirectory;
        private EditorTextBox tbMapPath;
        private EditorButton btnBrowseMapPath;
        private EditorButton btnLoad;
        private FileBrowserListBox lbFileList;

        private SettingsPanel settingsPanel;

        private int loadingStage;

        private EditorButton btnLangSwitch;

        private XNALabel lblGameDirectory;
        private XNALabel lblMapPath;
        private EditorButton btnCreateNewMap;
        private XNALabel lblCopyright;
        private XNALabel lblDirectoryListing;
        private XNALabel lblRecentFiles;

        public override void Initialize()
        {
            bool hasRecentFiles = UserSettings.Instance.RecentFiles.GetEntries().Count > 0;

            Name = nameof(MainMenu);
            Width = 570;
            Height = WindowManager.RenderResolutionY;

            lblGameDirectory = new XNALabel(WindowManager);
            lblGameDirectory.Name = nameof(lblGameDirectory);
            lblGameDirectory.X = Constants.UIEmptySideSpace;
            lblGameDirectory.Y = Constants.UIEmptyTopSpace;
            lblGameDirectory.Text = IsChinese ? "游戏目录路径：" : "Game Directory:";
            AddChild(lblGameDirectory);

            tbGameDirectory = new EditorTextBox(WindowManager);
            tbGameDirectory.Name = nameof(tbGameDirectory);
            tbGameDirectory.AllowSemicolon = true;
            tbGameDirectory.X = Constants.UIEmptySideSpace;
            tbGameDirectory.Y = lblGameDirectory.Bottom + Constants.UIVerticalSpacing;
            tbGameDirectory.Width = Width - Constants.UIEmptySideSpace * 3 - BrowseButtonWidth;
            
            // 根据当前游戏版本加载对应的目录路径
            string currentGameVersion = UserSettings.Instance.GameVersion.GetValue();
            if (currentGameVersion == "MentalOmegaClient")
            {
                // 加载MO目录
                tbGameDirectory.Text = UserSettings.Instance.MentalOmegaGameDirectory.GetValue();
                if (string.IsNullOrWhiteSpace(tbGameDirectory.Text))
                {
                    tbGameDirectory.Text = UserSettings.Instance.GameDirectory.GetValue();
                }
                Logger.Log($"初始化加载MO游戏目录: {tbGameDirectory.Text}");
                
                // 设置MO的可执行文件验证规则
                Constants.ExpectedClientExecutableNames = new string[] { "MentalOmegaClient.exe" };
            }
            else
            {
                // 加载YR目录
                tbGameDirectory.Text = UserSettings.Instance.YurisRevengeGameDirectory.GetValue();
                if (string.IsNullOrWhiteSpace(tbGameDirectory.Text))
                {
                    tbGameDirectory.Text = UserSettings.Instance.GameDirectory.GetValue();
                }
                Logger.Log($"初始化加载YR游戏目录: {tbGameDirectory.Text}");
                
                // 设置YR的可执行文件验证规则
                Constants.ExpectedClientExecutableNames = new string[] { "ra2md.exe", "gamemd.exe" };
            }
            
            // 如果目录仍为空，则尝试从注册表读取
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
            btnBrowseGameDirectory.Text = IsChinese ? "浏览..." : "Browse...";
            btnBrowseGameDirectory.Y = tbGameDirectory.Y;
            btnBrowseGameDirectory.X = tbGameDirectory.Right + Constants.UIEmptySideSpace;
            btnBrowseGameDirectory.Height = tbGameDirectory.Height;
            AddChild(btnBrowseGameDirectory);
            btnBrowseGameDirectory.LeftClick += BtnBrowseGameDirectory_LeftClick;

            lblMapPath = new XNALabel(WindowManager);
            lblMapPath.Name = nameof(lblMapPath);
            lblMapPath.X = Constants.UIEmptySideSpace;
            lblMapPath.Y = tbGameDirectory.Bottom + Constants.UIEmptyTopSpace;
            lblMapPath.Text = IsChinese ? "要加载的地图文件路径（可相对于游戏目录）:" : "Map file path to load (relative to game dir):";
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
            btnBrowseMapPath.Text = IsChinese ? "浏览..." : "Browse...";
            btnBrowseMapPath.Y = tbMapPath.Y;
            btnBrowseMapPath.X = tbMapPath.Right + Constants.UIEmptySideSpace;
            btnBrowseMapPath.Height = tbMapPath.Height;
            AddChild(btnBrowseMapPath);
            btnBrowseMapPath.LeftClick += BtnBrowseMapPath_LeftClick;

            btnLoad = new EditorButton(WindowManager);
            btnLoad.Name = nameof(btnLoad);
            btnLoad.Width = 150;
            btnLoad.Text = IsChinese ? "加载" : "Load";
            btnLoad.Y = Height - btnLoad.Height - Constants.UIEmptyBottomSpace;
            btnLoad.X = Width - btnLoad.Width - Constants.UIEmptySideSpace;
            AddChild(btnLoad);
            btnLoad.LeftClick += BtnLoad_LeftClick;

            btnCreateNewMap = new EditorButton(WindowManager);
            btnCreateNewMap.Name = nameof(btnCreateNewMap);
            btnCreateNewMap.Width = 150;
            btnCreateNewMap.Text = IsChinese ? "新建地图…" : "New Map...";
            btnCreateNewMap.X = Constants.UIEmptySideSpace;
            btnCreateNewMap.Y = btnLoad.Y;
            AddChild(btnCreateNewMap);
            btnCreateNewMap.LeftClick += BtnCreateNewMap_LeftClick;

            lblCopyright = new XNALabel(WindowManager);
            lblCopyright.Name = nameof(lblCopyright);
            lblCopyright.Text = IsChinese ? "Rampastring 制作" : "By Rampastring";
            lblCopyright.TextColor = UISettings.ActiveSettings.SubtleTextColor;
            AddChild(lblCopyright);
            lblCopyright.CenterOnControlVertically(btnCreateNewMap);
            lblCopyright.X = btnCreateNewMap.Right + ((btnLoad.X - btnCreateNewMap.Right) - lblCopyright.Width) / 2;

            int directoryListingY = tbMapPath.Bottom + Constants.UIVerticalSpacing * 2;

            if (hasRecentFiles)
            {
                const int recentFilesHeight = 150;

                lblRecentFiles = new XNALabel(WindowManager);
                lblRecentFiles.Name = nameof(lblRecentFiles);
                lblRecentFiles.X = Constants.UIEmptySideSpace;
                lblRecentFiles.Y = directoryListingY;
                lblRecentFiles.Text = IsChinese ? "最近文件：" : "Recent files:";
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

            lblDirectoryListing = new XNALabel(WindowManager);
            lblDirectoryListing.Name = nameof(lblDirectoryListing);
            lblDirectoryListing.X = Constants.UIEmptySideSpace;
            lblDirectoryListing.Y = directoryListingY;
            lblDirectoryListing.Text = IsChinese ? "或者，选择下方的地图文件：" : "Or, select a map file below:";
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

            // 初始化 settingsPanel
            if (settingsPanel == null)
            {
                settingsPanel = new SettingsPanel(WindowManager);
                settingsPanel.Name = nameof(settingsPanel);
                settingsPanel.X = Width;
                settingsPanel.Y = Constants.UIEmptyTopSpace;
                settingsPanel.Height = Height - Constants.UIEmptyTopSpace - Constants.UIEmptyBottomSpace;
                
                // 订阅游戏版本更改事件
                settingsPanel.GameVersionChanged += (sender, gameVersion) => {
                    // 记录日志
                    Logger.Log($"MainMenu收到游戏版本更改事件: {gameVersion}");
                    
                    // 在游戏版本更改时更新游戏目录路径和期望的可执行文件
                    // 先保存当前目录到对应版本的设置
                    string oldGameVersion = UserSettings.Instance.GameVersion.GetValue();
                    string oldDirectory = tbGameDirectory.Text;
                    
                    // 保存当前目录到旧版本的设置中
                    if (oldGameVersion == "MentalOmegaClient")
                    {
                        // 更新MentalOmega目录
                        if (!string.IsNullOrWhiteSpace(oldDirectory))
                        {
                            UserSettings.Instance.MentalOmegaGameDirectory.UserDefinedValue = oldDirectory;
                            Logger.Log($"已保存MO游戏目录: {oldDirectory}");
                        }
                    }
                    else
                    {
                        // 更新YR目录
                        if (!string.IsNullOrWhiteSpace(oldDirectory))
                        {
                            UserSettings.Instance.YurisRevengeGameDirectory.UserDefinedValue = oldDirectory;
                            Logger.Log($"已保存YR游戏目录: {oldDirectory}");
                        }
                    }
                    
                    // 保存设置
                    UserSettings.Instance.SaveSettings();
                    
                    // 为新版本切换目录路径
                    string newDirectory = string.Empty;
                    if (gameVersion == "MentalOmegaClient")
                    {
                        // 尝试加载之前保存的MO目录
                        newDirectory = UserSettings.Instance.MentalOmegaGameDirectory.GetValue();
                        if (string.IsNullOrWhiteSpace(newDirectory))
                        {
                            newDirectory = UserSettings.Instance.GameDirectory.GetValue();
                        }
                        
                        // 设置MO的可执行文件验证规则
                        Constants.ExpectedClientExecutableNames = new string[] { "MentalOmegaClient.exe" };
                        
                        Logger.Log($"切换到MO游戏目录: {newDirectory}, 可执行文件: {string.Join(",", Constants.ExpectedClientExecutableNames)}");
                    }
                    else
                    {
                        // 尝试加载之前保存的YR目录
                        newDirectory = UserSettings.Instance.YurisRevengeGameDirectory.GetValue();
                        if (string.IsNullOrWhiteSpace(newDirectory))
                        {
                            newDirectory = UserSettings.Instance.GameDirectory.GetValue();
                        }
                        
                        // 设置YR的可执行文件验证规则
                        Constants.ExpectedClientExecutableNames = new string[] { "ra2md.exe", "gamemd.exe" };
                        
                        Logger.Log($"切换到YR游戏目录: {newDirectory}, 可执行文件: {string.Join(",", Constants.ExpectedClientExecutableNames)}");
                    }
                    
                    // 更新游戏目录文本框
                    if (!string.IsNullOrWhiteSpace(newDirectory))
                    {
                        // 直接在UI线程更新文本框
                        tbGameDirectory.Text = newDirectory;
                        Logger.Log($"已更新游戏目录文本框: {newDirectory}");
                    }
                    
                    // 刷新文件列表
                    if (!string.IsNullOrWhiteSpace(tbGameDirectory.Text))
                    {
                        lbFileList.DirectoryPath = tbGameDirectory.Text;
                        Logger.Log($"已刷新文件列表: {tbGameDirectory.Text}");
                    }
                    
                    // 更新游戏目录验证逻辑
                    RefreshGameDirectoryVerification();
                };
                
                AddChild(settingsPanel);
                Width += settingsPanel.Width + Constants.UIEmptySideSpace;
            }

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

            // 在程序启动时，根据当前游戏版本初始化游戏目录
            RefreshGameDirectoryVerification();

            if (Program.args.Length > 0 && !string.IsNullOrWhiteSpace(Program.args[0]))
            {
                if (CheckGameDirectory())
                {
                    tbMapPath.Text = Program.args[0];
                    loadingStage++;
                }
            }

            // 语言切换按钮
            btnLangSwitch = new EditorButton(WindowManager);
            btnLangSwitch.Name = nameof(btnLangSwitch);
            btnLangSwitch.Width = 60;
            btnLangSwitch.Height = 28;
            btnLangSwitch.Text = IsChinese ? "English" : "中文";
            btnLangSwitch.X = Width - btnLangSwitch.Width - 10;
            btnLangSwitch.Y = 10;
            btnLangSwitch.LeftClick += (s, e) => {
                IsChinese = !IsChinese;
                UserSettings.Instance.Language.UserDefinedValue = IsChinese ? "zh" : "en";
                UserSettings.Instance.SaveSettingsAsync();
                Console.WriteLine($"[MainMenu] 切换语言，IsChinese={IsChinese}");
                RefreshLanguage();
                if (settingsPanel != null)
                {
                    Console.WriteLine($"[MainMenu] 调用 settingsPanel.RefreshLanguage, IsChinese={IsChinese}");
                    settingsPanel.RefreshLanguage(IsChinese);
                }

                {
                    
                }
            };
            AddChild(btnLangSwitch);
            RefreshLanguage();
            if (settingsPanel != null)
            {
                Console.WriteLine($"[MainMenu] 初始化后调用 settingsPanel.RefreshLanguage, IsChinese={IsChinese}");
                settingsPanel.RefreshLanguage(IsChinese);
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

                    // 添加对key的null检查
                    if (key == null)
                    {
                        // 注册表键不存在，继续尝试下一个路径
                        Logger.Log($"Registry key not found: {registryInstallPath}");
                        continue;
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
            // 确保使用当前游戏版本对应的可执行文件名
            RefreshGameDirectoryVerification();

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
                    "无效的游戏目录",
                    $"未找到 {Constants.ExpectedClientExecutableNames[0]}，请检查你输入的游戏目录是否正确。",
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
                
                // 确保使用的是最新的游戏版本设置
                RefreshGameDirectoryVerification();
                
                // 设置过滤器，显示适合当前游戏版本的可执行文件
                openFileDialog.Filter =
                    $"游戏可执行文件|{string.Join(';', Constants.ExpectedClientExecutableNames)}";
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
                    "无效的地图路径",
                    "未找到指定的地图文件，请重新检查地图文件路径。",
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

                var messageBox = new EditorMessageBox(WindowManager, "正在加载", "请稍候，正在加载地图……", MessageBoxButtons.None);
                var dp = new DarkeningPanel(WindowManager);
                AddChild(dp);
                dp.AddChild(messageBox);

                return;
            }

            loadingStage = 0;
            EditorMessageBox.Show(WindowManager, "加载文件出错", error, MessageBoxButtons.OK);
        }

        private void LoadTheater()
        {
            MapSetup.LoadTheaterGraphics(WindowManager, gameDirectory);
            WindowManager.RemoveControl(this);
        }

        private void RefreshLanguage()
        {
            btnLangSwitch.Text = IsChinese ? "English" : "中文";
            lblGameDirectory.Text = IsChinese ? "游戏目录路径：" : "Game Directory:";
            btnBrowseGameDirectory.Text = IsChinese ? "浏览..." : "Browse...";
            lblMapPath.Text = IsChinese ? "要加载的地图文件路径（可相对于游戏目录）:" : "Map file path to load (relative to game dir):";
            btnBrowseMapPath.Text = IsChinese ? "浏览..." : "Browse...";
            btnLoad.Text = IsChinese ? "加载" : "Load";
            btnCreateNewMap.Text = IsChinese ? "新建地图…" : "New Map...";
            lblCopyright.Text = IsChinese ? "Rampastring 制作" : "By Rampastring";
            lblDirectoryListing.Text = IsChinese ? "或者，选择下方的地图文件：" : "Or, select a map file below:";
            if (lblRecentFiles != null)
                lblRecentFiles.Text = IsChinese ? "最近文件：" : "Recent files:";
            if (settingsPanel != null)
            {
                Console.WriteLine($"[MainMenu] RefreshLanguage 调用 settingsPanel.RefreshLanguage, IsChinese={IsChinese}");
                settingsPanel.RefreshLanguage(IsChinese);
            }
        }

        /// <summary>
        /// 根据当前选择的游戏版本刷新游戏目录验证，并自动切换到对应的游戏目录
        /// </summary>
        public void RefreshGameDirectoryVerification()
        {
            // 检查UserSettings.Instance是否为空
            if (UserSettings.Instance == null)
            {
                Logger.Log("警告：UserSettings.Instance为null，无法刷新游戏目录验证");
                EditorMessageBox.Show(WindowManager,
                    "设置错误",
                    "用户设置实例未初始化，请重新启动应用程序。",
                    MessageBoxButtons.OK);
                return;
            }

            try
            {
                // 根据UserSettings中的游戏版本设置更新验证逻辑
                string gameVersion = UserSettings.Instance.GameVersion.GetValue() ?? "MentalOmegaClient";
                
                // 保存当前目录路径到对应版本的配置
                if (!string.IsNullOrWhiteSpace(tbGameDirectory.Text))
                {
                    if (gameVersion == "MentalOmegaClient")
                    {
                        // 更新MO目录
                        UserSettings.Instance.MentalOmegaGameDirectory.UserDefinedValue = tbGameDirectory.Text;
                    }
                    else
                    {
                        // 更新YR目录
                        UserSettings.Instance.YurisRevengeGameDirectory.UserDefinedValue = tbGameDirectory.Text;
                    }
                    UserSettings.Instance.SaveSettings();
                }
                
                // 根据游戏版本选择对应的游戏目录
                string newDirectory = string.Empty;
                if (gameVersion == "MentalOmegaClient")
                {
                    // 加载MO目录
                    newDirectory = UserSettings.Instance.MentalOmegaGameDirectory.GetValue();
                    
                    // 如果MO目录尚未设置，则尝试使用通用目录
                    if (string.IsNullOrWhiteSpace(newDirectory))
                    {
                        newDirectory = UserSettings.Instance.GameDirectory.GetValue();
                    }
                    
                    // 根据可执行文件名设置验证规则
                    Constants.ExpectedClientExecutableNames = new string[] { "MentalOmegaClient.exe" };
                }
                else
                {
                    // 加载YR目录
                    newDirectory = UserSettings.Instance.YurisRevengeGameDirectory.GetValue();
                    
                    // 如果YR目录尚未设置，则尝试使用通用目录
                    if (string.IsNullOrWhiteSpace(newDirectory))
                    {
                        newDirectory = UserSettings.Instance.GameDirectory.GetValue();
                    }
                    
                    // 根据可执行文件名设置验证规则
                    Constants.ExpectedClientExecutableNames = new string[] { "ra2md.exe", "gamemd.exe" };
                }
                
                // 如果新目录有效并且与当前显示的不同，则更新文本框
                if (!string.IsNullOrWhiteSpace(newDirectory) && newDirectory != tbGameDirectory.Text)
                {
                    tbGameDirectory.Text = newDirectory;
                    Logger.Log($"已切换游戏目录为: {newDirectory} (游戏版本: {gameVersion})");
                }

                // 更新当前目录到通用设置
                UserSettings.Instance.GameDirectory.UserDefinedValue = tbGameDirectory.Text;

                // 验证当前游戏目录是否有效
                bool isValidDirectory = VerifyGameDirectory();
                Logger.Log($"游戏目录验证结果: {isValidDirectory}, 游戏版本: {gameVersion}, 期望的可执行文件: {string.Join(", ", Constants.ExpectedClientExecutableNames)}");
            }
            catch (Exception ex)
            {
                Logger.Log($"刷新游戏目录验证时出错: {ex.Message}");
            }
        }
    }
}
