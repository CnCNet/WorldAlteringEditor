using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.IO;
using TSMapEditor.Scripts;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class RunScriptWindow : INItializableWindow
    {
        public RunScriptWindow(WindowManager windowManager, ScriptDependencies scriptDependencies) : base(windowManager)
        {
            this.scriptDependencies = scriptDependencies;
        }

        public event EventHandler ScriptRun;

        private readonly ScriptDependencies scriptDependencies;

        private EditorListBox lbScriptFiles;

        private string scriptPath;

        public override void Initialize()
        {
            Name = nameof(RunScriptWindow);
            base.Initialize();

            lbScriptFiles = FindChild<EditorListBox>(nameof(lbScriptFiles));
            FindChild<EditorButton>("btnRunScript").LeftClick += BtnRunScript_LeftClick;
        }

        private void BtnRunScript_LeftClick(object sender, EventArgs e)
        {
            // Run script on next game loop frame so that in case the script displays
            // UI, the UI will be shown on top of our window despite that the user
            // clicked on our window this frame
            AddCallback(RunScript_Callback);
        }

        private void RunScript_Callback()
        {
            if (lbScriptFiles.SelectedItem == null)
                return;

            string filePath = (string)lbScriptFiles.SelectedItem.Tag;
            if (!File.Exists(filePath))
            {
                EditorMessageBox.Show(WindowManager, 
                    Translate(this, "FileNotFound.Title", "Can't find file"),
                    Translate(this, "FileNotFound.Description", "The selected file does not exist! Maybe it was deleted?"),
                    MessageBoxButtons.OK);
                return;
            }

            scriptPath = filePath;

            string error = ScriptRunner.CompileScript(scriptDependencies, filePath);

            if (error != null)
            {
                Logger.Log("Compilation error when attempting to run script: " + error);
                EditorMessageBox.Show(WindowManager, 
                    Translate(this, "ScriptCompilationError.Title", "Error"),
                    string.Format(Translate(this, "ScriptCompilationError.Description", 
                        "Compiling the script failed! Check its syntax, or contact its author for support." + Environment.NewLine + Environment.NewLine +
                        "Returned error was: {0}"), error),
                   MessageBoxButtons.OK);
                return;
            }

            if (ScriptRunner.ActiveScriptAPIVersion == 1)
            {
                string confirmation = ScriptRunner.GetDescriptionFromScriptV1();

                confirmation = Renderer.FixText(confirmation, Constants.UIDefaultFont, Width).Text;

                var messageBox = EditorMessageBox.Show(WindowManager, 
                    Translate(this, "Confirm", "Are you sure?"),
                    confirmation, MessageBoxButtons.YesNo);
                messageBox.YesClickedAction = (_) => ApplyCode();

            }
            else if (ScriptRunner.ActiveScriptAPIVersion == 2)
            {
                error = ScriptRunner.RunScriptV2();

                if (error != null)
                    EditorMessageBox.Show(WindowManager, 
                        Translate(this, "ScriptRunError.Title", "Error running script"),
                        error,
                        MessageBoxButtons.OK);
            }
            else
            {
                EditorMessageBox.Show(WindowManager, 
                    Translate(this, "UnsupportedScriptApiVersion.Title", "Unsupported Scripting API Version"),
                    string.Format(Translate(this, "UnsupportedScriptApiVersion.Description", 
                        "Script uses an unsupported scripting API version: {0}"), ScriptRunner.ActiveScriptAPIVersion),
                    MessageBoxButtons.OK);
            }
        }

        private void ApplyCode()
        {
            if (scriptPath == null)
                throw new InvalidOperationException("Pending script path is null!");

            string result = ScriptRunner.RunScriptV1(scriptDependencies.Map, scriptPath);
            result = Renderer.FixText(result, Constants.UIDefaultFont, Width).Text;

            EditorMessageBox.Show(WindowManager, Translate(this, "Result", "Result"), result, MessageBoxButtons.OK);
            ScriptRun?.Invoke(this, EventArgs.Empty);
        }

        public void Open()
        {
            lbScriptFiles.Clear();

            string directoryPath = Path.Combine(Environment.CurrentDirectory, "Config", "Scripts");

            if (!Directory.Exists(directoryPath))
            {
                Logger.Log("WAE scipts directory not found!");
                EditorMessageBox.Show(WindowManager, 
                    Translate(this, "WAEScriptsDirectoryNotFound.Title", "Error"),
                    string.Format(Translate(this, "WAEScriptsDirectoryNotFound.Description", 
                        "Scripts directory not found!\r\n\r\nExpected path: {0}"), directoryPath),
                    MessageBoxButtons.OK);
                return;
            }

            var iniFiles = Directory.GetFiles(directoryPath, "*.cs");

            foreach (string filePath in iniFiles)
            {
                lbScriptFiles.AddItem(new XNAListBoxItem(Path.GetFileName(filePath)) { Tag = filePath });
            }

            Show();
        }
    }
}
