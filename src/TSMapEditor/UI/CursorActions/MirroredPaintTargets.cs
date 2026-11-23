using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TSMapEditor.UI.CursorActions;

public readonly record struct MirroredPaintTarget(Point2D Cell, int Variant, bool CanPaint);

public static class MirroredPaintTargets
{
    public static IEnumerable<MirroredPaintTarget> GetTargets(Map map, MapMirroring mirroring, IEnumerable<Point2D> sourceCells,
        int variantCount, Func<MapTile, int, bool> canPaint)
    {
        var visitedCells = new HashSet<Point2D>();
        Point2D doubledMirroringPoint = mirroring.GetDoubledMirroringPoint(map.Size);

        foreach (Point2D source in sourceCells)
        {
            if (visitedCells.Contains(source))
                continue;

            var counterparts = mirroring.Transforms.Select(transform => MapMirroring.TransformCell(source, doubledMirroringPoint, transform))
                .Distinct().ToArray();
            visitedCells.UnionWith(counterparts);

            // Pick one collection entry for the whole set. The same cell set
            // keeps its variant when the pointer crosses an axis or retries.
            Point2D first = counterparts.MinBy(cell => (cell.X, cell.Y));
            int variant = (int)((uint)HashCode.Combine(first.X, first.Y) % (uint)variantCount);
            foreach (Point2D counterpart in counterparts)
            {
                bool valid = map.GetTile(counterpart) is MapTile tile && canPaint(tile, variant);
                yield return new MirroredPaintTarget(counterpart, variant, valid);
            }
        }
    }
}
