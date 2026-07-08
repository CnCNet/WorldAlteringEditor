using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.Rendering;

namespace TSMapEditor.Initialization
{
    /// <summary>
    /// Parameters for headless, non-interactive random map generation.
    /// </summary>
    public class HeadlessRmgOptions
    {
        /// <summary>Path to the game's installation directory (contains Rules.ini / mix files).</summary>
        public string GameDirectory { get; set; }

        /// <summary>UI name of the theater to generate the map in (e.g. "Temperate", "Urban", "Snow").</summary>
        public string Theater { get; set; }

        /// <summary>Map width, in cells.</summary>
        public int Width { get; set; }

        /// <summary>Map height, in cells.</summary>
        public int Height { get; set; }

        /// <summary>Number of players the map should support (and place fair start locations for).</summary>
        public int PlayerCount { get; set; }

        /// <summary>Seed for deterministic random generation. If null, a random seed is used.</summary>
        public int? Seed { get; set; }

        /// <summary>
        /// Optional game mode name (e.g. "Standard", "MegaWealth"). Currently accepted and
        /// logged for forward-compatibility with the calling client, but does not yet alter
        /// generation - see plan.md / cross-session notes for the open question on whether
        /// this should drive resource density or a specific terrain generator preset.
        /// </summary>
        public string GameMode { get; set; }

        /// <summary>Path (including file name) that the generated .map file should be written to.</summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// Parses CLI arguments in either "--flag value" (space-separated) or "--flag=value"
        /// form. The "--generate-map" flag itself takes no value and is ignored here.
        /// </summary>
        public static HeadlessRmgOptions ParseArgs(string[] args)
        {
            var options = new HeadlessRmgOptions();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (!arg.StartsWith("--"))
                    continue;

                string key;
                string value = null;

                int separatorIndex = arg.IndexOf('=');
                if (separatorIndex >= 0)
                {
                    // --flag=value form
                    key = arg.Substring(2, separatorIndex - 2).Trim().ToLowerInvariant();
                    value = arg.Substring(separatorIndex + 1).Trim();
                }
                else
                {
                    // --flag value (space-separated) form
                    key = arg.Substring(2).Trim().ToLowerInvariant();
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                    {
                        value = args[i + 1].Trim();
                        i++;
                    }
                }

                if (value == null)
                    continue;

                switch (key)
                {
                    case "game-directory":
                        options.GameDirectory = value;
                        break;
                    case "theater":
                        options.Theater = value;
                        break;
                    case "width":
                        options.Width = int.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "height":
                        options.Height = int.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "players":
                        options.PlayerCount = int.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "seed":
                        options.Seed = int.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "gamemode":
                        options.GameMode = value;
                        break;
                    case "output":
                        options.OutputPath = value;
                        break;
                }
            }

            return options;
        }

