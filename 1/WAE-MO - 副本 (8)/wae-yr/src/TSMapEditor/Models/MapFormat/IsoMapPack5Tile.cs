using System;

namespace TSMapEditor.Models.MapFormat
{
    /// <summary>
    /// Low-level cell class.
    /// </summary>
    public class IsoMapPack5Tile
    {
        public const int Size = 11;

        public IsoMapPack5Tile() { }

        public IsoMapPack5Tile(byte[] data)
        {
            X = BitConverter.ToInt16(data, 0);
            Y = BitConverter.ToInt16(data, 2);
            TileIndex = BitConverter.ToInt32(data, 4);
            SubTileIndex = data[8];
            Level = data[9];
            IceGrowth = data[10];
        }

        public short X { get; set; }
        public short Y { get; set; }
        public int TileIndex { get; set; }
        public byte SubTileIndex { get; set; }
        public byte Level { get; set; }
        public byte IceGrowth { get; set; }
    }
}
