; Compile with scripts/Build.ps1 -Installer. Keep AppId stable for upgrades.
#ifndef AppVersion
  #error AppVersion must be provided by the build script
#endif
#ifndef FileVersion
  #error FileVersion must be provided by the build script
#endif
#ifndef SourceDir
  #error SourceDir must point to a clean self-contained publish directory
#endif
#ifndef RepoRoot
  #error RepoRoot must point to the repository root
#endif

[Setup]
AppId={{8772141F-185B-43D4-9B82-FE433835C4B7}
AppName=App Volume Controller
AppVersion={#AppVersion}
AppVerName=App Volume Controller {#AppVersion}
AppPublisher=App Volume Controller
AppPublisherURL=https://github.com/XayerMorgan/AppVolumeController
AppSupportURL=https://github.com/XayerMorgan/AppVolumeController/issues
AppUpdatesURL=https://github.com/XayerMorgan/AppVolumeController/releases
VersionInfoVersion={#FileVersion}
VersionInfoProductName=App Volume Controller
VersionInfoProductTextVersion={#AppVersion}
VersionInfoDescription=App Volume Controller Setup
DefaultDirName={localappdata}\Programs\AppVolumeController
DefaultGroupName=App Volume Controller
DisableProgramGroupPage=auto
AllowNoIcons=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19045
SetupIconFile={#RepoRoot}\src\AppVolumeController\Assets\speaker.ico
UninstallDisplayIcon={app}\AppVolumeController.exe
AppMutex=AppVolumeController.Running
CloseApplications=no
RestartApplications=no
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
OutputDir={#RepoRoot}\releases
OutputBaseFilename=AppVolumeController-{#AppVersion}-win-x64-Setup
Uninstallable=yes
UninstallDisplayName=App Volume Controller
UsePreviousAppDir=yes
UsePreviousGroup=yes
UsePreviousTasks=yes
DisableWelcomePage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Excludes: "*.pdb,*.log,ui-checks\*,ui-smoke.png,*.tmp"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\App Volume Controller"; Filename: "{app}\AppVolumeController.exe"; WorkingDir: "{app}"; IconFilename: "{app}\AppVolumeController.exe"
Name: "{group}\Testing guide"; Filename: "{app}\TESTING.txt"
Name: "{group}\User help"; Filename: "{app}\HELP.html"
Name: "{autodesktop}\App Volume Controller"; Filename: "{app}\AppVolumeController.exe"; WorkingDir: "{app}"; IconFilename: "{app}\AppVolumeController.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\AppVolumeController.exe"; Description: "Open App Volume Controller"; Flags: nowait postinstall skipifsilent

; Preferences live outside {app} and are intentionally retained after uninstall.
