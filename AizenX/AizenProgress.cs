using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AizenX;

public sealed class AizenProgress : Control
{
	private readonly Timer timer = new Timer
	{
		Interval = 24
	};

	private int x;

	private bool running;

	public AizenProgress()
	{
		Height = 3;
		BackColor = UiTheme.Surface;
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		timer.Tick += (object? _, EventArgs _) =>
		{
			x += 12;
			if (x > Width + 120)
			{
				x = -120;
			}
			Invalidate();
		};
	}

	public void SetRunning(bool value)
	{
		running = value;
		Visible = value;
		x = -120;
		if (value)
		{
			timer.Start();
		}
		else
		{
			timer.Stop();
		}
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.Clear(UiTheme.Surface);
		if (!running)
		{
			return;
		}
		using LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(x, 0, 120, Math.Max(1, Height)), UiTheme.CrimsonDark, UiTheme.CrimsonHot, 0f);
		e.Graphics.FillRectangle(brush, x, 0, 120, Height);
	}
}