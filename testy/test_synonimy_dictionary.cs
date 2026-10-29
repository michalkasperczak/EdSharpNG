// Odbior dystrybuowanego indeksu synonimow: nie tylko parser atrapy.
using System;
using System.Collections.Generic;
using System.IO;

public static class TestSynonimyDictionary {

static int iFailures;

static void True(bool bValue, string sName) {
if (!bValue) { Console.WriteLine("FAIL " + sName); iFailures++; }
else Console.WriteLine("PASS " + sName);
} // True method

static bool HasSynonym(List<Synonimy.Grupa> lGroups, string sWord) {
foreach (Synonimy.Grupa group in lGroups) foreach (string sSynonym in group.Synonyms) if (sSynonym == sWord) return true;
return false;
} // HasSynonym method

public static int Main() {
string sFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Synonyms", "plwordnet-synonyms.txt");
Synonimy.ResetForTests();
True(File.Exists(sFile), "runtime synonym index is present");
True(Synonimy.Load(sFile), "runtime synonym index loads");
List<Synonimy.Grupa> lDom = Synonimy.Find("DOM");
True(lDom.Count > 0, "common Polish word dom has a group");
True(HasSynonym(lDom, "domostwo") || HasSynonym(lDom, "chata") || HasSynonym(lDom, "mieszkanie"), "dom has a usable synonym");
List<Synonimy.Grupa> lWazny = Synonimy.Find("ważny");
True(lWazny.Count > 0, "important Polish word ważny has a group");
True(!HasSynonym(lWazny, "ważny"), "index does not return the source word");
Console.WriteLine("FAILURES=" + iFailures);
return iFailures == 0 ? 0 : 1;
} // Main method

} // TestSynonimyDictionary class
