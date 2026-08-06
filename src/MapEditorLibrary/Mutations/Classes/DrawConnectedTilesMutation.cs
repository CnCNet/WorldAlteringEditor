using MapEditorLibrary.CCEngine.TileData;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Misc;
using MapEditorLibrary.Models;

namespace MapEditorLibrary.Mutations.Classes;

/// <summary>
/// Applies an already validated connected-tile placement plan.
/// </summary>
public class DrawConnectedTilesMutation : Mutation
{
    public DrawConnectedTilesMutation(IMutationTarget mutationTarget, ConnectedTilePlan plan,
        int randomSeed, byte extraHeight) : base(mutationTarget)
    {
        this.plan = plan ?? throw new ArgumentNullException(nameof(plan));
        this.randomSeed = randomSeed;

        var originTile = mutationTarget.Map.GetTile(plan.Origin);
        if (originTile == null)
            throw new ArgumentException("The connected tile plan origin is outside the map.", nameof(plan));

        originLevel = originTile.Level + extraHeight;
    }

    private struct ConnectedTileUndoData
    {
        public Point2D CellCoords;
        public int TileIndex;
        public byte SubTileIndex;
        public byte Level;
    }

    private readonly List<ConnectedTileUndoData> undoData = new List<ConnectedTileUndoData>();
    private readonly HashSet<Point2D> affectedCellCoords = new HashSet<Point2D>();
    private readonly ConnectedTilePlan plan;
    private readonly int originLevel;
    private readonly int randomSeed;

    public IReadOnlyCollection<Point2D> AffectedCellCoords => affectedCellCoords;
    public ConnectedTilePlan Plan => plan;

    public override string GetDisplayString()
    {
        return string.Format(Translate(this, "DisplayString",
            "Draw Connected Tiles of type {0}"), plan.Type.Name);
    }

    public override void Perform()
    {
        var random = new Random(randomSeed);
        ConnectedTile lastPlacedTile = null;
        int lastPlacedTileIndex = -1;

        // Preserve legacy placement order. Besides retaining seeded visual variation, placing from end
        // to start keeps overlapping TMP subtile behavior identical to the former parent-chain walk.
        for (int placementIndex = plan.Placements.Count - 1; placementIndex >= 0; placementIndex--)
        {
            ConnectedTilePlacement placement = plan.Placements[placementIndex];
            ConnectedTile connectedTile = placement.Tile;
            var tileSet = MutationTarget.Map.TheaterInstance.Theater.TileSets.Find(
                tileSet => tileSet.SetName == connectedTile.TileSetName && tileSet.AllowToPlace);

            if (tileSet == null)
                throw new INIConfigException($"Tile Set {connectedTile.TileSetName} not found when placing connected tiles!");

            int tileIndex;
            if (connectedTile.IndicesInTileSet.Count > 1 && lastPlacedTile == connectedTile)
            {
                tileIndex = connectedTile.IndicesInTileSet.GetRandomElementIndex(random, lastPlacedTileIndex);
            }
            else
            {
                tileIndex = connectedTile.IndicesInTileSet.GetRandomElementIndex(random, -1);
            }

            int tileIndexInSet = connectedTile.IndicesInTileSet[tileIndex];
            ITileImage tileImage = MutationTarget.TheaterGraphicsData.GetTile(tileSet.StartTileIndex + tileIndexInSet);

            PlaceTile(tileImage, placement.Location);

            lastPlacedTileIndex = tileIndex;
            lastPlacedTile = connectedTile;
        }

        MutationTarget.InvalidateMap();
    }

    private void PlaceTile(ITileImage tile, Point2D targetCellCoords)
    {
        if (tile == null)
            return;

        for (int i = 0; i < tile.SubTileCount; i++)
        {
            ISubTileImage image = tile.GetSubTile(i);
            if (image == null)
                continue;

            int cx = targetCellCoords.X + i % tile.Width;
            int cy = targetCellCoords.Y + i / tile.Width;

            var mapTile = MutationTarget.Map.GetTile(cx, cy);
            if (mapTile != null)
            {
                undoData.Add(new ConnectedTileUndoData
                {
                    CellCoords = new Point2D(cx, cy),
                    TileIndex = mapTile.TileIndex,
                    SubTileIndex = mapTile.SubTileIndex,
                    Level = mapTile.Level
                });
                affectedCellCoords.Add(new Point2D(cx, cy));

                mapTile.ChangeTileIndex(tile.TileID, (byte)i);
                mapTile.Level = (byte)Math.Min(originLevel + image.TmpImage.Height,
                    Constants.MaxMapHeightLevel);
                RefreshCellLighting(mapTile);
            }
        }
    }

    public override void Undo()
    {
        for (int i = undoData.Count - 1; i >= 0; i--)
        {
            ConnectedTileUndoData data = undoData[i];
            var mapTile = MutationTarget.Map.GetTile(data.CellCoords);

            if (mapTile != null)
            {
                mapTile.ChangeTileIndex(data.TileIndex, data.SubTileIndex);
                mapTile.Level = data.Level;
                RefreshCellLighting(mapTile);
            }
        }

        undoData.Clear();
        affectedCellCoords.Clear();
        MutationTarget.InvalidateMap();
    }
}
