using System;
using System.IO;
using System.Linq;
using Rampastring.XNAUI;
using TSMapEditor.Models;
using TSMapEditor.Mutations;
using TSMapEditor.Scripts;
using TSMapEditor.Settings;
using TSMapEditor.UI.Controls;
using TSMapEditor.UI.CursorActions;
using TSMapEditor.UI.Windows;
using TSMapEditor.Models.Enums;
using Rampastring.Tools;
using System.Diagnostics;
using System.ComponentModel;

#if WINDOWS
using System.Windows.Forms;
#endif

namespace TSMapEditor.UI.TopBar
{
    class TopBarMenu : EditorPanel
    {
        public TopBarMenu(WindowManager windowManager, MutationManager mutationManager, MapUI mapUI, Map map, WindowController windowController) : base(windowManager)
        {
            this.mutationManager = mutationManager;
            this.mapUI = mapUI;
            this.map = map;
            this.windowController = windowController;
        }

        public event EventHandler<FileSelectedEventArgs> OnFileSelected;
        public event EventHandler InputFileReloadRequested;
        public event EventHandler MapWideOverlayLoadRequested;

        private readonly MutationManager mutationManager;
        private readonly MapUI mapUI;
        private readonly Map map;
        private readonly WindowController windowController;

        private MenuButton fileButton;
        private MenuButton editButton;
        private MenuButton viewButton;
        private MenuButton toolsButton;
        private MenuButton scriptingButton;
        private MenuButton[] menuButtons;
        
        private EditorContextMenu fileContextMenu;
        private EditorContextMenu editContextMenu;
        private EditorContextMenu viewContextMenu;
        private EditorContextMenu toolsContextMenu;
        private EditorContextMenu scriptingContextMenu;

        private DeleteTubeCursorAction deleteTunnelCursorAction;
        private PlaceTubeCursorAction placeTubeCursorAction;
        private ToggleIceGrowthCursorAction toggleIceGrowthCursorAction;
        private CheckDistanceCursorAction checkDistanceCursorAction;
        private CheckDistancePathfindingCursorAction checkDistancePathfindingCursorAction;
        private CalculateTiberiumValueCursorAction calculateTiberiumValueCursorAction;
        private ManageBaseNodesCursorAction manageBaseNodesCursorAction;
        private PlaceVeinholeMonsterCursorAction placeVeinholeMonsterCursorAction;

        private SelectBridgeWindow selectBridgeWindow;

        public override void Initialize()
        {
            Name = nameof(TopBarMenu);

            deleteTunnelCursorAction = new DeleteTubeCursorAction(mapUI);
            placeTubeCursorAction = new PlaceTubeCursorAction(mapUI);
            toggleIceGrowthCursorAction = new ToggleIceGrowthCursorAction(mapUI);
            checkDistanceCursorAction = new CheckDistanceCursorAction(mapUI);
            checkDistancePathfindingCursorAction = new CheckDistancePathfindingCursorAction(mapUI);
            calculateTiberiumValueCursorAction = new CalculateTiberiumValueCursorAction(mapUI);
            manageBaseNodesCursorAction = new ManageBaseNodesCursorAction(mapUI);
            placeVeinholeMonsterCursorAction = new PlaceVeinholeMonsterCursorAction(mapUI);

            selectBridgeWindow = new SelectBridgeWindow(WindowManager, map);
            var selectBridgeDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectBridgeWindow);
            selectBridgeDarkeningPanel.Hidden += SelectBridgeDarkeningPanel_Hidden;

            windowController.SelectConnectedTileWindow.ObjectSelected += SelectConnectedTileWindow_ObjectSelected;

            fileContextMenu = new EditorContextMenu(WindowManager);
            fileContextMenu.Name = nameof(fileContextMenu);
            fileContextMenu.AddItem("New", () => windowController.CreateNewMapWindow.Open(), null, null, null);
            fileContextMenu.AddItem("Open", () => Open(), null, null, null);

            fileContextMenu.AddItem("Save", () => SaveMap());
            fileContextMenu.AddItem("Save As", () => SaveAs(), null, null, null);
            fileContextMenu.AddItem(" ", null, () => false, null, null);
            fileContextMenu.AddItem("Reload Input File",
                () => InputFileReloadRequested?.Invoke(this, EventArgs.Empty),
                () => !string.IsNullOrWhiteSpace(map.LoadedINI.FileName),
                null, null);
            fileContextMenu.AddItem(" ", null, () => false, null, null);
            fileContextMenu.AddItem("Extract Megamap...", () => windowController.MegamapGenerationOptionsWindow.Open(false));
            fileContextMenu.AddItem("Generate Map Preview...", WriteMapPreviewConfirmation);
            fileContextMenu.AddItem(" ", null, () => false, null, null, null);
            fileContextMenu.AddItem("Open With Text Editor", OpenWithTextEditor, () => !string.IsNullOrWhiteSpace(map.LoadedINI.FileName));
            fileContextMenu.AddItem(" ", null, () => false, null, null);
            fileContextMenu.AddItem("Exit", WindowManager.CloseGame);

            fileButton = new MenuButton(WindowManager, fileContextMenu);
            fileButton.Name = nameof(fileButton);
            fileButton.Text = "File";
            AddChild(fileButton);

