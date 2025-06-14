using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Linq;
using TSMapEditor;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI.Controls;
using TSMapEditor.UI.CursorActions;
using TSMapEditor.UI.CursorActions.HeightActions;
using TSMapEditor.UI.Windows;

namespace TSMapEditor.UI.TopBar
{
    public class EditorControlsPanel : INItializableWindow
    {
        public EditorControlsPanel(WindowManager windowManager, Map map, TheaterGraphics theaterGraphics,
            EditorConfig editorConfig, EditorState editorState, WindowController windowController,
            PlaceTerrainCursorAction terrainPlacementAction,
            PlaceWaypointCursorAction placeWaypointCursorAction,
            ICursorActionTarget cursorActionTarget) : base(windowManager)
        {
            this.map = map;
            this.theaterGraphics = theaterGraphics;
            this.editorConfig = editorConfig;
            this.editorState = editorState;
            this.windowController = windowController;
            this.terrainPlacementAction = terrainPlacementAction;
            this.placeWaypointCursorAction = placeWaypointCursorAction;
            this.cursorActionTarget = cursorActionTarget;

            deletionModeCursorAction = new DeletionModeCursorAction(cursorActionTarget);
            fsRaiseGroundCursorAction = new FSRaiseGroundCursorAction(cursorActionTarget);
            fsLowerGroundCursorAction = new FSLowerGroundCursorAction(cursorActionTarget);
            raiseGroundCursorAction = new RaiseGroundCursorAction(cursorActionTarget);
            lowerGroundCursorAction = new LowerGroundCursorAction(cursorActionTarget);
            raiseCellsCursorAction = new RaiseCellsCursorAction(cursorActionTarget);
            lowerCellsCursorAction = new LowerCellsCursorAction(cursorActionTarget);
            flattenGroundCursorAction = new FlattenGroundCursorAction(cursorActionTarget);
        }

        private readonly Map map;
        private readonly TheaterGraphics theaterGraphics;
        private readonly EditorConfig editorConfig;
        private readonly EditorState editorState;
        private readonly WindowController windowController;
        private readonly ICursorActionTarget cursorActionTarget;
        private readonly PlaceTerrainCursorAction terrainPlacementAction;
        private readonly PlaceWaypointCursorAction placeWaypointCursorAction;
        private readonly DeletionModeCursorAction deletionModeCursorAction;
        private readonly FSRaiseGroundCursorAction fsRaiseGroundCursorAction;
        private readonly FSLowerGroundCursorAction fsLowerGroundCursorAction;
        private readonly RaiseGroundCursorAction raiseGroundCursorAction;
        private readonly LowerGroundCursorAction lowerGroundCursorAction;
        private readonly RaiseCellsCursorAction raiseCellsCursorAction;
        private readonly LowerCellsCursorAction lowerCellsCursorAction;
        private readonly FlattenGroundCursorAction flattenGroundCursorAction;

        private XNADropDown ddBrushSize;
        private XNACheckBox chkAutoLAT;
        private XNACheckBox chkOnlyPaintOnClearGround;
        private XNACheckBox chkDrawMapWideOverlay;
        private XNAButton btnLangSwitch;

        // ToolTip实例成员
        private ToolTip toolTipPlaceWaypoint;
        private ToolTip toolTipDeletionMode;
        private ToolTip toolTipFrameworkMode;
        private ToolTip toolTip2DMode;
        private ToolTip toolTipRaiseGround;
        private ToolTip toolTipLowerGround;
        private ToolTip toolTipRaiseGroundSteep;
        private ToolTip toolTipLowerGroundSteep;
        private ToolTip toolTipRaiseCells;
        private ToolTip toolTipLowerCells;
        private ToolTip toolTipFlattenGround;
        private ToolTip toolTipGenerateTerrain;
        private ToolTip toolTipTerrainGeneratorOptions;
        private ToolTip toolTipDrawConnectedTiles;

