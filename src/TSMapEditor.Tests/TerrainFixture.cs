using MapEditorLibrary;
using MapEditorLibrary.CCEngine;
using MapEditorLibrary.CCEngine.TileData;
using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Graphics;
using MapEditorLibrary.Models;
using MapEditorLibrary.Models.Enums;
using MapEditorLibrary.Mutations;

namespace TSMapEditor.Tests;

internal class TerrainFixture : IMutationTarget, ITheater
{
    internal const int Size = 64;

    internal TerrainFixture(int brushWidth, int brushHeight, int tileWidth = 1, int tileHeight = 1,
        bool autoLAT = false, byte tileHeightOffset = 0, int missingSubTile = -1)
    {
        AutoLATType.InitArray();
        BrushSize = new BrushSize(brushWidth, brushHeight);
        AutoLATEnabled = autoLAT;
        Map = new Map();
        typeof(Map).GetProperty(nameof(Map.EditorConfig)).SetValue(Map, new EditorConfig());
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
                Map.Tiles[y][x] = new MapTile { X = (short)x, Y = (short)y, Level = 3 };
        }

        var preparation = new GraphicsPreparationClass(512, 512);
        images.Add(CreateTile(preparation, 1, 1, 0, 0, 0, -1));
        Tile = CreateTile(preparation, tileWidth, tileHeight, 1, 1, tileHeightOffset, missingSubTile);
        images.Add(Tile);
        for (int i = 0; i < 16; i++)
            images.Add(CreateTile(preparation, 1, 1, 2, i + 2, 0, -1));

        Theater = new Theater("Fixture");
        var clear = new TileSet(0) { StartTileIndex = 0, LoadedTileCount = 1 };
        var ground = new TileSet(1) { StartTileIndex = 1, LoadedTileCount = 1 };
        var transition = new TileSet(2) { StartTileIndex = 2, LoadedTileCount = 16 };
        Theater.TileSets.AddRange(new[] { clear, ground, transition });
        Theater.LATGrounds.Add(new LATGround("Fixture LAT", ground, transition, clear, null));
        Map.TheaterInstance = this;
    }

    private readonly List<MGTileImage> images = new();
    internal MGTileImage Tile { get; }
    public Map Map { get; }
    public ITheater TheaterGraphicsData => this;
    public House ObjectOwner => null;
    public BrushSize BrushSize { get; set; }
    public Randomizer Randomizer { get; } = new();
    public bool AutoLATEnabled { get; set; }
    public LightingPreviewMode LightingPreviewState => LightingPreviewMode.NoLighting;
    public bool LightDisabledLightSources => false;
    public bool OnlyPaintOnClearGround => false;
    public Theater Theater { get; }
    public int TileCount => images.Count;
    public int GetTileSetId(int id) => images[id].TileSetId;
    public ITileImage GetTile(int id) => images[id];
    public int GetOverlayFrameCount(OverlayType overlayType) => 0;
    public void AddRefreshPoint(Point2D point, int size = 10) { }
    public void InvalidateMap() { }

    internal string[] Snapshot()
    {
        var snapshot = new string[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                MapTile cell = Map.GetTile(x, y);
                snapshot[y * Size + x] = $"{cell.TileIndex}:{cell.SubTileIndex}:{cell.Level}";
            }
        }

        return snapshot;
    }

    internal HashSet<Point2D> PaintedCells()
    {
        var cells = new HashSet<Point2D>();
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (Map.GetTile(x, y).TileIndex != 0)
                    cells.Add(new Point2D(x, y));
            }
        }

        return cells;
    }

    private static MGTileImage CreateTile(GraphicsPreparationClass preparation, int width, int height,
        int tileSetId, int tileId, byte heightOffset, int missingSubTile)
    {
        var subTiles = new MGSubTileImage[width * height];
        for (int i = 0; i < subTiles.Length; i++)
        {
            if (i == missingSubTile)
                continue;

            var data = new byte[52 + Constants.TileColorBufferSize];
            BitConverter.GetBytes((i % width - i / width) * Constants.CellSizeX / 2).CopyTo(data, 0);
            BitConverter.GetBytes((i % width + i / width) * Constants.CellSizeY / 2).CopyTo(data, 4);
            data[40] = heightOffset;
            using var stream = new MemoryStream(data);
            subTiles[i] = new MGSubTileImage(new TmpImage(stream, "fixture.tmp"), preparation, null, tileSetId);
        }

        return new MGTileImage(width, height, tileSetId, 0, tileId, subTiles);
    }
}
