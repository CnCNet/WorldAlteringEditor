using System;
using System.Collections.Generic;
using TSMapEditor.CCEngine.TileData;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations;
using TSMapEditor.Mutations.Classes;

namespace TSMapEditor.UI.CursorActions
{
    public class OverlayCollectionPlacementAction : LineAndRegularPaintingAction
    {
        public OverlayCollectionPlacementAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => Translate("Name", "Place Overlay Collection");

        protected override bool ClearPreviousCellOnMouseUp => true;

        private OverlayCollection _overlayCollection;
        public OverlayCollection OverlayCollection
        {
            get => _overlayCollection;
            set
            {
                if (value.Entries.Length == 0)
                {
                    throw new InvalidOperationException($"Overlay collection {value.Name} has no overlay entries!");
                }

                _overlayCollection = value;
            }
        }

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
        private int[] randomizedOverlayCollectionEntryIndexes;
        private OverlayCollection lastRandomizedCollection = null;

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
            if (lastRandomizedCollection != OverlayCollection || 
                randomizedOverlayCollectionEntryIndexes.Length < brushSize)
            {
                randomizedOverlayCollectionEntryIndexes = new int[brushSize];
                for (int i = 0; i < randomizedOverlayCollectionEntryIndexes.Length; i++)
                    randomizedOverlayCollectionEntryIndexes[i] = CursorActionTarget.Randomizer.GetRandomNumber(0, OverlayCollection.Entries.Length - 1);

                lastRandomizedCollection = OverlayCollection;
            }

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

                var entry = OverlayCollection.Entries[randomizedOverlayCollectionEntryIndexes[tileIndex]];

                // Do not place tiberium on impassable tiles
                if (entry.OverlayType.Tiberium)
                {
                    ITileImage tileImage = CursorActionTarget.Map.TheaterInstance.GetTile(tile.TileIndex);
                    ISubTileImage subCellImage = tileImage.GetSubTile(tile.SubTileIndex);
                    if (Helpers.IsLandTypeImpassable(subCellImage.TmpImage.TerrainType))
                    {
                        tileIndex++;
                        return;
                    }
                }

                // Apply new overlay info
                if (tile.Overlay == null)
                {
                    tile.Overlay = new Overlay()
                    {
                        Position = tile.CoordsToPoint(),
                        OverlayType = entry.OverlayType,
                        FrameIndex = entry.Frame
                    };
                }
                else
                {
                    tile.Overlay.OverlayType = entry.OverlayType;
                    tile.Overlay.FrameIndex = entry.Frame;
                }

                // Smooth out tiberium
                tile.Overlay.FrameIndex = CursorActionTarget.Map.GetOverlayFrameIndex(tile.CoordsToPoint());

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

                if (OverlayCollection.Entries[0].OverlayType.Tiberium)
                {
                    ITileImage tileImage = CursorActionTarget.Map.TheaterInstance.GetTile(tile.TileIndex);
                    ISubTileImage subCellImage = tileImage.GetSubTile(tile.SubTileIndex);
                    if (Helpers.IsLandTypeImpassable(subCellImage.TmpImage.TerrainType))
                    {
                        index++;
                        return;
                    }
                }

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

        protected override bool CanDrawLinePreview() => _overlayCollection != null;

        protected override ICheckableMutation CreateRegularPlacementMutation(Point2D cellCoords)
        {
            return new PlaceOverlayCollectionMutation(CursorActionTarget.MutationTarget, OverlayCollection, cellCoords);
        }

        protected override Mutation CreateLinePlacementMutation(Direction direction, int length)
        {
            return new PlaceOverlayCollectionLineMutation(MutationTarget, OverlayCollection, LineSourceCell.Value, direction, length);
        }

        protected override void ApplyLine(Point2D cellCoords)
        {
            if (_overlayCollection != null)
            {
                (Direction direction, int length) = GetLineInformation(cellCoords);
                var mutation = CreateLinePlacementMutation(direction, length);
                PerformMutation(mutation);
            }
        }
    }
}
