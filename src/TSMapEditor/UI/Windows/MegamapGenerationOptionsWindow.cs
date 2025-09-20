using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.Rendering;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class MegamapGenerationOptionsWindow : INItializableWindow
    {
        public MegamapGenerationOptionsWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        public event EventHandler<MegamapRenderOptions> OnGeneratePreview;

        public bool IsForPreview { get; set; }

        private XNALabel lblDescription;
        private XNACheckBox chkEmphasizeResources;
        private XNACheckBox chkIncludeOnlyVisibleArea;
        private XNACheckBox chkMarkPlayerSpots;
        private EditorButton btnGenerate;

        public override void Initialize()
        {
            Name = nameof(MegamapGenerationOptionsWindow);
            base.Initialize();

            lblDescription = FindChild<XNALabel>(nameof(lblDescription));
            chkEmphasizeResources = FindChild<XNACheckBox>(nameof(chkEmphasizeResources));
            chkIncludeOnlyVisibleArea = FindChild<XNACheckBox>(nameof(chkIncludeOnlyVisibleArea));
            chkMarkPlayerSpots = FindChild<XNACheckBox>(nameof(chkMarkPlayerSpots));
            btnGenerate = FindChild<EditorButton>(nameof(btnGenerate));

            btnGenerate.LeftClick += BtnGenerate_LeftClick;
        }

        private void BtnGenerate_LeftClick(object sender, EventArgs e)
        {
            MegamapRenderOptions megamapRenderOptions = MegamapRenderOptions.None;

            if (chkEmphasizeResources.Checked)     megamapRenderOptions |= MegamapRenderOptions.EmphasizeResources;
            if (chkIncludeOnlyVisibleArea.Checked) megamapRenderOptions |= MegamapRenderOptions.IncludeOnlyVisibleArea;
            if (chkMarkPlayerSpots.Checked)        megamapRenderOptions |= MegamapRenderOptions.MarkPlayerSpots;

            OnGeneratePreview?.Invoke(this, megamapRenderOptions);
            Hide();
        }

        public void Open(bool isForPreview)
        {
            IsForPreview = isForPreview;

            if (IsForPreview)
            {
                lblDescription.Text = Translate(this, "Preview.Description", "Preview generation options:");
                btnGenerate.Text = Translate(this, "Preview.Generate", "Generate Preview");
            }
            else
            {
                lblDescription.Text = Translate(this, "Extract.Description", "Megamap extraction options:");
                btnGenerate.Text = Translate(this, "Preview.Generate", "Extract Megamap");
            }

            Show();
        }
    }
}
