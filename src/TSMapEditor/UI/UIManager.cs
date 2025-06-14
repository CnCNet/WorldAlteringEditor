using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Linq;
using TSMapEditor.Misc;
using TSMapEditor.Models;
using TSMapEditor.Mutations;
using TSMapEditor.Rendering;
using TSMapEditor.Settings;
using TSMapEditor.UI.Controls;
using TSMapEditor.UI.CursorActions;
using TSMapEditor.UI.Notifications;
using TSMapEditor.UI.Sidebar;
using TSMapEditor.UI.TopBar;
using TSMapEditor.UI.Windows;
using TSMapEditor.UI.Windows.MainMenuWindows;

namespace TSMapEditor.UI
{
    public class CustomUISettings : UISettings
    {
        public CustomUISettings()
        {
            CheckBoxCheckedTexture = AssetLoader.LoadTexture("checkBoxChecked.png");
            CheckBoxClearTexture = AssetLoader.LoadTexture("checkBoxClear.png");
            CheckBoxDisabledCheckedTexture = AssetLoader.LoadTexture("checkBoxCheckedD.png");
            CheckBoxDisabledClearTexture = AssetLoader.LoadTexture("checkBoxClearD.png");
            PanelBackgroundColor = new Color(0, 0, 0, 128);
            PanelBorderColor = new Color(128, 128, 128, 255);
        }

        public Color ListBoxBackgroundColor { get; set; } = Color.Black;
        public Color ButtonMainBackgroundColor { get; set; } = new Color(0, 0, 0, 196);
        public Color ButtonSecondaryBackgroundColor { get; set; } = new Color(0, 0, 0, 255);
        public Color ButtonTertiaryBackgroundColor { get; set; } = Color.White;
    }

    class UIManager : XNAControl, IWindowParentControl
    {
        public UIManager(WindowManager windowManager, Map map, TheaterGraphics theaterGraphics,
            EditorGraphics editorGraphics) : base(windowManager)
        {
            DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
            this.map = map;
            this.theaterGraphics = theaterGraphics;
            this.editorGraphics = editorGraphics;
        }

        public event EventHandler RenderResolutionChanged;

        private static bool InitialDisplayModeSet = false;

        private Map map;
        private TheaterGraphics theaterGraphics;
        private EditorGraphics editorGraphics;

        private MapUI mapUI;
        private TileSelector tileSelector;
        private OverlayFrameSelector overlayFrameSelector;
        private EditorSidebar editorSidebar;
        private TopBarMenu topBarMenu;

        private EditorState editorState;
        private TileInfoDisplay tileInfoDisplay;

        private PlaceTerrainCursorAction placeTerrainCursorAction;
        private ChangeTechnoOwnerAction changeTechnoOwnerAction;
        private PlaceWaypointCursorAction placeWaypointCursorAction;
        private OverlayPlacementAction overlayPlacementAction;
        private CopyRectangularTerrainCursorAction copyRectangularTerrainCursorAction;
        private CopyCustomShapedTerrainCursorAction copyCustomShapedTerrainCursorAction;
        private PasteTerrainCursorAction pasteTerrainCursorAction;

        private WindowController windowController;

        private MutationManager mutationManager;

        private NotificationManager notificationManager;

        private int loadMapStage;
        private string loadMapFilePath;
        private CreateNewMapEventArgs newMapInfo;
        private DarkeningPanel mapLoadDarkeningPanel;

        private AutosaveTimer autosaveTimer;
        private MapFileWatcher mapFileWatcher;

        public void SetAutoUpdateChildOrder(bool value) => AutoUpdateChildOrder = value;

        public INotificationManager NotificationManager => notificationManager;