            editContextMenu = new EditorContextMenu(WindowManager);
            editContextMenu.Name = nameof(editContextMenu);
            editContextMenu.AddItem("Configure Copied Objects...", () => windowController.CopiedEntryTypesWindow.Open(), null, null, null, () => KeyboardCommands.Instance.ConfigureCopiedObjects.GetKeyDisplayString());
            editContextMenu.AddItem("Copy", () => KeyboardCommands.Instance.Copy.DoTrigger(), null, null, null, () => KeyboardCommands.Instance.Copy.GetKeyDisplayString());
            editContextMenu.AddItem("Copy Custom Shape", () => KeyboardCommands.Instance.CopyCustomShape.DoTrigger(), null, null, null, () => KeyboardCommands.Instance.CopyCustomShape.GetKeyDisplayString());
            editContextMenu.AddItem("Paste", () => KeyboardCommands.Instance.Paste.DoTrigger(), null, null, null, () => KeyboardCommands.Instance.Paste.GetKeyDisplayString());
            editContextMenu.AddItem(" ", null, () => false, null, null);
            editContextMenu.AddItem("Undo", () => mutationManager.Undo(), () => mutationManager.CanUndo(), null, null, () => KeyboardCommands.Instance.Undo.GetKeyDisplayString());
            editContextMenu.AddItem("Redo", () => mutationManager.Redo(), () => mutationManager.CanRedo(), null, null, () => KeyboardCommands.Instance.Redo.GetKeyDisplayString());
            editContextMenu.AddItem("Action History", () => {
                // 确保打开前刷新语言设置
                windowController.HistoryWindow.RefreshLanguage(MainMenu.IsChinese);
                windowController.HistoryWindow.Open();
            });
            editContextMenu.AddItem(" ", null, () => false, null, null);
            editContextMenu.AddItem("Basic", () => windowController.BasicSectionConfigWindow.Open(), null, null, null);
            editContextMenu.AddItem("Map Size", () => windowController.MapSizeWindow.Open(), null, null, null, null);
            editContextMenu.AddItem(" ", null, () => false, null, null);
            editContextMenu.AddItem("Lighting", () => windowController.LightingSettingsWindow.Open(), null, null, null);
            editContextMenu.AddItem(" ", null, () => false, null, null);
            editContextMenu.AddItem("Place Tunnel", () => mapUI.EditorState.CursorAction = placeTubeCursorAction, null, null, null, () => KeyboardCommands.Instance.PlaceTunnel.GetKeyDisplayString());
            editContextMenu.AddItem("Delete Tunnel", () => mapUI.EditorState.CursorAction = deleteTunnelCursorAction, null, null, null);
            editContextMenu.AddItem(" ", null, () => false, null, null);

            int bridgeCount = map.EditorConfig.Bridges.Count;
            if (bridgeCount > 0)
            {
                var bridges = map.EditorConfig.Bridges;
                if (bridgeCount == 1 && bridges[0].Kind == BridgeKind.Low)
                {
                    editContextMenu.AddItem("Draw Low Bridge", () => mapUI.EditorState.CursorAction =
                        new PlaceBridgeCursorAction(mapUI, bridges[0]), null, null, null);
                }
                else
                {
                    editContextMenu.AddItem("Draw Bridge...", SelectBridge, null, null, null);
                }
            }

            var theaterMatchingCliffs = map.EditorConfig.Cliffs.Where(cliff => cliff.AllowedTheaters.Exists(
                theaterName => theaterName.Equals(map.TheaterName, StringComparison.OrdinalIgnoreCase))).ToList();
            int cliffCount = theaterMatchingCliffs.Count;
            if (cliffCount > 0)
            {
                if (cliffCount == 1)
                {
                    editContextMenu.AddItem("Draw Connected Tiles", () => mapUI.EditorState.CursorAction =
                        new DrawCliffCursorAction(mapUI, theaterMatchingCliffs[0]), null, null, null);
                }
                else
                {
                    editContextMenu.AddItem("Repeat Last Connected Tile", RepeatLastConnectedTile, null, null, null, () => KeyboardCommands.Instance.RepeatConnectedTile.GetKeyDisplayString());
                    editContextMenu.AddItem("Draw Connected Tiles...", () => windowController.SelectConnectedTileWindow.Open(), null, null, null, () => KeyboardCommands.Instance.PlaceConnectedTile.GetKeyDisplayString());
                }
            }

            editContextMenu.AddItem("Toggle IceGrowth", () => { mapUI.EditorState.CursorAction = toggleIceGrowthCursorAction; toggleIceGrowthCursorAction.ToggleIceGrowth = true; mapUI.EditorState.HighlightIceGrowth = true; }, null, null, null);
            editContextMenu.AddItem("Clear IceGrowth", () => { mapUI.EditorState.CursorAction = toggleIceGrowthCursorAction; toggleIceGrowthCursorAction.ToggleIceGrowth = false; mapUI.EditorState.HighlightIceGrowth = true; }, null, null, null);
            editContextMenu.AddItem(" ", null, () => false, null, null);
            editContextMenu.AddItem("Manage Base Nodes", ManageBaseNodes_Selected, null, null, null);

