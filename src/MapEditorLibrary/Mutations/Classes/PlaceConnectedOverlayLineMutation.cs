using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;

namespace MapEditorLibrary.Mutations.Classes;

/// <summary>
/// A mutation that allows placing a line of connected overlays.
/// </summary>
public class PlaceConnectedOverlayLineMutation : ConnectedOverlayMutationBase
{
    public PlaceConnectedOverlayLineMutation(IMutationTarget mutationTarget, ConnectedOverlayType connectedOverlayType, Point2D sourceCoords, Direction direction, int length) : base(mutationTarget, connectedOverlayType)
    {
        this.sourceCoords = sourceCoords;
        this.direction = direction;
        this.length = length;
    }

    private readonly Point2D sourceCoords;
    private readonly Direction direction;
    private readonly int length;

    private OriginalOverlayInfo[] undoData;

    public override string GetDisplayString()
    {
        string directionString = Translate("Direction." + direction, Helpers.DirectionToName(direction));
        return string.Format(Translate(this, "DisplayString",
            "Place connected overlay '{0}' in a line from {1} towards {2} with length {3}"),
                ConnectedOverlayType.UIName, sourceCoords, directionString, length);
    }

    public override void Perform()
    {
        var lineCells = new List<Point2D>();
        Point2D step = Helpers.VisualDirectionToPoint(direction);

        // Collect all cells along the line
        for (int i = 0; i <= length; i++)
        {
            Point2D coords = sourceCoords + step.ScaleBy(i);
            var tile = MutationTarget.Map.GetTile(coords);
            if (tile != null)
                lineCells.Add(coords);
        }

        // Collect all affected cells (line cells + their surroundings) for undo and connection updates
        var affectedCells = new HashSet<Point2D>();
        foreach (Point2D cellCoords in lineCells)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    Point2D neighbor = cellCoords + new Point2D(x, y);
                    if (MutationTarget.Map.GetTile(neighbor) != null)
                        affectedCells.Add(neighbor);
                }
            }
        }

        // Save original overlay data for undo
        var originalOverlayInfos = new List<OriginalOverlayInfo>();
        foreach (Point2D cellCoords in affectedCells)
        {
            var tile = MutationTarget.Map.GetTile(cellCoords);
            originalOverlayInfos.Add(new OriginalOverlayInfo()
            {
                CellCoords = cellCoords,
                OverlayTypeIndex = tile.Overlay?.OverlayType.Index ?? -1,
                FrameIndex = tile.Overlay?.FrameIndex ?? -1,
            });
        }

        // Place connected overlays on the line cells
        foreach (Point2D cellCoords in lineCells)
        {
            var tile = MutationTarget.Map.GetTile(cellCoords);
            PlaceConnectedOverlay(tile);
        }

        // Update connections for all affected cells
        foreach (Point2D cellCoords in affectedCells)
        {
            var tile = MutationTarget.Map.GetTile(cellCoords);
            UpdateConnectedOverlay(tile);
        }

        undoData = originalOverlayInfos.ToArray();
        MutationTarget.AddRefreshPoint(sourceCoords, length + 2);
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

        MutationTarget.AddRefreshPoint(sourceCoords, length + 2);
    }
}
