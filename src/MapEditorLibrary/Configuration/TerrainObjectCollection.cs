using MapEditorLibrary.Misc;
using MapEditorLibrary.Models;
using Rampastring.Tools;

namespace MapEditorLibrary.Configuration;

/// <summary>
/// Combines many terrain objects into a single entry.
/// </summary>
public class TerrainObjectCollection : ObjectTypeCollection
{
    public struct TerrainObjectCollectionEntry
    {
        public TerrainType TerrainType;

        public TerrainObjectCollectionEntry(TerrainType terrainType)
        {
            TerrainType = terrainType;
        }
    }

    public TerrainObjectCollectionEntry[] Entries;

    public static TerrainObjectCollection InitFromIniSection(IniSection iniSection, List<TerrainType> terrainTypes)
    {
        var terrainObjectCollection = new TerrainObjectCollection();
        terrainObjectCollection.Name = iniSection.GetStringValue("Name", "Unnamed Collection");
        terrainObjectCollection.UIName = Translate(terrainObjectCollection, terrainObjectCollection.Name, terrainObjectCollection.Name);
        terrainObjectCollection.AllowedTheaters = iniSection.GetListValue("AllowedTheaters", ',', s => s);

        var entryList = new List<TerrainObjectCollectionEntry>();

        int i = 0;
        while (true)
        {
            string terrainTypeName = iniSection.GetStringValue("TerrainObjectType" + i, null);
            if (string.IsNullOrWhiteSpace(terrainTypeName))
                break;

            var terrainType = terrainTypes.Find(o => o.ININame == terrainTypeName);
            if (terrainType == null)
            {
                throw new INIConfigException($"Terrain object type \"{terrainTypeName}\" not found while initializing terrain object collection \"{terrainObjectCollection.Name}\"!");
            }

            entryList.Add(new TerrainObjectCollectionEntry(terrainType));

            i++;
        }

        terrainObjectCollection.Entries = entryList.ToArray();
        return terrainObjectCollection;
    }
}
