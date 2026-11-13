using MapEditorLibrary;
using MapEditorLibrary.CCEngine;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TSMapEditor.UI.TriggerGraph;

public sealed class TriggerGraphPanel : XNAScrollPanel
{
    public TriggerGraphPanel(WindowManager windowManager) : base(windowManager)
    {
        DrawBorders = true;
        AllowScroll = (true, true);
        HandlesDragging = true;
        canvas = new GraphCanvas(windowManager, this) { Name = "GraphCanvas", InputEnabled = false };
    }

    // XNAUI divides the Windows wheel delta of 120 per notch by 40.
    private const int WheelUnitsPerZoomStep = 3;
    private static readonly int[] ZoomLevels = [25, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160, 170, 180, 190, 200];

    private readonly GraphCanvas canvas;
    private int zoomIndex = Array.IndexOf(ZoomLevels, 100);
    private int wheelZoomUnits;
    private Point dragOrigin;
    private Point viewAtPress;
    private bool pointerDown;
    private bool movedSincePress;

    internal event Action<TriggerGraphNode> NodeClicked;
    internal event Action<TriggerGraphEdge> EdgeClicked;
    internal event Action<TriggerGraphNode, TriggerGraphEdge> HoverChanged;
    internal event Action ZoomChanged;
    internal event Action ClearSelectionRequested;

    internal int ZoomPercent => ZoomLevels[zoomIndex];
    internal bool CanZoomIn => zoomIndex < ZoomLevels.Length - 1;
    internal bool CanZoomOut => zoomIndex > 0;

    private float Zoom => ZoomPercent / 100f;
    private Point ViewCenter => new Point(ViewSize.X / 2, ViewSize.Y / 2);

    public override void Initialize()
    {
        base.Initialize();

        // The viewport owns input even when the graph only fills part of it.
        ContentPanel.InputEnabled = false;
        ContentPanel.AddChild(canvas);
        ViewPositionChanged += (_, _) => WindowManager.RequestRedraw();
    }

    public override void Draw(GameTime gameTime)
    {
        FillRectangle(new Rectangle(0, 0, Width, Height), UISettings.ActiveSettings.PanelBackgroundColor);
        base.Draw(gameTime);
    }

    internal void SetLayout(TriggerGraphLayout layout)
    {
        canvas.SetLayout(layout);
        CancelPan();
    }

    internal void ResetView() => CurrentViewPosition = Point.Zero;

    internal void StepZoom(int steps) => SetZoom(zoomIndex + steps, ViewCenter);

    internal void ResetZoom() => SetZoom(Array.IndexOf(ZoomLevels, 100), ViewCenter);

    public override void OnMouseLeftDown(InputEventArgs e)
    {
        base.OnMouseLeftDown(e);
        if (!IsCursorInViewport())
            return;

        dragOrigin = GetCursorPoint();
        viewAtPress = CurrentViewPosition;
        pointerDown = true;
        movedSincePress = false;
        e.Handled = true;
    }

    public override void OnLeftClick(InputEventArgs e)
    {
        e.Handled = true;
        if (movedSincePress || !IsCursorInViewport())
            return;

        canvas.UpdateHover();
        if (canvas.HoveredNode != null)
            NodeClicked?.Invoke(canvas.HoveredNode);
        else if (canvas.HoveredEdge != null)
            EdgeClicked?.Invoke(canvas.HoveredEdge);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (wheelZoomUnits != 0 && (!IsDirectlyActive || !IsCursorInViewport() || !Keyboard.IsCtrlHeldDown()))
            wheelZoomUnits = 0;

        if (pointerDown)
        {
            pointerDown = Cursor.LeftDown;

            Point delta = GetCursorPoint() - dragOrigin;
            movedSincePress |= Math.Abs(delta.X) + Math.Abs(delta.Y) > 5;
            if (movedSincePress)
            {
                CurrentViewPosition = viewAtPress - delta;
                canvas.ClearHover();
                return;
            }
        }

        canvas.UpdateHover();
    }

    private void CancelPan()
    {
        movedSincePress |= pointerDown;
        pointerDown = false;
        canvas.ClearHover();
    }

    public override void OnRightClick(InputEventArgs e)
    {
        if (IsCursorInViewport())
        {
            e.Handled = true;
            ClearSelectionRequested?.Invoke();
        }
        else
        {
            base.OnRightClick(e);
        }
    }

