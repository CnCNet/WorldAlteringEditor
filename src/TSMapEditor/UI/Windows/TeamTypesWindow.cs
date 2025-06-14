using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.UI.Controls;
using TSMapEditor.Translations;
using TSMapEditor.CCEngine;

namespace TSMapEditor.UI.Windows
{
    public enum TeamTypeSortMode
    {
        ID,
        Name,
        Color,
        ColorThenName,
    }

    public class TaskForceEventArgs : EventArgs
    {
        public TaskForceEventArgs(TaskForce taskForce)
        {
            TaskForce = taskForce;
        }

        public TaskForce TaskForce { get; }
    }

    public class ScriptEventArgs : EventArgs
    {
        public ScriptEventArgs(Script script)
        {
            Script = script;
        }

        public Script Script { get; }
    }

    public class TeamTypesWindow : INItializableWindow
    {
        public TeamTypesWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public event EventHandler<TaskForceEventArgs> TaskForceOpened;
        public event EventHandler<ScriptEventArgs> ScriptOpened;
        public event EventHandler<TagEventArgs> TagOpened;

        private EditorSuggestionTextBox tbFilter;
        private EditorListBox lbTeamTypes;
        private EditorTextBox tbName;
        private XNADropDown ddVeteranLevel;
        private XNADropDown ddHouse;
        private EditorNumberTextBox tbPriority;
        private EditorNumberTextBox tbMax;
        private EditorNumberTextBox tbTechLevel;
        private XNADropDown ddMindControlDecision;
        private EditorNumberTextBox tbTransportWaypoint;
        private EditorNumberTextBox tbGroup;
        private EditorNumberTextBox tbWaypoint;
        private EditorPopUpSelector selTaskForce;
        private EditorPopUpSelector selScript;
        private EditorPopUpSelector selTag;
        private XNADropDown ddTeamTypeColor;

        private EditorContextMenu sortContextMenu;
        private EditorContextMenu teamTypeContextMenu;

        private TeamType editedTeamType;
        private List<XNACheckBox> checkBoxes = new List<XNACheckBox>();

        private SelectTaskForceWindow selectTaskForceWindow;
        private SelectScriptWindow selectScriptWindow;
        private SelectTagWindow selectTagWindow;

        private TeamTypeSortMode _teamTypeSortMode;
        private TeamTypeSortMode TeamTypeSortMode
        {
            get => _teamTypeSortMode;
            set
            {
                if (value != _teamTypeSortMode)
                {
                    _teamTypeSortMode = value;
                    ListTeamTypes();
                }
            }
        }

        private static readonly List<string> UniversalHouseNames = GenerateUniversalHouseNames();

        private static List<string> GenerateUniversalHouseNames()
        {
            var names = new List<string> { "Neutral", "Special" };
            for (char c = 'A'; c <= 'H'; c++)
            {
                names.Add($"<Player @ {c}>");
            }
            return names;
        }

        private bool ShouldDisplayHouse(HouseType ht)
        {
            return UniversalHouseNames.Contains(ht.ININame) || CategoryNameManager.TryGetCategoryNameTranslation(ht.ININame, out _);
        }

        private string GetHouseDisplayName(HouseType ht)
        {
            if (MainMenu.IsChinese && CategoryNameManager.TryGetCategoryNameTranslation(ht.ININame, out string translatedName))
            {
                return translatedName;
            }
            return ht.ININame;
        }