        // 按钮成员变量
        private EditorButton btnPlaceWaypoint;
        private EditorButton btnDeletionMode;
        private EditorButton btnFrameworkMode;
        private EditorButton btn2DMode;
        private EditorButton btnRaiseGround;
        private EditorButton btnLowerGround;
        private EditorButton btnRaiseGroundSteep;
        private EditorButton btnLowerGroundSteep;
        private EditorButton btnRaiseCells;
        private EditorButton btnLowerCells;
        private EditorButton btnFlattenGround;
        private EditorButton btnGenerateTerrain;
        private EditorButton btnTerrainGeneratorOptions;
        private EditorButton btnDrawConnectedTiles;

        public override void Initialize()
        {
            SubDirectory = string.Empty;
            Name = nameof(EditorControlsPanel);
            base.Initialize();

            ddBrushSize = FindChild<XNADropDown>(nameof(ddBrushSize));
            foreach (var brushSize in editorConfig.BrushSizes)
            {
                ddBrushSize.AddItem(brushSize.Width + "x" + brushSize.Height);
            }
            ddBrushSize.SelectedIndexChanged += DdBrushSize_SelectedIndexChanged;
            ddBrushSize.SelectedIndex = 0;

            chkAutoLAT = FindChild<XNACheckBox>(nameof(chkAutoLAT));
            chkAutoLAT.Checked = editorState.AutoLATEnabled;
            chkAutoLAT.CheckedChanged += ChkAutoLat_CheckedChanged;

            chkOnlyPaintOnClearGround = FindChild<XNACheckBox>(nameof(chkOnlyPaintOnClearGround));
            chkOnlyPaintOnClearGround.Checked = editorState.OnlyPaintOnClearGround;
            chkOnlyPaintOnClearGround.CheckedChanged += ChkOnlyPaintOnClearGround_CheckedChanged;

            chkDrawMapWideOverlay = FindChild<XNACheckBox>(nameof(chkDrawMapWideOverlay));
            chkDrawMapWideOverlay.Checked = editorState.DrawMapWideOverlay;
            chkDrawMapWideOverlay.CheckedChanged += ChkDrawMapWideOverlay_CheckedChanged;
            CheckForMapWideOverlay();
            editorState.MapWideOverlayExistsChanged += (s, e) => CheckForMapWideOverlay();

            btnPlaceWaypoint = FindChild<EditorButton>("btnPlaceWaypoint", true);
            if (btnPlaceWaypoint != null)
            {
                toolTipPlaceWaypoint = new ToolTip(WindowManager, btnPlaceWaypoint);
                toolTipPlaceWaypoint.Text = TSMapEditor.UI.MainMenu.IsChinese ? "放置航点" : "Place Waypoint";
                toolTipPlaceWaypoint.ToolTipDelay = 0;
            }
            btnDeletionMode = FindChild<EditorButton>("btnDeletionMode", true);
            if (btnDeletionMode != null)
            {
                toolTipDeletionMode = new ToolTip(WindowManager, btnDeletionMode);
                toolTipDeletionMode.Text = TSMapEditor.UI.MainMenu.IsChinese ? "删除模式" : "Deletion Mode";
                toolTipDeletionMode.ToolTipDelay = 0;
            }
            btnFrameworkMode = FindChild<EditorButton>("btnFrameworkMode", true);
            if (btnFrameworkMode != null)
            {
                toolTipFrameworkMode = new ToolTip(WindowManager, btnFrameworkMode);
                toolTipFrameworkMode.Text = TSMapEditor.UI.MainMenu.IsChinese ? "框架模式（疯狂大理石）" : "Toggle Framework Mode (Marble Madness)";
                toolTipFrameworkMode.ToolTipDelay = 0;
            }
            btn2DMode = FindChild<EditorButton>("btn2DMode", true);
            if (btn2DMode != null)
            {
                toolTip2DMode = new ToolTip(WindowManager, btn2DMode);
                toolTip2DMode.Text = TSMapEditor.UI.MainMenu.IsChinese ? "2D模式" : "Toggle 2D Mode";
                toolTip2DMode.ToolTipDelay = 0;
            }
            btnRaiseGround = FindChild<EditorButton>("btnRaiseGround", true);
            if (btnRaiseGround != null)
            {
                toolTipRaiseGround = new ToolTip(WindowManager, btnRaiseGround);
                toolTipRaiseGround.Text = TSMapEditor.UI.MainMenu.IsChinese ? "抬高地形（非陡坡/FinalSun模式）" : "Raise Ground (Non-Steep Ramps / FinalSun Mode)";
                toolTipRaiseGround.ToolTipDelay = 0;
            }
            btnLowerGround = FindChild<EditorButton>("btnLowerGround", true);
            if (btnLowerGround != null)
            {
                toolTipLowerGround = new ToolTip(WindowManager, btnLowerGround);
                toolTipLowerGround.Text = TSMapEditor.UI.MainMenu.IsChinese ? "降低地形（非陡坡/FinalSun模式）" : "Lower Ground (Non-Steep Ramps / FinalSun Mode)";
                toolTipLowerGround.ToolTipDelay = 0;
            }
            btnRaiseGroundSteep = FindChild<EditorButton>("btnRaiseGroundSteep", true);
            if (btnRaiseGroundSteep != null)
            {
                toolTipRaiseGroundSteep = new ToolTip(WindowManager, btnRaiseGroundSteep);
                toolTipRaiseGroundSteep.Text = TSMapEditor.UI.MainMenu.IsChinese ? "抬高地形（陡坡）" : "Raise Ground (Steep Ramps)";
                toolTipRaiseGroundSteep.ToolTipDelay = 0;
            }
            btnLowerGroundSteep = FindChild<EditorButton>("btnLowerGroundSteep", true);
            if (btnLowerGroundSteep != null)
            {
                toolTipLowerGroundSteep = new ToolTip(WindowManager, btnLowerGroundSteep);
                toolTipLowerGroundSteep.Text = TSMapEditor.UI.MainMenu.IsChinese ? "降低地形（陡坡）" : "Lower Ground (Steep Ramps)";
                toolTipLowerGroundSteep.ToolTipDelay = 0;
            }
            btnRaiseCells = FindChild<EditorButton>("btnRaiseCells", true);
            if (btnRaiseCells != null)
            {
                toolTipRaiseCells = new ToolTip(WindowManager, btnRaiseCells);
                toolTipRaiseCells.Text = TSMapEditor.UI.MainMenu.IsChinese ? "抬高单元格" : "Raise Individual Cells";
                toolTipRaiseCells.ToolTipDelay = 0;
            }
            btnLowerCells = FindChild<EditorButton>("btnLowerCells", true);
            if (btnLowerCells != null)
            {
                toolTipLowerCells = new ToolTip(WindowManager, btnLowerCells);
                toolTipLowerCells.Text = TSMapEditor.UI.MainMenu.IsChinese ? "降低单元格" : "Lower Individual Cells";
                toolTipLowerCells.ToolTipDelay = 0;
            }
            btnFlattenGround = FindChild<EditorButton>("btnFlattenGround", true);
            if (btnFlattenGround != null)
            {
                toolTipFlattenGround = new ToolTip(WindowManager, btnFlattenGround);
                toolTipFlattenGround.Text = TSMapEditor.UI.MainMenu.IsChinese ? "地形抹平" : "Flatten Ground";
                toolTipFlattenGround.ToolTipDelay = 0;
            }
            btnGenerateTerrain = FindChild<EditorButton>("btnGenerateTerrain", true);
            if (btnGenerateTerrain != null)
            {
                toolTipGenerateTerrain = new ToolTip(WindowManager, btnGenerateTerrain);
                toolTipGenerateTerrain.Text = TSMapEditor.UI.MainMenu.IsChinese ? "生成地形" : "Generate Terrain";
                toolTipGenerateTerrain.ToolTipDelay = 0;
            }
            btnTerrainGeneratorOptions = FindChild<EditorButton>("btnTerrainGeneratorOptions", true);
            if (btnTerrainGeneratorOptions != null)
            {
                toolTipTerrainGeneratorOptions = new ToolTip(WindowManager, btnTerrainGeneratorOptions);
                toolTipTerrainGeneratorOptions.Text = TSMapEditor.UI.MainMenu.IsChinese ? "地形生成器选项" : "Terrain Generator Options";
                toolTipTerrainGeneratorOptions.ToolTipDelay = 0;
            }
            btnDrawConnectedTiles = FindChild<EditorButton>("btnDrawConnectedTiles", true);
            if (btnDrawConnectedTiles != null)
            {
                toolTipDrawConnectedTiles = new ToolTip(WindowManager, btnDrawConnectedTiles);
                toolTipDrawConnectedTiles.Text = TSMapEditor.UI.MainMenu.IsChinese ? "绘制连通地块" : "Draw Connected Tiles";
                toolTipDrawConnectedTiles.ToolTipDelay = 0;
            }

            FindChild<EditorButton>("btnPlaceWaypoint").LeftClick += (s, e) => editorState.CursorAction = placeWaypointCursorAction;
            FindChild<EditorButton>("btnDeletionMode").LeftClick += (s, e) => editorState.CursorAction = deletionModeCursorAction;

            btnRaiseGround = FindChild<EditorButton>("btnRaiseGround", true);
            if (btnRaiseGround != null)
                btnRaiseGround.LeftClick += (s, e) => { editorState.CursorAction = fsRaiseGroundCursorAction; editorState.BrushSize = map.EditorConfig.BrushSizes.Find(bs => bs.Width == 3 && bs.Height == 3); };

            btnLowerGround = FindChild<EditorButton>("btnLowerGround", true);
            if (btnLowerGround != null)
                btnLowerGround.LeftClick += (s, e) => { editorState.CursorAction = fsLowerGroundCursorAction; editorState.BrushSize = map.EditorConfig.BrushSizes.Find(bs => bs.Width == 2 && bs.Height == 2); };

            btnRaiseGroundSteep = FindChild<EditorButton>("btnRaiseGroundSteep", true);
            if (btnRaiseGroundSteep != null)
                btnRaiseGroundSteep.LeftClick += (s, e) => { editorState.CursorAction = raiseGroundCursorAction; editorState.BrushSize = map.EditorConfig.BrushSizes.Find(bs => bs.Width == 3 && bs.Height == 3); };

            btnLowerGroundSteep = FindChild<EditorButton>("btnLowerGroundSteep", true);
            if (btnLowerGroundSteep != null)
                btnLowerGroundSteep.LeftClick += (s, e) => { editorState.CursorAction = lowerGroundCursorAction; editorState.BrushSize = map.EditorConfig.BrushSizes.Find(bs => bs.Width == 2 && bs.Height == 2); };

            btnRaiseCells = FindChild<EditorButton>("btnRaiseCells", true);
            if (btnRaiseCells != null)
                btnRaiseCells.LeftClick += (s, e) => editorState.CursorAction = raiseCellsCursorAction;

            btnLowerCells = FindChild<EditorButton>("btnLowerCells", true);
            if (btnLowerCells != null)
                btnLowerCells.LeftClick += (s, e) => editorState.CursorAction = lowerCellsCursorAction;

            btnFlattenGround = FindChild<EditorButton>("btnFlattenGround", true);
            if (btnFlattenGround != null)
                btnFlattenGround.LeftClick += (s, e) => editorState.CursorAction = flattenGroundCursorAction;

            btnFrameworkMode = FindChild<EditorButton>("btnFrameworkMode", true);
            if (btnFrameworkMode != null)
                btnFrameworkMode.LeftClick += (s, e) => editorState.IsMarbleMadness = !editorState.IsMarbleMadness;

            btn2DMode = FindChild<EditorButton>("btn2DMode", true);
            if (btn2DMode != null)
                btn2DMode.LeftClick += (s, e) => editorState.Is2DMode = !editorState.Is2DMode;

            btnGenerateTerrain = FindChild<EditorButton>("btnGenerateTerrain", true);
            if (btnGenerateTerrain != null)
                btnGenerateTerrain.LeftClick += (s, e) => EnterTerrainGenerator();

            btnTerrainGeneratorOptions = FindChild<EditorButton>("btnTerrainGeneratorOptions", true);
            if (btnTerrainGeneratorOptions != null)
                btnTerrainGeneratorOptions.LeftClick += (s, e) => windowController.TerrainGeneratorConfigWindow.Open();

            btnDrawConnectedTiles = FindChild<EditorButton>("btnDrawConnectedTiles", true);
            if (btnDrawConnectedTiles != null)
                btnDrawConnectedTiles.LeftClick += (s, e) => windowController.SelectConnectedTileWindow.Open();

            KeyboardCommands.Instance.NextBrushSize.Triggered += NextBrushSize_Triggered;
            KeyboardCommands.Instance.PreviousBrushSize.Triggered += PreviousBrushSize_Triggered;
            KeyboardCommands.Instance.ToggleAutoLAT.Triggered += ToggleAutoLAT_Triggered;
            KeyboardCommands.Instance.ToggleMapWideOverlay.Triggered += (s, e) => { if (editorState.MapWideOverlayExists) chkDrawMapWideOverlay.Checked = !chkDrawMapWideOverlay.Checked; };

            editorState.AutoLATEnabledChanged += (s, e) => chkAutoLAT.Checked = editorState.AutoLATEnabled;
            editorState.OnlyPaintOnClearGroundChanged += (s, e) => chkOnlyPaintOnClearGround.Checked = editorState.OnlyPaintOnClearGround;
            editorState.DrawMapWideOverlayChanged += (s, e) => chkDrawMapWideOverlay.Checked = editorState.DrawMapWideOverlay;
            editorState.BrushSizeChanged += (s, e) => ddBrushSize.SelectedIndex = map.EditorConfig.BrushSizes.FindIndex(bs => bs == editorState.BrushSize);

            InitLATPanel();
            // 添加中英切换按钮，放在右下角
            btnLangSwitch = new XNAButton(WindowManager)
            {
                Name = "btnLangSwitch",
                Width = 60,
                Height = 24,
                Text = TSMapEditor.UI.MainMenu.IsChinese ? "中文/English" : "English/中文"
            };
            // 继续向左移动
            int langPanelX = this.Width - btnLangSwitch.Width - 160;
            int langPanelY = this.Height - btnLangSwitch.Height - 10;
            btnLangSwitch.X = 4;
            btnLangSwitch.Y = 4;
            var langPanel = new XNAPanel(WindowManager)
            {
                X = langPanelX,
                Y = langPanelY,
                Width = btnLangSwitch.Width + 24,
                Height = btnLangSwitch.Height + 24,
                DrawBorders = true,
                BorderColor = Microsoft.Xna.Framework.Color.CornflowerBlue
            };
            langPanel.AddChild(btnLangSwitch);
            AddChild(langPanel);
            btnLangSwitch.LeftClick += (s, e) =>
            {
                TSMapEditor.UI.MainMenu.IsChinese = !TSMapEditor.UI.MainMenu.IsChinese;
                btnLangSwitch.Text = TSMapEditor.UI.MainMenu.IsChinese ? "中文/English" : "English/中文";
                // 全局刷新，向上查找UIManager
                XNAControl parent = this.Parent;
                while (parent != null && !(parent is TSMapEditor.UI.UIManager))
                    parent = parent.Parent;
                (parent as TSMapEditor.UI.UIManager)?.RefreshAllLanguage();
            };
        }

