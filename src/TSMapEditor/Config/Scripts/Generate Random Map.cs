/*
Generate Random Map for World-Altering Editor
SPDX-License-Identifier: GPL-3.0-or-later

Includes material adapted from OpenTS's reconstruction of Tiberian Sun
(revision dbfbfbf), derived in part from Electronic Arts' Red Alert release.
Copyright 2025 Electronic Arts Inc.
Copyright 2026 OpenTS contributors.
Modified for World-Altering Editor. Adapted source files include mapgen.cpp,
map.cpp, random.cpp, and priority.h.

Source: https://github.com/OpenTS-Developers/TibSun/tree/dbfbfbf/code
License: https://github.com/OpenTS-Developers/TibSun/blob/dbfbfbf/LICENSE.md
Seed parity source: https://github.com/OpenTS-Developers/OpenTS/tree/42db31be0610/code
License: https://github.com/OpenTS-Developers/OpenTS/blob/42db31be0610/LICENSE.md
For the GNU GPL, version 3 or later, see the accompanying WAE LICENSE.txt.

ADDITIONAL TERMS per GNU GPL Section 7

No trademark or publicity rights are granted. This license does NOT give you
any right, title or interest in "Command & Conquer" or any other Electronic Arts
trademark. You may not distribute any modification of this program using any
Electronic Arts trademark or claim any affiliation or association with
Electronic Arts Inc. or its affiliates or their employees.

Any propagation or conveyance of this program must include this copyright
notice and these terms.

If you convey this program (or any modifications of it) and assume
contractual liability for the program to recipients of it, you agree to
indemnify Electronic Arts for any liability that those contractual
assumptions impose on Electronic Arts.

You may not misrepresent the origins of this program; modified versions of
the program must be marked as such and not identified as the original program.

This disclaimer supplements the one included in the General Public License.
TO THE FULLEST EXTENT PERMISSIBLE UNDER APPLICABLE LAW, THIS PROGRAM IS
PROVIDED TO YOU "AS IS," WITH ALL FAULTS, WITHOUT WARRANTY OF ANY KIND, AND
YOUR USE IS AT YOUR SOLE RISK. THE ENTIRE RISK OF SATISFACTORY QUALITY AND
PERFORMANCE RESIDES WITH YOU. ELECTRONIC ARTS DISCLAIMS ANY AND ALL EXPRESS,
IMPLIED OR STATUTORY WARRANTIES, INCLUDING IMPLIED WARRANTIES OF
MERCHANTABILITY, SATISFACTORY QUALITY, FITNESS FOR A PARTICULAR PURPOSE,
NONINFRINGEMENT OF THIRD PARTY RIGHTS, AND WARRANTIES (IF ANY) ARISING FROM A
COURSE OF DEALING, USAGE, OR TRADE PRACTICE. ELECTRONIC ARTS DOES NOT WARRANT
AGAINST INTERFERENCE WITH YOUR ENJOYMENT OF THE PROGRAM; THAT THE PROGRAM WILL
MEET YOUR REQUIREMENTS; THAT OPERATION OF THE PROGRAM WILL BE UNINTERRUPTED OR
ERROR-FREE, OR THAT THE PROGRAM WILL BE COMPATIBLE WITH THIRD PARTY SOFTWARE
OR THAT ANY ERRORS IN THE PROGRAM WILL BE CORRECTED. NO ORAL OR WRITTEN ADVICE
PROVIDED BY ELECTRONIC ARTS OR ANY AUTHORIZED REPRESENTATIVE SHALL CREATE A
WARRANTY. SOME JURISDICTIONS DO NOT ALLOW THE EXCLUSION OF OR LIMITATIONS ON
IMPLIED WARRANTIES OR THE LIMITATIONS ON THE APPLICABLE STATUTORY RIGHTS OF A
CONSUMER, SO SOME OR ALL OF THE ABOVE EXCLUSIONS AND LIMITATIONS MAY NOT APPLY
TO YOU.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using MapEditorLibrary;
using MapEditorLibrary.CCEngine;
using MapEditorLibrary.CCEngine.TileData;
using MapEditorLibrary.Extensions;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Misc;
using MapEditorLibrary.Models;
using MapEditorLibrary.Mutations.Classes.HeightMutations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.Rendering;
using TSMapEditor.Scripts;
using TSMapEditor.Settings;
using TSMapEditor.UI.Controls;
using TSMapEditor.UI.Windows;
using LandType = MapEditorLibrary.Models.Enums.LandType;
using TerrainOccupation = MapEditorLibrary.Models.Enums.TerrainOccupation;

namespace WAEScript
{
    public class GenerateRandomMapScript
    {
        public int ApiVersion => 2;
        public ScriptDependencies ScriptDependencies { get; set; }

        public void Perform()
        {
            string theater = ScriptDependencies.Map.LoadedTheaterName;
            EnvironmentType[] environments = theater.ToUpperInvariant() switch
            {
                "TEMPERATE" => new[] { EnvironmentType.Temperate, EnvironmentType.Desert, EnvironmentType.Mutated },
                "SNOW" => new[] { EnvironmentType.Tundra, EnvironmentType.Taiga },
                _ => Array.Empty<EnvironmentType>()
            };

            if (Constants.IsRA2YR || environments.Length == 0)
            {
                EditorMessageBox.Show(ScriptDependencies.WindowManager, Text("Title", "Generate Random Map"),
                    Text("UnsupportedTheater", "Open a Tiberian Sun Temperate or Snow map with the original theater assets."),
                    MessageBoxButtons.OK);

                return;
            }

            var window = new GeneratorWindow(ScriptDependencies, environments);
            ScriptDependencies.WindowController.RegisterWindow(window);
            window.Closed += (_, _) => ScriptDependencies.WindowController.UnregisterWindow(window);
            ScriptDependencies.WindowManager.AddCallback(() => window.Open());
        }

        private static string Text(string key, string value) => Translator.Translate("MapScripts.RandomMap." + key, value);

        internal enum EnvironmentType { Tundra, Taiga, Temperate, Desert, Mutated }

        internal sealed record Settings(EnvironmentType Environment, int Time, int Width, int Height, int Players,
            int Cliffs, int Accessibility, int Hills, int Tiberium, int Fields, int Water, int Vegetation, int Cities, int Seed)
        {
            public int TiberiumWildlife { get; init; }
        }

        private sealed class GeneratorWindow : INItializableWindow
        {
            private readonly ScriptDependencies dependencies;
            private readonly EnvironmentType[] environments;
            private readonly string settingsSection;
            private readonly List<XNATrackbar> sliders = new List<XNATrackbar>();
            private XNADropDown environmentDropDown;
            private XNADropDown timeDropDown;
            private XNADropDown widthDropDown;
            private XNADropDown heightDropDown;
            private EditorNumberTextBox seedBox;
            private XNAPanel previewPanel;
            private XNALabel summary;
            private Texture2D previewTexture;
            private Settings previewSettings;
            private Map previewMap;

            public GeneratorWindow(ScriptDependencies dependencies, EnvironmentType[] environments) : base(dependencies.WindowManager)
            {
                this.dependencies = dependencies;
                this.environments = environments;
                settingsSection = "RandomMap." + dependencies.Map.LoadedTheaterName;
            }

            public override void Initialize()
            {
                Name = nameof(GeneratorWindow);
                ConfigIni = new IniFile();
                ConfigIni.SetIntValue(Name, "Width", 690);
                ConfigIni.SetIntValue(Name, "Height", 476);
                ConfigIni.SetBooleanValue(Name, "CenterByDefault", true);
                base.Initialize();

                Settings settings = ReadPreferences(UserSettings.Instance.SettingsIni);
                Label(16, 12, GenerateRandomMapScript.Text("Title", "Generate Random Map")).FontIndex = 1;
                environmentDropDown = DropDown(42, GenerateRandomMapScript.Text("Environment", "Environment"),
                    environments.Select(EnvironmentName), Array.IndexOf(environments, settings.Environment));
                timeDropDown = DropDown(70, GenerateRandomMapScript.Text("Time", "Time of day"),
                    new[] { GenerateRandomMapScript.Text("Morning", "Morning"), GenerateRandomMapScript.Text("Afternoon", "Afternoon"),
                     GenerateRandomMapScript.Text("Dusk", "Dusk"), GenerateRandomMapScript.Text("Night", "Night") }, settings.Time);
                string[] sizes = { GenerateRandomMapScript.Text("Small", "Small"), GenerateRandomMapScript.Text("Medium", "Medium"),
                    GenerateRandomMapScript.Text("Large", "Large"), GenerateRandomMapScript.Text("VeryLarge", "Very large") };
                widthDropDown = DropDown(98, GenerateRandomMapScript.Text("Width", "Map width"), sizes, settings.Width);
                heightDropDown = DropDown(126, GenerateRandomMapScript.Text("Height", "Map height"), sizes, settings.Height);

                Slider(154, GenerateRandomMapScript.Text("Players", "Players"), 2, 8, settings.Players);
                Slider(182, GenerateRandomMapScript.Text("Cliffs.v2", "Cliffs"), 0, 100, settings.Cliffs);
                Slider(210, GenerateRandomMapScript.Text("Accessibility", "Accessibility"), 0, 100, settings.Accessibility);
                Slider(238, GenerateRandomMapScript.Text("Hills", "Hills"), 0, 100, settings.Hills);
                Slider(266, GenerateRandomMapScript.Text("Tiberium", "Tiberium amount"), 1, 100, settings.Tiberium);
                Slider(294, GenerateRandomMapScript.Text("Fields", "Tiberium fields"), 0, 100, settings.Fields);
                Slider(322, GenerateRandomMapScript.Text("Water", "Water"), 0, 100, settings.Water);
                Slider(350, GenerateRandomMapScript.Text("Vegetation", "Vegetation"), 0, 100, settings.Vegetation);
                Slider(378, GenerateRandomMapScript.Text("Cities", "Cities"), 0, 100, settings.Cities);

                Label(16, 407, GenerateRandomMapScript.Text("Seed", "Seed"));
                seedBox = new EditorNumberTextBox(WindowManager) { X = 160, Y = 403, Width = 144, Value = settings.Seed };
                AddChild(seedBox);

                previewPanel = new XNAPanel(WindowManager) { X = 338, Y = 42, Width = 336, Height = 337, DrawBorders = true };
                AddChild(previewPanel);
                summary = Label(338, 388, GenerateRandomMapScript.Text("PreviewHint", "Generate a preview to inspect the layout."));
                Button(338, 427, 108, GenerateRandomMapScript.Text("Surprise", "Surprise Me"), Surprise);
                Button(454, 427, 94, GenerateRandomMapScript.Text("Preview", "Preview"), Preview);
                Button(556, 427, 118, GenerateRandomMapScript.Text("Generate", "Generate"), Confirm);
                Button(16, 440, 117, GenerateRandomMapScript.Text("Cancel", "Cancel"), Hide);
            }

            public void Open() => Show();

            private static string EnvironmentName(EnvironmentType environment) => environment switch
            {
                EnvironmentType.Tundra => GenerateRandomMapScript.Text("Tundra", "Tundra"),
                EnvironmentType.Taiga => GenerateRandomMapScript.Text("Taiga", "Taiga"),
                EnvironmentType.Temperate => GenerateRandomMapScript.Text("Temperate", "Temperate"),
                EnvironmentType.Desert => GenerateRandomMapScript.Text("Desert", "Desert"),
                _ => GenerateRandomMapScript.Text("Mutated", "Mutated")
            };

            private XNALabel Label(int x, int y, string value)
            {
                var label = new XNALabel(WindowManager) { X = x, Y = y, Text = value };
                AddChild(label);

                return label;
            }

            private XNADropDown DropDown(int y, string label, IEnumerable<string> options, int selected)
            {
                Label(16, y + 3, label);
                var control = new XNADropDown(WindowManager) { X = 160, Y = y, Width = 144 };
                AddChild(control);
                foreach (string option in options)
                {
                    control.AddItem(option);
                }

                control.SelectedIndex = selected;

                return control;
            }

            private void Slider(int y, string label, int min, int max, int initial)
            {
                Label(16, y + 3, label);
                var trackbar = new XNATrackbar(WindowManager) { X = 160, Y = y, Width = 144, MinValue = min, MaxValue = max, Value = initial };
                AddChild(trackbar);
                var numberLabel = Label(311, y + 3, initial.ToString());
                trackbar.ValueChanged += (_, _) => numberLabel.Text = trackbar.Value.ToString();
                sliders.Add(trackbar);
            }

            private void Button(int x, int y, int width, string label, Action action)
            {
                var button = new EditorButton(WindowManager) { X = x, Y = y, Width = width, Text = label };
                button.LeftClick += (_, _) => action();
                AddChild(button);
            }

            private Settings ReadSettings()
            {
                if (!int.TryParse(seedBox.Text, out int seed) || seed < 0 || seed > 65535)
                    throw new InvalidOperationException(GenerateRandomMapScript.Text("InvalidSeed", "Enter a seed between 0 and 65535."));

                return new Settings(environments[environmentDropDown.SelectedIndex], timeDropDown.SelectedIndex, widthDropDown.SelectedIndex,
                    heightDropDown.SelectedIndex, sliders[0].Value, sliders[1].Value, sliders[2].Value, sliders[3].Value, sliders[4].Value,
                    sliders[5].Value, sliders[6].Value, sliders[7].Value, sliders[8].Value, seed);
            }

            private Settings ReadPreferences(IniFile ini)
            {
                int Read(string key, int defaultValue, int min = 0, int max = 100) =>
                    Math.Clamp(ini.GetIntValue(settingsSection, key, defaultValue), min, max);

                string biome = ini.GetStringValue(settingsSection, nameof(Settings.Environment), string.Empty);
                EnvironmentType environment = environments.FirstOrDefault(
                    value => value.ToString().Equals(biome, StringComparison.OrdinalIgnoreCase), environments[0]);
                int seed = ini.GetIntValue(settingsSection, nameof(Settings.Seed), -1);
                if (seed < 0 || seed > 65535)
                    seed = Random.Shared.Next(65536);

                return new Settings(environment, Read(nameof(Settings.Time), 1, 0, 3), Read(nameof(Settings.Width), 1, 0, 3),
                    Read(nameof(Settings.Height), 1, 0, 3), Read(nameof(Settings.Players), 2, 2, 8), Read(nameof(Settings.Cliffs), 50),
                    Read(nameof(Settings.Accessibility), 65), Read(nameof(Settings.Hills), 50), Read(nameof(Settings.Tiberium), 25, 1),
                    Read(nameof(Settings.Fields), 35), Read(nameof(Settings.Water), 30), Read(nameof(Settings.Vegetation), 50),
                    Read(nameof(Settings.Cities), 25), seed);
            }

            private void RememberSettings(Settings settings)
            {
                var userSettings = UserSettings.Instance;
                IniFile ini = userSettings.SettingsIni;
                ini.SetStringValue(settingsSection, nameof(Settings.Environment), settings.Environment.ToString());
                ini.SetIntValue(settingsSection, nameof(Settings.Time), settings.Time);
                ini.SetIntValue(settingsSection, nameof(Settings.Width), settings.Width);
                ini.SetIntValue(settingsSection, nameof(Settings.Height), settings.Height);
                ini.SetIntValue(settingsSection, nameof(Settings.Players), settings.Players);
                ini.SetIntValue(settingsSection, nameof(Settings.Cliffs), settings.Cliffs);
                ini.SetIntValue(settingsSection, nameof(Settings.Accessibility), settings.Accessibility);
                ini.SetIntValue(settingsSection, nameof(Settings.Hills), settings.Hills);
                ini.SetIntValue(settingsSection, nameof(Settings.Tiberium), settings.Tiberium);
                ini.SetIntValue(settingsSection, nameof(Settings.Fields), settings.Fields);
                ini.SetIntValue(settingsSection, nameof(Settings.Water), settings.Water);
                ini.SetIntValue(settingsSection, nameof(Settings.Vegetation), settings.Vegetation);
                ini.SetIntValue(settingsSection, nameof(Settings.Cities), settings.Cities);
                ini.SetIntValue(settingsSection, nameof(Settings.Seed), settings.Seed);
                _ = userSettings.SaveSettingsAsync();
            }

            private void Surprise()
            {
                Random random = Random.Shared;
                environmentDropDown.SelectedIndex = random.Next(environments.Length);
                timeDropDown.SelectedIndex = random.Next(4);
                widthDropDown.SelectedIndex = random.Next(4);
                heightDropDown.SelectedIndex = random.Next(4);
                foreach (var slider in sliders)
                {
                    slider.Value = random.Next(slider.MinValue, slider.MaxValue + 1);
                }

                seedBox.Value = random.Next(65536);
                Preview();
            }

            private bool PreparePreview()
            {
                try
                {
                    Settings settings = ReadSettings();
                    if (settings == previewSettings)
                        return true;

                    Map draft = new Generator(dependencies.Map, settings).Build();
                    var texture = CreatePreviewTexture(draft, WindowManager.GraphicsDevice, previewPanel.Width - 2, previewPanel.Height - 2);
                    previewTexture?.Dispose();
                    previewTexture = texture;
                    previewPanel.BackgroundTexture = texture;
                    previewMap = draft;
                    previewSettings = settings;
                    summary.Text = string.Format(GenerateRandomMapScript.Text("Summary", "{0} × {1} playable cells, {2} players"),
                        draft.LocalSize.Width, draft.LocalSize.Height, draft.Waypoints.Count);
                    RememberSettings(settings);

                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Log("Random map generation failed: " + ex);
                    EditorMessageBox.Show(WindowManager, GenerateRandomMapScript.Text("Title", "Generate Random Map"), ex.Message, MessageBoxButtons.OK);

                    return false;
                }
            }

            private void Preview() => PreparePreview();

            private void Confirm()
            {
                if (!PreparePreview())
                    return;

                var prompt = EditorMessageBox.Show(WindowManager, GenerateRandomMapScript.Text("Title", "Generate Random Map"),
                    GenerateRandomMapScript.Text("Confirm", "Replace the open map's terrain, objects and waypoints with this preview? This clears undo history. Save a copy first if you want to keep the current layout."),
                    MessageBoxButtons.YesNo);
                prompt.YesClickedAction = _ =>
                {
                    Apply(previewMap, dependencies.Map);
                    dependencies.CursorActionTarget.MutationManager.ClearUndoAndRedoLists();
                    dependencies.CursorActionTarget.TechnoUnderCursor = null;
                    dependencies.CursorActionTarget.InvalidateMap();
                    Hide();
                };
            }

            public override void Kill()
            {
                if (previewPanel != null)
                    previewPanel.BackgroundTexture = null;

                previewTexture?.Dispose();
                previewTexture = null;
                base.Kill();
            }
        }

        private static Texture2D CreatePreviewTexture(Map draft, GraphicsDevice device, int width, int height)
        {
            var bounds = draft.LocalSize;
            var pixels = new Color[width * height];
            float aspect = Constants.CellSizeX / (float)Constants.CellSizeY;
            float scaleY = Math.Min(width / (bounds.Width * aspect), height / (float)bounds.Height);
            float scaleX = scaleY * aspect;
            float left = (width - bounds.Width * scaleX) / 2;
            float top = (height - bounds.Height * scaleY) / 2;
            Array.Fill(pixels, new Color(24, 29, 36));

            var graphics = (TheaterGraphics)draft.TheaterInstance;
            for (int y = (int)Math.Ceiling(top); y < top + bounds.Height * scaleY && y < height; y++)
            {
                for (int x = (int)Math.Ceiling(left); x < left + bounds.Width * scaleX && x < width; x++)
                {
                    int column = bounds.X + (int)((x - left) / scaleX);
                    int halfRow = bounds.Y * 2 + (int)((y - top) * 2 / scaleY);
                    Point2D point = CellAt(draft.Size.X, column, halfRow);
                    MapTile cell = draft.GetTile(point);
                    var image = graphics.GetTileGraphics(cell.TileIndex, 0).GetSubTile(cell.SubTileIndex)?.TmpImage;
                    Color color = image == null ? Color.Gray : image.RadarLeftColor.ToXnaColor();
                    if (cell.Overlay?.OverlayType.Tiberium == true)
                        color = Color.LimeGreen;
                    else if (cell.Structures.Count > 0)
                        color = Color.SlateGray;
                    else if (cell.TerrainObject != null)
                        color = Color.DarkOliveGreen;

                    double ambient = draft.Lighting.Ambient;
                    pixels[y * width + x] = new Color((int)(color.R * ambient), (int)(color.G * ambient), (int)(color.B * ambient));
                }
            }

            foreach (var waypoint in draft.Waypoints)
            {
                Point2D point = waypoint.Position;
                int halfRow = point.X + point.Y - draft.Size.X - 1;
                int column = point.X - 1 - (halfRow + 1) / 2;
                int x = (int)(left + (column - bounds.X + 0.5f) * scaleX);
                int y = (int)(top + (halfRow / 2f - bounds.Y + 0.5f) * scaleY);
                for (int dy = -2; dy <= 2; dy++)
                {
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        if (x + dx >= 0 && x + dx < width && y + dy >= 0 && y + dy < height)
                            pixels[(y + dy) * width + x + dx] = Color.OrangeRed;
                    }
                }
            }

            var texture = new Texture2D(device, width, height);
            texture.SetData(pixels);

            return texture;
        }

        private static Point2D CellAt(int mapWidth, int column, int halfRow)
        {
            int row = halfRow / 2;
            return new Point2D(1 + row + column + halfRow % 2, mapWidth + row - column);
        }

        internal static readonly string[] TiberiumWildlifeTypes = { "JFISH", "VISC_SML", "VISC_LRG", "DOGGIE" };
        private static readonly int[] nativeTiberiumFrames = { 0, 1, 3, 4, 6, 7, 8, 10, 11 };
        private const int IceCracked = 16;
        private const int IceEdge = 17;
        private const int N = 1, NE = 2, E = 4, SE = 8, S = 16, SW = 32, W = 64, NW = 128;
        private static readonly Point2D[] adjacent =
        {
            new(0, -1), new(1, -1), new(1, 0), new(1, 1), new(0, 1), new(-1, 1), new(-1, 0), new(-1, -1)
        };

        private static int NeighborMask(Point2D point, Func<Point2D, bool> predicate)
        {
            int mask = 0;
            for (int i = 0; i < adjacent.Length; i++)
            {
                if (predicate(point + adjacent[i]))
                    mask |= 1 << i;
            }

            return mask;
        }

        private static double Distance(Point2D first, Point2D second)
        {
            int x = first.X - second.X;
            int y = first.Y - second.Y;
            return Math.Sqrt(x * x + y * y);
        }

        private static IEnumerable<MapTile> InIsometricOrder(Map map)
        {
            var cells = new List<MapTile>();
            map.DoForAllValidTiles(cells.Add);
            return cells.OrderBy(cell => cell.X + cell.Y).ThenBy(cell => cell.X);
        }

        private static bool InNativeRadarArea(Point2D point, int height, RampType ramp, int width, Rectangle local)
        {
            int diagonal = point.X + point.Y;
            int top = width + 2 * local.Top;
            if (ramp != RampType.None && diagonal < top + 4 + height)
                height++;

            return diagonal > top + height &&
                diagonal <= width + 2 * (local.Bottom + 1) + height &&
                point.X - point.Y < 2 * local.Right - width &&
                point.Y - point.X < width - 2 * local.Left;
        }

        private static bool CanPlaceNativePavedRoad(Map map, int clearTile, Func<string, int> tileStart, Rectangle rect,
            bool overlapRoad, bool overlapRoadEnd, bool requireLevel = false)
        {
            if (map.GetTile(rect.Left, rect.Top) is not MapTile origin ||
                map.GetTile(rect.Right - 1, rect.Top) == null ||
                map.GetTile(rect.Left, rect.Bottom - 1) == null ||
                map.GetTile(rect.Right - 1, rect.Bottom - 1) == null)
            {
                return false;
            }

            int roads = tileStart("PavedRoads");
            int ends = tileStart("PavedRoadEnds");
            int misc = tileStart("MiscPaveTile");
            int pavement = tileStart("PaveTile");
            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                for (int x = rect.Left; x < rect.Right; x++)
                {
                    MapTile cell = map.GetTile(x, y);
                    if (cell == null || (requireLevel && cell.Level != origin.Level))
                        return false;

                    int index = cell.TileIndex;
                    if ((uint)(index - roads) < 15)
                    {
                        if (!overlapRoad)
                            return false;
                    }
                    else if ((uint)(index - ends) < 4)
                    {
                        if (!overlapRoadEnd)
                            return false;
                    }
                    else if (index != clearTile && index != ushort.MaxValue &&
                        (uint)(index - misc) >= 14 && (uint)(index - pavement) >= 16)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool UsesNativeOverlayLand(OverlayType overlay) =>
            overlay?.NoUseTileLandType == true || overlay?.Land == LandType.Railroad;

        private static void FixupNativeGroundCover(Map map, MapTile cell, int family, int transition, Func<MapTile, bool> extraNeighbor = null)
        {
            if (transition < 0 || (cell.TileIndex != family && (uint)(cell.TileIndex - transition) >= 16))
                return;

            Point2D point = cell.CoordsToPoint();
            int mask = 0;
            for (int face = 0; face < adjacent.Length; face += 2)
            {
                MapTile neighbor = map.GetTile(point + adjacent[face]);
                if (neighbor == null || (neighbor.TileIndex != family && (uint)(neighbor.TileIndex - transition) >= 16 &&
                    extraNeighbor?.Invoke(neighbor) != true))
                {
                    mask |= 1 << (face / 2);
                }
            }

            cell.TileIndex = mask == 0 ? family : transition + mask;
        }

        private static void FixupNativeSwampLAT(Map map, MapTile cell, int swamp, int transition)
        {
            if (swamp >= 0)
                FixupNativeGroundCover(map, cell, swamp, transition, neighbor => (uint)(neighbor.TileIndex - swamp) < 10);
        }

        private static bool CliffNotch(int mask) =>
            (mask & (N | S)) == (N | S) || (mask & (E | W)) == (E | W) ||
            (mask & (N | W | SW | NE)) == (N | W | SW | NE) ||
            (mask & (W | S | SE)) == (W | SE) || (mask & (N | W | NE)) == (W | NE) ||
            (mask & (SW | S | E)) == (SW | E) || (mask & (N | NW | E)) == (NW | E) ||
            (mask & (S | E | NE)) == (S | NE) || (mask & (NW | W | S)) == (NW | S) ||
            (mask & (N | SE | E)) == (N | SE) || (mask & (N | W | SW)) == (N | SW);

        private static int SelectCliffVariant(Point2D point, int mask, int roll, Func<Point2D, int> highMask)
        {
            if ((mask & (N | W)) == (N | W))
                return (highMask(point + adjacent[4]) & W) != 0 || (highMask(point + adjacent[2]) & N) != 0 ? 34 : roll % 3 + 9;
            if ((mask & (N | E)) == (N | E))
                return 39;
            if ((mask & (E | S)) == (E | S))
                return 33;
            if ((mask & (S | W)) == (S | W))
                return 40;

            if ((mask & E) != 0)
            {
                if ((mask & SE) != 0 && (mask & (SW | S)) == 0 && (highMask(point + adjacent[4]) & (E | S)) != (E | S))
                    return roll % 3 + 35;

                return (mask & (SE | S)) != 0 || (highMask(point + adjacent[3]) & E) != 0 || (roll & 1) == 0 ? 38 : 1;
            }

            if ((mask & W) != 0)
                return (mask & SW) == 0 || (mask & (SE | S)) != 0 || (highMask(point + adjacent[4]) & (S | W)) == (S | W) ? 18 : roll % 3 + 15;
            if ((mask & S) != 0)
            {
                if ((mask & SE) != 0 && (mask & (NE | E)) == 0 && (highMask(point + adjacent[2]) & (E | S)) != (E | S))
                    return roll % 3 + 23;

                return (mask & (E | SE)) != 0 || (highMask(point + adjacent[3]) & S) != 0 || (roll & 1) == 0 ? 26 : 21;
            }

            if ((mask & N) != 0)
                return (mask & NE) == 0 || (mask & (E | SE)) != 0 || (highMask(point + adjacent[2]) & (N | E)) == (N | E) ? 8 : roll % 3 + 5;
            if ((mask & NE) != 0)
                return 2;
            if ((mask & SE) != 0)
                return (roll & 1) + 29;
            if ((mask & SW) != 0)
                return 22;

            return roll % 3 + 12;
        }

        internal sealed class Generator
        {
            private const byte StartingLevel = 4;
            private static readonly int[] minimumSizes = { 50, 65, 75, 85, 100, 120, 135 };
            private static readonly int[] maximumSizes = { 100, 115, 128, 140, 160, 170, 175 };
            private readonly Map source;
            private readonly Settings settings;
            private readonly GeneratorRandom random;
            private readonly IniFile config;
            private readonly IniFile rules;
            private readonly IniFile firestorm;
            private bool placeWaterfall;
            private LiquidWater liquidWater;
            private MapTile startSampleSlot;
            private bool startSampleSlotProtected;
            private readonly Map draft;
            private readonly List<Point2D> cells = new List<Point2D>();
            private readonly HashSet<Point2D> playable = new HashSet<Point2D>();
            private readonly HashSet<Point2D> water = new HashSet<Point2D>();
            private readonly HashSet<Point2D> protectedGround = new HashSet<Point2D>();
            private readonly HashSet<Point2D> rampApproaches = new HashSet<Point2D>();
            private readonly List<Point2D> rampLandings = new List<Point2D>();
            private readonly HashSet<Point2D> groundRoutes = new HashSet<Point2D>();
            private readonly ITheater theater;
            private readonly int? nativeRegionSize;
            private readonly int clearTile;
            private readonly bool snowy;
            private readonly bool impassableCliffBacks;
            private readonly IReadOnlyDictionary<LandType, float> wheelCosts;
            private readonly bool hasFirestorm;

            public Generator(Map source, Settings settings, int? nativeRegionSize = null)
            {
                this.source = source;
                this.settings = settings;
                this.nativeRegionSize = nativeRegionSize;
                random = new GeneratorRandom(settings.Seed);
                theater = source.TheaterInstance;
                CCFileManager fileManager = source.FileManager;
                config = IniFileEx.FromPathOrMix(theater.Theater.ConfigINIPath, fileManager.GameDirectory, fileManager);
                const string cliffBackKey = "CliffBackImpassability";
                rules = IniFileEx.FromPathOrMix(Constants.RulesIniPath, fileManager.GameDirectory, fileManager, true);
                firestorm = IniFileEx.FromPathOrMix(Constants.FirestormIniPath, fileManager.GameDirectory, fileManager, true);
                hasFirestorm = firestorm != null;
                int baseValue = rules?.GetIntValue("General", cliffBackKey, 0) ?? 0;
                int expansionValue = firestorm?.GetIntValue("General", cliffBackKey, baseValue) ?? baseValue;
                impassableCliffBacks = (source.LoadedINI?.GetIntValue("General", cliffBackKey, expansionValue) ?? expansionValue) == 2;
                if (UsesNativeStarts)
                    wheelCosts = NativeStartZones.ReadWheelCosts(rules, firestorm);
                snowy = settings.Environment == EnvironmentType.Tundra || settings.Environment == EnvironmentType.Taiga;
                if (snowy != source.LoadedTheaterName.Equals("Snow", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("The selected environment does not match the loaded theater.");

                int width = Dimension(settings.Width);
                int height = Dimension(settings.Height);
                if (width + 4 > Constants.MaxMapWidth || height + 12 > Constants.MaxMapHeight)
                    throw new InvalidOperationException(Text("InvalidDimensions", "The selected map exceeds the editor's size limits."));

                draft = new Map
                {
                    Size = new Point2D(width + 4, height + 12),
                    LocalSize = new Rectangle(2, 5, width, height),
                    TheaterInstance = theater
                };

                clearTile = Set("ClearTile").StartTileIndex;
                draft.SetTileData(null, StartingLevel);
                if (clearTile != 0)
                    draft.DoForAllValidTiles(cell => cell.ChangeTileIndex(clearTile, 0));

                // Include the staggered half rows of the original diamond, with a collar outside
                // LocalSize for terrain that crosses the visible map edge.
                for (int row = 2; row < (height + 9) * 2; row++)
                {
                    for (int column = 1; column < width + 3; column++)
                    {
                        Point2D point = CellAt(draft.Size.X, column, row);
                        cells.Add(point);
                        playable.Add(point);
                    }
                }
            }

            private int Dimension(int size)
            {
                float fraction = size * (1.0f / 3.0f);
                int index = settings.Players - 2;

                return (int)(minimumSizes[index] * (1.0f - fraction) + maximumSizes[index] * fraction);
            }

            private TileSet Set(string role)
            {
                var set = theater.Theater.TryGetTileSetById(config.GetIntValue("General", role, -1));
                if (set == null || set.LoadedTileCount == 0)
                    throw new InvalidOperationException(string.Format(Text("MissingTileSet", "The loaded theater lacks the required {0} tile set."), role));

                return set;
            }

            private ITileImage Tile(TileSet set, int offset)
            {
                if (offset < 0 || offset >= set.LoadedTileCount)
                    throw new InvalidOperationException(string.Format(Text("MissingVariant", "Tile set {0} lacks stock variant {1}."), set.SetName, offset + 1));

                return CurrentTile(set.StartTileIndex + offset);
            }

            private ITileImage CurrentTile(int index) =>
                theater is TheaterGraphics graphics ? graphics.GetTileGraphics(index, 0) : theater.GetTile(index);

            private void InitializeRandom(Action<string, Map, int> trace)
            {
                if (nativeRegionSize.HasValue || settings.Water > 0)
                    placeWaterfall = (float)random.Fraction() < 0.25f;

                trace?.Invoke("initial", draft, random.DrawCount);
            }

            private bool UsesNativeRegions => settings.Environment != EnvironmentType.Tundra &&
                (nativeRegionSize.HasValue || settings.Water > 0);

            private bool UsesNativeStarts => nativeRegionSize.HasValue || UsesNativeRegions;

            internal void TraceNativeRegions(string checkpoint, Action<string, Map, int, IReadOnlyDictionary<Point2D, int>> trace)
            {
                if (!UsesNativeRegions)
                    throw new InvalidOperationException("Region checkpoints require an OpenTS seed from a biome that generates cliffs.");

                TraceWater((phase, map, draws) => trace(phase, map, draws, null));
                BuildNativeRegions(checkpoint, trace);
            }

            private IReadOnlyDictionary<Point2D, int> BuildNativeRegions(string checkpoint, Action<string, Map, int, IReadOnlyDictionary<Point2D, int>> trace)
            {
                var regions = new NativeRegions(draft, source, random, clearTile, Set, TileStart, CurrentTile, liquidWater);
                if (liquidWater != null)
                {
                    trace?.Invoke("region-initialized", draft, random.DrawCount, regions.Owners);
                    if (checkpoint == "region-initialized")
                        return null;
                }

                regions.SplitOversizedRegions(nativeRegionSize ?? (100 - settings.Cliffs));
                trace?.Invoke("region-heights", draft, random.DrawCount, null);
                if (checkpoint == "region-heights")
                    return null;

                regions.ExpandHighGround();
                trace?.Invoke("region-expanded", draft, random.DrawCount, null);
                if (checkpoint == "region-expanded")
                    return null;

                var owners = regions.Rebuild();
                trace?.Invoke("region-rebuilt", draft, random.DrawCount, owners);
                if (checkpoint == "region-rebuilt")
                    return null;

                regions.Connect(settings.Accessibility);
                trace?.Invoke("region-connected", draft, random.DrawCount, owners);
                if (checkpoint == "region-connected")
                    return null;

                regions.BuildCliffTiles(settings.Environment == EnvironmentType.Mutated);
                if (liquidWater != null)
                    RefreshLiquidWater();

                trace?.Invoke("regions", draft, random.DrawCount, null);
                return owners;
            }

            internal IReadOnlySet<Point2D> StartGround => protectedGround;

            internal void TraceStartsThroughVegetation(string checkpoint, Action<string, Map, int> trace)
            {
                if (!UsesNativeStarts)
                    throw new InvalidOperationException("The native start checkpoints require an OpenTS seed.");

                var owners = BuildTerrain(trace);
                List<Point2D> sites = BuildStarts(owners, trace, out var recalculateRamp);
                if (checkpoint == "starts")
                    return;

                BuildLights(trace, recalculateRamp);
                if (checkpoint == "lights")
                    return;

                BuildTiberium(sites, trace, recalculateRamp);
                if (checkpoint == "tiberium")
                    return;

                NativeScenery scenery = BuildSettlements(trace, recalculateRamp);
                if (checkpoint == "settlements")
                    return;

                scenery.FinishTerrain();
                BuildHills(scenery, trace);
                if (checkpoint == "vegetation")
                    BuildVegetation(scenery, trace);
            }

            internal void TraceWater(Action<string, Map, int> trace)
            {
                InitializeRandom(trace);
                if (settings.Environment == EnvironmentType.Tundra)
                    SeedNativeArcticWater(trace);
                else if (settings.Water > 0)
                    SeedLiquidWater();

                trace?.Invoke("water", draft, random.DrawCount);
            }

            private IReadOnlyDictionary<Point2D, int> BuildTerrain(Action<string, Map, int> trace)
            {
                TraceWater(trace);

                IReadOnlyDictionary<Point2D, int> owners = null;
                if (UsesNativeRegions)
                    owners = BuildNativeRegions("regions", null);
                else
                    SeedCliffs();

                trace?.Invoke("regions", draft, random.DrawCount);

                return owners;
            }

            private List<Point2D> BuildStarts(IReadOnlyDictionary<Point2D, int> owners, Action<string, Map, int> trace,
                out Action<MapTile> recalculateRamp)
            {
                recalculateRamp = null;
                List<Point2D> spread;
                if (UsesNativeStarts)
                {
                    var attributes = settings.Environment != EnvironmentType.Tundra ? RecalculateStartAttributes(out recalculateRamp) : null;
                    trace?.Invoke("start-attributes", draft, random.DrawCount);
                    var zones = new NativeStartZones(draft, CurrentTile, impassableCliffBacks, wheelCosts, attributes);
                    var starts = new NativeStarts(draft, random, clearTile, zones, owners,
                        rect => CanPlaceNativePavedRoad(draft, clearTile, TileStart, rect, false, false));
                    spread = starts.Place(settings.Players, settings.Fields, protectedGround);
                    startSampleSlot = starts.SampleSlotZero;
                    startSampleSlotProtected = starts.SampleSlotZeroProtected;
                }
                else
                {
                    spread = ChooseStarts();
                }

                trace?.Invoke("starts", draft, random.DrawCount);
                return spread;
            }

            private Func<MapTile, (RampType Ramp, byte TerrainType)> RecalculateStartAttributes(out Action<MapTile> recalculateRamp)
            {
                const int rampBaseCount = 20;
                const int rampSmoothCount = 12;
                int rampBase = Set("RampBase").StartTileIndex;
                int rampSmooth = Set("RampSmooth").StartTileIndex;
                int swamp = TileStart("SwampTile");
                int swampTransition = TileStart("WaterToSwampLat");
                var attributes = new Dictionary<Point2D, (RampType Ramp, byte TerrainType)>();

                RampType RampAt(Point2D point)
                {
                    if (attributes.TryGetValue(point, out var original))
                        return original.Ramp;

                    // Ramp placement assigns flags only to the ramp body. Slope pieces
                    // acquire their TMP ramp flags when the native iterator reaches them.
                    MapTile neighbor = draft.GetTile(point);
                    int offset = (neighbor?.TileIndex ?? clearTile) - rampBase;
                    return (uint)offset < rampBaseCount ? (RampType)(offset + 1) : RampType.None;
                }

                void Recalculate(MapTile cell)
                {
                    ITileImage tile = CurrentTile(cell.TileIndex == ushort.MaxValue ? clearTile : cell.TileIndex);
                    TmpImage image = cell.SubTileIndex < tile.SubTileCount ? tile.GetSubTile(cell.SubTileIndex)?.TmpImage : null;
                    Point2D point = cell.CoordsToPoint();
                    if (image == null)
                    {
                        cell.ChangeTileIndex(clearTile, 0);
                        attributes[point] = (RampType.None, 0);
                        return;
                    }

                    RampType ramp = image.RampType;
                    attributes[point] = (ramp, image.TerrainType);

                    if (UsesNativeOverlayLand(cell.Overlay?.OverlayType))
                        return;

                    FixupNativeSwampLAT(draft, cell, swamp, swampTransition);
                    if (ramp > RampType.South && cell.Overlay?.OverlayType.Tiberium == true)
                        cell.Overlay = null;

                    if ((uint)(cell.TileIndex - rampBase) >= 4 && (uint)(cell.TileIndex - rampSmooth) >= rampSmoothCount)
                        return;

                    int mask = 0;
                    if (ramp is >= RampType.West and <= RampType.South)
                    {
                        int direction = ((int)ramp + 2) % 4 * 2;
                        if (RampAt(point + adjacent[direction]) == RampType.None)
                            mask |= 1;
                        if (RampAt(point + adjacent[(direction + 4) % 8]) == RampType.None)
                            mask |= 2;
                    }

                    int rampOffset = (int)ramp - 1;
                    int index = mask == 0 ? rampBase + rampOffset : rampSmooth + 3 * rampOffset + mask - 1;
                    cell.ChangeTileIndex(index, cell.SubTileIndex);
                }

                foreach (MapTile cell in InIsometricOrder(draft))
                    Recalculate(cell);

                recalculateRamp = Recalculate;
                return cell => attributes[cell.CoordsToPoint()];
            }


            public Map Build(Action<string, Map, int> trace = null)
            {
                var owners = BuildTerrain(trace);
                List<Point2D> spread = BuildStarts(owners, trace, out var recalculateRamp);
                BuildLights(trace, recalculateRamp);
                BuildTiberium(spread, trace, recalculateRamp);
                NativeScenery scenery = BuildSettlements(trace, recalculateRamp);
                scenery.FinishTerrain();
                BuildHills(scenery, trace);
                BuildVegetation(scenery, trace);
                scenery.FinishVegetationTerrain();
                if (UsesNativeStarts)
                {
                    BlendNativeTiberium();
                    scenery.FinishVegetationTerrain();
                }
                else
                {
                    BlendEditorTiberium();
                }
                ApplyLighting();
                trace?.Invoke("complete", draft, random.DrawCount);

                return draft;
            }

            private void ApplyLighting()
            {
                double ambient = settings.Time switch { 1 => 1.0, 3 => 0.5, _ => 0.75 };
                double theaterAmbient = ambient * (snowy ? 0.75 : 1.0);
                if (nativeRegionSize.HasValue)
                {
                    // Init_Map sets normal ambient after loading the theater-scaled defaults.
                    draft.Lighting.Ambient = ambient;
                    draft.Lighting.Level = 0.001;
                    draft.Lighting.Red = draft.Lighting.Green = draft.Lighting.Blue = 1.0;
                    draft.Lighting.IonAmbient = (int)(theaterAmbient * 100.0) / 100.0;
                    draft.Lighting.IonRed = draft.Lighting.IonGreen = draft.Lighting.IonBlue = 1.0;
                }
                else
                {
                    draft.Lighting.Ambient = theaterAmbient;
                    draft.Lighting.Level = 0.01;
                }
            }

            private List<Point2D> Grow(Point2D origin, int count, Func<Point2D, bool> canVisit, Func<Point2D, int, double> priority,
                Func<Point2D, Point2D, bool> canCross = null, Action<Point2D, Point2D> onVisit = null)
            {
                var visited = new HashSet<Point2D> { origin };
                var queue = new PriorityQueue<Point2D, (double Score, int Order)>();
                var result = new List<Point2D>();
                queue.Enqueue(origin, (0, 0));

                while (result.Count < count && queue.TryDequeue(out Point2D current, out _))
                {
                    result.Add(current);
                    for (int direction = 0; direction < adjacent.Length; direction += 2)
                    {
                        Point2D neighbor = current + adjacent[direction];
                        if (playable.Contains(neighbor) && (canCross == null || canCross(current, neighbor)) &&
                            canVisit(neighbor) && visited.Add(neighbor))
                        {
                            onVisit?.Invoke(current, neighbor);
                            queue.Enqueue(neighbor, (priority(neighbor, result.Count), visited.Count));
                        }
                    }
                }

                return result;
            }

            private bool Bare(Point2D point)
            {
                MapTile cell = draft.GetTile(point);
                return cell != null && !water.Contains(point) && cell.TileIndex == clearTile && cell.Overlay == null &&
                    cell.TerrainObject == null && cell.Structures.Count == 0;
            }

            private void SeedNativeArcticWater(Action<string, Map, int> trace)
            {
                if (settings.Water > 0)
                    new ArcticWater(draft, random, Set("Ice1Set").StartTileIndex, clearTile, settings.Water).Seed();

                trace?.Invoke("ice-seeded", draft, random.DrawCount);
                if (settings.Water == 0)
                    return;

                int[] iceSets = { Set("Ice1Set").StartTileIndex, Set("Ice2Set").StartTileIndex, Set("Ice3Set").StartTileIndex };
                new ArcticIceSmoother(draft, clearTile, iceSets, Set("WaterSet").StartTileIndex,
                    Set("ShorePieces").StartTileIndex, Set("IceShoreSet").StartTileIndex).Smooth();
                draft.DoForAllValidTiles(cell =>
                {
                    if (Helpers.IsLandTypeImpassable(GetTilePart(cell).TerrainType, true))
                        water.Add(cell.CoordsToPoint());
                });
            }

            private int TileStart(string role) =>
                theater.Theater.TryGetTileSetById(config.GetIntValue("General", role, -1))?.StartTileIndex ?? -1;

            private void SeedLiquidWater()
            {
                liquidWater = new LiquidWater(draft, random, settings, placeWaterfall, clearTile, StartingLevel,
                    TileStart, CurrentTile, source.Rules.TerrainTypes);
                liquidWater.Seed();
                RefreshLiquidWater();
            }

            private void RefreshLiquidWater()
            {
                water.Clear();
                draft.DoForAllValidTiles(cell =>
                {
                    if (liquidWater.IsWaterFeature(cell))
                        water.Add(cell.CoordsToPoint());
                });
            }

            private bool InLocal(Point2D point, int margin) => LocalMargin(point) >= margin;

            private int LocalMargin(Point2D point)
            {
                int halfRow = point.X + point.Y - draft.Size.X - 1;
                int column = point.X - 1 - (halfRow + 1) / 2;
                int row = halfRow >> 1;
                Rectangle local = draft.LocalSize;

                return Math.Min(Math.Min(column - local.Left, local.Right - 1 - column), Math.Min(row - local.Top, local.Bottom - 1 - row));
            }

            private bool CanStamp(ITileImage tile, Point2D origin, Func<Point2D, bool> canWrite)
            {
                for (int i = 0; i < tile.SubTileCount; i++)
                {
                    Point2D? offset = tile.GetSubTileCoordOffset(i);
                    if (offset.HasValue && !canWrite(origin + offset.Value))
                        return false;
                }

                return true;
            }

            private void Stamp(ITileImage tile, Point2D origin, int baseLevel)
            {
                for (int i = 0; i < tile.SubTileCount; i++)
                {
                    Point2D? offset = tile.GetSubTileCoordOffset(i);
                    if (offset.HasValue)
                    {
                        MapTile cell = draft.GetTileOrFail(origin + offset.Value);
                        cell.ChangeTileIndex(tile.TileID, (byte)i);
                        cell.Level = (byte)(baseLevel + tile.GetSubTile(i).TmpImage.Height);
                    }
                }
            }

            private void SeedCliffs()
            {
                if (settings.Environment == EnvironmentType.Tundra)
                {
                    if (nativeRegionSize.HasValue)
                    {
                        // Build_All_Cliffs visits every clear cell on the flat ice map.
                        // Place_Cliff rolls a variant before checking for a height difference.
                        draft.DoForAllValidTiles(cell =>
                        {
                            if (cell.TileIndex == clearTile)
                                random.Pick(0, 5);
                        });
                    }

                    return;
                }

                if (nativeRegionSize == null && settings.Cliffs == 0)
                    return;

                // OpenTS's RegionSize grows with the permitted size of flat regions.
                // The editor's Cliffs control increases in the opposite direction.
                int regionSize = nativeRegionSize ?? (100 - settings.Cliffs);
                int localArea = draft.LocalSize.Width * draft.LocalSize.Height;
                int regionLimit = (int)((regionSize * 0.005 + 0.05) * 2 * localArea);
                var ground = cells.Where(point => InLocal(point, 3) && Bare(point))
                    .GroupBy(point => draft.GetTileOrFail(point).Level);
                var regions = new Queue<List<Point2D>>();
                foreach (var level in ground)
                {
                    if (level.Key + rampDepth > Constants.MaxMapHeightLevel)
                        continue;

                    var interior = new HashSet<Point2D>(level.Where(point =>
                        Area(new Rectangle(point.X, point.Y, 1, 1), 2).All(other =>
                            Bare(other) && draft.GetTileOrFail(other).Level == level.Key)));
                    foreach (var region in ConnectedRegions(interior).Where(region => region.Count > regionLimit))
                        regions.Enqueue(region);
                }

                if (regions.Count == 0)
                    return;

                TileSet cliffSet = Set("CliffSet");
                TileSet slopeSet = Set("SlopeSetPieces");
                ITileImage ramp = Tile(Set("RampBase"), (int)RampType.South - 1);
                while (regions.TryDequeue(out var region))
                {
                    byte baseLevel = draft.GetTileOrFail(region[0]).Level;
                    var regionCells = new HashSet<Point2D>(region);
                    for (int attempt = 0; attempt < 24; attempt++)
                    {
                        int count = random.Pick(region.Count / 8, region.Count / 3);
                        Point2D origin = region[random.Pick(0, region.Count - 1)];
                        var highGround = GrowHighGround(regionCells, origin, count);
                        // OpenTS merges fragments of at most 100 cells instead of giving them their own height.
                        if (highGround.Count <= 100 || !TryBuildCliffRegion(highGround, regionCells, cliffSet, slopeSet, ramp, baseLevel))
                            continue;

                        // Keep later splits away from the previous faces and ramp
                        // entrances, then split each remaining piece independently.
                        regionCells.ExceptWith(highGround);
                        regionCells.RemoveWhere(point => Area(new Rectangle(point.X, point.Y, 1, 1), 2).Any(p =>
                        {
                            MapTile cell = draft.GetTile(p);
                            return cell == null || cell.TileIndex != clearTile || cell.Level != baseLevel || rampApproaches.Contains(p);
                        }));

                        foreach (var remainder in ConnectedRegions(regionCells))
                        {
                            if (remainder.Count > regionLimit)
                                regions.Enqueue(remainder);
                        }

                        break;
                    }
                }
            }

            private const int rampEndWidth = 3;
            private const int rampWidth = 3;
            private const int rampFootprintWidth = rampEndWidth * 2 + rampWidth;
            private const int rampDepth = 4;

            private static Rectangle RampGrade(Point2D start) => new Rectangle(start.X - 1, start.Y - 1, rampFootprintWidth + 2, rampDepth + 2);

            private HashSet<Point2D> GrowHighGround(HashSet<Point2D> region, Point2D origin, int count)
            {
                // Split_Region in OpenTS grows a frontier biased by an angle
                // that wanders as the region expands.
                double angle = random.Fraction() * 2 * Math.PI;
                var high = new HashSet<Point2D>();
                var seen = new HashSet<Point2D> { origin };
                var frontier = new PriorityQueue<Point2D, (double Score, int Order)>();
                frontier.Enqueue(origin, (0, 0));
                while (high.Count < count && frontier.TryDequeue(out Point2D point, out _))
                {
                    high.Add(point);
                    for (int direction = 0; direction < adjacent.Length; direction += 2)
                    {
                        Point2D neighbor = point + adjacent[direction];
                        if (!region.Contains(neighbor) || !seen.Add(neighbor))
                            continue;

                        int dx = neighbor.X - origin.X;
                        int dy = neighbor.Y - origin.Y;
                        double heading = dx == 0 ? Math.PI / 2 : Math.Atan(-(double)dy / dx) + (dx < 0 ? Math.PI : 0);
                        double difference = Math.Abs(Math.IEEERemainder(heading - angle, 2 * Math.PI));
                        double score = random.Fraction() * 2.5 + 0.15 * Distance(origin, neighbor) + 1.5 * difference;
                        frontier.Enqueue(neighbor, (score, seen.Count));
                    }

                    angle += random.SampleNormal() * (Math.PI / 8);
                }

                // The native loop fetches one more cell before stopping, then
                // assigns the rest of its queued frontier to the new region.
                frontier.TryDequeue(out _, out _);
                while (frontier.TryDequeue(out Point2D remaining, out _))
                    high.Add(remaining);

                bool expanded;
                do
                {
                    expanded = false;
                    foreach (Point2D point in region)
                    {
                        if (!high.Contains(point) && FillCliffNotch(point, high))
                        {
                            high.Add(point);
                            expanded = true;
                        }
                    }
                }
                while (expanded);

                return high;
            }

            private static bool FillCliffNotch(Point2D point, HashSet<Point2D> high)
            {
                int mask = NeighborMask(point, high.Contains);
                if (CliffNotch(mask))
                    return true;

                for (int direction = 0; direction < adjacent.Length; direction += 2)
                {
                    Point2D offset = adjacent[direction];
                    if (high.Contains(point - offset) && high.Contains(point + offset + offset))
                        return true;
                }

                return false;
            }

            private List<Point2D> GradeCliffRamps(HashSet<Point2D> high, HashSet<Point2D> region, byte baseLevel)
            {
                // Grade a short stretch of the cliff for the ramp and keep the
                // full width of its upper and lower landings open.
                var sites = new List<(Point2D Start, int Changes)>();
                foreach (Point2D point in high)
                {
                    if (high.Contains(point + adjacent[4]))
                        continue;

                    var start = point - new Point2D(rampFootprintWidth / 2, 0);
                    var grade = RampGrade(start);
                    if (!Area(grade).All(p => Bare(p) && !rampApproaches.Contains(p) && draft.GetTileOrFail(p).Level == baseLevel))
                        continue;

                    var upper = new Rectangle(grade.X, grade.Y, grade.Width, 2);
                    var lower = new Rectangle(grade.X, start.Y + 1, grade.Width, rampDepth);
                    if (!Area(upper).All(region.Contains))
                        continue;

                    int rampX = start.X + rampEndWidth;
                    int upperChanges = Area(upper).Count(p => !high.Contains(p));
                    int lowerChanges = Area(lower).Count(high.Contains);
                    if (upperChanges <= 10 && lowerChanges <= 15 &&
                        Area(new Rectangle(rampX, start.Y - 2, rampWidth, 1)).All(high.Contains) &&
                        Area(new Rectangle(rampX, start.Y + rampDepth, rampWidth, 2)).All(p =>
                            InLocal(p, 0) && !high.Contains(p) && Bare(p) && draft.GetTileOrFail(p).Level == baseLevel))
                    {
                        sites.Add((start, upperChanges + lowerChanges));
                    }
                }

                var graded = new HashSet<Point2D>();
                var ramps = new List<Point2D>();
                int rampCount = random.Pick(0, 100) < settings.Accessibility ? random.Pick(2, 3) : 1;
                while (ramps.Count < rampCount && sites.Count > 0)
                {
                    int minimumChanges = sites.Min(site => site.Changes);
                    var best = sites.Where(site => site.Changes == minimumChanges).ToArray();
                    Point2D start = best[random.Pick(0, best.Length - 1)].Start;
                    ramps.Add(start);

                    var grade = RampGrade(start);
                    graded.UnionWith(Area(grade));
                    high.UnionWith(Area(new Rectangle(grade.X, grade.Y, grade.Width, 2)));
                    high.ExceptWith(Area(new Rectangle(grade.X, start.Y + 1, grade.Width, rampDepth)));
                    sites.RemoveAll(site => Area(RampGrade(site.Start), 1).Any(graded.Contains));
                }

                return ramps;
            }

            private bool TryBuildCliffRegion(HashSet<Point2D> high, HashSet<Point2D> region, TileSet cliffSet, TileSet slopeSet, ITileImage ramp, byte baseLevel)
            {
                List<Point2D> ramps = GradeCliffRamps(high, region, baseLevel);
                if (ramps.Count == 0)
                    return false;

                var occupied = new HashSet<Point2D>(ramps.SelectMany(start => Area(new Rectangle(start.X, start.Y, rampFootprintWidth, rampDepth + 1))));
                var placements = new List<(ITileImage Tile, Point2D Point)>();
                int Mask(Point2D point) => NeighborMask(point, high.Contains);
                bool CanPlace(Point2D point) => !occupied.Contains(point) && !rampApproaches.Contains(point) &&
                    Bare(point) && draft.GetTileOrFail(point).Level == baseLevel;
                foreach (Point2D point in cells)
                {
                    if (high.Contains(point) || occupied.Contains(point))
                        continue;

                    int mask = Mask(point);
                    if (mask == 0)
                        continue;

                    int variant = CliffVariant(point, mask, Mask);
                    ITileImage piece = Tile(cliffSet, variant - 1);
                    if (!CanStamp(piece, point, CanPlace))
                    {
                        // The short stock straight pieces fit runs where a varied
                        // two-cell piece would collide with the next corner.
                        int shorter = ShortCliffVariant(mask);
                        if (shorter == variant)
                            continue;

                        piece = Tile(cliffSet, shorter - 1);
                        if (!CanStamp(piece, point, CanPlace))
                            continue;
                    }

                    placements.Add((piece, point));
                    for (int i = 0; i < piece.SubTileCount; i++)
                    {
                        if (piece.GetSubTileCoordOffset(i) is Point2D offset)
                            occupied.Add(point + offset);
                    }
                }

                // Keep a split only when the ridgeline is closed. Tiling a
                // rejected split leaves the draft untouched.
                foreach (Point2D point in high)
                {
                    for (int i = 0; i < adjacent.Length; i += 2)
                    {
                        Point2D other = point + adjacent[i];
                        if (!high.Contains(other) && !occupied.Contains(other))
                            return false;
                    }
                }

                foreach (Point2D point in high)
                    draft.GetTileOrFail(point).Level = (byte)(baseLevel + rampDepth);

                foreach (var (piece, point) in placements)
                    Stamp(piece, point, baseLevel);

                // A straight south-facing cut uses the two OpenTS slope ends
                // and a three-cell-wide slope between them.
                ITileImage first = Tile(slopeSet, 0);
                ITileImage last = Tile(slopeSet, 1);
                foreach (Point2D start in ramps)
                {
                    Stamp(first, start, baseLevel);
                    Stamp(last, start + new Point2D(rampEndWidth + rampWidth, 0), baseLevel);
                    for (int row = 0; row < rampDepth; row++)
                    {
                        for (int x = 0; x < rampWidth; x++)
                            Stamp(ramp, start + new Point2D(rampEndWidth + x, row), baseLevel + rampDepth - 1 - row);
                    }
                }

                var lowerLanding = new Point2D(rampFootprintWidth / 2, rampDepth + 1);
                var upperLanding = new Point2D(rampFootprintWidth / 2, -2);
                var walkable = GroundCells();
                Point2D entrance = ramps[0] + lowerLanding;
                var reachable = walkable.Contains(entrance)
                    ? new HashSet<Point2D>(Grow(entrance, cells.Count, walkable.Contains, (_, _) => 0, CanCrossGrade))
                    : new HashSet<Point2D>();
                // Reject splits that cut off any usable part of this formerly connected region.
                if (ramps.Any(start => !reachable.Contains(start + lowerLanding) || !reachable.Contains(start + upperLanding)) ||
                    region.Any(point => walkable.Contains(point) && !reachable.Contains(point)))
                {
                    foreach (Point2D point in high.Concat(occupied))
                    {
                        MapTile cell = draft.GetTileOrFail(point);
                        cell.ChangeTileIndex(clearTile, 0);
                        cell.Level = baseLevel;
                    }

                    return false;
                }

                foreach (Point2D start in ramps)
                {
                    rampApproaches.UnionWith(Area(new Rectangle(start.X + rampEndWidth - 1, start.Y - 2, rampWidth + 2, rampDepth + 5)));
                    rampLandings.Add(start + lowerLanding);
                    rampLandings.Add(start + upperLanding);
                }
                protectedGround.UnionWith(rampApproaches);

                return true;
            }

            private int CliffVariant(Point2D point, int mask, Func<Point2D, int> highMask) =>
                SelectCliffVariant(point, mask, random.Pick(0, 5), highMask);

            private static int ShortCliffVariant(int mask)
            {
                if ((mask & (N | W)) == (N | W))
                    return 34;
                if ((mask & (N | E)) == (N | E))
                    return 39;
                if ((mask & (E | S)) == (E | S))
                    return 33;
                if ((mask & (S | W)) == (S | W))
                    return 40;
                if ((mask & N) != 0)
                    return 8;
                if ((mask & W) != 0)
                    return 18;
                if ((mask & E) != 0)
                    return 38;
                if ((mask & S) != 0)
                    return 26;
                if ((mask & NW) != 0)
                    return 12;
                if ((mask & NE) != 0)
                    return 2;

                return (mask & SW) != 0 ? 22 : 29;
            }

            private static IEnumerable<Point2D> Area(Rectangle area, int border = 0)
            {
                for (int y = area.Top - border; y < area.Bottom + border; y++)
                {
                    for (int x = area.Left - border; x < area.Right + border; x++)
                        yield return new Point2D(x, y);
                }
            }

            private IEnumerable<List<Point2D>> ConnectedRegions(HashSet<Point2D> reachable, Func<Point2D, Point2D, bool> canCross = null)
            {
                while (reachable.Count > 0)
                {
                    Point2D origin = reachable.First();
                    reachable.Remove(origin);
                    yield return Grow(origin, cells.Count, reachable.Remove, (_, _) => 0, canCross);
                }
            }

            // CellClass::Recalc_Attributes tests these cells when deciding whether
            // clear land next to a cliff is actually passable.
            private static readonly Point2D[] cliffBackOffsets =
            {
                new(0, -1), new(-1, 0), new(2, 2), new(1, 1), new(-1, 1), new(1, -1)
            };

            private TmpImage GetTilePart(MapTile cell)
            {
                ITileImage tile = theater is TheaterGraphics graphics ? graphics.GetTileGraphics(cell.TileIndex, 0) : theater.GetTile(cell.TileIndex);

                return tile.GetSubTile(cell.SubTileIndex).TmpImage;
            }

            private HashSet<Point2D> GroundCells()
            {
                return new HashSet<Point2D>(cells.Where(point =>
                {
                    if (!InLocal(point, 0))
                        return false;

                    MapTile cell = draft.GetTileOrFail(point);
                    if (cell.Overlay?.OverlayType.ININame.StartsWith("LOBRDG", StringComparison.Ordinal) == true)
                        return true;
                    if (water.Contains(point))
                        return false;

                    byte terrainType = GetTilePart(cell).TerrainType;
                    if (impassableCliffBacks && (terrainType is 0x0 or 0xD) &&
                        cliffBackOffsets.Any(offset => draft.GetTile(point + offset)?.Level >= cell.Level + rampDepth))
                    {
                        return false;
                    }

                    return !Helpers.IsLandTypeImpassable(terrainType, true);
                }));
            }

            private bool CanCrossGrade(Point2D from, Point2D to)
            {
                MapTile first = draft.GetTileOrFail(from);
                MapTile second = draft.GetTileOrFail(to);
                int difference = first.Level - second.Level;
                return difference switch
                {
                    0 => true,
                    1 => GetTilePart(second).RampType != RampType.None,
                    -1 => GetTilePart(first).RampType != RampType.None,
                    _ => false
                };
            }

            private List<Point2D> ChooseStarts()
            {
                var largest = ConnectedRegions(GroundCells(), CanCrossGrade).MaxBy(zone => zone.Count) ?? new List<Point2D>();
                var candidates = largest.Where(p => InLocal(p, 4) && Area(new Rectangle(p.X - 5, p.Y - 5, 10, 10))
                    .All(q => Bare(q) && draft.GetTileOrFail(q).Level == draft.GetTileOrFail(p).Level)).ToList();
                if (candidates.Count < settings.Players)
                    throw new InvalidOperationException(Text("NoStarts", "There is too little connected clear ground for the requested players. Try a larger map or less water."));

                // Sample up to fifteen start sites per player and greedily maximize
                // minimum spacing, as in Pick_Spread_Cells.
                int sampleCount = Math.Min(candidates.Count, settings.Players * 15);
                for (int i = 0; i < sampleCount; i++)
                {
                    int j = random.Pick(i, candidates.Count - 1);
                    (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
                }

                var sample = candidates.Take(sampleCount).ToList();
                Point2D first = sample.MaxBy(p => sample.Max(q => SpreadDistance(p, q)));
                var spread = new List<Point2D> { first };
                sample.Remove(first);
                int total = Math.Min(sampleCount, settings.Players + 3 + settings.Fields / 10);
                while (spread.Count < total)
                {
                    Point2D point = sample.MaxBy(p => spread.Min(q => SpreadDistance(p, q)));
                    spread.Add(point);
                    sample.Remove(point);
                }

                for (int i = 0; i < settings.Players; i++)
                {
                    Point2D start = spread[i];
                    draft.AddWaypoint(new Waypoint { Identifier = i, Position = start });
                    protectedGround.UnionWith(Grow(start, 400, p => Bare(p) && InLocal(p, 0), (p, _) => Distance(start, p)));
                }

                if (rampLandings.Count > 0)
                    ProtectGroundRoutes(largest);

                return spread.Skip(settings.Players).ToList();
            }

            private void ProtectGroundRoutes(List<Point2D> zone)
            {
                // Reserve routes between the starts and ramp landings before planting
                // trees or placing buildings in the narrow gaps between cliff faces.
                Point2D origin = draft.Waypoints[0].Position;
                var ground = new HashSet<Point2D>(zone);
                var parents = new Dictionary<Point2D, Point2D>();
                Grow(origin, cells.Count, ground.Contains, (_, _) => 0, CanCrossGrade, (from, to) => parents.Add(to, from));

                foreach (Point2D destination in rampLandings.Concat(draft.Waypoints.Select(waypoint => waypoint.Position)))
                {
                    Point2D point = destination;
                    while (parents.TryGetValue(point, out Point2D previous) && groundRoutes.Add(point))
                        point = previous;
                }

                protectedGround.UnionWith(groundRoutes);
            }

            private double SpreadDistance(Point2D first, Point2D second)
                => Distance(first, second) + (draft.GetTileOrFail(first).Level != draft.GetTileOrFail(second).Level ? 20 : 0);

            private void BuildLights(Action<string, Map, int> trace, Action<MapTile> recalculateRamp)
            {
                SeedFloodlights(recalculateRamp);
                trace?.Invoke("lights", draft, random.DrawCount);
            }

            private void SeedFloodlights(Action<MapTile> recalculateRamp)
            {
                if (!UsesNativeStarts && settings.Time < 2)
                    return;

                BuildingType light = source.Rules.BuildingTypes.Find(type => string.Equals(type.ININame, "GALITE", StringComparison.OrdinalIgnoreCase));
                House owner = source.FindHouse("Special");
                if (light == null || owner == null)
                    return;

                if (UsesNativeStarts)
                {
                    SeedNativeFloodlights(light, owner, recalculateRamp);
                    return;
                }

                int count = settings.Time == 3 ? 4 : 2;
                foreach (var waypoint in draft.Waypoints)
                {
                    for (int attempt = 0; attempt < 21; attempt++)
                    {
                        double angle = random.Fraction() * 2 * Math.PI;
                        var positions = new Point2D[count];
                        for (int i = 0; i < count; i++, angle += 2 * Math.PI / count)
                        {
                            positions[i] = waypoint.Position + new Point2D((int)Math.Round(8 * Math.Cos(angle)), (int)Math.Round(8 * Math.Sin(angle)));
                        }

                        if (!positions.All(p => InLocal(p, 1) && Bare(p) && !rampApproaches.Contains(p) && !groundRoutes.Contains(p)))
                            continue;

                        foreach (Point2D point in positions)
                            draft.PlaceBuilding(new Structure(light) { Owner = owner, Position = point, HP = 256 });

                        break;
                    }
                }
            }

            private void SeedNativeFloodlights(BuildingType light, House owner, Action<MapTile> recalculateRamp)
            {
                const double fullTurn = 360.0 * Math.PI / 180.0;
                int count = settings.Time switch { 2 => 2, 3 => 4, _ => 0 };
                var positions = new Point2D[count];

                foreach (Waypoint waypoint in draft.Waypoints)
                {
                    for (int attempt = 0; attempt <= 20; attempt++)
                    {
                        // The engine picks an angle even when a morning or afternoon ring is empty.
                        double angle = random.Fraction() * fullTurn;
                        for (int i = 0; i < count; i++)
                            positions[i] = NativeFloodlightPosition(waypoint.Position, i * fullTurn / count + angle);

                        // Scenario initialization permits building on any terrain. The ring
                        // still has to fit inside the full map diamond.
                        if (!positions.All(draft.IsCoordWithinMap))
                            continue;

                        foreach (Point2D point in positions)
                        {
                            draft.PlaceBuilding(new Structure(light) { Owner = owner, Position = point, HP = 256 });
                            // The stock floodlight has a one-cell footprint, which the engine refreshes on placement.
                            recalculateRamp?.Invoke(draft.GetTileOrFail(point));
                        }

                        break;
                    }
                }
            }

            private static Point2D NativeFloodlightPosition(Point2D origin, double angle)
            {
                const int leptonsPerCell = 256;
                const int radius = 8 * leptonsPerCell;
                const double angleUnit = (360.0 / 65534.0) * Math.PI / 180.0;

                // Move_Coord converts through the engine's signed 16-bit direction.
                short direction = unchecked((short)(int)((angle - Math.PI / 2.0) / -angleUnit));
                double radians = (direction - 16383) * -angleUnit;
                int x = (int)(origin.X * leptonsPerCell + leptonsPerCell / 2 + Math.Cos(radians) * radius);
                int y = (int)(origin.Y * leptonsPerCell + leptonsPerCell / 2 - Math.Sin(radians) * radius);
                return new Point2D(x / leptonsPerCell, y / leptonsPerCell);
            }

            private void BuildTiberium(List<Point2D> sites, Action<string, Map, int> trace, Action<MapTile> recalculateRamp)
            {
                trace?.Invoke("veinholes", draft, random.DrawCount);
                if (UsesNativeStarts)
                {
                    // OpenTS forces blue Tiberium on when Firestorm is enabled.
                    int wildlife = hasFirestorm ? settings.TiberiumWildlife : 0;
                    new NativeTiberium(draft, source, random, clearTile, hasFirestorm, wildlife, protectedGround, recalculateRamp)
                        .Place(sites, settings.Tiberium);
                    if (recalculateRamp != null)
                    {
                        foreach (MapTile cell in InIsometricOrder(draft))
                            recalculateRamp(cell);
                    }
                }
                else
                {
                    SeedTiberium(sites);
                }

                trace?.Invoke("tiberium", draft, random.DrawCount);
            }

            private void SeedTiberium(List<Point2D> sites)
            {
                var tiberium = source.Rules.OverlayTypes.Where(overlay => overlay.Tiberium && overlay.ININame.Length == 5 &&
                    overlay.ININame.StartsWith("TIB", StringComparison.Ordinal) && int.TryParse(overlay.ININame.AsSpan(3), out _)).ToArray();
                if (tiberium.Length == 0)
                    throw new InvalidOperationException(Text("MissingTiberium", "The loaded rules lack the stock green Tiberium overlays."));

                var trees = source.Rules.TerrainTypes.Where(terrain => terrain.SpawnsTiberium && terrain.ININame.StartsWith("TIBT", StringComparison.Ordinal)).ToArray();
                if (sites.Count > 0)
                {
                    int amount = settings.Players * (30 * settings.Tiberium + 2500) / sites.Count;
                    foreach (Point2D site in sites)
                        SeedField(site, amount + (int)random.TruncatedNormal(0, 50, -100, 100), tiberium, trees, random.Fraction() < 0.25);
                }

                var starts = draft.Waypoints.Select(waypoint => waypoint.Position).ToArray();
                var meanDistances = starts.Select(start => sites.Count == 0 ? 0 : sites.Average(site => Distance(start, site))).ToArray();
                double closest = meanDistances.Min();
                for (int i = 0; i < starts.Length; i++)
                    SeedField(starts[i], 500 + (int)((meanDistances[i] - closest) * 15), tiberium, trees, true);
            }

            private void SeedField(Point2D site, int amount, OverlayType[] types, TerrainType[] trees, bool placeTree)
            {
                // The native amount measures growth units. Artwork has a range of
                // density frames, so convert it to a cell budget for this port.
                int cellBudget = Math.Max(1, amount / 10);
                var field = Grow(site, cellBudget + protectedGround.Count,
                    p => InLocal(p, 1) && (Bare(p) || draft.GetTileOrFail(p).Overlay?.OverlayType.Tiberium == true),
                    (p, _) => Distance(site, p) + random.Fraction() * 5)
                    .Where(p => !protectedGround.Contains(p) && Bare(p)).Take(cellBudget).ToArray();

                foreach (Point2D point in field)
                {
                    draft.GetTileOrFail(point).Overlay = new Overlay
                    {
                        Position = point,
                        OverlayType = types[random.Pick(0, types.Length - 1)],
                        FrameIndex = 0
                    };
                }

                if (placeTree && field.Length > 4 && trees.Length > 0)
                {
                    draft.GetTileOrFail(field[0]).Overlay = null;
                    draft.AddTerrainObject(new TerrainObject(trees[random.Pick(0, trees.Length - 1)], field[0]));
                }
            }

            private NativeScenery BuildSettlements(Action<string, Map, int> trace, Action<MapTile> recalculateAttributes)
            {
                var scenery = new NativeScenery(this, recalculateAttributes);
                if (settings.Cities > 0)
                    scenery.Place(settings.Cities);

                trace?.Invoke("settlements", draft, random.DrawCount);
                return scenery;
            }

            private void BuildHills(NativeScenery scenery, Action<string, Map, int> trace)
            {
                scenery.GenerateHills(settings.Hills);

                trace?.Invoke("hills", draft, random.DrawCount);
            }

            private void BuildVegetation(NativeScenery scenery, Action<string, Map, int> trace)
            {
                scenery.GenerateVegetation(settings.Vegetation, settings.Width, settings.Height);

                trace?.Invoke("vegetation", draft, random.DrawCount);
            }

            private void BlendEditorTiberium()
            {
                foreach (Point2D point in cells)
                {
                    MapTile cell = draft.GetTileOrFail(point);
                    if (cell.Overlay?.OverlayType.Tiberium == true)
                        cell.Overlay.FrameIndex = draft.GetOverlayFrameIndex(point);
                }
            }

            private void BlendNativeTiberium()
            {
                TiberiumType treeTiberium = source.Rules.TiberiumTypes[0];
                draft.DoForAllValidTiles(cell =>
                {
                    TiberiumType type = cell.Overlay?.OverlayType.TiberiumType;
                    if (type == null)
                        return;

                    Point2D point = cell.CoordsToPoint();
                    int count = 0;
                    foreach (Point2D offset in adjacent)
                    {
                        MapTile neighbor = draft.GetTile(point + offset);
                        TiberiumType adjacentType = neighbor?.Overlay?.OverlayType.TiberiumType;
                        if (adjacentType == null && neighbor?.TerrainObject?.TerrainType.SpawnsTiberium == true)
                            adjacentType = treeTiberium;

                        if (adjacentType == type)
                            count++;
                    }

                    cell.Overlay.FrameIndex = nativeTiberiumFrames[count];
                });
            }

            private sealed class NativeScenery
            {
                private readonly Map map;
                private readonly Map source;
                private readonly GeneratorRandom random;
                private readonly EnvironmentType environment;
                private readonly int clearTile;
                private readonly bool impassableCliffBacks;
                private readonly IniFile config;
                private readonly IniFile rules;
                private readonly IniFile firestorm;
                private readonly HashSet<Point2D> protectedGround;
                private readonly Func<int, ITileImage> currentTile;
                private readonly Action<MapTile> recalculateAttributes;
                private readonly MapTile[] cellGrid;
                private readonly int stride;
                private readonly bool startSlotProtected;
                private readonly Dictionary<string, int> tileStarts = new Dictionary<string, int>();
                private readonly HashSet<Point2D> rockLand = new HashSet<Point2D>();
                private readonly Dictionary<Point2D, RampType> stampedRamps = new Dictionary<Point2D, RampType>();

                public NativeScenery(Generator generator, Action<MapTile> recalculateAttributes)
                {
                    map = generator.draft;
                    source = generator.source;
                    random = generator.random;
                    environment = generator.settings.Environment;
                    clearTile = generator.clearTile;
                    impassableCliffBacks = generator.impassableCliffBacks;
                    config = generator.config;
                    rules = generator.rules;
                    firestorm = generator.firestorm;
                    protectedGround = generator.protectedGround;
                    currentTile = generator.CurrentTile;
                    this.recalculateAttributes = recalculateAttributes;

                    stride = map.Size.X + map.Size.Y + 1;
                    startSlotProtected = generator.startSampleSlotProtected;
                    cellGrid = new MapTile[stride * stride];
                    cellGrid[0] = generator.startSampleSlot;
                    map.DoForAllValidTiles(cell =>
                    {
                        cellGrid[cell.X + stride * cell.Y] = cell;
                        UpdateRockLand(cell, TilePart(cell));
                    });
                }

                public void GenerateHills(int ruggedness)
                {
                    var heights = new double[cellGrid.Length];
                    int shore = TileStart("ShorePieces");
                    foreach (MapTile cell in InIsometricOrder(map))
                    {
                        bool isShore = InTileSet(cell.TileIndex, shore, 42);
                        if (!isShore && !IsCliffTile(cell))
                            continue;

                        foreach (Point2D offset in adjacent)
                        {
                            Point2D point = cell.CoordsToPoint() + offset;
                            if (map.GetTile(point) == null || !IsClear(point))
                                continue;

                            if (isShore)
                                heights[CellSlot(point)] = 0.5;
                            protectedGround.Add(point);
                            break;
                        }
                    }

                    double minimum = ruggedness * 0.0025;
                    if (minimum >= 0.005)
                    {
                        double maximum = ruggedness * 0.005;
                        double step = ruggedness * 0.001 + 0.1;
                        var volatility = new double[heights.Length];
                        for (int index = 0; index < cellGrid.Length; index++)
                        {
                            MapTile cell = cellGrid[index];
                            if (cell == null)
                                continue;

                            Point2D point = cell.CoordsToPoint();
                            int northwest = NeighborSlot(point + adjacent[7]);
                            int west = NeighborSlot(point + adjacent[6]);
                            int north = NeighborSlot(point + adjacent[0]);
                            double up = northwest >= 0 ? heights[northwest] : 0.0;
                            double trend = 0.0;
                            if (west >= 0)
                            {
                                heights[index] += heights[west];
                                volatility[index] = volatility[west];
                                trend += heights[west] - up;
                            }
                            else
                            {
                                volatility[index] = minimum;
                            }

                            if (north >= 0)
                            {
                                heights[index] += heights[north];
                                volatility[index] += volatility[north];
                                trend += heights[north] - up;
                            }
                            else
                            {
                                volatility[index] += minimum;
                            }

                            heights[index] *= 0.5;
                            volatility[index] *= 0.5;
                            volatility[index] += random.NativeTruncatedNormal(0, 0.005, -volatility[index], maximum - volatility[index]);
                            bool isProtected = index == 0 ? startSlotProtected : protectedGround.Contains(point);
                            if (!isProtected)
                            {
                                double lower = -2.0 - heights[index];
                                double upper = 2.0 - heights[index];
                                trend = Math.Clamp(Math.Sign(trend) * step, lower, upper);
                                heights[index] += random.NativeTruncatedNormal(trend, volatility[index], lower, upper);
                            }
                        }

                        for (int index = 0; index < heights.Length; index++)
                            heights[index] = (int)(heights[index] + Math.Sign(heights[index]) * 0.5);
                    }

                    var deformer = new NativeHillDeformer(map, clearTile, TileStart("RampBase"), GetRamp, CanDeform, SetRamp);
                    deformer.Apply(cellGrid.Select((cell, index) => (cell, heights[index])).Where(entry => entry.cell != null));
                }

                private int CellSlot(Point2D point) => point.X + point.Y * stride;
                private int NeighborSlot(Point2D point) => map.GetTile(point) == null ? -1 : CellSlot(point);

                private bool CanDeform(MapTile cell) =>
                    cell.Overlay == null && !IsOccupied(cell) && !protectedGround.Contains(cell.CoordsToPoint()) &&
                    (cell.TileIndex == ushort.MaxValue || map.IsCellMorphable(cell));

                private void SetRamp(MapTile cell, RampType ramp) => stampedRamps[cell.CoordsToPoint()] = ramp;

                private bool IsCliffTile(MapTile cell)
                {
                    int tile = cell.TileIndex;
                    if (InTileSet(tile, TileStart("CliffSet"), 40))
                        return true;

                    return IsWaterfallCliff(cell, "WaterfallEast", 0, 4) ||
                        IsWaterfallCliff(cell, "WaterfallWest", 1, 3) ||
                        IsWaterfallCliff(cell, "WaterfallSouth", 0, 1) ||
                        IsWaterfallCliff(cell, "WaterfallNorth", 2, 3) ||
                        InTileSet(tile, TileStart("CliffRamps"), 20) ||
                        InTileSet(tile, TileStart("WaterCaves"), 8) ||
                        InTileSet(tile, TileStart("BridgeSet"), 16) ||
                        InTileSet(tile, TileStart("TrainBridgeSet"), 16) ||
                        InTileSet(tile, TileStart("DestroyableCliffs"), 2) ||
                        InTileSet(tile, TileStart("WaterCliffs"), 28);
                }

                private bool IsWaterfallCliff(MapTile cell, string role, int firstWaterSubtile, int secondWaterSubtile)
                {
                    int start = TileStart(role);
                    if (!InTileSet(cell.TileIndex, start, 4))
                        return false;

                    return (cell.TileIndex != start && cell.TileIndex != start + 3) ||
                        (cell.SubTileIndex != firstWaterSubtile && cell.SubTileIndex != secondWaterSubtile);
                }

                private static bool InTileSet(int tile, int start, int count) => start >= 0 && IsRoadTileInRange(tile, start, count);

                public void GenerateVegetation(int densitySetting, int widthSetting, int heightSetting)
                {
                    double density = densitySetting * 0.01;
                    bool arctic = environment is EnvironmentType.Tundra or EnvironmentType.Taiga;
                    if (environment == EnvironmentType.Mutated)
                        GenerateMutatedVegetation(widthSetting, heightSetting);

                    if (environment != EnvironmentType.Temperate)
                        ScatterRoughGround();

                    if (arctic)
                        GenerateArcticGroundCover(density);
                    else
                        GenerateTemperateGroundCover(density);

                    foreach (MapTile cell in InIsometricOrder(map))
                        FixupVegetationLAT(cell);

                    if (!arctic)
                        ScatterRockOverlays();
                }

                private void GenerateTemperateGroundCover(double density)
                {
                    bool desert = environment == EnvironmentType.Desert;
                    double greenChance = desert ? 0.0 : density * 0.02;
                    double roughChance = desert ? 0.01 : 0.005;
                    double sandChance = desert ? 0.03 : 0.0;
                    double forestChance = density * (desert ? 0.001 : 0.003);
                    int green = TileStart("GreenTile");
                    int rough = TileStart("RoughTile");
                    int sand = TileStart("SandTile");

                    foreach (MapTile cell in InIsometricOrder(map))
                    {
                        Point2D point = cell.CoordsToPoint();
                        if (!IsClear(point) || GetRamp(cell) != RampType.None || cell.Overlay != null ||
                            IsOccupied(cell) || protectedGround.Contains(point))
                        {
                            continue;
                        }

                        if (random.Fraction() < greenChance)
                            PlaceTilePatch(cell, green, (int)random.NativeTruncatedNormal(desert ? 20 : 30, 20, 4, 80));
                        else if (random.Fraction() < roughChance)
                            PlaceTilePatch(cell, rough, (int)random.NativeTruncatedNormal(20, 20, 4, 80));
                        else if (random.Fraction() < sandChance)
                            PlaceTilePatch(cell, sand, (int)random.NativeTruncatedNormal(desert ? 40 : 20, 20, 4, 80));
                        else if (random.Fraction() < forestChance)
                            SeedForest(cell, 25, 35);
                    }
                }

                private void GenerateArcticGroundCover(double density)
                {
                    bool tundra = environment == EnvironmentType.Tundra;
                    double forestChance = density * (tundra ? 0.0005 : 0.0015);
                    double rockChance = tundra ? 0.002 : 0.001;
                    int rough = TileStart("RoughTile");
                    int rocks = TileStart("Rocks");
                    foreach (MapTile cell in InIsometricOrder(map))
                    {
                        Point2D point = cell.CoordsToPoint();
                        if (!IsClear(point) || GetRamp(cell) != RampType.None || protectedGround.Contains(point))
                            continue;

                        if (random.Fraction() < 0.005)
                            PlaceTilePatch(cell, rough, (int)random.NativeTruncatedNormal(20, 15, 4, 60));
                        if (random.Fraction() < forestChance && !forested.Contains(point))
                            SeedForest(cell, 30, 45);
                        if (random.Fraction() < rockChance)
                            PlaceTilePatch(cell, rocks, (int)random.NativeTruncatedNormal(20, 11, 3, 30));
                    }
                }

                private void SeedForest(MapTile cell, int mean, int maximum)
                {
                    // OpenTS evaluates the density argument before the count on Win32.
                    double density = random.NativeTruncatedNormal(0.2, 0.1, 0.05, 0.4);
                    int count = (int)random.NativeTruncatedNormal(mean, 10, 10, maximum);
                    PlaceForest(cell, count, density);
                }

                private void ScatterRockOverlays()
                {
                    const int sandRocks = 168;
                    const int clearRocks = 173;
                    int count = random.Pick(0, 2 * map.Size.X * (map.Size.Y + 4) / 100);
                    if (environment == EnvironmentType.Temperate)
                        count /= 2;

                    int sandLAT = TileStart("ClearToSandLat");
                    int greenLAT = TileStart("ClearToGreenLat");
                    int placed = 0;
                    for (int attempt = 0; attempt < count * 5 && placed < count; attempt++)
                    {
                        MapTile cell = PickMapCell();
                        if (cell.Overlay != null)
                            continue;

                        Point2D point = cell.CoordsToPoint();
                        int start = -1;
                        if (InTileSet(cell.TileIndex, sandLAT, 16))
                            start = sandRocks;
                        else if (IsClear(point) || InTileSet(cell.TileIndex, greenLAT, 16))
                            start = clearRocks;

                        if (start >= 0)
                            cell.Overlay = new Overlay { Position = point, OverlayType = source.Rules.OverlayTypes[start + random.Pick(0, 4)] };

                        RecalculateVegetationCell(cell);
                        placed++;
                    }
                }

                private void PlaceVegetationTerrain(string name, MapTile cell)
                {
                    TerrainType type = source.Rules.TerrainTypes.Find(candidate => candidate.ININame == name) ??
                        throw new InvalidOperationException($"The loaded rules lack the stock terrain object {name}.");
                    map.AddTerrainObject(new TerrainObject(type, cell.CoordsToPoint()));
                    RecalculateVegetationCell(cell);
                }

                public void FinishVegetationTerrain()
                {
                    foreach (MapTile cell in InIsometricOrder(map))
                        RecalculateVegetationCell(cell);
                }

                private void RecalculateVegetationCell(MapTile cell)
                {
                    TmpImage part = TilePart(cell);
                    SetRamp(cell, part?.RampType ?? RampType.None);
                    if (part == null)
                    {
                        cell.ChangeTileIndex(clearTile, 0);
                    }
                    else
                    {
                        FixupVegetationLAT(cell);
                        if (part.RampType > RampType.South && cell.Overlay?.OverlayType.Tiberium == true)
                            cell.Overlay = null;
                    }

                    UpdateRockLand(cell, part);
                }

                private void FixupVegetationLAT(MapTile cell)
                {
                    FixupNativeGroundCover(map, cell, TileStart("RoughTile"), TileStart("ClearToRoughLat"));
                    FixupNativeGroundCover(map, cell, TileStart("SandTile"), TileStart("ClearToSandLat"));
                    FixupNativeGroundCover(map, cell, TileStart("GreenTile"), TileStart("ClearToGreenLat"));
                    FixupPavementLAT(cell);
                    FixupNativeGroundCover(map, cell, TileStart("CrystalTile"), TileStart("ClearToCrystalLat"), IsCrystalNeighbor);
                    FixupNativeSwampLAT(map, cell, TileStart("SwampTile"), TileStart("WaterToSwampLat"));
                    FixupNativeGroundCover(map, cell, TileStart("BlueMoldTile"), TileStart("ClearToBlueMoldLat"));

                    int rampBase = TileStart("RampBase");
                    int rampSmooth = TileStart("RampSmooth");
                    if (!InTileSet(cell.TileIndex, rampBase, 4) && !InTileSet(cell.TileIndex, rampSmooth, 12))
                        return;

                    Point2D point = cell.CoordsToPoint();
                    RampType ramp = GetRamp(cell);
                    int mask = 0;
                    if (ramp is >= RampType.West and <= RampType.South)
                    {
                        int direction = ((int)ramp + 2) % 4 * 2;
                        if (GetRamp(map.GetTile(point + adjacent[direction])) == RampType.None)
                            mask |= 1;
                        if (GetRamp(map.GetTile(point + adjacent[(direction + 4) % 8])) == RampType.None)
                            mask |= 2;
                    }

                    int rampOffset = (int)ramp - 1;
                    cell.TileIndex = mask == 0 ? rampBase + rampOffset : rampSmooth + 3 * rampOffset + mask - 1;
                }

                private bool IsCrystalNeighbor(MapTile cell)
                {
                    int crystalCliff = TileStart("CrystalCliff");
                    if (crystalCliff < 0)
                        return false;

                    return (cell.TileIndex - crystalCliff) switch
                    {
                        0 => cell.SubTileIndex < 2,
                        1 => cell.SubTileIndex >= 2,
                        4 => (cell.SubTileIndex & 1) == 0,
                        5 => (cell.SubTileIndex & 1) != 0,
                        _ => false
                    };
                }

                private readonly HashSet<Point2D> forested = new HashSet<Point2D>();
                private readonly Dictionary<Point2D, Point2D> vegetationPatchOrigins = new Dictionary<Point2D, Point2D>();

                private void ScatterRoughGround()
                {
                    int roughGround = TileStart("RoughGround");
                    dirtRoadRampStart = TileStart("RampBase");
                    dirtRoadRampSmoothStart = TileStart("RampSmooth");

                    int count = random.Pick(5, 30);
                    int placed = 0;
                    for (int attempt = 0; attempt < count * 20 && placed < count; attempt++)
                    {
                        int tile = roughGround + random.Pick(0, 9);
                        Point2D point = PickMapCell().CoordsToPoint();
                        if (!CanPlaceTerrainTile(tile, point, false, false))
                            continue;

                        PlaceTile(tile, point);
                        placed++;
                    }
                }

                private void PlaceForest(MapTile origin, int count, double density)
                {
                    Point2D seed = origin.CoordsToPoint();
                    var queue = new NativePriorityQueue<Point2D>();
                    forested.Add(seed);
                    queue.Add(seed, 0f);

                    int minTree = environment == EnvironmentType.Desert || environment == EnvironmentType.Mutated ? 21 : 1;
                    int maxTree = environment == EnvironmentType.Temperate ? 20 : 25;
                    for (int visited = 0; queue.TryTake(out Point2D point) && visited < count; visited++)
                    {
                        MapTile cell = map.GetTileOrFail(point);
                        if (IsClear(point) && !IsOccupied(cell) && cell.Overlay == null && !IsLandRock(cell)
                            && random.Fraction() < density)
                        {
                            int tree = random.Pick(minTree, maxTree);
                            string name = "TREE" + tree.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                            PlaceVegetationTerrain(name, cell);
                        }

                        foreach (Point2D offset in adjacent)
                        {
                            Point2D neighbor = point + offset;
                            if (map.GetTile(neighbor) == null || protectedGround.Contains(neighbor) || !forested.Add(neighbor))
                                continue;

                            // The caller marks the whole frontier, including cells left in the queue.
                            queue.Add(neighbor, VegetationSpreadScore(seed, neighbor));
                        }
                    }
                }

                private void PlaceTilePatch(MapTile origin, int tile, int count)
                {
                    Point2D seed = origin.CoordsToPoint();
                    var queue = new NativePriorityQueue<Point2D>();
                    vegetationPatchOrigins[seed] = seed;
                    queue.Add(seed, 0f);

                    for (int visited = 0; queue.TryTake(out Point2D point) && visited < count; visited++)
                    {
                        MapTile cell = map.GetTileOrFail(point);
                        cell.ChangeTileIndex(tile, cell.SubTileIndex);

                        foreach (Point2D offset in adjacent)
                        {
                            Point2D neighbor = point + offset;
                            MapTile next = map.GetTile(neighbor);
                            if (next == null || !IsClear(neighbor)
                                || (vegetationPatchOrigins.TryGetValue(neighbor, out Point2D priorSeed) && priorSeed == seed)
                                || GetRamp(next) != RampType.None || next.Overlay != null || IsOccupied(next))
                            {
                                continue;
                            }

                            // Arctic rough ground and rocks reuse their seed's stamp for cells still in the frontier.
                            vegetationPatchOrigins[neighbor] = seed;
                            queue.Add(neighbor, VegetationSpreadScore(seed, neighbor));
                        }
                    }
                }

                private float VegetationSpreadScore(Point2D origin, Point2D neighbor)
                {
                    return (float)((random.NextValue() * 5.0) * (1.0 / uint.MaxValue) + Distance(origin, neighbor));
                }

                private void GenerateMutatedVegetation(int widthSetting, int heightSetting)
                {
                    int mapSize = (widthSetting + 1) * (heightSetting + 1);
                    int molds = random.Pick(3, mapSize / 4 + 4);
                    for (int i = 0; i < molds; i++)
                        GenerateMutatedMold();

                    int cliffStart = TileStart("CliffSet");
                    var cliffs = new List<Point2D>();
                    foreach (MapTile cell in InIsometricOrder(map))
                    {
                        int cliff = cell.TileIndex - cliffStart;
                        if (cell.SubTileIndex == 0 && IsCliffTile(cell) &&
                            (cliff is >= 4 and <= 6 or >= 14 and <= 16))
                        {
                            cliffs.Add(cell.CoordsToPoint());
                        }
                    }

                    int crystals = random.Pick(6, mapSize + 8);
                    for (int i = 0; i < crystals; i++)
                    {
                        if (random.Pick(0, 1) == 0 && cliffs.Count > 0)
                        {
                            int index = random.Pick(0, cliffs.Count - 1);
                            GenerateMutatedCrystals(cliffs[index], cliffStart);
                            cliffs.RemoveAt(index);
                        }
                        else if (PickClearMapCell() is MapTile cell)
                        {
                            GenerateMutatedCrystals(cell.CoordsToPoint(), cliffStart);
                        }
                    }
                }

                private void GenerateMutatedMold()
                {
                    MapTile origin = null;
                    for (int attempt = 0; attempt < 200; attempt++)
                    {
                        MapTile candidate = PickClearMapCell();
                        if (candidate != null &&
                            IsMutatedAreaClear(new Rectangle(candidate.X - 1, candidate.Y - 1, 3, 3), true))
                        {
                            origin = candidate;
                            break;
                        }
                    }

                    if (origin == null)
                        return;

                    int mold = TileStart("BlueMoldTile");
                    List<Point2D> cells = GrowMutatedGround(origin.CoordsToPoint(), null, mold, true);
                    int foliage = random.Pick(0, 4);
                    for (int i = 0; i < foliage; i++)
                    {
                        MapTile cell = PickMutatedFoliageCell(cells, mold, false);
                        if (cell == null)
                            break;
                        if (cell.TerrainObject != null || cell.Overlay != null)
                            continue;

                        if (random.Fraction() < 0.75)
                        {
                            int variant = random.Pick(1, 5);
                            PlaceVegetationTerrain("FONA" + variant.ToString("D2"), cell);
                        }
                        else
                        {
                            OverlayType type = source.Rules.OverlayTypes.First(overlay => overlay.ININame == "BTIB01");
                            cell.Overlay = new Overlay { Position = cell.CoordsToPoint(), OverlayType = type, FrameIndex = 0 };
                        }
                    }
                }

                private void GenerateMutatedCrystals(Point2D point, int cliffStart)
                {
                    Point2D origin = point;
                    Point2D? secondSeed = null;
                    if (!IsClear(point))
                    {
                        int cliff = TileAt(point) - cliffStart;
                        bool horizontal = cliff is >= 4 and <= 6;
                        if (!horizontal && (cliff < 14 || cliff > 16))
                            return;

                        Point2D across = horizontal ? new Point2D(0, 1) : new Point2D(1, 0);
                        Point2D along = horizontal ? new Point2D(1, 0) : new Point2D(0, 1);
                        int stripWidth = horizontal ? 2 : 1;
                        int stripHeight = horizontal ? 1 : 2;
                        int variant = horizontal ? 0 : 4;
                        Point2D before = point - across;
                        if (IsMutatedAreaClear(new Rectangle(before.X, before.Y, stripWidth, stripHeight), false) &&
                            random.Pick(0, 1) == 0)
                        {
                            origin = before;
                        }
                        else
                        {
                            Point2D after = point + across + across;
                            if (!IsMutatedAreaClear(new Rectangle(after.X, after.Y, stripWidth, stripHeight), false))
                                return;

                            origin = after;
                            variant++;
                        }

                        int baseHeight = (map.GetTile(point + across)?.Level ?? 0) + 4;
                        PlaceTile(TileStart("CrystalCliff") + variant, point, baseHeight);
                        secondSeed = origin + along;
                    }

                    int crystal = TileStart("CrystalTile");
                    List<Point2D> cells = GrowMutatedGround(origin, secondSeed, crystal, false);
                    int foliage = random.Pick(0, 5);
                    for (int i = 0; i < foliage; i++)
                    {
                        MapTile cell = PickMutatedFoliageCell(cells, crystal, true);
                        if (cell == null)
                            break;

                        if (cell.TerrainObject == null)
                        {
                            int variant = random.Pick(6, 15);
                            PlaceVegetationTerrain("FONA" + variant.ToString("D2"), cell);
                        }
                    }
                }

                private List<Point2D> GrowMutatedGround(Point2D origin, Point2D? secondSeed, int tile, bool mold)
                {
                    int target = random.Pick(30, 150);
                    int insertLimit = Math.Max(100, 6 * target);
                    var pending = new NativePriorityQueue<Point2D>();
                    int inserted = 1;
                    if (secondSeed.HasValue)
                    {
                        pending.Add(secondSeed.Value, 0);
                        inserted++;
                    }

                    pending.Add(origin, 0);
                    var cells = new List<Point2D>(target);
                    for (int step = 0; step < target && pending.TryTake(out Point2D point); step++)
                    {
                        map.GetTileOrFail(point).ChangeTileIndex(tile, 0);
                        cells.Add(point);

                        // Native spread marks are written but never used for deduplication.
                        for (int facing = 0; facing < adjacent.Length; facing += 2)
                        {
                            Point2D next = point + adjacent[facing];
                            MapTile cell = map.GetTile(next);
                            if (cell == null || inserted >= insertLimit)
                                continue;

                            bool canSpread = mold
                                ? CanSpreadMutatedMold(next, tile)
                                : IsClear(next) && cell.Overlay == null;
                            if (canSpread)
                            {
                                pending.Add(next, MutatedSpreadScore(origin, next, step));
                                inserted++;
                            }
                        }
                    }

                    foreach (Point2D point in cells)
                        FixupVegetationLAT(map.GetTileOrFail(point));

                    return cells;
                }

                private bool CanSpreadMutatedMold(Point2D point, int mold)
                {
                    for (int facing = 0; facing < adjacent.Length; facing += 2)
                    {
                        Point2D neighbor = point + adjacent[facing];
                        if (!IsClear(neighbor) && TileAt(neighbor) != mold)
                            return false;
                    }

                    return !protectedGround.Contains(point);
                }

                private float MutatedSpreadScore(Point2D origin, Point2D point, int step)
                {
                    float distance = (float)Distance(origin, point);
                    float randomWeight = 10f * (float)random.Fraction();
                    return randomWeight + distance * 0.5f - step * 0.02f;
                }

                private MapTile PickMutatedFoliageCell(List<Point2D> cells, int tile, bool ensureUnprotected)
                {
                    for (int attempt = 0; attempt < 200; attempt++)
                    {
                        Point2D point = cells[random.Pick(0, cells.Count - 1)];
                        MapTile cell = map.GetTileOrFail(point);
                        if (cell.TileIndex == tile && (!ensureUnprotected || !protectedGround.Contains(point)))
                            return cell;
                    }

                    return null;
                }

                private bool IsMutatedAreaClear(Rectangle area, bool ensureUnprotected)
                {
                    for (int y = area.Top; y < area.Bottom; y++)
                    {
                        for (int x = area.Left; x < area.Right; x++)
                        {
                            var point = new Point2D(x, y);
                            MapTile cell = map.GetTile(point);
                            if (cell == null || !IsClear(point) || cell.Overlay != null ||
                                ensureUnprotected && protectedGround.Contains(point))
                            {
                                return false;
                            }
                        }
                    }

                    return true;
                }

                public void Place(int cities)
                {
                    InitDirtRoads();
                    if (environment is EnvironmentType.Tundra or EnvironmentType.Taiga)
                        GenerateRuralAreas((int)((cities + 0.49) * 2));
                    else
                        GenerateUrbanAreas(cities);
                }

                public void FinishTerrain()
                {
                    // The engine refreshes pavement transitions after all settlements are placed.
                    foreach (MapTile cell in InIsometricOrder(map))
                        RecalculateSettlementCell(cell);
                }

                private void GenerateUrbanAreas(int cities)
                {
                    int count = (int)((cities + 0.32) * 3);
                    int attempts = 0;
                    int placed = 0;

                    while (placed < count && attempts < 10)
                    {
                        bool found = FindSettlementSite(point => !protectedGround.Contains(point) || AreaIsCliffFree(point),
                            out Point2D origin);
                        if (random.Fraction() < 1.0 - cities)
                        {
                            if (PlaceRuralArea(origin))
                                placed++;
                            continue;
                        }

                        attempts++;
                        if (!found)
                            continue;

                        int mean = 2 * map.Size.X * map.Size.Y / 30;
                        int size = (int)random.NativeTruncatedNormal(mean, 300, 100, 2000);
                        List<Point2D> area = CreateUrbanArea(origin, size);
                        if (area == null)
                            continue;

                        GenerateUrbanRoads(area);
                        GenerateUrbanBuildings(area);
                        GenerateUrbanUnits(area);
                        GenerateUrbanPavement(area);
                        placed++;
                    }
                }

                private void GenerateRuralAreas(int count)
                {
                    int placed = 0;
                    for (int attempt = 0; attempt < 10 && placed < count; attempt++)
                    {
                        bool found = FindSettlementSite(point => !protectedGround.Contains(point) &&
                            CanPlacePavedRoad(new Rectangle(point.X - 4, point.Y - 4, 8, 8), false, false) &&
                            IsInLocalRadar(point), out Point2D origin);

                        if (found && PlaceRuralArea(origin))
                            placed++;
                    }
                }

                private bool PlaceRuralArea(Point2D origin)
                {
                    if (!GenerateRuralRoads(origin))
                        return false;

                    GenerateRuralBuildings(origin);
                    GenerateRuralUnits(origin);
                    return true;
                }

                private bool FindSettlementSite(Func<Point2D, bool> suitable, out Point2D origin)
                {
                    for (int attempt = 1; ; attempt++)
                    {
                        origin = PickClearMapCell()?.CoordsToPoint() ?? default;
                        if (attempt == 20)
                            return false;
                        if (suitable(origin))
                            return true;
                    }
                }

                private MapTile PickClearMapCell()
                {
                    for (int attempt = 0; attempt <= 200; attempt++)
                    {
                        MapTile cell = PickMapCell();
                        if (attempt < 200 && IsClear(cell.CoordsToPoint()))
                            return cell;
                    }

                    return null;
                }

                private MapTile PickMapCell()
                {
                    MapTile cell;
                    do
                    {
                        cell = cellGrid[random.Pick(0, cellGrid.Length - 1)];
                    }
                    while (cell == null);

                    return cell;
                }

                private bool AreaIsCliffFree(Point2D point)
                {
                    int cliff = TileStart("CliffSet");
                    int ramps = TileStart("CliffRamps");
                    for (int x = point.X - 7; x < point.X + 7; x++)
                    {
                        for (int y = point.Y - 7; y < point.Y + 7; y++)
                        {
                            int index = TileAt(new Point2D(x, y));
                            if (IsRoadTileInRange(index, cliff, 40) || IsRoadTileInRange(index, ramps, 20))
                                return false;
                        }
                    }

                    return true;
                }

                private List<Point2D> CreateUrbanArea(Point2D origin, int size)
                {
                    if (map.GetTile(origin) == null)
                        return null;

                    int pavement = TileStart("PaveTile");
                    int misc = TileStart("MiscPaveTile");
                    var area = new List<Point2D>();
                    var visited = new HashSet<Point2D> { origin };
                    var pending = new NativePriorityQueue<Point2D>();
                    pending.Add(origin, 0f);
                    while (area.Count < size && pending.TryTake(out Point2D point))
                    {
                        MapTile cell = map.GetTileOrFail(point);
                        cell.TileIndex = pavement;
                        area.Add(point);
                        foreach (Point2D offset in adjacent)
                        {
                            Point2D neighbor = point + offset;
                            MapTile other = map.GetTile(neighbor);
                            if (other == null || !IsClear(neighbor) || other.Overlay != null ||
                                protectedGround.Contains(neighbor) || !visited.Add(neighbor))
                            {
                                continue;
                            }

                            int distance = Math.Max(Math.Abs(origin.X - neighbor.X), Math.Abs(origin.Y - neighbor.Y));
                            float score = (float)((random.NextValue() * 3.0) * (1.0 / uint.MaxValue) + distance);
                            pending.Add(neighbor, score);
                        }
                    }

                    Rectangle bounds = BoundingRect(area);
                    if (area.Count <= 100 || bounds.Width * bounds.Height >= area.Count * 1.6)
                    {
                        foreach (Point2D point in area)
                            map.GetTileOrFail(point).TileIndex = clearTile;

                        return null;
                    }

                    for (int index = area.Count - 1; index >= 0; index--)
                    {
                        Point2D point = area[index];
                        foreach (Point2D offset in adjacent)
                        {
                            MapTile neighbor = map.GetTile(point + offset);
                            // Native deletion keeps using this index for the rest of the neighbors.
                            if (neighbor != null && neighbor.TileIndex != pavement && index < area.Count)
                                area.RemoveAt(index);
                        }
                    }

                    protectedGround.UnionWith(area);
                    int patches = area.Count / 3;
                    for (int patch = 0; patch < patches; patch++)
                    {
                        Point2D point = area[random.Pick(0, area.Count - 1)];
                        int variant = random.Pick(0, 6);
                        if (variant > 3 && !CanStampCityPavement(point, pavement))
                            continue;

                        PlaceTile(misc + variant, point);
                    }

                    return area;
                }

                private bool CanStampCityPavement(Point2D origin, int pavement)
                {
                    for (int x = 0; x < 2; x++)
                    {
                        for (int y = 0; y < 2; y++)
                        {
                            Point2D point = origin + new Point2D(x, y);
                            if (!protectedGround.Contains(point) || TileAt(point) != pavement)
                                return false;
                        }
                    }

                    return true;
                }

                private static Rectangle BoundingRect(IReadOnlyList<Point2D> points)
                {
                    int left = 999, top = 999, right = 0, bottom = 0;
                    foreach (Point2D point in points)
                    {
                        left = Math.Min(left, point.X);
                        top = Math.Min(top, point.Y);
                        right = Math.Max(right, point.X);
                        bottom = Math.Max(bottom, point.Y);
                    }

                    return new Rectangle(left, top, right - left + 1, bottom - top + 1);
                }

                private int TileStart(string role)
                {
                    if (!tileStarts.TryGetValue(role, out int start))
                    {
                        start = map.TheaterInstance.Theater.TryGetTileSetById(config.GetIntValue("General", role, -1))?.StartTileIndex ?? -1;
                        tileStarts.Add(role, start);
                    }

                    return start;
                }

                private int TileAt(Point2D point) => map.GetTile(point)?.TileIndex ?? ushort.MaxValue;

                private bool IsClear(Point2D point)
                {
                    int index = TileAt(point);
                    return index == clearTile || index == ushort.MaxValue;
                }

                private static bool IsOccupied(MapTile cell) =>
                    cell.TerrainObject != null || cell.Structures.Count > 0 || cell.Vehicles.Count > 0 ||
                    cell.Aircraft.Count > 0 || cell.GetFirstInfantry() != null;

                private TmpImage TilePart(MapTile cell)
                {
                    ITileImage tile = currentTile(cell.TileIndex == ushort.MaxValue ? clearTile : cell.TileIndex);
                    return cell.SubTileIndex < tile.SubTileCount ? tile.GetSubTile(cell.SubTileIndex)?.TmpImage : null;
                }

                private RampType GetRamp(MapTile cell)
                {
                    if (cell == null)
                        return RampType.None;

                    return stampedRamps.TryGetValue(cell.CoordsToPoint(), out RampType ramp)
                        ? ramp : (TilePart(cell)?.RampType ?? RampType.None);
                }

                private bool IsInLocalRadar(Point2D point) =>
                    map.GetTile(point) is MapTile cell &&
                    InNativeRadarArea(point, cell.Level, GetRamp(cell), map.Size.X, map.LocalSize);

                private void PlaceTile(int tileIndex, Point2D origin, int height = -1)
                {
                    ITileImage image = currentTile(tileIndex);
                    for (int sub = 0; sub < image.SubTileCount; sub++)
                    {
                        Point2D? offset = image.GetSubTileCoordOffset(sub);
                        if (!offset.HasValue)
                            continue;

                        Point2D point = origin + offset.Value;
                        MapTile cell = map.GetTile(point);
                        if (cell == null)
                            continue;

                        TmpImage part = image.GetSubTile(sub).TmpImage;
                        cell.ChangeTileIndex(tileIndex, (byte)sub);
                        if (height != -1)
                            cell.Level = (byte)(height + part.Height - 4);
                        stampedRamps[point] = part.RampType;
                    }
                }

                private bool IsLandRock(MapTile cell) => rockLand.Contains(cell.CoordsToPoint());

                private void UpdateRockLand(MapTile cell, TmpImage part)
                {
                    const int cliffHeight = 4;
                    Point2D point = cell.CoordsToPoint();
                    byte terrain = part?.TerrainType ?? 0;
                    OverlayType overlay = cell.Overlay?.OverlayType;
                    bool usesOverlayLand = overlay?.NoUseTileLandType == true ||
                        overlay?.Land == MapEditorLibrary.Models.Enums.LandType.Railroad;
                    bool rock = usesOverlayLand ? overlay.Land == MapEditorLibrary.Models.Enums.LandType.Rock : terrain is 7 or 8 or 15;
                    bool clearLand = !usesOverlayLand && (terrain is 0 or 1 or 2 or 3 or 4 or 9 or 10 or 13) &&
                        overlay?.Tiberium != true;
                    if (rock || (impassableCliffBacks && clearLand &&
                        cliffBackOffsets.Any(offset => map.GetTile(point + offset)?.Level >= cell.Level + cliffHeight)))
                    {
                        rockLand.Add(point);
                    }
                    else
                    {
                        rockLand.Remove(point);
                    }
                }

                private void RecalculateSettlementCell(MapTile cell)
                {
                    TmpImage part = TilePart(cell);
                    recalculateAttributes?.Invoke(cell);
                    Point2D point = cell.CoordsToPoint();
                    stampedRamps[point] = part?.RampType ?? RampType.None;
                    UpdateRockLand(cell, part);
                    FixupPavementLAT(cell);
                }

                private void FixupPavementLAT(MapTile cell)
                {
                    Point2D point = cell.CoordsToPoint();
                    int transition = TileStart("ClearToPaveLat");
                    int pavement = TileStart("PaveTile");
                    if (transition < 0 || (cell.TileIndex != pavement && !IsRoadTileInRange(cell.TileIndex, transition, 16)))
                        return;

                    int misc = TileStart("MiscPaveTile");
                    int medians = TileStart("Medians");
                    int roads = TileStart("PavedRoads");
                    int mask = 0;
                    for (int face = 0; face < adjacent.Length; face += 2)
                    {
                        int index = TileAt(point + adjacent[face]);
                        if (index != pavement && !IsRoadTileInRange(index, transition, 16) &&
                            !IsRoadTileInRange(index, misc, 14) && !IsRoadTileInRange(index, medians, 14) &&
                            !IsRoadTileInRange(index, roads, 8))
                        {
                            mask |= 1 << (face / 2);
                        }
                    }

                    cell.TileIndex = mask == 0 ? pavement : transition + mask;
                }

                private static readonly Point2D[] urbanPavementSizes =
                {
                    new(6, 4), new(4, 6), new(4, 4), new(4, 2), new(2, 4)
                };

                public void GenerateUrbanRoads(List<Point2D> cells)
                {
                    Rectangle bounds = BoundingRect(cells);
                    int roads = TileStart("PavedRoads");
                    int ends = TileStart("PavedRoadEnds");

                    if (bounds.Height > 5)
                    {
                        var start = new Point2D(bounds.Left, bounds.Top + 2);
                        int remaining = bounds.Height - 2;
                        while (remaining > 3)
                        {
                            Point2D junction = PlanPavedRoadJunction(start, bounds, false);
                            Point2D end = PlanPavedRoad(junction, bounds, false);
                            bool placed = end != Point2D.Zero && start != Point2D.Zero && end.X - start.X >= 6;
                            if (placed)
                                PlaceHorizontalUrbanRoad(junction, end, roads, ends);

                            int advance = placed ? 10 : 2;
                            start.Y += advance;
                            remaining -= advance;
                        }
                    }

                    if (bounds.Width > 8)
                    {
                        // The original uses the city's height for the eastward scan budget.
                        int remaining = bounds.Height - 5;
                        int medians = TileStart("Medians");
                        var start = new Point2D(bounds.Left + 5, bounds.Top);
                        while (remaining > 3)
                        {
                            Point2D junction = PlanPavedRoadJunction(start, bounds, true);
                            Point2D end = PlanPavedRoad(junction, bounds, true);
                            bool placed = end != Point2D.Zero && start != Point2D.Zero && end.Y - start.Y >= 6;
                            if (placed)
                                PlaceVerticalUrbanRoad(junction, end, roads, ends, medians);

                            int advance = placed ? 10 : 2;
                            start.X += advance;
                            remaining -= advance;
                        }
                    }
                }

                private void PlaceHorizontalUrbanRoad(Point2D junction, Point2D end, int roads, int ends)
                {
                    if (CanPlacePavedRoad(new Rectangle(junction.X - 4, junction.Y, 4, 3), false, false) && random.Pick(0, 1) != 0)
                        PlaceTile(roads + 9, junction - new Point2D(3, 0));
                    else
                        PlaceTile(ends + 2, junction);

                    for (int x = junction.X + 1; x < end.X; x++)
                        PlaceTile(roads, new Point2D(x, junction.Y));

                    int cap = CanPlacePavedRoad(new Rectangle(end.X, end.Y, 4, 3), false, false) && random.Pick(0, 1) != 0
                        ? roads + 10
                        : ends;
                    PlaceTile(cap, end);
                }

                private void PlaceVerticalUrbanRoad(Point2D cursor, Point2D end, int roads, int ends, int medians)
                {
                    bool inSegment = false;
                    bool initial = true;
                    bool medianPending = false;
                    bool medianFlip = false;

                    while (cursor.Y < end.Y)
                    {
                        if (!inSegment)
                        {
                            if (end.Y - cursor.Y < 2)
                                return;

                            var junction = new Rectangle(cursor.X, cursor.Y - 3, 3, 4);
                            if (initial && CanPlacePavedRoad(junction, false, false) && random.Pick(0, 1) != 0)
                            {
                                PlaceTile(roads + 13, cursor - new Point2D(0, 3));
                                inSegment = true;
                            }
                            else if (CanPlacePavedRoadJunction(junction, roads, medians))
                            {
                                PlaceTile(roads + 5, cursor - new Point2D(0, 3));
                                PlaceTile(roads + 1, cursor);
                                inSegment = true;
                            }
                            else if (CanPlacePavedRoad(new Rectangle(cursor.X, cursor.Y, 3, 2), false, false))
                            {
                                PlaceTile(ends + 1, cursor);
                                inSegment = true;
                            }
                        }
                        else if (CanPlacePavedRoad(new Rectangle(cursor.X, cursor.Y, 3, 1), false, false))
                        {
                            PlaceTile(roads + 1, cursor);
                            if (medianPending)
                            {
                                PlaceTile(medians + (medianFlip ? 5 : 4), cursor + new Point2D(1, 0));
                                medianFlip = false;
                            }
                        }
                        else
                        {
                            if (medianPending)
                            {
                                PlaceTile(medians + 3, cursor + new Point2D(1, -1));
                                medianPending = false;
                            }

                            if (CanPlacePavedRoadJunction(new Rectangle(cursor.X, cursor.Y, 3, 4), roads, medians))
                            {
                                if (end.Y - cursor.Y < 4)
                                {
                                    PlaceTile(roads + 7, cursor);
                                    return;
                                }

                                if (CanPlacePavedRoad(new Rectangle(cursor.X, cursor.Y + 3, 3, 2), false, false) && end.Y - cursor.Y > 5)
                                {
                                    medianPending = true;
                                    medianFlip = true;
                                }

                                PlaceTile(roads + 3, cursor);
                                if (CanPlacePavedRoad(new Rectangle(cursor.X - 1, cursor.Y, 1, 3), true, false))
                                    PlaceTile(medians + 12, cursor - new Point2D(1, 0));
                                if (CanPlacePavedRoad(new Rectangle(cursor.X + 3, cursor.Y, 1, 3), true, false))
                                    PlaceTile(medians + 12, cursor + new Point2D(3, 0));

                                cursor.Y += 2;
                            }
                            else
                            {
                                PlaceTile(ends + 3, cursor - new Point2D(0, 1));
                                var median = cursor + new Point2D(1, -2);
                                while (IsRoadTileInRange(TileAt(median), medians, 14))
                                {
                                    map.GetTileOrFail(median).ChangeTileIndex(roads + 1, 1);
                                    median.Y--;
                                }

                                inSegment = false;
                            }
                        }

                        initial = false;
                        cursor.Y++;
                    }

                    if (medianPending)
                        PlaceTile(medians + 3, cursor + new Point2D(1, -1));

                    int cap = CanPlacePavedRoad(new Rectangle(cursor.X, cursor.Y, 3, 4), false, false) && random.Pick(0, 1) != 0
                        ? roads + 12
                        : ends + 3;
                    PlaceTile(cap, cursor);
                }

                private Point2D PlanPavedRoadJunction(Point2D start, Rectangle bounds, bool south)
                {
                    int pavement = TileStart("PaveTile");
                    Point2D step = south ? new Point2D(0, 1) : new Point2D(1, 0);
                    Point2D cursor = start;

                    while (cursor.X < bounds.Right && cursor.Y < bounds.Bottom && TileAt(cursor) != pavement)
                        cursor += step;
                    while (cursor.X < bounds.Right && cursor.Y < bounds.Bottom &&
                        !CanPlacePavedRoad(PavedRoadPlanningStrip(cursor, south), south, south))
                    {
                        cursor += step;
                    }

                    if (cursor.X >= bounds.Right || cursor.Y >= bounds.Bottom)
                        return Point2D.Zero;

                    int length = Math.Max(cursor.X - start.X, cursor.Y - start.Y);
                    if (length > 0)
                    {
                        int shorten = random.Pick(0, length);
                        for (int i = 0; i < shorten; i++)
                        {
                            Point2D back = cursor - step;
                            if (!CanPlacePavedRoad(PavedRoadPlanningStrip(back, south), false, false))
                                break;

                            cursor = back;
                        }
                    }

                    return cursor;
                }

                private Point2D PlanPavedRoad(Point2D start, Rectangle bounds, bool south)
                {
                    Point2D step = south ? new Point2D(0, 1) : new Point2D(1, 0);
                    Point2D cursor = start;
                    while (cursor.X <= bounds.Right && cursor.Y <= bounds.Bottom &&
                        CanPlacePavedRoad(PavedRoadPlanningStrip(cursor, south), south, south))
                    {
                        cursor += step;
                    }

                    cursor -= step;
                    int length = Math.Max(cursor.X - start.X, cursor.Y - start.Y);
                    if (length > 6)
                    {
                        int shorten = random.Pick(0, 3);
                        if (south)
                            cursor.Y -= shorten;
                        else
                            cursor.X -= shorten;
                    }

                    return cursor;
                }

                private static Rectangle PavedRoadPlanningStrip(Point2D point, bool south) => south
                    ? new Rectangle(point.X - 3, point.Y, 9, 1)
                    : new Rectangle(point.X, point.Y - 3, 1, 9);

                public bool CanPlacePavedRoad(Rectangle rect, bool overlapRoad, bool overlapRoadEnd) =>
                    CanPlaceNativePavedRoad(map, clearTile, TileStart, rect, overlapRoad, overlapRoadEnd);

                private bool CanPlacePavedRoadJunction(Rectangle rect, int roads, int medians)
                {
                    if (!RoadAreaInRadar(rect))
                        return false;

                    for (int y = rect.Top; y < rect.Top + 3; y++)
                    {
                        for (int x = rect.Left; x < rect.Left + 3; x++)
                        {
                            int index = TileAt(new Point2D(x, y));
                            if (!IsRoadTileInRange(index, roads, 2) && !IsRoadTileInRange(index, medians, 14))
                                return false;
                        }
                    }

                    int misc = TileStart("MiscPaveTile");
                    int pavement = TileStart("PaveTile");
                    for (int x = rect.Left; x < rect.Left + 3; x++)
                    {
                        if (!IsRoadClearOrPaved(TileAt(new Point2D(x, rect.Top + 3)), misc, pavement))
                            return false;
                    }

                    return true;
                }

                private bool IsRoadClearOrPaved(int index, int misc, int pavement) =>
                    index == clearTile || index == ushort.MaxValue ||
                    IsRoadTileInRange(index, misc, 14) || IsRoadTileInRange(index, pavement, 16);

                private static bool IsRoadTileInRange(int index, int start, int count) => (uint)(index - start) < (uint)count;

                private bool RoadAreaInRadar(Rectangle rect) =>
                    map.GetTile(rect.Left, rect.Top) != null &&
                    map.GetTile(rect.Right - 1, rect.Top) != null &&
                    map.GetTile(rect.Left, rect.Bottom - 1) != null &&
                    map.GetTile(rect.Right - 1, rect.Bottom - 1) != null;

                public void GenerateUrbanPavement(List<Point2D> cells)
                {
                    int target = cells.Count / 50;
                    int placed = 0;
                    int pavement = TileStart("PaveTile");
                    int misc = TileStart("MiscPaveTile");
                    for (int tries = 0; tries < 10 * target && placed < target; tries++)
                    {
                        Point2D anchor = cells[random.Pick(0, cells.Count - 1)];
                        foreach (Point2D size in urbanPavementSizes)
                        {
                            var area = new Rectangle(anchor.X, anchor.Y, size.X, size.Y);
                            if (!CanPlaceMiscPavement(area, pavement, misc))
                                continue;

                            int tile = misc + 8 + random.Pick(0, 5);
                            for (int y = area.Top; y < area.Bottom; y += 2)
                            {
                                for (int x = area.Left; x < area.Right; x += 2)
                                    PlaceTile(tile, new Point2D(x, y));
                            }

                            placed++;
                            break;
                        }
                    }
                }

                private bool CanPlaceMiscPavement(Rectangle rect, int pavement, int misc)
                {
                    if (!RoadAreaInRadar(rect))
                        return false;

                    for (int y = rect.Top; y < rect.Bottom; y++)
                    {
                        for (int x = rect.Left; x < rect.Right; x++)
                        {
                            MapTile cell = map.GetTileOrFail(new Point2D(x, y));
                            if ((!IsRoadTileInRange(cell.TileIndex, pavement, 16) && !IsRoadTileInRange(cell.TileIndex, misc, 8)) ||
                                IsOccupied(cell))
                            {
                                return false;
                            }
                        }
                    }

                    return true;
                }

                private const int DirtRoadCurveCount = 24;
                private const int DirtRoadJunctionCount = 11;
                private const int DirtRoadStraightCount = 66;
                private const int DirtRoadTileCount = DirtRoadCurveCount + DirtRoadJunctionCount + DirtRoadStraightCount;
                private const int DirtRoadSlopeCount = 8;

                private enum DirtFacing
                {
                    N, NE, E, SE, S, SW, W, NW
                }

                private readonly record struct DirtRoadLink(int X, int Y, DirtFacing Facing)
                {
                    public Point2D Offset => new Point2D(X, Y);
                }

                private static readonly DirtRoadLink[][] dirtRoadLinks =
                {
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(2, 0, DirtFacing.NE) }, // 0
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(1, 0, DirtFacing.E) }, // 1
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(1, 1, DirtFacing.SE) }, // 2
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(1, 0, DirtFacing.SW) }, // 3
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(0, 0, DirtFacing.W) }, // 4
                    new DirtRoadLink[] { new(1, 0, DirtFacing.N), new(1, 1, DirtFacing.NW) }, // 5
                    new DirtRoadLink[] { new(1, 0, DirtFacing.NE), new(1, 1, DirtFacing.E) }, // 6
                    new DirtRoadLink[] { new(1, 0, DirtFacing.NE), new(1, 1, DirtFacing.SE) }, // 7
                    new DirtRoadLink[] { new(1, 0, DirtFacing.NE), new(0, 1, DirtFacing.S) }, // 8
                    new DirtRoadLink[] { new(1, 0, DirtFacing.NE), new(0, 0, DirtFacing.W) }, // 9
                    new DirtRoadLink[] { new(2, 0, DirtFacing.NE), new(1, 1, DirtFacing.NW) }, // 10
                    new DirtRoadLink[] { new(2, 0, DirtFacing.NE), new(0, 0, DirtFacing.N) }, // 11
                    new DirtRoadLink[] { new(1, 0, DirtFacing.E), new(1, 2, DirtFacing.SE) }, // 12
                    new DirtRoadLink[] { new(1, 0, DirtFacing.E), new(0, 1, DirtFacing.S) }, // 13
                    new DirtRoadLink[] { new(1, 0, DirtFacing.E), new(1, 0, DirtFacing.SW) }, // 14
                    new DirtRoadLink[] { new(1, 0, DirtFacing.E), new(1, 1, DirtFacing.NW) }, // 15
                    new DirtRoadLink[] { new(2, 1, DirtFacing.SE), new(0, 1, DirtFacing.S) }, // 16
                    new DirtRoadLink[] { new(1, 1, DirtFacing.SE), new(1, 0, DirtFacing.SW) }, // 17
                    new DirtRoadLink[] { new(1, 1, DirtFacing.SE), new(0, 0, DirtFacing.W) }, // 18
                    new DirtRoadLink[] { new(1, 1, DirtFacing.S), new(1, 0, DirtFacing.SW) }, // 19
                    new DirtRoadLink[] { new(0, 1, DirtFacing.S), new(0, 0, DirtFacing.W) }, // 20
                    new DirtRoadLink[] { new(0, 1, DirtFacing.S), new(1, 1, DirtFacing.NW) }, // 21
                    new DirtRoadLink[] { new(1, 1, DirtFacing.SW), new(1, 1, DirtFacing.NW) }, // 22
                    new DirtRoadLink[] { new(0, 1, DirtFacing.W), new(1, 1, DirtFacing.NW) }, // 23
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(1, 0, DirtFacing.E), new(0, 1, DirtFacing.S) }, // 24
                    new DirtRoadLink[] { new(1, 0, DirtFacing.E), new(0, 1, DirtFacing.S), new(0, 0, DirtFacing.W) }, // 25
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(0, 1, DirtFacing.S), new(0, 0, DirtFacing.W) }, // 26
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(1, 0, DirtFacing.E), new(0, 0, DirtFacing.W) }, // 27
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(1, 0, DirtFacing.E), new(0, 1, DirtFacing.S), new(0, 0, DirtFacing.W) }, // 28
                    new DirtRoadLink[] { new(2, 0, DirtFacing.NE), new(2, 2, DirtFacing.SE), new(1, 1, DirtFacing.SW) }, // 29
                    new DirtRoadLink[] { new(1, 1, DirtFacing.NW), new(1, 1, DirtFacing.SW), new(2, 2, DirtFacing.SE) }, // 30
                    new DirtRoadLink[] { new(2, 0, DirtFacing.NE), new(1, 1, DirtFacing.NW), new(1, 1, DirtFacing.SW) }, // 31
                    new DirtRoadLink[] { new(1, 1, DirtFacing.NW), new(2, 0, DirtFacing.NE), new(2, 2, DirtFacing.SE) }, // 32
                    new DirtRoadLink[] { new(1, 1, DirtFacing.NW), new(1, 1, DirtFacing.SW), new(2, 0, DirtFacing.NE), new(2, 2, DirtFacing.SE) }, // 33
                    new DirtRoadLink[] { new(3, 0, DirtFacing.NE), new(3, 2, DirtFacing.E), new(0, 2, DirtFacing.W) }, // 34
                    new DirtRoadLink[] { new(3, 0, DirtFacing.NE), new(1, 2, DirtFacing.SW) }, // 35
                    new DirtRoadLink[] { new(3, 0, DirtFacing.NE), new(1, 2, DirtFacing.SW) }, // 36
                    new DirtRoadLink[] { new(3, 0, DirtFacing.NE), new(1, 2, DirtFacing.SW) }, // 37
                    new DirtRoadLink[] { new(3, 0, DirtFacing.NE), new(1, 2, DirtFacing.SW) }, // 38
                    new DirtRoadLink[] { new(2, 0, DirtFacing.NE), new(1, 1, DirtFacing.SW) }, // 39
                    new DirtRoadLink[] { new(1, 0, DirtFacing.NE), new(1, 0, DirtFacing.SW) }, // 40
                    new DirtRoadLink[] { new(1, 0, DirtFacing.NE), new(1, 0, DirtFacing.SW) }, // 41
                    new DirtRoadLink[] { new(1, 2, DirtFacing.SW) }, // 42
                    new DirtRoadLink[] { new(1, 1, DirtFacing.SW) }, // 43
                    new DirtRoadLink[] { new(1, 1, DirtFacing.SW) }, // 44
                    new DirtRoadLink[] { new(3, 0, DirtFacing.NE) }, // 45
                    new DirtRoadLink[] { new(2, 0, DirtFacing.NE) }, // 46
                    new DirtRoadLink[] { new(2, 0, DirtFacing.NE) }, // 47
                    new DirtRoadLink[] { new(4, 0, DirtFacing.NE), new(1, 1, DirtFacing.SW) }, // 48
                    new DirtRoadLink[] { new(2, 0, DirtFacing.NE), new(1, 3, DirtFacing.SW) }, // 49
                    Array.Empty<DirtRoadLink>(), // 50
                    new DirtRoadLink[] { new(0, 0, DirtFacing.W), new(3, 0, DirtFacing.E) }, // 51
                    new DirtRoadLink[] { new(0, 0, DirtFacing.W), new(3, 0, DirtFacing.E) }, // 52
                    new DirtRoadLink[] { new(0, 0, DirtFacing.W), new(3, 0, DirtFacing.E) }, // 53
                    new DirtRoadLink[] { new(0, 0, DirtFacing.W), new(3, 0, DirtFacing.E) }, // 54
                    new DirtRoadLink[] { new(0, 0, DirtFacing.W), new(2, 0, DirtFacing.E) }, // 55
                    new DirtRoadLink[] { new(0, 0, DirtFacing.W), new(1, 0, DirtFacing.E) }, // 56
                    new DirtRoadLink[] { new(0, 0, DirtFacing.W), new(0, 0, DirtFacing.E) }, // 57
                    new DirtRoadLink[] { new(0, 0, DirtFacing.W), new(0, 0, DirtFacing.E) }, // 58
                    new DirtRoadLink[] { new(0, 0, DirtFacing.W) }, // 59
                    new DirtRoadLink[] { new(0, 0, DirtFacing.W) }, // 60
                    new DirtRoadLink[] { new(0, 1, DirtFacing.W) }, // 61
                    new DirtRoadLink[] { new(2, 0, DirtFacing.E) }, // 62
                    new DirtRoadLink[] { new(1, 0, DirtFacing.E) }, // 63
                    new DirtRoadLink[] { new(1, 0, DirtFacing.E) }, // 64
                    new DirtRoadLink[] { new(0, 0, DirtFacing.W), new(3, 1, DirtFacing.E) }, // 65
                    new DirtRoadLink[] { new(0, 1, DirtFacing.W), new(3, 0, DirtFacing.E) }, // 66
                    Array.Empty<DirtRoadLink>(), // 67
                    new DirtRoadLink[] { new(1, 1, DirtFacing.NW), new(3, 3, DirtFacing.SE) }, // 68
                    new DirtRoadLink[] { new(1, 1, DirtFacing.NW), new(3, 3, DirtFacing.SE) }, // 69
                    new DirtRoadLink[] { new(1, 1, DirtFacing.NW), new(3, 3, DirtFacing.SE) }, // 70
                    new DirtRoadLink[] { new(1, 1, DirtFacing.NW), new(3, 3, DirtFacing.SE) }, // 71
                    new DirtRoadLink[] { new(1, 1, DirtFacing.NW), new(2, 2, DirtFacing.SE) }, // 72
                    new DirtRoadLink[] { new(1, 1, DirtFacing.NW), new(1, 1, DirtFacing.SE) }, // 73
                    new DirtRoadLink[] { new(1, 1, DirtFacing.NW), new(1, 1, DirtFacing.SE) }, // 74
                    new DirtRoadLink[] { new(1, 1, DirtFacing.NW) }, // 75
                    new DirtRoadLink[] { new(1, 1, DirtFacing.NW) }, // 76
                    new DirtRoadLink[] { new(1, 1, DirtFacing.NW) }, // 77
                    new DirtRoadLink[] { new(3, 3, DirtFacing.SE) }, // 78
                    new DirtRoadLink[] { new(1, 2, DirtFacing.SE) }, // 79
                    new DirtRoadLink[] { new(2, 2, DirtFacing.SE) }, // 80
                    new DirtRoadLink[] { new(1, 1, DirtFacing.NW), new(4, 2, DirtFacing.SE) }, // 81
                    new DirtRoadLink[] { new(1, 1, DirtFacing.NW), new(2, 4, DirtFacing.SE) }, // 82
                    Array.Empty<DirtRoadLink>(), // 83
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(0, 3, DirtFacing.S) }, // 84
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(0, 3, DirtFacing.S) }, // 85
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(0, 3, DirtFacing.S) }, // 86
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(0, 3, DirtFacing.S) }, // 87
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(0, 2, DirtFacing.S) }, // 88
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(0, 1, DirtFacing.S) }, // 89
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(0, 0, DirtFacing.S) }, // 90
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(0, 0, DirtFacing.S) }, // 91
                    new DirtRoadLink[] { new(0, 3, DirtFacing.S) }, // 92
                    new DirtRoadLink[] { new(0, 2, DirtFacing.S) }, // 93
                    new DirtRoadLink[] { new(0, 2, DirtFacing.S) }, // 94
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N) }, // 95
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N) }, // 96
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N) }, // 97
                    new DirtRoadLink[] { new(1, 0, DirtFacing.N), new(0, 3, DirtFacing.S) }, // 98
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(1, 3, DirtFacing.S) }, // 99
                    Array.Empty<DirtRoadLink>(), // 100
                    new DirtRoadLink[] { new(0, 0, DirtFacing.W), new(0, 0, DirtFacing.E) }, // 101
                    new DirtRoadLink[] { new(0, 0, DirtFacing.W), new(0, 0, DirtFacing.E) }, // 102
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(0, 0, DirtFacing.S) }, // 103
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(0, 0, DirtFacing.S) }, // 104
                    new DirtRoadLink[] { new(0, 0, DirtFacing.W), new(0, 0, DirtFacing.E) }, // 105
                    new DirtRoadLink[] { new(0, 0, DirtFacing.W), new(0, 0, DirtFacing.E) }, // 106
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(0, 0, DirtFacing.S) }, // 107
                    new DirtRoadLink[] { new(0, 0, DirtFacing.N), new(0, 0, DirtFacing.S) }, // 108
                };

                // The order within each facing and way count determines the RNG-selected search order.
                private static readonly int[][][] dirtRoadLibrary =
                {
                    new int[][] // N
                    {
                        new int[] { 95, 96, 97 },
                        new int[] { 0, 1, 2, 3, 4, 5, 11, 84, 85, 86, 87, 88, 89, 90, 91, 98, 99 },
                        new int[] { 24, 26, 27 },
                        new int[] { 28 },
                    },
                    new int[][] // NE
                    {
                        new int[] { 45, 46, 47 },
                        new int[] { 0, 6, 7, 8, 9, 10, 11, 35, 36, 37, 38, 39, 40, 41, 48, 49 },
                        new int[] { 29, 31, 32, 34 },
                        new int[] { 33 },
                    },
                    new int[][] // E
                    {
                        new int[] { 62, 63, 64 },
                        new int[] { 1, 6, 12, 13, 14, 15, 51, 52, 53, 54, 55, 56, 57, 58, 65, 66 },
                        new int[] { 24, 25, 27, 34 },
                        new int[] { 28 },
                    },
                    new int[][] // SE
                    {
                        new int[] { 78, 79, 80 },
                        new int[] { 2, 7, 12, 16, 17, 18, 68, 69, 70, 71, 72, 73, 74, 81, 82 },
                        new int[] { 29, 30, 32 },
                        new int[] { 33 },
                    },
                    new int[][] // S
                    {
                        new int[] { 92, 93, 94 },
                        new int[] { 8, 13, 16, 19, 20, 21, 84, 85, 86, 87, 88, 89, 90, 91, 98, 99 },
                        new int[] { 24, 25, 26 },
                        new int[] { 28 },
                    },
                    new int[][] // SW
                    {
                        new int[] { 42, 43, 44 },
                        new int[] { 3, 14, 17, 19, 22, 35, 36, 37, 38, 39, 40, 41, 48, 49 },
                        new int[] { 29, 30, 31 },
                        new int[] { 33 },
                    },
                    new int[][] // W
                    {
                        new int[] { 59, 60, 61 },
                        new int[] { 4, 9, 18, 20, 23, 51, 52, 53, 54, 55, 56, 57, 58, 65, 66 },
                        new int[] { 25, 26, 27, 34 },
                        new int[] { 28 },
                    },
                    new int[][] // NW
                    {
                        new int[] { 75, 76, 77 },
                        new int[] { 5, 10, 15, 21, 22, 23, 68, 69, 70, 71, 72, 73, 74, 81, 82 },
                        new int[] { 30, 31, 32 },
                        new int[] { 33 },
                    },
                };

                private readonly record struct DirtRoadPlacement(int Tile, Point2D Origin, int BackLink);
                private readonly record struct DirtRoadNode(DirtRoadPlacement? Road, DirtRoadPlacement? Cap);

                private readonly HashSet<Point2D> dirtRoadReservations = new();
                private int dirtRoadCurveStart;
                private int dirtRoadJunctionStart;
                private int dirtRoadSlopeStart;
                private int dirtRoadRampStart;
                private int dirtRoadRampSmoothStart;

                private void InitDirtRoads()
                {
                    dirtRoadCurveStart = TileStart("DirtRoadCurve");
                    dirtRoadJunctionStart = TileStart("DirtRoadJunction");
                    dirtRoadSlopeStart = TileStart("DirtRoadSlopes");
                    dirtRoadRampStart = TileStart("RampBase");
                    dirtRoadRampSmoothStart = TileStart("RampSmooth");
                }

                private bool GenerateRuralRoads(Point2D origin)
                {
                    int offset = random.Pick(0, DirtRoadJunctionCount - 1);
                    int junction = -1;
                    for (int attempt = 0; attempt < DirtRoadJunctionCount; attempt++)
                    {
                        int candidate = dirtRoadJunctionStart + (offset + attempt) % DirtRoadJunctionCount;
                        if (CanPlaceTerrainTile(candidate, origin, false, false))
                        {
                            junction = candidate;
                            break;
                        }
                    }

                    if (junction == -1)
                        return false;

                    var queue = new Queue<DirtRoadNode>();
                    queue.Enqueue(new DirtRoadNode(new DirtRoadPlacement(junction, origin, -1), null));
                    var children = new List<DirtRoadNode>(4);
                    int processed = 0;
                    while (queue.Count > 0 && queue.Count < 1020)
                    {
                        DirtRoadNode node = queue.Dequeue();
                        children.Clear();
                        if (node.Road is DirtRoadPlacement road)
                        {
                            if (node.Cap is DirtRoadPlacement previousCap)
                                MarkDirtRoad(previousCap, false);

                            PlaceTile(road.Tile, road.Origin);
                            DirtRoadLink[] links = dirtRoadLinks[DirtRoadTileIndex(road.Tile)];
                            bool success = true;
                            if (links.Length > 1)
                            {
                                for (int i = 0; i < links.Length; i++)
                                {
                                    if (i == road.BackLink)
                                        continue;

                                    DirtRoadLink link = links[i];
                                    int facing = (int)link.Facing;
                                    int reverse = (facing + 4) & 7;
                                    Point2D connection = road.Origin + link.Offset + adjacent[facing];
                                    if (FindDirtRoadTile(reverse, connection, 1) is not DirtRoadPlacement cap)
                                    {
                                        success = false;
                                        break;
                                    }

                                    DirtRoadPlacement? continuation = FindDirtRoadTile(reverse, connection, 4);
                                    if (continuation is DirtRoadPlacement reservedRoad)
                                        MarkDirtRoad(reservedRoad, true);

                                    MarkDirtRoad(cap, true);
                                    children.Add(new DirtRoadNode(continuation, cap));
                                }
                            }

                            if (success)
                            {
                                foreach (DirtRoadNode child in children)
                                    queue.Enqueue(child);
                            }
                            else
                            {
                                RemoveDirtRoad(road);
                                foreach (DirtRoadNode child in children)
                                {
                                    if (child.Road is DirtRoadPlacement reservedRoad)
                                        MarkDirtRoad(reservedRoad, false);
                                    if (child.Cap is DirtRoadPlacement reservedCap)
                                        MarkDirtRoad(reservedCap, false);
                                }

                                if (node.Cap is DirtRoadPlacement cap)
                                    PlaceTile(cap.Tile, cap.Origin);
                            }
                        }
                        else if (node.Cap is DirtRoadPlacement cap)
                        {
                            PlaceTile(cap.Tile, cap.Origin);
                        }

                        processed++;
                    }

                    // At the queue limit the engine stamps the caps and leaves the reservations intact.
                    while (queue.Count != 0)
                    {
                        DirtRoadNode node = queue.Dequeue();
                        if (node.Cap is DirtRoadPlacement cap)
                            PlaceTile(cap.Tile, cap.Origin);
                    }

                    return processed > 1;
                }

                private int DirtRoadTileIndex(int tile)
                {
                    if ((uint)(tile - dirtRoadCurveStart) < DirtRoadTileCount)
                        return tile - dirtRoadCurveStart;

                    if (dirtRoadSlopeStart >= 0 && (uint)(tile - dirtRoadSlopeStart) < DirtRoadSlopeCount)
                        return tile - dirtRoadSlopeStart + DirtRoadTileCount;

                    return 0;
                }

                private DirtRoadPlacement? FindDirtRoadTile(int facing, Point2D target, int maxConnections)
                {
                    int initialConnections = maxConnections > 1
                        ? (int)random.NativeTruncatedNormal(2.0, 0.5, 2.0, maxConnections)
                        : 1;
                    int connections = initialConnections;
                    do
                    {
                        int[] tiles = dirtRoadLibrary[facing][connections - 1];
                        int start = connections == 2 && random.Fraction() < 0.7
                            ? random.Pick(7, tiles.Length - 1)
                            : random.Pick(0, tiles.Length - 1);
                        for (int i = 0; i < tiles.Length; i++)
                        {
                            int index = tiles[(start + i) % tiles.Length];
                            DirtRoadLink[] links = dirtRoadLinks[index];
                            int linkIndex = 0;
                            while ((int)links[linkIndex].Facing != facing)
                                linkIndex++;

                            Point2D origin = target - links[linkIndex].Offset;
                            int tile = dirtRoadCurveStart + index;
                            if (CanPlaceTerrainTile(tile, origin, true, true))
                                return new DirtRoadPlacement(tile, origin, linkIndex);
                        }

                        connections = connections == maxConnections ? 1 : connections + 1;
                    }
                    while (connections != initialConnections);

                    return null;
                }

                private bool CanPlaceTerrainTile(int tile, Point2D origin, bool collision, bool inMap)
                {
                    ITileImage image = currentTile(tile);
                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            Point2D point = origin + new Point2D(x, y);
                            MapTile cell = map.GetTile(point);
                            if (cell == null)
                            {
                                if (inMap)
                                    return false;

                                continue;
                            }

                            TmpImage part = image.GetSubTile(x + y * image.Width)?.TmpImage;
                            if (part == null)
                                continue;

                            int cellTile = cell.TileIndex;
                            bool validTile = cellTile == clearTile || cellTile == ushort.MaxValue ||
                                (dirtRoadRampStart >= 0 && (uint)(cellTile - dirtRoadRampStart) < 20) ||
                                (dirtRoadRampSmoothStart >= 0 && (uint)(cellTile - dirtRoadRampSmoothStart) < 12);
                            if (!validTile || GetRamp(cell) != part.RampType || collision && dirtRoadReservations.Contains(point))
                                return false;
                        }
                    }

                    return true;
                }

                private IEnumerable<MapTile> DirtRoadCells(DirtRoadPlacement placement)
                {
                    ITileImage image = currentTile(placement.Tile);
                    for (int index = 0; index < image.SubTileCount; index++)
                    {
                        if (image.GetSubTile(index)?.TmpImage != null &&
                            map.GetTile(placement.Origin + new Point2D(index % image.Width, index / image.Width)) is MapTile cell)
                        {
                            yield return cell;
                        }
                    }
                }

                private void MarkDirtRoad(DirtRoadPlacement placement, bool reserved)
                {
                    foreach (MapTile cell in DirtRoadCells(placement))
                    {
                        Point2D point = cell.CoordsToPoint();
                        if (reserved)
                            dirtRoadReservations.Add(point);
                        else
                            dirtRoadReservations.Remove(point);
                    }
                }

                private void RemoveDirtRoad(DirtRoadPlacement placement)
                {
                    // The editor stores a blank cell as the clear tile. Native Remove_Tile leaves the height and Ramp.
                    foreach (MapTile cell in DirtRoadCells(placement))
                        cell.ChangeTileIndex(clearTile, 0);
                }

                private static readonly string[] urbanBuildingNames =
                {
                    "CITY01", "CITY02", "CITY03", "CITY04", "CITY05", "CITY06",
                    "CITY07", "CITY08", "CITY09", "CITY10", "CITY11", "CITY12",
                    "CITY13", "CITY14", "CITY15", "CITY16", "CITY17", "CITY18", "CAHOSP"
                };

                private static readonly string[] ruralBuildingNames =
                {
                    "ABAN01", "ABAN02", "ABAN03", "ABAN04", "ABAN05", "ABAN06",
                    "ABAN07", "ABAN08", "ABAN09", "ABAN10", "ABAN11", "ABAN12",
                    "ABAN13", "ABAN14", "ABAN15",
                    "CA0001", "CA0002", "CA0003", "CA0004", "CA0005", "CA0006",
                    "CA0007", "CA0008", "CA0009", "CA0010", "CA0011", "CA0012",
                    "CA0013", "CA0014", "CA0015", "CA0016", "CA0017", "CA0018",
                    "CA0019", "CA0020", "CA0021"
                };

                private static readonly string[] urbanUnitNames = { "CIV1", "CIV2", "CIV3", "PICK", "WINI", "BUS", "CAR" };
                private static readonly string[] ruralUnitNames = { "CIV1", "CIV2", "CIV3", "PICK", "WINI", "CAR" };
                private static readonly int[] maxUrbanUnits = { 4, 6, 6, 3, 2 };
                private static readonly int[] maxRuralUnits = { 4, 6, 6, 3, 1 };
                private static readonly double[] conditionRedChances = { 0.5, 0.2, 0.2, 0.6, 0.8 };
                private static readonly double[] conditionYellowChances = { 0.5, 0.3, 0.2, 0.5, 0.95 };

                private void GenerateUrbanBuildings(List<Point2D> cells)
                {
                    int count = random.Pick(cells.Count / 20, cells.Count / 10) / 2;
                    List<string> names = new List<string>(urbanBuildingNames);
                    int pavement = TileStart("PaveTile");
                    int miscellaneousPavement = TileStart("MiscPaveTile");
                    bool OnPavement(MapTile cell) =>
                        (cell.TileIndex >= pavement && cell.TileIndex < pavement + 16)
                        || (cell.TileIndex >= miscellaneousPavement && cell.TileIndex < miscellaneousPavement + 14);

                    int placed = 0;
                    for (int tries = 0; tries < 2 * count && placed < count && names.Count > 1; tries++)
                    {
                        int nameIndex = random.Pick(0, names.Count - 1);
                        BuildingType type = source.Rules.BuildingTypes.Find(candidate => candidate.ININame == names[nameIndex]);
                        if (type == null)
                            throw new InvalidOperationException("The map lacks the stock " + names[nameIndex] + " building.");

                        for (int attempt = 0; attempt < 60; attempt++)
                        {
                            Point2D position = cells[random.Pick(0, cells.Count - 1)];
                            Structure building = TryPlaceSettlementBuilding(type, position, OnPavement);
                            if (building == null)
                                continue;

                            building.HP = DamagedSettlementHealth(type.ININame, true);
                            placed++;
                            if (nameIndex > 17)
                                names.RemoveAt(nameIndex);
                            break;
                        }
                    }
                }

                private void GenerateUrbanUnits(List<Point2D> cells)
                {
                    GenerateSettlementUnits(urbanUnitNames, maxUrbanUnits[(int)environment],
                        () => cells[random.Pick(0, cells.Count - 1)]);
                }

                private void GenerateRuralBuildings(Point2D center)
                {
                    int count = random.Pick(8, 12);
                    int height = map.GetTileOrFail(center).Level;
                    int firstBuilding = environment == EnvironmentType.Tundra || environment == EnvironmentType.Taiga ? 0 : 15;
                    List<string> names = new List<string>(ruralBuildingNames);
                    bool IsFlatClear(MapTile cell) => IsClear(cell.CoordsToPoint()) && cell.Level == height;

                    int placed = 0;
                    for (int tries = 0; tries < 2 * count && placed < count;)
                    {
                        int nameIndex = random.Pick(firstBuilding, names.Count - 1);
                        BuildingType type = source.Rules.BuildingTypes.Find(candidate => candidate.ININame == names[nameIndex]);
                        // The native rural pass only counts attempts with a recognized building type.
                        if (type == null)
                            continue;

                        for (int attempt = 0; attempt < 60; attempt++)
                        {
                            Point2D position = PickRuralCell(center, 7);
                            if (TryPlaceSettlementBuilding(type, position, IsFlatClear) == null)
                                continue;

                            placed++;
                            names.RemoveAt(nameIndex);
                            break;
                        }

                        tries++;
                    }
                }

                private void GenerateRuralUnits(Point2D center)
                {
                    GenerateSettlementUnits(ruralUnitNames, maxRuralUnits[(int)environment], () => PickRuralCell(center, 8));
                }

                private Point2D PickRuralCell(Point2D center, int radius)
                {
                    // Win32 evaluates the second argument to Cell first.
                    int y = random.Pick(0, 2 * radius) - radius;
                    int x = random.Pick(0, 2 * radius) - radius;
                    return center + new Point2D(x, y);
                }

                private House SettlementNeutral()
                {
                    return source.FindHouse("Neutral") ?? throw new InvalidOperationException("The map lacks the stock Neutral house.");
                }

                private Structure TryPlaceSettlementBuilding(BuildingType type, Point2D position, Func<MapTile, bool> acceptsTile)
                {
                    if (map.GetTile(position) == null)
                        return null;

                    // For these stock buildings the ordinary art foundation is also the game placement footprint.
                    Point2D[] footprint = type.ArtConfig.Foundation.FoundationCells;
                    foreach (Point2D offset in footprint)
                    {
                        Point2D target = position + offset;
                        MapTile cell = map.GetTile(target);
                        if (cell == null || !acceptsTile(cell) || IsOccupied(cell) || IsLandRock(cell) || !IsInLocalRadar(target))
                            return null;
                    }

                    var building = new Structure(type) { Owner = SettlementNeutral(), Position = position };
                    map.PlaceBuilding(building);
                    foreach (Point2D offset in footprint)
                        RecalculateSettlementCell(map.GetTileOrFail(position + offset));
                    return building;
                }

                private void GenerateSettlementUnits(string[] names, int maxCount, Func<Point2D> pickCell)
                {
                    int count = random.Pick(1, maxCount);
                    int placed = 0;
                    for (int tries = 0; tries < 2 * count && placed < count; tries++)
                    {
                        int nameIndex = random.Pick(0, names.Length - 1);
                        string name = names[nameIndex];
                        bool infantry = nameIndex <= 2;

                        for (int attempt = 0; attempt < 60; attempt++)
                        {
                            Point2D position = pickCell();
                            MapTile cell = map.GetTile(position);
                            if (cell == null || !IsInLocalRadar(position) || IsOccupied(cell))
                                continue;

                            byte facing = (byte)(random.Pick(0, 7) * 32);
                            House neutral = SettlementNeutral();
                            TechnoBase placedFoot;
                            if (infantry)
                            {
                                InfantryType type = source.Rules.InfantryTypes.Find(candidate => candidate.ININame == name)
                                    ?? throw new InvalidOperationException("The map lacks the stock " + name + " infantry.");
                                var foot = new Infantry(type)
                                {
                                    Owner = neutral, Position = position, Facing = facing, SubCell = SubCell.Center,
                                    AutocreateNoRecruitable = true, AutocreateYesRecruitable = true
                                };
                                map.PlaceInfantry(foot);
                                placedFoot = foot;
                            }
                            else
                            {
                                UnitType type = source.Rules.UnitTypes.Find(candidate => candidate.ININame == name)
                                    ?? throw new InvalidOperationException("The map lacks the stock " + name + " unit.");
                                var foot = new Unit(type)
                                {
                                    Owner = neutral, Position = position, Facing = facing,
                                    AutocreateNoRecruitable = true, AutocreateYesRecruitable = true
                                };
                                map.PlaceUnit(foot);
                                placedFoot = foot;
                            }

                            RecalculateSettlementCell(cell);
                            placedFoot.HP = DamagedSettlementHealth(name, false);
                            placed++;
                            break;
                        }
                    }
                }

                private int DamagedSettlementHealth(string typeName, bool roundBuildingHealth)
                {
                    int biome = (int)environment;
                    string condition;
                    if (random.Fraction() < conditionRedChances[biome])
                        condition = "ConditionRed";
                    else if (random.Fraction() < conditionYellowChances[biome])
                        condition = "ConditionYellow";
                    else
                        return Constants.ObjectHealthMax;

                    double ratio = ReadSettlementCondition(firestorm, condition, ReadSettlementCondition(rules, condition, 0.5));
                    int maximum = rules?.GetIntValue(typeName, "Strength", 0) ?? 0;
                    maximum = firestorm?.GetIntValue(typeName, "Strength", maximum) ?? maximum;
                    if (maximum <= 0)
                        throw new InvalidOperationException("Stock Strength is missing for settlement object " + typeName + ".");

                    int strength = (int)(maximum * ratio);
                    double mapHealth = (double)strength / maximum * Constants.ObjectHealthMax;
                    return (int)(mapHealth + (roundBuildingHealth ? 0.5 : 0.0));
                }

                private static double ReadSettlementCondition(IniFile ini, string key, double fallback)
                {
                    string input = ini?.GetStringValue("AudioVisual", key, null)?.Trim();
                    if (string.IsNullOrEmpty(input))
                        return fallback;

                    int percent = input.IndexOf('%');
                    string value = percent >= 0 ? input.Substring(0, percent) : input;
                    if (!float.TryParse(value, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float parsed))
                    {
                        return fallback;
                    }

                    return percent >= 0 ? parsed / 100.0 : parsed;
                }
            }
        }

        private sealed class NativeHillDeformer
        {
            private const int MaximumHeight = 12;

            // Heights are in units of RAMP_HALF_HEIGHT. Corner order is (0,0), (1,0), (1,1), (0,1).
            private static readonly byte[,] rampCorners =
            {
                { 0, 0, 0, 0 }, // Flat
                { 0, 1, 1, 0 }, // West
                { 0, 0, 1, 1 }, // North
                { 1, 0, 0, 1 }, // East
                { 1, 1, 0, 0 }, // South
                { 0, 0, 1, 0 }, // Corner NW
                { 0, 0, 0, 1 }, // Corner NE
                { 1, 0, 0, 0 }, // Corner SE
                { 0, 1, 0, 0 }, // Corner SW
                { 0, 1, 1, 1 }, // Mid NW
                { 1, 0, 1, 1 }, // Mid NE
                { 1, 1, 0, 1 }, // Mid SE
                { 1, 1, 1, 0 }, // Mid SW
                { 0, 1, 2, 1 }, // Steep SE
                { 1, 0, 1, 2 }, // Steep SW
                { 2, 1, 0, 1 }, // Steep NW
                { 1, 2, 1, 0 }, // Steep NE
                { 0, 1, 0, 1 }, // Double up SWNE
                { 1, 0, 1, 0 }, // Double down SWNE
                { 0, 1, 0, 1 }, // Double up NWSE
                { 1, 0, 1, 0 }  // Double down NWSE
            };

            private static readonly RampType[] innerCornerRamps =
                { RampType.MidSE, RampType.MidSW, RampType.MidNW, RampType.MidNE };

            private static readonly Point2D[] cornerOffsets = CornerHeightField.CornerOffsets;

            private readonly Map map;
            private readonly int clearTile;
            private readonly int rampStart;
            private readonly Func<MapTile, RampType> getRamp;
            private readonly Func<MapTile, bool> canDeform;
            private readonly Action<MapTile, RampType> setRamp;
            private readonly int gridSize;
            private readonly DeformPoint[] points;
            private readonly List<(int Index, int Height)> undo = new();

            private struct DeformPoint
            {
                public int Height;
                public bool Rigid;
                public bool Done;
            }

            public NativeHillDeformer(Map map, int clearTile, int rampStart, Func<MapTile, RampType> getRamp,
                Func<MapTile, bool> canDeform, Action<MapTile, RampType> setRamp)
            {
                this.map = map;
                this.clearTile = clearTile;
                this.rampStart = rampStart;
                this.getRamp = getRamp;
                this.canDeform = canDeform;
                this.setRamp = setRamp;
                gridSize = map.Size.X + map.Size.Y;
                points = new DeformPoint[gridSize * gridSize];
            }

            public void Apply(IEnumerable<(MapTile Cell, double Offset)> heights)
            {
                InitializeGrid();

                foreach (var (cell, offset) in heights)
                {
                    Point2D point = cell.CoordsToPoint();
                    int x = point.X - 1;
                    int y = point.Y - 1;
                    int height = GetDeformedHeight(cell, x, y);
                    int change = (int)(cell.Level + offset - height);
                    int direction = change < 0 ? -1 : 1;
                    for (int remaining = Math.Abs(change); remaining > 0; remaining--)
                        DeformCell(x, y, direction, GetDeformableCorners(x, y, direction));
                }

                MapTile[] tiles = InIsometricOrder(map).ToArray();
                CommitGrid(tiles);
                ClearRedundantRamps(tiles);
            }

            private int Index(int x, int y) => x + y * gridSize;

            private void InitializeGrid()
            {
                // The point array covers the native square MapRect with origin (1,1), including its final corner row and column.
                for (int y = 0; y < gridSize; y++)
                {
                    for (int x = 0; x < gridSize; x++)
                    {
                        MapTile cell = map.GetTile(x + 1, y + 1);
                        MapTile north = map.GetTile(x + 1, y);
                        MapTile northwest = map.GetTile(x, y);
                        MapTile west = map.GetTile(x, y + 1);
                        MapTile source = cell ?? north ?? northwest ?? west;

                        points[Index(x, y)] = new DeformPoint
                        {
                            // Edge points use corner zero even when the height is borrowed from an adjacent cell.
                            Height = source == null ? 0 : source.Level + rampCorners[(int)getRamp(source), 0],
                            Rigid = source == null || IsRigid(north) || IsRigid(northwest) || IsRigid(west) || IsRigid(cell)
                        };
                    }
                }
            }

            private bool IsRigid(MapTile cell) => cell != null && !canDeform(cell);

            private (int Min, int Max) CornerRange(int x, int y)
            {
                int first = points[Index(x, y)].Height;
                int second = points[Index(x + 1, y)].Height;
                int third = points[Index(x + 1, y + 1)].Height;
                int fourth = points[Index(x, y + 1)].Height;

                return (Math.Min(Math.Min(first, second), Math.Min(third, fourth)),
                    Math.Max(Math.Max(first, second), Math.Max(third, fourth)));
            }

            private int GetDeformedHeight(MapTile cell, int x, int y)
            {
                return canDeform(cell) ? CornerRange(x, y).Min : cell.Level;
            }

            private int GetDeformableCorners(int x, int y, int direction)
            {
                var (minimum, maximum) = CornerRange(x, y);
                int mask = 0;
                for (int corner = 0; corner < cornerOffsets.Length; corner++)
                {
                    Point2D offset = cornerOffsets[corner];
                    DeformPoint point = points[Index(x + offset.X, y + offset.Y)];
                    if (!point.Rigid &&
                        (minimum == maximum || (direction > 0 ? point.Height < maximum : point.Height > minimum)))
                    {
                        mask |= 1 << corner;
                    }
                }

                return mask;
            }

            private void DeformCell(int x, int y, int direction, int mask)
            {
                undo.Clear();
                // The mask is in clockwise order but the deformation visits the corners row by row.
                for (int step = 0; step < cornerOffsets.Length; step++)
                {
                    int corner = step < 2 ? step : 5 - step;
                    if ((mask & (1 << corner)) == 0)
                        continue;

                    Point2D offset = cornerOffsets[corner];
                    int pointX = x + offset.X;
                    int pointY = y + offset.Y;
                    int index = Index(pointX, pointY);
                    ref DeformPoint point = ref points[index];
                    if (point.Rigid)
                    {
                        Undo();
                        return;
                    }

                    int target = point.Height + direction;
                    if (target is >= 0 and <= MaximumHeight)
                        ChangeHeight(index, target);
                    else
                        point.Done = true;

                    if (!Ripple(pointX, pointY, direction))
                    {
                        Undo();
                        return;
                    }
                }
            }

            private bool Ripple(int x, int y, int direction)
            {
                int startHeight = points[Index(x, y)].Height;
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0)
                            continue;

                        int nextX = x + dx;
                        int nextY = y + dy;
                        if ((uint)nextX >= gridSize || (uint)nextY >= gridSize)
                            return false;

                        int index = Index(nextX, nextY);
                        DeformPoint point = points[index];
                        int difference = startHeight - point.Height;
                        if (Math.Abs(difference) <= 1)
                            continue;

                        if (point.Rigid)
                            return false;

                        if (direction * difference > 0)
                            ChangeHeight(index, startHeight - direction);

                        // An already higher neighbor may still have to propagate a raise, and vice versa.
                        if (!Ripple(nextX, nextY, direction))
                            return false;
                    }
                }

                return true;
            }

            private void ChangeHeight(int index, int height)
            {
                ref DeformPoint point = ref points[index];
                undo.Add((index, point.Height));
                point.Height = height;
                point.Done = true;
            }

            private void Undo()
            {
                // Native Undo_Deform restores heights but leaves each touched point marked Done for the commit.
                for (int i = undo.Count - 1; i >= 0; i--)
                {
                    var (index, height) = undo[i];
                    points[index].Height = height;
                }

                undo.Clear();
            }

            private void CommitGrid(IEnumerable<MapTile> tiles)
            {
                foreach (MapTile cell in tiles)
                {
                    Point2D point = cell.CoordsToPoint();
                    int x = point.X - 1;
                    int y = point.Y - 1;
                    DeformPoint first = points[Index(x, y)];
                    DeformPoint second = points[Index(x + 1, y)];
                    DeformPoint third = points[Index(x + 1, y + 1)];
                    DeformPoint fourth = points[Index(x, y + 1)];

                    if (!first.Done && !second.Done && !third.Done && !fourth.Done)
                        continue;

                    var (minimum, maximum) = CornerRange(x, y);
                    if (maximum - minimum > 1 || !canDeform(cell))
                        continue;

                    cell.Level = (byte)minimum;
                    // The final two ramp records duplicate the preceding two. Native commit never chooses them.
                    for (int ramp = 0; ramp < rampCorners.GetLength(0) - 2; ramp++)
                    {
                        if (rampCorners[ramp, 0] == first.Height - minimum &&
                            rampCorners[ramp, 1] == second.Height - minimum &&
                            rampCorners[ramp, 2] == third.Height - minimum &&
                            rampCorners[ramp, 3] == fourth.Height - minimum)
                        {
                            cell.ChangeTileIndex(ramp == 0 ? clearTile : rampStart + ramp - 1, cell.SubTileIndex);
                            setRamp(cell, (RampType)ramp);
                            break;
                        }
                    }
                }
            }

            private void ClearRedundantRamps(IEnumerable<MapTile> tiles)
            {
                var group = new MapTile[4];
                foreach (MapTile cell in tiles)
                {
                    RampType ramp = getRamp(cell);
                    if (ramp != RampType.CornerNW && ramp != RampType.MidSE)
                        continue;

                    Point2D origin = cell.CoordsToPoint();
                    bool matches = true;
                    for (int i = 0; i < group.Length; i++)
                    {
                        group[i] = map.GetTile(origin + cornerOffsets[i]);
                        RampType expected = ramp == RampType.CornerNW
                            ? (RampType)((int)RampType.CornerNW + i) : innerCornerRamps[i];
                        if (group[i] == null || getRamp(group[i]) != expected)
                            matches = false;
                    }

                    if (!matches)
                        continue;

                    int step = ramp == RampType.MidSE ? 1 : 0;
                    foreach (MapTile member in group)
                    {
                        member.ChangeTileIndex(clearTile, 0);
                        setRamp(member, RampType.None);
                        member.Level = (byte)(member.Level + step);
                    }
                }
            }
        }

        private sealed class NativeTiberium
        {
            private const int MaxGrowth = 11;
            private readonly Map map;
            private readonly GeneratorRandom random;
            private readonly int clearTile;
            private readonly bool useBlueTiberium;
            private readonly float wildlifeFactor;
            private readonly House neutral;
            private readonly UnitType[] creatures;
            private readonly InfantryType fiend;
            private readonly IReadOnlySet<Point2D> protectedGround;
            private readonly Action<MapTile> recalculateRamp;
            private readonly OverlayType[] green;
            private readonly OverlayType[] blue;
            private readonly TerrainType[] trees;
            private readonly int stride;
            private readonly int[] spread;

            public NativeTiberium(Map map, Map source, GeneratorRandom random, int clearTile, bool useBlueTiberium,
                int wildlife, IReadOnlySet<Point2D> protectedGround, Action<MapTile> recalculateRamp)
            {
                this.map = map;
                this.random = random;
                this.clearTile = clearTile;
                this.useBlueTiberium = useBlueTiberium;
                wildlifeFactor = wildlife * 0.01f;
                this.protectedGround = protectedGround;
                this.recalculateRamp = recalculateRamp;

                green = LoadOverlays(source, "TIB");
                if (useBlueTiberium)
                    blue = LoadOverlays(source, "TIB2_");
                trees = new[] { "TIBTRE01", "TIBTRE02", "TIBTRE03" }.Select(name =>
                    source.Rules.TerrainTypes.Find(type => type.ININame == name) ??
                    throw new InvalidOperationException($"The loaded rules lack the stock terrain object {name}.")).ToArray();

                if (wildlife > 0)
                {
                    neutral = source.FindHouse("Neutral") ?? throw new InvalidOperationException("The map lacks the stock Neutral house.");
                    creatures = TiberiumWildlifeTypes.Take(3).Select(name =>
                        source.Rules.UnitTypes.Find(type => type.ININame == name) ??
                        throw new InvalidOperationException($"The loaded rules lack the stock creature {name}.")).ToArray();
                    fiend = source.Rules.InfantryTypes.Find(type => type.ININame == TiberiumWildlifeTypes[3]) ??
                        throw new InvalidOperationException("The loaded rules lack the stock Tiberian fiend.");
                }

                stride = map.Size.X + map.Size.Y + 1;
                spread = new int[stride * stride];
            }

            private static OverlayType[] LoadOverlays(Map source, string prefix)
            {
                return Enumerable.Range(1, 12).Select(index =>
                {
                    string name = prefix + index.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                    return source.Rules.OverlayTypes.Find(type => type.ININame == name) ??
                        throw new InvalidOperationException($"The loaded rules lack the stock overlay {name}.");
                }).ToArray();
            }

            public void Place(List<Point2D> sites, int amount)
            {
                int fieldAmount = map.Waypoints.Count * (30 * amount + 2500) / sites.Count;
                for (int index = 0; index < sites.Count; index++)
                {
                    int budget = (int)(fieldAmount + random.NativeTruncatedNormal(0.0, 50.0, -100.0, 100.0));
                    bool primary = !useBlueTiberium || random.Pick(0, 100) < 70;
                    bool placeTree = primary && random.Pick(0, 100) < 25;
                    if (budget >= 0)
                        GrowPatch(sites[index], budget, index + 1, primary ? green : blue, placeTree, true);
                }

                double[] meanDistances = map.Waypoints.Select(waypoint =>
                    sites.Average(site => Distance(waypoint.Position, site))).ToArray();
                double closest = meanDistances.Min();
                bool primaryAtStarts = !useBlueTiberium || random.Pick(0, 100) < 75;
                for (int player = 0; player < map.Waypoints.Count; player++)
                {
                    int budget = (int)((meanDistances[player] - closest) * 15.0) + 500;
                    GrowPatch(map.Waypoints[player].Position, budget, player + 1, primaryAtStarts ? green : blue, primaryAtStarts, false);
                }
            }

            private void GrowPatch(Point2D seed, int amount, int patch, OverlayType[] overlays, bool placeTree, bool spawnWildlife)
            {
                // The subtraction is unsigned in the native code. Win32 returns INT_MIN if
                // the scaled result is outside the signed range. Both rolls are unconditional.
                double budget = unchecked((uint)(random.Pick(0, 4) - 2)) * (double)wildlifeFactor;
                int wildlife = budget >= 2147483648.0 ? int.MinValue : (int)budget;
                int wildlifeTrigger = random.Pick(0, amount);

                var points = new Point2D[10 * amount];
                var queue = new NativePriorityQueue<int>();
                Point2D origin = seed;
                Point2D? firstPlaced = null;
                bool reachedOpenGround = false;
                int placed = 0;
                int restarts = 0;
                int current = -1;
                int next = 0;

                while (placed < amount && restarts < 10)
                {
                    if (current < 0)
                    {
                        queue.Clear();
                        Array.Clear(spread);
                        points[0] = seed;
                        spread[IndexOf(seed)] = patch;
                        origin = seed;
                        current = 0;
                        next = 1;
                        reachedOpenGround = false;
                        restarts++;
                    }

                    Point2D point = points[current];
                    MapTile tile = map.GetTileOrFail(point);
                    if (!protectedGround.Contains(point))
                    {
                        if (!reachedOpenGround)
                        {
                            origin = point;
                            queue.Clear();
                            next = 0;
                            reachedOpenGround = true;
                        }

                        if (tile.Overlay == null)
                        {
                            tile.Overlay = new Overlay { Position = point, OverlayType = overlays[random.Pick(0, 11)] };
                            placed++;
                        }
                        else if (tile.Overlay.FrameIndex < MaxGrowth)
                        {
                            tile.Overlay.FrameIndex++;
                            placed++;
                        }

                        firstPlaced ??= point;
                        if (placed == wildlifeTrigger && wildlife > 0 && spawnWildlife)
                        {
                            PlaceWildlife(tile);
                            wildlife--;
                            if (placed + 1 < amount)
                                wildlifeTrigger = random.Pick(placed + 1, amount);
                        }
                    }

                    foreach (Point2D offset in adjacent)
                    {
                        // Resetting the pool may overwrite the node being expanded. Re-read
                        // its position in every direction, as the game does.
                        Point2D neighbor = points[current] + offset;
                        MapTile neighborTile = map.GetTile(neighbor);
                        if (neighborTile == null || (neighborTile.TileIndex != clearTile && neighborTile.TileIndex != ushort.MaxValue) ||
                            !InNativeRadarArea(neighbor, neighborTile.Level, RampType.None, map.Size.X, map.LocalSize))
                        {
                            continue;
                        }

                        int index = IndexOf(neighbor);
                        Overlay existing = neighborTile.Overlay;
                        bool hasTiberium = existing?.OverlayType.Tiberium == true ||
                            neighborTile.TerrainObject?.TerrainType.SpawnsTiberium == true;
                        if ((existing == null && spread[index] != patch) || ((existing?.FrameIndex ?? 0) < MaxGrowth && hasTiberium))
                        {
                            points[next] = neighbor;
                            float score = (float)((random.NextValue() * 5.0) * (1.0 / uint.MaxValue) + Distance(origin, neighbor));
                            spread[index] = patch;
                            queue.Add(next++, score);
                        }
                    }

                    current = queue.TryTake(out int indexTaken) ? indexTaken : -1;
                }

                if (placeTree && firstPlaced is Point2D treePosition)
                {
                    MapTile tile = map.GetTileOrFail(treePosition);
                    tile.Overlay = null;
                    TerrainType tree = trees[random.Pick(0, trees.Length - 1)];
                    if (tile.TerrainObject != null)
                        map.RemoveTerrainObject(treePosition);

                    map.AddTerrainObject(new TerrainObject(tree, treePosition));
                    recalculateRamp?.Invoke(tile);
                }
            }

            private void PlaceWildlife(MapTile tile)
            {
                Point2D position = tile.CoordsToPoint();
                int creature = random.Pick(0, TiberiumWildlifeTypes.Length - 1);
                if (creature < creatures.Length)
                {
                    map.PlaceUnit(new Unit(creatures[creature]) { Owner = neutral, Position = position });
                }
                else if (tile.Infantry[(int)SubCell.Center] == null)
                {
                    // The editor stores one object per subcell, as when it reads a native map.
                    map.PlaceInfantry(new Infantry(fiend) { Owner = neutral, Position = position, SubCell = SubCell.Center });
                }
            }

            private int IndexOf(Point2D point) => point.X + stride * point.Y;
        }

        private sealed class NativeStarts
        {
            private readonly Map map;
            private readonly GeneratorRandom random;
            private readonly int clearTile;
            private readonly NativeStartZones zones;
            private readonly Func<Rectangle, bool> fitsStartArea;
            private readonly MapTile[] grid;
            private readonly int[] owners;
            private readonly int stride;

            public NativeStarts(Map map, GeneratorRandom random, int clearTile,
                NativeStartZones zones, IReadOnlyDictionary<Point2D, int> regionOwners, Func<Rectangle, bool> fitsStartArea)
            {
                this.map = map;
                this.random = random;
                this.clearTile = clearTile;
                this.zones = zones;
                this.fitsStartArea = fitsStartArea;
                stride = map.Size.X + map.Size.Y + 1;
                grid = new MapTile[stride * stride];
                owners = new int[grid.Length];
                map.DoForAllValidTiles(cell =>
                {
                    Point2D point = cell.CoordsToPoint();
                    int index = IndexOf(point);
                    grid[index] = cell;
                    owners[index] = regionOwners == null ? 0 : regionOwners[point];
                });
            }

            // The region picker writes its most recent sample into raw grid slot zero.
            public MapTile SampleSlotZero => grid[0];
            public bool SampleSlotZeroProtected { get; private set; }

            private int IndexOf(Point2D point) => point.X + point.Y * stride;

            public List<Point2D> Place(int players, int fields, HashSet<Point2D> protectedGround)
            {
                int regionThreshold = (int)Math.Max(600.0, 0.06 * map.LocalSize.Width * map.LocalSize.Height);
                while (true)
                {
                    var eligible = new List<int>();
                    var counts = new SortedDictionary<int, int>();
                    for (int index = 0; index < grid.Length; index++)
                    {
                        if (grid[index] != null && owners[index] >= 0)
                            counts[owners[index]] = counts.GetValueOrDefault(owners[index]) + 1;
                    }

                    foreach (var (region, count) in counts)
                    {
                        if (count <= regionThreshold)
                            continue;

                        MapTile cell = PickRandomClear(region);
                        if (cell != null && zones.ZoneAt(cell.CoordsToPoint()) == zones.BestZone)
                            eligible.Add(region);
                    }

                    // A large zone can have clear cells yet contain too few legal start footprints.
                    // Use the native whole-map picker when the selected regions cannot supply enough starts.
                    if (eligible.Count > 0 && !HasStartCandidates(eligible, players))
                        eligible.Clear();
                    if (eligible.Count == 0 && !HasStartCandidates(eligible, players))
                        throw new InvalidOperationException(Text("NoStarts", "There is too little connected clear ground for the requested players. Try a larger map or less water."));

                    int sampleCount = 15 * players;
                    var samples = new List<Point2D>(sampleCount);
                    while (samples.Count < sampleCount)
                    {
                        MapTile candidate = eligible.Count == 0 ? PickRandomClear(null)
                            : PickInZone(eligible[random.Pick(0, eligible.Count - 1)], protectedGround);
                        if (candidate != null && CanPlaceStart(candidate.CoordsToPoint()))
                            samples.Add(candidate.CoordsToPoint());
                    }

                    var layout = Spread(samples, (int)(fields / 100.0 * 10 + players + 3));
                    Point2D[] starts = layout.Take(players).ToArray();

                    // The native Clear_Inviolates routine leaves existing flags in place on a retry.
                    if (starts.Distinct().Count() != players || !ReserveStarts(starts, protectedGround))
                        continue;

                    for (int player = 0; player < starts.Length; player++)
                        map.AddWaypoint(new Waypoint { Identifier = player, Position = starts[player] });

                    layout.RemoveRange(0, players);
                    return layout;
                }
            }

            private MapTile PickRandomClear(int? region)
            {
                for (int attempt = 0; attempt <= 200; attempt++)
                {
                    int index;
                    do
                    {
                        index = random.Pick(0, grid.Length - 1);
                    }
                    while (grid[index] == null || (region.HasValue && owners[index] != region.Value));

                    if (attempt < 200 && IsNativeStartClear(grid[index].TileIndex))
                        return grid[index];
                }

                return null;
            }

            private MapTile PickInZone(int region, IReadOnlySet<Point2D> protectedGround)
            {
                for (int attempt = 1; attempt <= 100; attempt++)
                {
                    int index = random.Pick(0, grid.Length - 1);

                    // OpenTS writes the picked record to slot zero, which can itself be drawn later.
                    MapTile cell = grid[0] = grid[index];
                    owners[0] = owners[index];
                    if (index != 0)
                        SampleSlotZeroProtected = cell != null && protectedGround.Contains(cell.CoordsToPoint());
                    if (attempt == 100)
                        break;

                    if (cell != null && owners[0] == region && IsNativeStartClear(cell.TileIndex) &&
                        zones.ZoneAt(cell.CoordsToPoint()) == zones.BestZone)
                    {
                        return cell;
                    }
                }

                return null;
            }

            private List<Point2D> Spread(List<Point2D> candidates, int size)
            {
                double Score(Point2D first, Point2D second) =>
                    Distance(first, second) + (owners[IndexOf(first)] == owners[IndexOf(second)] ? 0.0 : 20.0);

                int firstIndex = 0;
                double farthest = -1.0;
                for (int first = 0; first < candidates.Count - 1; first++)
                {
                    for (int second = first + 1; second < candidates.Count; second++)
                    {
                        double score = Score(candidates[first], candidates[second]);
                        if (score > farthest)
                        {
                            firstIndex = first;
                            farthest = score;
                        }
                    }
                }

                var layout = new List<Point2D>(size) { candidates[firstIndex] };
                while (layout.Count < size)
                {
                    Point2D best = default;
                    double bestScore = -1.0;
                    foreach (Point2D candidate in candidates)
                    {
                        double score = layout.Min(cell => Score(candidate, cell));
                        if (score > bestScore)
                        {
                            best = candidate;
                            bestScore = score;
                        }
                    }

                    layout.Add(best);
                }

                return layout;
            }

            private bool HasStartCandidates(IReadOnlyList<int> eligible, int players)
            {
                int count = 0;
                for (int index = 1; index < grid.Length; index++)
                {
                    MapTile cell = grid[index];
                    if (cell == null || !IsNativeStartClear(cell.TileIndex))
                        continue;

                    Point2D point = cell.CoordsToPoint();
                    if ((eligible.Count == 0 || (eligible.Contains(owners[index]) && zones.ZoneAt(point) == zones.BestZone)) &&
                        CanPlaceStart(point) && ++count >= players)
                    {
                        return true;
                    }
                }

                return false;
            }

            private bool CanPlaceStart(Point2D point)
            {
                var footprint = new Rectangle(point.X - 5, point.Y - 5, 10, 10);
                if (!fitsStartArea(footprint))
                    return false;

                Rectangle local = map.LocalSize;
                Rectangle bounds = new Rectangle(local.X + 4, local.Y + 4, local.Width - 8, local.Height - 8);
                int height = map.GetTileOrFail(point).Level;
                return InNativeRadarArea(point, height, RampType.None, map.Size.X, bounds);
            }

            private bool ReserveStarts(IReadOnlyList<Point2D> startCells, HashSet<Point2D> protectedCells)
            {
                const int protectionSize = 400;

                foreach (Point2D start in startCells)
                {
                    var visited = new HashSet<Point2D> { start };
                    var queue = new NativePriorityQueue<Point2D>();
                    queue.Add(start, 0f);

                    int spreadCount = 0;
                    while (queue.TryTake(out Point2D point) && spreadCount < protectionSize)
                    {
                        protectedCells.Add(point);
                        spreadCount++;

                        foreach (Point2D direction in adjacent)
                        {
                            Point2D neighbor = point + direction;
                            var tile = map.GetTile(neighbor);
                            if (tile != null && IsNativeStartClear(tile.TileIndex) && visited.Add(neighbor))
                                queue.Add(neighbor, (float)Distance(neighbor, start));
                        }
                    }

                    if (spreadCount < protectionSize)
                        return false;
                }

                return true;
            }

            private bool IsNativeStartClear(int tileIndex)
            {
                return tileIndex == clearTile || tileIndex == 0xFFFF;
            }
        }

        private sealed class NativeStartZones
        {
            private const int OutsideZone = 0xFFFF;
            private static readonly LandType[] landByTerrainType =
            {
                LandType.Clear, LandType.Ice, LandType.Ice, LandType.Ice, LandType.Ice, LandType.Tunnel, LandType.Railroad, LandType.Rock,
                LandType.Rock, LandType.Water, LandType.Beach, LandType.Road, LandType.Road, LandType.Clear, LandType.Rough, LandType.Rock
            };

            private static readonly Point2D[] cliffBackOffsets =
            {
                new Point2D(0, -1), new Point2D(-1, 0), new Point2D(2, 2),
                new Point2D(1, 1), new Point2D(-1, 1), new Point2D(1, -1)
            };

            private enum Passability : byte
            {
                Outside,
                Land,
                Water,
                Impassable,
                Crushable,
                Blocked,
                PartiallyBlocked
            }

            private readonly int stride;
            private readonly int[] rawZones;
            private readonly int[] movementZones;

            public NativeStartZones(Map map, Func<int, ITileImage> currentTile, bool impassableCliffBacks,
                IReadOnlyDictionary<LandType, float> wheelCosts, Func<MapTile, (RampType Ramp, byte TerrainType)> originalAttributes)
            {
                stride = map.Size.X + map.Size.Y + 1;
                rawZones = new int[stride * stride];
                var height = new byte[rawZones.Length];
                var passability = new Passability[rawZones.Length];

                Rectangle local = map.LocalSize;
                int width = map.Size.X;
                bool arctic = map.LoadedTheaterName.Equals("Snow", StringComparison.OrdinalIgnoreCase);
                map.DoForAllValidTiles(cell =>
                {
                    Point2D point = cell.CoordsToPoint();
                    int index = Index(point);
                    height[index] = cell.Level;
                    var (ramp, terrainType) = originalAttributes != null
                        ? originalAttributes(cell)
                        : ReadTileAttributes(cell, currentTile);
                    if (!InNativeRadarArea(point, cell.Level, ramp, width, local))
                        return;

                    // Recalc_Attributes uses the original tile part even when Fixup_LAT changes its tile ID.
                    LandType land = landByTerrainType[terrainType];
                    OverlayType overlay = cell.Overlay?.OverlayType;
                    bool usesOverlayLand = UsesNativeOverlayLand(overlay);
                    if (usesOverlayLand)
                        land = overlay.Land;

                    if (impassableCliffBacks &&
                        (usesOverlayLand || land is LandType.Clear or LandType.Ice or LandType.Water or LandType.Beach) &&
                        cliffBackOffsets.Any(offset => map.GetTile(point + offset)?.Level >= cell.Level + 4))
                    {
                        land = LandType.Rock;
                    }

                    passability[index] = ClassifyPassability(cell, land, overlay, wheelCosts, arctic);
                });

                var rawZonePassability = new List<Passability> { Passability.Outside };
                var adjacency = new HashSet<(int First, int Second)>();
                int largestRawZone = 0;
                int largestSpan = -1;
                int cellIndex = 0;
                while (cellIndex < rawZones.Length)
                {
                    Passability pass = passability[cellIndex];
                    if (pass == Passability.Outside || rawZones[cellIndex] != 0)
                    {
                        cellIndex++;
                        continue;
                    }

                    int zone = rawZonePassability.Count;
                    int span = FillZone(cellIndex, zone, pass, passability, height, adjacency, out int skip);
                    if (span > largestSpan)
                    {
                        largestRawZone = zone;
                        largestSpan = span;
                    }

                    rawZonePassability.Add(pass);
                    cellIndex += skip;
                }

                movementZones = StitchLandZones(rawZonePassability, adjacency);
                BestZone = movementZones[largestRawZone];
            }

            public int BestZone { get; }

            public int ZoneAt(Point2D point)
            {
                if (point.X < 0 || point.Y < 0 || point.X >= stride || point.Y >= stride)
                    return OutsideZone;

                return movementZones[rawZones[Index(point)]];
            }

            private int Index(Point2D point) => point.X + point.Y * stride;

            private static (RampType Ramp, byte TerrainType) ReadTileAttributes(MapTile cell, Func<int, ITileImage> currentTile)
            {
                ITileImage tile = currentTile(cell.TileIndex);
                TmpImage image = cell.SubTileIndex < tile.SubTileCount ? tile.GetSubTile(cell.SubTileIndex)?.TmpImage : null;

                return image != null ? (image.RampType, image.TerrainType) : (RampType.None, (byte)0);
            }

            private static Passability ClassifyPassability(MapTile cell, LandType land, OverlayType overlay,
                IReadOnlyDictionary<LandType, float> wheelCosts, bool arctic)
            {
                if (overlay != null)
                {
                    if (overlay.Crushable)
                        return Passability.Crushable;
                    if (overlay.Wall)
                        return Passability.Blocked;
                    if (wheelCosts[overlay.Land] == 0)
                        return Passability.Impassable;
                }

                if (land is LandType.Water or LandType.Beach)
                    return Passability.Water;
                if (wheelCosts[land] <= 0.01)
                    return Passability.Impassable;

                TerrainType terrain = cell.TerrainObject?.TerrainType;
                if (terrain == null)
                    return Passability.Land;

                TerrainOccupation occupied = arctic ? terrain.SnowOccupationBits : terrain.TemperateOccupationBits;
                const TerrainOccupation full = TerrainOccupation.East | TerrainOccupation.West | TerrainOccupation.South;
                return occupied == full ? Passability.Blocked : Passability.PartiallyBlocked;
            }

            public static IReadOnlyDictionary<LandType, float> ReadWheelCosts(IniFile rules, IniFile firestorm) =>
                Enum.GetValues<LandType>().ToDictionary(land => land,
                    land => ReadWheelCost(firestorm, land.ToString(), ReadWheelCost(rules, land.ToString(), 0)));

            private static float ReadWheelCost(IniFile ini, string land, float fallback)
            {
                string text = ini?.GetStringValue(land, "Wheel", "");
                if (string.IsNullOrWhiteSpace(text))
                    return fallback;

                int percent = text.IndexOf('%');
                string number = percent >= 0 ? text.Substring(0, percent) : text;
                if (!float.TryParse(number, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float cost))
                {
                    return fallback;
                }

                return (float)Math.Min(percent >= 0 ? cost / 100.0 : cost, 1.0);
            }

            private int FillZone(int index, int zone, Passability pass, Passability[] passability, byte[] height,
                HashSet<(int First, int Second)> adjacency, out int skip)
            {
                int lastAdjacentZone = 0;
                int filled = 0;
                int diagonalStride = stride + 1;
                var pending = new Stack<SpanFrame>();

                void RecordAdjacency(int neighbor, int otherHeight)
                {
                    int other = rawZones[neighbor];
                    if (other == 0 || other == zone || other == lastAdjacentZone ||
                        (pass != Passability.Impassable && Math.Abs(height[neighbor] - otherHeight) >= 2))
                    {
                        return;
                    }

                    adjacency.Add((other, zone));
                    lastAdjacentZone = other;
                }

                SpanFrame ExpandSpan(int origin, out int advance)
                {
                    int currentHeight = height[origin];
                    int left = origin;
                    while (passability[left] == pass && Math.Abs(height[left] - currentHeight) < 2)
                    {
                        rawZones[left] = zone;
                        currentHeight = height[left];
                        left--;
                    }

                    RecordAdjacency(left, currentHeight);

                    // Native Zone_Span carries the leftmost height into the rightward scan.
                    int right = origin;
                    while (passability[right] == pass && Math.Abs(height[right] - currentHeight) < 4)
                    {
                        rawZones[right] = zone;
                        currentHeight = height[right];
                        right++;
                    }

                    RecordAdjacency(right, currentHeight);
                    filled += right - left - 1;
                    advance = right - origin - 1;
                    left++;
                    right--;

                    return new SpanFrame
                    {
                        Upper = left - diagonalStride,
                        UpperEnd = right - stride + 1,
                        Lower = left + stride - 1,
                        LowerEnd = right + diagonalStride
                    };
                }

                SpanFrame frame = ExpandSpan(index, out skip);
                while (true)
                {
                    bool upper = frame.Upper <= frame.UpperEnd;
                    if (!upper && frame.Lower > frame.LowerEnd)
                    {
                        if (pending.Count == 0)
                            return filled;

                        frame = pending.Pop();
                        continue;
                    }

                    int neighbor = upper ? frame.Upper : frame.Lower;
                    int rowEnd = upper ? frame.UpperEnd : frame.LowerEnd;
                    int adjacent = (upper ? neighbor + stride : neighbor - stride) +
                        (neighbor < rowEnd - 1 ? 1 : neighbor == rowEnd - 1 ? 0 : -1);
                    int advance = 1;
                    if (rawZones[neighbor] != 0)
                    {
                        RecordAdjacency(neighbor, height[adjacent]);
                    }
                    else if (passability[neighbor] == pass && Math.Abs(height[neighbor] - height[adjacent]) < 2)
                    {
                        SpanFrame child = ExpandSpan(neighbor, out advance);
                        frame.Move(upper, advance);
                        pending.Push(frame);
                        frame = child;
                        continue;
                    }

                    frame.Move(upper, advance);
                }
            }

            private struct SpanFrame
            {
                public int Upper;
                public int UpperEnd;
                public int Lower;
                public int LowerEnd;

                public void Move(bool upper, int advance)
                {
                    if (upper)
                        Upper += advance;
                    else
                        Lower += advance;
                }
            }

            private static int[] StitchLandZones(List<Passability> passability, HashSet<(int First, int Second)> adjacency)
            {
                // MZONE_NORMAL connects land zones. Low bridge overlays contribute passable ground directly.
                var representatives = new int[passability.Count];
                for (int zone = 1; zone < representatives.Length; zone++)
                    representatives[zone] = zone;

                int Root(int zone)
                {
                    while (zone != representatives[zone])
                    {
                        representatives[zone] = representatives[representatives[zone]];
                        zone = representatives[zone];
                    }

                    return zone;
                }

                foreach (var (first, second) in adjacency)
                {
                    if (passability[first] != Passability.Land || passability[second] != Passability.Land)
                        continue;

                    int left = Root(first);
                    int right = Root(second);
                    if (left != right)
                        representatives[Math.Max(left, right)] = Math.Min(left, right);
                }

                int nextMovementZone = 2;
                var result = new int[representatives.Length];
                result[0] = OutsideZone;
                for (int zone = 1; zone < result.Length; zone++)
                {
                    if (passability[zone] != Passability.Land)
                    {
                        result[zone] = 1;
                    }
                    else
                    {
                        int root = Root(zone);
                        result[zone] = root == zone ? nextMovementZone++ : result[root];
                    }
                }

                return result;
            }
        }

        private sealed class NativeRegions
        {
            private const int OriginalSide = -2;
            private const int NewSide = -3;
            private const int CliffHeight = 4;

            private sealed class Region
            {
                public readonly int Id;
                public readonly List<Point2D> Cells = new List<Point2D>();
                public int Height;
                public bool ContainsWater;
                public bool Unsplittable;
                public bool Discarded;

                public Region(int id, int height)
                {
                    Id = id;
                    Height = height;
                }
            }

            private readonly Map map;
            private readonly Map source;
            private readonly GeneratorRandom random;
            private readonly int clearTile;
            private readonly Func<string, TileSet> tileset;
            private readonly Func<string, int> tileStart;
            private readonly Func<int, ITileImage> currentTile;
            private readonly List<Point2D> grid = new List<Point2D>();
            private readonly List<Region> regions = new List<Region>();
            private readonly Dictionary<Point2D, int> owners = new Dictionary<Point2D, int>();
            private readonly LiquidWater liquid;
            private readonly Point2D[] mapOrder;
            private int slopePiecesStart;
            private int rampStart;
            private int nextId;
            private int? cliffTilesStart;
            private int cliffRampsStart;

            public NativeRegions(Map map, Map source, GeneratorRandom random, int clearTile, Func<string, TileSet> tileset,
                Func<string, int> tileStart, Func<int, ITileImage> currentTile, LiquidWater liquid)
            {
                this.map = map;
                this.source = source;
                this.random = random;
                this.clearTile = clearTile;
                this.tileset = tileset;
                this.tileStart = tileStart;
                this.currentTile = currentTile;
                this.liquid = liquid;
                map.DoForAllValidTiles(cell => grid.Add(cell.CoordsToPoint()));
                mapOrder = InIsometricOrder(map).Select(cell => cell.CoordsToPoint()).ToArray();
                if (liquid == null)
                {
                    var initial = new Region(nextId++, CliffHeight);
                    regions.Add(initial);
                    foreach (Point2D point in grid)
                        owners.Add(point, initial.Id);
                }
                else
                {
                    InitializeWetRegions();
                }

                // Make_Ice_Regions fills the cell lists in descending order of the native grid.
                for (int i = grid.Count - 1; i >= 0; i--)
                {
                    int owner = owners[grid[i]];
                    if (owner >= 0)
                        regions[owner].Cells.Add(grid[i]);
                }
            }

            public IReadOnlyDictionary<Point2D, int> Owners => owners;

            private void ResetOwnership()
            {
                regions.Clear();
                nextId = 0;
                foreach (Point2D point in grid)
                    owners[point] = -1;
            }

            private void InitializeWetRegions()
            {
                ResetOwnership();
                var spread = new Dictionary<Point2D, int>();
                foreach (Point2D point in grid)
                {
                    if (owners[point] == -1 && liquid.IsWaterFeature(map.GetTileOrFail(point)))
                        CreateRegion(point, spread);
                }

                foreach (Region region in regions)
                {
                    GrowWaterMargin(region);
                    ClaimExpandableGround(region);
                }

                CreateRemainingRegions(spread);
            }

            private void GrowWaterMargin(Region region)
            {
                IEnumerable<Point2D> border = Border(region.Id).ToArray();
                for (int ring = 0; ring < 4; ring++)
                {
                    var next = new List<Point2D>();
                    foreach (Point2D point in border)
                    {
                        foreach (Point2D offset in adjacent)
                        {
                            Point2D neighbor = point + offset;
                            MapTile cell = map.GetTile(neighbor);
                            if (cell == null || cell.Level != region.Height)
                                continue;

                            int owner = owners[neighbor];
                            if (owner == -1 && IsCliffPassClear(cell))
                            {
                                owners[neighbor] = region.Id;
                                next.Add(neighbor);
                            }
                            else if (owner != region.Id)
                            {
                                return;
                            }
                        }
                    }

                    border = next;
                }
            }

            private void ClaimExpandableGround(Region region)
            {
                var pending = new Stack<Point2D>(Border(region.Id));
                while (pending.TryPop(out Point2D point))
                {
                    foreach (Point2D offset in adjacent)
                    {
                        Point2D neighbor = point + offset;
                        if (liquid.Expandable.Contains(neighbor) && owners[neighbor] == -1)
                        {
                            owners[neighbor] = region.Id;
                            pending.Push(neighbor);
                        }
                    }
                }
            }

            private IEnumerable<Point2D> Border(int id) =>
                mapOrder.Where(point => owners[point] == id && adjacent.Any(offset =>
                    owners.TryGetValue(point + offset, out int other) && other != id));

            public void SplitOversizedRegions(int regionSize)
            {
                double limit = (regionSize * 0.005 + 0.05) * map.LocalSize.Width * map.LocalSize.Height;
                int threshold = (int)(limit + limit);
                while (true)
                {
                    Region oversized = null;
                    foreach (Region region in regions)
                    {
                        if (region.Unsplittable || region.ContainsWater)
                            continue;
                        if (region.Cells.Count > threshold)
                        {
                            oversized = region;
                            break;
                        }

                        region.Unsplittable = true;
                    }

                    if (oversized == null)
                        return;

                    Split(oversized);
                }
            }

            private void Split(Region original)
            {
                foreach (Point2D point in original.Cells)
                    owners[point] = OriginalSide;

                int count = random.Pick(original.Cells.Count / 8, original.Cells.Count / 3);
                Point2D origin = original.Cells[random.Pick(0, original.Cells.Count - 1)];
                double angle = random.NextValue() * (2 * Math.PI / uint.MaxValue);
                var queued = new HashSet<Point2D> { origin };
                var queue = new NativePriorityQueue<Point2D>();
                queue.Add(origin, 0);
                for (int cells = 0; queue.TryTake(out Point2D point) && cells < count; cells++)
                {
                    owners[point] = NewSide;
                    for (int i = 0; i < adjacent.Length; i += 2)
                    {
                        Point2D neighbor = point + adjacent[i];
                        if (owners.TryGetValue(neighbor, out int owner) && owner == OriginalSide &&
                            IsCliffPassClear(map.GetTileOrFail(neighbor)) && queued.Add(neighbor))
                            queue.Add(neighbor, (float)AngleScore(origin, neighbor, angle));
                    }

                    angle += random.SampleNormal() * (22.5 * Math.PI / 180);
                }

                // The native loop has already fetched one extra cell before the frontier is assigned.
                while (queue.TryTake(out Point2D remaining))
                    owners[remaining] = NewSide;

                var replacements = new List<Region>();
                for (int i = original.Cells.Count - 1; i >= 0; i--)
                {
                    Point2D point = original.Cells[i];
                    int side = owners[point];
                    if (side == OriginalSide || side == NewSide)
                        replacements.Add(CollectRegion(point, side, original.Height));
                }

                for (int i = replacements.Count - 1; i >= 0; i--)
                    AssignHeight(replacements[i]);

                regions.RemoveAll(region => region == original || region.Discarded);
            }

            private double AngleScore(Point2D origin, Point2D neighbor, double referenceAngle)
            {
                int dx = neighbor.X - origin.X;
                int dy = neighbor.Y - origin.Y;
                double angle = dx != 0 ? Math.Atan(-(double)dy / dx) : Math.PI / 2;
                if (dx < 0)
                    angle += Math.PI;

                double difference = Math.Abs(angle - referenceAngle);
                while (difference >= 2 * Math.PI)
                    difference -= 2 * Math.PI;
                if (difference > Math.PI)
                    difference = 2 * Math.PI - difference;

                return random.Fraction() * 2.5 + 0.15 * Distance(origin, neighbor) + 1.5 * difference;
            }

            private Region CollectRegion(Point2D origin, int side, int height)
            {
                var region = new Region(nextId++, height);
                regions.Add(region);
                var open = new Stack<Point2D>();
                open.Push(origin);
                owners[origin] = region.Id;
                region.Cells.Add(origin);
                while (open.TryPop(out Point2D point))
                {
                    foreach (Point2D offset in adjacent)
                    {
                        Point2D neighbor = point + offset;
                        if (owners.TryGetValue(neighbor, out int owner) && owner == side)
                        {
                            region.Cells.Add(neighbor);
                            owners[neighbor] = region.Id;
                            open.Push(neighbor);
                        }
                    }
                }

                return region;
            }

            private void AssignHeight(Region region)
            {
                var neighbors = new HashSet<int>();
                foreach (Point2D point in region.Cells)
                {
                    foreach (Point2D offset in adjacent)
                    {
                        if (owners.TryGetValue(point + offset, out int owner) && owner != region.Id)
                            neighbors.Add(owner);
                    }
                }

                // Live regions are kept in ascending ID order, which breaks ties when merging.
                Region largest = null;
                int low = -1;
                int high = -1;
                foreach (Region other in regions)
                {
                    if (!neighbors.Contains(other.Id) || other.Discarded)
                        continue;
                    if (!other.ContainsWater && (largest == null || largest.Cells.Count < other.Cells.Count))
                        largest = other;
                    low = low == -1 ? other.Height : Math.Min(low, other.Height);
                    high = Math.Max(high, other.Height);
                }

                if (region.Cells.Count <= 100)
                {
                    if (largest != null)
                    {
                        for (int i = region.Cells.Count - 1; i >= 0; i--)
                        {
                            Point2D point = region.Cells[i];
                            largest.Cells.Add(point);
                            owners[point] = largest.Id;
                            map.GetTileOrFail(point).Level = (byte)(map.GetTileOrFail(point).Level + largest.Height - region.Height);
                        }

                        region.Discarded = true;
                    }

                    return;
                }

                var heights = new List<int>(2);
                switch (high - low)
                {
                    case 0:
                        if (low >= CliffHeight)
                            heights.Add(low - CliffHeight);
                        if (high <= 7)
                            heights.Add(high + CliffHeight);
                        break;
                    case CliffHeight:
                        heights.Add(high);
                        heights.Add(low);
                        break;
                    case 2 * CliffHeight:
                        heights.Add((high + low) / 2);
                        break;
                }

                int height = heights[random.Pick(0, heights.Count - 1)];
                foreach (Point2D point in region.Cells)
                {
                    MapTile cell = map.GetTileOrFail(point);
                    cell.Level = (byte)(cell.Level + height - region.Height);
                }

                region.Height = height;
            }

            public void ExpandHighGround()
            {
                var pending = new Stack<MapTile>();
                foreach (MapTile origin in InIsometricOrder(map))
                {
                    pending.Push(origin);
                    while (pending.TryPop(out MapTile cell))
                    {
                        Point2D point = cell.CoordsToPoint();
                        int mask = HighGroundMask(cell);
                        if (!ShouldExpand(point, mask))
                            continue;

                        cell.Level += CliffHeight;
                        owners[point] = -1;
                        for (int i = adjacent.Length - 1; i >= 0; i--)
                        {
                            if (map.GetTile(point + adjacent[i]) is MapTile neighbor)
                                pending.Push(neighbor);
                        }
                    }
                }
            }

            public void BuildCliffTiles(bool mutated)
            {
                int cliffStart = tileset("CliffSet").StartTileIndex;
                cliffTilesStart = cliffStart;
                cliffRampsStart = tileset("CliffRamps").StartTileIndex;
                ExpandHighGround();

                int crystalStart = mutated ? tileset("CrystalCliff").StartTileIndex : 0;
                Func<Point2D, int> maskAt = point => HighGroundMask(map.GetTile(point));

                foreach (MapTile cell in InIsometricOrder(map))
                {
                    if (!IsCliffPassClear(cell))
                        continue;

                    // The native loop rolls before it checks for high ground, even on a flat cell.
                    int roll = random.Pick(0, 5);
                    int mask = HighGroundMask(cell);
                    if (mask == 0)
                        continue;

                    Point2D origin = cell.CoordsToPoint();
                    int variant = SelectCliffVariant(origin, mask, roll, maskAt);
                    int index = cliffStart + variant - 1;
                    if (mutated && variant is >= 9 and <= 14 && random.Fraction() < 0.05)
                    {
                        bool insideCorner = variant <= 11;
                        index = crystalStart + (insideCorner ? 3 : 2);

                        // The native offset lookup reads past the table for crystal cliffs.
                        // OpenTS Release reads a one-cell X offset for the outside corner.
                        if (!insideCorner)
                            origin += new Point2D(1, 0);
                    }

                    StampCliffPassTile(currentTile(index), origin, cell.Level);
                }
            }

            private bool IsCliffPassClear(MapTile cell) => cell.TileIndex == clearTile || cell.TileIndex == ushort.MaxValue;

            private void StampCliffPassTile(ITileImage tile, Point2D origin, int baseHeight)
            {
                const int cliffOwner = -1;

                for (int index = 0; index < tile.SubTileCount; index++)
                {
                    Point2D? offset = tile.GetSubTileCoordOffset(index);
                    if (!offset.HasValue)
                        continue;

                    Point2D point = origin + offset.Value;
                    MapTile cell = map.GetTile(point);
                    if (cell == null)
                        continue;

                    if (!IsCliffPassClear(cell))
                    {
                        // A collision with an already stamped cliff ends this footprint without rolling it back.
                        if (owners[point] == cliffOwner)
                            return;

                        continue;
                    }

                    owners[point] = cliffOwner;
                    cell.ChangeTileIndex(tile.TileID, (byte)index);
                    cell.Level = (byte)(baseHeight + tile.GetSubTile(index).TmpImage.Height);
                }
            }

            private bool ShouldExpand(Point2D point, int mask)
            {
                if (CliffNotch(mask) ||
                    (mask & (SW | NE)) == (SW | NE) &&
                    ((mask & (S | E)) != (S | E) || (mask & (N | NW | W)) != 0) &&
                    ((mask & (N | W)) != (N | W) || (mask & (S | SE | E)) != 0) ||
                    (mask & (NW | SE)) == (NW | SE) &&
                    ((mask & (W | S)) != (W | S) || (mask & (N | E | NE)) != 0) &&
                    ((mask & (N | E)) != (N | E) || (mask & (W | SW | S)) != 0))
                {
                    return true;
                }

                for (int i = 0; i < adjacent.Length; i += 2)
                {
                    int opposite = (i + 4) % adjacent.Length;
                    if ((mask & (1 << i)) != 0 &&
                        (HighGroundMask(map.GetTile(point + adjacent[opposite])) & (1 << opposite)) != 0)
                    {
                        return true;
                    }
                }

                return false;
            }

            private int HighGroundMask(MapTile cell)
            {
                if (cell == null)
                    return 0;

                Point2D point = cell.CoordsToPoint();
                int mask = 0;
                for (int i = 0; i < adjacent.Length; i++)
                {
                    MapTile neighbor = map.GetTile(point + adjacent[i]);
                    if (neighbor?.Level == cell.Level + CliffHeight && !IsCliffTile(neighbor))
                        mask |= 1 << i;
                }

                return mask;
            }

            // The stock CliffRamps range includes the slope pieces at both ends of a ramp.
            private bool IsCliffTile(MapTile cell) =>
                liquid?.IsWaterfallCliff(cell.TileIndex, cell.SubTileIndex) == true ||
                cliffTilesStart.HasValue && ((uint)(cell.TileIndex - cliffTilesStart.Value) < 40 ||
                    (uint)(cell.TileIndex - cliffRampsStart) < 20);

            public IReadOnlyDictionary<Point2D, int> Rebuild()
            {
                ResetOwnership();
                CreateRemainingRegions(new Dictionary<Point2D, int>());

                return owners;
            }

            private void CreateRemainingRegions(Dictionary<Point2D, int> spread)
            {
                foreach (Point2D origin in grid)
                {
                    if (owners[origin] == -1)
                        CreateRegion(origin, spread);
                }
            }

            private void CreateRegion(Point2D origin, Dictionary<Point2D, int> spread)
            {
                const int unassigned = -1;
                int id = nextId;
                MapTile start = map.GetTileOrFail(origin);
                byte height = start.Level;
                bool wet = liquid?.IsWaterFeature(start) == true;
                var region = new List<Point2D>();
                var open = new Stack<Point2D>();
                open.Push(origin);
                while (open.TryPop(out Point2D point))
                {
                    spread[point] = id;
                    owners[point] = id;
                    region.Add(point);
                    foreach (Point2D offset in adjacent)
                    {
                        Point2D neighbor = point + offset;
                        if (owners.TryGetValue(neighbor, out int owner) && owner == unassigned &&
                            spread.GetValueOrDefault(neighbor, unassigned) != id)
                        {
                            spread[neighbor] = id;
                            MapTile cell = map.GetTileOrFail(neighbor);
                            if (cell.Level == height && (liquid?.IsWaterFeature(cell) == true) == wet)
                                open.Push(neighbor);
                        }
                    }
                }

                if (!wet && region.Count < 75 && MergeSmallRegion(origin, region, id, height))
                    return;

                regions.Add(new Region(nextId++, height) { ContainsWater = wet });
            }

            private bool MergeSmallRegion(Point2D origin, List<Point2D> region, int id, byte height)
            {
                MapTile neighbor;
                if (id == 0)
                {
                    var border = region.Where(point => adjacent.Any(offset =>
                        owners.TryGetValue(point + offset, out int owner) && owner != id))
                        .OrderBy(point => point.Y).ThenBy(point => point.X).ToArray();
                    Point2D pick = border[random.Pick(0, border.Length - 1)];
                    neighbor = adjacent.Select(offset => map.GetTile(pick + offset))
                        .FirstOrDefault(tile => tile != null && tile.Level != height && liquid?.IsWaterTile(tile) != true);
                }
                else
                {
                    neighbor = map.GetTile(origin + adjacent[6]) ?? map.GetTile(origin + adjacent[0]);
                }

                if (neighbor == null)
                    return false;

                int newOwner = id == 0 ? -1 : owners[neighbor.CoordsToPoint()];
                foreach (Point2D point in region)
                {
                    MapTile cell = map.GetTileOrFail(point);
                    cell.ChangeTileIndex(clearTile, 0);
                    cell.Level = neighbor.Level;
                    owners[point] = newOwner;
                }

                return true;
            }

            public void Connect(int accessibility)
            {
                slopePiecesStart = tileset("SlopeSetPieces").StartTileIndex;
                rampStart = tileset("RampBase").StartTileIndex;
                cliffTilesStart = tileset("CliffSet").StartTileIndex;
                cliffRampsStart = tileset("CliffRamps").StartTileIndex;
                var neighbors = new SortedSet<int>[regions.Count];
                var counts = new int[regions.Count];
                for (int id = 0; id < neighbors.Length; id++)
                    neighbors[id] = new SortedSet<int>();

                foreach (Point2D point in grid)
                {
                    int id = owners[point];
                    if (id < 0)
                        continue;

                    counts[id]++;
                    foreach (Point2D offset in adjacent)
                    {
                        if (owners.TryGetValue(point + offset, out int other) && other >= 0 && other != id)
                            neighbors[id].Add(other);
                    }
                }

                // Neighbor lists and counts are fixed before the ramps and bridges edit ownership.
                for (int id = 0; id < neighbors.Length; id++)
                {
                    if (regions[id].ContainsWater)
                    {
                        ConnectBridgeNeighbors(id, neighbors, counts);
                        continue;
                    }

                    foreach (int other in neighbors[id])
                    {
                        if (other <= id || regions[id].Height == regions[other].Height)
                            continue;

                        int target = random.Pick(0, 100) < accessibility ? 1 + random.Pick(1, 2) : 1;
                        bool otherIsHigher = regions[other].Height > regions[id].Height;
                        int upper = otherIsHigher ? other : id;
                        int lower = otherIsHigher ? id : other;
                        Point2D[] border = Border(upper).ToArray();
                        int placed = 0;

                        for (int attempt = 0; attempt < 100 && placed < target; attempt++)
                        {
                            Point2D point = border[random.Pick(0, border.Length - 1)];
                            if (InNativeLocalRadar(point) && ConnectRamp(point, upper, lower, attempt > 50))
                                placed++;
                        }
                    }
                }
            }

            private void ConnectBridgeNeighbors(int water, IReadOnlyList<SortedSet<int>> neighbors, IReadOnlyList<int> counts)
            {
                int[] eligible = neighbors[water].Where(id => (neighbors[id].Count > 1 || counts[id] > 50) &&
                    regions[id].Height == regions[water].Height).ToArray();
                for (int i = 0; i < eligible.Length - 1; i++)
                {
                    for (int j = i + 1; j < eligible.Length; j++)
                    {
                        if (!regions[eligible[j]].ContainsWater)
                            ConnectBridge(water, eligible[i], eligible[j]);
                    }
                }
            }

            private void ConnectBridge(int water, int first, int second)
            {
                int stride = map.Size.X + map.Size.Y + 1;
                for (int attempt = 0; attempt < 200; attempt++)
                {
                    Point2D point;
                    do
                    {
                        int index = random.Pick(0, stride * stride - 1);
                        point = new Point2D(index % stride, index / stride);
                    }
                    while (!owners.TryGetValue(point, out int owner) || owner != water);

                    Rectangle? vertical = FindBridgeSpan(point, first, second, false);
                    Rectangle? horizontal = FindBridgeSpan(point, first, second, true);
                    bool eastWest = horizontal.HasValue && (!vertical.HasValue || horizontal.Value.Width <= vertical.Value.Height);
                    Rectangle? chosen = eastWest ? horizontal : vertical;
                    if (!chosen.HasValue)
                        continue;

                    Rectangle bridge = chosen.Value;
                    int length = eastWest ? bridge.Width : bridge.Height;
                    // The span includes both end cells. The native length test measures their separation.
                    if (length - 1 < attempt / 25 + 8 && IsBridgeAllowed(bridge))
                    {
                        PlaceBridge(bridge, eastWest);
                        return;
                    }
                }
            }

            private Rectangle? FindBridgeSpan(Point2D origin, int first, int second, bool eastWest)
            {
                Rectangle start = eastWest ? new Rectangle(origin.X, origin.Y - 1, 1, 3) : new Rectangle(origin.X - 1, origin.Y, 3, 1);
                Rectangle near = start;
                Rectangle far = start;
                if (!FindBridgeBank(ref near, eastWest, -1))
                    return null;

                Rectangle approach = eastWest ? new Rectangle(near.X - 3, near.Y, 3, 3) : new Rectangle(near.X, near.Y - 3, 3, 3);
                if (!BridgeRoadFits(approach) || !FindBridgeBank(ref far, eastWest, 1))
                    return null;

                approach = eastWest ? new Rectangle(far.X + 1, far.Y, 3, 3) : new Rectangle(far.X, far.Y + 1, 3, 3);
                if (!BridgeRoadFits(approach))
                    return null;

                int nearOwner = owners[new Point2D(near.X, near.Y)];
                int farOwner = owners[new Point2D(far.X, far.Y)];
                if ((nearOwner != first || farOwner != second) && (nearOwner != second || farOwner != first))
                    return null;

                return eastWest ? new Rectangle(near.X, near.Y, far.X - near.X + 1, 3) :
                    new Rectangle(near.X, near.Y, 3, far.Y - near.Y + 1);
            }

            private bool FindBridgeBank(ref Rectangle strip, bool eastWest, int step)
            {
                while (!BridgeRoadFits(strip))
                {
                    strip.Offset(eastWest ? step : 0, eastWest ? 0 : step);
                    var first = new Point2D(strip.X, strip.Y);
                    var last = new Point2D(strip.Right - 1, strip.Bottom - 1);
                    if (!InNativeLocalRadar(first) || !InNativeLocalRadar(last) ||
                        IsCliffTile(map.GetTileOrFail(first)) || IsCliffTile(map.GetTileOrFail(last)))
                    {
                        return false;
                    }
                }

                return true;
            }

            private bool BridgeRoadFits(Rectangle rect, bool requireLevel = false) =>
                CanPlaceNativePavedRoad(map, clearTile, tileStart, rect, false, false, requireLevel);

            private bool IsBridgeAllowed(Rectangle bridge)
            {
                int height = map.GetTile(bridge.X, bridge.Y).Level;
                // The terrain check includes one extra row and column beyond the painted deck.
                var envelope = new Rectangle(bridge.X, bridge.Y, bridge.Width + 1, bridge.Height + 1);
                foreach (Point2D point in RampArea(envelope))
                {
                    MapTile cell = map.GetTile(point);
                    if (cell == null || cell.Overlay != null || cell.Level != height ||
                        !(IsCliffPassClear(cell) || liquid.IsWaterTile(cell)))
                    {
                        return false;
                    }
                }

                return true;
            }

            private void PlaceBridge(Rectangle bridge, bool eastWest)
            {
                foreach (Point2D point in RampArea(bridge))
                {
                    int variant = eastWest
                        ? point.X == bridge.Left ? 21 : point.X == bridge.Right - 1 ? 19 : 1 + point.X % 4
                        : point.Y == bridge.Top ? 23 : point.Y == bridge.Bottom - 1 ? 25 : 10 + point.Y % 4;
                    string name = "LOBRDG" + variant.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
                    OverlayType type = source.Rules.OverlayTypes.Find(overlay => overlay.ININame == name) ??
                        throw new InvalidOperationException($"The loaded rules lack the stock bridge overlay {name}.");
                    map.GetTileOrFail(point).Overlay = new Overlay
                    {
                        Position = point,
                        OverlayType = type,
                        FrameIndex = eastWest ? point.Y - bridge.Y : point.X - bridge.X
                    };
                }

                int roads = tileStart("PavedRoads");
                int ends = tileStart("PavedRoadEnds");
                int x = bridge.X;
                int y = bridge.Y;
                int right = bridge.Right;
                int bottom = bridge.Bottom;
                if (eastWest)
                {
                    bool longEnd = BridgeRoadFits(new Rectangle(right, y - 2, 6, 6), true) && random.Pick(0, 1) != 0;
                    StampBridgeApproach(longEnd ? roads + 10 : ends, new Point2D(right, y));
                    longEnd = BridgeRoadFits(new Rectangle(x - 6, y - 2, 6, 6), true) && random.Pick(0, 1) != 0;
                    StampBridgeApproach(longEnd ? roads + 9 : ends + 2, new Point2D(x - (longEnd ? 4 : 1), y));
                    if (!PlaceBridgeHut(new Rectangle(x, y - 1, 2, 5)))
                        PlaceBridgeHut(new Rectangle(x - 1, y - 2, 3, 7));
                    if (!PlaceBridgeHut(new Rectangle(right - 2, y - 1, 2, 5)))
                        PlaceBridgeHut(new Rectangle(right - 2, y - 2, 3, 7));
                }
                else
                {
                    bool longEnd = BridgeRoadFits(new Rectangle(x - 2, y - 6, 7, 6), true) && random.Pick(0, 1) != 0;
                    StampBridgeApproach(longEnd ? roads + 13 : ends + 1, new Point2D(x, y - (longEnd ? 4 : 1)));
                    longEnd = BridgeRoadFits(new Rectangle(x - 2, bottom, 7, 6), true) && random.Pick(0, 1) != 0;
                    StampBridgeApproach(longEnd ? roads + 12 : ends + 3, new Point2D(x, bottom));
                    if (!PlaceBridgeHut(new Rectangle(x - 1, y, 5, 2)))
                        PlaceBridgeHut(new Rectangle(x - 2, y - 1, 7, 3));
                    if (!PlaceBridgeHut(new Rectangle(x - 1, bottom - 2, 5, 2)))
                        PlaceBridgeHut(new Rectangle(x - 2, bottom - 2, 7, 3));
                }
            }

            private void StampBridgeApproach(int index, Point2D origin)
            {
                ITileImage image = currentTile(index);
                for (int i = 0; i < image.SubTileCount; i++)
                {
                    if (image.GetSubTile(i)?.TmpImage != null &&
                        map.GetTile(origin + new Point2D(i % image.Width, i / image.Width)) is MapTile cell)
                    {
                        cell.ChangeTileIndex(index, (byte)i);
                        owners[cell.CoordsToPoint()] = -1;
                    }
                }
            }

            private bool PlaceBridgeHut(Rectangle area)
            {
                var search = new Rectangle(area.X, area.Y, area.Width + 1, area.Height + 1);
                foreach (Point2D position in RampArea(search))
                {
                    MapTile cell = map.GetTile(position);
                    if (cell == null || !IsCliffPassClear(cell) || cell.Overlay != null ||
                        cell.Structures.Count > 0 || cell.TerrainObject != null)
                    {
                        continue;
                    }

                    BuildingType type = source.Rules.BuildingTypes.Find(building => building.ININame == "CABHUT") ??
                        throw new InvalidOperationException("The loaded rules lack the stock bridge repair hut.");
                    House neutral = source.FindHouse("Neutral") ??
                        throw new InvalidOperationException("The map lacks the stock Neutral house.");
                    map.PlaceBuilding(new Structure(type) { Position = position, Owner = neutral });
                    return true;
                }

                return false;
            }

            private bool InNativeLocalRadar(Point2D point)
            {
                MapTile cell = map.GetTile(point);
                if (cell == null)
                    return false;

                Rectangle local = map.LocalSize;
                int width = map.Size.X;
                int diagonal = point.X + point.Y;
                int height = cell.Level;
                if (diagonal < width + 2 * local.Top + 4 + height &&
                    !IsCliffPassClear(cell) && currentTile(cell.TileIndex).GetSubTile(cell.SubTileIndex).TmpImage.RampType != RampType.None)
                {
                    height++;
                }

                return diagonal > width + 2 * local.Top + height &&
                    diagonal <= width + 2 * (local.Bottom + 1) + height &&
                    point.X - point.Y < 2 * local.Right - width &&
                    point.Y - point.X < width - 2 * local.Left;
            }

            private bool OwnsRampPatch(Point2D center, int region)
            {
                int count = 0;
                foreach (Point2D point in RampArea(new Rectangle(center.X - 2, center.Y - 2, 5, 5)))
                {
                    if (owners.TryGetValue(point, out int owner) && owner == region && ++count == 6)
                        return true;
                }

                return false;
            }

            private bool ConnectRamp(Point2D point, int upper, int lower, bool allowFallback)
            {
                if (!OwnsRampPatch(point, upper))
                    return false;

                int mask = 0;
                for (int i = 0; i < adjacent.Length; i++)
                {
                    Point2D direction = adjacent[i];
                    if (OwnsRampPatch(point + new Point2D(5 * direction.X, 5 * direction.Y), upper))
                        mask |= 1 << i;
                }

                if (mask == 255)
                    return false;

                bool Has(int direction) => (mask & direction) != 0;
                bool Contains(int directions) => (mask & directions) == directions;
                bool IsClear(int directions) => (mask & directions) == 0;

                if (Contains(N | E) && IsClear(S | SW | W) &&
                    BuildCornerRampSW(point + new Point2D(-1, Has(NW) ? -4 : -5),
                        point + new Point2D(Has(SE) ? 4 : 5, 1), upper, lower))
                {
                    return true;
                }

                if (Contains(E | S) && IsClear(NE | SW) &&
                    BuildCornerRampNW(point + new Point2D(5, -1), point + new Point2D(-1, 5), upper, lower))
                {
                    return true;
                }

                if (Contains(S | W) && IsClear(N | NE | E) &&
                    BuildCornerRampNE(point + new Point2D(1, Has(SE) ? 4 : 5),
                        point + new Point2D(Has(NW) ? -6 : -5, -1), upper, lower))
                {
                    return true;
                }

                // This mask also calls the native NE builder.
                if (Contains(N | W) && IsClear(E | SE | S) &&
                    BuildCornerRampNE(point + new Point2D(Has(SW) ? -4 : -5, 1),
                        point + new Point2D(1, Has(NE) ? -4 : -5), upper, lower))
                {
                    return true;
                }

                int yShift = (Has(N) ? 1 : 0) - (Has(S) ? 1 : 0);
                if (IsClear(SW | W | NW) && Has(N | S) &&
                    BuildWestRamp(point + new Point2D(-1 + random.Pick(0, 1), -4 + yShift),
                        point + new Point2D(-1 + random.Pick(0, 1), 4 + yShift), upper, lower))
                {
                    return true;
                }

                if (IsClear(NE | E | SE) && Has(N | S) &&
                    BuildEastRamp(point + new Point2D(random.Pick(0, 1), 4 + yShift),
                        point + new Point2D(random.Pick(0, 1), -4 + yShift), upper, lower))
                {
                    return true;
                }

                int xShift = (Has(W) ? 1 : 0) - (Has(E) ? 1 : 0);
                if (IsClear(N | NE | NW) && Has(E | W) &&
                    BuildNorthRamp(point + new Point2D(4 + xShift, -1 + random.Pick(0, 1)),
                        point + new Point2D(-4 + xShift, -1 + random.Pick(0, 1)), upper, lower))
                {
                    return true;
                }

                if (IsClear(SE | S | SW) && Has(E | W) &&
                    BuildSouthRamp(point + new Point2D(-4 + xShift, random.Pick(0, 1)),
                        point + new Point2D(4 + xShift, random.Pick(0, 1)), upper, lower))
                {
                    return true;
                }

                return allowFallback && (
                    IsClear(S) && BuildSouthRamp(point + new Point2D(-4, 1), point + new Point2D(4, 1), upper, lower) ||
                    IsClear(E) && BuildEastRamp(point + new Point2D(1, 4), point + new Point2D(1, -4), upper, lower) ||
                    IsClear(S) && BuildNorthRamp(point + new Point2D(4, -1), point + new Point2D(-4, -1), upper, lower) ||
                    IsClear(W) && BuildWestRamp(point + new Point2D(-1, -4), point + new Point2D(-1, 4), upper, lower));
            }

            private static IEnumerable<Point2D> RampArea(Rectangle rect)
            {
                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    for (int x = rect.Left; x < rect.Right; x++)
                        yield return new Point2D(x, y);
                }
            }

            private bool IsRampAreaClear(Rectangle rect, int upperRegion, int lowerRegion)
            {
                if (!InNativeLocalRadar(new Point2D(rect.Left, rect.Top)) ||
                    !InNativeLocalRadar(new Point2D(rect.Right - 1, rect.Top)) ||
                    !InNativeLocalRadar(new Point2D(rect.Left, rect.Bottom - 1)) ||
                    !InNativeLocalRadar(new Point2D(rect.Right - 1, rect.Bottom - 1)))
                {
                    return false;
                }

                return RampArea(rect).All(point => owners.TryGetValue(point, out int owner) &&
                    (owner == upperRegion || owner == lowerRegion) && map.GetTileOrFail(point).TileIndex == clearTile);
            }

            private void FlattenRampArea(Rectangle rect, int region)
            {
                foreach (Point2D point in RampArea(rect))
                {
                    if (map.GetTile(point) is MapTile cell)
                    {
                        cell.Level = (byte)regions[region].Height;
                        owners[point] = region;
                    }
                }
            }

            private void PlaceSlopePiece(int variantOffset, Point2D origin, int region, int height)
            {
                ITileImage piece = currentTile(slopePiecesStart + variantOffset);
                for (int index = 0; index < piece.SubTileCount; index++)
                {
                    if (piece.GetSubTileCoordOffset(index) is not Point2D offset)
                        continue;

                    Point2D point = origin + offset;
                    if (map.GetTile(point) is MapTile cell)
                    {
                        cell.ChangeTileIndex(piece.TileID, (byte)index);
                        cell.Level = (byte)(height + piece.GetSubTile(index).TmpImage.Height - CliffHeight);
                        owners[point] = region;
                    }
                }
            }

            private void PlaceRampTile(Point2D point, int rampType, int height, int? region = null)
            {
                if (map.GetTile(point) is not MapTile cell)
                    return;

                cell.ChangeTileIndex(rampStart + rampType - 1, 0);
                cell.Level = (byte)height;
                if (region.HasValue)
                    owners[point] = region.Value;
            }

            private bool BuildSouthRamp(Point2D first, Point2D second, int upperRegion, int lowerRegion)
            {
                int dx = second.X - first.X;
                int dy = second.Y - first.Y;
                int drift = Math.Abs(dy);
                var rect = new Rectangle(first.X - 5, Math.Min(first.Y, second.Y) - 3, dx + 9, drift + 11);
                if (!IsRampAreaClear(rect, upperRegion, lowerRegion) || drift > dx - 5)
                    return false;

                int half = (dx + 1) / 2 + 3;
                FlattenRampArea(new Rectangle(rect.X, first.Y + 1, half + 2, rect.Height - first.Y + rect.Y + 1), lowerRegion);
                FlattenRampArea(new Rectangle(second.X - (dx + 1) / 2, second.Y + 1, half + 2, rect.Height - second.Y + rect.Y + 1), lowerRegion);
                FlattenRampArea(new Rectangle(rect.X + 4, rect.Y + 2, half - 2, first.Y - rect.Y - 1), upperRegion);
                FlattenRampArea(new Rectangle(second.X - half + 2, rect.Y + 2, half - 1, second.Y - rect.Y - 1), upperRegion);

                int height = regions[upperRegion].Height;
                PlaceSlopePiece(0, first, lowerRegion, height);
                PlaceSlopePiece(1, second - new Point2D(2, 0), lowerRegion, height);

                var origin = new Point2D(first.X + 3, first.Y);
                var tail = new Point2D(0, 0);
                if (dy != 0)
                {
                    int jogAtStart = random.Pick(0, 1);
                    bool down = dy > 0;
                    var bias = new Point2D(0, down ? 0 : -1);
                    if (jogAtStart == 1)
                        tail = new Point2D(drift, dy);
                    else
                        bias.X = dx - drift - 5;

                    ReadOnlySpan<int> jogRamps = stackalloc int[] { 11, 15, 15, 15, 7, 12, 16, 16, 16, 8 };
                    for (int offset = 0; offset < drift; offset++)
                    {
                        for (int row = 0; row < 5; row++)
                        {
                            var point = origin + new Point2D(offset, row + (down ? offset : -offset)) + bias;
                            PlaceRampTile(point, jogRamps[(down ? 5 : 0) + row], height - Math.Min(row + 1, 4));
                        }
                    }
                }

                for (int x = 0; x < dx - drift - 5; x++)
                {
                    for (int row = 0; row < 4; row++)
                        PlaceRampTile(origin + new Point2D(x, row) + tail, 4, height - row - 1, lowerRegion);
                }

                return true;
            }

            private bool BuildEastRamp(Point2D first, Point2D second, int upperRegion, int lowerRegion)
            {
                int dx = second.X - first.X;
                int dy = first.Y - second.Y;
                int drift = Math.Abs(dx);
                var rect = new Rectangle(Math.Min(first.X, second.X) - 3, second.Y - 4, drift + 11, dy + 9);
                if (!IsRampAreaClear(rect, upperRegion, lowerRegion) || drift > dy - 5)
                    return false;

                int center = (dy + 1) / 2;
                int half = center + 2;
                FlattenRampArea(new Rectangle(first.X + 1, first.Y - center, 7, half + 3), lowerRegion);
                FlattenRampArea(new Rectangle(second.X + 1, rect.Y, 7, half + 2), lowerRegion);
                FlattenRampArea(new Rectangle(rect.X + 2, first.Y - center, first.X - rect.X - 1, center + 1), upperRegion);
                FlattenRampArea(new Rectangle(rect.X + 2, second.Y, second.X - rect.X - 1, center), upperRegion);

                int height = regions[upperRegion].Height;
                PlaceSlopePiece(2, first - new Point2D(0, 2), lowerRegion, height);
                PlaceSlopePiece(3, second, lowerRegion, height);

                var origin = new Point2D(first.X, first.Y - 3);
                var tail = new Point2D(0, 0);
                if (dx != 0)
                {
                    int jogAtStart = random.Pick(0, 1);
                    bool right = dx > 0;
                    var bias = new Point2D(right ? 0 : -1, 0);
                    if (jogAtStart == 1)
                        tail = new Point2D(dx, -drift);
                    else
                        bias.Y = -(short)(dy - drift - 5);

                    ReadOnlySpan<int> jogRamps = stackalloc int[] { 10, 14, 14, 14, 6, 11, 15, 15, 15, 7 };
                    for (int offset = 0; offset < drift; offset++)
                    {
                        for (int row = 0; row < 5; row++)
                        {
                            var point = origin + new Point2D(row + (right ? offset : -offset), -offset) + bias;
                            PlaceRampTile(point, jogRamps[(right ? 5 : 0) + row], height - Math.Min(row + 1, 4));
                        }
                    }
                }

                for (int y = 0; y < dy - drift - 5; y++)
                {
                    for (int row = 0; row < 4; row++)
                        PlaceRampTile(origin + new Point2D(row, -y) + tail, 3, height - row - 1, lowerRegion);
                }

                return true;
            }

            private bool BuildNorthRamp(Point2D first, Point2D second, int upperRegion, int lowerRegion)
            {
                int dx = first.X - second.X;
                int dy = second.Y - first.Y;
                int drift = Math.Abs(dy);
                var rect = new Rectangle(second.X - 4, Math.Min(first.Y, second.Y) - 7, dx + 8, drift + 11);
                if (!IsRampAreaClear(rect, upperRegion, lowerRegion) || drift > dx - 5)
                    return false;

                int half = (dx + 1) / 2 + 3;
                FlattenRampArea(new Rectangle(rect.X, rect.Y, half + 2, second.Y - rect.Y), lowerRegion);
                FlattenRampArea(new Rectangle(first.X - half + 2, rect.Y, half + 3, first.Y - rect.Y), lowerRegion);
                FlattenRampArea(new Rectangle(second.X, second.Y, half - 2, rect.Height + rect.Y - second.Y - 2), upperRegion);
                FlattenRampArea(new Rectangle(first.X - half + 2, first.Y, half - 1, rect.Height + rect.Y - first.Y - 2), upperRegion);

                int height = regions[upperRegion].Height;
                PlaceSlopePiece(4, second - new Point2D(0, 3), lowerRegion, height);
                PlaceSlopePiece(5, first - new Point2D(2, 3), lowerRegion, height);
                PlaceSlopePiece(6, first - new Point2D(1, 1), lowerRegion, height);

                var origin = new Point2D(second.X + 3, second.Y);
                var tail = new Point2D(0, 0);
                if (dy != 0)
                {
                    int jogAtStart = random.Pick(0, 1);
                    bool down = dy > 0;
                    var bias = new Point2D(0, down ? 0 : 1);
                    if (jogAtStart == 1)
                        tail = new Point2D(drift, -dy);
                    else
                        bias.X = dx - drift - 5;

                    ReadOnlySpan<int> jogRamps = stackalloc int[] { 10, 14, 14, 14, 6, 9, 13, 13, 13, 5 };
                    for (int offset = 0; offset < drift; offset++)
                    {
                        for (int row = 0; row < 5; row++)
                        {
                            var point = origin + new Point2D(offset, -row + (down ? -offset : offset)) + bias;
                            PlaceRampTile(point, jogRamps[(down ? 5 : 0) + row], height - Math.Min(row + 1, 4));
                        }
                    }
                }

                for (int x = 0; x < dx - drift - 5; x++)
                {
                    for (int row = 0; row < 4; row++)
                        PlaceRampTile(origin + new Point2D(x, -row) + tail, 2, height - row - 1, lowerRegion);
                }

                return true;
            }

            private bool BuildWestRamp(Point2D first, Point2D second, int upperRegion, int lowerRegion)
            {
                int dx = second.X - first.X;
                int dy = second.Y - first.Y;
                int drift = Math.Abs(dx);
                var rect = new Rectangle(Math.Min(first.X, second.X) - 7, first.Y - 4, drift + 11, dy + 9);
                if (!IsRampAreaClear(rect, upperRegion, lowerRegion) || drift > dy - 5)
                    return false;

                int half = (dy + 1) / 2 + 2;
                FlattenRampArea(new Rectangle(rect.X, rect.Y, first.X - rect.X, half + 2), lowerRegion);
                FlattenRampArea(new Rectangle(rect.X, second.Y - half + 3, second.X - rect.X, half + 2), lowerRegion);
                FlattenRampArea(new Rectangle(first.X, first.Y, rect.X + rect.Width - first.X - 1, half - 1), upperRegion);
                FlattenRampArea(new Rectangle(second.X, second.Y - half + 3, rect.X + rect.Width - second.X - 1, half - 2), upperRegion);

                int height = regions[upperRegion].Height;
                PlaceSlopePiece(7, first - new Point2D(3, 0), lowerRegion, height);
                PlaceSlopePiece(8, second - new Point2D(3, 2), lowerRegion, height);
                PlaceSlopePiece(9, second - new Point2D(1, 1), lowerRegion, height);

                var origin = new Point2D(first.X, first.Y + 3);
                var tail = new Point2D(0, 0);
                if (dx != 0)
                {
                    int jogAtStart = random.Pick(0, 1);
                    bool right = dx > 0;
                    var bias = new Point2D(right ? 1 : 0, 0);
                    if (jogAtStart == 1)
                        tail = new Point2D(dx, drift);
                    else
                        bias.Y = dy - drift - 5;

                    ReadOnlySpan<int> jogRamps = stackalloc int[] { 9, 13, 13, 13, 5, 12, 16, 16, 16, 8 };
                    for (int offset = 0; offset < drift; offset++)
                    {
                        for (int row = 0; row < 5; row++)
                        {
                            var point = origin + new Point2D(-row + (right ? offset : -offset), offset) + bias;
                            PlaceRampTile(point, jogRamps[(right ? 5 : 0) + row], height - Math.Min(row + 1, 4));
                        }
                    }
                }

                for (int y = 0; y < dy - drift - 5; y++)
                {
                    for (int row = 0; row < 4; row++)
                        PlaceRampTile(origin + new Point2D(-row, y) + tail, 1, height - row - 1, lowerRegion);
                }

                return true;
            }

            private bool BuildCornerRampNE(Point2D first, Point2D second, int upperRegion, int lowerRegion)
            {
                int dx = first.X - second.X;
                int dy = first.Y - second.Y;

                if (dx < 3 || dy < 3)
                    return false;

                var bounds = new Rectangle(second.X - 2, second.Y - 7, dx + 10, dy + 10);
                if (!IsRampAreaClear(bounds, upperRegion, lowerRegion))
                    return false;

                FlattenRampArea(new Rectangle(bounds.X, bounds.Y, bounds.Width, 7), lowerRegion);
                FlattenRampArea(new Rectangle(first.X + 1, bounds.Y, 7, bounds.Height), lowerRegion);
                FlattenRampArea(new Rectangle(second.X, second.Y, 1, 2), upperRegion);
                FlattenRampArea(new Rectangle(first.X - 1, first.Y, 2, 1), upperRegion);
                FlattenRampArea(new Rectangle(second.X + 1, second.Y + 1, dx - 1, dy - 1), upperRegion);

                int height = regions[upperRegion].Height;
                PlaceSlopePiece(2, first - new Point2D(0, 2), lowerRegion, height);
                PlaceSlopePiece(4, second - new Point2D(0, 3), lowerRegion, height);

                for (int step = 0; step < 4; step++)
                    PlaceRampTile(new Point2D(first.X + step, second.Y - step), 6, height - step - 1);

                for (int column = 0; column < 4; column++)
                {
                    for (int row = 0; row < dy + column - 3; row++)
                        PlaceRampTile(new Point2D(first.X + column, first.Y - row - 3), 3, height - column - 1);
                }

                for (int row = 0; row < 4; row++)
                {
                    for (int column = 0; column < dx + row - 3; column++)
                        PlaceRampTile(new Point2D(second.X + column + 3, second.Y - row), 2, height - row - 1);
                }

                return true;
            }

            private bool BuildCornerRampNW(Point2D first, Point2D second, int upperRegion, int lowerRegion)
            {
                // The native parameter order for this corner is cell2, cell1.
                int dx = first.X - second.X;
                int dy = second.Y - first.Y;

                if (dx < 3 || dy < 3)
                    return false;

                var bounds = new Rectangle(second.X - 7, first.Y - 7, dx + 10, dy + 10);
                if (!IsRampAreaClear(bounds, upperRegion, lowerRegion))
                    return false;

                FlattenRampArea(new Rectangle(bounds.X, bounds.Y, bounds.Width, 7), lowerRegion);
                FlattenRampArea(new Rectangle(bounds.X, bounds.Y, 7, bounds.Height), lowerRegion);
                FlattenRampArea(new Rectangle(first.X, first.Y, 1, 2), upperRegion);
                FlattenRampArea(new Rectangle(second.X, second.Y, 2, 1), upperRegion);
                FlattenRampArea(new Rectangle(second.X + 1, first.Y + 1, dx - 1, dy - 1), upperRegion);

                int height = regions[upperRegion].Height;
                PlaceSlopePiece(5, first - new Point2D(2, 3), lowerRegion, height);
                PlaceSlopePiece(6, first - new Point2D(1, 1), lowerRegion, height);
                PlaceSlopePiece(8, second - new Point2D(3, 2), lowerRegion, height);
                PlaceSlopePiece(9, second - new Point2D(1, 1), lowerRegion, height);

                for (int step = 0; step < 4; step++)
                    PlaceRampTile(new Point2D(second.X - step, first.Y - step), 5, height - step - 1);

                for (int row = 0; row < 4; row++)
                {
                    for (int column = 0; column < dx + row - 3; column++)
                        PlaceRampTile(first - new Point2D(column + 3, row), 2, height - row - 1);
                }

                for (int column = 0; column < 4; column++)
                {
                    for (int row = 0; row < dy + column - 3; row++)
                        PlaceRampTile(second - new Point2D(column, row + 3), 1, height - column - 1);
                }

                return true;
            }

            private bool BuildCornerRampSW(Point2D first, Point2D second, int upperRegion, int lowerRegion)
            {
                int dx = second.X - first.X;
                int dy = second.Y - first.Y;

                if (dx < 3 || dy < 3)
                    return false;

                var bounds = new Rectangle(first.X - 7, first.Y - 4, dx + 10, dy + 10);
                if (!IsRampAreaClear(bounds, upperRegion, lowerRegion))
                    return false;

                FlattenRampArea(new Rectangle(bounds.X, bounds.Y, 7, bounds.Height), lowerRegion);
                FlattenRampArea(new Rectangle(bounds.X, second.Y + 1, bounds.Width, 7), lowerRegion);
                FlattenRampArea(new Rectangle(first.X, first.Y, 2, 1), upperRegion);
                FlattenRampArea(new Rectangle(second.X, second.Y - 1, 1, 2), upperRegion);
                FlattenRampArea(new Rectangle(first.X + 1, first.Y + 1, dx - 1, dy - 1), upperRegion);

                int height = regions[upperRegion].Height;
                PlaceSlopePiece(7, first - new Point2D(3, 0), lowerRegion, height);
                PlaceSlopePiece(1, second - new Point2D(2, 0), lowerRegion, height);

                for (int step = 0; step < 4; step++)
                    PlaceRampTile(new Point2D(first.X - step, second.Y + step), 8, height - step - 1);

                for (int column = 0; column < 4; column++)
                {
                    for (int row = 0; row < dy + column - 3; row++)
                        PlaceRampTile(new Point2D(first.X - column, first.Y + row + 3), 1, height - column - 1);
                }

                for (int row = 0; row < 4; row++)
                {
                    for (int column = 0; column < dx + row - 3; column++)
                        PlaceRampTile(new Point2D(second.X - column - 3, second.Y + row), 4, height - row - 1);
                }

                return true;
            }
        }

        private sealed class LiquidWater
        {
            private const int WaterCount = 14;
            private const int ShoreCount = 42;
            private const int SwampCount = 9;
            private static readonly float[] waterRatios = { 0.40f, 0.35f, 0.35f, 0.10f, 0.30f };
            private static readonly int[] shoreGroups =
            {
                0, 0, 0, 1, 2, 3, 4, 4, 5, 5, 5, 6, 7, 8, 9, 9, 10, 10, 10, 11, 12,
                13, 14, 14, 15, 15, 15, 16, 17, 18, 19, 19, 20, 20, 21, 21, 22, 22, 23, 23, 24, 25
            };
            private static readonly int[] shoreFacings =
            {
                4, 4, 4, 4, 4, 4, 3, 3, 2, 2, 2, 2, 2, 2, 1, 1, 0, 0, 0, 0, 0,
                0, 7, 7, 6, 6, 6, 6, 6, 6, 5, 5, 3, 3, 1, 1, 7, 7, 5, 5, 4, 4
            };
            private static readonly Point2D[] shoreOffsets =
            {
                new(0, -1), new(0, -1), new(0, -1), new(0, -1), new(0, -2), new(-1, -2), new(-1, -1), new(-1, -1),
                new(-1, 0), new(-1, 0), new(-1, 0), new(-1, 0), new(-2, -1), new(-2, 0), new(-1, 0), new(-1, 0),
                new(0, 0), new(0, 0), new(0, 0), new(0, 0), new(0, 0), new(-1, 0), new(0, 0), new(0, 0),
                new(0, 0), new(0, 0), new(0, 0), new(0, 0), new(0, -1), new(0, 0), new(0, -1), new(0, -1),
                new(-1, -1), new(-1, -1), new(-1, 0), new(-1, 0), new(0, 0), new(0, 0), new(0, -1), new(0, -1),
                new(0, 0), new(0, 0)
            };

            private readonly Map map;
            private readonly GeneratorRandom random;
            private readonly EnvironmentType biome;
            private readonly int waterAmount;
            private readonly int budget;
            private readonly bool waterfalls;
            private readonly int clearTile;
            private readonly int waterStart;
            private readonly int shoreStart;
            private readonly int swampStart;
            private readonly int swampLat;
            private readonly int[] waterfallStarts;
            private readonly Func<int, ITileImage> tileImage;
            private readonly IReadOnlyList<TerrainType> terrainTypes;
            private readonly MapTile[] cells;
            private readonly MapTile[] rowOrder;
            private readonly int stride;
            private readonly int[] regions;
            private readonly int[] waterMasks;
            private readonly HashSet<Point2D> expandable = new HashSet<Point2D>();
            private byte groundLevel;
            private int workingRegion;
            private int seededWater;

            public LiquidWater(Map map, GeneratorRandom random, Settings settings, bool waterfalls, int clearTile,
                byte groundLevel, Func<string, int> tileStart, Func<int, ITileImage> tileImage, IReadOnlyList<TerrainType> terrainTypes)
            {
                this.map = map;
                this.random = random;
                this.waterfalls = waterfalls;
                this.clearTile = clearTile;
                this.groundLevel = groundLevel;
                this.tileImage = tileImage;
                this.terrainTypes = terrainTypes;
                biome = settings.Environment;
                waterAmount = settings.Water;
                waterStart = tileStart("WaterSet");
                shoreStart = tileStart("ShorePieces");
                if (waterStart < 0 || shoreStart < 0)
                    throw new InvalidOperationException("The loaded theater lacks the stock water or shoreline tiles.");

                swampStart = tileStart("SwampTile");
                swampLat = tileStart("WaterToSwampLat");
                waterfallStarts = new[] { tileStart("WaterfallNorth"), tileStart("WaterfallEast"), tileStart("WaterfallSouth"), tileStart("WaterfallWest") };
                budget = (int)((double)waterRatios[(int)biome] * map.LocalSize.Width * map.LocalSize.Height * waterAmount * 0.02 + 100);
                cells = InIsometricOrder(map).ToArray();
                rowOrder = cells.OrderBy(cell => cell.Y).ThenBy(cell => cell.X).ToArray();
                stride = map.Size.X + map.Size.Y + 1;
                regions = new int[stride * stride];
                waterMasks = new int[regions.Length];
            }

            public void Seed()
            {
                workingRegion++;
                if (biome != EnvironmentType.Desert && waterAmount > 20)
                {
                    for (int attempt = 0; attempt < 10; attempt++)
                    {
                        if (SeedRiver())
                        {
                            workingRegion++;
                            break;
                        }
                    }
                }

                for (int attempt = 0; attempt < 10; attempt++)
                {
                    if (SeedLake())
                        break;
                }
            }

            public IReadOnlySet<Point2D> Expandable => expandable;

            public bool IsWaterFeature(MapTile cell) => IsWaterTile(cell) || IsSwamp(cell);

            public bool IsWaterTile(MapTile cell) =>
                IsWater(cell) || InSet(cell.TileIndex, shoreStart, ShoreCount) || IsWaterfall(cell.TileIndex);

            private int Slot(Point2D point) => point.X + point.Y * stride;

            private static bool InSet(int tile, int start, int count) => start >= 0 && (uint)(tile - start) < count;

            private bool IsClear(MapTile cell) => cell.TileIndex == clearTile || cell.TileIndex == ushort.MaxValue;
            private bool IsWater(MapTile cell) => cell != null && InSet(cell.TileIndex, waterStart, WaterCount);
            private bool IsSwamp(MapTile cell) => InSet(cell.TileIndex, swampStart, SwampCount) || InSet(cell.TileIndex, swampLat, 16);
            private bool IsWaterfall(int tile) => waterfallStarts.Any(start => InSet(tile, start, 4));

            private void PaintWater(MapTile cell)
            {
                int variant = random.Pick(0, 5);
                cell.ChangeTileIndex(waterStart + variant, (byte)random.Pick(0, 3));
                regions[Slot(cell.CoordsToPoint())] = workingRegion;
            }

            private bool SeedRiver(Point2D? origin = null, double angle = 0.0)
            {
                Point2D seed;
                if (origin.HasValue)
                {
                    seed = origin.Value;
                }
                else
                {
                    int direction = random.Pick(0, 3);
                    int x = random.Pick(0, map.Size.X - 1);
                    int y = random.Pick(0, map.Size.Y - 1);
                    var corner = new Point2D(1, map.Size.X);
                    var farCorner = new Point2D(map.Size.X + map.Size.Y - 1, map.Size.Y);
                    seed = direction switch
                    {
                        0 => corner + new Point2D(x, -x),
                        1 => farCorner - new Point2D(y, y),
                        2 => farCorner - new Point2D(x, -x),
                        _ => corner + new Point2D(y, y)
                    };
                    double heading = Radians(315) - direction * (Math.PI / 2);

                    angle = random.NativeTruncatedNormal(heading, Radians(30), heading - Math.PI / 4, heading + Math.PI / 4);
                }

                if (map.GetTile(seed) == null)
                    return false;

                double width = random.Pick(1, (int)Math.Max(1.0, waterAmount * 0.07));
                int half = (int)width / 2;
                double minWidth = width - half;
                double maxWidth = width + half;
                int length = random.Pick(35, 125);
                double headingNow = angle;
                double xCenter = seed.X + 0.5;
                double yCenter = seed.Y + 0.5;
                bool success = true;
                bool branched = origin.HasValue;
                bool hasWaterfall = false;
                double stop = 1.0;
                int steps = 0;

                while (map.GetTile((int)xCenter, (int)yCenter) != null && success)
                {
                    double stepX = Math.Sin(headingNow);
                    double stepY = Math.Cos(headingNow);
                    int span = (int)(width + 0.5);
                    double offsetX = (span - 1) * stepX * 0.5;
                    double offsetY = (span - 1) * stepY * 0.5;
                    double x = xCenter - offsetX;
                    double y = yCenter - offsetY;
                    bool sameX = true;
                    bool sameY = true;
                    for (int i = 0; i < span; i++)
                    {
                        var point = new Point2D((int)x, (int)y);
                        if (map.GetTile(point) is MapTile cell)
                        {
                            int region = regions[Slot(point)];
                            if (region == workingRegion || (region == 0 && IsClear(cell)))
                                PaintWater(cell);
                            else
                                success = false;
                        }

                        sameX &= (int)x == (int)(x + stepX);
                        sameY &= (int)y == (int)(y + stepY);
                        x += stepX;
                        y += stepY;
                    }

                    bool xRun = sameX && Math.Abs(stepY) > Math.Abs(stepX);
                    bool yRun = sameY && Math.Abs(stepY) <= Math.Abs(stepX);
                    if (waterfalls && !branched && !hasWaterfall && (xRun || yRun) && steps > length &&
                        Math.Abs((int)(headingNow - angle)) < Math.PI / 4)
                    {
                        int direction = xRun ? (stepY > 0 ? 2 : 6) : (stepX > 0 ? 0 : 4);
                        var first = new Point2D((int)(xCenter - offsetX), (int)(yCenter - offsetY));
                        var last = new Point2D((int)(x - stepX), (int)(y - stepY));
                        if (TryWaterfall(first, last, direction))
                        {
                            Point2D jump = adjacent[direction];
                            xCenter += jump.X * 12;
                            yCenter += jump.Y * 12;
                            workingRegion++;
                            hasWaterfall = true;
                        }
                    }

                    xCenter += stepY;
                    yCenter -= stepX;
                    if (random.Fraction() < 0.01 && success && !branched && !hasWaterfall)
                    {
                        branched = true;
                        double branch = random.NativeTruncatedNormal(headingNow + Math.PI / 2, Radians(30),
                            headingNow + Radians(30), headingNow + Radians(150));
                        success = SeedRiver(new Point2D((int)x, (int)y), branch);
                    }

                    if (steps > 5)
                    {
                        headingNow += random.NativeTruncatedNormal(0, Radians(18),
                            angle - Math.PI / 2 - headingNow, angle + Math.PI / 2 - headingNow);
                    }

                    if (half > 0)
                        width += random.NativeTruncatedNormal(0, 0.5, minWidth - width, maxWidth - width);

                    stop = random.Fraction();
                    steps++;
                    if (stop < 0.005)
                        break;
                }

                success &= steps >= 40;
                var end = new Point2D((int)xCenter, (int)yCenter);
                if (success && stop < 0.005 && map.GetTile(end) != null)
                    success = SeedLake(end);

                if (!origin.HasValue && success)
                    success = BuildShores();

                if (!origin.HasValue && success)
                {
                    if (!hasWaterfall && groundLevel == 4 && random.Fraction() < 0.7)
                    {
                        success = GrowWaterRegion(0.01f, null, seed, true) && GrowRegion(6);
                        if (success)
                        {
                            foreach (MapTile cell in cells)
                            {
                                if (regions[Slot(cell.CoordsToPoint())] != workingRegion)
                                    cell.Level += 4;
                            }

                            groundLevel += 4;
                        }
                    }
                    else
                    {
                        success = GrowRegion(2, raiseLand: hasWaterfall);
                    }
                }

                if (success)
                {
                    seededWater += steps;
                    return true;
                }

                ClearRegion(workingRegion);
                if (hasWaterfall)
                    ClearRegion(workingRegion - 1);

                return false;
            }

            private bool SeedLake(Point2D? origin = null)
            {
                int remaining = budget - seededWater;
                if (remaining <= 75)
                    return false;

                bool[] interior = origin.HasValue ? null : LakeInterior();
                Point2D seed = origin ?? default;
                if (!origin.HasValue)
                {
                    for (int attempt = 1; ; attempt++)
                    {
                        int x = random.Pick(0, map.Size.X - 1);
                        int y = random.Pick(0, map.Size.Y - 1);
                        seed = new Point2D(1 + x + y, map.Size.X - x + y);
                        if (attempt == 200)
                            return false;

                        int index = Slot(seed);
                        if (interior[index] && regions[index] == 0 && IsClear(map.GetTileOrFail(seed)))
                            break;
                    }
                }

                int target = (int)random.NativeTruncatedNormal(remaining / 3, remaining / 6, 75.0, remaining);
                var queue = new NativePriorityQueue<Point2D>();
                var seen = new HashSet<Point2D> { seed };
                var lake = new List<Point2D>();
                queue.Add(seed, 0);
                int painted = 0;
                bool success = true;
                while (queue.TryTake(out Point2D point) && success && painted < target)
                {
                    PaintWater(map.GetTileOrFail(point));
                    lake.Add(point);
                    for (int direction = 0; direction < adjacent.Length; direction += 2)
                    {
                        Point2D next = point + adjacent[direction];
                        MapTile neighbor = map.GetTile(next);
                        if (neighbor == null)
                            continue;

                        int slot = Slot(next);
                        int region = regions[slot];
                        if (region != 0)
                        {
                            success &= region == workingRegion;
                        }
                        else if ((interior == null || interior[slot]) && IsClear(neighbor) && seen.Add(next))
                        {
                            queue.Add(next, SpreadScore(seed, next, painted));
                        }
                    }

                    painted++;
                }

                // The spread loop fetches one extra node before checking its quota.
                while (queue.TryTake(out Point2D point) && success)
                {
                    int slot = Slot(point);
                    MapTile cell = map.GetTileOrFail(point);
                    if (regions[slot] == 0 && (interior == null || interior[slot]) && IsClear(cell))
                        PaintWater(cell);
                    else
                        success = false;

                    painted++;
                }

                success &= painted > 75 && painted > target / 4;
                if (!origin.HasValue && success)
                {
                    success = BuildShores() && GrowRegion(1);
                    if (success && biome == EnvironmentType.Mutated)
                    {
                        int swamps = random.Pick(0, 2);
                        for (int i = 0; i < swamps; i++)
                            SeedSwamp(lake, painted);
                    }
                }

                if (success)
                    seededWater += painted;
                else
                    ClearRegion(workingRegion);

                return success;
            }

            private bool[] LakeInterior()
            {
                var border = RegionBorder(0);
                for (int ring = 0; ring < 2; ring++)
                {
                    foreach (Point2D point in border)
                    {
                        regions[Slot(point)] = -2;
                        Flatten(point);
                    }

                    if (ring == 0)
                    {
                        border = border.AsEnumerable().Reverse().SelectMany(point => adjacent.Select(offset => point + offset))
                            .Where(point => map.GetTile(point) != null && regions[Slot(point)] == 0).Distinct().ToList();
                    }
                }

                var interior = new bool[regions.Length];
                foreach (MapTile cell in cells)
                {
                    int slot = Slot(cell.CoordsToPoint());
                    interior[slot] = regions[slot] == 0 || regions[slot] == workingRegion;
                    if (regions[slot] == -2)
                        regions[slot] = 0;
                }

                return interior;
            }

            private void SeedSwamp(List<Point2D> lake, int painted)
            {
                Point2D? origin = null;
                for (int i = 0; i < 200 && !origin.HasValue; i++)
                {
                    Point2D candidate = lake[random.Pick(0, lake.Count - 1)];
                    if (!IsSwamp(map.GetTileOrFail(candidate)))
                        origin = candidate;
                }

                if (!origin.HasValue)
                    return;

                int count = random.Pick(Math.Min(painted / 8, 50), Math.Min(painted / 2, 200));
                var queue = new NativePriorityQueue<Point2D>();
                var swamp = new List<Point2D>(count);
                queue.Add(origin.Value, 0);
                for (int step = 0; step < count && queue.TryTake(out Point2D point); step++)
                {
                    map.GetTileOrFail(point).ChangeTileIndex(swampStart, 0);
                    swamp.Add(point);
                    for (int direction = 0; direction < adjacent.Length; direction += 2)
                    {
                        Point2D next = point + adjacent[direction];
                        if (IsWater(map.GetTile(next)))
                            queue.Add(next, SpreadScore(origin.Value, next, step));
                    }
                }

                foreach (Point2D point in swamp)
                {
                    int mask = 0;
                    for (int facing = 0; facing < adjacent.Length; facing += 2)
                    {
                        MapTile neighbor = map.GetTile(point + adjacent[facing]);
                        if (neighbor == null || !(IsSwamp(neighbor) || neighbor.TileIndex == swampStart + SwampCount))
                            mask |= 1 << (facing / 2);
                    }

                    map.GetTileOrFail(point).TileIndex = mask == 0 ? swampStart : swampLat + mask;
                }

                List<int> variants = Enumerable.Range(swampStart + 1, SwampCount - 1).ToList();
                int patches = random.Pick(1, 8);
                for (int i = 0; i < patches; i++)
                {
                    int variant = random.Pick(0, variants.Count - 1);
                    ITileImage tile = tileImage(variants[variant]);
                    Point2D? patch = null;
                    for (int attempt = 0; attempt < 200 && !patch.HasValue; attempt++)
                    {
                        Point2D candidate = swamp[random.Pick(0, swamp.Count - 1)];
                        if (RectangleCells(new Rectangle(candidate.X, candidate.Y, tile.Width, tile.Height))
                            .All(point => map.GetTile(point)?.TileIndex == swampStart))
                        {
                            patch = candidate;
                        }
                    }

                    if (!patch.HasValue)
                        break;

                    for (int sub = 0; sub < tile.Width * tile.Height; sub++)
                    {
                        Point2D point = patch.Value + new Point2D(sub % tile.Width, sub / tile.Width);
                        map.GetTileOrFail(point).ChangeTileIndex(tile.TileID, (byte)sub);
                    }

                    variants.RemoveAt(variant);
                }

                int foliage = random.Pick(0, 4);
                for (int i = 0; i < foliage; i++)
                {
                    Point2D point = swamp[random.Pick(0, swamp.Count - 1)];
                    if (map.GetTileOrFail(point).TerrainObject != null)
                        continue;

                    string name = "FONA0" + random.Pick(1, 5);
                    TerrainType type = terrainTypes.First(candidate => candidate.ININame == name);
                    map.AddTerrainObject(new TerrainObject(type, point));
                }
            }

            private void ClearRegion(int id)
            {
                foreach (MapTile cell in cells)
                {
                    Point2D point = cell.CoordsToPoint();
                    int slot = Slot(point);
                    if (regions[slot] != id)
                        continue;

                    regions[slot] = 0;
                    expandable.Remove(point);
                    Flatten(point);
                }
            }

            private void Flatten(Point2D point)
            {
                MapTile cell = map.GetTileOrFail(point);
                cell.ChangeTileIndex(clearTile, 0);
                cell.Level = groundLevel;
            }

            private List<Point2D> RegionBorder(int id)
            {
                return rowOrder.Select(cell => cell.CoordsToPoint()).Where(point => regions[Slot(point)] == id &&
                    adjacent.Any(offset => map.GetTile(point + offset) != null && regions[Slot(point + offset)] != id)).ToList();
            }

            private bool GrowRegion(int rings, Rectangle? bounds = null, bool raiseLand = false)
            {
                List<Point2D> border = RegionBorder(workingRegion);
                for (int ring = 0; ring < rings; ring++)
                {
                    var nextBorder = new List<Point2D>();
                    foreach (Point2D point in border)
                    {
                        foreach (Point2D offset in adjacent)
                        {
                            Point2D next = point + offset;
                            MapTile cell = map.GetTile(next);
                            if (cell == null || !InBounds(next, bounds))
                                continue;

                            int slot = Slot(next);
                            int region = regions[slot];
                            bool reclaim = raiseLand && region == workingRegion - 1;
                            if ((region == 0 || reclaim) && (IsClear(cell) || (reclaim && (IsWater(cell) ||
                                InSet(cell.TileIndex, shoreStart, ShoreCount) || IsWaterfall(cell.TileIndex)))))
                            {
                                regions[slot] = workingRegion;
                                if (raiseLand && IsClear(cell))
                                    cell.Level = groundLevel;

                                nextBorder.Add(next);
                            }
                            else if (region != workingRegion)
                            {
                                return false;
                            }
                        }
                    }

                    border = nextBorder;
                }

                return true;
            }

            private bool GrowWaterRegion(float spreadScale, Rectangle? bounds, Point2D origin, bool claimFrontier)
            {
                // A cut at the far edge points the spread back into the retained half of the map.
                double angle = 0.0;
                if (bounds.HasValue)
                {
                    Rectangle rect = bounds.Value;
                    if (rect.X == 0 && rect.Width != NativeMapExtent)
                        angle = Math.PI;
                    if (rect.Y != 0)
                        angle = 3 * Math.PI / 2;
                    else if (rect.Height != NativeMapExtent)
                        angle = Math.PI / 2;
                }

                List<Point2D> border = RegionBorder(workingRegion);
                var queue = new NativePriorityQueue<Point2D>();
                foreach (Point2D seed in border)
                {
                    if (InBounds(seed, bounds))
                        queue.Add(seed, AngleScore(origin, seed, angle));
                }

                int count = (int)(1.0f / ((float)(1.0 / Math.Max(1.0, Math.Log(border.Count))) * spreadScale) * 0.5f);
                count += random.Pick(0, count / 2);
                var marked = new HashSet<Point2D>();
                bool success = true;
                for (int i = 0; queue.TryTake(out Point2D point) && success && i < count; i++)
                {
                    int slot = Slot(point);
                    if (regions[slot] == 0 && IsClear(map.GetTileOrFail(point)))
                    {
                        expandable.Add(point);
                        regions[slot] = workingRegion;
                    }

                    for (int direction = 0; direction < adjacent.Length; direction += 2)
                    {
                        Point2D next = point + adjacent[direction];
                        MapTile neighbor = map.GetTile(next);
                        if (neighbor == null)
                            continue;

                        int otherRegion = regions[Slot(next)];
                        if (otherRegion == 0)
                        {
                            if (InBounds(next, bounds) && IsClear(neighbor) && marked.Add(next))
                                queue.Add(next, AngleScore(origin, next, angle));
                        }
                        else if (otherRegion != workingRegion)
                        {
                            success = false;
                        }
                    }

                    angle += random.SampleNormal() * (Math.PI / 4);
                }

                while (claimFrontier && success && queue.TryTake(out Point2D next))
                {
                    int slot = Slot(next);
                    if (regions[slot] == 0)
                        regions[slot] = workingRegion;
                    else
                        success = regions[slot] == workingRegion;
                }

                return success;
            }

            private static bool InBounds(Point2D point, Rectangle? bounds) =>
                bounds == null || bounds.Value.Contains(point.X, point.Y);

            private static double Radians(double degrees) => degrees * Math.PI / 180.0;

            private float SpreadScore(Point2D origin, Point2D point, int step) =>
                10f * (float)random.Fraction() + (float)Distance(origin, point) * 0.5f - step * 0.02f;

            private float AngleScore(Point2D origin, Point2D point, double angle)
            {
                int dx = point.X - origin.X;
                int dy = point.Y - origin.Y;
                double heading = dx == 0 ? Math.PI / 2 : Math.Atan(-(double)dy / dx);
                if (dx < 0)
                    heading += Math.PI;

                double difference = Math.Abs(heading - angle);
                while (difference >= Math.PI * 2)
                    difference -= Math.PI * 2;
                if (difference > Math.PI)
                    difference = Math.PI * 2 - difference;

                return (float)(random.Fraction() * 2.0 + 1.5 * difference);
            }

            private static IEnumerable<Point2D> RectangleCells(Rectangle rectangle)
            {
                for (int y = rectangle.Top; y < rectangle.Bottom; y++)
                {
                    for (int x = rectangle.Left; x < rectangle.Right; x++)
                        yield return new Point2D(x, y);
                }
            }

            private bool BuildShores()
            {
                Array.Fill(waterMasks, -1);
                foreach (MapTile cell in cells)
                {
                    if (!ExpandWater(cell))
                        return false;
                }

                foreach (MapTile cell in cells)
                    PruneWater(cell);

                for (int pass = 1; pass <= 2; pass++)
                {
                    foreach (MapTile cell in cells)
                    {
                        if (!PlaceShore(cell, pass))
                            return false;
                    }
                }

                return true;
            }

            private int WaterMask(MapTile cell, int control)
            {
                if (cell == null || (control == 0 && !IsClear(cell)) || (control == 1 && IsWater(cell)))
                    return 0;

                Point2D point = cell.CoordsToPoint();
                int mask = waterMasks[Slot(point)];

                return mask >= 0 ? mask : NeighborMask(point, next => IsWater(map.GetTile(next)));
            }

            private bool ExpandWater(MapTile cell)
            {
                Point2D point = cell.CoordsToPoint();
                int slot = Slot(point);
                waterMasks[slot] = WaterMask(cell, 2);
                int mask = WaterMask(cell, 0);
                if (mask == 0)
                    return true;

                bool expand = mask is (NE | E | S) or (E | S | SW) ||
                    Has(mask, W | N) && (mask & (NE | SW)) == 0 ||
                    Has(mask, E | N) && (mask & (SE | NW)) == 0 ||
                    Has(mask, E | S) && (mask & (NE | SW)) == 0 ||
                    Has(mask, S | W) && (mask & (SE | NW)) == 0;

                for (int direction = 0; direction < adjacent.Length; direction += 2)
                {
                    if ((mask & (1 << ((direction + 4) % 8))) == 0)
                        continue;

                    Point2D offset = adjacent[direction];
                    int first = WaterMask(map.GetTile(point + offset), 0);
                    int second = WaterMask(map.GetTile(point + offset + offset), 0);
                    expand |= ((first | second) & (1 << direction)) != 0;
                }

                if (!expand && !Has(mask, N | S) && !Has(mask, E | W))
                    return true;

                if (regions[slot] > 0 && regions[slot] != workingRegion)
                    return false;

                cell.ChangeTileIndex(waterStart, 0);
                regions[slot] = workingRegion;
                foreach (Point2D offset in adjacent)
                {
                    Point2D next = point + offset;
                    if (map.GetTile(next) is MapTile neighbor)
                    {
                        waterMasks[Slot(next)] = -1;
                        ExpandWater(neighbor);
                    }
                }

                return true;
            }

            private void PruneWater(MapTile cell)
            {
                if (!InSet(cell.TileIndex, waterStart, WaterCount - 2))
                    return;

                int mask = WaterMask(cell, 2);
                if (mask is not ((N | NE | E | SE | NW) or (SE | S | SW | W | NW) or
                    (N | NE | SW | W | NW) or (NE | E | SE | S | SW) or
                    (N | E | SE | NW) or (SE | S | W | NW) or (N | NE | SW | W) or (NE | E | S | SW)))
                {
                    return;
                }

                cell.ChangeTileIndex(clearTile, 0);
                Point2D point = cell.CoordsToPoint();
                foreach (Point2D offset in adjacent)
                {
                    Point2D next = point + offset;
                    if (map.GetTile(next) is MapTile neighbor)
                    {
                        waterMasks[Slot(next)] = -1;
                        waterMasks[Slot(next)] = WaterMask(neighbor, 2);
                    }
                }
            }

            private static bool Has(int mask, int sides) => (mask & sides) == sides;

            private bool PlaceShore(MapTile cell, int pass)
            {
                // Every cell consumes a draw, including cells outside the shoreline.
                int variant = random.Pick(0, 5);
                int mask = WaterMask(cell, pass - 1);
                if (mask == 0)
                    return true;

                Point2D point = cell.CoordsToPoint();
                int index = pass == 1 ? StraightShore(point, mask, variant) : CornerShore(mask, variant);
                if (index == 0)
                    return true;

                int first = pass == 1 ? clearTile : shoreStart;
                int last = pass == 1 ? clearTile : shoreStart + ShoreCount - 1;

                return StampShoreOrWaterfall(shoreStart + index - 1, point + shoreOffsets[index - 1], cell.Level, first, last);
            }

            private int StraightShore(Point2D point, int mask, int variant)
            {
                if ((mask & E) != 0)
                {
                    int run = ShoreRun(point, adjacent[2], adjacent[4]);
                    return ((run & 1) != 0 && (mask & N) == 0) || (mask & SE) == 0 || (mask & (S | SW)) != 0
                        ? 12 : variant % 3 + 9;
                }

                if ((mask & W) != 0)
                {
                    int run = ShoreRun(point, adjacent[6], adjacent[4]);
                    return ((run & 1) != 0 && (mask & N) == 0) || (mask & SW) == 0 || (mask & (SE | S)) != 0
                        ? 28 : variant % 3 + 25;
                }

                if ((mask & S) != 0)
                {
                    int run = ShoreRun(point, adjacent[4], adjacent[2]);
                    return (run & 1) != 0 || (mask & SE) == 0 || (mask & (NE | E)) != 0 ? 4 : variant % 3 + 1;
                }

                if ((mask & N) != 0)
                {
                    int run = ShoreRun(point, adjacent[0], adjacent[2]);
                    return (run & 1) != 0 || (mask & NE) == 0 || (mask & (E | SE)) != 0 ? 20 : variant % 3 + 17;
                }

                if ((mask & NE) != 0)
                    return (variant & 1) + 35;
                if ((mask & SE) != 0)
                    return (variant & 1) + 33;
                if ((mask & SW) != 0)
                    return (variant & 1) + 39;

                return (mask & NW) != 0 ? (variant & 1) + 37 : 0;
            }

            private int ShoreRun(Point2D point, Point2D across, Point2D along)
            {
                int run = 1;
                for (Point2D next = point + along; !IsWater(map.GetTile(next)) && IsWater(map.GetTile(next + across)); next += along)
                    run++;

                return run;
            }

            private static int CornerShore(int mask, int variant)
            {
                if (Has(mask, N | W))
                    return Has(mask, NE | SW) ? (variant & 1) + 23 : (mask & NE) != 0 ? 21 : 30;
                if (Has(mask, N | E))
                    return Has(mask, NW | SE) ? (variant & 1) + 15 : (mask & SE) != 0 ? 14 : 22;
                if (Has(mask, S | E))
                    return Has(mask, NE | SW) ? (variant & 1) + 7 : (mask & NE) != 0 ? 13 : 6;
                if (Has(mask, W | S))
                    return Has(mask, NW | SE) ? (variant & 1) + 31 : (mask & SE) != 0 ? 5 : 29;

                return 0;
            }

            private bool StampShoreOrWaterfall(int index, Point2D origin, int height, int first, int last)
            {
                ITileImage tile = tileImage(index);
                int pendingShore = index - shoreStart;
                for (int sub = 0; sub < tile.Width * tile.Height; sub++)
                {
                    var point = origin + new Point2D(sub % tile.Width, sub / tile.Width);
                    MapTile cell = map.GetTile(point);
                    TmpImage part = tile.GetSubTile(sub)?.TmpImage;
                    if (cell == null || part == null)
                        continue;

                    int slot = Slot(point);
                    int id = regions[slot];
                    int existingShore = cell.TileIndex - shoreStart;
                    bool bothShore = (uint)existingShore < ShoreCount && (uint)pendingShore < ShoreCount;
                    if (id > 0 && id != workingRegion)
                    {
                        if (!IsClear(cell))
                            return bothShore && shoreGroups[existingShore] == shoreGroups[pendingShore];

                        regions[slot] = workingRegion;
                    }
                    else if (id == workingRegion)
                    {
                        if (bothShore && Math.Abs(shoreFacings[existingShore] - shoreFacings[pendingShore]) is >= 3 and <= 5)
                            return false;
                    }
                    else if (IsClear(cell))
                    {
                        regions[slot] = workingRegion;
                    }
                    else
                    {
                        return false;
                    }

                    if (IsClear(cell) || cell.TileIndex >= first && cell.TileIndex <= last)
                    {
                        cell.ChangeTileIndex(index, (byte)sub);
                        cell.Level = (byte)(height + part.Height);
                    }
                    else
                    {
                        // A blocked subtile ends the placement. Only shore and cliff clashes fail the pass.
                        return !((uint)existingShore < ShoreCount && IsWaterfallCliff(index, sub) ||
                            (uint)pendingShore < ShoreCount && IsWaterfallCliff(cell.TileIndex, cell.SubTileIndex));
                    }
                }

                return true;
            }

            public bool IsWaterfallCliff(int tile, int subtile)
            {
                for (int direction = 0; direction < waterfallStarts.Length; direction++)
                {
                    int start = waterfallStarts[direction];
                    if (!InSet(tile, start, 4))
                        continue;

                    if (tile == start + 1 || tile == start + 2)
                        return true;

                    return direction switch
                    {
                        0 => subtile is not (2 or 3),
                        1 => subtile is not (0 or 4),
                        2 => subtile is not (0 or 1),
                        _ => subtile is not (1 or 3)
                    };
                }

                return false;
            }

            private const int NativeMapExtent = 512;

            private bool TryWaterfall(Point2D first, Point2D last, int direction)
            {
                Rectangle approach;
                Rectangle inlet;
                Rectangle spillway;
                Rectangle bounds;
                Point2D center;
                int span;

                switch (direction)
                {
                    case 0:
                        span = last.X - first.X + 1;
                        approach = new Rectangle(first.X - 2, first.Y - 12, span + 4, 12);
                        inlet = new Rectangle(first.X, first.Y - 4, span, 4);
                        spillway = new Rectangle(first.X, first.Y - 12, span, 8);
                        bounds = new Rectangle(0, inlet.Y, NativeMapExtent, NativeMapExtent - inlet.Y);
                        center = new Point2D(first.X, inlet.Y);
                        break;
                    case 4:
                        span = first.X - last.X + 1;
                        approach = new Rectangle(last.X - 2, last.Y + 1, span + 4, 12);
                        inlet = new Rectangle(last.X, last.Y + 1, span, 4);
                        spillway = new Rectangle(last.X, last.Y + 5, span, 8);
                        bounds = new Rectangle(0, 0, NativeMapExtent, spillway.Y - 1);
                        center = new Point2D(last.X, last.Y + 1);
                        break;
                    case 2:
                        span = last.Y - first.Y + 1;
                        approach = new Rectangle(first.X + 1, first.Y - 2, 12, span + 4);
                        inlet = new Rectangle(first.X + 1, first.Y, 4, span);
                        spillway = new Rectangle(first.X + 5, first.Y, 8, span);
                        bounds = new Rectangle(0, 0, spillway.X - 1, NativeMapExtent);
                        center = new Point2D(first.X + 5, first.Y);
                        break;
                    default:
                        span = first.Y - last.Y + 1;
                        approach = new Rectangle(last.X - 12, last.Y - 2, 12, span + 4);
                        inlet = new Rectangle(last.X - 4, last.Y, 4, span);
                        spillway = new Rectangle(last.X - 12, last.Y, 8, span);
                        bounds = new Rectangle(inlet.X, 0, NativeMapExtent - inlet.X, NativeMapExtent);
                        center = new Point2D(inlet.X, last.Y);
                        break;
                }

                foreach (Point2D point in RectangleCells(approach))
                {
                    if (map.GetTile(point) is MapTile cell && (regions[Slot(point)] != 0 || !IsClear(cell)))
                        return false;
                }

                PaintRectangle(inlet);
                if (!GrowWaterRegion(0.003f, bounds, center, false) || !BuildShores() || !GrowRegion(2, bounds))
                    return false;

                foreach (MapTile cell in cells)
                {
                    if (regions[Slot(cell.CoordsToPoint())] == workingRegion)
                        cell.Level += 4;
                }

                PaintRectangle(spillway);
                StampWaterfall(inlet, spillway, direction);

                return true;
            }

            private void PaintRectangle(Rectangle rect)
            {
                foreach (Point2D point in RectangleCells(rect))
                {
                    if (map.GetTile(point) is MapTile cell)
                        PaintWater(cell);
                }
            }

            private void StampWaterfall(Rectangle inlet, Rectangle patch, int direction)
            {
                int start = waterfallStarts[direction / 2];
                if (start < 0)
                    throw new InvalidOperationException("The loaded theater lacks the stock waterfall tiles.");

                Point2D origin;
                Point2D tail;
                Point2D heightSample;
                Point2D firstCorner;
                Point2D secondCorner;
                Point2D along;
                int count;
                bool shifted;
                switch (direction)
                {
                    case 2:
                        origin = new Point2D(patch.X, patch.Bottom);
                        tail = origin - new Point2D(0, patch.Height + 2);
                        heightSample = origin + new Point2D(2, 0);
                        firstCorner = origin + new Point2D(0, 2);
                        secondCorner = origin - new Point2D(0, patch.Height + 3);
                        along = new Point2D(0, -1);
                        count = patch.Height;
                        shifted = false;
                        break;
                    case 6:
                        origin = new Point2D(inlet.X - 2, patch.Bottom);
                        tail = origin - new Point2D(0, patch.Height + 2);
                        heightSample = origin - new Point2D(2, 0);
                        firstCorner = origin + new Point2D(1, 2);
                        secondCorner = origin + new Point2D(1, -patch.Height - 3);
                        along = new Point2D(0, -1);
                        count = patch.Height;
                        shifted = true;
                        break;
                    case 4:
                        origin = new Point2D(patch.X - 2, patch.Y);
                        tail = origin + new Point2D(patch.Width + 2, 0);
                        heightSample = origin + new Point2D(0, 2);
                        firstCorner = origin - new Point2D(1, 0);
                        secondCorner = origin + new Point2D(patch.Width + 4, 0);
                        along = new Point2D(1, 0);
                        count = patch.Width;
                        shifted = false;
                        break;
                    default:
                        origin = new Point2D(inlet.X - 2, inlet.Y - 2);
                        tail = origin + new Point2D(patch.Width + 2, 0);
                        heightSample = origin - new Point2D(0, 2);
                        firstCorner = origin + new Point2D(-1, 1);
                        secondCorner = origin + new Point2D(patch.Width + 4, 1);
                        along = new Point2D(1, 0);
                        count = patch.Width;
                        shifted = true;
                        break;
                }

                int level = map.GetTile(heightSample)?.Level ?? 0;
                StampShoreOrWaterfall(start, origin, level, clearTile, ushort.MaxValue);
                StampShoreOrWaterfall(start + 3, tail, level, clearTile, ushort.MaxValue);

                if (direction == 6)
                {
                    ClearWaterfallCell(origin - new Point2D(0, 1));
                    ClearWaterfallCell(origin + new Point2D(0, patch.Width + 2));
                    LowerStrip(origin, along, count + 2);
                }
                else if (direction == 0)
                {
                    ClearWaterfallCell(origin + new Point2D(1, 0));
                    ClearWaterfallCell(tail);
                    LowerStrip(origin + along, along, count + 2);
                }

                RaiseWaterfallCorner(firstCorner);
                RaiseWaterfallCorner(secondCorner);

                bool vertical = along.Y != 0;
                for (int placed = 0; placed < count;)
                {
                    int segments = (count - placed) % 2 == 0 ? 2 : 1;
                    int distance = placed + (vertical ? segments : 2);
                    Point2D anchor = origin + new Point2D(along.X * distance, along.Y * distance);
                    if (shifted)
                        anchor += vertical ? new Point2D(1, 0) : new Point2D(0, 1);

                    StampShoreOrWaterfall(start + segments, anchor, level, clearTile, ushort.MaxValue);
                    placed += segments;
                }
            }

            private void ClearWaterfallCell(Point2D point)
            {
                if (map.GetTile(point) is MapTile cell)
                    cell.TileIndex = clearTile;
            }

            private void LowerStrip(Point2D point, Point2D direction, int length)
            {
                for (int i = 0; i < length; i++, point += direction)
                {
                    if (map.GetTile(point) is MapTile cell)
                        cell.Level -= 4;
                }
            }

            private void RaiseWaterfallCorner(Point2D point)
            {
                if (map.GetTile(point) is MapTile cell)
                {
                    cell.ChangeTileIndex(clearTile, 0);
                    cell.Level += 4;
                    regions[Slot(point)] = workingRegion;
                }
            }
        }


        private sealed class ArcticWater
        {
            private readonly Map map;
            private readonly GeneratorRandom random;
            private readonly int iceTile;
            private readonly int clearTile;
            private readonly int waterAmount;
            private readonly int waterBudget;
            private readonly HashSet<Point2D> region = new HashSet<Point2D>();
            private readonly HashSet<Point2D> iced = new HashSet<Point2D>();
            private int seededWaterAmount;

            public ArcticWater(Map map, GeneratorRandom random, int iceTile, int clearTile, int waterAmount)
            {
                this.map = map;
                this.random = random;
                this.iceTile = iceTile;
                this.clearTile = clearTile;
                this.waterAmount = waterAmount;
                waterBudget = (int)((double)0.40f * map.LocalSize.Width * map.LocalSize.Height * waterAmount * 0.02 + 100);
            }

            public void Seed()
            {
                if (waterAmount > 20)
                {
                    for (int attempt = 0; attempt < 10; attempt++)
                    {
                        if (SeedRiver())
                        {
                            region.Clear();
                            break;
                        }
                    }
                }

                for (int attempt = 0; attempt < 10; attempt++)
                {
                    if (SeedLake())
                        break;
                }
            }

            private static readonly Point2D[] neighbors = { new(0, -1), new(1, 0), new(0, 1), new(-1, 0) };

            private static double Radians(double degrees) => degrees * Math.PI / 180.0;

            private bool SeedRiver(Point2D? origin = null, double angle = 0)
            {
                Point2D seed;
                if (origin.HasValue)
                {
                    seed = origin.Value;
                }
                else
                {
                    int direction = random.Pick(0, 3);
                    int x = random.Pick(0, map.Size.X - 1);
                    int y = random.Pick(0, map.Size.Y - 1);
                    var corner = new Point2D(1, map.Size.X);
                    var farCorner = new Point2D(map.Size.X + map.Size.Y - 1, map.Size.Y);
                    seed = direction switch
                    {
                        0 => corner + new Point2D(x, -x),
                        1 => farCorner - new Point2D(y, y),
                        2 => farCorner - new Point2D(x, -x),
                        _ => corner + new Point2D(y, y)
                    };
                    double startAngle = Radians(315) - direction * (Math.PI / 2);
                    angle = random.NativeTruncatedNormal(startAngle, Radians(60), startAngle - Math.PI / 2, startAngle + Math.PI / 2);
                }

                if (map.GetTile(seed) == null)
                    return false;

                double width = random.Pick(3, (int)Math.Max(4, waterAmount * 0.09));
                int halfWidth = (int)width / 2;
                double minWidth = width - halfWidth;
                double maxWidth = width + halfWidth;
                double minAngle = angle - Math.PI / 2;
                double maxAngle = angle + Math.PI / 2;
                double xCenter = seed.X + 0.5;
                double yCenter = seed.Y + 0.5;
                var ice = new List<Point2D>();
                bool success = true;
                double stopRoll = 1.0;
                int steps = 0;

                while (map.GetTile((int)xCenter, (int)yCenter) != null && success)
                {
                    double stepX = Math.Sin(angle);
                    double stepY = Math.Cos(angle);
                    int span = (int)(width + 0.5);
                    double x = xCenter - stepX * (span - 1) * 0.5;
                    double y = yCenter - stepY * (span - 1) * 0.5;
                    for (int i = 0; i < span; i++)
                    {
                        var point = new Point2D((int)x, (int)y);
                        if (map.GetTile(point) is MapTile cell)
                        {
                            MarkIce(point, cell, ice, false);
                        }

                        x += stepX;
                        y += stepY;
                    }

                    xCenter += stepY;
                    yCenter -= stepX;
                    if (random.Fraction() < 0.01)
                    {
                        double branch = random.NativeTruncatedNormal(angle + Math.PI / 2, Radians(30),
                            angle + Radians(30), angle + Radians(150));
                        success = SeedRiver(new Point2D((int)x, (int)y), branch);
                    }

                    angle += random.NativeTruncatedNormal(0, Radians(18), minAngle - angle, maxAngle - angle);
                    width += random.NativeTruncatedNormal(0, 0.5, minWidth - width, maxWidth - width);
                    stopRoll = random.Fraction();
                    steps++;
                    if (stopRoll < 0.005)
                        break;
                }

                if (!success || steps < 40)
                {
                    ClearRegion();
                    return false;
                }

                var endpoint = new Point2D((int)xCenter, (int)yCenter);
                if (stopRoll < 0.005 && map.GetTile(endpoint) != null)
                    SeedLake(endpoint);

                PaintIce(ice, IceCracked, IceCracked - 1);
                PaintIce(ice, IceEdge, 1);
                seededWaterAmount += steps;
                return true;
            }

            private bool SeedLake(Point2D? origin = null)
            {
                int budget = waterBudget - seededWaterAmount;
                if (budget <= 0)
                    return false;

                Point2D seed;
                if (origin.HasValue)
                {
                    seed = origin.Value;
                }
                else
                {
                    int attempt = 0;
                    do
                    {
                        int x = random.Pick(0, map.Size.X - 1);
                        int y = random.Pick(0, map.Size.Y - 1);
                        seed = new Point2D(1 + x + y, map.Size.X - x + y);
                        if (++attempt == 200)
                            return false;
                    }
                    while (map.GetTile(seed)?.TileIndex != clearTile);
                }

                int target = (int)random.NativeTruncatedNormal(budget / 4, budget / 6, 75, Math.Max(budget, 76));
                var queue = new NativePriorityQueue<Point2D>();
                queue.Add(seed, 0);
                var seen = new HashSet<Point2D> { seed };
                var ice = new List<Point2D>();
                int steps = 0;
                while (queue.TryTake(out Point2D point) && steps < target)
                {
                    MarkIce(point, map.GetTileOrFail(point), ice, true);
                    foreach (Point2D offset in neighbors)
                    {
                        Point2D neighbor = point + offset;
                        if (map.GetTile(neighbor)?.TileIndex == clearTile && seen.Add(neighbor))
                        {
                            float randomScore = 10f * (float)random.Fraction();
                            float distanceScore = (float)Distance(seed, neighbor) * 0.5f;
                            queue.Add(neighbor, randomScore + distanceScore - steps * 0.02f);
                        }
                    }

                    steps++;
                }

                // The native loop has already removed one node when it tests the spread limit.
                while (queue.TryTake(out Point2D remaining))
                {
                    MarkIce(remaining, map.GetTileOrFail(remaining), ice, true);
                    steps++;
                }

                PaintIce(ice, IceCracked, IceCracked - 1);
                PaintIce(ice, IceEdge, 1);
                seededWaterAmount += steps;
                return true;
            }

            private void PaintIce(List<Point2D> ice, int variant, int randomRange)
            {
                int patches = random.Pick(0, 15);
                iced.ExceptWith(ice);
                var queue = new NativePriorityQueue<Point2D>();
                for (int patch = 0; patch < patches; patch++)
                {
                    int target = random.Pick(3, Math.Max(4, ice.Count / 20));
                    Point2D seed = ice[random.Pick(0, ice.Count - 1)];
                    iced.Add(seed);
                    queue.Clear();
                    queue.Add(seed, 0);
                    for (int step = 0; step < target && queue.TryTake(out Point2D point); step++)
                    {
                        map.GetTileOrFail(point).TileIndex = iceTile + variant;
                        foreach (Point2D offset in neighbors)
                        {
                            Point2D neighbor = point + offset;
                            int? tile = map.GetTile(neighbor)?.TileIndex;
                            if (tile >= iceTile && tile <= iceTile + IceEdge && iced.Add(neighbor))
                            {
                                double score = random.NextValue() * (double)randomRange * (1.0 / uint.MaxValue);
                                queue.Add(neighbor, (float)(score + Distance(seed, neighbor)));
                            }
                        }
                    }
                }
            }

            private void MarkIce(Point2D point, MapTile cell, List<Point2D> ice, bool allowGrowth)
            {
                cell.ChangeTileIndex(iceTile, 0);
                if (allowGrowth)
                    cell.IceGrowth = 1;

                region.Add(point);
                ice.Add(point);
            }

            private void ClearRegion()
            {
                foreach (Point2D point in region)
                    map.GetTileOrFail(point).ChangeTileIndex(clearTile, 0);

                region.Clear();
            }
        }

        private sealed class ArcticIceSmoother
        {
            private const int IceCount = 64;
            private const int IceShoreCount = 48;
            private const int WaterCount = 14;
            private const int ShoreCount = 42;

            private static readonly Point2D[] fullIceNeighbors = { new(0, -1), new(0, 1), new(1, 0), new(-1, 0) };

            // IceSetLut uses clockwise neighbors starting at NE. -2 fills a gap with ice.
            private static readonly sbyte[] transitions =
            {
                -1, 33, 2,  2,  34, 37, 2,  2,
                4,  26, 6,  6,  4,  26, 6,  6,
                35, 45, 17, 17, 38, 41, 17, 17,
                4,  26, 6,  6,  4,  26, 6,  6,
                8,  21, 10, 10, 27, 31, 10, 10,
                12, 23, 14, 14, 12, 23, 14, 14,
                8,  21, 10, 10, 27, 31, 10, 10,
                12, 23, 14, 14, 12, 23, 14, 14,
                32, 36, 25, 25, 44, 40, 25, 25,
                19, 30, 20, 20, 19, 30, 20, 20,
                39, 43, 29, 29, 42, 46, 29, 29,
                19, 30, 20, 20, 19, 30, 20, 20,
                8,  21, 10, 10, 27, 31, 10, 10,
                12, 23, 14, 14, 12, 23, 14, 14,
                8,  21, 10, 10, 27, 31, 10, 10,
                12, 23, 14, 14, 12, 23, 14, 14,
                1,  1,  3,  3,  16, 16, 3,  3,
                5,  5,  7,  7,  5,  5,  7,  7,
                24, 24, 18, 18, 28, 28, 18, 18,
                5,  5,  7,  7,  5,  5,  7,  7,
                9,  9,  11, 11, 22, 22, 11, 11,
                13, 13, -2, -2, 13, 13, -2, -2,
                9,  9,  11, 11, 22, 22, 11, 11,
                13, 13, -2, -2, 13, 13, -2, -2,
                1,  1,  3,  3,  16, 16, 3,  3,
                5,  5,  7,  7,  5,  5,  7,  7,
                24, 24, 18, 18, 28, 28, 18, 18,
                5,  5,  7,  7,  5,  5,  7,  7,
                9,  9,  11, 11, 22, 22, 11, 11,
                13, 13, -2, -2, 13, 13, -2, -2,
                9,  9,  11, 11, 22, 22, 11, 11,
                13, 13, -2, -2, 13, 13, -2, -2
            };

            private readonly Map map;
            private readonly int clearTile;
            private readonly int[] iceSets;
            private readonly int waterSet;
            private readonly int shoreSet;
            private readonly int iceShoreSet;

            // The headless OpenTS exporter initializes the scenario RNG with seed 1.
            private readonly GeneratorRandom scenarioRandom = new GeneratorRandom(1);

            public ArcticIceSmoother(Map map, int clearTile, int[] iceSets, int waterSet, int shoreSet, int iceShoreSet)
            {
                this.map = map;
                this.clearTile = clearTile;
                this.iceSets = iceSets;
                this.waterSet = waterSet;
                this.shoreSet = shoreSet;
                this.iceShoreSet = iceShoreSet;
            }

            public void Smooth()
            {
                // Preparing the blank scenario takes four draws before the first ice tile is selected.
                for (int i = 0; i < 4; i++)
                    scenarioRandom.NextValue();

                // MapClass::Iterate walks each isometric row from left to right.
                foreach (MapTile cell in InIsometricOrder(map))
                {
                    if (IsIce(cell.TileIndex))
                        SmoothCell(cell);
                }
            }

            private int TileAt(Point2D point) => map.GetTile(point)?.TileIndex ?? clearTile;
            private bool IsIce(int tile) => tile >= iceSets[0] && tile < iceSets[2] + IceCount;
            private bool IsFullIce(int tile) => iceSets.Any(start => Within(tile, start, IceCracked));
            private bool IsIceEdge(int tile) => iceSets.Any(start => Within(tile, start + IceEdge, IceCount - IceEdge));
            private static bool Within(int tile, int start, int count) => tile >= start && tile < start + count;

            private int PickIceSet()
            {
                uint index;
                do
                {
                    index = scenarioRandom.NextValue() & 3;
                }
                while (index == 3);

                return iceSets[index];
            }

            private void SmoothCell(MapTile cell)
            {
                Point2D point = cell.CoordsToPoint();
                int tile = cell.TileIndex;
                bool plain = iceSets.Contains(tile);
                if (plain && fullIceNeighbors.Any(offset => Within(TileAt(point + offset), shoreSet, ShoreCount)))
                {
                    tile = iceSets[0] + IceEdge;
                    cell.TileIndex = tile;
                    plain = false;
                }

                if (plain)
                    SmoothFullNeighbors(point);

                if (IsFullIce(tile))
                    SmoothFull(point);
                else if (IsIceEdge(tile))
                    SmoothEdge(point);

                if (!plain)
                    SmoothFullNeighbors(point);

                foreach (Point2D offset in adjacent)
                    SmoothEdge(point + offset);

                foreach (Point2D offset in adjacent)
                    SmoothShore(point + offset);
            }

            private void SmoothFullNeighbors(Point2D point)
            {
                foreach (Point2D offset in fullIceNeighbors)
                    SmoothFull(point + offset);
            }

            private void SmoothFull(Point2D point)
            {
                MapTile cell = map.GetTile(point);
                if (cell == null || !IsFullIce(cell.TileIndex))
                    return;

                int mask = 0;
                for (int i = 0; i < adjacent.Length; i += 2)
                {
                    if (!IsFullIce(TileAt(point + adjacent[i])))
                        mask |= 1 << (i / 2);
                }

                int variant = mask == 0 ? 0 : mask + 1;
                if (!iceSets.Any(start => cell.TileIndex == start + variant || mask == 0 && cell.TileIndex == start + 1))
                    cell.ChangeTileIndex(PickIceSet() + variant, 0);
            }

            private int NeighborTransition(Point2D point, Func<int, bool> include)
            {
                int mask = 0;
                for (int i = 0; i < adjacent.Length; i++)
                {
                    if (include(TileAt(point + adjacent[i])))
                        mask |= 1 << ((i + 7) % 8);
                }

                return transitions[mask];
            }

            private void SmoothEdge(Point2D point)
            {
                MapTile cell = map.GetTile(point);
                if (cell == null || !Within(cell.TileIndex, waterSet, WaterCount) && !IsIceEdge(cell.TileIndex))
                    return;

                int variant = NeighborTransition(point,
                    tile => !Within(tile, waterSet, WaterCount) && !IsIceEdge(tile) && !Within(tile, shoreSet, ShoreCount));
                if (variant == -1)
                {
                    cell.ChangeTileIndex(PickIceSet() + IceCount - 1, 0);
                    return;
                }

                int offset = (variant == -2 ? IceCracked - 1 : variant) + IceCracked;
                // The native edge pass exempts existing variants in the first two ice sets.
                if (cell.TileIndex == iceSets[0] + offset || cell.TileIndex == iceSets[1] + offset)
                    return;

                cell.ChangeTileIndex(PickIceSet() + offset, 0);
                SmoothFullNeighbors(point);
            }

            private void SmoothShore(Point2D point)
            {
                MapTile cell = map.GetTile(point);
                if (cell == null || cell.TileIndex != clearTile && !Within(cell.TileIndex, iceShoreSet, IceShoreCount))
                    return;

                int variant = NeighborTransition(point, IsIce);
                if (variant == -2)
                {
                    cell.ChangeTileIndex(PickIceSet(), 0);
                    SmoothCell(cell);
                }
                else if (variant >= 0 && cell.TileIndex != iceShoreSet + variant)
                {
                    cell.ChangeTileIndex(iceShoreSet + variant, 0);
                }
            }
        }

        private sealed class NativePriorityQueue<T>
        {
            // OpenTS uses a binary heap with single-precision scores and no secondary key.
            private readonly List<(T Item, float Score)> heap = new List<(T, float)>();

            public void Clear() => heap.Clear();

            public void Add(T item, float score)
            {
                int index = heap.Count;
                heap.Add(default);
                while (index > 0)
                {
                    int parent = (index - 1) / 2;
                    if (heap[parent].Score <= score)
                        break;

                    heap[index] = heap[parent];
                    index = parent;
                }

                heap[index] = (item, score);
            }

            public bool TryTake(out T item)
            {
                if (heap.Count == 0)
                {
                    item = default;
                    return false;
                }

                item = heap[0].Item;
                heap[0] = heap[^1];
                heap.RemoveAt(heap.Count - 1);
                int index = 0;
                while (true)
                {
                    int left = index * 2 + 1;
                    int right = left + 1;
                    int next = left < heap.Count && heap[left].Score < heap[index].Score ? left : index;
                    if (right < heap.Count && heap[right].Score < heap[next].Score)
                        next = right;

                    if (next == index)
                        return true;

                    (heap[index], heap[next]) = (heap[next], heap[index]);
                    index = next;
                }
            }
        }

        // Random2Class and the Random3Class initialization rounds used by TS.
        private sealed class GeneratorRandom
        {
            private static readonly uint[] mix1 = { 0xbaa96887, 0x1e17d32c, 0x03bcdc3c, 0x0f33d1b2 };
            private static readonly uint[] mix2 = { 0x4b0f3b58, 0xe874f0c3, 0x6955c5a6, 0x55a7ca46 };
            private readonly uint[] table = new uint[250];
            private int index1;
            private int index2 = 103;
            private double? spare;
            public int DrawCount { get; private set; }

            public GeneratorRandom(int seed)
            {
                for (int i = 0; i < table.Length; i++)
                {
                    int low = seed;
                    int high = i;
                    for (int round = 0; round < mix1.Length; round++)
                    {
                        int mixed = high ^ unchecked((int)mix1[round]);
                        int partLow = mixed & 0xffff;
                        int partHigh = mixed >> 16;
                        int product = unchecked(partLow * partLow + ~(partHigh * partHigh));
                        product = (product >> 16) | (product << 16);
                        int nextHigh = low ^ unchecked((product ^ (int)mix2[round]) + partLow * partHigh);
                        low = high;
                        high = nextHigh;
                    }

                    table[i] = unchecked((uint)high);
                }
            }

            public uint NextValue()
            {
                DrawCount++;
                uint value = table[index1] ^= table[index2];
                index1 = (index1 + 1) % table.Length;
                index2 = (index2 + 1) % table.Length;

                return value;
            }

            public double Fraction() => NextValue() * (1.0 / uint.MaxValue);

            public int Pick(int min, int max)
            {
                int result;
                do
                {
                    result = min + (int)((max - min + 1) * Fraction());
                }
                while (result > max);

                return result;
            }

            public double SampleNormal()
            {
                if (spare.HasValue)
                {
                    double next = spare.Value;
                    spare = null;

                    return next;
                }

                double u;
                double v;
                double radius;
                do
                {
                    u = Fraction() * 2 - 1;
                    v = Fraction() * 2 - 1;
                    radius = u * u + v * v;
                }
                while (radius is <= 0 or >= 1);

                double scale = Math.Sqrt(-2 * Math.Log(radius) / radius);
                spare = v * scale;

                return u * scale;
            }

            public double TruncatedNormal(double mean, double scale, double min, double max)
            {
                if (min == max || scale <= 0)
                    return Math.Clamp(mean, min, max);

                return NativeTruncatedNormal(mean, scale, min, max);
            }

            public double NativeTruncatedNormal(double mean, double scale, double min, double max)
            {
                if (mean - scale > max || mean + scale < min)
                {
                    scale = (max - min) * 0.5;
                    mean = min + scale;
                }

                double value;
                do
                {
                    value = mean + scale * SampleNormal();
                }
                while (value < min || value > max);

                return value;
            }
        }

        internal static void Apply(Map draft, Map map)
        {
            foreach (var structure in map.Structures.ToArray())
                map.RemoveBuilding(structure);
            foreach (var unit in map.Units.ToArray())
                map.RemoveUnit(unit);
            foreach (var aircraft in map.Aircraft.ToArray())
                map.RemoveAircraft(aircraft);
            foreach (var infantry in map.Infantry.ToArray())
                map.RemoveInfantry(infantry);
            foreach (var terrain in map.TerrainObjects.ToArray())
                map.RemoveTerrainObject(terrain);
            foreach (var waypoint in map.Waypoints.ToArray())
                map.RemoveWaypoint(waypoint);
            foreach (var cellTag in map.CellTags.ToArray())
                map.RemoveCellTagFrom(cellTag.Position);
            foreach (var house in map.GetHouses())
                house.BaseNodes.Clear();

            map.GraphicalBaseNodes.Clear();
            map.Tubes.Clear();
            if (map.Size != draft.Size)
                map.Resize(draft.Size, 0, 0);

            map.LocalSize = draft.LocalSize;
            map.DoForAllValidTiles(cell =>
            {
                MapTile source = draft.GetTile(cell.CoordsToPoint());
                cell.ChangeTileIndex(source?.TileIndex ?? 0, source?.SubTileIndex ?? 0);
                cell.Level = source?.Level ?? 4;
                cell.IceGrowth = source?.IceGrowth ?? 0;
                cell.Overlay = source?.Overlay;
                cell.Smudge = null;
            });

            foreach (var waypoint in draft.Waypoints)
                map.AddWaypoint(new Waypoint { Position = waypoint.Position, Identifier = waypoint.Identifier });
            foreach (var terrain in draft.TerrainObjects)
                map.AddTerrainObject(new TerrainObject(terrain.TerrainType, terrain.Position));
            foreach (var structure in draft.Structures)
            {
                map.PlaceBuilding(new Structure(structure.ObjectType)
                {
                    Position = structure.Position, Owner = structure.Owner, HP = structure.HP, Facing = structure.Facing
                });
            }

            foreach (Unit unit in draft.Units)
                map.PlaceUnit((Unit)unit.Clone());
            foreach (Infantry infantry in draft.Infantry)
                map.PlaceInfantry((Infantry)infantry.Clone());

            map.Basic.MinPlayer = 2;
            map.Basic.MaxPlayer = draft.Waypoints.Count;
            map.Basic.MultiplayerOnly = true;
            map.Lighting.Ambient = draft.Lighting.Ambient;
            map.Lighting.Level = draft.Lighting.Level;
            map.Lighting.Ground = 0;
            map.Lighting.Red = 1;
            map.Lighting.Green = 1;
            map.Lighting.Blue = 1;
            map.Lighting.RefreshLightingColors();
        }
    }
}
