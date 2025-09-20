using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.Rendering.Batching;
using TSMapEditor.Rendering.ObjectRenderers;
using TSMapEditor.Settings;
using TSMapEditor.UI;
using TSMapEditor.UI.Windows;

namespace TSMapEditor.Rendering
{
    public interface IMapView
    {
        Map Map { get; }
        TheaterGraphics TheaterGraphics { get; }
        void AddRefreshPoint(Point2D point, int size = 10);
        void InvalidateMap();
        LightingPreviewMode LightingPreviewState { get; }
        Camera Camera { get; }
        Texture2D MinimapTexture { get; }
        HashSet<object> MinimapUsers { get; }
    }

    /// <summary>
    /// The renderer. Draws the map.
    /// </summary>
    public class MapView : IMapView
    {
        struct WaypointDrawStruct
        {
            public Waypoint Waypoint;
            public Color Color;
            public Rectangle DrawRectangle;

            public WaypointDrawStruct(Waypoint waypoint, Color color, Rectangle drawRectangle)
            {
                Waypoint = waypoint;
                Color = color;
                DrawRectangle = drawRectangle;
            }
        }

        private static Color[] MarbleMadnessTileHeightLevelColors = new Color[]
        {
            new Color(165, 28, 68),
            new Color(202, 149, 101),
            new Color(170, 125, 76),
            new Color(149, 109, 64),
            new Color(133, 97, 56),
            new Color(226, 101, 182),
            new Color(194, 198, 255),
            new Color(20, 153, 20),
            new Color(4, 129, 16),
            new Color(40, 165, 28),
            new Color(230, 198, 109),
            new Color(153, 20, 48),
            new Color(80, 190, 56),
            new Color(56, 89, 133),
            new Color(194, 198, 255)
        };

        public MapView(WindowManager windowManager, Map map, TheaterGraphics theaterGraphics, EditorGraphics editorGraphics, EditorState editorState)
        {
            this.windowManager = windowManager;
            EditorState = editorState;
            Map = map;
            TheaterGraphics = theaterGraphics;
            EditorGraphics = editorGraphics;

            Camera = new Camera(windowManager, Map);
            Camera.CameraUpdated += (s, e) => 
            { 
                cameraMoved = true; 
                if (UserSettings.Instance.GraphicsLevel > 0) InvalidateMap(); 
            };
        }

        private WindowManager windowManager;

        private GraphicsDevice GraphicsDevice => windowManager.GraphicsDevice;

        public int Width => windowManager.RenderResolutionX;
        public int Height => windowManager.RenderResolutionY;

        public EditorState EditorState { get; private set; }
        public Map Map { get; private set; }
        public TheaterGraphics TheaterGraphics { get; private set; }
        public EditorGraphics EditorGraphics { get; private set; }

        public bool Is2DMode => EditorState.Is2DMode;
        public LightingPreviewMode LightingPreviewState => EditorState.IsLighting ? EditorState.LightingPreviewState : LightingPreviewMode.NoLighting;
        public Randomizer Randomizer => EditorState.Randomizer;

        public Texture2D MinimapTexture => minimapRenderTarget;

        /// <summary>
        /// Tracks users of the minimap.
        /// If the minimap texture is not used by anyone, we can save
        /// processing power and skip certain actions that would update it.
        /// </summary>
        public HashSet<object> MinimapUsers { get; } = new HashSet<object>();
        public Camera Camera { get; private set; }

        public MapWideOverlay MapWideOverlay { get; private set; }

        private RenderTarget2D mapRenderTarget;                      // Render target for terrain
        private RenderTarget2D transparencyRenderTarget;             // Render target for map UI elements (celltags etc.) that are only refreshed if something in the map changes (due to performance reasons)
        private RenderTarget2D transparencyPerFrameRenderTarget;     // Render target for map UI elements that are redrawn each frame
        private RenderTarget2D compositeRenderTarget;                // Render target where all the above is combined
        private RenderTarget2D compositeRenderTargetCopy;
        private RenderTarget2D alphaRenderTarget;                    // Render target for alpha map
        private RenderTarget2D minimapRenderTarget;                  // For minimap and megamap rendering

        private Effect palettedTerrainDrawEffect;                    // Effect for rendering terrain
        private Effect palettedColorDrawEffect;                      // Effect for rendering textures, both paletted and RGBA, with or without remap
        private Effect alphaMapDrawEffect;                           // Effect for rendering the alpha light map
        private Effect alphaImageToAlphaMapEffect;                   // Effect for rendering a single alpha image to the alpha light map

        private TerrainBatcher terrainBatcher;
        private GameObjectBatcher gameObjectBatcher;

        private bool mapInvalidated;
        private bool cameraMoved;
        private bool minimapNeedsRefresh;

        private List<Structure> structuresToRender = new List<Structure>();
        private List<Overlay> flatOverlaysToRender = new List<Overlay>();
        private List<GameObject> gameObjectsToRender = new List<GameObject>(); 
        private List<Smudge> smudgesToRender = new List<Smudge>();
        private List<AlphaImageRenderStruct> alphaImagesToRender = new List<AlphaImageRenderStruct>();
        private ObjectSpriteRecord objectSpriteRecord = new ObjectSpriteRecord();

        private List<WaypointDrawStruct> waypointsToRender = new List<WaypointDrawStruct>();

        private Stopwatch refreshStopwatch;

        private ulong refreshIndex;

        private AircraftRenderer aircraftRenderer;
        private AnimRenderer animRenderer;
        private BuildingRenderer buildingRenderer;
        private InfantryRenderer infantryRenderer;
        private OverlayRenderer overlayRenderer;
        private SmudgeRenderer smudgeRenderer;
        private TerrainRenderer terrainRenderer;
        private UnitRenderer unitRenderer;

        private Rectangle mapRenderSourceRectangle;
        private Rectangle mapRenderDestinationRectangle;

        private DepthStencilState depthRenderStencilState;  // Depth stencil state for rendering terrain. Reads and writes depth information.
        private DepthStencilState depthReadStencilState;    // Depth stencil state for rendering smudges and flat overlays. Reads depth information, but does not write it.
        private DepthStencilState objectRenderStencilState; // Depth stencil state for rendering objects. Reads and writes depth information. Also writes stencil information for shadow rendering.
        private DepthStencilState shadowRenderStencilState; // Depth stencil state for rendering shadows. Reads depth information. Also reads stencil information to avoid shadowing objects (the C&C engine does allow shadows on objects either).

        public void AddRefreshPoint(Point2D point, int size = 1)
        {
            InvalidateMap();
        }

        /// <summary>
        /// Schedules the visible portion of the map to be re-rendered
        /// on the next frame.
        /// </summary>
        public void InvalidateMap()
        {
            if (!mapInvalidated)
                refreshIndex++;

            mapInvalidated = true;
        }

        /// <summary>
        /// Schedules the entire map to be re-rendered on the next frame, regardless
        /// of what is visible on the screen.
        /// </summary>
        public void InvalidateMapForMinimap()
        {
            InvalidateMap();
            minimapNeedsRefresh = true;
        }

        public void Initialize()
        {
            LoadShaders();

            MapWideOverlay = new MapWideOverlay();
            EditorState.MapWideOverlayExists = MapWideOverlay.HasTexture;

            RefreshRenderTargets();
            CreateDepthStencilStates();
            InitBatchers();

            Map.LocalSizeChanged += (s, e) => InvalidateMap();
            Map.MapHeightChanged += (s, e) => InvalidateMap();
            Map.Lighting.ColorsRefreshed += (s, e) => Map_LightingColorsRefreshed();
            Map.CellLightingModified += Map_CellLightingModified;

            Map.HouseColorChanged += (s, e) => InvalidateMap();
            EditorState.HighlightImpassableCellsChanged += (s, e) => InvalidateMap();
            EditorState.HighlightIceGrowthChanged += (s, e) => InvalidateMap();
            EditorState.DrawMapWideOverlayChanged += (s, e) => MapWideOverlay.Enabled = EditorState.DrawMapWideOverlay;
            EditorState.MarbleMadnessChanged += (s, e) => InvalidateMapForMinimap();
            EditorState.Is2DModeChanged += (s, e) => InvalidateMapForMinimap();
            EditorState.IsLightingChanged += (s, e) => LightingChanged();
            EditorState.LightingPreviewStateChanged += (s, e) => LightingChanged();
            EditorState.RenderedObjectsChanged += (s, e) => InvalidateMapForMinimap();

            refreshStopwatch = new Stopwatch();

            InitRenderers();

            InvalidateMapForMinimap();
            Map_LightingColorsRefreshed();
        }

        public void Clear()
        {
            EditorState = null;
            TheaterGraphics = null;
            MapWideOverlay.Clear();

            depthRenderStencilState?.Dispose();
            shadowRenderStencilState?.Dispose();
            Map = null;

            ClearRenderTargets();
        }

        private void LoadShaders()
        {
            palettedTerrainDrawEffect = AssetLoader.LoadEffect("Shaders/PalettedTerrainDraw");
            palettedColorDrawEffect = AssetLoader.LoadEffect("Shaders/PalettedColorDraw");
            alphaMapDrawEffect = AssetLoader.LoadEffect("Shaders/AlphaMapApply");
            alphaImageToAlphaMapEffect = AssetLoader.LoadEffect("Shaders/AlphaImageToAlphaMap");
        }

        private void Map_CellLightingModified(object sender, CellLightingEventArgs e)
        {
            if (EditorState.IsLighting && EditorState.LightingPreviewState != LightingPreviewMode.NoLighting)
                Map.RefreshCellLighting(EditorState.LightingPreviewState, EditorState.LightDisabledLightSources, e.AffectedTiles);
        }

        private void LightingChanged()
        {
            Map.RefreshCellLighting(EditorState.IsLighting ? EditorState.LightingPreviewState : LightingPreviewMode.NoLighting, EditorState.LightDisabledLightSources, null);

            InvalidateMapForMinimap();
            if (Constants.VoxelsAffectedByLighting)
                TheaterGraphics.InvalidateVoxelCache();
        }

        private void Map_LightingColorsRefreshed()
        {
            MapColor? color = EditorState.LightingPreviewState switch
            {
                LightingPreviewMode.Normal => Map.Lighting.NormalColor,
                LightingPreviewMode.IonStorm => Map.Lighting.IonColor,
                LightingPreviewMode.Dominator => Map.Lighting.DominatorColor,
                _ => null,
            };

            if (color != null)
                TheaterGraphics.ApplyLightingToPalettes((MapColor)color);

            LightingChanged();
        }

