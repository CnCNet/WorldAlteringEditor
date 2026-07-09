using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TSMapEditor.CCEngine;
using TSMapEditor.CCEngine.TileData;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Rendering.Batching;
using TSMapEditor.Rendering.ObjectRenderers;
using TSMapEditor.Settings;

namespace TSMapEditor.Rendering
{
    /// <summary>
    /// An interface for an object that can be used to fetch 
    /// game logic related information about a theater.
    /// </summary>
    public interface ITheater
    {
        int GetTileSetId(int uniqueTileIndex);
        int TileCount { get; }
        ITileImage GetTile(int id);
        int GetOverlayFrameCount(OverlayType overlayType);
        Theater Theater { get; }
    }

    /// <summary>
    /// Class for preparing an arbitrary number of sprite sheets.
    /// </summary>
    public class GraphicsPreparationClass
    {
        public GraphicsPreparationClass(int width = RenderingConstants.MaximumDX11TextureSize, int height = RenderingConstants.MaximumDX11TextureSize) 
        {
            SheetWidth = width;
            SheetHeight = height;
        }

        public int SheetWidth { get; }
        public int SheetHeight { get; }

        public List<SpriteSheetPreparation> SpriteSheetPreparationObjects { get; } = new List<SpriteSheetPreparation>();

        public SpriteSheetPreparation CurrentSpriteSheetPreparationObject;

        public Action<object, SpriteSheetPreparation> PostProcessAction { get; set; }

        private readonly object locker = new object();

        public SpriteSheetPreparation GenerateNewSpriteSheetWorkingObject()
        {
            var spriteSheetPreparation = new SpriteSheetPreparation(SheetWidth, SheetHeight);

            lock (locker)
            {
                SpriteSheetPreparationObjects.Add(spriteSheetPreparation);
                CurrentSpriteSheetPreparationObject = spriteSheetPreparation;
            }

            return spriteSheetPreparation;
        }

        public SpriteSheetPreparation GetCurrent()
        {
            if (CurrentSpriteSheetPreparationObject == null)
                CurrentSpriteSheetPreparationObject = GenerateNewSpriteSheetWorkingObject();

            return CurrentSpriteSheetPreparationObject;
        }

        public Point AddImage(int width, int height, byte[] data, object meta)
        {
            if (!CanFitTexture(width, height))
                GenerateNewSpriteSheetWorkingObject();

            return GetCurrent().AddImage(width, height, data, meta);
        }

        public bool CanFitTexture(int width, int height) => GetCurrent().CanFitTexture(width, height);
    }

    /// <summary>
    /// Class for gathering 8-bit paletted sprites for generating a single sprite sheet texture.
    /// </summary>
    public class SpriteSheetPreparation
    {
        public SpriteSheetPreparation(int width, int height)
        {
            WorkingBufferWidth = width;
            WorkingBufferHeight = height;
            WorkingBuffer = new byte[WorkingBufferWidth * WorkingBufferHeight];
        }

        public int WorkingBufferWidth { get; }
        public int WorkingBufferHeight { get; }

        public readonly byte[] WorkingBuffer;
        public int maxX = 0;                        // Width of the whole mega-texture (width of the widest row of images).
        public int maxY = 0;                        // Height of the whole mega-texture (height of all rows summed).
        public int X { get; private set; } = 0;     // Horizontal start position of the next tile.
        public int Y { get; private set; } = 0;     // Vertical position of the current row.
        int rowHeight = 0;                          // Height of the current row (aka height of the tallest tile in the current row).

        public List<object> objs = new List<object>();

        public Texture2D Texture { get; set; }

        /// <summary>
        /// Sprite sheets get composited into bigger sprite sheets at the end of the graphics-loading phase.
        /// When that happens, the sprite sheet (and, individual images in the sprite sheet) might get shifted
        /// downwards compared to its original location. This records how much the shift was, so texture
        /// source rectangles can be adjusted accordingly.
        /// </summary>
        public int YOffset { get; set; }

        public int Width => Math.Max(maxX, X);

        public int Height => Y + rowHeight;

        public bool CanFitTexture(int width, int height)
        {
            if (width >= WorkingBufferWidth || height >= WorkingBufferHeight)
                throw new ArgumentException("Texture too large: " + width + "x" + height);

            // Check if fits on current row
            if (X + width < WorkingBufferWidth)
            {
                if (Y + height < WorkingBufferHeight)
                {
                    return true;
                }
            }

            // If not, check if the texture would fit if placed on a new row
            if (width < WorkingBufferWidth &&
                Y + rowHeight + height < WorkingBufferHeight)
            {
                return true;
            }

            return false;
        }

        public Point AddImage(int width, int height, byte[] imageData, object meta)
        {
            if (imageData.Length != width * height)
                throw new ArgumentException($"{nameof(SpriteSheetPreparation)}: Image data needs to match width x height. Expected size: {width * height}, actual: {imageData}");

            if (X + width > WorkingBufferWidth)
            {
                // Advance to next row
                maxX = Math.Max(maxX, X);
                X = 0;
                Y += rowHeight;
                rowHeight = 0;

                if (Y + height > WorkingBufferHeight)
                {
                    throw new InvalidOperationException("Image does not fit on MegaTexture!");
                }
            }

            // Save placement coord
            Point placementCoord = new Point(X, Y);

            // Copy buffer
            for (int h = 0; h < height; h++)
            {
                Buffer.BlockCopy(imageData, h * width, WorkingBuffer, (Y + h) * WorkingBufferWidth + X, width);
            }

            if (height > rowHeight)
                rowHeight = height;

            if (meta != null)
                objs.Add(meta);

            X += width;
            return placementCoord;
        }
    }

    public class VoxelModel : IDisposable
    {
        public VoxelModel(GraphicsDevice graphicsDevice, VxlFile vxl, HvaFile hva, XNAPalette palette,
            bool remapable = false, bool subjectToLighting = false, VplFile vpl = null)
        {
            this.graphicsDevice = graphicsDevice;
            this.vxl = vxl;
            this.hva = hva;
            this.vpl = vpl;
            this.palette = palette;
            this.remapable = remapable;
            this.subjectToLighting = subjectToLighting;
        }

        private readonly GraphicsDevice graphicsDevice;
        private readonly VxlFile vxl;
        private readonly HvaFile hva;
        private readonly VplFile vpl;
        private readonly XNAPalette palette;
        private readonly bool remapable;
        private readonly bool subjectToLighting;

        public Dictionary<(byte facing, RampType ramp), PositionedTexture> Frames { get; set; } = new();
        public Dictionary<(byte facing, RampType ramp), PositionedTexture> RemapFrames { get; set; } = new();

        public PositionedTexture GetFrame(byte facing, RampType ramp, bool affectedByLighting)
        {
            // The game only renders 32 facings, so round it to the closest true facing
            facing = Convert.ToByte(Math.Clamp(
                Math.Round((float)facing / 8, MidpointRounding.AwayFromZero) * 8,
                byte.MinValue,
                byte.MaxValue));

            var key = (facing, ramp);
            if (Frames.TryGetValue(key, out PositionedTexture value))
                return value;

            Palette palette = this.palette.GetPalette();
            (Texture2D texture, Point2D offset) = VxlRenderer.Render(graphicsDevice, facing, ramp, vxl, hva, palette, vpl, forRemap: false);
            if (texture == null)
            {
                Frames[key] = null;
                return Frames[key];
            }

            var positionedTexture = new PositionedTexture(texture.Width, texture.Height, offset.X, offset.Y, texture, new Rectangle(0, 0, texture.Width, texture.Height));
            Frames[key] = positionedTexture;
            return Frames[key];
        }

        public PositionedTexture GetRemapFrame(byte facing, RampType ramp, bool affectedByLighting)
        {
            if (!remapable)
                return null;

            // The game only renders 32 facings, so round it to the closest true facing
            facing = Convert.ToByte(Math.Clamp(
                Math.Round((float)facing / 8, MidpointRounding.AwayFromZero) * 8,
                byte.MinValue,
                byte.MaxValue));

            var key = (facing, ramp);
            if (RemapFrames.TryGetValue(key, out PositionedTexture value))
                return value;

            Palette palette = this.palette.GetPalette();
            (Texture2D texture, Point2D offset) = VxlRenderer.Render(graphicsDevice, facing, ramp, vxl, hva, palette, vpl, forRemap: true);
            if (texture == null)
            {
                RemapFrames[key] = null;
                return RemapFrames[key];
            }

            var colorData = new Color[texture.Width * texture.Height];
            texture.GetData(colorData);

            // The renderer has rendered the rest of the unit as Magenta, now strip it out
            for (int i = 0; i < colorData.Length; i++)
            {
                if (colorData[i] == Color.Magenta)
                    colorData[i] = Color.Transparent;
            }

            Color[] remapColorArray = colorData.Select(color =>
            {
                // Convert the color to grayscale
                float remapColor = Math.Max(color.R / 255.0f, Math.Max(color.G / 255.0f, color.B / 255.0f));

                // Brighten it up a bit
                remapColor *= Constants.RemapBrightenFactor;
                return new Color(remapColor, remapColor, remapColor, color.A);

            }).ToArray();

            var remapTexture = new Texture2D(graphicsDevice, texture.Width, texture.Height, false, SurfaceFormat.Color);
            remapTexture.SetData(remapColorArray);
            RemapFrames[key] = new PositionedTexture(remapTexture.Width, remapTexture.Height, offset.X, offset.Y, remapTexture, new Rectangle(0, 0, remapTexture.Width, remapTexture.Height));
            return RemapFrames[key];
        }

