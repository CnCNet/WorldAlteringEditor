using Rampastring.Tools;

namespace TSMapEditor.Models.ArtConfig
{
    public interface IArtConfig
    {
        void ReadFromIniSection(IniSection iniSection);
        bool Remapable { get; }
    }
}
