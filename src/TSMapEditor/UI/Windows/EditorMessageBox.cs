using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public enum MessageBoxButtons
    {
        None,
        OK,
        YesNo
    }

    public class EditorMessageBox : EditorWindow
    {
        public EditorMessageBox(WindowManager windowManager, string caption, string description, MessageBoxButtons messageBoxButtons) : base(windowManager)
        {
            this.caption = caption;
            this.description = description;
            this.messageBoxButtons = messageBoxButtons;
        }

        private readonly string caption;
        private readonly string description;
        private readonly MessageBoxButtons messageBoxButtons;

        /// <summary>
        /// The method that is called when the user clicks OK on the message box.
        /// </summary>
        public Action<EditorMessageBox> OKClickedAction { get; set; }

        /// <summary>
        /// The method that is called when the user clicks Yes on the message box.
        /// </summary>
        public Action<EditorMessageBox> YesClickedAction { get; set; }

        /// <summary>
        /// The method that is called when the user clicks No on the message box.
        /// </summary>
        public Action<EditorMessageBox> NoClickedAction { get; set; }

        private List<XNAButton> buttons = new List<XNAButton>();

        private XNALabel lblDescription;

        public override void Initialize()
        {
            Name = "MessageBox";

            XNALabel lblCaption = new XNALabel(WindowManager);
            lblCaption.X = Constants.UIEmptySideSpace;
            lblCaption.Y = Constants.UIEmptyTopSpace;
            lblCaption.FontIndex = Constants.UIBoldFont;
            lblCaption.Text = caption;
            AddChild(lblCaption);

            XNAPanel line = new XNAPanel(WindowManager);
            line.ClientRectangle = new Rectangle(Constants.UIEmptySideSpace,
                lblCaption.Bottom + Constants.UIVerticalSpacing, 0, 1);
            AddChild(line);

            lblDescription = new XNALabel(WindowManager);
            lblDescription.Text = description;
            lblDescription.X = Constants.UIEmptySideSpace;
            lblDescription.Y = line.Bottom + Constants.UIEmptyTopSpace;
            AddChild(lblDescription);

            Vector2 textDimensions = Renderer.GetTextDimensions(lblDescription.Text, lblDescription.FontIndex);
            int captionWidth = (int)Renderer.GetTextDimensions(caption, lblCaption.FontIndex).X;
            Width = Math.Max((int)textDimensions.X, captionWidth) + Constants.UIEmptySideSpace * 2;
            line.Width = Width - (Constants.UIEmptySideSpace * 2);

            switch (messageBoxButtons)
            {
                case MessageBoxButtons.OK:
                    AddOKButton();
                    break;
                case MessageBoxButtons.YesNo:
                    AddYesNoButtons();
                    break;
                case MessageBoxButtons.None:
                    break;
                default:
                    throw new NotImplementedException("Unknown message box button enum value of " + messageBoxButtons);
            }

            if (buttons.Count > 0)
                Height = buttons[0].Bottom + Constants.UIEmptyBottomSpace;
            else
                Height = lblDescription.Bottom + Constants.UIEmptyBottomSpace;

            base.Initialize();

            WindowManager.CenterControlOnScreen(this);

            if (Height > WindowManager.RenderResolutionY)
                Y -= Height - WindowManager.RenderResolutionY;
        }

        private void AddOKButton()
        {
            var btnOK = new EditorButton(WindowManager);
            btnOK.Width = 75;
            btnOK.Name = "btnOK";
            btnOK.Text = Translate(this, "OK", "OK");
            btnOK.HotKey = Microsoft.Xna.Framework.Input.Keys.Enter;
            btnOK.LeftClick += BtnOK_LeftClick;
            buttons.Add(btnOK);

            AddChild(btnOK);

            btnOK.CenterOnParentHorizontally();
            btnOK.Y = lblDescription.Bottom + Constants.UIEmptyTopSpace;
        }

        private void AddYesNoButtons()
        {
            var btnYes = new EditorButton(WindowManager);
            btnYes.Name = nameof(btnYes);
            btnYes.Width = 75;
            btnYes.Text = Translate(this, "Yes", "Yes");
            btnYes.LeftClick += BtnYes_LeftClick;
            buttons.Add(btnYes);
            AddChild(btnYes);

            btnYes.X = (Width - ((btnYes.Width * 2) + Constants.UIHorizontalSpacing)) / 2;
            btnYes.Y = lblDescription.Bottom + Constants.UIEmptyTopSpace;

            var btnNo = new EditorButton(WindowManager);
            btnNo.Name = nameof(btnNo);
            btnNo.Width = btnYes.Width;
            btnNo.Text = Translate(this, "No", "No");
            btnNo.LeftClick += BtnNo_LeftClick;
            buttons.Add(btnNo);
            AddChild(btnNo);

            btnNo.X = btnYes.Right + Constants.UIHorizontalSpacing;
            btnNo.Y = btnYes.Y;
        }

        private void BtnOK_LeftClick(object sender, EventArgs e)
        {
            Close();
            OKClickedAction?.Invoke(this);
        }

        private void BtnYes_LeftClick(object sender, EventArgs e)
        {
            Close();
            YesClickedAction?.Invoke(this);
        }

        private void BtnNo_LeftClick(object sender, EventArgs e)
        {
            Close();
            NoClickedAction?.Invoke(this);
        }

        private void Close()
        {
            if (this.Parent != null)
                WindowManager.RemoveControl(this.Parent);
            else
                WindowManager.RemoveControl(this);

            Destroy();
        }

        private void Destroy()
        {
            foreach (var button in buttons)
            {
                button.Kill();
            }

            Dispose();
        }

        /// <summary>
        /// Creates and displays a new message box with the specified caption, description and buttons.
        /// </summary>
        /// <param name="windowManager">The window manager.</param>
        /// <param name="caption">The caption/header of the message box.</param>
        /// <param name="description">The description of the message box.</param>
        /// <param name="messageBoxButtons">Specifies what buttons the message box should include.</param>
        public static EditorMessageBox Show(WindowManager windowManager, string caption, string description, MessageBoxButtons messageBoxButtons)
        {
            var msgBox = new EditorMessageBox(windowManager,
                Renderer.GetSafeString(caption, 1),
                Renderer.FixText(Renderer.GetSafeString(description, 0), 0, windowManager.RenderResolutionX).Text,
                messageBoxButtons);

            DarkeningPanel.AddAndInitializeWithControl(windowManager, msgBox, true);
            return msgBox;
        }
    }
}
