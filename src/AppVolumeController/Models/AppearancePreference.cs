namespace AppVolumeController.Models;

public sealed class AppearancePreference
{
    public string Theme { get; set; } = "System";
    public string Accent { get; set; } = "Teal";
    public string CustomAccent { get; set; } = "#14B8A6";
    public double TextScale { get; set; } = 1;

    public AppearancePreference Copy() => new() { Theme = Theme, Accent = Accent, CustomAccent = CustomAccent, TextScale = TextScale };

    public void Normalize()
    {
        if (Theme is not ("System" or "Light" or "Dark" or "High contrast")) Theme = "System";
        if (Accent is not ("Teal" or "Blue" or "Violet" or "Amber" or "Custom")) Accent = "Teal";
        if (!Services.ThemeService.TryParseHex(CustomAccent, out _)) CustomAccent = "#14B8A6";
        if (TextScale is not (1 or 1.25 or 1.5 or 2)) TextScale = 1;
    }
}
