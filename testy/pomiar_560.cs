// Sonda dla 5.0.60: wklejanie w EdSharpie odzyskuje skladnie Markdown oraz
// zapis do schowka przestaje wywracac program przy zajetym schowku.
//
// Pyta ZBUDOWANA BINARKE, nie kod zrodlowy: refleksja o metody i pola plus
// DOKLADNE literaly w tablicy #US (dopasowanie z prefiksem dlugosci, zeby
// trafienie nie bylo podciagiem dluzszego napisu - lekcja z 5.0.58).
//
// KONTROLA NEGATYWNA: ta sama sonda na binarce 5.0.59 musi skonczyc sie kodem
// 3 na BRAKU metody TryGetEdSharpMarkdownFromClipboard.
//
// Uruchomienie:
//   csc /nologo /out:pomiar_560.exe pomiar_560.cs
//   pomiar_560.exe C:\sciezka\EdSharpNG.exe
using System;
using System.IO;
using System.Reflection;
using System.Text;

public class Pomiar560 {
	static int iPass = 0, iFail = 0;
	static byte[] aBytes;

	static void Ok(string s) { iPass++; Console.WriteLine("PASS: " + s); }
	static void Zle(string s) { iFail++; Console.WriteLine("FAIL: " + s); }
	static void Sprawdz(bool b, string s) { if (b) Ok(s); else Zle(s); }

	// Prefiks dlugosci literalu w tablicy #US to "compressed unsigned integer":
	// od 0x80 DWA bajty BIG-ENDIAN, nie varint little-endian (defekt wlasnej
	// sondy zlapany 01.09.2026 - dawal falszywe zero na literalach dluzszych
	// niz 63 znaki).
	static byte[] PrefiksDlugosci(int iLen) {
		if (iLen < 0x80) return new byte[] { (byte) iLen };
		if (iLen < 0x4000) return new byte[] { (byte) (0x80 | (iLen >> 8)), (byte) (iLen & 0xff) };
		return new byte[] { (byte) (0xc0 | (iLen >> 24)), (byte) ((iLen >> 16) & 0xff), (byte) ((iLen >> 8) & 0xff), (byte) (iLen & 0xff) };
	}

