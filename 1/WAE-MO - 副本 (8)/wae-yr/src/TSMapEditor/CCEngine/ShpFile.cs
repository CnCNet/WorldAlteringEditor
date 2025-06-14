using System;
using System.Collections.Generic;
using System.IO;

namespace TSMapEditor.CCEngine
{
    public class ShpLoadException : Exception
    {
        public ShpLoadException(string message) : base(message)
        {
        }
    }

    [Flags]
    public enum ShpCompression
    {
        None = 0,
        HasTransparency = 1,
        UsesRle = 2
    }

    /// <summary>
    /// Represents the header of a SHP file.
    /// </summary>
    struct ShpFileHeader
    {
        public const int SizeOf = 8;

        public ShpFileHeader(byte[] buffer)
        {
            if (buffer.Length < SizeOf)
                throw new ShpLoadException(nameof(ShpFileHeader) + ": buffer is not long enough");

            Unknown = BitConverter.ToUInt16(buffer, 0);
            if (Unknown != 0)
                throw new ShpLoadException("Unexpected field value in SHP header");

            SpriteWidth = BitConverter.ToUInt16(buffer, 2);
            SpriteHeight = BitConverter.ToUInt16(buffer, 4);
            FrameCount = BitConverter.ToUInt16(buffer, 6);
        }

        public ushort Unknown;
        public ushort SpriteWidth;
        public ushort SpriteHeight;
        public ushort FrameCount;
    }

    /// <summary>
    /// Represents the information of a single frame in a SHP file.
    /// </summary>
    public class ShpFrameInfo
    {
        private const int SizeOf = 24;

        public ShpFrameInfo(Stream stream)
        {
            if (stream.Length < stream.Position + SizeOf)
                throw new ShpLoadException(nameof(ShpFrameInfo) + ": buffer is not long enough");

            XOffset = ReadUShortFromStream(stream);
            YOffset = ReadUShortFromStream(stream);
            Width = ReadUShortFromStream(stream);
            Height = ReadUShortFromStream(stream);
            Flags = (ShpCompression)ReadUIntFromStream(stream);
            byte r = (byte)stream.ReadByte();
            byte g = (byte)stream.ReadByte();
            byte b = (byte)stream.ReadByte();
            AverageColor = new RGBColor(r, g, b);
            Unknown1 = (byte)stream.ReadByte();
            Unknown2 = ReadUIntFromStream(stream);
            DataOffset = ReadUIntFromStream(stream);
        }

        private ushort ReadUShortFromStream(Stream stream)
        {
            stream.Read(buffer, 0, 2);
            return BitConverter.ToUInt16(buffer, 0);
        }

        private uint ReadUIntFromStream(Stream stream)
        {
            stream.Read(buffer, 0, 4);
            return BitConverter.ToUInt32(buffer, 0);
        }

        byte[] buffer = new byte[4];

        public ushort XOffset;
        public ushort YOffset;
        public ushort Width;
        public ushort Height;
        public ShpCompression Flags;
        public RGBColor AverageColor;
        public byte Unknown1;
        public uint Unknown2;
        public uint DataOffset;
    }

    /// <summary>
    /// Represents a SHP file. Combines the header and frame information
    /// and makes it possible to parse the actual graphical data.
    /// </summary>
    public class ShpFile
    {
        public ShpFile() { }

        public ShpFile(string fileName)
        {
            this.fileName = fileName;
        }

        private readonly string fileName;

        private ShpFileHeader shpFileHeader;
        private List<ShpFrameInfo> shpFrameInfos;

        

        public int FrameCount => shpFrameInfos.Count;

        public int Width => shpFileHeader.SpriteWidth;
        public int Height => shpFileHeader.SpriteHeight;

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
            try
            {
                shpFileHeader = new ShpFileHeader(buffer);
                shpFrameInfos = new List<ShpFrameInfo>(shpFileHeader.FrameCount);

                using (var memoryStream = new MemoryStream(buffer))
                {
                    memoryStream.Position = ShpFileHeader.SizeOf;

                    for (int i = 0; i < shpFileHeader.FrameCount; i++)
                    {
                        var shpFrameInfo = new ShpFrameInfo(memoryStream);
                        shpFrameInfos.Add(shpFrameInfo);
                    }
                }
            }
            catch (ShpLoadException ex)
            {
                throw new ShpLoadException("Failed to load SHP file. Make sure that the file is not corrupted. Filename: " + fileName + ", original exception: " + ex.Message);
            }
        }

