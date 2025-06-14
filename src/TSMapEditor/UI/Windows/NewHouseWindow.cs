using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    /// <summary>
    /// A window that prompts the user for the name and parent country of the new house.
    /// </summary>
    public class NewHouseWindow : INItializableWindow
    {
        public NewHouseWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private EditorTextBox tbHouseName;
        private XNADropDown ddParentCountry;
        private EditorButton btnAdd;
        
        // 添加标签字段以便于本地化
        private XNALabel lblHeader;
        private XNALabel lblHouseName;
        private XNALabel lblCountryNotice;
        private XNALabel lblParentCountry;

        public HouseType ParentCountry { get; set; }
        public bool Success { get; set; }

        private readonly Map map;

        public override void Initialize()
        {
            Name = nameof(NewHouseWindow);
            base.Initialize();

            tbHouseName = FindChild<EditorTextBox>(nameof(tbHouseName));
            ddParentCountry = FindChild<XNADropDown>(nameof(ddParentCountry));
            btnAdd = FindChild<EditorButton>(nameof(btnAdd));
            
            // 获取标签控件引用
            lblHeader = FindChild<XNALabel>("lblHeader");
            lblHouseName = FindChild<XNALabel>("lblHouseName");
            lblCountryNotice = FindChild<XNALabel>("lblCountryNotice");
            lblParentCountry = FindChild<XNALabel>("lblParentCountry");

            ddParentCountry.SelectedIndexChanged += DdParentCountry_SelectedIndexChanged;
            btnAdd.LeftClick += BtnAdd_LeftClick;

            if (!Constants.IsRA2YR)
            {
                ddParentCountry.Visible = false;
                lblParentCountry.Visible = false;
            }
            
            // 初始化时应用当前语言
            RefreshLanguage();
        }
        
        /// <summary>
        /// 刷新窗口的语言
        /// </summary>
        public void RefreshLanguage()
        {
            bool isChinese = MainMenu.IsChinese;
            
            if (isChinese)
            {
                // 设置窗口标题
                SetWindowTitle("添加新阵营");
                
                // 更新标签文本
                if (lblHeader != null)
                    lblHeader.Text = "添加新阵营";
                
                if (lblHouseName != null)
                    lblHouseName.Text = "阵营名称：";
                    
                if (lblCountryNotice != null)
                    lblCountryNotice.Text = "将为该阵营创建一个新的国家。";
                    
                if (lblParentCountry != null)
                    lblParentCountry.Text = "父级国家：";
                
                // 更新按钮文本
                if (btnAdd != null)
                    btnAdd.Text = "添加";
            }
            else
            {
                // 设置窗口标题
                SetWindowTitle("Add New House");
                
                // 更新标签文本
                if (lblHeader != null)
                    lblHeader.Text = "Add New House";
                    
                if (lblHouseName != null)
                    lblHouseName.Text = "House Name:";
                    
                if (lblCountryNotice != null)
                    lblCountryNotice.Text = "A new Country will also be created for the House.";
                    
                if (lblParentCountry != null)
                    lblParentCountry.Text = "Parent Country:";
                
                // 更新按钮文本
                if (btnAdd != null)
                    btnAdd.Text = "Add";
            }
        }
        
        // 通过修改INI配置设置窗口标题
        private void SetWindowTitle(string title)
        {
            try
            {
                // 使用反射获取ConfigIni
                var configIniField = GetType().BaseType.GetField("ConfigIni", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
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
                
                // 尝试调用刷新布局方法
                var method = GetType().BaseType.GetMethod("RefreshLayout", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
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

        private void DdParentCountry_SelectedIndexChanged(object sender, EventArgs e)
        {
            ParentCountry = (HouseType)ddParentCountry.SelectedItem.Tag;
        }

        private void BtnAdd_LeftClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbHouseName.Text))
            {
                bool isChinese = MainMenu.IsChinese;
                if (isChinese)
                {
                    EditorMessageBox.Show(WindowManager, "需要阵营名称",
                        "请输入阵营的名称。", MessageBoxButtons.OK);
                }
                else
                {
                    EditorMessageBox.Show(WindowManager, "House Name Required",
                        "Please input a name for the house.", MessageBoxButtons.OK);
                }

                return;
            }

            string houseName = tbHouseName.Text;
            string houseTypeName;
            if (houseName.EndsWith("House"))
                houseTypeName = houseName.Replace("House", "Country");
            else
                houseTypeName = houseName + "Country";

            var newHouseType = new HouseType(houseTypeName)
            {
                ParentCountry = ParentCountry.ININame,
                Index = map.Rules.RulesHouseTypes.Count + map.HouseTypes.Count,
                Side = ParentCountry.Side,
                Color = ParentCountry.Color,
                XNAColor = ParentCountry.XNAColor
            };

            Helpers.FindDefaultSideForNewHouseType(newHouseType, map.Rules);
            map.AddHouseType(newHouseType);

            var newHouse = new House(houseName)
            {
                Allies = houseName,
                Credits = 0,
                Edge = "West",
                IQ = 0,
                PercentBuilt = 100,
                PlayerControl = false,
                TechLevel = Constants.MaxHouseTechLevel,
                ID = map.Houses.Count
            };

            newHouse.Color = newHouseType.Color;
            newHouse.XNAColor = newHouseType.XNAColor;
            newHouse.Country = houseTypeName;
            newHouse.HouseType = newHouseType;

            map.AddHouse(newHouse);
            
            // 确保更新EditorState中的ObjectOwner，避免UI和数据不同步
            try
            {
                // 如果是第一个阵营，或者需要同步选择新建的阵营
                if (map.Houses.Count == 1 || TSMapEditor.Settings.UserSettings.Instance.SelectNewlyCreatedHouse?.GetValue() == true)
                {
                    // 设置新创建的阵营为当前对象所有者
                    var editorState = TSMapEditor.Settings.UserSettings.Instance.GetType().Assembly
                        .GetType("TSMapEditor.UI.EditorState")?.GetProperty("Instance")?.GetValue(null);
                    
                    if (editorState != null)
                    {
                        var objectOwnerProperty = editorState.GetType().GetProperty("ObjectOwner");
                        if (objectOwnerProperty != null)
                        {
                            objectOwnerProperty.SetValue(editorState, newHouse);
                        }
                    }
                }
            }
            catch
            {
                // 反射操作失败，静默忽略
            }

            Success = true;
            Hide();
        }

        private void ListParentCountries()
        {
            ddParentCountry.Items.Clear();

            map.Rules.RulesHouseTypes.ForEach(
                houseType => ddParentCountry.AddItem(new XNADropDownItem() 
            { 
                Text = houseType.ININame,
                TextColor = Helpers.GetHouseTypeUITextColor(houseType),
                Tag = houseType 
            }));
        }

        public void Open()
        {
            if (!Constants.IsRA2YR)
                throw new NotSupportedException(nameof(NewHouseWindow) + " should only be used with Countries.");

            // 打开窗口时刷新语言
            RefreshLanguage();
            Show();
            ListParentCountries();

            ddParentCountry.SelectedIndex = 0;
            tbHouseName.Text = "NewHouse";

            Success = false;
        }
    }
}