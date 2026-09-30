using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AppVolumeController.Models;
using AppVolumeController.Services;

namespace AppVolumeController;

// Deterministic UI checks. Preview windows never create an AudioService or write user preferences.
internal static class UiTest
{
    // Safe for hosted Windows CI: no window, audio endpoint, or user preferences required.
    public static int RunLogic()
    {
        var log = new List<string>();
        int result = 0;
        void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); log.Add("PASS: " + message); }
        try { CheckPalettes(Assert); CheckPreferences(Assert); }
        catch (Exception ex) { log.Add("FAIL: " + ex); result = 1; }
        File.WriteAllLines(Path.Combine(AppContext.BaseDirectory, "logic-test.log"), log);
        return result;
    }

    public static int Run()
    {
        var log = new List<string>();
        string output = Path.Combine(AppContext.BaseDirectory, "ui-checks");
        Directory.CreateDirectory(output);
        void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); log.Add("PASS: " + message); }
        try
        {
            CheckPalettes(Assert);
            CheckPreferences(Assert);
            var preferences = new Preferences { Appearance = new() { Theme = "Dark" } };
            ThemeService.Apply(preferences.Appearance);
            var window = new MainWindow(preferences, preview: true);
            window.Show();
            Assert(File.Exists(Path.Combine(AppContext.BaseDirectory, "HELP.html")), "Offline help is included beside the application");
            Assert(AutomationProperties.GetName(window.HelpButton) == "Open user help" && AutomationProperties.GetAcceleratorKey(window.HelpButton) == "F1", "Help exposes an accessible label and its keyboard shortcut");
            window.LoadPreviewRows(new[]
            {
                new AppRow { Key = "game", Name = "Black Desert Online", Favorite = true, Volume = 42, Detail = "6 audio sessions" },
                new AppRow { Key = "discord", Name = "Discord", Favorite = true, Volume = 80, Detail = "3 audio sessions" },
                new AppRow { Key = "browser", Name = "Browser with a longer application name", Volume = 26, Muted = true, Detail = "2 audio sessions" }
            });
            Pump();
            Capture(window, output, "mixer-dark.png");
            window.SearchBox.Text = "dIsCoRd";
            Assert(window.VisibleRows.Cast<AppRow>().Single().Key == "discord", "Search matches names without case sensitivity");
            window.SearchBox.Text = "missing";
            Assert(window.Empty.Visibility == Visibility.Visible && window.EmptyTitle.Text == "No matching apps", "Search has a distinct empty state");
            window.SearchBox.Clear();
            window.FavoritesOnly.IsChecked = true;
            window.FavoritesOnly.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Assert(window.VisibleRows.Cast<AppRow>().Count() == 2, "Favorites filter keeps only favorites");
            window.FavoritesOnly.IsChecked = false;
            window.FavoritesOnly.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Pump();

            var card = (ContentPresenter)window.AppList.ItemContainerGenerator.ContainerFromIndex(0);
            var slider = Descendants<Slider>(card).Single();
            var peer = UIElementAutomationPeer.CreatePeerForElement(slider)!;
            Assert(peer.GetName() == "Black Desert Online volume, percent", "App slider exposes an application-specific screen-reader name");
            var range = (IRangeValueProvider)peer.GetPattern(PatternInterface.RangeValue)!;
            Assert(range.Minimum == 0 && range.Maximum == 100 && range.SmallChange == 1 && range.LargeChange == 10, "Slider exposes the correct accessible range and keyboard increments");
            range.SetValue(51);
            Assert(window.Rows[0].Volume == 51, "Accessible slider changes update the bound value");
            slider.Focus();
            Slider.IncreaseSmall.Execute(null, slider);
            Assert(window.Rows[0].Volume == 52, "Keyboard slider command updates volume");
            Assert(slider.IsKeyboardFocused, "Volume control accepts keyboard focus");
            Capture(window, output, "mixer-keyboard-focus.png");
            var favorite = Descendants<CheckBox>(card).First();
            var favoritePeer = UIElementAutomationPeer.CreatePeerForElement(favorite)!;
            Assert(favoritePeer.GetName() == "Favorite Black Desert Online", "Favorite exposes an application-specific name");
            favorite.Focus();
            ((IToggleProvider)favoritePeer.GetPattern(PatternInterface.Toggle)!).Toggle();
            Pump();
            Assert(favorite.IsKeyboardFocused, "Changing a favorite preserves focus instead of moving its card");
            favorite.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            Assert(slider.IsKeyboardFocused, "Tab moves from favorite to the app volume slider");
            slider.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            var mute = Descendants<CheckBox>(card).Last();
            Assert(mute.IsKeyboardFocused, "Tab moves from the slider to mute without a focus trap");
            var mutePeer = UIElementAutomationPeer.CreatePeerForElement(mute)!;
            Assert(mutePeer.GetName() == "Mute Black Desert Online", "Mute exposes an application-specific name");
            ((IToggleProvider)mutePeer.GetPattern(PatternInterface.Toggle)!).Toggle();
            Assert(window.Rows[0].Muted && (string)mute.Content == "Muted", "Accessible mute toggles update the bound state and visible text");
            window.SearchBox.Focus(); Pump();
            Assert(window.Rows[0].Key == "discord", "Favorite sorting resumes after focus leaves the app list");

            ThemeService.Apply(new() { Theme = "Light", Accent = "Blue" });
            Capture(window, output, "mixer-light.png");
            ThemeService.Apply(new() { Theme = "High contrast" });
            Capture(window, output, "mixer-high-contrast.png");
            ThemeService.Apply(new() { Theme = "Dark", TextScale = 2 });
            window.Width = 640; window.Height = 600;
            Capture(window, output, "mixer-large-text.png");
            Assert(window.MainScroll.ExtentWidth <= window.MainScroll.ViewportWidth + 1, "200% text has no horizontal overflow at minimum mixer width");
            window.MainScroll.ScrollToBottom(); Pump();
            Capture(window, output, "mixer-large-text-bottom.png");

            window.LoadPreviewRows(Array.Empty<AppRow>());
            window.MainScroll.ScrollToTop();
            ThemeService.Apply(new() { Theme = "Dark" });
            window.Width = 860; window.Height = 850;
            Capture(window, output, "mixer-empty.png");
            Assert(window.EmptyTitle.Text == "No audio sessions yet", "No-session state explains how to find apps");

            var original = new AppearancePreference { Theme = "Dark" };
            AppearancePreference? saved = null;
            var dialog = new AppearanceWindow(original, p => saved = p.Copy()) { Owner = window };
            dialog.Show(); Pump();
            Capture(dialog, output, "appearance.png");
            Select(dialog.ThemeOptions, "Light");
            Assert(ThemeService.Current.Theme == "Light", "Theme selection previews immediately");
            Select(dialog.AccentOptions, "Custom");
            dialog.HexInput.Text = "#XYZ123";
            Click(dialog.SaveButton);
            Assert(dialog.IsVisible && saved == null && dialog.ColorFeedback.Text.Contains("#RRGGBB"), "Invalid custom color blocks saving and explains the format");
            dialog.HexInput.Text = "#FFFFFF";
            Click(dialog.SaveButton);
            Assert(saved?.CustomAccent == "#FFFFFF" && saved.Theme == "Light" && !dialog.IsVisible, "Saving persists the requested theme and valid custom color");
            Assert(ThemeService.Current.Theme == "Light", "Saved preview remains active");

            dialog = new AppearanceWindow(saved!, _ => throw new IOException("Simulated write failure")) { Owner = window };
            dialog.Show(); Pump();
            Select(dialog.ThemeOptions, "Dark");
            Click(dialog.SaveButton);
            Assert(dialog.IsVisible && dialog.SaveFeedback.Visibility == Visibility.Visible, "Save failure leaves settings open with recoverable feedback");
            Select(dialog.ScaleOptions, "2");
            dialog.Width = 520; dialog.Height = 600;
            Capture(dialog, output, "appearance-large-text.png");
            Assert(dialog.SettingsScroll.ExtentWidth <= dialog.SettingsScroll.ViewportWidth + 1, "200% settings text has no horizontal overflow at minimum width");
            dialog.SettingsScroll.ScrollToBottom(); Pump();
            Capture(dialog, output, "appearance-large-text-bottom.png");
            dialog.Close();
            Assert(ThemeService.Current.Theme == "Light" && ThemeService.Current.TextScale == 1, "Cancel or window close restores the full original appearance");
            dialog = new AppearanceWindow(saved!, p => saved = p.Copy()) { Owner = window };
            dialog.Show(); Pump();
            Click(Descendants<Button>(dialog).Single(b => b.Content as string == "Reset appearance"));
            Assert(ThemeService.Current.Theme == "System" && ThemeService.Current.Accent == "Teal" && ThemeService.Current.TextScale == 1, "Reset previews default appearance");
            dialog.Close();
            Assert(ThemeService.Current.Theme == "Light" && saved!.CustomAccent == "#FFFFFF", "Canceling a reset preserves the saved appearance");
            window.Close();
            File.WriteAllLines(Path.Combine(output, "results.log"), log);
            return 0;
        }
        catch (Exception ex)
        {
            log.Add("FAIL: " + ex);
            File.WriteAllLines(Path.Combine(output, "results.log"), log);
            return 1;
        }
    }

    private static void CheckPalettes(Action<bool, string> assert)
    {
        int checkedPalettes = 0;
        foreach (string theme in new[] { "Light", "Dark", "High contrast" })
        foreach (string accent in new[] { "Teal", "Blue", "Violet", "Amber", "Custom" })
        foreach (string custom in new[] { "#FFFFFF", "#000000", "#888888", "#FF0000", "#00FF00", "#0000FF" })
        {
            var p = ThemeService.CreatePalette(new() { Theme = theme, Accent = accent, CustomAccent = custom });
            foreach (var surface in new[] { p.Background, p.Surface, p.Raised, p.Hover })
            {
                if (ThemeService.Contrast(p.Text, surface) < 4.5 || ThemeService.Contrast(p.Secondary, surface) < 4.5 ||
                    ThemeService.Contrast(p.Accent, surface) < 4.5 || ThemeService.Contrast(p.Border, surface) < 3)
                    throw new Exception($"Insufficient contrast: {theme}/{accent}/{custom}");
            }
            if (ThemeService.Contrast(p.AccentText, p.Accent) < 4.5) throw new Exception("Selected indicator contrast failed");
            checkedPalettes++;
        }
        assert(true, $"{checkedPalettes} palette combinations meet 4.5:1 text and 3:1 control contrast");
        assert(!ThemeService.TryParseHex("red", out _) && !ThemeService.TryParseHex("#123", out _) && ThemeService.TryParseHex("#a1B2c3", out _), "Custom hex validation is strict and case insensitive");
        var systemDark = ThemeService.CreatePalette(new() { Theme = "System" }, systemDark: true);
        assert(systemDark == ThemeService.CreatePalette(new() { Theme = "Dark" }), "System theme resolves to the Windows preference");
        var systemContrast = ThemeService.CreatePalette(new() { Theme = "Light" }, systemHighContrast: true);
        assert(systemContrast.Background == SystemColors.WindowColor && systemContrast.Text == SystemColors.WindowTextColor && systemContrast.Accent == SystemColors.HighlightColor, "Windows high contrast overrides the chosen palette");
    }

    private static void CheckPreferences(Action<bool, string> assert)
    {
        string path = Path.Combine(Path.GetTempPath(), "appvolume-ui-" + Guid.NewGuid() + ".json");
        try
        {
            var preferences = new Preferences { Appearance = new() { Theme = "Dark", Accent = "Custom", CustomAccent = "#AABBCC", TextScale = 2 }, RememberVolumes = true };
            preferences.For("discord").Volume = 37;
            preferences.Save(path);
            var loaded = Preferences.Load(path);
            assert(loaded.Appearance.Theme == "Dark" && loaded.Appearance.CustomAccent == "#AABBCC" && loaded.Appearance.TextScale == 2 && loaded.For("DISCORD").Volume == 37 && loaded.RememberVolumes, "Appearance persists alongside existing audio preferences");
            File.WriteAllText(path, "{\"RememberVolumes\":true,\"Apps\":{}}");
            assert(Preferences.Load(path).Appearance.Theme == "System" && Preferences.Load(path).RememberVolumes, "Old preference files receive appearance defaults without losing audio settings");
            File.WriteAllText(path, "{\"Appearance\":{\"Theme\":\"invalid\",\"Accent\":null,\"CustomAccent\":\"oops\",\"TextScale\":99}}");
            loaded = Preferences.Load(path);
            assert(loaded.Appearance.Theme == "System" && loaded.Appearance.Accent == "Teal" && loaded.Appearance.CustomAccent == "#14B8A6" && loaded.Appearance.TextScale == 1, "Invalid appearance preferences recover to safe defaults");
        }
        finally { File.Delete(path); }
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    private static void Select(Panel panel, string tag) => panel.Children.OfType<RadioButton>().Single(r => (string)r.Tag == tag).IsChecked = true;
    private static void Click(Button button) { button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)); Pump(); }
    private static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
    private static void Capture(Window window, string directory, string name)
    {
        window.UpdateLayout(); Pump();
        var image = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth), (int)Math.Ceiling(window.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        image.Render(window);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = File.Create(Path.Combine(directory, name)); encoder.Save(stream);
    }
}