        public void ClearFrames()
        {
            foreach (var frame in Frames.Values)
                frame?.Dispose();

            foreach (var frame in RemapFrames.Values)
                frame?.Dispose();

            Frames.Clear();
            RemapFrames.Clear();
        }

        public void Dispose() => ClearFrames();
    }

    public class PositionedTexture
    {
        public int ShapeWidth;
        public int ShapeHeight;
        public int OffsetX;
        public int OffsetY;
        public Texture2D Texture;
        public Rectangle SourceRectangle;

        public PositionedTexture(int shapeWidth, int shapeHeight, int offsetX, int offsetY, Texture2D texture, Rectangle sourceRectangle)
        {
            ShapeWidth = shapeWidth;
            ShapeHeight = shapeHeight;
            OffsetX = offsetX;
            OffsetY = offsetY;
            Texture = texture;
            SourceRectangle = sourceRectangle;
        }

        public void Dispose()
        {
            if (Texture != null)
                Texture.Dispose();
        }
    }

    /// <summary>
    /// Graphical layer for the theater.
    /// </summary>
    public class TheaterGraphics : ITheater, ITheaterTileData
    {
        private const string SHP_FILE_EXTENSION = ".SHP";
        private const string VXL_FILE_EXTENSION = ".VXL";
        private const string HVA_FILE_EXTENSION = ".HVA";
        private const string PNG_FILE_EXTENSION = ".PNG";
        private const string TURRET_FILE_SUFFIX = "TUR";
        private const string BARREL_FILE_SUFFIX = "BARL";

        private Random random = new Random();

        public Theater Theater { get; }

        private CCFileManager fileManager;

        public readonly XNAPalette TheaterPalette;
        private readonly XNAPalette unitPalette;
        private readonly XNAPalette tiberiumPalette;
        private readonly XNAPalette animPalette;
        private readonly XNAPalette alphaPalette;
        private readonly XNAPalette palettePalette;
        private readonly VplFile vplFile;

        private readonly List<XNAPalette> palettes = new List<XNAPalette>();

        private List<GraphicsPreparationClass> graphicsPreparationObjects = new List<GraphicsPreparationClass>();
        private List<Texture2D> spriteSheets = new List<Texture2D>();
        private List<MGTileImage[]> terrainGraphicsList = new List<MGTileImage[]>();
        private List<MGTileImage[]> mmTerrainGraphicsList = new List<MGTileImage[]>();
        private List<bool> hasMMGraphics = new List<bool>();

        public int TileCount => terrainGraphicsList.Count;

        public MGTileImage GetTileGraphics(int id) => terrainGraphicsList[id][random.Next(terrainGraphicsList[id].Length)];
        public MGTileImage GetTileGraphics(int id, int variantId) => terrainGraphicsList[id][variantId];
        public MGTileImage GetMarbleMadnessTileGraphics(int id) => mmTerrainGraphicsList[id][0];
        public bool HasSeparateMarbleMadnessTileGraphics(int id) => hasMMGraphics[id];
        public TileImage GetTileImage(int id) => terrainGraphicsList[id][0];

        public ITileImage GetTile(int id) => GetTileGraphics(id);

        public ShapeImage[] TerrainObjectTextures { get; set; }
        public ShapeImage[] BuildingTextures { get; set; }
        public ShapeImage[] BuildingBibTextures { get; set; }
        public VoxelModel[] BuildingTurretModels { get; set; }
        public VoxelModel[] BuildingBarrelModels { get; set; }
        public ShapeImage[] UnitTextures { get; set; }
        public VoxelModel[] UnitModels { get; set; }
        public VoxelModel[] UnitTurretModels { get; set; }
        public VoxelModel[] UnitBarrelModels { get; set; }
        public VoxelModel[] AircraftModels { get; set; }
        public ShapeImage[] InfantryTextures { get; set; }
        public ShapeImage[] OverlayTextures { get; set; }
        public ShapeImage[] SmudgeTextures { get; set; }
        public ShapeImage[] AnimTextures { get; set; }
        public Dictionary<string, ShapeImage> AlphaImages { get; set; } = new Dictionary<string, ShapeImage>();

        public ShapeImage PipTextures { get; set; }

        private readonly object graphicsPreparationObjectLocker = new object();

        public TheaterGraphics(GraphicsDevice graphicsDevice, Theater theater, CCFileManager fileManager, Rules rules)
        {
            this.graphicsDevice = graphicsDevice;
            Theater = theater;
            this.fileManager = fileManager;

            TheaterPalette = GetPaletteOrFail(theater.TerrainPaletteName, false);
            unitPalette = GetPaletteOrFail(Theater.UnitPaletteName, true);
            animPalette = GetPaletteOrFail("anim.pal", true);
            tiberiumPalette = string.IsNullOrEmpty(Theater.TiberiumPaletteName) ? TheaterPalette : GetPaletteOrFail(Theater.TiberiumPaletteName, false);
            palettePalette = GetPaletteOrFail("palette.pal", true);
            vplFile = GetVplFile();

            RGBColor[] alphaPaletteColors = new RGBColor[Palette.LENGTH];
            for (int i = 0; i < alphaPaletteColors.Length; i++)
                alphaPaletteColors[i] = new RGBColor((byte)i, (byte)i, (byte)i);
            alphaPalette = new XNAPalette("AlphaPalette", alphaPaletteColors, graphicsDevice, false);

            if (UserSettings.Instance.MultithreadedTextureLoading)
            {
                var task1 = Task.Factory.StartNew(() => ReadTileTextures());
                var task2 = Task.Factory.StartNew(() => ReadTerrainObjectTextures(rules.TerrainTypes));
                var task3 = Task.Factory.StartNew(() => ReadBuildingTextures(rules.BuildingTypes));
                var task4 = Task.Factory.StartNew(() => ReadBuildingTurretModels(rules.BuildingTypes));
                var task5 = Task.Factory.StartNew(() => ReadBuildingBarrelModels(rules.BuildingTypes));
                var task6 = Task.Factory.StartNew(() => ReadUnitTextures(rules.UnitTypes));
                var task7 = Task.Factory.StartNew(() => ReadUnitModels(rules.UnitTypes));
                var task8 = Task.Factory.StartNew(() => ReadUnitTurretModels(rules.UnitTypes));
                var task9 = Task.Factory.StartNew(() => ReadUnitBarrelModels(rules.UnitTypes));
                var task10 = Task.Factory.StartNew(() => ReadAircraftModels(rules.AircraftTypes));
                var task11 = Task.Factory.StartNew(() => ReadInfantryTextures(rules.InfantryTypes));
                var task12 = Task.Factory.StartNew(() => ReadOverlayTextures(rules.OverlayTypes));
                var task13 = Task.Factory.StartNew(() => ReadSmudgeTextures(rules.SmudgeTypes));
                var task14 = Task.Factory.StartNew(() => ReadAnimTextures(rules.AnimTypes, rules.BuildingTypes));
                var task15 = Task.Factory.StartNew(() => ReadAlphaImages(rules));
                var task16 = Task.Factory.StartNew(() => ReadMiscTextures());
                Task.WaitAll(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14, task15, task16);
            }
            else
            {
                ReadTileTextures();
                ReadTerrainObjectTextures(rules.TerrainTypes);
                ReadBuildingTextures(rules.BuildingTypes);
                ReadBuildingTurretModels(rules.BuildingTypes);
                ReadBuildingBarrelModels(rules.BuildingTypes);
                ReadUnitTextures(rules.UnitTypes);
                ReadUnitModels(rules.UnitTypes);
                ReadUnitTurretModels(rules.UnitTypes);
                ReadUnitBarrelModels(rules.UnitTypes);
                ReadAircraftModels(rules.AircraftTypes);
                ReadInfantryTextures(rules.InfantryTypes);
                ReadOverlayTextures(rules.OverlayTypes);
                ReadSmudgeTextures(rules.SmudgeTypes);
                ReadAnimTextures(rules.AnimTypes, rules.BuildingTypes);
                ReadAlphaImages(rules);
                ReadMiscTextures();
            }

            CombineSpriteSheets();
        }

