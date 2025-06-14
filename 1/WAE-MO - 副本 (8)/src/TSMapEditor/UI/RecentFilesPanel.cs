using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Globalization;
using TSMapEditor.Settings;
using TSMapEditor.UI.Windows;

namespace TSMapEditor.UI
{
    public class RecentFilesPanel : XNAPanel
    {
        public RecentFilesPanel(WindowManager windowManager) : base(windowManager)
        {
            BackgroundTexture = AssetLoader.CreateTexture(UISettings.ActiveSettings.BackgroundColor, 2, 2);
        }

        public EventHandler<FileSelectedEventArgs> FileSelected;

        private List<XNALinkLabel> fileLabels = new List<XNALinkLabel>();

        public override void Initialize()
        {
            Name = nameof(RecentFilesPanel);

            var entries = UserSettings.Instance.RecentFiles.GetEntries();

            int y = Constants.UIEmptyTopSpace;

            for (int i = 0; i < entries.Count; i++)
            {
                string path = entries[i]; // Necessary to define a new local here for the lambda to capture

                var fileLabel = new XNALinkLabel(WindowManager);
                fileLabel.Name = nameof(fileLabel) + i.ToString(CultureInfo.InvariantCulture);
                fileLabel.X = Constants.UIEmptySideSpace;
                fileLabel.Y = y;
                fileLabel.Text = (i + 1).ToString(CultureInfo.InvariantCulture) + ") " + path;
                fileLabel.Tag = entries[i];
                fileLabel.LeftClick += (s, e) => FileSelected?.Invoke(this, new FileSelectedEventArgs(path));
                AddChild(fileLabel);

                y += fileLabel.Height + (Constants.UIVerticalSpacing * 2);

                fileLabels.Add(fileLabel);
            }

            base.Initialize();
        }

        public override void Kill()
        {
            BackgroundTexture?.Dispose();
            base.Kill();
        }
    }
}
