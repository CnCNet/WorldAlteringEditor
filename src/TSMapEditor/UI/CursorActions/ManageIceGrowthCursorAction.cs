using MapEditorLibrary;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;
using MapEditorLibrary.Mutations.Classes;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using System;

namespace TSMapEditor.UI.CursorActions;

public class ManageIceGrowthCursorAction : CursorAction
{
    public ManageIceGrowthCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
    {
    }

    private (int EventID, Point2D Cell)? lastDragPaint;

    public override string GetName() => Translate("Name", "Manage IceGrowth");

    public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
    {
        int startY = cellCoords.Y;
        int endY = cellCoords.Y + (CursorActionTarget.BrushSize.Height - 1);
        int startX = cellCoords.X;
        int endX = cellCoords.X + (CursorActionTarget.BrushSize.Width - 1);

        Func<Point2D, Map, Point2D> getCellTopLeftPoint = Is2DMode ? CellMath.CellTopLeftPointFromCellCoords : CellMath.CellTopLeftPointFromCellCoords_3D;

        Point2D startPoint = getCellTopLeftPoint(new Point2D(startX, startY), CursorActionTarget.Map) - cameraTopLeftPoint + new Point2D(Constants.CellSizeX / 2, 0);
        Point2D endPoint = getCellTopLeftPoint(new Point2D(endX, endY), CursorActionTarget.Map) - cameraTopLeftPoint + new Point2D(Constants.CellSizeX / 2, Constants.CellSizeY);
        Point2D corner1 = getCellTopLeftPoint(new Point2D(startX, endY), CursorActionTarget.Map) - cameraTopLeftPoint + new Point2D(0, Constants.CellSizeY / 2);
        Point2D corner2 = getCellTopLeftPoint(new Point2D(endX, startY), CursorActionTarget.Map) - cameraTopLeftPoint + new Point2D(Constants.CellSizeX, Constants.CellSizeY / 2);

        startPoint = startPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);
        endPoint = endPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);
        corner1 = corner1.ScaleBy(CursorActionTarget.Camera.ZoomLevel);
        corner2 = corner2.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

        Color lineColor = Color.LightSkyBlue;
        int thickness = 2;
        Renderer.DrawLine(startPoint.ToXNAVector(), corner1.ToXNAVector(), lineColor, thickness);
        Renderer.DrawLine(startPoint.ToXNAVector(), corner2.ToXNAVector(), lineColor, thickness);
        Renderer.DrawLine(corner1.ToXNAVector(), endPoint.ToXNAVector(), lineColor, thickness);
        Renderer.DrawLine(corner2.ToXNAVector(), endPoint.ToXNAVector(), lineColor, thickness);

        string text = Translate("Text", "Paint with left mouse button to enable IceGrowth.\r\nHold Shift while painting to disable IceGrowth.");

        DrawText(cellCoords, cameraTopLeftPoint, 60, -100, text, Color.Yellow);
    }

    public override void LeftClick(Point2D cellCoords)
    {
        // The mouse-up click can follow a drag that already painted this cell.
        if (lastDragPaint == (EventID, cellCoords))
            return;

        Paint(cellCoords);
    }

    public override void LeftDown(Point2D cellCoords)
    {
        Paint(cellCoords);
        lastDragPaint = (EventID, cellCoords);
    }

    private void Paint(Point2D cellCoords)
    {
        bool enableIceGrowth = !Keyboard.IsShiftHeldDown();

        PerformMutation(new SetIceGrowthMutation(MutationTarget, cellCoords, enableIceGrowth));
    }
}
