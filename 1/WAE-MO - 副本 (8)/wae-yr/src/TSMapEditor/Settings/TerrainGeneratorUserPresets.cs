using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;

namespace TSMapEditor.Settings
{
    public class TerrainGeneratorUserPresets
    {
        public TerrainGeneratorUserPresets(Map map)
        {
            this.map = map;
        }

        private readonly Map map;

        private const string ConfigFileName = "TerrainGeneratorUserPresets.ini";

        private List<TerrainGeneratorConfiguration> configurations = new List<TerrainGeneratorConfiguration>();

        public List<TerrainGeneratorConfiguration> GetConfigurationsForCurrentTheater() 
            => configurations.FindAll(c => c.Theater.Equals(map.LoadedTheaterName, StringComparison.OrdinalIgnoreCase));

        private bool isDirty;

        public void Load()
        {
            string path = GetConfigFilePath();
            if (!File.Exists(path))
                return;

            var iniFile = new IniFile(path);
            int i = 0;
            while (true)
            {
                string sectionName = "Preset" + i.ToString(CultureInfo.InvariantCulture);
                var iniSection = iniFile.GetSection(sectionName);
                if (iniSection == null)
                    break;

                var config = TerrainGeneratorConfiguration.FromConfigSection(iniSection, map.Rules, map.TheaterInstance.Theater, true);

                if (config != null)
                    configurations.Add(config);
                else
                    Logger.Log($"Failed to load terrain generator config from user preset #{i}!");

                i++;
            }
        }

        public bool SaveIfDirty()
        {
            if (!isDirty)
                return true;

            return ForceSave();
        }

        public bool ForceSave()
        {
            string path = GetConfigFilePath();

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            catch (IOException ex)
            {
                Logger.Log("IOException while trying to create directories for terrain generator user presets file! Returned error: " + ex.Message);
                return false;
            }

            IniFile iniFile = new IniFile();

            int i = 0;
            configurations.ForEach(c =>
            {
                iniFile.AddSection(c.GetIniConfigSection("Preset" + i.ToString(CultureInfo.InvariantCulture)));
                i++;
            });
            
            try
            {
                iniFile.WriteIniFile(path);
            }
            catch (IOException ex)
            {
                Logger.Log("IOException while saving terrain generator presets! Returned error: " + ex.Message);
                return false;
            }

            isDirty = false;
            return true;
        }

        public void DeleteConfig(string name)
        {
            int index = configurations.FindIndex(c => c.Name == name && c.Theater.Equals(map.LoadedTheaterName, StringComparison.OrdinalIgnoreCase));
            if (index > -1)
            {
                configurations.RemoveAt(index);
                isDirty = true;
            }
        }

        public void AddConfig(TerrainGeneratorConfiguration configuration)
        {
            if (configurations.Exists(c => c.Name == configuration.Name && c.Theater.Equals(map.LoadedTheaterName, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"A configuration with the name {configuration.Name} already exists!");

            configurations.Add(configuration);
            isDirty = true;
        }

        private string GetConfigFilePath() => Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), Constants.UserDataFolder, ConfigFileName);
    }
}
