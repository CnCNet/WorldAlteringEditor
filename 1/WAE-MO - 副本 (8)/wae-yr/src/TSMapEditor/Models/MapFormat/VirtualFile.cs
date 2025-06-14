using System;
using System.IO;
using System.Linq;
using System.Text;

namespace CNCMaps.FileFormats.VirtualFileSystem
{

    /// <summary>
    /// Virtual file class
    /// </summary>
    public class VirtualFile : Stream
    {
        public Stream BaseStream { get; protected internal set; }
        protected int BaseOffset;
        protected long Size;
        protected long Pos;
        public virtual string FileName { get; set; }

        byte[] buff;
        readonly bool isBuffered;
        bool isBufferInitialized;

        public VirtualFile(Stream baseStream, string filename, int baseOffset, long fileSize, bool isBuffered = false)
        {
            Size = fileSize;
            BaseOffset = baseOffset;
            BaseStream = baseStream;
            this.isBuffered = isBuffered;
            FileName = filename;
        }

        public VirtualFile(Stream baseStream, string filename = "", bool isBuffered = false)
        {
            BaseStream = baseStream;
            BaseOffset = 0;
            Size = baseStream.Length;
            this.isBuffered = isBuffered;
            FileName = filename;
        }

        public override bool CanRead => Pos < Size;

        public override bool CanWrite => false;

        public override long Length => Size;

        public override void Flush()
        {

        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            count = Math.Min(count, (int)(Length - Position));
            if (isBuffered)
            {
                if (!isBufferInitialized)
                    InitBuffer();

                Array.Copy(buff, Pos, buffer, offset, count);
            }
            else
            {
                // ensure
                BaseStream.Position = BaseOffset + Pos;
                BaseStream.Read(buffer, offset, count);
            }
            Pos += count;
            return count;
        }

        public string ReadCString(int count)
        {
            var arr = Read(count);
            var sb = new StringBuilder();
            int i = 0;
            while (i < count && arr[i] != 0)
                sb.Append((char)arr[i++]);
            return sb.ToString();
        }

        public unsafe int Read(byte* buffer, int count)
        {
            count = Math.Min(count, (int)(Length - Position));
            if (isBuffered)
            {
                if (!isBufferInitialized)
                    InitBuffer();

                for (int i = 0; i < count; i++)
                    *buffer++ = buff[Pos + i];
            }
            else
            {
                // ensure
                BaseStream.Position = BaseOffset + Pos;
                byte[] rbuff = new byte[count];
                BaseStream.Read(rbuff, 0, count);
                for (int i = 0; i < count; i++)
                    *buffer++ = rbuff[i];
            }
            Pos += count;
            return count;
        }

        private void InitBuffer()
        {
            // ensure
            BaseStream.Position = BaseOffset + Pos;
            buff = new byte[Size];
            BaseStream.Read(buff, 0, (int)Size);
            isBufferInitialized = true;
        }

        public byte[] Read(int numBytes)
        {
            var ret = new byte[numBytes];
            Read(ret, 0, numBytes);
            return ret;
        }

        public sbyte[] ReadSigned(int numBytes)
        {
            var b = new byte[numBytes];
            Read(b, 0, numBytes);
            sbyte[] ret = new sbyte[numBytes];
            Buffer.BlockCopy(b, 0, ret, 0, b.Length);
            return ret;
        }

        public new byte ReadByte()
        {
            return ReadUInt8();
        }

        public sbyte ReadSByte()
        {
            return unchecked((sbyte)ReadUInt8());
        }

        public byte ReadUInt8()
        {
            return Read(1)[0];
        }

        public int ReadInt32()
        {
            return BitConverter.ToInt32(Read(sizeof(Int32)), 0);
        }

        public uint ReadUInt32()
        {
            return BitConverter.ToUInt32(Read(sizeof(UInt32)), 0);
        }

        public short ReadInt16()
        {
            return BitConverter.ToInt16(Read(sizeof(Int16)), 0);
        }

        public ushort ReadUInt16()
        {
            return BitConverter.ToUInt16(Read(sizeof(UInt16)), 0);
        }

        public float ReadFloat()
        {
            return BitConverter.ToSingle(Read(sizeof(Single)), 0);
        }

        public float ReadFloat2()
        {
            var ori = Read(sizeof(Single)).ToList();
            byte[] rev = new[] { ori[3], ori[2], ori[1], ori[0] };
            return BitConverter.ToSingle(rev, 0);
        }

        public double ReadDouble()
        {
            return BitConverter.ToDouble(Read(sizeof(Double)), 0);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            base.Close();
            BaseStream?.Close();
        }

        public override void SetLength(long value)
        {
            Size = value;
        }

        public override long Position
        {
            get
            {
                return Pos;
            }
            set
            {
                Pos = value;
                if (!isBuffered && Pos + BaseOffset != BaseStream.Position)
                    BaseStream.Seek(Pos + BaseOffset, SeekOrigin.Begin);
            }
        }

        public long Remaining
        {
            get { return Length - Pos; }
        }

        public bool Eof
        {
            get { return Remaining <= 0; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
            }
            return Position;
        }

        public override string ToString()
        {
            return FileName;
        }
    }
}