        public override void Initialize()
        {
            Name = nameof(UIManager);

            // We should be the first control to subscribe to this event
            WindowManager.WindowSizeChangedByUser += WindowManager_WindowSizeChangedByUser;

            SetInitialDisplayMode();

            InitTheme();

            // Keyboard must be initialized before any other controls so it's properly usable
            InitKeyboard();

            windowController = new WindowController();
            editorState = new EditorState();
            editorState.BrushSize = map.EditorConfig.BrushSizes[0];
            mutationManager = new MutationManager();

            InitMapUI();

            placeTerrainCursorAction = new PlaceTerrainCursorAction(mapUI);
            placeWaypointCursorAction = new PlaceWaypointCursorAction(mapUI);
            changeTechnoOwnerAction = new ChangeTechnoOwnerAction(mapUI);
            editorState.ObjectOwnerChanged += (s, e) => editorState.CursorAction = changeTechnoOwnerAction;

            overlayPlacementAction = new OverlayPlacementAction(mapUI);

            editorSidebar = new EditorSidebar(WindowManager, editorState, map, theaterGraphics, mapUI, overlayPlacementAction);
            editorSidebar.Width = UserSettings.Instance.SidebarWidth.GetValue();
            editorSidebar.Y = Constants.UITopBarMenuHeight;
            editorSidebar.Height = WindowManager.RenderResolutionY - editorSidebar.Y;
            AddChild(editorSidebar);

            tileSelector = new TileSelector(WindowManager, map, theaterGraphics, placeTerrainCursorAction, editorState);
            tileSelector.X = editorSidebar.Right;
            tileSelector.Width = WindowManager.RenderResolutionX - tileSelector.X;
            tileSelector.Height = 300;
            tileSelector.Y = WindowManager.RenderResolutionY - tileSelector.Height;
            AddChild(tileSelector);
            tileSelector.TileDisplay.SelectedTileChanged += TileDisplay_SelectedTileChanged;
            tileSelector.ClientRectangleUpdated += UpdateTileAndOverlaySelectorArea;

            overlayFrameSelector = new OverlayFrameSelector(WindowManager, theaterGraphics, editorState);
            overlayFrameSelector.X = editorSidebar.Right;
            overlayFrameSelector.Width = tileSelector.Width;
            overlayFrameSelector.Height = tileSelector.Height;
            overlayFrameSelector.Y = tileSelector.Y;
            AddChild(overlayFrameSelector);
            overlayFrameSelector.SelectedFrameChanged += OverlayFrameSelector_SelectedFrameChanged;
            overlayFrameSelector.ClientRectangleUpdated += UpdateTileAndOverlaySelectorArea;
            overlayFrameSelector.Disable();

            tileInfoDisplay = new TileInfoDisplay(WindowManager, map, theaterGraphics, editorState);
            AddChild(tileInfoDisplay);
            tileInfoDisplay.X = Width - tileInfoDisplay.Width;
            mapUI.TileInfoDisplay = tileInfoDisplay;

            InitNotificationManager();
            windowController.Initialize(this, map, editorState, mapUI);

            topBarMenu = new TopBarMenu(WindowManager, mutationManager, mapUI, map, windowController);
            topBarMenu.Width = editorSidebar.Width;
            topBarMenu.OnFileSelected += OpenMapWindow_OnFileSelected;
            topBarMenu.MapWideOverlayLoadRequested += TopBarMenu_MapWideOverlayLoadRequested;
            AddChild(topBarMenu);

            var editorControlsPanel = new EditorControlsPanel(WindowManager, map, theaterGraphics,
                map.EditorConfig, editorState, windowController, placeTerrainCursorAction, placeWaypointCursorAction, mapUI);
            editorControlsPanel.X = topBarMenu.Right;
            AddChild(editorControlsPanel);

            AddChild(notificationManager);

            base.Initialize();

            placeWaypointCursorAction.PlaceWaypointWindow = windowController.PlaceWaypointWindow;

            editorState.CursorActionChanged += EditorState_CursorActionChanged;
            overlayPlacementAction.OverlayTypeChanged += OverlayPlacementAction_OverlayTypeChanged;

            copyRectangularTerrainCursorAction = new CopyRectangularTerrainCursorAction(mapUI);
            copyCustomShapedTerrainCursorAction = new CopyCustomShapedTerrainCursorAction(mapUI);
            pasteTerrainCursorAction = new PasteTerrainCursorAction(mapUI, Keyboard);

            InitAutoSaveAndSaveNotifications();

            mapFileWatcher = new MapFileWatcher(map);

            windowController.OpenMapWindow.OnFileSelected += OpenMapWindow_OnFileSelected;
            windowController.CreateNewMapWindow.OnCreateNewMap += CreateNewMapWindow_OnCreateNewMap;
            topBarMenu.InputFileReloadRequested += TopBarMenu_InputFileReloadRequested;

            // Try to select "Neutral" as default house
            editorState.ObjectOwner = map.GetHouses().Find(h => h.ININame == "Neutral");
            if (editorState.ObjectOwner == null && map.GetHouses().Count > 0)
                editorState.ObjectOwner = map.GetHouses()[0];
            editorState.CursorAction = null;

            Alpha = 0f;

            RefreshWindowTitle();

            // This makes the exit process technically faster, but the editor stays longer on the
            // screen so practically increases exit time from the user's perspective
            // WindowManager.GameClosing += (s, e) => ClearResources();
            WindowManager.SetMaximizeBox(true);
            WindowManager.GameClosing += WindowManager_GameClosing;
            KeyboardCommands.Instance.ToggleFullscreen.Triggered += ToggleFullscreen_Triggered;
            
            // 在初始化完成后立即刷新所有界面元素的语言
            RefreshAllLanguage();
        }

