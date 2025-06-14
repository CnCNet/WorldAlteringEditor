using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.IO;
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
        private XNALabel lblRenderedObjectsNote;
        private EditorButton btnGenerate;

        public override void Initialize()
        {
            Name = nameof(MegamapGenerationOptionsWindow);
            base.Initialize();

            lblDescription = FindChild<XNALabel>(nameof(lblDescription));
            chkEmphasizeResources = FindChild<XNACheckBox>(nameof(chkEmphasizeResources));
            chkIncludeOnlyVisibleArea = FindChild<XNACheckBox>(nameof(chkIncludeOnlyVisibleArea));
            chkMarkPlayerSpots = FindChild<XNACheckBox>(nameof(chkMarkPlayerSpots));
            lblRenderedObjectsNote = FindChild<XNALabel>(nameof(lblRenderedObjectsNote));
            btnGenerate = FindChild<EditorButton>(nameof(btnGenerate));

            btnGenerate.LeftClick += BtnGenerate_LeftClick;
            
            // 初始化时刷新语言 - 确保所有控件都已找到
            RefreshLanguage(MainMenu.IsChinese);
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
            RefreshLanguage(MainMenu.IsChinese);
            Show();
        }

        public void RefreshLanguage(bool isChinese)
        {
            // 检查控件是否已初始化
            if (lblDescription == null || chkEmphasizeResources == null || 
                chkIncludeOnlyVisibleArea == null || chkMarkPlayerSpots == null)
                return;

            if (isChinese)
            {
                if (IsForPreview)
                {
                    lblDescription.Text = "预览生成选项:";
                    if (btnGenerate != null)
                        btnGenerate.Text = "生成预览";
                }
                else
                {
                    lblDescription.Text = "巨型地图提取选项:";
                    if (btnGenerate != null)
                        btnGenerate.Text = "提取巨型地图";
                }

                chkEmphasizeResources.Text = "强调资源";
                chkIncludeOnlyVisibleArea.Text = "仅包括地图的可见部分 (LocalSize)";
                chkMarkPlayerSpots.Text = "在航点0-7上标记玩家点位";
                if (lblRenderedObjectsNote != null)
                    lblRenderedObjectsNote.Text = "注意: 您的渲染对象选项(查看 -> 配置渲染对象...)也会影响生成的预览。";
                
                // 使用反射设置窗口标题（因为基类中没有直接提供WindowTitle属性）
                SetWindowTitle("巨型地图选项");
            }
            else
            {
                if (IsForPreview)
                {
                    lblDescription.Text = "Preview generation options:";
                    if (btnGenerate != null)
                        btnGenerate.Text = "Generate Preview";
                }
                else
                {
                    lblDescription.Text = "Megamap extraction options:";
                    if (btnGenerate != null)
                        btnGenerate.Text = "Extract Megamap";
                }

                chkEmphasizeResources.Text = "Emphasize resources";
                chkIncludeOnlyVisibleArea.Text = "Include only visible part of map (LocalSize)";
                chkMarkPlayerSpots.Text = "Mark player spots on waypoints 0-7";
                if (lblRenderedObjectsNote != null)
                    lblRenderedObjectsNote.Text = "Note that your options for rendered objects (View -> Configure Rendered Objects...) also impact the generated preview.";
                
                // 使用反射设置窗口标题（因为基类中没有直接提供WindowTitle属性）
                SetWindowTitle("Megamap Options");
            }
        }
        
        // 通过修改INI配置文件来设置窗口标题
        private void SetWindowTitle(string title)
        {
            // 我们需要尝试使用反射来修改窗口标题
            try 
            {
                var configIniField = GetType().BaseType.GetField("ConfigIni", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (configIniField != null)
                {
                    var configIni = configIniField.GetValue(this) as Rampastring.Tools.IniFile;
                    if (configIni != null)
                    {
                        var section = configIni.GetSection(Name);
                        if (section != null)
                        {
                            section.SetStringValue("WindowTitle", title);
                        }
                    }
                }
                
                // 尝试调用刷新布局的方法，使标题生效
                var method = GetType().BaseType.GetMethod("RefreshLayout", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (method != null)
                {
                    method.Invoke(this, null);
                }
            }
            catch
            {
                // 如果反射操作失败，静默忽略
            }
        }
    }
}
