using System;
using MapEditorLibrary.Models;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace TSMapEditor.UI.Windows;

public class SelectLocalVariableWindow : SelectObjectWindow<LocalVariable>
{
    public SelectLocalVariableWindow(WindowManager windowManager, Map map) : base(windowManager)
    {
        this.map = map;
    }

    private readonly Map map;

    public override void Initialize()
    {
        Name = nameof(SelectLocalVariableWindow);
        base.Initialize();

        map.ScriptingChanged += Map_ScriptingChanged;
    }

    public override void Kill()
    {
        map.ScriptingChanged -= Map_ScriptingChanged;
        base.Kill();
    }

    private void Map_ScriptingChanged(object sender, ScriptingChangedEventArgs e)
    {
        if (e.Affects<LocalVariable>())
            QueueScriptingRefresh(localVariable => map.LocalVariables.Contains(localVariable));
    }

    protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (lbObjectList.SelectedItem == null)
        {
            SelectedObject = null;
            return;
        }

        SelectedObject = (LocalVariable)lbObjectList.SelectedItem.Tag;
    }

    protected override void ListObjects()
    {
        lbObjectList.Clear();

        foreach (LocalVariable localVariable in map.LocalVariables)
        {
            lbObjectList.AddItem(new XNAListBoxItem() { Text = $"{localVariable.Index} {localVariable.Name}", Tag = localVariable });
            if (localVariable == SelectedObject)
                lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
        }
    }
}
