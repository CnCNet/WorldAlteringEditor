using System;
using TSMapEditor.GameMath;

namespace TSMapEditor.CCEngine.TileData
{
    /// <summary>
    /// Base class for a full TMP tile, composed of one or more sub-tiles.
    /// </summary>
    public abstract class TileImage : ITileImage
    {
        protected TileImage(int width, int height, int tileSetId, int tileIndex, int tileId)
        {
            Width = width;
            Height = height;
            TileSetId = tileSetId;
            TileIndexInTileSet = tileIndex;
            TileID = tileId;
        }

        /// <summary>
        /// Width of the tile in cells.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Height of the tile in cells.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// The index of the tile set.
        /// </summary>
        public int TileSetId { get; set; }

        /// <summary>
        /// The index of the tile within its tileset.
        /// </summary>
        public int TileIndexInTileSet { get; set; }

        /// <summary>
        /// The unique ID of this tile within all tiles in the game.
        /// </summary>
        public int TileID { get; set; }

        public abstract ISubTileImage GetSubTile(int index);

        public Point2D? GetSubTileCoordOffset(int index)
        {
            if (GetSubTile(index) == null)
                return null;

            int x = index % Width;
            int y = index / Width;
            return new Point2D(x, y);
        }

        public abstract int SubTileCount { get; }

        /// <summary>
        /// Checks if a condition is true for any valid sub-tile.
        /// </summary>
        public bool CheckForAnyValidSubTile(Func<ISubTileImage, bool> condition)
        {
            for (int i = 0; i < SubTileCount; i++)
            {
                ISubTileImage image = GetSubTile(i);

                if (image?.TmpImage == null)
                    continue;

                if (condition(image))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Performs an action for all valid sub-tiles of the tile image.
        /// </summary>
        /// <param name="action">The action to perform. First parameter is the sub-tile, second parameter its offset within the tile, and the third parameter is the sub-tile's index.</param>
        public void DoForValidSubTiles(Action<ISubTileImage, Point2D, int> action)
        {
            for (int i = 0; i < SubTileCount; i++)
            {
                ISubTileImage image = GetSubTile(i);

                if (image == null)
                    continue;

                int cx = i % Width;
                int cy = i / Width;

                action(image, new Point2D(cx, cy), i);
            }
        }

        public bool Flat => !CheckForAnyValidSubTile(subTile => subTile.TmpImage.Height > 0);

        /// <summary>
        /// Calculates and returns the width of this full tile image.
        /// </summary>
        public int GetWidth(out int outMinX)
        {
            outMinX = 0;

            if (SubTileCount == 0)
                return 0;

            int maxX = int.MinValue;
            int minX = int.MaxValue;

            for (int i = 0; i < SubTileCount; i++)
            {
                var tmpData = GetSubTile(i)?.TmpImage;
                if (tmpData == null)
                    continue;

                if (tmpData.X < minX)
                    minX = tmpData.X;

                int cellRightXCoordinate = tmpData.X + Constants.CellSizeX;
                if (cellRightXCoordinate > maxX)
                    maxX = cellRightXCoordinate;

                if (tmpData.HasExtraData())
                {
                    int extraRightXCoordinate = tmpData.X + tmpData.XExtra + (int)tmpData.ExtraWidth;
                    if (extraRightXCoordinate > maxX)
                        maxX = extraRightXCoordinate;
                }
            }

            if (minX == int.MaxValue)
                return 0;

            outMinX = minX;
            return maxX - minX;
        }

        /// <summary>
        /// Calculates and returns the height of this full tile image.
        /// </summary>
        public int GetHeight()
        {
            if (SubTileCount == 0)
                return 0;

            int top = int.MaxValue;
            int bottom = int.MinValue;

            for (int i = 0; i < SubTileCount; i++)
            {
                var tmpData = GetSubTile(i)?.TmpImage;
                if (tmpData == null)
                    continue;

                int heightOffset = Constants.CellHeight * tmpData.Height;

                int cellTop = tmpData.Y - heightOffset;
                int cellBottom = cellTop + Constants.CellSizeY;

                if (cellTop < top)
                    top = cellTop;

                if (cellBottom > bottom)
                    bottom = cellBottom;

                if (tmpData.HasExtraData())
                {
                    int extraCellTop = tmpData.YExtra - heightOffset;
                    int extraCellBottom = extraCellTop + (int)tmpData.ExtraHeight;

                    if (extraCellTop < top)
                        top = extraCellTop;

                    if (extraCellBottom > bottom)
                        bottom = extraCellBottom;
                }
            }

            if (top == int.MaxValue)
                return 0;

            return bottom - top;
        }

        public int GetYOffset()
        {
            int height = GetHeight();

            int yOffset = 0;

            int maxTopCoord = int.MaxValue;
            int maxBottomCoord = int.MinValue;

            for (int i = 0; i < SubTileCount; i++)
            {
                var tmpData = GetSubTile(i)?.TmpImage;
                if (tmpData == null)
                    continue;

                int heightOffset = Constants.CellHeight * tmpData.Height;
                int cellTopCoord = tmpData.Y - heightOffset;
                int cellBottomCoord = tmpData.Y + Constants.CellSizeY - heightOffset;

                if (cellTopCoord < maxTopCoord)
                    maxTopCoord = cellTopCoord;

                if (cellBottomCoord > maxBottomCoord)
                    maxBottomCoord = cellBottomCoord;
            }

            for (int i = 0; i < SubTileCount; i++)
            {
                var tmpData = GetSubTile(i)?.TmpImage;
                if (tmpData == null)
                    continue;

                if (tmpData.HasExtraData())
                {
                    int heightOffset = Constants.CellHeight * tmpData.Height;

                    int extraTopCoord = tmpData.YExtra - heightOffset;
                    int extraBottomCoord = tmpData.YExtra + (int)tmpData.ExtraHeight - heightOffset;

                    if (extraTopCoord < maxTopCoord)
                        maxTopCoord = extraTopCoord;

                    if (extraBottomCoord > maxBottomCoord)
                        maxBottomCoord = extraBottomCoord;
                }
            }

            if (maxTopCoord == int.MaxValue)
                return 0;

            if (maxTopCoord < 0)
                yOffset = -maxTopCoord;
            else if (maxBottomCoord > height)
                yOffset = -(maxBottomCoord - height);

            return yOffset;
        }
    }
}
