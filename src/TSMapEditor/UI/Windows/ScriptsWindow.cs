using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TSMapEditor.CCEngine;
using TSMapEditor.Misc;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.Rendering;
using TSMapEditor.Settings;
using TSMapEditor.UI.Controls;
using TSMapEditor.UI.CursorActions;
using TSMapEditor.UI.Notifications;

namespace TSMapEditor.UI.Windows
{
    public enum ScriptSortMode
    {
        ID,
        Name,
        Color,
        ColorThenName,
    }

    /// <summary>
    /// A window that allows the user to edit map scripts.
    /// </summary>
    public class ScriptsWindow : INItializableWindow
    {
        public ScriptsWindow(WindowManager windowManager, Map map, EditorState editorState,
            INotificationManager notificationManager, ICursorActionTarget cursorActionTarget) : base(windowManager)
        {
            this.map = map;
            this.editorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
            this.notificationManager = notificationManager ?? throw new ArgumentNullException(nameof(notificationManager));
            selectCellCursorAction = new SelectCellCursorAction(cursorActionTarget);
        }

        private readonly Map map;
        private readonly EditorState editorState;
        private readonly INotificationManager notificationManager;
        private SelectCellCursorAction selectCellCursorAction;

        private EditorListBox lbScriptTypes;
        private EditorSuggestionTextBox tbFilter;
        private EditorTextBox tbName;
        private EditorListBox lbActions;
        private EditorPopUpSelector selTypeOfAction;
        private XNALabel lblParameterDescription;
        private EditorNumberTextBox tbParameterValue;
        private MenuButton btnEditorPresetValues;
        private EditorButton btnEditorPresetValuesWindow;
        private XNALabel lblActionDescriptionValue;
        private XNADropDown ddScriptColor;

        private SelectScriptActionWindow selectScriptActionWindow;
        private SelectScriptActionPresetOptionWindow selectScriptActionPresetOptionWindow;
        private EditorContextMenu actionListContextMenu;

        private SelectBuildingTargetWindow selectBuildingTargetWindow;
        private SelectAnimationWindow selectAnimationWindow;

        private Script editedScript;

        private bool isAddingAction = false;
        private int insertIndex = -1;

        private ScriptSortMode _scriptSortMode;
        private ScriptSortMode ScriptSortMode
        {
            get => _scriptSortMode;
            set
            {
                if (value != _scriptSortMode)
                {
                    _scriptSortMode = value;
                    ListScripts();
                }
            }
        }

