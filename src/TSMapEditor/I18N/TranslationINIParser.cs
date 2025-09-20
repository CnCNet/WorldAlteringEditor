using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.I18N;

namespace TSMapEditor.I18N
{
    /// <summary>
    /// Handles INI attribute parsing with translation support for UI controls.
    /// </summary>
    public class TranslationINIParser : IControlINIAttributeParser
    {
        private static TranslationINIParser _instance;
        public static TranslationINIParser Instance => _instance ??= new TranslationINIParser();

        /// <summary>
        /// Shorthand for localization function.
        /// </summary>
        private string Localize(XNAControl control, string attributeName, string defaultValue, bool notify = true)
            => Translation.Instance.LookUp(control, attributeName, defaultValue, notify);

        public bool ParseINIAttribute(XNAControl control, IniFile iniFile, string key, string value)
        {
            Logger.Log($"[DEBUG] TranslationINIParser.ParseINIAttribute: control={control?.Name ?? "null"}, key={key}, value={value}");
            
            switch (key)
            {
                case "Text":
                    string localizedText = Localize(control, key, value);
                    Logger.Log($"[DEBUG] Text localization: '{value}' -> '{localizedText}'");
                    control.Text = localizedText;
                    return true;
                case "Size":
                    string[] size = Localize(control, key, value, notify: false).Split(',');
                    if (size.Length >= 2 && 
                        int.TryParse(size[0], CultureInfo.InvariantCulture, out int width) &&
                        int.TryParse(size[1], CultureInfo.InvariantCulture, out int height))
                    {
                        control.ClientRectangle = new Rectangle(control.X, control.Y, width, height);
                    }
                    return true;
                case "Width":
                    if (int.TryParse(Localize(control, key, value, notify: false), CultureInfo.InvariantCulture, out int w))
                    {
                        control.Width = w;
                    }
                    return true;
                case "Height":
                    if (int.TryParse(Localize(control, key, value, notify: false), CultureInfo.InvariantCulture, out int h))
                    {
                        control.Height = h;
                    }
                    return true;
                case "Location":
                    string[] location = Localize(control, key, value, notify: false).Split(',');
                    if (location.Length >= 2 &&
                        int.TryParse(location[0], CultureInfo.InvariantCulture, out int x) &&
                        int.TryParse(location[1], CultureInfo.InvariantCulture, out int y))
                    {
                        control.ClientRectangle = new Rectangle(x, y, control.Width, control.Height);
                    }
                    return true;
                case "X":
                    if (int.TryParse(Localize(control, key, value, notify: false), CultureInfo.InvariantCulture, out int xPos))
                    {
                        control.X = xPos;
                    }
                    return true;
                case "Y":
                    if (int.TryParse(Localize(control, key, value, notify: false), CultureInfo.InvariantCulture, out int yPos))
                    {
                        control.Y = yPos;
                    }
                    return true;
                case "DistanceFromRightBorder":
                    if (control.Parent != null &&
                        int.TryParse(Localize(control, key, value, notify: false), CultureInfo.InvariantCulture, out int rightDistance))
                    {
                        control.ClientRectangle = new Rectangle(
                            control.Parent.Width - control.Width - rightDistance,
                            control.Y,
                            control.Width, control.Height);
                    }
                    return true;
                case "DistanceFromBottomBorder":
                    if (control.Parent != null &&
                        int.TryParse(Localize(control, key, value, notify: false), CultureInfo.InvariantCulture, out int bottomDistance))
                    {
                        control.ClientRectangle = new Rectangle(
                            control.X,
                            control.Parent.Height - control.Height - bottomDistance,
                            control.Width, control.Height);
                    }
                    return true;
                case "ToolTip":
                    // Check if control has ToolTipText property using reflection
                    var toolTipProperty = control.GetType().GetProperty("ToolTipText");
                    if (toolTipProperty != null && toolTipProperty.CanWrite)
                    {
                        toolTipProperty.SetValue(control, Localize(control, key, value));
                    }
                    return true;
            }

            return false;
        }
    }
}