using Microsoft.Xna.Framework.Input;
using Rampastring.Tools;
using System.Collections.Generic;
using TSMapEditor.Settings;

namespace TSMapEditor.UI
{
    public class KeyboardCommands
    {
        public KeyboardCommands()
        {
            Commands = new List<KeyboardCommand>()
            {
                Undo,
                Redo,
                Save,
                ConfigureCopiedObjects,
                Copy,
                CopyCustomShape,
                Paste,
                NextTile,
                PreviousTile,
                NextTileSet,
                PreviousTileSet,
                NextSidebarNode,
                PreviousSidebarNode,
                FrameworkMode,
                NextBrushSize,
                PreviousBrushSize,
                DeleteObject,
                ToggleAutoLAT,
                ToggleMapWideOverlay,
                Toggle2DMode,
                ZoomIn,
                ZoomOut,
                ResetZoomLevel,
                RotateUnit,
                RotateUnitOneStep,
                PlaceTerrainBelow,
                FillTerrain,
                CloneObject,
                OverlapObjects,
                ViewMegamap,
                GenerateTerrain,
                ConfigureTerrainGenerator,
                PlaceTunnel,
                ToggleFullscreen,
                AdjustTileHeightUp,
                AdjustTileHeightDown,
                PlaceConnectedTile,
                RepeatConnectedTile,
                CalculateCredits,
                CheckDistance,
                CheckDistancePathfinding,

                BuildingMenu,
                InfantryMenu,
                VehicleMenu,
                AircraftMenu,
                NavalMenu,
                TerrainObjectMenu,
                OverlayMenu,
                SmudgeMenu
            };

            // Theoretically not optimal for performance, but
            // cleaner this way
            if (Constants.IsFlatWorld)
                Commands.Remove(Toggle2DMode);
        }

        public void ReadFromSettings()
        {
            IniFile iniFile = UserSettings.Instance.UserSettingsIni;

            foreach (var command in Commands)
            {
                string dataString = iniFile.GetStringValue("Keybinds", command.ININame, null);
                if (string.IsNullOrWhiteSpace(dataString))
                    continue;

                command.Key.ApplyDataString(dataString);
            }
        }

        public void WriteToSettings()
        {
            IniFile iniFile = UserSettings.Instance.UserSettingsIni;

            foreach (var command in Commands)
            {
                iniFile.SetStringValue("Keybinds", command.ININame, command.Key.GetDataString());
            }
        }

        public void ClearCommandSubscriptions()
        {
            foreach (var command in Commands)
                command.ClearSubscriptions();
        }


        public static KeyboardCommands Instance { get; set; }

        public List<KeyboardCommand> Commands { get; }

