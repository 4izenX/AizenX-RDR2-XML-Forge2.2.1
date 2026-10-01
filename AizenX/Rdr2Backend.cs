using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AizenX;

public static class Rdr2Backend
{
	private static string? _resolveDir;

	private static bool _resolverInstalled;

	public static readonly IReadOnlyDictionary<string, string> ReverseTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		[".yad"] = "CodeX.Games.RDR2.Files.YadFile",
		[".ybn"] = "CodeX.Games.RDR2.Files.YbnFile",
		[".ycd"] = "CodeX.Games.RDR2.Files.YcdFile",
		[".ydd"] = "CodeX.Games.RDR2.Files.YddFile",
		[".ydr"] = "CodeX.Games.RDR2.Files.YdrFile",
		[".yed"] = "CodeX.Games.RDR2.Files.YedFile",
		[".yfd"] = "CodeX.Games.RDR2.Files.YfdFile",
		[".yft"] = "CodeX.Games.RDR2.Files.YftFile",
		[".yld"] = "CodeX.Games.RDR2.Files.YldFile",
		[".yldb"] = "CodeX.Games.RDR2.Files.YldbFile",
		[".ynv"] = "CodeX.Games.RDR2.Files.YnvFile",
		[".ypmd"] = "CodeX.Games.RDR2.Files.YpmdFile",
		[".ytd"] = "CodeX.Games.RDR2.Files.YtdFile",
		[".yvr"] = "CodeX.Games.RDR2.Files.YvrFile",
		[".ywr"] = "CodeX.Games.RDR2.Files.YwrFile"
	};

	private static readonly HashSet<string> SchemaBackedMetadataExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".ymt", ".ymap", ".ytyp"
	};

	public static readonly string[] ExportExtensions =
	{
		".yad", ".yas", ".ybn", ".ycd", ".ych", ".ydd", ".ydr", ".yed", ".yfd", ".yft",
		".yld", ".yldb", ".ymap", ".ymf", ".ymt", ".ynv", ".ypmd", ".ytd", ".ytyp", ".yvr", ".ywr"
	};

	public static string? FindEngine(AppConfig config)
	{
		if (File.Exists(config.EnginePath))
		{
			return config.EnginePath;
		}
		string text = Path.Combine(AppContext.BaseDirectory, "engine", "RDR2YtdExporter.exe");
		if (File.Exists(text))
		{
			return text;
		}
		string environmentVariable = Environment.GetEnvironmentVariable("AIZENX_ENGINE");
		if (!string.IsNullOrWhiteSpace(environmentVariable) && File.Exists(environmentVariable))
		{
			return environmentVariable;
		}
		try
		{
			return Directory.EnumerateFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"), "RDR2YtdExporter.exe", SearchOption.AllDirectories).FirstOrDefault((string p) => p.Contains("RDR2YtdExporterRuntime", StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	public static string? FindGame(AppConfig config)
	{
		if (File.Exists(Path.Combine(config.GamePath, "RDR2.exe")))
		{
			return config.GamePath;
		}
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		string text = new string[3] { "C:\\Program Files\\Rockstar Games\\Red Dead Redemption 2", "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Red Dead Redemption 2", "C:\\Program Files\\Steam\\steamapps\\common\\Red Dead Redemption 2" }.FirstOrDefault((string x) => File.Exists(Path.Combine(x, "RDR2.exe")));
		if (text != null)
		{
			return text;
		}
		try
		{
			return Directory.EnumerateFiles(Path.Combine(folderPath, "Downloads"), "RDR2.exe", SearchOption.AllDirectories).Select(Path.GetDirectoryName).FirstOrDefault((string x) => x?.Contains("Red Dead Redemption 2", StringComparison.OrdinalIgnoreCase) ?? false);
		}
		catch
		{
			return null;
		}
	}

	public static async Task<ProcessResult> RunProcessAsync(string fileName, IEnumerable<string> args, string? workingDir = null, int timeoutMs = 180000)
	{
		ProcessStartInfo processStartInfo = new ProcessStartInfo(fileName)
		{
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
			WorkingDirectory = (workingDir ?? Path.GetDirectoryName(fileName) ?? "")
		};
		foreach (string arg in args)
		{
			processStartInfo.ArgumentList.Add(arg);
		}
		using Process p = new Process
		{
			StartInfo = processStartInfo
		};
		StringBuilder stdout = new StringBuilder();
		StringBuilder stderr = new StringBuilder();
		p.OutputDataReceived += (object _, DataReceivedEventArgs e) =>
		{
			if (e.Data != null)
			{
				stdout.AppendLine(e.Data);
			}
		};
		p.ErrorDataReceived += (object _, DataReceivedEventArgs e) =>
		{
			if (e.Data != null)
			{
				stderr.AppendLine(e.Data);
			}
		};
		p.Start();
		p.BeginOutputReadLine();
		p.BeginErrorReadLine();
		using var timeout = new CancellationTokenSource(timeoutMs);
		try
		{
			await p.WaitForExitAsync(timeout.Token);
			return new ProcessResult(p.ExitCode, stdout.ToString(), stderr.ToString());
		}
		catch (OperationCanceledException)
		{
			try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
			stderr.AppendLine($"[TIMEOUT] Child process exceeded {timeoutMs / 1000} seconds and was terminated.");
			return new ProcessResult(124, stdout.ToString(), stderr.ToString());
		}
	}

	public static async Task<ProcessResult> ExportAsync(AppConfig config, IEnumerable<string> items, bool archiveMode, string outputDir, Action<string>? log = null)
	{
		string engine = FindEngine(config) ?? throw new FileNotFoundException("RDR2 exporter engine not found.");
		string game = FindGame(config) ?? throw new DirectoryNotFoundException("RDR2 game folder not found.");
		Directory.CreateDirectory(outputDir);
		List<string> requested = items.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		string list = Path.Combine(Path.GetTempPath(), $"aizenx_{Guid.NewGuid():N}.txt");
		await File.WriteAllLinesAsync(list, requested);
		try
		{
			string[] args = new string[12]
			{
				"--game",
				game,
				"--list",
				list,
				"--out",
				outputDir,
				"--mode",
				archiveMode ? "archive" : "local",
				"--format",
				"xml",
				"--layout",
				"flat"
			};
			int timeoutMs = archiveMode ? 180000 : 45000;
			ProcessResult processResult = await RunProcessAsync(engine, args, Path.GetDirectoryName(engine), timeoutMs);
			string[] array = (processResult.StdOut + processResult.StdErr).Split(new char[2] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

			if (!archiveMode || processResult.ExitCode == 0 || requested.Count == 0)
			{
				foreach (string obj in array)
				{
					log?.Invoke(obj);
					if (!archiveMode
						&& obj.Contains("Decompression failed", StringComparison.OrdinalIgnoreCase)
						&& obj.Contains("Verification size does not match", StringComparison.OrdinalIgnoreCase))
					{
						log?.Invoke("[LOOSE RESOURCE INVALID] File was found, but its declared decompressed size does not match the decoded resource size. It may be incomplete, improperly extracted, or incompatible with safe standalone decoding.");
					}
				}
				return processResult;
			}

			int recovered = 0;
			int unresolved = 0;
			var fallbackLog = new StringBuilder();
			var fallbackLines = new List<string>();
			var recoveredNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (string item in requested)
			{
				if (HasXmlExport(outputDir, item))
					continue;

				if (TryExtractNativeArchiveAsset(config, item, outputDir, out string? nativePath, out string message))
				{
					recovered++;
					recoveredNames.Add(Path.GetFileName(item));
					string line = "[NATIVE FALLBACK] " + Path.GetFileName(item) + " -> " + nativePath + " | " + message;
					fallbackLines.Add(line);
					fallbackLog.AppendLine(line);
				}
				else
				{
					unresolved++;
					string line = "[UNRESOLVED] " + Path.GetFileName(item) + " | " + message;
					fallbackLines.Add(line);
					fallbackLog.AppendLine(line);
				}
			}

			foreach (string obj in array)
			{
				string display = obj;
				if (obj.StartsWith("[FAIL]", StringComparison.OrdinalIgnoreCase)
					&& recoveredNames.Any(name => obj.Contains(name, StringComparison.OrdinalIgnoreCase)))
				{
					display = "[XML UNAVAILABLE]" + obj.Substring(6);
				}
				else if (obj.StartsWith("[DONE]", StringComparison.OrdinalIgnoreCase) && recovered > 0)
				{
					display = "[XML STAGE]" + obj.Substring(6) + $" | native fallback recovered={recovered}";
				}
				log?.Invoke(display);
			}

			foreach (string line in fallbackLines)
				log?.Invoke(line);

			if (recovered > 0 && unresolved == 0)
			{
				string summary = $"[AIZENX UNIVERSAL] Completed successfully: XML where supported, native fallback for {recovered} file(s).";
				log?.Invoke(summary);
				fallbackLog.AppendLine(summary);
				return new ProcessResult(0, processResult.StdOut + Environment.NewLine + fallbackLog, processResult.StdErr);
			}

			return processResult;
		}
		finally
		{
			try { File.Delete(list); } catch { }
		}
	}

	private sealed record ArchiveIndexEntry(uint Hash, string Extension, string ArchivePath, string StoredPath, object Entry);

	private sealed class ArchiveIndexCache
	{
		public readonly Dictionary<uint, List<ArchiveIndexEntry>> ByHash = new();
		public readonly HashSet<string> XmlConvertibleExtensions = new(StringComparer.OrdinalIgnoreCase);
		public int FileCount { get; set; }
		public int ArchiveCount { get; set; }
	}

	private static readonly object ArchiveIndexSync = new();
	private static ArchiveIndexCache? CachedArchiveIndex;
	private static string? CachedArchiveKey;

	private static ArchiveIndexCache GetArchiveIndex(AppConfig config)
	{
		string engine = FindEngine(config) ?? throw new FileNotFoundException("RDR2 exporter engine not found.");
		string game = FindGame(config) ?? throw new DirectoryNotFoundException("RDR2 game folder not found.");
		string key = Path.GetFullPath(game) + "|" + Path.GetFullPath(engine);

		lock (ArchiveIndexSync)
		{
			if (CachedArchiveIndex != null && string.Equals(CachedArchiveKey, key, StringComparison.OrdinalIgnoreCase))
				return CachedArchiveIndex;

			string runtime = Path.GetDirectoryName(engine) ?? throw new DirectoryNotFoundException("Exporter runtime folder not found.");
			string corePath = Path.Combine(runtime, "CodeX.Core.dll");
			string rdrPath = Path.Combine(runtime, "CodeX.Games.RDR2.dll");
			if (!File.Exists(corePath) || !File.Exists(rdrPath))
				throw new FileNotFoundException("CodeX RDR2 libraries were not found beside the exporter.");

			InstallResolver(runtime);
			Assembly core = AssemblyLoadContext.Default.LoadFromAssemblyPath(corePath);
			Assembly rdr = AssemblyLoadContext.Default.LoadFromAssemblyPath(rdrPath);
			Type gameType = rdr.GetType("CodeX.Games.RDR2.RDR2Game", true)!;
			Type managerType = rdr.GetType("CodeX.Games.RDR2.RPF8.Rpf8FileManager", true)!;
			object gameObj = Activator.CreateInstance(gameType) ?? throw new InvalidOperationException("Could not create RDR2 game context.");
			gameType.GetProperty("GameFolder")!.SetValue(gameObj, game);
			object manager = (managerType.GetConstructor(new[] { gameType }) ?? throw new MissingMethodException("Rpf8FileManager constructor not found."))
				.Invoke(new[] { gameObj });
			managerType.GetField("Folder", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(manager, game);
			managerType.GetMethod("InitFileTypes", BindingFlags.Instance | BindingFlags.Public)!.Invoke(manager, null);
			rdr.GetType("CodeX.Games.RDR2.RPF8.Rpf8Compression", true)!.GetMethod("Init", BindingFlags.Static | BindingFlags.Public)!.Invoke(null, new object[] { game });
			rdr.GetType("CodeX.Games.RDR2.RPF8.Rpf8Crypto", true)!.GetMethod("Init", BindingFlags.Static | BindingFlags.Public)!.Invoke(null, new object[] { game });

			string[] rootRpfs = Directory.GetFiles(game, "*.rpf", SearchOption.TopDirectoryOnly);
			(managerType.GetMethod("InitArchives", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(string[]) }, null)
				?? throw new MissingMethodException("Rpf8FileManager.InitArchives(string[]) not found."))
				.Invoke(manager, new object[] { rootRpfs });

			var result = new ArchiveIndexCache();
			object? fileTypesObj = managerType.GetField("FileTypes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(manager);
			if (fileTypesObj is System.Collections.IDictionary fileTypes)
			{
				foreach (System.Collections.DictionaryEntry pair in fileTypes)
				{
					string extension = pair.Key?.ToString() ?? "";
					object? info = pair.Value;
					object? convertible = info?.GetType().GetProperty("XmlConvertible", BindingFlags.Instance | BindingFlags.Public)?.GetValue(info);
					if (!string.IsNullOrWhiteSpace(extension) && convertible is bool yes && yes)
						result.XmlConvertibleExtensions.Add(extension);
				}
			}

			object? archivesObj = managerType.GetField("AllArchives", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(manager);
			if (archivesObj is not System.Collections.IEnumerable archives)
				throw new InvalidOperationException("RDR2 archive list is unavailable.");

			foreach (object archive in archives)
			{
				result.ArchiveCount++;
				Type archiveType = archive.GetType();
				string archivePath = archiveType.GetProperty("Path", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(archive)?.ToString() ?? "";
				object? entriesObj = archiveType.GetProperty("AllEntries", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(archive);
				if (entriesObj is not System.Collections.IEnumerable entries)
					continue;

				foreach (object entry in entries)
				{
					Type entryType = entry.GetType();
					object? hashObj = entryType.GetProperty("FileHash", BindingFlags.Instance | BindingFlags.Public)?.GetValue(entry);
					object? extObj = entryType.GetProperty("FileExtId", BindingFlags.Instance | BindingFlags.Public)?.GetValue(entry);
					if (hashObj == null || extObj == null)
						continue;

					object? rawHash = hashObj.GetType().GetField("Hash", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(hashObj);
					if (rawHash == null)
						continue;

					uint hash = Convert.ToUInt32(rawHash);
					string extName = extObj.ToString() ?? "";
					if (string.IsNullOrWhiteSpace(extName))
						continue;

					string extension = "." + extName.ToLowerInvariant();
					string storedPath = entryType.GetProperty("Path", BindingFlags.Instance | BindingFlags.Public)?.GetValue(entry)?.ToString() ?? "";
					var indexed = new ArchiveIndexEntry(hash, extension, archivePath, storedPath, entry);

					if (!result.ByHash.TryGetValue(hash, out List<ArchiveIndexEntry>? list))
					{
						list = new List<ArchiveIndexEntry>();
						result.ByHash.Add(hash, list);
					}
					list.Add(indexed);
					result.FileCount++;
				}
			}

			CachedArchiveIndex = result;
			CachedArchiveKey = key;
			return result;
		}
	}

	private static string NormalizeAssetName(string value)
	{
		string text = (value ?? "").Trim().Trim('"').Replace('/', '\\');
		int slash = text.LastIndexOf('\\');
		if (slash >= 0 && slash + 1 < text.Length)
			text = text[(slash + 1)..];

		string ext = Path.GetExtension(text);
		if (!string.IsNullOrWhiteSpace(ext) && ext.Length <= 8)
			text = text[..^ext.Length];

		return text.Trim();
	}

	private static uint GetAssetHash(AppConfig config, string value)
	{
		string baseName = NormalizeAssetName(value);
		if (baseName.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
			&& baseName.Length == 10
			&& uint.TryParse(baseName.AsSpan(2), System.Globalization.NumberStyles.HexNumber, null, out uint raw))
			return raw;

		string engine = FindEngine(config) ?? throw new FileNotFoundException("RDR2 exporter engine not found.");
		string runtime = Path.GetDirectoryName(engine) ?? throw new DirectoryNotFoundException("Exporter runtime folder not found.");
		InstallResolver(runtime);
		Assembly core = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(runtime, "CodeX.Core.dll"));
		Type hashType = core.GetType("CodeX.Core.Utilities.JenkHash", true)!;
		MethodInfo genHash = hashType.GetMethod("GenHash", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string) }, null)
			?? throw new MissingMethodException("JenkHash.GenHash(string) not found.");
		return Convert.ToUInt32(genHash.Invoke(null, new object[] { baseName }));
	}

	private static bool HasXmlExport(string outputDir, string requested)
	{
		string fileName = Path.GetFileName(requested);
		return File.Exists(Path.Combine(outputDir, fileName + ".xml"));
	}

	private static bool TryExtractNativeArchiveAsset(AppConfig config, string requested, string outputDir, out string? nativePath, out string message)
	{
		nativePath = null;
		message = "";
		try
		{
			string fileName = Path.GetFileName(requested);
			string extension = Path.GetExtension(fileName).ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(extension))
			{
				message = "No extension was supplied.";
				return false;
			}

			uint hash = GetAssetHash(config, fileName);
			ArchiveIndexCache index = GetArchiveIndex(config);
			if (!index.ByHash.TryGetValue(hash, out List<ArchiveIndexEntry>? matches))
			{
				message = "Asset hash is not present in the archive index.";
				return false;
			}

			ArchiveIndexEntry? match = matches.LastOrDefault(x => x.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase));
			if (match == null)
			{
				message = "The requested extension does not match the indexed asset.";
				return false;
			}

			object entry = match.Entry;
			Type entryType = entry.GetType();
			object? archive = entryType.GetProperty("Archive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(entry);
			if (archive == null)
			{
				message = "Archive object is unavailable.";
				return false;
			}

			MethodInfo? extract = archive.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
				.FirstOrDefault(m => m.Name == "ExtractFile" && m.GetParameters().Length == 2);
			if (extract == null)
			{
				message = "Native archive extraction API is unavailable.";
				return false;
			}

			byte[]? bytes = extract.Invoke(archive, new object[] { entry, true }) as byte[];
			if (bytes == null || bytes.Length == 0)
			{
				message = "Archive extraction returned no data.";
				return false;
			}

			string nativeDir = Path.Combine(outputDir, "_native");
			Directory.CreateDirectory(nativeDir);
			nativePath = Path.Combine(nativeDir, fileName);
			File.WriteAllBytes(nativePath, bytes);
			message = $"Native fallback extracted {bytes.Length:N0} bytes from {match.ArchivePath}.";
			return true;
		}
		catch (TargetInvocationException ex) when (ex.InnerException != null)
		{
			message = ex.InnerException.Message;
			return false;
		}
		catch (Exception ex)
		{
			message = ex.Message;
			return false;
		}
	}

	public static List<ArchiveAsset> SearchAssetsDetailed(AppConfig config, string query, int limit = 500)
	{
		if (string.IsNullOrWhiteSpace(query) || limit <= 0)
			return new List<ArchiveAsset>();

		string engine = FindEngine(config) ?? throw new FileNotFoundException("RDR2 exporter engine not found.");
		string runtime = Path.GetDirectoryName(engine) ?? throw new DirectoryNotFoundException("Exporter runtime folder not found.");
		string stringsPath = Directory.EnumerateFiles(runtime, "*strings.txt")
			.FirstOrDefault(x => Path.GetFileName(x).Contains("RDR2", StringComparison.OrdinalIgnoreCase))
			?? throw new FileNotFoundException("RDR2 strings database was not found beside the exporter.");

		ArchiveIndexCache index = GetArchiveIndex(config);
		Assembly core = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(runtime, "CodeX.Core.dll"));
		Type hashType = core.GetType("CodeX.Core.Utilities.JenkHash", true)!;
		MethodInfo genHash = hashType.GetMethod("GenHash", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string) }, null)
			?? throw new MissingMethodException("JenkHash.GenHash(string) not found.");

		string needle = NormalizeAssetName(query);
		var byKey = new Dictionary<string, ArchiveAsset>(StringComparer.OrdinalIgnoreCase);
		var testedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		void TryCandidate(string raw)
		{
			string name = NormalizeAssetName(raw);
			if (string.IsNullOrWhiteSpace(name) || !testedNames.Add(name))
				return;

			uint hash = Convert.ToUInt32(genHash.Invoke(null, new object[] { name }));
			if (!index.ByHash.TryGetValue(hash, out List<ArchiveIndexEntry>? matches))
				return;

			foreach (ArchiveIndexEntry match in matches)
			{
				string resultKey = hash.ToString("X8") + "|" + match.Extension;
				byKey[resultKey] = new ArchiveAsset(name, match.Extension, match.ArchivePath, match.StoredPath, hash, index.XmlConvertibleExtensions.Contains(match.Extension));
			}
		}

		uint rawQueryHash = 0;
		bool isRawHash = needle.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
			&& needle.Length == 10
			&& uint.TryParse(needle.AsSpan(2), System.Globalization.NumberStyles.HexNumber, null, out rawQueryHash);

		if (isRawHash)
		{
			if (index.ByHash.TryGetValue(rawQueryHash, out List<ArchiveIndexEntry>? rawMatches))
			{
				foreach (ArchiveIndexEntry match in rawMatches)
				{
					string resultKey = rawQueryHash.ToString("X8") + "|" + match.Extension;
					byKey[resultKey] = new ArchiveAsset(
						"0x" + rawQueryHash.ToString("X8"),
						match.Extension,
						match.ArchivePath,
						match.StoredPath,
						rawQueryHash,
						index.XmlConvertibleExtensions.Contains(match.Extension));
				}
			}
		}
		else
		{
			TryCandidate(needle);
			foreach (string line in File.ReadLines(stringsPath))
			{
				if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
					continue;
				if (!line.Contains(needle, StringComparison.OrdinalIgnoreCase))
					continue;
				TryCandidate(line);
			}
		}

		return byKey.Values
			.OrderBy(x => x.Name.Equals(needle, StringComparison.OrdinalIgnoreCase) ? 0 : x.Name.StartsWith(needle, StringComparison.OrdinalIgnoreCase) ? 1 : 2)
			.ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
			.ThenBy(x => x.Extension, StringComparer.OrdinalIgnoreCase)
			.Take(limit)
			.ToList();
	}

	public static List<string> SearchAssets(AppConfig config, string query, int limit = 500)
	{
		return SearchAssetsDetailed(config, query, limit)
			.Select(x => x.FileName)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	public static async Task<BuildResult> BuildOneAsync(AppConfig config, string xmlPath, string outputDir, Action<string>? log = null)
	{
		if (!File.Exists(xmlPath))
		{
			return new BuildResult(xmlPath, null, "", Success: false, Verified: false, "Input XML not found.");
		}
		bool renamedYmt = xmlPath.EndsWith(".ymt", StringComparison.OrdinalIgnoreCase) && IsXmlYmt(xmlPath);
		if (!xmlPath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) && !renamedYmt)
		{
			return new BuildResult(xmlPath, null, "", Success: false, Verified: false, "Input must be XML, including an XML-text .ymt file.");
		}
		string fileNameWithoutExtension = renamedYmt ? Path.GetFileName(xmlPath) : Path.GetFileNameWithoutExtension(xmlPath);
		string ext = Path.GetExtension(fileNameWithoutExtension).ToLowerInvariant();
		bool isSchemaMetadata = SchemaBackedMetadataExtensions.Contains(ext);
		if (!isSchemaMetadata && !ReverseTypes.ContainsKey(ext))
		{
			return new BuildResult(xmlPath, null, ext, Success: false, Verified: false, "Reverse conversion for " + ext + " is not available.");
		}
		try
		{
			Directory.CreateDirectory(outputDir);
			string outPath = Path.Combine(outputDir, fileNameWithoutExtension);
			log?.Invoke("BUILD " + Path.GetFileName(xmlPath) + " -> " + Path.GetFileName(outPath));
			string nativeKind = await Task.Run(() => CompileXml(config, xmlPath, outPath, ext));
			YmtContainerKind container = YmtSupport.DetectContainer(outPath);
			bool nativeHeaderOk = isSchemaMetadata ? container != YmtContainerKind.Unknown : container == YmtContainerKind.Rsc8;
			if (!nativeHeaderOk)
			{
				if (isSchemaMetadata)
				{
					try { File.Delete(outPath); } catch { }
				}
				string expected = isSchemaMetadata ? "a native RSC8 or PSO/PSIN metadata resource" : "an RSC8 resource";
				return new BuildResult(xmlPath, isSchemaMetadata ? null : outPath, ext, Success: false, Verified: false,
					$"Compiler returned {nativeKind}, but {expected} was not produced. Output {(isSchemaMetadata ? "removed" : "kept for inspection")}.");
			}
			bool flag = false;
			string text = "Verification disabled.";
			if (config.VerifyBuilds)
			{
				if (!isSchemaMetadata)
				{
					(flag, text) = await VerifyNativeAsync(config, outPath, log);
				}
				else
				{
					(flag, text) = await VerifyYmtSmartAsync(config, xmlPath, outPath, log);
				}
			}
			if (isSchemaMetadata && config.VerifyBuilds && !flag)
			{
				try
				{
					File.Delete(outPath);
				}
				catch
				{
				}
				return new BuildResult(xmlPath, null, ext, Success: false, Verified: false, "Native metadata verification failed; unverified output was removed. " + text);
			}
			ConfigStore.AddRecent(config, outPath);
			string message;
			if (isSchemaMetadata)
			{
				message = "Native metadata " + nativeKind + " build OK / " + (flag ? text : "NOT VERIFIED: " + text);
			}
			else
			{
				message = (flag ? "Build OK / Verify OK" : ("Build OK / " + text));
			}
			return new BuildResult(xmlPath, outPath, ext, Success: true, flag, message);
		}
		catch (Exception ex)
		{
			log?.Invoke("BUILD ERROR: " + ex.Message);
			return new BuildResult(xmlPath, null, ext, Success: false, Verified: false, ex.Message);
		}
	}

	private static void InstallResolver(string dir)
	{
		_resolveDir = dir;
		if (_resolverInstalled)
		{
			return;
		}
		AssemblyLoadContext.Default.Resolving += (AssemblyLoadContext _, AssemblyName name) =>
		{
			if (string.IsNullOrWhiteSpace(_resolveDir))
			{
				return (Assembly?)null;
			}
			string text = Path.Combine(_resolveDir, name.Name + ".dll");
			return (!File.Exists(text)) ? null : AssemblyLoadContext.Default.LoadFromAssemblyPath(text);
		};
		_resolverInstalled = true;
	}

	private static string CompileXml(AppConfig config, string xmlPath, string outPath, string ext)
	{
		string? path = FindEngine(config) ?? throw new FileNotFoundException("RDR2 exporter engine not found.");
		string text = FindGame(config) ?? throw new DirectoryNotFoundException("RDR2 game folder not found.");
		string? directoryName = Path.GetDirectoryName(path);
		string text2 = Path.Combine(directoryName, "CodeX.Core.dll");
		string text3 = Path.Combine(directoryName, "CodeX.Games.RDR2.dll");
		if (!File.Exists(text2) || !File.Exists(text3))
		{
			throw new FileNotFoundException("CodeX RDR2 libraries were not found beside the Manifest Tool exporter.");
		}
		InstallResolver(directoryName);
		Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(text2);
		Assembly assembly2 = AssemblyLoadContext.Default.LoadFromAssemblyPath(text3);
		Type type = assembly2.GetType("CodeX.Games.RDR2.RDR2Game", throwOnError: true);
		Type type2 = assembly2.GetType("CodeX.Games.RDR2.RPF8.Rpf8FileManager", throwOnError: true);
		object obj = Activator.CreateInstance(type);
		type.GetProperty("GameFolder").SetValue(obj, text);
		object obj2 = (type2.GetConstructor(new Type[1] { type }) ?? throw new MissingMethodException("Rpf8FileManager constructor not found.")).Invoke(new object[1] { obj });
		type2.GetField("Folder", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(obj2, text);
		type2.GetMethod("InitFileTypes").Invoke(obj2, null);
		Type type3 = assembly2.GetType("CodeX.Games.RDR2.RPF8.Rpf8Compression", throwOnError: true);
		Type type4 = assembly2.GetType("CodeX.Games.RDR2.RPF8.Rpf8Crypto", throwOnError: true);
		type3.GetMethod("Init", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[1] { text });
		type4.GetMethod("Init", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[1] { text });
		assembly.GetType("CodeX.Core.Utilities.JenkIndex", throwOnError: true).GetMethod("LoadStringsFile", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[1] { "RDR2" });
		string text4 = File.ReadAllText(xmlPath);
		string text5 = Path.GetDirectoryName(xmlPath) ?? Environment.CurrentDirectory;
		if (SchemaBackedMetadataExtensions.Contains(ext))
		{
			return CompileNativeYmt(assembly2, assembly, type2, obj2, text4, text5, outPath);
		}
		Type type5 = assembly2.GetType(ReverseTypes[ext], throwOnError: true);
		if (!(type2.GetMethods(BindingFlags.Instance | BindingFlags.Public).First((MethodInfo m) => m.Name == "ConvertFromXml" && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 1 && m.GetParameters().Length == 2).MakeGenericMethod(type5)
			.Invoke(obj2, new object[2] { text4, text5 }) is byte[] array) || array.Length == 0)
		{
			throw new InvalidOperationException("RDR2 compiler returned no data for " + ext + ".");
		}
		File.WriteAllBytes(outPath, array);
		return "RSC8";
	}

	private static string CompileNativeYmt(Assembly rdr, Assembly core, Type managerType, object manager, string xml, string folder, string outPath)
	{
		string text = XDocument.Parse(xml, LoadOptions.PreserveWhitespace).Root?.Name.LocalName ?? "";
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new InvalidDataException("YMT XML has no root element.");
		}
		Type? type = rdr.GetType("CodeX.Games.RDR2.RPF8.Rpf8Schemas", throwOnError: true);
		type.GetMethod("EnsureSchemas", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
		object obj = type.GetField("RscSchema", BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
		object obj2 = type.GetField("PsoSchema", BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
		if ((object)core.GetType("CodeX.Core.Engine.DataSchema", throwOnError: true).GetMethod("GetClass", new Type[1] { typeof(string) }) == null)
		{
			throw new MissingMethodException("DataSchema.GetClass(string) not found.");
		}
		bool flag = obj != null && NativeMetadataXml.HasSchema(text, pso: false);
		bool flag2 = obj2 != null && NativeMetadataXml.HasSchema(text, pso: true);
		if (!flag && !flag2)
		{
			throw new NotSupportedException("YMT root <" + text + "> is not present in the installed RDR2 RSC8 or PSO schema.");
		}
		if ((object)managerType.GetMethod("ConvertFromXml", BindingFlags.Instance | BindingFlags.Public, null, new Type[3]
		{
			typeof(string),
			typeof(string),
			typeof(string)
		}, null) == null)
		{
			throw new MissingMethodException("Rpf8FileManager.ConvertFromXml(string,string,string) not found.");
		}
		List<string> list = new List<string>();
		(YmtContainerKind, bool, string)[] array = new (YmtContainerKind, bool, string)[2]
		{
			(YmtContainerKind.Rsc8, flag, "aizenx_native.rsc.xml"),
			(YmtContainerKind.Pso, flag2, "aizenx_native.pso.xml")
		};
		for (int i = 0; i < array.Length; i++)
		{
			(YmtContainerKind, bool, string) tuple = array[i];
			if (!tuple.Item2)
			{
				continue;
			}
			try
			{
				byte[] array2 = NativeMetadataXml.Compile(xml, tuple.Item1 == YmtContainerKind.Pso);
				if (array2 == null || array2.Length < 4)
				{
					list.Add(YmtSupport.Label(tuple.Item1) + " compiler returned no data");
					continue;
				}
				YmtContainerKind ymtContainerKind = YmtSupport.DetectContainer(array2);
				if (ymtContainerKind != tuple.Item1)
				{
					list.Add(YmtSupport.Label(tuple.Item1) + " path returned " + YmtSupport.Label(ymtContainerKind));
					continue;
				}
				File.WriteAllBytes(outPath, array2);
				return YmtSupport.Label(ymtContainerKind) + "/" + text;
			}
			catch (TargetInvocationException ex) when (ex.InnerException != null)
			{
				list.Add(YmtSupport.Label(tuple.Item1) + ": " + ex.InnerException.Message);
			}
		}
		throw new InvalidOperationException("No valid native YMT container could be produced for <" + text + ">. " + string.Join(" | ", list));
	}

	private static async Task<(bool ok, string message)> VerifyYmtSmartAsync(AppConfig config, string sourceXml, string nativePath, Action<string>? log)
	{
		YmtContainerKind kind = YmtSupport.DetectContainer(nativePath);
		string container = YmtSupport.Label(kind);
		string verifyDir = null;
		try
		{
			string xml;
			switch (kind)
			{
			case YmtContainerKind.Pso:
				xml = await Task.Run(() => DecodePsoXml(config, nativePath));
				log?.Invoke("VERIFY PSO parser re-opened the compiled YMT successfully.");
				break;
			case YmtContainerKind.Rsc8:
			{
				verifyDir = Path.Combine(Path.GetTempPath(), "AizenXYmtVerify_" + Guid.NewGuid().ToString("N"));
				Directory.CreateDirectory(verifyDir);
				if ((await ExportAsync(config, new _003C_003Ez__ReadOnlySingleElementList<string>(nativePath), archiveMode: false, verifyDir, log)).ExitCode != 0)
				{
					return (ok: false, message: "Native RSC8 YMT was written, but round-trip decode failed.");
				}
				string nativeFileName = Path.GetFileName(nativePath);
				string text = Directory.EnumerateFiles(verifyDir, "*.xml", SearchOption.AllDirectories).FirstOrDefault((string p) => Path.GetFileName(p).StartsWith(nativeFileName, StringComparison.OrdinalIgnoreCase)) ?? Directory.EnumerateFiles(verifyDir, "*.xml", SearchOption.AllDirectories).FirstOrDefault();
				if (text == null)
				{
					return (ok: false, message: "Native RSC8 YMT decoded without producing verification XML.");
				}
				xml = File.ReadAllText(text);
				break;
			}
			default:
				return (ok: false, message: "Compiled YMT has an unknown native container header.");
			}
			XDocument xDocument = XDocument.Parse(NativeMetadataXml.Canonicalize(File.ReadAllText(sourceXml), kind == YmtContainerKind.Pso), LoadOptions.PreserveWhitespace);
			XDocument xDocument2 = XDocument.Parse(NativeMetadataXml.Canonicalize(xml, kind == YmtContainerKind.Pso), LoadOptions.PreserveWhitespace);
			if (xDocument.Root == null || xDocument2.Root == null)
			{
				return (ok: false, message: "Verification XML has no root element.");
			}
			if (!xDocument.Root.Name.LocalName.Equals(xDocument2.Root.Name.LocalName, StringComparison.Ordinal))
			{
				return (ok: false, message: $"Round-trip root changed: <{xDocument.Root.Name.LocalName}> -> <{xDocument2.Root.Name.LocalName}>.");
			}
			int value = xDocument.Root.DescendantsAndSelf().Count();
			int value2 = xDocument2.Root.DescendantsAndSelf().Count();
			if (XmlElementsEqual(xDocument.Root, xDocument2.Root))
			{
				string text2 = $"{container} exact round-trip OK: <{xDocument.Root.Name.LocalName}> {value:N0}/{value2:N0} elements.";
				log?.Invoke("VERIFY " + text2);
				return (ok: true, message: text2);
			}
			if (YmtSupport.SemanticallyEquivalent(xDocument.Root, xDocument2.Root, out string report))
			{
				string text3 = container + " " + report;
				log?.Invoke("VERIFY " + text3);
				return (ok: true, message: text3);
			}
			return (ok: false, message: $"{container} verification failed. {report} Raw element counts: {value:N0} -> {value2:N0}.");
		}
		catch (Exception ex)
		{
			return (ok: false, message: container + " verification error: " + ex.Message);
		}
		finally
		{
			if (verifyDir != null)
			{
				try
				{
					Directory.Delete(verifyDir, recursive: true);
				}
				catch
				{
				}
			}
		}
	}

	private static string DecodePsoXml(AppConfig config, string nativePath)
	{
		string engine = FindEngine(config) ?? throw new FileNotFoundException("RDR2 exporter engine not found.");
		string dir = Path.GetDirectoryName(engine) ?? throw new DirectoryNotFoundException("Exporter runtime folder not found.");
		string corePath = Path.Combine(dir, "CodeX.Core.dll");
		string rdrPath = Path.Combine(dir, "CodeX.Games.RDR2.dll");
		if (!File.Exists(corePath) || !File.Exists(rdrPath))
			throw new FileNotFoundException("CodeX RDR2 libraries were not found beside the Manifest Tool exporter.");

		InstallResolver(dir);
		Assembly core = AssemblyLoadContext.Default.LoadFromAssemblyPath(corePath);
		Assembly rdr = AssemblyLoadContext.Default.LoadFromAssemblyPath(rdrPath);
		core.GetType("CodeX.Core.Utilities.JenkIndex", true)!
			.GetMethod("LoadStringsFile", BindingFlags.Static | BindingFlags.Public)!
			.Invoke(null, new object[] { "RDR2" });
		Type schemas = rdr.GetType("CodeX.Games.RDR2.RPF8.Rpf8Schemas", true)!;
		schemas.GetMethod("EnsureSchemas", BindingFlags.Static | BindingFlags.Public)!.Invoke(null, null);

		Type psoType = rdr.GetType("CodeX.Games.RDR2.Files.PsoFile", true)!;
		Type entryType = rdr.GetType("CodeX.Games.RDR2.RPF8.Rpf8FileEntry", true)!;
		Type binaryEntryType = rdr.GetType("CodeX.Games.RDR2.RPF8.Rpf8BinaryFileEntry", true)!;
		object entry = Activator.CreateInstance(binaryEntryType)
			?? throw new InvalidOperationException("Could not create a PSO verification file entry.");
		object pso = psoType.GetConstructor(new[] { entryType })!.Invoke(new[] { entry });
		byte[] bytes = File.ReadAllBytes(nativePath);
		psoType.GetMethod("Load")!.Invoke(pso, new object[] { bytes });

		Exception? loadError = psoType.GetProperty("LoadException")?.GetValue(pso) as Exception;
		if (loadError != null) throw new InvalidDataException("PSO parser rejected the compiled file.", loadError);
		object? bag = psoType.GetProperty("Bag", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(pso);
		if (bag == null) throw new InvalidDataException("PSO parser opened the file but found no root data bag.");
		string? xml = bag.GetType().GetMethod("ToXml", Type.EmptyTypes)?.Invoke(bag, null) as string;
		if (string.IsNullOrWhiteSpace(xml)) throw new InvalidDataException("PSO parser could not reconstruct XML.");
		return xml;
	}

	private static bool XmlElementsEqual(XElement a, XElement b)
	{
		if (a.Name != b.Name)
		{
			return false;
		}
		Dictionary<XName, string> dictionary = a.Attributes().ToDictionary((XAttribute x) => x.Name, (XAttribute x) => x.Value);
		Dictionary<XName, string> ba = b.Attributes().ToDictionary((XAttribute x) => x.Name, (XAttribute x) => x.Value);
		if (dictionary.Count != ba.Count || dictionary.Any((KeyValuePair<XName, string> kv) => !ba.TryGetValue(kv.Key, out var value) || value != kv.Value))
		{
			return false;
		}
		if (!string.Equals(DirectText(a), DirectText(b), StringComparison.Ordinal))
		{
			return false;
		}
		XElement[] array = a.Elements().ToArray();
		XElement[] array2 = b.Elements().ToArray();
		if (array.Length != array2.Length)
		{
			return false;
		}
		for (int num = 0; num < array.Length; num++)
		{
			if (!XmlElementsEqual(array[num], array2[num]))
			{
				return false;
			}
		}
		return true;
		static string DirectText(XElement e)
		{
			return string.Concat(from x in e.Nodes().OfType<XText>()
				select x.Value).Trim();
		}
	}

	private static async Task<(bool ok, string message)> VerifyNativeAsync(AppConfig config, string nativePath, Action<string>? log)
	{
		string verifyDir = Path.Combine(Path.GetTempPath(), "AizenXVerify_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(verifyDir);
		try
		{
			ProcessResult processResult = await ExportAsync(config, new _003C_003Ez__ReadOnlySingleElementList<string>(nativePath), archiveMode: false, verifyDir, log);
			string path = Path.Combine(verifyDir, Path.GetFileName(nativePath) + ".xml");
			if (processResult.ExitCode == 0 && File.Exists(path))
			{
				return (ok: true, message: "Verify OK");
			}
			return (ok: false, message: "Verification failed; output was kept for inspection.");
		}
		finally
		{
			try
			{
				Directory.Delete(verifyDir, recursive: true);
			}
			catch
			{
			}
		}
	}

	public static FileInspection Inspect(string path)
	{
		FileInfo fileInfo = new FileInfo(path);
		if (!fileInfo.Exists)
		{
			return new FileInspection(path, 0L, Path.GetExtension(path), IsRsc8: false, "", "File not found.");
		}
		string extension = Path.GetExtension(path);
		byte[] array = new byte[Math.Min(16, (int)Math.Min(fileInfo.Length, 16L))];
		using (FileStream fileStream = File.OpenRead(path))
		{
			fileStream.Read(array, 0, array.Length);
		}
		YmtContainerKind kind = YmtSupport.DetectContainer(array);
		bool flag = kind == YmtContainerKind.Rsc8;
		string header = BitConverter.ToString(array);
		string details;
		if (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
		{
			string? text = File.ReadLines(path).FirstOrDefault((string x) => x.TrimStart().StartsWith("<") && !x.TrimStart().StartsWith("<?"));
			details = text == null ? "XML file" : "XML root: " + text.Trim();
		}
		else
		{
			details = kind switch
			{
				YmtContainerKind.Rsc8 => "RDR2 RSC8 resource detected.",
				YmtContainerKind.Pso => "RDR2 PSO/PSIN metadata resource detected.",
				_ => "Unknown/non-native resource header (may be plain metadata or a broken extraction)."
			};
		}
		return new FileInspection(path, fileInfo.Length, extension, flag, header, details);
	}

	public static string GetNativeContainer(string path) => YmtSupport.Label(YmtSupport.DetectContainer(path));

	public static string InstallToLml(AppConfig config, string nativePath, string? targetDir = null)
	{
		if (config.SafeMode)
		{
			throw new InvalidOperationException("Safe Mode is enabled. Disable it before installing into LML.");
		}
		string path = FindGame(config) ?? throw new DirectoryNotFoundException("RDR2 game folder not found.");
		if (targetDir == null)
		{
			targetDir = Path.Combine(path, "lml", "downloader", "FINAL_STREAM");
		}
		Directory.CreateDirectory(targetDir);
		string text = Path.Combine(targetDir, Path.GetFileName(nativePath));
		if (File.Exists(text) && config.BackupBeforeInstall)
		{
			string text2 = Path.Combine(string.IsNullOrWhiteSpace(config.OutputPath) ? Path.GetDirectoryName(nativePath) : config.OutputPath, "Backups", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
			Directory.CreateDirectory(text2);
			File.Copy(text, Path.Combine(text2, Path.GetFileName(text)), overwrite: true);
		}
		File.Copy(nativePath, text, overwrite: true);
		return text;
	}

	public static string CompareXml(string leftPath, string rightPath)
	{
		if (!File.Exists(leftPath) || !File.Exists(rightPath))
		{
			return "Both XML files must exist.";
		}
		string[] array = File.ReadAllLines(leftPath);
		string[] array2 = File.ReadAllLines(rightPath);
		int num = Math.Max(array.Length, array2.Length);
		int num2 = 0;
		List<string> list = new List<string>();
		for (int i = 0; i < num; i++)
		{
			string text = ((i < array.Length) ? array[i] : "<missing>");
			string text2 = ((i < array2.Length) ? array2[i] : "<missing>");
			if (!(text == text2))
			{
				num2++;
				if (list.Count < 20)
				{
					list.Add($"Line {i + 1}");
					list.Add("A: " + text);
					list.Add("B: " + text2);
					list.Add("");
				}
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
		handler.AppendLiteral("Left lines: ");
		handler.AppendFormatted(array.Length);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("Right lines: ");
		handler.AppendFormatted(array2.Length);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
		handler.AppendLiteral("Changed lines: ");
		handler.AppendFormatted(num2);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder.AppendLine();
		foreach (string item in list)
		{
			stringBuilder.AppendLine(item);
		}
		if (num2 > 20)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder6 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(34, 1, stringBuilder2);
			handler.AppendLiteral("... ");
			handler.AppendFormatted(num2 - 20);
			handler.AppendLiteral(" more changed lines not shown.");
			stringBuilder6.AppendLine(ref handler);
		}
		return stringBuilder.ToString();
	}

	public static bool IsXmlYmt(string path)
	{
		if (!path.EndsWith(".ymt", StringComparison.OrdinalIgnoreCase)) return false;
		try
		{
			using var reader = System.Xml.XmlReader.Create(path, new System.Xml.XmlReaderSettings
			{
				DtdProcessing = System.Xml.DtdProcessing.Prohibit,
				XmlResolver = null
			});
			return reader.MoveToContent() == System.Xml.XmlNodeType.Element;
		}
		catch { return false; }
	}

	public static IEnumerable<string> FindBuildableXmlFiles(string folder)
	{
		if (!Directory.Exists(folder)) yield break;
		IEnumerable<string> candidates = Directory.EnumerateFiles(folder, "*.xml", SearchOption.AllDirectories)
			.Concat(Directory.EnumerateFiles(folder, "*.ymt", SearchOption.AllDirectories).Where(IsXmlYmt));
		foreach (string item in candidates)
		{
			string extension = item.EndsWith(".ymt", StringComparison.OrdinalIgnoreCase)
				? ".ymt"
				: Path.GetExtension(Path.GetFileNameWithoutExtension(item));
			if (ReverseTypes.ContainsKey(extension) || SchemaBackedMetadataExtensions.Contains(extension))
				yield return item;
		}
	}
}