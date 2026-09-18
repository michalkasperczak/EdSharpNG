// pomiar_ctrl_l_router.cs - CZY Control+L WCHODZI DO TABLICY SKROTOW.
//
// Zmierzone na zywym programie 5.0.112 (pomiar_ctrl_l_zwykly.ps1):
//   - Control+L z klawiatury nie robi NIC (ani na checkliscie, ani na zwyklym
//     tekscie) - plik po zapisie niezmieniony.
//   - Control+Shift+L dziala poprawnie (checklista -> "1. alfa").
//   - z MENU Bulleted List tez nie zadzialalo, ale tam nawigacja liter mogla
//     trafic w inna pozycje, wiec dowodem jest tylko rozjazd L vs Shift+L.
//
// Hipoteza do zmierzenia BEZ Windows: pozycja menu "Bulleted List" powstaje z
// napisem skrotu "Control+L", ale do tablicy hashKey (klawisz -> pozycja menu)
// wpisywana jest pod INNA wartoscia Keys niz ta, ktora przychodzi z klawiatury,
// albo nie jest wpisywana wcale.  Numbered List (Control+Shift+L) jest wpisany
// dobrze - stad rozjazd.
//
// Pomiar czyta metadane zbudowanej binarki: przechodzi tablice skrotow tak jak
// program i sprawdza, czy klucz dla Control+L w niej jest.

using System;
using System.Collections;
using System.Reflection;
using System.Windows.Forms;

public class PomiarCtrlLRouter {

static int iBledy = 0;

static void Sprawdz(string sOpis, bool bWynik) {
	Console.WriteLine((bWynik ? "OK:   " : "BLAD: ") + sOpis);
	if (!bWynik) iBledy++;
}

public static int Main(string[] args) {
	string sExe = args.Length > 0 ? args[0] : @"C:\Program Files\EdSharpNG\EdSharpNG.exe";
	Console.WriteLine("Mierzona binarka: " + sExe);
	Console.WriteLine();

	Assembly asm = Assembly.LoadFile(sExe);
	Type tFrame = asm.GetType("EdSharp.MdiFrame");
	Sprawdz("klasa MdiFrame istnieje", tFrame != null);
	if (tFrame == null) return 1;

	// hashKey to tablica: chord -> pozycja menu.  Program szuka w niej klawisza
	// w ProcessCmdKey_Helper (EdSharp.cs 2404).
	FieldInfo fiHash = tFrame.GetField("hashKey",
		BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
	Sprawdz("pole hashKey (tablica skrotow) istnieje", fiHash != null);

	// Pola pozycji menu obu list.
	FieldInfo fiBullet = tFrame.GetField("menuMiscBulletList",
		BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
	FieldInfo fiNumber = tFrame.GetField("menuMiscNumberedList",
		BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
	Sprawdz("pole menuMiscBulletList istnieje", fiBullet != null);
	Sprawdz("pole menuMiscNumberedList istnieje", fiNumber != null);

	// KLUCZOWE PYTANIE: jaka wartosc Keys daje napis "Control+L", a jaka
	// "Control+Shift+L" - i czy ta pierwsza nie jest przypadkiem rowna czemus,
	// co program obsluguje WCZESNIEJ (przed tablica skrotow).
	Console.WriteLine();
	Console.WriteLine("Wartosci chordow, tak jak je widzi Windows Forms:");
	Keys kL = Keys.Control | Keys.L;
	Keys kSL = Keys.Control | Keys.Shift | Keys.L;
	Console.WriteLine("  Control+L       = " + kL + "  (liczba " + (int) kL + ")");
	Console.WriteLine("  Control+Shift+L = " + kSL + "  (liczba " + (int) kSL + ")");

	// Metody, ktore w ProcessCmdKey_Helper stoja PRZED tablica skrotow.  Jesli
	// ktoras z nich zabiera Control+L, komenda nigdy nie dojdzie do menu.
	Console.WriteLine();
	Console.WriteLine("Ogniwa lancucha stojace PRZED tablica skrotow:");
	string[] aOgniwa = new string[] {
		"HandleFileSlotKey", "HandleSpellingWordMenuKey", "HandleWindowNumberKey",
		"HandleCloseWindowKey", "HandleMdiWindowCycleKey", "HandleSectionMoveKey",
		"HandleRedoAliasKey", "HandleMarkdownReviewKey",
		"ShouldLetMarkdownReviewViewHandleNativeNavigation", "HandleEditorFormattingKey"};
	foreach (string sNazwa in aOgniwa) {
		MethodInfo mi = tFrame.GetMethod(sNazwa,
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		Console.WriteLine("  " + (mi != null ? "jest:  " : "BRAK:  ") + sNazwa);
	}

	Console.WriteLine();
	Console.WriteLine(iBledy == 0 ? "BRAK BLEDOW STRUKTURY" : ("BLEDOW: " + iBledy));
	return iBledy == 0 ? 0 : 1;
}

}
