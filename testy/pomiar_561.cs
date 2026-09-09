
// Sonda dla 5.0.61: zapis pliku .rtf rozstrzygany po PROWENIENCJI dokumentu,
// nie po rozszerzeniu, oraz rozroznienie dwoch pozycji listy eksportu o tej
// samej nazwie.
//
// Pyta ZBUDOWANA BINARKE: refleksja o pole i metody plus DOKLADNE literaly w
// tablicy #US (dopasowanie z prefiksem dlugosci, zeby trafienie nie bylo
// podciagiem dluzszego napisu - lekcja z 5.0.58).
//
// KONTROLA NEGATYWNA: ta sama sonda na binarce 5.0.60 musi skonczyc sie kodem 3
// na BRAKU pola IsRichTextDocument.
using System;
using System.IO;
using System.Reflection;
using System.Text;

public class Pomiar561 {
	static int iPass = 0, iFail = 0;
	static byte[] aBytes;
	static void Ok(string s) { iPass++; Console.WriteLine("PASS: " + s); }
	static void Zle(string s) { iFail++; Console.WriteLine("FAIL: " + s); }
	static void Sprawdz(bool b, string s) { if (b) Ok(s); else Zle(s); }

	// Prefiks dlugosci literalu w tablicy #US to "compressed unsigned integer":
	// od 0x80 DWA bajty BIG-ENDIAN, nie varint little-endian (defekt wlasnej
	// sondy zlapany 01.09.2026 - dawal falszywe zero na literalach dluzszych
	// niz 63 znaki).
	static byte[] Prefiks(int iBajtow) {
		int n = iBajtow + 1; // #US trzyma dodatkowy bajt znacznika na koncu
		if (n < 0x80) return new byte[] { (byte) n };
		if (n < 0x4000) return new byte[] { (byte) (0x80 | (n >> 8)), (byte) (n & 0xFF) };
		return new byte[] { (byte) (0xC0 | (n >> 24)), (byte) ((n >> 16) & 0xFF), (byte) ((n >> 8) & 0xFF), (byte) (n & 0xFF) };
	}

	static bool MaLiteral(string s) {
		byte[] aTekst = Encoding.Unicode.GetBytes(s);
		byte[] aPre = Prefiks(aTekst.Length);
		byte[] aWzor = new byte[aPre.Length + aTekst.Length];
		Array.Copy(aPre, 0, aWzor, 0, aPre.Length);
		Array.Copy(aTekst, 0, aWzor, aPre.Length, aTekst.Length);
		for (int i = 0; i + aWzor.Length <= aBytes.Length; i++) {
			bool bOk = true;
			for (int j = 0; j < aWzor.Length; j++) if (aBytes[i + j] != aWzor[j]) { bOk = false; break; }
			if (bOk) return true;
		}
		return false;
	}

	public static int Main(string[] args) {
		string sExe = args.Length > 0 ? args[0] : @"C:\edsharp_probe\EdSharpNG_561.exe";
		Console.WriteLine("BINARKA: " + sExe);
		aBytes = File.ReadAllBytes(sExe);
		Assembly asm = Assembly.LoadFile(sExe);
		Type tChild = asm.GetType("EdSharp.MdiChild");
		Type tFrame = asm.GetType("EdSharp.MdiFrame");
		if (tChild == null || tFrame == null) { Console.WriteLine("BRAK typow"); return 3; }

		// 1. POLE PROWENIENCJI ISTNIEJE. To jest asercja rozstrzygajaca dla
		//    kontroli negatywnej: w 5.0.60 tego pola NIE MA.
		FieldInfo fi = tChild.GetField("IsRichTextDocument");
		if (fi == null) { Console.WriteLine("BRAK pola IsRichTextDocument - to wersja sprzed zmiany"); return 3; }
		Ok("pole IsRichTextDocument istnieje w MdiChild");
		Sprawdz(fi.FieldType == typeof(bool), "pole jest typu bool");

		// 2. KONTROLA POZYTYWNA DOPASOWANIA LITERALU: napis, ktory w binarce
		//    JEST od dawna.  Bez niej zielone "brak literalu" nie odroznia
		//    naprawy od gluchej sondy (falszywe zero z kodowania/prefiksu).
		Sprawdz(MaLiteral("Treat as rich text?"), "kontrola pozytywna dopasowania: znany literal 'Treat as rich text?' jest widoczny");
		Sprawdz(!MaLiteral("Zupelnie nieistniejacy napis kontrolny 561"), "kontrola rozlaczna: napis, ktorego nie ma, NIE jest znajdowany");

		// 3. NOWY KOMUNIKAT MOWY O ZAPISIE TEKSTEM.
		Sprawdz(MaLiteral("Saving as plain text, not rich text"), "komunikat 'Saving as plain text, not rich text' jest w binarce");

		// 4. ROZROZNIENIE POZYCJI LISTY EKSPORTU.
		Sprawdz(MaLiteral(" (converted)"), "przyrostek ' (converted)' jest w binarce");
		Sprawdz(MaLiteral(" (as shown)"), "przyrostek ' (as shown)' jest w binarce");

		// 5. METODY ZAPISU I WCZYTANIA NADAL ISTNIEJA (nie zamienilem drogi na
		//    osierocony helper - lekcja z 5.0.44).
		Sprawdz(tChild.GetMethod("SaveTextOrRtfFile") != null, "SaveTextOrRtfFile nadal istnieje");
		MethodInfo miLoad = tChild.GetMethod("LoadTextOrRtfFile", new Type[] { typeof(string), typeof(bool) });
		Sprawdz(miLoad != null, "LoadTextOrRtfFile(string,bool) nadal istnieje");

		Console.WriteLine();
		Console.WriteLine("PASS " + iPass + " / FAIL " + iFail);
		return iFail == 0 ? 0 : 1;
	}
}
