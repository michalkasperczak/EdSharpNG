// Pomiar: PRZYPISY W PODGLADZIE POD ESCAPE.
//
// Zlecenie 1787840875741-1 (Kasperczak, 27.08.2026 16:27): przypisy w
// podgladzie maja dzialac "jak takie skip linki, czyli on mowi numer przypisu,
// na tym numerze przypisu naciskam Enter (...) on przeskakuje do tresci
// przypisu (...) i wraca do tego tekstu".  Decyzja o kursorze z 16:56:
// "chyba jednak bardziej naturalnie przeniesc".
//
// MIERZYMY DWIE RZECZY, bo obie moga sie zepsuc niezaleznie:
//   1. RENDER - czym znacznik JEST w podgladzie (czytnik ma go wymowic),
//   2. MAPE WIDOK->ZRODLO - czy kursor postawiony na tym napisie wraca na
//      znacznik w zrodle.  Bez tego Enter dzialalby na zlej pozycji, a to
//      wlasnie tam siedzial defekt: "footnote 1" jest DLUZSZE niz "[^1]",
//      wiec powstaja pozycje widoku bez odpowiednika w zrodle.
//
// Wszystko na ZBUDOWANEJ binarce przez refleksje, nie na kopii kodu.
//
// KONTROLE WAZNOSCI: ten sam render MUSI ukrywac kratki naglowka i gwiazdki
// pogrubienia (sonda nie jest glucha), a znacznik w BLOKU KODU MUSI zostac
// surowy (render nie zamienia wszystkiego jak leci).
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

