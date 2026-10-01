using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AizenX;

public static class ConfigStore
{
	private static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AizenX");

	private static readonly string PathFile = Path.Combine(Dir, "settings-v2.json");

	public static AppConfig Load()
	{
		try
		{
			if (!File.Exists(PathFile))
			{
				return new AppConfig();
			}
			return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(PathFile)) ?? new AppConfig();
		}
		catch
		{
			return new AppConfig();
		}
	}

	public static void Save(AppConfig config)
	{
		Directory.CreateDirectory(Dir);
		File.WriteAllText(PathFile, JsonSerializer.Serialize(config, new JsonSerializerOptions
		{
			WriteIndented = true
		}));
	}

	public static void AddRecent(AppConfig config, string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			config.RecentFiles.RemoveAll((string x) => x.Equals(path, StringComparison.OrdinalIgnoreCase));
			config.RecentFiles.Insert(0, path);
			if (config.RecentFiles.Count > 30)
			{
				config.RecentFiles.RemoveRange(30, config.RecentFiles.Count - 30);
			}
			Save(config);
		}
	}

	public static void ToggleFavorite(AppConfig config, string value)
	{
		string text = config.Favorites.FirstOrDefault((string x) => x.Equals(value, StringComparison.OrdinalIgnoreCase));
		if (text == null)
		{
			config.Favorites.Add(value);
		}
		else
		{
			config.Favorites.Remove(text);
		}
		Save(config);
	}
}