        private void EnterTerrainGenerator()
        {
            if (windowController.TerrainGeneratorConfigWindow.TerrainGeneratorConfig == null)
            {
                windowController.TerrainGeneratorConfigWindow.Open();
                return;
            }

            var generateTerrainCursorAction = new GenerateTerrainCursorAction(cursorActionTarget);
            generateTerrainCursorAction.TerrainGeneratorConfiguration = windowController.TerrainGeneratorConfigWindow.TerrainGeneratorConfig;
            editorState.CursorAction = generateTerrainCursorAction;
        }

        private void CheckForMapWideOverlay()
        {
            chkDrawMapWideOverlay.AllowChecking = editorState.MapWideOverlayExists;
        }

        private void InitLATPanel()
        {
            var latPanel = FindChild<XNAPanel>("LATPanel");

            var btnClearTerrain = new EditorButton(WindowManager);
            btnClearTerrain.Name = nameof(btnClearTerrain);
            btnClearTerrain.X = 0;
            btnClearTerrain.Y = 0;
            btnClearTerrain.Width = Constants.CellSizeX + Constants.UIHorizontalSpacing * 2;
            btnClearTerrain.Height = Constants.CellSizeY + 2;
            btnClearTerrain.ExtraTexture = theaterGraphics.GetTileGraphics(0).TMPImages[0].TextureFromTmpImage_RGBA(GraphicsDevice);
            btnClearTerrain.LeftClick += (s, e) => EnterLATPlacementMode(0);
            latPanel.AddChild(btnClearTerrain);
            var clearToolTip = new ToolTip(WindowManager, btnClearTerrain);
            clearToolTip.Text = TSMapEditor.UI.MainMenu.IsChinese ? "清空" : "Clear";
            clearToolTip.CNText = "清空";
            clearToolTip.ToolTipDelay = 0;

            int prevRight = btnClearTerrain.Right;
            int y = btnClearTerrain.Y;

            for (int i = 0; i < map.TheaterInstance.Theater.LATGrounds.Count; i++)
            {
                LATGround autoLATGround = map.TheaterInstance.Theater.LATGrounds[i];

                // If this LAT is for Marble Madness mode only, skip
                // (some modders might do this if they don't use all the
                // TS LAT slots)
                if (autoLATGround.GroundTileSet.NonMarbleMadness > -1)
                    continue;

                if (!autoLATGround.GroundTileSet.AllowToPlace)
                    continue;

                // If we already have a button for this ground type, then skip it
                // The editor can automatically place the correct LAT variations
                // of a tile based on its surroundings
                bool alreadyExists = false;
                for (int j = i - 1; j > -1; j--)
                {
                    if (map.TheaterInstance.Theater.LATGrounds[j].GroundTileSet == autoLATGround.GroundTileSet)
                    {
                        alreadyExists = true;
                        break;
                    }
                }

                if (alreadyExists)
                    continue;

                var btn = new EditorButton(WindowManager);
                btn.Name = "btn" + autoLATGround.GroundTileSet.SetName;
                btn.X = prevRight + Constants.UIHorizontalSpacing;
                btn.Y = y;
                btn.Width = btnClearTerrain.Width;
                btn.Height = btnClearTerrain.Height;
                var tileGraphics = theaterGraphics.GetTileGraphics(autoLATGround.GroundTileSet.StartTileIndex);
                btn.ExtraTexture = tileGraphics != null && tileGraphics.TMPImages.Length > 0 ? tileGraphics.TMPImages[0].TextureFromTmpImage_RGBA(GraphicsDevice) : null;
                btn.Tag = autoLATGround;
                btn.LeftClick += (s, e) => EnterLATPlacementMode(autoLATGround.GroundTileSet.StartTileIndex);
                latPanel.AddChild(btn);

                if (btn.Right > latPanel.Width)
                {
                    btn.X = btnClearTerrain.X;
                    prevRight = btn.Right;

                    y += btnClearTerrain.Height + Constants.UIVerticalSpacing;
                    btn.Y = y;
                }

                var toolTip = new ToolTip(WindowManager, btn);
                string[] allBases = map.TheaterInstance.Theater.LATGrounds.FindAll(lg => lg.GroundTileSet == autoLATGround.GroundTileSet).Select(lg =>
                {
                    if (lg.BaseTileSet == null)
                        return TSMapEditor.UI.MainMenu.IsChinese ? "清空" : "Clear";

                    string baseName = lg.BaseTileSet.SetName;
                    
                    // 如果是中文模式，翻译基础地形名称
                    if (TSMapEditor.UI.MainMenu.IsChinese)
                    {
                        switch (baseName)
                        {
                            case "Pavement": return "石头";
                            case "Dark Grass": return "深色草地";
                            case "Ground": return "土地";
                            case "Sand": return "沙地";
                            default: return baseName;
                        }
                    }
                    
                    return baseName;
                }).ToArray();

                // 硬编码翻译地形名称
                string terrainName = autoLATGround.GroundTileSet.SetName;
                string translatedName = terrainName;
                
                // 只有四种地形，直接硬编码翻译
                switch (terrainName)
                {
                    case "LAT Grass": translatedName = "LAT草地"; break;
                    case "LAT Dark Grass": translatedName = "LAT深色草地"; break;
                    case "LAT Sand": translatedName = "LAT沙地"; break;
                    case "LAT Rock": translatedName = "LAT岩石"; break;
                }
                
                string englishText = $"{terrainName} (placed on top of {string.Join(" or ", allBases)})";
                string chineseText = $"{translatedName} (放置在 {string.Join(" 或 ", allBases)} 上)";
                
                toolTip.Text = TSMapEditor.UI.MainMenu.IsChinese ? chineseText : englishText;
                toolTip.CNText = chineseText;

                toolTip.ToolTipDelay = 0;

                prevRight = btn.Right;
            }
        }

