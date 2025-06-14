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
    public class OverlayListPanel : XNAPanel, ISearchBoxContainer
    {
        public OverlayListPanel(WindowManager windowManager, EditorState editorState,
            Map map, TheaterGraphics theaterGraphics, ICursorActionTarget cursorActionTarget,
            OverlayPlacementAction overlayPlacementAction) : base(windowManager)
        {
            EditorState = editorState;
            Map = map;
            TheaterGraphics = theaterGraphics;
            this.cursorActionTarget = cursorActionTarget;
            this.overlayPlacementAction = overlayPlacementAction;
        }


        protected EditorState EditorState { get; }
        protected Map Map { get; }
        protected TheaterGraphics TheaterGraphics { get; }

        public XNASuggestionTextBox SearchBox { get; private set; }
        public TreeView ObjectTreeView { get; private set; }

        private readonly ICursorActionTarget cursorActionTarget;
        private readonly OverlayPlacementAction overlayPlacementAction;

        private OverlayCollectionPlacementAction overlayCollectionPlacementAction;
        private ConnectedOverlayPlacementAction connectedOverlayPlacementAction;

        private bool IsChinese => TSMapEditor.UI.MainMenu.IsChinese;
        
        // 连接型覆盖物中英文对照字典
        private static readonly Dictionary<string, string> ConnectedOverlayChineseNameMap = new Dictionary<string, string>
        {
            { "Ore", "矿石" },
            { "Gems", "宝石" },
            { "Clear Rocks", "碎石" },
            { "Sand Rocks", "沙石" },
            { "Allied Wall", "盟军墙" },
            { "Allied Wall (Damaged)", "盟军墙（受损）" },
            { "Allied Wall (Very Damaged)", "盟军墙（重度受损）" },
            { "Soviet Wall", "苏联墙" },
            { "Soviet Wall (Damaged)", "苏联墙（受损）" },
            { "Soviet Wall (Very Damaged)", "苏联墙（重度受损）" },
            { "Yuri Citadel Wall", "尤里城墙" },
            { "Yuri Citadel Wall (Damaged)", "尤里城墙（受损）" },
            { "Yuri Citadel Wall (Very Damaged)", "尤里城墙（重度受损）" },
            { "Sandbags", "沙袋" },
            { "Sandbags (Damaged)", "沙袋（受损）" },
            { "Black Fence", "黑色围栏" },
            { "Black Fence (Damaged)", "黑色围栏（受损）" },
            { "White Fence", "白色围栏" },
            { "White Fence (Damaged)", "白色围栏（受损）" },
            { "Prison Camp Fence", "战俘营围栏" },
            { "Prison Camp Fence (Damaged)", "战俘营围栏（受损）" },
            { "Kremlin Wall", "克里姆林宫墙" },
            { "Kremlin Wall (Damaged)", "克里姆林宫墙（受损）" },
            { "Kremlin Wall (Very Damaged)", "克里姆林宫墙（重度受损）" },
        };

        // 覆盖物集合中英文对照字典
        private static readonly Dictionary<string, string> OverlayCollectionChineseNameMap = new Dictionary<string, string>
        {
            { "Ore", "矿石" },
            { "Gems", "宝石" },
            { "Clear Rocks", "碎石" },
            { "Sand Rocks", "沙石" }
        };
        
        public static Dictionary<string, string> GetConnectedOverlayChineseNameMap()
        {
            return ConnectedOverlayChineseNameMap;
        }

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
                    case "erase overlay":
                        category.Text = isChinese ? "擦除地表覆盖" : "Erase Overlay";
                        break;
                    case "connected overlays":
                        category.Text = isChinese ? "连接型覆盖" : "Connected Overlays";
                        break;
                    default:
                        break;
                }
                category.DisplayName = category.Text;
                
                // 刷新每个节点的显示名
                foreach (var node in category.Nodes)
                {
                    if (node.Tag is ConnectedOverlayType connectedOverlay)
                    {
                        var displayName = connectedOverlay.UIName;
                        if (isChinese)
                        {
                            // 优先从INI文件中获取翻译
                            if (OverlayNameManager.TryGetConnectedOverlayTranslation(connectedOverlay.UIName, out var iniTranslation))
                            {
                                displayName = iniTranslation;
                            }
                            // 如果INI文件中没有，则使用硬编码字典
                            else if (ConnectedOverlayChineseNameMap.TryGetValue(connectedOverlay.UIName, out var zhName))
                            {
                                displayName = zhName;
                            }
                        }
                        node.DisplayName = displayName;
                        node.Text = displayName;
                    }
                    else if (node.Tag is OverlayType overlayType)
                    {
                        node.DisplayName = isChinese ? GetOverlayDisplayName(overlayType.ININame) : overlayType.Name;
                        // 保持Text不变，因为它被用于搜索
                    }
                    else if (node.Tag is OverlayCollection collection)
                    {
                        var displayName = collection.Name;
                        if (isChinese)
                        {
                            // 优先从INI文件中获取翻译
                            if (OverlayNameManager.TryGetOverlayCollectionTranslation(collection.Name, out var iniTranslation))
                            {
                                displayName = iniTranslation;
                            }
                            // 如果INI文件中没有，则使用硬编码字典
                            else if (OverlayCollectionChineseNameMap.TryGetValue(collection.Name, out var zhName))
                            {
                                displayName = zhName;
                            }
                        }
                        node.DisplayName = displayName;
                        node.Text = displayName;
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
            SearchBox.Suggestion = "Search overlay... (CTRL + F)";
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

            overlayCollectionPlacementAction = new OverlayCollectionPlacementAction(cursorActionTarget);
            connectedOverlayPlacementAction = new ConnectedOverlayPlacementAction(cursorActionTarget);
            ObjectTreeView.SelectedItemChanged += ObjectTreeView_SelectedItemChanged;
            overlayCollectionPlacementAction.ActionExited += (s, e) => ObjectTreeView.SelectedNode = null;
            connectedOverlayPlacementAction.ActionExited += (s, e) => ObjectTreeView.SelectedNode = null;
            overlayPlacementAction.ActionExited += (s, e) => ObjectTreeView.SelectedNode = null;

            InitOverlays();

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

            if (tag is OverlayCollection collection)
            {
                overlayCollectionPlacementAction.OverlayCollection = collection;
                EditorState.CursorAction = overlayCollectionPlacementAction;
            }
            else if (tag is ConnectedOverlayType connectedOverlay)
            {
                connectedOverlayPlacementAction.ConnectedOverlayType = connectedOverlay;
                EditorState.CursorAction = connectedOverlayPlacementAction;
            }
            else if (tag is OverlayType overlayType)
            {
                overlayPlacementAction.OverlayType = overlayType;
                EditorState.CursorAction = overlayPlacementAction;
            }
            else
            {
                // Assume this to be the overlay removal entry
                overlayPlacementAction.OverlayType = null;
                EditorState.CursorAction = overlayPlacementAction;
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

        private Texture2D GetSidebarTextureForOverlay(OverlayType overlayType, RenderTarget2D renderTarget, int defaultFrameNumber = 0, bool remap = false)
        {
            Texture2D fullSizeRGBATexture = null;

            var textures = TheaterGraphics.OverlayTextures[overlayType.Index];
            if (textures != null)
            {
                int frameCount = textures.GetFrameCount();
                int overlayFrameNumber = defaultFrameNumber;
                if (overlayType.Tiberium)
                    overlayFrameNumber = (frameCount / 2) - 1;

                if (frameCount > overlayFrameNumber)
                {
                    var frame = textures.GetFrame(overlayFrameNumber);
                    if (frame != null)
                    {
                        fullSizeRGBATexture = remap ? textures.GetRemapTextureForFrame_RGBA(overlayFrameNumber) :
                            textures.GetTextureForFrame_RGBA(overlayFrameNumber);
                    }
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

        private void InitOverlays()
        {
            var renderTarget = new RenderTarget2D(GraphicsDevice, ObjectTreeView.Width, ObjectTreeView.LineHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

            var categories = new List<TreeViewCategory>();

            categories.Add(new TreeViewCategory()
            {
                Key = "erase overlay",
                Text = IsChinese ? "擦除地表覆盖" : "Erase Overlay",
                Tag = new object()
            });

            if (Map.EditorConfig.OverlayCollections.Count > 0)
            {
                var collectionsCategory = new TreeViewCategory() { Key = "collections", Text = IsChinese ? "集合" : "Collections" };
                categories.Add(collectionsCategory);

                foreach (var collection in Map.EditorConfig.OverlayCollections)
                {
                    if (collection.Entries.Length == 0)
                        continue;

                    if (!collection.IsValidForTheater(Map.LoadedTheaterName))
                        continue;

                    var firstEntry = collection.Entries[0];

                    Texture2D remapTexture = null;
                    Color remapColor = Color.White;
                    if (firstEntry.OverlayType.Tiberium)
                    {
                        remapTexture = GetSidebarTextureForOverlay(firstEntry.OverlayType, renderTarget, firstEntry.Frame, remap: true);

                        if (firstEntry.OverlayType.TiberiumType != null)
                            remapColor = firstEntry.OverlayType.TiberiumType.XNAColor;
                    }

                    var displayName = collection.Name;
                    if (IsChinese && OverlayCollectionChineseNameMap.TryGetValue(collection.Name, out var zhName))
                    {
                        displayName = zhName;
                    }

                    collectionsCategory.Nodes.Add(new TreeViewNode()
                    {
                        Text = displayName,
                        DisplayName = displayName,
                        Tag = collection,
                        Texture = GetSidebarTextureForOverlay(firstEntry.OverlayType, renderTarget, firstEntry.Frame),
                        RemapTexture = remapTexture,
                        RemapColor = remapColor
                    });
                }
            }

            if (Map.EditorConfig.ConnectedOverlays.Count > 0)
            {
                var connectedOverlaysCategory = new TreeViewCategory() { Key = "connected overlays", Text = IsChinese ? "连接型覆盖" : "Connected Overlays" };
                categories.Add(connectedOverlaysCategory);

                foreach (var connectedOverlay in Map.EditorConfig.ConnectedOverlays)
                {
                    if (connectedOverlay.FrameCount == 0)
                        continue;

                    if (!connectedOverlay.Frames.TrueForAll(cof => cof.OverlayType.IsValidForTheater(Map.LoadedTheaterName)))
                        continue;

                    var firstEntry = connectedOverlay.Frames[0];

                    var displayName = connectedOverlay.UIName;
                    if (IsChinese && ConnectedOverlayChineseNameMap.TryGetValue(connectedOverlay.UIName, out var zhName))
                    {
                        displayName = zhName;
                    }
                    
                    connectedOverlaysCategory.Nodes.Add(new TreeViewNode()
                    {
                        Text = displayName,
                        DisplayName = displayName,
                        Tag = connectedOverlay,
                        Texture = GetSidebarTextureForOverlay(firstEntry.OverlayType, renderTarget, firstEntry.FrameIndex),
                    });
                }
            }

            for (int i = 0; i < Map.Rules.OverlayTypes.Count; i++)
            {
                TreeViewCategory category = null;
                OverlayType overlayType = Map.Rules.OverlayTypes[i];

                if (!overlayType.EditorVisible)
                    continue;

                if (!overlayType.IsValidForTheater(Map.LoadedTheaterName))
                    continue;

                if (string.IsNullOrEmpty(overlayType.EditorCategory))
                {
                    category = FindOrMakeCategory("uncategorized", categories, IsChinese);
                }
                else
                {
                    category = FindOrMakeCategory(overlayType.EditorCategory.ToLower(), categories, IsChinese);
                }

                int frameNumber = 0;
                var overlayImage = TheaterGraphics.OverlayTextures[i];
                if (overlayImage != null)
                {
                    int frameCount = overlayImage.GetFrameCount();
                    // Find the first valid frame and use that as our texture
                    for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                    {
                        var frame = overlayImage.GetFrame(frameIndex);
                        if (frame != null)
                        {
                            frameNumber = frameIndex;
                            break;
                        }
                    }
                }

                Texture2D remapTexture = null;
                Color remapColor = Color.White;
                if (overlayType.Tiberium)
                {
                    remapTexture = GetSidebarTextureForOverlay(overlayType, renderTarget, frameNumber, remap: true);

                    if (overlayType.TiberiumType != null)
                        remapColor = overlayType.TiberiumType.XNAColor;
                }

                category.Nodes.Add(new TreeViewNode()
                {
                    Text = overlayType.ININame,
                    DisplayName = IsChinese ? GetOverlayDisplayName(overlayType.ININame) : overlayType.Name,
                    Texture = GetSidebarTextureForOverlay(overlayType, renderTarget, frameNumber),
                    Tag = overlayType,
                    RemapTexture = remapTexture,
                    RemapColor = remapColor
                });

                category.Nodes = category.Nodes.OrderBy(n => n.Text).ToList();
            }

            categories.ForEach(c => ObjectTreeView.AddCategory(c));

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
                case "erase overlay": text = isChinese ? "擦除地表覆盖" : "Erase Overlay"; break;
                case "connected overlays": text = isChinese ? "连接型覆盖" : "Connected Overlays"; break;
                default: break;
            }
            category = new TreeViewCategory() { Key = key, Text = text };
            categoryList.Add(category);
            return category;
        }

        private string GetOverlayDisplayName(string iniName)
        {
            // 优先从INI文件中获取翻译
            if (OverlayNameManager.TryGetOverlayTranslation(iniName, out var translation))
            {
                return translation;
            }
            
            // 如果INI文件中没有找到翻译，则使用硬编码的翻译
            switch (iniName)
            {
                // TIB系列全部翻译为矿石
                case "TIB01":
                case "TIB02":
                case "TIB03":
                case "TIB04":
                case "TIB05":
                case "TIB06":
                case "TIB07":
                case "TIB08":
                case "TIB09":
                case "TIB10":
                case "TIB11":
                case "TIB12":
                case "TIB13":
                case "TIB14":
                case "TIB15":
                case "TIB16":
                case "TIB17":
                case "TIB18":
                case "TIB19":
                case "TIB20":
                    return "矿石" + iniName.Substring(3);
                
                // TIB3系列也翻译为矿石
                case "TIB3_01":
                case "TIB3_02":
                case "TIB3_03":
                case "TIB3_04":
                case "TIB3_05":
                case "TIB3_06":
                case "TIB3_07":
                case "TIB3_08":
                case "TIB3_09":
                case "TIB3_10":
                case "TIB3_11":
                case "TIB3_12":
                case "TIB3_13":
                case "TIB3_14":
                case "TIB3_15":
                case "TIB3_16":
                case "TIB3_17":
                case "TIB3_18":
                case "TIB3_19":
                case "TIB3_20":
                    return "矿石" + iniName.Substring(3);

                // GEM系列翻译为宝石
                case "GEM01":
                case "GEM02":
                case "GEM03":
                case "GEM04":
                case "GEM05":
                case "GEM06":
                case "GEM07":
                case "GEM08":
                case "GEM09":
                case "GEM10":
                case "GEM11":
                case "GEM12":
                    return "宝石" + iniName.Substring(3);

                // 沙石和岩石
                case "SROCK01":
                case "SROCK02":
                case "SROCK03":
                case "SROCK04":
                case "SROCK05":
                    return "沙石" + iniName.Substring(5);
                case "TROCK01":
                case "TROCK02":
                case "TROCK03":
                case "TROCK04":
                case "TROCK05":
                    return "岩石" + iniName.Substring(5);
                    
                // 建筑物和墙
                case "GASAND": return "沙袋";
                case "CABARB": return "战俘营围栏";
                case "GAWALL": return "盟军墙";
                case "NAWALL": return "苏联墙";
                case "CASWLL": return "石墙";
                case "CABRFC": return "黑色围栏";
                case "FARWAL": return "远东墙";
                case "CBLACK": return "黑色围栏";
                case "CBLACK02": return "黑色围栏（替代版）";
                case "GAFWLL": return "消防墙";
                case "CAWALL": return "石墙";
                case "FAWALL": return "远东墙";
                case "CAKRMW": return "克里姆林宫墙";
                
                // 桥梁
                case "BRIDGE1": return "桥梁1";
                case "BRIDGE2": return "桥梁2";
                case "BRIDGEB1": return "桥梁B1";
                case "BRIDGEB2": return "桥梁B2";
                
                // 低桥系列
                case "LOBRDG01":
                case "LOBRDG02":
                case "LOBRDG03":
                case "LOBRDG04":
                case "LOBRDG05":
                case "LOBRDG06":
                case "LOBRDG07":
                case "LOBRDG08":
                case "LOBRDG09":
                case "LOBRDG10":
                case "LOBRDG11":
                case "LOBRDG12":
                case "LOBRDG13":
                case "LOBRDG14":
                case "LOBRDG15":
                case "LOBRDG16":
                case "LOBRDG17":
                case "LOBRDG18":
                case "LOBRDG19":
                case "LOBRDG20":
                case "LOBRDG21":
                case "LOBRDG22":
                case "LOBRDG23":
                case "LOBRDG24":
                case "LOBRDG25":
                case "LOBRDG26":
                case "LOBRDG27":
                case "LOBRDG28":
                    return "低桥" + iniName.Substring(6);
                    
                // 低桥B系列
                case "LOBRDB01":
                case "LOBRDB02":
                case "LOBRDB03":
                case "LOBRDB04":
                case "LOBRDB05":
                case "LOBRDB06":
                case "LOBRDB07":
                case "LOBRDB08":
                case "LOBRDB09":
                case "LOBRDB10":
                case "LOBRDB11":
                case "LOBRDB12":
                case "LOBRDB13":
                case "LOBRDB14":
                case "LOBRDB15":
                case "LOBRDB16":
                case "LOBRDB17":
                case "LOBRDB18":
                case "LOBRDB19":
                case "LOBRDB20":
                case "LOBRDB21":
                case "LOBRDB22":
                case "LOBRDB23":
                case "LOBRDB24":
                case "LOBRDB25":
                case "LOBRDB26":
                case "LOBRDB27":
                case "LOBRDB28":
                    return "低桥B" + iniName.Substring(6);

                case "LOBRDGB1": return "低桥GB1";
                case "LOBRDGB2": return "低桥GB2";
                case "LOBRDGB3": return "低桥GB3";
                case "LOBRDGB4": return "低桥GB4";

                // 围栏
                case "CAFNCB": return "黑色围栏";
                case "CAFNCW": return "白色围栏";
                case "CAFNCP": return "战俘营围栏";
                
                // 杂项
                case "CRATE": return "箱子";
                case "WCRATE": return "木箱";
                case "CAOPIP": return "管道";
                case "CBOX1": return "盒子1";
                case "CACRCK": return "裂缝";
                
                // TIB2系列保持原名
                default:
                    // 如果是TIB2系列，保持原名
                    if (iniName.StartsWith("TIB2_"))
                        return "矿石2_" + iniName.Substring(5);
                    // 其他情况保持原名
                    return iniName;
            }
        }
    }
}
