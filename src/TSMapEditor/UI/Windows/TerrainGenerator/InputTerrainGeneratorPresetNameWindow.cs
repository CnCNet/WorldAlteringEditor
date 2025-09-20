using Rampastring.XNAUI;
using System;
using TSMapEditor.Models;
using TSMapEditor.Settings;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows.TerrainGenerator
{
    public class InputTerrainGeneratorPresetNameWindow : INItializableWindow
    {
        public InputTerrainGeneratorPresetNameWindow(WindowManager windowManager, TerrainGeneratorUserPresets userPresets, Map map) : base(windowManager)
        {
            this.userPresets = userPresets;
            this.map = map;
        }

        private readonly TerrainGeneratorUserPresets userPresets;
        private readonly Map map;

        public event EventHandler<string> SaveAccepted;

        private EditorSuggestionTextBox tbPresetName;

        public override void Initialize()
        {
            Name = nameof(InputTerrainGeneratorPresetNameWindow);
            base.Initialize();

            tbPresetName = FindChild<EditorSuggestionTextBox>("tbPresetName");
            FindChild<EditorButton>("btnOK").LeftClick += BtnOK_LeftClick;
            FindChild<EditorButton>("btnCancel").LeftClick += (s, e) => Hide();
        }

        private void BtnOK_LeftClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbPresetName.Text))
                return;

            if (userPresets.GetConfigurationsForCurrentTheater().Exists(c => c.Theater.Equals(map.LoadedTheaterName, StringComparison.OrdinalIgnoreCase) && c.Name == tbPresetName.Text))
            {
                EditorMessageBox.Show(WindowManager, 
                    Translate(this, "PresetAlreadyExists.Title", "Preset already exists"),
                    string.Format(Translate(this, "PresetAlreadyExists.Description", 
                        "A preset with the name {0} already exists for the current theater!"), tbPresetName.Text),
                    MessageBoxButtons.OK);
                return;
            }

            SaveAccepted?.Invoke(this, tbPresetName.Text);
            Hide();
        }

        public void Open()
        {
            tbPresetName.Text = tbPresetName.Suggestion;
            Show();
        }
    }
}
