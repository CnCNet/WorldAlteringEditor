using System;
using System.IO;
using TSMapEditor.Models;

namespace TSMapEditor.Misc
{
    /// <summary>
    /// Watches the map file for changes made outside of the editor.
    /// </summary>
    public class MapFileWatcher
    {
        public MapFileWatcher(Map map) 
        {
            this.map = map;

            if (!string.IsNullOrWhiteSpace(map.LoadedINI.FileName))
                StartWatching(map.LoadedINI.FileName);

            map.MapManuallySaved += Map_MapManuallySaved;
            map.PreSave += Map_PreSave;
            map.PostSave += Map_PostSave;
        }

        /// <summary>
        /// Disables the watcher when the map about to be saved.
        /// This prevents the editor from picking up its own file system event.
        /// </summary>
        private void Map_PreSave(object sender, EventArgs e)
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
            }
        }

        /// <summary>
        /// Enables the watcher after the map has been saved.
        /// </summary>
        private void Map_PostSave(object sender, EventArgs e)
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = true;
            }
        }

        private void Map_MapManuallySaved(object sender, EventArgs e)
        {
            if (map.LoadedINI.FileName != FilePath)
                StartWatching(map.LoadedINI.FileName);
        }

        private readonly Map map;

        public string FilePath { get; private set; }

        private FileSystemWatcher watcher;

        private volatile bool modifyEventDetected;

        public bool ModifyEventDetected => modifyEventDetected;

        public void StartWatching(string filePath)
        {
            DisposeWatcher();

            FilePath = filePath;
            watcher = new FileSystemWatcher(Path.GetDirectoryName(filePath), Path.GetFileName(filePath));
            watcher.EnableRaisingEvents = true;
            watcher.Created += Watcher_Created;
            watcher.Changed += Watcher_Changed;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            modifyEventDetected = true;
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            modifyEventDetected = true;
        }

        public void StopWatching()
        {
            if (watcher == null)
                return;

            DisposeWatcher();
        }

        public void DisposeWatcher()
        {
            if (watcher == null)
                return;

            watcher.EnableRaisingEvents = false;
            watcher.Created -= Watcher_Created;
            watcher.Changed -= Watcher_Changed;
            watcher.Dispose();
            watcher = null;
        }

        /// <summary>
        /// Attempts to handle a "map modified outside of the editor" event.
        /// Returns true if successful, false if unsuccessful or there was no event to handle.
        /// </summary>
        public bool HandleModifyEvent()
        {
            if (!modifyEventDetected)
                return false;

            if (map.ReloadINI())
            {
                modifyEventDetected = false;
                return true;
            }

            return false;
        }
    }
}
