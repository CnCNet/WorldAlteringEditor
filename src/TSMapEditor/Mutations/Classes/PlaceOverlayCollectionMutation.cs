using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that allows placing overlay collections.
    /// </summary>
    class PlaceOverlayCollectionMutation : Mutation
    {
        public PlaceOverlayCollectionMutation(IMutationTarget mutationTarget, OverlayCollection overlayCollection, Point2D cellCoords) : base(mutationTarget)
        {
            this.overlayCollection = overlayCollection;
            this.brush = mutationTarget.BrushSize;
            this.cellCoords = cellCoords;
        }

        private readonly OverlayCollection overlayCollection;
        private readonly BrushSize brush;
        private readonly Point2D cellCoords;

        private OriginalOverlayInfo[] undoData;

        public override string GetDisplayString()
        {
            return string.Format(Translate(this, "DisplayString", 
                "Place overlay collection {0} at {1} with a brush size of {2}"),
                    overlayCollection.Name, cellCoords, brush);
        }

        public override void Perform()
        {
            var originalOverlayInfos = new List<OriginalOverlayInfo>();

            brush.DoForBrushSize(offset =>
            {
                var tile = MutationTarget.Map.GetTile(cellCoords + offset);
                if (tile == null)
                    return;

                var collectionEntry = overlayCollection.Entries[MutationTarget.Randomizer.GetRandomNumber(0, overlayCollection.Entries.Length - 1)];
                
                if (collectionEntry.OverlayType.Tiberium)
                {
                    // Don't allow placing tiberium on impassable tiles, it's a common mapping error
                    // that leads to harvesters getting stuck

                    TileImage tileGraphics = MutationTarget.TheaterGraphics.GetTileGraphics(tile.TileIndex);
                    MGTMPImage subCellImage = tileGraphics.TMPImages[tile.SubTileIndex];
                    if (Helpers.IsLandTypeImpassable(subCellImage.TmpImage.TerrainType, true))
                        return;
                }

                originalOverlayInfos.Add(new OriginalOverlayInfo()
                {
                    CellCoords = tile.CoordsToPoint(),
                    OverlayTypeIndex = tile.Overlay == null ? -1 : tile.Overlay.OverlayType.Index,
                    FrameIndex = tile.Overlay == null ? -1 : tile.Overlay.FrameIndex,
                });

                tile.Overlay = new Overlay()
                {
                     Position = tile.CoordsToPoint(),
                     OverlayType = collectionEntry.OverlayType,
                     FrameIndex = collectionEntry.Frame
                };
            });

            for (int y = -brush.Height - 1; y <= brush.Height + 1; y++)
            {
                for (int x = -brush.Width - 1; x <= brush.Width + 1; x++)
                {
                    SetOverlayFrameIndexForTile(cellCoords + new Point2D(x, y));
                }
            }

            undoData = originalOverlayInfos.ToArray();
            MutationTarget.AddRefreshPoint(cellCoords, Math.Max(brush.Width, brush.Height) + 1);
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

            for (int y = -brush.Height - 1; y <= brush.Height + 1; y++)
            {
                for (int x = -brush.Width - 1; x <= brush.Width + 1; x++)
                {
                    SetOverlayFrameIndexForTile(cellCoords + new Point2D(x, y));
                }
            }

            MutationTarget.AddRefreshPoint(cellCoords, Math.Max(brush.Width, brush.Height) + 1);
        }
    }
}
