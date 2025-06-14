using System;
using System.Collections.Generic;
using System.IO;

namespace TSMapEditor.CCEngine
{
    /// <summary>
    /// A Tiberian Sun TMP file.
    /// </summary>
    public class TmpFile
    {
        public TmpFile(string fileName)
        {
            this.fileName = fileName;
        }

        private readonly string fileName;

        private TmpFileHeader tmpFileHeader;
        private List<TmpImage> tmpImages = new List<TmpImage>();

        public int ImageCount => tmpImages.Count;
        public TmpImage GetImage(int id) => tmpImages[id];

        public int CellsX => tmpFileHeader.Width;

        public int CellsY => tmpFileHeader.Height;

        public void ParseFromFile(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            {
                Parse(stream);
            }
        }

        public void Parse(Stream stream)
        {
            byte[] buffer = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(buffer, 0, buffer.Length);
            ParseFromBuffer(buffer);
        }

        public void ParseFromBuffer(byte[] buffer)
        {
            tmpFileHeader = new TmpFileHeader(buffer);
            int tileCount = tmpFileHeader.Width * tmpFileHeader.Height;

            List<int> tmpHeaderOffsets = new List<int>();

            for (int i = 0; i < tileCount; i++)
            {
                int offset = BitConverter.ToInt32(buffer, TmpFileHeader.SIZE + i * 4);
                tmpHeaderOffsets.Add(offset);
            }

            using (var memoryStream = new MemoryStream(buffer))
            {
                for (int i = 0; i < tileCount; i++)
                {
                    if (tmpHeaderOffsets[i] == 0)
                    {
                        tmpImages.Add(null);
                    }
                    else
                    {
                        memoryStream.Position = tmpHeaderOffsets[i];
                        TmpImage tmpImage = new TmpImage(memoryStream, fileName);
                        tmpImages.Add(tmpImage);
                    }
                }
            }
        }
    }

    /// <summary>
    /// A TMP file header.
    /// </summary>
    struct TmpFileHeader
    {
        public const int SIZE = 16;

        public TmpFileHeader(byte[] buffer)
        {
            if (buffer.Length < SIZE)
                throw new ArgumentException("buffer is not long enough");

            Width = BitConverter.ToInt32(buffer, 0);
            Height = BitConverter.ToInt32(buffer, 4);
            TileWidth = BitConverter.ToInt32(buffer, 8);
            TileHeight = BitConverter.ToInt32(buffer, 12);
        }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int TileWidth { get; private set; }
        public int TileHeight { get; private set; }
    }

    /// <summary>
    /// A single TMP image (representing a single cell).
    /// </summary>
    public class TmpImage
    {
        public const int IMAGE_HEADER_SIZE = 48;

        public TmpImage(Stream stream, string fileName)
        {
            long expectedLength = stream.Position + IMAGE_HEADER_SIZE + Constants.TileColorBufferSize;
            if (stream.Length < expectedLength)
            {
                throw new ArgumentException($"TMP file buffer ran out unexpectedly while reading ${fileName}: " +
                    $"expected length of at least {expectedLength}, actual length: {stream.Length}");
            }

            X = ReadIntFromStream(stream);
            Y = ReadIntFromStream(stream);
            ExtraDataOffset = ReadUIntFromStream(stream);
            ZDataOffset = ReadUIntFromStream(stream);
            ExtraZDataOffset = ReadUIntFromStream(stream);
            XExtra = ReadIntFromStream(stream);
            YExtra = ReadIntFromStream(stream);
            ExtraWidth = ReadUIntFromStream(stream);
            ExtraHeight = ReadUIntFromStream(stream);
            stream.Read(buffer, 0, 4);
            // The image flags of WW tiles contain
            // trash / uninitialized memory which we have to clear
            ImageFlags = (TmpImageFlags)(BitConverter.ToUInt32(buffer, 0));
            stream.Read(buffer, 0, 3);
            Height = buffer[0];
            TerrainType = buffer[1];
            RampType = (RampType)buffer[2];
            RadarLeftColor = ReadRGBColorFromStream(stream);
            RadarRightColor = ReadRGBColorFromStream(stream);
            stream.Read(buffer, 0, 3); // Discard 3 more bytes of WW trash data / uninitialized memory
            stream.Read(ColorData, 0, Constants.TileColorBufferSize);

            if ((ImageFlags & TmpImageFlags.HasZData) == TmpImageFlags.HasZData)
            {
                ZData = new byte[Constants.TileColorBufferSize];
                stream.Read(ZData, 0, ZData.Length);
            }
             
            if ((ImageFlags & TmpImageFlags.HasExtraData) == TmpImageFlags.HasExtraData)
            {
                ExtraGraphicsColorData = new byte[ExtraWidth * ExtraHeight];
                stream.Read(ExtraGraphicsColorData, 0, ExtraGraphicsColorData.Length);
            
                if ((ImageFlags & TmpImageFlags.HasZData) == TmpImageFlags.HasZData && ExtraZDataOffset > 0)
                {
                    ExtraGraphicsZData = new byte[ExtraWidth * ExtraHeight];
                    stream.Read(ExtraGraphicsZData, 0, ExtraGraphicsZData.Length);
                }
            }
        }

        public void FreeImageData()
        {
            ColorData = null;
            ZData = null;
            ExtraGraphicsColorData = null;
            ExtraGraphicsZData = null;
        }

        private int ReadIntFromStream(Stream stream)
        {
            stream.Read(buffer, 0, 4);
            return BitConverter.ToInt32(buffer, 0);
        }

        private uint ReadUIntFromStream(Stream stream)
        {
            stream.Read(buffer, 0, 4);
            return BitConverter.ToUInt32(buffer, 0);
        }

        private RGBColor ReadRGBColorFromStream(Stream stream)
        {
            stream.Read(buffer, 0, 3);
            return new RGBColor(buffer, 0, 0);
        }

        byte[] buffer = new byte[4];

        public int X { get; private set; }
        public int Y { get; private set; }
        public uint ExtraDataOffset { get; private set; }
        public uint ZDataOffset { get; private set; }
        public uint ExtraZDataOffset { get; private set; }
        public int XExtra { get; private set; }
        public int YExtra { get; private set; }
        public uint ExtraWidth { get; private set; }
        public uint ExtraHeight { get; private set; }
        public TmpImageFlags ImageFlags { get; private set; }
        public byte Height { get; private set; }
        public byte TerrainType { get; private set; }
        public RampType RampType { get; private set; }

        public RGBColor RadarLeftColor { get; set; }
        public RGBColor RadarRightColor { get; set; }

        public byte[] ColorData = new byte[Constants.TileColorBufferSize];
        public byte[] ZData = new byte[0];
        public byte[] ExtraGraphicsColorData = new byte[0];
        public byte[] ExtraGraphicsZData = new byte[0];
    }

    [Flags]
    public enum TmpImageFlags : uint
    {
        None = 0,
        HasExtraData = 0x01,
        HasZData = 0x02,
        HasDamagedData = 0x04
    }
}
