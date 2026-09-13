# POMIAR: co robi Control+O na roznych formatach w 5.0.99.
#
# Control+O jest od 5.0.99 jedynym otwieraniem i sam decyduje, czy plik pokazac
# surowo, czy przepuscic przez konwersje.  Lista polecen w kodzie tego NIE
# dowodzi - dowodzi tego tylko otwarty plik.  Sonda dla kazdej probki: uruchamia
# program, wola Control+O, podaje sciezke, a potem patrzy, CO widzi uzytkownik:
# albo okno wyboru formatu (czyta jego tytul), albo tresc w oknie edycji
# (kopiuje ja przez Control+A / Control+C i pokazuje pierwsze znaki).
#
# UZYCIE: powershell -ExecutionPolicy Bypass -File zywe_ctrl_o.ps1
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms

Add-Type @"
using System;
using System.Text;
using System.Runtime.InteropServices;
public class W {
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, IntPtr pid);
  [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
  [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr lParam);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  public delegate bool EnumProc(IntPtr h, IntPtr lParam);
  // UCHWYT OKNA BIERZEMY Z ENUMWINDOWS, NIE Z MainWindowHandle.  Zmierzone:
  // dla EdSharpa .MainWindowHandle zwraca 0, mimo ze okno istnieje i ma tytul
  // (formularz MDI zglasza sie inaczej, niz zaklada ta wlasciwosc .NET).  Sonda
  // wysylala wiec klawisze "w zero" i konczyla sie falszywym niepowodzeniem.
  // Pytamy system: pierwsze WIDOCZNE okno naleace do naszego numeru procesu.
  public static IntPtr OknoProcesu(int pid) {
    IntPtr znalezione = IntPtr.Zero;
    EnumWindows(delegate(IntPtr h, IntPtr l) {
      uint wlasciciel = 0;
      GetWindowThreadProcessIdOut(h, out wlasciciel);
      if (wlasciciel == (uint) pid && IsWindowVisible(h)) {
        StringBuilder sb = new StringBuilder(512);
        GetWindowText(h, sb, 512);
        if (sb.Length > 0) { znalezione = h; return false; }
      }
      return true;
    }, IntPtr.Zero);
    return znalezione;
  }
  [DllImport("user32.dll", EntryPoint="GetWindowThreadProcessId")]
  public static extern uint GetWindowThreadProcessIdOut(IntPtr h, out uint pid);
  // FOKUS TRZEBA WYMUSZAC, NIE ZAKLADAC.  Pierwsza wersja tej sondy wysylala
  // klawisze "do tego, co na wierzchu" i cicho zmierzyla CUDZE okno: na pulpicie
  // wisial instalator poprzedniej wersji z podwyzszonymi prawami, wiec kazda
  // probka wyszla identycznie i wynik byl bezwartosciowy.  Dlatego przed kazda
  // proba aktywujemy okno programu i SPRAWDZAMY, czy naprawde jest na wierzchu.
  //
  // Samo SetForegroundWindow NIE WYSTARCZA: Windows odrzuca je, gdy wolajacy
  // proces nie jest aktywny (a nasz, uruchomiony z konsoli w tle, nie jest) -
  // zmierzone, okno konsoli zostawalo na wierzchu.  Obejscie jest podrecznikowe:
  // na chwile DOLACZAMY swoja kolejke wejscia do watku okna aktywnego, wtedy
  // system traktuje nas jak czesc aktywnej aplikacji i zgode wydaje.
  public static bool NaWierzch(IntPtr h) {
    uint mojWatek = GetCurrentThreadId();
    uint obcyWatek = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
    if (obcyWatek != 0 && obcyWatek != mojWatek) AttachThreadInput(mojWatek, obcyWatek, true);
    ShowWindow(h, 9);
    BringWindowToTop(h);
    SetForegroundWindow(h);
    System.Threading.Thread.Sleep(500);
    if (obcyWatek != 0 && obcyWatek != mojWatek) AttachThreadInput(mojWatek, obcyWatek, false);
    return GetForegroundWindow() == h;
  }
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
  public static string Title() {
    StringBuilder sb = new StringBuilder(512);
    GetWindowText(GetForegroundWindow(), sb, 512);
    return sb.ToString();
  }
  public static string Klasa() {
    StringBuilder sb = new StringBuilder(512);
    GetClassName(GetForegroundWindow(), sb, 512);
    return sb.ToString();
  }
}
"@

$exe = "C:\Program Files\EdSharpNG\EdSharpNG.exe"
$katalog = "C:\Users\Michal\probki_599"
$probki = @("proba.txt", "proba.md", "proba.rst", "proba.html", "proba.docx", "proba.epub", "proba.rtf")

