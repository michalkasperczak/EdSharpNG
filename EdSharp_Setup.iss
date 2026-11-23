; EdSharp_Setup.iss -- Inno Setup script for the AnyCPU EdSharp baseline (x64 and ARM64).
;
; Compile with ISCC.exe (Inno Setup 5.6+ or 6.x). Run BuildEdSharp.cmd
; first so EdSharp.exe, EdSharp.dll, and nvdaControllerClient.dll exist.
; Produces EdSharp_Setup.exe in C:\EdSharp.
;
; This is a slimmed, 64-bit replacement for the old edsharp_setup.iss.
; The legacy Java / JRE detection block and the obsolete 32-bit support
; assemblies (JsSupport, VbSupport, saapi32, nvdaControllerClient32) have
; been removed. Native code generation via ngen is kept; on ARM64 it targets
; the ARM64 framework, and HasNgen skips it gracefully if ngen is absent.

[Setup]
AppName=EdSharpNG
AppVersion=5.0.133
AppVerName=EdSharpNG 5.0.133
VersionInfoVersion=5.0.133
SetupIconFile=EdSharp.ico
UninstallDisplayIcon={app}\EdSharpNG.exe
AppPublisher=Michal Kasperczak
AppPublisherURL=https://github.com/michalkasperczak/EdSharpNG
AppCopyright=EdSharpNG changes copyright 2026 by Michal Kasperczak; based on EdSharp, copyright 2006-2026 by Jamal Mazrui
DefaultDirName={autopf}\EdSharpNG
DefaultGroupName=EdSharpNG
; x64compatible matches both x64 and ARM64 (Inno Setup 6.3+), so the AnyCPU
; EdSharp.exe installs and runs natively on both.  MinVersion 10.0 matches the
; .NET Framework 4.8 / Windows 10+ requirement.
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
Compression=lzma2/max
SolidCompression=yes
OutputBaseFilename=EdSharpNG_Setup_5.0.133
OutputDir=out
SourceDir=stage
PrivilegesRequired=admin
ChangesAssociations=yes
ChangesEnvironment=yes
DisableProgramGroupPage=yes
DisableStartupPrompt=yes
Uninstallable=yes
SetupLogging=yes
; Updates require the editor to be closed normally. Never force close.
CloseApplications=no
RestartApplications=no
AppMutex=EdSharpNG_Running_Mutex

[Files]
; Release 5.0.131: exact staged runtime/source payload.  This deliberately avoids
; inherited skipifsourcedoesntexist and Excludes rules that could silently omit data.
; Preserve the user configuration on upgrade: the recursive payload entry excludes it, and the explicit entry below installs only the default for a new installation.
Source: "*"; DestDir: "{app}"; Excludes: ".edsharp-staging,EdSharp.ini,\Synonyms\plwordnet-forms.bin.gz"; Flags: ignoreversion recursesubdirs
Source: "EdSharp.ini"; DestDir: "{app}"; Flags: onlyifdoesntexist

[Dirs]
Name: "{userappdata}\EdSharp";
Name: "{userappdata}\EdSharp\Temp";

[InstallDelete]
; Clear out any pre-existing EdSharp desktop shortcut before the [Icons] section
; recreates the single hot-key shortcut below.  This matters because EdSharp --
; unlike DbDo, which is a brand-new app -- has a legacy installer that placed an
; Alt+Ctrl+E shortcut on the USER's desktop pointing at the old exe, and an
; earlier 5.0 install placed a hot-key-less shortcut on the COMMON desktop.
; Removing both leaves the {autodesktop} shortcut below as the sole owner of
; Alt+Ctrl+E.  (InstallDelete runs before [Icons], so the recreate still wins.)
Type: files; Name: "{userdesktop}\EdSharp.lnk"
Type: files; Name: "{commondesktop}\EdSharp.lnk"
Type: files; Name: "{userdesktop}\EdSharpNG.lnk"
Type: files; Name: "{commondesktop}\EdSharpNG.lnk"

