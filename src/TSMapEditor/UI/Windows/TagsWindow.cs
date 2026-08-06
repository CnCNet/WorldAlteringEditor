using MapEditorLibrary;
using MapEditorLibrary.Models;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Linq;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows;

public class TagsWindow : INItializableWindow
{
    public TagsWindow(WindowManager windowManager, Map map) : base(windowManager)
    {
        this.map = map;
    }

    private readonly Map map;

    private EditorListBox lbTags;
    private EditorTextBox tbName;
    private EditorPopUpSelector selTrigger;
    private XNADropDown ddPersistence;

    private Tag editedTag;
    private SelectTriggerWindow selectTriggerWindow;

    public override void Initialize()
    {
        Name = nameof(TagsWindow);
        base.Initialize();

        lbTags = FindChild<EditorListBox>(nameof(lbTags));
        tbName = FindChild<EditorTextBox>(nameof(tbName));
        selTrigger = FindChild<EditorPopUpSelector>(nameof(selTrigger));
        ddPersistence = FindChild<XNADropDown>(nameof(ddPersistence));

        lbTags.SelectedIndexChanged += LbTags_SelectedIndexChanged;
        tbName.TextChanged += TbName_TextChanged;
        ddPersistence.SelectedIndexChanged += DdRepeating_SelectedIndexChanged;

        FindChild<EditorButton>("btnNewTag").LeftClick += BtnNewTag_LeftClick;
        FindChild<EditorButton>("btnCloneTag").LeftClick += BtnCloneTag_LeftClick;
        FindChild<EditorButton>("btnDeleteTag").LeftClick += BtnDeleteTag_LeftClick;

        ddPersistence.AddItem(Translate(this, "Type0", "0 - one-time, single-object condition"));
        ddPersistence.AddItem(Translate(this, "Type1", "1 - one-time, multi-object condition"));
        ddPersistence.AddItem(Translate(this, "Type2", "2 - repeating, single-object condition"));

        selectTriggerWindow = new SelectTriggerWindow(WindowManager, map);
        var triggerDarkeningPanel = DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowManager, Parent, selectTriggerWindow);
        triggerDarkeningPanel.Hidden += TriggerDarkeningPanel_Hidden;

        selTrigger.LeftClick += SelTrigger_LeftClick;
        map.ScriptingChanged += Map_ScriptingChanged;
    }

    private void SelTrigger_LeftClick(object sender, EventArgs e)
    {
        if (editedTag == null)
            return;

        selectTriggerWindow.Open(editedTag.Trigger);
    }

    private void TriggerDarkeningPanel_Hidden(object sender, EventArgs e)
    {
        if (editedTag == null)
            return;

        if (selectTriggerWindow.SelectedObject == null)
            return;

        editedTag.Trigger = selectTriggerWindow.SelectedObject;
        EditTag(editedTag);
    }

    private void BtnNewTag_LeftClick(object sender, EventArgs e)
    {
        var tag = new Tag()
        {
            ID = map.GetNewUniqueInternalId(),
            Name = Translate(this, "NewTag", "New Tag"),
            Repeating = 0,
            Trigger = map.Triggers.FirstOrDefault(trig => !map.Tags.Exists(tag => tag.Trigger == trig))
        };

        map.AddTag(tag);
        ListTags();
        SelectTag(tag);
        WindowManager.SelectedControl = tbName;
        tbName.SetSelection(0, tbName.Text.Length);
    }

    private void BtnCloneTag_LeftClick(object sender, EventArgs e)
    {
        if (editedTag == null)
            return;

        var clone = new Tag()
        {
            ID = map.GetNewUniqueInternalId(),
            Name = Helpers.GetNameForClone(editedTag.Name),
            Repeating = editedTag.Repeating,
            Trigger = editedTag.Trigger
        };

        map.AddTag(clone);
        ListTags();
        SelectTag(clone);
    }

    private void BtnDeleteTag_LeftClick(object sender, EventArgs e)
    {
        if (editedTag == null)
            return;

        map.Tags.Remove(editedTag);
        editedTag = null;

        ListTags();
        RefreshSelectedTag();
    }

    private void TbName_TextChanged(object sender, EventArgs e)
    {
        if (editedTag == null)
            return;

        editedTag.Name = tbName.Text;

        if (lbTags.SelectedItem != null)
            lbTags.SelectedItem.Text = editedTag.GetDisplayString();
    }

    private void DdRepeating_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (editedTag == null || ddPersistence.SelectedItem == null)
            return;

        editedTag.Repeating = ddPersistence.SelectedIndex;
    }

    private void LbTags_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (lbTags.SelectedItem == null)
        {
            editedTag = null;
            RefreshSelectedTag();
            return;
        }

        EditTag((Tag)lbTags.SelectedItem.Tag);
    }

    private void EditTag(Tag tag)
    {
        editedTag = tag;
        RefreshSelectedTag();
    }

    private void RefreshSelectedTag()
    {
        if (editedTag == null)
        {
            tbName.Text = string.Empty;
            selTrigger.Text = string.Empty;
            selTrigger.Tag = null;
            ddPersistence.SelectedIndex = -1;
            return;
        }

        tbName.Text = editedTag.Name;

        selTrigger.Tag = editedTag.Trigger;
        selTrigger.Text = editedTag.Trigger == null ? Constants.NoneValue1 : $"{editedTag.Trigger.ID} {editedTag.Trigger.Name}";

        ddPersistence.SelectedIndex = Math.Max(0, Math.Min(MapEditorLibrary.Models.Tag.REPEAT_TYPE_MAX, editedTag.Repeating));
    }

    private void Map_ScriptingChanged(object sender, ScriptingChangedEventArgs e)
    {
        if (Visible && e.AffectsAny(typeof(Tag), typeof(Trigger)))
            AddCallback(RefreshScriptingData);
    }

    private void RefreshScriptingData()
    {
        var tag = editedTag;
        if (tag != null && !map.Tags.Contains(tag))
            tag = null;

        ListTags();
        SelectTag(tag);

        if (lbTags.SelectedItem == null)
            EditTag(null);
    }

    public void Open()
    {
        Show();
        ListTags();
        RefreshSelectedTag();
    }

    public void SelectTag(Tag tag)
    {
        lbTags.SelectedIndex = lbTags.Items.FindIndex(item => item.Tag == tag);
        if (lbTags.SelectedItem != null)
            lbTags.ScrollToSelectedElement();
    }

    private void ListTags()
    {
        lbTags.Clear();

        foreach (var tag in map.Tags)
        {
            Color color = lbTags.DefaultItemColor;
            if (tag.Trigger != null && !string.IsNullOrWhiteSpace(tag.Trigger.EditorColor))
                color = tag.Trigger.XNAColor;

            lbTags.AddItem(new XNAListBoxItem()
            {
                Text = tag.GetDisplayString(),
                Tag = tag,
                TextColor = color
            });
        }

        if (editedTag != null)
            lbTags.SelectedIndex = lbTags.Items.FindIndex(item => item.Tag == editedTag);
    }
}