        public KeyboardCommand Undo { get; } = new KeyboardCommand("Undo", Translate("KeyboardCommands.Undo", "Undo"), new KeyboardCommandInput(Keys.Z, KeyboardModifiers.Ctrl));
        public KeyboardCommand Redo { get; } = new KeyboardCommand("Redo", Translate("KeyboardCommands.Redo", "Redo"), new KeyboardCommandInput(Keys.Y, KeyboardModifiers.Ctrl));
        public KeyboardCommand Save { get; } = new KeyboardCommand("Save", Translate("KeyboardCommands.SaveMap", "Save Map"), new KeyboardCommandInput(Keys.S, KeyboardModifiers.Ctrl));
        public KeyboardCommand ConfigureCopiedObjects { get; } = new KeyboardCommand("ConfigureCopiedObjects", Translate("KeyboardCommands.ConfigureCopiedObjects", "Configure Copied Objects"), new KeyboardCommandInput(Keys.None, KeyboardModifiers.None), false);
        public KeyboardCommand Copy { get; } = new KeyboardCommand("Copy", Translate("KeyboardCommands.Copy", "Copy"), new KeyboardCommandInput(Keys.C, KeyboardModifiers.Ctrl));
        public KeyboardCommand CopyCustomShape { get; } = new KeyboardCommand("CopyCustomShape", Translate("KeyboardCommands.CopyCustomShape", "Copy Custom Shape"), new KeyboardCommandInput(Keys.C, KeyboardModifiers.Alt));
        public KeyboardCommand Paste { get; } = new KeyboardCommand("Paste", Translate("KeyboardCommands.Paste", "Paste"), new KeyboardCommandInput(Keys.V, KeyboardModifiers.Ctrl));
        public KeyboardCommand NextTile { get; } = new KeyboardCommand("NextTile", Translate("KeyboardCommands.SelectNextTile", "Select Next Tile"), new KeyboardCommandInput(Keys.M, KeyboardModifiers.None));
        public KeyboardCommand PreviousTile { get; } = new KeyboardCommand("PreviousTile", Translate("KeyboardCommands.SelectPreviousTile", "Select Previous Tile"), new KeyboardCommandInput(Keys.N, KeyboardModifiers.None));
        public KeyboardCommand NextTileSet { get; } = new KeyboardCommand("NextTileSet", Translate("KeyboardCommands.SelectNextTileSet", "Select Next TileSet"), new KeyboardCommandInput(Keys.J, KeyboardModifiers.None));
        public KeyboardCommand PreviousTileSet { get; } = new KeyboardCommand("PreviousTileSet", Translate("KeyboardCommands.SelectPreviousTileSet", "Select Previous TileSet"), new KeyboardCommandInput(Keys.H, KeyboardModifiers.None));
        public KeyboardCommand NextSidebarNode { get; } = new KeyboardCommand("NextSidebarNode", Translate("KeyboardCommands.SelectNextSidebarNode", "Select Next Sidebar Node"), new KeyboardCommandInput(Keys.P, KeyboardModifiers.None));
        public KeyboardCommand PreviousSidebarNode { get; } = new KeyboardCommand("PreviousSidebarNode", Translate("KeyboardCommands.SelectPreviousSidebarNode", "Select Previous Sidebar Node"), new KeyboardCommandInput(Keys.O, KeyboardModifiers.None));
        public KeyboardCommand FrameworkMode { get; } = new KeyboardCommand("MarbleMadness", Translate("KeyboardCommands.ToggleMarbleMadnessMode", "MarbleMadness", "Framework Mode (Marble Madness)"), new KeyboardCommandInput(Keys.F, KeyboardModifiers.Shift));
        public KeyboardCommand NextBrushSize { get; } = new KeyboardCommand("NextBrushSize", Translate("KeyboardCommands.NextBrushSize", "Next Brush Size"), new KeyboardCommandInput(Keys.OemPlus, KeyboardModifiers.None));
        public KeyboardCommand PreviousBrushSize { get; } = new KeyboardCommand("PreviousBrushSize", Translate("KeyboardCommands.PreviousBrushSize", "Previous Brush Size"), new KeyboardCommandInput(Keys.D0, KeyboardModifiers.None));
        public KeyboardCommand DeleteObject { get; } = new KeyboardCommand("DeleteObject", Translate("KeyboardCommands.DeleteObject", "Delete Object"), new KeyboardCommandInput(Keys.Delete, KeyboardModifiers.None));
        public KeyboardCommand ToggleAutoLAT { get; } = new KeyboardCommand("ToggleAutoLAT", Translate("KeyboardCommands.Toggle AutoLAT", "Toggle AutoLAT"), new KeyboardCommandInput(Keys.L, KeyboardModifiers.Ctrl));
        public KeyboardCommand ToggleMapWideOverlay { get; } = new KeyboardCommand("ToggleMapWideOverlay", Translate("KeyboardCommands.ToggleMapWideOverlay", "Toggle Map-Wide Overlay"), new KeyboardCommandInput(Keys.F2, KeyboardModifiers.None));
        public KeyboardCommand Toggle2DMode { get; } = new KeyboardCommand("Toggle2DMode", Translate("KeyboardCommands.Toggle2DMode", "Toggle 2D Mode"), new KeyboardCommandInput(Keys.D, KeyboardModifiers.Shift));
        public KeyboardCommand ZoomIn { get; } = new KeyboardCommand("ZoomIn", Translate("KeyboardCommands.ZoomIn", "Zoom In"), new KeyboardCommandInput(Keys.OemPlus, KeyboardModifiers.Ctrl));
        public KeyboardCommand ZoomOut { get; } = new KeyboardCommand("ZoomOut", Translate("KeyboardCommands.ZoomOut", "Zoom Out"), new KeyboardCommandInput(Keys.OemMinus, KeyboardModifiers.Ctrl));
        public KeyboardCommand ResetZoomLevel { get; } = new KeyboardCommand("ResetZoomLevel", Translate("KeyboardCommands.ResetZoomLevel", "Reset Zoom Level"), new KeyboardCommandInput(Keys.D0, KeyboardModifiers.Ctrl));
        public KeyboardCommand RotateUnit { get; } = new KeyboardCommand("RotateUnit", Translate("KeyboardCommands.RotateUnit", "Rotate Unit"), new KeyboardCommandInput(Keys.A, KeyboardModifiers.None));
        public KeyboardCommand RotateUnitOneStep { get; } = new KeyboardCommand("RotateUnitOneStep", Translate("KeyboardCommands.RotateObjectOneStep", "Rotate Object One Step"), new KeyboardCommandInput(Keys.A, KeyboardModifiers.Shift));
        public KeyboardCommand PlaceTerrainBelow { get; } = new KeyboardCommand("PlaceTerrainBelow", Translate("KeyboardCommands.PlaceTerrainBelowCursor", "Place Terrain Below Cursor"), new KeyboardCommandInput(Keys.None, KeyboardModifiers.Alt), true);
        public KeyboardCommand FillTerrain { get; } = new KeyboardCommand("FillTerrain", Translate("KeyboardCommands.FillTerrain", "Fill Terrain (1x1 tiles only)"), new KeyboardCommandInput(Keys.None, KeyboardModifiers.Ctrl), true);
        public KeyboardCommand CloneObject { get; } = new KeyboardCommand("CloneObject", Translate("KeyboardCommands.CloneObject", "Clone Object (Modifier)"), new KeyboardCommandInput(Keys.None, KeyboardModifiers.Shift), true);
        public KeyboardCommand OverlapObjects { get; } = new KeyboardCommand("OverlapObjects", Translate("KeyboardCommands.OverlapObjects", "Overlap Objects (Modifier)"), new KeyboardCommandInput(Keys.None, KeyboardModifiers.Alt), true);
        public KeyboardCommand ViewMegamap { get; } = new KeyboardCommand("ViewMegamap", Translate("KeyboardCommands.ViewMegamap", "View Megamap"), new KeyboardCommandInput(Keys.F12, KeyboardModifiers.None));
        public KeyboardCommand GenerateTerrain { get; } = new KeyboardCommand("GenerateTerrain", Translate("KeyboardCommands.GenerateTerrain", "Generate Terrain"), new KeyboardCommandInput(Keys.G, KeyboardModifiers.Ctrl));
        public KeyboardCommand ConfigureTerrainGenerator { get; } = new KeyboardCommand("ConfigureTerrainGenerator", Translate("KeyboardCommands.ConfigureTerrainGenerator", "Configure Terrain Generator"), new KeyboardCommandInput(Keys.G, KeyboardModifiers.Alt));
        public KeyboardCommand PlaceTunnel { get; } = new KeyboardCommand("PlaceTunnel", Translate("KeyboardCommands.PlaceTunnel", "Place Tunnel"), new KeyboardCommandInput(Keys.OemPeriod, KeyboardModifiers.None));
        public KeyboardCommand ToggleFullscreen { get; } = new KeyboardCommand("ToggleFullscreen", Translate("KeyboardCommands.ToggleFullScreen", "Toggle Full Screen"), new KeyboardCommandInput(Keys.F11, KeyboardModifiers.None));
        public KeyboardCommand AdjustTileHeightUp { get; } = new KeyboardCommand("AdjustTileHeightUp", Translate("KeyboardCommands.AdjustTileHeightUp", "Adjust Tile Height Up"), new KeyboardCommandInput(Keys.PageUp, KeyboardModifiers.None), forActionsOnly:true);
        public KeyboardCommand AdjustTileHeightDown { get; } = new KeyboardCommand("AdjustTileHeightDown", Translate("KeyboardCommands.AdjustTileHeightDown", "Adjust Tile Height Down"), new KeyboardCommandInput(Keys.PageDown, KeyboardModifiers.None), forActionsOnly:true);
        public KeyboardCommand PlaceConnectedTile { get; } = new KeyboardCommand("PlaceConnectedTile", Translate("KeyboardCommands.PlaceConnectedTile", "Place Connected Tile"), new KeyboardCommandInput(Keys.D, KeyboardModifiers.Alt));
        public KeyboardCommand RepeatConnectedTile { get; } = new KeyboardCommand("RepeatConnectedTile", Translate("KeyboardCommands.RepeatLastConnectedTile", "Repeat Last Connected Tile"), new KeyboardCommandInput(Keys.D, KeyboardModifiers.Ctrl));
        public KeyboardCommand CalculateCredits { get; } = new KeyboardCommand("CalculateCredits", Translate("KeyboardCommands.CalculateCredits", "Calculate Credits"), new KeyboardCommandInput(Keys.C, KeyboardModifiers.Shift));
        public KeyboardCommand CheckDistance { get; } = new KeyboardCommand("CheckDistance", Translate("KeyboardCommands.CheckDistance", "Check Distance"), new KeyboardCommandInput(Keys.B, KeyboardModifiers.None));
        public KeyboardCommand CheckDistancePathfinding { get; } = new KeyboardCommand("CheckDistancePathfinding", Translate("KeyboardCommands.CheckDistancePathfinding", "Check Distance (Pathfinding)"), new KeyboardCommandInput(Keys.B, KeyboardModifiers.Shift));

