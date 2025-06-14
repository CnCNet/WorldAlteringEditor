using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.Rendering;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class LightingSettingsWindow : INItializableWindow
    {
        public LightingSettingsWindow(WindowManager windowManager, Map map, EditorState state) : base(windowManager)
        {
            this.map = map;
            this.state = state;
        }

        private readonly Map map;
        private readonly EditorState state;

        // 标题和说明标签
        private XNALabel lblHeader;
        private XNALabel lblDescription;
        
        // 常规光照设置部分
        private XNALabel lblNormal;
        private EditorNumberTextBox tbAmbientNormal;
        private XNALabel lblAmbientNormal;
        private EditorNumberTextBox tbLevelNormal;
        private XNALabel lblLevelNormal;
        private EditorNumberTextBox tbGroundNormal;
        private XNALabel lblGroundNormal;
        private EditorNumberTextBox tbRedNormal;
        private XNALabel lblRedNormal;
        private EditorNumberTextBox tbGreenNormal;
        private XNALabel lblGreenNormal;
        private EditorNumberTextBox tbBlueNormal;
        private XNALabel lblBlueNormal;

        // 离子风暴光照设置部分
        private XNALabel lblIonStorm;
        private EditorNumberTextBox tbAmbientIS;
        private XNALabel lblAmbientIS;
        private EditorNumberTextBox tbLevelIS;
        private XNALabel lblLevelIS;
        private EditorNumberTextBox tbGroundIS;
        private XNALabel lblGroundIS;
        private EditorNumberTextBox tbRedIS;
        private XNALabel lblRedIS;
        private EditorNumberTextBox tbGreenIS;
        private XNALabel lblGreenIS;
        private EditorNumberTextBox tbBlueIS;
        private XNALabel lblBlueIS;

        // 统治者光照设置部分
        private XNALabel lblDominator;
        private EditorNumberTextBox tbAmbientDominator;
        private XNALabel lblAmbientDominator;
        private EditorNumberTextBox tbAmbientChangeRateDominator;
        private XNALabel lblAmbientChangeRateDominator;
        private EditorNumberTextBox tbLevelDominator;
        private XNALabel lblLevelDominator;
        private EditorNumberTextBox tbGroundDominator;
        private XNALabel lblGroundDominator;
        private EditorNumberTextBox tbRedDominator;
        private XNALabel lblRedDominator;
        private EditorNumberTextBox tbGreenDominator;
        private XNALabel lblGreenDominator;
        private EditorNumberTextBox tbBlueDominator;
        private XNALabel lblBlueDominator;

        // 光照预览部分
        private XNALabel lblLightingPreview;
        private XNADropDown ddLightingPreview;
        private EditorButton btnApply;

        public override void Initialize()
        {
            Name = nameof(LightingSettingsWindow);
            base.Initialize();

            // 获取标题和说明标签引用
            lblHeader = FindChild<XNALabel>("lblHeader");
            lblDescription = FindChild<XNALabel>("lblDescription");
            
            // 获取常规光照部分控件引用
            lblNormal = FindChild<XNALabel>("lblNormal");
            tbAmbientNormal = FindChild<EditorNumberTextBox>(nameof(tbAmbientNormal));
            lblAmbientNormal = FindChild<XNALabel>("lblAmbientNormal");
            tbLevelNormal = FindChild<EditorNumberTextBox>(nameof(tbLevelNormal));
            lblLevelNormal = FindChild<XNALabel>("lblLevelNormal");
            tbGroundNormal = FindChild<EditorNumberTextBox>(nameof(tbGroundNormal));
            lblGroundNormal = FindChild<XNALabel>("lblGroundNormal");
            tbRedNormal = FindChild<EditorNumberTextBox>(nameof(tbRedNormal));
            lblRedNormal = FindChild<XNALabel>("lblRedNormal");
            tbGreenNormal = FindChild<EditorNumberTextBox>(nameof(tbGreenNormal));
            lblGreenNormal = FindChild<XNALabel>("lblGreenNormal");
            tbBlueNormal = FindChild<EditorNumberTextBox>(nameof(tbBlueNormal));
            lblBlueNormal = FindChild<XNALabel>("lblBlueNormal");
            
            // 获取离子风暴部分控件引用
            lblIonStorm = FindChild<XNALabel>("lblIonStorm");
            tbAmbientIS = FindChild<EditorNumberTextBox>(nameof(tbAmbientIS));
            lblAmbientIS = FindChild<XNALabel>("lblAmbientIS");
            tbLevelIS = FindChild<EditorNumberTextBox>(nameof(tbLevelIS));
            lblLevelIS = FindChild<XNALabel>("lblLevelIS");
            tbGroundIS = FindChild<EditorNumberTextBox>(nameof(tbGroundIS));
            lblGroundIS = FindChild<XNALabel>("lblGroundIS");
            tbRedIS = FindChild<EditorNumberTextBox>(nameof(tbRedIS));
            lblRedIS = FindChild<XNALabel>("lblRedIS");
            tbGreenIS = FindChild<EditorNumberTextBox>(nameof(tbGreenIS));
            lblGreenIS = FindChild<XNALabel>("lblGreenIS");
            tbBlueIS = FindChild<EditorNumberTextBox>(nameof(tbBlueIS));
            lblBlueIS = FindChild<XNALabel>("lblBlueIS");
            
            // 获取统治者部分控件引用
            lblDominator = FindChild<XNALabel>("lblDominator");
            tbAmbientDominator = FindChild<EditorNumberTextBox>(nameof(tbAmbientDominator));
            lblAmbientDominator = FindChild<XNALabel>("lblAmbientDominator");
            tbAmbientChangeRateDominator = FindChild<EditorNumberTextBox>(nameof(tbAmbientChangeRateDominator));
            lblAmbientChangeRateDominator = FindChild<XNALabel>("lblAmbientChangeRateDominator");
            tbLevelDominator = FindChild<EditorNumberTextBox>(nameof(tbLevelDominator));
            lblLevelDominator = FindChild<XNALabel>("lblLevelDominator");
            tbGroundDominator = FindChild<EditorNumberTextBox>(nameof(tbGroundDominator));
            lblGroundDominator = FindChild<XNALabel>("lblGroundDominator");
            tbRedDominator = FindChild<EditorNumberTextBox>(nameof(tbRedDominator));
            lblRedDominator = FindChild<XNALabel>("lblRedDominator");
            tbGreenDominator = FindChild<EditorNumberTextBox>(nameof(tbGreenDominator));
            lblGreenDominator = FindChild<XNALabel>("lblGreenDominator");
            tbBlueDominator = FindChild<EditorNumberTextBox>(nameof(tbBlueDominator));
            lblBlueDominator = FindChild<XNALabel>("lblBlueDominator");

            // 获取光照预览部分控件引用
            lblLightingPreview = FindChild<XNALabel>("lblLightingPreview");
            ddLightingPreview = FindChild<XNADropDown>(nameof(ddLightingPreview));
            btnApply = FindChild<EditorButton>("btnApply");

            btnApply.LeftClick += BtnApply_LeftClick;
            
            // 初始化语言
            RefreshLanguage(MainMenu.IsChinese);
        }

        public void Open()
        {
            const string format = "0.000";

            tbAmbientNormal.Text = map.Lighting.Ambient.ToString(format);
            tbLevelNormal.Text = map.Lighting.Level.ToString(format);
            tbGroundNormal.Text = map.Lighting.Ground.ToString(format);
            tbRedNormal.Text = map.Lighting.Red.ToString(format);
            tbGreenNormal.Text = map.Lighting.Green.ToString(format);
            tbBlueNormal.Text = map.Lighting.Blue.ToString(format);

            tbAmbientIS.Text = map.Lighting.IonAmbient.ToString(format);
            tbLevelIS.Text = map.Lighting.IonLevel.ToString(format);
            tbGroundIS.Text = map.Lighting.IonGround.ToString(format);
            tbRedIS.Text = map.Lighting.IonRed.ToString(format);
            tbGreenIS.Text = map.Lighting.IonGreen.ToString(format);
            tbBlueIS.Text = map.Lighting.IonBlue.ToString(format);

            if (Constants.IsRA2YR)
            {
                tbAmbientDominator.Text = (map.Lighting.DominatorAmbient ?? 0).ToString(format);
                tbAmbientChangeRateDominator.Text = (map.Lighting.DominatorAmbientChangeRate ?? 0).ToString(format);
                tbLevelDominator.Text = (map.Lighting.DominatorLevel ?? 0).ToString(format);
                tbGroundDominator.Text = (map.Lighting.DominatorGround ?? 0).ToString(format);
                tbRedDominator.Text = (map.Lighting.DominatorRed ?? 0).ToString(format);
                tbGreenDominator.Text = (map.Lighting.DominatorGreen ?? 0).ToString(format);
                tbBlueDominator.Text = (map.Lighting.DominatorBlue ?? 0).ToString(format);
            }

            ddLightingPreview.SelectedIndex = (int)state.LightingPreviewState;
            
            // 确保使用当前语言设置
            RefreshLanguage(MainMenu.IsChinese);

            Show();
        }
        
        /// <summary>
        /// 刷新窗口中的语言
        /// </summary>
        public void RefreshLanguage(bool isChinese)
        {
            // 设置窗口标题和描述
            if (lblHeader != null)
                lblHeader.Text = isChinese ? "光照" : "LIGHTING";
                
            if (lblDescription != null)
                lblDescription.Text = isChinese ? "光照设置定义地图上地形的照明效果。" : "Lighting settings define the lighting of the terrain on the map.";
                
            // 设置常规光照部分的文本
            if (lblNormal != null)
                lblNormal.Text = isChinese ? "常规设置" : "Normal Settings";
                
            if (lblAmbientNormal != null)
                lblAmbientNormal.Text = isChinese ? "环境光:" : "Ambient:";
                
            if (lblLevelNormal != null)
                lblLevelNormal.Text = isChinese ? "层级:" : "Level:";
                
            if (lblGroundNormal != null)
                lblGroundNormal.Text = isChinese ? "地面:" : "Ground:";
                
            if (lblRedNormal != null)
                lblRedNormal.Text = isChinese ? "红色:" : "Red:";
                
            if (lblGreenNormal != null)
                lblGreenNormal.Text = isChinese ? "绿色:" : "Green:";
                
            if (lblBlueNormal != null)
                lblBlueNormal.Text = isChinese ? "蓝色:" : "Blue:";
                
            // 设置离子风暴部分的文本
            if (lblIonStorm != null)
                lblIonStorm.Text = isChinese ? "离子风暴设置" : "Ion Storm Settings";
                
            if (lblAmbientIS != null)
                lblAmbientIS.Text = isChinese ? "环境光:" : "Ambient:";
                
            if (lblLevelIS != null)
                lblLevelIS.Text = isChinese ? "层级:" : "Level:";
                
            if (lblGroundIS != null)
                lblGroundIS.Text = isChinese ? "地面:" : "Ground:";
                
            if (lblRedIS != null)
                lblRedIS.Text = isChinese ? "红色:" : "Red:";
                
            if (lblGreenIS != null)
                lblGreenIS.Text = isChinese ? "绿色:" : "Green:";
                
            if (lblBlueIS != null)
                lblBlueIS.Text = isChinese ? "蓝色:" : "Blue:";
                
            // 设置统治者部分的文本
            if (lblDominator != null)
                lblDominator.Text = isChinese ? "统治者设置" : "Dominator Settings";
                
            if (lblAmbientDominator != null)
                lblAmbientDominator.Text = isChinese ? "环境光:" : "Ambient:";
                
            if (lblAmbientChangeRateDominator != null)
                lblAmbientChangeRateDominator.Text = isChinese ? "环境变化率:" : "Ambient Change Rate:";
                
            if (lblLevelDominator != null)
                lblLevelDominator.Text = isChinese ? "层级:" : "Level:";
                
            if (lblGroundDominator != null)
                lblGroundDominator.Text = isChinese ? "地面:" : "Ground:";
                
            if (lblRedDominator != null)
                lblRedDominator.Text = isChinese ? "红色:" : "Red:";
                
            if (lblGreenDominator != null)
                lblGreenDominator.Text = isChinese ? "绿色:" : "Green:";
                
            if (lblBlueDominator != null)
                lblBlueDominator.Text = isChinese ? "蓝色:" : "Blue:";
                
            // 设置光照预览部分的文本
            if (lblLightingPreview != null)
                lblLightingPreview.Text = isChinese ? "光照预览:" : "Lighting Preview:";
                
            // 更新下拉框选项
            if (ddLightingPreview != null)
            {
                var selectedIndex = ddLightingPreview.SelectedIndex;
                ddLightingPreview.Items.Clear();
                
                if (isChinese)
                {
                    ddLightingPreview.AddItem("常规");
                    ddLightingPreview.AddItem("离子风暴");
                    if (Constants.IsRA2YR)
                        ddLightingPreview.AddItem("统治者");
                }
                else
                {
                    ddLightingPreview.AddItem("Normal");
                    ddLightingPreview.AddItem("Ion Storm");
                    if (Constants.IsRA2YR)
                        ddLightingPreview.AddItem("Dominator");
                }
                
                // 恢复选择的项
                if (selectedIndex >= 0 && selectedIndex < ddLightingPreview.Items.Count)
                    ddLightingPreview.SelectedIndex = selectedIndex;
            }
            
            // 设置应用按钮的文本
            if (btnApply != null)
                btnApply.Text = isChinese ? "应用" : "Apply";
        }

        private void BtnApply_LeftClick(object sender, System.EventArgs e)
        {
            map.Lighting.Ambient = tbAmbientNormal.DoubleValue;
            map.Lighting.Level = tbLevelNormal.DoubleValue;
            map.Lighting.Ground = tbGroundNormal.DoubleValue;
            map.Lighting.Red = tbRedNormal.DoubleValue;
            map.Lighting.Green = tbGreenNormal.DoubleValue;
            map.Lighting.Blue = tbBlueNormal.DoubleValue;

            map.Lighting.IonAmbient = tbAmbientIS.DoubleValue;
            map.Lighting.IonLevel = tbLevelIS.DoubleValue;
            map.Lighting.IonGround = tbGroundIS.DoubleValue;
            map.Lighting.IonRed = tbRedIS.DoubleValue;
            map.Lighting.IonGreen = tbGreenIS.DoubleValue;
            map.Lighting.IonBlue = tbBlueIS.DoubleValue;

            if (Constants.IsRA2YR)
            {
                map.Lighting.DominatorAmbient = tbAmbientDominator.DoubleValue;
                map.Lighting.DominatorAmbientChangeRate = tbAmbientChangeRateDominator.DoubleValue;
                map.Lighting.DominatorLevel = tbLevelDominator.DoubleValue;
                map.Lighting.DominatorGround = tbGroundDominator.DoubleValue;
                map.Lighting.DominatorRed = tbRedDominator.DoubleValue;
                map.Lighting.DominatorGreen = tbGreenDominator.DoubleValue;
                map.Lighting.DominatorBlue = tbBlueDominator.DoubleValue;
            }

            state.LightingPreviewState = (LightingPreviewMode)ddLightingPreview.SelectedIndex;

            map.Lighting.RefreshLightingColors();
        }
    }
}
