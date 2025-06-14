using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI.Controls;
using TSMapEditor.UI.CursorActions;
using TSMapEditor.Translations;

namespace TSMapEditor.UI
{
    enum TileSetSortMode
    {
        ID,
        Name
    }

    public class TileSelector : XNAControl
    {
        private const int TileSetListWidth = 180;
        private const int ResizeDragThreshold = 30;

        public TileSelector(WindowManager windowManager, Map map, TheaterGraphics theaterGraphics,
            PlaceTerrainCursorAction placeTerrainCursorAction, EditorState editorState) : base(windowManager)
        {
            this.map = map;
            this.theaterGraphics = theaterGraphics;
            this.placeTerrainCursorAction = placeTerrainCursorAction;
            this.editorState = editorState;
        }

        protected override void OnClientRectangleUpdated()
        {
            if (Initialized)
            {
                lbTileSetList.Height = Height - tbSearch.Bottom;
                lbTileSetList.Width = TileSetListWidth;
                TileDisplay.Height = Height;
                TileDisplay.Width = Width - TileSetListWidth;
            }

            base.OnClientRectangleUpdated();
        }

        private readonly Map map;
        private readonly TheaterGraphics theaterGraphics;
        private readonly PlaceTerrainCursorAction placeTerrainCursorAction;
        private readonly EditorState editorState;

        public TileDisplay TileDisplay { get; private set; }

        private bool IsChinese => TSMapEditor.UI.MainMenu.IsChinese;
        
        // 地形集中英文对照字典
        private static readonly Dictionary<string, string> TileSetChineseNameMap = new Dictionary<string, string>
        {
            { "LAT Grass", "LAT 草地" },
            { "Grass Ramps", "草地斜坡" },
            { "Grass Cliffs", "草地悬崖" },
            { "Sand Shorelines", "沙滩海岸线" },
            { "LAT Dark Grass", "LAT 深色草地" },
            { "Grass Water Cliffs", "草地水域悬崖" },
            { "Dirt Road Bends", "土路弯道" },
            { "Dirt Road Junctions", "土路交叉口" },
            { "Dirt Roads", "土路" },
            { "Grass Concrete Bridges", "草地水泥桥" },
            { "Highway Roads", "高速公路" },
            { "Water", "水域" },
            { "Dirt Road Ramps", "土路斜坡" },
            { "Grass Big Slopes", "草地大斜坡" },
            { "Grass Waterfalls A", "草地瀑布A" },
            { "LAT Ground", "LAT 地面" },
            { "Farm Crops", "农田作物" },
            { "Highway Road Ends", "高速公路末端" },
            { "Pavement Bits", "路面小块" },
            { "Highway Road Bits", "高速公路小块" },
            { "LAT Sand", "LAT 沙地" },
            { "LAT Pavement", "LAT 路面" },
            { "Highway Road Ramps", "高速公路斜坡" },
            { "Grass Waterfalls C", "草地瀑布C" },
            { "Grass Road Tunnels", "草地道路隧道" },
            { "Grass Tunnels Side", "草地隧道侧面" },
            { "Grass Water Caves A", "草地水洞A" },
            { "Grass Tunnels", "草地隧道" },
            { "Natural Sand Bridges", "自然沙桥" },
            { "Grass Waterridge", "草地水脊" },
            { "Sunken USS Arizona", "沉没的亚利桑那号" },
            { "Grass Wood Bridges", "草地木桥" },
            { "Pavement Shorelines A", "路面海岸线A" },
            { "Pavement Shorelines B", "路面海岸线B" },
            { "Grass Train Ramps", "草地火车斜坡" },
            { "Grass Train Tunnels", "草地火车隧道" },
            { "Train Crossings", "火车道口" },
            { "Train Junctions", "火车交叉口" },
            { "Train Ledges", "火车壁架" },
            { "Train Switches", "火车道岔" },
            { "Grass Water Tunnels", "草地水下隧道" },
            { "LAT Pavement B", "LAT 路面B" },
            { "Pavement B Fixes", "路面B修补" },
            { "Grass Water Caves B", "草地水洞B" },
            { "Highway Road Separators", "高速公路分隔带" },
            { "Train Underledge Fix", "火车地下壁架修补" },
            { "Grass Water Tunnel Sides", "草地水下隧道侧面" },
            { "Helipads", "直升机场" },
            { "Grass Train Bridges", "草地火车桥" },
            { "LAT Rubble", "LAT 碎石堆" }
        };