        private void EnterLATPlacementMode(int tileIndex)
        {
            terrainPlacementAction.Tile = theaterGraphics.GetTileGraphics(tileIndex);
            editorState.CursorAction = terrainPlacementAction;
        }

        private void DdBrushSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            editorState.BrushSize = editorConfig.BrushSizes[ddBrushSize.SelectedIndex];
        }

        private void ChkAutoLat_CheckedChanged(object sender, EventArgs e)
        {
            editorState.AutoLATEnabled = chkAutoLAT.Checked;
        }

        private void ChkOnlyPaintOnClearGround_CheckedChanged(object sender, EventArgs e)
        {
            editorState.OnlyPaintOnClearGround = chkOnlyPaintOnClearGround.Checked;
        }

        private void ChkDrawMapWideOverlay_CheckedChanged(object sender, EventArgs e)
        {
            editorState.DrawMapWideOverlay = chkDrawMapWideOverlay.Checked;
        }

        private void PreviousBrushSize_Triggered(object sender, EventArgs e)
        {
            if (ddBrushSize.SelectedIndex < 1)
                return;

            ddBrushSize.SelectedIndex--;
        }

        private void NextBrushSize_Triggered(object sender, EventArgs e)
        {
            if (ddBrushSize.SelectedIndex >= ddBrushSize.Items.Count - 1)
                return;

            ddBrushSize.SelectedIndex++;
        }

