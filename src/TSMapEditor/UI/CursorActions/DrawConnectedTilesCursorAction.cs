using MapEditorLibrary;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;
using MapEditorLibrary.Mutations;
using MapEditorLibrary.Mutations.Classes;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TSMapEditor.UI.CursorActions;

/// <summary>
/// Cursor action for drawing connected tiles.
/// </summary>
public class DrawConnectedTilesCursorAction : CursorAction
{
    public DrawConnectedTilesCursorAction(ICursorActionTarget cursorActionTarget, ConnectedTileType connectedTileType) : base(cursorActionTarget)
    {
        this.connectedTileType = connectedTileType;
        ActionExited += UndoOnExit;
    }

    public override string GetName() => Translate("Name", "Draw Connected Tiles");

    public override bool HandlesKeyboardInput => true;

    public override bool DrawCellCursor => true;

    private readonly ConnectedTileType connectedTileType;

    private sealed record PathPreview(Point2D[] Vertices, string Failure);

    private readonly List<Point2D> connectedTilePath = new List<Point2D>();
    private readonly List<PathPreview> previewPaths = new List<PathPreview>();
    private ConnectedTileSide connectedTileSide = ConnectedTileSide.Front;
    private IMutation previewMutation;
    private MapMirroring previewMirroring;
    private bool useEndPieces;
    private bool closed;
    private byte extraHeight = 0;

    private int randomSeed = new Random().Next();

    public override void OnActionEnter()
    {
        connectedTilePath.Clear();
        previewPaths.Clear();
        previewMutation = null;
        previewMirroring = CursorActionTarget.Mirroring;
        useEndPieces = false;
        closed = false;

        base.OnActionEnter();
    }

    public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
    {
        string mainText = Translate("MainText.V3", "Click on a cell to place a new vertex.\r\n\r\n" +
            "After placing at least three distinct vertices, you can optionally\r\nclick the first one to close the formation.\r\n\r\n" +
            "ENTER to confirm\r\n" +
            "Backspace to go back one step\r\n" +
            "R to re-generate the pattern\r\n");

        string tabText = Translate("TabText", "TAB to toggle between front and back sides\r\n");
        string pageUpDownText = Translate("PageUpDownText", "PageUp to raise the tiles, PageDown to lower them\r\n");
        string endPiecesText = string.Empty;
        if (connectedTileType.SupportsEndPieces && !closed)
        {
            endPiecesText = string.Format(
                Translate("EndPiecesText", "E to toggle ending pieces ({0})\r\n"),
                useEndPieces ? Translate("EndPieces.Enabled", "enabled") : Translate("EndPieces.Disabled", "disabled"));
        }

        string closedText = closed
            ? Translate("ClosedText", "Closed formation (ending pieces disabled); Backspace to reopen\r\n")
            : string.Empty;
        string exitText = Translate("ExitText", "Right-click or ESC to exit");

        string text = mainText + endPiecesText + closedText;
        if (!connectedTileType.FrontOnly)
            text += tabText;
        if (!Constants.IsFlatWorld)
            text += pageUpDownText;
        text += exitText;

        string previewPlanFailureText = previewPaths.Count > 0 ? previewPaths[0].Failure : null;
        if (previewPlanFailureText != null)
        {
            text += "\r\n\r\n" + string.Format(
                Translate("PlanFailureText", "Cannot create an exact connected pattern: {0}\r\nAdjust the vertices or press R to try another pattern."),
                previewPlanFailureText);
        }

        var mirroredFailures = previewPaths.Skip(1).Select(path => path.Failure).Where(failure => failure != null).Distinct().ToArray();
        if (mirroredFailures.Length > 0)
        {
            text += "\r\n\r\n" + string.Format(Translate("MirroredPathsSkipped", "Some mirrored paths were skipped ({0})."),
                string.Join(", ", mirroredFailures));
        }

        var textSize = Renderer.MeasureString(text, Constants.UIBoldFont);
        Color textColor = previewPlanFailureText != null || mirroredFailures.Length > 0 ? Color.OrangeRed : Color.Yellow;
        DrawText(cellCoords, cameraTopLeftPoint, 60, -(int)textSize.Y - 50, text, textColor);

        Func<Point2D, Map, Point2D> getCellCenterPoint = Is2DMode ? CellMath.CellCenterPointFromCellCoords : CellMath.CellCenterPointFromCellCoords_3D;

        bool pointingAtFirstVertex = connectedTilePath.Count > 0 && cellCoords == connectedTilePath[0];
        bool showingClosingGuide = closed || (pointingAtFirstVertex && HasEnoughVerticesToClose());

        Vector2 ToView(Point2D cell) =>
            (getCellCenterPoint(cell, Map) - cameraTopLeftPoint).ScaleBy(CursorActionTarget.Camera.ZoomLevel).ToXNAVector();

        for (int pathIndex = 0; pathIndex < previewPaths.Count; pathIndex++)
        {
            PathPreview path = previewPaths[pathIndex];
            Color pathColor = path.Failure == null ? Color.Goldenrod : Color.OrangeRed;
            Vector2 start = ToView(path.Vertices[0]);

            if (pathIndex == 0)
            {
                Color startColor = showingClosingGuide ? Color.LimeGreen : Color.Red;
                if (closed && path.Failure != null)
                    startColor = Color.OrangeRed;

                Renderer.DrawCircle(start, Constants.CellSizeY * 0.25f, startColor, 8, 3);
            }

            Vector2 previous = start;
            for (int i = 1; i < path.Vertices.Length; i++)
            {
                Vector2 next = ToView(path.Vertices[i]);
                Renderer.DrawLine(previous, next, pathColor, 3);
                previous = next;
            }

            if (showingClosingGuide && path.Vertices.Length > 1)
                Renderer.DrawLine(previous, start, path.Failure == null ? Color.LimeGreen : Color.OrangeRed, 3);
        }
    }

