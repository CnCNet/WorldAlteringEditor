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
        // Each row is the height of the four corners for that ramp type: normal ramps raise a
        // corner by one level, steep ramps raise their far corner by two. Rows 19/20
        // (DoubleUp/DownNWSE) duplicate 17/18 and exist only so existing tiles of those types
        // reconstruct correctly; they are never emitted.
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
        // {0,1,0,1}/{1,0,1,0} canonically resolve to the SWNE variants (17/18) and the
        // duplicate NWSE rows are never emitted.
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
                    // NW corner this point is). This keeps the field well-defined even on
                    // hand-edited maps where neighbours disagree on a shared corner.
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
        /// Seeds a cell to a uniform height (used to raise/lower/flatten a targeted cell).
        /// A corner anchored by an immutable cell that cannot take the height itself is
        /// snapped to the nearest height the immutable terrain actually has at that corner,
        /// if one exists within a one-level slope of the target — so a cell flattened against
        /// e.g. a cliff lip becomes a ramp rising to the lip. Returns false if some corner
        /// could not legally be seeded, in which case the whole operation must be rejected.
        /// </summary>
        public bool TrySeedFlat(Point2D cellCoords, int level)
        {
            level = Math.Clamp(level, 0, Constants.MaxMapHeightLevel);

            bool ok = true;
            for (int i = 0; i < CornerOffsets.Length; i++)
            {
                var pt = cellCoords + CornerOffsets[i];
                if (!TryResolveSeedCorner(pt.X, pt.Y, level, out int resolved))
                {
                    ok = false;
                    continue;
                }

                SetCornerDirect(pt.X, pt.Y, resolved);
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

        /// <summary>
        /// Read-only check of whether <see cref="TrySeedFlat"/> would succeed for a cell.
        /// Used to skip (rather than abort on) ramp cells that are already at the desired level
        /// but happen to be pinned by adjacent immutable terrain.
        /// </summary>
        public bool CanSeedFlat(Point2D cellCoords, int level)
        {
            level = Math.Clamp(level, 0, Constants.MaxMapHeightLevel);

            for (int i = 0; i < CornerOffsets.Length; i++)
            {
                var pt = cellCoords + CornerOffsets[i];
                if (!TryResolveSeedCorner(pt.X, pt.Y, level, out _))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Determines the height a corner should take when its cell is seeded to
        /// <paramref name="level"/>: the level itself for free corners, or the nearest height
        /// the immutable terrain actually has at the corner (within a one-level slope of the
        /// target) for anchored corners. Returns false if the corner cannot legally be seeded.
        /// </summary>
        private bool TryResolveSeedCorner(int px, int py, int level, out int resolved)
        {
            resolved = level;

            if (!InRegion(px, py))
                return false;

            int ix = px - originX;
            int iy = py - originY;

            // Off-map / outside-diamond corners are a free boundary; there is nothing to seed.
            if (!hasOwner[ix, iy])
                return true;

            if (!rigid[ix, iy])
                return true;

            // Anchored corners always snap to a height the immutable terrain actually has at
            // this corner. This makes the result independent of the order in which cells are
            // brushed: a cell flattened one level below a cliff lip, for example, always gets
            // its lip corners at the lip height (becoming a ramp), never a stale in-between
            // value from earlier smoothing.
            int? snapped = GetBestExactHeight(px, py, level - 1, level + 1, level);
            if (snapped == null)
                return false;

            resolved = snapped.Value;
            return true;
        }

        private void SetCornerDirect(int px, int py, int height)
        {
            int ix = px - originX;
            int iy = py - originY;

            if (!hasOwner[ix, iy])
                return;

            heights[ix, iy] = height;
            done[ix, iy] = true;
            worklist.Enqueue(new Point2D(px, py));
        }

        private bool TrySetCorner(int px, int py, int newHeight)
        {
            newHeight = Math.Clamp(newHeight, 0, Constants.MaxMapHeightLevel);

            if (!InRegion(px, py))
                return false;

            if (!CornerCanTake(px, py, newHeight))
                return false;

            int ix = px - originX;
            int iy = py - originY;

            // Off-map / outside-diamond corners are a free boundary; there is nothing to seed.
            if (!hasOwner[ix, iy])
                return true;

            heights[ix, iy] = newHeight;
            done[ix, iy] = true;
            worklist.Enqueue(new Point2D(px, py));
            return true;
        }

        /// <summary>
        /// Whether a corner may legally be set to <paramref name="newHeight"/>. Off-map corners
        /// are a free boundary (a no-op, not a rejection). A rigid corner may only take a height
        /// within the span that the immutable terrain touching it covers: a cliff face spans from
        /// its base level to its top level, so ground may legally meet it at any height in
        /// between. Flat immutable terrain (e.g. water, or a cliff base) has a single-height
        /// span, keeping mismatched edits against it rejected.
        /// </summary>
        private bool CornerCanTake(int px, int py, int newHeight)
        {
            int ix = px - originX;
            int iy = py - originY;

            if (!hasOwner[ix, iy])
                return true;

            if (rigid[ix, iy] && heights[ix, iy] != newHeight)
            {
                GetAdmissibleRange(px, py, out int admMin, out int admMax, out _);
                if (newHeight < admMin || newHeight > admMax)
                    return false;
            }

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
                bool startRigid = rigid[six, siy];

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
                        {
                            // A slope violation between two rigid corners is the immutable
                            // terrain's own geometry (e.g. a diagonal cliff cell whose art spans
                            // its full height within one cell) — never a conflict to resolve or
                            // reject over.
                            if (startRigid)
                                continue;

                            // A rigid corner may only move within the range of heights that the
                            // immutable terrain touching it implies (a cliff face, for example,
                            // spans from its base level to its top level, so ground may meet it
                            // at any height in between). If the start corner is out of slope
                            // range of even that span, the two cannot legally coexist — this
                            // happens around art anomalies such as exposed diagonal cliff ends.
                            // The immutable terrain wins: yield the morphable start corner into
                            // compliance instead of rejecting the whole edit.
                            GetAdmissibleRange(nx, ny, out int admMin, out int admMax, out bool bordersMorphable);
                            if (startHeight < admMin - threshold || startHeight > admMax + threshold)
                            {
                                startHeight = Math.Clamp(startHeight, admMin - threshold, admMax + threshold);
                                heights[six, siy] = startHeight;
                                done[six, siy] = true;
                                worklist.Enqueue(p);
                            }

                            // Slide the corner along the immutable face just far enough to be in
                            // slope range of the start corner. This ignores the pass direction on
                            // purpose: the move is bounded by the art span either way, and without
                            // it a corner can be left pinned at the wrong end of the face,
                            // producing "holes" (cells skipped by the write-back spread guard).
                            // Corners interior to immutable terrain (no morphable cell touches
                            // them) are left alone: moving them has no visual meaning and would
                            // create false conflicts inside cliff bodies.
                            int target = Math.Clamp(
                                Math.Clamp(neighborHeight, startHeight - threshold, startHeight + threshold),
                                admMin, admMax);

                            if (bordersMorphable && target != neighborHeight)
                            {
                                // Mark done so the touching cells are written back as ramps, but
                                // do NOT enqueue: the flood must never propagate through immutable
                                // terrain to its far side.
                                heights[nix, niy] = target;
                                done[nix, niy] = true;
                            }

                            continue;
                        }

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
                    // Rigid corners never act as flood fronts: they may have been marked done
                    // for write-back purposes, but propagation must not start from (or pass
                    // through) immutable terrain.
                    if (done[ix, iy] && !rigid[ix, iy])
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

        /// <summary>
        /// Of the corner heights that the immutable cells touching corner point (px, py) have
        /// at it, returns the one closest to <paramref name="preferred"/> that lies within
        /// [<paramref name="lo"/>, <paramref name="hi"/>], or null if none does. Morphable
        /// touching cells are ignored: their current heights are editable, not anchors.
        /// The cell touching the corner at corner-index k sits at (px, py) - CornerOffsets[k],
        /// and contributes its own corner k's height. The index k must match on both sides.
        /// </summary>
        private int? GetBestExactHeight(int px, int py, int lo, int hi, int preferred)
        {
            int? best = null;

            for (int k = 0; k < CornerOffsets.Length; k++)
            {
                var cell = map.GetTile(px - CornerOffsets[k].X, py - CornerOffsets[k].Y);
                if (cell == null || map.IsCellMorphable(cell))
                    continue;

                int h = cell.Level + RampCornerHeights[GetRampIndex(cell)][k];
                if (h < lo || h > hi)
                    continue;

                if (best == null || Math.Abs(h - preferred) < Math.Abs(best.Value - preferred))
                    best = h;
            }

            return best;
        }

        /// <summary>
        /// The lowest and highest corner heights implied for corner point (px, py) by the cells
        /// touching it (see <see cref="IsAdmissibleHeight"/>). Only called for corners with an
        /// owner, so at least the owning cell always contributes a value.
        /// Also reports whether any morphable cell touches the corner; corners interior to
        /// immutable terrain are treated differently by the flood.
        /// </summary>
        private void GetAdmissibleRange(int px, int py, out int min, out int max, out bool bordersMorphable)
        {
            min = int.MaxValue;
            max = int.MinValue;
            bordersMorphable = false;

            for (int k = 0; k < CornerOffsets.Length; k++)
            {
                var cell = map.GetTile(px - CornerOffsets[k].X, py - CornerOffsets[k].Y);
                if (cell == null)
                    continue;

                if (map.IsCellMorphable(cell))
                    bordersMorphable = true;

                int h = cell.Level + RampCornerHeights[GetRampIndex(cell)][k];
                if (h < min) min = h;
                if (h > max) max = h;
            }
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
