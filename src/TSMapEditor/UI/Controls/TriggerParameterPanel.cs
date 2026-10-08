using MapEditorLibrary;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TSMapEditor.UI.Controls;

/// <summary>
/// Displays trigger event and action parameters in a vertically scrollable panel.
/// </summary>
public class TriggerParameterPanel : XNAScrollPanel
{
    private const int LabelWidth = 160;
    private const int ButtonWidth = 30;
    private const int RowHeight = Constants.UIButtonHeight + Constants.UIVerticalSpacing;

    public TriggerParameterPanel(WindowManager windowManager) : base(windowManager)
    {
        AllowScroll = (false, true);
        AllowKeyboardInput = false;
        IncludeHiddenChildrenInContentSize = false;
        ScrollStep = RowHeight;
        ClientRectangleUpdated += (s, e) => LayoutRows();
    }

    private sealed class ParameterRow
    {
        public int ParameterIndex;
        public XNALabel Label;
        public EditorTextBox TextBox;
        public EditorButton PresetButton;
        public EditorButton TargetButton;
    }

    private readonly List<ParameterRow> rows = new List<ParameterRow>();
    private readonly List<ToolTip> toolTips = new List<ToolTip>();

    public IEnumerable<int> ParameterIndices => rows.Select(row => row.ParameterIndex);

    public EditorTextBox GetTextBox(int parameterIndex) => rows.First(row => row.ParameterIndex == parameterIndex).TextBox;

    public EditorTextBox AddParameter(int parameterIndex, string name, Action<int> selectPreset, Action<int> goToTarget)
    {
        var label = new XNALabel(WindowManager);
        label.Name = Name + "Label" + parameterIndex;
        label.Text = name;
        ContentPanel.AddChild(label);

        var textBox = new EditorTextBox(WindowManager);
        textBox.Name = Name + "Value" + parameterIndex;
        textBox.Tag = parameterIndex;
        ContentPanel.AddChild(textBox);

        var presetButton = new EditorButton(WindowManager);
        presetButton.Name = Name + "Preset" + parameterIndex;
        presetButton.Width = ButtonWidth;
        presetButton.Height = textBox.Height;
        presetButton.Text = Translate(this, "SelectPreset.Text", "...");
        presetButton.AllowClick = selectPreset != null;
        presetButton.LeftClick += (s, e) => selectPreset(parameterIndex);
        ContentPanel.AddChild(presetButton);

        var targetButton = new EditorButton(WindowManager);
        targetButton.Name = Name + "Target" + parameterIndex;
        targetButton.Width = ButtonWidth;
        targetButton.Height = textBox.Height;
        targetButton.Text = Translate(this, "GoToTarget.Text", "->");
        targetButton.AllowClick = goToTarget != null;
        targetButton.LeftClick += (s, e) => goToTarget(parameterIndex);
        ContentPanel.AddChild(targetButton);
        toolTips.Add(new ToolTip(WindowManager, targetButton) { Text = Translate(this, "GoToTarget.ToolTip", "Go To Target") });

        var row = new ParameterRow { ParameterIndex = parameterIndex, Label = label, TextBox = textBox,
            PresetButton = presetButton, TargetButton = targetButton };

        if (rows.Count > 0)
        {
            textBox.PreviousControl = rows[rows.Count - 1].TextBox;
            rows[rows.Count - 1].TextBox.NextControl = textBox;
        }

        rows.Add(row);

        textBox.SelectedChanged += (s, e) =>
        {
            if (WindowManager.SelectedControl == textBox)
                ScrollTo(new Rectangle(0, textBox.Y, ViewSize.X, Constants.UIButtonHeight));
        };

        return textBox;
    }

    public void LayoutRows()
    {
        bool scrollbarShown = RowHeight * rows.Count > Height;

        int right = Width - (scrollbarShown ? VerticalScrollBar.ScrollWidth + Constants.UIHorizontalSpacing : 0);
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            int y = i * RowHeight;
            row.Label.X = 0;
            row.Label.Y = y + (Constants.UITextBoxHeight - row.Label.Height) / 2;
            row.TextBox.X = LabelWidth + Constants.UIHorizontalSpacing;
            row.TextBox.Y = y + (Constants.UITextBoxHeight - row.TextBox.Height) / 2;
            row.TextBox.Width = right - row.TextBox.X - ButtonWidth * 2;
            row.PresetButton.X = row.TextBox.Right;
            row.PresetButton.Y = row.TextBox.Y;
            row.TargetButton.X = row.PresetButton.Right;
            row.TargetButton.Y = row.TextBox.Y;
        }
    }

    public override void Kill()
    {
        BackgroundTexture?.Dispose();
        ClearParameters();
        base.Kill();
    }

    public void ClearParameters()
    {
        foreach (var toolTip in toolTips)
        {
            toolTip.Disable();
            toolTip.Parent.RemoveChild(toolTip);
            toolTip.Kill();
        }

        toolTips.Clear();

        if (rows.Any(row => WindowManager.SelectedControl == row.TextBox))
            WindowManager.SelectedControl = null;

        foreach (var row in rows)
        {
            foreach (var control in new XNAControl[] { row.Label, row.TextBox, row.PresetButton, row.TargetButton })
            {
                control.Disable();
                ContentPanel.RemoveChild(control);
                control.Kill();
            }
        }

        rows.Clear();
        ScrollToTop();
    }

    protected override void DrawPanel()
    {
        base.DrawPanel();
    }
}
