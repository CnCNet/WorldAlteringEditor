using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TSMapEditor.UI
{
    /// <summary>
    /// A XNAPanel derivative that sets its background.
    /// </summary>
    public class EditorPanel : XNAPanel
    {
        public EditorPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        private bool isStockBackgroundTexture;

        public override void Initialize()
        {
            base.Initialize();

            if (BackgroundTexture == null)
            {
                BackgroundTexture = AssetLoader.CreateTexture(UISettings.ActiveSettings.PanelBackgroundColor, 2, 2);
                PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
                isStockBackgroundTexture = true;
            }

            AutoAssignTextBoxNextControls();
        }

        private void AutoAssignTextBoxNextControls()
        {
            var textBoxes = Children.OfType<XNATextBox>().ToList();

            for (int i = 0; i < textBoxes.Count; i++)
            {
                var box = textBoxes[i];

                if (box.NextControl != null)
                    continue;

                var next = FindStartingFromIndex(textBoxes, i + 1, tb => tb.PreviousControl == null && tb.Y == box.Y);

                if (next == null)
                    next = FindStartingFromIndex(textBoxes, i + 1, tb => tb.Y > box.Y);

                if (next != null)
                {
                    box.NextControl = next;
                    next.PreviousControl = box;
                }
            }
        }

        private T FindStartingFromIndex<T>(List<T> list, int startIndex, Func<T, bool> predicate)
        {
            for (int i = startIndex; i < list.Count; i++)
            {
                if (predicate(list[i]))
                    return list[i];
            }

            return default;
        }

        public override void Kill()
        {
            if (isStockBackgroundTexture)
                BackgroundTexture?.Dispose();

            base.Kill();
        }
    }
}
