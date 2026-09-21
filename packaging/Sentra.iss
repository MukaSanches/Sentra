#define MyAppName "SENTRA"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Samuel Sanches"
#define MyAppExeName "SENTRA.exe"

[Setup]
AppId={{6E0E8716-9F2A-4C10-965D-75E5024DDF28}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\SENTRA
DefaultGroupName=SENTRA
DisableProgramGroupPage=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=SENTRA-Setup-1.0.0-win-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Files]
Source: "..\artifacts\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\SENTRA"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\SENTRA"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos:"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir SENTRA"; Flags: nowait postinstall skipifsilent
