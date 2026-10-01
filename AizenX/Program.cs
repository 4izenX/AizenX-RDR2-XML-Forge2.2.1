using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AizenX;

internal static class Program
{
	private const int ATTACH_PARENT_PROCESS = -1;

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool AttachConsole(int dwProcessId);

	[DllImport("kernel32.dll")]
	private static extern nint GetConsoleWindow();

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(nint hWnd, int nCmdShow);

	private static void HideConsole()
	{
		try
		{
			nint consoleWindow = GetConsoleWindow();
			if (consoleWindow != IntPtr.Zero)
			{
				ShowWindow(consoleWindow, 0);
			}
		}
		catch
		{
		}
	}

	private static void AttachCliConsole()
	{
		try
		{
			AttachConsole(-1);
			Console.SetOut(new StreamWriter(Console.OpenStandardOutput())
			{
				AutoFlush = true
			});
			Console.SetError(new StreamWriter(Console.OpenStandardError())
			{
				AutoFlush = true
			});
		}
		catch
		{
		}
	}

	[STAThread]
	private static int Main(string[] args)
	{
		AppConfig config = ConfigStore.Load();
		Bootstrap(config);
		if (args.Length != 0)
		{
			AttachCliConsole();
			return CliRunner.Run(args, config).GetAwaiter().GetResult();
		}
		HideConsole();
		ApplicationConfiguration.Initialize();
		Application.Run(new MainForm(config));
		return 0;
	}

	private static void Bootstrap(AppConfig config)
	{
		bool flag = false;
		if (!File.Exists(config.EnginePath))
		{
			string text = Rdr2Backend.FindEngine(config);
			if (text != null)
			{
				config.EnginePath = text;
				flag = true;
			}
		}
		if (!File.Exists(Path.Combine(config.GamePath, "RDR2.exe")))
		{
			string text2 = Rdr2Backend.FindGame(config);
			if (text2 != null)
			{
				config.GamePath = text2;
				flag = true;
			}
		}
		if (string.IsNullOrWhiteSpace(config.OutputPath))
		{
			config.OutputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "AizenX_Output");
			flag = true;
		}
		string text3 = Path.Combine(AppContext.BaseDirectory, "rdr2_background.jpg");
		if (string.IsNullOrWhiteSpace(config.BackgroundPath) && File.Exists(text3))
		{
			config.BackgroundPath = text3;
			flag = true;
		}
		if (flag)
		{
			ConfigStore.Save(config);
		}
	}
}