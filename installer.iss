[Setup]
AppName=FoldVision
AppVersion=1.0.0
AppPublisher=FoldVision
DefaultDirName={autopf}\FoldVision
DisableProgramGroupPage=yes
OutputBaseFilename=FoldVision_Setup
SetupIconFile=FV.ico
Compression=none
SolidCompression=no
WizardStyle=modern

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "publish\FoldVision\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "FV.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\FoldVision"; Filename: "{app}\FoldVision.exe"; IconFilename: "{app}\FV.ico"
Name: "{autodesktop}\FoldVision"; Filename: "{app}\FoldVision.exe"; Tasks: desktopicon; IconFilename: "{app}\FV.ico"

[Run]
Filename: "{app}\FoldVision.exe"; Description: "{cm:LaunchProgram,FoldVision}"; Flags: nowait postinstall skipifsilent
