using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;

namespace MapEditorLibrary.Mutations.Classes;

/// <summary>
/// A mutation that allows placing regular overlay in a line.
/// </summary>
public class PlaceOverlayLineMutation : Mutation
{
    public PlaceOverlayLineMutation(IMutationTarget mutationTarget, OverlayType overlayType, int? forcedFrameIndex, Point2D sourceCoords, Direction direction, int length) : base(mutationTarget)
    {
        this.overlayType = overlayType ?? throw new ArgumentNullException(nameof(overlayType));
        this.forcedFrameIndex = forcedFrameIndex;
        this.sourceCoords = sourceCoords;
        this.direction = direction;
        this.length = length;
    }

    private readonly OverlayType overlayType;
    private readonly int? forcedFrameIndex;
    private readonly Point2D sourceCoords;
    private readonly Direction direction;
    private readonly int length;

    private OriginalOverlayInfo[] undoData;

    public override string GetDisplayString()
    {
        string directionString = Translate("Direction." + direction, Helpers.DirectionToName(direction));

        return string.Format(Translate(this, "DisplayString",
            "Place overlay '{0}' in a line from {1} towards {2} with length {3}"),
                overlayType.GetEditorDisplayName(), sourceCoords, directionString, length);
    }

    public override void Perform()
    {
        var originalOverlayInfos = new List<OriginalOverlayInfo>();
        Point2D step = Helpers.VisualDirectionToPoint(direction);

        for (int i = 0; i <= length; i++)
        {
            Point2D coords = sourceCoords + step.ScaleBy(i);
            var tile = MutationTarget.Map.GetTile(coords);
            if (tile == null)
                continue;

            originalOverlayInfos.Add(new OriginalOverlayInfo()
            {
                CellCoords = tile.CoordsToPoint(),
                OverlayTypeIndex = tile.Overlay == null ? -1 : tile.Overlay.OverlayType.Index,
                FrameIndex = tile.Overlay == null ? -1 : tile.Overlay.FrameIndex,
            });

            tile.Overlay = new Overlay()
            {
                Position = tile.CoordsToPoint(),
                OverlayType = overlayType,
                FrameIndex = forcedFrameIndex ?? 0
            };
        }

        if (!forcedFrameIndex.HasValue)
        {
            UpdateFrameIndexesAroundLine(step);
        }

        undoData = originalOverlayInfos.ToArray();
        MutationTarget.AddRefreshPoint(sourceCoords, length + 2);
    }

    private void UpdateFrameIndexesAroundLine(Point2D step)
    {
        var updatedCells = new HashSet<Point2D>();

        for (int i = 0; i <= length; i++)
        {
            Point2D coords = sourceCoords + step.ScaleBy(i);

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    Point2D neighbor = coords + new Point2D(x, y);
                    if (updatedCells.Add(neighbor))
                        SetOverlayFrameIndexForTile(neighbor);
                }
            }
        }
    }

    private void SetOverlayFrameIndexForTile(Point2D cellCoords)
    {
        var tile = MutationTarget.Map.GetTile(cellCoords);
        if (tile == null)
            return;

        if (tile.Overlay == null)
            return;

        tile.Overlay.FrameIndex = MutationTarget.Map.GetOverlayFrameIndex(cellCoords);
    }

    public override void Undo()
    {
        foreach (OriginalOverlayInfo info in undoData)
        {
            var tile = MutationTarget.Map.GetTile(info.CellCoords);
            if (info.OverlayTypeIndex == -1)
            {
                tile.Overlay = null;
                continue;
            }

            tile.Overlay = new Overlay()
            {
                OverlayType = MutationTarget.Map.Rules.OverlayTypes[info.OverlayTypeIndex],
                Position = info.CellCoords,
                FrameIndex = info.FrameIndex
            };
        }

        if (!forcedFrameIndex.HasValue)
        {
            UpdateFrameIndexesAroundLine(Helpers.VisualDirectionToPoint(direction));
        }

        MutationTarget.AddRefreshPoint(sourceCoords, length + 2);
    }
}
