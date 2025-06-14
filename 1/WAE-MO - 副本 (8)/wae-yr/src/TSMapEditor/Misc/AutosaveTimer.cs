using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TSMapEditor.Models;
using TSMapEditor.Settings;

namespace TSMapEditor.Misc
{
    public class AutosaveTimer
    {
        public AutosaveTimer(Map map) 
        {
            this.map = map;
            AutoSaveTime = TimeSpan.FromSeconds(UserSettings.Instance.AutoSaveInterval);
        }

        private readonly Map map;

        private const string AutoSavesDirectory = "AutoSaves";
        private const string MapFileExtension = ".map";

        public TimeSpan AutoSaveTime { get; set; }

        private void DoSave()
        {
            var now = DateTime.Now;
            string timestamp = $"{now.Year}_{now.Month:D2}_{now.Day:D2}_{now.Hour:D2}_{now.Minute:D2}_{now.Second:D2}";
            map.AutoSave(Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), AutoSavesDirectory, $"autosave_{timestamp}{MapFileExtension}"));
        }

        public string Update(TimeSpan elapsedTime)
        {
            AutoSaveTime -= elapsedTime;

            if (AutoSaveTime.TotalMilliseconds <= 0)
            {
                AutoSaveTime = TimeSpan.FromSeconds(UserSettings.Instance.AutoSaveInterval);

                try
                {
                    DoSave();
                }
                catch (Exception ex)
                {
                    if (ex is UnauthorizedAccessException || ex is IOException)
                    {
                        Logger.Log("Failed to auto-save map. Returned error message: " + ex.Message);
                        return ex.Message;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return null;
        }


        /// <summary>
        /// Purges old auto-saves from the autosaves directory.
        /// </summary>
        public static void Purge()
        {
            Logger.Log("Purging old auto-saves.");

            string path = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), AutoSavesDirectory);

            if (!Directory.Exists(path))
            {
                Logger.Log("Auto-saves directory does not exist!");
                return;
            }

            string[] filePaths = Directory.GetFiles(path);
            List<FileInfo> mapFileInfos = new List<FileInfo>();
            foreach (string filePath in filePaths)
            {
                if (!filePath.EndsWith(MapFileExtension))
                    continue;

                FileInfo fileInfo = new FileInfo(filePath);

                if (fileInfo.CreationTime < DateTime.Now.AddDays(-1))
                    mapFileInfos.Add(fileInfo);
            }

            // Leave the latest 5 files. Purge everything else.
            mapFileInfos = mapFileInfos.OrderBy(fileInfo => fileInfo.CreationTime).Reverse().ToList();
            const int leaveCount = 5;
            int purgeCount = mapFileInfos.Count - leaveCount;

            if (purgeCount <= 0)
            {
                Logger.Log("There are not enough old auto-saves to purge.");
                return;
            }

            Logger.Log($"Found {purgeCount} autosaves to purge.");

            for (int i = leaveCount; i < mapFileInfos.Count; i++)
            {
                var autosavePath = mapFileInfos[i].FullName;

                try
                {
                    File.Delete(autosavePath);
                }
                catch (IOException ex)
                {
                    Logger.Log($"Failed to delete auto-save {Path.GetFileName(autosavePath)}. Exception message: " + ex.Message);
                }
            }
        }
    }
}
