using MapEditorLibrary.Models;
using MapEditorLibrary.Models.Enums;
using MapEditorLibrary.Mutations.Classes;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows;

public class ObjectListWindow : INItializableWindow
{
    public ObjectListWindow(WindowManager windowManager, Map map, ICursorActionTarget cursorActionTarget, Action<Trigger> openTrigger) : base(windowManager)
    {
        this.map = map;
        this.cursorActionTarget = cursorActionTarget;
        this.openTrigger = openTrigger;
    }

    private readonly Map map;
    private readonly ICursorActionTarget cursorActionTarget;
    private readonly Action<Trigger> openTrigger;
    private Dictionary<RTTIType, (string Singular, string Plural)> categoryNames;

    private XNADropDown ddCategory;
    private XNADropDown ddOwner;
    private XNAMultiColumnListBox lbObjects;
    private EditorContextMenu objectContextMenu;
    private GameObject contextObject;

    private Trigger ContextTrigger => (contextObject as TechnoBase)?.AttachedTag?.Trigger;

    public override void Initialize()
    {
        Name = nameof(ObjectListWindow);
        base.Initialize();

        ddCategory = FindChild<XNADropDown>(nameof(ddCategory));
        ddOwner = FindChild<XNADropDown>(nameof(ddOwner));
        lbObjects = FindChild<XNAMultiColumnListBox>(nameof(lbObjects));

        categoryNames = new Dictionary<RTTIType, (string Singular, string Plural)>
        {
            { RTTIType.Building, (Translate(this, "Category.Building", "Building"),
                Translate(this, "CategoryFilter.Building", "Buildings")) },
            { RTTIType.Infantry, (Translate(this, "Category.Infantry", "Infantry"),
                Translate(this, "CategoryFilter.Infantry", "Infantry")) },
            { RTTIType.Aircraft, (Translate(this, "Category.Aircraft", "Aircraft"),
                Translate(this, "CategoryFilter.Aircraft", "Aircraft")) },
            { RTTIType.Unit, (Translate(this, "Category.Vehicle", "Vehicle"),
                Translate(this, "CategoryFilter.Vehicle", "Vehicles")) },
            { RTTIType.Terrain, (Translate(this, "Category.TerrainObject", "Terrain Object"),
                Translate(this, "CategoryFilter.TerrainObject", "Terrain Objects")) }
        };

        ddCategory.AddItem(Translate(this, "AllCategories", "All Categories"));
        foreach (var category in categoryNames)
        {
            ddCategory.AddItem(new XNADropDownItem { Text = category.Value.Plural, Tag = category.Key });
        }

        ddCategory.SelectedIndex = 0;
        ddCategory.SelectedIndexChanged += Filter_SelectedIndexChanged;
        ddOwner.SelectedIndexChanged += Filter_SelectedIndexChanged;

        objectContextMenu = new EditorContextMenu(WindowManager) { Name = nameof(objectContextMenu) };
        objectContextMenu.AddItem(Translate(this, "CenterCamera", "Center Camera"), () => CenterCamera(contextObject));
        objectContextMenu.AddItem(Translate(this, "OpenTrigger", "Open Trigger"), OpenContextTrigger, () => ContextTrigger != null);
        objectContextMenu.AddItem(Translate(this, "Delete", "Delete"), DeleteContextObject);
        AddChild(objectContextMenu);

        lbObjects.AllowRightClickUnselect = false;
        lbObjects.LeftClick += LbObjects_LeftClick;
        lbObjects.RightClick += LbObjects_RightClick;
    }

    public void Open()
    {
        RefreshOwners();
        RefreshObjects();
        Show();
    }

    private IEnumerable<TechnoBase> GetTechnos() => map.Structures.Cast<TechnoBase>().Concat(map.Infantry).Concat(map.Aircraft).Concat(map.Units);

    private void RefreshOwners()
    {
        var selectedOwner = ddOwner.SelectedItem?.Tag as House;

        ddOwner.Items.Clear();
        ddOwner.AddItem(Translate(this, "AllOwners", "All Owners"));

        foreach (var house in map.GetHouses().Concat(GetTechnos().Select(techno => techno.Owner)).Distinct())
        {
            ddOwner.AddItem(new XNADropDownItem { Text = house.ININame, TextColor = house.XNAColor, Tag = house });
        }

        ddOwner.SelectedIndexChanged -= Filter_SelectedIndexChanged;
        ddOwner.SelectedIndex = Math.Max(0, ddOwner.Items.FindIndex(item => item.Tag == selectedOwner));
        ddOwner.SelectedIndexChanged += Filter_SelectedIndexChanged;
    }

