using MapEditorLibrary.GameMath;
using MapEditorLibrary.Misc;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MapEditorLibrary.Models;

public enum ConnectedTileSide
{
    Front,
    Back
}

public readonly struct TileConnectionPoint
{
    /// <summary>
    /// Zero-based index of the connection point within its tile.
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Offset of this connection point relative to the tile's (0,0) point
    /// </summary>
    public Point2D CoordinateOffset { get; init; }

    /// <summary>
    /// Mask of bits determining which way the connection point "faces".
    /// Ordered in the same way as the Directions enum
    /// </summary>
    public byte ConnectionMask { get; init; }

    /// <summary>
    /// Connection mask with its first and last half swapped to bitwise and it with the opposing cliff's mask
    /// </summary>
    public byte ReversedConnectionMask => (byte)((ConnectionMask >> 4) + (0b11110000 & (ConnectionMask << 4)));

    /// <summary>
    /// List of tiles this connection point must connect to. RequiredTiles take priority over ForbiddenTiles
    /// </summary>
    public int[] RequiredTiles { get; init; }

    /// <summary>
    /// List of tiles this connection point cannot connect to. RequiredTiles take priority over ForbiddenTiles
    /// </summary>
    public int[] ForbiddenTiles { get; init; }

    /// <summary>
    /// Whether the connection point faces "backwards" or "forwards"
    /// </summary>
    public ConnectedTileSide Side { get; init; }
}

