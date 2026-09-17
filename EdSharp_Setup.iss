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
AppVersion=5.0.12
AppVerName=EdSharpNG 5.0.12 (beta)
VersionInfoVersion=5.0.12
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
OutputBaseFilename=EdSharpNG_Setup
OutputDir=C:\EdSharp
SourceDir=C:\EdSharp
PrivilegesRequired=admin
ChangesAssociations=yes
ChangesEnvironment=yes
DisableProgramGroupPage=yes
DisableStartupPrompt=yes
Uninstallable=yes
SetupLogging=yes
; TRYB CICHY (zlecenie Kasperczaka 11.09.2026: "Instalator w trybie cichym").
; Instalator obsluguje juz /SILENT i /VERYSILENT z samego Inno Setup, ale bez
; ponizszych ustawien tryb cichy PRZERYWAL prosba o zamkniecie dzialajacego
; EdSharpNG - a w trybie cichym nie ma komu tej prosby pokazac, wiec instalacja
; stawala.  Teraz:
;   CloseApplications=force   - dzialajacy EdSharpNG jest zamykany sam,
;   RestartApplications=no    - i NIE jest wznawiany przez instalator, bo to
;                               robi sam program (patrz InstallUpdateSilently
;                               w EdSharp.cs), zeby wrocil z tym samym
;                               dokumentem, a nie z pustym oknem,
;   AppMutex                  - po czym instalator ma po czym rozpoznac, ze
;                               program faktycznie zniknal z pamieci.
; Wywolanie bez interfejsu:  EdSharpNG_Setup.exe /VERYSILENT /NORESTART
CloseApplications=force
RestartApplications=no
AppMutex=EdSharpNG_Running_Mutex

[Files]
; Built artifacts (present after BuildEdSharp.cmd).
Source: "EdSharpNG.exe";        DestDir: "{app}"; Flags: ignoreversion
; Runtime configuration for EdSharpNG.exe -- carries the startup tuning (disables
; Authenticode publisher-evidence/CRL checks, enables concurrent GC).  It must
; sit next to EdSharpNG.exe, and ignoreversion ensures it is always refreshed so
; it stays in sync with the executable.
Source: "EdSharpNG.exe.config"; DestDir: "{app}"; Flags: ignoreversion
; EdSharp.dll (host JScript .NET) NIE JEST JUZ PAKOWANA - warstwa skryptow
; usunieta 16.09.2026 (docs/CO-USUWAMY.md 2.2).  Wpis w [UninstallDelete]
; ZOSTAJE, zeby plik z poprzednich instalacji zniknal przy odinstalowaniu.
Source: "nvdaControllerClient.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
; Source and build inputs (shipped so users can recompile, EdSharp-style).
Source: "EdSharp.cs";         DestDir: "{app}"; Flags: ignoreversion
Source: "Lbc.cs";             DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "Say.cs";             DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "Inix.cs";            DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "KeyMap.cs";          DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "Web.cs";             DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "Pisownia.cs";        DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "Skladniki.cs";       DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "Csv.cs";             DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "Sesja.cs";           DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "Ustawienia.cs";      DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
; Wyrazenia.cs -- wlasny kalkulator wyrazen, nastepca warstwy JScript .NET
; (16.09.2026, docs/CO-USUWAMY.md 2.2).  Zrodla jada z paczka, zeby program dal
; sie przekompilowac u uzytkownika, jak w oryginale Jamala.
Source: "Wyrazenia.cs";       DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
; Zadania.cs -- listy zadan (checklisty Markdown), funkcje czyste
Source: "Zadania.cs";         DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
; Zmiany.cs -- sledzenie zmian (CriticMarkup), funkcje czyste, krok 1a
Source: "Zmiany.cs";          DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "EdSharp.ico";        DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
; EdSharp.js (zrodlo hosta JScript .NET) NIE JEST JUZ PAKOWANY - warstwa
; skryptow usunieta 16.09.2026 razem z EdSharp.dll (docs/CO-USUWAMY.md 2.2).
Source: "EdSharp.manifest";   DestDir: "{app}"; Flags: ignoreversion
Source: "BuildEdSharp.cmd";   DestDir: "{app}"; Flags: ignoreversion
Source: "FetchConvertTools.ps1";   DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "FetchUde.ps1";            DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "ModernizePandocConfig.ps1"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "Tools.inix";              DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "EdSharp_Setup.iss";  DestDir: "{app}"; Flags: ignoreversion
Source: "Tektosyne.dll";      DestDir: "{app}"; Flags: ignoreversion
Source: "Ude.dll";            DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
; JAWS settings family (compiled into each installed JAWS version by [Code]).
Source: "Scripts\*";        DestDir: "{app}\Scripts"; Flags: ignoreversion recursesubdirs skipifsourcedoesntexist
; NVDA add-ons (installed on the Finish page via [Run]).
Source: "EdSharp.nvda-addon"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "EdSharpNG-spellcheck.nvda-addon"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
; Configuration: do not clobber a user's existing settings on upgrade.
Source: "EdSharp.ini";        DestDir: "{app}"; Flags: onlyifdoesntexist
; Hotkeys.ini holds ONLY the command descriptions spoken by Key Describer and
; the Hotkey Summary -- the old per-item [Keys] rebinding was removed, so this
; is a program resource, not user config.  It MUST be refreshed on upgrade,
; otherwise an existing install keeps announcing chords that have moved.
Source: "Hotkeys.ini";        DestDir: "{app}"; Flags: ignoreversion
; Hotkey Summary (Alt+Shift+H) opens this file from the program directory.
; The name must match what the code opens: EdSharp_Hotkeys.txt.  The old
; "HotKeys.txt" line staged a file that has not existed since 5.0.73, and the
; skipifsourcedoesntexist flag hid that silently.
Source: "EdSharp_Hotkeys.txt"; DestDir: "{app}"; Flags: ignoreversion
; Documentation.
Source: "EdSharp.md";         DestDir: "{app}"; Flags: ignoreversion
Source: "EdSharp.htm";        DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "Tutorial.md";        DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "Tutorial.htm";       DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "Announce.md";        DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "Announce.htm";       DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "Transform_Example.inix"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "CamelType_JAWSScript.md"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "CamelType_CSharp.md"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "EdSharp.inix"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "history.txt";        DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "lgpl.txt";           DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
; Data trees.
Source: "Snippets\*"; DestDir: "{app}\Snippets"; Flags: recursesubdirs ignoreversion skipifsourcedoesntexist
Source: "Convert\*";  DestDir: "{app}\Convert";  Excludes: "*.sln,*.vcproj,*.vcxproj,*.vcxproj.filters,*.suo,*.user,*.c,*.asm,*.cs,*.obj,*.zip,temp.htm,temp.txt"; Flags: recursesubdirs ignoreversion skipifsourcedoesntexist

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


