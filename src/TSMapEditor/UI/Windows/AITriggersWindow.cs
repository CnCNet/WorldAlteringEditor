using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class TeamTypeEventArgs : EventArgs
    {
        public TeamTypeEventArgs(TeamType teamType)
        {
            TeamType = teamType;
        }

        public TeamType TeamType { get; }
    }

    public class AITriggersWindow : INItializableWindow
    {
        public AITriggersWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public event EventHandler<TeamTypeEventArgs> TeamTypeOpened;

        private EditorListBox lbAITriggers;
        private XNADropDown ddActions;
        private EditorTextBox tbName;
        private XNADropDown ddSide;
        private XNADropDown ddHouseType;
        private XNADropDown ddConditionType;
        private XNADropDown ddComparator;
        private EditorNumberTextBox tbQuantity;
        private EditorPopUpSelector selComparisonObjectType;
        private EditorPopUpSelector selPrimaryTeam;
        private EditorPopUpSelector selSecondaryTeam;
        private EditorNumberTextBox tbInitial;
        private EditorNumberTextBox tbMinimum;
        private EditorNumberTextBox tbMaximum;
        private XNACheckBox chkEnabledOnEasy;
        private XNACheckBox chkEnabledOnMedium;
        private XNACheckBox chkEnabledOnHard;

        private SelectTeamTypeWindow selectTeamTypeWindow;
        private SelectTechnoTypeWindow selectTechnoTypeWindow;

        private AITriggerType editedAITrigger;

        public override void Initialize()
        {
            Name = nameof(AITriggersWindow);
            base.Initialize();

            lbAITriggers = FindChild<EditorListBox>(nameof(lbAITriggers));
            ddActions = FindChild<XNADropDown>(nameof(ddActions));
            tbName = FindChild<EditorTextBox>(nameof(tbName));
            ddSide = FindChild<XNADropDown>(nameof(ddSide));
            ddHouseType = FindChild<XNADropDown>(nameof(ddHouseType));
            ddConditionType = FindChild<XNADropDown>(nameof(ddConditionType));
            ddComparator = FindChild<XNADropDown>(nameof(ddComparator));
            tbQuantity = FindChild<EditorNumberTextBox>(nameof(tbQuantity));
            selComparisonObjectType = FindChild<EditorPopUpSelector>(nameof(selComparisonObjectType));
            selPrimaryTeam = FindChild<EditorPopUpSelector>(nameof(selPrimaryTeam));
            selSecondaryTeam = FindChild<EditorPopUpSelector>(nameof(selSecondaryTeam));
            tbInitial = FindChild<EditorNumberTextBox>(nameof(tbInitial));
            tbMinimum = FindChild<EditorNumberTextBox>(nameof(tbMinimum));
            tbMaximum = FindChild<EditorNumberTextBox>(nameof(tbMaximum));
            chkEnabledOnEasy = FindChild<XNACheckBox>(nameof(chkEnabledOnEasy));
            chkEnabledOnMedium = FindChild<XNACheckBox>(nameof(chkEnabledOnMedium));
            chkEnabledOnHard = FindChild<XNACheckBox>(nameof(chkEnabledOnHard));

            FindChild<EditorButton>("btnNew").LeftClick += BtnNew_LeftClick;
            FindChild<EditorButton>("btnDelete").LeftClick += BtnDelete_LeftClick;
            FindChild<EditorButton>("btnClone").LeftClick += BtnClone_LeftClick;

            FindChild<EditorButton>("btnOpenPrimaryTeam").LeftClick += BtnOpenPrimaryTeam_LeftClick;
            FindChild<EditorButton>("btnOpenSecondaryTeam").LeftClick += BtnOpenSecondaryTeam_LeftClick;

            selectTeamTypeWindow = new SelectTeamTypeWindow(WindowManager, map);
            selectTeamTypeWindow.IncludeNone = true;
            var teamTypeWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTeamTypeWindow);
            teamTypeWindowDarkeningPanel.Hidden += TeamTypeWindowDarkeningPanel_Hidden;

            selectTechnoTypeWindow = new SelectTechnoTypeWindow(WindowManager, map);
            selectTechnoTypeWindow.IncludeNone = true;
            var technoTypeDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTechnoTypeWindow);
            technoTypeDarkeningPanel.Hidden += TechnoTypeDarkeningPanel_Hidden;

            ddActions.SelectedIndexChanged += DdActions_SelectedIndexChanged;            

            lbAITriggers.SelectedIndexChanged += LbAITriggers_SelectedIndexChanged;

            RefreshLanguage();
        }

        public void RefreshLanguage()
        {
            // 汉化按钮和标签
            var btnNew = FindChild<EditorButton>("btnNew");
            if (btnNew != null)
                btnNew.Text = TSMapEditor.UI.MainMenu.IsChinese ? "新建" : "New";
            var btnDelete = FindChild<EditorButton>("btnDelete");
            if (btnDelete != null)
                btnDelete.Text = TSMapEditor.UI.MainMenu.IsChinese ? "删除" : "Delete";
            var btnClone = FindChild<EditorButton>("btnClone");
            if (btnClone != null)
                btnClone.Text = TSMapEditor.UI.MainMenu.IsChinese ? "克隆" : "Clone";
            var btnOpenPrimary = FindChild<EditorButton>("btnOpenPrimaryTeam");
            if (btnOpenPrimary != null)
                btnOpenPrimary.Text = TSMapEditor.UI.MainMenu.IsChinese ? "打开主队" : ">";
            var btnOpenSecondary = FindChild<EditorButton>("btnOpenSecondaryTeam");
            if (btnOpenSecondary != null)
                btnOpenSecondary.Text = TSMapEditor.UI.MainMenu.IsChinese ? "打开副队" : ">";

            // 下拉菜单
            ddActions.Items.Clear();
            ddActions.AddItem(TSMapEditor.UI.MainMenu.IsChinese ? "高级..." : "Advanced...");
            ddActions.AddItem(new XNADropDownItem() { Text = TSMapEditor.UI.MainMenu.IsChinese ? "为低难度克隆" : "Clone for Easier Difficulties", Tag = new Action(CloneForEasierDifficulties) });
            ddActions.SelectedIndex = 0;

            // 复选框
            if (chkEnabledOnEasy != null)
                chkEnabledOnEasy.Text = TSMapEditor.UI.MainMenu.IsChinese ? "简单可用" : "Available on Easy";
            if (chkEnabledOnMedium != null)
                chkEnabledOnMedium.Text = TSMapEditor.UI.MainMenu.IsChinese ? "中等可用" : "Available on Medium";
            if (chkEnabledOnHard != null)
                chkEnabledOnHard.Text = TSMapEditor.UI.MainMenu.IsChinese ? "困难可用" : "Available on Hard";

            // 标签
            var lblHeader = FindChild<XNALabel>("lblHeader");
            if (lblHeader != null)
                lblHeader.Text = TSMapEditor.UI.MainMenu.IsChinese ? "本地AI触发器" : "LOCAL AI TRIGGERS";
            var lblDesc = FindChild<XNALabel>("lblDescription");
            if (lblDesc != null)
                lblDesc.Text = TSMapEditor.UI.MainMenu.IsChinese ? "AI触发器允许AI动态生成TeamTypes。" : "AI Triggers allow the AI to dynamically build TeamTypes.";
            var lblAITriggers = FindChild<XNALabel>("lblAITriggers");
            if (lblAITriggers != null)
                lblAITriggers.Text = TSMapEditor.UI.MainMenu.IsChinese ? "AI触发器:" : "AI Triggers:";
            var lblSelected = FindChild<XNALabel>("lblSelectedAITrigger");
            if (lblSelected != null)
                lblSelected.Text = TSMapEditor.UI.MainMenu.IsChinese ? "已选AI触发器:" : "Selected AI Trigger:";
            var lblName = FindChild<XNALabel>("lblName");
            if (lblName != null)
                lblName.Text = TSMapEditor.UI.MainMenu.IsChinese ? "名称:" : "Name:";
            var lblSide = FindChild<XNALabel>("lblSide");
            if (lblSide != null)
                lblSide.Text = TSMapEditor.UI.MainMenu.IsChinese ? "势力:" : "Side:";
            var lblHouse = FindChild<XNALabel>("lblHouse");
            if (lblHouse != null)
                lblHouse.Text = TSMapEditor.UI.MainMenu.IsChinese ? "所属方:" : "House:";
            var lblConditionHeader = FindChild<XNALabel>("lblConditionHeader");
            if (lblConditionHeader != null)
                lblConditionHeader.Text = TSMapEditor.UI.MainMenu.IsChinese ? "条件" : "Condition";
            var lblConditionType = FindChild<XNALabel>("lblConditionType");
            if (lblConditionType != null)
                lblConditionType.Text = TSMapEditor.UI.MainMenu.IsChinese ? "类型:" : "Type:";
            var lblComparator = FindChild<XNALabel>("lblComparator");
            if (lblComparator != null)
                lblComparator.Text = TSMapEditor.UI.MainMenu.IsChinese ? "比较符:" : "Comparator:";
            var lblQuantity = FindChild<XNALabel>("lblQuantity");
            if (lblQuantity != null)
                lblQuantity.Text = TSMapEditor.UI.MainMenu.IsChinese ? "数量:" : "Quantity:";
            var lblComparisonObjectType = FindChild<XNALabel>("lblComparisonObjectType");
            if (lblComparisonObjectType != null)
                lblComparisonObjectType.Text = TSMapEditor.UI.MainMenu.IsChinese ? "对象类型:" : "Object Type:";
            var lblTeamsHeader = FindChild<XNALabel>("lblTeamsHeader");
            if (lblTeamsHeader != null)
                lblTeamsHeader.Text = TSMapEditor.UI.MainMenu.IsChinese ? "队伍类型" : "TeamTypes";
            var lblPrimary = FindChild<XNALabel>("lblPrimaryTeam");
            if (lblPrimary != null)
                lblPrimary.Text = TSMapEditor.UI.MainMenu.IsChinese ? "主队:" : "Primary:";
            var lblSecondary = FindChild<XNALabel>("lblSecondaryTeam");
            if (lblSecondary != null)
                lblSecondary.Text = TSMapEditor.UI.MainMenu.IsChinese ? "副队(可选):" : "Secondary (opt.):";
            var lblWeightHeader = FindChild<XNALabel>("lblWeightHeader");
            if (lblWeightHeader != null)
                lblWeightHeader.Text = TSMapEditor.UI.MainMenu.IsChinese ? "权重" : "Weights";
            var lblInitial = FindChild<XNALabel>("lblInitial");
            if (lblInitial != null)
                lblInitial.Text = TSMapEditor.UI.MainMenu.IsChinese ? "初始:" : "Initial:";
            var lblMinimum = FindChild<XNALabel>("lblMinimum");
            if (lblMinimum != null)
                lblMinimum.Text = TSMapEditor.UI.MainMenu.IsChinese ? "最小:" : "Min.:";
            var lblMaximum = FindChild<XNALabel>("lblMaximum");
            if (lblMaximum != null)
                lblMaximum.Text = TSMapEditor.UI.MainMenu.IsChinese ? "最大:" : "Max.:";

            // Store current selections before refreshing dropdowns
            string currentHouseININame = editedAITrigger?.OwnerName;
            int currentSideIndex = editedAITrigger?.Side ?? -1;
            int currentComparatorIndex = (int)(editedAITrigger?.Comparator.ComparatorOperator ?? 0); // 保存当前比较符的索引

            RefreshHouses(); // Repopulate ddHouseType with translated names
            RefreshSideDropdown(); // Repopulate ddSide with translated names
            RefreshComparators(); // Repopulate ddComparator with translated names

            // Restore ddHouseType selection
            if (currentHouseININame != null)
            {
                HouseType houseType = map.GetHouseTypes().Find(h => h.ININame == currentHouseININame);
                if (houseType != null && ShouldDisplayHouse(houseType))
                {
                    ddHouseType.SelectedIndex = ddHouseType.Items.FindIndex(item => item.Text == GetHouseDisplayName(houseType));
                }
                else
                {
                    ddHouseType.SelectedIndex = 0; // Default to "<all>" if no matching house found or no longer displayable
                }
            }
            else
            {
                ddHouseType.SelectedIndex = 0; // Default to "<all>" if no house was previously selected
            }

            // Restore ddSide selection
            if (currentSideIndex != -1 && currentSideIndex < ddSide.Items.Count)
            {
                ddSide.SelectedIndex = currentSideIndex;
            }
            else
            {
                ddSide.SelectedIndex = 0; // Default to "0 all sides" if no side was previously selected
            }

            // If an AI trigger is currently selected, re-edit it to refresh its displayed properties (e.g., TeamType names)
            if (editedAITrigger != null)
            {
                EditAITrigger(editedAITrigger);
            }

            // Restore ddComparator selection
            if (currentComparatorIndex != -1 && currentComparatorIndex < ddComparator.Items.Count)
            {
                ddComparator.SelectedIndex = currentComparatorIndex;
            }
            else
            {
                ddComparator.SelectedIndex = 0; // Default to "00 less than" if no comparator was previously selected
            }
        }

        private void RefreshComparators()
        {
            ddComparator.Items.Clear();
            string[] chineseComparators = new string[]
            {
                "00 小于",
                "01 小于等于",
                "02 等于",
                "03 大于等于",
                "04 大于",
                "05 不等于"
            };

            string[] englishComparators = new string[]
            {
                "00 less than",
                "01 less than or equal to",
                "02 equal to",
                "03 more than or equal to",
                "04 more than",
                "05 not equal to"
            };

            for (int i = 0; i < chineseComparators.Length; i++)
            {
                ddComparator.AddItem(TSMapEditor.UI.MainMenu.IsChinese ? chineseComparators[i] : englishComparators[i]);
            }
        }

        private void CloneForEasierDifficulties()
        {
            if (editedAITrigger == null)
                return;

            string title, content;
            if (TSMapEditor.UI.MainMenu.IsChinese)
            {
                title = "确认操作？";
                content = "为低难度克隆此AI触发器将为中等和简单难度创建副本，分别设置难度为中等和简单。\n" +
                          "当前AI触发器的难度将仅设为困难。\n\n" +
                          "如果AI触发器引用了主队或副队，这些队伍及其特遣队也会为低难度自动克隆。\n" +
                          "若这些副本已存在，则会直接引用对应队伍。\n\n" +
                          "脚本假定AI触发器及其队伍、特遣队名称中包含'H'或'Hard'字样。\n\n" +
                          "此操作不可撤销，是否继续？";
            }
            else
            {
                title = "Are you sure?";
                content = "Cloning this AI trigger for easier difficulties will create duplicate instances" + Environment.NewLine +
                    "of this AI trigger for Medium and Easy difficulties, setting the difficulty" + Environment.NewLine +
                    "setting for each AI trigger to Medium and Easy, respectively." + Environment.NewLine +
                    "This will set the current AI trigger's difficulty to Hard only." + Environment.NewLine + Environment.NewLine +
                    "In case the AI trigger references a Primary or Secondary TeamTypes," + Environment.NewLine +
                    "those TeamTypes and their TaskForoces would be duplicated for easier difficulties." + Environment.NewLine +
                    "If those duplicates already exist, this action will set the AI triggers to use those " + Environment.NewLine +
                    "TeamTypes instead." + Environment.NewLine + Environment.NewLine +
                    "The script assumes that this AI Trigger has the words 'H' or 'Hard'" + Environment.NewLine +
                    "in their name and in their respective TeamTypes and TaskForces." + Environment.NewLine + Environment.NewLine +
                    "No un-do is available. Do you want to continue?";
            }

            var messageBox = EditorMessageBox.Show(WindowManager, title, content, MessageBoxButtons.YesNo);

            messageBox.YesClickedAction = _ => DoCloneForEasierDifficulties();
        }

        private void DoCloneForEasierDifficulties()
        {
            editedAITrigger.Hard = true;
            editedAITrigger.Medium = false;
            editedAITrigger.Easy = false;

            var mediumAITrigger = editedAITrigger.Clone(map.GetNewUniqueInternalId());
            mediumAITrigger.Hard = false;
            mediumAITrigger.Medium = true;
            mediumAITrigger.Name = Helpers.ConvertNameToNewDifficulty(editedAITrigger.Name, Difficulty.Hard, Difficulty.Medium);
            map.AITriggerTypes.Add(mediumAITrigger);

            var easyAITrigger = editedAITrigger.Clone(map.GetNewUniqueInternalId());
            easyAITrigger.Hard = false;
            easyAITrigger.Easy = true;
            easyAITrigger.Name = Helpers.ConvertNameToNewDifficulty(editedAITrigger.Name, Difficulty.Hard, Difficulty.Easy);
            map.AITriggerTypes.Add(easyAITrigger);

            CloneTeamTypesAndAttachToAITrigger(mediumAITrigger, Difficulty.Medium, true);
            CloneTeamTypesAndAttachToAITrigger(mediumAITrigger, Difficulty.Medium, false);
            CloneTeamTypesAndAttachToAITrigger(easyAITrigger, Difficulty.Easy, true);
            CloneTeamTypesAndAttachToAITrigger(easyAITrigger, Difficulty.Easy, false);

            ListAITriggers();
        }

        private void CloneTeamTypesAndAttachToAITrigger(AITriggerType newAITrigger, Difficulty difficulty, bool isPrimaryTeamType)
        {
            var teamTypeToClone = isPrimaryTeamType ? editedAITrigger.PrimaryTeam : editedAITrigger.SecondaryTeam;

            if (teamTypeToClone == null)
                return;

            // Check if its lower difficulty counterpart already exists, if it doesn't, create it
            string newDifficultyName = Helpers.ConvertNameToNewDifficulty(teamTypeToClone.Name, Difficulty.Hard, difficulty);            

            var newDifficultyTeamType = map.TeamTypes.Find(teamType => teamType.Name == newDifficultyName);            

            if (newDifficultyTeamType == null)
            {
                // clone the teams type and give it a new name
                newDifficultyTeamType = teamTypeToClone.Clone(map.GetNewUniqueInternalId());
                newDifficultyTeamType.Name = newDifficultyName;

                map.AddTeamType(newDifficultyTeamType);                

                // If the team type has a task force, check if it has lower difficulty duplicate; if not, create it
                if (teamTypeToClone.TaskForce != null)
                {
                    var taskForceToClone = teamTypeToClone.TaskForce;

                    newDifficultyName = Helpers.ConvertNameToNewDifficulty(taskForceToClone.Name, Difficulty.Hard, difficulty);

                    var newDifficultyTaskForce = map.TaskForces.Find(taskForce => taskForce.Name == newDifficultyName);                    

                    if (newDifficultyTaskForce == null)
                    {
                        newDifficultyTaskForce = taskForceToClone.Clone(map.GetNewUniqueInternalId());
                        newDifficultyTaskForce.Name = newDifficultyName;

                        map.AddTaskForce(newDifficultyTaskForce);                        
                    }

                    newDifficultyTeamType.TaskForce = newDifficultyTaskForce;
                }
            }

            // Regardless: assign to relevant team to the new AI trigger
            if (isPrimaryTeamType)
            {
                newAITrigger.PrimaryTeam = newDifficultyTeamType;                
            }
            else
            {
                newAITrigger.SecondaryTeam = newDifficultyTeamType;                
            }
        }

        private void DdActions_SelectedIndexChanged(object sender, EventArgs e)
        {
            var item = ddActions.SelectedItem;
            if (item == null)
                return;

            if (item.Tag == null)
                return;

            if (item.Tag is Action action)
                action();

            ddActions.SelectedIndexChanged -= DdActions_SelectedIndexChanged;
            ddActions.SelectedIndex = 0;
            ddActions.SelectedIndexChanged += DdActions_SelectedIndexChanged;
        }

        private void BtnOpenPrimaryTeam_LeftClick(object sender, EventArgs e)
        {
            if (editedAITrigger == null || editedAITrigger.PrimaryTeam == null)
                return;

            OpenTeamType(editedAITrigger.PrimaryTeam);
        }

        private void BtnOpenSecondaryTeam_LeftClick(object sender, EventArgs e)
        {
            if (editedAITrigger == null || editedAITrigger.SecondaryTeam == null)
                return;

            OpenTeamType(editedAITrigger.SecondaryTeam);
        }

        private void OpenTeamType(TeamType teamType)
        {
            TeamTypeOpened?.Invoke(this, new TeamTypeEventArgs(teamType));
            PutOnBackground();
        }

        private void BtnNew_LeftClick(object sender, EventArgs e)
        {
            var aiTrigger = new AITriggerType(map.GetNewUniqueInternalId());
            aiTrigger.Name = "New AITrigger";
            aiTrigger.OwnerName = "<all>";            
            map.AITriggerTypes.Add(aiTrigger);
            ListAITriggers();
            SelectAITrigger(aiTrigger);
        }

        private void BtnDelete_LeftClick(object sender, EventArgs e)
        {
            if (editedAITrigger == null)
                return;

            map.AITriggerTypes.Remove(editedAITrigger);
            editedAITrigger = null;

            ListAITriggers();
        }

        private void BtnClone_LeftClick(object sender, EventArgs e)
        {
            if (editedAITrigger == null)
                return;

            var clone = editedAITrigger.Clone(map.GetNewUniqueInternalId());
            map.AITriggerTypes.Add(clone);
            ListAITriggers();
            SelectAITrigger(clone);
        }

        private void SelectAITrigger(AITriggerType aiTrigger)
        {
            lbAITriggers.SelectedIndex = lbAITriggers.Items.FindIndex(item => item.Tag == aiTrigger);

            if (lbAITriggers.LastIndex < lbAITriggers.SelectedIndex)
                lbAITriggers.ScrollToBottom(); // TODO we don't actually have a good way to scroll the listbox into a specific place right now
            else if (lbAITriggers.TopIndex > lbAITriggers.SelectedIndex)
                lbAITriggers.TopIndex = lbAITriggers.SelectedIndex;
        }

        private void TeamTypeWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectTeamTypeWindow.IsForSecondaryTeam)
            {
                editedAITrigger.SecondaryTeam = selectTeamTypeWindow.SelectedObject;
            }
            else
            {
                editedAITrigger.PrimaryTeam = selectTeamTypeWindow.SelectedObject;
            }

            EditAITrigger(editedAITrigger);
        }

        private void TechnoTypeDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            editedAITrigger.ConditionObject = selectTechnoTypeWindow.SelectedObject;

            EditAITrigger(editedAITrigger);
        }

        private void LbAITriggers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbAITriggers.SelectedItem == null)
            {
                EditAITrigger(null);
                return;
            }

            EditAITrigger((AITriggerType)lbAITriggers.SelectedItem.Tag);
        }

        private void EditAITrigger(AITriggerType aiTriggerType)
        {
            tbName.TextChanged -= TbName_TextChanged;
            ddSide.SelectedIndexChanged -= DdSide_SelectedIndexChanged;
            ddHouseType.SelectedIndexChanged -= DdHouse_SelectedIndexChanged;
            ddConditionType.SelectedIndexChanged -= DdConditionType_SelectedIndexChanged;
            ddComparator.SelectedIndexChanged -= DdComparator_SelectedIndexChanged;
            tbQuantity.TextChanged -= TbQuantity_TextChanged;
            selComparisonObjectType.LeftClick -= SelComparisonObjectType_LeftClick;
            selPrimaryTeam.LeftClick -= SelPrimaryTeam_LeftClick;
            selSecondaryTeam.LeftClick -= SelSecondaryTeam_LeftClick;
            tbInitial.TextChanged -= TbInitial_TextChanged;
            tbMinimum.TextChanged -= TbMinimum_TextChanged;
            tbMaximum.TextChanged -= TbMaximum_TextChanged;
            chkEnabledOnEasy.CheckedChanged -= ChkEnabledOnEasy_CheckedChanged;
            chkEnabledOnMedium.CheckedChanged -= ChkEnabledOnMedium_CheckedChanged;
            chkEnabledOnHard.CheckedChanged -= ChkEnabledOnHard_CheckedChanged;

            editedAITrigger = aiTriggerType;

            if (editedAITrigger == null)
            {
                tbName.Text = string.Empty;
                ddSide.SelectedIndex = -1;
                ddHouseType.SelectedIndex = -1;
                ddConditionType.SelectedIndex = -1;
                ddComparator.SelectedIndex = -1;
                tbQuantity.Text = string.Empty;
                selComparisonObjectType.Text = string.Empty;
                selComparisonObjectType.Tag = null;
                selPrimaryTeam.Text = string.Empty;
                selSecondaryTeam.Text = string.Empty;
                selPrimaryTeam.Tag = null;
                selSecondaryTeam.Tag = null;
                tbInitial.Text = string.Empty;
                tbMinimum.Text = string.Empty;
                tbMaximum.Text = string.Empty;
                chkEnabledOnEasy.Checked = false;
                chkEnabledOnMedium.Checked = false;
                chkEnabledOnHard.Checked = false;
                return;
            }

            tbName.Text = editedAITrigger.Name;
            ddSide.SelectedIndex = editedAITrigger.Side < ddSide.Items.Count ? editedAITrigger.Side : 0;
            HouseType houseType = map.GetHouseTypes().Find(h => h.ININame == editedAITrigger.OwnerName);
            if (houseType != null && ShouldDisplayHouse(houseType))
            {
                ddHouseType.SelectedIndex = ddHouseType.Items.FindIndex(item => item.Text == GetHouseDisplayName(houseType));
            }
            else
            {
                ddHouseType.SelectedIndex = -1;
            }
            ddConditionType.SelectedIndex = ((int)aiTriggerType.ConditionType + 1);
            ddComparator.SelectedIndex = (int)aiTriggerType.Comparator.ComparatorOperator;
            tbQuantity.Value = aiTriggerType.Comparator.Quantity;
            selComparisonObjectType.Text = aiTriggerType.ConditionObject != null ? $"{aiTriggerType.ConditionObject.GetEditorDisplayName()} ({aiTriggerType.ConditionObject.ININame})" : string.Empty;
            selComparisonObjectType.Tag = aiTriggerType.ConditionObject;
            selPrimaryTeam.Text = aiTriggerType.PrimaryTeam != null ? aiTriggerType.PrimaryTeam.GetDisplayName() : string.Empty;
            selPrimaryTeam.Tag = aiTriggerType.PrimaryTeam;
            selSecondaryTeam.Text = aiTriggerType.SecondaryTeam != null ? aiTriggerType.SecondaryTeam.GetDisplayName() : string.Empty;
            selSecondaryTeam.Tag = aiTriggerType.SecondaryTeam;
            tbInitial.DoubleValue = aiTriggerType.InitialWeight;
            tbMinimum.DoubleValue = aiTriggerType.MinimumWeight;
            tbMaximum.DoubleValue = aiTriggerType.MaximumWeight;
            chkEnabledOnEasy.Checked = aiTriggerType.Easy;
            chkEnabledOnMedium.Checked = aiTriggerType.Medium;
            chkEnabledOnHard.Checked = aiTriggerType.Hard;

            tbName.TextChanged += TbName_TextChanged;
            ddSide.SelectedIndexChanged += DdSide_SelectedIndexChanged;
            ddHouseType.SelectedIndexChanged += DdHouse_SelectedIndexChanged;
            ddConditionType.SelectedIndexChanged += DdConditionType_SelectedIndexChanged;
            ddComparator.SelectedIndexChanged += DdComparator_SelectedIndexChanged;
            tbQuantity.TextChanged += TbQuantity_TextChanged;
            selComparisonObjectType.LeftClick += SelComparisonObjectType_LeftClick;
            selPrimaryTeam.LeftClick += SelPrimaryTeam_LeftClick;
            selSecondaryTeam.LeftClick += SelSecondaryTeam_LeftClick;
            tbInitial.TextChanged += TbInitial_TextChanged;
            tbMinimum.TextChanged += TbMinimum_TextChanged;
            tbMaximum.TextChanged += TbMaximum_TextChanged;
            chkEnabledOnEasy.CheckedChanged += ChkEnabledOnEasy_CheckedChanged;
            chkEnabledOnMedium.CheckedChanged += ChkEnabledOnMedium_CheckedChanged;
            chkEnabledOnHard.CheckedChanged += ChkEnabledOnHard_CheckedChanged;
        }

        private void TbName_TextChanged(object sender, EventArgs e)
        {
            editedAITrigger.Name = tbName.Text;
            lbAITriggers.SelectedItem.Text = tbName.Text;
        }

        private void DdSide_SelectedIndexChanged(object sender, EventArgs e)
        {
            editedAITrigger.Side = ddSide.SelectedIndex;
            lbAITriggers.SelectedItem.TextColor = GetAITriggerUIColor(editedAITrigger);
        }

        private void DdHouse_SelectedIndexChanged(object sender, EventArgs e)
        {
            editedAITrigger.OwnerName = ddHouseType.SelectedItem.Text;
            lbAITriggers.SelectedItem.TextColor = GetAITriggerUIColor(editedAITrigger);
        }

        private void DdConditionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            editedAITrigger.ConditionType = (AITriggerConditionType)(ddConditionType.SelectedIndex - 1);
        }

        private void DdComparator_SelectedIndexChanged(object sender, EventArgs e)
        {
            editedAITrigger.Comparator = new AITriggerComparator((AITriggerComparatorOperator)ddComparator.SelectedIndex, editedAITrigger.Comparator.Quantity);
        }

        private void TbQuantity_TextChanged(object sender, EventArgs e)
        {
            editedAITrigger.Comparator = new AITriggerComparator(editedAITrigger.Comparator.ComparatorOperator, tbQuantity.Value);
        }

        private void SelComparisonObjectType_LeftClick(object sender, EventArgs e)
        {
            selectTechnoTypeWindow.Open(editedAITrigger.ConditionObject);
        }

        private void SelPrimaryTeam_LeftClick(object sender, EventArgs e)
        {
            selectTeamTypeWindow.IsForSecondaryTeam = false;
            selectTeamTypeWindow.Open(editedAITrigger.PrimaryTeam);
        }

        private void SelSecondaryTeam_LeftClick(object sender, EventArgs e)
        {
            selectTeamTypeWindow.IsForSecondaryTeam = true;
            selectTeamTypeWindow.Open(editedAITrigger.SecondaryTeam);
        }

        private void TbInitial_TextChanged(object sender, EventArgs e)
        {
            editedAITrigger.InitialWeight = tbInitial.DoubleValue;
        }

        private void TbMinimum_TextChanged(object sender, EventArgs e)
        {
            editedAITrigger.MinimumWeight = tbMinimum.DoubleValue;
        }

        private void TbMaximum_TextChanged(object sender, EventArgs e)
        {
            editedAITrigger.MaximumWeight = tbMaximum.DoubleValue;
        }

        private void ChkEnabledOnEasy_CheckedChanged(object sender, EventArgs e)
        {
            editedAITrigger.Easy = chkEnabledOnEasy.Checked;
        }

        private void ChkEnabledOnMedium_CheckedChanged(object sender, EventArgs e)
        {
            editedAITrigger.Medium = chkEnabledOnMedium.Checked;
        }

        private void ChkEnabledOnHard_CheckedChanged(object sender, EventArgs e)
        {
            editedAITrigger.Hard = chkEnabledOnHard.Checked;
        }

        public void Open()
        {
            ListAITriggers();
            RefreshHouses(); // 确保窗口打开时，所属方下拉框就能被正确汉化并显示
            RefreshSideDropdown(); // 确保窗口打开时，势力下拉框也能被正确汉化并显示
            Show();
        }

        private void ListAITriggers()
        {
            lbAITriggers.Clear();
            // ddSide.Items.Clear(); // 移除此处对 ddSide 的清空
            // ddHouseType.Items.Clear(); // 移除此处对 ddHouseType 的清空

            map.AITriggerTypes.ForEach(aitt =>
            {
                lbAITriggers.AddItem(new XNAListBoxItem() { Text = aitt.Name, Tag = aitt, TextColor = GetAITriggerUIColor(aitt) });
            });

            // ddSide.AddItem(TSMapEditor.UI.MainMenu.IsChinese ? "全部势力" : "0 all sides"); // 移除 ddSide 的填充逻辑
            // for (int i = 0; i < map.Rules.Sides.Count; i++)
            // {
            //     string sideName = map.Rules.Sides[i];
            //     string displayName = TSMapEditor.UI.MainMenu.IsChinese && TSMapEditor.Translations.CategoryNameManager.TryGetCategoryNameTranslation(sideName, out string cn) ? cn : sideName;
            //     ddSide.AddItem($"{i + 1} {displayName}");
            // }

            // ddHouseType.AddItem("<all>"); // 移除 ddHouseType 的填充逻辑
            // map.GetHouseTypes().ForEach(houseType => ddHouseType.AddItem(houseType.ININame, Helpers.GetHouseTypeUITextColor(houseType))); // 移除 ddHouseType 的填充逻辑

            LbAITriggers_SelectedIndexChanged(this, EventArgs.Empty);
        }

        private Color GetAITriggerUIColor(AITriggerType aitt)
        {
            if (!string.IsNullOrWhiteSpace(aitt.OwnerName))
            {
                var houseType = map.FindHouseType(aitt.OwnerName);
                if (houseType != null)
                {
                    return Helpers.GetHouseTypeUITextColor(houseType);
                }
            }

            if (aitt.Side > 0)
            {
                string sideName = aitt.Side > 0 && aitt.Side - 1 < map.Rules.Sides.Count ? map.Rules.Sides[aitt.Side - 1] : null;
                if (sideName != null)
                {
                    var houseTypeFromSide = map.GetHouseTypes().Find(ht => ht.Side == sideName);

                    if (houseTypeFromSide != null)
                    {
                        return Helpers.GetHouseTypeUITextColor(houseTypeFromSide);
                    }
                }
            }

            return UISettings.ActiveSettings.AltColor;
        }

        // 触发窗口同款过滤和翻译方法
        private bool ShouldDisplayHouse(HouseType ht)
        {
            return TSMapEditor.UI.Windows.TriggersWindow.UniversalHouseNames.Contains(ht.ININame) || TSMapEditor.Translations.CategoryNameManager.TryGetCategoryNameTranslation(ht.ININame, out _);
        }
        private string GetHouseDisplayName(HouseType ht)
        {
            if (TSMapEditor.UI.MainMenu.IsChinese && TSMapEditor.Translations.CategoryNameManager.TryGetCategoryNameTranslation(ht.ININame, out string translatedName))
            {
                return translatedName;
            }
            return ht.ININame;
        }

        private void RefreshHouses()
        {
            ddHouseType.Items.Clear();
            // 添加 <all> 选项，并进行汉化
            ddHouseType.AddItem(TSMapEditor.UI.MainMenu.IsChinese ? "<全部>" : "<all>");
            foreach (var ht in map.GetHouseTypes())
            {
                if (ShouldDisplayHouse(ht))
                {
                    ddHouseType.AddItem(GetHouseDisplayName(ht), Helpers.GetHouseTypeUITextColor(ht));
                }
            }
        }

        private void RefreshSideDropdown()
        {
            ddSide.Items.Clear();
            // 添加 "全部势力" 选项，并进行汉化
            ddSide.AddItem(TSMapEditor.UI.MainMenu.IsChinese ? "全部势力" : "0 all sides");

            var sides = map.Rules.Sides;
            for (int i = 0; i < sides.Count; i++)
            {
                string sideName = sides[i];
                string displayName = TSMapEditor.UI.MainMenu.IsChinese && TSMapEditor.Translations.CategoryNameManager.TryGetCategoryNameTranslation(sideName, out string cn) ? cn : sideName;
                ddSide.AddItem($"{i + 1} {displayName}");
            }
        }

        private void RefreshConditionTypes()
        {
            ddConditionType.Items.Clear();

            foreach (AITriggerConditionType type in Enum.GetValues(typeof(AITriggerConditionType)))
            {
                string englishText = "";
                string chineseText = "";

                switch (type)
                {
                    case AITriggerConditionType.None:
                        englishText = "-1 None";
                        chineseText = "-1 无";
                        break;
                    case AITriggerConditionType.EnemyOwns:
                        englishText = "0 Enemy Owns";
                        chineseText = "0 敌人拥有";
                        break;
                    case AITriggerConditionType.OwnerOwns:
                        englishText = "1 House Owns";
                        chineseText = "1 所属方拥有";
                        break;
                    case AITriggerConditionType.EnemyOnYellowPower:
                        englishText = "2 Enemy On Yellow Power";
                        chineseText = "2 敌人黄电";
                        break;
                    case AITriggerConditionType.EnemyOnRedPower:
                        englishText = "3 Enemy On Red Power";
                        chineseText = "3 敌人红电";
                        break;
                    case AITriggerConditionType.EnemyHasCredits:
                        englishText = "4 Enemy Has X Credits"; // 修正为 EnemyHasCredits
                        chineseText = "4 敌人有X金钱";
                        break;
                    case AITriggerConditionType.OwnerHasIronCurtainReady:
                        englishText = "5 Owner Has Iron Curtain Ready";
                        chineseText = "5 所属方铁幕就绪";
                        break;
                    case AITriggerConditionType.OwnerHasChronosphereReady:
                        englishText = "6 Owner Has Chronosphere Ready";
                        chineseText = "6 所属方时间停止就绪";
                        break;
                    case AITriggerConditionType.NeutralHouseOwns:
                        englishText = "7 Neutral House Owns";
                        chineseText = "7 中立所属方拥有";
                        break;
                    // 移除其他未在枚举中定义的 case 语句
                    default:
                        englishText = type.ToString();
                        chineseText = type.ToString();
                        break;
                }

                ddConditionType.AddItem(TSMapEditor.UI.MainMenu.IsChinese ? chineseText : englishText);
            }
        }
    }
}