            if (map.Rules.OverlayTypes.Exists(ot => ot.ININame == Constants.VeinholeMonsterTypeName) && map.Rules.OverlayTypes.Exists(ot => ot.ININame == Constants.VeinholeDummyTypeName))
            {
                editContextMenu.AddItem(" ", null, () => false, null, null);
                editContextMenu.AddItem("Place Veinhole Monster", () => mapUI.EditorState.CursorAction = placeVeinholeMonsterCursorAction, null, null, null, null);
            }

            editButton = new MenuButton(WindowManager, editContextMenu);
            editButton.Name = nameof(editButton);
            editButton.X = fileButton.Right;
            editButton.Text = "Edit";
            AddChild(editButton);

            viewContextMenu = new EditorContextMenu(WindowManager);                                                                                                    
            viewContextMenu.Name = nameof(viewContextMenu);
            viewContextMenu.AddItem("Configure Rendered Objects...", () => windowController.RenderedObjectsConfigurationWindow.Open());
            viewContextMenu.AddItem(" ", null, () => false, null, null);
            viewContextMenu.AddItem("Toggle Impassable Cells", () => mapUI.EditorState.HighlightImpassableCells = !mapUI.EditorState.HighlightImpassableCells, null, null, null);
            viewContextMenu.AddItem("Toggle IceGrowth Preview", () => mapUI.EditorState.HighlightIceGrowth = !mapUI.EditorState.HighlightIceGrowth, null, null, null);
            viewContextMenu.AddItem(" ", null, () => false, null, null);
            viewContextMenu.AddItem("View Minimap", () => windowController.MinimapWindow.Open());
            viewContextMenu.AddItem(" ", null, () => false, null, null);
            viewContextMenu.AddItem("Find Waypoint...", () => windowController.FindWaypointWindow.Open());
            viewContextMenu.AddItem("Center of Map", () => mapUI.Camera.CenterOnMapCenterCell());
            viewContextMenu.AddItem(" ", null, () => false, null, null);
            viewContextMenu.AddItem("No Lighting", () => mapUI.EditorState.LightingPreviewState = LightingPreviewMode.NoLighting);
            viewContextMenu.AddItem("Normal Lighting", () => mapUI.EditorState.LightingPreviewState = LightingPreviewMode.Normal);
            if (Constants.IsRA2YR)
            {
                viewContextMenu.AddItem("Lightning Storm Lighting", () => mapUI.EditorState.LightingPreviewState = LightingPreviewMode.IonStorm);
                viewContextMenu.AddItem("Dominator Lighting", () => mapUI.EditorState.LightingPreviewState = LightingPreviewMode.Dominator);
            }
            else
            {
                viewContextMenu.AddItem("Ion Storm Lighting", () => mapUI.EditorState.LightingPreviewState = LightingPreviewMode.IonStorm);
            }
            viewContextMenu.AddItem(" ", null, () => false, null, null);
            viewContextMenu.AddItem("Toggle Fullscreen Mode", () => KeyboardCommands.Instance.ToggleFullscreen.DoTrigger());

            viewButton = new MenuButton(WindowManager, viewContextMenu);
            viewButton.Name = nameof(viewButton);
            viewButton.X = editButton.Right;
            viewButton.Text = "View";
            AddChild(viewButton);

            toolsContextMenu = new EditorContextMenu(WindowManager);
            toolsContextMenu.Name = nameof(toolsContextMenu);
            // toolsContextMenu.AddItem("Options");
            if (windowController.AutoApplyImpassableOverlayWindow.IsAvailable)
                toolsContextMenu.AddItem("Apply Impassable Overlay...", () => windowController.AutoApplyImpassableOverlayWindow.Open(), null, null, null);

            toolsContextMenu.AddItem("Terrain Generator Options...", () => windowController.TerrainGeneratorConfigWindow.Open(), null, null, null, () => KeyboardCommands.Instance.ConfigureTerrainGenerator.GetKeyDisplayString());
            toolsContextMenu.AddItem("Generate Terrain", () => EnterTerrainGenerator(), null, null, null, () => KeyboardCommands.Instance.GenerateTerrain.GetKeyDisplayString());
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("Apply INI Code...", () => windowController.ApplyINICodeWindow.Open(), null, null, null);
            toolsContextMenu.AddItem("Run Script...", () => windowController.RunScriptWindow.Open(), null, null, null, null);
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("Deletion Options...", () => windowController.DeletionModeConfigurationWindow.Open());
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("Change Map Height...", () => {
                windowController.ChangeHeightWindow.RefreshLanguage(MainMenu.IsChinese);
                windowController.ChangeHeightWindow.Open();
            }, null, () => !Constants.IsFlatWorld, null, null);
            toolsContextMenu.AddItem(" ", null, () => false, () => !Constants.IsFlatWorld, null);
            toolsContextMenu.AddItem("Smoothen Ice", SmoothenIce, null, null, null, null);
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("Check Distance...", () => mapUI.EditorState.CursorAction = checkDistanceCursorAction, null, null, null, () => KeyboardCommands.Instance.CheckDistance.GetKeyDisplayString());
            toolsContextMenu.AddItem("Check Distance (Pathfinding)...", () => mapUI.EditorState.CursorAction = checkDistancePathfindingCursorAction, null, null, null, () => KeyboardCommands.Instance.CheckDistancePathfinding.GetKeyDisplayString());
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("Calculate Credits...", () => mapUI.EditorState.CursorAction = calculateTiberiumValueCursorAction, null, null, null, () => KeyboardCommands.Instance.CalculateCredits.GetKeyDisplayString());
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("Load Map-Wide Overlay...", () => MapWideOverlayLoadRequested?.Invoke(this, EventArgs.Empty), null, null, null, null);
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("Configure Hotkeys...", () => windowController.HotkeyConfigurationWindow.Open(), null, null, null);
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("About", () => windowController.AboutWindow.Open(), null, null, null, null);

