using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AizenX;

public sealed class BrandMark : Control
{
    public BrandMark()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);
        Size = new Size(46, 46);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = new RectangleF(2, 2, Width - 5, Height - 5);
        using var plate = UiTheme.RoundedRect(Rectangle.Round(r), 12);
        using var fill = new LinearGradientBrush(
            Rectangle.Round(r), UiTheme.Surface3, Color.FromArgb(12, 13, 18), 135f);
        using var border = new Pen(Color.FromArgb(110, UiTheme.Brass), 1f);
        g.FillPath(fill, plate);
        g.DrawPath(border, plate);

        float w = r.Width;
        float h = r.Height;
        PointF[] bladeA =
        {
            new(r.Left + w * .25f, r.Top + h * .72f),
            new(r.Left + w * .43f, r.Top + h * .20f),
            new(r.Left + w * .52f, r.Top + h * .20f),
            new(r.Left + w * .36f, r.Top + h * .72f)
        };
        PointF[] bladeX =
        {
            new(r.Left + w * .50f, r.Top + h * .24f),
            new(r.Left + w * .76f, r.Top + h * .70f),
            new(r.Left + w * .66f, r.Top + h * .76f),
            new(r.Left + w * .42f, r.Top + h * .31f)
        };
        PointF[] cross =
        {
            new(r.Left + w * .34f, r.Top + h * .50f),
            new(r.Left + w * .67f, r.Top + h * .50f),
            new(r.Left + w * .64f, r.Top + h * .58f),
            new(r.Left + w * .32f, r.Top + h * .58f)
        };

        using var crimson = new LinearGradientBrush(
            new RectangleF(r.Left, r.Top, r.Width, r.Height),
            UiTheme.CrimsonHot, UiTheme.CrimsonDeep, 90f);
        using var brass = new LinearGradientBrush(
            new RectangleF(r.Left, r.Top, r.Width, r.Height),
            UiTheme.Brass, Color.FromArgb(102, 78, 42), 90f);
        g.FillPolygon(crimson, bladeA);
        g.FillPolygon(crimson, bladeX);
        g.FillPolygon(brass, cross);

        using var edge = new Pen(Color.FromArgb(145, 255, 255, 255), 0.8f);
        g.DrawLines(edge, new[] { bladeA[0], bladeA[1], bladeA[2] });
        g.DrawLines(edge, new[] { bladeX[0], bladeX[1] });
    }
}