        private SortButton btnSort;
        private EditorSuggestionTextBox tbSearch;
        private TileSetListBox lbTileSetList;
        private XNAContextMenu tileSetContextMenu;
        private EditorContextMenu sortContextMenu;

        private TileSetSortMode _tileSetSortMode;
        private TileSetSortMode TileSetSortMode
        {
            get => _tileSetSortMode;
            set
            {
                _tileSetSortMode = value;
                RefreshTileSets();
            }
        }

        private bool isBeingDragged = false;
        private int previousMouseY;

        public override void Initialize()
        {
            Name = nameof(TileSelector);

            btnSort = new SortButton(WindowManager);
            btnSort.Name = nameof(btnSort);
            btnSort.X = TileSetListWidth - btnSort.Width;
            AddChild(btnSort);

            tbSearch = new EditorSuggestionTextBox(WindowManager);
            tbSearch.Name = nameof(tbSearch);
            tbSearch.Width = TileSetListWidth - btnSort.Width;
            tbSearch.Suggestion = IsChinese ? "搜索地形集..." : "Search TileSet...";
            AddChild(tbSearch);
            UIHelpers.AddSearchTipsBoxToControl(tbSearch);
            tbSearch.TextChanged += TbSearch_TextChanged;

            lbTileSetList = new TileSetListBox(WindowManager, theaterGraphics.Theater.TileSets.Count);
            lbTileSetList.Name = nameof(lbTileSetList);
            lbTileSetList.Y = tbSearch.Bottom;
            lbTileSetList.Height = Height - tbSearch.Bottom;
            lbTileSetList.Width = TileSetListWidth;
            lbTileSetList.AllowRightClickUnselect = false;
            lbTileSetList.SelectedIndexChanged += LbTileSetList_SelectedIndexChanged;
            AddChild(lbTileSetList);

            TileDisplay = new TileDisplay(WindowManager, map, theaterGraphics, placeTerrainCursorAction, editorState);
            TileDisplay.Name = nameof(TileDisplay);
            TileDisplay.Height = Height;
            TileDisplay.Width = Width - TileSetListWidth;
            TileDisplay.X = TileSetListWidth;
            AddChild(TileDisplay);

            lbTileSetList.BackgroundTexture = TileDisplay.BackgroundTexture;
            lbTileSetList.PanelBackgroundDrawMode = TileDisplay.PanelBackgroundDrawMode;

            sortContextMenu = new EditorContextMenu(WindowManager);
            sortContextMenu.Name = nameof(sortContextMenu);
            sortContextMenu.Width = 200;
            sortContextMenu.AddItem(IsChinese ? "按ID排序" : "Sort by ID", () => TileSetSortMode = TileSetSortMode.ID);
            sortContextMenu.AddItem(IsChinese ? "按名称排序" : "Sort by Name", () => TileSetSortMode = TileSetSortMode.Name);
            AddChild(sortContextMenu);

            btnSort.LeftClick += (s, e) => sortContextMenu.Open(GetCursorPoint());

            tileSetContextMenu = new EditorContextMenu(WindowManager);
            tileSetContextMenu.Name = nameof(tileSetContextMenu);
            tileSetContextMenu.Width = 200;
            tileSetContextMenu.AddItem(IsChinese ? "固定" : "Pin",
                () => { lbTileSetList.SetTileSetAsFavourite(((TileSet)lbTileSetList.SelectedItem.Tag).Index); RefreshTileSets(); },
                null,
                () => lbTileSetList.SelectedItem != null && !lbTileSetList.IsTileSetFavourite(((TileSet)lbTileSetList.SelectedItem.Tag).Index));
            tileSetContextMenu.AddItem(IsChinese ? "取消固定" : "Unpin",
                () => { lbTileSetList.ClearFavouriteStatus(((TileSet)lbTileSetList.SelectedItem.Tag).Index); RefreshTileSets(); },
                null,
                () => lbTileSetList.SelectedItem != null && lbTileSetList.IsTileSetFavourite(((TileSet)lbTileSetList.SelectedItem.Tag).Index));
            tileSetContextMenu.AddItem(IsChinese ? "取消选择" : "Unselect", () => lbTileSetList.SelectedIndex = -1);
            AddChild(tileSetContextMenu);

            lbTileSetList.RightClick += LbTileSetList_RightClick;

            base.Initialize();

            RefreshTileSets();

            KeyboardCommands.Instance.NextTileSet.Action = NextTileSet;
            KeyboardCommands.Instance.PreviousTileSet.Action = PreviousTileSet;
            WindowManager.RenderResolutionChanged += WindowManager_RenderResolutionChanged;
        }

