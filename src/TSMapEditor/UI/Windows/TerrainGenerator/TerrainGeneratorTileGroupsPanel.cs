using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows.TerrainGenerator
{
    /// <summary>
    /// A panel that allows the user to customize how the terrain 
    /// generator places terrain tiles on the map.
    /// </summary>
    public class TerrainGeneratorTileGroupsPanel : EditorPanel
    {
        private const int MaxTileGroupCount = 8;

        public TerrainGeneratorTileGroupsPanel(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private EditorPopUpSelector[] tileSetSelectors;
        private EditorTextBox[] tileIndices;
        private EditorNumberTextBox[] tileGroupOpenChances;
        private EditorNumberTextBox[] tileGroupOccupiedChances;
        private XNALabel[] lblTileSets;
        private XNALabel[] lblTileIndices;
        private XNALabel[] lblOpenChance;
        private XNALabel[] lblOccupiedChance;

        private SelectTileSetWindow selectTileSetWindow;

        private EditorPopUpSelector openedTileSetSelector;

        public override void Initialize()
        {
            tileSetSelectors = new EditorPopUpSelector[MaxTileGroupCount];
            tileIndices = new EditorTextBox[MaxTileGroupCount];
            tileGroupOpenChances = new EditorNumberTextBox[MaxTileGroupCount];
            tileGroupOccupiedChances = new EditorNumberTextBox[MaxTileGroupCount];
            lblTileSets = new XNALabel[MaxTileGroupCount];
            lblTileIndices = new XNALabel[MaxTileGroupCount];
            lblOpenChance = new XNALabel[MaxTileGroupCount];
            lblOccupiedChance = new XNALabel[MaxTileGroupCount];

            selectTileSetWindow = new SelectTileSetWindow(WindowManager, map);
            var tileSetDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent.Parent, selectTileSetWindow);
            tileSetDarkeningPanel.Hidden += TileSetDarkeningPanel_Hidden;

            int y = Constants.UIEmptyTopSpace;

            for (int i = 0; i < MaxTileGroupCount; i++)
            {
                var lblTileSet = new XNALabel(WindowManager);
                lblTileSet.Name = nameof(lblTileSet) + i;
                lblTileSet.X = Constants.UIEmptySideSpace;
                lblTileSet.Y = y;
                lblTileSet.FontIndex = Constants.UIBoldFont;
                lblTileSet.Text = $"Tile Set (Group #{i + 1})";
                AddChild(lblTileSet);
                lblTileSets[i] = lblTileSet;

                var selTileSet = new EditorPopUpSelector(WindowManager);
                selTileSet.Name = nameof(selTileSet) + i;
                selTileSet.X = lblTileSet.X;
                selTileSet.Y = lblTileSet.Bottom + Constants.UIVerticalSpacing;
                selTileSet.Width = 200;
                AddChild(selTileSet);
                tileSetSelectors[i] = selTileSet;
                selTileSet.LeftClick += SelTileSet_LeftClick;

                var lblTileIndicesLabel = new XNALabel(WindowManager);
                lblTileIndicesLabel.Name = nameof(lblTileIndicesLabel) + i;
                lblTileIndicesLabel.X = selTileSet.Right + Constants.UIHorizontalSpacing;
                lblTileIndicesLabel.Y = lblTileSet.Y;
                lblTileIndicesLabel.Text = $"Indexes of tiles to place (leave blank for all)";
                AddChild(lblTileIndicesLabel);
                lblTileIndices[i] = lblTileIndicesLabel;

                var tbTileIndices = new EditorTextBox(WindowManager);
                tbTileIndices.Name = nameof(selTileSet) + i;
                tbTileIndices.X = lblTileIndicesLabel.X;
                tbTileIndices.Y = lblTileIndicesLabel.Bottom + Constants.UIVerticalSpacing;
                tbTileIndices.Width = 280;
                AddChild(tbTileIndices);
                tileIndices[i] = tbTileIndices;

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
                tileGroupOpenChances[i] = tbOpenChance;

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
                tileGroupOccupiedChances[i] = tbOccupiedChance;

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
            for (int i = 0; i < MaxTileGroupCount; i++)
            {
                lblTileSets[i].Text = isChinese 
                    ? $"地形图块集 (组 #{i + 1})" 
                    : $"Tile Set (Group #{i + 1})";
                    
                lblTileIndices[i].Text = isChinese 
                    ? "要放置的图块索引（留空表示全部）" 
                    : "Indexes of tiles to place (leave blank for all)";
                    
                lblOpenChance[i].Text = isChinese 
                    ? "空闲单元格几率:" 
                    : "Open cell chance:";
                    
                lblOccupiedChance[i].Text = isChinese 
                    ? "被占用单元格几率:" 
                    : "Occupied cell chance:";
            }
        }

        private void TileSetDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            openedTileSetSelector.Tag = selectTileSetWindow.SelectedObject;

            if (selectTileSetWindow.SelectedObject == null)
                openedTileSetSelector.Text = string.Empty;
            else
                openedTileSetSelector.Text = selectTileSetWindow.SelectedObject.SetName;
        }

        private void SelTileSet_LeftClick(object sender, System.EventArgs e)
        {
            openedTileSetSelector = (EditorPopUpSelector)sender;
            selectTileSetWindow.Open((TileSet)openedTileSetSelector.Tag);
        }

        public List<TerrainGeneratorTileGroup> GetTileGroups()
        {
            var tileGroups = new List<TerrainGeneratorTileGroup>();

            for (int i = 0; i < tileSetSelectors.Length; i++)
            {
                var tileSet = (TileSet)tileSetSelectors[i].Tag;
                if (tileSet == null)
                    continue;

                List<int> tileIndexesInSet = null;
                string tileIndicesText = tileIndices[i].Text.Trim();
                if (!string.IsNullOrEmpty(tileIndicesText))
                {
                    string[] parts = tileIndicesText.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    tileIndexesInSet = parts.Select(str => Conversions.IntFromString(str, -1)).ToList();
                    int invalidElement = tileIndexesInSet.Find(index => index <= -1 || index >= tileSet.LoadedTileCount);

                    if (invalidElement != 0) // this can never be 0 if an invalid element exists, because each valid tileset has at least 1 tile
                    {
                        bool isChinese = MainMenu.IsChinese;
                        string title = isChinese ? "生成器配置错误" : "Generator Config Error";
                        string message = isChinese 
                            ? $"图块集 '{tileSet.SetName}' 中不存在索引为 '{invalidElement}' 的图块！" 
                            : $"Tile with index '{invalidElement}' does not exist in tile set '{tileSet.SetName}'!";
                            
                        EditorMessageBox.Show(WindowManager, title, message, MessageBoxButtons.OK);
                        return null;
                    }
                }

                var tileGroup = new TerrainGeneratorTileGroup(tileSet, tileIndexesInSet,
                    tileGroupOpenChances[i].DoubleValue,
                    tileGroupOccupiedChances[i].DoubleValue);

                tileGroups.Add(tileGroup);
            }

            return tileGroups;
        }

        public void LoadConfig(TerrainGeneratorConfiguration configuration)
        {
            for (int i = 0; i < configuration.TileGroups.Count && i < MaxTileGroupCount; i++)
            {
                var tileGroup = configuration.TileGroups[i];

                tileSetSelectors[i].Text = tileGroup.TileSet.SetName;
                tileSetSelectors[i].Tag = tileGroup.TileSet;

                if (tileGroup.TileIndicesInSet == null)
                    tileIndices[i].Text = string.Empty;
                else
                    tileIndices[i].Text = string.Join(",", tileGroup.TileIndicesInSet.Select(index => index.ToString(CultureInfo.InvariantCulture)));

                tileGroupOpenChances[i].DoubleValue = tileGroup.OpenChance;
                tileGroupOccupiedChances[i].DoubleValue = tileGroup.OverlapChance;
            }

            for (int i = configuration.TileGroups.Count; i < tileSetSelectors.Length; i++)
            {
                tileSetSelectors[i].Text = string.Empty;
                tileSetSelectors[i].Tag = null;
                tileIndices[i].Text = string.Empty;
                tileGroupOpenChances[i].DoubleValue = 0.0;
                tileGroupOccupiedChances[i].DoubleValue = 0.0;
            }
        }
    }
}
