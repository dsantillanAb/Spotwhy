#define MyAppName "SpotWhy"
#define MyAppVersion "1.0"
#define MyAppPublisher "Daniel Santillan"
#define MyAppExeName "SpotWhy.exe"
#define MyAppId "SpotWhy"

[Setup]
AppId={{#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=installer_output
OutputBaseFilename=SpotWhy-Setup-{#MyAppVersion}
SetupIconFile={#SourcePath}\SpotlightWindows\app.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=user
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "runonstartup"; Description: "Iniciar con Windows"; GroupDescription: "Opciones adicionales:"

[Files]
Source: "publish_selfcontained\*"; DestDir: "{app}"; Flags: recursesubdirs
Source: "publish_selfcontained\SpotWhy.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{autostartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: runonstartup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function UpdateCheckForUpdates: Boolean;
begin
  Result := False;
end;
