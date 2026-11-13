using MapEditorLibrary;
using MapEditorLibrary.CCEngine;
using MapEditorLibrary.Models;
using MapEditorLibrary.Models.Enums;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Linq;
using TSMapEditor.UI.Controls;
using TSMapEditor.UI.TriggerGraph;

namespace TSMapEditor.UI.Windows;

public sealed class TriggerGraphWindow : INItializableWindow
{
    public TriggerGraphWindow(WindowManager windowManager, Map map, ReferenceListWindow referenceListWindow, Action<object> navigateToReference)
        : base(windowManager)
    {
        this.map = map;
        this.referenceListWindow = referenceListWindow;
        this.navigateToReference = navigateToReference;
    }

    private readonly Map map;
    private readonly ReferenceListWindow referenceListWindow;
    private readonly Action<object> navigateToReference;

    private TriggerGraphPanel panelGraph;
    private EditorSuggestionTextBox tbSearch;
    private EditorListBox lbNodes;
    private XNACheckBox chkFocus;
    private EditorButton btnOpenSelected;
    private EditorButton btnZoomOut;
    private EditorButton btnResetZoom;
    private EditorButton btnZoomIn;
    private XNALabel lblStatus;
    private TriggerGraphModel graph;
    private TriggerGraphLayout overview;
    private TriggerGraphNode selectedNode;
    private TriggerGraphNode hoveredNode;
    private TriggerGraphEdge hoveredEdge;

    public override void Initialize()
    {
        Name = nameof(TriggerGraphWindow);
        base.Initialize();

        panelGraph = FindChild<TriggerGraphPanel>(nameof(panelGraph));
        tbSearch = FindChild<EditorSuggestionTextBox>(nameof(tbSearch));
        lbNodes = FindChild<EditorListBox>(nameof(lbNodes));
        chkFocus = FindChild<XNACheckBox>(nameof(chkFocus));
        btnOpenSelected = FindChild<EditorButton>(nameof(btnOpenSelected));
        btnZoomOut = FindChild<EditorButton>(nameof(btnZoomOut));
        btnResetZoom = FindChild<EditorButton>(nameof(btnResetZoom));
        btnZoomIn = FindChild<EditorButton>(nameof(btnZoomIn));
        lblStatus = FindChild<XNALabel>(nameof(lblStatus));

        FindChild<EditorButton>("btnRefresh").LeftClick += (_, _) => RebuildGraph();
        btnZoomOut.LeftClick += (_, _) => panelGraph.StepZoom(-1);
        btnResetZoom.LeftClick += (_, _) => panelGraph.ResetZoom();
        btnZoomIn.LeftClick += (_, _) => panelGraph.StepZoom(1);
        panelGraph.ZoomChanged += UpdateZoomControls;
        UpdateZoomControls();

        btnOpenSelected.LeftClick += (_, _) => OpenSelected();
        lbNodes.DoubleLeftClick += (_, _) => OpenSelected();
        lbNodes.SelectedIndexChanged += LbNodes_SelectedIndexChanged;
        tbSearch.TextChanged += TbSearch_TextChanged;
        chkFocus.CheckedChanged += (_, _) => DisplaySelection(updateLayout: true);
        panelGraph.NodeClicked += node => navigateToReference(node.Target);
        panelGraph.EdgeClicked += ShowEdgeReferences;
        panelGraph.ClearSelectionRequested += () => lbNodes.SelectedIndex = -1;
        panelGraph.HoverChanged += (node, edge) =>
        {
            hoveredNode = node;
            hoveredEdge = edge;
            UpdateStatus();
        };
        panelGraph.ClientRectangleUpdated += (_, _) => UpdateStatus();
    }

    public void Open()
    {
        RebuildGraph();
        Show();
    }

    private void RebuildGraph()
    {
        graph = TriggerGraphModel.Create(map);
        overview = TriggerGraphLayout.Create(graph);
        selectedNode = graph.Nodes.FirstOrDefault(node => ReferenceEquals(node.Target, selectedNode?.Target));
        FilterNodes(selectFirstMatch: false);
        DisplaySelection(updateLayout: true);
        if (selectedNode == null)
            panelGraph.ResetView();
    }

