using System.Drawing;
using System.Windows.Forms;

namespace AizenX;

public sealed class HiddenTabs : TabControl
{
	private const int TCM_ADJUSTRECT = 4904;

	public HiddenTabs()
	{
		Appearance = TabAppearance.FlatButtons;
		ItemSize = new Size(0, 1);
		SizeMode = TabSizeMode.Fixed;
		Multiline = true;
		SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
	}

	protected override void WndProc(ref Message m)
	{
		if (m.Msg == 4904 && !DesignMode)
		{
			m.Result = 1;
		}
		else
		{
			base.WndProc(ref m);
		}
	}
}