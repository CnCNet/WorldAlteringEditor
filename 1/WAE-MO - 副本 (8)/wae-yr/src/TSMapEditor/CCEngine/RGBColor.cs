using Microsoft.Xna.Framework;

namespace TSMapEditor.CCEngine
{
    public struct RGBColor
    {
        public RGBColor(byte[] buffer, int offset, int shift)
        {
            R = (byte)(buffer[offset] << shift);
            G = (byte)(buffer[offset + 1] << shift);
            B = (byte)(buffer[offset + 2] << shift);
        }

        public RGBColor(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public byte R;
        public byte G;
        public byte B;

        public Color ToXnaColor() => new Color(R, G, B);
    }
}
