using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.Misc;
using TSMapEditor.Rendering;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class DeletionModeConfigurationWindow : INItializableWindow
    {
        public DeletionModeConfigurationWindow(WindowManager windowManager, EditorState editorState) : base(windowManager)
        {
            this.editorState = editorState;
        }

        private readonly EditorState editorState;

        private XNACheckBox chkCellTags;
        private XNACheckBox chkWaypoints;
        private XNACheckBox chkAircraft;
        private XNACheckBox chkInfantry;
        private XNACheckBox chkVehicles;
        private XNACheckBox chkStructures;
        private XNACheckBox chkTerrainObjects;
        private XNALabel lblDescription;
        private EditorButton btnApply;

        public override void Initialize()
        {
            Name = nameof(DeletionModeConfigurationWindow);
            base.Initialize();

            chkCellTags = FindChild<XNACheckBox>(nameof(chkCellTags));
            chkWaypoints = FindChild<XNACheckBox>(nameof(chkWaypoints));
            chkAircraft = FindChild<XNACheckBox>(nameof(chkAircraft));
            chkInfantry = FindChild<XNACheckBox>(nameof(chkInfantry));
            chkVehicles = FindChild<XNACheckBox>(nameof(chkVehicles));
            chkStructures = FindChild<XNACheckBox>(nameof(chkStructures));
            chkTerrainObjects = FindChild<XNACheckBox>(nameof(chkTerrainObjects));
            lblDescription = FindChild<XNALabel>("lblDescription");
            btnApply = FindChild<EditorButton>("btnApply");

            btnApply.LeftClick += BtnApply_LeftClick;
            
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
                // 窗口标题通过反射设置
                SetWindowTitle("选择要删除的对象类型");
                
                // 更新描述标签
                if (lblDescription != null)
                    lblDescription.Text = "选择要从地图上删除对象时要擦除的对象类型。";
                    
                // 更新复选框标签
                if (chkCellTags != null)
                    chkCellTags.Text = "单元格标签";
                if (chkWaypoints != null)
                    chkWaypoints.Text = "航点";
                if (chkAircraft != null)
                    chkAircraft.Text = "飞行器";
                if (chkInfantry != null)
                    chkInfantry.Text = "步兵";
                if (chkVehicles != null)
                    chkVehicles.Text = "载具";
                if (chkStructures != null)
                    chkStructures.Text = "建筑";
                if (chkTerrainObjects != null)
                    chkTerrainObjects.Text = "地形对象";
                    
                // 更新按钮文本
                if (btnApply != null)
                    btnApply.Text = "应用";
            }
            else
            {
                // 窗口标题通过反射设置
                SetWindowTitle("Select Object Types to Erase");
                
                // 更新描述标签
                if (lblDescription != null)
                    lblDescription.Text = "Select object types to erase when deleting objects from the map.";
                    
                // 更新复选框标签
                if (chkCellTags != null)
                    chkCellTags.Text = "CellTags";
                if (chkWaypoints != null)
                    chkWaypoints.Text = "Waypoints";
                if (chkAircraft != null)
                    chkAircraft.Text = "Aircraft";
                if (chkInfantry != null)
                    chkInfantry.Text = "Infantry";
                if (chkVehicles != null)
                    chkVehicles.Text = "Vehicles";
                if (chkStructures != null)
                    chkStructures.Text = "Structures";
                if (chkTerrainObjects != null)
                    chkTerrainObjects.Text = "Terrain Objects";
                    
                // 更新按钮文本
                if (btnApply != null)
                    btnApply.Text = "Apply";
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

        private void BtnApply_LeftClick(object sender, EventArgs e)
        {
            DeletionMode deletionMode = DeletionMode.None;

            if (chkCellTags.Checked)       deletionMode |= DeletionMode.CellTags;
            if (chkWaypoints.Checked)      deletionMode |= DeletionMode.Waypoints;
            if (chkAircraft.Checked)       deletionMode |= DeletionMode.Aircraft;
            if (chkInfantry.Checked)       deletionMode |= DeletionMode.Infantry;
            if (chkVehicles.Checked)       deletionMode |= DeletionMode.Vehicles;
            if (chkStructures.Checked)     deletionMode |= DeletionMode.Structures;
            if (chkTerrainObjects.Checked) deletionMode |= DeletionMode.TerrainObjects;

            editorState.DeletionMode = deletionMode;

            Hide();
        }

        public void Open()
        {
            SetCheckBoxStates();
            // 打开窗口时刷新语言
            RefreshLanguage();
            Show();
        }

        private void SetCheckBoxStates()
        {
            chkCellTags.Checked = editorState.DeletionMode.HasFlag(DeletionMode.CellTags);
            chkWaypoints.Checked = editorState.DeletionMode.HasFlag(DeletionMode.Waypoints);
            chkAircraft.Checked = editorState.DeletionMode.HasFlag(DeletionMode.Aircraft);
            chkInfantry.Checked = editorState.DeletionMode.HasFlag(DeletionMode.Infantry);
            chkVehicles.Checked = editorState.DeletionMode.HasFlag(DeletionMode.Vehicles);
            chkStructures.Checked = editorState.DeletionMode.HasFlag(DeletionMode.Structures);
            chkTerrainObjects.Checked = editorState.DeletionMode.HasFlag(DeletionMode.TerrainObjects);
        }
    }
}
