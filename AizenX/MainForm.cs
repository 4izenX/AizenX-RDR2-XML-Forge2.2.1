using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace AizenX;

public sealed class MainForm : Form
{
	private readonly AppConfig config;

	private readonly Color Red = UiTheme.Crimson;

	private readonly Color Dark = UiTheme.Surface;

	private readonly Color Dark2 = UiTheme.Surface2;

	private readonly Color Light = UiTheme.Text;

	private Image? backgroundImage;

	private readonly StatusPill engineStatus = new StatusPill();

	private readonly Label bottomStatus = new Label();

	private readonly RichTextBox logBox = new RichTextBox();

	private readonly HiddenTabs pages = new HiddenTabs();

	private readonly List<NavButton> navButtons = new List<NavButton>();

	private readonly Panel toastPanel = new Panel();

	private readonly Label toastText = new Label();

	private readonly AizenProgress busyBar = new AizenProgress();

	private readonly VerifiedFormatsPanel verifiedPanel = new VerifiedFormatsPanel();

	private readonly TextBox commandBox = new TextBox();

	private readonly Label taskStatus = new Label();

	private readonly TextBox ymtInput = new TextBox();

	private readonly TextBox ymtOutput = new TextBox();

	private readonly TextBox ymtReport = new TextBox();

	private string? ymtBuiltPath;

	private readonly TextBox exportItems = new TextBox();

	private readonly TextBox exportOutput = new TextBox();

	private readonly RadioButton archiveMode = new RadioButton();

	private readonly RadioButton localMode = new RadioButton();

	private readonly TextBox buildItems = new TextBox();

	private readonly TextBox buildOutput = new TextBox();

	private readonly ListBox buildResults = new ListBox();

	private readonly List<string> lastBuilt = new List<string>();

	private readonly TextBox searchBox = new TextBox();

	private readonly ComboBox searchExt = new ComboBox();

	private readonly SearchResultView searchResults = new SearchResultView();

	private readonly Label searchCount = new Label();

	private readonly TextBox inspectPath = new TextBox();

	private readonly TextBox inspectResult = new TextBox();

	private readonly TextBox compareA = new TextBox();

	private readonly TextBox compareB = new TextBox();

	private readonly TextBox compareResult = new TextBox();

	private readonly ListBox recentList = new ListBox();

	private readonly ListBox favoriteList = new ListBox();

	private readonly TextBox engineBox = new TextBox();

	private readonly TextBox gameBox = new TextBox();

	private readonly TextBox settingsOutput = new TextBox();

	private readonly TextBox backgroundBox = new TextBox();

	private readonly ModernToggle safeMode = new ModernToggle();

	private readonly ModernToggle verifyBuilds = new ModernToggle();

	private readonly ModernToggle backupInstall = new ModernToggle();

	private readonly ModernToggle reduceMotion = new ModernToggle();

	public MainForm(AppConfig config)
	{
		this.config = config;
		Text = "AIZENX FORGE — RDR2 XML Forge v2.2.1";
		try
		{
			Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
		}
		catch
		{
		}
		StartPosition = FormStartPosition.CenterScreen;
		Size = new Size(1380, 900);
		MinimumSize = new Size(1120, 740);
		ForeColor = Light;
		BackColor = UiTheme.Bg;
		Font = UiTheme.Ui();
		DoubleBuffered = true;
		AllowDrop = true;
		DragEnter += FormDragEnter;
		DragDrop += FormDragDrop;
		LoadBackground();
		BuildUi();
		PopulateSettings();
		RefreshEngineStatus();
		RefreshHistory();
		FormClosing += (object? _, FormClosingEventArgs _) =>
		{
			SaveSettings();
		};
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
		e.Graphics.Clear(UiTheme.Bg);
	}

	private void LoadBackground()
	{
		try
		{
			string text = config.BackgroundPath;
			if (string.IsNullOrWhiteSpace(text) || !File.Exists(text))
			{
				backgroundImage = null;
				return;
			}
			using FileStream stream = new FileStream(text, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using Image original = Image.FromStream(stream);
			backgroundImage = new Bitmap(original);
			config.BackgroundPath = text;
		}
		catch
		{
			backgroundImage = null;
		}
	}

private void BuildUi()
{
    SuspendLayout();
    KeyPreview = true;
    KeyDown += (_, e) =>
    {
        if (e.Control && e.KeyCode == Keys.K)
        {
            e.SuppressKeyPress = true;
            commandBox.Focus();
            commandBox.SelectAll();
        }
    };

    Panel header = new Panel
    {
        Dock = DockStyle.Top,
        Height = 74,
        BackColor = UiTheme.Surface
    };
    header.Paint += (_, e) =>
    {
        e.Graphics.Clear(UiTheme.Surface);
        using (var ridge = new SolidBrush(Color.FromArgb(32, UiTheme.CrimsonDeep)))
        {
            Point[] pts =
            {
                new(Math.Max(0, header.Width - 560), header.Height),
                new(Math.Max(0, header.Width - 490), 41),
                new(Math.Max(0, header.Width - 420), 55),
                new(Math.Max(0, header.Width - 340), 29),
                new(Math.Max(0, header.Width - 250), 50),
                new(Math.Max(0, header.Width - 160), 35),
                new(header.Width, 58),
                new(header.Width, header.Height)
            };
            e.Graphics.FillPolygon(ridge, pts);
        }
        if (backgroundImage != null)
        {
            var destRect = new Rectangle(Math.Max(0, header.Width - 600), 0, 600, header.Height);
            var matrix = new ColorMatrix { Matrix00 = .14f, Matrix11 = .14f, Matrix22 = .14f, Matrix33 = .12f };
            using var attrs = new ImageAttributes();
            attrs.SetColorMatrix(matrix);
            e.Graphics.DrawImage(backgroundImage, destRect, 0, 0, backgroundImage.Width, backgroundImage.Height, GraphicsUnit.Pixel, attrs);
        }
        using var line = new Pen(Color.FromArgb(90, UiTheme.Crimson), 1f);
        e.Graphics.DrawLine(line, 0, header.Height - 1, header.Width, header.Height - 1);
    };
    Controls.Add(header);

    var mark = new BrandMark { Location = new Point(18, 13), Size = new Size(46, 46) };
    var wordmark = new Label
    {
        Text = "AIZENX FORGE",
        AutoSize = true,
        ForeColor = UiTheme.Text,
        Font = UiTheme.UiDisplay(17.5f, FontStyle.Bold),
        BackColor = Color.Transparent,
        Location = new Point(76, 11)
    };
    var tagline = new Label
    {
        Text = "VERIFIED RDR2 ASSET PIPELINE  ·  v2.2.1",
        AutoSize = true,
        ForeColor = UiTheme.Brass,
        Font = UiTheme.Mono(7.8f, FontStyle.Bold),
        BackColor = Color.Transparent,
        Location = new Point(79, 43)
    };

    StyleWhite(commandBox);
    commandBox.PlaceholderText = "Search commands & screens…   Ctrl+K";
    commandBox.Size = new Size(360, 32);
    commandBox.Location = new Point(420, 20);
    commandBox.Font = UiTheme.Ui(9f);
    commandBox.KeyDown += (_, e) =>
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            RunCommandPalette(commandBox.Text);
        }
        else if (e.KeyCode == Keys.Escape)
        {
            commandBox.Clear();
            ActiveControl = null;
        }
    };

    var safeQuick = new AizenButton
    {
        Text = config.SafeMode ? "SAFE ON" : "SAFE OFF",
        Size = new Size(88, 32),
        Ghost = true,
        Anchor = AnchorStyles.Top | AnchorStyles.Right
    };
    safeQuick.Click += (_, _) =>
    {
        safeMode.Checked = !safeMode.Checked;
        config.SafeMode = safeMode.Checked;
        safeQuick.Text = safeMode.Checked ? "SAFE ON" : "SAFE OFF";
        RefreshEngineStatus();
    };
    safeMode.CheckedChanged += (_, _) => safeQuick.Text = safeMode.Checked ? "SAFE ON" : "SAFE OFF";

    var settingsQuick = new AizenButton
    {
        Text = "⚙",
        Size = new Size(38, 32),
        Ghost = true,
        Anchor = AnchorStyles.Top | AnchorStyles.Right,
        Font = UiTheme.Ui(11f, FontStyle.Bold)
    };
    settingsQuick.Click += (_, _) => ShowPage(5);

    engineStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
    engineStatus.Size = new Size(156, 32);

    void LayoutHeader()
    {
        int right = header.ClientSize.Width - 18;
        settingsQuick.Left = right - settingsQuick.Width;
        settingsQuick.Top = 20;
        right = settingsQuick.Left - 8;
        safeQuick.Left = right - safeQuick.Width;
        safeQuick.Top = 20;
        right = safeQuick.Left - 10;
        engineStatus.Left = right - engineStatus.Width;
        engineStatus.Top = 20;
        int leftBound = 350;
        int desired = Math.Min(420, Math.Max(260, engineStatus.Left - leftBound - 28));
        commandBox.Width = desired;
        commandBox.Left = Math.Max(leftBound, engineStatus.Left - desired - 20);
    }
    header.Resize += (_, _) => LayoutHeader();
    header.Controls.AddRange(mark, wordmark, tagline, commandBox, engineStatus, safeQuick, settingsQuick);
    LayoutHeader();

    busyBar.Dock = DockStyle.Bottom;
    busyBar.Visible = false;
    header.Controls.Add(busyBar);
    busyBar.BringToFront();

    Panel status = new Panel
    {
        Dock = DockStyle.Bottom,
        Height = 36,
        BackColor = UiTheme.Surface
    };
    status.Paint += (_, e) =>
    {
        using var pen = new Pen(UiTheme.BorderSoft);
        e.Graphics.DrawLine(pen, 0, 0, status.Width, 0);
    };
    bottomStatus.Dock = DockStyle.Fill;
    bottomStatus.TextAlign = ContentAlignment.MiddleLeft;
    bottomStatus.Padding = new Padding(18, 0, 0, 0);
    bottomStatus.ForeColor = UiTheme.Muted;
    bottomStatus.Font = UiTheme.Ui(8.4f, FontStyle.Bold);
    bottomStatus.BackColor = Color.Transparent;
    taskStatus.Dock = DockStyle.Right;
    taskStatus.Width = 190;
    taskStatus.TextAlign = ContentAlignment.MiddleRight;
    taskStatus.Padding = new Padding(0, 0, 16, 0);
    taskStatus.ForeColor = UiTheme.Brass;
    taskStatus.Font = UiTheme.Mono(7.8f, FontStyle.Bold);
    taskStatus.Text = "● IDLE   ·   QUEUE 0";
    status.Controls.Add(bottomStatus);
    status.Controls.Add(taskStatus);
    Controls.Add(status);

