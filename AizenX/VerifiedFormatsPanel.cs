using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AizenX;

public sealed class VerifiedFormatsPanel : Panel
{
    private readonly List<VerifiedFormatRow> rows = new();
    private readonly Label title = new();
    private readonly Label summary = new();
    private readonly AizenButton collapse = new();
    private bool collapsed;

    public Action<string>? UseRequested;

    public VerifiedFormatsPanel()
    {
        Width = 372;
        Dock = DockStyle.Right;
        BackColor = UiTheme.Surface;
        Padding = new Padding(14, 16, 14, 14);
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint, true);

        title.Text = "VERIFIED FORMATS";
        title.ForeColor = UiTheme.Text;
        title.Font = UiTheme.Ui(10f, FontStyle.Bold);
        title.AutoSize = true;
        title.Location = new Point(16, 18);

        summary.Text = "v2.2.1 REGRESSION · ALL PASSED";
        summary.ForeColor = UiTheme.Success;
        summary.Font = UiTheme.Mono(7.6f, FontStyle.Bold);
        summary.AutoSize = true;
        summary.Location = new Point(16, 43);

        collapse.Text = "‹";
        collapse.Ghost = true;
        collapse.Size = new Size(32, 30);
        collapse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        collapse.Click += (_, _) => ToggleCollapsed();

        Controls.AddRange(title, summary, collapse);

        AddRow("◆", "RSC8", "Scenario YMT", "RSC8 Scenario YMT round-trip regression passed.", true, "RSC8");
        AddRow("◈", "PSO / MetaPed", "Native metadata", "MetaPed PSO/PSIN compile + parser round-trip passed.", true, "PSO");
        AddRow("YF", "YFT", "Fragment", "YFT rebuild + reopen verification passed.", true, "YFT");
        AddRow("YD", "YDD", "Drawable dictionary", "YDD rebuild + reopen verification passed.", true, "YDD");
        AddRow("YT", "YTD", "Texture dictionary", "YTD rebuild + reopen verification passed.", true, "YTD");
        AddRow("BW", "Blackwater", "375 overrides · 455 points", "78 map keys, 375 entity overrides and 455 entity scenario points retained. 96,371/96,371 elements.", true, "BLACKWATER");
        AddRow("…", "Other formats", "Native fallback", "Not all proprietary formats have semantic XML round-trip regression coverage.", false, "");

        Resize += (_, _) => LayoutRows();
        LayoutRows();
    }

    private void AddRow(string icon, string name, string detail, string tooltip, bool verified, string useKey)
    {
        var row = new VerifiedFormatRow(icon, name, detail, tooltip, verified, useKey);
        row.UseRequested = key => UseRequested?.Invoke(key);
        rows.Add(row);
        Controls.Add(row);
    }

    private void LayoutRows()
    {
        collapse.Left = Math.Max(8, Width - collapse.Width - 12);
        collapse.Top = 12;
        if (collapsed) return;

        int y = 76;
        foreach (var row in rows)
        {
            row.Location = new Point(10, y);
            row.Width = Math.Max(292, Width - 20);
            y += row.Height + 7;
        }
    }

    public void EnsureExpanded()
    {
        if (collapsed)
            ToggleCollapsed();
    }

    private void ToggleCollapsed()
    {
        collapsed = !collapsed;
        Width = collapsed ? 50 : 372;
        title.Visible = summary.Visible = !collapsed;
        collapse.Text = collapsed ? "›" : "‹";
        foreach (var row in rows) row.Visible = !collapsed;
        Invalidate(true);
        LayoutRows();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(UiTheme.BorderSoft);
        e.Graphics.DrawLine(pen, 0, 0, 0, Height);

        if (collapsed)
        {
            using var f = UiTheme.Ui(7.8f, FontStyle.Bold);
            TextRenderer.DrawText(
                e.Graphics, "VERIFIED", f,
                new Rectangle(7, 58, 36, 126),
                UiTheme.Success,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
        }
    }
}

internal sealed class VerifiedFormatRow : Panel
{
    public Action<string>? UseRequested;

    private readonly string icon;
    private readonly string name;
    private readonly string detail;
    private readonly string useKey;
    private readonly bool verified;
    private readonly ToolTip tip = new();
    private readonly AizenButton use = new();

    public VerifiedFormatRow(string icon, string name, string detail, string tooltip, bool verified, string useKey)
    {
        this.icon = icon;
        this.name = name;
        this.detail = detail;
        this.useKey = useKey;
        this.verified = verified;

        Height = 66;
        BackColor = UiTheme.Surface2;
        SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);

        use.Text = verified && useKey.Length > 0 ? "USE" : "LOCKED";
        use.Size = new Size(58, 30);
        use.Ghost = true;
        use.Enabled = verified && useKey.Length > 0;
        use.Font = UiTheme.Ui(7.8f, FontStyle.Bold);
        use.Click += (_, _) =>
        {
            if (this.verified && this.useKey.Length > 0)
                UseRequested?.Invoke(this.useKey);
        };
        Controls.Add(use);

        tip.SetToolTip(this, tooltip);
        tip.SetToolTip(use, verified ? "Switch to this verified format." : "No verified semantic round-trip yet.");

        Resize += (_, _) =>
        {
            LayoutUseButton();
            Invalidate();
        };
        LayoutUseButton();
    }

    private void LayoutUseButton()
    {
        use.Left = Width - use.Width - 10;
        use.Top = (Height - use.Height) / 2;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(UiTheme.Surface2);

        using (var rowPath = UiTheme.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 8))
        using (var border = new Pen(Color.FromArgb(26, 255, 255, 255), 1f))
        {
            g.DrawPath(border, rowPath);
        }

        var glyphRect = new Rectangle(10, 18, 30, 30);
        using (var glyphPath = UiTheme.RoundedRect(glyphRect, 7))
        using (var glyphBrush = new SolidBrush(UiTheme.Surface3))
        {
            g.FillPath(glyphBrush, glyphPath);
        }
        TextRenderer.DrawText(
            g, icon, UiTheme.Mono(8.2f, FontStyle.Bold), glyphRect,
            verified ? UiTheme.Brass : UiTheme.Warning,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        int useLeft = use.Left;
        int chipWidth = 76;
        int chipLeft = useLeft - 8 - chipWidth;
        var chipRect = new Rectangle(chipLeft, 22, chipWidth, 22);
        using (var chipPath = UiTheme.RoundedRect(chipRect, 10))
        using (var chipBrush = new SolidBrush(verified ? Color.FromArgb(22, 60, 38) : Color.FromArgb(62, 43, 13)))
        {
            g.FillPath(chipBrush, chipPath);
        }
        TextRenderer.DrawText(
            g, verified ? "VERIFIED" : "UNPROVEN",
            UiTheme.Ui(7f, FontStyle.Bold), chipRect,
            verified ? UiTheme.Success : UiTheme.Warning,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        int textLeft = 50;
        int textRight = chipLeft - 12;
        int textWidth = Math.Max(80, textRight - textLeft);

        var nameRect = new Rectangle(textLeft, 9, textWidth, 22);
        var detailRect = new Rectangle(textLeft, 34, textWidth, 20);

        TextRenderer.DrawText(
            g, name, UiTheme.Ui(8.8f, FontStyle.Bold), nameRect,
            UiTheme.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        TextRenderer.DrawText(
            g, detail, UiTheme.Ui(7.5f), detailRect,
            UiTheme.Muted,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}