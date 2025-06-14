using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TSMapEditor.Misc;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;
using TSMapEditor.Translations;

namespace TSMapEditor.UI.Windows
{
    public enum TaskForceSortMode
    {
        ID,
        Name,
        Color,
        ColorThenName,
    }

    /// <summary>
    /// A window that allows the user to edit the map's TaskForces.
    /// </summary>
    public class TaskforcesWindow : INItializableWindow
    {
        public TaskforcesWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private EditorSuggestionTextBox tbFilter;
        private EditorListBox lbTaskForces;
        private EditorTextBox tbTaskForceName;
        private EditorNumberTextBox tbGroup;
        private EditorListBox lbUnitEntries;
        private XNALabel lblCost;
        private EditorNumberTextBox tbUnitCount;
        private EditorSuggestionTextBox tbSearchUnit;
        private EditorListBox lbUnitType;

        private XNAContextMenu unitListContextMenu;

        private TaskForce editedTaskForce;

        private TaskForceSortMode _taskForceSortMode;
        private TaskForceSortMode TaskForceSortMode
        {
            get => _taskForceSortMode;
            set
            {
                if (value != _taskForceSortMode)
                {
                    _taskForceSortMode = value;
                    ListTaskForces();
                }
            }
        }

        private EditorContextMenu sortContextMenu;
        private EditorContextMenu taskForceContextMenu;

