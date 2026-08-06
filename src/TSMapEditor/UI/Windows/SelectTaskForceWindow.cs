using System;
using System.Collections.Generic;
using System.Linq;
using MapEditorLibrary.Models;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace TSMapEditor.UI.Windows;

/// <summary>
/// A window that allows the user to select a TaskForce (for example, for a TeamType).
/// </summary>
public class SelectTaskForceWindow : SelectObjectWindow<TaskForce>
{
    public SelectTaskForceWindow(WindowManager windowManager, Map map) : base(windowManager)
    {
        this.map = map;
    }

    private readonly Map map;

    private XNACheckBox chkIncludeGlobalTaskForces;

    public override void Initialize()
    {
        Name = nameof(SelectTaskForceWindow);
        base.Initialize();

        chkIncludeGlobalTaskForces = FindChild<XNACheckBox>(nameof(chkIncludeGlobalTaskForces));
        chkIncludeGlobalTaskForces.CheckedChanged += (_, _) => ListObjects();

        map.ScriptingChanged += Map_ScriptingChanged;
    }

    public override void Kill()
    {
        map.ScriptingChanged -= Map_ScriptingChanged;
        base.Kill();
    }

    private void Map_ScriptingChanged(object sender, ScriptingChangedEventArgs e)
    {
        if (e.Affects<TaskForce>())
        {
            QueueScriptingRefresh(taskForce => map.TaskForces.Contains(taskForce) ||
                (chkIncludeGlobalTaskForces.Checked && map.Rules.TaskForces.Contains(taskForce)));
        }
    }

    protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (lbObjectList.SelectedItem == null)
        {
            SelectedObject = null;
            return;
        }

        SelectedObject = (TaskForce)lbObjectList.SelectedItem.Tag;
    }

    protected override void ListObjects()
    {
        lbObjectList.Clear();

        IEnumerable<TaskForce> list = map.TaskForces;
        if (chkIncludeGlobalTaskForces.Checked)
            list = map.TaskForces.UnionBy(map.Rules.TaskForces, tf => tf.ININame);

        foreach (TaskForce taskForce in list)
        {
            lbObjectList.AddItem(new XNAListBoxItem() { Text = $"{taskForce.Name} ({taskForce.ININame})", Tag = taskForce });

            if (taskForce == SelectedObject)
                lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
        }
    }
}
