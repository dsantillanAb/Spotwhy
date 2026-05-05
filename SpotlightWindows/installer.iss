; SpotWhy Installer for Windows
; Inno Setup Script

#define MyAppName "SpotWhy"
#define MyAppVersion "1.0"
#define MyAppPublisher "SpotWhy"
#define MyAppURL "https://github.com/spotwhy"
#define MyAppExeName "SpotWhy.exe"

[Setup]
AppId={{B4F2E5D8-9A3C-4E7F-8D1A-5C6B3F2E9A7D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=.\installer_output
OutputBaseFilename=SpotWhy-Setup-{#MyAppVersion}
SetupIconFile=app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
MinVersion=10.0.19041

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear un acceso directo en el escritorio"; GroupDescription: "Accesos directos:"

[Files]
Source: ".\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\publish\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\publish\*.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\publish\*.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "app.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\app.ico"
Name: "{autoprograms}\{#MyAppName} (Background)"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\app.ico"; Parameters: "--background"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\app.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Ejecutar {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNet8Installed: Boolean;
var
  Version: string;
begin
  Result := RegQueryStringValue(
    HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.NETCore.App',
    '8.0.0', Version);
  if not Result then
    Result := RegQueryStringValue(
      HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x86\sharedfx\Microsoft.NETCore.App',
      '8.0.0', Version);
  if not Result then
    Result := RegQueryStringValue(
      HKCU, 'SOFTWARE\Microsoft\NET Core Setup\NDP\v4\Full',
      'Release', Version);
end;

procedure OpenDotNetDownload;
var
  ErrorCode: Integer;
begin
  ShellExec('open', 'https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime',
    '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
end;

function InitializeSetup: Boolean;
begin
  if not IsDotNet8Installed then
  begin
    if MsgBox(
      'SpotWhy requiere .NET 8 Runtime para funcionar.' + #13#10 +
      '¿Deseas descargar e instalar .NET 8 ahora?' + #13#10#13#10 +
      'Se abrirá la página de descarga de Microsoft.',
      mbConfirmation, MB_YESNO) = IDYES then
    begin
      OpenDotNetDownload;
    end;
  end;
  Result := True;
end;
