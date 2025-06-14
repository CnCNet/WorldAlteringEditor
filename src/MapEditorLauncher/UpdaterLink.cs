using Rampastring.Updater;
using System;
using System.IO;
using System.Threading;

namespace MapEditorLauncher
{
    /// <summary>
    /// Handles checking for updates and performing them.
    /// </summary>
    public class UpdaterLink
    {
        public UpdaterLink()
        {
            buildHandler = new BuildHandler(Environment.CurrentDirectory, "SecondStageUpdater/SecondStageUpdater.exe");

            string filePath = Path.Combine(Environment.CurrentDirectory, "UpdateMirrors.ini");

            if (File.Exists(filePath))
            {
                buildHandler.ParseUpdateMirrors(filePath);
            }
            else
            {
                buildHandler.AddUpdateMirror("https://rampastring.cnc-comm.com/DTAScenarioEditorUpdates/", "CnCNet (cnc-comm.com)");
            }

            buildHandler.ReadLocalBuildInfo();
            buildHandler.UpdateCheckFailed += BuildHandler_UpdateCheckFailed;
            buildHandler.BuildUpToDate += BuildHandler_BuildUpToDate;
            buildHandler.BuildOutdated += BuildHandler_BuildOutdated;
            UpdaterLogger.EnableLogging(Environment.CurrentDirectory + "/UpdaterLog.txt");
        }

        private BuildHandler buildHandler;

        private WaitHandle waitHandle;

        public void Run()
        {
#if DEBUG
#pragma warning disable 0162
            Logger.Log("!!! Debug build - proceeding with updater disabled.");
            return;
#endif

            if (File.Exists(Environment.CurrentDirectory + "/DisableUpdates"))
            {
                Logger.Log("!!! DisableUpdates file exists - proceeding with updater disabled.");
                return;
            }

            Logger.Log("Checking for updates.");
            waitHandle = new ManualResetEvent(false);
            buildHandler.CheckForUpdates();
            waitHandle.WaitOne();
        }

        private void BuildHandler_BuildOutdated(object sender, BuildOutdatedEventArgs e)
        {
            Logger.Log("The build is outdated. Available version: " + e.VersionDisplayString + " Size: " + e.EstimatedUpdateSize);

            Logger.Log("Starting downloader.");
            buildHandler.DownloadCompleted += BuildHandler_DownloadCompleted;
            buildHandler.UpdateCancelled += BuildHandler_UpdateCancelled;
            buildHandler.UpdateFailed += BuildHandler_UpdateFailed;
            buildHandler.PerformUpdate();
        }

        private void BuildHandler_UpdateFailed(object sender, UpdateFailureEventArgs e)
        {
            Logger.Log("The update failed! Message: " + e.ErrorMessage);
            Logger.Log("Program halted.");
        }

        private void BuildHandler_UpdateCancelled(object sender, EventArgs e)
        {
            Logger.Log("The update was cancelled!");
            SignalWaitHandle();
        }

        private void BuildHandler_DownloadCompleted(object sender, EventArgs e)
        {
            Logger.Log("Download completed. Exiting.");
            Environment.Exit(0);
        }

        private void BuildHandler_BuildUpToDate(object sender, EventArgs e)
        {
            Logger.Log("The build is up to date.");
            SignalWaitHandle();
        }

        private void BuildHandler_UpdateCheckFailed(object sender, EventArgs e)
        {
            Logger.Log("Checking for updates failed!");
            SignalWaitHandle();
        }

        private void SignalWaitHandle()
        {
            ((ManualResetEvent)waitHandle).Set();
        }
    }

    
}