        private void SetInitialDisplayMode()
        {
            if (InitialDisplayModeSet)
                return;

            InitialDisplayModeSet = true;

            Game.Window.AllowUserResizing = true;
            var form = (System.Windows.Forms.Form)System.Windows.Forms.Form.FromHandle(Game.Window.Handle);
            form.MaximizeBox = false;

            var screen = System.Windows.Forms.Screen.FromHandle(Game.Window.Handle);
            int width = screen.Bounds.Width - 300;
            int height = screen.Bounds.Height - 200;
            bool borderless = UserSettings.Instance.FullscreenWindowed;
            if (borderless)
            {
                width = screen.Bounds.Width;
                height = screen.Bounds.Height;
            }

            WindowManager.InitGraphicsMode(width, height, borderless);
            RefreshRenderResolution();
            WindowManager.CenterOnScreen();
            WindowManager.SetBorderlessMode(borderless);
        }

        private void ToggleFullscreen_Triggered(object sender, EventArgs e)
        {
            if (Game.Window.IsBorderless)
            {
                const int MarginX = 100;
                const int MarginY = 200;
                WindowManager.SetBorderlessMode(false);
                Game.Window.AllowUserResizing = true;
                WindowManager.InitGraphicsMode(WindowManager.WindowWidth - MarginX, WindowManager.WindowHeight - MarginY, false);
                WindowManager_WindowSizeChangedByUser(this, EventArgs.Empty);
                WindowManager.CenterOnScreen();
            }
            else
            {
#if WINDOWS
                // Hack to prevent Windows or MG from weirdly assigning our window off-screen
                // upon switching to fullscreen mode if our window happens to be maximized
                System.Windows.Forms.Form form = (System.Windows.Forms.Form)System.Windows.Forms.Control.FromHandle(Game.Window.Handle);
                form.WindowState = System.Windows.Forms.FormWindowState.Normal;

                WindowManager.SetBorderlessMode(true);
                Game.Window.AllowUserResizing = false;
                var screen = System.Windows.Forms.Screen.FromHandle(Game.Window.Handle);
                WindowManager.InitGraphicsMode(screen.Bounds.Width, screen.Bounds.Height, true);
                WindowManager_WindowSizeChangedByUser(this, EventArgs.Empty);
                WindowManager.CenterOnScreen();
#endif
            }
        }

        private void RefreshRenderResolution()
        {
            if (Game.Window.ClientBounds.Width == 0 || Game.Window.ClientBounds.Height == 0)
                return;

            int newRenderWidth = (int)(Game.Window.ClientBounds.Width / UserSettings.Instance.RenderScale);
            int newRenderHeight = (int)(Game.Window.ClientBounds.Height / UserSettings.Instance.RenderScale);

            if (newRenderWidth != WindowManager.RenderResolutionX || newRenderHeight != WindowManager.RenderResolutionY)
            {
                WindowManager.SetRenderResolution(newRenderWidth, newRenderHeight);
                RenderResolutionChanged?.Invoke(this, EventArgs.Empty);
                Width = WindowManager.RenderResolutionX;
                Height = WindowManager.RenderResolutionY;

                Parser.Instance.RefreshResolutionConstants(WindowManager);
                SetNotificationManagerSizeAndPosition();
            }
        }

        private void WindowManager_WindowSizeChangedByUser(object sender, EventArgs e)
        {
            RefreshRenderResolution();
        }

        private void WindowManager_GameClosing(object sender, EventArgs e) => mapFileWatcher.StopWatching();

