using System;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace TSMapEditor.UI.Controls
{
    public class EditorListBoxSearchTextBox : EditorSuggestionTextBox
    {
        public EditorListBoxSearchTextBox(WindowManager windowManager) : base(windowManager)
        {
            TextChanged += EditorListBoxSearchTextBox_TextChanged;
        }

        public XNAListBox ListBox { get; set; }

        private void EditorListBoxSearchTextBox_TextChanged(object sender, EventArgs e)
        {
            if (ListBox == null)
                return;

            if (string.IsNullOrWhiteSpace(Text) || Text == Suggestion)
            {
                foreach (var item in ListBox.Items)
                {
                    if (!item.Visible)
                        ListBox.ViewTop = 0;

                    item.Visible = true;
                }
            }
            else
            {
                ListBox.ViewTop = 0;
                ListBox.SelectedIndex = -1;

                for (int i = 0; i < ListBox.Items.Count; i++)
                {
                    var item = ListBox.Items[i];
                    item.Visible = item.Text.Contains(Text, StringComparison.OrdinalIgnoreCase);

                    if (item.Visible && ListBox.SelectedIndex == -1)
                        ListBox.SelectedIndex = i;
                }
            }

            ListBox.RefreshScrollbar();
        }
    }
}
