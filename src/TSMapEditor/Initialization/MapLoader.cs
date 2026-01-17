using CNCMaps.FileFormats.Encodings;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TSMapEditor.CCEngine;
using TSMapEditor.Extensions;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.Models.MapFormat;
using TSMapEditor.Rendering;

namespace TSMapEditor.Initialization
{
    /// <summary>
    /// Contains functions for parsing and applying different sections of a map file.
    /// </summary>
    public static class MapLoader
    {
        private const int BUILDING_PROPERTY_FIELD_COUNT = 17;
        private const int UNIT_PROPERTY_FIELD_COUNT = 14;
        private const int INFANTRY_PROPERTY_FIELD_COUNT = 14;
        private const int AIRCRAFT_PROPERTY_FIELD_COUNT = 12;
        private const int AI_TRIGGER_PROPERTY_FIELD_COUNT = 18;

        public static List<string> MapLoadErrors = new List<string>();

        private static void AddMapLoadError(string error)
        {
            Logger.Log(error);
            MapLoadErrors.Add(error);
        }

        public static void PreCheckMapIni(IniFile mapIni)
        {
            Logger.Log("Performing pre-load map checkup.");

            var section = mapIni.GetSection("Map");
            if (section == null)
                throw new MapLoadException("[Map] does not exist in the loaded file!");

            string size = section.GetStringValue("Size", null);
            if (size == null)
                throw new MapLoadException("Invalid [Map] Size=");
            string[] parts = size.Split(',');
            if (parts.Length != 4)
                throw new MapLoadException("Invalid [Map] Size=");

            int width = int.Parse(parts[2], CultureInfo.InvariantCulture);
            int height = int.Parse(parts[3], CultureInfo.InvariantCulture);

            if (width > Constants.MaxMapWidth)
            {
                throw new MapLoadException($"Map width cannot be greater than " +
                    $"{Constants.MaxMapWidth} cells; the map is {width} cells wide!");
            }

            if (height > Constants.MaxMapHeight)
            {
                throw new MapLoadException("Map height cannot be greater than " +
                    $"{Constants.MaxMapHeight} cells; the map is {height} cells high!");
            }

            Logger.Log("Pre-load map checkup complete.");
        }

        public static void PostCheckMap(IMap map, TheaterGraphics theaterGraphics)
        {
            Logger.Log("Performing post-load map checkup.");

            map.DoForAllValidTiles(t =>
            {
                if (t.TileIndex >= theaterGraphics.TileCount)
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.PostCheckMap.InvalidTileIndex",
                    "Invalid tile index {0} for cell at {1} - setting it to 0"), t.TileIndex, t.CoordsToPoint()));
                    t.TileIndex = 0;
                    t.SubTileIndex = 0;
                    return;
                }

