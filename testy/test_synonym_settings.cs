using System;
using System.Linq;

class SynonymSettingsTests {
static int Main() {
var choices = Ustawienia.Spis().Where(o => o.Klucz == "SynonymDictionary").ToArray();
if (choices.Length != 1) { Console.WriteLine("FAIL: dictionary choice must appear exactly once in Settings"); return 1; }
var option = choices[0];
if (option.Rodzaj != "lista" || option.Domyslna != "pl" || option.Zaawansowana || Ustawienia.Pomijany(option.Klucz)) {
Console.WriteLine("FAIL: dictionary must be an ordinary list defaulting to PL"); return 1;
}
if (!option.Wartosci.SequenceEqual(new string[] {"pl", "en", "both"}) ||
!option.Nazwy.SequenceEqual(new string[] {"Polski", "Angielski", "Oba"})) {
Console.WriteLine("FAIL: dictionary list must show human labels for the exact runtime values"); return 1;
}
for (int i=0; i<option.Wartosci.Length; i++) if (Ustawienia.PozycjaDlaWartosci(option, option.Wartosci[i]) != i) {
Console.WriteLine("FAIL: labels must round-trip to the same values"); return 1;
}
Console.WriteLine("OK: Settings exposes exactly one PL/EN/both list, correct labels/default and round-trip");
return 0;
}
}