    public override void Update(Point2D? cellCoords)
    {
        if (previewMirroring != CursorActionTarget.Mirroring)
            RedrawPreview();
    }

    public override void OnKeyPressed(KeyPressEventArgs e, Point2D cellCoords)
    {
        if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.Escape)
        {
            ExitAction();

            e.Handled = true;
        }
        else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.Tab)
        {
            if (!connectedTileType.FrontOnly)
            {
                connectedTileSide = connectedTileSide == ConnectedTileSide.Front ? ConnectedTileSide.Back : ConnectedTileSide.Front;
                RedrawPreview();
            }

            e.Handled = true;
        }
        else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.Back)
        {
            if (closed)
            {
                closed = false;
            }
            else if (connectedTilePath.Count > 0)
            {
                connectedTilePath.RemoveAt(connectedTilePath.Count - 1);
            }

            RedrawPreview();

            e.Handled = true;
        }
        else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.E)
        {
            if (connectedTileType.SupportsEndPieces && !closed)
            {
                useEndPieces = !useEndPieces;
                RedrawPreview();
            }

            e.Handled = true;
        }
        else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.R)
        {
            if (connectedTilePath.Count > 0)
            {
                randomSeed = new Random().Next();
                RedrawPreview();
            }

            e.Handled = true;
        }
        else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.PageUp)
        {
            if (!Constants.IsFlatWorld)
            {
                if (connectedTilePath.Count > 0)
                {
                    if (MutationTarget.Map.GetTile(connectedTilePath[0]).Level + extraHeight + 1 <= Constants.MaxMapHeightLevel)
                        extraHeight++;
                }

                RedrawPreview();
            }

            e.Handled = true;
        }
        else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.PageDown)
        {
            if (!Constants.IsFlatWorld)
            {
                if (connectedTilePath.Count > 0)
                {
                    if (MutationTarget.Map.GetTile(connectedTilePath[0]).Level + extraHeight - 1 >= 0)
                        extraHeight--;
                }

                RedrawPreview();
            }

            e.Handled = true;
        }
        else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.Enter)
        {
            if (previewMirroring != CursorActionTarget.Mirroring)
                RedrawPreview();

            if (previewMutation != null)
            {
                var mutationToCommit = previewMutation;
                mutationToCommit.Undo();
                previewMutation = null;
                MutationManager.PerformMutation(mutationToCommit);

                ExitAction();
            }

            e.Handled = true;
        }
    }

    public override void LeftClick(Point2D cellCoords)
    {
        if (closed)
            return;

        if (connectedTilePath.Count > 0 && cellCoords == connectedTilePath[0] && HasEnoughVerticesToClose())
        {
            closed = true;
            RedrawPreview();
            return;
        }

        connectedTilePath.Add(cellCoords);
        RedrawPreview();
    }

    private void RedrawPreview()
    {
        previewMutation?.Undo();
        previewMutation = null;
        previewMirroring = CursorActionTarget.Mirroring;
        previewPaths.Clear();

        if (connectedTilePath.Count == 0)
            return;

        var mutations = new List<IMutation>();
        var occupiedCells = new HashSet<Point2D>();
        Point2D doubledPoint = previewMirroring?.GetDoubledMirroringPoint(Map.Size) ?? Point2D.Zero;
        bool canPlan = connectedTilePath.Count >= 2 && (!closed || HasEnoughVerticesToClose());

        foreach (MirrorTransform transform in previewMirroring?.Transforms ?? new[] { default(MirrorTransform) })
        {
            Point2D[] vertices = transform == default ? connectedTilePath.ToArray()
                : connectedTilePath.Select(cell => MapMirroring.TransformCell(cell, doubledPoint, transform)).ToArray();
            if (previewPaths.Any(path => IsSamePath(vertices, path.Vertices)))
                continue;

            string failure = null;
            if (canPlan)
            {
                (DrawConnectedTilesMutation mutation, failure) = PlanPath(vertices, transform, occupiedCells);
                if (mutation != null)
                    mutations.Add(mutation);
            }

            previewPaths.Add(new PathPreview(vertices, failure));
        }

        if (mutations.Count == 0)
            return;

        previewMutation = mutations.Count == 1 ? mutations[0]
            : new MirroredPlacementMutation(MutationTarget, mutations, Mutation.EventIdNone);
        previewMutation.Perform();
    }

    private (DrawConnectedTilesMutation Mutation, string Failure) PlanPath(Point2D[] vertices, MirrorTransform transform,
        HashSet<Point2D> occupiedCells)
    {
        if (transform != default && vertices.Any(cell => Map.GetTile(cell) == null))
            return (null, Translate("PlanStatus.OutOfBounds", "a path extends outside the map"));

        ConnectedTileSide side = connectedTileSide;
        if (!connectedTileType.FrontOnly && transform != default && connectedTilePath[0] != connectedTilePath[1])
        {
            bool reversesPathDirection = PointsEastOrSouth(connectedTilePath[0], connectedTilePath[1]) !=
                PointsEastOrSouth(vertices[0], vertices[1]);

            // Compare the editor's eight visual directions so a small sideways drift
            // in a north-south cliff does not reverse its orientation.
            if (transform.Reflected != reversesPathDirection)
                side = side == ConnectedTileSide.Front ? ConnectedTileSide.Back : ConnectedTileSide.Front;
        }

        ConnectedTilePlanResult result = ConnectedTilePlanner.Plan(
            connectedTileType, vertices, side, randomSeed, useEndPieces && !closed, closed, coords => Map.GetTile(coords) != null);

        if (!result.IsSuccess)
            return (null, GetPlanFailureText(result));

        var mutation = new DrawConnectedTilesMutation(MutationTarget, result.Plan, randomSeed, extraHeight);
        if (previewMirroring == null)
            return (mutation, null);

        // The planner uses configured foundations, which can differ from the subtiles actually written.
        var placementCells = mutation.GetPlacementCells().ToHashSet();
        if (placementCells.Count == 0)
            return (null, Translate("PlanStatus.Empty", "no tiles could be placed"));

        if (transform != default && placementCells.Any(cell => Map.GetTile(cell) == null))
            return (null, Translate("PlanStatus.OutOfBounds", "a path extends outside the map"));

        if (occupiedCells.Overlaps(placementCells))
            return (null, Translate("PlanStatus.Overlap", "a path overlaps another placement"));

        occupiedCells.UnionWith(placementCells);
        return (mutation, null);
    }

    private static bool PointsEastOrSouth(Point2D start, Point2D end) =>
        Helpers.DirectionFromPoints(start, end) is Direction.NE or Direction.E or Direction.SE or Direction.S;

    private bool IsSamePath(Point2D[] path, Point2D[] other)
    {
        if (!closed)
            return path.SequenceEqual(other) || path.SequenceEqual(other.Reverse());

        // The same closed path can start at a different vertex or run in the opposite direction.
        for (int start = 0; start < other.Length; start++)
        {
            if (path[0] != other[start])
                continue;

            var indices = Enumerable.Range(1, path.Length - 1);
            if (indices.All(i => path[i] == other[(start + i) % other.Length]) ||
                indices.All(i => path[i] == other[(start - i + other.Length) % other.Length]))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasEnoughVerticesToClose()
    {
        return new HashSet<Point2D>(connectedTilePath).Count >= 3;
    }

    private string GetPlanFailureText(ConnectedTilePlanResult planResult)
    {
        return planResult.Status switch
        {
            ConnectedTilePlanStatus.InvalidInput => Translate("PlanStatus.InvalidInput", "the selected path is invalid"),
            ConnectedTilePlanStatus.NoSolution => Translate("PlanStatus.NoSolution", "no exact connection pattern fits the selected vertices"),
            ConnectedTilePlanStatus.SearchLimit => Translate("PlanStatus.SearchLimit", "the search limit was reached before an exact pattern was found"),
            _ when !string.IsNullOrWhiteSpace(planResult.Message) => planResult.Message,
            _ => Translate("PlanStatus.Unknown", "the connected tile planner failed")
        };
    }

    private void UndoOnExit(object sender, EventArgs e)
    {
        previewMutation?.Undo();
        previewMutation = null;
        previewPaths.Clear();
    }
}
