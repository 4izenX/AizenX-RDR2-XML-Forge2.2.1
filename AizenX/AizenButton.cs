using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AizenX;

public class AizenButton : Button
{
    private bool hover;
    private bool pressed;

    public bool Primary;
    public bool Danger;
    public bool Ghost;
    public bool Selected;
    public int CornerRadius = 8;

    public AizenButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        Cursor = Cursors.Hand;
        Height = 38;
        Font = UiTheme.Ui(9.1f, FontStyle.Bold);
        ForeColor = UiTheme.Text;
        BackColor = Color.Transparent;
        SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        hover = false;
        pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        pressed = true;
        Invalidate();
        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Color parentBack = Parent?.BackColor ?? UiTheme.Surface;
        g.Clear(parentBack);
        Rectangle rect = ClientRectangle;
        rect.Width--;
        rect.Height--;

        Color fill;
        Color border;

        if (!Enabled)
        {
            fill = Color.FromArgb(12, 255, 255, 255);
            border = UiTheme.BorderSoft;
        }
        else if (Selected)
        {
            fill = hover ? Color.FromArgb(58, 92, 18, 29) : Color.FromArgb(42, 92, 18, 29);
            border = Color.FromArgb(78, UiTheme.Crimson);
        }
        else if (Danger)
        {
            fill = hover ? Color.FromArgb(74, 51, 19, 24) : Color.FromArgb(48, 42, 18, 22);
            border = Color.FromArgb(120, UiTheme.Error);
        }
        else if (Ghost)
        {
            fill = hover ? UiTheme.Surface3 : Color.FromArgb(8, 255, 255, 255);
            border = hover ? UiTheme.Border : UiTheme.BorderSoft;
        }
        else
        {
            fill = hover ? UiTheme.Surface4 : UiTheme.Surface2;
            border = hover ? Color.FromArgb(66, 255, 255, 255) : UiTheme.Border;
        }

        using var path = UiTheme.RoundedRect(rect, CornerRadius);

        if (Primary && Enabled)
        {
            if (hover)
            {
                using var glow = new Pen(Color.FromArgb(72, UiTheme.CrimsonHot), 4f);
                g.DrawPath(glow, path);
            }
            using var gradient = new LinearGradientBrush(
                rect,
                pressed ? UiTheme.CrimsonDeep : (hover ? UiTheme.CrimsonHot : UiTheme.Crimson),
                UiTheme.CrimsonDeep,
                90f);
            g.FillPath(gradient, path);
            using var p = new Pen(Color.FromArgb(170, UiTheme.CrimsonHot));
            g.DrawPath(p, path);
            using var top = new Pen(Color.FromArgb(95, 255, 255, 255));
            g.DrawLine(top, rect.Left + CornerRadius, rect.Top + 1, rect.Right - CornerRadius, rect.Top + 1);
        }
        else
        {
            using var brush = new SolidBrush(fill);
            using var p = new Pen(border);
            g.FillPath(brush, path);
            g.DrawPath(p, path);
        }

        var textRect = rect;
        if (pressed) textRect.Offset(0, 1);
        TextRenderer.DrawText(
            g, Text, Font, textRect,
            Enabled ? ForeColor : UiTheme.Muted2,
            TextFormatFlags.EndEllipsis | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}