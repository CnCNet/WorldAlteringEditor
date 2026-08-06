using MapEditorLibrary;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;
using MapEditorLibrary.Mutations.Classes;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.Input;
using System;
using System.Collections.Generic;

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

    private List<Point2D> connectedTilePath;
    private ConnectedTileSide connectedTileSide = ConnectedTileSide.Front;
    private DrawConnectedTilesMutation previewMutation;
    private ConnectedTilePlan previewPlan;
    private string previewPlanFailureText;
    private bool useEndPieces;
    private bool closed;
    private byte extraHeight = 0;

    private int randomSeed = new Random().Next();

    public override void OnActionEnter()
    {
        connectedTilePath = new List<Point2D>();
        previewMutation = null;
        previewPlan = null;
        previewPlanFailureText = null;
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

        if (!string.IsNullOrEmpty(previewPlanFailureText))
        {
            text += "\r\n\r\n" + string.Format(
                Translate("PlanFailureText", "Cannot create an exact connected pattern: {0}\r\nAdjust the vertices or press R to try another pattern."),
                previewPlanFailureText);
        }

        DrawText(cellCoords, cameraTopLeftPoint, 60, -180, text,
            string.IsNullOrEmpty(previewPlanFailureText) ? Color.Yellow : Color.OrangeRed);

        Func<Point2D, Map, Point2D> getCellCenterPoint = Is2DMode ? CellMath.CellCenterPointFromCellCoords : CellMath.CellCenterPointFromCellCoords_3D;

        bool pointingAtFirstVertex = connectedTilePath.Count > 0 && cellCoords == connectedTilePath[0];
        bool showingClosingGuide = closed || (pointingAtFirstVertex && HasEnoughVerticesToClose());

        if (connectedTilePath.Count > 0)
        {
            Point2D start = connectedTilePath[0];
            start = getCellCenterPoint(start, CursorActionTarget.Map) - cameraTopLeftPoint;
            start = start.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

            Color color = closed && previewPlan == null
                ? Color.OrangeRed
                : showingClosingGuide
                    ? Color.LimeGreen
                    : Color.Red;
            int precision = 8;
            int thickness = 3;
            Renderer.DrawCircle(start.ToXNAVector(), Constants.CellSizeY * 0.25f, color, precision, thickness);
        }

        // Draw cliff path
        for (int i = 0; i < connectedTilePath.Count - 1; i++)
        {
            Point2D start = connectedTilePath[i];
            start = getCellCenterPoint(start, CursorActionTarget.Map) - cameraTopLeftPoint;
            start = start.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

            Point2D end = connectedTilePath[i + 1];
            end = getCellCenterPoint(end, CursorActionTarget.Map) - cameraTopLeftPoint;
            end = end.ScaleBy(CursorActionTarget.Camera.ZoomLevel);


            Color color = Color.Goldenrod;
            int thickness = 3;

            Renderer.DrawLine(start.ToXNAVector(), end.ToXNAVector(), color, thickness);
        }

        if (showingClosingGuide && connectedTilePath.Count > 1)
        {
            Point2D start = getCellCenterPoint(connectedTilePath[connectedTilePath.Count - 1], CursorActionTarget.Map) - cameraTopLeftPoint;
            start = start.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

            Point2D end = getCellCenterPoint(connectedTilePath[0], CursorActionTarget.Map) - cameraTopLeftPoint;
            end = end.ScaleBy(CursorActionTarget.Camera.ZoomLevel);

            Color closingGuideColor = closed && previewPlan == null ? Color.OrangeRed : Color.LimeGreen;
            Renderer.DrawLine(start.ToXNAVector(), end.ToXNAVector(), closingGuideColor, 3);
        }
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
            if (previewPlan != null)
            {
                ConnectedTilePlan planToCommit = previewPlan;
                previewMutation?.Undo();
                previewMutation = null;
                previewPlan = null;

                CursorActionTarget.MutationManager.PerformMutation(
                    new DrawConnectedTilesMutation(MutationTarget, planToCommit, randomSeed, extraHeight));

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
        previewPlan = null;
        previewPlanFailureText = null;

        if (connectedTilePath.Count < 2 || (closed && !HasEnoughVerticesToClose()))
            return;

        ConnectedTilePlanResult planResult = ConnectedTilePlanner.Plan(
            connectedTileType,
            connectedTilePath,
            connectedTileSide,
            randomSeed,
            useEndPieces && !closed,
            closed,
            coords => MutationTarget.Map.GetTile(coords) != null);

        if (!planResult.IsSuccess)
        {
            previewPlanFailureText = GetPlanFailureText(planResult);
            return;
        }

        previewPlan = planResult.Plan;
        previewMutation = new DrawConnectedTilesMutation(MutationTarget, previewPlan, randomSeed, extraHeight);
        previewMutation.Perform();
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
    }
}
