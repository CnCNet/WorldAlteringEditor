using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TSMapEditor.UI.TriggerGraph;

internal sealed record TriggerGraphPath(TriggerGraphEdge Edge, Point[] Points, Rectangle Bounds);

/// <summary>
/// Lays out each connected group separately. Edges that span columns or close a cycle use lanes outside the node area.
/// </summary>
internal sealed class TriggerGraphLayout
{
    public const int NodeWidth = 214;
    public const int NodeHeight = 48;

    private const int Margin = 24;
    private const int RowGap = 26;
    private const int MinimumColumnGap = 84;
    private const int LaneSpacing = 10;
    private const int BendMargin = 16;
    private const int PortSpacing = 8;
    private const int PortMargin = 10;

    private sealed class VerticalSegment(int fromY, int toY)
    {
        public int FromY { get; } = fromY;
        public int ToY { get; } = toY;
        public int X { get; set; }
    }

    private readonly Dictionary<TriggerGraphNode, Rectangle> bounds = new Dictionary<TriggerGraphNode, Rectangle>();
    private readonly List<TriggerGraphPath> paths = new List<TriggerGraphPath>();

    public IReadOnlyDictionary<TriggerGraphNode, Rectangle> Bounds => bounds;
    public IReadOnlyList<TriggerGraphPath> Paths => paths;
    public Point Size { get; private set; }

    public static TriggerGraphLayout Create(TriggerGraphModel graph)
    {
        var layout = new TriggerGraphLayout();
        var outgoing = graph.Nodes.ToDictionary(node => node, _ => new List<TriggerGraphEdge>());
        var incoming = graph.Nodes.ToDictionary(node => node, _ => new List<TriggerGraphEdge>());
        var order = graph.Nodes.Select((node, index) => (node, index)).ToDictionary(pair => pair.node, pair => pair.index);

        foreach (var edge in graph.Edges.OrderBy(edge => order[edge.From]).ThenBy(edge => order[edge.To]))
        {
            outgoing[edge.From].Add(edge);
            incoming[edge.To].Add(edge);
        }

        var visited = new HashSet<TriggerGraphNode>();
        var isolated = new List<TriggerGraphNode>();
        int width = 0;
        int y = Margin;

        foreach (var node in graph.Nodes)
        {
            if (!visited.Add(node))
                continue;

            if (incoming[node].Count == 0 && outgoing[node].Count == 0)
            {
                isolated.Add(node);
                continue;
            }

            var group = new List<TriggerGraphNode> { node };
            for (int i = 0; i < group.Count; i++)
            {
                foreach (var edge in outgoing[group[i]])
                {
                    if (visited.Add(edge.To))
                        group.Add(edge.To);
                }

                foreach (var edge in incoming[group[i]])
                {
                    if (visited.Add(edge.From))
                        group.Add(edge.From);
                }
            }

            group.Sort((a, b) => order[a].CompareTo(order[b]));
            var size = layout.PlaceGroup(group, outgoing, incoming, y);
            width = Math.Max(width, size.X);
            y += size.Y + Margin;
        }

        if (isolated.Count > 0)
        {
            int columns = Math.Min(4, isolated.Count);
            for (int i = 0; i < isolated.Count; i++)
            {
                int x = Margin + i % columns * (NodeWidth + Margin);
                layout.bounds.Add(isolated[i], new Rectangle(x, y + i / columns * (NodeHeight + RowGap), NodeWidth, NodeHeight));
            }

            width = Math.Max(width, columns * (NodeWidth + Margin) - Margin);
            y += ((isolated.Count - 1) / columns) * (NodeHeight + RowGap) + NodeHeight + Margin;
        }

        layout.Size = new Point(width + Margin * 2, y);
        return layout;
    }