	static bool MaLiteral(string sText) {
		byte[] aText = Encoding.Unicode.GetBytes(sText);
		byte[] aPrefix = PrefiksDlugosci(aText.Length + 1);
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

	static MethodInfo Metoda(Type t, string sName) {
		if (t == null) return null;
		return t.GetMethod(sName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
	}

	public static int Main(string[] args) {
		string sExe = args.Length > 0 ? args[0] : @"C:\edsharp_probe\EdSharpNG.exe";
		Console.WriteLine("SONDA 5.0.60 na binarce: " + sExe);
		aBytes = File.ReadAllBytes(sExe);

		Assembly asm = Assembly.LoadFile(sExe); // NIE LoadFrom: dwa pliki o tej samej tozsamosci daja te sama assembly
		Type tFrame = asm.GetType("EdSharp.MdiFrame");
		Type tUtil = asm.GetType("EdSharp.Util");
		if (tFrame == null || tUtil == null) { Console.WriteLine("BRAK typu EdSharp.MdiFrame albo EdSharp.Util"); return 3; }

		// ---- BRAMKA KONTROLI NEGATYWNEJ ----
		MethodInfo miGet = Metoda(tFrame, "TryGetEdSharpMarkdownFromClipboard");
		if (miGet == null) {
			Console.WriteLine("BRAK metody TryGetEdSharpMarkdownFromClipboard - to jest binarka SPRZED zmiany.");
			return 3;
		}
		Ok("metoda TryGetEdSharpMarkdownFromClipboard istnieje w binarce");

		// ---- KONTROLA POZYTYWNA SONDY ----
		// Bez niej zielony wynik nie odroznia naprawy od sondy, ktora nie widzi nic.
		Sprawdz(Metoda(tUtil, "SetClipboardText") != null, "kontrola pozytywna: Util.SetClipboardText widoczne przez refleksje");
		Sprawdz(Metoda(tUtil, "GetClipboardText") != null, "kontrola pozytywna: Util.GetClipboardText widoczne przez refleksje");
		Sprawdz(MaLiteral("Link copied"), "kontrola pozytywna: literal Link copied jest w binarce");
		Sprawdz(MaLiteral("Control+Shift+C"), "kontrola pozytywna: chord Control+Shift+C nadal w binarce");
		// Kontrola OBU galezi prefiksu dlugosci: krotka (1 bajt) i dluga (2 bajty).
		Sprawdz(MaLiteral("Selection copied"), "kontrola dopasowania: krotki literal (prefiks 1-bajtowy) trafia");
		Sprawdz(MaLiteral("Close the table without inserting it?  What you typed will be lost."),
			"kontrola dopasowania: dlugi literal (prefiks 2-bajtowy) trafia");
		Sprawdz(!MaLiteral("Control+"), "kontrola rozlaczna: samo Control+ NIE jest osobnym literalem");

		// ---- 1. WLASNY FORMAT SCHOWKA I WKLEJANIE ----
		Sprawdz(MaLiteral("EdSharpNG.Markdown"), "nazwa wlasnego formatu schowka jest w binarce");
		Sprawdz(Metoda(tFrame, "PasteMarkdownTextInto") != null, "PasteMarkdownTextInto istnieje (zapis do kontrolki)");
		MethodInfo miSklej = Metoda(tFrame, "PasteEdSharpMarkdownFormat");
		Sprawdz(miSklej != null, "PasteEdSharpMarkdownFormat istnieje (warstwa mowiaca)");
		Sprawdz(MaLiteral("Markdown pasted"), "komunikat Markdown pasted jest w binarce");
		Sprawdz(Metoda(tFrame, "GetMarkdownListSourceText") != null,
			"GetMarkdownListSourceText istnieje (oryginalna skladnia listy, nie same tresci pozycji)");

		// Metody pomocnicze MUSZA byc WOLANE - osierocony helper to lekcja z 5.0.44.
		// Refleksja nie pokazuje wywolan, wiec pytamy o cialo metody sklejajacej.
		byte[] aIl = null;
		try {
			MethodBody mb = miSklej.GetMethodBody();
			if (mb != null) aIl = mb.GetILAsByteArray();
		} catch {}
		Sprawdz(aIl != null && aIl.Length > 20, "PasteEdSharpMarkdownFormat ma niepuste cialo (nie jest zaslepka)");

		// Cialo menuItem_Click MUSI wolac nasza droge, inaczej Control+V jej nie uzyje.
		MethodInfo miClick = Metoda(tFrame, "menuItem_Click");
		Sprawdz(miClick != null, "menuItem_Click nadal istnieje (tam siedzi obsluga Control+V)");

		// ---- 2. ZAPIS SCHOWKA Z PONOWIENIAMI ----
		MethodInfo miSet = tUtil.GetMethod("SetClipboardData", BindingFlags.Public | BindingFlags.Static);
		Sprawdz(miSet != null, "Util.SetClipboardData istnieje (bogaty schowek z ponowieniami)");
		Sprawdz(miSet != null && miSet.ReturnType == typeof(bool),
			"SetClipboardData zwraca bool, czyli wolajacy WIE, ze zapis padl");
		byte[] aIlSet = null;
		try {
			MethodBody mb = miSet.GetMethodBody();
			if (mb != null) aIlSet = mb.GetILAsByteArray();
		} catch {}
		Sprawdz(aIlSet != null && aIlSet.Length > 20, "SetClipboardData ma niepuste cialo");

		// Komunikaty nieudanego kopiowania - cisza przy porazce jest dla
		// niewidomego nierozroznialna od powodzenia.
		Sprawdz(MaLiteral("Clipboard is busy, link not copied!"), "komunikat o zajetym schowku przy kopiowaniu odsylacza");
		Sprawdz(MaLiteral("Clipboard is busy, list not copied!"), "komunikat o zajetym schowku przy kopiowaniu listy");
		Sprawdz(MaLiteral("Clipboard is busy, selection not copied!"), "komunikat o zajetym schowku przy kopiowaniu zaznaczenia");
		Sprawdz(MaLiteral("Clipboard is busy, heading not copied!"), "komunikat o zajetym schowku przy kopiowaniu naglowka");
		Sprawdz(MaLiteral("Clipboard is busy, text not copied!"), "komunikat o zajetym schowku przy kopiowaniu wiersza");

		// ---- 3. NIENARUSZALNOSC POPRZEDNICH RODZIN ----
		Sprawdz(Metoda(tUtil, "MigrateDefaultExtensionToMarkdown") != null || Metoda(asm.GetType("EdSharp.App"), "MigrateDefaultExtensionToMarkdown") != null,
			"migracja rozszerzenia z 5.0.59 nadal w binarce");
		Sprawdz(MaLiteral("Mail client refused the attachment!"), "komunikat poczty z 5.0.59 nadal w binarce");
		Sprawdz(MaLiteral("Selection copied"), "kopiowanie zaznaczenia z 5.0.57 nadal w binarce");

		Console.WriteLine();
		Console.WriteLine("WYNIK: " + iPass + " PASS / " + iFail + " FAIL");
		return iFail == 0 ? 0 : 1;
	}
}
