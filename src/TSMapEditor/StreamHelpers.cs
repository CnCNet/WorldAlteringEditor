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
            byte[] buffer = new byte[8];

            if (stream.Read(buffer, 0, 4) != 4)
                throw new StreamHelperReadException("Failed to read integer from stream: end of stream");

            return BitConverter.ToInt32(buffer, 0);
        }

        public static long ReadLong(Stream stream)
        {
            byte[] buffer = new byte[8];

            if (stream.Read(buffer, 0, 8) != 8)
                throw new StreamHelperReadException("Failed to read integer from stream: end of stream");

            return BitConverter.ToInt64(buffer, 0);
        }

        public static string ReadASCIIString(Stream stream)
        {
            int length = ReadInt(stream);
            byte[] stringBuffer = new byte[length];
            if (stream.Read(stringBuffer, 0, length) != length)
                throw new StreamHelperReadException("Failed to read string from stream: end of stream");

            if (length == -1)
                return null;

            string result = Encoding.ASCII.GetString(stringBuffer);
            return result;
        }

        public static byte[] ASCIIStringToBytes(string str)
        {
            byte[] buffer = new byte[sizeof(int) + str.Length];
            Array.Copy(BitConverter.GetBytes(str.Length), buffer, sizeof(int));
            byte[] stringBytes = Encoding.ASCII.GetBytes(str);
            Array.Copy(stringBytes, 0, buffer, sizeof(int), stringBytes.Length);
            return buffer;
        }

        public static string ReadUnicodeString(Stream stream)
        {
            int length = ReadInt(stream);

            if (length == -1)
                return null;

            byte[] stringBuffer = new byte[length];
            if (stream.Read(stringBuffer, 0, length) != length)
                throw new StreamHelperReadException("Failed to read Unicode string from stream: end of stream");            

            string result = Encoding.UTF8.GetString(stringBuffer);
            return result;
        }

        public static byte[] UnicodeStringToBytes(string str)
        {            
            byte[] buffer = new byte[sizeof(int) + str.Length];
            
            Array.Copy(BitConverter.GetBytes(str.Length), buffer, sizeof(int));
            byte[] stringBytes = Encoding.UTF8.GetBytes(str);
            Array.Copy(stringBytes, 0, buffer, sizeof(int), stringBytes.Length);
            return buffer;
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
            byte[] bytes;

            if (string.IsNullOrEmpty(str))
            {
                stream.Write(BitConverter.GetBytes(-1));
            }
            else
            {
                bytes = Encoding.UTF8.GetBytes(str);
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
