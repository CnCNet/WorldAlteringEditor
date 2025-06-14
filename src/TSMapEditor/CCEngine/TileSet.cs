using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using TSMapEditor.Models;

namespace TSMapEditor.CCEngine
{
    public class TileSet : INIDefineable
    {
        public TileSet(int index)
        {
            Index = index;
            SortID = index.ToString();
        }

        public int Index { get; }
        public string SortID { get; set; }
        public string SetName { get; set; }
        public string FileName { get; set; }
        public int TilesInSet { get; set; }
        public bool Morphable { get; set; }
        public int MarbleMadness { get; set; } = -1;
        public int NonMarbleMadness { get; set; } = -1;
        public bool AllowTiberium { get; set; }
        public bool AllowToPlace { get; set; } = true;
        public bool Only1x1 { get; set; }
        public Color? Color { get; set; }

        /// <summary>
        /// The unique tile ID of the first tile of this tileset.
        /// </summary>
        public int StartTileIndex { get; set; }

        /// <summary>
        /// The actual amount of tiles successfully loaded for this tile set.
        /// </summary>
        public int LoadedTileCount { get; set; }

        /// <summary>
        /// Checks and returns a value that determines whether a tile with a specific
        /// index exists within this tile set.
        /// </summary>
        /// <param name="tileIndex">The index of the tile.</param>
        public bool ContainsTile(int tileIndex) => tileIndex >= StartTileIndex && tileIndex < StartTileIndex + LoadedTileCount;

        private static string[] only1x1TileSets = new string[] { "cliffs", "rivers", "shores", "dirt road" };

        public void Read(IniSection iniSection)
        {
            ReadPropertiesFromIniSection(iniSection);

            foreach (string namepart in only1x1TileSets)
            {
                if (SetName != null && SetName.ToLowerInvariant().Contains(namepart))
                    Only1x1 = true;
            }

            const string colorKeyName = "EditorColor";
            if (iniSection.KeyExists(colorKeyName))
                Color = iniSection.GetColorValue(colorKeyName, UISettings.ActiveSettings.AltColor);
        }
    }
}
