using MapEditorLibrary.Models;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TSMapEditor.UI.Windows;

public class SelectScriptWindow : SelectObjectWindow<Script>
{
    public SelectScriptWindow(WindowManager windowManager, Map map) : base(windowManager)
    {
        this.map = map;
    }

    private readonly Map map;

    private XNACheckBox chkIncludeGlobalScripts;

    public override void Initialize()
    {
        Name = nameof(SelectScriptWindow);
        base.Initialize();

        chkIncludeGlobalScripts = FindChild<XNACheckBox>(nameof(chkIncludeGlobalScripts));
        chkIncludeGlobalScripts.CheckedChanged += (_, _) => ListObjects();

        map.ScriptingChanged += Map_ScriptingChanged;
    }

    public override void Kill()
    {
        map.ScriptingChanged -= Map_ScriptingChanged;
        base.Kill();
    }

    private void Map_ScriptingChanged(object sender, ScriptingChangedEventArgs e)
    {
        if (e.Affects<Script>())
        {
            QueueScriptingRefresh(script => map.Scripts.Contains(script) ||
                (chkIncludeGlobalScripts.Checked && map.Rules.Scripts.Contains(script)));
        }
    }

    protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (lbObjectList.SelectedItem == null)
        {
            SelectedObject = null;
            return;
        }

        SelectedObject = (Script)lbObjectList.SelectedItem.Tag;
    }

    protected override void ListObjects()
    {
        lbObjectList.Clear();

        IEnumerable<Script> list = map.Scripts;
        if (chkIncludeGlobalScripts.Checked)
            list = map.Scripts.UnionBy(map.Rules.Scripts, script => script.ININame);

        foreach (Script script in list)
        {
            lbObjectList.AddItem(new XNAListBoxItem() 
            {
                Text = $"{script.Name} ({script.ININame})",
                Tag = script,
                TextColor = script.EditorColor == null ? lbObjectList.DefaultItemColor : script.XNAColor
            });

            if (script == SelectedObject)
                lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
        }
    }
}
