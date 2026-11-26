using MapEditorLibrary.CCEngine;
using MapEditorLibrary.CCEngine.TileData;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Misc;

namespace MapEditorLibrary.Models;

/// <summary>
/// Finds open connection points on terrain already on the map.
/// </summary>
public sealed class ConnectedTileConnectionFinder
{
    public readonly record struct TerrainCell(int TileIndex, byte SubTileIndex, byte Level);

    private readonly record struct Probe(ConnectedTile Tile, int PointIndex, ITileImage Image);

    private readonly Dictionary<(Point2D Offset, int TileIndex, byte SubTileIndex), List<Probe>> probes = new();
    private readonly HashSet<int> tileIndices = new();
    private readonly Point2D[] probeOffsets;
    private readonly Func<Point2D, TerrainCell?> getTerrain;

    public ConnectedTileConnectionFinder(ConnectedTileType type, Theater theater,
        Func<int, ITileImage> getTileImage, Func<Point2D, TerrainCell?> getTerrain)
    {
        this.getTerrain = getTerrain;

        foreach (ConnectedTile tile in type.Tiles)
        {
            var tileSet = theater.TileSets.Find(tileSet => tileSet.SetName == tile.TileSetName && tileSet.AllowToPlace)
                ?? throw new INIConfigException($"Tile Set {tile.TileSetName} not found for connected tiles.");

            foreach (int indexInSet in tile.IndicesInTileSet)
            {
                if (indexInSet >= tileSet.LoadedTileCount)
                    continue;

                int tileIndex = tileSet.StartTileIndex + indexInSet;
                ITileImage image = getTileImage(tileIndex);
                tileIndices.Add(tileIndex);

                foreach (TileConnectionPoint point in tile.ConnectionPoints)
                {
                    // A point may lie on an empty subtile, so use the closest visible
                    // subtile to identify its tile and reconstruct the tile's origin.
                    int bestIndex = -1;
                    int bestDistance = int.MaxValue;
                    Point2D bestOffset = Point2D.Zero;
                    for (int i = 0; i < image.SubTileCount; i++)
                    {
                        if (image.GetSubTileCoordOffset(i) is not Point2D offset)
                            continue;

                        Point2D delta = offset - point.CoordinateOffset;
                        int distance = delta.X * delta.X + delta.Y * delta.Y;
                        if (distance >= bestDistance)
                            continue;

                        bestIndex = i;
                        bestDistance = distance;
                        bestOffset = delta;
                        if (distance == 0)
                            break;
                    }

                    if (bestIndex < 0 || point.ConnectionMask == 0)
                        continue;

                    var key = (bestOffset, tileIndex, (byte)bestIndex);
                    if (!probes.TryGetValue(key, out var matches))
                    {
                        matches = new List<Probe>();
                        probes.Add(key, matches);
                    }

                    matches.Add(new Probe(tile, point.Index, image));
                }
            }
        }

        probeOffsets = probes.Keys.Select(key => key.Offset).Distinct()
            .OrderBy(offset => offset.X * offset.X + offset.Y * offset.Y).ToArray();
    }

    public bool ContainsTile(int tileIndex) => tileIndices.Contains(tileIndex);

    public ConnectedTileAnchor? FindOpenConnection(Point2D cell, byte directionMask = byte.MaxValue)
    {
        foreach (ConnectedTileAnchor anchor in FindConnections(cell))
        {
            if ((anchor.ConnectionPoint.ConnectionMask & directionMask) != 0 && IsOpen(anchor))
                return anchor;
        }

        return null;
    }

    private IEnumerable<ConnectedTileAnchor> FindConnections(Point2D cell)
    {
        foreach (Point2D offset in probeOffsets)
        {
            if (getTerrain(cell + offset) is not TerrainCell terrain ||
                !probes.TryGetValue((offset, terrain.TileIndex, terrain.SubTileIndex), out var matches))
            {
                continue;
            }

            foreach (Probe probe in matches)
            {
                Point2D location = cell - probe.Tile.ConnectionPoints[probe.PointIndex].CoordinateOffset;
                int baseLevel = GetBaseLevel(probe.Image, terrain, location);
                yield return new ConnectedTileAnchor(probe.Tile, location, probe.PointIndex, baseLevel);
            }
        }
    }

    private int GetBaseLevel(ITileImage image, TerrainCell terrain, Point2D location)
    {
        int subTileHeight = image.GetSubTile(terrain.SubTileIndex).TmpImage.Height;
        if (terrain.Level == Constants.MaxMapHeightLevel && subTileHeight > 0)
        {
            // A raised subtile can be clipped at the height limit. Prefer an
            // unclipped measurement from another surviving cell of the same tile.
            for (int i = 0; i < image.SubTileCount; i++)
            {
                if (image.GetSubTileCoordOffset(i) is Point2D offset &&
                    getTerrain(location + offset) is TerrainCell other && other.TileIndex == terrain.TileIndex &&
                    other.SubTileIndex == i && other.Level < Constants.MaxMapHeightLevel)
                {
                    int otherBaseLevel = other.Level - image.GetSubTile(i).TmpImage.Height;
                    if (otherBaseLevel + subTileHeight >= Constants.MaxMapHeightLevel)
                        return otherBaseLevel;
                }
            }
        }

        return Math.Max(0, terrain.Level - subTileHeight);
    }

    private bool IsOpen(ConnectedTileAnchor anchor)
    {
        Point2D cell = anchor.Coordinates;
        TileConnectionPoint point = anchor.ConnectionPoint;

        for (int i = 0; i < (int)Direction.Count; i++)
        {
            if ((point.ConnectionMask & (0b10000000 >> i)) == 0)
                continue;

            Point2D neighbor = cell + Helpers.VisualDirectionToPoint((Direction)i);
            foreach (ConnectedTileAnchor other in FindConnections(neighbor))
            {
                if (anchor.Location != other.Location && anchor.BaseLevel == other.BaseLevel &&
                    ConnectedTilePlanner.ConnectionPointsMeet(cell, point, neighbor, other.ConnectionPoint))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