    private Point PlaceGroup(List<TriggerGraphNode> group, Dictionary<TriggerGraphNode, List<TriggerGraphEdge>> outgoing,
        Dictionary<TriggerGraphNode, List<TriggerGraphEdge>> incoming, int top)
    {
        var ranks = RankNodes(group, outgoing);
        var columns = group.GroupBy(node => ranks[node]).OrderBy(column => column.Key).Select(column => column.ToList()).ToArray();

        OrderColumns(columns, ranks, outgoing, incoming);

        var heights = group.ToDictionary(node => node, node => Math.Max(NodeHeight,
            2 * PortMargin + (Math.Max(incoming[node].Count, outgoing[node].Count) - 1) * PortSpacing));
        int[] columnHeights = columns.Select(column => column.Sum(node => heights[node] + RowGap) - RowGap).ToArray();
        int nodeAreaHeight = columnHeights.Max();
        for (int rank = 0; rank < columns.Length; rank++)
        {
            int y = (nodeAreaHeight - columnHeights[rank]) / 2;
            foreach (var node in columns[rank])
            {
                bounds.Add(node, new Rectangle(0, y, NodeWidth, heights[node]));
                y += heights[node] + RowGap;
            }
        }

        var sourcePorts = AssignPorts(group, outgoing, useSource: true);
        var targetPorts = AssignPorts(group, incoming, useSource: false);
        var edges = group.SelectMany(node => outgoing[node]).ToArray();

        var upper = new List<(TriggerGraphEdge Edge, int Start, int End)>();
        var lower = new List<(TriggerGraphEdge Edge, int Start, int End)>();
        foreach (var edge in edges)
        {
            int from = ranks[edge.From];
            int to = ranks[edge.To];
            if (to == from + 1)
                continue;

            // Put long and return edges on the side closer to their ports.
            var interval = (edge, Math.Min(from, to), Math.Max(from, to));
            if (sourcePorts[edge] + targetPorts[edge] < nodeAreaHeight)
                upper.Add(interval);
            else
                lower.Add(interval);
        }

        var outerY = new Dictionary<TriggerGraphEdge, int>();
        int upperCount = AssignLanes(upper, clearance: 1, (edge, lane) => outerY.Add(edge, -BendMargin - lane * LaneSpacing));
        int lowerCount = AssignLanes(lower, clearance: 1, (edge, lane) => outerY.Add(edge, nodeAreaHeight + BendMargin + lane * LaneSpacing));
        int nodeTop = top + OuterLaneHeight(upperCount);

        var gaps = Enumerable.Range(0, columns.Length + 1).Select(_ => new List<VerticalSegment>()).ToArray();
        var routes = new List<(TriggerGraphEdge Edge, VerticalSegment First, VerticalSegment Last)>();
        foreach (var edge in edges)
        {
            int fromY = sourcePorts[edge];
            int toY = targetPorts[edge];
            int fromRank = ranks[edge.From];
            int toRank = ranks[edge.To];
            if (outerY.TryGetValue(edge, out int laneY))
            {
                var first = new VerticalSegment(fromY, laneY);
                var last = new VerticalSegment(laneY, toY);
                gaps[fromRank + 1].Add(first);
                gaps[toRank].Add(last);
                routes.Add((edge, first, last));
            }
            else
            {
                var segment = new VerticalSegment(fromY, toY);
                gaps[toRank].Add(segment);
                routes.Add((edge, segment, segment));
            }
        }

        int x = Margin;
        for (int gapIndex = 0; gapIndex < gaps.Length; gapIndex++)
        {
            // Upward and downward runs fan out in opposite orders to reduce crossings near shared nodes.
            var upward = new List<(VerticalSegment Segment, int Start, int End)>();
            var downward = new List<(VerticalSegment Segment, int Start, int End)>();
            foreach (var segment in gaps[gapIndex])
            {
                if (segment.FromY >= segment.ToY)
                    upward.Add((segment, segment.ToY, segment.FromY));
                else
                    downward.Add((segment, segment.FromY, segment.ToY));
            }

            int upwardCount = AssignLanes(upward, LaneSpacing, (segment, lane) => segment.X = lane * LaneSpacing);
            int downwardCount = AssignLanes(downward, LaneSpacing, (segment, lane) => segment.X = lane * LaneSpacing);
            int count = upwardCount + downwardCount;

            int laneWidth = Math.Max(0, count - 1) * LaneSpacing;
            int minimumGap = gapIndex == 0 || gapIndex == columns.Length ? 0 : MinimumColumnGap;
            int gapWidth = Math.Max(minimumGap, count == 0 ? 0 : 2 * BendMargin + laneWidth);
            int firstLaneX = x + (gapWidth - laneWidth) / 2;

            foreach (var (segment, _, _) in upward)
                segment.X += firstLaneX;

            foreach (var (segment, _, _) in downward)
                segment.X = firstLaneX + laneWidth - segment.X;

            x += gapWidth;
            if (gapIndex == columns.Length)
                break;

            foreach (var node in columns[gapIndex])
            {
                var rect = bounds[node];
                rect.Offset(x, nodeTop);
                bounds[node] = rect;
            }

            x += NodeWidth;
        }

        foreach (var (edge, first, last) in routes)
        {
            int fromY = nodeTop + first.FromY;
            int toY = nodeTop + last.ToY;
            var from = new Point(bounds[edge.From].Right, fromY);
            var to = new Point(bounds[edge.To].Left, toY);

            if (ReferenceEquals(first, last))
            {
                AddPath(edge, [from, new Point(first.X, fromY), new Point(first.X, toY), to]);
            }
            else
            {
                int laneY = nodeTop + first.ToY;
                AddPath(edge, [from, new Point(first.X, fromY), new Point(first.X, laneY), new Point(last.X, laneY), new Point(last.X, toY), to]);
            }
        }

        return new Point(x - Margin, nodeAreaHeight + OuterLaneHeight(upperCount) + OuterLaneHeight(lowerCount));
    }