        private void PostProcessPositionedTexture(object obj, SpriteSheetPreparation spriteSheetPreparationObject)
        {
            var positionedTexture = (PositionedTexture)obj;
            positionedTexture.Texture = spriteSheetPreparationObject.Texture;
            positionedTexture.SourceRectangle = new Rectangle(positionedTexture.SourceRectangle.X,
                positionedTexture.SourceRectangle.Y + spriteSheetPreparationObject.YOffset,
                positionedTexture.SourceRectangle.Width,
                positionedTexture.SourceRectangle.Height);
        }

        private Texture2D TextureFromBuffer(int width, int height, byte[] buffer)
        {
            // Create texture color buffer and copy the data from the working buffer to the color buffer.
            byte[] finalColorBuffer = new byte[width * height];

            for (int ty = 0; ty < height; ty++)
            {
                Buffer.BlockCopy(buffer, ty * RenderingConstants.MaximumDX11TextureSize, finalColorBuffer, ty * width, width);
            }

            // Create Texture2D instance and write data from the color buffer to it.
            var spriteSheet = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Alpha8);
            spriteSheet.SetData(finalColorBuffer);
            return spriteSheet;
        }

        private void CombineSpriteSheets()
        {
            Logger.Log("Combining loaded sprite sheets into as few larger sprite-sheets as possible.");

            byte[] hugeBuffer = new byte[RenderingConstants.MaximumDX11TextureSize * RenderingConstants.MaximumDX11TextureSize];

            int x = 0;
            int y = 0;

            var processedSpriteSheets = new List<SpriteSheetPreparation>();

            // Go through the graphics preparation objects and combine their textures as much as possible.
            // For simplicity, we do this by rendering sprite sheets one after another, each sprite sheet acting as a "row".
            // This is because most sprite sheets are much wider than they are tall.
            for (int i = 0; i < graphicsPreparationObjects.Count; i++)
            {
                var graphicsPreparationObject = graphicsPreparationObjects[i];

                for (int j = 0; j < graphicsPreparationObject.SpriteSheetPreparationObjects.Count; j++)
                {
                    var spriteSheetObject = graphicsPreparationObject.SpriteSheetPreparationObjects[j];

                    if (y + spriteSheetObject.Height > RenderingConstants.MaximumDX11TextureSize)
                    {
                        // Overflow - generate a Texture2D out of so-far processed sheets
                        Logger.Log("Sprite sheet buffer full - converting to Texture2D and making way for another sheet.");
                        var spriteSheet = TextureFromBuffer(x, y, hugeBuffer);
                        spriteSheets.Add(spriteSheet);
                        processedSpriteSheets.ForEach(ss => ss.Texture = spriteSheet);
                        processedSpriteSheets.Clear();

                        Array.Clear(hugeBuffer);

                        x = 0;
                        y = 0;
                    }

                    // Copy data from sprite sheet to our buffer
                    for (int sy = 0; sy < spriteSheetObject.Height; sy++)
                    {
                        Buffer.BlockCopy(spriteSheetObject.WorkingBuffer, sy * spriteSheetObject.WorkingBufferWidth, hugeBuffer, (y + sy) * RenderingConstants.MaximumDX11TextureSize, spriteSheetObject.Width);
                    }

                    x = Math.Max(x, spriteSheetObject.Width);
                    spriteSheetObject.YOffset = y;
                    y += spriteSheetObject.Height;
                    processedSpriteSheets.Add(spriteSheetObject);
                }
            }

            // All sprite sheets have been processed - generate texture from the last ones
            if (x > 0 && y > 0)
            {
                var spriteSheet = TextureFromBuffer(x, y, hugeBuffer);
                spriteSheets.Add(spriteSheet);
                processedSpriteSheets.ForEach(ss => ss.Texture = spriteSheet);
            }

            Logger.Log("All sprite sheets processed. Sprite sheet count: " + spriteSheets.Count);

            // Run post-process actions
            foreach (var graphicsPreparationObject in graphicsPreparationObjects)
            {
                if (graphicsPreparationObject.PostProcessAction == null)
                    continue;

                foreach (var spriteSheetPreparationObject in graphicsPreparationObject.SpriteSheetPreparationObjects)
                {
                    spriteSheetPreparationObject.objs.ForEach(obj => graphicsPreparationObject.PostProcessAction(obj, spriteSheetPreparationObject));
                }
            }

            // Free up some memory - we don't need the graphics preparation objects nor their sprite sheet preparation objects anymore
            graphicsPreparationObjects.Clear();
        }

        private readonly GraphicsDevice graphicsDevice;


        private static string[] NewTheaterHardcodedPrefixes = new string[] { "CA", "CT", "GA", "GT", "NA", "NT" };

        private void ReadTileTextures()
        {
            Logger.Log("Loading tile textures.");

            int currentTileIndex = 0; // Used for setting the starting tile ID of a tileset

            // Create a buffer that can fit as many TMPs as can theoretically fit into a Texture2D.
            // When converting into an actual texture, a suitable piece will be copied into
            // finalColorBuffer for assignment as the Texture2D color data.
            var graphicsPreparationObject = new GraphicsPreparationClass();

            for (int tsId = 0; tsId < Theater.TileSets.Count; tsId++)
            {
                TileSet tileSet = Theater.TileSets[tsId];
                tileSet.StartTileIndex = currentTileIndex;
                tileSet.LoadedTileCount = 0;

                Console.WriteLine("Loading " + tileSet.SetName);

                for (int i = 0; i < tileSet.TilesInSet; i++)
                {
                    var tileGraphics = new List<MGTileImage>();

                    // Handle graphics variation (clear00.tem, clear00a.tem, clear00b.tem etc.)
                    for (int v = 0; v < 'g' - 'a'; v++)
                    {
                        string baseName = tileSet.FileName + (i + 1).ToString("D2", CultureInfo.InvariantCulture);

                        if (v > 0)
                        {
                            baseName = baseName + ((char)('a' + (v - 1)));
                        }

                        string fileName = baseName + Theater.FileExtension;
                        byte[] data = fileManager.LoadFile(fileName);

                        if (data == null && !string.IsNullOrWhiteSpace(Theater.FallbackTileFileExtension))
                        {
                            // Support for FA2 NEWURBAN hack. FA2 Marble.mix does not contain Marble Madness graphics for NEWURBAN, only URBAN.
                            // To allow Marble Madness to work in NEWURBAN, FA2 also loads .urb files for NEWURBAN.
                            // We must do the same at least for now.
                            fileName = baseName + Theater.FallbackTileFileExtension;
                            data = fileManager.LoadFile(fileName);
                        }

                        if (data == null)
                        {
                            if (v == 0)
                            {
                                tileGraphics.Add(new MGTileImage(0, 0, tsId, i, currentTileIndex, Array.Empty<MGSubTileImage>()));
                                break;
                            }
                            else
                            {
                                break;
                            }
                        }

                        var tmpFile = new TmpFile(fileName);
                        tmpFile.ParseFromBuffer(data);

                        // Gather individual sub-tiles for this variation of the full tile
                        var tmpImages = new List<MGSubTileImage>();
                        for (int img = 0; img < tmpFile.ImageCount; img++)
                        {
                            var tmpImage = tmpFile.GetImage(img);

                            if (tmpImage == null)
                            {
                                tmpImages.Add(null);
                                continue;
                            }

                            var monoGameTmpImage = new MGSubTileImage(tmpImage, graphicsPreparationObject, TheaterPalette, tsId);
                            tmpImages.Add(monoGameTmpImage);
                        }

                        // Add this variation to list of variations for this tile
                        tileGraphics.Add(new MGTileImage(tmpFile.CellsX, tmpFile.CellsY, tsId, i, currentTileIndex, tmpImages.ToArray()));
                    }

                    tileSet.LoadedTileCount++;
                    currentTileIndex++;
                    terrainGraphicsList.Add(tileGraphics.ToArray());
                }
            }

            Logger.Log($"Writing {graphicsPreparationObject.SpriteSheetPreparationObjects.Count} sprite sheet(s) for TMP tiles from system memory to GPU memory.");

            lock (graphicsPreparationObjectLocker)
                graphicsPreparationObjects.Add(graphicsPreparationObject);

            // Assign this mega-texture to all tiles that were written into the texture.
            // Afterwards, we are done!
            graphicsPreparationObject.PostProcessAction = (obj, ssobj) => 
            {
                var mgTmpImage = (MGSubTileImage)obj;
                mgTmpImage.Texture = ssobj.Texture;

                mgTmpImage.SourceRectangle = mgTmpImage.SourceRectangle with { Y = mgTmpImage.SourceRectangle.Y + ssobj.YOffset };

                if (mgTmpImage.TmpImage.HasExtraData())
                {
                    mgTmpImage.ExtraSourceRectangle = mgTmpImage.ExtraSourceRectangle with { Y = mgTmpImage.ExtraSourceRectangle.Y + ssobj.YOffset };
                }
            };

            Logger.Log("Assigning marble madness mode tile textures.");

            // Assign marble-madness (MM) mode tile graphics
            int tileIndex = 0;
            for (int tsId = 0; tsId < Theater.TileSets.Count; tsId++)
            {
                TileSet tileSet = Theater.TileSets[tsId];
                if (tileSet.NonMarbleMadness > -1 || tileSet.MarbleMadness < 0 || tileSet.MarbleMadness >= Theater.TileSets.Count)
                {
                    // This is a MM tileset or a tileset with no MM graphics
                    for (int i = 0; i < tileSet.LoadedTileCount; i++)
                    {
                        mmTerrainGraphicsList.Add(terrainGraphicsList[tileIndex + i]);
                        hasMMGraphics.Add(tileSet.NonMarbleMadness > -1);
                    }

                    tileIndex += tileSet.LoadedTileCount;
                    continue;
                }

                // For non-MM tilesets with MM graphics, fetch the MM tileset
                TileSet mmTileSet = Theater.TileSets[tileSet.MarbleMadness];
                for (int i = 0; i < tileSet.LoadedTileCount; i++)
                {
                    mmTerrainGraphicsList.Add(terrainGraphicsList[mmTileSet.StartTileIndex + i]);
                    hasMMGraphics.Add(true);
                }
                tileIndex += tileSet.LoadedTileCount;
            }

            Logger.Log("Finished loading tile textures.");
        }