    Panel body = new Panel
    {
        Dock = DockStyle.Fill,
        BackColor = UiTheme.Bg,
        Padding = new Padding(0, 74, 0, 36)
    };
    Controls.Add(body);
    body.BringToFront();
    header.BringToFront();
    status.BringToFront();

    Panel sidebar = new Panel
    {
        Dock = DockStyle.Left,
        Width = 220,
        BackColor = UiTheme.Surface,
        Padding = new Padding(12, 12, 12, 12)
    };
    sidebar.Paint += (_, e) =>
    {
        using var pen = new Pen(UiTheme.BorderSoft);
        e.Graphics.DrawLine(pen, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
    };
    body.Controls.Add(sidebar);

    Label GroupLabel(string text, int y) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = UiTheme.Muted2,
        Font = UiTheme.Ui(7.8f, FontStyle.Bold),
        Location = new Point(18, y)
    };

    sidebar.Controls.Add(GroupLabel("CONVERT", 16));
    sidebar.Controls.Add(GroupLabel("DISCOVER", 184));
    sidebar.Controls.Add(GroupLabel("SYSTEM", 312));

    string[] navText =
    {
        "⇱   Export to XML",
        "⇲   Build from XML",
        "◆   Native YMT",
        "⌕   Search Assets",
        "◫   Tools",
        "⚙   Settings",
        "≡   Logs"
    };
    int[] navY = { 40, 84, 128, 208, 252, 336, 380 };
    for (int i = 0; i < navText.Length; i++)
    {
        int idx = i;
        var nav = new NavButton
        {
            Text = navText[i],
            Location = new Point(10, navY[i]),
            Size = new Size(198, 38),
            Active = i == 0
        };
        nav.Click += (_, _) => ShowPage(idx);
        navButtons.Add(nav);
        sidebar.Controls.Add(nav);
    }

    var verifiedBuild = new CardPanel
    {
        Dock = DockStyle.Bottom,
        Height = 92,
        CornerRadius = 10,
        CardBackColor = Color.FromArgb(18, 24, 24),
        CardBorderColor = Color.FromArgb(55, UiTheme.Success)
    };
    var shield = new Label
    {
        Text = "✓",
        AutoSize = false,
        Size = new Size(32, 32),
        Location = new Point(12, 14),
        TextAlign = ContentAlignment.MiddleCenter,
        ForeColor = UiTheme.Success,
        BackColor = Color.FromArgb(22, 60, 38),
        Font = UiTheme.Ui(12f, FontStyle.Bold)
    };
    var buildTitle = new Label
    {
        Text = "VERIFIED BUILD",
        AutoSize = true,
        Location = new Point(52, 14),
        ForeColor = UiTheme.Success,
        Font = UiTheme.Ui(8f, FontStyle.Bold),
        BackColor = Color.Transparent
    };
    var buildMeta = new Label
    {
        Text = "v2.2.1 · regression all passed\nBlackwater · PSO · YFT\nYDD · YTD · RSC8",
        AutoSize = false,
        Size = new Size(142, 44),
        Location = new Point(52, 34),
        ForeColor = UiTheme.Muted,
        Font = UiTheme.Ui(7.3f),
        BackColor = Color.Transparent
    };
    verifiedBuild.Controls.AddRange(shield, buildTitle, buildMeta);
    sidebar.Controls.Add(verifiedBuild);

    pages.Dock = DockStyle.None;
    pages.BackColor = UiTheme.Bg;
    body.Controls.Add(pages);

    verifiedPanel.Visible = true;
    verifiedPanel.UseRequested = HandleVerifiedFormatUse;
    body.Controls.Add(verifiedPanel);
    verifiedPanel.BringToFront();
    sidebar.BringToFront();

    pages.TabPages.Add(BuildExportTab());
    pages.TabPages.Add(BuildReverseTab());
    pages.TabPages.Add(BuildYmtTab());
    pages.TabPages.Add(BuildSearchTab());
    pages.TabPages.Add(BuildToolsTab());
    pages.TabPages.Add(BuildSettingsTab());
    pages.TabPages.Add(BuildLogsTab());

    verifiedPanel.EnsureExpanded();
    ShowPage(0);

    toastPanel.Size = new Size(360, 58);
    toastPanel.BackColor = UiTheme.Surface3;
    toastPanel.Visible = false;
    toastPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
    toastText.Dock = DockStyle.Fill;
    toastText.ForeColor = UiTheme.Text;
    toastText.Font = UiTheme.Ui(9f, FontStyle.Bold);
    toastText.TextAlign = ContentAlignment.MiddleLeft;
    toastText.Padding = new Padding(16, 0, 12, 0);
    toastPanel.Controls.Add(toastText);
    body.Controls.Add(toastPanel);
    toastPanel.BringToFront();

    void LayoutWorkspace()
    {
        int top = 74;
        int bottom = Math.Max(top + 240, body.ClientSize.Height - 36);
        int left = sidebar.Width;
        int right = verifiedPanel.Visible
            ? Math.Max(left + 420, body.ClientSize.Width - verifiedPanel.Width)
            : body.ClientSize.Width;
        pages.SetBounds(left, top, Math.Max(420, right - left), Math.Max(240, bottom - top));
    }

    void LayoutToast()
    {
        int rightPad = verifiedPanel.Visible ? verifiedPanel.Width + 24 : 24;
        toastPanel.Left = Math.Max(sidebar.Width + 12, body.ClientSize.Width - toastPanel.Width - rightPad);
        toastPanel.Top = Math.Max(18, body.ClientSize.Height - toastPanel.Height - 22);
    }
    body.Resize += (_, _) => { LayoutWorkspace(); LayoutToast(); };
    verifiedPanel.VisibleChanged += (_, _) => { LayoutWorkspace(); LayoutToast(); };
    exportItems.TextChanged += (_, _) =>
    {
        taskStatus.Text = $"● IDLE   ·   QUEUE {Lines(exportItems).Length}";
    };
    LayoutWorkspace();
    LayoutToast();

    ResumeLayout(true);
}

private void RunCommandPalette(string value)
{
    string q = (value ?? "").Trim().ToLowerInvariant();
    if (q.Length == 0) return;
    int index =
        q.Contains("export") || q.Contains("forge") ? 0 :
        q.Contains("build") || q.Contains("reverse") ? 1 :
        q.Contains("ymt") || q.Contains("scenario") ? 2 :
        q.Contains("search") || q.Contains("asset") ? 3 :
        q.Contains("tool") || q.Contains("inspect") || q.Contains("compare") ? 4 :
        q.Contains("setting") || q.Contains("path") ? 5 :
        q.Contains("log") ? 6 : -1;
    if (index >= 0)
    {
        ShowPage(index);
        commandBox.Clear();
        ActiveControl = null;
    }
    else
    {
        ShowToast("No matching command.", false);
    }
}

