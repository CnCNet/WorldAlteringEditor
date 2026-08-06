using System;
using MapEditorLibrary.Models;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace TSMapEditor.UI.Windows;

public class SelectTriggerWindow : SelectObjectWindow<Trigger>
{
    public SelectTriggerWindow(WindowManager windowManager, Map map) : base(windowManager)
    {
        this.map = map;
    }

    private readonly Map map;

    public override void Initialize()
    {
        Name = nameof(SelectTriggerWindow);
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
        if (e.Affects<Trigger>())
            QueueScriptingRefresh(trigger => map.Triggers.Contains(trigger));
    }

    protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (lbObjectList.SelectedItem == null)
        {
            SelectedObject = null;
            return;
        }

        SelectedObject = (Trigger)lbObjectList.SelectedItem.Tag;
    }

    protected override void ListObjects()
    {
        lbObjectList.Clear();

        lbObjectList.AddItem(Translate(this, "None", "None"));

        foreach (Trigger trigger in map.Triggers)
        {
            lbObjectList.AddItem(new XNAListBoxItem() 
            { 
                Text = $"{trigger.ID} {trigger.Name}",
                Tag = trigger, 
                TextColor = trigger.EditorColor == null ? lbObjectList.DefaultItemColor : trigger.XNAColor
            });
            
            if (trigger == SelectedObject)
                lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
        }
    }
}
