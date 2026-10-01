using System.Drawing;
using System.Windows.Forms;

namespace AizenX;

public sealed class NavButton : AizenButton
{
    public bool Active;

    public NavButton()
    {
        Ghost = true;
        TextAlign = ContentAlignment.MiddleLeft;
        Padding = new Padding(16, 0, 10, 0);
        Height = 40;
        CornerRadius = 9;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Selected = Active;
        base.OnPaint(e);
        if (!Active) return;

        using var bar = new SolidBrush(UiTheme.Crimson);
        e.Graphics.FillRectangle(bar, 1, 8, 3, Height - 16);

        using var glow = new SolidBrush(Color.FromArgb(50, UiTheme.Crimson));
        e.Graphics.FillRectangle(glow, 4, 10, 5, Height - 20);
    }
}