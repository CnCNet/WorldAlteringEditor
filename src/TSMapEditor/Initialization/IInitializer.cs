using Rampastring.Tools;
using TSMapEditor.Models;

namespace TSMapEditor.Initialization
{
    public interface IInitializer
    {
        void ReadObjectTypePropertiesFromINI<T>(T obj, IniFile iniFile) where T : INIDefineable, INIDefined;
        void ReadObjectTypeArtPropertiesFromINI<T>(T obj, IniFile iniFile) where T : AbstractObject, INIDefined;
        void ReadObjectTypeArtPropertiesFromINI<T>(T obj, IniFile iniFile, string sectionName) where T : AbstractObject, INIDefined;
    }
}
