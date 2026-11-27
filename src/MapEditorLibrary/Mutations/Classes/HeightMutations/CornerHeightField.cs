using MapEditorLibrary.CCEngine;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;
using MapEditorLibrary.Models.Enums;

namespace MapEditorLibrary.Mutations.Classes.HeightMutations;

/// <summary>
/// The direction in which a smoothing pass is allowed to move cell corners.
/// </summary>
public enum HeightFloodMode
{
    /// <summary>Only raise corners (used when raising ground).</summary>
    Up,

    /// <summary>Only lower corners (used when lowering ground).</summary>
    Down,

    /// <summary>
    /// Move corners either way (used when flattening ground).
    /// Runs a downward pass followed by an upward pass.
    /// </summary>
    Both
}

/// <summary>
/// A transient height field over cell corners, used for "smart" ground-height smoothing.
/// Corners are smoothed so that neighbouring corners never differ by more than the allowed
/// slope, and each cell's ramp is then derived from its four corner heights.
/// </summary>
public class CornerHeightField
{
    /// <summary>
    /// Offsets of a cell's NW, NE, SE and SW corner points, in <see cref="RampCornerHeights"/> order.
    /// </summary>
    public static readonly Point2D[] CornerOffsets =
    {
        new Point2D(0, 0), new Point2D(1, 0), new Point2D(1, 1), new Point2D(0, 1)
    };