        public void ReadTerrainObjectTextures(List<TerrainType> terrainTypes)
        {
            Logger.Log("Loading terrain object textures.");

            var unitPalette = GetPaletteOrFail(Theater.UnitPaletteName, true);

            var graphicsPreparationObject = new GraphicsPreparationClass();

            TerrainObjectTextures = new ShapeImage[terrainTypes.Count];
            for (int i = 0; i < terrainTypes.Count; i++)
            {
                var terrainType = terrainTypes[i];

                string shpFileName = terrainType.ArtConfig.Image != null ? terrainType.ArtConfig.Image : terrainType.ININame;
                string pngFileName = shpFileName + PNG_FILE_EXTENSION;

                if (terrainType.ArtConfig.Theater)
                    shpFileName += Theater.FileExtension;
                else
                    shpFileName += SHP_FILE_EXTENSION;

                byte[] data = fileManager.LoadFile(pngFileName);

                bool subjectToLighting = !terrainType.SpawnsTiberium || Constants.TiberiumTreesAffectedByLighting;

                if (data != null)
                {
                    // Load graphics as PNG

                    TerrainObjectTextures[i] = new ShapeImage(graphicsDevice, null, null, null, subjectToLighting, false, PositionedTextureFromBytes(data));
                }
                else
                {
                    // Try to load graphics as SHP

                    data = fileManager.LoadFile(shpFileName);

                    if (data == null)
                        continue;

                    var shpFile = new ShpFile(shpFileName);
                    shpFile.ParseFromBuffer(data);

                    var palette = TheaterPalette;
                    if (terrainType.SpawnsTiberium)
                        palette = unitPalette;
                    else if (!string.IsNullOrEmpty(terrainType.ArtConfig.Palette))
                        palette = GetPaletteOrDefault(terrainType.ArtConfig.Palette + Theater.FileExtension[1..] + ".pal", palette, true);

                    TerrainObjectTextures[i] = new ShapeImage(graphicsDevice, shpFile, data,
                        palette, subjectToLighting, false, graphicsPreparationObject, UserSettings.Instance.ConserveVRAM ? [0] : null);
                }
            }

            lock (graphicsPreparationObjectLocker)
                graphicsPreparationObjects.Add(graphicsPreparationObject);

            graphicsPreparationObject.PostProcessAction = PostProcessPositionedTexture;

            Logger.Log("Finished loading terrain object textures.");
        }

        public void ReadBuildingTextures(List<BuildingType> buildingTypes)
        {
            Logger.Log("Loading building textures.");

            var graphicsPreparationObject = new GraphicsPreparationClass();

            BuildingTextures = new ShapeImage[buildingTypes.Count];
            BuildingBibTextures = new ShapeImage[buildingTypes.Count];

            for (int i = 0; i < buildingTypes.Count; i++)
            {
                var buildingType = buildingTypes[i];

                string shpFileName;
                if (!string.IsNullOrWhiteSpace(buildingType.ArtConfig.Image))
                    shpFileName = buildingType.ArtConfig.Image;
                else if (!string.IsNullOrWhiteSpace(buildingType.Image))
                    shpFileName = buildingType.Image;
                else
                    shpFileName = buildingType.ININame;

                if (buildingType.ArtConfig.Theater)
                    shpFileName += Theater.FileExtension;
                else
                    shpFileName += SHP_FILE_EXTENSION;

                // The game has hardcoded NewTheater=yes behaviour for buildings that start with a specific prefix
                bool hardcodedNewTheater = Array.Exists(NewTheaterHardcodedPrefixes, prefix => buildingType.ININame.ToUpperInvariant().StartsWith(prefix));

                string loadedShpName = "";

                byte[] shpData = null;
                if (buildingType.ArtConfig.NewTheater || hardcodedNewTheater)
                {
                    string newTheaterShpName = shpFileName.Substring(0, 1) + Theater.NewTheaterBuildingLetter + shpFileName.Substring(2);

                    shpData = fileManager.LoadFile(newTheaterShpName);
                    loadedShpName = newTheaterShpName;
                }

                // Support generic building letter
                if (Constants.NewTheaterGenericBuilding && shpData == null)
                {
                    string newTheaterShpName = shpFileName.Substring(0, 1) + Constants.NewTheaterGenericLetter + shpFileName.Substring(2);

                    shpData = fileManager.LoadFile(newTheaterShpName);
                    loadedShpName = newTheaterShpName;
                }

                // The game can apparently fall back to the non-theater-specific SHP file name
                // if the theater-specific SHP is not found
                if (shpData == null)
                {
                    shpData = fileManager.LoadFile(shpFileName);
                    loadedShpName = shpFileName;

                    if (shpData == null)
                    {
                        continue;
                    }
                }

                // Palette override in RA2/YR
                XNAPalette palette = buildingType.ArtConfig.TerrainPalette ? TheaterPalette : unitPalette;
                if (!string.IsNullOrWhiteSpace(buildingType.ArtConfig.Palette))
                    palette = GetPaletteOrDefault(buildingType.ArtConfig.Palette + Theater.FileExtension[1..] + ".pal", palette, !buildingType.ArtConfig.TerrainPalette);

                var shpFile = new ShpFile(loadedShpName);
                shpFile.ParseFromBuffer(shpData);

                bool affectedByLighting = buildingType.ArtConfig.TerrainPalette && Constants.TerrainPaletteBuildingsAffectedByLighting;

                BuildingTextures[i] = new ShapeImage(graphicsDevice, shpFile, shpData, palette,
                    affectedByLighting, buildingType.ArtConfig.Remapable, graphicsPreparationObject);

                // If this building has a bib, attempt to load it
                if (!string.IsNullOrWhiteSpace(buildingType.ArtConfig.BibShape))
                {
                    string bibShpFileName = buildingType.ArtConfig.BibShape;

                    if (buildingType.ArtConfig.Theater)
                        bibShpFileName += Theater.FileExtension;
                    else
                        bibShpFileName += SHP_FILE_EXTENSION;

                    shpData = null;
                    if (buildingType.ArtConfig.NewTheater)
                    {
                        string newTheaterBibShpName = bibShpFileName.Substring(0, 1) + Theater.NewTheaterBuildingLetter + bibShpFileName.Substring(2);

                        shpData = fileManager.LoadFile(newTheaterBibShpName);
                        loadedShpName = newTheaterBibShpName;
                    }

                    if (Constants.NewTheaterGenericBuilding && shpData == null)
                    {
                        string newTheaterBibShpName = bibShpFileName.Substring(0, 1) + Constants.NewTheaterGenericLetter + bibShpFileName.Substring(2);

                        shpData = fileManager.LoadFile(newTheaterBibShpName);
                    }

                    if (shpData == null)
                    {
                        shpData = fileManager.LoadFile(bibShpFileName);
                        loadedShpName = bibShpFileName;
                    }
                        
                    if (shpData == null)
                    {
                        continue;
                    }

                    var bibShpFile = new ShpFile(loadedShpName);
                    bibShpFile.ParseFromBuffer(shpData);
                    BuildingBibTextures[i] = new ShapeImage(graphicsDevice, bibShpFile, shpData, palette,
                        affectedByLighting, buildingType.ArtConfig.Remapable, graphicsPreparationObject);
                }
            }

            lock (graphicsPreparationObjectLocker)
                graphicsPreparationObjects.Add(graphicsPreparationObject);

            graphicsPreparationObject.PostProcessAction = PostProcessPositionedTexture;

            Logger.Log("Finished loading building textures.");
        }

