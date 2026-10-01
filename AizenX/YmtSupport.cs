using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AizenX;

public enum YmtContainerKind
{
    Unknown,
    Rsc8,
    Pso
}

public static class YmtSupport
{
    static readonly Regex SpaceRx = new(@"\s+", RegexOptions.Compiled);
    static readonly Regex ZeroRx = new(@"^[+-]?0+(?:\.0+)?(?:[eE][+-]?0+)?$", RegexOptions.Compiled);

    public static YmtContainerKind DetectContainer(string path)
    {
        if (!File.Exists(path)) return YmtContainerKind.Unknown;
        using var fs = File.OpenRead(path);
        Span<byte> h = stackalloc byte[4];
        if (fs.Read(h) != 4) return YmtContainerKind.Unknown;
        return DetectContainer(h);
    }

    public static YmtContainerKind DetectContainer(ReadOnlySpan<byte> h)
    {
        if (h.Length < 4) return YmtContainerKind.Unknown;
        if (h[0] == (byte)'R' && h[1] == (byte)'S' && h[2] == (byte)'C' && h[3] == (byte)'8')
            return YmtContainerKind.Rsc8;
        if (h[0] == (byte)'P' && h[1] == (byte)'S' && h[2] == (byte)'I' && h[3] == (byte)'N')
            return YmtContainerKind.Pso;
        return YmtContainerKind.Unknown;
    }

    public static string Label(YmtContainerKind kind) => kind switch
    {
        YmtContainerKind.Rsc8 => "RSC8",
        YmtContainerKind.Pso => "PSO/PSIN",
        _ => "UNKNOWN"
    };

    public static bool SemanticallyEquivalent(XElement source, XElement rebuilt, out string report)
    {
        if (!source.Name.LocalName.Equals(rebuilt.Name.LocalName, StringComparison.Ordinal))
        {
            report = $"Root changed: <{source.Name.LocalName}> -> <{rebuilt.Name.LocalName}>.";
            return false;
        }

        var a = BuildSemanticBag(source);
        var b = BuildSemanticBag(rebuilt);
        var missing = Diff(a, b);
        var extra = Diff(b, a);

        if (missing.Count == 0 && extra.Count == 0)
        {
            int sa = source.DescendantsAndSelf().Count();
            int sb = rebuilt.DescendantsAndSelf().Count();
            int normalized = Math.Abs(sa - sb);
            report = normalized == 0
                ? $"Semantic round-trip exact: {sa}/{sb} elements."
                : $"Semantic round-trip OK: all meaningful values preserved; serializer normalized {normalized:N0} default/empty elements ({sa:N0} -> {sb:N0}).";
            return true;
        }

        report = $"Semantic round-trip differs: missing {missing.Sum(x => x.Value):N0} meaningful token(s), added {extra.Sum(x => x.Value):N0}. "
               + "Missing sample: " + Preview(missing) + " Added sample: " + Preview(extra);
        return false;
    }

    static Dictionary<string, int> BuildSemanticBag(XElement root)
    {
        var bag = new Dictionary<string, int>(StringComparer.Ordinal);
        Walk(root, "/" + root.Name.LocalName, bag);
        return bag;
    }

    static void Walk(XElement e, string path, Dictionary<string, int> bag)
    {
        foreach (var a in e.Attributes().OrderBy(x => x.Name.LocalName, StringComparer.Ordinal))
        {
            var value = Normalize(a.Value);
            if (!IsDefaultLike(value))
                Add(bag, path + "|@" + a.Name.LocalName + "=" + value);
        }

        string direct = Normalize(string.Concat(e.Nodes().OfType<XText>().Select(x => x.Value)));
        if (!IsDefaultLike(direct))
            Add(bag, path + "|#=" + direct);

        foreach (var child in e.Elements())
            Walk(child, path + "/" + child.Name.LocalName, bag);
    }

    static string Normalize(string value)
    {
        value = SpaceRx.Replace(value.Trim(), " ");
        if (value.Length == 0) return "";

        if (bool.TryParse(value, out bool bv))
            return bv ? "true" : "false";

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return value.ToLowerInvariant();

        if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal dv))
            return dv.ToString("G29", CultureInfo.InvariantCulture);

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double fv))
            return fv.ToString("R", CultureInfo.InvariantCulture);

        return value;
    }

    static bool IsDefaultLike(string value)
    {
        if (value.Length == 0) return true;
        if (value.Equals("false", StringComparison.OrdinalIgnoreCase)) return true;
        if (value.Equals("null", StringComparison.OrdinalIgnoreCase)) return true;
        if (value.Equals("none", StringComparison.OrdinalIgnoreCase)) return true;
        if (value.Equals("0x00000000", StringComparison.OrdinalIgnoreCase)) return true;
        return ZeroRx.IsMatch(value);
    }

    static void Add(Dictionary<string, int> bag, string token)
    {
        bag.TryGetValue(token, out int n);
        bag[token] = n + 1;
    }

    static Dictionary<string, int> Diff(Dictionary<string, int> left, Dictionary<string, int> right)
    {
        var d = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var kv in left)
        {
            right.TryGetValue(kv.Key, out int n);
            if (kv.Value > n) d[kv.Key] = kv.Value - n;
        }
        return d;
    }

    static string Preview(Dictionary<string, int> diff)
    {
        if (diff.Count == 0) return "none.";
        return string.Join("; ", diff.Take(4).Select(kv => $"{kv.Key} x{kv.Value}")) + (diff.Count > 4 ? "; ..." : ".");
    }
}