        public ShpFrameInfo GetShpFrameInfo(int frameIndex) => shpFrameInfos[frameIndex];

        public byte[] GetUncompressedFrameData(int frameIndex, byte[] fileData)
        {
            ShpFrameInfo frameInfo = shpFrameInfos[frameIndex];

            if (frameInfo.DataOffset == 0)
                return null;

            byte[] frameData = new byte[frameInfo.Width * frameInfo.Height];

            if ((frameInfo.Flags & ShpCompression.UsesRle) == ShpCompression.None)
            {
                for (int i = 0; i < frameData.Length; i++)
                {
                    frameData[i] = fileData[frameInfo.DataOffset + i];
                }
            }
            else
            {
                DecompressRLEZero(frameData, frameIndex, frameInfo, fileData);
            }

            return frameData;
        }

        private void DecompressRLEZero(byte[] frameData, int frameIndex, ShpFrameInfo frameInfo, byte[] fileData)
        {
            // https://moddingwiki.shikadi.net/wiki/Westwood_RLE-Zero

            int dataOffset = 0;

            // Read SHP line-by-line. RLE-zero only compresses the transparent parts (zero bytes) of each line, individually.
            for (int lineIndex = 0; lineIndex < frameInfo.Height; lineIndex++)
            {
                int lineDataStartOffset = (int)frameInfo.DataOffset + dataOffset;

                // Compose little-endian UInt16 from 2 bytes
                int lineDataLength = fileData[lineDataStartOffset] | (fileData[lineDataStartOffset + 1] << 8);

                if (lineDataStartOffset + lineDataLength > fileData.Length || lineDataLength < 2)
                {
                    throw new ShpLoadException($"Line data length out-of-bounds in SHP RLE-Zero frame. " +
                        $"File name: {fileName}, frame index: {frameIndex}, line data length: {lineDataLength}");
                }

                // Line length includes the two-byte line data length value, skip it
                int currentByteIndex = 2;

                // Define variable for current pixel # on the current line
                int pixelPositionOnLine = 0;

                // Read image damage of current line
                while (currentByteIndex < lineDataLength)
                {
                    byte value = fileData[lineDataStartOffset + currentByteIndex];

                    if (value == 0)
                    {
                        // If we are at the end of a line when we encounter a zero, something is wrong.
                        if (currentByteIndex == lineDataLength - 1)
                        {
                            throw new ShpLoadException($"Zero-byte encountered at end of line data in SHP RLE-Zero frame. " +
                                $"File name: {fileName}, line index: {lineIndex}, file offset: {lineDataStartOffset + currentByteIndex}");
                        }

                        // A zero value means transparent pixels. The following byte
                        // defines how many pixels are transparent.
                        byte transparentPixelCount = fileData[lineDataStartOffset + currentByteIndex + 1];

                        // -1 prevents us from counting the current pixel "twice"
                        if (pixelPositionOnLine + transparentPixelCount - 1 > frameInfo.Width)
                        {
                            throw new ShpLoadException($"Out-of-bounds pixel position on transparent data in SHP RLE-Zero frame. " +
                                $"File name: {fileName}, frame index: {frameIndex}, line index: {lineIndex}, file offset: {lineDataStartOffset + currentByteIndex}");
                        }

                        // Assign transparent pixel data
                        while (transparentPixelCount > 0)
                        {
                            frameData[lineIndex * frameInfo.Width + pixelPositionOnLine] = 0;
                            pixelPositionOnLine++;
                            transparentPixelCount--;
                        }

                        // Advance buffer position
                        currentByteIndex += 2;
                    }
                    else
                    {
                        if (pixelPositionOnLine >= frameInfo.Width)
                        {
                            throw new ShpLoadException($"Out-of-bounds pixel position on color data in SHP RLE-Zero frame. " +
                                $"File name: {fileName}, frame index: {frameIndex}, line index: {lineIndex}, file offset: {lineDataStartOffset + currentByteIndex}");
                        }

                        // A non-zero value is color data that should be just applied directly.
                        frameData[lineIndex * frameInfo.Width + pixelPositionOnLine] = value;
                        pixelPositionOnLine++;
                        currentByteIndex++;
                    }
                }

                dataOffset += lineDataLength;
            }
        }
    }


}