        private void InitTheme()
        {
            
            
            bool boldFont = UserSettings.Instance.UseBoldFont;
            if (boldFont)
            {
                Renderer.GetFontList()[0] = Renderer.GetFontList()[1];
            }

            UISettings.ActiveSettings = EditorThemes.Themes[UserSettings.Instance.Theme];

            Width = WindowManager.RenderResolutionX;
            Height = WindowManager.RenderResolutionY;
        }

        private void InitKeyboard()
        {
            Keyboard.OnKeyDown += Keyboard_OnKeyDown;

            KeyboardCommands.Instance = new KeyboardCommands();
            KeyboardCommands.Instance.Undo.Triggered += UndoAction;
            KeyboardCommands.Instance.Redo.Triggered += RedoAction;
            KeyboardCommands.Instance.Copy.Triggered += CopyRectanglularAreaAction;
            KeyboardCommands.Instance.CopyCustomShape.Triggered += CopyCustomShapedAreaAction;
            KeyboardCommands.Instance.Paste.Triggered += PasteAction;

            KeyboardCommands.Instance.ReadFromSettings();
        }

        private void ClearKeyboard()
        {
            Keyboard.OnKeyDown -= Keyboard_OnKeyDown;
            KeyboardCommands.Instance.ClearCommandSubscriptions();
        }

        private void InitMapUI()
        {
            mapUI = new MapUI(WindowManager, map, theaterGraphics, editorGraphics, editorState, mutationManager, windowController);
            AddChild(mapUI);
        }

        private void InitNotificationManager()
        {
            notificationManager = new NotificationManager(WindowManager);
            SetNotificationManagerSizeAndPosition();
        }

        private void SetNotificationManagerSizeAndPosition()
        {
            if (notificationManager == null)
                return;

            notificationManager.X = editorSidebar.X + Constants.UIEmptySideSpace;
            notificationManager.Width = WindowManager.RenderResolutionX - (notificationManager.X * 2);
            notificationManager.Y = 100;
        }

        private void InitAutoSaveAndSaveNotifications()
        {
            autosaveTimer = new AutosaveTimer(map);

            map.MapManuallySaved += (s, e) =>
            {
                notificationManager.AddNotification("Map saved.");
                RefreshWindowTitle();
                CheckForIssuesAfterManualSave(s, e);
            };

            map.MapAutoSaved += (s, e) => notificationManager.AddNotification("Map auto-saved.");
            map.MapSaveFailed += (s, e) => notificationManager.AddNotification("Saving map failed! Please see the log file for details.");
        }

        private void RefreshWindowTitle()
        {
            string baseTitle = "C&C World-Altering Editor (WAE) - {0}";
            string mapPath;

            mapPath = string.IsNullOrWhiteSpace(map.LoadedINI.FileName) ? "New map" : map.LoadedINI.FileName;

            Game.Window.Title = string.Format(baseTitle, mapPath);
        }

        private void CheckForIssuesAfterManualSave(object sender, EventArgs e)
        {
            var issues = map.CheckForIssues();

            if (issues.Count > 0)
            {
                if (issues.Count > 10)
                    issues = issues.Take(10).ToList();

                var newline = Environment.NewLine;

                string issuesString = string.Join(newline + newline, issues);

                EditorMessageBox.Show(WindowManager, "Issues Found",
                    "The map has been saved, but one or more issues have been found in the map. Please consider resolving them." + newline + newline + issuesString,
                    MessageBoxButtons.OK);
            }
        }

        private void CreateNewMapWindow_OnCreateNewMap(object sender, CreateNewMapEventArgs e)
        {
            loadMapFilePath = null;
            newMapInfo = e;
            StartLoadingMap();
        }

        private void OpenMapWindow_OnFileSelected(object sender, FileSelectedEventArgs e)
        {
            loadMapFilePath = e.FilePath;
            StartLoadingMap();
        }

        private void TopBarMenu_MapWideOverlayLoadRequested(object sender, EventArgs e)
        {
            mapUI.MapWideOverlay.LoadMapWideOverlay(GraphicsDevice);
            editorState.MapWideOverlayExists = mapUI.MapWideOverlay.HasTexture;
            if (editorState.MapWideOverlayExists)
                editorState.DrawMapWideOverlay = true;
        }

        private void TopBarMenu_InputFileReloadRequested(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(map.LoadedINI.FileName))
                return;

            loadMapFilePath = map.LoadedINI.FileName;
            StartLoadingMap();
        }

