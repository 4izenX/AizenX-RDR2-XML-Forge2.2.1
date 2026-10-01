using System.Drawing;
using System.Windows.Forms;

namespace AizenX;

public sealed class StableTabControl : TabControl
{
	private const int WS_CLIPCHILDREN = 33554432;

	private const int WS_CLIPSIBLINGS = 67108864;

	private const int WS_EX_COMPOSITED = 33554432;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams createParams = base.CreateParams;
			createParams.Style |= 100663296;
			createParams.ExStyle |= 33554432;
			return createParams;
		}
	}

	public StableTabControl()
	{
		SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		DrawMode = TabDrawMode.OwnerDrawFixed;
		SizeMode = TabSizeMode.Fixed;
		ItemSize = new Size(160, 32);
		HotTrack = false;
	}

	protected override void OnDrawItem(DrawItemEventArgs e)
	{
		Rectangle tabRect = GetTabRect(e.Index);
		bool flag = e.Index == SelectedIndex;
		using SolidBrush brush = new SolidBrush(flag ? Color.White : Color.FromArgb(238, 238, 240));
		e.Graphics.FillRectangle(brush, tabRect);
		using Pen pen = new Pen(Color.FromArgb(135, 135, 140));
		e.Graphics.DrawRectangle(pen, tabRect.X, tabRect.Y, tabRect.Width - 1, tabRect.Height - 1);
		if (flag)
		{
			using Pen pen2 = new Pen(Color.Black, 3f);
			e.Graphics.DrawLine(pen2, tabRect.Left + 1, tabRect.Bottom - 2, tabRect.Right - 2, tabRect.Bottom - 2);
		}
		TextRenderer.DrawText(e.Graphics, TabPages[e.Index].Text, Font, Rectangle.Inflate(tabRect, -8, -2), Color.Black, TextFormatFlags.EndEllipsis | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
	}
}