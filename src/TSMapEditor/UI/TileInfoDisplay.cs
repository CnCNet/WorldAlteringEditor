using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Globalization;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.Rendering;

namespace TSMapEditor.UI
{
    /// <summary>
    /// A panel that displays information about a single tile.
    /// </summary>
    public class TileInfoDisplay : EditorPanel
    {
        public TileInfoDisplay(WindowManager windowManager, Map map, TheaterGraphics theaterGraphics, EditorState editorState) : base(windowManager)
        {
            this.map = map;
            this.theaterGraphics = theaterGraphics;
            this.editorState = editorState;
        }

        private readonly Map map;
        private readonly TheaterGraphics theaterGraphics;
        private readonly EditorState editorState;

        private MapTile _mapTile;
        public MapTile MapTile
        {
            get => _mapTile;
            set { _mapTile = value; RefreshInfo(); }
        }

        private XNATextRenderer textRenderer;

        public override void Initialize()
        {
            Name = nameof(TileInfoDisplay);

            Width = 300;

            textRenderer = new XNATextRenderer(WindowManager);
            textRenderer.Name = nameof(textRenderer);
            textRenderer.X = Constants.UIEmptySideSpace;
            textRenderer.Y = Constants.UIEmptyTopSpace;
            textRenderer.Padding = 0;
            textRenderer.SpaceBetweenLines = 2;
            textRenderer.Width = Width - Constants.UIEmptySideSpace * 2;
            AddChild(textRenderer);

            WindowManager.RenderResolutionChanged += WindowManager_RenderResolutionChanged;

            base.Initialize();
        }

        private void WindowManager_RenderResolutionChanged(object sender, EventArgs e)
        {
            X = WindowManager.RenderResolutionX - Width;
        }

