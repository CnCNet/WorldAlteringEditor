using System.Collections.Generic;
using TSMapEditor.CCEngine.TileData;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that allows placing overlay collections in a line on the map.
    /// </summary>
    class PlaceOverlayCollectionLineMutation : Mutation
    {
        public PlaceOverlayCollectionLineMutation(IMutationTarget mutationTarget, OverlayCollection overlayCollection, Point2D sourceCoords, Direction direction, int length) : base(mutationTarget)
        {
            this.overlayCollection = overlayCollection;
            this.sourceCoords = sourceCoords;
            this.direction = direction;
            this.length = length;
        }

        private readonly OverlayCollection overlayCollection;
        private readonly Point2D sourceCoords;
        private readonly Direction direction;
        private readonly int length;

        private OriginalOverlayInfo[] undoData;

        public override string GetDisplayString()
        {
            string directionString = Translate("Direction." + direction, Helpers.DirectionToName(direction));
            return string.Format(Translate(this, "DisplayString",
                "Place overlay collection '{0}' in a line from {1} towards {2} with length {3}"),
                    overlayCollection.Name, sourceCoords, directionString, length);
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

                var collectionEntry = overlayCollection.Entries[MutationTarget.Randomizer.GetRandomNumber(0, overlayCollection.Entries.Length - 1)];

                if (collectionEntry.OverlayType.Tiberium)
                {
                    ITileImage tileGraphics = MutationTarget.Map.TheaterInstance.GetTile(tile.TileIndex);
                    ISubTileImage subCellImage = tileGraphics.GetSubTile(tile.SubTileIndex);
                    if (Helpers.IsLandTypeImpassable(subCellImage.TmpImage.TerrainType, true))
                        continue;
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
            }

            UpdateFrameIndexesAroundLine(step);

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

            UpdateFrameIndexesAroundLine(Helpers.VisualDirectionToPoint(direction));

            MutationTarget.AddRefreshPoint(sourceCoords, length + 2);
        }
    }
}
