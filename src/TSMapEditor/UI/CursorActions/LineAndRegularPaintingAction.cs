using MapEditorLibrary;
using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Misc;
using MapEditorLibrary.Models;
using MapEditorLibrary.Mutations;
using MapEditorLibrary.Mutations.Classes;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.Linq;

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

    public override IEnumerable<string> GetHintLines()
    {
        string key = KeyboardCommands.Instance.PlaceTerrainLine.GetKeyDisplayString();
        if (!string.IsNullOrEmpty(key))
            yield return string.Format(Translator.Translate("LineAndRegularPaintingAction.PaintInLineHint", "<{0}> to paint in line"), key);
    }

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

    protected static readonly BrushSize SingleCellBrush = new BrushSize(1, 1);

    protected virtual bool CanMirrorPaint => false;
    protected virtual int MirroredVariantCount => 1;

    protected virtual bool CanPaintMirroredCell(MapTile tile, int variant) => true;

    protected virtual IMutation CreateMirroredCellMutation(Point2D cell, int variant) =>
        throw new NotSupportedException("This tool does not support mirrored painting.");

    private protected virtual IEnumerable<IMutation> CreateMirroredPaintMutations(IEnumerable<MirroredPaintTarget> targets, bool line)
    {
        foreach (MirroredPaintTarget target in targets)
        {
            var mutation = CreateMirroredCellMutation(target.Cell, target.Variant);
            if (mutation is not ICheckableMutation checkable || checkable.ShouldPerform())
                yield return mutation;
        }
    }

    private (int Event, Point2D Origin, BrushSize Brush, MapMirroring Mirroring)? previousMirroredStamp;
    private readonly HashSet<Point2D> cellsPaintedThisStroke = new HashSet<Point2D>();

    protected virtual Point2D GetMirroredCursorCell(Point2D cellCoords) => cellCoords;

    private Point2D GetMirroredBrushOrigin(Point2D coords) =>
        CenterCellCoordsOnBrush ? GetCenteredBrushSizeCellCoords(coords) : coords;

    private IEnumerable<Point2D> GetMirroredLineCells(Point2D cellCoords)
    {
        (Direction direction, int length) = GetLineInformation(GetMirroredCursorCell(cellCoords));
        foreach (Point2D cell in CursorActionTarget.BrushSize.GetCellsAlongLine(GetMirroredBrushOrigin(LineSourceCell.Value), direction, length))
            yield return cell;
    }

    public override IEnumerable<(Point2D Cell, bool CanPlace)> GetMirroredPreview(Point2D cellCoords)
    {
        if (CursorActionTarget.Mirroring == null || !CanMirrorPaint)
            return null;

        if (Blocked)
            return Array.Empty<(Point2D, bool)>();

        var sourceCells = LineSourceCell.HasValue
            ? GetMirroredLineCells(cellCoords)
            : GetMirroredPreviewSourceCells(GetMirroredCursorCell(cellCoords));
        bool highlightPaintedCells = !LineSourceCell.HasValue && previousMirroredStamp?.Event == EventID &&
            CursorActionTarget.WindowManager.Cursor.LeftDown;

        return MirroredPaintTargets.GetTargets(Map, CursorActionTarget.Mirroring, sourceCells, MirroredVariantCount, CanPaintMirroredCell)
            .Select(target => (target.Cell, target.CanPaint || (highlightPaintedCells && cellsPaintedThisStroke.Contains(target.Cell))));
    }

    protected virtual IEnumerable<Point2D> GetMirroredPreviewSourceCells(Point2D cellCoords) =>
        CursorActionTarget.BrushSize.GetCells(GetMirroredBrushOrigin(cellCoords));

    protected bool TryPerformMirroredPaint(IEnumerable<Point2D> source, bool line = false, Point2D? stampOrigin = null)
    {
        if (CursorActionTarget.Mirroring == null || !CanMirrorPaint)
            return false;

        if (stampOrigin.HasValue)
        {
            var stamp = (EventID, stampOrigin.Value, CursorActionTarget.BrushSize, CursorActionTarget.Mirroring);
            if (previousMirroredStamp == stamp)
                return true;

            if (previousMirroredStamp?.Event != EventID)
                cellsPaintedThisStroke.Clear();

            previousMirroredStamp = stamp;
        }

        var targets = MirroredPaintTargets.GetTargets(Map, CursorActionTarget.Mirroring, source, MirroredVariantCount,
            CanPaintMirroredCell).Where(target => target.CanPaint);

        bool trackPaintedCells = stampOrigin.HasValue;
        if (trackPaintedCells)
            targets = targets.ToArray();

        var mutations = CreateMirroredPaintMutations(targets, line).ToArray();
        if (mutations.Length > 0)
        {
            PerformMutation(new MirroredPlacementMutation(MutationTarget, mutations, line ? Mutation.EventIdNone : EventID));

            if (trackPaintedCells)
                cellsPaintedThisStroke.UnionWith(targets.Select(target => target.Cell));
        }

        return true;
    }

    protected (Direction direction, int length) GetLineInformation(Point2D cellCoords)
    {
        // A zero-length line previews one brush stamp, for which direction is irrelevant.
        if (cellCoords == LineSourceCell.Value)
            return (Direction.NE, 0);

        Direction direction = Helpers.DirectionFromPoints(LineSourceCell.Value, cellCoords);
        Point2D vector = cellCoords - LineSourceCell.Value;
        int length = Math.Max(Math.Abs(vector.X), Math.Abs(vector.Y));

        return (direction, length);
    }

    public override void InactiveUpdate()
    {
        LineSourceCell = null;
        Blocked = false;
        previousMirroredStamp = null;

        if (LinePreviewMutation != null)
            ClearLinePreview();
    }

    protected void ApplyLinePreview(Point2D cellCoords)
    {
        if (!LineSourceCell.HasValue || !CanDrawLinePreview())
            return;

        (Direction direction, int length) = GetLineInformation(cellCoords);

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
        if (!TryPerformMirroredPaint(GetMirroredLineCells(cellCoords), line: true))
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
            if (!TryPerformMirroredPaint(CursorActionTarget.BrushSize.GetCells(coords), stampOrigin: coords))
            {
                var mutation = CreateRegularPlacementMutation(coords);
                if (mutation.ShouldPerform())
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

        cell = Map.GetTile(cellCoords);

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