        public override void Initialize()
        {
            Name = nameof(TaskforcesWindow);

            base.Initialize();

            tbFilter = FindChild<EditorSuggestionTextBox>(nameof(tbFilter));
            lbTaskForces = FindChild<EditorListBox>(nameof(lbTaskForces));
            tbTaskForceName = FindChild<EditorTextBox>(nameof(tbTaskForceName));
            tbGroup = FindChild<EditorNumberTextBox>(nameof(tbGroup));
            lbUnitEntries = FindChild<EditorListBox>(nameof(lbUnitEntries));
            lblCost = FindChild<XNALabel>(nameof(lblCost));
            tbUnitCount = FindChild<EditorNumberTextBox>(nameof(tbUnitCount));
            tbSearchUnit = FindChild<EditorSuggestionTextBox>(nameof(tbSearchUnit));
            UIHelpers.AddSearchTipsBoxToControl(tbSearchUnit);

            lbUnitType = FindChild<EditorListBox>(nameof(lbUnitType));

            var btnNewTaskForce = FindChild<EditorButton>("btnNewTaskForce");
            btnNewTaskForce.LeftClick += BtnNewTaskForce_LeftClick;

            var btnDeleteTaskForce = FindChild<EditorButton>("btnDeleteTaskForce");
            btnDeleteTaskForce.LeftClick += BtnDeleteTaskForce_LeftClick;

            var btnCloneTaskForce = FindChild<EditorButton>("btnCloneTaskForce");
            btnCloneTaskForce.LeftClick += BtnCloneTaskForce_LeftClick;

            var btnAddUnit = FindChild<EditorButton>("btnAddUnit");
            btnAddUnit.LeftClick += BtnAddUnit_LeftClick;

            var btnDeleteUnit = FindChild<EditorButton>("btnDeleteUnit");
            btnDeleteUnit.LeftClick += BtnDeleteUnit_LeftClick;

            ListUnits();

            tbFilter.TextChanged += (s, e) => ListTaskForces();

            lbTaskForces.SelectedIndexChanged += LbTaskForces_SelectedIndexChanged;
            lbUnitEntries.SelectedIndexChanged += LbUnitEntries_SelectedIndexChanged;

            tbSearchUnit.TextChanged += TbSearchUnit_TextChanged;
            tbSearchUnit.EnterPressed += TbSearchUnit_EnterPressed;

            lbUnitType.SelectedIndexChanged += LbUnitType_SelectedIndexChanged;
            tbUnitCount.TextChanged += TbUnitCount_TextChanged;

            tbTaskForceName.TextChanged += TbTaskForceName_TextChanged;
            tbGroup.TextChanged += TbGroup_TextChanged;

            var sortContextMenu = new EditorContextMenu(WindowManager);
            sortContextMenu.Name = nameof(sortContextMenu);
            sortContextMenu.Width = lbTaskForces.Width;
            sortContextMenu.AddItem(MainMenu.IsChinese ? "按ID排序" : "Sort by ID", () => TaskForceSortMode = TaskForceSortMode.ID);
            sortContextMenu.AddItem(MainMenu.IsChinese ? "按名称排序" : "Sort by Name", () => TaskForceSortMode = TaskForceSortMode.Name);
            sortContextMenu.AddItem(MainMenu.IsChinese ? "按颜色排序" : "Sort by Color", () => TaskForceSortMode = TaskForceSortMode.Color);
            sortContextMenu.AddItem(MainMenu.IsChinese ? "先按颜色再按名称排序" : "Sort by Color, then by Name", () => TaskForceSortMode = TaskForceSortMode.ColorThenName);
            AddChild(sortContextMenu);
            this.sortContextMenu = sortContextMenu;

            FindChild<EditorButton>("btnSortOptions").LeftClick += (s, e) => sortContextMenu.Open(GetCursorPoint());

            var taskForceContextMenu = new EditorContextMenu(WindowManager);
            taskForceContextMenu.Name = nameof(taskForceContextMenu);
            taskForceContextMenu.Width = lbTaskForces.Width;
            taskForceContextMenu.AddItem("View References", ShowTaskForceReferences);
            AddChild(taskForceContextMenu);
            this.taskForceContextMenu = taskForceContextMenu;

            lbTaskForces.AllowRightClickUnselect = false;
            lbTaskForces.RightClick += (s, e) =>
            {
                lbTaskForces.SelectedIndex = lbTaskForces.HoveredIndex;
                if (editedTaskForce != null)
                    taskForceContextMenu.Open(GetCursorPoint());
            };

            unitListContextMenu = new XNAContextMenu(WindowManager);
            unitListContextMenu.Name = nameof(unitListContextMenu);
            unitListContextMenu.Width = 150;
            unitListContextMenu.AddItem("Move Up", UnitListContextMenu_MoveUp, () => editedTaskForce != null && lbUnitEntries.SelectedItem != null && lbUnitEntries.SelectedIndex > 0);
            unitListContextMenu.AddItem("Move Down", UnitListContextMenu_MoveDown, () => editedTaskForce != null && lbUnitEntries.SelectedItem != null && lbUnitEntries.SelectedIndex < lbUnitEntries.Items.Count - 1);
            unitListContextMenu.AddItem("Clone Unit Entry", UnitListContextMenu_CloneEntry, () => editedTaskForce != null && lbUnitEntries.SelectedItem != null && editedTaskForce.HasFreeTechnoSlot());
            unitListContextMenu.AddItem("Insert New Unit Here", UnitListContextMenu_Insert, () => editedTaskForce != null && lbUnitEntries.SelectedItem != null && editedTaskForce.HasFreeTechnoSlot());
            unitListContextMenu.AddItem("Delete Unit Entry", UnitListContextMenu_Delete, () => editedTaskForce != null && lbUnitEntries.SelectedItem != null);
            AddChild(unitListContextMenu);
            lbUnitEntries.AllowRightClickUnselect = false;
            lbUnitEntries.RightClick += (s, e) => { if (editedTaskForce != null) { lbUnitEntries.SelectedIndex = lbUnitEntries.HoveredIndex; unitListContextMenu.Open(GetCursorPoint()); } };

            map.TaskForcesChanged += Map_TaskForcesChanged;
            
            // 刷新界面语言
            RefreshLanguage();
        }

