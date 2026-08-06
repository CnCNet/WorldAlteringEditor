using MapEditorLibrary;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;
using MapEditorLibrary.Mutations;
using MapEditorLibrary.Mutations.Classes;
using System;
using System.Collections.Generic;

namespace TSMapEditor.UI.CursorActions;

/// <summary>
/// A cursor action that allows placing individual overlay.
/// </summary>
public class OverlayPlacementAction : LineAndRegularPaintingAction
{
    public OverlayPlacementAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
    {
    }

    public override string GetName() => Translate("Name", "Place Overlay");

    protected override bool PreventInputEventsOnPreviousCell => false; // Not needed, PlaceOverlayMutation.ShouldPerform does the job already

    public event EventHandler OverlayTypeChanged;

    private OverlayType _overlayType;
    public OverlayType OverlayType 
    { 
        get => _overlayType; 
        set
        {
            if (_overlayType != value)
            {
                _overlayType = value;
                OverlayTypeChanged?.Invoke(this, EventArgs.Empty);
            }
        } 
    }

    public int? FrameIndex { get; set; }

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

        int brushSize = CursorActionTarget.BrushSize.Width * CursorActionTarget.BrushSize.Height;

        int tileIndex = 0;
        CursorActionTarget.BrushSize.DoForBrushSize(offset =>
        {
            var tile = CursorActionTarget.Map.GetTile(cellCoords + offset);
            if (tile == null)
                return;

            // Store original overlay info
            if (tile.Overlay != null)
                originalOverlay.Add(new OriginalOverlayInfo(tile.Overlay.OverlayType, tile.Overlay.FrameIndex));
            else
                originalOverlay.Add(new OriginalOverlayInfo(null, Constants.NO_OVERLAY));

            // Apply new overlay info
            if (tile.Overlay == null)
            {
                // Creating new object instances each frame is not very performance-friendly, we might want to revise this later...
                tile.Overlay = new Overlay()
                {
                    Position = tile.CoordsToPoint(),
                    OverlayType = OverlayType,
                    FrameIndex = 0
                };
            }
            else
            {
                tile.Overlay.OverlayType = OverlayType;
                tile.Overlay.FrameIndex = 0;
            }

            if (FrameIndex == null)
                tile.Overlay.FrameIndex = CursorActionTarget.Map.GetOverlayFrameIndex(tile.CoordsToPoint());
            else
                tile.Overlay.FrameIndex = FrameIndex.Value;

            tileIndex++;
        });

        CursorActionTarget.AddRefreshPoint(cellCoords, Math.Max(CursorActionTarget.BrushSize.Height, CursorActionTarget.BrushSize.Width));
    }

    public override void PostMapDraw(Point2D cellCoords)
    {
        if (LineSourceCell.HasValue)
        {
            ClearLinePreview();
            return;
        }

        int index = 0;

        CursorActionTarget.BrushSize.DoForBrushSize(offset =>
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

        CursorActionTarget.AddRefreshPoint(cellCoords, Math.Max(CursorActionTarget.BrushSize.Height, CursorActionTarget.BrushSize.Width) + 1);
    }

    protected override bool CanDrawLinePreview() => OverlayType != null;

    protected override ICheckableMutation CreateRegularPlacementMutation(Point2D cellCoords)
    {
        return new PlaceOverlayMutation(CursorActionTarget.MutationTarget, OverlayType, FrameIndex, cellCoords);
    }

    protected override Mutation CreateLinePlacementMutation(Direction direction, int length)
    {
        return new PlaceOverlayLineMutation(MutationTarget, OverlayType, FrameIndex, LineSourceCell.Value, direction, length);
    }

    protected override void ApplyLine(Point2D cellCoords)
    {
        if (OverlayType != null)
        {
            (Direction direction, int length) = GetLineInformation(cellCoords);
            var mutation = CreateLinePlacementMutation(direction, length);
            PerformMutation(mutation);
        }
    }
}