        private void RefreshInfo()
        {
            textRenderer.ClearTextParts();

            if (MapTile == null)
            {
                Visible = false;
                return;
            }

            Color subtleTextColor = Color.Gray;
            Color baseTextColor = Color.White;

            Visible = true;

            bool isChinese = TSMapEditor.UI.MainMenu.IsChinese;

            // 标签映射
            string GetLabel(string key)
            {
                if (!isChinese) return key;
                switch (key)
                {
                    case "Selected tool:": return "当前工具：";
                    case "No tool selected": return "未选择工具";
                    case "TileSet:": return "地表类型：";
                    case "Tile #:": return "地块编号：";
                    case "Terrain Type:": return "地形类型：";
                    case "Height:": return "高度：";
                    case "CellTag:": return "单元格标签：";
                    case "Overlay:": return "覆盖物：";
                    case "Aircraft:": return "飞行器：";
                    case "Vehicle:": return "载具：";
                    case "Structure:": return "建筑：";
                    case "Infantry:": return "步兵：";
                    case "Usages of waypoint": return "航点用途";
                    default: return key;
                }
            }

            // 地形类型映射
            string GetTerrainTypeDisplayName(string terrainType)
            {
                if (!isChinese) return terrainType;
                switch (terrainType)
                {
                    case "Clear": return "净地";
                    case "Rough": return "崎岖";
                    case "Road": return "道路";
                    case "Water": return "水域";
                    case "Beach": return "沙滩";
                    case "Pavement": return "铺装";
                    case "Veins": return "藤壶";
                    case "Unknown": return "未知";
                    default: return terrainType;
                }
            }

            if (editorState.CursorAction != null)
            {
                textRenderer.AddTextPart(new XNATextPart(GetLabel("Selected tool:"), Constants.UIDefaultFont, subtleTextColor));
                textRenderer.AddTextPart(new XNATextPart(editorState.CursorAction.GetName() + Environment.NewLine, Constants.UIDefaultFont, baseTextColor));
            }
            else
            {
                textRenderer.AddTextPart(new XNATextPart(GetLabel("No tool selected") + Environment.NewLine, Constants.UIDefaultFont, subtleTextColor));
            }

            textRenderer.AddTextPart(new XNATextPart(MapTile.X + ", " + MapTile.Y + Environment.NewLine, Constants.UIDefaultFont, baseTextColor));

            TileImage tileGraphics = theaterGraphics.GetTileGraphics(MapTile.TileIndex);
            TileSet tileSet = theaterGraphics.Theater.TileSets[tileGraphics.TileSetId];
            textRenderer.AddTextPart(new XNATextPart(GetLabel("TileSet:"), Constants.UIDefaultFont, subtleTextColor));
            textRenderer.AddTextPart(new XNATextPart(tileSet.SetName + " (" + tileGraphics.TileSetId + ")", Constants.UIDefaultFont, baseTextColor));
            textRenderer.AddTextPart(new XNATextPart(GetLabel("Tile #:"), Constants.UIDefaultFont, subtleTextColor));
            textRenderer.AddTextPart(new XNATextPart((MapTile.TileIndex - tileSet.StartTileIndex).ToString(System.Globalization.CultureInfo.InvariantCulture), Constants.UIDefaultFont, baseTextColor));

            MGTMPImage subCellImage = MapTile.SubTileIndex < tileGraphics.TMPImages.Length ? tileGraphics.TMPImages[MapTile.SubTileIndex] : null;
            string terrainType = subCellImage != null && subCellImage.TmpImage != null ? Helpers.LandTypeToString(subCellImage.TmpImage.TerrainType) : "Unknown";

            textRenderer.AddTextLine(new XNATextPart(GetLabel("Terrain Type:"), Constants.UIDefaultFont, subtleTextColor));
            textRenderer.AddTextPart(new XNATextPart(GetTerrainTypeDisplayName(terrainType), Constants.UIDefaultFont, baseTextColor));

            if (!Constants.IsFlatWorld)
            {
                textRenderer.AddTextLine(new XNATextPart(GetLabel("Height:"), Constants.UIDefaultFont, subtleTextColor));
                textRenderer.AddTextPart(new XNATextPart(MapTile.Level.ToString(), Constants.UIDefaultFont, baseTextColor));
            }

            CellTag cellTag = MapTile.CellTag;
            if (cellTag != null)
            {
                textRenderer.AddTextLine(new XNATextPart(GetLabel("CellTag:"), Constants.UIDefaultFont, subtleTextColor));
                textRenderer.AddTextPart(new XNATextPart(cellTag.Tag.Name + " (" + cellTag.Tag.ID + ")", Constants.UIDefaultFont, cellTag.Tag.Trigger.EditorColor == null ? baseTextColor : cellTag.Tag.Trigger.XNAColor));
            }

            Overlay overlay = MapTile.Overlay;
            if (overlay != null)
            {
                textRenderer.AddTextLine(new XNATextPart(GetLabel("Overlay:"), Constants.UIDefaultFont, subtleTextColor));
                textRenderer.AddTextPart(new XNATextPart(overlay.OverlayType.Name + " (" + overlay.OverlayType.Index + " " + overlay.OverlayType.ININame + "), Frame: " + overlay.FrameIndex + ", Terrain Type: " + GetTerrainTypeDisplayName(overlay.OverlayType.Land.ToString()), Constants.UIDefaultFont, baseTextColor));
            }

            MapTile.DoForAllAircraft(aircraft => AddObjectInformation(GetLabel("Aircraft:"), aircraft));
            MapTile.DoForAllVehicles(unit => AddObjectInformation(GetLabel("Vehicle:"), unit));
            MapTile.DoForAllBuildings(structure => AddObjectInformation(GetLabel("Structure:"), structure));
            MapTile.DoForAllInfantry(inf => AddObjectInformation(GetLabel("Infantry:"), inf));
            MapTile.DoForAllWaypoints(waypoint => AddWaypointInfo(waypoint));

            textRenderer.PrepareTextParts();

            Height = textRenderer.Bottom + Constants.UIEmptyBottomSpace;
        }

