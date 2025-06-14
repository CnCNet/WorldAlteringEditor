using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows.TerrainGenerator
{
    /// <summary>
    /// A panel that allows the user to customize how the terrain 
    /// generator places terrain types on the map.
    /// </summary>
    public class TerrainGeneratorTerrainTypeGroupsPanel : EditorPanel
    {
        private const int MaxTerrainTypeGroupCount = 8;

        public TerrainGeneratorTerrainTypeGroupsPanel(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private EditorTextBox[] terrainTypeTextBoxes;
        private EditorNumberTextBox[] terrainTypeOpenChances;
        private EditorNumberTextBox[] terrainTypeOccupiedChances;
        private XNALabel[] lblTerrainTypes;
        private XNALabel[] lblOpenChance;
        private XNALabel[] lblOccupiedChance;

        public override void Initialize()
        {
            terrainTypeTextBoxes = new EditorTextBox[MaxTerrainTypeGroupCount];
            terrainTypeOpenChances = new EditorNumberTextBox[MaxTerrainTypeGroupCount];
            terrainTypeOccupiedChances = new EditorNumberTextBox[MaxTerrainTypeGroupCount];
            lblTerrainTypes = new XNALabel[MaxTerrainTypeGroupCount];
            lblOpenChance = new XNALabel[MaxTerrainTypeGroupCount];
            lblOccupiedChance = new XNALabel[MaxTerrainTypeGroupCount];

            int y = Constants.UIEmptyTopSpace;

            for (int i = 0; i < MaxTerrainTypeGroupCount; i++)
            {
                var lblTerrainTypeHeader = new XNALabel(WindowManager);
                lblTerrainTypeHeader.Name = nameof(lblTerrainTypeHeader) + i;
                lblTerrainTypeHeader.X = Constants.UIEmptySideSpace;
                lblTerrainTypeHeader.Y = y;
                lblTerrainTypeHeader.FontIndex = Constants.UIBoldFont;
                lblTerrainTypeHeader.Text = $"Terrain Types (Group #{i + 1})";
                AddChild(lblTerrainTypeHeader);
                lblTerrainTypes[i] = lblTerrainTypeHeader;

                var tbTerrainTypes = new EditorTextBox(WindowManager);
                tbTerrainTypes.Name = nameof(tbTerrainTypes) + i;
                tbTerrainTypes.X = lblTerrainTypeHeader.X;
                tbTerrainTypes.Y = lblTerrainTypeHeader.Bottom + Constants.UIVerticalSpacing;
                tbTerrainTypes.Width = (Width - 252) - tbTerrainTypes.X - Constants.UIEmptySideSpace;
                AddChild(tbTerrainTypes);
                terrainTypeTextBoxes[i] = tbTerrainTypes;

                var lblOpenChanceLabel = new XNALabel(WindowManager);
                lblOpenChanceLabel.Name = nameof(lblOpenChanceLabel) + i;
                lblOpenChanceLabel.X = tbTerrainTypes.Right + Constants.UIHorizontalSpacing;
                lblOpenChanceLabel.Y = lblTerrainTypeHeader.Y;
                lblOpenChanceLabel.Text = "Open cell chance:";
                AddChild(lblOpenChanceLabel);
                lblOpenChance[i] = lblOpenChanceLabel;

                var tbOpenChance = new EditorNumberTextBox(WindowManager);
                tbOpenChance.Name = nameof(tbOpenChance) + i;
                tbOpenChance.X = lblOpenChanceLabel.X;
                tbOpenChance.Y = tbTerrainTypes.Y;
                tbOpenChance.AllowDecimals = true;
                tbOpenChance.Width = 120;
                AddChild(tbOpenChance);
                terrainTypeOpenChances[i] = tbOpenChance;

                var lblOccupiedChanceLabel = new XNALabel(WindowManager);
                lblOccupiedChanceLabel.Name = nameof(lblOccupiedChanceLabel) + i;
                lblOccupiedChanceLabel.X = tbOpenChance.Right + Constants.UIHorizontalSpacing;
                lblOccupiedChanceLabel.Y = lblOpenChanceLabel.Y;
                lblOccupiedChanceLabel.Text = "Occupied cell chance:";
                AddChild(lblOccupiedChanceLabel);
                lblOccupiedChance[i] = lblOccupiedChanceLabel;

                var tbOccupiedChance = new EditorNumberTextBox(WindowManager);
                tbOccupiedChance.Name = nameof(tbOpenChance) + i;
                tbOccupiedChance.X = lblOccupiedChanceLabel.X;
                tbOccupiedChance.Y = tbTerrainTypes.Y;
                tbOccupiedChance.AllowDecimals = true;
                tbOccupiedChance.Width = 120;
                AddChild(tbOccupiedChance);
                terrainTypeOccupiedChances[i] = tbOccupiedChance;

                y = tbOccupiedChance.Bottom + Constants.UIEmptyTopSpace;
            }

            Height = y + Constants.UIEmptyBottomSpace;

            // 初始化完成后设置语言
            RefreshLanguage(MainMenu.IsChinese);

            base.Initialize();
        }
        
        /// <summary>
        /// 刷新面板的语言
        /// </summary>
        public void RefreshLanguage(bool isChinese)
        {
            for (int i = 0; i < MaxTerrainTypeGroupCount; i++)
            {
                lblTerrainTypes[i].Text = isChinese 
                    ? $"地形类型 (组 #{i + 1})" 
                    : $"Terrain Types (Group #{i + 1})";
                    
                lblOpenChance[i].Text = isChinese 
                    ? "空闲单元格几率:" 
                    : "Open cell chance:";
                    
                lblOccupiedChance[i].Text = isChinese 
                    ? "被占用单元格几率:" 
                    : "Occupied cell chance:";
            }
        }

        public List<TerrainGeneratorTerrainTypeGroup> GetTerrainTypeGroups()
        {
            var terrainTypeGroups = new List<TerrainGeneratorTerrainTypeGroup>();

            for (int i = 0; i < terrainTypeTextBoxes.Length; i++)
            {
                string text = terrainTypeTextBoxes[i].Text.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                string[] parts = text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var terrainTypes = new List<TerrainType>();
                for (int a = 0; a < parts.Length; a++)
                {
                    var terrainType = map.Rules.TerrainTypes.Find(tt => tt.ININame == parts[a]);
                    if (terrainType == null)
                    {
                        bool isChinese = MainMenu.IsChinese;
                        string title = isChinese ? "生成器配置错误" : "Generator Config Error";
                        string message = isChinese
                            ? $"指定的地形类型 '{parts[a]}' 不存在！"
                            : $"Specified terrain type '{parts[a]}' does not exist!";
                            
                        EditorMessageBox.Show(WindowManager, title, message, MessageBoxButtons.OK);
                        return null;
                    }
                    terrainTypes.Add(terrainType);
                }

                var terrainTypeGroup = new TerrainGeneratorTerrainTypeGroup(terrainTypes,
                    terrainTypeOpenChances[i].DoubleValue,
                    terrainTypeOccupiedChances[i].DoubleValue);

                terrainTypeGroups.Add(terrainTypeGroup);
            }

            return terrainTypeGroups;
        }

        public void LoadConfig(TerrainGeneratorConfiguration configuration)
        {
            for (int i = 0; i < configuration.TerrainTypeGroups.Count && i < MaxTerrainTypeGroupCount; i++)
            {
                var ttGroup = configuration.TerrainTypeGroups[i];

                terrainTypeTextBoxes[i].Text = string.Join(",", ttGroup.TerrainTypes.Select(tt => tt.ININame));
                terrainTypeOpenChances[i].DoubleValue = ttGroup.OpenChance;
                terrainTypeOccupiedChances[i].DoubleValue = ttGroup.OverlapChance;
            }

            for (int i = configuration.TerrainTypeGroups.Count; i < terrainTypeTextBoxes.Length; i++)
            {
                terrainTypeTextBoxes[i].Text = string.Empty;
                terrainTypeOpenChances[i].DoubleValue = 0.0;
                terrainTypeOccupiedChances[i].DoubleValue = 0.0;
            }
        }
    }
}