    private void Filter_SelectedIndexChanged(object sender, EventArgs e) => RefreshObjects();

    private void RefreshObjects(int topIndex = 0)
    {
        lbObjects.SelectedIndex = -1;
        lbObjects.TopIndex = 0;
        lbObjects.ClearItems();

        var category = (RTTIType?)ddCategory.SelectedItem?.Tag;
        var owner = ddOwner.SelectedItem?.Tag as House;

        var objects = GetTechnos().Cast<GameObject>().Concat(map.TerrainObjects)
            .Select(mapObject => (Object: mapObject, Category: mapObject.WhatAmI(), Techno: mapObject as TechnoBase))
            .Where(entry => (category == null || entry.Category == category) && (owner == null || entry.Techno?.Owner == owner))
            .OrderBy(entry => !entry.Object.IsTechno())
            .ThenBy(entry => categoryNames[entry.Category].Singular, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(entry => entry.Techno?.Owner?.ININame, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(entry => entry.Object.GetObjectType().Index);

        foreach (var entry in objects)
        {
            AddObjectRow(entry.Object, categoryNames[entry.Category].Singular, entry.Techno);
        }

        if (topIndex > 0)
        {
            lbObjects.TopIndex = topIndex;
            if (lbObjects.LastIndex == lbObjects.ItemCount - 1)
                lbObjects.ScrollToBottom();
        }
    }

    private void AddObjectRow(GameObject mapObject, string category, TechnoBase techno)
    {
        var objectType = mapObject.GetObjectType();
        var owner = techno?.Owner;

        string mission = mapObject switch
        {
            Infantry infantry => infantry.Mission,
            Aircraft aircraft => aircraft.Mission,
            Unit unit => unit.Mission,
            _ => string.Empty
        };

        lbObjects.AddItem(new XNAListBoxItem[]
        {
            new XNAListBoxItem($"{objectType.GetEditorDisplayName()} ({objectType.ININame})") { Tag = mapObject },
            new XNAListBoxItem(category),
            owner == null ? new XNAListBoxItem(string.Empty) : new XNAListBoxItem(owner.ININame, owner.XNAColor),
            new XNAListBoxItem(mapObject.Position.X.ToString(CultureInfo.InvariantCulture)),
            new XNAListBoxItem(mapObject.Position.Y.ToString(CultureInfo.InvariantCulture)),
            new XNAListBoxItem(mission),
            new XNAListBoxItem(techno?.AttachedTag?.Name ?? string.Empty)
        });
    }

    private void LbObjects_LeftClick(object sender, InputEventArgs e)
    {
        if (lbObjects.IsValidIndexSelected() && lbObjects.SelectedIndex == lbObjects.HoveredIndex)
            CenterCamera((GameObject)lbObjects.GetItem(0, lbObjects.SelectedIndex).Tag);
    }

    private void LbObjects_RightClick(object sender, InputEventArgs e)
    {
        e.Handled = true;

        int row = lbObjects.HoveredIndex;
        if (row < 0)
            return;

        contextObject = (GameObject)lbObjects.GetItem(0, row).Tag;
        lbObjects.SelectedIndex = row;
        objectContextMenu.Open(GetCursorPoint());
    }

    private void CenterCamera(GameObject mapObject)
    {
        if (map.GetTile(mapObject.Position)?.ContainsObject(mapObject) == true)
            cursorActionTarget.Camera.CenterOnCell(mapObject.Position);
    }

    private void OpenContextTrigger()
    {
        if (ContextTrigger is Trigger trigger)
            openTrigger(trigger);
    }

    private void DeleteContextObject()
    {
        if (map.GetTile(contextObject.Position)?.ContainsObject(contextObject) == true)
        {
            var mutation = new DeleteMapObjectsMutation(cursorActionTarget.MutationTarget, new List<GameObject> { contextObject });
            cursorActionTarget.MutationManager.PerformMutation(mutation);
        }

        RefreshObjects(lbObjects.TopIndex);
    }
}
