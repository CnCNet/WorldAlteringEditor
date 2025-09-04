using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Globalization;
using System.Windows.Forms;
using TSMapEditor.GameMath;
using TSMapEditor.Misc;
using TSMapEditor.Settings;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI
{
    /// <summary>
    /// A single screen resolution.
    /// </summary>
    sealed class ScreenResolution : IComparable<ScreenResolution>
    {
        public ScreenResolution(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// The width of the resolution in pixels.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// The height of the resolution in pixels.
        /// </summary>
        public int Height { get; set; }

        public override string ToString()
        {
            return Width + "x" + Height;
        }

        public int CompareTo(ScreenResolution res2)
        {
            if (this.Width < res2.Width)
                return -1;
            else if (this.Width > res2.Width)
                return 1;
            else // equal
            {
                if (this.Height < res2.Height)
                    return -1;
                else if (this.Height > res2.Height)
                    return 1;
                else return 0;
            }
        }

        public override bool Equals(object obj)
        {
            var resolution = obj as ScreenResolution;

            if (resolution == null)
                return false;

            return CompareTo(resolution) == 0;
        }

        public override int GetHashCode()
        {
            return Width * 10000 + Height;
        }
    }

    public class SettingsPanel : INItializableWindow
    {
        public SettingsPanel(WindowManager windowManager) : base(windowManager)
        {
            CenterByDefault = false;
            BackgroundTexture = AssetLoader.CreateTexture(UISettings.ActiveSettings.BackgroundColor, 2, 2);
        }

        private XNADropDown ddLanguage;
        private XNADropDown ddRenderScale;
        private XNADropDown ddTargetFPS;
        private XNACheckBox chkBorderless;
        private XNADropDown ddTheme;
        private XNADropDown ddScrollRate;
        private XNACheckBox chkUseBoldFont;
        private XNACheckBox chkGraphicsLevel;
        private XNACheckBox chkSmartScriptActionCloning;
        private EditorTextBox tbTextEditorPath;

        public override void Kill()
        {
            BackgroundTexture?.Dispose();
            base.Kill();
        }

        public override void Initialize()
        {
            Name = nameof(SettingsPanel);
            base.Initialize();

            ddLanguage = FindChild<XNADropDown>(nameof(ddLanguage));
            TranslatorSetup.Translations.ForEach(tr => ddLanguage.AddItem(new XNADropDownItem() { Text = tr.UIName, Tag = tr }));
            ddLanguage.SelectedIndexChanged += DdLanguage_SelectedIndexChanged;

            const int MinWidth = 1024;
            const int MinHeight = 600;
            int MaxWidth = Screen.PrimaryScreen.Bounds.Width;
            int MaxHeight = Screen.PrimaryScreen.Bounds.Height;

            ddRenderScale = FindChild<XNADropDown>(nameof(ddRenderScale));
            var renderScales = new double[] { 4.0, 2.5, 3.0, 2.5, 2.0, 1.75, 1.5, 1.25, 1.0, 0.75, 0.5 };
            for (int i = 0; i < renderScales.Length; i++)
            {
                Point2D screenSize = new Point2D((int)(MaxWidth / renderScales[i]), (int)(MaxHeight / renderScales[i]));
                if (screenSize.X > MinWidth && screenSize.Y > MinHeight)
                {
                    ddRenderScale.AddItem(new XNADropDownItem() { Text = renderScales[i].ToString("F2", CultureInfo.InvariantCulture) + "x", Tag = renderScales[i] });
                }
            }

            ddTargetFPS = FindChild<XNADropDown>(nameof(ddTargetFPS));
            var targetFramerates = new int[] { 1000, 480, 240, 144, 120, 90, 75, 60, 30, 20 };
            foreach (int frameRate in targetFramerates)
                ddTargetFPS.AddItem(new XNADropDownItem() { Text = frameRate.ToString(CultureInfo.InvariantCulture), Tag = frameRate });

            ddTheme = FindChild<XNADropDown>(nameof(ddTheme));
            foreach (var theme in EditorThemes.Themes)
                ddTheme.AddItem(theme.Key);

            ddScrollRate = FindChild<XNADropDown>(nameof(ddScrollRate));
            var scrollRateNames = new string[] 
            { 
                Translate(this, "ScrollRateFastest", "Fastest"),
                Translate(this, "ScrollRateFaster", "Faster"),
                Translate(this, "ScrollRateFast", "Fast"),
                Translate(this, "ScrollRateNormal", "Normal"),
                Translate(this, "ScrollRateSlow", "Slow"),
                Translate(this, "ScrollRateSlower", "Slower"),
                Translate(this, "ScrollRateSlowest", "Slowest"),
            };
            var scrollRateValues = new int[] { 21, 18, 15, 12, 9, 6, 3 };
            for (int i = 0; i < scrollRateNames.Length; i++)
            {
                ddScrollRate.AddItem(new XNADropDownItem() { Text = scrollRateNames[i], Tag = scrollRateValues[i] });
            }

            chkBorderless = FindChild<XNACheckBox>(nameof(chkBorderless));

            chkUseBoldFont = FindChild<XNACheckBox>(nameof(chkUseBoldFont));

            chkGraphicsLevel = FindChild<XNACheckBox>(nameof(chkGraphicsLevel));

            chkSmartScriptActionCloning = FindChild<XNACheckBox>(nameof(chkSmartScriptActionCloning));

            tbTextEditorPath = FindChild<EditorTextBox>(nameof(tbTextEditorPath));

            LoadSettings();
        }

        private void DdLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ddLanguage.SelectedItem == null)
                return;

            WindowManager.AddCallback(() =>
            {
                TranslatorSetup.SetActiveTranslation(((Translation)ddLanguage.SelectedItem.Tag).InternalName);
                RefreshLayout();
            });
        }

        private void LoadSettings()
        {
            var userSettings = UserSettings.Instance;

            ddLanguage.SelectedIndex = TranslatorSetup.ActiveTranslation.Index;
            ddRenderScale.SelectedIndex = ddRenderScale.Items.FindIndex(i => (double)i.Tag == userSettings.RenderScale.GetValue());
            ddTargetFPS.SelectedIndex = ddTargetFPS.Items.FindIndex(item => (int)item.Tag == userSettings.TargetFPS.GetValue());

            int selectedTheme = ddTheme.Items.FindIndex(i => i.Text == userSettings.Theme);
            if (selectedTheme == -1)
                selectedTheme = ddTheme.Items.FindIndex(i => i.Text == "Default");
            ddTheme.SelectedIndex = selectedTheme;
            ddScrollRate.SelectedIndex = ddScrollRate.Items.FindIndex(item => (int)item.Tag == userSettings.ScrollRate.GetValue());

            chkBorderless.Checked = userSettings.Borderless;
            chkUseBoldFont.Checked = userSettings.UseBoldFont;
            chkGraphicsLevel.Checked = userSettings.GraphicsLevel > 0;
            chkSmartScriptActionCloning.Checked = userSettings.SmartScriptActionCloning;

            tbTextEditorPath.Text = userSettings.TextEditorPath;
        }

        public void ApplySettings()
        {
            var userSettings = UserSettings.Instance;

            userSettings.Language.UserDefinedValue = ((Translation)ddLanguage.SelectedItem.Tag).InternalName;
            userSettings.UseBoldFont.UserDefinedValue = chkUseBoldFont.Checked;
            userSettings.GraphicsLevel.UserDefinedValue = chkGraphicsLevel.Checked ? 1 : 0;
            userSettings.SmartScriptActionCloning.UserDefinedValue = chkSmartScriptActionCloning.Checked;

            userSettings.Theme.UserDefinedValue = ddTheme.SelectedItem.Text;
            if (ddScrollRate.SelectedItem != null)
                userSettings.ScrollRate.UserDefinedValue = (int)ddScrollRate.SelectedItem.Tag;

            userSettings.Borderless.UserDefinedValue = chkBorderless.Checked;
            userSettings.FullscreenWindowed.UserDefinedValue = chkBorderless.Checked;

            if (ddRenderScale.SelectedItem != null)
                userSettings.RenderScale.UserDefinedValue = (double)ddRenderScale.SelectedItem.Tag;

            if (ddTargetFPS.SelectedItem != null)
            {
                userSettings.TargetFPS.UserDefinedValue = (int)ddTargetFPS.SelectedItem.Tag;
                WindowManager.Game.TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / UserSettings.Instance.TargetFPS);
            }

            userSettings.TextEditorPath.UserDefinedValue = tbTextEditorPath.Text;
        }
    }
}
