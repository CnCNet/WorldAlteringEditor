using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class LocalVariablesWindow : INItializableWindow
    {
        public LocalVariablesWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

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

            // 汉化按钮和标签
            var btnNew = FindChild<EditorButton>("btnNewLocalVariable");
            if (btnNew != null)
                btnNew.Text = TSMapEditor.UI.MainMenu.IsChinese ? "新建变量" : "New Local Variable";
            var btnDelete = FindChild<EditorButton>("btnDeleteLocalVariable");
            if (btnDelete != null)
                btnDelete.Text = TSMapEditor.UI.MainMenu.IsChinese ? "删除变量" : "Delete Local Variable";
            var btnView = FindChild<EditorButton>("btnViewVariableUsages");
            if (btnView != null)
                btnView.Text = TSMapEditor.UI.MainMenu.IsChinese ? "查看引用" : "View Variable Usages";
            var lblName = FindChild<XNALabel>("lblName");
            if (lblName != null)
                lblName.Text = TSMapEditor.UI.MainMenu.IsChinese ? "名称：" : "Name:";
            var lblVars = FindChild<XNALabel>("lblLocalVariables");
            if (lblVars != null)
                lblVars.Text = TSMapEditor.UI.MainMenu.IsChinese ? "局部变量：" : "Local Variables:";
            if (chkInitialState != null)
                chkInitialState.Text = TSMapEditor.UI.MainMenu.IsChinese ? "开/真/1（开局时）" : "'Set/on/true' on scenario start";

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
        }

        public void RefreshLanguage()
        {
            var btnNew = FindChild<EditorButton>("btnNewLocalVariable");
            if (btnNew != null)
                btnNew.Text = TSMapEditor.UI.MainMenu.IsChinese ? "新建变量" : "New Local Variable";
            var btnDelete = FindChild<EditorButton>("btnDeleteLocalVariable");
            if (btnDelete != null)
                btnDelete.Text = TSMapEditor.UI.MainMenu.IsChinese ? "删除变量" : "Delete Local Variable";
            var btnView = FindChild<EditorButton>("btnViewVariableUsages");
            if (btnView != null)
                btnView.Text = TSMapEditor.UI.MainMenu.IsChinese ? "查看引用" : "View Variable Usages";
            var lblName = FindChild<XNALabel>("lblName");
            if (lblName != null)
                lblName.Text = TSMapEditor.UI.MainMenu.IsChinese ? "名称：" : "Name:";
            var lblVars = FindChild<XNALabel>("lblLocalVariables");
            if (lblVars != null)
                lblVars.Text = TSMapEditor.UI.MainMenu.IsChinese ? "局部变量：" : "Local Variables:";
            if (chkInitialState != null)
                chkInitialState.Text = TSMapEditor.UI.MainMenu.IsChinese ? "开/真/1（开局时）" : "'Set/on/true' on scenario start";
        }

        private void BtnNewLocalVariable_LeftClick(object sender, EventArgs e)
        {
            int newIndex = 0;
            while (map.LocalVariables.Exists(v => v.Index == newIndex))
            {
                newIndex++;
            }

            map.LocalVariables.Insert(newIndex, new LocalVariable(newIndex) { Name = "New Local Variable" });
            ListLocalVariables();
            lbLocalVariables.SelectedIndex = newIndex;
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
                    TSMapEditor.UI.MainMenu.IsChinese ? "请选择一个变量" : "Select a variable",
                    TSMapEditor.UI.MainMenu.IsChinese ? "请先选择一个变量。" : "Please select a variable first.",
                    MessageBoxButtons.OK);
                return;
            }

            var list = new List<string>();

            map.Triggers.ForEach(trigger =>
            {
                foreach (var action in trigger.Actions)
                {
                    var actionType = map.EditorConfig.TriggerActionTypes.GetValueOrDefault(action.ActionIndex);
                    if (actionType == null)
                        continue;

                    for (int i = 0; i < actionType.Parameters.Length; i++)
                    {
                        var parameter = actionType.Parameters[i];
                        if (parameter.TriggerParamType == TriggerParamType.LocalVariable)
                        {
                            if (Conversions.IntFromString(action.Parameters[i], -1) == editedLocalVariable.Index)
                            {
                                list.Add($"Trigger action of '{trigger.Name}' ({trigger.ID})");
                                break;
                            }
                        }
                    }
                }

                foreach (var triggerEvent in trigger.Conditions)
                {
                    var eventType = map.EditorConfig.TriggerEventTypes.GetValueOrDefault(triggerEvent.ConditionIndex);
                    if (eventType == null)
                        continue;

                    for (int i = 0; i < eventType.Parameters.Length; i++)
                    {
                        var parameter = eventType.Parameters[i];
                        if (parameter.TriggerParamType == TriggerParamType.LocalVariable)
                        {
                            if (Conversions.IntFromString(triggerEvent.Parameters[i], -1) == editedLocalVariable.Index)
                            {
                                list.Add($"Trigger event of '{trigger.Name}' ({trigger.ID})");
                                break;
                            }
                        }
                    }
                }
            });

            map.Scripts.ForEach(script =>
            {
                foreach (var scriptAction in script.Actions)
                {
                    var scriptActionType = map.EditorConfig.ScriptActions.GetValueOrDefault(scriptAction.Action);

                    if (scriptActionType == null)
                        continue;

                    if (scriptActionType.ParamType == TriggerParamType.LocalVariable &&
                        scriptAction.Argument == editedLocalVariable.Index)
                    {
                        list.Add($"Script action of '{script.Name}' ({script.ININame})");
                    }
                }
            });

            if (list.Count == 0)
            {
                EditorMessageBox.Show(WindowManager,
                    TSMapEditor.UI.MainMenu.IsChinese ? "未找到引用" : "No usages found",
                    TSMapEditor.UI.MainMenu.IsChinese
                        ? $"没有触发器或脚本引用所选局部变量 '{editedLocalVariable.Name}'"
                        : $"No triggers or scripts make use of the selected local variable '{editedLocalVariable.Name}'",
                    MessageBoxButtons.OK);
            }
            else
            {
                EditorMessageBox.Show(WindowManager,
                    TSMapEditor.UI.MainMenu.IsChinese ? "局部变量引用" : "Local Variable Usages",
                    (TSMapEditor.UI.MainMenu.IsChinese
                        ? $"下列内容引用了所选局部变量 '{editedLocalVariable.Name}'："
                        : $"The following usages were found for the selected local variable '{editedLocalVariable.Name}':")
                    + Environment.NewLine + Environment.NewLine +
                    string.Join(Environment.NewLine, list.Select(e => "- " + e)),
                    MessageBoxButtons.OK);
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
}
