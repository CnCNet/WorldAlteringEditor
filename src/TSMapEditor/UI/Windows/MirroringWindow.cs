using MapEditorLibrary.GameMath;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.Rendering;
using TSMapEditor.UI.Controls;
using TSMapEditor.UI.CursorActions;
using TSMapEditor.UI.Notifications;

namespace TSMapEditor.UI.Windows;

public class MirroringWindow : INItializableWindow
{
    public MirroringWindow(WindowManager windowManager, EditorState editorState, ICursorActionTarget cursorActionTarget,
        INotificationManager notificationManager) : base(windowManager)
    {
        this.editorState = editorState;
        this.cursorActionTarget = cursorActionTarget;
        this.notificationManager = notificationManager;
        selectCellAction = new SelectCellCursorAction(cursorActionTarget);
    }

    private readonly EditorState editorState;
    private readonly ICursorActionTarget cursorActionTarget;
    private readonly INotificationManager notificationManager;
    private readonly SelectCellCursorAction selectCellAction;

    private (XNACheckBox CheckBox, MirrorAxes Axis)[] options;
    private EditorNumberTextBox tbXOffset;
    private EditorNumberTextBox tbYOffset;
    private XNALabel lblOffsetHint;
    private EditorButton applyButton;

    private Color offsetHintColor;
    private CursorAction previousCursorAction;

    public override void Initialize()
    {
        Name = nameof(MirroringWindow);
        base.Initialize();

        options = new[]
        {
            (FindChild<XNACheckBox>("chkNorthSouth"), MirrorAxes.NorthSouth),
            (FindChild<XNACheckBox>("chkEastWest"), MirrorAxes.EastWest),
            (FindChild<XNACheckBox>("chkNorthEastSouthWest"), MirrorAxes.NorthEastSouthWest),
            (FindChild<XNACheckBox>("chkNorthWestSouthEast"), MirrorAxes.NorthWestSouthEast),
            (FindChild<XNACheckBox>("chkHalfTurn"), MirrorAxes.HalfTurn)
        };

        tbXOffset = FindChild<EditorNumberTextBox>(nameof(tbXOffset));
        tbYOffset = FindChild<EditorNumberTextBox>(nameof(tbYOffset));
        tbXOffset.DoubleDefaultValue = double.NaN;
        tbYOffset.DoubleDefaultValue = double.NaN;
        lblOffsetHint = FindChild<XNALabel>(nameof(lblOffsetHint));
        offsetHintColor = lblOffsetHint.TextColor;

        selectCellAction.CellSelected += MirroringPointSelected;
        FindChild<EditorButton>("btnSelectMirroringPoint").LeftClick += SelectMirroringPoint;
        FindChild<EditorButton>("btnCenterOfMap").LeftClick += (_, _) => SetOffsets(Point2D.Zero);

        applyButton = FindChild<EditorButton>("btnApply");
        applyButton.LeftClick += Apply;
        FindChild<EditorButton>("btnCancel").LeftClick += (_, _) => Hide();
    }

    public void Open()
    {
        if (editorState.CursorAction == selectCellAction)
        {
            selectCellAction.ExitAction();
            return;
        }

        foreach (var option in options)
            option.CheckBox.Checked = editorState.Mirroring.Axes.HasFlag(option.Axis);

        SetOffsets(editorState.Mirroring.OffsetInHalfTiles);
        Show();
    }

    private MirrorAxes GetSelectedAxes()
    {
        MirrorAxes axes = MirrorAxes.None;
        foreach (var (checkBox, axis) in options)
        {
            if (checkBox.Checked)
                axes |= axis;
        }

        return axes;
    }

    private void SetOffsets(Point2D offset)
    {
        tbXOffset.DoubleValue = offset.X / 2.0;
        tbYOffset.DoubleValue = offset.Y / 2.0;
    }

    private bool TryGetOffsets(out Point2D offset)
    {
        bool validX = TryReadOffset(tbXOffset, out int x);
        bool validY = TryReadOffset(tbYOffset, out int y);
        offset = new Point2D(x, y);
        return validX && validY;
    }

    private static bool TryReadOffset(EditorNumberTextBox textBox, out int halfTiles)
    {
        halfTiles = 0;
        if (string.IsNullOrWhiteSpace(textBox.Text))
            return true;

        double offset = textBox.DoubleValue * 2.0;
        if (!double.IsFinite(offset) || offset < short.MinValue || offset > short.MaxValue || offset != Math.Truncate(offset))
            return false;

        halfTiles = (int)offset;
        return true;
    }

    private void SelectMirroringPoint(object sender, EventArgs e)
    {
        previousCursorAction = editorState.CursorAction;
        Hide();

        editorState.CursorAction = selectCellAction;
        editorState.CursorActionChanged += FinishPointSelection;
        notificationManager.AddNotification(Translate(this, "SelectPointOnMap", "Select a cell for the mirroring point."));
    }

    private void MirroringPointSelected(object sender, Point2D cell)
    {
        SetOffsets(MapMirroring.GetOffsetForCell(cell, cursorActionTarget.Map.Size));
    }

    private void FinishPointSelection(object sender, EventArgs e)
    {
        editorState.CursorActionChanged -= FinishPointSelection;

        CursorAction previous = previousCursorAction;
        previousCursorAction = null;

        // If a different tool was selected during picking, let it take over.
        if (editorState.CursorAction != null)
            return;

        editorState.CursorAction = previous;
        Show();
    }

    private void Apply(object sender, EventArgs e)
    {
        MirrorAxes axes = GetSelectedAxes();
        if (axes == MirrorAxes.None || !TryGetOffsets(out Point2D offset))
            return;

        editorState.Mirroring = new MapMirroring(axes, offset);
        editorState.MirroringEnabled = true;
        Hide();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        bool validOffsets = TryGetOffsets(out _);
        lblOffsetHint.TextColor = validOffsets ? offsetHintColor : Color.OrangeRed;
        applyButton.AllowClick = GetSelectedAxes() != MirrorAxes.None && validOffsets;
    }
}
