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
using System.Threading.Tasks;

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
    private sealed record PathToPlan(int PreviewIndex, Point2D[] Vertices, MirrorTransform Transform, ConnectedTileSide Side,
        ConnectedTileAnchor? StartAnchor, ConnectedTileAnchor? EndAnchor);

    private readonly List<Point2D> connectedTilePath = new List<Point2D>();
    private readonly List<PathPreview> previewPaths = new List<PathPreview>();
    private readonly Dictionary<Point2D, ConnectedTileConnectionFinder.TerrainCell> terrainUnderPreview = new();
    private ConnectedTileConnectionFinder connectionFinder;
    private ConnectedTileAnchor? startAnchor;
    private ConnectedTileAnchor? endAnchor;
    private ConnectedTileSide connectedTileSide = ConnectedTileSide.Front;
    private IMutation previewMutation;
    private MapMirroring previewMirroring;
    private bool useEndPieces;
    private bool closed;
    private byte extraHeight = 0;

    private int randomSeed = new Random().Next();

    private bool IsAttached => startAnchor.HasValue || endAnchor.HasValue;

    private bool CanUseEndPieces => connectedTileType.SupportsEndPieces && !closed && (!startAnchor.HasValue || !endAnchor.HasValue);

    public override void OnActionEnter()
    {
        connectedTilePath.Clear();
        previewPaths.Clear();
        previewMutation = null;
        previewMirroring = CursorActionTarget.Mirroring;
        useEndPieces = false;
        closed = false;
        startAnchor = null;
        endAnchor = null;
        terrainUnderPreview.Clear();
        var theaterGraphics = CursorActionTarget.TheaterGraphics;
        connectionFinder = new ConnectedTileConnectionFinder(connectedTileType, theaterGraphics.Theater,
            tileIndex => theaterGraphics.GetTileGraphics(tileIndex, 0), GetTerrain);

        base.OnActionEnter();
    }

    public override void DrawPreview(Point2D cellCoords, Point2D cameraTopLeftPoint)
    {
        string mainText;
        if (IsAttached)
        {
            mainText = endAnchor.HasValue ? string.Empty : Translate("VertexText", "Click on a cell to place a new vertex.\r\n\r\n");
            mainText += Translate("ControlsText", "ENTER to confirm\r\nBackspace to go back one step\r\nR to re-generate the pattern\r\n");
        }
        else
        {
            mainText = Translate("MainText.V3", "Click on a cell to place a new vertex.\r\n\r\n" +
                "After placing at least three distinct vertices, you can optionally\r\nclick the first one to close the formation.\r\n\r\n" +
                "ENTER to confirm\r\nBackspace to go back one step\r\nR to re-generate the pattern\r\n");
        }

        string tabText = Translate("TabText", "TAB to toggle between front and back sides\r\n");
        string pageUpDownText = Translate("PageUpDownText", "PageUp to raise the tiles, PageDown to lower them\r\n");
        string endPiecesText = string.Empty;
        if (CanUseEndPieces)
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
        if (!connectedTileType.FrontOnly && !startAnchor.HasValue)
            text += tabText;
        if (!Constants.IsFlatWorld && !IsAttached)
            text += pageUpDownText;
        text += exitText;

        bool pointingAtConnection = FindHoveredConnection(cellCoords).HasValue;
        if (pointingAtConnection)
        {
            text += "\r\n\r\n" + (connectedTilePath.Count == 0
                ? Translate("StartConnectionText", "Click to start from this connection point.")
                : Translate("EndConnectionText", "Click to connect the path to this point."));
        }

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
        bool showingClosingGuide = closed || (pointingAtFirstVertex && CanClosePath());

        Vector2 ToView(Point2D cell) =>
            (getCellCenterPoint(cell, Map) - cameraTopLeftPoint).ScaleBy(CursorActionTarget.Camera.ZoomLevel).ToXNAVector();

        if (pointingAtConnection)
            Renderer.DrawCircle(ToView(cellCoords), Constants.CellSizeY * 0.25f, Color.LimeGreen, 8, 3);

        for (int pathIndex = 0; pathIndex < previewPaths.Count; pathIndex++)
        {
            PathPreview path = previewPaths[pathIndex];
            Color pathColor = path.Failure == null ? Color.Goldenrod : Color.OrangeRed;
            Vector2 start = ToView(path.Vertices[0]);

            if (pathIndex == 0)
            {
                Color startColor = showingClosingGuide || startAnchor.HasValue ? Color.LimeGreen : Color.Red;
                if ((closed || startAnchor.HasValue) && path.Failure != null)
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

            if (pathIndex == 0 && endAnchor.HasValue)
                Renderer.DrawCircle(previous, Constants.CellSizeY * 0.25f, path.Failure == null ? Color.LimeGreen : Color.OrangeRed, 8, 3);
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
            if (!connectedTileType.FrontOnly && !startAnchor.HasValue)
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
                endAnchor = null;
                if (connectedTilePath.Count == 0)
                    startAnchor = null;
            }

            RedrawPreview();

            e.Handled = true;
        }
        else if (e.PressedKey == Microsoft.Xna.Framework.Input.Keys.E)
        {
            if (CanUseEndPieces)
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
            if (!Constants.IsFlatWorld && !IsAttached)
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
            if (!Constants.IsFlatWorld && !IsAttached)
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
        if (closed || endAnchor.HasValue)
            return;

        if (connectedTilePath.Count > 0 && cellCoords == connectedTilePath[0])
        {
            if (CanClosePath())
            {
                closed = true;
                RedrawPreview();
                return;
            }

            if (startAnchor.HasValue)
                return;
        }

        ConnectedTileAnchor? anchor = FindHoveredConnection(cellCoords);
        if (connectedTilePath.Count == 0)
            startAnchor = anchor;
        else
            endAnchor = anchor;

        connectedTilePath.Add(cellCoords);
        RedrawPreview();
    }

    private ConnectedTileAnchor? FindHoveredConnection(Point2D cellCoords)
    {
        if (closed || endAnchor.HasValue || (connectedTilePath.Count > 0 && cellCoords == connectedTilePath[0]))
            return null;

        ConnectedTileAnchor? anchor = connectionFinder.FindOpenConnection(cellCoords);
        return anchor.HasValue && connectedTilePath.Count > 0 ? PreferConnectionToward(anchor.Value, connectedTilePath[^1]) : anchor;
    }

    private ConnectedTileAnchor PreferConnectionToward(ConnectedTileAnchor anchor, Point2D cell)
    {
        if (anchor.Coordinates == cell)
            return anchor;

        var direction = Helpers.DirectionFromPoints(anchor.Coordinates, cell);
        byte mask = (byte)(0b10000000 >> (int)direction);
        return (anchor.ConnectionPoint.ConnectionMask & mask) != 0
            ? anchor
            : connectionFinder.FindOpenConnection(anchor.Coordinates, mask) ?? anchor;
    }

    private ConnectedTileConnectionFinder.TerrainCell? GetTerrain(Point2D cell)
    {
        if (terrainUnderPreview.TryGetValue(cell, out var original))
            return original;

        var tile = Map.GetTile(cell);
        return tile == null ? null : new ConnectedTileConnectionFinder.TerrainCell(tile.TileIndex, tile.SubTileIndex, tile.Level);
    }

    private void RedrawPreview()
    {
        previewMutation?.Undo();
        previewMutation = null;
        terrainUnderPreview.Clear();
        previewMirroring = CursorActionTarget.Mirroring;
        previewPaths.Clear();

        if (connectedTilePath.Count == 0)
            return;

        var pathsToPlan = new List<PathToPlan>();
        var map = Map;
        Func<Point2D, bool> isCellValid = cell => map.GetTile(cell) != null;
        Point2D doubledPoint = previewMirroring?.GetDoubledMirroringPoint(map.Size) ?? Point2D.Zero;
        bool canPlan = connectedTilePath.Count >= 2;
        ConnectedTileAnchor? sourceStart = canPlan && startAnchor.HasValue
            ? PreferConnectionToward(startAnchor.Value, connectedTilePath[1])
            : startAnchor;

        foreach (MirrorTransform transform in previewMirroring?.Transforms ?? new[] { default(MirrorTransform) })
        {
            Point2D[] vertices = transform == default ? connectedTilePath.ToArray()
                : connectedTilePath.Select(cell => MapMirroring.TransformCell(cell, doubledPoint, transform)).ToArray();
            if (previewPaths.Any(path => IsSamePath(vertices, path.Vertices)))
                continue;

            string failure = null;
            if (canPlan)
            {
                if (transform != default && vertices.Any(cell => !isCellValid(cell)))
                {
                    failure = Translate("PlanStatus.OutOfBounds", "a path extends outside the map");
                }
                else
                {
                    ConnectedTileAnchor? pathStart = GetMirroredAnchor(sourceStart, vertices[0], transform);
                    ConnectedTileAnchor? pathEnd = GetMirroredAnchor(endAnchor, vertices[^1], transform);
                    if ((startAnchor.HasValue && !pathStart.HasValue) || (endAnchor.HasValue && !pathEnd.HasValue))
                    {
                        failure = Translate("PlanStatus.MissingConnection", "no matching open connection point was found");
                    }
                    else if (pathStart.HasValue && pathEnd.HasValue && pathStart.Value.BaseLevel != pathEnd.Value.BaseLevel)
                    {
                        failure = Translate("PlanStatus.ConnectionHeight", "the existing connection points are at different elevations");
                    }
                    else
                    {
                        var side = pathStart?.ConnectionPoint.Side ?? GetStartingSide(vertices, transform);
                        pathsToPlan.Add(new PathToPlan(previewPaths.Count, vertices, transform, side, pathStart, pathEnd));
                    }
                }
            }

            previewPaths.Add(new PathPreview(vertices, failure));
        }

        if (pathsToPlan.Count == 0)
            return;

        var plans = new ConnectedTilePlanResult[pathsToPlan.Count];
        int seed = randomSeed;
        bool isClosed = closed;
        bool hasEndPieces = useEndPieces && !isClosed;
        byte? sourceFrontSeamDirections = null;

        bool CanPlaceOnCell(Point2D cell) => !connectionFinder.ContainsTile(map.GetTile(cell).TileIndex);
        Func<Point2D, bool> canPlaceOnCell = IsAttached ? CanPlaceOnCell : null;

        void PlanPath(int index)
        {
            var path = pathsToPlan[index];
            byte? frontSeamDirections = sourceFrontSeamDirections;
            if (frontSeamDirections.HasValue && path.Transform.Reflected)
                frontSeamDirections = (byte)~frontSeamDirections.Value;

            plans[index] = ConnectedTilePlanner.Plan(connectedTileType, path.Vertices, path.Side, seed, hasEndPieces, isClosed,
                isCellValid, frontSeamDirections, path.StartAnchor, path.EndAnchor, canPlaceOnCell);
        }

        int firstPathToPlan = 0;
        if (isClosed && !connectedTileType.FrontOnly && pathsToPlan.Count > 1)
        {
            PlanPath(0);
            firstPathToPlan = 1;

            if (plans[0].Plan is ConnectedTilePlan sourcePlan)
                sourceFrontSeamDirections = GetFrontSeamDirections(sourcePlan);
        }

        // The editor thread waits here, keeping the map and tile definitions stable for the workers.
        if (pathsToPlan.Count - firstPathToPlan == 1)
            PlanPath(firstPathToPlan);
        else
            Parallel.For(firstPathToPlan, pathsToPlan.Count, PlanPath);

        var mutations = new List<IMutation>();
        var occupiedCells = new HashSet<Point2D>();

        // Accept footprints in transform order so the same path wins when copies overlap.
        for (int i = 0; i < pathsToPlan.Count; i++)
        {
            var path = pathsToPlan[i];
            (DrawConnectedTilesMutation mutation, string failure) = CreatePathMutation(plans[i], path, occupiedCells);
            if (mutation != null)
                mutations.Add(mutation);
            else
                previewPaths[path.PreviewIndex] = new PathPreview(path.Vertices, failure);
        }

        if (mutations.Count == 0)
            return;

        previewMutation = mutations.Count == 1 ? mutations[0]
            : new MirroredPlacementMutation(MutationTarget, mutations, Mutation.EventIdNone);

        // Hit testing must see the original terrain under the temporary placements.
        foreach (Point2D cell in occupiedCells)
        {
            if (GetTerrain(cell) is ConnectedTileConnectionFinder.TerrainCell original)
                terrainUnderPreview.Add(cell, original);
        }

        previewMutation.Perform();
    }

    private ConnectedTileAnchor? GetMirroredAnchor(ConnectedTileAnchor? anchor, Point2D cell, MirrorTransform transform)
    {
        if (!anchor.HasValue || transform == default)
            return anchor;

        byte directionMask = 0;
        for (int i = 0; i < (int)Direction.Count; i++)
        {
            if ((anchor.Value.ConnectionPoint.ConnectionMask & (0b10000000 >> i)) == 0)
                continue;

            Point2D offset = transform.ApplyToOffset(Helpers.VisualDirectionToPoint((Direction)i));
            int direction = (int)Helpers.DirectionFromPoints(Point2D.Zero, offset);
            directionMask |= (byte)(0b10000000 >> direction);
        }

        return connectionFinder.FindOpenConnection(cell, directionMask);
    }

    private ConnectedTileSide GetStartingSide(Point2D[] vertices, MirrorTransform transform)
    {
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

        return side;
    }

    private (DrawConnectedTilesMutation Mutation, string Failure) CreatePathMutation(ConnectedTilePlanResult result,
        PathToPlan path, HashSet<Point2D> occupiedCells)
    {
        if (!result.IsSuccess)
            return (null, GetPlanFailureText(result));

        int? baseLevel = path.StartAnchor?.BaseLevel ?? path.EndAnchor?.BaseLevel;
        var mutation = new DrawConnectedTilesMutation(MutationTarget, result.Plan, randomSeed, extraHeight, baseLevel);

        // The planner uses configured foundations, which can differ from the subtiles actually written.
        var placementCells = mutation.GetPlacementCells().ToHashSet();
        if ((previewMirroring != null || IsAttached) && placementCells.Count == 0)
            return (null, Translate("PlanStatus.Empty", "no tiles could be placed"));

        if ((path.Transform != default || IsAttached) && placementCells.Any(cell => Map.GetTile(cell) == null))
            return (null, Translate("PlanStatus.OutOfBounds", "a path extends outside the map"));

        if (IsAttached && placementCells.Any(cell => connectionFinder.ContainsTile(Map.GetTile(cell).TileIndex)))
            return (null, Translate("PlanStatus.ConnectionOverlap", "a path overlaps existing connected tiles"));

        if (occupiedCells.Overlaps(placementCells))
            return (null, Translate("PlanStatus.Overlap", "a path overlaps another placement"));

        occupiedCells.UnionWith(placementCells);
        return (mutation, null);
    }

    private static byte GetFrontSeamDirections(ConnectedTilePlan plan)
    {
        // The initial side applies to the seam's connection into the first tile.
        // Reflections reverse the side relative to travel and rotations preserve it.
        ConnectedTilePlacement first = plan.Placements[0];
        bool entersEastOrSouth = PointsEastOrSouth(plan.Placements[^1].ExitCoordinates.Value, first.EntryCoordinates.Value);
        bool frontEntersEastOrSouth = (first.EntryPoint.Value.Side == ConnectedTileSide.Front) == entersEastOrSouth;

        return frontEntersEastOrSouth ? (byte)0b11110000 : (byte)0b00001111;
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

    private bool CanClosePath()
    {
        return !IsAttached && new HashSet<Point2D>(connectedTilePath).Count >= 3;
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
        terrainUnderPreview.Clear();
    }
}
