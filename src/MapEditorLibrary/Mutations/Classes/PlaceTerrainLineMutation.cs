using MapEditorLibrary.CCEngine.TileData;
using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;

namespace MapEditorLibrary.Mutations.Classes;

public class PlaceTerrainLineMutation : Mutation
{
    public PlaceTerrainLineMutation(IMutationTarget mutationTarget, MapTile source, Direction direction, int length, TileImage tile) : base(mutationTarget)
    {
        sourceTile = source;
        this.direction = direction;
        this.length = length;
        this.tile = tile;
        autoLATEnabled = mutationTarget.AutoLATEnabled;
        brushSize = mutationTarget.BrushSize;
        onlyPaintOnClearGround = mutationTarget.OnlyPaintOnClearGround;
    }

    private readonly MapTile sourceTile;
    private readonly Direction direction;
    private readonly int length;
    private readonly TileImage tile;
    private readonly bool autoLATEnabled;
    private readonly BrushSize brushSize;
    private readonly bool onlyPaintOnClearGround;
    private List<OriginalCellTerrainData> undoData = new List<OriginalCellTerrainData>();
    private HashSet<Point2D> undoDataCellCoords = new HashSet<Point2D>();
    private HashSet<Point2D> placedTerrainCellCoords = new HashSet<Point2D>();

    public override string GetDisplayString()
    {
        string directionString = Translate("Direction." + direction, Helpers.DirectionToName(direction));
        return string.Format(Translate(this, "DisplayString", "Place Terrain Line from {0} towards {1} with distance of {2}"), sourceTile.CoordsToPoint(), direction, length);
    }

    int GetSouthernmostSubTileOffset()
    {
        int maxY = 1;

        for (int i = 0; i < tile.SubTileCount; i++)
        {
            Point2D? subTileOffset = tile.GetSubTileCoordOffset(i);

            if (subTileOffset == null)
                continue;

            int southOffset = (subTileOffset.Value.X + subTileOffset.Value.Y) / 2;
            if (southOffset > maxY)
                maxY = southOffset;
        }

        return maxY;
    }

    int GetEasternmostSubTileOffset()
    {
        int maxEast = 1;

        for (int i = 0; i < tile.SubTileCount; i++)
        {
            Point2D? subTileOffset = tile.GetSubTileCoordOffset(i);

            if (subTileOffset == null)
                continue;

            int eastOffset = subTileOffset.Value.X - subTileOffset.Value.Y;

            if (eastOffset > maxEast)
                maxEast = eastOffset;
        }

        return maxEast;
    }

    int GetWesternmostSubTileOffset()
    {
        int maxWest = 1;

        for (int i = 0; i < tile.SubTileCount; i++)
        {
            Point2D? subTileOffset = tile.GetSubTileCoordOffset(i);

            if (subTileOffset == null)
                continue;

            int westOffset = subTileOffset.Value.Y - subTileOffset.Value.X;

            if (westOffset > maxWest)
                maxWest = westOffset;
        }

        return maxWest;
    }

    public override void Perform()
    {
        undoData.Clear();
        undoDataCellCoords.Clear();
        placedTerrainCellCoords.Clear();

        var brushOrigins = new List<Point2D>();
        int processedLength = 0;
        while (processedLength <= length)
        {
            Point2D coords = sourceTile.CoordsToPoint() + Helpers.VisualDirectionToPoint(direction).ScaleBy(processedLength);

            brushOrigins.Add(coords);
            brushSize.DoForBrushSize(offset =>
            {
                PlaceTile(coords + new Point2D(offset.X * tile.Width, offset.Y * tile.Height));
            });

            switch (direction)
            {
                case Direction.NE:
                case Direction.SW:
                    processedLength += tile.Height;
                    break;
                case Direction.NW:
                case Direction.SE:
                    processedLength += tile.Width;
                    break;
                case Direction.N:
                case Direction.S:
                    processedLength += GetSouthernmostSubTileOffset();
                    break;
                case Direction.E:
                    processedLength += GetEasternmostSubTileOffset();
                    break;
                case Direction.W:
                    processedLength += GetWesternmostSubTileOffset();
                    break;
            }
        }

        // Apply transitions after placing the entire stroke so overlapping brush
        // stamps cannot turn a transition tile into a later placement's input.
        if (autoLATEnabled)
        {
            foreach (Point2D coords in brushOrigins)
            {
                ApplyAutoLATForTilePlacement(tile, brushSize, coords, AddCellToUndoData);
            }
        }

        MutationTarget.InvalidateMap();
    }

    private void PlaceTile(Point2D coords)
    {
        // Process cells within tile foundation
        for (int i = 0; i < tile.SubTileCount; i++)
        {
            Point2D? subTileOffset = tile.GetSubTileCoordOffset(i);

            if (subTileOffset == null)
                continue;

            Point2D cellCoords = coords + subTileOffset.Value;
            var cell = Map.GetTile(cellCoords);
            if (cell == null || (onlyPaintOnClearGround && !cell.IsClearGround()))
                continue;

            if (!placedTerrainCellCoords.Add(cellCoords))
                continue;

            AddCellToUndoData(cellCoords);
            cell.ChangeTileIndex(tile.TileID, (byte)i);
            cell.Level = (byte)Math.Min(cell.Level + tile.GetSubTile(i).TmpImage.Height, Constants.MaxMapHeightLevel);

            RefreshCellLighting(cell);
        }
    }

    private void AddCellToUndoData(Point2D cellCoords)
    {
        if (!undoDataCellCoords.Add(cellCoords))
            return;

        var mapTile = Map.GetTileOrFail(cellCoords);

        undoData.Add(new OriginalCellTerrainData(cellCoords, mapTile.TileIndex, mapTile.SubTileIndex, mapTile.Level));
    }

    public override void Undo()
    {
        for (int i = 0; i < undoData.Count; i++)
        {
            OriginalCellTerrainData originalTerrainData = undoData[i];

            var mapCell = MutationTarget.Map.GetTile(originalTerrainData.CellCoords);
            if (mapCell != null)
            {
                mapCell.ChangeTileIndex(originalTerrainData.TileIndex, originalTerrainData.SubTileIndex);
                mapCell.Level = originalTerrainData.HeightLevel;
                RefreshCellLighting(mapCell);
            }
        }

        MutationTarget.InvalidateMap();
    }
}
