# Development and releases

## Requirements

- Windows 10 22H2 / Windows 11, Intel/AMD x64.
- .NET 10 SDK, Git, and PowerShell.
- Inno Setup 6.4+ for installer builds only.
- A working playback device for local Core Audio and live startup checks.

The build uses `dotnet` from PATH, or the optional private SDK under `%LOCALAPPDATA%\AppVolumeControllerSDK`. Inno Setup can be supplied explicitly with `-InnoSetupPath 'C:\path\ISCC.exe'` or `INNO_SETUP_PATH`; common installed locations are also detected. Build scripts do not install tools automatically.

## Build and verify

```powershell
.\Build.ps1
.\Run.cmd

# Logic, UI, Core Audio, and startup checks in a separate verification folder.
.\Build.ps1 -Verify

# Self-contained installer, portable ZIP, offline help, MIT license, and checksums.
.\Build.ps1 -Installer -Verify

# Installs a temporary copy, verifies it, reinstalls it, then removes it.
.\scripts\Test-Installer.ps1
```

Close running copies first. The installer check refuses to replace an existing registered installation. Its temporary app and logs are under `artifacts/installer-test`; it verifies the installed payload, shortcut target/icon, uninstall registration, startup, and unchanged user preferences. A failed test attempts to uninstall only its own temporary copy.

Local `--ui-test` checks use sample rows and never create an audio service or write user preferences. `--self-test` creates and changes only its own silent audio session. `--smoke-test` reads real sessions with remembered-volume restoration disabled. `--logic-test` covers palettes and preference persistence without UI focus or audio hardware; this is the check used on hosted Windows CI.

Direct diagnostic arguments write results beside the executable, so run them from a writable development folder. `Build.ps1 -Verify` automatically uses an isolated folder and keeps all logs/screenshots out of release packages.

## Project layout

| Path | Contents |
| --- | --- |
| `src/AppVolumeController/` | WPF windows, models, services, icon assets, and project file |
| `tests/AppVolumeController.Checks/` | Diagnostic checks linked into the app |
| `scripts/` | Build, installer verification, and icon generation |
| `installer/` | Inno Setup definition |
| `docs/` | Online/offline help, development guide, and sample screenshots |
| `licenses/` | Source dependency notices |
| `.github/` | Windows CI, issue forms, and pull request template |
| `artifacts/` | Ignored build intermediates, publish output, and verification results |
| `releases/` | Ignored release binaries, guides, and checksums |

`Directory.Build.props` routes intermediates into `artifacts`. Legacy outputs may be archived locally under `artifacts/legacy-output`; they are not published. `Run.cmd` launches the current publish folder.

## Prepare a release

1. Update `Version`, `AssemblyVersion`, and `FileVersion` in the project. Use a beta version until validation warrants a stable release.
2. Update `CHANGELOG.md`, relevant help pages, download links, and release notes. Keep the installer AppId unchanged.
3. Run `Build.ps1 -Installer -Verify`, then `scripts/Test-Installer.ps1` on a clean installation account.
4. Check the package contents and `SHA256SUMS.txt`. Include the MIT license and all runtime/dependency notices. Do not package screenshots of live user sessions, preferences, logs, signing keys, or build intermediates.
5. Commit the verified source and create an annotated version tag. Publish that tag's installer, ZIP, `HELP.html`, `TESTING.txt`, `LICENSE.txt`, and `SHA256SUMS.txt` as GitHub Release assets. Mark beta releases as prereleases.

Binaries are not committed to Git. Releases are currently unsigned; disclose this in the download instructions. Never disable platform security protections or distribute a signing key in the repository.

## UI and documentation changes

Use semantic brushes in `ThemeService` and `App.xaml`. Preserve keyboard commands, screen-reader labels, and text cues. Verify minimum window widths at 200% text. Keep `docs/HELP.html` fully offline, without external scripts/fonts/tracking; it is copied beside the EXE during both build and publish.

After UI changes, regenerate sample renders with the UI checks and copy only `mixer-dark.png` and `appearance.png` from the sample UI output into `docs/images`. Never use the real-device smoke screenshot for public documentation. `scripts/Generate-Icon.ps1` regenerates the app ICO, matching favicon, and PNG.
