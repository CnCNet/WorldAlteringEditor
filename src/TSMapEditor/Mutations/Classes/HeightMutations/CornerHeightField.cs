using System;
using System.Collections.Generic;
using TSMapEditor.CCEngine;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;

namespace TSMapEditor.Mutations.Classes.HeightMutations
{
    /// <summary>
    /// The direction in which a flood-fill smoothing pass is allowed to move cell corners.
    /// </summary>
    public enum HeightFloodMode
    {
        /// <summary>Only raise corners that are too low (used when raising ground).</summary>
        Up,

        /// <summary>Only lower corners that are too high (used when lowering ground).</summary>
        Down,

        /// <summary>
        /// Move corners either way toward their neighbours (used when flattening ground).
        /// Implemented as a downward pass followed by an upward pass so that every corner
        /// moves monotonically and the field always converges.
        /// </summary>
        Both
    }

    /// <summary>
    /// A transient cell-corner height field used for "smart" ground-height smoothing.
    ///
    /// Ported from the original Tiberian Sun terrain smoothing system (smooth.cpp).
    /// Instead of operating on per-cell height levels and guessing ramps from neighbour
    /// patterns, this assigns a height to every cell <i>corner</i>, smooths the corner
    /// field so neighbouring corners never differ by more than the allowed slope, and then
    /// derives each cell's ramp as a pure function of its four corner heights.
    ///
    /// Heights are expressed in whole map height levels: a normal ramp raises a corner by
    /// one level, a steep ramp raises its far corner by two levels.
    /// </summary>
    public class CornerHeightField
    {
        /// <summary>
        /// Corner point offsets from a cell, in the order used by <see cref="RampCornerHeights"/>.
        /// Verified against RaiseGroundMutationBase.CreateSmallHill: index 0..3 are the
        /// cell's NW, NE, SE and SW corner points respectively.
        /// </summary>
        public static readonly Point2D[] CornerOffsets =
        {
            new Point2D(0, 0), new Point2D(1, 0), new Point2D(1, 1), new Point2D(0, 1)
        };

        // Per-ramp corner heights in level units, in CornerOffsets order, indexed by RampType.
        // Ported verbatim from the game's _ramptypes table (smooth.cpp): HALFHEIGHT -> 1,
        // FULLHEIGHT -> 2. Rows 19/20 (DoubleUp/DownNWSE) duplicate 17/18 and exist only so
        // existing tiles of those types reconstruct correctly; they are never emitted.
        private static readonly int[][] RampCornerHeights =
        {
            new[] { 0, 0, 0, 0 }, // None
            new[] { 0, 1, 1, 0 }, // West
            new[] { 0, 0, 1, 1 }, // North
            new[] { 1, 0, 0, 1 }, // East
            new[] { 1, 1, 0, 0 }, // South
            new[] { 0, 0, 1, 0 }, // CornerNW
            new[] { 0, 0, 0, 1 }, // CornerNE
            new[] { 1, 0, 0, 0 }, // CornerSE
            new[] { 0, 1, 0, 0 }, // CornerSW
            new[] { 0, 1, 1, 1 }, // MidNW
            new[] { 1, 0, 1, 1 }, // MidNE
            new[] { 1, 1, 0, 1 }, // MidSE
            new[] { 1, 1, 1, 0 }, // MidSW
            new[] { 0, 1, 2, 1 }, // SteepSE
            new[] { 1, 0, 1, 2 }, // SteepSW
            new[] { 2, 1, 0, 1 }, // SteepNW
            new[] { 1, 2, 1, 0 }, // SteepNE
            new[] { 0, 1, 0, 1 }, // DoubleUpSWNE
            new[] { 1, 0, 1, 0 }, // DoubleDownSWNE
            new[] { 0, 1, 0, 1 }, // DoubleUpNWSE   (duplicate of DoubleUpSWNE)
            new[] { 1, 0, 1, 0 }, // DoubleDownNWSE (duplicate of DoubleDownSWNE)
        };

        // The highest ramp index that may be emitted. Rows above this duplicate earlier rows.
        private const int LastEmittedRamp = 18;