        public KeyboardCommand BuildingMenu { get; } = new KeyboardCommand("BuildingMenu", Translate("KeyboardCommands.BuildingMenu", "Building Menu"), new KeyboardCommandInput(Keys.D1, KeyboardModifiers.None));
        public KeyboardCommand InfantryMenu { get; } = new KeyboardCommand("InfantryMenu", Translate("KeyboardCommands.InfantryMenu", "Infantry Menu"), new KeyboardCommandInput(Keys.D2, KeyboardModifiers.None));
        public KeyboardCommand VehicleMenu { get; } = new KeyboardCommand("VehicleMenu", Translate("KeyboardCommands.VehicleMenu", "Vehicle Menu"), new KeyboardCommandInput(Keys.D3, KeyboardModifiers.None));
        public KeyboardCommand AircraftMenu { get; } = new KeyboardCommand("AircraftMenu", Translate("KeyboardCommands.AircraftMenu", "Aircraft Menu"), new KeyboardCommandInput(Keys.D4, KeyboardModifiers.None));
        public KeyboardCommand NavalMenu { get; } = new KeyboardCommand("NavalMenu", Translate("KeyboardCommands.NavalMenu", "Naval Menu"), new KeyboardCommandInput(Keys.D5, KeyboardModifiers.None));
        public KeyboardCommand TerrainObjectMenu { get; } = new KeyboardCommand("TerrainObjectMenu", Translate("KeyboardCommands.TerrainObjectsMenu", "Terrain Objects Menu"), new KeyboardCommandInput(Keys.D6, KeyboardModifiers.None));
        public KeyboardCommand OverlayMenu { get; } = new KeyboardCommand("OverlayMenu", Translate("KeyboardCommands.OverlayMenu", "Overlay Menu"), new KeyboardCommandInput(Keys.D7, KeyboardModifiers.None));
        public KeyboardCommand SmudgeMenu { get; } = new KeyboardCommand("SmudgeMenu", Translate("KeyboardCommands.SmudgeMenu", "Smudge Menu"), new KeyboardCommandInput(Keys.D8, KeyboardModifiers.None));
    }
}