[Icons]
Name: "{group}\Launch EdSharpNG";   Filename: "{app}\EdSharpNG.exe"; WorkingDir: "{app}"
Name: "{group}\EdSharpNG Manual";   Filename: "{app}\EdSharp.htm"
Name: "{group}\EdSharpNG Tutorial"; Filename: "{app}\Tutorial.htm"
Name: "{group}\EdSharpNG 5.0.1 Announcement"; Filename: "{app}\Announce.htm"
Name: "{group}\Uninstall EdSharpNG"; Filename: "{uninstallexe}"
; Desktop shortcut WITHOUT a global hot key: Alt+Ctrl+E was dropped because on a
; Polish keyboard AltGr+E types the letter e-ogonek (e with tail) and Windows maps
; AltGr as Ctrl+Alt, so the hot key would swallow that character system-wide.
; EdSharpNG is single-instance (OnStartupNextInstance brings the running copy to
; the foreground), so launching from the shortcut still just activates it.
Name: "{autodesktop}\EdSharpNG"; Filename: "{app}\EdSharpNG.exe"; WorkingDir: "{app}"; IconFilename: "{app}\EdSharp.ico"; Comment: "Launch or activate EdSharpNG 5.0.1"

[Run]
; STRONA FINISH JEST PUSTA - SWIADOMIE (zgloszenie Kasperczaka 01.09: "zeby on
; nie instalowal tych skryptow do JOSa i wtyczki do NVDA, bo juz mam to
; zainstalowane").  Trzy kroki postinstall (skrypty JAWS oraz dwa dodatki NVDA)
; zostaly ZDJETE.  Dla osoby niewidomej strona Finish z trzema polami wyboru to
; realny koszt przy KAZDEJ aktualizacji, a wersje testowe wychodza czesto.
;
; PLIKI ZOSTAJA W KATALOGU PROGRAMU i nie zniknely z sekcji [Files]: kto zechce
; zainstalowac dodatek NVDA, otwiera EdSharpNG-spellcheck.nvda-addon z katalogu
; programu, a skrypty JAWS wgrywa poleceniem "EdSharpNG.exe
; --install-jaws-settings".  Zdjete jest wiec KLIKANIE, nie funkcja - i to
; zostaje odwracalne jednym wpisem, gdyby przy upublicznieniu okazalo sie
; potrzebne (ustalenie edsharpng-27 mowi, ze skrypty JAWS wchodza do instalatora
; dopiero przy upublicznieniu).
; Pre-generate native images for faster startup (64-bit ngen).
; ngen zostaje: dziala ukryty (runhidden), nie wymaga zadnego klikniecia i
; skraca start programu.
Filename: "{code:NgenExe}"; Parameters: "uninstall EdSharp /nologo /silent"; Flags: runhidden; Check: HasNgen
Filename: "{code:NgenExe}"; Parameters: "install ""{app}\EdSharpNG.exe"" /AppBase:""{app}"" /nologo /silent"; Flags: runhidden; Check: HasNgen

[UninstallRun]
Filename: "{code:NgenExe}"; Parameters: "uninstall EdSharp /nologo /silent"; Flags: runhidden; Check: HasNgen

[UninstallDelete]
Type: files; Name: "{app}\EdSharpNG.exe"
Type: files; Name: "{app}\EdSharp.dll"
Type: files; Name: "{app}\BuildEdSharp.log"

[Registry]
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\EdSharpNG.exe"; ValueType: string; ValueName: ""; ValueData: "{app}\EdSharpNG.exe"; Flags: uninsdeletekey

[Code]
function NgenExe(sParam: string): string;
begin
  // ngen ships with the 64-bit .NET Framework runtime; on an ARM64 system the
  // Framework64 path is the ARM64 framework.  HasNgen guards a missing file.
  result := ExpandConstant('{win}\Microsoft.NET\Framework64\v4.0.30319\ngen.exe');
end;

function HasNgen(): boolean;
begin
  result := FileExists(ExpandConstant('{code:NgenExe}'));
end;


