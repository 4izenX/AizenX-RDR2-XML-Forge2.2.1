using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AizenX;

public sealed class CardPanel : Panel
{
	public int CornerRadius = 12;

	public Color CardBackColor = UiTheme.Surface;

	public Color CardBorderColor = UiTheme.BorderSoft;

	public CardPanel()
	{
		BackColor = Color.Transparent;
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle clientRectangle = ClientRectangle;
		clientRectangle.Width--;
		clientRectangle.Height--;
		using GraphicsPath path = UiTheme.RoundedRect(clientRectangle, CornerRadius);
		using SolidBrush brush = new SolidBrush(Color.FromArgb(28, 0, 0, 0));
		Rectangle rect = clientRectangle;
		rect.Offset(0, 3);
		using GraphicsPath path2 = UiTheme.RoundedRect(rect, CornerRadius);
		e.Graphics.FillPath(brush, path2);
		using SolidBrush brush2 = new SolidBrush(CardBackColor);
		using Pen pen = new Pen(CardBorderColor);
		e.Graphics.FillPath(brush2, path);
		e.Graphics.DrawPath(pen, path);
		base.OnPaint(e);
	}
}