using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.IO;
using TSMapEditor.Extensions;
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
                EditorMessageBox.Show(WindowManager, "Can't find file".L10N(),
                    "The selected file does not exist! Maybe it was deleted?".L10N(), MessageBoxButtons.OK);

                return;
            }

            scriptPath = filePath;

            string error = ScriptRunner.CompileScript(scriptDependencies, filePath);

            if (error != null)
            {
                Logger.Log("Compilation error when attempting to run script: " + error);
                EditorMessageBox.Show(WindowManager, "Error".L10N(),
                    "Compiling the script failed! Check its syntax, or contact its author for support.".L10N() + Environment.NewLine + Environment.NewLine +
                    "Returned error was: ".L10N() + error, MessageBoxButtons.OK);
                return;
            }

            if (ScriptRunner.ActiveScriptAPIVersion == 1)
            {
                string confirmation = ScriptRunner.GetDescriptionFromScriptV1();

                confirmation = Renderer.FixText(confirmation, Constants.UIDefaultFont, Width).Text;

                var messageBox = EditorMessageBox.Show(WindowManager, "Are you sure?".L10N(),
                    confirmation, MessageBoxButtons.YesNo);
                messageBox.YesClickedAction = (_) => ApplyCode();

            }
            else if (ScriptRunner.ActiveScriptAPIVersion == 2)
            {
                error = ScriptRunner.RunScriptV2();

                if (error != null)
                    EditorMessageBox.Show(WindowManager, "Error running script".L10N(), error, MessageBoxButtons.OK);
            }
            else
            {
                EditorMessageBox.Show(WindowManager, "Unsupported Scripting API Version".L10N(),
                    "Script uses an unsupported scripting API version: ".L10N() + ScriptRunner.ActiveScriptAPIVersion, MessageBoxButtons.OK);
            }
        }

        private void ApplyCode()
        {
            if (scriptPath == null)
                throw new InvalidOperationException("Pending script path is null!");

            string result = ScriptRunner.RunScriptV1(scriptDependencies.Map, scriptPath);
            result = Renderer.FixText(result, Constants.UIDefaultFont, Width).Text;

            EditorMessageBox.Show(WindowManager, "Result".L10N(), result, MessageBoxButtons.OK);
            ScriptRun?.Invoke(this, EventArgs.Empty);
        }

        public void Open()
        {
            lbScriptFiles.Clear();

            string directoryPath = Path.Combine(Environment.CurrentDirectory, "Config", "Scripts");

            if (!Directory.Exists(directoryPath))
            {
                Logger.Log("WAE scipts directory not found!");
                EditorMessageBox.Show(WindowManager, "Error".L10N(), "Scripts directory not found!\r\n\r\nExpected path: ".L10N() + directoryPath, MessageBoxButtons.OK);
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
