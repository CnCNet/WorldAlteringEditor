using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;

namespace TSMapEditor.Models
{
    /// <summary>
    /// A color as specified in the Tiberian Sun 
    /// Rules.ini file and stored in a format usable by MonoGame.
    /// </summary>
    public class RulesColor
    {
        public RulesColor(int index, string name, string tsHsvColorValue)
        {
            Index = index;
            Name = name;

            string[] hsvParts = tsHsvColorValue.Split(',');
            if (hsvParts.Length < 3)
                return;

            float hue = (Conversions.IntFromString(hsvParts[0], 0) * 360.0f) / 255.0f;
            float saturation = Conversions.IntFromString(hsvParts[1], 0) / 255.0f;
            float value = Conversions.IntFromString(hsvParts[2], 0) / 255.0f;

            // HSV to RGB conversion formula taken from https://www.rapidtables.com/convert/color/hsv-to-rgb.html
            float c = value * saturation;
            float x = c * (1 - Math.Abs(((hue / 60) % 2) - 1));
            float m = value - c;

            float r_ = 0f;
            float g_ = 0f;
            float b_ = 0f;

            if (hue < 60)
            {
                r_ = c;
                g_ = x;
            }
            else if (hue < 120)
            {
                r_ = x;
                g_ = c;
            }
            else if (hue < 180)
            {
                g_ = c;
                b_ = x;
            }
            else if (hue < 240)
            {
                g_ = x;
                b_ = c;
            }
            else if (hue < 300)
            {
                r_ = x;
                b_ = c;
            }
            else // if hue < 360
            {
                r_ = c;
                b_ = x;
            }

            int r = (int)((r_ + m) * 255);
            int g = (int)((g_ + m) * 255);
            int b = (int)((b_ + m) * 255);

            XNAColor = new Color(r, g, b);
        }

        public int Index { get; }
        public string Name { get; }
        public Color XNAColor { get; }
    }
}
