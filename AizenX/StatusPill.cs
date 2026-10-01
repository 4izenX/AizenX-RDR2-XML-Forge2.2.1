using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AizenX;

public sealed class StatusPill : Control
{
	public bool Good = true;

	public bool PulseEnabled = true;

	public string StatusText = "ENGINE READY";

	private bool pulse;

	private readonly Timer timer = new Timer
	{
		Interval = 650
	};

	public StatusPill()
	{
		Size = new Size(170, 32);
		Font = UiTheme.Ui(9f, FontStyle.Bold);
		timer.Tick += (object? _, EventArgs _) =>
		{
			if (!PulseEnabled)
			{
				pulse = false;
			}
			else
			{
				pulse = !pulse;
				Invalidate();
			}
		};
		timer.Start();
		SetStyle(ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle clientRectangle = ClientRectangle;
		clientRectangle.Width--;
		clientRectangle.Height--;
		Color color = (Good ? Color.FromArgb(26, 54, 38) : Color.FromArgb(58, 26, 28));
		Color color2 = (Good ? Color.FromArgb(55, 106, 74) : Color.FromArgb(124, 50, 55));
		using GraphicsPath path = UiTheme.RoundedRect(clientRectangle, 16);
		using SolidBrush brush = new SolidBrush(color);
		using Pen pen = new Pen(color2);
		e.Graphics.FillPath(brush, path);
		e.Graphics.DrawPath(pen, path);
		Color color3 = (Good ? UiTheme.Success : UiTheme.Error);
		Rectangle rect = new Rectangle(14, 11, 9, 9);
		if (pulse)
		{
			using SolidBrush brush2 = new SolidBrush(Color.FromArgb(55, color3));
			e.Graphics.FillEllipse(brush2, new Rectangle(10, 7, 17, 17));
		}
		using SolidBrush brush3 = new SolidBrush(color3);
		e.Graphics.FillEllipse(brush3, rect);
		TextRenderer.DrawText(e.Graphics, StatusText, Font, new Rectangle(31, 0, Width - 38, Height), Good ? UiTheme.Success : UiTheme.Error, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
	}
}