        public override void Initialize()
        {
            Name = nameof(TeamTypesWindow);
            base.Initialize();

            tbFilter = FindChild<EditorSuggestionTextBox>(nameof(tbFilter));
            lbTeamTypes = FindChild<EditorListBox>(nameof(lbTeamTypes));
            tbName = FindChild<EditorTextBox>(nameof(tbName));
            ddVeteranLevel = FindChild<XNADropDown>(nameof(ddVeteranLevel));
            ddHouse = FindChild<XNADropDown>(nameof(ddHouse));
            tbPriority = FindChild<EditorNumberTextBox>(nameof(tbPriority));
            tbMax = FindChild<EditorNumberTextBox>(nameof(tbMax));
            tbTechLevel = FindChild<EditorNumberTextBox>(nameof(tbTechLevel));
            ddMindControlDecision = FindChild<XNADropDown>(nameof(ddMindControlDecision));
            tbTransportWaypoint = FindChild<EditorNumberTextBox>(nameof(tbTransportWaypoint));
            tbGroup = FindChild<EditorNumberTextBox>(nameof(tbGroup));
            tbWaypoint = FindChild<EditorNumberTextBox>(nameof(tbWaypoint));
            selTaskForce = FindChild<EditorPopUpSelector>(nameof(selTaskForce));
            selScript = FindChild<EditorPopUpSelector>(nameof(selScript));
            selTag = FindChild<EditorPopUpSelector>(nameof(selTag));
            ddTeamTypeColor = FindChild<XNADropDown>(nameof(ddTeamTypeColor));

            ddTeamTypeColor.AddItem(MainMenu.IsChinese ? "势力颜色" : "House Color");
            foreach (var supportedColor in TeamType.SupportedColors)
            {
                string colorName = supportedColor.Name;
                switch (colorName)
                {
                    case "Teal": colorName = MainMenu.IsChinese ? "青色" : "Teal"; break;
                    case "Green": colorName = MainMenu.IsChinese ? "绿色" : "Green"; break;
                    case "Dark Green": colorName = MainMenu.IsChinese ? "深绿" : "Dark Green"; break;
                    case "Lime Green": colorName = MainMenu.IsChinese ? "浅绿" : "Lime Green"; break;
                    case "Yellow": colorName = MainMenu.IsChinese ? "黄色" : "Yellow"; break;
                    case "Orange": colorName = MainMenu.IsChinese ? "橙色" : "Orange"; break;
                    case "Red": colorName = MainMenu.IsChinese ? "红色" : "Red"; break;
                    case "Blood Red": colorName = MainMenu.IsChinese ? "深红" : "Blood Red"; break;
                    case "Pink": colorName = MainMenu.IsChinese ? "粉色" : "Pink"; break;
                    case "Cherry": colorName = MainMenu.IsChinese ? "樱桃红" : "Cherry"; break;
                    case "Purple": colorName = MainMenu.IsChinese ? "紫色" : "Purple"; break;
                    case "Sky Blue": colorName = MainMenu.IsChinese ? "天蓝" : "Sky Blue"; break;
                    case "Blue": colorName = MainMenu.IsChinese ? "蓝色" : "Blue"; break;
                    case "Brown": colorName = MainMenu.IsChinese ? "棕色" : "Brown"; break;
                    case "Metalic": colorName = MainMenu.IsChinese ? "金属色" : "Metalic"; break;
                }
                ddTeamTypeColor.AddItem(colorName, supportedColor.Value);
            }
            ddTeamTypeColor.SelectedIndexChanged += DdTeamTypeColor_SelectedIndexChanged;

            tbFilter.TextChanged += TbFilter_TextChanged;

            var panelBooleans = FindChild<EditorPanel>("panelBooleans");
            AddBooleanProperties(panelBooleans);

            lbTeamTypes.SelectedIndexChanged += LbTeamTypes_SelectedIndexChanged;

            var btnNewTeamType = FindChild<EditorButton>("btnNewTeamType");
            if (btnNewTeamType != null)
            {
                btnNewTeamType.LeftClick += BtnNewTeamType_LeftClick;
                btnNewTeamType.Text = MainMenu.IsChinese ? "新建" : "New";
            }
            
            var btnDeleteTeamType = FindChild<EditorButton>("btnDeleteTeamType");
            if (btnDeleteTeamType != null)
            {
                btnDeleteTeamType.LeftClick += BtnDeleteTeamType_LeftClick;
                btnDeleteTeamType.Text = MainMenu.IsChinese ? "删除" : "Delete";
            }
            
            var btnCloneTeamType = FindChild<EditorButton>("btnCloneTeamType");
            if (btnCloneTeamType != null)
            {
                btnCloneTeamType.LeftClick += BtnCloneTeamType_LeftClick;
                btnCloneTeamType.Text = MainMenu.IsChinese ? "克隆" : "Clone";
            }

            // 添加标签的汉化
            FindChild<XNALabel>("lblName").Text = MainMenu.IsChinese ? "名称:" : "Name:";
            FindChild<XNALabel>("lblVeteranLevel").Text = MainMenu.IsChinese ? "老兵等级:" : "Veteran Level:";
            FindChild<XNALabel>("lblHouse").Text = MainMenu.IsChinese ? "所属方:" : "House:";
            FindChild<XNALabel>("lblPriority").Text = MainMenu.IsChinese ? "优先级:" : "Priority:";
            FindChild<XNALabel>("lblMax").Text = MainMenu.IsChinese ? "最大数量:" : "Max:";
            FindChild<XNALabel>("lblTechLevel").Text = MainMenu.IsChinese ? "科技等级:" : "Tech Level:";
            FindChild<XNALabel>("lblMindControlDecision").Text = MainMenu.IsChinese ? "心灵控制:" : "On Mind Control:";
            
            // 添加第二列标签的汉化
            FindChild<XNALabel>("lblTeamTypeColor").Text = MainMenu.IsChinese ? "颜色:" : "Color:";
            FindChild<XNALabel>("lblGroup").Text = MainMenu.IsChinese ? "分组:" : "Group:";
            FindChild<XNALabel>("lblWaypoint").Text = MainMenu.IsChinese ? "路径点:" : "Waypoint:";
            FindChild<XNALabel>("lblTaskForce").Text = MainMenu.IsChinese ? "特遣队:" : "TaskForce:";
            FindChild<XNALabel>("lblScript").Text = MainMenu.IsChinese ? "脚本:" : "Script:";
            FindChild<XNALabel>("lblTag").Text = MainMenu.IsChinese ? "标签:" : "Tag:";
            FindChild<XNALabel>("lblTransportWaypoint").Text = MainMenu.IsChinese ? "运输路径点:" : "Transport Wpt:";

            selectTaskForceWindow = new SelectTaskForceWindow(WindowManager, map);
            var taskForceDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTaskForceWindow);
            taskForceDarkeningPanel.Hidden += (s, e) => SelectionWindow_ApplyEffect(w => editedTeamType.TaskForce = w.SelectedObject, selectTaskForceWindow);

            selectScriptWindow = new SelectScriptWindow(WindowManager, map);
            var scriptDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectScriptWindow);
            scriptDarkeningPanel.Hidden += (s, e) => SelectionWindow_ApplyEffect(w => editedTeamType.Script = w.SelectedObject, selectScriptWindow);

