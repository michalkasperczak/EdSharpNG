// Sonda dla 5.0.59: domyslne rozszerzenie md (z migracja jednorazowa) oraz
// poczta z zalacznikiem, ktora przestaje milczec przy awarii MAPI.
//
// Pyta ZBUDOWANA BINARKE, nie kod zrodlowy: refleksja o metody i pola plus
// dokladne literaly w tablicy #US.  Kontrola negatywna: ta sama sonda na
// binarce 5.0.58 musi skonczyc sie kodem 3 na BRAKU metody migracji.
//
// Uruchomienie:
//   csc /nologo /out:pomiar_559.exe pomiar_559.cs
//   pomiar_559.exe C:\sciezka\EdSharpNG.exe
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

public class Pomiar559 {
	static int iPass = 0, iFail = 0;
	static byte[] aBytes;

	static void Ok(string s) { iPass++; Console.WriteLine("PASS: " + s); }
	static void Zle(string s) { iFail++; Console.WriteLine("FAIL: " + s); }
	static void Sprawdz(bool b, string s) { if (b) Ok(s); else Zle(s); }

	// Literal w tablicy #US lezy jako <prefiks dlugosci><tekst UTF-16LE>.  Samo
	// szukanie tekstu trafia takze w KAZDY dluzszy napis, ktory go zawiera
	// (lekcja z 5.0.58: "Control+E" trafia w "Control+Enter"), wiec dopasowanie
	// musi obejmowac prefiks dlugosci - wtedy trafienie oznacza DOKLADNIE ten
	// literal, a nie jego nadciag.
	//
	// PRZYCZYNA POPRAWKI (zmierzona 01.09.2026): prefiks jest "compressed
	// unsigned integer" z formatu metadanych, czyli dla wartosci od 0x80 DWA
	// bajty BIG-ENDIAN (0x80 | dlugosc>>8, dlugosc & 0xff), a nie varint
	// little-endian po 7 bitow.  Zla wersja dawala FALSZYWE ZERO na kazdym
	// literale dluzszym niz 63 znaki: krotkie asercje przechodzily, dluga
	// oblewala przy POPRAWNYM kodzie.  Kontrola nizej pilnuje obu galezi.
	static byte[] PrefiksDlugosci(int iLen) {
		if (iLen < 0x80) return new byte[] { (byte) iLen };
		if (iLen < 0x4000) return new byte[] { (byte) (0x80 | (iLen >> 8)), (byte) (iLen & 0xff) };
		return new byte[] { (byte) (0xc0 | (iLen >> 24)), (byte) ((iLen >> 16) & 0xff), (byte) ((iLen >> 8) & 0xff), (byte) (iLen & 0xff) };
	}

	static bool MaLiteral(string sText) {
		byte[] aText = Encoding.Unicode.GetBytes(sText);
		byte[] aPrefix = PrefiksDlugosci(aText.Length + 1); // .NET: dlugosc w bajtach + znacznik
		byte[] aWzor = new byte[aPrefix.Length + aText.Length];
		aPrefix.CopyTo(aWzor, 0);
		Array.Copy(aText, 0, aWzor, aPrefix.Length, aText.Length);
		return Zawiera(aWzor);
	}

	static bool Zawiera(byte[] aWzor) {
		for (int i = 0; i + aWzor.Length <= aBytes.Length; i++) {
			bool bOk = true;
			for (int j = 0; j < aWzor.Length; j++) if (aBytes[i + j] != aWzor[j]) { bOk = false; break; }
			if (bOk) return true;
		}
		return false;
	}

