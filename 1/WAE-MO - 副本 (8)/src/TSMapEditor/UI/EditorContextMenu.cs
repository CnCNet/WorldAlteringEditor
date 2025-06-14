using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace TSMapEditor.UI
{
    class EditorContextMenu : XNAContextMenu
    {
        public EditorContextMenu(WindowManager windowManager) : base(windowManager)
        {
            Width = 250;
            TextHorizontalPadding = Constants.UIEmptySideSpace;
            FontIndex = Constants.UIBoldFont;
            ItemHeight = 25;
        }

        public void AddEmptyLine()
        {
            AddItem(string.Empty, null, () => false, null, null);
        }

        public override void Draw(GameTime gameTime)
        {
            FillRectangle(new Rectangle(0, 0, Width, Height), BackColor);
            DrawRectangle(new Rectangle(0, 0, Width, Height), BorderColor);

            int y = BORDER_WIDTH;

            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Visible)
                    y += DrawItem(i, new Point(BORDER_WIDTH, y));
            }

            DrawChildren(gameTime);
        }
    }
}