            selectTagWindow = new SelectTagWindow(WindowManager, map);
            var tagDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTagWindow);
            tagDarkeningPanel.Hidden += (s, e) => SelectionWindow_ApplyEffect(w => editedTeamType.Tag = w.SelectedObject, selectTagWindow);

            selTaskForce.LeftClick += (s, e) => 
            {
                if (editedTeamType == null)
                    return;

                if (Keyboard.IsCtrlHeldDown() && editedTeamType.TaskForce != null)
                {
                    OpenTaskForce();
                }
                else
                {
                    selectTaskForceWindow.Open(editedTeamType.TaskForce);
                }
            };

            selScript.LeftClick += (s, e) =>
            {
                if (editedTeamType == null)
                    return;

                if (Keyboard.IsCtrlHeldDown() && editedTeamType.Script != null)
                {
                    OpenScript();
                }
                else
                {
                    selectScriptWindow.Open(editedTeamType.Script);
                }
            };

            FindChild<EditorButton>("btnOpenTaskForce").LeftClick += (s, e) => OpenTaskForce();
            FindChild<EditorButton>("btnOpenScript").LeftClick += (s, e) => OpenScript();
            FindChild<EditorButton>("btnOpenTag").LeftClick += (s, e) => OpenTag();

            selTag.LeftClick += (s, e) => { if (editedTeamType != null) selectTagWindow.Open(editedTeamType.Tag); };

            sortContextMenu = new EditorContextMenu(WindowManager);
            sortContextMenu.Name = nameof(sortContextMenu);
            sortContextMenu.Width = lbTeamTypes.Width;
            sortContextMenu.AddItem(MainMenu.IsChinese ? "按ID排序" : "Sort by ID", () => TeamTypeSortMode = TeamTypeSortMode.ID);
            sortContextMenu.AddItem(MainMenu.IsChinese ? "按名称排序" : "Sort by Name", () => TeamTypeSortMode = TeamTypeSortMode.Name);
            sortContextMenu.AddItem(MainMenu.IsChinese ? "按颜色排序" : "Sort by Color", () => TeamTypeSortMode = TeamTypeSortMode.Color);
            sortContextMenu.AddItem(MainMenu.IsChinese ? "按颜色和名称排序" : "Sort by Color, then by Name", () => TeamTypeSortMode = TeamTypeSortMode.ColorThenName);
            AddChild(sortContextMenu);

            FindChild<EditorButton>("btnSortOptions").LeftClick += (s, e) => sortContextMenu.Open(GetCursorPoint());

            teamTypeContextMenu = new EditorContextMenu(WindowManager);
            teamTypeContextMenu.Name = nameof(teamTypeContextMenu);
            teamTypeContextMenu.Width = lbTeamTypes.Width;
            teamTypeContextMenu.AddItem(MainMenu.IsChinese ? "查看引用" : "View References", ShowTeamTypeReferences);
            AddChild(teamTypeContextMenu);

            lbTeamTypes.AllowRightClickUnselect = false;
            lbTeamTypes.RightClick += (s, e) =>
            {
                lbTeamTypes.SelectedIndex = lbTeamTypes.HoveredIndex;
                if (editedTeamType != null)
                    teamTypeContextMenu.Open(GetCursorPoint());
            };

            map.TeamTypesChanged += Map_TeamTypesChanged;
        }

        private void Map_TeamTypesChanged(object sender, EventArgs e)
        {
            if (Visible)
            {
                ListTeamTypes();
                SelectTeamType(editedTeamType);
            }
        }

        private void OpenTaskForce()
        {
            if (editedTeamType == null || editedTeamType.TaskForce == null)
                return;

            TaskForceOpened?.Invoke(this, new TaskForceEventArgs(editedTeamType.TaskForce));
            PutOnBackground();
        }

        private void OpenScript()
        {
            if (editedTeamType == null || editedTeamType.Script == null)
                return;

            ScriptOpened?.Invoke(this, new ScriptEventArgs(editedTeamType.Script));
            PutOnBackground();
        }

        private void OpenTag()
        {
            if (editedTeamType == null || editedTeamType.Tag == null)
                return;

            TagOpened?.Invoke(this, new TagEventArgs(editedTeamType.Tag));
            PutOnBackground();
        }

        private void SelectionWindow_ApplyEffect<T>(Action<T> action, T window)
        {
            if (lbTeamTypes.SelectedItem == null || editedTeamType == null)
            {
                return;
            }

            action(window);
            EditTeamType(editedTeamType);
        }

        private void ShowTeamTypeReferences()
        {
            if (editedTeamType == null)
                return;

            var stringBuilder = new StringBuilder();

            foreach (var trigger in map.Triggers)
            {
                int refActionCount = 0;

                foreach (var action in trigger.Actions)
                {
                    if (!map.EditorConfig.TriggerActionTypes.TryGetValue(action.ActionIndex, out var triggerActionType))
                        continue;

                    for (int i = 0; i < triggerActionType.Parameters.Length; i++)
                    {
                        if (triggerActionType.Parameters[i].TriggerParamType == TriggerParamType.TeamType
                            && action.Parameters[i] == editedTeamType.ININame)
                        {
                            refActionCount++;
                        }
                    }
                }

                int refConditionCount = 0;

                foreach (var condition in trigger.Conditions)
                {
                    if (!map.EditorConfig.TriggerEventTypes.TryGetValue(condition.ConditionIndex, out var triggerEventType))
                        continue;

                    for (int i = 0; i < triggerEventType.Parameters.Length; i++)
                    {
                        if (triggerEventType.Parameters[i].TriggerParamType == TriggerParamType.TeamType
                            && condition.Parameters[i] == editedTeamType.ININame)
                        {
                            refConditionCount++;
                        }
                    }
                }

                if (refActionCount > 0)
                {
                    stringBuilder.AppendLine($"- Trigger \"{trigger.Name}\" ({trigger.ID}) in {refActionCount} action parameter(s)");
                }

                if (refConditionCount > 0)
                {
                    stringBuilder.AppendLine($"- Trigger \"{trigger.Name}\" ({trigger.ID}) in {refConditionCount} event parameter(s)");
                }
            }

            foreach (var aiTrigger in map.AITriggerTypes)
            {
                if (aiTrigger.PrimaryTeam == editedTeamType)
                {
                    stringBuilder.AppendLine($"- Local AITrigger \"{aiTrigger.Name}\" ({aiTrigger.ININame}) as primary team");
                }

                if (aiTrigger.SecondaryTeam == editedTeamType)
                {
                    stringBuilder.AppendLine($"- Local AITrigger \"{aiTrigger.Name}\" ({aiTrigger.ININame}) as secondary team");
                }
            }

            var globalTeamType = map.Rules.TeamTypes.Find(tt => tt.ININame == editedTeamType.ININame);
            if (globalTeamType != null)
            {
                stringBuilder.AppendLine($"- This TeamType overrides a global TeamType {globalTeamType.ININame}. As such, it maybe be used by global AI Triggers.");
            }

            if (stringBuilder.Length == 0)
            {
                EditorMessageBox.Show(WindowManager, "No references found",
                    $"The selected TeamType \"{editedTeamType.Name}\" ({editedTeamType.ININame}) is not used by any Triggers or AITriggers.", MessageBoxButtons.OK);
            }
            else
            {
                EditorMessageBox.Show(WindowManager, "TeamType References",
                    $"The selected TeamType \"{editedTeamType.Name}\" ({editedTeamType.ININame}) is used by the following scripting elements:" + Environment.NewLine + Environment.NewLine +
                    stringBuilder.ToString(), MessageBoxButtons.OK);
            }
        }

        private void BtnNewTeamType_LeftClick(object sender, EventArgs e)
        {
            map.TeamTypesChanged -= Map_TeamTypesChanged;

            var teamType = new TeamType(map.GetNewUniqueInternalId()) { Name = "New TeamType" };
            map.EditorConfig.TeamTypeFlags.ForEach(flag => { if (flag.DefaultValue) teamType.EnableFlag(flag.Name); });
            map.AddTeamType(teamType);
            ListTeamTypes();
            SelectTeamType(teamType);

            map.TeamTypesChanged += Map_TeamTypesChanged;
        }

        private void BtnDeleteTeamType_LeftClick(object sender, EventArgs e)
        {
            if (editedTeamType == null)
                return;

            if (Keyboard.IsShiftHeldDown())
            {
                DeleteTeamType();
            }
            else
            {
                var messageBox = EditorMessageBox.Show(WindowManager,
                    "Confirm",
                    $"Are you sure you wish to delete '{editedTeamType.Name}'?" + Environment.NewLine + Environment.NewLine +
                    $"You'll need to manually fix any Triggers and AITriggers using the TeamType." + Environment.NewLine + Environment.NewLine +
                    "(You can hold Shift to skip this confirmation dialog.)",
                    MessageBoxButtons.YesNo);
                messageBox.YesClickedAction = _ => DeleteTeamType();
            }
        }

        private void DeleteTeamType()
        {
            if (editedTeamType == null)
                return;

            map.TeamTypesChanged -= Map_TeamTypesChanged;

            map.RemoveTeamType(editedTeamType);
            map.AITriggerTypes.ForEach(aitt =>
            {
                if (aitt.PrimaryTeam == editedTeamType)
                    aitt.PrimaryTeam = null;

                if (aitt.SecondaryTeam == editedTeamType)
                    aitt.SecondaryTeam = null;
            });
            ListTeamTypes();
            RefreshSelectedTeamType();

            map.TeamTypesChanged += Map_TeamTypesChanged;
        }

        private void BtnCloneTeamType_LeftClick(object sender, EventArgs e)
        {
            if (editedTeamType == null)
                return;

            map.TeamTypesChanged -= Map_TeamTypesChanged;

            var newTeamType = editedTeamType.Clone(map.GetNewUniqueInternalId());
            map.AddTeamType(newTeamType);
            ListTeamTypes();
            SelectTeamType(newTeamType);

            map.TeamTypesChanged += Map_TeamTypesChanged;
        }

        private void TbFilter_TextChanged(object sender, EventArgs e) => ListTeamTypes();

        private void LbTeamTypes_SelectedIndexChanged(object sender, EventArgs e) => RefreshSelectedTeamType();

        private void RefreshSelectedTeamType()
        {
            if (lbTeamTypes.SelectedItem == null)
            {
                lbTeamTypes.SelectedIndex = -1;
                editedTeamType = null;
                EditTeamType(null);
                return;
            }

            EditTeamType((TeamType)lbTeamTypes.SelectedItem.Tag);
        }

        private void AddBooleanProperties(EditorPanel panelBooleans)
        {
            // 英文到中文的映射表
            var flagNameCnDict = new Dictionary<string, string>
            {
                {"Full", "满员"},
                {"Whiner", "抱怨"},
                {"Droppod", "空投"},
                {"UseTransportOrigin", "使用运输起点"},
                {"Suicide", "自杀"},
                {"Loadable", "可装载"},
                {"Prebuild", "预建"},
                {"Annoyance", "骚扰"},
                {"IonImmune", "离子免疫"},
                {"Recruiter", "招募者"},
                {"Reinforce", "增援"},
                {"Aggressive", "激进"},
                {"Autocreate", "自动生成"},
                {"GuardSlower", "慢速警戒"},
                {"OnTransOnly", "仅运输中"},
                {"AvoidThreats", "规避威胁"},
                {"LooseRecruit", "松散招募"},
                {"IsBaseDefense", "基地防御"},
                {"OnlyTargetHouseEnemy", "只攻击敌方"},
                {"TransportsReturnOnUnload", "运输卸载后返回"},
                {"AreTeamMembersRecruitable", "成员可被招募"}
            };

            int currentColumnRight = 0;
            int currentColumnX = Constants.UIEmptySideSpace;
            XNACheckBox previousCheckBoxOnColumn = null;

            foreach (var teamTypeFlag in map.EditorConfig.TeamTypeFlags)
            {
                var checkBox = new XNACheckBox(WindowManager);
                checkBox.Tag = teamTypeFlag.Name;
                checkBox.Text = MainMenu.IsChinese && flagNameCnDict.ContainsKey(teamTypeFlag.Name)
                    ? flagNameCnDict[teamTypeFlag.Name]
                    : teamTypeFlag.Name;
                panelBooleans.AddChild(checkBox);
                checkBoxes.Add(checkBox);

                if (previousCheckBoxOnColumn == null)
                {
                    checkBox.Y = Constants.UIEmptyTopSpace;
                    checkBox.X = currentColumnX;
                }
                else
                {
                    checkBox.Y = previousCheckBoxOnColumn.Bottom + Constants.UIVerticalSpacing;
                    checkBox.X = currentColumnX;

                    // Start new column
                    if (checkBox.Bottom > panelBooleans.Height - Constants.UIEmptyBottomSpace)
                    {
                        currentColumnX = currentColumnRight + Constants.UIHorizontalSpacing * 2;
                        checkBox.Y = Constants.UIEmptyTopSpace;
                        checkBox.X = currentColumnX;
                        currentColumnRight = 0;
                    }
                }

                previousCheckBoxOnColumn = checkBox;
                currentColumnRight = Math.Max(currentColumnRight, checkBox.Right);
            }
        }

        public void Open()
        {
            Show();
            ListHouses();
            ListTeamTypes();
            SelectTeamType(editedTeamType);
        }

        private void ListHouses()
        {
            ddHouse.Items.Clear();
            ddHouse.AddItem(MainMenu.IsChinese ? "<无>" : Constants.NoneValue1);
            map.GetHouseTypes().ForEach(ht => 
            {
                if (ShouldDisplayHouse(ht))
                {
                    ddHouse.AddItem(GetHouseDisplayName(ht), Helpers.GetHouseTypeUITextColor(ht));
                }
            });
        }

        private void ListTeamTypes()
        {
            lbTeamTypes.Clear();
            
            IEnumerable<TeamType> sortedTeamTypes = map.TeamTypes;

            var shouldViewTop = false; // when filtering the scroll bar should update so we use a flag here
            if (tbFilter.Text != string.Empty && tbFilter.Text != tbFilter.Suggestion)
            {
                sortedTeamTypes = sortedTeamTypes.Where(teamType => teamType.Name.Contains(tbFilter.Text, StringComparison.CurrentCultureIgnoreCase));
                shouldViewTop = true;
            }

            switch (TeamTypeSortMode)
            {
                case TeamTypeSortMode.Color:
                    sortedTeamTypes = sortedTeamTypes.OrderBy(teamType => teamType.GetXNAColor().ToString()).ThenBy(teamType => teamType.ININame);
                    break;
                case TeamTypeSortMode.Name:
                    sortedTeamTypes = sortedTeamTypes.OrderBy(teamType => teamType.Name).ThenBy(teamType => teamType.ININame);
                    break;
                case TeamTypeSortMode.ColorThenName:
                    sortedTeamTypes = sortedTeamTypes.OrderBy(teamType => teamType.GetXNAColor().ToString()).ThenBy(teamType => teamType.Name);
                    break;
                case TeamTypeSortMode.ID:
                default:
                    sortedTeamTypes = sortedTeamTypes.OrderBy(teamType => teamType.ININame);
                    break;
            }

            foreach (var teamType in sortedTeamTypes)
            {
                lbTeamTypes.AddItem(new XNAListBoxItem() 
                { 
                    Text = teamType.Name,
                    Tag = teamType,
                    TextColor = teamType.GetXNAColor() 
                });
            }
            
            if (shouldViewTop)
                lbTeamTypes.TopIndex = 0;
        }

        public void SelectTeamType(TeamType teamType)
        {
            if (teamType == null)
            {
                lbTeamTypes.SelectedIndex = -1;
                return;
            }

            int index = lbTeamTypes.Items.FindIndex(lbi => lbi.Tag == teamType);

            if (index < 0)
            {
                lbTeamTypes.SelectedIndex = -1;
                return;
            }

            lbTeamTypes.SelectedIndex = index;
            lbTeamTypes.ScrollToSelectedElement();
        }

        private void EditTeamType(TeamType teamType)
        {
            tbName.TextChanged -= TbName_TextChanged;
            ddVeteranLevel.SelectedIndexChanged -= DdVeteranLevel_SelectedIndexChanged;
            ddHouse.SelectedIndexChanged -= DdHouse_SelectedIndexChanged;
            tbPriority.TextChanged -= TbPriority_TextChanged;
            tbMax.TextChanged -= TbMax_TextChanged;
            tbTechLevel.TextChanged -= TbTechLevel_TextChanged;
            ddMindControlDecision.SelectedIndexChanged -= DdMindControlDecision_SelectedIndexChanged;
            ddTeamTypeColor.SelectedIndexChanged -= DdTeamTypeColor_SelectedIndexChanged;
            tbGroup.TextChanged -= TbGroup_TextChanged;
            tbWaypoint.TextChanged -= TbWaypoint_TextChanged;
            tbTransportWaypoint.TextChanged -= TbTransportWaypoint_TextChanged;
            checkBoxes.ForEach(chk => chk.CheckedChanged -= FlagCheckBox_CheckedChanged);

            editedTeamType = teamType;

            if (editedTeamType == null)
            {
                tbName.Text = string.Empty;
                ddVeteranLevel.SelectedIndex = -1;
                ddHouse.SelectedIndex = -1;
                tbPriority.Text = string.Empty;
                tbMax.Text = string.Empty;
                tbTechLevel.Text = string.Empty;
                ddMindControlDecision.SelectedIndex = -1;

                ddTeamTypeColor.SelectedIndex = -1;
                tbGroup.Text = string.Empty;
                tbWaypoint.Text = string.Empty;
                tbTransportWaypoint.Text = string.Empty;

                selTaskForce.Text = string.Empty;
                selTaskForce.Tag = null;

                selScript.Text = string.Empty;
                selScript.Tag = null;

                selTag.Text = string.Empty;
                selTag.Tag = null;

                checkBoxes.ForEach(chk => chk.Checked = false);

                return;
            }

            tbName.Text = editedTeamType.Name;

            if (editedTeamType.HouseType != null && ShouldDisplayHouse(editedTeamType.HouseType))
            {
                ddHouse.SelectedIndex = ddHouse.Items.FindIndex(item => item.Text == GetHouseDisplayName(editedTeamType.HouseType));
            }
            else
            {
                ddHouse.SelectedIndex = 0; // Set to <none>
            }

            ddVeteranLevel.SelectedIndex = editedTeamType.VeteranLevel - 1;
            tbPriority.Value = editedTeamType.Priority;
            tbMax.Value = editedTeamType.Max;
            tbTechLevel.Value = editedTeamType.TechLevel;

            ddTeamTypeColor.SelectedIndex = ddTeamTypeColor.Items.FindIndex(item => item.Text == editedTeamType.EditorColor);
            if (ddTeamTypeColor.SelectedIndex == -1)
                ddTeamTypeColor.SelectedIndex = 0;

            tbGroup.Value = editedTeamType.Group;
            tbWaypoint.Value = Helpers.GetWaypointNumberFromAlphabeticalString(editedTeamType.Waypoint);

            if (Constants.IsRA2YR)
            {
                ddMindControlDecision.SelectedIndex = editedTeamType.MindControlDecision ?? -1;
                tbTransportWaypoint.Value = Helpers.GetWaypointNumberFromAlphabeticalString(editedTeamType.TransportWaypoint);
            }
            
            if (editedTeamType.TaskForce != null)
                selTaskForce.Text = editedTeamType.TaskForce.Name + " (" + editedTeamType.TaskForce.ININame + ")";
            else
                selTaskForce.Text = string.Empty;

            if (editedTeamType.Script != null)
                selScript.Text = editedTeamType.Script.Name + " (" + editedTeamType.Script.ININame + ")";
            else
                selScript.Text = string.Empty;

            if (editedTeamType.Tag != null)
                selTag.Text = editedTeamType.Tag.Name + " (" + editedTeamType.Tag.ID + ")";
            else
                selTag.Text = string.Empty;

            checkBoxes.ForEach(chk => chk.Checked = editedTeamType.IsFlagEnabled((string)chk.Tag));

            tbName.TextChanged += TbName_TextChanged;
            ddVeteranLevel.SelectedIndexChanged += DdVeteranLevel_SelectedIndexChanged;
            ddHouse.SelectedIndexChanged += DdHouse_SelectedIndexChanged;
            tbPriority.TextChanged += TbPriority_TextChanged;
            tbMax.TextChanged += TbMax_TextChanged;
            tbTechLevel.TextChanged += TbTechLevel_TextChanged;
            ddMindControlDecision.SelectedIndexChanged += DdMindControlDecision_SelectedIndexChanged;
            ddTeamTypeColor.SelectedIndexChanged += DdTeamTypeColor_SelectedIndexChanged;
            tbGroup.TextChanged += TbGroup_TextChanged;
            tbWaypoint.TextChanged += TbWaypoint_TextChanged;
            tbTransportWaypoint.TextChanged += TbTransportWaypoint_TextChanged;
            checkBoxes.ForEach(chk => chk.CheckedChanged += FlagCheckBox_CheckedChanged);
        }

        private void FlagCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            var checkBox = (XNACheckBox)sender;
            if (checkBox.Checked)
                editedTeamType.EnableFlag((string)checkBox.Tag);
            else
                editedTeamType.DisableFlag((string)checkBox.Tag);
        }

        private void TbWaypoint_TextChanged(object sender, EventArgs e)
        {
            editedTeamType.Waypoint = Helpers.WaypointNumberToAlphabeticalString(tbWaypoint.Value);
        }

        private void TbTransportWaypoint_TextChanged(object sender, EventArgs e)
        {
            if (Constants.IsRA2YR)
            {
                editedTeamType.TransportWaypoint = Helpers.WaypointNumberToAlphabeticalString(tbTransportWaypoint.Value);
            }
        }

        private void DdTeamTypeColor_SelectedIndexChanged(object sender, EventArgs e)
        {
            editedTeamType.EditorColor = ddTeamTypeColor.SelectedIndex < 1 ? null : ddTeamTypeColor.SelectedItem.Text;
            lbTeamTypes.SelectedItem.TextColor = editedTeamType.GetXNAColor();
        }

        private void TbGroup_TextChanged(object sender, EventArgs e)
        {
            editedTeamType.Group = tbGroup.Value;
        }

        private void TbTechLevel_TextChanged(object sender, EventArgs e)
        {
            editedTeamType.TechLevel = tbTechLevel.Value;
        }

        private void TbMax_TextChanged(object sender, EventArgs e)
        {
            editedTeamType.Max = tbMax.Value;
        }

        private void TbPriority_TextChanged(object sender, EventArgs e)
        {
            editedTeamType.Priority = tbPriority.Value;
        }

        private void DdHouse_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ddHouse.SelectedItem == null || ddHouse.SelectedIndex == 0)
            {
                editedTeamType.HouseType = null;
            }
            else
            {
                var selectedHouseType = map.GetHouseTypes().Find(ht => GetHouseDisplayName(ht) == ddHouse.SelectedItem.Text);
                editedTeamType.HouseType = selectedHouseType;
            }

            lbTeamTypes.SelectedItem.TextColor = editedTeamType.GetXNAColor();
        }

        private void DdVeteranLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            editedTeamType.VeteranLevel = ddVeteranLevel.SelectedIndex + 1;
        }

        private void DdMindControlDecision_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Constants.IsRA2YR)
            {
                editedTeamType.MindControlDecision = ddMindControlDecision.SelectedIndex;
            }
        }

        private void TbName_TextChanged(object sender, EventArgs e)
        {
            editedTeamType.Name = tbName.Text;
            lbTeamTypes.SelectedItem.Text = tbName.Text;
        }

        public void RefreshLanguage()
        {
            var lblDescription = FindChild<XNALabel>("lblDescription");
            if (lblDescription != null)
            {
                lblDescription.Text = MainMenu.IsChinese
                    ? "TeamTypes将触发器与特遣队和脚本连接起来，并定义单位的行为方式。"
                    : "TeamTypes connect a Trigger with a TaskForce and Script and define how the units behave.";
            }
            
            var lblTeamTypes = FindChild<XNALabel>("lblTeamTypes");
            if (lblTeamTypes != null)
            {
                lblTeamTypes.Text = MainMenu.IsChinese ? "团队类型:" : "TeamTypes:";
            }
            
            var lblSelectedTeamType = FindChild<XNALabel>("lblSelectedTeamType");
            if (lblSelectedTeamType != null)
            {
                lblSelectedTeamType.Text = MainMenu.IsChinese ? "已选择的TeamType:" : "Selected TeamType:";
            }

            var btnNewTeamType = FindChild<EditorButton>("btnNewTeamType");
            if (btnNewTeamType != null)
            {
                btnNewTeamType.Text = MainMenu.IsChinese ? "新建" : "New";
            }
            
            var btnDeleteTeamType = FindChild<EditorButton>("btnDeleteTeamType");
            if (btnDeleteTeamType != null)
            {
                btnDeleteTeamType.Text = MainMenu.IsChinese ? "删除" : "Delete";
            }
            
            var btnCloneTeamType = FindChild<EditorButton>("btnCloneTeamType");
            if (btnCloneTeamType != null)
            {
                btnCloneTeamType.Text = MainMenu.IsChinese ? "克隆" : "Clone";
            }

            // 刷新颜色下拉框
            ddTeamTypeColor.Items.Clear();
            ddTeamTypeColor.AddItem(MainMenu.IsChinese ? "势力颜色" : "House Color");
            foreach (var supportedColor in TeamType.SupportedColors)
            {
                string colorName = supportedColor.Name;
                switch (colorName)
                {
                    case "Teal": colorName = MainMenu.IsChinese ? "青色" : "Teal"; break;
                    case "Green": colorName = MainMenu.IsChinese ? "绿色" : "Green"; break;
                    case "Dark Green": colorName = MainMenu.IsChinese ? "深绿" : "Dark Green"; break;
                    case "Lime Green": colorName = MainMenu.IsChinese ? "浅绿" : "Lime Green"; break;
                    case "Yellow": colorName = MainMenu.IsChinese ? "黄色" : "Yellow"; break;
                    case "Orange": colorName = MainMenu.IsChinese ? "橙色" : "Orange"; break;
                    case "Red": colorName = MainMenu.IsChinese ? "红色" : "Red"; break;
                    case "Blood Red": colorName = MainMenu.IsChinese ? "深红" : "Blood Red"; break;
                    case "Pink": colorName = MainMenu.IsChinese ? "粉色" : "Pink"; break;
                    case "Cherry": colorName = MainMenu.IsChinese ? "樱桃红" : "Cherry"; break;
                    case "Purple": colorName = MainMenu.IsChinese ? "紫色" : "Purple"; break;
                    case "Sky Blue": colorName = MainMenu.IsChinese ? "天蓝" : "Sky Blue"; break;
                    case "Blue": colorName = MainMenu.IsChinese ? "蓝色" : "Blue"; break;
                    case "Brown": colorName = MainMenu.IsChinese ? "棕色" : "Brown"; break;
                    case "Metalic": colorName = MainMenu.IsChinese ? "金属色" : "Metalic"; break;
                }
                ddTeamTypeColor.AddItem(colorName, supportedColor.Value);
            }

            // 保持原来选中的颜色
            if (editedTeamType != null)
            {
                ddTeamTypeColor.SelectedIndex = ddTeamTypeColor.Items.FindIndex(item => item.Text == editedTeamType.EditorColor);
                if (ddTeamTypeColor.SelectedIndex == -1)
                    ddTeamTypeColor.SelectedIndex = 0;
            }

            // 刷新老兵等级下拉栏
            ddVeteranLevel.Items.Clear();
            ddVeteranLevel.AddItem(MainMenu.IsChinese ? "新兵" : "Regular");
            ddVeteranLevel.AddItem(MainMenu.IsChinese ? "老兵" : "Veteran");
            ddVeteranLevel.AddItem(MainMenu.IsChinese ? "精英" : "Elite");
            
            // 恢复选中的等级
            if (editedTeamType != null)
            {
                ddVeteranLevel.SelectedIndex = editedTeamType.VeteranLevel - 1;
            }

            // 刷新阵营下拉框
            ddHouse.Items.Clear();
            ddHouse.AddItem(MainMenu.IsChinese ? "<无>" : Constants.NoneValue1);
            map.GetHouseTypes().ForEach(ht => 
            {
                if (ShouldDisplayHouse(ht))
                {
                    ddHouse.AddItem(GetHouseDisplayName(ht), Helpers.GetHouseTypeUITextColor(ht));
                }
            });

            // 恢复选中的阵营
            if (editedTeamType != null)
            {
                if (editedTeamType.HouseType != null && ShouldDisplayHouse(editedTeamType.HouseType))
                {
                    ddHouse.SelectedIndex = ddHouse.Items.FindIndex(item => item.Text == GetHouseDisplayName(editedTeamType.HouseType));
                }
                else
                {
                    ddHouse.SelectedIndex = 0;
                }
            }

            // 刷新排序菜单
            if (sortContextMenu != null)
            {
                sortContextMenu.Items[0].Text = MainMenu.IsChinese ? "按ID排序" : "Sort by ID";
                sortContextMenu.Items[1].Text = MainMenu.IsChinese ? "按名称排序" : "Sort by Name";
                sortContextMenu.Items[2].Text = MainMenu.IsChinese ? "按颜色排序" : "Sort by Color";
                sortContextMenu.Items[3].Text = MainMenu.IsChinese ? "按颜色和名称排序" : "Sort by Color, then by Name";
            }

            // 刷新团队类型菜单
            if (teamTypeContextMenu != null)
            {
                teamTypeContextMenu.Items[0].Text = MainMenu.IsChinese ? "查看引用" : "View References";
            }

            // 刷新团队类型Flag复选框
            var flagNameCnDict = new Dictionary<string, string>
            {
                {"Full", "满员"},
                {"Whiner", "抱怨"},
                {"Droppod", "空投"},
                {"UseTransportOrigin", "使用运输起点"},
                {"Suicide", "自杀"},
                {"Loadable", "可装载"},
                {"Prebuild", "预建"},
                {"Annoyance", "骚扰"},
                {"IonImmune", "离子免疫"},
                {"Recruiter", "招募者"},
                {"Reinforce", "增援"},
                {"Aggressive", "激进"},
                {"Autocreate", "自动生成"},
                {"GuardSlower", "慢速警戒"},
                {"OnTransOnly", "仅运输中"},
                {"AvoidThreats", "规避威胁"},
                {"LooseRecruit", "松散招募"},
                {"IsBaseDefense", "基地防御"},
                {"OnlyTargetHouseEnemy", "只攻击敌方"},
                {"TransportsReturnOnUnload", "运输卸载后返回"},
                {"AreTeamMembersRecruitable", "成员可被招募"}
            };
            foreach (var chk in checkBoxes)
            {
                var flagName = chk.Tag as string;
                chk.Text = MainMenu.IsChinese && flagNameCnDict.ContainsKey(flagName)
                    ? flagNameCnDict[flagName]
                    : flagName;
            }

            // 刷新标签
            FindChild<XNALabel>("lblName").Text = MainMenu.IsChinese ? "名称:" : "Name:";
            FindChild<XNALabel>("lblVeteranLevel").Text = MainMenu.IsChinese ? "老兵等级:" : "Veteran Level:";
            FindChild<XNALabel>("lblHouse").Text = MainMenu.IsChinese ? "所属方:" : "House:";
            FindChild<XNALabel>("lblPriority").Text = MainMenu.IsChinese ? "优先级:" : "Priority:";
            FindChild<XNALabel>("lblMax").Text = MainMenu.IsChinese ? "最大数量:" : "Max:";
            FindChild<XNALabel>("lblTechLevel").Text = MainMenu.IsChinese ? "科技等级:" : "Tech Level:";
            FindChild<XNALabel>("lblMindControlDecision").Text = MainMenu.IsChinese ? "心灵控制:" : "On Mind Control:";
            FindChild<XNALabel>("lblTeamTypeColor").Text = MainMenu.IsChinese ? "颜色:" : "Color:";
            FindChild<XNALabel>("lblGroup").Text = MainMenu.IsChinese ? "分组:" : "Group:";
            FindChild<XNALabel>("lblWaypoint").Text = MainMenu.IsChinese ? "路径点:" : "Waypoint:";
            FindChild<XNALabel>("lblTaskForce").Text = MainMenu.IsChinese ? "特遣队:" : "TaskForce:";
            FindChild<XNALabel>("lblScript").Text = MainMenu.IsChinese ? "脚本:" : "Script:";
            FindChild<XNALabel>("lblTag").Text = MainMenu.IsChinese ? "标签:" : "Tag:";
            FindChild<XNALabel>("lblTransportWaypoint").Text = MainMenu.IsChinese ? "运输路径点:" : "Transport Wpt:";

            // 刷新心灵控制下拉框
            ddMindControlDecision.Items.Clear();
            ddMindControlDecision.AddItem(MainMenu.IsChinese ? "无" : "None");
            ddMindControlDecision.AddItem(MainMenu.IsChinese ? "加入队伍" : "Add to Team");
            ddMindControlDecision.AddItem(MainMenu.IsChinese ? "送往绞肉机" : "Send to Grinder");
            ddMindControlDecision.AddItem(MainMenu.IsChinese ? "送往生化反应堆" : "Send to Bio Reactor");
            ddMindControlDecision.AddItem(MainMenu.IsChinese ? "去狩猎" : "Go to Hunt");
            ddMindControlDecision.AddItem(MainMenu.IsChinese ? "什么也不做" : "Do Nothing");
            if (editedTeamType != null && editedTeamType.MindControlDecision.HasValue)
            {
                ddMindControlDecision.SelectedIndex = editedTeamType.MindControlDecision.Value;
            }

            // 刷新特遣队/脚本/标签选择弹窗
            selectTaskForceWindow?.RefreshLanguage();
            selectScriptWindow?.RefreshLanguage();
            selectTagWindow?.RefreshLanguage();
        }
    }
}