        public void ReadAlphaImages(Rules rules)
        {
            Logger.Log("Loading alpha image textures.");

            List<GameObjectType> gameObjectTypes = new List<GameObjectType>(rules.BuildingTypes);
            gameObjectTypes.AddRange(rules.TerrainTypes);

            for (int i = 0; i < gameObjectTypes.Count; i++)
            {
                var gameObjectType = gameObjectTypes[i];

                if (string.IsNullOrWhiteSpace(gameObjectType.AlphaImage))
                    continue;

                if (AlphaImages.TryGetValue(gameObjectType.AlphaImage, out ShapeImage value))
                {
                    gameObjectType.AlphaShape = value;
                    continue;
                }

                string shpFileName = gameObjectType.AlphaImage + SHP_FILE_EXTENSION;

                var alphaShapeData = fileManager.LoadFile(shpFileName);
                if (alphaShapeData == null)
                    continue;

                var shpFile = new ShpFile(shpFileName);
                shpFile.ParseFromBuffer(alphaShapeData);
                var shapeImage = new ShapeImage(graphicsDevice, shpFile, alphaShapeData, alphaPalette, false);
                AlphaImages.Add(gameObjectType.AlphaImage, shapeImage);
                gameObjectType.AlphaShape = shapeImage;
            }

            Logger.Log("Finished loading alpha image textures.");
        }

        public void ReadBuildingTurretModels(List<BuildingType> buildingTypes)
        {
            Logger.Log("Loading building turrets' voxel models.");

            var loadedModels = new Dictionary<string, VoxelModel>();
            BuildingTurretModels = new VoxelModel[buildingTypes.Count];

            for (int i = 0; i < buildingTypes.Count; i++)
            {
                var buildingType = buildingTypes[i];

                if (!(buildingType.Turret && buildingType.TurretAnimIsVoxel))
                    continue;

                string artName = string.IsNullOrWhiteSpace(buildingType.Image) ? buildingType.ArtConfig.Image : buildingType.Image;
                if (string.IsNullOrEmpty(artName))
                    artName = buildingType.ININame;

                string turretModelName = string.IsNullOrEmpty(buildingType.TurretAnim) ? artName + TURRET_FILE_SUFFIX : buildingType.TurretAnim;
                if (loadedModels.TryGetValue(turretModelName, out VoxelModel loadedModel))
                {
                    BuildingTurretModels[i] = loadedModel;
                    continue;
                }

                byte[] vxlData = fileManager.LoadFile(turretModelName + VXL_FILE_EXTENSION);
                if (vxlData == null)
                    continue;

                byte[] hvaData = fileManager.LoadFile(turretModelName + HVA_FILE_EXTENSION);

                if (hvaData == null)
                {
                    Logger.Log($"WARNING: Building {buildingType.ININame} is missing .hva file for its turret {turretModelName + HVA_FILE_EXTENSION}! This will cause the game to crash!");
                    continue;
                }

                var vxlFile = new VxlFile(vxlData, turretModelName);
                var hvaFile = new HvaFile(hvaData, turretModelName);

                XNAPalette palette = buildingType.ArtConfig.TerrainPalette ? TheaterPalette : unitPalette;
                if (!string.IsNullOrWhiteSpace(buildingType.ArtConfig.Palette))
                    palette = GetPaletteOrFail(buildingType.ArtConfig.Palette + Theater.FileExtension[1..] + ".pal", !buildingType.ArtConfig.TerrainPalette);

                BuildingTurretModels[i] = new VoxelModel(graphicsDevice, vxlFile, hvaFile, palette,
                    buildingType.ArtConfig.Remapable, Constants.VoxelsAffectedByLighting, vplFile);
                loadedModels[turretModelName] = BuildingTurretModels[i];
            }

            Logger.Log("Finished loading building turrets' voxel models.");
        }

        public void ReadBuildingBarrelModels(List<BuildingType> buildingTypes)
        {
            Logger.Log("Loading building barrels' voxel models.");

            var loadedModels = new Dictionary<string, VoxelModel>();
            BuildingBarrelModels = new VoxelModel[buildingTypes.Count];

            for (int i = 0; i < buildingTypes.Count; i++)
            {
                var buildingType = buildingTypes[i];

                bool hasVoxelTurret = buildingType.Turret && buildingType.TurretAnimIsVoxel;
                bool hasShapeTurretAndVoxelBarrel = buildingType.Turret && !buildingType.TurretAnimIsVoxel &&
                                                    buildingType.BarrelAnimIsVoxel;

                if (!(hasVoxelTurret || hasShapeTurretAndVoxelBarrel))
                    continue;

                string artName = string.IsNullOrWhiteSpace(buildingType.Image) ? buildingType.ArtConfig.Image : buildingType.Image;
                if (string.IsNullOrEmpty(artName))
                    artName = buildingType.ININame;

                string barrelModelName = string.IsNullOrEmpty(buildingType.VoxelBarrelFile) ? artName + BARREL_FILE_SUFFIX : buildingType.VoxelBarrelFile;
                if (loadedModels.TryGetValue(barrelModelName, out VoxelModel loadedModel))
                {
                    BuildingBarrelModels[i] = loadedModel;
                    continue;
                }

                byte[] vxlData = fileManager.LoadFile(barrelModelName + VXL_FILE_EXTENSION);
                if (vxlData == null)
                    continue;

                byte[] hvaData = fileManager.LoadFile(barrelModelName + HVA_FILE_EXTENSION);

                if (hvaData == null)
                {
                    Logger.Log($"WARNING: Building {buildingType.ININame} is missing .hva file for its barrel {barrelModelName + HVA_FILE_EXTENSION}! This will cause the game to crash!");
                    continue;
                }

                var vxlFile = new VxlFile(vxlData, barrelModelName);
                var hvaFile = new HvaFile(hvaData, barrelModelName);

                XNAPalette palette = buildingType.ArtConfig.TerrainPalette ? TheaterPalette : unitPalette;
                if (!string.IsNullOrWhiteSpace(buildingType.ArtConfig.Palette))
                    palette = GetPaletteOrFail(buildingType.ArtConfig.Palette + Theater.FileExtension[1..] + ".pal", !buildingType.ArtConfig.TerrainPalette);

                BuildingBarrelModels[i] = new VoxelModel(graphicsDevice, vxlFile, hvaFile, palette,
                    buildingType.ArtConfig.Remapable, Constants.VoxelsAffectedByLighting, vplFile);
                loadedModels[barrelModelName] = BuildingBarrelModels[i];
            }

            Logger.Log("Finished loading building barrels' voxel models.");
        }

        public void ReadAnimTextures(List<AnimType> animTypes, List<BuildingType> buildingTypes)
        {
            Logger.Log("Loading animation textures.");

            var graphicsPreparationObject = new GraphicsPreparationClass();

            AnimTextures = new ShapeImage[animTypes.Count];

            for (int i = 0; i < animTypes.Count; i++)
            {
                var animType = animTypes[i];

                // Is this animation actually used as a building animation? If not, we can skip loading it.
                if (!buildingTypes.Exists(btype => btype.ArtConfig.TurretAnim == animType || Array.Exists(btype.ArtConfig.Anims, atype => atype == animType) || Array.Exists(btype.ArtConfig.PowerUpAnims, atype => atype == animType)))
                    continue;

                string shpFileName = string.IsNullOrWhiteSpace(animType.ArtConfig.Image) ? animType.ININame : animType.ArtConfig.Image;
                string loadedShpName = "";

                if (animType.ArtConfig.Theater)
                    shpFileName += Theater.FileExtension;
                else
                    shpFileName += SHP_FILE_EXTENSION;

                byte[] shpData = null;
                if (animType.ArtConfig.NewTheater)
                {
                    string newTheaterShpName = shpFileName.Substring(0, 1) + Theater.NewTheaterBuildingLetter + shpFileName.Substring(2);

                    shpData = fileManager.LoadFile(newTheaterShpName);
                    loadedShpName = newTheaterShpName;
                }

                // Support generic theater letter
                if (Constants.NewTheaterGenericBuilding && shpData == null)
                {
                    string newTheaterShpName = shpFileName.Substring(0, 1) + Constants.NewTheaterGenericLetter + shpFileName.Substring(2);

                    shpData = fileManager.LoadFile(newTheaterShpName);
                    loadedShpName = newTheaterShpName;
                }

                // The game can apparently fall back to the non-theater-specific SHP file name
                // if the theater-specific SHP is not found
                if (shpData == null)
                {
                    shpData = fileManager.LoadFile(shpFileName);
                    loadedShpName = shpFileName;

                    if (shpData == null)
                    {
                        continue;
                    }
                }

                // Palette override in RA2/YR
                // NOTE: Until we use indexed color rendering, we have to assume that a building
                // anim will only be used as a building anim (because it forces unit palette).

                var parentBuildingType = animType.ArtConfig.ParentBuildingType;
                bool useBuildingPalette = parentBuildingType != null && animType.ArtConfig.ShouldUseCellDrawer;
                XNAPalette palette = useBuildingPalette || animType.ArtConfig.AltPalette ? unitPalette : animPalette;

                if (parentBuildingType != null && parentBuildingType.ArtConfig.TerrainPalette)
                {
                    palette = TheaterPalette;
                }
                else if (useBuildingPalette && !string.IsNullOrWhiteSpace(parentBuildingType.ArtConfig.Palette))
                {
                    palette = GetPaletteOrDefault(parentBuildingType.ArtConfig.Palette + Theater.FileExtension[1..] + ".pal", palette, true);
                }
                else if (!useBuildingPalette && !string.IsNullOrWhiteSpace(animType.ArtConfig.CustomPalette))
                {
                    palette = GetPaletteOrDefault(
                        animType.ArtConfig.CustomPalette.Replace("~~~", Theater.FileExtension.Substring(1)),
                        palette, true);
                }

                var shpFile = new ShpFile(loadedShpName);
                shpFile.ParseFromBuffer(shpData);
                AnimTextures[i] = new ShapeImage(graphicsDevice, shpFile, shpData, palette,
                    !animType.ArtConfig.IsBuildingAnim, animType.ArtConfig.Remapable || animType.ArtConfig.IsBuildingAnim, graphicsPreparationObject);
            }

            lock (graphicsPreparationObjectLocker)
                graphicsPreparationObjects.Add(graphicsPreparationObject);

            graphicsPreparationObject.PostProcessAction = PostProcessPositionedTexture;

            Logger.Log("Finished loading animation textures.");
        }