        /// <summary>
        /// Validates that all required options were provided. Returns null if valid,
        /// otherwise an error message describing the first problem found.
        /// </summary>
        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(GameDirectory))
                return "Missing required argument: --game-directory";

            if (!Directory.Exists(GameDirectory))
                return $"Game directory does not exist: {GameDirectory}";

            if (string.IsNullOrWhiteSpace(Theater))
                return "Missing required argument: --theater";

            if (Width <= 0 || Height <= 0)
                return "Map width and height must both be positive (--width, --height)";

            if (PlayerCount < 2 || PlayerCount > Constants.MultiplayerMaxPlayers)
                return $"Player count must be between 2 and {Constants.MultiplayerMaxPlayers} (--players)";

            if (string.IsNullOrWhiteSpace(OutputPath))
                return "Missing required argument: --output";

            return null;
        }
    }

    /// <summary>
    /// Drives headless (non-interactive) random map generation: builds an in-memory
    /// <see cref="Map"/> from scratch, procedurally generates terrain, resources, and
    /// symmetric player start locations, then writes the result to a .map file.
    ///
    /// This class does not touch any WinForms/interactive-UI types. It still requires a
    /// live MonoGame <see cref="GraphicsDevice"/> to be supplied by the caller, because
    /// <see cref="TheaterGraphics"/> (theater tile/overlay/smudge graphics metadata) uploads
    /// GPU textures as part of loading - see Phase 1 investigation notes in plan.md for
    /// details on why this can't currently be avoided without a deeper engine refactor.
    /// </summary>
    public static class HeadlessMapGenerator
    {
        /// <summary>
        /// Generates a random map according to <paramref name="options"/> and writes it to disk.
        /// Returns null on success, or an error message describing the failure.
        /// </summary>
        public static string Generate(HeadlessRmgOptions options, GraphicsDevice graphicsDevice)
        {
            string validationError = options.Validate();
            if (validationError != null)
                return validationError;

            try
            {
                var ccFileManager = new CCFileManager() { GameDirectory = options.GameDirectory };
                ccFileManager.ReadConfig();

                var gameConfigIniFiles = new GameConfigINIFiles(options.GameDirectory, ccFileManager);

                var map = new Map(ccFileManager);
                map.InitNew(gameConfigIniFiles, options.Theater, new Point2D(options.Width, options.Height), 0);

                Theater theater = map.EditorConfig.Theaters.Find(t => t.UIName.Equals(options.Theater, StringComparison.InvariantCultureIgnoreCase));
                if (theater == null)
                    return $"Unknown theater: {options.Theater}";

                theater.ReadConfigINI(options.GameDirectory, ccFileManager);

                foreach (string theaterMIXName in theater.ContentMIXName)
                    ccFileManager.LoadRequiredMixFile(theaterMIXName);

                foreach (string theaterMIXName in theater.OptionalContentMIXName)
                    ccFileManager.LoadOptionalMixFile(theaterMIXName);

                var theaterGraphics = new TheaterGraphics(graphicsDevice, theater, ccFileManager, map.Rules);
                map.TheaterInstance = theaterGraphics;

                MapLoader.PostCheckMap(map, theaterGraphics);
                map.EditorConfig.PostTheaterInit(map.Rules);

                int seed = options.Seed ?? Environment.TickCount;
                var random = new Random(seed);

                var mutationTarget = new HeadlessMutationTarget(map);

                GenerateTerrain(map, mutationTarget, theater, random);
                PlaceResources(map, random);
                PlacePlayerStarts(map, options.PlayerCount);

                map.AutoSave(options.OutputPath);

                Logger.Log($"Headless RMG: generated {options.Width}x{options.Height} {options.Theater} map " +
                    $"with {options.PlayerCount} player starts (seed {seed}, gamemode {options.GameMode ?? "(none)"}) -> {options.OutputPath}");

                return null;
            }
            catch (Exception ex)
            {
                Logger.Log("Headless RMG failed: " + ex);
                return "Map generation failed: " + ex.Message;
            }
        }

        /// <summary>
        /// Returns all cells within the map's local (playable) size.
        /// </summary>
        private static List<Point2D> GetPlayableCells(Map map)
        {
            var cells = new List<Point2D>();

            for (int y = map.LocalSize.Y; y < map.LocalSize.Y + map.LocalSize.Height; y++)
            {
                for (int x = map.LocalSize.X; x < map.LocalSize.X + map.LocalSize.Width; x++)
                {
                    if (map.GetTile(x, y) != null)
                        cells.Add(new Point2D(x, y));
                }
            }

            return cells;
        }

        /// <summary>
        /// Runs the existing brush-based terrain generator engine (<see cref="TerrainGenerationMutation"/>)
        /// over the entire playable area of the map, using the first terrain generator preset
        /// configured for the map's theater in Config/TerrainGeneratorPresets.ini.
        /// </summary>
        private static void GenerateTerrain(Map map, HeadlessMutationTarget mutationTarget, Theater theater, Random random)
        {
            var presetsIni = Helpers.ReadConfigINI("TerrainGeneratorPresets.ini");

            TerrainGeneratorConfiguration configuration = null;

            foreach (string sectionName in presetsIni.GetSections())
            {
                string sectionTheater = presetsIni.GetStringValue(sectionName, "Theater", string.Empty);
                if (!string.IsNullOrWhiteSpace(sectionTheater) && !sectionTheater.Equals(map.TheaterName, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                configuration = TerrainGeneratorConfiguration.FromConfigSection(
                    presetsIni.GetSection(sectionName),
                    false,
                    map.Rules.TerrainTypes,
                    theater.TileSets,
                    map.Rules.OverlayTypes,
                    map.Rules.SmudgeTypes);

                if (configuration != null)
                    break;
            }

            if (configuration == null)
            {
                Logger.Log("Headless RMG: no terrain generator preset found for theater " + map.TheaterName + "; skipping terrain variety generation.");
                return;
            }

            var cells = GetPlayableCells(map);

            // Shuffle deterministically using our own seeded Random so that repeated
            // generations with the same seed produce the same layout, independent of
            // TerrainGenerationMutation's internal (currently DateTime-seeded) fallback.
            for (int i = cells.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (cells[i], cells[j]) = (cells[j], cells[i]);
            }

            var mutation = new TerrainGenerationMutation(mutationTarget, cells, configuration, random.Next());
            mutation.Generate();
        }

        /// <summary>
        /// Scatters basic Tiberium/ore/gem resource fields across the playable area.
        /// This is a simple proof-of-concept distribution (random patches), not a
        /// balance-tuned resource layout.
        /// </summary>
        private static void PlaceResources(Map map, Random random)
        {
            var tiberiumOverlays = map.Rules.OverlayTypes.Where(o => o.Tiberium).ToList();
            if (tiberiumOverlays.Count == 0)
            {
                Logger.Log("Headless RMG: no Tiberium/ore overlay types found in Rules; skipping resource placement.");
                return;
            }

            var cells = GetPlayableCells(map);

            const double patchChance = 0.02; // chance per cell to seed a resource patch
            const int patchRadius = 3;

            foreach (var cellCoords in cells)
            {
                if (random.NextDouble() > patchChance)
                    continue;

                var overlayType = tiberiumOverlays[random.Next(tiberiumOverlays.Count)];

                for (int y = -patchRadius; y <= patchRadius; y++)
                {
                    for (int x = -patchRadius; x <= patchRadius; x++)
                    {
                        if (x * x + y * y > patchRadius * patchRadius)
                            continue;

                        if (random.NextDouble() > 0.6)
                            continue;

                        var targetCoords = cellCoords + new Point2D(x, y);
                        var tile = map.GetTile(targetCoords);
                        if (tile == null || tile.Overlay != null)
                            continue;

                        if (tile.MatchesLandType(Models.Enums.LandType.Water) || tile.MatchesLandType(Models.Enums.LandType.Road))
                            continue;

                        tile.Overlay = new Overlay()
                        {
                            OverlayType = overlayType,
                            FrameIndex = 0,
                            Position = targetCoords
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Places <paramref name="playerCount"/> player-start waypoints (identifiers 0..N-1)
        /// arranged with rotational symmetry around the map center, so every player has an
        /// equally fair starting position.
        /// </summary>
        private static void PlacePlayerStarts(Map map, int playerCount)
        {
            double centerX = map.LocalSize.X + map.LocalSize.Width / 2.0;
            double centerY = map.LocalSize.Y + map.LocalSize.Height / 2.0;

            // Use a radius that comfortably fits inside the playable area for all player counts.
            double radiusX = map.LocalSize.Width / 2.0 * 0.75;
            double radiusY = map.LocalSize.Height / 2.0 * 0.75;

            for (int i = 0; i < playerCount; i++)
            {
                double angle = (2 * Math.PI * i / playerCount) - (Math.PI / 2); // start at top, go clockwise
                int x = (int)Math.Round(centerX + radiusX * Math.Cos(angle));
                int y = (int)Math.Round(centerY + radiusY * Math.Sin(angle));

                x = Math.Clamp(x, map.LocalSize.X, map.LocalSize.X + map.LocalSize.Width - 1);
                y = Math.Clamp(y, map.LocalSize.Y, map.LocalSize.Y + map.LocalSize.Height - 1);

                var position = new Point2D(x, y);

                // Make sure we're not placing the start waypoint on top of an existing one
                // (can happen on very small maps with many players); nudge outward if so.
                var existingTile = map.GetTile(position);
                if (existingTile != null && existingTile.Waypoints.Count > 0)
                {
                    position = FindNearestFreeCell(map, position);
                }

                map.AddWaypoint(new Waypoint() { Identifier = i, Position = position });
            }
        }

        private static Point2D FindNearestFreeCell(Map map, Point2D origin)
        {
            for (int radius = 1; radius < Math.Max(map.Size.X, map.Size.Y); radius++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        var candidate = origin + new Point2D(x, y);
                        var tile = map.GetTile(candidate);
                        if (tile != null && tile.Waypoints.Count == 0)
                            return candidate;
                    }
                }
            }

            return origin;
        }
    }
}