        private void StartLoadingMap()
        {
            var messageBox = new EditorMessageBox(WindowManager, "Loading", "Please wait, loading map...", MessageBoxButtons.None);
            mapLoadDarkeningPanel = new DarkeningPanel(WindowManager);
            mapLoadDarkeningPanel.DrawOrder = int.MaxValue;
            mapLoadDarkeningPanel.UpdateOrder = int.MaxValue;
            AddChild(mapLoadDarkeningPanel);
            mapLoadDarkeningPanel.AddChild(messageBox);

            loadMapStage = 1;
        }

        private void LoadMap()
        {
            mapFileWatcher.StopWatching();

            bool createNew = loadMapFilePath == null;

            string error = MapSetup.InitializeMap(UserSettings.Instance.GameDirectory, createNew,
                loadMapFilePath,
                createNew ? newMapInfo : null,
                WindowManager);

            if (error != null)
            {
                EditorMessageBox.Show(WindowManager, "Failed to open map",
                    error, MessageBoxButtons.OK);
                loadMapStage = 0;
                RemoveChild(mapLoadDarkeningPanel);
                mapLoadDarkeningPanel.Kill();
                mapLoadDarkeningPanel = null;
                return;
            }

            if (!createNew)
            {
                UserSettings.Instance.LastScenarioPath.UserDefinedValue = loadMapFilePath;
                UserSettings.Instance.RecentFiles.PutEntry(loadMapFilePath);
                _ = UserSettings.Instance.SaveSettingsAsync();
            }

            ClearResources();
            WindowManager.RemoveControl(this);

            MapSetup.LoadTheaterGraphics(WindowManager, UserSettings.Instance.GameDirectory);
        }

        private void ClearResources()
        {
            // We need to free memory of everything that we've ever created

            map.Rules.TutorialLines.ShutdownFSW();
            windowController.OpenMapWindow.OnFileSelected -= OpenMapWindow_OnFileSelected;
            windowController.CreateNewMapWindow.OnCreateNewMap -= CreateNewMapWindow_OnCreateNewMap;
            topBarMenu.InputFileReloadRequested -= TopBarMenu_InputFileReloadRequested;

            editorState.CursorActionChanged -= EditorState_CursorActionChanged;
            overlayPlacementAction.OverlayTypeChanged -= OverlayPlacementAction_OverlayTypeChanged;

            WindowManager.GameClosing -= WindowManager_GameClosing;
            WindowManager.WindowSizeChangedByUser -= WindowManager_WindowSizeChangedByUser;
            KeyboardCommands.Instance.ToggleFullscreen.Triggered -= ToggleFullscreen_Triggered;

            Disable();

            Kill();

            ClearKeyboard();

            // TODO free up memory of textures created for controls - they should be mostly 
            // insignificant compared to the map textures though, so it shouldn't be too bad like this

            tileSelector = null;
            overlayFrameSelector = null;
            editorSidebar = null;

            map.Clear();
            map = null;

            windowController.Clear();
            windowController = null;

            theaterGraphics.DisposeAll();
            theaterGraphics = null;

            editorGraphics.DisposeAll();
            editorGraphics = null;

            mapUI.Clear();

            GC.Collect();
        }

        private void OverlayPlacementAction_OverlayTypeChanged(object sender, EventArgs e)
        {
            if (overlayPlacementAction.OverlayType == null)
            {
                ShowTileSelector();
                return;
            }

            ShowOverlayFrameSelector();
            overlayFrameSelector.SetOverlayType(overlayPlacementAction.OverlayType);
            return;
        }

        private void EditorState_CursorActionChanged(object sender, EventArgs e)
        {
            if (editorState.CursorAction == null)
            {
                ShowTileSelector();
                return;
            }

            var overlayPlacementAction = editorState.CursorAction as OverlayPlacementAction;
            if (overlayPlacementAction == null || overlayPlacementAction.OverlayType == null)
            {
                ShowTileSelector();
                return;
            }

            ShowOverlayFrameSelector();
        }

        private void ShowTileSelector()
        {
            tileSelector.Enable();
            overlayFrameSelector.Disable();
        }

        private void ShowOverlayFrameSelector()
        {
            tileSelector.Disable();
            overlayFrameSelector.Enable();
        }

