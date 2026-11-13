using MapEditorLibrary.CCEngine;
using MapEditorLibrary.Models;
using MapEditorLibrary.Models.Enums;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TSMapEditor.UI.TriggerGraph;

internal sealed record TriggerGraphNode(object Target, string Name, string Id, Color? EditorColor = null)
{
    public bool IsVariable => Target is LocalVariable;
}

internal sealed record TriggerGraphEdge(TriggerGraphNode From, TriggerGraphNode To, IReadOnlyList<TriggerParameterReference?> References);

internal sealed class TriggerGraphModel
{
    private TriggerGraphModel(IReadOnlyList<TriggerGraphNode> nodes, IReadOnlyList<TriggerGraphEdge> edges)
    {
        Nodes = nodes;
        Edges = edges;
    }

    public IReadOnlyList<TriggerGraphNode> Nodes { get; }
    public IReadOnlyList<TriggerGraphEdge> Edges { get; }

    public static TriggerGraphModel Create(Map map)
    {
        var triggers = map.Triggers.ToDictionary(trigger => trigger,
            trigger => new TriggerGraphNode(trigger, trigger.Name, trigger.ID, trigger.EditorColor == null ? null : trigger.XNAColor));
        var variables = map.LocalVariables.ToDictionary(variable => variable.Index,
            variable => new TriggerGraphNode(variable, variable.Name, variable.Index.ToString(CultureInfo.InvariantCulture)));

        var triggersById = new Dictionary<string, TriggerGraphNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in triggers.Values)
            triggersById.TryAdd(node.Id, node);

        var tagsById = new Dictionary<string, Tag>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in map.Tags)
            tagsById.TryAdd(tag.ID, tag);

        var references = new Dictionary<(TriggerGraphNode From, TriggerGraphNode To), List<TriggerParameterReference?>>();
        foreach (var (trigger, source) in triggers)
        {
            if (trigger.LinkedTrigger != null && triggers.TryGetValue(trigger.LinkedTrigger, out var linked))
                AddReference(source, linked, null);

            foreach (var reference in TriggerParameterReferences.Enumerate(trigger, map.EditorConfig))
            {
                if (string.IsNullOrWhiteSpace(reference.Value))
                    continue;

                switch (reference.ParameterType)
                {
                    case TriggerParamType.Trigger:
                        if (triggersById.TryGetValue(reference.Value, out var target))
                            AddReference(source, target, reference);
                        break;
                    case TriggerParamType.Tag:
                        if (tagsById.TryGetValue(reference.Value, out var tag) && tag.Trigger != null &&
                            triggers.TryGetValue(tag.Trigger, out var taggedTrigger))
                        {
                            AddReference(source, taggedTrigger, reference);
                        }
                        break;
                    case TriggerParamType.LocalVariable:
                        if (int.TryParse(reference.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index) &&
                            variables.TryGetValue(index, out var variable))
                        {
                            if (reference.VariableAccess == TriggerVariableAccess.Read)
                                AddReference(variable, source, reference);
                            else
                                AddReference(source, variable, reference);
                        }
                        break;
                }
            }
        }

        var nodes = triggers.Values.OrderBy(node => node.Id, StringComparer.Ordinal)
            .Concat(variables.OrderBy(pair => pair.Key).Select(pair => pair.Value)).ToArray();
        var edges = references.Select(pair => new TriggerGraphEdge(pair.Key.From, pair.Key.To, pair.Value)).ToArray();

        return new TriggerGraphModel(nodes, edges);

        void AddReference(TriggerGraphNode from, TriggerGraphNode to, TriggerParameterReference? reference)
        {
            if (!references.TryGetValue((from, to), out var edgeReferences))
            {
                edgeReferences = new List<TriggerParameterReference?>();
                references.Add((from, to), edgeReferences);
            }

            edgeReferences.Add(reference);
        }
    }

    public TriggerGraphModel Focus(TriggerGraphNode center)
    {
        var visible = new HashSet<TriggerGraphNode> { center };
        foreach (var edge in Edges)
        {
            if (edge.From == center || edge.To == center)
            {
                visible.Add(edge.From);
                visible.Add(edge.To);
            }
        }

        return new TriggerGraphModel(Nodes.Where(visible.Contains).ToArray(),
            Edges.Where(edge => visible.Contains(edge.From) && visible.Contains(edge.To)).ToArray());
    }
}