        public override void Initialize()
        {
            Name = nameof(ScriptsWindow);
            base.Initialize();

            lbScriptTypes = FindChild<EditorListBox>(nameof(lbScriptTypes));
            tbFilter = FindChild<EditorSuggestionTextBox>(nameof(tbFilter));            
            tbName = FindChild<EditorTextBox>(nameof(tbName));
            lbActions = FindChild<EditorListBox>(nameof(lbActions));
            selTypeOfAction = FindChild<EditorPopUpSelector>(nameof(selTypeOfAction));
            lblParameterDescription = FindChild<XNALabel>(nameof(lblParameterDescription));
            tbParameterValue = FindChild<EditorNumberTextBox>(nameof(tbParameterValue));
            btnEditorPresetValues = FindChild<MenuButton>(nameof(btnEditorPresetValues));
            btnEditorPresetValuesWindow = FindChild<EditorButton>(nameof(btnEditorPresetValuesWindow));
            lblActionDescriptionValue = FindChild<XNALabel>(nameof(lblActionDescriptionValue));
            ddScriptColor = FindChild<XNADropDown>(nameof(ddScriptColor));            

            ddScriptColor.AddItem(Translate(this, "None", "None"));
            Array.ForEach(Script.SupportedColors, supportedColor =>
            {
                ddScriptColor.AddItem(supportedColor.Name, supportedColor.Value);
            });

            tbFilter.TextChanged += TbFilter_TextChanged;

            var presetValuesContextMenu = new EditorContextMenu(WindowManager);
            presetValuesContextMenu.Width = 250;
            btnEditorPresetValues.ContextMenu = presetValuesContextMenu;
            btnEditorPresetValues.ContextMenu.OptionSelected += ContextMenu_OptionSelected;
            btnEditorPresetValues.LeftClick += BtnEditorPresetValues_LeftClick;

            btnEditorPresetValuesWindow.LeftClick += BtnEditorPresetValuesWindow_LeftClick;
            btnEditorPresetValuesWindow.Disable();

            tbName.TextChanged += TbName_TextChanged;
            tbParameterValue.TextChanged += TbParameterValue_TextChanged;
            lbScriptTypes.SelectedIndexChanged += LbScriptTypes_SelectedIndexChanged;
            lbActions.SelectedIndexChanged += LbActions_SelectedIndexChanged;

            var sortContextMenu = new EditorContextMenu(WindowManager);
            sortContextMenu.Name = nameof(sortContextMenu);
            sortContextMenu.Width = lbScriptTypes.Width;
            sortContextMenu.AddItem(Translate(this, "SortByID", "Sort by ID"), () => ScriptSortMode = ScriptSortMode.ID);
            sortContextMenu.AddItem(Translate(this, "SortByName", "Sort by Name"), () => ScriptSortMode = ScriptSortMode.Name);
            sortContextMenu.AddItem(Translate(this, "SortByColor", "Sort by Color"), () => ScriptSortMode = ScriptSortMode.Color);
            sortContextMenu.AddItem(Translate(this, "SortByColorName", "Sort by Color, then by Name"), () => ScriptSortMode = ScriptSortMode.ColorThenName);
            AddChild(sortContextMenu);

            FindChild<EditorButton>("btnSortOptions").LeftClick += (s, e) => sortContextMenu.Open(GetCursorPoint());

            var scriptContextMenu = new EditorContextMenu(WindowManager);
            scriptContextMenu.Name = nameof(scriptContextMenu);
            scriptContextMenu.Width = lbScriptTypes.Width;
            scriptContextMenu.AddItem(Translate(this, "ViewReferences", "View References"), ShowScriptReferences);
            AddChild(scriptContextMenu);

            lbScriptTypes.AllowRightClickUnselect = false;
            lbScriptTypes.RightClick += (s, e) =>
            {
                lbScriptTypes.SelectedIndex = lbScriptTypes.HoveredIndex;
                if (editedScript != null)
                    scriptContextMenu.Open(GetCursorPoint());
            };

            selectScriptActionWindow = new SelectScriptActionWindow(WindowManager, map.EditorConfig);
            var selectScriptActionDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectScriptActionWindow);
            selectScriptActionDarkeningPanel.Hidden += SelectScriptActionDarkeningPanel_Hidden;

            selectScriptActionPresetOptionWindow = new SelectScriptActionPresetOptionWindow(WindowManager, map);
            var selectScriptActionPresetDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectScriptActionPresetOptionWindow);
            selectScriptActionPresetDarkeningPanel.Hidden += SelectScriptActionPresetDarkeningPanel_Hidden;