        public void ReadUnitTextures(List<UnitType> unitTypes)
        {
            Logger.Log("Loading unit textures.");

            var graphicsPreparationObject = new GraphicsPreparationClass();

            var loadedTextures = new Dictionary<string, ShapeImage>();
            UnitTextures = new ShapeImage[unitTypes.Count];

            for (int i = 0; i < unitTypes.Count; i++)
            {
                var unitType = unitTypes[i];

                if (unitType.ArtConfig.Voxel)
                    continue;

                string shpFileName = string.IsNullOrWhiteSpace(unitType.Image) ? unitType.ININame : unitType.Image;
                shpFileName += SHP_FILE_EXTENSION;
                if (loadedTextures.TryGetValue(shpFileName, out ShapeImage loadedImage))
                {
                    UnitTextures[i] = loadedImage;
                    continue;
                }

                byte[] shpData = fileManager.LoadFile(shpFileName);

                if (shpData == null)
                    continue;

                var shpFile = new ShpFile(shpFileName);
                shpFile.ParseFromBuffer(shpData);

                // Palette override in RA2/YR
                // Only actually used in-game for vehicles with Phobos enabled.
                XNAPalette palette = unitPalette;
                if (!string.IsNullOrWhiteSpace(unitType.ArtConfig.Palette))
                    palette = GetPaletteOrDefault(unitType.ArtConfig.Palette + Theater.FileExtension[1..] + ".pal", palette, true);

                UnitTextures[i] = new ShapeImage(graphicsDevice, shpFile, shpData, palette, true, unitType.ArtConfig.Remapable, graphicsPreparationObject);
                loadedTextures[shpFileName] = UnitTextures[i];
            }

            lock (graphicsPreparationObjectLocker)
                graphicsPreparationObjects.Add(graphicsPreparationObject);

            graphicsPreparationObject.PostProcessAction = PostProcessPositionedTexture;

            Logger.Log("Finished loading unit textures.");
        }

        public void ReadUnitModels(List<UnitType> unitTypes)
        {
            Logger.Log("Loading unit voxel models.");

            var loadedModels = new Dictionary<string, VoxelModel>();
            UnitModels = new VoxelModel[unitTypes.Count];

            for (int i = 0; i < unitTypes.Count; i++)
            {
                var unitType = unitTypes[i];

                if (!unitType.ArtConfig.Voxel)
                    continue;

                string unitImage = string.IsNullOrWhiteSpace(unitType.Image) ? unitType.ININame : unitType.Image;
                if (loadedModels.TryGetValue(unitImage, out VoxelModel loadedModel))
                {
                    UnitModels[i] = loadedModel;
                    continue;
                }

                byte[] vxlData = fileManager.LoadFile(unitImage + VXL_FILE_EXTENSION);
                if (vxlData == null)
                    continue;

                byte[] hvaData = fileManager.LoadFile(unitImage + HVA_FILE_EXTENSION);

                if (hvaData == null)
                {
                    Logger.Log($"WARNING: Unit {unitType.ININame} is missing its .hva file {unitImage + HVA_FILE_EXTENSION}! This will cause the game to crash!");
                    continue;
                }

                var vxlFile = new VxlFile(vxlData, unitImage);
                var hvaFile = new HvaFile(hvaData, unitImage);

                // Palette override in RA2/YR
                // Only actually used in-game for vehicles with Phobos enabled.
                XNAPalette palette = unitPalette;
                if (!string.IsNullOrWhiteSpace(unitType.ArtConfig.Palette))
                    palette = GetPaletteOrDefault(unitType.ArtConfig.Palette + Theater.FileExtension[1..] + ".pal", palette, true);

                UnitModels[i] = new VoxelModel(graphicsDevice, vxlFile, hvaFile, palette, unitType.ArtConfig.Remapable,
                    Constants.VoxelsAffectedByLighting, vplFile);
                loadedModels[unitImage] = UnitModels[i];
            }

            Logger.Log("Finished loading unit voxel models.");
        }

        public void ReadUnitTurretModels(List<UnitType> unitTypes)
        {
            Logger.Log("Loading unit turrets' voxel models.");

            var loadedModels = new Dictionary<string, VoxelModel>();
            UnitTurretModels = new VoxelModel[unitTypes.Count];

            for (int i = 0; i < unitTypes.Count; i++)
            {
                var unitType = unitTypes[i];

                if (!unitType.Turret)
                    continue;

                string turretModelName = string.IsNullOrWhiteSpace(unitType.Image) ? unitType.ININame : unitType.Image;
                turretModelName += TURRET_FILE_SUFFIX;
                if (loadedModels.TryGetValue(turretModelName, out VoxelModel loadedModel))
                {
                    UnitTurretModels[i] = loadedModel;
                    continue;
                }

                byte[] vxlData = fileManager.LoadFile(turretModelName + VXL_FILE_EXTENSION);
                if (vxlData == null)
                    continue;

                byte[] hvaData = fileManager.LoadFile(turretModelName + HVA_FILE_EXTENSION);

                if (hvaData == null)
                {
                    Logger.Log($"WARNING: Unit {unitType.ININame} is missing .hva file for its turret {turretModelName + HVA_FILE_EXTENSION}! This will cause the game to crash!");
                    continue;
                }

                var vxlFile = new VxlFile(vxlData, turretModelName);
                var hvaFile = new HvaFile(hvaData, turretModelName);

                // Palette override in RA2/YR
                // Only actually used in-game for vehicles with Phobos enabled.
                XNAPalette palette = unitPalette;
                if (!string.IsNullOrWhiteSpace(unitType.ArtConfig.Palette))
                    palette = GetPaletteOrDefault(unitType.ArtConfig.Palette + Theater.FileExtension[1..] + ".pal", palette, true);

                UnitTurretModels[i] = new VoxelModel(graphicsDevice, vxlFile, hvaFile, palette, unitType.ArtConfig.Remapable,
                    Constants.VoxelsAffectedByLighting, vplFile);
                loadedModels[turretModelName] = UnitTurretModels[i];
            }

            Logger.Log("Finished loading unit turrets' voxel models.");
        }