                var tile = theaterGraphics.GetTile(t.TileIndex);
                var tileSet = theaterGraphics.Theater.TileSets[tile.TileSetId];
                int maxSubTileIndex = tile.SubTileCount - 1;
                if (t.SubTileIndex > maxSubTileIndex)
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.PostCheckMap.InvalidSubTileIndex", 
                        "Invalid sub-tile index {0} for cell at {1} (max: {2}) - setting it to 0. " +
                        "TileSet: {3} ({4}), index of tile within its set: {5}"),
                        t.SubTileIndex, t.CoordsToPoint(), maxSubTileIndex, tileSet.SetName, tileSet.FileName, tile.TileIndexInTileSet));

                    t.SubTileIndex = 0;

                    if (maxSubTileIndex < 0)
                    {
                        AddMapLoadError(string.Format(Translate("MapLoader.PostCheckMap.MaxSubTileCount", 
                            "    Maximum sub-tile count of 0 detected for tile at {0}, also setting the cell's tile index to 0."),
                            t.CoordsToPoint()));
                        t.TileIndex = 0;
                    }

                    return;
                }

                if (tile.GetSubTile(t.SubTileIndex) == null)
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.PostCheckMap.NullSubTile", 
                        "Null sub-tile {0} for cell at {1} - clearing the tile. " +
                        "TileSet: {2} ({3}), index of tile within its set: {4}"),
                        t.SubTileIndex, t.CoordsToPoint(), tileSet.SetName, tileSet.FileName, tile.TileIndexInTileSet));

                    t.ChangeTileIndex(0, 0);
                }
            });

            Logger.Log("Post-load map checkup complete.");
        }

        public static void ReadMapSection(IMap map, IniFile mapIni)
        {
            Logger.Log("Reading [Map] section.");

            var section = mapIni.GetSection("Map");
            if (section == null)
                throw new MapLoadException("[Map] does not exist in the loaded file!");

            string size = section.GetStringValue("Size", null);
            string[] parts = size.Split(',');

            int width = int.Parse(parts[2], CultureInfo.InvariantCulture);
            int height = int.Parse(parts[3], CultureInfo.InvariantCulture);
            map.Size = new Point2D(width, height);

            string localSize = section.GetStringValue("LocalSize", null);
            if (localSize == null)
                throw new MapLoadException("Invalid [Map] LocalSize=");
            parts = localSize.Split(',');
            if (parts.Length != 4)
                throw new MapLoadException("Invalid [Map] LocalSize=");

            map.LocalSize = new Rectangle(
                Conversions.IntFromString(parts[0], 0),
                Conversions.IntFromString(parts[1], 0),
                Conversions.IntFromString(parts[2], width),
                Conversions.IntFromString(parts[3], height));

            map.TheaterName = section.GetStringValue("Theater", string.Empty);

            Logger.Log("[Map] section read successfully.");
        }

        public static void ReadIsoMapPack(IMap map, IniFile mapIni)
        {
            Logger.Log("Reading IsoMapPack5.");

            var section = mapIni.GetSection("IsoMapPack5");
            if (section == null)
            {
                map.SetTileData(new List<MapTile>(0));
                return;
            }

            if (section.Keys.Count == 0)
            {
                Logger.Log("[IsoMapPack5] has no data!");
                map.SetTileData(new List<MapTile>(0));
                return;
            }

            StringBuilder sb = new StringBuilder();
            section.Keys.ForEach(kvp => sb.Append(kvp.Value));

            byte[] compressedData = Convert.FromBase64String(sb.ToString());
            if (compressedData.Length < 4)
                throw new InvalidOperationException("Invalid IsoMapPack5 format");

            Logger.Log("IsoMapPack5 CompressedData length: " + compressedData.Length);

            List<byte> uncompressedData = new List<byte>();

            int position = 0;

            while (position < compressedData.Length)
            {
                ushort inputSize = BitConverter.ToUInt16(compressedData, position);
                ushort outputSize = BitConverter.ToUInt16(compressedData, position + 2);

                Logger.Log("Decoding IsoMapPack5 block: pos: " + position + ", inSize: " + inputSize + ", outSize: " + outputSize);

                if (position + inputSize + 4 > compressedData.Length)
                    throw new InvalidOperationException("Error decoding IsoMapPack5");

                byte[] inData = new byte[inputSize];
                Array.Copy(compressedData, position + 4, inData, 0, inputSize);
                byte[] outData = new byte[outputSize];
                MiniLZO.MiniLZO.Decompress(inData, outData);
                uncompressedData.AddRange(outData);

                position += inputSize + 4;
            }

            // if ((uncompressedData.Count % IsoMapPack5Tile.Size) != 4)
            //      throw new InvalidOperationException("Decompressed IsoMapPack5 size does not match expected struct size");

            var tiles = new List<MapTile>(uncompressedData.Count / IsoMapPack5Tile.Size);
            position = 0;
            while (position < uncompressedData.Count - IsoMapPack5Tile.Size)
            {
                var mapTile = new MapTile(uncompressedData.GetRange(position, IsoMapPack5Tile.Size).ToArray());
                if (mapTile.TileIndex == ushort.MaxValue)
                {
                    mapTile.TileIndex = 0;
                }
                tiles.Add(mapTile);
                position += IsoMapPack5Tile.Size;
            }

            map.SetTileData(tiles);

            Logger.Log("IsoMapPack5 read successfully.");
        }

        public static void ReadBasicSection(IMap map, IniFile mapIni)
        {
            Logger.Log("Reading [Basic] section.");

            var section = mapIni.GetSection("Basic");
            if (section == null)
                return;

            map.Basic.ReadPropertiesFromIniSection(section);

            Logger.Log("[Basic] section read successfully.");
        }

        public static void ReadTerrainObjects(IMap map, IniFile mapIni)
        {
            Logger.Log("Reading TerrainObjects.");

            IniSection section = mapIni.GetSection("Terrain");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                string coords = kvp.Key;
                int yLength = coords.Length - 3;
                int y = Conversions.IntFromString(coords.Substring(0, yLength), -1);
                int x = Conversions.IntFromString(coords.Substring(yLength), -1);
                if (y < 0 || x < 0)
                    continue;

                TerrainType terrainType = map.Rules.TerrainTypes.Find(tt => tt.ININame == kvp.Value);
                if (terrainType == null)
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadTerrainObjects.NonexistentTerrainObjectType",
                        "Skipping loading of terrain type {0}, placed at {1}, {2}, because it does not exist in Rules."),
                        kvp.Value, x, y));
                    continue;
                }

                var terrainObject = new TerrainObject(terrainType, new Point2D(x, y));
                var tile = map.GetTile(x, y);
                if (tile == null)
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadTerrainObjects.TerrainObjectOutsideOfMap", 
                        "Terrain object {0} has been placed outside of the valid map area, at {1}, {2}. Skipping placing it on the map."),
                        terrainType.ININame, x, y));
                    continue;
                }

                map.TerrainObjects.Add(terrainObject);
                tile.TerrainObject = terrainObject;
            }

            Logger.Log("TerrainObjects read successfully.");
        }

        private static void FindAttachedTag(IMap map, TechnoBase techno, string attachedTagString)
        {
            if (attachedTagString != Constants.NoneValue1 && attachedTagString != Constants.NoneValue2)
            {
                Tag tag = map.Tags.Find(t => t.ID == attachedTagString);
                if (tag == null)
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.FindAttachedTag.TagNotFound",
                        "Unable to find tag {0} attached to {1} at {2}"),
                        attachedTagString, techno.WhatAmI(), techno.Position));
                    return;
                }

                techno.AttachedTag = tag;
            }
        }

        public static void ReadBuildings(IMap map, IniFile mapIni)
        {
            Logger.Log("Reading Structures.");

            IniSection section = mapIni.GetSection("Structures");
            if (section == null)
                return;

            // [Structures]
            // INDEX=OWNER,ID,HEALTH,X,Y,FACING,TAG,AI_SELLABLE,AI_REBUILDABLE,POWERED_ON,UPGRADES,SPOTLIGHT,UPGRADE_1,UPGRADE_2,UPGRADE_3,AI_REPAIRABLE,NOMINAL

            foreach (var kvp in section.Keys)
            {
                string[] values = kvp.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (values.Length < BUILDING_PROPERTY_FIELD_COUNT)
                    continue;

                string ownerName = values[0];
                string buildingTypeId = values[1];
                int health = Math.Min(Constants.ObjectHealthMax, Math.Max(0, Conversions.IntFromString(values[2], Constants.ObjectHealthMax)));
                int x = Conversions.IntFromString(values[3], 0);
                int y = Conversions.IntFromString(values[4], 0);
                int facing = Math.Min(Constants.FacingMax, Math.Max(0, Conversions.IntFromString(values[5], Constants.FacingMax)));
                string attachedTag = values[6];
                bool aiSellable = Conversions.BooleanFromString(values[7], true);
                // AI_REBUILDABLE is a leftover
                bool powered = Conversions.BooleanFromString(values[9], true);
                int upgradeCount = Conversions.IntFromString(values[10], 0);
                int spotlight = Conversions.IntFromString(values[11], 0);
                string[] upgradeIds = new string[] { values[12], values[13], values[14] };
                bool aiRepairable = Conversions.BooleanFromString(values[15], false);
                bool nominal = Conversions.BooleanFromString(values[16], false);

                var buildingType = map.Rules.BuildingTypes.Find(bt => bt.ININame == buildingTypeId);
                if (buildingType == null)
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadBuildings.BuildingTypeNotFound",
                        "Unable to find building type {0} - skipping adding it to map."), buildingTypeId));
                    continue;
                }

                House owner = map.FindOrMakeHouse(ownerName);
                var building = new Structure(buildingType)
                {
                    HP = health,
                    Position = new Point2D(x, y),
                    Facing = (byte)facing,
                    AISellable = aiSellable,
                    Powered = powered,
                    Spotlight = (SpotlightType)spotlight,
                    AIRepairable = aiRepairable,
                    Nominal = nominal,
                    Owner = map.FindOrMakeHouse(ownerName)
                };

                if (upgradeCount > 0)
                {
                    int appliedUpgrades = 0;

                    for (int i = 0; i < Structure.MaxUpgradeCount; i++)
                    {
                        if (!Helpers.IsStringNoneValue(upgradeIds[i]))
                        {
                            var upgradeBuildingType = map.Rules.BuildingTypes.Find(b => b.ININame == upgradeIds[i]);
                            if (upgradeBuildingType == null)
                            {
                                AddMapLoadError(string.Format(Translate("MapLoader.ReadBuildings.BuildingTypeForUpgradeNotFound", 
                                    "Invalid building upgrade specified for building {0}: {1}"), 
                                    buildingTypeId, upgradeIds[i]));
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(upgradeBuildingType.PowersUpBuilding) || !upgradeBuildingType.PowersUpBuilding.Equals(buildingType.ININame, StringComparison.OrdinalIgnoreCase))
                            {
                                AddMapLoadError(string.Format(Translate("MapLoader.ReadBuildings.UpgradeInvalid", 
                                    "Building {0} has an upgrade {1}, but" + Environment.NewLine + "{2} " +
                                    "does not specify {3} in its PowersUpBuilding= key. Skipping adding upgrade to map."),
                                    buildingTypeId, upgradeBuildingType.ININame, upgradeBuildingType.ININame, buildingTypeId));
                                continue;
                            }

                            if (appliedUpgrades >= buildingType.Upgrades)
                            {
                                AddMapLoadError(string.Format(Translate("MapLoader.ReadBuildings.UpgradeCountMismatch", 
                                    "Building {0} at {1} has more upgrades ({2}) " +
                                    "than specified by its Upgrades= value ({3}) in Rules. Skipping adding one or more of the building's upgrades."),
                                    buildingTypeId, building.Position, appliedUpgrades + 1, buildingType.Upgrades));
                                break;
                            }

                            building.Upgrades[appliedUpgrades] = upgradeBuildingType;
                            appliedUpgrades++;
                        }
                    }
                }

                FindAttachedTag(map, building, attachedTag);

                bool isClear = false;

                void CheckFoundationCell(Point2D cellCoords)
                {
                    var tile = map.GetTile(cellCoords);
                    if (tile != null)
                    {
                        isClear = true;
                        tile.Structures.Add(building);
                    }
                }

                // Go through foundation cells and register the building to all tiles that are valid on its foundation.
                // If the building was added to no cells (all cells on its foundation were nonexistent),
                // then the building is outside of the map and we should not consider it as belonging to the map.
                buildingType.ArtConfig.DoForFoundationCoordsOrOrigin(offset => CheckFoundationCell(building.Position + offset));

                if (!isClear)
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.CheckFoundationCell.TileOutOfBounds",
                        "Building {0} has been placed outside of the map at {1}. Skipping adding it to map."),
                        buildingType.ININame, building.Position));
                    continue;
                }

                map.Structures.Add(building);

                building.LightTiles(map.Tiles);
            }

            map.Structures.ForEach(s => s.UpdatePowerUpAnims());

            Logger.Log("Structures read successfully.");
        }

        public static void ReadAircraft(IMap map, IniFile mapIni)
        {
            Logger.Log("Reading Aircraft.");

            IniSection section = mapIni.GetSection("Aircraft");
            if (section == null)
                return;

            // [Aircraft]
            // INDEX=OWNER,ID,HEALTH,X,Y,FACING,MISSION,TAG,VETERANCY,GROUP,AUTOCREATE_NO_RECRUITABLE,AUTOCREATE_YES_RECRUITABLE

            foreach (var kvp in section.Keys)
            {
                string[] values = kvp.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (values.Length < AIRCRAFT_PROPERTY_FIELD_COUNT)
                    continue;

                string ownerName = values[0];
                string aircraftTypeId = values[1];
                int health = Math.Min(Constants.ObjectHealthMax, Math.Max(0, Conversions.IntFromString(values[2], Constants.ObjectHealthMax)));
                int x = Conversions.IntFromString(values[3], 0);
                int y = Conversions.IntFromString(values[4], 0);
                int facing = Math.Min(Constants.FacingMax, Math.Max(0, Conversions.IntFromString(values[5], Constants.FacingMax)));
                string mission = values[6];
                string attachedTag = values[7];
                int veterancy = Conversions.IntFromString(values[8], 0);
                int group = Conversions.IntFromString(values[9], 0);
                bool autocreateNoRecruitable = Conversions.BooleanFromString(values[10], false);
                bool autocreateYesRecruitable = Conversions.BooleanFromString(values[11], false);

                var aircraftType = map.Rules.AircraftTypes.Find(ut => ut.ININame == aircraftTypeId);
                if (aircraftType == null)
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadAircraft.AircraftTypeNotFound", 
                        "Unable to find aircraft type {0} - skipping adding it to map."), aircraftTypeId));
                    continue;
                }

                var aircraft = new Aircraft(aircraftType)
                {
                    HP = health,
                    Position = new Point2D(x, y),
                    Facing = (byte)facing,
                    Mission = mission,
                    Veterancy = veterancy,
                    Group = group,
                    AutocreateNoRecruitable = autocreateNoRecruitable,
                    AutocreateYesRecruitable = autocreateYesRecruitable,
                    Owner = map.FindOrMakeHouse(ownerName)
                };

                FindAttachedTag(map, aircraft, attachedTag);

                map.Aircraft.Add(aircraft);
                var tile = map.GetTile(x, y);
                if (tile != null)
                    tile.Aircraft.Add(aircraft);
            }

            Logger.Log("Aircraft read successfully.");
        }

        public static void ReadUnits(IMap map, IniFile mapIni)
        {
            Logger.Log("Reading Units.");

            IniSection section = mapIni.GetSection("Units");
            if (section == null)
                return;

            // [Units]
            // INDEX=OWNER,ID,HEALTH,X,Y,FACING,MISSION,TAG,VETERANCY,GROUP,HIGH,FOLLOWS_INDEX,AUTOCREATE_NO_RECRUITABLE,AUTOCREATE_YES_RECRUITABLE

            foreach (var kvp in section.Keys)
            {
                string[] values = kvp.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (values.Length < UNIT_PROPERTY_FIELD_COUNT)
                    continue;

                string ownerName = values[0];
                string unitTypeId = values[1];
                int health = Math.Min(Constants.ObjectHealthMax, Math.Max(0, Conversions.IntFromString(values[2], Constants.ObjectHealthMax)));
                int x = Conversions.IntFromString(values[3], 0);
                int y = Conversions.IntFromString(values[4], 0);
                int facing = Math.Min(Constants.FacingMax, Math.Max(0, Conversions.IntFromString(values[5], Constants.FacingMax)));
                string mission = values[6];
                string attachedTag = values[7];
                int veterancy = Conversions.IntFromString(values[8], 0);
                int group = Conversions.IntFromString(values[9], 0);
                bool high = Conversions.BooleanFromString(values[10], false);
                int followsIndex = Conversions.IntFromString(values[11], 0);
                bool autocreateNoRecruitable = Conversions.BooleanFromString(values[12], false);
                bool autocreateYesRecruitable = Conversions.BooleanFromString(values[13], false);

                var unitType = map.Rules.UnitTypes.Find(ut => ut.ININame == unitTypeId);
                if (unitType == null)
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadUnits.UnitTypeNotFound",
                        "Unable to find unit type {0} - skipping adding it to map."), unitTypeId));
                    continue;
                }

                var unit = new Unit(unitType)
                {
                    HP = health,
                    Position = new Point2D(x, y),
                    Facing = (byte)facing,
                    Mission = mission,
                    Veterancy = veterancy,
                    Group = group,
                    High = high,
                    FollowerID = followsIndex,
                    AutocreateNoRecruitable = autocreateNoRecruitable,
                    AutocreateYesRecruitable = autocreateYesRecruitable,
                    Owner = map.FindOrMakeHouse(ownerName)
                };

                FindAttachedTag(map, unit, attachedTag);

                map.Units.Add(unit);
                var tile = map.GetTile(x, y);
                if (tile != null)
                    tile.Vehicles.Add(unit);
            }

            // Process follow IDs
            foreach (var unit in map.Units)
            {
                if (unit.FollowerID < 0 || unit.FollowerID >= map.Units.Count)
                    continue;

                unit.FollowerUnit = map.Units[unit.FollowerID];
            }

            Logger.Log("Units read successfully.");
        }

        public static void ReadInfantry(IMap map, IniFile mapIni)
        {
            Logger.Log("Reading Infantry.");

            IniSection section = mapIni.GetSection("Infantry");
            if (section == null)
                return;

            // [Infantry]
            // INDEX=OWNER,ID,HEALTH,X,Y,SUB_CELL,MISSION,FACING,TAG,VETERANCY,GROUP,HIGH,AUTOCREATE_NO_RECRUITABLE,AUTOCREATE_YES_RECRUITABLE

            foreach (var kvp in section.Keys)
            {
                string[] values = kvp.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (values.Length < INFANTRY_PROPERTY_FIELD_COUNT)
                    continue;

                string ownerName = values[0];
                string infantryTypeId = values[1];
                int health = Math.Min(Constants.ObjectHealthMax, Math.Max(0, Conversions.IntFromString(values[2], Constants.ObjectHealthMax)));
                int x = Conversions.IntFromString(values[3], 0);
                int y = Conversions.IntFromString(values[4], 0);
                SubCell subCell = (SubCell)Conversions.IntFromString(values[5], 0);
                string mission = values[6];
                int facing = Math.Min(Constants.FacingMax, Math.Max(0, Conversions.IntFromString(values[7], Constants.FacingMax)));
                string attachedTag = values[8];
                int veterancy = Conversions.IntFromString(values[9], 0);
                int group = Conversions.IntFromString(values[10], 0);
                bool high = Conversions.BooleanFromString(values[11], false);
                bool autocreateNoRecruitable = Conversions.BooleanFromString(values[12], false);
                bool autocreateYesRecruitable = Conversions.BooleanFromString(values[13], false);

                var infantryType = map.Rules.InfantryTypes.Find(it => it.ININame == infantryTypeId);
                if (infantryType == null)
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadInfantry.InfantryTypeNotFound",
                        "Unable to find infantry type {0} - skipping adding it to map."), infantryTypeId));
                    continue;
                }

                var infantry = new Infantry(infantryType)
                {
                    HP = health,
                    Position = new Point2D(x, y), // TODO handle sub-cell in position?
                    Facing = (byte)facing,
                    Veterancy = veterancy,
                    Group = group,
                    High = high,
                    AutocreateNoRecruitable = autocreateNoRecruitable,
                    AutocreateYesRecruitable = autocreateYesRecruitable,
                    SubCell = subCell,
                    Mission = mission,
                    Owner = map.FindOrMakeHouse(ownerName)
                };

                FindAttachedTag(map, infantry, attachedTag);

                map.Infantry.Add(infantry);
                var tile = map.GetTile(x, y);
                if (tile != null)
                    tile.Infantry[(int)subCell] = infantry;
            }

            Logger.Log("Infantry read successfully.");
        }

        public static void ReadSmudges(IMap map, IniFile mapIni)
        {
            Logger.Log("Reading Smudges.");

            var smudgesSection = mapIni.GetSection("Smudge");
            if (smudgesSection == null)
                return;

            foreach (var kvp in smudgesSection.Keys)
            {
                string[] values = kvp.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (values.Length < 3)
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadSmudges.InvalidSmudgeSyntax",
                        "Invalid syntax in smudge defined in map: {0}"), kvp.Value));
                    continue;
                }

                string smudgeTypeId = values[0];
                int x = Conversions.IntFromString(values[1], -1);
                int y = Conversions.IntFromString(values[2], -1);
                if (values.Length > 3 && values[3] != "0")
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadSmudges.InvalidSmudgeSyntaxAtPosition", 
                        "Invalid syntax in smudge at {0},{1}: {2}"), x, y, kvp.Value));
                    continue;
                }

                var smudgeType = map.Rules.SmudgeTypes.Find(st => st.ININame == smudgeTypeId);
                if (smudgeType == null)
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadSmudges.SmudgeTypeNotFound", 
                        "Cell at {0},{1} contains a smudge '{2}' that does not exist in Rules.ini. Ignoring it."), x, y, smudgeTypeId));
                    continue;
                }

                var cell = map.GetTile(x, y);
                if (cell == null)
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadSmudges.SmudgeOutOfBounds", 
                        "Smudge at {0},{1} is placed outside of the map. Ignoring it."), x, y));
                    continue;
                }

                cell.Smudge = new Smudge() { SmudgeType = smudgeType, Position = new Point2D(x, y) };
            }

            Logger.Log("Smudges read successfully.");
        }

        public static void ReadOverlays(IMap map, IniFile mapIni)
        {
            Logger.Log("Reading Overlays (OverlayPack and OverlayDataPack).");

            var overlayPackSection = mapIni.GetSection("OverlayPack");
            var overlayDataPackSection = mapIni.GetSection("OverlayDataPack");
            if (overlayPackSection == null || overlayDataPackSection == null)
                return;

            bool needsExtendedOverlayPack = map.Basic.NewINIFormat >= 5;

            var stringBuilder = new StringBuilder();
            overlayPackSection.Keys.ForEach(kvp => stringBuilder.Append(kvp.Value));
            byte[] compressedData = Convert.FromBase64String(stringBuilder.ToString());
            byte[] uncompressedOverlayPack = new byte[Constants.MAX_MAP_LENGTH_IN_DIMENSION * Constants.MAX_MAP_LENGTH_IN_DIMENSION * (needsExtendedOverlayPack ? 2 : 1)];
            Format5.DecodeInto(compressedData, uncompressedOverlayPack, Constants.OverlayPackFormat);

            stringBuilder.Clear();
            overlayDataPackSection.Keys.ForEach(kvp => stringBuilder.Append(kvp.Value));
            compressedData = Convert.FromBase64String(stringBuilder.ToString());
            byte[] uncompressedOverlayDataPack = new byte[Constants.MAX_MAP_LENGTH_IN_DIMENSION * Constants.MAX_MAP_LENGTH_IN_DIMENSION];
            Format5.DecodeInto(compressedData, uncompressedOverlayDataPack, Constants.OverlayPackFormat);

            for (int y = 0; y < map.Tiles.Length; y++)
            {
                for (int x = 0; x < map.Tiles[y].Length; x++)
                {
                    var tile = map.Tiles[y][x];
                    if (tile == null)
                        continue;

                    int overlayDataIndex = (tile.Y * Constants.MAX_MAP_LENGTH_IN_DIMENSION) + tile.X;

                    int overlayTypeIndex;
                    if (needsExtendedOverlayPack)
                    {
                        ushort index = BinaryPrimitives.ReadUInt16LittleEndian(new ReadOnlySpan<byte>(uncompressedOverlayPack, overlayDataIndex * 2, 2));
                        overlayTypeIndex = index != ushort.MaxValue ? index : Constants.NO_OVERLAY;
                    }
                    else
                    {
                        byte index = uncompressedOverlayPack[overlayDataIndex];
                        overlayTypeIndex = index != byte.MaxValue ? index : Constants.NO_OVERLAY;
                    }

                    if (overlayTypeIndex == Constants.NO_OVERLAY)
                        continue;

                    if (overlayTypeIndex >= map.Rules.OverlayTypes.Count)
                    {                        
                        AddMapLoadError(string.Format(Translate("MapLoader.ReadOverlays.OverlayIndexOutOfBounds",
                            "Ignoring overlay on tile at {0},{1} because it's out of bounds compared to Rules.ini overlay list"), x, y));
                        continue;
                    }

                    var overlayType = map.Rules.OverlayTypes[overlayTypeIndex];
                    var overlay = new Overlay()
                    {
                        OverlayType = overlayType,
                        FrameIndex = uncompressedOverlayDataPack[overlayDataIndex],
                        Position = new Point2D(tile.X, tile.Y)
                    };
                    tile.Overlay = overlay;
                }
            }

            Logger.Log("Overlays read successfully.");
        }

        public static void ReadWaypoints(IMap map, IniFile mapIni)
        {
            Logger.Log("Reading Waypoints.");

            var waypointsSection = mapIni.GetSection("Waypoints");
            if (waypointsSection == null)
                return;

            foreach (var kvp in waypointsSection.Keys)
            {
                var waypoint = Waypoint.ParseWaypoint(kvp.Key, kvp.Value);
                if (waypoint == null)
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadWaypoints.InvalidWaypointSyntax", 
                        "Invalid syntax encountered for waypoint: {0}={1}"), kvp.Key, kvp.Value));
                    continue;
                }

                var tile = map.GetTile(waypoint.Position.X, waypoint.Position.Y);
                if (tile == null)
                {
                    Point2D oldPosition = waypoint.Position;

                    // Find new cell to move waypoint to
                    // Lazy and inefficient implementation, but waypoints outside the map aren't common
                    int lowestDistance = int.MaxValue;
                    Point2D nearestCell = Point2D.NegativeOne;
                    map.DoForAllValidTiles(cell =>
                    {
                        int distance = cell.CoordsToPoint().DistanceTo(waypoint.Position);
                        if (distance < lowestDistance)
                        {
                            lowestDistance = distance;
                            nearestCell = cell.CoordsToPoint();
                        }
                    });

                    waypoint.Position = nearestCell;
                    tile = map.GetTile(waypoint.Position);

                    AddMapLoadError(string.Format(Translate("MapLoader.ReadWaypoints.WaypointOutOfBounds", 
                        "Waypoint {0} at {1} was not within the valid map area. It has been moved to {2}."),
                            waypoint.Identifier, oldPosition, waypoint.Position));
                }

                if (tile.Waypoints.Count > 0)
                {
                    Logger.Log($"NOTE: Waypoint {waypoint.Identifier} exists in the cell at {waypoint.Position} that already contains other waypoints: {string.Join(", ", tile.Waypoints.Select(s => s.Identifier))}");
                }

                waypoint.ParseEditorInfo(mapIni);

                map.AddWaypoint(waypoint);
            }

            Logger.Log("Waypoints read successfully.");
        }

        public static void ReadTaskForces(IMap map, IniFile mapIni)
        {
            Logger.Log("Reading TaskForces.");

            map.TaskForces.ReadTaskForces(mapIni, map.Rules, AddMapLoadError);

            Logger.Log("TaskForces read successfully.");
        }

        public static void ReadTriggers(IMap map, IniFile mapIni)
        {
            Logger.Log("Reading Triggers (Triggers, Events and Actions).");

            var section = mapIni.GetSection("Triggers");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key) || string.IsNullOrWhiteSpace(kvp.Value))
                    continue;

                var trigger = Trigger.ParseTrigger(kvp.Key, kvp.Value);
                if (trigger != null)
                    map.AddTrigger(trigger);

                string actionData = mapIni.GetStringValue("Actions", trigger.ID, null);
                trigger.ParseActions(actionData);

                string conditionData = mapIni.GetStringValue("Events", trigger.ID, null);
                trigger.ParseConditions(conditionData, map.EditorConfig);

                trigger.ParseEditorInfo(mapIni);
            }

            // Parse and apply linked triggers
            foreach (var trigger in map.Triggers)
            {
                if (Helpers.IsStringNoneValue(trigger.LinkedTriggerId))
                    continue;

                trigger.LinkedTrigger = map.Triggers.Find(otherTrigger => otherTrigger.ID == trigger.LinkedTriggerId);
            }

            Logger.Log("Triggers read successfully.");

            TriggerFix(map);
        }

        /// <summary>
        /// Checks for triggers having invalid values for uncustomizable parameters.
        /// Some earlier versions of WAE could set these parameters wrong, so we are
        /// cleaning up any potential mess we caused.
        /// </summary>
        private static void TriggerFix(IMap map)
        {
            Logger.Log("Checking for bugged triggers.");

            // Check for mismatched uncustomizable trigger action parameters
            foreach (var trigger in map.Triggers)
            {
                foreach (var action in trigger.Actions)
                {
                    if (!map.EditorConfig.TriggerActionTypes.TryGetValue(action.ActionIndex, out var triggerActionType))
                        continue;

                    for (int i = 0; i < triggerActionType.Parameters.Length - 1; i++)
                    {
                        var paramType = triggerActionType.Parameters[i].TriggerParamType;

                        string valueToSet = null;

                        if ((int)paramType < 0 && Conversions.IntFromString(action.Parameters[i], 0) != Math.Abs((int)paramType))
                        {
                            valueToSet = Math.Abs((int)paramType).ToString(CultureInfo.InvariantCulture);
                        }

                        if (paramType == TriggerParamType.Unused && action.Parameters[i] != "0")
                        {
                            valueToSet = "0";
                        }

                        if (valueToSet != null)
                        {
                            AddMapLoadError(string.Format(Translate("MapLoader.TriggerFix.InvalidTriggerAction",
                                "Trigger \"{0}\" had action \"{1}\" with invalid value for uncustomizable " +
                                    "parameter #{2}: \"{3}\". It has been automatically corrected to \"{4}\"."),
                                    trigger.Name, triggerActionType.Name, i, action.Parameters[i], valueToSet));
                            action.Parameters[i] = valueToSet;
                        }
                    }

                    const int lastParamIndex = TriggerActionType.MAX_PARAM_COUNT - 1;
                    // Handle P7 separately due to WaypointZZ hardcoding
                    if (triggerActionType.Parameters[lastParamIndex].TriggerParamType == TriggerParamType.Unused && action.Parameters[lastParamIndex] != "A")
                    {
                        string valueToSet = "A";
                        AddMapLoadError(string.Format(Translate("MapLoader.TriggerFix.InvalidTriggerAction",
                            "Trigger \"{0}\" had action \"{1}\" with invalid value for uncustomizable parameter #{2}: \"{3}\". " +
                                "It has been automatically corrected to \"{4}\"."),
                                trigger.Name, triggerActionType.Name, lastParamIndex, action.Parameters[lastParamIndex], valueToSet));
                        action.Parameters[lastParamIndex] = valueToSet;
                    }
                }
            }

            Logger.Log("Checking for bugged triggers completed.");
        }

        public static void ReadTags(IMap map, IniFile mapIni)
        {
            Logger.Log("Reading Tags.");

            var section = mapIni.GetSection("Tags");
            if (section == null)
                return;

            // [Tags]
            // ID=REPEATING,NAME,TRIGGER_ID

            foreach (var kvp in section.Keys)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key) || string.IsNullOrWhiteSpace(kvp.Value))
                    continue;

                string[] parts = kvp.Value.Split(',');
                if (parts.Length != 3)
                    continue;

                int repeating = Conversions.IntFromString(parts[0], -1);
                if (repeating < 0 || repeating > Tag.REPEAT_TYPE_MAX)
                    continue;

                string triggerId = parts[2];
                Trigger trigger = map.Triggers.Find(t => t.ID == triggerId);
                if (trigger == null)
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadTags.TriggerNotFound",
                        "Ignoring tag {0} because its related trigger {1} does not exist!"),
                            kvp.Key, triggerId));
                    continue;
                }

                var tag = new Tag() { ID = kvp.Key, Repeating = repeating, Name = parts[1], Trigger = trigger };
                map.AddTag(tag);
            }

            Logger.Log("Tags read successfully.");
        }

        public static void ReadScripts(IMap map, IniFile mapIni)
        {
            Logger.Log("Reading ScriptTypes.");

            map.Scripts.ReadScripts(mapIni, AddMapLoadError);

            Logger.Log("ScriptTypes read successfully.");
        }

        public static void ReadTeamTypes(IMap map, IniFile mapIni, List<TeamTypeFlag> teamTypeFlags)
        {
            Logger.Log("Reading TeamTypes.");

            map.TeamTypes.ReadTeamTypes(mapIni,
                name => map.FindHouseType(name),
                name => map.Scripts.Concat(map.Rules.Scripts).FirstOrDefault(s => s.ININame == name),
                name => map.TaskForces.Concat(map.Rules.TaskForces).FirstOrDefault(tf => tf.ININame == name),
                name => map.Tags.Find(t => t.ID == name),
                teamTypeFlags,
                AddMapLoadError,
                false);

            Logger.Log("TeamTypes read successfully.");
        }

        public static void ReadAITriggerTypes(IMap map, IniFile mapIni)
        {
            Logger.Log("Reading AITriggerTypes.");

            var section = mapIni.GetSection("AITriggerTypes");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key) || string.IsNullOrWhiteSpace(kvp.Value))
                    continue;

                string[] parts = kvp.Value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != AI_TRIGGER_PROPERTY_FIELD_COUNT)
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadAITriggerTypes.InvalidAITriggerType",
                        "AITriggerType {0} is invalid, skipping reading it."), kvp.Key));
                    continue;
                }

                var aiTriggerType = new AITriggerType(kvp.Key);

                aiTriggerType.Name = parts[0];
                aiTriggerType.PrimaryTeam = map.TeamTypes.Concat(map.Rules.TeamTypes).FirstOrDefault(tt => tt.ININame == parts[1]);

                if (aiTriggerType.PrimaryTeam == null)
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadAITriggerTypes.PrimaryTeamNotFound", 
                        "AITriggerType \"{0}\" ({1}) has a nonexistent team ({2}) specified as its primary team!"),
                            aiTriggerType.Name, kvp.Key, parts[1]));
                }

                aiTriggerType.OwnerName = parts[2];

                if (!int.TryParse(parts[3], CultureInfo.InvariantCulture, out int techLevel))
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadAITriggerTypes.InvalidTechLevel", 
                        "AITriggerType {0} has an invalid tech level, skipping parsing of the AI trigger."), kvp.Key));
                    continue;
                }
                aiTriggerType.TechLevel = techLevel;

                if (!int.TryParse(parts[4], CultureInfo.InvariantCulture, out int conditionType))
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadAITriggerTypes.InvalidConditionType", 
                        "AITriggerType {0} has an invalid condition type, skipping parsing of the AI trigger."), kvp.Key));
                    continue;
                }

                aiTriggerType.ConditionType = (AITriggerConditionType)conditionType;

                if (!Helpers.IsStringNoneValue(parts[5]))
                {
                    TechnoType conditionObject = map.Rules.FindTechnoType(parts[5]);

                    if (conditionObject == null)
                    {
                        AddMapLoadError(string.Format(Translate("MapLoader.ReadAITriggerTypes.ConditionObjectNotFound", 
                            "AITriggerType {0} has a non-existent condition object \"{1}\""), kvp.Key, parts[5]));
                    }

                    aiTriggerType.ConditionObject = conditionObject;
                }

                aiTriggerType.LoadedComparatorString = parts[6];
                AITriggerComparator? comparator = AITriggerComparator.Parse(aiTriggerType.LoadedComparatorString);
                if (comparator == null)
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadAITriggerTypes.ConditionComparatorParseError", 
                        "Failed to parse comparator of AITriggerType {0} ({1})! Skipping loading of the AI trigger."),
                        kvp.Key, aiTriggerType.Name));
                    continue;
                }
                aiTriggerType.Comparator = comparator.Value;

                aiTriggerType.InitialWeight = Conversions.DoubleFromString(parts[7], 0.0);
                aiTriggerType.MinimumWeight = Conversions.DoubleFromString(parts[8], 0.0);
                aiTriggerType.MaximumWeight = Conversions.DoubleFromString(parts[9], 0.0);
                aiTriggerType.EnabledInMultiplayer = parts[10] != "0";
                aiTriggerType.Unused = parts[11] != "0";
                aiTriggerType.Side = Conversions.IntFromString(parts[12], 0);
                aiTriggerType.IsBaseDefense = parts[13] != "0";

                if (!Helpers.IsStringNoneValue(parts[14]) )
                {
                    aiTriggerType.SecondaryTeam = map.TeamTypes.Concat(map.Rules.TeamTypes).FirstOrDefault(tt => tt.ININame == parts[14]);

                    if (aiTriggerType.SecondaryTeam == null)
                    {
                        AddMapLoadError(string.Format(Translate("MapLoader.ReadAITriggerTypes.SecondaryTeamNotFound",
                            "AITriggerType {0} has a non-existent secondary team type \"{1}\""),
                            kvp.Key, parts[14]));
                    }
                }

                aiTriggerType.Easy = parts[15] != "0";
                aiTriggerType.Medium = parts[16] != "0";
                aiTriggerType.Hard = parts[17] != "0";

                map.AITriggerTypes.Add(aiTriggerType);
            }

            Logger.Log("AITriggerTypes read successfully.");

            section = mapIni.GetSection("AITriggerTypesEnable");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                string aiTriggerININame = kvp.Key;

                var aiTrigger = map.AITriggerTypes.Find(aiTriggerType => aiTriggerType.ININame == aiTriggerININame);
                if (aiTrigger == null)
                    continue;

                aiTrigger.Enabled = Conversions.BooleanFromString(kvp.Value, aiTrigger.Enabled);
            }

            Logger.Log("AITriggerTypesEnable read successfully.");
        }

        public static void ReadHouseTypes(IMap map, IniFile mapIni)
        {
            Logger.Log("Reading HouseTypes. Using countries: " + Constants.IsRA2YR);

            var section = mapIni.GetSection(Constants.IsRA2YR ? "Countries" : "Houses");
            if (section == null)
                return;

            int id = 0;
            foreach (var kvp in section.Keys)
            {
                IniSection houseTypeSection = mapIni.GetSection(kvp.Value);

                // HouseTypes can't be redefined, so check if the HouseType already exists.
                // If it does, in Tiberian Sun we can skip it.
                // In RA2/YR we need to search for the HouseType from Rules as well as the map itself.
                // In case it exists in either, we still need to read the HouseType's properties,
                // but skip adding the HouseType to the list of map-specific HouseTypes.
                if (Constants.IsRA2YR)
                {
                    var existingHouseType = map.FindHouseType(kvp.Value);
                    if (existingHouseType != null)
                    {
                        if (houseTypeSection != null)
                        {
                            existingHouseType.ReadFromIniSection(houseTypeSection);
                            existingHouseType.ModifiedInMap = true;
                        }

                        continue;
                    }
                }
                else
                {
                    if (map.HouseTypes.Exists(ht => ht.ININame == kvp.Value))
                        continue;
                }

                var houseType = new HouseType(kvp.Value);
                houseType.Index = id + (Constants.IsRA2YR ? map.Rules.RulesHouseTypes.Count : 0);
                id++;

                if (houseTypeSection != null)
                    houseType.ReadFromIniSection(houseTypeSection);

                map.HouseTypes.Add(houseType);
            }

            // Assign colors
            map.GetHouseTypes().ForEach(houseType =>
            {
                var color = map.Rules.Colors.Find(c => c.Name == houseType.Color);
                if (color == null)
                {
                    houseType.XNAColor = Color.White;
                }
                else
                {
                    houseType.XNAColor = color.XNAColor;
                }
            });

            Logger.Log("HouseTypes read successfully.");
        }

        public static void ReadHouses(IMap map, IniFile mapIni)
        {
            Logger.Log("Reading Houses.");

            var section = mapIni.GetSection("Houses");
            if (section == null)
                return;

            int id = 0;
            foreach (var kvp in section.Keys)
            {
                string houseName = kvp.Value;
                HouseType houseType = null;

                var house = new House(houseName);
                house.ID = id;
                id++;

                map.Houses.Add(house);

                var houseSection = mapIni.GetSection(houseName);
                if (houseSection != null)
                {
                    house.ReadFromIniSection(houseSection);
                    var color = map.Rules.Colors.Find(c => c.Name == house.Color);
                    if (color == null)
                    {
                        house.XNAColor = Color.Black;
                    }
                    else
                    {
                        house.XNAColor = color.XNAColor;
                    }
                }

                var invalidBaseNodes = house.BaseNodes.FindAll(bn => !map.Rules.BuildingTypes.Exists(bt => bt.ININame == bn.StructureTypeName));
                invalidBaseNodes.ForEach(bn =>
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadHouses.InvalidBaseNode", 
                        "Skipping loading invalid base node of house {0} for building type \"{1}\". The building type does not exist in Rules!"),
                            houseName, bn.StructureTypeName));
                    house.BaseNodes.Remove(bn);
                });

                if (Constants.IsRA2YR)
                {
                    if (house.Country != null)
                        houseType = map.FindHouseType(house.Country);

                    if (houseType == null)
                    {
                        houseType = map.GetHouseTypes()[0];
                        AddMapLoadError(string.Format(Translate("MapLoader.ReadHouses.InvalidCountry", 
                            "Nonexistent Country= or no Country= specified for House {0}. This makes it default to the first standard Country ({1})."),
                                houseName, houseType.ININame));
                    }
                }
                else
                {
                    houseType = map.HouseTypes[house.ID];
                }

                house.HouseType = houseType;
                if (houseType != null)
                {
                    houseType.Color = house.Color;
                    houseType.XNAColor = house.XNAColor;
                }
            }
            
            // Once all houses were loaded into WAE, read and set up the alliances of each house as a list of allied houses
            foreach (var house in map.Houses)
            {
                var houseSection = mapIni.GetSection(house.ININame);
                if (houseSection != null)
                {
                    List<string> allyHouseNames = houseSection.GetListValue("Allies", ',', s => s);
                    foreach (string allyHouseName in allyHouseNames)
                    {
                        var alliedHouse = map.Houses.Find(house => house.ININame == allyHouseName);
                        if (alliedHouse == null)
                        {
                            AddMapLoadError(string.Format(Translate("MapLoader.ReadHouses.NonexistentAlliedHouse", 
                                "House with name {0} was not found when loading up allies for the house {1}. Skipping the alliance."),
                                    allyHouseName, house.ININame));
                            continue;
                        }

                        house.Allies.Add(alliedHouse);
                    }
                }
            }

            Logger.Log("Houses read successfully.");
        }

        public static void ReadCellTags(IMap map, IniFile mapIni)
        {
            Logger.Log("Reading CellTags.");

            var section = mapIni.GetSection("CellTags");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                Point2D? coords = Helpers.CoordStringToPoint(kvp.Key);
                if (coords == null)
                    continue;

                var tile = map.GetTile(coords.Value.X, coords.Value.Y);
                if (tile == null)
                    continue;

                Tag tag = map.Tags.Find(t => t.ID == kvp.Value);
                if (tag == null)
                    continue;

                map.AddCellTag(new CellTag(coords.Value, tag));
            }

            Logger.Log("CellTags read successfully.");
        }

        public static void ReadLocalVariables(IMap map, IniFile mapIni)
        {
            Logger.Log("Reading local variables (VariableNames).");

            var section = mapIni.GetSection("VariableNames");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                if (!int.TryParse(kvp.Key, out int variableIndex))
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadLocalVariables.InvalidIndex", 
                        "Invalid local variable index in entry {0}: {1}, skipping reading local variable."), kvp.Key, kvp.Value));
                    continue;
                }

                if (map.LocalVariables.Exists(c => c.Index == variableIndex))
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadLocalVariables.DuplicateIndex", 
                        "Duplicate local variable index in entry {0}: {1}, skipping reading local variable."), kvp.Key, kvp.Value));
                    continue;
                }

                string[] parts = kvp.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2)
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadLocalVariables.InvalidSyntax", 
                        "Invalid local variable syntax in entry {0}: {1}, skipping reading local variable."), kvp.Key, kvp.Value));
                    continue;
                }

                var localVariable = new LocalVariable(variableIndex);
                localVariable.Name = parts[0];
                localVariable.InitialState = int.Parse(parts[1], CultureInfo.InvariantCulture);

                map.LocalVariables.Add(localVariable);
            }

            Logger.Log("Local variables read successfully.");
        }

        public static void ReadTubes(IMap map, IniFile mapIni)
        {
            Logger.Log("Reading Tubes.");

            var section = mapIni.GetSection("Tubes");
            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                // Index=ENTER_X,ENTER_Y,FACING,EXIT_X,EXIT_Y,DIRECTIONS

                string[] parts = kvp.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 6)
                    return;

                int enterX = Conversions.IntFromString(parts[0], -1);
                int enterY = Conversions.IntFromString(parts[1], -1);
                TubeDirection initialFacing = (TubeDirection)Conversions.IntFromString(parts[2], -1);
                int exitX = Conversions.IntFromString(parts[3], -1);
                int exitY = Conversions.IntFromString(parts[4], -1);
                var directions = new List<TubeDirection>();
                for (int i = 5; i < parts.Length; i++)
                {
                    directions.Add((TubeDirection)Conversions.IntFromString(parts[i], -1));
                }

                if (enterX < 1 || enterY < 1 || exitX < 1 || exitY < 1 || (int)initialFacing < -1 || initialFacing > TubeDirection.Max)
                {
                    AddMapLoadError(string.Format(Translate("MapLoader.ReadTubes.InvalidTube", 
                        "Invalid tube entry: {0}"), kvp.Value));
                    continue;
                }

                var tube = new Tube(new Point2D(enterX, enterY), new Point2D(exitX, exitY), initialFacing, directions);
                map.Tubes.Add(tube);
            }

            Logger.Log("Tubes read successfully.");
        }
    }
}
