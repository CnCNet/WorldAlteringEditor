using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI.CursorActions;
using TSMapEditor.Translations;

namespace TSMapEditor.UI.Sidebar
{
    public class TerrainObjectListPanel : XNAPanel, ISearchBoxContainer
    {
        public TerrainObjectListPanel(WindowManager windowManager, EditorState editorState,
            Map map, TheaterGraphics theaterGraphics, ICursorActionTarget cursorActionTarget) : base(windowManager)
        {
            EditorState = editorState;
            Map = map;
            TheaterGraphics = theaterGraphics;
            this.cursorActionTarget = cursorActionTarget;
        }

        protected EditorState EditorState { get; }
        protected Map Map { get; }
        protected TheaterGraphics TheaterGraphics { get; }

        public XNASuggestionTextBox SearchBox { get; private set; }
        public TreeView ObjectTreeView { get; private set; }

        private readonly ICursorActionTarget cursorActionTarget;

        private TerrainObjectPlacementAction terrainObjectPlacementAction;

        private TerrainObjectCollectionPlacementAction terrainObjectCollectionPlacementAction;

        private bool IsChinese => TSMapEditor.UI.MainMenu.IsChinese;

        // 地形物体中英文对照字典 - 作为备用，优先使用INI文件中的翻译
        private static readonly Dictionary<string, string> TerrainObjectChineseNameMap = new Dictionary<string, string>
        {
        

            // 用户提供的翻译
            { "Deciduous Trees", "阔叶林" },
            { "Conifer Trees", "针叶林" },
            { "Cacti", "仙人掌" },
            { "Alrington Stones", "阿灵顿石" },
            { "Bush", "灌木" },
            { "Cactus", "仙人掌" },
            { "Cherry Tree", "樱花树" },
            { "Cylinder", "圆柱体" },
            { "Dark Tree", "深色树" },
            { "Flower Tree", "花树" },
            { "Green Tree", "绿树" },
            { "Hedge", "树篱" },
            { "Lightpost A", "路灯A" },
            { "Lightpost B", "路灯B" },
            { "Lightpost C", "路灯C" },
            { "Lightpost D", "路灯D" },
            { "Lightpost Euro A", "欧式路灯A" },
            { "Lightpost Euro B", "欧式路灯B" },
            { "Lightpost Signed A", "标志路灯A" },
            { "Lightpost Signed B", "标志路灯B" },
            { "Lightpost Signed C", "标志路灯C" },
            { "Lightpost Signed D", "标志路灯D" },
            { "Neon Tree", "霓虹树" },
            { "Palm Tree", "棕榈树" },
            { "Ramp Shrub", "坡地灌木" },
            { "Spruce Tree", "云杉" },
            { "Street Sign A", "街道标志A" },
            { "Street Sign B", "街道标志B" },
            { "Street Sign C", "街道标志C" },
            { "Street Sign D", "街道标志D" },
            { "Street Sign E", "街道标志E" },
            { "Street Sign F", "街道标志F" },
            { "Tiberium Tree", "泰伯利亚树" },
            { "Traffic Light A", "红绿灯A" },
            { "Traffic Light B", "红绿灯B" },
            { "Traffic Light C", "红绿灯C" },
            { "Traffic Light D", "红绿灯D" },
            { "Tunnel Wall Fill A", "隧道墙A" },
            { "Tunnel Wall Fill B", "隧道墙B" },
            { "Urban Tree", "城市树" },
            { "Utility Pole A", "电线杆A" },
            { "Utility Pole B", "电线杆B" },
            { "White Tree", "白树" },
        };

        public void RefreshLanguage(bool isChinese)
        {
            foreach (var category in ObjectTreeView.Categories)
            {
                switch (category.Key)
                {
                    case "collections":
                        category.Text = isChinese ? "集合" : "Collections";
                        break;
                    case "uncategorized":
                        category.Text = isChinese ? "未分类" : "Uncategorized";
                        break;
                    default:
                        break;
                }
                category.DisplayName = category.Text;

                // 刷新每个节点的显示名
                foreach (var node in category.Nodes)
                {
                    if (node.Tag is TerrainObjectCollection collection)
                    {
                        string displayName = collection.Name;
                        if (isChinese)
                        {
                            // 优先从INI文件中获取翻译
                            if (TerrainObjectNameManager.TryGetTerrainObjectCollectionTranslation(collection.Name, out var iniTranslation))
                            {
                                displayName = iniTranslation;
                            }
                            // 如果INI文件中没有，则使用硬编码字典
                            else if (TerrainObjectChineseNameMap.TryGetValue(collection.Name, out var zhName))
                            {
                                displayName = zhName;
                            }
                        }
                        node.DisplayName = displayName;
                        node.Text = displayName;
                    }
                    else if (node.Tag is TerrainType terrainType)
                    {
                        string displayName = terrainType.GetEditorDisplayName();
                        if (isChinese)
                        {
                            // 优先从INI文件中获取翻译
                            if (TerrainObjectNameManager.TryGetTerrainObjectTranslation(terrainType.GetEditorDisplayName(), out var iniTranslation))
                            {
                                displayName = iniTranslation;
                            }
                            // 如果INI文件中没有，则使用硬编码字典
                            else if (TerrainObjectChineseNameMap.TryGetValue(terrainType.GetEditorDisplayName(), out var zhName))
                            {
                                displayName = zhName;
                            }
                        }
                        node.DisplayName = displayName + " (" + terrainType.ININame + ")";
                        node.Text = displayName + " (" + terrainType.ININame + ")";
                    }
                }
            }
            ObjectTreeView.RefreshScrollbar();
          
        }