    // Corner heights of each ramp type, in CornerOffsets order, indexed by RampType.
    // The NWSE double ramps duplicate the SWNE ones and are never emitted.
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
        new[] { 0, 1, 0, 1 }, // DoubleUpNWSE
        new[] { 1, 0, 1, 0 }, // DoubleDownNWSE
    };

    private const int LastEmittedRamp = 18;

    private static readonly Dictionary<int, RampType> RampByCornerPattern = BuildReverseLookup();

    public CornerHeightField(Map map, int cellMinX, int cellMinY, int cellMaxX, int cellMaxY)
    {
        this.map = map;
        rampTileSetStart = map.TheaterInstance.Theater.RampTileSet.StartTileIndex;

        // A height change can ripple outward by up to MaxMapHeightLevel cells,
        // so pad the region enough for the flood to never reach its edge.
        int margin = Constants.MaxMapHeightLevel + 2;
        originX = Math.Max(0, cellMinX - margin);
        originY = Math.Max(0, cellMinY - margin);
        int regionMaxCellX = cellMaxX + margin;
        int regionMaxCellY = cellMaxY + margin;

        pointWidth = (regionMaxCellX - originX) + 2;
        pointHeight = (regionMaxCellY - originY) + 2;

        heights = new int[pointWidth, pointHeight];
        rigid = new bool[pointWidth, pointHeight];
        done = new bool[pointWidth, pointHeight];
        hasHeight = new bool[pointWidth, pointHeight];
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
    private readonly bool[,] hasHeight;

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

                hasHeight[ix, iy] = TryGetCornerHeight(cellX, cellY, out int cornerHeight);
                heights[ix, iy] = cornerHeight;

                // A corner is rigid if any cell touching it is non-morphable (e.g. a cliff).
                // Off-map cells don't count.
                rigid[ix, iy] = IsCellRigid(cellX, cellY) || IsCellRigid(cellX - 1, cellY) ||
                                IsCellRigid(cellX - 1, cellY - 1) || IsCellRigid(cellX, cellY - 1);

                done[ix, iy] = false;
            }
        }
    }

    /// <summary>
    /// Seeds a cell to a uniform height. Rigid corners snap to the nearest height the
    /// immutable terrain has at them, so e.g. a cell flattened against a cliff lip becomes a ramp.
    /// Returns false if some corner could not be seeded, in which case the edit must be rejected.
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
    /// Adjusts a single corner by a delta (used for the centre corner of a 2x2 hill).
    /// Returns false if the corner is anchored by immutable terrain.
    /// </summary>
    public bool TrySeedAdjustCorner(Point2D point, int delta)
    {
        if (!InRegion(point.X, point.Y))
            return false;

        int current = heights[point.X - originX, point.Y - originY];
        return TrySetCorner(point.X, point.Y, current + delta);
    }

    /// <summary>
    /// Checks whether <see cref="TrySeedFlat"/> would succeed for a cell, without modifying the field.
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

    private bool TryResolveSeedCorner(int px, int py, int level, out int resolved)
    {
        resolved = level;

        if (!InRegion(px, py))
            return false;

        int ix = px - originX;
        int iy = py - originY;

        if (!hasHeight[ix, iy] || !rigid[ix, iy])
            return true;

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

        if (!hasHeight[ix, iy])
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

        if (!hasHeight[ix, iy])
            return true;

        heights[ix, iy] = newHeight;
        done[ix, iy] = true;
        worklist.Enqueue(new Point2D(px, py));
        return true;
    }

    /// <summary>
    /// Whether a corner may be set to the given height. A rigid corner may only take
    /// heights within the span of the immutable terrain touching it (e.g. from a cliff's base to its top).
    /// </summary>
    private bool CornerCanTake(int px, int py, int newHeight)
    {
        int ix = px - originX;
        int iy = py - originY;

        if (!hasHeight[ix, iy])
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
    /// Smooths the field so that neighbouring corners differ by no more than the allowed slope.
    /// Returns false if the edit had to be rejected.
    /// </summary>
    public bool Flood(HeightFloodMode mode, bool allowSteep) => Flood(mode, allowSteep, null);

    internal bool Flood(HeightFloodMode mode, bool allowSteep, int? flattenLevel)
    {
        if (mode == HeightFloodMode.Both)
        {
            if (!FloodPass(HeightFloodMode.Down, allowSteep, flattenLevel))
                return false;

            RequeueAllDone();

            return FloodPass(HeightFloodMode.Up, allowSteep, flattenLevel);
        }

        return FloodPass(mode, allowSteep, flattenLevel);
    }

    private bool FloodPass(HeightFloodMode mode, bool allowSteep, int? flattenLevel)
    {
        while (worklist.Count > 0)
        {
            var p = worklist.Dequeue();
            int six = p.X - originX;
            int siy = p.Y - originY;
            int startHeight = heights[six, siy];
            bool startRigid = rigid[six, siy];

            // If the corner is out of slope range of adjacent immutable terrain, the terrain wins
            // and the corner yields to it. All rigid neighbours are considered at once; yielding to
            // them one at a time can bounce the corner between two of them forever.
            if (!startRigid && TryGetRigidNeighbourBounds(p.X, p.Y, allowSteep, out int yieldMin, out int yieldMax))
            {
                int yielded = Math.Clamp(startHeight, yieldMin, yieldMax);
                if (yielded != startHeight)
                {
                    startHeight = yielded;
                    heights[six, siy] = startHeight;
                    done[six, siy] = true;
                }
            }

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                        continue;

                    int nx = p.X + dx;
                    int ny = p.Y + dy;
                    if (!InRegion(nx, ny))
                        continue;

                    int nix = nx - originX;
                    int niy = ny - originY;

                    if (!hasHeight[nix, niy])
                        continue;

                    bool diagonal = dx != 0 && dy != 0;
                    int threshold = (diagonal && allowSteep) ? 2 : 1;

                    int neighborHeight = heights[nix, niy];
                    int diff = neighborHeight - startHeight;
                    if (Math.Abs(diff) <= threshold)
                        continue;

                    if (rigid[nix, niy])
                    {
                        // Slopes between two rigid corners are the immutable terrain's own geometry.
                        if (startRigid)
                            continue;

                        GetAdmissibleRange(nx, ny, out int admMin, out int admMax, out bool bordersMorphable);

                        // Slide the rigid corner along the immutable face (in either direction)
                        // until it's in slope range, so the cells touching it get written back as ramps.
                        // It is not enqueued, so the flood never passes through immutable terrain.
                        int target = Math.Clamp(
                            Math.Clamp(neighborHeight, startHeight - threshold, startHeight + threshold),
                            admMin, admMax);

                        // Ground flattened to the cliff top should meet the lip at its full height.
                        if (bordersMorphable && startHeight == flattenLevel && target > neighborHeight &&
                            target < startHeight && TouchesCliffTop(nx, ny, startHeight))
                        {
                            target = startHeight;
                        }

                        if (bordersMorphable && target != neighborHeight)
                        {
                            heights[nix, niy] = target;
                            done[nix, niy] = true;
                        }

                        continue;
                    }

                    int newHeight = neighborHeight;
                    if (diff < 0)
                    {
                        if (mode != HeightFloodMode.Down)
                            newHeight = startHeight - threshold;
                    }
                    else
                    {
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
                if (done[ix, iy] && !rigid[ix, iy])
                    worklist.Enqueue(new Point2D(originX + ix, originY + iy));
            }
        }
    }

    /// <summary>
    /// Writes the smoothed field back to the map, setting the height level and ramp tile
    /// of every cell with at least one changed corner. Returns the changed cells.
    /// </summary>
    /// <param name="allowSteep">Whether steep ramps may be emitted.</param>
    /// <param name="addUndo">Called with a cell's coords right before it is changed.</param>
    public List<MapTile> WriteBack(bool allowSteep, Action<Point2D> addUndo)
    {
        var changedCells = new List<MapTile>();
        int maxSpread = allowSteep ? 2 : 1;

        int lastCellX = originX + pointWidth - 2;
        int lastCellY = originY + pointHeight - 2;

        for (int cellY = originY; cellY <= lastCellY; cellY++)
        {
            for (int cellX = originX; cellX <= lastCellX; cellX++)
            {
                bool anyDone = false;
                bool allKnown = true;
                int min = int.MaxValue;
                int max = int.MinValue;
                int c0 = 0, c1 = 0, c2 = 0, c3 = 0;

                for (int i = 0; i < CornerOffsets.Length; i++)
                {
                    int ix = (cellX + CornerOffsets[i].X) - originX;
                    int iy = (cellY + CornerOffsets[i].Y) - originY;

                    if (done[ix, iy])
                        anyDone = true;
                    if (!hasHeight[ix, iy])
                        allKnown = false;

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

                if (!anyDone || !allKnown)
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
                    continue;

                addUndo(new Point2D(cellX, cellY));

                int oldLevel = cell.Level;
                cell.Level = (byte)min;

                if (rampType == RampType.None)
                {
                    // Flat ground whose level didn't change keeps its tile.
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
        return cell != null && !map.IsCellMorphable(cell);
    }

    /// <summary>
    /// Gets a corner's height from the cell whose NW corner it is, or, at the map edge,
    /// from another cell touching it. Returns false if no cell touches the corner.
    /// </summary>
    private bool TryGetCornerHeight(int px, int py, out int height)
    {
        for (int k = 0; k < CornerOffsets.Length; k++)
        {
            var cell = map.GetTile(px - CornerOffsets[k].X, py - CornerOffsets[k].Y);
            if (cell != null)
            {
                height = cell.Level + RampCornerHeights[GetRampIndex(cell)][k];
                return true;
            }
        }

        height = 0;
        return false;
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
    /// Of the heights that the immutable cells touching a corner have at it, returns the one
    /// within [lo, hi] closest to the preferred height, or null if there is none.
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
    /// Gets the lowest and highest heights that the cells touching a corner have at it,
    /// and whether any of those cells is morphable.
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

    /// <summary>
    /// Gets the range of heights a corner may take while staying within slope range of every
    /// rigid corner around it. Returns false if there are no rigid neighbours, or if no height satisfies them all.
    /// </summary>
    private bool TryGetRigidNeighbourBounds(int px, int py, bool allowSteep, out int min, out int max)
    {
        min = int.MinValue;
        max = int.MaxValue;
        bool anyRigid = false;

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                int nx = px + dx;
                int ny = py + dy;
                if (!InRegion(nx, ny))
                    continue;

                int nix = nx - originX;
                int niy = ny - originY;
                if (!hasHeight[nix, niy] || !rigid[nix, niy])
                    continue;

                bool diagonal = dx != 0 && dy != 0;
                int threshold = (diagonal && allowSteep) ? 2 : 1;

                GetAdmissibleRange(nx, ny, out int admMin, out int admMax, out _);
                min = Math.Max(min, admMin - threshold);
                max = Math.Min(max, admMax + threshold);
                anyRigid = true;
            }
        }

        return anyRigid && min <= max;
    }

    private bool TouchesCliffTop(int px, int py, int level)
    {
        foreach (var offset in CornerOffsets)
        {
            var cell = map.GetTile(px - offset.X, py - offset.Y);
            if (cell == null || cell.Level != level || map.IsCellMorphable(cell))
                continue;

            var tmpImage = map.TheaterInstance.GetTile(cell.TileIndex).GetSubTile(cell.SubTileIndex).TmpImage;
            if (tmpImage.Height > 0 && tmpImage.RampType == RampType.None)
                return true;
        }

        return false;
    }

    // First match wins, so the ambiguous double-ramp patterns resolve to the SWNE variants.
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
