﻿using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TSMapEditor.GameMath;
using TSMapEditor.UI;

namespace TSMapEditor.Models
{
    public enum CliffSide
    {
        Front,
        Back
    }

    public class CliffConnectionPoint
    {
        public Vector2 CoordinateOffset { get; set; }
        public byte ConnectsTo { get; set; }
        // Swap the first and last 4 bits to then and then with another point to get the directions they can connect
        public byte ReversedConnectsTo => (byte)((ConnectsTo >> 4) + (0b11110000 & (ConnectsTo << 4)));
        public CliffSide Side { get; set; }

        public List<CliffAStarNode> ConnectTo(CliffAStarNode node, CliffTile tile)
        {
            var possibleNeighbors = tile.ConnectionPoints.Select(cp =>
            {
                (CliffConnectionPoint cp, List<Direction> dirs) connection = (cp, GetDirectionsInMask((byte)(cp.ReversedConnectsTo & ConnectsTo)));
                return connection;
            }).Where(connection => connection.dirs.Count > 0).ToList();

            var neighbors = new List<CliffAStarNode>();
            foreach (var neighbor in possibleNeighbors)
            {
                if (neighbor.cp.Side != node.Exit.Side)
                    continue;

                foreach (Direction dir in neighbor.dirs)
                {
                    Vector2 placementOffset = (Vector2)Helpers.VisualDirectionToPoint(dir) - neighbor.cp.CoordinateOffset;
                    Vector2 placementCoords = node.ExitCoords + placementOffset;

                    var exit = tile.GetExit(neighbor.cp.CoordinateOffset);
                    exit = (CliffConnectionPoint)exit.MemberwiseClone();

                    neighbors.Add(new CliffAStarNode(node, exit, placementCoords, tile));
                }
            }
            return neighbors;
        }

        public List<CliffAStarNode> GetConnections(CliffAStarNode node, List<CliffTile> tiles)
        {
            return tiles.SelectMany(tile => ConnectTo(node, tile)).ToList();
        }

        private List<Direction> GetDirectionsInMask(byte mask)
        {
            List <Direction> directions = new List<Direction>();

            for (int direction = 0; direction < (int)Direction.Count; direction++)
            {
                if ((mask & (byte)(0b10000000 >> direction)) > 0)
                    directions.Add((Direction)direction);
            }

            return directions;
        }

    }

    public class CliffAStarNode
    {
        private CliffAStarNode() {}

        public CliffAStarNode(CliffAStarNode parent, CliffConnectionPoint exit, Vector2 location, CliffTile tile)
        {
            Location = location;
            Tile = tile;

            Parent = parent;
            Exit = exit;
            Destination = Parent.Destination;
        }

        public static CliffAStarNode MakeStartNode(Vector2 location, Vector2 destination, CliffSide startingSide)
        {
            CliffConnectionPoint connectionPoint = new CliffConnectionPoint
            {
                ConnectsTo = 0b11111111,
                CoordinateOffset = new Vector2(0, 0),
                Side = startingSide
            };

            var startNode = new CliffAStarNode()
            {
                Location = location,
                Tile = null,

                Parent = null,
                Exit = connectionPoint,
                Destination = destination
            };

            return startNode;
        }

        public List<CliffAStarNode> GetNeighbors(List<CliffTile> tiles)
        {
            var neighbors = Exit.GetConnections(this, tiles);
            return neighbors;
        }

        ///// Tile Config

        /// <summary>
        /// Absolute world coordinates of the tile
        /// </summary>
        public Vector2 Location;

        /// <summary>
        /// Absolute world coordinates of the tile's exit
        /// </summary>
        public Vector2 ExitCoords => Location + Exit.CoordinateOffset;

        /// <summary>
        /// Tile Data
        /// </summary>
        public CliffTile Tile;

        ///// A* Stuff

        /// <summary>
        /// A* end point
        /// </summary>
        public Vector2 Destination;

        /// <summary>
        /// Where this tile connects to other tiles
        /// </summary>
        public CliffConnectionPoint Exit;

        // Distance from starting node
        public float GScore => Parent == null ? 0 : Parent.GScore + Helpers.VectorDistance(Parent.ExitCoords, ExitCoords);

        // Distance to end node
        public float HScore => Helpers.VectorDistance(Destination, ExitCoords);
        public float FScore => GScore + HScore;
        public CliffAStarNode Parent;
    }

    public class CliffTile
    {
        public string TileSet { get; set; }
        public int TileIndexInSet { get; set; }
        public List<CliffConnectionPoint> ConnectionPoints { get; set; }

        public CliffConnectionPoint GetExit(Vector2 entryOffset)
        {
            return ConnectionPoints.FirstOrDefault(cp => cp.CoordinateOffset != entryOffset) ?? ConnectionPoints.First();
        }
    }

    public class CliffType
    {
        public CliffType(IniFile iniConfig, string tileSet)
        {
            TileSet = tileSet;
            Tiles = new List<CliffTile>();

            foreach (var sectionName in iniConfig.GetSections())
            {
                var parts = sectionName.Split('.');
                if (parts.Length != 2 || parts[0] != tileSet || !int.TryParse(parts[1], out int tileIndexInSet))
                    continue;

                var iniSection = iniConfig.GetSection(sectionName);

                List<CliffConnectionPoint> connectionPoints = new List<CliffConnectionPoint>();

                // I was going to allow infinite connection points, but to avoid complications I'm limiting them to 2
                for (int i = 0; i < 2; i++)
                {
                    string coordsString = iniSection.GetStringValue($"ConnectionPoint{i}", null);
                    if (coordsString == null || !Regex.IsMatch(coordsString, "^\\d+?,\\d+?$"))
                        break;

                    var coordParts = coordsString.Split(',').Select(int.Parse).ToList();
                    Vector2 coords = new Vector2(coordParts[0], coordParts[1]);

                    string directionsString = iniSection.GetStringValue($"ConnectionPoint{i}.Directions", null);
                    if (directionsString == null || directionsString.Length != (int)Direction.Count || Regex.IsMatch(directionsString, "[^01]"))
                        break;

                    byte directions = Convert.ToByte(directionsString, 2);

                    string sideString = iniSection.GetStringValue($"ConnectionPoint{i}.Side", string.Empty);
                    CliffSide side = sideString.ToLower() switch
                    {
                        "front" => CliffSide.Front,
                        "back" => CliffSide.Back,
                        _ => throw new INIConfigException($"Cliff {sectionName} has an invalid Side {sideString}!")
                    };

                    connectionPoints.Add(new CliffConnectionPoint()
                    {
                        ConnectsTo = directions,
                        CoordinateOffset = coords,
                        Side = side
                    });
                }

                Tiles.Add(new CliffTile()
                {
                    ConnectionPoints = connectionPoints,
                    TileIndexInSet = tileIndexInSet
                });
            }
        }

        public string TileSet { get; set; }

        public List<CliffTile> Tiles { get; set; }

    }
}