        public void ReadUnitBarrelModels(List<UnitType> unitTypes)
        {
            Logger.Log("Loading unit barrels' voxel models.");

            var loadedModels = new Dictionary<string, VoxelModel>();
            UnitBarrelModels = new VoxelModel[unitTypes.Count];

            for (int i = 0; i < unitTypes.Count; i++)
            {
                var unitType = unitTypes[i];

                if (!unitType.Turret)
                    continue;

                string barrelModelName = string.IsNullOrWhiteSpace(unitType.Image) ? unitType.ININame : unitType.Image;
                barrelModelName += BARREL_FILE_SUFFIX;
                if (loadedModels.TryGetValue(barrelModelName, out VoxelModel loadedModel))
                {
                    UnitBarrelModels[i] = loadedModel;
                    continue;
                }

                byte[] vxlData = fileManager.LoadFile(barrelModelName + VXL_FILE_EXTENSION);
                if (vxlData == null)
                    continue;

                byte[] hvaData = fileManager.LoadFile(barrelModelName + HVA_FILE_EXTENSION);

                if (hvaData == null)
                {
                    Logger.Log($"WARNING: Unit {unitType.ININame} is missing .hva file for its barrel {barrelModelName + HVA_FILE_EXTENSION}! This will cause the game to crash!");
                    continue;
                }

                var vxlFile = new VxlFile(vxlData, barrelModelName);
                var hvaFile = new HvaFile(hvaData, barrelModelName);

                // Palette override in RA2/YR
                // Only actually used in-game for vehicles with Phobos enabled.
                XNAPalette palette = unitPalette;
                if (!string.IsNullOrWhiteSpace(unitType.ArtConfig.Palette))
                    palette = GetPaletteOrDefault(unitType.ArtConfig.Palette + Theater.FileExtension[1..] + ".pal", palette, true);

                UnitBarrelModels[i] = new VoxelModel(graphicsDevice, vxlFile, hvaFile, palette, unitType.ArtConfig.Remapable,
                    Constants.VoxelsAffectedByLighting, vplFile);
                loadedModels[barrelModelName] = UnitBarrelModels[i];
            }

            Logger.Log("Finished loading unit barrels' voxel models.");
        }

        public void ReadAircraftModels(List<AircraftType> aircraftTypes)
        {
            Logger.Log("Loading aircraft voxel models.");

            var loadedModels = new Dictionary<string, VoxelModel>();
            AircraftModels = new VoxelModel[aircraftTypes.Count];

            for (int i = 0; i < aircraftTypes.Count; i++)
            {
                var aircraftType = aircraftTypes[i];

                string aircraftImage = string.IsNullOrWhiteSpace(aircraftType.Image) ? aircraftType.ININame : aircraftType.Image;
                if (loadedModels.TryGetValue(aircraftImage, out VoxelModel loadedModel))
                {
                    AircraftModels[i] = loadedModel;
                    continue;
                }

                byte[] vxlData = fileManager.LoadFile(aircraftImage + VXL_FILE_EXTENSION);
                if (vxlData == null)
                    continue;

                byte[] hvaData = fileManager.LoadFile(aircraftImage + HVA_FILE_EXTENSION);

                if (hvaData == null)
                {
                    Logger.Log($"WARNING: Aircraft {aircraftType.ININame} is missing its .hva file {aircraftImage + HVA_FILE_EXTENSION}! This will cause the game to crash!");
                    continue;
                }

                var vxlFile = new VxlFile(vxlData, aircraftImage);
                var hvaFile = new HvaFile(hvaData, aircraftImage);

                // Palette override in RA2/YR
                XNAPalette palette = unitPalette;
                if (!string.IsNullOrWhiteSpace(aircraftType.ArtConfig.Palette))
                    palette = GetPaletteOrDefault(aircraftType.ArtConfig.Palette + Theater.FileExtension[1..] + ".pal", palette, true);

                AircraftModels[i] = new VoxelModel(graphicsDevice, vxlFile, hvaFile, palette, aircraftType.ArtConfig.Remapable,
                    Constants.VoxelsAffectedByLighting, vplFile);
                loadedModels[aircraftImage] = AircraftModels[i];
            }

            Logger.Log("Finished loading aircraft voxel models.");
        }

        public void ReadInfantryTextures(List<InfantryType> infantryTypes)
        {
            Logger.Log("Loading infantry textures.");

            var graphicsPreparationObject = new GraphicsPreparationClass();

            var loadedTextures = new Dictionary<string, ShapeImage>();
            InfantryTextures = new ShapeImage[infantryTypes.Count];

            for (int i = 0; i < infantryTypes.Count; i++)
            {
                var infantryType = infantryTypes[i];

                string image = string.IsNullOrWhiteSpace(infantryType.Image) ? infantryType.ININame : infantryType.Image;
                string shpFileName = string.IsNullOrWhiteSpace(infantryType.ArtConfig.Image) ? image : infantryType.ArtConfig.Image;
                shpFileName += SHP_FILE_EXTENSION;
                if (loadedTextures.TryGetValue(shpFileName, out ShapeImage loadedImage))
                {
                    InfantryTextures[i] = loadedImage;
                    continue;
                }

                if (infantryType.ArtConfig.Sequence == null)
                {
                    continue;
                }

                byte[] shpData = fileManager.LoadFile(shpFileName);

                if (shpData == null)
                    continue;

                var framesToLoad = new List<int>();
                const int FACING_COUNT = 8;
                var readySequence = infantryType.ArtConfig.Sequence.Ready;
                for (int j = 0; j < FACING_COUNT; j++)
                {
                    framesToLoad.Add(readySequence.StartFrame + (readySequence.FrameCount * readySequence.FacingMultiplier * j));
                }

                var shpFile = new ShpFile(shpFileName);
                shpFile.ParseFromBuffer(shpData);

                // Load shadow frames
                int regularFrameCount = framesToLoad.Count;
                for (int j = 0; j < regularFrameCount; j++)
                    framesToLoad.Add(framesToLoad[j] + (shpFile.FrameCount / 2));

                // Palette override in RA2/YR
                XNAPalette palette = unitPalette;
                if (!string.IsNullOrWhiteSpace(infantryType.ArtConfig.Palette))
                    palette = GetPaletteOrDefault(infantryType.ArtConfig.Palette + Theater.FileExtension[1..] + ".pal", palette, true);

                InfantryTextures[i] = new ShapeImage(graphicsDevice, shpFile, shpData, palette, true, infantryType.ArtConfig.Remapable, graphicsPreparationObject, framesToLoad);
                loadedTextures[shpFileName] = InfantryTextures[i];
            }

            lock (graphicsPreparationObjectLocker)
                graphicsPreparationObjects.Add(graphicsPreparationObject);

            graphicsPreparationObject.PostProcessAction = PostProcessPositionedTexture;

            Logger.Log("Finished loading infantry textures.");
        }

        public void ReadOverlayTextures(List<OverlayType> overlayTypes)
        {
            Logger.Log("Loading overlay textures.");

            var graphicsPreparationObject = new GraphicsPreparationClass();

            OverlayTextures = new ShapeImage[overlayTypes.Count];
            for (int i = 0; i < overlayTypes.Count; i++)
            {
                var overlayType = overlayTypes[i];

                string imageName = overlayType.ININame;
                if (overlayType.ArtConfig.Image != null)
                    imageName = overlayType.ArtConfig.Image;
                else if (overlayType.Image != null)
                    imageName = overlayType.Image;

                string pngFileName = imageName + PNG_FILE_EXTENSION;

                byte[] pngData = fileManager.LoadFile(pngFileName);

                if (pngData != null)
                {
                    // Load graphics as PNG

                    OverlayTextures[i] = new ShapeImage(graphicsDevice, null, null, null,
                        true, false, PositionedTextureFromBytes(pngData));
                }
                else
                {
                    // Load graphics as SHP

                    string loadedShpName = "";

                    byte[] shpData;

                    if (overlayType.ArtConfig.NewTheater)
                    {
                        string shpFileName = imageName + SHP_FILE_EXTENSION;
                        string newTheaterImageName = shpFileName.Substring(0, 1) + Theater.NewTheaterBuildingLetter + shpFileName.Substring(2);
                        
                        shpData = fileManager.LoadFile(newTheaterImageName);
                        loadedShpName = newTheaterImageName;

                        if (shpData == null)
                        {
                            newTheaterImageName = shpFileName.Substring(0, 1) + Constants.NewTheaterGenericLetter + shpFileName.Substring(2);
                            shpData = fileManager.LoadFile(newTheaterImageName);
                            loadedShpName = newTheaterImageName;
                        }
                    }
                    else
                    {
                        string fileExtension = overlayType.ArtConfig.Theater ? Theater.FileExtension : SHP_FILE_EXTENSION;
                        shpData = fileManager.LoadFile(imageName + fileExtension);
                        loadedShpName = imageName + fileExtension;
                    }

                    if (shpData == null)
                        continue;

                    var shpFile = new ShpFile(loadedShpName);
                    shpFile.ParseFromBuffer(shpData);
                    XNAPalette palette = TheaterPalette;
                    
                    if (overlayType.Wall || overlayType.IsVeinholeMonster)
                        palette = unitPalette;
                    else if (overlayType.Tiberium)
                        palette = Constants.TheaterPaletteForTiberium ? tiberiumPalette : unitPalette;
                    else if (overlayType.IsVeins)
                        palette = Constants.TheaterPaletteForVeins ? tiberiumPalette : unitPalette;

                    // Palette override for wall overlays in Phobos
                    if (overlayType.Wall && !string.IsNullOrWhiteSpace(overlayType.ArtConfig.Palette))
                        palette = GetPaletteOrDefault(overlayType.ArtConfig.Palette + Theater.FileExtension[1..] + ".pal", palette, true);

                    bool isRemapable = overlayType.Tiberium && !Constants.TheaterPaletteForTiberium;
                    bool affectedByLighting = !overlayType.Tiberium || Constants.TiberiumAffectedByLighting;

                    OverlayTextures[i] = new ShapeImage(graphicsDevice, shpFile, shpData, palette, affectedByLighting, isRemapable, graphicsPreparationObject);
                }
            }

            lock (graphicsPreparationObjectLocker)
                graphicsPreparationObjects.Add(graphicsPreparationObject);

            graphicsPreparationObject.PostProcessAction = PostProcessPositionedTexture;

            Logger.Log("Finished loading overlay textures.");
        }