function Wyslij($s) { [System.Windows.Forms.SendKeys]::SendWait($s); Start-Sleep -Milliseconds 400 }

foreach ($probka in $probki) {
    Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Milliseconds 700
    $proc = Start-Process $exe -PassThru
    # NA OKNO SIE CZEKA W PETLI, NIE NA SLEPO.  Przy stalym odczekaniu 3 sekund
    # okno czasem jeszcze nie istnialo (MainWindowHandle = 0) i sonda meldowala
    # "nie znalazlem okna" dla dzialajacego programu - falszywe niepowodzenie
    # zalezne od obciazenia maszyny.  Pytamy wiec do 15 sekund, do pierwszego
    # prawdziwego uchwytu.
    $okno = [IntPtr]::Zero
    for ($i = 0; $i -lt 30; $i++) {
        Start-Sleep -Milliseconds 500
        $proc.Refresh()
        if ($proc.HasExited) { break }
        if ($proc.MainWindowHandle -ne [IntPtr]::Zero) { $okno = $proc.MainWindowHandle; break }
        $z = [W]::OknoProcesu($proc.Id)
        if ($z -ne [IntPtr]::Zero) { $okno = $z; break }
    }
    if ($okno -eq [IntPtr]::Zero) {
        Write-Output ("{0,-12} -> POMIAR NIEWAZNY: okno EdSharpNG nie pojawilo sie w 15 s" -f $probka)
        continue
    }
    Start-Sleep -Seconds 1

    # BEZ POTWIERDZONEGO FOKUSU POMIAR NIE MA WARTOSCI - patrz komentarz w W.NaWierzch.
    if (-not [W]::NaWierzch($okno)) {
        Write-Output ("{0,-12} -> POMIAR NIEWAZNY: nie udalo sie ustawic okna EdSharpa na wierzchu (na wierzchu: '{1}')" -f $probka, [W]::Title())
        continue
    }

    Wyslij "^o"
    Start-Sleep -Seconds 1
    $tytulOkna = [W]::Title()
    # TYTUL OKNA SYSTEMOWEGO JEST W JEZYKU WINDOWSA.  Pierwsza wersja pytala o
    # "Open" i odrzucala kazdy pomiar, bo Windows na tej maszynie jest po polsku
    # i pisze "Otwieranie".  Dopuszczamy oba, a rozstrzygamy po KLASIE okna
    # (#32770 to standardowy dialog), zeby jezyk nie decydowal o pomiarze.
    if ($tytulOkna -notmatch "Open|Otwieranie" -and [W]::Klasa() -ne "#32770") {
        Write-Output ("{0,-12} -> POMIAR NIEWAZNY: po Control+O na wierzchu jest '{1}' (klasa {2}), nie okno otwarcia" -f $probka, $tytulOkna, [W]::Klasa())
        continue
    }
    # Pole nazwy pliku w oknie otwarcia ma fokus; podajemy pelna sciezke.
    Wyslij ($katalog + "\" + $probka)
    Wyslij "{ENTER}"
    Start-Sleep -Seconds 3

    $tytul = [W]::Title()
    $klasa = [W]::Klasa()
    # Okno edycji EdSharpa to formularz MDI; okno wyboru formatu jest osobnym
    # dialogiem, wiec rozstrzyga sama nazwa w pasku tytulu.
    if ($tytul -match "^Import|^Open RTF File As") {
        Write-Output ("{0,-12} -> PYTA: '{1}'" -f $probka, $tytul)
        # Odczytujemy tez pozycje listy, bo to one mowia uzytkownikowi, co dostanie.
        Wyslij "{ESC}"
    }
    else {
        [System.Windows.Forms.Clipboard]::Clear()
        Wyslij "^a"
        Wyslij "^c"
        Start-Sleep -Milliseconds 600
        $tresc = ""
        try { $tresc = [System.Windows.Forms.Clipboard]::GetText() } catch { $tresc = "(schowek pusty)" }
        $tresc = ($tresc -replace "`r`n", " / " -replace "`n", " / ")
        if ($tresc.Length -gt 90) { $tresc = $tresc.Substring(0, 90) + "..." }
        Write-Output ("{0,-12} -> OTWARTE bez pytania | tytul '{1}' | tresc: {2}" -f $probka, $tytul, $tresc)
    }
}

Get-Process EdSharpNG -ErrorAction SilentlyContinue | Stop-Process -Force
Write-Output "KONIEC POMIARU"
