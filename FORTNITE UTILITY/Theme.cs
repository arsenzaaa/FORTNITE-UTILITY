using System.Drawing;

namespace FortniteUtility;

internal static class Theme
{
    // Monochrome / "yin-yang" palette
    public static readonly Color Background = Color.FromArgb(18, 18, 18);
    public static readonly Color Surface = Color.FromArgb(24, 24, 24);
    public static readonly Color SurfaceAlt = Color.FromArgb(30, 30, 30);
    public static readonly Color Accent = Color.FromArgb(245, 245, 245);
    public static readonly Color AccentSecondary = Color.FromArgb(210, 210, 210);
    public static readonly Color Danger = Color.FromArgb(245, 245, 245);
    public static readonly Color TextPrimary = Color.FromArgb(245, 245, 245);
    public static readonly Color TextMuted = Color.FromArgb(190, 190, 190);

    private static Font MakeFont(string family, float size, FontStyle style)
    {
        try { return new Font(family, size, style, GraphicsUnit.Point); }
        catch { return new Font("Segoe UI", size, style, GraphicsUnit.Point); }
    }

    public static readonly Font HeadingFont = MakeFont("Cascadia Code SemiBold", 18f, FontStyle.Bold);
    public static readonly Font BodyFont = MakeFont("Cascadia Code", 10.5f, FontStyle.Regular);
    public static readonly Font BodyBold = MakeFont("Cascadia Code SemiBold", 10.5f, FontStyle.Bold);
    public static readonly Font Small = MakeFont("Cascadia Code", 9f, FontStyle.Regular);
}
