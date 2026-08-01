using Microsoft.Xna.Framework;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations;
using TSMapEditor.Rendering;

namespace TSMapEditor.AI;

public class MapObjectInfo
{
    public string RTTI { get; }
    public int X { get; }
    public int Y { get; }
    public string ININame { get; }

    public MapObjectInfo(string rtti, int x, int y, string iniName)
    {
        RTTI = rtti;
        X = x;
        Y = y;
        ININame = iniName;
    }
}

public class MapOverlayInfo : MapObjectInfo
{
    public MapOverlayInfo(int x, int y, string iniName, int frameId) : base(RTTIType.Overlay.ToString(), x, y, iniName)
    {
        FrameID = frameId;
    }

    public int FrameID { get; }
}

public class CellInfo
{
    public CellInfo(int x, int y, string tileSetName, int tileIndexInTileSet, int height, MapObjectInfo terrainObjectInfo, MapOverlayInfo overlayInfo)
    {
        X = x;
        Y = y;
        TileSetName = tileSetName;
        TileIndexInTileSet = tileIndexInTileSet;
        Height = height;
        TerrainObjectInfo = terrainObjectInfo;
        OverlayInfo = overlayInfo;
    }

    public int X { get; }
    public int Y { get; }
    public string TileSetName { get; }
    public int TileIndexInTileSet { get; }
    public int Height { get; }
    public MapObjectInfo TerrainObjectInfo { get; }
    public MapOverlayInfo OverlayInfo { get; }

    public static CellInfo FromMapCell(ITheater theater, MapTile mapTile)
    {
        int tileSetIndex = theater.GetTileSetId(mapTile.TileIndex);
        var tileSet = theater.Theater.TileSets[tileSetIndex];

        var terrainObjectInfo = mapTile.TerrainObject == null ? null : new MapObjectInfo(RTTIType.Terrain.ToString(), mapTile.TerrainObject.Position.X, mapTile.TerrainObject.Position.Y, mapTile.TerrainObject.TerrainType.ININame);
        var overlayInfo = mapTile.Overlay == null ? null : new MapOverlayInfo(mapTile.Overlay.Position.X, mapTile.Overlay.Position.Y, mapTile.Overlay.OverlayType.ININame, mapTile.Overlay.FrameIndex);

        return new CellInfo(mapTile.X, mapTile.Y, tileSet.SetName, mapTile.TileIndex - tileSet.StartTileIndex, mapTile.Level, terrainObjectInfo, overlayInfo);
    }
}

public class MapInfo
{
    public MapInfo(string theaterName, int width, int height)
    {
        TheaterName = theaterName;
        Width = width;
        Height = height;
    }

    public string TheaterName { get; }
    public int Width { get; }
    public int Height { get; }
}

/// <summary>
/// Facade that performs operations on the map for the Model Context Protocol component.
/// </summary>
public class MapFacade
{
    public MapFacade(Map map, MutationManager mutationManager)
    {
        this.map = map;
        this.mutationManager = mutationManager;
    }

    private readonly Map map;
    private readonly MutationManager mutationManager;

    public MapInfo GetMapInfo()
    {
        return new MapInfo(map.LoadedTheaterName, map.Size.X, map.Size.Y);
    }

    public int GetMapRevision()
    {
        return mutationManager.Revision;
    }

    public List<CellInfo> InspectRegion(Rectangle rectangle)
    {
        var returnValue = new List<CellInfo>();

        for (int y = rectangle.Y; y < rectangle.Bottom; y++)
        {
            for (int x = rectangle.X; x < rectangle.Right; x++)
            {
                Point2D coords = new Point2D(x, y);
                if (!map.IsCoordWithinMap(coords))
                {
                    continue;
                }

                var mapCell = map.GetTile(coords);

                returnValue.Add(CellInfo.FromMapCell(map.TheaterInstance, mapCell));
            }
        }

        return returnValue;
    }
}
