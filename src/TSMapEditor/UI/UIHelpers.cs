using Rampastring.XNAUI.Input;
using Rampastring.XNAUI.XNAControls;
using System.Collections.Generic;
using TSMapEditor.Misc;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI
{
    public static class UIHelpers
    {
        public static void AddSearchTipsBoxToControl(XNAControl control)
        {
            var lblSearchTips = new XNALabel(control.WindowManager);
            lblSearchTips.Name = nameof(lblSearchTips);
            lblSearchTips.Text = "?";
            lblSearchTips.X = control.Width - Constants.UIEmptySideSpace - lblSearchTips.Width;
            lblSearchTips.Y = (control.Height - lblSearchTips.Height) / 2;
            control.AddChild(lblSearchTips);
            var tooltip = new ToolTip(control.WindowManager, lblSearchTips);
            tooltip.Text = Translate("UIHelpers.AddSearchTipsBoxToControl.SearchTip", "Search Tips\r\n\r\nWith the text box activated:\r\n- Press ENTER to move to next match in list\r\n- Press ESC to clear search query");
        }

        public static T GetScrollItem<T>(List<T> list, T current, Cursor cursor, bool allowSelectionIfNull)
        {
            if (current == null)
            {
                if (list.Count > 0 && allowSelectionIfNull)
                {
                    return current;
                }

                return default(T);
            }

            int index = list.IndexOf(current);

            // Check for possible error condition
            if (index < 0)
            {
                return default(T);
            }

            if (index > 0 && cursor.ScrollWheelValue > 0)
            {
                return list[index - 1];
            }

            if (index < list.Count - 1 && cursor.ScrollWheelValue < 0)
            {
                return list[index + 1];
            }

            return current;
        }

        public static void AddColorOptionsToDropDown(NamedColor[] colors, XNADropDown dropDown)
        {
            foreach (var supportedColor in colors)
            {
                dropDown.AddItem(new XNADropDownItem()
                {
                    Text = Translate("NamedColors." + supportedColor.Name, supportedColor.Name),
                    TextColor = supportedColor.Value,
                    Tag = supportedColor.Name
                });
            }
        }
    }
}