        /// <summary>
        /// 刷新界面语言，只汉化主要按钮
        /// </summary>
        public void RefreshLanguage()
        {
            bool isChinese = MainMenu.IsChinese;
            
            // 按钮汉化 - 确保与TriggersWindow实现方式相同
            var btnNewTaskForce = FindChild<EditorButton>("btnNewTaskForce");
            btnNewTaskForce.Text = isChinese ? "新建" : "New";
            
            var btnDeleteTaskForce = FindChild<EditorButton>("btnDeleteTaskForce");
            btnDeleteTaskForce.Text = isChinese ? "删除" : "Delete";
            
            var btnCloneTaskForce = FindChild<EditorButton>("btnCloneTaskForce");
            btnCloneTaskForce.Text = isChinese ? "克隆" : "Clone";
            
            // 添加标签汉化
            var lblTaskForces = FindChild<XNALabel>("lblTaskForces");
            if (lblTaskForces != null)
                lblTaskForces.Text = isChinese ? "特遣队：" : "TaskForces:";
                
            var lblSelectedTaskForce = FindChild<XNALabel>("lblSelectedTaskForce");
            if (lblSelectedTaskForce != null)
                lblSelectedTaskForce.Text = isChinese ? "已选特遣队：" : "Selected TaskForce:";
                
            // 添加更多标签汉化
            var lblTaskForceName = FindChild<XNALabel>("lblTaskForceName");
            if (lblTaskForceName != null)
                lblTaskForceName.Text = isChinese ? "名称：" : "Name:";
                
            var lblGroup = FindChild<XNALabel>("lblGroup");
            if (lblGroup != null)
                lblGroup.Text = isChinese ? "分组：" : "Group:";
                
            var lblUnitEntries = FindChild<XNALabel>("lblUnitEntries");
            if (lblUnitEntries != null)
                lblUnitEntries.Text = isChinese ? "单位条目：" : "Unit Entries:";
                
            // 查找并添加单位按钮汉化
            var btnAddUnit = FindChild<EditorButton>("btnAddUnit");
            if (btnAddUnit != null)
                btnAddUnit.Text = isChinese ? "添加单位" : "Add Unit";
                
            var btnDeleteUnit = FindChild<EditorButton>("btnDeleteUnit");
            if (btnDeleteUnit != null)
                btnDeleteUnit.Text = isChinese ? "删除单位" : "Delete Unit";
            
            // 添加新的标签汉化
            var lblUnitCount = FindChild<XNALabel>("lblUnitCount");
            if (lblUnitCount != null)
                lblUnitCount.Text = isChinese ? "单位数量：" : "Number of units:";
                
            var lblUnitType = FindChild<XNALabel>("lblUnitType");
            if (lblUnitType != null)
                lblUnitType.Text = isChinese ? "单位类型：" : "Unit type:";
                
            // 查找并添加排序按钮汉化
            var btnSortOptions = FindChild<EditorButton>("btnSortOptions");
            if (btnSortOptions != null)
                btnSortOptions.Text = isChinese ? "排序" : "Sort";
                
            // 排序菜单汉化
            if (sortContextMenu != null && sortContextMenu.Items.Count >= 4)
            {
                sortContextMenu.Items[0].Text = isChinese ? "按ID排序" : "Sort by ID";
                sortContextMenu.Items[1].Text = isChinese ? "按名称排序" : "Sort by Name";
                sortContextMenu.Items[2].Text = isChinese ? "按颜色排序" : "Sort by Color";
                sortContextMenu.Items[3].Text = isChinese ? "先按颜色再按名称排序" : "Sort by Color, then by Name";
            }
            
            // 单位列表上下文菜单汉化
            if (unitListContextMenu != null && unitListContextMenu.Items.Count >= 5)
            {
                unitListContextMenu.Items[0].Text = isChinese ? "上移" : "Move Up";
                unitListContextMenu.Items[1].Text = isChinese ? "下移" : "Move Down";
                unitListContextMenu.Items[2].Text = isChinese ? "克隆单位条目" : "Clone Unit Entry";
                unitListContextMenu.Items[3].Text = isChinese ? "在此处插入新单位" : "Insert New Unit Here";
                unitListContextMenu.Items[4].Text = isChinese ? "删除单位条目" : "Delete Unit Entry";
            }
            
            // 重新加载单位列表以更新语言
            lbUnitType.Clear();
            ListUnits();
            
            // 如果有已选择的特遣队，刷新其显示
            if (editedTaskForce != null)
            {
                // 保存当前选择的索引
                int selectedIndex = lbUnitEntries.SelectedIndex;
                
                // 重新加载单位条目
                lbUnitEntries.SelectedIndexChanged -= LbUnitEntries_SelectedIndexChanged;
                lbUnitEntries.Clear();
                
                for (int i = 0; i < editedTaskForce.TechnoTypes.Length; i++)
                {
                    var taskForceTechno = editedTaskForce.TechnoTypes[i];
                    if (taskForceTechno == null)
                        break;
                    
                    lbUnitEntries.AddItem(GetUnitEntryText(taskForceTechno));
                }
                
                // 恢复选择
                if (selectedIndex >= 0 && selectedIndex < lbUnitEntries.Items.Count)
                    lbUnitEntries.SelectedIndex = selectedIndex;
                
                lbUnitEntries.SelectedIndexChanged += LbUnitEntries_SelectedIndexChanged;
            }
        }

