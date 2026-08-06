using Rampastring.Tools;

namespace MapEditorLibrary.Models.ArtConfig;

public interface IArtConfig
{
    void ReadFromIniSection(IniSection iniSection);
    bool Remapable { get; }
}
