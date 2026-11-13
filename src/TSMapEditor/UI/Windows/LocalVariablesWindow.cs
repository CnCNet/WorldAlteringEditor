using MapEditorLibrary;
using MapEditorLibrary.Models;
using MapEditorLibrary.Models.Enums;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows;

public class LocalVariablesWindow : INItializableWindow
{
    public LocalVariablesWindow(WindowManager windowManager, Map map, ReferenceListWindow referenceListWindow) : base(windowManager)
    {
        this.map = map;
        this.referenceListWindow = referenceListWindow;
    }

    private readonly Map map;
    private readonly ReferenceListWindow referenceListWindow;

    private EditorListBox lbLocalVariables;
    private EditorTextBox tbName;
    private XNACheckBox chkInitialState;
    private XNALabel lblInitialState;
    private EditorNumberTextBox tbInitialState;

    private LocalVariable editedLocalVariable;

    public override void Initialize()
    {
        Name = nameof(LocalVariablesWindow);
        base.Initialize();

        lbLocalVariables = FindChild<EditorListBox>(nameof(lbLocalVariables));
        tbName = FindChild<EditorTextBox>(nameof(tbName));
        chkInitialState = FindChild<XNACheckBox>(nameof(chkInitialState));
        lblInitialState = FindChild<XNALabel>(nameof(lblInitialState));
        tbInitialState = FindChild<EditorNumberTextBox>(nameof(tbInitialState));

        if (Constants.IntegerVariables)
        {
            chkInitialState.Disable();
        }
        else
        {
            tbInitialState.Disable();
            lblInitialState.Disable();
        }

        FindChild<EditorButton>("btnNewLocalVariable").LeftClick += BtnNewLocalVariable_LeftClick;
        FindChild<EditorButton>("btnDeleteLocalVariable").LeftClick += BtnDeleteLocalVariable_LeftClick;
        FindChild<EditorButton>("btnViewVariableUsages").LeftClick += BtnViewVariableUsages_LeftClick;

        lbLocalVariables.SelectedIndexChanged += LbLocalVariables_SelectedIndexChanged;
        map.ScriptingChanged += Map_ScriptingChanged;
    }

    private void BtnNewLocalVariable_LeftClick(object sender, EventArgs e)
    {
        int newIndex = 0;
        while (map.LocalVariables.Exists(v => v.Index == newIndex))
        {
            newIndex++;
        }

        map.LocalVariables.Insert(newIndex, new LocalVariable(newIndex) { Name = Translate(this, "NewLocalVariable", "New Local Variable") });
        ListLocalVariables();
        lbLocalVariables.SelectedIndex = newIndex;
        WindowManager.SelectedControl = tbName;
        tbName.SetSelection(0, tbName.Text.Length);
    }

    private void BtnDeleteLocalVariable_LeftClick(object sender, EventArgs e)
    {
        if (lbLocalVariables.SelectedItem == null)
            return;

        map.LocalVariables.Remove(lbLocalVariables.SelectedItem.Tag as LocalVariable);
        ListLocalVariables();
        lbLocalVariables.SelectedIndex--;
    }

    private void BtnViewVariableUsages_LeftClick(object sender, EventArgs e)
    {
        if (editedLocalVariable == null)
        {
            EditorMessageBox.Show(WindowManager,
                Translate(this, "SelectVariableError.Title", "Select a variable"),
                Translate(this, "SelectVariableError.Description", "Please select a variable first."),
                MessageBoxButtons.OK);

            return;
        }

        var references = GetReferences(editedLocalVariable).ToList();
        if (references.Count == 0)
        {
            EditorMessageBox.Show(WindowManager,
                Translate(this, "NoUsagesFound.Title", "No usages found"),
                string.Format(Translate(this, "NoUsagesFound.Description",
                    "No triggers or scripts make use of the selected local variable '{0}'"), editedLocalVariable.Name),
                MessageBoxButtons.OK);

            return;
        }

        referenceListWindow.Open(Translate("ReferenceListWindow.SourceType.LocalVariable", "Local Variable"), editedLocalVariable.Name, references);
    }

