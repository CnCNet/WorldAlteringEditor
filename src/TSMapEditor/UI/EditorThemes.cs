using MapEditorLibrary;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.Graphics;
using System.Collections.Generic;

namespace TSMapEditor.UI;

public static class EditorThemes
{
    public static Dictionary<string, UISettings> Themes { get; set; } = new Dictionary<string, UISettings>();

    private static readonly Dictionary<(int Width, int Height, Color Main, Color Secondary, Color Tertiary), TextureReference> buttonTextures = new();

    internal static TextureReference GetButtonTexture(int width, int height, Color main, Color secondary, Color tertiary)
    {
        var key = (width, height, main, secondary, tertiary);
        if (buttonTextures.TryGetValue(key, out TextureReference texture))
            return texture;

        Color[] pixels = Helpers.CreateUITexturePixels(width, height, main, secondary, tertiary);
        texture = AssetLoader.CreateUITexture(pixels, width, height, premultiplyAlpha: false);
        buttonTextures.Add(key, texture);

        return texture;
    }

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
                else if (property.PropertyType == typeof(TextureReference))
                {
                    string textureString = themeSection.GetPathStringValue(property.Name, null);
                    if (textureString == null)
                        continue;

                    property.SetValue(theme, AssetLoader.LoadUITexture(textureString));
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