        private void UpdateTileAndOverlaySelectorArea(object sender, EventArgs e)
        {
            tileSelector.ClientRectangleUpdated -= UpdateTileAndOverlaySelectorArea;
            overlayFrameSelector.ClientRectangleUpdated -= UpdateTileAndOverlaySelectorArea;

            if (sender == tileSelector)
                overlayFrameSelector.ClientRectangle = tileSelector.ClientRectangle;
            else
                tileSelector.ClientRectangle = overlayFrameSelector.ClientRectangle;

            tileSelector.ClientRectangleUpdated += UpdateTileAndOverlaySelectorArea;
            overlayFrameSelector.ClientRectangleUpdated += UpdateTileAndOverlaySelectorArea;
        }

        private void Keyboard_OnKeyDown(object sender, Rampastring.XNAUI.Input.KeyPressEventArgs e)
        {
            if (!WindowManager.HasFocus)
                return;

            if (!IsActive)
                return;

            var selectedControl = WindowManager.SelectedControl;
            if (selectedControl != null)
            {
                if (selectedControl is XNATextBox || selectedControl is XNAListBox)
                    return;
            }

            // First, check for commands that match when all modifiers are fully considered
            // - for example, if there's two commands, one that is activated by pressing A,
            // and another that is activated by pressing Alt + A, DON'T match on the first
            // one if Alt is down
            foreach (var keyboardCommand in KeyboardCommands.Instance.Commands)
            {
                if (keyboardCommand.ForActionsOnly)
                    continue;

                if (e.PressedKey == keyboardCommand.Key.Key)
                {
                    // Key matches, check modifiers

                    if (((keyboardCommand.Key.Modifiers & KeyboardModifiers.Alt) == KeyboardModifiers.Alt) != Keyboard.IsAltHeldDown())
                        continue;

                    if (((keyboardCommand.Key.Modifiers & KeyboardModifiers.Ctrl) == KeyboardModifiers.Ctrl) != Keyboard.IsCtrlHeldDown())
                        continue;

                    if (((keyboardCommand.Key.Modifiers & KeyboardModifiers.Shift)) == KeyboardModifiers.Shift != Keyboard.IsShiftHeldDown())
                        continue;

                    // All keys match, perform the command!
                    e.Handled = true;

                    keyboardCommand.Action?.Invoke();
                    keyboardCommand.DoTrigger();
                    break;
                }
            }

            // If the key wasn't handled, check for modifierless keys and allow pressing
            // them even if a modifier key is down
            if (!e.Handled)
            {
                foreach (var keyboardCommand in KeyboardCommands.Instance.Commands)
                {
                    if (keyboardCommand.ForActionsOnly)
                        continue;

                    if (e.PressedKey == keyboardCommand.Key.Key)
                    {
                        // Key matches, check modifiers

                        if (((keyboardCommand.Key.Modifiers & KeyboardModifiers.Alt) == KeyboardModifiers.Alt) && !Keyboard.IsAltHeldDown())
                            continue;

                        if (((keyboardCommand.Key.Modifiers & KeyboardModifiers.Ctrl) == KeyboardModifiers.Ctrl) && !Keyboard.IsCtrlHeldDown())
                            continue;

                        if (((keyboardCommand.Key.Modifiers & KeyboardModifiers.Shift)) == KeyboardModifiers.Shift && !Keyboard.IsShiftHeldDown())
                            continue;

                        // All keys match, perform the command!
                        e.Handled = true;

                        keyboardCommand.Action?.Invoke();
                        keyboardCommand.DoTrigger();
                        break;
                    }
                }
            }
        }

        private void TileDisplay_SelectedTileChanged(object sender, EventArgs e)
        {
            mapUI.CursorAction = placeTerrainCursorAction;
            placeTerrainCursorAction.Tile = tileSelector.TileDisplay.SelectedTile;
            if (placeTerrainCursorAction.Tile != null)
            {
                // Check if this is a tile that is not benefical to place as larger than 1x1
                if (theaterGraphics.Theater.TileSets[placeTerrainCursorAction.Tile.TileSetId].Only1x1)
                    editorState.BrushSize = map.EditorConfig.BrushSizes.Find(bs => bs.Height == 1 && bs.Width == 1);
            }
        }

