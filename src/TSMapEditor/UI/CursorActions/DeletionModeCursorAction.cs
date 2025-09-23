using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Mutations.Classes;

namespace TSMapEditor.UI.CursorActions
{
    /// <summary>
    /// A cursor action that allows the user to delete anything.
    /// </summary>
    public class DeletionModeCursorAction : CursorAction
    {
        public DeletionModeCursorAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
        {
        }

        public override string GetName() => Translate("Name", "Delete Object");

        public override bool DrawCellCursor => false;

        public override bool UseOnBridge => true;

        public override void OnActionEnter()
        {
            CursorActionTarget.BrushSize = Map.EditorConfig.BrushSizes.Find(bs => bs.Width == 1 && bs.Height == 1);
            base.OnActionEnter();
        }

        public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
        {
            var brushSize = CursorActionTarget.BrushSize;

            // Draw rectangle representing the brush size.
            Point2D topCellCoords = cellCoords - new Point2D((brushSize.Width - 1) / 2, (brushSize.Height - 1) / 2);
            Point2D bottomCellCoords = cellCoords + new Point2D(brushSize.Width / 2, brushSize.Height / 2);
            Point2D cornerRight = cellCoords + new Point2D(brushSize.Width / 2, (brushSize.Height - 1) / -2);
            Point2D cornerLeft = cellCoords + new Point2D((brushSize.Width - 1) / -2, brushSize.Height / 2);

            Func<Point2D, Map, Point2D> func = Is2DMode ? CellMath.CellTopLeftPointFromCellCoords : CellMath.CellTopLeftPointFromCellCoords_3D;

            Point2D topPixelPoint = func(topCellCoords, CursorActionTarget.Map) - cameraTopLeftPoint + new Point2D(Constants.CellSizeX / 2, 0);
            Point2D bottomPixelPoint = func(bottomCellCoords, CursorActionTarget.Map) - cameraTopLeftPoint + new Point2D(Constants.CellSizeX / 2, Constants.CellSizeY);
            Point2D leftPixelPoint = func(cornerLeft, CursorActionTarget.Map) - cameraTopLeftPoint + new Point2D(0, Constants.CellSizeY / 2);
            Point2D rightPixelPoint = func(cornerRight, CursorActionTarget.Map) - cameraTopLeftPoint + new Point2D(Constants.CellSizeX, Constants.CellSizeY / 2);

            topPixelPoint = topPixelPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);
            bottomPixelPoint = bottomPixelPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);
            rightPixelPoint = rightPixelPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);
            leftPixelPoint = leftPixelPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

            Color lineColor = Color.Red;
            int thickness = 2;
            Renderer.DrawLine(topPixelPoint.ToXNAVector(), rightPixelPoint.ToXNAVector(), lineColor, thickness);
            Renderer.DrawLine(topPixelPoint.ToXNAVector(), leftPixelPoint.ToXNAVector(), lineColor, thickness);
            Renderer.DrawLine(rightPixelPoint.ToXNAVector(), bottomPixelPoint.ToXNAVector(), lineColor, thickness);
            Renderer.DrawLine(leftPixelPoint.ToXNAVector(), bottomPixelPoint.ToXNAVector(), lineColor, thickness);

            // Draw "Delete" in the center of the area.
            Point2D cellCenterPoint;

            if (CursorActionTarget.Is2DMode)
                cellCenterPoint = CellMath.CellCenterPointFromCellCoords(cellCoords, Map) - cameraTopLeftPoint;
            else
                cellCenterPoint = CellMath.CellCenterPointFromCellCoords_3D(cellCoords, Map) - cameraTopLeftPoint;

            cellCenterPoint = cellCenterPoint.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

            string text = Translate("Text", "Delete");
            var textDimensions = Renderer.GetTextDimensions(text, Constants.UIBoldFont);
            int x = cellCenterPoint.X - (int)(textDimensions.X / 2);
            int y = cellCenterPoint.Y - (int)(textDimensions.Y / 2);

            Renderer.DrawStringWithShadow(text,
                Constants.UIBoldFont,
                new Vector2(x, y),
                Color.Red);
        }

        public override void LeftClick(Point2D cellCoords)
        {
            var mutation = new DeleteObjectMutation(MutationTarget, cellCoords, CursorActionTarget.BrushSize, CursorActionTarget.DeletionMode);

            CursorActionTarget.MutationManager.PerformMutation(mutation);

            if (mutation.DeletedCount == 0)
            {
                // There was nothing to delete. Undo the mutation to avoid needlessly cluttering the mutation list.
                CursorActionTarget.MutationManager.Undo();
            }
            else
            {
                CursorActionTarget.AddRefreshPoint(cellCoords);
            }
        }

        public override void LeftDown(Point2D cellCoords) => LeftClick(cellCoords);
    }
}
