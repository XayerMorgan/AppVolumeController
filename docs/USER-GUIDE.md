# App Volume Controller — User guide

[Download](https://github.com/XayerMorgan/AppVolumeController/releases) · [Report an issue](https://github.com/XayerMorgan/AppVolumeController/issues/new/choose) · [Project overview](../README.md)

## Install

1. From Releases, download the versioned **Setup.exe**, then run it. This installs for your Windows account, adds a Start menu shortcut, and offers an optional desktop shortcut.
2. Start **App Volume Controller** from the Start menu.
3. Play audio in an app. It appears automatically; select **Rescan** for an immediate update.

The release includes .NET. Requirements: Windows 10 22H2 (build 19045) or Windows 11, an x64-compatible PC, and a working playback device. Intel/AMD x64 is tested; Windows on ARM is unverified.

For a portable copy, download **Portable.zip**, extract the entire archive, and run `AppVolumeController.exe`. Keep all files together. GitHub's automatically generated Source code archives are for building the app, not installing it.

**Unsigned beta:** Windows may show an unknown-publisher or SmartScreen warning. Use the official release page and decide whether you trust the file before running it. Do not disable Windows security protections. `SHA256SUMS.txt` allows comparison with the published files; it is not a digital signature. To calculate a downloaded file's hash in PowerShell, run `Get-FileHash -Algorithm SHA256 '.\downloaded-file.exe'`.

## Mixer controls

- **Volume:** each application has a 0–100% slider. Volume changes keep the current mute state.
- **Mute:** checked and labeled **Muted** when active. Uncheck to restore sound.
- **Favorite:** keeps an app near the top. Sorting waits until focus leaves the app cards to keep keyboard navigation stable. Black Desert Online and Discord are favorites by default.
- **Find an app:** filters names as you type. Clear removes the search; **Favorites only** is a separate filter.
- **Master output:** controls only the current Windows default multimedia playback device. The label follows device changes. Controls are disabled when no default device is available.
- **Remember my volume changes:** off by default. When enabled, previously changed volume/mute settings are restored as new sessions appear. Turning it off stops restoration without deleting saved values. Checking it does not immediately overwrite connected sessions.

Sessions with the same executable name are grouped across active playback devices. Changes apply to every session in the group. Displayed volume comes from the first session; mute is checked only if all grouped sessions are muted. **Different volumes** / **Some muted** describe mixed states. Moving the slider aligns session volumes.

Sessions update about every three seconds. Quiet but unexpired sessions remain available. Refresh pauses during mouse dragging and briefly after keyboard volume adjustments. The app controls playback sessions, not microphone inputs or exclusive-mode audio.

## Appearance and accessibility

Open **Appearance** or press **Ctrl+,**.

- **System / Light / Dark / High contrast:** System follows the Windows app theme. Active Windows high contrast takes priority and uses system colors.
- **Teal / Blue / Violet / Amber / Custom:** a custom color uses `#RRGGBB`, such as `#14B8A6`. The displayed shade is adjusted for contrast while the requested color remains saved. The preview shows the effective shade and card contrast.
- **100% / 125% / 150% / 200% text:** applies in addition to Windows display scaling. Content wraps and scrolls as text grows.

Changes preview immediately. **Save changes** persists them; **Cancel**, **Escape**, or closing the dialog restores the previous appearance. **Reset appearance** previews defaults; save to keep the reset. Favorites and audio settings are unaffected.

The app uses native controls, visible keyboard focus, application-specific screen-reader labels, and text/checkmarks as well as color. There are no interface animations. Palette checks use [W3C text contrast guidance](https://www.w3.org/WAI/WCAG21/Understanding/contrast-minimum) and [non-text contrast guidance](https://www.w3.org/TR/WCAG22/#non-text-contrast). These checks do not establish complete accessibility conformance; screen-reader feedback is welcome.

## Keyboard shortcuts

| Shortcut | Action |
| --- | --- |
| Tab / Shift+Tab | Move between controls |
| Arrow keys | Adjust a focused slider by 1%; select a radio option |
| Page Up / Page Down | Adjust a focused slider by 10% |
| Home / End | Set a focused slider to 0% / 100% |
| Space | Toggle a focused checkbox |
| Ctrl+F | Focus app search |
| Escape in search | Clear search text |
| F5 | Rescan audio sessions |
| Ctrl+, | Open appearance settings |
| Escape in appearance | Cancel the preview |
| F1 in the mixer | Open the included offline help in your browser |

The same help is available as `HELP.html` beside the executable and through the Start menu's **User help** shortcut.

## Troubleshooting

| Symptom | What to try |
| --- | --- |
| An app is missing | Play audio in it, clear search, turn off Favorites only, and rescan. It must create a Windows playback session. |
| Slider moves but no sound | Check app mute, master mute/volume, the app's own settings, and its output device. Changing volume does not unmute. |
| Master control affects the wrong output | Master follows the Windows default multimedia device. Change that default in Windows Sound settings, then rescan. App sessions may be on other devices. |
| Device unavailable | Reconnect/select a playback device, then rescan. |
| Old volumes return after restarting an app | Turn off Remember my volume changes to stop restoration for new sessions. |
| Custom color differs from the typed hex | The displayed shade is adjusted for readability; Windows high contrast also overrides custom accents. |
| Settings cannot be saved | Check disk space and write permission for `%LOCALAPPDATA%\AppVolumeController`. Changes may last only for the current session after a save error. |
| Portable app will not start | Extract the entire ZIP and retain every file. Confirm the Windows requirements and report the exact error if it continues. |
| Help does not open | Open `HELP.html` manually in a browser or use this online guide. Reinstall/re-extract if the file is missing. |

## Preferences, privacy, and backups

Favorites, remembered volume/mute values, and appearance settings are saved in `%LOCALAPPDATA%\AppVolumeController\preferences.json`. This can identify applications you use; avoid posting the entire file publicly. Close the app before making a backup.

The app does not upload your app list or preferences. No background service, startup task, telemetry, or automatic updater is installed. Closing its window stops it. Help opens a local file; following GitHub links uses your browser's internet connection.

To reset all settings intentionally, close the app and rename `preferences.json` to `preferences.backup.json`; the next launch uses defaults. To reset only appearance, use the Appearance dialog.

## Update or uninstall

Close the app and run a newer installer from Releases. It reuses the existing installation and retains preferences. Uninstall through **Windows Settings → Apps → Installed apps** (Apps & features on Windows 10). Preferences are retained for reinstall. The portable version uses the same settings location.

## Report a problem

Use the [issue forms](https://github.com/XayerMorgan/AppVolumeController/issues/new/choose). Include the version from the downloaded release, Windows version, install type, application/output device, steps, and expected/actual behavior. For accessibility issues include assistive technology, text size, and display scaling. Screenshots are optional; exclude private data.

See [development instructions](DEVELOPMENT.md) to build from source and [contribution guidelines](../CONTRIBUTING.md) to submit changes.