public class ConnectedTile
{
    public ConnectedTile(IniSection iniSection, int index)
    {
        Index = index;

        string indicesString = iniSection.GetStringValue("TileIndices", null);
        if (indicesString == null || !Regex.IsMatch(indicesString, "^((?:\\d+?,)*(?:\\d+?))$"))
            throw new INIConfigException($"Connected Tile {iniSection.SectionName} has invalid TileIndices list: {indicesString}!");

        string tileSet = iniSection.GetStringValue("TileSet", null);
        if (string.IsNullOrWhiteSpace(tileSet))
            throw new INIConfigException($"Connected Tile {iniSection.SectionName} has no TileSet!");

        TileSetName = tileSet;

        IndicesInTileSet = indicesString.Split(',').Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToList();

        var connectionPointValues = new SortedDictionary<int, string>();

        foreach (var keyValuePair in iniSection.Keys)
        {
            Match match = Regex.Match(keyValuePair.Key, "^ConnectionPoint(\\d+)$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success)
                continue;

            if (!int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int connectionPointIndex))
            {
                throw new INIConfigException(
                    $"Connected Tile {iniSection.SectionName} has an invalid connection point index in key {keyValuePair.Key}!");
            }

            if (!connectionPointValues.TryAdd(connectionPointIndex, keyValuePair.Value))
            {
                throw new INIConfigException(
                    $"Connected Tile {iniSection.SectionName} defines ConnectionPoint{connectionPointIndex} multiple times!");
            }
        }

        if (!connectionPointValues.ContainsKey(0))
            throw new INIConfigException($"Connected Tile {iniSection.SectionName} has no ConnectionPoint0!");

        int expectedConnectionPointIndex = 0;
        foreach (int connectionPointIndex in connectionPointValues.Keys)
        {
            if (connectionPointIndex != expectedConnectionPointIndex)
            {
                throw new INIConfigException(
                    $"Connected Tile {iniSection.SectionName} has no ConnectionPoint{expectedConnectionPointIndex}; " +
                    "connection point indices must be contiguous!");
            }

            expectedConnectionPointIndex++;
        }

        ConnectionPoints = new TileConnectionPoint[connectionPointValues.Count];

        for (int i = 0; i < ConnectionPoints.Length; i++)
        {
            string coordsString = connectionPointValues[i];
            if (coordsString == null || !Regex.IsMatch(coordsString, "^\\d+?,\\d+?$"))
                throw new INIConfigException($"Connected Tile {iniSection.SectionName} has invalid ConnectionPoint{i} value: {coordsString}!");

            Point2D coords = Point2D.FromString(coordsString);

            string directionsString = iniSection.GetStringValue($"ConnectionPoint{i}.Directions", null);
            string[] directionParts = directionsString?.Split(',') ?? Array.Empty<string>();
            byte directions = 0;

            // Try parsing the string as a comma-separated list of named directions
            if (directionParts.Length > 0)
            {
                foreach (string part in directionParts)
                {
                    byte dir = DirectionFromString(part);
                    if (dir != byte.MaxValue)
                        directions |= dir;
                }
            }
            
            // We failed to read any named directions, try as a bit mask
            if (directions == 0)
            {
                if (directionsString == null || directionsString.Length != (int)Direction.Count || Regex.IsMatch(directionsString, "[^01]"))
                    throw new INIConfigException($"Connected Tile {iniSection.SectionName} has invalid ConnectionPoint{i}.Directions value: {directionsString}!");

                directions = Convert.ToByte(directionsString, 2);
            }

            string sideString = iniSection.GetStringValue($"ConnectionPoint{i}.Side", string.Empty);
            ConnectedTileSide side = sideString.ToLowerInvariant() switch
            {
                "front" => ConnectedTileSide.Front,
                "back" => ConnectedTileSide.Back,
                "" => ConnectedTileSide.Front,
                _ => throw new INIConfigException($"Connected Tile {iniSection.SectionName} has an invalid ConnectionPoint{i}.Side value: {sideString}!")
            };

            int[] requiredTiles, forbiddenTiles;

            var requiredTilesList = iniSection.GetListValue($"ConnectionPoint{i}.RequiredTiles", ',',
                value => int.Parse(value, CultureInfo.InvariantCulture));

            if (requiredTilesList.Count > 0)
            {
                requiredTiles = requiredTilesList.ToArray();
                forbiddenTiles = Array.Empty<int>();
            }
            else
            {
                var forbiddenTilesList = iniSection.GetListValue($"ConnectionPoint{i}.ForbiddenTiles", ',',
                    value => int.Parse(value, CultureInfo.InvariantCulture));

                forbiddenTiles = forbiddenTilesList.ToArray();
                requiredTiles = Array.Empty<int>();
            }

            ConnectionPoints[i] = new TileConnectionPoint
            {
                Index = i,
                ConnectionMask = directions,
                CoordinateOffset = coords,
                Side = side,
                RequiredTiles = requiredTiles,
                ForbiddenTiles = forbiddenTiles
            };
        }

        if (iniSection.KeyExists("Foundation"))
        {
            string foundationString = iniSection.GetStringValue("Foundation", string.Empty);
            if (!Regex.IsMatch(foundationString, "^((?:\\d+?,\\d+?\\|)*(?:\\d+?,\\d+?))$"))
                throw new INIConfigException($"Connected Tile {iniSection.SectionName} has an invalid Foundation: {foundationString}!");

            Foundation = foundationString.Split("|").Select(Point2D.FromString).ToHashSet();
        }

        ExtraPriority = -iniSection.GetIntValue("ExtraPriority", IsStraight(ConnectionPoints) ? -1 : 0); // negated because sorting is in ascending order by default, but it's more intuitive to have larger numbers be more important
        DistanceModifier = iniSection.GetIntValue("DistanceModifier", IsDiagonal(ConnectionPoints) ? -3 : 0);
    }

    /// <summary>
    /// Tile's in-editor index
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// The name of the tile's Tile Set
    /// </summary>
    public string TileSetName { get; set; }

    /// <summary>
    /// Indices of tiles relative to the Tile Set
    /// </summary>
    public List<int> IndicesInTileSet { get; set; }

    /// <summary>
    /// Places this tile connects to other tiles
    /// </summary>
    public TileConnectionPoint[] ConnectionPoints { get; set; }

    /// <summary>
    /// Whether this tile has one connection point and can cap an open connected-tile path.
    /// </summary>
    public bool IsEndingPiece => ConnectionPoints.Length == 1;

    /// <summary>
    /// Whether this tile has two connection points and can be used as a linear path segment.
    /// </summary>
    public bool IsLinear => ConnectionPoints.Length == 2;

    /// <summary>
    /// Whether this tile has three or more connection points and can form a junction.
    /// </summary>
    public bool IsJunction => ConnectionPoints.Length >= 3;

    /// <summary>
    /// Set of all relative cell coordinates this tile occupies
    /// </summary>
    public HashSet<Point2D> Foundation { get; set; }

