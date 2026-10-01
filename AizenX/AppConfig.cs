using System.Collections.Generic;

namespace AizenX;

public sealed class AppConfig
{
	public string EnginePath { get; set; } = "";

	public string GamePath { get; set; } = "";

	public string OutputPath { get; set; } = "";

	public string BackgroundPath { get; set; } = "";

	public bool SafeMode { get; set; } = true;

	public bool VerifyBuilds { get; set; } = true;

	public bool BackupBeforeInstall { get; set; } = true;

	public int BackgroundDarkness { get; set; } = 110;

	public bool ReduceMotion { get; set; }

	public List<string> RecentFiles { get; set; } = new List<string>();

	public List<string> Favorites { get; set; } = new List<string>();
}