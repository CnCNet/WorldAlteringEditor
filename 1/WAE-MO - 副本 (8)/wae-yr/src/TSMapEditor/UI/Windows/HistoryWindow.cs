using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.Mutations;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class HistoryWindow : INItializableWindow
    {
        public HistoryWindow(WindowManager windowManager, MutationManager mutationManager) : base(windowManager)
        {
            this.mutationManager = mutationManager;
        }

        private readonly MutationManager mutationManager;

        private XNAListBox lbRedoHistory;
        private XNAListBox lbUndoHistory;

        private int lastUndoCount;
        private int lastRedoCount;

        public override void Initialize()
        {
            Name = nameof(HistoryWindow);
            base.Initialize();

            lbRedoHistory = FindChild<XNAListBox>(nameof(lbRedoHistory));
            lbUndoHistory = FindChild<XNAListBox>(nameof(lbUndoHistory));
            FindChild<EditorButton>("btnRedoUpToSelected").LeftClick += BtnRedoUpToSelected_LeftClick;
            FindChild<EditorButton>("btnUndoUpToSelected").LeftClick += BtnUndoUpToSelected_LeftClick;
        }

        private void BtnRedoUpToSelected_LeftClick(object sender, EventArgs e)
        {
            if (lbRedoHistory.SelectedItem == null)
                return;

            int count = lbRedoHistory.SelectedIndex + 1;
            for (int i = 0; i < count; i++)
                mutationManager.Redo();

            lbRedoHistory.SelectedIndex = -1;
            RefreshHistory();
        }

        private void BtnUndoUpToSelected_LeftClick(object sender, EventArgs e)
        {
            if (lbUndoHistory.SelectedItem == null)
                return;

            int count = lbUndoHistory.SelectedIndex + 1;
            for (int i = 0; i < count; i++)
                mutationManager.UndoOne();

            lbUndoHistory.SelectedIndex = -1;
            RefreshHistory();
        }

        public void Open()
        {
            RefreshHistory();
            Show();
        }

        private void RefreshHistory()
        {
            const int MaxItems = 1000;

            lastRedoCount = mutationManager.RedoList.Count;
            lastUndoCount = mutationManager.UndoList.Count;

            lbUndoHistory.Clear();
            lbUndoHistory.ViewTop = 0;

            // The mutation history is a simple list where the newer the item, the lower its index.
            // For the UI, it makes sense to show the list in reversed order.
            for (int i = mutationManager.UndoList.Count - 1; i >= 0 && lbUndoHistory.Items.Count < MaxItems; i--)
            {
                lbUndoHistory.AddItem(i.ToString() + " - " + mutationManager.UndoList[i].GetDisplayString());
            }

            lbRedoHistory.Clear();
            lbRedoHistory.ViewTop = 0;

            // Same goes for the redo list, but to the user it makes more sense if the flip the indexes.
            for (int i = mutationManager.RedoList.Count - 1; i >= 0 && lbRedoHistory.Items.Count < MaxItems; i--)
            {
                lbRedoHistory.AddItem((mutationManager.RedoList.Count - 1 - i).ToString() + " - " + mutationManager.RedoList[i].GetDisplayString());
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (mutationManager.UndoList.Count != lastUndoCount || mutationManager.RedoList.Count != lastRedoCount)
                RefreshHistory();
        }
    }
}
