using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.UI.Windows;

namespace TSMapEditor.UI.Controls
{
    public class DarkeningPanel : XNAPanel
    {
        private const float ALPHA_RATE = 0.6f;

        public DarkeningPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        public event EventHandler Hidden;

        public override void Initialize()
        {
            Name = nameof(DarkeningPanel);

            SetPositionAndSize();

            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            DrawBorders = false;

            WindowManager.RenderResolutionChanged += WindowManager_RenderResolutionChanged;

            base.Initialize();
        }

        private void WindowManager_RenderResolutionChanged(object sender, EventArgs e)
        {
            SetPositionAndSize();
        }

        public override void Kill()
        {
            if (BackgroundTexture != null)
            {
                BackgroundTexture.Dispose();
                BackgroundTexture = null;
            }

            WindowManager.RenderResolutionChanged -= WindowManager_RenderResolutionChanged;

            base.Kill();
        }

        public void SetPositionAndSize()
        {
            if (Parent != null)
            {
                ClientRectangle = new Rectangle(-Parent.X, -Parent.Y,
                    WindowManager.RenderResolutionX,
                    WindowManager.RenderResolutionY);
            }
            else
            {
                ClientRectangle = new Rectangle(0, 0, WindowManager.RenderResolutionX, WindowManager.RenderResolutionY);
            }
        }

        public override void AddChild(XNAControl child)
        {
            base.AddChild(child);

            child.VisibleChanged += Child_VisibleChanged;
        }

        private void Child_VisibleChanged(object sender, EventArgs e)
        {
            var xnaControl = (XNAControl)sender;

            if (xnaControl.Visible)
                Show();
            else
                Hide();
        }

        public void Show()
        {
            Enabled = true;
            Visible = true;
            AlphaRate = ALPHA_RATE;
            Alpha = 0.01f;

            foreach (XNAControl child in Children)
            {
                child.Enabled = true;
                child.Visible = true;
            }
        }

        public void Hide()
        {
            AlphaRate = -ALPHA_RATE;

            foreach (XNAControl child in Children)
            {
                child.Enabled = false;
                child.Visible = false;
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            RemapColor = Color.White * Alpha;

            if (Alpha <= 0.0f)
            {
                Enabled = false;
                Visible = false;
                Hidden?.Invoke(this, EventArgs.Empty);
            }
        }

        public static void AddAndInitializeWithControl(WindowManager wm, XNAControl control, bool display)
        {
            var dp = new DarkeningPanel(wm);
            dp.DrawOrder = int.MaxValue;
            dp.UpdateOrder = int.MaxValue;
            wm.AddAndInitializeControl(dp);
            dp.AddChild(control);

            if (display)
            {
                dp.Show();
            }
            else
            {
                dp.Hide();
                dp.Disable();
            }
        }

        public static DarkeningPanel InitializeAndAddToParentControlWithChild(WindowManager windowManager, XNAControl parent, XNAControl child)
        {
            var darkeningPanel = new DarkeningPanel(windowManager);
            darkeningPanel.DrawOrder = 1;
            darkeningPanel.UpdateOrder = 1;
            parent.AddChild(darkeningPanel);
            darkeningPanel.AddChild(child);
            darkeningPanel.Hide();
            darkeningPanel.Alpha = 0f;
            darkeningPanel.DrawOrder = WindowController.ChildWindowOrderValue * 2;
            darkeningPanel.UpdateOrder = WindowController.ChildWindowOrderValue * 2;
            darkeningPanel.Disable();
            child.CenterOnParent();

            return darkeningPanel;
        }
    }
}