        public override void Initialize()
        {
            SearchBox = new XNASuggestionTextBox(WindowManager);
            SearchBox.Name = nameof(SearchBox);
            SearchBox.X = Constants.UIEmptySideSpace;
            SearchBox.Y = Constants.UIEmptyTopSpace;
            SearchBox.Width = Width - Constants.UIEmptySideSpace * 2;
            SearchBox.Height = Constants.UITextBoxHeight;
            SearchBox.Suggestion = "Search object... (CTRL + F)";
            AddChild(SearchBox);
            SearchBox.TextChanged += SearchBox_TextChanged;
            SearchBox.EnterPressed += SearchBox_EnterPressed;
            UIHelpers.AddSearchTipsBoxToControl(SearchBox);

            ObjectTreeView = new TreeView(WindowManager);
            ObjectTreeView.Name = nameof(ObjectTreeView);
            ObjectTreeView.Y = SearchBox.Bottom + Constants.UIVerticalSpacing;
            ObjectTreeView.Height = Height - ObjectTreeView.Y;
            ObjectTreeView.Width = Width;
            AddChild(ObjectTreeView);
            ObjectTreeView.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 222), 2, 2);

            base.Initialize();

            terrainObjectPlacementAction = new TerrainObjectPlacementAction(cursorActionTarget);
            terrainObjectCollectionPlacementAction = new TerrainObjectCollectionPlacementAction(cursorActionTarget);
            ObjectTreeView.SelectedItemChanged += ObjectTreeView_SelectedItemChanged;
            terrainObjectCollectionPlacementAction.ActionExited += (s, e) => ObjectTreeView.SelectedNode = null;
            terrainObjectPlacementAction.ActionExited += (s, e) => ObjectTreeView.SelectedNode = null;

            InitTerrainObjects();

