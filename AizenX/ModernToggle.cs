using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AizenX;

public sealed class ModernToggle : CheckBox
{
	public string Caption = "";

	public string Helper = "";

	public ModernToggle()
	{
		AutoSize = false;
		Height = 52;
		ForeColor = UiTheme.Text;
		BackColor = UiTheme.Surface;
		Font = UiTheme.Ui(9.5f, FontStyle.Bold);
		Cursor = Cursors.Hand;
		AutoCheck = false;
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		e.Graphics.Clear(BackColor);
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		Rectangle rect = new Rectangle(Width - 52, 13, 42, 22);
		using GraphicsPath path = UiTheme.RoundedRect(rect, 11);
		using SolidBrush brush = new SolidBrush(Checked ? UiTheme.Crimson : UiTheme.Surface3);
		using Pen pen = new Pen(Checked ? UiTheme.CrimsonHot : UiTheme.Border);
		e.Graphics.FillPath(brush, path);
		e.Graphics.DrawPath(pen, path);
		int x = (Checked ? (rect.Right - 19) : (rect.Left + 3));
		Rectangle rect2 = new Rectangle(x, rect.Top + 3, 16, 16);
		using SolidBrush brush2 = new SolidBrush(Color.WhiteSmoke);
		e.Graphics.FillEllipse(brush2, rect2);
		string text = (string.IsNullOrWhiteSpace(Caption) ? Text : Caption);
		TextRenderer.DrawText(e.Graphics, text, Font, new Rectangle(0, 4, Width - 70, 20), UiTheme.Text, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
		if (!string.IsNullOrWhiteSpace(Helper))
		{
			TextRenderer.DrawText(e.Graphics, Helper, UiTheme.Ui(8.4f), new Rectangle(0, 26, Width - 70, 20), UiTheme.Muted, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
		}
	}

	protected override void OnClick(EventArgs e)
	{
		Checked = !Checked;
		Invalidate();
	}
}