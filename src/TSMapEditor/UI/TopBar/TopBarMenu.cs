using System;
using System.IO;
using System.Linq;
using Rampastring.XNAUI;
using TSMapEditor.Extensions;
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

        private MenuButton[] menuButtons;

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

            var fileContextMenu = new EditorContextMenu(WindowManager);
            fileContextMenu.Name = nameof(fileContextMenu);
            fileContextMenu.AddItem("New".L10N(), () => windowController.CreateNewMapWindow.Open(), null, null, null);
            fileContextMenu.AddItem("Open".L10N(), () => Open(), null, null, null);

            fileContextMenu.AddItem("Save".L10N(), () => SaveMap());
            fileContextMenu.AddItem("Save As".L10N(), () => SaveAs(), null, null, null);
            fileContextMenu.AddItem(" ", null, () => false, null, null);
            fileContextMenu.AddItem("Reload Input File".L10N(),
                () => InputFileReloadRequested?.Invoke(this, EventArgs.Empty),
                () => !string.IsNullOrWhiteSpace(map.LoadedINI.FileName),
                null, null);
            fileContextMenu.AddItem(" ", null, () => false, null, null);
            fileContextMenu.AddItem("Extract Megamap...".L10N(), () => windowController.MegamapGenerationOptionsWindow.Open(false));
            fileContextMenu.AddItem("Generate Map Preview...".L10N(), WriteMapPreviewConfirmation);
            fileContextMenu.AddItem(" ", null, () => false, null, null, null);
            fileContextMenu.AddItem("Open With Text Editor".L10N(), OpenWithTextEditor, () => !string.IsNullOrWhiteSpace(map.LoadedINI.FileName));
            fileContextMenu.AddItem(" ", null, () => false, null, null);
            fileContextMenu.AddItem("Exit".L10N(), WindowManager.CloseGame);

            var fileButton = new MenuButton(WindowManager, fileContextMenu);
            fileButton.Name = nameof(fileButton);
            fileButton.Text = "File".L10N();
            AddChild(fileButton);

            var editContextMenu = new EditorContextMenu(WindowManager);
            editContextMenu.Name = nameof(editContextMenu);
            editContextMenu.AddItem("Configure Copied Objects...".L10N(), () => windowController.CopiedEntryTypesWindow.Open(), null, null, null, () => KeyboardCommands.Instance.ConfigureCopiedObjects.GetKeyDisplayString());
            editContextMenu.AddItem("Copy".L10N(), () => KeyboardCommands.Instance.Copy.DoTrigger(), null, null, null, () => KeyboardCommands.Instance.Copy.GetKeyDisplayString());
            editContextMenu.AddItem("Copy Custom Shape".L10N(), () => KeyboardCommands.Instance.CopyCustomShape.DoTrigger(), null, null, null, () => KeyboardCommands.Instance.CopyCustomShape.GetKeyDisplayString());
            editContextMenu.AddItem("Paste".L10N(), () => KeyboardCommands.Instance.Paste.DoTrigger(), null, null, null, () => KeyboardCommands.Instance.Paste.GetKeyDisplayString());
            editContextMenu.AddItem(" ", null, () => false, null, null);
            editContextMenu.AddItem("Undo".L10N(), () => mutationManager.Undo(), () => mutationManager.CanUndo(), null, null, () => KeyboardCommands.Instance.Undo.GetKeyDisplayString());
            editContextMenu.AddItem("Redo".L10N(), () => mutationManager.Redo(), () => mutationManager.CanRedo(), null, null, () => KeyboardCommands.Instance.Redo.GetKeyDisplayString());
            editContextMenu.AddItem("Action History".L10N(), () => windowController.HistoryWindow.Open());
            editContextMenu.AddItem(" ", null, () => false, null, null);
            editContextMenu.AddItem("Basic".L10N(), () => windowController.BasicSectionConfigWindow.Open(), null, null, null);
            editContextMenu.AddItem("Map Size".L10N(), () => windowController.MapSizeWindow.Open(), null, null, null, null);
            editContextMenu.AddItem(" ", null, () => false, null, null);
            editContextMenu.AddItem("Lighting".L10N(), () => windowController.LightingSettingsWindow.Open(), null, null, null);
            editContextMenu.AddItem(" ", null, () => false, null, null);
            editContextMenu.AddItem("Place Tunnel".L10N(), () => mapUI.EditorState.CursorAction = placeTubeCursorAction, null, null, null, () => KeyboardCommands.Instance.PlaceTunnel.GetKeyDisplayString());
            editContextMenu.AddItem("Delete Tunnel".L10N(), () => mapUI.EditorState.CursorAction = deleteTunnelCursorAction, null, null, null);
            editContextMenu.AddItem(" ", null, () => false, null, null);

            int bridgeCount = map.EditorConfig.Bridges.Count;
            if (bridgeCount > 0)
            {
                var bridges = map.EditorConfig.Bridges;
                if (bridgeCount == 1 && bridges[0].Kind == BridgeKind.Low)
                {
                    editContextMenu.AddItem("Draw Low Bridge".L10N(), () => mapUI.EditorState.CursorAction =
                        new PlaceBridgeCursorAction(mapUI, bridges[0]), null, null, null);
                }
                else
                {
                    editContextMenu.AddItem("Draw Bridge...".L10N(), SelectBridge, null, null, null);
                }
            }

            var theaterMatchingCliffs = map.EditorConfig.Cliffs.Where(cliff => cliff.AllowedTheaters.Exists(
                theaterName => theaterName.Equals(map.TheaterName, StringComparison.OrdinalIgnoreCase))).ToList();
            int cliffCount = theaterMatchingCliffs.Count;
            if (cliffCount > 0)
            {
                if (cliffCount == 1)
                {
                    editContextMenu.AddItem("Draw Connected Tiles".L10N(), () => mapUI.EditorState.CursorAction =
                        new DrawCliffCursorAction(mapUI, theaterMatchingCliffs[0]), null, null, null);
                }
                else
                {
                    editContextMenu.AddItem("Repeat Last Connected Tile".L10N(), RepeatLastConnectedTile, null, null, null, () => KeyboardCommands.Instance.RepeatConnectedTile.GetKeyDisplayString());
                    editContextMenu.AddItem("Draw Connected Tiles...".L10N(), () => windowController.SelectConnectedTileWindow.Open(), null, null, null, () => KeyboardCommands.Instance.PlaceConnectedTile.GetKeyDisplayString());
                }
            }

            editContextMenu.AddItem("Toggle IceGrowth".L10N(), () => { mapUI.EditorState.CursorAction = toggleIceGrowthCursorAction; toggleIceGrowthCursorAction.ToggleIceGrowth = true; mapUI.EditorState.HighlightIceGrowth = true; }, null, null, null);
            editContextMenu.AddItem("Clear IceGrowth".L10N(), () => { mapUI.EditorState.CursorAction = toggleIceGrowthCursorAction; toggleIceGrowthCursorAction.ToggleIceGrowth = false; mapUI.EditorState.HighlightIceGrowth = true; }, null, null, null);
            editContextMenu.AddItem(" ", null, () => false, null, null);
            editContextMenu.AddItem("Manage Base Nodes".L10N(), ManageBaseNodes_Selected, null, null, null);

            if (map.Rules.OverlayTypes.Exists(ot => ot.ININame == Constants.VeinholeMonsterTypeName) && map.Rules.OverlayTypes.Exists(ot => ot.ININame == Constants.VeinholeDummyTypeName))
            {
                editContextMenu.AddItem(" ", null, () => false, null, null);
                editContextMenu.AddItem("Place Veinhole Monster".L10N(), () => mapUI.EditorState.CursorAction = placeVeinholeMonsterCursorAction, null, null, null, null);
            }

            var editButton = new MenuButton(WindowManager, editContextMenu);
            editButton.Name = nameof(editButton);
            editButton.X = fileButton.Right;
            editButton.Text = "Edit".L10N();
            AddChild(editButton);

            var viewContextMenu = new EditorContextMenu(WindowManager);
            viewContextMenu.Name = nameof(viewContextMenu);
            viewContextMenu.AddItem("Configure Rendered Objects...".L10N(), () => windowController.RenderedObjectsConfigurationWindow.Open());
            viewContextMenu.AddItem(" ", null, () => false, null, null);
            viewContextMenu.AddItem("Toggle Impassable Cells".L10N(), () => mapUI.EditorState.HighlightImpassableCells = !mapUI.EditorState.HighlightImpassableCells, null, null, null);
            viewContextMenu.AddItem("Toggle IceGrowth Preview".L10N(), () => mapUI.EditorState.HighlightIceGrowth = !mapUI.EditorState.HighlightIceGrowth, null, null, null);
            viewContextMenu.AddItem(" ", null, () => false, null, null);
            viewContextMenu.AddItem("View Minimap".L10N(), () => windowController.MinimapWindow.Open());
            viewContextMenu.AddItem(" ", null, () => false, null, null);
            viewContextMenu.AddItem("Find Waypoint...".L10N(), () => windowController.FindWaypointWindow.Open());
            viewContextMenu.AddItem("Center of Map".L10N(), () => mapUI.Camera.CenterOnMapCenterCell());
            viewContextMenu.AddItem(" ", null, () => false, null, null);
            viewContextMenu.AddItem("No Lighting".L10N(), () => mapUI.EditorState.LightingPreviewState = LightingPreviewMode.NoLighting);
            viewContextMenu.AddItem("Normal Lighting".L10N(), () => mapUI.EditorState.LightingPreviewState = LightingPreviewMode.Normal);
            if (Constants.IsRA2YR)
            {
                viewContextMenu.AddItem("Lightning Storm Lighting".L10N(), () => mapUI.EditorState.LightingPreviewState = LightingPreviewMode.IonStorm);
                viewContextMenu.AddItem("Dominator Lighting".L10N(), () => mapUI.EditorState.LightingPreviewState = LightingPreviewMode.Dominator);
            }
            else
            {
                viewContextMenu.AddItem("Ion Storm Lighting".L10N(), () => mapUI.EditorState.LightingPreviewState = LightingPreviewMode.IonStorm);
            }
            viewContextMenu.AddItem(" ", null, () => false, null, null);
            viewContextMenu.AddItem("Toggle Light From Disabled Buildings".L10N(), () => mapUI.EditorState.LightDisabledLightSources = !mapUI.EditorState.LightDisabledLightSources);
            viewContextMenu.AddItem(" ", null, () => false, null, null);
            viewContextMenu.AddItem("Toggle Fullscreen Mode".L10N(), () => KeyboardCommands.Instance.ToggleFullscreen.DoTrigger());

            var viewButton = new MenuButton(WindowManager, viewContextMenu);
            viewButton.Name = nameof(viewButton);
            viewButton.X = editButton.Right;
            viewButton.Text = "View".L10N();
            AddChild(viewButton);

            var toolsContextMenu = new EditorContextMenu(WindowManager);
            toolsContextMenu.Name = nameof(toolsContextMenu);
            // toolsContextMenu.AddItem("Options");
            if (windowController.AutoApplyImpassableOverlayWindow.IsAvailable)
                toolsContextMenu.AddItem("Apply Impassable Overlay...".L10N(), () => windowController.AutoApplyImpassableOverlayWindow.Open(), null, null, null);

            toolsContextMenu.AddItem("Terrain Generator Options...".L10N(), () => windowController.TerrainGeneratorConfigWindow.Open(), null, null, null, () => KeyboardCommands.Instance.ConfigureTerrainGenerator.GetKeyDisplayString());
            toolsContextMenu.AddItem("Generate Terrain".L10N(), () => EnterTerrainGenerator(), null, null, null, () => KeyboardCommands.Instance.GenerateTerrain.GetKeyDisplayString());
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("Apply INI Code...".L10N(), () => windowController.ApplyINICodeWindow.Open(), null, null, null);
            toolsContextMenu.AddItem("Run Script...".L10N(), () => windowController.RunScriptWindow.Open(), null, null, null, null);
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("Deletion Options...".L10N(), () => windowController.DeletionModeConfigurationWindow.Open());
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("Change Map Height...".L10N(), () => windowController.ChangeHeightWindow.Open(), null, () => !Constants.IsFlatWorld, null, null);
            toolsContextMenu.AddItem(" ", null, () => false, () => !Constants.IsFlatWorld, null);
            toolsContextMenu.AddItem("Smoothen Ice".L10N(), SmoothenIce, null, null, null, null);
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("Check Distance...".L10N(), () => mapUI.EditorState.CursorAction = checkDistanceCursorAction, null, null, null, () => KeyboardCommands.Instance.CheckDistance.GetKeyDisplayString());
            toolsContextMenu.AddItem("Check Distance (Pathfinding)...".L10N(), () => mapUI.EditorState.CursorAction = checkDistancePathfindingCursorAction, null, null, null, () => KeyboardCommands.Instance.CheckDistancePathfinding.GetKeyDisplayString());
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("Calculate Credits...".L10N(), () => mapUI.EditorState.CursorAction = calculateTiberiumValueCursorAction, null, null, null, () => KeyboardCommands.Instance.CalculateCredits.GetKeyDisplayString());
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("Load Map-Wide Overlay...".L10N(), () => MapWideOverlayLoadRequested?.Invoke(this, EventArgs.Empty), null, null, null, null);
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("Configure Hotkeys...".L10N(), () => windowController.HotkeyConfigurationWindow.Open(), null, null, null);
            toolsContextMenu.AddItem(" ", null, () => false, null, null);
            toolsContextMenu.AddItem("About".L10N(), () => windowController.AboutWindow.Open(), null, null, null, null);

            var toolsButton = new MenuButton(WindowManager, toolsContextMenu);
            toolsButton.Name = nameof(toolsButton);
            toolsButton.X = viewButton.Right;
            toolsButton.Text = "Tools".L10N();
            AddChild(toolsButton);

            var scriptingContextMenu = new EditorContextMenu(WindowManager);
            scriptingContextMenu.Name = nameof(scriptingContextMenu);
            scriptingContextMenu.AddItem("Houses".L10N(), () => windowController.HousesWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("Triggers".L10N(), () => windowController.TriggersWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("TaskForces".L10N(), () => windowController.TaskForcesWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("Scripts".L10N(), () => windowController.ScriptsWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("TeamTypes".L10N(), () => windowController.TeamTypesWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("Local Variables".L10N(), () => windowController.LocalVariablesWindow.Open(), null, null, null);
            scriptingContextMenu.AddItem("AITriggers".L10N(), () => windowController.AITriggersWindow.Open(), null, null, null, null);

            var scriptingButton = new MenuButton(WindowManager, scriptingContextMenu);
            scriptingButton.Name = nameof(scriptingButton);
            scriptingButton.X = toolsButton.Right;
            scriptingButton.Text = "Scripting".L10N();
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
            var messageBox = EditorMessageBox.Show(WindowManager, "Confirmation",
                "This will write the current minimap as the map preview to the map file." + Environment.NewLine + Environment.NewLine +
                "This provides the map with a preview if it is used as a custom map" + Environment.NewLine + 
                "in the CnCNet Client or in-game, but is not necessary if the map will" + Environment.NewLine +
                "have an external preview. It will also significantly increase the size" + Environment.NewLine +
                "of the map file." + Environment.NewLine + Environment.NewLine +
                "Do you want to continue?" + Environment.NewLine + Environment.NewLine +
                "Note: The preview won't be actually written to the map before" + Environment.NewLine + 
                "you save the map.", Windows.MessageBoxButtons.YesNo);

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
                EditorMessageBox.Show(WindowManager, "Houses Required",
                    "The map has no houses set up. Houses need to be configured before base nodes can be added." + Environment.NewLine + Environment.NewLine +
                    "You can configure Houses from Scripting -> Houses.", TSMapEditor.UI.Windows.MessageBoxButtons.OK);

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
    }
}
