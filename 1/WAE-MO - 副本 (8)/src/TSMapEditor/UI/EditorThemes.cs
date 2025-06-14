using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;

namespace TSMapEditor.UI
{
    public static class EditorThemes
    {
        public static Dictionary<string, UISettings> Themes { get; set; } = new Dictionary<string, UISettings>();

        public static void Initialize()
        {
            var iniFile = Helpers.ReadConfigINI("EditorThemes.ini");
            var themesSection = iniFile.GetSection("EditorThemes");

            if (themesSection == null || themesSection.Keys.Count == 0)
            {
                Themes.Add("Default", new CustomUISettings());
                return;
            }

            foreach (var themeName in themesSection.Keys)
            {
                var themeSection = iniFile.GetSection(themeName.Value);
                if (themeSection == null)
                    continue;

                var theme = new CustomUISettings();
                foreach (var property in theme.GetType().GetProperties())
                {
                    if (!themeSection.KeyExists(property.Name))
                        continue;

                    if (property.PropertyType == typeof(Color))
                    {
                        string colorString = themeSection.GetStringValue(property.Name, null);
                        if (colorString == null)
                            continue;

                        Color color = Helpers.ColorFromString(colorString);
                        property.SetValue(theme, color);
                    }
                    else if (property.PropertyType == typeof(float))
                    {
                        property.SetValue(theme, themeSection.GetSingleValue(property.Name, (float)property.GetValue(theme)));
                    }
                    else if (property.PropertyType == typeof(Texture2D))
                    {
                        string textureString = themeSection.GetPathStringValue(property.Name, null);
                        if (textureString == null)
                            continue;

                        property.SetValue(theme, AssetLoader.LoadTexture(textureString));
                    }
                }

                Themes.Add(themeName.Value, theme);
            }

            if (!Themes.ContainsKey("Default"))
            {
                Themes.Add("Default", new CustomUISettings());
            }
        }
    }
}
