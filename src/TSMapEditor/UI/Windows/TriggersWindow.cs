using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TSMapEditor.CCEngine;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.Rendering;
using TSMapEditor.UI.Controls;
using TSMapEditor.UI.CursorActions;

namespace TSMapEditor.UI.Windows
{
    public enum TriggerSortMode
    {
        ID,
        Name,
        Color,
        ColorThenName,
    }

    public class TriggersWindow : INItializableWindow
    {
        public TriggersWindow(WindowManager windowManager, Map map, EditorState editorState, ICursorActionTarget cursorActionTarget) : base(windowManager)
        {
            this.map = map;
            this.editorState = editorState;
            this.cursorActionTarget = cursorActionTarget;

            placeCellTagCursorAction = new PlaceCellTagCursorAction(cursorActionTarget);
            changeAttachedTagCursorAction = new ChangeAttachedTagCursorAction(cursorActionTarget);
            copiedTriggerData = new CopiedTriggerData();
        }

        public event EventHandler<TeamTypeEventArgs> TeamTypeOpened;

        private readonly Map map;
        private readonly ICursorActionTarget cursorActionTarget;
        private readonly PlaceCellTagCursorAction placeCellTagCursorAction;
        private readonly ChangeAttachedTagCursorAction changeAttachedTagCursorAction;
        private readonly EditorState editorState;
        private readonly TriggerParamType[] supportedGoToSourceTriggerParamTypes = 
        {
            TriggerParamType.Trigger,
            TriggerParamType.TeamType,
            TriggerParamType.Waypoint,
            TriggerParamType.WaypointZZ
        };
        private readonly CopiedTriggerData copiedTriggerData;

        private XNADropDown ddActions;

        // Trigger list
        private EditorListBox lbTriggers;
        private EditorSuggestionTextBox tbFilter;

        // General trigger settings
        private EditorTextBox tbName;
        private XNADropDown ddHouseType;
        private XNADropDown ddType;
        private EditorPopUpSelector selAttachedTrigger;
        private XNADropDown ddTriggerColor;
        private XNACheckBox chkDisabled;
        private XNACheckBox chkEasy;
        private XNACheckBox chkMedium;
        private XNACheckBox chkHard;

        // Events
        private EditorListBox lbEvents;
        private EditorPopUpSelector selEventType;
        private EditorDescriptionPanel panelEventDescription;
        private EditorListBox lbEventParameters;
        private EditorTextBox tbEventParameterValue;
        private XNAContextMenu ctxEventParameterPresetValues;

        // Actions
        private EditorListBox lbActions;
        private EditorPopUpSelector selActionType;
        private EditorDescriptionPanel panelActionDescription;
        private EditorListBox lbActionParameters;
        private EditorTextBox tbActionParameterValue;
        private EditorButton btnActionGoToTarget;        
        private XNAContextMenu ctxActionParameterPresetValues;

        private SelectEventWindow selectEventWindow;
        private SelectActionWindow selectActionWindow;
        private SelectAnimationWindow selectAnimationWindow;
        private SelectBuildingTypeWindow selectBuildingTypeWindow;
        private SelectTeamTypeWindow selectTeamTypeWindow;
        private SelectTriggerWindow selectTriggerWindow;
        private SelectGlobalVariableWindow selectGlobalVariableWindow;
        private SelectLocalVariableWindow selectLocalVariableWindow;
        private SelectHouseWindow selectHouseWindow;
        private SelectHouseTypeWindow selectHouseTypeWindow;
        private SelectTutorialLineWindow selectTutorialLineWindow;
        private SelectThemeWindow selectThemeWindow;
        private SelectTechnoTypeWindow selectTechnoTypeWindow;
        private SelectTagWindow selectTagWindow;
        private SelectStringWindow selectStringWindow;
        private SelectSpeechWindow selectSpeechWindow;
        private SelectSoundWindow selectSoundWindow;
        private SelectSuperWeaponTypeWindow selectSuperWeaponTypeWindow;
        private SelectParticleSystemTypeWindow selectParticleSystemTypeWindow;
        private SelectColorsWindow selectColorsWindow;
        private CreateRandomTriggerSetWindow createRandomTriggerSetWindow;

        private XNAContextMenu actionContextMenu;
        private XNAContextMenu eventContextMenu;

        private Trigger editedTrigger;

        /// <summary>
        /// Used to determine what we should do when the trigger selection window closes.
        /// (apply selected trigger to a parameter for an action or attach selected trigger to our trigger)
        /// </summary>
        private bool isAttachingTrigger;

        private TriggerSortMode _triggerSortMode;
        private TriggerSortMode TriggerSortMode
        {
            get => _triggerSortMode;
            set
            {
                if (value != _triggerSortMode)
                {
                    _triggerSortMode = value;
                }
                ListTriggers();
            }
        }

        public override void Initialize()
        {
            Name = nameof(TriggersWindow);
            base.Initialize();

            lbTriggers = FindChild<EditorListBox>(nameof(lbTriggers));
            tbName = FindChild<EditorTextBox>(nameof(tbName));
            tbName.AllowComma = false;

            tbFilter = FindChild<EditorSuggestionTextBox>(nameof(tbFilter));            
            tbFilter.TextChanged += TbFilter_TextChanged;            

            ddHouseType = FindChild<XNADropDown>(nameof(ddHouseType));
            ddType = FindChild<XNADropDown>(nameof(ddType));
            selAttachedTrigger = FindChild<EditorPopUpSelector>(nameof(selAttachedTrigger));
            chkDisabled = FindChild<XNACheckBox>(nameof(chkDisabled));
            chkEasy = FindChild<XNACheckBox>(nameof(chkEasy));
            chkMedium = FindChild<XNACheckBox>(nameof(chkMedium));
            chkHard = FindChild<XNACheckBox>(nameof(chkHard));

            // Init color dropdown options
            ddTriggerColor = FindChild<XNADropDown>(nameof(ddTriggerColor));
            ddTriggerColor.AddItem(Translate(this, "Color.None", "None"));
            Array.ForEach(Trigger.SupportedColors, sc =>
            {
                ddTriggerColor.AddItem(Translate("NamedColors." + sc.Name, sc.Name), sc.Value);
            });

            lbEvents = FindChild<EditorListBox>(nameof(lbEvents));
            selEventType = FindChild<EditorPopUpSelector>(nameof(selEventType));
            panelEventDescription = FindChild<EditorDescriptionPanel>(nameof(panelEventDescription));
            lbEventParameters = FindChild<EditorListBox>(nameof(lbEventParameters));
            tbEventParameterValue = FindChild<EditorTextBox>(nameof(tbEventParameterValue));
            btnActionGoToTarget = FindChild<EditorButton>(nameof(btnActionGoToTarget));

            ctxEventParameterPresetValues = new XNAContextMenu(WindowManager);
            ctxEventParameterPresetValues.Name = nameof(ctxEventParameterPresetValues);
            ctxEventParameterPresetValues.Width = 150;
            AddChild(ctxEventParameterPresetValues);
            ctxEventParameterPresetValues.OptionSelected += CtxEventParameterPresetValues_OptionSelected;

            lbActions = FindChild<EditorListBox>(nameof(lbActions));
            selActionType = FindChild<EditorPopUpSelector>(nameof(selActionType));
            panelActionDescription = FindChild<EditorDescriptionPanel>(nameof(panelActionDescription));
            lbActionParameters = FindChild<EditorListBox>(nameof(lbActionParameters));
            tbActionParameterValue = FindChild<EditorTextBox>(nameof(tbActionParameterValue));

            ctxActionParameterPresetValues = new XNAContextMenu(WindowManager);
            ctxActionParameterPresetValues.Name = nameof(ctxActionParameterPresetValues);
            ctxActionParameterPresetValues.Width = 150;
            AddChild(ctxActionParameterPresetValues);
            ctxActionParameterPresetValues.OptionSelected += CtxActionParameterPresetValues_OptionSelected;

            ddType.AddItem(Translate(this, "Type0", "0 - one-time, single-object condition"));
            ddType.AddItem(Translate(this, "Type1", "1 - one-time, multi-object condition"));
            ddType.AddItem(Translate(this, "Type2", "2 - repeating, single-object condition"));

            lbEvents.AllowMultiLineItems = false;
            lbActions.AllowMultiLineItems = false;

            var triggerContextMenu = new EditorContextMenu(WindowManager);
            triggerContextMenu.Name = nameof(triggerContextMenu);
            triggerContextMenu.Width = 270;
            triggerContextMenu.AddItem(Translate(this, "PlaceCellTag", "Place CellTag"), PlaceCellTag, null, () => editedTrigger != null);
            triggerContextMenu.AddItem(Translate(this, "ClearCellTags", "Clear CellTags"), ClearCellTags, null, () => editedTrigger != null);
            triggerContextMenu.AddItem(Translate(this, "AttachToObjects", "Attach to Objects"), AttachTagToObjects, null, () => editedTrigger != null);
            triggerContextMenu.AddItem(Translate(this, "ViewReferences", "View References"), ShowReferences, null, () => editedTrigger != null);
            if (!Constants.IsRA2YR)
            {
                triggerContextMenu.AddItem(Translate(this, "WrapEVA", "Wrap in EVA disable/enable actions"), WrapInEVADisableAndEnableActions, null,() => editedTrigger != null);
            }
            triggerContextMenu.AddItem(Translate(this, "CloneDiffs", "Clone for Easier Diffs"), CloneForEasierDifficulties, null, () => editedTrigger != null);
            triggerContextMenu.AddItem(Translate(this, "CloneDiffsNoDeps", "Clone for Easier Diffs (No Dependencies)"), CloneForEasierDifficultiesWithoutDependencies, null, () => editedTrigger != null);
            triggerContextMenu.AddItem(Translate(this, "CopyTrigger", "Copy Trigger"), CopyTrigger, null, () => editedTrigger != null);
            triggerContextMenu.AddItem(Translate(this, "PasteTrigger", "Paste Trigger"), PasteTrigger, () => Trigger.HasTriggerInClipboard());
            AddChild(triggerContextMenu);

            FindChild<EditorButton>("btnNewTrigger").LeftClick += BtnNewTrigger_LeftClick;
            FindChild<EditorButton>("btnDeleteTrigger").LeftClick += BtnDeleteTrigger_LeftClick;
            FindChild<EditorButton>("btnCloneTrigger").LeftClick += BtnCloneTrigger_LeftClick;
            ddActions = FindChild<XNADropDown>(nameof(ddActions));
            ddActions.AddItem(Translate(this, "ActionsAdvanced", "Advanced..."));
            // Add context menu options to Advanced menu for backwards compatibility
            for (int i = 0; i < triggerContextMenu.Items.Count; i++)
            {
                var contextMenuOption = triggerContextMenu.Items[i];
                ddActions.AddItem(new XNADropDownItem() { Text = contextMenuOption.Text, Tag = contextMenuOption.SelectAction });
            }
            ddActions.AddItem(new XNADropDownItem() { Text = Translate(this, "RegenerateTriggerIDs", "Re-generate Trigger IDs"), Tag = new Action(RegenerateIDs) });
            ddActions.AddItem(new XNADropDownItem() { Text = Translate(this, "CreateRandomTriggerSet", "Create Random Trigger Set"), Tag = new Action(OpenCreateRandomTriggersSetWindow) });

            ddActions.SelectedIndex = 0;
            ddActions.SelectedIndexChanged += DdActions_SelectedIndexChanged;

            FindChild<EditorButton>("btnAddEvent").LeftClick += BtnAddEvent_LeftClick;
            FindChild<EditorButton>("btnDeleteEvent").LeftClick += BtnDeleteEvent_LeftClick;
            FindChild<EditorButton>("btnCloneEvent").LeftClick += BtnCloneEvent_LeftClick;

            FindChild<EditorButton>("btnAddAction").LeftClick += BtnAddAction_LeftClick;
            FindChild<EditorButton>("btnDeleteAction").LeftClick += BtnDeleteAction_LeftClick;
            FindChild<EditorButton>("btnCloneAction").LeftClick += BtnCloneAction_LeftClick;

            FindChild<EditorButton>("btnActionParameterValuePreset").LeftClick += BtnActionParameterValuePreset_LeftClick;
            FindChild<EditorButton>("btnEventParameterValuePreset").LeftClick += BtnEventParameterValuePreset_LeftClick;

            btnActionGoToTarget.LeftClick += btnActionGoToTarget_LeftClick;

            selectEventWindow = new SelectEventWindow(WindowManager, map);
            var eventWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectEventWindow);
            eventWindowDarkeningPanel.Hidden += EventWindowDarkeningPanel_Hidden;

            selectActionWindow = new SelectActionWindow(WindowManager, map);
            var actionWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectActionWindow);
            actionWindowDarkeningPanel.Hidden += ActionWindowDarkeningPanel_Hidden;

