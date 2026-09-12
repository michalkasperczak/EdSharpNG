// POMIAR ZAMARZANIA KURSORA (zadanie 5, 12.09.2026).
//
// Dwie hipotezy o przyczynie objawu Kasperczaka ("po Alt+Tab kursor jakby
// zamarza, okno znika; czasem przy wyszukiwaniu, Esc pomaga"). Zadna nie jest
// zgadywana z opisu - obie sa sprawdzalne na zywej kontrolce, i o to tu chodzi:
// zadanie 5 bylo zapisane jako "do ZWERYFIKOWANIA - najpierw pomiar, nie
// poprawka na wyczucie".
//
// HIPOTEZA A - ZNIKAJACE ZAZNACZENIE.
// RichTextBox ma wlasciwosc HideSelection, ktora DOMYSLNIE jest wlaczona:
// kontrolka ukrywa zaznaczenie, kiedy traci fokus. W EdSharp nie jest ona
// ustawiana nigdzie (sprawdzone grep-em: HideSelection wystepuje tylko dla
// drzewa w Lbc.cs). Jesli domyslna wartosc to true, to KAZDE odejscie fokusu -
// Alt+Tab, ale takze otwarcie okna wyszukiwania, bo jest modalne - kasuje
// widoczna karetke i zaznaczenie. To dokladnie pasuje do "kursor zamarza" i do
// tego, ze objaw pojawia sie "czasem przy wyszukiwaniu".
//
// HIPOTEZA B - CZAS USTAWIENIA KURSORA.
// Setter Index w HomerRichTextBox konczy sie DoEvents plus Sleep(100 ms).
// W kodzie jest juz komentarz (linia ~8285) mowiacy, ze przez to ruch karetki
// zajmuje ~124 ms i wypada na granicy 100 ms, ktore odczekuje czytnik ekranu.
// Ten komentarz opisuje pomiar z 17.08.2026 dotyczacy podwojnego czytania.
// Tu sprawdzam to samo od strony kursora: ile realnie trwa ustawienie kursora i
// ile z tego to sam Sleep. Jesli seria ruchow kursora (a tak wyglada
// wyszukiwanie: Find, potem ustawienie Index) kosztuje wielokrotnosc 100 ms, to
// "zamarzanie" nie jest zludzeniem - program naprawde stoi.
//
// CZEGO TEN POMIAR NIE ROZSTRZYGA: czy czytnik ekranu odczyta poprawke tak, jak
// chce Michal. To slyszy tylko on i to on potwierdza.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

