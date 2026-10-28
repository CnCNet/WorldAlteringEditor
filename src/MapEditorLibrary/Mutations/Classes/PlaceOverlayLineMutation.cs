using MapEditorLibrary.Configuration;
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
        brushSize = mutationTarget.BrushSize;
    }

    private readonly OverlayType overlayType;
    private readonly int? forcedFrameIndex;
    private readonly Point2D sourceCoords;
    private readonly Direction direction;
    private readonly int length;
    private readonly BrushSize brushSize;

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
        foreach (Point2D coords in brushSize.GetCellsAlongLine(sourceCoords, direction, length, 1))
        {
            var tile = Map.GetTile(coords);
            if (tile == null)
                continue;

            originalOverlayInfos.Add(new OriginalOverlayInfo()
            {
                CellCoords = coords,
                OverlayTypeIndex = tile.Overlay?.OverlayType.Index ?? -1,
                FrameIndex = tile.Overlay?.FrameIndex ?? -1,
            });
        }

        foreach (Point2D coords in brushSize.GetCellsAlongLine(sourceCoords, direction, length))
        {
            var tile = MutationTarget.Map.GetTile(coords);
            if (tile == null)
                continue;

            tile.Overlay = new Overlay()
            {
                Position = tile.CoordsToPoint(),
                OverlayType = overlayType,
                FrameIndex = forcedFrameIndex ?? 0
            };
        }

        if (!forcedFrameIndex.HasValue)
        {
            UpdateFrameIndexesAroundLine();
        }

        undoData = originalOverlayInfos.ToArray();
        MutationTarget.AddRefreshPoint(sourceCoords, length + brushSize.Max + 1);
    }

    private void UpdateFrameIndexesAroundLine()
    {
        foreach (Point2D coords in brushSize.GetCellsAlongLine(sourceCoords, direction, length, 1))
        {
            SetOverlayFrameIndexForTile(coords);
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

        MutationTarget.AddRefreshPoint(sourceCoords, length + brushSize.Max + 1);
    }
}
