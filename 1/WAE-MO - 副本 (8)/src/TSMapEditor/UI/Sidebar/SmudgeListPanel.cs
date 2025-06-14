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

namespace TSMapEditor.UI.Sidebar
{
    class SmudgeListPanel : XNAPanel, ISearchBoxContainer
    {
        public SmudgeListPanel(WindowManager windowManager, EditorState editorState,
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
        private PlaceSmudgeCursorAction smudgePlacementAction;
        private PlaceSmudgeCollectionCursorAction smudgeCollectionPlacementAction;

        private bool IsChinese => TSMapEditor.UI.MainMenu.IsChinese;
        
        // 污点集合中英文对照字典
        private static readonly Dictionary<string, string> SmudgeCollectionChineseNameMap = new Dictionary<string, string>
        {
            { "Random Crater", "随机弹坑" },
            { "Random 1×1 Crater", "随机1×1弹坑" },
            { "Random 2×2 Crater", "随机2×2弹坑" },
            { "Random Burn", "随机燃烧" }
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
                    case "erase smudges":
                        category.Text = isChinese ? "擦除污点" : "Erase Smudges";
                        break;
                    default:
                        break;
                }
                category.DisplayName = category.Text;
                
                // 刷新每个节点的显示名
                foreach (var node in category.Nodes)
                {
                    if (node.Tag is SmudgeCollection collection)
                    {
                        var displayName = collection.Name;
                        if (isChinese && SmudgeCollectionChineseNameMap.TryGetValue(collection.Name, out var zhName))
                        {
                            displayName = zhName;
                        }
                        node.DisplayName = displayName;
                        node.Text = displayName;
                    }
                    else if (node.Tag is SmudgeType smudgeType)
                    {
                        node.DisplayName = isChinese ? GetSmudgeDisplayName(smudgeType.ININame) : smudgeType.Name;
                        // 保持Text不变，因为它被用于搜索
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
            SearchBox.Suggestion = "Search smudge... (CTRL + F)";
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

            ObjectTreeView.SelectedItemChanged += ObjectTreeView_SelectedItemChanged;
            smudgePlacementAction = new PlaceSmudgeCursorAction(cursorActionTarget);
            smudgeCollectionPlacementAction = new PlaceSmudgeCollectionCursorAction(cursorActionTarget);
            smudgeCollectionPlacementAction.ActionExited += (s, e) => ObjectTreeView.SelectedNode = null;
            smudgePlacementAction.ActionExited += (s, e) => ObjectTreeView.SelectedNode = null;

            InitSmudges();

            KeyboardCommands.Instance.NextSidebarNode.Triggered += NextSidebarNode_Triggered;
            KeyboardCommands.Instance.PreviousSidebarNode.Triggered += PreviousSidebarNode_Triggered;
        }

        private void ObjectTreeView_SelectedItemChanged(object sender, EventArgs e)
        {
            if (ObjectTreeView.SelectedNode == null)
                return;

            var tag = ObjectTreeView.SelectedNode.Tag;
            if (tag == null)
                return;

            if (tag is SmudgeCollection collection)
            {
                smudgeCollectionPlacementAction.SmudgeCollection = collection;
                EditorState.CursorAction = smudgeCollectionPlacementAction;
            }
            else if (tag is SmudgeType smudgeType)
            {
                smudgePlacementAction.SmudgeType = smudgeType;
                EditorState.CursorAction = smudgePlacementAction;
            }
            else
            {
                // Assume this to be the smudge removal entry
                smudgePlacementAction.SmudgeType = null;
                EditorState.CursorAction = smudgePlacementAction;
            }
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

        private Texture2D GetSidebarTextureForSmudge(SmudgeType smudgeType, RenderTarget2D renderTarget)
        {
            Texture2D fullSizeRGBATexture = null;

            var textures = TheaterGraphics.SmudgeTextures[smudgeType.Index];
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

        private void InitSmudges()
        {
            var renderTarget = new RenderTarget2D(GraphicsDevice, ObjectTreeView.Width, ObjectTreeView.LineHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

            var categories = new List<TreeViewCategory>();

            categories.Add(new TreeViewCategory()
            {
                Key = "erase smudges",
                Text = IsChinese ? "擦除污点" : "Erase Smudges",
                Tag = new object()
            });

            if (Map.EditorConfig.SmudgeCollections.Count > 0)
            {
                var collectionsCategory = new TreeViewCategory() { Key = "collections", Text = IsChinese ? "集合" : "Collections" };
                categories.Add(collectionsCategory);

                foreach (var collection in Map.EditorConfig.SmudgeCollections)
                {
                    if (collection.Entries.Length == 0)
                        continue;

                    if (!collection.IsValidForTheater(Map.LoadedTheaterName))
                        continue;

                    var firstEntry = collection.Entries[0];

                    var displayName = collection.Name;
                    if (IsChinese && SmudgeCollectionChineseNameMap.TryGetValue(collection.Name, out var zhName))
                    {
                        displayName = zhName;
                    }

                    collectionsCategory.Nodes.Add(new TreeViewNode()
                    {
                        Text = displayName,
                        DisplayName = displayName,
                        Tag = collection,
                        Texture = GetSidebarTextureForSmudge(firstEntry.SmudgeType, renderTarget)
                    });
                }
            }

            for (int i = 0; i < Map.Rules.SmudgeTypes.Count; i++)
            {
                TreeViewCategory category = null;
                SmudgeType smudgeType = Map.Rules.SmudgeTypes[i];

                if (!smudgeType.EditorVisible)
                    continue;

                if (!smudgeType.IsValidForTheater(Map.LoadedTheaterName))
                    continue;

                if (string.IsNullOrEmpty(smudgeType.EditorCategory))
                {
                    category = FindOrMakeCategory("uncategorized", categories, IsChinese);
                }
                else
                {
                    category = FindOrMakeCategory(smudgeType.EditorCategory.ToLower(), categories, IsChinese);
                }

                category.Nodes.Add(new TreeViewNode()
                {
                    Text = smudgeType.ININame,
                    DisplayName = IsChinese ? GetSmudgeDisplayName(smudgeType.ININame) : smudgeType.Name,
                    Texture = GetSidebarTextureForSmudge(smudgeType, renderTarget),
                    Tag = smudgeType
                });

                category.Nodes = category.Nodes.OrderBy(n => n.Text).ToList();
            }

            categories.ForEach(ObjectTreeView.AddCategory);

            renderTarget.Dispose();
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
                case "erase smudges": text = isChinese ? "擦除污点" : "Erase Smudges"; break;
                default: break;
            }
            category = new TreeViewCategory() { Key = key, Text = text };
            categoryList.Add(category);
            return category;
        }

        private string GetSmudgeDisplayName(string iniName)
        {
            // 这里可以用字典查表，演示用简单switch
            switch (iniName)
            {
                // 弹坑系列
                case "CRATER01":
                case "CRATER02":
                case "CRATER03":
                case "CRATER04":
                case "CRATER05":
                case "CRATER06":
                case "CRATER07":
                case "CRATER08":
                case "CRATER09":
                case "CRATER10":
                case "CRATER11":
                case "CRATER12":
                    return "弹坑" + iniName.Substring(6);
                
                // 燃烧痕迹系列
                case "BURN01":
                case "BURN02":
                case "BURN03":
                case "BURN04":
                case "BURN05":
                case "BURN06":
                case "BURN07":
                case "BURN08":
                case "BURN09":
                case "BURN10":
                case "BURN11":
                case "BURN12":
                    return "燃烧痕迹" + iniName.Substring(4);
                
                // 灼烧类
                case "SCORCH":
                    return "灼烧";
                
                // 其他
                default:
                    return iniName;
            }
        }
    }
}