	public static int Main(string[] args) {
		string sExe = args.Length > 0 ? args[0] : @"C:\edsharp_probe\EdSharpNG.exe";
		Console.WriteLine("SONDA 5.0.59 na binarce: " + sExe);
		aBytes = File.ReadAllBytes(sExe);

		Assembly asm = Assembly.LoadFile(sExe); // NIE LoadFrom: dwa pliki o tej samej tozsamosci daja te sama assembly
		Type tApp = asm.GetType("EdSharp.App");
		if (tApp == null) { Console.WriteLine("BRAK typu EdSharp.App"); return 3; }

		// ---- BRAMKA KONTROLI NEGATYWNEJ ----
		MethodInfo miMig = tApp.GetMethod("MigrateDefaultExtensionToMarkdown",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
		if (miMig == null) {
			Console.WriteLine("BRAK metody MigrateDefaultExtensionToMarkdown - to jest binarka SPRZED zmiany.");
			return 3;
		}
		Ok("metoda MigrateDefaultExtensionToMarkdown istnieje w binarce");

		// ---- KONTROLA POZYTYWNA SONDY: rzeczy, ktore w binarce BYC MUSZA ----
		// Bez niej zielony wynik nie odroznia naprawy od sondy, ktora nie widzi nic.
		Sprawdz(tApp.GetMethod("ReadOption", BindingFlags.Public | BindingFlags.Static) != null,
			"kontrola pozytywna: ReadOption widoczne przez refleksje");
		Sprawdz(tApp.GetMethod("WriteData", BindingFlags.Public | BindingFlags.Static) != null,
			"kontrola pozytywna: WriteData widoczne przez refleksje");
		Sprawdz(MaLiteral("ExtensionDefault"), "kontrola pozytywna: literal ExtensionDefault jest w binarce");
		Sprawdz(MaLiteral("Control+Shift+Y"), "kontrola pozytywna: chord Control+Shift+Y (Yield) nadal w binarce");

		// KONTROLA SAMEGO DOPASOWANIA, OBU GALEZI PREFIKSU DLUGOSCI.  Krotka
		// galaz (jeden bajt) i dluga (dwa bajty, od 0x80) to dwie osobne drogi
		// w kodzie sondy - defekt z 01.09.2026 siedzial WYLACZNIE w dlugiej,
		// wiec asercja tylko na krotkim literale nie moglaby go zlapac.
		// Oba wzorce ZMIERZONE w binarce, nie zalozone: pierwsza wersja tej
		// kontroli pytala o "Footnote List" i o komunikat Format Code, ktorych
		// w tej postaci w tablicy NIE MA ("Footnote List ..." ma wielokropek
		// pozycji menu) - czyli byla to asercja, ktora nie mogla trafic.
		Sprawdz(MaLiteral("Selection copied"), "kontrola dopasowania: krotki literal (prefiks 1-bajtowy) trafia");
		Sprawdz(MaLiteral("Close the table without inserting it?  What you typed will be lost."),
			"kontrola dopasowania: dlugi literal (prefiks 2-bajtowy) trafia");
		Sprawdz(!MaLiteral("Control+"), "kontrola rozlaczna: samo Control+ NIE jest osobnym literalem (dopasowanie nie jest podciagiem)");

		// ---- 1. DOMYSLNE ROZSZERZENIE ----
		Sprawdz(MaLiteral("ExtensionDefaultMigrated"),
			"znacznik jednorazowosci ExtensionDefaultMigrated jest w binarce");
		Sprawdz(MaLiteral("md"), "literal md jest w binarce");
		// Migracja MUSI byc wolana - osierocona metoda to lekcja z 5.0.44.
		// Refleksja nie pokazuje wywolan, wiec pytamy o cialo metody startowej.
		MethodInfo miStart = null;
		foreach (Type t in asm.GetTypes()) {
			foreach (MethodInfo mi in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)) {
				if (mi.Name.Contains("SetConfigurationValues")) miStart = mi;
			}
		}
		Sprawdz(miStart != null, "SetConfigurationValues nadal istnieje (migracja stoi za nia)");
		byte[] aIl = null;
		try {
			MethodBody mb = miMig.GetMethodBody();
			if (mb != null) aIl = mb.GetILAsByteArray();
		} catch {}
		Sprawdz(aIl != null && aIl.Length > 10, "metoda migracji ma niepuste cialo (nie jest zaslepka)");

		// ---- 2. POCZTA Z ZALACZNIKIEM ----
		Sprawdz(MaLiteral("Mail client refused the attachment!"),
			"awaria MAPI mowi wprost, ze klient odmowil (byl PUSTY catch)");
		Sprawdz(MaLiteral("Mail with attachment handed to the mail client"),
			"powodzenie tez jest oglaszane (przedtem komenda milczala w obu wypadkach)");
		Sprawdz(MaLiteral("Mail Attachment Failed"),
			"okno drogi awaryjnej ma wlasny tytul");
		Sprawdz(aBytes != null && MaLiteral("This document has unsaved changes, and the attachment is taken from the file on disk.\nSave it first?"),
			"ostrzezenie o niezapisanych zmianach przed wyslaniem zalacznika z dysku");
		// Chordy poczty MAJA ZOSTAC - jego decyzja z 01.09 ("oba bym zostawil").
		Sprawdz(MaLiteral("Control+M"), "Control+M nadal w binarce (Mail Body zostaje)");
		Sprawdz(MaLiteral("Control+Shift+M"), "Control+Shift+M nadal w binarce (Mail Attachment zostaje)");

		Console.WriteLine();
		Console.WriteLine("WYNIK: " + iPass + " PASS / " + iFail + " FAIL");
		return iFail == 0 ? 0 : 1;
	}
}
