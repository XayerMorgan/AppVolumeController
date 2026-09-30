using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using AppVolumeController.Models;

namespace AppVolumeController.Services;

public sealed record ThemePalette(Color Background, Color Surface, Color Raised, Color Hover,
    Color Text, Color Secondary, Color Border, Color Accent, Color AccentText);

public static class ThemeService
{
    public static AppearancePreference Current { get; private set; } = new();
    public static event Action? Changed;
    public static bool UsesHighContrast => SystemParameters.HighContrast || Current.Theme == "High contrast";

    public static void Initialize(AppearancePreference preference)
    {
        Apply(preference);
        SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;
    }

    public static void Shutdown() => SystemEvents.UserPreferenceChanged -= OnSystemPreferenceChanged;

    private static void OnSystemPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        var dispatcher = Application.Current.Dispatcher;
        if (!dispatcher.HasShutdownStarted) dispatcher.BeginInvoke(new Action(() => Apply(Current)));
    }

    public static void Apply(AppearancePreference preference)
    {
        Current = preference.Copy();
        Current.Normalize();
        var palette = CreatePalette(Current, SystemUsesDarkTheme(), SystemParameters.HighContrast);
        var resources = Application.Current.Resources;
        void Brush(string key, Color color) { var brush = new SolidColorBrush(color); brush.Freeze(); resources[key] = brush; }
        Brush("WindowBrush", palette.Background);
        Brush("SurfaceBrush", palette.Surface);
        Brush("RaisedBrush", palette.Raised);
        Brush("HoverBrush", palette.Hover);
        Brush("TextBrush", palette.Text);
        Brush("SecondaryBrush", palette.Secondary);
        Brush("BorderBrush", palette.Border);
        Brush("AccentBrush", palette.Accent);
        Brush("AccentTextBrush", palette.AccentText);
        foreach (string name in new[] { "Teal", "Blue", "Violet", "Amber" })
        {
            var option = Current.Copy(); option.Accent = name;
            Brush(name + "SwatchBrush", CreatePalette(option, SystemUsesDarkTheme(), SystemParameters.HighContrast).Accent);
        }
        resources["BodySize"] = 14d * Current.TextScale;
        resources["SmallSize"] = 12d * Current.TextScale;
        resources["HeadingSize"] = 20d * Current.TextScale;
        resources["TitleSize"] = 30d * Current.TextScale;
        Changed?.Invoke();
    }

    public static ThemePalette CreatePalette(AppearancePreference preference, bool systemDark = false, bool systemHighContrast = false)
    {
        if (systemHighContrast)
            return new(SystemColors.WindowColor, SystemColors.WindowColor, SystemColors.ControlColor,
                SystemColors.ControlColor, SystemColors.WindowTextColor, SystemColors.WindowTextColor,
                SystemColors.WindowTextColor, SystemColors.HighlightColor, SystemColors.HighlightTextColor);
        if (preference.Theme == "High contrast")
            return new(Colors.Black, Colors.Black, Colors.Black, Colors.Black, Colors.White, Colors.White,
                Colors.White, Hex("#FFFF00"), Colors.Black);
        bool dark = preference.Theme == "Dark" || (preference.Theme == "System" && systemDark);
        var background = Hex(dark ? "#0E1525" : "#F2F5FA");
        var surface = Hex(dark ? "#172237" : "#FFFFFF");
        var raised = Hex(dark ? "#203049" : "#E8EFF6");
        var hover = Hex(dark ? "#26384D" : "#DCE7F2");
        var requested = preference.Accent switch
        {
            "Blue" => "#3B82F6", "Violet" => "#A78BFA", "Amber" => "#F59E0B",
            "Custom" when TryParseHex(preference.CustomAccent, out _) => preference.CustomAccent,
            _ => "#14B8A6"
        };
        var accent = EnsureContrast(Hex(requested), new[] { background, surface, raised, hover }, dark);
        var accentText = Contrast(accent, Colors.Black) >= Contrast(accent, Colors.White) ? Colors.Black : Colors.White;
        return new(background, surface, raised, hover, Hex(dark ? "#F1F5FB" : "#17263B"),
            Hex(dark ? "#B4C2D4" : "#4A5D73"), Hex(dark ? "#8093AC" : "#63778F"), accent, accentText);
    }

    // Adjust the requested accent toward white/black, retaining its hue as far as possible.
    // The accent is used for text as well as control outlines, so require 4.5:1 on every surface.
    private static Color EnsureContrast(Color color, Color[] surfaces, bool dark)
    {
        var target = dark ? Colors.White : Colors.Black;
        for (int i = 0; i <= 255; i++)
        {
            double t = i / 255d;
            var candidate = Color.FromRgb((byte)Math.Round(color.R + (target.R - color.R) * t),
                (byte)Math.Round(color.G + (target.G - color.G) * t), (byte)Math.Round(color.B + (target.B - color.B) * t));
            if (surfaces.All(surface => Contrast(candidate, surface) >= 4.5)) return candidate;
        }
        return target;
    }

    public static double Contrast(Color a, Color b)
    {
        static double Luminance(Color c)
        {
            static double Linear(byte value) { double v = value / 255d; return v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4); }
            return .2126 * Linear(c.R) + .7152 * Linear(c.G) + .0722 * Linear(c.B);
        }
        double x = Luminance(a), y = Luminance(b);
        return (Math.Max(x, y) + .05) / (Math.Min(x, y) + .05);
    }

    public static bool TryParseHex(string? value, out Color color)
    {
        color = default;
        if (value is null || value.Length != 7 || value[0] != '#' ||
            !uint.TryParse(value.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint rgb)) return false;
        color = Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
        return true;
    }

    private static Color Hex(string value) { TryParseHex(value, out var color); return color; }
    private static bool SystemUsesDarkTheme()
    {
        try { return Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1) is int value && value == 0; }
        catch { return false; }
    }
}
