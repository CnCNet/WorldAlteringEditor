using MapEditorLibrary;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;
using MapEditorLibrary.Mutations;
using MapEditorLibrary.Mutations.Classes;
using System;
using System.Collections.Generic;

namespace TSMapEditor.UI.CursorActions;

public class ConnectedOverlayPlacementAction : LineAndRegularPaintingAction
{
    public ConnectedOverlayPlacementAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
    {
    }

    public override string GetName() => Translate("Name", "Place Connected Overlay");
    protected override bool ClearPreviousCellOnMouseUp => true;

    public ConnectedOverlayType ConnectedOverlayType { get; set; }
    struct OriginalOverlayInfo
    {
        public OverlayType OverlayType;
        public int FrameIndex;

        public OriginalOverlayInfo(OverlayType overlayType, int frameIndex)
        {
            OverlayType = overlayType;
            FrameIndex = frameIndex;
        }
    }

    private List<OriginalOverlayInfo> originalOverlay = new List<OriginalOverlayInfo>();

    public override void OnActionExit()
    {
        ClearLinePreview();
        base.OnActionExit();
    }

    public override void PreMapDraw(Point2D cellCoords)
    {
        if (LineSourceCell.HasValue)
        {
            ApplyLinePreview(cellCoords);
            return;
        }

        originalOverlay.Clear();

        CursorActionTarget.BrushSize.DoForBrushSizeAndSurroundings(offset =>
        {
            var tile = CursorActionTarget.Map.GetTile(cellCoords + offset);
            if (tile == null)
                return;

            // Store original overlay info
            originalOverlay.Add(tile.Overlay != null
                ? new OriginalOverlayInfo(tile.Overlay.OverlayType, tile.Overlay.FrameIndex)
                : new OriginalOverlayInfo(null, Constants.NO_OVERLAY));
        });

        new PlaceConnectedOverlayMutation(CursorActionTarget.MutationTarget, ConnectedOverlayType, cellCoords).Perform();
    }

    public override void PostMapDraw(Point2D cellCoords)
    {
        if (LineSourceCell.HasValue)
        {
            ClearLinePreview();
            return;
        }

        int index = 0;

        CursorActionTarget.BrushSize.DoForBrushSizeAndSurroundings(offset =>
        {
            var tile = CursorActionTarget.Map.GetTile(cellCoords + offset);
            if (tile == null)
                return;

            var originalOverlayData = originalOverlay[index];

            if (originalOverlayData.OverlayType == null)
            {
                tile.Overlay = null;
            }
            else
            {
                tile.Overlay.OverlayType = originalOverlayData.OverlayType;
                tile.Overlay.FrameIndex = originalOverlayData.FrameIndex;
            }

            index++;
        });

        originalOverlay.Clear();

        CursorActionTarget.AddRefreshPoint(cellCoords, Math.Max(CursorActionTarget.BrushSize.Height, CursorActionTarget.BrushSize.Width));
    }

    protected override ICheckableMutation CreateRegularPlacementMutation(Point2D cellCoords)
    {
        return new PlaceConnectedOverlayMutation(CursorActionTarget.MutationTarget, ConnectedOverlayType, cellCoords);
    }

    protected override Mutation CreateLinePlacementMutation(Direction direction, int length)
    {
        return new PlaceConnectedOverlayLineMutation(MutationTarget, ConnectedOverlayType, LineSourceCell.Value, direction, length);
    }

    protected override void ApplyLine(Point2D cellCoords)
    {
        (Direction direction, int length) = GetLineInformation(cellCoords);
        var mutation = CreateLinePlacementMutation(direction, length);
        PerformMutation(mutation);
    }
}
