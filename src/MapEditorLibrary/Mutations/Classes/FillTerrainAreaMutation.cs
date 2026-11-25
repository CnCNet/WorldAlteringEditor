using MapEditorLibrary.CCEngine.TileData;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;

namespace MapEditorLibrary.Mutations.Classes;

/// <summary>
/// A mutation that fills an area with terrain.
/// </summary>
public partial class FillTerrainAreaMutation : Mutation
{
    public FillTerrainAreaMutation(IMutationTarget mutationTarget, MapTile target, TileImage tile,
        MapMirroring mirroring = null, int eventID = EventIdNone) : base(mutationTarget)
    {
        if (tile.Width > 1 || tile.Height > 1)
            throw new InvalidOperationException("Only 1x1 tiles can be used to fill areas.");

        TargetCell = target;
        Tile = tile;
        Mirroring = mirroring;
        EventID = eventID;
    }

    public MapTile TargetCell { get; }
    public TileImage Tile { get; }

    public MapMirroring Mirroring { get; }

    private OriginalCellTerrainData[] undoData;

    /// <summary>
    /// Finds mirrored fill start cells and checks that they match the source terrain.
    /// Each accepted seed uses its own elevation when finding the area to fill.
    /// </summary>
    public static IEnumerable<(Point2D Cell, bool CanFill)> GetMirroredSeeds(Map map, MapTile target, MapMirroring mirroring)
    {
        var theater = map.TheaterInstance;
        var sourceImage = theater.GetTile(target.TileIndex);
        byte terrainType = sourceImage.GetSubTile(target.SubTileIndex).TmpImage.TerrainType;
        Point2D source = target.CoordsToPoint();
        Point2D pivot = mirroring.GetDoubledMirroringPoint(map.Size);

        foreach (Point2D coords in mirroring.Transforms.Select(transform => MapMirroring.TransformCell(source, pivot, transform)).Distinct())
        {
            var cell = map.GetTile(coords);
            var image = cell == null ? null : theater.GetTile(cell.TileIndex);
            bool canFill = image != null && image.TileSetId == sourceImage.TileSetId &&
                image.GetSubTile(cell.SubTileIndex).TmpImage.TerrainType == terrainType;

            yield return (coords, canFill);
        }
    }

    private HashSet<Point2D> GetMirroredFillAreaTiles()
    {
        var cells = new HashSet<Point2D>();
        foreach (var (seed, canFill) in GetMirroredSeeds(Map, TargetCell, Mirroring))
        {
            if (canFill && !cells.Contains(seed))
                cells.UnionWith(Helpers.GetFillAreaTiles(Map.GetTileOrFail(seed), Map, Map.TheaterInstance));
        }

        return cells;
    }

    public override string GetDisplayString()
    {
        return string.Format(Translate(this, "DisplayString", 
            "Flood-fill terrain tiles at {0} with tile from set '{1}'"),
                TargetCell.CoordsToPoint(), MutationTarget.TheaterGraphicsData.Theater.TileSets[Tile.TileSetId].TranslatedName);
    }

    public override void Perform()
    {
        var originalData = new List<OriginalCellTerrainData>();

        // Discover all areas before changing any terrain so one fill cannot change another's bounds.
        var tilesToProcess = Mirroring == null
            ? Helpers.GetFillAreaTiles(TargetCell, Map, Map.TheaterInstance)
            : GetMirroredFillAreaTiles();

        // Process tiles
        foreach (Point2D cellCoords in tilesToProcess)
        {
            var cell = MutationTarget.Map.GetTile(cellCoords);
            originalData.Add(new OriginalCellTerrainData(cellCoords, cell.TileIndex, cell.SubTileIndex, cell.Level));

            cell.ChangeTileIndex(Tile.TileID, 0);
        }

        undoData = originalData.ToArray();
        MutationTarget.InvalidateMap();
    }

    public override void Undo()
    {
        foreach (var originalTerrainData in undoData)
        {
            var cell = MutationTarget.Map.GetTile(originalTerrainData.CellCoords);
            cell.ChangeTileIndex(originalTerrainData.TileIndex, originalTerrainData.SubTileIndex);
        }

        MutationTarget.InvalidateMap();
    }
}
