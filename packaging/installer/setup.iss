; Inno Setup Script for WoodStream PLAZA Desktop Client
#define MyAppName "WoodStream PLAZA"
#define MyAppVersion "1.0.0.1"
#define MyAppPublisher "WoodStream Networks Tomokazu Kizawa"
#define MyAppURL "https://windows-podcast.com/plaza/"
#define MyAppExeName "WoodStreamPlaza.exe"

#ifndef TargetArch
  #define TargetArch "x64"
#endif

#if TargetArch == "arm64"
  #define ArchFolder "win-arm64"
  #define ArchInstallMode "arm64"
  #define ArchAllowed "arm64"
#else
  #define ArchFolder "win-x64"
  #define ArchInstallMode "x64compatible"
  #define ArchAllowed "x64compatible"
#endif

[Setup]
AppId={{D97D8BF4-7C91-44E2-8113-E0CA2962C24B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\WoodStreamPlaza
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\..\Installer
OutputBaseFilename=WoodStreamPlaza_Setup_{#MyAppVersion}_{#TargetArch}
SetupIconFile=..\..\app.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed={#ArchAllowed}
ArchitecturesInstallIn64BitMode={#ArchInstallMode}
PrivilegesRequired=lowest
DisableProgramGroupPage=yes

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\..\publish\{#ArchFolder}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
