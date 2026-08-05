using System.Collections.Generic;
using System.Linq;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes.AIMutations;

public abstract class OverlayBatchMutationBase : Mutation
{
    protected OverlayBatchMutationBase(IMutationTarget mutationTarget)
        : base(mutationTarget)
    {
    }

    private OriginalOverlayInfo[] undoData;

    protected HashSet<Point2D> GetCellsWithinRadius(IEnumerable<Point2D> cellCoords, int radius)
    {
        var affectedCellCoords = new HashSet<Point2D>();
        foreach (Point2D coords in cellCoords)
        {
            for (int yOffset = -radius; yOffset <= radius; yOffset++)
            {
                for (int xOffset = -radius; xOffset <= radius; xOffset++)
                {
                    Point2D affectedCoords = coords + new Point2D(xOffset, yOffset);
                    if (Map.GetTile(affectedCoords) != null)
                        affectedCellCoords.Add(affectedCoords);
                }
            }
        }

        return affectedCellCoords;
    }

    protected void CaptureOriginalOverlays(IEnumerable<Point2D> cellCoords)
    {
        undoData = cellCoords
            .Distinct()
            .OrderBy(coords => coords.Y)
            .ThenBy(coords => coords.X)
            .Select(coords =>
            {
                MapTile mapTile = Map.GetTile(coords);
                return new OriginalOverlayInfo(
                    mapTile.Overlay?.OverlayType.Index ?? -1,
                    mapTile.Overlay?.FrameIndex ?? -1,
                    coords);
            })
            .ToArray();
    }

    protected void SmoothResourceOverlays(IEnumerable<Point2D> cellCoords, ISet<Point2D> excludedCellCoords = null)
    {
        foreach (Point2D coords in cellCoords.OrderBy(coords => coords.Y).ThenBy(coords => coords.X))
        {
            if (excludedCellCoords?.Contains(coords) == true)
                continue;

            MapTile mapTile = Map.GetTile(coords);
            if (mapTile?.HasTiberium() == true)
                mapTile.Overlay.FrameIndex = Map.GetOverlayFrameIndex(coords);
        }
    }

    protected void RestoreOriginalOverlays()
    {
        foreach (OriginalOverlayInfo info in undoData)
        {
            MapTile mapTile = Map.GetTile(info.CellCoords);
            if (info.OverlayTypeIndex < 0)
            {
                mapTile.Overlay = null;
                continue;
            }

            mapTile.Overlay = new Overlay
            {
                OverlayType = Map.Rules.OverlayTypes[info.OverlayTypeIndex],
                Position = info.CellCoords,
                FrameIndex = info.FrameIndex
            };
        }
    }
}
