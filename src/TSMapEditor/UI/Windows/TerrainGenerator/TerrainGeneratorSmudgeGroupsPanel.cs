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
    /// generator places smudges on the map.
    /// </summary>
    public class TerrainGeneratorSmudgeGroupsPanel : EditorPanel
    {
        private const int MaxSmudgeTypeGroupCount = 8;

        public TerrainGeneratorSmudgeGroupsPanel(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private EditorTextBox[] smudgeTypeTextBoxes;
        private EditorNumberTextBox[] smudgeTypeOpenChances;
        private EditorNumberTextBox[] smudgeTypeOccupiedChances;
        private XNALabel[] lblSmudgeTypes;
        private XNALabel[] lblOpenChance;
        private XNALabel[] lblOccupiedChance;

        public override void Initialize()
        {
            smudgeTypeTextBoxes = new EditorTextBox[MaxSmudgeTypeGroupCount];
            smudgeTypeOpenChances = new EditorNumberTextBox[MaxSmudgeTypeGroupCount];
            smudgeTypeOccupiedChances = new EditorNumberTextBox[MaxSmudgeTypeGroupCount];
            lblSmudgeTypes = new XNALabel[MaxSmudgeTypeGroupCount];
            lblOpenChance = new XNALabel[MaxSmudgeTypeGroupCount];
            lblOccupiedChance = new XNALabel[MaxSmudgeTypeGroupCount];

            int y = Constants.UIEmptyTopSpace;

            for (int i = 0; i < MaxSmudgeTypeGroupCount; i++)
            {
                var lblSmudgeTypesHeader = new XNALabel(WindowManager);
                lblSmudgeTypesHeader.Name = nameof(lblSmudgeTypesHeader) + i;
                lblSmudgeTypesHeader.X = Constants.UIEmptySideSpace;
                lblSmudgeTypesHeader.Y = y;
                lblSmudgeTypesHeader.FontIndex = Constants.UIBoldFont;
                lblSmudgeTypesHeader.Text = $"Smudge Types (Group #{i + 1})";
                AddChild(lblSmudgeTypesHeader);
                lblSmudgeTypes[i] = lblSmudgeTypesHeader;

                var tbSmudgeTypes = new EditorTextBox(WindowManager);
                tbSmudgeTypes.Name = nameof(tbSmudgeTypes) + i;
                tbSmudgeTypes.X = lblSmudgeTypesHeader.X;
                tbSmudgeTypes.Y = lblSmudgeTypesHeader.Bottom + Constants.UIVerticalSpacing;
                tbSmudgeTypes.Width = (Width - 252) - tbSmudgeTypes.X - Constants.UIEmptySideSpace;
                AddChild(tbSmudgeTypes);
                smudgeTypeTextBoxes[i] = tbSmudgeTypes;

                var lblOpenChanceLabel = new XNALabel(WindowManager);
                lblOpenChanceLabel.Name = nameof(lblOpenChanceLabel) + i;
                lblOpenChanceLabel.X = tbSmudgeTypes.Right + Constants.UIHorizontalSpacing;
                lblOpenChanceLabel.Y = lblSmudgeTypesHeader.Y;
                lblOpenChanceLabel.Text = "Open cell chance:";
                AddChild(lblOpenChanceLabel);
                lblOpenChance[i] = lblOpenChanceLabel;

                var tbOpenChance = new EditorNumberTextBox(WindowManager);
                tbOpenChance.Name = nameof(tbOpenChance) + i;
                tbOpenChance.X = lblOpenChanceLabel.X;
                tbOpenChance.Y = tbSmudgeTypes.Y;
                tbOpenChance.AllowDecimals = true;
                tbOpenChance.Width = 120;
                AddChild(tbOpenChance);
                smudgeTypeOpenChances[i] = tbOpenChance;

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
                tbOccupiedChance.Y = tbSmudgeTypes.Y;
                tbOccupiedChance.AllowDecimals = true;
                tbOccupiedChance.Width = 120;
                AddChild(tbOccupiedChance);
                smudgeTypeOccupiedChances[i] = tbOccupiedChance;

                y = tbOccupiedChance.Bottom + Constants.UIEmptyTopSpace;
            }
            
            // 初始化完成后设置语言
            RefreshLanguage(MainMenu.IsChinese);

            base.Initialize();
        }
        
        /// <summary>
        /// 刷新面板的语言
        /// </summary>
        public void RefreshLanguage(bool isChinese)
        {
            for (int i = 0; i < MaxSmudgeTypeGroupCount; i++)
            {
                lblSmudgeTypes[i].Text = isChinese 
                    ? $"污迹类型 (组 #{i + 1})" 
                    : $"Smudge Types (Group #{i + 1})";
                    
                lblOpenChance[i].Text = isChinese 
                    ? "空闲单元格几率:" 
                    : "Open cell chance:";
                    
                lblOccupiedChance[i].Text = isChinese 
                    ? "被占用单元格几率:" 
                    : "Occupied cell chance:";
            }
        }

        public List<TerrainGeneratorSmudgeGroup> GetSmudgeGroups()
        {
            var smudgeGroups = new List<TerrainGeneratorSmudgeGroup>();

            for (int i = 0; i < smudgeTypeTextBoxes.Length; i++)
            {
                string text = smudgeTypeTextBoxes[i].Text.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                string[] parts = text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var smudgeTypes = new List<SmudgeType>();
                for (int a = 0; a < parts.Length; a++)
                {
                    var smudgeType = map.Rules.SmudgeTypes.Find(tt => tt.ININame == parts[a]);
                    if (smudgeType == null)
                    {
                        bool isChinese = MainMenu.IsChinese;
                        string title = isChinese ? "生成器配置错误" : "Generator Config Error";
                        string message = isChinese 
                            ? $"指定的污迹类型 '{parts[a]}' 不存在！" 
                            : $"Specified smudge type '{parts[a]}' does not exist!";
                            
                        EditorMessageBox.Show(WindowManager, title, message, MessageBoxButtons.OK);
                        return null;
                    }
                    smudgeTypes.Add(smudgeType);
                }

                var smudgeGroup = new TerrainGeneratorSmudgeGroup(smudgeTypes,
                    smudgeTypeOpenChances[i].DoubleValue,
                    smudgeTypeOccupiedChances[i].DoubleValue);

                smudgeGroups.Add(smudgeGroup);
            }

            return smudgeGroups;
        }

        public void LoadConfig(TerrainGeneratorConfiguration configuration)
        {
            for (int i = 0; i < configuration.SmudgeGroups.Count && i < MaxSmudgeTypeGroupCount; i++)
            {
                var smudgeGroup = configuration.SmudgeGroups[i];

                smudgeTypeTextBoxes[i].Text = string.Join(",", smudgeGroup.SmudgeTypes.Select(st => st.ININame));
                smudgeTypeOpenChances[i].DoubleValue = smudgeGroup.OpenChance;
                smudgeTypeOccupiedChances[i].DoubleValue = smudgeGroup.OverlapChance;
            }

            for (int i = configuration.SmudgeGroups.Count; i < smudgeTypeTextBoxes.Length; i++)
            {
                smudgeTypeTextBoxes[i].Text = string.Empty;
                smudgeTypeOpenChances[i].DoubleValue = 0.0;
                smudgeTypeOccupiedChances[i].DoubleValue = 0.0;
            }
        }
    }
}
