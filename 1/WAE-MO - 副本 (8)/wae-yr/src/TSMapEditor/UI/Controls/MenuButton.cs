using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;

namespace TSMapEditor.UI.Controls
{
    /// <summary>
    /// A button that opens a menu.
    /// </summary>
    public class MenuButton : XNAButton
    {
        public MenuButton(WindowManager windowManager) : base(windowManager)
        {
            Height = Constants.UITopBarMenuHeight;
            FontIndex = Constants.UIBoldFont;
        }

        public MenuButton(WindowManager windowManager, XNAContextMenu contextMenu) : this(windowManager)
        {
            ContextMenu = contextMenu;
        }

        private XNAContextMenu _contextMenu;
        public XNAContextMenu ContextMenu 
        {
            get => _contextMenu;
            set
            {
                if (_contextMenu != null)
                    throw new InvalidOperationException("The context menu of a menu button cannot be changed once assigned!");

                _contextMenu = value;
                _contextMenu.EnabledChanged += ContextMenu_EnabledChanged;
                AddChild(_contextMenu);
            }
        }

        /*
         * Warning: this class pretty much hacks around the typical behaviour 
         * of context menus to allow them to convincingly work as menus.
         * Firstly, the menu should open when the primary mouse button is
         * pressed down on this button. 
         * Secondly, clicking on this button (iow. releasing the mouse button)
         * should only close the context menu if it wasn't opened when the button
         * was first pressed down.
         */

        private bool contextMenuDisabledOnCurrentFrame = false;
        private bool contextMenuOpenedOnClick = false;

        public override void Initialize()
        {
            base.Initialize();

            Width = (int)Renderer.GetTextDimensions(Text, FontIndex).X + Constants.UIEmptySideSpace * 2;
        }

        private void ContextMenu_EnabledChanged(object sender, System.EventArgs e)
        {
            if (!ContextMenu.Enabled)
            {
                RemapColor = UISettings.ActiveSettings.AltColor;
                TextColorIdle = UISettings.ActiveSettings.ButtonTextColor;
                contextMenuDisabledOnCurrentFrame = true;
            }
        }

        public override void OnMouseLeftDown()
        {
            base.OnMouseLeftDown();

            if (!ContextMenu.Enabled)
            {
                OpenContextMenu();
                contextMenuOpenedOnClick = true;
            }            
        }

        public override void OnLeftClick()
        {
            base.OnLeftClick();

            if ((!ContextMenu.Enabled && !contextMenuDisabledOnCurrentFrame) || contextMenuOpenedOnClick)
                OpenContextMenu();
            else if (!contextMenuOpenedOnClick)
                ContextMenu.Disable();

            contextMenuOpenedOnClick = false;
        }

        public override void OnMouseLeave()
        {
            base.OnMouseLeave();

            if (contextMenuOpenedOnClick)
                contextMenuOpenedOnClick = false;
        }

        public void OpenContextMenu()
        {
            if (ContextMenu.Items.Count == 0)
                return;

            if (!ContextMenu.Enabled)
            {
                ContextMenu.Open(new Point(0, Height));
                RemapColor = UISettings.ActiveSettings.FocusColor;
                TextColorIdle = UISettings.ActiveSettings.ButtonHoverColor;

                ContextMenu.InputEnabled = false;
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (ContextMenu.Enabled && !ContextMenu.InputEnabled && !contextMenuOpenedOnClick)
                ContextMenu.InputEnabled = true;

            base.Update(gameTime);
            contextMenuDisabledOnCurrentFrame = false;
        }
    }
}