            toolsButton = new MenuButton(WindowManager, toolsContextMenu);
            toolsButton.Name = nameof(toolsButton);
            toolsButton.X = viewButton.Right;
            toolsButton.Text = "Tools";
            AddChild(toolsButton);

            scriptingContextMenu = new EditorContextMenu(WindowManager);
            scriptingContextMenu.Name = nameof(scriptingContextMenu);
            scriptingContextMenu.AddItem("Houses", () => windowController.HousesWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("Triggers", () => windowController.TriggersWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("TaskForces", () => windowController.TaskForcesWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("Scripts", () => windowController.ScriptsWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("TeamTypes", () => windowController.TeamTypesWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("Local Variables", () => windowController.LocalVariablesWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("AITriggers", () => windowController.AITriggersWindow.Open(), null, null, null, null);

            scriptingButton = new MenuButton(WindowManager, scriptingContextMenu);
            scriptingButton.Name = nameof(scriptingButton);
            scriptingButton.X = toolsButton.Right;
            scriptingButton.Text = "Scripting";
            AddChild(scriptingButton);

            base.Initialize();

            Height = fileButton.Height;

            menuButtons = new MenuButton[] { fileButton, editButton, viewButton, toolsButton, scriptingButton };
            Array.ForEach(menuButtons, b => b.MouseEnter += MenuButton_MouseEnter);

            KeyboardCommands.Instance.ConfigureCopiedObjects.Triggered += (s, e) => windowController.CopiedEntryTypesWindow.Open();
            KeyboardCommands.Instance.GenerateTerrain.Triggered += (s, e) => EnterTerrainGenerator();
            KeyboardCommands.Instance.ConfigureTerrainGenerator.Triggered += (s, e) => windowController.TerrainGeneratorConfigWindow.Open();
            KeyboardCommands.Instance.PlaceTunnel.Triggered += (s, e) => mapUI.EditorState.CursorAction = placeTubeCursorAction;
            KeyboardCommands.Instance.PlaceConnectedTile.Triggered += (s, e) => windowController.SelectConnectedTileWindow.Open();
            KeyboardCommands.Instance.RepeatConnectedTile.Triggered += (s, e) => RepeatLastConnectedTile();
            KeyboardCommands.Instance.CalculateCredits.Triggered += (s, e) => mapUI.EditorState.CursorAction = calculateTiberiumValueCursorAction;
            KeyboardCommands.Instance.CheckDistance.Triggered += (s, e) => mapUI.EditorState.CursorAction = checkDistanceCursorAction;
            KeyboardCommands.Instance.CheckDistancePathfinding.Triggered += (s, e) => mapUI.EditorState.CursorAction = checkDistancePathfindingCursorAction;
            KeyboardCommands.Instance.Save.Triggered += (s, e) => SaveMap();

            windowController.TerrainGeneratorConfigWindow.ConfigApplied += TerrainGeneratorConfigWindow_ConfigApplied;
        }

        private void TerrainGeneratorConfigWindow_ConfigApplied(object sender, EventArgs e)
        {
            EnterTerrainGenerator();
        }

        private void SaveMap()
        {
            if (string.IsNullOrWhiteSpace(map.LoadedINI.FileName))
            {
                SaveAs();
                return;
            }

            TrySaveMap();
        }

        private void TrySaveMap()
        {
            try
            {
                map.Save();
            }
            catch (Exception ex)
            {
                if (ex is UnauthorizedAccessException || ex is IOException)
                {
                    Logger.Log("Failed to save the map file. Returned error message: " + ex.Message);

                    EditorMessageBox.Show(WindowManager, "Failed to save map",
                        "Failed to write the map file. Please make sure that WAE has write access to the path." + Environment.NewLine + Environment.NewLine +
                        "A common source of this error is trying to save the map to Program Files or another" + Environment.NewLine +
                        "write-protected directory without running WAE with administrative rights." + Environment.NewLine + Environment.NewLine +
                        "Returned error was: " + ex.Message, Windows.MessageBoxButtons.OK);
                }
                else
                {
                    throw;
                }
            }
        }

        private void WriteMapPreviewConfirmation()
        {
            string title = MainMenu.IsChinese ? "确认" : "Confirmation";
            string message = MainMenu.IsChinese ? 
                "这将把当前小地图作为地图预览写入地图文件。" + Environment.NewLine + Environment.NewLine +
                "如果地图作为自定义地图在CnCNet客户端或游戏中使用，" + Environment.NewLine + 
                "这将为地图提供预览，但如果地图将有外部预览则不是必须的。" + Environment.NewLine +
                "这也会显著增加地图文件的大小。" + Environment.NewLine + Environment.NewLine +
                "您要继续吗？" + Environment.NewLine + Environment.NewLine +
                "注意：预览不会真正写入地图，直到您保存地图。" :

                "This will write the current minimap as the map preview to the map file." + Environment.NewLine + Environment.NewLine +
                "This provides the map with a preview if it is used as a custom map" + Environment.NewLine + 
                "in the CnCNet Client or in-game, but is not necessary if the map will" + Environment.NewLine +
                "have an external preview. It will also significantly increase the size" + Environment.NewLine +
                "of the map file." + Environment.NewLine + Environment.NewLine +
                "Do you want to continue?" + Environment.NewLine + Environment.NewLine +
                "Note: The preview won't be actually written to the map before" + Environment.NewLine + 
                "you save the map.";

            var messageBox = EditorMessageBox.Show(WindowManager, title, message, Windows.MessageBoxButtons.YesNo);

            messageBox.YesClickedAction = _ => windowController.MegamapGenerationOptionsWindow.Open(true);
        }

        private void RepeatLastConnectedTile()
        {
            if (windowController.SelectConnectedTileWindow.SelectedObject == null)
                windowController.SelectConnectedTileWindow.Open();
            else
                SelectConnectedTileWindow_ObjectSelected(this, EventArgs.Empty);
        }

        private void OpenWithTextEditor()
        {
            string textEditorPath = UserSettings.Instance.TextEditorPath;

            if (string.IsNullOrWhiteSpace(textEditorPath) || !File.Exists(textEditorPath))
            {
                textEditorPath = GetDefaultTextEditorPath();

                if (textEditorPath == null)
                {
                    EditorMessageBox.Show(WindowManager, "No text editor found!", "No valid text editor has been configured and no default choice was found.", Windows.MessageBoxButtons.OK);
                    return;
                }
            }

            try
            {
                Process.Start(textEditorPath, "\"" + map.LoadedINI.FileName + "\"");
            }
            catch (Exception ex) when (ex is Win32Exception || ex is ObjectDisposedException)
            {
                Logger.Log("Failed to launch text editor! Message: " + ex.Message);
                EditorMessageBox.Show(WindowManager, "Failed to launch text editor",
                    "An error occurred when trying to open the map file with the text editor." + Environment.NewLine + Environment.NewLine +
                    "Received error was: " + ex.Message, Windows.MessageBoxButtons.OK);
            }
        }

        private string GetDefaultTextEditorPath()
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            var pathsToSearch = new[]
            {
                Path.Combine(programFiles, "Notepad++", "notepad++.exe"),
                Path.Combine(programFilesX86, "Notepad++", "notepad++.exe"),
                Path.Combine(programFiles, "Microsoft VS Code", "vscode.exe"),
                Path.Combine(Environment.SystemDirectory, "notepad.exe"),
            };

            foreach (string path in pathsToSearch)
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        private void ManageBaseNodes_Selected()
        {
            if (map.Houses.Count == 0)
            {
                string title = MainMenu.IsChinese ? "需要阵营设置" : "Houses Required";
                string message;
                
                if (MainMenu.IsChinese)
                {
                    message = "地图没有设置阵营。需要先配置阵营，才能添加基地节点。" + Environment.NewLine + Environment.NewLine +
                              "您可以从\"脚本 -> 阵营\"菜单中配置阵营。";
                }
                else
                {
                    message = "The map has no houses set up. Houses need to be configured before base nodes can be added." + Environment.NewLine + Environment.NewLine +
                              "You can configure Houses from Scripting -> Houses.";
                }
                
                EditorMessageBox.Show(WindowManager, title, message, TSMapEditor.UI.Windows.MessageBoxButtons.OK);

                return;
            }

            mapUI.EditorState.CursorAction = manageBaseNodesCursorAction;
        }

        private void SmoothenIce()
        {
            new SmoothenIceScript().Perform(map);
            mapUI.InvalidateMap();
        }

        private void EnterTerrainGenerator()
        {
            if (windowController.TerrainGeneratorConfigWindow.TerrainGeneratorConfig == null)
            {
                windowController.TerrainGeneratorConfigWindow.Open();
                return;
            }

            var generateTerrainCursorAction = new GenerateTerrainCursorAction(mapUI);
            generateTerrainCursorAction.TerrainGeneratorConfiguration = windowController.TerrainGeneratorConfigWindow.TerrainGeneratorConfig;
            mapUI.CursorAction = generateTerrainCursorAction;
        }

        private void SelectBridge()
        {
            selectBridgeWindow.Open();
        }

        private void SelectBridgeDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectBridgeWindow.SelectedObject != null)
                mapUI.EditorState.CursorAction = new PlaceBridgeCursorAction(mapUI, selectBridgeWindow.SelectedObject);
        }

        private void SelectConnectedTileWindow_ObjectSelected(object sender, EventArgs e)
        {
            mapUI.EditorState.CursorAction = new DrawCliffCursorAction(mapUI, windowController.SelectConnectedTileWindow.SelectedObject);
        }

        private void Open()
        {
#if WINDOWS
            string initialPath = string.IsNullOrWhiteSpace(UserSettings.Instance.LastScenarioPath.GetValue()) ? UserSettings.Instance.GameDirectory : Path.GetDirectoryName(UserSettings.Instance.LastScenarioPath.GetValue());

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = initialPath;
                openFileDialog.Filter = Constants.OpenFileDialogFilter.Replace(':', ';');
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    OnFileSelected?.Invoke(this, new FileSelectedEventArgs(openFileDialog.FileName));
                }
            }
#else
            windowController.OpenMapWindow.Open();
#endif
        }

        private void SaveAs()
        {
#if WINDOWS
            string initialPath = string.IsNullOrWhiteSpace(UserSettings.Instance.LastScenarioPath.GetValue()) ? UserSettings.Instance.GameDirectory : UserSettings.Instance.LastScenarioPath.GetValue();

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = Path.GetDirectoryName(initialPath);
                saveFileDialog.FileName = Path.GetFileName(initialPath);
                saveFileDialog.Filter = Constants.OpenFileDialogFilter.Replace(':', ';');
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    map.LoadedINI.FileName = saveFileDialog.FileName;
                    TrySaveMap();

                    if (UserSettings.Instance.LastScenarioPath.GetValue() != saveFileDialog.FileName)
                    {
                        UserSettings.Instance.RecentFiles.PutEntry(saveFileDialog.FileName);
                        UserSettings.Instance.LastScenarioPath.UserDefinedValue = saveFileDialog.FileName;
                        _ = UserSettings.Instance.SaveSettingsAsync();
                    }
                }
            }
#else
            windowController.SaveMapAsWindow.Open();
#endif
        }