        private void LbTileSetList_RightClick(object sender, EventArgs e)
        {
            lbTileSetList.SelectedIndex = lbTileSetList.HoveredIndex;

            if (lbTileSetList.SelectedItem != null)
                tileSetContextMenu.Open(GetCursorPoint());
        }

        private void WindowManager_RenderResolutionChanged(object sender, EventArgs e)
        {
            Width = WindowManager.RenderResolutionX - X;
            Y = WindowManager.RenderResolutionY - Height;
        }

        public override void Kill()
        {
            WindowManager.RenderResolutionChanged -= WindowManager_RenderResolutionChanged;
            base.Kill();
        }

        public void RefreshLanguage(bool isChinese)
        {
            tbSearch.Suggestion = isChinese ? "搜索地形集..." : "Search TileSet...";
            
            // 更新搜索提示框
            foreach (var control in tbSearch.Children)
            {
                if (control is XNALabel label && label.Name == "lblSearchTips")
                {
                    foreach (var tooltipControl in label.Children)
                    {
                        if (tooltipControl is ToolTip tooltip)
                        {
                            tooltip.Text = isChinese ? 
                                "搜索提示\r\n\r\n在文本框激活状态下：\r\n- 按回车键 (ENTER) 移动到列表中的下一个匹配项\r\n- 按ESC键清除搜索内容" :
                                "Search Tips\r\n\r\nWith the text box activated:\r\n- Press ENTER to move to next match in list\r\n- Press ESC to clear search query";
                            break;
                        }
                    }
                    break;
                }
            }
            
            // 更新菜单文本
            if (sortContextMenu != null)
            {
                sortContextMenu.Items[0].Text = isChinese ? "按ID排序" : "Sort by ID";
                sortContextMenu.Items[1].Text = isChinese ? "按名称排序" : "Sort by Name";
            }
            
            if (tileSetContextMenu != null)
            {
                tileSetContextMenu.Items[0].Text = isChinese ? "固定" : "Pin";
                tileSetContextMenu.Items[1].Text = isChinese ? "取消固定" : "Unpin";
                tileSetContextMenu.Items[2].Text = isChinese ? "取消选择" : "Unselect";
            }
            
            // 刷新地形集列表
            RefreshTileSets();
        }

        private void TbSearch_TextChanged(object sender, EventArgs e)
        {
            lbTileSetList.ViewTop = 0;

            if (string.IsNullOrWhiteSpace(tbSearch.Text) || tbSearch.Text == tbSearch.Suggestion)
            {
                foreach (var item in lbTileSetList.Items)
                    item.Visible = true;
            }
            else
            {
                lbTileSetList.SelectedIndex = -1;

                for (int i = 0; i < lbTileSetList.Items.Count; i++)
                {
                    var item = lbTileSetList.Items[i];
                    item.Visible = item.Text.Contains(tbSearch.Text, StringComparison.OrdinalIgnoreCase);

                    if (item.Visible && lbTileSetList.SelectedIndex == -1)
                        lbTileSetList.SelectedIndex = i;
                }
            }

            lbTileSetList.RefreshScrollbar();
        }

        private void NextTileSet()
        {
            if (lbTileSetList.Items.Count == 0)
                return;

            if (lbTileSetList.SelectedItem == null)
                lbTileSetList.SelectedIndex = 0;

            if (lbTileSetList.SelectedIndex == lbTileSetList.Items.Count - 1)
                return;

            lbTileSetList.SelectedIndex++;
        }

        private void PreviousTileSet()
        {
            if (lbTileSetList.Items.Count == 0)
                return;

            if (lbTileSetList.SelectedItem == null)
                lbTileSetList.SelectedIndex = lbTileSetList.Items.Count - 1;

            if (lbTileSetList.SelectedIndex == 0)
                return;

            lbTileSetList.SelectedIndex--;
        }