        private void ClearRenderTargets()
        {
            mapRenderTarget?.Dispose();
            transparencyRenderTarget?.Dispose();
            transparencyPerFrameRenderTarget?.Dispose();
            compositeRenderTarget?.Dispose();
            compositeRenderTargetCopy?.Dispose();
            alphaRenderTarget?.Dispose();
            minimapRenderTarget?.Dispose();
        }

        public void RefreshRenderTargets()
        {
            ClearRenderTargets();

            mapRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
            transparencyRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color);
            transparencyPerFrameRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color);
            compositeRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color, DepthFormat.Depth24);
            compositeRenderTargetCopy = CreateFullMapRenderTarget(SurfaceFormat.Color);
            alphaRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Alpha8);
            minimapRenderTarget = CreateFullMapRenderTarget(SurfaceFormat.Color);

            Constants.DepthRenderStep = (float)Constants.CellSizeY / mapRenderTarget.Height;
        }

        private void CreateDepthStencilStates()
        {
            if (depthRenderStencilState == null)
            {
                depthRenderStencilState = new DepthStencilState()
                {
                    DepthBufferEnable = true,
                    DepthBufferWriteEnable = true,
                    DepthBufferFunction = CompareFunction.GreaterEqual,
                };
            }

            if (depthReadStencilState == null)
            {
                depthReadStencilState = new DepthStencilState()
                {
                    DepthBufferEnable = true,
                    DepthBufferWriteEnable = false,
                    DepthBufferFunction = CompareFunction.GreaterEqual,
                };
            }

            // Depth stencil state for rendering objects.
            // Sets the stencil value in the stencil buffer to prevent shadows from being drawn over objects.
            // While it'd usually look nicer, shadows cannot be cast over objects in the C&C engine.
            if (objectRenderStencilState == null)
            {
                objectRenderStencilState = new DepthStencilState()
                {
                    DepthBufferEnable = true,
                    DepthBufferWriteEnable = true,
                    DepthBufferFunction = CompareFunction.GreaterEqual,
                    StencilEnable = true,
                    StencilPass = StencilOperation.Replace,
                    StencilFunction = CompareFunction.Always,
                    ReferenceStencil = 1
                };
            }

            if (shadowRenderStencilState == null)
            {
                shadowRenderStencilState = new DepthStencilState()
                {
                    DepthBufferEnable = true,
                    DepthBufferWriteEnable = true,
                    DepthBufferFunction = CompareFunction.Greater,
                    StencilEnable = true,
                    StencilFail = StencilOperation.Keep,
                    StencilPass = StencilOperation.Replace,
                    StencilFunction = CompareFunction.Greater,
                    ReferenceStencil = 1
                };
            }
        }

        private void InitBatchers()
        {
            terrainBatcher = new TerrainBatcher(GraphicsDevice, palettedTerrainDrawEffect, depthRenderStencilState);
            gameObjectBatcher = new GameObjectBatcher(GraphicsDevice, palettedColorDrawEffect);
        }

        private RenderDependencies CreateRenderDependencies()
        {
            return new RenderDependencies(Map, TheaterGraphics, EditorState, windowManager.GraphicsDevice, objectSpriteRecord, palettedColorDrawEffect, Camera, GetCameraRightXCoord, GetCameraBottomYCoord);
        }

        private void InitRenderers()
        {
            aircraftRenderer = new AircraftRenderer(CreateRenderDependencies());
            animRenderer = new AnimRenderer(CreateRenderDependencies());
            buildingRenderer = new BuildingRenderer(CreateRenderDependencies());
            infantryRenderer = new InfantryRenderer(CreateRenderDependencies());
            overlayRenderer = new OverlayRenderer(CreateRenderDependencies());
            smudgeRenderer = new SmudgeRenderer(CreateRenderDependencies());
            terrainRenderer = new TerrainRenderer(CreateRenderDependencies());
            unitRenderer = new UnitRenderer(CreateRenderDependencies());
        }

        private RenderTarget2D CreateFullMapRenderTarget(SurfaceFormat surfaceFormat, DepthFormat depthFormat = DepthFormat.None)
        {
           return new RenderTarget2D(GraphicsDevice,
               Map.WidthInPixels,
               Map.HeightInPixels + (Constants.CellHeight * Constants.MaxMapHeightLevel), false, surfaceFormat,
               depthFormat, 0, RenderTargetUsage.PreserveContents);
        }

        public void DrawVisibleMapPortion()
        {
            refreshStopwatch.Restart();

            smudgesToRender.Clear();
            flatOverlaysToRender.Clear();
            structuresToRender.Clear();
            gameObjectsToRender.Clear();
            alphaImagesToRender.Clear();

            Renderer.PushRenderTarget(mapRenderTarget);

            if (mapInvalidated)
            {
                GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Black, 0f, 0);
                objectSpriteRecord.Clear(false);
            }

            // Draw terrain tiles in batched mode for performance if we can.
            // In Marble Madness mode we currently need to mix and match paletted and non-paletted graphics, so there's no avoiding immediate mode.
            SetTerrainEffectParams(TheaterGraphics.TheaterPalette.GetTexture());

            terrainBatcher.Begin(null, depthRenderStencilState);
            DoForVisibleCells(DrawTerrainTileAndRegisterObjects);
            terrainBatcher.End();

            // We do not need to write to the depth render target when drawing smudges and flat overlays.
            // Swap to using only the main map render target.
            // At this point of drawing, depth testing is done on depth buffer embedded in the main map render target.
            Renderer.PopRenderTarget();

            var palettedTerrainDrawSettings = new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.Opaque, null, depthReadStencilState, null, palettedTerrainDrawEffect);
            Renderer.PushRenderTarget(mapRenderTarget, palettedTerrainDrawSettings);

            // Smudges can be drawn as part of regular terrain.
            // Afterwards, we need to switch to the more complex shader, so we pop rendering settings.
            DrawSmudges();
            Renderer.PopSettings();

            // Flat overlays can also be drawn as part of regular terrain, but they need to use the more complex shader.
            SetPaletteEffectParams(palettedColorDrawEffect, TheaterGraphics.TheaterPalette.GetTexture(), true, false, false);
            var palettedColorDrawSettings = new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.Opaque, null, depthReadStencilState, null, palettedColorDrawEffect);
            Renderer.PushSettings(palettedColorDrawSettings);
            DrawFlatOverlays();

            Renderer.PopRenderTarget();

            // Render non-flat objects
            Renderer.PushRenderTarget(mapRenderTarget, new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, objectRenderStencilState, null, palettedColorDrawEffect));

            DrawBuildings();
            DrawGameObjects();

            // Then draw on-map UI elements
            DrawMapUIElements();

            Renderer.PopRenderTarget();

            refreshStopwatch.Stop();
            Console.WriteLine("Map render time: " + refreshStopwatch.Elapsed.TotalMilliseconds);
        }

        private void DrawMapUIElements()
        {
            Renderer.PushRenderTarget(transparencyRenderTarget, new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null));
            GraphicsDevice.Clear(Color.Transparent);

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.BaseNodes) == RenderObjectFlags.BaseNodes)
                DrawBaseNodes();

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.CellTags) == RenderObjectFlags.CellTags)
                DrawCellTags();

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Waypoints) == RenderObjectFlags.Waypoints)
                DrawWaypoints();

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.TunnelTubes) == RenderObjectFlags.TunnelTubes)
                DrawTubes();

            if (EditorState.HighlightImpassableCells)
            {
                Map.DoForAllValidTiles(DrawImpassableHighlight);
            }

            if (EditorState.HighlightIceGrowth)
            {
                Map.DoForAllValidTiles(DrawIceGrowthHighlight);
            }

            Renderer.PopRenderTarget();
        }

        private void SetPaletteEffectParams(Effect effect, Texture2D paletteTexture, bool usePalette, bool useRemap, bool isShadow = false)
        {
            if (paletteTexture != null)
            {
                effect.Parameters["PaletteTexture"].SetValue(paletteTexture);
            }

            effect.Parameters["IsShadow"].SetValue(isShadow);
            effect.Parameters["UsePalette"].SetValue(usePalette);
            effect.Parameters["UseRemap"].SetValue(useRemap);
        }

        private void SetTerrainEffectParams(Texture2D paletteTexture)
        {
            palettedTerrainDrawEffect.Parameters["PaletteTexture"].SetValue(paletteTexture);
        }

        private void DoForVisibleCells(Action<MapTile> action)
        {
            int tlX;
            int tlY;
            int camRight;
            int camBottom;

            if (minimapNeedsRefresh && MinimapUsers.Count > 0)
            {
                // If the minimap needs a full refresh, then we need to re-render the whole map
                tlX = 0;
                tlY = -Constants.MapYBaseline;
                camRight = mapRenderTarget.Width;
                camBottom = mapRenderTarget.Height;
            }
            else
            {
                // Otherwise, screen contents will do.
                // Add some padding to take objects just outside of the visible screen to account
                tlX = Camera.TopLeftPoint.X - Constants.RenderPixelPadding;
                tlY = Camera.TopLeftPoint.Y - Constants.RenderPixelPadding - Constants.MapYBaseline;

                if (tlX < 0)
                    tlX = 0;

                if (tlY < 0)
                    tlY = 0;

                camRight = GetCameraRightXCoord() + Constants.RenderPixelPadding;
                camBottom = GetCameraBottomYCoord() + Constants.RenderPixelPadding;
            }

            Point2D firstVisibleCellCoords = CellMath.CellCoordsFromPixelCoords_2D(new Point2D(tlX, tlY), Map);

            int xCellCount = (camRight - tlX) / Constants.CellSizeX;
            xCellCount += 2; // Add some padding for edge cases

            int yCellCount = (camBottom - tlY) / Constants.CellSizeY;

            // Add some padding to take height levels into account
            const int yPadding = 8;
            yCellCount += yPadding;

            for (int offset = 0; offset < yCellCount; offset++)
            {
                int x = firstVisibleCellCoords.X + offset;
                int y = firstVisibleCellCoords.Y + offset;

                // Draw two horizontal rows of the map

                for (int sx = 0; sx < xCellCount; sx++)
                {
                    int coordX = x + sx;
                    int coordY = y - sx;

                    var cell = Map.GetTile(coordX, coordY);

                    if (cell != null)
                        action(cell);
                }

                for (int sx = 0; sx < xCellCount; sx++)
                {
                    int coordX = x + 1 + sx;
                    int coordY = y - sx;

                    var cell = Map.GetTile(coordX, coordY);

                    if (cell != null)
                        action(cell);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCameraWidth() => (int)(Width / (float)Camera.ZoomLevel);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCameraHeight() => (int)(Height / (float)Camera.ZoomLevel);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCameraRightXCoord() => Math.Min(Camera.TopLeftPoint.X + GetCameraWidth(), Map.Size.X * Constants.CellSizeX);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCameraBottomYCoord() => Math.Min(Camera.TopLeftPoint.Y + GetCameraHeight(), Map.Size.Y * Constants.CellSizeY + Constants.MapYBaseline);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Rectangle GetCameraRectangle() => new Rectangle(Camera.TopLeftPoint.X, Camera.TopLeftPoint.Y, GetCameraWidth(), GetCameraHeight());

        public void DrawTerrainTileAndRegisterObjects(MapTile tile)
        {
            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Terrain) == RenderObjectFlags.Terrain)
                DrawTerrainTile(tile);

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Smudges) == RenderObjectFlags.Smudges && tile.Smudge != null)
                smudgesToRender.Add(tile.Smudge);

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Overlay) == RenderObjectFlags.Overlay && tile.Overlay != null && tile.Overlay.OverlayType != null)
            {
                if (tile.Overlay.OverlayType.DrawFlat && tile.Overlay.OverlayType.HighBridgeDirection == BridgeDirection.None && !tile.Overlay.OverlayType.Wall)
                    AddFlatOverlayToRender(tile.Overlay);
                else
                    AddGameObjectToRender(tile.Overlay);
            }

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Structures) == RenderObjectFlags.Structures)
            {
                // Do not use tile.DoForAllBuildings here due to lambdas being expensive due to memory allocation + function calls
                for (int i = 0; i < tile.Structures.Count; i++)
                {
                    var structure = tile.Structures[i];

                    if (structure.Position == tile.CoordsToPoint())
                    {
                        AddStructureToRender(structure);

                        if (structure.ObjectType.AlphaShape != null && IsRenderFlagEnabled(RenderObjectFlags.AlphaLights))
                            alphaImagesToRender.Add(new AlphaImageRenderStruct(structure.Position, structure.ObjectType.AlphaShape, structure));
                    }
                }
            }

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Infantry) == RenderObjectFlags.Infantry)
            {
                for (int i = 0; i < tile.Infantry.Length; i++)
                {
                    if (tile.Infantry[i] != null)
                        AddGameObjectToRender(tile.Infantry[i]);
                }
            }

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Aircraft) == RenderObjectFlags.Aircraft)
            {
                for (int i = 0; i < tile.Aircraft.Count; i++)
                {
                    AddGameObjectToRender(tile.Aircraft[i]);
                }
            }

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.Vehicles) == RenderObjectFlags.Vehicles)
            {
                for (int i = 0; i < tile.Vehicles.Count; i++)
                {
                    AddGameObjectToRender(tile.Vehicles[i]);
                }
            }

            if ((EditorState.RenderObjectFlags & RenderObjectFlags.TerrainObjects) == RenderObjectFlags.TerrainObjects && tile.TerrainObject != null)
            {
                AddGameObjectToRender(tile.TerrainObject);

                if (tile.TerrainObject.TerrainType.AlphaShape != null && IsRenderFlagEnabled(RenderObjectFlags.AlphaLights))
                    alphaImagesToRender.Add(new AlphaImageRenderStruct(tile.TerrainObject.Position, tile.TerrainObject.TerrainType.AlphaShape, tile.TerrainObject));
            }
        }

        private bool IsRenderFlagEnabled(RenderObjectFlags flag)
        {
            return (EditorState.RenderObjectFlags & flag) == flag;
        }

        private void AddStructureToRender(Structure structure)
        {
            if (objectSpriteRecord.ProcessedObjects.Contains(structure))
                return;

            structuresToRender.Add(structure);
        }

        private void AddFlatOverlayToRender(Overlay overlay)
        {
            if (objectSpriteRecord.ProcessedObjects.Contains(overlay))
                return;

            flatOverlaysToRender.Add(overlay);
        }

        private void AddGameObjectToRender(GameObject gameObject)
        {
            if (objectSpriteRecord.ProcessedObjects.Contains(gameObject))
                return;

            gameObjectsToRender.Add(gameObject);
        }

        public void DrawTerrainTile(MapTile tile)
        {
            if (tile.LastRefreshIndex == refreshIndex)
                return;

            tile.LastRefreshIndex = refreshIndex;

            if (tile.TileIndex >= TheaterGraphics.TileCount)
                return;

            Point2D drawPointWithoutCellHeight = CellMath.CellTopLeftPointFromCellCoords(new Point2D(tile.X, tile.Y), Map);

            if (tile.TileImage == null)
            {
                var theater = TheaterGraphics.Theater;

                // Hardcode variant 0 for bridges and train bridges so they don't appear damaged
                // Ideally we'd need to check HasDamagedData in the subcell's TmpImage, but that
                // would be very messy..
                if (theater.BridgeTileSet.ContainsTile(tile.TileIndex) ||
                    (theater.TrainBridgeTileSet != null && theater.TrainBridgeTileSet.ContainsTile(tile.TileIndex)) ||
                    (theater.WoodBridgeTileSet != null && theater.WoodBridgeTileSet.ContainsTile(tile.TileIndex)))
                {
                    tile.TileImage = TheaterGraphics.GetTileGraphics(tile.TileIndex, 0);
                }
                else
                {
                    tile.TileImage = TheaterGraphics.GetTileGraphics(tile.TileIndex);
                }
            }

            TileImage tileImage;
            int subTileIndex;
            int level;
            if (tile.PreviewTileImage != null)
            {
                tileImage = tile.PreviewTileImage;
                subTileIndex = tile.PreviewSubTileIndex;
                level = tile.PreviewLevel;
            }
            else
            {
                tileImage = tile.TileImage;
                subTileIndex = tile.SubTileIndex;
                level = tile.Level;
            }

            // Framework Mode / Marble Madness support
            if (EditorState.IsMarbleMadness)
                tileImage = TheaterGraphics.GetMarbleMadnessTileGraphics(tileImage.TileID);

            int drawX = drawPointWithoutCellHeight.X;
            int drawY = drawPointWithoutCellHeight.Y;

            if (subTileIndex >= tileImage.TMPImages.Length)
            {
                // Renderer.DrawString(subTileIndex.ToString(), 0, new Vector2(drawPoint.X, drawPoint.Y), Color.Red);
                return;
            }

            MGTMPImage tmpImage = tileImage.TMPImages[subTileIndex];

            if (tmpImage == null || tmpImage.Texture == null)
            {
                // Renderer.DrawString(subTileIndex.ToString(), 0, new Vector2(drawPoint.X, drawPoint.Y), Color.Red);
                return;
            }

            float depthTop = CellMath.GetDepthForPixel(drawPointWithoutCellHeight.Y, drawPointWithoutCellHeight.Y, tile, Map);
            float depthBottom = CellMath.GetDepthForPixel(drawPointWithoutCellHeight.Y + Constants.CellSizeY, drawPointWithoutCellHeight.Y, tile, Map);

            if (!EditorState.Is2DMode)
                drawY -= Constants.CellHeight * level;

            // Divide the color by 2f. This is done because unlike map lighting which can exceed 1.0 and go up to 2.0,
            // the Color instance values are capped at 1.0.
            // We lose a bit of precision from doing this, but we'll have to accept that.
            // Alpha component is irrelevant as long as it's not >= 1.0f (if it is, shader uses marble madness mode code)
            Color color = new Color((float)tile.CellLighting.R / 2f, (float)tile.CellLighting.G / 2f, (float)tile.CellLighting.B / 2f, 0.0f);

            Texture2D textureToDraw = tmpImage.Texture;
            Rectangle sourceRectangle = tmpImage.SourceRectangle;

            // Replace terrain lacking MM graphics with colored cells to denote height if we are in marble madness mode
            if (EditorState.IsMarbleMadness && !Constants.IsFlatWorld)
            {
                if (!TheaterGraphics.HasSeparateMarbleMadnessTileGraphics(tileImage.TileID))
                {
                    textureToDraw = EditorGraphics.GenericTileWithBorderTexture;
                    sourceRectangle = new Rectangle(0, 0, textureToDraw.Width, textureToDraw.Height);
                    color = MarbleMadnessTileHeightLevelColors[level];
                }
            }

            terrainBatcher.Draw(textureToDraw, 
                new Rectangle(drawX, drawY, Constants.CellSizeX, Constants.CellSizeY),
                sourceRectangle, color, depthTop, depthBottom);

            if (tmpImage.TmpImage.HasExtraData())
            {
                drawX = drawX + tmpImage.TmpImage.XExtra - tmpImage.TmpImage.X;
                drawY = drawPointWithoutCellHeight.Y + tmpImage.TmpImage.YExtra - tmpImage.TmpImage.Y;

                depthTop = CellMath.GetDepthForPixel(drawY, drawPointWithoutCellHeight.Y, tile, Map);
                depthBottom = CellMath.GetDepthForPixel(drawY + tmpImage.ExtraSourceRectangle.Height, drawPointWithoutCellHeight.Y, tile, Map);

                if (!EditorState.Is2DMode)
                    drawY -= Constants.CellHeight * level;

                var exDrawRectangle = new Rectangle(drawX, drawY,
                    tmpImage.ExtraSourceRectangle.Width,
                    tmpImage.ExtraSourceRectangle.Height);

                terrainBatcher.Draw(tmpImage.Texture, exDrawRectangle, tmpImage.ExtraSourceRectangle, color, depthTop, depthBottom);
            }
        }

        /// <summary>
        /// Draws all waypoints visible on the screen, utilizing batching as much as possible.
        /// </summary>
        private void DrawWaypoints()
        {
            waypointsToRender.Clear();

            // Instead of drawing one waypoint at a time, we draw the same-texture element of
            // all waypoints at once, and iterate through the waypoints multiple times.
            // While it seems heavier, this approach allows MonoGame's SpriteBatch to batch
            // the draw calls, making the process much lighter in practice.

            // Gather waypoints to draw
            for (int i = 0; i < Map.Waypoints.Count; i++)
            {
                var waypoint = Map.Waypoints[i];

                Point2D drawPoint = CellMath.CellTopLeftPointFromCellCoords(waypoint.Position, Map);

                var cell = Map.GetTile(waypoint.Position);
                if (cell != null && !EditorState.Is2DMode)
                    drawPoint -= new Point2D(0, cell.Level * Constants.CellHeight);

                if (MinimapUsers.Count == 0 &&
                    (Camera.TopLeftPoint.X > drawPoint.X + EditorGraphics.TileBorderTexture.Width ||
                    Camera.TopLeftPoint.Y > drawPoint.Y + EditorGraphics.TileBorderTexture.Height ||
                    GetCameraRightXCoord() < drawPoint.X ||
                    GetCameraBottomYCoord() < drawPoint.Y))
                {
                    // This waypoint is outside the camera
                    continue;
                }

                Color waypointColor = string.IsNullOrEmpty(waypoint.EditorColor) ? Color.Fuchsia : waypoint.XNAColor;
                var drawRectangle = new Rectangle(drawPoint.X, drawPoint.Y, EditorGraphics.GenericTileTexture.Width, EditorGraphics.GenericTileTexture.Height);

                waypointsToRender.Add(new WaypointDrawStruct(waypoint, waypointColor, drawRectangle));
            }

            // Draw darkened background for all waypoints
            for (int i = 0; i < waypointsToRender.Count; i++)
            {
                var waypoint = waypointsToRender[i];

                Renderer.DrawTexture(EditorGraphics.GenericTileTexture, waypoint.DrawRectangle, new Color(0, 0, 0, 128));
            }

            // Draw tile border for all waypoints
            for (int i = 0; i < waypointsToRender.Count; i++)
            {
                var waypoint = waypointsToRender[i];

                Renderer.DrawTexture(EditorGraphics.TileBorderTexture, waypoint.DrawRectangle, waypoint.Color);
            }

            // Draw text for all waypoints
            for (int i = 0; i < waypointsToRender.Count; i++)
            {
                int fontIndex = Constants.UIBoldFont;
                string waypointIdentifier = waypointsToRender[i].Waypoint.Identifier.ToString();
                var textDimensions = Renderer.GetTextDimensions(waypointIdentifier, fontIndex);
                Renderer.DrawStringWithShadow(waypointIdentifier, fontIndex,
                    new Vector2(waypointsToRender[i].DrawRectangle.X + ((Constants.CellSizeX - textDimensions.X) / 2),
                    waypointsToRender[i].DrawRectangle.Y + ((Constants.CellSizeY - textDimensions.Y) / 2)),
                    waypointsToRender[i].Color);
            }
        }

        private void DrawWaypoint(Waypoint waypoint)
        {
            Point2D drawPoint = CellMath.CellTopLeftPointFromCellCoords(waypoint.Position, Map);

            var cell = Map.GetTile(waypoint.Position);
            if (cell != null && !EditorState.Is2DMode)
                drawPoint -= new Point2D(0, cell.Level * Constants.CellHeight);

            if (MinimapUsers.Count == 0 &&
                (Camera.TopLeftPoint.X > drawPoint.X + EditorGraphics.TileBorderTexture.Width ||
                Camera.TopLeftPoint.Y > drawPoint.Y + EditorGraphics.TileBorderTexture.Height ||
                GetCameraRightXCoord() < drawPoint.X ||
                GetCameraBottomYCoord() < drawPoint.Y))
            {
                // This waypoint is outside the camera
                return;
            }

            Color waypointColor = string.IsNullOrEmpty(waypoint.EditorColor) ? Color.Fuchsia : waypoint.XNAColor;
            var drawRectangle = new Rectangle(drawPoint.X, drawPoint.Y, EditorGraphics.GenericTileTexture.Width, EditorGraphics.GenericTileTexture.Height);

            Renderer.DrawTexture(EditorGraphics.GenericTileTexture, drawRectangle, new Color(0, 0, 0, 128));
            Renderer.DrawTexture(EditorGraphics.TileBorderTexture, drawRectangle, waypointColor);

            int fontIndex = Constants.UIBoldFont;
            string waypointIdentifier = waypoint.Identifier.ToString();
            var textDimensions = Renderer.GetTextDimensions(waypointIdentifier, fontIndex);
            Renderer.DrawStringWithShadow(waypointIdentifier,
                fontIndex,
                new Vector2(drawPoint.X + ((Constants.CellSizeX - textDimensions.X) / 2), drawPoint.Y + ((Constants.CellSizeY - textDimensions.Y) / 2)),
                waypointColor);
        }

        private void DrawCellTags()
        {
            DoForVisibleCells(t =>
            {
                if (t.CellTag != null)
                    DrawCellTag(t.CellTag);
            });
        }

        private int CompareGameObjectsForRendering(GameObject obj1, GameObject obj2)
        {
            // Use pixel coords for sorting. Objects closer to the top are rendered first.
            // In case of identical Y coordinates, objects closer to the left take priority.
            // For buildings, we take their foundation into account when calculating their center pixel coords.

            // In case the pixels coords are identical, sort by RTTI type.
            Point2D obj1Point = GetObjectCoordsForComparison(obj1);
            Point2D obj2Point = GetObjectCoordsForComparison(obj2);

            int result = obj1Point.Y.CompareTo(obj2Point.Y);
            if (result != 0)
                return result;

            result = obj1Point.X.CompareTo(obj2Point.X);
            if (result != 0)
                return result;

            return ((int)obj1.WhatAmI()).CompareTo((int)obj2.WhatAmI());
        }

        private Point2D GetObjectCoordsForComparison(GameObject obj)
        {
            return obj.WhatAmI() switch
            {
                RTTIType.Building => buildingRenderer.GetBuildingCenterPoint((Structure)obj),
                RTTIType.Anim => ((Animation)obj).IsBuildingAnim ?
                    buildingRenderer.GetBuildingCenterPoint(((Animation)obj).ParentBuilding) :
                    CellMath.CellCenterPointFromCellCoords(obj.Position, Map),
                _ => CellMath.CellCenterPointFromCellCoords(obj.Position, Map)
            };
        }

        /// <summary>
        /// Draws smudges.
        /// Smudges are the "bottom-most" layer after terrain tiles and cannot ever overlap
        /// other objects, making them convenient to render separately from others.
        /// </summary>
        private void DrawSmudges()
        {
            smudgesToRender.Sort(CompareGameObjectsForRendering);

            for (int i = 0; i < smudgesToRender.Count; i++)
            {
                smudgeRenderer.DrawNonRemap(smudgesToRender[i], smudgeRenderer.GetDrawPoint(smudgesToRender[i]));
            }
            smudgesToRender.ForEach(DrawObject);
        }

        private void DrawFlatOverlays()
        {
            flatOverlaysToRender.Sort(CompareGameObjectsForRendering);
            for (int i = 0; i < flatOverlaysToRender.Count; i++)
            {
                DrawObject(flatOverlaysToRender[i]);
                objectSpriteRecord.ProcessedObjects.Add(flatOverlaysToRender[i]);
            }

            ProcessObjectSpriteRecord(true, false, true); // Flat overlays do not render to the depth buffer
            objectSpriteRecord.Clear(true);
        }

        /// <summary>
        /// Draws buildings. Due to their large size and non-flat shape in the game world,
        /// buildings are rendered with different shader settings from other objects and
        /// thus need to be drawn separately.
        /// </summary>
        private void DrawBuildings()
        {
            structuresToRender.Sort(CompareGameObjectsForRendering);
            for (int i = 0; i < structuresToRender.Count; i++)
            {
                DrawObject(structuresToRender[i]);
                objectSpriteRecord.ProcessedObjects.Add(structuresToRender[i]);
            }

            ProcessObjectSpriteRecord(false, false, false); // Do not process building shadows yet, let DrawGameObjects do it
            objectSpriteRecord.Clear(true);
        }

        /// <summary>
        /// Draws all game objects that have been queued for rendering.
        /// </summary>
        private void DrawGameObjects()
        {
            gameObjectsToRender.Sort(CompareGameObjectsForRendering);

            for (int i = 0; i < gameObjectsToRender.Count; i++)
            {
                DrawObject(gameObjectsToRender[i]);
                objectSpriteRecord.ProcessedObjects.Add(gameObjectsToRender[i]);
            }

            ProcessObjectSpriteRecord(false, true, false);
            objectSpriteRecord.Clear(false);
        }

        private void DrawObject(GameObject gameObject)
        {
            if (!EditorState.RenderInvisibleInGameObjects && gameObject.IsInvisibleInGame())
                return;

            switch (gameObject.WhatAmI())
            {
                case RTTIType.Aircraft:
                    aircraftRenderer.Draw(gameObject as Aircraft, false);
                    return;
                case RTTIType.Anim:
                    animRenderer.Draw(gameObject as Animation, false);
                    return;
                case RTTIType.Building:
                    buildingRenderer.Draw(gameObject as Structure, false);
                    return;
                case RTTIType.Infantry:
                    infantryRenderer.Draw(gameObject as Infantry, false);
                    return;
                case RTTIType.Overlay:
                    overlayRenderer.Draw(gameObject as Overlay, false);
                    return;
                case RTTIType.Smudge:
                    smudgeRenderer.Draw(gameObject as Smudge, false);
                    return;
                case RTTIType.Terrain:
                    terrainRenderer.Draw(gameObject as TerrainObject, false);
                    return;
                case RTTIType.Unit:
                    unitRenderer.Draw(gameObject as Unit, false);
                    return;
                default:
                    throw new NotImplementedException("No renderer implemented for type " + gameObject.WhatAmI());
            }
        }

        private void ProcessObjectSpriteRecord(bool noDepthWriting, bool processShadows, bool alphaBlendNonPalettedSprites)
        {
            if (objectSpriteRecord.LineEntries.Count > 0)
            {
                SetPaletteEffectParams(palettedColorDrawEffect, null, false, false, false);
                Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, noDepthWriting ? depthReadStencilState : objectRenderStencilState, null, palettedColorDrawEffect));

                for (int i = 0; i < objectSpriteRecord.LineEntries.Count; i++)
                {
                    var lineEntry = objectSpriteRecord.LineEntries[i];
                    Renderer.DrawLine(lineEntry.Source, lineEntry.Destination,
                        lineEntry.Color, lineEntry.Thickness, lineEntry.Depth);
                }

                Renderer.PopSettings();
            }

            foreach (var kvp in objectSpriteRecord.SpriteEntries)
            {
                if (kvp.Value.Count == 0)
                    continue;

                Texture2D paletteTexture = kvp.Key.Item1;
                bool isRemap = kvp.Key.Item2;

                SetPaletteEffectParams(palettedColorDrawEffect, paletteTexture, true, isRemap, false);
                gameObjectBatcher.Begin(palettedColorDrawEffect, noDepthWriting ? depthReadStencilState : objectRenderStencilState);

                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    var spriteEntry = kvp.Value[i];

                    gameObjectBatcher.Draw(spriteEntry.Texture, spriteEntry.DrawingBounds, spriteEntry.SourceRectangle, spriteEntry.Color, spriteEntry.DepthRectangle);
                }

                gameObjectBatcher.End();
            }

            if (objectSpriteRecord.NonPalettedSpriteEntries.Count > 0)
            {
                SetPaletteEffectParams(palettedColorDrawEffect, null, false, false, false);
                gameObjectBatcher.Begin(palettedColorDrawEffect, noDepthWriting ? depthReadStencilState : objectRenderStencilState);

                for (int i = 0; i < objectSpriteRecord.NonPalettedSpriteEntries.Count; i++)
                {
                    var spriteEntry = objectSpriteRecord.NonPalettedSpriteEntries[i];
                    gameObjectBatcher.Draw(spriteEntry.Texture, spriteEntry.DrawingBounds, spriteEntry.SourceRectangle, spriteEntry.Color, spriteEntry.DepthRectangle);
                }

                gameObjectBatcher.End();
            }

            if (processShadows && objectSpriteRecord.ShadowEntries.Count > 0)
            {
                SetPaletteEffectParams(palettedColorDrawEffect, null, false, false, true);
                gameObjectBatcher.Begin(palettedColorDrawEffect, shadowRenderStencilState);
                // GraphicsDevice.BlendState = BlendState.AlphaBlend;

                for (int i = 0; i < objectSpriteRecord.ShadowEntries.Count; i++)
                {
                    var spriteEntry = objectSpriteRecord.ShadowEntries[i];

                    // It doesn't really matter what we give as color to the shadow
                    gameObjectBatcher.Draw(spriteEntry.Texture, spriteEntry.DrawingBounds, spriteEntry.SourceRectangle, Color.White,
                        spriteEntry.DepthRectangle);
                }

                gameObjectBatcher.End();
            }

            if (objectSpriteRecord.TextEntries.Count > 0)
            {
                SetPaletteEffectParams(palettedColorDrawEffect, null, false, false, false);
                Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, depthRenderStencilState, null, palettedColorDrawEffect));

                for (int i = 0; i < objectSpriteRecord.TextEntries.Count; i++)
                {
                    var textEntry = objectSpriteRecord.TextEntries[i];
                    Renderer.DrawStringWithShadow(textEntry.Text, Constants.UIBoldFont, textEntry.DrawPoint.ToXNAVector(), textEntry.Color, 1f, 1f, 1f);
                }

                Renderer.PopSettings();
            }
        }

        private void DrawBaseNodes()
        {
            if (Map.GraphicalBaseNodes.Count > 0)
            {
                foreach (var baseNode in Map.GraphicalBaseNodes)
                {
                    RecordBaseNode(baseNode);
                }

                ProcessObjectSpriteRecord(false, false, false);
                objectSpriteRecord.Clear(false);
            }
        }

        private void RecordBaseNode(GraphicalBaseNode graphicalBaseNode)
        {
            // TODO add base nodes to the regular rendering code

            int baseNodeIndex = graphicalBaseNode.Owner.BaseNodes.FindIndex(bn => bn == graphicalBaseNode.BaseNode);
            Color baseNodeIndexColor = Color.White * 0.7f;

            Point2D drawPoint = CellMath.CellTopLeftPointFromCellCoords_3D(graphicalBaseNode.BaseNode.Position, Map);

            // Base nodes can be large, let's increase the level of padding for them.
            int padding = Constants.RenderPixelPadding * 2;
            if (MinimapUsers.Count == 0 &&
                (Camera.TopLeftPoint.X > drawPoint.X + padding || Camera.TopLeftPoint.Y > drawPoint.Y + padding ||
                GetCameraRightXCoord() < drawPoint.X - padding || GetCameraBottomYCoord() < drawPoint.Y - padding))
            {
                return;
            }

            const float opacity = 0.5f;

            ShapeImage bibGraphics = TheaterGraphics.BuildingBibTextures[graphicalBaseNode.BuildingType.Index];
            ShapeImage graphics = TheaterGraphics.BuildingTextures[graphicalBaseNode.BuildingType.Index];
            Color replacementColor = Color.DarkBlue;
            string iniName = graphicalBaseNode.BuildingType.ININame;
            Color remapColor;
            Color nonRemapColor = new Color(128, 128, 128, 255);

            if (!graphicalBaseNode.BuildingType.ArtConfig.Remapable)
                remapColor = nonRemapColor;
            else
                remapColor = new Color((byte)graphicalBaseNode.Owner.XNAColor.R, (byte)graphicalBaseNode.Owner.XNAColor.G, (byte)graphicalBaseNode.Owner.XNAColor.B, (byte)255) * opacity;

            int yDrawOffset = Constants.CellSizeY / -2;
            int frameIndex = 0;

            if ((graphics == null || graphics.GetFrame(frameIndex) == null) && (bibGraphics == null || bibGraphics.GetFrame(0) == null))
            {
                objectSpriteRecord.AddTextEntry(new TextEntry(iniName, replacementColor, drawPoint));
                objectSpriteRecord.AddTextEntry(new TextEntry("# " + baseNodeIndex, baseNodeIndexColor, drawPoint + new Point2D(0, 20)));
                return;
            }

            var cell = Map.GetTile(graphicalBaseNode.BaseNode.Position);
            var lighting = cell == null ? Vector4.One : cell.CellLighting.ToXNAVector4Ambient();

            Texture2D texture;

            if (bibGraphics != null)
            {
                PositionedTexture bibFrame = bibGraphics.GetFrame(0);

                if (bibFrame != null && bibFrame.Texture != null)
                {
                    texture = bibFrame.Texture;

                    int bibFinalDrawPointX = drawPoint.X - bibFrame.ShapeWidth / 2 + bibFrame.OffsetX + Constants.CellSizeX / 2;
                    int bibFinalDrawPointY = drawPoint.Y - bibFrame.ShapeHeight / 2 + bibFrame.OffsetY + Constants.CellSizeY / 2 + yDrawOffset;

                    objectSpriteRecord.AddGraphicsEntry(new ObjectSpriteEntry(bibGraphics.GetPaletteTexture(), bibFrame,
                        new Rectangle(bibFinalDrawPointX, bibFinalDrawPointY,
                        bibFrame.SourceRectangle.Width, bibFrame.SourceRectangle.Height),
                        nonRemapColor * opacity, false, false, new DepthRectangle(1f, 1f)));

                    if (bibGraphics.HasRemapFrames())
                    {
                        var remapFrame = bibGraphics.GetRemapFrame(0);

                        if (remapFrame != null)
                        {
                            objectSpriteRecord.AddGraphicsEntry(new ObjectSpriteEntry(bibGraphics.GetPaletteTexture(), bibGraphics.GetRemapFrame(0),
                                new Rectangle(bibFinalDrawPointX, bibFinalDrawPointY,
                                bibFrame.SourceRectangle.Width, bibFrame.SourceRectangle.Height),
                                remapColor, true, false, new DepthRectangle(1f, 1f)));
                        }
                    }
                }
            }

            var frame = graphics.GetFrame(frameIndex);
            if (frame == null)
            {
                objectSpriteRecord.AddTextEntry(new TextEntry("#" + baseNodeIndex, baseNodeIndexColor, drawPoint));
                return;
            }

            texture = frame.Texture;

            int x = drawPoint.X - frame.ShapeWidth / 2 + frame.OffsetX + Constants.CellSizeX / 2;
            int y = drawPoint.Y - frame.ShapeHeight / 2 + frame.OffsetY + Constants.CellSizeY / 2 + yDrawOffset;
            Rectangle drawRectangle = new Rectangle(x, y, frame.SourceRectangle.Width, frame.SourceRectangle.Height);

            objectSpriteRecord.AddGraphicsEntry(new ObjectSpriteEntry(graphics.GetPaletteTexture(), texture, frame.SourceRectangle, drawRectangle, nonRemapColor * opacity, false, false, new DepthRectangle(1f, 1f)));

            if (graphics.HasRemapFrames())
            {
                var remapFrame = graphics.GetRemapFrame(frameIndex);

                if (remapFrame != null)
                {
                    objectSpriteRecord.AddGraphicsEntry(new ObjectSpriteEntry(graphics.GetPaletteTexture(), graphics.GetRemapFrame(frameIndex).Texture,
                        graphics.GetRemapFrame(frameIndex).SourceRectangle, drawRectangle, remapColor, true, false, new DepthRectangle(1f, 1f)));
                }
            }

            objectSpriteRecord.AddTextEntry(new TextEntry("#" + baseNodeIndex, baseNodeIndexColor, drawPoint));
        }

        private void DrawCellTag(CellTag cellTag)
        {
            Point2D drawPoint = EditorState.Is2DMode ? 
                CellMath.CellTopLeftPointFromCellCoords(cellTag.Position, Map) : 
                CellMath.CellTopLeftPointFromCellCoords_3D(cellTag.Position, Map);

            const float cellTagAlpha = 0.45f;

            Color color = cellTag.Tag.Trigger.EditorColor == null ? UISettings.ActiveSettings.AltColor : cellTag.Tag.Trigger.XNAColor;
            Renderer.DrawTexture(EditorGraphics.CellTagTexture, 
                new Rectangle(drawPoint.X, drawPoint.Y, EditorGraphics.CellTagTexture.Width, EditorGraphics.CellTagTexture.Height), color * cellTagAlpha);
        }

        public Rectangle GetMapLocalViewRectangle()
        {
            const int InitialHeight = 3; // TS engine assumes the first cell to be at this height
            const double HeightAddition = 5.0; // TS engine adds this specified map height <3

            int x = (int)(Map.LocalSize.X * Constants.CellSizeX);
            int y = (int)(Map.LocalSize.Y - InitialHeight) * Constants.CellSizeY + Constants.MapYBaseline;
            int width = (int)(Map.LocalSize.Width * Constants.CellSizeX);
            int height = (int)(Map.LocalSize.Height + HeightAddition) * Constants.CellSizeY;

            return new Rectangle(x, y, width, height);
        }

        private void DrawMapBorder()
        {
            const int BorderThickness = 4;

            const int TopImpassableCellCount = 3; // The northernmost 3 cells are impassable in the TS engine, we'll also display this border

            var rectangle = GetMapLocalViewRectangle();

            Renderer.DrawRectangle(rectangle, Color.Blue, BorderThickness);

            int impassableY = (int)(rectangle.Y + (Constants.CellSizeY * TopImpassableCellCount));
            Renderer.FillRectangle(new Rectangle(rectangle.X, impassableY - (BorderThickness / 2), rectangle.Width, BorderThickness), Color.Teal * 0.25f);
        }

        public void DrawTechnoRangeIndicators(TechnoBase techno)
        {
            if (techno == null)
                return;

            double range = techno.GetWeaponRange();
            if (range > 0.0)
            {
                DrawRangeIndicator(techno, range, techno.Owner.XNAColor);
            }

            range = techno.GetGuardRange();
            if (range > 0.0)
            {
                DrawRangeIndicator(techno, range, techno.Owner.XNAColor * 0.25f);
            }

            range = techno.GetGapGeneratorRange();
            if (range > 0.0)
            {
                DrawRangeIndicator(techno, range, Color.Black * 0.75f);
            }

            range = techno.GetCloakGeneratorRange();
            if (range > 0.0)
            {
                DrawRangeIndicator(techno, range, techno.GetRadialColor());
            }

            range = techno.GetSensorArrayRange();
            if (range > 0.0)
            {
                DrawRangeIndicator(techno, range, techno.GetRadialColor());
            }
        }

        private void DrawRangeIndicator(TechnoBase techno, double range, Color color)
        {
            Point2D center = EditorState.Is2DMode ? 
                CellMath.CellCenterPointFromCellCoords(techno.Position, Map) : 
                CellMath.CellCenterPointFromCellCoords_3D(techno.Position, Map);

            int bridgeHeightOffset = techno.IsOnBridge() ? (Constants.CellHeight * Constants.HighBridgeHeight) : 0;

            // Range is specified in "tile edge lengths",
            // so we need a bit of trigonometry
            double horizontalPixelRange = Constants.CellSizeX / Math.Sqrt(2.0);
            double verticalPixelRange = Constants.CellSizeY / Math.Sqrt(2.0);

            int startX = center.X - (int)(range * horizontalPixelRange);
            int startY = center.Y - bridgeHeightOffset - (int)(range * verticalPixelRange);
            int endX = center.X + (int)(range * horizontalPixelRange);
            int endY = center.Y - bridgeHeightOffset + (int)(range * verticalPixelRange);

            // startX = Camera.ScaleIntWithZoom(startX - Camera.TopLeftPoint.X);
            // startY = Camera.ScaleIntWithZoom(startY - Camera.TopLeftPoint.Y);
            // endX = Camera.ScaleIntWithZoom(endX - Camera.TopLeftPoint.X);
            // endY = Camera.ScaleIntWithZoom(endY - Camera.TopLeftPoint.Y);

            Renderer.DrawTexture(EditorGraphics.RangeIndicatorTexture,
                new Rectangle(startX, startY, endX - startX, endY - startY), color);
        }

        public void DrawOnTileUnderCursor(MapTile tileUnderCursor, CursorAction cursorAction, bool isDraggingObject, bool isRotatingObject,
            IMovable draggedOrRotatedObject, bool isCloning, bool overlapObjects)
        {
            if (tileUnderCursor == null)
            {
                Renderer.DrawString(Translate(this, "DrawOnTileUnderCursor.NullTile", "Null tile"), 0, new Vector2(0f, 40f), Color.White);
                return;
            }

            if (cursorAction != null)
            {
                if (cursorAction.DrawCellCursor)
                    DrawTileCursor(tileUnderCursor);

                return;
            }

            if (isDraggingObject)
            {
                var startCell = Map.GetTile(draggedOrRotatedObject.Position);
                if (startCell == tileUnderCursor)
                    return;

                Color lineColor = isCloning ? new Color(0, 255, 255) : Color.White;
                if (!Map.CanPlaceObjectAt(draggedOrRotatedObject, tileUnderCursor.CoordsToPoint(), isCloning, overlapObjects) ||
                    (isCloning && !Helpers.IsCloningSupported(draggedOrRotatedObject)))
                {
                    lineColor = Color.Red;
                }

                Point2D cameraAndCellCenterOffset = new Point2D(-Camera.TopLeftPoint.X + Constants.CellSizeX / 2,
                                                 -Camera.TopLeftPoint.Y + Constants.CellSizeY / 2);

                Point2D startDrawPoint = CellMath.CellTopLeftPointFromCellCoords(draggedOrRotatedObject.Position, Map) + cameraAndCellCenterOffset;
                
                if (startCell != null)
                {
                    if (!EditorState.Is2DMode)
                        startDrawPoint -= new Point2D(0, startCell.Level * Constants.CellHeight);

                    if (draggedOrRotatedObject.IsOnBridge())
                        startDrawPoint -= new Point2D(0, Constants.HighBridgeHeight * Constants.CellHeight);

                    if (draggedOrRotatedObject.WhatAmI() == RTTIType.Infantry)
                        startDrawPoint += CellMath.GetSubCellOffset(((Infantry)draggedOrRotatedObject).SubCell) - new Point2D(0, Constants.CellHeight / 2);
                }

                Point2D endDrawPoint = CellMath.CellTopLeftPointFromCellCoords(tileUnderCursor.CoordsToPoint(), Map) + cameraAndCellCenterOffset;

                if (!EditorState.Is2DMode)
                    endDrawPoint -= new Point2D(0, tileUnderCursor.Level * Constants.CellHeight);

                if (draggedOrRotatedObject.IsOnBridge())
                    endDrawPoint -= new Point2D(0, Constants.HighBridgeHeight * Constants.CellHeight);

                startDrawPoint = startDrawPoint.ScaleBy(Camera.ZoomLevel);
                endDrawPoint = endDrawPoint.ScaleBy(Camera.ZoomLevel);

                Renderer.DrawLine(startDrawPoint.ToXNAVector(), endDrawPoint.ToXNAVector(), lineColor, 1);
            }
            else if (isRotatingObject)
            {
                var startCell = Map.GetTile(draggedOrRotatedObject.Position);
                if (startCell == tileUnderCursor)
                    return;

                Color lineColor = Color.Yellow;

                Point2D cameraAndCellCenterOffset = new Point2D(-Camera.TopLeftPoint.X + Constants.CellSizeX / 2,
                                                 -Camera.TopLeftPoint.Y + Constants.CellSizeY / 2);

                Point2D startDrawPoint = CellMath.CellTopLeftPointFromCellCoords(draggedOrRotatedObject.Position, Map) + cameraAndCellCenterOffset;
                
                if (startCell != null)
                {
                    if (!EditorState.Is2DMode)
                        startDrawPoint -= new Point2D(0, Map.GetTile(draggedOrRotatedObject.Position).Level * Constants.CellHeight);

                    if (draggedOrRotatedObject.IsOnBridge())
                        startDrawPoint -= new Point2D(0, Constants.HighBridgeHeight * Constants.CellHeight);

                    if (draggedOrRotatedObject.WhatAmI() == RTTIType.Infantry)
                        startDrawPoint += CellMath.GetSubCellOffset(((Infantry)draggedOrRotatedObject).SubCell) - new Point2D(0, Constants.CellHeight / 2);
                }

                Point2D endDrawPoint = CellMath.CellTopLeftPointFromCellCoords(tileUnderCursor.CoordsToPoint(), Map) + cameraAndCellCenterOffset;

                if (!EditorState.Is2DMode)
                    endDrawPoint -= new Point2D(0, tileUnderCursor.Level * Constants.CellHeight);

                if (draggedOrRotatedObject.IsOnBridge())
                    endDrawPoint -= new Point2D(0, Constants.HighBridgeHeight * Constants.CellHeight);

                startDrawPoint = startDrawPoint.ScaleBy(Camera.ZoomLevel);
                endDrawPoint = endDrawPoint.ScaleBy(Camera.ZoomLevel);

                Renderer.DrawLine(startDrawPoint.ToXNAVector(), endDrawPoint.ToXNAVector(), lineColor, 1);

                if (draggedOrRotatedObject.IsTechno())
                {
                    var techno = (TechnoBase)draggedOrRotatedObject;
                    Point2D point = tileUnderCursor.CoordsToPoint() - draggedOrRotatedObject.Position;

                    float angle = point.Angle() + ((float)Math.PI / 2.0f);
                    if (angle > (float)Math.PI * 2.0f)
                    {
                        angle -= ((float)Math.PI * 2.0f);
                    }
                    else if (angle < 0f)
                    {
                        angle += (float)Math.PI * 2.0f;
                    }

                    float percent = angle / ((float)Math.PI * 2.0f);
                    byte facing = (byte)Math.Ceiling(percent * (float)byte.MaxValue);

                    techno.Facing = facing;
                    AddRefreshPoint(techno.Position, 2);
                }
            }
            else
            {
                DrawTileCursor(tileUnderCursor);
            }
        }

        private void DrawTileCursor(MapTile tileUnderCursor)
        {
            Color lineColor = new Color(96, 168, 96, 128);
            Point2D cellTopLeftPoint = CellMath.CellTopLeftPointFromCellCoords(new Point2D(tileUnderCursor.X, tileUnderCursor.Y), Map) - Camera.TopLeftPoint;

            int height = 0;

            if (!EditorState.Is2DMode)
            {
                height = tileUnderCursor.Level * Constants.CellHeight;

                var techno = tileUnderCursor.GetTechno();
                if (techno != null && techno.IsOnBridge())
                    height += Constants.HighBridgeHeight * Constants.CellHeight;
            }

            cellTopLeftPoint = new Point2D((int)(cellTopLeftPoint.X * Camera.ZoomLevel), (int)((cellTopLeftPoint.Y - height) * Camera.ZoomLevel));

            var cellTopPoint = new Vector2(cellTopLeftPoint.X + (int)((Constants.CellSizeX / 2) * Camera.ZoomLevel), cellTopLeftPoint.Y);
            var cellLeftPoint = new Vector2(cellTopLeftPoint.X, cellTopLeftPoint.Y + (int)((Constants.CellSizeY / 2) * Camera.ZoomLevel));
            var cellRightPoint = new Vector2(cellTopLeftPoint.X + (int)(Constants.CellSizeX * Camera.ZoomLevel), cellLeftPoint.Y);
            var cellBottomPoint = new Vector2(cellTopPoint.X, cellTopLeftPoint.Y + (int)(Constants.CellSizeY * Camera.ZoomLevel));

            Renderer.DrawLine(cellTopPoint, cellLeftPoint, lineColor, 1);
            Renderer.DrawLine(cellRightPoint, cellTopPoint, lineColor, 1);
            Renderer.DrawLine(cellBottomPoint, cellLeftPoint, lineColor, 1);
            Renderer.DrawLine(cellRightPoint, cellBottomPoint, lineColor, 1);

            var shadowColor = new Color(0, 0, 0, 128);
            var down = new Vector2(0, 1f);

            Renderer.DrawLine(cellTopPoint + down, cellLeftPoint + down, shadowColor, 1);
            Renderer.DrawLine(cellRightPoint + down, cellTopPoint + down, shadowColor, 1);
            Renderer.DrawLine(cellBottomPoint + down, cellLeftPoint + down, shadowColor, 1);
            Renderer.DrawLine(cellRightPoint + down, cellBottomPoint + down, shadowColor, 1);

            int zoomedHeight = (int)(height * Camera.ZoomLevel);

            Color heightBarColor = new Color(16, 16, 16, (int)byte.MaxValue) * 0.75f;
            const int baseHeightLineSpaceAtBeginningOfStep = 6;
            int heightLineSpaceAtBeginningOfStep = Camera.ScaleIntWithZoom(baseHeightLineSpaceAtBeginningOfStep);
            int heightBarStep = Camera.ScaleIntWithZoom(Constants.CellHeight - baseHeightLineSpaceAtBeginningOfStep);
            const int heightBarWidth = 2;

            int y = 0;
            while (y < zoomedHeight - heightBarStep)
            {
                y += heightLineSpaceAtBeginningOfStep;
                Renderer.FillRectangle(new Rectangle((int)cellLeftPoint.X - 1, (int)cellLeftPoint.Y + y, heightBarWidth, heightBarStep), heightBarColor);
                Renderer.FillRectangle(new Rectangle((int)cellBottomPoint.X - 1, (int)cellBottomPoint.Y + y, heightBarWidth, heightBarStep), heightBarColor);
                Renderer.FillRectangle(new Rectangle((int)cellRightPoint.X - 1, (int)cellRightPoint.Y + y, heightBarWidth, heightBarStep), heightBarColor);
                y += heightBarStep;
            }
        }

        private void DrawImpassableHighlight(MapTile cell)
        {
            var subTile = TheaterGraphics.GetTileGraphics(cell.TileIndex).GetSubTile(cell.SubTileIndex);
            
            if (!Helpers.IsLandTypeImpassable(subTile.TmpImage.TerrainType, false) && 
                (cell.Overlay == null || cell.Overlay.OverlayType == null || !Helpers.IsLandTypeImpassable(cell.Overlay.OverlayType.Land, false)))
            {
                return;
            }

            Point2D cellTopLeftPoint = EditorState.Is2DMode ?
                CellMath.CellTopLeftPointFromCellCoords(cell.CoordsToPoint(), Map) :
                CellMath.CellTopLeftPointFromCellCoords_3D(cell.CoordsToPoint(), Map);

            Renderer.DrawTexture(EditorGraphics.ImpassableCellHighlightTexture, 
                new Rectangle(cellTopLeftPoint.X, cellTopLeftPoint.Y, 
                EditorGraphics.ImpassableCellHighlightTexture.Width, EditorGraphics.ImpassableCellHighlightTexture.Height),
                Color.White);
        }

        private void DrawIceGrowthHighlight(MapTile cell)
        {
            if (cell.IceGrowth <= 0)
                return;

            Point2D cellTopLeftPoint = EditorState.Is2DMode ?
                CellMath.CellTopLeftPointFromCellCoords(cell.CoordsToPoint(), Map) :
                CellMath.CellTopLeftPointFromCellCoords_3D(cell.CoordsToPoint(), Map);

            Renderer.DrawTexture(EditorGraphics.IceGrowthHighlightTexture,
                new Rectangle(cellTopLeftPoint.X, cellTopLeftPoint.Y,
                EditorGraphics.IceGrowthHighlightTexture.Width, EditorGraphics.IceGrowthHighlightTexture.Height),
                Color.White);
        }

        private void DrawTubes()
        {
            foreach (var tube in Map.Tubes)
            {
                var entryCellCenterPoint = CellMath.CellCenterPointFromCellCoords(tube.EntryPoint, Map);
                var exitCellCenterPoint = CellMath.CellCenterPointFromCellCoords(tube.ExitPoint, Map);
                var entryCell = Map.GetTile(tube.EntryPoint);
                int height = 0;
                if (entryCell != null && !EditorState.Is2DMode)
                    height = entryCell.Level * Constants.CellHeight;

                Point2D currentPoint = tube.EntryPoint;

                Color color = tube.Pending ? Color.Orange : Color.LimeGreen;

                if (tube.Directions.Count == 0)
                {
                    var drawPoint = CellMath.CellTopLeftPointFromCellCoords_3D(tube.EntryPoint, Map).ToXNAPoint();
                    var drawRectangle = new Rectangle(drawPoint.X, drawPoint.Y, EditorGraphics.GenericTileWithBorderTexture.Width, EditorGraphics.GenericTileWithBorderTexture.Height);
                    Renderer.DrawTexture(EditorGraphics.GenericTileWithBorderTexture, drawRectangle, color);
                }

                foreach (var direction in tube.Directions)
                {
                    Point2D nextPoint = currentPoint.NextPointFromTubeDirection(direction);

                    if (nextPoint != currentPoint)
                    {
                        var currentPixelPoint = CellMath.CellCenterPointFromCellCoords(currentPoint, Map);
                        var nextPixelPoint = CellMath.CellCenterPointFromCellCoords(nextPoint, Map);

                        DrawArrow(currentPixelPoint.ToXNAVector() - new Vector2(0, height),
                            nextPixelPoint.ToXNAVector() - new Vector2(0, height),
                            color, 0.25f, 10f, 1);
                    }

                    currentPoint = nextPoint;
                }
            }
        }

        private static void DrawArrow(Vector2 start, Vector2 end,
            Color color, float angleDiff, float sideLineLength, int thickness = 1)
            => RendererExtensions.DrawArrow(start, end, color, angleDiff, sideLineLength, thickness);

        public void Draw(bool isActive, TechnoBase technoUnderCursor, MapTile tileUnderCursor, CursorAction cursorAction)
        {
            if (isActive && tileUnderCursor != null && cursorAction != null)
            {
                cursorAction.PreMapDraw(tileUnderCursor.CoordsToPoint());
            }

            if (mapInvalidated || cameraMoved)
            {
                DrawVisibleMapPortion();
                mapInvalidated = false;
                cameraMoved = false;
            }

            CalculateMapRenderRectangles();

            DrawPerFrameTransparentElements(technoUnderCursor);

            DrawWorld();

            if (EditorState.DrawMapWideOverlay)
            {
                MapWideOverlay.Draw(new Rectangle(
                        (int)(-Camera.TopLeftPoint.X * Camera.ZoomLevel),
                        (int)((-Camera.TopLeftPoint.Y + Constants.MapYBaseline) * Camera.ZoomLevel),
                        (int)(mapRenderTarget.Width * Camera.ZoomLevel),
                        (int)((mapRenderTarget.Height - Constants.MapYBaseline) * Camera.ZoomLevel)));
            }

            if (isActive && tileUnderCursor != null && cursorAction != null)
            {
                cursorAction.PostMapDraw(tileUnderCursor.CoordsToPoint());
                cursorAction.DrawPreview(tileUnderCursor.CoordsToPoint(), Camera.TopLeftPoint);
            }
        }

        private void DrawPerFrameTransparentElements(TechnoBase technoUnderCursor)
        {
            Renderer.PushRenderTarget(transparencyPerFrameRenderTarget);

            GraphicsDevice.Clear(Color.Transparent);

            DrawMapBorder();
            DrawTechnoRangeIndicators(technoUnderCursor);

            Renderer.PopRenderTarget();
        }

        /// <summary>
        /// Draws the visible part of the map to the minimap.
        /// </summary>
        public void DrawOnMinimap()
        {
            if (MinimapUsers.Count > 0)
            {
                Renderer.PushRenderTarget(minimapRenderTarget);

                if (minimapNeedsRefresh)
                {
                    Renderer.DrawTexture(compositeRenderTarget,
                        new Rectangle(0, 0, mapRenderTarget.Width, mapRenderTarget.Height),
                        new Rectangle(0, 0, mapRenderTarget.Width, mapRenderTarget.Height),
                        Color.White);
                }
                else
                {
                    Renderer.DrawTexture(compositeRenderTarget,
                        mapRenderSourceRectangle,
                        mapRenderSourceRectangle,
                        Color.White);
                }

                Renderer.PopRenderTarget();
            }

            minimapNeedsRefresh = false;
        }

        private void CalculateMapRenderRectangles()
        {
            int zoomedWidth = (int)(Width / Camera.ZoomLevel);
            int zoomedHeight = (int)(Height / Camera.ZoomLevel);

            // Constrain draw coordinates so that we don't draw out of bounds and cause weird artifacts on map edge

            int sourceX = Camera.TopLeftPoint.X;
            int destinationX = 0;
            int destinationWidth = Width;
            if (sourceX < 0)
            {
                sourceX = 0;
                destinationX = (int)(-Camera.TopLeftPoint.X * Camera.ZoomLevel);
                destinationWidth -= destinationX;
                zoomedWidth += Camera.TopLeftPoint.X;
            }

            int sourceY = Camera.TopLeftPoint.Y;
            int destinationY = 0;
            int destinationHeight = Height;
            if (sourceY < 0)
            {
                sourceY = 0;
                destinationY = (int)(-Camera.TopLeftPoint.Y * Camera.ZoomLevel);
                destinationHeight -= destinationY;
                zoomedHeight += Camera.TopLeftPoint.Y;
            }

            if (sourceX + zoomedWidth > mapRenderTarget.Width)
            {
                zoomedWidth = mapRenderTarget.Width - sourceX;
                destinationWidth = (int)(zoomedWidth * Camera.ZoomLevel);
            }

            if (sourceY + zoomedHeight > mapRenderTarget.Height)
            {
                zoomedHeight = mapRenderTarget.Height - sourceY;
                destinationHeight = (int)(zoomedHeight * Camera.ZoomLevel);
            }

            mapRenderSourceRectangle = new Rectangle(sourceX, sourceY, zoomedWidth, zoomedHeight);
            mapRenderDestinationRectangle = new Rectangle(destinationX, destinationY, destinationWidth, destinationHeight);
        }

        private void DrawWorld()
        {
            Rectangle sourceRectangle = new Rectangle(0, 0, mapRenderTarget.Width, mapRenderTarget.Height);
            Rectangle destinationRectangle = sourceRectangle;

            Renderer.PushRenderTarget(compositeRenderTarget, new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, depthRenderStencilState, null, null));

            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 0f, 0);

            // Draw the map to the composite render target.
            Renderer.DrawTexture(mapRenderTarget,
                sourceRectangle,
                destinationRectangle,
                Color.White);

            // Rendering alpha images is a relatively expensive operation. Only do it if necessary.
            if (IsRenderFlagEnabled(RenderObjectFlags.AlphaLights) && alphaImagesToRender.Count > 0)
            {
                // Then draw alpha effects. First, render all alpha effects into the alpha surface. Then,
                // render the alpha surface on the composite render target using a special shader.

                Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, alphaImageToAlphaMapEffect));
                GraphicsDevice.SetRenderTarget(alphaRenderTarget);
                GraphicsDevice.Clear(new Color(0.5f, 0f, 0f, 0f));

                for (int i = 0; i < alphaImagesToRender.Count; i++)
                {
                    var alphaShape = alphaImagesToRender[i].AlphaImage;
                    int frameCount = alphaShape.GetFrameCount();

                    if (frameCount <= 0)
                        continue;

                    int frame = 0;

                    if (frameCount > 1)
                    {
                        if (alphaImagesToRender[i].OwnerObject is TechnoBase ownerTechno)
                            frame = ownerTechno.Facing / ((Constants.FacingMax + 1) / frameCount);
                    }

                    var alphaTexture = alphaShape.GetFrame(frame);
                    if (alphaTexture == null)
                        continue;

                    var pixelPoint = EditorState.Is2DMode ? CellMath.CellCenterPointFromCellCoords(alphaImagesToRender[i].Point, Map) :
                        CellMath.CellCenterPointFromCellCoords_3D(alphaImagesToRender[i].Point, Map);
                    var alphaDrawRectangle = new Rectangle(pixelPoint.X - alphaTexture.ShapeWidth / 2 + alphaTexture.OffsetX,
                        pixelPoint.Y - alphaTexture.ShapeHeight / 2 + alphaTexture.OffsetY, alphaTexture.Texture.Width, alphaTexture.Texture.Height);

                    Renderer.DrawTexture(alphaTexture.Texture, alphaDrawRectangle, Color.White);
                }

                Renderer.PopSettings();

                // Copy of the composite render target so we can sample it while rendering to it
                Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, null, null, null));
                GraphicsDevice.SetRenderTarget(compositeRenderTargetCopy);
                Renderer.DrawTexture(compositeRenderTarget, new Rectangle(0, 0, compositeRenderTarget.Width, compositeRenderTarget.Height), Color.White);
                Renderer.PopSettings();

                GraphicsDevice.SetRenderTarget(compositeRenderTarget);
                alphaMapDrawEffect.Parameters["RenderSurface"].SetValue(compositeRenderTargetCopy);
                Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, null, null, alphaMapDrawEffect));
                Renderer.DrawTexture(alphaRenderTarget, new Rectangle(0, 0, alphaRenderTarget.Width, alphaRenderTarget.Height), Color.White);
                Renderer.PopSettings();
            }

            // Then draw transparency layers.

            Renderer.DrawTexture(transparencyRenderTarget,
                sourceRectangle,
                destinationRectangle,
                Color.White);

            Renderer.DrawTexture(transparencyPerFrameRenderTarget,
                sourceRectangle,
                destinationRectangle,
                Color.White);

            Renderer.PopRenderTarget();

            // Last, draw the composite render target directly to the screen.

            Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null));

            Renderer.DrawTexture(compositeRenderTarget,
                mapRenderSourceRectangle,
                mapRenderDestinationRectangle,
                Color.White);

            Renderer.PopSettings();
        }

        public void AddPreviewToMap(MegamapRenderOptions megamapRenderOptions)
        {
            var megamapTexture = GenerateMegamapTexture(megamapRenderOptions);

            // Scale down the minimap texture
            var finalPreviewRenderTarget = new RenderTarget2D(GraphicsDevice, Constants.MapPreviewMaxWidth, Constants.MapPreviewMaxHeight, false, SurfaceFormat.Color, DepthFormat.None);
            var minimapTexture = Helpers.RenderTextureAsSmaller(megamapTexture, finalPreviewRenderTarget, GraphicsDevice);

            Map.WritePreview(minimapTexture);

            // Cleanup
            megamapTexture.Dispose();
            finalPreviewRenderTarget.Dispose();

            InvalidateMapForMinimap();
        }

        /// <summary>
        /// Renders the entire map into a new render target and returns the render target as a texture.
        /// </summary>
        private Texture2D GenerateMegamapTexture(MegamapRenderOptions megamapRenderOptions)
        {
            InstantRenderMegamap(megamapRenderOptions);

            RenderTarget2D texture;
            Rectangle sourceRectangle;

            if (megamapRenderOptions.HasFlag(MegamapRenderOptions.IncludeOnlyVisibleArea))
            {
                sourceRectangle = GetMapLocalViewRectangle();
            }
            else
            {
                sourceRectangle = new Rectangle(0, 0, compositeRenderTarget.Width, compositeRenderTarget.Height);
            }

            texture = new RenderTarget2D(GraphicsDevice, sourceRectangle.Width, sourceRectangle.Height, false, SurfaceFormat.Color, DepthFormat.None);

            Renderer.BeginDraw();
            Renderer.PushRenderTarget(texture);
            Renderer.DrawTexture(MinimapTexture, sourceRectangle, new Rectangle(0, 0, texture.Width, texture.Height), Color.White);
            Renderer.PopRenderTarget();
            Renderer.EndDraw();

            return texture;
        }

        public void ExtractMegamapTo(MegamapRenderOptions megamapRenderOptions, string path)
        {
            var megamapTexture = GenerateMegamapTexture(megamapRenderOptions);

            try
            {
                using (var stream = File.OpenWrite(path))
                {
                    megamapTexture.SaveAsPng(stream, megamapTexture.Width, megamapTexture.Height);
                }
            }
            catch (IOException ex)
            {
                Logger.Log("Failed to extract megamap texture. Returned error message: " + ex.Message);
                Logger.Log("Stacktrace: " + ex.StackTrace);

                EditorMessageBox.Show(windowManager, 
                    Translate(this, "ExtractMegamapTo.Failure.Title", "Failed to extract megamap"),
                    string.Format(Translate(this, "ExtractMegamapTo.Failure.Description", 
                        "Error encountered while attempting to extract megamap. Returned operating system error message: {0}"),
                            ex.Message), 
                    MessageBoxButtons.OK);
            }

            megamapTexture.Dispose();
        }

        private void InstantRenderMegamap(MegamapRenderOptions megamapRenderOptions)
        {
            EditorState.RenderInvisibleInGameObjects = false;

            // Register ourselves as a minimap user so the minimap texture gets refreshed
            MinimapUsers.Add(this);

            Renderer.BeginDraw();

            // Clear out existing map UI
            Renderer.PushRenderTarget(transparencyPerFrameRenderTarget);
            GraphicsDevice.Clear(Color.Transparent);
            Renderer.PopRenderTarget();

            // Emphasize cells with resources if that was requested
            if (megamapRenderOptions.HasFlag(MegamapRenderOptions.EmphasizeResources))
            {
                Map.DoForAllValidTiles(cell =>
                {
                    if (cell.Overlay != null && cell.Overlay.OverlayType.TiberiumType != null)
                    {
                        var tiberiumType = cell.Overlay.OverlayType.TiberiumType;
                        cell.CellLighting = new MapColor(tiberiumType.XNAColor.R / 128.0f, tiberiumType.XNAColor.G / 128.0f, tiberiumType.XNAColor.B / 128.0f);
                    }
                });
            }

            InvalidateMapForMinimap();
            DrawVisibleMapPortion();
            CalculateMapRenderRectangles();
            DrawWorld();

            // Mark player spots if that was requested
            if (megamapRenderOptions.HasFlag(MegamapRenderOptions.MarkPlayerSpots))
            {
                Renderer.PushRenderTarget(compositeRenderTarget);

                for (int i = 0; i < Constants.MultiplayerMaxPlayers; i++)
                {
                    var wp = Map.Waypoints.Find(wp => wp.Identifier == i);
                    if (wp != null)
                    {
                        var wpCenterPoint = EditorState.Is2DMode ? CellMath.CellCenterPointFromCellCoords(wp.Position, Map) :
                            CellMath.CellCenterPointFromCellCoords_3D(wp.Position, Map);

                        var wpRectangle = new Rectangle(wpCenterPoint.X - (int)(Constants.CellSizeX * 1.5),
                            wpCenterPoint.Y - (int)(Constants.CellSizeY * 1.5), Constants.CellSizeX * 3, Constants.CellSizeY * 3);

                        Renderer.DrawTexture(EditorGraphics.GenericTileWithBorderTexture, wpRectangle, Color.Red);

                        string wpString = wp.Identifier.ToString(CultureInfo.InvariantCulture);
                        float scale = Constants.IsRA2YR ? 5.25f : 5.0f;

                        var stringSize = Renderer.GetTextDimensions(wpString, Constants.UIBoldFont) * scale;
                        Renderer.DrawString(wpString, Constants.UIBoldFont,
                            new Vector2(wpRectangle.X + (wpRectangle.Width - stringSize.X) / 2,
                            wpRectangle.Y + (wpRectangle.Height - stringSize.Y) / 2),
                            Color.White, scale, 0f);
                    }
                }

                Renderer.PopRenderTarget();
            }

            DrawOnMinimap();

            mapInvalidated = false;
            cameraMoved = false;

            Renderer.EndDraw();

            MinimapUsers.Remove(this);

            if (megamapRenderOptions.HasFlag(MegamapRenderOptions.EmphasizeResources))
            {
                LightingChanged();
            }

            EditorState.RenderInvisibleInGameObjects = true;
        }
    }
}
