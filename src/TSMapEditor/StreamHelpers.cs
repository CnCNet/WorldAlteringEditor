using System;
using System.IO;
using System.Text;

namespace TSMapEditor
{
    public class StreamHelperReadException : Exception
    {
        public StreamHelperReadException(string message) : base(message)
        {
        }
    }

    public static class StreamHelpers
    {
        public static int ReadInt(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[4];

            if (stream.Read(buffer) != sizeof(int))
                throw new StreamHelperReadException("Failed to read integer from stream: end of stream");

            return BitConverter.ToInt32(buffer);
        }

        public static long ReadLong(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[8];

            if (stream.Read(buffer) != sizeof(long))
                throw new StreamHelperReadException("Failed to read long integer from stream: end of stream");

            return BitConverter.ToInt64(buffer);
        }

        public static string ReadASCIIString(Stream stream)
        {
            int length = ReadInt(stream);
            Span<byte> stringBuffer = stackalloc byte[length];
            if (stream.Read(stringBuffer) != length)
                throw new StreamHelperReadException("Failed to read ASCII string from stream: end of stream");

            if (length == -1)
                return null;

            string result = Encoding.ASCII.GetString(stringBuffer);
            return result;
        }

        public static byte[] ASCIIStringToBytes(string str)
        {
            int length = sizeof(int) + str.Length;
            Span<byte> span = stackalloc byte[length];

            BitConverter.TryWriteBytes(span, str.Length);
            Encoding.ASCII.GetBytes(str, span.Slice(sizeof(int)));

            return span.ToArray();
        }

        public static string ReadUnicodeString(Stream stream)
        {
            int length = ReadInt(stream);

            if (length == -1)
                return null;

            Span<byte> stringBuffer = stackalloc byte[length];
            if (stream.Read(stringBuffer) != length)
                throw new StreamHelperReadException("Failed to read Unicode string from stream: end of stream");            

            string result = Encoding.UTF8.GetString(stringBuffer);
            return result;
        }

        public static byte[] UnicodeStringToBytes(string str)
        {
            int length = sizeof(int) + str.Length;
            Span<byte> span = stackalloc byte[length];

            BitConverter.TryWriteBytes(span, str.Length);
            Encoding.Unicode.GetBytes(str, span.Slice(sizeof(int)));

            return span.ToArray();
        }

        public static bool ReadBool(Stream stream)
        {
            return stream.ReadByte() == 1;
        }

        public static void WriteInt(Stream stream, int integer)
        {
            stream.Write(BitConverter.GetBytes(integer));
        }

        public static void WriteUShort(Stream stream, ushort shortNum)
        {
            stream.Write(BitConverter.GetBytes(shortNum));
        }

        public static void WriteUnicodeString(Stream stream, string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                stream.Write(BitConverter.GetBytes(-1));
            }
            else
            {
                byte[] bytes = Encoding.UTF8.GetBytes(str);
                stream.Write(BitConverter.GetBytes(bytes.Length));
                stream.Write(bytes);
            }
        }

        public static void WriteBool(Stream stream, bool value)
        {
            stream.WriteByte((byte)(value == true ? 1 : 0));
        }
    }
}
