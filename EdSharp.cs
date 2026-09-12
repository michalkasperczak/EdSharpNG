//EdSharpNG 5.0 - a continuation of EdSharp, maintained by Michal Kasperczak.
//
//Based on EdSharp by Jamal Mazrui.
//Original work copyright 2007 - 2026 by Jamal Mazrui.
//Modifications for EdSharpNG copyright 2026 by Michal Kasperczak.
// GNU Lesser General Public License (LGPL)
//
//This is a modified version.  It is not released or supported by the
//original author; reports about EdSharpNG belong on
//https://github.com/michalkasperczak/EdSharpNG/issues

using Microsoft.VisualBasic.ApplicationServices;
using System.Windows.Automation.Provider;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Tektosyne.NetMail ;
using Tektosyne.Win32Api;
using Homer;

[assembly: AssemblyTitle("EdSharpNG")]
[assembly: AssemblyProduct("EdSharpNG")]
[assembly: AssemblyVersion("5.0.*")]
[assembly: AssemblyDescription("EdSharpNG editor")]
[assembly: AssemblyCompany("Michal Kasperczak")]
[assembly: AssemblyCopyright("Based on EdSharp, copyright 2007 - 2026 by Jamal Mazrui.  EdSharpNG modifications copyright 2026 by Michal Kasperczak.  LGPL v3.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCulture("")]

namespace EdSharp {
public class App : WindowsFormsApplicationBase {
// Dotted-numeric version used by the Elevate Version command to compare with
// the latest GitHub release tag, oraz przez okno About.
//
// JEDNO ZRODLO PRAWDY O WERSJI (naprawa 06.09.2026).  Wczesniej numer stal w
// TRZECH miejscach niezaleznie: tutaj "5.0.0", w oknie About literal
// "EdSharpNG 5.0.1 (beta)" i w EdSharp_Setup.iss (podbijany przez skrypt buildu
// tylko w stagingu).  Skutek zmierzony: instalator mowil 5.0.70, a program o
// sobie 5.0.1 - czyli po instalacji nie bylo JAK sprawdzic, ktora wersje sie
// ma.  Dla osoby niewidomej testujacej kolejne paczki to najwazniejsza
// informacja w calym oknie About.
public const string VersionString = "5.0.91";
// GDZIE IDA ZGLOSZENIA (dolozone 11.09.2026).  Adres formularza zgloszen w
// NASZYM repozytorium; uzywany przez "Report a Problem" i przez okno awarii,
// gdy nie ma skonfigurowanego punktu odbiorczego (klucz ReportUrl w pliku
// ustawien).  Adres e-mail opiekuna programu NIE jest wpisany w kod - siedzi w
// pliku ustawien pod kluczem ReportMail, bo adres prywatny nie ma czego szukac
// w binarce rozdawanej testerom.
public const string ReportIssuesUrl = "https://github.com/michalkasperczak/EdSharpNG/issues/new";
public static App Shell;
public static MdiFrame Frame;
public static string ProgramName;
public static string NetDir;
public static string ProgramDir;
public static string DataDir;
public static string DefaultIniFile;
public static string HotkeyIniFile;
public static string IniFile;
public static string IndentModeFile;
public static string TempFile;
public static List<string> TempFiles = new List<string>();
//public static object Word = null;
public static object Boo = null;
public static object JAWS = null;
public static object Wineyes = null;
public static bool WordCreated = false;
public static bool ExtraSpeech = true;
public static bool IndentChange = true;
public static bool CaptureOutput = false;
public static string SpeechLog;
public static string MatchChunk = @"\s+";
public static string MatchParagraph = @"\n(\s*\n)+\s*";
public static string MatchSentence = @"([.?!]\s+)|(" + MatchParagraph + ")";
public static Dictionary<string, int> BomDictionary = null;

// Uchwyt mutexu rozpoznawanego przez instalator (AppMutex w
// EdSharp_Setup.iss).  Statyczny, zeby nie zostal sprzatniety przez GC w
// trakcie dzialania programu - bo wtedy instalator uznalby, ze program juz sie
// zamknal, i podmienil pliki pod dzialajaca kopia.
private static System.Threading.Mutex mutexRunning = null;

[STAThread]
public static void Main(string[] cmdLineArgs) {
// Installer Finish-page option: "EdSharp.exe --install-jaws-settings" copies
// EdSharp's JAWS settings family into every installed JAWS version and
// compiles them there, then reports and exits without launching the editor.
// (DbDo-style: the compile logic is here in C#, invoked from the installer's
// [Run] section, not done silently in the installer script.)
foreach (string sArg in cmdLineArgs) {
if (sArg.Equals("--install-jaws-settings", StringComparison.OrdinalIgnoreCase)
 || sArg.Equals("/install-jaws-settings", StringComparison.OrdinalIgnoreCase)) {
int iCopied, iCompiled;
string sScriptsDir = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Scripts");
string sReport = JawsScripts.install(sScriptsDir, out iCopied, out iCompiled);
MessageBox.Show(sReport, "EdSharp JAWS scripts: " + iCopied + " copied, " + iCompiled + " compiled");
return;
}
}
// MUTEX DLA INSTALATORA W TRYBIE CICHYM (dolozone 11.09.2026 razem z
// CloseApplications/AppMutex w EdSharp_Setup.iss).  Nazwa MUSI byc identyczna
// z AppMutex w skrypcie instalatora: EdSharpNG_Running_Mutex.
// Po co: w trybie cichym nie ma komu pokazac prosby "zamknij program", wiec
// instalator musi sam sprawdzic, czy EdSharpNG siedzi w pamieci, i poczekac az
// zniknie.  Bez tego cicha aktualizacja albo staje, albo podmienia pliki pod
// dzialajacym programem - a to drugie konczy sie awaria u kogos, kto ma
// otwarty niezapisany dokument.
// "Local\\" (nie "Global\\") swiadomie: wystarczy jedna sesja uzytkownika, a
// mutex globalny wymaga uprawnien, ktorych zwykly start programu nie ma.
// Uchwyt trzymamy w statycznym polu do konca zycia procesu i NIE zwalniamy go
// jawnie - system oddaje go przy zakonczeniu, takze po awarii.
// Cale w try: brak mozliwosci zalozenia mutexu nie moze przeszkodzic w
// uruchomieniu edytora.
try { mutexRunning = new System.Threading.Mutex(false, "Local\\EdSharpNG_Running_Mutex"); }
catch {}

// Multicore background JIT: record JIT decisions on first launch and, on
// later launches, compile methods in parallel on background cores. This
// shortens startup for a large single-assembly app. Wrapped so a failure
// (e.g. read-only profile folder) never blocks launch.
try {
string sProfileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EdSharp");
Directory.CreateDirectory(sProfileDir);
System.Runtime.ProfileOptimization.SetProfileRoot(sProfileDir);
System.Runtime.ProfileOptimization.StartProfile("startup.jitprofile");
}
catch {}
// Environment.SetEnvironmentVariable("EdSharpIndent", "", EnvironmentVariableTarget.User);
if (System.IO.File.Exists(App.IndentModeFile)) System.IO.File.Delete(App.IndentModeFile);
Application.EnableVisualStyles();
//Application.SetCompatibleTextRenderingDefault(true);
Application.SetCompatibleTextRenderingDefault(false);
Application.OleRequired();

Shell = new App();
Shell.Run(cmdLineArgs);
} // Main method

public App() {
base.IsSingleInstance = true;
/*
//this.IsSingleInstance = true;
base.IsSingleInstance = true;
App.ProgramName = GetAppName();
App.NetDir = RuntimeEnvironment.GetRuntimeDirectory();
// App.ProgramDir = GetProgramDir();
App.ProgramDir = GetProgramDir();
App.DataDir = GetDataDir();
App.TempFile = GetTempFile();
App.DefaultIniFile = GetDefaultIniFile();
App.HotkeyIniFile = Path.Combine(App.ProgramDir, "Hotkeys.ini");
App.IniFile = GetIniFile();

App.BomDictionary = Util.GetBomDictionary();
SetConfigurationValues();
App.SpeechLog = Path.Combine(App.DataDir, "Speech.log");
if (File.Exists(App.SpeechLog)) File.Delete(App.SpeechLog);
App.ExtraSpeech = (App.ReadOption("E&xtraSpeech", "Y").ToLower().Substring(0, 1) == "n") ? false : true;

InitNetSdk();
InitJFW();
*/

this.Shutdown += delegate(object o, EventArgs e) {
if (App.WordCreated) {
Util.Say("Exiting Microsoft Word");
COM.WordExit();
}
if (App.Boo != null) COM.Release(ref App.Boo);
if (App.JAWS != null) COM.Release(ref App.JAWS);
if (App.Wineyes != null) COM.Release(ref App.Wineyes);

if (System.IO.File.Exists(App.IndentModeFile)) System.IO.File.Delete(App.IndentModeFile);
foreach (string sFile in App.TempFiles) if (File.Exists(sFile)) File.Delete(sFile);
};

this.UnhandledException += delegate(object sender, Microsoft.VisualBasic.ApplicationServices.UnhandledExceptionEventArgs e) {
Exception ex = (Exception) e.Exception;
string sMessage = ex.Message;
sMessage += "\n\nStack trace:\n" + ex.StackTrace;
// sMessage += "\nExit EdSharp?\n\nStack trace:\n" + ex.StackTrace;
// e.ExitApplication = Dialog.Confirm("Confirm", "Unexpected event!\n" + sMessage + ".\nExit EdSharp?", "N") == "Y";
string[] aButtons = {"&Report the Problem", "Copy to Clipboard", "Exit EdSharp"};
string sButton = Dialog.Choose("Unexpected Event", sMessage, aButtons, 0);
switch (sButton) {
case "&Report the Problem" :
// ZGLOSZENIE AWARII IDZIE DO NAS, NIE DO AUTORA ORYGINALU (naprawa 11.09.2026).
// Do tej pory ten przycisk nazywal sie "Mail to Developer" i wysylal awarie
// EdSharpNG na jamal@EmpowermentZone.com - adres autora programu, z ktorego
// forkowalismy.  Zgloszenie o NASZEJ zmianie szlo wiec do czlowieka, ktory tego
// kodu nie pisal i nie moze go naprawic, a my nie dowiadywalismy sie o awarii
// wcale.  Teraz slad awarii wchodzi do tego samego okna zgloszen, co reszta:
// z opisem, kopia na dysku i uczciwym komunikatem, czy wyszlo.
Util.Say("Please add steps to reproduce the problem, if possible.");
try {
if (App.Shell != null && App.Frame != null) App.Frame.ReportProblem("EdSharp error: " + ex.Message, sMessage);
}
catch {}
break;
case "Copy to Clipboard" :
Util.SetClipboardText(sMessage);
break;
case "Exit EdSharp" :
// Application.Exit();
e.ExitApplication = true;
return;
}
e.ExitApplication = false;
};

this.Startup += delegate(object sender, Microsoft.VisualBasic.ApplicationServices.StartupEventArgs e) {

//this.IsSingleInstance = true;
// base.IsSingleInstance = true;
App.ProgramName = GetAppName();
App.NetDir = RuntimeEnvironment.GetRuntimeDirectory();
App.ProgramDir = GetProgramDir();
App.DataDir = GetDataDir();
App.TempFile = GetTempFile();
App.DefaultIniFile = GetDefaultIniFile();
App.HotkeyIniFile = Path.Combine(App.ProgramDir, "Hotkeys.ini");
App.IniFile = GetIniFile();
App.IndentModeFile = Path.Combine(App.DataDir, "IndentMode.tmp");
// IniFile = GetIniFile();

App.BomDictionary = Util.GetBomDictionary();
SetConfigurationValues();
MigrateDefaultExtensionToMarkdown();
MigrateBookmarksOutOfFavorites();
// Kolejnosc ma znaczenie: czyszczenie klucza ExtraSpeech musi pojsc PRZED jego
// odczytem ponizej, inaczej pierwszy start po aktualizacji nadal wczytalby
// zapisane "wylaczone" i user uslyszalby cisze do nastepnego uruchomienia.
ClearExtraSpeechOption();
App.SpeechLog = Path.Combine(App.DataDir, "Speech.log");
if (File.Exists(App.SpeechLog)) File.Delete(App.SpeechLog);
App.ExtraSpeech = (App.ReadOption("E&xtraSpeech", "Y").ToLower().Substring(0, 1) == "n") ? false : true;
App.IndentChange = App.ReadOption("E&xtraSpeech", "Y").Contains("-") ? false : true;

InitNetSdk();
InitJFW();

Frame = new MdiFrame();
Homer.Say.attach(Frame);
this.MainForm = Frame;
MdiChild child = new MdiChild(Frame);
if (App.ReadOption("OpenPrevious", "Y").ToLower().Substring(0, 1) != "n") {
string[] aFiles = App.ReadSectionKeys("Previous");
int iCount = 0;
foreach (string s in aFiles) {
if (!File.Exists(s)) continue;
iCount ++;
int iIndex = Int32.Parse(App.ReadValue("Previous", s, "-1"));
// App.Frame.OpenOrActivateWindow(s, 1);
App.Frame.OpenOrActivateWindow(s, App.Frame.GetViewLevel(s));
if (App.Frame.Child.RTB.Index == 0) App.Frame.Child.RTB.Index = iIndex;
}
if (iCount > 0) App.Frame.AddMessage("Opened " + iCount + " previous file" + (iCount == 1 ? "" : "s"));
}
App.DeleteSection("Previous");

ReadOnlyCollection<string> cmdLineArgs = this.CommandLineArgs;
if (cmdLineArgs.Count > 0) {
string sFile = cmdLineArgs[0];
string sLine = "";
string sColumn = "";
if (cmdLineArgs.Count > 1) sLine = cmdLineArgs[1];
if (cmdLineArgs.Count > 2) sColumn = cmdLineArgs[2];
// Frame.OpenOrActivateWindow(sFile, 1, sLine, sColumn);
App.Frame.OpenOrActivateWindow(sFile, App.Frame.GetViewLevel(sFile), sLine, sColumn);
}
// SPRAWDZANIE NOWEJ WERSJI PRZY URUCHOMIENIU (zadanie 7 z listy 11.09.2026).
// Ostatnia rzecz po otwarciu plikow: start ma sie skonczyc, a dopiero potem
// program moze zagladac do sieci.
CheckForUpdateOnStartup();
// Skladniki konwersji - osobne sprawdzenie, wlasny watek, wlasny wylacznik.
CheckComponentsOnStartup();
};

} // App constructor

// SPRAWDZANIE AKTUALIZACJI PRZY STARCIE - CICHE, W TLE, RAZ NA DOBE.
// Zlecenie Kasperczaka 11.09.2026: "Sprawdzanie nowej wersji przy uruchomieniu".
//
// TRZY REGULY, KTORYCH TU NIE WOLNO ZLAMAC:
//
// 1. ZERO OKIEN I ZERO PYTAN.  Osoba niewidoma po uruchomieniu edytora ma
//    fokus w dokumencie i zaczyna pisac.  Okno dialogowe wyskakujace sekunde
//    pozniej zabiera fokus i zjada wpisany tekst, a pytanie "czy pobrac"
//    zatrzymuje prace, ktorej nikt nie zaczynal po to, by aktualizowac
//    program.  Wiadomosc idzie wiec TYLKO do paska wiadomosci ramki
//    (AddMessage) - tam, gdzie i tak lada komunikaty startowe.  Pobranie
//    zostaje swiadomym wyborem: F11.
//
// 2. NIC NIE MOZE OPOZNIC STARTU.  Cala robota siedzi na watku w tle o
//    niskim priorytecie i jest oznaczona jako IsBackground, wiec zamkniecie
//    programu nie czeka na zawieszone polaczenie.  Gdy sieci nie ma, nie
//    mowimy NIC - user nie prosil o sprawdzenie, wiec nie ma go po co
//    informowac o nieudanym sprawdzeniu, ktorego nie zlecil.  To rozni sie od
//    F11, gdzie milczenie byloby zignorowaniem polecenia.
//
// 3. RAZ NA DOBE, NIE PRZY KAZDYM URUCHOMIENIU.  Edytor odpala sie po
//    kilkanascie razy dziennie i pytanie GitHuba za kazdym razem to ruch bez
//    wartosci.  Data ostatniego sprawdzenia siedzi w kluczu
//    UpdateCheckLastDate.
//
// Wylaczenie: w EdSharpNG.ini w sekcji [Options] wpisac
// CheckUpdateOnStartup=N.  Sprawdzanie zostaje domyslnie WLACZONE, bo
// przeoczona aktualizacja to dla testera realny koszt - siedzi na wersji z
// bledem, ktory jest juz naprawiony.
public void CheckForUpdateOnStartup() {
try {
if (App.ReadOption("CheckUpdateOnStartup", "Y").ToLower().StartsWith("n")) return;
string sToday = DateTime.Now.ToString("yyyy-MM-dd");
if (App.ReadData("UpdateCheckLastDate", "") == sToday) return;

System.Threading.Thread thread = new System.Threading.Thread(delegate() {
try {
int iHttp;
string sNotes, sAssetUrl;
string sTag = Util.FetchLatestRelease("michalkasperczak/EdSharpNG", "EdSharpNG_Setup.exe", out sNotes, out sAssetUrl, out iHttp);
// Date zapisujemy TYLKO po udanym sprawdzeniu.  Inaczej jeden dzien bez
// internetu kasowalby sprawdzanie do nastepnej doby.
if (sTag.Length == 0) return;
App.WriteData("UpdateCheckLastDate", sToday);
string sLatest = sTag.TrimStart('v', 'V').Trim();
if (Util.CompareVersions(sLatest, App.VersionString) <= 0) return;
// Wracamy na watek okna: AddMessage dotyka interfejsu.
if (App.Frame == null || !App.Frame.IsHandleCreated) return;
App.Frame.BeginInvoke((MethodInvoker)delegate() {
try {
App.Frame.AddMessage("EdSharpNG " + sLatest + " is available. Press F11 to update. You have " + App.VersionString + ".");
}
catch {}
});
}
catch {}
});
thread.IsBackground = true;
thread.Priority = System.Threading.ThreadPriority.BelowNormal;
thread.Start();
}
catch {}
} // CheckForUpdateOnStartup method

// SKLADNIKI CONVERT PRZY STARCIE (zadanie 8, 5.0.85).
//
// Narzedzia do konwersji (pandoc, tidy, xpdf, liblouis, astyle) NIE sa w
// instalatorze - to osobne programy obcych autorow, razem ponad 60 MB, i
// pakowanie ich do naszej paczki oznaczaloby, ze kazda nasza poprawka wazy
// tyle samo.  Skutek byl jednak taki, ze u uzytkownika po prostu ich nie bylo
// i konwersje milczaly.  Teraz program dociaga je sam.
//
// TE SAME ZASADY, CO PRZY SPRAWDZANIU WERSJI PROGRAMU (5.0.81):
//
// 1. ZERO OKIEN I ZERO PYTAN.  Meldunek na pasku wiadomosci, gdy cos
//    dociagnalem.  Pytanie "czy pobrac pandoca" nie daje uzytkownikowi
//    zadnego realnego wyboru - bez pandoca konwersja nie zadziala.
//
// 2. START SIE NIE OPOZNIA.  Watek w tle, niski priorytet, IsBackground.
//    Bez internetu program milczy.
//
// 3. PRZY STARCIE TYLKO TO, CZEGO BRAKUJE.  Aktualizowanie dzialajacych
//    narzedzi w tle byloby ryzykiem: mogloby trafic w chwile, gdy uzytkownik
//    wlasnie z nich korzysta.  Pelne aktualizowanie jest w menu, na zadanie.
//
// 4. RAZ NA DOBE.  Klucz ComponentCheckLastDate.  Data zapisywana TYLKO po
//    udanej probie, zeby jeden dzien bez sieci nie kasowal sprawdzania.
//
// RESTARTU NIE MA I NIE JEST POTRZEBNY: te narzedzia sa wolane dopiero w
// chwili konwersji, nic ich nie trzyma w pamieci, wiec swiezo pobrany pandoc
// dziala od razu.
//
// Wylaczenie: w EdSharpNG.ini w sekcji [Options] wpisac
// CheckComponentsOnStartup=N.
public void CheckComponentsOnStartup() {
try {
if (App.ReadOption("CheckComponentsOnStartup", "Y").ToLower().StartsWith("n")) return;
string sToday = DateTime.Now.ToString("yyyy-MM-dd");
if (App.ReadData("ComponentCheckLastDate", "") == sToday) return;

// Gdy nic nie brakuje, nie ruszam sieci wcale.
if (Skladniki.Brakujace().Count == 0) return;

System.Threading.Thread thread = new System.Threading.Thread(delegate() {
try {
bool bTylkoBrakujace = true;
string sZrobione = Skladniki.SprawdzIUzupelnij(bTylkoBrakujace);
App.WriteData("ComponentCheckLastDate", sToday);
if (sZrobione.Length == 0) return;
if (App.Frame == null || !App.Frame.IsHandleCreated) return;
App.Frame.BeginInvoke((MethodInvoker)delegate() {
try {
App.Frame.AddMessage("Pobrano brakujace skladniki konwersji: " + sZrobione + ".");
}
catch {}
});
}
catch {}
});
thread.IsBackground = true;
thread.Priority = System.Threading.ThreadPriority.BelowNormal;
thread.Start();
}
catch {}
} // CheckComponentsOnStartup method

protected override void OnStartupNextInstance(StartupNextInstanceEventArgs e) {
/*
Util.ActivatePid(Process.GetCurrentProcess().Id);
Microsoft.VisualBasic.Interaction.AppActivate(App.Frame.Text);
App.Frame.Activate();
Util.ActivateTitle(App.Frame.Text);
*/
//COM.ActivateTitle(App.Frame.Text);
//System.Threading.Thread.Sleep(1000);

/*
object oAutoIt = COM.CreateObject("AutoItX3.Control");
object[] aParams = {"WinTitleMatchMode", 4};
COM.CallMethod(oAutoIt, "AutoItSetOption", aParams);
string sParam = "handle=" + App.Frame.TopLevelControl.Handle.ToString();
COM.CallMethod(oAutoIt, "WinActivate", sParam);
Win32.SetForegroundWindow(App.Frame.TopLevelControl.Handle);
*/

//Process.Start(Path.Combine(App.ProgramDir, "ForceWin.exe"), App.Frame.TopLevelControl.Handle.ToString());
Win32.ForceWindow(App.Frame.TopLevelControl.Handle);

if (e.CommandLine.Count == 0) return;
string sFile = Util.Unquote(e.CommandLine[0]);
string sLine = "";
string sColumn = "";
if (e.CommandLine.Count > 1) sLine = e.CommandLine[1];
if (e.CommandLine.Count > 2) sColumn = e.CommandLine[2];
if (sFile != null && File.Exists(sFile)) App.Frame.OpenOrActivateWindow(sFile, App.Frame.GetViewLevel(sFile), sLine, sColumn);
} // OnStartUpNextInstance handler

public static bool InitJFW() {
string sDir = Win32.GetJFWDir();
if (sDir.Length == 0) return false;

string sPath = Environment.GetEnvironmentVariable("PATH");
sDir += ";";
if (!sPath.ToLower().Contains(sDir.ToLower())) {
sPath = sDir + sPath;
Environment.SetEnvironmentVariable("PATH", sPath);
}
return true;
} // InitJFW method

public static bool InitNetSdk() {
// Does not work
// string sDir = Win32.GetNetRuntimeDir();
// string sDir = Win32.GetNetSdkDir();
string sDir = RuntimeEnvironment.GetRuntimeDirectory();
// Dialog.Show("RuntimeEnvironment", RuntimeEnvironment.GetSystemVersion() + "\r\n" + RuntimeEnvironment.GetRuntimeDirectory() + "\r\n" + RuntimeEnvironment.SystemConfigurationFile);
// Dialog.Show(sDir);

if (sDir.Length == 0) return false;
if (sDir.EndsWith(@"\")) sDir = sDir.Substring(0, sDir.Length - 2);

string sPath = Environment.GetEnvironmentVariable("PATH");
sDir += ";";
if (!sPath.ToLower().Contains(sDir.ToLower())) {
sPath = sDir + sPath;
Environment.SetEnvironmentVariable("PATH", sPath);
// Clipboard.SetText(Environment.GetEnvironmentVariable("PATH"));
}
return true;
} // InitNetSdk method

public static string GetAppName() {
// NOTE: this name drives the config folder, the .ini/.tmp filenames (App.DataDir,
// GetIniFile, GetTempFile) and the Help .htm lookup. It is deliberately kept as
// "EdSharp" (NOT the EdSharpNG product/title used for the UI, About and installer)
// so existing user settings, the shipped EdSharp.ini and EdSharp.htm keep working
// after the EdSharpNG rebrand. Change only if you also rename those data files.
return "EdSharp";
} // GetAppName method

public static string GetProgramDir() {
//string sApp = System.Reflection.Assembly.GetExecutingAssembly().Location;
//string sApp = Application.ExecutablePath;
//string sReturn = Path.GetDirectoryName(sApp);
string sReturn = Application.StartupPath;
return sReturn;
} // GetProgramDir method

public static string GetDataDir() {
string sName = GetAppName();
//string sDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
//string sDir = Application.UserAppDataPath;
//string sDir = Application.LocalUserAppDataPath
string sDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
string sReturn = Path.Combine(sDir, sName);
if (!Directory.Exists(sReturn)) Directory.CreateDirectory(sReturn);
return sReturn;
} // GetDataDir method

public static string GetTempFile() {
string sName = GetAppName() + ".tmp";
string sDir = GetDataDir();
string sReturn = Path.Combine(sDir, sName);
App.TempFiles.Add(sReturn);
return sReturn;
} // GetTempFile method

public static string GetIniFile() {
string sName = GetAppName() + ".ini";
string sDir = GetDataDir();
string sReturn = Path.Combine(sDir, sName);
return sReturn;
} // GetIniFile method

public static string GetDefaultIniFile() {
string sName = GetAppName() + ".ini";
string sDir = GetProgramDir();
string sReturn = Path.Combine(sDir, sName);
return sReturn;
} // GetDefaultIniFile method

public static string ReadData(string sKey, string sDefault) {
string sSection = "Data";
return ReadValue(sSection, sKey, sDefault);
} // ReadData method

public static bool WriteData(string sKey, string sValue) {
string sSection = "Data";
//return WriteValue(sSection, sKey, sValue);
return Ini.WriteQuote(App.IniFile, sSection, sKey, sValue);
} // WriteData method

public static string ReadDefaultOption(string sKey, string sDefault) {
string sSection = "Options";
return Ini.ReadValue(App.DefaultIniFile, sSection, sKey, sDefault);
} // ReadDefaultOption method

public static string ReadOption(string sKey, string sDefault) {
string sSection = "Options";
return ReadValue(sSection, sKey, sDefault);
} // ReadOption method

public static string ReadValue(string sSection, string sKey, string sDefault) {
return Ini.ReadValue(App.IniFile, sSection, sKey, sDefault);
} // ReadValue method

public static bool WriteOption(string sKey, string sValue) {
string sSection = "Options";
return WriteValue(sSection, sKey, sValue);
} // WriteOption method

public static bool WriteValue(string sSection, string sKey, string sValue) {
// return Ini.WriteValue(App.IniFile, sSection, sKey, sValue);
return Ini.WriteQuote(App.IniFile, sSection, sKey, sValue);
} // WriteValue method

public static bool DeleteKey(string sSection, string sKey) {
return Ini.DeleteKey(App.IniFile, sSection, sKey);
} // DeleteKey method

public static bool DeleteSection(string sSection) {
return Ini.DeleteSection(App.IniFile, sSection);
} // DeleteSection method

public static string[] ReadDefaultOptions() {
string sSection = "Options";
return ReadDefaultSectionKeys(sSection);
} // ReadOptions method

public static string[] ReadDefaultSectionKeys(string sSection) {
bool bIncludeComments = false;
return ReadDefaultSectionKeys(sSection, bIncludeComments);
} // ReadDefaultSectionKeys method

public static string[] ReadDefaultSectionKeys(string sSection, bool bIncludeComments) {
return Ini.ReadSectionKeys(App.DefaultIniFile, sSection, bIncludeComments);
} // ReadDefaultSectionKeys method

public static string[] ReadSectionKeys(string sSection) {
return Ini.ReadSectionKeys(App.IniFile, sSection);
} // ReadSectionKeys method

public static string[] ReadSections() {
return Ini.ReadSections(App.IniFile);
} // ReadSections method

public static void SetConfigurationValues() {
string[] aSections = Ini.ReadSections(App.DefaultIniFile);
foreach (string sSection in aSections) {
string[] aCommands = Ini.ReadSectionKeys(App.DefaultIniFile, sSection, false);
string[] aKeys = new string[aCommands.Length];
for (int i = 0; i < aCommands.Length; i++) {
string sCommand = aCommands[i];
string sKey = Ini.ReadValue(App.DefaultIniFile, sSection, sCommand, "");
sKey = Ini.ReadValue(App.IniFile, sSection, sCommand, sKey);
aKeys[i] = sKey;
}

//Ini.DeleteSection(App.IniFile, sSection);
for (int i = 0; i < aCommands.Length; i++) {
string sCommand = aCommands[i];
string sKey = aKeys[i];
//if (sSection == "Import" || sSection == "Export") Ini.WriteValue(App.IniFile, sSection, sCommand, sKey);
//else Ini.WriteQuote(App.IniFile, sSection, sCommand, sKey);
Ini.WriteQuote(App.IniFile, sSection, sCommand, sKey);
}
}
} // SetConfigurationValues method

// DOMYSLNE ROZSZERZENIE NOWEGO PLIKU: md, nie rtf.
//
// Zmierzone 01.09.2026: EdSharp.ini z paczki ma ExtensionDefault="rtf", a to
// ta wartosc trafia do SaveFileDialog.DefaultExt, czyli nowy dokument bez
// wpisanego rozszerzenia zapisywal sie jako .rtf.  Skutek dla uzytkownika byl
// szerszy niz sama nazwa pliku: caly Markdown (spis tresci, przypisy,
// komentarze, listy, wstawianie odsylacza) ma bramke na plik .md, wiec w takim
// dokumencie ODMAWIAL dzialania.
//
// Migracja jest JEDNORAZOWA i tylko z wartosci "rtf" (znacznik
// ExtensionDefaultMigrated w sekcji Data).  Kto swiadomie ustawil wlasne
// rozszerzenie - cs, py, cokolwiek - zostaje przy swoim; kto raz wybral rtf
// po tej migracji, tez zostaje przy swoim, bo znacznik jest juz zapisany.
// Bez znacznika przepisywalibysmy uzytkownikowi ustawienie przy KAZDYM
// starcie programu, czyli odbieralibysmy mu wybor zamiast poprawiac domyslna
// wartosc.
public static void MigrateDefaultExtensionToMarkdown() {
try {
if (ReadData("ExtensionDefaultMigrated", "").Trim().Length > 0) return;
string sCurrent = ReadOption("ExtensionDefault", "").Trim().Trim('.');
if (Util.Equiv(sCurrent, "rtf")) WriteOption("ExtensionDefault", "md");
WriteData("ExtensionDefaultMigrated", "Y");
}
catch {}
} // MigrateDefaultExtensionToMarkdown method

// PO USUNIECIU PRZELACZNIKA DODATKOWEJ MOWY: klucz ExtraSpeech schodzi z pliku
// ustawien, ale segment wyciszenia zmian wciecia ZOSTAJE.
//
// Kasperczak kazal usunac komende Extra Speech Toggle calkiem (03.09.2026:
// "Tak.  Usunac.  To bylo glownie pod Jaws").  Sam wczesniej pytalem, czy
// zostawic ja w menu, bo stan "wylaczone" zapisuje sie na dysku, a przelacznik
// byl jedyna droga powrotu.  Skoro komenda znika, to znika tez wpis: kto ma na
// dysku "N", ten po aktualizacji odzyskuje dodatkowe komunikaty mowy.
//
// PULAPKA, KTORA ROZSTRZYGA KSZTALT TEJ METODY: ten JEDEN klucz niesie DWA
// ustawienia.  Wiersz 206 czyta z niego takze IndentChange - myslnik w wartosci
// wycisza oglaszanie zmian wciecia.  Slepe DeleteKey odebraloby komus TO
// ustawienie, o ktorym nikt nie mowil.  Dlatego: gdy w wartosci jest myslnik,
// klucz zostaje z sama wartoscia "-" (wciecia nadal ciche, mowa wraca do
// domyslnego "Y"); gdy myslnika nie ma, klucz kasujemy.
//
// Znacznik ExtraSpeechDropped w sekcji Data pilnuje, zeby to bylo JEDNORAZOWE:
// bez niego kazdy start programu przepisywalby plik ustawien bez potrzeby.
public static void ClearExtraSpeechOption() {
try {
if (ReadData("ExtraSpeechDropped", "").Trim().Length > 0) return;
string sValue = ReadOption("E&xtraSpeech", "");
if (sValue.Length > 0) {
if (sValue.Contains("-")) WriteOption("E&xtraSpeech", "-");
else DeleteKey("Options", "E&xtraSpeech");
}
WriteData("ExtraSpeechDropped", "Y");
}
catch {}
} // ClearExtraSpeechOption method

// ZAKLADKI ZWYKLE WYPROWADZONE Z SEKCJI FAVORITES DO WLASNEJ SEKCJI BOOKMARKS.
//
// Jego decyzja z 01.09.2026 10:42, doslownie: "Rozdzielic.  Zakladki a ulubione
// pliki nie sa powiazane w jedna i druga strone.  To osobne sprawy."  Do 5.0.63
// obie rzeczy siedzialy w JEDNEJ sekcji Favorites, wiec sama zakladka czynila
// plik ulubionym, a zdjecie pliku z ulubionych czyscilo jego zakladki.
//
// FORMAT WPISU SIE NIE ZMIENIA: nadal "offset|offset|...|G/M|W/U", bo segmenty
// G/M (guard) i W/U (zawijanie) sa wlasnosciami OTWARCIA pliku i zostaja przy
// ulubionych, a do sekcji Bookmarks przenosimy same liczby.  Dzieki temu
// odczyty guard i zawijania (ApplyGuard, ApplyWrap, SaveGuardFlag) pracuja jak
// dotad na Favorites i nie trzeba ich ruszac.
//
// Migracja jest JEDNORAZOWA (znacznik BookmarksSplit w sekcji Data) i tylko
// PRZEPISUJE, nigdy nie kasuje: plik, ktory user swiadomie dodal do ulubionych,
// zostaje ulubionym, a jego zakladki trafiaja do nowej sekcji.  Wpis w
// Favorites zostawiamy WTEDY i TYLKO WTEDY, gdy niesie cos poza liczbami
// (segment G/M/W/U albo zastepcze "-1"), bo taki wpis znaczy "to jest
// ulubiony".  Wpis skladajacy sie z samych zakladek to plik, ktory nigdy nie
// byl ulubionym swiadomie - tylko z powodu zrosniecia obu list - i taki klucz
// z Favorites schodzi.
public static void MigrateBookmarksOutOfFavorites() {
try {
if (ReadData("BookmarksSplit", "").Trim().Length > 0) return;
string[] aFav = ReadSectionKeys("Favorites");
if (aFav != null) {
foreach (string sKey in aFav) {
if (sKey == null || sKey.Trim().Length == 0) continue;
string sValue = ReadValue("Favorites", sKey, "");
if (sValue == null || sValue.Length == 0) continue;

HomerList hlMarks = new HomerList(sValue);
hlMarks.KeepLike(@"^\d+$");
hlMarks.Remove("-1");
HomerList hlRest = new HomerList(sValue);
for (int i = hlRest.Count - 1; i >= 0; i--) {
string sSeg = (hlRest[i] ?? "").Trim();
if (sSeg.Length == 0) { hlRest.RemoveAt(i); continue; }
if (sSeg == "-1") continue;
int iTmp;
if (Int32.TryParse(sSeg, out iTmp)) hlRest.RemoveAt(i);
}

if (hlMarks.Count > 0) WriteValue("Bookmarks", sKey, hlMarks.Segments);
if (hlRest.Count > 0) WriteValue("Favorites", sKey, hlRest.Segments);
else if (hlMarks.Count > 0) DeleteKey("Favorites", sKey);
}
}
WriteData("BookmarksSplit", "Y");
}
catch {}
} // MigrateBookmarksOutOfFavorites method

} // App class

public class MdiChild : Form {
public HomerRichTextBox RTB;
public Encoding YieldEncoding = null;
public bool IsUnixLineBreak = false;

// CZY DOKUMENT ZOSTAL WCZYTANY JAKO RICH TEXT, a nie samo rozszerzenie pliku.
//
// Zmierzone 02.09.2026 (testy/harness_rtf.cs na zywej kontrolce z binarki):
// zapis rozstrzygany po ROZSZERZENIU niszczyl plik, gdy dokument byl zwyklym
// tekstem, a plik nazywal sie .rtf.  SaveFile(RichText) na takiej tresci
// ESCAPUJE kazdy znak sterujacy zrodla, wiec znacznik poczatku dokumentu
// laduje w pliku z PODWOJONYM ukosnikiem wstecznym - plik po ponownym otwarciu
// pokazuje znaczniki zamiast dokumentu.
// Ta droga jest zwyczajna, nie egzotyczna: Control+O na pliku .rtf pokazuje
// liste wariantow otwarcia, a jeden z nich to wlasnie zwykly tekst.
// Rozszerzenie mowi, jak plik sie NAZYWA; to pole mowi, czym dokument JEST.
public bool IsRichTextDocument = false;

// Rodzaj koncow wiersza, ktore mial PLIK NA DYSKU w momencie otwarcia:
// "Windows", "Unix", "Macintosh", "mixed" albo pusto.  Musi byc zapamietany
// tutaj, bo kontrolka edycyjna normalizuje kazdy rodzaj do samego LF (pomiar
// 28.08.2026 na zywej kontrolce), wiec po wczytaniu nie da sie tego odtworzyc
// z tekstu w pamieci.  Czytane przez Alt+Z.
public string FileLineBreakKind = "";
public int AppendFromClipboard = 0;
public IntPtr NextClipboardViewer = (IntPtr) 0;
public int LastTickCount = 0;
public string LastClipboardText = "";
// Markdown review cache (shared by Markdown copy/navigation)
	public bool MarkdownReviewMode = false;
	public int TextRevision = 0;
	public int MarkdownReviewCacheRevision = -1;
	public int MarkdownReviewCacheLength = 0;
	public List<int[]> MarkdownReviewInlineLinks = null;
	public List<int> MarkdownReviewHeadings = null;
	public List<int> MarkdownReviewLists = null;
	public List<int> MarkdownReviewListItems = null;
	public List<int> MarkdownReviewLinks = null;
	public List<int> MarkdownReviewTables = null;
	// Markdown review (preview) mode: a read-only rendered view layered over
	// the editor, toggled with Escape on .md files.
	public bool MarkdownReviewOldGuard = false;
	public MarkdownReviewTextBox MarkdownReviewView = null;
	public bool MarkdownReviewSync = false;
	public EventHandler MarkdownReviewRtbSelectionHandler = null;
	public int[] MarkdownReviewSourceToView = null;
	public int[] MarkdownReviewViewToSource = null;
	public int MarkdownReviewLastHeadingRowStart = -1;
	public int MarkdownReviewLastLinkRowStart = -1;
	public bool MarkdownReviewAnnounceHeading = false;
	public int MarkdownReviewViewRevision = -1;
	// When true, the preview is "detached": caret changes in the preview are
	// NOT synced back to the editor, and on leaving the preview the editor
	// caret is restored to where it was when the detached preview was entered.
	// Toggled with Shift+Escape (plain Escape uses the synced mode).
	public bool MarkdownReviewNoSync = false;
	public int MarkdownReviewSavedEditPos = -1;
	public int MarkdownReviewSavedEditLength = 0;
	// Document Navigation (F6): the character offset of the heading that was
	// selected in the tree when it was last closed, so reopening the tree
	// returns to the reader's place. Per window, not persisted to disk -- it is
	// reading position within one editing session, like a scroll position.
	// -1 means "never opened in this window".
	public int DocumentNavigationLastOffset = -1;
private string sFile = "";
public string File {
get {
return sFile;
}
set {
sFile = value;
}
} // File property

public DateTime FileTime;
public bool FileTimeChecked = false;
// Order in which this window was opened, used by the Control+digit window
// navigation: Control+1 goes to the first window opened, Control+2 to the
// second, and so on (Kasperczak, Telegram 14.08.2026 19:23).  The framework
// MdiChildren array is ordered by activation, not by opening, so the order
// has to be remembered explicitly.
private static int iNextOpenSequence = 0;
public int OpenSequence = 0;
public MdiChild(MdiFrame frame) {
string sTitle = frame.GetNoNameTitle();
new MdiChild(frame, sTitle);
} // MdiChild constructor

public MdiChild(MdiFrame frame, string sTitle) {
this.OpenSequence = ++iNextOpenSequence;
this.SuspendLayout();
this.MdiParent = frame;
HomerRichTextBox rtb = new HomerRichTextBox();
rtb.GotFocus += CheckFileTime;
rtb.AccessibleRole = AccessibleRole.Text;
rtb.AutoWordSelection = false;
rtb.Dock = DockStyle.Fill;
rtb.Multiline = true;
rtb.TextChanged += delegate(object o, EventArgs e) {try {this.TextRevision++;} catch {}};

string sFont = App.ReadOption("FontDefault", "");
if (sFont.Length > 0) {
string[] a = sFont.Split(',');
List<string> list = new List<string>(a);
int iCount = list.Count;
string sColor = list[iCount - 1];
try {
sColor = sColor.Split('=')[1];
rtb.ForeColor = Util.String2Color(sColor);
}
catch {}

list.RemoveAt(iCount - 1);
a = list.ToArray();
sFont = String.Join(",", a);
try {
//sFont = "Arial Unicode MS";
rtb.Font = Util.String2Font(sFont);
}
catch {}
}

string s = App.ReadOption("WordWrap", "Y").Trim().ToUpper();
if (s == "N" || s == "NO") rtb.SetWrap(false);
else rtb.SetWrap(true);
//rtb.ScrollBars = RichTextBoxScrollBars.Vertical;
rtb.ScrollBars = RichTextBoxScrollBars.Vertical | RichTextBoxScrollBars.Horizontal;
rtb.AcceptsTab = true;
rtb.FindText = "";
rtb.JumpLine = "";
rtb.GoPercent = "";
rtb.SearchTopic = "";
rtb.SelectionChanged += App.Frame.SetStatusAddress;
this.Controls.Add(rtb);
this.RTB = rtb;
//this.File = frame.GetNoNameTitle();
this.File = sTitle;
this.Text = System.IO.Path.GetFileName(this.File);
this.StartPosition = FormStartPosition.CenterParent;
this.AutoSize = true;
this.ResumeLayout();
this.KeyPreview = true;
this.Activated += delegate(object o, EventArgs e) {
frame.SetStatusAddress(this, null);
//this.WindowState = FormWindowState.Maximized;
//Win32.SetForegroundWindow(App.Frame.Handle);
//Win32.SetForegroundWindow(this.Handle);
//COM.ActivateTitle("EdSharp");
//int iPid = Process.GetCurrentProcess().Id;
//if (iPid > 0) Util.ActivatePid(iPid);

//Win32.ForceWindow(App.Frame.TopLevelControl.Handle);
};

string sText, sResult = "";
this.Shown += delegate(object o, EventArgs e) {
this.WindowState = FormWindowState.Maximized;
//Win32.ForceWindow(App.Frame.TopLevelControl.Handle);

sFile = this.File;
if (!sFile.Contains(@"\")) return;

// Pozycja zakladki czytana z sekcji Bookmarks (rozdzielenie zakladek od
// ulubionych, 5.0.64) - wczesniej z Favorites, wiec plik nieulubiony nie
// wracal na zakladke.
sText = App.ReadValue("Bookmarks", sFile, "");
try {
string[] a = sText.Split('|');
sText = a[0];
rtb.Index = Int32.Parse(sText);
return;
}
catch {}

sText = App.ReadValue("Recent", sFile, "");
if (sText.Length == 0) return;

rtb = this.RTB;
HomerList hl = new HomerList(sText);
hl.KeepLike(@"^\d+$");
// hl.Remove("-1");
if (hl.Count == 0) {
return;
}

sResult = hl[0];
rtb.Index = Int32.Parse(sResult);
App.Frame.AddMessage("Previous percent " + rtb.Percent);
// Util.Say(rtb.RowText);
}; // Shown

this.Closing += delegate(object o, CancelEventArgs e) {
sFile = this.File;
if (!sFile.Contains(@"\")) return;
rtb = this.RTB;
int iIndex = rtb.Index;
if (iIndex == 0) return;

sText = App.ReadValue("Recent", sFile, "");
HomerList hl = new HomerList(sText);
hl.KeepLike(@"\d+");
hl.Remove("-1");
DateTime dt = DateTime.Now;
string sTime = dt.ToString("u");
sTime = sTime.Substring(0, sTime.Length - 1);
sText = sTime + "|" + iIndex + "|" + (App.Frame.GetUserGuard(this) ? "G" : "M") + "|" + (string) Util.If(rtb.WordWrap, "W", "U");
// hl.AddUniqueRange(sText);
// sText = hl.Segments;
App.WriteValue("Recent", sFile, sText);
}; // Closing

this.FileTime = System.IO.File.GetLastWriteTime(this.File);
this.Show();
} // child constructor

public void CheckFileTime(object sender, EventArgs e) {
if (App.Frame.KeyDescriber) {
App.Frame.SetMessage("No Key Describer");
App.Frame.KeyDescriber = false;
}

string sFile = this.File;
bool b = System.IO.File.Exists(App.IndentModeFile);
if (b && !this.RTB.IndentMode) System.IO.File.Delete(App.IndentModeFile);
else if (!b && this.RTB.IndentMode) System.IO.File.Create(App.IndentModeFile).Close();
if (this.FileTimeChecked || sFile.IndexOf(@"\") == -1 || !System.IO.File.Exists(sFile)) return;

DateTime dt = System.IO.File.GetLastWriteTime(sFile);
//if (this.FileTime >= dt || Util.File2String(sFile).Length == 0) return;
if (this.FileTime >= dt) return;
this.FileTimeChecked = true;
switch (Dialog.Confirm("Confirm", this.Text + " on disk is newer than the version opened in this window.  Open Again?", "Y")) {
case "Y" :
int iIndex = this.RTB.Index;
this.LoadTextOrRtfFile(sFile);
this.RTB.Index = iIndex;
break;
case "N":
break;
default :
this.FileTimeChecked = false;
return;
}
} // CheckFileTime handler

protected override void WndProc(ref Message m) {
base.WndProc(ref m);

const int WM_CHANGECBCHAIN = 0x30D;
const int WM_DRAWCLIPBOARD = 0x308;
//if (m.Msg == 776) {
switch (m.Msg) {
case WM_DRAWCLIPBOARD :
if ((int) this.NextClipboardViewer > 0) Win32.SendMessage(this.NextClipboardViewer, m.Msg, (int) m.LParam, (int) m.WParam);

if (this.AppendFromClipboard == -1) {
this.AppendFromClipboard = 1;
}
else if (this.AppendFromClipboard == 1) {
string sClipboard = Util.GetClipboardText();
// Do not append a copy made in this same collecting window, which would copy the
// document back into itself.  this.ContainsFocus is true only when the input
// focus is in this document, so the copy originated here; a copy made in another
// EdSharp window or in another application is still collected.
if (this.ContainsFocus) sClipboard = "";
//if (sClipboard == this.LastClipboardText && ((Environment.TickCount - this.LastTickCount) < 100)) sClipboard = "";
if (sClipboard == this.LastClipboardText) sClipboard = "";
if (sClipboard.Length > 0) {
this.LastTickCount = Environment.TickCount;
this.LastClipboardText = sClipboard;
Console.Beep();

HomerRichTextBox rtb = this.RTB;
string sText = rtb.Text;
sText = sText.TrimEnd(new char[] {'\n'});
int iLength = sText.Length;
if (iLength >0) sText += "\f\n";
//if (iLength > 0 && sText.Substring(iLength - 1) != "\n") sText += "\n";
sClipboard = sClipboard.TrimEnd(new char[] {'\n'});
sText += sClipboard;
rtb.Text = sText;
rtb.Index = rtb.Text.Length - 1;
} // sClipboard.Length
} // this.AppendFromClipboard
break;
case WM_CHANGECBCHAIN :
IntPtr hNextClipboardViewer = m.WParam;
if (this.NextClipboardViewer == hNextClipboardViewer) this.NextClipboardViewer = m.LParam;
else if ((int) this.NextClipboardViewer > 0) Win32.SendMessage(hNextClipboardViewer, m.Msg, (int) m.LParam, (int) m.WParam);
break;
} // switch msg
} // WndProc event handler

public Encoding GetYieldEncoding() {
// The YieldEncoding option is the explicit encoding the user wants for both
// reading and writing a document. Blank returns null, which lets the open
// path auto-detect and the save path default to UTF-8 with BOM (utf8b).
// Friendly names are recognized in addition to a numeric code page or any
// .NET encoding name.
Encoding en = null;
string sEncoding = App.ReadOption("YieldEncoding", "").Trim();
string sKey = sEncoding.Replace("-", "").Replace("_", "").ToLower();
if (sKey == "utf8n") {
en = new UTF8Encoding(false);
this.IsUnixLineBreak = true;
}
else if (sKey == "utf8b" || sKey == "utf8") en = new UTF8Encoding(true);
else if (sKey == "utf16" || sKey == "utf16le" || sKey == "unicode") en = Encoding.Unicode;
else if (sKey == "utf16be") en = Encoding.BigEndianUnicode;
else if (sKey == "ansi" || sKey == "default") en = Encoding.Default;
// STARE POLSKIE KODOWANIA POD NAZWAMI, KTORE COS ZNACZA (zadanie 9, 12.09.2026).
// Numer strony kodowej dziala dalej (galaz nizej), ale nikt nie pamieta, ze
// polski DOS to 852 - a "mazovia", "latin2" i "cp1250" pamieta kazdy, kto ma
// takie pliki.  Mazovia idzie przez wlasna klase, bo .NET tej strony nie zna;
// pozostale dwie sa w systemie.
else if (sKey == "mazovia" || sKey == "cp667" || sKey == "667" || sKey == "maz") en = new MazoviaEncoding();
else if (sKey == "latin2" || sKey == "latinii" || sKey == "cp852" || sKey == "dos852" || sKey == "852") en = Encoding.GetEncoding(852);
else if (sKey == "cp1250" || sKey == "windows1250" || sKey == "win1250" || sKey == "1250") en = Encoding.GetEncoding(1250);
else if (sEncoding.Length > 0 ) {
try {
if (Util.IsNumeric(sEncoding)) en = Encoding.GetEncoding(Int32.Parse(sEncoding));
else en = Encoding.GetEncoding(sEncoding);
}
catch (Exception ex) {
Dialog.Show("Error", ex.Message);
en = new UTF8Encoding(true);
}
}
return en;
} // GetYieldEncoding method

public void LoadTextOrRtfFile(string sFile) {
bool bLiteral = false;
LoadTextOrRtfFile(sFile, bLiteral);
} // LoadTextOrRtfFile method

public void LoadTextOrRtfFile(string sFile, bool bLiteral) {
this.FileTimeChecked = false;
this.FileTime = System.IO.File.GetLastWriteTime(sFile);
// Kasperczak's request of 27 August 2026, and Jim's of 24 August in the
// author's own tree: the current directory used to follow only SAVES, so
// opening a file to read it left the next Open dialog pointing at wherever
// the last save happened, and reaching a sibling of the file just opened
// meant navigating the whole way again. Every load now brings the current
// directory along, so opening a file makes its folder the starting point
// for the next Control+O. Failure is ignored on purpose: a file on a
// vanished drive or a path the process may not enter must not stop the
// document from opening.
try {
if (sFile != null && sFile.IndexOf(@":\") > 0) Directory.SetCurrentDirectory(Path.GetDirectoryName(sFile));
}
catch (Exception) {}
try {
// Flaga rozstrzyga PROWENIENCJE dokumentu, nie nazwe pliku, i musi byc
// ustawiona na OBU galeziach: dokument wczytany raz jako rich text, raz jako
// tekst, trafia do zapisu ta sama droga i tylko ta wartosc je rozroznia.
if (!bLiteral && Path.GetExtension(sFile).ToLower() == ".rtf") {
this.RTB.LoadFile(sFile, RichTextBoxStreamType.RichText);
this.IsRichTextDocument = true;
}
//else this.RTB.LoadFile(sFile, RichTextBoxStreamType.UnicodePlainText);
else {
this.IsRichTextDocument = false;
Encoding en = GetYieldEncoding();
string sText = Util.File2String(sFile, ref en);
// Rodzaj koncow wiersza zapamietujemy TERAZ, z tekstu prosto z dysku.
// Przypisanie do kontrolki zamienia CRLF i CR na samo LF, wiec po tej
// linii informacja jest bezpowrotnie stracona (zmierzone).
this.FileLineBreakKind = Util.DetectLineBreakKind(sText);
this.RTB.Text = sText;
// Dialog.Show(sText.Length, this.RTB.TextLength);
if (sText.Length > 1 && this.RTB.TextLength == 1) {
en = Encoding.Unicode;
this.RTB.Text = Util.File2String(sFile, ref en);
}
this.YieldEncoding = en;
// PLIK CSV: PROPOZYCJA OTWARCIA JAKO TABELA (zadanie 10, 5.0.87).
//
// Nie otwieramy tabeli SAMI, bo plik CSV bywa tez po prostu tekstem do
// przeczytania albo do poprawienia recznie - decyzja nalezy do uzytkownika.
// Pytamy tylko wtedy, gdy tresc NAPRAWDE wyglada na tabele (rowne kolumny,
// co najmniej dwie), czyli gdy odpowiedz "tak" ma sens. Plik .csv, ktory w
// srodku jest zwyklym tekstem, otwiera sie po cichu jak tekst.
try {
string sExt = Path.GetExtension(sFile).ToLower();
if ((sExt == ".csv" || sExt == ".tsv") && !bLiteral) {
char cSepDetect;
int iColsDetect, iRowsDetect;
if (EdSharp.Csv.WygladaNaTabele(sText, out cSepDetect, out iColsDetect, out iRowsDetect)) {
string sQ = "This looks like a table: " + iRowsDetect + " rows and " + iColsDetect + " columns.\n\nOpen it as a table you can read and edit column by column?";
if (Dialog.Confirm("CSV Table", sQ, "Y") == "Y") {
// Okno tabeli otwieramy PO tym, jak dokument stanie sie widoczny -
// inaczej modalne okno wstaje nad niegotowa jeszcze ramka.
string sCsvPath = sFile;
System.Threading.Tasks.Task.Delay(150).ContinueWith(delegate(System.Threading.Tasks.Task t) {
try {
if (App.Frame != null && !App.Frame.IsDisposed)
App.Frame.BeginInvoke((MethodInvoker) delegate() { App.Frame.EditCsvAsTable(sCsvPath); });
}
catch {}
});
}
}
}
}
catch {}
// POWIEDZ, GDY PLIK BYL W STARYM POLSKIM KODOWANIU (zadanie 9, 12.09.2026).
// Bez tego konwersja dzieje sie po cichu: uzytkownik widzi poprawne polskie
// litery, ale nie wie, ze plik na dysku jest inny niz to, co ma na ekranie -
// dowiaduje sie dopiero, gdy zapisze i ktos otworzy plik starym programem.
// Komunikat idzie w pasek wiadomosci, nie w okno - okno zabiera fokus.
if (en != null) {
int iCp = en.CodePage;
string sOld = null;
if (iCp == 667) sOld = "Mazovia";
else if (iCp == 852) sOld = "Latin II (CP852)";
else if (iCp == 1250) sOld = "Windows-1250";
if (sOld != null)
App.Frame.AddMessage("Opened as " + sOld + "; will be saved as UTF-8.");
}
}
//else this.RTB.Text = Util.OldFile2String(sFile);
//else this.RTB.Text = System.IO.File.ReadAllText(sFile, System.Text.Encoding.UTF8);
//else this.RTB.Text = System.IO.File.ReadAllText(sFile, System.Text.Encoding.Default);
//else this.RTB.Text = System.IO.File.ReadAllText(sFile, System.Text.Encoding.GetEncoding(1252));
this.RTB.Modified = false;
this.Text = Path.GetFileName(sFile);
this.File = sFile;
}
catch {
App.Frame.AddMessage("Cannot open file!  Opening temporary copy.");
if (System.IO.File.Exists(App.TempFile)) System.IO.File.Delete(App.TempFile);
System.IO.File.Copy(sFile, App.TempFile);
App.Frame.OpenOrActivateWindow(App.TempFile);
}
//Dialog.Show(this.File);
// Stop double bookmark at message
// App.Frame.ApplyFileOptions(sFile);
} // LoadTextFile method

public void SaveTextOrRtfFile(string sFile) {
if (System.IO.File.Exists(sFile)) {
string sKeepBackup = App.ReadOption("KeepBackup", "N").Trim().ToLower();
if (sKeepBackup == "y" || sKeepBackup == "yes") {
string sBak = sFile + ".bak";
if (System.IO.File.Exists(sBak)) System.IO.File.Delete(sBak);
System.IO.File.Copy(sFile, sBak);
}
}

// ZAPIS RICH TEXT TYLKO DLA DOKUMENTU, KTORY RICH TEXTEM JEST.
//
// Dawny warunek patrzyl WYLACZNIE na rozszerzenie, wiec dokument tekstowy w
// pliku .rtf przechodzil przez SaveFile(RichText) i tracil tresc: znaczniki
// zrodla zostawaly zaescapowane, czyli plik przestawal byc dokumentem, a
// stawal sie opisem dokumentu.  Zmierzone harnessem na zywej kontrolce
// (testy/harness_rtf.cs, punkty 1 i 3).  Dokument wczytany jako rich text
// zapisuje sie jak dotad - to sprawdza punkt 4 tego samego harnessu, wiec
// poprawka jest ROZLACZNA: nie zabiera niczego plikom naprawde bogatym.
if (Path.GetExtension(sFile).ToLower() == ".rtf" && this.IsRichTextDocument) this.RTB.SaveFile(sFile, RichTextBoxStreamType.RichText);
else if (Path.GetExtension(sFile).ToLower() == ".rtf") {
// NAZWA PLIKU MOWI JEDNO, ZAWARTOSC DRUGIE - to trzeba POWIEDZIEC.
// Widzacy dostrzeze w oknie, ze tresc jest zwyklym tekstem; niewidomy nie ma
// z czego tego wyczytac, a plik .rtf zapisany tekstem otworzy sie w Wordzie
// inaczej, niz sie spodziewa.  Komunikat idzie PRZED zapisem, zeby nie
// przepadl pod ogloszeniem nazwy pliku w tytule okna.
App.Frame.AddMessage("Saving as plain text, not rich text");
Encoding enPlain = this.YieldEncoding;
if (enPlain == null) enPlain = GetYieldEncoding();
if (enPlain == null) enPlain = new UTF8Encoding(true);
string sPlain = this.RTB.Text;
if (!this.IsUnixLineBreak) sPlain = Util.Convert2WinLineBreak(sPlain);
Util.String2File(sPlain, sFile, ref enPlain);
}
//else if (Util.IsUnicode(this.RTB.Text)) Util.String2File(this.RTB.Text, sFile);
//else this.RTB.SaveFile(sFile, RichTextBoxStreamType.PlainText);
else {
Encoding en = this.YieldEncoding;
if (en == null) en = GetYieldEncoding();
if (en == null) en = new UTF8Encoding(true);
// STARA STRONA KODOWA IDZIE NA UTF-8 (jego decyzja z 28.08.2026: "program
// powinien otwierac polskie literki zawsze konwertowac do UTF (...) czyli
// UTF-8").  Plik raz otwarty u nas przestaje byc pulapka na polskie litery.
// Jawny wybor uzytkownika w ustawieniach ma pierwszenstwo - GetYieldEncoding
// zwraca wtedy niepusta wartosc i tego nie ruszamy.
if (App.ReadOption("YieldEncoding", "").Trim().Length == 0) {
Encoding enSave = Util.GetSaveEncoding(en);
if (enSave != null && enSave.CodePage != en.CodePage) {
en = enSave;
this.YieldEncoding = en;
}
}
string sText = this.RTB.Text;
if (!this.IsUnixLineBreak) sText = Util.Convert2WinLineBreak(sText);
Util.String2File(sText, sFile, ref en);
}
this.RTB.Modified = false;
App.Frame.SetRecent(sFile);
this.Text = Path.GetFileName(sFile);
this.File = sFile;
this.FileTime = System.IO.File.GetLastWriteTime(sFile);
this.FileTimeChecked = false;
} // SaveTextOrRtfFile method

protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
return App.Frame.ProcessCmdKey_Helper(ref msg, keyData);
} // ProcessCmdKey handler

} // MdiChild class

public class MdiFrame : Form {
public string LastDescription = "";
public bool KeyDescriber = false;
public string KeyString = "";
public int KeyRepeat = 0;
public int KeyIndex = -1;

public MdiChild Child {
get {
MdiChild child = this.ActiveMdiChild as MdiChild;
if (child == null) {
Form[] children = this.MdiChildren;
int iLength = children.Length;
if (children.Length > 0) child = (MdiChild) children[iLength - 1];
}
return child;
}
set {
value.Activate();
}
} // Child property

public bool FindWithRegExp = false;
public bool bCommandComplete = true;
public static string CR = "\r";
public static string LF = "\n";
public static string LB = LF;
public static string LineBreak = Environment.NewLine;
public static string FF = "\f";
public static string SB = FF + LB;
public static string DD = "----------";
public static string SectionBreak = LB + DD + LB + SB;
public static string EOD = LB + DD + LB + "End of Document" + LB;

public static Dictionary<Keys, ToolStripMenuItem> hashKey = new Dictionary<Keys, ToolStripMenuItem>();
public MenuStrip menuMain;
public ToolStripMenuItem menuFile, menuFileNew, menuFileNewFromClipboard, menuFileOpen, menuFileOpenOtherFormat, menuFileOpenAgain, menuFileRecent, menuFileSetFavorite, menuFileClearFavorite, menuFileListFavorites, menuFileFind, menuFileSave, menuFileSaveAs, menuFileSaveCopy, menuFileExport, menuFileRename, menuFileProperties, menuFileMailBody, menuFileMailAttach, menuFilePrint, menuFileRun, menuFileCurrentWindows, menuFileClose, menuFileCloseAllButCurrentWindow, menuFileSlots, menuFileExit;
public ToolStripMenuItem menuEdit, menuEditSelectAll, menuEditUnselectAll, menuEditCopy, menuEditCopyAppend, menuEditCopyRichText, menuEditCut, menuEditCutAppend, menuEditPaste, menuEditPasteFile, menuEditUndo, menuEditRedo, menuEditStartSelection, menuEditCompleteSelection, menuEditReselect, menuEditCopyAll, menuEditSelectChunk, menuEditAppendFromClipboard, menuEditQuote, menuEditUnquote, menuEditUpperCase, menuEditLowerCase, menuEditProperCase, menuEditSwapCase, menuEditYieldEncoding, menuEditJoinLines, menuEditHardLineBreak, menuEditEnterNewLine, menuEditIndentNewLine, menuEditIndentNewLinePrior, menuEditIndent, menuEditOutdent, menuEditAlign, menuEditIndentMode, menuEditJustify, menuEditStyle, menuEditBaseline, menuEditSetSelectionFont;
public ToolStripMenuItem menuDelete, menuDeleteReplaceRegular, menuDeleteReplaceWithRegExp, menuDeleteHardLine, menuDeleteParagraph, menuDeleteLine, menuDeleteRight, menuDeleteLeft, menuDeleteDown, menuDeleteUp, menuDeleteFile, menuDeleteTrimBlanks;
public ToolStripMenuItem menuNavigate, menuNavigateForwardFind, menuNavigateReverseFind, menuNavigateForwardFindWithRegExp, menuNavigateReverseFindWithRegExp,  menuNavigateForwardFindAtCursor, menuNavigateReverseFindAtCursor, menuNavigateForwardFindAgain, menuNavigateReverseFindAgain, menuNavigateJumpToLine, menuNavigateJumpToLineAgain, menuNavigateGoToPercent, menuNavigateGoToPercentAgain, menuNavigateSetBookmark, menuNavigateClearBookmark, menuNavigateGoToBookmark, menuNavigateHomeCharacter, menuNavigateEndCharacter, menuNavigateStartTag, menuNavigateEndTag, menuNavigateRightBrace, menuNavigateLeftBrace, menuNavigateNextIndent, menuNavigatePriorIndent, menuNavigateNextChunk,  menuNavigatePriorChunk, menuNavigateNextSentence, menuNavigatePriorSentence, menuNavigateNextParagraph, menuNavigatePriorParagraph, menuNavigateNextSection, menuNavigatePriorSection, menuNavigateNextSectionSameLevel, menuNavigatePriorSectionSameLevel, menuNavigateGoToStartOfSelection, menuNavigateNextBookmark, menuNavigatePriorBookmark, menuNavigateSetNamedBookmark, menuNavigateNamedBookmarkList, menuNavigateDocumentNavigation, menuNavigateGoToContents, menuNavigateNextEmphasis, menuNavigatePriorEmphasis, menuNavigateNextList, menuNavigatePriorList, menuNavigateLinkList, menuNavigateNextLink, menuNavigatePriorLink;
public ToolStripMenuItem menuQuery, menuQueryAddress, menuQueryBraces, menuQueryIndent, menuQueryPath, menuQueryTopic, menuQueryYield, menuQueryStatus, menuQueryCompiler, menuQuerySelected, menuQueryChunk, menuQueryReadAll, menuQueryClipboard, menuQueryTime, menuQueryStyles, menuQueryFont;
public ToolStripMenuItem menuMisc, menuMiscSetDefaultFont, menuMiscConfigurationOptions, menuMiscManualOptions, menuMiscResetConfiguration, menuMiscGoToFolder, menuMiscGoToSpecialFolder, menuMiscWordWrap, menuMiscUnwrap, menuMiscExtraSpeechLog, menuMiscEnvironmentVariables, menuMiscSpellCheck, menuMiscSpellingWordMenu, menuMiscThesaurus, menuMiscLookupTerm, menuMiscTranslateLanguage, menuMiscGuardDocument, menuMiscPyBrace, menuMiscPyDent, menuMiscInferIndent, menuMiscRepeatLine, menuMiscSectionBreak, menuMiscPathToClipboard, menuMiscPathList, menuMiscInsertTime, menuMiscPreviewMarkdownBrowser, menuMiscTextCombine, menuMiscInsertTable, menuMiscCsvTable, menuMiscBulletList, menuMiscNumberedList, menuMiscInsertLink, menuMiscTableOfContents, menuMiscInsertFootnote, menuMiscGoToFootnote, menuMiscNextFootnote, menuMiscPriorFootnote, menuMiscFootnoteList, menuMiscExportFootnotes, menuMiscInsertComment, menuMiscNextComment, menuMiscPriorComment, menuMiscCommentList, menuMiscRegExpTool, menuMiscRunAtCursor, menuMiscSpecialCharacter, menuMiscEvaluateExpression, menuMiscReplaceTokens, menuMiscTransformFiles, menuMiscGoToEnvironment, menuMiscCompile, menuMiscPickCompiler, menuMiscPromptCommand, menuMiscReviewOutput, menuMiscSaveSnippet, menuMiscInvokeSnippet, menuMiscViewSnippet, menuMiscKeepUniqueItems, menuMiscNumberItems, menuMiscOrderItems, menuMiscReverseItems, menuMiscListDifferentItems, menuMiscQueryCommonItems, menuMiscExplorerFolder, menuMiscCommandPrompt, menuMiscWebDownload, menuMiscWebClientUtilities;
public ToolStripMenuItem menuWindow, menuWindowNext, menuWindowPrior, menuWindowArrangeIcons, menuWindowCascade, menuWindowTileHorizontal, menuWindowTileVertical;
public ToolStripMenuItem menuHelpCommandPalette;
public ToolStripMenuItem menuHelp, menuHelpAbout, menuHelpDocumentation, menuHelpTutorial, menuHelpHistoryOfChanges, menuHelpKeyDescriber, menuHelpHotKeySummary, menuHelpAlternateMenu, menuHelpContextMenu, menuHelpSendToMenu, menuHelpElevateVersion, menuHelpReinstall, menuHelpUpdateComponents, menuHelpReportProblem;
public StatusStrip statusBar;
public ToolStripStatusLabel lblStatus;

public MdiFrame() {
SectionBreak = Util.Literalize(App.ReadOption("SectionBreak", SectionBreak));
this.SuspendLayout();
this.IsMdiContainer = true;
menuMain = CreateMainMenu();
//menuMain.ShowItemToolTips = true;
menuMain.AccessibleRole = AccessibleRole.MenuBar;
menuFile = CreateMenu("&File");
menuFileNew = CreateMenuItem("&New", "Control+N", menuItem_Click, "frame speak");
menuFileNewFromClipboard = CreateMenuItem("New from Clipboard", "Control+Shift+N", menuItem_Click, "frame speak");
menuFileOpen = CreateMenuItem("&Open ...", "Control+O", menuItem_Click, "frame speak");
menuFileOpenOtherFormat = CreateMenuItem("Open Other Format ...", "Control+Shift+O", menuItem_Click, "frame speak");
menuFileOpenAgain = CreateMenuItem("Open Again", "Alt+O", menuItem_Click, "child speak");
menuFileRecent = CreateMenuItem("Recent Files ...", "Alt+R", menuItem_Click, "frame silent");
// ULUBIONE PRZESZLY NA LITERE L Z ALTEM (Kasperczak, 31.08.2026, ustalenie
// edsharpng-55): "Alt-l ulubione, alt-Shift-l dodaj do ulubionych i w sumie,
// jesli jestem w ulubionym pliku to jako przelacznik tez moze usuwac z
// ulubionych.  Oczywiscie usuwanie ulubionych z poziomu listy tez zostaje".
// Control+L i Control+Shift+L oddaja miejsce listom Markdown, bo ulubionych
// dodaje sie do listy raz, a listy pisze sie caly czas.
//
// JEDNA POZYCJA MENU ZAMIAST DWOCH: Set Favorite i Clear Favorite byly osobnymi
// komendami na osobnych klawiszach; teraz jest JEDEN przelacznik, ktory patrzy,
// czy plik JUZ jest na liscie.  Clear Favorite ZOSTAJE w menu bez skrotu, bo
// jest jedyna droga dla kogos, kto chce zdjac plik z listy nie ufajac
// przelacznikowi, a zdejmowanie pozycji z menu jest trudniejsze do cofniecia
// niz jej zostawienie.
//
// MOWA PODAJE SKUTEK, NIE NAZWE KOMENDY (jego zgloszenie 06.09.2026: "Alt-l
// Toggle favorite.  Nie potrzebnie, niech mowi Added favorites, Remove
// favorites").  Opcja "speak" kazala menuItem_Click wypowiedziec NAZWE pozycji
// menu, wiec po jednym nacisnieciu szly DWIE wypowiedzi: najpierw "Toggle
// Favorite", potem "Added to favorites".  Pierwsza z nich nie niesie zadnej
// informacji, bo nazwe klawisza uzytkownik zna, a przy przelaczniku wazny jest
// KIERUNEK zmiany.  Opcja "quiet" nie oddaje nazwy nigdzie - ani mowie, ani
// paskowi stanu - a mowi wylacznie AddMessage ze skutkiem.  Samo "silent" tu
// nie wystarczylo: nazwa zostawala na pasku i wracala w mowie razem z
// komunikatem (poprawione 11.09.2026).
menuFileSetFavorite = CreateMenuItem("Toggle Favorite", "Alt+Shift+L", menuItem_Click, "child quiet");
// Clear Favorite tak samo, plus komunikat, ktorego dotad NIE BYLO WCALE: ta
// komenda kasowala klucz w ciszy, wiec dla niewidomego brzmiala identycznie na
// pliku ulubionym i na pliku, ktorego na liscie nigdy nie bylo.
menuFileClearFavorite = CreateMenuItem("Clear Favorite", "", menuItem_Click, "child quiet");
menuFileListFavorites = CreateMenuItem("List Favorites ...", "Alt+L", menuItem_Click, "frame silent");
menuFileFind = CreateMenuItem("File Find ...", "Alt+Shift+F", menuItem_Click, "frame speak");
menuFileSave = CreateMenuItem("&Save", "Control+S", menuItem_Click, "child speak");
menuFileSaveAs = CreateMenuItem("Save &As ...", "Control+Shift+S", menuItem_Click, "child silent");
menuFileSaveCopy = CreateMenuItem("Save Copy ...", "Alt+Shift+S", menuItem_Click, "child speak");
menuFileExport = CreateMenuItem("Export Format ...", "Alt+Shift+E", menuItem_Click, "child silent");
menuFileRename = CreateMenuItem("Rename ...", "Alt+Shift+R", menuItem_Click, "child silent");
menuFileProperties = CreateMenuItem("Properties", "Alt+Enter", menuItem_Click, "child speak");
menuFileMailBody = CreateMenuItem("&Mail Body ...", "Control+M", menuItem_Click, "child speak");
menuFileMailAttach = CreateMenuItem("Mail Attachment ...", "Control+Shift+M", menuItem_Click, "child speak");
menuFilePrint = CreateMenuItem("&Print", "Control+P", menuItem_Click, "child silent");
// Run has no chord any more: F5 now opens the Markdown preview in the web
// browser, which is Kasperczak's decision of 20 August 2026 -- EdSharpNG is
// to be a Markdown editor rather than a programming one. The command stays
// in the File menu and in the Alternate Menu, so nothing is lost.
menuFileRun = CreateMenuItem("Run", "", menuItem_Click, "child speak");
menuFileCurrentWindows = CreateMenuItem("Current Windows ...", "F4", menuItem_Click, "frame silent");
menuFileClose = CreateMenuItem("&Close Window", "Control+F4", menuItem_Click, "child speak");
// Control+W is an ADDITIONAL chord for Close Window (Kasperczak, 13.08.2026);
// Control+F4 above stays as it was.  A menu item can register only one chord,
// so the extra one is dispatched in HandleCloseWindowKey.

// ZAMYKANIE WSZYSTKICH OKEN POZA BIEZACYM: Control+Shift+F4 -> Control+Shift+W
// (Kasperczak, ustalenie edsharpng-7).  Powod jego wyboru: "jak w wielu
// programach" - Control+W zamyka biezacy plik, wiec Control+Shift+W zamyka
// reszte.  Control+Shift+W byl WOLNY od 5.0.34, kiedy Unwrap zeszlo do menu.
// Zwolniony Control+Shift+F4 przejela LISTA FOLDEROW SYSTEMOWYCH.
// Control+F4 (zamknij biezace okno) ZOSTAJE NIETKNIETY - patrz komentarz
// przy Go to Folder nizej.
menuFileCloseAllButCurrentWindow = CreateMenuItem("Close All but Current Window", "Control+Shift+W", menuItem_Click, "child speak");
// Numbered Files: Alt+digit opens the file remembered under that digit,
// Alt+Shift+digit assigns the current file to it (see HandleFileSlotKey).  This
// menu item lists them so the feature is discoverable with a screen reader.
menuFileSlots = CreateMenuItem("Numbered Files ...", "Alt+Shift+F2", menuItem_Click, "frame silent");
menuFileExit = CreateMenuItem("&E&xit EdSharp", "Alt+F4", menuItem_Click, "frame speak");
menuFile.DropDownItems.AddRange(new ToolStripItem[] {menuFileNew, menuFileNewFromClipboard, menuFileOpen, menuFileOpenOtherFormat, menuFileOpenAgain, menuFileRecent, menuFileSetFavorite, menuFileClearFavorite, menuFileListFavorites, menuFileFind, menuFileSave, menuFileSaveAs, menuFileSaveCopy, menuFileExport, menuFileRename, menuFileProperties, menuFileMailBody, menuFileMailAttach, menuFilePrint, menuFileRun, menuFileCurrentWindows, menuFileClose, menuFileCloseAllButCurrentWindow, menuFileSlots, menuFileExit});
//Dialog.Show("File.", menuFile.DropDownItems.Count);

menuEdit = CreateMenu("&Edit");
menuEditSelectAll = CreateMenuItem("Select &All", "Control+A", menuItem_Click, "child speak");
menuEditUnselectAll = CreateMenuItem("Unselect All", "Control+Shift+A", menuItem_Click, "child speak");
menuEditCopy = CreateMenuItem("&Copy", "Control+C", menuItem_Click, "child speak");
menuEditCopyAppend = CreateMenuItem("Copy Append", "Alt+C", menuItem_Click, "child speak");
menuEditCopyRichText = CreateMenuItem("Copy Rich Text", "Control+Shift+C", menuItem_Click, "child speak");
menuEditCut = CreateMenuItem("Cut", "Control+&X", menuItem_Click, "child speak");
menuEditCutAppend = CreateMenuItem("Cut Append", "Alt+X", menuItem_Click, "child speak");
menuEditPaste = CreateMenuItem("Paste", "Control+&V", menuItem_Click, "child speak");
menuEditPasteFile = CreateMenuItem("Paste File ...", "Control+Shift+V", menuItem_Click, "child speak");
menuEditUndo = CreateMenuItem("Undo", "Control+&Z", menuItem_Click, "child speak");
menuEditRedo = CreateMenuItem("Redo", "Control+Shift+Z", menuItem_Click, "child speak");
menuEditStartSelection = CreateMenuItem("Start Selection", "F8", menuItem_Click, "child speak");
menuEditCompleteSelection = CreateMenuItem("Complete Selection", "Shift+F8", menuItem_Click, "child speak");
menuEditReselect = CreateMenuItem("Reselect", "Control+Shift+F8", menuItem_Click, "child speak");
menuEditCopyAll = CreateMenuItem("Copy All", "Control+F8", menuItem_Click, "child speak");
menuEditSelectChunk = CreateMenuItem("Select Chunk", "Control+Space", menuItem_Click, "child silent");
// Alt+D7 was freed for the File Slot commands (Alt+7 opens file slot 7), on
// Kasperczak's explicit authorization of that collision.  Moved to a free
// chord that does NOT involve Control+Alt, since Util.Say() suppresses speech
// while both Alt and Control are held.
// Dopisywanie do schowka na Alt+Shift+C - wybor Kasperczaka (Telegram
// 27.08.2026: "Alt+Shift+C wlacza i wylacza dopisywanie do schowka").
// Klawisz zwolnily ustawienia, ktore przeszly na Control+przecinek.
// Wczesniejsze Alt+F9 bylo naszym zastepnikiem po tym, jak cyfry poszly na
// pliki numerowane, wiec niczyjego przyzwyczajenia ta zmiana nie lamie.
menuEditAppendFromClipboard = CreateMenuItem("Append from Clipboard", "Alt+Shift+C", menuItem_Click, "child silent");
menuEditQuote = CreateMenuItem("&Quote", "Control+Q", menuItem_Click, "child speak");
menuEditUnquote = CreateMenuItem("Unquote", "Control+Shift+Q", menuItem_Click, "child speak");
menuEditUpperCase = CreateMenuItem("&Upper Case", "Control+U", menuItem_Click, "child speak");
menuEditLowerCase = CreateMenuItem("Lower Case", "Control+Shift+U", menuItem_Click, "child speak");
menuEditProperCase = CreateMenuItem("Proper Case", "Alt+U", menuItem_Click, "child speak");
menuEditSwapCase = CreateMenuItem("Swap Case", "Alt+Shift+U", menuItem_Click, "child speak");
menuEditYieldEncoding = CreateMenuItem("Yield Encoding", "Alt+Shift+Y", menuItem_Click, "child silent");
menuEditJoinLines = CreateMenuItem("Join Lines", "Control+Shift+J", menuItem_Click, "child speak");
// SKROT ZDJETY, FUNKCJA ZOSTAJE W MENU (jego decyzja 03.09.2026, wariant B:
// "Chyba lepsze.  Nie wiem, jakie praktyczne to ma znaczenie").  Twarde lamanie
// wierszy przydaje sie rzadko, glownie przy tekstach pod waska szerokosc, wiec
// litera H schodzi z Control+Shift i jest teraz WOLNA.  Sama komenda zostaje,
// bo to praca na tekscie, a nie na formatowaniu strony.
// Pusty chord = Keys.None, czyli komenda menu-only: bez wpisu w hashKey i bez
// wyswietlanego skrotu.
menuEditHardLineBreak = CreateMenuItem("Hard Line Break ...", "", menuItem_Click, "child silent");
menuEditEnterNewLine = CreateMenuItem("Enter New Line", "Enter", menuItem_Click, "child silent");
menuEditIndentNewLine = CreateMenuItem("Indent New Line", "Shift+Enter", menuItem_Click, "child silent");
menuEditIndentNewLinePrior = CreateMenuItem("Indent New Line Prior", "Alt+Shift+Enter", menuItem_Click, "child speak");
menuEditIndent = CreateMenuItem("Indent", "Tab", menuItem_Click, "child silent");
menuEditOutdent = CreateMenuItem("Outdent", "Shift+Tab", menuItem_Click, "child silent");
menuEditAlign = CreateMenuItem("Align", "Alt+Shift+A", menuItem_Click, "child speak");
menuEditIndentMode = CreateMenuItem("Indent Mode", "Alt+Shift+I", menuItem_Click, "child speak");
menuEditJustify = CreateMenuItem("Justify ...", "Alt+Shift+J", menuItem_Click, "child silent");
menuEditStyle = CreateMenuItem("Style ...", "Alt+Shift+OemQuestion", menuItem_Click, "child silent");
// Moved off Alt+Shift+D6 so file slot 6 can be assigned like every other
// slot -- Kasperczak authorized this move explicitly (14.08.2026 18:19).
// Kept the "6" mnemonic by landing on the matching function key.
menuEditBaseline = CreateMenuItem("Baseline ...", "Alt+Shift+F6", menuItem_Click, "child silent");
menuEditSetSelectionFont = CreateMenuItem("Set Selection Font ...", "Alt+Shift+OemMinus", menuItem_Click, "child speak");
menuEdit.DropDownItems.AddRange(new ToolStripItem[] {menuEditSelectAll, menuEditUnselectAll, menuEditCopy, menuEditCopyAppend, menuEditCopyRichText, menuEditCut, menuEditCutAppend, menuEditPaste, menuEditPasteFile, menuEditUndo, menuEditRedo, menuEditStartSelection, menuEditCompleteSelection, menuEditReselect, menuEditCopyAll, menuEditSelectChunk, menuEditAppendFromClipboard, menuEditQuote, menuEditUnquote, menuEditUpperCase, menuEditLowerCase, menuEditProperCase, menuEditSwapCase, menuEditYieldEncoding, menuEditJoinLines, menuEditHardLineBreak, menuEditEnterNewLine, menuEditIndentNewLine, menuEditIndentNewLinePrior, menuEditIndent, menuEditOutdent, menuEditAlign, menuEditIndentMode, menuEditJustify, menuEditStyle, menuEditBaseline, menuEditSetSelectionFont});
//Dialog.Show("Edit.", menuEdit.DropDownItems.Count);

menuDelete = CreateMenu("&Delete");
menuDeleteReplaceRegular = CreateMenuItem("&Replace ...", "Control+R", menuItem_Click, "child silent");
menuDeleteReplaceWithRegExp = CreateMenuItem("Replace with Regular Expression ...", "Control+Shift+R", menuItem_Click, "child silent");
menuDeleteHardLine = CreateMenuItem("Delete Hard Line", "Control+D", menuItem_Click, "child silent");
menuDeleteParagraph = CreateMenuItem("Delete Paragraph", "Control+Shift+D", menuItem_Click, "child silent");
menuDeleteLine = CreateMenuItem("Delete Line", "Alt+Back", menuItem_Click, "child silent");
menuDeleteRight = CreateMenuItem("Delete Right", "Control+Shift+Delete", menuItem_Click, "child silent");
menuDeleteLeft = CreateMenuItem("Delete Left", "Control+Shift+Back", menuItem_Click, "child silent");
menuDeleteDown = CreateMenuItem("Delete Down", "Alt+Shift+Delete", menuItem_Click, "child speak");
menuDeleteUp = CreateMenuItem("Delete Up", "Alt+Shift+Back", menuItem_Click, "child speak");
menuDeleteFile = CreateMenuItem("Delete File", "Alt+Shift+D", menuItem_Click, "child speak");
menuDeleteTrimBlanks = CreateMenuItem("Trim Blanks", "Control+Shift+Enter", menuItem_Click, "child speak");
menuDelete.DropDownItems.AddRange(new ToolStripMenuItem[] {menuDeleteReplaceRegular, menuDeleteReplaceWithRegExp, menuDeleteHardLine, menuDeleteParagraph, menuDeleteLine, menuDeleteRight, menuDeleteLeft, menuDeleteDown, menuDeleteUp, menuDeleteFile, menuDeleteTrimBlanks});
//Dialog.Show("Delete.", menuDelete.DropDownItems.Count);

menuNavigate = CreateMenu("&Navigate");
menuNavigateForwardFind = CreateMenuItem("Forward &Find ...", "Control+F", menuItem_Click, "child silent");
menuNavigateReverseFind = CreateMenuItem("Reverse Find ...", "Control+Shift+F", menuItem_Click, "child silent");
menuNavigateForwardFindWithRegExp = CreateMenuItem("Forward Find with Regular Expression ...", "Control+F3", menuItem_Click, "child silent");
menuNavigateReverseFindWithRegExp = CreateMenuItem("Reverse Find with Regular Expression ...", "Control+Shift+F3", menuItem_Click, "child silent");
menuNavigateForwardFindAtCursor = CreateMenuItem("Forward Find at Cursor", "Alt+F3", menuItem_Click, "child silent");
menuNavigateReverseFindAtCursor = CreateMenuItem("Reverse Find at Cursor", "Alt+Shift+F3", menuItem_Click, "child silent");
menuNavigateForwardFindAgain = CreateMenuItem("Forward Find Again", "F3", menuItem_Click, "child silent");
menuNavigateReverseFindAgain = CreateMenuItem("Reverse Find Again", "Shift+F3", menuItem_Click, "child silent");
menuNavigateJumpToLine = CreateMenuItem("&Jump to Line ...", "Control+J", menuItem_Click, "child silent");
menuNavigateJumpToLineAgain = CreateMenuItem("Jump to Line Again", "Alt+J", menuItem_Click, "child silent");
menuNavigateGoToPercent = CreateMenuItem("&Go to Percent ...", "Control+G", menuItem_Click, "child silent");
menuNavigateGoToPercentAgain = CreateMenuItem("Go to Percent Again", "Alt+G", menuItem_Click, "child silent");
// Go to Part (Alt+Shift+G) USUNIETA na polecenie Kasperczaka
// (27.08.2026, zlecenie 1787793592388-1) razem z Next Part (Alt+PageDown)
// i Prior Part (Alt+PageUp), ktorych deklaracje sa nizej.  Wszystkie trzy
// szukaly wyrazenia NavigatePart, ustawianego PER JEZYK PROGRAMOWANIA
// (w konfiguracji kompilatora; domyslnie Chapter, Section lub Part plus
// liczba), wiec w edytorze Markdown nie mialy czego znalezc.  Jego slowa:
// ta nawigacja dziala wylacznie z wzorcami ustawianymi per jezyk
// programowania, wiec w edytorze Markdown jest bezuzyteczna.
// Strukture dokumentu opisuja naglowki Markdown: Control z PageUp albo
// PageDown, drzewo pod F6, spis tresci pod Alt+Shift+T i skok pod Shift+F6.
// Skroty Alt+Shift+G, Alt+PageUp i Alt+PageDown sa teraz WOLNE.
// Nie przywracac przy scalaniu z upstream, gdzie te komendy nadal sa.
// ZAKLADKI PRZENIESIONE NA B (Kasperczak, ustalenia edsharpng-4 i edsharpng-12,
// zlecenie 1788041000816-5).  Control+B wstawia zakladke, Alt+B pokazuje ich
// liste.  Oba chordy zwolnila usunieta nawigacja po blokach kodu i usuniete
// pytanie o blok - patrz komentarze wyzej.
// Powod jego wyboru: B jak "bookmark", a K bylo potrzebne na WSTAWIANIE LINKU
// (ustalenie edsharpng-9), ktore siedzi teraz na Control+K.
// USUWANIE ZAKLADKI BEZ WLASNEGO KLAWISZA - jego decyzja, doslownie: "usuwanie
// zakladki z poziomu tekstu niepotrzebne (Delete na liscie wystarcza)".
// Komenda ZOSTAJE w menu, bo droga do niej musi istniec takze dla kogos, kto
// listy nie otwiera; klawisza nie zajmuje.  Delete na liscie zakladek
// (Dialog.PickBookmark) dziala jak dotad.  Control+Shift+K sie ZWALNIA.
menuNavigateSetBookmark = CreateMenuItem("Set Bookmar&k", "Control+B", menuItem_Click, "child speak");
menuNavigateClearBookmark = CreateMenuItem("Clear Bookmark", "", menuItem_Click, "child speak");
menuNavigateGoToBookmark = CreateMenuItem("Go to Bookmark", "Alt+B", menuItem_Click, "child speak");
// Sequential bookmark navigation (Michal Kasperczak, 13.08.2026): jump
// straight to the next / previous bookmark without opening the list, and
// speak the whole line landed on. Shift+PageUp / Shift+PageDown were
// unassigned (verified: no occurrence in any *.cs or EdSharp.ini).
menuNavigateNextBookmark = CreateMenuItem("Next Bookmark", "Shift+PageDown", menuItem_Click, "child silent");
menuNavigatePriorBookmark = CreateMenuItem("Prior Bookmark", "Shift+PageUp", menuItem_Click, "child silent");
// ZAKLADKI Z NAZWA (Kasperczak, ustalenie edsharpng-40 i jego zgloszenie z
// 31.08.2026 23:25: "ctrl+shift+B i alt+shift+B dalej nie dzialaja").  Oba
// chordy byly WOLNE: Control+Shift+B zwolnila usunieta nawigacja po blokach
// kodu, Alt+Shift+B zdjete nagrywanie plyt.
// DWIE OSOBNE LISTY, nie jedno okienko z wyborem rodzaju - jego decyzja.
// MAGAZYN JEST INNY NIZ U ZAKLADEK ZWYKLYCH i to jest celowe: zwykle siedza w
// sekcji Favorites (dlatego zakladka czyni plik ulubionym, o czym on wie i co
// czeka na jego decyzje), a nazwane maja WLASNA sekcje NamedBookmarks z
// osobnym kluczem na kazda zakladke: "<sciezka>|<indeks>" = nazwa.  Klucz na
// zakladke, a nie jedna dluga wartosc, bo GetPrivateProfileString czyta do
// bufora 260 znakow - lista w jednej wartosci ucinalaby sie po kilkunastu
// nazwach BEZ ZADNEGO komunikatu.
menuNavigateSetNamedBookmark = CreateMenuItem("Set &Named Bookmark ...", "Control+Shift+B", menuItem_Click, "child silent");
menuNavigateNamedBookmarkList = CreateMenuItem("Named Bookmark &List ...", "Alt+Shift+B", menuItem_Click, "child silent");
menuNavigateHomeCharacter = CreateMenuItem("Home Character", "Alt+Home", menuItem_Click, "child silent");
menuNavigateEndCharacter = CreateMenuItem("End Character", "Alt+End", menuItem_Click, "child silent");
menuNavigateStartTag = CreateMenuItem("Start Tag", "Control+Shift+Oemcomma", menuItem_Click, "child silent");
menuNavigateEndTag = CreateMenuItem("End Tag", "Control+Shift+OemPeriod", menuItem_Click, "child silent");
// SKOKI PO BOGATYM FORMATOWANIU USUNIETE na polecenie Kasperczaka
// (29.08.2026, zlecenie 1788006241286-4, jego mini korekta naszej listy).
// Zniknely: Next/Prior Alignment (Control z nawiasami kwadratowymi),
// Next/Prior Style (Control z ukosnikiem), Next/Prior Baseline (Control+F2
// i Control+Shift+F2, czyli indeks gorny i dolny) oraz Next/Prior Font
// (Control z myslnikiem).  Jego slowa: "usuwamy te wszystkie wyrownania,
// wciecia rozumiane jako bogate formatowanie strony i tak dalej. W ten
// sposob edytor stanie sie czysto tekstowy i nie ma sensu zeby skakac do
// czcionki, stylu i tak dalej".
// UWAGA na przyszlosc: program formatowanie UMIE, bo obsluguje pliki RTF -
// te komendy usuwamy jako NIEPOTRZEBNE dla kierunku Markdown, nie jako
// niemozliwe.  W Markdownie pogrubienie to gwiazdki W TEKSCIE, wiec skok po
// zmianie kroju pisma nie ma czego znalezc.
// Same USTAWIACZE formatowania zostaja w menu Edit (Justify, Style,
// Baseline, Set Selection Font) - dotykaja plikow RTF, ktore program nadal
// otwiera, i o ich losie Kasperczak nie rozstrzygal.
// Zwolnione chordy: Control+OemCloseBrackets, Control+OemOpenBrackets,
// Control+OemQuestion, Control+Shift+OemQuestion, Control+F2,
// Control+Shift+F2, Control+OemMinus, Control+Shift+OemMinus.
// Nie przywracac przy scalaniu z upstream, gdzie te komendy nadal sa.
menuNavigateRightBrace = CreateMenuItem("Right Brace", "Control+Shift+OemCloseBrackets", menuItem_Click, "child silent");
menuNavigateLeftBrace = CreateMenuItem("Left Brace", "Control+Shift+OemOpenBrackets", menuItem_Click, "child silent");
// NAWIGACJA PO BLOKACH KODU USUNIETA (Kasperczak, ustalenie edsharpng-4,
// zlecenie hurtowe 1788041000816-5).  Next Block siedzial na Control+B, Prior
// Block na Control+Shift+B, a szly one po WCIECIACH kodu (blok to ciag wierszy
// o tym samym albo wiekszym wciecu).  W edytorze Markdown nie ma po czym tak
// skakac: wciecie znaczy tam pozycje listy albo cytat, nie strukture programu.
// Control+B jest teraz WSTAWIANIEM ZAKLADKI, a Control+Shift+B zostaje wolny.
// Skoki po zmianach wciecia (Control+I, Control+Shift+I) ZOSTAJA - o nich nie
// rozstrzygal, a dotycza takze tekstu.
menuNavigateNextIndent = CreateMenuItem("Next Indent", "Control+I", menuItem_Click, "child silent");
menuNavigatePriorIndent = CreateMenuItem("Prior Indent", "Control+Shift+I", menuItem_Click, "child silent");
menuNavigateNextChunk = CreateMenuItem("Next Chunk", "Alt+Right", menuItem_Click, "child silent");
menuNavigatePriorChunk = CreateMenuItem("Prior Chunk", "Alt+Left", menuItem_Click, "child silent");
menuNavigateNextSentence = CreateMenuItem("Next Sentence", "Alt+Down", menuItem_Click, "child silent");
menuNavigatePriorSentence = CreateMenuItem("Prior Sentence", "Alt+Up", menuItem_Click, "child silent");
menuNavigateNextParagraph = CreateMenuItem("Next Paragraph", "Control+Down", menuItem_Click, "child silent");
menuNavigatePriorParagraph = CreateMenuItem("Prior Paragraph", "Control+Up", menuItem_Click, "child silent");
// Next Part (Alt+PageDown) i Prior Part (Alt+PageUp) USUNIETE - powod przy
// deklaracji Go to Part wyzej (zlecenie 1787793592388-1).
menuNavigateNextSection= CreateMenuItem("Next Section", "Control+PageDown", menuItem_Click, "child silent");
menuNavigatePriorSection= CreateMenuItem("Prior Section", "Control+PageUp", menuItem_Click, "child silent");
// Same-level variants, approved by Kasperczak 14.08.2026 18:50.  Shift means
// "only my level", skipping subsections, mirroring how screen readers let you
// walk headings of one level in a browser.
menuNavigateNextSectionSameLevel= CreateMenuItem("Next Section at Same Level", "Control+Shift+PageDown", menuItem_Click, "child silent");
menuNavigatePriorSectionSameLevel= CreateMenuItem("Prior Section at Same Level", "Control+Shift+PageUp", menuItem_Click, "child silent");
// Document Navigation, F6 -- a tree of the document's Markdown headings,
// the way a book reader (PaperBack) presents a table of contents: branches
// expand and collapse, letter keys jump, Enter moves the cursor to that
// heading in the text, and reopening the tree returns to the place last
// focused. Requested by Michal Kasperczak (18.08.2026 23:43), who also
// decided that F6 takes precedence over whatever else used F6: "To F6
// zastapi ewentualne inne funkcje F6." So the old structured-text command
// Go to Section moved off F6 (see below). With no heading in the document
// the command only says so and opens nothing -- his decision (23:46:18):
// "Komunikat, tak jak mowisz. I tyle."
menuNavigateDocumentNavigation = CreateMenuItem("Document Navigation ...", "F6", menuItem_Click, "child silent");
// Go to Section (Control+Shift+F12) i Go to Contents (Shift+F6) zostaly
// USUNIETE na polecenie Michala Kasperczaka (26.08.2026, zlecenie -8:
// "Zgadzam sie z wszystkim innym odnosnie [...] usuniecia wszystkiego
// spis tresci w obecnej formie"). Nalezaly do ORYGINALNEGO modelu tekstu
// strukturalnego (sekcje rozdzielane znakiem Form Feed), ktory EdSharpNG
// zastapil naglowkami Markdown, wiec w plikach uzytkownika mogly tylko
// odpowiedziec "Not found!" (zmierzone: 0 znakow Form Feed w jego liscie
// testow, w pliku wynikow i w podreczniku). Role spisu tresci pelni
// drzewo Document Navigation pod F6. Skrot Shift+F6 zostaje WOLNY -
// Kasperczak wskazal go na przyszly, prawdziwy spis tresci.
// Nie przywracac przy scalaniu z upstream, gdzie te komendy nadal sa.
//
// Shift+F6 wrocilo 27.08.2026 jako skok miedzy spisem tresci i rozdzialem
// (zlecenie 1787793592380-0).  Kasperczak wskazal ten skrot sam i chcial
// go KONTEKSTOWEGO w obie strony: "Shift-F6 kontekstowo w obie strony to
// dobre rozwiazanie.  Jestem za takimi kontekstowymi rozwiazaniami
// wielofunkcyjnymi".  Z tresci dokumentu skacze do pozycji spisu, a z
// pozycji spisu do rozdzialu.  Spis tworzy Alt+Shift+T (menu Misc).
menuNavigateGoToContents = CreateMenuItem("Go to Contents", "Shift+F6", menuItem_Click, "child silent");
// Search for Topic (Control+F6) i Search for Topic Again (Alt+F6) USUNIETE
// 27.08.2026 na zgode Kasperczaka ("Tak. Mozna podmienic ten klawisz.").
// Nalezaly do ORYGINALNEGO modelu tekstu strukturalnego: szukaly wzorca
// SB + tekst + LB, gdzie SB to znak Form Feed z lamaniem wiersza.  W plikach
// Markdown znaku Form Feed nie ma ani jednego (zmierzone na jego liscie
// testow, pliku wynikow i podreczniku), wiec komendy mogly tylko odpowiedziec
// "Not found!".  Sam zglosil to jako defekt w tescie 1.10: "Alt-F6 nie
// dziala".  Ta sama rodzina, z ktorej odeszly juz Go to Section i stary spis
// tresci.  Rola szukania rozdzialu pelni drzewo F6 i Control z PageUp/PageDown.
// Nie przywracac przy scalaniu z upstream, gdzie te komendy nadal sa.
// Zwolnione klawisze przejely przypisy - patrz menu Misc.
menuNavigateGoToStartOfSelection = CreateMenuItem("Go to Start of Selection", "Alt+Shift+F8", menuItem_Click, "child speak");
// NAWIGACJA PO ELEMENTACH MARKDOWN (rozmowa 30.08.2026 15:41-15:42).
// Kasperczak rozstrzygnal wahanie o klawisz jednym zdaniem: "Control/
// pogrubienie i podkreslenie razem, control myslnik, skakanie po listach"
// (ustalenia edsharpng-45 i edsharpng-46).  Czyli:
//   Control z ukosnikiem  - nastepne WYROZNIENIE (pogrubienie ALBO pochylenie),
//   Control z myslnikiem  - nastepna LISTA.
// Wariant Control+Shift z myslnikiem pod wyroznienia ODPADL wlasnie dlatego,
// ze myslnik jest jego wyborem dla list, a dwie rodziny nie zmieszcza sie na
// jednym klawiszu.
// JEDNA KOMENDA NA OBA WYROZNIENIA, nie dwie: ustalenie edsharpng-42 mowi,
// ze osobnej komendy dla pochylenia NIE CHCE ("tego sie raczej za duzo nie
// uzywam"), a teraz dolozyl, ze pochylenie ma byc szukane RAZEM z
// pogrubieniem.  Wypowiedz nazywa rodzaj, wiec niewidomy wie, na czym stoi.
// LISTY STAJA NA POCZATKU LISTY, nie na kazdej pozycji - jego slowa z 15:42:
// "Na poczatek kazdej nowej listy, bo po elementach mozna chodzic strzalka w
// dol".  Program juz umie odroznic POCZATEK listy od jej pozycji (parser
// podgladu Markdown zbiera jedno i drugie osobno), wiec bierzemy ten sam
// pomiar, ktory czyta lista elementow pod F7.
// Chordy byly wolne od 5.0.44, gdy zniknely skoki po bogatym formatowaniu -
// sprawdzone w CreateMenuItem ORAZ w handlerach, bo sam Hotkeys.ini klamie.
menuNavigateNextEmphasis = CreateMenuItem("Next Emphasis", "Control+OemQuestion", menuItem_Click, "child silent");
menuNavigatePriorEmphasis = CreateMenuItem("Prior Emphasis", "Control+Shift+OemQuestion", menuItem_Click, "child silent");
menuNavigateNextList = CreateMenuItem("Next List", "Control+OemMinus", menuItem_Click, "child silent");
menuNavigatePriorList = CreateMenuItem("Prior List", "Control+Shift+OemMinus", menuItem_Click, "child silent");
menuNavigateLinkList = CreateMenuItem("Link List ...", "Control+F6", menuItem_Click, "child silent");
// SKOK PO ODNOSNIKACH (ustalenie edsharpng-36, jego decyzja z 30.08.2026):
// "Skok po linkach na Alt+PageUp i Alt+PageDown, skok po komentarzach na
// Alt+Shift+PageUp i Alt+Shift+PageDown".  Lista odnosnikow pod Control+F6
// ZOSTAJE i nic nie traci: lista pokazuje caly dokument naraz, a te dwa
// klawisze przechodza od odnosnika do odnosnika bez otwierania okna, tak samo
// jak przy naglowkach (Control+PageUp/PageDown) i przypisach
// (Control+Alt+PageUp/PageDown).  Cala rodzina PageUp/PageDown jest teraz
// spojna: goly modyfikator wybiera RODZAJ elementu, Shift odwraca kierunek.
//
// CHORDY ZMIERZONE JAKO WOLNE, nie zalozone: mapa_chordow.py na HEAD nie
// wymienia zadnego Alt+PageUp ani Alt+PageDown (Next Part i Prior Part, ktore
// je nosily do 27.08.2026, sa USUNIETE), a w ProcessCmdKey_Helper zaden jawny
// warunek ich nie lapie - sprawdzone w OBU miejscach, bo sam spis skrotow
// pokazuje tylko czesc prawdy (lekcja z 5.0.43 i golego F9).
menuNavigateNextLink = CreateMenuItem("Next Link", "Alt+PageDown", menuItem_Click, "child silent");
menuNavigatePriorLink = CreateMenuItem("Prior Link", "Alt+PageUp", menuItem_Click, "child silent");
menuNavigate.DropDownItems.AddRange(new ToolStripItem[] {menuNavigateForwardFind, menuNavigateReverseFind, menuNavigateForwardFindWithRegExp, menuNavigateReverseFindWithRegExp,  menuNavigateForwardFindAtCursor, menuNavigateReverseFindAtCursor, menuNavigateForwardFindAgain, menuNavigateReverseFindAgain, menuNavigateJumpToLine, menuNavigateJumpToLineAgain, menuNavigateGoToPercent, menuNavigateGoToPercentAgain, menuNavigateSetBookmark, menuNavigateClearBookmark, menuNavigateGoToBookmark, menuNavigateNextBookmark, menuNavigatePriorBookmark, menuNavigateSetNamedBookmark, menuNavigateNamedBookmarkList, menuNavigateHomeCharacter, menuNavigateEndCharacter, menuNavigateStartTag, menuNavigateEndTag, menuNavigateRightBrace, menuNavigateLeftBrace, menuNavigateNextIndent, menuNavigatePriorIndent, menuNavigateNextChunk,  menuNavigatePriorChunk, menuNavigateNextSentence, menuNavigatePriorSentence, menuNavigateNextParagraph, menuNavigatePriorParagraph, menuNavigateNextSection, menuNavigatePriorSection, menuNavigateNextSectionSameLevel, menuNavigatePriorSectionSameLevel, menuNavigateDocumentNavigation, menuNavigateGoToContents, menuNavigateNextEmphasis, menuNavigatePriorEmphasis, menuNavigateNextList, menuNavigatePriorList, menuNavigateLinkList, menuNavigateNextLink, menuNavigatePriorLink, menuNavigateGoToStartOfSelection});
//Dialog.Show("Navigate.", menuNavigate.DropDownItems.Count);

menuQuery = CreateMenu("&Query");
menuQueryAddress = CreateMenuItem("Address", "Alt+A", menuItem_Click, "child silent");
menuQueryBraces = CreateMenuItem("Braces", "Alt+Shift+OemCloseBrackets", menuItem_Click, "child silent");
menuQueryIndent = CreateMenuItem("Indentation", "Alt+I", menuItem_Click, "child silent");
menuQueryPath = CreateMenuItem("Path", "Alt+P", menuItem_Click, "child silent");
menuQueryTopic = CreateMenuItem("Topic", "Alt+T", menuItem_Click, "child speak");
menuQueryYield = CreateMenuItem("Yield", "Alt+Y", menuItem_Click, "child speak");
menuQueryStatus = CreateMenuItem("Status", "Alt+Z", menuItem_Click, "child silent");
// Alt+D0 was freed for the File Slot commands (Alt+0 opens file slot 10).
menuQueryCompiler = CreateMenuItem("Compiler", "Control+F9", menuItem_Click, "frame silent");
menuQuerySelected = CreateMenuItem("Selected", "Shift+Space", menuItem_Click, "child silent");
menuQueryChunk = CreateMenuItem("Chunk", "Shift+Back", menuItem_Click, "child silent");
menuQueryReadAll = CreateMenuItem("Read All", "Alt+F8", menuItem_Click, "child speak");
// KOMENDA "Windows Open" (Shift+F4, mowila tytuly otwartych okien) USUNIETA -
// Kasperczak, ustalenie edsharpng-6, jego uzasadnienie: F4 i tak pokazuje
// LISTE okien, po ktorej mozna chodzic strzalkami, a lista jest lepsza niz
// wyliczanka, bo w wyliczance nie da sie zatrzymac.  Shift+F4 dostaje LISTE
// FOLDEROW (Go to Folder) - patrz ustalenie edsharpng-5.
menuQueryClipboard = CreateMenuItem("Clipboard", "Alt+OemQuotes", menuItem_Click, "frame silent");
menuQueryTime = CreateMenuItem("Time", "Alt+OemSemicolon", menuItem_Click, "frame silent");
menuQueryStyles = CreateMenuItem("Styles", "Alt+OemQuestion", menuItem_Click, "child silent");
menuQueryFont = CreateMenuItem("Font", "Alt+OemMinus", menuItem_Click, "child silent");
menuQuery.DropDownItems.AddRange(new ToolStripItem[] {menuQueryAddress, menuQueryBraces, menuQueryIndent, menuQueryPath, menuQueryTopic, menuQueryYield, menuQueryStatus, menuQueryCompiler, menuQuerySelected, menuQueryChunk, menuQueryReadAll, menuQueryClipboard, menuQueryTime, menuQueryStyles, menuQueryFont});
//Dialog.Show("Query.", menuQuery.DropDownItems.Count);

menuMisc = CreateMenu("&Misc");
menuMiscSetDefaultFont = CreateMenuItem("Set Default Font and Color ...", "Alt+Shift+Oemplus", menuItem_Click, "child speak");
// Ustawienia programu na Control+przecinek - wybor Kasperczaka (Telegram
// 27.08.2026: "Ctrl+przecinek uaktywnia ustawienia").  To zwyczaj z wiekszosci
// dzisiejszych programow, a zwolniony Alt+Shift+C dostaje dopisywanie do
// schowka, ktore o ten wlasnie chord poprosil.  Chord byl wolny: jedyne
// wystapienie przecinka w mapie klawiszy to Control+Shift+Oemcomma (Start Tag).
menuMiscConfigurationOptions = CreateMenuItem("Configuration Options ...", "Control+Oemcomma", menuItem_Click, "frame silent");
menuMiscManualOptions = CreateMenuItem("Manual Options", "Alt+Shift+M", menuItem_Click, "frame silent");
// Moved off Alt+Shift+D0 so file slot 10 can be assigned like every other
// slot -- Kasperczak authorized this move explicitly (14.08.2026 18:19).
// This command wipes settings, so it now sits on a chord that is hard to
// hit by accident.
menuMiscResetConfiguration = CreateMenuItem("Reset Configuration", "Alt+Shift+F10", menuItem_Click, "frame silent");
// FOLDERY W RODZINIE F4 (Kasperczak, ustalenia edsharpng-5 i edsharpng-8).
// Jego zamysl to spojna rodzina wokol F4: F4 lista okien, Shift+F4 lista
// folderow, Control+Shift+F4 lista folderow systemowych.  Shift+F4 zwolnila
// usunieta komenda mowiaca tytuly okien, Control+Shift+F4 - przeniesione
// zamykanie okien.  Stare Control+0 i Control+Alt+0 sa teraz WOLNE.
//
// CZEGO TU NIE MA I DLACZEGO: ustalenie edsharpng-5 wymienia takze
// "Control+F4 zmiana folderu".  Control+F4 to dzis ZAMKNIJ OKNO - i tak jest
// w kazdym programie wielookienkowym Windows, nie tylko u nas.  Oddanie go
// folderom odebraloby standardowy klawisz zamykania, wiec tego NIE ROBIMY bez
// jego slowa; zapytany osobno.
menuMiscGoToFolder = CreateMenuItem("Go to Folder", "Shift+F4", menuItem_Click, "frame silent");
menuMiscGoToSpecialFolder = CreateMenuItem("Go to Special Folder", "Control+Shift+F4", menuItem_Click, "frame silent");
// ZAWIJANIE I ROZWIJANIE WIERSZY: MENU, BEZ SKROTOW (Kasperczak, Telegram
// 27.08.2026 22:21: "Oba do menu.  Najwyzej bedziemy przywracac do klawiszy
// potem").  Wczesniej Word Wrap siedzial na Control+F12, a Unwrap na
// Control+Shift+W.  Oba chordy sa teraz WOLNE.  Puste sKey daje Keys.None,
// czyli komenda menu-only: bez wpisu w hashKey i bez wyswietlanego skrotu.
// Control+W nadal zamyka okno.
menuMiscWordWrap = CreateMenuItem("&Word Wrap", "", menuItem_Click, "child speak");
menuMiscUnwrap = CreateMenuItem("Unwrap", "", menuItem_Click, "child speak");
// KOMENDA USUNIETA CALKIEM (jego decyzja 03.09.2026, wariant B: "Tak.
// Usunac.  To bylo glownie pod Jaws").  Pytalem wprost, bo usuniecie
// przelacznika w stanie "wylaczone" odebraloby dodatkowe komunikaty mowy na
// stale - i wlasnie dlatego razem z komenda schodzi WPIS z pliku ustawien:
// App.ClearExtraSpeechOption() usuwa klucz ExtraSpeech, wiec przy nastepnym
// starcie odczyt wraca do domyslnego "Y".  Nikt nie zostanie z cisza bez
// wlacznika.  Dziennik mowy (Extra Speech Log, Alt+Shift+X) ZOSTAJE - to
// osobna funkcja, ktora czyta plik, a nie przelacza ustawienie.
menuMiscExtraSpeechLog = CreateMenuItem("Extra Speech Log", "Alt+Shift+X", menuItem_Click, "frame speak");
// SKROT ZDJETY, FUNKCJA ZOSTAJE W MENU (jego decyzja 01.09.2026): "To jest
// wartosciowa funkcja, ale zupelnie nie rozumiem, po co pod takim dosyc
// prostym, waznym Ctrl-E.  To moze byc w jakis narzedziach, moim zdaniem bez
// skrotu klawiszowego".  Zmiana zmiennych srodowiskowych Windows jest rzecza
// programistyczna, obca edytorowi Markdown, a litera E jest cenna.
// Pusty chord = Keys.None, czyli komenda menu-only: bez wpisu w hashKey i bez
// wyswietlanego skrotu.  Litera E jest teraz WOLNA pod Control.
menuMiscEnvironmentVariables = CreateMenuItem("&Environment Variables ...", "", menuItem_Click, "frame speak");
menuMiscSpellCheck = CreateMenuItem("Spell Check", "F7", menuItem_Click, "child speak");
// Ta sama rzecz co klawisz Aplikacje, ale widoczna w menu - dla klawiatur,
// ktore klawisza Aplikacje nie maja (laptopy, klawiatury brajlowskie).
menuMiscSpellingWordMenu = CreateMenuItem("Word Spelling Menu", "Apps", menuItem_Click, "child silent");
menuMiscThesaurus = CreateMenuItem("Thesaurus", "Shift+F7", menuItem_Click, "child speak");
menuMiscLookupTerm = CreateMenuItem("Lookup Term", "Alt+F7", menuItem_Click, "frame silent");
menuMiscTranslateLanguage = CreateMenuItem("Translate Language", "Alt+Shift+F7", menuItem_Click, "frame speak");
// KOMUNIKAT MOWI SKUTEK, NIE NAZWE KOMENDY (jego zgloszenie 03.09.2026:
// "nacisnalem ctrl+f7 i mi zabezpieczyl dokument a nacisnalem
// ctrl+shift+f7 nic mi nie powiedzial a kiedys mowil").
//
// JEDEN SKROT ZAMIAST DWOCH (jego decyzja 03.09.2026: "Ja bym dal wspolny
// skrot Ctrl-F7 wlacz/wylacz zabezpieczenie i tyle, a z shiftem bedzie
// wolny").  Do 5.0.65 byly DWIE komendy jednokierunkowe: Guard Document na
// Control+F7 i No Guard na Control+Shift+F7.  Teraz jest jedna komenda
// PRZELACZAJACA, a Control+Shift+F7 jest wolny.
//
// MOWA PODAJE SKUTEK, NIE NAZWE KOMENDY (zaczete w 5.0.65 i tu zostaje):
// nazwa pozycji menu nie niesie stanu, wiec nie da sie odroznic realnej
// zmiany od nacisniecia bez skutku.  Przy przelaczniku to wazniejsze niz
// przy dwoch komendach, bo TEN SAM klawisz robi dwie rozne rzeczy - jedyne,
// z czego niewidomy pozna, co sie stalo, to komunikat.
// SetGuard ZWRACA stan poprzedni, wiec skutek jest wyliczalny bez zadnego
// nowego odczytu.
// Opcja "silent" NIE oznacza tu ciszy: SetStatus stawia nazwe komendy na
// pasku stanu (zostaje do wzroku i dla czytnika na zadanie), a mowa idzie
// JEDNA droga, przez AddMessage ze skutkiem.
menuMiscGuardDocument = CreateMenuItem("Guard Document", "Control+F7", menuItem_Click, "child silent");
menuMiscPyBrace = CreateMenuItem("PyBrace", "Alt+Shift+OemOpenBrackets", menuItem_Click, "child speak");
menuMiscPyDent = CreateMenuItem("PyDent", "Alt+OemOpenBrackets", menuItem_Click, "child speak");
menuMiscInferIndent = CreateMenuItem("Infer Indent", "Alt+OemCloseBrackets", menuItem_Click, "child silent");
// Format Code (originally Control+4, later Control+Shift+F6 here) has been
// REMOVED from this fork on Kasperczak's decision (Telegram 26.08.2026:
// "ta funkcja format code ... to na pewno ja usuwamy, bo ona tylko mieszala
// i byla beznadziejna"). It rewrote the whole document through an external
// tool, and in the 5.0 beta original it did so on ANY file, because the
// guard read "if (sExt.Contains(sExt))" - an extension always contains
// itself. That cost him a file of test results on 26.08.2026. A whole
// document rewrite is not something a text editor for blind users should
// offer, so the command is gone rather than merely fixed. If it ever
// reappears from upstream, this comment is the reason it must not.
// SKROT ZDJETY, FUNKCJA ZOSTAJE W MENU (jego decyzja 01.09.2026): "Usuwamy,
// bo chyba jest to CTRL+C od razu, ktore kopiuje wiersz, wiec to jest prawie
// to samo.  To znaczy rozumiem roznice, ale chyba bysmy podarowali sobie".
// Roznica jest realna (Control+C kopiuje wiersz DO SCHOWKA, Repeat Line pisze
// jego kopie od razu w dokumencie), wiec komenda zostaje w menu; zwalnia sie
// sam klawisz.  Control+Y przejmuje PONAWIANIE, o co poprosil w pierwszym
// zdaniu tego samego pliku: "CTRL-y ponawianie proponuje".
menuMiscRepeatLine = CreateMenuItem("Repeat Line", "", menuItem_Click, "child speak");
menuMiscSectionBreak = CreateMenuItem("Section Break", "Control+Enter", menuItem_Click, "child speak");
menuMiscPathToClipboard = CreateMenuItem("Path to Clipboard", "Alt+Shift+P", menuItem_Click, "child speak");
menuMiscPathList = CreateMenuItem("Path List", "Control+Shift+P", menuItem_Click, "frame speak");
menuMiscInsertTime = CreateMenuItem("Insert Time", "Alt+Shift+OemSemicolon", menuItem_Click, "child speak");
// Calculate Date USUNIETA na polecenie Kasperczaka (29.08.2026, zlecenie
// 1788006241286-4).  Otwierala okno z czterema polami (rok, miesiac, tydzien,
// dzien) i wstawiala policzona date do dokumentu.  W edytorze Markdown to
// kalkulator kalendarza, nie praca nad tekstem; Insert Time (Alt+Shift ze
// srednikiem) wstawia biezaca date i zostaje nietknieta.
// Chord Control+Shift+OemSemicolon jest teraz WOLNY.  Metoda CalculateDate
// oraz jej pola konfiguracji Year, Month, Week i Day tez zostaly usuniete;
// Util.Month2Num i Util.Day2Num ZOSTAJA, bo uzywa ich takze Util samo z siebie.
// Nie przywracac przy scalaniu z upstream, gdzie ta komenda nadal jest.
menuMiscPreviewMarkdownBrowser = CreateMenuItem("Preview Markdown in Web Browser", "F5", menuItem_Click, "child silent");
// TEXT CONVERT USUNIETA CALKOWICIE (Kasperczak, ustalenie edsharpng-1,
// zlecenie 1788041000816-5).  Etap pierwszy, 29.08.2026, zdjal jej klawisz
// Control+T i zostawil pozycje w menu.  On kazal usunac FUNKCJE, nie tylko
// klawisz, doslownie: "zglasza blad Unexpected Event i jest zbedna".
// Hurtowa zamiana listy plikow na pliki .txt na dysku nie nalezy do pracy nad
// tekstem, a byla jedyna komenda tego programu, ktora PISALA NA DYSK bez
// pytania - przy dokumencie, ktory nie jest lista plikow, dawalo to okno
// bledu.  Control+T zostaje WOLNY, jego wlasne slowa: "byc moze na tlumacza".
// TEXT COMBINE ZOSTAJE (menu, bez skrotu): laczy pliki w NOWYM oknie,
// niczego nie nadpisuje, a on sam prosil, zeby ja zachowac.
// TABELE (punkt 2 mapy drogowej, ustalenia Kasperczaka z rozmowy 28.08.2026
// 17:00-17:06).  Kreator tabeli na Control+Shift+T - on sam zapytal o skrot
// ("jakim skrotem uruchamiamy Kreator tabeli?") i przyjal propozycje slowami
// "Control shift t to dobry pomysl".
//
// Text Combine TRACI chord i schodzi do MENU-ONLY, nie ginie.  Jego decyzja:
// "Laczenie plikow wyodrebnianie rozdzialow do osobnych plikow to funkcja
// ktora kiedys jakos osobno opracujemy nawet moze pod jakims control shift
// Klawisz funkcyjny ale to zupelnie kiedys indziej".  Puste sKey daje
// Keys.None, czyli brak wpisu w hashKey i brak wyswietlanego skrotu - ten
// sam wzorzec co Word Wrap i Unwrap w 5.0.34.
menuMiscTextCombine = CreateMenuItem("Text Combine", "", menuItem_Click, "child speak");
menuMiscInsertTable = CreateMenuItem("Insert Table ...", "Control+Shift+T", menuItem_Click, "child silent");
// CSV JAKO TABELA NA ZADANIE (zadanie 10, 5.0.87). Bez skrotu klawiszowego -
// przy otwieraniu pliku .csv program pyta sam, a to jest droga dla pliku
// otwartego wczesniej albo otwartego jako tekst swiadomie.
menuMiscCsvTable = CreateMenuItem("Edit CSV as Table ...", "", menuItem_Click, "child silent");
// LISTY POD LITERA L (Kasperczak, 31.08.2026, ustalenia edsharpng-56 i -57):
// "CTRL-l punktowana, cTRL-Shift-l numerowana.  Jak przelaczniku wlacza/zamienia
// na tekst zwykly.  CTRL-Shift-7 i 8 staja sie wolne".  Litera L jest tu
// mnemonikiem (list), a cyfry byly nie do zapamietania.
//
// PRZELACZNIK ZAMIENIA NA TEKST ZWYKLY, NIE NA DRUGA LISTE - jego uscislenie
// z 23:08, po tym jak zapytal sam siebie, jak inaczej wyjsc z listy.  Czyli
// Control+L na liscie punktowanej ZDEJMUJE punktory, a nie robi z niej
// numerowanej; zamiana rodzaju to dwa nacisniecia przez tekst zwykly.
// Odrzucil przy tym wlasny pomysl kasowania calego formatowania Markdown
// (edsharpng-58) - "ta pierwsza opcja bedzie lepsza".
menuMiscBulletList = CreateMenuItem("Bulleted List", "Control+L", menuItem_Click, "child silent");
menuMiscNumberedList = CreateMenuItem("Numbered List", "Control+Shift+L", menuItem_Click, "child silent");
// WSTAWIANIE LINKU (Control+K) - ustalenie edsharpng-9, zlecenie
// 1788041000816-5.  Control+K zwolnily ZAKLADKI, ktore przeszly na Control+B.
// Okno pyta o tresc, adres i RODZAJ linku: zwykly, graficzny albo wewnetrzny
// (do naglowka w tym dokumencie).  Przy wewnetrznym adresu sie nie wpisuje -
// wybiera sie naglowek z listy, a kotwice liczy BuildMarkdownAnchors, czyli
// dokladnie ta sama metoda, ktora wpisuje odsylacze do spisu tresci.  Wlasnej
// implementacji specyfikacji tu NIE MA i byc nie moze: kotwica policzona
// inaczej niz w spisie tresci prowadzilaby po eksporcie w nicosc.
// Skrot Control+Shift+9 (szkielet linku do recznego wypelnienia) ZOSTAJE -
// to inna droga dla kogos, kto adres ma juz w schowku, a nie w okienku.
menuMiscInsertLink = CreateMenuItem("Insert &Link ...", "Control+K", menuItem_Click, "child silent");
// Text Contents (Alt+Shift+T) USUNIETA na polecenie Kasperczaka
// (26.08.2026, zlecenie -8) razem z Go to Section i Go to Contents.
// Wpisywala na poczatek dokumentu spis "tematow" rozpoznawanych po znaku
// Form Feed - w pliku Markdown dawala spis z JEDNEJ pozycji (zmierzone na
// jego liscie testow: 15 naglowkow, spis 1 pozycja), a przy tym PISALA
// do dokumentu, tak jak usunieta wczesniej komenda Format Code.
// Skrot Alt+Shift+T dostal 27.08.2026 nowa komende: Table of Contents,
// czyli markdownowy spis tresci (zlecenie 1787793592380-0).  Kasperczak
// zdecydowal, ze klawisz zostaje ten sam co w oryginale, a funkcja jest
// nowa: spis obejmuje WSZYSTKIE poziomy naglowkow ("nigdy nie wiadomo,
// jaka kto tak naprawde zastosuje hierarchie"), a pozycje sa LINKAMI
// WEWNETRZNYMI, ktore dzialaja takze po eksporcie - w Markdown, w HTML
// i w Wordzie, gdzie pandoc zamienia kotwice naglowkow na zakladki.
menuMiscTableOfContents = CreateMenuItem("Table of Contents", "Alt+Shift+T", menuItem_Click, "child speak");
// PRZYPISY.  Pierwotnie rodzina F6 (zlecenie 1787832545532-3, ustalenia
// Kasperczaka z rozmowy 27.08.2026 13:51-14:04), od 30.08.2026 rodzina K -
// powod i cytaty przy samych klawiszach nizej.  Wstawianie i liste przenosi
// jego wlasna zasada "Ctrl wstawia, Alt pokazuje liste"; kontekstowy skok
// zostaje na Alt+F6 do jego rozstrzygniecia.
// PRZENIESIENIE Z RODZINY F6 DO RODZINY K (rozmowa 29.08.2026 22:40-23:09).
// On sam ustalil zasade nadrzedna: "Alt literki to byly pewne listy a z Ctrl
// sie wstawiala reszte".  Stad przypisy ida tam, gdzie ich rodzenstwo:
//   Control+K        wstawia link      (juz jest, od 5.0.45),
//   Control+Shift+K  wstawia przypis,
//   Alt+K            pokazuje liste przypisow.
// Powod przeniesienia jest KONKRETNY, nie porzadkowy: Control+F6 ma dostac
// LISTE LINKOW ("to CTRL-F6 linki fajne"), a nie moze jej dostac, dopoki
// siedzi na nim wstawianie przypisu.  Kolejnosc ma wiec znaczenie.
// Chordy sprawdzone jako WOLNE w OBU miejscach, nie tylko w Hotkeys.ini:
// zero trafien na Control+Shift+K i Alt+K w tablicy skrotow ORAZ zero
// wystapien Keys.K w ProcessCmdKey_Helper, gdzie goly F9 i Shift+F9 zabieraja
// zdarzenie przed tablica (ta pulapka dala juz raz martwy klawisz przy
// zielonym buildzie).  Stary wpis "Go to Bookmark=Alt+K" w EdSharp.ini jest
// MARTWY: sekcja [Keys] nie jest juz czytana, klawisze sa w kodzie.
menuMiscInsertFootnote = CreateMenuItem("Insert Footnote ...", "Control+Shift+K", menuItem_Click, "child silent");
// SKOK KONTEKSTOWY: ODPOWIEDZ PRZYSZLA, chord przeniesiony (30.08.2026 10:01).
// Pytalismy, gdzie ma zamieszkac skok znacznik-tresc-znacznik; odpowiedzial
// "Skok przypis tekst tekst przypis robimy Alt-CTRL-ka to bedzie lepsza
// komenda chyba co? (...) Tak z ego F6 w przypisach bysmy rezygnowali".
// Czyli cala rodzina F6 wychodzi z przypisow, a skok dolacza do rodziny K,
// gdzie siedzi jego rodzenstwo (Control+Shift+K wstawia, Alt+K daje liste).
//
// UWAGA, PULAPKA ZMIERZONA, NIE ZGADNIETA - i to jest powod, dla ktorego ta
// jedna linia nie wystarczyla: Util.Say ma twardy warunek (l.17711), ktory
// TLUMI KAZDA MOWE, gdy uzytkownik trzyma Control+Alt.  Komenda przeniesiona
// tu bez dalszej zmiany WYKONALA BY skok, ale niewidomy NIE USLYSZALBY nic -
// czyli dla niego "klawisz nie dziala".  Zmierzone harnessem na zywych
// modyfikatorach (/mnt/c/tmp/ctrlaltk/h.cs, 6/6): Say zwykly przy Ctrl+Alt
// zwraca false, Say w trybie globalnym zwraca true, a sam Alt (dzisiejsze
// Alt+F6) nie tlumi wcale.  Dlatego wszystkie wypowiedzi tej komendy ida
// przez tryb GLOBALNY - tak samo jak przesuwanie sekcji na Control+Alt+Up.
//
// Chord sprawdzony jako WOLNY w OBU miejscach, nie tylko w tablicy skrotow:
// zero trafien na Control+Alt+K w CreateMenuItem oraz Keys.K w
// ProcessCmdKey_Helper wystepuje TYLKO w podgladzie, ktory wychodzi wczesniej
// przy wcisnietym Control albo Alt.  Litera K jest tez bezpieczna na polskiej
// klawiaturze: AltGr+K nie daje zadnego diakrytyku.
menuMiscGoToFootnote = CreateMenuItem("Go to Footnote", "Control+Alt+K", menuItem_Click, "child silent");
// SKOK PO PRZYPISACH ZSZEDL Z TEGO KLAWISZA (jego decyzja 01.09.2026, dwie
// wiadomosci pod rzad).  Wersja 5.0.57, na jego wlasna prosbe z 31.08 21:25,
// miala oba zachowania na jednym chordzie; po probie na zywym programie
// odrzucil to sam: "jestem w tej tresci przypisu, potem jestem na dole, potem
// znowu wchodze w kolejny przypis jest takie troche mylace (...) Dlatego z
// tego bysmy zrezygnowali", oraz "Skakanie po przypisach moze byc
// Alt+Ctrl+PageUp/PageDown, to nawet nie o to chodzi, ze Alt+Ctrl+K myli,
// tylko po prostu jest za duzo klikania".
//
// CO Z TEGO WYNIKA W KODZIE: Control+Alt+K jest znowu WYLACZNIE
// przelacznikiem miedzy znacznikiem i trescia przypisu, jak Shift+F6 miedzy
// spisem tresci i tekstem.  Gdy kursor nie stoi ani przy znaczniku, ani w
// tresci, komenda MOWI, czego brakuje, i nie rusza kursora - to nie slepy
// zaulek, bo skok po przypisach ma teraz wlasna pare klawiszy.
//
// SKOK PO PRZYPISACH: Control+Alt+PageDown i Control+Alt+PageUp - jego wybor
// klawiszy, podany doslownie.  Control+Alt z klawiszem NAWIGACYJNYM jest na
// polskim ukladzie bezpieczny (AltGr robi diakrytyki tylko na literach), a
// straznik pisania i tak to sprawdza przy budowie menu.  Gole
// Shift+PageUp/PageDown zostaje przy zakladkach, a Control+PageUp/PageDown
// przy sekcjach - zaden istniejacy chord nie zmienia wlasciciela.
// Control+Alt+Shift+K, ktory nosil skok do poprzedniego przypisu w 5.0.57,
// zostaje ZWOLNIONY i nie nalezy do zadnej komendy.
menuMiscNextFootnote = CreateMenuItem("Next Footnote", "Control+Alt+PageDown", menuItem_Click, "child silent");
menuMiscPriorFootnote = CreateMenuItem("Prior Footnote", "Control+Alt+PageUp", menuItem_Click, "child silent");
menuMiscFootnoteList = CreateMenuItem("Footnote List ...", "Alt+K", menuItem_Click, "child silent");
// EKSPORT PRZYPISOW - jego wiadomosc 31.08.2026 21:21: "To moze dwie opcje do
// wyboru.  Eksportuj przypisy, eksportuj przypisy ze zdaniami i zalatwi to
// sprawe".  Dwie opcje sa w JEDNYM okienku, tak jak przy wstawianiu przypisu
// (tam tez wybor miejsca tresci stoi w oknie z trescia, a nie w drugim oknie
// po zatwierdzeniu): to jedna decyzja o jednym eksporcie.
//
// BEZ SKROTU, SWIADOMIE: rodzina K jest pelna (Control+K, Control+Shift+K,
// Alt+K, Control+Alt+K, Control+Alt+Shift+K, Alt+Shift+K zajete przez usuwanie
// powtorzen), a eksportu uzywa sie raz na dokument, nie raz na akapit.  Jego
// zasada "najpierw skroty, dopoki starcza miejsca" jest tu zachowana wlasnie
// przez to, ze miejsca w tej rodzinie juz nie ma.  Doklejenie chordu pozniej
// jest tanie; odebranie zajetego nie jest.
menuMiscExportFootnotes = CreateMenuItem("Export Footnotes ...", "", menuItem_Click, "child silent");
// KOMENTARZE WEWNETRZNE (punkt 4 mapy drogowej).  Klawisze W RODZINIE F9, bo
// to on sam ja na to przeznaczyl - Telegram 27.08.2026: "szkoda mi tego F9,
// wolalbym na komentarze albo inne funkcje typu Tlumaczenie zostawic".
//   Alt+F9           wstawia komentarz albo poprawia ten pod kursorem,
//   Control+Alt+F9   pokazuje liste.
//
// GOLY F9 I SHIFT+F9 SA WOLNE OD 5.0.63 - to zmiana wobec tego, co stalo tu
// wczesniej.  Do 5.0.62 przechwytywal je WPROST warunek w
// ProcessCmdKey_Helper, PRZED tablica skrotow menu, i obslugiwal czytanie do
// konca przez skrypt JAWS; sonda na samym Hotkeys.ini pokazywala je wtedy
// falszywie jako wolne, a objawem bylo, ze klawisz nie robi NIC.  On sam z
// tego zrezygnowal (03.09.2026), warunek zszedl i chordy nie naleza teraz do
// zadnej komendy.  Zajecie ich jest odtad zwyklym dopisaniem pozycji menu.
//
// SKOKI PO KOMENTARZACH WYSZLY Z RODZINY F9 (ustalenie edsharpng-36, jego
// decyzja z 30.08.2026: "skok po komentarzach na Alt+Shift+PageUp i
// Alt+Shift+PageDown").  Powod jest jego: skoki po elementach dokumentu maja
// mieszkac w JEDNEJ rodzinie PageUp/PageDown, a nie kazdy przy swojej funkcji.
// Wstawianie i lista ZOSTAJA w rodzinie F9, bo to nie skoki - Alt+F9 pisze do
// dokumentu, Control+Alt+F9 otwiera okno.
// ZWOLNIONE tym samym ruchem: Control+Shift+F9 i Alt+Shift+F9.  Oba zmierzone
// jako nienalezace juz do zadnej komendy - chord zwolniony, ktory zostaje w
// CreateMenuItem, dawal by modalny alert na starcie i po cichu zabijal komende.
menuMiscInsertComment = CreateMenuItem("Insert Comment ...", "Alt+F9", menuItem_Click, "child silent");
menuMiscNextComment = CreateMenuItem("Next Comment", "Alt+Shift+PageDown", menuItem_Click, "child silent");
menuMiscPriorComment = CreateMenuItem("Prior Comment", "Alt+Shift+PageUp", menuItem_Click, "child silent");
menuMiscCommentList = CreateMenuItem("Comment List ...", "Control+Alt+F9", menuItem_Click, "child silent");
// DWIE KOMENDY WYRAZEN REGULARNYCH POLACZONE W JEDNA (jego decyzja 03.09.2026,
// wariant B: "Lacze w JEDNA komende, a w okienku wybierasz, co ma zrobic").
// Yield liczyl trafienia, Extract wypisywal je do nowego okna - obie pytaly o to
// samo wyrazenie i roznily sie tylko tym, co robia z wynikiem.  Teraz jest jedna
// pozycja menu na Control+Shift+Y, ktora najpierw pyta o dzialanie, potem o
// wzorzec.  Extract with Regular Expression zniklo calkiem: pole klasy, pozycja
// menu, wpis w AddRange, obsluga i opisy mowione w trzech plikach.
menuMiscRegExpTool = CreateMenuItem("Regular Expression Tool ...", "Control+Shift+Y", menuItem_Click, "child silent");
menuMiscRunAtCursor = CreateMenuItem("Run at Cursor ...", "Shift+F5", menuItem_Click, "child silent");
menuMiscSpecialCharacter = CreateMenuItem("Special Character ...", "F2", menuItem_Click, "child silent");
menuMiscEvaluateExpression = CreateMenuItem("Evaluate Expression", "Control+Oemplus", menuItem_Click, "child speak");
menuMiscReplaceTokens = CreateMenuItem("Replace Tokens", "Control+Shift+Oemplus", menuItem_Click, "child silent");
menuMiscTransformFiles = CreateMenuItem("Transform Files", "Alt+Oemplus", menuItem_Click, "child speak");
// SKROT ZDJETY, FUNKCJA ZOSTAJE W MENU (jego decyzja 01.09.2026): "To chyba
// tez w jakichs narzedziach powinno byc, prawda, jezeli w ogole bysmy to
// chcieli, na pewno ten skr..." - zdanie urwalo sie w pliku, ktory przyslal,
// ale kierunek jest jednoznaczny i dotyczy SKROTU, nie funkcji.  Uruchomienie
// srodowiska kompilatora zostaje wiec dostepne z menu.
menuMiscGoToEnvironment = CreateMenuItem("Go to Environment", "", menuItem_Click, "frame speak");
menuMiscCompile = CreateMenuItem("Compile", "Control+F5", menuItem_Click, "child speak");
menuMiscPickCompiler = CreateMenuItem("Pick Compiler", "Control+Shift+F5", menuItem_Click, "frame silent");
menuMiscPromptCommand = CreateMenuItem("Prompt Command", "Alt+F5", menuItem_Click, "child silent");
menuMiscReviewOutput = CreateMenuItem("Review Output", "Alt+Shift+F5", menuItem_Click, "child speak");
menuMiscSaveSnippet = CreateMenuItem("Save Snippet", "Alt+S", menuItem_Click, "child speak");
menuMiscInvokeSnippet = CreateMenuItem("Invoke Snippet", "Alt+V", menuItem_Click, "child speak");
menuMiscViewSnippet = CreateMenuItem("View Snippet", "Alt+Shift+V", menuItem_Click, "frame speak");
menuMiscKeepUniqueItems = CreateMenuItem("Keep Unique Items", "Alt+Shift+K", menuItem_Click, "child speak");
menuMiscNumberItems = CreateMenuItem("Number Items ...", "Alt+Shift+N", menuItem_Click, "child silent");
menuMiscOrderItems = CreateMenuItem("Order Items", "Alt+Shift+O", menuItem_Click, "child speak");
menuMiscReverseItems = CreateMenuItem("Reverse Items", "Alt+Shift+Z", menuItem_Click, "child speak");
// PRZENIESIONE Z Alt+Shift+L NA Alt+Shift+G (Kasperczak, 31.08.2026, ustalenie
// edsharpng-55).  Poprosil o Alt+Shift+L dla dodawania do ulubionych, pisząc
// "nie wiem do konca co on robi" - a robil wlasnie to porownanie dwoch list.
// Chord byl WIEC ZAJETY, nie wolny.  Alt+Shift+G zwolnilo sie po usunieciu
// nawigacji po czesciach (Go to Part) 27.08.2026 i jest w tej samej rodzinie
// liter co reszta komend porownywania list.
menuMiscListDifferentItems = CreateMenuItem("List Different Items", "Alt+Shift+G", menuItem_Click, "child speak");
menuMiscQueryCommonItems = CreateMenuItem("Query Common Items", "Alt+Shift+Q", menuItem_Click, "child speak");
menuMiscExplorerFolder = CreateMenuItem("Explorer Folder", "Alt+Oem5", menuItem_Click, "frame speak");
menuMiscCommandPrompt = CreateMenuItem("Command Prompt", "Control+Oem5", menuItem_Click, "frame speak");
// NAGRYWANIE PLYT USUNIETE (jego polecenie z 30.08.2026 10:05, doslownie:
// "ALT+SHIFT+B nagrywanie plikow to na pewno usun moze kiedys to wymyslimy
// jakos inaczej").  "Na pewno" znaczy, ze o to nie dopytuje.
//
// ZWOLNIONY CHORD MA JUZ PRZEZNACZENIE, wiec kolejnosc prac ma sens: Alt+Shift+B
// to w jego ukladzie LISTA ZAKLADEK Z NAZWA (ustalenie z 29.08 23:08, "to dwie
// osobne listy zakladek i zakladek z nazwami").  Zdjecie nagrywania robi na to
// miejsce.
//
// POMIAR PRZY OKAZJI, ktorego nie zamawial: instalator NIGDY nie pakowal
// Burn2CD.exe - w EdSharp_Setup.iss nie ma ani jednego wpisu o tym pliku, a
// komenda uruchamiala go z katalogu programu.  U kazdego, kto zainstalowal
// EdSharpNG z naszej paczki, ta funkcja byla wiec MARTWA od poczatku: pytala o
// filtr rozszerzen, a potem nie robila nic.  Usuwamy funkcje, ktora i tak nie
// dzialala, nie funkcje dzialajaca.
// Pliki Burn2CD.exe i Burn2CD.dll zostaja w repozytorium: sa dziedzictwem
// wersji autora, nie sa przez nic wolane, a ich usuniecie z historii to osobna
// decyzja o zawartosci repo, nie o zachowaniu programu.
menuMiscWebDownload = CreateMenuItem("Web Download", "Alt+Shift+W", menuItem_Click, "frame speak");
menuMiscWebClientUtilities = CreateMenuItem("Web Client Utilities", "Alt+Shift+Space", menuItem_Click, "frame speak");
menuMisc.DropDownItems.AddRange(new ToolStripItem[] {menuMiscSetDefaultFont, menuMiscConfigurationOptions, menuMiscManualOptions, menuMiscResetConfiguration, menuMiscGoToFolder, menuMiscGoToSpecialFolder, menuMiscWordWrap, menuMiscUnwrap, menuMiscExtraSpeechLog, menuMiscEnvironmentVariables, menuMiscSpellCheck, menuMiscSpellingWordMenu, menuMiscThesaurus, menuMiscLookupTerm, menuMiscTranslateLanguage, menuMiscGuardDocument, menuMiscPyBrace, menuMiscPyDent, menuMiscInferIndent, menuMiscRepeatLine, menuMiscSectionBreak, menuMiscPathToClipboard, menuMiscPathList, menuMiscInsertTime, menuMiscPreviewMarkdownBrowser, menuMiscTextCombine, menuMiscInsertTable, menuMiscCsvTable, menuMiscBulletList, menuMiscNumberedList, menuMiscInsertLink, menuMiscTableOfContents, menuMiscInsertFootnote, menuMiscGoToFootnote, menuMiscNextFootnote, menuMiscPriorFootnote, menuMiscFootnoteList, menuMiscExportFootnotes, menuMiscInsertComment, menuMiscNextComment, menuMiscPriorComment, menuMiscCommentList, menuMiscRegExpTool, menuMiscRunAtCursor, menuMiscSpecialCharacter, menuMiscEvaluateExpression, menuMiscReplaceTokens, menuMiscTransformFiles, menuMiscGoToEnvironment, menuMiscCompile, menuMiscPickCompiler, menuMiscPromptCommand, menuMiscReviewOutput, menuMiscSaveSnippet, menuMiscInvokeSnippet, menuMiscViewSnippet, menuMiscKeepUniqueItems, menuMiscNumberItems, menuMiscOrderItems, menuMiscReverseItems, menuMiscListDifferentItems, menuMiscQueryCommonItems, menuMiscExplorerFolder, menuMiscCommandPrompt, menuMiscWebDownload, menuMiscWebClientUtilities});
//Dialog.Show("Misc.", menuMisc.DropDownItems.Count);

menuWindow = CreateMenu("&Window");
menuWindowNext = CreateMenuItem("Next Window", "Control+Tab", menuItem_Click, "child speak");
menuWindowPrior = CreateMenuItem("Prior Window", "Control+Shift+Tab", menuItem_Click, "child speak");
menuWindowArrangeIcons = CreateMenuItem("Arrange Icons", "Alt+F11", menuItem_Click, "child speak");
menuWindowCascade = CreateMenuItem("Cascade", "Control+F11", menuItem_Click, "child speak");
menuWindowTileHorizontal = CreateMenuItem("Tile Horizontal", "Alt+Shift+F11", menuItem_Click, "child speak");
menuWindowTileVertical = CreateMenuItem("Tile Vertical", "Control+Shift+F11", menuItem_Click, "child speak");
menuWindow.DropDownItems.AddRange(new ToolStripMenuItem[] {menuWindowNext, menuWindowPrior, menuWindowArrangeIcons, menuWindowCascade, menuWindowTileHorizontal, menuWindowTileVertical});
//Dialog.Show("Window.", menuWindow.DropDownItems.Count);

menuHelp = CreateMenu("&Help");
menuHelpAbout = CreateMenuItem("&About ...", "Alt+F1", menuItem_Click, "frame silent");
menuHelpDocumentation = CreateMenuItem("Documentation", "F1", menuItem_Click, "frame speak");
menuHelpTutorial = CreateMenuItem("Tutorial", "Control+Shift+F1", menuItem_Click, "frame speak");
menuHelpHistoryOfChanges = CreateMenuItem("History of Changes", "Shift+F1", menuItem_Click, "frame speak");
menuHelpKeyDescriber = CreateMenuItem("Key Describer", "Control+F1", menuItem_Click, "frame silent");
menuHelpHotKeySummary = CreateMenuItem("Hotkey Summary", "Alt+Shift+H", menuItem_Click, "frame speak");
menuHelpAlternateMenu= CreateMenuItem("Alternate Menu ...", "Alt+F10", menuItem_Click, "frame silent");
menuHelpContextMenu= CreateMenuItem("Context Menu ...", "Shift+F10", menuItem_Click, "child silent");
menuHelpSendToMenu= CreateMenuItem("SendTo Menu ...", "Control+F10", menuItem_Click, "child silent");
menuHelpElevateVersion = CreateMenuItem("Elevate Version", "F11", menuItem_Click, "frame speak");
// ZGLOSZENIE PROBLEMU JAKO POZYCJA MENU POMOC (dolozone 11.09.2026).  Wzorzec z
// czytnikow ekranu: droga od "cos nie dziala" do zgloszenia ma byc w programie,
// a nie w cudzej stronie internetowej, bo tester nie ma czym jej znalezc.
// Skrot Alt+Shift+F1 - jest wolny (sprawdzone na liscie wszystkich skrotow) i
// stoi obok Alt+F1 (About), gdzie uzytkownik szuka rzeczy o samym programie.
menuHelpReinstall = CreateMenuItem("Reinstall Current Version", "", menuItem_Click, "frame speak");
// SKLADNIKI KONWERSJI NA ZADANIE (5.0.85, zadanie 8).  Przy starcie program
// dociaga tylko to, czego BRAKUJE; ta pozycja aktualizuje takze narzedzia,
// ktore sa, ale sa przedawnione.  Swiadomie BEZ skrotu klawiszowego: to
// czynnosc raz na kilka miesiecy, a kazdy nowy skrot to ryzyko kolizji.
menuHelpUpdateComponents = CreateMenuItem("Update Components", "", menuItem_Click, "frame speak");
menuHelpReportProblem = CreateMenuItem("Report a Problem ...", "Alt+Shift+F1", menuItem_Click, "frame silent");
// PALETA POLECEN JAKO POZYCJA MENU (11.09.2026).  Skrot Control+Shift+X:
// Control+Shift+P, ktory sam zaproponowalem, jest ZAJETY przez Path List
// (EdSharp.cs oraz Hotkeys.ini) - sprawdzone przed przypisaniem.  X jest
// wolne w kodzie i w Hotkeys.ini.  Swiadomie NIE Control+Alt+litera: prawy
// Alt w Windows to Ctrl+Alt, wiec takie skroty zjadaja polskie znaki.
menuHelpCommandPalette = CreateMenuItem("Command Palette ...", "Control+Shift+X", menuItem_Click, "frame silent");
menuHelp.DropDownItems.AddRange(new ToolStripItem[] {menuHelpAbout, menuHelpDocumentation, menuHelpTutorial, menuHelpHistoryOfChanges, menuHelpKeyDescriber, menuHelpHotKeySummary, menuHelpAlternateMenu, menuHelpContextMenu, menuHelpSendToMenu, menuHelpElevateVersion, menuHelpReinstall, menuHelpUpdateComponents, menuHelpReportProblem, menuHelpCommandPalette});
//Dialog.Show("Help.", menuHelp.DropDownItems.Count);

menuMain.Items.AddRange(new ToolStripItem[] {menuFile, menuEdit, menuDelete, menuNavigate, menuQuery, menuMisc, menuWindow, menuHelp});
//menuMain.Items.AddRange(new ToolStripItem[] {menuFile, menuEdit, menuDelete, menuNavigate, menuQuery, menuMisc, menuHelp});
statusBar = CreateStatusBar();
// this.Controls.AddRange(new Control[] {menuMain, statusBar});
this.Controls.AddRange(new Control[] {statusBar, menuMain});
this.MainMenuStrip = menuMain;
menuMain.MdiWindowListItem = menuWindow;
//this.AutoSize = true;
this.Size = new Size(600, 600);
this.StartPosition = FormStartPosition.CenterScreen;
this.Text = "EdSharpNG";
this.ResumeLayout();
this.KeyPreview = true;
//this.MdiChildActivate += delegate(object o, EventArgs e) {this.Child = (MdiChild) this.ActiveMdiChild;};
// When the app regains activation (e.g. via Alt+Tab), Windows/MDI often
// leaves keyboard focus on the child form or MDI client rather than the
// actual edit control, so the screen reader loses the caret until the user
// re-enters the window or toggles preview (double Escape). Force focus back
// onto the active child's real edit control.
this.Activated += delegate(object o, EventArgs e) {
FocusChildEditControl();
};
// FOKUS PO ZAMKNIECIU MENU (zgloszenie Kasperczaka: po wyjsciu z menu
// Escape'em fokus nie zawsze wraca wprost do pola edycyjnego, tak jak przy
// Alt+Tab).  MenuStrip oddaje fokus temu, co bylo aktywne PRZED otwarciem
// menu, a w oknie MDI bywa to formatka dziecka albo klient MDI, nie sama
// kontrolka edycyjna - czytnik traci wtedy karetke i trzeba go budzic
// podwojnym Escape.  Ten sam nosnik, ktory naprawia Alt+Tab, obsluguje
// wiec takze zamkniecie menu.
menuMain.MenuDeactivate += delegate(object o, EventArgs e) {
FocusChildEditControl();
};
string s = App.ReadOption("MaximizeWindow", "N").Trim().ToUpper();
if (s == "Y" || s == "YES") this.Shown += delegate(object o, EventArgs e) {
this.WindowState = FormWindowState.Maximized;
this.Activate();
Win32.SetForegroundWindow(this.Handle);
};
this.Shown += delegate(object o, EventArgs e) {
Util.ActivateTitle(this.Text);
};

string sDir = Directory.GetCurrentDirectory();
string sFile = Path.Combine(App.DataDir, App.ReadData("Compiler", "Default") + ".ini");
s = Ini.ReadValue(sFile, "Data", "Directory", "");
if (Directory.Exists(s) && !Util.Equiv(sDir, s)) {
//Dialog.Show(sDir, s);
//Directory.SetCurrentDirectory(s);
AddMessage("Folder " + Path.GetFileName(s));
Directory.SetCurrentDirectory(s);
}

} // MdiFrame constructor

public void SetStatus(object o) {
string sText = o.ToString();
this.statusBar.Items[0].Text = sText;
} // SetStatus method

public string GetNoNameTitle() {
object[] children = this.MdiChildren;
List<int> list = new List<int>();
foreach (object o in children) {
MdiChild child = (MdiChild) o;
string sTitle = child.Text;
if (sTitle.StartsWith("NoName") && Path.GetExtension(sTitle).Length == 0) {
string s = sTitle.Substring(6);
int i = Int32.Parse(s);
list.Add(i);
}
}

int iTitle = 0;
for (int i = 1; i <= children.Length; i++) {
if (!list.Contains(i)) {
iTitle = i;
break;
}
}

if (iTitle == 0) iTitle = children.Length + 1;
string sReturn = "NoName" + iTitle.ToString();
return sReturn;
} // GetNoNameTitle

// PONAWIANIE MA DWA KLAWISZE: Control+Shift+Z i Control+Y.
//
// DLACZEGO NIE PRZEZ CreateMenuItem: tablica skrotow (hashKey) trzyma JEDEN
// chord na komende i przy drugim wpisie tej samej komendy zglosilaby duplikat
// przy starcie programu.  Drugi klawisz obslugujemy wiec tutaj, w lancuchu
// ProcessCmdKey_Helper, PRZED tablica - i wolamy DOKLADNIE te sama pozycje
// menu, wiec oba klawisze robia to samo co do znaku, razem z mowa.
//
// DLACZEGO W OGOLE: Control+Shift+Z ZOSTAJE (ustalenie edsharpng-72, to
// standard Windows obok Control+Z), a Control+Y w wiekszosci edytorow ponawia
// i on poprosil o to wprost: "CTRL-y ponawianie proponuje" (01.09.2026).
// Repeat Line, ktore siedzialo na tym klawiszu, zeszlo do menu bez skrotu.
private bool HandleRedoAliasKey(Keys keyData) {
if (keyData != (Keys.Control | Keys.Y)) return false;
if (menuEditRedo == null) return false;
// Pozycja menu jest wlaczona tylko wtedy, gdy jest otwarte okno edycji;
// bez tego sprawdzenia klawisz w pustej ramce wolalby handler bez dokumentu.
if (this.Child == null) return false;
this.bCommandComplete = false;
menuEditRedo.PerformClick();
this.bCommandComplete = true;
return true;
} // HandleRedoAliasKey method

public bool ProcessCmdKey_Helper(ref Message msg, Keys keyData) {
string sKey = keyData.ToString();
int iIndex = -1;
if (this.Child != null) iIndex = this.Child.RTB.Index;
if (sKey.StartsWith("Menu,") || sKey.StartsWith("ControlKey,") || sKey.StartsWith("ShiftKey,")) sKey = "";
else if (iIndex == this.KeyIndex && sKey == this.KeyString) this.KeyRepeat += 1;
else this.KeyRepeat = 0;
if (sKey.Length > 0) {
this.KeyString = sKey;
this.KeyIndex = iIndex;
}
// Util.Say(keyData.ToString());
//Clipboard.SetText(Clipboard.GetText() + keyData.ToString() + "\r\n");
// Util.Say("Repeat " + this.KeyRepeat);

if (HandleFileSlotKey(keyData)) return true;
if (HandleSpellingWordMenuKey(keyData)) return true;
if (HandleWindowNumberKey(keyData)) return true;
if (HandleCloseWindowKey(keyData)) return true;
if (HandleMdiWindowCycleKey(keyData)) return true;
if (HandleSectionMoveKey(keyData)) return true;
if (HandleRedoAliasKey(keyData)) return true;
// Markdown review (preview) mode: Escape toggles it on .md files; while
// active, navigation/elements-list keys are handled here, and arrow keys
// arm the heading announcement. Checked before the normal editor keys.
{
MdiChild reviewChild = this.Child;
if (reviewChild != null && reviewChild.MarkdownReviewMode) {
Keys reviewKeyCode = keyData & Keys.KeyCode;
if (reviewKeyCode == Keys.Up || reviewKeyCode == Keys.Down || reviewKeyCode == Keys.Left || reviewKeyCode == Keys.Right) reviewChild.MarkdownReviewAnnounceHeading = true;
}
}
if (HandleMarkdownReviewKey(keyData)) return true;
if (ShouldLetMarkdownReviewViewHandleNativeNavigation(keyData)) return false;
if (HandleEditorFormattingKey(keyData)) return true;
ToolStripMenuItem menuItem;
// GOLY F9 I SHIFT+F9 ZWOLNIONE (jego decyzja 03.09.2026 01:42: "Z tego
// klawisza F dziewiec i skryptu raczej rezygnujemy i tak nie bede go
// testowac z programem").
//
// CO TU STALO DO 5.0.62 i dlaczego to bylo warte usuniecia, a nie zostawienia:
// goly F9 zrzucal tekst od kursora do konca dokumentu do pliku tymczasowego
// i wolal po COM funkcje skryptu JAWS SayAllTempFile, ktora wkladala ten
// tekst do wirtualnego bufora JAWS-a i czytala go do konca; Shift+F9 czytal
// z tego samego pliku LICZBE zapisana tam przez skrypt i przesuwal kursor o
// tyle znakow, zeby po przerwanym czytaniu kursor stanal tam, gdzie mowa
// ucichla.  Bez zainstalowanych skryptow JAWS (a instalator ich nie stawia,
// ustalenie edsharpng-27) oba klawisze robily dokladnie NIC: pierwszy pisal
// plik tymczasowy, ktorego nikt nie czytal, a drugi przesuwal kursor o
// liczbe z pliku po poprzednim czytaniu albo wychodzil na niepustej tresci.
// Cichy klawisz jest dla niewidomego gorszy od klawisza wolnego: przy
// wolnym slychac, ze nic sie nie stalo, przy cichym nie wiadomo, czy komenda
// nie zadzialala, czy zadzialala bez mowy.
//
// TE DWA CHORDY SA TERAZ WOLNE i nie naleza do zadnej komendy - takze nie do
// komentarzy, ktore ta rodzine kiedys dostaly (Alt+F9 wstawianie,
// Control+Alt+F9 lista ZOSTAJA nietkniete).  Zajecie ich czymkolwiek jest
// odtad zwyklym dopisaniem pozycji menu; wczesniej wymagaloby zdjecia
// warunku STAD, bo ten warunek stal PRZED tablica skrotow menu i zabieral
// zdarzenie (ta pulapka dala raz martwy klawisz przy zielonym buildzie,
// lekcja z 5.0.43).
//
// SKRYPTY JAWS ZOSTAJA W PACZCE (scripts/EdSharp.JSS, funkcja SayAllTempFile
// i mapa klawiszy edsharp.jkm) - to jego pliki autora, ktore przy
// upublicznieniu programu wracaja do rozmowy jako calosc.  Zdjeta jest
// obsluga W PROGRAMIE, czyli jedyna czesc, ktora zajmowala klawisz.
if (keyData == (Keys.Insert)) {
// Util.Say("Insert key now");
return true;
}

else if (hashKey.TryGetValue(keyData, out menuItem)) {
//if (this.Child != null && !this.Child.RTB.IndentMode && menuItem == menuEditEnterNewLine) return base.ProcessCmdKey (ref msg, keyData);
this.bCommandComplete = false;
menuItem.PerformClick();
this.bCommandComplete = true;
return true;
}
else return base.ProcessCmdKey (ref msg, keyData);
} // ProcessCmdKey_Helper method

protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
return this.ProcessCmdKey_Helper(ref msg, keyData);
} // ProcessCmdKey handler

static MenuStrip CreateMainMenu() {
MenuStrip menuMain = new MenuStrip();
menuMain.AccessibleRole = AccessibleRole.MenuBar;
//menuMain.AutoSize = true;
//menuMain.CanOverflow = false;
menuMain.Dock = DockStyle.Top;
//menuMain.LayoutStyle = ToolStripLayoutStyle.Flow;
//menuMain.Stretch = false;
return menuMain;
} // CreateMainMenu method

static ToolStripMenuItem CreateMenu(string sText) {
ToolStripMenuItem menuItem = new ToolStripMenuItem(sText);
menuItem.AccessibleRole = AccessibleRole.MenuItem;
return menuItem;
} // CreateMenu method

static ToolStripMenuItem CreateMenuItem(string sText, string sKey, EventHandler eh) {
bool bFrame = false;
return CreateMenuItem(sText, sKey, eh, bFrame);
} // CreateMenuItem method

static ToolStripMenuItem CreateMenuItem(string sText, string sKey, EventHandler eh, bool bFrame) {
string sOptions = "";
if (bFrame) sOptions += "frame ";
if (sText.EndsWith(" ...") || sText.EndsWith("Again")) sOptions += "silent ";
return CreateMenuItem(sText, sKey, eh, sOptions);
}  // CreateMenuItem method

static ToolStripMenuItem CreateMenuItem(string sText, string sKey, EventHandler eh, string sOptions) {
ToolStripMenuItem menuItem = new ToolStripMenuItem(sText, null, eh);
menuItem.AccessibleRole = AccessibleRole.MenuItem;
menuItem.Tag = sOptions;

string sCommand = sText.Replace("&", "").Replace("...", "").Trim();
menuItem.Name = sCommand;
// Keybindings are defined directly in code, via the sKey argument. The
// former per-item [Keys] override read from EdSharp.ini has been removed:
// it cost an INI read for every menu item at startup, and runtime
// rebinding is no longer supported (defaults are chosen to be optimal).
sKey = sKey.Replace("&", "");
Keys keyData = Util.String2Key(sKey);
// Keys.None means a menu-only command: no shortcut text, and no entry in
// the hashKey table, where a second unbound item would otherwise alert as
// a duplicate of the first one at startup. KeyMap.register below still
// runs, since it models unbound commands already.
if (keyData == Keys.None) {
menuItem.AccessibleName = sText.Replace("&", "");
menuItem.Text = sText;
}
else if (hashKey.ContainsKey(keyData)) {
string s = hashKey[keyData].Name;
Dialog.Show("Alert", "Cannot assign " + sKey + " to " + sCommand + ",\nsince already assigned to " + s);
}
// STRAZNIK PISANIA: chord, ktorym uzytkownik WPISUJE znak, nie moze zostac
// skrotem.  Kasperczak zapytal o to wprost (30.08.2026): "Alt+Ctrl to tez to
// samo co prawy Alt, a prawy Alt to polska literka".  Na polskim ukladzie
// a z ogonkiem to Control+Alt+A, c z kreska to Control+Alt+C i tak dla
// dziewieciu liter - skrot na takim chordzie ZABIERA pisanie.
// DLACZEGO KOD, A NIE SAMA OSTROZNOSC PRZY WYBORZE KLAWISZA: zmierzone na
// zywym programie, ze przypisanie komendy do Control+Alt+E odbiera litere
// e z ogonkiem po cichu.  Build jest zielony, menu wyglada dobrze, alarm o
// duplikacie NIE pada (bo to nie kolizja z inna komenda), a niewidomy
// dowiaduje sie o tym dopiero, gdy nie moze napisac slowa.
// Pytamy UKLAD (VkKeyScanEx), nie liste liter na sztywno: uklad rozstrzyga i
// na klawiaturze bez polskich znakow ten straznik nie przeszkadza.
else if (Util.IsTypingChord(keyData)) {
Dialog.Show("Alert", "Cannot assign " + sKey + " to " + sCommand
+ ",\nsince this chord types a character on the current keyboard layout.");
}
else {
string sFriendlyKey = Util.GetFriendlyKeyName(sKey);
menuItem.ShortcutKeyDisplayString = sFriendlyKey;
menuItem.AccessibleName = sText.Replace("&", "") + "   " + sFriendlyKey;
menuItem.Text = sText;
hashKey.Add(keyData, menuItem);
}

// Register the command in the central KeyMap (Homer): command -> key,
// owning menu item, and UI context (the sOptions string, e.g. "frame
// speak"). Additive -- hashKey above is unchanged; KeyMap becomes the
// single table the status bar, Key Describer, and Alternate Menu read.
KeyMap.register(sCommand, keyData, menuItem, sOptions);

menuItem.Paint += delegate(object oSender, PaintEventArgs e) {
// if (!App.Frame.KeyDescriber) return;
foreach (ToolStripMenuItem menu in App.Frame.menuMain.Items) {
foreach (object o in menu.DropDownItems) {
ToolStripMenuItem item = o as ToolStripMenuItem;
if (item == null) continue;
if (!item.Selected) continue;
string[] aSummary = App.Frame.GetKeySummary(item);
string sSummary = aSummary[0] + " = " + aSummary[1] + ", " + aSummary[2];
string sDescription = aSummary[2];
if (sDescription != App.Frame.LastDescription) {
// System.Threading.Thread.Sleep(1000);
// Util.Say(sDescription);
App.Frame.SetStatus(sDescription);
App.Frame.LastDescription = sDescription;
}
break;
}
}
};

return menuItem;
} // CreateMenuItem method

static StatusStrip CreateStatusBar() {
StatusStrip sb = new StatusStrip();
sb.AccessibleRole = AccessibleRole.StatusBar;
sb.SuspendLayout();
ToolStripStatusLabel lblStatus = new ToolStripStatusLabel("Ready");
lblStatus.AutoSize = true;
sb.Items.AddRange(new ToolStripItem[] {lblStatus});
sb.AutoSize = true;
sb.Dock = DockStyle.Bottom;
sb.ResumeLayout();
return sb;
} // CreateStatusBar method

public string GetPercentAddress(HomerRichTextBox rtb) {
return String.Format("Line {0}   Column {1}   Percent{2}", rtb.Line, rtb.Column, rtb.Percent);
} // GetPercentAddress method

public string GetPageAddress(HomerRichTextBox rtb) {
string sText = rtb.Text;
int iIndex = rtb.Index;
sText = sText.Substring(0, iIndex);
int iPage = sText.Length - sText.Replace("\f", "").Length + 1;
iIndex = sText.LastIndexOf("\f");
if (iIndex >= 0) sText = sText.Substring(iIndex);
if (sText.StartsWith("\f")) sText = sText.Remove(0, 1);
int iLine = sText.Length - sText.Replace("\n", "").Length + 1;
iIndex = sText.LastIndexOf("\n");
int iColumn = sText.Length - iIndex;
return String.Format("Page {0}   Line {1}   Column {2}", iPage, iLine, iColumn);
} // GetPageAddress method

public void SetStatusAddress(object sender, EventArgs e) {
if (sender != null && !this.bCommandComplete) return;
if (this.Child == null) {
SetStatus("");
return;
}

HomerRichTextBox rtb = this.Child.RTB;
//string sText = String.Format("Line {0}\tColumn {1}\tPercent{2}", rtb.Line, rtb.Column, rtb.Percent);
string sText = "";
int iIndex = -1;
bool bPageAddress = true;
if (App.ReadOption("HardPageAddress", "N").ToLower().Substring(0, 1) != "y") bPageAddress = false;
if (bPageAddress) sText = GetPageAddress(rtb);
else  sText = GetPercentAddress(rtb);

iIndex = rtb.Index;
char c = ' ';
if (iIndex < rtb.TextLength) c = rtb.Text[iIndex];

int iNewIndex = rtb.Index;
int iNewTextLength = rtb.TextLength;
int iDelta = Math.Abs(iNewIndex - rtb.OldIndex);
if (!bPageAddress || iDelta != 1 || iNewTextLength != rtb.OldTextLength) {} // Do nothing
else if (c == '\f') Util.Say("FormFeed");
else if (c == '\n') Util.Say("LineFeed");
else if (c == '\t') Util.Say("TabChar");
rtb.OldIndex = iNewIndex;
rtb.OldTextLength = iNewTextLength;

if (sender == null) Util.Say(sText);
this.SetStatus(sText);

if (!rtb.IndentMode) return;
string sLine = rtb.RowText.Trim();
if (sLine.Length == 0) return;
string sComment = App.ReadOption("QuotePrefix", "> ");
if (sLine.StartsWith(sComment)) return;
int iLevels = GetIndent();
if (rtb.IndentLevels == iLevels) {
// Environment.SetEnvironmentVariable("EdSharpIndent", "", EnvironmentVariableTarget.User);
// Ini.WriteValue(App.IndentModeFile, "Data", "IndentChange", "", false);
System.IO.File.Create(App.IndentModeFile).Close();
return;}
string sDelta = GetDelta(rtb.IndentLevels, iLevels);
// Environment.SetEnvironmentVariable("EdSharpIndent", sDelta, EnvironmentVariableTarget.User);
Ini.WriteValue(App.IndentModeFile, "Data", "IndentChange", sDelta, false);
if (App.IndentChange) Util.Say(sDelta);
rtb.IndentLevels = iLevels;
} // SetStatusAddress method

public void AddMessage(object oText) {
bool bGlobal = false;
AddMessage(oText, bGlobal);
} // AddMessage method

public void AddMessage(object oText, bool bGlobal) {
string sText = oText.ToString();
Util.Say(sText, bGlobal);
if (App.CaptureOutput) Util.StringAppend2File(sText + "\r\n", App.TempFile);
//sText = this.statusBar.Items[0].Text + "\t" + sText;
// PASEK STANU BEZ WIODACYCH ODSTEPOW, gdy nic na nim nie stalo: przy opcji
// "quiet" nazwy komendy nie ma, wiec sklejanie dawaloby "   Added to
// favorites" - trzy spacje, ktore czytnik potrafi wypowiedziec jako pauze.
string sPrior = this.statusBar.Items[0].Text;
sText = (sPrior.Length == 0) ? sText : sPrior + "   " + sText;
SetStatus(sText);
} // AddMessage method

public void SetMessage(object oText) {
SetStatus(oText);
Util.Say(oText);
} // SetMessage method

public void GetRowAndCol(out int iRow, out int iCol) {
HomerRichTextBox rtb = this.Child.RTB;
int iIndex = rtb.SelectionStart + rtb.SelectionLength;
iRow = rtb.GetLineFromCharIndex(iIndex);
iCol = iIndex - rtb.GetFirstCharIndexOfCurrentLine();
} // GetRowAndCol method

bool IsEmptyWindow() {
return !(this.Child == null || this.Child.RTB.Modified || this.Child.RTB.TextLength > 0);
} // IsEmptyWindow method

public bool IsCharacter() {
int iIndex;
return IsCharacter(out iIndex);
} // IsCharacter method

public bool IsCharacter(out int iIndex) {
HomerRichTextBox rtb = this.Child.RTB;
iIndex = rtb.Index;
if (iIndex >= rtb.TextLength) {
AddMessage("No character at cursor!");
return false;
}
else return true;
} // IsCharacter method

public int GetIndent() {
return GetIndent(this.Child.RTB.Row);
} // GetIndent method

public int GetIndent(int iRow) {
string sIndent = App.ReadOption("IndentUnit", "  ");
sIndent = Util.Literalize(sIndent);
MdiChild child = this.Child;
HomerRichTextBox rtb = child.RTB;
string sLine = rtb.GetRowText(iRow);
int iLength = sIndent.Length;
int iLevels = 0;
while (sLine.StartsWith(sIndent)) {
iLevels++;
if (sLine.Length == iLength) sLine = "";
else sLine = sLine.Substring(iLength);
}
return iLevels;
} // GetIndent method

public string GetDelta(int iBefore, int iAfter) {
if (iBefore < iAfter) return "In " + (iAfter - iBefore);
else return "Out " + (iBefore - iAfter);
} // GetDelta method

public string GetStyleText() {
HomerRichTextBox rtb = this.Child.RTB;
string sText = "";
if (rtb.SelectionFont.Bold) sText += "Bold ";
if (rtb.SelectionFont.Italic) sText += "Italic ";
if (rtb.SelectionFont.Underline) sText += "Underline";
sText = sText.Trim();
if (sText.Length == 0) sText = "Regular";
return sText;
} // GetStyleText method

public string GetJustifyText() {
HomerRichTextBox rtb = this.Child.RTB;
string sText = "Left";
HorizontalAlignment ha = rtb.SelectionAlignment;
if (rtb.SelectionBullet) sText = "Bullet";
else if (ha == HorizontalAlignment.Center) sText = "Center";
else if (ha == HorizontalAlignment.Right) sText = "Right";
return sText;
} // GetJustifyText method

public string GetBaselineText() {
HomerRichTextBox rtb = this.Child.RTB;
string sText = "Flat";
int iOffset = rtb.SelectionCharOffset;
if (iOffset < 0) sText = "Down";
else if (iOffset > 0) sText = "Up";
return sText;
} // GetBaselineText method

public string GetFontText(Font font, Color color) {
string sFont = Util.Font2String(font);
string sColor = Util.Color2String(color);
sFont += ", Color=" + sColor;
return sFont;
} // GetFontText method

public string[] GetSnippetFiles(out string[] aValues) {
string sBaseDir = @"Snippets\" + App.ReadData("Compiler", "Default");
string sDir = Path.Combine(App.DataDir, sBaseDir);
if (!Directory.Exists(sDir)) Directory.CreateDirectory(sDir);
string[] aResults = Directory.GetFiles(sDir);

List<string> listResults = new List<string>(aResults);
List<string> listFiles = new List<string>();
foreach (string s in aResults) listFiles.Add(Path.GetFileName(s).ToLower());

sBaseDir = @"Snippets\Default";
sDir = Path.Combine(App.DataDir, sBaseDir);
if (!Directory.Exists(sDir)) Directory.CreateDirectory(sDir);
aResults = Directory.GetFiles(sDir);
foreach (string s in aResults) if (!listFiles.Contains(Path.GetFileName(s).ToLower())) listResults.Add(s);
aResults = listResults.ToArray();

aValues = new string[aResults.Length];
for (int i = 0; i < aResults.Length; i++) aValues[i] = Path.GetFileName(aResults[i]);
return aResults;
} // GetSnippetFiles method

public void GetDateAndTime(out string sDate, out string sTime) {
DateTime dt = DateTime.Now;
string sDateFormat = App.ReadOption("DateFormat", "");
string sTimeFormat = App.ReadOption("TimeFormat", "");
if (sDateFormat == "0") sDate = "";
else sDate = (sDateFormat.Length > 0) ? dt.ToString(sDateFormat) : dt.ToLongDateString();

if (sTimeFormat == "0") sTime = "";
else sTime = (sTimeFormat.Length > 0) ? dt.ToString(sTimeFormat) : dt.ToShortTimeString();
} // GetDateAndTime method

public string ReplaceTokens(string sText) {
string[] aTokens = App.ReadSectionKeys("Tokens");
foreach (string sToken in aTokens) {
if (sText.IndexOf(sToken) == -1) continue;
string s = App.ReadValue("Tokens", sToken, "");
string sFile = GetSnippetDir() + @"\" + s;
if (File.Exists(sFile)) s = Util.File2String(sFile);
//Dialog.Show(sFile, s);
string sResult = Script.run(s);
if (sResult == null) sResult = "";
sText = sText.Replace("%" + sToken + "%",sResult);
}

return sText;
} // ReplaceTokens method

public void TransFormFiles() {
// A transform job is now a Regexer-style .inix file: one [Section] per task,
// each with Find, Replace, Options, Extract, and Divider keys, and values that
// may span multiple lines. Replace is processed like Regexer (Regex.Unescape,
// so \n, \t, and \" work; $# substitutes the running match count); Extract
// collects the matched text to the clipboard. Options is a comma-separated list
// of .NET RegexOptions names (multiline, ignorecase, singleline, compiled, ...).
// The current document supplies the list of source files, one path per line.
if (File.Exists(App.TempFile)) File.Delete(App.TempFile);
App.CaptureOutput = true;
HomerRichTextBox rtb = this.Child.RTB;

string sJob = App.ReadData("Job", "");
string sTransformFile = Dialog.OpenFile("Open Job", sJob);
if (sTransformFile.Length == 0) { App.CaptureOutput = false; return; }
App.WriteData("Job", sTransformFile);

string sJobBody = Util.File2String(sTransformFile);
string[] aJobLines = sJobBody.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
List<InixCodec.Section> lsAll = InixCodec.parseLines(aJobLines);
List<InixCodec.Section> lsTasks = new List<InixCodec.Section>();
foreach (InixCodec.Section section in lsAll) {
if (section.get("Find") != null) lsTasks.Add(section);
}
if (lsTasks.Count == 0) {
Dialog.Show("Transform Files", "No tasks found.  A job is now an .inix file with one [Section] per task, each having Find, Replace, Options, Extract, and Divider keys.");
App.CaptureOutput = false;
return;
}

string[] aChoices = {"&Test", "&Run", "&Verbose"};
string sChoice = Dialog.Choose("Choose Mode", "", aChoices, 0);
if (sChoice.Length == 0) { App.CaptureOutput = false; return; }
bool bApply = (sChoice != "&Test");
bool bVerbose = (sChoice == "&Verbose");

string sSourceList = rtb.Text.Trim();
string[] aSourceLines = sSourceList.Split('\n');
string sDir = Directory.GetCurrentDirectory();
string sExtractText = "";
string sClipboardDivider = "\f\n";
int iExtractTotal = 0;

foreach (string sSourceLine in aSourceLines) {
string sSourceFile = sSourceLine.Trim();
if (sSourceFile.Length == 0) continue;
string sSourceDir = Path.GetDirectoryName(sSourceFile);
if (sSourceDir.Length > 0 && Directory.Exists(sSourceDir)) sDir = sSourceDir;
else sSourceFile = Path.Combine(sDir, Path.GetFileName(sSourceFile));
if (!File.Exists(sSourceFile)) continue;

AddMessage(Path.GetFileName(sSourceFile));
Encoding en = App.Frame.Child.GetYieldEncoding();
string sSourceBody = Util.File2String(sSourceFile, ref en);
bool bWasCrlf = sSourceBody.Contains("\r\n");
sSourceBody = sSourceBody.Replace("\r\n", "\n");
bool bChanged = false;

foreach (InixCodec.Section section in lsTasks) {
string sFind = section.get("Find");
if (sFind == null || sFind.Length == 0) continue;
string sReplace = section.get("Replace");
sReplace = Regex.Unescape(sReplace == null ? "" : sReplace);
RegexOptions options = Util.RegexOptionsFromString(section.get("Options"));
bool bExtract = Util.ToBool(section.get("Extract"));
string sDivider = section.get("Divider");
sDivider = (sDivider == null || sDivider.Length == 0) ? "\f\n" : Regex.Unescape(sDivider);

Regex rex;
try { rex = new Regex(sFind, options); }
catch (Exception ex) {
Dialog.Show("Error", section.Name + ": " + ex.Message);
App.CaptureOutput = false;
return;
}

if (bVerbose || !bApply) AddMessage(section.Name);
int iCount = 0;
if (!bApply) {
iCount = rex.Matches(sSourceBody).Count;
}
else if (bExtract) {
// Extract tasks collect matches to the clipboard and never modify the file.
foreach (Match m in rex.Matches(sSourceBody)) {
iCount++;
sExtractText += (sExtractText.Length > 0 ? sDivider : "") + m.ToString();
}
if (iCount > 0) { iExtractTotal += iCount; sClipboardDivider = sDivider; }
}
else {
// Replace / delete tasks rewrite the file. An empty Replace deletes matches;
// either way, any match means the content changed and the file must be saved.
sSourceBody = rex.Replace(sSourceBody, delegate(Match m) {
iCount++;
return m.Result(sReplace).Replace("$#", iCount.ToString());
});
if (iCount > 0) bChanged = true;
}
if (bVerbose || !bApply) AddMessage(Util.Pluralize(iCount, "match", "matches"));
}

if (bApply && bChanged) {
if (bWasCrlf) sSourceBody = sSourceBody.Replace("\n", "\r\n");
Util.String2File(sSourceBody, sSourceFile, ref en);
}
}

if (sExtractText.Length > 0) {
sExtractText = sExtractText.Replace("\n", "\r\n");
try {
string sClip = Util.GetClipboardText();
if (sClip.Length > 0) sClip += sClipboardDivider.Replace("\n", "\r\n");
Util.SetClipboardText(sClip + sExtractText);
AddMessage(Util.Pluralize(iExtractTotal, "match", "matches") + " to clipboard");
}
catch {}
}

AddMessage("Done", true);
App.CaptureOutput = false;
} // TransForm files method

public static string GetSnippetDir() {
string sBaseDir = @"Snippets\" + App.ReadData("Compiler", "Default");
string sDir = Path.Combine(App.DataDir, sBaseDir);
if (!Directory.Exists(sDir)) Directory.CreateDirectory(sDir);
return sDir;
} // GetSnippetDir method

public string GetDirChoice() {
string[] aButtons = {"&Current", "&Program", "&Data", "&Snippet", "&Other"};
string sButton = Dialog.Choose("Choose Directory", "", aButtons, 0);
if (sButton.Length == 0) return "";

string sDir = Directory.GetCurrentDirectory();
switch (sButton) {
case "&Current" :
if (this.Child != null && this.Child.File.IndexOf(@"\") >= 0) sDir = Path.GetDirectoryName(this.Child.File);
break;
case "&Program" :
sDir = App.ProgramDir;
break;
case "&Data" :
sDir = App.DataDir;
break;
case "&Snippet" :
sDir = App.DataDir + @"\Snippets\" + App.ReadData("Compiler", "Default");
if (!Directory.Exists(sDir)) Directory.CreateDirectory(sDir);
break;
case "&Other" :
sDir = Dialog.OpenFolder("Open Folder", "Name", Directory.GetCurrentDirectory());
if (sDir.Length == 0) return "";
break;
}
return sDir;
} // GetDirChoice method

public int GetViewLevel(string sFile) {
// Decide whether a file is converted when it is opened from outside the editor
// (Windows Explorer, "Open with", the command line, or Recent Files).  A return
// of 0 opens the file raw; 1 converts it through the Import table.  The ordinary
// Open command, Control+O, always opens raw regardless of this value.
// Precedence: an explicit ViewLevels entry wins, so the user can force any
// extension either way (e.g. "docx:0" to see a Word file raw, or "rst:1" to
// convert reStructuredText); otherwise binary / document formats convert,
// because their raw bytes are unreadable (and especially noisy for a screen
// reader); otherwise every text, markup, data, source, or unknown format opens
// raw.  This guarantees no text format is auto-converted -- now or as new
// converters are added -- unless it is explicitly listed.
const string sBinaryFormats = "doc docx xls xlsx ppt pptx pdf epub epub3 hlp wpd rtf";
string sExt = Path.GetExtension(sFile).TrimStart('.').ToLower();
string sViewLevels = App.ReadOption("ViewLevels", "");
foreach (string sViewLevel in sViewLevels.Split(' ')) {
string[] aViewLevel = sViewLevel.Split(':');
if (aViewLevel.Length < 2) continue;
if (sExt != aViewLevel[0].Trim().ToLower()) continue;
try { return Int32.Parse(aViewLevel[1]); }
catch (Exception ex) { Dialog.Show("Error", ex.Message); }
}
foreach (string sBinaryFormat in sBinaryFormats.Split(' ')) if (sExt == sBinaryFormat) return 1;
return 0;
} // GetViewLevel method

public string[] GetKeySummary(ToolStripMenuItem item) {
string sCommand = item.Name;
// KeyMap (Homer) is the single source. The first time a command's summary
// is requested it is read once from Hotkeys.ini and cached here, so the
// status bar, Key Describer, and Alternate Menu all agree and later reads
// cost nothing. The Hotkeys.ini format and parse are unchanged.
string sValue = KeyMap.getSummary(sCommand);
if (sValue.Length == 0) {
sValue = Ini.ReadValue(App.HotkeyIniFile, "Hotkeys", sCommand, "");
if (sValue.Length == 0) sValue = Ini.ReadValue(App.HotkeyIniFile, "Hotkeys", "Say " + sCommand, "");
if (sValue.Length == 0) sValue = "No description available";
KeyMap.setSummary(sCommand, sValue);
}
string sKey = "";
string sDescription = "";
int iComma = sValue.IndexOf(",");
if (iComma == -1) sDescription = sValue;
else {
sKey = sValue.Substring(0, iComma);
sDescription = sValue.Substring(iComma + 1);
}
return new string[] {sCommand, sKey, sDescription};
} // GetKeySummary method

// FOKUS WRACA DO POLA EDYCYJNEGO.  Jeden nosnik dla DWOCH wejsc: uaktywnienia
// okna (Alt+Tab) i zamkniecia menu.  Wczesniej to samo cialo stalo w domknieciu
// zdarzenia Activated, wiec nie dalo sie go wywolac z drugiego miejsca ani
// zmierzyc refleksja.
//
// BeginInvoke jest tu konieczne, nie kosmetyczne: ustawienie fokusu wprost w
// obsludze zdarzenia laduje PRZED tym, jak Windows skonczy swoje wlasne
// przekazywanie fokusu, i zostaje nadpisane.  Odlozenie do kolejki komunikatow
// stawia nas PO nim.
//
// W podgladzie Markdown fokus idzie do kontrolki podgladu, nie do zrodla -
// inaczej Escape z menu wyrzucalby uzytkownika z podgladu (ustalenie
// edsharpng-22: z podgladu wychodzi sie WYLACZNIE recznie).
public void FocusChildEditControl() {
try {
this.BeginInvoke((MethodInvoker) delegate {
try {
MdiChild child = this.Child;
if (child == null || child.IsDisposed) return;
if (child.MarkdownReviewMode && child.MarkdownReviewView != null && !child.MarkdownReviewView.IsDisposed) child.MarkdownReviewView.Focus();
else if (child.RTB != null && !child.RTB.IsDisposed) child.RTB.Focus();
}
catch {}
});
}
catch {}
} // FocusChildEditControl method

public void menuItem_Click(object sender, EventArgs e) {
//Util.Beep();
HomerRichTextBox rtb = null;
string[] aLabels, aValues, aResults;
bool bSelected;
int iLength, iStart, iEnd, iResult, iIndex, iLine, iPercent, iCount;
string sFile, sMatch, sReplace, sPattern, sSubstitute, sLine, sTitle, sText, sResult, sLabel, sValue;

ToolStripMenuItem menuItem = (ToolStripMenuItem) sender;
string sOptions = (string) menuItem.Tag;
sOptions = " " + sOptions.Trim().ToLower() + " ";
//sLabel = menuItem.Text.Replace("&", "").Replace(" ...", "").Split('\t')[0];
sLabel = menuItem.Name;
if (this.KeyDescriber && sLabel != "Key Describer") {
string[] aSummary = GetKeySummary(menuItem);
// TRZECIA SCIEZKA MOWY, KTORA BRAMKA CONTROL+ALT TEZ TLUMI (30.08.2026).
// Ctrl+F1 wlacza tryb opisywania klawiszy - niewidomy naciska skrot, a
// program mowi, co ten skrot robi.  Dla KAZDEJ komendy na Control+Alt
// (Control+Alt+K skok przypisu, Control+Alt+F9 lista komentarzy,
// Control+Alt+0 foldery systemowe) ten opis MILCZAL, bo idzie przez zwykle
// SetMessage/AddMessage.  Czyli funkcja sluzaca do nauki skrotow nie
// dzialala dokladnie tam, gdzie jest najbardziej potrzebna - przy skrotach
// najtrudniejszych do zapamietania.  Tego Kasperczak nie zglaszal; wyszlo
// z pomiaru bramki przy przenoszeniu skoku przypisu.
bool bDescribeGlobal = false;
Keys keysDescribe = menuItem.ShortcutKeys;
if ((keysDescribe & Keys.Control) != 0 && (keysDescribe & Keys.Alt) != 0) bDescribeGlobal = true;
SetStatus(aSummary[0]);
Util.Say(aSummary[0], bDescribeGlobal);
AddMessage(aSummary[1], bDescribeGlobal);
AddMessage(aSummary[2], bDescribeGlobal);
return;
}

// OPCJA "quiet": ani mowy, ani nazwy komendy na pasku stanu.
//
// "silent" zdejmowalo tylko MOWE, ale nazwe pozycji nadal kladlo na pasek
// stanu, a AddMessage DOPISUJE swoj komunikat do tego, co na pasku juz
// stoi.  Po Alt+Shift+L pasek mial wiec "Toggle Favorite   Added to
// favorites" i stad wracalo slowo "toggle", ktore Kasperczak zglosil
// powtornie 11.09.2026 ("niepotrzebnie za kazdym razem mowi toggle") -
// pierwsza poprawka uciszyla mowe, ale nie ruszyla paska.  Przy
// przelaczniku nazwa komendy nie niesie nic: uzytkownik wie, ktory klawisz
// nacisnal, a chce uslyszec KIERUNEK zmiany.
if (sOptions.Contains(" quiet ")) SetStatus("");
else if (sOptions.Contains(" silent ")) SetStatus(sLabel);
else SetMessage(sLabel);

MdiChild child = this.Child;
if (child == null) {
if (!sOptions.Contains(" frame ")) return;
}
else {
rtb = Child.RTB;
}

if (menuItem == menuFileNew) {
child = new MdiChild(App.Frame);
}

if (menuItem == menuFileNewFromClipboard) {
new MdiChild(App.Frame);
child = App.Frame.Child;
Child.RTB.Text = Util.GetClipboardText();
child.RTB.Modified = true;
}

if (menuItem == menuFileOpen) {

string sDir = "";
/*
if (child != null) sDir = Path.GetDirectoryName(child.File);
*/
sFile = Dialog.OpenFile("", sDir);
if (sFile.Length == 0) return;

int iConvert = 0;
// PLIK .RTF: LISTA WARIANTOW, NIE PYTANIE TAK-NIE (jego decyzja, ustalenie
// edsharpng-106, wariant B: "bardziej spojne").
//
// CO BYLO ZLE: Control+O na .rtf pytal "Treat as rich text?" z odpowiedziami
// tak, nie i anuluj.  Trzy rzeczy naraz byly przez to niejasne.  Po pierwsze
// "nie" nie znaczylo "nie otwieraj", a "otworz jako zwykly tekst", czyli
// pokaz surowe znaczniki RTF.  Po drugie konwersja do Markdown, ktora jest w
// tym programie droga domyslna dla obcych formatow, byla schowana ZA
// odpowiedzia "tak" i dopiero wtedy pokazywala sie lista formatow.  Po
// trzecie okno komunikatu nie mowi, co ktora odpowiedz zrobi - lista mowi to
// nazwa pozycji, wiec czytnik przeczyta wybor, a nie litere.
//
// Lista jest w kolejnosci od najczestszego uzycia; pozycja "Other
// conversion" prowadzi do tabeli Import, czyli do tego, co dotad bylo pod
// odpowiedzia "tak" (docx, html i cokolwiek user sam dopisze do EdSharp.ini).
if (Path.GetExtension(sFile).ToLower() == ".rtf") {
string[] aRtfChoice = new string[] {"md", "rich", "plain", "other"};
string[] aRtfDisplay = new string[] {
"Convert to Markdown",
"Open as rich text, keeping formatting",
"Open as plain text, showing RTF source",
"Other conversion ..."};
string sRtfChoice = Dialog.Pick("Open RTF File As", aRtfChoice, aRtfDisplay, false, 0);
if (sRtfChoice.Length == 0) return;

if (sRtfChoice == "md") {
// Klucz tabeli Import podany WPROST, zeby nie pytac drugi raz o to samo:
// bez niego ConvertFile2String pokazalby liste formatow, ktora wlasnie
// zastapilismy ta jedna.
OpenOrActivateWindow(sFile, 2, "", "", "rtf2md");
return;
}
if (sRtfChoice == "rich") iConvert = iOpenRichText;
else if (sRtfChoice == "other") iConvert = 2;
}
OpenOrActivateWindow(sFile, iConvert);
}

if (menuItem == menuFileOpenOtherFormat) {
sFile = Dialog.OpenFile("", "");
if (sFile.Length == 0) return;

OpenOrActivateWindow(sFile, 2);
return;
/*
AddMessage("Converting");
try {
sText = COM.ConvertFile2String(sFile);
}
catch (Exception ex) {
Dialog.Show("Error", ex.Message);
return;
}

if (!IsEmptyWindow()) new MdiChild(this);
child = this.Child;
rtb = child.RTB;
rtb.Text = sText;
rtb.Index = 0;
child.Text = Path.GetFileNameWithoutExtension(sFile) + ".txt";
//rtb.Modified = true;
rtb.Modified = false;
//AddMessage("Done");
*/
}

if (menuItem == menuFileOpenAgain) {
sFile = child.File;
if (!sFile.Contains(@"\")) {
AddMessage("No disk file is open for this command!");
return;
}

iIndex = rtb.Index;
if (Dialog.Confirm("Confirm", "Existing text will be  replaced.  Sure?", "Y") != "Y") return;
child.LoadTextOrRtfFile(sFile);
rtb.Index = iIndex;
}

if (menuItem == menuFileRecent) {
aResults = App.ReadSectionKeys("Recent");
List<string> list = new List<string>(aResults);
for (int i = list.Count - 1; i >=0; i--) {
string s = list[i];
if (File.Exists(s)) continue;
App.DeleteKey("Recent", s);
list.RemoveAt(i);
}

aResults = list.ToArray();
if (aResults.Length == 0) {
AddMessage("No items!");
return;
}

string[] aTime = new string[aResults.Length];
for (int i = 0; i < aTime.Length; i++) aTime[i] = App.ReadValue("Recent", aResults[i], "");
Array.Sort(aTime, aResults);
//Array.Reverse(aTime);
Array.Reverse(aResults);

iLength = aResults.Length;
int iMax = Int32.Parse(App.ReadOption("RecentFiles", "30"));
if (iLength > iMax) {
List<string> listResults = new List<string>(aResults);
for (int i = iLength - 1; i >= iMax; i--) {
App.DeleteKey("Recent", listResults[i]);
listResults.RemoveAt(i);
}
aResults = listResults.ToArray();
}

string[] aDisplay = new string[aResults.Length];
for (int i = 0; i < aDisplay.Length; i++) aDisplay[i] = Path.GetFileName(aResults[i]);

sFile = Dialog.PickFile("Recent Files", aResults, aDisplay, false, 0, "Recent");
if (sFile.Length == 0) return;

OpenOrActivateWindow(sFile, GetViewLevel(sFile));
}

if (menuItem == menuFileFind) {
FileFind();
}

if (menuItem == menuFileSaveCopy) {
AddMessage("Save Copy");
sFile = child.File + ".bak";
sFile = Dialog.SaveFile("", sFile);
if (sFile.Length == 0) return;
// Save Copy mial TEN SAM defekt co zwykly zapis, a nawet szerszy: pytal o
// PODCIAG ".rtf" w calej sciezce, wiec kopia pliku "notatki.rtf.txt" tez szla
// przez zapis bogaty.  Teraz pyta o ROZSZERZENIE i o proweniencje dokumentu.
if (Path.GetExtension(sFile).ToLower() == ".rtf" && child.IsRichTextDocument) rtb.SaveFile(sFile, RichTextBoxStreamType.RichText);
//else if (Util.IsUnicode(rtb.Text)) Util.String2File(rtb.Text, sFile);
//else rtb.SaveFile(sFile, RichTextBoxStreamType.PlainText);
else Util.String2File(Util.Convert2WinLineBreak(rtb.Text), sFile);
this.SetRecent(sFile);
}

if ((menuItem == menuFileSave) || (menuItem == menuFileSaveAs)) {
sFile = child.File;
if ((menuItem == menuFileSave) && sFile.Contains(@"\")) sText = "";//AddMessage("Save");
else {
sFile = Dialog.SaveFile("", sFile);
if (sFile.Length == 0) return;
}

//Dialog.Show(sFile);
//if (Path.GetExtension(sFile).Length == 0) sFile += ".txt";
child.SaveTextOrRtfFile(sFile);
//this.SetRecent(sFile);
rtb.Modified = false;
}

if (menuItem == menuFileExport) {
sFile = child.File;
aValues = Ini.ReadSectionKeys(App.IniFile, "Export");
//aValues = Array.FindAll(aValues, delegate(string s) {return s.StartsWith("pdf2");} );
//aValues = aValues.ConvertAll(delegate(string s){s.ToLower();});
HomerList hl = new HomerList(aValues);
hl.ToLower();
//list = list.ConvertAll<string>(delegate(string s) { return s.ToLower(); });
string sExt = Path.GetExtension(child.File).ToLower().TrimStart('.');
sMatch = @"^\w+2\w+$";
HomerList hl2 = hl.FindLike(sMatch);
hl.RemoveLike(sMatch);
sMatch = "^" + sExt + @"2\w+";
hl2 = hl2.FindLike(sMatch);
hl.AddRange(hl2);
hl.AddRange("asc|doc|htm|mac|rtf|unx|xml");
// do not offer original format, since already available with Control+O
if (sExt.Length > 0 && !hl.Contains(sExt)) hl.Add(sExt + "2" + sExt);
// hl.Add("Other");
hl.KeepUnique();
aValues = hl.ToArray();
hl.ReplaceLike(@"^\w+2(\w+)$", "$1");
string[] aDisplay = hl.ToArray();
// DWIE POZYCJE O TEJ SAMEJ NAZWIE ROBILY DWIE ROZNE RZECZY.
//
// Zmierzone 02.09.2026 na prawdziwych kluczach z EdSharp.ini: dla pliku .md
// lista eksportu pokazywala "rtf" DWA razy (wartosci "md2rtf" i "rtf") oraz
// "htm" DWA razy ("md2htm" i "htm").  Pozycja bez cyfry 2 zapisuje biezaca
// tresc kontrolki, pozycja z konwerterem przepuszcza dokument przez pandoca,
// czyli daje plik o INNEJ zawartosci.  Widzacy moze zgadnac po kolejnosci; dla
// czytnika ekranu obie pozycje sa nierozroznialne, a wybor rozstrzyga o tym,
// czy wyjdzie prawdziwy dokument, czy tekst z widocznymi znacznikami.
// Rozroznienie jest w NAZWIE, bo to jedyna warstwa, ktora czytnik wymawia.
for (int i = 0; i < aDisplay.Length; i++) {
int iSame = 0;
for (int j = 0; j < aDisplay.Length; j++) if (aDisplay[j] == aDisplay[i]) iSame++;
if (iSame < 2) continue;
if (aValues[i].IndexOf('2') >= 0) aDisplay[i] = aDisplay[i] + " (converted)";
else aDisplay[i] = aDisplay[i] + " (as shown)";
}
Array.Sort(aDisplay, aValues);

hl.Clear();
hl.AddRange(aDisplay);
hl.Add("Other");
aDisplay = hl.ToArray();
hl.Clear();
hl.AddRange(aValues);
hl.Add("Other");
aValues = hl.ToArray();

sTitle = "Export " + sExt + " to ";
sResult = Dialog.Pick(sTitle, aValues, aDisplay, false, 0);
//Dialog.Show(sResult);
if (sResult.Length == 0) return;

int iCodePage = -1;
string sCodePage = "";
if (sResult == "Other") {
iCodePage = Dialog.PickEncoding("", 0);
if (iCodePage == -1) return;
sCodePage = iCodePage.ToString();
sResult = sCodePage;
}

string sTargetExt = Util.RegExpReplaceCase(sResult, @"^\w+2", "");
sFile = Path.ChangeExtension(sFile, sTargetExt);
sFile = Dialog.SaveFile("", sFile);
if (sFile.Length == 0) return;

if (sCodePage.Length > 0) sExt = "Other";
else sExt = sResult.ToLower();
switch (sExt) {
case "Other" :
Encoding en = Encoding.GetEncoding(iCodePage);
sText = rtb.Text;
sText = Util.Convert2WinLineBreak(sText);
File.WriteAllText(sFile, sText, en);
break;
case "asc" :
case "mac" :
case "unx" :
sText = rtb.Text;
if (sExt == "asc") sText = Util.Convert2Ascii(sText);
else if (sExt == "mac") sText = Util.Convert2MacLineBreak(sText);
else if (sExt == "unx") sText = Util.Convert2UnixLineBreak(sText);
// Util.String2File(sText, sFile);
// KONCE WIERSZA MACA I LINUKSA IDA W UTF-8, NIE W ANSI (Kasperczak 28.08.2026:
// "mozliwosc zapisu i wyboru innych wierszy typu Mac i Linux powinna byc, ale
// to chyba gdzies tam przy zapisie tylko" - a tego samego dnia o kodowaniu:
// "czyli UTF-8").  To jest droga, ktora WSKAZALISMY mu jako zapis z obcymi
// koncami wiersza, wiec musi byc spojna z reszta programu.
// ZMIERZONE (testy/pomiar_eksportu_kodowania.cs): przy Encoding.Default litera
// "z z kropka" zapisywala sie jako JEDEN bajt bf ze strony 1250, czyli plik z
// koncami Linuksa mial polskie litery w kodowaniu, ktorego zaden system poza
// polskim Windowsem nie odczyta - a po Linuksie i Macu sie go wlasnie spodziewa.
// Po zmianie sa to bajty c5 bc, czyli UTF-8.
// BEZ ZNACZNIKA BOM: plik z koncami Linuksa to zwykle skrypt albo plik
// konfiguracyjny, a znacznik na poczatku psuje takie pliki.
// ASCII (pozycja "asc") ZOSTAJE na Encoding.Default swiadomie: Convert2Ascii
// i tak zdejmuje ogonki, wiec plik jest jednobajtowy z definicji.
if (sExt == "asc") File.WriteAllText(sFile, sText, Encoding.Default);
else File.WriteAllText(sFile, sText, new UTF8Encoding(false));
break;
case "rtf" :
rtb.SaveFile(sFile, RichTextBoxStreamType.RichText);
break;
default :
string s = Path.GetExtension(child.File);
if (Util.Equiv(s, ".rtf")) sText = rtb.Rtf;
else sText = rtb.Text;
Util.ConvertString2FileFormat(sText, s, sFile, sExt);
break;
}
if (File.Exists(sFile)) AddMessage("Done");
else AddMessage("Error!");
}

if (menuItem == menuFileRename) {
sFile = child.File;
if (!sFile.Contains(@"\")) {
AddMessage("No disk file is open for this command!");
return;
}

string sName = Dialog.Input("Rename", "File Name", child.Text).Trim();
if (sName.Length == 0) return;

string sNewFile = Path.Combine(Path.GetDirectoryName(sFile), sName);
try {
File.Move(sFile, sNewFile);
}
catch (Exception ex) {
Dialog.Show("Error", ex.Message);
return;
}
child.Text = sName;
child.File = sNewFile;
SetRecent(sNewFile);
}

if (menuItem == menuFileMailBody) {
string sSubject = Path.GetFileNameWithoutExtension(child.Text);
string sBody = rtb.Text;
// string sRecipient = "";
try {
MapiMail.SendMail(sSubject, sBody, null, null);
}
catch {
Util.MailMessage("", sSubject, sBody);
}
return;

/*
try {
sText = rtb.Text;
sText = Util.Convert2WinLineBreak(sText);
sText = Util.RegExpReplaceCase(sText, "\r\n", "%0D%0A");
sText = Util.RegExpReplaceCase(sText, " ", "%20");
sText = Util.RegExpReplaceCase(sText, "\t", "%09");
sText = Util.RegExpReplaceCase(sText, "\"", "%22");
sText = Util.RegExpReplaceCase(sText, "'", "%27");
sText = Util.RegExpReplaceCase(sText, "\\\\", "%5C");
string sCommand = "mailto:?BODY=" + sText;
Process.Start(sCommand);
}
catch {
Mail(false);
}
*/
}

if (menuItem == menuFileMailAttach) {
sFile = child.File;
if (!sFile.Contains(@"\")) {
AddMessage("No disk file is open for this command!");
return;
}

// ZALACZNIK IDZIE Z DYSKU, WIEC NIEZAPISANE ZMIANY BY W NIM NIE POJECHALY.
// Zmierzone 01.09.2026 na zapytanie Kasperczaka "jedna funkcja dziala, druga
// nie i musimy to sprawdzic": tresc listu bierze tekst Z OKNA (rtb.Text),
// a zalacznik plik Z DYSKU - przy zmodyfikowanym dokumencie te dwie komendy
// wysylaly ROZNA tresc, i to bez slowa ostrzezenia.
if (rtb.Modified) {
if (Dialog.Confirm("Confirm", "This document has unsaved changes, and the attachment is taken from the file on disk.\nSave it first?", "Y") == "Y") {
child.SaveTextOrRtfFile(sFile);
rtb.Modified = false;
}
}

string sSubject = Path.GetFileName(child.Text) + " attached";
KeyValuePair<string, string>[] aAttachments = {new KeyValuePair<String, String>(Path.GetFileName(sFile), sFile)};
try {
MapiMail.SendMail(sSubject, "", null, aAttachments);
AddMessage("Mail with attachment handed to the mail client");
}
catch (Exception exMail) {
// PUSTY catch BYL TU CICHA AWARIA: gdy MAPI zawiedzie (brak
// zainstalowanego klienta poczty, anulowanie okna, blad sesji), komenda
// NIE MOWILA NIC i niewidomy uzytkownik nie mial jak odroznic wyslania od
// niewyslania.  Tresc listu miala tu przewage: ona przy tym samym bledzie
// spada na mailto:, wiec cos sie u niego dzialo.  Zalacznika przez mailto:
// wyslac NIE MOZNA (ten protokol nie ma pola na plik), wiec droga awaryjna
// jest inna: mowimy wprost, co sie stalo, i zostawiamy wybor.
string sMailError = exMail.Message;
if (sMailError == null) sMailError = "";
AddMessage("Mail client refused the attachment!");
if (Dialog.Confirm("Mail Attachment Failed", "The mail client did not accept the attachment.\n" + sMailError + "\n\nSend the text as the message body instead (without the file attached)?", "Y") == "Y") {
Util.MailMessage("", Path.GetFileNameWithoutExtension(child.Text), rtb.Text);
}
}
return;

//Mail(true);
}

if (menuItem == menuFileRun) {
sFile = child.File;
if (!sFile.Contains(@"\") || rtb.Modified) {
sFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(sFile));
sText = rtb.Text;
Util.String2File(sText, sFile);
}

Process.Start(sFile);
}

if (menuItem == menuFilePrint) {
sFile = child.File;
sFile = Path.GetFileName(sFile);
sFile = child.File;
if (!sFile.Contains(@"\")) {
AddMessage("No disk file is open for this command!");
return;
}

if (Dialog.Confirm("Confirm", "Print " + sFile + "?", "Y") != "Y") return;

if (Path.GetExtension(sFile).ToLower() == ".rtf") COM.InvokeVerb(sFile, "Print");
else Util.RunHideWait("Notepad.exe /p " + Util.Quote(sFile));
/*
sFile = Path.Combine(Path.GetTempPath(), sFile);
sText = rtb.Text;
sText = Util.Convert2WinLineBreak(sText);
Util.String2File(sText, sFile);
string sExe;
if (Path.GetExtension(sFile).ToLower() == ".rtf") sExe = "cmd.exe /c WordPad.exe";
else sExe = "Notepad.exe";
string sCommand = sExe + " /P " + Util.Quote(sFile);
Util.RunHideWait(sCommand);
File.Delete(sFile);
*/
}

if (menuItem == menuFileProperties) {
sFile = child.File;
if (!sFile.Contains(@"\")) {
AddMessage("No disk file is open for this command!");
return;
}

Dialog.Properties(sFile);
}

if (menuItem == menuFileCurrentWindows) {
CurrentWindows();
}

if (menuItem == menuFileSlots) {
PickFileSlot();
}

if (menuItem == menuFileClose) {
CloseWindow(child);
}

if (menuItem == menuFileCloseAllButCurrentWindow) {
CloseAllButCurrentWindow();
}

if (menuItem == menuFileExit) {
ExitApp();
}

if (menuItem == menuEditSelectAll) {
rtb.SelectAll();
iCount = rtb.SelectedText.Length;
AddMessage(Util.Pluralize(iCount, "character"));
}

if (menuItem == menuEditUnselectAll) {
rtb.DeselectAll();
iIndex = rtb.Index;
if (iIndex >= rtb.TextLength) return;
sText = rtb.GetRange(rtb.Index, rtb.Index + 1);
AddMessage(sText);
}

if (menuItem == menuEditCopy) {
if (rtb.SelectionLength == 0) {
AddMessage("Line");
sText = rtb.RowText + LineBreak;
}
else {
AddMessage("Selected");
sText = rtb.SelectedText;
rtb.StoreSelection();
}
sText = Util.Convert2WinLineBreak(sText);
Util.SetClipboardText(sText);
}

if (menuItem == menuEditCopyAppend) {
sText = Util.GetClipboardText();
sText = Util.Convert2UnixLineBreak(sText);
if (sText.Length > 0 && !sText.EndsWith(LB)) sText += LB;
if (rtb.SelectionLength == 0) {
AddMessage("Line");
sText += rtb.RowText + LB;
}
else {
AddMessage("Selected");
sText += rtb.SelectedText;
rtb.StoreSelection();
}
sText = Util.Convert2WinLineBreak(sText);
Util.SetClipboardText(sText);
}

if (menuItem == menuEditCopyRichText) {
// Markdown-aware rich copy: a Markdown link becomes an RTF hyperlink, a
// Markdown list becomes a real (Word-style) list, and a formatted line
// keeps its inline markup; otherwise fall back to the plain RTF copy.
// WIELOWIERSZOWE ZAZNACZENIE (Kasperczak, zlecenie 1788204748663-2,
// "nie zapominaj o tamtych rzeczach zwiazanych z formatowaniem, z
// kopiowaniem formatowania"): trzy sciezki nizej obsluguja WIERSZ, w
// ktorym stoi kursor.  TryCopyMarkdownRichLine ma wprost warunek
// "SelectionLength != 0 return false", wiec zaznaczenie kilku wierszy
// spadalo na rtb.Copy() i do Worda szedl goly tekst z GWIAZDKAMI.
// TryCopyMarkdownSelection idzie PIERWSZA i tylko wtedy, gdy naprawde
// jest zaznaczenie obejmujace wiecej niz jeden wiersz - jednowierszowe
// przypadki zostaja przy dotychczasowych sciezkach, ktore rozpoznaja
// odsylacz pod kursorem i pojedyncza liste.
if (!TryCopyMarkdownSelection(this.Child) && !TryCopyMarkdownLinkAsRichText(this.Child) && !TryCopyMarkdownList(rtb) && !TryCopyMarkdownRichLine(this.Child)) rtb.Copy();
}

if (menuItem == menuEditCut) {
if (rtb.SelectionLength == 0) {
AddMessage("Line");
sText = rtb.RowText + LineBreak;
rtb.Select(rtb.RowStart, rtb.RowLength);
}
else {
AddMessage("Selected");
sText = rtb.SelectedText;
}
rtb.Cut();
sText = Util.Convert2WinLineBreak(sText);
Util.SetClipboardText(sText);
Util.Say(rtb.RowText);
}

if (menuItem == menuEditCutAppend) {
sText = Util.GetClipboardText();
sText = Util.Convert2UnixLineBreak(sText);
if (sText.Length > 0 && !sText.EndsWith(LB)) sText += LB;
if (rtb.SelectionLength == 0) {
AddMessage("Line");
sText += rtb.RowText + LB;
rtb.Select(rtb.RowStart, rtb.RowLength);
}
else {
AddMessage("Selected");
sText += rtb.SelectedText;
}
rtb.Cut();
sText = Util.Convert2WinLineBreak(sText);
Util.SetClipboardText(sText);
Util.Say(rtb.RowText);
}

if (menuItem == menuEditPaste) {
// WKLEJANIE WEWNATRZ EDSHARPA ODZYSKUJE SKLADNIE MARKDOWN (zgloszenie
// Kasperczaka 01.09: "skopiowalem i wkleilem w EdSharpie w innym miejscu
// bogato sformatowany link, a on wkleil sama nazwe, sam tytul linku").
// ZMIERZONA PRZYCZYNA: nasze kopiowanie z formatowaniem sklada schowek pod
// WORDA - tekstowy format to sam tytul, a odsylacz siedzi w postaci RTF w
// polu HYPERLINK.  Kontrolka edycyjna w pliku tekstowym bierze wlasnie
// format tekstowy, wiec wklejala tytul BEZ adresu, czyli traciła odsylacz.
// Adresu nie da sie odzyskac z tego, co zostaje w schowku dla Worda, dlatego
// przy kopiowaniu dokładamy WLASNY format schowka ze skladnia Markdown, a
// tutaj czytamy go PRZED zwyklym wklejeniem.  Word i LibreOffice nieznanego
// formatu nie widza, wiec kopiowanie na zewnatrz zachowuje sie jak dotad.
if (PasteEdSharpMarkdownFormat(rtb)) return;
rtb.Paste();
sText = Util.GetClipboardText();
sText = Util.Convert2UnixLineBreak(sText);
aResults = Util.RegExpExtractCase(sText, @"\s+\Z");
if (aResults.Length > 0) {
iIndex = rtb.Index;
sText = aResults[0];
rtb.ReplaceRange(iIndex, iIndex, sText);
rtb.Index = iIndex + sText.Length;
}
}

if (menuItem == menuEditPasteFile) {
sFile = Dialog.OpenFile("", "");
if (sFile.Length == 0) return;

sText = Util.File2String(sFile);
string sChoice = "N";
if (Path.GetExtension(sFile).ToLower() == ".rtf") sChoice = Dialog.Confirm("Confirm", "Treat as rich text?", "Y");
if (sChoice.Length == 0) return;

rtb.Index = rtb.SelectionStart + rtb.SelectionLength;
if (sChoice == "Y") rtb.SelectedRtf = sText;
else rtb.SelectedText = sText;
Util.Say(rtb.RowText);
}

if (menuItem == menuEditUndo) {
rtb.Undo();
}

if (menuItem == menuEditRedo) {
rtb.Redo();
}

if (menuItem == menuEditStartSelection) {
//if (!IsCharacter(out iIndex)) return;
iIndex = rtb.Index;
rtb.StartSelection = iIndex;
if (iIndex >=0 && iIndex < rtb.TextLength) {
sText = rtb.GetRange(rtb.Index, rtb.Index + 1);
AddMessage(sText);
}
}

if (menuItem == menuEditCompleteSelection) {
iStart = rtb.StartSelection;
iEnd = rtb.Index;
if (iStart > iEnd) Util.Swap(ref iStart, ref iEnd);
rtb.SelectRange(iStart, iEnd);
iCount = rtb.SelectedText.Length;
AddMessage(Util.Pluralize(iCount, "character"));
rtb.OldSelectionStart = rtb.SelectionStart;
rtb.OldSelectionLength = rtb.SelectionLength;
}

if (menuItem == menuEditReselect) {
rtb.Reselect();
}

if (menuItem == menuEditCopyAll) {
sText = rtb.Text;
sText = Util.Convert2WinLineBreak(sText);
Util.SetClipboardText(sText);
}

if (menuItem == menuEditSelectChunk) {
bool bLoop = false;
string c = "";
object[] a = GetChunk();
iStart = (int) a[0];
iIndex = iStart;
sText = rtb.Text;
if (rtb.SelectionLength == 0) {
AddMessage("Select Chunk");
}
else {
iStart = rtb.SelectionStart;
bLoop = iIndex < sText.Length;
while (bLoop) {
c = sText.Substring(iIndex, 1);
bLoop = (c.Trim().Length == 0);
iIndex++;
bLoop = (bLoop && iIndex < sText.Length);
}
}

bLoop = iIndex < sText.Length;
while (bLoop) {
c = sText.Substring(iIndex, 1);
bLoop = (c.Trim().Length > 0);
iIndex++;
bLoop = (bLoop && iIndex < sText.Length);
}
iEnd = iIndex;
rtb.SelectRange(iStart, iEnd);
}

if (menuItem == menuEditAppendFromClipboard) {
if (child.AppendFromClipboard == 0) {
AddMessage("Append from Clipboard On");
child.AppendFromClipboard = -1;
child.NextClipboardViewer =  Util.SetClipboardViewer(child.Handle);
}
else {
AddMessage("No Append from Clipboard");
child.AppendFromClipboard = 0;
Util.ChangeClipboardChain(child.Handle, child.NextClipboardViewer);
child.NextClipboardViewer = (IntPtr) 0;
}
}

if (menuItem == menuEditQuote) {
//if (!IsCharacter(out iIndex)) return;

string sPrefix = App.ReadOption("QuotePrefix", "> ");
if (rtb.SelectionLength == 0) {
AddMessage("Line");
iStart = rtb.RowStart;
iEnd = iStart + rtb.RowText.Length;
//Dialog.Show(iStart, iEnd);
}
else {
AddMessage("Selected");
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
}

sText = rtb.GetRange(iStart, iEnd);
sText = Util.RegExpReplaceCase(sText, "^", sPrefix);
rtb.ReplaceRange(iStart, iEnd, sText);
rtb.Index = iStart;
Util.Say(rtb.RowText);
}

if (menuItem == menuEditUnquote) {
string sPrefix = App.ReadOption("QuotePrefix", "> ");
if (rtb.SelectionLength == 0) {
AddMessage("Line");
iStart = rtb.RowStart;
iEnd = iStart + rtb.RowText.Length;
}
else {
AddMessage("Selected");
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
}

sText = rtb.GetRange(iStart, iEnd);
//sText = Util.RegExpReplaceCase(sText, @"^( |\t|\>)+", "");
sText = Util.RegExpReplaceCase(sText, @"^" + sPrefix, "");
rtb.ReplaceRange(iStart, iEnd, sText);
rtb.Index = iStart;
Util.Say(rtb.RowText);
}

if (menuItem == menuEditUpperCase) {
if (rtb.SelectionLength == 0) {
AddMessage("Character");
iStart = rtb.Index;
iEnd = iStart + 1;
bSelected = false;
}
else {
AddMessage("Selected");
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
bSelected = true;
}

sText = rtb.GetRange(iStart, iEnd);
sText = sText.ToUpper();
rtb.ReplaceRange(iStart, iEnd, sText);
rtb.Index = iStart;
if (bSelected) sText = rtb.RowText;
else sText = rtb.GetRange(rtb.Index, rtb.Index + 1);
AddMessage(sText);
}

if (menuItem == menuEditLowerCase) {
if (rtb.SelectionLength == 0) {
AddMessage("Character");
iStart = rtb.Index;
iEnd = iStart + 1;
bSelected = false;
}
else {
AddMessage("Selected");
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
bSelected = true;
}

sText = rtb.GetRange(iStart, iEnd);
sText = sText.ToLower();
rtb.ReplaceRange(iStart, iEnd, sText);
rtb.Index = iStart;
if (bSelected) sText = rtb.RowText;
else sText = rtb.GetRange(rtb.Index, rtb.Index + 1);
AddMessage(sText);
}

if (menuItem == menuEditProperCase) {
if (rtb.SelectionLength == 0) {
AddMessage("Character");
iStart = rtb.Index;
iEnd = iStart + 1;
bSelected = false;
}
else {
AddMessage("Selected");
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
bSelected = true;
}

sText = rtb.GetRange(iStart, iEnd);
sText = Util.ProperCase(sText);
rtb.ReplaceRange(iStart, iEnd, sText);
rtb.Index = iStart;
if (bSelected) sText = rtb.RowText;
else sText = rtb.GetRange(rtb.Index, rtb.Index + 1);
AddMessage(sText);
}

if (menuItem == menuEditSwapCase) {
if (rtb.SelectionLength == 0) {
AddMessage("Character");
iStart = rtb.Index;
iEnd = iStart + 1;
bSelected = false;
}
else {
AddMessage("Selected");
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
bSelected = true;
}

sText = rtb.GetRange(iStart, iEnd);
sText = Util.SwapCase(sText);
rtb.ReplaceRange(iStart, iEnd, sText);
rtb.Index = iStart;
if (bSelected) sText = rtb.RowText;
else sText = rtb.GetRange(rtb.Index, rtb.Index + 1);
AddMessage(sText);
}

if (menuItem == menuEditYieldEncoding) {
if (rtb.SelectionLength == 0) {
sTitle = "Yield Encoding All";
iStart = 0;
iEnd = rtb.TextLength;
}
else {
sTitle = "Yield Encoding Selected";
iStart = rtb.SelectionStart;
// Dialog.Show(iStart, rtb.SelectionLength);
iEnd = iStart + rtb.SelectionLength;
}

sText = rtb.GetRange(iStart, iEnd);

// string[] aButtons = {"&Default", "&ASCII", "UTF-&7", "UTF-&8", "&Unicode", "UTF-&32", "&Latin1", "&Other"};
string[] aButtons = {"&ASCII", "&Latin1", "UTF-&8", "&UTF-16", "UTF-&7", "UTF-&32", "&Other", "&Codes"};
string sButton = Dialog.Choose(sTitle, "", aButtons, 0);
if (sButton.Length == 0) return;

Encoding def = Encoding.Default;
byte[] aBytes = new byte[def.GetByteCount(sText)];
def.GetBytes(sText, 0, sText.Length, aBytes, 0);
switch (sButton) {
case "&Default" :
sText = def.GetString(aBytes);
break;
case "&ASCII" :
Encoding asc = Encoding.GetEncoding("us-ascii", new EncoderReplacementFallback(""), new DecoderReplacementFallback(""));
// aBytes = new byte[asc.GetByteCount(sText)];
// asc.GetBytes(sText, 0, sText.Length, aBytes, 0);
sText = asc.GetString(aBytes);
break;
case "UTF-&7" :
sText = Encoding.UTF7.GetString(aBytes);
break;
case "UTF-&8" :
sText = Encoding.UTF8.GetString(aBytes);
break;
case "&UTF-16" :
sText = Encoding.Unicode.GetString(aBytes);
break;
case "UTF-&32" :
sText = Encoding.UTF32.GetString(aBytes);
break;
case "&Latin1" :
Encoding latin1 = Encoding.GetEncoding(1252, new EncoderReplacementFallback(""), new DecoderReplacementFallback(""));
// aBytes = new byte[latin1.GetByteCount(sText)];
// latin1.GetBytes(sText, 0, sText.Length, aBytes, 0);
sText = latin1.GetString(aBytes);
break;
case "&Codes" :
sResult = "\n";
foreach (char c in sText) {
sResult+= ((int) c).ToString() + "\n";
}
sText = sResult;
break;

case "&Other" :
/*
string sCodePage = Dialog.Input("Input", "Code Page:", "");
if (sCodePage.Length == 0) return;
Encoding other;
try {
if (Util.IsNumeric(sCodePage)) other = Encoding.GetEncoding(Int32.Parse(sCodePage), new EncoderReplacementFallback(""), new DecoderReplacementFallback(""));
else other = Encoding.GetEncoding(sCodePage, new EncoderReplacementFallback(""), new DecoderReplacementFallback(""));
}
catch (Exception ex) {
Dialog.Show("Error", ex.Message);
return;
}
*/

int iCodePage = Dialog.PickEncoding("", 0);
if (iCodePage == -1) return;

Encoding other = Encoding.GetEncoding(iCodePage, new EncoderReplacementFallback(""), new DecoderReplacementFallback(""));

// aBytes = new byte[other.GetByteCount(sText)];
// other.GetBytes(sText, 0, sText.Length, aBytes, 0);
sText = other.GetString(aBytes);
break;
}

rtb.ReplaceRange(iStart, iEnd, sText);
rtb.Index = iStart + sText.Length;
Util.Say(rtb.RowText);
}

if (menuItem == menuEditJoinLines) {
if (rtb.SelectionLength == 0) {
AddMessage("All");
iStart = 0;
iEnd = rtb.TextLength;
}
else {
AddMessage("Selected");
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
}

sText = rtb.GetRange(iStart, iEnd);
sText = Util.RegExpReplaceCase(sText, @" +\n", "\n");
sText = Util.RegExpReplaceCase(sText, "([^\n])\n([^\n])", "$1 $2");
rtb.ReplaceRange(iStart, iEnd, sText);
rtb.Index = iStart + sText.Length;
Util.Say(rtb.RowText);
}

if (menuItem == menuEditHardLineBreak) {
HardLineBreak();
}

if (menuItem == menuEditEnterNewLine|| menuItem == menuEditIndentNewLine) {
SetStatus("");
if ((!rtb.IndentMode && menuItem == menuEditEnterNewLine) || (rtb.IndentMode && menuItem == menuEditIndentNewLine)) {
// Reduce verbosity
// if (rtb.IndentMode) AddMessage("Enter New Line");
// else SetStatus("Enter New Line");
SetStatus("Indent New Line");
sText = "\n";
iIndex = rtb.Index;
rtb.ReplaceRange(iIndex, iIndex, sText);
rtb.Index = iIndex + sText.Length;
}
else {
SetStatus("Indent New Line");
// Reduce verbosity
// AddMessage("Indent New Line");
sText = rtb.RowText;
iIndex = rtb.RowStart + sText.Length;
sMatch = @"^(\s*).*";
sReplace = "$1";
sText = Util.RegExpReplaceCase(sText, sMatch, sReplace);
sText = "\n" + sText;
rtb.ReplaceRange(iIndex, iIndex, sText);
rtb.Index = iIndex + sText.Length;
AddMessage("Level " + this.GetIndent());
// Reduce verbosity
// Util.Say(rtb.RowText);
}
}

if (menuItem == menuEditIndentNewLinePrior) {
sText = rtb.RowText;
iIndex = rtb.RowStart;
sMatch = @"^(\s*).*";
sReplace = "$1";
sText = Util.RegExpReplaceCase(sText, sMatch, sReplace);
sText = sText + "\n";
rtb.ReplaceRange(iIndex, iIndex, sText);
rtb.Index--;
AddMessage("Level " + this.GetIndent());
// Reduce verbosity
// Util.Say(rtb.RowText);
}

if (menuItem == menuEditIndent) {
string sIndent = App.ReadOption("IndentUnit", "  ");
sIndent = Util.Literalize(sIndent);
iIndex = rtb.Index;
bool bLine;
if (rtb.SelectionLength == 0) {
// AddMessage("Line");
iStart = rtb.RowStart;
iEnd = iStart + rtb.RowText.Length;
bLine = true;
}
else {
AddMessage("Selected");
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
bLine = false;
}

sText = rtb.GetRange(iStart, iEnd);
sText = Util.RegExpReplaceCase(sText, "^", sIndent);
rtb.ReplaceRange(iStart, iEnd, sText);

//if (bLine) rtb.Index = iIndex;
if (bLine) rtb.Index = iIndex + sIndent.Length;
else rtb.Index  = iIndex + sText.Length;
AddMessage("Level " + this.GetIndent());
//Util.Say(rtb.RowText);
}

if (menuItem == menuEditOutdent) {
string sIndent = App.ReadOption("IndentUnit", "  ");
sIndent = Util.Literalize(sIndent);
iIndex = rtb.Index;
if (rtb.SelectionLength == 0) {
// AddMessage("Line");
iStart = rtb.RowStart;
iEnd = iStart + rtb.RowText.Length;
}
else {
AddMessage("Selected");
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
}

sText = rtb.GetRange(iStart, iEnd);
iLength = sText.Length;
sText = Util.RegExpReplaceCase(sText, "^" + sIndent, "");
rtb.ReplaceRange(iStart, iEnd, sText);
if (sText.Length < iLength) rtb.Index = iIndex - sIndent.Length;
AddMessage("Level " + this.GetIndent());
//Util.Say(rtb.RowText);
}

if (menuItem == menuEditAlign) {
string sIndent = App.ReadOption("IndentUnit", "  ");
sIndent = Util.Literalize(sIndent);
iIndex = rtb.Index;
bool bLine;
if (rtb.SelectionLength == 0) {
AddMessage("Line");
iStart = rtb.RowStart;
iEnd = iStart + rtb.RowText.Length;
bLine = true;
}
else {
AddMessage("Selected");
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
bLine = false;
}

char[] a = {' ', '\t'};
sLine = "";
string sComment = App.ReadOption("QuotePrefix", "> ");
int iRow = rtb.GetIndexRow(iStart);
// Dialog.Show("iStart " + iStart, "iRow " + iRow);
int iLevels = GetIndent(iRow);
int i = iLevels;
int iTop = 0;
while (iRow > iTop) {
iRow--;
// Dialog.Show("row " + iRow);
sLine = rtb.GetRowText(iRow).Trim(a);
if (sLine.Length == 0 || sLine.StartsWith(sComment)) continue;
i = GetIndent(iRow);
if (iLevels != i) break;
}

int iDelta = i - iLevels;
// Dialog.Show(iDelta);

sText = rtb.GetRange(iStart, iEnd);
if (iDelta > 0) {
for (i = 0; i < iDelta; i++) {
iLength = sText.Length;
sText = Util.RegExpReplaceCase(sText, "^", sIndent);
if (sText.Length > iLength) iIndex += sIndent.Length;
}
}
else {
iDelta = iDelta * -1;
for (i = 0; i < iDelta; i++) {
iLength = sText.Length;
sText = Util.RegExpReplaceCase(sText, "^" + sIndent, "");
if (sText.Length < iLength) iIndex -= sIndent.Length;
}
}

rtb.ReplaceRange(iStart, iEnd, sText);
bLine = !bLine;
rtb.Index = iIndex;
AddMessage("Level " + this.GetIndent());
}

if (menuItem == menuEditIndentMode) {
rtb.IndentMode = !rtb.IndentMode;
AddMessage(rtb.IndentMode ? "On" : "Off");
// Environment.SetEnvironmentVariable("EdSharpIndent", "", EnvironmentVariableTarget.User);
bool b = System.IO.File.Exists(App.IndentModeFile);
if (b && !rtb.IndentMode) System.IO.File.Delete(App.IndentModeFile);
else if (!b && rtb.IndentMode) System.IO.File.Create(App.IndentModeFile).Close();
//return;
}

if (menuItem == menuEditJustify) {
if (rtb.SelectionLength == 0) {
sTitle = "Justify Cursor";
}
else {
sTitle = "Justify Selected";
}

aValues = new string[] {"&Left", "&Bullet", "&Center", "&Right"};
int i = 0;
if (rtb.SelectionBullet) i = 1;
if (rtb.SelectionAlignment == HorizontalAlignment.Center) i = 2;
else if (rtb.SelectionAlignment == HorizontalAlignment.Right) i = 3;
sResult = Dialog.Choose(sTitle, "", aValues, i);
if (sResult.Length == 0) return;

rtb.SelectionBullet = false;
switch (sResult) {
case "&Left" :
rtb.SelectionAlignment = HorizontalAlignment.Left;
break;
case "&Bullet" :
rtb.SelectionBullet = true;
break;
case "&Center" :
rtb.SelectionAlignment = HorizontalAlignment.Center;
break;
case "&Right" :
rtb.SelectionAlignment = HorizontalAlignment.Right;
break;
}
}

if (menuItem == menuEditStyle) {
if (rtb.SelectionLength == 0) {
sTitle = "Style Cursor";
}
else {
sTitle = "Style Selected";
}

aValues = new string[] {"Bold", "Italic", "Underline"};
List<int> listSelect = new List<int>();
if (rtb.SelectionFont.Bold) listSelect.Add(0);
if (rtb.SelectionFont.Italic) listSelect.Add(1);
if (rtb.SelectionFont.Underline) listSelect.Add(2);
int[] aSelect = listSelect.ToArray();

//aResults = Dialog.MultiPick(sTitle, aValues, aSelect, false);
aResults = Dialog.MultiCheck(sTitle, aValues, aSelect, false, 0);
if (aResults.Length == 0) return;

if (!listSelect.Contains(0) && Array.IndexOf(aResults, "Bold") >= 0) rtb.SelectionFont = Util.SetBold(rtb.SelectionFont, true);
if (listSelect.Contains(0) && Array.IndexOf(aResults, "Bold") < 0) rtb.SelectionFont = Util.SetBold(rtb.SelectionFont, false);
if (!listSelect.Contains(0) && Array.IndexOf(aResults, "Italic") >= 0) rtb.SelectionFont = Util.SetItalic(rtb.SelectionFont, true);
if (listSelect.Contains(0) && Array.IndexOf(aResults, "Italic") < 0) rtb.SelectionFont = Util.SetItalic(rtb.SelectionFont, false);
if (!listSelect.Contains(0) && Array.IndexOf(aResults, "Underline") >= 0) rtb.SelectionFont = Util.SetUnderline(rtb.SelectionFont, true);
if (listSelect.Contains(0) && Array.IndexOf(aResults, "Underline") < 0) rtb.SelectionFont = Util.SetUnderline(rtb.SelectionFont, false);
}

if (menuItem == menuEditBaseline) {
if (rtb.SelectionLength == 0) {
sTitle = "Baseline Cursor";
}
else {
sTitle = "Baseline Selected";
}

aValues = new string[] {"&Down", "&Flat", "&Up"};
int i = 1;
if (rtb.SelectionCharOffset < 0) i = 0;
else if (rtb.SelectionCharOffset > 0) i = 2;
sResult = Dialog.Choose(sTitle, "", aValues, i);
if (sResult.Length == 0) return;

switch (sResult) {
case "&Down" :
rtb.SelectionCharOffset = -4;
break;
case "&Flat" :
rtb.SelectionCharOffset = 0;
break;
case "&Up" :
rtb.SelectionCharOffset = 4;
break;
}
}

if (menuItem == menuEditSetSelectionFont) {
if (rtb.SelectionLength == 0) {
AddMessage("Cursor");
}
else {
AddMessage("Selected");
}

object[] a = Dialog.GetFont(rtb.SelectionFont, rtb.SelectionColor);
if (a.Length == 0) return;

rtb.SelectionFont = (Font) a[0];
rtb.SelectionColor = (Color) a[1];
}

if (menuItem == menuMiscEnvironmentVariables) {
string sChoice = Dialog.Choose("Target", "", new string[] {"&Process", "&User", "&Machine"}, 0);
if (sChoice.Length == 0) return;

EnvironmentVariableTarget target = EnvironmentVariableTarget.Process;
if (sChoice == "&User") target = EnvironmentVariableTarget.User;
else if (sChoice == "&Machine") target = EnvironmentVariableTarget.Machine;

IDictionary dic = Environment.GetEnvironmentVariables(target);
iCount = dic.Count;
aLabels = new string[iCount];
aValues = new string[iCount];
string[] aKeys = new string[iCount];

int iKey = 0;
foreach (DictionaryEntry de in dic) {
aKeys[iKey] = ((string) de.Key).ToLower();
aLabels[iKey] = "&" + (string) de.Key;
iKey++;
}
Array.Sort(aKeys, aLabels);

iKey = 0;
foreach (DictionaryEntry de in dic) {
aKeys[iKey] = ((string) de.Key).ToLower();
aValues[iKey] = (string) de.Value;
iKey++;
}
Array.Sort(aKeys, aValues);

sTitle = "Variables for ";
if (sChoice == "&Process") sTitle += "Process " + Process.GetCurrentProcess().ProcessName;
else if (sChoice == "&User") sTitle += "User " + Environment.UserName;
else if (sChoice == "&Machine") sTitle += "Machine " + Environment.MachineName;
aResults = Dialog.MultiInput(sTitle, aLabels, aValues);
if (aResults.Length == 0) return;

try {
for (int i = 0; i < iCount; i++) {
if (aResults[i] == aValues[i]) continue;

if (Dialog.Confirm("Confirm", "Change " + aKeys[i] + "?", "Y") != "Y") continue;
Environment.SetEnvironmentVariable(aKeys[i], aResults[i], target);
}
}
catch (Exception ex) {
Dialog.Show("Error", ex.Message);
return;
}
AddMessage("Done");
}

if (menuItem == menuMiscSpellCheck) {
SpellCheck();
}

if (menuItem == menuMiscSpellingWordMenu) {
SpellingWordMenu();
}

if (menuItem == menuMiscExtraSpeechLog) {
OpenOrActivateWindow(App.SpeechLog, 0);
}

if (menuItem == menuMiscThesaurus) {
Thesaurus();
}

if (menuItem == menuMiscLookupTerm) {
if (this.Child != null) {
if (rtb.SelectionLength == 0) {
//AddMessage("Chunk");
object[] a = GetChunk();
iStart = (int) a[0];
sText = (string) a[1];
}
else {
//AddMessage("Selected");
iStart = rtb.SelectionStart;
sText = rtb.SelectedText;
iEnd = iStart + sText.Length;
}

sText = sText.TrimEnd();
}
else sText = "";

if (sText.Length == 0) sText = App.ReadData("Term", "");
sResult = Dialog.Input("Lookup", "Term", sText).Trim();
if (sResult.Length == 0) return;

App.WriteData("Term", sResult);
//AddMessage("Please wait");
AddMessage("Connecting");
sText = VB.LookupTerm(sResult);
if (!IsEmptyWindow()) new MdiChild(this);
child = this.Child;
child.Text = sResult + ".txt";
child.File = child.Text;
rtb = child.RTB;
rtb.Text = sText;
}

if (menuItem == menuMiscTranslateLanguage) {
if (rtb.SelectionLength == 0) {
iStart = 0;
iEnd = rtb.TextLength;
}
else {
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
}
sText = rtb.GetRange(iStart, iEnd);

string[] aLanguageNames, aLanguageAbbreviations;
Util.GetGoogleLanguages(out aLanguageNames, out aLanguageAbbreviations);
string sSourceLanguage = Dialog.Pick("Source Language", aLanguageAbbreviations, aLanguageNames, false, 0);
if (sSourceLanguage.Length == 0) return;

string sTargetLanguage = Dialog.Pick("Target Language", aLanguageAbbreviations, aLanguageNames, false, 0);
if (sTargetLanguage.Length == 0) return;

string sExe = App.ProgramDir + @"\Convert\TranLang.exe";
string sSourceFile = App.TempFile;
Encoding en = Encoding.UTF8;
en = null;
Util.String2File(sText, sSourceFile, ref en);
string sTargetFile = sSourceFile;
string sCommand = Util.Quote(sExe) + " " + sSourceLanguage + " " + Util.Quote(sSourceFile) + " " + sTargetLanguage + " " + Util.Quote(sTargetFile);
Util.RunHideWait(sCommand);
en = Encoding.UTF8;
// en = null;
sText = Util.File2String(sTargetFile, ref en);
File.Delete(sSourceFile);
File.Delete(sTargetFile);

if (!IsEmptyWindow()) new MdiChild(this);
child = this.Child;
// child.Text = sResult + ".txt";
child.File = child.Text;
rtb = child.RTB;
rtb.Text = sText;

}

// JEDNA KOMENDA PRZELACZA OCHRONE (jego decyzja 03.09.2026).
// Stan brany z GetUserGuard, nie z rtb.ReadOnly: podglad Markdown wlacza
// ochrone na czas podgladu, wiec surowy odczyt kontrolki kazalby
// przelacznikowi ZDJAC ochrone, ktorej uzytkownik nigdy nie wlaczyl.
// Sam SetGuard w podgladzie nie ma sensu i tam komenda milczy o skutku,
// bo wyjscie z podgladu i tak przywraca stan uzytkownika.
// Mowa podaje SKUTEK, bo przy przelaczniku ten sam klawisz robi dwie rozne
// rzeczy i komunikat jest jedynym sposobem, zeby wiedziec ktora.
if (menuItem == menuMiscGuardDocument) {
if (child.MarkdownReviewMode) {
AddMessage("Close the preview first!");
return;
}
bool bWasGuarded = GetUserGuard(child);
rtb.SetGuard(!bWasGuarded);
SaveGuardFlag(child.File, !bWasGuarded);
AddMessage(bWasGuarded ? "Guard off" : "Guard on");
}

if (menuItem == menuMiscPyBrace) {
if (rtb.SelectionLength == 0) {
AddMessage("All");
iStart = 0;
iEnd = rtb.TextLength;
sFile = Path.GetFileNameWithoutExtension(child.Text);
if (Path.GetExtension(child.Text).ToLower() == ".boo") sFile += ".bob";
else sFile += ".pyb";
}
else {
AddMessage("Selected");
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
sFile = "";
}
sText = rtb.GetRange(iStart, iEnd);
sText = PyDent2Brace(sText);

if (sFile.Length == 0) {
rtb.ReplaceRange(iStart, iEnd, sText);
rtb.Index = iStart;
}
else {
child = new MdiChild(App.Frame, sFile);
Child.RTB.Text = sText;
child.RTB.Modified = true;
}
AddMessage("Done");
}

if (menuItem == menuMiscPyDent) {
sFile = child.File;
string sExt = Path.GetExtension(sFile).ToLower();
sFile = Path.GetFileNameWithoutExtension(sFile);
if (sExt == ".bob") sFile += ".boo";
else sFile += ".py";

if (rtb.SelectionLength == 0) {
AddMessage("All");
iStart = 0;
iEnd = rtb.TextLength;
}
else {
AddMessage("Selected");
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
sFile = "";
}
sText = rtb.GetRange(iStart, iEnd);

//if (sExt != ".bob" && sExt != ".pyb") sText = PyDent2Brace(sText);
if (sExt == ".boo" || sExt == ".py") sText = PyDent2Brace(sText);
sText = PyBrace2Dent(sText);

if (sFile.Length == 0) {
rtb.ReplaceRange(iStart, iEnd, sText);
rtb.Index = iStart;
}
else {
child = new MdiChild(App.Frame, sFile);
Child.RTB.Text = sText;
child.RTB.Modified = true;
}
AddMessage("Done");
}

if (menuItem == menuMiscInferIndent) {
sText = rtb.Text;
aResults = Util.RegExpExtractCase(sText, @"^( |\t)+");
if (aResults.Length == 0) {
AddMessage("No indentation found!");
return;
}

string sIndent = aResults[0];
if (this.KeyRepeat % 2 == 0) {
AddMessage("Infer Indent");
if (sIndent.Contains(" ") && sIndent.Contains("\t")) {
foreach (char c in sIndent) {
if (c == ' ') AddMessage("Space");
else AddMessage("Tab");
}
}
else {
if (sIndent.StartsWith(" ")) sText = "space";
else sText = "tab";
AddMessage(Util.Pluralize(sIndent.Length, sText));
}
}
else {
sIndent = sIndent.Replace(" ", @"\040");
sIndent = sIndent.Replace("\t", @"\t");
App.WriteOption("IndentUnit", sIndent);
AddMessage("IndentUnit configured");
}
}

if (menuItem == menuMiscRepeatLine) {
sText = CR + rtb.RowText;
iIndex = rtb.RowStart + rtb.RowText.Length;
rtb.ReplaceRange(iIndex, iIndex, sText);
rtb.Index = iIndex + 1;
Util.Say(rtb.RowText);
}

// Control+Enter starts a new section.  In this fork a section is a Markdown
// heading, so it inserts "## " at the level of the heading above rather than
// the original dashes-plus-form-feed break.  Kasperczak reported the old
// behaviour as a regression (Telegram 14.08.2026 18:26) and chose the level
// rule himself (18:34: "CTRL-Enter to naglowek taki, jak naglowek wyzej").
if (menuItem == menuMiscSectionBreak) {
InsertMarkdownHeadingAtCursor(rtb);
}

if (menuItem == menuDeleteReplaceRegular) {
if (rtb.SelectionLength == 0) {
sTitle = "Replace All";
iStart = 0;
iEnd = rtb.TextLength;
}
else {
sTitle = "Replace Selected";
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
}

aLabels = new string[] {"&Match", "&Replace"};
sMatch = App.ReadData("Match", "");
sReplace = App.ReadData("Replace", "");
aValues = new string[] {sMatch, sReplace};
aResults = Dialog.MultiInput(sTitle, aLabels, aValues);
if (aResults == null || aResults.Length == 0) return;

sMatch = aResults[0];
App.WriteData("Match", sMatch);
sMatch = Util.Literalize(sMatch, true);
sMatch = Regex.Escape(sMatch);
sReplace = aResults[1];
App.WriteData("Replace", sReplace);
sReplace = Util.Literalize(sReplace, true);

sText = rtb.GetRange(iStart, iEnd);
iCount = Util.RegExpCountEquiv(sText, sMatch);
sText = Util.RegExpReplaceEquiv(sText, sMatch, sReplace);
if (iCount > 0) rtb.ReplaceRange(iStart, iEnd, sText);
AddMessage(Util.Pluralize(iCount, "match", "matches"));
}

if (menuItem == menuDeleteReplaceWithRegExp) {
if (rtb.SelectionLength == 0) {
sTitle = "Replace All with Regular Expression";
iStart = 0;
iEnd = rtb.TextLength;
}
else {
sTitle = "Replace Selected with Regular Expression";
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
}

aLabels = new string[] {"&Pattern", "&Substitute"};
sPattern = App.ReadData("Pattern", "");
sSubstitute = App.ReadData("Substitute", "");
aValues = new string[] {sPattern, sSubstitute};
aResults = Dialog.MultiInput(sTitle, aLabels, aValues);
if (aResults == null || aResults.Length == 0) return;

sPattern = aResults[0];
App.WriteData("Pattern", sPattern);
//sPattern = Util.Literalize(sPattern);
sSubstitute = aResults[1];
App.WriteData("Substitute", sSubstitute);
sSubstitute = Util.Literalize(sSubstitute);
sText = rtb.GetRange(iStart, iEnd);
iCount = Util.RegExpCountCase(sText, sPattern);
sText = Util.RegExpReplaceCase(sText, sPattern, sSubstitute);
if (iCount > 0) rtb.ReplaceRange(iStart, iEnd, sText);
AddMessage(Util.Pluralize(iCount, "match", "matches"));
}

if (menuItem == menuMiscRegExpTool) {
// JEDNA KOMENDA, DWA DZIALANIA (jego decyzja 03.09.2026).  Kolejnosc pytan
// jest celowa: najpierw CO zrobic, potem wzorzec.  Odwrotnie user pisalby
// wyrazenie regularne, nie wiedzac jeszcze, do czego posluzy - a wzorzec
// bywa dlugi i zaczynanie go od nowa po zmianie zdania jest kosztowne.
string[] aRegExpValues = new string[] {"count", "extract"};
string[] aRegExpNames = new string[] {"Count matches", "Extract matches to a new window"};
string sAction = Dialog.Pick("Regular Expression Tool", aRegExpValues, aRegExpNames, false, 0);
if (sAction.Length == 0) return;
bool bExtract = (sAction == "extract");

if (rtb.SelectionLength == 0) {
sTitle = (bExtract ? "Extract All with Regular Expression" : "Yield All with Regular Expression");
iStart = 0;
iEnd = rtb.TextLength;
}
else {
sTitle = (bExtract ? "Extract Selected with Regular Expression" : "Yield Selected with Regular Expression");
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
}

sLabel = "Pattern";
sValue = App.ReadData("Pattern", "");
sResult = Dialog.Input(sTitle, sLabel, sValue);
if (sResult.Length == 0) return;

App.WriteData("Pattern", sResult);
sText = rtb.GetRange(iStart, iEnd);
if (!bExtract) {
iCount = Util.RegExpCountCase(sText, sResult);
AddMessage(Util.Pluralize(iCount, "match", "matches"));
}
else {
aResults = Util.RegExpExtractCase(sText, sResult);
iCount = aResults.Length;
AddMessage(Util.Pluralize(iCount, "match", "matches"));
if (iCount == 0) return;

new MdiChild(this);
rtb = App.Frame.Child.RTB;
sText = String.Join(SectionBreak, aResults);
rtb.ReplaceRange(0, 0, sText);
rtb.Index = 0;
}
}

if (menuItem == menuMiscRunAtCursor) {
if (rtb.SelectionLength == 0) {
sTitle = "Run Chunk at Cursor";
object[] a = GetChunk();
sText = (string) a[1];
}
else {
sTitle = "Run Selected at Cursor";
sText = rtb.SelectedText;
}

sLabel = "Path";
sReplace = "";
sMatch = "(\r|\n)";
sText = Util.RegExpReplaceCase(sText, sMatch, sReplace);
sMatch = "^(\\<| )+";
sText = Util.RegExpReplaceCase(sText, sMatch, sReplace);
sMatch = "(\\>| |\\.)+$";
sText = Util.RegExpReplaceCase(sText, sMatch, sReplace);

if (sText.Contains("://")) sText = sText.Trim(); //do nothing
else if (sText.ToLower().StartsWith("www.")) sText = "http://" + sText;
else if (sText.Contains("@") && !sText.ToLower().StartsWith("mailto")) sText = "MailTo:" + sText;

sResult = Dialog.Input(sTitle, sLabel, sText).Trim();
if (sResult.Length == 0) return;
Process.Start(sResult);
}

if (menuItem == menuMiscSpecialCharacter) {
string sCode = App.ReadData("Code", "");
sResult = Dialog.Input("Special Character", "Code:", sCode).Trim().ToLower();
if (sResult.Length == 0) return;

App.WriteData("Code", sResult);
if (sResult.StartsWith(@"\")) sResult = sResult.Remove(0, 1);
if (sResult.StartsWith("d")) {
sResult = sResult.Remove(0, 1);
try {
int iCode = Int32.Parse(sResult);
sText = Util.Code2String(iCode);
}
catch (Exception ex) {
Dialog.Show("Error", ex.Message);
return;
}
}
else {
if (sResult.StartsWith("u")) sResult = sResult.Remove(0, 1);
string s = sResult;
sText = Util.Literalize(@"\u" + sResult.PadLeft(4, '0'));
if (sText.Length == 0 || sText == "\u0000") {
Dialog.Show("Error", "Invalid Unicode number");
return;
}
}

iIndex = rtb.Index;
rtb.ReplaceRange(iIndex, iIndex, sText);
rtb.Index = iIndex + 1;
AddMessage(sText);
}

if (menuItem == menuDeleteHardLine) {
iStart = rtb.RowStart;
iEnd = rtb.Text.IndexOf("\n", iStart);
if (iEnd >= 0) iEnd++;
else iEnd = rtb.TextLength;
rtb.ReplaceRange(iStart, iEnd, "");
rtb.Index = iStart;
Util.Say(rtb.RowText);
}

if (menuItem == menuDeleteParagraph) {
iStart = rtb.RowStart;
sMatch = @"\n\s*\n";
object[] a = Util.RegExpContainsCase(rtb.Text, sMatch, iStart);
iEnd = (int) a[0];
//Dialog.Show(iEnd, ((string) a[1]).Length);
if (iEnd >= 0) iEnd += ((string) a[1]).Length;
else iEnd = rtb.TextLength;
rtb.ReplaceRange(iStart, iEnd, "");
rtb.Index = iStart;
Util.Say(rtb.RowText);
}

if (menuItem == menuDeleteLine) {
iStart = rtb.RowStart;
iEnd = iStart + rtb.RowLength;
rtb.ReplaceRange(iStart, iEnd, "");
rtb.Index = iStart;
Util.Say(rtb.RowText);
}

if (menuItem == menuDeleteRight) {
iStart = rtb.Index;
iEnd = rtb.RowStart + rtb.RowText.Length;
//if (iEnd != rtb.TextLength) iEnd--;
rtb.ReplaceRange(iStart, iEnd, "");
rtb.Index = iStart;
Util.Say(rtb.RowText);
}

if (menuItem == menuDeleteLeft) {
iStart = rtb.RowStart;
iEnd = rtb.Index;
rtb.ReplaceRange(iStart, iEnd, "");
rtb.Index = iStart;
Util.Say(rtb.RowText);
}

if (menuItem == menuDeleteDown) {
iStart = rtb.Index;
iEnd = rtb.TextLength;
rtb.ReplaceRange(iStart, iEnd, "");
rtb.Index = iStart;
Util.Say(rtb.RowText);
}

if (menuItem == menuDeleteUp) {
iStart = 0;
iEnd = rtb.Index;
rtb.ReplaceRange(iStart, iEnd, "");
rtb.Index = iStart;
Util.Say(rtb.RowText);
}

if (menuItem == menuDeleteFile) {
sFile = child.File;
if (!sFile.Contains(@"\")) {
AddMessage("No disk file is open for this command!");
return;
}

if (Dialog.Confirm("Confirm", "Delete " + child.Text + "?", "N") != "Y") return;
File.Delete(sFile);
child.Close();
}

if (menuItem == menuDeleteTrimBlanks) {
if (rtb.SelectionLength == 0) {
AddMessage("Line");
iStart = rtb.RowStart;
iEnd = iStart + rtb.RowText.Length;
}
else {
AddMessage("Selected");
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
}

sText = rtb.GetRange(iStart, iEnd);
sText = Util.RegExpReplaceCase(sText, "^( |\t)+", "");
sText = Util.RegExpReplaceCase(sText, "( |\t)+$", "");
sText = Util.RegExpReplaceCase(sText, "\n\n\n\n+", "\n\n\n");
rtb.ReplaceRange(iStart, iEnd, sText);
rtb.Index = iStart;
Util.Say(rtb.RowText);
}

if (menuItem == menuNavigateForwardFind || menuItem == menuNavigateForwardFindAtCursor || (!this.FindWithRegExp && menuItem == menuNavigateForwardFindAgain)) {
iIndex = rtb.Index;
iStart = 0;
sText = App.ReadData("Find", "");
if (menuItem == menuNavigateForwardFind) sText = Dialog.Input("Forward Find", "Text", sText);
else if (menuItem == menuNavigateForwardFindAtCursor) {
if (rtb.SelectionLength == 0) {
object[] a = GetChunk();
iStart = (int) a[0];
sText = (string) a[1];
}
else {
iStart = rtb.SelectionStart;
sText = rtb.SelectedText;
}
}
if (sText.Length == 0) return;

App.WriteData("Find", sText);
this.FindWithRegExp = false;
sText = Util.Literalize(sText, true);
sText = Util.Convert2MacLineBreak(sText);

if (menuItem == menuNavigateForwardFindAtCursor) {
iStart += sText.Length;
iEnd = -1;
}
else if (rtb.SelectionLength == 0) {
iStart = iIndex;
iEnd = -1;
}
else {
iStart = rtb.SelectionStart;
iEnd = rtb.SelectionStart + rtb.SelectionLength;
}

iIndex = rtb.Find(sText, iStart, iEnd, RichTextBoxFinds.NoHighlight);
if (iIndex >= 0) {
rtb.Index = iIndex + sText.Length;
Util.Say(rtb.RowText);
}
else AddMessage("Not found!");
}

if (menuItem == menuNavigateReverseFind || menuItem == menuNavigateReverseFindAtCursor || (!this.FindWithRegExp && menuItem == menuNavigateReverseFindAgain)) {
iIndex = rtb.Index;
iEnd = 0;
sText = App.ReadData("Find", "");
if (menuItem == menuNavigateReverseFind) sText = Dialog.Input("Reverse Find", "Text", sText);
else if (menuItem == menuNavigateReverseFindAtCursor) {
if (rtb.SelectionLength == 0) {
object[] a = GetChunk();
iEnd = (int) a[0];
sText = (string) a[1];
}
else {
iEnd = rtb.SelectionStart;
sText = rtb.SelectedText;
}
}
if (sText.Length == 0) return;

App.WriteData("Find", sText);
this.FindWithRegExp = false;
sText = Util.Literalize(sText, true);
sText = Util.Convert2MacLineBreak(sText);

if (menuItem == menuNavigateReverseFindAtCursor) {
iStart = 0;
}
else if (rtb.SelectionLength == 0) {
iStart = 0;
iEnd = iIndex;
}
else {
iStart = rtb.SelectionStart;
iEnd = rtb.SelectionStart + rtb.SelectionLength;
}

iIndex = rtb.Find(sText, iStart, iEnd, RichTextBoxFinds.Reverse | RichTextBoxFinds.NoHighlight);
//if (iIndex >= 0) {
if (iIndex >= 0 && iIndex < iEnd) {
rtb.Index = iIndex;
Util.Say(rtb.RowText);
}
else AddMessage("Not found!");
}

if (menuItem == menuNavigateForwardFindWithRegExp || (this.FindWithRegExp && menuItem == menuNavigateForwardFindAgain)) {
sMatch = App.ReadData("Pattern", "");
if (menuItem == menuNavigateForwardFindWithRegExp) sMatch = Dialog.Input("Forward Find with Regular Expression", "Pattern", sMatch);
if (sMatch.Length == 0) return;

App.WriteData("Pattern", sMatch);
this.FindWithRegExp = true;

if (rtb.SelectionLength == 0) {
iStart = rtb.Index;
iEnd = rtb.TextLength;
}
else {
iStart = rtb.SelectionStart;
iEnd = rtb.SelectionStart + rtb.SelectionLength;
}
sText = rtb.GetRange(iStart, iEnd);

object[] a = Util.RegExpContainsCase(sText, sMatch);
iIndex = (int) a[0];
if (iIndex >= 0) {
sValue = (string) a[1];
rtb.Index = iStart + iIndex + sValue.Length;
Util.Say(rtb.RowText);
}
else AddMessage("Not found!");
}

if (menuItem == menuNavigateReverseFindWithRegExp || (this.FindWithRegExp && menuItem == menuNavigateReverseFindAgain)) {
sMatch = App.ReadData("Pattern", "");
if (menuItem == menuNavigateReverseFindWithRegExp) sMatch = Dialog.Input("Reverse Find with Regular Expression", "Pattern", sMatch);
if (sMatch.Length == 0) return;

App.WriteData("Pattern", sMatch);
this.FindWithRegExp = true;

if (rtb.SelectionLength == 0) {
iStart = 0;
iEnd = rtb.Index;
}
else {
iStart = rtb.SelectionStart;
iEnd = rtb.SelectionStart + rtb.SelectionLength;
}
sText = rtb.GetRange(iStart, iEnd);

object[] a = Util.RegExpContainsLastCase(sText, sMatch);
iIndex = (int) a[0];
if (iIndex >= 0) {
sValue = (string) a[1];
rtb.Index = iStart + iIndex;
Util.Say(rtb.RowText);
}
else AddMessage("Not found!");
}

if (menuItem == menuNavigateJumpToLine || menuItem == menuNavigateJumpToLineAgain) {
sText = App.ReadData("Jump", "");
if (menuItem == menuNavigateJumpToLine) sText = Dialog.Input("Jump to", "Line", sText);
if (sText.Length == 0) return;

string[] a = sText.Split(',');
sLine = a[0].Trim();
if (sLine.Length == 0) sLine = rtb.Line.ToString();
string sColumn = a.Length > 1 ? a[1].Trim() : "1";

try {
iLine = Int32.Parse(sLine);
}
catch (Exception ex) {
Dialog.Show("Error", ex.Message);
return;
}

int iColumn = 1;
try {
iColumn = Int32.Parse(sColumn);
}
catch (Exception ex) {
Dialog.Show("Error", ex.Message);
return;
}

App.WriteData("Jump", sText);
try {
rtb.Line = iLine;
rtb.Column = iColumn;
Util.Say(rtb.RowText);
}
catch {
Dialog.Show("Error", "Invalid position!");
return;
}
}
if (menuItem == menuNavigateGoToPercent || menuItem == menuNavigateGoToPercentAgain) {
sText = App.ReadData("Percent", "");
if (menuItem == menuNavigateGoToPercent) sText = Dialog.Input("Go to", "Percent", sText);
if (sText.Length == 0) return;

try {
iPercent = Int32.Parse(sText);
}
catch (Exception ex) {
Dialog.Show("Error", ex.Message);
return;
}

App.WriteData("Percent", sText);
rtb.Percent = iPercent;
Util.Say(rtb.RowText);
}

if (menuItem == menuNavigateSetBookmark) {
sFile = child.File;
if (!sFile.Contains(@"\")) {
AddMessage("No disk file is open for this command!");
return;
}

// ZAKLADKI SIEDZA W SEKCJI Bookmarks, NIE Favorites (jego decyzja z
// 01.09.2026: "Rozdzielic.  Zakladki a ulubione pliki nie sa powiazane w jedna
// i druga strone.  To osobne sprawy.").  Postawienie zakladki NIE czyni juz
// pliku ulubionym.
sText = App.ReadValue("Bookmarks", sFile, "");
HomerList hl = new HomerList(sText);
hl.KeepLike(@"\d+");
hl.Remove("-1");
hl.AddUniqueRange(rtb.Index.ToString());
sText = hl.Segments;
App.WriteValue("Bookmarks", sFile, sText);
}

if (menuItem == menuNavigateClearBookmark) {
sFile = child.File;
if (!sFile.Contains(@"\")) {
AddMessage("No disk file is open for this command!");
return;
}

sText = App.ReadValue("Bookmarks", sFile, "");
if (sText.Length == 0) return;
HomerList hl = new HomerList(sText);
hl.Remove(rtb.Index.ToString());
sText = hl.Segments;
if (sText.Length == 0) App.DeleteKey("Bookmarks", sFile);
else App.WriteValue("Bookmarks", sFile, sText);
}

if (menuItem == menuNavigateGoToBookmark) {
sFile = child.File;
if (!sFile.Contains(@"\")) {
AddMessage("No disk file is open for this command!");
return;
}

sText = App.ReadValue("Bookmarks", sFile, "");
HomerList hl = new HomerList(sText);
hl.KeepLike(@"\d+");
hl.Remove("-1");
if (hl.Count == 0) {
AddMessage("No bookmark!");
return;
}

if (hl.Count == 1) sResult = hl[0];
else {
hl.PadLeft(hl.MaxLength(), ' ');
hl.Sort();
HomerList hlLines = new HomerList();
foreach (string sIndex in hl) {
iIndex = Int32.Parse(sIndex);
int iRow = rtb.GetLineFromCharIndex(iIndex);
iStart = rtb.GetFirstCharIndexFromLine(iRow);
iEnd = rtb.GetFirstCharIndexFromLine(iRow + 1);
if (iEnd == -1) iEnd = rtb.TextLength;
sLine = rtb.GetRange(iStart, iEnd).Trim();
hlLines.Add(sLine);
}
string[] aDisplay = hlLines.ToArray();
aValues = hl.ToArray();
iIndex = rtb.Index;
int iDefault = -1;
for (int i = 0; i < hl.Count; i++) {
//Dialog.Show(iIndex, Int32.Parse(hl[i]));
if (iIndex < Int32.Parse(hl[i])) {
iDefault = i;
break;
}
}
//Dialog.Show(iDefault);

sResult = Dialog.PickBookmark("Bookmarks", aValues, aDisplay, iDefault, sFile);
if (sResult.Length == 0) return;
}

rtb.Index = Int32.Parse(sResult);
Util.Say(rtb.RowText);
}

// Next / Prior Bookmark (Shift+PageDown / Shift+PageUp). Sequential
// bookmark navigation: jump straight to the nearest bookmark in the given
// direction WITHOUT opening the Bookmarks list, then speak the whole line
// landed on (same feedback as Go to Bookmark). At the last / first
// bookmark it stays put and says so, instead of wrapping around.
if (menuItem == menuNavigateNextBookmark || menuItem == menuNavigatePriorBookmark) {
bool bNextBookmark = (menuItem == menuNavigateNextBookmark);
sFile = child.File;
if (!sFile.Contains(@"\")) {
AddMessage("No disk file is open for this command!");
return;
}

sText = App.ReadValue("Bookmarks", sFile, "");
HomerList hlMarks = new HomerList(sText);
hlMarks.KeepLike(@"^\d+$");
hlMarks.Remove("-1");
if (hlMarks.Count == 0) {
AddMessage("No bookmark!");
return;
}

List<int> listMarks = new List<int>();
foreach (string sMark in hlMarks) {
int iMark;
if (!Int32.TryParse(sMark.Trim(), out iMark)) continue;
if (iMark < 0) continue;
if (iMark > rtb.TextLength) iMark = rtb.TextLength;
if (!listMarks.Contains(iMark)) listMarks.Add(iMark);
}
if (listMarks.Count == 0) {
AddMessage("No bookmark!");
return;
}
listMarks.Sort();

iIndex = rtb.Index;
iResult = -1;
if (bNextBookmark) {
for (int i = 0; i < listMarks.Count; i++) {
if (listMarks[i] > iIndex) { iResult = listMarks[i]; break; }
}
}
else {
for (int i = listMarks.Count - 1; i >= 0; i--) {
if (listMarks[i] < iIndex) { iResult = listMarks[i]; break; }
}
}

if (iResult < 0) {
AddMessage(bNextBookmark ? "Last bookmark!" : "First bookmark!");
return;
}

rtb.Index = iResult;
Util.Say(rtb.RowText);
}

// ZAKLADKI Z NAZWA: wstawienie (Control+Shift+B) i lista (Alt+Shift+B).
// Nazwa jest OBOWIAZKOWA - zakladka bez nazwy nie rozni sie niczym od zwyklej,
// a pusta pozycja na liscie jest dla niewidomego gorsza niz jej brak.
// Stojac na wierszu, ktory JUZ ma nazwana zakladke, ten sam klawisz POPRAWIA
// nazwe (okno mowi tytulem, ktory to przypadek), a wyczyszczenie pola ZDEJMUJE
// zakladke - dokladnie ta sama zasada, ktora dostaly komentarze na Alt+F9.
if (menuItem == menuNavigateSetNamedBookmark) {
if (child == null) return;
sFile = child.File;
if (!sFile.Contains(@"\")) {
AddMessage("No disk file is open for this command!");
return;
}

int iRowNamed = rtb.GetLineFromCharIndex(rtb.Index);
string sExisting = ReadNamedBookmark(sFile, iRowNamed);
bool bEditNamed = (sExisting.Length > 0);
// PROPOZYCJA NAZWY TO SLOWO POD KURSOREM, NIE CALY WIERSZ.  Jego decyzja z
// 01.09.2026 10:34, doslownie: "Nazwa zakladki nie tyle z wiersza co ze slowa
// na ktorym jest kursor.  To powiedzmy sobie szczerze bardziej sensowne i
// precyzyjne."  Caly wiersz akapitu bywa dluzszy niz limit 120 znakow i po
// obcieciu dawal nazwe uciety w polowie zdania.  GetChunk to ta sama droga,
// ktora program bierze slowo dla slownika i wyszukiwania terminu, wiec
// zachowanie jest spojne z resztka programu.
string sSuggest = "";
if (rtb.SelectionLength > 0) sSuggest = rtb.SelectedText;
else {
object[] aChunk = GetChunk();
sSuggest = (string) aChunk[1];
}
if (sSuggest == null) sSuggest = "";
sSuggest = sSuggest.Trim();

LbcDialog dlgNamed = new LbcDialog(bEditNamed ? "Rename Bookmark" : "Set Named Bookmark", this);
TextBox txtNamed = dlgNamed.addInputBox("Bookmark &name", bEditNamed ? sExisting : sSuggest);
if (!dlgNamed.runOkCancel()) {
dlgNamed.Dispose();
return;
}
string sNamed = (txtNamed.Text ?? "").Trim();
dlgNamed.Dispose();

// Nazwa idzie do klucza INI, wiec znaki lamiace plik ustawien trzeba zdjac.
sNamed = sNamed.Replace("\r", " ").Replace("\n", " ").Replace("=", "-").Replace("|", "-").Trim();
if (sNamed.Length > 120) sNamed = sNamed.Substring(0, 120).Trim();

if (sNamed.Length == 0) {
if (bEditNamed) {
DeleteNamedBookmark(sFile, iRowNamed);
AddMessage("Named bookmark removed");
}
else AddMessage("No name, no bookmark set!");
return;
}

WriteNamedBookmark(sFile, iRowNamed, sNamed);
AddMessage(bEditNamed ? "Bookmark renamed" : "Named bookmark set");
}

// Lista zakladek z nazwa (Alt+Shift+B).  OSOBNA od listy zakladek zwyklych
// pod Alt+B - jego decyzja, ustalenie edsharpng-40.  Delete usuwa pozycje bez
// wychodzenia z okna, jak na liscie zakladek zwyklych.
if (menuItem == menuNavigateNamedBookmarkList) {
if (child == null) return;
sFile = child.File;
if (!sFile.Contains(@"\")) {
AddMessage("No disk file is open for this command!");
return;
}

List<int> listRows = new List<int>();
List<string> listNames = new List<string>();
ReadNamedBookmarks(sFile, listRows, listNames);
if (listRows.Count == 0) {
AddMessage("No named bookmark!");
return;
}

string[] aRowValues = new string[listRows.Count];
string[] aRowShow = new string[listRows.Count];
for (int i = 0; i < listRows.Count; i++) {
aRowValues[i] = listRows[i].ToString();
// TRESC PRZED WSPOLRZEDNA, jak wszedzie w tym programie: nazwa, potem wiersz.
aRowShow[i] = listNames[i] + ", line " + (listRows[i] + 1);
}

int iRowNow = rtb.GetLineFromCharIndex(rtb.Index);
int iDefaultNamed = -1;
for (int i = 0; i < listRows.Count; i++) {
if (listRows[i] >= iRowNow) {iDefaultNamed = i; break;}
}
if (iDefaultNamed < 0) iDefaultNamed = listRows.Count - 1;

string sPickedRow = Dialog.PickNamedBookmark("Named Bookmarks", aRowValues, aRowShow, iDefaultNamed, sFile);
if (sPickedRow == null || sPickedRow.Length == 0) return;
int iRowGo;
if (!Int32.TryParse(sPickedRow.Trim(), out iRowGo)) return;
if (iRowGo < 0) iRowGo = 0;
if (iRowGo > rtb.Lines.Length - 1) iRowGo = rtb.Lines.Length - 1;
rtb.Index = rtb.GetFirstCharIndexFromLine(iRowGo);
Util.Say(rtb.RowText);
}

if (menuItem == menuFileSetFavorite) {
sFile = child.File;
if (!sFile.Contains(@"\")) {
AddMessage("No disk file is open for this command!");
return;
}

// PRZELACZNIK, nie samo dodawanie (jego decyzja 31.08.2026): w pliku, ktory
// JUZ jest ulubiony, ten sam klawisz zdejmuje go z listy.  Obecnosc pliku
// rozstrzyga ReadValue z wartoscia domyslna, ktorej klucz nie moze przyjac -
// pusty ciag jest legalna wartoscia wpisu, wiec nie odroznia braku klucza.
//
// ZDJECIE Z ULUBIONYCH NIE RUSZA JUZ ZAKLADEK (jego decyzja z 01.09.2026:
// "Rozdzielic.  Zakladki a ulubione pliki nie sa powiazane w jedna i druga
// strone.  To osobne sprawy.").  Do 5.0.63 zakladki siedzialy w tej samej
// sekcji Favorites, wiec ten przelacznik kasowal je razem z ulubionym i musial
// o tym MOWIC; teraz maja wlasna sekcje Bookmarks i komunikat wraca do prostego
// "Removed from favorites", bo nic wiecej sie nie dzieje.
sText = App.ReadValue("Favorites", sFile, "\u0000");
if (sText != "\u0000") {
App.DeleteKey("Favorites", sFile);
AddMessage("Removed from favorites");
return;
}

HomerList hl = new HomerList("");
hl.Add("-1");
sText = (GetUserGuard(child) ? "G" : "M") + "|" + (string) Util.If(rtb.WordWrap, "W", "U");
hl.AddUniqueRange(sText);
sText = hl.Segments;
App.WriteValue("Favorites", sFile, sText);
AddMessage("Added to favorites");
}

if (menuItem == menuFileClearFavorite) {
sFile = child.File;
if (!sFile.Contains(@"\")) {
AddMessage("No disk file is open for this command!");
return;
}

// KOMENDA MILCZALA CALKOWICIE (naprawa 06.09.2026, przy jego zgloszeniu o
// mowie przelacznika).  Kasowala klucz i nie mowila nic, wiec na pliku
// ulubionym i na pliku, ktorego na liscie nigdy nie bylo, brzmiala
// IDENTYCZNIE - czyli nie brzmiala wcale.  Dla niewidomego cisza po
// nacisnieciu jest nierozroznialna od zepsutego klawisza.
// Obecnosc rozstrzyga ReadValue z wartoscia domyslna, ktorej klucz nie moze
// przyjac: pusty ciag jest legalna wartoscia wpisu, wiec nie odroznia braku.
sText = App.ReadValue("Favorites", sFile, "\u0000");
if (sText == "\u0000") {
AddMessage("Not in favorites");
return;
}

App.DeleteKey("Favorites", sFile);
AddMessage("Removed from favorites");
}

if (menuItem == menuFileListFavorites) {
aResults = App.ReadSectionKeys("Favorites");
List<string> list = new List<string>(aResults);
for (int i = list.Count - 1; i >=0; i--) {
string s = list[i];
if (File.Exists(s)) continue;
App.DeleteKey("Favorites", s);
list.RemoveAt(i);
}

aResults = list.ToArray();
if (aResults.Length == 0) {
AddMessage("No items!");
return;
}

string[] aDisplay = new string[aResults.Length];
for (int i = 0; i < aDisplay.Length; i++) aDisplay[i] = Path.GetFileName(aResults[i]);
sFile = Dialog.PickFile("List Favorites", aResults, aDisplay, true, 0, "Favorites");
if (sFile.Length == 0) return;

OpenOrActivateWindow(sFile, 0);
}

if (menuItem == menuNavigateGoToStartOfSelection) {
rtb.Index = rtb.StartSelection;
Util.Say(rtb.RowText);
}

if (menuItem == menuNavigateHomeCharacter) {
sText = rtb.RowText;
iLength = sText.TrimStart().Length;
if (iLength == 0) return;
iIndex = rtb.RowStart + (sText.Length - iLength);
rtb.Index = iIndex;
sText = rtb.GetRange(iIndex, iIndex + 1);
AddMessage(sText);
}

if (menuItem == menuNavigateEndCharacter) {
sText = rtb.RowText;
iLength = sText.TrimEnd().Length;
if (iLength == 0) return;
iIndex = rtb.RowStart + iLength - 1;
rtb.Index = iIndex;
sText = rtb.GetRange(iIndex, iIndex + 1);
AddMessage(sText);
}

if (menuItem == menuNavigateStartTag) {
iIndex = rtb.Index;
iEnd = rtb.Text.IndexOf(">", rtb.Index);
if (iEnd == -1) {
//AddMessage("Not found!");
//return;
iEnd = iIndex - 1;
}

iStart = 0;
iEnd++;
sText = rtb.GetRange(iStart, iEnd);
sMatch = @"</?\w+( |>)";
object[] a = Util.RegExpContainsLastCase(sText, sMatch);
iStart = (int) a[0];
if (iStart == -1) {
AddMessage("Not found!");
return;
}

string sWord = (string) a[1];
if (sWord.IndexOf("/") == -1) {
if (iStart == iIndex) {
if (iIndex > 0) iIndex = sText.Substring(0, iIndex - 1).LastIndexOf("<");
if (iStart == 0 || iIndex < 0) {
AddMessage("Not found!");
return;
}
}
else iIndex = iStart;
}

else {
sWord = "<" + sWord.Substring(2, sWord.Length - 3);
sMatch = sWord + "( |>)";
iIndex = (int) Util.RegExpContainsEquiv(rtb.Text, sMatch)[0];
if (iIndex == -1) {
AddMessage("Not found!");
return;
}

}

rtb.Index = iIndex;
sText = (string) GetChunk()[1];
Util.Say(sText);
}

if (menuItem == menuNavigateEndTag) {
iIndex = rtb.Index;
iEnd = rtb.Text.IndexOf(">", rtb.Index);
if (iEnd == -1) {
AddMessage("Not found!");
return;
}

iStart = 0;
iEnd++;
sText = rtb.GetRange(iStart, iEnd);
sMatch = @"</?\w+( |>)";
object[] a = Util.RegExpContainsLastCase(sText, sMatch);
iStart = (int) a[0];
if (iStart == -1) {
AddMessage("Not found!");
return;
}

string sWord = (string) a[1];
iStart += sWord.Length - 1;
if (sWord.IndexOf("/") >= 0) {
if (iStart == iIndex) {
if (iIndex < rtb.TextLength - 1) iIndex = rtb.Text.IndexOf(">", iIndex + 1);
if (iStart == rtb.TextLength - 1 || iIndex < 0) {
AddMessage("Not found!");
return;
}
}
else iIndex = iStart;
}

else {
sMatch = "</" + sWord.Substring(1, sWord.Length -2) + ">";
a = Util.RegExpContainsEquiv(rtb.Text, sMatch, iEnd);
iIndex = (int) a[0];
if (iIndex == -1) {
AddMessage("Not found!");
return;
}

iIndex += ((string) a[1]).Length - 1;
}

rtb.Index = iIndex;
sText = (string) GetChunk()[1];
Util.Say(sText);
}

// KOMENDA "Block" (Alt+B, mowila reszte biezacego bloku kodu) USUNIETA razem
// z nawigacja po blokach - Kasperczak, ustalenia edsharpng-4 i edsharpng-12.
// Pytanie o blok kodu traci sens w edytorze Markdown, a Alt+B jest teraz
// LISTA ZAKLADEK.  Alt+I (pytanie o wciecie) ZOSTAJE - wciecie ma w tekscie
// znaczenie takze poza kodem, a o nim nie rozstrzygal.

if (menuItem == menuNavigateRightBrace || menuItem == menuNavigateLeftBrace || menuItem == menuQueryBraces) {
if (rtb.Text.Length == 0) {
AddMessage("No text!");
return;
}

sText = App.ReadOption("BraceMatch", "{}");
string sLeft = sText.Substring(0, 1);
string sRight = sText.Substring(1, 1);
iIndex = rtb.Index;
string s = rtb.GetRange(iIndex, iIndex + 1);
switch (s) {
case "{" :
case "}" :
sLeft = "{";
sRight = "}";
break;
case "<" :
case ">" :
sLeft = "<";
sRight = ">" ;
break;
case "[" :
case "]" :
sLeft = "[";
sRight = "]";
break;
case "(" :
case ")" :
sLeft = "(";
sRight = ")";
break;
}

if (menuItem == menuNavigateRightBrace) {
iStart = iIndex;
iEnd = rtb.TextLength;
sText = rtb.GetRange(iStart, iEnd);
iCount = 0;
int i = 0;
// Dialog.Show(i, sText.Length);
bool bLoop = true;
while (bLoop) {
if (i == sText.Length) {
bLoop = false;
AddMessage("Not found!");
}
else if (sText.Substring(i, 1) == sLeft && i > 0) {
iCount++;
i++;
}
else if (sText.Substring(i, 1) == sRight && iCount > 0) {
iCount --;
i++;
}
else if (sText.Substring(i, 1) == sRight && iCount == 0 && i > 0) {
bLoop = false;
iIndex = iStart + i;
rtb.Index = iIndex;
}
else i++;
}
sText = (string) GetChunk()[1];
Util.Say(sText);
//Util.Say(rtb.RowText);
}
else if (menuItem == menuNavigateLeftBrace) {
iStart = 0;
iEnd = iIndex;
sText = rtb.GetRange(iStart, iEnd);
sText = Util.Reverse(sText);
iCount = 0;
int i = 0;
bool bLoop = true;
while (bLoop) {
if (i == sText.Length) {
bLoop = false;
AddMessage("Not found!");
}
else if (sText.Substring(i, 1) == sRight) {
iCount++;
i++;
}
else if (sText.Substring(i, 1) == sLeft && iCount > 0) {
iCount --;
i++;
}
else if (sText.Substring(i, 1) == sLeft && iCount == 0) {
bLoop = false;
iIndex = iEnd - i - 1;
rtb.Index = iIndex;
}
else i++;
}
//Util.Say(rtb.RowText);
sText = (string) GetChunk()[1];
Util.Say(sText);
}
else if (menuItem == menuQueryBraces) {
iStart = iIndex;
iEnd = rtb.TextLength;
sText = rtb.GetRange(iStart, iEnd);
iCount = 0;
int i = 0;
bool bLoop = true;
while (bLoop) {
if (i == sText.Length) {
bLoop = false;
}
else if (sText.Substring(i, 1) == sLeft && i > 0) {
iCount++;
i++;
}
else if (sText.Substring(i, 1) == sRight) {
iCount--;
i++;
}
else i++;
}
int iOutLevels = iCount;
iStart = 0;
iEnd = iIndex;
sText = rtb.GetRange(iStart, iEnd);
sText = Util.Reverse(sText);
iCount = 0;
i = 0;
bLoop = true;
while (bLoop) {
if (i == sText.Length) {
bLoop = false;
}
else if (sText.Substring(i, 1) == sRight) {
iCount++;
i++;
}
else if (sText.Substring(i, 1) == sLeft) {
iCount--;
i++;
}
else i++;
}
int iInLevels = iCount;
AddMessage(Util.Absolute(iInLevels) + " left");
AddMessage(Util.Absolute(iOutLevels) + " right");
}
}

// HANDLERY NAWIGACJI PO BLOKACH KODU (Next Block, Prior Block) USUNIETE
// razem z komendami - Kasperczak, ustalenie edsharpng-4.  Szly po wcieciach
// kodu, czego w Markdownie nie ma.  Control+B przejely ZAKLADKI.

if (menuItem == menuNavigateNextIndent) {
string sComment = App.ReadOption("QuotePrefix", "> ");
//sComment = Util.Literalize(sComment);
int iLevels = GetIndent();
int i = iLevels;
int iRow = rtb.Row;
int iBottom = rtb.BottomRow;
//rtb.BeginUpdate();
while (iRow < iBottom) {
iRow++;
rtb.Row = iRow;
sLine = rtb.RowText.Trim();
if (sLine.Length == 0 || sLine.StartsWith(sComment)) continue;
i = GetIndent();
if (iLevels != i) break;
}
//rtb.EndUpdate();

if (iLevels == i) {
AddMessage("Bottom!");
rtb.Index = rtb.TextLength;
}
else AddMessage(GetDelta(iLevels, i));
//Util.Say(rtb.RowText);
Util.Say(rtb.RowText);
}

if (menuItem == menuNavigatePriorIndent) {
string sComment = App.ReadOption("QuotePrefix", "> ");
//sComment = Util.Literalize(sComment);
int iLevels = GetIndent();
int i = iLevels;
int iRow = rtb.Row;
int iTop = 0;
//rtb.BeginUpdate();
while (iRow > iTop) {
iRow--;
rtb.Row = iRow;
sLine = rtb.RowText.Trim();
if (sLine.Length == 0 || sLine.StartsWith(sComment)) continue;
i = GetIndent();
if (iLevels != i) break;
}
//rtb.EndUpdate();

if (iLevels == i) {
AddMessage("Top!");
rtb.Index = 0;
}
else AddMessage(GetDelta(iLevels, i));
//Util.Say(rtb.RowText);
Util.Say(rtb.RowText);
}

if (menuItem == menuNavigateNextChunk) {
NavigateNextMatch(App.MatchChunk);
}

if (menuItem == menuNavigatePriorChunk) {
NavigatePriorMatch(App.MatchChunk);
}

if (menuItem == menuNavigateNextSentence) {
// Alt with Down is a screen reader sentence command too, and the reader
// speaks the whole destination itself -- we only move the caret.
NavigateNextMatchReaderSaysAll(App.MatchSentence);
}

if (menuItem == menuNavigatePriorSentence) {
// Alt with Up is a screen reader sentence command too, and the reader
// speaks the whole destination itself -- we only move the caret.
NavigatePriorMatchReaderSaysAll(App.MatchSentence);
}

if (menuItem == menuNavigateNextParagraph) {
// Control with Up/Down is a screen reader paragraph command too, so the
// reader speaks the destination itself -- we only move the caret.
NavigateNextMatch(App.MatchParagraph, false, true);
}

if (menuItem == menuNavigatePriorParagraph) {
// Control with Up/Down is a screen reader paragraph command too, so the
// reader speaks the destination itself -- we only move the caret.
NavigatePriorMatch(App.MatchParagraph, false, true);
}

// A section in this fork is a Markdown heading, not the original
// form-feed section break.  Kasperczak reported the old behaviour as a
// regression from EdSharp 4 (Telegram 14.08.2026 18:26: "Control-PageUp-
// PageDown to byla nawigacja po sekcjach, czyli nawigacja po naglowkach")
// and confirmed it should walk headings of EVERY level (18:34).  This
// reuses the same fence-aware parser as Ctrl+Alt+Up/Down section move, so
// all three commands agree on what a section is.
if (menuItem == menuNavigateNextSection) {
GoToAdjacentMarkdownHeading(rtb, false);
}

if (menuItem == menuNavigatePriorSection) {
GoToAdjacentMarkdownHeading(rtb, true);
}

if (menuItem == menuNavigateNextSectionSameLevel) {
GoToAdjacentMarkdownHeading(rtb, false, true);
}

if (menuItem == menuNavigatePriorSectionSameLevel) {
GoToAdjacentMarkdownHeading(rtb, true, true);
}

if (menuItem == menuNavigateDocumentNavigation) {
ShowDocumentNavigationTree(rtb);
}

if (menuItem == menuNavigateGoToContents) {
GoToMarkdownContentsOrHeading(rtb);
}

if (menuItem == menuNavigateNextEmphasis || menuItem == menuNavigatePriorEmphasis) {
GoToMarkdownEmphasis(rtb, menuItem == menuNavigateNextEmphasis);
}

if (menuItem == menuNavigateNextList || menuItem == menuNavigatePriorList) {
GoToMarkdownList(rtb, menuItem == menuNavigateNextList);
}

// Lista linkow pod Control+F6 (ustalenia edsharpng-37, 38 i 47).
//
// JEDNA bramka, nie trzy.  Podglad odpada, bo w nim kursor chodzi po tekscie
// PRZETWORZONYM i przesuniecia odsylaczy z pliku zrodlowego nie maja tam
// pokrycia; do tego podglad ma juz wlasna liste elementow z filtrem Links.
// Bramki "tylko pliki Markdown" tu NIE MA, choc jest przy wstawianiu linku,
// tabeli, przypisie i komentarzu - i to jest swiadoma roznica: tamte komendy
// PISZA skladnie Markdown, ktora w zwyklym pliku bylaby smieciem, a ta tylko
// CZYTA i goly adres w pliku .txt jest prawdziwym odsylaczem.
if (menuItem == menuNavigateLinkList) {
if (child == null) return;
if (child.MarkdownReviewMode) {
AddMessage("Close the preview first!");
return;
}
ShowMarkdownLinkList(rtb);
}

// SKOK PO ODNOSNIKACH pod Alt+PageDown i Alt+PageUp (ustalenie edsharpng-36).
// BRAMKI DOKLADNIE TE SAME, CO PRZY LISCIE ODNOSNIKOW, i to jest wymog, nie
// wygoda: obie komendy czytaja ten sam parser i te same odsylacze, wiec gdyby
// jedna dzialala w pliku .txt, a druga nie, uzytkownik dostawal by dwa rozne
// obrazy tego samego dokumentu.  Bramki "tylko Markdown" tu wiec NIE MA
// (swiadoma roznica wobec Control+K, ustalenie edsharpng-49), a podglad odpada,
// bo tam kursor chodzi po tekscie PRZETWORZONYM i przesuniecia z pliku
// zrodlowego nie maja pokrycia.
if (menuItem == menuNavigateNextLink || menuItem == menuNavigatePriorLink) {
if (child == null) return;
if (child.MarkdownReviewMode) {
AddMessage("Close the preview first!");
return;
}
GoToMarkdownLink(rtb, menuItem == menuNavigateNextLink);
}

if (menuItem == menuQueryAddress) {
if (this.KeyRepeat % 2 == 0) SetStatusAddress(null, null);
else if (App.ReadOption("HardPageAddress", "N").ToLower().Substring(0, 1) != "y") AddMessage(GetPageAddress(rtb));
else AddMessage(GetPercentAddress(rtb));
}

if (menuItem == menuQueryIndent) {
// Jared request to always say levels
//if (!rtb.IndentMode)  AddMessage("Level " + this.GetIndent());
// else {
if (this.KeyRepeat % 2 == 0) {
AddMessage("Level " + this.GetIndent());
return;
}
// AddMessage("Block");

char[] a = {' ', '\t'};
sLine = "";
string sComment = App.ReadOption("QuotePrefix", "> ");
int iLevels = GetIndent();
int i = iLevels;
int iRow = rtb.Row;
int iTop = 0;
while (iRow > iTop) {
iRow--;
sLine = rtb.GetRowText(iRow).Trim(a);
if (sLine.Length == 0 || sLine.StartsWith(sComment)) continue;
i = GetIndent(iRow);
// Util.Say("row " + iRow + " indent " + i);
// Only stop for less indentation
// if (iLevels != i) break;
if (iLevels > i) break;
}

if (iLevels == i) {
//AddMessage("Top!");
}
//else AddMessage(GetDelta(iLevels, i));
Util.Say(sLine);
// }
}

if (menuItem == menuQueryPath) {
sText = child.File;
if (this.KeyRepeat % 2 == 0) AddMessage(sText);
else {
Util.Spell(sText);
}
}

if (menuItem == menuQueryTopic) {
sText = rtb.Text;
iIndex = sText.IndexOf(LB, rtb.Index);
if (iIndex == -1) {
AddMessage("Not found!");
return;
}
iEnd = iIndex + LB.Length;
sText = sText.Substring(0, iEnd);
iIndex = sText.LastIndexOf(SB);
if (iIndex == -1) {
AddMessage("Not found!");
return;
}
iStart = iIndex + SB.Length;
iIndex = sText.IndexOf(LB, iStart);
if (iIndex == -1) {
AddMessage("Not found!");
return;
}
iEnd = iIndex + LB.Length;
sLine = sText.Substring(iStart, iEnd - iStart);
sText = sLine;
if (this.KeyRepeat % 2 == 0) AddMessage(sText);
else {
Util.Spell(sText);
}
}

if (menuItem == menuQueryYield) {
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
if (iStart == iEnd) {
AddMessage("All");
iStart = 0;
iEnd = rtb.TextLength;
}
else  AddMessage("Selected");

sText = rtb.GetRange(iStart, iEnd);
iResult = sText.Length;
AddMessage(Util.Pluralize(iResult, "character"));

if (iResult > 0) {
iResult = Util.RegExpCountCase(sText, "\\S+");
AddMessage(Util.Pluralize(iResult, "Word"));
iResult = Util.RegExpCountCase(sText, LB) + 1;
AddMessage("\t" + Util.Pluralize(iResult, "Line"));
}

}

if (menuItem == menuQueryStatus) {
if (this.KeyRepeat % 2 == 0) {
sText = rtb.Modified ? "Modified" : "Unmodified" + "\t";
sText += rtb.WordWrap ? "Wrap" : "Unwrap";
sText += rtb.ReadOnly ? "Guard" : "";
//sText += App.ReadData("Compiler", "Default");
}
else {
if (child == null || child.YieldEncoding == null) sText = "No disk file with encoding information is open!";
else if (child.IsUnixLineBreak) sText = "Encoding Unicode (UTF-8N)";
else sText = "Encoding " + child.YieldEncoding.EncodingName + " = " + child.YieldEncoding.CodePage;
// RODZAJ KONCOW WIERSZA doklada sie do tej samej wypowiedzi na jego prosbe
// (28.08.2026: "Przy Alt+Z tez konce wiersza moga byc czytane").  Liczony z
// tekstu WCZYTANEGO z dysku, bo kontrolka edycyjna trzyma w pamieci same LF -
// zmierzone.  Dlatego pytamy o rodzaj ODCZYTANY przy otwarciu pliku.
if (child != null && child.FileLineBreakKind.Length > 0)
sText += ", line breaks " + child.FileLineBreakKind;
}
AddMessage(sText);
}

if (menuItem == menuQueryCompiler) {
AddMessage("Compiler " + App.ReadData("Compiler", "Default"));
AddMessage("Folder " + Path.GetFileName(Directory.GetCurrentDirectory()));
}

if (menuItem == menuQuerySelected) {
sText = rtb.SelectedText;
if (sText.Length == 0) sText = "No text!";
if (this.KeyRepeat % 2 == 0) AddMessage(sText);
else {
Util.Spell(sText);
}
}

if (menuItem == menuQueryChunk) {
object[] a = GetChunk();
sText = (string) a[1];
if (sText.Length == 0) sText = "No text!";
if (this.KeyRepeat % 2 == 0) AddMessage(sText);
else {
Util.Spell(sText);
}
}

if (menuItem == menuQueryReadAll) {
sText = rtb.Text;
if (sText.Length == 0) sText = "No text!";
sText = Util.ConvertQuotes(sText);
if (this.KeyRepeat % 2 == 0) AddMessage(sText);
else {
Util.Spell(sText);
}
}

if (menuItem == menuQueryClipboard) {
sText = Util.GetClipboardText();
if (sText.Length == 0) sText = "No text!";
if (this.KeyRepeat % 2 == 0) AddMessage(sText);
else {
Util.Spell(sText);
// SetStatus(sText);
}
}

if (menuItem == menuQueryTime || menuItem == menuMiscInsertTime) {
string sDate, sTime;
GetDateAndTime(out sDate, out sTime);
//sText = dt.ToShortTimeString() + " on " + dt.ToLongDateString();
// sText = sTime + " on " + sDate;
sText = sTime;
if (sTime.Length > 0 && sDate.Length > 0) sText += " ";
sText += sDate;
if (menuItem == menuQueryTime) {
if (sTime.Length > 0) AddMessage(sTime);
if (sDate.Length > 0) AddMessage(sDate);
}
else {
rtb.ReplaceRange(rtb.Index, rtb.Index, sText);
Util.Say(rtb.RowText);
}
}

if (menuItem == menuQueryStyles) {
sText = GetStyleText() + " ";
sText += GetJustifyText() + " ";
sText += GetBaselineText() + " ";
sText = sText.Replace("Regular ", "");
sText = sText.Replace("Left ", "");
sText = sText.Replace("Flat ", "");
if (sText.Trim().Length == 0) sText = "Default";
AddMessage(sText);
}

if (menuItem == menuQueryFont) {
sText = GetFontText(rtb.SelectionFont, rtb.SelectionColor);
AddMessage(sText);
}

if (menuItem == menuMiscPreviewMarkdownBrowser) {
// Render the current document as HTML and open it in the default web
// browser, via a temporary file that EdSharp deletes at exit. The point is
// the screen reader's virtual buffer: in the browser, NVDA and JAWS move by
// heading (H), link (K) and table (T) using their own keys, which the
// editor window cannot offer. This does NOT replace Markdown Review on
// Escape -- that one keeps the cursor in the source and can stay synced.
// Both were kept side by side deliberately (Kasperczak, 20 August 2026).
if (rtb.TextLength == 0) {
AddMessage("Document is empty, nothing to preview");
return;
}

string sPreviewName = (child.File != null && child.File.Contains("\\")) ? Path.GetFileNameWithoutExtension(child.File) : "Untitled";
string sPreviewHtml = MarkdownDocumentToHtml(rtb.Text, sPreviewName);
if (sPreviewHtml.Length == 0) {
AddMessage("Could not convert this document to HTML");
return;
}

try {
string sPreviewFile = Path.Combine(Path.GetTempPath(), "edsharp_preview_" + Guid.NewGuid().ToString("N") + ".htm");
Util.String2FileU(sPreviewHtml, sPreviewFile);
App.TempFiles.Add(sPreviewFile);
Process.Start(sPreviewFile);
AddMessage("Markdown preview opened in web browser");
}
catch (Exception exPreview) {
AddMessage("Could not open the preview: " + exPreview.Message);
}

return;
}

if (menuItem == menuMiscTextCombine) {
List<string> list = new List<string>();
aResults = rtb.Lines;
string sDir = Directory.GetCurrentDirectory();
string sTempDir = "";
for (int i = 0; i < aResults.Length; i++) {
string s = aResults[i].Trim();
if (s.Length == 0) continue;
// PRZYCZYNA "Unexpected Event", ktore zglosil Kasperczak 29.08.2026 01:24.
// Ta petla traktuje KAZDY wiersz dokumentu jako sciezke pliku, a
// Path.GetDirectoryName RZUCA ArgumentException na tresci, ktora sciezka
// nie jest.  Zmierzone: zdanie z cudzyslowem ("On powiedzial "tak"") oraz
// WIERSZ TABELI MARKDOWN ("| Imie | Wiek |") wywalaja te metode - czyli
// komenda przewracala sie na dokumencie, ktory sami uczymy go tworzyc
// kreatorem tabeli.
// Wiersz, ktory nie jest sciezka, po prostu POMIJAMY: to lista plikow, a
// nie tekst, wiec zdanie w niej i tak nie ma sensu.  Komenda konczy sie
// wtedy komunikatem "No files found!", zamiast rzucac okno bledu.
// Bramka zostaje TAKZE po calkowitym usunieciu Text Convert (30.08.2026):
// ten sam kod obsluguje Text Combine, ktora w menu ZOSTAJE na jego zyczenie,
// a wiersz niebedacy sciezka wywracal ja dokladnie tak samo.
try {sTempDir = Path.GetDirectoryName(s);}
catch {continue;}
if (sTempDir == null) continue;
if (sTempDir.Length == 0) s = Path.Combine(sDir, s);
else if (Directory.Exists(sTempDir)) sDir = sTempDir;
try {if (File.Exists(s)) list.Add(s);}
catch {continue;}
}

aResults = list.ToArray();
if (aResults.Length == 0) {
AddMessage("No files found!");
return;
}

sText = Util.GetExtensions(aResults);
sResult = Dialog.Input("Filter", "Extensions", sText).Trim();
if (sResult.Length == 0) return;

aResults = Util.GetPathsWithExtensions(aResults, sResult);
if (aResults.Length == 0) {
AddMessage("No files!");
return;
}

StringBuilder sb = new StringBuilder();
iCount = 0;
AddMessage("Converting");
for (int i = 0; i < aResults.Length; i++) {
string sSource = aResults[i];
string sName = Path.GetFileName(sSource);
AddMessage(sName);
//sText = COM.WordFile2String(sSource);
//sText = COM.ConvertFile2String(sSource);
int iConvert = 2;
string sTargetExt = "txt";
bool bTextOnly = true;
sText = COM.ConvertFile2String(sSource, ref iConvert, ref sTargetExt, bTextOnly);
if (sText.Length == 0) {
AddMessage("Error!");
continue;
}

iCount++;
if (iCount == 1) sb.Append(sName + LB + LB + sText);
else sb.Append(SectionBreak + sName + LB + LB + sText);
}

AddMessage("Converted " + Util.Pluralize(iCount, "file"), true);
if (iCount == 0) return;

if (!IsEmptyWindow()) new MdiChild(this);
sText = sb.ToString();
sText += EOD;
rtb = this.Child.RTB;
rtb.Text = sText;
rtb.Modified = false;
}

if (menuItem == menuMiscTableOfContents) {
if (child == null) return;
// Komenda PRZEPISUJE dokument, wiec ma bramki wymagane po utracie pliku
// Kasperczaka przy Format Code: bramka na typ pliku, zadnego zapisu na
// dysk i podmiana COFALNA przez ReplaceRange (Control+Z dziala).
if (!MarkdownReview_IsMarkdownFile(child.File)) {
AddMessage("Table of Contents works only on Markdown files!");
return;
}
if (child.MarkdownReviewMode) {
AddMessage("Close the preview first!");
return;
}
if (rtb.ReadOnly) {
AddMessage("Document is guarded!");
return;
}
WriteMarkdownContents(rtb);
}

if (menuItem == menuMiscInsertFootnote || menuItem == menuMiscGoToFootnote || menuItem == menuMiscNextFootnote || menuItem == menuMiscPriorFootnote || menuItem == menuMiscFootnoteList || menuItem == menuMiscExportFootnotes) {
if (child == null) return;
// MOWA BRAMEK TEZ MUSI OMINAC TLUMIENIE PRZY CONTROL+ALT (30.08.2026).
// Skok kontekstowy przeniosl sie na Control+Alt+K, a Util.Say milczy, gdy te
// dwa modyfikatory sa trzymane.  Gdybym poprawil tylko wypowiedzi udanego
// skoku, ODMOWY zamilkly by po cichu: niewidomy nacisnalby klawisz w pliku
// .txt albo w podgladzie i nie uslyszal NICZEGO, czyli dostal by najgorszy
// wariant - brak skutku bez powodu.  Pozostale komendy tej rodziny
// (Control+Shift+K, Alt+K, eksport z menu) nie maja Control+Alt, wiec tryb
// globalny jest im obojetny; wlaczam go po chordzie WOLAJACEGO, nie na
// wszystkich hurtem.  Skok do POPRZEDNIEGO przypisu ma Control+Alt+Shift+K,
// czyli te same dwa modyfikatory plus Shift - warunek tlumienia w Util.Say
// patrzy na Control i Alt i Shifta nie sprawdza, wiec on tez potrzebuje
// trybu globalnego.  To wlasnie ten przypadek, ktory zielony build
// przepuszcza, a niewidomy odbiera jako martwy klawisz.
// SKOK PO PRZYPISACH MA TERAZ Control+Alt+PageUp/PageDown, czyli DALEJ te dwa
// modyfikatory, ktore tlumia Util.Say - tryb globalny obowiazuje wiec nadal
// dla obu klawiszy tej pary, a nie tylko dla dawnego Control+Alt+Shift+K.
bool bGlobalSpeech = (menuItem == menuMiscGoToFootnote || menuItem == menuMiscNextFootnote || menuItem == menuMiscPriorFootnote);
// Bramka na typ pliku: przypis markdownowy w pliku .txt bylby smieciem,
// ktorego nic nie zrozumie.  Ta sama zasada, co przy spisie tresci.
if (!MarkdownReview_IsMarkdownFile(child.File)) {
AddMessage("Footnotes work only on Markdown files!", bGlobalSpeech);
return;
}
if (child.MarkdownReviewMode) {
AddMessage("Close the preview first!", bGlobalSpeech);
return;
}
if (menuItem == menuMiscInsertFootnote) {
// Wstawianie zmienia dokument, wiec dodatkowo nie moze ruszyc dokumentu
// zabezpieczonego.  Skoki i lista sa tylko do czytania i tej bramki nie
// potrzebuja - dzialaja tez na zabezpieczonym pliku.
if (rtb.ReadOnly) {
AddMessage("Document is guarded!");
return;
}
InsertMarkdownFootnote(rtb);
}
else if (menuItem == menuMiscGoToFootnote) GoToMarkdownFootnoteOrBack(rtb);
else if (menuItem == menuMiscNextFootnote) GoToMarkdownFootnoteMarker(rtb, true);
else if (menuItem == menuMiscPriorFootnote) GoToMarkdownFootnoteMarker(rtb, false);
// Eksport NIE rusza dokumentu otwartego - pisze w NOWYM oknie, wiec bramka
// dokumentu zabezpieczonego go nie dotyczy.  Zrodlo czyta tylko do czytania.
else if (menuItem == menuMiscExportFootnotes) ExportMarkdownFootnotes(rtb);
else ShowMarkdownFootnoteList(rtb);
}

if (menuItem == menuMiscInsertComment || menuItem == menuMiscNextComment
|| menuItem == menuMiscPriorComment || menuItem == menuMiscCommentList) {
if (child == null) return;
// Te same dwie bramki, co przy przypisach, tabelach i spisie tresci: komentarz
// w skladni Markdown ma sens tylko w pliku Markdown (w .txt bylby widocznym
// smieciem), a w podgladzie nie ma gdzie pisac.
if (!MarkdownReview_IsMarkdownFile(child.File)) {
AddMessage("Comments work only on Markdown files!");
return;
}
if (child.MarkdownReviewMode) {
AddMessage("Close the preview first!");
return;
}
if (menuItem == menuMiscInsertComment) {
// Bramka na dokument zabezpieczony dotyczy TYLKO wstawiania i poprawiania:
// skoki i lista sa do czytania, wiec dzialaja tez na zabezpieczonym pliku.
if (rtb.ReadOnly) {
AddMessage("Document is guarded!");
return;
}
InsertOrEditMarkdownComment(rtb);
}
else if (menuItem == menuMiscNextComment) GoToMarkdownComment(rtb, true);
else if (menuItem == menuMiscPriorComment) GoToMarkdownComment(rtb, false);
else ShowMarkdownCommentList(rtb);
}

if (menuItem == menuMiscInsertLink) {
if (child == null) return;
// Te same trzy bramki, co przy tabeli, przypisie i spisie tresci: skladnia
// Markdown ma sens tylko w pliku Markdown, w podgladzie nie ma gdzie pisac,
// a dokument zabezpieczony jest do czytania.
if (!MarkdownReview_IsMarkdownFile(child.File)) {
AddMessage("Links work only on Markdown files!");
return;
}
if (child.MarkdownReviewMode) {
AddMessage("Close the preview first!");
return;
}
if (rtb.ReadOnly) {
AddMessage("Document is guarded!");
return;
}
InsertMarkdownLink(rtb);
}

// CSV JAKO TABELA (zadanie 10, 5.0.87). Dziala na pliku, ktory jest otwarty
// w edytorze - i wymaga, by byl zapisany na dysku, bo tabela czyta i
// nadpisuje PLIK, nie tresc w oknie. Niezapisane zmiany pytamy o zapis
// najpierw, inaczej tabela pokazalaby stara tresc.
if (menuItem == menuMiscCsvTable) {
if (child == null) return;
if (child.File == null || child.File.Length == 0) {
AddMessage("Save this file first, then it can be edited as a table.");
return;
}
if (rtb != null && rtb.Modified) {
if (Dialog.Confirm("CSV Table", "This file has unsaved changes.  Save them now and open it as a table?", "Y") != "Y") return;
child.SaveTextOrRtfFile(child.File);
if (rtb != null) rtb.Modified = false;
}
EditCsvAsTable(child.File);
// Tresc pliku na dysku mogla sie zmienic w tabeli - okno w edytorze
// pokazywalo by wtedy stara. Wczytanie na nowo zdejmuje ta rozbieznosc.
if (File.Exists(child.File)) {
child.LoadTextOrRtfFile(child.File, true);
if (rtb != null) rtb.Modified = false;
}
return;
}

if (menuItem == menuMiscInsertTable) {
if (child == null) return;
// Te same dwie bramki, co przy przypisach i spisie tresci: tabela w skladni
// Markdown ma sens tylko w pliku Markdown, a w podgladzie nie ma gdzie pisac.
if (!MarkdownReview_IsMarkdownFile(child.File)) {
AddMessage("Tables work only on Markdown files!");
return;
}
if (child.MarkdownReviewMode) {
AddMessage("Close the preview first!");
return;
}
if (rtb.ReadOnly) {
AddMessage("Document is guarded!");
return;
}
RunMarkdownTableWizard(rtb);
}

// LISTY POD LITERA L.  Bramka na blok kodu jest tu KONIECZNA, choc przy tabeli
// jej nie ma: w bloku kodu wiersz zaczynajacy sie od myslnika jest TRESCIA
// programu, a nie pozycja listy, wiec dolozenie punktora zmienialoby kod, ktory
// user pisze.  Bramki na typ pliku i podglad jak przy tabeli.  W pliku RTF obie
// komendy dzialaja na prawdziwym formatowaniu bogatego tekstu - to sciezka,
// ktora metody juz mialy, i zostaje nietknieta.
if (menuItem == menuMiscBulletList || menuItem == menuMiscNumberedList) {
if (child == null) return;
bool bNumbered = (menuItem == menuMiscNumberedList);
if (!IsRichTextFile(child)) {
if (!MarkdownReview_IsMarkdownFile(child.File)) {
AddMessage("Lists work only on Markdown files!");
return;
}
if (child.MarkdownReviewMode) {
AddMessage("Close the preview first!");
return;
}
}
if (rtb.ReadOnly) {
AddMessage("Document is guarded!");
return;
}
if (!IsRichTextFile(child)) {
string sFenceText = rtb.Text;
int iCur = rtb.Index;
if (iCur < 0) iCur = 0;
if (iCur > sFenceText.Length) iCur = sFenceText.Length;
if (IsMarkdownIndexInFence(MarkdownReview_FindFenceRanges(sFenceText), iCur)) {
AddMessage("Cannot make a list inside a code block!");
return;
}
}
if (bNumbered) ToggleNumberedListShortcut(child);
else ToggleBulletListShortcut(child);
}

if (menuItem == menuMiscSetDefaultFont) {
object[] a = Dialog.GetFont(rtb.Font, rtb.ForeColor);
if (a.Length == 0) return;

rtb.Font = (Font) a[0];
rtb.ForeColor = (Color) a[1];
string sFont = GetFontText(rtb.Font, rtb.ForeColor);
App.WriteOption("FontDefault", sFont);
}

if (menuItem == menuMiscConfigurationOptions) {
aResults = App.ReadDefaultOptions();
//Array.Sort(aResults);
aLabels = new string[aResults.Length];
string[] aDefaults = new string[aResults.Length];
aValues = new string[aResults.Length];
for (int i = 0; i < aResults.Length; i++) {
aLabels[i] = (aResults[i].IndexOf("&") >= 0 ? "" : "&") + aResults[i];
aDefaults[i] = App.ReadDefaultOption(aResults[i], "");
aValues[i] = App.ReadOption(aResults[i], aDefaults[i]);
}

string[] a = Dialog.MultiInput("Configuration Options", aLabels, aValues);
if (a.Length == 0) return;
for (int i = 0; i < a.Length; i++) App.WriteOption(aResults[i], a[i]);
}

if (menuItem == menuMiscManualOptions) {
//OpenOrActivateWindow(App.IniFile, 0);
string sCompiler = App.ReadData("Compiler", "Default");
//sText = sCompiler + " Compiler";
//sResult = Dialog.Choose("Manual Options", "", new string[] {"&Main", "&" + sText}, 0);
sResult = Dialog.Choose("Manual Options", "", new string[] {"&Main", "&" + sCompiler}, 0);
if (sResult.Length == 0) return;

if (sResult == "&Main") sFile = App.IniFile;
else sFile = Path.Combine(App.DataDir, sCompiler + ".ini");
OpenOrActivateWindow(sFile, 0);
}

if (menuItem == menuMiscResetConfiguration) {
/*
if (Dialog.Confirm("Confirm", "Reset Configuration to Default?", "Y") == "Y") {
System.IO.File.Delete(App.IniFile);
App.SetConfigurationValues();
*/

string sCompiler = App.ReadData("Compiler", "Default");
//sText = sCompiler + " Compiler";
//sResult = Dialog.Choose("Manual Options", "", new string[] {"&Main", "&" + sText}, 0);
sResult = Dialog.Choose("Reset Configuration", "", new string[] {"&Main", "&" + sCompiler, "&Both", "&New"}, 0);
if (sResult.Length == 0) return;

if (sResult == "&Main" || sResult == "&Both") {
if (System.IO.File.Exists(App.IniFile)) System.IO.File.Delete(App.IniFile);
System.IO.File.Copy(App.DefaultIniFile, App.IniFile);
}

if (sResult == sCompiler || sResult == "&Both") {
sFile = Path.Combine(App.DataDir, App.ReadData("Compiler", "Default") + ".ini");
if (System.IO.File.Exists(sFile)) System.IO.File.Delete(sFile);
}

// MARTWE POLE USTAWIEN "NavigatePart" (zmierzone 28.08.2026).  Kasperczak
// zapytal, czy nawigacja po czesciach na pewno zniknela: "A moze jest, ale nie
// w menu w klawiszach, a tylko w programie".  Mial racje.  Komendy Go to Part,
// Next Part i Prior Part sa usuniete z menu, z klawiszy, z Hotkeys.ini i z
// binarki (zmierzone sonda na .exe, z kontrola pozytywna), ALE to pole nadal
// pyta uzytkownika o wzorzec i zapisuje go do sekcji Options - a nikt go juz
// nie czyta.  NIE USUWAM go tutaj samodzielnie z dwoch powodow: pozycja w tej
// tablicy wyznacza INDEKS w spakowanej tyldami wartosci ustawien kompilatora
// (nizej: a[3]), wiec skrocenie listy przesunelo by QuotePrefix i pozostale
// pola w konfiguracji, ktora uzytkownik JUZ ma na dysku.  Sprzatniecie nalezy
// do punktu 5 mapy drogowej (uporzadkowanie programu) i wymaga jego decyzji
// oraz przepisania istniejacych wpisow, a nie samego skrocenia tablicy.
if (sResult == "&New") {
aLabels = new string[] {"&Name", "&CompileCommand", "&JumpPosition", "&AbbreviateOutput", "&NavigatePart", "&QuotePrefix", "&ExtensionDefault", "&GoToEnvironment"};
aValues = new string[] {"", "", "", "", "", "", "", ""};
aResults = Dialog.MultiInput("Create Compiler setting", aLabels, aValues);
if (aResults.Length == 0) return;

sCompiler = aResults[0];
HomerList hl = new HomerList(aResults);
hl.RemoveAt(0);
string sSetting = hl.GetSegments('~');
App.WriteValue("Compilers", sCompiler, sSetting);
}
AddMessage("Done");
return;
}

if (menuItem == menuMiscGoToFolder) {
string sDir;
HomerList hl = new HomerList();
aResults = App.ReadSectionKeys("Recent");
foreach (string s in aResults) {
sDir = Path.GetDirectoryName(s);
if (!hl.Contains(sDir)) hl.Add(sDir);
}

aResults = App.ReadSectionKeys("Favorites");
foreach (string s in aResults) {
sDir = Path.GetDirectoryName(s);
if (!hl.Contains(sDir)) hl.Add(sDir);
}

if (hl.Count == 0) {
AddMessage("No items!");
return;
}

aResults = hl.ToArray();
string[] aDisplay = new string[aResults.Length];
for (int i = 0; i < aDisplay.Length; i++) aDisplay[i] = Path.GetFileName(aResults[i]);
sDir = Dialog.Pick("Go to Folder", aResults, aDisplay, true, 0);
if (sDir.Length == 0) return;

Directory.SetCurrentDirectory(sDir);
}

if (menuItem == menuMiscGoToSpecialFolder) {
string sDir = PickSpecialFolder();
if (sDir.Length == 0) return;

Directory.SetCurrentDirectory(sDir);
}

if (menuItem == menuMiscWordWrap) {
rtb.SetWrap(true);
SetRecent(child.File);
}

if (menuItem == menuMiscUnwrap) {
rtb.SetWrap(false);
SetRecent(child.File);
}

if (menuItem == menuMiscPathToClipboard) {
sText = child.File;
Util.SetClipboardText(sText);
AddMessage(sText);
}

if (menuItem == menuMiscPathList) {
sTitle = "Open Folder";
string sDir = Dialog.OpenFolder(sTitle, "Name", Directory.GetCurrentDirectory());
if (sDir.Length == 0) return;

Directory.SetCurrentDirectory(sDir);

sText = Util.GetExtensions(sDir);
if (sText.Length == 0) {
AddMessage("No files!");
return;
}

sResult = Dialog.Input("Filter", "Extensions", sText);
if (sResult.Length == 0) return;

aResults = Util.GetPathsWithExtensions(Directory.GetFiles(sDir), sResult);
iLength = aResults.Length;
sText = Util.Pluralize(iLength, "file");
AddMessage(sText);

if (!IsEmptyWindow()) new MdiChild(this);
child = this.Child;
rtb = child.RTB;
for (int i = 0; i < iLength; i++) {
if (i == 0) sText = aResults[i] + "\n";
else sText += Path.GetFileName(aResults[i]) + "\n";
}
rtb.Text = sText;
rtb.Modified = true;
}

if (menuItem == menuMiscExplorerFolder) {
string sDir = GetDirChoice();
if (sDir.Length == 0) return;
ExplorerFolder(sDir);
}

if (menuItem == menuMiscEvaluateExpression) {
if (rtb.SelectionLength == 0) {
AddMessage("Line");
sText = rtb.RowText;
iIndex = rtb.RowStart + sText.Length;
}
else {
AddMessage("Selected");
sText = rtb.SelectedText;
iIndex = rtb.SelectionStart + sText.Length;
}

sText = Script.run(sText);
if (sText.Length == 0) return;

sText = LB + sText;
rtb.ReplaceRange(iIndex, iIndex, sText);
rtb.Index = iIndex + 1;
Util.Say(rtb.RowText);
}

if (menuItem == menuMiscReplaceTokens) {
if (rtb.SelectionLength == 0) {
//AddMessage("Chunk");
object[] a = GetChunk();
iStart = (int) a[0];
sText = (string) a[1];
iEnd = iStart + sText.Length;
}
else {
AddMessage("Selected");
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
}
sText = rtb.GetRange(iStart, iEnd);

if (rtb.SelectionLength == 0 && !sText.StartsWith("%")) {
AddMessage("Replace Snippet");
aResults = GetSnippetFiles(out aValues);
HomerList hlResults = new HomerList(aResults);
HomerList hlValues = new HomerList(aValues);
//sMatch = @"^" + sText + @".*";
sMatch = sText;
Regex rx = new Regex(sMatch, RegexOptions.IgnoreCase);
iCount = hlResults.Count;
for (int i = iCount - 1; i >= 0; i--) {
if (rx.IsMatch(aValues[i])) continue;
hlResults.RemoveAt(i);
hlValues.RemoveAt(i);
}

if (hlResults.Count == 0) {
AddMessage("No match!");
return;
}

aResults = hlResults.ToArray();
aValues = hlValues.ToArray();

string sSnippet;
if (aResults.Length == 1) sSnippet = aResults[0];
else {
sSnippet = Dialog.Pick("Pick", aResults, aValues, false, 0);
if (sSnippet.Length == 0) return;
}

InvokeSnippet(sSnippet, sText, iStart, iEnd);
}
else {
if (rtb.SelectionLength == 0) AddMessage("Replace Token");
else AddMessage("Replace Selected Tokens");
string sTemp = sText;
sText = ReplaceTokens(sText);
if (sText == sTemp) {
AddMessage("No match!");
return;
}

rtb.ReplaceRange(iStart, iEnd, sText);
rtb.Index = iStart + sText.Length;
AddMessage(rtb.RowText);
}
}

if (menuItem == menuMiscTransformFiles) {
TransFormFiles();
}

if (menuItem == menuMiscPickCompiler) {
aResults = Ini.ReadSectionKeys(App.IniFile, "Compilers");
//Array.Sort(aResults);
sResult = App.ReadData("Compiler", "Default");
//Dialog.Show(sResult, String.Join("\n", aResults));
int i = Array.IndexOf(aResults, sResult);
//Dialog.Show(i);
if (i == -1) i = 0;
sResult = Dialog.Pick("Pick Compiler", aResults, false, i);
if (sResult.Length == 0) return;

sFile = Path.Combine(App.DataDir, App.ReadData("Compiler", "Default") + ".ini");
string sDir = Directory.GetCurrentDirectory();
Ini.WriteValue(sFile, "Data", "Directory", sDir);
App.WriteData("Compiler", sResult);
sFile = Path.Combine(App.DataDir, sResult + ".ini");
string s = Ini.ReadValue(sFile, "Data", "Directory", "");
if (Directory.Exists(s) && !Util.Equiv(sDir, s)) {
AddMessage("Folder " + Path.GetFileName(s));
Directory.SetCurrentDirectory(s);
}

sValue = Ini.ReadValue(App.IniFile, "Compilers", sResult, "");
string[] a = sValue.Split('~');
Ini.WriteQuote(App.IniFile, "Options", "CompileCommand", a[0]);
if (a.Length > 1) Ini.WriteQuote(App.IniFile, "Options", "JumpPosition", a[1]);
if (a.Length > 2) Ini.WriteQuote(App.IniFile, "Options", "AbbreviateOutput", a[2]);
if (a.Length > 3) Ini.WriteQuote(App.IniFile, "Options", "NavigatePart", a[3]);
if (a.Length > 4) Ini.WriteQuote(App.IniFile, "Options", "QuotePrefix", a[4]);
if (a.Length > 5) Ini.WriteQuote(App.IniFile, "Options", "ExtensionDefault", a[5]);
if (a.Length > 6) Ini.WriteQuote(App.IniFile, "Options", "GoToEnvironment", a[6]);
// More robust alternative to the tilde-packed value: if a section named
// "Compiler <name>" exists (most naturally in EdSharp.inix, which keeps each
// value verbatim), its named keys override the unpacked fields. This avoids any
// collision with the ~ delimiter and makes regex settings easy to read and edit.
string sSection = "Compiler " + sResult;
string[] aKeys = new string[] {"CompileCommand", "JumpPosition", "AbbreviateOutput", "NavigatePart", "QuotePrefix", "ExtensionDefault", "GoToEnvironment"};
foreach (string sKey in aKeys) {
string sVal = Ini.ReadValue(App.IniFile, sSection, sKey, "\0");
if (sVal != "\0") Ini.WriteQuote(App.IniFile, "Options", sKey, sVal);
}
}

if (menuItem == menuMiscGoToEnvironment) {
string sCommand = @"%ProgDir%\ijs.exe";
sCommand = App.ReadOption("GoToEnvironment", sCommand);
if (this.Child == null) sFile = "temp.txt";
else sFile = child.File;
if (!sFile.Contains(@"\")) sFile = Path.Combine(Directory.GetCurrentDirectory(), sFile);
sCommand = Util.ExpandCommandLine(sCommand, sFile, sFile);
string sProcess = Path.GetFileNameWithoutExtension(sCommand);
if (!Util.ActivateProcess(sProcess)) Util.Run(sCommand);
}

if (menuItem == menuMiscCompile|| menuItem == menuMiscPromptCommand) {
string sCommand;
string sDefaultJump = "";
string sDefaultAbbreviate = "";
if (menuItem == menuMiscCompile) {
sCommand = App.ReadOption("CompileCommand", "");
// Built-in default: with no compiler configured, compile a C# (.cs) file with
// the latest available .NET Framework C# compiler (Roslyn csc if present, else
// the framework csc that always ships with .NET). This makes Control+F5 work on
// a .cs file out of the box, jumping to the first csc error position.
if (sCommand.Trim().Length == 0 && child.File.ToLower().EndsWith(".cs")) {
string sCsc = Util.FindCscPath();
if (sCsc.Length > 0) {
sCommand = "\"" + sCsc + "\" /nologo \"%SourceLong%\" 2>&1";
sDefaultJump = @"\(\d+,\d+\)";
}
}
// The same courtesy for Python, taken from the author's tree (his 5.0.30):
// with no compiler configured, a .py or .pyw file runs with a real Python,
// jumping to the traceback's line number. Picking a compiler with
// Control+Shift+F5 still overrides this default.
if (sCommand.Trim().Length == 0 && (child.File.ToLower().EndsWith(".py") || child.File.ToLower().EndsWith(".pyw"))) {
string sPythonExe = Util.FindPythonPath();
sCommand = ((sPythonExe.Length > 0) ? "\"" + sPythonExe + "\"" : "python") + " \"%SourceLong%\" 2>&1";
sDefaultJump = @"line \d+";
// Python names the file in every traceback frame -- File "C:\long\path
// \script.py", line 4 -- and hearing your own path read out before the
// error wastes the moment that matters. Drop the file prefix and the
// traceback banner, so speech starts at "line 4" and reaches the
// message itself immediately.
sDefaultAbbreviate = @"(^[ \t]*File "".*?"", )|(^Traceback \(most recent call last\):[ \t]*\r?\n)";
}
if (sCommand.Trim().Length == 0) {
AddMessage("No compiler configured. Press Control+Shift+F5 to pick one.");
return;
}
}
else {
sCommand = App.ReadOption("PromptCommand", "");
sCommand = Dialog.Input("Prompt", "Command", sCommand).Trim();
if (sCommand.Length == 0) return;
App.WriteOption("PromptCommand", sCommand);
}

sFile = child.File;
if (!sFile.Contains(@"\")) sFile = "";
if (sCommand.IndexOf("%Source") >=0 && sFile.Length == 0) {
AddMessage("No disk file is open for this command!");
return;
}
else if (sFile.Length > 0) child.SaveTextOrRtfFile(sFile);

string sDir = Directory.GetCurrentDirectory();
if (sCommand.IndexOf("%SourceDir%") >=0) Directory.SetCurrentDirectory(Path.GetDirectoryName(sFile));
sCommand = Util.ExpandCommandLine(sCommand, sFile, Path.ChangeExtension(sFile, ".exe"));
// Dialog.Show(sCommand);

// Try with COMSpec
// string sOutput = Util.GetProgramOutput(@"c:\windows\system32\cmd.exe", "/c " + sCommand);

// Debug JAWS script
// if (!sCommand.Trim().EndsWith(">1")) sOutput = File.ReadAllText(App.TempFile);
// Util.Run(sCommand);

string sOutput = "";
// if (sCommand.Trim().EndsWith(">1") || sCommand.Trim().EndsWith("&1")) Util.GetProgramOutput("cmd.exe", "/c " + sCommand);
if (sCommand.Trim().EndsWith(">1") || sCommand.Trim().EndsWith("&1")) sOutput = Util.GetProgramOutput("cmd.exe", "/c " + sCommand);
else {
Util.RunHideWait(sCommand);
sOutput = File.ReadAllText(App.TempFile);
}

// Dialog.Show("output", sOutput.Length);

/*
Dialog.Show(sCommand);
int i = sCommand.IndexOf(".exe");
string sParams = sCommand.Substring(i + 5);
sCommand = sCommand.Substring(0, i + 4);
Dialog.Show(sCommand, sParams);
string sOutput = Util.GetProgramOutput(sCommand, sParams);
Dialog.Show(sOutput);
*/

if (sDir != Directory.GetCurrentDirectory()) Directory.SetCurrentDirectory(sDir);

if (menuItem == menuMiscCompile) {
string sJumpPosition = App.ReadOption("JumpPosition", "");
if (sJumpPosition.Trim().Length == 0) sJumpPosition = sDefaultJump;
object[] a = Util.RegExpContainsCase(sOutput, sJumpPosition);
iIndex = (int) a[0];
if (iIndex >= 0) {
sText = (string) a[1];
a = Util.RegExpContainsCase(sText, @"\d+");
iIndex = (int) a[0];
if (iIndex >= 0) {
sLine = (string) a[1];
iIndex += sLine.Length;
sText = sText.Substring(iIndex);
a = Util.RegExpContainsCase(sText, @"\d+");
iIndex = (int) a[0];
string sColumn;
if (iIndex == -1) sColumn = "1";
else sColumn = (string) a[1];
// An indentation error is about the whitespace at the START of the
// line, whatever the tool's marker points at -- Python puts its caret
// at the end of the line, which is the least useful place to land when
// the fix belongs at the beginning. The cursor goes to column 1, ready
// for the edit. Taken from the author's tree.
if (sOutput.IndexOf("IndentationError", StringComparison.OrdinalIgnoreCase) >= 0 || sOutput.IndexOf("TabError", StringComparison.OrdinalIgnoreCase) >= 0) sColumn = "1";
string s = sLine + ", " + sColumn;
// Dialog.Show("s", s);
App.WriteData("Line", s);

try {
rtb.Line = Int32.Parse(sLine);
rtb.Column = Int32.Parse(sColumn);
}
catch {}
}
}

string sAbbreviateOutput = App.ReadOption("AbbreviateOutput", "\r");
// A per-language default applies only when the user has not set one: the
// stored value is still the shipped backslash-r or is empty. Anything the
// user chose in Configuration Options wins, as before.
if (sDefaultAbbreviate.Length > 0 && (sAbbreviateOutput == "\\r" || sAbbreviateOutput == "\r" || sAbbreviateOutput.Trim().Length == 0)) sAbbreviateOutput = sDefaultAbbreviate;
sOutput = Util.RegExpReplaceEquiv(sOutput, sAbbreviateOutput, "\n").Trim();
if (sOutput.Length == 0) sOutput = "Done";
AddMessage(sOutput);
}
Util.String2File(sOutput, App.TempFile);
}

if (menuItem == menuMiscReviewOutput) {
sFile = App.TempFile;
if (!File.Exists(sFile)) {
AddMessage("No output file found!");
return;
}
OpenOrActivateWindow(sFile, 0);
}

if (menuItem == menuMiscSaveSnippet) {
if (rtb.SelectionLength == 0) {
AddMessage("All");
iStart = 0;
iEnd = rtb.TextLength;
}
else {
AddMessage("Selected");
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
}
sText = rtb.GetRange(iStart, iEnd);

string sDir = @"Snippets\" + App.ReadData("Compiler", "Default");
sDir = Path.Combine(App.DataDir, sDir);
if (!Directory.Exists(sDir)) Directory.CreateDirectory(sDir);
sFile = Path.Combine(sDir, Path.GetFileName(child.File));
//sFile = Path.ChangeExtension(sFile, ".txt");
if (Path.GetExtension(sFile).Length == 0) sFile += ".txt";
sFile = Dialog.SaveFile("", sFile);
if (sFile.Length == 0) return;

if (rtb.SelectionLength == 0) child.SaveTextOrRtfFile(sFile);
else Util.String2File(sText, sFile);
AddMessage("Done");
}

if (menuItem == menuMiscInvokeSnippet) {
if (rtb.SelectionLength == 0) {
AddMessage("Cursor");
//iStart = 0;
//iEnd = rtb.TextLength;
iStart = rtb.Index;
iEnd = iStart;
}
else {
AddMessage("Selected");
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
}
sText = rtb.GetRange(iStart, iEnd);

aResults = GetSnippetFiles(out aValues);

/*
foreach (string sSnippetFile in aResults) {
Util.Say(Path.GetFileNameWithoutExtension(sSnippetFile));
string sSnippetText = Util.File2String(sSnippetFile);
string[] aSnippetLines = sSnippetText.Split('\n');
StringBuilder sbSnippet = new StringBuilder();
foreach (string sSnippetLine in aSnippetLines) {
if (sSnippetLine.Trim().Length == 0) continue;
sbSnippet.Append(sSnippetLine.Trim() + "\r\n");
}
Util.String2File(sbSnippet.ToString(), sSnippetFile);
}
*/

if (aResults.Length == 0) {
AddMessage("No files!");
return;
}

string sSnippet = Dialog.Pick("Pick", aResults, aValues, false, 0);
if (sSnippet.Length == 0) return;

InvokeSnippet(sSnippet, sText, iStart, iEnd);
}

if (menuItem == menuMiscViewSnippet) {
aResults = GetSnippetFiles(out aValues);
if (aResults.Length == 0) {
AddMessage("No files!");
return;
}

sResult = Dialog.Pick("Pick", aResults, aValues, false, 0);
if (sResult.Length == 0) return;

OpenOrActivateWindow(sResult, 0);
}

if (menuItem == menuMiscKeepUniqueItems) {
string sLimitItem = Util.Literalize(App.ReadOption("LimitItem", "\n"));
if (rtb.SelectionLength == 0) {
AddMessage("All");
iStart = 0;
iEnd = rtb.TextLength;
}
else {
AddMessage("Selected");
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
}

sText = rtb.GetRange(iStart, iEnd);
aResults = Regex.Split(sText, sLimitItem);
List<string> listNormal = new List<string>();
List<string> listLower = new List<string>();
foreach (string s in aResults) {
string sLower = s.ToLower();
if (listLower.Contains(sLower)) continue;
listLower.Add(sLower);
listNormal.Add(s);
}

aResults = listNormal.ToArray();
sText = String.Join(sLimitItem, aResults);
rtb.ReplaceRange(iStart, iEnd, sText);
rtb.Index = iStart;
}

if (menuItem == menuMiscNumberItems) {
string sLimitItem = Util.Literalize(App.ReadOption("LimitItem", "\n"));
if (rtb.SelectionLength == 0) {
sTitle = "Number Items All";
iStart = 0;
iEnd = rtb.TextLength;
}
else {
sTitle = "Number Items Selected";
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
}

sResult = Dialog.Input(sTitle, "Start", "1").Trim();
if (sResult.Length == 0) return;

try {
iLine = Int32.Parse(sResult);
}
catch (Exception ex) {
Dialog.Show("Error", ex.Message);
return;
}

sText = rtb.GetRange(iStart, iEnd);
aResults = Regex.Split(sText, sLimitItem);
for (int i = 0; i < aResults.Length; i++) {
string s = aResults[i];
// if (s.Trim().Length > 0) s = iLine++ + ". " + s;
s = iLine++ + ". " + s;
aResults[i] = s;
}

sText = String.Join(sLimitItem, aResults);
rtb.ReplaceRange(iStart, iEnd, sText);
rtb.Index = iStart;
}

if (menuItem == menuMiscOrderItems) {
string sLimitItem = Util.Literalize(App.ReadOption("LimitItem", "\n"));
if (rtb.SelectionLength == 0) {
AddMessage("All");
iStart = 0;
iEnd = rtb.TextLength;
}
else {
AddMessage("Selected");
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
}

sText = rtb.GetRange(iStart, iEnd);
// aResults = sText.Split('\n');
aResults = Regex.Split(sText, sLimitItem);
string[] a = new string[aResults.Length];
for (int i = 0; i < a.Length; i++) a[i] = aResults[i].ToLower();
Array.Sort(a, aResults);
// sText = String.Join("\n", aResults);
sText = String.Join(sLimitItem, aResults);
rtb.ReplaceRange(iStart, iEnd, sText);
rtb.Index = iStart;
}

if (menuItem == menuMiscReverseItems) {
string sLimitItem = Util.Literalize(App.ReadOption("LimitItem", "\n"));
if (rtb.SelectionLength == 0) {
AddMessage("All");
iStart = 0;
iEnd = rtb.TextLength;
}
else {
AddMessage("Selected");
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
}

sText = rtb.GetRange(iStart, iEnd);
aResults = Regex.Split(sText, sLimitItem);
Array.Reverse(aResults);
sText = String.Join(sLimitItem, aResults);
rtb.ReplaceRange(iStart, iEnd, sText);
rtb.Index = iStart;
}

if (menuItem == menuMiscListDifferentItems) {
string sLimitItem = Util.Literalize(App.ReadOption("LimitItem", "\n"));
// aResults = rtb.GetRange(0, rtb.RowStart).Split('\n');
sText = rtb.GetRange(0, rtb.RowStart);
aResults = Regex.Split(sText, sLimitItem);
sText = rtb.GetRange(rtb.RowStart, rtb.TextLength);
string[] a = Regex.Split(sText, sLimitItem);
// string[] a = rtb.GetRange(rtb.RowStart, rtb.TextLength).Split('\n');
List<string> list = new List<string>();
foreach (string s in aResults) if (s.Trim().Length > 0 && Array.IndexOf(a, s) == -1) list.Add(s);
aResults = list.ToArray();
if (aResults.Length == 0) {
AddMessage("No output!");
return;
}

AddMessage(Util.Pluralize(aResults.Length, "line"));
// sText = String.Join("\n", aResults).TrimEnd('\n') + "\n";
sText = String.Join(sLimitItem, aResults);
child = new MdiChild(App.Frame);
Child.RTB.Text = sText;
rtb.Index = 0;
}

if (menuItem == menuMiscQueryCommonItems) {
// aResults = rtb.GetRange(0, rtb.RowStart).Split('\n');
// string[] a = rtb.GetRange(rtb.RowStart, rtb.TextLength).Split('\n');
string sLimitItem = Util.Literalize(App.ReadOption("LimitItem", "\n"));
sText = rtb.GetRange(0, rtb.RowStart);
aResults = Regex.Split(sText, sLimitItem);
sText = rtb.GetRange(rtb.RowStart, rtb.TextLength);
string[] a = Regex.Split(sText, sLimitItem);

List<string> list = new List<string>();
foreach (string s in aResults) if (s.Trim().Length > 0 && Array.IndexOf(a, s) >= 0) list.Add(s);
aResults = list.ToArray();
if (aResults.Length == 0) {
AddMessage("No output!");
return;
}

AddMessage(Util.Pluralize(aResults.Length, "line"));
// sText = String.Join("\n", aResults).TrimEnd('\n') + "\n";
sText = String.Join(sLimitItem, aResults);
child = new MdiChild(App.Frame);
Child.RTB.Text = sText;
rtb.Index = 0;
}

if (menuItem == menuMiscCommandPrompt) {
string sDir = GetDirChoice();
if (sDir.Length == 0) return;
CommandPrompt(sDir);
}

if (menuItem == menuMiscWebDownload) {
string sButton = "Web Page";
if (App.Frame.Child != null) {
sButton = Dialog.Choose("Choose Source of URLs", "", new string[] {"&Web Page", "&Current Document"}, 0);
if (sButton.Length == 0) return;
} // if

List<string[]> listLinks;
if (sButton.Replace("&", "") == "Web Page") {
string sUrl = COM.GetUrl();
if (sUrl.Length == 0) sUrl = App.ReadData("Url", "");
sUrl = Dialog.Input("Web Download", "Address", sUrl);
if (sUrl.Length == 0) return;

AddMessage("Please wait");
App.WriteData("Url", sUrl);
listLinks = Homer.Web.getLinks(sUrl);
}
else {
listLinks = new List<string[]>();
aResults = Util.RegExpExtractCase(App.Frame.Child.RTB.Text, @"\w+\:\/\/[^\s""\'\)]+");
if (aResults.Length == 0) {
AddMessage("No URLs found!");
return;
}

for (int i = 0; i < aResults.Length; i++) {
listLinks.Add(new string[] {aResults[i], ""});
} // for
}

List<string> listFiles = new List<string>();
string sRef;
foreach (string[] aLink in listLinks) {
sRef = aLink[0];
sFile = Util.GetFileFromUri(sRef);
listFiles.Add(sFile);
}

string[] aFiles = listFiles.ToArray();
sText = Util.GetExtensions(aFiles);
sResult = Dialog.Input("Filter", "Extensions", sText).Replace(".", "").Trim().ToLower();
if (sResult.Length == 0) return;

aResults = Util.GetPathsWithExtensions(aFiles, sResult);

listFiles.Clear();
List<string> listItems = new List<string>();
List<string> listRefs = new List<string>();
foreach (string[] aLink in listLinks) {
sRef = aLink[0];
sFile = Util.GetFileFromUri(sRef);
string sExt = Path.GetExtension(sFile).TrimStart('.').ToLower();
//if (Array.IndexOf(aResults, sExt) == -1) continue;
if (Array.IndexOf(aResults, sFile) == -1) continue;

sText = aLink[1];
if (String.IsNullOrEmpty(sText)) sText = sRef;

listItems.Add(sText + " = " + sFile);
listFiles.Add(sFile);
listRefs.Add(sRef);
}

if (listItems.Count == 0) {
AddMessage("No items!");
return;
}

aValues = listItems.ToArray();
//aResults = Dialog.MultiPick("Pick Files", aValues, new int[] {}, false);
aResults = Dialog.MultiCheck("Pick Files", aValues, new int[] {}, false, 0);
if (aResults.Length == 0) return;

sTitle = "Open Folder";
string sDir = App.ReadData("DownloadFolder", Directory.GetCurrentDirectory());
sDir = Dialog.OpenFolder(sTitle, "Name", sDir);

if (sDir.Length == 0) return;

App.WriteData("DownloadFolder", sDir);
Directory.SetCurrentDirectory(sDir);
AddMessage("Downloading");
foreach (string s in aResults) {
int i = listItems.IndexOf(s);
sFile = listFiles[i];
sRef = listRefs[i];
// Homer.Web.download follows redirects with a real User-Agent and modern TLS,
// takes the file name from the Content-Disposition header when the server
// supplies one (otherwise the link's name plus an extension guessed from the
// content type), and sanitizes and uniquifies the result within sDir.
string sSaved = Homer.Web.download(sRef, sDir, Path.GetFileName(sFile));
if (sSaved.Length > 0) AddMessage(Path.GetFileName(sSaved));
else AddMessage("Could not download " + Path.GetFileName(sFile));
}
AddMessage("Done", true);
}
if (menuItem == menuMiscWebClientUtilities) {
App.Frame.WebClientUtilities();
}

if (menuItem == menuWindowNext) {
NextWindow();
}

if (menuItem == menuWindowPrior) {
PriorWindow();
}

if (menuItem == menuWindowArrangeIcons) {
this.LayoutMdi(MdiLayout.ArrangeIcons);
return;
}

if (menuItem == menuWindowCascade) {
this.LayoutMdi(MdiLayout.Cascade);
return;
}

if (menuItem == menuWindowTileHorizontal) {
this.LayoutMdi(MdiLayout.TileHorizontal);
return;
}

if (menuItem == menuWindowTileVertical) {
this.LayoutMdi(MdiLayout.TileVertical);
return;
}

if (menuItem == menuHelpAbout) {
// NUMER WERSJI CZYTAMY Z App.VersionString, nie z literalu (naprawa 06.09.2026).
// Literal "5.0.1" wisial tu od sierpnia i klamal przy kazdej kolejnej paczce:
// tester dostawal instalator 5.0.70, a program mowil mu o sobie 5.0.1.  Data
// wydania tez zeszla, bo staly napis "August 5, 2026" byl drugim zrodlem tej
// samej nieprawdy - datownik bierzemy z pliku programu, wiec zawsze zgadza sie
// z tym, co user naprawde uruchomil.
sText = "EdSharpNG " + App.VersionString + " (beta)\n" + Util.GetProgramBuildDate() + "\n\n";
sText += "Based on EdSharp by Jamal Mazrui.\nOriginal work copyright 2007 - 2026 by Jamal Mazrui.\nEdSharpNG changes copyright 2026 by Michal Kasperczak.\nGNU Lesser General Public License (LGPL)\n\n";
sText += ".NET Framework " + RuntimeEnvironment.GetSystemVersion() + "\n\n";
sText += Util.GetPortableExecutableKind();
Dialog.Show("About", sText);
}

if (menuItem == menuHelpDocumentation) {
sFile = Path.Combine(App.ProgramDir, App.ProgramName) + ".htm";
Process.Start(sFile);
}

if (menuItem == menuHelpTutorial) {
// Open the tutorial in the default browser associated with the .htm extension.
sFile = Path.Combine(App.ProgramDir, "Tutorial.htm");
Process.Start(sFile);
}
if (menuItem == menuHelpHistoryOfChanges) {
sFile = Path.Combine(App.ProgramDir, "History.txt");
OpenOrActivateWindow(sFile, 1);
}

if (menuItem == menuHelpKeyDescriber) {
if (this.KeyDescriber) {
SetMessage("No Key Describer");
this.KeyDescriber = false;
}
else {
SetMessage("Key Describer On");
this.KeyDescriber = true;
}
}

if (menuItem == menuHelpHotKeySummary) {
sFile = Path.Combine(App.ProgramDir, "HotKeys.txt");
OpenOrActivateWindow(sFile, 1);
}

if (menuItem == menuHelpAlternateMenu) {
AlternateMenu();
}

if (menuItem == menuHelpCommandPalette) {
CommandPalette();
}

if (menuItem == menuHelpContextMenu) {
sFile = child.File;
if (!sFile.Contains(@"\")) {
AddMessage("No disk file is open for this command!");
return;
}

ContextMenu(sFile);
}

if (menuItem == menuHelpSendToMenu) {
sFile = child.File;
if (!sFile.Contains(@"\")) {
AddMessage("No disk file is open for this command!");
return;
}

SendToMenu(sFile);
}

if (menuItem == menuHelpElevateVersion) {
ElevateVersion();
}

if (menuItem == menuHelpReinstall) {
ElevateVersion(true);
}

if (menuItem == menuHelpUpdateComponents) {
UpdateComponents();
}

if (menuItem == menuHelpReportProblem) {
ReportProblem();
}

} // menuItem_Click handler

// SKLADNIKI NA ZADANIE UZYTKOWNIKA (menu Help, Update Components).
//
// Rozni sie od sprawdzania przy starcie w dwoch rzeczach, i to jest celowe:
// 1. Aktualizuje TAKZE narzedzia, ktore juz sa - przy starcie tylko dociaga
//    brakujace, zeby nie podmieniac czegos w trakcie uzywania.
// 2. Zawsze MELDUJE wynik, nawet gdy nie bylo nic do roboty albo gdy nie ma
//    internetu.  Przy starcie milczenie bylo wlasciwe, bo nikt nie prosil;
//    tutaj milczenie byloby zignorowaniem polecenia.
//
// Pobieranie idzie w tle (ponad 60 MB przy pustym folderze), zeby program nie
// zamarl.  Meldunek lada na pasku wiadomosci - swiadomie nie w okno, zeby nie
// porwac fokusu w chwili, gdy uzytkownik zdazyl wrocic do pisania.
public void UpdateComponents() {
try {
App.Frame.AddMessage("Sprawdzam skladniki konwersji...");
System.Threading.Thread thread = new System.Threading.Thread(delegate() {
string sWynik;
try {
bool bTylkoBrakujace = false;
string sZrobione = Skladniki.SprawdzIUzupelnij(bTylkoBrakujace);
System.Collections.Generic.List<string> lBrak = Skladniki.Brakujace();
if (sZrobione.Length > 0 && lBrak.Count == 0)
sWynik = "Skladniki gotowe: " + sZrobione + ".";
else if (sZrobione.Length > 0)
sWynik = "Pobrano: " + sZrobione + ". Nadal brakuje: " + string.Join(", ", lBrak.ToArray()) + ".";
else if (lBrak.Count == 0)
sWynik = "Wszystkie skladniki sa aktualne.";
else
sWynik = "Nie udalo sie pobrac: " + string.Join(", ", lBrak.ToArray()) + ". Sprawdz polaczenie z internetem.";
}
catch { sWynik = "Sprawdzanie skladnikow nie doszlo do skutku."; }

try {
if (App.Frame == null || !App.Frame.IsHandleCreated) return;
string sPrzekaz = sWynik;
App.Frame.BeginInvoke((MethodInvoker)delegate() {
try { App.Frame.AddMessage(sPrzekaz); } catch {}
});
}
catch {}
});
thread.IsBackground = true;
thread.Priority = System.Threading.ThreadPriority.BelowNormal;
thread.Start();
}
catch {}
} // UpdateComponents method

object[] GetChunk() {
bool bLoop;
int iIndex, iStart, iEnd;
string c, sText;
HomerRichTextBox rtb = this.Child.RTB;
sText = rtb.Text;
iIndex = rtb.Index;
c = "";

bLoop = true;
while (bLoop) {
if (iIndex == sText.Length) c = "";
else c = sText.Substring(iIndex, 1);
bLoop = (c.Trim().Length == 0);
bLoop = (bLoop && iIndex > 0);
if (bLoop) iIndex--;
}

bLoop = iIndex < sText.Length;
while (bLoop) {
c = sText.Substring(iIndex, 1);
bLoop = (c.Trim().Length > 0);
bLoop = (bLoop && iIndex > 0);
if (bLoop) iIndex--;
}
if (c.Trim().Length == 0) iIndex++;
iStart = iIndex;

bLoop = iIndex < sText.Length;
while (bLoop) {
c = sText.Substring(iIndex, 1);
bLoop = (c.Trim().Length > 0);
iIndex++;
bLoop = (bLoop && iIndex < sText.Length);
}
iEnd = iIndex;

if (iStart == iEnd) sText = "";
else sText = rtb.GetRange(iStart, iEnd);
sText = sText.TrimEnd();
return new object[] {iStart, sText};
} // GetChunk method

public void InvokeSnippet(string sSnippet, string sText, int iStart, int iEnd) {
string[] aLabels, aValues, aResults;
int iIndex;
HomerRichTextBox rtb = this.Child.RTB;
string sLabel, sValue, sMatch;
string sExt = Path.GetExtension(sSnippet).ToLower().TrimStart('.');
string sBody = Util.File2String(sSnippet);
sBody = Util.Convert2UnixLineBreak(sBody);
if (sExt == "js") {
Script.run(sBody);
return;
}
else if (sExt == "boo") {
if (App.Boo == null) App.Boo = COM.CreateObject("Iron.COM");
sSnippet = (string) COM.CallMethod(App.Boo, "Eval", new string[] {sBody, "", "", "", ""});
//Dialog.Show(sSnippet);
return;
}

aResults = sBody.Split('\n');

string sPre = "";
string sPost = "";

string sKeywords = aResults[0];
string[] aKeywords = sKeywords.Split(' ');
string sType = aKeywords[0];
if (sType == "text") {
sText = sBody.Substring(sKeywords.Length + 1);
iIndex = rtb.Index;
}
else if (sType == "html") {
List<string> listResults = new List<string>(aResults);
for (int i = listResults.Count - 1; i > 0; i--) if (listResults[i].Trim().Length == 0 || listResults[i].StartsWith(";")) listResults.RemoveAt(i);
aResults = listResults.ToArray();
sPre = "<" + aResults[1];
if (Array.IndexOf(aResults, "empty") == -1) sPost = "</" + sPre.Substring(1) + ">";

if (aResults.Length > 2) {
List<string> listLabels = new List<string>();
List<string> listValues = new List<string>();
for (int i = 2; i < aResults.Length; i++) {
string sLine = aResults[i];
//if (sLine.Trim().Length == 0 || sLine.StartsWith(";")) continue;
string[] a = sLine.Split('=');
sLabel = a[0];
sValue = "";
if (a.Length > 1) sValue = a[1];
listLabels.Add("&" + sLabel);
listValues.Add(sValue);
}

aLabels = listLabels.ToArray();
aValues = listValues.ToArray();
aResults = Dialog.MultiInput("Attributes", aLabels, aValues);
if (aResults.Length == 0) return;

for (int i = 0; i < aResults.Length; i++) {
sLabel = aLabels[i].Substring(1);
sValue = aResults[i];
sValue = Util.Literalize(sValue);
if (sValue.Length == 0) continue;
sPre += " " + sLabel + "=\"" + sValue + "\"";
}
}

sPre += ">";
if (Array.IndexOf(aKeywords, "phrase") == -1) sPost += "\n";
sText = sPre + sText + sPost;
}
else {
sText = sBody;
iIndex = iStart + sText.Length;
}

if (Array.IndexOf(aKeywords, "form") >= 0) {
string sDate, sTime;
GetDateAndTime(out sDate, out sTime);
sText = sText.Replace("%Date%", sDate);
sText = sText.Replace("%Time%", sTime);
string sUserName = Environment.UserName;
sUserName = sUserName.Replace(".", " ");
sText = sText.Replace("%UserName%", sUserName);
string[] aNames = (sUserName + " ").Split(' ');
sText = sText.Replace("%UserFirstName%", aNames[0]);
sText = sText.Replace("%UserLastName%", aNames[1]);

sMatch = @"\%\w+\=.*?\%";
string[] aVars = Util.RegExpExtractCase(sText, sMatch);
if (aVars.Length > 0) {
List<string> listLabels = new List<string>();
List<string> listValues = new List<string>();
List<string> listVars = new List<string>(aVars);
foreach (string sVar in aVars) {
string[] aParts = sVar.Split('=');
sLabel = aParts[0];
sLabel = "&" + sLabel.Substring(1, sLabel.Length - 1);
sValue = aParts[1];
sValue = sValue.Substring(0, sValue.Length - 1);
if (listLabels.Contains(sLabel)) {
// Stop reverse bug
listVars.Reverse();
listVars.Remove(sVar);
listVars.Reverse();
continue;
}

listLabels.Add(sLabel);
listValues.Add(sValue);
}
aLabels = listLabels.ToArray();
aValues = listValues.ToArray();
aResults = Dialog.MultiInput("Variables", aLabels, aValues);
if (aResults.Length == 0) return;

aVars = listVars.ToArray();
for (int i = 0; i < aVars.Length; i++) {
string sVar = aVars[i];
// Dialog.Show("sVar=" + sVar, "result=" + aResults[i]);
sText = sText.Replace(sVar, aResults[i]);
sVar = sVar.Split('=')[0] + "=%";
sText = sText.Replace(sVar, aResults[i]);
}
}

sText = ReplaceTokens(sText);
}

if (Array.IndexOf(aKeywords, "caret") >= 0) {
int i = sText.IndexOf("^^");
if (i >= 0) {
iIndex = iStart + i;
sText = sText.Remove(i, 2);
}
else iIndex = iStart;
}
else {
iIndex = iStart + sText.Length;
if (rtb.SelectionLength == 0) iIndex -= sPost.Length;
}

rtb.ReplaceRange(iStart, iEnd, sText);
rtb.Index = iIndex;
Util.Say(rtb.RowText);
} // InvokeSnippet method

public string PyDent2Brace(string sText) {
sText = Util.RegExpReplaceCase(sText, @"^\t*\# end \w+$", "");
string sOld = sText;
while (true) {
sText = Util.RegExpReplaceCase(sText, @"^(\t*) ", "$1\t");
if (sText == sOld) break;
else sOld = sText;
}

// does not work
/*
sText = Util.RegExpReplaceCase(sText, @"^\t*\#", "#");
sText = Util.RegExpReplaceCase(sText, @"^\#([^ ])", "# $1");
string[] aIndent = Util.RegExpExtractCase(sText, @"^\t+");
int iMax = 0;
int iMin = 1000;
foreach (string s in aIndent) {
iLength = s.Length;
if (iLength > iMax) iMax = iLength;
if (iLength < iMin) iMin = iLength;
}

if (iMin > 0) {
string sMin = "\t".PadRight(iMin, '\t');
string sAbbrev = "\t".PadRight(iMin - 1, '\t');
for (int n = 1; n <= iMax / iMin; n++) {
sText = Util.RegExpReplaceCase(sText, @"^" + sMin, sAbbrev);
}
}
*/

HomerList hl = new HomerList(sText.Split('\n'));
int i = 0;
int iOldLevel = 0;
int iCount = 0;
char[] a = {' ', ':'};
HomerList hlCode = new HomerList();
HomerList hlLevel = new HomerList();
bool bTripleQuote = false;
int iBrace = 0;
int iBracket = 0;
int iParen = 0;
int iTripleQuote = 0;
int iDoubleQuote = 0;
int iSingleQuote = 0;
bool bQuote = false;

while ( i < hl.Count) {
string sLine = hl[i];
string sTrim = sLine.TrimEnd();
string sPack = sTrim.TrimStart();
int iTrim = sTrim.Length;
int iPack = sPack.Length;

if (!bTripleQuote && (iPack == 0 || sPack.StartsWith("#"))) hl[i] = sPack;
else {
iParen = 0;
iBracket = 0;
iBrace = 0;
iSingleQuote = 0;
iDoubleQuote = 0;
while (true) {
int iCharCount = iPack;
int iChar = 0;
while (iChar < iCharCount) {
switch (sPack[iChar]) {
case '"' :
if (iChar > 0 && sPack[iChar - 1] == '\\') break;
if ((iChar + 2 < iCharCount) && sPack[iChar + 1] == '"' && sPack[iChar + 2] == '"') {
if (bTripleQuote && iTripleQuote > 0) {
bTripleQuote = false;
bQuote = false;
iTripleQuote--;
iSingleQuote = 0;
iDoubleQuote = 0;
}
else if (!bQuote && !bTripleQuote && iTripleQuote == 0) {
bTripleQuote = true;
bQuote = true;
iTripleQuote++;
iSingleQuote = 0;
iDoubleQuote = 0;
}
iChar += 2;
}
else if (bQuote && iDoubleQuote > 0) {
bQuote = false;
iDoubleQuote--;
}
else if (!bQuote && iDoubleQuote == 0) {
bQuote = true;
iDoubleQuote++;
}
break;
case '\'' :
if (iChar > 0 && sPack[iChar - 1] == '\\') break;
if (bQuote && iSingleQuote > 0) {
bQuote = false;
iSingleQuote--;
}
else if (!bQuote && iSingleQuote == 0) {
bQuote = true;
iSingleQuote++;
}
break;
case '(' :
if (!bQuote) iParen++;
break;
case ')' :
if (!bQuote) iParen--;
break;
case '[' :
if (!bQuote) iBracket++;
break;
case ']' :
if (!bQuote) iBracket--;
break;
case '{' :
if (!bQuote) iBrace++;
break;
case '}' :
if (!bQuote) iBrace--;
break;
}
iChar++;
}

if ((iParen + iBracket + iBrace == 0) || bTripleQuote) break;
hl[i] = sPack + @" \";

do  i++;
while (i < hl.Count && (hl[i].Trim().Length == 0 || hl[i].TrimStart().StartsWith("#")));
if (i == hl.Count) break;

sLine = hl[i];
sTrim = sLine.TrimEnd();
iTrim = sTrim.Length;
sPack = sTrim.TrimStart();
iPack = sPack.Length;
}

if (i == hl.Count) break;

if (bTripleQuote) {
hl[i] = sTrim;
}
else {
int iNewLevel = iTrim - iPack;
//if (i == 75 || i == 76) Dialog.Show(iNewLevel, hl[i]);
int iDelta = iOldLevel - iNewLevel;

int k = i - 1;
while (k >= 0 && hl[k].StartsWith("#")) k--;
k++;

while (hlCode.Count > 0 && iDelta > 0 && Int32.Parse(hlLevel[hlLevel.Max]) >= iNewLevel) {
hl.Insert(k, "} end " + hlCode.Pop());
hlLevel.Pop();
iCount--;
i++;
k++;
iDelta--;
}

if (iOldLevel > iNewLevel) {
hl.Insert(k, "");
i++;
}
iOldLevel = iNewLevel;

if (sPack.EndsWith(":")) {
sPack = sPack.TrimEnd(a) + " {";
iCount++;
string[] aCode = sPack.Split(' ');
hlCode.Add(aCode[0].TrimEnd('{'));
hlLevel.Add(iNewLevel.ToString());
}
else if (sPack.EndsWith(@"\")) {
hl[i] = sPack;
i++;
if (i == iCount) break;
sPack = hl[i].Trim();
}
hl[i] = sPack;

}
}
i++;
}

while (iCount > 0) {
hl.Add("} end " + hlCode.Pop());
iCount--;
}

sText = String.Join("\n", hl.ToArray()).Trim() + "\n";;
sText = Util.RegExpReplaceCase(sText, @"\n\n+", "\n\n");
sText = Util.RegExpReplaceCase(sText, @"\n+\n\}", "\n}");
sText = Util.RegExpReplaceCase(sText, @"\n+el", "\nel");
return sText;
} // PyDent2Brace method

public string PyBrace2Dent(string sText) {
sText = Util.RegExpReplaceCase(sText, @"^\t*\# end \w+$", "");
//sText = Util.RegExpReplaceCase(sText, @"^\t*\#", "#");
//sText = Util.RegExpReplaceCase(sText, @"^\#([^ ])", "# $1");

HomerList hl = new HomerList(sText.Split('\n'));
int i = 0;
int iCount = 0;
char[] a = {' ', '{'};
HomerList hlCode = new HomerList();
string sIndent = App.ReadOption("IndentUnit", "  ");
sIndent = Util.Literalize(sIndent);

while ( i < hl.Count) {
string sPack = hl[i].Trim();
//if (iCount > 0) sLine = "\t".PadLeft(iCount, '\t') + sPack;
string sLine;
if (iCount > 0) sLine = Util.Replicate(sIndent, iCount) + sPack;
else sLine = sPack;

if (sPack.EndsWith("{")) {
sLine = sLine.TrimEnd(a) + ":";
iCount++;
string[] aCode = sPack.Split(' ');
hlCode.Add(aCode[0].TrimEnd('{'));
}
else if (sPack.StartsWith("}")) {
sLine = "# end " + hlCode.Pop();
//if (iCount > 1) sLine = "\t".PadLeft(iCount - 1, '\t') + sLine;
if (iCount > 1) sLine = Util.Replicate(sIndent, iCount - 1) + sLine;
iCount--;
}
hl[i] = sLine;
i++;
}

sText = String.Join("\n", hl.ToArray()).Trim() + "\n";;
sText = Util.RegExpReplaceCase(sText, @"\n+\n", "\n\n");
//sText = Util.RegExpReplaceCase(sText, @"\n+(\t*)el", "\n$1el");
sText = Util.RegExpReplaceCase(sText, @"\n+(" + sIndent + ")el", "\n$1el");
return sText;
} // PyBrace2Dent method

public void HardLineBreak() {
bool bLoop;
string sResult, sTitle, sBody, sText, sLine;
int iWidth, iLength, iIndex, iStart, iEnd, i;
HomerRichTextBox rtb = this.Child.RTB;
if (rtb.SelectionLength == 0) {
sTitle = "Hard Line Break All";
iStart = 0;
iEnd = rtb.TextLength;
}
else {
sTitle = "Hard Line Break Selected";
iStart = rtb.SelectionStart;
iEnd = iStart + rtb.SelectionLength;
}

iWidth = 0;
foreach (string s in rtb.Lines) {
iLength = s.Length;
if (iLength > iWidth) iWidth = iLength;
}

sText = iWidth.ToString();
sResult = Dialog.Input(sTitle, "Width", sText).Trim();
if (sResult.Length == 0) return;

try {
iWidth = Int32.Parse(sResult);
}
catch (Exception ex) {
Dialog.Show("Error", ex.Message);
return;
}

sText = rtb.GetRange(iStart, iEnd);
sBody = "";
iIndex = 0;
iLength = sText.Length;
bLoop = true;

while (bLoop) {
//Dialog.Show("index", iIndex);
if (iLength - iIndex <= iWidth) {
sLine = sText.Substring(iIndex);
bLoop = false;
}
else {
sLine = sText.Substring(iIndex, iWidth);
i = sLine.LastIndexOf("\n");
//Dialog.Show("break", i);
if (i >=0) {
sLine = sLine.Substring(0, i + 1);
}
else {
i = sLine.LastIndexOf(" ");
//Dialog.Show("space", i);
if (i >=0) sLine = sLine.Substring(0, i + 1);
}
}
sBody += sLine.TrimEnd('\n') + "\n";
iIndex += sLine.Length;
}

rtb.ReplaceRange(iStart, iEnd, sBody);
rtb.Index = iStart + sText.Length;
Util.Say(rtb.RowText);
} // HardLineBreak method

public void Mail(bool bAttach) {
bool bCreate, bVisible, bSendMailAttach;
int iDisplayAlerts;
string sText, sFile, sDir;
object oApp, oOptions, oDocs, oDoc;

HomerRichTextBox rtb = this.Child.RTB;
sText = rtb.Text;

if (sText.Length == 0) {
AddMessage("No text!");
return;
}
sText = Util.Convert2WinLineBreak(sText);
sFile = this.Child.Text;
sDir = Path.GetTempPath();
sFile = Path.Combine(sDir, sFile);
if (Path.GetExtension(sFile).Length == 0) sFile += ".txt";
//Util.String2File(sText, sFile);
Util.String2File(sText, App.TempFile);
App.TempFiles.Add(sFile);

bool bAppVisible = false;
//oApp = COM.GetOrCreateObject("Word.Application", out bCreate);
oApp = COM.WordAccess(out bCreate);
bVisible = (bool) COM.GetProperty(oApp, "Visible");
iDisplayAlerts = (int) COM.GetProperty(oApp, "DisplayAlerts");
COM.SetProperty(oApp, "Visible", bAppVisible);
COM.SetProperty(oApp, "DisplayAlerts", 0);
oOptions = COM.GetProperty(oApp, "Options");
bSendMailAttach = (bool) COM.GetProperty(oOptions, "SendMailAttach");
COM.SetProperty(oOptions, "SendMailAttach", bAttach);
oDocs = COM.GetProperty(oApp, "Documents");
oDoc = VB.WordOpen(oDocs, App.TempFile, bAppVisible);
if (File.Exists(sFile)) File.Delete(sFile);
VB.WordSaveAs(oDoc, sFile, 2);
COM.CallMethod(oDoc, "SendMail");
VB.WordClose(oDoc);
COM.Release(ref oDoc);
COM.Release(ref oDocs);
if (bCreate) {
//VB.WordQuit(oApp);
}
else {
COM.SetProperty(oApp, "Visible", bVisible);
COM.SetProperty(oApp, "DisplayAlerts", iDisplayAlerts);
COM.SetProperty(oOptions, "SendMailAttach", bSendMailAttach);
}
COM.Release(ref oOptions);
COM.Release(ref oApp);
File.Delete(sFile);

App.Frame.Activate();
App.Frame.Child.RTB.Select();
} // MailBody method

// SPRAWDZANIE PISOWNI (F7).
//
// Najpierw probuje sprawdzania wbudowanego w Windows (Pisownia.cs): zna polski,
// nie wymaga Worda i nie wyrzuca uzytkownika z edytora.  Gdy system tego nie ma
// albo nie zna wybranego jezyka, spada na stara droge przez Worda - zeby nikomu
// nie zabrac dzialajacej funkcji.
// Na zadanie: wpis SpellUseWord=Y w [Options] pliku EdSharpNG.ini wymusza Worda.
public void SpellCheck() {
bool bWymusWord = App.ReadOption("SpellUseWord", "N").ToUpper().StartsWith("Y");
if (!bWymusWord && Pisownia.Dostepne()) { SpellCheckSystem(); return; }
SpellCheckWord();
} // SpellCheck method

// Sprawdzanie pisowni sprawdzaczem systemu Windows.  Idzie blad za bledem: dla
// kazdego pokazuje slowo, jego OTOCZENIE (bez kontekstu nie widac, o ktore
// miejsce chodzi) i liste podpowiedzi.  Kursor w dokumencie ustawia sie na
// biezacym bledzie, zeby czytnik ekranu czytal to samo miejsce, ktore widac.
// Jeden blad w trakcie poprawiania - stan trzymam obok samego bledu, bo
// lista bledow i lista poprawek musza sie zgadzac az do konca pracy.
class BladWTrakcie {
public Pisownia.Blad B;
public string Nowe = "";      // pusty = jeszcze nie poprawione
public bool Zrobione = false; // poprawione, pominiete albo dodane do slownika
public string Los = "";       // co sie z nim stalo - do podsumowania
}

public void SpellCheckSystem() {
HomerRichTextBox rtb = this.Child.RTB;
int iBaza;
string sText;
// SPRAWDZANIE OD KURSORA, NIE OD POCZATKU PLIKU (12.09.2026, na prosbe
// Kasperczaka: "Sprawdzanie F7 od kursora a nie od poczatku pliku").
// Powod jest praktyczny: przy dlugim tekscie, do ktorego dopisuje sie na
// koncu, sprawdzanie od gory przeprowadza uzytkownika przez setki spraw
// juz raz rozstrzygnietych, zanim dojdzie do zdania, ktore wlasnie
// napisal. Zaznaczenie ma pierwszenstwo - gdy cos jest zaznaczone,
// sprawdzamy dokladnie to i nic wiecej.
//
// Gdy od kursora do konca nie ma nic do poprawienia, NIE konczymy w
// ciszy "nie ma bledow" - to bylby falszywy spokoj, skoro wyzej moglo
// zostac cokolwiek. Pytamy wtedy, czy sprawdzic tekst od poczatku.
bool bOdKursora = false;
if (rtb.SelectionLength > 0) { AddMessage("Selected"); iBaza = rtb.SelectionStart; sText = rtb.SelectedText; }
else if (rtb.SelectionStart > 0) {
bOdKursora = true; iBaza = rtb.SelectionStart; sText = rtb.Text.Substring(iBaza);
AddMessage("From cursor");
}
else { AddMessage("All"); iBaza = 0; sText = rtb.Text; }
if (sText.Length == 0) { AddMessage("No text!"); return; }

List<Pisownia.Blad> lBledy = Pisownia.Sprawdz(sText);
if (lBledy.Count == 0 && bOdKursora) {
// Od kursora czysto - proponujemy calosc, zeby brak bledow nie znaczyl
// czegos innego, niz uzytkownik uslyszal.
if (String.Equals(Dialog.Confirm("Spelling", "No spelling errors from the cursor to the end.\n\nCheck the whole document from the beginning?", "Y"), "Y")) {
iBaza = 0; sText = rtb.Text; bOdKursora = false;
lBledy = Pisownia.Sprawdz(sText);
}
}
if (lBledy.Count == 0) {
string sGdzie = bOdKursora ? "No spelling errors from the cursor to the end" : "No spelling errors found";
AddMessage(sGdzie); Say.say(sGdzie); return;
}

List<BladWTrakcie> lStan = new List<BladWTrakcie>();
foreach (Pisownia.Blad b in lBledy) { BladWTrakcie w = new BladWTrakcie(); w.B = b; lStan.Add(w); }

int iPoprawionych = 0, iDodanych = 0, iPominietych = 0, iIgnorowanych = 0;
bool bPrzerwane = false;

// LISTA BLEDOW NA WIERZCHU (12.09.2026, zgloszenie Kasperczaka: "Nie
// powinna ta lista bledow byc na wierzchu i potem dopiero tabem na
// sugestie").  Powod jest praktyczny, nie kosmetyczny: przy przechodzeniu
// blad za bledem uzytkownik slyszy JEDNA propozycje i nie wie ani ile
// bledow zostalo, ani ze wiekszosc z nich to nazwiska i skroty, ktorych
// poprawiac nie chce.  Z listy widzi calosc i wybiera, czym sie zajac.
// Przy jednym bledzie listy nie pokazuje - byloby to puste klikniecie.
bool bLista = lStan.Count > 1;
int iWybrany = 0;

while (true) {
if (bLista) {
int iZostalo = 0;
foreach (BladWTrakcie w in lStan) if (!w.Zrobione) iZostalo++;
if (iZostalo == 0) break;

List<string> lWiersze = new List<string>();
List<int> lMapa = new List<int>();   // wiersz listy -> numer bledu
for (int i = 0; i < lStan.Count; i++) {
if (lStan[i].Zrobione) continue;
lMapa.Add(i);
lWiersze.Add(String.Format("{0} - {1}", lStan[i].B.Slowo, Otoczenie(sText, lStan[i].B.Start, lStan[i].B.Dlugosc)));
}
// Zaznaczenie wraca na blad, przy ktorym uzytkownik ostatnio byl - po
// poprawieniu jednego nie zaczyna sie od gory listy.
int iZazn = lMapa.IndexOf(iWybrany);
if (iZazn < 0) { for (int k = 0; k < lMapa.Count; k++) if (lMapa[k] >= iWybrany) { iZazn = k; break; } }
if (iZazn < 0) iZazn = lWiersze.Count - 1;

LbcDialog dlgL = new LbcDialog(String.Format("Spelling: {0} of {1} to check", iZostalo, lStan.Count), App.Frame);
ListBox lstB = dlgL.addListBox("Misspelled words", lWiersze, lWiersze[iZazn]);
dlgL.setInitialFocus(lstB);
// Kursor w dokumencie idzie za wyborem z listy, zeby czytnik czytal to
// samo miejsce, ktore jest zaznaczone w oknie.
{
ListBox lstL = lstB; List<int> lMapaL = lMapa; HomerRichTextBox rtbL = rtb; int iBazaL = iBaza; List<BladWTrakcie> lStanL = lStan;
lstB.SelectedIndexChanged += delegate(object s, EventArgs e) {
int ix = lstL.SelectedIndex;
if (ix < 0 || ix >= lMapaL.Count) return;
Pisownia.Blad bb = lStanL[lMapaL[ix]].B;
rtbL.Index = iBazaL + bb.Start;
rtbL.Select(iBazaL + bb.Start, bb.Dlugosc);
};
}
string sBtnL = dlgL.runWithButtons(new string[] {"Correct", "&Add to dictionary", "&Ignore all", "Finish"});
int iSel = lstB.SelectedIndex;
dlgL.Dispose();
if (sBtnL.Length == 0 || String.Equals(sBtnL, "Finish", StringComparison.OrdinalIgnoreCase)) { bPrzerwane = true; break; }
if (iSel < 0 || iSel >= lMapa.Count) continue;
iWybrany = lMapa[iSel];

// "Add to dictionary" WPROST Z LISTY (12.09.2026, prosba Kasperczaka:
// "Lepiej zeby na wierzchu przed wejsciem w korekte danego slowa bylo
// dodaje do slownika, jezeli widzimy, ze slowo jest OK").  Zdecydowana
// wiekszosc tego, co sprawdzacz zglasza w polskim tekscie, to nazwiska,
// skroty i nazwy wlasne - slowa poprawne, ktorych nie ma w slowniku.
// Otwieranie dla kazdego z nich okna z podpowiedziami, ktorych i tak sie
// nie uzyje, to czysty koszt.
//
// Dodanie do slownika ZARAZEM zdejmuje blad ze wszystkich wystapien (i
// z przyszlych sprawdzen), bo slowo uznane za poprawne przestaje byc
// bledem - nie trzeba go osobno ignorowac.
if (String.Equals(sBtnL, "Add to dictionary", StringComparison.OrdinalIgnoreCase)) {
string sSl = lStan[iWybrany].B.Slowo;
if (Pisownia.Dodaj(sSl)) {
iDodanych++;
int iIle = OznaczWszystkie(lStan, sSl, "added");
AddMessage(String.Format("Added: {0} ({1})", sSl, iIle));
}
else AddMessage("Could not add word");
continue;
}

// "Ignore all" z listy - dla nazwiska, ktore wraca kilkanascie razy, bez
// wchodzenia w okno poprawiania.
if (String.Equals(sBtnL, "Ignore all", StringComparison.OrdinalIgnoreCase)) {
string sSl = lStan[iWybrany].B.Slowo;
Pisownia.Pomijaj(sSl);
int iIle = OznaczWszystkie(lStan, sSl, "ignored");
iIgnorowanych += iIle;
AddMessage(String.Format("Ignored: {0} ({1})", sSl, iIle));
continue;
}
} else {
// Bez listy: idziemy po kolei pierwszym niezrobionym.
iWybrany = -1;
for (int i = 0; i < lStan.Count; i++) if (!lStan[i].Zrobione) { iWybrany = i; break; }
if (iWybrany < 0) break;
}

BladWTrakcie w2 = lStan[iWybrany];
Pisownia.Blad b = w2.B;
rtb.Index = iBaza + b.Start;   // czytnik ekranu idzie za kursorem
rtb.Select(iBaza + b.Start, b.Dlugosc);

int iIleRazy = IleWystapien(lStan, b.Slowo);
string sOtoczenie = Otoczenie(sText, b.Start, b.Dlugosc);
LbcDialog dlg = new LbcDialog(String.Format("Spelling: {0}", b.Slowo), App.Frame);
dlg.addLabel(String.Format("Not in dictionary: {0}", b.Slowo));
dlg.addLabel(String.Format("Context: {0}", sOtoczenie));
if (iIleRazy > 1) dlg.addLabel(String.Format("This word occurs {0} times", iIleRazy));

// KOLEJNOSC: najpierw LISTA PODPOWIEDZI z fokusem, potem pole tekstowe
// (12.09.2026, zgloszenie Kasperczaka).  Powod: gdy fokus startowal w
// polu z pierwsza propozycja, uzytkownik slyszal JEDNA wersje i nie
// wiedzial, ze sa inne - lista byla schowana za polem.  Teraz slyszy
// "3 podpowiedzi, komputer" i strzalka sprawdza reszte.
List<string> lWybor = new List<string>(b.Podpowiedzi);
ListBox lst = null;
if (lWybor.Count > 0) lst = dlg.addListBox("Suggestions", lWybor, lWybor[0]);
TextBox txt = dlg.addInputBox("Replace with", b.Podpowiedzi.Count > 0 ? b.Podpowiedzi[0] : b.Slowo);
// Wybor z listy przepisuje sie do pola tekstowego, zeby dalo sie i wybrac
// podpowiedz, i dopisac wlasna wersje - bez przeskakiwania miedzy trybami.
if (lst != null) {
ListBox lstL = lst; TextBox txtL = txt;
lst.SelectedIndexChanged += delegate(object s, EventArgs e) { if (lstL.SelectedItem != null) txtL.Text = lstL.SelectedItem.ToString(); };
dlg.setInitialFocus(lst);
} else {
dlg.setInitialFocus(txt);   // brak podpowiedzi - jedyne, co da sie zrobic, to wpisac wlasna wersje
}

// PRZYCISKI, nie pole kombi (decyzja Kasperczaka 12.09.2026: "Pomin raz i
// Ignoruj czyli pomin w calym tekscie.  To dwie osobne opcje").  Przycisk
// robi rzecz od razu i sam mowi, czym jest; kombi wymaga wybrania trybu, a
// potem zatwierdzenia - dwie czynnosci i trzeba pamietac, co jest wybrane.
// "Replace all" tylko gdy slowo faktycznie wraca - inaczej byloby to
// martwym przyciskiem do przetabowania.
List<string> lPrzyciski = new List<string>();
lPrzyciski.Add("&Replace");
if (iIleRazy > 1) lPrzyciski.Add("Replace a&ll");
lPrzyciski.Add("Ski&p");
lPrzyciski.Add("&Ignore all");
lPrzyciski.Add("&Add to dictionary");
lPrzyciski.Add("Cancel");
string sBtn = dlg.runWithButtons(lPrzyciski.ToArray());
string sNowe = txt.Text;
dlg.Dispose();

if (sBtn.Length == 0 || String.Equals(sBtn, "Cancel", StringComparison.OrdinalIgnoreCase)) {
// Anulowanie w oknie jednego bledu wraca DO LISTY, nie konczy calego
// sprawdzania - inaczej pomylka kosztowalaby cala prace.  Sprawdzanie
// konczy przycisk Finish na liscie albo Escape na niej.
if (bLista) continue;
bPrzerwane = true; break;
}
if (String.Equals(sBtn, "Add to dictionary", StringComparison.OrdinalIgnoreCase)) {
if (Pisownia.Dodaj(b.Slowo)) {
iDodanych++;
OznaczWszystkie(lStan, b.Slowo, "added");
AddMessage(String.Format("Added: {0}", b.Slowo));
}
else AddMessage("Could not add word");
continue;
}
if (String.Equals(sBtn, "Ignore all", StringComparison.OrdinalIgnoreCase)) {
Pisownia.Pomijaj(b.Slowo);
int iIle = OznaczWszystkie(lStan, b.Slowo, "ignored");
iIgnorowanych += iIle;
AddMessage(String.Format("Ignored: {0} ({1})", b.Slowo, iIle));
continue;
}
if (String.Equals(sBtn, "Skip", StringComparison.OrdinalIgnoreCase)) {
// POMIN RAZ - tylko to jedno wystapienie; kolejne beda pytane znowu.
w2.Zrobione = true; w2.Los = "skipped"; iPominietych++;
continue;
}
if (sNowe.Length == 0 || sNowe == b.Slowo) { w2.Zrobione = true; w2.Los = "unchanged"; continue; }

if (String.Equals(sBtn, "Replace all", StringComparison.OrdinalIgnoreCase)) {
int iIle = 0;
foreach (BladWTrakcie w3 in lStan) {
if (w3.Zrobione || w3.B.Slowo != b.Slowo) continue;
w3.Nowe = sNowe; w3.Zrobione = true; w3.Los = "replaced"; iIle++;
}
iPoprawionych += iIle;
AddMessage(String.Format("Replaced {0}: {1} -> {2}", iIle, b.Slowo, sNowe));
continue;
}

w2.Nowe = sNowe; w2.Zrobione = true; w2.Los = "replaced"; iPoprawionych++;
} // while - lista albo kolejne bledy

// Poprawki nakladam OD KONCA, bo kazda zmienia dlugosc tekstu, a wtedy
// pozycje kolejnych bledow przestalyby sie zgadzac.
List<BladWTrakcie> lDoZamiany = new List<BladWTrakcie>();
foreach (BladWTrakcie w in lStan) if (w.Los == "replaced" && w.Nowe.Length > 0) lDoZamiany.Add(w);
lDoZamiany.Sort(delegate(BladWTrakcie x, BladWTrakcie y) { return y.B.Start.CompareTo(x.B.Start); });
foreach (BladWTrakcie w in lDoZamiany) {
int iOd = iBaza + w.B.Start;
rtb.ReplaceRange(iOd, iOd + w.B.Dlugosc, w.Nowe);
}
rtb.Index = iBaza;

// Podsumowanie wymienia tylko to, co faktycznie sie stalo - zerowe liczniki
// sa halasem, przez ktory trzeba przesluchac cale zdanie.
List<string> lCzesci = new List<string>();
if (iPoprawionych > 0) lCzesci.Add(String.Format("{0} replaced", iPoprawionych));
if (iDodanych > 0) lCzesci.Add(String.Format("{0} added to dictionary", iDodanych));
if (iIgnorowanych > 0) lCzesci.Add(String.Format("{0} ignored", iIgnorowanych));
if (iPominietych > 0) lCzesci.Add(String.Format("{0} skipped", iPominietych));
if (lCzesci.Count == 0) lCzesci.Add("nothing changed");
string sPodsumowanie = String.Join(", ", lCzesci.ToArray()) + (bPrzerwane ? ", stopped" : "");
AddMessage(sPodsumowanie);
Say.say(sPodsumowanie);
} // SpellCheckSystem method

// Ile razy ten sam wyraz jest jeszcze do sprawdzenia - decyduje o tym, czy
// pokazac "Replace all" i czy zapowiedziec powtorzenia.
static int IleWystapien(List<BladWTrakcie> lStan, string sSlowo) {
int iIle = 0;
foreach (BladWTrakcie w in lStan) if (!w.Zrobione && w.B.Slowo == sSlowo) iIle++;
return iIle;
}

// Oznacza WSZYSTKIE niezrobione wystapienia wyrazu - uzywane przez "Ignore
// all" i dodanie do slownika; bez tego ten sam wyraz wrocilby w liscie, choc
// uzytkownik juz o nim zdecydowal.
static int OznaczWszystkie(List<BladWTrakcie> lStan, string sSlowo, string sLos) {
int iIle = 0;
foreach (BladWTrakcie w in lStan) {
if (w.Zrobione || w.B.Slowo != sSlowo) continue;
w.Zrobione = true; w.Los = sLos; iIle++;
}
return iIle;
}

// Otoczenie slowa - do przeczytania na glos, zeby bylo wiadomo, o ktore
// miejsce w tekscie chodzi.  Lamania linii zamieniam na odstepy, bo w jednej
// linijce okna i tak sie nie pokaza, a czytnik czytalby je jako przerwy.
static string Otoczenie(string sText, int iStart, int iDlugosc) {
int iOd = Math.Max(0, iStart - 40);
int iDo = Math.Min(sText.Length, iStart + iDlugosc + 40);
string s = sText.Substring(iOd, iDo - iOd).Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
while (s.Contains("  ")) s = s.Replace("  ", " ");
return (iOd > 0 ? "..." : "") + s.Trim() + (iDo < sText.Length ? "..." : "");
} // Otoczenie method

public void SpellCheckWord() {
bool bCreate, bVisible;
int iDisplayAlerts, iStart, iEnd, iLength;
string sText, sOldText;
object oApp, oDocs, oDoc, oSelection;

HomerRichTextBox rtb = this.Child.RTB;
if (rtb.SelectionLength == 0) {
AddMessage("All");
iStart = 0;
sText = rtb.Text;
}
else {
AddMessage("Selected");
iStart = rtb.SelectionStart;
sText = rtb.SelectedText;
}

if (sText.Length == 0) {
AddMessage("No text!");
return;
}

iEnd = iStart + sText.Length;
sText = sText.TrimEnd();
sOldText = sText;
sText = Util.Convert2MacLineBreak(sText);

bool bAppVisible = true;
//oApp = COM.GetOrCreateObject("Word.Application", out bCreate);
oApp = COM.WordAccess(out bCreate);
bVisible = (bool) COM.GetProperty(oApp, "Visible");
iDisplayAlerts = (int) COM.GetProperty(oApp, "DisplayAlerts");
COM.SetProperty(oApp, "Visible", bAppVisible);
COM.SetProperty(oApp, "DisplayAlerts", 0);
oDocs = COM.GetProperty(oApp, "Documents");
oDoc = COM.CallMethod(oDocs, "Add");
COM.CallMethod(oDoc, "Activate");
oSelection = COM.GetProperty(oApp, "Selection");
COM.CallMethod(oSelection, "TypeText", sText);
Util.ActivateProcess("WinWord");
COM.CallMethod(oDoc, "CheckSpelling");
iLength = (int) COM.GetProperty(oSelection, "StoryLength");
COM.CallMethod(oSelection, "SetRange", new object[] {0, iLength});
sText = (string) COM.GetProperty(oSelection, "Text");
sText = sText.Trim();
COM.Release(ref oSelection);
VB.WordClose(oDoc);
COM.Release(ref oDoc);
COM.Release(ref oDocs);
if (bCreate) {
//VB.WordQuit(oApp);
}
else {
COM.SetProperty(oApp, "Visible", bVisible);
COM.SetProperty(oApp, "DisplayAlerts", iDisplayAlerts);
}
COM.Release(ref oApp);

App.Frame.Activate();
App.Frame.Child.RTB.Select();
sText = Util.Convert2UnixLineBreak(sText);
if (sText == sOldText) AddMessage("No changes!");
else {
rtb.ReplaceRange(iStart, iEnd, sText);
rtb.Index = iStart;
//AddMessage("Done");
}
} // SpellCheck method

public void Thesaurus() {
bool bCreate, bVisible;
int iDisplayAlerts, iStart, iEnd, iLength;
string sText, sOldText;
object[] aResults;
object oApp, oDocs, oDoc, oSelection, oRange;

HomerRichTextBox rtb = this.Child.RTB;
if (rtb.SelectionLength == 0) {
//AddMessage("Chunk");
aResults = GetChunk();
iStart = (int) aResults[0];
sText = (string) aResults[1];
}
else {
//AddMessage("Selected");
iStart = rtb.SelectionStart;
sText = rtb.SelectedText;
}

sText = sText.TrimEnd();
if (sText.Length == 0) {
AddMessage("No text!");
return;
}

iEnd = iStart + sText.Length;
sOldText = sText;
sText = Util.Convert2MacLineBreak(sText);

bool bAppVisible = true;
//oApp = COM.GetOrCreateObject("Word.Application", out bCreate);
oApp = COM.WordAccess(out bCreate);
bVisible = (bool) COM.GetProperty(oApp, "Visible");
iDisplayAlerts = (int) COM.GetProperty(oApp, "DisplayAlerts");
COM.SetProperty(oApp, "Visible", bAppVisible);
COM.SetProperty(oApp, "DisplayAlerts", 0);
oDocs = COM.GetProperty(oApp, "Documents");
oDoc = COM.CallMethod(oDocs, "Add");
oSelection = COM.GetProperty(oApp, "Selection");
COM.CallMethod(oSelection, "TypeText", sText);
oRange = COM.GetProperty(oSelection, "Range");
Util.ActivateProcess("WinWord");
COM.CallMethod(oRange, "CheckSynonyms");
iLength = (int) COM.GetProperty(oSelection, "StoryLength");
COM.CallMethod(oSelection, "SetRange", new object[] {0, iLength});
sText = (string) COM.GetProperty(oSelection, "Text");
sText = sText.Trim();
COM.Release(ref oRange);
COM.Release(ref oSelection);
VB.WordClose(oDoc);
COM.Release(ref oDoc);
COM.Release(ref oDocs);
if (bCreate) {
//VB.WordQuit(oApp);
}
else {
COM.SetProperty(oApp, "Visible", bVisible);
COM.SetProperty(oApp, "DisplayAlerts", iDisplayAlerts);
}
COM.Release(ref oApp);

App.Frame.Activate();
App.Frame.Child.RTB.Select();
sText = Util.Convert2UnixLineBreak(sText);
if (sText == sOldText) AddMessage("No changes!");
else {
rtb.ReplaceRange(iStart, iEnd, sText);
rtb.Index = iStart;
//AddMessage("Done");
}
} // Thesaurus method

// bForce = user SWIADOMIE wybral ponowna instalacje biezacej wersji
// (menu Reinstall Current Version).  Przy zwyklym sprawdzaniu aktualizacji
// (F11) bForce jest false i rownosc wersji konczy sie samym komunikatem.
public void ElevateVersion() { ElevateVersion(false); }
public void ElevateVersion(bool bForce) {
// Check GitHub for the latest EdSharp release and, if the user agrees, download
// and run its installer.  This replaces the old AppStamp.ini / Win32.Url2File
// mechanism with the GitHub Releases approach used by the sibling DbDo project.
// All network work goes through Homer.Web, which sends a User-Agent and uses
// modern TLS.  EdSharp is not closed here: the Inno Setup installer detects the
// running EdSharp and offers to close it before proceeding.
// AKTUALIZACJA CIAGNIE Z NASZEGO REPO, NIE Z UPSTREAMU (naprawa 06.09.2026).
// Do tej pory stalo tu "JamalMazrui/EdSharp" - repozytorium autora oryginalnego
// programu, ktore odziedziczylismy z forka.  Skutek zmierzony: F11 pobieralo
// EdSharp_Setup.exe autora (dzis tag v5.0.40) i URUCHAMIALO ten instalator, czyli
// nadpisywalo EdSharpNG CUDZYM programem.  Dla osoby niewidomej to najgorszy
// rodzaj awarii: klawisz nazywa sie "aktualizuj", a odbiera wszystkie nasze
// funkcje bez jednego slowa ostrzezenia.
//
// REPOZYTORIUM ZMIENIONE NA michalkasperczak/EdSharpNG (11.09.2026).  Poprzednie
// "michaldziwisz/EdSharp" tez nie bylo nasze i nie mialo ANI JEDNEGO wydania, co
// zmierzone: API zwracalo 404.  Wydania ida teraz do repozytorium wlasciciela
// programu, wiec F11 wreszcie ma skad brac paczki.
string sOwnerRepo = "michalkasperczak/EdSharpNG";
string sReleasesUrl = "https://github.com/" + sOwnerRepo + "/releases/latest";
string sName = "EdSharpNG_Setup.exe";

Util.Say("Checking for updates");
int iHttp;
string sNotes, sAssetUrl;
string sTag = Util.FetchLatestRelease(sOwnerRepo, sName, out sNotes, out sAssetUrl, out iHttp);
if (sTag.Length == 0) {
// DWA ROZNE POWODY, DWA ROZNE KOMUNIKATY.  404 znaczy, ze serwer odpowiedzial
// i wydania po prostu nie ma - mowic wtedy o "sprawdz polaczenie z internetem"
// byloby wysylaniem czlowieka do naprawy czegos, co dziala.
if (iHttp == 404)
Dialog.Show("Elevate Version", "No release has been published for EdSharpNG yet, so there is nothing to update to.\nYou are running version " + App.VersionString + ".\nTest builds are delivered to you directly, outside this command.");
else
Dialog.Show("Elevate Version", "Could not check for updates right now.\nPlease check your internet connection and try again.\nYou can also download the latest installer from\n" + sReleasesUrl);
return;
}

string sLocal = App.VersionString;
string sLatest = sTag.TrimStart('v', 'V').Trim();
int iCompare = Util.CompareVersions(sLatest, sLocal);
// GDY WERSJA JEST AKTUALNA, NIE PYTAMY O NIC (poprawka 11.09.2026).
// Do tej pory rownosc wersji konczyla sie pytaniem "Download and install
// the latest release from the web now?" - czyli program pytal user, czy
// pobrac to, co juz ma.  Michal zglosil to wprost: "to nie powinno byc
// tak/nie, jesli mam najnowsza".  Mial racje: pytanie bez sensownej
// odpowiedzi to nie ostroznosc, tylko przerzucanie decyzji na user.
// Teraz mowimy jedno zdanie i konczymy.  Ponowne pobranie tej samej
// wersji zostaje mozliwe, ale jako SWIADOMY wybor z menu (Reinstall
// Current Version), a nie jako pytanie, ktore wyskakuje samo.
if (iCompare == 0 && !bForce) {
Dialog.Show("Elevate Version", "EdSharpNG " + sLocal + " is up to date.");
return;
}
string sDefault = "N";
string sMsg;
if (iCompare > 0) {
sMsg = "A newer EdSharp is available.\nInstalled: " + sLocal + "\nAvailable: " + sLatest + "\n\nDownload and run the new installer now?";
sDefault = "Y";
}
else if (iCompare == 0) sMsg = "EdSharpNG " + sLocal + " is already the latest release.\n\nDownload and install it again anyway?";
else sMsg = "EdSharp's version number (" + sLocal + ") is higher than the latest public release (" + sLatest + ").\n\nDownload and install the latest public release from the web anyway?";
if (Dialog.Confirm("Elevate Version", sMsg, sDefault) != "Y") return;

Util.Say("Downloading installer");
// Adres z API wydania; skladanie z nazwy tylko wtedy, gdy API go nie oddalo.
string sUrl = (sAssetUrl.Length > 0) ? sAssetUrl : (sReleasesUrl + "/download/" + sName);
string sFile = Homer.Web.download(sUrl, Path.GetTempPath(), sName);
if (sFile.Length == 0) {
Dialog.Show("Elevate Version", "The download did not complete.\nYou can download the installer manually from\n" + sReleasesUrl);
return;
}

// BEZPIECZNA AKTUALIZACJA ZNACZY SPRAWDZONA PACZKA (dolozone 11.09.2026).
// W opisie wydania publikujemy sume SHA-256 instalatora.  Tutaj liczymy ja z
// pobranego pliku i porownujemy.  Instalator, ktory nie zgadza sie z suma, NIE
// jest uruchamiany - to jedyny moment, w ktorym program moze wykryc, ze zamiast
// naszej paczki przyszlo cos innego (uszkodzone pobranie, podmiana w drodze).
// Gdy w wydaniu sumy NIE MA, mowimy to wprost i pytamy o zgode, zamiast cicho
// pominac kontrole - milczenie kazaloby userowi wierzyc, ze sprawdzenie bylo.
string sExpected = "";
Match matchSum = Regex.Match(sNotes == null ? "" : sNotes, "\\b([0-9a-fA-F]{64})\\b");
if (matchSum.Success) sExpected = matchSum.Groups[1].Value.ToUpperInvariant();
if (sExpected.Length == 64) {
Util.Say("Checking the installer");
string sActual = Util.FileSha256(sFile);
if (sActual.Length == 0) {
Dialog.Show("Elevate Version", "The downloaded installer could not be checked, so it was NOT started.\nThe file is here, if you want to inspect it yourself:\n" + sFile);
return;
}
if (sActual != sExpected) {
// Plik kasujemy: zostawienie w katalogu tymczasowym pliku o nazwie
// EdSharpNG_Setup.exe, ktory nie jest nasza paczka, samo w sobie jest pulapka.
try { File.Delete(sFile); } catch {}
Dialog.Show("Elevate Version", "STOP: the downloaded installer does not match the checksum published with release " + sTag + ".\nIt was NOT started and has been deleted.\n\nThis can mean a broken download, or a file that is not ours.\nTry again later, or download it yourself from\n" + sReleasesUrl);
return;
}
}
else {
if (Dialog.Confirm("Elevate Version", "Release " + sTag + " does not publish a checksum, so EdSharp cannot verify that the downloaded installer is genuine.\n\nRun it anyway?", "N") != "Y") {
Dialog.Show("Elevate Version", "The installer was not started.\nThe downloaded file is here:\n" + sFile);
return;
}
}

Util.Say("Starting installer");
// PROGRAM ZAMYKA SIE SAM PRZED AKTUALIZACJA (poprawka 12.09.2026).
//
// Michal zglosil: przy aktualizacji instalator pokazywal "Setup has detected
// that EdSharpNG is currently running. Please close all instances of it now".
// To nasza wina, nie jego: program uruchamial instalator i DALEJ dzialal.
//
// Dlaczego CloseApplications=force w skrypcie instalatora tego nie zalatwilo:
// to dwa rozne mechanizmy. AppMutex jest sprawdzany na samym POCZATKU (zanim
// pojawi sie pierwsze okno instalatora) i konczy sie wlasnie tym zdaniem,
// a CloseApplications dziala DUZO pozniej, na etapie "Preparing to install".
// Skoro nasz mutex zyje, gdy instalator startuje, pytanie MUSI sie pojawic.
// Poprawka moze byc tylko po naszej stronie: wyjsc, zanim instalator wstanie.
//
// KOLEJNOSC JEST TU WAZNA i celowa:
// 1. Najpierw zamykamy otwarte pliki normalna droga (ExitApp -> CloseWindow),
//    czyli z pytaniem o zapis tego, co niezapisane. Aktualizacja NIE MOZE
//    kosztowac nikogo niezapisanej pracy.
// 2. Gdy uzytkownik w ktoryms z tych pytan wybierze Anuluj, przerywamy CALA
//    aktualizacje - anulowanie zapisu znaczy "nie teraz", a nie "zapomnij, co
//    napisalem". Plik instalatora zostaje na dysku i mowimy, gdzie lezy.
// 3. Instalator odpalamy z KILKUSEKUNDOWYM opoznieniem, przez cmd.exe. Bez
//    tego byloby wyscig: my dopiero konczymy prace (zwalniamy mutex, zapisujemy
//    ustawienia), a instalator w tym czasie juz sprawdza mutex i znow pyta.
//    Kilka sekund to zapas na spokojne zamkniecie, a nie "chyba wystarczy".
bool bLaunched = false;
string sDelayedError = "";
try {
// Opoznienie: ping do siebie zamiast "timeout" - timeout.exe wymaga
// prawdziwej konsoli i w tle potrafi zakonczyc sie bledem, ping dziala
// wszedzie. 5 pakietow to ok. 4 sekundy.
ProcessStartInfo psiDelayed = new ProcessStartInfo();
psiDelayed.FileName = "cmd.exe";
psiDelayed.Arguments = "/c ping -n 5 127.0.0.1 >nul & start \"\" \"" + sFile + "\"";
psiDelayed.UseShellExecute = false;
psiDelayed.CreateNoWindow = true;
Process.Start(psiDelayed);
bLaunched = true;
}
catch (Exception ex) {
sDelayedError = ex.Message;
}

if (!bLaunched) {
// Awaryjnie: stara droga, bez opoznienia. Wtedy pytanie o zamkniecie moze
// sie pojawic - ale to lepsze niz brak aktualizacji.
try {
ProcessStartInfo processStartInfo = new ProcessStartInfo();
processStartInfo.FileName = sFile;
processStartInfo.UseShellExecute = true;
Process.Start(processStartInfo);
bLaunched = true;
}
catch (Exception ex) {
Dialog.Show("Elevate Version", "The installer downloaded but could not be started.\n" + ex.Message + "\n\nThe file is here:\n" + sFile);
return;
}
}

// Teraz wychodzimy. Mowimy o tym wprost, bo samoczynne zamkniecie programu
// bez slowa wygladaloby jak awaria - zwlaszcza przy czytniku ekranu.
Util.Say("Closing EdSharp so the update can install");
if (!ExitApp()) {
// Uzytkownik anulowal zapis - program zostaje otwarty. Instalator juz
// czeka w tle, wiec uczciwie mowimy, co sie stanie.
Dialog.Show("Elevate Version", "The update was not installed, because closing EdSharp was cancelled.\nNothing was lost - your files are still open here.\n\nThe installer is ready at\n" + sFile + "\nand will start in a moment; you can close EdSharp and let it run, or cancel it and update later.");
}
} // ElevateVersion method

public void ReportProblem() {
ReportProblem("", "");
} // ReportProblem method

public void ReportProblem(string sPreSubject, string sPreBody) {
// ZGLOSZENIE PROBLEMU LUB PROSBY O FUNKCJE (dolozone 11.09.2026).
//
// Dwa argumenty sa dla wywolania z okna awarii: temat i slad wyjatku sa juz
// wtedy znane, a uzytkownik ma tylko dopisac, co robil.  Z menu Pomoc oba sa
// puste i formularz otwiera sie czysty.
//
// Okno ma cztery pola i nic wiecej: temat, adres e-mail, rodzaj zgloszenia
// (lista rozwijana) i opis.  Imienia i nazwiska NIE PYTAMY - adres zwrotny
// wystarcza do odpowiedzi, a kazde dodatkowe pole to kolejny przystanek dla
// osoby, ktora wlasnie na cos sie natknela i chce to opisac, nie wypelniac
// ankiety.
//
// PIERWSZA RZECZ TO ZAPIS NA DYSKU, DOPIERO POTEM WYSYLKA.  Tresc, ktora ktos
// napisal, nie moze zniknac przez brak internetu ani przez awarie serwera:
// kopia lezy w katalogu danych programu i komunikat zawsze mowi, gdzie.
// Nie wolno powiedziec "wyslano", kiedy wyslanie sie nie udalo, wiec kazda z
// trzech drog konczy sie osobnym, prawdziwym komunikatem.
string[] asKind = new string[] {
"Something does not work",
"Request for a new feature",
"Question or other remark"
};
string sEmailLast = App.ReadData("ReportEmail", "");

LbcDialog dlg = new LbcDialog("Report a Problem", App.Frame);
TextBox txtSubject = dlg.addInputBox("&Subject", sPreSubject == null ? "" : sPreSubject);
// Adres pamietamy miedzy zgloszeniami - tester zglaszajacy piata rzecz nie ma
// powodu wpisywac go za kazdym razem.
TextBox txtEmail = dlg.addInputBox("Your &e-mail address", sEmailLast);
ComboBox cboKind = dlg.addComboPickBox("&Kind of report", new List<string>(asKind), asKind[0], "");
// Przy awarii slad wyjatku jest juz w polu opisu, a kursor stoi nad nim: user
// dopisuje "co robilem", nie przepisuje komunikatu bledu z pamieci.
string sBodyStart = "";
if (sPreBody != null && sPreBody.Trim().Length > 0)
sBodyStart = "\r\n\r\n--- what EdSharp reported ---\r\n" + sPreBody;
TextBox txtBody = dlg.addTextMemo("&Description", sBodyStart);
bool bOk = dlg.runOkCancel();
string sSubject = bOk ? txtSubject.Text.Trim() : "";
string sEmail = bOk ? txtEmail.Text.Trim() : "";
string sKind = bOk ? (cboKind.Text == null ? "" : cboKind.Text.Trim()) : "";
string sBody = bOk ? txtBody.Text : "";
dlg.Dispose();
if (!bOk) return;

if (sSubject.Length == 0 && sBody.Trim().Length == 0) {
Dialog.Show("Report a Problem", "Nothing was written, so no report was created.");
return;
}
if (sSubject.Length == 0) sSubject = "(no subject)";
if (sKind.Length == 0) sKind = asKind[0];
if (sEmail.Length > 0) App.WriteData("ReportEmail", sEmail);

// Dane techniczne dokladamy sami.  Pytanie testera o wersje programu i system
// jest pytaniem o rzecz, ktora program o sobie wie.
StringBuilder sbEnv = new StringBuilder();
sbEnv.Append("EdSharpNG " + App.VersionString + " (" + Util.GetProgramBuildDate() + ")\r\n");
try { sbEnv.Append("Windows: " + Environment.OSVersion.VersionString + (Environment.Is64BitOperatingSystem ? " 64-bit" : " 32-bit") + "\r\n"); } catch {}
try { sbEnv.Append(".NET: " + Environment.Version.ToString() + "\r\n"); } catch {}
try { sbEnv.Append("Culture: " + CultureInfo.CurrentCulture.Name + "\r\n"); } catch {}
sbEnv.Append("Reported: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n");
string sEnv = sbEnv.ToString();

StringBuilder sbReport = new StringBuilder();
sbReport.Append("Kind: " + sKind + "\r\n");
sbReport.Append("E-mail: " + (sEmail.Length > 0 ? sEmail : "(not given)") + "\r\n");
sbReport.Append("\r\n" + sBody.TrimEnd() + "\r\n");
sbReport.Append("\r\n---\r\n" + sEnv);
string sReport = sbReport.ToString();

// [1] KOPIA NA DYSKU - zawsze, przed jakakolwiek siecia.
string sSaved = "";
try {
string sDir = Path.Combine(App.DataDir, "Reports");
if (!Directory.Exists(sDir)) Directory.CreateDirectory(sDir);
string sFile = Path.Combine(sDir, "report-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
File.WriteAllText(sFile, "Subject: " + sSubject + "\r\n" + sReport, Encoding.UTF8);
sSaved = sFile;
}
catch {}

// [2] WYSYLKA NA NASZ PUNKT ODBIORCZY, gdy jest skonfigurowany.  Adres siedzi w
// pliku ustawien (klucz ReportUrl), a nie w kodzie, zeby zmiana punktu odbioru
// nie wymagala nowej wersji programu i zeby tester mogl zglaszac do wlasnego.
string sUrl = App.ReadData("ReportUrl", "").Trim();
if (sUrl.Length > 0) {
Util.Say("Sending report");
StringBuilder sbJson = new StringBuilder();
sbJson.Append("{\"product\":\"EdSharpNG\"");
sbJson.Append(",\"version\":\"" + Homer.Web.jsonEscape(App.VersionString) + "\"");
sbJson.Append(",\"kind\":\"" + Homer.Web.jsonEscape(sKind) + "\"");
sbJson.Append(",\"subject\":\"" + Homer.Web.jsonEscape(sSubject) + "\"");
sbJson.Append(",\"email\":\"" + Homer.Web.jsonEscape(sEmail) + "\"");
sbJson.Append(",\"body\":\"" + Homer.Web.jsonEscape(sBody) + "\"");
sbJson.Append(",\"environment\":\"" + Homer.Web.jsonEscape(sEnv) + "\"");
sbJson.Append("}");
int iStatus;
string sError;
Homer.Web.post(sUrl, sbJson.ToString(), "application/json", out iStatus, out sError);
if (iStatus >= 200 && iStatus < 300) {
Dialog.Show("Report a Problem", "Your report was sent. Thank you.\r\n\r\nA copy is kept here:\r\n" + (sSaved.Length > 0 ? sSaved : "(the copy could not be written)"));
return;
}
// Wysylka nie wyszla - mowimy to wprost i proponujemy droge zapasowa, zamiast
// udawac sukces albo zostawic czlowieka z komunikatem o kodzie HTTP.
string sWhy = (iStatus == 0) ? "There was no answer from the server (no internet connection, or the server is down)." : ("The server refused the report (HTTP " + iStatus + ").");
if (Dialog.Confirm("Report a Problem", "The report could NOT be sent.\r\n" + sWhy + "\r\n\r\nA copy is saved here:\r\n" + (sSaved.Length > 0 ? sSaved : "(the copy could not be written)") + "\r\n\r\nSend it by e-mail instead?", "Y") != "Y") return;
}

// [3] DROGA ZAPASOWA: otwarcie formularza zgloszen w przegladarce z wypelnionym
// tematem i trescia, albo listu e-mail, gdy opiekun programu ma wpisany adres w
// pliku ustawien (klucz ReportMail).  Ta droga dziala bez naszego serwera, wiec
// jest tym, co zostaje, gdy zawiedzie wszystko inne.
string sMailTo = App.ReadData("ReportMail", "").Trim();
string sOpen;
if (sMailTo.Length > 0)
sOpen = "mailto:" + sMailTo
+ "?subject=" + Homer.Web.urlEncode("[EdSharpNG] " + sSubject)
+ "&body=" + Homer.Web.urlEncode(sReport);
else
sOpen = App.ReportIssuesUrl
+ "?title=" + Homer.Web.urlEncode("[" + sKind + "] " + sSubject)
+ "&body=" + Homer.Web.urlEncode(sReport);
bool bOpened = false;
try {
ProcessStartInfo psiOpen = new ProcessStartInfo();
psiOpen.FileName = sOpen;
psiOpen.UseShellExecute = true;
Process.Start(psiOpen);
bOpened = true;
}
catch {}
string sWhere = (sSaved.Length > 0) ? sSaved : "(the copy could not be written)";
if (bOpened && sMailTo.Length > 0)
Dialog.Show("Report a Problem", "Your e-mail program was opened with the report ready to send.\r\nCheck it and press send there.\r\n\r\nA copy is kept here:\r\n" + sWhere);
else if (bOpened)
Dialog.Show("Report a Problem", "The report form was opened in your web browser with everything filled in.\r\nPress the button that submits it there.\r\n\r\nA copy is kept here:\r\n" + sWhere);
else
Dialog.Show("Report a Problem", "The report could not be sent from EdSharp, and neither your web browser nor an e-mail program answered.\r\n\r\nThe report is saved here, so nothing is lost:\r\n" + sWhere + "\r\n\r\nYou can attach that file to a report at\r\n" + App.ReportIssuesUrl);
} // ReportProblem method

public bool ExitApp() {
while (this.Child != null) {
if (!CloseWindow(this.Child, true)) return false;
}
Application.Exit();
return true;
} // ExitApp method

public bool CloseWindow(MdiChild child) {
bool bExiting = false;
return CloseWindow(child, bExiting);
} // CloseWindow method

public bool CloseWindow(MdiChild child, bool bExiting) {
HomerRichTextBox rtb = child.RTB;
if (rtb.Modified) {
switch (Dialog.Confirm("Confirm", "Save changes to " + child.Text + "?", "Y")) {
case "Y" :
menuFileSave.PerformClick();
if (rtb.Modified) return false;
else break;
case "" :
return false;
}
}

if (bExiting && !rtb.Modified && child.File.IndexOf(@"\") >=0 && !Util.Equiv(child.File, App.IniFile)) Ini.WriteValue(App.IniFile, "Previous", child.File, rtb.Index.ToString());
child.Close();
return true;
} // CloseWindow method

// Persist the guard flag for a file in BOTH stores that remember it.
//
// Guard Document / No Guard used to call SetRecent only, which rewrites
// the Recent entry. But ApplyGuard consults Favorites FIRST, so for a
// favourite file the stale "G" survived and the document opened guarded
// again on the next visit -- the user cleared it with Control+Shift+F7
// over and over and it kept coming back. Update the favourite entry too.
public void SaveGuardFlag(string sFile, bool bGuard) {
if (sFile == null || !sFile.Contains(@"\")) return;
string sStored = App.ReadValue("Favorites", sFile, "");
if (sStored.Length > 0) {
HomerList hlFav = new HomerList(sStored);
for (int i = 0; i < hlFav.Count; i++) {
string sSeg = hlFav[i];
if (sSeg == "G" || sSeg == "M") hlFav[i] = bGuard ? "G" : "M";
}
if (!hlFav.Contains("G") && !hlFav.Contains("M")) hlFav.Add(bGuard ? "G" : "M");
App.WriteValue("Favorites", sFile, hlFav.Segments);
}
SetRecent(sFile);
} // SaveGuardFlag method

public void SetRecent(string sFile) {
if (!sFile.Contains(@"\")) return;
if (Util.Equiv(sFile, App.IniFile)) return;

DateTime dt = DateTime.Now;
string sTime = dt.ToString("u");
sTime = sTime.Substring(0, sTime.Length - 1);
int iIndex = this.Child.RTB.Index;
sTime += "|" + iIndex;
if (this.MdiChildren.Length == 0) sTime += "|N|W";
else sTime += "|" + (GetUserGuard(this.Child) ? "G" : "M") + "|" + Util.If(this.Child.RTB.WordWrap, "W", "U");
App.WriteValue("Recent", sFile, sTime);
string sDir = Path.GetDirectoryName(sFile);
if (Directory.Exists(sDir)) Directory.SetCurrentDirectory(sDir);
sFile = Path.Combine(App.DataDir, App.ReadData("Compiler", "Default") + ".ini");
Ini.WriteValue(sFile, "Data", "Directory", sDir);
} // SetRecent method

bool ApplyWrap(string sSection, string sFile) {
string sText = App.ReadValue(sSection, sFile, "");
if (sText.Length == 0) return false;

HomerRichTextBox rtb = this.Child.RTB;
sText = App.ReadOption("WordWrap", "Y");
sText = "-1|M|" + Util.If((sText == "N"), "U", "W");
sText = App.ReadValue(sSection, sFile, sText);
bool b = (bool) Util.If(sText.EndsWith("U"), false, true);
if (b && !rtb.WordWrap) {
AddMessage("Word wrap");
rtb.SetWrap(true);
}
else if (!b && rtb.WordWrap) {
AddMessage("Unwrap");
rtb.SetWrap(false);
}
return true;
} // ApplyWrap method

// The document's REAL guard (read-only) state, as the user set it.
//
// The Markdown preview turns the guard ON for as long as the preview is
// open (MarkdownReview_Enter) and restores the previous value on exit.
// Anything that PERSISTS the guard flag must therefore not read
// rtb.ReadOnly directly: bookmarking or favouriting a file while the
// preview was open stored "G", and the file then opened read-only for
// ever after, with no way to tell why. Reported as "files open guarded
// and I never guarded them", and it only showed on favourites because
// that is where the flag gets written. Ask this helper instead.
public bool GetUserGuard(MdiChild child) {
if (child == null) return false;
HomerRichTextBox rtbGuard = child.RTB;
if (rtbGuard == null) return false;
if (child.MarkdownReviewMode) return child.MarkdownReviewOldGuard;
return rtbGuard.ReadOnly;
} // GetUserGuard method

bool ApplyGuard(string sSection, string sFile) {
string sText = App.ReadValue(sSection, sFile, "");
if (sText.Length == 0) return false;

HomerRichTextBox rtb = this.Child.RTB;
sText = App.ReadOption("WordWrap", "Y");
sText = "-1|M|" + Util.If((sText == "N"), "U", "W");
sText = App.ReadValue(sSection, sFile, sText);
if (sText.IndexOf("G") >= 0) {
AddMessage("Guard");
rtb.SetGuard(true);
}
return true;
} // ApplyGuard method

public void ApplyFileOptions(string sFile) {
if (!ApplyGuard("Favorites", sFile)) ApplyGuard("Recent", sFile);
if (!ApplyWrap("Favorites", sFile)) {
ApplyWrap("Recent", sFile);
}

// SKOK NA ZAKLADKE czyta sekcje Bookmarks, a NIE Favorites (rozdzielenie z
// 5.0.64).  Wczesniej ten odczyt siedzial pod warunkiem ApplyWrap, wiec
// wznowienie pozycji dostawal TYLKO plik ulubiony - teraz zakladka dziala
// niezaleznie od tego, czy plik jest na liscie ulubionych.
HomerRichTextBox rtb = this.Child.RTB;
string sText = App.ReadValue("Bookmarks", sFile, "");
if (sText.Length == 0) return;
try {
string[] a = sText.Split('|');
sText = a[0];
rtb.Index = Int32.Parse(sText);
AddMessage("Bookmark at percent " + rtb.Percent);
}
catch {}
} // ApplyFavorite method

public string PickSpecialFolder() {
string sName = "";
string sPath = "";
StringBuilder sbNames = new StringBuilder();
StringBuilder sbPaths = new StringBuilder("\n");
object oShell = COM.CreateObject("Shell.Application");
for (int i = 0; i < 100; i++) {
try {
Object oDir = COM.CallMethod(oShell, "Namespace", new object[] {i});
Object oItem = COM.GetProperty(oDir, "Self");
sPath = (string) COM.GetProperty(oItem, "Path");
if (!Directory.Exists(sPath)) continue;
if (Util.IsNumeric(Path.GetFileName(sPath))) continue;
if (sbPaths.ToString().ToLower().Trim('\\').Contains("\n" + sPath.ToLower().Trim('\\') + "\n")) continue;
sbPaths.Append(sPath + "\n");
sName = (string) COM.GetProperty(oItem, "Name");
if (Util.Equiv(sName, "Temporary Internet Files")) sName = "Internet Cache";
else if (Util.Equiv(sName, "History")) sName = "Internet History";
else if (Util.Equiv(sName, "NetHood")) sName = "Network Neighborhood";
else if (Util.Equiv(sName, "PrintHood")) sName = "Printer Neighborhood";
else if ((@"\" + sPath.ToLower() + @"\").Contains(@"\all users\")) sName = "Common " + sName;
else if (!Util.Equiv(sName, "History") && (@"\" + sPath.ToLower() + @"\").Contains(@"\local settings\")) sName = "Local " + sName;
sbNames.Append(sName + "\n");
}
catch {
continue;
}
}

Environment.SpecialFolder folder;
for (int i = 0; i < 100; i++) {
sPath = "";
try {
folder = (Environment.SpecialFolder) i;
sPath = Environment.GetFolderPath(folder);
}
catch {
continue;
}
if (!Directory.Exists(sPath)) continue;
if (Util.IsNumeric(Path.GetFileName(sPath))) continue;
if (sbPaths.ToString().ToLower().Trim('\\').Contains("\n" + sPath.ToLower().Trim('\\') + "\n")) continue;
sbPaths.Append(sPath + "\n");
sName = folder.ToString();
sbNames.Append(sName + "\n");
}
sbNames.Append("Temp" + "\n");
sbPaths.Append(Util.GetTempFolder() + "\n");

string[] aNames = sbNames.ToString().Trim().Split('\n');
string[] aPaths = sbPaths.ToString().Trim().Split('\n');

string sDir = Dialog.Pick("Go to Special Folder", aPaths, aNames, true, 0);
return sDir;
} // PickSpecialFolder method

public void OpenOrActivateWindow(string sFile) {
int iConvert = 0;
OpenOrActivateWindow(sFile, iConvert);
} // OpenOrActivateWindow method

public void OpenOrActivateWindow(string sFile, int iConvert) {
string sLine = "";
string sColumn = "";
OpenOrActivateWindow(sFile, iConvert, sLine, sColumn);
} // OpenOrActivateWindow method

// OTWARCIE BOGATEGO DOKUMENTU BEZ KONWERSJI, jawnie i nie przez przypadek.
// Wartosci iConvert znaczyly do 5.0.65: 0 surowy tekst, 1 i 2 przez tabele
// Import, a -1 powstawalo WEWNATRZ ConvertFile2String jako skutek uboczny
// wyboru "rtf" na liscie formatow.  Zeby dac uzytkownikowi wybor "otworz jako
// rich text" wprost z listy, potrzebna byla wartosc, ktora omija konwersje i
// jest widoczna w kodzie pod nazwa, a nie liczba.
public const int iOpenRichText = -2;

public void OpenOrActivateWindow(string sFile, int iConvert, string sLine, string sColumn) {
OpenOrActivateWindow(sFile, iConvert, sLine, sColumn, "");
} // OpenOrActivateWindow method

public void OpenOrActivateWindow(string sFile, int iConvert, string sLine, string sColumn, string sForceImport) {
string sText;
sFile = Util.Unquote(sFile);
if (!File.Exists(sFile)) {
AddMessage("File not found!");
return;
}

sFile = Util.GetLfn(sFile);
// ApplyFileOptions(sFile);
// SetRecent(sFile);
object[] children = this.MdiChildren;
foreach (MdiChild child in children) {
if (Util.Equiv(child.File, sFile)) {
Util.Say("returning");
child.Activate();
SetCursorPosition(child.RTB, sLine, sColumn);
return;
}
}

string sTargetExt = "txt";
if (iConvert == 0 || iConvert == iOpenRichText) sText = "";
else {
// Dialog.Show("iConvert " + iConvert, "sTargetExt " + sTargetExt);
sText = COM.ConvertFile2String(sFile, ref iConvert, ref sTargetExt, false, sForceImport);
// Dialog.Show("iConvert " + iConvert, "sTargetExt " + sTargetExt);

if (iConvert >= 1 && sText.Trim().Length == 0) {
AddMessage("No text!");
return;
}
// Disable because also speaks after recent files
// else App.Frame.AddMessage("Done");
}

// Did so above
// SetRecent(sFile);
//if (!IsEmptyWindow()) new MdiChild(this);
//if (!IsEmptyWindow()) new MdiChild(this, "");
if (!IsEmptyWindow()) new MdiChild(this, sFile);
if (iConvert <= 0) {
this.Child.LoadTextOrRtfFile(sFile, (iConvert == 0 ? true : false));
//Dialog.Show(sFile);
ApplyFileOptions(sFile);

if (sFile == App.IniFile) return;
}
else {
string s = sText.Trim().ToLower();
// Ta sama zasada na drodze KONWERSJI: dokument jest rich textem tylko wtedy,
// gdy tresc naprawde nia jest.  Konwersja do .md albo .txt daje zwykly tekst,
// wiec bez tego przypisania plik zapisany potem pod nazwa .rtf poszedlby
// znowu przez zapis bogaty i tresc znowu by ucierpiala.
if (s.StartsWith(@"{\rtf") && s.EndsWith("}")) {
this.Child.RTB.Rtf = sText;
this.Child.IsRichTextDocument = true;
}
else {
this.Child.RTB.Text = sText;
this.Child.IsRichTextDocument = false;
}
this.Child.Text = Path.GetFileNameWithoutExtension(sFile) + "." + sTargetExt;
this.Child.File = this.Child.Text;
this.Child.RTB.Modified = false;

}
// Try disabling for auto bookmark
// SetRecent(sFile);

SetCursorPosition(this.Child.RTB, sLine, sColumn);
} // OpenOrActivateWindow method

public static bool SetCursorPosition(HomerRichTextBox rtb, string sLine, string sColumn) {
bool bReturn = false;
try {
if (sLine.Length > 0) rtb.Line = Int32.Parse(sLine);
if (sColumn.Length > 0) rtb.Column = Int32.Parse(sColumn);
bReturn = true;
}
catch {}
return bReturn;
} // SetCursorPosition method

public void NextWindow() {
object[] children = this.MdiChildren;
if (children.Length == 0) AddMessage("No windows!");
else if (children.Length == 1) AddMessage("Only this window!");
else {
MdiChild child = this.Child;
int iPosition = Array.IndexOf(children, child);
iPosition++;
if (iPosition == children.Length) iPosition = 0;
((MdiChild) children[iPosition]).Activate();
}
} // NextWindow method

public void PriorWindow() {
object[] children = this.MdiChildren;
if (children.Length == 0) AddMessage("No windows!");
else if (children.Length == 1) AddMessage("Only this window!");
else {
MdiChild child = this.Child;
int iPosition = Array.IndexOf(children, child);
iPosition--;
if (iPosition == -1) iPosition = children.Length - 1;
((MdiChild) children[iPosition]).Activate();
}
} // PriorWindow method

public void CloseAllButCurrentWindow() {
MdiChild child = this.Child;
if (child == null) return;

object[] children = this.MdiChildren;
int iCount = 0;
foreach (MdiChild o in children) {
if (o != child) {
o.Close();
iCount++;
}
}
} // CloseAllButCurrent method

// Announce arrival for the Navigate* family (chunk, sentence, paragraph,
// part) with EXACTLY ONE spoken result.
//
// The reader announces the line by itself when the caret moves after a
// navigation keystroke, while these commands ALSO spoke the range they
// moved over. Both start with the same first line, so the user heard the
// first line of the paragraph twice, in both directions -- reported
// verbatim as "when I navigate by paragraph it reads the first line of
// the paragraph twice, either way". Cancelling the reader first and then
// forcing our own message leaves one announcement (same pattern as the
// section-move and window-name announcements).
// Set for the duration of one Navigate* call when the chord is also a screen
// reader navigation command (Control with Up/Down = paragraph).  A field keeps
// the change additive: the existing two-argument Navigate* methods, and every
// caller of them, stay exactly as they were.
bool bNavigateReaderSpeaks = false;

// Set for the duration of one Navigate* call when the screen reader speaks the
// WHOLE destination by itself, so we must add nothing at all -- Alt with Up or
// Down (sentence).  Kept separate from bNavigateReaderSpeaks, which means "the
// reader spoke only up to the next hard line break, add the remainder".
//
// MEASURED 18.08.2026, because the difference is exactly what made the last
// three attempts wrong:
//  - NVDA maps kb:alt+upArrow and kb:alt+downArrow to caret_previousSentence
//    and caret_nextSentence (source/editableText.py), so the chord is the
//    reader's own;
//  - _caretMoveBySentenceHelper sends the key to us, then waits at most
//    config.conf["editableText"]["caretMoveTimeoutMs"] -- DEFAULT 100 ms
//    (config/configSpec.py) -- for the caret to move.  If it notices the move
//    it speaks UNIT_LINE, and if it does not it moves by UNIT_SENTENCE itself
//    and speaks the sentence.  Either way it SPEAKS;
//  - our own move takes about 124 ms, because the setter of
//    HomerRichTextBox.Index ends with DoEvents plus Thread.Sleep(100), so we
//    land on the edge of that 100 ms window and the user drifts between the two
//    branches.  That is what Kasperczak described (17.08.2026): "Tak, czyta 2
//    razy, ale tak jakby tez nie zawsze" -- the "not always" is the race, not
//    an inconsistent screen reader.
// Staying silent does NOT cost the feature: in the second branch the reader
// navigates sentences in this control by itself, measured on a live RICHEDIT50W
// control where Move(tomSentence) succeeds and Expand(tomSentence) returns the
// sentence.  Subtracting a "remainder" here, as we do for paragraphs, would
// bring the double reading straight back, because the reader's unit for this
// chord is a whole line or a whole sentence, not a first hard line.
bool bNavigateReaderSaysAll = false;

void AnnounceNavigateMessage(string sText) {
bool bReaderSpeaks = false;
AnnounceNavigateMessage(sText, bReaderSpeaks);
} // AnnounceNavigateMessage method

// Returns the part of sText that follows sRow, or an empty string when sRow
// already covers all of it.  Used to say only what the screen reader did NOT
// read, so the user hears the whole paragraph without hearing its first row
// twice.  Defensive: when sRow is not the start of sText (caret and reader out
// of step) the whole text is returned rather than a confusing fragment.
static string GetTextAfterRow(string sText, string sRow) {
if (sText == null) return "";
if (sRow == null) sRow = "";
string sAll = sText.Replace(HomerRichTextBox.CR, "");
string sFirst = sRow.Replace(HomerRichTextBox.CR, "").Trim();
if (sFirst.Length == 0) return sAll.Trim();
string sFlat = sAll.TrimStart();
if (sFlat.StartsWith(sFirst, StringComparison.Ordinal)) return sFlat.Substring(sFirst.Length).Trim();
return sAll.Trim();
} // GetTextAfterRow method

// What the screen reader ALREADY SAID after a paragraph move, so that we can
// say only the rest.  This must be the unit the READER asks for, not the one
// that happens to be convenient for us.
//
// MEASURED 17.08.2026 in the reader's own code and on a live control, because
// guessing this wrong is exactly what produced the last three reports:
//  - NVDA maps Control with Up/Down to script_caret_moveByParagraph, which
//    speaks textInfos.UNIT_PARAGRAPH (editableText.py in NVDA's library.zip);
//  - for a control with editAPIVersion 5 (RICHEDIT50W, which we now create)
//    that unit is tomParagraph of ITextDocument;
//  - asking a live RICHEDIT50W control gives, for one long wrapped paragraph,
//    the WHOLE paragraph -- while rtb.RowText gives only the first VISUAL row.
// So subtracting RowText left the tail of the paragraph to be spoken a SECOND
// time whenever word wrap was on, which is the default.  A paragraph made of
// several HARD lines behaves differently: there the reader says only the first
// hard line, and the rest genuinely has to be added by us.
//
// Both cases are covered by one rule: the reader said everything up to the
// next HARD line break, so that is what we subtract.
static string GetTextAfterReaderUnit(string sText) {
if (sText == null) return "";
string sAll = sText.Replace(HomerRichTextBox.CR, "").TrimStart();
int i = sAll.IndexOf('\n');
if (i < 0) return "";
return sAll.Substring(i + 1).Trim();
} // GetTextAfterReaderUnit method

// bReaderSpeaks: the chord that triggered this move is ALSO a screen-reader
// navigation command, so the reader speaks the destination by itself right
// after the keystroke.  We must not repeat what it read, or the user hears the
// same text twice -- reported for paragraph navigation (Control with Up/Down):
// "Akapity - tak samo, pierwszy wiersz podwojnie" (17.08.2026).  Cancelling
// the reader's speech first does NOT fix it, because the reader speaks after
// the keystroke has been handled, not before.  But staying completely silent is
// not right either: the reader reads only the ROW at the caret, so on a
// multi-row paragraph the user heard the first line and nothing else -- his
// follow-up report the same day.  So we say the REMAINDER only.  Chords the
// reader does NOT claim (Alt with arrows for chunk and sentence, Alt with
// PageUp and PageDown for part) keep speaking in full, otherwise they would be
// silent.
void AnnounceNavigateMessage(string sText, bool bReaderSpeaks) {
if (sText == null || sText.Trim().Length == 0) return;
// The reader says the WHOLE destination for this chord, so we add nothing to
// speech.  The text still goes to the status bar, which a screen reader reads
// only on request, so nothing is lost for a user who wants to check it.  See
// bNavigateReaderSaysAll for the measurement.
if (this.bNavigateReaderSaysAll) {
SetStatus(this.statusBar.Items[0].Text + "   " + sText);
return;
}
if (bReaderSpeaks) {
SetStatus(this.statusBar.Items[0].Text + "   " + sText);
// The reader announces its own paragraph unit, which stops at the next
// HARD line break -- reported by Kasperczak (17.08.2026): "kontrol
// strzalka w gore, w dol, kiedy sa akapity.  On czyta pierwszy wiersz
// tego akapitu tylko, a nie czyta juz dalej".  An EdSharp paragraph is a
// block between BLANK lines, so it can hold several hard lines and the
// user lost all but the first.  We therefore add only the remainder; see
// GetTextAfterReaderUnit for the measurement behind the chosen unit.
// When the paragraph is a single hard line the remainder is EMPTY and we
// stay silent, which keeps the earlier double-reading fix intact.
// Deferred with BeginInvoke so our words are queued after the keystroke
// has been handled, because the reader speaks after that point too.
string sRest = GetTextAfterReaderUnit(sText);
if (sRest.Length > 0) {
this.BeginInvoke((MethodInvoker) delegate {
try {Util.Say(sRest, true);} catch {}
});
}
return;
}
try {if (Win32.IsNVDAActive()) Win32.NVDACancelSpeech();} catch {}
AddMessage(sText, true);
} // AnnounceNavigateMessage method

public void NavigateNextMatch(string sMatch) {
bool bLine = false;
NavigateNextMatch(sMatch, bLine);
} // NavigateNextMatch method

// Move without saying anything, because the screen reader speaks the whole
// destination for this chord by itself; see bNavigateReaderSaysAll.
public void NavigateNextMatchReaderSaysAll(string sMatch) {
this.bNavigateReaderSaysAll = true;
try { NavigateNextMatch(sMatch, false); }
finally { this.bNavigateReaderSaysAll = false; }
} // NavigateNextMatchReaderSaysAll method

// Move without saying anything, because the screen reader speaks the whole
// destination for this chord by itself; see bNavigateReaderSaysAll.
public void NavigatePriorMatchReaderSaysAll(string sMatch) {
this.bNavigateReaderSaysAll = true;
try { NavigatePriorMatch(sMatch, false); }
finally { this.bNavigateReaderSaysAll = false; }
} // NavigatePriorMatchReaderSaysAll method

// Overload taking bReaderSpeaks; see AnnounceNavigateMessage for why a
// reader-claimed chord must not be announced by us as well.
public void NavigateNextMatch(string sMatch, bool bLine, bool bReaderSpeaks) {
this.bNavigateReaderSpeaks = bReaderSpeaks;
try { NavigateNextMatch(sMatch, bLine); }
finally { this.bNavigateReaderSpeaks = false; }
} // NavigateNextMatch method

public void NavigateNextMatch(string sMatch, bool bLine) {
int iIndex, iStart, iEnd, iForward;
string sValue, sText;
object[] aResults;
HomerRichTextBox rtb = this.Child.RTB;
if (bLine) iIndex = rtb.RowEnd + 1;
else iIndex = rtb.Index;
iStart = iIndex;
iEnd = rtb.TextLength;
if (iStart >= iEnd) aResults = new object[] {-1, ""};
else {
sText = rtb.GetRange(iStart, iEnd);
aResults = Util.RegExpContainsEquiv(sText, sMatch);
}
if ((int) aResults[0] == -1) {
this.AddMessage("Bottom!");
iStart = iEnd;
iIndex = iEnd;
}
else if (bLine) {
iIndex += (int) aResults[0];
iIndex += ((string) aResults[1]).Length;
}
else {
iForward = (int) aResults[0];
sValue = (string) aResults[1];
iIndex += iForward + sValue.Length;
iStart = iIndex;
sText = rtb.GetRange(iStart, iEnd);
aResults = Util.RegExpContainsEquiv(sText, sMatch);
if ((int) aResults[0] == -1) {
}
else {
iForward = (int) aResults[0];
sValue = (string) aResults[1];
iEnd = iStart + iForward + sValue.Length;
}
}

if (bLine) {
rtb.Index = iIndex;
rtb.Col = 0;
sText = rtb.RowText;
}
else {
sText = rtb.GetRange(iStart, iEnd);
rtb.Index = iIndex;
}
this.AnnounceNavigateMessage(sText, this.bNavigateReaderSpeaks);
} // NavigateNextMatch method

public void NavigatePriorMatch(string sMatch) {
bool bLine = false;
NavigatePriorMatch(sMatch, bLine);
} // NavigatePriorMatch method

// Overload taking bReaderSpeaks; see AnnounceNavigateMessage for why a
// reader-claimed chord must not be announced by us as well.
public void NavigatePriorMatch(string sMatch, bool bLine, bool bReaderSpeaks) {
this.bNavigateReaderSpeaks = bReaderSpeaks;
try { NavigatePriorMatch(sMatch, bLine); }
finally { this.bNavigateReaderSpeaks = false; }
} // NavigatePriorMatch method

public void NavigatePriorMatch(string sMatch, bool bLine) {
int iIndex, iStart, iEnd, iBackward;
string sValue, sText;
object[] aResults;
HomerRichTextBox rtb = this.Child.RTB;
if (bLine) iIndex = rtb.RowStart;
else iIndex = rtb.Index;
iStart = 0;
iEnd = iIndex;
sText = rtb.GetRange(iStart, iEnd);
aResults = Util.RegExpContainsLastEquiv(sText, sMatch);
if ((int) aResults[0] == -1) {
this.AddMessage("Top!");
iIndex = iStart;
iEnd = iStart;
}
else if (bLine) {
iIndex = (int) aResults[0];
iIndex += ((string) aResults[1]).Length;

if (iIndex == rtb.Index) {
iEnd = (int) aResults[0];
sText = rtb.GetRange(iStart, iEnd);
aResults = Util.RegExpContainsLastEquiv(sText, sMatch);
iIndex = (int) aResults[0];
if ((int) aResults[0] == -1) {
this.AddMessage("Top!");
iIndex = iStart;
iEnd = iStart;
}
else iIndex += ((string) aResults[1]).Length;
}
}
else {
iBackward = (int) aResults[0];
sValue = (string) aResults[1];
// Dialog.Show(sValue, iBackward);
iEnd = iBackward;
sText = rtb.GetRange(iStart, iEnd);
aResults = Util.RegExpContainsLastEquiv(sText, sMatch);
if ((int) aResults[0] == -1) {
iIndex = iStart;
}
else {
iBackward = (int) aResults[0];
sValue = (string) aResults[1];
// Dialog.Show(sValue, iBackward);
iStart = iBackward + sValue.Length;
iIndex = iStart;
}
}

if (bLine) {
rtb.Index = iIndex;
rtb.Col = 0;
sText = rtb.RowText;
}
else {
sText = rtb.GetRange(iStart, iEnd);
rtb.Index = iIndex;
}
this.AnnounceNavigateMessage(sText, this.bNavigateReaderSpeaks);
} // NavigatePriorMatch method

public void FileFind() {
string sContains, sFilter, sDir, sFile;
string[] aLabels, aValues, aFilters, aResults, aFiles, aNames;

sContains = App.ReadData("Contains", "");
sFilter = App.ReadData("Filter", "*.*");
string sTitle = "Open Folder";
sDir = Dialog.OpenFolder(sTitle, "Name", Directory.GetCurrentDirectory());
if (sDir.Length == 0) return;

Directory.SetCurrentDirectory(sDir);
aLabels = new string[] {"&Contains", "&Filter"};
aValues = new string[] {sContains, sFilter};
aResults = Dialog.MultiInput("Criteria", aLabels, aValues);
if (aResults.Length == 0) return;

sContains = aResults[0];
sFilter = aResults[1].Trim();
if (sFilter.Length == 0) sFilter = "*.*";
App.WriteData("Contains", sContains);
App.WriteData("Filter", sFilter);
aFilters = sFilter.Split('|');
sDir = Directory.GetCurrentDirectory();
aFiles = Util.FindInFiles(sContains, sDir, aFilters, false);
if (aFiles.Length == 0) {
Dialog.Show("Alert", "No matches!");
return;
}

aNames = new string[aFiles.Length];
for (int i = 0; i < aNames.Length; i++) aNames[i] = Path.GetFileName(aFiles[i]);
//Array.Sort(aNames, aFiles);
sFile = Dialog.Pick("Pick", aFiles, aNames, true, 0);
if (sFile.Length == 0) return;

OpenOrActivateWindow(sFile, 1);
/*
string[] aNames = null;
string[] aPaths = null;
int iIndex = -1;
string sPath = "";
string sName = "";
string sPaths = "";
string sNames = "";

string sDir = Directory.GetCurrentDirectory();
string sMatch = App.ReadData("FileFindMatch", "");
string sFilter = App.ReadData("FileFindFilter", "");
string sFields = "&Text\t&Filter";
string sValues = sMatch + "\t" + sFilter;
string[] aFields = sFields.Split('\t');
string[] aValues = sValues.Split('\t');
string[] aResults = Dialog.MultiInput("File Find", aFields, aValues);
if (aResults.Length == 0) return;

sMatch = aResults[0];
sFilter = aResults[1];
if (true) {
//if (sDir == App.sFileFindDir && sMatch == App.sFileFindMatch && sFilter == App.sFileFindFilter) {
AddMessage("Repeat search");
//aNames = App.aFileFind;
//iIndex = App.iFileFind + 1;
if (iIndex == -1) iIndex = 0;
}
else {
AddMessage("Please wait");
//ReadOnlyCollection<string> oPaths = null;
string[] aPaths = Util.GetFiles(sDir);
//if (sMatch == "") oPaths = LbcVB.GetFiles(sDir, sFilter);
//else oPaths = LbcVB.FindInFiles(sDir, sMatch, sFilter);
//if (oPaths.Count == 0) {
AddMessage("No files found!");
return;
}
//foreach (string s in oPaths) {
foreach (string s in aPaths) {
sPaths += s + "\n";
sName = s.Substring(sDir.Length + 1);
sNames += sName + "\n";
}
aPaths = sPaths.Trim().Split('\n');
aNames = sNames.Trim().Split('\n');
iIndex = 0;
}

App.WriteData("FileFindMatch", sMatch);
App.WriteData("FileFindFilter", sFilter);
sName = Dialog.Pick("Pick", aNames, true, iIndex);
if (sName.Length == 0) return;

int iName = Array.IndexOf(aNames, sName);
App.WriteData("FileFindDir", sDir);
App.aFileFind = aNames;
App.iFileFind = iName;
sPath = aPaths[iName];
sDir = Path.GetDirectoryName(sPath);
if (sDir.Length == 0) return;
if (Directory.Exists(sDir)) {
OpenOrActivateWindow(sFile, 1);
}
else AddMessage("Folder " + sDir + " not found!");
*/
} //FileFind method

public void CurrentWindows() {
object[] children = this.MdiChildren;
string sTitles = "";
foreach (MdiChild child in children) {
sTitles += child.Text + "\n";
}
string[] aTitles = sTitles.Trim().Split('\n');
string sTitle = Dialog.Pick("Current Windows", aTitles, true, 0);
if (sTitle.Length == 0) return;

int iTitle = Array.IndexOf(aTitles, sTitle);
((MdiChild) children[iTitle]).Activate();
} // CurrentWindows method

public void ExplorerFolder(string sDir) {
string sCommand = sDir;
try {
Process.Start(sCommand);
}
catch (Exception ex) {
Dialog.Show("Error", ex.Message);
return;
}
} // ExplorerFolder method

public void CommandPrompt(string sDir) {
string sCommand = Environment.GetEnvironmentVariable("COMSPEC");
sDir = Util.Quote(sDir);
string sParams = "/k cd " + sDir;
try {
Process.Start(sCommand, sParams);
}
catch (Exception ex) {
Dialog.Show("Error", ex.Message);
return;
}
} // CommandPrompt method

public void WebClientUtilities() {

bool bSort;
int iCount, iIndex;
string sCommand, sExe, sDir, sFile, sName, sValue, sBase, sTitle, sInputFile, sOutputFile, sCodeFile;

sDir = Path.Combine(App.ProgramDir, "WebClient");
string[] aFiles = Directory.GetFiles(sDir, "WebClient_*.py");
iCount = aFiles.Length;
HomerList hlNames = new HomerList();
HomerList hlValues = new HomerList();
for (int iFile = 0; iFile <iCount; iFile++) {
sFile = aFiles[iFile];
sName = Path.GetFileName(sFile);
sBase = Path.GetFileNameWithoutExtension(sName);
sBase = sBase.Substring("WebClient_".Length);
hlNames.Add(sBase);
sValue = Path.Combine(sDir, sName);
hlValues.Add(sValue);
} // for

sBase = App.ReadData("WebClientUtilities", "");
iIndex = -1;
if (sBase.Length > 0) {
iIndex = hlNames.IndexOf(sBase);
}
if (iIndex == -1) iIndex = 0;
sTitle = "Web Client Utilities";
bSort = false;
string[] aNames = hlNames.ToArray();
sName = Dialog.Pick(sTitle, aNames, bSort, iIndex);
if (sName.Length == 0) return;

App.WriteData("WebClientUtilities", sName);
iIndex = hlNames.IndexOf(sName);
sFile = hlValues[iIndex];
sExe = Path.Combine(sDir, "InPy.exe");
sExe = Win32.GetShortPath(sExe);
sInputFile = Path.Combine(App.DataDir, "WebClient.ini");
sBase = Path.GetFileNameWithoutExtension(sFile);
sOutputFile = Path.Combine(App.DataDir, sBase + ".txt");
sCodeFile = sFile;
sCommand = sExe + " " + Util.Quote(sCodeFile) + " " + Util.Quote(sInputFile) + " " + Util.Quote(sOutputFile);
if (File.Exists(sOutputFile)) File.Delete(sOutputFile);
Util.SetClipboardText(sCommand);
Util.RunWait(sCommand);
if (File.Exists(sOutputFile))  Process.Start(sOutputFile);
} // WebClientUtilities method

// BurnToCD i GetPathsFromDocument USUNIETE razem z komenda nagrywania plyt
// (30.08.2026, jego "ALT+SHIFT+B nagrywanie plikow to na pewno usun").
// GetPathsFromDocument schodzi RAZEM z nia, bo policzone: miala DOKLADNIE
// JEDNEGO wolajacego, wiec zostawiona bylaby martwym kodem, ktory nadal pyta
// uzytkownika o filtr rozszerzen.  To ta sama warstwa, ktora on sam zlapal przy
// nawigacji po czesciach ("a moze jest, ale nie w menu w klawiszach, a tylko w
// programie").
// NIE RUSZAM Util.GetExtensions ani Util.GetPathsWithExtensions: maja po cztery
// wystapienia, czyli innych klientow (miedzy innymi Path List pod
// Control+Shift+P i pobieranie z sieci).  Zdjecie ich razem z ta komenda dalo by
// zielony build i ciche zepsucie tamtych funkcji.
public void AlternateMenu() {
int iChoice = -1;
List<ToolStripMenuItem> items = new List<ToolStripMenuItem>();
string sItems = "";
StringBuilder sb = new StringBuilder();
foreach (ToolStripMenuItem menu in menuMain.Items) {
foreach (object o in menu.DropDownItems) {
ToolStripMenuItem item = o as ToolStripMenuItem;
if (item == null) continue;
if (item == menuHelpAlternateMenu) continue;
// string sText = item.Text.Replace("&", "") + "\t" + item.ShortcutKeyDisplayString;
// if ("1234567890".Contains(sText.Substring(0, 1))) continue;
if (item.IsMdiWindowListEntry) continue;
string[] aSummary = GetKeySummary(item);
string sText = aSummary[0] + " = " + aSummary[1] + ", " + aSummary[2];
sb.Append(sText + "\n");
items.Add(item);
}
}
sItems = sb.ToString();
string[] aItems = sItems.Trim().Split('\n');
string sItem = Dialog.Pick("Alternate Menu", aItems, true, 0);
if (sItem.Length == 0) return;

foreach (ToolStripMenuItem item in items) {
//if (sItem == item.Text.Replace("&", "")) {
// if (sItem == item.Text.Replace("&", "") + "\t" + item.ShortcutKeyDisplayString) {
string[] aSummary = GetKeySummary(item);
string sText = aSummary[0] + " = " + aSummary[1] + ", " + aSummary[2];
if (sItem == sText) {
iChoice = items.IndexOf(item);
break;
}
}
items[iChoice].PerformClick();
} // AlternateMenu method

// PALETA POLECEN (Control+Shift+P) - jego zlecenie 11.09.2026: "Paleta
// polecen.  Trzeba ja wprowadzic, jak w AMC.  Zaproponuj skrot klawiszowy",
// a nastepnie: "Paleta w AMC jezeli chodzi o filtrowanie i to co czyta NVDA,
// jest dobrze zrobiona.  Mozesz sie jakos tam wzorowac".
//
// Rozne od Alternate Menu (Control+Shift+M), ktore pokazuje CALE menu na
// raz i wymaga strzalkowania przez ~200 pozycji: tutaj sie PISZE, a lista
// sie zawęza.  Zapozyczone z AMC (CommandPaletteSearch.Filter i FoldForSearch):
//   - zapytanie dzielone na SLOWA, pozycja zostaje gdy zawiera WSZYSTKIE
//     (wiec "zap plik" znajduje "Save File As" po polsku i angielsku bez
//     pamietania kolejnosci),
//   - porownanie bez wielkosci liter i BEZ OGONKOW, bo szukanie ma dzialac,
//     gdy sie pisze "zapisz" albo "zaznacz" z klawiatury programisty.
// Filtr jest w polu tekstowym, nie w liscie: NVDA sam czyta wpisywane znaki,
// a strzalka w dol schodzi do wynikow.  Skrot Control+Shift+P byl wolny
// (sprawdzone w KeyMap i menu); NIE uzywam Control+Alt+litera, bo prawy Alt
// zjada polskie znaki.
public void CommandPalette() {
List<ToolStripMenuItem> items = new List<ToolStripMenuItem>();
List<string> lLabels = new List<string>();
foreach (ToolStripMenuItem menu in menuMain.Items) {
foreach (object o in menu.DropDownItems) {
ToolStripMenuItem item = o as ToolStripMenuItem;
if (item == null) continue;
if (item == menuHelpAlternateMenu) continue;
if (item == menuHelpCommandPalette) continue;   // nie wypisuj samej palety
if (item.IsMdiWindowListEntry) continue;
if (!item.Enabled) continue;
string[] aSummary = GetKeySummary(item);
string sKeys = (aSummary[1] == null) ? "" : aSummary[1].Trim();
string sLabel = menu.Text.Replace("&", "") + ": " + aSummary[0];
if (sKeys.Length > 0) sLabel += ", " + sKeys;
items.Add(item);
lLabels.Add(sLabel);
}
}
if (items.Count == 0) { Say.sayForced("No commands available"); return; }

int iChosen = Dialog.PickCommand("Command Palette", lLabels);
if (iChosen < 0 || iChosen >= items.Count) return;
items[iChosen].PerformClick();
} // CommandPalette method

new void ContextMenu(string sFile) {
MdiChild child = this.Child;

string[] aVerbs = COM.Verbs(sFile);
bool bFound = false;
foreach (string s in aVerbs) {
if (s.Contains("pen Wit")) bFound = true;
if (bFound) break;
} // foreach s
if (!bFound) {
Array.Resize(ref aVerbs, aVerbs.Length + 1);
aVerbs[aVerbs.Length - 1] = "Open With...";
}

string[] aNames = new string[aVerbs.Length];
for (int iVerb = 0; iVerb < aVerbs.Length; iVerb++) aNames[iVerb] = aVerbs[iVerb].Replace("&", "");

string sName = Dialog.Pick("Context Menu", aNames, true, 0);
if (sName.Length == 0) return;

int i = Array.IndexOf(aNames, sName);
string sVerb = aVerbs[i];

// Clipboard.SetText(sVerb);
// "Open With..." goes through ShellExecuteEx verb "openas" (Win32.OpenWith),
// which shows the modern "Open with" dialog including "Always use this app";
// the old Rundll32 OpenAs_RunDLL path does not offer that on Windows 10/11.
if (sVerb.Replace("&", "") == "Open With...") Win32.OpenWith(sFile, this.Handle);
else COM.InvokeVerb(sFile, sVerb);
} // ContextMenu method

public void SendToMenu(string sFile) {
MdiChild child = this.Child;
string sDir = Environment.GetFolderPath(Environment.SpecialFolder.SendTo);
string[]aLinks = Directory.GetFiles(sDir);
string sNameList = "";
foreach (string s in aLinks) sNameList += Path.GetFileNameWithoutExtension(s) + "\n";
string[]aNames = sNameList.Trim().Split('\n');
string sName = Dialog.Pick("SendTo Menu", aNames, true, 0);
if (sName.Length == 0) return;

int i = Array.IndexOf(aNames, sName);
string sLink = aLinks[i];

Process.Start(sLink, sFile);
} // SendToMenu method

public void ListBox_KeyUp(Object sender, KeyEventArgs e) {
ListBox lst = (ListBox) sender;
bool bChecked = false;
if (lst is CheckedListBox) bChecked = true;

if(e.KeyCode == Keys.Space && !e.Alt && !e.Control && e.Shift) {
if (bChecked) {
foreach (int i in ((CheckedListBox) sender).CheckedIndices) {
//Util.Say(lst.Items[i].ToString());
Util.Say(i);
}
e.Handled = true;
}
}
else if(e.KeyCode == Keys.J && ((e.Alt && !e.Control) || (!e.Alt && e.Control)) && !e.Shift) {
string sText = Dialog.Jump;
if (e.Control) {
sText = Dialog.Input("Jump", "Text", sText);
if (sText.Length == 0) return;
}

int iIndex = lst.SelectedIndex;
if (e.Alt || sText == Dialog.Jump) iIndex++;
else iIndex = 0;
Dialog.Jump = sText;

int iCount = lst.Items.Count;
//while (iIndex < iCount && lst.Items[iIndex].ToString().ToLower().IndexOf(sText) == -1) iIndex ++;
while (iIndex < iCount && lst.Items[iIndex].ToString().ToLower().IndexOf(sText) == -1) {
//Util.Say(iIndex);
iIndex ++;
}
if (iIndex < iCount) LbcDialog.selectOnly(lst, iIndex);
else AddMessage("Not found!");
//lst.Update();
e.Handled = true;
}
else e.Handled = false;
} // ListBox_KeyUp handler





// ---- Markdown rich-text copy, heading/list shortcuts, and shared Markdown parsing ----
private static readonly Regex MarkdownHeadingPrefixRegex = new Regex(@"^\s*#{1,6}\s+", RegexOptions.CultureInvariant);
private static readonly Regex MarkdownHeadingSuffixRegex = new Regex(@"\s+#+\s*$", RegexOptions.CultureInvariant);
private static readonly Regex MarkdownBulletPrefixRegex = new Regex(@"^(?<indent>\s*)(?<marker>[-*+])\s+", RegexOptions.CultureInvariant);
private static readonly Regex MarkdownNumberPrefixRegex = new Regex(@"^(?<indent>\s*)(?<number>\d+)[.)]\s+", RegexOptions.CultureInvariant);
private static readonly Regex MarkdownTableRowRegex = new Regex(@"^\s*\|.*\|\s*$", RegexOptions.CultureInvariant);
// Wiersz kreskek tabeli.  Z wiodaca kreska pionowa wystarcza JEDNA kolumna
// ("| --- |"), bez niej potrzebne sa co najmniej dwie - inaczej pozioma linia
// "---" stala by sie tabela.  Poprzednia wersja WYMAGALA dwoch kolumn zawsze,
// wiec tabela jednokolumnowa nie byla tabela ani dla podgladu, ani dla naszego
// eksportu HTML, ani dla listy elementow - a nasz wlasny konwerter 2htm robil z
// niej prawidlowa tabele.  Zmierzone: 2htm daje <table> z <th>, a
// MarkdownDocumentToHtml z tej samej binarki nie dawal nic.
private static readonly Regex MarkdownTableSeparatorRegex = new Regex(@"^\s*(?:\|\s*:?-{3,}:?\s*(?:\|\s*:?-{3,}:?\s*)*\|?|:?-{3,}:?\s*(?:\|\s*:?-{3,}:?\s*)+\|?)\s*$", RegexOptions.CultureInvariant);
private static readonly Regex MarkdownHorizontalRuleRegex = new Regex(@"^\s{0,3}([-*_])(\s*\1){2,}\s*$", RegexOptions.CultureInvariant);
private static readonly Regex HtmlTagRegex = new Regex(@"<[^>]+>", RegexOptions.CultureInvariant);
private static readonly Regex MarkdownInlineLinkRegex = new Regex(@"!?\[(?<text>[^\]]+)\]\s*\((?<url>[^)]+)\)", RegexOptions.CultureInvariant);
private static readonly Regex MarkdownAutoLinkRegex = new Regex(@"<https?://[^>]+>", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
private static readonly Regex MarkdownBareUrlRegex = new Regex(@"\bhttps?://[^\s<>]+", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
private static readonly Regex MarkdownWwwUrlRegex = new Regex(@"\bwww\.[^\s<>]+", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
// ADRESY E-MAIL JAKO ODSYLACZE (jego decyzja 03.09.2026 przy pytaniu o bramke
// tylko-Markdown: "Lista linkow w innych plikach tez spokojnie moglaby dzialac,
// parsujac linki, adresy stron mailowe itp.").  Goly adres pocztowy w zwyklym
// pliku jest odsylaczem dla czlowieka, wiec musi byc na liscie pod Control+F6.
// Wzorzec jest CELOWO waski: znak malpy, kropka w domenie i co najmniej dwie
// litery koncowki.  Szerszy lapal by numery wersji i sciezki - falszywy odsylacz
// jest gorszy niz jego brak, bo user szuka czegos, czego nie ma.
private static readonly Regex MarkdownMailAddressRegex = new Regex(@"\b[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}\b", RegexOptions.CultureInvariant);

// KOMENTARZE WEWNETRZNE (punkt 4 mapy drogowej).  Zlecenie Kasperczaka z
// 28.08.2026 00:24: "To na razie komentarze wewnetrzne, a pozniej komentarze
// dla Worda, ale to juz mocno pozniej."
//
// SKLADNIA WYBRANA POMIAREM, NIE UPODOBANIEM.  Markdown nie ma wlasnej
// skladni komentarza, wiec kandydatow bylo trzech.  Przepuscilem probke przez
// NASZE wlasne konwertery, te, ktore jada w jego paczce:
//   - komentarz HTML <!-- ... -->  : 2htm zostawia go jako komentarz HTML
//     (niewidoczny na stronie), pandoc do docx NIE wpuszcza jego tresci do
//     dokumentu, pandoc do tekstu tez ja zjada.  Czyli dokladnie to, o co
//     prosil: uwaga robocza, ktora nie wychodzi na zewnatrz.
//   - CriticMarkup {>> ... <<}     : ODPADA.  Zmierzone: i 2htm, i pandoc
//     zostawiaja go jako ZWYKLY TEKST z klamrami, wiec uwaga robocza
//     wyladowalaby w gotowym dokumencie u czytelnika.
//   - odsylacz [//]: # (...)       : znika z obu wyjsc, ale dziala tylko na
//     poczatku wiersza i nie da sie go wstawic w srodek zdania.
// Dlatego komentarz to <!-- ... -->.
//
// Wzorzec obejmuje TEZ komentarz wielowierszowy (Singleline sprawia, ze kropka
// lapie znak konca wiersza), bo uwaga robocza czesto ma kilka zdan.
private static readonly Regex MarkdownCommentRegex = new Regex(@"<!--(?<body>.*?)-->", RegexOptions.CultureInvariant | RegexOptions.Singleline);

				private enum MarkdownReviewKind {
			Heading,
			List,
	ListItem,
	Link,
	Table
	} // MarkdownReviewKind enum

		private sealed class MarkdownReviewInlineSpan {
		public int Start;
		public int End;
		public FontStyle Style;
		public MarkdownReviewInlineSpan(int iStart, int iEnd, FontStyle style) {
		Start = iStart;
		End = iEnd;
		Style = style;
		}
		}

			private void ApplyHeadingShortcut(MdiChild child, int iLevel) {
			if (child == null) return;
			HomerRichTextBox rtb = child.RTB;
			if (rtb == null) return;

			if (IsRichTextFile(child)) {
			Font fontBase = rtb.Font;
			if (fontBase == null) return;

			float fSize = fontBase.Size;
			switch (iLevel) {
			case 1: fSize += 6; break;
			case 2: fSize += 4; break;
			case 3: fSize += 2; break;
			case 4: fSize += 1; break;
			default: break;
			}

			Font fontHeading = null;
			try {
			fontHeading = new Font(fontBase.FontFamily, fSize, FontStyle.Bold);
			int iSelStart = rtb.SelectionStart;
			int iSelLength = rtb.SelectionLength;
			if (iSelLength == 0) {
			int iStart = rtb.RowStart;
			int iEnd = rtb.RowStart + rtb.RowText.Length;
			rtb.Select(iStart, iEnd - iStart);
			rtb.SelectionFont = fontHeading;
			rtb.Select(iSelStart, 0);
			}
			else {
			rtb.SelectionFont = fontHeading;
			}
			}
			catch {}
			finally {
			if (fontHeading != null) fontHeading.Dispose();
			}
			return;
			}

			// Markdown/plain text heading: prefix with hashes.
			int iStart2, iEnd2;
			GetSelectedLineSpan(rtb, out iStart2, out iEnd2);
			if (iEnd2 <= iStart2) return;

			string sText = rtb.GetRange(iStart2, iEnd2);
			string[] aLines = sText.Split('\n');
			for (int i = 0; i < aLines.Length; i++) {
			string sLine = aLines[i];
			bool bCR = sLine.EndsWith("\r");
			if (bCR) sLine = sLine.Substring(0, sLine.Length - 1);

			if (sLine.Trim().Length == 0) {
			aLines[i] = bCR ? "\r" : "";
			continue;
			}

			string sContent = sLine.TrimStart();
			sContent = MarkdownHeadingPrefixRegex.Replace(sContent, "");
			sContent = MarkdownHeadingSuffixRegex.Replace(sContent, "").Trim();
			aLines[i] = new string('#', iLevel) + " " + sContent + (bCR ? "\r" : "");
			}

			string sNewText = String.Join("\n", aLines);
			rtb.ReplaceRange(iStart2, iEnd2, sNewText);
			} // ApplyHeadingShortcut method

				private static string BuildHtmlClipboardFragment(string sFragment) {
				if (String.IsNullOrEmpty(sFragment)) return "";
				try {
				string sStart = "<html><body><!--StartFragment-->";
				string sEnd = "<!--EndFragment--></body></html>";
				string sTemplate = "Version:0.9\r\nStartHTML:{0:D10}\r\nEndHTML:{1:D10}\r\nStartFragment:{2:D10}\r\nEndFragment:{3:D10}\r\n";
				string sDummyHeader = String.Format(CultureInfo.InvariantCulture, sTemplate, 0, 0, 0, 0);
				int iStartHtml = Encoding.UTF8.GetByteCount(sDummyHeader);
				int iStartFragment = iStartHtml + Encoding.UTF8.GetByteCount(sStart);
				int iEndFragment = iStartFragment + Encoding.UTF8.GetByteCount(sFragment);
				int iEndHtml = iEndFragment + Encoding.UTF8.GetByteCount(sEnd);
				string sHeader = String.Format(CultureInfo.InvariantCulture, sTemplate, iStartHtml, iEndHtml, iStartFragment, iEndFragment);
				return sHeader + sStart + sFragment + sEnd;
				}
				catch {
				return "";
				}
				} // BuildHtmlClipboardFragment method

					private static string BuildRtfHeading(string sText, int iLevel) {
					if (String.IsNullOrEmpty(sText)) return "";
					try {
					using (RichTextBox box = new RichTextBox()) {
					box.Text = sText;
					box.SelectAll();
					float fSize = box.Font.Size;
					if (iLevel == 1) fSize += 6;
					else if (iLevel == 2) fSize += 4;
					else if (iLevel == 3) fSize += 2;
					box.SelectionFont = new Font(box.Font.FontFamily, fSize, FontStyle.Bold);
					return box.Rtf;
					}
					}
					catch {
					return "";
					}
					} // BuildRtfHeading method

			// WLASNY FORMAT SCHOWKA.  Nazwa idzie do systemu jako nazwa formatu, wiec
			// jest jawnie oznaczona nasza aplikacja - obce programy jej nie znaja i
			// ZIGNORUJA, dlatego kopiowanie do Worda dziala dokladnie jak dotad.
			// Niesie ORYGINALNA SKLADNIE MARKDOWN tego, co skopiowano, bo z postaci
			// zlozonej dla Worda (tekst = sam tytul, RTF = pole HYPERLINK) adresu
			// odzyskac nie da sie w sposob pewny.
			// UWAGA NA PRZYSZLOSC: sciezki kopiowania podaja tu SUROWA skladnie z
			// dokumentu, wiec kazda nowa sciezka Control+Shift+C musi ten format
			// wypelnic - inaczej wklejenie w EdSharpie znowu odda sam tekst.
			private const string EdSharpMarkdownFormat = "EdSharpNG.Markdown";

			// Wklejenie WLASNEGO formatu, jesli w schowku jest.  Zwraca true, gdy
			// wklejono - wtedy zwykla droga Control+V juz sie nie wykonuje.
			//
			// ROZDZIAL NA TRZY CZESCI JEST CELOWY, ZEBY DALO SIE TO ZMIERZYC:
			// odczyt schowka i sam zapis do kontrolki sa STATYCZNE, wiec sonda wola
			// je wprost na zbudowanej binarce.  Ta metoda tylko je skleja i MOWI,
			// bo komunikat wymaga instancji okna glownego, ktorej w sondzie nie ma.
			private bool PasteEdSharpMarkdownFormat(HomerRichTextBox rtb) {
			string sMarkdown;
			if (!TryGetEdSharpMarkdownFromClipboard(out sMarkdown)) return false;
			// W pliku RTF formatowanie jest PRAWDZIWE, wiec wstawianie tam skladni
			// Markdown byloby regresja - niech pracuje zwykle wklejenie bogatego
			// tekstu.
			if (IsRichTextFile(this.Child)) return false;
			// Bramka na dokument zabezpieczony: kontrolka sama odrzuca zapis przy
			// ReadOnly (zmierzone harnessem: tresc zostaje nietknieta), ale wtedy
			// MILCZY, a niewidomy nie ma jak odroznic tego od pustego schowka.
			if (rtb != null && rtb.ReadOnly) {
			AddMessage("Document is guarded!");
			return true;
			}
			if (!PasteMarkdownTextInto(rtb, sMarkdown)) return false;
			AddMessage("Markdown pasted");
			return true;
			} // PasteEdSharpMarkdownFormat method

			// Odczyt naszego formatu ze schowka.  Zwraca false takze wtedy, gdy
			// schowek trzyma inny proces (menedzer schowka) - wklejania nie
			// przerywamy, bo zwykla droga ma wlasne ponowienia.
			private static bool TryGetEdSharpMarkdownFromClipboard(out string sMarkdown) {
			sMarkdown = null;
			try {
			IDataObject data = Clipboard.GetDataObject();
			if (data == null) return false;
			if (!data.GetDataPresent(EdSharpMarkdownFormat)) return false;
			sMarkdown = data.GetData(EdSharpMarkdownFormat) as string;
			}
			catch (Exception) {
			return false;
			}
			return !String.IsNullOrEmpty(sMarkdown);
			} // TryGetEdSharpMarkdownFromClipboard method

			// Wstawienie tresci do kontrolki: zaznaczenie zostaje ZASTAPIONE, kursor
			// staje ZA wstawionym tekstem.
			private static bool PasteMarkdownTextInto(HomerRichTextBox rtb, string sMarkdown) {
			if (rtb == null || String.IsNullOrEmpty(sMarkdown)) return false;
			if (rtb.ReadOnly) return false;
			int iStart = rtb.SelectionStart;
			rtb.Select(iStart, rtb.SelectionLength);
			rtb.SelectedText = sMarkdown;
			try {rtb.Select(iStart + sMarkdown.Length, 0);} catch {}
			rtb.Modified = true;
			return true;
			} // PasteMarkdownTextInto method

			// Oryginalna skladnia ciagu pozycji listy, wprost z dokumentu.  Osobno od
			// GetMarkdownListItemTexts, ktore zwraca SAME TRESCI pozycji (bez
			// punktorow i numeracji) na potrzeby prawdziwej listy Worda.
			private static string GetMarkdownListSourceText(HomerRichTextBox rtb, int iFirst, int iLast) {
			if (rtb == null) return "";
			StringBuilder sb = new StringBuilder();
			for (int i = iFirst; i <= iLast; i++) {
			if (i > iFirst) sb.Append("\n");
			sb.Append(rtb.GetRowText(i).TrimEnd('\r', '\n'));
			}
			return sb.ToString();
			} // GetMarkdownListSourceText method

				private static string BuildRtfHyperlink(string sTitle, string sUrl) {
			return @"{\rtf1\ansi\uc1{\fonttbl{\f0 Arial;}}{\field{\*\fldinst{" +
			RtfEncode("HYPERLINK \"" + sUrl + "\"") +
			@"}}{\fldrslt{" + RtfEncode(sTitle) + @"}}}}";
			} // BuildRtfHyperlink method

				private static string BuildRtfListParagraph(List<string> items, bool bOrdered) {
				if (items == null || items.Count == 0) return "";
				try {
				string sTab = @"\tab";
				string sBullet = @"\'b7";
				StringBuilder sb = new StringBuilder();
				// RTF header + font table. \uc1 so non-RTF readers skip one fallback char.
				// Symbol uses \fcharset2 so the bullet glyph renders reliably.
				sb.Append(@"{\rtf1\ansi\ansicpg1250\deff0\uc1{\fonttbl{\f0\fnil Calibri;}{\f1\fnil\fcharset2 Symbol;}}");
				// Stylesheet defining "List Paragraph" as \s1 so Word applies that
				// named paragraph style on paste (matches Word's built-in style).
				sb.Append(@"{\stylesheet{\s1\fi-360\li720\sa0\jclisttab\tx720 List Paragraph;}}");
				// List table: one list definition (list id 1).
				if (bOrdered) {
				sb.Append(@"{\*\listtable{\list\listtemplateid1\listsimple");
				sb.Append(@"{\listlevel\levelnfc0\leveljc0\levelfollow0\levelstartat1{\leveltext\leveltemplateid1\'02\'00.;}{\levelnumbers\'01;}\fi-360\li720 }");
				sb.Append(@"\listid1}}");
				}
				else {
				sb.Append(@"{\*\listtable{\list\listtemplateid1\listsimple");
				sb.Append(@"{\listlevel\levelnfc23\leveljc0\levelfollow0\levelstartat1{\leveltext\leveltemplateid1\'01" + sBullet + @";}{\levelnumbers;}\f1\fi-360\li720 }");
				sb.Append(@"\listid1}}");
				}
				// List override table maps list id 1 to list style number \ls1.
				sb.Append(@"{\*\listoverridetable{\listoverride\listid1\listoverridecount0\ls1}}");
				// Body paragraphs: \s1 = List Paragraph style, \ls1\ilvl0 = list binding.
				for (int n = 0; n < items.Count; n++) {
				string sItem = RtfEncodeInline(items[n]);
				sb.Append(@"\pard\plain\s1\ls1\ilvl0\fi-360\li720\sa0\jclisttab\tx720 ");
				if (bOrdered) {
				sb.Append(@"{\listtext\f0 ");
				sb.Append((n + 1).ToString(CultureInfo.InvariantCulture));
				sb.Append("." + sTab + "}");
				}
				else {
				sb.Append(@"{\listtext\f1 " + sBullet + sTab + "}");
				}
				sb.Append(@"\f0 ");
				sb.Append(sItem);
				sb.Append(@"\par");
				sb.Append("\r\n");
				}
				sb.Append("}");
				return sb.ToString();
				}
				catch {
				return "";
				}
				} // BuildRtfListParagraph method

					private static string BuildRtfPlainText(string sText) {
					if (String.IsNullOrEmpty(sText)) return "";
					try {
					using (RichTextBox box = new RichTextBox()) {
					box.Text = sText;
					return box.Rtf;
					}
					}
					catch {
					return "";
					}
					} // BuildRtfPlainText method

				private bool CopyMarkdownInlineLinkSpanAsRichText(MdiChild child, int[] span) {
				if (child == null || child.RTB == null) return false;
				if (span == null || span.Length < 6) return false;

				string sSource = child.RTB.Text ?? "";
				string sTitle = "";
				string sUrlRaw = "";
				try {sTitle = sSource.Substring(span[2], span[3] - span[2]);} catch {sTitle = "";}
				try {sUrlRaw = sSource.Substring(span[4], span[5] - span[4]);} catch {sUrlRaw = "";}

			sTitle = MarkdownReview_CollapseWhitespace(sTitle);
			string sUrl = MarkdownReview_NormalizeUrl(sUrlRaw);
			if (sUrl.Length == 0) sUrl = MarkdownReview_CleanUrl(sUrlRaw);
			if (sUrl.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) sUrl = "https://" + sUrl;
			if (sUrl.Length == 0) return false;
			if (sTitle.Length == 0) sTitle = sUrl;

			DataObject data = new DataObject();
			data.SetData(DataFormats.UnicodeText, sTitle);
			data.SetData(DataFormats.Text, sTitle);
			data.SetData(DataFormats.Rtf, BuildRtfHyperlink(sTitle, sUrl));
			// Wlasny format: wklejenie WEWNATRZ EdSharpa ma odtworzyc skladnie
			// Markdown, a nie sam tytul (zgloszenie Kasperczaka 01.09).
			data.SetData(EdSharpMarkdownFormat, "[" + sTitle + "](" + sUrl + ")");
			if (!Util.SetClipboardData(data)) {AddMessage("Clipboard is busy, link not copied!"); return false;}
				AddMessage("Link copied");
				return true;
				} // CopyMarkdownInlineLinkSpanAsRichText method

			private static int GetLeadingWhitespaceLength(string s) {
			if (String.IsNullOrEmpty(s)) return 0;
			int i = 0;
			while (i < s.Length && (s[i] == ' ' || s[i] == '\t')) i++;
			return i;
			} // GetLeadingWhitespaceLength method

				private static List<string> GetMarkdownListItemTexts(HomerRichTextBox rtb, int iFirst, int iLast) {
				List<string> items = new List<string>();
				if (rtb == null) return items;
				for (int i = iFirst; i <= iLast; i++) {
				string sItem;
				if (!MarkdownReview_TryGetListItemText(rtb.GetRowText(i), out sItem)) continue;
				sItem = sItem.Trim();
				if (sItem.Length == 0) sItem = " ";
				items.Add(sItem);
				}
				return items;
				} // GetMarkdownListItemTexts method

			private static void GetSelectedLineSpan(HomerRichTextBox rtb, out int iStart, out int iEnd) {
			iStart = 0;
			iEnd = 0;
			if (rtb == null) return;

			int iSelStart = rtb.SelectionStart;
			int iSelLength = rtb.SelectionLength;
			if (iSelLength == 0) {
			iStart = rtb.RowStart;
			iEnd = rtb.RowStart + rtb.RowText.Length;
			return;
			}

			int iSelEnd = iSelStart + iSelLength;
			int iEndIndex = (iSelEnd > iSelStart) ? iSelEnd - 1 : iSelStart;
			int iStartRow = rtb.GetLineFromCharIndex(iSelStart);
			int iEndRow = rtb.GetLineFromCharIndex(iEndIndex);
			if (iStartRow < 0) iStartRow = 0;
			if (iEndRow < 0) iEndRow = iStartRow;

			iStart = rtb.GetFirstCharIndexFromLine(iStartRow);
			int iNext = rtb.GetFirstCharIndexFromLine(iEndRow + 1);
			iEnd = (iNext < 0) ? rtb.TextLength : iNext;
			} // GetSelectedLineSpan method

			private bool HandleEditorFormattingKey(Keys keyData) {
// Yield to any command 5.0 already binds to this chord (e.g. Control+Shift+D6
// Prior Baseline): never shadow an existing hotkey.  The Enter list-continuation
// path below is exempt because Enter is not a registered command chord.
if (keyData != Keys.Enter && hashKey.ContainsKey(keyData)) return false;
			MdiChild child = this.Child;
			if (child == null) return false;

			HomerRichTextBox rtb = child.RTB;
			if (rtb == null) return false;

			// List behavior: Enter continues list; Enter on an empty list item ends it.
			if (keyData == Keys.Enter && !child.MarkdownReviewMode) {
			if (IsRichTextFile(child) && HandleRichTextNumberedListEnter(rtb)) return true;
			if (IsMarkdownOrDefault(child) && HandleMarkdownListEnter(rtb)) return true;
			}

			if ((keyData & (Keys.Control | Keys.Shift | Keys.Alt)) != (Keys.Control | Keys.Shift)) return false;

			Keys keyCode = keyData & Keys.KeyCode;
			int iDigit = -1;
			if (keyCode >= Keys.D0 && keyCode <= Keys.D9) iDigit = (int) keyCode - (int) Keys.D0;
			else if (keyCode >= Keys.NumPad0 && keyCode <= Keys.NumPad9) iDigit = (int) keyCode - (int) Keys.NumPad0;
			else return false;

			if (iDigit >= 1 && iDigit <= 6) {
			if (this.KeyDescriber) {
			AddMessage("Heading " + iDigit);
			return true;
			}
			ApplyHeadingShortcut(child, iDigit);
			AddMessage("Heading " + iDigit);
			return true;
			}

			// LISTY ZESZLY Z CYFR NA LITERE L (Kasperczak, 31.08.2026, ustalenie
			// edsharpng-57: "CTRL-Shift-7 i 8 staja sie wolne").  Przelaczanie listy
			// punktowanej i numerowanej ma teraz Control+L i Control+Shift+L, czyli
			// pozycje menu z handlerem, a nie ten router cyfr.  Metody
			// ToggleBulletListShortcut i ToggleNumberedListShortcut ZOSTAJA - to one
			// wykonuja prace, tylko wolane sa z handlera menu.  Cyfry 1-6
			// (poziomy naglowka), 9 (szkielet linku) i 0 (czyszczenie formatowania)
			// zostaja nietkniete: o nich nie rozstrzygal.
			if (iDigit == 7 || iDigit == 8) return false;

			if (iDigit == 9) {
			if (this.KeyDescriber) {
			AddMessage("Insert Markdown link");
			return true;
			}
			InsertMarkdownLinkShortcut(child);
			AddMessage("Insert Markdown link");
			return true;
			}

			if (iDigit == 0) {
			if (this.KeyDescriber) {
			AddMessage("Clear formatting");
			return true;
			}
			ClearFormattingShortcut(child);
			AddMessage("Clear formatting");
			return true;
			}

			return false;
			} // HandleEditorFormattingKey method

			private static bool HandleMarkdownListEnter(HomerRichTextBox rtb) {
			if (rtb == null) return false;
			if (rtb.SelectionLength != 0) return false;

			string sLine = rtb.RowText;
			bool bCR = sLine.EndsWith("\r");
			if (bCR) sLine = sLine.Substring(0, sLine.Length - 1);

			Match mBullet = MarkdownBulletPrefixRegex.Match(sLine);
			Match mNumber = MarkdownNumberPrefixRegex.Match(sLine);
			Match m = mBullet.Success ? mBullet : mNumber;
			if (!m.Success) return false;

			int iRowStart = rtb.RowStart;
			int iContentStart = m.Index + m.Length;
			string sContent = (iContentStart <= sLine.Length) ? sLine.Substring(iContentStart) : "";

			// End list if the current item is empty (double Enter behavior).
			if (sContent.Trim().Length == 0) {
			int iRemoveStart = iRowStart + m.Groups["indent"].Value.Length;
			int iRemoveEnd = iRowStart + iContentStart;
			if (iRemoveEnd > iRemoveStart) rtb.ReplaceRange(iRemoveStart, iRemoveEnd, "");
			rtb.Index = iRemoveStart;
			return true;
			}

			// Continue list on Enter.
			string sIndent = m.Groups["indent"].Value;
			string sInsert;
			if (mBullet.Success) {
			string sMarker = mBullet.Groups["marker"].Value;
			sInsert = "\n" + sIndent + sMarker + " ";
			}
			else {
			int iNum = 1;
			try {iNum = Int32.Parse(mNumber.Groups["number"].Value) + 1;} catch {iNum = 1;}
			sInsert = "\n" + sIndent + iNum.ToString() + ". ";
			}

			int iIndex = rtb.Index;
			rtb.ReplaceRange(iIndex, iIndex, sInsert);
			return true;
			} // HandleMarkdownListEnter method

			private static bool HandleRichTextNumberedListEnter(HomerRichTextBox rtb) {
			if (rtb == null) return false;
			if (rtb.SelectionLength != 0) return false;

			string sLine = rtb.RowText;
			bool bCR = sLine.EndsWith("\r");
			if (bCR) sLine = sLine.Substring(0, sLine.Length - 1);

			Match mNumber = MarkdownNumberPrefixRegex.Match(sLine);
			if (!mNumber.Success) return false;

			int iRowStart = rtb.RowStart;
			int iContentStart = mNumber.Index + mNumber.Length;
			string sContent = (iContentStart <= sLine.Length) ? sLine.Substring(iContentStart) : "";

			// End list if the current item is empty (double Enter behavior).
			if (sContent.Trim().Length == 0) {
			int iRemoveStart = iRowStart + mNumber.Groups["indent"].Value.Length;
			int iRemoveEnd = iRowStart + iContentStart;
			if (iRemoveEnd > iRemoveStart) rtb.ReplaceRange(iRemoveStart, iRemoveEnd, "");
			rtb.Index = iRemoveStart;
			return true;
			}

			// Continue list on Enter.
			string sIndent = mNumber.Groups["indent"].Value;
			int iNum = 1;
			try {iNum = Int32.Parse(mNumber.Groups["number"].Value) + 1;} catch {iNum = 1;}
			string sInsert = "\n" + sIndent + iNum.ToString() + ". ";
			int iIndex = rtb.Index;
			rtb.ReplaceRange(iIndex, iIndex, sInsert);
			return true;
			} // HandleRichTextNumberedListEnter method

			private void InsertMarkdownLinkShortcut(MdiChild child) {
			if (child == null) return;
			HomerRichTextBox rtb = child.RTB;
			if (rtb == null) return;

			int iSelStart = rtb.SelectionStart;
			int iSelLength = rtb.SelectionLength;
			string sSelected = "";
			if (iSelLength > 0) sSelected = rtb.SelectedText.Trim();

			string sInsert;
			int iCursor;
			if (sSelected.Length == 0) {
			sInsert = "[]()";
			iCursor = iSelStart + 1;
			}
			else if (IsLikelyMarkdownUrl(sSelected)) {
			sInsert = "[](" + sSelected + ")";
			iCursor = iSelStart + 1;
			}
			else {
			sInsert = "[" + sSelected + "]()";
			iCursor = iSelStart + sInsert.Length - 1;
			}

			rtb.Select(iSelStart, iSelLength);
			rtb.SelectedText = sInsert;
			try {rtb.Select(iCursor, 0);} catch {}
			rtb.Modified = true;
			} // InsertMarkdownLinkShortcut method

			private static bool IsExactShortcut(Keys keyData, Keys keyCode, Keys modifiers) {
			Keys actualKey = keyData & Keys.KeyCode;
			Keys actualModifiers = keyData & (Keys.Control | Keys.Shift | Keys.Alt);
			return actualKey == keyCode && actualModifiers == modifiers;
			} // IsExactShortcut method

			private static bool IsLikelyMarkdownUrl(string sText) {
			if (String.IsNullOrEmpty(sText)) return false;
			string s = sText.Trim();
			return s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
			s.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
			s.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
			s.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
			s.StartsWith("www.", StringComparison.OrdinalIgnoreCase);
			} // IsLikelyMarkdownUrl method

			private static bool IsMarkdownOrDefault(MdiChild child) {
			if (child == null) return false;
			if (MarkdownReview_IsMarkdownFile(child.File)) return true;
			try {
			string sExt = Path.GetExtension(child.File);
			if (String.IsNullOrEmpty(sExt)) {
			string sDefault = App.ReadOption("ExtensionDefault", "md").Trim();
			if (Util.Equiv(sDefault, "md")) return true;
			}
			}
			catch {}
			return false;
			} // IsMarkdownOrDefault method

			private static bool IsRichTextFile(MdiChild child) {
			if (child == null) return false;
			try {
			return String.Equals(Path.GetExtension(child.File), ".rtf", StringComparison.OrdinalIgnoreCase);
			}
			catch {
			return false;
			}
			} // IsRichTextFile method

					private static string MarkdownInlineTextToHtml(string sText) {
					if (String.IsNullOrEmpty(sText)) return "";
					string s = System.Net.WebUtility.HtmlEncode(sText);
					try {
					s = Regex.Replace(s, @"`([^`]*)`", "<code>$1</code>");
					for (int i = 0; i < 3; i++) {
					s = Regex.Replace(s, @"(\*\*|__)(.+?)\1", "<strong>$2</strong>");
					s = Regex.Replace(s, @"(\*|_)(.+?)\1", "<em>$2</em>");
					s = Regex.Replace(s, @"~~(.+?)~~", "<del>$1</del>");
					}
					}
					catch {}
					return s;
					} // MarkdownInlineTextToHtml method

				private static string MarkdownInlineToHtml(string sText) {
				if (String.IsNullOrEmpty(sText)) return "";
				StringBuilder sb = new StringBuilder();
				int iPos = 0;
				try {
				foreach (Match m in MarkdownInlineLinkRegex.Matches(sText)) {
				if (m == null || !m.Success || m.Index < iPos) continue;
					sb.Append(MarkdownInlineTextToHtml(sText.Substring(iPos, m.Index - iPos)));
					string sTitle = StripMarkdownFormatting(m.Groups["text"].Value);
				string sUrl = MarkdownReview_NormalizeUrl(m.Groups["url"].Value);
				if (sUrl.Length > 0) {
				sb.Append("<a href=\"");
				sb.Append(System.Net.WebUtility.HtmlEncode(sUrl));
				sb.Append("\">");
				sb.Append(System.Net.WebUtility.HtmlEncode(sTitle));
				sb.Append("</a>");
				}
				else sb.Append(System.Net.WebUtility.HtmlEncode(sTitle));
				iPos = m.Index + m.Length;
				}
				}
				catch {}
					if (iPos < sText.Length) sb.Append(MarkdownInlineTextToHtml(sText.Substring(iPos)));
					return sb.ToString();
					} // MarkdownInlineToHtml method

		// ---- Markdown document to standalone HTML (for Preview Markdown in Web Browser) ----
		// Built on the block regexes already used by Markdown Review and the
		// heading shortcuts, plus MarkdownInlineToHtml for the inline layer, so
		// the preview agrees with what Review and Document Navigation report.
		// Deliberately NOT a new library: upstream 5.0.16 solved the same
		// problem with Markdig from NuGet, but this fork builds with bare
		// csc.exe from BuildEdSharp.cmd (no MSBuild, no NuGet, no restore), so
		// a package reference would break the build for a preview feature.
		// The point of the browser view is the screen reader's virtual buffer:
		// headings, links, lists and tables must land in the HTML as real
		// elements, which is what this renderer guarantees.

		private static void MarkdownHtml_CloseList(StringBuilder sb, ref string sOpenList) {
			if (sOpenList.Length == 0) return;
			sb.Append("</" + sOpenList + ">\r\n");
			sOpenList = "";
		} // MarkdownHtml_CloseList method

		private static void MarkdownHtml_AppendTable(StringBuilder sb, List<string> rows) {
			// A GitHub-style pipe table. The row after the header is the
			// separator (dashes and optional colons); it is layout, not data, so
			// it never becomes a table row. Cells become th in the header row and
			// td below it, which is what lets a screen reader announce column
			// headers while moving through the table.
			if (rows == null || rows.Count == 0) return;
			bool bHasHeader = rows.Count > 1 && MarkdownTableSeparatorRegex.IsMatch(rows[1]);
			sb.Append("<table>\r\n");
			for (int i = 0; i < rows.Count; i++) {
				if (bHasHeader && i == 1) continue;
				string sRow = rows[i].Trim();
				if (sRow.StartsWith("|")) sRow = sRow.Substring(1);
				if (sRow.EndsWith("|")) sRow = sRow.Substring(0, sRow.Length - 1);
				string sCellTag = (bHasHeader && i == 0) ? "th" : "td";
				sb.Append("<tr>");
				string[] aCells = sRow.Split('|');
				for (int j = 0; j < aCells.Length; j++) {
					sb.Append("<" + sCellTag + ">");
					sb.Append(MarkdownInlineToHtml(aCells[j].Trim()));
					sb.Append("</" + sCellTag + ">");
				}
				sb.Append("</tr>\r\n");
			}
			sb.Append("</table>\r\n");
		} // MarkdownHtml_AppendTable method

		private static void MarkdownHtml_FlushParagraph(StringBuilder sb, List<string> paragraph) {
			if (paragraph.Count == 0) return;
			sb.Append("<p>" + MarkdownInlineToHtml(String.Join(" ", paragraph.ToArray())) + "</p>\r\n");
			paragraph.Clear();
		} // MarkdownHtml_FlushParagraph method

		public static string MarkdownDocumentToHtml(string sSource, string sTitle) {
			// Convert a whole Markdown document to a standalone HTML document.
			// Never throws: a failed preview must not take the editor down, so on
			// any error the caller gets "" and shows a message instead.
			try {
			if (sSource == null) sSource = "";
			StringBuilder sb = new StringBuilder();
			sb.Append("<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n");
			sb.Append("<meta charset=\"utf-8\" />\r\n");
			sb.Append("<title>" + System.Net.WebUtility.HtmlEncode(sTitle == null ? "" : sTitle) + "</title>\r\n");
			sb.Append("</head>\r\n<body>\r\n");

			string[] aLines = sSource.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
			string sOpenList = "";
			bool bInFence = false;
			List<string> tableRows = new List<string>();
			List<string> paragraph = new List<string>();

			for (int i = 0; i < aLines.Length; i++) {
				string sLine = aLines[i];
				string sTrim = sLine.Trim();

				// A fenced code block is verbatim: nothing inside it is parsed as
				// Markdown, which is why the fence state is tested first.
				bool bFenceLine = sTrim.StartsWith("```") || sTrim.StartsWith("~~~");
				if (bFenceLine) {
					if (!bInFence) {
						MarkdownHtml_FlushParagraph(sb, paragraph);
						if (tableRows.Count > 0) { MarkdownHtml_AppendTable(sb, tableRows); tableRows.Clear(); }
						MarkdownHtml_CloseList(sb, ref sOpenList);
						sb.Append("<pre><code>");
						bInFence = true;
					}
					else {
						sb.Append("</code></pre>\r\n");
						bInFence = false;
					}
					continue;
				}
				if (bInFence) {
					sb.Append(System.Net.WebUtility.HtmlEncode(sLine));
					sb.Append("\r\n");
					continue;
				}

				if (MarkdownTableRowRegex.IsMatch(sLine)) {
					MarkdownHtml_FlushParagraph(sb, paragraph);
					MarkdownHtml_CloseList(sb, ref sOpenList);
					tableRows.Add(sLine);
					continue;
				}
				if (tableRows.Count > 0) { MarkdownHtml_AppendTable(sb, tableRows); tableRows.Clear(); }

				if (sTrim.Length == 0) {
					MarkdownHtml_FlushParagraph(sb, paragraph);
					MarkdownHtml_CloseList(sb, ref sOpenList);
					continue;
				}

				int iLevel;
				string sHeadingTitle;
				if (TryGetMarkdownSectionHeadingLine(sLine, out iLevel, out sHeadingTitle)) {
					MarkdownHtml_FlushParagraph(sb, paragraph);
					MarkdownHtml_CloseList(sb, ref sOpenList);
					if (iLevel < 1) iLevel = 1;
					if (iLevel > 6) iLevel = 6;
					string sTag = "h" + iLevel.ToString(CultureInfo.InvariantCulture);
					sb.Append("<" + sTag + ">" + MarkdownInlineToHtml(sHeadingTitle) + "</" + sTag + ">\r\n");
					continue;
				}

				if (MarkdownHorizontalRuleRegex.IsMatch(sLine)) {
					MarkdownHtml_FlushParagraph(sb, paragraph);
					MarkdownHtml_CloseList(sb, ref sOpenList);
					sb.Append("<hr />\r\n");
					continue;
				}

				Match mBullet = MarkdownBulletPrefixRegex.Match(sLine);
				Match mNumber = MarkdownNumberPrefixRegex.Match(sLine);
				if (mBullet.Success || mNumber.Success) {
					MarkdownHtml_FlushParagraph(sb, paragraph);
					string sWanted = mBullet.Success ? "ul" : "ol";
					if (sOpenList != sWanted) {
						MarkdownHtml_CloseList(sb, ref sOpenList);
						sb.Append("<" + sWanted + ">\r\n");
						sOpenList = sWanted;
					}
					Match mUsed = mBullet.Success ? mBullet : mNumber;
					string sItem = sLine.Substring(mUsed.Index + mUsed.Length);
					sb.Append("<li>" + MarkdownInlineToHtml(sItem.Trim()) + "</li>\r\n");
					continue;
				}

				// A plain line while a list is open continues that list rather than
				// opening a paragraph inside ul or ol, which would be invalid HTML.
				if (sOpenList.Length > 0) {
					sb.Append("<li>" + MarkdownInlineToHtml(sTrim) + "</li>\r\n");
					continue;
				}

				if (sTrim.StartsWith(">")) {
					MarkdownHtml_FlushParagraph(sb, paragraph);
					sb.Append("<blockquote><p>" + MarkdownInlineToHtml(sTrim.TrimStart('>').Trim()) + "</p></blockquote>\r\n");
					continue;
				}

				paragraph.Add(sTrim);
			}

			MarkdownHtml_FlushParagraph(sb, paragraph);
			if (tableRows.Count > 0) MarkdownHtml_AppendTable(sb, tableRows);
			MarkdownHtml_CloseList(sb, ref sOpenList);
			if (bInFence) sb.Append("</code></pre>\r\n");

			sb.Append("</body>\r\n</html>\r\n");
			return sb.ToString();
			}
			catch (Exception) {
			return "";
			}
		} // MarkdownDocumentToHtml method


					private static bool MarkdownLineHasRichInlineMarkup(string sLine) {
					if (String.IsNullOrEmpty(sLine)) return false;
					if (sLine.IndexOf("**", StringComparison.Ordinal) >= 0) return true;
					if (sLine.IndexOf("__", StringComparison.Ordinal) >= 0) return true;
					if (sLine.IndexOf("~~", StringComparison.Ordinal) >= 0) return true;
					if (sLine.IndexOf('`') >= 0) return true;
					try {
					if (MarkdownInlineLinkRegex.IsMatch(sLine)) return true;
					}
					catch {}
					return false;
					} // MarkdownLineHasRichInlineMarkup method

	private static void MarkdownReview_AddNonInlineLinksFromLine(string sLine, int iLineStart, List<int> links, List<int[]> inlineLinks) {
	if (String.IsNullOrEmpty(sLine)) return;

	try {
	foreach (Match m in MarkdownAutoLinkRegex.Matches(sLine)) {
	if (m == null || !m.Success) continue;
	int i = iLineStart + m.Index;
	if (MarkdownReview_IsIndexInAnyInlineLinkSpan(inlineLinks, i)) continue;
	links.Add(i);
	}
	foreach (Match m in MarkdownBareUrlRegex.Matches(sLine)) {
	if (m == null || !m.Success) continue;
	int i = iLineStart + m.Index;
	if (MarkdownReview_IsIndexInAnyInlineLinkSpan(inlineLinks, i)) continue;
	links.Add(i);
	}
	foreach (Match m in MarkdownWwwUrlRegex.Matches(sLine)) {
	if (m == null || !m.Success) continue;
	int i = iLineStart + m.Index;
	if (MarkdownReview_IsIndexInAnyInlineLinkSpan(inlineLinks, i)) continue;
	links.Add(i);
	}
	}
	catch {}
	} // MarkdownReview_AddNonInlineLinksFromLine method

	private static string MarkdownReview_CleanUrl(string sUrl) {
	if (String.IsNullOrEmpty(sUrl)) return "";
	string s = sUrl.Trim();
	int iParenOpen = 0, iParenClose = 0, iBracketOpen = 0, iBracketClose = 0, iBraceOpen = 0, iBraceClose = 0;
	try {
	for (int i = 0; i < s.Length; i++) {
	char c = s[i];
	if (c == '(') iParenOpen++;
	else if (c == ')') iParenClose++;
	else if (c == '[') iBracketOpen++;
	else if (c == ']') iBracketClose++;
	else if (c == '{') iBraceOpen++;
	else if (c == '}') iBraceClose++;
	}
	}
	catch {
	iParenOpen = 0;
	iParenClose = 0;
	iBracketOpen = 0;
	iBracketClose = 0;
	iBraceOpen = 0;
	iBraceClose = 0;
	}

	while (s.Length > 0) {
	char c = s[s.Length - 1];
	if (c == ')') {
	if (iParenClose > iParenOpen) {
	iParenClose--;
	s = s.Substring(0, s.Length - 1);
	continue;
	}
	break;
	}
	if (c == ']') {
	if (iBracketClose > iBracketOpen) {
	iBracketClose--;
	s = s.Substring(0, s.Length - 1);
	continue;
	}
	break;
	}
	if (c == '}') {
	if (iBraceClose > iBraceOpen) {
	iBraceClose--;
	s = s.Substring(0, s.Length - 1);
	continue;
	}
	break;
	}
	if (c == '.' || c == ',' || c == ';' || c == ':' || c == '!' || c == '?' || c == '"' || c == '\'') {
	s = s.Substring(0, s.Length - 1);
	continue;
	}
	break;
	}
	return s.Trim();
	} // MarkdownReview_CleanUrl method

	private static string MarkdownReview_CollapseWhitespace(string sText) {
	if (String.IsNullOrEmpty(sText)) return "";
	StringBuilder sb = new StringBuilder(sText.Length);
	bool bSpace = false;
	for (int i = 0; i < sText.Length; i++) {
	char c = sText[i];
	if (Char.IsWhiteSpace(c)) {
	bSpace = true;
	continue;
	}
	if (bSpace && sb.Length > 0) sb.Append(' ');
	bSpace = false;
	sb.Append(c);
	}
	return sb.ToString().Trim();
	} // MarkdownReview_CollapseWhitespace method

	private void MarkdownReview_EnsureCache(MdiChild child) {
	if (child == null) return;
	HomerRichTextBox rtb = child.RTB;
	if (rtb == null) return;

	string sText = rtb.Text;
	int iRevision = child.TextRevision;
	if (child.MarkdownReviewHeadings != null && child.MarkdownReviewCacheRevision == iRevision) return;

	List<int> headings, lists, items, links, tables;
	List<int[]> inlineLinks;
	MarkdownReview_ParseText(sText, out headings, out lists, out items, out links, out tables, out inlineLinks);

	child.MarkdownReviewCacheRevision = iRevision;
	child.MarkdownReviewCacheLength = sText.Length;
	child.MarkdownReviewInlineLinks = inlineLinks;
	child.MarkdownReviewHeadings = headings;
	child.MarkdownReviewLists = lists;
	child.MarkdownReviewListItems = items;
	child.MarkdownReviewLinks = links;
	child.MarkdownReviewTables = tables;
	} // MarkdownReview_EnsureCache method

	private static List<int[]> MarkdownReview_FindFenceRanges(string sText) {
	List<int[]> ranges = new List<int[]>();
	if (String.IsNullOrEmpty(sText)) return ranges;

	bool bInFence = false;
	int iFenceStart = -1;
	int iPos = 0;
	int iLen = sText.Length;
	while (iPos <= iLen) {
	int iLf = -1;
	if (iPos < iLen) {
	try {iLf = sText.IndexOf('\n', iPos);} catch {iLf = -1;}
	}
	bool bHasLf = (iLf >= 0);
	if (!bHasLf) iLf = iLen;

	int iLineTextEnd = iLf;
	if (iLineTextEnd > iPos && sText[iLineTextEnd - 1] == '\r') iLineTextEnd--;

	int iFirst = iPos;
	while (iFirst < iLineTextEnd && Char.IsWhiteSpace(sText[iFirst])) iFirst++;
	bool bFenceLine = false;
	if (iFirst + 2 < iLineTextEnd) {
	char c = sText[iFirst];
	if ((c == '`' || c == '~') && sText[iFirst + 1] == c && sText[iFirst + 2] == c) bFenceLine = true;
	}

	if (bFenceLine) {
	if (!bInFence) {
	bInFence = true;
	iFenceStart = iPos;
	}
	else {
	bInFence = false;
	int iFenceEnd = bHasLf ? (iLf + 1) : iLen;
	if (iFenceStart < 0) iFenceStart = 0;
	if (iFenceEnd < iFenceStart) iFenceEnd = iFenceStart;
	ranges.Add(new int[] {iFenceStart, iFenceEnd});
	iFenceStart = -1;
	}
	}

	if (!bHasLf) break;
	iPos = iLf + 1;
	}

	if (bInFence && iFenceStart >= 0) ranges.Add(new int[] {iFenceStart, iLen});
	return ranges;
	} // MarkdownReview_FindFenceRanges method

			private static List<int[]> MarkdownReview_FindInlineLinks(string sText) {
			List<int[]> links = new List<int[]>();
			if (String.IsNullOrEmpty(sText)) return links;

			List<int[]> fenceRanges = MarkdownReview_FindFenceRanges(sText);

			int iLen = sText.Length;
			int i = 0;
			while (i < iLen) {
			int iBracket = -1;
			try {iBracket = sText.IndexOf('[', i);} catch {iBracket = -1;}
			if (iBracket < 0) break;

			if (MarkdownReview_IsIndexInRangesSorted(fenceRanges, iBracket)) {
			i = iBracket + 1;
			continue;
			}

			int iMatchStart = iBracket;
			try {
			if (iBracket > 0 && sText[iBracket - 1] == '!') iMatchStart = iBracket - 1;
			}
			catch {iMatchStart = iBracket;}

			int iTextStart = iBracket + 1;

			int iClose = -1;
			bool bEsc = false;
			for (int j = iTextStart; j < iLen; j++) {
			char c = sText[j];
			if (bEsc) {
			bEsc = false;
			continue;
			}
			if (c == '\\') {
			bEsc = true;
			continue;
			}
			if (c == ']') {
			iClose = j;
			break;
			}
			}
			if (iClose < 0) {
			i = iBracket + 1;
			continue;
			}

			int iTextEnd = iClose;

			int k = iClose + 1;
			while (k < iLen && Char.IsWhiteSpace(sText[k])) k++;
			if (k >= iLen || sText[k] != '(') {
			i = iBracket + 1;
			continue;
			}

			int iUrlStart = k + 1;
			int depth = 1;
			bool bInDouble = false;
			bool bInSingle = false;
			bool bEscape = false;
			int iUrlEnd = -1;
			int iMatchEnd = -1;
			for (int p = iUrlStart; p < iLen; p++) {
			char c = sText[p];
			if (bEscape) {
			bEscape = false;
			continue;
			}
			if (c == '\\') {
			bEscape = true;
			continue;
			}

			if (!bInSingle && c == '"') {
			bInDouble = !bInDouble;
			continue;
			}
			if (!bInDouble && c == '\'') {
			bInSingle = !bInSingle;
			continue;
			}

			if (!bInDouble && !bInSingle) {
			if (c == '(') {
			depth++;
			continue;
			}
			if (c == ')') {
			depth--;
			if (depth == 0) {
			iUrlEnd = p;
			iMatchEnd = p + 1;
			break;
			}
			}
			}
			}
			if (iUrlEnd < 0 || iMatchEnd < 0) {
			i = iBracket + 1;
			continue;
			}

			if (iMatchStart < 0) iMatchStart = 0;
			if (iMatchEnd < iMatchStart) iMatchEnd = iMatchStart;
			if (iTextStart < iMatchStart) iTextStart = iMatchStart;
			if (iTextEnd < iTextStart) iTextEnd = iTextStart;
			if (iUrlStart < iMatchStart) iUrlStart = iMatchStart;
			if (iUrlEnd < iUrlStart) iUrlEnd = iUrlStart;

			links.Add(new int[] {iMatchStart, iMatchEnd, iTextStart, iTextEnd, iUrlStart, iUrlEnd});

			i = iMatchEnd;
			}

			return links;
			} // MarkdownReview_FindInlineLinks method

	private static bool MarkdownReview_IsFenceLine(string sLine) {
	if (sLine == null) return false;
	string s = sLine.TrimStart();
	return s.StartsWith("```") || s.StartsWith("~~~");
	} // MarkdownReview_IsFenceLine method

// ---- Markdown section move (Control+Alt+Up/Down) ----
		private bool HandleSectionMoveKey(Keys keyData) {
		if ((keyData & (Keys.Control | Keys.Alt)) != (Keys.Control | Keys.Alt)) return false;
		if ((keyData & Keys.Shift) == Keys.Shift) return false;
		Keys keyCode = keyData & Keys.KeyCode;
		if (keyCode != Keys.Up && keyCode != Keys.Down) return false;

		if (this.KeyDescriber) {
		AddMessage(keyCode == Keys.Up ? "Move section up" : "Move section down");
		return true;
		}

		MdiChild child = this.Child;
		if (child == null || child.RTB == null) return true;
		MoveCurrentSection(child.RTB, keyCode == Keys.Up);
		return true;
		} // HandleSectionMoveKey method

		// File Slots -- Alt+digit opens the file assigned to that slot,
		// Alt+Shift+digit assigns the current file to it.  Requested by
		// Kasperczak as a regression from EdSharp 4 (Telegram 13.08.2026).
		// Slots live in the INI section "FileSlots" under keys 1..10, so they
		// survive restarts.  Digit 0 is slot 10 (the keyboard order 1..9,0).
		// Both the top-row digits and the numeric keypad are accepted.
		// NOTE: Alt+7 / Alt+0 / Alt+Shift+0 previously belonged to other
		// commands; those were moved to free chords on his explicit
		// authorization.  Alt+Shift+6 (Baseline) is deliberately NOT claimed
		// here -- it stays with Baseline until he decides otherwise, so
		// assigning to slot 6 is the one gap in the range.
		private const string c_sFileSlotSection = "FileSlots";

		// MENU KONTEKSTOWE PISOWNI NA WYRAZIE POD KURSOREM (12.09.2026, prosba
		// Kasperczaka: "Dobrze tez, zeby to wszystko bylo dostepne z menu
		// kontekstowego na danym slowie").
		//
		// Klawisz: Aplikacje (Keys.Apps, ten obok prawego Ctrl) - i TYLKO on.
		// Dlaczego wlasnie ten i dlaczego nie ma drugiego:
		//   - Keys.Apps nie wystepowal w calym kodzie ani w Hotkeys.ini - byl
		//     wolny, a jest to klawisz, ktory kazdy uzytkownik czytnika ma w
		//     palcach jako "menu na tym, na czym stoje".
		//   - Shift+F10 (drugi naturalny kandydat) jest ZAJETY przez "Context
		//     Menu" powloki Windows dla pliku.
		//   - Alt+Shift+F7 sprawdzilem i tez jest ZAJETY - "Translate Language"
		//     w Hotkeys.ini.  Pierwotnie wpisalem tu wlasnie ten skrot i byl to
		//     blad wychwycony dopiero przy sprawdzeniu Hotkeys.ini, nie kodu:
		//     kolizje skrotow trzeba sprawdzac W OBU miejscach, bo plik skrotow
		//     zna przypisania, ktorych w kodzie C# nie widac.
		//   - Ctrl+Alt+litera odpada z zasady: przez AltGr zjada polskie znaki.
		// Dla klawiatur bez klawisza Aplikacje ta sama rzecz jest w menu
		// Miscellaneous jako "Word Spelling Menu", wiec nikt nie zostaje bez
		// dostepu tylko z powodu sprzetu.
		//
		// Menu dziala na wyrazie POD KURSOREM, bez uruchamiania calego
		// sprawdzania - to jest szybka sciezka dla jednego slowa, ktore wlasnie
		// wyglada podejrzanie.
		private bool HandleSpellingWordMenuKey(Keys keyData) {
			Keys keyCode = keyData & Keys.KeyCode;
			if (keyCode != Keys.Apps) return false;
			if ((keyData & (Keys.Control | Keys.Alt | Keys.Shift)) != Keys.None) return false;
			if (this.Child == null) return false;
			this.SpellingWordMenu();
			return true;
		} // HandleSpellingWordMenuKey method

// Menu pisowni dla JEDNEGO wyrazu - tego, na ktorym stoi kursor.
// Kolejnosc pozycji jest celowa: najpierw "Add to dictionary", bo w
// polskim tekscie najczestszym powodem zgloszenia jest poprawne nazwisko
// lub nazwa wlasna, ktorej brak w slowniku.  Podpowiedzi sa nizej, bo
// siega sie po nie rzadziej.
public void SpellingWordMenu() {
	MdiChild child = this.Child;
	if (child == null) return;
	HomerRichTextBox rtb = child.RTB;

	if (!Pisownia.Dostepne()) { Say.say("Spell checking not available"); return; }

	// Granice wyrazu wokol kursora.  Litera to dla nas wszystko, co .NET
	// uznaje za litere (wiec polskie znaki tez), plus apostrof i lacznik
	// wewnatrz wyrazu.
	string sText = rtb.Text;
	int iPos = rtb.SelectionStart;
	if (sText.Length == 0) { Say.say("No text"); return; }
	if (iPos >= sText.Length) iPos = sText.Length - 1;
	if (iPos < 0) iPos = 0;
	// Gdy kursor stoi ZA wyrazem (spacja, koniec zdania), cofamy sie o
	// jeden znak - tak jak dziala czytanie wyrazu w czytniku ekranu.
	if (!Char.IsLetter(sText[iPos]) && iPos > 0 && Char.IsLetter(sText[iPos - 1])) iPos--;
	if (!Char.IsLetter(sText[iPos])) { Say.say("No word at cursor"); return; }

	int iStart = iPos;
	while (iStart > 0 && (Char.IsLetter(sText[iStart - 1]) || sText[iStart - 1] == '\'' || sText[iStart - 1] == '-')) iStart--;
	int iEnd = iPos;
	while (iEnd + 1 < sText.Length && (Char.IsLetter(sText[iEnd + 1]) || sText[iEnd + 1] == '\'' || sText[iEnd + 1] == '-')) iEnd++;
	// Lacznik/apostrof na samym brzegu nie nalezy do wyrazu.
	while (iEnd > iStart && !Char.IsLetter(sText[iEnd])) iEnd--;
	string sWord = sText.Substring(iStart, iEnd - iStart + 1);
	if (sWord.Length == 0) { Say.say("No word at cursor"); return; }

	List<Pisownia.Blad> lBledy = Pisownia.Sprawdz(sWord);
	bool bBlad = (lBledy.Count > 0);
	List<string> lPodp = bBlad ? lBledy[0].Podpowiedzi : new List<string>();

	List<string> lOpcje = new List<string>();
	lOpcje.Add("Add to dictionary");
	lOpcje.Add("Ignore this word");
	if (bBlad) {
		int iIle = 0;
		foreach (string sP in lPodp) {
			if (sP == null || sP.Length == 0) continue;
			lOpcje.Add("Replace with: " + sP);
			iIle++;
			if (iIle >= 10) break;
		}
	}
	lOpcje.Add("Spell check from here");

	string sTytul = bBlad ? ("Spelling: " + sWord) : ("Spelling: " + sWord + " (correct)");
	string sWybor = Dialog.Pick(sTytul, lOpcje.ToArray(), true, 0);
	if (sWybor.Length == 0) return;

	if (String.Equals(sWybor, "Add to dictionary", StringComparison.OrdinalIgnoreCase)) {
		if (Pisownia.Dodaj(sWord)) { AddMessage("Added: " + sWord); Say.say("Added to dictionary"); }
		else Say.say("Could not add word");
		return;
	}
	if (String.Equals(sWybor, "Ignore this word", StringComparison.OrdinalIgnoreCase)) {
		if (Pisownia.Pomijaj(sWord)) { AddMessage("Ignoring: " + sWord); Say.say("Ignoring this word"); }
		else Say.say("Could not ignore word");
		return;
	}
	if (String.Equals(sWybor, "Spell check from here", StringComparison.OrdinalIgnoreCase)) {
		// Od poczatku TEGO wyrazu, nie od miejsca kursora w jego srodku -
		// inaczej wyraz, ktory wlasnie ogladamy, wypadlby ze sprawdzania.
		rtb.Select(iStart, 0);
		this.SpellCheckSystem();
		return;
	}
	if (sWybor.StartsWith("Replace with: ")) {
		string sNowe = sWybor.Substring("Replace with: ".Length);
		rtb.Select(iStart, sWord.Length);
		rtb.SelectedText = sNowe;
		rtb.Select(iStart + sNowe.Length, 0);
		AddMessage(String.Format("Replaced: {0} -> {1}", sWord, sNowe));
		Say.say("Replaced");
		return;
	}
} // SpellingWordMenu method

private bool HandleFileSlotKey(Keys keyData) {
		if ((keyData & Keys.Alt) != Keys.Alt) return false;
		if ((keyData & Keys.Control) == Keys.Control) return false;
		bool bAssign = ((keyData & Keys.Shift) == Keys.Shift);

		Keys keyCode = keyData & Keys.KeyCode;
		int iDigit = -1;
		if (keyCode >= Keys.D0 && keyCode <= Keys.D9) iDigit = (int) keyCode - (int) Keys.D0;
		else if (keyCode >= Keys.NumPad0 && keyCode <= Keys.NumPad9) iDigit = (int) keyCode - (int) Keys.NumPad0;
		else return false;

		// Keyboard order: 1..9 are slots 1..9, and 0 is slot 10.
		int iSlot = (iDigit == 0) ? 10 : iDigit;

		// Yield to any command still holding this chord, so we never silently
		// shadow an existing hotkey.  As of 5.0.8 the whole Alt+digit and
		// Alt+Shift+digit range is free (Baseline moved to Alt+Shift+F6 and
		// Reset Configuration to Alt+Shift+F10), so all ten slots work.
		if (hashKey.ContainsKey(keyData)) return false;

		if (this.KeyDescriber) {
		AddMessage((bAssign ? "Assign numbered file " : "Open numbered file ") + iSlot);
		return true;
		}

		if (bAssign) AssignFileSlot(iSlot);
		else OpenFileSlot(iSlot);
		return true;
		} // HandleFileSlotKey method

		// Control+digit -- go to an open editing window by the order in which it
		// was opened, and say its file name.  Requested by Kasperczak as a
		// regression from EdSharp 4 (Telegram 14.08.2026 19:23): "Control 1 do
		// Control 9 to jest cos takiego jak Control Tab, Control Shift Tab, czyli
		// nawigacja pomiedzy plikami w otwartych zakladkach (...) program powinien
		// czytac, jak nacisne Control 1 - nazwe pierwszego pliku (...) pierwszego
		// i drugiego to rozumiem w kolejnosci otwarcia."  Deliberately unrelated
		// to the Alt+digit numbered files: those are fixed assignments, these are
		// just the tabs that happen to be open now.
		// Chords: Control+1..Control+9 only.  Control+0 is NOT claimed: there is no
		// tenth tab slot in what he asked for.  (Control+D0 used to be Go to Folder;
		// since the F4 family was tidied up on 30.08.2026 that command sits on
		// Shift+F4 and Control+0 is simply free.)  Control+4 and Control+6 now navigate
		// too: he authorized moving the two commands that held them (Telegram
		// 14.08.2026 22:35, "Przenies. CTRL-1-9 opening files tab"), so Format
		// Code went to Control+Shift+F6 and Next Baseline to Control+F2.
		private bool HandleWindowNumberKey(Keys keyData) {
		if ((keyData & Keys.Control) != Keys.Control) return false;
		if ((keyData & Keys.Alt) == Keys.Alt) return false;
		if ((keyData & Keys.Shift) == Keys.Shift) return false;

		Keys keyCode = keyData & Keys.KeyCode;
		int iDigit = -1;
		if (keyCode >= Keys.D1 && keyCode <= Keys.D9) iDigit = (int) keyCode - (int) Keys.D0;
		else if (keyCode >= Keys.NumPad1 && keyCode <= Keys.NumPad9) iDigit = (int) keyCode - (int) Keys.NumPad0;
		else return false;

		// Never shadow a command that already owns this chord.
		if (hashKey.ContainsKey(keyData)) return false;

		if (this.KeyDescriber) {
		AddMessage("Go to window " + iDigit);
		return true;
		}

		GoToWindowByOpenOrder(iDigit);
		return true;
		} // HandleWindowNumberKey method

		// Activate the nth window in opening order and say its file name.
		private void GoToWindowByOpenOrder(int iNumber) {
		List<MdiChild> children = new List<MdiChild>();
		foreach (MdiChild c in this.MdiChildren) children.Add(c);
		if (children.Count == 0) {
		AddMessage("No windows!");
		return;
		}

		children.Sort(delegate(MdiChild a, MdiChild b) {return a.OpenSequence.CompareTo(b.OpenSequence);});
		if (iNumber > children.Count) {
		AddMessage("Only " + children.Count + (children.Count == 1 ? " window!" : " windows!"));
		return;
		}

		MdiChild target = children[iNumber - 1];
		string sName = GetWindowSpokenName(target);
		if (target == this.Child) {
		// Already here: say the name anyway, so the key always answers.
		AnnounceWindowName(sName);
		return;
		}

		target.Activate();
		// Activating a window moves the focus, and a plain message would be
		// swallowed or delayed by the screen reader announcing the control.
		// Force-speak it, the same way the section move commands do.
		AnnounceWindowName(sName);
		} // GoToWindowByOpenOrder method

		// Say a window name so it survives the focus change.
		private void AnnounceWindowName(string sName) {
		if (sName == null || sName.Length == 0) return;
		try {if (Win32.IsNVDAActive()) Win32.NVDACancelSpeech();} catch {}
		AddMessage(sName, true);
		} // AnnounceWindowName method

		// What a window is called out loud: the file name, or the window title
		// for a document that has never been saved.
		private static string GetWindowSpokenName(MdiChild child) {
		if (child == null) return "";
		string sFile = child.File;
		if (sFile != null && sFile.Contains(@"\")) return Path.GetFileName(sFile);
		return (child.Text == null) ? "" : child.Text;
		} // GetWindowSpokenName method

		// Control+W = Close Window, an additional chord alongside Control+F4.
		// In stock EdSharp Control+W was Word Wrap; that command was moved to
		// Control+F12 on Kasperczak's explicit authorization.
		// Control+Right / Control+Left are deliberately NOT handled here.
		// We used to move the caret ourselves, because the OLD RichEdit window
		// class (riched20) broke a word at every accented letter.  The real fault
		// was the window class, and it is now fixed at the source: the editing
		// control asks Windows for RICHEDIT50W (see HomerRichTextBox.CreateParams),
		// which applies Unicode word rules and reports Polish words correctly.
		// Handling the chord here as well made things WORSE, twice over, and both
		// regressions were reported by Kasperczak:
		//  - our own Util.Say added a SECOND voice, because the screen reader binds
		//    Control with an arrow to its own "move by word" command and announces
		//    the destination itself ("czyta dwa razy kazde slowo", 17.08.2026);
		//  - staying silent was not enough either.  Our caret and the control
		//    disagreed about where a word starts around punctuation -- MEASURED,
		//    5 of 16 sequences differed, e.g. "slowo, przecinek": ours stopped at
		//    7 18, the control at 5 7 16 18.  The reader speaks the word IT thinks
		//    the caret is in, so a caret parked at our boundary made it read from
		//    the previous word ("Doklada slowo, jesli jest polska literka").
		// Letting the control move its own caret keeps caret and announcement in
		// agreement, which is the only state where a blind user hears one word.
		// Rodzina Control+F6 nalezy do NAS, nie do systemowego cyklu okien MDI.
		//
		// CO SIE DZIALO (zmierzone 31.08.2026 na zywym NVDA): Kasperczak zglosil,
		// ze Control+Shift+F6 "wlacza wylacza tryb" i mowi "Accessible Selection".
		// Napisu "Accessible Selection" NIE MA ani w naszym kodzie, ani w binarce
		// (obie kodowania, zero trafien), ani w NVDA, ani w jego dodatkach - to
		// nazwa OKNA, ktora czytnik zlozyl z nazwy klasy pola edycji, gdy fokus
		// przeskoczyl do drugiego okna dokumentu.  Sprawca to WinForms: MdiClient
		// obsluguje Control+F6 i Control+Shift+F6 jako wbudowany cykl okien
		// potomnych, ZANIM zobaczy je nasza tablica skrotow.  Przy jednym
		// otwartym pliku nic sie nie dzieje (stad "tego nie bylo"), a przy dwoch
		// klawisz cicho przelacza dokument - dokladnie to zmierzylismy:
		// windowControlID pola edycji zmienil sie 9767026 -> 8980688.
		//
		// DLACZEGO BLOKUJEMY: jego ustalenie edsharpng-47 mowi wprost, ze
		// systemowe przechodzenie miedzy okienkami dokumentow na tym klawiszu
		// nalezy ODEBRAC, bo Control+F6 ma dostac liste linkow.  Do zmiany okna
		// sluzy Control+Tab i Control+Shift+Tab (Next/Prior Window), a lista
		// okien jest pod F4.  Kradziez tych dwoch chordow przez MdiClient
		// oznaczala, ze przyszla lista linkow po prostu by nie zadzialala.
		//
		// Blokada USTEPUJE naszym wlasnym komendom: gdy chord trafi kiedys do
		// tablicy skrotow (hashKey), obsluga wraca do menu i ta metoda milczy.
		private bool HandleMdiWindowCycleKey(Keys keyData) {
		if (keyData != (Keys.Control | Keys.F6) && keyData != (Keys.Control | Keys.Shift | Keys.F6)) return false;
		if (hashKey.ContainsKey(keyData)) return false;

		if (this.KeyDescriber) {
		AddMessage("Nothing");
		return true;
		}

		return true;
		} // HandleMdiWindowCycleKey method

		private bool HandleCloseWindowKey(Keys keyData) {
		if (keyData != (Keys.Control | Keys.W)) return false;
		if (hashKey.ContainsKey(keyData)) return false;

		if (this.KeyDescriber) {
		AddMessage("Close Window");
		return true;
		}

		MdiChild child = this.Child;
		if (child == null) return true;
		SetMessage("Close Window");
		CloseWindow(child);
		return true;
		} // HandleCloseWindowKey method


		// Assign the current document to a slot.  Requires a real file on
		// disk: an unsaved NoName window has nothing to remember.
		private void AssignFileSlot(int iSlot) {
		MdiChild child = this.Child;
		string sFile = (child == null) ? "" : child.File;
		if (sFile.Length == 0 || !sFile.Contains(@"\")) {
		AddMessage("No disk file is open for this command!");
		return;
		}

		App.WriteValue(c_sFileSlotSection, iSlot.ToString(), sFile);
		AddMessage("Numbered file " + iSlot + " is " + Path.GetFileName(sFile));
		} // AssignFileSlot method

		// Open (or activate, if already open) the file remembered in a slot.
		// Kasperczak asked for the speech to be plain (Telegram 14.08.2026
		// 19:16): "Alt-cyfra Returning. Niepotrzebne, Wystarczy, jak program
		// powie nazwe pliku. Jesli nie jest otwarty, to najwyzej opening nazwa
		// pliku."  So the generic "returning" of OpenOrActivateWindow is bypassed
		// here: an already open file just says its name, and a closed one says
		// "Opening" and the name.
		private void OpenFileSlot(int iSlot) {
		string sFile = App.ReadValue(c_sFileSlotSection, iSlot.ToString(), "");
		sFile = Util.Unquote(sFile).Trim();
		if (sFile.Length == 0) {
		AddMessage("Numbered file " + iSlot + " is empty!");
		return;
		}

		if (!File.Exists(sFile)) {
		AddMessage("Numbered file " + iSlot + " not found!");
		return;
		}

		string sName = Path.GetFileName(sFile);
		foreach (MdiChild child in this.MdiChildren) {
		if (!Util.Equiv(child.File, Util.GetLfn(sFile))) continue;
		child.Activate();
		AnnounceWindowName(sName);
		return;
		}

		AnnounceWindowName("Opening " + sName);
		OpenOrActivateWindow(sFile, 0);
		} // OpenFileSlot method

		// The Numbered Files menu item: lists every assigned number so the
		// feature is discoverable, and opens the one chosen.  Named "Numbered
		// Files" on Kasperczak's choice (Telegram 14.08.2026 19:26: "po ang.
		// Numbered files"; Polish "pliki numerowane" for the manual).
		private void PickFileSlot() {
		List<string> lsValue = new List<string>();
		List<string> lsDisplay = new List<string>();
		for (int i = 1; i <= 10; i++) {
		string sFile = Util.Unquote(App.ReadValue(c_sFileSlotSection, i.ToString(), "")).Trim();
		if (sFile.Length == 0) continue;
		int iKey = (i == 10) ? 0 : i;
		lsValue.Add(sFile);
		lsDisplay.Add("Alt+" + iKey + "   " + Path.GetFileName(sFile) + "   " + sFile);
		}

		if (lsValue.Count == 0) {
		AddMessage("No numbered files are assigned!");
		return;
		}

		string sPick = Dialog.Pick("Numbered Files", lsValue.ToArray(), lsDisplay.ToArray(), false, 0);
		if (sPick.Length == 0) return;
		if (!File.Exists(sPick)) {
		AddMessage("File not found!");
		return;
		}

		OpenOrActivateWindow(sPick, 0);
		} // PickFileSlot method


			private void MoveCurrentSection(HomerRichTextBox rtb, bool bUp) {
			if (rtb == null) return;
			string sText = rtb.Text;
			if (sText.Length == 0) return;

			if (MoveCurrentMarkdownHeadingSection(rtb, bUp)) return;

				List<int> starts = GetSectionStarts(sText);
			if (starts.Count <= 1) {
			AnnounceSectionMoveMessage("No section to move");
			return;
			}

		int iCurrent = rtb.Index;
		if (iCurrent >= sText.Length && sText.Length > 0) iCurrent = sText.Length - 1;

		int iSection = 0;
		for (int i = 0; i < starts.Count; i++) {
		int iStart = starts[i];
		int iEnd = (i + 1 < starts.Count) ? starts[i + 1] : sText.Length;
		if (iCurrent >= iStart && iCurrent < iEnd) {
		iSection = i;
		break;
		}
		}

			if (bUp && iSection == 0) {
			AnnounceSectionMoveMessage("Top!");
			return;
			}

			if (!bUp && iSection >= starts.Count - 1) {
			AnnounceSectionMoveMessage("Bottom!");
			return;
			}

		int iThisStart = starts[iSection];
		int iThisEnd = (iSection + 1 < starts.Count) ? starts[iSection + 1] : sText.Length;
		int iOffset = iCurrent - iThisStart;
		string sCurrent = sText.Substring(iThisStart, iThisEnd - iThisStart);

			if (bUp) {
			int iPrevStart = starts[iSection - 1];
			string sBefore = sText.Substring(0, iPrevStart);
			string sPrevious = sText.Substring(iPrevStart, iThisStart - iPrevStart);
			string sAfter = sText.Substring(iThisEnd);
				string sPreviousTitle = GetSectionAnnouncementTitle(sPrevious);
				rtb.Text = sBefore + sCurrent + sPrevious + sAfter;
				rtb.Index = iPrevStart + iOffset;
				AnnounceSectionMoveMessage("Above " + sPreviousTitle);
				}
				else {
			int iNextEnd = (iSection + 2 < starts.Count) ? starts[iSection + 2] : sText.Length;
			string sBefore = sText.Substring(0, iThisStart);
			string sNext = sText.Substring(iThisEnd, iNextEnd - iThisEnd);
			string sAfter = sText.Substring(iNextEnd);
				string sNextTitle = GetSectionAnnouncementTitle(sNext);
				rtb.Text = sBefore + sNext + sCurrent + sAfter;
				rtb.Index = iThisStart + sNext.Length + iOffset;
				AnnounceSectionMoveMessage("Below " + sNextTitle);
				}
				rtb.Modified = true;
				} // MoveCurrentSection method

			private void AnnounceSectionMoveMessage(string sMessage) {
			try {if (Win32.IsNVDAActive()) Win32.NVDACancelSpeech();} catch {}
				AddMessage(sMessage, true);
				} // AnnounceSectionMoveMessage method

				private class MarkdownSectionHeading {
				public int Start;
				public int Level;
				public string Title;
				} // MarkdownSectionHeading class

				private bool MoveCurrentMarkdownHeadingSection(HomerRichTextBox rtb, bool bUp) {
				string sText = rtb.Text ?? "";
				List<MarkdownSectionHeading> headings = GetMarkdownSectionHeadings(sText);
				if (headings.Count == 0) return false;

				int iCurrent = rtb.Index;
				if (iCurrent >= sText.Length && sText.Length > 0) iCurrent = sText.Length - 1;

				int iHeading = GetCurrentMarkdownSectionHeadingIndex(headings, iCurrent);
				if (iHeading < 0) {
				AnnounceSectionMoveMessage("No heading section");
				return true;
				}

				MarkdownSectionHeading current = headings[iHeading];
				int iSibling = bUp ? GetPreviousSiblingHeadingIndex(headings, iHeading) : GetNextSiblingHeadingIndex(headings, iHeading);
				if (iSibling < 0) {
				AnnounceSectionMoveMessage(bUp ? "Top!" : "Bottom!");
				return true;
				}

				int iCurrentStart = current.Start;
				int iCurrentEnd = GetMarkdownSectionEnd(sText, headings, iHeading);
				int iOffset = Math.Max(0, iCurrent - iCurrentStart);
				string sCurrent = sText.Substring(iCurrentStart, iCurrentEnd - iCurrentStart);

				if (bUp) {
				MarkdownSectionHeading previous = headings[iSibling];
				int iPreviousStart = previous.Start;
				string sBefore = sText.Substring(0, iPreviousStart);
				string sPrevious = sText.Substring(iPreviousStart, iCurrentStart - iPreviousStart);
				string sAfter = sText.Substring(iCurrentEnd);
				rtb.Text = sBefore + sCurrent + sPrevious + sAfter;
				rtb.Index = iPreviousStart + Math.Min(iOffset, sCurrent.Length);
				AnnounceSectionMoveMessage("Above " + GetMarkdownSectionHeadingTitle(previous));
				}
				else {
				MarkdownSectionHeading next = headings[iSibling];
				int iNextEnd = GetMarkdownSectionEnd(sText, headings, iSibling);
				string sBefore = sText.Substring(0, iCurrentStart);
				string sNext = sText.Substring(iCurrentEnd, iNextEnd - iCurrentEnd);
				string sAfter = sText.Substring(iNextEnd);
				rtb.Text = sBefore + sNext + sCurrent + sAfter;
				rtb.Index = iCurrentStart + sNext.Length + Math.Min(iOffset, sCurrent.Length);
				AnnounceSectionMoveMessage("Below " + GetMarkdownSectionHeadingTitle(next));
				}

				rtb.Modified = true;
				return true;
				} // MoveCurrentMarkdownHeadingSection method

				// Control+PageDown / Control+PageUp -- go to the next or previous
				// Markdown heading of ANY level.  Kasperczak confirmed "every
				// level" explicitly (Telegram 14.08.2026 18:34).  With Shift held
				// it walks only headings of the SAME level as the section the
				// cursor is in, skipping subsections -- he approved that too
				// (18:50: "Pomysl z nawigacja Ctrl + Page Up, Page Down i z
				// Shiftem po wszystkich podanego poziomu sekcjach to jest pomysl
				// jak najbardziej dobry").  At the last or first matching heading
				// the cursor stays put and only a message is announced, matching
				// what we agreed for bookmarks (no wrapping).
				private void GoToAdjacentMarkdownHeading(HomerRichTextBox rtb, bool bUp) {
				GoToAdjacentMarkdownHeading(rtb, bUp, false);
				} // GoToAdjacentMarkdownHeading method

				private void GoToAdjacentMarkdownHeading(HomerRichTextBox rtb, bool bUp, bool bSameLevelOnly) {
				if (rtb == null) return;
				string sText = rtb.Text;
				List<MarkdownSectionHeading> headings = GetMarkdownSectionHeadings(sText);
				if (headings.Count == 0) {
				AddMessage("No headings!");
				return;
				}

				int iCurrent = rtb.Index;

				// "Same level" means the level of the section the cursor sits in.
				// Before the first heading of the file there is no such section,
				// so fall back to the level of the first heading.
				int iWantLevel = 0;
				if (bSameLevelOnly) {
				int iHere = GetCurrentMarkdownSectionHeadingIndex(headings, iCurrent);
				iWantLevel = (iHere >= 0) ? headings[iHere].Level : headings[0].Level;
				}

				int iTarget = -1;
				if (bUp) {
				for (int i = headings.Count - 1; i >= 0; i--) {
				if (headings[i].Start >= iCurrent) continue;
				if (bSameLevelOnly && headings[i].Level != iWantLevel) continue;
				iTarget = i;
				break;
				}
				if (iTarget == -1) {
				AddMessage(bSameLevelOnly ? "First heading at this level!" : "First heading!");
				return;
				}
				}
				else {
				for (int i = 0; i < headings.Count; i++) {
				if (headings[i].Start <= iCurrent) continue;
				if (bSameLevelOnly && headings[i].Level != iWantLevel) continue;
				iTarget = i;
				break;
				}
				if (iTarget == -1) {
				AddMessage(bSameLevelOnly ? "Last heading at this level!" : "Last heading!");
				return;
				}
				}

				rtb.Index = headings[iTarget].Start;
				// Announce the heading TEXT and then its level, e.g.
				// "Installation, heading 3".  Nothing else.
				// History of this wording, so it is not "fixed" back by mistake:
				// he wrote "wpierw tresc, a potem naglowek" (14.08.2026 18:50),
				// which was first read as "the body under the heading" -- wrong.
				// He clarified (14.08.2026 19:19): "Tresc jako tytul naglowka, w
				// sensie tresc tekstowa, a nie informacja. Informacja naglowek 3,
				// Dalsze prace naglowek 4. Tak powinno byc czytane."  So "tresc"
				// meant the heading's own text, and the body line must NOT be read.
				// The level is announced as "heading 3", NOT "heading level 3"
				// (14.08.2026 18:59: "nie tresc poziom naglowka 3").
				string sHeading = GetMarkdownSectionHeadingTitle(headings[iTarget]);
				Util.Say(sHeading + ", heading " + headings[iTarget].Level);
				} // GoToAdjacentMarkdownHeading method



				// Control+Enter -- start a new section, i.e. insert a Markdown
				// heading prefix at the level of the heading the cursor sits in.
				// Kasperczak's rule (Telegram 14.08.2026 18:34): "naglowek taki,
				// jak naglowek wyzej".  Before the first heading of the file there
				// is nothing above, so it becomes a level 1 title.
				private void InsertMarkdownHeadingAtCursor(HomerRichTextBox rtb) {
				if (rtb == null) return;
				string sText = rtb.Text;
				List<MarkdownSectionHeading> headings = GetMarkdownSectionHeadings(sText);
				int iCurrent = rtb.Index;

				int iLevel = 1;
				int iHeading = GetCurrentMarkdownSectionHeadingIndex(headings, iCurrent);
				if (iHeading >= 0) iLevel = headings[iHeading].Level;
				if (iLevel < 1) iLevel = 1;
				if (iLevel > 6) iLevel = 6;

				// Start the heading on a line of its own, with a blank line before
				// it when there is text above, so the Markdown stays valid.
				string sPrefix = "";
				if (iCurrent > 0) {
				bool bAtLineStart = (sText[iCurrent - 1] == '\n');
				if (!bAtLineStart) sPrefix = LF + LF;
				else {
				int iPrev = iCurrent - 1;
				if (iPrev > 0 && sText[iPrev - 1] == '\r') iPrev--;
				bool bBlankAbove = (iPrev == 0) || (sText[iPrev - 1] == '\n');
				if (!bBlankAbove) sPrefix = LF;
				}
				}

				string sHashes = new String('#', iLevel);
				string sInsert = sPrefix + sHashes + " ";
				rtb.ReplaceRange(iCurrent, iCurrent, sInsert);
				rtb.Index = iCurrent + sInsert.Length;
				rtb.Modified = true;
				AddMessage("Heading " + iLevel);
				} // InsertMarkdownHeadingAtCursor method

				// Document Navigation, F6.  The document's Markdown headings shown as a
				// TREE the way a book reader (PaperBack, and the old MHT/CHM help
				// viewers) shows a table of contents: sub-headings hang under their
				// parent, a branch expands and collapses, letter keys jump, and Enter
				// moves the editor cursor to that heading in the text.
				//
				// Kasperczak's specification (Telegram 18.08.2026 23:43-23:46):
				//   "F6 jako Nawigacje po dokumencie, cos jak w Paperback czytnik
				//   ksiazek.  Czyli drzewo z rozwijanymi naglowkami/podnaglowkami.
				//   Literowa nawigacja dziala, Enter skacze do tego naglowka w tekscie,
				//   F6 otwiera drzewo i pamieta ostatnie zaokusowane miejsce."
				//   "Czyli taka nawigacja, jak kiedys w plikach MHT byla."
				//   Empty document: "Komunikat, tak jak mowisz. I tyle." -- so with no
				//   heading the command SPEAKS and opens NOTHING.
				//
				// Why a TreeView and not the existing flat element list (F7 in preview):
				// a screen reader announces a tree node's LEVEL and its expanded or
				// collapsed state by itself, which is the whole point of the request --
				// the user hears "Installation, level 2, collapsed" and can step over a
				// whole branch.  A flat list cannot carry that.
				//
				// "Remembers the last focused place": the heading that was selected when
				// the tree was last closed, per window, kept in MdiChild.  On first open
				// (or when that heading is gone after editing) the selection falls back
				// to the heading the cursor is currently in, which is what a reader
				// expects when opening the contents while reading.
				private void ShowDocumentNavigationTree(HomerRichTextBox rtb) {
				if (rtb == null) return;
				MdiChild child = this.Child;
				if (child == null) return;

				string sText = rtb.Text ?? "";
				List<MarkdownSectionHeading> headings = GetMarkdownSectionHeadings(sText);
				if (headings.Count == 0) {
				// His decision: only a message, no empty window to escape from.
				AddMessage("No headings!");
				return;
				}

				// Build the tree.  Each node's Tag carries the heading's character
				// offset, so the jump never depends on the node's position or label.
				// A heading deeper than its predecessor becomes its child; equal or
				// shallower climbs back up.  Levels may skip (h1 then h3) and a file
				// may start at h2 -- both are normal in real documents, so parenting
				// walks up the open branch instead of assuming level == depth.
				LbcDialog dlg = new LbcDialog("Document Navigation", App.Frame);
				TreeView tv = dlg.addTreeView("Enter goes to the heading");
				dlg.setHelpDetail(tv, "Keys: Up and Down Arrow move, Right Arrow expands a heading, Left Arrow collapses it or goes to the parent, typing letters jumps to a heading starting with them, Enter goes to the selected heading in the text, Escape closes without moving. The tree reopens where you left it.");

				List<TreeNode> nodes = new List<TreeNode>();
				List<int> nodeLevels = new List<int>();
				tv.BeginUpdate();
				for (int i = 0; i < headings.Count; i++) {
				MarkdownSectionHeading heading = headings[i];
				TreeNode node = new TreeNode(GetDocumentNavigationNodeLabel(heading));
				node.Tag = heading.Start;

				TreeNode parent = null;
				for (int j = nodes.Count - 1; j >= 0; j--) {
				if (nodeLevels[j] < heading.Level) {parent = nodes[j]; break;}
				}
				if (parent == null) tv.Nodes.Add(node);
				else parent.Nodes.Add(node);

				nodes.Add(node);
				nodeLevels.Add(heading.Level);
				}
				// Start fully expanded so Down Arrow alone walks the whole document;
				// collapsing is the user's choice, not a state we impose.
				tv.ExpandAll();
				tv.EndUpdate();

				// Where to put the selection.  Remembered heading first (his "pamieta
				// ostatnie zaokusowane miejsce"), then the heading the cursor sits in.
				int iSelect = -1;
				if (child.DocumentNavigationLastOffset >= 0) {
				for (int i = 0; i < headings.Count; i++) {
				if (headings[i].Start == child.DocumentNavigationLastOffset) {iSelect = i; break;}
				}
				}
				if (iSelect < 0) iSelect = GetCurrentMarkdownSectionHeadingIndex(headings, rtb.Index);
				if (iSelect < 0) iSelect = 0;
				tv.SelectedNode = nodes[iSelect];
				dlg.setInitialFocus(tv);

				// Enter inside the tree accepts the dialog.  A TreeView swallows Enter
				// on its own, so without this the key would do nothing where the user
				// most expects it to act.
				tv.KeyDown += delegate(object oSender, KeyEventArgs ev) {
				if (ev.KeyData != Keys.Enter) return;
				ev.Handled = true; ev.SuppressKeyPress = true;
				Form frmHost = tv.FindForm();
				if (frmHost == null) return;
				frmHost.DialogResult = DialogResult.OK;
				frmHost.Close();
				};

				bool bOk = dlg.runOkCancel();
				TreeNode chosen = tv.SelectedNode;
				int iTarget = -1;
				if (chosen != null && chosen.Tag is int) iTarget = (int) chosen.Tag;
				dlg.Dispose();

				// Remember the place even when the user escaped: reopening the contents
				// should land where they were reading, whether or not they jumped.
				if (iTarget >= 0) child.DocumentNavigationLastOffset = iTarget;
				if (!bOk || iTarget < 0) return;

				if (iTarget > sText.Length) iTarget = sText.Length;
				rtb.Index = iTarget;
				// Announce exactly like the heading navigation commands do, so the two
				// ways of reaching a heading sound identical: text, then level.
				//
				// ODROCZENIE (test 1.5, zgloszenie Kasperczaka 26.08.2026): mowiac od
				// razu, tracilismy komunikat. Zamkniecie okna przenosi fokus z powrotem
				// na pole edycji, a czytnik oglasza NOWY fokus ("pole edycji ## ...")
				// PO tym, jak my juz powiedzielismy swoje - wiec kasowal nasza wypowiedz
				// i uzytkownik slyszal tytul okna zamiast poziomu naglowka. BeginInvoke
				// wypycha nasz komunikat za ogloszenie fokusu, a NVDACancelSpeech w
				// AnnounceSectionMoveMessage ucina tamto.
				int iLanded = GetCurrentMarkdownSectionHeadingIndex(headings, iTarget);
				string sMessage = "";
				if (iLanded >= 0) sMessage = GetMarkdownSectionHeadingTitle(headings[iLanded]) + ", heading " + headings[iLanded].Level;
				else sMessage = rtb.RowText;
				string sDeferred = sMessage;
				try {
				App.Frame.BeginInvoke((MethodInvoker) delegate {
				try {AnnounceSectionMoveMessage(sDeferred);} catch {}
				});
				}
				catch {AnnounceSectionMoveMessage(sMessage);}
				} // ShowDocumentNavigationTree method

				private static int GetCurrentMarkdownSectionHeadingIndex(List<MarkdownSectionHeading> headings, int iCurrent) {
				int iHeading = -1;
				for (int i = 0; i < headings.Count; i++) {
				if (headings[i].Start <= iCurrent) iHeading = i;
				else break;
				}
				return iHeading;
				} // GetCurrentMarkdownSectionHeadingIndex method

				private static int GetPreviousSiblingHeadingIndex(List<MarkdownSectionHeading> headings, int iHeading) {
				int iLevel = headings[iHeading].Level;
				for (int i = iHeading - 1; i >= 0; i--) {
				if (headings[i].Level < iLevel) break;
				if (headings[i].Level == iLevel) return i;
				}
				return -1;
				} // GetPreviousSiblingHeadingIndex method

				private static int GetNextSiblingHeadingIndex(List<MarkdownSectionHeading> headings, int iHeading) {
				int iLevel = headings[iHeading].Level;
				for (int i = iHeading + 1; i < headings.Count; i++) {
				if (headings[i].Level < iLevel) break;
				if (headings[i].Level == iLevel) return i;
				}
				return -1;
				} // GetNextSiblingHeadingIndex method

				private static int GetMarkdownSectionEnd(string sText, List<MarkdownSectionHeading> headings, int iHeading) {
				int iLevel = headings[iHeading].Level;
				for (int i = iHeading + 1; i < headings.Count; i++) {
				if (headings[i].Level <= iLevel) return headings[i].Start;
				}
				return sText.Length;
				} // GetMarkdownSectionEnd method

				private static string GetMarkdownSectionHeadingTitle(MarkdownSectionHeading heading) {
				if (heading == null || String.IsNullOrEmpty(heading.Title)) return "section";
				return heading.Title;
				} // GetMarkdownSectionHeadingTitle method

				// Etykieta jednej pozycji w drzewie Document Navigation (F6).
				//
				// POZIOM NAGLOWKA W ETYKIECIE (Kasperczak, Telegram 26.08.2026,
				// zlecenia 1787700133929-5 i 1787700193547-6). Zglosil, ze czytnik
				// oglasza w drzewie GLEBOKOSC GALEZI ("poziom 1" na naglowku drugiego
				// poziomu) i ze go to myli: "wolalbym zeby w tym drzewie mowil
				// prawdziwe naglowki a pomijal to, co mowi windows w dostepnosci w
				// poziomie drzewa". Glebokosci galezi nie da sie wyciszyc po naszej
				// stronie - oglasza ja czytnik z samej struktury drzewa - ale mozemy
				// dolozyc PRAWDZIWY poziom naglowka do tekstu pozycji, a on ten
				// wariant wybral wprost: "tresc naglowek dwa pierwszy poziom moze byc".
				//
				// Kolejnosc "tytul, heading N" jest ta sama, co przy Control z
				// PageUp/PageDown i po Enter w tym drzewie, wiec oba sposoby dojscia
				// do naglowka brzmia identycznie (jego wybor z 14.08.2026: "tresc jako
				// tytul naglowka", poziom po tytule).
				//
				// Dopisek idzie na KONIEC, bo nawigacja literowa w drzewie dopasowuje
				// POCZATEK etykiety - poziom z przodu zabilby test 1.4.
				//
				// Osobna metoda, a nie wyrazenie w petli, zeby dalo sie zmierzyc
				// refleksja po zbudowanym pliku wykonywalnym: fokus klawiatury na tej
				// maszynie potrafi byc zajety przez cudze okno systemowe i wtedy
				// pomiar przez czytnik nie startuje.
				private static string GetDocumentNavigationNodeLabel(MarkdownSectionHeading heading) {
				if (heading == null) return "section";
				return GetMarkdownSectionHeadingTitle(heading) + ", heading " + heading.Level;
				} // GetDocumentNavigationNodeLabel method

				// Every command that understands a "section" as a Markdown heading reads
				// this list: Control+Enter (insert), Control+PageUp/PageDown and their
				// Shift variants (navigate), Control+Alt+Up/Down (move a section), and
				// the F6 Document Navigation tree.
				//
				// FENCE AWARENESS (added 19.08.2026, found by the F6 harness): a '#'
				// line inside a fenced code block is NOT a heading -- in a document about
				// Markdown, shell scripts, or INI files, '# something' inside ``` is a
				// comment or an example. Without this the tree listed phantom headings
				// and every heading navigation command stopped on them. The fence ranges
				// come from MarkdownReview_FindFenceRanges, the SAME helper the preview
				// uses, so the outline and the preview cannot disagree.

// Skok po linku WEWNETRZNYM spod kursora, uzywany przez Enter w podgladzie
// Markdown.  Kotwica jest liczona tak samo jak przy tworzeniu spisu, wiec
// dziala na kazdym linku "#cos", nie tylko na naszym spisie.  Zwraca false,
// gdy pod kursorem nie ma linku wewnetrznego - wtedy dziala stara sciezka
// otwierania adresu w przegladarce.
private bool TryGoToInternalAnchorAtCursor(MdiChild child) {
if (child == null) return false;
HomerRichTextBox rtb = child.RTB;
if (rtb == null) return false;

string sAnchor = "";
try {
string sLine = rtb.RowText ?? "";
int iOffset = rtb.SelectionStart - rtb.RowStart;
if (iOffset < 0) iOffset = 0;
foreach (Match m in MarkdownInlineLinkRegex.Matches(sLine)) {
if (m == null || !m.Success) continue;
if (m.Index > iOffset || iOffset > m.Index + m.Length) continue;
string sDest = m.Groups["url"].Value.Trim();
if (!sDest.StartsWith("#")) continue;
sAnchor = sDest.Substring(1).Trim();
break;
}
}
catch {return false;}
if (sAnchor.Length == 0) return false;

string sText = rtb.Text ?? "";
int iBlockStart, iBlockEnd;
bool bHave = TryGetMarkdownContentsBlock(sText, out iBlockStart, out iBlockEnd);
List<MarkdownSectionHeading> headings = GetMarkdownHeadingsOutsideContents(sText, bHave ? iBlockStart : -1, iBlockEnd);
List<string> anchors = BuildMarkdownAnchors(headings);
for (int i = 0; i < anchors.Count; i++) {
if (anchors[i].Length == 0) continue;
if (!String.Equals(anchors[i], sAnchor, StringComparison.OrdinalIgnoreCase)) continue;
try {rtb.Select(headings[i].Start, 0);} catch {}
try {if (Win32.IsNVDAActive()) Win32.NVDACancelSpeech();} catch {}
AddMessage(GetMarkdownSectionHeadingTitle(headings[i]) + ", heading " + headings[i].Level);
return true;
}

AddMessage("No heading for that link!");
return true;
} // TryGoToInternalAnchorAtCursor method

// PRZYPIS W PODGLADZIE POD ESCAPE (zlecenie 1787840875741-1).
//
// Kasperczak: "nie powinny one w Podgladzie dzialac jak tez takie skip linki,
// czyli on mowi numer przypisu, na tym numerze przypisu naciskam Enter albo
// Spacje, on przeskakuje do tresci przypisu (...) i wraca do tego tekstu".
// Na pytanie, czy kursor ma sie PRZENIESC, czy zostac w zdaniu, odpowiedzial
// 27.08 16:56: "chyba jednak bardziej naturalnie przeniesc".  Wiec skok, nie
// rozwijanie na miejscu - i to jest jego decyzja, nie moja rekomendacja.
//
// DLACZEGO OSOBNA METODA, A NIE WYWOLANIE GoToMarkdownFootnoteOrBack:
// w podgladzie kursor stoi w DRUGIEJ kontrolce (MarkdownReviewView) i to ona
// ma fokus.  Ustawienie rtb.Index samo nie przeniesie widoku, wiec po skoku
// trzeba przeliczyc pozycje widoku z mapy zrodlo->widok.  Do tego mowa musi
// isc przez AddMessage z wymuszeniem, bo Util.Say bywa gubione, gdy fokus
// jest w podgladzie (ta sama lekcja co przy skoku spisem tresci).
//
// Zwraca false, gdy pod kursorem nie ma ani znacznika, ani tresci przypisu -
// wtedy Enter idzie dalej stara sciezka (spis tresci, potem adres).
private bool TryGoToFootnoteInReview(MdiChild child) {
if (child == null) return false;
HomerRichTextBox rtb = child.RTB;
if (rtb == null) return false;
if (!MarkdownReview_IsMarkdownFile(child.File)) return false;

string sText = rtb.Text ?? "";
List<MarkdownFootnote> refs = GetMarkdownFootnoteRefs(sText);
List<MarkdownFootnote> defs = GetMarkdownFootnoteDefs(sText);
if (refs.Count == 0 && defs.Count == 0) return false;

int iCursor = rtb.SelectionStart;
if (iCursor < 0) iCursor = 0;
if (iCursor > sText.Length) iCursor = sText.Length;

// Z TRESCI PRZYPISU WRACAMY DO ZDANIA.  Sprawdzane pierwsze, bo wiersz
// tresci sam zawiera znacznik na poczatku i inaczej skakalibysmy w miejscu.
string sLine = GetTextLineAtIndex(sText, iCursor);
string sLabel, sBody;
if (IsMarkdownFootnoteDefinitionLine(sLine, out sLabel, out sBody)) {
int iRef = FindMarkdownFootnoteByLabel(refs, sLabel);
if (iRef < 0) {
MarkdownReview_SayInView("Footnote " + sLabel + " has no marker in the text!");
return true;
}
MarkdownReview_GoToSourceIndex(child, refs[iRef].Start);
// CYTOWANE ZDANIE BEZ SUROWEJ SKLADNI (znalezione pomiarem NVDA, iteracja 3):
// przy powrocie slychac bylo "Zdanie z przypisem[^1] w tresci., footnote 1 in
// text", czyli w podgladzie, gdzie wlasnie usunelismy nawias i daszek, ten sam
// znacznik wracal w komunikacie.  Podmieniamy go na TEN SAM napis, ktory widzi
// czytnik w tekscie, wiec mowa i widok mowia to samo.
string sRefLine = GetTextLineAtIndex(sText, refs[iRef].Start).Trim();
try {sRefLine = MarkdownFootnoteRefRegex.Replace(sRefLine, MarkdownReview_FootnoteMarkerReplacement);} catch {}
if (sRefLine.Length == 0) sRefLine = "Footnote " + refs[iRef].Label;
MarkdownReview_SayInView(sRefLine + ", footnote " + refs[iRef].Label + " in text");
return true;
}

// ZE ZDANIA IDZIEMY DO TRESCI.  Ten sam helper co Alt+F6 w edytorze, wiec
// wystarczy stac gdziekolwiek w wierszu ze znacznikiem.
int iHere = FindMarkdownFootnoteRefAtIndex(refs, sText, iCursor);
if (iHere < 0) return false;
int iDef = FindMarkdownFootnoteByLabel(defs, refs[iHere].Label);
if (iDef < 0) {
MarkdownReview_SayInView("Footnote " + refs[iHere].Label + " has no text at the end of the document!");
return true;
}
MarkdownReview_GoToSourceIndex(child, defs[iDef].Start);
MarkdownReview_SayInView(defs[iDef].Text + ", footnote " + defs[iDef].Label);
return true;
} // TryGoToFootnoteInReview method

// Przeniesienie kursora w podgladzie: pozycje liczymy w ZRODLE, a ustawiamy w
// OBU kontrolkach, bo fokus jest w widoku i sama zmiana rtb.Index nie
// przewinelaby tego, co czlowiek widzi i slyszy.
private void MarkdownReview_GoToSourceIndex(MdiChild child, int iSourceIndex) {
if (child == null) return;
HomerRichTextBox rtb = child.RTB;
if (rtb == null) return;
try {
if (iSourceIndex < 0) iSourceIndex = 0;
if (iSourceIndex > rtb.TextLength) iSourceIndex = rtb.TextLength;
rtb.Select(iSourceIndex, 0);
}
catch {}

try {
if (child.MarkdownReviewView == null || child.MarkdownReviewView.IsDisposed) return;
int[] aSourceToView = child.MarkdownReviewSourceToView;
int iViewIndex = iSourceIndex;
if (aSourceToView != null && iSourceIndex < aSourceToView.Length) iViewIndex = aSourceToView[iSourceIndex];
if (iViewIndex < 0) iViewIndex = 0;
if (iViewIndex > child.MarkdownReviewView.TextLength) iViewIndex = child.MarkdownReviewView.TextLength;
child.MarkdownReviewView.Select(iViewIndex, 0);
child.MarkdownReviewView.ScrollToCaret();
}
catch {}
} // MarkdownReview_GoToSourceIndex method

// Mowa po skoku w podgladzie.  Kasowanie biezacej wypowiedzi jest tu
// konieczne, bo przesuniecie kursora w widoku samo wywoluje czytanie wiersza
// i nasz komunikat przepadlby pod nim - ta sama pulapka co przy skoku spisem
// tresci (zlecenie 1787793592380-0).
private void MarkdownReview_SayInView(string sMessage) {
if (String.IsNullOrEmpty(sMessage)) return;
try {if (Win32.IsNVDAActive()) Win32.NVDACancelSpeech();} catch {}
AddMessage(sMessage, true);
} // MarkdownReview_SayInView method


// ===================== MARKDOWNOWY SPIS TRESCI ==========================
// Alt+Shift+T tworzy spis u gory dokumentu, Shift+F6 skacze kontekstowo
// w obie strony.  Ustalenia Kasperczaka (Telegram 26.08.2026 22:26-23:07):
//   - spis obejmuje WSZYSTKIE poziomy naglowkow, bez wybierania zakresu,
//     "nigdy nie wiadomo, jaka kto tak naprawde zastosuje hierarchie",
//   - pozycje sa LINKAMI WEWNETRZNYMI, ktore dzialaja TAKZE PO EKSPORCIE:
//     "on chce klikac pozycje spisu w wyeksportowanym dokumencie",
//   - Shift+F6 z tresci idzie do spisu, a ze spisu do rozdzialu.
//
// DLACZEGO KOTWICA W TEJ POSTACI: pozycja to [tytul](#kotwica), a kotwica
// powstaje tak, jak liczy ja pandoc dla gfm - male litery, znaki inne niz
// litera, cyfra, minus i podkreslenie usuniete, kazda spacja na minus,
// powtorki z sufiksem -1, -2.  Zmierzone na naszym pandoc 3.10.2 z katalogu
// Convert: eksport md -> docx zamienia te kotwice na ZAKLADKI Worda
// (bookmarkStart o tej samej nazwie) i pozycje spisu staja sie klikalnymi
// hiperlaczami w Wordzie, a w HTML to zwykle <a href="#...">.  Gdyby
// kotwica liczona byla inaczej, link po eksporcie prowadzilby w nicosc.
private static string GetMarkdownAnchorText(string sTitle) {
if (String.IsNullOrEmpty(sTitle)) return "";
string sPlain = GetMarkdownPlainText(sTitle).ToLowerInvariant();
StringBuilder sb = new StringBuilder();
foreach (char c in sPlain) {
if (Char.IsLetterOrDigit(c)) sb.Append(c);
else if (c == '-' || c == '_') sb.Append(c);
else if (c == ' ') sb.Append('-');
}
return sb.ToString();
} // GetMarkdownAnchorText method

// Tytul naglowka bez znacznikow Markdown, zeby pozycja spisu czytala sie
// jak zdanie, a nie jak kod: [tekst](adres) zostawia tekst, a gwiazdki,
// grawisy i tyldy znikaja.  Nawiasu kwadratowego nie wolno przepuscic do
// etykiety linku, bo rozwalilby sama pozycje spisu.
private static string GetMarkdownPlainText(string sTitle) {
if (String.IsNullOrEmpty(sTitle)) return "";
string s = sTitle;
try {s = MarkdownInlineLinkRegex.Replace(s, "${text}");} catch {}
try {s = Regex.Replace(s, @"<(https?://[^>]+)>", "$1");} catch {}
s = s.Replace("`", "").Replace("*", "").Replace("~", "");
s = s.Replace("[", "").Replace("]", "");
try {s = Regex.Replace(s, @"\s+", " ").Trim();} catch {s = s.Trim();}
return s;
} // GetMarkdownPlainText method

// Czy wiersz jest POZYCJA spisu tresci, czyli punktem listy, ktorego cala
// tresc to link wewnetrzny.  Rozpoznajemy tez liste numerowana i gwiazdke,
// bo user moze spis przerobic recznie, a Shift+F6 ma dzialac dalej.
private static readonly Regex MarkdownContentsEntryRegex = new Regex(
@"^\s*(?:[-*+]|\d+[.)])\s+\[(?<label>[^\]]*)\]\(#(?<anchor>[^)]*)\)\s*$",
RegexOptions.CultureInvariant);

private static bool IsMarkdownContentsEntryLine(string sLine) {
if (String.IsNullOrEmpty(sLine)) return false;
try {return MarkdownContentsEntryRegex.IsMatch(sLine);} catch {return false;}
} // IsMarkdownContentsEntryLine method

// Granice bloku spisu tresci w dokumencie.  Blok to ciag pozycji spisu, a
// jesli bezposrednio nad nim (dopuszczalna jedna pusta linia) stoi naglowek
// o tytule "Contents", to nalezy do bloku i tez sie odswieza.  Naglowka
// o INNYM tytule nie ruszamy: to moglby byc tekst uzytkownika.
private static bool TryGetMarkdownContentsBlock(string sText, out int iBlockStart, out int iBlockEnd) {
iBlockStart = -1;
iBlockEnd = -1;
if (String.IsNullOrEmpty(sText)) return false;

List<int> lineStarts = new List<int>();
List<int> lineEnds = new List<int>();
List<string> lines = new List<string>();
int iScan = 0;
while (iScan <= sText.Length) {
int iNewLine = sText.IndexOf('\n', iScan);
int iEnd = (iNewLine >= 0) ? iNewLine : sText.Length;
string sLine = sText.Substring(iScan, iEnd - iScan);
if (sLine.EndsWith("\r")) sLine = sLine.Substring(0, sLine.Length - 1);
lineStarts.Add(iScan);
lineEnds.Add((iNewLine >= 0) ? iNewLine + 1 : sText.Length);
lines.Add(sLine);
if (iNewLine < 0) break;
iScan = iNewLine + 1;
}

int iFirst = -1;
List<int[]> fences = MarkdownReview_FindFenceRanges(sText);
for (int i = 0; i < lines.Count; i++) {
bool bInFence = false;
for (int j = 0; j < fences.Count; j++) {
if (lineStarts[i] >= fences[j][0] && lineStarts[i] < fences[j][1]) {bInFence = true; break;}
}
if (bInFence) continue;
if (IsMarkdownContentsEntryLine(lines[i])) {iFirst = i; break;}
}
if (iFirst < 0) return false;

int iLast = iFirst;
while (iLast + 1 < lines.Count && IsMarkdownContentsEntryLine(lines[iLast + 1])) iLast++;

int iTop = iFirst;
int iAbove = iFirst - 1;
if (iAbove >= 0 && lines[iAbove].Trim().Length == 0) iAbove--;
if (iAbove >= 0) {
int iLevel;
string sTitle;
if (TryGetMarkdownSectionHeadingLine(lines[iAbove], out iLevel, out sTitle) &&
String.Equals(GetMarkdownPlainText(sTitle), "Contents", StringComparison.OrdinalIgnoreCase)) {
iTop = iAbove;
}
}

int iBottom = iLast;
if (iBottom + 1 < lines.Count && lines[iBottom + 1].Trim().Length == 0) iBottom++;

iBlockStart = lineStarts[iTop];
iBlockEnd = lineEnds[iBottom];
return true;
} // TryGetMarkdownContentsBlock method

// Naglowki dokumentu BEZ tych, ktore naleza do samego spisu tresci -
// inaczej odswiezenie spisu wpisywalo by do niego pozycje "Contents".
private List<MarkdownSectionHeading> GetMarkdownHeadingsOutsideContents(string sText, int iBlockStart, int iBlockEnd) {
List<MarkdownSectionHeading> all = GetMarkdownSectionHeadings(sText);
List<MarkdownSectionHeading> outside = new List<MarkdownSectionHeading>();
foreach (MarkdownSectionHeading h in all) {
if (iBlockStart >= 0 && h.Start >= iBlockStart && h.Start < iBlockEnd) continue;
outside.Add(h);
}
return outside;
} // GetMarkdownHeadingsOutsideContents method

// Kotwice dla wszystkich naglowkow naraz, z sufiksem powtorki jak w pandoc:
// dwa rozdzialy o tej samej nazwie dostaja "tytul" i "tytul-1".
private static List<string> BuildMarkdownAnchors(List<MarkdownSectionHeading> headings) {
List<string> anchors = new List<string>();
Dictionary<string, int> used = new Dictionary<string, int>();
foreach (MarkdownSectionHeading h in headings) {
string sBase = GetMarkdownAnchorText(GetMarkdownSectionHeadingTitle(h));
// PUSTA kotwica zostaje pusta i pozycja spisu idzie BEZ linku.  Naglowek
// zlozony z samych znakow przestankowych ("## !!!") nie dostaje u pandoca
// zadnego celu (zmierzone na pandoc 3.10.2 z katalogu Convert), wiec
// jakakolwiek kotwica wymyslona tu przez nas prowadzilaby w nicosc.
// Pozycja bez linku jest uczciwsza niz link, ktory nie dziala.
if (sBase.Length == 0) {
anchors.Add("");
continue;
}
string sAnchor = sBase;
if (used.ContainsKey(sBase)) {
used[sBase] = used[sBase] + 1;
sAnchor = sBase + "-" + used[sBase];
}
else used[sBase] = 0;
anchors.Add(sAnchor);
}
return anchors;
} // BuildMarkdownAnchors method

// Tresc spisu: naglowek "Contents" i lista wciecana wedlug poziomow, zeby
// hierarchia byla widoczna takze po eksporcie (pandoc robi z tego liste
// zagniezdzona).  Poziom najwyzszy w dokumencie jest poziomem zerowym
// wciecia, wiec plik zaczynajacy sie od naglowka drugiego poziomu nie
// dostaje pustego wciecia na starcie.
private static string BuildMarkdownContentsText(List<MarkdownSectionHeading> headings, List<string> anchors) {
int iMin = 6;
foreach (MarkdownSectionHeading h in headings) if (h.Level < iMin) iMin = h.Level;
if (iMin < 1) iMin = 1;

StringBuilder sb = new StringBuilder();
sb.Append("# Contents");
sb.Append(LF);
sb.Append(LF);
for (int i = 0; i < headings.Count; i++) {
int iDepth = headings[i].Level - iMin;
if (iDepth < 0) iDepth = 0;
sb.Append(new String(' ', iDepth * 2));
string sLabel = GetMarkdownPlainText(GetMarkdownSectionHeadingTitle(headings[i]));
if (sLabel.Length == 0) sLabel = GetMarkdownSectionHeadingTitle(headings[i]);
if (anchors[i].Length == 0) {
// Bez celu do skoku: sama tresc pozycji, zeby spis byl kompletny, a
// zaden odsylacz nie obiecywal skoku, ktorego nie da sie wykonac.
sb.Append("- ");
sb.Append(sLabel);
}
else {
sb.Append("- [");
sb.Append(sLabel);
sb.Append("](#");
sb.Append(anchors[i]);
sb.Append(")");
}
sb.Append(LF);
}
sb.Append(LF);
return sb.ToString();
} // BuildMarkdownContentsText method

private void WriteMarkdownContents(HomerRichTextBox rtb) {
if (rtb == null) return;
string sText = rtb.Text ?? "";
int iBlockStart, iBlockEnd;
bool bHave = TryGetMarkdownContentsBlock(sText, out iBlockStart, out iBlockEnd);
List<MarkdownSectionHeading> headings = GetMarkdownHeadingsOutsideContents(sText, bHave ? iBlockStart : -1, iBlockEnd);
if (headings.Count == 0) {
AddMessage("No headings!");
return;
}

List<string> anchors = BuildMarkdownAnchors(headings);
string sContents = BuildMarkdownContentsText(headings, anchors);

if (bHave) {
rtb.ReplaceRange(iBlockStart, iBlockEnd, sContents);
rtb.Index = iBlockStart;
AddMessage("Contents updated, " + Util.Pluralize(headings.Count, "item"));
}
else {
rtb.ReplaceRange(0, 0, sContents);
rtb.Index = 0;
AddMessage("Contents, " + Util.Pluralize(headings.Count, "item"));
}
rtb.Modified = true;
} // WriteMarkdownContents method

// Shift+F6.  Jedna komenda, dwa kierunki, wybierane po tym, GDZIE stoi
// kursor - decyzja Kasperczaka.  Komunikaty tez sa jego: gdy w dokumencie
// nie ma naglowkow, mowimy krotko; gdy naglowki SA, a spisu jeszcze nie ma,
// komunikat podpowiada klawisz tworzenia spisu.
private void GoToMarkdownContentsOrHeading(HomerRichTextBox rtb) {
if (rtb == null) return;
string sText = rtb.Text ?? "";
int iBlockStart, iBlockEnd;
bool bHave = TryGetMarkdownContentsBlock(sText, out iBlockStart, out iBlockEnd);
List<MarkdownSectionHeading> headings = GetMarkdownHeadingsOutsideContents(sText, bHave ? iBlockStart : -1, iBlockEnd);
int iCurrent = rtb.Index;

if (bHave && iCurrent >= iBlockStart && iCurrent < iBlockEnd) {
GoFromContentsEntryToHeading(rtb, sText, headings, iCurrent);
return;
}

if (!bHave) {
if (headings.Count == 0) AddMessage("No headings!");
else AddMessage("No contents yet, press Alt+Shift+T to create it!");
return;
}

GoFromHeadingToContentsEntry(rtb, sText, headings, iBlockStart, iBlockEnd, iCurrent);
} // GoToMarkdownContentsOrHeading method

private void GoFromContentsEntryToHeading(HomerRichTextBox rtb, string sText, List<MarkdownSectionHeading> headings, int iCurrent) {
string sLine = GetTextLineAtIndex(sText, iCurrent);
Match m = null;
try {m = MarkdownContentsEntryRegex.Match(sLine);} catch {}
if (m == null || !m.Success) {
AddMessage("Put the cursor on a contents item!");
return;
}

string sAnchor = m.Groups["anchor"].Value.Trim();
List<string> anchors = BuildMarkdownAnchors(headings);
for (int i = 0; i < anchors.Count; i++) {
if (anchors[i].Length == 0) continue;
if (!String.Equals(anchors[i], sAnchor, StringComparison.OrdinalIgnoreCase)) continue;
rtb.Index = headings[i].Start;
// Ta sama forma wypowiedzi co przy Control z PageUp i PageDown oraz po
// Enter w drzewie F6, wiec kazde dojscie do naglowka brzmi identycznie.
Util.Say(GetMarkdownSectionHeadingTitle(headings[i]) + ", heading " + headings[i].Level);
return;
}
AddMessage("That heading is gone, press Alt+Shift+T to refresh the contents!");
} // GoFromContentsEntryToHeading method

private void GoFromHeadingToContentsEntry(HomerRichTextBox rtb, string sText, List<MarkdownSectionHeading> headings, int iBlockStart, int iBlockEnd, int iCurrent) {
int iHere = GetCurrentMarkdownSectionHeadingIndex(headings, iCurrent);
string sWant = "";
if (iHere >= 0) {
List<string> anchors = BuildMarkdownAnchors(headings);
// Pusta kotwica znaczy pozycje bez linku - wtedy nie mamy czego szukac
// i ladujemy na poczatku spisu, zamiast trafiac w pierwsza z brzegu.
sWant = anchors[iHere];
}

if (sWant.Length > 0) {
int iScan = iBlockStart;
while (iScan < iBlockEnd && iScan <= sText.Length) {
int iNewLine = sText.IndexOf('\n', iScan);
int iEnd = (iNewLine >= 0 && iNewLine < iBlockEnd) ? iNewLine : Math.Min(iBlockEnd, sText.Length);
string sLine = sText.Substring(iScan, iEnd - iScan);
if (sLine.EndsWith("\r")) sLine = sLine.Substring(0, sLine.Length - 1);
Match m = null;
try {m = MarkdownContentsEntryRegex.Match(sLine);} catch {}
if (m != null && m.Success && String.Equals(m.Groups["anchor"].Value.Trim(), sWant, StringComparison.OrdinalIgnoreCase)) {
rtb.Index = iScan;
Util.Say(m.Groups["label"].Value.Trim() + ", contents");
return;
}
if (iNewLine < 0) break;
iScan = iNewLine + 1;
}
}

rtb.Index = iBlockStart;
Util.Say("Contents");
} // GoFromHeadingToContentsEntry method

// PRZYPISY MARKDOWNOWE (zlecenie 1787832545532-3, ustalenia Kasperczaka z
// rozmowy 27.08.2026 13:51-14:04).  Znacznik [^1] stoi w zdaniu, a tresc
// przypisu [^1]: ... na koncu dokumentu.  Skladni NIE wymyslamy: nasz wlasny
// konwerter Convert/2htm (Markdig z UseFootnotes) juz ja rozumie i produkuje
// z niej dostepne odsylacze w obie strony, wiec kazdy inny zapis rozjechalby
// edytor z wlasnym eksportem.  Numer nadajemy automatycznie, po najwyzszym
// numerze uzytym w dokumencie - Markdown numeruje przypisy w kolejnosci
// wystapienia w tekscie, wiec etykiety NIE musza byc uporzadkowane i nic nie
// trzeba przenumerowywac po wstawieniu w srodku.
private class MarkdownFootnote {
public int Start;
public int End;
public string Label;
public string Text;
} // MarkdownFootnote class

// Znacznik przypisu w tekscie.  Etykieta bez bialych znakow i bez nawiasu,
// zeby zwykly nawias kwadratowy w zdaniu nie udawal przypisu.
private static readonly Regex MarkdownFootnoteRefRegex = new Regex(
@"\[\^(?<label>[^\]\s\^]+)\]",
RegexOptions.CultureInvariant);

// Tresc przypisu: wiersz zaczynajacy sie od etykiety z dwukropkiem.  Do trzech
// spacji wciecia, tak jak liczy je Markdown.
private static readonly Regex MarkdownFootnoteDefRegex = new Regex(
@"^[ \t]{0,3}\[\^(?<label>[^\]\s\^]+)\]:[ \t]*(?<text>.*)$",
RegexOptions.CultureInvariant);

private static bool IsMarkdownFootnoteDefinitionLine(string sLine, out string sLabel, out string sBody) {
sLabel = "";
sBody = "";
if (String.IsNullOrEmpty(sLine)) return false;
Match m = null;
try {m = MarkdownFootnoteDefRegex.Match(sLine);} catch {return false;}
if (m == null || !m.Success) return false;
sLabel = m.Groups["label"].Value.Trim();
sBody = m.Groups["text"].Value.Trim();
return sLabel.Length > 0;
} // IsMarkdownFootnoteDefinitionLine method

private static bool IsMarkdownIndexInFence(List<int[]> fences, int iIndex) {
if (fences == null) return false;
for (int i = 0; i < fences.Count; i++) {
if (iIndex >= fences[i][0] && iIndex < fences[i][1]) return true;
}
return false;
} // IsMarkdownIndexInFence method

// Znaczniki przypisow W TEKSCIE.  Blok kodu pomijamy tym samym parserem
// ogrodzen co naglowki, bo w przykladzie kodu [^1] to nie przypis.  Wiersz
// z trescia przypisu tez nie jest znacznikiem, choc pasuje do wzorca.
private static List<MarkdownFootnote> GetMarkdownFootnoteRefs(string sText) {
List<MarkdownFootnote> notes = new List<MarkdownFootnote>();
if (String.IsNullOrEmpty(sText)) return notes;
List<int[]> fences = MarkdownReview_FindFenceRanges(sText);
MatchCollection ms = null;
try {ms = MarkdownFootnoteRefRegex.Matches(sText);} catch {return notes;}
foreach (Match m in ms) {
if (IsMarkdownIndexInFence(fences, m.Index)) continue;
string sLine = GetTextLineAtIndex(sText, m.Index);
string sLabel, sBody;
if (IsMarkdownFootnoteDefinitionLine(sLine, out sLabel, out sBody)) {
// Wiersz jest trescia przypisu; znacznikiem jest tylko wtedy, gdy w tym
// samym wierszu stoi DRUGI nawias, dalej niz sama etykieta na poczatku.
int iLineStart = GetTextLineStartAtIndex(sText, m.Index);
int iDefLabelEnd = iLineStart + sLine.IndexOf(']') + 1;
if (m.Index < iDefLabelEnd) continue;
}
MarkdownFootnote note = new MarkdownFootnote();
note.Start = m.Index;
note.End = m.Index + m.Length;
note.Label = m.Groups["label"].Value.Trim();
note.Text = "";
notes.Add(note);
}
return notes;
} // GetMarkdownFootnoteRefs method

// Tresci przypisow, czyli wiersze [^etykieta]: tresc.  Poza blokami kodu.
private static List<MarkdownFootnote> GetMarkdownFootnoteDefs(string sText) {
List<MarkdownFootnote> notes = new List<MarkdownFootnote>();
if (String.IsNullOrEmpty(sText)) return notes;
List<int[]> fences = MarkdownReview_FindFenceRanges(sText);
int iStart = 0;
while (iStart <= sText.Length) {
int iNewLine = sText.IndexOf('\n', iStart);
int iEnd = (iNewLine >= 0) ? iNewLine : sText.Length;
string sLine = sText.Substring(iStart, iEnd - iStart);
if (sLine.EndsWith("\r")) sLine = sLine.Substring(0, sLine.Length - 1);
string sLabel, sBody;
if (!IsMarkdownIndexInFence(fences, iStart) && IsMarkdownFootnoteDefinitionLine(sLine, out sLabel, out sBody)) {
MarkdownFootnote note = new MarkdownFootnote();
note.Start = iStart;
note.End = iStart + sLine.Length;
note.Label = sLabel;
note.Text = sBody;
notes.Add(note);
}
if (iNewLine < 0) break;
iStart = iNewLine + 1;
}
return notes;
} // GetMarkdownFootnoteDefs method

// Nastepny numer: o jeden wyzszy od najwyzszego LICZBOWEGO, jaki wystepuje w
// dokumencie, liczac i znaczniki, i tresci.  Etykiety nieliczbowe (uzytkownik
// moze wpisac swoje) zostawiamy w spokoju, tylko ich nie dublujemy.
private static string GetNextMarkdownFootnoteLabel(string sText) {
int iMax = 0;
List<MarkdownFootnote> all = new List<MarkdownFootnote>();
all.AddRange(GetMarkdownFootnoteRefs(sText));
all.AddRange(GetMarkdownFootnoteDefs(sText));
foreach (MarkdownFootnote note in all) {
int iValue;
if (Int32.TryParse(note.Label, out iValue) && iValue > iMax) iMax = iValue;
}
return (iMax + 1).ToString();
} // GetNextMarkdownFootnoteLabel method

// Tresc przypisu dopisywana na koniec dokumentu, zawsze oddzielona pusta
// linia od tego, co tam stoi - inaczej Markdown wciagnalby ja do ostatniego
// akapitu i przypis przestalby byc przypisem.
private static string BuildMarkdownFootnoteDefinition(string sText, string sLabel, string sContent) {
string sPrefix = "";
string s = sText ?? "";
if (s.Length > 0) {
if (!s.EndsWith("\n")) sPrefix = LF + LF;
else if (!s.EndsWith("\n\n") && !s.EndsWith("\n\r\n")) sPrefix = LF;
}
return sPrefix + "[^" + sLabel + "]: " + sContent + LF;
} // BuildMarkdownFootnoteDefinition method

// GDZIE WSTAWIC TRESC PRZYPISU, gdy user wybral koniec ROZDZIALU, a nie
// koniec dokumentu.  Zlecenie Kasperczaka z 29.08.2026 01:40: "moze byc tak,
// ze komus bedzie latwiej przypisy w danym rozdziale, a ma plik z iloma
// rozdzialami, ktore potem bedziemy eksportowac (...) albo zapisywac w
// osobnych plikach".  Czyli tresc musi zostac PRZY swoim rozdziale, bo inaczej
// po podzieleniu pliku znaczniki zostaja bez tresci.
//
// Zwraca offset, w ktory ma wejsc tresc, albo -1 gdy kursor nie stoi w zadnym
// rozdziale (dokument bez naglowkow, albo tekst przed pierwszym naglowkiem) -
// wtedy wolajacy spada na koniec dokumentu, bo "koniec rozdzialu" nie istnieje.
//
// KONIEC ROZDZIALU liczymy TYM SAMYM helperem, ktorego uzywa przestawianie
// sekcji (GetMarkdownSectionEnd), zeby oba rozumialy rozdzial identycznie.
// Cofamy sie przed puste wiersze na jego koncu: bez tego tresc przypisu
// wpadalaby ZA odstep oddzielajacy rozdzialy, czyli optycznie do nastepnego.
private static int GetMarkdownFootnoteChapterEnd(string sText, int iCursor) {
string s = sText ?? "";
List<MarkdownSectionHeading> headings = GetMarkdownSectionHeadings(s);
if (headings.Count == 0) return -1;

int iAt = iCursor;
if (iAt >= s.Length && s.Length > 0) iAt = s.Length - 1;
if (iAt < 0) iAt = 0;
int iHeading = GetCurrentMarkdownSectionHeadingIndex(headings, iAt);
if (iHeading < 0) return -1;

int iEnd = GetMarkdownSectionEnd(s, headings, iHeading);
if (iEnd > s.Length) iEnd = s.Length;
while (iEnd > headings[iHeading].Start && iEnd > 0
&& (s[iEnd - 1] == '\n' || s[iEnd - 1] == '\r')) iEnd--;
return iEnd;
} // GetMarkdownFootnoteChapterEnd method

// Tresc przypisu wstawiana W SRODEK dokumentu (koniec rozdzialu) potrzebuje
// odstepu z OBU stron, inaczej sklei sie z ostatnim akapitem rozdzialu albo z
// naglowkiem nastepnego - i w zadnym z tych przypadkow nie bylaby przypisem
// dla konwertera.  Na koncu dokumentu wystarczal odstep z jednej strony,
// dlatego to osobna metoda, a nie parametr tamtej.
private static string BuildMarkdownFootnoteDefinitionAt(string sText, int iAt, string sLabel, string sContent) {
string s = sText ?? "";
if (iAt < 0) iAt = 0;
if (iAt > s.Length) iAt = s.Length;

string sPrefix = LF + LF;
if (iAt == 0) sPrefix = "";
else if (s[iAt - 1] == '\n') {
sPrefix = LF;
if (iAt >= 2 && (s[iAt - 2] == '\n' || s[iAt - 2] == '\r')) sPrefix = "";
}

string sSuffix = LF;
if (iAt < s.Length) sSuffix = LF + LF;
return sPrefix + "[^" + sLabel + "]: " + sContent + sSuffix;
} // BuildMarkdownFootnoteDefinitionAt method

// Ktory znacznik jest "ten pod kursorem": najpierw taki, w ktorego obrebie
// kursor stoi, potem pierwszy w tym samym wierszu za kursorem, potem ostatni
// w tym wierszu przed kursorem.  Dzieki temu wystarczy stac gdziekolwiek w
// zdaniu ze znacznikiem, a nie dokladnie na nawiasie.
private static int FindMarkdownFootnoteRefAtIndex(List<MarkdownFootnote> refs, string sText, int iIndex) {
if (refs == null || refs.Count == 0) return -1;
for (int i = 0; i < refs.Count; i++) {
if (iIndex >= refs[i].Start && iIndex <= refs[i].End) return i;
}
int iLineStart = GetTextLineStartAtIndex(sText, iIndex);
int iLineEnd = (sText ?? "").IndexOf('\n', iLineStart);
if (iLineEnd < 0) iLineEnd = (sText ?? "").Length;
int iAfter = -1;
int iBefore = -1;
for (int i = 0; i < refs.Count; i++) {
if (refs[i].Start < iLineStart || refs[i].Start > iLineEnd) continue;
if (refs[i].Start >= iIndex) {if (iAfter < 0) iAfter = i;}
else iBefore = i;
}
if (iAfter >= 0) return iAfter;
return iBefore;
} // FindMarkdownFootnoteRefAtIndex method

private static int FindMarkdownFootnoteByLabel(List<MarkdownFootnote> notes, string sLabel) {
if (notes == null || sLabel == null) return -1;
for (int i = 0; i < notes.Count; i++) {
if (String.Equals(notes[i].Label, sLabel, StringComparison.OrdinalIgnoreCase)) return i;
}
return -1;
} // FindMarkdownFootnoteByLabel method

// Jak znacznik przypisu BRZMI w podgladzie pod Escape (zlecenie
// 1787840875741-1).  Kasperczak zglosil, ze przypisy w podgladzie maja
// dzialac "jak takie skip linki, czyli on mowi numer przypisu, na tym
// numerze przypisu naciskam Enter".  Surowe "[^1]" czytnik wymawia jako
// nawias, daszek i cyfre, wiec zamieniamy je na slowo - zaproponowane mu
// wprost w wiadomosci z 27.08 17:05 i spojne z tym, jak nazywamy przypisy
// w komunikatach edytora.
//
// Jedno zrodlo prawdy: TEN SAM napis uzywa render podgladu i sonda pomiaru,
// wiec nie moga sie rozjechac.
private static string MarkdownReview_FootnoteMarkerText(string sLabel) {
string s = (sLabel ?? "").Trim();
if (s.Length == 0) return "footnote";
// SPACJA NA POCZATKU (znaleziona pomiarem NVDA, iteracja 3): bez niej czytnik
// wymawial "przypisemfootnote 1" jednym slowem, bo w zdaniu znacznik przylega
// do wyrazu ("...przypisem[^1]").  Nawias i daszek same rozdzielaly wyrazy,
// wiec po zamianie na slowa trzeba ten rozdzielnik dolozyc jawnie.
return " footnote " + s;
} // MarkdownReview_FootnoteMarkerText method

// Podmiana znacznika w CYTOWANYM zdaniu, uzywana przy powrocie z tresci
// przypisu w podgladzie.  Osobna metoda, a nie wyrazenie lambda w miejscu
// uzycia, zeby dala sie zmierzyc refleksja na zbudowanej binarce.
private static string MarkdownReview_FootnoteMarkerReplacement(Match m) {
if (m == null) return "";
return MarkdownReview_FootnoteMarkerText(m.Groups["label"].Value);
} // MarkdownReview_FootnoteMarkerReplacement method

// Wstawienie przypisu.  Jego wlasny szkic: "okienko, wpisujesz tresc, Enter -
// przypis wstawiony, a Ty zostajesz w zdaniu".  Tresc leci na koniec
// dokumentu PIERWSZA, bo wtedy pozycja kursora jest jeszcze wazna, a znacznik
// wchodzi drugi, dokladnie tam, gdzie pisal.
// TABELE MARKDOWN - KREATOR (Control+Shift+T).  Cala forma jest wyborem
// Kasperczaka z rozmowy 28.08.2026 17:00-17:06 i celowo nasladuje arkusz
// kalkulacyjny, bo taki uklad zna kazdy uzytkownik czytnika ekranu:
//   - okno startuje z JEDNA komorka, bez pytania o wymiary ("pytanie o
//     wymiary z gory jest sztuczne, bo przy pisaniu rzadko sie to wie"),
//   - strzalka w prawo z OSTATNIEJ kolumny doklada kolumne, strzalka w dol
//     z OSTATNIEGO wiersza doklada wiersz, wiec tabela rosnie w miare
//     pisania ("Czyli co naciskam strzalke i od razu pisze"),
//   - pisanie na komorce od razu ZASTEPUJE jej tresc, a F2 otwiera do
//     poprawki to, co w niej jest, i Enter zatwierdza - doslownie jego
//     propozycja "F dwa pole edycyjne wpisuje i Enter tak jak w Excelu",
//   - Control+Enter wstawia gotowa tabele do dokumentu (jego propozycja,
//     spojna z Control+Enter = wstaw naglowek),
//   - Escape zamyka BEZ wstawiania, ale pyta o potwierdzenie, gdy cokolwiek
//     jest wpisane, zeby nie stracic pracy przez przypadek.
//
// KREATOR TABELI: WSTAWIANIE NOWEJ ALBO EDYCJA GOTOWEJ
//
// Ten sam skrot robi dwie rzeczy, bo dla uzytkownika to jedna czynnosc: praca
// nad tabela.  Rozstrzyga POLOZENIE KURSORA - stoi w gotowej tabeli, wiec ja
// otwieramy i podmieniamy na miejscu; stoi gdziekolwiek indziej, wiec
// wstawiamy nowa.  Tytul okna mowi ktory tryb ("Edit Table" albo "Insert
// Table"), bo czytnik czyta tytul przy otwarciu i to jest jedyne miejsce, w
// ktorym uzytkownik dowiaduje sie tego bez pytania.
//
// PIERWSZY WIERSZ SIATKI TO NAGLOWEK TABELY - nie jest to nasz wybor
// projektowy, tylko wymog skladni: tabela Markdown bez wiersza naglowka i
// bez wiersza kreskek nie jest tabela dla zadnego konwertera.  Dlatego
// naglowek tego wiersza mowi "Header", a kolejne "Row 1", "Row 2" - czytnik
// wymawia to przy kazdym przejsciu, wiec uzytkownik zawsze wie, gdzie jest.
//
// Puste kolumny i wiersze na KONCU sa przy wstawianiu ucinane: doklada sie je
// jednym nacisnieciem strzalki, wiec nadmiarowa kolumna zdarzy sie kazdemu,
// a martwej kolumny nie da sie inaczej cofnac.
// WSTAWIANIE LINKU (Control+K).  Ksztalt okna jest wprost z jego zlecenia:
// "okienko z adresem i tytulem, plus mozliwosc linku graficznego albo
// wewnetrznego (do kotwicy w dokumencie)".
//
// TRZY RODZAJE W JEDNYM OKNIE, nie trzy komendy - bo dla uzytkownika to jedna
// czynnosc: wstawiam odsylacz.  Rodzaj wybiera sie z listy rozwijanej, a nie
// osobnym klawiszem: link graficzny i wewnetrzny robi sie rzadko, a kazdy
// osobny klawisz to jeden mniej dla rzeczy czestych.
//
// PRZY LINKU WEWNETRZNYM NIE PYTAMY O ADRES, tylko dajemy LISTE NAGLOWKOW tego
// dokumentu.  Kotwice liczy BuildMarkdownAnchors - TA SAMA metoda, ktora
// wpisuje odsylacze do spisu tresci, wraz z jej obsluga powtorzonych tytulow
// ("tytul", "tytul-1").  Wlasnej implementacji specyfikacji tu nie ma i byc
// nie moze: kotwica policzona inaczej niz w spisie tresci prowadzilaby po
// eksporcie do Worda albo HTML w nicosc, a to jest dokladnie ta klasa bledu,
// ktorej niewidomy nie ma jak zobaczyc.
//
// Naglowek BEZ kotwicy (zlozony z samych znakow przestankowych) na liscie SIE
// NIE POJAWIA - pandoc nie daje mu celu, wiec link do niego bylby martwy.
// Gdy zadnego naglowka z kotwica nie ma, mowimy o tym i rodzaj wewnetrzny
// jest niedostepny, zamiast dawac puste okno wyboru.
//
// Zaznaczony tekst wchodzi do pola tresci jako domysl, bo najczestsza droga to
// "zaznacz slowo, zrob z niego link".
private void InsertMarkdownLink(HomerRichTextBox rtb) {
if (rtb == null) return;
string sText = rtb.Text ?? "";
int iSelStart = rtb.SelectionStart;
int iSelLength = rtb.SelectionLength;
string sSelected = (iSelLength > 0) ? (rtb.SelectedText ?? "").Trim() : "";

// Blok kodu: znacznik linku jest tam MARTWY (konwerter wypisuje go jako
// widoczny tekst przykladu), wiec komunikat "Link inserted" klamalby.  Ta
// sama bramka i ten sam powod, co przy przypisie i komentarzu.
int iCursor = (iSelLength > 0) ? iSelStart : rtb.Index;
if (iCursor < 0) iCursor = 0;
if (iCursor > sText.Length) iCursor = sText.Length;
List<int[]> fences = MarkdownReview_FindFenceRanges(sText);
if (IsMarkdownIndexInFence(fences, iCursor)) {
AddMessage("Cannot put a link inside a code block!");
return;
}

// Naglowki z NIEPUSTA kotwica - kandydaci na cel linku wewnetrznego.
List<MarkdownSectionHeading> headings = GetMarkdownSectionHeadings(sText);
List<string> anchors = BuildMarkdownAnchors(headings);
List<string> lAnchorValues = new List<string>();
List<string> lAnchorLabels = new List<string>();
for (int i = 0; i < headings.Count && i < anchors.Count; i++) {
if (anchors[i].Length == 0) continue;
string sHeadingLabel = GetMarkdownPlainText(GetMarkdownSectionHeadingTitle(headings[i]));
if (sHeadingLabel.Length == 0) sHeadingLabel = GetMarkdownSectionHeadingTitle(headings[i]);
lAnchorValues.Add(anchors[i]);
lAnchorLabels.Add(sHeadingLabel);
}
bool bInternalPossible = (lAnchorValues.Count > 0);

List<string> lKinds = new List<string>();
lKinds.Add("Web or file link");
lKinds.Add("Image");
if (bInternalPossible) lKinds.Add("Place in this document");

LbcDialog dlg = new LbcDialog("Insert Link", this);
TextBox txtLabel = dlg.addInputBox("Link &text", sSelected);
TextBox txtAddress = dlg.addInputBox("&Address", "");
ComboBox cboKind = dlg.addComboPickBox("Link &kind", lKinds, lKinds[0],
bInternalPossible ? "Image inserts a picture; place in this document jumps to a heading"
: "No heading with an anchor in this document, so an internal link is not offered");
ComboBox cboTarget = null;
if (bInternalPossible) cboTarget = dlg.addComboPickBox("&Heading to jump to",
lAnchorLabels, lAnchorLabels[0], "Used only when the kind is a place in this document");
if (!dlg.runOkCancel()) {
dlg.Dispose();
return;
}
string sLabelText = (txtLabel.Text ?? "").Trim();
string sAddress = (txtAddress.Text ?? "").Trim();
int iKind = cboKind.SelectedIndex;
int iTarget = (cboTarget == null) ? -1 : cboTarget.SelectedIndex;
dlg.Dispose();

bool bImage = (iKind == 1);
bool bInternal = (bInternalPossible && iKind == 2);

// Cel linku wewnetrznego bierzemy Z LISTY, nie z pola adresu: user nie ma
// prawa znac zasady liczenia kotwic, a my ja znamy.
if (bInternal) {
if (iTarget < 0 || iTarget >= lAnchorValues.Count) {
AddMessage("No heading picked, nothing inserted!");
return;
}
sAddress = "#" + lAnchorValues[iTarget];
if (sLabelText.Length == 0) sLabelText = lAnchorLabels[iTarget];
}

if (sAddress.Length == 0) {
AddMessage("No address, nothing inserted!");
return;
}

// Pusty opis: dla obrazka to tekst alternatywny, wiec brak jest bledem
// dostepnosci, o ktorym mowimy wprost - obrazek bez opisu jest dla
// niewidomego niewidzialny.  Przy zwyklym linku wstawiamy sam adres jako
// tresc, bo odsylacz musi cos mowic.
if (sLabelText.Length == 0) {
if (bImage) {
AddMessage("An image needs a description for screen reader users!");
return;
}
sLabelText = sAddress;
}

// Nawias kwadratowy w tresci rozwalilby sama skladnie odsylacza, a nawias
// okragly w adresie urwalby go w polowie.  Zamieniamy je, zamiast cicho
// wstawiac polamany link.
sLabelText = sLabelText.Replace("[", "(").Replace("]", ")");
sAddress = sAddress.Replace("(", "%28").Replace(")", "%29").Replace(" ", "%20");

string sMarkup = (bImage ? "!" : "") + "[" + sLabelText + "](" + sAddress + ")";
int iStart = (iSelLength > 0) ? iSelStart : iCursor;
int iEnd = (iSelLength > 0) ? (iSelStart + iSelLength) : iCursor;
rtb.ReplaceRange(iStart, iEnd, sMarkup);
rtb.Index = iStart + sMarkup.Length;
rtb.Modified = true;
AddMessage(bImage ? "Image inserted" : (bInternal ? "Internal link inserted" : "Link inserted"));
} // InsertMarkdownLink method

// ==================== LISTA LINKOW (Control+F6) ====================
//
// Ustalenia Kasperczaka edsharpng-37, edsharpng-38 i edsharpng-47: Control+F6
// to LISTA LINKOW w dokumencie, analogicznie do F6 dla naglowkow; na liscie
// Control+C kopiuje link jako Markdown, Control+Shift+C jako link z
// formatowaniem, a F2 otwiera okienko edycji tytulu i adresu.  Systemowy cykl
// okien MdiClient odebralismy temu klawiszowi w 5.0.52 - bez tego lista nie
// mialaby szansy sie otworzyc, bo WinForms bral chord przed nasza tablica.
private class MarkdownLink {
public int Start;      // pierwszy znak calego odsylacza (przy obrazku wykrzyknik)
public int End;        // za ostatnim znakiem odsylacza
public int TextStart;  // pierwszy znak TRESCI (przy golym adresie rowny Start)
public int TextEnd;    // za ostatnim znakiem tresci
public string Title;   // tresc odsylacza, przy golym adresie pusta
public string Url;     // adres znormalizowany, do mowy i do schowka
public bool Image;     // odsylacz do obrazka, czyli wykrzyknik przed nawiasem
public bool Inline;    // postac [tresc](adres); false dla golego adresu
} // MarkdownLink class

// Wszystkie odsylacze dokumentu, w kolejnosci wystapienia.  Zrodlem prawdy sa
// TE SAME dwa pomiary, ktore zasilaja liste elementow w podgladzie:
// MarkdownReview_FindInlineLinks dla postaci [tresc](adres) oraz trzy wyrazenia
// regularne na gole adresy.  Gdyby lista miala wlasny parser, obie funkcje
// rozjechaly by sie po pierwszej poprawce.
//
// Bloki kodu sa pomijane: tam adres jest przykladem w tekscie, a nie
// odsylaczem - ta sama bramka, co przy naglowkach, przypisach i komentarzach.
private static List<MarkdownLink> GetMarkdownLinks(string sText) {
List<MarkdownLink> links = new List<MarkdownLink>();
if (String.IsNullOrEmpty(sText)) return links;

List<int[]> inlineLinks = MarkdownReview_FindInlineLinks(sText);
foreach (int[] span in inlineLinks) {
if (span == null || span.Length < 6) continue;
MarkdownLink link = new MarkdownLink();
link.Start = span[0];
link.End = span[1];
link.TextStart = span[2];
link.TextEnd = span[3];
string sTitle = "";
string sUrlRaw = "";
try {sTitle = sText.Substring(span[2], span[3] - span[2]);} catch {sTitle = "";}
try {sUrlRaw = sText.Substring(span[4], span[5] - span[4]);} catch {sUrlRaw = "";}
link.Title = MarkdownReview_CollapseWhitespace(sTitle);
string sUrl = MarkdownReview_NormalizeUrl(sUrlRaw);
// Adres wewnetrzny (#kotwica) i sciezka do pliku NIE przechodza przez
// normalizacje, bo ta przepuszcza tylko http, ftp i mailto.  Odsylacz do
// miejsca w dokumencie jest jednak pelnoprawnym linkiem i musi byc na
// liscie - inaczej spis tresci wygladalby na dokument bez odsylaczy.
if (sUrl.Length == 0) sUrl = MarkdownReview_CleanUrl(sUrlRaw);
link.Url = sUrl;
link.Image = (span[0] >= 0 && span[0] < sText.Length && sText[span[0]] == '!');
link.Inline = true;
links.Add(link);
}

// Gole adresy: <http://...>, http://... oraz www...  Wiersz po wierszu, bo
// wyrazenia regularne pracuja na wierszu, a bloki kodu odsiewamy ogrodzeniami.
bool bInFence = false;
int iPos = 0;
while (true) {
int iLf = sText.IndexOf('\n', iPos);
bool bHasLf = (iLf >= 0);
if (!bHasLf) iLf = sText.Length;
int iLineTextEnd = iLf;
if (iLineTextEnd > iPos && sText[iLineTextEnd - 1] == '\r') iLineTextEnd--;
string sLine = sText.Substring(iPos, iLineTextEnd - iPos);

if (MarkdownReview_IsFenceLine(sLine)) bInFence = !bInFence;
else if (!bInFence) AddMarkdownBareLinksFromLine(sLine, iPos, links, inlineLinks);

if (!bHasLf) break;
iPos = iLf + 1;
}

links.Sort(delegate(MarkdownLink a, MarkdownLink b) {return a.Start.CompareTo(b.Start);});
return links;
} // GetMarkdownLinks method

// Gole adresy z jednego wiersza.  Te same trzy wyrazenia, ktorych uzywa
// MarkdownReview_AddNonInlineLinksFromLine - tam potrzebny byl tylko POCZATEK
// odsylacza, tu potrzebny jest caly zakres, bo F2 ma prawo go przepisac.
private static void AddMarkdownBareLinksFromLine(string sLine, int iLineStart, List<MarkdownLink> links, List<int[]> inlineLinks) {
if (String.IsNullOrEmpty(sLine)) return;
try {
Regex[] aRegex = new Regex[] {MarkdownAutoLinkRegex, MarkdownBareUrlRegex, MarkdownWwwUrlRegex, MarkdownMailAddressRegex};
foreach (Regex rx in aRegex) {
bool bMail = (rx == MarkdownMailAddressRegex);
foreach (Match m in rx.Matches(sLine)) {
if (m == null || !m.Success) continue;
int iStart = iLineStart + m.Index;
if (MarkdownReview_IsIndexInAnyInlineLinkSpan(inlineLinks, iStart)) continue;
// Jeden adres nie moze wejsc dwa razy: wyrazenie na <adres> i wyrazenie
// na goly adres trafiaja w to samo miejsce, tylko z innym poczatkiem.
bool bDuplikat = false;
foreach (MarkdownLink existing in links) {
if (iStart >= existing.Start && iStart < existing.End) {bDuplikat = true; break;}
}
if (bDuplikat) continue;

string sRaw = m.Value;
// Adres pocztowy w postaci mailto:kto@gdzie jest juz zlapany przez wzorzec
// golego adresu (schemat mailto), wiec sama czesc po dwukropku nie moze
// wejsc drugi raz jako osobna pozycja.  Sprawdzamy ZNAK PRZED trafieniem,
// nie zakres istniejacych odsylaczy: tamten test patrzy na POCZATEK, a tu
// poczatek trafienia lezy w srodku wczesniejszego odsylacza.
if (bMail && m.Index > 0) {
char cBefore = sLine[m.Index - 1];
if (cBefore == ':' || cBefore == '/' || cBefore == '@' || cBefore == '.') continue;
}
// Znak przestankowy na koncu zdania nie jest czescia adresu, wiec zakres
// odsylacza konczy sie tam, gdzie konczy sie ADRES - inaczej edycja
// zjadlaby kropke konczaca zdanie.
string sClean = MarkdownReview_CleanUrl(sRaw);
int iLen = (sClean.Length > 0 && sRaw.StartsWith(sClean)) ? sClean.Length : sRaw.Length;

MarkdownLink link = new MarkdownLink();
link.Start = iStart;
link.End = iStart + iLen;
link.TextStart = iStart;
link.TextEnd = iStart + iLen;
link.Title = "";
string sUrl = MarkdownReview_NormalizeUrl(sRaw);
if (sUrl.Length == 0) sUrl = MarkdownReview_CleanUrl(sRaw);
link.Url = sUrl;
link.Image = false;
link.Inline = false;
links.Add(link);
}
}
}
catch {}
} // AddMarkdownBareLinksFromLine method

// Mowa i wiersz listy: najpierw TRESC, potem czym ona jest - ta sama kolejnosc,
// co przy naglowkach, przypisach, komentarzach i wyroznieniach.
private static string GetMarkdownLinkSpeech(MarkdownLink link) {
if (link == null) return "";
string sKind = link.Image ? "image" : "link";
if (link.Title.Length > 0 && link.Url.Length > 0) return link.Title + ", " + link.Url + ", " + sKind;
if (link.Title.Length > 0) return link.Title + ", " + sKind;
if (link.Url.Length > 0) return link.Url + ", " + sKind;
return sKind;
} // GetMarkdownLinkSpeech method

// WIERSZ LISTY ODSYLACZY: sama tresc, bez adresu (jego decyzja 03.09.2026).
// Adres user dopowiada sobie strzalka w lewo, wiec w wierszu zostaje to, czego
// szuka.  Rodzaj mowimy TYLKO przy obrazku, bo obrazek jest wyjatkiem, a slowo
// "link" przy kazdej pozycji listy odsylaczy nie wnosi nic.  Goly adres nie ma
// tresci, wiec dla niego wierszem JEST adres - inaczej pozycja byla by pusta.
private static string GetMarkdownLinkListLine(MarkdownLink link) {
if (link == null) return "";
if (link.Title.Length > 0) return link.Image ? link.Title + ", image" : link.Title;
if (link.Url.Length > 0) return link.Image ? link.Url + ", image" : link.Url;
return link.Image ? "image" : "link";
} // GetMarkdownLinkListLine method

// TO, CO STRZALKA W LEWO DOPOWIADA NA LISCIE ODSYLACZY: adres ORAZ numer
// wiersza.  Numer wiersza zszedl tutaj z wiersza listy (jego zgloszenie 04.09.2026
// po raz drugi), bo wymawiany przy kazdej pozycji byl balastem - a wspolrzedna
// sama w sobie jest przydatna, wiec nie kasujemy jej, tylko przenosimy tam, gdzie
// user pyta o nia SAM.  Gdy odsylacz nie ma tresci, w wierszu stoi juz adres -
// wtedy strzalka mowi o tym wprost, zamiast powtarzac to samo.
private static string GetMarkdownLinkAddressSpeech(MarkdownLink link, int iLine) {
if (link == null) return "";
string sLine = (iLine > 0) ? ", line " + iLine : "";
if (link.Url.Length == 0) return "This link has no address" + sLine;
if (link.Title.Length == 0) return "Address only, " + link.Url + sLine;
return link.Url + sLine;
} // GetMarkdownLinkAddressSpeech method

// Postac Markdown odsylacza do schowka pod Control+C (ustalenie edsharpng-38):
// doslownie ten fragment tekstu, ktory w dokumencie jest odsylaczem.  Dla golego
// adresu to sam adres, bo nawiasy dopisane w tym miejscu zmienialy by rodzaj
// odsylacza bez pytania uzytkownika.
private static string GetMarkdownLinkMarkup(MarkdownLink link, string sText) {
if (link == null) return "";
try {return sText.Substring(link.Start, link.End - link.Start);} catch {}
return link.Url;
} // GetMarkdownLinkMarkup method

// Link z FORMATOWANIEM do schowka pod Control+Shift+C: obok zwyklego tekstu
// idzie postac RTF z polem HYPERLINK, ktora Word i LibreOffice wklejaja jako
// klikalny odsylacz.  Ta sama tresc, ktora wysyla kopiowanie linku z tekstu.
private bool CopyMarkdownLinkAsRichText(MarkdownLink link) {
if (link == null) return false;
string sUrl = link.Url;
if (sUrl.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) sUrl = "https://" + sUrl;
// Adres pocztowy bez schematu nie jest odsylaczem dla Worda ani przegladarki:
// kliknieciem ma otwierac program pocztowy, wiec przy kopiowaniu z
// formatowaniem dopisujemy mailto.  W dokumencie i na liscie zostaje sam
// adres, bo tak go czyta czlowiek.
if (sUrl.IndexOf('@') > 0 && sUrl.IndexOf(':') < 0) sUrl = "mailto:" + sUrl;
if (sUrl.Length == 0) return false;
string sTitle = (link.Title.Length > 0) ? link.Title : sUrl;

DataObject data = new DataObject();
data.SetData(DataFormats.UnicodeText, sTitle);
data.SetData(DataFormats.Text, sTitle);
data.SetData(DataFormats.Rtf, BuildRtfHyperlink(sTitle, sUrl));
// Wlasny format ze skladnia Markdown - zeby wklejenie w EdSharpie odtworzylo
// odsylacz, a nie sam tytul (zgloszenie Kasperczaka 01.09).
data.SetData(EdSharpMarkdownFormat, GetMarkdownLinkMarkup(link, App.Frame.Child != null && App.Frame.Child.RTB != null ? App.Frame.Child.RTB.Text : ""));
if (!Util.SetClipboardData(data)) return false;
return true;
} // CopyMarkdownLinkAsRichText method

// Edycja tytulu i adresu pod F2 (ustalenie edsharpng-38).  Zwraca true, gdy
// dokument zostal zmieniony - wtedy lista musi zostac przebudowana, bo
// przesuniecia wszystkich dalszych odsylaczy sa juz inne.
private bool EditMarkdownLink(HomerRichTextBox rtb, MarkdownLink link) {
if (rtb == null || link == null) return false;
if (rtb.ReadOnly) {
AddMessage("Document is guarded!");
return false;
}
string sText = rtb.Text ?? "";

LbcDialog dlg = new LbcDialog("Edit Link", this);
TextBox txtLabel = dlg.addInputBox("Link &text", link.Title,
link.Inline ? "" : "This is a plain address; giving it a text turns it into a Markdown link");
TextBox txtAddress = dlg.addInputBox("&Address", link.Url, "");
if (!dlg.runOkCancel()) {
dlg.Dispose();
return false;
}
string sTitle = (txtLabel.Text ?? "").Trim();
string sAddress = (txtAddress.Text ?? "").Trim();
dlg.Dispose();

if (sAddress.Length == 0) {
AddMessage("No address, nothing changed!");
return false;
}

// Te same dwie zamiany, co przy wstawianiu linku: nawias kwadratowy w tresci
// rozwalilby skladnie odsylacza, a okragly w adresie urwalby go w polowie.
sTitle = sTitle.Replace("[", "(").Replace("]", ")");
string sAddressEscaped = sAddress.Replace("(", "%28").Replace(")", "%29").Replace(" ", "%20");

string sMarkup;
if (sTitle.Length == 0) {
// Bez tresci zostaje goly adres.  Przy obrazku to blad dostepnosci, o
// ktorym mowimy wprost - obrazek bez opisu jest dla niewidomego niewidzialny.
if (link.Image) {
AddMessage("An image needs a description for screen reader users!");
return false;
}
sMarkup = sAddressEscaped;
}
else sMarkup = (link.Image ? "!" : "") + "[" + sTitle + "](" + sAddressEscaped + ")";

string sOld = "";
try {sOld = sText.Substring(link.Start, link.End - link.Start);} catch {sOld = "";}
if (sOld == sMarkup) {
AddMessage("Link unchanged");
return false;
}

rtb.ReplaceRange(link.Start, link.End, sMarkup);
rtb.Index = link.Start + sMarkup.Length;
rtb.Modified = true;
AddMessage("Link changed");
return true;
} // EditMarkdownLink method

// Lista linkow pod Control+F6.  Okno zbudowane tutaj, a nie przez Dialog.Pick,
// bo trzy klawisze na liscie (Control+C, Control+Shift+C, F2) musza siegnac do
// tresci dokumentu i do metod okna glownego.
private void ShowMarkdownLinkList(HomerRichTextBox rtb) {
if (rtb == null) return;

while (true) {
string sText = rtb.Text ?? "";
List<MarkdownLink> links = GetMarkdownLinks(sText);
if (links.Count == 0) {
AddMessage("No links!");
return;
}

List<string> lsShow = new List<string>();
foreach (MarkdownLink link in links) {
// WIERSZ NIESIE SAMA TRESC ODSYLACZA, ADRES DOPOWIADA STRZALKA W LEWO (jego
// decyzja 03.09.2026, wariant B).  Wczesniej w wierszu stal tytul, adres i
// rodzaj razem, wiec przy dlugim adresie czytnik mowil pol minuty o jednej
// pozycji.  Zasada jest ta sama, co na liscie zakladek i przypisow: w wierszu
// to, czego user szuka, a strzalka w lewo dopowiada to, czego w wierszu NIE MA.
// NUMERU WIERSZA W WIERSZU LISTY NIE MA WCALE (jego zgloszenie 01.09 i
// PONOWNIE 04.09.2026: "cialgle na liscie linkow mowi line numer to jest bez
// sensu").  Przesuniecie wspolrzednej na koniec nie wystarczylo, bo czytnik
// nadal wymawia "line" i liczbe przy KAZDEJ pozycji, a przy dwudziestu
// odsylaczach to dwadziescia razy ten sam balast.  Wspolrzedna schodzi do
// dopowiedzenia strzalka w lewo, razem z adresem - zgodnie z zasada, ze
// strzalka mowi to, czego w wierszu NIE MA (ustalenie edsharpng-74).
lsShow.Add(GetMarkdownLinkListLine(link));
}

LbcDialog dlg = new LbcDialog("Links", this);
ListBox lst = dlg.addListBox(lsShow, "", "");
dlg.setHelpDetail(lst, "Keys: Left Arrow reads the web address and the line number, Control+C copies the link as Markdown, Control+Shift+C copies it as a formatted link, F2 edits the title and address, Enter goes to the link in the text, Escape closes the list.");
// Znacznik mowi okienku Lbc, zeby ODDALO nam Control+C: jego domyslna
// obsluga skopiowala by widoczny wiersz listy razem z numerem wiersza,
// a my kopiujemy sam odsylacz.
lst.Tag = "edsharp-linklist";
LbcDialog.selectOnly(lst, 0);

bool bReopen = false;
int iEdit = -1;
lst.KeyDown += delegate(object oSender, KeyEventArgs ev) {
ListBox lb = oSender as ListBox;
if (lb == null) return;
int i = lb.SelectedIndex;
if (i < 0 || i >= links.Count) return;
MarkdownLink link = links[i];

if (ev.KeyData == Keys.Left) {
// STRZALKA W LEWO DOPOWIADA ADRES I NUMER WIERSZA (jego decyzja 03.09.2026,
// numer wiersza dolozony 04.09.2026 po drugim zgloszeniu).  Say.sayForced,
// nie Util.Say: dopowiedzenie na liscie musi przejsc nawet przy wyciszeniu
// dodatkowych komunikatow - tak samo jak na liscie przypisow i zakladek.
ev.Handled = true; ev.SuppressKeyPress = true;
Say.sayForced(GetMarkdownLinkAddressSpeech(link, GetTextLineNumberAtIndex(sText, link.Start)));
}
else if (ev.KeyData == (Keys.Control | Keys.C)) {
string sMarkup = GetMarkdownLinkMarkup(link, sText);
if (sMarkup.Length == 0) AddMessage("Nothing to copy!");
else if (!Util.SetClipboardText(sMarkup)) AddMessage("Clipboard is busy, link not copied!");
else AddMessage("Copied as Markdown");
ev.Handled = true; ev.SuppressKeyPress = true;
}
else if (ev.KeyData == (Keys.Control | Keys.Shift | Keys.C)) {
if (CopyMarkdownLinkAsRichText(link)) AddMessage("Copied as formatted link");
else AddMessage("This link has no web address to format!");
ev.Handled = true; ev.SuppressKeyPress = true;
}
else if (ev.KeyData == Keys.F2) {
ev.Handled = true; ev.SuppressKeyPress = true;
// Okienko edycji otwieramy PO zamknieciu listy, nie nad nia: dwa okna
// modalne jedno na drugim gubia fokus czytnika, a lista i tak musi byc
// przebudowana, bo przesuniecia dalszych odsylaczy sie zmienily.
// Numer trzymamy w ZMIENNEJ tej metody, nie w polu Tag okna - po
// zamknieciu formy Tag jest juz nie do odczytania.
Form frmHost = lb.FindForm();
if (frmHost != null) {
bReopen = true;
iEdit = i;
frmHost.Close();
}
}
};

bool bOk = dlg.runOkCancel();
int iPicked = lst.SelectedIndex;
dlg.Dispose();

if (bReopen) {
if (iEdit < 0 || iEdit >= links.Count) return;
EditMarkdownLink(rtb, links[iEdit]);
continue;   // lista wraca z aktualnymi przesunieciami
}

if (!bOk) return;
if (iPicked < 0 || iPicked >= links.Count) return;
MarkdownLink pick = links[iPicked];
// Kursor staje na TRESCI odsylacza, nie na nawiasie - ta sama zasada, co przy
// skoku po wyroznieniach: czytnik ma przeczytac tresc, nie znaki skladni.
int iAt = pick.TextStart;
if (iAt < 0) iAt = 0;
if (iAt > sText.Length) iAt = sText.Length;
rtb.Index = iAt;
Util.Say(GetMarkdownLinkSpeech(pick));
return;
}
} // ShowMarkdownLinkList method

// CSV JAKO TABELA - PRZEGLADANIE I EDYCJA (zadanie 10, 5.0.87).
//
// Zgloszenie Kasperczaka: "CSV ma sie otwierac jako tabela w naszym
// systemie-kreatorze tabel", a po dopytaniu: "i edycje chyba tez w takiej
// formie". Czyli JEDNO okno do obu rzeczy - nie osobna przegladarka i osobny
// edytor.
//
// Po co to w ogole: plik CSV otwarty jako tekst to jeden dlugi wiersz na
// rekord. Zeby dowiedziec sie, co stoi w trzeciej kolumnie, trzeba liczyc
// przecinki w pamieci. W siatce czytnik przy kazdej komorce mowi nazwe
// kolumny, wiec ta sama informacja jest slyszalna od razu.
//
// NAZWY KOLUMN BIERZEMY Z PIERWSZEGO WIERSZA PLIKU, nie "Column 1, Column 2".
// To jest cala roznica miedzy "kolumna trzecia: 1978" i "rok urodzenia: 1978".
// Kreator tabel Markdown ma tam numery, bo tam pierwszy wiersz jest trescia
// tabeli, ktora uzytkownik wlasnie pisze; w pliku CSV pierwszy wiersz to
// prawie zawsze naglowki, wiec je wykorzystujemy.
//
// Zapis idzie tam, skad plik przyszedl - z zachowaniem separatora i rodzaju
// konca wiersza, ktore plik mial. Zmiana pliku ze srednikami na przecinkowy
// przy okazji edycji jednej komorki byla by cicha zmiana cudzych danych.
public void EditCsvAsTable(string sFile) {
if (sFile == null || sFile.Length == 0) return;
if (!File.Exists(sFile)) {
Dialog.Show("CSV Table", "This file no longer exists:\n" + sFile);
return;
}

string sText;
// Kodowanie wykrywamy z pliku (przekazujemy null), tak samo jak przy
// otwieraniu dokumentu. Zapamietujemy je, bo zapis MUSI wrocic w tym samym
// kodowaniu - inaczej plik w Latin II wrocilby jako UTF-8 i polskie litery
// zmienily by sie w krzaki dla programu, ktory ten plik czyta.
Encoding enFile = null;
try {
sText = Util.File2String(sFile, ref enFile);
if (enFile == null) enFile = new UTF8Encoding(true);
}
catch (Exception ex) {
Dialog.Show("CSV Table", "This file could not be read.\n" + ex.Message);
return;
}

// Separator i konce wiersza z PLIKU - zapamietane przed rozlozeniem na
// komorki, bo po rozlozeniu nie da sie ich odtworzyc.
char cSep = EdSharp.Csv.RozpoznajSeparator(sText);
string sBreak = "\r\n";
if (sText.IndexOf("\r\n") < 0) {
if (sText.IndexOf('\n') >= 0) sBreak = "\n";
else if (sText.IndexOf('\r') >= 0) sBreak = "\r";
}

List<List<string>> rows = EdSharp.Csv.Czytaj(sText, cSep, 0);
if (rows.Count == 0) {
Dialog.Show("CSV Table", "This file is empty, so there is no table to show.");
return;
}

int iCols = EdSharp.Csv.NajwiecejKolumn(rows);
if (iCols < 1) iCols = 1;

// Pierwszy wiersz jako naglowki - ale tylko gdy naprawde na nie wyglada:
// same niepuste pola, bez powtorzen. Plik BEZ naglowkow (od razu dane)
// dostaje numery kolumn, bo wziecie pierwszego rekordu za naglowki
// UKRYLOBY ten rekord przed uzytkownikiem, a to jest utrata danych z widoku.
bool bMaNaglowki = true;
List<string> naglowki = new List<string>();
if (rows.Count < 2) bMaNaglowki = false;
else {
List<string> w0 = rows[0];
for (int c = 0; c < iCols; c++) {
string sH = (c < w0.Count && w0[c] != null) ? w0[c].Trim() : "";
if (sH.Length == 0) { bMaNaglowki = false; break; }
// Powtorzona nazwa kolumny byla by dla czytnika myląca ("rok" dwa razy).
foreach (string sIstniejacy in naglowki) {
if (string.Compare(sIstniejacy, sH, true) == 0) { bMaNaglowki = false; break; }
}
if (!bMaNaglowki) break;
naglowki.Add(sH);
}
}

string sTytul = Path.GetFileName(sFile);
LbcDialog dlg = new LbcDialog("CSV Table - " + sTytul, App.Frame);
Homer.LbcGrid grid = dlg.addPickGrid("&Table cells", "Correct the cells; Control+Enter saves the file");
dlg.setHelpDetail(grid, "Keys: Arrow keys move between cells, Right Arrow from the last column adds a column, Down Arrow from the last row adds a row, typing replaces what is in the cell, F2 edits what is already there and Enter confirms it, Delete clears the cell, Control+Enter saves the file, Escape closes without changing the file. Column names come from the first line of the file.");

for (int c = 0; c < iCols; c++) {
string sHeader = bMaNaglowki ? naglowki[c] : ("Column " + (c + 1));
Homer.LbcDialog.addGridColumn(grid, "c" + (c + 1), sHeader);
}

// Gdy pierwszy wiersz posluzyl za naglowki, do siatki wchodza wiersze od
// drugiego - inaczej naglowki widniały by dwa razy.
int iFirstData = bMaNaglowki ? 1 : 0;
for (int r = iFirstData; r < rows.Count; r++) {
int iNew = grid.Rows.Add();
for (int c = 0; c < rows[r].Count && c < iCols; c++)
grid.Rows[iNew].Cells[c].Value = rows[r][c];
}
if (grid.Rows.Count == 0) grid.Rows.Add();

// Naglowki wierszy PUSTE. Czytnik czyta naglowek wiersza przed trescia
// komorki, a komorka podaje juz swoja wspolrzedna sama (LbcGridCell) -
// numer w naglowku znaczyl by slyszenie tego samego dwa razy.
for (int i = 0; i < grid.Rows.Count; i++) grid.Rows[i].HeaderCell.Value = "";

if (grid.Rows.Count > 0 && grid.Columns.Count > 0) grid.CurrentCell = grid.Rows[0].Cells[0];
dlg.setInitialFocus(grid);

// Stan wejsciowy - do rozpoznania, czy Escape ma o cokolwiek pytac.
string sOryginal = BuildCsvFromGrid(grid, bMaNaglowki ? naglowki : null, cSep, sBreak);

bool[] abSave = new bool[] {false};

grid.PreviewCmdKey += delegate(object oSender, KeyEventArgs ev) {
Keys keyData = ev.KeyData;
bool bEditing = grid.IsCurrentCellInEditMode;
TextBox tbEdit = grid.EditingControl as TextBox;

if (keyData == (Keys.Control | Keys.Enter)) {
ev.Handled = true;
if (bEditing) grid.EndEdit();
abSave[0] = true;
Form frmHost = grid.FindForm();
if (frmHost != null) {frmHost.DialogResult = DialogResult.OK; frmHost.Close();}
return;
}

if (keyData == Keys.Escape) {
if (bEditing) return;
if (BuildCsvFromGrid(grid, bMaNaglowki ? naglowki : null, cSep, sBreak) != sOryginal) {
if (Dialog.Confirm("Confirm", "Close the table without saving your changes to the file?", "N") != "Y") {
ev.Handled = true;
return;
}
}
ev.Handled = true;
Form frmHost = grid.FindForm();
if (frmHost != null) {frmHost.DialogResult = DialogResult.Cancel; frmHost.Close();}
return;
}

if (grid.CurrentCell == null) return;

if (keyData == Keys.Delete) {
if (bEditing) return;
ev.Handled = true;
string sBylo = GetMarkdownTableCellText(grid, grid.CurrentCell.RowIndex, grid.CurrentCell.ColumnIndex);
if (sBylo.Trim().Length == 0) { Say.sayForced("Cell is already empty"); return; }
grid.CurrentCell.Value = "";
Say.sayForced("Cleared");
return;
}

if (keyData == Keys.Left) {
if (bEditing && grid.EditStartedByF2 && tbEdit != null
    && (tbEdit.SelectionStart + tbEdit.SelectionLength) > 0) return;
if (!bEditing) return;
ev.Handled = true;
int iRowL = grid.CurrentCell.RowIndex;
int iColL = grid.CurrentCell.ColumnIndex;
grid.EndEdit();
if (iColL > 0) grid.CurrentCell = grid.Rows[iRowL].Cells[iColL - 1];
return;
}

if (keyData == Keys.Right) {
if (bEditing && tbEdit != null
    && (tbEdit.SelectionStart + tbEdit.SelectionLength) < tbEdit.TextLength) return;
if (grid.CurrentCell.ColumnIndex != grid.Columns.Count - 1) return;
ev.Handled = true;
int iRow = grid.CurrentCell.RowIndex;
if (bEditing) grid.EndEdit();
// Nowa kolumna dostaje numer, nie nazwe - nazwy z pliku sa tylko dla
// kolumn, ktore plik mial.
int iNew = Homer.LbcDialog.addGridColumn(grid, "c" + (grid.Columns.Count + 1), "Column " + (grid.Columns.Count + 1));
if (bMaNaglowki) naglowki.Add("Column " + grid.Columns.Count);
grid.CurrentCell = grid.Rows[iRow].Cells[iNew];
return;
}

if (keyData == Keys.Down) {
if (grid.CurrentCell.RowIndex != grid.Rows.Count - 1) return;
ev.Handled = true;
int iCol = grid.CurrentCell.ColumnIndex;
if (bEditing) grid.EndEdit();
int iNew = grid.Rows.Add();
grid.Rows[iNew].HeaderCell.Value = "";
grid.CurrentCell = grid.Rows[iNew].Cells[iCol];
return;
}
};

dlg.runWithButtons(new string[] {"Save", "Cancel"});
if (!abSave[0]) return;

string sNowy = BuildCsvFromGrid(grid, bMaNaglowki ? naglowki : null, cSep, sBreak);
if (sNowy == sOryginal) {
AddMessage("Nothing changed in " + sTytul);
return;
}

// KOPIA ZAPASOWA PRZED NADPISANIEM. Zapis tabeli podmienia CALY plik, wiec
// blad w moim kodzie kosztowal by uzytkownika dane, ktorych nie da sie
// odtworzyc. Kopia .bak jest tania i zdejmuje ten koszt.
try {
string sBak = sFile + ".bak";
File.Copy(sFile, sBak, true);
}
catch {}

try {
File.WriteAllText(sFile, sNowy, enFile);
}
catch (Exception ex) {
Dialog.Show("CSV Table", "The file could not be saved.\n" + ex.Message + "\n\nYour previous version is still on disk.");
return;
}

// Gdy ten sam plik jest otwarty w edytorze, jego tresc na ekranie jest juz
// nieaktualna - milczenie kazalo by uzytkownikowi pracowac na starej.
AddMessage("Saved " + sTytul + " (a copy of the previous version is in " + Path.GetFileName(sFile) + ".bak)");
} // EditCsvAsTable method

// Sklada plik CSV z siatki. Naglowki wracaja jako pierwszy wiersz TYLKO
// wtedy, gdy stamtad przyszly - dopisanie ich do pliku, ktory ich nie mial,
// byloby dodaniem wiersza danych, ktorego uzytkownik nie wpisal.
private string BuildCsvFromGrid(DataGridView grid, List<string> naglowki, char cSep, string sBreak) {
List<List<string>> rows = new List<List<string>>();
if (naglowki != null && naglowki.Count > 0) {
List<string> h = new List<string>();
for (int c = 0; c < grid.Columns.Count; c++)
h.Add(c < naglowki.Count ? naglowki[c] : ("Column " + (c + 1)));
rows.Add(h);
}

int iRows, iCols;
GetMarkdownTableGridExtent(grid, out iRows, out iCols);
for (int r = 0; r < iRows; r++) {
List<string> w = new List<string>();
for (int c = 0; c < iCols; c++) w.Add(GetMarkdownTableCellText(grid, r, c));
rows.Add(w);
}
return EdSharp.Csv.Zapisz(rows, cSep, sBreak);
} // BuildCsvFromGrid method

private void RunMarkdownTableWizard(HomerRichTextBox rtb) {
if (rtb == null) return;
string sText = rtb.Text ?? "";
int iCursor = rtb.Index;
if (iCursor < 0) iCursor = 0;
if (iCursor > sText.Length) iCursor = sText.Length;

List<int[]> fencesHere = MarkdownReview_FindFenceRanges(sText);

// Czy kursor stoi w GOTOWEJ tabeli?  Blok wierszy z kreskami pionowymi,
// ktorego druga linia jest wierszem kreskek - dokladnie to, co konwertery
// uznaja za tabele.  Wyrownania kolumn z wiersza kreskek sa ZACHOWYWANE:
// uzytkownik moze je miec wpisane, a ich zgubienie byloby cicha utrata jego
// pracy.
int iTableStart = -1;
int iTableEnd = -1;
List<string> rowsFound = null;
List<string> alignFound = null;
bool bEdit = FindMarkdownTableAtCursor(sText, iCursor, fencesHere, out iTableStart, out iTableEnd, out rowsFound, out alignFound);

// Ta sama bramka na miejsce kursora, co przy przypisach: w bloku kodu tabela
// jest MARTWA (konwertery zobacza goly tekst z kreskami), a komunikat
// "Table inserted" byl by nieprawda.  Tabeli JUZ ISTNIEJACEJ to nie dotyczy -
// jej nie ma w bloku kodu, bo rozpoznanie bloki kodu pomija.
if (!bEdit && IsMarkdownIndexInFence(fencesHere, iCursor)) {
AddMessage("Cannot put a table inside a code block!");
return;
}

LbcDialog dlg = new LbcDialog(bEdit ? "Edit Table" : "Insert Table", App.Frame);
Homer.LbcGrid grid = dlg.addPickGrid("&Table cells", bEdit ? "Correct the cells; Control+Enter saves the table" : "Type to fill a cell; Control+Enter inserts the table");
dlg.setHelpDetail(grid, "Keys: Arrow keys move between cells, Right Arrow from the last column adds a column, Down Arrow from the last row adds a row, typing replaces what is in the cell, F2 edits what is already there and Enter confirms it, Delete clears the cell, Control+Enter puts the table into the document, Escape closes without changing the document. The first row is the table header. Empty columns and rows at the end are left out.");

if (bEdit) FillGridFromMarkdownTable(grid, rowsFound);
else {
Homer.LbcDialog.addGridColumn(grid, "c1", "Column 1");
grid.Rows.Add();
}
SetMarkdownTableRowHeaders(grid);
if (grid.Rows.Count > 0 && grid.Columns.Count > 0) grid.CurrentCell = grid.Rows[0].Cells[0];
dlg.setInitialFocus(grid);

// Stan wejsciowy siatki.  Przy EDYCJI "czy cos wpisano" nie wystarcza -
// tabela jest wypelniona od poczatku, wiec kazde Escape pytalo by o
// potwierdzenie.  Pytamy tylko, gdy tresc REALNIE sie rozni od tej z pliku.
string sOryginal = BuildMarkdownTableFromGrid(grid, alignFound);

// Wynik pracy okna.  Zbierany w domkniecu klawiszy, bo tylko tam wiemy, czy
// uzytkownik wstawil tabele (Control+Enter), czy wyszedl (Escape).
bool[] abInsert = new bool[] {false};

grid.PreviewCmdKey += delegate(object oSender, KeyEventArgs ev) {
Keys keyData = ev.KeyData;
bool bEditing = grid.IsCurrentCellInEditMode;
TextBox tbEdit = grid.EditingControl as TextBox;

// Control+Enter: zatwierdz komorke, w ktorej wlasnie pisze, i wstaw tabele.
// Bez EndEdit ostatnio wpisana komorka nie trafila by do tabeli.
if (keyData == (Keys.Control | Keys.Enter)) {
ev.Handled = true;
if (bEditing) grid.EndEdit();
if (IsMarkdownTableGridEmpty(grid)) {
// Przy edycji pusta siatka nie znaczy "usun tabele" - takiej decyzji nie
// podejmujemy za uzytkownika, bo usuniecie tresci musi byc jawne.
Say.sayForced(bEdit ? "The table is empty, nothing to save" : "The table is empty, nothing to insert");
return;
}
abInsert[0] = true;
Form frmHost = grid.FindForm();
if (frmHost != null) {frmHost.DialogResult = DialogResult.OK; frmHost.Close();}
return;
}

// Escape: w trakcie pisania w komorce nalezy do komorki (cofa jej edycje),
// dopiero poza edycja zamyka okno - i tylko za potwierdzeniem, gdy tresc
// rozni sie od tej, ktora byla na wejsciu.
if (keyData == Keys.Escape) {
if (bEditing) return;
if (BuildMarkdownTableFromGrid(grid, alignFound) != sOryginal) {
if (Dialog.Confirm("Confirm", bEdit ? "Close the table without saving your changes?" : "Close the table without inserting it?  What you typed will be lost.", "N") != "Y") {
ev.Handled = true;
return;
}
}
ev.Handled = true;
Form frmHost = grid.FindForm();
if (frmHost != null) {frmHost.DialogResult = DialogResult.Cancel; frmHost.Close();}
return;
}

if (grid.CurrentCell == null) return;

// Delete = wyczysc tresc komorki.  Jego pytanie: "jak usuwac dane?  (...)
// delete powinien kasowac zawartosc tresci komorki".  W trakcie EDYCJI
// Delete nalezy do pola tekstowego (usuwa jeden znak za karetka) - inaczej
// poprawianie literowki kasowalo by cala komorke.
if (keyData == Keys.Delete) {
if (bEditing) return;
ev.Handled = true;
string sBylo = GetMarkdownTableCellText(grid, grid.CurrentCell.RowIndex, grid.CurrentCell.ColumnIndex);
if (sBylo.Trim().Length == 0) {
Say.sayForced("Cell is already empty");
return;
}
grid.CurrentCell.Value = "";
// Mowa WYMUSZONA, bo tresc komorki sie zmienila, a fokus NIE drgnal -
// czytnik nie ma z czego sam wywnioskowac, ze cos zniknelo.
Say.sayForced("Cleared");
return;
}

// Strzalka w LEWO: w trakcie PISANIA zatwierdza komorke i przechodzi w lewo,
// tak jak w arkuszu.  Po F2 (poprawianie tresci, ktora tam byla) nalezy do
// karetki, dopoki nie stoi na poczatku tekstu.  Bez tego rozroznienia
// wpisana wartosc i nastepne slowo skleily by sie w jednej komorce -
// zmierzone na zywym NVDA: "Ala" wpadlo w srodek "30" dajac "3Ala0".
if (keyData == Keys.Left) {
if (bEditing && grid.EditStartedByF2 && tbEdit != null
    && (tbEdit.SelectionStart + tbEdit.SelectionLength) > 0) return;
if (!bEditing) return;
ev.Handled = true;
int iRowL = grid.CurrentCell.RowIndex;
int iColL = grid.CurrentCell.ColumnIndex;
grid.EndEdit();
if (iColL > 0) grid.CurrentCell = grid.Rows[iRowL].Cells[iColL - 1];
return;
}

// Strzalka w prawo z ostatniej kolumny = nowa kolumna.  W trakcie pisania
// oddajemy klawisz komorce, dopoki karetka nie stoi na koncu tekstu - tak
// samo dziala przechodzenie miedzy komorkami w arkuszu.
if (keyData == Keys.Right) {
if (bEditing && tbEdit != null
    && (tbEdit.SelectionStart + tbEdit.SelectionLength) < tbEdit.TextLength) return;
if (grid.CurrentCell.ColumnIndex != grid.Columns.Count - 1) return;
ev.Handled = true;
int iRow = grid.CurrentCell.RowIndex;
if (bEditing) grid.EndEdit();
int iNew = Homer.LbcDialog.addGridColumn(grid, "c" + (grid.Columns.Count + 1), "Column " + (grid.Columns.Count + 1));
grid.CurrentCell = grid.Rows[iRow].Cells[iNew];
return;
}

// Strzalka w dol z ostatniego wiersza = nowy wiersz.  Komorka jest
// jednowierszowa, wiec w trakcie pisania strzalka w dol i tak konczy edycje.
if (keyData == Keys.Down) {
if (grid.CurrentCell.RowIndex != grid.Rows.Count - 1) return;
ev.Handled = true;
int iCol = grid.CurrentCell.ColumnIndex;
if (bEditing) grid.EndEdit();
int iNew = grid.Rows.Add();
SetMarkdownTableRowHeaders(grid);
grid.CurrentCell = grid.Rows[iNew].Cells[iCol];
return;
}
};

dlg.runWithButtons(new string[] {bEdit ? "Save" : "Insert", "Cancel"});
bool bInsert = abInsert[0];
string sTable = bInsert ? BuildMarkdownTableFromGrid(grid, alignFound) : "";
int iRows = 0, iCols = 0;
if (bInsert) GetMarkdownTableGridExtent(grid, out iRows, out iCols);
dlg.Dispose();

if (!bInsert || sTable.Length == 0) {
AddMessage(bEdit ? "Table not changed" : "No table inserted");
return;
}

// EDYCJA: podmieniamy DOKLADNIE zakres starej tabeli, bez dokladania pustych
// linii - tabela juz stoi na swoich wierszach.  Tresc konca ostatniego
// wiersza zostaje nietknieta, wiec plik nie puchnie o puste linie przy kazdym
// przejsciu przez kreator.
if (bEdit) {
string sNowa = sTable;
if (sNowa.EndsWith(LF)) sNowa = sNowa.Substring(0, sNowa.Length - 1);
if (sNowa == sText.Substring(iTableStart, iTableEnd - iTableStart)) {
AddMessage("Table not changed");
return;
}
rtb.ReplaceRange(iTableStart, iTableEnd, sNowa);
rtb.Index = iTableStart;
rtb.Modified = true;
AddMessage("Table saved, " + iRows + (iRows == 1 ? " row, " : " rows, ") + iCols + (iCols == 1 ? " column" : " columns"));
return;
}

// Tabela musi stac na wlasnych wierszach, z pusta linia nad soba, gdy nad
// kursorem jest tekst - ta sama zasada, co przy wstawianiu naglowka.
string sPrefix = "";
if (iCursor > 0) {
bool bAtLineStart = (sText[iCursor - 1] == '\n');
if (!bAtLineStart) sPrefix = LF + LF;
else {
int iPrev = iCursor - 1;
if (iPrev > 0 && sText[iPrev - 1] == '\r') iPrev--;
bool bBlankAbove = (iPrev == 0) || (sText[iPrev - 1] == '\n');
if (!bBlankAbove) sPrefix = LF;
}
}

string sInsert = sPrefix + sTable;
// ReplaceRange, wiec Control+Z to cofa, i zadnego zapisu na dysk - regula z
// utraty pliku przy Format Code.
rtb.ReplaceRange(iCursor, iCursor, sInsert);
rtb.Index = iCursor + sInsert.Length;
rtb.Modified = true;
AddMessage("Table inserted, " + iRows + (iRows == 1 ? " row, " : " rows, ") + iCols + (iCols == 1 ? " column" : " columns"));
} // RunMarkdownTableWizard method

// Gotowa tabela pod kursorem.  Blok SASIADUJACYCH linii, z ktorych kazda ma
// kreske pionowa, a DRUGA jest wierszem kreskek - taka sama definicja, jakiej
// uzywaja konwertery, wiec kreator nie otworzy czegos, co tabela nie jest.
// Zwraca zakres w tekscie (bez konca ostatniej linii), wiersze tresci BEZ
// wiersza kreskek oraz wyrownania kolumn z tego wiersza.
private bool FindMarkdownTableAtCursor(string sText, int iCursor, List<int[]> fences, out int iTableStart, out int iTableEnd, out List<string> rows, out List<string> aligns) {
iTableStart = -1;
iTableEnd = -1;
rows = new List<string>();
aligns = new List<string>();
if (String.IsNullOrEmpty(sText)) return false;

// Linie z offsetami.  Koniec linii to koniec TRESCI, bez CR i LF - dokladnie
// ten zakres wchodzi potem do ReplaceRange.
List<int> aStart = new List<int>();
List<int> aEnd = new List<int>();
List<string> aLine = new List<string>();
int iPos = 0;
while (iPos <= sText.Length) {
int iLf = sText.IndexOf('\n', iPos);
bool bHasLf = (iLf >= 0);
if (!bHasLf) iLf = sText.Length;
int iTextEnd = iLf;
if (iTextEnd > iPos && sText[iTextEnd - 1] == '\r') iTextEnd--;
aStart.Add(iPos);
aEnd.Add(iTextEnd);
aLine.Add(sText.Substring(iPos, iTextEnd - iPos));
if (!bHasLf) break;
iPos = iLf + 1;
}

// Linia kursora.  Kursor na samym koncu linii nalezy jeszcze do tej linii.
int iCur = -1;
for (int i = 0; i < aStart.Count; i++) {
if (iCursor >= aStart[i] && iCursor <= aEnd[i]) {iCur = i; break;}
}
if (iCur < 0) return false;
if (IsMarkdownIndexInFence(fences, aStart[iCur])) return false;
if (!IsMarkdownTableLine(aLine[iCur])) return false;

int iFirst = iCur;
while (iFirst > 0 && IsMarkdownTableLine(aLine[iFirst - 1]) && !IsMarkdownIndexInFence(fences, aStart[iFirst - 1])) iFirst--;
int iLast = iCur;
while (iLast + 1 < aLine.Count && IsMarkdownTableLine(aLine[iLast + 1]) && !IsMarkdownIndexInFence(fences, aStart[iLast + 1])) iLast++;

// Wiersz kreskek MUSI byc drugi.  Bez niego to nie tabela, tylko tekst z
// kreskami pionowymi - i wtedy kreator ma wstawic nowa, a nie przepisywac
// akapit uzytkownika.
if (iLast - iFirst < 1) return false;
if (!MarkdownTableSeparatorRegex.IsMatch(aLine[iFirst + 1])) return false;

iTableStart = aStart[iFirst];
iTableEnd = aEnd[iLast];
aligns = ParseMarkdownTableAlignments(aLine[iFirst + 1]);
for (int i = iFirst; i <= iLast; i++) {
if (i == iFirst + 1) continue;
rows.Add(aLine[i]);
}
return true;
} // FindMarkdownTableAtCursor method

// Linia nalezaca do tabeli: niepusta i z kreska pionowa.  Kreska ZASLONIETA
// odwrotnym ukosnikiem tez sie liczy - to tresc komorki, wiec linia i tak
// nalezy do tabeli.
private static bool IsMarkdownTableLine(string sLine) {
if (sLine == null) return false;
if (sLine.Trim().Length == 0) return false;
return sLine.IndexOf('|') >= 0;
} // IsMarkdownTableLine method

// Wyrownania kolumn z wiersza kreskek.  Zachowujemy je, bo uzytkownik moze je
// miec wpisane, a ich zgubienie po przejsciu przez kreator byloby cicha utrata
// jego pracy.
private static List<string> ParseMarkdownTableAlignments(string sSeparator) {
List<string> aligns = new List<string>();
if (sSeparator == null) return aligns;
string s = sSeparator.Trim();
if (s.StartsWith("|")) s = s.Substring(1);
if (s.EndsWith("|")) s = s.Substring(0, s.Length - 1);
string[] aCell = s.Split('|');
for (int i = 0; i < aCell.Length; i++) {
string sCell = aCell[i].Trim();
bool bLeft = sCell.StartsWith(":");
bool bRight = sCell.EndsWith(":");
if (bLeft && bRight) aligns.Add("center");
else if (bRight) aligns.Add("right");
else if (bLeft) aligns.Add("left");
else aligns.Add("");
}
return aligns;
} // ParseMarkdownTableAlignments method

// Tresc komorek z jednego wiersza tabeli.  Odwrotnosc EscapeMarkdownTableCell:
// kreska zaslonieta odwrotnym ukosnikiem NIE konczy komorki i wraca do siatki
// jako zwykly znak - inaczej kazde przejscie przez kreator rozbijalo by taka
// komorke na dwie.
private static List<string> ParseMarkdownTableRowCells(string sRow) {
List<string> cells = new List<string>();
if (sRow == null) return cells;
string s = sRow.Trim();
if (s.StartsWith("|")) s = s.Substring(1);
StringBuilder sb = new StringBuilder();
bool bTrailingPipe = false;
for (int i = 0; i < s.Length; i++) {
char c = s[i];
if (c == '\\' && i + 1 < s.Length && s[i + 1] == '|') {sb.Append('|'); i++; continue;}
if (c == '|') {
cells.Add(sb.ToString().Trim());
sb.Length = 0;
bTrailingPipe = true;
continue;
}
sb.Append(c);
bTrailingPipe = false;
}
// Ostatnia kreska pionowa zamyka ostatnia komorke i nie otwiera nowej.
if (sb.ToString().Trim().Length > 0 || !bTrailingPipe) cells.Add(sb.ToString().Trim());
return cells;
} // ParseMarkdownTableRowCells method

// Wypelnienie siatki gotowa tabela.  Liczba kolumn to NAJSZERSZY wiersz:
// tabele pisane recznie miewaja wiersze krotsze, a siatka musi je pokazac
// wszystkie, zeby dalo sie je uzupelnic.  Kolumny dokladamy TYLKO przez
// addGridColumn, bo zwykle Columns.Add gubi dostepnosc komorki.
private void FillGridFromMarkdownTable(Homer.LbcGrid grid, List<string> rows) {
if (grid == null) return;
List<List<string>> aRow = new List<List<string>>();
int iCols = 0;
if (rows != null) {
for (int i = 0; i < rows.Count; i++) {
List<string> cells = ParseMarkdownTableRowCells(rows[i]);
aRow.Add(cells);
if (cells.Count > iCols) iCols = cells.Count;
}
}
if (iCols < 1) iCols = 1;
if (aRow.Count < 1) aRow.Add(new List<string>());
for (int c = 0; c < iCols; c++) Homer.LbcDialog.addGridColumn(grid, "c" + (c + 1), "Column " + (c + 1));
for (int r = 0; r < aRow.Count; r++) {
int iNew = grid.Rows.Add();
for (int c = 0; c < aRow[r].Count && c < iCols; c++) grid.Rows[iNew].Cells[c].Value = aRow[r][c];
}
} // FillGridFromMarkdownTable method

// Naglowki wierszy siatki.  Czytnik czyta naglowek wiersza PRZED trescia
// komorki, wiec musi byc KROTKI i nie moze dublowac wspolrzednej, ktora
// komorka podaje sama (patrz LbcGridCell).  Zmierzone na zywym NVDA:
// domyslne naglowki mowily "Wiersz 0" przed trescia, czyli uzytkownik slyszal
// numer liczony od zera, a potem drugi raz ten sam wiersz.  Stad puste
// naglowki wierszy tresci i samo "Header" dla wiersza naglowkowego.
private void SetMarkdownTableRowHeaders(DataGridView grid) {
if (grid == null) return;
for (int i = 0; i < grid.Rows.Count; i++) {
grid.Rows[i].HeaderCell.Value = (i == 0) ? "Header" : "";
}
} // SetMarkdownTableRowHeaders method

private string GetMarkdownTableCellText(DataGridView grid, int iRow, int iCol) {
if (grid == null) return "";
if (iRow < 0 || iRow >= grid.Rows.Count) return "";
if (iCol < 0 || iCol >= grid.Columns.Count) return "";
object oValue = grid.Rows[iRow].Cells[iCol].Value;
if (oValue == null) return "";
return oValue.ToString();
} // GetMarkdownTableCellText method

private bool IsMarkdownTableGridEmpty(DataGridView grid) {
int iRows, iCols;
GetMarkdownTableGridExtent(grid, out iRows, out iCols);
return (iRows == 0 || iCols == 0);
} // IsMarkdownTableGridEmpty method

// Zasieg realnie wypelnionej czesci siatki.  Puste kolumny i wiersze na koncu
// nie wchodza do tabeli: doklada sie je jednym nacisnieciem strzalki, wiec
// nadmiarowa kolumna zdarzy sie kazdemu, a inaczej nie da sie jej cofnac.
private void GetMarkdownTableGridExtent(DataGridView grid, out int iRows, out int iCols) {
iRows = 0;
iCols = 0;
if (grid == null) return;
for (int r = 0; r < grid.Rows.Count; r++) {
for (int c = 0; c < grid.Columns.Count; c++) {
if (GetMarkdownTableCellText(grid, r, c).Trim().Length == 0) continue;
if (r + 1 > iRows) iRows = r + 1;
if (c + 1 > iCols) iCols = c + 1;
}
}
} // GetMarkdownTableGridExtent method

// Tresc komorki w skladni tabeli.  Kreska pionowa konczy komorke, wiec
// wpisana przez uzytkownika musi byc zabezpieczona, inaczej rozjechala by
// cala tabele.  Znaki konca wiersza zamieniamy na spacje, bo komorka tabeli
// Markdown nie moze ich zawierac.
private string EscapeMarkdownTableCell(string sValue) {
if (sValue == null) return "";
string s = sValue.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");
s = s.Replace("|", "\\|");
return s.Trim();
} // EscapeMarkdownTableCell method

// Wiersz kreskek dla jednej kolumny, z zachowanym wyrownaniem z pliku.
private static string MarkdownTableDashCell(List<string> aligns, int iCol) {
string sAlign = (aligns != null && iCol < aligns.Count) ? aligns[iCol] : "";
if (sAlign == "center") return " :---: |";
if (sAlign == "right") return " ---: |";
if (sAlign == "left") return " :--- |";
return " --- |";
} // MarkdownTableDashCell method

private string BuildMarkdownTableFromGrid(DataGridView grid) {
return BuildMarkdownTableFromGrid(grid, null);
} // BuildMarkdownTableFromGrid method

private string BuildMarkdownTableFromGrid(DataGridView grid, List<string> aligns) {
int iRows, iCols;
GetMarkdownTableGridExtent(grid, out iRows, out iCols);
if (iRows == 0 || iCols == 0) return "";

StringBuilder sb = new StringBuilder();
for (int r = 0; r < iRows; r++) {
sb.Append("|");
for (int c = 0; c < iCols; c++) {
sb.Append(" ");
sb.Append(EscapeMarkdownTableCell(GetMarkdownTableCellText(grid, r, c)));
sb.Append(" |");
}
sb.Append(LF);
// Wiersz kreskek zaraz za naglowkiem - bez niego zaden konwerter nie uzna
// tego za tabele.
if (r != 0) continue;
sb.Append("|");
for (int c = 0; c < iCols; c++) sb.Append(MarkdownTableDashCell(aligns, c));
sb.Append(LF);
}
return sb.ToString();
} // BuildMarkdownTableFromGrid method

private void InsertMarkdownFootnote(HomerRichTextBox rtb) {
if (rtb == null) return;
string sText = rtb.Text ?? "";
int iCursor = rtb.Index;
if (iCursor < 0) iCursor = 0;
if (iCursor > sText.Length) iCursor = sText.Length;

// BRAMKA NA MIEJSCE KURSORA (pomiar iteracji 2, zlecenie -3): w bloku kodu
// i w tresci innego przypisu znacznik jest MARTWY, a komunikat "Footnote N
// inserted" klamalby.  Zmierzone naszymi wlasnymi narzedziami z katalogu
// Convert, tymi, ktore jada w paczce u niego:
//   - blok kodu: podglad 2htm nie robi odsylacza, a pandoc daje ZERO
//     przypisow Worda i zostawia goly tekst "[^1]" w akapicie,
//   - przypis w przypisie: tresc zagniezdzonego przypisu NIE dociera do
//     docx ani jako przypis, ani jako tekst - po prostu jej nie ma, czyli
//     uzytkownik TRACI to, co napisal.
// Naglowek celowo NIE jest blokowany: tam przypis zyje (przypis Worda
// obecny, tekst naglowka nietkniety), a kursor stoi w naglowku po kazdym
// skoku spisem tresci, wiec blokada bylaby uciazliwa bez powodu.
List<int[]> fencesHere = MarkdownReview_FindFenceRanges(sText);
if (IsMarkdownIndexInFence(fencesHere, iCursor)) {
AddMessage("Cannot put a footnote inside a code block!");
return;
}
string sCursorLine = GetTextLineAtIndex(sText, iCursor);
string sCursorLabel, sCursorBody;
if (IsMarkdownFootnoteDefinitionLine(sCursorLine, out sCursorLabel, out sCursorBody)) {
AddMessage("Cannot put a footnote inside the text of footnote " + sCursorLabel + "!");
return;
}

// WYBOR MIEJSCA TRESCI w tym samym okienku, co tresc przypisu - jego
// zlecenie z 29.08.2026 01:40 i jego wlasne uscislenie: "to chyba bedzie do
// wyboru w okienku tworzenia przypisu".  Wybor jest w JEDNYM oknie z trescia,
// a nie w drugim okienku po zatwierdzeniu, bo to jedna decyzja o jednym
// przypisie - drugie okno kazaloby czytnikowi ogloszac cala rzecz dwa razy.
//
// Kolejnosc pol ma znaczenie dla czytnika: NAJPIERW tresc, potem miejsce.
// Tresc jest tym, po co user otworzyl okno, a miejsce ma domysl, wiec czesto
// przejdzie przez nie Tabem bez zmiany.
int iChapterEnd = GetMarkdownFootnoteChapterEnd(sText, iCursor);
bool bChapterPossible = (iChapterEnd >= 0);

LbcDialog dlg = new LbcDialog("Insert Footnote", this);
TextBox txtContent = dlg.addInputBox("Footnote &text", "");
List<string> lPlaces = new List<string>();
lPlaces.Add("End of document");
// Pozycji "koniec rozdzialu" NIE POKAZUJEMY, gdy rozdzialu nie ma: martwa
// pozycja na liscie jest dla niewidomego gorsza niz jej brak, bo wybiera ja,
// czeka na skutek i nie dostaje go.  Powod nazywamy w podpowiedzi ponizej.
if (bChapterPossible) lPlaces.Add("End of current chapter");
ComboBox cboPlace = dlg.addComboPickBox("Footnote text &goes to",
lPlaces, lPlaces[0],
bChapterPossible ? "Where the footnote text lands"
: "Only end of document: the cursor is not inside a chapter");
if (!dlg.runOkCancel()) {
dlg.Dispose();
return;
}
string sContent = (txtContent.Text ?? "").Trim();
bool bToChapter = bChapterPossible && cboPlace.SelectedIndex == 1;
dlg.Dispose();

if (sContent.Length == 0) {
AddMessage("No footnote text, nothing inserted!");
return;
}

string sLabel = GetNextMarkdownFootnoteLabel(sText);
string sRef = "[^" + sLabel + "]";
int iDefinitionAt = sText.Length;
string sDefinition = BuildMarkdownFootnoteDefinition(sText, sLabel, sContent);
if (bToChapter) {
iDefinitionAt = iChapterEnd;
sDefinition = BuildMarkdownFootnoteDefinitionAt(sText, iChapterEnd, sLabel, sContent);
}

// Obie zmiany przez ReplaceRange, wiec Control+Z je cofa; zapisu na dysk tu
// nie ma - to regula z utraty pliku przy Format Code.
//
// KOLEJNOSC JEST ISTOTNA i to nie kosmetyka.  Tresc na koncu ROZDZIALU wpada
// w SRODEK dokumentu, wiec gdyby poszla pierwsza, przesunelaby wszystkie
// offsety za soba - w tym pozycje kursora, przez co znacznik wyladowalby w
// zlym miejscu.  Dlatego przy rozdziale wstawiamy najpierw ZNACZNIK, a tresc
// z offsetem poprawionym o jego dlugosc.  Przy koncu dokumentu zostaje stara
// kolejnosc, bo tam tresc nie rusza niczego przed soba.
if (bToChapter) {
rtb.ReplaceRange(iCursor, iCursor, sRef);
int iShifted = iDefinitionAt;
if (iShifted >= iCursor) iShifted += sRef.Length;
rtb.ReplaceRange(iShifted, iShifted, sDefinition);
}
else {
rtb.ReplaceRange(sText.Length, sText.Length, sDefinition);
rtb.ReplaceRange(iCursor, iCursor, sRef);
}
rtb.Index = iCursor + sRef.Length;
rtb.Modified = true;
// Mowimy, GDZIE wyladowala tresc, a nie tylko ze cokolwiek sie stalo: user
// wlasnie podjal te decyzje, wiec potwierdzenie jej jest tanie i rozstrzyga,
// czy klawisz zrobil to, co wybral.
AddMessage("Footnote " + sLabel + (bToChapter ? " inserted at end of chapter" : " inserted"));
} // InsertMarkdownFootnote method

// Skok kontekstowy w obie strony - ta sama zasada, ktora sam wybral dla spisu
// tresci: "Jestem za takimi kontekstowymi rozwiazaniami wielofunkcyjnymi".
// Ze zdania idziemy do tresci przypisu, a z tresci wracamy do zdania.
private void GoToMarkdownFootnoteOrBack(HomerRichTextBox rtb) {
if (rtb == null) return;
// TRYB GLOBALNY MOWY, BO CHORD MA CONTROL+ALT (zmierzone 30.08.2026).
// Komenda siedzi na Control+Alt+K, a Util.Say tlumi kazda mowe, gdy te dwa
// modyfikatory sa trzymane (l.17711).  Bez trybu globalnego skok BY SIE
// WYKONAL, a niewidomy nie uslyszalby ani tresci przypisu, ani odmowy - czyli
// nie do odroznienia od "klawisz nie dziala".  Ten sam zabieg co przy
// przesuwaniu sekcji na Control+Alt+Up (AnnounceSectionMoveMessage).
string sText = rtb.Text ?? "";
List<MarkdownFootnote> refs = GetMarkdownFootnoteRefs(sText);
List<MarkdownFootnote> defs = GetMarkdownFootnoteDefs(sText);
if (refs.Count == 0 && defs.Count == 0) {
AddMessage("No footnotes!", true);
return;
}

int iCursor = rtb.Index;
string sLine = GetTextLineAtIndex(sText, iCursor);
string sLabel, sBody;
if (IsMarkdownFootnoteDefinitionLine(sLine, out sLabel, out sBody)) {
int iRef = FindMarkdownFootnoteByLabel(refs, sLabel);
if (iRef < 0) {
AddMessage("Footnote " + sLabel + " has no marker in the text!", true);
return;
}
GoToMarkdownFootnoteRef(rtb, sText, refs[iRef], true);
return;
}

int iHere = FindMarkdownFootnoteRefAtIndex(refs, sText, iCursor);
if (iHere < 0) {
// KURSOR POZA PRZYPISEM: MOWIMY, CZEGO BRAKUJE, I NIE RUSZAMY KURSORA.
// W 5.0.57 ta sciezka szla do NASTEPNEGO znacznika, na jego wlasna prosbe
// z 31.08.  Odrzucil to po probie 01.09.2026: "jest za duzo klikania (...)
// z tego bysmy zrezygnowali".  Komunikat nazywa DRUGI klawisz, wiec nie
// jest to slepy zaulek: skok po przypisach ma teraz Control+Alt+PageDown
// i Control+Alt+PageUp.
AddMessage("Put the cursor in a line with a footnote marker, or use Control+Alt+PageDown to find one!", true);
return;
}
int iDef = FindMarkdownFootnoteByLabel(defs, refs[iHere].Label);
if (iDef < 0) {
AddMessage("Footnote " + refs[iHere].Label + " has no text at the end of the document!", true);
return;
}
rtb.Index = defs[iDef].Start;
// Forma wypowiedzi jak przy naglowkach i spisie tresci: najpierw tresc,
// potem czym ona jest - jego wybor, powtorzony tu swiadomie.
Util.Say(defs[iDef].Text + ", footnote " + defs[iDef].Label, true);
} // GoToMarkdownFootnoteOrBack method

// Parametr bGlobal istnieje WYLACZNIE z powodu bramki mowy przy Control+Alt.
// Skok kontekstowy (Control+Alt+K) MUSI wolac z true, bo inaczej milczy.
// Lista przypisow (Alt+K) wola z false SWIADOMIE: tryb globalny omija takze
// sprawdzenie, czy okno programu jest aktywne, a tego zabezpieczenia nie ma
// powodu zdejmowac tam, gdzie nie przeszkadza.
private void GoToMarkdownFootnoteRef(HomerRichTextBox rtb, string sText, MarkdownFootnote note, bool bGlobal) {
rtb.Index = note.Start;
string sLine = GetTextLineAtIndex(sText, note.Start).Trim();
if (sLine.Length == 0) sLine = "Footnote " + note.Label;
Util.Say(sLine + ", footnote " + note.Label + " in text", bGlobal);
} // GoToMarkdownFootnoteRef method

// Lista przypisow.  Enter idzie do ZNACZNIKA w zdaniu, bo tam sie pisze;
// gdy znacznika nie ma (tresc zostala sama), ladujemy na tresci.
private void ShowMarkdownFootnoteList(HomerRichTextBox rtb) {
if (rtb == null) return;
string sText = rtb.Text ?? "";
List<MarkdownFootnote> refs = GetMarkdownFootnoteRefs(sText);
List<MarkdownFootnote> defs = GetMarkdownFootnoteDefs(sText);
List<string> lsLabel = new List<string>();
List<string> lsShow = new List<string>();
foreach (MarkdownFootnote note in defs) {
lsLabel.Add(note.Label);
string sShow = note.Text;
if (sShow.Length == 0) sShow = "(no text)";
lsShow.Add(note.Label + ". " + sShow);
}
foreach (MarkdownFootnote note in refs) {
if (FindMarkdownFootnoteByLabel(defs, note.Label) >= 0) continue;
if (lsLabel.Contains(note.Label)) continue;
lsLabel.Add(note.Label);
lsShow.Add(note.Label + ". (no text at the end of the document)");
}
if (lsLabel.Count == 0) {
AddMessage("No footnotes!");
return;
}

string[] aFootValues = lsLabel.ToArray();
string[] aFootShow = lsShow.ToArray();
// WLASNE OKNO ZAMIAST Dialog.Pick: lista przypisow potrzebuje STRZALKI W LEWO,
// ktora czyta wiersz ze znacznikiem (jego prosba 01.09.2026: "na liscie
// przypisow mogla by czytac linie w ktorej jest przypis").  Pozycja niesie
// TRESC przypisu, wiec brakujaca informacja to zdanie, do ktorego przypis
// nalezy - dokladnie odwrotnie niz na liscie zakladek zwyklych.
LbcDialog dlgFoot = new LbcDialog("Footnotes", this);
ListBox lstFoot = dlgFoot.addListBox(new List<string>(aFootShow), "", "");
dlgFoot.setHelpDetail(lstFoot, "Keys: Left Arrow reads the sentence line where the footnote marker is, Enter goes to the marker in the text, Escape closes the list.");
LbcDialog.selectOnly(lstFoot, 0);

lstFoot.KeyDown += delegate(object oSender, KeyEventArgs ev) {
if (ev.KeyData != Keys.Left) return;
ListBox lb = oSender as ListBox;
if (lb == null) return;
ev.Handled = true; ev.SuppressKeyPress = true;
int iSel = lb.SelectedIndex;
if (iSel < 0 || iSel >= aFootValues.Length) return;
int iRefRead = FindMarkdownFootnoteByLabel(refs, aFootValues[iSel]);
if (iRefRead < 0) {
Say.sayForced("Footnote " + aFootValues[iSel] + " has no marker in the text");
return;
}
// ZNACZNIK ZDJETY Z CYTOWANEGO WIERSZA: cytat jest do sluchania, a numer
// przypisu stoi juz w wybranej pozycji listy.  Ta sama zasada, co przy
// powrocie z tresci przypisu w podgladzie.
string sMarkLine = GetTextLineAtIndex(sText, refs[iRefRead].Start).Trim();
try {sMarkLine = MarkdownFootnoteRefRegex.Replace(sMarkLine, MarkdownReview_FootnoteMarkerReplacement);} catch {}
sMarkLine = sMarkLine.Trim();
Say.sayForced(sMarkLine.Length == 0 ? "Empty line" : sMarkLine);
};

bool bFootOk = dlgFoot.runOkCancel();
int iFootPicked = lstFoot.SelectedIndex;
dlgFoot.Dispose();
if (!bFootOk) return;
if (iFootPicked < 0 || iFootPicked >= aFootValues.Length) return;
string sPicked = aFootValues[iFootPicked];
if (sPicked == null || sPicked.Length == 0) return;
int iRef = FindMarkdownFootnoteByLabel(refs, sPicked);
if (iRef >= 0) {
// Lista chodzi na Alt+K, bez Control, wiec bramka mowy jej nie dotyczy -
// zostaje tryb zwykly, ktory nadal pilnuje, czy okno jest aktywne.
GoToMarkdownFootnoteRef(rtb, sText, refs[iRef], false);
return;
}
int iDef = FindMarkdownFootnoteByLabel(defs, sPicked);
if (iDef < 0) return;
rtb.Index = defs[iDef].Start;
Util.Say(defs[iDef].Text + ", footnote " + defs[iDef].Label);
} // ShowMarkdownFootnoteList method

// WYBOR NASTEPNEGO ALBO POPRZEDNIEGO ZNACZNIKA - osobna metoda statyczna, bo
// tego samego wyboru uzywaja DWIE komendy (skok kontekstowy, gdy kursor stoi
// poza przypisem, oraz para skokow po przypisach), a rozjechanie ich znaczyloby,
// ze dwa klawisze rozumieja "nastepny przypis" inaczej.  Statyczna takze po to,
// zeby dala sie zmierzyc refleksja bez uruchamiania okna.
//
// BEZ ZAWIJANIA: zwraca -1, gdy po tej stronie kursora nic nie ma.  Wolajacy
// mowi wtedy, ze to koniec albo poczatek, i kursor stoi.
private static int FindMarkdownFootnoteMarkerIndex(List<MarkdownFootnote> refs, int iCursor, bool bForward) {
if (refs == null || refs.Count == 0) return -1;
if (bForward) {
for (int i = 0; i < refs.Count; i++) {
if (refs[i].Start > iCursor) return i;
}
return -1;
}
for (int i = refs.Count - 1; i >= 0; i--) {
if (refs[i].End <= iCursor) return i;
}
return -1;
} // FindMarkdownFootnoteMarkerIndex method

// SKOK PO ZNACZNIKACH PRZYPISOW W TEKSCIE (jego wiadomosc 31.08.2026 21:25).
// Osobna metoda, a nie parametr skoku kontekstowego, bo to inne pytanie:
// kontekstowy pyta "gdzie stoi kursor i gdzie jest druga strona TEGO
// przypisu", a ten pyta "gdzie jest NASTEPNY przypis w dokumencie".
//
// BEZ ZAWIJANIA, tak jak zakladki, komentarze i wyroznienia: niewidomy nie
// widzi, ze wrocil na poczatek dokumentu, i szukalby fragmentu, ktory juz
// przeczytal.  Zamiast tego mowimy "Last footnote!" i kursor stoi.
//
// Chodzimy po ZNACZNIKACH, nie po tresciach: tresci stoja wszystkie razem na
// koncu dokumentu albo rozdzialu, wiec skakanie po nich byloby chodzeniem po
// jednym akapicie, a to robi zwykla strzalka.  Do tresci prowadzi
// Control+Alt+K, ktore juz dziala.
private void GoToMarkdownFootnoteMarker(HomerRichTextBox rtb, bool bForward) {
if (rtb == null) return;
string sText = rtb.Text ?? "";
List<MarkdownFootnote> refs = GetMarkdownFootnoteRefs(sText);
if (refs.Count == 0) {
AddMessage("No footnotes!", true);
return;
}
int iCursor = rtb.Index;
if (iCursor < 0) iCursor = 0;

int iFound = FindMarkdownFootnoteMarkerIndex(refs, iCursor, bForward);
if (iFound < 0) {
AddMessage(bForward ? "Last footnote!" : "First footnote!", true);
return;
}
// Tryb globalny mowy, bo oba chordy tej pary maja Control+Alt (jeden z
// Shiftem) - Util.Say milczy przy tych modyfikatorach.
GoToMarkdownFootnoteRef(rtb, sText, refs[iFound], true);
} // GoToMarkdownFootnoteMarker method

// ZDANIE, W KTORYM STOI ZNACZNIK PRZYPISU.  Potrzebne przy eksporcie "przypisy
// ze zdaniami": bez zdania lista przypisow jest zbiorem uwag bez tego, do
// czego sie odnosza, a to wlasnie robi z niej material do czytania.
//
// GRANICE ZDANIA liczymy TYM SAMYM wzorcem, ktorego uzywa nawigacja po
// zdaniach w edytorze (App.MatchSentence: kropka, pytajnik albo wykrzyknik z
// bialym znakiem, albo pusty wiersz).  Wlasna definicja rozjechalaby sie z
// tym, co uzytkownik slyszy pod Alt ze strzalka.
//
// ZNACZNIK USUWAMY z cytowanego zdania (a nie zostawiamy "[^1]"), bo cytat
// jest do CZYTANIA - numer przypisu stoi juz obok, w wierszu wyniku.
private static string GetMarkdownSentenceAt(string sText, int iIndex) {
string s = sText ?? "";
if (s.Length == 0) return "";
if (iIndex < 0) iIndex = 0;
if (iIndex >= s.Length) iIndex = s.Length - 1;

// Poczatek: za ostatnim konczacym znakiem interpunkcyjnym albo za pustym
// wierszem przed kursorem.
int iStart = 0;
for (int i = iIndex; i > 0; i--) {
char c = s[i - 1];
if (c == '\n') {
// Pusty wiersz konczy akapit, wiec i zdanie.
int iBefore = i - 1;
// CR[0] zamiast literalu: narzedzia edycji tego pliku wstawiaja w miejsce
// literalu z odwrotnym ukosnikiem REALNY bajt konca wiersza i rozwalaja kod.
if (iBefore > 0 && s[iBefore - 1] == CR[0]) iBefore--;
if (iBefore == 0 || s[iBefore - 1] == '\n') {iStart = i; break;}
continue;
}
if ((c == '.' || c == '?' || c == '!') && i < s.Length && Char.IsWhiteSpace(s[i])) {
iStart = i;
break;
}
}
// Koniec: pierwszy konczacy znak za kursorem, albo koniec akapitu.
int iEnd = s.Length;
for (int i = iIndex; i < s.Length; i++) {
char c = s[i];
if (c == '.' || c == '?' || c == '!') {
if (i + 1 >= s.Length || Char.IsWhiteSpace(s[i + 1])) {iEnd = i + 1; break;}
continue;
}
if (c == '\n') {
int iAfter = i + 1;
if (iAfter < s.Length && s[iAfter] == CR[0]) iAfter++;
if (iAfter >= s.Length || s[iAfter] == '\n') {iEnd = i; break;}
}
}
if (iEnd <= iStart) return "";
string sSentence = s.Substring(iStart, iEnd - iStart);
// Znaczniki przypisow wychodza z cytatu, konce wiersza staja sie spacjami:
// zdanie w wyniku ma byc JEDNYM wierszem, zeby lista czytala sie rowno.
try {sSentence = MarkdownFootnoteRefRegex.Replace(sSentence, "");} catch {}
sSentence = sSentence.Replace(CR + "\n", " ").Replace(CR, " ").Replace("\n", " ");
while (sSentence.Contains("  ")) sSentence = sSentence.Replace("  ", " ");
return sSentence.Trim();
} // GetMarkdownSentenceAt method

// TRESC EKSPORTU PRZYPISOW.  Osobna metoda od okna, zeby dala sie zmierzyc
// refleksja na zbudowanej binarce bez klikania w okno dialogowe.
//
// KOLEJNOSC: wedle ZNACZNIKOW w tekscie, nie wedle etykiet.  Tak samo numeruje
// przypisy Markdown przy eksporcie, wiec czytajacy dostaje ten sam porzadek co
// w dokumencie docelowym - a etykiety moga byc dowolne i nieuporzadkowane.
//
// PRZYPIS BEZ ZNACZNIKA nie ginie po cichu: dopisujemy go na koniec z jawna
// adnotacja.  Cichy brak byl by dla niewidomego nie do wykrycia.
private static string BuildMarkdownFootnoteExport(string sText, bool bWithSentences) {
string s = sText ?? "";
List<MarkdownFootnote> refs = GetMarkdownFootnoteRefs(s);
List<MarkdownFootnote> defs = GetMarkdownFootnoteDefs(s);
StringBuilder sb = new StringBuilder();
List<string> lsDone = new List<string>();

foreach (MarkdownFootnote note in refs) {
if (lsDone.Contains(note.Label)) continue;
lsDone.Add(note.Label);
int iDef = FindMarkdownFootnoteByLabel(defs, note.Label);
string sBody = (iDef >= 0) ? defs[iDef].Text : "(no text at the end of the document)";
if (bWithSentences) {
string sSentence = GetMarkdownSentenceAt(s, note.Start);
if (sSentence.Length == 0) sSentence = "(no sentence)";
sb.Append(note.Label + ". " + sSentence + LF);
sb.Append("   " + sBody + LF + LF);
}
else {
sb.Append(note.Label + ". " + sBody + LF);
}
}
// Tresci, do ktorych nie ma znacznika - na koniec, z powodem.
bool bOrphanHeader = false;
foreach (MarkdownFootnote note in defs) {
if (lsDone.Contains(note.Label)) continue;
lsDone.Add(note.Label);
if (!bOrphanHeader) {
sb.Append(LF + "Footnotes with no marker in the text:" + LF);
bOrphanHeader = true;
}
string sBody = note.Text;
if (sBody.Length == 0) sBody = "(no text)";
sb.Append(note.Label + ". " + sBody + LF);
}
return sb.ToString();
} // BuildMarkdownFootnoteExport method

// EKSPORT PRZYPISOW DO NOWEGO OKNA - jego wiadomosc 31.08.2026 21:21: "To moze
// dwie opcje do wyboru.  Eksportuj przypisy, eksportuj przypisy ze zdaniami i
// zalatwi to sprawe".  Dwie opcje w JEDNYM okienku, bo to jedna decyzja.
//
// WYNIK IDZIE W NOWE OKNO EDYTORA, nie na dysk i nie do schowka.  Powody, po
// kolei: plik na dysku kazalby wymyslac za uzytkownika nazwe i miejsce, schowek
// da sie przeczytac tylko wklejajac, a nowe okno mozna od razu czytac strzalkami
// czytnika, zapisac pod dowolna nazwa albo skopiowac w calosci - i nie rusza
// dokumentu, z ktorego eksportujemy.  Ten sam wzorzec, co przy wyodrebnianiu
// wyrazeniem regularnym (Control+Shift+E), ktore w tym programie dziala tak od
// wersji autora.
private void ExportMarkdownFootnotes(HomerRichTextBox rtb) {
if (rtb == null) return;
string sText = rtb.Text ?? "";
List<MarkdownFootnote> refs = GetMarkdownFootnoteRefs(sText);
List<MarkdownFootnote> defs = GetMarkdownFootnoteDefs(sText);
// ODMOWA PRZED OKNEM, nie po nim: okno, ktore po zatwierdzeniu mowi "nie ma
// przypisow", kaze niewidomemu przejsc cala droge po nic.
if (refs.Count == 0 && defs.Count == 0) {
AddMessage("No footnotes!");
return;
}

LbcDialog dlg = new LbcDialog("Export Footnotes", this);
List<string> lKinds = new List<string>();
lKinds.Add("Footnotes only");
lKinds.Add("Footnotes with their sentences");
ComboBox cboKind = dlg.addComboPickBox("&What to export", lKinds, lKinds[0],
"Footnotes only gives a numbered list; with sentences each footnote is preceded by the sentence that carries it");
if (!dlg.runOkCancel()) {
dlg.Dispose();
return;
}
bool bWithSentences = (cboKind.SelectedIndex == 1);
dlg.Dispose();

string sExport = BuildMarkdownFootnoteExport(sText, bWithSentences);
if (sExport.Length == 0) {
AddMessage("No footnotes!");
return;
}

new MdiChild(this);
HomerRichTextBox rtbNew = App.Frame.Child.RTB;
if (rtbNew == null) {
AddMessage("Could not open a window for the export!");
return;
}
rtbNew.ReplaceRange(0, 0, sExport);
rtbNew.Index = 0;
rtbNew.Modified = true;
// Mowimy ILE i CZEGO, bo w nowym oknie kursor stoi na pierwszym wierszu i sam
// z siebie nie powie, czy eksport w ogole cos zawiera.
int iCount = 0;
List<string> lsSeen = new List<string>();
foreach (MarkdownFootnote note in refs) {if (!lsSeen.Contains(note.Label)) {lsSeen.Add(note.Label); iCount++;}}
foreach (MarkdownFootnote note in defs) {if (!lsSeen.Contains(note.Label)) {lsSeen.Add(note.Label); iCount++;}}
AddMessage(Util.Pluralize(iCount, "footnote", "footnotes") + " exported"
+ (bWithSentences ? " with sentences" : ""));
} // ExportMarkdownFootnotes method

// ==================== KOMENTARZE WEWNETRZNE ====================
//
// Punkt 4 mapy drogowej.  Jego zlecenie z 28.08.2026 00:20 i 00:24: "A
// komentarze, nie da sie robic Markdown komentarzy zgodnych potem z Wordem?
// Jakas tu ich sensowna obsluga?" oraz rozstrzygniecie "To na razie komentarze
// wewnetrzne, a pozniej komentarze dla Worda, ale to juz mocno pozniej".
//
// CZYM JEST KOMENTARZ WEWNETRZNY: uwaga robocza dla samego autora.  Zostaje w
// pliku Markdown, a przy zamianie na Worda, HTML czy tekst WYLATUJE.  To nie
// jest to samo co komentarz Worda na marginesie - tego on sam odlozyl na
// pozniej i tu go nie ma.
//
// Struktura opisujaca jeden komentarz.  Osobna klasa, a nie ponowne uzycie
// MarkdownFootnote, bo komentarz nie ma etykiety ani pary znacznik-tresc -
// wspolne pole "Label" byloby martwe i mylace przy czytaniu kodu.
private class MarkdownComment {
public int Start;
public int End;
public string Text;
} // MarkdownComment class

// Wszystkie komentarze w dokumencie, w kolejnosci wystepowania.
//
// BLOK KODU POMIJAMY tym samym parserem ogrodzen, co naglowki i przypisy.
// Powod jest zmierzony, nie teoretyczny: w bloku kodu <!-- ... --> to
// PRZYKLAD HTML-a i nasz wlasny podglad 2htm zostawia go jako widoczny tekst
// (zmierzone: "blok kodu z &lt;!-- komentarzem w kodzie --&gt;").  Czyli tam
// to NIE jest komentarz i skakanie po nim wprowadzalo by w blad.
private static List<MarkdownComment> GetMarkdownComments(string sText) {
List<MarkdownComment> comments = new List<MarkdownComment>();
if (String.IsNullOrEmpty(sText)) return comments;
List<int[]> fences = MarkdownReview_FindFenceRanges(sText);
MatchCollection ms = null;
try {ms = MarkdownCommentRegex.Matches(sText);} catch {return comments;}
foreach (Match m in ms) {
if (IsMarkdownIndexInFence(fences, m.Index)) continue;
MarkdownComment c = new MarkdownComment();
c.Start = m.Index;
c.End = m.Index + m.Length;
c.Text = MarkdownReview_CollapseWhitespace(m.Groups["body"].Value ?? "").Trim();
comments.Add(c);
}
return comments;
} // GetMarkdownComments method

// Komentarz, W KTORYM stoi kursor, albo -1.  Kursor NA SAMYM koncu komentarza
// tez sie liczy: uzytkownik, ktory dopiero wstawil komentarz, stoi dokladnie
// za nim, a wtedy "popraw ten komentarz" musi znalezc ten, ktory widzi.
private static int FindMarkdownCommentAtIndex(List<MarkdownComment> comments, int iIndex) {
if (comments == null) return -1;
for (int i = 0; i < comments.Count; i++) {
if (iIndex >= comments[i].Start && iIndex <= comments[i].End) return i;
}
return -1;
} // FindMarkdownCommentAtIndex method

// Tekst komentarza dla czytnika.  Pusty komentarz nazywamy wprost, bo inaczej
// czytnik mowilby samo slowo "komentarz" i nie dalo by sie odroznic pustego od
// awarii odczytu.
// ==================== ZAKLADKI Z NAZWA: MAGAZYN ====================
//
// SEKCJA NamedBookmarks, klucz "<sciezka pliku>|<numer wiersza>" = nazwa.
// DLACZEGO KLUCZ NA ZAKLADKE, A NIE JEDNA WARTOSC NA PLIK: Ini.ReadValue idzie
// przez GetPrivateProfileString z buforem 260 znakow, wiec dluga lista nazw
// zostalaby UCIETA w polowie i to bez komunikatu.  Wyliczanie kluczy sekcji
// (Ini.ReadSectionKeys) czyta plik wlasnym parserem przez Util.IniFile2String,
// ktory radzi sobie z ANSI - a wiec z polskimi znakami w sciezce i w nazwie.
//
// NUMER WIERSZA, A NIE OFFSET ZNAKU: zwykle zakladki trzymaja offset i po
// edycji tekstu wyzej wskazuja inne miejsce.  Zakladka z nazwa ma byc trwalym
// znacznikiem miejsca w dokumencie, wiec wiersz jest odporniejszy: dopisanie
// slowa w akapicie go nie rusza.
public static string NamedBookmarkKey(string sFile, int iRow) {
return (sFile ?? "") + "|" + iRow.ToString();
} // NamedBookmarkKey method

private static string ReadNamedBookmark(string sFile, int iRow) {
string s = App.ReadValue("NamedBookmarks", NamedBookmarkKey(sFile, iRow), "");
return (s ?? "").Trim();
} // ReadNamedBookmark method

private static void WriteNamedBookmark(string sFile, int iRow, string sName) {
App.WriteValue("NamedBookmarks", NamedBookmarkKey(sFile, iRow), sName);
} // WriteNamedBookmark method

private static void DeleteNamedBookmark(string sFile, int iRow) {
App.DeleteKey("NamedBookmarks", NamedBookmarkKey(sFile, iRow));
} // DeleteNamedBookmark method

// Wszystkie zakladki z nazwa dla podanego pliku, posortowane po numerze
// wiersza.  Klucze innych plikow pomijamy porownaniem sciezki BEZ wzgledu na
// wielkosc liter (Windows), a klucz bez poprawnego numeru wiersza ignorujemy -
// recznie zepsuty plik ustawien nie ma prawa wywalic listy.
private static void ReadNamedBookmarks(string sFile, List<int> listRows, List<string> listNames) {
listRows.Clear();
listNames.Clear();
string[] aKeys = App.ReadSectionKeys("NamedBookmarks");
if (aKeys == null) return;
List<int> listFound = new List<int>();
foreach (string sKey in aKeys) {
if (sKey == null) continue;
int iBar = sKey.LastIndexOf('|');
if (iBar <= 0 || iBar >= sKey.Length - 1) continue;
string sKeyFile = sKey.Substring(0, iBar);
if (!Util.Equiv(sKeyFile, sFile)) continue;
int iRow;
if (!Int32.TryParse(sKey.Substring(iBar + 1).Trim(), out iRow)) continue;
if (iRow < 0) continue;
if (listFound.Contains(iRow)) continue;
string sName = App.ReadValue("NamedBookmarks", sKey, "");
if (sName == null) sName = "";
sName = sName.Trim();
if (sName.Length == 0) continue;
listFound.Add(iRow);
}
listFound.Sort();
foreach (int iRow in listFound) {
listRows.Add(iRow);
listNames.Add(ReadNamedBookmark(sFile, iRow));
}
} // ReadNamedBookmarks method

private static string GetMarkdownCommentSpeech(MarkdownComment c) {
if (c == null) return "comment";
string s = (c.Text ?? "").Trim();
if (s.Length == 0) return "empty comment";
return s + ", comment";
} // GetMarkdownCommentSpeech method

// Wstawienie NOWEGO komentarza albo poprawka tego, w ktorym stoi kursor.
//
// JEDEN KLAWISZ NA DWIE RZECZY - ta sama zasada, ktora sam wybral dla kreatora
// tabeli i dla skoku po przypisach ("Jestem za takimi kontekstowymi
// rozwiazaniami wielofunkcyjnymi").  Rozstrzyga POLOZENIE KURSORA: stoi w
// komentarzu, wiec go poprawiamy; stoi gdziekolwiek indziej, wiec wstawiamy
// nowy.  Tytul okna mowi ktory tryb, bo czytnik czyta tytul przy otwarciu i to
// jedyne miejsce, w ktorym uzytkownik dowie sie tego bez pytania.
private void InsertOrEditMarkdownComment(HomerRichTextBox rtb) {
if (rtb == null) return;
string sText = rtb.Text ?? "";
int iCursor = rtb.Index;
if (iCursor < 0) iCursor = 0;
if (iCursor > sText.Length) iCursor = sText.Length;

List<MarkdownComment> comments = GetMarkdownComments(sText);
int iHere = FindMarkdownCommentAtIndex(comments, iCursor);
bool bEdit = (iHere >= 0);

// BRAMKA NA BLOK KODU, ale TYLKO przy wstawianiu nowego.  Powod jest ten sam,
// co przy przypisach i zmierzony na naszym konwerterze: komentarz wstawiony w
// blok kodu NIE jest komentarzem, bo 2htm wypisuje go jako widoczny tekst
// przykladu.  Komunikat "comment inserted" klamalby.
if (!bEdit) {
List<int[]> fences = MarkdownReview_FindFenceRanges(sText);
if (IsMarkdownIndexInFence(fences, iCursor)) {
AddMessage("Cannot put a comment inside a code block!");
return;
}
}

LbcDialog dlg = new LbcDialog(bEdit ? "Edit Comment" : "Insert Comment", this);
TextBox txtBody = dlg.addInputBox("Comment &text", bEdit ? comments[iHere].Text : "");
if (!dlg.runOkCancel()) {
dlg.Dispose();
return;
}
string sBody = (txtBody.Text ?? "").Trim();
dlg.Dispose();

// PUSTA TRESC PRZY POPRAWCE USUWA KOMENTARZ.  To decyzja, nie przeoczenie:
// wyczyszczenie pola jest naturalnym "nie chce go tu", a osobny klawisz do
// usuwania zajmowalby chord za rzecz robiona raz na kilka dni.  Mowimy o tym
// wprost, bo cicha utrata tekstu byla by dla niewidomego nie do wykrycia.
if (bEdit && sBody.Length == 0) {
rtb.ReplaceRange(comments[iHere].Start, comments[iHere].End, "");
rtb.Index = comments[iHere].Start;
rtb.Modified = true;
AddMessage("Comment removed");
return;
}
if (sBody.Length == 0) {
AddMessage("No comment text, nothing inserted!");
return;
}

// Tresc zabezpieczamy: dwa myslniki i nawias zamykajacy w SRODKU komentarza
// zamknelyby go za wczesnie, a reszta uwagi wysypalaby sie do dokumentu jako
// widoczny tekst.  Zmiana przez ReplaceRange, wiec Control+Z cofa calosc.
string sSafe = SanitizeMarkdownCommentBody(sBody);
string sMarkup = "<!-- " + sSafe + " -->";
if (bEdit) {
rtb.ReplaceRange(comments[iHere].Start, comments[iHere].End, sMarkup);
rtb.Index = comments[iHere].Start + sMarkup.Length;
rtb.Modified = true;
AddMessage("Comment changed");
return;
}
rtb.ReplaceRange(iCursor, iCursor, sMarkup);
rtb.Index = iCursor + sMarkup.Length;
rtb.Modified = true;
AddMessage("Comment inserted");
} // InsertOrEditMarkdownComment method

// Zabezpieczenie tresci komentarza.  "-->" w srodku ZAMKNELOBY komentarz i
// wypchnelo resztke uwagi do widocznego dokumentu, wiec zamieniamy go na wersje
// nieszkodliwa.  Odwrotnosci nie ma i byc nie moze: to jest ochrona pliku, nie
// kodowanie znakow.
private static string SanitizeMarkdownCommentBody(string sBody) {
string s = sBody ?? "";
// Kolejnosc ma znaczenie: najpierw pelny "-->", potem sam ciag myslnikow,
// inaczej pierwsza zamiana rozbila by wzorzec drugiej.
s = s.Replace("-->", "- - >");
while (s.Contains("--")) s = s.Replace("--", "- -");
return s;
} // SanitizeMarkdownCommentBody method

// Skok do nastepnego albo poprzedniego ODSYLACZA (ustalenie edsharpng-36,
// Alt+PageDown i Alt+PageUp).  Parser to TA SAMA metoda GetMarkdownLinks,
// ktora zasila liste pod Control+F6 - dwie komendy o jednym zrodle prawdy nie
// moga sie rozjechac po pierwszej poprawce parsera.
//
// BEZ ZAWIJANIA na koncu dokumentu, jak przy zakladkach, przypisach,
// komentarzach, wyroznieniach i listach: niewidomy nie widzi, ze wrocil na
// poczatek, i szukalby odsylacza, ktory juz czytal.
private void GoToMarkdownLink(HomerRichTextBox rtb, bool bForward) {
if (rtb == null) return;
string sText = rtb.Text ?? "";
List<MarkdownLink> links = GetMarkdownLinks(sText);
if (links.Count == 0) {
AddMessage("No links!");
return;
}
int iCursor = rtb.Index;
if (iCursor < 0) iCursor = 0;

int iFound = -1;
if (bForward) {
for (int i = 0; i < links.Count; i++) {
if (links[i].TextStart > iCursor) {iFound = i; break;}
}
if (iFound < 0) {
AddMessage("Last link!");
return;
}
}
else {
for (int i = links.Count - 1; i >= 0; i--) {
if (links[i].End < iCursor) {iFound = i; break;}
}
if (iFound < 0) {
AddMessage("First link!");
return;
}
}

// KURSOR STAJE NA TRESCI (TextStart), nie na nawiasie kwadratowym ani na
// wykrzykniku obrazka.  Postawienie go na Start dalo by komende, ktora
// dziala, a czytnik czytal by niewidomemu znaki skladni - ten sam wymog, co
// przy liscie odsylaczow i przy skoku po wyroznieniach.
MarkdownLink found = links[iFound];
rtb.Index = found.TextStart;
// Mowa jak wszedzie w tym programie: najpierw TRESC, potem czym ona jest.
// Ta sama metoda, ktora buduje wiersz listy odsylaczow, wiec skok i lista
// mowia o tym samym odsylaczu identycznie.
Util.Say(GetMarkdownLinkSpeech(found));
} // GoToMarkdownLink method

// Skok do nastepnego albo poprzedniego komentarza.  Na ostatnim (pierwszym)
// zostaje w miejscu i mowi o tym - ta sama zasada, ktora dostaly zakladki na
// Shift+PageDown i Shift+PageUp: bez zawijania, bo niewidomy nie widzi, ze
// wrocil na poczatek, i szukalby uwagi, ktora juz czytal.
private void GoToMarkdownComment(HomerRichTextBox rtb, bool bForward) {
if (rtb == null) return;
string sText = rtb.Text ?? "";
List<MarkdownComment> comments = GetMarkdownComments(sText);
if (comments.Count == 0) {
AddMessage("No comments!");
return;
}
int iCursor = rtb.Index;
if (iCursor < 0) iCursor = 0;

int iFound = -1;
if (bForward) {
for (int i = 0; i < comments.Count; i++) {
if (comments[i].Start > iCursor) {iFound = i; break;}
}
if (iFound < 0) {
AddMessage("Last comment!");
return;
}
}
else {
for (int i = comments.Count - 1; i >= 0; i--) {
if (comments[i].End < iCursor) {iFound = i; break;}
}
if (iFound < 0) {
AddMessage("First comment!");
return;
}
}
rtb.Index = comments[iFound].Start;
// Forma wypowiedzi jak przy naglowkach, spisie tresci i przypisach: najpierw
// tresc, potem czym ona jest - jego wybor, powtorzony tu swiadomie.
Util.Say(GetMarkdownCommentSpeech(comments[iFound]));
} // GoToMarkdownComment method

// ==================== NAWIGACJA PO WYROZNIENIACH I LISTACH ====================
//
// Ustalenia edsharpng-45 i edsharpng-46 (jego rozstrzygniecie 30.08.2026
// 15:41-15:42).  Control z ukosnikiem szuka WYROZNIENIA, czyli pogrubienia
// ALBO pochylenia w jednej komendzie; Control z myslnikiem szuka POCZATKU
// LISTY.  Shift odwraca kierunek, jak w kazdej innej parze skokow programu.

private class MarkdownEmphasis {
public int Start;      // pierwszy znacznik (pierwsza gwiazdka albo podkreslnik)
public int TextStart;  // pierwszy znak TRESCI
public int TextEnd;    // za ostatnim znakiem tresci
public int End;        // za ostatnim znacznikiem
public int Level;      // 1 pochylenie, 2 pogrubienie, 3 jedno i drugie
public char Marker;    // '*' albo '_'
} // MarkdownEmphasis class

// Wyroznienia w tekscie.  Parser wlasny, nie wyrazenie regularne: liczba
// znacznikow rozstrzyga rodzaj (jeden pochylenie, dwa pogrubienie, trzy oba),
// a przy wyrazeniu regularnym trojka i dwojka wchodza sobie w droge.
// Trzy rzeczy, ktore MUSZA byc pomijane, bo inaczej komenda skakalaby na
// smieci:
//   1. bloki kodu - w przykladzie kodu gwiazdka jest gwiazdka, nie pogrubieniem
//      (ten sam parser ogrodzen co przy naglowkach i przypisach),
//   2. punktor listy - wiersz "* pozycja" zaczyna sie gwiazdka, ktora NIE
//      otwiera wyroznienia; Markdown wymaga, by po znaczniku otwierajacym
//      NIE bylo bialego znaku,
//   3. podkreslnik W SRODKU SLOWA - nazwa_pliku_txt to jedno slowo, nie
//      pochylenie; dlatego '_' liczy sie tylko na granicy slowa.
private static List<MarkdownEmphasis> GetMarkdownEmphases(string sText) {
List<MarkdownEmphasis> notes = new List<MarkdownEmphasis>();
if (String.IsNullOrEmpty(sText)) return notes;
List<int[]> fences = MarkdownReview_FindFenceRanges(sText);
int iLen = sText.Length;
int i = 0;
while (i < iLen) {
char c = sText[i];
if (c != '*' && c != '_') {i++; continue;}
if (IsMarkdownIndexInFence(fences, i)) {i++; continue;}

// Ile znacznikow z rzedu, do trzech.
int iRun = 0;
while (i + iRun < iLen && sText[i + iRun] == c && iRun < 3) iRun++;
int iTextStart = i + iRun;
if (iTextStart >= iLen) break;

// Znacznik otwierajacy nie moze miec po sobie bialego znaku - to wlasnie
// odsiewa punktor listy ("* pozycja") i mnozenie w zdaniu.
if (Char.IsWhiteSpace(sText[iTextStart])) {i = iTextStart; continue;}

// Podkreslnik tylko na granicy slowa, zeby nazwa_z_podkresleniami nie
// udawala pochylenia.
if (c == '_' && i > 0) {
char cBefore = sText[i - 1];
if (Char.IsLetterOrDigit(cBefore)) {i = iTextStart; continue;}
}

// Domkniecie: ten sam znak, ta sama liczba, bez bialego znaku PRZED nim.
int iClose = -1;
int j = iTextStart;
while (j < iLen) {
if (sText[j] == '\n') break;   // wyroznienie nie przechodzi przez wiersz
if (sText[j] != c) {j++; continue;}
int iRunClose = 0;
while (j + iRunClose < iLen && sText[j + iRunClose] == c) iRunClose++;
if (iRunClose >= iRun && !Char.IsWhiteSpace(sText[j - 1])) {iClose = j; break;}
j += (iRunClose > 0) ? iRunClose : 1;
}
if (iClose < 0) {i = iTextStart; continue;}

MarkdownEmphasis note = new MarkdownEmphasis();
note.Start = i;
note.TextStart = iTextStart;
note.TextEnd = iClose;
note.End = iClose + iRun;
note.Level = iRun;
note.Marker = c;
notes.Add(note);
i = note.End;
}
return notes;
} // GetMarkdownEmphases method

// Poczatki list.  Ten sam pomiar, ktory czyta lista elementow w podgladzie
// (MarkdownReview_ParseText), wiec obie funkcje nie moga sie rozjechac:
// POCZATEK listy to pozycja, ktorej nie poprzedza inna pozycja tej listy.
private static List<int> GetMarkdownListStarts(string sText) {
List<int> starts = new List<int>();
if (String.IsNullOrEmpty(sText)) return starts;
List<int> headings, lists, items, links, tables;
List<int[]> inlineLinks;
MarkdownReview_ParseText(sText, out headings, out lists, out items, out links, out tables, out inlineLinks);
if (lists != null) starts.AddRange(lists);
return starts;
} // GetMarkdownListStarts method

// Nazwa rodzaju wyroznienia, mowiona po tresci - ta sama kolejnosc, co przy
// naglowkach, przypisach i komentarzach: najpierw TRESC, potem czym ona jest.
private static string GetMarkdownEmphasisKind(MarkdownEmphasis note) {
if (note == null) return "";
if (note.Level >= 3) return "bold italic";
if (note.Level == 2) return "bold";
return "italic";
} // GetMarkdownEmphasisKind method

// Skok do nastepnego albo poprzedniego wyroznienia.  Bez zawijania, tak jak
// zakladki i komentarze: niewidomy nie widzi, ze wrocil na poczatek, i szukal
// by fragmentu, ktory juz czytal.
private void GoToMarkdownEmphasis(HomerRichTextBox rtb, bool bForward) {
if (rtb == null) return;
string sText = rtb.Text ?? "";
List<MarkdownEmphasis> notes = GetMarkdownEmphases(sText);
if (notes.Count == 0) {
AddMessage("No bold or italic text!");
return;
}
int iCursor = rtb.Index;
if (iCursor < 0) iCursor = 0;

int iFound = -1;
if (bForward) {
for (int i = 0; i < notes.Count; i++) {
if (notes[i].Start > iCursor) {iFound = i; break;}
}
if (iFound < 0) {
AddMessage("Last bold or italic text!");
return;
}
}
else {
for (int i = notes.Count - 1; i >= 0; i--) {
if (notes[i].End < iCursor) {iFound = i; break;}
}
if (iFound < 0) {
AddMessage("First bold or italic text!");
return;
}
}

// Kursor staje na TRESCI, nie na gwiazdce: jego slowa z 30.08 15:41 ("sam
// pogrubiony fragment") i potwierdzenie, ze gwiazdek slyszec nie chce.
MarkdownEmphasis found = notes[iFound];
rtb.Index = found.TextStart;
string sBody = "";
try {sBody = sText.Substring(found.TextStart, found.TextEnd - found.TextStart);} catch {}
Util.Say(sBody + ", " + GetMarkdownEmphasisKind(found));
} // GoToMarkdownEmphasis method

// Skok do nastepnej albo poprzedniej LISTY, na jej POCZATEK.  Jego decyzja
// z 30.08.2026 15:42 (ustalenie edsharpng-46): "Na poczatek kazdej nowej
// listy, bo po elementach mozna chodzic strzalka w dol".  Czyli komenda
// przenosi miedzy listami, a wewnatrz listy pracuje zwykla strzalka.
private void GoToMarkdownList(HomerRichTextBox rtb, bool bForward) {
if (rtb == null) return;
string sText = rtb.Text ?? "";
List<int> starts = GetMarkdownListStarts(sText);
if (starts.Count == 0) {
AddMessage("No lists!");
return;
}
int iCursor = rtb.Index;
if (iCursor < 0) iCursor = 0;

int iFound = -1;
if (bForward) {
for (int i = 0; i < starts.Count; i++) {
if (starts[i] > iCursor) {iFound = i; break;}
}
if (iFound < 0) {
AddMessage("Last list!");
return;
}
}
else {
for (int i = starts.Count - 1; i >= 0; i--) {
if (starts[i] < iCursor) {iFound = i; break;}
}
if (iFound < 0) {
AddMessage("First list!");
return;
}
}

rtb.Index = starts[iFound];
// Mowa mowi TRESC pierwszej pozycji i ile ich jest w tej liscie - sam
// punktor nie powiedzialby niewidomemu nic o tym, gdzie wlasnie stanal.
string sLine = GetTextLineAtIndex(sText, starts[iFound]).Trim();
int iCount = CountMarkdownListItemsFrom(sText, starts[iFound]);
Util.Say(GetMarkdownPlainText(sLine) + ", list of " + iCount + (iCount == 1 ? " item" : " items"));
} // GoToMarkdownList method

// Ile pozycji ma lista zaczynajaca sie w tym miejscu.  Pusty wiersz konczy
// liste, tak samo jak w parserze podgladu, wiec obie liczby sie zgadzaja.
private static int CountMarkdownListItemsFrom(string sText, int iStart) {
if (String.IsNullOrEmpty(sText)) return 0;
int iCount = 0;
int iPos = iStart;
while (iPos < sText.Length) {
int iLf = sText.IndexOf('\n', iPos);
int iEnd = (iLf >= 0) ? iLf : sText.Length;
int iTextEnd = iEnd;
if (iTextEnd > iPos && sText[iTextEnd - 1] == '\r') iTextEnd--;
string sLine = sText.Substring(iPos, iTextEnd - iPos);
if (MarkdownReview_IsListItemLine(sLine)) iCount++;
else if (sLine.Trim().Length == 0) break;
else if (iCount > 0 && sLine.TrimStart().Length == sLine.Length) break;
if (iLf < 0) break;
iPos = iLf + 1;
}
return iCount;
} // CountMarkdownListItemsFrom method

// Lista komentarzy.  Enter idzie na komentarz w tekscie, bo tam sie pracuje.
private void ShowMarkdownCommentList(HomerRichTextBox rtb) {
if (rtb == null) return;
string sText = rtb.Text ?? "";
List<MarkdownComment> comments = GetMarkdownComments(sText);
if (comments.Count == 0) {
AddMessage("No comments!");
return;
}
string[] aValues = new string[comments.Count];
string[] aShow = new string[comments.Count];
for (int i = 0; i < comments.Count; i++) {
aValues[i] = comments[i].Start.ToString();
string sShow = comments[i].Text;
if (sShow.Length == 0) sShow = "(empty)";
// Numer wiersza, a nie numer komentarza po kolei: uzytkownik szuka miejsca
// w dokumencie, a nie tego, ktory komentarz napisal jako trzeci.
int iLine = GetTextLineNumberAtIndex(sText, comments[i].Start);
aShow[i] = "Line " + iLine + ". " + sShow;
}
string sPicked = Dialog.Pick("Comments", aValues, aShow, false, 0);
if (sPicked == null || sPicked.Length == 0) return;
int iAt;
if (!Int32.TryParse(sPicked, out iAt)) return;
if (iAt < 0 || iAt > sText.Length) return;
rtb.Index = iAt;
int iIdx = FindMarkdownCommentAtIndex(comments, iAt);
if (iIdx >= 0) Util.Say(GetMarkdownCommentSpeech(comments[iIdx]));
} // ShowMarkdownCommentList method

// Numer wiersza (od jedynki) dla podanego offsetu.  Liczy TYLKO znaki konca
// wiersza LF, bo kontrolka edycyjna trzyma tekst wlasnie w tej postaci -
// zmierzone przy koncach wiersza w 5.0.37.
private static int GetTextLineNumberAtIndex(string sText, int iIndex) {
string s = sText ?? "";
int iAt = iIndex;
if (iAt < 0) iAt = 0;
if (iAt > s.Length) iAt = s.Length;
int iLine = 1;
for (int i = 0; i < iAt; i++) {
if (s[i] == '\n') iLine++;
}
return iLine;
} // GetTextLineNumberAtIndex method

// Poczatek wiersza, w ktorym WIDAC karetke.  Zmierzone 27.08.2026 przy
// przypisach: gdy kursor stoi na KONCU wiersza, jego pozycja jest rowna
// indeksowi znaku lamania, a wtedy zwykle szukanie w tyl znajduje TEN znak i
// wypada o wiersz za daleko.  Dla uzytkownika karetka jest jednak nadal w
// wierszu, ktory sie tu konczy - koniec wiersza to najczestsze miejsce
// kursora, bo tam sie przestaje pisac.  Ta jedna poprawka leczy tez skok po
// spisie tresci (Shift+F6), ktory na koncu pozycji spisu mowil "Put the
// cursor on a contents item!" zamiast skakac.
private static int GetTextLineStartAtIndex(string sText, int iIndex) {
if (String.IsNullOrEmpty(sText)) return 0;
if (iIndex < 0) iIndex = 0;
if (iIndex > sText.Length) iIndex = sText.Length;
int iSearch = iIndex;
if (iSearch > 0 && iSearch < sText.Length && (sText[iSearch] == '\n' || sText[iSearch] == '\r')) iSearch = iSearch - 1;
if (iSearch <= 0) return 0;
int iNewLine = sText.LastIndexOf('\n', Math.Min(iSearch, sText.Length - 1));
return iNewLine + 1;
} // GetTextLineStartAtIndex method

private static string GetTextLineAtIndex(string sText, int iIndex) {
if (String.IsNullOrEmpty(sText)) return "";
int iStart = GetTextLineStartAtIndex(sText, iIndex);
int iEnd = sText.IndexOf('\n', iStart);
if (iEnd < 0) iEnd = sText.Length;
string sLine = sText.Substring(iStart, iEnd - iStart);
if (sLine.EndsWith("\r")) sLine = sLine.Substring(0, sLine.Length - 1);
return sLine;
} // GetTextLineAtIndex method

				private static List<MarkdownSectionHeading> GetMarkdownSectionHeadings(string sText) {
				List<MarkdownSectionHeading> headings = new List<MarkdownSectionHeading>();
				if (String.IsNullOrEmpty(sText)) return headings;

				List<int[]> fences = MarkdownReview_FindFenceRanges(sText);

				int iStart = 0;
				while (iStart <= sText.Length) {
				int iNewLine = sText.IndexOf('\n', iStart);
				int iEnd = (iNewLine >= 0) ? iNewLine : sText.Length;
				string sLine = sText.Substring(iStart, iEnd - iStart);
				if (sLine.EndsWith("\r")) sLine = sLine.Substring(0, sLine.Length - 1);

				bool bInFence = false;
				for (int i = 0; i < fences.Count; i++) {
				if (iStart >= fences[i][0] && iStart < fences[i][1]) {bInFence = true; break;}
				}

				int iLevel;
				string sTitle;
				if (!bInFence && TryGetMarkdownSectionHeadingLine(sLine, out iLevel, out sTitle)) {
				headings.Add(new MarkdownSectionHeading {Start = iStart, Level = iLevel, Title = sTitle});
				}

				if (iNewLine < 0) break;
				iStart = iNewLine + 1;
				}

				return headings;
				} // GetMarkdownSectionHeadings method

				private static bool TryGetMarkdownSectionHeadingLine(string sLine, out int iLevel, out string sTitle) {
				iLevel = 0;
				sTitle = "";
				if (String.IsNullOrEmpty(sLine)) return false;
				if (MarkdownReview_IsHeadingLine(sLine, out iLevel, out sTitle)) return true;

				string sTrim = sLine.TrimStart();
				int iLeading = sLine.Length - sTrim.Length;
				if (iLeading > 3) return false;

				int i = 0;
				while (i < sTrim.Length && sTrim[i] == '#' && i < 6) i++;
				if (i == 0 || i >= sTrim.Length) return false;

				sTitle = sTrim.Substring(i).Trim();
				if (sTitle.Length == 0) return false;
				try {sTitle = Regex.Replace(sTitle, @"\s+#+\s*$", "");} catch {}
				sTitle = Regex.Replace(sTitle, @"\s+", " ").Trim();
				if (sTitle.Length == 0) return false;
				iLevel = i;
				return true;
				} // TryGetMarkdownSectionHeadingLine method

					private static string GetSectionAnnouncementTitle(string sSection) {
			if (String.IsNullOrEmpty(sSection)) return "section";
			string[] aLines = sSection.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
			foreach (string sLine in aLines) {
			string s = sLine.Replace(FF, "").Trim();
			if (s.Length == 0) continue;
			s = MarkdownHeadingPrefixRegex.Replace(s, "");
			s = MarkdownHeadingSuffixRegex.Replace(s, "");
			s = MarkdownBulletPrefixRegex.Replace(s, "");
			s = MarkdownNumberPrefixRegex.Replace(s, "");
			s = Regex.Replace(s, @"\s+", " ").Trim();
			if (s.Length > 0) return s;
			}
			return "section";
			} // GetSectionAnnouncementTitle method

			private static List<int> GetSectionStarts(string sText) {
		List<int> starts = new List<int>();
		starts.Add(0);
		if (String.IsNullOrEmpty(sText)) return starts;

		int iSearch = 0;
		while (iSearch < sText.Length) {
		int i = sText.IndexOf(SB, iSearch, StringComparison.Ordinal);
		if (i < 0) break;
		if (!starts.Contains(i)) starts.Add(i);
		iSearch = i + SB.Length;
		}
		starts.Sort();
		return starts;
		} // GetSectionStarts method

		private static bool MarkdownReview_IsHeadingLine(string sLine, out int iLevel, out string sHeading) {
	iLevel = 0;
	sHeading = "";
	if (String.IsNullOrEmpty(sLine)) return false;

	string sTrim = sLine.TrimStart();
	int iLeading = sLine.Length - sTrim.Length;
	if (iLeading > 3) return false;

	int i = 0;
	while (i < sTrim.Length && sTrim[i] == '#' && i < 6) i++;
	if (i == 0) return false;
	if (i >= sTrim.Length) return false;
	if (!Char.IsWhiteSpace(sTrim[i])) return false;

	iLevel = i;
	sHeading = sTrim.Substring(i).Trim();
	// Strip optional closing sequence of hashes, e.g. "## Heading ##"
	try {
	sHeading = Regex.Replace(sHeading, @"\s+#+\s*$", "");
	}
	catch {}
	sHeading = sHeading.Trim();
	return true;
	} // MarkdownReview_IsHeadingLine method

	private static bool MarkdownReview_IsIndexInAnyInlineLinkSpan(List<int[]> inlineLinks, int iIndex) {
	if (inlineLinks == null || inlineLinks.Count == 0) return false;
	int lo = 0;
	int hi = inlineLinks.Count - 1;
	while (lo <= hi) {
	int mid = (lo + hi) / 2;
	int[] span = inlineLinks[mid];
	if (span == null || span.Length < 2) return false;
	if (iIndex < span[0]) hi = mid - 1;
	else if (iIndex >= span[1]) lo = mid + 1;
	else return true;
	}
	return false;
	} // MarkdownReview_IsIndexInAnyInlineLinkSpan method

	private static bool MarkdownReview_IsIndexInAnySpan(int iIndex, List<int[]> spans) {
	if (spans == null) return false;
	foreach (int[] span in spans) {
	if (span == null || span.Length < 2) continue;
	if (iIndex >= span[0] && iIndex < span[1]) return true;
	}
	return false;
	} // MarkdownReview_IsIndexInAnySpan method

	private static bool MarkdownReview_IsIndexInRangesSorted(List<int[]> ranges, int iIndex) {
	if (ranges == null || ranges.Count == 0) return false;
	int lo = 0;
	int hi = ranges.Count - 1;
	while (lo <= hi) {
	int mid = (lo + hi) / 2;
	int[] span = ranges[mid];
	if (span == null || span.Length < 2) return false;
	if (iIndex < span[0]) hi = mid - 1;
	else if (iIndex >= span[1]) lo = mid + 1;
	else return true;
	}
	return false;
	} // MarkdownReview_IsIndexInRangesSorted method

	private static bool MarkdownReview_IsListItemLine(string sLine) {
	if (String.IsNullOrEmpty(sLine)) return false;
	string s = sLine.TrimStart();
	if (s.Length < 2) return false;

	char c = s[0];
	if ((c == '-' || c == '+' || c == '*') && Char.IsWhiteSpace(s[1])) return true;

	if (!Char.IsDigit(c)) return false;
	int i = 0;
	while (i < s.Length && Char.IsDigit(s[i])) i++;
	if (i == 0 || i + 1 >= s.Length) return false;
	if (s[i] != '.' && s[i] != ')') return false;
	return Char.IsWhiteSpace(s[i + 1]);
	} // MarkdownReview_IsListItemLine method

				private static bool MarkdownReview_IsNumberedListItemLine(string sLine) {
				if (String.IsNullOrEmpty(sLine)) return false;
				string s = sLine.TrimStart();
				if (s.Length < 3 || !Char.IsDigit(s[0])) return false;
				int i = 0;
				while (i < s.Length && Char.IsDigit(s[i])) i++;
				if (i == 0 || i + 1 >= s.Length) return false;
				if (s[i] != '.' && s[i] != ')') return false;
				return Char.IsWhiteSpace(s[i + 1]);
				} // MarkdownReview_IsNumberedListItemLine method

	private static string MarkdownReview_NormalizeUrl(string sUrl) {
	if (String.IsNullOrEmpty(sUrl)) return "";
	string s = sUrl.Trim();

	// Remove optional title portion from inline link destinations.
	try {
	if (s.StartsWith("<") && s.EndsWith(">") && s.Length > 2) {
	s = s.Substring(1, s.Length - 2).Trim();
	}
	else {
	int iSpace = s.IndexOfAny(new char[] {' ', '\t', '\r', '\n'});
	if (iSpace > 0) s = s.Substring(0, iSpace).Trim();
	}
	}
	catch {}

	try {s = Util.Unquote(s).Trim();} catch {}
	s = MarkdownReview_CleanUrl(s);

	if (s.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) s = "https://" + s;

	bool bOk = s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
	s.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
	s.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
	s.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);
	if (!bOk) return "";

	return s;
	} // MarkdownReview_NormalizeUrl method

	private static void MarkdownReview_ParseText(string sText, out List<int> headings, out List<int> lists, out List<int> items, out List<int> links, out List<int> tables, out List<int[]> inlineLinks) {
	headings = new List<int>();
	lists = new List<int>();
	items = new List<int>();
	links = new List<int>();
	tables = new List<int>();
	inlineLinks = new List<int[]>();

	if (String.IsNullOrEmpty(sText)) return;

	inlineLinks = MarkdownReview_FindInlineLinks(sText);
	try {
	foreach (int[] span in inlineLinks) {
	if (span == null || span.Length < 4) continue;
	int iTarget = span[2];
	if (iTarget >= 0 && iTarget <= sText.Length) links.Add(iTarget);
	}
	}
	catch {}

	bool bInFence = false;
	bool bPrevListItem = false;
	string sPrevLine = null;
	int iPrevStart = -1;
	int iPos = 0;
	while (true) {
	int iLf = sText.IndexOf('\n', iPos);
	bool bHasLf = (iLf >= 0);
	if (!bHasLf) iLf = sText.Length;
	int iLineTextEnd = iLf;
	if (iLineTextEnd > iPos && sText[iLineTextEnd - 1] == '\r') iLineTextEnd--;
	string sLine = sText.Substring(iPos, iLineTextEnd - iPos);
	int iStart = iPos;

	if (MarkdownReview_IsFenceLine(sLine)) {
	bInFence = !bInFence;
	bPrevListItem = false;
	sPrevLine = null;
	iPrevStart = -1;
	}

	else if (!bInFence) {
	if (sPrevLine != null && sPrevLine.IndexOf('|') >= 0 && MarkdownTableSeparatorRegex.IsMatch(sLine)) tables.Add(iPrevStart);

	int iLevel;
	string sHeading;
	if (MarkdownReview_IsHeadingLine(sLine, out iLevel, out sHeading)) headings.Add(iStart);

	bool bListItem = MarkdownReview_IsListItemLine(sLine);
	if (bListItem) {
	items.Add(iStart);
	if (!bPrevListItem) lists.Add(iStart);
	bPrevListItem = true;
	}
	else if (sLine.Trim().Length == 0) bPrevListItem = false;
	else bPrevListItem = false;

	MarkdownReview_AddNonInlineLinksFromLine(sLine, iStart, links, inlineLinks);

	sPrevLine = sLine;
	iPrevStart = iStart;
	}

	if (!bHasLf) break;
	iPos = iLf + 1;
	}

	MarkdownReview_SortUnique(headings);
	MarkdownReview_SortUnique(lists);
	MarkdownReview_SortUnique(items);
	MarkdownReview_SortUnique(links);
	MarkdownReview_SortUnique(tables);
	} // MarkdownReview_ParseText method

	private static void MarkdownReview_SortUnique(List<int> list) {
	if (list == null || list.Count < 2) return;
	list.Sort();
	int j = 1;
	for (int i = 1; i < list.Count; i++) {
	if (list[i] != list[i - 1]) {
	list[j] = list[i];
	j++;
	}
	}
	if (j < list.Count) list.RemoveRange(j, list.Count - j);
	} // MarkdownReview_SortUnique method

	private static bool MarkdownReview_TryGetInlineLinkSpanContainingIndex(List<int[]> inlineLinks, int iIndex, out int[] span) {
	span = null;
	if (inlineLinks == null || inlineLinks.Count == 0) return false;
	for (int i = 0; i < inlineLinks.Count; i++) {
	int[] s = inlineLinks[i];
	if (s == null || s.Length < 6) continue;
	if (iIndex >= s[0] && iIndex <= s[1]) {
	span = s;
	return true;
	}
	}
	return false;
	} // MarkdownReview_TryGetInlineLinkSpanContainingIndex method

	private static bool MarkdownReview_TryGetLinkUrlFromLine(string sLine, int iOffset, out string sUrl) {
	sUrl = "";
	if (String.IsNullOrEmpty(sLine)) return false;
	if (iOffset < 0) iOffset = 0;
	if (iOffset > sLine.Length) iOffset = sLine.Length;

	try {
	foreach (Match m in MarkdownInlineLinkRegex.Matches(sLine)) {
	if (m == null || !m.Success) continue;
	if (m.Index <= iOffset && iOffset <= m.Index + m.Length) {
	string s = MarkdownReview_NormalizeUrl(m.Groups["url"].Value);
	if (s.Length == 0) continue;
	sUrl = s;
	return true;
	}
	}
	foreach (Match m in MarkdownAutoLinkRegex.Matches(sLine)) {
	if (m == null || !m.Success) continue;
	if (m.Index <= iOffset && iOffset <= m.Index + m.Length) {
	string s = MarkdownReview_NormalizeUrl(m.Value.Trim('<', '>'));
	if (s.Length == 0) continue;
	sUrl = s;
	return true;
	}
	}
	foreach (Match m in MarkdownBareUrlRegex.Matches(sLine)) {
	if (m == null || !m.Success) continue;
	if (m.Index <= iOffset && iOffset <= m.Index + m.Length) {
	string s = MarkdownReview_NormalizeUrl(m.Value);
	if (s.Length == 0) continue;
	sUrl = s;
	return true;
	}
	}
	foreach (Match m in MarkdownWwwUrlRegex.Matches(sLine)) {
	if (m == null || !m.Success) continue;
	if (m.Index <= iOffset && iOffset <= m.Index + m.Length) {
	string s = MarkdownReview_NormalizeUrl(m.Value);
	if (s.Length == 0) continue;
	sUrl = s;
	return true;
	}
	}
	}
	catch {}

	return false;
	} // MarkdownReview_TryGetLinkUrlFromLine method

	private static bool MarkdownReview_TryGetListItemText(string sLine, out string sText) {
	sText = "";
	if (String.IsNullOrEmpty(sLine)) return false;
	string s = sLine.TrimStart();
	if (s.Length < 2) return false;

	char c = s[0];
	if ((c == '-' || c == '+' || c == '*') && Char.IsWhiteSpace(s[1])) {
	sText = s.Substring(2).TrimStart();
	return true;
	}

	if (!Char.IsDigit(c)) return false;
	int i = 0;
	while (i < s.Length && Char.IsDigit(s[i])) i++;
	if (i == 0 || i + 1 >= s.Length) return false;
	if (s[i] != '.' && s[i] != ')') return false;
	if (!Char.IsWhiteSpace(s[i + 1])) return false;
	sText = s.Substring(i + 2).TrimStart();
	return true;
	} // MarkdownReview_TryGetListItemText method

				private static bool MarkdownReview_TryGetSingleInlineLinkSpanOnLine(List<int[]> inlineLinks, int iLineStart, int iLineEnd, out int[] span) {
				span = null;
				if (inlineLinks == null || inlineLinks.Count == 0) return false;
				foreach (int[] s in inlineLinks) {
				if (s == null || s.Length < 6) continue;
				if (s[0] < iLineStart || s[1] > iLineEnd) continue;
				if (span != null) {
				span = null;
				return false;
				}
				span = s;
				}
				return span != null;
				} // MarkdownReview_TryGetSingleInlineLinkSpanOnLine method

				private static bool MarkdownReview_TryGetSingleUrlFromLine(string sLine, out string sUrl) {
				sUrl = "";
				if (String.IsNullOrEmpty(sLine)) return false;
				List<string> urls = new List<string>();
				List<int[]> spans = new List<int[]>();

				try {
				foreach (Match m in MarkdownInlineLinkRegex.Matches(sLine)) {
				if (m == null || !m.Success) continue;
				string s = MarkdownReview_NormalizeUrl(m.Groups["url"].Value);
				if (s.Length == 0) continue;
				urls.Add(s);
				spans.Add(new int[] {m.Index, m.Index + m.Length});
				}
				foreach (Match m in MarkdownAutoLinkRegex.Matches(sLine)) {
				if (m == null || !m.Success) continue;
				if (MarkdownReview_IsIndexInAnySpan(m.Index, spans)) continue;
				string s = MarkdownReview_NormalizeUrl(m.Value.Trim('<', '>'));
				if (s.Length == 0) continue;
				urls.Add(s);
				spans.Add(new int[] {m.Index, m.Index + m.Length});
				}
				foreach (Match m in MarkdownBareUrlRegex.Matches(sLine)) {
				if (m == null || !m.Success) continue;
				if (MarkdownReview_IsIndexInAnySpan(m.Index, spans)) continue;
				string s = MarkdownReview_NormalizeUrl(m.Value);
				if (s.Length == 0) continue;
				urls.Add(s);
				spans.Add(new int[] {m.Index, m.Index + m.Length});
				}
				foreach (Match m in MarkdownWwwUrlRegex.Matches(sLine)) {
				if (m == null || !m.Success) continue;
				if (MarkdownReview_IsIndexInAnySpan(m.Index, spans)) continue;
				string s = MarkdownReview_NormalizeUrl(m.Value);
				if (s.Length == 0) continue;
				urls.Add(s);
				spans.Add(new int[] {m.Index, m.Index + m.Length});
				}
				}
				catch {}

				if (urls.Count != 1) return false;
				sUrl = urls[0];
				return sUrl.Length > 0;
				} // MarkdownReview_TryGetSingleUrlFromLine method

			private static string RtfEncode(string sText) {
			if (sText == null) return "";
			StringBuilder sb = new StringBuilder();
			foreach (char c in sText) {
			switch (c) {
			case '\\':
			case '{':
			case '}':
			sb.Append('\\');
			sb.Append(c);
			break;
			case '\r':
			break;
			case '\n':
			sb.Append(@"\par ");
			break;
			default:
			if (c <= 0x7f) sb.Append(c);
			else {
			int i = (int) c;
			if (i > 32767) i -= 65536;
			sb.Append(@"\u");
			sb.Append(i.ToString(CultureInfo.InvariantCulture));
			sb.Append('?');
			}
			break;
			}
			}
			return sb.ToString();
			} // RtfEncode method

				private static string RtfEncodeInline(string sText) {
				if (String.IsNullOrEmpty(sText)) return "";
				StringBuilder sb = new StringBuilder();
				foreach (char c in sText) {
				if (c == '\\' || c == '{' || c == '}') { sb.Append('\\'); sb.Append(c); }
				else if (c == '\r' || c == '\n') { sb.Append(' '); }
				else if (c <= 0x7f) sb.Append(c);
				else {
				int i = (int) c;
				if (i > 32767) i -= 65536;
				sb.Append(@"\u");
				sb.Append(i.ToString(CultureInfo.InvariantCulture));
				sb.Append('?');
				}
				}
				return sb.ToString();
				} // RtfEncodeInline method

			private static string StripHtmlFormatting(string sLine) {
			if (String.IsNullOrEmpty(sLine)) return "";
			sLine = Util.RegExpReplaceCase(sLine, @"<\s*br\s*/?\s*>", " ");
			sLine = Util.RegExpReplaceCase(sLine, @"</\s*(p|div|li|tr|h[1-6])\s*>", " ");
			sLine = HtmlTagRegex.Replace(sLine, "");
			try {sLine = System.Net.WebUtility.HtmlDecode(sLine);} catch {}
			return sLine;
			} // StripHtmlFormatting method

			private static string StripMarkdownFormatting(string sText) {
			if (sText == null) return "";
			string[] aLines = sText.Split('\n');
			bool bInFence = false;
			for (int i = 0; i < aLines.Length; i++) {
			string sLine = aLines[i];
			bool bCR = sLine.EndsWith("\r");
			if (bCR) sLine = sLine.Substring(0, sLine.Length - 1);

			string sTrim = sLine.Trim();
			bool bFence = sTrim.StartsWith("```") || sTrim.StartsWith("~~~");
			if (bFence) {
			bInFence = !bInFence;
			sLine = "";
			}

			if (!bInFence && !bFence) {
			if (MarkdownTableSeparatorRegex.IsMatch(sLine) || MarkdownHorizontalRuleRegex.IsMatch(sLine)) {
			aLines[i] = bCR ? "\r" : "";
			continue;
			}

			// Line-level markup.
			sLine = MarkdownHeadingPrefixRegex.Replace(sLine, "");
			sLine = MarkdownHeadingSuffixRegex.Replace(sLine, "");
			sLine = Util.RegExpReplaceCase(sLine, @"^\s*>+\s?", "");
			sLine = MarkdownBulletPrefixRegex.Replace(sLine, "${indent}", 1);
			sLine = MarkdownNumberPrefixRegex.Replace(sLine, "${indent}", 1);

				// Links and images.
				sLine = StripMarkdownInlineLinks(sLine);
				sLine = Util.RegExpReplaceCase(sLine, @"<(?<url>https?://[^>]+)>", "${url}");

			// Inline code.
			sLine = Util.RegExpReplaceCase(sLine, @"`([^`]*)`", "$1");

			// Emphasis/strike (apply multiple times to unwrap nested markers).
			for (int j = 0; j < 3; j++) {
			sLine = Util.RegExpReplaceCase(sLine, @"(\*\*|__)(.+?)\1", "$2");
			sLine = Util.RegExpReplaceCase(sLine, @"(\*|_)(.+?)\1", "$2");
			sLine = Util.RegExpReplaceCase(sLine, @"~~(.+?)~~", "$1");
			}

			if (MarkdownTableRowRegex.IsMatch(sLine)) sLine = StripMarkdownTableRow(sLine);
			sLine = StripHtmlFormatting(sLine);
			}

			aLines[i] = sLine + (bCR ? "\r" : "");
			}

				return String.Join("\n", aLines);
				} // StripMarkdownFormatting method

				private static string StripMarkdownInlineLinks(string sLine) {
				if (String.IsNullOrEmpty(sLine)) return "";
				List<int[]> links = null;
				try {links = MarkdownReview_FindInlineLinks(sLine);} catch {links = null;}
				if (links == null || links.Count == 0) return sLine;

				StringBuilder sb = new StringBuilder(sLine.Length);
				int iPos = 0;
				foreach (int[] span in links) {
				if (span == null || span.Length < 6) continue;
					int iSpanStart = span[0];
					if (iSpanStart > 0 && sLine[iSpanStart - 1] == '!') iSpanStart--;
					if (iSpanStart < iPos || span[0] > sLine.Length) continue;
					if (span[1] < span[0] || span[1] > sLine.Length) continue;
					if (span[2] < span[0] || span[3] < span[2] || span[3] > span[1]) continue;
					sb.Append(sLine.Substring(iPos, iSpanStart - iPos));
					sb.Append(sLine.Substring(span[2], span[3] - span[2]));
					iPos = span[1];
				}
				if (iPos < sLine.Length) sb.Append(sLine.Substring(iPos));
				return sb.ToString();
				} // StripMarkdownInlineLinks method

			private static string StripMarkdownTableRow(string sLine) {
			if (String.IsNullOrEmpty(sLine)) return "";
			string s = sLine.Trim();
			if (s.StartsWith("|")) s = s.Substring(1);
			if (s.EndsWith("|")) s = s.Substring(0, s.Length - 1);
			string[] aCells = s.Split('|');
			for (int i = 0; i < aCells.Length; i++) aCells[i] = aCells[i].Trim();
			return String.Join("\t", aCells);
			} // StripMarkdownTableRow method

			private void ToggleBulletListShortcut(MdiChild child) {
			if (child == null) return;
			HomerRichTextBox rtb = child.RTB;
			if (rtb == null) return;

			if (IsRichTextFile(child)) {
			rtb.SelectionBullet = !rtb.SelectionBullet;
			AddMessage(rtb.SelectionBullet ? "Bulleted list on" : "Bulleted list off");
			return;
			}

			int iStart, iEnd;
			GetSelectedLineSpan(rtb, out iStart, out iEnd);
			if (iEnd <= iStart) return;
			string sText = rtb.GetRange(iStart, iEnd);
			string[] aLines = sText.Split('\n');

			bool bAllBulleted = true;
			for (int i = 0; i < aLines.Length; i++) {
			string sLine = aLines[i];
			if (sLine.EndsWith("\r")) sLine = sLine.Substring(0, sLine.Length - 1);
			if (sLine.Trim().Length == 0) continue;
			if (!MarkdownBulletPrefixRegex.IsMatch(sLine)) {
			bAllBulleted = false;
			break;
			}
			}

			for (int i = 0; i < aLines.Length; i++) {
			string sLine = aLines[i];
			bool bCR = sLine.EndsWith("\r");
			if (bCR) sLine = sLine.Substring(0, sLine.Length - 1);

			if (sLine.Trim().Length == 0) {
			aLines[i] = bCR ? "\r" : "";
			continue;
			}

			if (bAllBulleted) {
			// Remove bullet marker, keep indentation.
			aLines[i] = MarkdownBulletPrefixRegex.Replace(sLine, "${indent}", 1) + (bCR ? "\r" : "");
			}
			else {
			// Convert numbered list items to bullets, and add bullets where missing.
			sLine = MarkdownNumberPrefixRegex.Replace(sLine, "${indent}", 1);
			if (!MarkdownBulletPrefixRegex.IsMatch(sLine)) {
			int iIndent = GetLeadingWhitespaceLength(sLine);
			string sIndent = sLine.Substring(0, iIndent);
			string sRest = sLine.Substring(iIndent);
			sLine = sIndent + "- " + sRest;
			}
			aLines[i] = sLine + (bCR ? "\r" : "");
			}
			}

			string sNewText = String.Join("\n", aLines);
			rtb.ReplaceRange(iStart, iEnd, sNewText);
			AddMessage(bAllBulleted ? "Bulleted list off" : "Bulleted list on");
			} // ToggleBulletListShortcut method

			private void ToggleNumberedListShortcut(MdiChild child) {
			if (child == null) return;
			HomerRichTextBox rtb = child.RTB;
			if (rtb == null) return;

			if (IsRichTextFile(child)) {
			int iSelStart = rtb.SelectionStart;
			int iSelLength = rtb.SelectionLength;

			int iStartRow;
			int iEndRow;
			if (iSelLength == 0) {
			iStartRow = rtb.GetLineFromCharIndex(iSelStart);
			iEndRow = iStartRow;
			}
			else {
			int iSelEnd = iSelStart + iSelLength;
			int iEndIndex = (iSelEnd > iSelStart) ? iSelEnd - 1 : iSelStart;
			iStartRow = rtb.GetLineFromCharIndex(iSelStart);
			iEndRow = rtb.GetLineFromCharIndex(iEndIndex);
			}
			if (iStartRow < 0) iStartRow = 0;
			if (iEndRow < 0) iEndRow = iStartRow;

			bool bAllNumbered = true;
			for (int iRow = iStartRow; iRow <= iEndRow; iRow++) {
			string sLine = rtb.GetRowText(iRow);
			if (sLine.Trim().Length == 0) continue;
			if (!MarkdownNumberPrefixRegex.IsMatch(sLine)) {
			bAllNumbered = false;
			break;
			}
			}

			if (bAllNumbered) {
			for (int iRow = iEndRow; iRow >= iStartRow; iRow--) {
			string sLine = rtb.GetRowText(iRow);
			if (sLine.Trim().Length == 0) continue;
			Match m = MarkdownNumberPrefixRegex.Match(sLine);
			if (!m.Success) continue;
			string sIndent = m.Groups["indent"].Value;
			int iRowStart = rtb.GetFirstCharIndexFromLine(iRow);
			if (iRowStart < 0) continue;
			rtb.Select(iRowStart, m.Length);
			rtb.SelectedText = sIndent;
			}
			AddMessage("Numbered list off");
			return;
			}

			int iNumber = 1;
			for (int iRow = iStartRow; iRow <= iEndRow; iRow++) {
			string sLine = rtb.GetRowText(iRow);
			if (sLine.Trim().Length == 0) continue;

			Match mPrefix = MarkdownBulletPrefixRegex.Match(sLine);
			if (!mPrefix.Success) mPrefix = MarkdownNumberPrefixRegex.Match(sLine);
			if (mPrefix.Success) {
			string sIndent = mPrefix.Groups["indent"].Value;
			int iRowStart = rtb.GetFirstCharIndexFromLine(iRow);
			if (iRowStart >= 0) {
			rtb.Select(iRowStart, mPrefix.Length);
			rtb.SelectedText = sIndent;
			}
			sLine = rtb.GetRowText(iRow);
			}

			int iIndentLength = GetLeadingWhitespaceLength(sLine);
			int iInsert = rtb.GetFirstCharIndexFromLine(iRow) + iIndentLength;
			if (iInsert < 0) continue;
			rtb.Select(iInsert, 0);
			rtb.SelectedText = iNumber.ToString() + ". ";
			iNumber++;
			}

			AddMessage("Numbered list on");
			return;
			}

			int iStart, iEnd;
			GetSelectedLineSpan(rtb, out iStart, out iEnd);
			if (iEnd <= iStart) return;
			string sText = rtb.GetRange(iStart, iEnd);
			string[] aLines = sText.Split('\n');

			bool bAllNumbered2 = true;
			for (int i = 0; i < aLines.Length; i++) {
			string sLine = aLines[i];
			if (sLine.EndsWith("\r")) sLine = sLine.Substring(0, sLine.Length - 1);
			if (sLine.Trim().Length == 0) continue;
			if (!MarkdownNumberPrefixRegex.IsMatch(sLine)) {
			bAllNumbered2 = false;
			break;
			}
			}

			int iNumber2 = 1;
			for (int i = 0; i < aLines.Length; i++) {
			string sLine = aLines[i];
			bool bCR = sLine.EndsWith("\r");
			if (bCR) sLine = sLine.Substring(0, sLine.Length - 1);

			if (sLine.Trim().Length == 0) {
			aLines[i] = bCR ? "\r" : "";
			continue;
			}

			if (bAllNumbered2) {
			aLines[i] = MarkdownNumberPrefixRegex.Replace(sLine, "${indent}", 1) + (bCR ? "\r" : "");
			}
			else {
			// Convert bullets to numbers, and add numbers where missing.
			sLine = MarkdownBulletPrefixRegex.Replace(sLine, "${indent}", 1);
			sLine = MarkdownNumberPrefixRegex.Replace(sLine, "${indent}", 1);
			int iIndent = GetLeadingWhitespaceLength(sLine);
			string sIndent = sLine.Substring(0, iIndent);
			string sRest = sLine.Substring(iIndent);
			sLine = sIndent + iNumber2.ToString() + ". " + sRest;
			aLines[i] = sLine + (bCR ? "\r" : "");
			iNumber2++;
			}
			}

			string sNewText = String.Join("\n", aLines);
			rtb.ReplaceRange(iStart, iEnd, sNewText);
			AddMessage(bAllNumbered2 ? "Numbered list off" : "Numbered list on");
			} // ToggleNumberedListShortcut method

				private bool TryCopyMarkdownLinkAsRichText(MdiChild child) {
				if (child == null || child.RTB == null) return false;
				HomerRichTextBox rtb = child.RTB;
				MarkdownReview_EnsureCache(child);

				int iIndex = rtb.SelectionStart;
				int[] span = null;
				if (!MarkdownReview_TryGetInlineLinkSpanContainingIndex(child.MarkdownReviewInlineLinks, iIndex, out span)) {
				int iRow = rtb.GetLineFromCharIndex(iIndex);
				int iLineStart = (iRow >= 0) ? rtb.GetFirstCharIndexFromLine(iRow) : -1;
				int iLineEnd = (iRow >= 0) ? rtb.GetFirstCharIndexFromLine(iRow + 1) : -1;
				if (iLineStart >= 0) {
				if (iLineEnd < 0) iLineEnd = rtb.TextLength;
				MarkdownReview_TryGetSingleInlineLinkSpanOnLine(child.MarkdownReviewInlineLinks, iLineStart, iLineEnd, out span);
				}
				}
				if (span == null || span.Length < 6) return TryCopyMarkdownUrlAsRichText(child, iIndex);
				return CopyMarkdownInlineLinkSpanAsRichText(child, span);
				} // TryCopyMarkdownLinkAsRichText method

					private static bool TryCopyMarkdownList(HomerRichTextBox rtb) {
				if (rtb == null) return false;
				int iRow = rtb.Row;
				if (iRow < 0) return false;
				string sLine = rtb.GetRowText(iRow);
				if (!MarkdownReview_IsListItemLine(sLine)) return false;
				bool bOrdered = MarkdownReview_IsNumberedListItemLine(sLine);

				int iFirst = iRow;
				while (iFirst > 0 && MarkdownReview_IsListItemLine(rtb.GetRowText(iFirst - 1))) iFirst--;

			int iLast = iRow;
			int iBottom = rtb.BottomRow;
			while (iLast < iBottom && MarkdownReview_IsListItemLine(rtb.GetRowText(iLast + 1))) iLast++;

			int iStart = rtb.GetFirstCharIndexFromLine(iFirst);
			int iEnd = rtb.GetFirstCharIndexFromLine(iLast + 1);
			if (iStart < 0) return false;
			if (iEnd < 0) iEnd = rtb.TextLength;
			if (iEnd < iStart) return false;

				List<string> items = GetMarkdownListItemTexts(rtb, iFirst, iLast);
				string sText = Util.Convert2WinLineBreak(String.Join("\n", items.ToArray()));
				DataObject data = new DataObject();
				data.SetData(DataFormats.UnicodeText, sText);
				data.SetData(DataFormats.Text, sText);
				// Build a true Word list via RTF list tables so it pastes as the
				// "List Paragraph" style, not manual bullets. Per consultation we do
				// NOT add CF_HTML here: leaving HTML in the clipboard can make Word
				// pick the HTML path (which does not yield a real list). RTF + plain
				// text gives the most deterministic native-list paste into Word.
				string sRtf = BuildRtfListParagraph(items, bOrdered);
				if (sRtf.Length > 0) data.SetData(DataFormats.Rtf, sRtf);
				// Wlasny format: wklejenie w EdSharpie odtwarza skladnie listy.
				data.SetData(EdSharpMarkdownFormat, Util.Convert2WinLineBreak(GetMarkdownListSourceText(rtb, iFirst, iLast)));
				if (!Util.SetClipboardData(data)) {App.Frame.AddMessage("Clipboard is busy, list not copied!"); return false;}
					App.Frame.AddMessage("List copied");
					return true;
					} // TryCopyMarkdownList method

					private bool TryCopyMarkdownRichLine(MdiChild child) {
					if (child == null || child.RTB == null) return false;
					HomerRichTextBox rtb = child.RTB;
					if (rtb.SelectionLength != 0) return false;
					int iRow = rtb.Row;
					if (iRow < 0) return false;
					string sLine = rtb.GetRowText(iRow);
					if (String.IsNullOrEmpty(sLine)) return false;
					sLine = sLine.TrimEnd('\r', '\n');
					if (sLine.Trim().Length == 0) return false;

					int iLevel;
					string sHeading;
					if (MarkdownReview_IsHeadingLine(sLine, out iLevel, out sHeading)) {
					string sPlain = StripMarkdownFormatting(sHeading).Trim();
					if (sPlain.Length == 0) return false;
					DataObject data = new DataObject();
					data.SetData(DataFormats.UnicodeText, sPlain);
					data.SetData(DataFormats.Text, sPlain);
					data.SetData(DataFormats.Html, BuildHtmlClipboardFragment("<h" + iLevel.ToString(CultureInfo.InvariantCulture) + ">" + MarkdownInlineToHtml(sHeading) + "</h" + iLevel.ToString(CultureInfo.InvariantCulture) + ">"));
					string sRtf = BuildRtfHeading(sPlain, iLevel);
					if (sRtf.Length > 0) data.SetData(DataFormats.Rtf, sRtf);
					data.SetData(EdSharpMarkdownFormat, sLine);
					if (!Util.SetClipboardData(data)) {AddMessage("Clipboard is busy, heading not copied!"); return false;}
					AddMessage("Heading copied");
					return true;
					}

					if (!MarkdownLineHasRichInlineMarkup(sLine)) return false;
					string sPlainLine = StripMarkdownFormatting(sLine).Trim();
					if (sPlainLine.Length == 0) return false;
					DataObject dataLine = new DataObject();
					dataLine.SetData(DataFormats.UnicodeText, sPlainLine);
					dataLine.SetData(DataFormats.Text, sPlainLine);
					dataLine.SetData(DataFormats.Html, BuildHtmlClipboardFragment("<p>" + MarkdownInlineToHtml(sLine) + "</p>"));
					string sRtfLine = BuildRtfPlainText(sPlainLine);
					if (sRtfLine.Length > 0) dataLine.SetData(DataFormats.Rtf, sRtfLine);
					dataLine.SetData(EdSharpMarkdownFormat, sLine);
					if (!Util.SetClipboardData(dataLine)) {AddMessage("Clipboard is busy, text not copied!"); return false;}
					AddMessage("Rich text copied");
					return true;
					} // TryCopyMarkdownRichLine method

				// KOPIOWANIE ZAZNACZENIA Z FORMATOWANIEM (Kasperczak, zlecenie
				// 1788204748663-2).  Pozostale trzy sciezki Control+Shift+C patrza na
				// WIERSZ POD KURSOREM, wiec zaznaczenie kilku wierszy szlo do Worda
				// jako goly tekst z gwiazdkami.  Ta metoda bierze CALE zaznaczenie i
				// sklada z niego RTF blok po bloku: naglowek zostaje naglowkiem, ciag
				// pozycji listy prawdziwa lista Worda, reszta akapitem z pogrubieniem
				// i pochyleniem.
				//
				// Zwraca false, gdy zaznaczenia nie ma albo miesci sie w jednym
				// wierszu - wtedy pracuje dotychczasowa sciezka, ktora umie wiecej
				// (rozpoznaje odsylacz DOKLADNIE pod kursorem).  Zwraca false takze w
				// pliku RTF, gdzie formatowanie jest prawdziwe i rtb.Copy() niesie je
				// samo, oraz gdy w zaznaczeniu nie ma ANI JEDNEGO znacznika Markdown -
				// przepuszczanie zwyklego tekstu przez nasz generator nic by nie
				// dawalo, a odbieraloby oryginalne zachowanie kontrolki.
				private bool TryCopyMarkdownSelection(MdiChild child) {
				if (child == null || child.RTB == null) return false;
				if (IsRichTextFile(child)) return false;
				HomerRichTextBox rtb = child.RTB;
				if (rtb.SelectionLength <= 0) return false;

				string sSelected = "";
				try {sSelected = rtb.GetRange(rtb.SelectionStart, rtb.SelectionStart + rtb.SelectionLength);} catch {return false;}
				if (String.IsNullOrEmpty(sSelected)) return false;

				string[] aLines = SplitTextLines(sSelected);
				if (aLines.Length < 2) return false;
				if (!SelectionHasMarkdownMarkup(aLines)) return false;

				string sRtf = BuildRtfFromMarkdownLines(aLines);
				if (sRtf.Length == 0) return false;

				string sPlain = StripMarkdownFormatting(String.Join("\n", aLines));
				sPlain = Util.Convert2WinLineBreak(sPlain);

				DataObject data = new DataObject();
				data.SetData(DataFormats.UnicodeText, sPlain);
				data.SetData(DataFormats.Text, sPlain);
				data.SetData(DataFormats.Rtf, sRtf);
				// Swiadomie BEZ CF_HTML: przy liscie Word wybiera sciezke HTML i nie
				// robi z niej prawdziwej listy (ta sama decyzja co w
				// TryCopyMarkdownList).
				// Wlasny format: wklejenie w EdSharpie odtwarza CALE zaznaczenie ze
				// skladnia, a nie tekst pozbawiony znacznikow.
				data.SetData(EdSharpMarkdownFormat, Util.Convert2WinLineBreak(String.Join("\n", aLines)));
				if (!Util.SetClipboardData(data)) {AddMessage("Clipboard is busy, selection not copied!"); return false;}
				AddMessage("Selection copied");
				return true;
				} // TryCopyMarkdownSelection method

				// Podzial na wiersze niezalezny od rodzaju koncow wiersza.
				private static string[] SplitTextLines(string sText) {
				if (sText == null) return new string[0];
				string s = sText.Replace("\r\n", "\n");
				s = s.Replace('\r', '\n');
				return s.Split('\n');
				} // SplitTextLines method

				// Czy w zaznaczeniu jest COKOLWIEK, co niesie formatowanie.  Bez tej
				// bramki komenda przejmowalaby kopiowanie zwyklego tekstu, gdzie nic
				// nie wnosi.
				private static bool SelectionHasMarkdownMarkup(string[] aLines) {
				if (aLines == null) return false;
				int iLevel;
				string sHeading;
				foreach (string sRaw in aLines) {
				string sLine = sRaw == null ? "" : sRaw;
				if (MarkdownReview_IsHeadingLine(sLine, out iLevel, out sHeading)) return true;
				if (MarkdownReview_IsListItemLine(sLine)) return true;
				if (MarkdownLineHasRichInlineMarkup(sLine)) return true;
				}
				return false;
				} // SelectionHasMarkdownMarkup method

				// Zaznaczenie jako RTF, blok po bloku.  Blok kodu jest doslowny:
				// gwiazdka w programie nie jest pogrubieniem, wiec fence wylacza
				// rozpoznawanie skladni - ta sama zasada, ktora ma podglad i
				// StripMarkdownFormatting.
				private static string BuildRtfFromMarkdownLines(string[] aLines) {
				if (aLines == null || aLines.Length == 0) return "";
				try {
				StringBuilder sb = new StringBuilder();
				sb.Append(@"{\rtf1\ansi\ansicpg1250\deff0\uc1{\fonttbl{\f0\fnil Calibri;}{\f1\fnil\fcharset2 Symbol;}}");
				sb.Append(@"{\stylesheet{\s1\fi-360\li720\sa0\jclisttab\tx720 List Paragraph;}}");
				sb.Append(@"{\*\listtable{\list\listtemplateid1\listsimple");
				sb.Append(@"{\listlevel\levelnfc23\leveljc0\levelfollow0\levelstartat1{\leveltext\leveltemplateid1\'01\'b7;}{\levelnumbers;}\f1\fi-360\li720 }");
				sb.Append(@"\listid1}");
				sb.Append(@"{\list\listtemplateid2\listsimple");
				sb.Append(@"{\listlevel\levelnfc0\leveljc0\levelfollow0\levelstartat1{\leveltext\leveltemplateid2\'02\'00.;}{\levelnumbers\'01;}\fi-360\li720 }");
				sb.Append(@"\listid2}}");
				sb.Append(@"{\*\listoverridetable{\listoverride\listid1\listoverridecount0\ls1}{\listoverride\listid2\listoverridecount0\ls2}}");

				bool bInFence = false;
				int iOrdinal = 0;
				int iLevel;
				string sHeading;
				for (int i = 0; i < aLines.Length; i++) {
				string sLine = aLines[i] == null ? "" : aLines[i];
				string sTrim = sLine.Trim();

				if (sTrim.StartsWith("```") || sTrim.StartsWith("~~~")) {
				bInFence = !bInFence;
				iOrdinal = 0;
				continue;
				}

				if (bInFence) {
				iOrdinal = 0;
				sb.Append(@"\pard\plain\f0 ");
				sb.Append(RtfEncodeInline(sLine));
				sb.Append(@"\par ");
				continue;
				}

				if (sTrim.Length == 0) {
				iOrdinal = 0;
				sb.Append(@"\pard\plain\f0\par ");
				continue;
				}

				if (MarkdownReview_IsHeadingLine(sLine, out iLevel, out sHeading)) {
				iOrdinal = 0;
				int iHalfPoints = 24;
				if (iLevel == 1) iHalfPoints = 36;
				else if (iLevel == 2) iHalfPoints = 32;
				else if (iLevel == 3) iHalfPoints = 28;
				sb.Append(@"\pard\plain\f0\b\fs");
				sb.Append(iHalfPoints.ToString(CultureInfo.InvariantCulture));
				sb.Append(" ");
				sb.Append(RtfEncodeMarkdownInline(sHeading));
				sb.Append(@"\b0\fs22\par ");
				continue;
				}

				string sItem;
				if (MarkdownReview_TryGetListItemText(sLine, out sItem)) {
				bool bOrdered = MarkdownReview_IsNumberedListItemLine(sLine);
				if (bOrdered) iOrdinal++;
				else iOrdinal = 0;
				sb.Append(bOrdered ? @"\pard\plain\s1\ls2\ilvl0" : @"\pard\plain\s1\ls1\ilvl0");
				sb.Append(@"\fi-360\li720\sa0\jclisttab\tx720 ");
				if (bOrdered) {
				sb.Append(@"{\listtext\f0 ");
				sb.Append(iOrdinal.ToString(CultureInfo.InvariantCulture));
				sb.Append(@".\tab}");
				}
				else sb.Append(@"{\listtext\f1 \'b7\tab}");
				sb.Append(@"\f0 ");
				sb.Append(RtfEncodeMarkdownInline(sItem));
				sb.Append(@"\par ");
				continue;
				}

				iOrdinal = 0;
				sb.Append(@"\pard\plain\f0 ");
				sb.Append(RtfEncodeMarkdownInline(sLine.Trim()));
				sb.Append(@"\par ");
				}

				sb.Append("}");
				return sb.ToString();
				}
				catch {
				return "";
				}
				} // BuildRtfFromMarkdownLines method

				// Jeden wiersz Markdown na RTF: gwiazdki znikaja, a w ich miejsce
				// wchodzi PRAWDZIWE pogrubienie i pochylenie.  Odsylacz staje sie polem
				// HYPERLINK, czyli klikalnym odsylaczem Worda, spojnie z
				// BuildRtfHyperlink uzywanym przy kopiowaniu pojedynczego linku.
				private static string RtfEncodeMarkdownInline(string sText) {
				if (String.IsNullOrEmpty(sText)) return "";
				StringBuilder sb = new StringBuilder();
				int iPos = 0;
				try {
				foreach (Match m in MarkdownInlineLinkRegex.Matches(sText)) {
				if (m == null || !m.Success || m.Index < iPos) continue;
				sb.Append(RtfEncodeMarkdownSpan(sText.Substring(iPos, m.Index - iPos)));
				string sTitle = StripMarkdownFormatting(m.Groups["text"].Value).Trim();
				string sUrl = MarkdownReview_NormalizeUrl(m.Groups["url"].Value);
				if (sUrl.Length > 0) {
				sb.Append(@"{\field{\*\fldinst{");
				sb.Append(RtfEncodeInline("HYPERLINK \"" + sUrl + "\""));
				sb.Append(@"}}{\fldrslt{");
				sb.Append(RtfEncodeInline(sTitle.Length == 0 ? sUrl : sTitle));
				sb.Append(@"}}}");
				}
				else sb.Append(RtfEncodeInline(sTitle));
				iPos = m.Index + m.Length;
				}
				}
				catch {}
				if (iPos < sText.Length) sb.Append(RtfEncodeMarkdownSpan(sText.Substring(iPos)));
				return sb.ToString();
				} // RtfEncodeMarkdownInline method

				// Kawalek wiersza BEZ odsylaczy: same znaczniki pogrubienia, pochylenia,
				// przekreslenia i kodu.  Petla po znakach, a nie wyrazenie regularne, bo
				// znaczniki sie zagniezdzaja, a niedomkniety znacznik ma zostac TRESCIA
				// - w edytorze tekstu gwiazdka bywa po prostu gwiazdka.
				private static string RtfEncodeMarkdownSpan(string sText) {
				if (String.IsNullOrEmpty(sText)) return "";
				StringBuilder sb = new StringBuilder();
				bool bBold = false, bItalic = false, bStrike = false, bCode = false;
				int i = 0;
				while (i < sText.Length) {
				char c = sText[i];

				if (c == '`' && !bCode) {
				bCode = true;
				sb.Append(@"{\f0 ");
				i++;
				continue;
				}

				if (c == '`' && bCode) {
				bCode = false;
				sb.Append("}");
				i++;
				continue;
				}

				if (bCode) {
				sb.Append(RtfEncodeInline(c.ToString()));
				i++;
				continue;
				}

				if (i + 1 < sText.Length && c == '~' && sText[i + 1] == '~') {
				bStrike = !bStrike;
				sb.Append(bStrike ? @"\strike " : @"\strike0 ");
				i += 2;
				continue;
				}

				if (i + 1 < sText.Length && (c == '*' || c == '_') && sText[i + 1] == c) {
				bBold = !bBold;
				sb.Append(bBold ? @"\b " : @"\b0 ");
				i += 2;
				continue;
				}

				if (c == '*' || c == '_') {
				bItalic = !bItalic;
				sb.Append(bItalic ? @"\i " : @"\i0 ");
				i++;
				continue;
				}

				sb.Append(RtfEncodeInline(c.ToString()));
				i++;
				}
				// Znacznik niedomkniety w zaznaczeniu (ktos zaznaczyl polowe
				// pogrubienia) nie moze wyciec na kolejne akapity.
				if (bCode) sb.Append("}");
				if (bStrike) sb.Append(@"\strike0 ");
				if (bItalic) sb.Append(@"\i0 ");
				if (bBold) sb.Append(@"\b0 ");
				return sb.ToString();
				} // RtfEncodeMarkdownSpan method


				private bool TryCopyMarkdownUrlAsRichText(MdiChild child, int iIndex) {
				if (child == null || child.RTB == null) return false;
			HomerRichTextBox rtb = child.RTB;
			int iRow = rtb.GetLineFromCharIndex(iIndex);
			if (iRow < 0) return false;
			int iStart = rtb.GetFirstCharIndexFromLine(iRow);
			int iEnd = rtb.GetFirstCharIndexFromLine(iRow + 1);
			if (iStart < 0) return false;
			if (iEnd < 0) iEnd = rtb.TextLength;
				if (iEnd < iStart) return false;
				string sLine = rtb.GetRange(iStart, iEnd).TrimEnd('\r', '\n');
				int iOffset = Math.Max(0, iIndex - iStart);
				string sUrl;
				if (!MarkdownReview_TryGetLinkUrlFromLine(sLine, iOffset, out sUrl) && !MarkdownReview_TryGetSingleUrlFromLine(sLine, out sUrl)) return false;
				sUrl = MarkdownReview_NormalizeUrl(sUrl);
				if (sUrl.Length == 0) return false;

			DataObject data = new DataObject();
			data.SetData(DataFormats.UnicodeText, sUrl);
			data.SetData(DataFormats.Text, sUrl);
			data.SetData(DataFormats.Rtf, BuildRtfHyperlink(sUrl, sUrl));
			// Tu tekstem JEST adres, wiec wlasny format niesie to samo - dodany dla
			// jednolitosci, zeby wklejenie w EdSharpie nigdy nie zalezalo od tego,
			// KTORA sciezka kopiowania wypelnila schowek.
			data.SetData(EdSharpMarkdownFormat, sUrl);
			if (!Util.SetClipboardData(data)) {AddMessage("Clipboard is busy, link not copied!"); return false;}
					AddMessage("Link copied");
					return true;
					} // TryCopyMarkdownUrlAsRichText method



	private static bool MarkdownReview_IsMarkdownFile(string sFile) {
	if (String.IsNullOrEmpty(sFile)) return false;
	try {
	return String.Equals(Path.GetExtension(sFile), ".md", StringComparison.OrdinalIgnoreCase);
	}
	catch {
	return false;
	}
	} // MarkdownReview_IsMarkdownFile method

private void RefreshMarkdownReviewAfterEdit(MdiChild child) {
try {
if (child != null && child.MarkdownReviewMode) MarkdownReview_RenderView(child);
}
catch {}
} // RefreshMarkdownReviewAfterEdit method

			private void ClearFormattingShortcut(MdiChild child) {
			if (child == null) return;
			HomerRichTextBox rtb = child.RTB;
			if (rtb == null) return;

				if (IsRichTextFile(child)) {
				if (rtb.SelectionLength == 0) {
				int iIndex = rtb.SelectionStart;
				int iLineStart, iLineEnd;
				GetSelectedLineSpan(rtb, out iLineStart, out iLineEnd);
				string sPlainLine = rtb.GetRange(iLineStart, iLineEnd);
				Font fontBaseLine = rtb.Font;
				Color colorBaseLine = rtb.ForeColor;
				rtb.Select(iLineStart, Math.Max(0, iLineEnd - iLineStart));
				try {
				rtb.SelectionBullet = false;
				rtb.SelectionAlignment = HorizontalAlignment.Left;
				if (fontBaseLine != null) rtb.SelectionFont = fontBaseLine;
				rtb.SelectionColor = colorBaseLine;
				rtb.SelectedText = sPlainLine;
				}
				catch {}
				rtb.Select(Math.Min(iIndex, rtb.TextLength), 0);
				rtb.Modified = true;
				RefreshMarkdownReviewAfterEdit(child);
				return;
				}

			string sPlain = rtb.SelectedText;
			Font fontBase = rtb.Font;
			Color colorBase = rtb.ForeColor;
			int iSelStart = rtb.SelectionStart;
			try {
			rtb.SelectionBullet = false;
			rtb.SelectionAlignment = HorizontalAlignment.Left;
			if (fontBase != null) rtb.SelectionFont = fontBase;
			rtb.SelectionColor = colorBase;
			rtb.SelectedText = sPlain;
			}
			catch {}
			finally {
			try {rtb.Select(iSelStart, 0);} catch {}
			}
			rtb.Modified = true;
			RefreshMarkdownReviewAfterEdit(child);
			return;
			}

				int iSelStart2 = rtb.SelectionStart;
				int iSelLength2 = rtb.SelectionLength;
				int iIndex2 = rtb.SelectionStart;
				if (iSelLength2 == 0) {
				int iLineStart2, iLineEnd2;
				GetSelectedLineSpan(rtb, out iLineStart2, out iLineEnd2);
				string sText = rtb.GetRange(iLineStart2, iLineEnd2);
				string sNewText = StripMarkdownFormatting(sText);
				rtb.Select(iLineStart2, Math.Max(0, iLineEnd2 - iLineStart2));
				rtb.SelectedText = sNewText;
				rtb.Select(Math.Min(iIndex2, rtb.TextLength), 0);
				}
				else {
				string sText = rtb.SelectedText;
				string sNewText = StripMarkdownFormatting(sText);
				rtb.Select(iSelStart2, iSelLength2);
				rtb.SelectedText = sNewText;
				rtb.Select(iSelStart2, sNewText.Length);
			}
			rtb.Modified = true;
			RefreshMarkdownReviewAfterEdit(child);
			} // ClearFormattingShortcut method
					private bool ShouldLetMarkdownReviewViewHandleNativeNavigation(Keys keyData) {
			MdiChild child = this.Child;
			if (child == null || !child.MarkdownReviewMode || child.MarkdownReviewView == null) return false;
			Keys keyCode = keyData & Keys.KeyCode;
			Keys modifiers = keyData & (Keys.Control | Keys.Shift | Keys.Alt);
			return modifiers == Keys.Control && (keyCode == Keys.Up || keyCode == Keys.Down);
			} // ShouldLetMarkdownReviewViewHandleNativeNavigation method

	private static string MarkdownReview_GetKindName(MarkdownReviewKind kind) {
	switch (kind) {
	case MarkdownReviewKind.Heading:
	return "heading";
	case MarkdownReviewKind.List:
	return "list";
	case MarkdownReviewKind.ListItem:
	return "list item";
	case MarkdownReviewKind.Link:
	return "link";
	case MarkdownReviewKind.Table:
	return "table";
	default:
	return "item";
	}
	} // MarkdownReview_GetKindName method

	private static string MarkdownReview_GetKindPlural(MarkdownReviewKind kind) {
	switch (kind) {
	case MarkdownReviewKind.Heading:
	return "headings";
	case MarkdownReviewKind.List:
	return "lists";
	case MarkdownReviewKind.ListItem:
	return "list items";
	case MarkdownReviewKind.Link:
	return "links";
	case MarkdownReviewKind.Table:
	return "tables";
	default:
	return "items";
	}
	} // MarkdownReview_GetKindPlural method

		private static List<MarkdownReviewInlineSpan> MarkdownReview_GetInlineStyleSpans(string sLine, int iListMarkerStart, int iListMarkerLength, out bool[] aHide) {
		aHide = null;
		List<MarkdownReviewInlineSpan> spans = new List<MarkdownReviewInlineSpan>();
		if (String.IsNullOrEmpty(sLine)) return spans;

		int iLen = sLine.Length;
		aHide = new bool[iLen];

		bool[] aProtected = null;
		try {
		aProtected = new bool[iLen];
		foreach (Match m in MarkdownAutoLinkRegex.Matches(sLine)) {
		if (m == null || !m.Success) continue;
		int iStart = m.Index;
		int iEnd = m.Index + m.Length;
		if (iStart < 0) iStart = 0;
		if (iEnd > iLen) iEnd = iLen;
		for (int i = iStart; i < iEnd; i++) aProtected[i] = true;
		}
		foreach (Match m in MarkdownBareUrlRegex.Matches(sLine)) {
		if (m == null || !m.Success) continue;
		int iStart = m.Index;
		int iEnd = m.Index + m.Length;
		if (iStart < 0) iStart = 0;
		if (iEnd > iLen) iEnd = iLen;
		for (int i = iStart; i < iEnd; i++) aProtected[i] = true;
		}
		foreach (Match m in MarkdownWwwUrlRegex.Matches(sLine)) {
		if (m == null || !m.Success) continue;
		int iStart = m.Index;
		int iEnd = m.Index + m.Length;
		if (iStart < 0) iStart = 0;
		if (iEnd > iLen) iEnd = iLen;
		for (int i = iStart; i < iEnd; i++) aProtected[i] = true;
		}
		}
		catch {
		aProtected = null;
		}

		Stack<int> stStrongStar = new Stack<int>();

		bool bInCode = false;
		int iPos = 0;
		while (iPos < iLen) {
		// Skip list marker prefix (e.g., "* " or "1. ").
		if (iListMarkerLength > 0 && iPos >= iListMarkerStart && iPos < iListMarkerStart + iListMarkerLength) {
		iPos = iListMarkerStart + iListMarkerLength;
		continue;
		}

		if (aProtected != null && iPos < aProtected.Length && aProtected[iPos]) {
		iPos++;
		continue;
		}

		char c = sLine[iPos];

		// Escape sequence.
		if (c == '\\' && iPos + 1 < iLen) {
		iPos += 2;
		continue;
		}

		// Inline code span (basic).
		if (c == '`') {
		bInCode = !bInCode;
		iPos++;
		continue;
		}
		if (bInCode) {
		iPos++;
		continue;
		}

		// Strong with '**'.
		if (c == '*') {
		if (iPos + 1 < iLen && sLine[iPos + 1] == '*') {
		if (stStrongStar.Count > 0) {
		int iOpen = stStrongStar.Pop();
		int iContentStart = iOpen + 2;
		int iContentEnd = iPos;
		if (iContentEnd > iContentStart) spans.Add(new MarkdownReviewInlineSpan(iContentStart, iContentEnd, FontStyle.Bold));
		if (iOpen >= 0 && iOpen + 1 < iLen) {aHide[iOpen] = true; aHide[iOpen + 1] = true;}
		if (iPos >= 0 && iPos + 1 < iLen) {aHide[iPos] = true; aHide[iPos + 1] = true;}
		}
		else {
		stStrongStar.Push(iPos);
		}
		iPos += 2;
		continue;
		}
		}

		iPos++;
		}

		return spans;
		} // MarkdownReview_GetInlineStyleSpans method

		private bool HandleMarkdownReviewKey(Keys keyData) {
	// Preserve historical behavior: swallow Insert key.
	if (keyData == Keys.Insert) return true;

	MdiChild child = this.Child;

	Keys escCode = keyData & Keys.KeyCode;
	Keys escMods = keyData & (Keys.Control | Keys.Shift | Keys.Alt);
	if (escCode == Keys.Escape && (escMods == Keys.None || escMods == Keys.Shift)) {
	if (child == null) return false;
	bool bWantNoSync = (escMods == Keys.Shift);
	if (!child.MarkdownReviewMode && !MarkdownReview_IsMarkdownFile(child.File)) return false;
	if (this.KeyDescriber) {
	// While previewing, Escape always leaves and Shift+Escape only flips
	// the synchronization, so describe the keys by what they really do.
	if (child.MarkdownReviewMode) AddMessage(bWantNoSync ? "Switch preview synchronization" : "Back to editing");
	else AddMessage(bWantNoSync ? "Open detached preview" : "Open preview");
	return true;
	}
	MarkdownReview_ToggleCurrent(bWantNoSync);
	return true;
	}

	if (child == null) return false;
	if (!child.MarkdownReviewMode) return false;

	Keys keyCodeF7 = keyData & Keys.KeyCode;
	Keys modifiersF7 = keyData & (Keys.Control | Keys.Shift | Keys.Alt);
	if (keyCodeF7 == Keys.F7 && modifiersF7 == Keys.None) {
	if (this.KeyDescriber) {
	AddMessage("Elements list");
	return true;
	}
	if (!MarkdownReview_IsMarkdownFile(child.File)) {
	AddMessage("Markdown review mode is only for .md files!");
	return true;
	}
	MarkdownReview_ShowElementsList(child);
	return true;
	}

	if ((keyData & Keys.Control) == Keys.Control) return false;
	if ((keyData & Keys.Alt) == Keys.Alt) return false;

	Keys keyCode = keyData & Keys.KeyCode;
	if (keyCode == Keys.Enter) {
	if (this.KeyDescriber) {
	AddMessage("Open link");
	return true;
	}
	// KURSOR NA PUNKTORZE POZYCJI LISTY (zgloszenie Kasperczaka 27.08.2026
	// 21:33: "kursor jest na punktorze, a wewnetrzny link daje sie uaktywnic
	// dopiero, kiedy przesuniemy sie o 2 znaki").  Korekta jest TUTAJ, w
	// jednym miejscu przed wszystkimi trzema sciezkami Enter, bo na punktorze
	// mozna stanac na kilka sposobow: strzalka czytnika (kursor laduje na
	// poczatku wiersza), nasza nawigacja po listach, albo wybor z listy
	// elementow pod F7.  Poprawienie samego spisu tresci zalatalo by jeden
	// przypadek z kilku, a spis tresci to zwykla lista markdownowa.
	try {
	HomerRichTextBox rtbHere = child.RTB;
	if (rtbHere != null && rtbHere.SelectionLength == 0) {
	int iHere = rtbHere.SelectionStart;
	int iFixed = MarkdownReview_SkipListMarkerAt(rtbHere.Text, iHere);
	if (iFixed != iHere) MarkdownReview_GoToSourceIndex(child, iFixed);
	}
	}
	catch {}

	// PRZYPIS W PODGLADZIE jest przed spisem tresci, bo znacznik przypisu NIE
	// jest linkiem markdownowym i tamta sciezka go nie widzi.  Kasperczak
	// (zlecenie 1787840875741-1): "on mowi numer przypisu, na tym numerze
	// przypisu naciskam Enter albo Spacje, on przeskakuje do tresci przypisu",
	// a na pytanie, czy przenosic kursor, odpowiedzial 27.08 16:56 "chyba
	// jednak bardziej naturalnie przeniesc".  Wiec Enter PRZENOSI, w obie
	// strony, tak samo jak Alt+F6 w edytorze i Shift+F6 w spisie tresci.
	if (TryGoToFootnoteInReview(child)) return true;

	// Link WEWNETRZNY (pozycja spisu tresci) skacze do rozdzialu w tym samym
	// dokumencie, a nie otwiera przegladarki.  Kasperczak wymagal, zeby spis
	// dzialal takze w podgladzie pod Escape (zlecenie 1787793592380-0,
	// punkt 4).  Sprawdzamy go PRZED adresem, bo NormalizeUrl przepuszcza
	// tylko http, https, ftp i mailto i na "#kotwica" mowilby "No link".
	if (TryGoToInternalAnchorAtCursor(child)) return true;

	string sUrl;
	if (MarkdownReview_TryGetLinkUrlAtCursor(child, out sUrl)) {
	string sError;
	if (!MarkdownReview_TryOpenUrl(sUrl, out sError)) Dialog.Show("Error", sError);
	}
	else {
	AddMessage("No link at cursor");
	}
	return true;
	}

	bool bReverse = (keyData & Keys.Shift) == Keys.Shift;
	MarkdownReviewKind kind;
	switch (keyCode) {
	case Keys.H:
	kind = MarkdownReviewKind.Heading;
	break;
	case Keys.L:
	kind = MarkdownReviewKind.List;
	break;
	case Keys.I:
	kind = MarkdownReviewKind.ListItem;
	break;
	case Keys.K:
	kind = MarkdownReviewKind.Link;
	break;
	case Keys.T:
	kind = MarkdownReviewKind.Table;
	break;
	default:
	return false;
	}

	if (this.KeyDescriber) {
	string sDirection = bReverse ? "previous" : "next";
	AddMessage("Go to " + sDirection + " " + MarkdownReview_GetKindName(kind));
	return true;
	}

	if (!MarkdownReview_IsMarkdownFile(child.File)) {
	AddMessage("Markdown review mode is only for .md files!");
	return true;
	}

	MarkdownReview_Navigate(child, kind, bReverse);
	return true;
	} // HandleMarkdownReviewKey method

	private void MarkdownReview_NavigateLinkOrList(MdiChild child, bool bReverse) {
	if (child == null) return;
	HomerRichTextBox rtb = child.RTB;
	if (rtb == null) return;

	MarkdownReview_EnsureCache(child);
	List<int> targets = new List<int>();
	Dictionary<int, MarkdownReviewKind> kinds = new Dictionary<int, MarkdownReviewKind>();

	if (child.MarkdownReviewLists != null) {
	foreach (int i in child.MarkdownReviewLists) {
	if (!kinds.ContainsKey(i)) kinds.Add(i, MarkdownReviewKind.List);
	targets.Add(i);
	}
	}

	if (child.MarkdownReviewLinks != null) {
	foreach (int i in child.MarkdownReviewLinks) {
	kinds[i] = MarkdownReviewKind.Link;
	targets.Add(i);
	}
	}

	MarkdownReview_SortUnique(targets);
	if (targets.Count == 0) {
	AddMessage("No lists or links!");
	return;
	}

	int iTarget = MarkdownReview_FindTarget(targets, rtb.Index, bReverse);
	if (iTarget < 0) {
	AddMessage("No " + (bReverse ? "previous" : "next") + " list or link!");
	return;
	}

	try {rtb.Select(iTarget, 0);} catch {}
	MarkdownReviewKind kind = kinds.ContainsKey(iTarget) ? kinds[iTarget] : MarkdownReviewKind.Link;
	string sMessage = MarkdownReview_FormatMessage(kind, child, rtb, iTarget);
	try {if (Win32.IsNVDAActive()) Win32.NVDACancelSpeech();} catch {}
	AddMessage(sMessage, true);
	MarkdownReview_SyncViewToEdit(child);
	} // MarkdownReview_NavigateLinkOrList method

	// Show a screen-reader friendly "elements list" dialog for the current
	// Markdown document while in preview mode. This is EdSharp's own equivalent
	// of the NVDA elements list (NVDA+F7), which cannot work over a RichTextBox
	// because there is no virtual buffer. A first dialog lets the user filter by
	// type (All / Headings / Links / Lists / Tables, with counts); the second
	// lists the elements of that type in document order. Choosing one moves the
	// source caret, which the existing view sync mirrors into the preview, and
	// announces the target.
	private void MarkdownReview_ShowElementsList(MdiChild child) {
	if (child == null) return;
	HomerRichTextBox rtb = child.RTB;
	if (rtb == null) return;

	MarkdownReview_EnsureCache(child);

	// Per-type counts (main structures only; individual list items are
	// intentionally omitted so a long bullet list does not flood the dialog).
	// "All" is counted as the number of UNIQUE offsets across every type, to
	// match the de-duplicated list that PickAndJump builds (an element that is
	// several kinds at one offset must not be double-counted).
	int iHeadings = MarkdownReview_CountValid(child.MarkdownReviewHeadings, rtb.TextLength);
	int iLinks = MarkdownReview_CountValid(child.MarkdownReviewLinks, rtb.TextLength);
	int iLists = MarkdownReview_CountValid(child.MarkdownReviewLists, rtb.TextLength);
	int iTables = MarkdownReview_CountValid(child.MarkdownReviewTables, rtb.TextLength);
	int iAll = MarkdownReview_CountUniqueValid(rtb.TextLength, child.MarkdownReviewHeadings, child.MarkdownReviewLists, child.MarkdownReviewTables, child.MarkdownReviewLinks);

	if (iAll == 0) {
	AddMessage("No elements!");
	return;
	}

	// Build the type filter, including only types that actually occur. "all" is
	// a sentinel value handled below; the others map to MarkdownReviewKind.
	List<string> filterValues = new List<string>();
	List<string> filterDisplay = new List<string>();
	filterValues.Add("all"); filterDisplay.Add("All, " + iAll);
	if (iHeadings > 0) {filterValues.Add(((int) MarkdownReviewKind.Heading).ToString()); filterDisplay.Add("Headings, " + iHeadings);}
	if (iLinks > 0) {filterValues.Add(((int) MarkdownReviewKind.Link).ToString()); filterDisplay.Add("Links, " + iLinks);}
	if (iLists > 0) {filterValues.Add(((int) MarkdownReviewKind.List).ToString()); filterDisplay.Add("Lists, " + iLists);}
	if (iTables > 0) {filterValues.Add(((int) MarkdownReviewKind.Table).ToString()); filterDisplay.Add("Tables, " + iTables);}

	// Skip the filter step when there is only one type of element present
	// (filterValues then holds just "all" plus that single type): go straight
	// to the element list with no filter.
	if (filterValues.Count > 2) {
	string sFilter = Dialog.Pick("Filter by type", filterValues.ToArray(), filterDisplay.ToArray(), false, 0);
	if (sFilter == null || sFilter.Length == 0) return; // cancelled at the filter step
	int iKind;
	if (sFilter != "all" && Int32.TryParse(sFilter, out iKind)) MarkdownReview_PickAndJump(child, rtb, (MarkdownReviewKind) iKind);
	else MarkdownReview_PickAndJump(child, rtb, null);
	}
	else {
	MarkdownReview_PickAndJump(child, rtb, null);
	}
	} // MarkdownReview_ShowElementsList method

	// Build the element list for the given filter (null = all main structures),
	// show it, and jump to the chosen element. Returns false if nothing was
	// shown/chosen. Pure label building (no caret movement) until the jump.
	private bool MarkdownReview_PickAndJump(MdiChild child, HomerRichTextBox rtb, MarkdownReviewKind? filter) {
	if (child == null || rtb == null) return false;

	List<int> offsets = new List<int>();
	Dictionary<int, MarkdownReviewKind> kinds = new Dictionary<int, MarkdownReviewKind>();
	// Order of adding decides which kind wins on a shared offset (structure
	// before link).
	if (filter == null || filter == MarkdownReviewKind.Heading) MarkdownReview_AddElements(child.MarkdownReviewHeadings, MarkdownReviewKind.Heading, offsets, kinds);
	if (filter == null || filter == MarkdownReviewKind.List) MarkdownReview_AddElements(child.MarkdownReviewLists, MarkdownReviewKind.List, offsets, kinds);
	if (filter == null || filter == MarkdownReviewKind.Table) MarkdownReview_AddElements(child.MarkdownReviewTables, MarkdownReviewKind.Table, offsets, kinds);
	if (filter == null || filter == MarkdownReviewKind.Link) MarkdownReview_AddElements(child.MarkdownReviewLinks, MarkdownReviewKind.Link, offsets, kinds);

	MarkdownReview_SortUnique(offsets);

	string sText = rtb.Text;
	List<string> listValues = new List<string>();
	List<string> listDisplay = new List<string>();
	int iCurrent = rtb.Index;
	int iIndex = 0;
	for (int i = 0; i < offsets.Count; i++) {
	int iOffset = offsets[i];
	if (iOffset < 0 || iOffset > sText.Length) continue; // guard against a stale/corrupt cache
	MarkdownReviewKind kind = kinds.ContainsKey(iOffset) ? kinds[iOffset] : MarkdownReviewKind.Link;
	listValues.Add(iOffset.ToString());
	listDisplay.Add(MarkdownReview_BuildLabelAt(child, sText, kind, iOffset));
	if (iOffset <= iCurrent) iIndex = listValues.Count - 1; // default selection: nearest element at or before the caret
	}

	if (listValues.Count == 0) {
	AddMessage("No elements!");
	return false;
	}

	string[] aValues = listValues.ToArray();
	string[] aDisplay = listDisplay.ToArray();

	string sPlural = (filter == null) ? "" : MarkdownReview_GetKindPlural(filter.Value);
	string sTitle = (filter == null) ? "Markdown elements" : (sPlural.Length > 0 ? Char.ToUpper(sPlural[0]) + sPlural.Substring(1) : "Markdown elements");
	// sort=false: the list is already in document order; alphabetical sorting
	// would scramble it.
	string sChoice = Dialog.Pick(sTitle, aValues, aDisplay, false, iIndex);
	if (sChoice == null || sChoice.Length == 0) return false; // cancelled: end the operation, do not loop back to filter

	// Map the chosen value back to its exact offset and pre-computed label, so
	// the announcement does not depend on where the caret happens to land.
	int iChosenIndex = -1;
	for (int i = 0; i < aValues.Length; i++) {
	if (aValues[i] == sChoice) {iChosenIndex = i; break;}
	}
	if (iChosenIndex < 0) return false;

	int iTarget;
	if (!Int32.TryParse(sChoice, out iTarget)) return false;
	if (iTarget < 0) iTarget = 0;
	if (iTarget > sText.Length) iTarget = sText.Length;
	// KURSOR ZA PUNKTOREM (zgloszenie 27.08.2026 21:33): przy pozycji listy
	// stajemy na pierwszym znaku tresci, nie na znaczniku, zeby Enter od razu
	// widzial odsylacz.  Etykieta pozycji zostaje ta sama.
	MarkdownReviewKind kindChosen = kinds.ContainsKey(iTarget) ? kinds[iTarget] : MarkdownReviewKind.Link;
	if (kindChosen == MarkdownReviewKind.List || kindChosen == MarkdownReviewKind.ListItem) iTarget = MarkdownReview_SkipListMarkerAt(sText, iTarget);

	bool bSelected = false;
	try {rtb.Select(iTarget, 0); bSelected = true;} catch {}
	if (!bSelected) return false; // do not announce a wrong element if the jump failed
	MarkdownReview_SyncViewToEdit(child); // explicit sync, like the other navigators (do not rely solely on SelectionChanged)
	try {if (child.MarkdownReviewView != null && !child.MarkdownReviewView.IsDisposed) child.MarkdownReviewView.Focus();} catch {}
	try {if (Win32.IsNVDAActive()) Win32.NVDACancelSpeech();} catch {}
	AddMessage(aDisplay[iChosenIndex], true);
	return true;
	} // MarkdownReview_PickAndJump method

	private static int MarkdownReview_CountValid(List<int> source, int iMax) {
	if (source == null) return 0;
	int iCount = 0;
	foreach (int i in source) if (i >= 0 && i <= iMax) iCount++;
	return iCount;
	} // MarkdownReview_CountValid method

	// Count unique valid offsets across several lists, matching how the combined
	// "All" element list is de-duplicated (so the filter count agrees with it).
	private static int MarkdownReview_CountUniqueValid(int iMax, params List<int>[] sources) {
	if (sources == null) return 0;
	Dictionary<int, bool> seen = new Dictionary<int, bool>();
	foreach (List<int> source in sources) {
	if (source == null) continue;
	foreach (int i in source) {
	if (i < 0 || i > iMax) continue;
	if (!seen.ContainsKey(i)) seen.Add(i, true);
	}
	}
	return seen.Count;
	} // MarkdownReview_CountUniqueValid method

	private static void MarkdownReview_AddElements(List<int> source, MarkdownReviewKind kind, List<int> offsets, Dictionary<int, MarkdownReviewKind> kinds) {
	if (source == null || offsets == null || kinds == null) return;
	foreach (int i in source) {
	if (i < 0) continue; // skip stale/invalid offsets defensively
	offsets.Add(i);
	if (!kinds.ContainsKey(i)) kinds.Add(i, kind);
	}
	} // MarkdownReview_AddElements method

	// Return the text of the line containing iOffset, without touching the
	// caret/selection of any control. Used to label elements list entries.
	private static string MarkdownReview_GetLineAt(string sText, int iOffset) {
	if (String.IsNullOrEmpty(sText)) return "";
	if (iOffset < 0) iOffset = 0;
	if (iOffset > sText.Length) iOffset = sText.Length;
	int iStart = iOffset;
	while (iStart > 0 && sText[iStart - 1] != '\n') iStart--;
	int iEnd = iOffset;
	while (iEnd < sText.Length && sText[iEnd] != '\n') iEnd++;
	if (iEnd > iStart && sText[iEnd - 1] == '\r') iEnd--;
	try {return sText.Substring(iStart, iEnd - iStart);} catch {return "";}
	} // MarkdownReview_GetLineAt method

	// Build a "Type: text" label for an element from the source text alone,
	// without moving the real caret (so no spurious screen-reader output and no
	// scroll/selection side effects). Prefix-with-type ordering chosen for the
	// elements list per accessibility review.
	private static string MarkdownReview_BuildLabelAt(MdiChild child, string sText, MarkdownReviewKind kind, int iOffset) {
	string sLine = MarkdownReview_GetLineAt(sText, iOffset);
	string sTrim = (sLine == null) ? "" : sLine.Trim();

	switch (kind) {
	case MarkdownReviewKind.Heading:
	int iLevel;
	string sHeading;
	if (MarkdownReview_IsHeadingLine(sLine, out iLevel, out sHeading)) {
	if (sHeading.Length == 0) return "Heading " + iLevel;
	return "Heading " + iLevel + ": " + sHeading;
	}
	return sTrim.Length == 0 ? "Heading" : "Heading: " + sTrim;
	case MarkdownReviewKind.List:
	string sItem;
	if (MarkdownReview_TryGetListItemText(sLine, out sItem)) return sItem.Length == 0 ? "List" : "List: " + sItem;
	return sTrim.Length == 0 ? "List" : "List: " + sTrim;
	case MarkdownReviewKind.Link:
	string sLink = MarkdownReview_BuildLinkLabelAt(child, sText, iOffset);
	return sLink.Length == 0 ? "Link" : "Link: " + sLink;
	case MarkdownReviewKind.Table:
	string sTable = MarkdownReview_FormatTableLine(sLine);
	return sTable.Length == 0 ? "Table" : "Table: " + sTable;
	default:
	return sTrim;
	}
	} // MarkdownReview_BuildLabelAt method

	// Resolve a human label for a link at iOffset using the cached inline-link
	// spans (text, then destination), falling back to the bare URL token that
	// starts at iOffset. Pure: reads only from sText, never the live control.
	private static string MarkdownReview_BuildLinkLabelAt(MdiChild child, string sText, int iOffset) {
	try {
	int[] span;
	if (child != null && child.MarkdownReviewInlineLinks != null &&
	MarkdownReview_TryGetInlineLinkSpanAtIndex(child.MarkdownReviewInlineLinks, iOffset, out span) &&
	span != null && span.Length >= 6) {
	string sTextPart = "";
	try {sTextPart = sText.Substring(span[2], span[3] - span[2]);} catch {sTextPart = "";}
	sTextPart = MarkdownReview_CollapseWhitespace(sTextPart);
	if (sTextPart.Length > 0) return sTextPart;
	string sDestRaw = "";
	try {sDestRaw = sText.Substring(span[4], span[5] - span[4]);} catch {sDestRaw = "";}
	string sDest = MarkdownReview_NormalizeUrl(sDestRaw.Trim().Trim('<', '>'));
	if (sDest.Length > 0) return sDest;
	}
	}
	catch {}

	try {
	int iEnd = iOffset;
	while (iEnd < sText.Length && !Char.IsWhiteSpace(sText[iEnd])) iEnd++;
	string sUrl = sText.Substring(iOffset, iEnd - iOffset).Trim().Trim('<', '>');
	if (sUrl.Length > 0) return MarkdownReview_NormalizeUrl(sUrl);
	}
	catch {}
	return "";
	} // MarkdownReview_BuildLinkLabelAt method

	private void MarkdownReview_Announce(string sMessage) {
	// Speak the mode change authoritatively: cancel NVDA's pending speech (the
	// focus change to/from the preview otherwise queues the control name and
	// buries our word, which made the direction feel reversed) and force-speak
	// via the global path so our label is the last, unambiguous thing heard.
	try {if (Win32.IsNVDAActive()) Win32.NVDACancelSpeech();} catch {}
	AddMessage(sMessage, true);
	} // MarkdownReview_Announce method

	private void MarkdownReview_ToggleCurrent(bool bNoSync) {
	MdiChild child = this.Child;
	if (child == null) return;

	if (child.MarkdownReviewMode) {
	// Already previewing.  Rule agreed with Kasperczak (Telegram
	// 14.08.2026 19:17, "Tak przyjmuje. Zgadza sie."): plain Escape is the
	// ONE and ONLY way out of the preview, no matter which mode you entered
	// with, and Shift+Escape never leaves -- it only flips the
	// synchronization in place.  That way the two keys cannot get in each
	// other's way and Escape always means "back to editing".
	if (bNoSync) MarkdownReview_SwitchSyncMode(child, !child.MarkdownReviewNoSync);
	else MarkdownReview_Exit(child);
	return;
	}

	if (!MarkdownReview_IsMarkdownFile(child.File)) {
	AddMessage("Markdown review mode is only for .md files!");
	return;
	}

	MarkdownReview_Enter(child, bNoSync);
	} // MarkdownReview_ToggleCurrent method

	private void MarkdownReview_SwitchSyncMode(MdiChild child, bool bNoSync) {
	if (child == null) return;
	HomerRichTextBox rtb = child.RTB;
	if (rtb == null) return;

	if (bNoSync) {
	// Synced -> detached: remember where editing was and stop syncing the
	// editor caret to the preview caret.
	child.MarkdownReviewSavedEditPos = rtb.SelectionStart;
	child.MarkdownReviewSavedEditLength = rtb.SelectionLength;
	try {
	if (child.MarkdownReviewRtbSelectionHandler != null) rtb.SelectionChanged -= child.MarkdownReviewRtbSelectionHandler;
	}
	catch {}
	child.MarkdownReviewRtbSelectionHandler = null;
	child.MarkdownReviewNoSync = true;
	MarkdownReview_Announce("Detached");
	}
	else {
	// Detached -> synced: reattach the editor->preview sync and align the
	// preview to the current editor caret.
	child.MarkdownReviewNoSync = false;
	if (child.MarkdownReviewRtbSelectionHandler == null) {
	try {
	EventHandler eh = delegate(object o, EventArgs e) {MarkdownReview_SyncViewToEdit(child);};
	child.MarkdownReviewRtbSelectionHandler = eh;
	rtb.SelectionChanged += eh;
	}
	catch {}
	}
	try {MarkdownReview_SyncViewToEdit(child);} catch {}
	MarkdownReview_Announce("Synced");
	}
	} // MarkdownReview_SwitchSyncMode method

	private void MarkdownReview_Enter(MdiChild child, bool bNoSync) {
	if (child == null) return;
	HomerRichTextBox rtb = child.RTB;
	if (rtb == null) return;

		child.MarkdownReviewOldGuard = rtb.ReadOnly;
			child.MarkdownReviewMode = true;
			child.MarkdownReviewNoSync = bNoSync;
			child.MarkdownReviewSavedEditPos = rtb.SelectionStart;
			child.MarkdownReviewSavedEditLength = rtb.SelectionLength;
			child.MarkdownReviewLastHeadingRowStart = -1;
			child.MarkdownReviewLastLinkRowStart = -1;
			child.MarkdownReviewAnnounceHeading = false;
		rtb.SetGuard(true);

	MarkdownReview_EnsureCache(child);
	MarkdownReview_ShowView(child);
	MarkdownReview_Announce(bNoSync ? "Preview detached" : "Preview");
	} // MarkdownReview_Enter method

	private void MarkdownReview_Exit(MdiChild child) {
	if (child == null) return;
	HomerRichTextBox rtb = child.RTB;
	if (rtb == null) return;

	bool bRestore = child.MarkdownReviewNoSync;
	int iRestorePos = child.MarkdownReviewSavedEditPos;
	int iRestoreLen = child.MarkdownReviewSavedEditLength;

	MarkdownReview_HideView(child);
	child.MarkdownReviewMode = false;
	child.MarkdownReviewNoSync = false;
	rtb.SetGuard(child.MarkdownReviewOldGuard);

	// Detached preview: return the caret to where editing was when the
	// detached preview was entered, regardless of where the user browsed.
	if (bRestore && iRestorePos >= 0) {
	try {
	int iMax = rtb.TextLength;
	int iPos = iRestorePos; if (iPos > iMax) iPos = iMax;
	int iLen = iRestoreLen; if (iPos + iLen > iMax) iLen = iMax - iPos;
	rtb.Select(iPos, iLen < 0 ? 0 : iLen);
	rtb.ScrollToCaret();
	}
	catch {}
	}
	MarkdownReview_Announce("Editing");
	} // MarkdownReview_Exit method

	private void MarkdownReview_ShowView(MdiChild child) {
	if (child == null) return;
	HomerRichTextBox rtb = child.RTB;
	if (rtb == null) return;

		if (child.MarkdownReviewView == null || child.MarkdownReviewView.IsDisposed) {
		MarkdownReviewTextBox view = new MarkdownReviewTextBox();
		view.AccessibleRole = AccessibleRole.Document;
		view.AccessibleName = "Markdown preview";
		view.AutoWordSelection = false;
		view.Dock = DockStyle.Fill;
	view.Multiline = true;
	view.ReadOnly = true;
	view.ScrollBars = rtb.ScrollBars;
	view.WordWrap = rtb.WordWrap;
	view.Font = rtb.Font;
	view.ForeColor = rtb.ForeColor;
	view.BackColor = rtb.BackColor;
	view.SelectionChanged += delegate(object o, EventArgs e) {MarkdownReview_SyncFromView(child);};
		child.Controls.Add(view);
		child.MarkdownReviewView = view;
		}
		else {
		try {child.MarkdownReviewView.Visible = true;} catch {}
		}

		if (child.MarkdownReviewRtbSelectionHandler == null && !child.MarkdownReviewNoSync) {
		try {
		EventHandler eh = delegate(object o, EventArgs e) {MarkdownReview_SyncViewToEdit(child);};
		child.MarkdownReviewRtbSelectionHandler = eh;
		rtb.SelectionChanged += eh;
		}
		catch {}
		}

		MarkdownReview_RenderView(child);

		try {
		child.MarkdownReviewView.BringToFront();
		child.MarkdownReviewView.Focus();
	}
	catch {}
	} // MarkdownReview_ShowView method

		private void MarkdownReview_HideView(MdiChild child) {
		if (child == null) return;
		if (child.MarkdownReviewView == null) return;

		try {
		if (child.RTB != null && child.MarkdownReviewRtbSelectionHandler != null) child.RTB.SelectionChanged -= child.MarkdownReviewRtbSelectionHandler;
		}
		catch {}
		child.MarkdownReviewRtbSelectionHandler = null;

		try {child.MarkdownReviewView.Visible = false;} catch {}

		try {
		if (child.RTB != null) {
		child.RTB.BringToFront();
		child.RTB.Focus();
		child.RTB.ScrollToCaret();
		}
		}
		catch {}
		} // MarkdownReview_HideView method

	private void MarkdownReview_SyncFromView(MdiChild child) {
	if (child == null) return;
	if (child.MarkdownReviewSync) return;
	if (child.MarkdownReviewNoSync) return;
	if (!child.MarkdownReviewMode) return;
	if (child.MarkdownReviewView == null) return;
	HomerRichTextBox rtb = child.RTB;
	if (rtb == null) return;

		try {
		child.MarkdownReviewSync = true;
		int iStart = child.MarkdownReviewView.SelectionStart;
		int iLength = child.MarkdownReviewView.SelectionLength;
		int iSourceStart = iStart;
		int iSourceLength = iLength;
		int[] aViewToSource = child.MarkdownReviewViewToSource;
		if (aViewToSource != null && aViewToSource.Length == child.MarkdownReviewView.TextLength + 1) {
		int iViewStart = iStart;
		int iViewEnd = iStart + iLength;
		if (iViewStart < 0) iViewStart = 0;
		if (iViewEnd < 0) iViewEnd = 0;
		if (iViewStart >= aViewToSource.Length) iViewStart = aViewToSource.Length - 1;
		if (iViewEnd >= aViewToSource.Length) iViewEnd = aViewToSource.Length - 1;
		iSourceStart = aViewToSource[iViewStart];
		int iSourceEnd = aViewToSource[iViewEnd];
		iSourceLength = iSourceEnd - iSourceStart;
		if (iSourceLength < 0) {
		iSourceStart = iSourceEnd;
		iSourceLength = 0;
		}
		}

		if (iSourceStart < 0) iSourceStart = 0;
		if (iSourceStart > rtb.TextLength) iSourceStart = rtb.TextLength;
		if (iSourceLength < 0) iSourceLength = 0;
		if (iSourceStart + iSourceLength > rtb.TextLength) iSourceLength = rtb.TextLength - iSourceStart;

		if (iSourceStart != rtb.SelectionStart || iSourceLength != rtb.SelectionLength) {
		rtb.Select(iSourceStart, iSourceLength);
		}

			// Announce headings when moving with caret (e.g. arrow keys).
				try {
				string sLine = rtb.RowText;
				bool bAnnounceNavigation = child.MarkdownReviewAnnounceHeading;
				int iLevel;
				string sHeading;
				if (MarkdownReview_IsHeadingLine(sLine, out iLevel, out sHeading)) {
				child.MarkdownReviewAnnounceHeading = false;
				int iRowStart = rtb.RowStart;
				if (iRowStart != child.MarkdownReviewLastHeadingRowStart) {
				child.MarkdownReviewLastHeadingRowStart = iRowStart;
				if (bAnnounceNavigation) Util.Say("Heading level " + iLevel);
				}
				}
				else {
				child.MarkdownReviewLastHeadingRowStart = -1;
				child.MarkdownReviewAnnounceHeading = false;
				}
				string sLinkAnnouncement;
				if (MarkdownReview_TryGetLineLinkAnnouncement(child, rtb, iSourceStart, out sLinkAnnouncement)) {
				int iRowStart = rtb.RowStart;
				if (iRowStart != child.MarkdownReviewLastLinkRowStart) {
				child.MarkdownReviewLastLinkRowStart = iRowStart;
				if (bAnnounceNavigation) Util.Say(sLinkAnnouncement);
				}
				}
				else {
				child.MarkdownReviewLastLinkRowStart = -1;
				}
				}
				catch {}
		}
		catch {}
		finally {
		child.MarkdownReviewSync = false;
		}
		} // MarkdownReview_SyncFromView method

		private static bool MarkdownReview_TryGetLineLinkAnnouncement(MdiChild child, HomerRichTextBox rtb, int iSourceIndex, out string sMessage) {
		sMessage = "";
		if (child == null || rtb == null) return false;
		try {
		MarkdownReview_EnsureCacheStatic(child);
		int iRowStart = rtb.RowStart;
		string sLine = rtb.RowText ?? "";
		int iLineEnd = iRowStart + sLine.Length;
		if (iLineEnd < iRowStart) iLineEnd = iRowStart;

		if (child.MarkdownReviewInlineLinks != null) {
		foreach (int[] span in child.MarkdownReviewInlineLinks) {
		if (span == null || span.Length < 6) continue;
		if (span[0] < iRowStart || span[0] >= iLineEnd) continue;
		if (span[0] <= iSourceIndex && iSourceIndex <= span[1]) {
		sMessage = "Link";
		return true;
		}
		if (iSourceIndex >= iRowStart && iSourceIndex <= iLineEnd) {
		sMessage = "Link";
		return true;
		}
		}
		}

		string sUrl;
		int iOffset = iSourceIndex - iRowStart;
		if (iOffset < 0) iOffset = 0;
		if (MarkdownReview_TryGetLinkUrlFromLine(sLine, iOffset, out sUrl) || MarkdownReview_TryGetSingleUrlFromLine(sLine, out sUrl)) {
		sMessage = "Link";
		return true;
		}
		}
		catch {}
		return false;
		} // MarkdownReview_TryGetLineLinkAnnouncement method

		private static void MarkdownReview_EnsureCacheStatic(MdiChild child) {
		try {
		if (App.Frame != null) App.Frame.MarkdownReview_EnsureCache(child);
		}
		catch {}
		} // MarkdownReview_EnsureCacheStatic method

		private void MarkdownReview_SyncViewToEdit(MdiChild child) {
	if (child == null) return;
	if (child.MarkdownReviewSync) return;
	if (!child.MarkdownReviewMode) return;
	if (child.MarkdownReviewView == null) return;
	HomerRichTextBox rtb = child.RTB;
	if (rtb == null) return;

		try {
		child.MarkdownReviewSync = true;
		int iStart = rtb.SelectionStart;
		int iLength = rtb.SelectionLength;
		int iViewStart = iStart;
		int iViewLength = iLength;
		int[] aSourceToView = child.MarkdownReviewSourceToView;
		if (aSourceToView != null && aSourceToView.Length == rtb.TextLength + 1) {
		int iSourceStart = iStart;
		int iSourceEnd = iStart + iLength;
		if (iSourceStart < 0) iSourceStart = 0;
		if (iSourceEnd < 0) iSourceEnd = 0;
		if (iSourceStart >= aSourceToView.Length) iSourceStart = aSourceToView.Length - 1;
		if (iSourceEnd >= aSourceToView.Length) iSourceEnd = aSourceToView.Length - 1;
		iViewStart = aSourceToView[iSourceStart];
		int iViewEnd = aSourceToView[iSourceEnd];
		iViewLength = iViewEnd - iViewStart;
		if (iViewLength < 0) {
		iViewStart = iViewEnd;
		iViewLength = 0;
		}
		}

		if (iViewStart < 0) iViewStart = 0;
		if (iViewLength < 0) iViewLength = 0;
		if (iViewStart > child.MarkdownReviewView.TextLength) iViewStart = child.MarkdownReviewView.TextLength;
		if (iViewStart + iViewLength > child.MarkdownReviewView.TextLength) iViewLength = child.MarkdownReviewView.TextLength - iViewStart;

		if (child.MarkdownReviewView.SelectionStart != iViewStart || child.MarkdownReviewView.SelectionLength != iViewLength) {
		child.MarkdownReviewView.Select(iViewStart, iViewLength);
		child.MarkdownReviewView.ScrollToCaret();
		}
		}
		catch {}
	finally {
	child.MarkdownReviewSync = false;
	}
	} // MarkdownReview_SyncViewToEdit method

		private void MarkdownReview_RenderView(MdiChild child) {
		if (child == null) return;
		HomerRichTextBox rtb = child.RTB;
		if (rtb == null) return;
		if (child.MarkdownReviewView == null) return;

		int iRevision = child.TextRevision;
		try {
		if (child.MarkdownReviewViewRevision == iRevision &&
		child.MarkdownReviewSourceToView != null &&
		child.MarkdownReviewSourceToView.Length == rtb.TextLength + 1 &&
		child.MarkdownReviewViewToSource != null &&
		child.MarkdownReviewViewToSource.Length == child.MarkdownReviewView.TextLength + 1) {
		MarkdownReview_SyncViewToEdit(child);
		return;
		}
		}
		catch {}

		string sSource = rtb.Text;
		int[] aSourceToView;
		int[] aViewToSource;
		string sViewText = MarkdownReview_RenderTextForView(sSource, child.MarkdownReviewInlineLinks, out aSourceToView, out aViewToSource);
		child.MarkdownReviewSourceToView = aSourceToView;
		child.MarkdownReviewViewToSource = aViewToSource;
		child.MarkdownReviewView.SuspendLayout();
		child.MarkdownReviewView.Text = sViewText;
		MarkdownReview_ApplyViewFormatting(child.MarkdownReviewView, sSource, aSourceToView);
		child.MarkdownReviewView.ResumeLayout();
		child.MarkdownReviewViewRevision = iRevision;

		MarkdownReview_SyncViewToEdit(child);
		} // MarkdownReview_RenderView method

		private static string MarkdownReview_RenderTextForView(string sText, List<int[]> inlineLinks, out int[] aSourceToView, out int[] aViewToSource) {
		aSourceToView = new int[1] {0};
		aViewToSource = new int[1] {0};
		if (String.IsNullOrEmpty(sText)) return "";

		int iLen = sText.Length;
		int[] aMap = new int[iLen + 1];
		StringBuilder sb = new StringBuilder(iLen);
		int iView = 0;
		int iInlineLink = 0;
		int[] inlineLinkSpan = (inlineLinks != null && inlineLinks.Count > 0) ? inlineLinks[0] : null;

		bool bInFence = false;
		bool bInHtmlTag = false;
		int iHtmlTagView = -1;
		int iPos = 0;
		while (iPos < iLen) {
		int iLf = sText.IndexOf('\n', iPos);
		bool bHasLf = (iLf >= 0);
		if (!bHasLf) iLf = iLen;
		int iLineTextEnd = iLf;
		bool bHasCr = false;
		if (iLineTextEnd > iPos && sText[iLineTextEnd - 1] == '\r') {
		bHasCr = true;
		iLineTextEnd--;
		}
		string sLine = sText.Substring(iPos, iLineTextEnd - iPos);

		bool bFenceLine = MarkdownReview_IsFenceLine(sLine);
		if (bFenceLine) {
		bInHtmlTag = false;
		iHtmlTagView = -1;
		}
		if (!bInFence && !bFenceLine && MarkdownTableSeparatorRegex.IsMatch(sLine)) {
		// Hide table separator row.
		for (int i = iPos; i < iLineTextEnd; i++) aMap[i] = iView;
		}
		else {
			int iHeadingLevel, iHeadingStart, iHeadingEnd;
			if (!bInFence && !bFenceLine && MarkdownReview_TryGetHeadingSpan(sLine, out iHeadingLevel, out iHeadingStart, out iHeadingEnd)) {
			bool[] aHide = null;
			try {MarkdownReview_GetInlineStyleSpans(sLine, 0, 0, out aHide);} catch {}
			for (int i = iPos; i < iPos + iHeadingStart; i++) aMap[i] = iView;
			for (int i = iPos + iHeadingStart; i < iPos + iHeadingEnd; i++) {
			int j = i - iPos;
			aMap[i] = iView;
			while (inlineLinkSpan != null && i >= inlineLinkSpan[1]) {
			iInlineLink++;
			inlineLinkSpan = (inlineLinks != null && iInlineLink < inlineLinks.Count) ? inlineLinks[iInlineLink] : null;
			}
			if (inlineLinkSpan != null &&
			i >= inlineLinkSpan[0] && i < inlineLinkSpan[1] &&
			!(i >= inlineLinkSpan[2] && i < inlineLinkSpan[3])) continue;
			if (aHide != null && j >= 0 && j < aHide.Length && aHide[j]) continue;
			sb.Append(sText[i]);
			iView++;
			}
				for (int i = iPos + iHeadingEnd; i < iLineTextEnd; i++) aMap[i] = iView;
				}
			else {
				int iMarkerStart = 0;
				int iMarkerLength = 0;
				bool bHasMarker = false;
				if (!bInFence && !bFenceLine) bHasMarker = MarkdownReview_GetListMarkerSpan(sLine, out iMarkerStart, out iMarkerLength);
				bool[] aHide = null;
				try {MarkdownReview_GetInlineStyleSpans(sLine, (bHasMarker ? iMarkerStart : 0), (bHasMarker ? iMarkerLength : 0), out aHide);} catch {}
				bool bUnorderedMarker = false;
			if (bHasMarker && iMarkerLength == 2) {
			try {
			string sTrim = sLine.TrimStart();
		if (sTrim.Length > 0) {
		char c = sTrim[0];
		bUnorderedMarker = (c == '-' || c == '+' || c == '*');
		}
		}
		catch {}
		}

		List<Match> aAuto = null;
		if (!bInFence && !bFenceLine) {
		try {
		aAuto = new List<Match>();
		foreach (Match m in MarkdownAutoLinkRegex.Matches(sLine)) if (m != null && m.Success) aAuto.Add(m);
		}
		catch {}
		}

		// ZNACZNIKI PRZYPISOW W PODGLADZIE (zlecenie 1787840875741-1).  Surowe
		// "[^1]" czytnik wymawia jako nawias i daszek, a Kasperczak chce na tym
		// numerze NACISNAC ENTER, wiec musi go najpierw uslyszec jako przypis.
		// W bloku kodu nie ruszamy nic - tam [^1] to zwykly tekst przykladu,
		// dokladnie tak samo jak przy wstawianiu (bramka z iteracji 2).
		List<Match> aFootnote = null;
		if (!bInFence && !bFenceLine) {
		try {
		aFootnote = new List<Match>();
		foreach (Match m in MarkdownFootnoteRefRegex.Matches(sLine)) if (m != null && m.Success) aFootnote.Add(m);
		}
		catch {}
		}

			int iAuto = 0;
			int iFootnote = 0;
			int i = 0;
				while (i < sLine.Length) {
				if (bInHtmlTag) {
				int iMapView = (iHtmlTagView >= 0) ? iHtmlTagView : iView;
				int iGt = -1;
				try {iGt = sLine.IndexOf('>', i);} catch {iGt = -1;}
				if (iGt >= i) {
				for (int j = i; j <= iGt; j++) aMap[iPos + j] = iMapView;
				i = iGt + 1;
				bInHtmlTag = false;
				iHtmlTagView = -1;
				continue;
				}
				for (int j = i; j < sLine.Length; j++) aMap[iPos + j] = iMapView;
				i = sLine.Length;
				continue;
				}
				int iGlobal = iPos + i;
				while (inlineLinkSpan != null && iGlobal >= inlineLinkSpan[1]) {
				iInlineLink++;
				inlineLinkSpan = (inlineLinks != null && iInlineLink < inlineLinks.Count) ? inlineLinks[iInlineLink] : null;
				}
				if (inlineLinkSpan != null &&
				iGlobal >= inlineLinkSpan[0] && iGlobal < inlineLinkSpan[1] &&
				!(iGlobal >= inlineLinkSpan[2] && iGlobal < inlineLinkSpan[3])) {
				aMap[iGlobal] = iView;
				i++;
				continue;
				}
				if (aHide != null && i >= 0 && i < aHide.Length && aHide[i]) {
				aMap[iPos + i] = iView;
				i++;
				continue;
				}
				// Hide raw HTML tags (common in Pandoc output), but preserve autolinks like <https://...>.
				if (!bInFence && !bFenceLine && i < sLine.Length && sLine[i] == '<') {
				int iGt = -1;
				try {iGt = sLine.IndexOf('>', i + 1);} catch {iGt = -1;}
				if (iGt > i) {
				string sInner = "";
				try {sInner = sLine.Substring(i + 1, iGt - i - 1).Trim();} catch {sInner = "";}
				bool bAutoLink = false;
				try {
				bAutoLink = sInner.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
				sInner.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
				sInner.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);
				}
				catch {bAutoLink = false;}

				if (bAutoLink) {
				aMap[iPos + i] = iView;
				for (int j = i + 1; j < iGt; j++) {
				aMap[iPos + j] = iView;
				sb.Append(sLine[j]);
				iView++;
				}
				aMap[iPos + iGt] = iView;
				i = iGt + 1;
				continue;
				}

				for (int j = i; j <= iGt; j++) aMap[iPos + j] = iView;
				sb.Append(' ');
				iView++;
				i = iGt + 1;
				continue;
				}
				bool bLooksLikeTag = false;
				try {
				char c1 = (i + 1 < sLine.Length) ? sLine[i + 1] : '\0';
				bLooksLikeTag = Char.IsLetter(c1) || c1 == '/' || c1 == '!' || c1 == '?';
				}
				catch {bLooksLikeTag = false;}
				if (bLooksLikeTag) {
				iHtmlTagView = iView;
				for (int j = i; j < sLine.Length; j++) aMap[iPos + j] = iHtmlTagView;
				sb.Append(' ');
				iView++;
				i = sLine.Length;
				bInHtmlTag = true;
				continue;
				}
				}

		// Znacznik przypisu: zamiast "[^1]" pokazujemy "przypis 1", zeby czytnik
		// wymowil go po ludzku i zeby bylo na czym stanac przed Enter.  Cala
		// dlugosc znacznika w zrodle mapuje sie na POCZATEK tego napisu, wiec
		// kursor postawiony gdziekolwiek w napisie wraca do znacznika w zrodle -
		// to jest to, co Enter potem czyta.
		Match mFn = (aFootnote != null && iFootnote < aFootnote.Count) ? aFootnote[iFootnote] : null;
		while (mFn != null && mFn.Index < i) {
		iFootnote++;
		mFn = (aFootnote != null && iFootnote < aFootnote.Count) ? aFootnote[iFootnote] : null;
		}
		if (mFn != null && mFn.Success && mFn.Index == i) {
		string sShown = MarkdownReview_FootnoteMarkerText(mFn.Groups["label"].Value);
		// Spacja rozdzielajaca jest potrzebna TYLKO wtedy, gdy znacznik
		// przylega do wyrazu ("...przypisem[^1]").  Na poczatku wiersza - a
		// tak stoi etykieta przy tresci przypisu na koncu dokumentu - dalaby
		// brzydkie wciecie, wiec ja tam zdejmujemy.
		if (i == 0 || Char.IsWhiteSpace(sLine[i - 1])) sShown = sShown.TrimStart();
		for (int j = 0; j < mFn.Length; j++) aMap[iPos + i + j] = iView;
		for (int j = 0; j < sShown.Length; j++) {
		sb.Append(sShown[j]);
		iView++;
		}
		i += mFn.Length;
		iFootnote++;
		continue;
		}

		Match mAuto = (aAuto != null && iAuto < aAuto.Count) ? aAuto[iAuto] : null;
		if (mAuto != null && mAuto.Success && mAuto.Index == i) {
		int iMatchEnd = mAuto.Index + mAuto.Length;
		int iLocalStart = mAuto.Index;
		int iLocalEnd = iMatchEnd;
			// Skip '<' and '>' brackets.
			for (int j = iLocalStart; j < iLocalEnd; j++) {
			if (j == iLocalStart || j == iLocalEnd - 1) aMap[iPos + j] = iView;
			else {
			aMap[iPos + j] = iView;
			if (aHide != null && j >= 0 && j < aHide.Length && aHide[j]) continue;
			sb.Append(sLine[j]);
			iView++;
			}
			}
		i = iMatchEnd;
		iAuto++;
		continue;
		}

		if (bHasMarker && i == iMarkerStart && iMarkerLength > 0) {
		if (bUnorderedMarker && iMarkerLength >= 2) {
		// Render "- " as "• ".
		aMap[iPos + i] = iView;
		sb.Append('•');
		iView++;
		aMap[iPos + i + 1] = iView;
		sb.Append(' ');
		iView++;
		}
		else {
		for (int j = 0; j < iMarkerLength; j++) {
		aMap[iPos + i + j] = iView;
		sb.Append(sLine[i + j]);
		iView++;
		}
		}
		i += iMarkerLength;
		continue;
		}

		char c0 = sLine[i];
		if (!bInFence && !bFenceLine && c0 == '|') c0 = ' ';
		aMap[iPos + i] = iView;
		sb.Append(c0);
		iView++;
		i++;
		}
		}
		}

		// Map CR (if any) to the current view index; output a single LF.
		if (bHasCr) aMap[iLf - 1] = iView;
		if (bHasLf) {
		aMap[iLf] = iView;
		sb.Append('\n');
		iView++;
		iPos = iLf + 1;
		}
		else {
		iPos = iLf;
		}

		if (bFenceLine) bInFence = !bInFence;
		}

		aMap[iLen] = iView;
		int[] aReverse = new int[iView + 1];
		for (int i = 0; i <= iLen; i++) {
		int v = aMap[i];
		if (v < 0) v = 0;
		if (v > iView) v = iView;
		aReverse[v] = i;
		}

		// DZIURY W MAPIE POWROTNEJ (znalezione przy renderowaniu znacznika
		// przypisu, zlecenie 1787840875741-1).  Dotad kazdy znak WIDOKU mial
		// swoj znak ZRODLA, wiec petla wyzej wypelniala aReverse w calosci.
		// "footnote 1" jest DLUZSZE niz "[^1]", czyli powstaja pozycje widoku,
		// na ktore nie wskazuje zadne zrodlo - i zostawaly tam zera.  Kursor
		// postawiony w takim miejscu wracal na POCZATEK dokumentu, zamiast na
		// znacznik.  Wypelniamy dziury ostatnia znana pozycja zrodla, dzieki
		// czemu kursor w dowolnym miejscu napisu "footnote 1" mapuje sie na
		// znacznik [^1].
		int iLast = 0;
		bool[] aKnown = new bool[iView + 1];
		for (int i = 0; i <= iLen; i++) {
		int v = aMap[i];
		if (v < 0) v = 0;
		if (v > iView) v = iView;
		aKnown[v] = true;
		}
		for (int v = 0; v <= iView; v++) {
		if (aKnown[v]) iLast = aReverse[v];
		else aReverse[v] = iLast;
		}

		aSourceToView = aMap;
		aViewToSource = aReverse;
		return sb.ToString();
		} // MarkdownReview_RenderTextForView method

		private static bool MarkdownReview_TryGetHeadingSpan(string sLine, out int iLevel, out int iTextStart, out int iTextEnd) {
		iLevel = 0;
		iTextStart = 0;
		iTextEnd = 0;
		if (String.IsNullOrEmpty(sLine)) return false;

		string sTrim = sLine.TrimStart();
		int iLeading = sLine.Length - sTrim.Length;
		if (iLeading > 3) return false;

		int i = 0;
		while (i < sTrim.Length && sTrim[i] == '#' && i < 6) i++;
		if (i == 0) return false;
		if (i >= sTrim.Length) return false;
		if (!Char.IsWhiteSpace(sTrim[i])) return false;

		iLevel = i;

		int j = iLeading + i;
		while (j < sLine.Length && Char.IsWhiteSpace(sLine[j])) j++;
		iTextStart = j;

		int iEnd = sLine.Length;
		while (iEnd > iTextStart && Char.IsWhiteSpace(sLine[iEnd - 1])) iEnd--;

		int iHashStart = iEnd;
		while (iHashStart > iTextStart && sLine[iHashStart - 1] == '#') iHashStart--;
		if (iHashStart < iEnd && iHashStart > iTextStart && Char.IsWhiteSpace(sLine[iHashStart - 1])) {
		iEnd = iHashStart - 1;
		while (iEnd > iTextStart && Char.IsWhiteSpace(sLine[iEnd - 1])) iEnd--;
		}

		iTextEnd = iEnd;
		if (iTextEnd < iTextStart) iTextEnd = iTextStart;
		return true;
		} // MarkdownReview_TryGetHeadingSpan method

		private static bool MarkdownReview_GetListMarkerSpan(string sLine, out int iMarkerStart, out int iMarkerLength) {
		iMarkerStart = 0;
		iMarkerLength = 0;
		if (String.IsNullOrEmpty(sLine)) return false;

	int iLeading = sLine.Length - sLine.TrimStart().Length;
	string s = sLine.TrimStart();
	if (s.Length < 2) return false;

	char c = s[0];
	if ((c == '-' || c == '+' || c == '*') && Char.IsWhiteSpace(s[1])) {
	iMarkerStart = iLeading;
	iMarkerLength = 2;
	return true;
	}

	if (!Char.IsDigit(c)) return false;
	int i = 0;
	while (i < s.Length && Char.IsDigit(s[i])) i++;
	if (i == 0 || i + 1 >= s.Length) return false;
	if (s[i] != '.' && s[i] != ')') return false;
	if (!Char.IsWhiteSpace(s[i + 1])) return false;
	iMarkerStart = iLeading;
	iMarkerLength = i + 2;
	return true;
	} // MarkdownReview_GetListMarkerSpan method

	// KURSOR NA PUNKTORZE POZYCJI LISTY (zgloszenie Kasperczaka 27.08.2026 21:33:
	// "NVDA nawiguje na liscie i element listy, ale kursor jest na punktorze, a
	// wewnetrzny link daje sie uaktywnic dopiero, kiedy przesuniemy sie o 2 znaki").
	//
	// Pozycja listy zaczyna sie od znacznika: "- " w liscie punktowanej albo
	// "1. " w numerowanej.  Podglad renderuje "- " jako "* " tej samej dlugosci,
	// wiec kursor postawiony na poczatku wiersza mapuje sie w zrodle na SAM
	// ZNACZNIK - a tam nie ma ani linku, ani znacznika przypisu, bo tresc
	// pozycji zaczyna sie dopiero za nim.  Dla uzytkownika czytnika wygladalo
	// to tak, ze Enter na pozycji spisu tresci nic nie robi.
	//
	// Ten helper zwraca offset PIERWSZEGO ZNAKU TRESCI pozycji, gdy podany
	// offset lezy we wcieciu albo w samym znaczniku; w kazdym innym wypadku
	// zwraca offset bez zmian.  Dotyczy CALEJ KLASY (listy punktowane i
	// numerowane, spis tresci to tylko ich szczegolny przypadek).
	// W bloku kodu nie ruszamy nic - tam "- " to zwykly tekst przykladu.
	private static int MarkdownReview_SkipListMarkerAt(string sText, int iOffset) {
	if (String.IsNullOrEmpty(sText)) return iOffset;
	if (iOffset < 0) iOffset = 0;
	if (iOffset > sText.Length) iOffset = sText.Length;

	const char cLf = (char) 10;
	const char cCr = (char) 13;

	int iLineStart = iOffset;
	while (iLineStart > 0 && sText[iLineStart - 1] != cLf) iLineStart--;
	int iLineEnd = iOffset;
	while (iLineEnd < sText.Length && sText[iLineEnd] != cLf) iLineEnd++;
	int iTextEnd = iLineEnd;
	if (iTextEnd > iLineStart && sText[iTextEnd - 1] == cCr) iTextEnd--;
	if (iTextEnd <= iLineStart) return iOffset;

	string sLine = "";
	try {sLine = sText.Substring(iLineStart, iTextEnd - iLineStart);} catch {return iOffset;}

	int iMarkerStart, iMarkerLength;
	if (!MarkdownReview_GetListMarkerSpan(sLine, out iMarkerStart, out iMarkerLength)) return iOffset;

	try {
	if (MarkdownReview_IsIndexInRangesSorted(MarkdownReview_FindFenceRanges(sText), iLineStart)) return iOffset;
	}
	catch {}

	int iContent = iLineStart + iMarkerStart + iMarkerLength;
	// Pozycja bez tresci ("-" i koniec wiersza): nie ma na czym stanac, wiec
	// zostawiamy kursor tam, gdzie byl.
	if (iContent >= iTextEnd) return iOffset;
	if (iOffset >= iContent) return iOffset;
	return iContent;
	} // MarkdownReview_SkipListMarkerAt method

	// Ta sama korekta zastosowana do CALEJ listy celow nawigacji.  Dotyczy
	// pozycji list (i poczatkow list), bo tylko one zaczynaja sie znacznikiem;
	// naglowki, tabele i linki zostaja bez zmian.
	private static List<int> MarkdownReview_ShiftPastListMarkers(string sText, List<int> source, MarkdownReviewKind kind) {
	if (source == null) return null;
	if (kind != MarkdownReviewKind.List && kind != MarkdownReviewKind.ListItem) return source;
	if (String.IsNullOrEmpty(sText)) return source;
	List<int> result = new List<int>();
	foreach (int i in source) result.Add(MarkdownReview_SkipListMarkerAt(sText, i));
	MarkdownReview_SortUnique(result);
	return result;
	} // MarkdownReview_ShiftPastListMarkers method

		private static void MarkdownReview_ApplyViewFormatting(RichTextBox rtb, string sSource, int[] aSourceToView) {
		if (rtb == null) return;
		if (String.IsNullOrEmpty(sSource)) return;
		if (aSourceToView == null) return;
		if (aSourceToView.Length != sSource.Length + 1) return;

	int iSelStart = rtb.SelectionStart;
	int iSelLength = rtb.SelectionLength;

	List<string> aLines = new List<string>();
	List<int> aStarts = new List<int>();
	int iPos = 0;
	while (true) {
	aStarts.Add(iPos);
	int iEnd = sSource.IndexOf('\n', iPos);
	if (iEnd < 0) iEnd = sSource.Length;
	string sLine = sSource.Substring(iPos, iEnd - iPos);
	if (sLine.EndsWith("\r")) sLine = sLine.Substring(0, sLine.Length - 1);
	aLines.Add(sLine);
	if (iEnd >= sSource.Length) break;
	iPos = iEnd + 1;
	}

		bool bInFence = false;
		Font fontBase = rtb.Font;
		if (fontBase == null) return;
		Dictionary<int, Font> dFonts = new Dictionary<int, Font>();
		Dictionary<string, Font> dInlineFonts = new Dictionary<string, Font>();
		for (int i = 0; i < aLines.Count; i++) {
		string sLine = aLines[i];
		int iStart = aStarts[i];
		if (MarkdownReview_IsFenceLine(sLine)) {
		bInFence = !bInFence;
		continue;
		}
		if (bInFence) continue;

			int iLevel, iHeadingStart, iHeadingEnd;
			if (MarkdownReview_TryGetHeadingSpan(sLine, out iLevel, out iHeadingStart, out iHeadingEnd)) {
			float fSize = fontBase.Size;
			switch (iLevel) {
		case 1:
		fSize += 6;
		break;
		case 2:
		fSize += 4;
		break;
		case 3:
		fSize += 2;
		break;
		default:
		break;
		}
			try {
			Font fontHeading;
			if (!dFonts.TryGetValue(iLevel, out fontHeading)) {
			fontHeading = new Font(fontBase.FontFamily, fSize, FontStyle.Bold);
			dFonts[iLevel] = fontHeading;
			}
			int iSourceStart = iStart + iHeadingStart;
			int iSourceEnd = iStart + iHeadingEnd;
			if (iSourceStart < 0) iSourceStart = 0;
			if (iSourceEnd < 0) iSourceEnd = 0;
			if (iSourceStart > sSource.Length) iSourceStart = sSource.Length;
			if (iSourceEnd > sSource.Length) iSourceEnd = sSource.Length;
			int iViewStart = aSourceToView[iSourceStart];
			int iViewEnd = aSourceToView[iSourceEnd];
			int iViewLength = iViewEnd - iViewStart;
			if (iViewLength > 0) {
			if (iViewStart < 0) iViewStart = 0;
			if (iViewStart > rtb.TextLength) iViewStart = rtb.TextLength;
			if (iViewStart + iViewLength > rtb.TextLength) iViewLength = rtb.TextLength - iViewStart;
			if (iViewLength > 0) {
			rtb.Select(iViewStart, iViewLength);
			rtb.SelectionFont = fontHeading;
			}
			}
			}
			catch {}
			}

			try {
			int iMarkerStart = 0;
			int iMarkerLength = 0;
			bool bHasMarker = MarkdownReview_GetListMarkerSpan(sLine, out iMarkerStart, out iMarkerLength);
			bool[] aHide;
			List<MarkdownReviewInlineSpan> spans = MarkdownReview_GetInlineStyleSpans(sLine, (bHasMarker ? iMarkerStart : 0), (bHasMarker ? iMarkerLength : 0), out aHide);
			foreach (MarkdownReviewInlineSpan span in spans) {
			if (span == null) continue;
			int iSourceStart = iStart + span.Start;
			int iSourceEnd = iStart + span.End;
			if (iSourceStart < 0) iSourceStart = 0;
			if (iSourceEnd < 0) iSourceEnd = 0;
			if (iSourceStart > sSource.Length) iSourceStart = sSource.Length;
			if (iSourceEnd > sSource.Length) iSourceEnd = sSource.Length;
			int iViewStart = aSourceToView[iSourceStart];
			int iViewEnd = aSourceToView[iSourceEnd];
			int iViewLength = iViewEnd - iViewStart;
			if (iViewLength <= 0) continue;
			if (iViewStart < 0) iViewStart = 0;
			if (iViewStart > rtb.TextLength) iViewStart = rtb.TextLength;
			if (iViewStart + iViewLength > rtb.TextLength) iViewLength = rtb.TextLength - iViewStart;
			if (iViewLength <= 0) continue;
			rtb.Select(iViewStart, iViewLength);
			Font fontCurrent = rtb.SelectionFont;
			if (fontCurrent == null) fontCurrent = fontBase;
			FontStyle styleNew = fontCurrent.Style | span.Style;
			string sKey = fontCurrent.FontFamily.Name + "|" + fontCurrent.Size.ToString(CultureInfo.InvariantCulture) + "|" + ((int) styleNew).ToString();
			Font fontInline;
			if (!dInlineFonts.TryGetValue(sKey, out fontInline)) {
			fontInline = new Font(fontCurrent.FontFamily, fontCurrent.Size, styleNew);
			dInlineFonts[sKey] = fontInline;
			}
			rtb.SelectionFont = fontInline;
			}
			}
			catch {}
		}

	try {
	rtb.Select(iSelStart, iSelLength);
	}
	catch {}

		try {
		foreach (Font f in dFonts.Values) if (f != null) f.Dispose();
		foreach (Font f in dInlineFonts.Values) if (f != null) f.Dispose();
		}
		catch {}
		} // MarkdownReview_ApplyViewFormatting method

	private void MarkdownReview_Navigate(MdiChild child, MarkdownReviewKind kind, bool bReverse) {
	if (child == null) return;
	HomerRichTextBox rtb = child.RTB;
	if (rtb == null) return;

	MarkdownReview_EnsureCache(child);
	List<int> list = MarkdownReview_GetList(child, kind);
	if (list == null || list.Count == 0) {
	AddMessage("No " + MarkdownReview_GetKindPlural(kind) + "!");
	return;
	}

	int iCurrent = rtb.Index;
	// KURSOR ZA PUNKTOREM (zgloszenie 27.08.2026 21:33).  Cala lista celow jest
	// korygowana RAZEM z pozycja biezaca, bo inaczej skok w tyl wracalby na
	// punktor tej samej pozycji i komenda stalaby w miejscu.
	List<int> aFixed = MarkdownReview_ShiftPastListMarkers(rtb.Text, list, kind);
	int iTarget = MarkdownReview_FindTarget(aFixed, iCurrent, bReverse);
	if (iTarget < 0) {
	AddMessage("No " + (bReverse ? "previous" : "next") + " " + MarkdownReview_GetKindName(kind) + "!");
	return;
	}

	try {rtb.Select(iTarget, 0);} catch {}
		string sMessage = MarkdownReview_FormatMessage(kind, child, rtb, iTarget);
		try {if (Win32.IsNVDAActive()) Win32.NVDACancelSpeech();} catch {}
		AddMessage(sMessage);
		} // MarkdownReview_Navigate method

	private static int MarkdownReview_FindTarget(List<int> list, int iCurrent, bool bReverse) {
	if (list == null || list.Count == 0) return -1;
	if (!bReverse) {
	for (int i = 0; i < list.Count; i++) if (list[i] > iCurrent) return list[i];
	return -1;
	}
	else {
	for (int i = list.Count - 1; i >= 0; i--) if (list[i] < iCurrent) return list[i];
	return -1;
	}
	} // MarkdownReview_FindTarget method

	private static List<int> MarkdownReview_GetList(MdiChild child, MarkdownReviewKind kind) {
	switch (kind) {
	case MarkdownReviewKind.Heading:
	return child.MarkdownReviewHeadings;
	case MarkdownReviewKind.List:
	return child.MarkdownReviewLists;
	case MarkdownReviewKind.ListItem:
	return child.MarkdownReviewListItems;
	case MarkdownReviewKind.Link:
	return child.MarkdownReviewLinks;
	case MarkdownReviewKind.Table:
	return child.MarkdownReviewTables;
	default:
	return null;
	}
	} // MarkdownReview_GetList method

	private static bool MarkdownReview_TryGetInlineLinkSpanAtIndex(List<int[]> inlineLinks, int iIndex, out int[] span) {
	span = null;
	if (inlineLinks == null || inlineLinks.Count == 0) return false;
	int lo = 0;
	int hi = inlineLinks.Count - 1;
	while (lo <= hi) {
	int mid = (lo + hi) / 2;
	int[] s = inlineLinks[mid];
	if (s == null || s.Length < 6) return false;
	if (iIndex < s[0]) hi = mid - 1;
	else if (iIndex >= s[1]) lo = mid + 1;
	else {
	span = s;
	return true;
	}
	}
	return false;
	} // MarkdownReview_TryGetInlineLinkSpanAtIndex method

	private static string MarkdownReview_FormatTableLine(string sLine) {
	if (String.IsNullOrEmpty(sLine)) return "";
	string s = sLine.Trim();
	if (s.Length == 0) return "";
	if (s.StartsWith("|")) s = s.Substring(1);
	if (s.EndsWith("|")) s = s.Substring(0, s.Length - 1);
	string[] a = s.Split('|');
	List<string> list = new List<string>();
	foreach (string t in a) {
	string u = t.Trim();
	if (u.Length == 0) continue;
	list.Add(u);
	}
	if (list.Count == 0) return sLine.Trim();
	return String.Join(" | ", list.ToArray());
	} // MarkdownReview_FormatTableLine method

	private static void MarkdownReview_AddLinksFromLine(string sLine, int iLineStart, List<int> links) {
	if (String.IsNullOrEmpty(sLine)) return;

	try {
	List<int[]> spans = new List<int[]>();
	foreach (Match m in MarkdownInlineLinkRegex.Matches(sLine)) {
	if (m == null || !m.Success) continue;
	links.Add(iLineStart + m.Index);
	spans.Add(new int[] {m.Index, m.Index + m.Length});
	}
	foreach (Match m in MarkdownAutoLinkRegex.Matches(sLine)) {
	if (m == null || !m.Success) continue;
	links.Add(iLineStart + m.Index);
	spans.Add(new int[] {m.Index, m.Index + m.Length});
	}
	foreach (Match m in MarkdownBareUrlRegex.Matches(sLine)) {
	if (m == null || !m.Success) continue;
	if (MarkdownReview_IsIndexInAnySpan(m.Index, spans)) continue;
	links.Add(iLineStart + m.Index);
	spans.Add(new int[] {m.Index, m.Index + m.Length});
	}
	foreach (Match m in MarkdownWwwUrlRegex.Matches(sLine)) {
	if (m == null || !m.Success) continue;
	if (MarkdownReview_IsIndexInAnySpan(m.Index, spans)) continue;
	links.Add(iLineStart + m.Index);
	spans.Add(new int[] {m.Index, m.Index + m.Length});
	}
	}
	catch {}
	} // MarkdownReview_AddLinksFromLine method

		private static string MarkdownReview_FormatMessage(MarkdownReviewKind kind, MdiChild child, HomerRichTextBox rtb, int iTarget) {
		string sLine = "";
		try {
		sLine = rtb.RowText;
		}
	catch {}
	sLine = (sLine == null) ? "" : sLine.Trim();

	switch (kind) {
	case MarkdownReviewKind.Heading:
	int iLevel;
	string sHeading;
	if (MarkdownReview_IsHeadingLine(sLine, out iLevel, out sHeading)) {
	if (sHeading.Length == 0) return "Heading " + iLevel;
	return sHeading + " Heading " + iLevel;
	}
	if (sLine.Length == 0) return "Heading";
	return sLine + " Heading";
	case MarkdownReviewKind.List:
	string sItem;
	if (MarkdownReview_TryGetListItemText(sLine, out sItem)) {
	if (sItem.Length == 0) return "List";
	return sItem + " List";
	}
	if (sLine.Length == 0) return "List";
	return sLine + " List";
		case MarkdownReviewKind.ListItem:
		if (MarkdownReview_TryGetListItemText(sLine, out sItem)) {
		if (sItem.Length == 0) return "Item";
		return sItem + " Item";
		}
		if (sLine.Length == 0) return "Item";
		return sLine + " Item";
		case MarkdownReviewKind.Link:
		return MarkdownReview_FormatLinkMessage(child, rtb, iTarget);
		case MarkdownReviewKind.Table:
		string sTable = MarkdownReview_FormatTableLine(sLine);
		if (sTable.Length == 0) return "Table";
		return sTable + " Table";
	default:
		return sLine;
		}
		} // MarkdownReview_FormatMessage method

	private static string MarkdownReview_FormatLinkMessage(MdiChild child, HomerRichTextBox rtb, int iTarget) {
	string sLine = "";
	try {
	sLine = rtb.RowText ?? "";
	}
	catch {}

	try {
	int[] span;
	if (child != null && child.RTB != null &&
	child.MarkdownReviewInlineLinks != null &&
	MarkdownReview_TryGetInlineLinkSpanAtIndex(child.MarkdownReviewInlineLinks, iTarget, out span) &&
	span != null && span.Length >= 6 &&
	iTarget >= span[2] && iTarget < span[3]) {
	string sSource = "";
	try {sSource = child.RTB.Text ?? "";} catch {sSource = "";}

	string sText = "";
	try {sText = sSource.Substring(span[2], span[3] - span[2]);} catch {sText = "";}
	sText = MarkdownReview_CollapseWhitespace(sText);

	string sDestRaw = "";
	try {sDestRaw = sSource.Substring(span[4], span[5] - span[4]);} catch {sDestRaw = "";}

		string sDest = MarkdownReview_NormalizeUrl(sDestRaw);
	if (sDest.Length == 0) {
	sDest = sDestRaw.Trim();
	try {
	if (sDest.StartsWith("<") && sDest.EndsWith(">") && sDest.Length > 2) {
	sDest = sDest.Substring(1, sDest.Length - 2).Trim();
	}
	else {
	int iSpace = sDest.IndexOfAny(new char[] {' ', '\t', '\r', '\n'});
	if (iSpace > 0) sDest = sDest.Substring(0, iSpace).Trim();
	}
	}
	catch {}
	try {sDest = Util.Unquote(sDest).Trim();} catch {}
	sDest = MarkdownReview_CleanUrl(sDest);
	if (sDest.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) sDest = "https://" + sDest;
	}

		if (sText.Length > 0) return sText + " Link";
		if (sDest.Length > 0) return sDest + " Link";
		return "Link";
	}

	int iOffset = 0;
	try {
	iOffset = iTarget - rtb.RowStart;
	if (iOffset < 0) iOffset = 0;
	}
	catch {
	iOffset = 0;
	}

	foreach (Match m in MarkdownAutoLinkRegex.Matches(sLine)) {
	if (m == null || !m.Success) continue;
	if (m.Index <= iOffset && iOffset < m.Index + m.Length) return MarkdownReview_NormalizeUrl(m.Value.Trim('<', '>')) + " Link";
	}
	foreach (Match m in MarkdownBareUrlRegex.Matches(sLine)) {
	if (m == null || !m.Success) continue;
	if (m.Index <= iOffset && iOffset < m.Index + m.Length) return MarkdownReview_NormalizeUrl(m.Value) + " Link";
	}
	foreach (Match m in MarkdownWwwUrlRegex.Matches(sLine)) {
	if (m == null || !m.Success) continue;
	if (m.Index <= iOffset && iOffset < m.Index + m.Length) return MarkdownReview_NormalizeUrl(m.Value) + " Link";
	}
	}
	catch {}

	if (sLine == null) return "Link";
	sLine = sLine.Trim();
	if (sLine.Length == 0) return "Link";
	return sLine + " Link";
	} // MarkdownReview_FormatLinkMessage method

	private static bool MarkdownReview_TryGetLinkUrlAtCursor(MdiChild child, out string sUrl) {
	sUrl = "";
	if (child == null) return false;
	HomerRichTextBox rtb = child.RTB;
	if (rtb == null) return false;

	string sSource = "";
	int iIndex = 0;
	try {
	sSource = rtb.Text ?? "";
	iIndex = rtb.SelectionStart;
	}
	catch {
	sSource = "";
	iIndex = 0;
	}
	if (iIndex < 0) iIndex = 0;
	if (iIndex > sSource.Length) iIndex = sSource.Length;

	try {
	int[] span;
	if (child.MarkdownReviewInlineLinks != null &&
	MarkdownReview_TryGetInlineLinkSpanAtIndex(child.MarkdownReviewInlineLinks, iIndex, out span) &&
	span != null && span.Length >= 6 &&
	iIndex >= span[2] && iIndex < span[3]) {
	string sDestRaw = "";
	try {sDestRaw = sSource.Substring(span[4], span[5] - span[4]);} catch {sDestRaw = "";}
	string sNorm = MarkdownReview_NormalizeUrl(sDestRaw);
	if (sNorm.Length > 0) {
	sUrl = sNorm;
	return true;
	}
	}
	}
	catch {}

	string sLine = "";
	int iOffset = 0;
	try {
	sLine = rtb.RowText ?? "";
	iOffset = iIndex - rtb.RowStart;
	}
	catch {
	sLine = "";
	iOffset = 0;
	}
	if (iOffset < 0) iOffset = 0;
	return MarkdownReview_TryGetLinkUrlFromLine(sLine, iOffset, out sUrl);
	} // MarkdownReview_TryGetLinkUrlAtCursor method

	private static bool MarkdownReview_TryOpenUrl(string sUrl, out string sError) {
	sError = "";
	string s = MarkdownReview_NormalizeUrl(sUrl);
	if (s.Length == 0) {
	sError = "Invalid URL.";
	return false;
	}

	try {
	ProcessStartInfo psi = new ProcessStartInfo();
	psi.FileName = s;
	psi.UseShellExecute = true;
	Process.Start(psi);
	return true;
	}
	catch (Exception ex) {
	sError = ex.Message;
	return false;
	}
	} // MarkdownReview_TryOpenUrl method
} // MdiFrame class

// MarkdownReviewTextBox: the read-only rendered preview shown in Markdown
// review mode. Routes command keys back through the frame's central
// dispatcher so review-mode shortcuts (Escape to leave, h/l/i/k/t
// navigation, etc.) work while focus is in the preview.
public class MarkdownReviewTextBox : RichTextBox {

protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
if (App.Frame != null && App.Frame.ProcessCmdKey_Helper(ref msg, keyData)) return true;
return base.ProcessCmdKey(ref msg, keyData);
} // ProcessCmdKey handler

} // MarkdownReviewTextBox class


public class HomerRichTextBox : RichTextBox {
public int OldIndex = -1;
// Use the MODERN RichEdit window class (RICHEDIT50W from msftedit.dll) instead
// of the riched20 class WinForms creates by default.  This is a screen reader
// fix, not cosmetics: the old class gets Polish word boundaries WRONG, and a
// screen reader asks the control itself where a word starts and ends.
// MEASURED 17.08.2026 on "Parafia Wszystkich Swietych oraz zarowka":
//   riched20        word walk = [Parafia ][Wszystkich S][wietych ][oraz z][arowka ]
//                   word at the accented letter = "Wszystkich Swietych"
//   RICHEDIT50W     word walk = [Parafia ][Wszystkich ][Swietych ][oraz ][zarowka ]
//                   word at the accented letter = "Swietych"
// So the old class both SPLITS a Polish word and REPORTS the previous word glued
// to the current one -- exactly what Kasperczak heard (Telegram 17.08.2026):
// "Parafia Wszystkich S Wszystkich Swietych" and "Doklada slowo, jesli jest
// polska literka", while plain ASCII words were "poprawnie".
// The knobs did NOT help (all measured, all still wrong on riched20):
// EM_SETLANGOPTIONS 0 / IMF_UIFONTS (so AutoFont/DualFont is not the cause), an
// explicit Unicode-covering font, and EM_SETWORDBREAKPROC -- which riched20
// silently ignores: the callback was invoked ZERO times.  Changing the window
// class is therefore the only fix that reaches the component doing it wrong.
// Compatibility checked against everything EdSharp does with the control
// (28 assertions PASS on the new class): plain text and Rtf round trip with
// Polish letters, Lines/GetLineFromCharIndex/GetFirstCharIndexFromLine,
// SelectedText, SelectionFont (used in 47 places), LoadFile/SaveFile, Find,
// Undo/CanUndo, ReadOnly (the guard feature), WordWrap, ZoomFactor, DetectUrls.
protected override CreateParams CreateParams {
get {
// Must be loaded before the window is created, or the class is unknown and
// the control silently falls back to the old one.
if (hMsftEdit == IntPtr.Zero) hMsftEdit = Win32.LoadLibrary("msftedit.dll");
CreateParams cp = base.CreateParams;
if (hMsftEdit != IntPtr.Zero) cp.ClassName = "RICHEDIT50W";
return cp;
}
} // CreateParams property
private static IntPtr hMsftEdit = IntPtr.Zero;
public int OldTextLength = -1;
// Ile ms czekac po ustawieniu kursora.  Statyczne: wartosc jest wspolna dla
// wszystkich okien i czytana raz.
private static int iCaretMoveDelayMs = 0;
public static string CR = "\r";
public static string LF = "\n";
public static string LB = LF;
public static string LineBreak = Environment.NewLine;
public static string FF = "\f";
public static string SB = FF + LB;
public static string DD = "----------";
public static string SectionBreak = LB + DD + LB + SB;
public static string EOD = LB + DD + LB + "End of Document" + LB;

public bool IndentMode = false;
public int IndentLevels = 0;
public int Index {
get {
return this.SelectionStart + this.SelectionLength;
}
set {
this.DeselectAll();
this.SelectionStart = value;
this.ScrollToCaret();
this.Update();
this.Refresh();
Application.DoEvents();
// CZEKANIE PO KAZDYM RUCHU KURSORA - USUNIETE Z DOMYSLNEGO DZIALANIA
// (zadanie 5, 12.09.2026).
//
// Stalo tu Thread.Sleep(100) - bezwarunkowo, przy KAZDYM ustawieniu
// kursora.  ZMIERZONE (testy/pomiar_kursor.cs) na zywej kontrolce:
//   jeden ruch kursora z tym czekaniem   114,4 ms
//   ten sam ruch bez niego                 2,9 ms
//   20 ruchow pod rzad: 2287 ms wobec 58 ms
// Czyli 97 procent kosztu ruchu kursora to bylo samo czekanie, a nie praca
// kontrolki - odswiezenie jest szybkie.  Dwadziescia ruchow to ponad dwie
// sekundy stania programu, i to jest druga polowa "zamarzania kursora":
// pierwsza to ukrywanie karetki (patrz konstruktor, HideSelection).
//
// UBOCZNY SKUTEK, KTORY TRZEBA ZNAC: czytnik ekranu czeka na ruch karetki
// najwyzej 100 ms (NVDA, caretMoveTimeoutMs).  Ruch trwal 114 ms, czyli
// WYPADAL NA GRANICY tego okna - raz w nim, raz poza nim.  To wyjasnione
// juz w komentarzu przy bNavigateReaderSaysAll: stad "czyta 2 razy, ale
// tak jakby tez nie zawsze".  Teraz ruch trwa 3 ms, wiec wypada w oknie
// ZAWSZE.  Zachowanie przestaje byc losowe - to jest poprawa - ale przy
// Alt+strzalka czytnik bedzie teraz konsekwentnie czytal wiersz, zamiast
// czasem zdania.  Slyszy to tylko uzytkownik, wiec zostawiam furtke:
// wpis CaretMoveDelayMs w sekcji [Options] przywraca czekanie (100
// odtwarza stan sprzed tej zmiany).
if (iCaretMoveDelayMs > 0) System.Threading.Thread.Sleep(iCaretMoveDelayMs);
}
} // Index property

public int Row {
get {
return this.GetLineFromCharIndex(this.Index);
}
set {
int iIndex = this.GetFirstCharIndexFromLine(value);
this.DeselectAll();
this.SelectionStart = iIndex;
}
} // Row property

public int Col {
get {
return this.Index - this.GetFirstCharIndexOfCurrentLine();
}
set {
this.Index = GetFirstCharIndexOfCurrentLine() + value;
}
} // Col property

public int RowStart {
get {
return this.GetFirstCharIndexOfCurrentLine();
}
set {
}
} // RowStart property

public string RowText {
get {
return this.GetRowText(this.Row);
}
set {
}
} // RowText property

public int RowEnd {
get {
return this.RowStart + this.RowText.Length;
}
set {
}
} // RowEnd property

public int Line {
get {
return this.Row + 1;
}
set {
this.Row = value - 1;
}
} // Line property

public int Column {
get {
return this.Col + 1;
}
set {
this.Col = value - 1;
}
} // Column property

public double Percent {
get {
if (this.Text.Length == 0) return 0;
else return Math.Round((double) ((100.0 * this.Index) / this.Text.Length), 1);
}
set {
int iIndex = (int) ((this.Text.Length * value) / 100.0);
this.DeselectAll();
this.SelectionStart = iIndex;
}
} // Percent property

public void SetRowAndCol(int iRow, int iCol) {
int iRowStart = this.GetFirstCharIndexFromLine(iRow);
int iIndex = iRowStart + iCol;
this.DeselectAll();
this.SelectionStart = iIndex;
} // SetRowAndCol method

public void SetLineAndColumn(int iLine, int iColumn) {
int iRow = iLine - 1;
int iCol = iColumn - 1;
this.SetRowAndCol(iRow, iCol);
} // SetLineAndColumn method

public string GetRange(int iStart, int iEnd) {
int iLength = iEnd - iStart ;
string sText = this.Text;
return sText.Substring(iStart, iLength);
} // GetRange method

public void ReplaceRange(int iStart, int iEnd, string sText) {
this.DeselectAll();
this.Select(iStart, iEnd - iStart);
this.SelectedText = sText;
this.Index = iStart + sText.Length;
} // ReplaceRange method

public void SelectRange(int iStart, int iEnd) {
this.DeselectAll();
this.Select(iStart, iEnd - iStart);
} // SelectRange method

private int iStartSelection;
public int StartSelection {
get {
return iStartSelection;
}
set {
iStartSelection = value;
}
} // StartSelection property

private int iBookmark;
public int Bookmark {
get {
return iBookmark;
}
set {
iBookmark = value;
}
} // Bookmark property

private string sFindText;
public string FindText {
get {
return sFindText;
}
set {
sFindText = value;
}
} // FindText property

private string sMatchText;
public string MatchText {
get {
return sMatchText;
}
set {
sMatchText = value;
}
} // MatchText property

private string sReplaceText;
public string ReplaceText {
get {
return sReplaceText;
}
set {
sReplaceText = value;
}
} // ReplaceText property

private string sPatternText;
public string PatternText {
get {
return sPatternText;
}
set {
sPatternText = value;
}
} // PatternText property

private string sSubstituteText;
public string SubstituteText {
get {
return sSubstituteText;
}
set {
sSubstituteText = value;
}
} // SubstituteText property

private string sJumpLine;
public string JumpLine {
get {
return sJumpLine;
}
set {
sJumpLine = value;
}
} // JumpLine property

private string sGoPercent;
public string GoPercent {
get {
return sGoPercent;
}
set {
sGoPercent = value;
}
} // GoPercent property

private string sSearchTopic;
public string SearchTopic {
get {
return sSearchTopic;
}
set {
sSearchTopic = value;
}
} // SearchTopic property

private int iOldSelectionStart;
public int OldSelectionStart {
get {
return iOldSelectionStart;
}
set {
iOldSelectionStart = value;
}
} // OldSelectionStart property

private int iOldSelectionLength;
public int OldSelectionLength {
get {
return iOldSelectionLength;
}
set {
iOldSelectionLength = value;
}
} // OldSelectionLength property

public void StoreSelection() {
this.OldSelectionStart = this.SelectionStart;
this.OldSelectionLength = this.SelectionLength;
this.DeselectAll();
this.Index = this.OldSelectionStart + this.OldSelectionLength;
} // StoreSelection method

public void Reselect() {
this.DeselectAll();
this.Select(this.OldSelectionStart, this.OldSelectionLength);
} // Reselect method

public bool IsBottomRow {
get {
int iIndex = GetFirstCharIndexFromLine(this.Row + 1);
return iIndex < 0;
}
set {
}
} // IsBottomRow property

public int BottomRow {
get {
return this.Text.Split('\n').Length - 1;
}
set {
}
} // BottomRow property

public int RowLength {
get {
int iRow = this.Row;
int iStart = GetFirstCharIndexFromLine(iRow);
int iEnd = GetFirstCharIndexFromLine(iRow + 1);
//if (iEnd <= 0) iEnd = iStart;
if (iEnd <= 0) iEnd = this.TextLength;
;
int iLength = iEnd - iStart;
/*
int iLength = this.Lines[this.Row].Length;
if (!this.IsBottomRow) iLength++;
*/
return iLength;
}
set {
}
} // RowLength property

public HomerRichTextBox() {
// KARETKA I ZAZNACZENIE ZOSTAJA WIDOCZNE, GDY OKNO TRACI FOKUS
// (zadanie 5, 12.09.2026 - zgloszenie: "po Alt+Tab kursor jakby zamarza,
// czasem przy wyszukiwaniu, Esc pomaga").
//
// RichTextBox ma HideSelection DOMYSLNIE WLACZONE - ZMIERZONE, nie
// zalozone: swieza kontrolka zwraca True (testy/pomiar_kursor.cs).
// EdSharp nie ustawial tego nigdzie, wiec dzialal z ta wartoscia, a to
// znaczy, ze przy KAZDYM odejsciu fokusu kontrolka ukrywala zaznaczenie
// i karetke.  Odejsc fokusu jest wiecej, niz sie wydaje: Alt+Tab, ale
// takze otwarcie okna wyszukiwania - jest modalne, wiec zabiera fokus.
// Stad "czasem przy wyszukiwaniu".
//
// Samo polozenie kursora PRZEZYWA utrate fokusu (zmierzone: start i
// dlugosc wracaja te same).  Znika WIDOCZNOSC - a to wlasnie widoczna
// karetka jest tym, czego szuka czytnik ekranu i czym jest dla
// uzytkownika "kursor".  Dlatego objawem bylo "zamarza", a nie "skacze":
// nic sie nie psulo w polozeniu, tylko przestawalo byc pokazywane.
//
// Escape "pomagal" z tego samego powodu i nie byl rozwiazaniem: zamykal
// okno wyszukiwania, czyli oddawal fokus kontrolce, a wtedy zaznaczenie
// znow stawalo sie widoczne.  Poprawka usuwa potrzebe tej sztuczki.
this.HideSelection = false;
// Czekanie po ruchu kursora - domyslnie ZERO (patrz setter Index, gdzie
// stoi pomiar).  Czytane raz, przy tworzeniu kontrolki, bo setter Index
// chodzi tysiace razy i siegania do pliku ini przy kazdym ruchu kursora
// bylo by tym samym bledem, ktory tu naprawiam.
string sOpoznienie = App.ReadOption("CaretMoveDelayMs", "0").Trim();
if (!Int32.TryParse(sOpoznienie, out iCaretMoveDelayMs) || iCaretMoveDelayMs < 0) iCaretMoveDelayMs = 0;
SectionBreak = App.ReadOption("SectionBreak", SectionBreak);
string s = App.ReadOption("UseIndentModeDefault", "N").Trim().ToUpper();
if (s == "Y" || s == "YES") this.IndentMode = true;
Ini.WriteValue(App.IniFile, "Data", "IndentMode", (this.IndentMode ? "1" : "0"), false);
} // HomerRichTextBox constructor

public int GetIndexRow(int iIndex) {
return this.GetLineFromCharIndex(iIndex);
} // GetIndexRow method

public int GetRowStart(int iRow) {
return this.GetFirstCharIndexFromLine(iRow);
} // GetRowStart method

public string GetRowText(int iRow) {
int iStart = this.GetFirstCharIndexFromLine(iRow);
int iEnd = this.GetFirstCharIndexFromLine(iRow + 1);
// Dialog.Show("iStart " + iStart, "iEnd " + iEnd);
if (iEnd == -1) iEnd = this.Text.Length;
else iEnd --;
return this.GetRange(iStart, iEnd);
} // GetRowText method

public bool SetWrap(bool bWrap) {
bool bOldWrap = this.WordWrap;
bool bModified = this.Modified;
this.WordWrap = bWrap;
this.Modified = bModified;
return bOldWrap;
} // SetWrap method

public bool SetGuard(bool bGuard) {
bool bOldGuard = this.ReadOnly;
bool bModified = this.Modified;
this.ReadOnly = bGuard;
this.Modified = bModified;
return bOldGuard;
} // SetGuard method

protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
return App.Frame.ProcessCmdKey_Helper(ref msg, keyData);
} // ProcessCmdKey handler

} // HomerRichTextBox class

public class ListForm : Form {

public ListBox lst;
public DataTable tbl;
public BindingSource bs ;
public string Filter;
public DataTable tblDefault = null;
public int CheckFirst = -1;
public int CheckLast = -1;

protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
ListBox lst = this.lst;
bool bChecked = false;
if (lst is CheckedListBox) bChecked = true;

// ALT+F4 ZAMYKA TO OKNO, NIE CALY PROGRAM (Kasperczak, 11.09.2026).
// Ta klasa jest NIEZALEZNA od LbcForm, wiec ta sama poprawka musi byc
// w obu - inaczej czesc list zamykalaby program, a czesc nie, i nikt by
// nie wiedzial, ktora jest ktora.  Cancel, czyli to samo co Escape.
if (keyData == (Keys.Alt | Keys.F4)) {
this.DialogResult = DialogResult.Cancel;
this.Close();
return true;
}

switch (keyData) {
case Keys.Alt | Keys.A :
App.Frame.AddMessage("Alpha order");
bs.Sort = "Item asc";
bs.Position = 0;
return true;
case Keys.Alt | Keys.Shift | Keys.A :
App.Frame.AddMessage("Reverse alpha order");
bs.Sort = "Item desc";
bs.Position = 0;
return true;
case Keys.Alt | Keys.D :
App.Frame.AddMessage("Default order");
if (this.tblDefault == null) {
this.tblDefault = new DataTable();
this.tblDefault.Columns.Add("Item", typeof(string));
this.tblDefault.Columns.Add("Value", typeof(string));
for (int i = 0; i < tbl.Rows.Count; i++)  this.tblDefault.Rows.Add(tbl.Rows[i][0].ToString(), tbl.Rows[i][1].ToString());
}

tbl = this.tblDefault;
bs.Sort = "";
bs.Position = 0;
return true;
case Keys.Alt | Keys.Shift | Keys.D :
App.Frame.AddMessage("Reverse default order");
if (this.tblDefault == null) {
this.tblDefault = new DataTable();
this.tblDefault.Columns.Add("Item", typeof(string));
this.tblDefault.Columns.Add("Value", typeof(string));
for (int i = 0; i < tbl.Rows.Count; i++)  this.tblDefault.Rows.Add(tbl.Rows[i][0].ToString(), tbl.Rows[i][1].ToString());
}

DataTable tblNew = new DataTable();
tblNew.Columns.Add("Item", typeof(string));
tblNew.Columns.Add("Value", typeof(string));
for (int i = this.tblDefault.Rows.Count -1; i >= 0; i--) tblNew.Rows.Add(this.tblDefault.Rows[i][0].ToString(), tblDefault.Rows[i][1].ToString());
tbl = tblNew;
//bs = new BindingSource();
bs.DataSource = tbl;
//this.BS = bs;
//bs.ResetBindings();
bs.Sort = "";
bs.Position = 0;
return true;
case Keys.Alt | Keys.Delete :
App.Frame.AddMessage((bs.Position + 1) + " of " + tbl.DefaultView.Count);
return true;
case Keys.Shift | Keys.Space :
if (bChecked) {
int iChecked = ((CheckedListBox) lst).CheckedItems.Count;
if (iChecked == 0) App.Frame.AddMessage("No items checked!");
else App.Frame.AddMessage("Checked" + iChecked);
List<int> listChecked = new List<int>();
foreach (int i in ((CheckedListBox) lst).CheckedIndices) listChecked.Add(i);
listChecked.Sort();
foreach (int i in listChecked) App.Frame.AddMessage(tbl.DefaultView[i][0].ToString());
}
else {
App.Frame.AddMessage("Selected");
foreach (int i in lst.SelectedIndices) App.Frame.AddMessage(tbl.DefaultView[i][0].ToString());
}
return true;
case Keys.Space :
//if (!bChecked || this.ActiveControl is Button) return base.ProcessCmdKey (ref msg, keyData);
if (!bChecked || !(this.ActiveControl is ListBox)) return base.ProcessCmdKey (ref msg, keyData);

{
int i = bs.Position;
bool b = ((CheckedListBox) lst).GetItemChecked(i);
((CheckedListBox) lst).SetItemChecked(i, !b);
return true;
}
case Keys.Control | Keys.Home :
if (!bChecked || !(this.ActiveControl is ListBox)) return base.ProcessCmdKey (ref msg, keyData);

int iStart = -1;
for (int i = 0; i < tbl.DefaultView.Count; i++) {
if (((CheckedListBox) lst).GetItemChecked(i)) {
iStart = i;
break;
}
}

if (iStart >= 0) bs.Position = iStart;
else App.Frame.AddMessage("Not found!");
return true;
case Keys.Control | Keys.End :
if (!bChecked || !(this.ActiveControl is ListBox)) return base.ProcessCmdKey (ref msg, keyData);

int iEnd = -1;
for (int i = tbl.DefaultView.Count - 1; i >= 0; i--) {
if (((CheckedListBox) lst).GetItemChecked(i)) {
iEnd = i;
break;
}
}

if (iEnd >= 0) bs.Position = iEnd;
else App.Frame.AddMessage("Not found!");
return true;
case Keys.Control | Keys.Down :
if (!bChecked || !(this.ActiveControl is ListBox)) return base.ProcessCmdKey (ref msg, keyData);

int iNext = -1;
for (int i = bs.Position + 1; i < tbl.DefaultView.Count; i++) {
if (((CheckedListBox) lst).GetItemChecked(i)) {
iNext = i;
break;
}
}

if (iNext >= 0) bs.Position = iNext;
else App.Frame.AddMessage("Not found!");
return true;
case Keys.F8 :
case Keys.Shift | Keys.F8 :
case Keys.Alt | Keys.Shift | Keys.F8 :
case Keys.Shift | Keys.Clear :
case Keys.Alt | Keys.Shift | Keys.Clear :
case Keys.Shift | Keys.Down :
case Keys.Alt | Keys.Shift | Keys.Down :
case Keys.Shift | Keys.Up :
case Keys.Alt | Keys.Shift | Keys.Up :
case Keys.Shift | Keys.End :
case Keys.Alt | Keys.Shift | Keys.End :
case Keys.Shift | Keys.Home :
case Keys.Alt | Keys.Shift | Keys.Home :
if (!bChecked || !(this.ActiveControl is ListBox)) return base.ProcessCmdKey (ref msg, keyData);

bool bState;
int iFirst, iLast;
int iAfter = bs.Position;
string sKey = Util.Key2String(keyData);

if (keyData == Keys.F8) {
App.Frame.AddMessage("Start Check or Uncheck");
this.CheckFirst = iAfter;
return true;
}
else if (keyData == (Keys.Shift | Keys.F8)) {
App.Frame.AddMessage("Complete Check");
bState = true;
iFirst = this.CheckFirst;
iLast = iAfter;
}
else if (keyData == (Keys.Alt | Keys.Shift | Keys.F8)) {
App.Frame.AddMessage("Complete Uncheck");
bState = false;
iFirst = this.CheckFirst;
iLast = iAfter;
}
else {
if (sKey.IndexOf("Alt+") >= 0) bState = false;
else bState = true;

if (sKey.IndexOf("+End") >= 0) {
iLast = tbl.DefaultView.Count - 1;
iAfter = iLast;
}
else iLast = iAfter;

if (sKey.IndexOf("+Home") >= 0) {
iFirst = 0;
iAfter = iFirst;
}
else iFirst = iAfter;

if (sKey.IndexOf("+Up") >= 0) iAfter--;
if (sKey.IndexOf("+Down") >= 0) iAfter++;

}

if (iFirst > iLast) Util.Swap(ref iFirst, ref iLast);
for (int iPosition = iFirst; iPosition <= iLast; iPosition ++) ((CheckedListBox) lst).SetItemChecked(iPosition, bState);
if (iAfter != bs.Position && iAfter >=0 && iAfter < tbl.DefaultView.Count) bs.Position = iAfter;
return true;
case Keys.Control | Keys.Up :
if (!bChecked || !(this.ActiveControl is ListBox)) return base.ProcessCmdKey (ref msg, keyData);

int iPrevious = -1;
for (int i = bs.Position - 1; i >= 0; i--) {
if (((CheckedListBox) lst).GetItemChecked(i)) {
iPrevious = i;
break;
}
}

if (iPrevious >= 0) bs.Position = iPrevious;
else App.Frame.AddMessage("Not found!");
return true;
case Keys.Control | Keys.A :
if (!bChecked || !(this.ActiveControl is ListBox)) return base.ProcessCmdKey (ref msg, keyData);

if (bChecked) {
App.Frame.AddMessage("Check All");
for (int i = 0; i < tbl.DefaultView.Count; i++) ((CheckedListBox) lst).SetItemChecked(i, true);
}
return true;
case Keys.Control | Keys.Shift | Keys.A :
if (!bChecked || !(this.ActiveControl is ListBox)) return base.ProcessCmdKey (ref msg, keyData);

if (bChecked) {
App.Frame.AddMessage("Uncheck All");
for (int i = 0; i < tbl.DefaultView.Count; i++) ((CheckedListBox) lst).SetItemChecked(i, false);
}
return true;
case Keys.Control | Keys.F :
case Keys.Control | Keys.Shift | Keys.F :
string sFilterSql = "";
string sFilter = "";
if (keyData == (Keys.Control | Keys.Shift | Keys.F)) App.Frame.AddMessage("Clear filter");
else {
Dialog.hashFilter.TryGetValue(this.Text, out sFilter);
sFilter = Dialog.Input("Filter", "Text", sFilter);
if (sFilter.Length == 0) return true;
//sFilterSql = "Item like '" + sFilter + "'";
sFilterSql = GetFilterSql(sFilter);
}

string sTemp = bs.Filter;
try {
bs.Filter = sFilterSql;
this.Filter = sFilter;
}
catch (Exception ex) {
Dialog.Show("Error", ex.Message);
bs.Filter = sTemp;
return true;
}

bs.Position = 0;
App.Frame.AddMessage(Util.Pluralize(bs.Count, "item"));

//if (keyData == (Keys.Control | Keys.F)) {
if (Dialog.hashFilter.ContainsKey(this.Text)) Dialog.hashFilter.Remove(this.Text);
if (sFilter.Trim().Length > 0) Dialog.hashFilter.Add(this.Text, sFilter);
//}
return true;
case Keys.Control | Keys.J :
case Keys.Alt | Keys.J :
string sTitle = this.Text;
string sJump = "";
Dialog.hashJump.TryGetValue(sTitle, out sJump);
if (keyData == (Keys.Control | Keys.J)) {
sJump = Dialog.Input("Jump", "Text", sJump);
if (sJump.Length == 0) return true;
}

int iIndex = bs.Position;
if (keyData == (Keys.Alt | Keys.J) || sJump == Dialog.Jump) iIndex++;
else iIndex = 0;
if (Dialog.hashJump.ContainsKey(sTitle)) Dialog.hashJump.Remove(sTitle);
Dialog.hashJump.Add(sTitle, sJump);

int iCount = tbl.DefaultView.Count;
//while (iIndex < iCount && tbl.DefaultView[iIndex].ToString().ToLower().IndexOf(sJump) == -1) iIndex ++;
/*
while (iIndex < iCount && tbl.DefaultView[iIndex].ToString().ToLower().IndexOf(sJump) == -1) {
//App.Frame.AddMessage(iIndex);
iIndex ++;
}
*/
while (iIndex < iCount && tbl.DefaultView[iIndex][0].ToString().ToLower().IndexOf(sJump) == -1) {
iIndex ++;
}
//if (iIndex < iCount) bs.Position = iIndex;
if (iIndex < iCount) bs.Position = iIndex;
else App.Frame.AddMessage("Not found!");
//lst.Update();
return true;
}

return base.ProcessCmdKey (ref msg, keyData);
} // ProcessCmdKey handler

public string GetFilterSql(string sText) {
if (sText == null) sText = "";
sText = sText.Trim();
if (sText == "" || sText == "*") return "";
string[] aFilters = sText.Split('|');
string s = "";
for (int i =0; i < aFilters.Length; i++) {
if (i == 0) s += "(";
string[] a = aFilters[i].Split('*');
for (int j = 0; j < a.Length; j++) {
string sPrefix = "";
string sSuffix = "";
if (j == 0) s += " (";
if (a[j].Length > 0) {
if (j > 0) sPrefix = "*";
if (j < a.Length - 1) sSuffix = "*";
s += "Item like '" + sPrefix + a[j] + sSuffix + "'";
}

if (j == a.Length - 1) s += ") ";
else s += " and ";
}
if (i == aFilters.Length - 1) s+=")";
else s += " or ";
}

s = s.Replace("( and ", "(");
s = s.Replace(" and )", ")");
s = s.Replace("**", "*");
s = s.Replace("  ", " ");
s = s.Replace("( ", "(");
s = s.Replace(" )", ")");
s = s.Trim();
return s;
} // GetFilterSql method

} // ListForm class

public class Dialog {
public static string Jump = "";
public static Dictionary<string, string> hashItem = new Dictionary<string, string>();
public static Dictionary<string, string> hashFilter = new Dictionary<string, string>();
public static Dictionary<string, string> hashSort = new Dictionary<string, string>();
public static Dictionary<string, string> hashJump = new Dictionary<string, string>();

public static int PickEncoding(string sTitle, int iDefault) {
EncodingInfo[] eis = Encoding.GetEncodings();
int iCount = eis.Length;
string[] aNames = new string[iCount];
int[] aCodes = new int[iCount];
for (int i = 0; i < iCount; i++) {
EncodingInfo ei = eis[i];
Encoding en = ei.GetEncoding();
aNames[i] = en.EncodingName + " = " + en.CodePage;
aCodes[i] = en.CodePage;
}

Array.Sort(aNames, aCodes);
int iPosition = Array.IndexOf(aCodes, Encoding.Default.CodePage);
if (iPosition == -1) iPosition = 0;

if (sTitle.Length == 0) sTitle = "Pick Encoding";
string sItem = "";
if (hashItem.TryGetValue(sTitle, out sItem)) iPosition = 0;

string sName = Dialog.Pick(sTitle, aNames, false, iPosition);
if (sName.Length == 0) return -1;

iPosition = Array.IndexOf(aNames, sName);
int iCodePage = aCodes[iPosition];
return iCodePage;
} // PickEncoding method

public static string OpenFile(string sTitle, string sPath) {
string sReturn = "";
string sDir;

OpenFileDialog dlg = new OpenFileDialog();
if (sTitle.Length > 0) dlg.Title = sTitle;
if (File.Exists(sPath)) {
dlg.FileName = sPath;
sDir = Path.GetDirectoryName(sPath);
}
else sDir = sPath;

if (!Directory.Exists(sDir)) sDir = Directory.GetCurrentDirectory();
dlg.InitialDirectory = sDir;

string sFilter = "All files (*.*)|*.*|Text files (*.txt)|*.txt|Rich Text Format files (*.rtf)|*.rtf";
string sCompiler = App.ReadData("Compiler", "Default");
string sExtensionDefault = App.ReadOption("ExtensionDefault", "");
if (sCompiler != "Default") sFilter = sCompiler + " files (*." + sExtensionDefault + ")|*." + sExtensionDefault + "|" + sFilter;
dlg.Filter = sFilter;
dlg.FilterIndex = 1;
dlg.ValidateNames = true;
dlg.CheckPathExists = true;

if (dlg.ShowDialog() == DialogResult.OK) sReturn = dlg.FileName;
dlg.Dispose();
return sReturn;
} // OpenFile method

public static string SaveFile(string sTitle, string sPath) {
string sReturn = "";
string sDir;

SaveFileDialog dlg = new SaveFileDialog();
if (sTitle.Length > 0) dlg.Title = sTitle;
if (Directory.Exists(sPath)) sDir = sPath;
else {
dlg.FileName = sPath;
sDir = Path.GetDirectoryName(sPath);
}

if (Directory.Exists(sDir)) dlg.InitialDirectory = sDir;

string sFilter = "All files (*.*)|*.*|Text files (*.txt)|*.txt|Rich Text Format files (*.rtf)|*.rtf";
string sCompiler = App.ReadData("Compiler", "Default");
string sExtensionDefault = App.ReadOption("ExtensionDefault", "");
if (sCompiler != "Default") sFilter = sCompiler + " files (*." + sExtensionDefault + ")|*." + sExtensionDefault + "|" + sFilter;
dlg.Filter = sFilter;
dlg.FilterIndex = 1;
dlg.CheckPathExists = true;
dlg.SupportMultiDottedExtensions = true;

dlg.CreatePrompt = false;
dlg.ValidateNames = true;
dlg.AddExtension = true;
//dlg.AddExtension = false;
//dlg.DefaultExt = "txt";
dlg.DefaultExt = App.ReadOption("ExtensionDefault", "");

if (dlg.ShowDialog() == DialogResult.OK) sReturn = dlg.FileName;
dlg.Dispose();
return sReturn;
} // SaveFile method

public static string OldInput(string sTitle, string sLabel, string sValue) {
return Input(sTitle, sLabel, sValue);
} // Input method

public static string Input(string sTitle, string sLabel, string sValue) {
string[] aLabel = new string[] {sLabel};
string[] aValue = new string[] {sValue};
string[] aReturn = MultiInput(sTitle, aLabel, aValue);
//string sReturn = aReturn[0];
string sReturn = "";
if (aReturn != null && aReturn.Length > 0) sReturn = aReturn[0];
return sReturn;
} // Input method

public static string[] MultiInput(string sTitle, string[] aLabel, string[] aValue) {
LbcDialog dlg = new LbcDialog(sTitle, App.Frame);
List<TextBox> lBoxes = new List<TextBox>();
for (int i = 0; i < aLabel.Length; i++) {
string sVal = (aValue != null && i < aValue.Length && aValue[i] != null) ? aValue[i] : "";
lBoxes.Add(dlg.addInputBox(aLabel[i], sVal));
}
List<string> lReturn = new List<string>();
if (dlg.runOkCancel()) foreach (TextBox txt in lBoxes) lReturn.Add(txt.Text);
dlg.Dispose();
return lReturn.ToArray();
} // MultiInput method

public static string Pick(string sTitle, string[] aValue, bool bSort) {
string[] aDisplay = null;
int iIndex = 0;
return Pick(sTitle, aValue, aDisplay, bSort, iIndex);
} // Pick method

public static string[] MultiPick(string sTitle, string[] aValues, int[] aSelect, bool bSort) {
List<string> lSelectedValues = new List<string>();
if (aSelect != null) foreach (int i in aSelect) if (i >= 0 && i < aValues.Length) lSelectedValues.Add(aValues[i]);
string[] aItems = (string[]) aValues.Clone();
if (bSort) Array.Sort(aItems, new CaseInsensitiveComparer());
List<int> lChecked = new List<int>();
foreach (string sVal in lSelectedValues) { int idx = Array.IndexOf(aItems, sVal); if (idx >= 0) lChecked.Add(idx); }
List<string> lNames = new List<string>(aItems);
LbcDialog dlg = new LbcDialog(sTitle, App.Frame);
CheckedListBox clb = dlg.addCheckListBox(lNames, lChecked, "");
List<string> lReturn = new List<string>();
if (dlg.runOkCancel()) foreach (int i in clb.CheckedIndices) lReturn.Add(aItems[i]);
dlg.Dispose();
return lReturn.ToArray();
} // MultiPick method

public static string[] MultiCheck(string sTitle, string[] aValues, int[] aSelect, bool bSort, int iIndex) {
string[] aDisplay = null;
return MultiCheck(sTitle, aDisplay, aValues, aSelect, bSort, iIndex);
} // MultiCheck method

public static string[] MultiCheck(string sTitle, string[] aDisplay, string[] aValues, int[] aSelect, bool bSort, int iIndex) {
string[] aVal = (string[]) aValues.Clone();
string[] aDisp = (aDisplay == null) ? (string[]) aVal.Clone() : (string[]) aDisplay.Clone();
if (bSort) {
if (aDisplay == null) { Array.Sort(aVal, new CaseInsensitiveComparer()); aDisp = (string[]) aVal.Clone(); }
else Array.Sort(aDisp, aVal);
}
List<int> lChecked = new List<int>();
if (aSelect != null) foreach (int i in aSelect) if (i >= 0 && i < aValues.Length) { int idx = Array.IndexOf(aVal, aValues[i]); if (idx >= 0) lChecked.Add(idx); }
List<string> lNames = new List<string>(aDisp);
LbcDialog dlg = new LbcDialog(sTitle, App.Frame);
CheckedListBox clb = dlg.addCheckListBox(lNames, lChecked, "");
List<string> lReturn = new List<string>();
if (dlg.runOkCancel()) foreach (int i in clb.CheckedIndices) if (i >= 0 && i < aVal.Length) lReturn.Add(aVal[i]);
dlg.Dispose();
return lReturn.ToArray();
} // MultiCheck method

public static string Pick(string sTitle, string[] aValue, bool bSort, int iIndex) {
string[] aDisplay = null;
return Pick(sTitle, aValue, aDisplay, bSort, iIndex);
} // Pick method

public static string Pick(string sTitle, string[] aValue, string[] aDisplay, bool bSort, int iIndex) {
string[] aVal = (string[]) aValue.Clone();
string[] aDisp = (aDisplay == null) ? (string[]) aVal.Clone() : (string[]) aDisplay.Clone();
if (bSort) {
if (aDisplay == null) { Array.Sort(aVal, new CaseInsensitiveComparer()); aDisp = (string[]) aVal.Clone(); }
else Array.Sort(aDisp, aVal);
}
List<string> lNames = new List<string>(aDisp);
LbcDialog dlg = new LbcDialog(sTitle, App.Frame);
ListBox lst = dlg.addListBox(lNames, "", "");
LbcDialog.selectOnly(lst, iIndex);
string sReturn = "";
if (dlg.runOkCancel()) {
int i = lst.SelectedIndex;
if (i >= 0 && i < aVal.Length) sReturn = aVal[i];
}
dlg.Dispose();
return sReturn;
} // Pick method

// PickBookmark: the Bookmarks list opened by Go to Bookmark (Alt+K),
// specialized so a bookmark can be dropped without leaving the dialog:
//   Delete / Backspace - remove the highlighted bookmark
// Requested by Michal Kasperczak (13.08.2026). Removal rewrites the
// bookmark list stored for sFile in the INI Favorites section - the same
// store Clear Bookmark (Control+Shift+K) edits - so the change is
// permanent as soon as the key is pressed. After a removal the highlight
// stays at the same list position (which is now the following bookmark);
// on the last item it moves up. Emptying the list closes the dialog.
// aValue holds the character indexes (possibly space-padded, as Pick
// does), aDisplay the line texts.
// PickCommand: okno palety polecen.  Pole filtra na gorze, lista wynikow
// pod nim; zwraca INDEKS w przekazanej liscie (nie tekst), bo wywolujacy
// trzyma obok rownolegla liste pozycji menu do klikniecia.  -1 = rezygnacja.
//
// Wzorowane na palecie AMC (CommandPaletteSearch), o ktora sam prosil:
// filtr slowo-po-slowie i zwijanie ogonkow, patrz FoldCommandSearch nizej.
// Pod czytnik istotne sa trzy rzeczy, ktorych brak w zwyklym Pick:
//   1. Fokus startuje w POLU FILTRA - NVDA czyta wpisywane znaki sam,
//      wiec nie trzeba nic wymuszac przy pisaniu.
//   2. Po kazdej zmianie filtra mowimy LICZBE wynikow i PIERWSZY z nich,
//      inaczej niewidomy pisze w prozni: lista sie zmienia bezglosnie, bo
//      fokus zostal w polu.  Mowa wymuszona, bo fokus sie nie przesunal.
//   3. Enter W POLU wybiera pierwszy wynik - najczestszy przypadek jest
//      wtedy dwuruchowy (pisz, Enter), bez schodzenia do listy.
// Strzalka w dol z pola wchodzi do listy, gdzie Enter wybiera podswietlone.
public static int PickCommand(string sTitle, IList<string> lLabels) {
List<string> lAll = new List<string>(lLabels);
List<int> lMap = new List<int>();      // pozycja na liscie -> indeks w lAll
for (int i = 0; i < lAll.Count; i++) lMap.Add(i);

LbcDialog dlg = new LbcDialog(sTitle, App.Frame);
dlg.addLabel("Type to filter commands:");
TextBox tbFilter = dlg.addTextBox("", "");
dlg.addLabel("Commands:");
ListBox lst = dlg.addListBox(lAll, "", "");
lst.SelectionMode = SelectionMode.One;
dlg.setHelpDetail(tbFilter, "Type words in any order to narrow the list; matching ignores case and Polish diacritics. Enter runs the first match, Down Arrow moves into the list, Escape closes the palette.");
dlg.setHelpDetail(lst, "Enter runs the highlighted command, Escape closes the palette, Shift+Tab returns to the filter box.");
LbcDialog.selectOnly(lst, 0);
dlg.setInitialFocus(tbFilter);

tbFilter.TextChanged += delegate(object oSender, EventArgs ev) {
string sQuery = tbFilter.Text;
lMap.Clear();
lst.BeginUpdate();
lst.Items.Clear();
for (int i = 0; i < lAll.Count; i++) {
if (!CommandMatches(lAll[i], sQuery)) continue;
lMap.Add(i);
lst.Items.Add(lAll[i]);
}
lst.EndUpdate();
if (lst.Items.Count > 0) LbcDialog.selectOnly(lst, 0);
// Licznik i pierwszy wynik - jedyny sygnal, ze pisanie cokolwiek dalo.
if (lst.Items.Count == 0) Say.sayForced("No matching command");
else Say.sayForced(lst.Items.Count + " commands, " + lAll[lMap[0]]);
};

int iResult = -1;
tbFilter.KeyDown += delegate(object oSender, KeyEventArgs ev) {
if (ev.KeyData == Keys.Down) {
// Wejscie do listy.  Focus() sam wystarczy - czytnik oglosi pozycje.
if (lst.Items.Count == 0) { ev.Handled = true; ev.SuppressKeyPress = true; return; }
ev.Handled = true; ev.SuppressKeyPress = true;
lst.Focus();
return;
}
if (ev.KeyData == Keys.Enter) {
// Enter w polu = uruchom pierwszy wynik, bez schodzenia do listy.
ev.Handled = true; ev.SuppressKeyPress = true;
if (lMap.Count == 0) { Say.sayForced("No matching command"); return; }
iResult = lMap[0];
dlg.form.DialogResult = DialogResult.OK;
dlg.form.Close();
}
};

lst.KeyDown += delegate(object oSender, KeyEventArgs ev) {
if (ev.KeyData != Keys.Enter) return;
ev.Handled = true; ev.SuppressKeyPress = true;
int i = lst.SelectedIndex;
if (i < 0 || i >= lMap.Count) return;
iResult = lMap[i];
dlg.form.DialogResult = DialogResult.OK;
dlg.form.Close();
};

if (dlg.runOkCancel() && iResult < 0) {
// Wyjscie przyciskiem OK, bez Entera na pozycji.
int i = lst.SelectedIndex;
if (i >= 0 && i < lMap.Count) iResult = lMap[i];
}
dlg.Dispose();
return iResult;
} // PickCommand method

// CommandMatches: pozycja pasuje, gdy zawiera WSZYSTKIE slowa zapytania,
// w dowolnej kolejnosci.  Kolejnosc slow to rzecz, ktorej nikt nie pamieta,
// a przy "zapisz jako" i "Save As" i tak sie rozjezdza miedzy jezykami.
private static bool CommandMatches(string sLabel, string sQuery) {
string[] aTokens = FoldCommandSearch(sQuery).Split(
new char[] {' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
if (aTokens.Length == 0) return true;
string sFolded = FoldCommandSearch(sLabel);
foreach (string sToken in aTokens) {
if (sFolded.IndexOf(sToken, StringComparison.Ordinal) < 0) return false;
}
return true;
}

// FoldCommandSearch: wielkie litery i BEZ OGONKOW, zeby "zazn" znalazlo
// "zaznacz", a "lacz" znalazlo "Lacz".  Rozklad Unicode zdejmuje znaki
// diakrytyczne z a-c-e-n-o-s-z-u; kreslone l trzeba podmienic osobno, bo
// nie jest litera z akcentem, tylko wlasnym znakiem.
private static string FoldCommandSearch(string sValue) {
if (sValue == null) return "";
StringBuilder sb = new StringBuilder(sValue.Length);
foreach (char c in sValue.Normalize(NormalizationForm.FormD)) {
if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
if (c == 'ł' || c == 'Ł') { sb.Append('L'); continue; }
sb.Append(Char.ToUpperInvariant(c));
}
return sb.ToString();
}

// PickBookmark: the Bookmarks list opened by Go to Bookmark (Alt+K),
// specialized so a bookmark can be dropped without leaving the dialog:
//   Delete / Backspace - remove the highlighted bookmark
// Requested by Michal Kasperczak (13.08.2026). Removal rewrites the
// bookmark list stored for sFile in the INI Favorites section - the same
// store Clear Bookmark (Control+Shift+K) edits - so the change is
// permanent as soon as the key is pressed. After a removal the highlight
// stays at the same list position (which is now the following bookmark);
// on the last item it moves up. Emptying the list closes the dialog.
// aValue holds the character indexes (possibly space-padded, as Pick
// does), aDisplay the line texts.
public static string PickBookmark(string sTitle, string[] aValue, string[] aDisplay, int iIndex, string sFile) {
List<string> lVal = new List<string>(aValue);
List<string> lDisp = new List<string>((aDisplay == null) ? aValue : aDisplay);

LbcDialog dlg = new LbcDialog(sTitle, App.Frame);
// PASEK STANU PUSTY, PODPOWIEDZ POD F1 (jego zgloszenie 01.09.2026:
// "Niepotrzebnie czyta, ze na liscie zakladek DELETE REMOVE, tego to wiem i
// nie musi tego czytac").  Tekst paska stanu czytnik oglasza w ramach
// otwarcia okna, czyli PRZED nazwa pozycji - a ten czlowiek zna juz swoj
// program.  Odkrywalnosc zostaje: setHelpDetail pokazuje klawisze w pomocy
// F1 i wymawia je na zadanie przez Shift+F1.
ListBox lst = dlg.addListBox(lDisp, "", "");
dlg.setHelpDetail(lst, "Keys: Delete or Backspace removes the bookmark, Left Arrow previews the bookmarked line, Enter goes to the bookmark, Escape closes the list. Removing the last bookmark leaves the list open and empty.");
LbcDialog.selectOnly(lst, iIndex);

lst.KeyDown += delegate(object oSender, KeyEventArgs ev) {
ListBox lb = oSender as ListBox;
if (lb == null) return;

// STRZALKA W LEWO CZYTA TRESC WIERSZA, NIE JEGO NUMER (Kasperczak,
// 11.09.2026: "Strzalka w lewo nie ma czytac numeru linii z zakladka, tylko
// tresc wiersza, w ktorej jest zakladka, ale bez wychodzenia z menu.  Taki
// podglad").  Do 5.0.78 mowila "Line 148" - numer sam nie mowi, gdzie sie
// jest, a tresc mowi.  W 5.0.79 dopisywalem numer na koncu tresci z wlasnej
// inicjatywy - ZLE, sprostowal to od razu (11.09.2026: "on nie ma mowic
// numeru linii tylko ma mowic tresc linii ma czytac ten wiersz po prostu a
// nie jakis numer linii 138").  Sama tresc, nic wiecej: numer jest widoczny
// w samej pozycji listy, a dopowiadanie go przy kazdym podgladzie to halas.
// Pusty wiersz trzeba nazwac, inaczej strzalka w lewo brzmi jak awaria.
// Fokus nie drgnal, wiec mowa MUSI byc wymuszona - czytnik nie ma z czego
// sam wywnioskowac zmiany.  Okno zostaje otwarte: to podglad, nie skok.
if (ev.KeyData == Keys.Left) {
ev.Handled = true; ev.SuppressKeyPress = true;
int iSel = lb.SelectedIndex;
if (iSel < 0 || iSel >= lVal.Count) return;
int iCharAt;
if (!Int32.TryParse(lVal[iSel].Trim(), out iCharAt)) return;
HomerRichTextBox rtbHere = (App.Frame.Child == null) ? null : App.Frame.Child.RTB;
if (rtbHere == null) return;
int iRowHere = rtbHere.GetLineFromCharIndex(iCharAt);
string[] aLinesHere = rtbHere.Lines;
string sRowHere = (iRowHere >= 0 && iRowHere < aLinesHere.Length)
? (aLinesHere[iRowHere] ?? "").Trim() : "";
// "Empty line" tym samym slowem co lista zakladek nazwanych nizej - dwa
// rozne komunikaty na to samo zjawisko brzmialyby jak dwa rozne bledy.
Say.sayForced(sRowHere.Length == 0 ? "Empty line" : sRowHere);
return;
}

if (ev.KeyData != Keys.Delete && ev.KeyData != Keys.Back) return;
int i = lb.SelectedIndex;
ev.Handled = true; ev.SuppressKeyPress = true;
if (i < 0 || i >= lVal.Count) return;

// Drop the bookmark from the INI store. Values may be space-padded
// for aligned display, so match on the trimmed number.
// Sekcja Bookmarks, nie Favorites: zakladki maja wlasny magazyn od 5.0.64.
if (sFile.Length > 0) {
string sStored = App.ReadValue("Bookmarks", sFile, "");
HomerList hl = new HomerList(sStored);
hl.Remove(lVal[i].Trim());
if (hl.Segments.Length == 0) App.DeleteKey("Bookmarks", sFile);
else App.WriteValue("Bookmarks", sFile, hl.Segments);
}

lVal.RemoveAt(i);
lDisp.RemoveAt(i);
lb.Items.RemoveAt(i);
// PUSTA LISTA NIE ZAMYKA JUZ OKNA (jego zgloszenie 02.09.2026 01:41: "jednak
// po usunieciu ostatniej zakladki program powinien mowic, ze brak zakladek i
// wtedy escape'em wychodzimy.  Jak ktos sie rozpedzi, bedzie dlugo naciskal
// delete to sie nie zorientuje, ze po zakladkach bedzie juz w tekscie").
// Samoczynne zamkniecie przenosilo fokus do TEKSTU dokumentu w trakcie serii
// nacisniec Delete, wiec kolejny Delete kasowal znak w dokumencie zamiast
// zakladki - dla niewidomego cicha zmiana kontekstu z niszczacym skutkiem.
// Wyjscie jest teraz zawsze jego decyzja: Escape.
if (lb.Items.Count == 0) {
Say.sayForced("No bookmarks, press Escape to close the list");
return;
}
if (i >= lb.Items.Count) i = lb.Items.Count - 1;
LbcDialog.selectOnly(lb, i);
App.Frame.AddMessage("Bookmark removed");
};

string sReturn = "";
if (dlg.runOkCancel()) {
int i = lst.SelectedIndex;
if (i >= 0 && i < lVal.Count) sReturn = lVal[i];
}
dlg.Dispose();
return sReturn;
} // PickBookmark method

// PickNamedBookmark: lista zakladek Z NAZWA (Alt+Shift+B).  Osobna od
// PickBookmark, bo magazyn jest inny (sekcja NamedBookmarks, klucz na zakladke)
// i pozycja niesie NUMER WIERSZA, nie offset znaku.  Delete albo Backspace
// zdejmuje zakladke od razu i bez wychodzenia z okna, dokladnie jak na liscie
// zakladek zwyklych; oproznienie listy zamyka okno.  Podpowiedz w pasku stanu
// jest KROTKA swiadomie: dlugi tekst czytnik czyta PRZED nazwa pozycji.
public static string PickNamedBookmark(string sTitle, string[] aValue, string[] aDisplay, int iIndex, string sFile) {
List<string> lVal = new List<string>(aValue);
List<string> lDisp = new List<string>((aDisplay == null) ? aValue : aDisplay);

LbcDialog dlg = new LbcDialog(sTitle, App.Frame);
// PASEK STANU PUSTY, KLAWISZE POD F1 - ta sama zmiana i ten sam powod, co na
// liscie zakladek zwyklych (jego zgloszenie 01.09.2026).
ListBox lst = dlg.addListBox(lDisp, "", "");
dlg.setHelpDetail(lst, "Keys: Delete or Backspace removes the named bookmark, Left Arrow reads the line it points to, Enter goes to the bookmark, Escape closes the list. Removing the last bookmark leaves the list open and empty.");
LbcDialog.selectOnly(lst, iIndex);

lst.KeyDown += delegate(object oSender, KeyEventArgs ev) {
ListBox lb = oSender as ListBox;
if (lb == null) return;

// STRZALKA W LEWO CZYTA WIERSZ, Z KTOREGO ZAKLADKA POCHODZI (jego prosba
// 01.09.2026: "moze strzalka w lewo na liscie zakladek mogla by czytac linie
// z ktorej ta zakladka pochodzi").  Pozycja listy niesie NAZWE i numer
// wiersza, wiec brakujaca informacja to TRESC tego wiersza - inaczej niz na
// liscie zakladek zwyklych, gdzie tresc jest widoczna, a brakuje numeru.
if (ev.KeyData == Keys.Left) {
ev.Handled = true; ev.SuppressKeyPress = true;
int iSel = lb.SelectedIndex;
if (iSel < 0 || iSel >= lVal.Count) return;
int iRowRead;
if (!Int32.TryParse(lVal[iSel].Trim(), out iRowRead)) return;
HomerRichTextBox rtbHere = (App.Frame.Child == null) ? null : App.Frame.Child.RTB;
if (rtbHere == null) return;
string[] aLinesHere = rtbHere.Lines;
if (iRowRead < 0 || iRowRead >= aLinesHere.Length) {
Say.sayForced("This line is gone from the document");
return;
}
string sRowRead = (aLinesHere[iRowRead] ?? "").Trim();
Say.sayForced(sRowRead.Length == 0 ? "Empty line" : sRowRead);
return;
}

if (ev.KeyData != Keys.Delete && ev.KeyData != Keys.Back) return;
int i = lb.SelectedIndex;
ev.Handled = true; ev.SuppressKeyPress = true;
if (i < 0 || i >= lVal.Count) return;

int iRowGone;
if (sFile.Length > 0 && Int32.TryParse(lVal[i].Trim(), out iRowGone)) {
App.DeleteKey("NamedBookmarks", MdiFrame.NamedBookmarkKey(sFile, iRowGone));
}

lVal.RemoveAt(i);
lDisp.RemoveAt(i);
lb.Items.RemoveAt(i);
// PUSTA LISTA NIE ZAMYKA OKNA - ta sama zmiana i ten sam powod, co na liscie
// zakladek zwyklych (jego zgloszenie 02.09.2026 01:41).  Zgloszenie dotyczylo
// jednej listy, ale wada jest w OBU: seria nacisniec Delete konczyla sie w
// tekscie dokumentu i nastepny Delete kasowal znak.  Naprawa jednej listy
// zostawilaby drugie wejscie w to samo.
if (lb.Items.Count == 0) {
Say.sayForced("No named bookmarks, press Escape to close the list");
return;
}
if (i >= lb.Items.Count) i = lb.Items.Count - 1;
LbcDialog.selectOnly(lb, i);
App.Frame.AddMessage("Named bookmark removed");
};

string sReturn = "";
if (dlg.runOkCancel()) {
int i = lst.SelectedIndex;
if (i >= 0 && i < lVal.Count) sReturn = lVal[i];
}
dlg.Dispose();
return sReturn;
} // PickNamedBookmark method

// PickFile: a Pick list specialized for file paths (Recent Files,
// List Favorites). Behaves exactly like Pick(value, display, ...) for
// choosing a file, but the list also answers per-item file actions so a
// screen-reader user never has to leave the dialog:
//   Right Arrow    - Open With... (system "Open with" dialog)
//   Left Arrow     - speak the full path of the current item
//   Ctrl+Enter     - show the file in Windows Explorer
//   Ctrl+C         - copy the selected FILES (pasteable in Explorer);
//                    their paths ride along as text in the same clipboard
//   Ctrl+Shift+C   - free, does nothing here (see the key handler for why)
//   Alt+C          - append the full paths to the clipboard as text
//   Delete / Back  - remove EVERY selected entry from the list (and sSection)
//   Shift+Delete   - permanently delete the file from disk (confirmed)
// sSection is the INI section the list is stored in ("Recent" /
// "Favorites"); pass "" to disable the entry-removal actions.
public static string PickFile(string sTitle, string[] aValue, string[] aDisplay, bool bSort, int iIndex, string sSection) {
string[] aVal = (string[]) aValue.Clone();
string[] aDisp = (aDisplay == null) ? (string[]) aVal.Clone() : (string[]) aDisplay.Clone();
if (bSort) {
if (aDisplay == null) { Array.Sort(aVal, new CaseInsensitiveComparer()); aDisp = (string[]) aVal.Clone(); }
else Array.Sort(aDisp, aVal);
}
// Keep a mutable view so entry-removal can drop items live.
List<string> lVal = new List<string>(aVal);
List<string> lDisp = new List<string>(aDisp);

LbcDialog dlg = new LbcDialog(sTitle, App.Frame);
// NO status-bar tip and NO spoken key hint on this list. A screen
// reader speaks the status bar as part of the dialog's opening
// announcement, so anything placed there delays the file name the user
// actually opened the list for. An earlier full key list was cut down to
// "Press F1 for keys", and that shorter hint was dropped as well on
// request -- the list announces the file name and nothing else. The keys
// stay documented in the manual (EdSharp.md).
ListBox lst = dlg.addListBox(lDisp, "", "");
// Mark the list so LbcDialog's default Ctrl+C (copies the display
// name) defers to us - we copy the full path instead.
lst.Tag = "edsharp-filelist";
// LISTA PLIKOW TEZ BIERZE WIELOKROTNY WYBOR (jego zgloszenie 05.09.2026:
// "nie dziala zaznaczanie i kopiowanie wielu Control+C (...) na listach
// Alt+L to na pewno sprawdzilem, to pliki i sciezki").  W 5.0.68 ta jedna
// lista byla z wielokrotnego wyboru WYLACZONA, wiec sprawdzil dokladnie to
// miejsce, w ktorym funkcja nie dzialala.  Wyjatek zdjety, ale klawisze
// NISZCZACE zostaja przy jednej pozycji: Shift+Delete (kasowanie z dysku)
// odmawia przy kilku zaznaczonych, bo tam pomylka kosztuje plik.
LbcDialog.selectOnly(lst, iIndex);

lst.KeyDown += delegate(object oSender, KeyEventArgs ev) {
ListBox lb = oSender as ListBox;
if (lb == null) return;
int i = lb.SelectedIndex;
if (i < 0 || i >= lVal.Count) return;
string sFile = lVal[i];
if (string.IsNullOrEmpty(sFile)) return;

switch (ev.KeyData) {
case Keys.Right:
try { Win32.OpenWith(sFile); App.Frame.AddMessage("Open with"); }
catch (Exception ex) { Dialog.Show("Error", ex.Message); }
ev.Handled = true; ev.SuppressKeyPress = true;
break;

case Keys.Left:
App.Frame.AddMessage(sFile);
ev.Handled = true; ev.SuppressKeyPress = true;
break;

case Keys.Control | Keys.Enter:
try {
string sArg = File.Exists(sFile) ? "/select," + Util.Quote(sFile) : Util.Quote(Path.GetDirectoryName(sFile));
Process.Start("explorer.exe", sArg);
}
catch (Exception ex) { Dialog.Show("Error", ex.Message); }
ev.Handled = true; ev.SuppressKeyPress = true;
break;

// JEDEN KLAWISZ KOPIUJACY, NIE DWA (jego decyzja 11.09.2026: "zostawiamy
// Copied Ctrl+C, a Ctrl+Shift+C na listach plikow zwalniamy, nie robi nic").
//
// Control+C bierze WSZYSTKIE zaznaczone pozycje i kladzie je na schowek W OBU
// FORMATACH NARAZ: jako pliki (CF_HDROP, wkleja sie w Eksploratorze i w Total
// Commanderze) i rownolegle jako sciezki tekstem (wkleja sie w dokumencie).
// O tym, ktory format zostanie uzyty, decyduje MIEJSCE WKLEJENIA, a nie my.
//
// Dlatego osobny skrot "kopiuj sama sciezke" byl fikcja: udawal wybor, ktorego
// w Windows nie ma, a uzytkownik musialby pamietac rozroznienie, ktore system i
// tak ignoruje.  Control+Shift+C kladl do 5.0.73 same NAZWY plikow - tekst,
// ktorego zadna powloka za plik nie uzna - wiec byl po prostu zepsuty.  Zamiast
// dublowac nim Control+C, ZWALNIAMY go: na tych listach nie robi nic i jest
// wolny pod przyszla komende.  Alt+C zostaje i dopisuje sciezki do schowka.
case Keys.Control | Keys.C:
PickFileCopySelection(lb, lVal, lDisp, "files", false);
ev.Handled = true; ev.SuppressKeyPress = true;
break;

case Keys.Alt | Keys.C:
PickFileCopySelection(lb, lVal, lDisp, "text", true);
ev.Handled = true; ev.SuppressKeyPress = true;
break;

// Shift+Delete must be checked before plain Delete/Back, which only
// remove the entry from the list.
//
// DWA KLAWISZE "USUN", DWIE ROZNE STAWKI.  Shift+Delete kasuje plik Z
// DYSKU, wiec przy kilku zaznaczonych pozycjach odmawia i mowi ILE ich
// jest: tam pomylka kosztuje plik, ktorego nie ma jak odzyskac, a milczaca
// odmowa wygladalaby na zepsuty klawisz.  Samo Delete zdejmuje tylko WPIS
// z listy (z sekcji pliku ustawien), plik na dysku zostaje nietkniety -
// to jest odwracalne otwarciem pliku na nowo, wiec bierze WSZYSTKIE
// zaznaczone pozycje naraz (jego zgloszenie 06.09.2026: "nie da sie usunac
// zaznaczonych dwoch").
case Keys.Shift | Keys.Delete:
if (lb.SelectedIndices.Count > 1) App.Frame.AddMessage(lb.SelectedIndices.Count + " items selected; deleting from disk works on one file at a time!");
else PickFileDeleteFromDisk(lb, lVal, lDisp, sSection);
ev.Handled = true; ev.SuppressKeyPress = true;
break;

case Keys.Delete:
case Keys.Back:
PickFileRemoveSelection(lb, lVal, lDisp, sSection);
ev.Handled = true; ev.SuppressKeyPress = true;
break;
}
};

string sReturn = "";
if (dlg.runOkCancel()) {
int i = lst.SelectedIndex;
if (i >= 0 && i < lVal.Count) sReturn = lVal[i];
}
dlg.Dispose();
return sReturn;
} // PickFile method

// PickFileCopySelection: kopiowanie z listy plikow, dla WSZYSTKICH
// zaznaczonych pozycji naraz.
//
// bPaths rozstrzyga, CO idzie do schowka: pelne sciezki (Control+C) albo
// same nazwy plikow (Control+Shift+C).  Sciezka jest tu domysla, bo wiersz
// listy pokazuje wylacznie nazwe - po sciezke sie tu wlasnie siega, a nazwe
// mozna tez odczytac z ekranu.
// bAppend dopisuje do schowka zamiast go zastapic (Alt+C).
//
// Przy sciezkach na schowek idzie TAKZE format plikowy CF_HDROP, wiec
// wklejenie dziala w Eksploratorze, a nie tylko w polu tekstowym.  Szczegoly
// i powod przy Util.SetClipboardFileDrop.
//
// Kolejnosc bierzemy z SelectedIndices, ktore ListBox trzyma posortowane
// rosnaco, wiec skopiowany blok czyta sie tak, jak stoi na liscie,
// niezaleznie od tego, czy zaznaczano z gory w dol czy odwrotnie.
private static void PickFileCopySelection(ListBox lb, List<string> lVal, List<string> lDisp, string sMode, bool bAppend) {
bool bFiles = (sMode == "files");
List<string> lsPicked = new List<string>();
foreach (int iSel in lb.SelectedIndices) {
if (iSel >= 0 && iSel < lVal.Count) lsPicked.Add(lVal[iSel]);
}
if (lsPicked.Count == 0) { App.Frame.AddMessage("No item!"); return; }
string sText = string.Join("\r\n", lsPicked.ToArray());
bool bPaths = bFiles;
// LICZBA POZYCJI W KOMUNIKACIE: bez niej niewidomy nie wie, ile wlasnie
// zabral, bo podswietlenia nie slyszy.
string sHowMany = (lsPicked.Count == 1) ? "" : " " + lsPicked.Count + " items";
string sWhat = "path";
string sWhatMany = "paths";
if (bAppend) {
string sPrior = Util.GetClipboardText();
if (sPrior.Length > 0 && !sPrior.EndsWith("\n")) sPrior += "\r\n";
if (Util.SetClipboardText(sPrior + sText)) App.Frame.AddMessage((lsPicked.Count == 1) ? "Appended " + sWhat : "Appended" + sHowMany);
else App.Frame.AddMessage("Clipboard is busy, nothing appended!");
return;
}
// KOPIOWANIE SCIEZEK KLADZIE NA SCHOWEK TAKZE SAME PLIKI (jego zgloszenie
// 05.09.2026: "kopiowanie wielu elementow dziala, ale pliki nie chca sie
// wklejac").  Eksplorator wkleja plik wylacznie z formatu CF_HDROP, wiec
// samym tekstem ze sciezka nie dalo sie tego zrobic ani przy jednej, ani
// przy wielu pozycjach.  Sciezki widoczne dla powloki musza ISTNIEC na
// dysku: martwy wpis listy plikow (plik przeniesiony albo skasowany) w
// CF_HDROP dalby przy wklejaniu blad powloki, ktorego nasz program nie
// tlumaczy.  Wpisy martwe zostaja wiec w TEKSCIE, a do formatu plikowego
// idzie to, co faktycznie jest.
// Kopiowanie NAZW (Control+Shift+C) formatu plikowego NIE dostaje: nazwa bez
// katalogu nie wskazuje pliku, wiec powloka nie mialaby czego wkleic.
if (bPaths) {
List<string> lsFiles = new List<string>();
foreach (string sPick in lsPicked) {
try { if (File.Exists(sPick) || Directory.Exists(sPick)) lsFiles.Add(sPick); }
catch (Exception) {}
}
int iMissing = lsPicked.Count - lsFiles.Count;
if (lsFiles.Count > 0) {
if (!Util.SetClipboardFileDrop(lsFiles, sText)) { App.Frame.AddMessage("Clipboard is busy, nothing copied!"); return; }
// KOMUNIKAT MOWI, ZE SKOPIOWANO - I NIC WIECEJ (jego decyzja 11.09.2026:
// "trocha mylaco mowi Copied file (...) powinien mowic Copied po prostu").
// Slowo "file" nazywalo FORMAT schowka, czyli wewnetrzna sprawe programu, a
// nie to, co sie stalo.  Na schowku i tak leza OBA formaty naraz - plik i
// sciezka jako tekst - i to miejsce wklejenia wybiera, ktory wezmie: pole
// tekstowe wklei sciezke, Total Commander czy Eksplorator caly plik.
// Zapowiadanie jednego z nich z gory bylo wiec mylace w druga strone niz
// mial ten komunikat pomagac.
// Liczba przy wielu pozycjach ZOSTAJE: bez niej niewidomy nie wie, ile
// wlasnie zabral, bo podswietlenia nie slyszy.
string sSaid = (lsFiles.Count == 1) ? "Copied" : "Copied " + lsFiles.Count + " items";
if (iMissing > 0) sSaid += ", " + iMissing + " missing as text only";
App.Frame.AddMessage(sSaid);
return;
}
// Zaden z zaznaczonych wpisow nie istnieje na dysku: pliku nie ma czym
// skopiowac, wiec MOWIMY to wprost, a na schowek idzie sama sciezka jako
// tekst - to jedyne, co w tej sytuacji da sie oddac.
if (!Util.SetClipboardText(sText)) { App.Frame.AddMessage("Clipboard is busy, nothing copied!"); return; }
App.Frame.AddMessage((lsPicked.Count == 1) ? "Copied path as text, file no longer on disk" : "Copied" + sHowMany + " as text, files no longer on disk");
return;
}
if (Util.SetClipboardText(sText)) App.Frame.AddMessage((lsPicked.Count == 1) ? Char.ToUpper(sWhat[0]) + sWhat.Substring(1) + " copied" : "Copied" + sHowMany + " (" + sWhatMany + ")");
else App.Frame.AddMessage("Clipboard is busy, nothing copied!");
} // PickFileCopySelection method

// PickFileRemoveSelection: zdejmij z listy WSZYSTKIE zaznaczone wpisy.
//
// Jego decyzja z 06.09.2026 ("nie da sie usunac zaznaczonych dwoch"):
// zdejmowanie wpisu z listy ulubionych albo ostatnio uzywanych NIE dotyka
// plikow na dysku, tylko sekcji pliku ustawien, wiec pomylka kosztuje jedno
// otwarcie pliku na nowo.  Odmowa przy kilku zaznaczonych byla wiec ostroznoscia
// nie na miejscu.  Kasowanie z dysku (Shift+Delete) zostaje jednopozycyjne, bo
// tam stawka jest inna i tego rozroznienia sie nie znosi.
//
// KOLEJNOSC MALEJACA jest tu warunkiem poprawnosci, nie stylem: usuniecie
// pozycji 2 przesuwa wszystko powyzej o jeden, wiec petla rosnaca skasowalaby
// przy drugim obiegu NIE TEN wpis, ktory byl zaznaczony.  Pojedyncze zdjecie
// idzie z bSpeak=false, bo N komunikatow pod rzad zaglusza sie wzajemnie - mowa
// idzie RAZ, z LICZBA zdjetych pozycji.
private static void PickFileRemoveSelection(ListBox lb, List<string> lVal, List<string> lDisp, string sSection) {
List<int> liSel = new List<int>();
foreach (int iSel in lb.SelectedIndices) if (iSel >= 0 && iSel < lVal.Count) liSel.Add(iSel);
if (liSel.Count == 0) { App.Frame.AddMessage("No item!"); return; }
liSel.Sort();
int iFirst = liSel[0];
for (int i = liSel.Count - 1; i >= 0; i--) PickFileRemoveEntry(lb, lVal, lDisp, sSection, liSel[i], false);
if (lb.Items.Count == 0) { App.Frame.AddMessage("List is now empty"); return; }
// Kursor staje tam, gdzie stal PIERWSZY zdjety wpis, czyli na tym, co po
// usunieciu weszlo na jego miejsce.  Bez tego zaznaczenie zostawaloby na
// przypadkowej pozycji po ostatnim obiegu petli.
if (iFirst >= lb.Items.Count) iFirst = lb.Items.Count - 1;
LbcDialog.selectOnly(lb, iFirst);
// MOWIMY, ILE ZDJETO I ZE TO LISTA, nie dysk.  Ten komunikat stoi obok
// Shift+Delete, ktore kasuje plik z dysku, a po fakcie nie ma jak tych dwoch
// odroznic sluchem.
App.Frame.AddMessage((liSel.Count == 1) ? "Removed from list" : "Removed " + liSel.Count + " entries from list");
} // PickFileRemoveSelection method

// Remove the current item from the list box and (when sSection is set)
// from the backing INI section. Returns the path that was removed.
private static void PickFileRemoveEntry(ListBox lb, List<string> lVal, List<string> lDisp, string sSection, int i, bool bSpeak) {
if (i < 0 || i >= lVal.Count) return;
string sFile = lVal[i];
if (sSection.Length > 0) App.DeleteKey(sSection, sFile);
lVal.RemoveAt(i);
lDisp.RemoveAt(i);
lb.Items.RemoveAt(i);
if (lb.Items.Count == 0) {
if (bSpeak) App.Frame.AddMessage("List is now empty");
return;
}
if (i >= lb.Items.Count) i = lb.Items.Count - 1;
LbcDialog.selectOnly(lb, i);
// Say WHICH removal happened.  A bare "Removed" is ambiguous next to
// Shift+Delete, which erases the file from disk, and a screen reader user
// has no other way to tell the two apart after the fact.
if (bSpeak) App.Frame.AddMessage("Removed from list");
} // PickFileRemoveEntry method

// Permanently delete the current file from disk after an explicit
// confirmation, then drop its list entry. Mirrors the safeguards of the
// document-level delete: never deletes directories, closes any editor
// windows holding the file first, and aborts if a close is canceled.
private static void PickFileDeleteFromDisk(ListBox lb, List<string> lVal, List<string> lDisp, string sSection) {
int i = lb.SelectedIndex;
if (i < 0 || i >= lVal.Count) return;
string sFile = lVal[i];
if (sFile.Length == 0) return;
try {
if (Directory.Exists(sFile)) { App.Frame.AddMessage("This is a folder, not a file"); return; }
if (!File.Exists(sFile)) {
if (Dialog.Confirm("Confirm", "File not found on disk. Remove this entry from the list?\n" + sFile, "N") != "Y") return;
PickFileRemoveEntry(lb, lVal, lDisp, sSection, i, true);
return;
}
if (Dialog.Confirm("Confirm", "Permanently delete this file from disk, not just remove it from the list?\n" + sFile, "N") != "Y") return;
// Close any editor windows on this file first (snapshot the
// collection; Close() mutates MdiChildren while we iterate).
foreach (Form f in new List<Form>(App.Frame.MdiChildren)) {
MdiChild child = f as MdiChild;
if (child == null) continue;
if (String.Equals(child.File, sFile, StringComparison.OrdinalIgnoreCase)) child.Close();
}
foreach (Form f in App.Frame.MdiChildren) {
MdiChild child = f as MdiChild;
if (child == null) continue;
if (String.Equals(child.File, sFile, StringComparison.OrdinalIgnoreCase) && !child.IsDisposed) {
App.Frame.AddMessage("Delete canceled");
return;
}
}
File.Delete(sFile);
App.Frame.AddMessage("Deleted from disk");
PickFileRemoveEntry(lb, lVal, lDisp, sSection, i, false);
}
catch (Exception ex) { Dialog.Show("Error", ex.Message); }
} // PickFileDeleteFromDisk method

// CZYTANIE TRESCI OKIENKA PRZEZ CZYTNIK EKRANU (zgloszenie Kasperczaka
// 12.09.2026: "warto, zeby NVDA sam czytal te okienka, bo na razie czyta Tak/Nie,
// a recznie zawartosc musze przeczytac").
//
// DLACZEGO TRESC NIE BYLA CZYTANA.  Okienka stawia MessageBox.Show.  Czytnik
// oglasza to, co dostaje FOKUS, a fokus w takim okienku dostaje PRZYCISK -
// stad samo "Tak/Nie". Sam tekst komunikatu to etykieta bez fokusu, wiec
// uzytkownik musi po niego wracac recznie. Przy pytaniu, na ktore odpowiada
// sie Tak albo Nie, to jest grozne: mozna odpowiedziec, nie wiedzac na co.
//
// DLACZEGO Z OPOZNIENIEM, A NIE OD RAZU.  Gdybysmy powiedzieli tekst przed
// pokazaniem okienka, czytnik natychmiast przerwalby go wlasnym oglaszaniem
// okna i przycisku - uzytkownik uslyszalby poczatek zdania i "Tak". Dlatego
// mowimy PO tym, jak czytnik skonczy swoje: w osobnym watku, po krotkiej
// przerwie. Watek jest konieczny, bo MessageBox.Show blokuje wszystko do
// zamkniecia okienka.
//
// Dlugosc przerwy da sie zmienic wpisem DialogSpeechDelayMs w sekcji [Options],
// a wpisanie 0 wylacza czytanie calkiem - gdyby czyjs czytnik radzil sobie sam
// i mowil wszystko dwa razy.
// OKIENKA KOMUNIKATOW I PYTAN - WLASNE, NIE MessageBox (12.09.2026).
//
// ZGLOSZENIE Kasperczaka: "F11 czyta wpierw OK a potem EdSharpNG 5.0.87 is up
// to date.  A powinien wpierw okno a potem OK, tak samo wpierw okno o
// dostepnych aktualizacjach, a potem Tak/Nie."  I ogolniej: "tak okienka
// powinny dzialac".
//
// DLACZEGO POPRZEDNIE PODEJSCIE BYLO ZLE.  W 5.0.84 dolozylem czytanie tresci
// z opoznieniem (SayDialogText: odczekaj 400 ms, potem powiedz).  To ZLE
// rozwiazanie, mimo ze cos poprawialo: opoznienie jest WYSCIGIEM z czytnikiem.
// Nie da sie dobrac liczby, ktora bedzie dobra zawsze - przy szybkiej mowie
// czytnik konczy wczesniej i tresc pada w cisze, przy wolnej wchodzi mu w
// slowo, a na obcionym komputerze wszystko sie przesuwa.  Wynik slyszalny dla
// uzytkownika: najpierw "OK", potem tresc - czyli odwrotnie niz trzeba.
//
// WLASCIWE ROZWIAZANIE: ZMIENIC BUDOWE OKNA, NIE DOBIERAC OPOZNIENIA.
// Czytnik czyta zawartosc okna dialogowego i element, na ktorym stoi fokus.
// W MessageBox fokus startuje NA PRZYCISKU, a tresc jest zwyklym napisem
// (statycznym tekstem), ktorego kolejnosc odczytu zalezy od czytnika.  Dlatego
// budujemy okno SAMI: tresc jest POLEM TEKSTOWYM tylko do czytania i to ONO
// dostaje fokus na starcie.  Wtedy kolejnosc "najpierw tresc, potem przycisk"
// wynika z BUDOWY okna, a nie z tego, czy zdazylismy - i jest taka sama przy
// kazdej szybkosci mowy i w kazdym czytniku.
//
// Skutki dodatkowe, wszystkie pozytywne:
// - tresc da sie PRZECZYTAC PONOWNIE strzalkami, bez zamykania okna;
// - dluga tresc da sie przewijac i skopiowac (Ctrl+C);
// - Enter i Escape dzialaja jak wszedzie w programie;
// - kazde okno ma przycisk Help (F1), jak reszta okien EdSharpNG.
//
// TA SAMA PULAPKA W AMC: Alt+F4 przy nagrywaniu w tle nie meldowal, co robi.
// Wzorzec do zapamietania: NIGDY nie zalatwiaj kolejnosci mowy opoznieniem -
// ustaw fokus na tresci.  Opis w umiejetnosci komunikaty-i-skroty-dla-czytnika-ekranu.
//
// Wpis DialogSpeechDelayMs=0 w [Options] wraca do zwyklego MessageBox - na
// wypadek gdyby ktos wolal zachowanie systemowe.
static bool UzyjWlasnychOkien() {
try {
string sDelay = App.ReadOption("DialogSpeechDelayMs", "400").Trim();
int iDelay;
if (!Int32.TryParse(sDelay, out iDelay)) return true;
return iDelay != 0;
}
catch { return true; }
} // UzyjWlasnychOkien method

// Buduje okno z trescia jako polem do czytania i podanymi przyciskami.
// Zwraca napis przycisku, ktory uzytkownik nacisnal ("" przy Escape).
static string PokazOknoZTrescia(string sTitle, string sText, string[] asButtons, string sTip) {
LbcDialog dlg = new LbcDialog(sTitle, App.Frame);
TextBox tb = dlg.addMemo(sText, sTip);
tb.ReadOnly = true;
// WYSOKOSC DOPASOWANA DO TRESCI.  addMemo daje staly rozmiar na kilka
// wierszy, bo sluzy do WPISYWANIA tekstu.  Tutaj tresc jest znana z gory:
// jednozdaniowy komunikat ("EdSharpNG 5.0.87 is up to date") w wysokim
// pustym prostokacie wyglada jak blad, a dluga tresc ma sie zmiescic bez
// przewijania.  Licze wiersze i dokladam zapas na zawijanie dlugich linii.
try {
int iLinie = 1;
foreach (string sLinia in sText.Replace("\r\n", "\n").Split('\n')) {
iLinie += 1 + (sLinia.Length / 70);   // 70 znakow na wiersz przy tej szerokosci
}
if (iLinie < 2) iLinie = 2;
if (iLinie > 18) iLinie = 18;         // wyzej i tak nie zmiesci sie na ekranie
tb.Height = tb.Font.Height * iLinie + 8;
}
catch {}
// Fokus NA TRESCI - to jest cala poprawka.  Bez tego czytnik zaczyna od
// przycisku, tak jak w MessageBox.
tb.AccessibleName = "Message";
dlg.setInitialFocus(tb);
return dlg.runWithButtons(asButtons);
} // PokazOknoZTrescia method

// SayDialogText - czytanie tresci z opoznieniem.  ZOSTAJE tylko dla drogi
// awaryjnej (DialogSpeechDelayMs=0 wlacza systemowy MessageBox, gdzie nic
// innego nie da sie zrobic).  W zwyklej pracy programu NIE jest uzywana -
// patrz komentarz przy UzyjWlasnychOkien.
static void SayDialogText(string sTitle, string sText) {
try {
string sDelay = App.ReadOption("DialogSpeechDelayMs", "400").Trim();
int iDelay;
if (!Int32.TryParse(sDelay, out iDelay) || iDelay < 0) iDelay = 400;
if (iDelay == 0) return;
if (String.IsNullOrEmpty(sText)) return;
// Tytul dokladamy tylko wtedy, gdy nie powtarza tresci - czytnik i tak
// oglasza nazwe okna, a slyszenie tego samego dwa razy pod rzad jest
// gorsze niz nieslyszenie w ogole.
string sSay = sText;
System.Threading.Thread th = new System.Threading.Thread(delegate() {
try {
System.Threading.Thread.Sleep(iDelay);
Say.sayForced(sSay);
}
catch {}
});
th.IsBackground = true;   // nie moze trzymac programu przy zamykaniu
th.Start();
}
catch {}
} // SayDialogText method

public static string Confirm(string sTitle, string sText, string sDefault) {
MessageBoxDefaultButton defaultButton;
if (sDefault.ToLower() == "n") defaultButton = MessageBoxDefaultButton.Button2;
else defaultButton = MessageBoxDefaultButton.Button1;

// WLASNE OKNO: tresc pytania ma fokus, wiec czytnik mowi JA pierwsza, a
// dopiero potem przycisk - patrz dlugi komentarz przy UzyjWlasnychOkien.
// Kolejnosc przyciskow zalezy od domyslnej odpowiedzi, bo pierwszy przycisk
// jest tym, ktory naciska Enter.  Przy pytaniu grozniejszym (domyslne "nie")
// pod Enterem MUSI byc "No" - inaczej odruchowy Enter robi to, czego
// uzytkownik nie chcial.
if (UzyjWlasnychOkien()) {
try {
string[] asButtons = (sDefault.ToLower() == "n")
  ? new string[] {"&No", "&Yes", "Cancel"}
  : new string[] {"&Yes", "&No", "Cancel"};
string sWybor = PokazOknoZTrescia(sTitle, sText, asButtons,
  "Read the question with the arrow keys, then choose an answer.  Enter presses the first button, Escape cancels.");
if (sWybor == "Yes") return "Y";
if (sWybor == "No") return "N";
return "";
}
catch {
// Gdyby wlasne okno z jakiegos powodu nie wstalo, pytanie MUSI sie
// pokazac - inaczej program cicho podjalby decyzje za uzytkownika.
}
}

SayDialogText(sTitle, sText);
switch (MessageBox.Show(sText, sTitle, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, defaultButton)) {
case DialogResult.Yes :
//Util.Say("Yes");
return "Y";
case DialogResult.No :
//Util.Say("No");
return "N";
}
/*Util.Say("Cancel");*/
return "";
} // Confirm method

public static void Show(object oText) {
Show("Show", oText);
} // Show method

public static void Show(object oTitle, object oText) {
string sTitle = oTitle.ToString();
string sText = oText.ToString();
if (oTitle is bool) sTitle = ((bool) oTitle) ? "true" : "false";
if (oText is bool) sText = (bool) oText ? "true" : "false";
// Tresc czytana JAKO PIERWSZA, bo ma fokus - w tym wlasnie tkwila skarga
// na F11 ("czyta wpierw OK a potem EdSharpNG jest aktualny").  Chodzi
// zwykle o komunikat, ktorego uzytkownik NIE zamowil, wiec przeczytanie go
// w calosci i we wlasciwej kolejnosci jest jedynym sposobem, zeby dotarl.
if (UzyjWlasnychOkien()) {
try {
PokazOknoZTrescia(sTitle, sText, new string[] {"OK"},
  "Read the message with the arrow keys.  Enter or Escape closes this window.");
return;
}
catch {
// Komunikat MUSI sie pokazac - spadamy na okno systemowe.
}
}

SayDialogText(sTitle, sText);
MessageBox.Show(sText, sTitle);
} // Show method

public static void Properties(string sPath) {
COM.InvokeVerb(sPath, "P&roperties");
COM.InvokeVerb(sPath, "Properties");
// Win32.ShellExecute("Properties", sPath);
} // Properties method

public static string Choose (string sTitle, string sText, string[] aButtons, int iDefault) {
LbcDialog dlg = new LbcDialog(sTitle, App.Frame);
if (sText != "") dlg.addLabel(sText);
List<string> lButtons = new List<string>(aButtons);
bool bHasCancel = false;
foreach (string sButton in aButtons) if (sButton.Replace("&", "").Equals("Cancel", StringComparison.OrdinalIgnoreCase)) bHasCancel = true;
if (!bHasCancel) lButtons.Add("Cancel");
string sClicked = dlg.runWithButtons(lButtons.ToArray());
dlg.Dispose();
string sResult = "";
foreach (string sButton in aButtons) if (sButton.Replace("&", "") == sClicked) { sResult = sButton; break; }
Util.Say(sResult.Replace("&", ""));
return sResult;
} // Choose method

public static object[] PickAndChoose(string sTitle, object[] aValue, string[] aDisplay, string[] aButton, bool bSort, int iIndex) {
object[] aVal = (object[]) aValue.Clone();
string[] aDisp;
if (aDisplay == null) {
aDisp = new string[aVal.Length];
for (int i = 0; i < aVal.Length; i++) aDisp[i] = (aVal[i] == null) ? "" : aVal[i].ToString();
}
else aDisp = (string[]) aDisplay.Clone();
if (bSort) Array.Sort(aDisp, aVal);
List<string> lNames = new List<string>(aDisp);
LbcDialog dlg = new LbcDialog(sTitle, App.Frame);
ListBox lst = dlg.addListBox(lNames, "", "");
LbcDialog.selectOnly(lst, (iIndex >= 0 && iIndex < lNames.Count) ? iIndex : 0);
List<string> lButtons = new List<string>(aButton);
bool bHasCancel = false;
foreach (string sButton in aButton) if (sButton.Replace("&", "").Equals("Cancel", StringComparison.OrdinalIgnoreCase)) bHasCancel = true;
if (!bHasCancel) lButtons.Add("Cancel");
string sClicked = dlg.runWithButtons(lButtons.ToArray());
int iPicked = lst.SelectedIndex;
dlg.Dispose();
string sButtonResult = "";
foreach (string sButton in aButton) if (sButton.Replace("&", "") == sClicked) { sButtonResult = sButton; break; }
object[] aResult = {};
if (sButtonResult != "" && iPicked >= 0 && iPicked < aVal.Length) aResult = new object[] {aVal[iPicked], sButtonResult};
return aResult;
} // PickAndChoose method

public static object[] GetFont(Font font, Color color) {
//ColorDialog d = new ColorDialog();
//d.ShowDialog();
FontDialog dlg = new FontDialog();
dlg.FontMustExist = true;
dlg.ShowColor = true;
dlg.Font = font;
dlg.Color = color;
object[] aReturn = {};
if(dlg.ShowDialog() == DialogResult.OK) aReturn = new object[] {dlg.Font, dlg.Color};
dlg.Dispose();
return aReturn;
} // GetFont method

public static string OpenFolder(string sTitle, string sLabel, string sValue) {
string sResult = "";

Form frm = new Form();
frm.SuspendLayout();
frm.AutoSize = true;
frm.AutoSizeMode = AutoSizeMode.GrowAndShrink;

FlowLayoutPanel flpMain = new FlowLayoutPanel();
flpMain.SuspendLayout();
flpMain.AutoSize = true;
flpMain.AutoSizeMode  = AutoSizeMode.GrowAndShrink;
flpMain.FlowDirection = FlowDirection.TopDown;

FlowLayoutPanel flpInput = new FlowLayoutPanel();
flpInput.SuspendLayout();
flpInput.Anchor = AnchorStyles.None;
flpInput.AutoSize = true;
flpInput.AutoSizeMode  = AutoSizeMode.GrowAndShrink;
flpInput.FlowDirection = FlowDirection.LeftToRight;

Label lbl = new Label();
lbl.AutoSize = true;
lbl.Text = sLabel + ":";
TextBox txt = new TextBox();
//txt.ScrollBars = ScrollBars.None;
txt.Width *= 2;
txt.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
txt.AutoCompleteSource = AutoCompleteSource.FileSystemDirectories;
txt.Text = sValue;
txt.AccessibleName = lbl.Text.Replace("&", "");
txt.GotFocus += delegate(object o, EventArgs e) {
txt.SelectAll();
};

Button btnBrowse = new Button();
btnBrowse.Click += delegate(object o, EventArgs e) {
txt.Text = Dialog.BrowseForFolder("", sValue, false);
txt.Select();
};
btnBrowse.Text = "&Browse";
btnBrowse.AccessibleName = btnBrowse.Text.Replace("&", "");

flpInput.Controls.AddRange(new Control[] {lbl, txt, btnBrowse});
flpInput.ResumeLayout();

FlowLayoutPanel flpButtons = new FlowLayoutPanel();
flpButtons.SuspendLayout();
flpButtons.Anchor = AnchorStyles.None;
flpButtons.AutoSize = true;
flpButtons.AutoSizeMode  = AutoSizeMode.GrowAndShrink;
flpButtons.FlowDirection = FlowDirection.LeftToRight;

Button btnOK = new Button();
btnOK.Click += delegate(object o, EventArgs e) {
sResult = txt.Text.Trim();
if (sResult != "" && !Directory.Exists(sResult)) {
string sChoice = Dialog.Confirm("Confirm", "Cannot find folder\n" + sResult + "\nCreate it?", "Y");
if (sChoice == "Y") {
try {
DirectoryInfo di = new DirectoryInfo(sResult);
di.Create();
}
catch (Exception ex) {
Dialog.Show("Error", ex.Message);
}
}
}
if (Directory.Exists(sResult)) frm.Close();
else {
txt.SelectAll();
txt.Select();
}
};

btnOK.Text = "OK";
btnOK.AccessibleName = btnOK.Text;

Button btnCancel = new Button();
btnCancel.Click += delegate(object o, EventArgs e) {
/*Util.Say("Cancel");*/ sResult = "";
frm.Close();
};
btnCancel.Text = "Cancel";
btnCancel.AccessibleName = btnCancel.Text;

flpButtons.Controls.AddRange(new Control[] {btnOK, btnCancel});
flpButtons.ResumeLayout();

flpMain.Controls.AddRange(new Control[] {flpInput, flpButtons});
flpMain.ResumeLayout();

frm.AcceptButton = btnOK;
frm.CancelButton = btnCancel;
frm.StartPosition = FormStartPosition.CenterParent;
frm.Text = sTitle;
frm.Controls.Add(flpMain);
frm.ResumeLayout();
frm.Shown += delegate(object sender, EventArgs e) {
Win32.SetForegroundWindow(frm.Handle);
};
frm.ShowDialog();
frm.Dispose();
return sResult;
} // GetDirectory method

public static string BrowseForFolder(string sTitle, string sDir) {
bool bNewFolder = false;
return BrowseForFolder(sTitle, sDir, bNewFolder);
} // BrowseForFolder method

public static string BrowseForFolder(string sTitle, string sDir, bool bNewFolder) {
string sReturn = "";
FolderBrowserDialog dlg = new FolderBrowserDialog();
dlg.Description = sTitle;
dlg.ShowNewFolderButton = bNewFolder;
//dlg.RootFolder = sRootFolder;
dlg.SelectedPath = sDir;

if (dlg.ShowDialog() == DialogResult.OK) sReturn = dlg.SelectedPath;
dlg.Dispose();
return sReturn;
} // BrowseForFolder method

public static string[] PickAndInputDialog(string sTitle, string sLblList, string[] aValues, string sLblInput, string sValue, bool bSort, int iIndex) {
string[] aResults = {};

Form frm = new Form();
frm.SuspendLayout();
frm.AutoSize = true;
frm.AutoSizeMode = AutoSizeMode.GrowAndShrink;

FlowLayoutPanel flpMain = new FlowLayoutPanel();
flpMain.SuspendLayout();
flpMain.AutoSize = true;
flpMain.AutoSizeMode  = AutoSizeMode.GrowAndShrink;
flpMain.FlowDirection = FlowDirection.TopDown;

FlowLayoutPanel flpInput = new FlowLayoutPanel();
flpInput.SuspendLayout();
flpInput.Anchor = AnchorStyles.None;
flpInput.AutoSize = true;
flpInput.AutoSizeMode  = AutoSizeMode.GrowAndShrink;
flpInput.FlowDirection = FlowDirection.LeftToRight;

Label lblList = new Label();
lblList.Text = sLblList + ":";
lblList.AccessibleName = lblList.Text.Replace("&", "");

ListBox lst = new ListBox();
if (bSort) lst.Sorted = true;
lst.Items.AddRange(aValues);
LbcDialog.selectOnly(lst, iIndex);

Label lblInput = new Label();
lblInput.Text = sLblInput + ":";
lblInput.AccessibleName = lblInput.Text.Replace("&", "");
TextBox txt = new TextBox();
txt.Width *= 2;
txt.AccessibleName = lblInput.AccessibleName;
if (lblInput.Text.Contains("Password:")) txt.UseSystemPasswordChar = true;
txt.Text = sValue;

flpInput.Controls.AddRange(new Control[] {lblList, lst, lblInput, txt});
flpInput.ResumeLayout();

FlowLayoutPanel flpButtons = new FlowLayoutPanel();
flpButtons.SuspendLayout();
flpButtons.Anchor = AnchorStyles.None;
flpButtons.AutoSize = true;
flpButtons.AutoSizeMode  = AutoSizeMode.GrowAndShrink;
flpButtons.FlowDirection = FlowDirection.LeftToRight;

Button btnOK = new Button();
btnOK.Click += delegate(object o, EventArgs e) {
aResults = new string[] {
lst.Text, txt.Text
};
frm.Close();
};

btnOK.Text = "OK";
btnOK.AccessibleName = btnOK.Text;

Button btnCancel = new Button();
btnCancel.Click += delegate(object o, EventArgs e) {
/*Util.Say("Cancel");*/ frm.Close();
};
btnCancel.Text = "Cancel";
btnCancel.AccessibleName = btnCancel.Text;

flpButtons.Controls.AddRange(new Control[] {btnOK, btnCancel});
flpButtons.ResumeLayout();

flpMain.Controls.AddRange(new Control[] {flpInput, flpButtons});
flpMain.ResumeLayout();

frm.AcceptButton = btnOK;
frm.CancelButton = btnCancel;
frm.StartPosition = FormStartPosition.CenterParent;
frm.Text = sTitle;
frm.Controls.Add(flpMain);
frm.ResumeLayout();
frm.Shown += delegate(object sender, EventArgs e) {
Win32.SetForegroundWindow(frm.Handle);
};
frm.ShowDialog();
frm.Dispose();
return aResults;
} // PickAndInput method

} // Dialog class

// Script: late-bound bridge to EdSharp.dll, the JScript .NET host built
// from EdSharp.js. Loaded by path (not /reference) so the exe and the
// same-named dll do not collide at load time. The MethodInfo is cached
// after first use. run returns the script result string, or text that
// begins "ERROR: " on a compile or runtime fault in the snippet.
// PickItem: carries a display string together with the index of the value
// it represents, so a sorted pick-list can map the selected row back to the
// original value array. Replaces the former VB6 ListBox ItemData shim.
// ===== Speech subsystem (Say + UIA) moved to Say.cs (namespace Homer) =

// ===== Lbc dialog classes moved to Lbc.cs (portable, shared with DbDuo) =====

// ===== JAWS script installer (DbDo-style) ==================================
// Invoked by the installer's Finish-page option as
// "EdSharp.exe --install-jaws-settings". For every installed JAWS version
// (per-user %APPDATA%\Freedom Scientific\JAWS\<ver>\Settings\<lang>), copies
// EdSharp's JAWS settings family in and compiles homer.jss then EdSharp.jss
// (Homer first, since EdSharp.jss does Use "Homer.jsb"). scompile.exe for each
// version is found via HKLM\Software\Freedom Scientific\JAWS\<ver>\Target.
public static class JawsScripts {

static string findScompilePath(string sVersion) {
try {
using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"Software\Freedom Scientific\JAWS\" + sVersion)) {
if (key != null) {
string sTarget = key.GetValue("Target") as string;
if (!String.IsNullOrEmpty(sTarget)) {
string sCompile = Path.Combine(sTarget, "scompile.exe");
if (File.Exists(sCompile)) return sCompile;
}
}
}
}
catch {}
string sPf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
string sFallback = Path.Combine(sPf, @"Freedom Scientific\JAWS\" + sVersion + @"\scompile.exe");
if (File.Exists(sFallback)) return sFallback;
return null;
} // findScompilePath method

static void compileOne(string sScompile, string sLangPath, string sJss, StringBuilder sb, ref int iCompiled) {
if (String.IsNullOrEmpty(sScompile)) return;
try {
ProcessStartInfo psi = new ProcessStartInfo(sScompile, "\"" + sJss + "\"");
psi.WorkingDirectory = sLangPath;
psi.UseShellExecute = false;
psi.CreateNoWindow = true;
using (Process proc = Process.Start(psi)) {
proc.WaitForExit(15000);
string sJsb = Path.Combine(sLangPath, Path.GetFileNameWithoutExtension(sJss) + ".jsb");
if (proc.HasExited && File.Exists(sJsb)) iCompiled++;
else sb.AppendLine("WARN: compile may have failed: " + sJss + " in " + sLangPath);
}
}
catch (Exception ex) { sb.AppendLine("FAIL: compile " + sJss + ": " + ex.Message); }
} // compileOne method

public static string install(string sScriptsFolder, out int iCopied, out int iCompiled) {
iCopied = 0;
iCompiled = 0;
StringBuilder sb = new StringBuilder();
string sJawsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Freedom Scientific\JAWS");
if (!Directory.Exists(sJawsRoot)) {
sb.AppendLine("JAWS does not appear to be installed for the current user.");
sb.AppendLine("(No folder at " + sJawsRoot + ")");
return sb.ToString();
}
// Settings family to place in each JAWS settings folder: {source, target}.
string[][] aFiles = new string[][] {
new string[] {"EdSharp.JSS", "EdSharp.jss"},
new string[] {"edsharp.jkm", "EdSharp.jkm"},
new string[] {"EdSharp.JCF", "EdSharp.jcf"},
new string[] {"EdSharp.jsd", "EdSharp.jsd"},
new string[] {"Homer.jsh", "Homer.jsh"},
new string[] {"MSAA.jsh", "MSAA.jsh"},
new string[] {"homer.jss", "homer.jss"},
new string[] {"homer.jsd", "homer.jsd"}
};
foreach (string sVersionPath in Directory.GetDirectories(sJawsRoot)) {
string sVersion = Path.GetFileName(sVersionPath);
string sSettingsPath = Path.Combine(sVersionPath, "Settings");
if (!Directory.Exists(sSettingsPath)) continue;
string sScompile = findScompilePath(sVersion);
foreach (string sLangPath in Directory.GetDirectories(sSettingsPath)) {
foreach (string[] aPair in aFiles) {
string sSrc = Path.Combine(sScriptsFolder, aPair[0]);
string sDst = Path.Combine(sLangPath, aPair[1]);
if (!File.Exists(sSrc)) continue;
try { File.Copy(sSrc, sDst, true); iCopied++; }
catch (Exception ex) { sb.AppendLine("FAIL: copy " + aPair[1] + ": " + ex.Message); }
}
compileOne(sScompile, sLangPath, "homer.jss", sb, ref iCompiled);
compileOne(sScompile, sLangPath, "EdSharp.jss", sb, ref iCompiled);
if (String.IsNullOrEmpty(sScompile)) sb.AppendLine("WARN: scompile.exe not found for JAWS " + sVersion + "; files placed but not compiled.");
else sb.AppendLine("JAWS " + sVersion + " / " + Path.GetFileName(sLangPath) + ": done");
}
}
return sb.ToString();
} // install method

} // JawsScripts class

// InixCodec: order-preserving INI/INIX reader-writer ported from DbDo.
// ===== InixCodec moved to Inix.cs (namespace Homer) =========================

public class PickItem {
public int iValue;
public string sText;

public PickItem(string sText, int iValue) {
this.sText = sText;
this.iValue = iValue;
} // PickItem constructor

public override string ToString() { return sText; }
} // PickItem class

// VB: convenience methods ported from the former VB.vb support module,
// translated to C# with late-bound COM through dynamic so no separate
// VB.dll is needed. The Office text extractors (Xls2Txt, Ppt2Txt) and the
// Word automation are legacy COM, and are candidates to be replaced by
// Pandoc in a later stage. LookupTerm and GetLinks drive Internet Explorer,
// which is removed from current Windows; they are kept only for source
// compatibility and will likely be retired.
public class VB {
private const int iMsoTextEffect = 15; // MsoShapeType.msoTextEffect
private const int iWordFormatText = 2; // WdSaveFormat.wdFormatText
private const int iXlTextFormat = 21; // XlFileFormat current-region text
private const string sFormFeed = "\f";

// ---- Excel ----
public static object ExcelOpen(object oXlss, string sFile) {
dynamic xlss = oXlss;
try { return xlss.Open(sFile, false, true); } // UpdateLinks, ReadOnly
catch (Exception ex) { MessageBox.Show(ex.Message, "Error!"); return null; }
} // ExcelOpen method

public static void ExcelSaveAs(object oXls, string sFile, int iFileFormat) {
dynamic xls = oXls;
try { xls.SaveAs(sFile, iFileFormat); }
catch (Exception ex) { MessageBox.Show(ex.Message, "Error!"); }
} // ExcelSaveAs method

public static void ExcelClose(object oXls) {
dynamic xls = oXls;
try { xls.Close(0); }
catch (Exception ex) { MessageBox.Show(ex.Message, "Error!"); }
} // ExcelClose method

public static void ExcelQuit(object oApp) {
dynamic app = oApp;
try { app.Quit(); }
catch (Exception ex) { MessageBox.Show(ex.Message, "Error!"); }
} // ExcelQuit method

// ---- PowerPoint ----
public static object PowerPointOpen(object oPpts, string sFile) {
dynamic ppts = oPpts;
try { return ppts.Open(sFile, true); } // ReadOnly
catch (Exception ex) { MessageBox.Show(ex.Message, "Error!"); return null; }
} // PowerPointOpen method

public static void PowerPointSaveAs(object oPpt, string sFile, int iFileFormat) {
dynamic ppt = oPpt;
try { ppt.SaveAs(sFile); }
catch (Exception ex) { MessageBox.Show(ex.Message, "Error!"); }
} // PowerPointSaveAs method

public static void PowerPointClose(object oPpt) {
dynamic ppt = oPpt;
try { ppt.Close(); }
catch (Exception ex) { MessageBox.Show(ex.Message, "Error!"); }
} // PowerPointClose method

public static void PowerPointQuit(object oApp) {
dynamic app = oApp;
try { app.Quit(); }
catch (Exception ex) { MessageBox.Show(ex.Message, "Error!"); }
} // PowerPointQuit method

// ---- Word ----
public static object WordOpen(object oDocs, string sFile, bool bAppVisible) {
dynamic docs = oDocs;
try { return docs.Open(sFile, false, false, false); } // ConfirmConversions, ReadOnly, AddToRecentFiles
catch (Exception ex) { MessageBox.Show(ex.Message, "Error!"); return null; }
} // WordOpen method

public static void WordSaveAs(object oDoc, string sFile, int iFileFormat) {
dynamic doc = oDoc;
try { doc.SaveAs(sFile, iFileFormat, false, "", false); } // LockComments, Password, AddToRecentFiles
catch (Exception ex) { MessageBox.Show(ex.Message, "Error!"); }
} // WordSaveAs method

public static void WordClose(object oDoc) {
dynamic doc = oDoc;
ClearNormalTemplate(doc.Application);
try { doc.Close(0); }
catch (Exception ex) { MessageBox.Show(ex.Message, "Error!"); }
} // WordClose method

public static void ClearNormalTemplate(object oApp) {
dynamic app = oApp;
dynamic template = app.NormalTemplate;
template.Saved = true;
Marshal.ReleaseComObject(template);
} // ClearNormalTemplate method

public static void WordQuit(object oApp) {
dynamic app = oApp;
ClearNormalTemplate(app);
try { app.Quit(0); }
catch (Exception ex) { MessageBox.Show(ex.Message, "Error!"); }
} // WordQuit method

// ---- Office text extractors (legacy COM) ----
public static object Ppt2Txt(string sSource, string sTarget) {
dynamic app = null;
bool bGet = false;
int iAlerts = 0;
try { app = Marshal.GetActiveObject("PowerPoint.Application"); iAlerts = (int) app.DisplayAlerts; bGet = true; }
catch { app = Activator.CreateInstance(Type.GetTypeFromProgID("PowerPoint.Application")); bGet = false; }
app.Visible = true; // must be visible for COM automation
app.DisplayAlerts = false;
dynamic ppts = app.Presentations;
dynamic ppt = PowerPointOpen(ppts, sSource);

string s = ppt.Name;
string sText = Path.GetFileNameWithoutExtension(s);
dynamic slides = ppt.Slides;
int iSlideCount = (int) slides.Count;
sText = sText + "\r\n" + iSlideCount.ToString() + " Slide" + (iSlideCount == 1 ? "" : "s");

int iSlide = 1;
while (iSlide <= iSlideCount) {
dynamic slide = slides.Item(iSlide);
sText = sText + "\r\n" + "\r\n" + "----------" + "\r\n" + sFormFeed + "\r\n" + "Slide " + iSlide.ToString();

dynamic notes = slide.NotesPage;
int iNoteCount = (int) notes.Count;
bool bNoteLabel = true;
int iNote = 1;
while (iNote <= iNoteCount) {
dynamic note = notes.Item(iNote);
dynamic ships = note.Shapes;
int iShipCount = (int) ships.Count;
int iShip = 1;
while (iShip <= iShipCount) {
dynamic ship = ships.Item(iShip);
if ((int) ship.HasTextFrame != 0) {
dynamic frame = ship.TextFrame;
dynamic text = frame.TextRange;
s = text.Text;
if (s != "") {
if (bNoteLabel) { sText = sText + "\r\n" + "Notes:" + "\r\n" + s; bNoteLabel = false; }
else sText = sText + "\r\n" + s;
}
}
iShip = iShip + 1;
}
sText = sText.Trim();
iNote = iNote + 1;
}

dynamic shapes = slide.Shapes;
int iShapeCount = (int) shapes.Count;
bool bOutlineLabel = true;
int iShape = 1;
while (iShape <= iShapeCount) {
dynamic shape = shapes.Item(iShape);
s = "";
if ((int) shape.HasTextFrame != 0) {
dynamic textFrame = shape.TextFrame;
dynamic textRange = textFrame.TextRange;
s = textRange.Text;
if (s != "" && s.ToLower() != "outline") {
if (bOutlineLabel) { sText = sText + "\r\n" + "Outline:" + "\r\n" + s; bOutlineLabel = false; }
else sText = sText + "\r\n" + s;
}
}
if ((int) shape.HasTextFrame == 0 || s == "") {
s = shape.AlternativeText;
if (s != "") sText = sText + "\r\n" + s;
int iType = (int) shape.Type;
if (iType == iMsoTextEffect) {
dynamic textEffect = shape.TextEffect;
s = textEffect.Text;
if (s != "" && s != (string) shape.AlternativeText) sText = sText + "\r\n" + "Text Effect: " + s;
}
}
sText = sText.Trim();
iShape = iShape + 1;
}
sText = sText.Trim();
iSlide = iSlide + 1;
}

File.WriteAllText(sTarget, sText);
ppt.Saved = true;
PowerPointClose(ppt);
Marshal.ReleaseComObject(ppt);
Marshal.ReleaseComObject(ppts);
if (bGet) { app.DisplayAlerts = iAlerts; }
else PowerPointQuit(app);
Marshal.ReleaseComObject(app);
return File.Exists(sTarget);
} // Ppt2Txt method

public static bool Xls2Txt(string sSource, string sTarget) {
dynamic app = null;
bool bGet = false;
int iAlerts = 0, iUpdating = 0, iVisible = 0;
try {
app = Marshal.GetActiveObject("Excel.Application");
bGet = true;
iVisible = (int) app.Visible;
iAlerts = (int) app.DisplayAlerts;
iUpdating = (int) app.ScreenUpdating;
}
catch { app = Activator.CreateInstance(Type.GetTypeFromProgID("Excel.Application")); bGet = false; }
app.Visible = false;
app.DisplayAlerts = false;
app.ScreenUpdating = false;
dynamic xlss = app.Workbooks;
dynamic xls = ExcelOpen(xlss, sSource);
dynamic sheets = xls.Sheets;
int iSheetCount = (int) sheets.Count;
int iSheet = 1;
string sBook = "";
while (iSheet <= iSheetCount) {
dynamic sheet = sheets.Item(iSheet);
if (File.Exists(sTarget)) File.Delete(sTarget);
ExcelSaveAs(xls, sTarget, iXlTextFormat);
string sName = "Sheet " + iSheet.ToString();
if (((string) sheet.Name).Length > 0) sName = sName + ": " + sheet.Name;
ExcelClose(xls);
string s = sName + "\r\n" + File.ReadAllText(sTarget).Trim();
sBook = sBook + (iSheet > 1 ? "\r\n" + "----------" + "\r\n" + sFormFeed + "\r\n" : "") + s;
xls = ExcelOpen(xlss, sSource);
sheets = xls.Sheets;
iSheet = iSheet + 1;
}
if (File.Exists(sTarget)) File.Delete(sTarget);
File.WriteAllText(sTarget, sBook);
ExcelClose(xls);
Marshal.ReleaseComObject(xls);
Marshal.ReleaseComObject(xlss);
if (bGet) { app.Visible = iVisible; app.DisplayAlerts = iAlerts; app.ScreenUpdating = iUpdating; }
else ExcelQuit(app);
Marshal.ReleaseComObject(app);
return File.Exists(sTarget);
} // Xls2Txt method

// ---- Web helpers ----
// DownloadFile: simple authenticated download. Modern WebClient replaces
// the former My.Computer.Network.DownloadFile.
public static void DownloadFile(string sUrl, string sFile, string sUserName, string sPassword) {
Homer.Web.configure();
using (WebClient web = new WebClient()) {
web.Headers[HttpRequestHeader.UserAgent] = Homer.Web.userAgent();
if (sUserName.Length > 0) web.Credentials = new NetworkCredential(sUserName, sPassword);
web.DownloadFile(sUrl, sFile);
}
} // DownloadFile method

// LookupTerm: legacy dictionary lookup via Internet Explorer automation.
// IE is removed from current Windows; retained for source compatibility.
public static string LookupTerm(string sWord) {
if (sWord.Length == 0) return "";
string sUrl = "http://dictionary.reference.com/browse/";
string sDivider = "\r\n" + "----------" + "\r\n" + sFormFeed + "\r\n";
string sText = sWord + "\r\n" + "\r\n" + "Contents" + "\r\n";
sText = sText + "dictionary.com" + "\r\n" + "thesaurus.com" + "\r\n" + "wikipedia.org";

dynamic ie = Activator.CreateInstance(Type.GetTypeFromProgID("InternetExplorer.Application"));
sUrl = Uri.EscapeUriString(sUrl + sWord);
ie.Navigate(sUrl);
while ((int) ie.ReadyState != 4) System.Threading.Thread.Sleep(100);
dynamic doc = ie.Document;
dynamic tables = doc.GetElementByTagName("table");
sText = sText + sDivider + "dictionary.com" + "\r\n" + "\r\n";
foreach (dynamic table in tables) sText = sText + table.InnerText + "\r\n" + "\r\n";
sText = sText.Trim() + "\r\n";
try { doc.Close(); ie.Quit(); Marshal.ReleaseComObject(ie); } catch { }
return sText;
} // LookupTerm method

// GetLinks: legacy link harvest via Internet Explorer automation.
public static List<string[]> GetLinks(string sUrl) {
List<string[]> listLinks = new List<string[]>();
List<string> listRefs = new List<string>();
dynamic ie = Activator.CreateInstance(Type.GetTypeFromProgID("InternetExplorer.Application"));
ie.Visible = false;
ie.Silent = true;
ie.Navigate(sUrl);
while ((int) ie.ReadyState != 4) System.Threading.Thread.Sleep(100);
dynamic doc = ie.Document;
dynamic links = doc.Links;
foreach (dynamic link in links) {
string sRef = link.HRef;
if (sRef.ToLower().StartsWith("mailto:")) continue;
try { Uri uri = new Uri(sRef); } catch { sRef = ""; }
if (sRef.Length == 0 || listRefs.Contains(sRef)) continue;
listRefs.Add(sRef);
listLinks.Add(new string[] {sRef, (string) link.InnerText});
}
try { doc.Close(); ie.Quit(); Marshal.ReleaseComObject(ie); } catch { }
return listLinks;
} // GetLinks method
} // VB class

public class Script {
private static MethodInfo miRun;

private static MethodInfo GetRunMethod() {
if (miRun != null) return miRun;
string sDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
string sDll = Path.Combine(sDir, "EdSharp.dll");
Assembly asmHost = Assembly.LoadFrom(sDll);
Type typeJs = asmHost.GetType("EdSharp.JS");
miRun = typeJs.GetMethod("runScript", new Type[] {typeof(string), typeof(object), typeof(object)});
return miRun;
} // GetRunMethod method

// run: evaluate sCode with the active editor window as frm and its
// RichTextBox as rtb, both visible to the snippet. Either may be null
// for host-internal expressions that need no document context.
public static string run(string sCode) {
object frm = (App.Frame != null) ? App.Frame.Child : null;
object rtb = (App.Frame != null && App.Frame.Child != null) ? (object) App.Frame.Child.RTB : null;
return (string) GetRunMethod().Invoke(null, new object[] {sCode, frm, rtb});
} // run method
} // Script class

public class COM {
public static object CreateObject(string sProgID) {
Type t = Type.GetTypeFromProgID(sProgID);
object oResult = Activator.CreateInstance(t);
return oResult;
} // CreateObject method

public static object GetObject(string sProgID) {
object oResult = Marshal.GetActiveObject(sProgID);
return oResult;
} // GetObject method

public static object GetOrCreateObject(string sProgID, out bool bCreate, string sMessage) {
object oResult;
try {
oResult = GetObject(sProgID);
bCreate = false;
}
catch {
Util.Say(sMessage);
oResult = CreateObject(sProgID);
bCreate = true;
}
return oResult;
} // GetOrCreateObject method

public static object CallMethod(object o, string sMethod) {
object[] args = {};
return CallMethod(o, sMethod, args);
} // CallMethod method

public static object CallMethod(object o, string sMethod, string sValue) {
object[] args = {sValue};
return CallMethod(o, sMethod, args);
} // CallMethod method

public static object CallMethod(object o, string sMethod, int iValue) {
object[] args = {iValue};
return CallMethod(o, sMethod, args);
} // CallMethod method

public static object CallMethod(object o, string sMethod, object[] args) {
Type t = o.GetType();
object oResult = t.InvokeMember(sMethod, BindingFlags.InvokeMethod, null, o, args);
return oResult;
} // CallMethod method

public static object SetProperty(object o, string sProperty, string sValue) {
object[] args = {sValue};
return SetProperty(o, sProperty, args);
} // SetProperty method

public static object SetProperty(object o, string sProperty, int iValue) {
object[] args = {iValue};
return SetProperty(o, sProperty, args);
} // SetProperty method

public static object SetProperty(object o, string sProperty, bool bValue) {
object[] args = {bValue};
return SetProperty(o, sProperty, args);
} // SetProperty method

public static object SetProperty(object o, string sProperty, object[] args) {
Type t = o.GetType();
object oResult = t.InvokeMember(sProperty, BindingFlags.SetProperty, null, o, args);
return oResult;
} // SetProperty method

public static object GetProperty(object o, string sProperty) {
object[] args = new object[] {};
return GetProperty(o, sProperty, args);
} // GetProperty method

public static object GetProperty(object o, string sProperty, object[] args) {
Type t = o.GetType();
object oResult = t.InvokeMember(sProperty, BindingFlags.GetProperty, null, o, args);
return oResult;
} // GetProperty method

public static bool JFWSay(string sText) {
// object oJFW = null;
// return JFWSay(sText, ref oJFW);
return JFWSay(sText, ref App.JAWS);
} // JFWSay method

public static bool JFWSay(string sText, ref object oJFW) {
try {
if (oJFW == null) oJFW = CreateObject("FreedomSci.JawsApi");
// int iResult = (int) CallMethod(oJFW, "SayString", new object[] {sText, 0});
// return iResult == 1;
// Console.Beep();
bool bResult = (bool) CallMethod(oJFW, "SayString", new object[] {sText, false});
return bResult;
}
catch {
return false;
}
} // JFWSay method

// COM.JFWRunFunction USUNIETE razem z golym F9 (5.0.63).  Ta para metod
// wolala po COM funkcje skryptu JAWS po nazwie i jej JEDYNYM konsumentem w
// calym programie bylo czytanie do konca pod F9 - po jego decyzji o rezygnacji
// nie zostaje wiec martwy helper, ktory nastepny czytajacy wzialby za
// dzialajaca droge do JAWS-a (lekcja z 5.0.44: osierocony helper przezyl
// usuniecie rodziny komend i mylil przy kolejnej pracy).
// COM.JFWSay ZOSTAJE swiadomie: nie jest sierota po TEJ zmianie, lezy tam od
// upstreamu jako czesc rodziny kanalow mowy (JFWSay/WESay/SASay) i mowienie
// przez JAWS-a jest zywa droga - Say.cs mowi przez FreedomSci.JawsApi.

public static bool WESay(string sText) {
//object oWE = null;
//return WESay(sText, ref oWE);
return WESay(sText, ref App.Wineyes);
} // WESay method

public static bool WESay(string sText, ref object oWE) {
//if ((int) Win32.FindWindow(0, "Window-Eyes") == 0) return false;
//don't even check since last resort
if (!Win32.IsWinEyesActive()) return false;

try {
if (oWE == null) oWE = CreateObject("GwSpeak.Speak");
CallMethod(oWE, "SpeakString", sText);
// if (oWE == null) oWE = CreateObject("WindowEyes.Application");
// object oSpeech = COM.GetProperty(oWE, "Speech");
// CallMethod(oSpeech, "Speak", sText);
return true;
}
catch {
return false;
}
} // WESay method

public static bool SAPISay(string sText) {
object oSAPI = null;
return SAPISay(sText, oSAPI);
} // SAPISay method

public static bool SAPISay(string sText, object oSAPI) {
try {
if (oSAPI == null) oSAPI = CreateObject("SAPI.SPVoice");
CallMethod(oSAPI, "Speak", sText);
return true;
}
catch {
return false;
}
} // SAPISay method

public static void InvokeVerb(string sPath, string sVerb) {
// Dialog.Show(sPath, sVerb);
object o = COM.CreateObject("Shell.Application");
string sDir = Path.GetDirectoryName(sPath);
string sName = Path.GetFileName(sPath);
o = COM.CallMethod(o, "Namespace", new string[] {sDir});
// o = COM.GetProperty(o, "Self");
o = COM.CallMethod(o, "ParseName", new string[] {sName});
o = COM.CallMethod(o, "InvokeVerb", new string[] {sVerb});
} // InvokeVerb method

public static string[] Verbs(string sPath) {
object o = COM.CreateObject("Shell.Application");
string sDir = Path.GetDirectoryName(sPath);
string sName = Path.GetFileName(sPath);
o = COM.CallMethod(o, "Namespace", new string[] {sDir});
o = COM.CallMethod(o, "ParseName", new string[] {sName});
try {
o = COM.CallMethod(o, "Verbs", new object[] {});
}
catch {
return new string[] {};
}
int iCount = (int) COM.GetProperty(o, "Count");
StringBuilder sb = new StringBuilder();
for (int i = 0; i < iCount; i++) {
object oVerb = COM.CallMethod(o, "Item", new object[] {(int) i});
string sVerb = (string) COM.GetProperty(oVerb, "Name");
if (sVerb.Trim() != "") sb.Append(sVerb + "\n");
}
string[] aVerbs = sb.ToString().Trim().Split('\n');
return aVerbs;
} // Verbs method

public static string ConvertFile2String(string sSource) {
int iConvert = 2;
return ConvertFile2String(sSource, ref iConvert);
} // ConvertFile2String method

public static string ConvertFile2String(string sSource, ref int iConvert) {
string sTargetExt = "txt";
return ConvertFile2String(sSource, ref iConvert, ref sTargetExt);
} // ConvertFile2String method

public static string ConvertFile2String(string sSource, ref int iConvert, ref string sTargetExt) {
bool bTextOnly = false;
return ConvertFile2String(sSource, ref iConvert, ref sTargetExt, bTextOnly);
} // Convert File2String method

public static string ConvertFile2String(string sSource, ref int iConvert, ref string sTargetExt, bool bTextOnly) {
return ConvertFile2String(sSource, ref iConvert, ref sTargetExt, bTextOnly, "");
} // ConvertFile2String method

// sForceImport: klucz tabeli Import wybrany JUZ WCZESNIEJ przez wywolujacego
// (np. "rtf2md", gdy uzytkownik wskazal konwersje do Markdown na liscie
// wariantow otwarcia pliku .rtf).  Pusty napis zachowuje dawne zachowanie,
// czyli pytanie o format tutaj.  Dzieki temu ten sam wybor nie jest zadawany
// dwa razy pod rzad, co przy czytniku ekranu jest szczegolnie meczace.
public static string ConvertFile2String(string sSource, ref int iConvert, ref string sTargetExt, bool bTextOnly, string sForceImport) {
string sText = "";
if (iConvert == 0) sText = Util.File2String(sSource);
else {
//string sTarget = App.TempFile;
string sTarget = Path.GetTempFileName();
App.TempFiles.Add(sTarget);
//sTarget = Win32.GetShortPath(sTarget);
string sResult;
string[] aResults = App.ReadSectionKeys("Import");
HomerList hl = new HomerList(aResults);
hl.AddUniqueRange("rtf|htm|html|xhtml");
hl.ToLower();
string sExt = Path.GetExtension(sSource).ToLower().TrimStart('.');
string sMatch = "^" + sExt + @"(2\w+)?$";
if (bTextOnly) sMatch = "^" + sExt + @"2txt$";
hl.KeepLike(sMatch);
//if (hl.Count > 0) hl.Push(sExt + "2" + sExt);
// do not offer original format, since already available with Control+O
if (hl.Count > 1) hl.Push(sExt + "2" + sExt);
aResults = hl.ToArray();
hl.ReplaceLike("^" + sExt + "$", sExt + "2txt");
hl.ReplaceLike(@"^\w+2", "");
string[] aDisplay = hl.ToArray();
Array.Sort(aDisplay, aResults);
// Solved above instead
// Solve TextConvert with brf
// if (bTextOnly) aResults = new string[] { sExt + "2txt"};
// if (bTextOnly) aResults = new string[] { sExt, sExt + "2txt"};
if (aResults.Length == 0) {
//Dialog.Show("Alert", "No import options for " + sExt);
//return "";
sResult = "";
}
else if (aResults.Length == 1) sResult = aResults[0];
else if (sForceImport.Length > 0) sResult = sForceImport;
else {
//sResult = Dialog.Pick("Import Format", aResults, true, 0);
string sTitle = "Import " + sExt + " to ";
sResult = Dialog.Pick(sTitle, aResults, aDisplay, true, 0);
//Dialog.Show(sResult);
if (sResult.Length == 0) return "";

}
string sTempExt = Util.RegExpReplaceCase(sResult, @"^\w+2", "");
//Dialog.Show(s, sResult);
if (sTempExt != sResult) sTargetExt = sTempExt;
else sResult = sExt;
string sCommand = Ini.ReadValue(App.IniFile, "Import", sResult, "");
if (sCommand.Length > 0) {
// Dialog.Show(sTargetExt, "target extension");
string s = Path.ChangeExtension(sTarget, sTargetExt);
if (!Util.Equiv(sTarget, s)) {
if (File.Exists(s)) File.Delete(s);
System.IO.File.Move(sTarget, s);
sTarget = s;
}

sCommand = Util.ExpandCommandLine(sCommand, sSource, sTarget);
// Dialog.Show(sTarget, "target file");
// Dialog.Show(sCommand);
//Clipboard.SetText(sCommand);
//Clipboard.SetText(sTarget);
App.Frame.AddMessage("Converting");
if (File.Exists(sTarget)) sTarget = Win32.GetShortPath(sTarget);
if (File.Exists(sTarget)) File.Delete(sTarget);
Util.RunHideWait(sCommand);
if (!File.Exists(sTarget)) {
//Util.RunHide(sCommand);
sCommand = "cmd.exe /c " + sCommand;
//Util.RunHide(sCommand);
Util.RunHideWait(sCommand);
/*
int iLoop = 20;
while (iLoop > 0 && !File.Exists(sTarget)) {
System.Threading.Thread.Sleep(100);
iLoop--;
}
*/
}
// Read the converted target. File2String detects its encoding (byte-order
// mark first, then content detection) and decodes it correctly, so the old
// re-encode pass through Convert\EasyEncode\utf8b.exe is no longer needed.
// Dropping it removes that external tool from the conversion path.
if (File.Exists(sTarget)) sText = Util.File2String(sTarget);

if (sText.Length == 0) Dialog.Show("Error", "Command line:\n" + sCommand);
}
else {
if (sTargetExt == sExt) {
sExt = "";
iConvert = 1;
}

switch (sExt) {
case "rtf" :
if (iConvert > 0) iConvert = -1;
break;
//Use OfficeConvert utilities
/*
case "doc" :
case "docx" :
App.Frame.AddMessage("Converting");
sText = WordFile2String(sSource);
break;
case "ppt" :
case "pptx" :
App.Frame.AddMessage("Converting");
VB.Ppt2Txt(sSource, sTarget);
sText = Util.File2String(sTarget);
break;
case "xls" :
case "xlsx" :
App.Frame.AddMessage("Converting");
VB.Xls2Txt(sSource, sTarget);
sText = Util.File2String(sTarget);
break;
*/
default :
// Disable Word conversions of unknown extensions
// if (iConvert == 1) {
if (iConvert != -1) {
sText = Util.File2String(sSource);
iConvert = 0;
}
else {
App.Frame.AddMessage("Converting");
sText = WordFile2String(sSource);
}
break;
}
}
}
App.Frame.Activate();
sText = Util.Convert2UnixLineBreak(sText);
return sText;
} // ConvertFile2String method

public static string WordFile2String(string sSource) {
bool bCreate, bVisible;
int iDisplayAlerts;
bool bAppVisible = false;
//object oApp = COM.GetOrCreateObject("Word.Application", out bCreate);
object oApp = COM.WordAccess(out bCreate);
bVisible = (bool) COM.GetProperty(oApp, "Visible");
iDisplayAlerts = (int) COM.GetProperty(oApp, "DisplayAlerts");
COM.SetProperty(oApp, "Visible", bAppVisible);
COM.SetProperty(oApp, "DisplayAlerts", 0);
object oDocs =COM.GetProperty(oApp, "Documents");
object oDoc = VB.WordOpen(oDocs, sSource, bAppVisible);
string sTarget = Path.GetTempFileName();
if (File.Exists(sTarget)) File.Delete(sTarget);
object oSelection = COM.GetProperty(oApp, "Selection");
int iLength = (int) COM.GetProperty(oSelection, "StoryLength");
COM.CallMethod(oSelection, "SetRange", new object[] {0, iLength});
string sText = (string) COM.GetProperty(oSelection, "Text");
COM.Release(ref oSelection);
sText = sText.Trim();
sText = Util.RegExpReplaceCase(sText, "\r\f", "\f\r");
sText = Util.Convert2UnixLineBreak(sText);
sText = Util.RegExpReplaceCase(sText, MdiFrame.SB, MdiFrame.SectionBreak);
//sText = Util.Convert2WinLineBreak(sText);
Util.String2File(sText, sTarget);
//VB.WordSaveAs(oDoc, sTarget, 2);
VB.WordClose(oDoc);
COM.Release(ref oDoc);
COM.Release(ref oDocs);

if (bCreate) {
//VB.WordQuit(oApp);
}
else {
COM.SetProperty(oApp, "Visible", bVisible);
COM.SetProperty(oApp, "DisplayAlerts", iDisplayAlerts);
}

COM.Release(ref oApp);
if (File.Exists(sTarget)) {
string sReturn = Util.File2String(sTarget);
File.Delete(sTarget);
return sReturn;
}
else return "";
} // WordFile2String();

public static object WordOpen(object oDocs, string sFile, bool bAppVisible) {
bool bConfirmConversions = false;
bool bReadOnly = false;
bool bAddToRecentFiles = false;
object sPasswordDocument = Missing.Value;
object sPasswordTemplate = Missing.Value;
bool bRevert = true;
object sWritePasswordDocument = Missing.Value;
object sWritePasswordTemplate = Missing.Value;
object iFormat = Missing.Value;
object iEncoding = Missing.Value;
bool bVisible = bAppVisible;
object oOpenConflictDocument = Missing.Value;
bool bOpenAndRepair = true;
object iDocumentDirection = Missing.Value;
bool bNoEncodingDialog = true;

object[] oParams = {sFile, bConfirmConversions, bReadOnly, bAddToRecentFiles, sPasswordDocument, sPasswordTemplate, bRevert, sWritePasswordDocument, sWritePasswordTemplate, iFormat, iEncoding, bVisible, oOpenConflictDocument, bOpenAndRepair, iDocumentDirection, bNoEncodingDialog};
oParams = new object[] {sFile, bConfirmConversions, bReadOnly, bAddToRecentFiles};

object oDoc = null;
try {
oDoc = CallMethod(oDocs, "Open", oParams);
}
catch (COMException ex) {
Dialog.Show("Error", ex.Message);
}
return oDoc;
} // OpenWordDocument method

public static void WordSaveAs(object oDoc, string sFile, int iSaveFormat) {
int iFileFormat = iSaveFormat;
bool bLockComments = false;
object oPassword = Type.Missing;
bool bAddToRecentFiles = false;
object oWritePassword = Type.Missing;
bool bReadOnlyRecommended = false;
bool bEmbedTrueTypeFonts = false;
bool bSaveNativePictureFormat = false;
bool bSaveFormsData = false;
bool bSaveAsAOCELetter = false;
object oEncoding= Type.Missing;
bool bInsertLineBreaks = false;
bool bAllowSubstitutions = false;
object sLineEnding = Type.Missing;
bool bAddBiDiMarks = false;
object[] oParams = {sFile, iFileFormat, bLockComments, oPassword, bAddToRecentFiles, oWritePassword, bReadOnlyRecommended, bEmbedTrueTypeFonts, bSaveNativePictureFormat,
bSaveFormsData, bSaveAsAOCELetter, oEncoding, bInsertLineBreaks, bAllowSubstitutions, sLineEnding, bAddBiDiMarks
};
try {
CallMethod(oDoc, "SaveAs", oParams);
}
catch (COMException ex) {
Dialog.Show("Error", ex.Message);
}
} // WordSaveAs method

public static void WordClose(object oDoc) {
object oApp = GetProperty(oDoc, "Application");
ClearNormalTemplate(oApp);

int iSaveChanges = 0;
object iOriginalFormat = Type.Missing;
bool bRouteDocument = false;
object[] oParams = {iSaveChanges, iOriginalFormat, bRouteDocument};
oParams = new object[] {iSaveChanges};

try {
CallMethod(oDoc, "Close", oParams);
}
catch (COMException ex) {
Dialog.Show("Error", ex.Message);
}
} // WordClose method

public static void ClearNormalTemplate(object oApp) {
object oTemplate = COM.GetProperty(oApp, "NormalTemplate");
COM.SetProperty(oTemplate, "Saved", true);
Release(ref oTemplate);
} // ClearNormalTemplate method

public static void WordQuit(object oApp) {
COM.ClearNormalTemplate(oApp);

int iSaveChanges = 0;
object iFormat = Type.Missing;
bool bRouteDocument = false;

object[] oParams = {iSaveChanges, iFormat, bRouteDocument};
oParams = new object[] {iSaveChanges};

try {
CallMethod(oApp, "Quit", oParams);
}
catch (COMException ex) {
Dialog.Show("Error", ex.Message);
}
} // WordQuit method

public static void Release(ref object o) {
Marshal.ReleaseComObject(o);
o = null;
} // Release method

public static object WordAccess(out bool bCreate) {
string sMessage = "Initializing Microsoft Word";
object oApp = GetOrCreateObject("Word.Application", out bCreate, sMessage);
if (bCreate) App.WordCreated = true;
return oApp;
} // WordAccess method

public static void WordExit() {
object oApp = null;
bool bLoop = true;
while (bLoop) {
try {
oApp = GetObject("Word.Application");
//Util.Say("quit");
//WordQuit(oApp);
//break;
VB.WordQuit(oApp);
Release(ref oApp);
}
catch {
break;
}
}
Util.TerminateProcess("WinWord");
} // WordExit method;

public static bool WordSource2TargetFormat(string sSource, string sTarget, string sFormat) {
int iFormat = 2; // text;
if (sFormat == "doc") iFormat = 0;
else if (sFormat == "htm") iFormat = 10;
else if (sFormat == "xml") iFormat = 11;
bool bAppVisible = false;
bool bCreate;
object oApp = COM.WordAccess(out bCreate);
bool bVisible = (bool) COM.GetProperty(oApp, "Visible");
int iDisplayAlerts = (int) COM.GetProperty(oApp, "DisplayAlerts");
COM.SetProperty(oApp, "Visible", bAppVisible);
COM.SetProperty(oApp, "DisplayAlerts", 0);
object oDocs = COM.GetProperty(oApp, "Documents");
object oDoc = VB.WordOpen(oDocs, sSource, bAppVisible);
object oSelection = COM.GetProperty(oApp, "Selection");
//object oRange = COM.GetProperty(oSelection, "Range");
int iLength = (int) COM.GetProperty(oSelection, "StoryLength");
object oRange = COM.CallMethod(oDoc, "Range", new object[] {0, iLength});
COM.CallMethod(oRange, "AutoFormat");
VB.WordSaveAs(oDoc, sTarget, iFormat);
COM.Release(ref oRange);
VB.WordClose(oDoc);
COM.Release(ref oDoc);
COM.Release(ref oDocs);
if (!bCreate) {
COM.SetProperty(oApp, "Visible", bVisible);
COM.SetProperty(oApp, "DisplayAlerts", iDisplayAlerts);
}
COM.Release(ref oApp);
App.Frame.Activate();
return File.Exists(sTarget);
} // WordSource2TargetFormat method

public static string GetUrl() {
string sUrl = "";
try {
object oShell = COM.CreateObject("Shell.Application");
object oWindows = COM.CallMethod(oShell, "Windows");
int iCount = (int) COM.GetProperty(oWindows, "Count");
if (iCount > 0) {
object oWindow = COM.CallMethod(oWindows, "Item", new object[] {iCount - 1});
sUrl = (string) COM.GetProperty(oWindow, "LocationURL");
}
}
catch {}
return sUrl;
} // GetUrl method

public static void ActivateTitle(string sTitle) {
object oShell = CreateObject("WScript.Shell");
CallMethod(oShell, "AppActivate", sTitle);
} // ActivateTitle method

} // COM class

// Inix: app-level accessor for an optional EdSharp.inix layered over the
// classic .ini. Reads are inix-first with .ini fallback (see Ini.ReadValue):
// when no EdSharp.inix exists, or a key is absent from it, lookups miss and the
// classic .ini path runs unchanged, so default behavior is preserved. Built on
// the portable Homer.InixCodec.
public static class Inix {
static List<Homer.InixCodec.Section> lSections = null;
static bool bLoaded = false;

static string getPath() {
string sIni = App.IniFile;
if (String.IsNullOrEmpty(sIni)) return null;
if (sIni.ToLower().EndsWith(".ini")) return sIni.Substring(0, sIni.Length - 4) + ".inix";
return sIni + ".inix";
} // getPath method

static void ensureLoaded() {
if (bLoaded) return;
bLoaded = true;
try {
string sPath = getPath();
if (sPath != null && File.Exists(sPath)) lSections = Homer.InixCodec.read(sPath);
}
catch { lSections = null; }
} // ensureLoaded method

public static bool tryGet(string sSection, string sKey, out string sValue) {
sValue = null;
ensureLoaded();
if (lSections == null) return false;
foreach (Homer.InixCodec.Section sec in lSections) {
if (!Util.Equiv(sec.Name, sSection)) continue;
foreach (Homer.InixCodec.Pair pair in sec.Pairs) if (Util.Equiv(pair.Key, sKey)) { sValue = pair.Value; return true; }
}
return false;
} // tryGet method

// syncWrite: when an EdSharp.inix exists (the user opted into the layer),
// keep it consistent with writes to the main .ini so an override never masks
// a value the app just changed. A no-op when no EdSharp.inix is present, so
// default behavior is unchanged. Guarded and caught; never disturbs the .ini
// write that already happened.
public static void syncWrite(string sFile, string sSection, string sKey, string sValue) {
if (String.IsNullOrEmpty(sFile) || !Util.Equiv(sFile, App.IniFile)) return;
string sPath = getPath();
if (sPath == null || !File.Exists(sPath)) return;
try { Homer.InixCodec.writeValue(sPath, sSection, sKey, sValue); reload(); }
catch {}
} // syncWrite method

public static void reload() { bLoaded = false; lSections = null; } // reload method
} // Inix class

public class Ini {
public static string RedirectFile(string sFile, string sSection) {
// Sekcja Bookmarks idzie tam, gdzie Favorites: do pliku biezacego kompilatora.
// Bez tego wpisu zakladki wyladowalyby w EdSharp.ini, a ulubione i przypisy
// zostalyby w <kompilator>.ini - dwa magazyny o roznym zasiegu, czyli
// zakladki "gubilyby sie" po przelaczeniu kompilatora.
if(Util.Equiv(sFile, App.IniFile) && (Util.Equiv(sSection, "Favorites") || Util.Equiv(sSection, "Bookmarks") || Util.Equiv(sSection, "Recent") || Util.Equiv(sFile, "Tokens"))) sFile = Path.Combine(App.DataDir, App.ReadData("Compiler", "Default") + ".ini");
return sFile;
} // RedirectFile method

[DllImport("kernel32.dll")]
public static extern int GetPrivateProfileString(string sSection, string sKey, string sDefault, StringBuilder sReturnString, int iLength, string sFile);
public static String ReadValue(String sFile, String sSection, String sKey, string sDefault) {
string sInix;
if (Util.Equiv(sFile, App.IniFile) && Inix.tryGet(sSection, sKey, out sInix)) return sInix;
sFile = RedirectFile(sFile, sSection);
StringBuilder sb = new StringBuilder(260);
if (GetPrivateProfileString(sSection, sKey, sDefault, sb, sb.Capacity, sFile) > 0) return sb.ToString();
else return sDefault;
} // ReadValue method

[DllImport("kernel32.dll")]
public static extern bool WritePrivateProfileString(string sSection, string sKey, string sValue, string sFile);
public static bool WriteQuote(String sFile, String sSection, String sKey, String sValue) {
bool bQuote = true;
return WriteValue(sFile, sSection, sKey, sValue, bQuote);
} // WriteQuote method

public static bool WriteValue(String sFile, String sSection, String sKey, String sValue) {
bool bQuote = false;
return WriteValue(sFile, sSection, sKey, sValue, bQuote);
} // WriteValue method

public static bool WriteValue(String sFile, String sSection, String sKey, String sValue, bool bQuote) {
sFile = RedirectFile(sFile, sSection);
string sRaw = sValue;
if (bQuote) sValue = "\"" + sValue + "\"";
bool bReturn = WritePrivateProfileString(sSection, sKey, sValue, sFile);
FlushFile(sFile);
Inix.syncWrite(sFile, sSection, sKey, sRaw);
return bReturn;
} // WriteValue method

[DllImport("kernel32.dll")]
public static extern bool WritePrivateProfileString(string sSection, string sKey, int iValue, string sFile);
public static bool DeleteKey(String sFile, String sSection, String sKey) {
sFile = RedirectFile(sFile, sSection);
int iValue = 0;
bool bReturn = WritePrivateProfileString(sSection, sKey, iValue, sFile);
FlushFile(sFile);
return bReturn;
} // DeleteKey method

[DllImport("kernel32.dll")]
public static extern bool WritePrivateProfileString(string sSection, int iKey, int iValue, string sFile);
public static bool DeleteSection(String sFile, String sSection) {
int iKey = 0;
int iValue = 0;
bool bReturn = WritePrivateProfileString(sSection, iKey, iValue, sFile);
FlushFile(sFile);
return bReturn;
} // DeleteSection method

[DllImport("kernel32.dll")]
public static extern bool WritePrivateProfileString(int iSection, int iKey, int iValue, string sFile);
public static bool FlushFile(String sFile) {
int iSection = 0;
int iKey = 0;
int iValue = 0;
return WritePrivateProfileString(iSection, iKey, iValue, sFile);
} // FlushFile method

public static string[] ReadSectionKeys(string sFile, string sSection) {
bool bIncludeComments = false;
return ReadSectionKeys(sFile, sSection, bIncludeComments);
} // ReadSectionKeys method

public static string[] ReadSectionKeys(string sFile, string sSection, bool bIncludeComments) {
sFile = RedirectFile(sFile, sSection);
string[] aDefault = new string[] {};
if (!File.Exists(sFile)) return aDefault;

string sText = Util.IniFile2String(sFile);
string sMatch = "^\\[" + sSection + "\\](.|\n)*?((\n\\[)|\\Z)";
object[] aResult = Util.RegExpContainsCase(sText, sMatch);
int iIndex = (int) aResult[0];
if (iIndex == -1) return aDefault;

string sValue = (string) aResult[1];
string[] aLines = sValue.Split('\n');
StringBuilder sb = new StringBuilder();
foreach (string sLine in aLines) {
string s = sLine.TrimStart();
if (s.Length == 0 || (!bIncludeComments && s.StartsWith(";")) || s.StartsWith("=") || !s.Contains("=")) continue;
int i = s.IndexOf("=");
string sKey = s.Substring(0, i).TrimEnd();
sb.Append(sKey + "\n");
}

string sKeys = sb.ToString().Trim();
if (sKeys.Length == 0) return aDefault;

string[] aReturn = sKeys.Split('\n');
return aReturn;
} // ReadSectionKeys method

public static string[] ReadSections(string sFile) {
string[] aDefault = new string[] {};
if (!File.Exists(sFile)) return aDefault;

string sText = Util.IniFile2String(sFile);
string sMatch = "^\\[.+?\\]\r\n";
string[] aResults = Util.RegExpExtractCase(sText, sMatch);
string sSections = String.Join("", aResults).Trim();
if (sSections.Length == 0) return aDefault;

sSections = sSections.Replace("[", "").Replace("]", "").Replace("\r", "");
string[] aReturn = sSections.Split('\n');
return aReturn;
} // ReadSections method

} // Ini class

public class Win32 {
// Klawiatura: potrzebne strazniowi pisania (Util.IsTypingChord).  VkKeyScanEx
// odpowiada na pytanie odwrotne do zwyklego - dla ZNAKU zwraca klawisz oraz
// modyfikatory, ktorymi ten znak powstaje.  Dolny bajt to kod klawisza, gorny
// to modyfikatory: bit 0 Shift, bit 1 Control, bit 2 Alt.  Control i Alt
// razem, czyli maska 6, to prawy Alt (AltGr).
[DllImport("user32.dll", CharSet = CharSet.Unicode)]
public static extern short VkKeyScanEx(char ch, IntPtr dwhkl);

[DllImport("user32.dll")]
public static extern IntPtr GetKeyboardLayout(uint idThread);

// Needed before creating the editing control: the modern RichEdit window class
// RICHEDIT50W only exists once msftedit.dll is loaded into the process.
[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
public static extern IntPtr LoadLibrary(string sFile);

[DllImport("user32.dll")]
public static extern int AttachThreadInput(int iThread1, int iThread2, int iAttach);

[DllImport("user32.dll")]
public static extern IntPtr GetActiveWindow();

[DllImport("user32.dll")]
public static extern int BringWindowToTop(IntPtr h);

[DllImport("user32.dll")]
public static extern int ShowWindow(IntPtr h, int iState);

[DllImport("kernel32.dll")]
public static extern int GetCurrentThreadId();

[DllImport("user32.dll")]
public static extern int GetWindowThreadProcessId(IntPtr h, int iProcess);

public static bool ForceWindow(IntPtr h) {
int iForegroundThread = GetWindowThreadProcessId(GetForegroundWindow(), 0);
int iAppThread = GetCurrentThreadId();
if (iForegroundThread == iAppThread) {
BringWindowToTop(h);
ShowWindow(h,3);
}
else {
AttachThreadInput(iForegroundThread, iAppThread, 1);
BringWindowToTop(h);
ShowWindow(h,3);
AttachThreadInput(iForegroundThread, iAppThread, 0);
}

return GetActiveWindow() == h;
} // ForceWindow method

[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
public static extern int GetShortPathName(string path, StringBuilder shortPath, int shortPathLength);
public static string GetShortPath(string sLongPath) {
StringBuilder sbShortPath = new StringBuilder(260);
GetShortPathName(sLongPath, sbShortPath, sbShortPath.Capacity);
string sReturn = sbShortPath.ToString().Trim();
if (sReturn.Length == 0) sReturn = Path.Combine(GetShortPath(Path.GetDirectoryName(sLongPath)), Path.GetFileName(sLongPath));
return sReturn;
} // GetShortPath method

[DllImport("user32.dll")]
static extern bool SystemParametersInfo(int iAction, int iParam, out bool bActive, int iUpdate);
public static bool IsScreenReaderActive() {
int iAction = 70; // SPI_GETSCREENREADER constant;
int iParam = 0;
bool bActive;
int iUpdate = 0;
bool bReturn = SystemParametersInfo(iAction, iParam, out bActive, iUpdate);
return bReturn && bActive;
} // IsScreenReaderActive method

public static bool IsJAWSActive() {
string sClass = "JFWUI2";
string sTitle = "JAWS";
return (int) FindWindow(sClass, sTitle) != 0;
} // IsJAWSActive method

public static bool IsWinEyesActive() {
//string sClass = "AfxFrameOrView42";
//string sTitle = "Window-Eyes";
string sClass = "GWMExternalControl";
string sTitle = "External Control";
return (int) FindWindow(sClass, sTitle) != 0;
//int iClass = 0;
//return (int) FindWindow(iClass, sTitle) != 0;
} // IsWinEyesActive method

[DllImport("jfwapi.dll")]
public static extern int JFWRunFunction(string sText);

public static bool JAWSSay(string sText) {
//if (sText.Length < 2000) return JFWSay(sText);
if (JFWSay(sText)) return true;
//if (!JFWSay("")) return false;
if (!IsJAWSActive()) return false;

Util.String2FileA(sText, App.TempFile);
return JFWRunFunction("SayTempFile") == 1;
} // JAWSSay method

[DllImport("jfwapi.dll")]
public static extern int JFWSayString(string sText, int iInterrupt);

public static bool JFWSay(string sText) {
try {
return JFWSayString(sText, 0) == 1;
}
catch {
return false;
}
} // JFWSay method

[DllImport("nvdaControllerClient32.dll", CharSet = CharSet.Auto)]
public static extern int nvdaController_testIfRunning();

public static bool IsNVDAActive() {
return nvdaController_testIfRunning() == 0;
} // IsNVDAActive method

[DllImport("nvdaControllerClient32.dll", CharSet = CharSet.Auto)]
public static extern int nvdaController_speakText(string sText);

public static bool NVDASay(string sText) {
return nvdaController_speakText(sText) == 0;
} // NVDASay method

[DllImport("nvdaControllerClient32.dll", CharSet = CharSet.Auto)]
public static extern int nvdaController_cancelSpeech();

public static bool NVDACancelSpeech() {
return nvdaController_cancelSpeech() == 0;
} // NVDACancelSpeech method

[DllImport("nvdaControllerClient32.dll", CharSet = CharSet.Auto)]
public static extern int nvdaController_brailleMessage(string sText);

public static bool NVDABraille(string sText) {
return nvdaController_brailleMessage(sText) == 0;
} // NVDASay method

[DllImport("saapi32.dll")]
public static extern int SA_IsRunning();

public static bool IsSAActive() {
try {
return SA_IsRunning() == 1;
}
catch {
return false;
}
} // IsSAActive method

[DllImport("saapi32.dll")]
public static extern int SA_SayU(string sText);

public static bool SASay(string sText) {
try {
return SA_SayU(sText) == 1;
}
catch {
return false;
}
} // SASay method

[DllImport("user32.dll")]
public static extern int SendMessage(IntPtr h, int iMsg, int wParam, int lParam);

[DllImport("user32.dll")]
public static extern IntPtr GetForegroundWindow();

[DllImport("user32.dll")]
public static extern int SetForegroundWindow(IntPtr h);

[DllImport("user32.dll")]
public static extern IntPtr FindWindow(string sClass, string sTitle);

[DllImport("user32.dll")]
public static extern IntPtr FindWindow(int iClass, string sTitle);

[DllImport("user32.dll")]
public static extern IntPtr FindWindow(string sClass, int iTitle);

[DllImport("shell32.dll")]
public static extern int ShellExecute(int i1, string sVerb, string sFile, int i2, int i3, int i4);

public static int ShellExecute(string sVerb, string sFile) {
return ShellExecute(0, sVerb, sFile, 0, 0, 1);
} // ShellExecute method

[DllImport("shell32.dll")]
public static extern int ShellExecute(int i1, int i2, string sFile, int i3, int i4, int i5);

public static int ShellDefault(string sFile) {
return ShellExecute(0, 0, sFile, 0, 0, 1);
} // ShellDefault method

[DllImport("MSCorEE.dll", CharSet = CharSet.Auto)]
public static extern int GetCORSystemDirectory  (StringBuilder sbPath, int iSize, out int iLength);
public static string GetNetSdkDir() {
int iSize = 260;
StringBuilder sbPath = new StringBuilder(iSize);
int iLength;
GetCORSystemDirectory  (sbPath, iSize, out iLength);
return sbPath.ToString();
} // GetNetSdkDir method

[DllImport("MSCorEE.dll", CharSet = CharSet.Auto)]
// public static extern int GetRuntimeDirectory  (StringBuilder sbPath, int iSize, out int iLength);
public static extern int GetRuntimeDirectory  (StringBuilder sbPath, out int iLength);
public static string GetNetRuntimeDir() {
int iSize = 260;
StringBuilder sbPath = new StringBuilder(iSize);
// int iLength;
int iLength = 260;
// GetRuntimeDirectory  (sbPath, iSize, out iLength);
GetRuntimeDirectory  (sbPath, out iLength);
return sbPath.ToString();
} // GetNetRuntimeDir method


public static string GetJFWDir() {
RegistryKey key = Registry.LocalMachine;
string sSubKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\";
string sName = "Path";
string sPath = GetRegString(key, (sSubKey + "jfw.exe"), sName);

if (sPath == "") {
string[] sVersions = {"12", "11", "10", "90", "81", "80", "8", "71", "70", "7", "62", "61", "60", "6"};
sName = "";
foreach (string sVersion in sVersions) {
sPath = GetRegString(key, (sSubKey + "jaws" + sVersion + ".exe"), sName);
if (sPath != "") {
sPath = Path.GetDirectoryName(sPath);
break;
}
}
}
//if (sPath !="" && !sPath.EndsWith(@"\")) sPath = String.Concat(sPath, @"\");
sPath = sPath.TrimEnd('\\');
return sPath;
} // GetJFWDir method

public static string GetWEDir() {
RegistryKey key = Registry.LocalMachine;
string sSubKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\WinEyes.exe";
string sName = "Path";
string sPath = GetRegString(key, sSubKey, sName);
if (sPath !="" && !sPath.EndsWith(@"\")) sPath = String.Concat(sPath, @"\");
return sPath;
} // GetWEDir method

public static string GetRegString(RegistryKey key, string sSubKey, string sName) {
RegistryKey subkey = null;
string sData = "";

try {
subkey = key.OpenSubKey(sSubKey);
sData = subkey.GetValue(sName).ToString();
}
catch {
}
finally {
if (subkey != null) subkey.Close();
}
return sData;
} // GetRegString method

[DllImport("urlmon.dll")]
public static extern int URLDownloadToFile(int i1, string sUrl, string sFile, int i2, int i3, int i4);

public static bool Url2File(string sUrl, string sFile) {
int iResult = URLDownloadToFile(0, sUrl, sFile, 0, 0, 0);
return iResult == 0;
} // Url2File method

[Serializable]
public struct ShellExecuteInfo {
public int Size;
public uint Mask;
public IntPtr hwnd;
public string Verb;
public string File;
public string Parameters;
public string Directory;
public uint Show;
public IntPtr InstApp;
public IntPtr IDList;
public string Class;
public IntPtr hkeyClass;
public uint HotKey;
public IntPtr Icon;
public IntPtr Monitor;
}

[DllImport("shell32.dll", SetLastError = true)]
extern public static bool ShellExecuteEx(ref ShellExecuteInfo lpExecInfo);

public const uint SW_NORMAL = 1;
public const uint SEE_MASK_INVOKEIDLIST = 0x0000000C;
public const uint OAIF_ALLOW_REGISTRATION = 0x00000001;
public const uint OAIF_EXEC = 0x00000004;

public static void OpenWith(string file) {
OpenWith(file, IntPtr.Zero);
} //OpenWith method

public static void OpenWith(string file, IntPtr hParent) {
if (String.IsNullOrEmpty(file)) throw new ArgumentException("No file was specified.", "file");

// Primary path: classic "Open with" dialog via ShellExecuteEx verb "openas".
// This is the dialog that offers "Always use this app to open ..." so the user
// can make the association permanent. SHOpenWithDialog (below) cannot do that
// on Windows 10/11 - it only opens the file once and ignores registration flags.
try {
ShellExecuteInfo sei = new ShellExecuteInfo();
sei.Size = Marshal.SizeOf(sei);
sei.Mask = SEE_MASK_INVOKEIDLIST;
sei.hwnd = hParent;
sei.Verb = "openas";
sei.File = file;
sei.Show = SW_NORMAL;
if (ShellExecuteEx(ref sei)) return;
}
catch (EntryPointNotFoundException) {}
catch (DllNotFoundException) {}

// Fallback: SHOpenWithDialog (single open only on Win10/11, no "always").
OpenAsInfo info = new OpenAsInfo();
info.pcszFile = file;
info.pcszClass = null;
info.oaifInFlags = OAIF_ALLOW_REGISTRATION | OAIF_EXEC;

int iResult = SHOpenWithDialog(hParent, ref info);
if (iResult >= 0) return;
if (iResult == unchecked((int) 0x800704C7)) return; // user cancelled
Marshal.ThrowExceptionForHR(iResult);
} //OpenWith method

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct OpenAsInfo {
[MarshalAs(UnmanagedType.LPWStr)]
public string pcszFile;
[MarshalAs(UnmanagedType.LPWStr)]
public string pcszClass;
public uint oaifInFlags;
}

[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
public static extern int SHOpenWithDialog(IntPtr hwndParent, ref OpenAsInfo poainfo);

} // Win32 class

// KODOWANIE MAZOVIA (strona kodowa 667), DOLOZONE 12.09.2026.
// Zadanie 9 z listy Kasperczaka: "Mazovia, Latin II (CP852), Windows-1250.
// Wczytanie + automatyczna konwersja do UTF-8".
//
// DLACZEGO WLASNA KLASA, A NIE Encoding.GetEncoding: .NET NIE ZNA Mazovii.
// Windows-1250 (strona 1250) i Latin II (strona 852) sa w systemie i wystarczy
// je zawolac po numerze - Mazovii nie ma tam wcale, wiec tablica musi byc
// nasza.  To NIE jest zgadywanie: Mazovia to strona 437 z siedemnastoma
// pozycjami podmienionymi na polskie litery, a pozycje te maja ustalone wartosci
// (Wikipedia "Mazovia encoding", tablica strony 667; ten sam uklad w justapedia
// i w opisie konwertera PLC Gryszkalisa).
//
// GORNA POLOWA BIERZE SIE ZE STRONY 437 W CZASIE DZIALANIA, nie z przepisanej
// recznie listy 128 znakow: ramki i znaki matematyczne sa w Mazovii DOKLADNIE
// takie jak w 437 (to byl caly sens tego kodowania - Norton Commander mial
// rysowac ramki poprawnie), a przepisywanie ich z palca to 128 okazji na
// literowke, ktorej nikt nie zauwazy.  Podmieniamy tylko te 17 pozycji, ktore
// FAKTYCZNIE sie roznia (wszystkie w zakresie 0x86-0xA7).
//
// ZAPIS: dokument wczytany jako Mazovia zapisuje sie w UTF-8 - robi to
// GetSaveEncoding, bo 667 nie jest na liscie kodowan zostawianych w spokoju.
// O to wlasnie chodzilo w zadaniu: plik raz otwarty w naszym edytorze przestaje
// byc pulapka na polskie litery.
public class MazoviaEncoding : Encoding {
// Znaki dla bajtow 0x80-0xFF.  Indeks 0 to bajt 0x80.
private static char[] aMap = null;
private static Dictionary<char, byte> dBack = null;

// Pozycje, w ktorych Mazovia rozni sie od strony kodowej 437.
// Bajt, potem znak Unicode.
private static int[] aDiffByte = new int[] {
0x86, 0x8D, 0x8F, 0x90, 0x91, 0x92, 0x95, 0x98,
0x9C, 0x9E, 0xA0, 0xA1, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7
};
private static char[] aDiffChar = new char[] {
'\u0105', // 86 a z ogonkiem
'\u0107', // 8D c z kreska
'\u0104', // 8F A z ogonkiem
'\u0118', // 90 E z ogonkiem
'\u0119', // 91 e z ogonkiem
'\u0142', // 92 l z kreska
'\u0106', // 95 C z kreska
'\u015A', // 98 S z kreska
'\u0141', // 9C L z kreska
'\u015B', // 9E s z kreska
'\u0179', // A0 Z z kreska
'\u017B', // A1 Z z kropka
'\u00D3', // A3 O z kreska
'\u0144', // A4 n z kreska
'\u0143', // A5 N z kreska
'\u017A', // A6 z z kreska
'\u017C'  // A7 z z kropka
};

private static void build() {
if (aMap != null) return;
char[] a = new char[128];
// Podstawa: strona kodowa 437.  Gdyby jej w systemie nie bylo (co sie nie
// zdarza na Windowsie, ale kod ma nie wybuchac), zostaja znaki zapytania -
// polskie litery i tak beda poprawne, bo ida z podmiany ponizej.
try {
Encoding en437 = Encoding.GetEncoding(437);
byte[] aBytes = new byte[128];
for (int i = 0; i < 128; i++) aBytes[i] = (byte)(128 + i);
string s = en437.GetString(aBytes);
for (int i = 0; i < 128 && i < s.Length; i++) a[i] = s[i];
}
catch {
for (int i = 0; i < 128; i++) a[i] = '?';
}
for (int i = 0; i < aDiffByte.Length; i++) a[aDiffByte[i] - 128] = aDiffChar[i];
Dictionary<char, byte> d = new Dictionary<char, byte>();
for (int i = 0; i < 128; i++) if (!d.ContainsKey(a[i])) d[a[i]] = (byte)(128 + i);
dBack = d;
aMap = a;
} // build method

public MazoviaEncoding() { build(); }

public override int CodePage { get { return 667; } }
public override string EncodingName { get { return "Polish (Mazovia)"; } }
public override string WebName { get { return "cp667"; } }
public override bool IsSingleByte { get { return true; } }

public override int GetByteCount(char[] aChars, int iIndex, int iCount) { return iCount; }
public override int GetCharCount(byte[] aBytes, int iIndex, int iCount) { return iCount; }
public override int GetMaxByteCount(int iCharCount) { return iCharCount; }
public override int GetMaxCharCount(int iByteCount) { return iByteCount; }

public override int GetBytes(char[] aChars, int iCharIndex, int iCharCount, byte[] aBytes, int iByteIndex) {
build();
for (int i = 0; i < iCharCount; i++) {
char c = aChars[iCharIndex + i];
byte b;
if (c < 128) b = (byte)c;
else if (dBack.TryGetValue(c, out b)) {}
// Znak, ktorego w Mazovii NIE MA, idzie jako pytajnik - tak samo jak w
// kazdym jednobajtowym kodowaniu .NET-u.  Zapis do Mazovii i tak nie jest
// nasza droga wyjscia (zapisujemy w UTF-8), ale kodowanie musi dzialac w
// obie strony, bo .NET wola GetBytes np. przy liczeniu dlugosci.
else b = (byte)'?';
aBytes[iByteIndex + i] = b;
}
return iCharCount;
} // GetBytes method

public override int GetChars(byte[] aBytes, int iByteIndex, int iByteCount, char[] aChars, int iCharIndex) {
build();
for (int i = 0; i < iByteCount; i++) {
byte b = aBytes[iByteIndex + i];
aChars[iCharIndex + i] = (b < 128) ? (char)b : aMap[b - 128];
}
return iByteCount;
} // GetChars method
} // MazoviaEncoding class

public class Util {

public static string GetPortableExecutableKind() {
PortableExecutableKinds peKind  ;
ImageFileMachine machine  ;

// Module module = App.Shell.GetType().Module;
Module module = Assembly.GetExecutingAssembly().ManifestModule;
module.GetPEKind(out peKind, out machine);

if ((peKind & PortableExecutableKinds.ILOnly) != 0) // Assembly is platform independent.
{}
else { // assembly is platform dependent
switch (machine) {
case ImageFileMachine.I386: // i386, x86, IA-32, ... dependent.
break;
case ImageFileMachine.IA64: // IA-64 dependent.
break;
case ImageFileMachine.AMD64: // AMD-64, x64 dependent.
break;
} // switch
} // if

Dictionary<string, string> dFlags = new Dictionary<string, string>();
dFlags.Add("NotAPortableExecutableImage", "The file is not in portable executable (PE) file format.");
dFlags.Add("ILOnly", "The executable contains only Microsoft intermediate language (MSIL).");
dFlags.Add("Required32Bit", "The executable can be run on a 32-bit platform, or in the 32-bit Windows on Windows (WOW) environment on a 64-bit platform.");
dFlags.Add("PE32Plus", "The executable requires a 64-bit platform.");
dFlags.Add("Unmanaged32Bit", "The executable contains pure unmanaged code.");
dFlags.Add("I386", "Targets a 32-bit Intel processor.");
dFlags.Add("IA64", "Targets a 64-bit Intel processor.");
dFlags.Add("AMD64", "Targets a 64-bit AMD processor.");
string sReturn = "";
string sPEKind = peKind.ToString();
string[] aPEKind = sPEKind.Split(',');
string sMachine = machine.ToString();
foreach (string s in aPEKind) {
sPEKind = s.Trim();
if (dFlags.ContainsKey(sPEKind)) sReturn += dFlags[sPEKind] + "\n\n";
else sReturn += sPEKind + "\n\n";
} // foreach

// Not useful info
// if (dFlags.ContainsKey(sMachine)) sReturn += dFlags[sMachine] + "\n\n";
// else sReturn += sMachine + "\n\n";
sReturn += "Running in " + (IntPtr.Size == 8 ? "64" : "32") + "-bit mode.";
// sReturn = sReturn.Replace("\nTargets a ", "\nRunning on a ");
// Dialog.Show("Portable Executable Kind", sReturn);
return sReturn;
} // GetPortableExecutableKind method

public static string GetBomStringFromBytes(byte[] aBom) {
string sReturn = "";
foreach (byte b in aBom) {
if (sReturn.Length > 0) sReturn += "|";
sReturn += b;
}
return sReturn;
} // GetBomStringFromBytes method

public static string GetBomStringFromFile(string sFile) {
FileStream file = new FileStream(sFile, FileMode.Open, FileAccess.Read, FileShare.Read);
byte[] aBom = new byte[4];
int iCount = file.Read(aBom, 0, 4);
file.Close();
byte[] aReturn = new byte[iCount];
for (int i = 0; i < iCount; i++) aReturn[i] = aBom[i];
return GetBomStringFromBytes(aReturn);
} // GetBom method

public static Dictionary<string, int> GetBomDictionary() {
Dictionary<string, int> dCodes = new Dictionary<string, int>();
Dictionary<string, int> dBoms = new Dictionary<string, int>();
dCodes.Add("Unicode (Big-Endian)", 1201);
dCodes.Add("Unicode (UTF-32 Big-Endian)", 12001);
dCodes.Add("Unicode (UTF-32)", 12000);
// dCodes.Add("Unicode (UTF-7)", 65000);
dCodes.Add("Unicode (UTF-8)", 65001);
dCodes.Add("Unicode", 1200);

string sBody = "";
foreach (string sKey in dCodes.Keys) {
int iValue = dCodes[sKey];
Encoding en = Encoding.GetEncoding(iValue);
string sFile = Path.GetTempFileName();
File.WriteAllText(sFile, sBody, en);

string sBom = GetBomStringFromFile(sFile);
// MessageBox.Show(en.EncodingName, sBom);
// if (dBoms.ContainsKey(sBom)) MessageBox.Show(en.EncodingName, Encoding.GetEncoding(dBoms[sBom]).EncodingName);
dBoms.Add(sBom, iValue);
File.Delete(sFile);
}
return dBoms;
} // GetBomDictionary method

public static Encoding GetFileEncoding(string sFile) {
Dictionary<string, int> dBom = GetBomDictionary();
return GetFileEncoding(sFile, dBom);
} // GetFileEncoding method

public static Encoding GetFileEncoding(string sFile, Dictionary<string, int> dBom) {
// A byte-order mark is definitive, so it wins. Without one, fall back to
// content detection (DetectEncodingNoBom), which prefers UTF-8 with BOM.
string sBom = GetBomStringFromFile(sFile);
Encoding en = null;
foreach (string s in dBom.Keys) {
if (sBom.StartsWith(s)) {
en = Encoding.GetEncoding(dBom[s]);
break;
}
}
if (en == null) en = DetectEncodingNoBom(sFile);
return en;
} // GetFileEncoding method

// Rodzaj koncow wiersza w tekscie: "Windows", "Unix", "Macintosh", "mixed"
// albo pusto, gdy tekst nie ma ani jednego zlamania wiersza.
//
// Sluzy do POWIEDZENIA uzytkownikowi, co ma w pliku (Alt+Z) - nie do decyzji o
// zapisie.  Liczy pary, nie zgaduje: CRLF jest zliczany jako jedno zlamanie i
// NIE moze byc policzony jako Unix, mimo ze zawiera LF.  Wiecej niz jeden
// rodzaj naraz to "mixed" - taki plik istnieje realnie (sklejony z dwoch zrodel)
// i milczenie o tym byloby myleniem uzytkownika.
public static string DetectLineBreakKind(string sText) {
if (sText == null || sText.Length == 0) return "";
int iCrLf = 0, iLf = 0, iCr = 0;
for (int i = 0; i < sText.Length; i++) {
char c = sText[i];
if (c == '\r') {
if (i + 1 < sText.Length && sText[i + 1] == '\n') {iCrLf++; i++;}
else iCr++;
}
else if (c == '\n') iLf++;
}
int iKinds = (iCrLf > 0 ? 1 : 0) + (iLf > 0 ? 1 : 0) + (iCr > 0 ? 1 : 0);
if (iKinds == 0) return "";
if (iKinds > 1) return "mixed";
if (iCrLf > 0) return "Windows";
if (iLf > 0) return "Unix";
return "Macintosh";
} // DetectLineBreakKind method

// Kodowanie, ktorym zapisujemy dokument, na podstawie tego, czym zostal
// odczytany.  Decyzja Kasperczaka (28.08.2026): "program powinien otwierac
// polskie literki zawsze konwertowac do UTF (...) Dobrze, czyli UTF-8".
//
// Czyli plik w STAREJ jednobajtowej stronie kodowej (windows-1250 i kazda inna
// ANSI) po zapisaniu ma byc UTF-8 - raz otwarty w naszym edytorze przestaje byc
// pulapka na polskie litery.  Konwersja jest BEZPIECZNA w jedna strone: UTF-8
// obejmuje kazdy znak, ktory dalo sie zapisac w stronie kodowej, wiec nic nie
// ginie.  Odwrotnie NIE jest prawda, dlatego nie ruszamy kodowan szerokich:
// UTF-16 i UTF-32 zostaja swoje, bo user wybral je swiadomie i zamiana zmienila
// by rozmiar pliku, ktory ktos moze czytac innym programem.
public static Encoding GetSaveEncoding(Encoding enRead) {
if (enRead == null) return null;
int iCode = enRead.CodePage;
// Szerokie i UTF-owe zostaja bez zmian: 1200 utf16le, 1201 utf16be,
// 12000/12001 utf32, 65001 utf8, 65000 utf7.
if (iCode == 1200 || iCode == 1201 || iCode == 12000 || iCode == 12001
    || iCode == 65000 || iCode == 65001) return enRead;
// Wszystko pozostale to stara strona kodowa - idzie na UTF-8 z BOM, czyli
// domyslny format zapisu EdSharpa (utf8b).
return new UTF8Encoding(true);
} // GetSaveEncoding method

public static bool IsStrictUtf8(byte[] aBytes) {
// True tylko wtedy, gdy CALA zawartosc jest poprawnym UTF-8. Strict znaczy
// bez cichego podstawiania znaku zastepczego: UTF8Encoding(false, true) rzuca
// wyjatkiem na pierwszym niedozwolonym bajcie, zamiast wstawiac U+FFFD.
// Uzywane przy wykrywaniu kodowania, gdzie "nie jest UTF-8" jest dowodem, a
// nie domyslem - patrz komentarz w DetectEncodingNoBom.
if (aBytes == null || aBytes.Length == 0) return true;
try {
new UTF8Encoding(false, true).GetString(aBytes);
return true;
}
catch (DecoderFallbackException) { return false; }
catch (ArgumentException) { return false; }
} // IsStrictUtf8 method

public static Encoding DetectEncodingNoBom(string sFile) {
// Content-based detection for a file with no byte-order mark. EdSharp's
// default for text is UTF-8 with BOM ("utf8b"), so pure-ASCII, BOM-less
// UTF-8, undetectable, or empty content all resolve to utf8b. A clearly
// detected legacy or wide encoding (windows-1252, UTF-16 without BOM,
// Shift-JIS, ...) is honored so the file is read -- and later saved --
// without corruption. Detection uses the Ude charset detector when the
// Ude.dll library is present at build time (the HAVEUDE symbol); without
// it, detection degrades to the utf8b default.
Encoding enUtf8b = new UTF8Encoding(true);
#if HAVEUDE
try {
byte[] aBytes = System.IO.File.ReadAllBytes(sFile);
if (aBytes.Length == 0) return enUtf8b;
Ude.CharsetDetector charsetDetector = new Ude.CharsetDetector();
charsetDetector.Feed(aBytes, 0, aBytes.Length);
charsetDetector.DataEnd();
string sCharset = charsetDetector.Charset;
// POLSKIE LITERY W PLIKU ANSI (punkt 3 mapy drogowej, zmierzone 28.08.2026).
// Ude nie rozpoznaje polskiego windows-1250: na probce "Zazolc gesla jazn" z
// ogonkami zwraca Charset = null z pewnoscia 0. Kod spadal wtedy na utf8b i
// czytal plik jako UTF-8, co zamienialo KAZDA polska litere na znak
// zastepczy U+FFFD. Utrata jest BEZPOWROTNA: bajt zostal podmieniony w
// pamieci, wiec ani Yield Encoding, ani ponowny zapis nie maja czego
// odzyskac - a zapis utrwala uszkodzenie na dysku.
// To nie zgadywanie, a arytmetyka, ta sama co przy UTF-16 nizej: bajt
// niedozwolony w UTF-8 DOWODZI, ze plik UTF-8 nie jest, wiec czytanie go tak
// gwarantuje utrate znakow. Bez rozstrzygniecia detektora bierzemy systemowa
// strone kodowa ANSI - to zreszta zachowanie, ktore podrecznik opisuje dla
// pustego ustawienia YieldEncoding ("the default encoding configured in the
// regional settings applet of Windows Control Panel").
// Plik, ktory JEST poprawnym UTF-8, nadal idzie na utf8b; czysty ASCII
// takze, bo kazdy bajt ASCII jest poprawnym UTF-8.
if (String.IsNullOrEmpty(sCharset)) {
if (!IsStrictUtf8(aBytes)) return PickPolishLegacyEncoding(aBytes);
return enUtf8b;
}
Encoding enDetected = CharsetName2Encoding(sCharset, enUtf8b);
// A sanity check on the heuristic, taken from the author's tree after
// Scott's plain-text conversion of 25 August 2026: the detector reported
// UTF-16 for ordinary single-byte text with no byte-order mark, and
// reading it that way fused every two letters into one far-eastern
// character. Detection is guesswork, but this part is arithmetic --
// every Latin letter in UTF-16 carries a zero byte, so content with no
// zero bytes CANNOT be UTF-16 or UTF-32, whatever the detector says.
// When the two disagree, the arithmetic wins.
if (enDetected == Encoding.Unicode || enDetected == Encoding.BigEndianUnicode || enDetected == Encoding.UTF32) {
int iSample = Math.Min(aBytes.Length, 4096);
bool bAnyZero = false;
for (int i = 0; i < iSample; i++) if (aBytes[i] == 0) { bAnyZero = true; break; }
if (!bAnyZero) return enUtf8b;
}
return enDetected;
}
catch { return enUtf8b; }
#else
return enUtf8b;
#endif
} // DetectEncodingNoBom method

// KTORE ZE STARYCH POLSKICH KODOWAN (zadanie 9, 12.09.2026).
//
// Wywolywane, gdy o pliku wiemy JEDNO: nie jest poprawnym UTF-8 (a to dowod, a
// nie domysl - patrz IsStrictUtf8), i detektor Ude nie umial go nazwac.  Do tej
// pory brano wtedy systemowa strone ANSI, czyli na polskim Windowsie 1250.  To
// dobra odpowiedz dla pliku z Windowsa i ZLA dla pliku z DOS-u: tekst w Mazovii
// albo Latin II czytany jako 1250 daje polskie litery zamienione na przypadkowe
// znaki, a po zapisaniu utrwala to na dysku.
//
// CZEMU DA SIE TO ROZSTRZYGNAC, A NIE TYLKO ZGADNAC: te trzy kodowania
// UMIESZCZAJA polskie litery w ROZNYCH miejscach.  Zliczamy wiec, ile bajtow
// pliku wypada na pozycje, gdzie dane kodowanie ma polska litere, i ile na
// pozycje, gdzie ma znak, ktory w polskim tekscie nie ma czego szukac (ramki,
// znaki matematyczne, litery obcych alfabetow).  Wygrywa kodowanie z najlepszym
// bilansem.  To ta sama arytmetyka, ktora wyzej odrzuca falszywe UTF-16.
//
// REMIS ROZSTRZYGA SIE NA KORZYSC WINDOWS-1250, bo tak bylo do tej pory i tak
// wyglada wiekszosc plikow, ktore trafiaja do edytora dzisiaj.  Zmiana nie moze
// pogorszyc przypadku, ktory dzialal.
//
// CZEGO TU NIE MA: rozpoznawania po slowach ("czy tekst wyglada po polsku").
// Kusi, ale plik z jednym polskim slowem na strone byloby wtedy loteria, a
// bilans bajtow dziala tak samo na kazdej dlugosci.
public static Encoding PickPolishLegacyEncoding(byte[] aBytes) {
Encoding enAnsi = Encoding.Default;
try { enAnsi = Encoding.GetEncoding(1250); } catch {}
if (aBytes == null || aBytes.Length == 0) return enAnsi;

// Kandydaci: windows-1250, Latin II (DOS 852), Mazovia (667).
Encoding[] aTry = new Encoding[3];
aTry[0] = enAnsi;
try { aTry[1] = Encoding.GetEncoding(852); } catch { aTry[1] = null; }
aTry[2] = new MazoviaEncoding();

// Litery polskiego alfabetu z ogonkami, male i wielkie.
string sPolish = "\u0105\u0107\u0119\u0142\u0144\u00F3\u015B\u017A\u017C"
               + "\u0104\u0106\u0118\u0141\u0143\u00D3\u015A\u0179\u017B";
// Litery obce, ktore w polskim tekscie zdarzaja sie NAPRAWDE (nazwy wlasne,
// cytaty): za nie nie karzemy, ale tez nie nagradzamy.
string sTolerated = "\u00E4\u00F6\u00FC\u00DF\u00E9\u00E8\u00EA\u00E0\u00E2\u00E7\u00F1\u00C4\u00D6\u00DC\u00C9\u00C7";

int iBestScore = Int32.MinValue;
Encoding enBest = enAnsi;
// Liczymy na probce - poczatek pliku wystarcza, a duzy plik nie ma zmuszac
// uzytkownika do czekania na otwarcie.
int iSample = Math.Min(aBytes.Length, 65536);

for (int iCand = 0; iCand < aTry.Length; iCand++) {
Encoding en = aTry[iCand];
if (en == null) continue;
int iScore = 0;
for (int i = 0; i < iSample; i++) {
byte b = aBytes[i];
if (b < 128) continue;
string s;
try { s = en.GetString(new byte[] { b }); }
catch { continue; }
if (s.Length == 0) continue;
char c = s[0];
if (sPolish.IndexOf(c) >= 0) iScore += 3;
else if (sTolerated.IndexOf(c) >= 0) {}
// RAMKI I BLOKI TO SYGNAL DOS-U, NIE BLAD - i to poprawka po pomiarze
// (testy/pomiar_kodowania_polskie.cs, przypadek "tabelka DOS z ramkami").
// Poprzednia wersja karala ramki, wiec tabelka DOS-owa - a takie wlasnie sa
// stare polskie pliki z ramkami - przegrywala z windows-1250, mimo ze w 1250
// te same bajty dawaly czeskie i wegierskie litery, ktorych w polskim
// tekscie nie ma.  Ramka jest DOWODEM na strone DOS-owa: windows-1250 nie ma
// ich w ogole, wiec plik z ramkami nie moze byc w 1250.
else if (c >= '\u2500' && c <= '\u259F') iScore += 1;
// Litera lacinska, ktorej w polskim ani w typowych cytatach nie ma (czeskie
// r z haczkiem, wegierskie o z dwoma kreskami itd.): to najczystszy sygnal
// zlej strony kodowej, bo wlasnie na te litery rozsypuje sie polski tekst
// czytany nie tym kodowaniem, ktorym go zapisano.
else if (Char.IsLetter(c)) iScore -= 2;
else if (c >= '\u0370' && c <= '\u03FF') iScore -= 2;
else if (c == '\uFFFD') iScore -= 3;
}
// Remis: pierwszy kandydat (windows-1250) zostaje.
if (iScore > iBestScore) { iBestScore = iScore; enBest = en; }
}
return enBest;
} // PickPolishLegacyEncoding method

public static Encoding CharsetName2Encoding(string sName, Encoding enDefault) {
// Map a detector charset name to a .NET Encoding. ASCII and BOM-less UTF-8
// fold to the caller's default (utf8b) for the certainty of a BOM; UTF-16
// and UTF-32 map to their .NET encodings; anything else is looked up by
// name so legacy code pages round-trip.
string sKey = sName.Trim().Replace("-", "").Replace("_", "").ToLower();
if (sKey == "ascii" || sKey == "usascii" || sKey == "utf8") return enDefault;
if (sKey == "utf16le" || sKey == "utf16" || sKey == "unicode") return Encoding.Unicode;
if (sKey == "utf16be") return Encoding.BigEndianUnicode;
if (sKey == "utf32" || sKey == "utf32le") return Encoding.UTF32;
try { return Encoding.GetEncoding(sName); }
catch { return enDefault; }
} // CharsetName2Encoding method

public static string GetProgramBuildDate() {
// DATA WYDANIA Z PLIKU PROGRAMU, nie ze stalego napisu (06.09.2026).  Okno About
// pokazywalo date wpisana recznie, wiec przy kazdej kolejnej paczce byla
// nieprawdziwa.  Bierzemy czas zapisu samej binarki: to jedyna data, ktora
// zmienia sie razem z tym, co user faktycznie uruchomil.  Format dlugi
// miesiacem slownie, bo czytnik ekranu wymawia go jednoznacznie, inaczej niz
// zapis cyframi (05.09 to piaty wrzesnia albo dziewiaty maja).
try {
string sExe = Assembly.GetExecutingAssembly().Location;
if (sExe.Length > 0 && File.Exists(sExe))
return File.GetLastWriteTime(sExe).ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
}
catch {}
return "";
} // GetProgramBuildDate method

public static string FetchLatestReleaseTag(string sOwnerRepo) {
int iStatus;
return FetchLatestReleaseTag(sOwnerRepo, out iStatus);
} // FetchLatestReleaseTag method

public static string FetchLatestReleaseTag(string sOwnerRepo, out int iStatus) {
string sBody, sAsset;
return FetchLatestRelease(sOwnerRepo, "", out sBody, out sAsset, out iStatus);
} // FetchLatestReleaseTag method

public static string FetchLatestRelease(string sOwnerRepo, string sAssetName, out string sBody, out string sAssetUrl, out int iStatus) {
// Oddaj tag NAJNOWSZEGO wydania oraz - dolozone 11.09.2026 - opis wydania i
// adres pliku instalatora.  Opis jest potrzebny, bo w nim publikujemy sume
// kontrolna SHA-256 paczki; bez niej "bezpieczna aktualizacja" znaczylaby tylko
// "pobierz i uruchom, co przyszlo", a to nie jest zadne zabezpieczenie.
// Adres pliku bierzemy z API, a nie skladamy z nazwy, bo przy zmianie nazwy
// artefaktu skladanie po cichu dawaloby 404 zamiast aktualizacji.
sBody = "";
sAssetUrl = "";
string sApiUrl = "https://api.github.com/repos/" + sOwnerRepo + "/releases/latest";
string sFinalUrl = "";
string sJson = Homer.Web.getPage(sApiUrl, out sFinalUrl, out iStatus);
if (sJson.Length > 0) {
Match matchBody = Regex.Match(sJson, "\"body\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
if (matchBody.Success) sBody = JsonUnescape(matchBody.Groups[1].Value);
if (sAssetName != null && sAssetName.Length > 0) {
// Kazdy artefakt wydania to obiekt z "name" i "browser_download_url";
// szukamy pary, w ktorej nazwa zgadza sie z zadana.
foreach (Match m in Regex.Matches(sJson, "\"name\"\\s*:\\s*\"([^\"]+)\"[^{}]*?\"browser_download_url\"\\s*:\\s*\"([^\"]+)\"")) {
if (string.Equals(m.Groups[1].Value, sAssetName, StringComparison.OrdinalIgnoreCase)) { sAssetUrl = m.Groups[2].Value; break; }
}
if (sAssetUrl.Length == 0) {
// Zapasowo: pierwszy artefakt .exe w wydaniu.  Lepiej wziac instalator o
// innej nazwie niz nie zaktualizowac wcale.
foreach (Match m in Regex.Matches(sJson, "\"browser_download_url\"\\s*:\\s*\"([^\"]+\\.exe)\"")) { sAssetUrl = m.Groups[1].Value; break; }
}
}
Match matchTag = Regex.Match(sJson, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
if (matchTag.Success) return matchTag.Groups[1].Value;
}
string sPageUrl = "https://github.com/" + sOwnerRepo + "/releases/latest";
Homer.Web.getPage(sPageUrl, out sFinalUrl);
string sRedirect = (sFinalUrl == null ? "" : sFinalUrl).TrimEnd('/');
int iSlash = sRedirect.LastIndexOf('/');
if (iSlash >= 0 && iSlash < sRedirect.Length - 1) {
string sTag = sRedirect.Substring(iSlash + 1);
if (!sTag.Equals("latest", StringComparison.OrdinalIgnoreCase)) return sTag;
}
return "";
} // FetchLatestRelease method

public static string JsonUnescape(string sText) {
// Rozkoduj zawartosc lancucha JSON.  Opis wydania jest wieloliniowy, wiec bez
// tego suma kontrolna tonelaby w literalnych "\n".
if (sText == null) return "";
StringBuilder sb = new StringBuilder(sText.Length);
for (int i = 0; i < sText.Length; i++) {
char c = sText[i];
if (c != '\\' || i + 1 >= sText.Length) { sb.Append(c); continue; }
char cNext = sText[++i];
switch (cNext) {
case 'n': sb.Append('\n'); break;
case 'r': sb.Append('\r'); break;
case 't': sb.Append('\t'); break;
case 'b': sb.Append('\b'); break;
case 'f': sb.Append('\f'); break;
case '/': sb.Append('/'); break;
case '"': sb.Append('"'); break;
case '\\': sb.Append('\\'); break;
case 'u':
if (i + 4 < sText.Length) {
int iCode;
if (int.TryParse(sText.Substring(i + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out iCode)) { sb.Append((char) iCode); i += 4; }
}
break;
default: sb.Append(cNext); break;
}
}
return sb.ToString();
} // JsonUnescape method

public static string FileSha256(string sPath) {
// Suma kontrolna pliku, pisana wielkimi literami bez separatorow - w tej
// postaci porownujemy ja z suma z opisu wydania.
try {
using (FileStream fs = new FileStream(sPath, FileMode.Open, FileAccess.Read))
using (System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create()) {
return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "");
}
}
catch { return ""; }
} // FileSha256 method

public static int CompareVersions(string sA, string sB) {
// Compare two dotted-numeric version strings, e.g. "5.0.1" versus "5.0.0".
// Returns a negative value if sA is older, zero if equal, positive if newer.
// Missing trailing parts count as zero (so "5.0" equals "5.0.0"); any part
// that is not an integer falls back to ordinal string comparison.
string[] asA = (sA == null ? "" : sA).Split('.');
string[] asB = (sB == null ? "" : sB).Split('.');
int iCount = Math.Max(asA.Length, asB.Length);
int iIndex, iValA, iValB;
for (iIndex = 0; iIndex < iCount; iIndex++) {
string sPartA = iIndex < asA.Length ? asA[iIndex].Trim() : "0";
string sPartB = iIndex < asB.Length ? asB[iIndex].Trim() : "0";
bool bNumA = int.TryParse(sPartA, out iValA);
bool bNumB = int.TryParse(sPartB, out iValB);
if (bNumA && bNumB) { if (iValA != iValB) return iValA - iValB; }
else { int iCmp = string.CompareOrdinal(sPartA, sPartB); if (iCmp != 0) return iCmp; }
}
return 0;
} // CompareVersions method

public static string GetClipboardText() {
// Read text from the clipboard defensively.  The clipboard is a shared resource
// that another process can briefly lock; on Windows 11 (cloud clipboard and
// clipboard history) GetText throws an ExternalException intermittently when
// that happens.  Retry a few times, then give up quietly with "" rather than
// letting the exception crash EdSharp (the Append From Clipboard viewer reads
// the clipboard from inside WndProc, where an unhandled throw is fatal).
int iTry;
for (iTry = 0; iTry < 10; iTry++) {
try {
if (Clipboard.ContainsText()) return Clipboard.GetText();
return "";
}
catch (Exception) {
System.Threading.Thread.Sleep(40);
}
}
return "";
} // GetClipboardText method

// ZWRACA, CZY ZAPIS DOSZEDL.  Wczesniej metoda byla void, wiec po dziesieciu
// nieudanych probach wolajacy nie mial jak sie o tym dowiedziec i mowil
// "skopiowane" przy pustym schowku.  Dla niewidomego to najgorszy wariant:
// klawisz brzmi identycznie w obu wypadkach, a przy wklejaniu okazuje sie, ze
// tresci nie ma.  Wszystkie dotychczasowe wywolania ignoruja wynik i dzialaja
// jak dotad (C# pozwala odrzucic wartosc zwracana), wiec zmiana nie psuje
// niczego, a nowe wywolania moga POWIEDZIEC o niepowodzeniu.
public static bool SetClipboardText(string sText) {
// Write text to the clipboard defensively, with the same retry-on-contention
// logic as GetClipboardText.  An empty or null string clears the clipboard
// instead of throwing (Clipboard.SetText rejects the empty string).
int iTry;
for (iTry = 0; iTry < 10; iTry++) {
try {
if (sText == null || sText.Length == 0) Clipboard.Clear();
else Clipboard.SetText(sText);
return true;
}
catch (Exception) {
System.Threading.Thread.Sleep(40);
}
}
return false;
} // SetClipboardText method

// ZAPIS BOGATEGO SCHOWKA Z PONOWIENIAMI.  Zmierzone 01.09.2026 harnessem na
// zywej kontrolce: gdy schowek trzyma inny proces (u Kasperczaka menedzer
// schowka Ditto), Clipboard.SetDataObject rzuca ExternalException 0x800401d0
// ("zadana operacja schowka nie powiodla sie").  Nasze siedem wywolan w
// sciezkach kopiowania z formatowaniem NIE MIALO zadnej oslony, wiec wyjatek
// szedl do obslugi nieprzewidzianych zdarzen i konczyl sie oknem "Unexpected
// Event" ze sladem stosu - dokladnie ten komunikat o bledzie, ktory zglosil.
// Zwykla droga (SetClipboardText) miala ponowienia od dawna, bogata nie.
// Ta sama petla: dziesiec prob po 40 ms.  Blokada 250 ms zostala przez nia
// przeczekana w pomiarze, wiec typowa kolizja z menedzerem schowka mija.
// Zwraca false, gdy zapis NIE doszedl - wtedy wolajacy MOWI o tym zamiast
// udawac, ze skopiowal (cisza przy nieudanym kopiowaniu jest dla niewidomego
// gorsza niz komunikat: klawisz zachowuje sie identycznie w obu wypadkach).
public static bool SetClipboardData(DataObject data) {
if (data == null) return false;
int iTry;
for (iTry = 0; iTry < 10; iTry++) {
try {
Clipboard.SetDataObject(data, true);
return true;
}
catch (Exception) {
System.Threading.Thread.Sleep(40);
}
}
return false;
} // SetClipboardData method

// SCHOWEK PLIKOW, nie sam tekst ze sciezkami.  Zgloszenie Kasperczaka
// 05.09.2026: "kopiowanie wielu elementow dziala, ale pliki nie chca sie
// wklejac".  Przyczyna byla po naszej stronie: kopiowanie z listy plikow
// kladlo na schowek WYLACZNIE tekst, a Eksplorator Windows wkleja plik
// tylko wtedy, gdy w schowku jest format CF_HDROP (DataFormats.FileDrop)
// z lista sciezek.  Tekstu ze sciezka zadna powloka za plik nie uzna, wiec
// nie dzialalo to takze przy JEDNEJ pozycji - i nie mial z tym nic
// wspolnego menedzer schowka.
//
// "Preferred DropEffect" mowi powloce, czy wklejenie ma KOPIOWAC czy
// PRZENOSIC.  Bez tej wartosci czesc powlok wybiera przeniesienie, czyli po
// wklejeniu plik znika z miejsca zrodlowego - dla niewidomego strata bez
// zadnego sygnalu.  Stawiamy DROPEFFECT_COPY na sztywno.
//
// Tekst idzie do schowka ROWNOLEGLE, zeby wklejenie sciezek do dokumentu
// albo do pola adresu zachowalo sie tak jak dotad.
public static bool SetClipboardFileDrop(List<string> lsPaths, string sText) {
if (lsPaths == null || lsPaths.Count == 0) return false;
DataObject data = new DataObject();
StringCollection scPaths = new StringCollection();
foreach (string sPath in lsPaths) scPaths.Add(sPath);
data.SetFileDropList(scPaths);
byte[] aEffect = BitConverter.GetBytes((int) DragDropEffects.Copy);
data.SetData("Preferred DropEffect", new MemoryStream(aEffect));
if (sText != null && sText.Length > 0) {
data.SetData(DataFormats.UnicodeText, sText);
data.SetData(DataFormats.Text, sText);
}
return SetClipboardData(data);
} // SetClipboardFileDrop method

public static string FindPythonPath() {
// Locate a real Python interpreter. Windows puts a stub named python.exe on
// the PATH (in WindowsApps) that does not run anything: it opens the
// Microsoft Store advertisement instead. Hearing an advertisement when you
// expected either your program's output or an error message is baffling, so
// that folder is skipped and the search continues. Returns "" when nothing
// is found, and the caller then falls back to the bare name "python".
try {
string sPath = Environment.GetEnvironmentVariable("PATH");
if (sPath != null) {
foreach (string sDir in sPath.Split(';')) {
if (sDir.Trim().Length == 0) continue;
if (sDir.IndexOf(@"\WindowsApps", StringComparison.OrdinalIgnoreCase) >= 0) continue;
string sTry = Path.Combine(sDir.Trim(), "python.exe");
if (File.Exists(sTry)) return sTry;
}
}
List<string> lsRoots = new List<string>();
lsRoots.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
lsRoots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Python"));
// The python.org installer's own default for an all-users install is a
// folder straight off the drive root, such as C:\Python314.
lsRoots.Add(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)));
List<string> lsFound = new List<string>();
foreach (string sRoot in lsRoots) {
if (sRoot == null || !Directory.Exists(sRoot)) continue;
foreach (string sDir in Directory.GetDirectories(sRoot, "Python3*")) {
string sTry = Path.Combine(sDir, "python.exe");
if (File.Exists(sTry)) lsFound.Add(sTry);
}
}
lsFound.Sort();
lsFound.Reverse();
if (lsFound.Count > 0) return lsFound[0];
}
catch (Exception) {}
return "";
} // FindPythonPath method

public static string FindCscPath() {
// Locate a C# compiler: prefer the newest Roslyn csc (from VS Build Tools, for
// the latest C# language version), then fall back to the csc.exe that ships
// with the running .NET Framework, which is always present. Returns "" if none.
string sWin = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
string[] aCandidates = new string[] {
@"C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe",
@"C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe",
@"C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe",
@"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe",
@"C:\Program Files (x86)\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe",
@"C:\Program Files (x86)\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe",
Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "csc.exe"),
Path.Combine(sWin, @"Microsoft.NET\Framework64\v4.0.30319\csc.exe"),
Path.Combine(sWin, @"Microsoft.NET\Framework\v4.0.30319\csc.exe")
};
foreach (string s in aCandidates) {
try { if (File.Exists(s)) return s; } catch {}
}
return "";
} // FindCscPath method

public static void GetGoogleLanguages(out string[] aLanguageNames, out string[] aLanguageAbbreviations) {
List<string[]> lLanguages = new List<string[]>();
lLanguages.Add(new string[] {"AFRIKAANS", "af"});
lLanguages.Add(new string[] {"ALBANIAN", "sq"});
lLanguages.Add(new string[] {"AMHARIC", "am"});
lLanguages.Add(new string[] {"ARABIC", "ar"});
lLanguages.Add(new string[] {"ARMENIAN", "hy"});
lLanguages.Add(new string[] {"AZERBAIJANI", "az"});
lLanguages.Add(new string[] {"BASQUE", "eu"});
lLanguages.Add(new string[] {"BELARUSIAN", "be"});
lLanguages.Add(new string[] {"BENGALI", "bn"});
lLanguages.Add(new string[] {"BIHARI", "bh"});
lLanguages.Add(new string[] {"BULGARIAN", "bg"});
lLanguages.Add(new string[] {"BURMESE", "my"});
lLanguages.Add(new string[] {"CATALAN", "ca"});
lLanguages.Add(new string[] {"CHEROKEE", "chr"});
lLanguages.Add(new string[] {"CHINESE", "zh"});
lLanguages.Add(new string[] {"CHINESE_SIMPLIFIED", "zh-CN"});
lLanguages.Add(new string[] {"CHINESE_TRADITIONAL", "zh-TW"});
lLanguages.Add(new string[] {"CROATIAN", "hr"});
lLanguages.Add(new string[] {"CZECH", "cs"});
lLanguages.Add(new string[] {"DANISH", "da"});
lLanguages.Add(new string[] {"DHIVEHI", "dv"});
lLanguages.Add(new string[] {"DUTCH", "nl"});
lLanguages.Add(new string[] {"ENGLISH", "en"});
lLanguages.Add(new string[] {"ESPERANTO", "eo"});
lLanguages.Add(new string[] {"ESTONIAN", "et"});
lLanguages.Add(new string[] {"FILIPINO", "tl"});
lLanguages.Add(new string[] {"FINNISH", "fi"});
lLanguages.Add(new string[] {"FRENCH", "fr"});
lLanguages.Add(new string[] {"GALICIAN", "gl"});
lLanguages.Add(new string[] {"GEORGIAN", "ka"});
lLanguages.Add(new string[] {"GERMAN", "de"});
lLanguages.Add(new string[] {"GREEK", "el"});
lLanguages.Add(new string[] {"GUARANI", "gn"});
lLanguages.Add(new string[] {"GUJARATI", "gu"});
lLanguages.Add(new string[] {"HEBREW", "iw"});
lLanguages.Add(new string[] {"HINDI", "hi"});
lLanguages.Add(new string[] {"HUNGARIAN", "hu"});
lLanguages.Add(new string[] {"ICELANDIC", "is"});
lLanguages.Add(new string[] {"INDONESIAN", "id"});
lLanguages.Add(new string[] {"INUKTITUT", "iu"});
lLanguages.Add(new string[] {"ITALIAN", "it"});
lLanguages.Add(new string[] {"JAPANESE", "ja"});
lLanguages.Add(new string[] {"KANNADA", "kn"});
lLanguages.Add(new string[] {"KAZAKH", "kk"});
lLanguages.Add(new string[] {"KHMER", "km"});
lLanguages.Add(new string[] {"KOREAN", "ko"});
lLanguages.Add(new string[] {"KURDISH", "ku"});
lLanguages.Add(new string[] {"KYRGYZ", "ky"});
lLanguages.Add(new string[] {"LAOTHIAN", "lo"});
lLanguages.Add(new string[] {"LATVIAN", "lv"});
lLanguages.Add(new string[] {"LITHUANIAN", "lt"});
lLanguages.Add(new string[] {"MACEDONIAN", "mk"});
lLanguages.Add(new string[] {"MALAY", "ms"});
lLanguages.Add(new string[] {"MALAYALAM", "ml"});
lLanguages.Add(new string[] {"MALTESE", "mt"});
lLanguages.Add(new string[] {"MARATHI", "mr"});
lLanguages.Add(new string[] {"MONGOLIAN", "mn"});
lLanguages.Add(new string[] {"NEPALI", "ne"});
lLanguages.Add(new string[] {"NORWEGIAN", "no"});
lLanguages.Add(new string[] {"ORIYA", "or"});
lLanguages.Add(new string[] {"PASHTO", "ps"});
lLanguages.Add(new string[] {"PERSIAN", "fa"});
lLanguages.Add(new string[] {"POLISH", "pl"});
lLanguages.Add(new string[] {"PORTUGUESE", "pt-PT"});
lLanguages.Add(new string[] {"PUNJABI", "pa"});
lLanguages.Add(new string[] {"ROMANIAN", "ro"});
lLanguages.Add(new string[] {"RUSSIAN", "ru"});
lLanguages.Add(new string[] {"SANSKRIT", "sa"});
lLanguages.Add(new string[] {"SERBIAN", "sr"});
lLanguages.Add(new string[] {"SINDHI", "sd"});
lLanguages.Add(new string[] {"SINHALESE", "si"});
lLanguages.Add(new string[] {"SLOVAK", "sk"});
lLanguages.Add(new string[] {"SLOVENIAN", "sl"});
lLanguages.Add(new string[] {"SPANISH", "es"});
lLanguages.Add(new string[] {"SWAHILI", "sw"});
lLanguages.Add(new string[] {"SWEDISH", "sv"});
lLanguages.Add(new string[] {"TAJIK", "tg"});
lLanguages.Add(new string[] {"TAMIL", "ta"});
lLanguages.Add(new string[] {"TAGALOG", "tl"});
lLanguages.Add(new string[] {"TELUGU", "te"});
lLanguages.Add(new string[] {"THAI", "th"});
lLanguages.Add(new string[] {"TIBETAN", "bo"});
lLanguages.Add(new string[] {"TURKISH", "tr"});
lLanguages.Add(new string[] {"UKRAINIAN", "uk"});
lLanguages.Add(new string[] {"URDU", "ur"});
lLanguages.Add(new string[] {"UZBEK", "uz"});
lLanguages.Add(new string[] {"UIGHUR", "ug"});
lLanguages.Add(new string[] {"VIETNAMESE", "vi"});
lLanguages.Add(new string[] {"UNKNOWN", ""});

int iCount = lLanguages.Count;
aLanguageNames = new string[iCount];
aLanguageAbbreviations = new string[iCount];
for (int i = 0; i < iCount; i++) {
string[] a = lLanguages[i];
aLanguageNames[i] = a[0];
aLanguageAbbreviations[i] = a[1];
};
} // GetGoogleLanguages method

public static string[] OldGetGoogleLanguages() {
HomerList hl = new HomerList();
hl.Add("Arabic");
hl.Add("Bulgarian");
hl.Add("Chinese");
hl.Add("Catalan");
hl.Add("Croatian");
hl.Add("Czech");
hl.Add("Danish");
hl.Add("Dutch");
hl.Add("English");
hl.Add("Filipino");
hl.Add("Finnish");
hl.Add("French");
hl.Add("German");
hl.Add("Greek");
hl.Add("Hebrew");
hl.Add("Hindi");
hl.Add("Indonesian");
hl.Add("Italian");
hl.Add("Japanese");
hl.Add("Korean");
hl.Add("Latvian");
hl.Add("Lithuanian");
hl.Add("Norwegian");
hl.Add("Polish");
hl.Add("Portuguese");
hl.Add("Romanian");
hl.Add("Russian");
hl.Add("Spanish");
hl.Add("Serbian");
hl.Add("Slovak");
hl.Add("Slovenian");
hl.Add("Swedish");
hl.Add("Turkish");
hl.Add("Ukrainian");
hl.Add("Vietnamese");
hl.Add("Unknown");
return hl.ToArray();
} // OldGetGoogleLanguages method

public static bool MailMessage(string sRecipient, string sSubject, string sBody) {
sBody = Util.RegExpReplaceCase(sBody, "\r\n", "\r");
sBody = Util.RegExpReplaceCase(sBody, "\n", "\r");
sBody = Util.RegExpReplaceCase(sBody, "\r", "\r\n");
sBody = Util.RegExpReplaceCase(sBody, "\r\n", "%0D%0A");
sBody = Util.RegExpReplaceCase(sBody, " ", "%20");
sBody = Util.RegExpReplaceCase(sBody, "\t", "%09");
sBody = Util.RegExpReplaceCase(sBody, "\"", "%22");
sBody = Util.RegExpReplaceCase(sBody, "'", "%27");
sBody = Util.RegExpReplaceCase(sBody, "\\\\", "%5C");
// sBody = StringReplaceCase(sBody, "\\", "%5C");
// string sCommand = "mailto:?BODY=" + sBody;
string sCommand = "mailto:" + sRecipient + "?SUBJECT=" + sSubject + "&BODY=" + sBody;
bool bResult;
try {
Process.Start(sCommand);
bResult = true;
}
catch (Exception ex) {
Dialog.Show("Error", ex.Message);
bResult = false;
}
return bResult;
} // MailMessage method

public static Encoding OldGetFileEncoding(string sFile) {
System.Text.Encoding enc = null;
System.IO.FileStream file = new System.IO.FileStream(sFile,
FileMode.Open, FileAccess.Read, FileShare.Read);
if (file.CanSeek)
{
byte[] bom = new byte[4]; // Get the byte-order mark, if there is one
file.Read(bom, 0, 4);
if ((bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) || // utf-8
(bom[0] == 0xff && bom[1] == 0xfe) || // ucs-2le, ucs-4le, and ucs-16le
(bom[0] == 0xfe && bom[1] == 0xff) || // utf-16 and ucs-2
(bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff)) // ucs-4
{
enc = System.Text.Encoding.Unicode;
}
else
{
// enc = System.Text.Encoding.ASCII;
enc = System.Text.Encoding.Default;
}

// Now reposition the file cursor back to the start of the file
file.Seek(0, System.IO.SeekOrigin.Begin);
}
else
{
// The file cannot be randomly accessed, so you need to decide what to set the default to
// based on the data provided. If you're expecting data from a lot of older applications,
// default your encoding to Encoding.ASCII. If you're expecting data from a lot of newer
// applications, default your encoding to Encoding.Unicode. Also, since binary files are
// single byte-based, so you will want to use Encoding.ASCII, even though you'll probably
// never need to use the encoding then since the Encoding classes are really meant to get
// strings from the byte array that is the file.

// enc = System.Text.Encoding.ASCII;
enc = System.Text.Encoding.Default;
}
file.Close();
return enc;
} // OldGetFileEncoding method

[DllImport("user32.dll")]
public static extern IntPtr SetClipboardViewer(IntPtr h);

[DllImport("user32.dll")]
public static extern IntPtr     ChangeClipboardChain(IntPtr hCurrentClipboardViewer, IntPtr hNextClipboardViewer);

public static string GetTempFolder() {
object oSystem = COM.CreateObject("Scripting.FileSystemObject");
Object oDir = COM.CallMethod(oSystem, "GetSpecialFolder", new object[] {2});
string sPath = (string) COM.GetProperty(oDir, "Path");
return sPath;
} // GetTempFolder method

public static bool IsUnicode(string sText) {
foreach (char c in sText) {
// if (((int) c) > 255) Dialog.Show(c, (int) c);
if (((int) c) > 255) return true;
}
return false;
} // IsUnicode method

public static string Replicate(string sText, int iCount) {
string sReturn = sText;
for (int i = 1; i < iCount; i++) sReturn += sText;
return sReturn;
} // Replicate method

public static bool IsAppActiveWindow() {
IntPtr h = Win32.GetForegroundWindow();
foreach (Form frm in Application.OpenForms) if (frm.Handle == h) return true;
return false;
} // IsAppActiveWindow method

public static bool Spell(object oText) {
string sText = oText.ToString();
bool bReturn = false;
string sReturn = "";
for (int i = 0; i < sText.Length; i++) {
string s = sText.Substring(i, 1);
switch (s) {
case " " :
s = "Space";
sReturn += "Space\n";
break;
default :
sReturn += s + "\n";
break;
}
s = " " + s + " ";
bReturn = Say(s);
}
// return Say(sReturn, bGlobal);
return bReturn;
} // Spell method

public static bool Say(object oText) {
bool bGlobal = false;
return Say(oText, bGlobal);
} // Say method

public static bool Say(object oText, bool bGlobal) {
string sText = oText.ToString();
if (sText.Trim().Length == 0) sText = "Blank";
if (!App.ExtraSpeech) {
Util.StringAppend2File(sText + "\r\n", App.SpeechLog);
return false;
}

if (!bGlobal) {
if (!IsAppActiveWindow()) return false;

if ((Control.ModifierKeys & Keys.Alt) != 0 && (Control.ModifierKeys & Keys.Control) != 0) return false;
}

// if (Win32.JAWSSay(sText)) return true;
// Speech goes through Say (ported from DbDo): JAWS COM, then the
// NVDA controller client, then a native UIA notification reaching Narrator
// and any UIA-listening reader. Replaces the former per-reader COM/Win32
// chain (JFWSay / WESay / SASay / NVDASay / SAPISay).
Homer.Say.sayForced(sText);
return true;
} // Say method

public static string Key2String(Keys keyData) {
return TypeDescriptor.GetConverter(typeof(Keys)).ConvertToString(keyData);
} // Key2String method

// STRAZNIK PISANIA.  Czy tym chordem uzytkownik WPISUJE znak na biezacym
// ukladzie klawiatury?  Jesli tak, chord nie moze zostac skrotem komendy.
//
// SKAD TO PYTANIE: Kasperczak (30.08.2026) o naszym Control+Alt+K: "Alt+Ctrl
// to tez to samo co prawy Alt, a prawy Alt to polska literka, ale mozna to
// jakos tak zrobic, zeby dzialalo prawidlowo".  Ma racje co do klawiatury: na
// ukladzie Polish (Programmers) prawy Alt to wlasnie Control+Alt, a dziewiec
// polskich liter powstaje tak wlasnie (a z ogonkiem na A, c z kreska na C,
// e z ogonkiem na E, l z kreska na L, n z kreska na N, o z kreska na O,
// s z kreska na S, z z kreska na X, z z kropka na Z).
//
// DLACZEGO PYTAMY UKLAD, A NIE MAMY LISTY LITER W KODZIE: lista bylaby
// prawdziwa tylko dla polskiej klawiatury i klamala by dla kazdej innej.
// VkKeyScanEx zwraca dla ZNAKU klawisz i modyfikatory, wiec odpowiada
// dokladnie na to pytanie i sam z siebie milczy tam, gdzie znaku nie ma.
//
// ZAKRES: sprawdzamy CONTROL+ALT bez Shifta i z Shiftem (wielka litera to
// Control+Alt+Shift na tym samym klawiszu).  Chordy z samym Control albo samym
// Alt nie wpisuja znakow, wiec ich nie ruszamy - inaczej straznik zabral by
// nam wszystkie normalne skroty.
public static bool IsTypingChord(Keys keyData) {
if (keyData == Keys.None) return false;
Keys mods = keyData & (Keys.Control | Keys.Shift | Keys.Alt);
if (mods != (Keys.Control | Keys.Alt) && mods != (Keys.Control | Keys.Alt | Keys.Shift)) return false;
Keys keyCode = keyData & Keys.KeyCode;

IntPtr hkl;
try {hkl = Win32.GetKeyboardLayout(0);} catch {return false;}
if (hkl == IntPtr.Zero) return false;

// Znaki, ktore w ogole moglyby powstac z AltGr, poznajemy nie z listy, a
// przez przeszukanie: pytamy uklad o kazdy znak z zakresu Latin-1 oraz
// Latin Extended-A (tam mieszcza sie wszystkie europejskie diakrytyki) i
// patrzymy, czy powstaje TYM klawiszem z Control+Alt.
bool bShift = ((mods & Keys.Shift) == Keys.Shift);
for (int c = 0x20; c <= 0x17F; c++) {
short r;
try {r = Win32.VkKeyScanEx((char) c, hkl);} catch {return false;}
if (r == -1) continue;
int vk = r & 0xFF;
int m = (r >> 8) & 7;
if ((int) keyCode != vk) continue;
// Modyfikatory znaku musza sie zgadzac z chordem: Control+Alt dla malej
// litery, Control+Alt+Shift dla wielkiej.
bool bZnakShift = ((m & 1) != 0);
bool bZnakAltGr = ((m & 6) == 6);
if (!bZnakAltGr) continue;
if (bZnakShift == bShift) return true;
}
return false;
} // IsTypingChord method

public static Keys String2Key(string sKey) {
// A blank key means "menu-only command": the item appears in the menu and
// is reachable from the Alternate Menu, but carries no chord. Keys.None
// models that, and KeyMap.register already skips unbound commands.
// Measured 21 August 2026 with a standalone probe against
// KeysConverter: ConvertFromString("") and ConvertFromString("   ")
// return NULL, so the former direct cast raised NullReferenceException,
// and ConvertFromString(null) raises NotSupportedException. Every such
// throw happened while the menus were being built, i.e. during startup.
if (sKey == null) return Keys.None;
sKey = sKey.Trim();
if (sKey.Length == 0) return Keys.None;
object oKey = TypeDescriptor.GetConverter(typeof(Keys)).ConvertFromString(sKey);
if (oKey == null) return Keys.None;
return (Keys) oKey;
} // String2Key method

public static string Font2String(Font font) {
return TypeDescriptor.GetConverter(typeof(Font)).ConvertToString(font);
} // Font2String method

public static Font String2Font(string sFont) {
return (Font) TypeDescriptor.GetConverter(typeof(Font)).ConvertFromString(sFont);
} // String2Font method

public static string Color2String(Color color) {
return TypeDescriptor.GetConverter(typeof(Color)).ConvertToString(color);
} // Color2String method

public static Color String2Color(string sColor) {
return (Color) TypeDescriptor.GetConverter(typeof(Color)).ConvertFromString(sColor);
} // String2Color method

public static string GetFriendlyKeyName(string sKey) {
if (sKey.Contains("+OemQuotes")) sKey = sKey.Replace("+OemQuotes", "+Apostrophe");
if (sKey.Contains("+Back")) sKey = sKey.Replace("+Back", "+Backspace");
if (sKey.Contains("+Oem5")) sKey = sKey.Replace("+Oem5", "+Backslash");
if (sKey.Contains("+Oemplus")) sKey = sKey.Replace("+Oemplus", "+Equals");
if (sKey.Contains("+OemMinus")) sKey = sKey.Replace("+OemMinus", "+Dash");
if (sKey.Contains("+OemSemicolon")) sKey = sKey.Replace("+OemSemicolon", "+Semicolon");
if (sKey.Contains("+D6")) sKey = sKey.Replace("+D6", "+Caret");
if (sKey.Contains("+D0")) sKey = sKey.Replace("+D0", "+0");
if (sKey.Contains("+OemQuestion")) sKey = sKey.Replace("+OemQuestion", "+Slash");
if (sKey.Contains("+OemOpenBrackets")) sKey = sKey.Replace("+OemOpenBrackets", "+LeftBracket");
if (sKey.Contains("+OemCloseBrackets")) sKey = sKey.Replace("+OemCloseBrackets", "+RightBracket");
if (sKey.Contains("+Oemcomma")) sKey = sKey.Replace("+Oemcomma", "+Comma");
if (sKey.Contains("+OemPeriod")) sKey = sKey.Replace("+OemPeriod", "+Period");
return sKey;
} // GetFriendlyKeyName method

public static string RegExpReplaceEquiv(string sText, string sMatch, string sReplace) {
bool bCaseSensitive = false;
return RegExpReplace(sText, sMatch, sReplace, bCaseSensitive);
} // RegExpReplaceEquiv method

public static string RegExpReplaceCase(string sText, string sMatch, string sReplace) {
bool bCaseSensitive = true;
return RegExpReplace(sText, sMatch, sReplace, bCaseSensitive);
} // RegExpReplaceCase method

public static string RegExpReplace(string sText, string sMatch, string sReplace, bool bCaseSensitive) {
RegexOptions options = RegexOptions.Multiline;
if (!bCaseSensitive) options |= RegexOptions.IgnoreCase;
Regex rx = new Regex(sMatch, options);
string sReturn = rx.Replace(sText, sReplace);
return sReturn;
} // RegExpReplace method

public static int RegExpCountEquiv(string sText, string sMatch) {
bool bCaseSensitive = false;
return RegExpCount(sText, sMatch, bCaseSensitive);
} // RegExpCountEquiv method

public static int RegExpCountCase(string sText, string sMatch) {
bool bCaseSensitive = true;
return RegExpCount(sText, sMatch, bCaseSensitive);
} // RegExpCountCase method

public static int RegExpCount(string sText, string sMatch, bool bCaseSensitive) {
RegexOptions options = RegexOptions.Multiline;
if (!bCaseSensitive) options |= RegexOptions.IgnoreCase;
Regex rx = new Regex(sMatch, options);
MatchCollection matches = rx.Matches(sText);
int iReturn = matches.Count;
return iReturn;
} // RegExpCount method

public static string[] RegExpExtractEquiv(string sText, string sMatch) {
bool bCaseSensitive = false;
return RegExpExtract(sText, sMatch, bCaseSensitive);
} // RegExpExtractEquiv method

public static string[] RegExpExtractCase(string sText, string sMatch) {
bool bCaseSensitive = true;
return RegExpExtract(sText, sMatch, bCaseSensitive);
} // RegExpExtractCase method

public static string[] RegExpExtract(string sText, string sMatch, bool bCaseSensitive) {
RegexOptions options = RegexOptions.Multiline;
if (!bCaseSensitive) options |= RegexOptions.IgnoreCase;
Regex rx = new Regex(sMatch, options);
MatchCollection matches = rx.Matches(sText);
string[] aReturn = new string[matches.Count];
for (int i = 0; i < aReturn.Length; i++) aReturn[i] = matches[i].Value;
return aReturn;
} // RegExpExtract method

public static object[][] RegExpExtractWithIndex(string sText, string sMatch, bool bCaseSensitive) {
RegexOptions options = RegexOptions.Multiline;
if (!bCaseSensitive) options |= RegexOptions.IgnoreCase;
Regex rx = new Regex(sMatch, options);
MatchCollection matches = rx.Matches(sText);
object[][] aReturn = new object[matches.Count][];
for (int i = 0; i < aReturn.Length; i++) aReturn[i] = new object[] {matches[i].Index, matches[i].Value};
return aReturn;
} // RegExpExtractWithIndex method

public static object[] RegExpContainsEquiv(string sText, string sMatch) {
int iStart = 0;
return RegExpContainsEquiv(sText, sMatch, iStart);
} // RegExpContainsEquiv method

public static object[] RegExpContainsEquiv(string sText, string sMatch, int iStart) {
bool bCaseSensitive = false;
bool bLast = false;
return RegExpContains(sText, sMatch, bCaseSensitive, bLast, iStart);
} // RegExpContainsEquiv method

public static object[] RegExpContainsCase(string sText, string sMatch) {
int iStart = 0;
return RegExpContainsCase(sText, sMatch, iStart);
} // RegExpContainsCase method

public static object[] RegExpContainsCase(string sText, string sMatch, int iStart) {
bool bCaseSensitive = true;
bool bLast = false;
return RegExpContains(sText, sMatch, bCaseSensitive, bLast, iStart);
} // RegExpContainsCase method

public static object[] RegExpContainsLastEquiv(string sText, string sMatch) {
bool bCaseSensitive = false;
bool bLast = true;
return RegExpContains(sText, sMatch, bCaseSensitive, bLast);
} // RegExpContainsLastEquiv method

public static object[] RegExpContainsLastCase(string sText, string sMatch) {
bool bCaseSensitive = true;
bool bLast = true;
return RegExpContains(sText, sMatch, bCaseSensitive, bLast);
} // RegExpContainsLastCase method

public static object[] RegExpContains(string sText, string sMatch, bool bCaseSensitive, bool bLast) {
int iStart;
//if (bLast)  iStart = sText.Length - 1;
if (bLast)  iStart = sText.Length;
else iStart = 0;
return RegExpContains(sText, sMatch, bCaseSensitive, bLast, iStart);
} // RegExpContains method

public static object[] RegExpContains(string sText, string sMatch, bool bCaseSensitive, bool bLast, int iStart) {
RegexOptions options = RegexOptions.Multiline;
if (!bCaseSensitive) options |= RegexOptions.IgnoreCase;
if (bLast) options |= RegexOptions.RightToLeft;
Regex rx = new Regex(sMatch, options);

Match match = rx.Match(sText, iStart);
object[] aReturn = new object[] {-1, null};
// Dialog.Show(match.Success);
if (match.Success) aReturn = new object[] {match.Index, match.Value};
return aReturn;
} // RegExpContains method

public static bool Equiv(string s1, string s2) {
return String.Compare(s1, s2, true) == 0;
} // Equiv method

public static bool ToBool(string sValue) {
// Liberal truth test for .inix-style flags, matching Regexer's convention.
if (sValue == null) return false;
sValue = sValue.Trim().ToLower();
return (sValue == "true" || sValue == "yes" || sValue == "on" || sValue == "1" || sValue == "y");
} // ToBool method

public static RegexOptions RegexOptionsFromString(string sOptions) {
// Build a RegexOptions value from a comma-separated list of .NET option names,
// as Regexer does for each [Section]'s Options key. Unknown names are ignored;
// blank yields RegexOptions.None.
RegexOptions options = RegexOptions.None;
if (sOptions == null) return options;
string[] aOptions = sOptions.Replace(" ", "").ToLower().Split(',');
foreach (string sOption in aOptions) {
if (sOption == "compiled") options = options | RegexOptions.Compiled;
else if (sOption == "cultureinvariant") options = options | RegexOptions.CultureInvariant;
else if (sOption == "ecmascript") options = options | RegexOptions.ECMAScript;
else if (sOption == "explicitcapture") options = options | RegexOptions.ExplicitCapture;
else if (sOption == "ignorecase") options = options | RegexOptions.IgnoreCase;
else if (sOption == "ignorepatternwhitespace") options = options | RegexOptions.IgnorePatternWhitespace;
else if (sOption == "multiline") options = options | RegexOptions.Multiline;
else if (sOption == "righttoleft") options = options | RegexOptions.RightToLeft;
else if (sOption == "singleline") options = options | RegexOptions.Singleline;
}
return options;
} // RegexOptionsFromString method

public static string Pluralize(int iCount, string sSingular) {
string sPlural = null;
return Pluralize(iCount, sSingular, sPlural);
} // Pluralize method

public static string Pluralize(int iCount, string sSingular, string sPlural) {
if (sPlural == null) sPlural = sSingular + "s";
string sReturn = iCount.ToString() + " ";
if (iCount == 1) sReturn += sSingular;
else sReturn += sPlural;
return sReturn;
} // Pluralize method

// Read an .ini file for KEY ENUMERATION.  Windows writes .ini files through
// WritePrivateProfileString in the system ANSI code page, so on a Polish
// system a file name containing "\u0142" is stored as the single byte 0xB3.
// File2String assumes UTF-8 when there is no byte-order mark, which turned
// that byte into U+FFFD; the recovered key then matched no file on disk and
// the Recent/Favorites cleanup loops DELETED the entry.  Kasperczak reported
// it verbatim (Telegram 17.08.2026): a favorite whose name starts with a
// Polish letter disappeared from both lists after closing, and renaming the
// file to plain ASCII made it stay.  Measured with an EM/WritePrivateProfile
// harness: byte 0xB3 written, UTF-8 read gives U+FFFD, an ANSI read restores
// the original path.  So: a byte-order mark still wins, then STRICT UTF-8
// (a real UTF-8 file decodes), and only a file that is not valid UTF-8 falls
// back to the ANSI code page.
public static string IniFile2String(string sFile) {
if (!File.Exists(sFile)) return "";
byte[] aBytes = System.IO.File.ReadAllBytes(sFile);
if (aBytes.Length >= 3 && aBytes[0] == 0xEF && aBytes[1] == 0xBB && aBytes[2] == 0xBF) return new UTF8Encoding(true).GetString(aBytes, 3, aBytes.Length - 3);
if (aBytes.Length >= 2 && aBytes[0] == 0xFF && aBytes[1] == 0xFE) return Encoding.Unicode.GetString(aBytes, 2, aBytes.Length - 2);
if (aBytes.Length >= 2 && aBytes[0] == 0xFE && aBytes[1] == 0xFF) return Encoding.BigEndianUnicode.GetString(aBytes, 2, aBytes.Length - 2);
try {
UTF8Encoding enStrict = new UTF8Encoding(false, true);
return enStrict.GetString(aBytes);
}
catch (DecoderFallbackException) { return Encoding.Default.GetString(aBytes); }
} // IniFile2String method

public static string File2String(string sFile) {
Encoding en = null;
return File2String(sFile, ref en);
} // File2String method

public static string File2String(string sFile, ref Encoding en) {
//return System.IO.File.ReadAllText(sFile);
// Dialog.Show("Encoding", Util.GetFileEncoding(sFile));
if (en == null) en = Util.GetFileEncoding(sFile, App.BomDictionary);
// return System.IO.File.ReadAllText(sFile, System.Text.Encoding.Default);
// return System.IO.File.ReadAllText(sFile, encoding);
string sText = System.IO.File.ReadAllText(sFile, en);
return sText;
} // File2String method

public static string OldFile2String(string sFile) {
if (!File.Exists(sFile)) return "";
StreamReader textReader = new StreamReader(sFile);
string sBody = textReader.ReadToEnd();
textReader.Close();
return sBody;
} // OldFile2String method

public static void String2FileU(string sBody, string sFile) {
// bool bAppend = false;
System.IO.File.WriteAllText(sFile, sBody, Encoding.UTF8);
} // String2FileU method

public static void StringAppend2File(string sBody, string sFile) {
File.AppendAllText(sFile, sBody);
} // StringAppend2File method

public static void String2FileA(string sBody, string sFile) {
Encoding en = null;
String2FileA(sBody, sFile, en);
} // String2FileA method

public static void String2FileA(string sBody, string sFile, Encoding en) {
StreamWriter textWriter = new StreamWriter(sFile);
textWriter.Write(sBody);
textWriter.Close();
} // OldString2File method

public static void String2File(string sBody, string sFile) {
Encoding en = null;
String2File(sBody, sFile, ref en);
} // String2File method

public static void String2File(string sBody, string sFile, ref Encoding en) {
// Dialog.Show(IsUnicode(sBody));
if (en != null) {}
// Do nothing
else if (IsUnicode(sBody))en = Encoding.UTF8;
else en = Encoding.Default;

// sBody = Util.Convert2WinLineBreak(sBody);
File.WriteAllText(sFile, sBody, en);
} // String2File method

public static string Quote(string sText) {
return "\"" + Unquote(sText) + "\"";
} // Quote method

public static string Unquote(string sText) {
return sText.Trim('"');
} // Unquote method

public static string ConvertQuotes(string sText) {
string sReturn = sText.Replace(@"?", @"""");
sReturn  = sReturn.Replace(@"?", @"""");
sReturn  = sReturn.Replace(@"-", @"-");
sReturn  = sReturn.Replace(@"?", @"...");
sReturn  = sReturn.Replace(@"?", @"'");
sReturn  = sReturn.Replace(Util.Code2String(65533), @"'");
return sReturn;
} // ConvertQuotes method

public static string Convert2Ascii(string sText) {
int iLength = sText.Length;
for (int i = iLength - 1; i >= 0; i--) {
if ((int) sText[i] > 127) sText = sText.Remove(i, 1);
}
return sText;
} //Convert2Ascii method

public static string Convert2MacLineBreak(string sText) {
//Convert to Macintosh line break, \r;
string sMatch, sReplace;

sMatch = "\r\n";
sReplace = "\r";
sText = Util.RegExpReplaceCase(sText, sMatch, sReplace);
sMatch = "\n";
sText = Util.RegExpReplaceCase(sText, sMatch, sReplace);
return sText;
} // Convert2MacLineBreak metod

public static string Convert2UnixLineBreak(string sText) {
//Convert to Unix line break, \n;
string sMatch, sReplace;
sMatch = "\r\n";
sReplace = "\n";
sText = Util.RegExpReplaceCase(sText, sMatch, sReplace);
sMatch = "\r";
sText = Util.RegExpReplaceCase(sText, sMatch, sReplace);
return sText;
} // Convert2UnixLineBreak method

public static string Convert2WinLineBreak(string sText) {
//Convert to standard Windows line break, \r\nVar;
string sMatch, sReplace;
sMatch = "\r\n";
sReplace = "\n";
sText = Util.RegExpReplaceCase(sText, sMatch, sReplace);
sMatch = "\r";
sText = Util.RegExpReplaceCase(sText, sMatch, sReplace);
sMatch = "\n";
sReplace = "\r\n";
sText = Util.RegExpReplaceCase(sText, sMatch, sReplace);
return sText;
} // Convert2WinLineBreakMethod

public static int RunHideWait(string sPath) {
return runShell(sPath, ProcessWindowStyle.Hidden, true);
} //RunHideWait method

public static int RunHide(string sPath) {
return runShell(sPath, ProcessWindowStyle.Hidden, false);
} //RunHide method

public static int Run(string sPath) {
return runShell(sPath, ProcessWindowStyle.Normal, false);
} //Run method

public static int RunWait(string sPath) {
return runShell(sPath, ProcessWindowStyle.Normal, true);
} //RunWait method

// runShell: launch a command line as a process (replaces VB Shell). The
// command line is split into executable and arguments so window style is
// honored on the child without a cmd wrapper. Returns the process id, or 0.
static int runShell(string sCommand, ProcessWindowStyle style, bool bWait) {
string sExe, sArgs;
sCommand = sCommand.Trim();
if (sCommand.StartsWith("\"")) {
int iEnd = sCommand.IndexOf('\"', 1);
sExe = sCommand.Substring(1, iEnd - 1);
sArgs = sCommand.Substring(iEnd + 1).Trim();
}
else {
int iSpace = sCommand.IndexOf(' ');
if (iSpace < 0) { sExe = sCommand; sArgs = ""; }
else { sExe = sCommand.Substring(0, iSpace); sArgs = sCommand.Substring(iSpace + 1).Trim(); }
}
ProcessStartInfo psi = new ProcessStartInfo(sExe, sArgs);
psi.UseShellExecute = false;
psi.WindowStyle = style;
psi.CreateNoWindow = (style == ProcessWindowStyle.Hidden);
Process p = Process.Start(psi);
if (bWait && p != null) p.WaitForExit();
return (p != null) ? p.Id : 0;
} // runShell method

public static void ActivatePid(int iPid) {
Process p = Process.GetProcessById(iPid);
if (p != null && p.MainWindowHandle != IntPtr.Zero) Win32.SetForegroundWindow(p.MainWindowHandle);
} // ActivatePid method

public static bool ActivateProcess(string sProcess) {
Process[] processes = Process.GetProcessesByName(sProcess);
if (processes.Length == 0) return false;
Process process = processes[0];
//Dialog.Show(process.ProcessName, process.MainWindowTitle);

int iPid = processes[0].Id;
try {
ActivatePid(iPid);
return true;
}
catch {
return false;
}
} // ActivateProcess method

public static void ActivateTitle(string sTitle) {
IntPtr h = Win32.FindWindow(0, sTitle);
if ((int) h != 0) Win32.SetForegroundWindow(h);
} // ActivateTitle method

public static void Beep() {
System.Media.SystemSounds.Beep.Play();
} // Beep method

public static object If(bool bExp, object oTrue, object oFalse) {
return bExp ? oTrue : oFalse;
} // If method

public static int If(bool bExp, int iTrue, int iFalse) {
if (bExp) return iTrue;
else return iFalse;
} // If method

public static string If(bool bExp, string sTrue, string sFalse) {
if (bExp) return sTrue;
else return sFalse;
} // If method

public static string GetCommandLine() {
string[] aArgs = Environment.GetCommandLineArgs();
StringBuilder sb = new StringBuilder();
for (int i = 1; i < aArgs.Length; i++) { if (i > 1) sb.Append(" "); sb.Append(aArgs[i]); }
return sb.ToString();
} // GetCommandLine method

public static string[] OldGetFiles(string sDir, string sFilter, bool bSubDirs) {
string sFiles;
string[] a, aDirs, aFiles;
StringBuilder sb = new StringBuilder();

aDirs = Directory.GetDirectories(sDir);
if (bSubDirs) {
foreach (string s in aDirs) {
a = OldGetFiles(s, sFilter, bSubDirs);
sFiles = String.Join("\n", a);
if (sFiles.Length > 0) sb.Append(sFiles + "\n");
}
}

aFiles = Directory.GetFiles(sDir, sFilter);
sFiles = String.Join("\n", aFiles);
if (sFiles.Length > 0) sb.Append(sFiles + "\n");

sFiles = sb.ToString().TrimEnd();
if (sFiles.Length > 0) aFiles = sFiles.Split('\n');
else aFiles = new string[] {};
return aFiles;
} // OldGetFiles method

public static string[] FindInFiles(string sText, string sDir, string[] aFilters, bool bSubdirs) {
string[] aFiles = GetFiles(sDir, aFilters, bSubdirs);
List<string> list = new List<string>();
foreach (string sFile in aFiles) {
try { if (File.ReadAllText(sFile).IndexOf(sText, StringComparison.OrdinalIgnoreCase) >= 0) list.Add(sFile); }
catch { }
}
return list.ToArray();
} // FindInFiles method

public static string[] GetFiles(string sDir, string[] aFilters, bool bSubdirs) {
SearchOption searchOption = bSubdirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
List<string> list = new List<string>();
if (aFilters == null || aFilters.Length == 0) list.AddRange(Directory.GetFiles(sDir, "*", searchOption));
else foreach (string sFilter in aFilters) list.AddRange(Directory.GetFiles(sDir, sFilter, searchOption));
return list.ToArray();
} // GetFiles method

public static string[] GetPathsWithExtensions(string[] aFiles, string sExtensions) {
string sResult = "." + sExtensions.Trim().Replace(" ", " .");
sResult = sResult.Replace("..", ".");
string [] aResults = sResult.Split(' ');
List<string> list = new List<string>(aFiles);
for (int i = list.Count -1; i >=0; i--) {
string sFile = list[i];
string sExtension = Path.GetExtension(sFile).ToLower();
if (sExtension.Length == 0) sExtension = ".";
if (Array.IndexOf(aResults, sExtension) == -1) list.RemoveAt(i);
}
return list.ToArray();
} // GetPathsWithExtensions method

public static string GetExtensions(string sDir) {
return GetExtensions(Directory.GetFiles(sDir));
} // GetExtensions method

public static string GetExtensions(string[] aFiles) {
//string[] aFilters = new string[] {"*.*"};
//bool bSubdirs = false;
//string[] aFiles = GetFiles(sDir, aFilters, bSubdirs);
List<string> list = new List<string>(aFiles.Length);
for (int i = 0; i < aFiles.Length; i++) {
string s = aFiles[i];
s = Path.GetExtension(s);
//if (s.Length == 0) continue;
s = s.TrimStart('.');
s = s.ToLower();
if (s.Length == 0) s = ".";
if (!list.Contains(s)) list.Add(s);
}

list.Sort();
string[] aExtensions = list.ToArray();
return String.Join(" ", aExtensions);
} // GetExtensions method

public static bool PathExists(string sPath) {
return (Directory.Exists(sPath) || File.Exists(sPath));
} // PathExists method

public static void DeletePath(string sPath, bool bRecycle) {
FileAttributes attr = File.GetAttributes(sPath);
FileAttributes flag = FileAttributes.ReadOnly;
File.SetAttributes(sPath, (attr | flag) ^ flag);
if (Directory.Exists(sPath)) DeleteDirectory(sPath, bRecycle);
else if (File.Exists(sPath)) DeleteFile(sPath, bRecycle);
} // DeletePath method

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
struct ShFileOpStruct {
public IntPtr hwnd;
public uint wFunc;
[MarshalAs(UnmanagedType.LPWStr)] public string pFrom;
[MarshalAs(UnmanagedType.LPWStr)] public string pTo;
public ushort fFlags;
public int fAnyOperationsAborted;
public IntPtr hNameMappings;
[MarshalAs(UnmanagedType.LPWStr)] public string lpszProgressTitle;
} // ShFileOpStruct struct

[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
static extern int SHFileOperation(ref ShFileOpStruct lpFileOp);

// recycle: send a file or folder to the Recycle Bin through the shell, the
// pure .NET Framework equivalent of the former VB SendToRecycleBin option.
static void recycle(string sPath) {
ShFileOpStruct op = new ShFileOpStruct();
op.wFunc = 3; // FO_DELETE
op.pFrom = sPath + "\0\0"; // list is double-null terminated
op.fFlags = (ushort) (0x0040 | 0x0010 | 0x0004 | 0x0400); // ALLOWUNDO|NOCONFIRMATION|SILENT|NOERRORUI
SHFileOperation(ref op);
} // recycle method

// copyDirectory: recursive directory copy (System.IO has no built-in one).
static void copyDirectory(string sSource, string sTarget) {
Directory.CreateDirectory(sTarget);
foreach (string sFile in Directory.GetFiles(sSource))
File.Copy(sFile, Path.Combine(sTarget, Path.GetFileName(sFile)), true);
foreach (string sDir in Directory.GetDirectories(sSource))
copyDirectory(sDir, Path.Combine(sTarget, Path.GetFileName(sDir)));
} // copyDirectory method

public static void DeleteDirectory(string sPath, bool bRecycle) {
if (!Directory.Exists(sPath)) return;
if (bRecycle) recycle(sPath);
else Directory.Delete(sPath, true);
} // DeleteDirectory method

public static void DeleteFile(string sPath, bool bRecycle) {
if (!File.Exists(sPath)) return;
if (bRecycle) recycle(sPath);
else File.Delete(sPath);
} // DeleteFile method

public static void CopyDirectory(string sSource, string sTarget, bool bRecycle) {
if (Directory.Exists(sTarget)) DeleteDirectory(sTarget, bRecycle);
else if (File.Exists(sTarget)) DeleteFile(sTarget, bRecycle);
copyDirectory(sSource, sTarget);
} // CopyDirectory method

public static void MoveDirectory(string sSource, string sTarget, bool bRecycle) {
if (Directory.Exists(sTarget)) DeleteDirectory(sTarget, bRecycle);
else if (File.Exists(sTarget)) DeleteFile(sTarget, bRecycle);
Directory.Move(sSource, sTarget);
} // MoveDirectory method

public static void CopyFile(string sSource, string sTarget, bool bRecycle) {
if (Directory.Exists(sTarget)) DeleteDirectory(sTarget, bRecycle);
else if (File.Exists(sTarget)) DeleteFile(sTarget, bRecycle);
File.Copy(sSource, sTarget, true);
} // CopyFile method

public static void MoveFile(string sSource, string sTarget, bool bRecycle) {
if (Directory.Exists(sTarget)) DeleteDirectory(sTarget, bRecycle);
else if (File.Exists(sTarget)) DeleteFile(sTarget, bRecycle);
File.Move(sSource, sTarget);
} // MoveFile method

public static void SendKeys(string sKeys) {
System.Windows.Forms.SendKeys.SendWait(sKeys);
} // SendKeys method

public static string ProperCase(string sText) {
return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(sText.ToLower());

/*
string[] aWords = sText.Split(' ');
for (int i = 0; i < aWords.Length; i++) {
string sWord = aWords[i];
string sInitial = sWord.Substring(0, 1).ToUpper();
string sRest = "";
if (sWord.Length > 1) sRest = sWord.Substring(1).ToLower();
sWord = sInitial + sRest;
aWords[i] = sWord;
}

string sReturn = String.Join(" ", aWords);
return sReturn;
*/
} // ProperCase method

public static string SwapCase(string sText) {
string sReturn = "";
StringBuilder sb = new StringBuilder(sText.Length);
for (int i = 0; i < sText.Length; i++) {
string s = sText.Substring(i, 1);
string sLower = s.ToLower();
string sUpper = s.ToUpper();
if (sLower == sUpper) sb.Append(s);
else if (s == sLower) sb.Append(sUpper);
else if (s == sUpper) sb.Append(sLower);
}

sReturn = sb.ToString();
return sReturn;
} // SwapCase method

public static int Month2Num(string sMonth) {
sMonth = Util.ProperCase(sMonth.Trim());
string[] aMonths = {"January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"};
int iReturn = -1;
for (int i = 0; i < aMonths.Length; i++) {
string s = aMonths[i];
if (!s.StartsWith(sMonth)) continue;
iReturn = i + 1;
break;
}
return iReturn;
} // Month2Num method

public static int Day2Num(string sDay) {
sDay = Util.ProperCase(sDay.Trim());
string[] aDays = {"Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"};
int iReturn = -1;
for (int i = 0; i < aDays.Length; i++) {
string s = aDays[i];
if (!s.StartsWith(sDay)) continue;
iReturn = i;
break;
}
return iReturn;
} // Day2Num method

public static string Type2String(object o) {
return TypeDescriptor.GetConverter(o.GetType()).ConvertToString(o);
} // Type2String method

public static object String2Type(string s, object o) {
return TypeDescriptor.GetConverter(o.GetType()).ConvertFromString(s);
} // String2Type method

public static void TerminateProcess(string sName) {
bool bLoop = true;
while (bLoop) {
Process[] processes = Process.GetProcessesByName(sName);
if (processes.Length == 0) break;

Process process = processes[0];
int iPid = process.Id;
process.CloseMainWindow();
System.Threading.Thread.Sleep(500);
try {
process = Process.GetProcessById(iPid);
process.Kill();
}
catch {
break;
}
}
} // TerminateProcess method

public static string GetLfn(string sPath) {
object oShell = COM.CreateObject("WScript.Shell");
object oShortcut = COM.CallMethod(oShell, "CreateShortcut", "temp.lnk");
COM.SetProperty(oShortcut, "TargetPath", sPath);
string sReturn = (string) COM.GetProperty(oShortcut, "TargetPath");
//COM.Release(ref oShortcut);
//COM.Release(ref oShell);
return sReturn;
} // GetLfn method

public static string String2Html(string sText) {
// Use System.Net.WebUtility.HtmlEncode (in System.dll, always loaded)
// rather than System.Web.HttpUtility, whose assembly fails to load at
// runtime in this desktop x64 process and crashed the Control+H HTML
// Format command. Same HTML entity-encoding result, no System.Web.
return System.Net.WebUtility.HtmlEncode(sText);
} // String2Html method

public static string ExpandCommandLine(string sCommand, string sSource, string sTarget ) {
// Dialog.Show(sTarget);

sCommand = sCommand.Replace("%NetDirLong%", App.NetDir);
sCommand = sCommand.Replace("%NetDir%", Win32.GetShortPath(App.NetDir));

sCommand = sCommand.Replace("%ProgDirLong%", App.ProgramDir);
sCommand = sCommand.Replace("%ProgDir%", Win32.GetShortPath(App.ProgramDir));

sCommand = sCommand.Replace("%DataDirLong%", App.DataDir);
sCommand = sCommand.Replace("%DataDir%", Win32.GetShortPath(App.DataDir));

if (sSource.Length > 0) {
sCommand = sCommand.Replace("%SourceLong%", sSource);
sCommand = sCommand.Replace("%Source%", Win32.GetShortPath(sSource));

sCommand = sCommand.Replace("%SourceDirLong%", Path.GetDirectoryName(sSource));
sCommand = sCommand.Replace("%SourceDir%", Win32.GetShortPath(Path.GetDirectoryName(sSource)));

sCommand = sCommand.Replace("%SourceNameLong%", Path.GetFileName(sSource));
sCommand = sCommand.Replace("%SourceName%", Path.GetFileName(Win32.GetShortPath(sSource)));

sCommand = sCommand.Replace("%SourceRootLong%", Path.GetFileNameWithoutExtension(sSource));
sCommand = sCommand.Replace("%SourceRoot%", Path.GetFileNameWithoutExtension(Win32.GetShortPath(sSource)));

sCommand = sCommand.Replace("%SourceExtLong%", Path.GetExtension(sSource));
sCommand = sCommand.Replace("%SourceExt%", Path.GetExtension(Win32.GetShortPath(sSource)));
}

if (sTarget.Length > 0) {
sCommand = sCommand.Replace("%TargetLong%", sTarget);
// Dialog.Show(sCommand, "");
sCommand = sCommand.Replace("%Target%", Win32.GetShortPath(sTarget));
// Dialog.Show(sCommand, "");

sCommand = sCommand.Replace("%TargetDirLong%", Path.GetDirectoryName(sTarget));
sCommand = sCommand.Replace("%TargetDir%", Win32.GetShortPath(Path.GetDirectoryName(sTarget)));

sCommand = sCommand.Replace("%TargetNameLong%", Path.GetFileName(sTarget));
sCommand = sCommand.Replace("%TargetName%", Path.GetFileName(Win32.GetShortPath(sTarget)));

sCommand = sCommand.Replace("%TargetRootLong%", Path.GetFileNameWithoutExtension(sTarget));
sCommand = sCommand.Replace("%TargetRoot%", Path.GetFileNameWithoutExtension(Win32.GetShortPath(sTarget)));

sCommand = sCommand.Replace("%TargetExtLong%", Path.GetExtension(sTarget));
sCommand = sCommand.Replace("%TargetExt%", Path.GetExtension(Win32.GetShortPath(sTarget)));
}

sCommand = sCommand.Replace("%TempFile%", App.TempFile);
sCommand = Environment.ExpandEnvironmentVariables(sCommand);
return sCommand.Trim();
} // ExpandCommandLine method

public static string GetProgramOutput(string sExe, string sParams) {
Process process = new Process();
ProcessStartInfo startInfo = new ProcessStartInfo(sExe, sParams);
startInfo.UseShellExecute = false;
//startInfo.RedirectStandardInput = true;
startInfo.RedirectStandardOutput = true;
startInfo.RedirectStandardError = true;
startInfo.WorkingDirectory = Path.GetDirectoryName(sExe);
startInfo.ErrorDialog = true;
startInfo.CreateNoWindow = true;
startInfo.WindowStyle = ProcessWindowStyle.Hidden;
process.StartInfo = startInfo;
process.Start();
StreamReader stream = process.StandardOutput;
process.WaitForExit();
string sText = stream.ReadToEnd();
stream.Close();
process.Close();
return sText;
} // GetProgramOutput method

public static bool ConvertString2FileFormat(string sText, string sTarget, string sTargetFormat) {
string sSourceFormat = "";
return ConvertString2FileFormat(sText, sSourceFormat, sTarget, sTargetFormat);
} // ConvertString2FileFormat method

public static bool ConvertString2FileFormat(string sText, string sSourceFormat, string sTarget, string sTargetFormat) {
/*
string sSource = Path.GetTempFileName();
if (sSourceFormat.Length > 0) {
string s = Path.ChangeExtension(sSource, sSourceFormat);
if (File.Exists(s)) File.Delete(s);
File.Move(sSource, s);
sSource = s;
}

//sSource = @"C:\edsharp\edsharp.htm";
App.TempFiles.Add(sSource);
//Util.String2File(sText, sSource);
//Util.StringAppend2File(sText, sSource);
*/

string sDir = Path.Combine(App.DataDir, "Temp");
if (Directory.Exists(sDir)) Util.DeleteDirectory(sDir, false);
Directory.CreateDirectory(sDir);
string sSource = Path.Combine(sDir, "Source.tmp");
if (sSourceFormat.Length > 0) sSource = Path.ChangeExtension(sSource, sSourceFormat);

Util.String2FileA(sText, sSource);
sSource = Win32.GetShortPath(sSource);
// Dialog.Show(sSource, Util.File2String(sSource));
string sCommand = Ini.ReadValue(App.IniFile, "Export", sTargetFormat, "");
//Dialog.Show(sCommand);
if (sCommand.Length > 0) {
sCommand = Util.ExpandCommandLine(sCommand, sSource, sTarget);
// Dialog.Show("show", sCommand);
App.Frame.AddMessage("Converting");
if (File.Exists(sTarget)) File.Delete(sTarget);
Util.RunHideWait(sCommand);
if (!File.Exists(sTarget)) {
sCommand = "cmd.exe /c " + sCommand;
Util.RunHideWait(sCommand);
}
sText = "";
if (File.Exists(sTarget)) sText = Util.File2String(sTarget);
if (sText.Length == 0) {
if (File.Exists(sTarget)) File.Delete(sTarget);
// Do 5.0.84 uzytkownik dostawal tu SAM wiersz polecenia - z niego nie
// wynikalo, ze konwersja nie doszla, bo BRAKUJE narzedzia.  Teraz
// najpierw sprawdzam, czy to nie ten wlasnie przypadek, i mowie wprost,
// czego brakuje oraz co zrobic.
string sBrak = Skladniki.BrakujaceDlaPolecenia(sCommand);
if (sBrak.Length > 0) {
Dialog.Show("Brakuje skladnika", "Konwersja wymaga narzedzia " + sBrak
  + ", ktorego nie ma w folderze programu.\n\n"
  + "Program dociaga skladniki sam przy starcie, gdy jest internet. "
  + "Mozesz tez pobrac je od razu: menu Tools, Update Components.");
}
else Dialog.Show("Error", "Command line:\n" + sCommand);
}
}
else {
COM.WordSource2TargetFormat(sSource, sTarget, sTargetFormat);
}
//Dialog.Show("Error", "Command line:\n" + sCommand);
App.Frame.Activate();
return File.Exists(sTarget);
} // ConvertString2FileFormat method

public static string Literalize(string sText) {
bool bCheckPrefix = false;
return Literalize(sText, bCheckPrefix);
} // Literalize method

public static string Literalize(string sText, bool bCheckPrefix) {
if (bCheckPrefix) {
if (sText.StartsWith("@")) return sText.Substring(1);
else if (sText.StartsWith(@"\@")) sText = sText.Substring(1);
}
string sReturn = null;
try {
sReturn = Script.run("\"" + sText + "\"");
}
catch {}

//string sReturn = JS.Eval("\"" + sText + "\"").ToString();
//if (sReturn.Length == 0) sReturn = sText;
if (sReturn == null) sReturn = sText;
return sReturn;
} // Literalize method

public static string Reverse(string sText) {
/*
string[] a = sText.Split();
Array.Reverse(a);
sText = String.Join("", a);
*/
int iLength = sText.Length;
StringBuilder sb = new StringBuilder(iLength);
for (int i = iLength - 1; i >= 0; i--) sb.Append(sText.Substring(i, 1));
sText = sb.ToString();
return sText;
} // Reverse method

public static int Absolute(int i) {
if (i < 0) i = -1 * i;
return i;
} // Absolute method

public static bool IsNumeric(string sText) {
double dValue; return double.TryParse(sText, out dValue);
} // IsNumeric method

public static bool IsDate(string sText) {
DateTime dtValue; return DateTime.TryParse(sText, out dtValue);
} // IsDate method

public static bool IsNothing(string sText) {
return sText == null;
} // IsNothing method

public static string Left(string sText, int iChars) {
if (sText == null) return sText;
if (iChars < 0) iChars = 0;
return (iChars >= sText.Length) ? sText : sText.Substring(0, iChars);
} // Left method

public static string Right(string sText, int iChars) {
if (sText == null) return sText;
if (iChars < 0) iChars = 0;
return (iChars >= sText.Length) ? sText : sText.Substring(sText.Length - iChars);
} // Right method

public static Font SetBold(Font font, bool bState) {
return new Font(font, bState ? font.Style | FontStyle.Bold : font.Style & ~FontStyle.Bold);
} // SetBold method

public static Font SetItalic(Font font, bool bState) {
return new Font(font, bState ? font.Style | FontStyle.Italic : font.Style & ~FontStyle.Italic);
} // SetItalic method

public static Font SetUnderline(Font font, bool bState) {
return new Font(font, bState ? font.Style | FontStyle.Underline : font.Style & ~FontStyle.Underline);
} // SetUnderline method

public static string GetFileFromUri(string sUri) {
string sFile;
Uri oUri = new Uri(sUri);
//if (oUri.IsFile) {
sFile = oUri.LocalPath;
try {
sFile = Path.GetFileName(sFile);
}
catch {
sFile = "";
}
//else {
if (sFile.Length == 0) {
sFile = oUri.PathAndQuery;
sFile = Uri.UnescapeDataString(sFile);
StringBuilder sb = new StringBuilder();
for (int i = 0; i < sFile.Length; i++) {
if (Char.IsLetterOrDigit(sFile, i)) sb.Append(sFile.Substring(i, 1));
else sb.Append("_");
}
sFile = sb.ToString();
sFile = Util.RegExpReplaceCase(sFile, @"_+", "_");
sFile = sFile.Trim(new Char[] {'_', ' '});
if (sFile.Length == 0) sFile = "page";
if (!sFile.ToLower().EndsWith(".htm") && !sFile.ToLower().EndsWith(".html")) sFile += ".htm";
}
if (Path.GetExtension(sFile).Length == 0) sFile += ".htm";
return sFile;
} // GetFileFromUri method

public static string GetUniqueName(string sSource) {
if (!Directory.Exists(sSource) && !File.Exists(sSource)) return sSource;
string sTarget = "";
string sDir = Path.GetDirectoryName(sSource);
string sRoot = Path.GetFileNameWithoutExtension(sSource);
sRoot = Regex.Replace(sRoot, @"_\d\d$", "");
//Regex rx = new Regex(@"_\d\d$");
//sRoot = rx.Replace(sRoot, "");
string sExt = Path.GetExtension(sSource);
//for (int i = 1; i < 100; i++) {
for (int i = 1; i < 10000; i++) {
//string sNewName = sRoot + "_" + i.ToString().PadLeft(2, '0') + sExt;
string sNewName = sRoot + "_" + i.ToString().PadLeft(4, '0') + sExt;
sTarget = Path.Combine(sDir, sNewName);
if (!Directory.Exists(sTarget) && !File.Exists(sTarget)) break;
}
//if (Directory.Exists(sTarget) || File.Exists(sTarget)) sTarget = "";
return sTarget;
} // GetUniqueName method

public static void Swap(ref int i1, ref int i2) {
int i  = i1;
i1 = i2;
i2 = i;
} // Swap method

public static char Code2Char(int iCode) {
return (char) iCode;
} // Code2Char method

public static string Code2String(int iCode) {
return Code2Char(iCode).ToString();
} // Code2String method

} // Util class

public class HomerList : List<string> {

public char Delimiter = '|';
public bool CaseSensitive = false;

public int Max {
get {
return this.Count - 1;
}
}

public string Segments {
get {
string[] aSegments = this.ToArray();
string sSegments = String.Join(this.Delimiter.ToString(), aSegments);
return sSegments;
}
set {
string[] aSegments = value.Split(this.Delimiter);
this.Clear();
if (value.Length > 0) this.AddRange(aSegments);
}
} // Segments property

public HomerList() {
//new HomerList(this.Segments, this.Delimiter, this.CaseSensitive);
} // HomerList constructor

public HomerList(string sSegments) {
//new HomerList(sSegments, this.Delimiter, this.CaseSensitive);
this.Segments = sSegments;
//new HomerList();
} // HomerList constructor

public HomerList(string sSegments, char cDelimiter) {
this.Delimiter = cDelimiter;
this.Segments = sSegments;
} // HomerList constructor

public HomerList(string sSegments, char cDelimiter, bool bCaseSensitive) {
this.Delimiter = cDelimiter;
this.Segments = sSegments;
this.CaseSensitive = bCaseSensitive;
} // HomerList constructor

public HomerList(string[] aItems) {
this.AddRange(aItems);
} // HomerList constructor

public new int IndexOf(string sItem) {
if (this.CaseSensitive) return base.IndexOf(sItem);
else {
int iIndex = -1;
string sValue = sItem.ToLower();
for (int i = 0; i < this.Count; i++) {
if (this[i].ToLower() == sValue) {
iIndex = i;
break;
}
}
return iIndex;
}
} // IndexOf method

public new bool Contains(string sItem) {
return this.IndexOf(sItem) >= 0;
} // Contains method

public new void Sort() {
if (this.CaseSensitive) base.Sort();
else {
string[] a = this.ToArray();
Array.Sort(a, new CaseInsensitiveComparer());
this.Clear();
this.AddRange(a);
}
} // Sort method

public string GetSegments(char cDelimiter) {
this.Delimiter = cDelimiter;
return this.Segments;
} // GetSegments method

public void KeepUnique() {
for (int i = this.Count - 1; i >=0; i--) {
string s = this[i];
if (this.IndexOf(s) < i) this.RemoveAt(i);
}
} // KeepUnique method

public void RemoveLike(string sMatch) {
RegexOptions options = RegexOptions.Multiline;
if (!this.CaseSensitive) options |= RegexOptions.IgnoreCase;
Regex rx = new Regex(sMatch, options);

for (int i = this.Count - 1; i >= 0; i--) {
if (rx.IsMatch(this[i])) this.RemoveAt(i);
}
} // RemoveLike method

public void KeepLike(string sMatch) {
HomerList hl = this.FindLike(sMatch);
this.Clear();
this.AddRange(hl);
} // KeepLike method

public HomerList FindLike(string sMatch) {
RegexOptions options = RegexOptions.Multiline;
if (!this.CaseSensitive) options |= RegexOptions.IgnoreCase;
Regex rx = new Regex(sMatch, options);

HomerList hl = new HomerList();
foreach (string s in this) {
if (rx.IsMatch(s)) hl.Add(s);
}
return hl;
} // FindLike method

public void ReplaceLike(string sMatch, string sReplace) {
RegexOptions options = RegexOptions.Multiline;
if (!this.CaseSensitive) options |= RegexOptions.IgnoreCase;
Regex rx = new Regex(sMatch, options);

for (int i = 0; i < this.Count; i++)  this[i] = rx.Replace(this[i], sReplace);
} // ReplaceLike method

public void Push(string sItem) {
this.Insert(0, sItem);
} // Push method

public string Pop() {
int iUpper = this.Count - 1;
string sItem = this[iUpper];
this.RemoveAt(iUpper);
return sItem;
} // Pop method

public string Shift() {
int iLower = 0;
string sItem = this[iLower];
this.RemoveAt(iLower);
return sItem;
} // Shift method

public new void Remove(string sItem) {
bool bLoop = true;
while (bLoop) {
int iIndex = this.IndexOf(sItem);
if (iIndex == -1) break;
this.RemoveAt(iIndex);
}
} // Remove method

public void RemoveRange(HomerList hl) {
foreach (string sItem in hl) this.Remove(sItem);
} // RemoveRange method

public void RemoveRange(string[] aItems) {
HomerList hl = new HomerList(aItems);
this.RemoveRange(hl);
} // RemoveRange method

public void AddUnique(string sItem) {
if (!this.Contains(sItem)) this.Add(sItem);
} // AddUnique method

public void PushUnique(string sItem) {
if (!this.Contains(sItem)) this.Push(sItem);
} // PushUnique method

public void RemoveRange(string sSegments) {
Char cDelimiter = '|';
HomerList hl = new HomerList(sSegments, cDelimiter);
this.RemoveRange(hl);
} // RemoveRange method

public void RemoveRange(string sSegments, Char cDelimiter) {
HomerList hl = new HomerList(sSegments, cDelimiter);
this.RemoveRange(hl);
} // RemoveRange method

public void saveAddRange(HomerList hl) {
this.AddRange(hl);
} // AddRange method

public void OldAddRange(string[] aItems) {
HomerList hl = new HomerList(aItems);
this.AddRange(hl);
} // AddRange method

public void AddRange(string sSegments) {
Char cDelimiter = this.Delimiter;
HomerList hl = new HomerList(sSegments, cDelimiter);
this.AddRange(hl);
} // AddRange method

public void AddRange(string sSegments, Char cDelimiter) {
HomerList hl = new HomerList(sSegments, cDelimiter);
this.AddRange(hl);
} // AddRange method

public void AddUniqueRange(HomerList hl) {
foreach (string s in hl) if (!this.Contains(s)) this.Add(s);
} // AddUniqueRange method

public void AddUniqueRange(string[] aItems) {
HomerList hl = new HomerList(aItems);
this.AddUniqueRange(hl);
} // AddUniqueRange method

public void AddUniqueRange(string sSegments) {
Char cDelimiter = this.Delimiter;
HomerList hl = new HomerList(sSegments, cDelimiter);
this.AddUniqueRange(hl);
} // AddUniqueRange method

public void AddUniqueRange(string sSegments, Char cDelimiter) {
HomerList hl = new HomerList(sSegments, cDelimiter);
this.AddUniqueRange(hl);
} // AddUniqueRange method

public HomerList FindRange(HomerList hl) {
HomerList hlReturn = new HomerList();
foreach (string sItem in hl) if (this.Contains(sItem)) hlReturn.Add(sItem);
return hlReturn;
} // FindRange method

public void FindRange(string[] aItems) {
HomerList hl = new HomerList(aItems);
this.FindRange(hl);
} // FindRange method

public void FindRange(string sSegments) {
Char cDelimiter = '|';
HomerList hl = new HomerList(sSegments, cDelimiter);
this.FindRange(hl);
} // FindRange method

public void FindRange(string sSegments, Char cDelimiter) {
HomerList hl = new HomerList(sSegments, cDelimiter);
this.FindRange(hl);
} // FindRange method

public HomerList Clone() {
string[] aItems = this.ToArray();
HomerList hl = new HomerList(aItems);
return hl;
} // Clone method

public string MinValue() {
if (this.Count == 0) return "";

HomerList hl = this.Clone();
hl.Sort();
return hl[0];
} // MinValue method

public string MaxValue() {
if (this.Count == 0) return "";

HomerList hl = this.Clone();
hl.Sort();
return hl[hl.Count - 1];
} // MaxValue method

public int MinLength() {
int iLength = 2000000000;
foreach (string sItem in this) if (sItem.Length < iLength) iLength = sItem.Length;
if (iLength == 2000000000) iLength = 0;
return iLength;
} // MinLength method

public int MaxLength() {
int iLength = 0;
foreach (string sItem in this) if (sItem.Length > iLength) iLength = sItem.Length;
return iLength;
} // MaxLength method

public void SortLength() {
this.Sort(delegate(string s1, string s2) {
return s1.Length.CompareTo(s2.Length);
} );
} // SortLength method

public void ToLower() {
for (int i = 0; i < this.Count; i++) this[i] = this[i].ToLower();
} // ToLower method

public void ToUpper() {
for (int i = 0; i < this.Count; i++) this[i] = this[i].ToUpper();
} // ToUpper method

public void TrimStart() {
for (int i = 0; i < this.Count; i++) this[i] = this[i].TrimStart();
} // TrimStart method

public void TrimEnd() {
for (int i = 0; i < this.Count; i++) this[i] = this[i].TrimEnd();
} // TrimEnd method

public void TrimStart(char[] a) {
for (int i = 0; i < this.Count; i++) this[i] = this[i].TrimStart(a);
} // TrimStart method

public void TrimEnd(char[] a) {
for (int i = 0; i < this.Count; i++) this[i] = this[i].TrimEnd(a);
} // TrimEnd method

public void PadLeft(int iLength, char c) {
for (int i = 0; i < this.Count; i++) this[i] = this[i].PadLeft(iLength, c);
} // PadLeft method

public void PadRight(int iLength, char c) {
for (int i = 0; i < this.Count; i++) this[i] = this[i].PadRight(iLength, c);
} // PadRight method

public void PushRange(HomerList hl) {
this.InsertRange(0, hl);
} // PushRange method

public void PushRange(string sSegments) {
Char cDelimiter = this.Delimiter;
HomerList hl = new HomerList(sSegments, cDelimiter);
this.PushRange(hl);
} // PushRange method

public void PushRange(string sSegments, Char cDelimiter) {
HomerList hl = new HomerList(sSegments, cDelimiter);
this.PushRange(hl);
} // PushRange method

public void PushUniqueRange(HomerList hl) {
foreach (string s in hl) if (!this.Contains(s)) this.Push(s);
} // PushUniqueRange method

public void PushUniqueRange(string[] aItems) {
HomerList hl = new HomerList(aItems);
this.PushUniqueRange(hl);
} // PushUniqueRange method

public void PushUniqueRange(string sSegments) {
Char cDelimiter = this.Delimiter;
HomerList hl = new HomerList(sSegments, cDelimiter);
this.PushUniqueRange(hl);
} // PushUniqueRange method

public void PushUniqueRange(string sSegments, Char cDelimiter) {
HomerList hl = new HomerList(sSegments, cDelimiter);
this.PushUniqueRange(hl);
} // PushUniqueRange method

} // HomerList class

public class Segment {
public static bool CaseSensitive = false;
public static char Delimiter = '|';

public int Count(string sSegments) {
string[] aSegments = sSegments.Split(Segment.Delimiter);
return sSegments.Length == 0 ? 0 : aSegments.Length;
} // Count method

public static string Get(string sSegments, int iIndex) {
string[] aSegments = sSegments.Split(Segment.Delimiter);
return aSegments[iIndex];
} // Get method

public static int IndexOf(string sSegments, string sSegment) {
string[] aSegments = sSegments.Split(Segment.Delimiter);
List<string> listSegments = new List<string>(aSegments);
if (!Segment.CaseSensitive) for (int i = 0; i < listSegments.Count; i++) listSegments[i] = listSegments[i].ToLower();
return listSegments.IndexOf(sSegment);
} // IndexOf method

public static bool Contains(string sSegments, string sSegment) {
return IndexOf(sSegments, sSegment) >= 0;
} // Contains method

public static string RemoveAt(string sSegments, int iIndex) {
string[] aSegments = sSegments.Split(Segment.Delimiter);
List<string> listSegments = new List<string>(aSegments);
listSegments.RemoveAt(iIndex);
aSegments = listSegments.ToArray();
return String.Join(Segment.Delimiter.ToString(), aSegments);
} // RemoveAt method

public static string Remove(string sSegments, string sSegment) {
int iIndex = IndexOf(sSegments, sSegment);
return RemoveAt(sSegments, iIndex);
} // Remove method

public static string RemoveIfContains(string sSegments, string sSegment) {
int iIndex = IndexOf(sSegments, sSegment);
if (iIndex >= 0) sSegments = RemoveAt(sSegments, iIndex);
return sSegments;
} // RemoveIfContains method

public static string Insert(string sSegments, int iIndex, string sSegment) {
string[] aSegments = sSegments.Split(Segment.Delimiter);
List<string> listSegments = new List<string>(aSegments);
listSegments.Insert(iIndex, sSegment);
aSegments = listSegments.ToArray();
return String.Join(Segment.Delimiter.ToString(), aSegments);
} // Insert method

public static string Add(string sSegments, string sSegment) {
string[] aSegments = sSegments.Split(Segment.Delimiter);
List<string> listSegments = new List<string>(aSegments);
listSegments.Add(sSegment);
aSegments = listSegments.ToArray();
return String.Join(Segment.Delimiter.ToString(), aSegments);
} // Add method

public static string AddIfUnique(string sSegments, string sSegment) {
if (!Contains(sSegments, sSegment)) sSegments = Add(sSegments, sSegment);
return sSegments;
} // AddIfUnique method

public static string ReplaceAt(string sSegments, int iIndex, string sSegment) {
string[] aSegments = sSegments.Split(Segment.Delimiter);
List<string> listSegments = new List<string>(aSegments);
listSegments[iIndex] = sSegment;
aSegments = listSegments.ToArray();
return String.Join(Segment.Delimiter.ToString(), aSegments);
} // ReplaceAt method

public static string Replace(string sSegments, string sSegment) {
int iIndex = IndexOf(sSegments, sSegment);
return ReplaceAt(sSegments, iIndex, sSegment);
} // Replace method

public static string ReplaceIfContains(string sSegments, string sSegment) {
int iIndex = IndexOf(sSegments, sSegment);
if (iIndex >= 0) sSegments = ReplaceAt(sSegments, iIndex, sSegment);
return sSegments;
} // ReplaceIfContains method

public static string Sort(string sSegments) {
string[] aSegments = sSegments.Split(Segment.Delimiter);
List<string> listSegments = new List<string>(aSegments);
if (!Segment.CaseSensitive) for (int i = 0; i < listSegments.Count; i++) listSegments[i] = listSegments[i].ToLower();
string[] aKeys = listSegments.ToArray();
Array.Sort(aKeys, aSegments);
return String.Join(Segment.Delimiter.ToString(), aSegments);
} // Sort method

public static string Unique(string sSegments) {
string[] aSegments = sSegments.Split(Segment.Delimiter);
List<string> listSegments = new List<string>();
if (Segment.CaseSensitive) foreach (string sSegment in aSegments) if (!listSegments.Contains(sSegment)) listSegments.Add(sSegment);
else {
List<string> listLower = new List<string>();
foreach (string s in aSegments) {
if (!listLower.Contains(s.ToLower())) {
listSegments.Add(s);
listLower.Add(s.ToLower());
}
}
}

aSegments = listSegments.ToArray();
return String.Join(Segment.Delimiter.ToString(), aSegments);
} // Unique method

} // Segment class

} // EdSharp namespace