        private void MenuButton_MouseEnter(object sender, EventArgs e)
        {
            var menuButton = (MenuButton)sender;

            // Is a menu open?
            int openIndex = Array.FindIndex(menuButtons, b => b.ContextMenu.Enabled);
            if (openIndex > -1)
            {
                // Switch to the new button's menu
                menuButtons[openIndex].ContextMenu.Disable();
                menuButton.OpenContextMenu();
            }
        }

        public void RefreshLanguage(bool isChinese)
        {
            // 更新菜单按钮的文本
            fileButton.Text = isChinese ? "文件" : "File";
            editButton.Text = isChinese ? "编辑" : "Edit";
            viewButton.Text = isChinese ? "视图" : "View";
            toolsButton.Text = isChinese ? "工具" : "Tools";
            scriptingButton.Text = isChinese ? "脚本" : "Scripting";
            
            // 更新File菜单项
            if (fileContextMenu != null && fileContextMenu.Items.Count > 0)
            {
                int index = 0;
                fileContextMenu.Items[index++].Text = isChinese ? "新建" : "New";
                fileContextMenu.Items[index++].Text = isChinese ? "打开" : "Open";
                fileContextMenu.Items[index++].Text = isChinese ? "保存" : "Save";
                fileContextMenu.Items[index++].Text = isChinese ? "另存为" : "Save As";
                fileContextMenu.Items[index++].Text = " "; // 分隔符
                fileContextMenu.Items[index++].Text = isChinese ? "重载输入文件" : "Reload Input File";
                fileContextMenu.Items[index++].Text = " "; // 分隔符
                fileContextMenu.Items[index++].Text = isChinese ? "提取巨型地图..." : "Extract Megamap...";
                fileContextMenu.Items[index++].Text = isChinese ? "生成地图预览..." : "Generate Map Preview...";
                fileContextMenu.Items[index++].Text = " "; // 分隔符
                fileContextMenu.Items[index++].Text = isChinese ? "用文本编辑器打开" : "Open With Text Editor";
                fileContextMenu.Items[index++].Text = " "; // 分隔符
                fileContextMenu.Items[index++].Text = isChinese ? "退出" : "Exit";
            }
            
            // 更新Edit菜单项
            if (editContextMenu != null && editContextMenu.Items.Count > 0)
            {
                int index = 0;
                editContextMenu.Items[index++].Text = isChinese ? "配置复制对象..." : "Configure Copied Objects...";
                editContextMenu.Items[index++].Text = isChinese ? "复制" : "Copy";
                editContextMenu.Items[index++].Text = isChinese ? "自定义形状复制" : "Copy Custom Shape";
                editContextMenu.Items[index++].Text = isChinese ? "粘贴" : "Paste";
                editContextMenu.Items[index++].Text = " "; // 分隔符
                editContextMenu.Items[index++].Text = isChinese ? "撤销" : "Undo";
                editContextMenu.Items[index++].Text = isChinese ? "重做" : "Redo";
                editContextMenu.Items[index++].Text = isChinese ? "操作历史" : "Action History";
                editContextMenu.Items[index++].Text = " "; // 分隔符
                editContextMenu.Items[index++].Text = isChinese ? "基本" : "Basic";
                editContextMenu.Items[index++].Text = isChinese ? "地图大小" : "Map Size";
                editContextMenu.Items[index++].Text = " "; // 分隔符
                editContextMenu.Items[index++].Text = isChinese ? "光照" : "Lighting";
                
                // 检查剩余菜单项数量，因为有些菜单项可能是条件生成的
                if (index < editContextMenu.Items.Count && editContextMenu.Items[index].Text == " ")
                {
                    editContextMenu.Items[index++].Text = " "; // 分隔符
                    if (index < editContextMenu.Items.Count)
                        editContextMenu.Items[index++].Text = isChinese ? "放置隧道" : "Place Tunnel";
                    if (index < editContextMenu.Items.Count)
                        editContextMenu.Items[index++].Text = isChinese ? "删除隧道" : "Delete Tunnel";
                }
                
                // 后续菜单项是根据配置动态生成的，需要根据具体情况翻译
                for (; index < editContextMenu.Items.Count; index++)
                {
                    if (editContextMenu.Items[index].Text == " ")
                        continue;
                        
                    if (editContextMenu.Items[index].Text == "Draw Low Bridge")
                        editContextMenu.Items[index].Text = isChinese ? "绘制低桥" : "Draw Low Bridge";
                    else if (editContextMenu.Items[index].Text == "Draw Bridge...")
                        editContextMenu.Items[index].Text = isChinese ? "绘制桥梁..." : "Draw Bridge...";
                    else if (editContextMenu.Items[index].Text == "Draw Connected Tiles")
                        editContextMenu.Items[index].Text = isChinese ? "绘制连接地形" : "Draw Connected Tiles";
                    else if (editContextMenu.Items[index].Text == "Repeat Last Connected Tile")
                        editContextMenu.Items[index].Text = isChinese ? "重复上次连接地形" : "Repeat Last Connected Tile";
                    else if (editContextMenu.Items[index].Text == "Draw Connected Tiles...")
                        editContextMenu.Items[index].Text = isChinese ? "绘制连接地形..." : "Draw Connected Tiles...";
                    else if (editContextMenu.Items[index].Text == "Toggle IceGrowth")
                        editContextMenu.Items[index].Text = isChinese ? "切换冰面生长" : "Toggle IceGrowth";
                    else if (editContextMenu.Items[index].Text == "Clear IceGrowth")
                        editContextMenu.Items[index].Text = isChinese ? "清除冰面生长" : "Clear IceGrowth";
                    else if (editContextMenu.Items[index].Text == "Manage Base Nodes")
                        editContextMenu.Items[index].Text = isChinese ? "管理基地节点" : "Manage Base Nodes";
                    else if (editContextMenu.Items[index].Text == "Place Veinhole Monster")
                        editContextMenu.Items[index].Text = isChinese ? "放置矿脉怪物" : "Place Veinhole Monster";
                }
            }
            
            // 更新View菜单项
            if (viewContextMenu != null && viewContextMenu.Items.Count > 0)
            {
                int index = 0;
                viewContextMenu.Items[index++].Text = isChinese ? "配置渲染对象..." : "Configure Rendered Objects...";
                viewContextMenu.Items[index++].Text = " "; // 分隔符
                viewContextMenu.Items[index++].Text = isChinese ? "切换不可通行单元格显示" : "Toggle Impassable Cells";
                viewContextMenu.Items[index++].Text = isChinese ? "切换冰面生长预览" : "Toggle IceGrowth Preview";
                viewContextMenu.Items[index++].Text = " "; // 分隔符
                viewContextMenu.Items[index++].Text = isChinese ? "查看小地图" : "View Minimap";
                viewContextMenu.Items[index++].Text = " "; // 分隔符
                viewContextMenu.Items[index++].Text = isChinese ? "查找航点..." : "Find Waypoint...";
                viewContextMenu.Items[index++].Text = isChinese ? "地图中心" : "Center of Map";
                viewContextMenu.Items[index++].Text = " "; // 分隔符
                viewContextMenu.Items[index++].Text = isChinese ? "无光照" : "No Lighting";
                viewContextMenu.Items[index++].Text = isChinese ? "正常光照" : "Normal Lighting";
                
                if (index < viewContextMenu.Items.Count)
                {
                    if (viewContextMenu.Items[index].Text == "Lightning Storm Lighting")
                        viewContextMenu.Items[index++].Text = isChinese ? "闪电风暴光照" : "Lightning Storm Lighting";
                    else if (viewContextMenu.Items[index].Text == "Ion Storm Lighting")
                        viewContextMenu.Items[index++].Text = isChinese ? "离子风暴光照" : "Ion Storm Lighting";
                }
                
                if (index < viewContextMenu.Items.Count && viewContextMenu.Items[index].Text == "Dominator Lighting")
                    viewContextMenu.Items[index++].Text = isChinese ? "统治者光照" : "Dominator Lighting";
                
                if (index < viewContextMenu.Items.Count && viewContextMenu.Items[index].Text == " ")
                    viewContextMenu.Items[index++].Text = " "; // 分隔符
                    
                if (index < viewContextMenu.Items.Count && viewContextMenu.Items[index].Text == "Toggle Fullscreen Mode")
                    viewContextMenu.Items[index++].Text = isChinese ? "切换全屏模式" : "Toggle Fullscreen Mode";
            }
            
            // 更新Tools菜单项
            if (toolsContextMenu != null && toolsContextMenu.Items.Count > 0)
            {
                int index = 0;
                
                // "Apply Impassable Overlay..."条件项
                if (index < toolsContextMenu.Items.Count && toolsContextMenu.Items[index].Text.Contains("Apply Impassable Overlay"))
                    toolsContextMenu.Items[index++].Text = isChinese ? "应用不可通行覆层..." : "Apply Impassable Overlay...";
                
                // 正常顺序的菜单项，按初始化顺序排列
                if (index < toolsContextMenu.Items.Count)
                    toolsContextMenu.Items[index++].Text = isChinese ? "地形生成器选项..." : "Terrain Generator Options...";
                if (index < toolsContextMenu.Items.Count)
                    toolsContextMenu.Items[index++].Text = isChinese ? "生成地形" : "Generate Terrain";
                if (index < toolsContextMenu.Items.Count && toolsContextMenu.Items[index].Text == " ")
                    toolsContextMenu.Items[index++].Text = " "; // 分隔符
                if (index < toolsContextMenu.Items.Count)
                    toolsContextMenu.Items[index++].Text = isChinese ? "应用INI代码..." : "Apply INI Code...";
                if (index < toolsContextMenu.Items.Count)
                    toolsContextMenu.Items[index++].Text = isChinese ? "运行脚本..." : "Run Script...";
                if (index < toolsContextMenu.Items.Count && toolsContextMenu.Items[index].Text == " ")
                    toolsContextMenu.Items[index++].Text = " "; // 分隔符
                if (index < toolsContextMenu.Items.Count)
                    toolsContextMenu.Items[index++].Text = isChinese ? "删除选项..." : "Deletion Options...";
                if (index < toolsContextMenu.Items.Count && toolsContextMenu.Items[index].Text == " ")
                    toolsContextMenu.Items[index++].Text = " "; // 分隔符
                if (index < toolsContextMenu.Items.Count)
                    toolsContextMenu.Items[index++].Text = isChinese ? "更改地图高度..." : "Change Map Height...";
                if (index < toolsContextMenu.Items.Count && toolsContextMenu.Items[index].Text == " ")
                    toolsContextMenu.Items[index++].Text = " "; // 分隔符
                if (index < toolsContextMenu.Items.Count)
                    toolsContextMenu.Items[index++].Text = isChinese ? "平滑冰面" : "Smoothen Ice";
                if (index < toolsContextMenu.Items.Count && toolsContextMenu.Items[index].Text == " ")
                    toolsContextMenu.Items[index++].Text = " "; // 分隔符
                if (index < toolsContextMenu.Items.Count)
                    toolsContextMenu.Items[index++].Text = isChinese ? "检查距离..." : "Check Distance...";
                if (index < toolsContextMenu.Items.Count)
                    toolsContextMenu.Items[index++].Text = isChinese ? "检查寻路距离..." : "Check Distance (Pathfinding)...";
                if (index < toolsContextMenu.Items.Count && toolsContextMenu.Items[index].Text == " ")
                    toolsContextMenu.Items[index++].Text = " "; // 分隔符
                if (index < toolsContextMenu.Items.Count)
                    toolsContextMenu.Items[index++].Text = isChinese ? "计算矿石价值..." : "Calculate Credits...";
                if (index < toolsContextMenu.Items.Count && toolsContextMenu.Items[index].Text == " ")
                    toolsContextMenu.Items[index++].Text = " "; // 分隔符
                if (index < toolsContextMenu.Items.Count)
                    toolsContextMenu.Items[index++].Text = isChinese ? "加载全地图覆层..." : "Load Map-Wide Overlay...";
                if (index < toolsContextMenu.Items.Count && toolsContextMenu.Items[index].Text == " ")
                    toolsContextMenu.Items[index++].Text = " "; // 分隔符
                if (index < toolsContextMenu.Items.Count)
                    toolsContextMenu.Items[index++].Text = isChinese ? "配置热键..." : "Configure Hotkeys...";
                if (index < toolsContextMenu.Items.Count && toolsContextMenu.Items[index].Text == " ")
                    toolsContextMenu.Items[index++].Text = " "; // 分隔符
                if (index < toolsContextMenu.Items.Count)
                    toolsContextMenu.Items[index++].Text = isChinese ? "关于" : "About";
            }
            
            // 更新Scripting菜单项
            if (scriptingContextMenu != null && scriptingContextMenu.Items.Count > 0)
            {
                int index = 0;
                // 按照添加菜单项的顺序进行翻译
                scriptingContextMenu.Items[index++].Text = isChinese ? "阵营" : "Houses";
                scriptingContextMenu.Items[index++].Text = isChinese ? "触发" : "Triggers";
                scriptingContextMenu.Items[index++].Text = isChinese ? "特遣队" : "TaskForces";
                scriptingContextMenu.Items[index++].Text = isChinese ? "脚本" : "Scripts";
                scriptingContextMenu.Items[index++].Text = isChinese ? "团队类型" : "TeamTypes";
                scriptingContextMenu.Items[index++].Text = isChinese ? "本地变量" : "Local Variables";
                
                // AITriggers可能是最后一项
                if (index < scriptingContextMenu.Items.Count)
                    scriptingContextMenu.Items[index++].Text = isChinese ? "AI触发器" : "AITriggers";
            }
            
            // 根据文本长度调整按钮位置
            UpdateMenuButtonPositions();
        }
        
        private void UpdateMenuButtonPositions()
        {
            // 更新菜单按钮位置，考虑文本长度变化
            fileButton.X = 0;
            editButton.X = fileButton.Right;
            viewButton.X = editButton.Right;
            toolsButton.X = viewButton.Right;
            scriptingButton.X = toolsButton.Right;
        }
    }
}
