using Rampastring.Tools;

namespace MapEditorLibrary.Models.ArtConfig;

public class TerrainArtConfig : IArtConfig
{
    /// <summary>
    /// Defined in Art.ini. If set to true,
    /// the art for this terrain type is theater-specific;
    /// if false, the art is a generic .SHP used for every theater.
    /// </summary>
    public bool Theater { get; set; }

    public string Image { get; set; }
    public bool Remapable => false;

    /// <summary>
    /// Palette override for TerrainTypes in Phobos
    /// </summary>
    public string Palette { get; set; }

    public void ReadFromIniSection(IniSection iniSection)
    {
        Theater = iniSection.GetBooleanValue(nameof(Theater), Theater);
        Image = iniSection.GetStringValue(nameof(Image), Image);
        Palette = iniSection.GetStringValue(nameof(Palette), Palette);
    }
}