    public override void OnMouseScrolled(InputEventArgs e)
    {
        if (Keyboard.IsCtrlHeldDown() && IsCursorInViewport())
        {
            e.Handled = true;

            // Accumulate partial touchpad deltas and limit a coalesced event to one step.
            wheelZoomUnits += Math.Clamp(Cursor.ScrollWheelValue, -WheelUnitsPerZoomStep, WheelUnitsPerZoomStep);
            if (Math.Abs(wheelZoomUnits) >= WheelUnitsPerZoomStep)
            {
                int direction = Math.Sign(wheelZoomUnits);

                wheelZoomUnits -= direction * WheelUnitsPerZoomStep;
                SetZoom(zoomIndex + direction, GetCursorPoint());
            }
        }
        else
        {
            base.OnMouseScrolled(e);
        }
    }

    private bool IsCursorInViewport() => new Rectangle(Point.Zero, ViewSize).Contains(GetCursorPoint());

    private void SetZoom(int index, Point anchor)
    {
        index = Math.Clamp(index, 0, ZoomLevels.Length - 1);
        if (index == zoomIndex)
            return;

        Vector2 layoutPoint = (CurrentViewPosition + anchor).ToVector2() / Zoom;
        zoomIndex = index;
        canvas.ResizeForZoom();
        CurrentViewPosition = Scale(layoutPoint) - anchor;
        CancelPan();

        ZoomChanged?.Invoke();
        WindowManager.RequestRedraw();
    }

    private Point Scale(Vector2 point) => new Point((int)MathF.Round(point.X * Zoom), (int)MathF.Round(point.Y * Zoom));

    private Rectangle Scale(Rectangle bounds)
    {
        Point topLeft = Scale(bounds.Location.ToVector2());
        Point bottomRight = Scale(new Vector2(bounds.Right, bounds.Bottom));

        return new Rectangle(topLeft, bottomRight - topLeft);
    }

    internal void SelectNode(TriggerGraphNode node)
    {
        canvas.SelectedNode = node;
        if (node != null && canvas.Layout.Bounds.TryGetValue(node, out var bounds))
            CurrentViewPosition = Scale(bounds.Center.ToVector2()) - ViewCenter;

        WindowManager.RequestRedraw();
    }

    internal static string GetNodeCaption(TriggerGraphNode node)
    {
        string format = node.IsVariable
            ? Translate("TriggerGraphWindow.VariableId", "Local variable {0}")
            : Translate("TriggerGraphWindow.TriggerId", "Trigger {0}");

        return string.Format(format, node.Id);
    }

    private sealed class GraphCanvas : XNAControl
    {
        private static readonly Color TriggerColor = new Color(135, 192, 232);
        private static readonly Color VariableColor = new Color(255, 0, 200);
        private static readonly Color LinkColor = new Color(181, 154, 220);
        private static readonly Color ReadColor = new Color(115, 211, 196);
        private static readonly Color WriteColor = new Color(229, 162, 112);
        private static readonly Color CardBackground = new Color(24, 31, 39);
        private static readonly Color HoveredCardBackground = new Color(41, 52, 63);
        private static readonly Color SelectedCardBackground = new Color(62, 80, 98);

        public GraphCanvas(WindowManager windowManager, TriggerGraphPanel panel) : base(windowManager)
        {
            this.panel = panel;
        }

        private readonly TriggerGraphPanel panel;
        private readonly Dictionary<TriggerGraphNode, (string Name, string Caption)> labels = new();
        private readonly Dictionary<TriggerGraphEdge, Color> edgeColors = new();
        private readonly Dictionary<TriggerGraphNode, (int Top, int Bottom)> incomingPorts = new();
        private readonly Dictionary<TriggerGraphNode, (int Top, int Bottom)> outgoingPorts = new();
        private TriggerGraphPath hoveredPath;

        public TriggerGraphLayout Layout { get; private set; }
        public TriggerGraphNode SelectedNode { get; set; }
        public TriggerGraphNode HoveredNode { get; private set; }
        public TriggerGraphEdge HoveredEdge => hoveredPath?.Edge;

