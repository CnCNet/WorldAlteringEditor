using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;

namespace MapEditorLibrary.Mutations.Classes;

public abstract class ConnectedOverlayMutationBase : Mutation
{
    protected ConnectedOverlayMutationBase(IMutationTarget mutationTarget, ConnectedOverlayType connectedOverlayType) : base(mutationTarget)
    {
        ConnectedOverlayType = connectedOverlayType;
    }

    protected ConnectedOverlayType ConnectedOverlayType { get; }

    protected void UpdateConnectedOverlay(MapTile tile)
    {
        if (tile?.Overlay == null || !ConnectedOverlayType.ContainsOverlay(tile.Overlay))
            return;

        var connectedOverlayFrame = ConnectedOverlayType.GetContainedConnectedOverlayType(tile.Overlay)
            ?.GetOverlayForCell(MutationTarget, tile.CoordsToPoint());
        if (connectedOverlayFrame == null)
            return;

        tile.Overlay = new Overlay()
        {
            Position = tile.CoordsToPoint(),
            OverlayType = connectedOverlayFrame.OverlayType,
            FrameIndex = connectedOverlayFrame.FrameIndex
        };
    }

    protected void PlaceConnectedOverlay(MapTile tile)
    {
        if (tile == null)
            return;

        var connectedOverlayFrame = ConnectedOverlayType.GetOverlayForCell(MutationTarget, tile.CoordsToPoint()) ?? ConnectedOverlayType.Frames[0];

        tile.Overlay = new Overlay()
        {
            Position = tile.CoordsToPoint(),
            OverlayType = connectedOverlayFrame.OverlayType,
            FrameIndex = connectedOverlayFrame.FrameIndex
        };
    }
}

/// <summary>
/// A mutation that allows placing connected overlays.
/// </summary>
public class PlaceConnectedOverlayMutation : ConnectedOverlayMutationBase, ICheckableMutation
{
    public PlaceConnectedOverlayMutation(IMutationTarget mutationTarget, ConnectedOverlayType connectedOverlayType, Point2D cellCoords)
        : this(mutationTarget, connectedOverlayType, cellCoords, mutationTarget.BrushSize)
    {
    }

    public PlaceConnectedOverlayMutation(IMutationTarget mutationTarget, ConnectedOverlayType connectedOverlayType, Point2D cellCoords, BrushSize brush)
        : base(mutationTarget, connectedOverlayType)
    {
        this.cellCoords = cellCoords;
        this.brush = brush ?? throw new ArgumentNullException(nameof(brush));
    }

    private readonly BrushSize brush;
    private readonly Point2D cellCoords;

    private OriginalOverlayInfo[] undoData;

    public bool ShouldPerform() => true;

    public override string GetDisplayString()
    {
        return string.Format(Translate(this, "DisplayString", 
            "Place connected overlay '{0}' at {1} with a brush size of {2}"),
                ConnectedOverlayType.UIName, cellCoords, brush);
    }

    public override void Perform()
    {
        var originalOverlayInfos = new List<OriginalOverlayInfo>();

        // Save the original overlays
        brush.DoForBrushSizeAndSurroundings(offset =>
        {
            var tile = MutationTarget.Map.GetTile(cellCoords + offset);
            if (tile == null)
                return;

            originalOverlayInfos.Add(new OriginalOverlayInfo()
            {
                CellCoords = tile.CoordsToPoint(),
                OverlayTypeIndex = tile.Overlay?.OverlayType.Index ?? -1,
                FrameIndex = tile.Overlay?.FrameIndex ?? -1,
            });
        });

        // Now place overlays
        brush.DoForBrushSize(offset =>
        {
            var tile = MutationTarget.Map.GetTile(cellCoords + offset);
            if (tile == null)
                return;

            PlaceConnectedOverlay(tile);
        });

        // And then update them all to make sure they are connected properly
        brush.DoForBrushSizeAndSurroundings(offset =>
        {
            var tile = MutationTarget.Map.GetTile(cellCoords + offset);
            if (tile == null)
                return;

            UpdateConnectedOverlay(tile);
        });

        undoData = originalOverlayInfos.ToArray();
        MutationTarget.AddRefreshPoint(cellCoords, Math.Max(brush.Width, brush.Height) + 1);
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

        MutationTarget.AddRefreshPoint(cellCoords, Math.Max(brush.Width, brush.Height) + 1);
    }
}