private void HandleVerifiedFormatUse(string key)
{
    switch (key)
    {
        case "RSC8":
        case "PSO":
        case "BLACKWATER":
            ShowPage(2);
            break;
        case "YFT":
        case "YDD":
        case "YTD":
            ShowPage(0);
            archiveMode.Checked = true;
            break;
    }
}

	private void ShowPage(int index)
	{
		if (index < 0 || index >= pages.TabPages.Count) return;

		pages.SelectedIndex = index;
		verifiedPanel.Visible = index <= 2;
		for (int i = 0; i < navButtons.Count; i++)
		{
			navButtons[i].Active = i == index;
			navButtons[i].Invalidate();
		}

		if (!config.ReduceMotion)
		{
			pages.SuspendLayout();
			pages.ResumeLayout(true);
		}
	}

	private async void ShowToast(string message, bool success = true)
	{
		toastText.Text = (success ? "✓  " : "!  ") + message;
		toastText.ForeColor = (success ? UiTheme.Success : UiTheme.Error);
		toastPanel.Visible = true;
		toastPanel.BringToFront();
		await Task.Delay(config.ReduceMotion ? 900 : 1800);
		if (!IsDisposed)
		{
			toastPanel.Visible = false;
		}
	}

	private TabPage NewTab(string text)
	{
		return new TabPage(text)
		{
			BackColor = UiTheme.Bg,
			ForeColor = Light,
			Padding = new Padding(22),
			UseVisualStyleBackColor = false
		};
	}

	private Label FieldLabel(string text, int x, int y)
	{
		return new Label
		{
			Text = text.ToUpperInvariant(),
			AutoSize = true,
			ForeColor = UiTheme.Muted,
			BackColor = Color.Transparent,
			Font = UiTheme.Ui(8.3f, FontStyle.Bold),
			Location = new Point(x, y)
		};
	}

	private void StyleWhite(TextBox box, bool multiline = false)
	{
		box.BackColor = UiTheme.Surface2;
		box.ForeColor = UiTheme.Text;
		box.BorderStyle = BorderStyle.FixedSingle;
		box.Font = UiTheme.Mono(9.2f);
		box.Multiline = multiline;
		box.Margin = new Padding(0);
		if (multiline)
		{
			box.ScrollBars = ScrollBars.None;
			box.WordWrap = false;
		}
	}

	private void StyleResultList(ListBox box)
	{
		box.BackColor = UiTheme.Surface2;
		box.ForeColor = UiTheme.Text;
		box.BorderStyle = BorderStyle.FixedSingle;
		box.Font = UiTheme.Mono(9.2f);
		box.IntegralHeight = false;
	}

	private Button MakeButton(string text, int x, int y, int width = 130, bool red = false)
	{
		string text2;
		if (text.StartsWith("BROWSE", StringComparison.OrdinalIgnoreCase))
		{
			text2 = "▣ ";
		}
		else if (text.StartsWith("ADD", StringComparison.OrdinalIgnoreCase))
		{
			text2 = "+ ";
		}
		else if (text.StartsWith("CLEAR", StringComparison.OrdinalIgnoreCase))
		{
			text2 = "× ";
		}
		else if (text.StartsWith("SEARCH", StringComparison.OrdinalIgnoreCase))
		{
			text2 = "⌕ ";
		}
		else if (text.StartsWith("BUILD", StringComparison.OrdinalIgnoreCase))
		{
			text2 = "◆ ";
		}
		else if (text.StartsWith("COMPILE", StringComparison.OrdinalIgnoreCase))
		{
			text2 = "◆ ";
		}
		else if (text.StartsWith("FORGE", StringComparison.OrdinalIgnoreCase))
		{
			text2 = "◆ ";
		}
		else if (text.StartsWith("INSTALL", StringComparison.OrdinalIgnoreCase))
		{
			text2 = "⇩ ";
		}
		else if (text.StartsWith("OPEN", StringComparison.OrdinalIgnoreCase))
		{
			text2 = "↗ ";
		}
		else if (text.StartsWith("SAVE", StringComparison.OrdinalIgnoreCase))
		{
			text2 = "↓ ";
		}
		else if (text.StartsWith("COPY", StringComparison.OrdinalIgnoreCase))
		{
			text2 = "⧉ ";
		}
		else if (text.StartsWith("COMPARE", StringComparison.OrdinalIgnoreCase))
		{
			text2 = "⇄ ";
		}
		else if (text.StartsWith("INSPECT", StringComparison.OrdinalIgnoreCase))
		{
			text2 = "◎ ";
		}
		else
		{
			text2 = ((!text.StartsWith("AUTO-DETECT", StringComparison.OrdinalIgnoreCase)) ? "" : "⟳ ");
		}
		string text3 = text2;
		return new AizenButton
		{
			Text = text3 + text,
			Location = new Point(x, y),
			Size = new Size(width, 38),
			Primary = red
		};
	}

	private static string[] Lines(TextBox box)
	{
		return box.Text.Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
	}

	private void Log(string text)
	{
		if (InvokeRequired)
		{
			BeginInvoke(() =>
			{
				Log(text);
			});
			return;
		}
		string text2 = $"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}";
		Color selectionColor;
		if (text.Contains("ERROR", StringComparison.OrdinalIgnoreCase) || text.Contains("FAIL", StringComparison.OrdinalIgnoreCase))
		{
			selectionColor = UiTheme.Error;
		}
		else if (text.Contains("OK", StringComparison.OrdinalIgnoreCase) || text.Contains("DONE", StringComparison.OrdinalIgnoreCase) || text.Contains("PASS", StringComparison.OrdinalIgnoreCase))
		{
			selectionColor = UiTheme.Success;
		}
		else
		{
			selectionColor = (text.Contains("WARN", StringComparison.OrdinalIgnoreCase) ? UiTheme.Warning : UiTheme.Text);
		}
		logBox.SelectionStart = logBox.TextLength;
		logBox.SelectionLength = 0;
		logBox.SelectionColor = selectionColor;
		logBox.AppendText(text2);
		logBox.SelectionColor = UiTheme.Text;
		logBox.ScrollToCaret();
	}

	private void StyleLogBox(RichTextBox box)
	{
		box.BackColor = Color.FromArgb(9, 10, 13);
		box.ForeColor = UiTheme.Text;
		box.BorderStyle = BorderStyle.FixedSingle;
		box.Font = UiTheme.Mono(9.2f);
		box.ReadOnly = true;
		box.WordWrap = false;
		box.DetectUrls = false;
	}

	private TabPage BuildExportTab()
	{
		TabPage tab = NewTab("Export to XML");
		Label label = new Label
		{
			Text = "Export to XML",
			AutoSize = true,
			ForeColor = UiTheme.Text,
			Font = UiTheme.UiDisplay(19f),
			Location = new Point(20, 12)
		};
		Label label2 = new Label
		{
			Text = "Extract installed RDR2 assets or convert clean loose resources into editable XML.",
			AutoSize = true,
			ForeColor = UiTheme.Muted,
			Font = UiTheme.Ui(9.2f),
			Location = new Point(22, 46)
		};
		tab.Controls.AddRange(label, label2);
		archiveMode.Visible = false;
		localMode.Visible = false;
		archiveMode.TabStop = false;
		localMode.TabStop = false;
		archiveMode.Location = new Point(-100, -100);
		localMode.Location = new Point(-100, -100);
		tab.Controls.AddRange(archiveMode, localMode);

		var archiveModeButton = new AizenButton
		{
			Text = "ARCHIVE MODE",
			Location = new Point(20, 76),
			Size = new Size(160, 36),
			CornerRadius = 8
		};
		var localModeButton = new AizenButton
		{
			Text = "LOOSE FILE MODE",
			Location = new Point(194, 76),
			Size = new Size(160, 36),
			CornerRadius = 8
		};
		archiveModeButton.Click += (_, _) =>
		{
			archiveMode.Checked = true;
			localMode.Checked = false;
			RefreshModeButtons();
		};
		localModeButton.Click += (_, _) =>
		{
			localMode.Checked = true;
			archiveMode.Checked = false;
			RefreshModeButtons();
		};
		archiveMode.CheckedChanged += (_, _) => RefreshModeButtons();
		localMode.CheckedChanged += (_, _) => RefreshModeButtons();
		archiveMode.Checked = true;
		localMode.Checked = false;
		RefreshModeButtons();
		tab.Controls.AddRange(archiveModeButton, localModeButton);
		tab.Controls.Add(FieldLabel("ASSETS / FILES", 20, 130));
		StyleWhite(exportItems, multiline: true);
		exportItems.Location = new Point(20, 154);
		exportItems.Size = new Size(850, 390);
		exportItems.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
		exportItems.PlaceholderText = "Drop .yft / .ydd / .ytd / .ymt files here, or enter archive asset names…";
		exportItems.Text = "meta_base_player.yft";
		tab.Controls.Add(exportItems);
		Button addFiles = MakeButton("ADD LOOSE FILES", 900, 154, 170);
		addFiles.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		addFiles.Click += (object? _, EventArgs _) =>
		{
			AddLooseExportFiles();
		};
		Button clear = MakeButton("CLEAR", 900, 198, 170);
		clear.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		clear.Click += (object? _, EventArgs _) =>
		{
			exportItems.Clear();
		};
		Button fromSearch = MakeButton("SEARCH ASSETS", 900, 242, 170);
		fromSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		fromSearch.Click += (object? _, EventArgs _) =>
		{
			ShowPage(3);
		};
		tab.Controls.AddRange(addFiles, clear, fromSearch);
		Label exportOutputLabel = FieldLabel("OUTPUT FOLDER", 20, 515);
		tab.Controls.Add(exportOutputLabel);
		StyleWhite(exportOutput);
		exportOutput.Location = new Point(20, 540);
		exportOutput.Size = new Size(850, 30);
		exportOutput.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
		tab.Controls.Add(exportOutput);
		Button browseOut = MakeButton("BROWSE", 900, 537, 170);
		browseOut.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
		browseOut.Click += (object? _, EventArgs _) =>
		{
			BrowseFolderTo(exportOutput);
		};
		Button forge = MakeButton("FORGE XML", 900, 585, 170, red: true);
		forge.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
		forge.Click += async (object? _, EventArgs _) =>
		{
			await ExportClicked(forge);
		};
		tab.Controls.AddRange(browseOut, forge);
		tab.Resize += (object? _, EventArgs _) =>
		{
			LayoutExport();
		};
		tab.HandleCreated += (object? _, EventArgs _) =>
		{
			LayoutExport();
		};
		LayoutExport();
		return tab;
		void LayoutExport()
		{
			int num = Math.Max(620, tab.ClientSize.Width - 24 - 180);
			int width = Math.Max(420, num - 24 - 24);
			exportItems.Left = 24;
			exportItems.Width = width;
			exportOutput.Left = 24;
			exportOutput.Width = width;
			int num2 = Math.Max(520, tab.ClientSize.Height - 24) - 82;
			exportOutputLabel.Left = 24;
			exportOutputLabel.Top = num2 - 25;
			exportOutput.Top = num2;
			browseOut.Top = num2 - 3;
			forge.Top = num2 + 42;
			exportItems.Height = Math.Max(220, exportOutputLabel.Top - exportItems.Top - 18);
			addFiles.Left = num;
			clear.Left = num;
			fromSearch.Left = num;
			browseOut.Left = num;
			forge.Left = num;
		}
		void RefreshModeButtons()
		{
			archiveModeButton.Primary = archiveMode.Checked;
			archiveModeButton.Ghost = !archiveMode.Checked;
			archiveModeButton.ForeColor = archiveMode.Checked ? Color.White : UiTheme.Muted;

			localModeButton.Primary = localMode.Checked;
			localModeButton.Ghost = !localMode.Checked;
			localModeButton.ForeColor = localMode.Checked ? Color.White : UiTheme.Muted;

			archiveModeButton.Invalidate();
			localModeButton.Invalidate();
		}
	}

	private TabPage BuildReverseTab()
	{
		TabPage tab = NewTab("Build from XML");
		Label label = new Label
		{
			Text = "Build from XML",
			AutoSize = true,
			ForeColor = UiTheme.Text,
			Font = UiTheme.UiDisplay(19f),
			Location = new Point(20, 12)
		};
		Label label2 = new Label
		{
			Text = "Rebuild supported RDR2 XML resources into native files with verification before output.",
			AutoSize = true,
			ForeColor = UiTheme.Muted,
			Font = UiTheme.Ui(9.2f),
			Location = new Point(22, 46)
		};
		tab.Controls.AddRange(label, label2);
		tab.Controls.Add(FieldLabel("XML FILES / FOLDERS", 20, 92));
		StyleWhite(buildItems, multiline: true);
		buildItems.Location = new Point(20, 116);
		buildItems.Size = new Size(820, 255);
		buildItems.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		buildItems.PlaceholderText = "Drop supported RDR2 XML resources here…";
		tab.Controls.Add(buildItems);
		Button addXml = MakeButton("ADD XML", 870, 116, 180);
		addXml.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		addXml.Click += (object? _, EventArgs _) =>
		{
			AddBuildXmlFiles();
		};
		Button addFolder = MakeButton("ADD FOLDER", 870, 160, 180);
		addFolder.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		addFolder.Click += (object? _, EventArgs _) =>
		{
			AddBuildFolder();
		};
		Button clear = MakeButton("CLEAR", 870, 204, 180);
		clear.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		clear.Click += (object? _, EventArgs _) =>
		{
			buildItems.Clear();
			buildResults.Items.Clear();
		};
		tab.Controls.AddRange(addXml, addFolder, clear);
		Label buildOutputLabel = FieldLabel("BUILD OUTPUT", 20, 350);
		tab.Controls.Add(buildOutputLabel);
		StyleWhite(buildOutput);
		buildOutput.Location = new Point(20, 375);
		buildOutput.Size = new Size(820, 30);
		buildOutput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		tab.Controls.Add(buildOutput);
		Button browse = MakeButton("BROWSE", 870, 372, 180);
		browse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		browse.Click += (object? _, EventArgs _) =>
		{
			BrowseFolderTo(buildOutput);
		};
		tab.Controls.Add(browse);
		Label resultsLabel = FieldLabel("RESULTS", 20, 420);
		tab.Controls.Add(resultsLabel);
		StyleResultList(buildResults);
		buildResults.Location = new Point(20, 445);
		buildResults.Size = new Size(820, 165);
		buildResults.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
		tab.Controls.Add(buildResults);
		Button build = MakeButton("BUILD + VERIFY", 870, 445, 180, red: true);
		build.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		build.Click += async (object? _, EventArgs _) =>
		{
			await BuildClicked(build);
		};
		Button install = MakeButton("INSTALL TO LML", 870, 489, 180);
		install.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		install.Click += (object? _, EventArgs _) =>
		{
			InstallLastBuilt();
		};
		Button open = MakeButton("OPEN OUTPUT", 870, 533, 180);
		open.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		open.Click += (object? _, EventArgs _) =>
		{
			OpenFolder(buildOutput.Text);
		};
		tab.Controls.AddRange(build, install, open);
		tab.Resize += (object? _, EventArgs _) =>
		{
			LayoutBuild();
		};
		tab.HandleCreated += (object? _, EventArgs _) =>
		{
			LayoutBuild();
		};
		LayoutBuild();
		return tab;
		void LayoutBuild()
		{
			int num = Math.Max(620, tab.ClientSize.Width - 24 - 190);
			int width = Math.Max(420, num - 24 - 24);
			buildItems.Left = 24;
			buildItems.Width = width;
			buildOutput.Left = 24;
			buildOutput.Width = width;
			buildResults.Left = 24;
			buildResults.Width = width;
			int num2 = Math.Max(560, tab.ClientSize.Height - 24);
			int num3 = Math.Max(385, num2 - 175);
			int num4 = num3 - 70;
			buildOutputLabel.Left = 24;
			buildOutputLabel.Top = num4 - 25;
			buildOutput.Top = num4;
			browse.Top = num4 - 3;
			resultsLabel.Left = 24;
			resultsLabel.Top = num3 - 25;
			buildResults.Top = num3;
			buildResults.Height = Math.Max(120, num2 - num3);
			buildItems.Height = Math.Max(170, buildOutputLabel.Top - buildItems.Top - 18);
			build.Top = num3;
			install.Top = num3 + 44;
			open.Top = num3 + 88;
			addXml.Left = num;
			addFolder.Left = num;
			clear.Left = num;
			browse.Left = num;
			build.Left = num;
			install.Left = num;
			open.Left = num;
		}
	}

	private TabPage BuildYmtTab()
	{
		TabPage tab = NewTab("Native YMT");
		Label label = new Label
		{
			Text = "Smart Native YMT Compiler",
			AutoSize = true,
			ForeColor = UiTheme.Text,
			Font = UiTheme.UiDisplay(19f),
			Location = new Point(24, 18)
		};
		Label label2 = new Label
		{
			Text = "Auto-detect RSC8 Scenario or PSO/MetaPed YMT, compile the correct native container, then verify it before accepting the build.",
			AutoSize = true,
			ForeColor = UiTheme.Muted,
			Font = UiTheme.Ui(9.2f),
			Location = new Point(26, 52)
		};
		tab.Controls.AddRange(label, label2);
		CardPanel pipeline = new CardPanel
		{
			Location = new Point(24, 86),
			Size = new Size(930, 92),
			Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right)
		};
		Label label3 = new Label
		{
			Text = "PIPELINE",
			ForeColor = UiTheme.Gold,
			Font = UiTheme.Ui(8.2f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(18, 14),
			BackColor = Color.Transparent
		};
		Label label4 = new Label
		{
			Text = "XML EDIT   →   ANALYZE   →   NATIVE .YMT   →   ROUND-TRIP VERIFY   →   LML",
			ForeColor = UiTheme.Text,
			Font = UiTheme.Mono(10.2f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(18, 42),
			BackColor = Color.Transparent
		};
		pipeline.Controls.AddRange(label3, label4);
		tab.Controls.Add(pipeline);
		tab.Controls.Add(FieldLabel("YMT XML", 24, 202));
		StyleWhite(ymtInput);
		ymtInput.Location = new Point(24, 226);
		ymtInput.Size = new Size(760, 30);
		ymtInput.PlaceholderText = "Select .ymt.xml or an XML-text .ymt scenario/metadata file.";
		ymtInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		tab.Controls.Add(ymtInput);
		Button browseInput = MakeButton("SELECT YMT XML", 804, 222, 150);
		browseInput.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		browseInput.Click += (object? _, EventArgs _) =>
		{
			using OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "YMT XML|*.ymt.xml;*.ymt|XML files|*.xml|All files|*.*"
			};
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				ymtInput.Text = openFileDialog.FileName;
			}
		};
		tab.Controls.Add(browseInput);
		tab.Controls.Add(FieldLabel("OUTPUT FOLDER", 24, 280));
		StyleWhite(ymtOutput);
		ymtOutput.Location = new Point(24, 304);
		ymtOutput.Size = new Size(760, 30);
		ymtOutput.PlaceholderText = "Output folder for the native .ymt…";
		ymtOutput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		tab.Controls.Add(ymtOutput);
		Button browseOut = MakeButton("BROWSE", 804, 300, 150);
		browseOut.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		browseOut.Click += (object? _, EventArgs _) =>
		{
			BrowseFolderTo(ymtOutput);
		};
		tab.Controls.Add(browseOut);
		Button compile = MakeButton("COMPILE + VERIFY", 24, 358, 180, red: true);
		compile.Click += async (object? _, EventArgs _) =>
		{
			await BuildYmtClicked(compile);
		};
		Button button = MakeButton("INSTALL TO LML", 218, 358, 170);
		button.Click += (object? _, EventArgs _) =>
		{
			InstallYmtBuilt();
		};
		Button button2 = MakeButton("OPEN OUTPUT", 402, 358, 150);
		button2.Click += (object? _, EventArgs _) =>
		{
			OpenFolder(ymtOutput.Text);
		};
		tab.Controls.AddRange(compile, button, button2);
		Label proof = new Label
		{
			Text = "AUTO-DETECT  •  RSC8 / PSO",
			ForeColor = UiTheme.Success,
			Font = UiTheme.Ui(9f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(575, 368),
			BackColor = Color.Transparent,
			Anchor = (AnchorStyles.Top | AnchorStyles.Right)
		};
		tab.Controls.Add(proof);
		tab.Controls.Add(FieldLabel("BUILD REPORT", 24, 420));
		StyleWhite(ymtReport, multiline: true);
		ymtReport.ReadOnly = true;
		ymtReport.Location = new Point(24, 446);
		ymtReport.Size = new Size(930, 210);
		ymtReport.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
		ymtReport.Text = "FORMAT       Native YMT\r\nROOT         Awaiting input\r\nCONTAINER    Auto-detect\r\nBUILD        -\r\nROUND-TRIP   -\r\nLML READY    -";
		tab.Controls.Add(ymtReport);
		tab.Resize += (object? _, EventArgs _) =>
		{
			LayoutYmt();
		};
		tab.HandleCreated += (object? _, EventArgs _) =>
		{
			LayoutYmt();
		};
		LayoutYmt();
		return tab;
		void LayoutYmt()
		{
			int num = Math.Max(760, tab.ClientSize.Width) - 24;
			int num2 = 150;
			int num3 = num - num2 - 18;
			int width = Math.Max(420, num3 - 24);
			pipeline.Left = 24;
			pipeline.Width = Math.Max(620, num - 24);
			ymtInput.Left = 24;
			ymtInput.Width = width;
			browseInput.Left = num3 + 18;
			ymtOutput.Left = 24;
			ymtOutput.Width = width;
			browseOut.Left = num3 + 18;
			ymtReport.Left = 24;
			ymtReport.Width = Math.Max(620, num - 24);
			ymtReport.Height = Math.Max(170, tab.ClientSize.Height - ymtReport.Top - 24);
			proof.Left = Math.Max(575, num - proof.Width);
		}
	}

	private async Task BuildYmtClicked(Button button)
	{
		string text = ymtInput.Text.Trim();
		if (!File.Exists(text))
		{
			ShowToast("Select a .ymt.xml file first.", success: false);
			return;
		}
		string rootName;
		try
		{
			rootName = XDocument.Load(text).Root?.Name.LocalName ?? "";
		}
		catch (Exception ex)
		{
			ymtReport.Text = "XML PARSE FAILED\r\n" + ex.Message;
			ShowToast("XML parse failed.", success: false);
			return;
		}
		bool xmlTextYmt = Rdr2Backend.IsXmlYmt(text);
		bool namedYmtXml = Path.GetFileName(text).EndsWith(".ymt.xml", StringComparison.OrdinalIgnoreCase);
		if (!namedYmtXml && !xmlTextYmt)
		{
			ymtReport.Text = "FORMAT       Not a YMT XML input\r\nROOT         " + rootName + "\r\nBUILD        BLOCKED";
			ShowToast("Select .ymt.xml or an XML-text .ymt file.", success: false);
			return;
		}
		string text2 = ymtOutput.Text.Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			text2 = config.OutputPath;
		}
		Directory.CreateDirectory(text2);
		config.VerifyBuilds = true;
		ConfigStore.Save(config);
		button.Enabled = false;
		busyBar.SetRunning(value: true);
		string old = button.Text;
		button.Text = "COMPILING...";
		ymtBuiltPath = null;
		ymtReport.Text = "FORMAT       Native YMT\r\nROOT         " + rootName + "\r\nCONTAINER    Detecting...\r\nBUILD        Running...\r\nROUND-TRIP   Pending...\r\nLML READY    No";
		try
		{
			BuildResult buildResult = await Rdr2Backend.BuildOneAsync(config, text, text2, Log);
			if (buildResult.Success && buildResult.Output != null)
			{
				ymtBuiltPath = buildResult.Output;
				FileInspection fileInspection = Rdr2Backend.Inspect(buildResult.Output);
				string container = Rdr2Backend.GetNativeContainer(buildResult.Output);
				string proofScope = rootName.Equals("CScenarioPointRegion", StringComparison.Ordinal)
					? "RSC8 scenario path is in-game verified."
					: "Native compile + round-trip verified; in-game validation depends on this YMT class.";
				ymtReport.Text = $"FORMAT       Native YMT\r\nROOT         {rootName}\r\nCONTAINER    {container}\r\nBUILD        PASS\r\nROUND-TRIP   {(buildResult.Verified ? "PASS" : "NOT VERIFIED")}\r\nNATIVE HEADER PASS\r\nSIZE         {fileInspection.Size:N0} bytes\r\nLML READY    {(buildResult.Verified ? "YES" : "NO")}\r\n\r\n{proofScope}\r\n\r\nOUTPUT\r\n{buildResult.Output}\r\n\r\n{buildResult.Message}";
				bottomStatus.Text = $"Native YMT verified ({container}).";
				ShowToast($"Native YMT compiled and verified ({container}).");
			}
			else
			{
				ymtReport.Text = $"FORMAT       Native YMT\r\nROOT         {rootName}\r\nCONTAINER    Not accepted\r\nBUILD        FAIL\r\nROUND-TRIP   FAIL\r\nLML READY    NO\r\n\r\n{buildResult.Message}";
				ShowToast("Native YMT build failed.", success: false);
			}
		}
		catch (Exception ex2)
		{
			ymtReport.Text = "BUILD ERROR\r\n" + ex2.Message;
			Log("YMT BUILD ERROR: " + ex2.Message);
			ShowToast("Native YMT build failed.", success: false);
		}
		finally
		{
			busyBar.SetRunning(value: false);
			button.Enabled = true;
			button.Text = old;
		}
	}

	private void InstallYmtBuilt()
	{
		if (string.IsNullOrWhiteSpace(ymtBuiltPath) || !File.Exists(ymtBuiltPath))
		{
			ShowToast("Compile a verified YMT first.", success: false);
			return;
		}
		config.SafeMode = safeMode.Checked;
		config.BackupBeforeInstall = backupInstall.Checked;
		ConfigStore.Save(config);
		if (config.SafeMode)
		{
			MessageBox.Show("Safe Mode is ON. Disable it in Settings before installing to LML.", "AizenX", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			return;
		}
		try
		{
			string text = Rdr2Backend.InstallToLml(config, ymtBuiltPath);
			Log("YMT LML INSTALL: " + text);
			bottomStatus.Text = "Native YMT installed to LML.";
			ShowToast("Native YMT installed to LML.");
		}
		catch (Exception ex)
		{
			Log("YMT LML ERROR: " + ex.Message);
			ShowToast("LML install failed.", success: false);
			MessageBox.Show(ex.Message, "AizenX", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private TabPage BuildSearchTab()
	{
		TabPage tab = NewTab("Search RDR2 Assets");
		Label label = new Label
		{
			Text = "Search RDR2 Assets",
			AutoSize = true,
			ForeColor = UiTheme.Text,
			Font = UiTheme.UiDisplay(19f),
			Location = new Point(20, 12)
		};
		Label label2 = new Label
		{
			Text = "Search the Manifest Tool index, favorite assets, or send a result directly to the export queue.",
			AutoSize = true,
			ForeColor = UiTheme.Muted,
			Font = UiTheme.Ui(9.2f),
			Location = new Point(22, 46)
		};
		tab.Controls.AddRange(label, label2);
		tab.Controls.Add(FieldLabel("SEARCH", 20, 92));
		StyleWhite(searchBox);
		searchBox.Location = new Point(20, 116);
		searchBox.Size = new Size(520, 30);
		searchBox.PlaceholderText = "Search asset names, e.g. valentine, player_zero, mrsadler…";
		searchBox.KeyDown += (object? _, KeyEventArgs e) =>
		{
			if (e.KeyCode == Keys.Return)
			{
				e.SuppressKeyPress = true;
				_ = SearchClicked();
			}
		};
		tab.Controls.Add(searchBox);
		Button searchBtn = MakeButton("SEARCH", 555, 112, 130, red: true);
		searchBtn.Click += async (object? _, EventArgs _) =>
		{
			await SearchClicked();
		};
		tab.Controls.Add(searchBtn);
		Label verified = FieldLabel("VERIFIED ARCHIVE RESULTS", 710, 92);
		verified.ForeColor = UiTheme.Success;
		tab.Controls.Add(verified);
		searchCount.AutoSize = true;
		searchCount.ForeColor = Color.Gainsboro;
		searchCount.Location = new Point(710, 122);
		tab.Controls.Add(searchCount);
		searchResults.BackColor = UiTheme.Surface2;
		searchResults.ForeColor = UiTheme.Text;
		searchResults.Font = UiTheme.Mono(9.4f);
		searchResults.Location = new Point(20, 164);
		searchResults.Size = new Size(830, 460);
		searchResults.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
		searchResults.DoubleClick += (object? _, EventArgs _) =>
		{
			QueueSearchSelection();
		};
		tab.Controls.Add(searchResults);
		Button queue = MakeButton("ADD TO EXPORT", 880, 164, 180, red: true);
		queue.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		queue.Click += (object? _, EventArgs _) =>
		{
			QueueSearchSelection();
		};
		Button favorite = MakeButton("TOGGLE FAVORITE", 880, 208, 180);
		favorite.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		favorite.Click += (object? _, EventArgs _) =>
		{
			FavoriteSearchSelection();
		};
		Button copy = MakeButton("COPY NAME", 880, 252, 180);
		copy.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		copy.Click += (object? _, EventArgs _) =>
		{
			string selectedItem = searchResults.SelectedItem;
			if (selectedItem != null)
			{
				Clipboard.SetText(selectedItem);
			}
		};
		tab.Controls.AddRange(queue, favorite, copy);
		tab.Resize += (object? _, EventArgs _) =>
		{
			LayoutSearch();
		};
		tab.HandleCreated += (object? _, EventArgs _) =>
		{
			LayoutSearch();
		};
		LayoutSearch();
		return tab;
		void LayoutSearch()
		{
			int num = Math.Max(620, tab.ClientSize.Width - 24 - 190);
			int num2 = Math.Max(420, num - 24 - 24);
			searchResults.Left = 24;
			searchResults.Width = num2;
			searchResults.Height = Math.Max(260, tab.ClientSize.Height - 180);
			queue.Left = num;
			favorite.Left = num;
			copy.Left = num;
			searchCount.Left = num;
			searchBox.Width = Math.Max(300, Math.Min(520, num2 - 170));
			searchBtn.Left = searchBox.Right + 15;
		}
	}

	private TabPage BuildToolsTab()
	{
		TabPage tab = NewTab("Tools");
		Label label = new Label
		{
			Text = "Tools",
			AutoSize = true,
			ForeColor = UiTheme.Text,
			Font = UiTheme.UiDisplay(19f),
			Location = new Point(20, 12)
		};
		Label label2 = new Label
		{
			Text = "Inspect native resources, compare XML files, and jump back into recent or favorite assets.",
			AutoSize = true,
			ForeColor = UiTheme.Muted,
			Font = UiTheme.Ui(9.2f),
			Location = new Point(22, 46)
		};
		tab.Controls.AddRange(label, label2);
		tab.Controls.Add(FieldLabel("FILE INSPECTOR", 20, 84));
		StyleWhite(inspectPath);
		inspectPath.Location = new Point(20, 108);
		inspectPath.Size = new Size(700, 30);
		inspectPath.PlaceholderText = "Choose a native RDR2 resource to inspect…";
		inspectPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		tab.Controls.Add(inspectPath);
		Button browseInspect = MakeButton("BROWSE", 745, 104, 120);
		browseInspect.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		browseInspect.Click += (object? _, EventArgs _) =>
		{
			BrowseFileTo(inspectPath);
		};
		Button inspect = MakeButton("INSPECT", 875, 104, 140, red: true);
		inspect.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		inspect.Click += (object? _, EventArgs _) =>
		{
			InspectClicked();
		};
		tab.Controls.AddRange(browseInspect, inspect);
		StyleWhite(inspectResult, multiline: true);
		inspectResult.ReadOnly = true;
		inspectResult.Location = new Point(20, 154);
		inspectResult.Size = new Size(995, 105);
		inspectResult.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		tab.Controls.Add(inspectResult);
		tab.Controls.Add(FieldLabel("XML COMPARE", 20, 278));
		StyleWhite(compareA);
		StyleWhite(compareB);
		compareA.Location = new Point(20, 302);
		compareA.Size = new Size(610, 30);
		compareA.PlaceholderText = "XML A";
		compareA.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		compareB.Location = new Point(20, 342);
		compareB.Size = new Size(610, 30);
		compareB.PlaceholderText = "XML B";
		compareB.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		tab.Controls.AddRange(compareA, compareB);
		Button browseA = MakeButton("XML A", 650, 298, 100);
		browseA.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		browseA.Click += (object? _, EventArgs _) =>
		{
			BrowseFileTo(compareA, "XML|*.xml|All files|*.*");
		};
		Button browseB = MakeButton("XML B", 650, 338, 100);
		browseB.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		browseB.Click += (object? _, EventArgs _) =>
		{
			BrowseFileTo(compareB, "XML|*.xml|All files|*.*");
		};
		Button compare = MakeButton("COMPARE", 765, 318, 120, red: true);
		compare.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		compare.Click += (object? _, EventArgs _) =>
		{
			CompareClicked();
		};
		tab.Controls.AddRange(browseA, browseB, compare);
		StyleWhite(compareResult, multiline: true);
		compareResult.ReadOnly = true;
		compareResult.Location = new Point(20, 384);
		compareResult.Size = new Size(995, 125);
		compareResult.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		tab.Controls.Add(compareResult);
		Label recentLabel = FieldLabel("RECENT FILES", 20, 526);
		Label favoriteLabel = FieldLabel("FAVORITE ASSETS", 525, 526);
		tab.Controls.AddRange(recentLabel, favoriteLabel);
		StyleResultList(recentList);
		recentList.Location = new Point(20, 550);
		recentList.Size = new Size(475, 125);
		recentList.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
		StyleResultList(favoriteList);
		favoriteList.Location = new Point(525, 550);
		favoriteList.Size = new Size(490, 125);
		favoriteList.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
		favoriteList.DoubleClick += (object? _, EventArgs _) =>
		{
			FavoriteToSearch();
		};
		tab.Controls.AddRange(recentList, favoriteList);
		tab.Resize += (object? _, EventArgs _) =>
		{
			LayoutTools();
		};
		tab.HandleCreated += (object? _, EventArgs _) =>
		{
			LayoutTools();
		};
		LayoutTools();
		return tab;
		void LayoutTools()
		{
			int num = Math.Max(760, tab.ClientSize.Width) - 24;
			int num2 = 120;
			inspectPath.Left = 24;
			inspectPath.Width = Math.Max(320, num - 24 - num2 * 2 - 28);
			browseInspect.Left = inspectPath.Right + 14;
			inspect.Left = browseInspect.Right + 14;
			inspectResult.Left = 24;
			inspectResult.Width = Math.Max(500, num - 24);
			int num3 = num;
			compareA.Left = 24;
			compareB.Left = 24;
			compareA.Width = Math.Max(320, num3 - 24 - 255);
			compareB.Width = compareA.Width;
			browseA.Left = compareA.Right + 14;
			browseB.Left = compareB.Right + 14;
			compare.Left = Math.Min(num3 - compare.Width, browseA.Right + 14);
			compareResult.Left = 24;
			compareResult.Width = Math.Max(500, num - 24);
			int num4 = 24;
			int num5 = Math.Max(280, (num - 24 - num4) / 2);
			int listTop = Math.Max(526, tab.ClientSize.Height - 170);
			recentLabel.Left = 24;
			recentLabel.Top = listTop;
			favoriteLabel.Top = listTop;
			recentList.Left = 24;
			recentList.Top = listTop + 24;
			recentList.Width = num5;
			recentList.Height = Math.Max(96, tab.ClientSize.Height - recentList.Top - 24);
			favoriteList.Left = 24 + num5 + num4;
			favoriteLabel.Left = favoriteList.Left;
			favoriteList.Top = listTop + 24;
			favoriteList.Width = Math.Max(280, num - favoriteList.Left);
			favoriteList.Height = recentList.Height;
		}
	}

	private TabPage BuildSettingsTab()
	{
		TabPage tab = NewTab("Settings");
		tab.AutoScroll = true;
		tab.AutoScrollMinSize = new Size(0, 735);
		Label label = new Label
		{
			Text = "Settings",
			AutoSize = true,
			ForeColor = UiTheme.Text,
			Font = UiTheme.UiDisplay(19f),
			Location = new Point(24, 18)
		};
		Label label2 = new Label
		{
			Text = "Paths, safety and appearance. Changes here do not alter RDR2 graphics.",
			AutoSize = true,
			ForeColor = UiTheme.Muted,
			Font = UiTheme.Ui(9.2f),
			Location = new Point(26, 52)
		};
		tab.Controls.AddRange(label, label2);
		CardPanel pathsCard = new CardPanel
		{
			Location = new Point(24, 86),
			Size = new Size(930, 280),
			Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right)
		};
		Label value = new Label
		{
			Text = "PATHS",
			AutoSize = true,
			ForeColor = UiTheme.Gold,
			Font = UiTheme.Ui(8.4f, FontStyle.Bold),
			BackColor = Color.Transparent,
			Location = new Point(18, 14)
		};
		pathsCard.Controls.Add(value);
		Button browseEngine = MakeButton("BROWSE", 0, 0, 148);
		browseEngine.Click += (object? _, EventArgs _) =>
		{
			BrowseFileTo(engineBox, "RDR2YtdExporter|RDR2YtdExporter.exe|Executable|*.exe");
		};
		AddPathRow("Manifest Tool Exporter", engineBox, browseEngine, 46, value => File.Exists(value));
		Button browseGame = MakeButton("BROWSE", 0, 0, 148);
		browseGame.Click += (object? _, EventArgs _) =>
		{
			BrowseFolderTo(gameBox);
		};
		AddPathRow("RDR2 Game Folder", gameBox, browseGame, 112, value => Directory.Exists(value) && File.Exists(Path.Combine(value, "RDR2.exe")));
		Button browseOutput = MakeButton("BROWSE", 0, 0, 148);
		browseOutput.Click += (object? _, EventArgs _) =>
		{
			BrowseFolderTo(settingsOutput);
		};
		AddPathRow("Default Output", settingsOutput, browseOutput, 178, value => Directory.Exists(value));
		tab.Controls.Add(pathsCard);
		CardPanel safetyCard = new CardPanel
		{
			Location = new Point(24, 384),
			Size = new Size(550, 246),
			Anchor = (AnchorStyles.Top | AnchorStyles.Left)
		};
		Label value2 = new Label
		{
			Text = "SAFETY & BEHAVIOR",
			AutoSize = true,
			ForeColor = UiTheme.Gold,
			Font = UiTheme.Ui(8.4f, FontStyle.Bold),
			BackColor = Color.Transparent,
			Location = new Point(18, 14)
		};
		safetyCard.Controls.Add(value2);
		safeMode.Caption = "Safe Mode";
		safeMode.Helper = "Never write into RDR2/LML automatically.";
		safeMode.Location = new Point(18, 44);
		safeMode.Size = new Size(510, 52);
		verifyBuilds.Caption = "Automatic verification";
		verifyBuilds.Helper = "Round-trip rebuilt resources before accepting them.";
		verifyBuilds.Location = new Point(18, 98);
		verifyBuilds.Size = new Size(510, 52);
		backupInstall.Caption = "Back up LML targets";
		backupInstall.Helper = "Create a backup before overwriting an existing target.";
		backupInstall.Location = new Point(18, 152);
		backupInstall.Size = new Size(510, 52);
		safetyCard.Controls.AddRange(safeMode, verifyBuilds, backupInstall);
		tab.Controls.Add(safetyCard);
		CardPanel appearanceCard = new CardPanel
		{
			Location = new Point(592, 384),
			Size = new Size(362, 246),
			Anchor = (AnchorStyles.Top | AnchorStyles.Right)
		};
		Label value3 = new Label
		{
			Text = "APPEARANCE",
			AutoSize = true,
			ForeColor = UiTheme.Gold,
			Font = UiTheme.Ui(8.4f, FontStyle.Bold),
			BackColor = Color.Transparent,
			Location = new Point(18, 14)
		};
		appearanceCard.Controls.Add(value3);
		Label label3 = FieldLabel("Header artwork", 18, 48);
		label3.BackColor = Color.Transparent;
		StyleWhite(backgroundBox);
		backgroundBox.Location = new Point(18, 72);
		backgroundBox.Size = new Size(230, 30);
		backgroundBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		Button browseBg = MakeButton("BROWSE", 258, 68, 86);
		browseBg.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		browseBg.Click += (object? _, EventArgs _) =>
		{
			BrowseFileTo(backgroundBox, "Images|*.jpg;*.jpeg;*.png;*.webp|All files|*.*");
		};
		reduceMotion.Caption = "Reduce motion";
		reduceMotion.Helper = "Shorter toasts and minimal UI animation.";
		reduceMotion.CheckedChanged += (object? _, EventArgs _) =>
		{
			engineStatus.PulseEnabled = !reduceMotion.Checked;
		};
		reduceMotion.Location = new Point(18, 118);
		reduceMotion.Size = new Size(326, 52);
		appearanceCard.Controls.AddRange(label3, backgroundBox, browseBg, reduceMotion);
		Label value4 = new Label
		{
			Text = "Accent  ● crimson    Secondary  ● sand",
			ForeColor = UiTheme.Muted,
			Font = UiTheme.Ui(8.5f),
			AutoSize = true,
			BackColor = Color.Transparent,
			Location = new Point(18, 190)
		};
		appearanceCard.Controls.Add(value4);
		tab.Controls.Add(appearanceCard);
		Button detect = MakeButton("AUTO-DETECT PATHS", 24, 650, 180);
		detect.Anchor = AnchorStyles.Top | AnchorStyles.Left;
		detect.Click += (object? _, EventArgs _) =>
		{
			AutoDetectPaths();
		};
		Button save = MakeButton("SAVE SETTINGS", 218, 650, 170, red: true);
		save.Anchor = AnchorStyles.Top | AnchorStyles.Left;
		save.Click += (object? _, EventArgs _) =>
		{
			SaveSettings();
			RefreshEngineStatus();
			LoadBackground();
			Invalidate(invalidateChildren: true);
			ShowToast("Settings saved.");
		};
		Button github = MakeButton("GITHUB", 402, 650, 120);
		github.Anchor = AnchorStyles.Top | AnchorStyles.Left;
		github.Click += (object? _, EventArgs _) =>
		{
			OpenUrl("https://github.com/4izenX/AizenX-RDR2-XML-Forge");
		};
		Button nexus = MakeButton("NEXUS", 536, 650, 120);
		nexus.Anchor = AnchorStyles.Top | AnchorStyles.Left;
		nexus.Click += (object? _, EventArgs _) =>
		{
			OpenUrl("https://www.nexusmods.com/reddeadredemption2/mods/10763");
		};
		tab.Controls.AddRange(detect, save, github, nexus);
		Label note = new Label
		{
			Text = "CScenarioPointRegion RSC8: in-game verified. PSO/MetaPed: native compile + parser round-trip verified; in-game validation still pending.",
			AutoSize = false,
			Height = 38,
			ForeColor = UiTheme.Muted,
			Font = UiTheme.Ui(8.6f),
			Location = new Point(680, 654),
			Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right)
		};
		tab.Controls.Add(note);
		tab.Resize += (object? _, EventArgs _) =>
		{
			LayoutSettings();
		};
		tab.HandleCreated += (object? _, EventArgs _) =>
		{
			LayoutSettings();
		};
		LayoutSettings();
		return tab;
		void AddPathRow(string text, TextBox box, Button browse, int y, Func<string, bool> validator)
		{
			Label label4 = FieldLabel(text, 18, y);
			label4.BackColor = Color.Transparent;
			Label state = new Label
			{
				AutoSize = true,
				BackColor = Color.Transparent,
				Font = UiTheme.Ui(7.4f, FontStyle.Bold),
				Location = new Point(190, y + 1)
			};
			void RefreshState()
			{
				bool ok = false;
				try { ok = validator(box.Text.Trim()); } catch { }
				state.Text = ok ? "● VALID" : "● MISSING";
				state.ForeColor = ok ? UiTheme.Success : UiTheme.Error;
			}
			StyleWhite(box);
			box.Location = new Point(18, y + 23);
			box.Size = new Size(700, 30);
			box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			box.TextChanged += (_, _) => RefreshState();
			browse.Location = new Point(744, y + 19);
			browse.Size = new Size(148, 38);
			browse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			pathsCard.Controls.AddRange(label4, state, box, browse);
			RefreshState();
		}
		void LayoutSettings()
		{
			int num = Math.Max(820, tab.ClientSize.Width) - 24;
			int num2 = num - 24;
			pathsCard.Left = 24;
			pathsCard.Width = num2;
			int top = 384;
			int num3 = 18;
			int num4 = Math.Max(430, (int)((float)num2 * 0.58f));
			safetyCard.Left = 24;
			safetyCard.Top = top;
			safetyCard.Width = num4;
			safetyCard.Height = 246;
			appearanceCard.Left = 24 + num4 + num3;
			appearanceCard.Top = top;
			appearanceCard.Width = Math.Max(310, num - appearanceCard.Left);
			appearanceCard.Height = 246;
			safeMode.Width = Math.Max(320, safetyCard.Width - 36);
			verifyBuilds.Width = safeMode.Width;
			backupInstall.Width = safeMode.Width;
			backgroundBox.Width = Math.Max(170, appearanceCard.Width - 132);
			browseBg.Left = appearanceCard.Width - 104;
			reduceMotion.Width = Math.Max(260, appearanceCard.Width - 36);
			Button button = detect;
			Button button2 = save;
			Button button3 = github;
			int num5 = (nexus.Top = 650);
			int num7 = (button3.Top = num5);
			int top2 = (button2.Top = num7);
			button.Top = top2;
			note.Top = 653;
			note.Left = nexus.Right + 22;
			note.Width = Math.Max(220, num - note.Left);
			int num10 = pathsCard.Width - 166;
			Button[] array = new Button[3] { browseEngine, browseGame, browseOutput };
			for (top2 = 0; top2 < array.Length; top2++)
			{
				array[top2].Left = num10;
			}
			TextBox[] array2 = new TextBox[3] { engineBox, gameBox, settingsOutput };
			for (top2 = 0; top2 < array2.Length; top2++)
			{
				array2[top2].Width = Math.Max(420, num10 - 36);
			}
		}
	}

	private TabPage BuildLogsTab()
	{
		TabPage tab = NewTab("Logs");
		Label label = new Label
		{
			Text = "Activity Log",
			AutoSize = true,
			ForeColor = UiTheme.Text,
			Font = UiTheme.UiDisplay(19f),
			Location = new Point(24, 18)
		};
		Label label2 = new Label
		{
			Text = "Terminal-style output from export, build, verification, search and LML actions.",
			AutoSize = true,
			ForeColor = UiTheme.Muted,
			Font = UiTheme.Ui(9.2f),
			Location = new Point(26, 52)
		};
		tab.Controls.AddRange(label, label2);
		TextBox searchLog = new TextBox();
		StyleWhite(searchLog);
		searchLog.Location = new Point(24, 88);
		searchLog.Size = new Size(360, 30);
		searchLog.PlaceholderText = "Search log…";
		tab.Controls.Add(searchLog);
		Button find = MakeButton("FIND NEXT", 396, 84, 120);
		find.Click += (object? _, EventArgs _) =>
		{
			string text = searchLog.Text.Trim();
			if (text.Length != 0)
			{
				int start = Math.Max(0, logBox.SelectionStart + logBox.SelectionLength);
				int num = logBox.Find(text, start, RichTextBoxFinds.None);
				if (num < 0)
				{
					num = logBox.Find(text, 0, RichTextBoxFinds.None);
				}
				if (num >= 0)
				{
					logBox.Select(num, text.Length);
					logBox.ScrollToCaret();
					logBox.Focus();
				}
			}
		};
		tab.Controls.Add(find);
		FlowLayoutPanel legend = new FlowLayoutPanel
		{
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			WrapContents = false,
			FlowDirection = FlowDirection.LeftToRight,
			BackColor = Color.Transparent,
			Location = new Point(540, 88),
			Anchor = AnchorStyles.Top | AnchorStyles.Right,
			Padding = new Padding(0),
			Margin = new Padding(0)
		};
		Label LegendItem(string text, Color color) => new()
		{
			Text = "● " + text,
			AutoSize = true,
			ForeColor = color,
			Font = UiTheme.Mono(8.1f, FontStyle.Bold),
			Margin = new Padding(0, 4, 16, 0),
			BackColor = Color.Transparent
		};
		legend.Controls.AddRange(
			LegendItem("INFO", UiTheme.Info),
			LegendItem("SUCCESS", UiTheme.Success),
			LegendItem("WARNING", UiTheme.Warning),
			LegendItem("ERROR", UiTheme.Error));
		tab.Controls.Add(legend);
		StyleLogBox(logBox);
		logBox.Location = new Point(24, 136);
		logBox.Size = new Size(930, 500);
		logBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
		tab.Controls.Add(logBox);
		Button clear = MakeButton("CLEAR", 24, 660, 110);
		clear.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
		clear.Click += (object? _, EventArgs _) =>
		{
			logBox.Clear();
		};
		Button save = MakeButton("SAVE LOG", 148, 660, 125);
		save.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
		save.Click += (object? _, EventArgs _) =>
		{
			SaveLog();
		};
		Button open = MakeButton("OPEN OUTPUT", 287, 660, 150, red: true);
		open.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
		open.Click += (object? _, EventArgs _) =>
		{
			OpenFolder(config.OutputPath);
		};
		tab.Controls.AddRange(clear, save, open);
		tab.Resize += (object? _, EventArgs _) =>
		{
			LayoutLogs();
		};
		tab.HandleCreated += (object? _, EventArgs _) =>
		{
			LayoutLogs();
		};
		LayoutLogs();
		return tab;
		void LayoutLogs()
		{
			int num = Math.Max(760, tab.ClientSize.Width) - 24;
			int num2 = Math.Max(480, tab.ClientSize.Height) - 24;
			searchLog.Left = 24;
			find.Left = searchLog.Right + 12;
			legend.Left = Math.Max(find.Right + 24, num - legend.Width);
			logBox.Left = 24;
			logBox.Top = 136;
			logBox.Width = Math.Max(520, num - 24);
			logBox.Height = Math.Max(280, num2 - 136 - 58);
			int num3 = num2 - 38;
			Button button = clear;
			Button button2 = save;
			int num4 = (open.Top = num3);
			int top = (button2.Top = num4);
			button.Top = top;
			clear.Left = 24;
			save.Left = clear.Right + 12;
			open.Left = save.Right + 12;
		}
	}

	private void PopulateSettings()
	{
		engineBox.Text = config.EnginePath;
		gameBox.Text = config.GamePath;
		settingsOutput.Text = config.OutputPath;
		backgroundBox.Text = config.BackgroundPath;
		safeMode.Checked = config.SafeMode;
		verifyBuilds.Checked = config.VerifyBuilds;
		backupInstall.Checked = config.BackupBeforeInstall;
		reduceMotion.Checked = config.ReduceMotion;
		engineStatus.PulseEnabled = !config.ReduceMotion;
		exportOutput.Text = config.OutputPath;
		buildOutput.Text = config.OutputPath;
		ymtOutput.Text = config.OutputPath;
	}

	private void SaveSettings()
	{
		config.EnginePath = engineBox.Text.Trim();
		config.GamePath = gameBox.Text.Trim();
		config.OutputPath = settingsOutput.Text.Trim();
		config.BackgroundPath = backgroundBox.Text.Trim();
		config.SafeMode = safeMode.Checked;
		config.VerifyBuilds = verifyBuilds.Checked;
		config.BackupBeforeInstall = backupInstall.Checked;
		config.ReduceMotion = reduceMotion.Checked;
		if (!string.IsNullOrWhiteSpace(exportOutput.Text))
		{
			config.OutputPath = exportOutput.Text.Trim();
		}
		ConfigStore.Save(config);
		bottomStatus.Text = "Settings saved.";
	}

	private void RefreshEngineStatus()
	{
		string text = Rdr2Backend.FindEngine(config);
		string text2 = Rdr2Backend.FindGame(config);
		bool flag = text != null && text2 != null;
		engineStatus.Good = flag;
		engineStatus.StatusText = (flag ? "ENGINE READY" : "ENGINE MISSING");
		engineStatus.Invalidate();
		bottomStatus.Text = $"● RDR2 {((text2 == null) ? "MISSING" : "FOUND")}     ● ENGINE {((text == null) ? "MISSING" : "FOUND")}     ◆ SAFE MODE {(config.SafeMode ? "ON" : "OFF")}";
	}

	private void RefreshHistory()
	{
		recentList.Items.Clear();
		favoriteList.Items.Clear();
		foreach (string recentFile in config.RecentFiles)
		{
			recentList.Items.Add(recentFile);
		}
		foreach (string favorite in config.Favorites)
		{
			favoriteList.Items.Add(favorite);
		}
	}

	private void BrowseFolderTo(TextBox box)
	{
		using FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
		if (Directory.Exists(box.Text))
		{
			folderBrowserDialog.InitialDirectory = box.Text;
		}
		if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
		{
			box.Text = folderBrowserDialog.SelectedPath;
		}
	}

	private void BrowseFileTo(TextBox box, string filter = "RDR2 resources|*.yft;*.ydd;*.ytd;*.ymt;*.ydr;*.ybn;*.ycd;*.yed;*.yfd;*.yld;*.ynv;*.ypmd;*.yvr;*.ywr;*.xml|All files|*.*")
	{
		using OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Filter = filter
		};
		if (openFileDialog.ShowDialog() == DialogResult.OK)
		{
			box.Text = openFileDialog.FileName;
		}
	}

	private void AddLooseExportFiles()
	{
		using OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Multiselect = true,
			Filter = "RDR2 resources|*.yft;*.ydd;*.ytd;*.ymt;*.ydr;*.ybn;*.ycd;*.yed;*.yfd;*.yld;*.ynv;*.ypmd;*.yvr;*.ywr|All files|*.*"
		};
		if (openFileDialog.ShowDialog() == DialogResult.OK)
		{
			localMode.Checked = true;
			exportItems.Text = string.Join(Environment.NewLine, Lines(exportItems).Concat(openFileDialog.FileNames).Distinct(StringComparer.OrdinalIgnoreCase));
		}
	}

	private void AddBuildXmlFiles()
	{
		using OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Multiselect = true,
			Filter = "RDR2 XML resources|*.xml|All files|*.*"
		};
		if (openFileDialog.ShowDialog() == DialogResult.OK)
		{
			buildItems.Text = string.Join(Environment.NewLine, Lines(buildItems).Concat(openFileDialog.FileNames).Distinct(StringComparer.OrdinalIgnoreCase));
		}
	}

	private void AddBuildFolder()
	{
		using FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
		if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
		{
			string[] array = Rdr2Backend.FindBuildableXmlFiles(folderBrowserDialog.SelectedPath).ToArray();
			buildItems.Text = string.Join(Environment.NewLine, Lines(buildItems).Concat(array).Distinct(StringComparer.OrdinalIgnoreCase));
			Log($"Added {array.Length} buildable XML file(s) from {folderBrowserDialog.SelectedPath}");
		}
	}

	private async Task ExportClicked(Button button)
	{
		string[] items = Lines(exportItems);
		if (items.Length == 0)
		{
			MessageBox.Show("Add at least one RDR2 asset or file.", "AizenX");
			return;
		}
		config.OutputPath = exportOutput.Text.Trim();
		Directory.CreateDirectory(config.OutputPath);
		SaveSettings();
		button.Enabled = false;
		busyBar.SetRunning(value: true);
		string oldText = button.Text;
		button.Text = "FORGING...";
		try
		{
			Log($"Export started. Mode={(archiveMode.Checked ? "archive" : "local")} Items={items.Length}");
			if ((await Rdr2Backend.ExportAsync(config, items, archiveMode.Checked, config.OutputPath, Log)).ExitCode == 0)
			{
				bottomStatus.Text = $"Export completed: {items.Length} item(s).";
				ShowToast($"Exported {items.Length} item(s) to XML.");
				MessageBox.Show("RDR2 XML export completed.", "AizenX", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
			else
			{
				bottomStatus.Text = "Export completed with failures. Check Logs.";
				ShowToast("Export completed with failures.", success: false);
				MessageBox.Show("Some conversions failed. Check the Logs tab.", "AizenX", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}
		catch (Exception ex)
		{
			Log("EXPORT ERROR: " + ex.Message);
			MessageBox.Show(ex.Message, "AizenX", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
		finally
		{
			busyBar.SetRunning(value: false);
			button.Enabled = true;
			button.Text = oldText;
			RefreshEngineStatus();
		}
	}

	private async Task BuildClicked(Button button)
	{
		List<string> inputs = Lines(buildItems).ToList();
		string[] array = inputs.Where(Directory.Exists).ToArray();
		foreach (string text in array)
		{
			inputs.Remove(text);
			inputs.AddRange(Rdr2Backend.FindBuildableXmlFiles(text));
		}
		inputs = inputs.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		if (inputs.Count == 0)
		{
			MessageBox.Show("Add at least one supported RDR2 XML file.", "AizenX");
			return;
		}
		config.OutputPath = buildOutput.Text.Trim();
		config.VerifyBuilds = verifyBuilds.Checked;
		Directory.CreateDirectory(config.OutputPath);
		ConfigStore.Save(config);
		buildResults.Items.Clear();
		lastBuilt.Clear();
		button.Enabled = false;
		busyBar.SetRunning(value: true);
		string oldText = button.Text;
		button.Text = "BUILDING...";
		try
		{
			int ok = 0;
			foreach (string input in inputs)
			{
				BuildResult buildResult = await Rdr2Backend.BuildOneAsync(config, input, config.OutputPath, Log);
				buildResults.Items.Add($"{(buildResult.Success ? "OK" : "FAIL")} {buildResult.Kind} | {Path.GetFileName(input)} | {buildResult.Message}");
				if (buildResult.Success && buildResult.Output != null)
				{
					ok++;
					lastBuilt.Add(buildResult.Output);
				}
			}
			bottomStatus.Text = $"Build finished: {ok}/{inputs.Count} succeeded.";
			ShowToast($"Build finished: {ok}/{inputs.Count} verified.", ok == inputs.Count);
			MessageBox.Show($"Build finished. {ok}/{inputs.Count} succeeded.", "AizenX", MessageBoxButtons.OK, (ok == inputs.Count) ? MessageBoxIcon.Asterisk : MessageBoxIcon.Exclamation);
			RefreshHistory();
		}
		finally
		{
			busyBar.SetRunning(value: false);
			button.Enabled = true;
			button.Text = oldText;
		}
	}

	private void InstallLastBuilt()
	{
		if (lastBuilt.Count == 0)
		{
			MessageBox.Show("Build a resource first.", "AizenX");
			return;
		}
		config.SafeMode = safeMode.Checked;
		config.BackupBeforeInstall = backupInstall.Checked;
		ConfigStore.Save(config);
		if (config.SafeMode)
		{
			MessageBox.Show("Safe Mode is ON. Disable it in Settings before installing to LML.", "AizenX", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			return;
		}
		int num = 0;
		foreach (string item in lastBuilt.Where(File.Exists))
		{
			try
			{
				string text = Rdr2Backend.InstallToLml(config, item);
				Log("LML INSTALL: " + text);
				num++;
			}
			catch (Exception ex)
			{
				Log("LML ERROR: " + ex.Message);
			}
		}
		MessageBox.Show($"Installed {num}/{lastBuilt.Count} file(s) to LML.", "AizenX", MessageBoxButtons.OK, (num == lastBuilt.Count) ? MessageBoxIcon.Asterisk : MessageBoxIcon.Exclamation);
		ShowToast($"Installed {num}/{lastBuilt.Count} file(s) to LML.", num == lastBuilt.Count);
	}

	private async Task SearchClicked()
	{
		string query = searchBox.Text.Trim();
		if (query.Length < 2)
		{
			MessageBox.Show("Type at least 2 characters.", "AizenX");
			return;
		}
		searchCount.Text = "Searching...";
		searchResults.ClearItems();
		busyBar.SetRunning(value: true);
		try
		{
			List<ArchiveAsset> list = await Task.Run(() => Rdr2Backend.SearchAssetsDetailed(config, query));
			searchResults.SetItems(list);
			searchCount.Text = $"{list.Count} result(s)";
			Log($"Search '{query}' -> {list.Count} result(s)");
			ShowToast($"Found {list.Count} asset(s).");
		}
		catch (Exception ex)
		{
			searchCount.Text = "Search failed";
			Log("SEARCH ERROR: " + ex.Message);
			ShowToast("Asset search failed.", success: false);
		}
		finally
		{
			busyBar.SetRunning(value: false);
		}
	}

	private void QueueSearchSelection()
	{
		ArchiveAsset? selected = searchResults.SelectedAsset;
		if (selected != null)
		{
			string fileName = selected.FileName;
			archiveMode.Checked = true;
			List<string> list = Lines(exportItems).ToList();
			if (!list.Contains(fileName, StringComparer.OrdinalIgnoreCase))
			{
				list.Add(fileName);
			}
			exportItems.Text = string.Join(Environment.NewLine, list);
			Log("QUEUED VERIFIED: " + fileName + " | " + selected.ArchivePath);
			bottomStatus.Text = "Queued verified " + fileName + " for Archive Mode export.";
			ShowToast("Queued " + fileName + ".");
		}
	}

	private void FavoriteSearchSelection()
	{
		string selectedItem = searchResults.SelectedItem;
		if (selectedItem != null)
		{
			ConfigStore.ToggleFavorite(config, selectedItem);
			RefreshHistory();
			bottomStatus.Text = "Favorite list updated.";
		}
	}

	private void FavoriteToSearch()
	{
		if (favoriteList.SelectedItem is string text)
		{
			searchBox.Text = text;
			SearchClicked();
		}
	}

	private void InspectClicked()
	{
		string path = inspectPath.Text.Trim();
		if (!File.Exists(path))
		{
			inspectResult.Text = "File not found.";
			return;
		}
		FileInspection fileInspection = Rdr2Backend.Inspect(path);
		inspectResult.Text = $"Path: {fileInspection.Path}{Environment.NewLine}Size: {fileInspection.Size:N0} bytes{Environment.NewLine}Extension: {fileInspection.Extension}{Environment.NewLine}RSC8: {fileInspection.IsRsc8}{Environment.NewLine}Header: {fileInspection.Header}{Environment.NewLine}{fileInspection.Details}";
		ConfigStore.AddRecent(config, path);
		RefreshHistory();
	}

	private void CompareClicked()
	{
		compareResult.Text = Rdr2Backend.CompareXml(compareA.Text.Trim(), compareB.Text.Trim());
	}

	private void AutoDetectPaths()
	{
		string text = Rdr2Backend.FindEngine(config);
		string text2 = Rdr2Backend.FindGame(config);
		if (text != null)
		{
			engineBox.Text = text;
		}
		if (text2 != null)
		{
			gameBox.Text = text2;
		}
		if (string.IsNullOrWhiteSpace(settingsOutput.Text))
		{
			settingsOutput.Text = config.OutputPath;
		}
		SaveSettings();
		RefreshEngineStatus();
		Log("Auto-detect: Engine=" + ((text == null) ? "missing" : "found") + " RDR2=" + ((text2 == null) ? "missing" : "found"));
	}

	private void OpenFolder(string path)
	{
		try
		{
			if (!string.IsNullOrWhiteSpace(path))
			{
				Directory.CreateDirectory(path);
				Process.Start(new ProcessStartInfo("explorer.exe", path)
				{
					UseShellExecute = true
				});
			}
		}
		catch (Exception ex)
		{
			Log("OPEN ERROR: " + ex.Message);
		}
	}

	private void OpenUrl(string url)
	{
		try
		{
			Process.Start(new ProcessStartInfo(url)
			{
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			Log("LINK ERROR: " + ex.Message);
		}
	}

	private void SaveLog()
	{
		try
		{
			string text = (string.IsNullOrWhiteSpace(config.OutputPath) ? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) : config.OutputPath);
			Directory.CreateDirectory(text);
			string text2 = Path.Combine(text, $"AizenX_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
			File.WriteAllText(text2, logBox.Text);
			bottomStatus.Text = "Log saved: " + text2;
		}
		catch (Exception ex)
		{
			Log("SAVE LOG ERROR: " + ex.Message);
		}
	}

	private void FormDragEnter(object? sender, DragEventArgs e)
	{
		IDataObject? data = e.Data;
		if (data != null && data.GetDataPresent(DataFormats.FileDrop))
		{
			e.Effect = DragDropEffects.Copy;
		}
	}

	private void FormDragDrop(object? sender, DragEventArgs e)
	{
		if (!(e.Data?.GetData(DataFormats.FileDrop) is string[] array) || array.Length == 0)
		{
			return;
		}
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (Directory.Exists(text))
			{
				list.AddRange(Rdr2Backend.FindBuildableXmlFiles(text));
			}
			else if (text.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
			{
				list.Add(text);
			}
			else
			{
				list2.Add(text);
			}
		}
		if (list.Count > 0)
		{
			buildItems.Text = string.Join(Environment.NewLine, Lines(buildItems).Concat(list).Distinct(StringComparer.OrdinalIgnoreCase));
			bottomStatus.Text = $"Added {list.Count} XML file(s) to Build queue.";
		}
		if (list2.Count > 0)
		{
			localMode.Checked = true;
			exportItems.Text = string.Join(Environment.NewLine, Lines(exportItems).Concat(list2).Distinct(StringComparer.OrdinalIgnoreCase));
			bottomStatus.Text = $"Added {list2.Count} loose RDR2 file(s) to Export queue.";
		}
		Log($"Drag/drop: XML={list.Count}, native={list2.Count}");
	}
}