// Kontrakt prototypu polskiego slownika synonimow dla EdSharpNG.
// Test jest niezalezny od pelnej bazy: sprawdza dokladnie format indeksu,
// ktory pozniej wygenerujemy z plWordNet 5.0.
using System;
using System.Collections.Generic;
using System.IO;

public static class TestSynonimy {

static int iFailures;

static void Equal(string sExpected, string sActual, string sName) {
if (sExpected != sActual) {
Console.WriteLine("FAIL {0}: expected [{1}], got [{2}]", sName, sExpected, sActual);
iFailures++;
}
else Console.WriteLine("PASS " + sName);
} // Equal method

static void True(bool bValue, string sName) {
if (!bValue) { Console.WriteLine("FAIL " + sName); iFailures++; }
else Console.WriteLine("PASS " + sName);
} // True method

public static int Main() {
string sFile = Path.Combine(Path.GetTempPath(), "edsharpng-synonyms-test.txt");
File.WriteAllText(sFile,
"# EdSharpNG Polish thesaurus index v1\r\n" +
"ważny\ta\t1\tistotny|znaczący|ważki\r\n" +
"ważny\ta\t2\tpoważny|doniosły\r\n" +
"dom\tn\t1\tchata|chałupa|lokum\r\n", System.Text.Encoding.UTF8);
try {
Synonimy.ResetForTests();
Equal("", Synonimy.Error, "no error before loading");
True(Synonimy.Load(sFile), "loads UTF-8 index");
Equal("", Synonimy.Error, "no error after loading");
List<Synonimy.Grupa> lGrupy = Synonimy.Find("WAŻNY");
Equal("2", lGrupy.Count.ToString(), "keeps separate senses");
Equal("istotny|znaczący|ważki", String.Join("|", lGrupy[0].Synonyms.ToArray()), "returns first sense synonyms");
Equal("poważny|doniosły", String.Join("|", lGrupy[1].Synonyms.ToArray()), "returns second sense synonyms");
Equal("a", lGrupy[0].PartOfSpeech, "keeps part of speech");
Equal("0", Synonimy.Find("nieistniejące").Count.ToString(), "missing word has no groups");
Equal("0", Synonimy.Find("ważny\n").Count.ToString(), "rejects multiword selection");
True(Synonimy.IsSingleWord("dom"), "accepts one exact word");
True(!Synonimy.IsSingleWord(" dom "), "rejects padded selection without moving its range");
Equal("Chata", Synonimy.ApplyCapitalization("Dom", "chata"), "preserves initial capital");
Equal("CHATA", Synonimy.ApplyCapitalization("DOM", "chata"), "preserves all capitals");
Equal("chata", Synonimy.ApplyCapitalization("dom", "chata"), "keeps lower case");
File.WriteAllBytes(sFile, new byte[] { 0xc3, 0x28 });
Synonimy.ResetForTests();
True(!Synonimy.Load(sFile), "rejects malformed UTF-8 index");
File.WriteAllText(sFile, "ważny\ta\t1\tważny|istotny|ISTOTNY\r\n", System.Text.Encoding.UTF8);
Synonimy.ResetForTests();
True(Synonimy.Load(sFile), "loads duplicate synonym fixture");
lGrupy = Synonimy.Find("ważny");
Equal("istotny", String.Join("|", lGrupy[0].Synonyms.ToArray()), "removes source word and duplicate synonym");
}
finally {
try { File.Delete(sFile); } catch {}
}
Console.WriteLine("FAILURES=" + iFailures);
return iFailures == 0 ? 0 : 1;
} // Main method

} // TestSynonimy class