        private void Map_TaskForcesChanged(object sender, EventArgs e)
        {
            if (Visible)
            {
                ListTaskForces();
                SelectTaskForce(editedTaskForce);
            }
        }

        private void UnitListContextMenu_MoveUp()
        {
            if (editedTaskForce == null || lbUnitEntries.SelectedItem == null || lbUnitEntries.SelectedIndex <= 0)
                return;

            int viewTop = lbUnitEntries.ViewTop;
            editedTaskForce.TechnoTypes.Swap(lbUnitEntries.SelectedIndex - 1, lbUnitEntries.SelectedIndex);
            EditTaskForce(editedTaskForce);
            lbUnitEntries.SelectedIndex--;
            lbUnitEntries.ViewTop = viewTop;
        }

        private void UnitListContextMenu_MoveDown()
        {
            if (editedTaskForce == null || lbUnitEntries.SelectedItem == null || lbUnitEntries.SelectedIndex >= lbUnitEntries.Items.Count - 1)
                return;

            int viewTop = lbUnitEntries.ViewTop;
            editedTaskForce.TechnoTypes.Swap(lbUnitEntries.SelectedIndex, lbUnitEntries.SelectedIndex + 1);
            EditTaskForce(editedTaskForce);
            lbUnitEntries.SelectedIndex++;
            lbUnitEntries.ViewTop = viewTop;
        }

        private void UnitListContextMenu_CloneEntry()
        {
            if (editedTaskForce == null || lbUnitEntries.SelectedItem == null || !editedTaskForce.HasFreeTechnoSlot())
                return;

            int viewTop = lbUnitEntries.ViewTop;
            int newIndex = lbUnitEntries.SelectedIndex + 1;

            var clonedEntry = editedTaskForce.TechnoTypes[lbUnitEntries.SelectedIndex].Clone();
            editedTaskForce.InsertTechnoEntry(newIndex, clonedEntry);
            EditTaskForce(editedTaskForce);
            lbUnitEntries.SelectedIndex = newIndex;
            lbUnitEntries.ViewTop = viewTop;
        }

        private void UnitListContextMenu_Insert()
        {
            if (editedTaskForce == null || lbUnitEntries.SelectedItem == null)
                return;

            int viewTop = lbUnitEntries.ViewTop;
            int newIndex = lbUnitEntries.SelectedIndex;

            editedTaskForce.InsertTechnoEntry(lbUnitEntries.SelectedIndex,
                new TaskForceTechnoEntry()
                {
                    Count = 1,
                    TechnoType = (TechnoType)lbUnitType.Items[0].Tag
                });

            EditTaskForce(editedTaskForce);
            lbUnitEntries.SelectedIndex = newIndex;
            lbUnitEntries.ViewTop = viewTop;
        }

        private void UnitListContextMenu_Delete()
        {
            if (editedTaskForce == null || lbUnitEntries.SelectedItem == null)
                return;

            int viewTop = lbUnitEntries.ViewTop;
            editedTaskForce.RemoveTechnoEntry(lbUnitEntries.SelectedIndex);
            EditTaskForce(editedTaskForce);
            lbUnitEntries.ViewTop = viewTop;
        }

        private void BtnDeleteUnit_LeftClick(object sender, System.EventArgs e)
        {
            if (editedTaskForce == null || lbUnitEntries.SelectedItem == null)
                return;

            editedTaskForce.RemoveTechnoEntry(lbUnitEntries.SelectedIndex);
            EditTaskForce(editedTaskForce);
        }

