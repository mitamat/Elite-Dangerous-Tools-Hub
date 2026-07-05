; Inno Setup script for Elite Dangerous Tools Hub.
; Requires the self-contained single-file build to exist at ..\publish-sc\EDHub.exe
; (dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
;  -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o ..\publish-sc)

#define MyAppName "EDHub"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "mitamat"
#define MyAppURL "https://github.com/mitamat/Elite-Dangerous-Tools-Hub"
#define MyAppExeName "EDHub.exe"

[Setup]
AppId={{EE8D4781-F06C-4DD7-838D-C287FC436C91}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=EDHub-Setup
SetupIconFile=..\EDHub\icon.ico
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\publish-sc\EDHub.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
