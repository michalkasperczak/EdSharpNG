using System;
using System.Collections.Generic;
using System.Text;

public class T {

enum MarkdownReviewKind {Heading = 1, List = 2, ListItem = 3, Link = 4, Table = 5}

	static int MarkdownReview_SkipListMarkerAt(string sText, int iOffset) {
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

	static List<int> MarkdownReview_ShiftPastListMarkers(string sText, List<int> source, MarkdownReviewKind kind) {
	if (source == null) return null;
	if (kind != MarkdownReviewKind.List && kind != MarkdownReviewKind.ListItem) return source;
	if (String.IsNullOrEmpty(sText)) return source;
	List<int> result = new List<int>();
	foreach (int i in source) result.Add(MarkdownReview_SkipListMarkerAt(sText, i));
	MarkdownReview_SortUnique(result);
	return result;
	} // MarkdownReview_ShiftPastListMarkers method


		static bool MarkdownReview_GetListMarkerSpan(string sLine, out int iMarkerStart, out int iMarkerLength) {
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


	static List<int[]> MarkdownReview_FindFenceRanges(string sText) {
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


	static bool MarkdownReview_IsIndexInRangesSorted(List<int[]> ranges, int iIndex) {
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


	static void MarkdownReview_SortUnique(List<int> list) {
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


	static int MarkdownReview_FindTarget(List<int> list, int iCurrent, bool bReverse) {
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

static int iFail = 0;
static void Chk(string sName, object oGot, object oWant) {
bool bOk = String.Equals(Convert.ToString(oGot), Convert.ToString(oWant));
Console.WriteLine((bOk ? "PASS " : "FAIL ") + sName + "  got=" + oGot + " want=" + oWant);
if (!bOk) iFail++;
}

public static int Main() {
string LB = "\r\n";

// 1. Punktor listy punktowanej: kursor na poczatku wiersza -> pierwszy znak tresci.
string s1 = "- [Instalacja](#instalacja)" + LB;
Chk("1a punktor->tresc", MarkdownReview_SkipListMarkerAt(s1, 0), 2);
Chk("1b w spacji za punktorem", MarkdownReview_SkipListMarkerAt(s1, 1), 2);
Chk("1c juz w tresci: bez zmian", MarkdownReview_SkipListMarkerAt(s1, 5), 5);

// 2. Lista NUMEROWANA (cala klasa, nie tylko punktory).
string s2 = "12. [Rozdzial](#rozdzial)" + LB;
Chk("2a numer->tresc", MarkdownReview_SkipListMarkerAt(s2, 0), 4);
Chk("2b w kropce", MarkdownReview_SkipListMarkerAt(s2, 2), 4);

// 3. WCIECIE (spis tresci ma poziomy przez dwie spacje na poziom).
string s3 = "  - [Podrozdzial](#p)" + LB;
Chk("3a wciecie->tresc", MarkdownReview_SkipListMarkerAt(s3, 0), 4);
Chk("3b w punktorze", MarkdownReview_SkipListMarkerAt(s3, 2), 4);

// 4. KONTROLA NEGATYWNA: zwykly akapit i naglowek NIE sa ruszane.
string s4 = "Zwykly akapit z [linkiem](#a)." + LB;
Chk("4a akapit bez zmian", MarkdownReview_SkipListMarkerAt(s4, 0), 0);
string s4b = "## Naglowek" + LB;
Chk("4b naglowek bez zmian", MarkdownReview_SkipListMarkerAt(s4b, 0), 0);

// 5. KONTROLA NEGATYWNA: myslnik bez spacji to NIE lista.
string s5 = "-nie lista" + LB;
Chk("5 myslnik bez spacji", MarkdownReview_SkipListMarkerAt(s5, 0), 0);

// 6. Pozycja BEZ TRESCI: nie ma na czym stanac, kursor stoi.
string s6 = "- " + LB + "dalej";
Chk("6 pusta pozycja", MarkdownReview_SkipListMarkerAt(s6, 0), 0);

// 7. BLOK KODU: "- " w przykladzie to zwykly tekst, nie ruszamy.
string s7 = "```" + LB + "- to jest przyklad" + LB + "```" + LB;
int iInFence = 5;
Chk("7 w bloku kodu bez zmian", MarkdownReview_SkipListMarkerAt(s7, iInFence), iInFence);

// 8. DRUGI wiersz listy w dokumencie wielowierszowym.
string s8 = "# Spis" + LB + LB + "- [A](#a)" + LB + "- [B](#b)" + LB;
int iDrugi = s8.IndexOf("- [B]");
Chk("8 druga pozycja", MarkdownReview_SkipListMarkerAt(s8, iDrugi), iDrugi + 2);

// 9. Korekta CALEJ listy celow tylko dla list.
List<int> cele = new List<int>();
cele.Add(s8.IndexOf("- [A]"));
cele.Add(iDrugi);
List<int> poprawione = MarkdownReview_ShiftPastListMarkers(s8, cele, MarkdownReviewKind.ListItem);
Chk("9a pierwszy cel", poprawione[0], cele[0] + 2);
Chk("9b drugi cel", poprawione[1], cele[1] + 2);
List<int> naglowki = MarkdownReview_ShiftPastListMarkers(s8, cele, MarkdownReviewKind.Heading);
Chk("9c naglowki nietkniete", naglowki[0], cele[0]);

// 10. ROZSTRZYGAJACY: nawigacja w PRZOD i w TYL nie stoi w miejscu.
// Stoimy na tresci pozycji B; Shift+I ma cofnac do tresci pozycji A.
int iNaB = iDrugi + 2;
int iWTyl = MarkdownReview_FindTarget(poprawione, iNaB, true);
Chk("10a w tyl na tresc A", iWTyl, cele[0] + 2);
int iWPrzod = MarkdownReview_FindTarget(poprawione, cele[0] + 2, false);
Chk("10b w przod na tresc B", iWPrzod, iDrugi + 2);

// 11. KONTROLA NEGATYWNA STAREGO STANU: bez korekty cele to PUNKTORY, wiec
// stojac na tresci pozycji B skok w tyl trafia na punktor TEJ SAMEJ pozycji B -
// nawigacja stoi w miejscu, a pod kursorem nadal nie ma odsylacza.  Ten test
// MUSI pokazac roznice, inaczej caly pomiar nic nie dowodzi.
int iStary = MarkdownReview_FindTarget(cele, iNaB, true);
Chk("11a stary cel to punktor tego samego wiersza", iStary, cele[1]);
Chk("11b znak pod starym kursorem", s8[iStary].ToString(), "-");
Chk("11c znak pod nowym kursorem", s8[iWTyl].ToString(), "[");
Chk("11d stary skok nie zmienil wiersza", iStary > cele[0] + 2, true);

Console.WriteLine(iFail == 0 ? "WSZYSTKIE PRZESZLY" : ("BLEDOW: " + iFail));
return iFail == 0 ? 0 : 1;
}
}
