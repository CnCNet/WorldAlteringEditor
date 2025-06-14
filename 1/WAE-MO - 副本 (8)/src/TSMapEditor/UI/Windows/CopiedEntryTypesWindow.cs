using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    /// <summary>
    /// A window that allows the user to configure which 
    /// object types should be copied when copying parts of a map.
    /// </summary>
    public class CopiedEntryTypesWindow : INItializableWindow
    {
        public CopiedEntryTypesWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        private XNACheckBox chkTerrainTiles;
        private XNACheckBox chkOverlay;
        private XNACheckBox chkSmudges;
        private XNACheckBox chkTerrainObjects;
        private XNACheckBox chkStructures;
        private XNACheckBox chkVehicles;
        private XNACheckBox chkInfantry;
        private XNALabel lblDescription;
        private EditorButton btnClose;


        public override void Initialize()
        {
            Name = nameof(CopiedEntryTypesWindow);
            base.Initialize();

            chkTerrainTiles = FindChild<XNACheckBox>(nameof(chkTerrainTiles));
            chkOverlay = FindChild<XNACheckBox>(nameof(chkOverlay));
            chkSmudges = FindChild<XNACheckBox>(nameof(chkSmudges));
            chkTerrainObjects = FindChild<XNACheckBox>(nameof(chkTerrainObjects));
            chkStructures = FindChild<XNACheckBox>(nameof(chkStructures));
            chkVehicles = FindChild<XNACheckBox>(nameof(chkVehicles));
            chkInfantry = FindChild<XNACheckBox>(nameof(chkInfantry));
            lblDescription = FindChild<XNALabel>(nameof(lblDescription));
            btnClose = FindChild<EditorButton>(nameof(btnClose));
            
            // 初始化时应用当前语言
            RefreshLanguage(MainMenu.IsChinese);
        }

        public void Open()
        {
            // 打开窗口时应用当前语言
            RefreshLanguage(MainMenu.IsChinese);
            Show();
        }
        
        /// <summary>
        /// 根据语言设置刷新界面文本
        /// </summary>
        public void RefreshLanguage(bool isChinese)
        {
            // 检查控件是否已初始化
            if (lblDescription == null || chkTerrainTiles == null || 
                chkOverlay == null || chkSmudges == null ||
                chkTerrainObjects == null || chkStructures == null ||
                chkVehicles == null || chkInfantry == null ||
                btnClose == null)
                return;
            
            if (isChinese)
            {
                lblDescription.Text = "选择复制地图数据时要复制的对象类型。";
                chkTerrainTiles.Text = "地形图块";
                chkOverlay.Text = "覆盖层";
                chkSmudges.Text = "污渍";
                chkTerrainObjects.Text = "地形对象";
                chkStructures.Text = "建筑";
                chkVehicles.Text = "载具";
                chkInfantry.Text = "步兵";
                btnClose.Text = "关闭";
                
                // 使用反射修改窗口标题
                SetWindowTitle("配置复制对象");
            }
            else
            {
                lblDescription.Text = "Select object types to copy when copying map data.";
                chkTerrainTiles.Text = "Terrain Tiles";
                chkOverlay.Text = "Overlay";
                chkSmudges.Text = "Smudges";
                chkTerrainObjects.Text = "Terrain Objects";
                chkStructures.Text = "Structures";
                chkVehicles.Text = "Vehicles";
                chkInfantry.Text = "Infantry";
                btnClose.Text = "Close";
                
                // 使用反射修改窗口标题
                SetWindowTitle("Configure Copied Objects");
            }
        }
        
        // 通过修改INI配置文件来设置窗口标题
        private void SetWindowTitle(string title)
        {
            // 使用反射来修改窗口标题
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

        public CopiedEntryType GetEnabledEntryTypes()
        {
            CopiedEntryType entryType = CopiedEntryType.Invalid;

            if (chkTerrainTiles.Checked)
                entryType |= CopiedEntryType.Terrain;
            if (chkOverlay.Checked)
                entryType |= CopiedEntryType.Overlay;
            if (chkSmudges.Checked)
                entryType |= CopiedEntryType.Smudge;
            if (chkTerrainObjects.Checked)
                entryType |= CopiedEntryType.TerrainObject;
            if (chkStructures.Checked)
                entryType |= CopiedEntryType.Structure;
            if (chkVehicles.Checked)
                entryType |= CopiedEntryType.Vehicle;
            if (chkInfantry.Checked)
                entryType |= CopiedEntryType.Infantry;

            return entryType;
        }
    }
}
