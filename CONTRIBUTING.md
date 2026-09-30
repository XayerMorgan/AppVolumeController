# Contributing

Bug reports, accessibility feedback, documentation fixes, and small focused pull requests are welcome.

## Report a problem

Read the [troubleshooting guide](docs/USER-GUIDE.md#troubleshooting), search existing issues, then use the [bug report form](https://github.com/XayerMorgan/AppVolumeController/issues/new/choose). Include the app version, Windows version, installation type, reproduction steps, and expected/actual behavior. For audio issues include the application and output device; for accessibility issues include your assistive technology and display settings.

Do not post secrets, private audio, or a complete preferences file. Preferences can identify applications you use.

## Submit a change

1. Fork the repository and create a branch for one change.
2. Follow the [development instructions](docs/DEVELOPMENT.md).
3. Run checks relevant to the change and describe the results in your pull request.
4. Update user help when a control, setting, shortcut, or installation step changes.

Use the shared semantic theme resources for UI colors. Keep native keyboard behavior, visible focus, descriptive automation labels, and readable layouts at 200% text. Do not use color as the only state indicator. Test audio changes only with your own test session unless you intentionally choose to change another app's volume.

Changes to the installer should preserve the AppId and existing preferences. Avoid adding generated binaries, user preferences, logs, private paths, or signing keys to commits. Keep third-party notices with distributed code.

By contributing, you agree that your contribution is provided under this project's [MIT license](LICENSE). Be respectful, describe concrete problems, and keep discussion focused on improving the app.
