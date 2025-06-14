using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace TSMapEditor.UI.Controls
{
    public class EditorSuggestionTextBox : XNASuggestionTextBox
    {
        public EditorSuggestionTextBox(WindowManager windowManager) : base(windowManager)
        {
            Height = Constants.UITextBoxHeight;
        }
    }
}
