using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using AppVolumeController.Models;
using AppVolumeController.Services;

namespace AppVolumeController;

public partial class AppearanceWindow : Window
{
    private readonly AppearancePreference original;
    private readonly Action<AppearancePreference> save;
    private AppearancePreference draft;
    private bool loading;
    private bool saved;

    public AppearanceWindow(AppearancePreference preference, Action<AppearancePreference> save)
    {
        original = preference.Copy();
        draft = preference.Copy();
        this.save = save;
        InitializeComponent();
        Height = Math.Min(Height, SystemParameters.WorkArea.Height);
        LoadControls();
        ThemeService.Changed += UpdatePreview;
        Closed += (_, _) => { ThemeService.Changed -= UpdatePreview; if (!saved) ThemeService.Apply(original); };
        Loaded += (_, _) => ThemeOptions.Children.OfType<RadioButton>().First(r => r.IsChecked == true).Focus();
    }

    private void LoadControls()
    {
        loading = true;
        foreach (var option in ThemeOptions.Children.OfType<RadioButton>()) option.IsChecked = (string)option.Tag == draft.Theme;
        foreach (var option in AccentOptions.Children.OfType<RadioButton>()) option.IsChecked = (string)option.Tag == draft.Accent;
        foreach (var option in ScaleOptions.Children.OfType<RadioButton>()) option.IsChecked = double.Parse((string)option.Tag, CultureInfo.InvariantCulture) == draft.TextScale;
        HexInput.Text = draft.CustomAccent;
        loading = false;
        ApplyPreview();
    }

    private void ApplyPreview()
    {
        CustomPanel.Visibility = draft.Accent == "Custom" ? Visibility.Visible : Visibility.Collapsed;
        ColorFeedback.Text = "";
        ThemeService.Apply(draft);
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        bool highContrast = ThemeService.UsesHighContrast;
        ContrastNotice.Text = SystemParameters.HighContrast
            ? "Windows high contrast is active and takes priority over your saved colors."
            : highContrast ? "High contrast uses a black background, white text, and yellow controls."
            : "System follows your Windows light or dark preference. Windows high contrast is always respected.";
        AccentOptions.IsEnabled = CustomPanel.IsEnabled = !highContrast;
        var accent = ((SolidColorBrush)Application.Current.Resources["AccentBrush"]).Color;
        var surface = ((SolidColorBrush)Application.Current.Resources["SurfaceBrush"]).Color;
        PaletteSummary.Text = highContrast ? "High contrast palette active" : $"{draft.Accent} · #{accent.R:X2}{accent.G:X2}{accent.B:X2} · {ThemeService.Contrast(accent, surface):0.0}:1 on cards";
    }

    private void ThemeChecked(object sender, RoutedEventArgs e) { if (loading) return; draft.Theme = (string)((RadioButton)sender).Tag; ApplyPreview(); }
    private void AccentChecked(object sender, RoutedEventArgs e) { if (loading) return; draft.Accent = (string)((RadioButton)sender).Tag; ApplyPreview(); }
    private void ScaleChecked(object sender, RoutedEventArgs e) { if (loading) return; draft.TextScale = double.Parse((string)((RadioButton)sender).Tag, CultureInfo.InvariantCulture); ApplyPreview(); }
    private void HexChanged(object sender, TextChangedEventArgs e) { if (!loading && ColorFeedback != null) ColorFeedback.Text = "Choose Apply color to preview, or Save changes to keep this color."; }

    private bool ApplyCustomColor()
    {
        if (draft.Accent != "Custom") return true;
        string hex = HexInput.Text.Trim();
        if (!ThemeService.TryParseHex(hex, out _))
        {
            ColorFeedback.Text = "Enter a color as #RRGGBB, using six digits from 0–9 or A–F.";
            Announce(ColorFeedback);
            HexInput.Focus(); HexInput.SelectAll();
            return false;
        }
        draft.CustomAccent = hex.ToUpperInvariant();
        ApplyPreview();
        ColorFeedback.Text = "Color applied to preview. Save changes to keep it.";
        Announce(ColorFeedback);
        return true;
    }

    private void ApplyColorClick(object sender, RoutedEventArgs e) => ApplyCustomColor();
    private void ResetClick(object sender, RoutedEventArgs e) { draft = new(); LoadControls(); }
    private void CancelClick(object sender, RoutedEventArgs e) => Close();
    private void SaveClick(object sender, RoutedEventArgs e)
    {
        if (!ApplyCustomColor()) return;
        try { save(draft); saved = true; Close(); }
        catch (Exception ex)
        {
            SaveFeedback.Text = "Could not save appearance. Try again, or cancel to restore your previous settings.";
            SaveFeedback.ToolTip = ex.Message;
            SaveFeedback.Visibility = Visibility.Visible;
            Announce(SaveFeedback);
        }
    }
    private static void Announce(TextBlock text) =>
        (UIElementAutomationPeer.FromElement(text) ?? UIElementAutomationPeer.CreatePeerForElement(text))?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
}