        private void AddWaypointInfo(Waypoint waypoint)
        {
            // Find all usages for this waypoint
            List<string> usages = new List<string>(0);

            foreach (Trigger trigger in map.Triggers)
            {
                bool usageFound = false;

                foreach (var action in trigger.Actions)
                {
                    if (usageFound)
                        break;

                    var triggerActionType = map.EditorConfig.TriggerActionTypes.GetValueOrDefault(action.ActionIndex);

                    if (triggerActionType == null)
                        continue;

                    for (int i = 0; i < triggerActionType.Parameters.Length; i++)
                    {
                        if (triggerActionType.Parameters[i] == null)
                            continue;

                        var param = triggerActionType.Parameters[i];

                        if (param.TriggerParamType == TriggerParamType.Waypoint && action.Parameters[i] == waypoint.Identifier.ToString())
                        {
                            usageFound = true;
                        }
                        else if (param.TriggerParamType == TriggerParamType.WaypointZZ && action.Parameters[i] == Helpers.WaypointNumberToAlphabeticalString(waypoint.Identifier))
                        {
                            usageFound = true;
                        }
                    }
                }

                foreach (var condition in trigger.Conditions)
                {
                    if (usageFound)
                        break;

                    var triggerEventType = map.EditorConfig.TriggerEventTypes.GetValueOrDefault(condition.ConditionIndex);

                    if (triggerEventType == null)
                        continue;

                    for (int i = 0; i < triggerEventType.Parameters.Length; i++)
                    {
                        if (triggerEventType.Parameters[i] == null)
                            continue;

                        var param = triggerEventType.Parameters[i];

                        if (param.TriggerParamType == TriggerParamType.Waypoint && condition.Parameters[i] == waypoint.Identifier.ToString())
                        {
                            usageFound = true;
                        }
                        else if (param.TriggerParamType == TriggerParamType.WaypointZZ && condition.Parameters[i] == Helpers.WaypointNumberToAlphabeticalString(waypoint.Identifier))
                        {
                            usageFound = true;
                        }
                    }
                }

                if (usageFound)
                    usages.Add("trigger '" + trigger.Name + "', ");
            }

            foreach (Script script in map.Scripts)
            {
                foreach (var actionEntry in script.Actions)
                {
                    var scriptAction = map.EditorConfig.ScriptActions.GetValueOrDefault(actionEntry.Action);

                    if (scriptAction == null)
                    {
                        continue;
                    }

                    if (scriptAction.ParamType == TriggerParamType.Waypoint && actionEntry.Argument == waypoint.Identifier)
                    {
                        usages.Add("script '" + script.Name + "', ");
                    }
                }
            }

            foreach (TeamType team in map.TeamTypes)
            {
                if (team.Waypoint == Helpers.WaypointNumberToAlphabeticalString(waypoint.Identifier))
                {
                    usages.Add("team '" + team.Name + "', ");
                }
            }

            if (usages.Count > 0)
            {
                string lastUsage = usages[usages.Count - 1];
                usages[usages.Count - 1] = lastUsage.Substring(0, lastUsage.Length - 2);

                textRenderer.AddTextLine(new XNATextPart("Usages of waypoint " + waypoint.Identifier + ":", Constants.UIDefaultFont, Color.Gray));

                foreach (var usage in usages)
                {
                    textRenderer.AddTextPart(new XNATextPart(usage, Constants.UIDefaultFont, Color.White));
                }
            }
        }

        private void AddObjectInformation<T>(string objectTypeLabel, Techno<T> techno) where T : TechnoType
        {
            textRenderer.AddTextLine(new XNATextPart(objectTypeLabel,
                Constants.UIDefaultFont, Color.Gray));
            textRenderer.AddTextPart(new XNATextPart(techno.ObjectType.Name + " (" + techno.ObjectType.ININame + "), Owner:",
                    Constants.UIDefaultFont, Color.White));
            textRenderer.AddTextPart(new XNATextPart(techno.Owner.ININame, Constants.UIBoldFont, techno.Owner.XNAColor));

            if (techno.IsFoot())
            {
                var technoAsFoot = techno as Foot<T>;
                textRenderer.AddTextPart(new XNATextPart("Mission: " + technoAsFoot.Mission, Constants.UIDefaultFont, Color.White));
            }

            if (techno.WhatAmI() == RTTIType.Unit)
            {
                var unit = techno as Unit;
                int id = map.Units.IndexOf(unit);
                textRenderer.AddTextPart(new XNATextPart("Facing: " + techno.Facing, Constants.UIDefaultFont, Color.White));

                if (unit.FollowerUnit != null)
                {
                    int followerId = map.Units.IndexOf(unit.FollowerUnit);
                    if (followerId > -1)
                    {
                        string followerName = unit.FollowerUnit.UnitType.GetEditorDisplayName();
                        textRenderer.AddTextPart(new XNATextPart("Follower: " + followerName + " at " + unit.FollowerUnit.Position, Constants.UIDefaultFont, Color.White));
                    }
                }
            }
            
            if (techno.AttachedTag != null)
            {
                textRenderer.AddTextPart(new XNATextPart(",", Constants.UIDefaultFont, Color.White));
                textRenderer.AddTextPart(new XNATextPart("Tag:", Constants.UIDefaultFont, Color.White));
                textRenderer.AddTextPart(new XNATextPart(techno.AttachedTag.Name + " (" + techno.AttachedTag.ID + ")", Constants.UIBoldFont, Color.White));
            }
        }
    }
}