    private static Dictionary<TriggerGraphNode, int> RankNodes(List<TriggerGraphNode> group,
        Dictionary<TriggerGraphNode, List<TriggerGraphEdge>> outgoing)
    {
        var discovered = new HashSet<TriggerGraphNode>();
        var active = new HashSet<TriggerGraphNode>();
        var backEdges = new HashSet<TriggerGraphEdge>();
        var finished = new List<TriggerGraphNode>();
        var stack = new Stack<(TriggerGraphNode Node, int NextEdge)>();

        foreach (var root in group)
        {
            if (!discovered.Add(root))
                continue;

            active.Add(root);
            stack.Push((root, 0));

            while (stack.TryPop(out var entry))
            {
                var (node, nextEdge) = entry;
                if (nextEdge == outgoing[node].Count)
                {
                    active.Remove(node);
                    finished.Add(node);
                    continue;
                }

                var edge = outgoing[node][nextEdge];
                stack.Push((node, nextEdge + 1));

                if (active.Contains(edge.To))
                {
                    backEdges.Add(edge);
                }
                else if (discovered.Add(edge.To))
                {
                    active.Add(edge.To);
                    stack.Push((edge.To, 0));
                }
            }
        }

        var ranks = group.ToDictionary(node => node, _ => 0);
        for (int i = finished.Count - 1; i >= 0; i--)
        {
            var node = finished[i];
            foreach (var edge in outgoing[node])
            {
                if (!backEdges.Contains(edge))
                    ranks[edge.To] = Math.Max(ranks[edge.To], ranks[node] + 1);
            }
        }

        return ranks;
    }

    private static void OrderColumns(List<TriggerGraphNode>[] columns, Dictionary<TriggerGraphNode, int> ranks,
        Dictionary<TriggerGraphNode, List<TriggerGraphEdge>> outgoing, Dictionary<TriggerGraphNode, List<TriggerGraphEdge>> incoming)
    {
        var positions = new Dictionary<TriggerGraphNode, double>();
        foreach (var column in columns)
            UpdatePositions(column);

        // Alternating sweeps put nodes near the average position of their neighbors in earlier or later columns.
        for (int pass = 0; pass < 4; pass++)
        {
            bool forward = pass % 2 == 0;
            for (int step = 0; step < columns.Length; step++)
            {
                int rank = forward ? step : columns.Length - 1 - step;
                columns[rank] = columns[rank].OrderBy(node => NeighborPosition(node, rank, forward)).ToList();
                UpdatePositions(columns[rank]);
            }
        }

        double NeighborPosition(TriggerGraphNode node, int rank, bool forward)
        {
            var neighbors = forward
                ? incoming[node].Select(edge => edge.From).Where(other => ranks[other] < rank)
                : outgoing[node].Select(edge => edge.To).Where(other => ranks[other] > rank);

            return neighbors.Select(other => positions[other]).DefaultIfEmpty(positions[node]).Average();
        }

        void UpdatePositions(List<TriggerGraphNode> column)
        {
            for (int i = 0; i < column.Count; i++)
                positions[column[i]] = (i + 0.5) / column.Count;
        }
    }

    // Reuse a lane once the previous segment has cleared it. Only overlapping segments need more room.
    private static int AssignLanes<T>(IEnumerable<(T Item, int Start, int End)> intervals, int clearance, Action<T, int> assign)
    {
        var occupied = new PriorityQueue<int, int>();
        var available = new PriorityQueue<int, int>();
        int count = 0;

        foreach (var (item, start, end) in intervals.OrderBy(interval => interval.Start).ThenBy(interval => interval.End))
        {
            while (occupied.TryPeek(out _, out int previousEnd) && previousEnd + clearance <= start)
            {
                int released = occupied.Dequeue();
                available.Enqueue(released, released);
            }

            int lane = available.Count > 0 ? available.Dequeue() : count++;
            occupied.Enqueue(lane, end);
            assign(item, lane);
        }

        return count;
    }

    private static int OuterLaneHeight(int count) =>
        count == 0 ? 0 : BendMargin + (count - 1) * LaneSpacing;

    private Dictionary<TriggerGraphEdge, int> AssignPorts(List<TriggerGraphNode> group,
        Dictionary<TriggerGraphNode, List<TriggerGraphEdge>> connections, bool useSource)
    {
        var ports = new Dictionary<TriggerGraphEdge, int>();
        foreach (var node in group)
        {
            var edges = connections[node].OrderBy(edge => bounds[useSource ? edge.To : edge.From].Center.Y).ToArray();
            int y = bounds[node].Center.Y - (edges.Length - 1) * PortSpacing / 2;
            foreach (var edge in edges)
            {
                ports.Add(edge, y);
                y += PortSpacing;
            }
        }

        return ports;
    }

    private void AddPath(TriggerGraphEdge edge, Point[] points)
    {
        int left = points.Min(point => point.X);
        int top = points.Min(point => point.Y);
        var pathBounds = new Rectangle(left, top, points.Max(point => point.X) - left + 1, points.Max(point => point.Y) - top + 1);
        pathBounds.Inflate(6, 6);
        paths.Add(new TriggerGraphPath(edge, points, pathBounds));
    }
}