        public void SetLayout(TriggerGraphLayout layout)
        {
            Layout = layout;
            ResizeForZoom();
            labels.Clear();
            edgeColors.Clear();
            incomingPorts.Clear();
            outgoingPorts.Clear();

            foreach (var node in layout.Bounds.Keys)
            {
                const int LabelWidth = TriggerGraphLayout.NodeWidth - 20;
                labels.Add(node, (
                    Renderer.GetStringWithLimitedWidth(node.Name ?? string.Empty, Constants.UIBoldFont, LabelWidth),
                    Renderer.GetStringWithLimitedWidth(GetNodeCaption(node), Constants.UIDefaultFont, LabelWidth)));
            }

            foreach (var path in layout.Paths)
            {
                Color[] colors = path.Edge.References.Select(GetReferenceColor).Distinct().ToArray();
                edgeColors.Add(path.Edge, colors.Length == 1 ? colors[0] : UISettings.ActiveSettings.TextColor);
                AddPort(outgoingPorts, path.Edge.From, path.Points[0].Y);
                AddPort(incomingPorts, path.Edge.To, path.Points[^1].Y);
            }

            WindowManager.RequestRedraw();
        }

        private static void AddPort(Dictionary<TriggerGraphNode, (int Top, int Bottom)> ports, TriggerGraphNode node, int y)
        {
            ports[node] = ports.TryGetValue(node, out var extent) ? (Math.Min(extent.Top, y), Math.Max(extent.Bottom, y)) : (y, y);
        }

        private static Rectangle GetCardBounds(Rectangle bounds) =>
            new Rectangle(bounds.X, bounds.Center.Y - TriggerGraphLayout.NodeHeight / 2, bounds.Width, TriggerGraphLayout.NodeHeight);

        public void ResizeForZoom()
        {
            if (Layout == null)
                return;

            var size = Layout.Size.ToVector2() * panel.Zoom;
            ClientRectangle = new Rectangle(0, 0, (int)MathF.Ceiling(size.X), (int)MathF.Ceiling(size.Y));
        }

        public void ClearHover() => SetHover(null, null);

        public void UpdateHover()
        {
            if (Layout == null || !panel.IsDirectlyActive || !panel.IsCursorInViewport())
            {
                ClearHover();
                return;
            }

            Point cursor = panel.GetCursorPoint() + panel.CurrentViewPosition;

            foreach (var (node, bounds) in Layout.Bounds)
            {
                if (panel.Scale(GetCardBounds(bounds)).Contains(cursor))
                {
                    SetHover(node, null);
                    return;
                }
            }

            TriggerGraphPath closestPath = null;
            const int HitRadius = 5;
            float closestDistance = HitRadius;
            Vector2 point = cursor.ToVector2() / panel.Zoom;
            var hitArea = new Rectangle(cursor.X - HitRadius, cursor.Y - HitRadius, HitRadius * 2 + 1, HitRadius * 2 + 1);
            foreach (var path in Layout.Paths)
            {
                if (!panel.Scale(path.Bounds).Intersects(hitArea))
                    continue;

                for (int i = 1; i < path.Points.Length; i++)
                {
                    Point start = path.Points[i - 1];
                    Point end = path.Points[i];
                    float x = Math.Clamp(point.X, Math.Min(start.X, end.X), Math.Max(start.X, end.X));
                    float y = Math.Clamp(point.Y, Math.Min(start.Y, end.Y), Math.Max(start.Y, end.Y));
                    float distance = (Math.Abs(point.X - x) + Math.Abs(point.Y - y)) * panel.Zoom;
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPath = path;
                    }
                }
            }

            SetHover(null, closestPath);
        }

        private void SetHover(TriggerGraphNode node, TriggerGraphPath path)
        {
            if (node == HoveredNode && path == hoveredPath)
                return;

            HoveredNode = node;
            hoveredPath = path;
            panel.HoverChanged?.Invoke(node, path?.Edge);
            WindowManager.RequestRedraw();
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            if (Layout == null)
                return;

            var viewport = panel.CurrentViewRectangle;
            var highlightedNode = HoveredNode ?? SelectedNode;
            int lineWidth = Math.Max(1, (int)MathF.Round(panel.Zoom));
            foreach (var path in Layout.Paths)
            {
                if (path == hoveredPath || IsConnected(path, highlightedNode) || !viewport.Intersects(panel.Scale(path.Bounds)))
                    continue;

                float opacity = hoveredPath == null && highlightedNode == null ? 0.75f : 0.28f;
                DrawPath(path, edgeColors[path.Edge] * opacity, lineWidth);
            }

            if (highlightedNode != null)
            {
                foreach (var path in Layout.Paths)
                {
                    if (path != hoveredPath && IsConnected(path, highlightedNode) && viewport.Intersects(panel.Scale(path.Bounds)))
                        DrawPath(path, edgeColors[path.Edge], lineWidth + 1);
                }
            }

            if (hoveredPath != null && viewport.Intersects(panel.Scale(hoveredPath.Bounds)))
                DrawPath(hoveredPath, edgeColors[hoveredPath.Edge], lineWidth + 2);

            foreach (var (node, bounds) in Layout.Bounds)
            {
                var scaledBounds = panel.Scale(bounds);
                if (viewport.Intersects(scaledBounds))
                    DrawNode(node, bounds);
            }
        }

