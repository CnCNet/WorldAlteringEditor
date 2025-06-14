using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Globalization;
using System.Windows.Forms;
using TSMapEditor.GameMath;
using TSMapEditor.Settings;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI
{
    /// <summary>
    /// A single screen resolution.
    /// </summary>
    sealed class ScreenResolution : IComparable<ScreenResolution>
    {
        public ScreenResolution(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// The width of the resolution in pixels.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// The height of the resolution in pixels.
        /// </summary>
        public int Height { get; set; }

        public override string ToString()
        {
            return Width + "x" + Height;
        }

        public int CompareTo(ScreenResolution res2)
        {
            if (this.Width < res2.Width)
                return -1;
            else if (this.Width > res2.Width)
                return 1;
            else // equal
            {
                if (this.Height < res2.Height)
                    return -1;
                else if (this.Height > res2.Height)
                    return 1;
                else return 0;
            }
        }

        public override bool Equals(object obj)
        {
            var resolution = obj as ScreenResolution;

            if (resolution == null)
                return false;

            return CompareTo(resolution) == 0;
        }

        public override int GetHashCode()
        {
            return Width * 10000 + Height;
        }
    }

    public class SettingsPanel : EditorPanel
    {
        public SettingsPanel(WindowManager windowManager) : base(windowManager)
        {
            BackgroundTexture = AssetLoader.CreateTexture(UISettings.ActiveSettings.BackgroundColor, 2, 2);
        }

        private XNADropDown ddRenderScale;
        private XNADropDown ddTargetFPS;
        private XNACheckBox chkBorderless;
        private XNADropDown ddTheme;
        private XNADropDown ddScrollRate;
        private XNACheckBox chkUseBoldFont;
        private XNACheckBox chkGraphicsLevel;
        private XNACheckBox chkSmartScriptActionCloning;
        private EditorTextBox tbTextEditorPath;
        private XNADropDown ddGameVersion;
        private XNALabel lblHeader;
        private XNALabel lblRenderScale;
        private XNALabel lblTargetFPS;
        private XNALabel lblTheme;
        private XNALabel lblScrollRate;
        private XNALabel lblTextEditorPath;
        private XNALabel lblGameVersion;

        // 添加一个事件，用于通知游戏版本已更改
        public event EventHandler<string> GameVersionChanged;

        // 添加一个公共方法，获取当前选择的游戏版本
        public string GetSelectedGameVersion()
        {
            return ddGameVersion?.SelectedItem?.Text ?? "Yuri's Revenge";
        }

        // 更新当前游戏可执行文件期望值
        public void UpdateExpectedExecutableNames()
        {
            string gameVersion = GetSelectedGameVersion();
            if (gameVersion == "Yuri's Revenge")
            {
                Constants.ExpectedClientExecutableNames = new string[] { "ra2md.exe", "gamemd.exe" };
            }
            else if (gameVersion == "MentalOmegaClient")
            {
                Constants.ExpectedClientExecutableNames = new string[] { "MentalOmegaClient.exe" };
            }
            
            Rampastring.Tools.Logger.Log($"已更新游戏可执行文件期望值: {string.Join(", ", Constants.ExpectedClientExecutableNames)} (游戏版本: {gameVersion})");
        }

        public override void Kill()
        {
            BackgroundTexture?.Dispose();
            base.Kill();
        }

        public override void Initialize()
        {
            Width = 230;

            lblHeader = new XNALabel(WindowManager);
            lblHeader.Name = nameof(lblHeader);
            lblHeader.FontIndex = Constants.UIBoldFont;
            lblHeader.Text = "设置";
            lblHeader.Y = Constants.UIEmptyTopSpace;
            AddChild(lblHeader);
            lblHeader.CenterOnParentHorizontally();

            lblRenderScale = new XNALabel(WindowManager);
            lblRenderScale.Name = nameof(lblRenderScale);
            lblRenderScale.Text = "渲染缩放：";
            lblRenderScale.X = Constants.UIEmptySideSpace;
            lblRenderScale.Y = lblHeader.Bottom + Constants.UIEmptyTopSpace + 1;
            AddChild(lblRenderScale);

            const int MinWidth = 1024;
            const int MinHeight = 600;
            int MaxWidth = Screen.PrimaryScreen.Bounds.Width;
            int MaxHeight = Screen.PrimaryScreen.Bounds.Height;

            ddRenderScale = new XNADropDown(WindowManager);
            ddRenderScale.Name = nameof(ddRenderScale);
            ddRenderScale.X = 120;
            ddRenderScale.Y = lblRenderScale.Y - 1;
            ddRenderScale.Width = Width - ddRenderScale.X - Constants.UIEmptySideSpace;
            AddChild(ddRenderScale);
            var renderScales = new double[] { 4.0, 2.5, 3.0, 2.5, 2.0, 1.75, 1.5, 1.25, 1.0, 0.75, 0.5 };
            for (int i = 0; i < renderScales.Length; i++)
            {
                Point2D screenSize = new Point2D((int)(MaxWidth / renderScales[i]), (int)(MaxHeight / renderScales[i]));
                if (screenSize.X > MinWidth && screenSize.Y > MinHeight)
                {
                    ddRenderScale.AddItem(new XNADropDownItem() { Text = renderScales[i].ToString("F2", CultureInfo.InvariantCulture) + "x", Tag = renderScales[i] });
                }
            }

            lblTargetFPS = new XNALabel(WindowManager);
            lblTargetFPS.Name = nameof(lblTargetFPS);
            lblTargetFPS.Text = "目标帧率：";
            lblTargetFPS.X = Constants.UIEmptySideSpace;
            lblTargetFPS.Y = ddRenderScale.Bottom + Constants.UIEmptyTopSpace + 1;
            AddChild(lblTargetFPS);

            ddTargetFPS = new XNADropDown(WindowManager);
            ddTargetFPS.Name = nameof(ddTargetFPS);
            ddTargetFPS.X = ddRenderScale.X;
            ddTargetFPS.Y = lblTargetFPS.Y - 1;
            ddTargetFPS.Width = ddRenderScale.Width;
            AddChild(ddTargetFPS);
            var targetFramerates = new int[] { 1000, 480, 240, 144, 120, 90, 75, 60, 30, 20 };
            foreach (int frameRate in targetFramerates)
                ddTargetFPS.AddItem(new XNADropDownItem() { Text = frameRate.ToString(CultureInfo.InvariantCulture), Tag = frameRate });

            lblTheme = new XNALabel(WindowManager);
            lblTheme.Name = nameof(lblTheme);
            lblTheme.Text = "主题：";
            lblTheme.X = lblRenderScale.X;
            lblTheme.Y = ddTargetFPS.Bottom + Constants.UIEmptyTopSpace;
            AddChild(lblTheme);

            ddTheme = new XNADropDown(WindowManager);
            ddTheme.Name = nameof(ddTheme);
            ddTheme.X = ddRenderScale.X;
            ddTheme.Y = lblTheme.Y - 1;
            ddTheme.Width = ddRenderScale.Width;
            AddChild(ddTheme);
            foreach (var theme in EditorThemes.Themes)
                ddTheme.AddItem(theme.Key);

            lblScrollRate = new XNALabel(WindowManager);
            lblScrollRate.Name = nameof(lblScrollRate);
            lblScrollRate.Text = "滚动速度：";
            lblScrollRate.X = lblRenderScale.X;
            lblScrollRate.Y = ddTheme.Bottom + Constants.UIEmptyTopSpace;
            AddChild(lblScrollRate);

            ddScrollRate = new XNADropDown(WindowManager);
            ddScrollRate.Name = nameof(ddScrollRate);
            ddScrollRate.X = ddRenderScale.X;
            ddScrollRate.Y = lblScrollRate.Y - 1;
            ddScrollRate.Width = ddRenderScale.Width;
            AddChild(ddScrollRate);
            var scrollRateNames = new string[] { "Fastest", "Faster", "Fast", "Normal", "Slow", "Slower", "Slowest" };
            var scrollRateValues = new int[] { 21, 18, 15, 12, 9, 6, 3 };
            for (int i = 0; i < scrollRateNames.Length; i++)
            {
                ddScrollRate.AddItem(new XNADropDownItem() { Text = scrollRateNames[i], Tag = scrollRateValues[i] });
            }

            chkBorderless = new XNACheckBox(WindowManager);
            chkBorderless.Name = nameof(chkBorderless);
            chkBorderless.X = Constants.UIEmptySideSpace;
            chkBorderless.Y = ddScrollRate.Bottom + Constants.UIVerticalSpacing;
            chkBorderless.Text = "无边框模式启动";
            AddChild(chkBorderless);

            chkUseBoldFont = new XNACheckBox(WindowManager);
            chkUseBoldFont.Name = nameof(chkUseBoldFont);
            chkUseBoldFont.X = Constants.UIEmptySideSpace;
            chkUseBoldFont.Y = chkBorderless.Bottom + Constants.UIVerticalSpacing;
            chkUseBoldFont.Text = "使用粗体字体";
            AddChild(chkUseBoldFont);

            chkGraphicsLevel = new XNACheckBox(WindowManager);
            chkGraphicsLevel.Name = nameof(chkGraphicsLevel);
            chkGraphicsLevel.X = Constants.UIEmptySideSpace;
            chkGraphicsLevel.Y = chkUseBoldFont.Bottom + Constants.UIVerticalSpacing;
            chkGraphicsLevel.Text = "增强画质";
            AddChild(chkGraphicsLevel);

            chkSmartScriptActionCloning = new XNACheckBox(WindowManager);
            chkSmartScriptActionCloning.Name = nameof(chkSmartScriptActionCloning);
            chkSmartScriptActionCloning.X = Constants.UIEmptySideSpace;
            chkSmartScriptActionCloning.Y = chkGraphicsLevel.Bottom + Constants.UIVerticalSpacing;
            chkSmartScriptActionCloning.Text = "智能脚本克隆";
            AddChild(chkSmartScriptActionCloning);

            lblTextEditorPath = new XNALabel(WindowManager);
            lblTextEditorPath.Name = nameof(lblTextEditorPath);
            lblTextEditorPath.Text = "文本编辑器路径：";
            lblTextEditorPath.X = Constants.UIEmptySideSpace;
            lblTextEditorPath.Y = chkSmartScriptActionCloning.Bottom + Constants.UIVerticalSpacing * 2;
            AddChild(lblTextEditorPath);

            tbTextEditorPath = new EditorTextBox(WindowManager);
            tbTextEditorPath.Name = nameof(tbTextEditorPath);
            tbTextEditorPath.AllowSemicolon = true;
            tbTextEditorPath.X = Constants.UIEmptySideSpace;
            tbTextEditorPath.Y = lblTextEditorPath.Bottom + Constants.UIVerticalSpacing;
            tbTextEditorPath.Width = Width - tbTextEditorPath.X - Constants.UIEmptySideSpace;
            AddChild(tbTextEditorPath);

            lblGameVersion = new XNALabel(WindowManager);
            lblGameVersion.Name = nameof(lblGameVersion);
            lblGameVersion.Text = "游戏版本：";
            lblGameVersion.X = Constants.UIEmptySideSpace;
            lblGameVersion.Y = tbTextEditorPath.Bottom + Constants.UIVerticalSpacing * 2;
            AddChild(lblGameVersion);

            ddGameVersion = new XNADropDown(WindowManager);
            ddGameVersion.Name = nameof(ddGameVersion);
            ddGameVersion.X = Constants.UIEmptySideSpace;
            ddGameVersion.Y = lblGameVersion.Bottom + Constants.UIVerticalSpacing;
            ddGameVersion.Width = Width - ddGameVersion.X - Constants.UIEmptySideSpace;
            ddGameVersion.AddItem("Yuri's Revenge");
            ddGameVersion.AddItem("MentalOmegaClient");
            ddGameVersion.SelectedIndexChanged += DdGameVersion_SelectedIndexChanged;
            AddChild(ddGameVersion);

            LoadSettings();

            base.Initialize();
            
            // 在初始化完成后，触发一次游戏版本更改事件，确保所有配置同步
            string currentGameVersion = ddGameVersion.SelectedItem?.Text;
            if (!string.IsNullOrEmpty(currentGameVersion))
            {
                Rampastring.Tools.Logger.Log($"初始化时触发游戏版本配置同步: {currentGameVersion}");
                // 手动触发一次版本切换事件，参数设为null表示这是初始化触发，不是用户手动触发
                DdGameVersion_SelectedIndexChanged(null, EventArgs.Empty);
            }
        }

        private void DdGameVersion_SelectedIndexChanged(object sender, EventArgs e)
        {
            string oldGameVersion = UserSettings.Instance.GameVersion.UserDefinedValue;
            string newGameVersion = ddGameVersion.SelectedItem.Text;
            
            // 判断是否是初始化触发（sender为null表示初始化触发）
            bool isInitialSync = sender == null;
            
            // 如果游戏版本没有变化且不是初始化触发，则不做任何处理
            if (oldGameVersion == newGameVersion && !isInitialSync)
                return;
                
            // 如果是初始化触发但版本没变化，直接进行配置同步而不显示消息框
            bool showMessageBox = !isInitialSync || oldGameVersion != newGameVersion;
            
            try
            {
                // 记录旧的可执行文件名，以便日志显示
                string[] oldExecutables = Constants.ExpectedClientExecutableNames != null ? 
                    (string[])Constants.ExpectedClientExecutableNames.Clone() : 
                    new string[0];
                
                // 记录当前游戏目录
                string currentDirectory = string.Empty;
                
                // 查找MainMenu实例
                MainMenu mainMenu = null;
                var parent = Parent;
                while (parent != null)
                {
                    if (parent is MainMenu mm)
                    {
                        mainMenu = mm;
                        break;
                    }
                    parent = parent.Parent;
                }
                
                if (mainMenu != null)
                {
                    // 获取当前显示的目录路径
                    var tbGameDirectory = mainMenu.GetType().GetField("tbGameDirectory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(mainMenu);
                    if (tbGameDirectory != null)
                    {
                        var textProperty = tbGameDirectory.GetType().GetProperty("Text");
                        if (textProperty != null)
                        {
                            currentDirectory = (string)textProperty.GetValue(tbGameDirectory);
                            Rampastring.Tools.Logger.Log($"获取到当前游戏目录: {currentDirectory}");
                        }
                    }
                }
                
                // 保存当前目录到旧版本的设置中
                if (oldGameVersion == "MentalOmegaClient")
                {
                    if (!string.IsNullOrWhiteSpace(currentDirectory))
                    {
                        UserSettings.Instance.MentalOmegaGameDirectory.UserDefinedValue = currentDirectory;
                        Rampastring.Tools.Logger.Log($"保存MO目录: {currentDirectory}");
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(currentDirectory))
                    {
                        UserSettings.Instance.YurisRevengeGameDirectory.UserDefinedValue = currentDirectory;
                        Rampastring.Tools.Logger.Log($"保存YR目录: {currentDirectory}");
                    }
                }
                
                // 立即更新游戏版本设置
                UserSettings.Instance.GameVersion.UserDefinedValue = newGameVersion;
                UserSettings.Instance.SaveSettings();
                
                // 根据所选版本设置对应的可执行文件名
                UpdateExpectedExecutableNames();
                
                // 获取新版本的目录路径
                string newDirectory = null;
                if (newGameVersion == "MentalOmegaClient")
                {
                    newDirectory = UserSettings.Instance.MentalOmegaGameDirectory.GetValue();
                    Rampastring.Tools.Logger.Log($"读取到MO目录: {newDirectory}");
                }
                else
                {
                    newDirectory = UserSettings.Instance.YurisRevengeGameDirectory.GetValue();
                    Rampastring.Tools.Logger.Log($"读取到YR目录: {newDirectory}");
                }
                
                // 如果没有保存的目录，则使用通用设置
                if (string.IsNullOrWhiteSpace(newDirectory))
                {
                    newDirectory = UserSettings.Instance.GameDirectory.GetValue();
                    Rampastring.Tools.Logger.Log($"使用通用目录: {newDirectory}");
                }
                
                // 如果有MainMenu实例，直接更新文本框
                if (mainMenu != null && !string.IsNullOrWhiteSpace(newDirectory))
                {
                    var tbGameDirectory = mainMenu.GetType().GetField("tbGameDirectory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(mainMenu);
                    if (tbGameDirectory != null)
                    {
                        var textProperty = tbGameDirectory.GetType().GetProperty("Text");
                        if (textProperty != null)
                        {
                            textProperty.SetValue(tbGameDirectory, newDirectory);
                            Rampastring.Tools.Logger.Log($"成功更新游戏目录文本框为: {newDirectory}");
                        }
                    }
                }
                
                // 重新加载配置
                ReloadConfigForGameVersion(showMessageBox);
                
                // 触发游戏版本更改事件
                GameVersionChanged?.Invoke(this, newGameVersion);
                
                // 记录日志
                Rampastring.Tools.Logger.Log($"游戏版本已更改: {oldGameVersion} -> {newGameVersion}, " + 
                    $"可执行文件名: {string.Join(",", oldExecutables)} -> {string.Join(",", Constants.ExpectedClientExecutableNames)}");
                
                // 只在非初始化触发时才显示提示框
                if (showMessageBox)
                {
                    // 显示提示框，告知用户游戏版本已切换
                    string message = IsChinese() ? 
                        $"游戏版本已切换为：{newGameVersion}\n\n期望的游戏可执行文件已更改为：{string.Join(", ", Constants.ExpectedClientExecutableNames)}\n\n配置文件已重新加载。" :
                        $"Game version has been changed to: {newGameVersion}\n\nExpected game executable files have been changed to: {string.Join(", ", Constants.ExpectedClientExecutableNames)}\n\nConfiguration files have been reloaded.";
                        
                    string title = IsChinese() ? "游戏版本已切换" : "Game Version Changed";
                    
                    // 如果成功更新目录，添加到提示信息
                    if (mainMenu != null && !string.IsNullOrWhiteSpace(newDirectory))
                    {
                        message += IsChinese() ? 
                            $"\n\n游戏目录已更新为：{newDirectory}" :
                            $"\n\nGame directory has been updated to: {newDirectory}";
                    }
                    
                    TSMapEditor.UI.Windows.EditorMessageBox.Show(
                        WindowManager,
                        title,
                        message,
                        TSMapEditor.UI.Windows.MessageBoxButtons.OK);
                }
            }
            catch (Exception ex)
            {
                Rampastring.Tools.Logger.Log($"游戏版本切换时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 重新加载当前游戏版本对应的配置文件
        /// </summary>
        /// <param name="showErrorMessageBox">是否显示错误消息框</param>
        private void ReloadConfigForGameVersion(bool showErrorMessageBox = true)
        {
            try
            {
                // 保存当前选择的游戏版本
                string currentGameVersion = GetSelectedGameVersion();
                
                // 清除配置缓存
                ConfigManager.ClearCache();
                
                // 重新初始化常量
                Constants.Init(true);
                
                // 重新初始化翻译系统
                TSMapEditor.Translations.IniTranslationManager.Initialize();
                TSMapEditor.Translations.TechnoNameManager.Initialize();
                TSMapEditor.Translations.CategoryNameManager.Initialize();
                TSMapEditor.Translations.OverlayNameManager.Initialize();
                TSMapEditor.Translations.TerrainObjectNameManager.Initialize();
                
                // 更新UI（但不重置游戏版本选择）
                RefreshLanguageWithoutResettingGameVersion(IsChinese());
                
                // 确保游戏版本仍然是当前选择的值
                int gameVersionIndex = ddGameVersion.Items.FindIndex(i => i.Text == currentGameVersion);
                if (gameVersionIndex >= 0)
                {
                    ddGameVersion.SelectedIndex = gameVersionIndex;
                }
                
                // 确保可执行文件名被正确设置
                UpdateExpectedExecutableNames();
                
                // 确保更改游戏版本后立即触发目录路径更新事件
                GameVersionChanged?.Invoke(this, currentGameVersion);
                
                Rampastring.Tools.Logger.Log($"游戏配置已重新加载，当前版本: {currentGameVersion}，可执行文件: {string.Join(", ", Constants.ExpectedClientExecutableNames)}");
            }
            catch (Exception ex)
            {
                Rampastring.Tools.Logger.Log($"重新加载配置时出错: {ex.Message}\n{ex.StackTrace}");
                
                // 只在需要时显示错误消息框
                if (showErrorMessageBox)
                {
                    string message = IsChinese() ? 
                        $"重新加载配置时发生错误：{ex.Message}" :
                        $"Error while reloading configuration: {ex.Message}";
                        
                    string title = IsChinese() ? "重新加载配置错误" : "Configuration Reload Error";
                    
                    TSMapEditor.UI.Windows.EditorMessageBox.Show(
                        WindowManager,
                        title,
                        message,
                        TSMapEditor.UI.Windows.MessageBoxButtons.OK);
                }
            }
        }
        
        /// <summary>
        /// 刷新语言设置，但不重置游戏版本选择
        /// </summary>
        /// <param name="isChinese">是否使用中文</param>
        private void RefreshLanguageWithoutResettingGameVersion(bool isChinese)
        {
            lblHeader.Text = isChinese ? "设置" : "Settings";
            lblRenderScale.Text = isChinese ? "渲染缩放：" : "Render Scale:";
            lblTargetFPS.Text = isChinese ? "目标帧率：" : "Target FPS:";
            lblTheme.Text = isChinese ? "主题：" : "Theme:";
            lblScrollRate.Text = isChinese ? "滚动速度：" : "Scroll Rate:";
            chkBorderless.Text = isChinese ? "无边框模式启动" : "Enable Borderless Mode";
            chkUseBoldFont.Text = isChinese ? "使用粗体字体" : "Use Bold Font";
            chkGraphicsLevel.Text = isChinese ? "增强画质" : "Enhanced Graphics";
            chkSmartScriptActionCloning.Text = isChinese ? "智能脚本克隆" : "Smart Script Cloning";
            lblTextEditorPath.Text = isChinese ? "文本编辑器路径：" : "Text Editor Path:";
            lblGameVersion.Text = isChinese ? "游戏版本：" : "Game Version:";

            var userSettings = UserSettings.Instance;
            
            // 渲染缩放下拉框内容
            double selectedRenderScale = ddRenderScale.SelectedItem != null ? (double)ddRenderScale.SelectedItem.Tag : userSettings.RenderScale.UserDefinedValue;
            double[] renderScales = { 4.0, 3.0, 2.5, 2.0, 1.75, 1.5, 1.25, 1.0, 0.75, 0.5 };
            ddRenderScale.Items.Clear();
            foreach (var scale in renderScales)
            {
                ddRenderScale.AddItem(new XNADropDownItem() { Text = scale.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "x", Tag = scale });
            }
            ddRenderScale.SelectedIndex = ddRenderScale.Items.FindIndex(i => (double)i.Tag == selectedRenderScale);

            // 目标帧率下拉框内容
            int selectedTargetFPS = ddTargetFPS.SelectedItem != null ? (int)ddTargetFPS.SelectedItem.Tag : userSettings.TargetFPS.UserDefinedValue;
            int[] targetFramerates = { 1000, 480, 240, 144, 120, 90, 75, 60, 30, 20 };
            ddTargetFPS.Items.Clear();
            foreach (int frameRate in targetFramerates)
                ddTargetFPS.AddItem(new XNADropDownItem() { Text = frameRate.ToString(System.Globalization.CultureInfo.InvariantCulture), Tag = frameRate });
            ddTargetFPS.SelectedIndex = ddTargetFPS.Items.FindIndex(item => (int)item.Tag == selectedTargetFPS);

            // 主题下拉框内容（假设主题名不变）
            string selectedTheme = ddTheme.SelectedItem != null ? ddTheme.SelectedItem.Text : userSettings.Theme.UserDefinedValue;
            ddTheme.Items.Clear();
            foreach (var theme in EditorThemes.Themes)
                ddTheme.AddItem(theme.Key);
            int themeIndex = ddTheme.Items.FindIndex(i => i.Text == selectedTheme);
            if (themeIndex == -1)
                themeIndex = ddTheme.Items.FindIndex(i => i.Text == "Default");
            ddTheme.SelectedIndex = themeIndex;

            // 滚动速度下拉框内容
            int selectedScrollRate = ddScrollRate.SelectedItem != null ? (int)ddScrollRate.SelectedItem.Tag : userSettings.ScrollRate.UserDefinedValue;
            string[] scrollRateNamesCN = { "最快", "更快", "快", "正常", "慢", "更慢", "最慢" };
            string[] scrollRateNamesEN = { "Fastest", "Faster", "Fast", "Normal", "Slow", "Slower", "Slowest" };
            int[] scrollRateValues = { 21, 18, 15, 12, 9, 6, 3 };
            ddScrollRate.Items.Clear();
            for (int i = 0; i < scrollRateNamesEN.Length; i++)
            {
                ddScrollRate.AddItem(new XNADropDownItem() { Text = isChinese ? scrollRateNamesCN[i] : scrollRateNamesEN[i], Tag = scrollRateValues[i] });
            }
            ddScrollRate.SelectedIndex = ddScrollRate.Items.FindIndex(item => (int)item.Tag == selectedScrollRate);

            // 游戏版本下拉框内容，保留当前选择
            string currentGameVersion = ddGameVersion.SelectedItem?.Text ?? userSettings.GameVersion.UserDefinedValue;
            ddGameVersion.Items.Clear();
            ddGameVersion.AddItem("Yuri's Revenge");
            ddGameVersion.AddItem("MentalOmegaClient");
            int gameVersionIndex = ddGameVersion.Items.FindIndex(i => i.Text == currentGameVersion);
            if (gameVersionIndex == -1)
                gameVersionIndex = 0;
            ddGameVersion.SelectedIndex = gameVersionIndex;
            
            // 保持原有的其他设置
            chkBorderless.Checked = userSettings.Borderless.UserDefinedValue;
            chkUseBoldFont.Checked = userSettings.UseBoldFont.UserDefinedValue;
            chkGraphicsLevel.Checked = userSettings.GraphicsLevel.UserDefinedValue > 0;
            chkSmartScriptActionCloning.Checked = userSettings.SmartScriptActionCloning.UserDefinedValue;
            tbTextEditorPath.Text = userSettings.TextEditorPath.UserDefinedValue ?? string.Empty;
        }
        
        // 判断当前是否为中文界面
        private bool IsChinese()
        {
            return MainMenu.IsChinese;
        }

        private void LoadSettings()
        {
            var userSettings = UserSettings.Instance;

            ddRenderScale.SelectedIndex = ddRenderScale.Items.FindIndex(i => (double)i.Tag == userSettings.RenderScale.UserDefinedValue);
            ddTargetFPS.SelectedIndex = ddTargetFPS.Items.FindIndex(item => (int)item.Tag == userSettings.TargetFPS.UserDefinedValue);

            int selectedTheme = ddTheme.Items.FindIndex(i => i.Text == userSettings.Theme.UserDefinedValue);
            if (selectedTheme == -1)
                selectedTheme = ddTheme.Items.FindIndex(i => i.Text == "Default");
            ddTheme.SelectedIndex = selectedTheme;

            int selectedGameVersion = ddGameVersion.Items.FindIndex(i => i.Text == userSettings.GameVersion.UserDefinedValue);
            if (selectedGameVersion == -1)
                selectedGameVersion = 0;
            ddGameVersion.SelectedIndex = selectedGameVersion;
                    
            // 根据保存的游戏版本设置正确的可执行文件名
            UpdateExpectedExecutableNames();

            ddScrollRate.SelectedIndex = ddScrollRate.Items.FindIndex(item => (int)item.Tag == userSettings.ScrollRate.UserDefinedValue);

            chkBorderless.Checked = userSettings.Borderless.UserDefinedValue;
            chkUseBoldFont.Checked = userSettings.UseBoldFont.UserDefinedValue;
            chkGraphicsLevel.Checked = userSettings.GraphicsLevel.UserDefinedValue > 0;
            chkSmartScriptActionCloning.Checked = userSettings.SmartScriptActionCloning.UserDefinedValue;

            // 添加空值检查，避免设置为null
            tbTextEditorPath.Text = userSettings.TextEditorPath.UserDefinedValue ?? string.Empty;
        }

        public void ApplySettings()
        {
            var userSettings = UserSettings.Instance;

            userSettings.RenderScale.UserDefinedValue = (double)ddRenderScale.SelectedItem.Tag;
            userSettings.TargetFPS.UserDefinedValue = (int)ddTargetFPS.SelectedItem.Tag;
            userSettings.Theme.UserDefinedValue = ddTheme.SelectedItem.Text;
            userSettings.UseBoldFont.UserDefinedValue = chkUseBoldFont.Checked;
            userSettings.Borderless.UserDefinedValue = chkBorderless.Checked;
            userSettings.ScrollRate.UserDefinedValue = (int)ddScrollRate.SelectedItem.Tag;
            userSettings.GraphicsLevel.UserDefinedValue = chkGraphicsLevel.Checked ? 1 : 0;
            userSettings.SmartScriptActionCloning.UserDefinedValue = chkSmartScriptActionCloning.Checked;
            userSettings.TextEditorPath.UserDefinedValue = tbTextEditorPath.Text;
            userSettings.GameVersion.UserDefinedValue = ddGameVersion.SelectedItem.Text;

            // 确保根据游戏版本设置正确的可执行文件名
            UpdateExpectedExecutableNames();

            userSettings.SaveSettings();
        }

        public void RefreshLanguage(bool isChinese)
        {
            lblHeader.Text = isChinese ? "设置" : "Settings";
            lblRenderScale.Text = isChinese ? "渲染缩放：" : "Render Scale:";
            lblTargetFPS.Text = isChinese ? "目标帧率：" : "Target FPS:";
            lblTheme.Text = isChinese ? "主题：" : "Theme:";
            lblScrollRate.Text = isChinese ? "滚动速度：" : "Scroll Rate:";
            chkBorderless.Text = isChinese ? "无边框模式启动" : "Enable Borderless Mode";
            chkUseBoldFont.Text = isChinese ? "使用粗体字体" : "Use Bold Font";
            chkGraphicsLevel.Text = isChinese ? "增强画质" : "Enhanced Graphics";
            chkSmartScriptActionCloning.Text = isChinese ? "智能脚本克隆" : "Smart Script Cloning";
            lblTextEditorPath.Text = isChinese ? "文本编辑器路径：" : "Text Editor Path:";
            lblGameVersion.Text = isChinese ? "游戏版本：" : "Game Version:";

            // 渲染缩放下拉框内容
            double[] renderScales = { 4.0, 3.0, 2.5, 2.0, 1.75, 1.5, 1.25, 1.0, 0.75, 0.5 };
            ddRenderScale.Items.Clear();
            foreach (var scale in renderScales)
            {
                ddRenderScale.AddItem(new XNADropDownItem() { Text = scale.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "x", Tag = scale });
            }

            // 目标帧率下拉框内容
            int[] targetFramerates = { 1000, 480, 240, 144, 120, 90, 75, 60, 30, 20 };
            ddTargetFPS.Items.Clear();
            foreach (int frameRate in targetFramerates)
                ddTargetFPS.AddItem(new XNADropDownItem() { Text = frameRate.ToString(System.Globalization.CultureInfo.InvariantCulture), Tag = frameRate });

            // 主题下拉框内容（假设主题名不变）
            ddTheme.Items.Clear();
            foreach (var theme in EditorThemes.Themes)
                ddTheme.AddItem(theme.Key);

            // 滚动速度下拉框内容
            string[] scrollRateNamesCN = { "最快", "更快", "快", "正常", "慢", "更慢", "最慢" };
            string[] scrollRateNamesEN = { "Fastest", "Faster", "Fast", "Normal", "Slow", "Slower", "Slowest" };
            int[] scrollRateValues = { 21, 18, 15, 12, 9, 6, 3 };
            ddScrollRate.Items.Clear();
            for (int i = 0; i < scrollRateNamesEN.Length; i++)
            {
                ddScrollRate.AddItem(new XNADropDownItem() { Text = isChinese ? scrollRateNamesCN[i] : scrollRateNamesEN[i], Tag = scrollRateValues[i] });
            }

            // 游戏版本下拉框内容
            ddGameVersion.Items.Clear();
            ddGameVersion.AddItem("Yuri's Revenge");
            ddGameVersion.AddItem("MentalOmegaClient");

            // 保持原有选择
            LoadSettings();
        }
    }
}