    private void FilterNodes(bool selectFirstMatch)
    {
        if (graph == null)
            return;

        string query = tbSearch.Text == tbSearch.Suggestion ? string.Empty : tbSearch.Text.Trim();
        lbNodes.SelectedIndexChanged -= LbNodes_SelectedIndexChanged;
        lbNodes.Clear();

        foreach (var node in graph.Nodes.OrderBy(node => node.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            if (query.Length > 0 && node.Name?.Contains(query, StringComparison.CurrentCultureIgnoreCase) != true &&
                !node.Id.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string text = string.Format(Translate(this, "SearchResult", "{0} ({1})"), node.Name, TriggerGraphPanel.GetNodeCaption(node));
            lbNodes.AddItem(new XNAListBoxItem { Text = text, Tag = node, TextColor = node.EditorColor ?? lbNodes.DefaultItemColor });
        }

        int selectedIndex = lbNodes.Items.FindIndex(item => ReferenceEquals(item.Tag, selectedNode));
        if (selectedIndex < 0 && selectFirstMatch && query.Length > 0 && lbNodes.Items.Count > 0)
            selectedIndex = 0;

        lbNodes.SelectedIndex = selectedIndex;
        lbNodes.SelectedIndexChanged += LbNodes_SelectedIndexChanged;
        if (selectedIndex >= 0)
            lbNodes.ScrollToSelectedElement();

        selectedNode = lbNodes.SelectedItem?.Tag as TriggerGraphNode;
    }

    private void TbSearch_TextChanged(object sender, EventArgs e)
    {
        var previousSelection = selectedNode;
        FilterNodes(selectFirstMatch: true);

        if (selectedNode != previousSelection)
            DisplaySelection(updateLayout: chkFocus.Checked);
        else
            UpdateStatus();
    }

    private void LbNodes_SelectedIndexChanged(object sender, EventArgs e)
    {
        selectedNode = lbNodes.SelectedItem?.Tag as TriggerGraphNode;
        DisplaySelection(updateLayout: chkFocus.Checked);
    }

    private void DisplaySelection(bool updateLayout)
    {
        if (graph == null)
            return;

        btnOpenSelected.Enabled = selectedNode != null;
        if (updateLayout)
        {
            var layout = chkFocus.Checked && selectedNode != null
                ? TriggerGraphLayout.Create(graph.Focus(selectedNode))
                : overview;

            panelGraph.SetLayout(layout);
        }

        panelGraph.SelectNode(selectedNode);
        UpdateStatus();
    }

    private void OpenSelected()
    {
        if (selectedNode != null)
            navigateToReference(selectedNode.Target);
    }

    private void UpdateZoomControls()
    {
        btnZoomOut.Enabled = panelGraph.CanZoomOut;
        btnZoomIn.Enabled = panelGraph.CanZoomIn;
        btnResetZoom.Text = string.Format(Translate(this, "ZoomPercent", "{0}%"), panelGraph.ZoomPercent);
    }

    private void UpdateStatus()
    {
        if (graph == null)
            return;

        string text;
        if (graph.Nodes.Count == 0)
        {
            text = Translate(this, "Empty", "This map has no triggers or local variables.");
        }
        else if (hoveredEdge != null)
        {
            text = string.Format(Translate(this, "EdgeStatus", "{0} to {1} ({2})"),
                hoveredEdge.From.Name, hoveredEdge.To.Name, DescribeReference(hoveredEdge.References[0]));
        }
        else if (hoveredNode != null)
        {
            text = string.Format(Translate(this, "SearchResult", "{0} ({1})"), hoveredNode.Name, TriggerGraphPanel.GetNodeCaption(hoveredNode));
        }
        else if (lbNodes.Items.Count == 0)
        {
            text = Translate(this, "NoSearchResults", "No triggers or local variables match the search.");
        }
        else
        {
            text = Translate(this, "Usage.v3",
                "Drag to pan. Ctrl+wheel to zoom. Click a node or line to open it. Right-click to clear selection.");
        }

        lblStatus.Text = Renderer.GetStringWithLimitedWidth(text, lblStatus.FontIndex, panelGraph.Width);
    }

    private void ShowEdgeReferences(TriggerGraphEdge edge)
    {
        string sourceType = Translate(this, "Connection", "connection");
        string connection = string.Format(Translate(this, "ConnectionEndpoints", "{0} to {1}"), edge.From.Name, edge.To.Name);
        var references = edge.References.Select(reference =>
        {
            object target = reference?.VariableAccess == TriggerVariableAccess.Read ? edge.To.Target : edge.From.Target;
            return (DescribeReference(reference), target);
        }).ToArray();

        referenceListWindow.Open(sourceType, connection, references);
    }

    private string DescribeReference(TriggerParameterReference? reference)
    {
        if (!reference.HasValue)
            return Translate(this, "LinkedTrigger", "Linked trigger");

        var parameter = reference.Value;
        string entryFormat = parameter.IsEvent
            ? Translate(this, "EventReference", "Event #{0}, {1}")
            : Translate(this, "ActionReference", "Action #{0}, {1}");

        string entry = string.Format(entryFormat, parameter.EntryIndex + 1, parameter.EntryName);
        return parameter.VariableAccess switch
        {
            TriggerVariableAccess.Read => string.Format(Translate(this, "ReadReference", "{0} (reads local variable)"), entry),
            TriggerVariableAccess.Write => string.Format(Translate(this, "WriteReference", "{0} (writes local variable)"), entry),
            _ when parameter.ParameterType == TriggerParamType.Tag =>
                string.Format(Translate(this, "TagReference", "{0} (via tag {1})"), entry, parameter.Value),
            _ => entry
        };
    }
}