        private void ToggleAutoLAT_Triggered(object sender, EventArgs e)
        {
            chkAutoLAT.Checked = !chkAutoLAT.Checked;
        }

        public void RefreshLanguage(bool isChinese)
        {
            // 语言切换按钮文本
            if (btnLangSwitch != null)
                btnLangSwitch.Text = isChinese ? "中文/English" : "English/中文";

            // 手动设置复选框文本，确保它们能够正确显示中文
            if (chkAutoLAT != null)
                chkAutoLAT.Text = isChinese ? "自动-LAT" : "Auto-LAT";
            if (chkOnlyPaintOnClearGround != null)
                chkOnlyPaintOnClearGround.Text = isChinese ? "仅在净地绘制" : "Only Paint on Clear";
            if (chkDrawMapWideOverlay != null)
                chkDrawMapWideOverlay.Text = isChinese ? "全图叠加层" : "Map-Wide Overlay";
            
            // 设置笔刷大小标签的文本
            var lblBrushSize = FindChild<XNALabel>("lblBrushSize", true);
            if (lblBrushSize != null)
                lblBrushSize.Text = isChinese ? "笔刷大小:" : "Brush Size:";

            // 重新初始化LAT面板
            var latPanel = FindChild<XNAPanel>("LATPanel", true);
            if (latPanel != null)
            {
                
                // 重新初始化LAT面板
                InitLATPanel();
            }

            // 重新初始化控件，从INI文件中加载文本
            RefreshLayout();
        }
    }
}