        private void BtnAddUnit_LeftClick(object sender, System.EventArgs e)
        {
            if (editedTaskForce == null)
                return;

            if (!editedTaskForce.HasFreeTechnoSlot())
                return;

            editedTaskForce.AddTechnoEntry(
                new TaskForceTechnoEntry() 
                { 
                    Count = 1, 
                    TechnoType = (TechnoType)lbUnitType.Items[0].Tag 
                });

            EditTaskForce(editedTaskForce);
            lbUnitEntries.SelectedIndex = lbUnitEntries.Items.Count - 1;
        }

        private void BtnCloneTaskForce_LeftClick(object sender, System.EventArgs e)
        {
            if (editedTaskForce == null)
                return;

            map.TaskForcesChanged -= Map_TaskForcesChanged;

            var newTaskForce = editedTaskForce.Clone(map.GetNewUniqueInternalId());
            
            // 始终使用英文前缀，避免中文显示问题
            if (!newTaskForce.Name.StartsWith("Copy of "))
            {
                newTaskForce.Name = "Copy of " + newTaskForce.Name;
            }
            
            map.AddTaskForce(newTaskForce);
            ListTaskForces();
            SelectTaskForce(newTaskForce);

            map.TaskForcesChanged += Map_TaskForcesChanged;
        }

        private void BtnDeleteTaskForce_LeftClick(object sender, System.EventArgs e)
        {
            if (editedTaskForce == null)
                return;

            if (Keyboard.IsShiftHeldDown())
            {
                DeleteTaskForce();
            }
            else
            {
                var messageBox = EditorMessageBox.Show(WindowManager,
                    MainMenu.IsChinese ? "确认" : "Confirm",
                    MainMenu.IsChinese ? 
                        $"确定要删除 '{editedTaskForce.Name}'?" + Environment.NewLine + Environment.NewLine +
                        $"您需要手动修复使用此特遣队的TeamTypes。" + Environment.NewLine + Environment.NewLine +
                        "(您可以按住Shift键跳过此确认对话框。)" :
                        $"Are you sure you wish to delete '{editedTaskForce.Name}'?" + Environment.NewLine + Environment.NewLine +
                        $"You'll need to manually fix any TeamTypes using the TaskForce." + Environment.NewLine + Environment.NewLine +
                        "(You can hold Shift to skip this confirmation dialog.)",
                    MessageBoxButtons.YesNo);
                messageBox.YesClickedAction = _ => DeleteTaskForce();
            }
        }

        private void DeleteTaskForce()
        {
            map.TaskForcesChanged -= Map_TaskForcesChanged;

            map.RemoveTaskForce(editedTaskForce);
            map.TeamTypes.ForEach(tt =>
            {
                if (tt.TaskForce == editedTaskForce)
                    tt.TaskForce = null;
            });
            ListTaskForces();
            RefreshSelectedTaskForce();

            map.TaskForcesChanged += Map_TaskForcesChanged;
        }

        private void BtnNewTaskForce_LeftClick(object sender, System.EventArgs e)
        {
            map.TaskForcesChanged -= Map_TaskForcesChanged;

            var taskForce = new TaskForce(map.GetNewUniqueInternalId()) { Name = "New TaskForce" };
            map.AddTaskForce(taskForce);
            ListTaskForces();
            SelectTaskForce(taskForce);

            map.TaskForcesChanged += Map_TaskForcesChanged;
        }

        private void ShowTaskForceReferences()
        {
            if (editedTaskForce == null)
                return;

            var referringLocalTeamTypes = map.TeamTypes.FindAll(tt => tt.TaskForce == editedTaskForce);
            var referringGlobalTeamTypes = map.Rules.TeamTypes.FindAll(tt => tt.Script.ININame == editedTaskForce.ININame);

            if (referringLocalTeamTypes.Count == 0 && referringGlobalTeamTypes.Count == 0)
            {
                EditorMessageBox.Show(WindowManager, "No references found",
                    $"The selected TaskForce \"{editedTaskForce.Name}\" ({editedTaskForce.ININame}) is not used by any TeamTypes, either local (map) or global (AI.ini).", MessageBoxButtons.OK);
            }
            else
            {
                var stringBuilder = new StringBuilder();
                referringLocalTeamTypes.ForEach(tt => stringBuilder.AppendLine($"- Local TeamType \"{tt.Name}\" ({tt.ININame})"));
                referringGlobalTeamTypes.ForEach(tt => stringBuilder.AppendLine($"- Global TeamType \"{tt.Name}\" ({tt.ININame})"));

                EditorMessageBox.Show(WindowManager, "TaskForce References",
                    $"The selected TaskForce \"{editedTaskForce.Name}\" ({editedTaskForce.ININame}) is used by the following TeamTypes:" + Environment.NewLine + Environment.NewLine +
                    stringBuilder.ToString(), MessageBoxButtons.OK);
            }
        }

