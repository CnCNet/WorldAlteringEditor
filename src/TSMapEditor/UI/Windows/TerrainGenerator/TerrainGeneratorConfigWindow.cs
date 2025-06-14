using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.Settings;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows.TerrainGenerator
{
    /// <summary>
    /// A window that allows the user to configure the terrain generator (see <see cref="TerrainGeneratorConfiguration"/>).
    /// </summary>
    public class TerrainGeneratorConfigWindow : EditorWindow
    {
        public TerrainGeneratorConfigWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        public event EventHandler ConfigApplied;

        private readonly Map map;

        public TerrainGeneratorConfiguration TerrainGeneratorConfig { get; private set; }

        private TerrainGeneratorTerrainTypeGroupsPanel terrainTypeGroupsPanel;
        private TerrainGeneratorTileGroupsPanel tileGroupsPanel;
        private TerrainGeneratorOverlayGroupsPanel overlayGroupsPanel;
        private TerrainGeneratorSmudgeGroupsPanel smudgeGroupsPanel;

        private TerrainGeneratorUserPresets terrainGeneratorUserPresets;
        private InputTerrainGeneratorPresetNameWindow inputTerrainGeneratorPresetNameWindow;
        private DeleteTerrainGeneratorPresetWindow deleteTerrainGeneratorPresetWindow;

        private XNADropDown ddPresets;
        private XNALabel lblHeader;
        private XNALabel lblPresets;
        private EditorButton btnSaveConfig;
        private EditorButton btnDeleteConfig;
        private XNATabControl tabControl;
        private EditorButton btnApply;

        private XNAPanel[] panels;

        public override void Initialize()
        {
            Width = 900;
            Name = nameof(TerrainGeneratorConfigWindow);

            lblHeader = new XNALabel(WindowManager);
            lblHeader.Name = nameof(lblHeader);
            lblHeader.FontIndex = Constants.UIBoldFont;
            lblHeader.Y = Constants.UIEmptyTopSpace;
            AddChild(lblHeader);
            lblHeader.CenterOnParentHorizontally();

            lblPresets = new XNALabel(WindowManager);
            lblPresets.Name = nameof(lblPresets);
            lblPresets.Y = lblHeader.Bottom + Constants.UIEmptyTopSpace;
            lblPresets.X = Constants.UIEmptySideSpace;
            lblPresets.Width = 120;
            AddChild(lblPresets);

            ddPresets = new XNADropDown(WindowManager);
            ddPresets.Name = nameof(ddPresets);
            ddPresets.Width = 350;
            ddPresets.Y = lblPresets.Y - 1;
            ddPresets.X = lblPresets.Right + Constants.UIHorizontalSpacing;
            AddChild(ddPresets);
            ddPresets.SelectedIndexChanged += DdPresets_SelectedIndexChanged;
            InitPresets();

            btnSaveConfig = new EditorButton(WindowManager);
            btnSaveConfig.Name = nameof(btnSaveConfig);
            btnSaveConfig.Width = 200;
            btnSaveConfig.X = ddPresets.Right + Constants.UIHorizontalSpacing * 2;
            btnSaveConfig.Y = ddPresets.Y;
            AddChild(btnSaveConfig);
            btnSaveConfig.LeftClick += BtnSaveConfig_LeftClick;

            btnDeleteConfig = new EditorButton(WindowManager);
            btnDeleteConfig.Name = nameof(btnDeleteConfig);
            btnDeleteConfig.Width = 200;
            btnDeleteConfig.X = btnSaveConfig.Right + Constants.UIHorizontalSpacing;
            btnDeleteConfig.Y = btnSaveConfig.Y;
            AddChild(btnDeleteConfig);
            btnDeleteConfig.LeftClick += BtnDeleteConfig_LeftClick;

            var customUISettings = UISettings.ActiveSettings as CustomUISettings;

            const int tabWidth = 170;
            const int tabHeight = 24;

            var idleTexture = Helpers.CreateUITexture(GraphicsDevice, tabWidth, tabHeight,
                customUISettings.ButtonMainBackgroundColor,
                customUISettings.ButtonSecondaryBackgroundColor,
                customUISettings.ButtonTertiaryBackgroundColor);

            var selectedTexture = Helpers.CreateUITexture(GraphicsDevice, tabWidth, tabHeight,
                new Color(128, 128, 128, 196),
                new Color(128, 128, 128, 255), Color.White);

            tabControl = new XNATabControl(WindowManager);
            tabControl.Name = nameof(tabControl);
            tabControl.X = Constants.UIEmptySideSpace;
            tabControl.Y = ddPresets.Bottom + Constants.UIEmptyTopSpace;
            tabControl.Width = Width;
            tabControl.Height = idleTexture.Height;
            tabControl.FontIndex = Constants.UIBoldFont;
            AddChild(tabControl);
            tabControl.SelectedIndexChanged += (s, e) => { HideAllPanels(); panels[tabControl.SelectedTab].Enable(); };

            panels = new XNAPanel[4];

            terrainTypeGroupsPanel = new TerrainGeneratorTerrainTypeGroupsPanel(WindowManager, map);
            terrainTypeGroupsPanel.Name = nameof(terrainTypeGroupsPanel);
            terrainTypeGroupsPanel.X = Constants.UIEmptySideSpace;
            terrainTypeGroupsPanel.Y = tabControl.Bottom + 1;
            terrainTypeGroupsPanel.Width = Width - Constants.UIEmptySideSpace * 2;
            AddChild(terrainTypeGroupsPanel);
            panels[0] = terrainTypeGroupsPanel;

            tileGroupsPanel = new TerrainGeneratorTileGroupsPanel(WindowManager, map);
            tileGroupsPanel.Name = nameof(tileGroupsPanel);
            tileGroupsPanel.X = terrainTypeGroupsPanel.X;
            tileGroupsPanel.Y = terrainTypeGroupsPanel.Y;
            tileGroupsPanel.Width = terrainTypeGroupsPanel.Width;
            tileGroupsPanel.Height = terrainTypeGroupsPanel.Height;
            AddChild(tileGroupsPanel);
            panels[1] = tileGroupsPanel;

            overlayGroupsPanel = new TerrainGeneratorOverlayGroupsPanel(WindowManager, map);
            overlayGroupsPanel.Name = nameof(overlayGroupsPanel);
            overlayGroupsPanel.X = terrainTypeGroupsPanel.X;
            overlayGroupsPanel.Y = terrainTypeGroupsPanel.Y;
            overlayGroupsPanel.Width = terrainTypeGroupsPanel.Width;
            overlayGroupsPanel.Height = terrainTypeGroupsPanel.Height;
            AddChild(overlayGroupsPanel);
            panels[2] = overlayGroupsPanel;

            smudgeGroupsPanel = new TerrainGeneratorSmudgeGroupsPanel(WindowManager, map);
            smudgeGroupsPanel.Name = nameof(smudgeGroupsPanel);
            smudgeGroupsPanel.X = terrainTypeGroupsPanel.X;
            smudgeGroupsPanel.Y = terrainTypeGroupsPanel.Y;
            smudgeGroupsPanel.Width = terrainTypeGroupsPanel.Width;
            smudgeGroupsPanel.Height = terrainTypeGroupsPanel.Height;
            AddChild(smudgeGroupsPanel);
            panels[3] = smudgeGroupsPanel;

            HideAllPanels();

            tabControl.SelectedTab = 0;
            panels[0].Enable();

            btnApply = new EditorButton(WindowManager);
            btnApply.Name = nameof(btnApply);
            btnApply.Y = terrainTypeGroupsPanel.Bottom + Constants.UIEmptyTopSpace;
            btnApply.Width = 100;
            AddChild(btnApply);
            btnApply.CenterOnParentHorizontally();
            btnApply.LeftClick += BtnApply_LeftClick;

            Height = btnApply.Bottom + Constants.UIEmptyBottomSpace;

            var closeButton = new EditorButton(WindowManager);
            closeButton.Name = "btnCloseX";
            closeButton.Width = Constants.UIButtonHeight;
            closeButton.Height = Constants.UIButtonHeight;
            closeButton.Text = "X";
            closeButton.X = Width - closeButton.Width;
            closeButton.Y = 0;
            AddChild(closeButton);
            closeButton.LeftClick += (s, e) => Hide();

            terrainGeneratorUserPresets = new TerrainGeneratorUserPresets(map);
            terrainGeneratorUserPresets.Load();

            inputTerrainGeneratorPresetNameWindow = new InputTerrainGeneratorPresetNameWindow(WindowManager, terrainGeneratorUserPresets, map);
            var presetNameWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, inputTerrainGeneratorPresetNameWindow);
            inputTerrainGeneratorPresetNameWindow.SaveAccepted += InputTerrainGeneratorPresetNameWindow_SaveAccepted;

            InitUserPresets();

            deleteTerrainGeneratorPresetWindow = new DeleteTerrainGeneratorPresetWindow(WindowManager, terrainGeneratorUserPresets);
            var deletePresetWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, deleteTerrainGeneratorPresetWindow);
            deletePresetWindowDarkeningPanel.Hidden += DeletePresetWindowDarkeningPanel_Hidden;

            // 添加语言刷新
            RefreshLanguage(MainMenu.IsChinese);
            
            base.Initialize();
        }
        
        public void RefreshLanguage(bool isChinese)
        {
            lblHeader.Text = isChinese ? "地形生成器配置" : "TERRAIN GENERATOR CONFIGURATION";
            lblHeader.CenterOnParentHorizontally();
            
            lblPresets.Text = isChinese ? "加载预设配置:" : "Load Preset Config:";
            btnSaveConfig.Text = isChinese ? "保存自定义预设..." : "Save Custom Preset...";
            btnDeleteConfig.Text = isChinese ? "删除自定义预设..." : "Delete Custom Preset...";
            
            // 刷新选项卡文本
            // 重新创建选项卡控件
            var customUISettings = UISettings.ActiveSettings as CustomUISettings;
            const int tabWidth = 170;
            const int tabHeight = 24;
            
            var idleTexture = Helpers.CreateUITexture(GraphicsDevice, tabWidth, tabHeight,
                customUISettings.ButtonMainBackgroundColor,
                customUISettings.ButtonSecondaryBackgroundColor,
                customUISettings.ButtonTertiaryBackgroundColor);

            var selectedTexture = Helpers.CreateUITexture(GraphicsDevice, tabWidth, tabHeight,
                new Color(128, 128, 128, 196),
                new Color(128, 128, 128, 255), Color.White);
                
            // 保存当前选中的选项卡索引
            int currentTab = tabControl.SelectedTab;
            
            // 移除旧选项卡控件
            RemoveChild(tabControl);
            
            // 重新创建选项卡控件
            tabControl = new XNATabControl(WindowManager);
            tabControl.Name = nameof(tabControl);
            tabControl.X = Constants.UIEmptySideSpace;
            tabControl.Y = ddPresets.Bottom + Constants.UIEmptyTopSpace;
            tabControl.Width = Width;
            tabControl.Height = idleTexture.Height;
            tabControl.FontIndex = Constants.UIBoldFont;
            AddChild(tabControl);
            
            // 根据当前语言添加选项卡
            if (isChinese)
            {
                tabControl.AddTab("地形类型", idleTexture, selectedTexture);
                tabControl.AddTab("地形图块", idleTexture, selectedTexture);
                tabControl.AddTab("覆盖物", idleTexture, selectedTexture);
                tabControl.AddTab("污迹", idleTexture, selectedTexture);
            }
            else
            {
                tabControl.AddTab("Terrain Types", idleTexture, selectedTexture);
                tabControl.AddTab("Terrain Tiles", idleTexture, selectedTexture);
                tabControl.AddTab("Overlays", idleTexture, selectedTexture);
                tabControl.AddTab("Smudges", idleTexture, selectedTexture);
            }
            
            // 恢复选项卡切换事件
            tabControl.SelectedIndexChanged += (s, e) => { HideAllPanels(); panels[tabControl.SelectedTab].Enable(); };
            
            // 尝试恢复之前选中的选项卡（确保索引有效，我们知道总共有4个选项卡）
            tabControl.SelectedTab = currentTab >= 0 && currentTab < 4 ? currentTab : 0;
            
            // 确保当前面板被启用
            HideAllPanels();
            panels[tabControl.SelectedTab].Enable();
            
            btnApply.Text = isChinese ? "应用" : "Apply";
        }
        
        /// <summary>
        /// 刷新所有子面板的语言
        /// </summary>
        public void RefreshAllPanelsLanguage(bool isChinese)
        {
            // 主窗口语言刷新
            RefreshLanguage(isChinese);
            
            // 子面板语言刷新
            if (terrainTypeGroupsPanel != null)
                terrainTypeGroupsPanel.RefreshLanguage(isChinese);
                
            if (tileGroupsPanel != null)
                tileGroupsPanel.RefreshLanguage(isChinese);
                
            if (overlayGroupsPanel != null)
                overlayGroupsPanel.RefreshLanguage(isChinese);
                
            if (smudgeGroupsPanel != null)
                smudgeGroupsPanel.RefreshLanguage(isChinese);
        }

        private void BtnDeleteConfig_LeftClick(object sender, EventArgs e) => deleteTerrainGeneratorPresetWindow.Open(null);

        private void BtnSaveConfig_LeftClick(object sender, EventArgs e) => inputTerrainGeneratorPresetNameWindow.Open();

        private void InputTerrainGeneratorPresetNameWindow_SaveAccepted(object sender, string presetName)
        {
            var config = GatherConfiguration(presetName);
            if (config == null)
                return;

            terrainGeneratorUserPresets.AddConfig(config);
            InitUserPresets();

            bool success = terrainGeneratorUserPresets.SaveIfDirty();

            if (!success)
            {
                bool isChinese = MainMenu.IsChinese;
                string title = isChinese ? "保存预设失败" : "Failed to save presets";
                string message = isChinese ? 
                    "保存地形生成器预设失败。请查看地图编辑器日志文件了解详情。" : 
                    "Failed to save terrain generator presets. Please see the map editor logfile for details.";
                EditorMessageBox.Show(WindowManager, title, message, MessageBoxButtons.OK);
            }
        }

        private void DeletePresetWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            var configToDelete = deleteTerrainGeneratorPresetWindow.SelectedObject;

            if (configToDelete == null)
                return;

            terrainGeneratorUserPresets.DeleteConfig(configToDelete.Name);
            InitUserPresets();

            bool success = terrainGeneratorUserPresets.SaveIfDirty();

            if (!success)
            {
                bool isChinese = MainMenu.IsChinese;
                string title = isChinese ? "保存预设失败" : "Failed to save presets";
                string message = isChinese ? 
                    "保存地形生成器预设失败。请查看地图编辑器日志文件了解详情。" : 
                    "Failed to save terrain generator presets. Please see the map editor logfile for details.";
                EditorMessageBox.Show(WindowManager, title, message, MessageBoxButtons.OK);
            }
        }

        private void HideAllPanels()
        {
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != null)
                    panels[i].Disable();
            }
        }

        private void InitPresets()
        {
            ddPresets.Items.Clear();
            
            var presetsIni = Helpers.ReadConfigINI("TerrainGeneratorPresets.ini");
            foreach (string sectionName in presetsIni.GetSections())
            {
                string theater = presetsIni.GetStringValue(sectionName, "Theater", string.Empty);
                if (!string.IsNullOrWhiteSpace(theater) && !theater.Equals(map.TheaterName, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var presetConfiguration = TerrainGeneratorConfiguration.FromConfigSection(
                    presetsIni.GetSection(sectionName),
                    false,
                    map.Rules.TerrainTypes,
                    map.TheaterInstance.Theater.TileSets,
                    map.Rules.OverlayTypes,
                    map.Rules.SmudgeTypes);

                if (presetConfiguration != null)
                    ddPresets.AddItem(new XNADropDownItem() { Text = presetConfiguration.Name, Tag = presetConfiguration, TextColor = presetConfiguration.Color });
            }
        }

        private void InitUserPresets()
        {
            // Remove all existing user presets from the dropdown
            var itemsCopy = new List<XNADropDownItem>(ddPresets.Items);

            foreach (var item in itemsCopy)
            {
                var config = (TerrainGeneratorConfiguration)item.Tag;
                if (config.IsUserConfiguration)
                    ddPresets.Items.Remove(item);
            }

            var configs = terrainGeneratorUserPresets.GetConfigurationsForCurrentTheater();
            configs.ForEach(c => ddPresets.AddItem(new XNADropDownItem() { Text = c.Name, Tag = c }));
        }

        private void DdPresets_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ddPresets.SelectedIndex < 0)
                return;

            var config = ddPresets.SelectedItem.Tag as TerrainGeneratorConfiguration;
            LoadConfig(config);
            ddPresets.SelectedIndex = -1;
        }

        private void BtnApply_LeftClick(object sender, EventArgs e)
        {
            TerrainGeneratorConfig = GatherConfiguration("Customized Configuration");

            if (TerrainGeneratorConfig != null)
            {
                // Do not close the window if there's an error condition
                Hide();

                ConfigApplied?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Open()
        {
            // 刷新所有语言
            RefreshAllPanelsLanguage(MainMenu.IsChinese);
            
            Show();

            if (TerrainGeneratorConfig == null)
            {
                // Load first preset as default config if one exists

                if (ddPresets.Items.Count > 0)
                {
                    var config = ddPresets.Items[0].Tag as TerrainGeneratorConfiguration;
                    LoadConfig(config);
                }
                else
                {
                    LoadConfig(new TerrainGeneratorConfiguration("Blank Config",
                        map.LoadedTheaterName,
                        true,
                        new List<TerrainGeneratorTerrainTypeGroup>(),
                        new List<TerrainGeneratorTileGroup>(),
                        new List<TerrainGeneratorOverlayGroup>(),
                        new List<TerrainGeneratorSmudgeGroup>()));
                }
            }
        }

        private void LoadConfig(TerrainGeneratorConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            terrainTypeGroupsPanel.LoadConfig(configuration);
            tileGroupsPanel.LoadConfig(configuration);
            overlayGroupsPanel.LoadConfig(configuration);
            smudgeGroupsPanel.LoadConfig(configuration);
        }

        private TerrainGeneratorConfiguration GatherConfiguration(string name)
        {
            var terrainTypeGroups = terrainTypeGroupsPanel.GetTerrainTypeGroups();
            var tileGroups = tileGroupsPanel.GetTileGroups();
            var overlayGroups = overlayGroupsPanel.GetOverlayGroups();
            var smudgeGroups = smudgeGroupsPanel.GetSmudgeGroups();

            // One of the panels returning null means there's an error condition
            if (terrainTypeGroups == null || tileGroups == null || overlayGroups == null || smudgeGroups == null)
                return null;

            return new TerrainGeneratorConfiguration(name, map.LoadedTheaterName, true, terrainTypeGroups, tileGroups, overlayGroups, smudgeGroups);
        }
    }
}
