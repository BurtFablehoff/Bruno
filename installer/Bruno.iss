#ifndef ProductionExecutable
  #define ProductionExecutable "..\test-exes\Bruno.exe"
#endif

[Setup]
AppId={{5477976A-F8EC-4233-BA5D-0312BF2727F5}
AppName=Bruno
AppVersion=0.1.3
AppVerName=Bruno 0.1.3
AppPublisher=Bruno Companion
DefaultDirName={localappdata}\Programs\Bruno
DefaultGroupName=Bruno
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
DisableProgramGroupPage=yes
AppMutex=Local\BrunoCompanionV1
CloseApplications=yes
RestartApplications=no
UninstallDisplayName=Bruno
UninstallDisplayIcon={app}\Bruno.exe
OutputDir=..\build\release
OutputBaseFilename=Bruno-Setup-0.1.3
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
VersionInfoVersion=0.1.3.0

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#ProductionExecutable}"; DestDir: "{app}"; DestName: "Bruno.exe"

[Icons]
Name: "{group}\Bruno"; Filename: "{app}\Bruno.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Bruno"; Filename: "{app}\Bruno.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\Bruno.exe"; Description: "Launch Bruno"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  StartupCommand: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'Bruno', StartupCommand) then
      if CompareText(StartupCommand, '"' + ExpandConstant('{app}\Bruno.exe') + '"') = 0 then
        RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'Bruno');
  end;
end;