    private IEnumerable<(string Text, object Target)> GetReferences(LocalVariable localVariable)
    {
        string actionFormat = Translate("ReferenceListWindow.TriggerAction", "Trigger \"{0}\" ({1}), action #{2} \"{3}\"");
        string eventFormat = Translate("ReferenceListWindow.TriggerEvent", "Trigger \"{0}\" ({1}), event #{2} \"{3}\"");

        foreach (var trigger in map.Triggers)
        {
            for (int actionIndex = 0; actionIndex < trigger.Actions.Count; actionIndex++)
            {
                var action = trigger.Actions[actionIndex];
                if (!map.EditorConfig.TriggerActionTypes.TryGetValue(action.ActionIndex, out var actionType))
                    continue;

                int parameterCount = Math.Min(action.Parameters.Length, actionType.Parameters.Length);
                for (int i = 0; i < parameterCount; i++)
                {
                    if (actionType.Parameters[i].TriggerParamType != TriggerParamType.LocalVariable ||
                        Conversions.IntFromString(action.Parameters[i], -1) != localVariable.Index)
                    {
                        continue;
                    }

                    yield return (string.Format(actionFormat, trigger.Name, trigger.ID, actionIndex + 1, actionType.Name), trigger);
                    break;
                }
            }

            for (int eventIndex = 0; eventIndex < trigger.Conditions.Count; eventIndex++)
            {
                var condition = trigger.Conditions[eventIndex];
                if (!map.EditorConfig.TriggerEventTypes.TryGetValue(condition.ConditionIndex, out var eventType))
                    continue;

                int parameterCount = Math.Min(condition.Parameters.Length, eventType.Parameters.Length);
                for (int i = 0; i < parameterCount; i++)
                {
                    if (eventType.Parameters[i].TriggerParamType != TriggerParamType.LocalVariable ||
                        Conversions.IntFromString(condition.Parameters[i], -1) != localVariable.Index)
                    {
                        continue;
                    }

                    yield return (string.Format(eventFormat, trigger.Name, trigger.ID, eventIndex + 1, eventType.Name), trigger);
                    break;
                }
            }
        }

        string scriptActionFormat = Translate("ReferenceListWindow.ScriptAction", "Script \"{0}\" ({1}), action #{2} \"{3}\"");
        foreach (var script in map.Scripts)
        {
            for (int actionIndex = 0; actionIndex < script.Actions.Count; actionIndex++)
            {
                var action = script.Actions[actionIndex];
                if (!map.EditorConfig.ScriptActions.TryGetValue(action.Action, out var actionType))
                    continue;

                if (actionType.ParamType == TriggerParamType.LocalVariable && action.Argument == localVariable.Index)
                    yield return (string.Format(scriptActionFormat, script.Name, script.ININame, actionIndex + 1, actionType.Name), script);
            }
        }
    }

    private void LbLocalVariables_SelectedIndexChanged(object sender, EventArgs e)
    {
        tbName.TextChanged -= TbName_TextChanged;
        chkInitialState.CheckedChanged -= ChkInitialState_CheckedChanged;
        tbInitialState.TextChanged -= TbInitialState_TextChanged;

        if (lbLocalVariables.SelectedItem == null)
        {
            editedLocalVariable = null;
            tbName.Text = string.Empty;
            return;
        }

        editedLocalVariable = (LocalVariable)lbLocalVariables.SelectedItem.Tag;
        tbName.Text = editedLocalVariable.Name;
        chkInitialState.Checked = editedLocalVariable.InitialState > 0;
        tbInitialState.Value = editedLocalVariable.InitialState;

        tbName.TextChanged += TbName_TextChanged;
        chkInitialState.CheckedChanged += ChkInitialState_CheckedChanged;
        tbInitialState.TextChanged += TbInitialState_TextChanged;
    }

    private void ChkInitialState_CheckedChanged(object sender, EventArgs e)
    {
        editedLocalVariable.InitialState = chkInitialState.Checked ? 1 : 0;
    }

    private void TbInitialState_TextChanged(object sender, EventArgs e)
    {
        editedLocalVariable.InitialState = tbInitialState.Value;
    }

    private void TbName_TextChanged(object sender, EventArgs e)
    {
        editedLocalVariable.Name = tbName.Text;
        ListLocalVariables();
    }

    private void Map_ScriptingChanged(object sender, ScriptingChangedEventArgs e)
    {
        if (Visible && e.Affects<LocalVariable>())
            AddCallback(RefreshScriptingData);
    }

    private void RefreshScriptingData()
    {
        var localVariable = editedLocalVariable;
        if (localVariable != null && !map.LocalVariables.Contains(localVariable))
            localVariable = null;

        ListLocalVariables();
        SelectLocalVariable(localVariable);
    }

    public void SelectLocalVariable(LocalVariable localVariable)
    {
        int index = lbLocalVariables.Items.FindIndex(item => item.Tag == localVariable);
        if (index == lbLocalVariables.SelectedIndex)
            LbLocalVariables_SelectedIndexChanged(this, EventArgs.Empty);
        else
            lbLocalVariables.SelectedIndex = index;

        if (index >= 0)
            lbLocalVariables.ScrollToSelectedElement();
    }

    public void Open()
    {
        Show();
        ListLocalVariables();
    }

    private void ListLocalVariables()
    {
        lbLocalVariables.Clear();

        foreach (var localVariable in map.LocalVariables)
        {
            lbLocalVariables.AddItem(new XNAListBoxItem() { Text = localVariable.Index + " " + localVariable.Name, Tag = localVariable });
        }
    }
}
