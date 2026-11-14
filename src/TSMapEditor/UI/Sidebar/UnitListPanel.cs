using MapEditorLibrary;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Graphics;
using MapEditorLibrary.Models;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using System;
using TSMapEditor.Rendering;
using TSMapEditor.UI.CursorActions;

namespace TSMapEditor.UI.Sidebar;

/// <summary>
/// A sidebar panel for listing units.
/// </summary>
public class UnitListPanel : ObjectListPanel
{
    public UnitListPanel(WindowManager windowManager, EditorState editorState, Map map, TheaterGraphics theaterGraphics, ICursorActionTarget cursorActionTarget, bool isNaval) : base(windowManager, editorState, map, theaterGraphics)
    {
        unitPlacementAction = new UnitPlacementAction(cursorActionTarget, Keyboard);
        unitPlacementAction.ActionExited += UnitPlacementAction_ActionExited;

        this.isNaval = isNaval;
    }

    private void UnitPlacementAction_ActionExited(object sender, System.EventArgs e)
    {
        ObjectTreeView.SelectedNode = null;
    }

    private readonly bool isNaval;
    private readonly UnitPlacementAction unitPlacementAction;

    private UnitPreviewRenderer previewRenderer;

    protected override (Texture2D regular, Texture2D remap) GetObjectTextures<T>(T objectType, ShapeImage[] textures)
    {
        const int facingUnitsPerDirection = (Constants.FacingMax + 1) / (int)Direction.Count;
        var unit = new Unit(objectType as UnitType) { Facing = (byte)((int)Direction.SE * facingUnitsPerDirection) };
        return previewRenderer.Render(unit);
    }

    protected override void InitObjects()
    {
        Func<UnitType, bool> filterFunction = null;

        if (isNaval)
        {
            filterFunction = u => u.SpeedType == "Float" || u.SpeedType == "Amphibious" || u.SpeedType == "Hover" || u.MovementZone == "Water";
        }
        else
        {
            filterFunction = u => u.SpeedType != "Float" && u.MovementZone != "Water";
        }

        int previewWidth = Math.Max(1, Constants.UITreeViewLineIndentation - Constants.UIEmptySideSpace);
        using var renderer = new UnitPreviewRenderer(GraphicsDevice, TheaterGraphics, previewWidth, ObjectTreeView.LineHeight);
        previewRenderer = renderer;
        InitObjectsBase(Map.Rules.UnitTypes, null, filterFunction);
        previewRenderer = null;
    }

    protected override void ObjectSelected()
    {
        if (ObjectTreeView.SelectedNode == null)
            return;

        unitPlacementAction.UnitType = (UnitType)ObjectTreeView.SelectedNode.Tag;
        EditorState.CursorAction = unitPlacementAction;
    }
}
