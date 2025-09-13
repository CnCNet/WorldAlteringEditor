using Microsoft.Xna.Framework;

namespace TSMapEditor.Misc
{
    public static class NamedColors
    {        
        public static NamedColor[] GenericSupportedNamedColors = new NamedColor[]
        {
            new NamedColor(Translate("NamedColors.Teal", "Teal"), new Color(0, 196, 196)),
            new NamedColor(Translate("NamedColors.Green", "Green"), new Color(0, 255, 0)),
            new NamedColor(Translate("NamedColors.Dark Green", "Dark Green"), Color.Green),
            new NamedColor(Translate("NamedColors.Lime Green", "Lime Green"), Color.LimeGreen),
            new NamedColor(Translate("NamedColors.Yellow", "Yellow"), Color.Yellow),
            new NamedColor(Translate("NamedColors.Orange", "Orange"), Color.Orange),
            new NamedColor(Translate("NamedColors.Red", "Red"), Color.Red),
            new NamedColor(Translate("NamedColors.BloodRed", "Blood Red"), Color.DarkRed),
            new NamedColor(Translate("NamedColors.Pink", "Pink"), Color.HotPink),
            new NamedColor(Translate("NamedColors.Cherry", "Cherry"), Color.Pink),
            new NamedColor(Translate("NamedColors.Purple", "Purple"), Color.MediumPurple),
            new NamedColor(Translate("NamedColors.BlueSky", "Sky Blue"), Color.SkyBlue),
            new NamedColor(Translate("NamedColors.Blue", "Blue"), new Color(40, 40, 255)),
            new NamedColor(Translate("NamedColors.Brown", "Brown"), Color.Brown),
            new NamedColor(Translate("NamedColors.Metalic", "Metalic"), new Color(160, 160, 200)),
        };
    }

    public struct NamedColor
    {
        public string Name;
        public Color Value;

        public NamedColor(string name, Color value)
        {
            Name = name;
            Value = value;
        }
    }
}
