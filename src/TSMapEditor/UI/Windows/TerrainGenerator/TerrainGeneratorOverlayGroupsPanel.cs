using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows.TerrainGenerator
{
    /// <summary>
    /// A panel that allows the user to customize how the terrain 
    /// generator places overlay on the map.
    /// </summary>
    public class TerrainGeneratorOverlayGroupsPanel : EditorPanel
    {
        private const int MaxOverlayGroupCount = 8;

        public TerrainGeneratorOverlayGroupsPanel(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private EditorTextBox[] overlayNames;
        private EditorTextBox[] frameIndices;
        private EditorNumberTextBox[] overlayGroupOpenChances;
        private EditorNumberTextBox[] overlayGroupOccupiedChances;
        private XNALabel[] lblOverlayTypes;
        private XNALabel[] lblFrameIndices;
        private XNALabel[] lblOpenChance;
        private XNALabel[] lblOccupiedChance;

        public override void Initialize()
        {
            overlayNames = new EditorTextBox[MaxOverlayGroupCount];
            frameIndices = new EditorTextBox[MaxOverlayGroupCount];
            overlayGroupOpenChances = new EditorNumberTextBox[MaxOverlayGroupCount];
            overlayGroupOccupiedChances = new EditorNumberTextBox[MaxOverlayGroupCount];
            lblOverlayTypes = new XNALabel[MaxOverlayGroupCount];
            lblFrameIndices = new XNALabel[MaxOverlayGroupCount];
            lblOpenChance = new XNALabel[MaxOverlayGroupCount];
            lblOccupiedChance = new XNALabel[MaxOverlayGroupCount];

            int y = Constants.UIEmptyTopSpace;

            for (int i = 0; i < MaxOverlayGroupCount; i++)
            {
                var lblTileSet = new XNALabel(WindowManager);
                lblTileSet.Name = nameof(lblTileSet) + i;
                lblTileSet.X = Constants.UIEmptySideSpace;
                lblTileSet.Y = y;
                lblTileSet.FontIndex = Constants.UIBoldFont;
                lblTileSet.Text = $"Overlay Type Name (Group #{i + 1})";
                AddChild(lblTileSet);
                lblOverlayTypes[i] = lblTileSet;

                var selTileSet = new EditorTextBox(WindowManager);
                selTileSet.Name = nameof(selTileSet) + i;
                selTileSet.X = lblTileSet.X;
                selTileSet.Y = lblTileSet.Bottom + Constants.UIVerticalSpacing;
                selTileSet.Width = 200;
                AddChild(selTileSet);
                overlayNames[i] = selTileSet;

                var lblTileIndices = new XNALabel(WindowManager);
                lblTileIndices.Name = nameof(lblTileIndices) + i;
                lblTileIndices.X = selTileSet.Right + Constants.UIHorizontalSpacing;
                lblTileIndices.Y = lblTileSet.Y;
                lblTileIndices.Text = $"Indexes of frames to place (leave blank for all)";
                AddChild(lblTileIndices);
                lblFrameIndices[i] = lblTileIndices;

                var tbTileIndices = new EditorTextBox(WindowManager);
                tbTileIndices.Name = nameof(selTileSet) + i;
                tbTileIndices.X = lblTileIndices.X;
                tbTileIndices.Y = lblTileIndices.Bottom + Constants.UIVerticalSpacing;
                tbTileIndices.Width = 280;
                AddChild(tbTileIndices);
                frameIndices[i] = tbTileIndices;

                var lblOpenChanceLabel = new XNALabel(WindowManager);
                lblOpenChanceLabel.Name = nameof(lblOpenChanceLabel) + i;
                lblOpenChanceLabel.X = tbTileIndices.Right + Constants.UIHorizontalSpacing;
                lblOpenChanceLabel.Y = lblTileSet.Y;
                lblOpenChanceLabel.Text = "Open cell chance:";
                AddChild(lblOpenChanceLabel);
                lblOpenChance[i] = lblOpenChanceLabel;

                var tbOpenChance = new EditorNumberTextBox(WindowManager);
                tbOpenChance.Name = nameof(tbOpenChance) + i;
                tbOpenChance.X = lblOpenChanceLabel.X;
                tbOpenChance.Y = selTileSet.Y;
                tbOpenChance.AllowDecimals = true;
                tbOpenChance.Width = 120;
                AddChild(tbOpenChance);
                overlayGroupOpenChances[i] = tbOpenChance;

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
                tbOccupiedChance.Y = selTileSet.Y;
                tbOccupiedChance.AllowDecimals = true;
                tbOccupiedChance.Width = Width - tbOccupiedChance.X - Constants.UIEmptySideSpace;
                AddChild(tbOccupiedChance);
                overlayGroupOccupiedChances[i] = tbOccupiedChance;

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
            for (int i = 0; i < MaxOverlayGroupCount; i++)
            {
                lblOverlayTypes[i].Text = isChinese 
                    ? $"覆盖物类型名称 (组 #{i + 1})" 
                    : $"Overlay Type Name (Group #{i + 1})";
                    
                lblFrameIndices[i].Text = isChinese 
                    ? "要放置的帧索引（留空表示全部）" 
                    : "Indexes of frames to place (leave blank for all)";
                    
                lblOpenChance[i].Text = isChinese 
                    ? "空闲单元格几率:" 
                    : "Open cell chance:";
                    
                lblOccupiedChance[i].Text = isChinese 
                    ? "被占用单元格几率:" 
                    : "Occupied cell chance:";
            }
        }

        public List<TerrainGeneratorOverlayGroup> GetOverlayGroups()
        {
            var overlayGroups = new List<TerrainGeneratorOverlayGroup>();

            for (int i = 0; i < overlayNames.Length; i++)
            {
                string overlayTypeName = overlayNames[i].Text;
                if (string.IsNullOrWhiteSpace(overlayTypeName))
                    continue;

                var overlayType = map.Rules.OverlayTypes.Find(ot => ot.ININame == overlayTypeName);
                if (overlayType == null)
                {
                    bool isChinese = MainMenu.IsChinese;
                    string title = isChinese ? "生成器配置错误" : "Generator Config Error";
                    string message = isChinese 
                        ? $"不存在名为 '{overlayTypeName}' 的覆盖物类型！请确保您输入了正确的覆盖物类型INI名称并正确拼写。" 
                        : $"An overlay type named '{overlayTypeName}' does not exist! Make sure you typed the overlay type's INI name and spelled it correctly.";
                        
                    EditorMessageBox.Show(WindowManager, title, message, MessageBoxButtons.OK);
                    return null;
                }

                List<int> frameIndexes = null;
                string frameIndexesText = frameIndices[i].Text.Trim();
                if (!string.IsNullOrEmpty(frameIndexesText))
                {
                    string[] parts = frameIndexesText.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    frameIndexes = parts.Select(str => Conversions.IntFromString(str, -1)).ToList();
                    int invalidElement = frameIndexes.Find(index => index <= -1 || index >= map.TheaterInstance.GetOverlayFrameCount(overlayType));

                    if (invalidElement != 0) // this can never be 0 if an invalid element exists, because each valid overlay has at least 1 frame
                    {
                        bool isChinese = MainMenu.IsChinese;
                        string title = isChinese ? "生成器配置错误" : "Generator Config Error";
                        string message = isChinese 
                            ? $"覆盖物类型 '{overlayType.ININame}' 中不存在帧 '{invalidElement}'！" 
                            : $"Frame '{invalidElement}' does not exist in overlay type '{overlayType.ININame}'!";
                            
                        EditorMessageBox.Show(WindowManager, title, message, MessageBoxButtons.OK);
                        return null;
                    }
                }

                var overlayGroup = new TerrainGeneratorOverlayGroup(overlayType, frameIndexes,
                    overlayGroupOpenChances[i].DoubleValue,
                    overlayGroupOccupiedChances[i].DoubleValue);

                overlayGroups.Add(overlayGroup);
            }

            return overlayGroups;
        }

        public void LoadConfig(TerrainGeneratorConfiguration configuration)
        {
            for (int i = 0; i < configuration.OverlayGroups.Count && i < MaxOverlayGroupCount; i++)
            {
                var overlayGroup = configuration.OverlayGroups[i];

                overlayNames[i].Text = overlayGroup.OverlayType.ININame;

                if (overlayGroup.FrameIndices == null)
                    frameIndices[i].Text = string.Empty;
                else
                    frameIndices[i].Text = string.Join(",", overlayGroup.FrameIndices.Select(index => index.ToString(CultureInfo.InvariantCulture)));

                overlayGroupOpenChances[i].DoubleValue = overlayGroup.OpenChance;
                overlayGroupOccupiedChances[i].DoubleValue = overlayGroup.OverlapChance;
            }

            for (int i = configuration.OverlayGroups.Count; i < overlayNames.Length; i++)
            {
                overlayNames[i].Text = string.Empty;
                frameIndices[i].Text = string.Empty;
                overlayGroupOpenChances[i].DoubleValue = 0.0;
                overlayGroupOccupiedChances[i].DoubleValue = 0.0;
            }
        }
    }
}