        public void ReadSmudgeTextures(List<SmudgeType> smudgeTypes)
        {
            Logger.Log("Loading smudge textures.");

            SmudgeTextures = new ShapeImage[smudgeTypes.Count];
            for (int i = 0; i < smudgeTypes.Count; i++)
            {
                var smudgeType = smudgeTypes[i];

                string imageName = smudgeType.ININame;
                string fileExtension = smudgeType.Theater ? Theater.FileExtension : SHP_FILE_EXTENSION;
                string finalShpName = imageName + fileExtension;
                byte[] shpData = fileManager.LoadFile(finalShpName);

                if (shpData == null)
                    continue;

                var shpFile = new ShpFile(finalShpName);
                shpFile.ParseFromBuffer(shpData);
                XNAPalette palette = TheaterPalette;
                SmudgeTextures[i] = new ShapeImage(graphicsDevice, shpFile, shpData, palette, true);
            }

            Logger.Log("Finished loading smudge textures.");
        }

        private ShapeImage LoadShape(string name, XNAPalette palette, GraphicsPreparationClass graphicsPreparationObject)
        {
            byte[] data = fileManager.LoadFile("PIPS.SHP");
            if (data != null)
            {
                var shpFile = new ShpFile(name);
                shpFile.ParseFromBuffer(data);
                return new ShapeImage(graphicsDevice, shpFile, data, palette, false, false, graphicsPreparationObject);
            }

            return null;
        }

        public void ReadMiscTextures()
        {
            Logger.Log("Loading miscellaneous textures.");

            var graphicsPreparationObject = new GraphicsPreparationClass(100, 100);

            PipTextures = LoadShape("PIPS.SHP", palettePalette, graphicsPreparationObject);

            lock (graphicsPreparationObjectLocker)
                graphicsPreparationObjects.Add(graphicsPreparationObject);

            graphicsPreparationObject.PostProcessAction = PostProcessPositionedTexture;
        }

        public static int GetVeterancyFrame(int veteranLevel)
        {
            return (Constants.IsRA2YR, veteranLevel) switch
            {
                (true, >= 200) => 15,
                (true, >= 100) => 14,
                (false, >= 200) => 8,
                (false, >= 100) => 7,
                _ => -1,
            };
        }

        public void ApplyLightingToPalettes(MapColor lighting)
        {
            palettes.ForEach(p => p.ApplyLighting(lighting));
        }

        public void InvalidateVoxelCache()
        {
            Array.ForEach(BuildingTurretModels, m => m?.ClearFrames());
            Array.ForEach(BuildingBarrelModels, m => m?.ClearFrames());
            Array.ForEach(UnitModels, m => m?.ClearFrames());
            Array.ForEach(UnitTurretModels, m => m?.ClearFrames());
            Array.ForEach(UnitBarrelModels, m => m?.ClearFrames());
            Array.ForEach(AircraftModels, m => m?.ClearFrames());
        }

        public int GetOverlayFrameCount(OverlayType overlayType)
        {
            int frameCount = OverlayTextures[overlayType.Index].GetFrameCount();

            int lastValidFrame = -1;

            // We only consider non-blank frames as valid frames
            for (int i = 0; i < frameCount; i++)
            {
                var texture = OverlayTextures[overlayType.Index].GetFrame(i);
                if (texture != null && texture.Texture != null)
                    lastValidFrame = i;
            }

            // No blank overlay frame existed - return the full frame count divided by two (the rest are used up by shadows)
            if (lastValidFrame == frameCount - 1)
                return OverlayTextures[overlayType.Index].GetFrameCount() / 2;
            else
                return lastValidFrame + 1;
        }

        /// <summary>
        /// Frees up all memory used by the theater graphics textures
        /// (or more precisely, diposes them so the garbage collector can free them).
        /// Make sure no rendering is attempted afterwards!
        /// </summary>
        public void DisposeAll()
        {
            //var task1 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(TerrainObjectTextures));
            //var task2 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(BuildingTextures));
            var task3 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(BuildingTurretModels));
            var task4 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(BuildingBarrelModels));
            //var task5 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(UnitTextures));
            var task6 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(UnitModels));
            var task7 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(UnitTurretModels));
            var task8 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(UnitBarrelModels));
            var task9 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(AircraftModels));
            //var task10 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(InfantryTextures));
            //var task11 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(OverlayTextures));
            //var task12 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(SmudgeTextures));
            var task13 = Task.Factory.StartNew(() => { spriteSheets.ForEach(tex2D => tex2D.Dispose()); });
            //var task14 = Task.Factory.StartNew(() => DisposeObjectImagesFromArray(AnimTextures));
            //Task.WaitAll(task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11, task12, task13, task14);
            Task.WaitAll(task3, task4, task6, task7, task8, task9, task13);

            spriteSheets.Clear();
            terrainGraphicsList.Clear();
            mmTerrainGraphicsList.Clear();

            TerrainObjectTextures = null;
            BuildingTextures = null;
            BuildingTurretModels = null;
            BuildingBarrelModels = null;
            UnitTextures = null;
            UnitModels = null;
            UnitTurretModels = null;
            UnitBarrelModels = null;
            AircraftModels = null;
            InfantryTextures = null;
            OverlayTextures = null;
            SmudgeTextures = null;
            AnimTextures = null;

            foreach (ShapeImage alphaShape in AlphaImages.Values)
                alphaShape.Dispose();

            AlphaImages = null;

            PipTextures?.Dispose();
            PipTextures = null;

            palettes.ForEach(p => p.Dispose());
            alphaPalette.Dispose();
            palettes.Clear();
        }

        private void DisposeObjectImagesFromArray(IDisposable[] objImageArray)
        {
            Array.ForEach(objImageArray, objectImage => { if (objectImage != null) objectImage.Dispose(); });
            Array.Clear(objImageArray);
        }

        private XNAPalette GetPaletteOrFail(string paletteFileName, bool hasFullyBrightColors)
        {
            var existing = palettes.Find(p => p.Name == paletteFileName);
            if (existing != null)
                return existing;

            byte[] paletteData = fileManager.LoadFile(paletteFileName);
            if (paletteData == null)
                throw new KeyNotFoundException(paletteFileName + " not found from loaded MIX files!");

            var newPalette = new XNAPalette(paletteFileName, paletteData, graphicsDevice, hasFullyBrightColors);
            palettes.Add(newPalette);
            return newPalette;
        }

        private XNAPalette GetPaletteOrDefault(string paletteFileName, XNAPalette palette, bool hasFullyBrightColors)
        {
            var existing = palettes.Find(p => p.Name == paletteFileName);
            if (existing != null)
                return existing;

            byte[] paletteData = fileManager.LoadFile(paletteFileName);
            if (paletteData == null)
                return palette;

            var newPalette = new XNAPalette(paletteFileName, paletteData, graphicsDevice, hasFullyBrightColors);
            palettes.Add(newPalette);
            return newPalette;
        }

        private VplFile GetVplFile(string filename = "voxels.vpl")
        {
            byte[] vplData = fileManager.LoadFile(filename);
            if (vplData == null)
                throw new KeyNotFoundException(filename + " not found from loaded MIX files!");

            return new VplFile(vplData);
        }

        private PositionedTexture PositionedTextureFromBytes(byte[] data)
        {
            using (var memstream = new MemoryStream(data))
            {
                var tex2d = Texture2D.FromStream(graphicsDevice, memstream);

                // premultiply alpha
                Color[] colorData = new Color[tex2d.Width * tex2d.Height];
                tex2d.GetData(colorData);
                for (int i = 0; i < colorData.Length; i++)
                {
                    var color = colorData[i];
                    color.R = (byte)((color.R * color.A) / byte.MaxValue);
                    color.G = (byte)((color.G * color.A) / byte.MaxValue);
                    color.B = (byte)((color.B * color.A) / byte.MaxValue);
                    colorData[i] = color;
                }

                tex2d.SetData(colorData);

                return new PositionedTexture(tex2d.Width, tex2d.Height, 0, 0, tex2d, new Rectangle(0, 0, tex2d.Width, tex2d.Height));
            }
        }

        public int GetTileSetId(int uniqueTileIndex)
        {
            return GetTileImage(uniqueTileIndex).TileSetId;
        }
    }
}
