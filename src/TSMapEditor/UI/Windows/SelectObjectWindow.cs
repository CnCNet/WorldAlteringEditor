using System;
using Rampastring.XNAUI;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public abstract class SelectObjectWindow<T> : INItializableWindow
    {
        public SelectObjectWindow(WindowManager windowManager) : base(windowManager)
        {
            HasCloseButton = true;
        }

        public event EventHandler ObjectSelected;

        protected EditorListBoxSearchTextBox tbSearch;
        protected EditorListBox lbObjectList;

        /// <summary>
        /// If the object is being selected for a trigger event or action,
        /// this can be used to determine whether the object is selected
        /// for an event or an action.
        /// </summary>
        public bool IsForEvent { get; set; }

        public T SelectedObject { get; protected set; }

        private T initialSelection;

        private SelectObjectWindowInfoPanel infoPanel;

        public override void Initialize()
        {
            base.Initialize();

            DrawOrder = WindowController.ChildWindowOrderValue * 2;
            UpdateOrder = DrawOrder;

            if (Parent != null)
            {
                Parent.DrawOrder = DrawOrder;
                Parent.UpdateOrder = UpdateOrder;
            }

            tbSearch = FindChild<EditorListBoxSearchTextBox>(nameof(tbSearch));
            UIHelpers.AddSearchTipsBoxToControl(tbSearch);

            lbObjectList = FindChild<EditorListBox>(nameof(lbObjectList));
            tbSearch.ListBox = lbObjectList;

            lbObjectList.AllowRightClickUnselect = false;
            lbObjectList.DoubleLeftClick += (s, e) => ConfirmSelection();
            lbObjectList.SelectedIndexChanged += LbObjectList_SelectedIndexChanged;
            lbObjectList.HoveredIndexChanged += LbObjectList_HoveredIndexChanged;

            FindChild<EditorButton>("btnSelect").LeftClick += (s, e) => ConfirmSelection();

            tbSearch.EnterPressed += (s, e) => { ConfirmSelection(); };

            // Make pressing X not save changes
            if (btnClose != null)
                btnClose.LeftClick += (s, e) => SelectedObject = initialSelection;

            infoPanel = new SelectObjectWindowInfoPanel(WindowManager);
            AddChild(infoPanel);

            EnabledChanged += SelectObjectWindow_EnabledChanged;
        }

        private void ConfirmSelection()
        {
            if (lbObjectList.SelectedItem != null)
            {
                ObjectSelected?.Invoke(this, EventArgs.Empty);
                Hide();
            }
        }

        public override void Kill()
        {
            Keyboard.OnKeyDown -= Keyboard_OnKeyDown;
            base.Kill();
        }

        private void SelectObjectWindow_EnabledChanged(object sender, EventArgs e)
        {
            if (!Enabled)
            {
                HideInfoPanel();
                Keyboard.OnKeyDown -= Keyboard_OnKeyDown;
            }
            else
            {
                Keyboard.OnKeyDown += Keyboard_OnKeyDown;
            }
        }

        private void Keyboard_OnKeyDown(object sender, Rampastring.XNAUI.Input.KeyPressEventArgs e)
        {
            if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.Enter)
            {
                ConfirmSelection();
                e.Handled = true;
            }
        }

        private void HideInfoPanel()
        {
            if (infoPanel.Detached)
            {
                infoPanel.Attach();
                infoPanel.Disable();
            }
        }

        public void Open(T initialSelection)
        {
            HideInfoPanel();
            SelectedObject = initialSelection;
            this.initialSelection = SelectedObject;
            OnOpen();
            ListObjects();

            if (lbObjectList.SelectedItem == null)
            {
                // If the initially selected object wasn't found for some reason, then clear selection
                SelectedObject = default;
            }

            if (SelectedObject == null)
            {
                lbObjectList.SelectedIndex = -1;
            }

            if (lbObjectList.SelectedIndex > lbObjectList.LastIndex)
            {
                lbObjectList.LastIndex = lbObjectList.SelectedIndex;
            }

            Show();

            tbSearch.Text = string.Empty;
            WindowManager.SelectedControl = tbSearch;
        }

        /// <summary>
        /// Can be overridden in derived classes to perform operations when the window is opened.
        /// </summary>
        protected virtual void OnOpen() { }

        protected abstract void LbObjectList_SelectedIndexChanged(object sender, EventArgs e);

        private void LbObjectList_HoveredIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.HoveredItem == null)
            {
                 infoPanel.Hide();
                 return;
            }

            if (lbObjectList.HoveredItem.Tag == null)
                return;

            var item = (T)lbObjectList.HoveredItem.Tag;

            if (item is not IHintable)
                return;

            var itemAsHintable = item as IHintable;

            infoPanel.Open(itemAsHintable.GetHeaderText(),
                itemAsHintable.GetHintText(),
                new Point2D(Width, GetCursorPoint().Y));
        }

        protected abstract void ListObjects();
    }
}
