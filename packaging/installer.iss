; ============================================================
; QuickTidy (SuLi) - Inno Setup installer script (per-user)
; Build:  iscc packaging\installer.iss
; Output: packaging\out\QuickTidy-1.0.0-setup.exe
; ============================================================
#define MyAppName "QuickTidy"
#define MyAppVer "1.0.0"

[Setup]
AppId={{6B2F5B7E-3A44-4B7C-9E8D-2A5C7F0D9E31}
AppName={#MyAppName}
AppVersion={#MyAppVer}
AppPublisher=QuickTidy
DefaultDirName={localappdata}\Programs\QuickTidy
PrivilegesRequired=lowest
DisableProgramGroupPage=yes
OutputDir=out
OutputBaseFilename=QuickTidy-{#MyAppVer}-setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName=QuickTidy
UninstallDisplayIcon={app}\QuickTidy.exe
CloseApplications=no

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; Flags: unchecked

[Files]
Source: "payload\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{autoprograms}\QuickTidy"; Filename: "{app}\QuickTidy.exe"
Name: "{autodesktop}\QuickTidy"; Filename: "{app}\QuickTidy.exe"; Tasks: desktopicon

[Run]
; 注册经典右键菜单 + 尝试注册 Win11 新式菜单（静默，结果写日志 %APPDATA%\QuickTidy\QuickTidy.log）
Filename: "{app}\QuickTidy.exe"; Parameters: "/setup"; Flags: runhidden
Filename: "{app}\QuickTidy.exe"; Description: "Open QuickTidy settings"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\QuickTidy.exe"; Parameters: "/cleanup"; Flags: runhidden; RunOnceId "QuickTidyCleanup"
