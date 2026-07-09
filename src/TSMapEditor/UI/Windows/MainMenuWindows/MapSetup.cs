using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.IO;
using TSMapEditor.CCEngine;
using TSMapEditor.Initialization;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.Extensions;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.CCEngine.TileData;

namespace TSMapEditor.UI.Windows.MainMenuWindows
{
    /// <summary>
    /// Helper class for setting up a map.
    /// </summary>
    public class MapSetup
    {
        private readonly List<string> mapLoadErrors = new List<string>();

        public Map LoadedMap { get; private set; }
        public CCFileManager FileManager { get; private set; }

        public IReadOnlyList<string> MapLoadErrors => mapLoadErrors;

        /// <summary>
        /// Tries to load a map. If successful, returns null. If loading the map
        /// fails, returns an error message.
        /// </summary>
        /// <param name="gameDirectory">The path to the game directory.</param>
        /// <param name="createNew">Whether a new map should be created (instead of loading an existing map).</param>
        /// <param name="existingMapPath">The path to the existing map file to load, if loading an existing map. Can be null if creating a new map.</param>
        /// <param name="newMapTheater">The theater of the map, if creating a new map.</param>
        /// <param name="newMapSize">The size of the map, if creating a new map.</param>
        /// <param name="windowManager">The XNAUI window manager.</param>
        /// <returns>Null of loading the map was successful, otherwise an error message.</returns>
        public string InitializeMap(string gameDirectory, bool createNew, string existingMapPath, CreateNewMapEventArgs newMapParameters, WindowManager windowManager = null)
        {
            LoadedMap = null;
            mapLoadErrors.Clear();

            FileManager = new() { GameDirectory = gameDirectory };
            FileManager.ReadConfig();

            var gameConfigIniFiles = new GameConfigINIFiles(gameDirectory, FileManager);

            // Search for tutorial lines from all directories specified in the file manager configuration
            string tutorialsPath = FileManager.FindFileFromDirectories(Constants.TutorialIniPath);
            if (tutorialsPath == null)
                tutorialsPath = Path.Combine(gameDirectory, Constants.TutorialIniPath);

            Action<Action> tutorialLinesModifyCallback = windowManager == null ? _ => { } : a => windowManager.AddCallback(a, null);
            var tutorialLines = new TutorialLines(tutorialsPath, tutorialLinesModifyCallback);
            var themes = new Themes(IniFileEx.FromPathOrMix(Constants.ThemeIniPath, gameDirectory, FileManager));
            var evaSpeeches = new EvaSpeeches(IniFileEx.FromPathOrMix(Constants.EvaIniPath, gameDirectory, FileManager));
            var sounds = new Sounds(IniFileEx.FromPathOrMix(Constants.SoundIniPath, gameDirectory, FileManager));

            Map map = new Map(FileManager);

            if (createNew)
            {
                if (newMapParameters == null)
                    throw new NullReferenceException("Null new map parameters encountered when creating a new map!");

                map.InitNew(gameConfigIniFiles, newMapParameters.Theater, newMapParameters.MapSize, newMapParameters.StartingLevel);
            }
            else
            {
                try
                {
                    IniFileEx mapIni = new(Path.Combine(gameDirectory, existingMapPath), FileManager);

                    MapLoader.PreCheckMapIni(mapIni);

                    map.LoadExisting(gameConfigIniFiles, mapIni);
                }
                catch (IniParseException ex)
                {
                    return string.Format(Translate("MapSetup.InitializeMap.IniParseException", 
                        "The selected file does not appear to be a proper map file (INI file). Maybe it's corrupted?" +
                        Environment.NewLine + Environment.NewLine +
                        "Returned error: {0}"), ex.Message);
                }
                catch (MapLoadException ex)
                {
                    return string.Format(Translate("MapSetup.InitializeMap.MapLoadException",
                        "Failed to load the selected map file." +
                        Environment.NewLine + Environment.NewLine +
                        "Returned error: {0}"), ex.Message);
                }
            }

            map.Rules.TutorialLines = tutorialLines;
            map.Rules.Themes = themes;
            map.Rules.Speeches = evaSpeeches;
            map.Rules.Sounds = sounds;

            Console.WriteLine();
            Console.WriteLine("Map created.");

            LoadedMap = map;
            mapLoadErrors.AddRange(MapLoader.MapLoadErrors);

            return null;
        }

        private Theater InitTheater()
        {
            Theater theater = LoadedMap.EditorConfig.Theaters.Find(t => t.UIName.Equals(LoadedMap.TheaterName, StringComparison.InvariantCultureIgnoreCase));
            if (theater == null)
                throw new InvalidOperationException("Theater of map not found: " + LoadedMap.TheaterName);

            theater.ReadConfigINI(FileManager.GameDirectory, FileManager);

            foreach (string theaterMIXName in theater.ContentMIXName)
                FileManager.LoadRequiredMixFile(theaterMIXName);

            foreach (string theaterMIXName in theater.OptionalContentMIXName)
                FileManager.LoadOptionalMixFile(theaterMIXName);

            return theater;
        }