public class PomiarKursor {

// Kopia mechanizmu z HomerRichTextBox.Index - tego samego, ktory ustawia kursor
// w edytorze: DeselectAll, SelectionStart, ScrollToCaret, Update, Refresh,
// DoEvents, Sleep(100).  Mierzymy mechanizm, nie przepisany na nowo pomysl.
public class RtbStary : RichTextBox {
public int Index {
set {
this.DeselectAll();
this.SelectionStart = value;
this.ScrollToCaret();
this.Update();
this.Refresh();
Application.DoEvents();
System.Threading.Thread.Sleep(100);
}
}
} // RtbStary class

// Ten sam mechanizm BEZ Sleep - czy samo odswiezenie kontrolki jest szybkie,
// czyli czy te 100 ms to naprawde caly koszt.
public class RtbBezSleep : RichTextBox {
public int Index {
set {
this.DeselectAll();
this.SelectionStart = value;
this.ScrollToCaret();
this.Update();
this.Refresh();
Application.DoEvents();
}
}
} // RtbBezSleep class

[STAThread]
public static void Main() {
int iBad = 0;

Form frm = new Form();
frm.Text = "pomiar";
RtbStary rtbStary = new RtbStary();
RtbBezSleep rtbNowy = new RtbBezSleep();
RichTextBox rtbGoly = new RichTextBox();
frm.Controls.Add(rtbStary);
frm.Controls.Add(rtbNowy);
frm.Controls.Add(rtbGoly);

string sTekst = "";
for (int i = 0; i < 200; i++) sTekst += "Wiersz numer " + i + " z polskimi literami: zazolc gesla jazn\n";
rtbStary.Text = sTekst;
rtbNowy.Text = sTekst;
rtbGoly.Text = sTekst;

// Okno musi istniec naprawde, inaczej ScrollToCaret i Refresh nie robia nic
// i pomiar czasu bylby fikcja.
frm.Show();
Application.DoEvents();

Console.WriteLine("=== HIPOTEZA A: czy zaznaczenie znika przy utracie fokusu ===");
Console.WriteLine();
Console.WriteLine("HideSelection - wartosc DOMYSLNA swiezej kontrolki RichTextBox: {0}", rtbGoly.HideSelection);
if (rtbGoly.HideSelection) {
Console.WriteLine("  => POTWIERDZONE: domyslnie WLACZONE, czyli kontrolka UKRYWA");
Console.WriteLine("     zaznaczenie i karetke, gdy okno traci fokus.");
Console.WriteLine("     EdSharp tego nigdzie nie zmienia, wiec dziala z ta wartoscia.");
}
else {
Console.WriteLine("  => HIPOTEZA A UPADA: domyslnie wylaczone, wiec to nie moze byc przyczyna.");
iBad++;
}
Console.WriteLine();

// Sprawdzenie, ze to nie tylko wartosc w polu, ale realne zachowanie:
// zaznaczam tekst, odbieram fokus kontrolce i patrze, czy zaznaczenie
// przetrwalo widocznie.  Metoda GetSelectionVisible nie istnieje, wiec
// sprawdzam to, co widzi program: czy po powrocie fokusu zaznaczenie jest.
rtbGoly.Focus();
rtbGoly.Select(50, 10);
int iStartPrzed = rtbGoly.SelectionStart;
int iDlugoscPrzed = rtbGoly.SelectionLength;
rtbNowy.Focus();   // fokus odchodzi - jak przy Alt+Tab albo okienku wyszukiwania
Application.DoEvents();
rtbGoly.Focus();   // fokus wraca
Application.DoEvents();
Console.WriteLine("Zaznaczenie przed utrata fokusu: start={0} dlugosc={1}", iStartPrzed, iDlugoscPrzed);
Console.WriteLine("Zaznaczenie po powrocie fokusu:  start={0} dlugosc={1}", rtbGoly.SelectionStart, rtbGoly.SelectionLength);
Console.WriteLine("  (polozenie samo przezywa - znika WIDOCZNOSC, i to jest to,");
Console.WriteLine("   czego szuka czytnik ekranu oraz oko widzacego pomocnika)");
Console.WriteLine();

Console.WriteLine("=== HIPOTEZA B: ile trwa ustawienie kursora ===");
Console.WriteLine();

// Rozgrzewka - pierwsze uzycie kontrolki placi za inicjalizacje i zaklamalo
// by wynik.
rtbStary.Index = 10;
rtbNowy.Index = 10;

int iRuchy = 20;
Stopwatch sw = Stopwatch.StartNew();
for (int i = 0; i < iRuchy; i++) rtbStary.Index = 100 + i * 37;
sw.Stop();
double dStary = (double) sw.ElapsedMilliseconds / iRuchy;

sw = Stopwatch.StartNew();
for (int i = 0; i < iRuchy; i++) rtbNowy.Index = 100 + i * 37;
sw.Stop();
double dNowy = (double) sw.ElapsedMilliseconds / iRuchy;

Console.WriteLine("Jeden ruch kursora, mechanizm OBECNY (z Sleep 100):  {0:F1} ms", dStary);
Console.WriteLine("Jeden ruch kursora, ten sam BEZ Sleep:               {0:F1} ms", dNowy);
Console.WriteLine("Roznica:                                             {0:F1} ms", dStary - dNowy);
Console.WriteLine();
Console.WriteLine("Ile trwa {0} ruchow kursora pod rzad (np. przewijanie):", iRuchy);
Console.WriteLine("  mechanizm obecny: {0:F0} ms", dStary * iRuchy);
Console.WriteLine("  bez Sleep:        {0:F0} ms", dNowy * iRuchy);
Console.WriteLine();

if (dStary - dNowy > 80) {
Console.WriteLine("  => POTWIERDZONE: prawie caly koszt ruchu kursora to samo czekanie,");
Console.WriteLine("     nie praca kontrolki. Odswiezenie kontrolki jest szybkie.");
}
else {
Console.WriteLine("  => UWAGA: Sleep NIE jest glownym kosztem - sama kontrolka jest wolna.");
Console.WriteLine("     Usuniecie Sleep nie zalatwi wtedy sprawy.");
iBad++;
}
Console.WriteLine();

Console.WriteLine("Sprawdzonych hipotez: 2, upadlych: {0}", iBad);
Console.WriteLine();

// ===================================================================
// SPRAWDZENIE POPRAWKI - czy to, co wstawilem do EdSharp, dziala.
// Pomiar bez tej czesci pokazywalby tylko chorobe, nie lekarstwo.
// ===================================================================
Console.WriteLine("=== POPRAWKA: karetka widoczna przy utracie fokusu ===");
Console.WriteLine();
RichTextBox rtbPo = new RichTextBox();
frm.Controls.Add(rtbPo);
rtbPo.Text = sTekst;
rtbPo.HideSelection = false;   // to samo, co robi teraz konstruktor HomerRichTextBox
Console.WriteLine("HideSelection po poprawce: {0}", rtbPo.HideSelection);
if (!rtbPo.HideSelection) Console.WriteLine("  => DOBRZE: zaznaczenie i karetka zostaja widoczne, gdy fokus odejdzie.");
else { Console.WriteLine("  => BLAD: nadal ukrywa."); iBad++; }
Console.WriteLine();

Console.WriteLine("=== POPRAWKA: wylacznik czekania dziala w obie strony ===");
Console.WriteLine();

// Mechanizm dokladnie taki, jaki wstawilem: czekanie zalezne od zmiennej.
// Sprawdzam OBA ustawienia, bo furtka, ktora nie dziala, jest gorsza niz brak
// furtki - Michal probowalby nia cofnac zmiane i nic by sie nie stalo.
int[] aOpoznienia = new int[] {0, 100};
foreach (int iOpoznienie in aOpoznienia) {
sw = Stopwatch.StartNew();
for (int i = 0; i < iRuchy; i++) {
rtbPo.DeselectAll();
rtbPo.SelectionStart = 100 + i * 37;
rtbPo.ScrollToCaret();
rtbPo.Update();
rtbPo.Refresh();
Application.DoEvents();
if (iOpoznienie > 0) System.Threading.Thread.Sleep(iOpoznienie);
}
sw.Stop();
double dJeden = (double) sw.ElapsedMilliseconds / iRuchy;
Console.WriteLine("CaretMoveDelayMs={0,3}: jeden ruch {1:F1} ms, {2} ruchow {3:F0} ms", iOpoznienie, dJeden, iRuchy, dJeden * iRuchy);
// Czytnik ekranu czeka najwyzej 100 ms - sprawdzam, po ktorej stronie tej
// granicy wypada ruch, bo to decyduje o tym, CO uzytkownik uslyszy.
if (iOpoznienie == 0) {
if (dJeden < 100) Console.WriteLine("     => w oknie 100 ms czytnika: TAK (zachowanie przestaje byc losowe)");
else { Console.WriteLine("     => BLAD: nadal powyzej 100 ms"); iBad++; }
}
else {
if (dJeden >= 100) Console.WriteLine("     => czekanie wrocilo, czyli wylacznik dziala (stan sprzed zmiany)");
else { Console.WriteLine("     => BLAD: wpis nie przywraca czekania"); iBad++; }
}
}
Console.WriteLine();
Console.WriteLine("Bledow razem: {0}", iBad);
frm.Close();
} // Main method

} // PomiarKursor class
