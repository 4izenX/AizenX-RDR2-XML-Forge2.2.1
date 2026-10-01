using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace AizenX;

public static class UiTheme
{
    public static readonly Color Bg = Color.FromArgb(7, 8, 11);          // #07080B
    public static readonly Color Surface = Color.FromArgb(13, 15, 20);   // #0D0F14
    public static readonly Color Surface2 = Color.FromArgb(20, 23, 31);  // #14171F
    public static readonly Color Surface3 = Color.FromArgb(28, 32, 42);
    public static readonly Color Surface4 = Color.FromArgb(36, 41, 53);

    public static readonly Color Border = Color.FromArgb(45, 255, 255, 255);
    public static readonly Color BorderSoft = Color.FromArgb(24, 255, 255, 255);
    public static readonly Color Text = Color.FromArgb(244, 245, 248);
    public static readonly Color Muted = Color.FromArgb(154, 160, 174);
    public static readonly Color Muted2 = Color.FromArgb(108, 114, 128);

    public static readonly Color Crimson = Color.FromArgb(225, 29, 46);   // #E11D2E
    public static readonly Color CrimsonHot = Color.FromArgb(241, 48, 64);
    public static readonly Color CrimsonDeep = Color.FromArgb(179, 18, 31); // #B3121F
    public static readonly Color CrimsonDark = Color.FromArgb(103, 14, 26);

    public static readonly Color Brass = Color.FromArgb(200, 169, 106);   // #C8A96A
    public static readonly Color Gold = Brass;
    public static readonly Color Success = Color.FromArgb(34, 197, 94);  // #22C55E
    public static readonly Color Warning = Color.FromArgb(245, 158, 11); // #F59E0B
    public static readonly Color Error = Color.FromArgb(255, 77, 77);
    public static readonly Color Info = Color.FromArgb(96, 165, 250);

    public const int Radius = 12;
    public const int RadiusSmall = 8;
    public const int Space = 16;
    public const int SpaceLg = 24;

    private static readonly string UiFamily = FontFamily.Families
        .Any(x => x.Name.Equals("Inter", StringComparison.OrdinalIgnoreCase))
            ? "Inter" : "Segoe UI Variable Text";

    private static readonly string DisplayFamily = FontFamily.Families
        .Any(x => x.Name.Equals("Space Grotesk", StringComparison.OrdinalIgnoreCase))
            ? "Space Grotesk" : "Segoe UI Variable Display";

    public static Font Ui(float size = 9.5f, FontStyle style = FontStyle.Regular)
        => new(UiFamily, size, style);

    public static Font UiDisplay(float size = 18f, FontStyle style = FontStyle.Bold)
        => new(DisplayFamily, size, style);

    public static Font Mono(float size = 9.5f, FontStyle style = FontStyle.Regular)
    {
        string family = FontFamily.Families.Any(x => x.Name.Equals("JetBrains Mono", StringComparison.OrdinalIgnoreCase))
            ? "JetBrains Mono" : "Cascadia Mono";
        return new Font(family, size, style);
    }

    public static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = Math.Max(2, radius * 2);
        path.AddArc(rect.Left, rect.Top, d, d, 180f, 90f);
        path.AddArc(rect.Right - d, rect.Top, d, d, 270f, 90f);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0f, 90f);
        path.AddArc(rect.Left, rect.Bottom - d, d, d, 90f, 90f);
        path.CloseFigure();
        return path;
    }

    public static Color Blend(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return Color.FromArgb(
            (int)(a.A + (b.A - a.A) * t),
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));
    }
}