        // Reverse lookup: normalized 4-corner pattern (base-3 key) -> RampType. Built from
        // rows 0..18 only, first match wins, so the ambiguous double patterns
        // {0,1,0,1}/{1,0,1,0} canonically resolve to the SWNE variants (17/18), matching
        // the game's emission loop (for ramp = 0..18).
        private static readonly Dictionary<int, RampType> RampByCornerPattern = BuildReverseLookup();

        public CornerHeightField(Map map, int cellMinX, int cellMinY, int cellMaxX, int cellMaxY)
        {
            this.map = map;
            rampTileSetStart = map.TheaterInstance.Theater.RampTileSet.StartTileIndex;

            // A single height change can ripple outward up to MaxMapHeightLevel cells (each
            // level of difference pushes the smoothing one cell further). Expand the working
            // region by that much (+2 slack) so a flood started inside never needs to
            // reference a corner outside the region.
            int margin = Constants.MaxMapHeightLevel + 2;
            originX = Math.Max(0, cellMinX - margin);
            originY = Math.Max(0, cellMinY - margin);
            int regionMaxCellX = cellMaxX + margin;
            int regionMaxCellY = cellMaxY + margin;

            // Cells [originX..regionMaxCellX] own corner points [originX..regionMaxCellX + 1].
            pointWidth = (regionMaxCellX - originX) + 2;
            pointHeight = (regionMaxCellY - originY) + 2;

            heights = new int[pointWidth, pointHeight];
            rigid = new bool[pointWidth, pointHeight];
            done = new bool[pointWidth, pointHeight];
            hasOwner = new bool[pointWidth, pointHeight];
        }

        private readonly Map map;
        private readonly int rampTileSetStart;

        private readonly int originX;
        private readonly int originY;
        private readonly int pointWidth;
        private readonly int pointHeight;

        private readonly int[,] heights;
        private readonly bool[,] rigid;
        private readonly bool[,] done;
        private readonly bool[,] hasOwner;

        private readonly Queue<Point2D> worklist = new Queue<Point2D>();

        /// <summary>
        /// Reconstructs the corner-height field from the current terrain in the region.
        /// </summary>
        public void Build()
        {
            for (int iy = 0; iy < pointHeight; iy++)
            {
                for (int ix = 0; ix < pointWidth; ix++)
                {
                    int cellX = originX + ix;
                    int cellY = originY + iy;

                    // A corner takes its height from a single owning cell (the cell whose
                    // NW corner this point is), matching the game. This keeps the field
                    // well-defined even on hand-edited maps where neighbours disagree.
                    var owner = map.GetTile(cellX, cellY);
                    if (owner == null)
                    {
                        hasOwner[ix, iy] = false;
                        heights[ix, iy] = 0;
                    }
                    else
                    {
                        hasOwner[ix, iy] = true;
                        heights[ix, iy] = owner.Level + RampCornerHeights[GetRampIndex(owner)][0];
                    }

                    // A corner is rigid if any of the up-to-four cells touching it cannot
                    // be morphed (or is off the map).
                    rigid[ix, iy] = IsCellRigid(cellX, cellY) || IsCellRigid(cellX - 1, cellY) ||
                                    IsCellRigid(cellX - 1, cellY - 1) || IsCellRigid(cellX, cellY - 1);

                    done[ix, iy] = false;
                }
            }
        }

        /// <summary>
        /// Sets all four corners of a cell to a uniform height (used to raise/lower/flatten
        /// a targeted cell). Returns false if a corner anchored by an immutable cell would
        /// have to move, in which case the whole operation must be rejected.
        /// </summary>
        public bool TrySeedFlat(Point2D cellCoords, int level)
        {
            bool ok = true;
            for (int i = 0; i < CornerOffsets.Length; i++)
            {
                var pt = cellCoords + CornerOffsets[i];
                if (!TrySetCorner(pt.X, pt.Y, level))
                    ok = false;
            }

            return ok;
        }

        /// <summary>
        /// Adjusts a single corner by a delta (used to raise the shared centre corner of a
        /// 2x2 "small hill"). Returns false if the corner is anchored by an immutable cell.
        /// </summary>
        public bool TrySeedAdjustCorner(Point2D point, int delta)
        {
            if (!InRegion(point.X, point.Y))
                return false;

            int current = heights[point.X - originX, point.Y - originY];
            return TrySetCorner(point.X, point.Y, current + delta);
        }

