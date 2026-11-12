using MapEditorLibrary;
using MapEditorLibrary.Models;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows;

public class ReferenceListWindow : INItializableWindow
{
    public ReferenceListWindow(WindowManager windowManager, Action<object> navigateToReference) : base(windowManager)
    {
        this.navigateToReference = navigateToReference;
    }

    private readonly Action<object> navigateToReference;

    private XNALabel lblHeader;
    private ReferenceListPanel panelReferences;

    public override void Initialize()
    {
        Name = nameof(ReferenceListWindow);
        base.Initialize();

        lblHeader = FindChild<XNALabel>(nameof(lblHeader));
        panelReferences = FindChild<ReferenceListPanel>(nameof(panelReferences));
        CenterOnParent();
    }

    public void Open(Trigger trigger, IReadOnlyList<(string Text, object Target)> references, string notice)
    {
        string header = string.Format(Translate(this, "Header", "References for \"{0}\""), trigger.Name);
        lblHeader.Text = Renderer.GetStringWithLimitedWidth(header, lblHeader.FontIndex, btnClose.X - lblHeader.X - Constants.UIHorizontalSpacing);
        panelReferences.SetReferences(references, notice, navigateToReference);

        Show();
    }
}

public class ReferenceListPanel : XNAScrollPanel
{
    public ReferenceListPanel(WindowManager windowManager) : base(windowManager)
    {
        DrawBorders = true;
        AllowScroll = (false, true);
        OverscrollMargin = new Point(0, Constants.UIEmptyBottomSpace);
    }

    private readonly List<XNALabel> rows = new List<XNALabel>();

    public override void Initialize()
    {
        if (BackgroundTexture == null)
        {
            BackgroundTexture = AssetLoader.CreateTexture(Color.White, 2, 2);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
        }

        base.Initialize();
    }

    public override void Kill()
    {
        BackgroundTexture?.Dispose();
        BackgroundTexture = null;

        base.Kill();
    }

    public override void Draw(GameTime gameTime)
    {
        RemapColor = UISettings.ActiveSettings.PanelBackgroundColor;
        base.Draw(gameTime);
    }

    public void SetReferences(IReadOnlyList<(string Text, object Target)> references, string notice, Action<object> navigateToReference)
    {
        foreach (var row in rows)
        {
            row.Disable();
            ContentPanel.RemoveChild(row);
            row.Kill();
        }

        rows.Clear();

        int width = Width - VerticalScrollBar.ScrollWidth - Constants.UIHorizontalSpacing * 2;
        int y = Constants.UIVerticalSpacing;
        if (notice != null)
            y = AddRow(new XNALabel(WindowManager), notice, y, width) + Constants.UIVerticalSpacing;

        foreach (var reference in references)
        {
            var link = new ReferenceLinkLabel(WindowManager, navigateToReference);
            link.Tag = reference.Target;
            y = AddRow(link, reference.Text, y, width);
        }

        ScrollToTop();
    }

    private int AddRow(XNALabel label, string text, int y, int width)
    {
        label.Text = string.Join(Environment.NewLine, Renderer.GetFixedTextLines(text, label.FontIndex, width));
        label.X = Constants.UIHorizontalSpacing;
        label.Y = y;
        ContentPanel.AddChild(label);
        rows.Add(label);

        return label.Bottom + Constants.UIVerticalSpacing;
    }

    private sealed class ReferenceLinkLabel : XNALinkLabel
    {
        public ReferenceLinkLabel(WindowManager windowManager, Action<object> navigateToReference) : base(windowManager)
        {
            HoverColor = UISettings.ActiveSettings.AltColor;
            
            this.navigateToReference = navigateToReference;
        }

        private readonly Action<object> navigateToReference;

        public override void OnLeftClick(InputEventArgs inputEventArgs)
        {
            inputEventArgs.Handled = true;
            base.OnLeftClick(inputEventArgs);
            navigateToReference(Tag);
        }

        public override void Draw(GameTime gameTime)
        {
            TextColor = RemapColor;
            base.Draw(gameTime);
        }
    }
}
