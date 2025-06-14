using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using System;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.Settings;

namespace TSMapEditor.UI.Windows.TerrainGenerator
{
    internal class DeleteTerrainGeneratorPresetWindow : SelectObjectWindow<TerrainGeneratorConfiguration>
    {
        public DeleteTerrainGeneratorPresetWindow(WindowManager windowManager, TerrainGeneratorUserPresets userPresets) : base(windowManager)
        {
            this.userPresets = userPresets;
        }

        private readonly TerrainGeneratorUserPresets userPresets;

        public override void Initialize()
        {
            Name = nameof(DeleteTerrainGeneratorPresetWindow);
            base.Initialize();
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (TerrainGeneratorConfiguration)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            var configs = userPresets.GetConfigurationsForCurrentTheater();

            for (int i = 0; i < configs.Count; i++)
            {
                TerrainGeneratorConfiguration config = configs[i];

                lbObjectList.AddItem(new XNAListBoxItem() { Text = $"{config.Name}", Tag = config });
            }
        }
    }
}