        private bool TrySetCorner(int px, int py, int newHeight)
        {
            newHeight = Math.Clamp(newHeight, 0, Constants.MaxMapHeightLevel);

            if (!InRegion(px, py))
                return false;

            int ix = px - originX;
            int iy = py - originY;

            // Off-map / outside-diamond corners are a free boundary; there is nothing to seed.
            if (!hasOwner[ix, iy])
                return true;

            if (rigid[ix, iy] && heights[ix, iy] != newHeight)
                return false;

            heights[ix, iy] = newHeight;
            done[ix, iy] = true;
            worklist.Enqueue(new Point2D(px, py));
            return true;
        }

        /// <summary>
        /// Smooths the field so that neighbouring corners differ by no more than the allowed
        /// slope. Returns false if the edit cannot be smoothed without moving a corner that
        /// is anchored by an immutable cell, in which case nothing has been written to the map.
        /// </summary>
        public bool Flood(HeightFloodMode mode, bool allowSteep)
        {
            if (mode == HeightFloodMode.Both)
            {
                if (!FloodPass(HeightFloodMode.Down, allowSteep))
                    return false;

                RequeueAllDone();

                return FloodPass(HeightFloodMode.Up, allowSteep);
            }

            return FloodPass(mode, allowSteep);
        }

        private bool FloodPass(HeightFloodMode mode, bool allowSteep)
        {
            while (worklist.Count > 0)
            {
                var p = worklist.Dequeue();
                int six = p.X - originX;
                int siy = p.Y - originY;
                int startHeight = heights[six, siy];

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0)
                            continue;

                        int nx = p.X + dx;
                        int ny = p.Y + dy;
                        if (!InRegion(nx, ny))
                            continue; // the region margin guarantees a real ripple never reaches the edge

                        int nix = nx - originX;
                        int niy = ny - originY;

                        // Corners with no owning cell (off the map / outside the iso diamond)
                        // are a free boundary, not a constraint: they neither move nor reject.
                        if (!hasOwner[nix, niy])
                            continue;

                        bool diagonal = dx != 0 && dy != 0;
                        int threshold = (diagonal && allowSteep) ? 2 : 1;

                        int neighborHeight = heights[nix, niy];
                        int diff = neighborHeight - startHeight;
                        if (Math.Abs(diff) <= threshold)
                            continue;

                        if (rigid[nix, niy])
                            return false; // would have to move a corner anchored by immutable terrain

                        int newHeight = neighborHeight;
                        if (diff < 0)
                        {
                            // Neighbour is too low; raise it (unless we are only lowering).
                            if (mode != HeightFloodMode.Down)
                                newHeight = startHeight - threshold;
                        }
                        else
                        {
                            // Neighbour is too high; lower it (unless we are only raising).
                            if (mode != HeightFloodMode.Up)
                                newHeight = startHeight + threshold;
                        }

                        if (newHeight != neighborHeight)
                        {
                            heights[nix, niy] = newHeight;
                            done[nix, niy] = true;
                            worklist.Enqueue(new Point2D(nx, ny));
                        }
                    }
                }
            }

            return true;
        }

        private void RequeueAllDone()
        {
            for (int iy = 0; iy < pointHeight; iy++)
            {
                for (int ix = 0; ix < pointWidth; ix++)
                {
                    if (done[ix, iy])
                        worklist.Enqueue(new Point2D(originX + ix, originY + iy));
                }
            }
        }

        /// <summary>
        /// Writes the smoothed field back to the map: for every cell with at least one
        /// changed corner, sets its height level and ramp tile derived from its four corner
        /// heights. Returns the list of cells that were changed.
        /// </summary>
        /// <param name="allowSteep">Whether steep ramps (corner spread of 2) may be emitted.</param>
        /// <param name="addUndo">Called with a cell's coords immediately before it is mutated.</param>
        public List<MapTile> WriteBack(bool allowSteep, Action<Point2D> addUndo)
        {
            var changedCells = new List<MapTile>();
            int maxSpread = allowSteep ? 2 : 1;

            // Cells [originX..originX + pointWidth - 2] have all four corners inside the field.
            int lastCellX = originX + pointWidth - 2;
            int lastCellY = originY + pointHeight - 2;

            for (int cellY = originY; cellY <= lastCellY; cellY++)
            {
                for (int cellX = originX; cellX <= lastCellX; cellX++)
                {
                    bool anyDone = false;
                    bool allOwned = true;
                    int min = int.MaxValue;
                    int max = int.MinValue;
                    int c0 = 0, c1 = 0, c2 = 0, c3 = 0;

                    for (int i = 0; i < CornerOffsets.Length; i++)
                    {
                        int ix = (cellX + CornerOffsets[i].X) - originX;
                        int iy = (cellY + CornerOffsets[i].Y) - originY;

                        if (done[ix, iy])
                            anyDone = true;
                        if (!hasOwner[ix, iy])
                            allOwned = false;

                        int h = heights[ix, iy];
                        switch (i)
                        {
                            case 0: c0 = h; break;
                            case 1: c1 = h; break;
                            case 2: c2 = h; break;
                            default: c3 = h; break;
                        }

                        if (h < min) min = h;
                        if (h > max) max = h;
                    }

                    if (!anyDone || !allOwned)
                        continue;

                    if (max - min > maxSpread)
                        continue;

                    var cell = map.GetTile(cellX, cellY);
                    if (cell == null || !map.IsCellMorphable(cell))
                        continue;

                    var tmpImage = map.TheaterInstance.GetTile(cell.TileIndex).GetSubTile(cell.SubTileIndex).TmpImage;
                    LandType landType = (LandType)tmpImage.TerrainType;
                    if (landType == LandType.Rock || landType == LandType.Water)
                        continue;

                    int key = PatternKey(c0 - min, c1 - min, c2 - min, c3 - min);
                    if (!RampByCornerPattern.TryGetValue(key, out RampType rampType))
                        continue; // unreachable given the spread guard, but stay safe

                    addUndo(new Point2D(cellX, cellY));

                    int oldLevel = cell.Level;
                    cell.Level = (byte)min;

                    if (rampType == RampType.None)
                    {
                        // Reset to the clear tile if the cell is currently a ramp, or if its
                        // level changed (which also safely breaks up any multi-cell tile that
                        // would otherwise be split across height levels). Flat ground that is
                        // merely adjacent to the edit keeps its tile, preserving its texture.
                        if (tmpImage.RampType != RampType.None || min != oldLevel)
                            cell.ChangeTileIndex(0, 0);
                    }
                    else
                    {
                        cell.ChangeTileIndex(rampTileSetStart + ((int)rampType - 1), 0);
                    }

                    changedCells.Add(cell);
                }
            }

            return changedCells;
        }

        private bool InRegion(int px, int py)
            => px >= originX && py >= originY && px < originX + pointWidth && py < originY + pointHeight;

        private bool IsCellRigid(int cellX, int cellY)
        {
            var cell = map.GetTile(cellX, cellY);
            return cell == null || !map.IsCellMorphable(cell);
        }

        private int GetRampIndex(MapTile cell)
        {
            var subTile = map.TheaterInstance.GetTile(cell.TileIndex).GetSubTile(cell.SubTileIndex);
            int rampIndex = (int)subTile.TmpImage.RampType;
            if (rampIndex < 0 || rampIndex >= RampCornerHeights.Length)
                rampIndex = 0;

            return rampIndex;
        }

        private static Dictionary<int, RampType> BuildReverseLookup()
        {
            var dict = new Dictionary<int, RampType>();
            for (int ramp = 0; ramp <= LastEmittedRamp; ramp++)
            {
                int key = PatternKey(RampCornerHeights[ramp][0], RampCornerHeights[ramp][1],
                    RampCornerHeights[ramp][2], RampCornerHeights[ramp][3]);

                if (!dict.ContainsKey(key))
                    dict.Add(key, (RampType)ramp);
            }

            return dict;
        }

        private static int PatternKey(int c0, int c1, int c2, int c3)
            => c0 + c1 * 3 + c2 * 9 + c3 * 27;
    }
}
