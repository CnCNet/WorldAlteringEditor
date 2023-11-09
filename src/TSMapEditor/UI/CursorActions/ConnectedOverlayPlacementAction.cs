﻿using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.Rendering;

namespace TSMapEditor.UI.CursorActions
{
    public class ConnectedOverlayPlacementAction : CursorAction
    {
        public ConnectedOverlayPlacementAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => "Place Connected Overlay";
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

        public override void PreMapDraw(Point2D cellCoords)
        {
            originalOverlay.Clear();

            CursorActionTarget.BrushSize.DoForBrushSizeAndSurroundings(offset =>
            {
                var tile = CursorActionTarget.Map.GetTile(cellCoords + offset);
                if (tile == null)
                    return;

                // Store original overlay info
                if (tile.Overlay != null)
                    originalOverlay.Add(new OriginalOverlayInfo(tile.Overlay.OverlayType, tile.Overlay.FrameIndex));
                else
                    originalOverlay.Add(new OriginalOverlayInfo(null, Constants.NO_OVERLAY));
            });

            CursorActionTarget.BrushSize.DoForBrushSize(offset =>
            {
                var previewMutation = new PlaceConnectedOverlayMutation(CursorActionTarget.MutationTarget, ConnectedOverlayType, cellCoords);
                previewMutation.Perform();
            });
        }

        public override void PostMapDraw(Point2D cellCoords)
        {
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

        public override void LeftDown(Point2D cellCoords)
        {
            var mutation = new PlaceConnectedOverlayMutation(CursorActionTarget.MutationTarget, ConnectedOverlayType, cellCoords);
            CursorActionTarget.MutationManager.PerformMutation(mutation);
        }

        public override void LeftClick(Point2D cellCoords) => LeftDown(cellCoords);
    }
}