            selectAnimationWindow = new SelectAnimationWindow(WindowManager, map);
            var animationWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectAnimationWindow);
            animationWindowDarkeningPanel.Hidden += AnimationWindowDarkeningPanel_Hidden;

            selectBuildingTypeWindow = new SelectBuildingTypeWindow(WindowManager, map);
            var buildingTypeWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectBuildingTypeWindow);
            buildingTypeWindowDarkeningPanel.Hidden += BuildingTypeWindowDarkeningPanel_Hidden;

            selectTeamTypeWindow = new SelectTeamTypeWindow(WindowManager, map);
            var teamTypeWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTeamTypeWindow);
            teamTypeWindowDarkeningPanel.Hidden += TeamTypeWindowDarkeningPanel_Hidden;

            selectTriggerWindow = new SelectTriggerWindow(WindowManager, map);
            var triggerWindowDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTriggerWindow);
            triggerWindowDarkeningPanel.Hidden += TriggerWindowDarkeningPanel_Hidden;

            selectGlobalVariableWindow = new SelectGlobalVariableWindow(WindowManager, map);
            var globalVariableDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectGlobalVariableWindow);
            globalVariableDarkeningPanel.Hidden += GlobalVariableDarkeningPanel_Hidden;

            selectLocalVariableWindow = new SelectLocalVariableWindow(WindowManager, map);
            var localVariableDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectLocalVariableWindow);
            localVariableDarkeningPanel.Hidden += LocalVariableDarkeningPanel_Hidden;

            selectHouseWindow = new SelectHouseWindow(WindowManager, map);
            var houseDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectHouseWindow);
            houseDarkeningPanel.Hidden += HouseDarkeningPanel_Hidden;

            selectHouseTypeWindow = new SelectHouseTypeWindow(WindowManager, map);
            var houseTypeDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectHouseTypeWindow);
            houseTypeDarkeningPanel.Hidden += HouseTypeDarkeningPanel_Hidden;

            selectTutorialLineWindow = new SelectTutorialLineWindow(WindowManager, map);
            var tutorialDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTutorialLineWindow);
            tutorialDarkeningPanel.Hidden += TutorialDarkeningPanel_Hidden;

            selectThemeWindow = new SelectThemeWindow(WindowManager, map, false);
            var themeDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectThemeWindow);
            themeDarkeningPanel.Hidden += ThemeDarkeningPanel_Hidden;

            selectTechnoTypeWindow = new SelectTechnoTypeWindow(WindowManager, map);
            var technoTypeDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTechnoTypeWindow);
            technoTypeDarkeningPanel.Hidden += TechnoTypeDarkeningPanel_Hidden;

            selectTagWindow = new SelectTagWindow(WindowManager, map);
            var tagDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTagWindow);
            tagDarkeningPanel.Hidden += TagDarkeningPanel_Hidden;

            selectStringWindow = new SelectStringWindow(WindowManager, map);
            var stringDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectStringWindow);
            stringDarkeningPanel.Hidden += StringDarkeningPanel_Hidden;

            selectSpeechWindow = new SelectSpeechWindow(WindowManager, map);
            var speechDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectSpeechWindow);
            speechDarkeningPanel.Hidden += SpeechDarkeningPanel_Hidden;

            selectSoundWindow = new SelectSoundWindow(WindowManager, map);
            var soundDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectSoundWindow);
            soundDarkeningPanel.Hidden += SoundDarkeningPanel_Hidden;

            selectParticleSystemTypeWindow = new SelectParticleSystemTypeWindow(WindowManager, map);
            var particleSystemTypeDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectParticleSystemTypeWindow);
            particleSystemTypeDarkeningPanel.Hidden += ParticleSystemTypeDarkeningPanel_Hidden;

            selectSuperWeaponTypeWindow = new SelectSuperWeaponTypeWindow(WindowManager, map);
            var swDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectSuperWeaponTypeWindow);
            swDarkeningPanel.Hidden += SuperWeaponDarkeningPanel_Hidden;

            selectColorsWindow = new SelectColorsWindow(WindowManager, map);
            var colorDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectColorsWindow);
            colorDarkeningPanel.Hidden += ColorDarkeningPanel_Hidden;

            createRandomTriggerSetWindow = new CreateRandomTriggerSetWindow(WindowManager, map);
            var createRandomTriggersSetDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, createRandomTriggerSetWindow);
            createRandomTriggerSetWindow.RandomTriggerSetTriggersCreated += CreateRandomTriggerSetWindow_RandomTriggersSetCreated;

            eventContextMenu = new EditorContextMenu(WindowManager);
            eventContextMenu.Name = nameof(eventContextMenu);
            eventContextMenu.Width = lbEvents.Width;
            eventContextMenu.AddItem(Translate(this, "MoveUp","Move Up"), EventContextMenu_MoveUp, () => editedTrigger != null && lbEvents.SelectedItem != null && lbEvents.SelectedIndex > 0);
            eventContextMenu.AddItem(Translate(this, "MoveDown", "Move Down"), EventContextMenu_MoveDown, () => editedTrigger != null && lbEvents.SelectedItem != null && lbEvents.SelectedIndex < lbEvents.Items.Count - 1);
            eventContextMenu.AddItem(Translate(this, "CloneEvent", "Clone Event"), EventContextMenu_CloneEvent, () => editedTrigger != null && lbEvents.SelectedItem != null);
            eventContextMenu.AddItem(Translate(this, "DeleteEvent", "Delete Event"), () => BtnDeleteEvent_LeftClick(this, EventArgs.Empty), () => editedTrigger != null && lbEvents.SelectedItem != null);
            eventContextMenu.AddItem(Translate(this, "CopyEvent", "Copy Event"), EventContextMenu_CopyAction, () => editedTrigger != null && lbEvents.SelectedItem != null);
            eventContextMenu.AddItem(Translate(this, "PasteEvent", "Paste Event"), EventContextMenu_PasteEvent, () => editedTrigger != null && copiedTriggerData.HasTriggerEventDataInClipboard());
            AddChild(eventContextMenu);

            lbEvents.AllowRightClickUnselect = false;
            lbEvents.RightClick += (s, e) => { if (editedTrigger != null) { lbEvents.OnMouseLeftDown(new InputEventArgs()); eventContextMenu.Open(GetCursorPoint()); } };

            actionContextMenu = new EditorContextMenu(WindowManager);
            actionContextMenu.Name = nameof(actionContextMenu);
            actionContextMenu.Width = lbActions.Width;
            actionContextMenu.AddItem(Translate(this, "MoveUp", "Move Up"), ActionContextMenu_MoveUp, () => editedTrigger != null && lbActions.SelectedItem != null && lbActions.SelectedIndex > 0);
            actionContextMenu.AddItem(Translate(this, "MoveUp", "Move Down"), ActionContextMenu_MoveDown, () => editedTrigger != null && lbActions.SelectedItem != null && lbActions.SelectedIndex < lbActions.Items.Count - 1);
            actionContextMenu.AddItem(Translate(this, "CloneAction", "Clone Action"), ActionContextMenu_CloneAction, () => editedTrigger != null && lbActions.SelectedItem != null);
            actionContextMenu.AddItem(Translate(this, "DeleteAction", "Delete Action"), () => BtnDeleteAction_LeftClick(this, EventArgs.Empty), () => editedTrigger != null && lbActions.SelectedItem != null);
            actionContextMenu.AddItem(Translate(this, "CopyAction", "Copy Action"), ActionContextMenu_CopyAction, () => editedTrigger != null && lbActions.SelectedItem != null);
            actionContextMenu.AddItem(Translate(this, "PasteAction", "Paste Action"), ActionContextMenu_PasteAction, () => editedTrigger != null && copiedTriggerData.HasTriggerActionDataInClipboard());
            AddChild(actionContextMenu);

            lbActions.AllowRightClickUnselect = false;
            lbActions.RightClick += (s, e) => { if (editedTrigger != null) { lbActions.OnMouseLeftDown(new InputEventArgs()); actionContextMenu.Open(GetCursorPoint()); } };

            var sortContextMenu = new EditorContextMenu(WindowManager);
            sortContextMenu.Name = nameof(sortContextMenu);
            sortContextMenu.Width = lbTriggers.Width;
            sortContextMenu.AddItem(Translate(this, "SortByID", "Sort by ID"), () => TriggerSortMode = TriggerSortMode.ID);
            sortContextMenu.AddItem(Translate(this, "SortByName", "Sort by Name"), () => TriggerSortMode = TriggerSortMode.Name);
            sortContextMenu.AddItem(Translate(this, "SortByColor" ,"Sort by Color"), () => TriggerSortMode = TriggerSortMode.Color);
            sortContextMenu.AddItem(Translate(this, "SortByColorName", "Sort by Color, then by Name"), () => TriggerSortMode = TriggerSortMode.ColorThenName);
            AddChild(sortContextMenu);

            FindChild<EditorButton>("btnSortOptions").LeftClick += (s, e) => sortContextMenu.Open(GetCursorPoint());

            lbTriggers.AllowRightClickUnselect = false;
            lbTriggers.RightClick += (s, e) => { lbTriggers.OnMouseLeftDown(new InputEventArgs()); triggerContextMenu.Open(GetCursorPoint()); };
            lbTriggers.SelectedIndexChanged += LbTriggers_SelectedIndexChanged;

            WindowManager.WindowSizeChangedByUser += WindowManager_WindowSizeChangedByUser;            
        }

        private void CreateRandomTriggerSetWindow_RandomTriggersSetCreated(object sender, RandomTriggerSetTriggersCreatedEventArgs e)
        {
            ListTriggers();
            SelectTrigger(e.BaseTrigger);
        }

        private void WindowManager_WindowSizeChangedByUser(object sender, EventArgs e)
        {
            ListTriggers();
        }

        public override void Kill()
        {
            WindowManager.WindowSizeChangedByUser -= WindowManager_WindowSizeChangedByUser;
            base.Kill();
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

        private void PlaceCellTag()
        {
            if (editedTrigger == null)
                return;

            Tag tag = map.Tags.Find(t => t.Trigger == editedTrigger);

            if (tag == null)
                return;

            placeCellTagCursorAction.Tag = tag;
            editorState.CursorAction = placeCellTagCursorAction;
        }

        private void ClearCellTags()
        {
            if (editedTrigger == null)
                return;

            Tag tag = map.Tags.Find(t => t.Trigger == editedTrigger);

            if (tag == null)
                return;

            var messageBox = EditorMessageBox.Show(WindowManager, Translate(this, "ClearCellTags.Title", "Are you sure?"),
                string.Format(Translate(this, "ClearCellTags.Description", "This will delete all CellTags related to trigger \"{0}\". No un-do is available. Do you want to continue?"), editedTrigger.Name),
                MessageBoxButtons.YesNo);

            messageBox.YesClickedAction = _ =>
            {
                var cellTagsCopy = new List<CellTag>(map.CellTags);

                foreach (var cellTag in cellTagsCopy)
                {
                    if (cellTag.Tag == tag)
                    {
                        map.RemoveCellTagFrom(cellTag.Position);
                    }
                }

                cursorActionTarget.InvalidateMap();
            };
        }

        private void AttachTagToObjects()
        {
            if (editedTrigger == null)
                return;

            Tag tag = map.Tags.Find(t => t.Trigger == editedTrigger);

            if (tag == null)
            {
                EditorMessageBox.Show(WindowManager,
                    Translate(this, "NoTagFound.Title", "No tag found"),
                    string.Format(Translate(this, "NoTagFound.Description", "The selected trigger '{0}' has no associated tag. As such, it cannot be attached to any objects." + Environment.NewLine + Environment.NewLine +
                        "This should never happen, have you modified the map with another editor?"), 
                        editedTrigger.Name),
                    MessageBoxButtons.OK);

                return;
            }

            changeAttachedTagCursorAction.TagToAttach = tag;
            editorState.CursorAction = changeAttachedTagCursorAction;
        }

        #region Viewing linked objects

        private void ShowReferences()
        {
            if (editedTrigger == null)
                return;

            var stringBuilder = new StringBuilder();

            var tag = map.Tags.Find(t => t.Trigger == editedTrigger);
            if (tag == null)
            {
                stringBuilder.Append(string.Format(Translate(this, "NoAssociatedTag", "The selected trigger {0} has no associated tag. As such, it is not attached to any objects."), editedTrigger.Name));
            }
            else
            {
                var objectList = new List<TechnoBase>();
                map.Infantry.ForEach(inf => AddObjectToListIfLinkedToTag(inf, objectList, tag));
                map.Units.ForEach(unit => AddObjectToListIfLinkedToTag(unit, objectList, tag));
                map.Structures.ForEach(structure => AddObjectToListIfLinkedToTag(structure, objectList, tag));
                map.Aircraft.ForEach(aircraft => AddObjectToListIfLinkedToTag(aircraft, objectList, tag));

                if (objectList.Count > 0)
                {
                    stringBuilder.Append(string.Format(Translate(this, "ObjectReferences", "The selected trigger '{0}' is linked to the following objects:\r\n"), editedTrigger.Name));

                    objectList.ForEach(techno =>
                    {
                        switch (techno.WhatAmI())
                        {
                            case RTTIType.Aircraft:
                                AppendToStringBuilder((Aircraft)techno, stringBuilder);
                                break;
                            case RTTIType.Building:
                                AppendToStringBuilder((Structure)techno, stringBuilder);
                                break;
                            case RTTIType.Infantry:
                                AppendToStringBuilder((Infantry)techno, stringBuilder);
                                break;
                            case RTTIType.Unit:
                                AppendToStringBuilder((Unit)techno, stringBuilder);
                                break;
                            default:
                                throw new NotImplementedException("Unknown RTTI type encountered when listing linked objects for a trigger.");
                        }
                    });

                    stringBuilder.Append(Environment.NewLine);
                }

                var teamTypes = map.TeamTypes.FindAll(tt => tt.Tag == tag);
                if (teamTypes.Count > 0)
                {
                    foreach (var teamType in teamTypes)
                    {
                        stringBuilder.Append(string.Format(Translate(this, "TeamTypeReferences", "The trigger is linked to TeamType '{0}' ({1})."), teamType.Name, teamType.ININame));
                        stringBuilder.Append(Environment.NewLine);
                    }
                }

                var celltag = map.CellTags.Find(ct => ct.Tag == tag);
                if (celltag != null)
                {
                    stringBuilder.Append(Environment.NewLine);
                    stringBuilder.Append(string.Format(Translate(this, "LinkedCellTags", "The trigger is linked to one or more celltags (first match at {0})."), celltag.Position));
                }
            }

            // Check other triggers to see whether this trigger is referenced by them
            var allReferringTriggers = map.Triggers.FindAll(trig =>
            {
                foreach (var triggerAction in trig.Actions)
                {
                    if (!map.EditorConfig.TriggerEventTypes.ContainsKey(triggerAction.ActionIndex))
                        continue;

                    var actionType = map.EditorConfig.TriggerActionTypes[triggerAction.ActionIndex];

                    for (int i = 0; i < triggerAction.Parameters.Length && i < actionType.Parameters.Length; i++)
                    {
                        string paramValue = triggerAction.Parameters[i];
                        if (actionType.Parameters[i].TriggerParamType == TriggerParamType.Trigger && paramValue == editedTrigger.ID)
                        {
                            return true;
                        }
                    }
                }

                if (trig.LinkedTrigger == editedTrigger)
                    return true;

                return false;
            });

            if (allReferringTriggers.Count > 0)
            {
                stringBuilder.Append(Environment.NewLine);
                stringBuilder.Append(Translate(this, "TriggerReferences", "The trigger is referenced by the following other triggers:"));
                allReferringTriggers.ForEach(trig => stringBuilder.Append(Environment.NewLine + string.Format(Translate(this, "TriggerReference", "    - {0} ({1})"), trig.Name, trig.ID)));
            }

            if (stringBuilder.Length == 0)
            {
                EditorMessageBox.Show(WindowManager, 
                    Translate(this, "LinkedObjects.Title", "Linked Objects"),
                    string.Format(Translate(this, "NoLinkedObjects.Description", "The selected trigger '{0}' is not linked to any objects, CellTags or other triggers."), editedTrigger.Name),
                    MessageBoxButtons.OK);
            }
            else
            {
                if (stringBuilder[0] == Environment.NewLine[0])
                    stringBuilder.Remove(0, Environment.NewLine.Length);

                EditorMessageBox.Show(WindowManager, Translate(this, "LinkedObjects.Title", "Linked Objects"), stringBuilder.ToString(), MessageBoxButtons.OK);
            }

            return;
        }

        private void AppendToStringBuilder<T>(Techno<T> techno, StringBuilder stringBuilder) where T : TechnoType
        {
            string rtti = techno.WhatAmI().ToString();
            string name = techno.ObjectType.Name;
            string position = techno.Position.ToString();

            stringBuilder.Append($"    - {rtti}: {name} at {position}{Environment.NewLine}");
        }

        private void AddObjectToListIfLinkedToTag(TechnoBase techno, List<TechnoBase> technoList, Tag tag)
        {
            if (techno.AttachedTag == tag)
                technoList.Add(techno);
        }

        private void WrapInEVADisableAndEnableActions()
        {
            if (editedTrigger == null)
                return;

            const int TSDisableSpeechActionIndex = 102;
            const int TSEnableSpeechActionIndex = 103;

            if (!map.EditorConfig.TriggerActionTypes.TryGetValue(TSDisableSpeechActionIndex, out TriggerActionType disableSpeechTriggerActionType))
            {
                EditorMessageBox.Show(WindowManager, 
                    Translate(this, "ActionTypeNotFound.Title", "Trigger action type not found"), 
                    string.Format(Translate(this, "DisableSpeechNotFound", "Could not find trigger action type for \"Disable Speech\" {0}"), TSDisableSpeechActionIndex), 
                    MessageBoxButtons.OK);
                return;
            }

            if (!map.EditorConfig.TriggerActionTypes.TryGetValue(TSEnableSpeechActionIndex, out TriggerActionType enableSpeechTriggerActionType))
            {
                EditorMessageBox.Show(WindowManager, 
                    Translate(this, "ActionTypeNotFound.Title", "Trigger action type not found"), 
                    string.Format(Translate(this, "EnableSpeechNotFound", "Could not find trigger action type for \"Enable Speech\" {0}"), TSEnableSpeechActionIndex), 
                    MessageBoxButtons.OK);
                return;
            }

            editedTrigger.Actions.Insert(0, CreateTriggerAction(disableSpeechTriggerActionType));
            editedTrigger.Actions.Add(CreateTriggerAction(enableSpeechTriggerActionType));

            EditTrigger(editedTrigger);
        }

        #endregion

        private void RegenerateIDs()
        {
            var messageBox = EditorMessageBox.Show(WindowManager, 
                Translate(this, "RegenerateIDs.Title", "Are you sure?"),
                Translate(this, "RegenerateIDs.Description", 
                    "This will re-generate the internal IDs (01000000, 01000001 etc.) for ALL* of your map's script elements" + Environment.NewLine +
                    "that start their ID with 0100 (all editor-generated script elements do)." + Environment.NewLine + Environment.NewLine +
                    "It might make the list more sensible in case there are deleted triggers. However, this feature is" + Environment.NewLine +
                    "experimental and if it goes wrong, it can destroy all of your scripting. Do you want to continue?"),
                MessageBoxButtons.YesNo);

            messageBox.YesClickedAction = _ => map.RegenerateInternalIds();
            ListTriggers();
            LbTriggers_SelectedIndexChanged(this, EventArgs.Empty);
        }

        private void CloneForEasierDifficulties()
        {
            if (editedTrigger == null)
                return;

            var messageBox = EditorMessageBox.Show(WindowManager,
                Translate(this, "CloneForEasierDiffs.Title", "Are you sure?"),
                Translate(this, "CloneForEasierDiffs.Description", 
                    "Cloning this trigger for easier difficulties will create duplicate instances" + Environment.NewLine +
                    "of this trigger for Medium and Easy difficulties, replacing Hard-mode globals" + Environment.NewLine +
                    "with respective globals of easier difficulties." + Environment.NewLine + Environment.NewLine +
                    "In case the trigger references TeamTypes, duplicates of the TeamTypes" + Environment.NewLine +
                    "and their TaskForces are also created for the easier-difficulty triggers." + Environment.NewLine + Environment.NewLine +
                    "No un-do is available. Do you want to continue?"),
                MessageBoxButtons.YesNo);

            messageBox.YesClickedAction = _ => DoCloneForEasierDifficulties(true);
        }

        private void CloneForEasierDifficultiesWithoutDependencies()
        {
            if (editedTrigger == null)
                return;

            var messageBox = EditorMessageBox.Show(WindowManager,
                Translate(this, "CloneForEasierDiffsNoDependencies.Title", "Are you sure?"),
                Translate(this, "CloneForEasierDiffsNoDependencies.Description",
                    "Cloning this trigger for easier difficulties will create duplicate instances" + Environment.NewLine +
                    "of this trigger for Medium and Easy difficulties, replacing Hard-mode globals" + Environment.NewLine +
                    "with respective globals of easier difficulties." + Environment.NewLine + Environment.NewLine +
                    "No un-do is available. Do you want to continue?"),
                MessageBoxButtons.YesNo);

            messageBox.YesClickedAction = _ => DoCloneForEasierDifficulties(false);
        }

        private TeamType FindOrCloneTeamTypeForDifficulty(TeamType hardTeamType, Difficulty targetDifficulty)
        {
            string targetDiffTeamTypeName = Helpers.ConvertNameToNewDifficulty(hardTeamType.Name, Difficulty.Hard, targetDifficulty);
            TeamType targetDiffTeamType = map.TeamTypes.Find(tt => tt.Name == targetDiffTeamTypeName);

            // Only create new TeamType and TaskForce if they weren't already found
            if (targetDiffTeamType == null)
            {
                string targetDiffTaskForceName = Helpers.ConvertNameToNewDifficulty(hardTeamType.TaskForce.Name, Difficulty.Hard, targetDifficulty);
                TaskForce targetDiffTaskForce = map.TaskForces.Find(tt => tt.Name == targetDiffTaskForceName);

                if (targetDiffTaskForce == null)
                {
                    targetDiffTaskForce = hardTeamType.TaskForce.Clone(map.GetNewUniqueInternalId());
                    targetDiffTaskForce.Name = targetDiffTaskForceName;
                    map.AddTaskForce(targetDiffTaskForce);
                }

                targetDiffTeamType = hardTeamType.Clone(map.GetNewUniqueInternalId());
                targetDiffTeamType.Name = targetDiffTeamTypeName;
                targetDiffTeamType.TaskForce = targetDiffTaskForce;
                map.AddTeamType(targetDiffTeamType);
            }

            return targetDiffTeamType;
        }

        private void DoCloneForEasierDifficulties(bool cloneDependencies)
        {
            map.TriggersChanged -= Map_TriggersChanged;

            var originalTag = map.Tags.Find(t => t.Trigger == editedTrigger);

            var mediumDifficultyTrigger = editedTrigger.Clone(map.GetNewUniqueInternalId());
            mediumDifficultyTrigger.Hard = false;
            mediumDifficultyTrigger.Normal = true;
            mediumDifficultyTrigger.Easy = false;
            map.AddTrigger(mediumDifficultyTrigger);

            var easyDifficultyTrigger = editedTrigger.Clone(map.GetNewUniqueInternalId());
            easyDifficultyTrigger.Hard = false;
            easyDifficultyTrigger.Normal = false;
            easyDifficultyTrigger.Easy = true;
            map.AddTrigger(easyDifficultyTrigger);

            if (editedTrigger.Name.Contains("Hard"))
            {
                mediumDifficultyTrigger.Name = editedTrigger.Name.Replace("Hard", "Medium");
                easyDifficultyTrigger.Name = editedTrigger.Name.Replace("Hard", "Easy");
            }
            else if (editedTrigger.Name.StartsWith("H "))
            {
                mediumDifficultyTrigger.Name = "M " + editedTrigger.Name[2..];
                easyDifficultyTrigger.Name = "E " + editedTrigger.Name[2..];
            }
            else if (editedTrigger.Name.EndsWith(" H"))
            {
                mediumDifficultyTrigger.Name = editedTrigger.Name[..^2] + " M";
                easyDifficultyTrigger.Name = editedTrigger.Name[..^2] + " E";
            }

            map.AddTag(new Tag()
            {
                ID = map.GetNewUniqueInternalId(),
                Name = mediumDifficultyTrigger.Name + " (tag)",
                Trigger = mediumDifficultyTrigger,
                Repeating = originalTag == null ? 0 : originalTag.Repeating
            });

            map.AddTag(new Tag()
            {
                ID = map.GetNewUniqueInternalId(),
                Name = easyDifficultyTrigger.Name + " (tag)",
                Trigger = easyDifficultyTrigger,
                Repeating = originalTag == null ? 0 : originalTag.Repeating
            });

            int mediumDiffGlobalVariableIndex = map.Rules.GlobalVariables.FindIndex(gv => gv.Name == "Difficulty Medium");
            int easyDiffGlobalVariableIndex = map.Rules.GlobalVariables.FindIndex(gv => gv.Name == "Difficulty Easy");

            if (mediumDiffGlobalVariableIndex < 0)
            {
                Logger.Log($"{nameof(TriggersWindow)}.{nameof(DoCloneForEasierDifficulties)}: Medium difficulty global variable not found!");
            }

            if (easyDiffGlobalVariableIndex < 0)
            {
                Logger.Log($"{nameof(TriggersWindow)}.{nameof(DoCloneForEasierDifficulties)}: Easy difficulty global variable not found!");
            }

            // Go through used events. If there's a reference to the
            // "Difficulty Hard" global, replace it
            // with references to the Medium and Easy globals.
            for (int i = 0; i < editedTrigger.Conditions.Count; i++)
            {
                TriggerCondition condition = editedTrigger.Conditions[i];

                if (map.EditorConfig.TriggerEventTypes[condition.ConditionIndex].Parameters[1].TriggerParamType == TriggerParamType.GlobalVariable)
                {
                    int paramValue = Conversions.IntFromString(condition.Parameters[1], 0);
                    if (!map.Rules.GlobalVariables.Exists(gv => gv.Index == paramValue))
                        continue;

                    if (map.Rules.GlobalVariables.Find(gv => gv.Index == paramValue).Name == "Difficulty Hard")
                    {
                        if (mediumDiffGlobalVariableIndex > -1)
                        {
                            mediumDifficultyTrigger.Conditions[i].Parameters[1] = mediumDiffGlobalVariableIndex.ToString();
                        }

                        if (easyDiffGlobalVariableIndex > -1)
                        {
                            easyDifficultyTrigger.Conditions[i].Parameters[1] = easyDiffGlobalVariableIndex.ToString();
                        }
                    }
                }
            }

            if (cloneDependencies)
            {
                // Go through used actions and their parameters.
                // If they refer to any TeamTypes, clone the TeamTypes and replace the references.

                for (int i = 0; i < editedTrigger.Actions.Count; i++)
                {
                    TriggerAction action = editedTrigger.Actions[i];

                    TriggerActionType triggerActionType = map.EditorConfig.TriggerActionTypes[action.ActionIndex];

                    for (int j = 0; j < triggerActionType.Parameters.Length; j++)
                    {
                        var param = triggerActionType.Parameters[j];

                        if (param != null && param.TriggerParamType == TriggerParamType.TeamType)
                        {
                            TeamType teamType = map.TeamTypes.Find(tt => tt.ININame == action.ParamToString(j));

                            if (teamType != null && teamType.TaskForce != null)
                            {
                                TeamType mediumTeamType = FindOrCloneTeamTypeForDifficulty(teamType, Difficulty.Medium);
                                TeamType easyTeamType = FindOrCloneTeamTypeForDifficulty(teamType, Difficulty.Easy);

                                mediumDifficultyTrigger.Actions[i].Parameters[j] = mediumTeamType.ININame;
                                easyDifficultyTrigger.Actions[i].Parameters[j] = easyTeamType.ININame;
                            }
                        }
                    }
                }
            }

            ListTriggers();

            map.TriggersChanged += Map_TriggersChanged;
        }

        #region Event and action context menus

        private void ActionContextMenu_MoveUp() => MoveUpEventOrAction(lbActions, editedTrigger?.Actions);

        private void EventContextMenu_MoveUp() => MoveUpEventOrAction(lbEvents, editedTrigger?.Conditions);

        private void MoveUpEventOrAction<T>(XNAListBox listBox, List<T> objectList)
        {
            if (editedTrigger == null || objectList == null || listBox.SelectedItem == null || listBox.SelectedIndex < 1)
                return;

            var tmp = objectList[listBox.SelectedIndex - 1];
            objectList[listBox.SelectedIndex - 1] = objectList[listBox.SelectedIndex];
            objectList[listBox.SelectedIndex] = tmp;

            EditTrigger(editedTrigger);
            listBox.SelectedIndex--;
        }

        private void ActionContextMenu_MoveDown() => MoveDownEventOrAction(lbActions, editedTrigger?.Actions);

        private void EventContextMenu_MoveDown() => MoveDownEventOrAction(lbEvents, editedTrigger?.Conditions);

        private void MoveDownEventOrAction<T>(XNAListBox listBox, List<T> objectList)
        {
            if (editedTrigger == null || listBox.SelectedItem == null || listBox.SelectedIndex >= listBox.Items.Count - 1)
                return;

            var tmp = objectList[listBox.SelectedIndex + 1];
            objectList[listBox.SelectedIndex + 1] = objectList[listBox.SelectedIndex];
            objectList[listBox.SelectedIndex] = tmp;

            EditTrigger(editedTrigger);
            listBox.SelectedIndex++;
        }

        private void EventContextMenu_CloneEvent() => CloneEventOrAction(lbEvents, editedTrigger?.Conditions);

        private void ActionContextMenu_CloneAction() => CloneEventOrAction(lbActions, editedTrigger?.Actions);

        private void ActionContextMenu_CopyAction() => CopyActionOrEvent(false);

        private void EventContextMenu_CopyAction() => CopyActionOrEvent(true);

        private void CopyActionOrEvent(bool isEvent)
        {
            var relevantListBox = isEvent ? lbEvents : lbActions;
            var tag = relevantListBox.SelectedItem?.Tag;
            if (tag == null)
                return;

            if (isEvent)
                copiedTriggerData.SetCopiedTriggerEvent((TriggerCondition)tag);
            else
                copiedTriggerData.SetCopiedTriggerAction((TriggerAction)tag);

            copiedTriggerData.CopyToClipboard();
        }

        private void CopyTrigger()
        {
            if (editedTrigger == null)
                return;

            var associatedTag = map.Tags.Find(tag => tag.Trigger == editedTrigger);
            if (associatedTag == null)
                return;

            Trigger.CopyToClipboard(editedTrigger, associatedTag);
        }

        private void PasteTrigger()
        {
            var (trigger, tag) = Trigger.GetTriggerAndTagFromClipboard(map);
            if (trigger == null || tag == null)
                return;

            map.AddTrigger(trigger);
            map.AddTag(tag);

            ListTriggers();

            int triggerIndex = lbTriggers.Items.FindIndex(listedTrigger => listedTrigger.Tag == trigger);
            if (triggerIndex >= 0)
            {
                lbTriggers.SelectedIndex = triggerIndex;
                lbTriggers.ScrollToSelectedElement();
            }
        }

        private void EventContextMenu_PasteEvent() => PasteActionOrEvent(true);
        private void ActionContextMenu_PasteAction() => PasteActionOrEvent(false);

        private void PasteActionOrEvent(bool isEvent)
        {
            if (editedTrigger == null)
                return;

            if (isEvent)
            {
                var triggerEvent = copiedTriggerData.GetTriggerEventFromClipboard();
                if (triggerEvent == null)
                    return;

                editedTrigger.Conditions.Add(triggerEvent);
                EditTrigger(editedTrigger);

                int eventIndex = lbEvents.Items.FindIndex(listedEvent => listedEvent.Tag == triggerEvent);
                if (eventIndex >= 0)
                {
                    lbEvents.SelectedIndex = eventIndex;
                    lbEvents.ScrollToSelectedElement();
                }
            }
            else
            {
                var triggerAction = copiedTriggerData.GetTriggerActionFromClipboard();
                if (triggerAction == null)
                    return;

                editedTrigger.Actions.Add(triggerAction);
                EditTrigger(editedTrigger);

                int actionIndex = lbActions.Items.FindIndex(listedAction => listedAction.Tag == triggerAction);
                if (actionIndex >= 0)
                {
                    lbActions.SelectedIndex = actionIndex;
                    lbActions.ScrollToSelectedElement();
                }
            }
        }

        private void CloneEventOrAction<T>(XNAListBox listBox, List<T> objectList) where T : ICloneable
        {
            if (editedTrigger == null || listBox.SelectedItem == null)
                return;

            var tag = (T)listBox.SelectedItem.Tag;
            var clone = (T)tag.Clone();
            objectList.Insert(listBox.SelectedIndex + 1, clone);
            EditTrigger(editedTrigger);
            listBox.SelectedIndex++;
        }

        #endregion

        private void BtnEventParameterValuePreset_LeftClick(object sender, EventArgs e)
        {
            if (editedTrigger == null || lbEvents.SelectedItem == null || lbEventParameters.SelectedItem == null)
                return;

            var triggerEvent = (TriggerCondition)lbEvents.SelectedItem.Tag;
            var triggerEventType = GetTriggerEventType(triggerEvent.ConditionIndex);
            int paramIndex = (int)lbEventParameters.SelectedItem.Tag;

            if (triggerEventType == null)
                return;

            TriggerEventParam parameter = triggerEventType.Parameters[paramIndex];

            // If the parameter has preset options defined, then show them in a context menu instead of opening a window
            if (parameter.PresetOptions != null && parameter.PresetOptions.Count > 0)
            {
                ctxEventParameterPresetValues.ClearItems();
                parameter.PresetOptions.ForEach(ctxEventParameterPresetValues.AddItem);
                ctxEventParameterPresetValues.Open(GetCursorPoint());
                return;
            }

            int paramValue;
            switch (triggerEventType.Parameters[paramIndex].TriggerParamType)
            {
                case TriggerParamType.GlobalVariable:
                    ctxEventParameterPresetValues.ClearItems();
                    ctxEventParameterPresetValues.Width = 250;
                    map.Rules.GlobalVariables.ForEach(globalVariable => ctxEventParameterPresetValues.AddItem(globalVariable.Index.ToString(CultureInfo.InvariantCulture) + " " + globalVariable.Name));
                    ctxEventParameterPresetValues.Open(GetCursorPoint());

                    // GlobalVariable existingGlobalVariable = map.Rules.GlobalVariables.Find(gv => gv.Index == Conversions.IntFromString(triggerEvent.Parameters[paramIndex], -1));
                    // selectGlobalVariableWindow.IsForEvent = true;
                    // selectGlobalVariableWindow.Open(existingGlobalVariable);
                    break;
                case TriggerParamType.LocalVariable:
                    ctxEventParameterPresetValues.ClearItems();
                    ctxEventParameterPresetValues.Width = 250;
                    map.LocalVariables.ForEach(localVariable => ctxEventParameterPresetValues.AddItem(localVariable.Index.ToString(CultureInfo.InvariantCulture) + " " + localVariable.Name));
                    ctxEventParameterPresetValues.Open(GetCursorPoint());

                    // LocalVariable existingLocalVariable = map.LocalVariables.Find(lv => lv.Index == Conversions.IntFromString(triggerEvent.Parameters[paramIndex], -1));
                    // selectLocalVariableWindow.IsForEvent = true;
                    // selectLocalVariableWindow.Open(existingLocalVariable);
                    break;
                case TriggerParamType.HouseType:
                    selectHouseTypeWindow.IsForEvent = true;
                    paramValue = Conversions.IntFromString(triggerEvent.Parameters[paramIndex], -1);
                    if (paramValue > -1 && paramValue < map.GetHouseTypes().Count)
                        selectHouseTypeWindow.Open(map.GetHouseTypes()[paramValue]);
                    else
                        selectHouseTypeWindow.Open(null);
                    break;
                case TriggerParamType.House:
                    selectHouseWindow.IsForEvent = true;
                    paramValue = Conversions.IntFromString(triggerEvent.Parameters[paramIndex], -1);
                    if (paramValue > -1 && paramValue < map.GetHouses().Count)
                        selectHouseWindow.Open(map.GetHouses()[paramValue]);
                    else
                        selectHouseWindow.Open(null);
                    break;
                case TriggerParamType.Building:
                    paramValue = Conversions.IntFromString(triggerEvent.Parameters[paramIndex], -1);
                    BuildingType existingBuilding = paramValue < 0 || paramValue >= map.Rules.BuildingTypes.Count ? null : map.Rules.BuildingTypes[paramValue];
                    selectBuildingTypeWindow.IsForEvent = true;
                    selectBuildingTypeWindow.Tag = TriggerParamType.Building;
                    selectBuildingTypeWindow.Open(existingBuilding);
                    break;
                case TriggerParamType.Techno:
                    TechnoType existingTechno = map.GetAllTechnoTypes().Find(t => t.ININame == triggerEvent.Parameters[paramIndex]);
                    selectTechnoTypeWindow.IsForEvent = true;
                    selectTechnoTypeWindow.Open(existingTechno);
                    break;
                case TriggerParamType.Waypoint:
                case TriggerParamType.WaypointZZ:
                    ctxEventParameterPresetValues.ClearItems();
                    ctxEventParameterPresetValues.Width = 100;
                    map.Waypoints.ForEach(wp => ctxEventParameterPresetValues.AddItem(wp.Identifier.ToString(CultureInfo.InvariantCulture)));
                    ctxEventParameterPresetValues.Open(GetCursorPoint());
                    break;
                case TriggerParamType.SuperWeapon:
                    int swTypeIndex = Conversions.IntFromString(triggerEvent.Parameters[paramIndex], -1);
                    selectSuperWeaponTypeWindow.IsForEvent = true;
                    selectSuperWeaponTypeWindow.UseININameAsValue = false;
                    if (swTypeIndex > -1 && swTypeIndex < map.Rules.SuperWeaponTypes.Count)
                        selectSuperWeaponTypeWindow.Open(map.Rules.SuperWeaponTypes[swTypeIndex]);
                    break;
                case TriggerParamType.SuperWeaponName:
                    string swTypeID = triggerEvent.Parameters[paramIndex];
                    selectSuperWeaponTypeWindow.IsForEvent = true;
                    selectSuperWeaponTypeWindow.UseININameAsValue = true;
                    selectSuperWeaponTypeWindow.Open(map.Rules.SuperWeaponTypes.Find(swType => swType.ININame.Equals(swTypeID, StringComparison.Ordinal)));
                    break;
                case TriggerParamType.TeamType:
                    TeamType existingTeamType = map.TeamTypes.Find(tt => tt.ININame == triggerEvent.Parameters[paramIndex]);
                    selectTeamTypeWindow.IsForEvent = true;
                    selectTeamTypeWindow.Open(existingTeamType);
                    break;
                default:
                    break;
            }
        }

        private void CtxActionParameterPresetValues_OptionSelected(object sender, ContextMenuItemSelectedEventArgs e)
        {
            tbActionParameterValue.Text = ctxActionParameterPresetValues.Items[e.ItemIndex].Text;
        }

        private void BtnActionParameterValuePreset_LeftClick(object sender, EventArgs e)
        {
            if (editedTrigger == null || lbActions.SelectedItem == null || lbActionParameters.SelectedItem == null)
                return;

            var triggerAction = (TriggerAction)lbActions.SelectedItem.Tag;
            var triggerActionType = GetTriggerActionType(triggerAction.ActionIndex);
            int paramIndex = (int)lbActionParameters.SelectedItem.Tag;

            if (triggerActionType == null)
                return;

            TriggerActionParam parameter = triggerActionType.Parameters[paramIndex];

            // If the parameter has preset options defined, then show them in a context menu instead of opening a window
            if (parameter.PresetOptions != null && parameter.PresetOptions.Count > 0)
            {
                ctxActionParameterPresetValues.ClearItems();
                parameter.PresetOptions.ForEach(ctxActionParameterPresetValues.AddItem);
                ctxActionParameterPresetValues.Open(GetCursorPoint());
                return;
            }

            switch (parameter.TriggerParamType)
            {
                case TriggerParamType.Animation:
                    AnimType existingAnimType = map.Rules.AnimTypes.Find(at => at.Index == Conversions.IntFromString(triggerAction.Parameters[paramIndex], -1));
                    selectAnimationWindow.IsForEvent = false;
                    selectAnimationWindow.Open(existingAnimType);
                    break;
                case TriggerParamType.TeamType:
                    TeamType existingTeamType = map.TeamTypes.Find(tt => tt.ININame == triggerAction.Parameters[paramIndex]);
                    selectTeamTypeWindow.IsForEvent = false;
                    selectTeamTypeWindow.Open(existingTeamType);
                    break;
                case TriggerParamType.Trigger:
                    Trigger existingTrigger = map.Triggers.Find(tt => tt.ID == triggerAction.Parameters[paramIndex]);
                    isAttachingTrigger = false;
                    selectTriggerWindow.Open(existingTrigger);
                    break;
                case TriggerParamType.GlobalVariable:
                    ctxActionParameterPresetValues.ClearItems();
                    ctxActionParameterPresetValues.Width = 250;
                    map.Rules.GlobalVariables.ForEach(globalVariable => ctxActionParameterPresetValues.AddItem(globalVariable.Index.ToString(CultureInfo.InvariantCulture) + " " + globalVariable.Name));
                    ctxActionParameterPresetValues.Open(GetCursorPoint());

                    // GlobalVariable existingGlobalVariable = map.Rules.GlobalVariables.Find(gv => gv.Index == Conversions.IntFromString(triggerAction.Parameters[paramIndex], -1));
                    // selectGlobalVariableWindow.IsForEvent = false;
                    // selectGlobalVariableWindow.Open(existingGlobalVariable);
                    break;
                case TriggerParamType.LocalVariable:
                    ctxActionParameterPresetValues.ClearItems();
                    ctxActionParameterPresetValues.Width = 250;
                    map.LocalVariables.ForEach(localVariable => ctxActionParameterPresetValues.AddItem(localVariable.Index.ToString(CultureInfo.InvariantCulture) + " " + localVariable.Name));
                    ctxActionParameterPresetValues.Open(GetCursorPoint());

                    // LocalVariable existingLocalVariable = map.LocalVariables.Find(lv => lv.Index == Conversions.IntFromString(triggerAction.Parameters[paramIndex], -1));
                    // selectLocalVariableWindow.IsForEvent = false;
                    // selectLocalVariableWindow.Open(existingLocalVariable);
                    break;
                case TriggerParamType.HouseType:
                    int houseTypeIndex = Conversions.IntFromString(triggerAction.Parameters[paramIndex], -1);
                    selectHouseTypeWindow.IsForEvent = false;
                    if (houseTypeIndex > -1 && houseTypeIndex < map.GetHouseTypes().Count)
                        selectHouseTypeWindow.Open(map.GetHouseTypes()[houseTypeIndex]);
                    else
                        selectHouseTypeWindow.Open(null);
                    break;
                case TriggerParamType.House:
                    int houseIndex = Conversions.IntFromString(triggerAction.Parameters[paramIndex], -1);
                    selectHouseWindow.IsForEvent = false;
                    if (houseIndex > -1 && houseIndex < map.GetHouses().Count)
                        selectHouseWindow.Open(map.GetHouses()[houseIndex]);
                    else
                        selectHouseWindow.Open(null);
                    break;
                case TriggerParamType.Text:
                    selectTutorialLineWindow.Open(new TutorialLine(Conversions.IntFromString(triggerAction.Parameters[paramIndex], -1), string.Empty));
                    break;
                case TriggerParamType.Theme:
                    selectThemeWindow.Open(map.Rules.Themes.Get(Conversions.IntFromString(triggerAction.Parameters[paramIndex], -1)));
                    break;
                case TriggerParamType.Tag:
                    Tag existingTag = map.Tags.Find(tag => tag.ID == triggerAction.Parameters[paramIndex]);
                    selectTagWindow.IsForEvent = false;
                    selectTagWindow.Open(existingTag);
                    break;
                case TriggerParamType.WaypointZZ:
                case TriggerParamType.Waypoint:
                    ctxActionParameterPresetValues.ClearItems();
                    ctxActionParameterPresetValues.Width = 100;
                    map.Waypoints.ForEach(wp => ctxActionParameterPresetValues.AddItem(wp.Identifier.ToString(CultureInfo.InvariantCulture)));
                    ctxActionParameterPresetValues.Open(GetCursorPoint());
                    break;
                case TriggerParamType.StringTableEntry:
                    string label = triggerAction.Parameters[paramIndex];
                    CsfString existingString = map.StringTable.LookUpString(label) ?? new(label, string.Empty);
                    selectStringWindow.IsForEvent = false;
                    selectStringWindow.Open(existingString);
                    break;
                case TriggerParamType.SuperWeapon:
                    int swTypeIndex = Conversions.IntFromString(triggerAction.Parameters[paramIndex], -1);
                    selectSuperWeaponTypeWindow.IsForEvent = false;
                    selectSuperWeaponTypeWindow.UseININameAsValue = false;
                    if (swTypeIndex > -1 && swTypeIndex < map.Rules.SuperWeaponTypes.Count)
                        selectSuperWeaponTypeWindow.Open(map.Rules.SuperWeaponTypes[swTypeIndex]);
                    break;
                case TriggerParamType.SuperWeaponName:
                    string swTypeID = triggerAction.Parameters[paramIndex];
                    selectSuperWeaponTypeWindow.IsForEvent = false;
                    selectSuperWeaponTypeWindow.UseININameAsValue = true;
                    if (!string.IsNullOrEmpty(swTypeID))
                        selectSuperWeaponTypeWindow.Open(map.Rules.SuperWeaponTypes.Find(swType => swType.ININame.Equals(swTypeID, StringComparison.Ordinal)));
                    break;
                case TriggerParamType.ParticleSystem:
                    ParticleSystemType existingParticleSystemType = map.Rules.ParticleSystemTypes.Find(pst => pst.Index == Conversions.IntFromString(triggerAction.Parameters[paramIndex], -1));
                    selectParticleSystemTypeWindow.IsForEvent = false;
                    selectParticleSystemTypeWindow.Open(existingParticleSystemType);
                    break;
                case TriggerParamType.Speech:
                    selectSpeechWindow.IsForEvent = false;
                    EvaSpeech speech = Constants.IsRA2YR
                        ? map.Rules.Speeches.Get(triggerAction.Parameters[paramIndex])
                        : map.EditorConfig.Speeches.Get(Conversions.IntFromString(triggerAction.Parameters[paramIndex], -1));
                    selectSpeechWindow.Open(speech);
                    break;
                case TriggerParamType.Sound:
                    selectSoundWindow.IsForEvent = false;
                    Sound sound = Constants.IsRA2YR
                        ? map.Rules.Sounds.Get(triggerAction.Parameters[paramIndex])
                        : map.Rules.Sounds.Get(Conversions.IntFromString(triggerAction.Parameters[paramIndex], -1));
                    selectSoundWindow.Open(sound);
                    break;
                case TriggerParamType.BuildingName:
                    selectBuildingTypeWindow.IsForEvent = false;
                    selectBuildingTypeWindow.Tag = TriggerParamType.BuildingName;
                    BuildingType buildingType = map.Rules.BuildingTypes.Find(bt => bt.ININame == triggerAction.Parameters[paramIndex]);
                    selectBuildingTypeWindow.Open(buildingType);
                    break;
                case TriggerParamType.Color:
                    int colorIndex = Conversions.IntFromString(triggerAction.Parameters[paramIndex], -1);
                    selectColorsWindow.IsForEvent = false;
                    if (colorIndex > -1 && colorIndex < map.Rules.Colors.Count)
                        selectColorsWindow.Open(map.Rules.Colors[colorIndex]);
                    else
                        selectColorsWindow.Open(null);
                    break;                    
                default:
                    break;
            }
        }

        private void btnActionGoToTarget_LeftClick(object sender, EventArgs e)
        {
            if (lbActions.SelectedItem == null)
                return;

            if (lbActionParameters.SelectedItem == null)
                return;

            GetTriggerActionAndParamIndex(out TriggerAction triggerAction, out int paramIndex);            

            var triggerActionType = GetTriggerActionType(triggerAction.ActionIndex);
            var triggerActionParam = triggerActionType.Parameters[paramIndex];
            var triggerParamType = triggerActionParam.TriggerParamType;            

            switch (triggerParamType)
            {
                case TriggerParamType.Trigger:
                    var triggerId = triggerAction.Parameters[paramIndex];
                    var triggerIndex = lbTriggers.Items.FindIndex(listBoxTrigger => ((Trigger)listBoxTrigger.Tag).ID == triggerId);
                    if (triggerIndex == -1)
                        break;

                    lbTriggers.SelectedIndex = triggerIndex;
                    break;

                case TriggerParamType.TeamType:
                    var teamTypeId = triggerAction.Parameters[paramIndex];
                    var teamType = map.TeamTypes.Find(teamType => teamType.ININame == teamTypeId);
                    if (teamType == null) 
                        break;

                    OpenTeamType(teamType);
                    break;

                case TriggerParamType.Waypoint:
                case TriggerParamType.WaypointZZ:
                    int waypointNumber;
                    if (triggerParamType == TriggerParamType.WaypointZZ)
                    {
                        waypointNumber = Helpers.GetWaypointNumberFromAlphabeticalString(triggerAction.Parameters[paramIndex]);
                    }
                    else
                    {
                        waypointNumber = Conversions.IntFromString(triggerAction.Parameters[paramIndex], -1);
                    }

                    if (waypointNumber == -1)
                        break;
                    
                    Waypoint waypoint = map.Waypoints.Find(wp => wp.Identifier == waypointNumber);
                    if (waypoint == null)
                        break;

                    cursorActionTarget.Camera.CenterOnCell(waypoint.Position);
                    break;

                default:
                    break;
            }
        }

        private void AnimationWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectAnimationWindow.SelectedObject == null)
                return;

            AssignParamValue(selectAnimationWindow.IsForEvent, selectAnimationWindow.SelectedObject.Index);
        }

        private void BuildingTypeWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectBuildingTypeWindow.SelectedObject == null)
                return;

            if ((TriggerParamType)selectBuildingTypeWindow.Tag == TriggerParamType.BuildingName)
                AssignParamValue(selectBuildingTypeWindow.IsForEvent, selectBuildingTypeWindow.SelectedObject.ININame);
            else
                AssignParamValue(selectBuildingTypeWindow.IsForEvent, selectBuildingTypeWindow.SelectedObject.Index);
        }

        private void ThemeDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectThemeWindow.SelectedObject == null)
                return;

            AssignParamValue(selectThemeWindow.IsForEvent, selectThemeWindow.SelectedObject.Index);
        }

        private void TechnoTypeDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectTechnoTypeWindow.SelectedObject == null)
                return;

            AssignParamValue(selectTechnoTypeWindow.IsForEvent, selectTechnoTypeWindow.SelectedObject.ININame);
        }

        private void TagDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectTagWindow.SelectedObject == null)
                return;

            AssignParamValue(selectTagWindow.IsForEvent, selectTagWindow.SelectedObject.ID);
        }

        private void TutorialDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectTutorialLineWindow.SelectedObject.ID < 0 || selectTutorialLineWindow.SelectedObject.Text == null)
                return;

            AssignParamValue(selectTutorialLineWindow.IsForEvent, selectTutorialLineWindow.SelectedObject.ID);
        }

        private void HouseTypeDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectHouseTypeWindow.SelectedObject == null)
                return;

            int houseTypeIndex = selectHouseTypeWindow.SelectedObject.Index;
            AssignParamValue(selectHouseTypeWindow.IsForEvent, houseTypeIndex);
        }

        private void HouseDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectHouseWindow.SelectedObject == null)
                return;

            int houseIndex = map.GetHouses().FindIndex(h => h == selectHouseWindow.SelectedObject);
            AssignParamValue(selectHouseWindow.IsForEvent, houseIndex);
        }

        private void LocalVariableDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectLocalVariableWindow.SelectedObject == null)
                return;

            int localVariableIndex = selectLocalVariableWindow.SelectedObject.Index;
            AssignParamValue(selectLocalVariableWindow.IsForEvent, localVariableIndex);
        }

        private void GlobalVariableDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectGlobalVariableWindow.SelectedObject == null)
                return;

            int globalVariableIndex = selectGlobalVariableWindow.SelectedObject.Index;
            AssignParamValue(selectGlobalVariableWindow.IsForEvent, globalVariableIndex);
        }

        private void StringDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectStringWindow.SelectedObject == null)
                return;

            AssignParamValue(selectStringWindow.IsForEvent, selectStringWindow.SelectedObject.ID);
        }

        private void SpeechDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectSpeechWindow.SelectedObject == null)
                return;

            var speech = selectSpeechWindow.SelectedObject;
            AssignParamValue(selectSpeechWindow.IsForEvent, Constants.IsRA2YR ? speech.Name : speech.Index.ToString(CultureInfo.InvariantCulture));
        }

        private void SoundDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectSoundWindow.SelectedObject == null)
                return;

            var sound = selectSoundWindow.SelectedObject;
            AssignParamValue(selectSoundWindow.IsForEvent, Constants.IsRA2YR ? sound.Name : sound.Index.ToString(CultureInfo.InvariantCulture));
        }

        private void ParticleSystemTypeDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectParticleSystemTypeWindow.SelectedObject == null)
                return;

            AssignParamValue(selectParticleSystemTypeWindow.IsForEvent, selectParticleSystemTypeWindow.SelectedObject.Index);
        }

        private void SuperWeaponDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectSuperWeaponTypeWindow.SelectedObject == null)
                return;

            var swType = selectSuperWeaponTypeWindow.SelectedObject;

            if (selectSuperWeaponTypeWindow.UseININameAsValue)
                AssignParamValue(selectSuperWeaponTypeWindow.IsForEvent, swType.ININame);
            else
                AssignParamValue(selectSuperWeaponTypeWindow.IsForEvent, swType.Index);
        }

        private void ColorDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectColorsWindow.SelectedObject == null)
                return;

            int colorIndex = selectColorsWindow.SelectedObject.Index;
            AssignParamValue(selectColorsWindow.IsForEvent, colorIndex);
        }

        private void AssignParamValue(bool isForEvent, int paramValue)
        {
            if (isForEvent)
            {
                GetTriggerEventAndParamIndex(out TriggerCondition triggerCondition, out int paramIndex);
                triggerCondition.Parameters[paramIndex] = paramValue.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                GetTriggerActionAndParamIndex(out TriggerAction triggerAction, out int paramIndex);
                triggerAction.Parameters[paramIndex] = paramValue.ToString(CultureInfo.InvariantCulture);
            }

            EditTrigger(editedTrigger);
        }

        private void AssignParamValue(bool isForEvent, string paramValue)
        {
            if (isForEvent)
            {
                GetTriggerEventAndParamIndex(out TriggerCondition triggerCondition, out int paramIndex);
                triggerCondition.Parameters[paramIndex] = paramValue;
            }
            else
            {
                GetTriggerActionAndParamIndex(out TriggerAction triggerAction, out int paramIndex);
                triggerAction.Parameters[paramIndex] = paramValue;
            }

            EditTrigger(editedTrigger);
        }

        private void TriggerWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (isAttachingTrigger)
            {
                editedTrigger.LinkedTrigger = selectTriggerWindow.SelectedObject;
                EditTrigger(editedTrigger);
                return;
            }

            if (selectTriggerWindow.SelectedObject == null)
                return;

            GetTriggerActionAndParamIndex(out TriggerAction triggerAction, out int paramIndex);
            triggerAction.Parameters[paramIndex] = selectTriggerWindow.SelectedObject.ID;
            EditTrigger(editedTrigger);
        }

        private void TeamTypeWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (selectTeamTypeWindow.SelectedObject == null)
                return;

            var teamType = selectTeamTypeWindow.SelectedObject;
            AssignParamValue(selectTeamTypeWindow.IsForEvent, teamType.ININame);
        }

        private void GetTriggerActionAndParamIndex(out TriggerAction triggerAction, out int paramIndex)
        {
            triggerAction = (TriggerAction)lbActions.SelectedItem.Tag;
            paramIndex = (int)lbActionParameters.SelectedItem.Tag;
        }

        private void GetTriggerEventAndParamIndex(out TriggerCondition triggerEvent, out int paramIndex)
        {
            triggerEvent = (TriggerCondition)lbEvents.SelectedItem.Tag;
            paramIndex = (int)lbEventParameters.SelectedItem.Tag;
        }

        private void BtnNewTrigger_LeftClick(object sender, EventArgs e)
        {
            map.TriggersChanged -= Map_TriggersChanged;

            var newTrigger = new Trigger(map.GetNewUniqueInternalId()) { Name = "New trigger", HouseType = "Neutral" };
            map.AddTrigger(newTrigger);
            map.AddTag(new Tag() { ID = map.GetNewUniqueInternalId(), Name = "New tag", Trigger = newTrigger });
            ListTriggers();
            SelectTrigger(newTrigger);

            map.TriggersChanged += Map_TriggersChanged;
        }

        private void BtnCloneTrigger_LeftClick(object sender, EventArgs e)
        {
            if (editedTrigger == null)
                return;

            map.TriggersChanged -= Map_TriggersChanged;

            var originalTag = map.Tags.Find(t => t.Trigger == editedTrigger);

            var clone = editedTrigger.Clone(map.GetNewUniqueInternalId());
            map.AddTrigger(clone);
            map.AddTag(new Tag() { ID = map.GetNewUniqueInternalId(), Name = clone.Name + " (tag)", Trigger = clone, Repeating = originalTag == null ? 0 : originalTag.Repeating });
            ListTriggers();
            SelectTrigger(clone);

            map.TriggersChanged += Map_TriggersChanged;
        }

        private void BtnDeleteTrigger_LeftClick(object sender, EventArgs e)
        {
            if (editedTrigger == null)
                return;

            if (Keyboard.IsShiftHeldDown())
            {
                DeleteTrigger();
            }
            else
            {
                var msgBox = EditorMessageBox.Show(WindowManager,
                    Translate(this, "DeleteTrigger.Title", "Are you sure?"),
                    string.Format(Translate(this, "DeleteTrigger.Description", "Do you really want to delete trigger \"{0}\"?" + Environment.NewLine + Environment.NewLine +
                        "(You can hold Shift to skip this confirmation dialog.)"),
                        editedTrigger.Name), 
                    MessageBoxButtons.YesNo);

                msgBox.YesClickedAction = _ => DeleteTrigger();
            }
        }

        private void DeleteTrigger()
        {
            map.TriggersChanged -= Map_TriggersChanged;

            map.RemoveTrigger(editedTrigger);
            map.Triggers.ForEach(t => { if (t.LinkedTrigger == editedTrigger) t.LinkedTrigger = null; });
            map.RemoveTagsAssociatedWithTrigger(editedTrigger);
            editedTrigger = null;

            ListTriggers();

            map.TriggersChanged += Map_TriggersChanged;
        }

        public void SelectTrigger(Trigger trigger)
        {
            lbTriggers.SelectedIndex = lbTriggers.Items.FindIndex(item => item.Tag == trigger);
            if (lbTriggers.SelectedItem != null)
                lbTriggers.ScrollToSelectedElement();
        }

        private void EventWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (editedTrigger == null || selectEventWindow.SelectedObject == null)
                return;

            TriggerEventType triggerEventType = selectEventWindow.SelectedObject;

            if (selectEventWindow.IsAddingNew)
            {
                editedTrigger.Conditions.Add(new TriggerCondition(triggerEventType));
                EditTrigger(editedTrigger);
                lbEvents.SelectedIndex = lbEvents.Items.Count - 1;
            }
            else
            {
                if (lbEvents.SelectedItem == null)
                    return;
            }

            TriggerCondition condition = editedTrigger.Conditions[lbEvents.SelectedIndex];
            condition.ConditionIndex = selectEventWindow.SelectedObject.ID;

            for (int i = 0; i < TriggerEventType.MAX_PARAM_COUNT; i++)
            {
                if ((int)triggerEventType.Parameters[i].TriggerParamType < 0)
                {
                    condition.Parameters[i] = Math.Abs((int)triggerEventType.Parameters[i].TriggerParamType).ToString(CultureInfo.InvariantCulture);
                    continue;
                }

                if (triggerEventType.Parameters[i].TriggerParamType == TriggerParamType.Unused)
                {
                    // additional params need to be empty instead of 0 if they're unused
                    if (i >= TriggerCondition.DEF_PARAM_COUNT)
                        condition.Parameters[i] = string.Empty;
                    else
                        condition.Parameters[i] = "0";
                }
            }

            EditTrigger(editedTrigger);
        }

        private void ActionWindowDarkeningPanel_Hidden(object sender, EventArgs e)
        {
            if (editedTrigger == null || selectActionWindow.SelectedObject == null)
                return;

            TriggerActionType triggerActionType = selectActionWindow.SelectedObject;

            if (selectActionWindow.IsAddingNew)
            {
                if (triggerActionType == null)
                    return;

                editedTrigger.Actions.Add(CreateTriggerAction(triggerActionType));
                EditTrigger(editedTrigger);
                lbActions.SelectedIndex = lbActions.Items.Count - 1;
            }
            else
            {
                if (lbActions.SelectedItem == null)
                    return;

                TriggerAction existingAction = editedTrigger.Actions[lbActions.SelectedIndex];
                existingAction.ActionIndex = selectActionWindow.SelectedObject.ID;
                SetTriggerActionHardcodedParameters(existingAction);
            }

            EditTrigger(editedTrigger);
        }

        private TriggerAction CreateTriggerAction(TriggerActionType triggerActionType)
        {
            var triggerAction = new TriggerAction();
            triggerAction.ActionIndex = triggerActionType.ID;
            SetTriggerActionHardcodedParameters(triggerAction);

            return triggerAction;
        }

        private void SetTriggerActionHardcodedParameters(TriggerAction triggerAction)
        {
            if (!map.EditorConfig.TriggerActionTypes.TryGetValue(triggerAction.ActionIndex, out var triggerActionType))
            {
                Logger.Log($"{nameof(TriggersWindow)}.{nameof(SetTriggerActionHardcodedParameters)}: Unknown action type {triggerAction.ActionIndex}");
                return;
            }

            for (int i = 0; i < TriggerActionType.MAX_PARAM_COUNT; i++)
            {
                if ((int)triggerActionType.Parameters[i].TriggerParamType < 0)
                {
                    triggerAction.Parameters[i] = Math.Abs((int)triggerActionType.Parameters[i].TriggerParamType).ToString(CultureInfo.InvariantCulture);
                    continue;
                }

                // Special dirty hack for handling for P7Type, defaults to "WaypointZZ"
                if (i == TriggerActionType.MAX_PARAM_COUNT - 1 && triggerActionType.Parameters[i].TriggerParamType == TriggerParamType.Unused)
                {
                    triggerAction.Parameters[i] = "A";
                    continue;
                }

                if (triggerActionType.Parameters[i].TriggerParamType == TriggerParamType.Unused)
                {
                    triggerAction.Parameters[i] = "0";
                    continue;
                }
            }
        }

        private void BtnAddAction_LeftClick(object sender, EventArgs e)
        {
            if (editedTrigger == null)
                return;

            selectActionWindow.IsAddingNew = true;
            selectActionWindow.Open(null);
        }

        private void BtnDeleteAction_LeftClick(object sender, EventArgs e)
        {
            if (editedTrigger == null || lbActions.SelectedItem == null)
                return;

            editedTrigger.Actions.RemoveAt(lbActions.SelectedIndex);
            EditTrigger(editedTrigger);
        }

        private void BtnCloneAction_LeftClick(object sender, EventArgs e) => CloneEventOrAction(lbActions, editedTrigger.Actions);

        private void BtnAddEvent_LeftClick(object sender, EventArgs e)
        {
            if (editedTrigger == null)
                return;

            selectEventWindow.IsAddingNew = true;
            selectEventWindow.Open(null);
        }

        private void BtnDeleteEvent_LeftClick(object sender, EventArgs e)
        {
            if (editedTrigger == null || lbEvents.SelectedItem == null)
                return;

            editedTrigger.Conditions.RemoveAt(lbEvents.SelectedIndex);
            EditTrigger(editedTrigger);
        }

        private void BtnCloneEvent_LeftClick(object sender, EventArgs e) => CloneEventOrAction(lbEvents, editedTrigger.Conditions);

        private void LbTriggers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbTriggers.SelectedItem == null)
            {
                EditTrigger(null);
                return;
            }

            EditTrigger((Trigger)lbTriggers.SelectedItem.Tag);
        }

        public void Open()
        {
            ListTriggers();
            RefreshHouses();
            Show();
        }

        private void RefreshHouses()
        {
            ddHouseType.Items.Clear();
            map.GetHouseTypes().ForEach(ht => ddHouseType.AddItem(ht.ININame, Helpers.GetHouseTypeUITextColor(ht)));
        }

        private void ListTriggers()
        {
            lbTriggers.Clear();

            IEnumerable<Trigger> sortedTriggers = map.Triggers;

            var shouldViewTop = false; // when filtering the scroll bar should update so we use a flag here
            if (tbFilter.Text != string.Empty && tbFilter.Text != tbFilter.Suggestion)
            {
                sortedTriggers = sortedTriggers.Where(sortedTrigger => sortedTrigger.Name.Contains(tbFilter.Text, StringComparison.CurrentCultureIgnoreCase));
                shouldViewTop = true;
            }

            switch (TriggerSortMode)
            {
                case TriggerSortMode.Color:
                    sortedTriggers = sortedTriggers.OrderBy(t => t.EditorColor).ThenBy(t => t.ID);
                    break;
                case TriggerSortMode.Name:
                    sortedTriggers = sortedTriggers.OrderBy(t => t.Name).ThenBy(t => t.ID);
                    break;                
                case TriggerSortMode.ColorThenName:
                    sortedTriggers = sortedTriggers.OrderBy(t => t.EditorColor).ThenBy(t => t.Name);
                    break;
                case TriggerSortMode.ID:
                default:
                    sortedTriggers = sortedTriggers.OrderBy(t => t.ID);
                    break;
            }

            foreach (Trigger trigger in sortedTriggers)
            {
                lbTriggers.AddItem(new XNAListBoxItem()
                { 
                    Text = trigger.Name, 
                    Tag = trigger, 
                    TextColor = trigger.EditorColor == null ? lbTriggers.DefaultItemColor : trigger.XNAColor
                });
            }

            LbTriggers_SelectedIndexChanged(this, EventArgs.Empty);

            if (shouldViewTop)
                lbTriggers.TopIndex = 0;
        }

        private void EditTrigger(Trigger trigger)
        {
            lbEvents.SelectedIndexChanged -= LbEvents_SelectedIndexChanged;
            lbActions.SelectedIndexChanged -= LbActions_SelectedIndexChanged;
            tbName.TextChanged -= TbName_TextChanged;
            ddHouseType.SelectedIndexChanged -= DdHouse_SelectedIndexChanged;
            ddType.SelectedIndexChanged -= DdType_SelectedIndexChanged;
            chkDisabled.CheckedChanged -= ChkDisabled_CheckedChanged;
            selAttachedTrigger.LeftClick -= SelAttachedTrigger_LeftClick;
            ddTriggerColor.SelectedIndexChanged -= DdTriggerColor_SelectedIndexChanged;
            chkEasy.CheckedChanged -= ChkEasy_CheckedChanged;
            chkMedium.CheckedChanged -= ChkMedium_CheckedChanged;
            chkHard.CheckedChanged -= ChkHard_CheckedChanged;

            editedTrigger = trigger;

            if (editedTrigger == null)
            {
                tbName.Text = string.Empty;
                ddHouseType.SelectedIndex = -1;
                ddType.SelectedIndex = -1;
                selAttachedTrigger.Text = string.Empty;
                chkDisabled.Checked = false;

                lbEvents.Clear();
                selEventType.Text = string.Empty;
                panelEventDescription.Text = string.Empty;
                lbEventParameters.Clear();
                tbEventParameterValue.Text = string.Empty;

                lbActions.Clear();
                selActionType.Text = string.Empty;
                panelActionDescription.Text = string.Empty;
                lbActionParameters.Clear();
                tbActionParameterValue.Text = string.Empty;

                btnActionGoToTarget.Disable();

                return;
            }

            var tag = map.Tags.Find(t => t.Trigger == editedTrigger);
            if (tag == null)
            {
                ddType.AllowDropDown = false;

                if (ddType.Items.Count < 4)
                    ddType.AddItem(Translate(this, "NoTagError", "Error: No tag exists for this trigger!"));
            }
            else
            {
                ddType.AllowDropDown = true;

                if (ddType.Items.Count > 3)
                    ddType.Items.RemoveAt(3);
            }

            tbName.Text = editedTrigger.Name;
            ddHouseType.SelectedIndex = map.GetHouseTypes().FindIndex(h => h.ININame == trigger.HouseType);
            ddType.SelectedIndex = tag == null ? 3 : tag.Repeating;
            selAttachedTrigger.Text = editedTrigger.LinkedTrigger == null ? Constants.NoneValue1 : editedTrigger.LinkedTrigger.Name;
            selAttachedTrigger.Tag = editedTrigger.LinkedTrigger;
            chkDisabled.Checked = editedTrigger.Disabled;
            chkEasy.Checked = editedTrigger.Easy;
            chkMedium.Checked = editedTrigger.Normal;
            chkHard.Checked = editedTrigger.Hard;
            ddTriggerColor.SelectedIndex = ddTriggerColor.Items.FindIndex(item => item.Text == editedTrigger.EditorColor);
            if (ddTriggerColor.SelectedIndex < 0)
                ddTriggerColor.SelectedIndex = 0;

            lbEvents.ViewTop = 0;
            lbActions.ViewTop = 0;

            lbEvents.Clear();
            editedTrigger.Conditions.ForEach(c => AddEvent(c));

            lbActions.Clear();
            editedTrigger.Actions.ForEach(a => AddAction(a));

            LbEvents_SelectedIndexChanged(this, EventArgs.Empty);
            LbActions_SelectedIndexChanged(this, EventArgs.Empty);

            lbEvents.SelectedIndexChanged += LbEvents_SelectedIndexChanged;
            lbActions.SelectedIndexChanged += LbActions_SelectedIndexChanged;
            tbName.TextChanged += TbName_TextChanged;
            ddHouseType.SelectedIndexChanged += DdHouse_SelectedIndexChanged;
            ddType.SelectedIndexChanged += DdType_SelectedIndexChanged;
            chkDisabled.CheckedChanged += ChkDisabled_CheckedChanged;
            selAttachedTrigger.LeftClick += SelAttachedTrigger_LeftClick;
            ddTriggerColor.SelectedIndexChanged += DdTriggerColor_SelectedIndexChanged;
            chkEasy.CheckedChanged += ChkEasy_CheckedChanged;
            chkMedium.CheckedChanged += ChkMedium_CheckedChanged;
            chkHard.CheckedChanged += ChkHard_CheckedChanged;
        }

        private void ChkEasy_CheckedChanged(object sender, EventArgs e)
        {
            editedTrigger.Easy = chkEasy.Checked;
        }

        private void ChkMedium_CheckedChanged(object sender, EventArgs e)
        {
            editedTrigger.Normal = chkMedium.Checked;
        }

        private void ChkHard_CheckedChanged(object sender, EventArgs e)
        {
            editedTrigger.Hard = chkHard.Checked;
        }

        private void SelAttachedTrigger_LeftClick(object sender, EventArgs e)
        {
            isAttachingTrigger = true;
            selectTriggerWindow.Open((Trigger)selAttachedTrigger.Tag);
        }

        private void DdTriggerColor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ddTriggerColor.SelectedIndex < 1)
            {
                editedTrigger.EditorColor = null;
                lbTriggers.SelectedItem.TextColor = lbTriggers.DefaultItemColor;
                return;
            }

            editedTrigger.EditorColor = ddTriggerColor.SelectedItem.Text;
            lbTriggers.SelectedItem.TextColor = ddTriggerColor.SelectedItem.TextColor.Value;
        }

        private void DdType_SelectedIndexChanged(object sender, EventArgs e)
        {
            var tag = map.Tags.Find(t => t.Trigger == editedTrigger);
            if (tag != null)
                tag.Repeating = ddType.SelectedIndex;
        }

        private void DdHouse_SelectedIndexChanged(object sender, EventArgs e)
        {
            editedTrigger.HouseType = ddHouseType.SelectedItem.Text;
        }

        private void ChkDisabled_CheckedChanged(object sender, EventArgs e)
        {
            editedTrigger.Disabled = chkDisabled.Checked;
        }

        private void TbName_TextChanged(object sender, EventArgs e)
        {
            var tag = map.Tags.Find(t => t.Trigger == editedTrigger);
            if (tag != null)
                tag.Name = tbName.Text + " (tag)";

            editedTrigger.Name = tbName.Text;
            lbTriggers.SelectedItem.Text = tbName.Text;
        }

        private void TbFilter_TextChanged(object sender, EventArgs e) => ListTriggers();

        private void LbActions_SelectedIndexChanged(object sender, EventArgs e)
        {
            lbActionParameters.SelectedIndexChanged -= LbActionParameters_SelectedIndexChanged;

            if (lbActions.SelectedItem == null)
            {
                selActionType.Text = string.Empty;
                panelActionDescription.Text = string.Empty;
                lbActionParameters.Clear();
                tbActionParameterValue.Text = string.Empty;

                btnActionGoToTarget.Disable();
                return;
            }

            var triggerAction = (TriggerAction)lbActions.SelectedItem.Tag;
            TriggerActionType triggerActionType = map.EditorConfig.TriggerActionTypes.GetValueOrDefault(triggerAction.ActionIndex);

            selActionType.Text = triggerAction.ActionIndex + " " + (triggerActionType == null ? Translate(this, "UnknownActionType", "Unknown") : triggerActionType.Name);
            panelActionDescription.Text = triggerActionType == null ? Translate(this, "UnknownActionDescription", "Unknown action. It has most likely been added with another editor.") : triggerActionType.Description;

            lbActionParameters.Clear();
            if (triggerActionType == null)
            {
                for (int i = 0; i < TriggerActionType.MAX_PARAM_COUNT; i++)
                {
                    lbActionParameters.AddItem(new XNAListBoxItem() { Text = $"Parameter {i}", Tag = i });
                }
            }
            else
            {
                for (int i = 0; i < triggerActionType.Parameters.Length; i++)
                {
                    var param = triggerActionType.Parameters[i];
                    if (param.TriggerParamType == TriggerParamType.Unused || (int)param.TriggerParamType < 0)
                        continue;

                    lbActionParameters.AddItem(new XNAListBoxItem() { Text = param.NameOverride ?? param.TriggerParamType.ToString(), Tag = i });
                }
            }

            if (lbActionParameters.SelectedItem == null && lbActionParameters.Items.Count > 0)
                lbActionParameters.SelectedIndex = 0;

            LbActionParameters_SelectedIndexChanged(this, EventArgs.Empty);

            lbActionParameters.SelectedIndexChanged += LbActionParameters_SelectedIndexChanged;
            selActionType.LeftClick += SelActionType_LeftClick;
        }

        private void SelActionType_LeftClick(object sender, EventArgs e)
        {
            if (editedTrigger == null || lbActions.SelectedItem == null)
                return;

            int actionTypeIndex = editedTrigger.Actions[lbActions.SelectedIndex].ActionIndex;
            selectActionWindow.IsAddingNew = false;
            selectActionWindow.Open(GetTriggerActionType(actionTypeIndex));
        }

        private void LbActionParameters_SelectedIndexChanged(object sender, EventArgs e)
        {
            tbActionParameterValue.TextChanged -= TbActionParameterValue_TextChanged;

            if (lbActionParameters.SelectedItem == null || editedTrigger == null || lbActions.SelectedItem == null)
            {
                tbActionParameterValue.Text = string.Empty;
                btnActionGoToTarget.Disable();
                return;
            }

            TriggerAction triggerAction = editedTrigger.Actions[lbActions.SelectedIndex];
            int paramNumber = (int)lbActionParameters.SelectedItem.Tag;
            var triggerActionType = GetTriggerActionType(triggerAction.ActionIndex);
            if (triggerActionType != null)
            {
                var triggerActionParam = triggerActionType.Parameters[paramNumber];
                var triggerParamType = triggerActionParam.TriggerParamType;

                tbActionParameterValue.Text = GetParamValueText(triggerAction.Parameters[paramNumber], triggerParamType, triggerActionParam.PresetOptions);
                tbActionParameterValue.TextColor = GetParamValueColor(triggerAction.Parameters[paramNumber], triggerParamType);

                bool isSupportedGoToSourceParamType = supportedGoToSourceTriggerParamTypes.Contains(triggerParamType);
                if (isSupportedGoToSourceParamType)
                {
                    btnActionGoToTarget.Enable();
                }
                else
                {
                    btnActionGoToTarget.Disable();
                }
            }
            else
            {
                tbActionParameterValue.Text = triggerAction.Parameters[paramNumber];
                tbActionParameterValue.TextColor = UISettings.ActiveSettings.AltColor;

                btnActionGoToTarget.Disable();
            }

            tbActionParameterValue.TextChanged += TbActionParameterValue_TextChanged;
        }

        private void TbActionParameterValue_TextChanged(object sender, EventArgs e)
        {
            if (lbActionParameters.SelectedItem == null || editedTrigger == null || lbActions.SelectedItem == null)
            {
                return;
            }

            int paramNumber = (int)lbActionParameters.SelectedItem.Tag;
            var triggerAction = (TriggerAction)lbActions.SelectedItem.Tag;

            string value = tbActionParameterValue.Text.Split(' ')[0];
            var triggerActionType = GetTriggerActionType(triggerAction.ActionIndex);

            if (triggerActionType != null)
            {
                var triggerParamType = triggerActionType.Parameters[paramNumber].TriggerParamType;

                if (triggerParamType == TriggerParamType.WaypointZZ)
                {
                    // Write waypoint with A-ZZ notation
                    value = Helpers.WaypointNumberToAlphabeticalString(Conversions.IntFromString(value, 0));
                }
                else if (triggerParamType == TriggerParamType.Float)
                {
                    // Float values need to be converted into integers
                    if (float.TryParse(value, CultureInfo.InvariantCulture, out float floatValue))
                        value = BitConverter.ToInt32(BitConverter.GetBytes(floatValue)).ToString(CultureInfo.InvariantCulture);
                }
            }

            triggerAction.Parameters[paramNumber] = value;
        }

        private void AddAction(TriggerAction action)
        {
            var triggerActionType = map.EditorConfig.TriggerActionTypes.GetValueOrDefault(action.ActionIndex);

            if (triggerActionType == null)
            {
                lbActions.AddItem(new XNAListBoxItem() { Text = action.ActionIndex + Translate(this, "UnknownActionType", "Unknown"), Tag = action });
                return;
            }

            lbActions.AddItem(new XNAListBoxItem() { Text = action.ActionIndex + " " + triggerActionType.Name, Tag = action });
        }

        private void LbEvents_SelectedIndexChanged(object sender, EventArgs e)
        {
            lbEventParameters.SelectedIndexChanged -= LbEventParameters_SelectedIndexChanged;
            selEventType.LeftClick -= SelEventType_LeftClick;

            if (lbEvents.SelectedItem == null)
            {
                selEventType.Text = string.Empty;
                panelEventDescription.Text = string.Empty;
                lbEventParameters.Clear();
                tbEventParameterValue.Text = string.Empty;
                return;
            }

            var triggerCondition = (TriggerCondition)lbEvents.SelectedItem.Tag;
            TriggerEventType triggerEventType = map.EditorConfig.TriggerEventTypes.GetValueOrDefault(triggerCondition.ConditionIndex);

            selEventType.Text = triggerCondition.ConditionIndex + " " + (triggerEventType == null ? Translate(this, "UnknownEventType", "Unknown") : triggerEventType.Name);
            panelEventDescription.Text = triggerEventType == null ? Translate(this, "UnknownEventDescription", "Unknown event. It has most likely been added with another editor.") : triggerEventType.Description;

            lbEventParameters.Clear();
            if (triggerEventType == null)
            {
                for (int i = 0; i < TriggerEventType.MAX_PARAM_COUNT; i++)
                {
                    lbEventParameters.AddItem(new XNAListBoxItem() { Text = $"Parameter {i}", Tag = i });
                }
            }
            else
            {
                for (int i = 0; i < triggerEventType.Parameters.Length; i++)
                {
                    var param = triggerEventType.Parameters[i];
                    if (param.TriggerParamType == TriggerParamType.Unused || (int)param.TriggerParamType < 0)
                        continue;

                    lbEventParameters.AddItem(new XNAListBoxItem() { Text = param.NameOverride ?? param.TriggerParamType.ToString(), Tag = i });
                }
            }

            if (lbEventParameters.SelectedItem == null && lbEventParameters.Items.Count > 0)
                lbEventParameters.SelectedIndex = 0;

            LbEventParameters_SelectedIndexChanged(this, EventArgs.Empty);

            lbEventParameters.SelectedIndexChanged += LbEventParameters_SelectedIndexChanged;
            selEventType.LeftClick += SelEventType_LeftClick;
        }

        private void SelEventType_LeftClick(object sender, EventArgs e)
        {
            if (editedTrigger == null || lbEvents.SelectedItem == null)
                return;

            int eventTypeIndex = editedTrigger.Conditions[lbEvents.SelectedIndex].ConditionIndex;
            selectEventWindow.IsAddingNew = false;
            selectEventWindow.Open(GetTriggerEventType(eventTypeIndex));
        }

        private void LbEventParameters_SelectedIndexChanged(object sender, EventArgs e)
        {
            tbEventParameterValue.TextChanged -= TbEventParameterValue_TextChanged;

            if (lbEventParameters.SelectedItem == null || editedTrigger == null || lbEvents.SelectedItem == null)
            {
                tbEventParameterValue.Text = string.Empty;
                return;
            }

            TriggerCondition triggerCondition = editedTrigger.Conditions[lbEvents.SelectedIndex];
            int paramNumber = (int)lbEventParameters.SelectedItem.Tag;
            var triggerEventType = GetTriggerEventType(editedTrigger.Conditions[lbEvents.SelectedIndex].ConditionIndex);
            var triggerEventParam = triggerEventType.Parameters[paramNumber];            

            if (triggerEventType != null)
            {
                var triggerParamType = triggerEventType.Parameters[paramNumber]?.TriggerParamType ?? TriggerParamType.Unknown;

                tbEventParameterValue.Text = GetParamValueText(triggerCondition.Parameters[paramNumber], triggerParamType, triggerEventParam.PresetOptions);
                tbEventParameterValue.TextColor = GetParamValueColor(triggerCondition.Parameters[paramNumber], triggerParamType);
            }
            else
            {
                tbEventParameterValue.Text = triggerCondition.Parameters[paramNumber];
                tbEventParameterValue.TextColor = UISettings.ActiveSettings.AltColor;
            }

            tbEventParameterValue.TextChanged += TbEventParameterValue_TextChanged;
        }

        private void CtxEventParameterPresetValues_OptionSelected(object sender, ContextMenuItemSelectedEventArgs e)
        {
            tbEventParameterValue.Text = ctxEventParameterPresetValues.Items[e.ItemIndex].Text;
        }

        private void TbEventParameterValue_TextChanged(object sender, EventArgs e)
        {
            if (lbEventParameters.SelectedItem == null || editedTrigger == null || lbEvents.SelectedItem == null)
            {
                return;
            }

            int paramNumber = (int)lbEventParameters.SelectedItem.Tag;
            var triggerCondition = (TriggerCondition)lbEvents.SelectedItem.Tag;

            string value = tbEventParameterValue.Text.Split(' ')[0];
            var triggerEventType = GetTriggerActionType(triggerCondition.ConditionIndex);

            if (triggerEventType != null &&
                triggerEventType.Parameters[paramNumber].TriggerParamType == TriggerParamType.WaypointZZ)
            {
                // Write waypoint with A-ZZ notation
                value = Helpers.WaypointNumberToAlphabeticalString(Conversions.IntFromString(value, 0));
            }

            triggerCondition.Parameters[paramNumber] = value;
        }

        private void AddEvent(TriggerCondition condition)
        {
            var triggerEventType = map.EditorConfig.TriggerEventTypes.GetValueOrDefault(condition.ConditionIndex);

            if (triggerEventType == null)
            {
                lbEvents.AddItem(new XNAListBoxItem() { Text = condition.ConditionIndex + Translate(this, "UnknownEventName",  "Unknown"), Tag = condition });
                return;
            }

            lbEvents.AddItem(new XNAListBoxItem() { Text = condition.ConditionIndex + " " + triggerEventType.Name, Tag = condition });
        }

        private TriggerEventType GetTriggerEventType(int index)
        {
            return map.EditorConfig.TriggerEventTypes.GetValueOrDefault(index);
        }

        private TriggerActionType GetTriggerActionType(int index)
        {
            return map.EditorConfig.TriggerActionTypes.GetValueOrDefault(index);
        }

        private Color GetParamValueColor(string paramValue, TriggerParamType paramType)
        {
            bool intParseSuccess = int.TryParse(paramValue, NumberStyles.None, CultureInfo.InvariantCulture, out int intValue);

            switch (paramType)
            {
                case TriggerParamType.HouseType:
                    if (intParseSuccess)
                    {
                        var houseType = map.FindHouseType(intValue);
                        if (houseType == null)
                            goto case TriggerParamType.Unused;

                        return Helpers.GetHouseTypeUITextColor(houseType);
                    }
                    goto case TriggerParamType.Unused;
                case TriggerParamType.House:
                    if (intParseSuccess)
                    {
                        var houses = map.GetHouses();
                        if (intValue >= houses.Count)
                            goto case TriggerParamType.Unused;

                        return houses[intValue].XNAColor;
                    }
                    goto case TriggerParamType.Unused;
                case TriggerParamType.TeamType:
                    var teamType = map.TeamTypes.Find(tt => tt.ININame == paramValue);
                    if (teamType != null)
                    {
                        return Helpers.GetHouseTypeUITextColor(teamType.HouseType);
                    }
                    goto case TriggerParamType.Unused;
                case TriggerParamType.Trigger:
                    Trigger trigger = map.Triggers.Find(t => t.ID == paramValue);
                    if (trigger == null || trigger.EditorColor == null)
                        goto case TriggerParamType.Unused;

                    return trigger.XNAColor;
                case TriggerParamType.Color:
                    var color = map.Rules.Colors.Find(color => color.Index == intValue);
                    if (color == null)
                        goto case TriggerParamType.Unused;

                    return color.XNAColor;
                case TriggerParamType.Unused:
                default:
                    return UISettings.ActiveSettings.AltColor;
            }
        }

        private string GetParamValueText(string paramValue, TriggerParamType paramType, List<string> presetOptions)
        {
            bool intParseSuccess = int.TryParse(paramValue, NumberStyles.None, CultureInfo.InvariantCulture, out int intValue);
            if (paramValue == null)
                paramValue = string.Empty;

            if (presetOptions != null && presetOptions.Count > 0)
            {
                if (!intParseSuccess)
                    return paramValue;

                string presetOption = presetOptions.Find(s => s.StartsWith(intValue.ToString(CultureInfo.InvariantCulture)));
                if (presetOption != null)
                    return presetOption;
            }

            switch (paramType)
            {
                case TriggerParamType.Animation:
                    if (!intParseSuccess)
                        return paramValue;

                    if (intValue >= map.Rules.AnimTypes.Count)
                        return intValue + Translate(this, "UnknownAnimation", " - nonexistent animation");

                    return intValue + " " + map.Rules.AnimTypes[intValue].ININame;
                case TriggerParamType.HouseType:
                    if (intParseSuccess)
                    {
                        var houseType = map.FindHouseType(intValue);
                        if (houseType == null)
                            return intValue.ToString() + Translate(this, "UnknownHouseType", " - Unknown HouseType");

                        return intValue + " " + houseType.ININame;
                    }

                    return paramValue;
                case TriggerParamType.House:
                    if (intParseSuccess)
                    {
                        var houses = map.GetHouses();
                        if (intValue >= houses.Count)
                            return intValue.ToString() + Translate(this, "UnknownHouse", " - Unknown House");

                        return intValue + " " + houses[intValue].ININame;
                    }

                    return paramValue;
                case TriggerParamType.GlobalVariable:
                    if (!intParseSuccess)
                        return paramValue;

                    if (!map.Rules.GlobalVariables.Exists(v => v.Index == intValue))
                        return intValue + Translate(this, "NonexistentGlobalVariable", " - nonexistent variable");

                    return intValue + " " + map.Rules.GlobalVariables.Find(v => v.Index == intValue).Name;
                case TriggerParamType.LocalVariable:
                    if (!intParseSuccess)
                        return paramValue;

                    if (!map.LocalVariables.Exists(v => v.Index == intValue))
                        return intValue + Translate(this, "NonexistentLocalVariable", " - nonexistent variable");

                    return intValue + " " + map.LocalVariables.Find(v => v.Index == intValue).Name;
                case TriggerParamType.WaypointZZ:
                    if (!intParseSuccess)
                        return Helpers.GetWaypointNumberFromAlphabeticalString(paramValue).ToString();

                    return intValue.ToString();
                case TriggerParamType.TeamType:
                    TeamType teamType = map.TeamTypes.Find(t => t.ININame == paramValue);
                    if (teamType == null)
                        return paramValue;

                    return paramValue + " " + teamType.Name;
                case TriggerParamType.Trigger:
                    Trigger trigger = map.Triggers.Find(t => t.ID == paramValue);
                    if (trigger == null)
                        return paramValue;

                    return paramValue + " " + trigger.Name;
                case TriggerParamType.Building:
                    return GetObjectValueText(RTTIType.Building, map.Rules.BuildingTypes, paramValue);
                case TriggerParamType.BuildingName:
                    BuildingType buildingType = map.Rules.BuildingTypes.Find(bt => bt.ININame == paramValue);
                    if (buildingType == null)
                        return paramValue;

                    return paramValue + " (" + buildingType.GetEditorDisplayName() + ")";
                case TriggerParamType.Aircraft:
                    return GetObjectValueText(RTTIType.Aircraft, map.Rules.AircraftTypes, paramValue);
                case TriggerParamType.Infantry:
                    return GetObjectValueText(RTTIType.Infantry, map.Rules.InfantryTypes, paramValue);
                case TriggerParamType.Unit:
                    return GetObjectValueText(RTTIType.Unit, map.Rules.UnitTypes, paramValue);
                case TriggerParamType.Text:
                    if (!intParseSuccess)
                        return paramValue + Translate(this, "UnknownTextLine", " - Unknown text line");

                    return paramValue + " " + map.Rules.TutorialLines.GetStringByIdOrEmptyString(intValue);
                case TriggerParamType.Theme:
                    if (!intParseSuccess)
                        return paramValue;

                    Theme theme = map.Rules.Themes.Get(intValue);
                    if (theme == null)
                        return paramValue + Translate(this, "NonexistentTheme", " - nonexistent theme");

                    return theme.ToString();
                case TriggerParamType.Tag:
                    Tag tag = map.Tags.Find(t => t.ID == paramValue);

                    if (tag == null)
                        return paramValue + Translate(this, "NonexistentTag", " - nonexistent tag");

                    return paramValue + " " + tag.Name;
                case TriggerParamType.SuperWeapon:
                    if (!intParseSuccess)
                        return paramValue;

                    if (intValue >= map.Rules.SuperWeaponTypes.Count)
                        return intValue + Translate(this, "NonexistentSuperWeapon", " - nonexistent super weapon");

                    return intValue + " " + map.Rules.SuperWeaponTypes[intValue].GetDisplayStringWithoutIndex();
                case TriggerParamType.SuperWeaponName:
                    var swType = map.Rules.SuperWeaponTypes.Find(sw => sw.ININame.Equals(paramValue, StringComparison.Ordinal));

                    if (swType == null)
                        return paramValue;

                    return swType.GetDisplayStringWithoutIndex();
                case TriggerParamType.ParticleSystem:
                    if (!intParseSuccess)
                        return paramValue;

                    if (intValue >= map.Rules.ParticleSystemTypes.Count)
                        return intValue + Translate(this, "NonexistentParticleSystem", " - nonexistent particle system");

                    return intValue + " " + map.Rules.ParticleSystemTypes[intValue].ININame;
                case TriggerParamType.Speech:
                    EvaSpeech speech;

                    if (Constants.IsRA2YR)
                    {
                        speech = map.Rules.Speeches.Get(paramValue);

                        if (speech == null)
                            return paramValue + Translate(this, "UnknownSpeech", " - unknown speech");

                        return speech.Name;
                    }
                    else
                    {
                        if (!intParseSuccess)
                            return paramValue;

                        speech = map.EditorConfig.Speeches.Get(intValue);

                        if (speech == null)
                            return intValue + Translate(this, "UnknownSpeech", " - unknown speech");

                        return $"{intValue} {speech.Name}";
                    }
                case TriggerParamType.Sound:
                    Sound sound;

                    if (Constants.IsRA2YR)
                    {
                        sound = map.Rules.Sounds.Get(paramValue);

                        if (sound == null)
                            return paramValue + Translate(this, "UnknownSound", " - unknown sound");

                        return sound.Name;
                    }
                    else
                    {
                        if (!intParseSuccess)
                            return paramValue;

                        sound = map.Rules.Sounds.Get(intValue);

                        if (sound == null)
                            return intValue + Translate(this, "UnknownSound", " - unknown sound");

                        return $"{intValue} {sound.Name}";
                    }
                case TriggerParamType.Float:
                    if (!intParseSuccess)
                        return paramValue;

                    float floatValue = BitConverter.ToSingle(BitConverter.GetBytes(intValue));
                    return floatValue.ToString(CultureInfo.InvariantCulture) + " (" + paramValue + ")";
                case TriggerParamType.Color:
                    if (!intParseSuccess)
                        return paramValue;

                    if (!map.Rules.Colors.Exists(color => color.Index == intValue))
                        return intValue + Translate(this, "UnknownColor", " - nonexistent color");

                    return intValue + " " + map.Rules.Colors.Find(v => v.Index == intValue).Name;
                case TriggerParamType.Boolean:
                default:
                    return paramValue;
            }
        }

        private string GetObjectValueText<T>(RTTIType rtti, List<T> objectTypeList, string paramValue) where T : TechnoType
        {
            bool intParseSuccess = int.TryParse(paramValue, NumberStyles.None, CultureInfo.InvariantCulture, out int intValue);

            if (!intParseSuccess)
                return paramValue;

            if (intValue >= objectTypeList.Count)
            {
                switch (rtti)
                {
                    case RTTIType.Aircraft:
                        return intValue + Translate(this, "UnknownAircraft", " - Unknown Aircraft");
                    case RTTIType.Building:
                        return intValue + Translate(this, "UnknownBuilding", " - Unknown Building");
                    case RTTIType.Infantry:
                        return intValue + Translate(this, "UnknownInfantry", " - Unknown Infantry");
                    case RTTIType.Unit:
                        return intValue + Translate(this, "UnknownUnit", " - Unknown Unit");
                    default:
                        return intValue + Translate(this, "UnknownObject", " - Unknown Object");
                }
            }

            return intValue + " " + objectTypeList[intValue].GetEditorDisplayName();
        }

        private void OpenTeamType(TeamType teamType)
        {
            TeamTypeOpened?.Invoke(this, new TeamTypeEventArgs(teamType));
            PutOnBackground();
        }

        private void OpenCreateRandomTriggersSetWindow()
        {
            createRandomTriggerSetWindow.Open();
            PutOnBackground();
        }

        private void Map_TriggersChanged(object sender, EventArgs e)
        {
            if (Visible)
            {
                ListTriggers();
                SelectTrigger(editedTrigger);
            }
        }
    }
}
