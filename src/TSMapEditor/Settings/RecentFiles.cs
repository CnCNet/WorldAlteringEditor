using Rampastring.Tools;
using System.Collections.Generic;
using System.Globalization;
using TSMapEditor.Misc;

namespace TSMapEditor.Settings
{
    public class RecentFiles
    {
        public const int MaxEntries = 4;
        private const string IniSectionName = "RecentFiles";

        private List<string> entries = new List<string>(MaxEntries);

        public void PutEntry(string entry)
        {
            int existingIndex = entries.IndexOf(entry);
            if (existingIndex == 0)
                return;

            if (existingIndex > -1)
            {
                entries.Swap(0, existingIndex);
            }
            else
            {
                entries.Insert(0, entry);
                TrimEntries();
            }
        }

        public List<string> GetEntries() => new List<string>(entries);

        private void TrimEntries()
        {
            if (entries.Count > MaxEntries)
                entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);
        }

        public void WriteToIniFile(IniFile iniFile)
        {
            iniFile.RemoveSection(IniSectionName);

            if (entries.Count == 0)
                return;

            var section = new IniSection(IniSectionName);
            for (int i = 0; i < entries.Count; i++)
            {
                section.AddKey(i.ToString(CultureInfo.InvariantCulture), entries[i]);
            }
            iniFile.AddSection(section);
        }

        public void ReadFromIniFile(IniFile iniFile)
        {
            var keys = iniFile.GetSectionKeys(IniSectionName);
            if (keys == null)
                return;

            foreach (string key in keys)
            {
                string path = iniFile.GetStringValue(IniSectionName, key, string.Empty);
                if (!string.IsNullOrWhiteSpace(path))
                    entries.Add(path);
            }

            TrimEntries();
        }
    }
}