public class PomiarPodgladuPrzypisow {

static int iPass = 0;
static int iFail = 0;

static void Ok(bool b, string sName, string sGot) {
if (b) {iPass++; Console.WriteLine("PASS  " + sName);}
else {iFail++; Console.WriteLine("FAIL  " + sName + "  -> " + sGot);}
}

static MethodInfo miRender = null;

// Zwraca tekst widoku i OBIE mapy.
static string Render(string sDoc, out int[] aSourceToView, out int[] aViewToSource) {
object[] aArgs = new object[] {sDoc, null, null, null};
string s = (string) miRender.Invoke(null, aArgs);
aSourceToView = (int[]) aArgs[2];
aViewToSource = (int[]) aArgs[3];
return s;
}

public static int Main(string[] args) {
Assembly asm = Assembly.LoadFrom(Path.GetFullPath("EdSharpNG.exe"));
Type tFrame = asm.GetType("EdSharp.MdiFrame");
if (tFrame == null) {Console.WriteLine("Brak typu EdSharp.MdiFrame"); return 3;}

miRender = tFrame.GetMethod("MarkdownReview_RenderTextForView",
  BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
if (miRender == null) {Console.WriteLine("Brak metody MarkdownReview_RenderTextForView"); return 3;}

MethodInfo miMarker = tFrame.GetMethod("MarkdownReview_FootnoteMarkerText",
  BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
if (miMarker == null) {Console.WriteLine("Brak metody MarkdownReview_FootnoteMarkerText"); return 3;}
string sMarker = (string) miMarker.Invoke(null, new object[] {"1"});
Console.WriteLine("Znacznik w podgladzie brzmi: \"" + sMarker + "\"");
Ok(sMarker == " footnote 1", "napis znacznika to \" footnote 1\" ZE SPACJA wiodaca", "[" + sMarker + "]");
// Spacja wiodaca jest WYMOGIEM, nie ozdoba: bez niej NVDA wymawialo
// "przypisemfootnote 1" jednym slowem (zmierzone na zywo 27.08).
Ok(sMarker.StartsWith(" "), "znacznik ma spacje rozdzielajaca od poprzedniego wyrazu", "[" + sMarker + "]");

// KONTROLA NEGATYWNA: wymyslona metoda MUSI nie istniec.  Bez niej
// "znalazlem metode" nie dowodzi niczego o tej binarce.
MethodInfo miNiema = tFrame.GetMethod("MarkdownReview_FootnoteMarkerTextNiemaTakiej",
  BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
Ok(miNiema == null, "KONTROLA NEGATYWNA: wymyslona metoda NIE istnieje", "istnieje");

// ---------- 1. RENDER ZWYKLEGO ZDANIA ----------
string sDoc = "# Tytul\n\nZdanie z **grubym** i przypisem[^1].\n\n[^1]: Tresc przypisu.\n";
int[] aS2V, aV2S;
string sView = Render(sDoc, out aS2V, out aV2S);
Console.WriteLine("--- PODGLAD WIDZI TO ---");
Console.WriteLine(sView);
Console.WriteLine("--- KONIEC ---");

Ok(!sView.Contains("# Tytul"), "KONTROLA: kratka naglowka UKRYTA", "kratka widoczna");
Ok(sView.Contains("Tytul"), "KONTROLA: tekst naglowka widoczny", "brak tytulu");
Ok(!sView.Contains("**grubym**"), "KONTROLA: gwiazdki pogrubienia UKRYTE", "gwiazdki widoczne");

Ok(!sView.Contains("[^1]."), "znacznik NIE jest surowym [^1] w zdaniu", "surowy znacznik w zdaniu");
Ok(sView.Contains("Zdanie z grubym i przypisem" + sMarker + "."),
  "znacznik brzmi \"footnote 1\" w miejscu, gdzie stal", sView);
Ok(sView.Contains("Tresc przypisu."), "tresc przypisu jest w podgladzie widoczna", "brak tresci");

// WIERSZ TRESCI TEZ TRACI NAWIAS I DASZEK - i to jest zamierzone, choc
// wyszlo z pomiaru, nie z planu.  Punkt 16.16 mojej listy brzmial "czytnik
// wymawia nawias i daszek", a to dotyczy OBU miejsc: znacznika w zdaniu i
// etykiety przy tresci na koncu dokumentu.  W widoku brzmi to
// "footnote 1: Tresc przypisu.", czyli po ludzku.
// Rozpoznanie wiersza tresci przez program NIE zalezy od widoku - czyta
// ZRODLO (rtb.Text), wiec skok w obie strony dziala mimo zmiany renderu.
// Asercja nizej pilnuje wlasnie tego, ze dwukropek i tresc zostaja na
// miejscu; bez niego wiersz przestalby byc rozpoznawalny dla czlowieka.
Ok(sView.Contains(sMarker.Trim() + ": Tresc przypisu."),
  "wiersz tresci brzmi \"footnote 1: Tresc przypisu.\"", sView);
Ok(!sView.Contains("[^1]"), "w calym podgladzie NIE ma juz surowego [^1]", sView);

// SPACJA TYLKO TAM, GDZIE POTRZEBNA (poprawka po pierwszym pomiarze): znacznik
// przylegajacy do wyrazu dostaje rozdzielnik, ale etykieta na POCZATKU wiersza
// (tresc przypisu na koncu dokumentu) nie moze dostac wciecia.
Ok(sView.Contains("\nfootnote 1: Tresc przypisu."),
  "wiersz tresci zaczyna sie od \"footnote 1:\" BEZ wciecia", sView);
Ok(!sView.Contains("\n footnote 1:"),
  "KONTROLA: wiersz tresci NIE ma spacji na poczatku", sView);


// ---------- 2. MAPA WIDOK->ZRODLO (tu byl defekt) ----------
int iSrcRef = sDoc.IndexOf("[^1].");
int iViewMarker = sView.IndexOf(sMarker);
Ok(iSrcRef > 0 && iViewMarker > 0, "znaleziono znacznik w zrodle i w widoku",
  "src=" + iSrcRef + " view=" + iViewMarker);

bool bAllBack = true;
string sBad = "";
for (int k = 0; k < sMarker.Length; k++) {
int v = iViewMarker + k;
if (v >= aV2S.Length) {bAllBack = false; sBad = "poza mapa v=" + v; break;}
int iBack = aV2S[v];
if (iBack < iSrcRef || iBack > iSrcRef + 4) {
bAllBack = false;
sBad = "widok " + v + " (znak '" + sView[v] + "') -> zrodlo " + iBack + ", oczekiwano okolic " + iSrcRef;
break;
}
}
Ok(bAllBack, "kazda pozycja napisu \"footnote 1\" wraca na znacznik w zrodle", sBad);

// KONTROLA DYSKRYMINACYJNA MAPY: gdyby dziury byly wypelniane zerem (stary
// blad), pozycja w SRODKU napisu wracalaby na 0.  Sprawdzamy to wprost.
int iMid = iViewMarker + sMarker.Length / 2;
Ok(iMid < aV2S.Length && aV2S[iMid] != 0,
  "KONTROLA: srodek napisu NIE wraca na poczatek dokumentu", "wraca na 0 - dziura w mapie");

// Mapa musi miec dokladnie dlugosc tekstu widoku + 1, inaczej synchronizacja
// jej NIE UZYJE (warunek w MarkdownReview_SyncFromView) i cala poprawka bylaby
// martwa przy dzialajacym renderze.
Ok(aV2S.Length == sView.Length + 1,
  "mapa widok->zrodlo ma dlugosc tekstu widoku + 1", "len=" + aV2S.Length + " view=" + sView.Length);
Ok(aS2V.Length == sDoc.Length + 1,
  "mapa zrodlo->widok ma dlugosc zrodla + 1", "len=" + aS2V.Length + " src=" + sDoc.Length);

// Mapa zrodlo->widok na znaczniku musi wskazywac POCZATEK napisu, bo tego
// uzywa skok (MarkdownReview_GoToSourceIndex).
Ok(aS2V[iSrcRef] == iViewMarker,
  "skok na znacznik w zrodle laduje na poczatku \"footnote 1\" w widoku",
  "s2v=" + aS2V[iSrcRef] + " oczekiwano " + iViewMarker);

// ---------- 3. BLOK KODU: znacznik zostaje SUROWY ----------
string sCode = "Zdanie z przypisem[^1].\n\n```\nprzyklad [^1] w kodzie\n```\n\n[^1]: Tresc.\n";
int[] aS2Vc, aV2Sc;
string sViewCode = Render(sCode, out aS2Vc, out aV2Sc);
Console.WriteLine("--- PODGLAD Z BLOKIEM KODU ---");
Console.WriteLine(sViewCode);
Console.WriteLine("--- KONIEC ---");
Ok(sViewCode.Contains("przyklad [^1] w kodzie"),
  "KONTROLA DYSKRYMINACYJNA: w bloku kodu znacznik zostaje SUROWY", sViewCode);
Ok(sViewCode.Contains("Zdanie z przypisem" + sMarker + "."),
  "a w TYM SAMYM dokumencie zdanie poza blokiem dostaje \"footnote 1\"", sViewCode);

// ---------- 4. WIELE PRZYPISOW W JEDNYM WIERSZU ----------
string sTwo = "Raz[^1] i dwa[^2] w jednym zdaniu.\n\n[^1]: Pierwsza.\n[^2]: Druga.\n";
int[] aS2V2, aV2S2;
string sViewTwo = Render(sTwo, out aS2V2, out aV2S2);
Ok(sViewTwo.Contains("Raz" + sMarker + " i dwa" + (string) miMarker.Invoke(null, new object[] {"2"}) + " w jednym zdaniu."),
  "dwa znaczniki w jednym wierszu renderuja sie oba", sViewTwo);
int iSrc2 = sTwo.IndexOf("[^2]");
Ok(aS2V2[iSrc2] == sViewTwo.IndexOf((string) miMarker.Invoke(null, new object[] {"2"})),
  "drugi znacznik mapuje sie na swoj napis, nie na pierwszy",
  "s2v=" + aS2V2[iSrc2]);

// ---------- 5. DOKUMENT BEZ PRZYPISOW - render bez zmian ----------
string sPlain = "# Tytul\n\nZwykle zdanie bez przypisow.\n";
int[] aS2Vp, aV2Sp;
string sViewPlain = Render(sPlain, out aS2Vp, out aV2Sp);
Ok(sViewPlain.Contains("Zwykle zdanie bez przypisow."),
  "KONTROLA: dokument bez przypisow renderuje sie normalnie", sViewPlain);
Ok(!sViewPlain.Contains("footnote"),
  "KONTROLA: i nie pojawia sie w nim slowo footnote", sViewPlain);

// ---------- 6. NAWIAS KWADRATOWY, KTORY NIE JEST PRZYPISEM ----------
string sBracket = "Zdanie z [odsylaczem](http://a.b) i [zwyklym] nawiasem.\n";
int[] aS2Vb, aV2Sb;
string sViewBracket = Render(sBracket, out aS2Vb, out aV2Sb);
Ok(!sViewBracket.Contains("footnote"),
  "KONTROLA DYSKRYMINACYJNA: zwykly nawias NIE udaje przypisu", sViewBracket);

// ---------- 7. SKOK CZYTA ZRODLO, NIE WIDOK ----------
// To jest asercja, ktora broni calej konstrukcji: render zmienil wiersz
// tresci na "footnote 1: ...", wiec gdyby rozpoznawanie wiersza tresci
// dzialalo na WIDOKU, powrot do zdania przestalby dzialac.  Pytamy wprost
// helpera, ktorego uzywa skok, o SUROWY wiersz zrodla i o wiersz WIDOKU.
MethodInfo miIsDef = tFrame.GetMethod("IsMarkdownFootnoteDefinitionLine",
  BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
if (miIsDef == null) {Console.WriteLine("Brak IsMarkdownFootnoteDefinitionLine"); return 3;}

object[] aDef = new object[] {"[^1]: Tresc przypisu.", null, null};
bool bSrcIsDef = (bool) miIsDef.Invoke(null, aDef);
Ok(bSrcIsDef, "skok rozpoznaje wiersz tresci w ZRODLE (\"[^1]: ...\")", "nie rozpoznaje");
Ok((string) aDef[1] == "1", "i wyciaga z niego etykiete 1", (string) aDef[1]);

object[] aDefView = new object[] {sMarker + ": Tresc przypisu.", null, null};
bool bViewIsDef = (bool) miIsDef.Invoke(null, aDefView);
Ok(!bViewIsDef,
  "KONTROLA DYSKRYMINACYJNA: wiersz z WIDOKU nie jest brany za tresc przypisu",
  "widok udaje zrodlo - skok dzialalby na zlym tekscie");

// Znaczniki i tresci liczone ze ZRODLA (tego uzywa TryGoToFootnoteInReview).
MethodInfo miRefs = tFrame.GetMethod("GetMarkdownFootnoteRefs",
  BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
MethodInfo miDefs = tFrame.GetMethod("GetMarkdownFootnoteDefs",
  BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
if (miRefs == null || miDefs == null) {Console.WriteLine("Brak helperow przypisow"); return 3;}
System.Collections.ICollection cRefs = (System.Collections.ICollection) miRefs.Invoke(null, new object[] {sDoc});
System.Collections.ICollection cDefs = (System.Collections.ICollection) miDefs.Invoke(null, new object[] {sDoc});
Ok(cRefs.Count == 1, "w zrodle jest 1 znacznik", "znalezniono " + cRefs.Count);
Ok(cDefs.Count == 1, "w zrodle jest 1 tresc przypisu", "znalezniono " + cDefs.Count);

// Metoda skoku MUSI istniec w tej binarce, inaczej caly pomiar renderu
// mierzylby polowe funkcji.
MethodInfo miJump = tFrame.GetMethod("TryGoToFootnoteInReview",
  BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
Ok(miJump != null, "binarka zawiera skok po przypisie w podgladzie", "brak TryGoToFootnoteInReview");
MethodInfo miGoTo = tFrame.GetMethod("MarkdownReview_GoToSourceIndex",
  BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
Ok(miGoTo != null, "binarka zawiera przenoszenie kursora w podgladzie", "brak MarkdownReview_GoToSourceIndex");

Console.WriteLine();
Console.WriteLine("PASS: " + iPass + "   FAIL: " + iFail);
return (iFail == 0) ? 0 : 1;
} // Main

} // PomiarPodgladuPrzypisow class