        /// <summary>
        /// Loads the theater graphics for the last-loaded map.
        /// </summary>
        /// <param name="windowManager">The window manager.</param>
        public void LoadTheaterGraphics(WindowManager windowManager)
        {
            if (LoadedMap == null)
                throw new InvalidOperationException("Cannot load theater graphics before a map has been initialized.");

            Theater theater = InitTheater();

            TheaterGraphics theaterGraphics = new TheaterGraphics(windowManager.GraphicsDevice, theater, FileManager, LoadedMap.Rules);
            LoadedMap.TheaterInstance = theaterGraphics;
            FillConnectedTileFoundations(theaterGraphics);

            MapLoader.MapLoadErrors.Clear();
            MapLoader.MapLoadErrors.AddRange(mapLoadErrors);

            MapLoader.PostCheckMap(LoadedMap, theaterGraphics);
            mapLoadErrors.Clear();
            mapLoadErrors.AddRange(MapLoader.MapLoadErrors);

            EditorGraphics editorGraphics = new EditorGraphics();

            LoadedMap.EditorConfig.PostTheaterInit(LoadedMap.Rules);

            var uiManager = new UIManager(windowManager, LoadedMap, theaterGraphics, editorGraphics);
            windowManager.AddAndInitializeControl(uiManager);

            const int margin = 60;
            string errorList = string.Join("\r\n\r\n", mapLoadErrors);
            int errorListHeight = (int)Renderer.GetTextDimensions(errorList, Constants.UIDefaultFont).Y;

            if (errorListHeight > windowManager.RenderResolutionY - margin)
            {
                EditorMessageBox.Show(windowManager, 
                    Translate("MapSetup.ManyMapLoadErrors.Title", "Errors while loading map"),
                    Translate("MapSetup.ManyMapLoadErrors.Description", "A massive number of errors was encountered while loading the map. See MapEditorLog.log for details."),
                    MessageBoxButtons.OK);
            }
            else if (mapLoadErrors.Count > 0)
            {
                EditorMessageBox.Show(windowManager, 
                    Translate("MapSetup.MapLoadErrors.Title", "Errors while loading map"),
                    string.Format(Translate("MapSetup.MapLoadErrors.Description", 
                        "One or more errors were encountered while loading the map:" + Environment.NewLine + Environment.NewLine + "{0}"), errorList),
                    MessageBoxButtons.OK);
            }
        }

        /// <summary>
        /// Loads theater data without forming GPU texture instances.
        /// </summary>
        public void LoadNonGraphicalTheater()
        {
            if (LoadedMap == null)
                throw new InvalidOperationException("Cannot load theater before a map has been initialized.");

            Theater theater = InitTheater();

            TheaterTileData theaterTileData = new TheaterTileData(theater, FileManager, LoadedMap.Rules);
            LoadedMap.TheaterInstance = theaterTileData;
            FillConnectedTileFoundations(theaterTileData);

            MapLoader.MapLoadErrors.Clear();
            MapLoader.MapLoadErrors.AddRange(mapLoadErrors);

            MapLoader.PostCheckMap(LoadedMap, theaterTileData);
            mapLoadErrors.Clear();
            mapLoadErrors.AddRange(MapLoader.MapLoadErrors);

            LoadedMap.EditorConfig.PostTheaterInit(LoadedMap.Rules);
        }

        /// <summary>
        /// Automatically fills the foundations of all connected tiles
        /// for which the foundation has not been specified in the config.
        /// </summary>
        private void FillConnectedTileFoundations(ITheater theaterTileInfo)
        {
            foreach (var cliffType in LoadedMap.EditorConfig.Cliffs)
            {
                if (!cliffType.AllowedTheaters.Select(at => at.ToUpperInvariant()).Contains(LoadedMap.LoadedTheaterName.ToUpperInvariant()))
                    continue;

                var tiles = cliffType.Tiles;
                if (tiles.Count == 0)
                    throw new INIConfigException($"Connected terrain type {cliffType.IniName} has 0 tiles!");

                foreach (var cliffTypeTile in cliffType.Tiles)
                {
                    var tileSet = theaterTileInfo.Theater.TileSets.Find(ts => ts.SetName == cliffTypeTile.TileSetName && ts.AllowToPlace);

                    if (tileSet == null)
                    {
                        string errorMessage = $"Unable to find TileSet \"{cliffTypeTile.TileSetName}\" " +
                            $"for connected terrain type \"{cliffType.IniName}\", tile index {cliffTypeTile.Index}";
#if DEBUG
                        throw new INIConfigException(errorMessage);
#else
                        Logger.Log("WARNING: " + errorMessage + ". Disabling the connected terrain type.");
                        cliffType.IsLegal = false;
                        break;
#endif
                    }

                    if (cliffTypeTile.IndicesInTileSet.Count == 0)
                        continue;

                    if (cliffTypeTile.Foundation != null)
                        continue;

                    cliffTypeTile.Foundation = new HashSet<GameMath.Point2D>();

                    int firstTileIndexWithinSet = cliffTypeTile.IndicesInTileSet[0];

                    int totalFirstTileIndex = tileSet.StartTileIndex + firstTileIndexWithinSet;

                    var tile = theaterTileInfo.GetTile(totalFirstTileIndex);

                    for (int i = 0; i < tile.SubTileCount; i++)
                    {
                        var subTile = tile.GetSubTile(i);
                        if (subTile == null)
                            continue;

                        var offset = tile.GetSubTileCoordOffset(i).Value;
                        cliffTypeTile.Foundation.Add(offset);
                    }
                }
            }
        }
    }
}
