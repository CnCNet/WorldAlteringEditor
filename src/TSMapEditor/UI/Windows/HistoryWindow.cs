using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Reflection;
using TSMapEditor.Mutations;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class HistoryWindow : INItializableWindow
    {
        public HistoryWindow(WindowManager windowManager, MutationManager mutationManager) : base(windowManager)
        {
            this.mutationManager = mutationManager;
        }

        private readonly MutationManager mutationManager;

        private XNAListBox lbRedoHistory;
        private XNAListBox lbUndoHistory;
        private XNALabel lblRedoHistory;
        private XNALabel lblUndoHistory;
        private EditorButton btnRedoUpToSelected;
        private EditorButton btnUndoUpToSelected;

        private int lastUndoCount;
        private int lastRedoCount;

        public override void Initialize()
        {
            Name = nameof(HistoryWindow);
            base.Initialize();

            lbRedoHistory = FindChild<XNAListBox>(nameof(lbRedoHistory));
            lbUndoHistory = FindChild<XNAListBox>(nameof(lbUndoHistory));
            lblRedoHistory = FindChild<XNALabel>(nameof(lblRedoHistory));
            lblUndoHistory = FindChild<XNALabel>(nameof(lblUndoHistory));
            btnRedoUpToSelected = FindChild<EditorButton>("btnRedoUpToSelected");
            btnUndoUpToSelected = FindChild<EditorButton>("btnUndoUpToSelected");
            
            btnRedoUpToSelected.LeftClick += BtnRedoUpToSelected_LeftClick;
            btnUndoUpToSelected.LeftClick += BtnUndoUpToSelected_LeftClick;
            
            // 初始化时应用当前语言
            RefreshLanguage(MainMenu.IsChinese);
        }

        private void BtnRedoUpToSelected_LeftClick(object sender, EventArgs e)
        {
            if (lbRedoHistory.SelectedItem == null)
                return;

            int count = lbRedoHistory.SelectedIndex + 1;
            for (int i = 0; i < count; i++)
                mutationManager.Redo();

            lbRedoHistory.SelectedIndex = -1;
            RefreshHistory();
        }

        private void BtnUndoUpToSelected_LeftClick(object sender, EventArgs e)
        {
            if (lbUndoHistory.SelectedItem == null)
                return;

            int count = lbUndoHistory.SelectedIndex + 1;
            for (int i = 0; i < count; i++)
                mutationManager.UndoOne();

            lbUndoHistory.SelectedIndex = -1;
            RefreshHistory();
        }

        public void Open()
        {
            // 确保应用当前语言设置
            RefreshLanguage(MainMenu.IsChinese);
            // 刷新历史记录内容
            RefreshHistory();
            Show();
        }
        
        /// <summary>
        /// 根据语言设置刷新界面文本
        /// </summary>
        public void RefreshLanguage(bool isChinese)
        {
            // 检查控件是否已初始化
            if (lblRedoHistory == null || lblUndoHistory == null || 
                btnRedoUpToSelected == null || btnUndoUpToSelected == null)
                return;
            
            if (isChinese)
            {
                lblRedoHistory.Text = "已撤销且可以重做的操作历史：";
                lblUndoHistory.Text = "已在地图上执行且可以撤销的操作历史：";
                btnRedoUpToSelected.Text = "重做到选中项";
                btnUndoUpToSelected.Text = "撤销到选中项";
                
                // 使用反射修改窗口标题
                SetWindowTitle("操作历史");
            }
            else
            {
                lblRedoHistory.Text = "History of actions that have been undone and can be re-done:";
                lblUndoHistory.Text = "History of actions that have been performed on the map and can be undone:";
                btnRedoUpToSelected.Text = "Redo Up To Selected";
                btnUndoUpToSelected.Text = "Undo Up To Selected";
                
                // 使用反射修改窗口标题
                SetWindowTitle("History");
            }
        }
        
        // 通过修改INI配置文件来设置窗口标题
        private void SetWindowTitle(string title)
        {
            // 使用反射来修改窗口标题
            try 
            {
                var configIniField = GetType().BaseType.GetField("ConfigIni", BindingFlags.Instance | BindingFlags.NonPublic);
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
                var method = GetType().BaseType.GetMethod("RefreshLayout", BindingFlags.Instance | BindingFlags.Public);
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

        private void RefreshHistory()
        {
            const int MaxItems = 1000;

            lastRedoCount = mutationManager.RedoList.Count;
            lastUndoCount = mutationManager.UndoList.Count;

            lbUndoHistory.Clear();
            lbUndoHistory.ViewTop = 0;

            // The mutation history is a simple list where the newer the item, the lower its index.
            // For the UI, it makes sense to show the list in reversed order.
            for (int i = mutationManager.UndoList.Count - 1; i >= 0 && lbUndoHistory.Items.Count < MaxItems; i--)
            {
                string displayString = mutationManager.UndoList[i].GetDisplayString();
                string translatedString = Mutations.MutationTranslator.Translate(displayString, MainMenu.IsChinese);
                System.Console.WriteLine($"[HistoryWindow] 原始文本: {displayString}");
                System.Console.WriteLine($"[HistoryWindow] 翻译文本: {translatedString}");
                lbUndoHistory.AddItem(i.ToString() + " - " + translatedString);
            }

            lbRedoHistory.Clear();
            lbRedoHistory.ViewTop = 0;

            // Same goes for the redo list, but to the user it makes more sense if the flip the indexes.
            for (int i = mutationManager.RedoList.Count - 1; i >= 0 && lbRedoHistory.Items.Count < MaxItems; i--)
            {
                string displayString = mutationManager.RedoList[i].GetDisplayString();
                string translatedString = Mutations.MutationTranslator.Translate(displayString, MainMenu.IsChinese);
                System.Console.WriteLine($"[HistoryWindow] 原始文本: {displayString}");
                System.Console.WriteLine($"[HistoryWindow] 翻译文本: {translatedString}");
                lbRedoHistory.AddItem((mutationManager.RedoList.Count - 1 - i).ToString() + " - " + translatedString);
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (mutationManager.UndoList.Count != lastUndoCount || mutationManager.RedoList.Count != lastRedoCount)
                RefreshHistory();
        }
    }
}
