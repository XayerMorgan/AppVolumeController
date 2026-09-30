using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AppVolumeController.Models;
using AppVolumeController.Services;

namespace AppVolumeController;

public partial class MainWindow : Window
{
    public ObservableCollection<AppRow> Rows { get; } = new();
    public ICollectionView VisibleRows { get; }
    private readonly AudioService? audio;
    private readonly Preferences preferences;
    private readonly bool preview;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(3) };
    private readonly DispatcherTimer saveTimer = new() { Interval = TimeSpan.FromMilliseconds(450) };
    private HashSet<string> previous = new();
    private bool syncing;
    private DateTime lastKeyboardAdjustment;
    private string? saveError;

    public MainWindow(Preferences? preferences = null, bool preview = false)
    {
        this.preferences = preferences ?? Preferences.Load();
        this.preview = preview;
        if (!preview) audio = new();
        VisibleRows = CollectionViewSource.GetDefaultView(Rows);
        VisibleRows.Filter = MatchesFilter;
        InitializeComponent();
        Height = Math.Min(Height, SystemParameters.WorkArea.Height);
        DataContext = this;
        Remember.IsChecked = this.preferences.RememberVolumes;
        Loaded += (_, _) => { if (!preview) Refresh(); };
        timer.Tick += (_, _) =>
        {
            if (Mouse.Captured == null && DateTime.UtcNow - lastKeyboardAdjustment > TimeSpan.FromMilliseconds(800)) Refresh();
        };
        saveTimer.Tick += (_, _) => { saveTimer.Stop(); Save(); };
        AppList.LostKeyboardFocus += (_, _) => Dispatcher.BeginInvoke(new Action(() => { if (!AppList.IsKeyboardFocusWithin) Sort(); }));
        if (!preview) timer.Start();
        Closed += (_, _) => { timer.Stop(); if (saveTimer.IsEnabled) { saveTimer.Stop(); Save(); } audio?.Dispose(); };
    }

    private bool MatchesFilter(object value) => value is AppRow row &&
        (FavoritesOnly?.IsChecked != true || row.Favorite) &&
        row.Name.Contains(SearchBox?.Text.Trim() ?? "", StringComparison.CurrentCultureIgnoreCase);

    private void Refresh(bool announce = false)
    {
        if (audio == null) return;
        syncing = true;
        try
        {
            audio.Scan();
            foreach (var group in audio.Sessions.GroupBy(s => s.Key))
            {
                var first = group.First();
                var pref = preferences.For(group.Key);
                var sessions = group.ToList();
                foreach (var session in sessions.Where(s => !previous.Contains(s.Id)))
                    if (preferences.RememberVolumes)
                        try
                        {
                            if (pref.Volume.HasValue) session.Control.SimpleAudioVolume.Volume = Math.Clamp(pref.Volume.Value, 0, 100) / 100f;
                            if (pref.Muted.HasValue) session.Control.SimpleAudioVolume.Mute = pref.Muted.Value;
                        }
                        catch { }
                var row = Rows.FirstOrDefault(r => r.Key == group.Key);
                if (row == null)
                {
                    row = new() { Key = group.Key, Name = first.Name, Icon = GetIcon(first.Path), Favorite = pref.Favorite };
                    row.Changed = Change;
                    Rows.Add(row);
                }
                row.Sessions = sessions;
                try
                {
                    row.Sync((int)Math.Round(first.Control.SimpleAudioVolume.Volume * 100), sessions.All(s => s.Control.SimpleAudioVolume.Mute));
                    bool mixedVolume = sessions.Any(s => Math.Abs(s.Control.SimpleAudioVolume.Volume * 100 - row.Volume) > 1);
                    bool mixedMute = !row.Muted && sessions.Any(s => s.Control.SimpleAudioVolume.Mute);
                    row.Detail = sessions.Count == 1 ? "1 audio session" : $"{sessions.Count} audio sessions";
                    if (mixedVolume) row.Detail += " · Different volumes";
                    if (mixedMute) row.Detail += " · Some muted";
                }
                catch { row.Detail = "Session reconnecting…"; }
            }
            var keys = audio.Sessions.Select(s => s.Key).ToHashSet();
            foreach (var row in Rows.Where(r => !keys.Contains(r.Key)).ToList())
            {
                if (Keyboard.FocusedElement is FrameworkElement focused && focused.DataContext == row) SearchBox.Focus();
                Rows.Remove(row);
            }
            Sort();
            previous = audio.Sessions.Select(s => s.Id).ToHashSet();
            MasterSlider.IsEnabled = MasterMute.IsEnabled = audio.Master != null;
            DeviceName.Text = audio.Master?.FriendlyName ?? "No default playback device";
            if (audio.Master != null)
            {
                MasterSlider.Value = Math.Round(audio.Master.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
                MasterMute.IsChecked = audio.Master.AudioEndpointVolume.Mute;
                MasterValue.Text = $"{MasterSlider.Value:0}%";
            }
            else { MasterSlider.Value = 0; MasterMute.IsChecked = false; MasterValue.Text = "—"; }
            UpdateListState();
            SetStatus(saveError ?? (audio.Error == null
                ? (announce ? "Audio sessions refreshed. Listening for new apps." : "All playback devices · Refreshes automatically every 3 seconds")
                : "Some audio devices are unavailable. Check your output connection, then rescan."), announce || audio.Error != null);
            Status.ToolTip = saveError ?? audio.Error;
        }
        catch (Exception e) { SetStatus("Unable to refresh audio. Rescan to try again.", true); Status.ToolTip = e.Message; }
        finally { syncing = false; }
    }

    private void Sort()
    {
        // Keep tab order and focus stable while someone is working inside an app card.
        if (AppList.IsKeyboardFocusWithin || Mouse.Captured != null) return;
        var ordered = Rows.OrderByDescending(r => r.Favorite).ThenBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            int current = Rows.IndexOf(ordered[i]);
            if (current != i) Rows.Move(current, i);
        }
    }

    private void Change(AppRow row, string field)
    {
        if (syncing) return;
        var pref = preferences.For(row.Key);
        if (field == "favorite")
        {
            pref.Favorite = row.Favorite;
            Sort();
            if (FavoritesOnly.IsChecked == true && !row.Favorite)
            {
                SearchBox.Focus();
                VisibleRows.Refresh();
            }
            UpdateListState();
            Save();
            return;
        }
        if (preview) return;
        int succeeded = 0;
        foreach (var session in row.Sessions)
            try
            {
                if (field == "volume") session.Control.SimpleAudioVolume.Volume = row.Volume / 100f;
                else session.Control.SimpleAudioVolume.Mute = row.Muted;
                succeeded++;
            }
            catch { }
        if (succeeded > 0)
        {
            if (field == "volume") pref.Volume = row.Volume; else pref.Muted = row.Muted;
            saveTimer.Stop(); saveTimer.Start();
        }
        if (succeeded < row.Sessions.Count) SetStatus("An app's audio session changed. Rescan to reconnect.", true);
    }

    private void Save()
    {
        if (preview) return;
        try { preferences.Save(); if (saveError != null) SetStatus("Preferences saved.", true); saveError = null; }
        catch (Exception e) { saveError = "Could not save preferences. Your changes will last only for this session."; SetStatus(saveError, true); Status.ToolTip = e.Message; }
    }

    private void SetStatus(string text, bool announce = false)
    {
        if (Status.Text == text) return;
        Status.Text = text;
        if (announce) (UIElementAutomationPeer.FromElement(Status) ?? UIElementAutomationPeer.CreatePeerForElement(Status))?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }

    private void UpdateListState()
    {
        int count = VisibleRows.Cast<AppRow>().Count();
        AppCount.Text = count == Rows.Count ? $"{count} connected" : $"{count} of {Rows.Count} shown";
        Empty.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyTitle.Text = Rows.Count == 0 ? "No audio sessions yet" : "No matching apps";
        EmptyDescription.Text = Rows.Count == 0 ? "Play some audio in an app, then rescan. Apps will also appear automatically."
            : "Try a different name or turn off Favorites only to see more apps.";
    }

    private static ImageSource? GetIcon(string? path)
    {
        if (path == null) return null;
        try
        {
            using var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
            if (icon == null) return null;
            var image = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            image.Freeze(); return image;
        }
        catch { return null; }
    }

    private void RefreshClick(object sender, RoutedEventArgs e) => Refresh(true);
    private void RememberClick(object sender, RoutedEventArgs e) { preferences.RememberVolumes = Remember.IsChecked == true; Save(); }
    private void SearchChanged(object sender, TextChangedEventArgs e) { if (Empty == null) return; RefreshFilter(); }
    private void FilterClick(object sender, RoutedEventArgs e) => RefreshFilter();
    private void RefreshFilter()
    {
        string previousCount = AppCount.Text;
        VisibleRows.Refresh(); UpdateListState();
        if (previousCount != AppCount.Text)
            (UIElementAutomationPeer.FromElement(AppCount) ?? UIElementAutomationPeer.CreatePeerForElement(AppCount))?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }
    private void ClearSearchClick(object sender, RoutedEventArgs e) { SearchBox.Clear(); SearchBox.Focus(); }

    private void AppearanceClick(object sender, RoutedEventArgs e)
    {
        var dialog = new AppearanceWindow(preferences.Appearance, appearance =>
        {
            var previousAppearance = preferences.Appearance;
            preferences.Appearance = appearance.Copy();
            try { if (!preview) preferences.Save(); }
            catch { preferences.Appearance = previousAppearance; throw; }
        }) { Owner = this };
        dialog.ShowDialog();
        AppearanceButton.Focus();
    }

    private void WindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F1) { HelpClick(this, e); e.Handled = true; }
        else if (e.Key == Key.F5) { Refresh(true); e.Handled = true; }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F) { SearchBox.Focus(); SearchBox.SelectAll(); e.Handled = true; }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.OemComma) { AppearanceClick(this, e); e.Handled = true; }
        else if (e.Key == Key.Escape && SearchBox.IsKeyboardFocused) { SearchBox.Clear(); e.Handled = true; }
        if (Keyboard.FocusedElement is Slider && e.Key is Key.Left or Key.Right or Key.Up or Key.Down or Key.PageUp or Key.PageDown or Key.Home or Key.End)
            lastKeyboardAdjustment = DateTime.UtcNow;
    }

    private void HelpClick(object sender, RoutedEventArgs e)
    {
        string path = System.IO.Path.Combine(AppContext.BaseDirectory, "HELP.html");
        try
        {
            if (!System.IO.File.Exists(path)) throw new System.IO.FileNotFoundException("HELP.html is missing from the application folder.");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            SetStatus("Could not open help. Open HELP.html in the app folder or visit the project's GitHub user guide.", true);
            Status.ToolTip = ex.Message;
        }
    }

    private void MasterChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MasterValue != null) MasterValue.Text = $"{e.NewValue:0}%";
        if (syncing || audio?.Master == null) return;
        try { audio.Master.AudioEndpointVolume.MasterVolumeLevelScalar = (float)e.NewValue / 100; }
        catch (Exception ex) { SetStatus("Output unavailable. Check your device, then rescan.", true); Status.ToolTip = ex.Message; }
    }
    private void MasterMuteClick(object sender, RoutedEventArgs e)
    {
        try { if (audio?.Master != null) audio.Master.AudioEndpointVolume.Mute = MasterMute.IsChecked == true; }
        catch (Exception ex) { SetStatus("Could not change output mute. Rescan to reconnect.", true); Status.ToolTip = ex.Message; }
    }

    internal void LoadPreviewRows(IEnumerable<AppRow> rows)
    {
        Rows.Clear();
        foreach (var row in rows) { row.Changed = Change; Rows.Add(row); }
        DeviceName.Text = "Speakers · Preview output";
        MasterSlider.Value = 72;
        Sort(); UpdateListState();
        SetStatus("Preview · No audio settings are changed");
    }
}