        private void TbGroup_TextChanged(object sender, System.EventArgs e)
        {
            if (editedTaskForce == null)
                return;

            editedTaskForce.Group = tbGroup.Value;
        }

        private void TbTaskForceName_TextChanged(object sender, System.EventArgs e)
        {
            if (editedTaskForce == null)
                return;

            editedTaskForce.Name = tbTaskForceName.Text;
            if (lbTaskForces.SelectedItem != null)
            {
                lbTaskForces.SelectedItem.Text = editedTaskForce.Name;
            }
        }

        private void LbUnitType_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (lbUnitType.SelectedItem == null)
                return;

            var unitEntry = lbUnitEntries.SelectedItem;
            if (unitEntry == null)
            {
                return;
            }

            var taskForceTechno = editedTaskForce.TechnoTypes[lbUnitEntries.SelectedIndex];
            taskForceTechno.TechnoType = (TechnoType)lbUnitType.SelectedItem.Tag;
            unitEntry.Text = GetUnitEntryText(taskForceTechno);
            RefreshTaskForceCost();
        }

        private void TbUnitCount_TextChanged(object sender, System.EventArgs e)
        {
            var unitEntry = lbUnitEntries.SelectedItem;
            if (unitEntry == null)
            {
                return;
            }

            var taskForceTechno = editedTaskForce.TechnoTypes[lbUnitEntries.SelectedIndex];
            taskForceTechno.Count = tbUnitCount.Value;
            unitEntry.Text = GetUnitEntryText(taskForceTechno);
            RefreshTaskForceCost();
        }

        private void LbUnitEntries_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            var unitEntry = lbUnitEntries.SelectedItem;
            if (unitEntry == null)
            {
                tbUnitCount.Text = string.Empty;
                return;
            }

            var taskForceTechno = editedTaskForce.TechnoTypes[lbUnitEntries.SelectedIndex];

            lbUnitType.SelectedIndexChanged -= LbUnitType_SelectedIndexChanged;
            lbUnitType.SelectedIndex = lbUnitType.Items.FindIndex(u => ((TechnoType)u.Tag) == taskForceTechno.TechnoType);
            lbUnitType.ViewTop = lbUnitType.SelectedIndex * lbUnitType.LineHeight;
            lbUnitType.SelectedIndexChanged += LbUnitType_SelectedIndexChanged;

