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
    /// The symmetry strategy used when placing player start locations and distributing
    /// resource fields, so that every player has a comparably fair position.
    /// </summary>
    public enum SymmetryMode
    {
        /// <summary>N-fold rotational symmetry around the map center (points evenly spaced on a circle/ellipse).</summary>
        Rotational,

        /// <summary>Left/right axis-mirror symmetry across a vertical line through the map center.</summary>
        Mirror
    }

    /// <summary>
    /// Terrain generation strategy used by the headless RMG pipeline.
    /// </summary>
    public enum TerrainAlgorithm
    {
        /// <summary>The original approach: run <see cref="Mutations.Classes.TerrainGenerationMutation"/>
        /// once over the whole map with all configured tile groups competing independently per cell.</summary>
        Scatter,

        /// <summary>
        /// Divide the map into blocks and use a Wave Function Collapse solver (see
        /// <see cref="WaveFunctionCollapseSolver"/>) to decide which tile group governs each
        /// block, respecting the theater's real LAT ground/base adjacency rules so neighboring
        /// blocks are only assigned tile groups that are actually allowed to sit next to each
        /// other. Produces more spatially coherent terrain clusters than <see cref="Scatter"/>.
        /// </summary>
        WaveFunctionCollapse
    }

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

        /// <summary>
        /// Symmetry mode used for player start placement and resource distribution.
        /// Defaults to <see cref="SymmetryMode.Rotational"/>.
        /// </summary>
        public SymmetryMode Symmetry { get; set; } = SymmetryMode.Rotational;

        /// <summary>
        /// Terrain generation strategy. Defaults to <see cref="TerrainAlgorithm.Scatter"/>
        /// (the original, already-shipped behavior) for backwards compatibility; pass
        /// "--algorithm wfc" to opt into the newer Wave-Function-Collapse-based layout.
        /// </summary>
        public TerrainAlgorithm Algorithm { get; set; } = TerrainAlgorithm.Scatter;

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
                    case "symmetry":
                        if (value.Equals("mirror", StringComparison.OrdinalIgnoreCase))
                            options.Symmetry = SymmetryMode.Mirror;
                        else if (value.Equals("rotational", StringComparison.OrdinalIgnoreCase))
                            options.Symmetry = SymmetryMode.Rotational;
                        break;
                    case "algorithm":
                        if (value.Equals("wfc", StringComparison.OrdinalIgnoreCase) ||
                            value.Equals("wavefunctioncollapse", StringComparison.OrdinalIgnoreCase))
                            options.Algorithm = TerrainAlgorithm.WaveFunctionCollapse;
                        else if (value.Equals("scatter", StringComparison.OrdinalIgnoreCase))
                            options.Algorithm = TerrainAlgorithm.Scatter;
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

                GenerateTerrain(map, mutationTarget, theater, random, options.Algorithm);
                PlaceResources(map, random, options.PlayerCount, options.Symmetry);
                PlacePlayerStarts(map, options.PlayerCount, options.Symmetry);

                map.AutoSave(options.OutputPath);

                Logger.Log($"Headless RMG: generated {options.Width}x{options.Height} {options.Theater} map " +
                    $"with {options.PlayerCount} player starts ({options.Symmetry} symmetry, {options.Algorithm} terrain algorithm, seed {seed}, gamemode {options.GameMode ?? "(none)"}) -> {options.OutputPath}");

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
        /// configured for the map's theater in Config/TerrainGeneratorPresets.ini. Dispatches to
        /// either the original whole-map "scatter" strategy or the newer Wave-Function-Collapse
        /// block layout strategy, depending on <paramref name="algorithm"/>.
        /// </summary>
        private static void GenerateTerrain(Map map, HeadlessMutationTarget mutationTarget, Theater theater, Random random, TerrainAlgorithm algorithm)
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

            if (algorithm == TerrainAlgorithm.WaveFunctionCollapse && configuration.TileGroups.Count >= 2)
            {
                if (GenerateTerrainWithWfc(map, mutationTarget, theater, configuration, random))
                    return;

                Logger.Log("Headless RMG: Wave Function Collapse terrain layout failed (contradiction or setup issue); falling back to the scatter algorithm.");
            }

            var mutation = new TerrainGenerationMutation(mutationTarget, cells, configuration, random.Next());
            mutation.Generate();
        }

        /// <summary>
        /// Lays out terrain by dividing the map into fixed-size blocks and using a Wave
        /// Function Collapse solver to decide which single <see cref="TerrainGeneratorTileGroup"/>
        /// governs each block, so neighboring blocks are only ever assigned tile groups that the
        /// theater's actual LAT ground/base data says are allowed to be adjacent (falling back to
        /// "always compatible with the theater's default base tileset", index 0, which every LAT
        /// transition in the theater is ultimately defined relative to - see Theater.InitLATGround).
        /// Each block is then painted using the existing, already-vetted
        /// <see cref="TerrainGenerationMutation"/> engine, restricted to that one tile group.
        ///
        /// Returns false (without modifying the map further) if the solver hits a contradiction
        /// or the configuration doesn't have enough tile groups to make WFC meaningful, so the
        /// caller can fall back to the scatter algorithm instead.
        /// </summary>
        private static bool GenerateTerrainWithWfc(Map map, HeadlessMutationTarget mutationTarget, Theater theater, TerrainGeneratorConfiguration configuration, Random random)
        {
            const int blockSize = 6; // cells per block edge; arbitrary proof-of-concept granularity

            var domains = configuration.TileGroups;
            int domainCount = domains.Count;

            var weights = new double[domainCount];
            for (int i = 0; i < domainCount; i++)
                weights[i] = Math.Max(domains[i].OpenChance, 0.01);

            var compatibility = new bool[domainCount, domainCount];
            for (int i = 0; i < domainCount; i++)
            {
                for (int j = 0; j < domainCount; j++)
                {
                    if (i == j)
                    {
                        compatibility[i, j] = true;
                        continue;
                    }

                    var tileSetA = domains[i].TileSet;
                    var tileSetB = domains[j].TileSet;

                    bool compatible =
                        tileSetA == tileSetB ||
                        tileSetA.Index == 0 || tileSetB.Index == 0 || // the theater's default base/clear tileset is always a safe neighbor
                        theater.LATGrounds.Exists(lg =>
                            (lg.GroundTileSet == tileSetA && lg.BaseTileSet == tileSetB) ||
                            (lg.GroundTileSet == tileSetB && lg.BaseTileSet == tileSetA));

                    compatibility[i, j] = compatible;
                }
            }

            int blocksX = Math.Max(1, (int)Math.Ceiling(map.LocalSize.Width / (double)blockSize));
            int blocksY = Math.Max(1, (int)Math.Ceiling(map.LocalSize.Height / (double)blockSize));

            var solver = new WaveFunctionCollapseSolver(blocksX, blocksY, weights, compatibility);
            int[] assignment = solver.Solve(random);

            if (assignment == null)
                return false;

            for (int by = 0; by < blocksY; by++)
            {
                for (int bx = 0; bx < blocksX; bx++)
                {
                    int domainIndex = assignment[by * blocksX + bx];
                    var tileGroup = domains[domainIndex];

                    var blockCells = new List<Point2D>();
                    int startX = map.LocalSize.X + bx * blockSize;
                    int startY = map.LocalSize.Y + by * blockSize;
                    int endX = Math.Min(startX + blockSize, map.LocalSize.X + map.LocalSize.Width);
                    int endY = Math.Min(startY + blockSize, map.LocalSize.Y + map.LocalSize.Height);

                    for (int y = startY; y < endY; y++)
                    {
                        for (int x = startX; x < endX; x++)
                        {
                            if (map.GetTile(x, y) != null)
                                blockCells.Add(new Point2D(x, y));
                        }
                    }

                    if (blockCells.Count == 0)
                        continue;

                    for (int i = blockCells.Count - 1; i > 0; i--)
                    {
                        int j = random.Next(i + 1);
                        (blockCells[i], blockCells[j]) = (blockCells[j], blockCells[i]);
                    }

                    var blockConfiguration = new TerrainGeneratorConfiguration(
                        configuration.Name + " (WFC block)",
                        configuration.Theater,
                        configuration.IsUserConfiguration,
                        configuration.TerrainTypeGroups,
                        new List<TerrainGeneratorTileGroup> { tileGroup },
                        new List<TerrainGeneratorOverlayGroup>(), // resource/overlay placement is handled separately by PlaceResources
                        configuration.SmudgeGroups);

                    try
                    {
                        var mutation = new TerrainGenerationMutation(mutationTarget, blockCells, blockConfiguration, random.Next());
                        mutation.Generate();
                    }
                    catch (Exception ex)
                    {
                        // Don't let one bad block abort the whole map; log and keep going so the
                        // rest of the map still gets a usable layout.
                        Logger.Log($"Headless RMG: WFC block ({bx},{by}) terrain generation failed, leaving it unpainted: {ex.Message}");
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Scatters Tiberium/ore/gem resource fields across the playable area with
        /// per-player-wedge balance: patch centers are generated only within one
        /// "fundamental domain" (a single rotational wedge, or one side of the mirror axis)
        /// and then replicated via the same symmetry transform used for player start
        /// placement, so every player's fair share of the map gets an equal number of
        /// resource patches of the same size/type. Patch count scales with playable area.
        /// </summary>
        private static void PlaceResources(Map map, Random random, int playerCount, SymmetryMode symmetry)
        {
            var tiberiumOverlays = map.Rules.OverlayTypes.Where(o => o.Tiberium).ToList();
            if (tiberiumOverlays.Count == 0)
            {
                Logger.Log("Headless RMG: no Tiberium/ore overlay types found in Rules; skipping resource placement.");
                return;
            }

            var (centerX, centerY, radiusX, radiusY) = GetSymmetryEllipse(map);

            // Replica count per generated patch: one per player under rotational symmetry
            // (one patch per wedge = exactly one per player), or two under mirror symmetry
            // (one patch and its mirror image, independent of player count).
            int replicas = symmetry == SymmetryMode.Mirror ? 2 : playerCount;

            int playableArea = map.LocalSize.Width * map.LocalSize.Height;
            const int cellsPerPatchAttemptPerWedge = 900; // arbitrary proof-of-concept density
            int patchesPerWedge = Math.Max(1, (playableArea / replicas) / cellsPerPatchAttemptPerWedge);

            for (int p = 0; p < patchesPerWedge; p++)
            {
                double angleDeg;
                if (symmetry == SymmetryMode.Rotational)
                {
                    // Sample within the first wedge only; replicas below rotate it to the rest.
                    double wedgeDeg = 360.0 / playerCount;
                    angleDeg = -90 + random.NextDouble() * wedgeDeg;
                }
                else
                {
                    // Sample within the right half only; the single mirror replica covers the left half.
                    angleDeg = -90 + random.NextDouble() * 180;
                }

                double radiusFraction = 0.15 + random.NextDouble() * 0.7; // avoid the very center and the map edge
                double baseAngle = angleDeg * Math.PI / 180.0;

                var overlayType = tiberiumOverlays[random.Next(tiberiumOverlays.Count)];
                int patchRadius = 2 + random.Next(3); // 2..4

                for (int r = 0; r < replicas; r++)
                {
                    double px, py;

                    if (symmetry == SymmetryMode.Rotational)
                    {
                        double rotatedAngle = baseAngle + r * (2 * Math.PI / playerCount);
                        px = centerX + radiusX * radiusFraction * Math.Cos(rotatedAngle);
                        py = centerY + radiusY * radiusFraction * Math.Sin(rotatedAngle);
                    }
                    else
                    {
                        double bx = centerX + radiusX * radiusFraction * Math.Cos(baseAngle);
                        double by = centerY + radiusY * radiusFraction * Math.Sin(baseAngle);
                        px = r == 0 ? bx : centerX - (bx - centerX); // r == 1: mirror across the vertical axis
                        py = by;
                    }

                    var patchCenter = new Point2D((int)Math.Round(px), (int)Math.Round(py));
                    PlacePatch(map, random, patchCenter, patchRadius, overlayType);
                }
            }
        }

        private static void PlacePatch(Map map, Random random, Point2D center, int patchRadius, OverlayType overlayType)
        {
            for (int y = -patchRadius; y <= patchRadius; y++)
            {
                for (int x = -patchRadius; x <= patchRadius; x++)
                {
                    if (x * x + y * y > patchRadius * patchRadius)
                        continue;

                    if (random.NextDouble() > 0.6)
                        continue;

                    var targetCoords = center + new Point2D(x, y);
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

        /// <summary>
        /// Returns the center point and ellipse radii (as a fraction of the map's local/playable
        /// size) used as the basis for both player start placement and resource-patch replication,
        /// so both follow the exact same geometric symmetry.
        /// </summary>
        private static (double centerX, double centerY, double radiusX, double radiusY) GetSymmetryEllipse(Map map)
        {
            double centerX = map.LocalSize.X + map.LocalSize.Width / 2.0;
            double centerY = map.LocalSize.Y + map.LocalSize.Height / 2.0;

            // Use a radius that comfortably fits inside the playable area.
            double radiusX = map.LocalSize.Width / 2.0 * 0.75;
            double radiusY = map.LocalSize.Height / 2.0 * 0.75;

            return (centerX, centerY, radiusX, radiusY);
        }

        /// <summary>
        /// Places <paramref name="playerCount"/> player-start waypoints (identifiers 0..N-1),
        /// arranged according to <paramref name="symmetry"/> so every player has an equally
        /// fair starting position: either N-fold rotational symmetry around the map center, or
        /// left/right mirror symmetry across a vertical axis through the map center.
        /// </summary>
        private static void PlacePlayerStarts(Map map, int playerCount, SymmetryMode symmetry)
        {
            var (centerX, centerY, radiusX, radiusY) = GetSymmetryEllipse(map);

            var positions = new List<Point2D>();

            if (symmetry == SymmetryMode.Rotational)
            {
                for (int i = 0; i < playerCount; i++)
                {
                    double angle = (2 * Math.PI * i / playerCount) - (Math.PI / 2); // start at top, go clockwise
                    positions.Add(ClampToLocalSize(map,
                        (int)Math.Round(centerX + radiusX * Math.Cos(angle)),
                        (int)Math.Round(centerY + radiusY * Math.Sin(angle))));
                }
            }
            else
            {
                // Pair players up across a vertical mirror axis through the map center.
                // For an odd player count, the final player is placed on the axis itself.
                int pairCount = playerCount / 2;
                bool hasAxisPlayer = playerCount % 2 == 1;

                for (int i = 0; i < pairCount; i++)
                {
                    double t = pairCount == 1 ? 0.5 : (double)i / (pairCount - 1);
                    double angleDeg = -70 + t * 140; // spread across the right half, avoiding the axis extremes
                    double angle = angleDeg * Math.PI / 180.0;

                    double rightX = centerX + radiusX * Math.Cos(angle);
                    double y = centerY + radiusY * Math.Sin(angle);
                    double leftX = centerX - (rightX - centerX);

                    positions.Add(ClampToLocalSize(map, (int)Math.Round(rightX), (int)Math.Round(y)));
                    positions.Add(ClampToLocalSize(map, (int)Math.Round(leftX), (int)Math.Round(y)));
                }

                if (hasAxisPlayer)
                {
                    positions.Add(ClampToLocalSize(map, (int)Math.Round(centerX), (int)Math.Round(centerY + radiusY)));
                }
            }

            for (int i = 0; i < positions.Count; i++)
            {
                var position = positions[i];

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

        private static Point2D ClampToLocalSize(Map map, int x, int y)
        {
            x = Math.Clamp(x, map.LocalSize.X, map.LocalSize.X + map.LocalSize.Width - 1);
            y = Math.Clamp(y, map.LocalSize.Y, map.LocalSize.Y + map.LocalSize.Height - 1);
            return new Point2D(x, y);
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