        public override void OnMouseLeftDown()
        {
            if (IsActive)
            {
                var cursorPoint = GetCursorPoint();

                if (!isBeingDragged && cursorPoint.Y > 0 && cursorPoint.Y < ResizeDragThreshold && Cursor.LeftDown)
                {
                    isBeingDragged = true;
                    previousMouseY = GetCursorPoint().Y;
                }
            }

            base.OnMouseLeftDown();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (isBeingDragged)
            {
                var cursorPoint = GetCursorPoint();

                if (cursorPoint.Y < previousMouseY)
                {
                    int difference = previousMouseY - cursorPoint.Y;
                    Y -= difference;
                    Height += difference;

                    if (Height > WindowManager.RenderResolutionY)
                    {
                        Height = WindowManager.RenderResolutionY;
                        Y = 0;
                    }
                }
                else if (cursorPoint.Y > previousMouseY)
                {
                    int difference = cursorPoint.Y - previousMouseY;
                    Y += difference;
                    Height -= difference;

                    if (Height <= 10)
                    {
                        Height = 10;
                        Y = WindowManager.RenderResolutionY - ScaledHeight;
                    }
                }

                previousMouseY = GetCursorPoint().Y;

                if (!Cursor.LeftDown)
                    isBeingDragged = false;
            }
        }

        private void LbTileSetList_SelectedIndexChanged(object sender, EventArgs e)
        {
            TileSet tileSet = null;
            if (lbTileSetList.SelectedItem != null)
                tileSet = lbTileSetList.SelectedItem.Tag as TileSet;

            TileDisplay.SetTileSet(tileSet);

            // Unselect the listbox
            if (WindowManager.SelectedControl == lbTileSetList)
                WindowManager.SelectedControl = null;
        }

        private void RefreshTileSets()
        {
            lbTileSetList.Clear();
            IOrderedEnumerable<TileSet> sortedTileSets = theaterGraphics.Theater.TileSets.OrderBy(ts => !lbTileSetList.IsTileSetFavourite(ts.Index));

            switch (TileSetSortMode)
            {
                case TileSetSortMode.ID:
                    sortedTileSets = sortedTileSets.ThenBy(ts => ts.Index);
                    break;
                case TileSetSortMode.Name:
                    sortedTileSets = sortedTileSets.ThenBy(ts => ts.SetName);
                    break;
            }

            foreach (TileSet tileSet in sortedTileSets)
            {
                if (tileSet.NonMarbleMadness > -1)
                    continue;

                if (tileSet.AllowToPlace && tileSet.LoadedTileCount > 0)
                {
                    var displayName = tileSet.SetName;
                    if (IsChinese)
                    {
                        displayName = GetTileSetChineseName(tileSet.SetName);
                    }
                    
                    lbTileSetList.AddItem(new XNAListBoxItem()
                    {
                        Text = displayName,
                        Tag = tileSet,
                        TextColor = tileSet.Color.HasValue ? tileSet.Color.Value : UISettings.ActiveSettings.AltColor
                    });

                    if (tileSet == TileDisplay.TileSet)
                    {
                        lbTileSetList.SelectedIndexChanged -= LbTileSetList_SelectedIndexChanged;
                        lbTileSetList.SelectedIndex = lbTileSetList.Items.Count;
                        lbTileSetList.SelectedIndexChanged += LbTileSetList_SelectedIndexChanged;
                    }
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            FillRectangle(new Rectangle(0, 0, Width, ResizeDragThreshold), new Color(0, 0, 0, 64));
            DrawChildren(gameTime);
        }

        // 获取地形集的中文翻译
        private string GetTileSetChineseName(string englishName)
        {
            // 优先从INI文件中获取翻译
            if (TileSetNameManager.TryGetTileSetNameTranslation(englishName, out var iniTranslation))
            {
                return iniTranslation;
            }
            
            // 如果INI文件中没有，则使用硬编码字典
            if (TileSetChineseNameMap.TryGetValue(englishName, out var hardcodedTranslation))
            {
                return hardcodedTranslation;
            }
            
            // 如果都没有找到翻译，返回原始英文名称
            return englishName;
        }
    }
}
