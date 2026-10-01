using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AizenX;

public sealed class SearchResultView : ScrollableControl
{
	private readonly List<ArchiveAsset> items = new List<ArchiveAsset>();

	private int selectedIndex = -1;

	private int hoverIndex = -1;

	private const int PaddingTop = 10;

	private const int ItemGap = 4;

	public ArchiveAsset? SelectedAsset
	{
		get
		{
			if (selectedIndex < 0 || selectedIndex >= items.Count)
				return null;
			return items[selectedIndex];
		}
	}

	public string? SelectedItem => SelectedAsset?.FileName;

	public SearchResultView()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = UiTheme.Surface2;
		ForeColor = UiTheme.Text;
		Font = UiTheme.Mono(9.3f);
		AutoScroll = true;
		TabStop = true;
		Cursor = Cursors.Hand;
	}

	public void SetItems(IEnumerable<ArchiveAsset> values)
	{
		items.Clear();
		items.AddRange(values);
		selectedIndex = -1;
		hoverIndex = -1;
		UpdateScrollSize();
		Invalidate();
	}

	public void ClearItems()
	{
		items.Clear();
		selectedIndex = -1;
		hoverIndex = -1;
		UpdateScrollSize();
		Invalidate();
	}

	private void UpdateScrollSize()
	{
		int num = Math.Max(28, Font.Height + 11);
		AutoScrollMinSize = new Size(0, 20 + items.Count * (num + 4));
	}

	private int HitTest(int y)
	{
		int num = Math.Max(28, Font.Height + 11);
		int num2 = y - AutoScrollPosition.Y - 10;
		if (num2 < 0)
		{
			return -1;
		}
		int num3 = num + 4;
		int num4 = num2 / num3;
		if (num2 % num3 >= num || num4 < 0 || num4 >= items.Count)
		{
			return -1;
		}
		return num4;
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
		e.Graphics.Clear(UiTheme.Surface2);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		int num = Math.Max(28, Font.Height + 11);
		int num2 = 10 + AutoScrollPosition.Y;
		if (items.Count == 0)
		{
			TextRenderer.DrawText(e.Graphics, "No results yet. Search the RDR2 asset index above.", UiTheme.Ui(9.2f), new Rectangle(18, 18, Math.Max(100, ClientSize.Width - 36), 28), UiTheme.Muted, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
			return;
		}
		for (int i = 0; i < items.Count; i++)
		{
			Rectangle rect = new Rectangle(10, num2, Math.Max(10, ClientSize.Width - 32), num);
			if (rect.Bottom >= 0 && rect.Top <= ClientSize.Height)
			{
				Color color;
				if (i == selectedIndex)
				{
					color = Color.FromArgb(72, UiTheme.Crimson);
				}
				else if (i == hoverIndex)
				{
					color = UiTheme.Surface3;
				}
				else
				{
					color = ((i % 2 == 0) ? Color.FromArgb(22, 22, 28) : UiTheme.Surface2);
				}
				using SolidBrush brush = new SolidBrush(color);
				e.Graphics.FillRectangle(brush, rect);
				if (i == selectedIndex)
				{
					using SolidBrush brush2 = new SolidBrush(UiTheme.CrimsonHot);
					e.Graphics.FillRectangle(brush2, rect.Left, rect.Top, 3, rect.Height);
				}
				TextRenderer.DrawText(e.Graphics, items[i].DisplayText, Font, new Rectangle(rect.X + 11, rect.Y, rect.Width - 18, rect.Height), (i == selectedIndex) ? Color.White : ForeColor, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
			}
			num2 += num + 4;
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		int num = HitTest(e.Y);
		if (num != hoverIndex)
		{
			hoverIndex = num;
			Invalidate();
		}
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		hoverIndex = -1;
		Invalidate();
		base.OnMouseLeave(e);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		Focus();
		selectedIndex = HitTest(e.Y);
		Invalidate();
	}

	protected override void OnFontChanged(EventArgs e)
	{
		base.OnFontChanged(e);
		UpdateScrollSize();
	}
}