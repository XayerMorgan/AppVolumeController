# App Volume Controller 0.1.0-beta.2

The first public beta of a native Windows mixer with per-app volume/mute, favorites, app search, accessible themes, and custom accent palettes.

## Download

- **Setup.exe**: recommended. Installs for the current user, includes .NET, creates a Start menu shortcut, offers a desktop shortcut, and supports uninstall.
- **Portable.zip**: extract the complete archive and run `AppVolumeController.exe`.
- **HELP.html**: offline help; also included in both packages and available through Help / F1.
- **TESTING.txt**: setup instructions and a short test checklist.
- **LICENSE.txt** and **SHA256SUMS.txt**: MIT license and download checksums.

Requires Windows 10 22H2 (build 19045) or Windows 11 and an x64-compatible PC. Intel/AMD x64 is tested; Windows on ARM is unverified. No separate .NET installation or administrator access is required. GitHub's Source code archives do not contain the compiled app.

**Unsigned beta:** Windows may display an unknown-publisher or SmartScreen warning. Use this official repository and decide whether you trust the release before running it. Checksums are provided for comparison; they are not a digital signature.

## Included

- App volume, mute, favorites, search, and an optional setting to remember changed volumes.
- Master control for the Windows default multimedia playback device.
- System/light/dark/high contrast themes, preset and custom accents, and 100–200% text size.
- Keyboard navigation, visible focus, application-specific automation labels, and readable palette checks.
- Offline and online help, troubleshooting, build instructions, and issue forms.

Audio sessions with the same executable name are grouped across active playback devices. Changes apply to the entire group. Microphone and exclusive-mode audio control are not supported.

## Feedback

Please use the [issue forms](https://github.com/XayerMorgan/AppVolumeController/issues/new/choose). Include the release version, Windows version, installation type, affected app/device, steps, and expected/actual behavior. Avoid posting private audio or your full preferences file.

Hands-on screen-reader testing, device reconnects, mixed-DPI monitors, and wider hardware coverage remain areas for beta feedback. There is no automatic updater; check Releases for future builds. Upgrades and uninstall retain preferences.

See the [user guide](https://github.com/XayerMorgan/AppVolumeController/blob/main/docs/USER-GUIDE.md) and [changelog](https://github.com/XayerMorgan/AppVolumeController/blob/main/CHANGELOG.md).
