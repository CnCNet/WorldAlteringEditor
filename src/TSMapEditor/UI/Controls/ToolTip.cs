using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;

namespace TSMapEditor.UI.Controls
{
    /// <summary>
    /// A tool tip.
    /// </summary>
    public class ToolTip : XNAControl
    {
        private const int ToolTipMargin = 5;
        private const int DisplayOffsetX = 10;
        private const int DisplayOffsetY = 10;
        private const float AlphaRate = 5.0f;

        /// <summary>
        /// If set to true - makes tooltip not appear and instantly hides it if currently shown.
        /// </summary>
        public bool Blocked { get; set; }

        public int ToolTipDelay { get; set; } = 500;

        /// <summary>
        /// 中文提示文本
        /// </summary>
        public string CNText { get; set; }

        /// <summary>
        /// Creates a new tool tip and attaches it to the given control.
        /// </summary>
        /// <param name="windowManager">The window manager.</param>
        /// <param name="masterControl">The control to attach the tool tip to.</param>
        public ToolTip(WindowManager windowManager, XNAControl masterControl) : base(windowManager)
        {
            this.masterControl = masterControl ?? throw new ArgumentNullException("masterControl");
            masterControl.MouseEnter += MasterControl_MouseEnter;
            masterControl.MouseLeave += MasterControl_MouseLeave;
            masterControl.MouseMove += MasterControl_MouseMove;
            masterControl.EnabledChanged += MasterControl_EnabledChanged;
            InputEnabled = false;
            DrawOrder = int.MaxValue;
            GetParentControl(masterControl.Parent).AddChild(this);
            Visible = false;
        }

        private XNAControl GetParentControl(XNAControl parent)
        {
            if (parent is INItializableWindow)
                return parent as INItializableWindow;
            else if (parent.Parent == null) // Allow adding us to non-window panels
                return parent;
            else
                return GetParentControl(parent.Parent);
        }

        private void MasterControl_EnabledChanged(object sender, EventArgs e)
            => Enabled = masterControl.Enabled;

            public override string Text
    {
        get => TSMapEditor.UI.MainMenu.IsChinese && !string.IsNullOrEmpty(CNText) ? CNText : base.Text;
        set
        {
            base.Text = value;
            Vector2 textSize = Renderer.GetTextDimensions(GetDisplayText(), Constants.UIDefaultFont);
            Width = (int)textSize.X + ToolTipMargin * 2;
            Height = (int)textSize.Y + ToolTipMargin * 2;
        }
    }
    
    private string GetDisplayText()
    {
        return TSMapEditor.UI.MainMenu.IsChinese && !string.IsNullOrEmpty(CNText) ? CNText : base.Text;
    }

        public override float Alpha { get; set; }
        public bool IsMasterControlOnCursor { get; set; }

        private XNAControl masterControl;

        private TimeSpan cursorTime = TimeSpan.Zero;


        private void MasterControl_MouseEnter(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Text))
                return;

            DisplayAtLocation(SumPoints(WindowManager.Cursor.Location,
                new Point(DisplayOffsetX, DisplayOffsetX)));
            IsMasterControlOnCursor = true;
        }

        private void MasterControl_MouseLeave(object sender, EventArgs e)
        {
            IsMasterControlOnCursor = false;
            cursorTime = TimeSpan.Zero;
        }

        private void MasterControl_MouseMove(object sender, EventArgs e)
        {
            if (!Visible && !string.IsNullOrEmpty(Text))
            {
                // Move the tooltip if the cursor has moved while staying 
                // on the control area and we're invisible
                DisplayAtLocation(SumPoints(WindowManager.Cursor.Location,
                    new Point(DisplayOffsetX, DisplayOffsetX)));
            }
        }

        /// <summary>
        /// Sets the tool tip's location, checking that it doesn't exceed the window's bounds.
        /// </summary>
        /// <param name="location">The point at location coordinates.</param>
        public void DisplayAtLocation(Point location)
        {
            X = location.X + Width > WindowManager.RenderResolutionX ?
                WindowManager.RenderResolutionX - Width : location.X;
            Y = location.Y - Height < 0 ? 0 : location.Y - Height;
        }

        public override void Update(GameTime gameTime)
        {
            if (Blocked)
            {
                HideToolTip();
                return;
            }

            if (IsMasterControlOnCursor)
            {
                cursorTime += gameTime.ElapsedGameTime;

                if (cursorTime > TimeSpan.FromMilliseconds(ToolTipDelay))
                {
                    Alpha += AlphaRate * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    ShowToolTip();
                    return;
                }
            }

            Alpha -= AlphaRate * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (Alpha < 0f)
            {
                HideToolTip();
            }
        }

        private void ShowToolTip()
        {
            if (Alpha > 1.0f)
                Alpha = 1.0f;

            Visible = true;

            if (!Detached)
                Detach();
        }

        private void HideToolTip()
        {
            Alpha = 0f;
            Visible = false;

            if (Detached)
                Attach();
        }

        public override void Draw(GameTime gameTime)
        {
            Renderer.FillRectangle(ClientRectangle,
                UISettings.ActiveSettings.BackgroundColor * Alpha);
            Renderer.DrawRectangle(ClientRectangle,
                UISettings.ActiveSettings.AltColor * Alpha);
            Renderer.DrawString(GetDisplayText(), Constants.UIDefaultFont,
                new Vector2(X + ToolTipMargin, Y + ToolTipMargin),
                UISettings.ActiveSettings.AltColor * Alpha, 1.0f);
        }

        private Point SumPoints(Point p1, Point p2) => p1 + p2;
    }
}
