[Setup]
AppName={#AppName}
AppVersion={#AppVersion}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputDir=Output
OutputBaseFilename={#AppName}-{#AppVersion}-setup
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}";        Filename: "{app}\{#ExeName}"
Name: "{autodesktop}\{#AppName}";  Filename: "{app}\{#ExeName}"

[Run]
Filename: "{app}\{#ExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent