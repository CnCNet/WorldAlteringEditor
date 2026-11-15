using MapEditorLibrary;
using MapEditorLibrary.Graphics;
using MapEditorLibrary.Models;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using System;
using TSMapEditor.Rendering;
using TSMapEditor.UI.CursorActions;

namespace TSMapEditor.UI.Sidebar;

public class BuildingListPanel : ObjectListPanel
{
    public BuildingListPanel(WindowManager windowManager, EditorState editorState,
        Map map, TheaterGraphics theaterGraphics, ICursorActionTarget cursorActionTarget) : 
        base(windowManager, editorState, map, theaterGraphics)
    {
        buildingPlacementAction = new BuildingPlacementAction(cursorActionTarget, Keyboard);
        buildingPlacementAction.ActionExited += BuildingPlacementAction_ActionExited;
    }

    private void BuildingPlacementAction_ActionExited(object sender, System.EventArgs e)
    {
        ObjectTreeView.SelectedNode = null;
    }

    private readonly BuildingPlacementAction buildingPlacementAction;

    private ObjectPreviewRenderer previewRenderer;

    protected override (Texture2D regular, Texture2D remap) GetObjectTextures<T>(T objectType, ShapeImage[] textures)
    {
        return previewRenderer.Render(objectType as BuildingType);
    }

    protected override void InitObjects()
    {
        int previewWidth = Math.Max(1, Constants.UITreeViewLineIndentation - Constants.UIEmptySideSpace);
        using var renderer = new ObjectPreviewRenderer(GraphicsDevice, TheaterGraphics, previewWidth, ObjectTreeView.LineHeight);
        previewRenderer = renderer;
        InitObjectsBase(Map.Rules.BuildingTypes, null);
        previewRenderer = null;
    }

    protected override void ObjectSelected()
    {
        if (ObjectTreeView.SelectedNode == null)
            return;

        buildingPlacementAction.BuildingType = (BuildingType)ObjectTreeView.SelectedNode.Tag;
        EditorState.CursorAction = buildingPlacementAction;
    }
}