            selectBuildingTargetWindow = new SelectBuildingTargetWindow(WindowManager, map);
            var buildingTargetWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectBuildingTargetWindow);
            buildingTargetWindowDarkeningPanel.Hidden += BuildingTargetWindowDarkeningPanel_Hidden;

            selectAnimationWindow = new SelectAnimationWindow(WindowManager, map);
            selectAnimationWindow.IncludeNone = false;
            var animationWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectAnimationWindow);
            animationWindowDarkeningPanel.Hidden += AnimationWindowDarkeningPanel_Hidden;

            selTypeOfAction.MouseLeftDown += SelTypeOfAction_MouseLeftDown;

            FindChild<EditorButton>("btnAddScript").LeftClick += BtnAddScript_LeftClick;
            FindChild<EditorButton>("btnDeleteScript").LeftClick += BtnDeleteScript_LeftClick;
            FindChild<EditorButton>("btnCloneScript").LeftClick += BtnCloneScript_LeftClick;
            FindChild<EditorButton>("btnAddAction").LeftClick += BtnAddAction_LeftClick;
            FindChild<EditorButton>("btnDeleteAction").LeftClick += BtnDeleteAction_LeftClick;
            FindChild<EditorButton>("btnInsertAction").LeftClick += (_, _) => InsertAction();
            FindChild<EditorButton>("btnCloneAction").LeftClick += (_, _) => CloneAction();
            FindChild<EditorButton>("btnMoveUp").LeftClick += (_, _) => MoveActionUp();
            FindChild<EditorButton>("btnMoveDown").LeftClick += (_, _) => MoveActionDown();

            selectCellCursorAction.CellSelected += SelectCellCursorAction_CellSelected;

            actionListContextMenu = new EditorContextMenu(WindowManager);
            actionListContextMenu.Name = nameof(actionListContextMenu);
            actionListContextMenu.Width = 180;
            actionListContextMenu.AddItem(Translate(this, "MoveUp", "Move Up"), MoveActionUp, () => editedScript != null && lbActions.SelectedItem != null && lbActions.SelectedIndex > 0);
            actionListContextMenu.AddItem(Translate(this, "MoveDown", "Move Down"), MoveActionDown, () => editedScript != null && lbActions.SelectedItem != null && lbActions.SelectedIndex < lbActions.Items.Count - 1);
            actionListContextMenu.AddItem(Translate(this, "CloneAction", "Clone Action"), CloneAction, () => editedScript != null && lbActions.SelectedItem != null);
            actionListContextMenu.AddItem(Translate(this, "InsertNewAction", "Insert New Action Here"), InsertAction, () => editedScript != null && lbActions.SelectedItem != null);
            actionListContextMenu.AddItem(Translate(this, "DeleteAction", "Delete Action"), ActionListContextMenu_Delete, () => editedScript != null && lbActions.SelectedItem != null);
            AddChild(actionListContextMenu);

            lbActions.AllowRightClickUnselect = false;
            lbActions.RightClick += (s, e) => { if (editedScript != null) { lbActions.SelectedIndex = lbActions.HoveredIndex; actionListContextMenu.Open(GetCursorPoint()); } };
        }

        private void AnimationWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (editedScript == null || lbActions.SelectedItem == null)
                return;

            if (selectAnimationWindow.SelectedObject != null)
            {
                editedScript.Actions[lbActions.SelectedIndex].Argument = selectAnimationWindow.SelectedObject.Index;
                RefreshParameterEntryText();
            }
        }

        private void BuildingTargetWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (editedScript == null || lbActions.SelectedItem == null)
                return;

            if (selectBuildingTargetWindow.SelectedObject > -1)
            {
                tbParameterValue.Text = GetBuildingWithPropertyText(selectBuildingTargetWindow.SelectedObject, selectBuildingTargetWindow.Property);
            }
        }

        private void MoveActionUp()
        {
            if (editedScript == null || lbActions.SelectedItem == null || lbActions.SelectedIndex <= 0)
                return;

            int viewTop = lbActions.ViewTop;
            editedScript.Actions.Swap(lbActions.SelectedIndex - 1, lbActions.SelectedIndex);
            EditScript(editedScript);
            lbActions.SelectedIndex--;
            lbActions.ViewTop = viewTop;
        }

        private void MoveActionDown()
        {
            if (editedScript == null || lbActions.SelectedItem == null || lbActions.SelectedIndex >= editedScript.Actions.Count - 1)
                return;

            int viewTop = lbActions.ViewTop;
            editedScript.Actions.Swap(lbActions.SelectedIndex, lbActions.SelectedIndex + 1);
            EditScript(editedScript);
            lbActions.SelectedIndex++;
            lbActions.ViewTop = viewTop;
        }

        private void CloneAction()
        {
            if (editedScript == null || lbActions.SelectedItem == null)
                return;

            int viewTop = lbActions.ViewTop;
            int index = lbActions.SelectedIndex + 1;

            var clonedEntry = editedScript.Actions[lbActions.SelectedIndex].Clone();

            // Smart script action cloning
            if (UserSettings.Instance.SmartScriptActionCloning || Keyboard.IsShiftHeldDown() || Keyboard.IsAltHeldDown())
            {
                var scriptActionType = map.EditorConfig.ScriptActions[clonedEntry.Action];

                if (scriptActionType.ParamType == TriggerParamType.Waypoint)
                {
                    int indexOffset = Keyboard.IsAltHeldDown() ? -1 : 1;

                    if (map.Waypoints.Exists(wp => wp.Identifier == clonedEntry.Argument + indexOffset))
                    {
                        clonedEntry.Argument = clonedEntry.Argument + indexOffset;
                    }
                }
            }

            editedScript.Actions.Insert(index, clonedEntry);
            EditScript(editedScript);
            lbActions.SelectedIndex = index;
            lbActions.ViewTop = viewTop;
        }

        private void InsertAction()
        {
            if (editedScript == null || lbActions.SelectedItem == null)
                return;

            isAddingAction = true;
            insertIndex = lbActions.SelectedIndex;
            selectScriptActionWindow.Open(null);
        }

        private void ActionListContextMenu_Delete()
        {
            if (editedScript == null || lbActions.SelectedItem == null)
                return;

            int viewTop = lbActions.ViewTop;
            editedScript.Actions.RemoveAt(lbActions.SelectedIndex);
            EditScript(editedScript);
            lbActions.ViewTop = viewTop;
        }

        private void SelectCellCursorAction_CellSelected(object sender, GameMath.Point2D e)
        {
            tbParameterValue.Text = ((e.Y * 1000) + e.X).ToString(CultureInfo.InvariantCulture);
        }

        private void BtnEditorPresetValues_LeftClick(object sender, EventArgs e)
        {
            if (editedScript == null)
                return;

            if (lbActions.SelectedItem == null)
                return;

            ScriptActionEntry entry = editedScript.Actions[lbActions.SelectedIndex];
            ScriptAction action = map.EditorConfig.ScriptActions.GetValueOrDefault(entry.Action);

            if (action == null)
                return;

            if (action.ParamType == TriggerParamType.Cell)
            {
                editorState.CursorAction = selectCellCursorAction;
                notificationManager.AddNotification(Translate(this, "SelectCell", "Select a cell from the map."));
            }
            else if (action.ParamType == TriggerParamType.BuildingWithProperty)
            {
                var (index, property) = SplitBuildingWithProperty(entry.Argument);
                selectBuildingTargetWindow.Open(index, property);
            }
            else if (action.ParamType == TriggerParamType.Animation)
            {
                var animType = entry.Argument > -1 && entry.Argument < map.Rules.AnimTypes.Count ? map.Rules.AnimTypes[entry.Argument] : null;
                selectAnimationWindow.Open(animType);
            }
        }

        private void BtnEditorPresetValuesWindow_LeftClick(object sender, EventArgs e)
        {
            if (editedScript == null)
                return;

            if (lbActions.SelectedItem == null)
                return;

            var item = selectScriptActionPresetOptionWindow.GetMatchingItem(tbParameterValue.Text);
            selectScriptActionPresetOptionWindow.Open(item);
        }

        private void ShowScriptReferences()
        {
            if (editedScript == null)
                return;

            var referringLocalTeamTypes = map.TeamTypes.FindAll(tt => tt.Script == editedScript);
            var referringGlobalTeamTypes = map.Rules.TeamTypes.FindAll(tt => tt.Script.ININame == editedScript.ININame);

            if (referringLocalTeamTypes.Count == 0 && referringGlobalTeamTypes.Count == 0)
            {
                EditorMessageBox.Show(WindowManager,
                    Translate(this, "NoReferencesFound.Title", "No references found"),
                    string.Format(Translate(this, "NoReferencesFound.Description", "The selected Script \"{0}\" ({1}) is not used by any TeamTypes, either local (map) or global (AI.ini)."), 
                        editedScript.Name, editedScript.ININame),
                    MessageBoxButtons.OK);
            }
            else
            {
                var stringBuilder = new StringBuilder();
                referringLocalTeamTypes.ForEach(tt => stringBuilder.AppendLine(
                    string.Format(Translate(this, "ReferringTeamTypes.Local", 
                        "- Local TeamType \"{0}\" ({1})"), tt.Name, tt.ININame)));
                referringGlobalTeamTypes.ForEach(tt => stringBuilder.AppendLine(
                    string.Format(Translate(this, "ReferringTeamTypes.Global", 
                        "- Global TeamType \"{0}\" ({1})"), tt.Name, tt.ININame)));

                EditorMessageBox.Show(WindowManager, 
                    Translate(this, "ScriptReferences.Title", "Script References"),
                    string.Format(Translate(this, "ScriptReferences.Description", "The selected Script \"{0}\" ({1}) is used by the following TeamTypes:" + Environment.NewLine + Environment.NewLine + "{2}"),
                        editedScript.Name, editedScript.ININame, stringBuilder.ToString()),
                    MessageBoxButtons.OK);
            }
        }

        private void BtnAddScript_LeftClick(object sender, EventArgs e)
        {
            var newScript = new Script(map.GetNewUniqueInternalId()) { Name = "New script" };
            map.Scripts.Add(newScript);
            ListScripts();
            SelectScript(newScript);
        }

        private void BtnDeleteScript_LeftClick(object sender, EventArgs e)
        {
            if (editedScript == null)
                return;

            if (Keyboard.IsShiftHeldDown())
            {
                DeleteScript();
            }
            else
            {
                var messageBox = EditorMessageBox.Show(WindowManager,
                    Translate(this, "DeleteConfirm.Title", "Confirm"),
                    string.Format(Translate(this, "DeleteConfirm.Description", "Are you sure you wish to delete '{0}'?" + Environment.NewLine + Environment.NewLine +
                        "You'll need to manually fix any TeamTypes using the Script." + Environment.NewLine + Environment.NewLine +
                        "(You can hold Shift to skip this confirmation dialog.)"), 
                        editedScript.Name),
                    MessageBoxButtons.YesNo);
                messageBox.YesClickedAction = _ => DeleteScript();
            }
        }

        private void DeleteScript()
        {
            if (lbScriptTypes.SelectedItem == null)
                return;

            map.RemoveScript((Script)lbScriptTypes.SelectedItem.Tag);
            map.TeamTypes.ForEach(tt =>
            {
                if (tt.Script == editedScript)
                    tt.Script = null;
            });
            ListScripts();
            RefreshSelectedScript();
        }

        private void BtnCloneScript_LeftClick(object sender, EventArgs e)
        {
            if (editedScript == null)
                return;

            var newScript = editedScript.Clone(map.GetNewUniqueInternalId());
            map.Scripts.Add(newScript);
            ListScripts();
            SelectScript(newScript);
        }

        private void BtnAddAction_LeftClick(object sender, EventArgs e)
        {
            if (editedScript == null)
                return;

            isAddingAction = true;
            insertIndex = -1;
            selectScriptActionWindow.Open(null);
        }

        private void BtnDeleteAction_LeftClick(object sender, EventArgs e)
        {
            if (editedScript == null || lbActions.SelectedItem == null)
                return;

            editedScript.Actions.RemoveAt(lbActions.SelectedIndex);
            EditScript(editedScript);
        }

        private void TbName_TextChanged(object sender, EventArgs e)
        {
            if (editedScript == null)
                return;

            editedScript.Name = tbName.Text;
            lbScriptTypes.SelectedItem.Text = tbName.Text;
        }

        private void TbFilter_TextChanged(object sender, EventArgs e) => ListScripts();

        private void DdScriptColor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ddScriptColor.SelectedIndex < 1)
            {
                editedScript.EditorColor = null;
                lbScriptTypes.SelectedItem.TextColor = lbScriptTypes.DefaultItemColor;
                return;
            }

            editedScript.EditorColor = ddScriptColor.SelectedItem.Text;
            lbScriptTypes.SelectedItem.TextColor = ddScriptColor.SelectedItem.TextColor.Value;
        }

        private void TbParameterValue_TextChanged(object sender, EventArgs e)
        {
            if (lbActions.SelectedItem == null || editedScript == null)
                return;

            ScriptActionEntry entry = editedScript.Actions[lbActions.SelectedIndex];
            entry.Argument = tbParameterValue.Value;
            lbActions.SelectedItem.Text = GetActionEntryText(lbActions.SelectedIndex, entry);
        }

        private void ContextMenu_OptionSelected(object sender, ContextMenuItemSelectedEventArgs e)
        {
            if (lbActions.SelectedItem == null || editedScript == null)
            {
                return;
            }

            tbParameterValue.Text = btnEditorPresetValues.ContextMenu.Items[e.ItemIndex].Text;
        }

        private void SelTypeOfAction_MouseLeftDown(object sender, EventArgs e)
        {
            if (lbActions.SelectedItem == null || editedScript == null)
            {
                return;
            }

            ScriptActionEntry entry = editedScript.Actions[lbActions.SelectedIndex];

            ScriptAction scriptAction = map.EditorConfig.ScriptActions.GetValueOrDefault(entry.Action);

            isAddingAction = false;
            selectScriptActionWindow.Open(scriptAction);
        }

        private void SelectScriptActionDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (editedScript == null)
            {
                return;
            }

            if (!isAddingAction && lbActions.SelectedItem == null)
            {
                return;
            }

            if (selectScriptActionWindow.SelectedObject != null)
            {
                if (isAddingAction)
                {
                    if (insertIndex > -1 && insertIndex < editedScript.Actions.Count)
                    {
                        int viewTop = lbActions.ViewTop;
                        int index = lbActions.SelectedIndex;
                        editedScript.Actions.Insert(index, new ScriptActionEntry(selectScriptActionWindow.SelectedObject.ID, 0));
                        EditScript(editedScript);
                        lbActions.SelectedIndex = index;
                        lbActions.ViewTop = viewTop;
                        insertIndex = -1;
                    }
                    else
                    {
                        editedScript.Actions.Add(new ScriptActionEntry(selectScriptActionWindow.SelectedObject.ID, 0));
                        EditScript(editedScript);
                        lbActions.SelectedIndex = lbActions.Items.Count - 1;
                        lbActions.ScrollToBottom();
                    }

                    isAddingAction = false;
                }
                else
                {
                    ScriptActionEntry entry = editedScript.Actions[lbActions.SelectedIndex];
                    entry.Action = selectScriptActionWindow.SelectedObject.ID;
                    lbActions.Items[lbActions.SelectedIndex].Text = GetActionEntryText(lbActions.SelectedIndex, entry);
                }
            }

            LbActions_SelectedIndexChanged(this, EventArgs.Empty);

            // Reduce chance of the user accidentally using buttons to edit scripts after the script action selection window has been hidden
            InputIgnoreTime = TimeSpan.FromSeconds(Constants.UIAccidentalClickPreventionTime);
        }


        private void SelectScriptActionPresetDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (lbActions.SelectedItem == null || editedScript == null)
            {
                return;
            }

            if (selectScriptActionPresetOptionWindow.SelectedObject != null)
                tbParameterValue.Text = selectScriptActionPresetOptionWindow.GetSelectedItemText();
        }

        private void RefreshParameterEntryText()
        {
            if (lbActions.SelectedItem == null || editedScript == null)
                return;

            ScriptActionEntry entry = editedScript.Actions[lbActions.SelectedIndex];
            ScriptAction action = map.EditorConfig.ScriptActions.GetValueOrDefault(entry.Action);
            tbParameterValue.TextChanged -= TbParameterValue_TextChanged;
            SetParameterEntryText(entry, action);
            tbParameterValue.TextChanged += TbParameterValue_TextChanged;
        }

        private void LbActions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbActions.SelectedItem == null || editedScript == null)
            {
                selTypeOfAction.Text = string.Empty;
                selTypeOfAction.Tag = null;
                tbParameterValue.Text = string.Empty;
                lblParameterDescription.Text = Translate(this, "ParameterDescriptionText", "Parameter:");
                lblActionDescriptionValue.Text = string.Empty;
                return;
            }

            ScriptActionEntry entry = editedScript.Actions[lbActions.SelectedIndex];
            ScriptAction action = map.EditorConfig.ScriptActions.GetValueOrDefault(entry.Action);

            selTypeOfAction.Text = GetActionNameFromIndex(entry.Action);

            tbParameterValue.TextChanged -= TbParameterValue_TextChanged;
            SetParameterEntryText(entry, action);
            tbParameterValue.TextChanged += TbParameterValue_TextChanged;

            lblParameterDescription.Text = action == null ? 
                Translate(this, "ParameterDescriptionText", "Parameter:") :
                action.ParamDescription + ":";
            lblActionDescriptionValue.Text = GetActionDescriptionFromIndex(entry.Action);

            string text = null;

            if (action.UseWindowSelection && action.PresetOptions.Count > 0)
            {
                btnEditorPresetValues.Disable();
                btnEditorPresetValuesWindow.Enable();
                text = selectScriptActionPresetOptionWindow.FillPresetOptions(entry, action);
            }
            else
            {
                btnEditorPresetValues.Enable();
                btnEditorPresetValuesWindow.Disable();
                text = FillPresetContextMenu(entry, action);
            }

            if (text != null)
                tbParameterValue.Text = text;
        }

        private void SetParameterEntryText(ScriptActionEntry scriptActionEntry, ScriptAction action)
        {
            if (action == null)
            {
                tbParameterValue.Value = scriptActionEntry.Argument;
                return;
            }

            if (action.ParamType == TriggerParamType.BuildingWithProperty)
            {
                tbParameterValue.Text = GetBuildingWithPropertyText(scriptActionEntry.Argument);
                return;
            }
            else if (action.ParamType == TriggerParamType.Animation)
            {
                if (scriptActionEntry.Argument > -1 && scriptActionEntry.Argument < map.Rules.AnimTypes.Count)
                    tbParameterValue.Text = scriptActionEntry.Argument.ToString(CultureInfo.InvariantCulture) + " - " + map.Rules.AnimTypes[scriptActionEntry.Argument].ININame;
                else
                    tbParameterValue.Text = scriptActionEntry.Argument.ToString(CultureInfo.InvariantCulture) + Translate(this, "UnknownAnimation", " - unknown animation");

                return;
            }

            int presetIndex = action.PresetOptions.FindIndex(p => p.Value == scriptActionEntry.Argument);

            if (presetIndex > -1)
            {
                tbParameterValue.Text = action.PresetOptions[presetIndex].GetOptionText();
            }
            else
            {
                tbParameterValue.Value = scriptActionEntry.Argument;
            }
        }

        private static Tuple<int, BuildingWithPropertyType> SplitBuildingWithProperty(int argument)
        {
            var property = argument switch
            {
                < (int)BuildingWithPropertyType.HighestThreat => BuildingWithPropertyType.LeastThreat,
                < (int)BuildingWithPropertyType.Nearest => BuildingWithPropertyType.HighestThreat,
                < (int)BuildingWithPropertyType.Farthest => BuildingWithPropertyType.Nearest,
                _ => BuildingWithPropertyType.Farthest,
            };
            return new(argument - (int)property, property);
        }

        private string GetBuildingWithPropertyText(int buildingTypeIndex, BuildingWithPropertyType property)
        {
            string description = property.ToDescription();
            BuildingType buildingType = map.Rules.BuildingTypes.GetElementIfInRange(buildingTypeIndex);
            int value = buildingTypeIndex + (int)property;

            if (buildingType == null)
                return value + Translate(this, "InvalidValue", " - invalid value");

            return value + " - " + buildingType.GetEditorDisplayName() + " (" + description + ")";
        }

        private string GetBuildingWithPropertyText(int argument)
        {
            var (index, property) = SplitBuildingWithProperty(argument);
            return GetBuildingWithPropertyText(index, property);
        }

        private string FillPresetContextMenu(ScriptActionEntry entry, ScriptAction action)
        {
            btnEditorPresetValues.ContextMenu.ClearItems();

            if (action == null)
            {
                return null;
            }

            action.PresetOptions.ForEach(p => btnEditorPresetValues.ContextMenu.AddItem(new XNAContextMenuItem() { Text = p.GetOptionText() }));

            if (action.ParamType == TriggerParamType.LocalVariable)
            {
                for (int i = 0; i < map.LocalVariables.Count; i++)
                {
                    btnEditorPresetValues.ContextMenu.AddItem(new XNAContextMenuItem() { Text = i + " - " + map.LocalVariables[i].Name });
                }
            }
            else if (action.ParamType == TriggerParamType.Waypoint)
            {
                foreach (Waypoint waypoint in map.Waypoints)
                {
                    btnEditorPresetValues.ContextMenu.AddItem(new XNAContextMenuItem() { Text = waypoint.Identifier.ToString() });
                }
            }
            else if (action.ParamType == TriggerParamType.HouseType)
            {
                foreach (var houseType in map.GetHouseTypes())
                {
                    btnEditorPresetValues.ContextMenu.AddItem(new XNAContextMenuItem() { Text = houseType.Index + " " + houseType.ININame, TextColor = Helpers.GetHouseTypeUITextColor(houseType) });
                }
            }
            else if (action.ParamType == TriggerParamType.House)
            {
                foreach (var house in map.GetHouses())
                {
                    btnEditorPresetValues.ContextMenu.AddItem(new XNAContextMenuItem() { Text = house.ID + " " + house.ININame, TextColor = Helpers.GetHouseUITextColor(house) });
                }
            }

            var fittingItem = btnEditorPresetValues.ContextMenu.Items.Find(item => item.Text == entry.Argument.ToString());
            if (fittingItem == null)
                fittingItem = btnEditorPresetValues.ContextMenu.Items.Find(item => item.Text.StartsWith(entry.Argument.ToString()));

            if (fittingItem != null)
                return fittingItem.Text;

            return null;
        }

        private void LbScriptTypes_SelectedIndexChanged(object sender, EventArgs e) => RefreshSelectedScript();

        private void RefreshSelectedScript()
        {
            if (lbScriptTypes.SelectedItem == null)
            {
                lbScriptTypes.SelectedIndex = -1;
                EditScript(null);
                return;
            }

            EditScript((Script)lbScriptTypes.SelectedItem.Tag);
        }

        public void Open()
        {
            ListScripts();

            Show();
        }

        public void SelectScript(Script script)
        {
            int index = lbScriptTypes.Items.FindIndex(lbi => lbi.Tag == script);

            if (index < 0)
                return;

            lbScriptTypes.SelectedIndex = index;
            lbScriptTypes.ScrollToSelectedElement();
        }

        private void ListScripts()
        {
            lbScriptTypes.Clear();

            IEnumerable<Script> sortedScripts = map.Scripts;

            bool shouldViewTop = false; // when filtering the scroll bar should update so we use a flag here
            if (tbFilter.Text != string.Empty && tbFilter.Text != tbFilter.Suggestion)
            {
                sortedScripts = sortedScripts.Where(script => script.Name.Contains(tbFilter.Text, StringComparison.CurrentCultureIgnoreCase));
                shouldViewTop = true;
            }

            switch (ScriptSortMode)
            {
                case ScriptSortMode.Color:
                    sortedScripts = sortedScripts.OrderBy(script => script.EditorColor).ThenBy(script => script.ININame);
                    break;
                case ScriptSortMode.Name:
                    sortedScripts = sortedScripts.OrderBy(script => script.Name).ThenBy(script => script.ININame);
                    break;
                case ScriptSortMode.ColorThenName:
                    sortedScripts = sortedScripts.OrderBy(script => script.EditorColor).ThenBy(script => script.Name);
                    break;
                case ScriptSortMode.ID:
                default:
                    sortedScripts = sortedScripts.OrderBy(script => script.ININame);
                    break;
            }

            foreach (var script in sortedScripts)
            {
                lbScriptTypes.AddItem(new XNAListBoxItem() { 
                    Text = script.Name,
                    Tag = script,
                    TextColor = script.EditorColor == null ? lbScriptTypes.DefaultItemColor : script.XNAColor
                });
            }

            if (shouldViewTop)
                lbScriptTypes.TopIndex = 0;
        }

        private void EditScript(Script script)
        {
            editedScript = script;
            ddScriptColor.SelectedIndexChanged -= DdScriptColor_SelectedIndexChanged;

            lbActions.Clear();
            lbActions.ViewTop = 0;

            if (editedScript == null)
            {
                tbName.Text = string.Empty;
                selTypeOfAction.Text = string.Empty;
                selTypeOfAction.Tag = null;
                tbParameterValue.Text = string.Empty;
                btnEditorPresetValues.ContextMenu.ClearItems();
                lblActionDescriptionValue.Text = string.Empty;
                lblParameterDescription.Text = Translate(this, "ParameterDescriptionText", "Parameter:");
                ddScriptColor.SelectedIndex = -1;

                return;
            }

            tbName.Text = editedScript.Name;
            for (int i = 0; i < editedScript.Actions.Count; i++)
            {
                var actionEntry = editedScript.Actions[i];
                lbActions.AddItem(new XNAListBoxItem()
                {
                    Text = GetActionEntryText(i, actionEntry),
                    Tag = actionEntry
                });
            }

            ddScriptColor.SelectedIndex = ddScriptColor.Items.FindIndex(item => item.Text == editedScript.EditorColor);
            if (ddScriptColor.SelectedIndex < 0)
                ddScriptColor.SelectedIndex = 0;

            LbActions_SelectedIndexChanged(this, EventArgs.Empty);
            ddScriptColor.SelectedIndexChanged += DdScriptColor_SelectedIndexChanged;
            lbActions.ScrollToSelectedElement();
        }

        private string GetActionEntryText(int index, ScriptActionEntry entry)
        {
            ScriptAction action = GetScriptAction(entry.Action);
            if (action == null)
                return "#" + index + " - Unknown (" + entry.Argument.ToString(CultureInfo.InvariantCulture) + ")";

            return "#" + index + " - " + action.Name + " (" + entry.Argument.ToString(CultureInfo.InvariantCulture) + ")";
        }

        private string GetActionNameFromIndex(int index)
        {
            ScriptAction action = GetScriptAction(index);
            if (action == null)
                return index + " Unknown";

            return index + " " + action.Name;
        }

        private string GetActionDescriptionFromIndex(int index)
        {
            ScriptAction action = GetScriptAction(index);
            string description = action == null ? Translate(this, "UnknownScriptAction", "Unknown script action. It has most likely been added with another editor.") : action.Description;

            return Renderer.FixText(description,
                lblActionDescriptionValue.FontIndex,
                lblActionDescriptionValue.Parent.Width - lblActionDescriptionValue.X * 2).Text;
        }

        private ScriptAction GetScriptAction(int index)
        {
            return map.EditorConfig.ScriptActions.GetValueOrDefault(index);
        }
    }
}