        private static bool IsConnected(TriggerGraphPath path, TriggerGraphNode node) =>
            path.Edge.From == node || path.Edge.To == node;

        private void DrawPath(TriggerGraphPath path, Color color, int thickness)
        {
            for (int i = 1; i < path.Points.Length; i++)
            {
                if (path.Points[i - 1] != path.Points[i])
                    DrawLine(path.Points[i - 1].ToVector2() * panel.Zoom, path.Points[i].ToVector2() * panel.Zoom, color, thickness);
            }

            Vector2 tip = (path.Points[^1].ToVector2() - Vector2.UnitX) * panel.Zoom;
            DrawLine(tip, tip + new Vector2(-7, -4) * panel.Zoom, color, thickness);
            DrawLine(tip, tip + new Vector2(-7, 4) * panel.Zoom, color, thickness);
        }

        private void DrawNode(TriggerGraphNode node, Rectangle bounds)
        {
            var card = GetCardBounds(bounds);
            var scaledBounds = panel.Scale(card);
            bool isSelected = node == SelectedNode;
            Color accent = node.EditorColor ?? (node.IsVariable ? VariableColor : TriggerColor);
            Color background = isSelected ? SelectedCardBackground : node == HoveredNode ? HoveredCardBackground : CardBackground;
            DrawPortRail(incomingPorts, node, card, card.Left, accent);
            DrawPortRail(outgoingPorts, node, card, card.Right, accent);

            FillRectangle(scaledBounds, background);
            DrawRectangle(scaledBounds, isSelected ? Color.White : accent, isSelected && panel.ZoomPercent >= 67 ? 2 : 1);

            int accentWidth = Math.Max(1, (int)MathF.Round(3 * panel.Zoom));
            FillRectangle(new Rectangle(scaledBounds.X + 1, scaledBounds.Y + 1, accentWidth, scaledBounds.Height - 2), accent);

            if (panel.ZoomPercent < 67)
                return;

            var (name, caption) = labels[node];
            int nameOffset = panel.ZoomPercent >= 80 ? -18 : -8;

            // Keep the configured color on the card accent and lighten dark palette colors for text.
            Color nameColor = node.EditorColor is Color editorColor ? Color.Lerp(editorColor, Color.White, 0.65f) : UISettings.ActiveSettings.TextColor;
            DrawStringWithShadow(name, Constants.UIBoldFont, new Vector2(bounds.X + 10, bounds.Center.Y + nameOffset) * panel.Zoom,
                nameColor, panel.Zoom);

            if (panel.ZoomPercent >= 80)
            {
                Color captionColor = node.IsVariable ? VariableColor : TriggerColor;
                DrawStringWithShadow(caption, Constants.UIDefaultFont, new Vector2(bounds.X + 10, bounds.Center.Y + 2) * panel.Zoom,
                    captionColor, panel.Zoom);
            }
        }

        private void DrawPortRail(Dictionary<TriggerGraphNode, (int Top, int Bottom)> ports, TriggerGraphNode node,
            Rectangle card, int x, Color accent)
        {
            if (!ports.TryGetValue(node, out var extent))
                return;

            Color color = accent * (node == HoveredNode || node == SelectedNode ? 1f : 0.65f);
            if (extent.Top < card.Top)
                DrawLine(new Vector2(x, extent.Top) * panel.Zoom, new Vector2(x, card.Top) * panel.Zoom, color);

            if (extent.Bottom > card.Bottom)
                DrawLine(new Vector2(x, card.Bottom) * panel.Zoom, new Vector2(x, extent.Bottom) * panel.Zoom, color);
        }

        private static Color GetReferenceColor(TriggerParameterReference? reference)
        {
            if (!reference.HasValue)
                return LinkColor;

            return reference.Value.VariableAccess switch
            {
                TriggerVariableAccess.Read => ReadColor,
                TriggerVariableAccess.Write => WriteColor,
                _ => TriggerColor
            };
        }
    }
}