        private void OverlayFrameSelector_SelectedFrameChanged(object sender, EventArgs e)
        {
            if (overlayFrameSelector.SelectedFrameIndex < 0)
                overlayPlacementAction.FrameIndex = null;
            else
                overlayPlacementAction.FrameIndex = overlayFrameSelector.SelectedFrameIndex;
        }

        private void UndoAction(object sender, EventArgs e) => mutationManager.Undo();

        private void RedoAction(object sender, EventArgs e) => mutationManager.Redo();

        private void CopyRectanglularAreaAction(object sender, EventArgs e)
        {
            copyRectangularTerrainCursorAction.StartCellCoords = null;
            ApplyCopyAction(copyRectangularTerrainCursorAction);
        }

        private void CopyCustomShapedAreaAction(object sender, EventArgs e)
        {
            ApplyCopyAction(copyCustomShapedTerrainCursorAction);
        }

        private void ApplyCopyAction(CopyTerrainCursorActionBase copyAction)
        {
            copyAction.EntryTypes = windowController.CopiedEntryTypesWindow.GetEnabledEntryTypes();
            mapUI.CursorAction = copyAction;
        }

        private void PasteAction(object sender, EventArgs e) => mapUI.CursorAction = pasteTerrainCursorAction;

        private void UpdateMapFileWatcher()
        {
            // We update the map file watcher here so we're not checking its status
            // "too often"; the file system watcher might give out multiple events
            // in case some application writes the file in multiple passes.

            // By checking the map file watcher here, we only check it once per frame.

            if (mapFileWatcher.ModifyEventDetected)
            {
                if (mapFileWatcher.HandleModifyEvent())
                {
                    notificationManager.AddNotification("The map file has been modified outside of the editor. The map's INI data has been reloaded." + Environment.NewLine + Environment.NewLine +
                        "If you made edits to visible map data (terrain, objects, overlay etc.) outside of the editor, you can" + Environment.NewLine +
                        "re-load the map to apply the effects. If you only made changes to other INI data, you can ignore this message.");
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (loadMapStage > 0)
            {
                loadMapStage++;

                if (loadMapStage > 3)
                    LoadMap();
            }
            else
            {
                string error = autosaveTimer.Update(gameTime.ElapsedGameTime);
                if (error != null)
                {
                    NotificationManager.AddNotification("Failed to auto-save the map." + Environment.NewLine + Environment.NewLine + 
                        "Please make sure that you are not running the editor from a write-protected directory (such as Program Files)." + Environment.NewLine + Environment.NewLine + 
                        "Returned OS error: " + error);
                }

                UpdateMapFileWatcher();
            }

            if (Alpha < 1.0f)
            {
                Alpha += (float)(gameTime.ElapsedGameTime.TotalSeconds * 1.0f);
            }
        }

        public void RefreshAllLanguage()
        {
            // 主菜单
            topBarMenu?.RefreshLanguage(TSMapEditor.UI.MainMenu.IsChinese);
            // 侧边栏
            editorSidebar?.RefreshLanguage(TSMapEditor.UI.MainMenu.IsChinese);
            // 地形选择器
            tileSelector?.RefreshLanguage(TSMapEditor.UI.MainMenu.IsChinese);
            // 工具栏
            foreach (var child in Children)
            {
                if (child is EditorControlsPanel panel)
                    panel.RefreshLanguage(TSMapEditor.UI.MainMenu.IsChinese);
            }
            
            // 添加对BasicSectionConfigWindow的支持
            if (windowController != null)
            {
                if (windowController.BasicSectionConfigWindow != null)
                    windowController.BasicSectionConfigWindow.RefreshLanguage(TSMapEditor.UI.MainMenu.IsChinese);
                    
                // 添加对MapSizeWindow的支持
                if (windowController.MapSizeWindow != null)
                    windowController.MapSizeWindow.RefreshLanguage(TSMapEditor.UI.MainMenu.IsChinese);
                    
                // 添加对ExpandMapWindow的支持
                if (windowController.ExpandMapWindow != null)
                    windowController.ExpandMapWindow.RefreshLanguage(TSMapEditor.UI.MainMenu.IsChinese);
                    
                // 添加对LightingSettingsWindow的支持
                if (windowController.LightingSettingsWindow != null)
                    windowController.LightingSettingsWindow.RefreshLanguage(TSMapEditor.UI.MainMenu.IsChinese);
                    
                // 添加对RenderedObjectsConfigurationWindow的支持
                if (windowController.RenderedObjectsConfigurationWindow != null)
                    windowController.RenderedObjectsConfigurationWindow.RefreshLanguage();
                    
                // 添加对FindWaypointWindow的支持
                if (windowController.FindWaypointWindow != null)
                    windowController.FindWaypointWindow.RefreshLanguage();
                    
                // 添加对PlaceWaypointWindow的支持
                if (windowController.PlaceWaypointWindow != null)
                    windowController.PlaceWaypointWindow.RefreshLanguage();
                    
                // 添加对TerrainGeneratorConfigWindow的支持
                if (windowController.TerrainGeneratorConfigWindow != null)
                    windowController.TerrainGeneratorConfigWindow.RefreshAllPanelsLanguage(TSMapEditor.UI.MainMenu.IsChinese);
                    
                // 添加对SelectConnectedTileWindow的支持
                if (windowController.SelectConnectedTileWindow != null)
                    windowController.SelectConnectedTileWindow.RefreshLanguage();
                    
                // 添加对DeletionModeConfigurationWindow的支持
                if (windowController.DeletionModeConfigurationWindow != null)
                    windowController.DeletionModeConfigurationWindow.RefreshLanguage();
                    
                // 添加对RunScriptWindow的支持
                if (windowController.RunScriptWindow != null)
                    windowController.RunScriptWindow.RefreshLanguage();
                    
                // 添加对HousesWindow的支持
                if (windowController.HousesWindow != null)
                    windowController.HousesWindow.RefreshLanguage();
                    
                // 添加对TriggersWindow的支持
                if (windowController.TriggersWindow != null)
                    windowController.TriggersWindow.RefreshLanguage();
                
                // 添加对TaskForcesWindow的支持
                if (windowController.TaskForcesWindow != null)
                    windowController.TaskForcesWindow.RefreshLanguage();
                    
                // 添加对TeamTypesWindow的支持
                if (windowController.TeamTypesWindow != null)
                    windowController.TeamTypesWindow.RefreshLanguage();
                    
                // 添加对ScriptsWindow的支持
                if (windowController.ScriptsWindow != null)
                    windowController.ScriptsWindow.RefreshLanguage();
                    
                // 添加对AITriggersWindow的支持
                if (windowController.AITriggersWindow != null)
                    windowController.AITriggersWindow.RefreshLanguage();

                // 添加对HistoryWindow的支持
                if (windowController.HistoryWindow != null)
                {
                    windowController.HistoryWindow.RefreshLanguage(TSMapEditor.UI.MainMenu.IsChinese);
                }
                
                // 添加对LocalVariablesWindow的支持
                if (windowController.LocalVariablesWindow != null)
                    windowController.LocalVariablesWindow.RefreshLanguage();
                    
                // 通过反射获取SelectBridgeWindow实例（如果有的话）并刷新
                try
                {
                    var field = windowController.GetType().GetField("selectBridgeWindow", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (field != null)
                    {
                        var selectBridgeWindow = field.GetValue(windowController) as SelectBridgeWindow;
                        if (selectBridgeWindow != null)
                            selectBridgeWindow.RefreshLanguage();
                    }
                }
                catch
                {
                    // 如果反射失败，静默忽略
                }
                
                // 通过反射获取SelectThemeWindow实例（如果有的话）并刷新
                try
                {
                    var field = windowController.GetType().GetField("selectThemeWindow", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (field != null)
                    {
                        var selectThemeWindow = field.GetValue(windowController) as SelectThemeWindow;
                        if (selectThemeWindow != null)
                            selectThemeWindow.RefreshLanguage(TSMapEditor.UI.MainMenu.IsChinese);
                    }
                }
                catch
                {
                    // 如果反射失败，静默忽略
                }
            }
            
            // 其他窗口、弹窗等可按需补充
            windowController.DeletionModeConfigurationWindow.RefreshLanguage();
            windowController.RunScriptWindow.RefreshLanguage();
            windowController.HousesWindow.RefreshLanguage();
            windowController.TriggersWindow.RefreshLanguage();

            // The following windows are special and are not initialized at all times, so we need null-checks for them
            var newHouseWindow = windowController.HousesWindow.GetType().GetField("newHouseWindow", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(windowController.HousesWindow) as NewHouseWindow;
            if (newHouseWindow != null)
            {
                // Additional logic for newHouseWindow
            }
        }
    }
}
