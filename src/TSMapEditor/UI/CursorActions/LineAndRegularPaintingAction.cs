using MapEditorLibrary;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;
using MapEditorLibrary.Mutations;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using System;

namespace TSMapEditor.UI.CursorActions;

/// <summary>
/// Base class for all mutations that can paint objects either directly on cells or in a line.
/// </summary>
public abstract class LineAndRegularPaintingAction : CursorAction
{
    protected LineAndRegularPaintingAction(ICursorActionTarget cursorActionTarget) : base(cursorActionTarget)
    {
    }

    public override bool OnlyUniqueCellEvents => false;

    protected virtual bool PreventInputEventsOnPreviousCell => true;

    protected virtual bool CenterCellCoordsOnBrush => false;

    protected virtual bool ClearPreviousCellOnMouseUp => false;

    protected Point2D GetCenteredBrushSizeCellCoords(Point2D cellCoords) => CursorActionTarget.BrushSize.CenterWithinBrush(cellCoords);

    protected Point2D? LineSourceCell { get; set; }
    protected bool Blocked { get; set; }

    /// <summary>
    /// Used to prevent a regular placement input on a cell
    /// where the input began as a line input.
    /// </summary>
    protected Point2D PreviousCellCoords { get; set; }

    protected Mutation LinePreviewMutation { get; set; }

    protected abstract ICheckableMutation CreateRegularPlacementMutation(Point2D cellCoords);

    protected abstract Mutation CreateLinePlacementMutation(Direction direction, int length);

    protected (Direction direction, int length) GetLineInformation(Point2D cellCoords)
    {
        Direction direction = Helpers.DirectionFromPoints(LineSourceCell.Value, cellCoords);
        Point2D vector = cellCoords - LineSourceCell.Value;
        int length = Math.Max(Math.Abs(vector.X), Math.Abs(vector.Y));

        return (direction, length);
    }

    public override void InactiveUpdate()
    {
        LineSourceCell = null;
        Blocked = false;

        if (LinePreviewMutation != null)
            ClearLinePreview();
    }

    protected void ApplyLinePreview(Point2D cellCoords)
    {
        if (!LineSourceCell.HasValue || LineSourceCell.Value == cellCoords || !CanDrawLinePreview())
            return;

        (Direction direction, int length) = GetLineInformation(cellCoords);

        if (length < 1)
            return;

        LinePreviewMutation = CreateLinePlacementMutation(direction, length);
        LinePreviewMutation.Perform();
    }

    protected void ClearLinePreview()
    {
        if (LinePreviewMutation != null)
        {
            LinePreviewMutation.Undo();
            LinePreviewMutation = null;
        }

        CursorActionTarget.InvalidateMap();
    }

    protected void ApplyLineInternal(Point2D cellCoords)
    {
        ApplyLine(cellCoords);
        LineSourceCell = null;
    }

    protected abstract void ApplyLine(Point2D cellCoords);

    protected virtual bool CanDrawLinePreview() => true;

    public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
    {
        if (!LineSourceCell.HasValue || !CanDrawLinePreview())
            return;

        if (cellCoords == LineSourceCell.Value)
            return;

        (Direction direction, int length) = GetLineInformation(cellCoords);

        Point2D cameraPoint1 = (CellMath.CellCenterPointFromCellCoords_3D(LineSourceCell.Value, Map) - cameraTopLeftPoint).ScaleBy(CursorActionTarget.Camera.ZoomLevel);
        Point2D cameraPoint2 = (CellMath.CellCenterPointFromCellCoords_3D(LineSourceCell.Value + Helpers.VisualDirectionToPoint(direction).ScaleBy(length), Map) - cameraTopLeftPoint).ScaleBy(CursorActionTarget.Camera.ZoomLevel);

        Renderer.DrawLine(cameraPoint1.ToXNAVector(), cameraPoint2.ToXNAVector(), Color.Orange, 2);
    }

    protected virtual void TryPerformRegularPlacementMutation(Point2D coords)
    {
        if (!PreventInputEventsOnPreviousCell || PreviousCellCoords != coords)
        {
            var mutation = CreateRegularPlacementMutation(coords);
            if (mutation.ShouldPerform())
            {
                CursorActionTarget.MutationManager.PerformMutation(mutation);
            }
            PreviousCellCoords = coords;
        }
    }

    public override void LeftDown(Point2D cellCoords)
    {
        if (Blocked)
            return;

        Point2D coords = cellCoords;
        MapTile cell;
        if (CenterCellCoordsOnBrush)
        {
            coords = GetCenteredBrushSizeCellCoords(cellCoords);
        }
        else
        {
            coords = cellCoords;
        }

        cell = Map.GetTile(coords);

        if (KeyboardCommands.Instance.PlaceTerrainLine.AreKeysOrModifiersDown(Keyboard))
        {
            if (LineSourceCell == null && cell != null)
            {
                LineSourceCell = cellCoords;
                PreviousCellCoords = cellCoords;
            }

            return;
        }

        TryPerformRegularPlacementMutation(coords);
    }

    public override void LeftClick(Point2D cellCoords)
    {
        if (KeyboardCommands.Instance.PlaceTerrainLine.AreKeysOrModifiersDown(Keyboard))
        {
            if (LineSourceCell != null && cellCoords != LineSourceCell.Value)
            {
                ApplyLineInternal(cellCoords);
            }

            return;
        }

        LeftDown(cellCoords);
        Blocked = false;
    }

    public override void Update(Point2D? cellCoords)
    {
        if (LineSourceCell != null && cellCoords != null && LineSourceCell != cellCoords)
        {
            if (!KeyboardCommands.Instance.PlaceTerrainLine.AreKeysOrModifiersDown(Keyboard))
            {
                ApplyLineInternal(cellCoords.Value);
                Blocked = true;
            }
        }

        if (!CursorActionTarget.WindowManager.Cursor.LeftDown && !CursorActionTarget.WindowManager.Cursor.LeftClicked)
        {
            Blocked = false;

            if (ClearPreviousCellOnMouseUp)
                PreviousCellCoords = Point2D.NegativeOne;
        }
    }
}