            KeyboardCommands.Instance.NextSidebarNode.Triggered += NextSidebarNode_Triggered;
            KeyboardCommands.Instance.PreviousSidebarNode.Triggered += PreviousSidebarNode_Triggered;
        }

        public override void RefreshSize()
        {
            Width = Parent.Width;
            SearchBox.Width = Width - Constants.UIEmptySideSpace * 2;
            ObjectTreeView.Width = Width;
            Height = Parent.Height - Y;
            ObjectTreeView.Height = Height - ObjectTreeView.Y;
        }

        private void NextSidebarNode_Triggered(object sender, EventArgs e)
        {
            if (Enabled)
                ObjectTreeView.SelectNextNode();
        }

        private void PreviousSidebarNode_Triggered(object sender, EventArgs e)
        {
            if (Enabled)
                ObjectTreeView.SelectPreviousNode();
        }

        private void ObjectTreeView_SelectedItemChanged(object sender, EventArgs e)
        {
            if (ObjectTreeView.SelectedNode == null)
                return;

            var tag = ObjectTreeView.SelectedNode.Tag;
            if (tag == null)
                return;

            if (tag is TerrainObjectCollection collection)
            {
                terrainObjectCollectionPlacementAction.TerrainObjectCollection = collection;
                EditorState.CursorAction = terrainObjectCollectionPlacementAction;
            }
            else if (tag is TerrainType terrainType)
            {
                terrainObjectPlacementAction.TerrainType = terrainType;
                EditorState.CursorAction = terrainObjectPlacementAction;
            }
        }

        private void SearchBox_EnterPressed(object sender, EventArgs e)
        {
            ObjectTreeView.FindNode(SearchBox.Text, true);
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text) || SearchBox.Text == SearchBox.Suggestion)
                return;

            ObjectTreeView.FindNode(SearchBox.Text, false);
        }

        private Texture2D GetSidebarTextureForTerrainType(TerrainType terrainType, RenderTarget2D renderTarget)
        {
            Texture2D fullSizeRGBATexture = null;

            var textures = TheaterGraphics.TerrainObjectTextures[terrainType.Index];
            if (textures != null)
            {
                const int frameIndex = 0;

                if (textures.GetFrameCount() > frameIndex)
                {
                    var frame = textures.GetFrame(frameIndex);
                    if (frame != null)
                        fullSizeRGBATexture = textures.GetTextureForFrame_RGBA(frameIndex);
                }
            }

            Texture2D finalTexture = null;
            if (fullSizeRGBATexture != null)
            {
                // Render a smaller version of the full-size texture to save VRAM
                finalTexture = Helpers.RenderTextureAsSmaller(fullSizeRGBATexture, renderTarget, GraphicsDevice);
                fullSizeRGBATexture.Dispose();
            }

            return finalTexture;
        }

        private void InitTerrainObjects()
        {
            var categories = new List<TreeViewCategory>();

            var renderTarget = new RenderTarget2D(GraphicsDevice, ObjectTreeView.Width, ObjectTreeView.LineHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

            if (Map.EditorConfig.TerrainObjectCollections.Count > 0)
            {
                var collectionsCategory = new TreeViewCategory() { Key = "collections", Text = IsChinese ? "集合" : "Collections" };
                categories.Add(collectionsCategory);

                foreach (var collection in Map.EditorConfig.TerrainObjectCollections)
                {
                    if (collection.Entries.Length == 0)
                        continue;

                    if (!collection.IsValidForTheater(Map.LoadedTheaterName))
                        continue;

                    var firstEntry = collection.Entries[0];

                    // 处理集合名称的翻译
                    string displayName = collection.Name;
                    if (IsChinese)
                    {
                        // 优先从INI文件中获取翻译
                        if (TerrainObjectNameManager.TryGetTerrainObjectCollectionTranslation(collection.Name, out var iniTranslation))
                        {
                            displayName = iniTranslation;
                        }
                        // 如果INI文件中没有，则使用硬编码字典
                        else if (TerrainObjectChineseNameMap.TryGetValue(collection.Name, out var zhName))
                        {
                            displayName = zhName;
                        }
                    }

                    collectionsCategory.Nodes.Add(new TreeViewNode()
                    {
                        Text = displayName,
                        DisplayName = displayName,
                        Tag = collection,
                        Texture = GetSidebarTextureForTerrainType(firstEntry.TerrainType, renderTarget)
                    });
                }
            }

            for (int i = 0; i < Map.Rules.TerrainTypes.Count; i++)
            {
                TreeViewCategory category = null;
                TerrainType terrainType = Map.Rules.TerrainTypes[i];

                if (!terrainType.EditorVisible)
                    continue;

                if (!terrainType.IsValidForTheater(Map.LoadedTheaterName))
                    continue;

                if (string.IsNullOrEmpty(terrainType.EditorCategory))
                {
                    category = FindOrMakeCategory("uncategorized", categories, IsChinese);
                }
                else
                {
                    category = FindOrMakeCategory(terrainType.EditorCategory.ToLower(), categories, IsChinese);
                }

                // 处理地形对象名称的翻译
                string displayName = terrainType.GetEditorDisplayName();
                if (IsChinese)
                {
                    // 优先从INI文件中获取翻译
                    if (TerrainObjectNameManager.TryGetTerrainObjectTranslation(terrainType.GetEditorDisplayName(), out var iniTranslation))
                    {
                        displayName = iniTranslation;
                    }
                    // 如果INI文件中没有，则使用硬编码字典
                    else if (TerrainObjectChineseNameMap.TryGetValue(terrainType.GetEditorDisplayName(), out var zhName))
                    {
                        displayName = zhName;
                    }
                }

                category.Nodes.Add(new TreeViewNode()
                {
                    Text = displayName + " (" + terrainType.ININame + ")",
                    DisplayName = displayName + " (" + terrainType.ININame + ")",
                    Texture = GetSidebarTextureForTerrainType(terrainType, renderTarget),
                    Tag = terrainType
                });

                category.Nodes = category.Nodes.OrderBy(n => n.Text).ToList();
            }

            renderTarget.Dispose();

            categories.ForEach(ObjectTreeView.AddCategory);
        }

        private TreeViewCategory FindOrMakeCategory(string key, List<TreeViewCategory> categoryList, bool isChinese)
        {
            var category = categoryList.Find(c => c.Key == key);
            if (category != null)
                return category;
            string text = key;
            switch (key)
            {
                case "collections": text = isChinese ? "集合" : "Collections"; break;
                case "uncategorized": text = isChinese ? "未分类" : "Uncategorized"; break;
                default: break;
            }
            category = new TreeViewCategory() { Key = key, Text = text };
            categoryList.Add(category);
            return category;
        }
    }
}
