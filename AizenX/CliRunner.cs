using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AizenX;

public static class CliRunner
{
	public static async Task<int> Run(string[] args, AppConfig config)
	{
		string text = args[0].ToLowerInvariant();
		Dictionary<string, string> dictionary = Parse(args.Skip(1).ToArray());
		if (dictionary.TryGetValue("engine", out var value))
		{
			config.EnginePath = value;
		}
		if (dictionary.TryGetValue("game", out var value2))
		{
			config.GamePath = value2;
		}
		if (dictionary.TryGetValue("out", out var value3))
		{
			config.OutputPath = value3;
		}
		bool json = dictionary.ContainsKey("json");
		try
		{
			int result;
			switch (text)
			{
			case "export":
				result = await Export(dictionary, config, json);
				break;
			case "build":
				result = await Build(dictionary, config, json);
				break;
			case "search":
				result = Search(dictionary, config, json);
				break;
			case "inspect":
				result = Inspect(dictionary, json);
				break;
			case "--help":
			case "help":
			case "-h":
				result = Help();
				break;
			default:
				result = Unknown(text);
				break;
			}
			return result;
		}
		catch (Exception ex)
		{
			if (json)
			{
				Console.WriteLine(JsonSerializer.Serialize(new
				{
					ok = false,
					error = ex.Message
				}));
			}
			else
			{
				Console.Error.WriteLine("AizenX ERROR: " + ex.Message);
			}
			return 1;
		}
	}

	private static Dictionary<string, string> Parse(string[] args)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < args.Length; i++)
		{
			if (args[i].StartsWith("--"))
			{
				string key = args[i].Substring(2);
				if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
				{
					dictionary[key] = args[++i];
				}
				else
				{
					dictionary[key] = "true";
				}
			}
		}
		return dictionary;
	}

	private static async Task<int> Export(Dictionary<string, string> opt, AppConfig config, bool json)
	{
		if (!opt.TryGetValue("input", out string value))
		{
			throw new ArgumentException("Missing --input.");
		}
		string mode = (opt.TryGetValue("mode", out string value2) ? value2 : "archive");
		bool archiveMode = !mode.Equals("local", StringComparison.OrdinalIgnoreCase);
		string output = (opt.TryGetValue("out", out string value3) ? value3 : config.OutputPath);
		IEnumerable<string> items = ((!File.Exists(value) || !Path.GetExtension(value).Equals(".txt", StringComparison.OrdinalIgnoreCase)) ? new _003C_003Ez__ReadOnlySingleElementList<string>(value) : (from x in File.ReadAllLines(value)
			where !string.IsNullOrWhiteSpace(x)
			select x));
		List<string> logs = new List<string>();
		ProcessResult processResult = await Rdr2Backend.ExportAsync(config, items, archiveMode, output, logs.Add);
		if (json)
		{
			Console.WriteLine(JsonSerializer.Serialize(new
			{
				ok = (processResult.ExitCode == 0),
				exitCode = processResult.ExitCode,
				mode = mode,
				output = output,
				log = logs
			}));
		}
		else
		{
			foreach (string item in logs)
			{
				Console.WriteLine(item);
			}
			Console.WriteLine((processResult.ExitCode == 0) ? "AIZENX_EXPORT_OK" : "AIZENX_EXPORT_FAILED");
		}
		return (processResult.ExitCode != 0) ? 1 : 0;
	}

	private static async Task<int> Build(Dictionary<string, string> opt, AppConfig config, bool json)
	{
		if (!opt.TryGetValue("input", out string value))
		{
			throw new ArgumentException("Missing --input.");
		}
		string output = (opt.TryGetValue("out", out string value2) ? value2 : config.OutputPath);
		if (opt.ContainsKey("no-verify"))
		{
			config.VerifyBuilds = false;
		}
		List<string> list = (Directory.Exists(value) ? Rdr2Backend.FindBuildableXmlFiles(value).ToList() : new List<string> { value });
		List<BuildResult> results = new List<BuildResult>();
		foreach (string item in list)
		{
			results.Add(await Rdr2Backend.BuildOneAsync(config, item, output, (string s) =>
			{
				if (!json)
				{
					Console.WriteLine(s);
				}
			}));
		}
		if (json)
		{
			Console.WriteLine(JsonSerializer.Serialize(results));
		}
		else
		{
			foreach (BuildResult item2 in results)
			{
				Console.WriteLine($"{(item2.Success ? "OK" : "FAIL")} {item2.Kind} {item2.Input} :: {item2.Message}");
			}
		}
		return (results.Count <= 0 || !results.All((BuildResult x) => x.Success)) ? 1 : 0;
	}

	private static int Search(Dictionary<string, string> opt, AppConfig config, bool json)
	{
		if (!opt.TryGetValue("query", out string value))
		{
			throw new ArgumentException("Missing --query.");
		}
		List<string> list = Rdr2Backend.SearchAssets(config, value, (opt.TryGetValue("limit", out string value2) && int.TryParse(value2, out var result)) ? result : 200);
		if (json)
		{
			Console.WriteLine(JsonSerializer.Serialize(list));
		}
		else
		{
			foreach (string item in list)
			{
				Console.WriteLine(item);
			}
		}
		return 0;
	}

	private static int Inspect(Dictionary<string, string> opt, bool json)
	{
		if (!opt.TryGetValue("input", out string value))
		{
			throw new ArgumentException("Missing --input.");
		}
		FileInspection fileInspection = Rdr2Backend.Inspect(value);
		if (json)
		{
			Console.WriteLine(JsonSerializer.Serialize(fileInspection));
		}
		else
		{
			Console.WriteLine("Path: " + fileInspection.Path);
			Console.WriteLine($"Size: {fileInspection.Size}");
			Console.WriteLine("Extension: " + fileInspection.Extension);
			Console.WriteLine($"RSC8: {fileInspection.IsRsc8}");
			Console.WriteLine("Header: " + fileInspection.Header);
			Console.WriteLine(fileInspection.Details);
		}
		return (!File.Exists(value)) ? 1 : 0;
	}

	private static int Help()
	{
		Console.WriteLine("AizenX RDR2 XML Forge v2.2");
		Console.WriteLine("  export --input NAME_OR_FILE --mode archive|local --out DIR [--game DIR] [--engine EXE] [--json]");
		Console.WriteLine("  build --input FILE_OR_FOLDER --out DIR [--game DIR] [--engine EXE] [--no-verify] [--json]");
		Console.WriteLine("  search --query TEXT [--limit N] [--engine EXE] [--json]");
		Console.WriteLine("  inspect --input FILE [--json]");
		return 0;
	}

	private static int Unknown(string command)
	{
		Console.Error.WriteLine("Unknown command: " + command);
		if (Help() != 0)
		{
			return 2;
		}
		return 2;
	}
}