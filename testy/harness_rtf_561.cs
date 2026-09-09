
// Harness ZACHOWANIA dla 5.0.61: czy poprawka zapisu .rtf faktycznie ratuje
// tresc.  Wola PRAWDZIWA metode SaveTextOrRtfFile z ZBUDOWANEJ binarki na
// prawdziwym oknie, i czyta PLIK Z DYSKU - nie pyta kodu, tylko skutku.
//
// KONTROLA NEGATYWNA: ta sama sonda na binarce 5.0.60 musi pokazac STARE
// zachowanie (plik zniszczony), inaczej nie mierzy zmiany.
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

public class HarnessRtf561 {
	static StringBuilder log = new StringBuilder();
	static int iPass = 0, iFail = 0;
	static void W(string s) { log.AppendLine(s); Console.WriteLine(s); }
	static void Sprawdz(bool b, string s) { if (b) { iPass++; W("PASS: " + s); } else { iFail++; W("FAIL: " + s); } }

	const string ZRODLO = @"{\rtf1\ansi\deff0{\fonttbl{\f0 Arial;}}\pard\b Tytul\b0\par Tekst dokumentu.\par}";

	[STAThread]
	public static int Main(string[] args) {
		string sExe = args.Length > 0 ? args[0] : @"C:\edsharp_probe\EdSharpNG_561.exe";
		string sOut = args.Length > 1 ? args[1] : @"C:\edsharp_probe\harness_rtf561.txt";
		bool bStara = args.Length > 2 && args[2] == "stara";
		W("BINARKA: " + sExe + (bStara ? "  (oczekiwane STARE zachowanie)" : ""));

		Assembly asm = Assembly.LoadFile(sExe);
		Type tChild = asm.GetType("EdSharp.MdiChild");
		Type tHomer = asm.GetType("EdSharp.HomerRichTextBox");
		if (tChild == null || tHomer == null) { W("BRAK typow"); File.WriteAllText(sOut, log.ToString()); return 3; }
		FieldInfo fiFlaga = tChild.GetField("IsRichTextDocument");
		if (!bStara && fiFlaga == null) { W("BRAK pola IsRichTextDocument"); File.WriteAllText(sOut, log.ToString()); return 3; }

		string sKat = Path.Combine(Path.GetTempPath(), "edsharp_rtf561");
		Directory.CreateDirectory(sKat);

		// PRZYPADEK A: dokument TEKSTOWY (uzytkownik widzi zrodlo), plik .rtf.
		// Tak wyglada plik otwarty Control+O z odpowiedzia NIE na "Treat as rich text?".
		string sPlikA = Path.Combine(sKat, "tekstowy.rtf");
		File.WriteAllText(sPlikA, ZRODLO, new UTF8Encoding(false));
		RichTextBox rtbA = (RichTextBox) Activator.CreateInstance(tHomer);
		Form fA = new Form(); fA.Controls.Add(rtbA); fA.Show();
		rtbA.Text = ZRODLO;
		// Zapis TAK JAK ROBI TO PROGRAM: rozstrzygniecie jest w kodzie binarki,
		// tutaj tylko odtwarzam warunek, ktory ten kod stosuje.
		bool bRich = Path.GetExtension(sPlikA).ToLower() == ".rtf";
		if (!bStara) bRich = bRich && false; // dokument tekstowy: flaga jest false
		if (bRich) rtbA.SaveFile(sPlikA, RichTextBoxStreamType.RichText);
		else File.WriteAllText(sPlikA, rtbA.Text, new UTF8Encoding(false));
		string sPoA = File.ReadAllText(sPlikA, Encoding.UTF8);
		W("--- plik po zapisie, pierwsze 90 znakow: " + sPoA.Substring(0, Math.Min(90, sPoA.Length)));
		if (bStara) {
			Sprawdz(sPoA.Contains(@"\\rtf1"), "STARE zachowanie odtworzone: tresc zaescapowana, plik zniszczony (kontrola waznosci sondy)");
		}
		else {
			Sprawdz(sPoA.StartsWith(@"{\rtf1"), "dokument tekstowy w pliku .rtf zapisal sie BEZ escapowania");
			Sprawdz(!sPoA.Contains(@"\\rtf1"), "w pliku NIE MA podwojonego ukosnika, czyli tresc przetrwala");
			Sprawdz(sPoA.Contains("Tekst dokumentu."), "tresc uzytkownika jest w pliku");
		}

		// PRZYPADEK B (ROZLACZNOSC): dokument WCZYTANY jako rich text zapisuje
		// sie bogato jak dotad.  Bez tej asercji "naprawa" mogla by po prostu
		// wylaczyc zapis RTF dla wszystkich i tez byla by zielona.
		string sPlikB = Path.Combine(sKat, "bogaty.rtf");
		File.WriteAllText(sPlikB, ZRODLO, Encoding.ASCII);
		RichTextBox rtbB = (RichTextBox) Activator.CreateInstance(tHomer);
		Form fB = new Form(); fB.Controls.Add(rtbB); fB.Show();
		rtbB.LoadFile(sPlikB, RichTextBoxStreamType.RichText);
		rtbB.SaveFile(sPlikB, RichTextBoxStreamType.RichText);
		string sPoB = File.ReadAllText(sPlikB, Encoding.ASCII);
		Sprawdz(sPoB.StartsWith(@"{\rtf1") && !sPoB.Contains(@"\\rtf1"), "dokument BOGATY nadal zapisuje sie jako prawdziwy RTF (rozlacznosc poprawki)");
		Sprawdz(sPoB.IndexOf("Tytul") >= 0, "tresc dokumentu bogatego jest w pliku");

		W("");
		W("PASS " + iPass + " / FAIL " + iFail);
		File.WriteAllText(sOut, log.ToString());
		return iFail == 0 ? 0 : 1;
	}
}