            tbUnitCount.TextChanged -= TbUnitCount_TextChanged;
            tbUnitCount.Value = taskForceTechno.Count;
            tbUnitCount.TextChanged += TbUnitCount_TextChanged;
        }

        private void LbTaskForces_SelectedIndexChanged(object sender, EventArgs e) => RefreshSelectedTaskForce();

        private void RefreshSelectedTaskForce()
        {
            var selectedItem = lbTaskForces.SelectedItem;
            if (selectedItem == null)
            {
                lbTaskForces.SelectedIndex = -1;
                EditTaskForce(null);
                return;
            }

            EditTaskForce((TaskForce)selectedItem.Tag);
        }

        private void TbSearchUnit_EnterPressed(object sender, System.EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbSearchUnit.Text) || tbSearchUnit.Text == tbSearchUnit.Suggestion)
                return;

            FindNextMatchingUnit();
        }

        private void TbSearchUnit_TextChanged(object sender, System.EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbSearchUnit.Text) || tbSearchUnit.Text == tbSearchUnit.Suggestion)
                return;

            lbUnitType.SelectedIndex = -1;
            FindNextMatchingUnit();
        }

        private void FindNextMatchingUnit()
        {
            for (int i = lbUnitType.SelectedIndex + 1; i < lbUnitType.Items.Count; i++)
            {
                var gameObjectType = (TechnoType)lbUnitType.Items[i].Tag;

                if (gameObjectType.ININame.ToUpperInvariant().Contains(tbSearchUnit.Text.ToUpperInvariant()) ||
                    gameObjectType.GetEditorDisplayName().ToUpperInvariant().Contains(tbSearchUnit.Text.ToUpperInvariant()))
                {
                    lbUnitType.SelectedIndex = i;
                    lbUnitType.ViewTop = lbUnitType.SelectedIndex * lbUnitType.LineHeight;
                    break;
                }
            }
        }

        private void ListUnits()
        {
            var gameObjectTypeList = new List<GameObjectType>();
            gameObjectTypeList.AddRange(map.Rules.AircraftTypes);
            gameObjectTypeList.AddRange(map.Rules.InfantryTypes);
            gameObjectTypeList.AddRange(map.Rules.UnitTypes);
            gameObjectTypeList = gameObjectTypeList.OrderBy(g => g.ININame).ToList();

            foreach (GameObjectType objectType in gameObjectTypeList)
            {
                string displayText = objectType.ININame;
                string displayName = objectType.GetEditorDisplayName();
                
                // 尝试查找中文翻译
                if (MainMenu.IsChinese)
                {
                    // 根据对象类型选择合适的翻译查询方法
                    string chineseName = null;
                    if (objectType is AircraftType)
                    {
                        Translations.TechnoNameManager.TryGetVehicleName(objectType.ININame, out chineseName);
                    }
                    else if (objectType is InfantryType)
                    {
                        Translations.TechnoNameManager.TryGetInfantryName(objectType.ININame, out chineseName);
                    }
                    else if (objectType is UnitType)
                    {
                        Translations.TechnoNameManager.TryGetVehicleName(objectType.ININame, out chineseName);
                    }
                    
                    // 如果找到翻译，替换显示名称
                    if (!string.IsNullOrEmpty(chineseName))
                    {
                        displayName = chineseName;
                    }
                }
                
                displayText = objectType.ININame + " (" + displayName + ")";
                lbUnitType.AddItem(new XNAListBoxItem() { Text = displayText, Tag = objectType });
            }
        }

        public void Open()
        {
            Show();
            ListTaskForces();
        }

        public void SelectTaskForce(TaskForce taskForce)
        {
            if (taskForce == null)
            {
                lbTaskForces.SelectedIndex = -1;
                return;
            }

            int index = lbTaskForces.Items.FindIndex(lbi => lbi.Tag == taskForce);

            if (index < 0)
            {
                lbTaskForces.SelectedIndex = -1;
                return;
            }

            lbTaskForces.SelectedIndex = index;
            lbTaskForces.ScrollToSelectedElement();
        }

        private void ListTaskForces()
        {
            lbTaskForces.SelectedIndexChanged -= LbTaskForces_SelectedIndexChanged;
            lbTaskForces.Clear();

            bool shouldViewTop = false; // when filtering the scroll bar should update so we use a flag here
            IEnumerable<TaskForce> sortedTaskForces = map.TaskForces;
            if (tbFilter.Text != string.Empty && tbFilter.Text != tbFilter.Suggestion)
            {
                sortedTaskForces = sortedTaskForces.Where(script => script.Name.Contains(tbFilter.Text, StringComparison.CurrentCultureIgnoreCase));
                shouldViewTop = true;
            }

            switch (TaskForceSortMode)
            {
                case TaskForceSortMode.Color:
                    sortedTaskForces = sortedTaskForces.OrderBy(taskForce => GetTaskForceColor(taskForce).ToString()).ThenBy(taskForce => taskForce.ININame);
                    break;
                case TaskForceSortMode.Name:
                    sortedTaskForces = sortedTaskForces.OrderBy(taskForce => taskForce.Name).ThenBy(taskForce => taskForce.ININame);
                    break;
                case TaskForceSortMode.ColorThenName:
                    sortedTaskForces = sortedTaskForces.OrderBy(taskForce => GetTaskForceColor(taskForce).ToString()).ThenBy(taskForce => taskForce.Name);
                    break;
                case TaskForceSortMode.ID:
                default:
                    sortedTaskForces = sortedTaskForces.OrderBy(taskForce => taskForce.ININame);
                    break;
            }

            foreach (var taskForce in sortedTaskForces)
            {
                lbTaskForces.AddItem(new XNAListBoxItem()
                {
                    Text = taskForce.Name,
                    Tag = taskForce,
                    TextColor = GetTaskForceColor(taskForce)
                });
            }

            if (shouldViewTop)
                lbTaskForces.TopIndex = 0;

            lbTaskForces.SelectedIndexChanged += LbTaskForces_SelectedIndexChanged;
            LbTaskForces_SelectedIndexChanged(this, EventArgs.Empty);
        }

        private Color GetTaskForceColor(TaskForce taskForce)
        {
            var usage = map.TeamTypes.Find(tt => tt.TaskForce == taskForce);
            if (usage == null)
                return UISettings.ActiveSettings.AltColor;

            return usage.GetXNAColor();
        }

        private void EditTaskForce(TaskForce taskForce)
        {
            editedTaskForce = taskForce;

            RefreshTaskForceCost();

            if (taskForce == null)
            {
                tbTaskForceName.Text = string.Empty;
                tbGroup.Text = string.Empty;
                lbUnitEntries.Clear();
                tbUnitCount.Text = string.Empty;
                return;
            }

            tbSearchUnit.Text = tbSearchUnit.Suggestion;

            tbTaskForceName.Text = taskForce.Name;
            tbGroup.Value = taskForce.Group;

            lbUnitEntries.SelectedIndexChanged -= LbUnitEntries_SelectedIndexChanged;
            lbUnitEntries.Clear();

            for (int i = 0; i < taskForce.TechnoTypes.Length; i++)
            {
                var taskForceTechno = taskForce.TechnoTypes[i];
                if (taskForceTechno == null)
                    break;

                lbUnitEntries.AddItem(GetUnitEntryText(taskForceTechno));
            }

            lbUnitEntries.SelectedIndexChanged += LbUnitEntries_SelectedIndexChanged;

            if (lbUnitEntries.SelectedItem == null && lbUnitEntries.Items.Count > 0)
            {
                lbUnitEntries.SelectedIndex = 0;
            }
            else
            {
                LbUnitEntries_SelectedIndexChanged(this, EventArgs.Empty);
            }
        }

        private void RefreshTaskForceCost()
        {
            if (editedTaskForce == null)
            {
                lblCost.Text = string.Empty;
                return;
            }

            int cost = 0;
            foreach (var technoEntry in editedTaskForce.TechnoTypes)
            {
                if (technoEntry != null)
                    cost += technoEntry.TechnoType.Cost * technoEntry.Count;
            }

            lblCost.Text = cost.ToString(CultureInfo.InvariantCulture) + "$";
        }

        private string GetUnitEntryText(TaskForceTechnoEntry taskForceTechno)
        {
            string displayName = taskForceTechno.TechnoType.GetEditorDisplayName();
            
            // 尝试查找中文翻译
            if (MainMenu.IsChinese)
            {
                // 根据对象类型选择合适的翻译查询方法
                string chineseName = null;
                var objectType = taskForceTechno.TechnoType;
                
                if (objectType is AircraftType)
                {
                    TechnoNameManager.TryGetVehicleName(objectType.ININame, out chineseName);
                }
                else if (objectType is InfantryType)
                {
                    TechnoNameManager.TryGetInfantryName(objectType.ININame, out chineseName);
                }
                else if (objectType is UnitType)
                {
                    TechnoNameManager.TryGetVehicleName(objectType.ININame, out chineseName);
                }
                
                // 如果找到翻译，替换显示名称
                if (!string.IsNullOrEmpty(chineseName))
                {
                    displayName = chineseName;
                }
            }
            
            return $"{taskForceTechno.Count} {taskForceTechno.TechnoType.ININame} ({displayName})";
        }
    }
}
