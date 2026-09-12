// KOPIA PickPolishLegacyEncoding wycieta z EdSharp.cs WYLACZNIE do pomiaru.
using System;
using System.Collections.Generic;
using System.Text;

public class LegacyProbe {
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
} // LegacyProbe class