    /// <summary>
    /// Extra priority to be used as a secondary key when sorting tiles
    /// </summary>
    public int ExtraPriority { get; set; }

    /// <summary>
    /// A distance modifier added directly to the FScore. Use with caution!
    /// </summary>
    public int DistanceModifier { get; set; }

    private bool IsStraight(TileConnectionPoint[] connectionPoints)
    {
        if (connectionPoints.Length != 2)
            return false;

        int mask = connectionPoints[0].ConnectionMask & connectionPoints[1].ReversedConnectionMask;
        return mask > 0;
    }

    private bool IsDiagonal(TileConnectionPoint[] connectionPoints)
    {
        if (connectionPoints.Length != 2)
            return false;

        var directions = Helpers.GetDirectionsInMask((byte)(connectionPoints[0].ConnectionMask &
                                                            connectionPoints[1].ReversedConnectionMask));

        return directions.Contains(Direction.E) ||
               directions.Contains(Direction.S) ||
               directions.Contains(Direction.W) ||
               directions.Contains(Direction.N);
    }

    private static byte DirectionFromString(string str)
    {
        switch (str.Trim().ToUpperInvariant())
        {
            case "NORTH":
            case "TOPRIGHT":
                return 1 << 7;

            case "NORTHEAST":
            case "RIGHT":
                return 1 << 6;

            case "EAST":
            case "BOTTOMRIGHT":
                return 1 << 5;

            case "SOUTHEAST":
            case "BOTTOM":
                return 1 << 4;

            case "SOUTH":
            case "BOTTOMLEFT":
                return 1 << 3;

            case "SOUTHWEST":
            case "LEFT":
                return 1 << 2;

            case "WEST":
            case "TOPLEFT":
                return 1 << 1;

            case "NORTHWEST":
            case "TOP":
                return 1 << 0;
        }

        return byte.MaxValue;
    }
}

public class ConnectedTileType
{
    public static ConnectedTileType FromIniSection(IniFile iniFile, string sectionName)
    {
        IniSection cliffSection = iniFile.GetSection(sectionName);
        if (cliffSection == null)
            return null;

        string cliffName = cliffSection.GetStringValue("Name", null);

        if (string.IsNullOrEmpty(cliffName))
            return null;

        var allowedTheaters = cliffSection.GetListValue("AllowedTheaters", ',', s => s);

        bool frontOnly = cliffSection.GetBooleanValue("FrontOnly", false);

        Color? color = null;
        if (cliffSection.KeyExists("Color"))
            color = cliffSection.GetColorValue("Color", Microsoft.Xna.Framework.Color.White);

        return new ConnectedTileType(iniFile, sectionName, cliffName, frontOnly, allowedTheaters, color);
    }

    private ConnectedTileType(IniFile iniFile, string iniName, string name, bool frontOnly, List<string> allowedTheaters, Color? color)
    {
        IniName = iniName;
        Name = Translate(this, iniName, name);
        AllowedTheaters = allowedTheaters;
        FrontOnly = frontOnly;
        Color = color;

        Tiles = new List<ConnectedTile>();

        foreach (var sectionName in iniFile.GetSections())
        {
            var parts = sectionName.Split('.');
            if (parts.Length != 2 || parts[0] != IniName || !int.TryParse(parts[1], out int index))
                continue;

            if (Tiles.Exists(tile => tile.Index == index))
            {
                throw new INIConfigException(
                    $"Connected Tile {iniName} has multiple tiles with the same index {index}!");
            }

            Tiles.Add(new ConnectedTile(iniFile.GetSection(sectionName), index));
        }
    }

    public string IniName { get; }
    public string Name { get; }
    public bool FrontOnly { get; }
    public Color? Color { get; set; }
    public List<string> AllowedTheaters { get; set; }
    public List<ConnectedTile> Tiles { get; }
    public bool SupportsEndPieces => Tiles.Exists(tile => tile.IsEndingPiece);

    /// <summary>
    /// Determines whether the connected tile type is properly configured and usable in the editor.
    /// Intentionally never assigned to anything else in debug mode - but is in release mode.
    /// </summary>
    public bool IsLegal { get; set; } = true;
}
