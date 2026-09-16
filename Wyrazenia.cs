// Wyrazenia.cs -- WLASNY KALKULATOR WYRAZEN, NASTEPCA SILNIKA JSCRIPT .NET.
//
// POWOD POWSTANIA (decyzja Michala Kasperczaka, docs/CO-USUWAMY.md punkt 2.2,
// 16.09.2026): "MK. Usuwamy." o warstwie skryptow JScript .NET, i osobno
// "MK. Tak." o komendzie Evaluate Expression, ktora ma ZOSTAC ("to kalkulator,
// jest tani").  Te dwie decyzje razem znacza: liczenie wyrazen musi dzialac
// BEZ biblioteki EdSharp.dll i bez pozno wiazanego JScriptu.
//
// Stary Evaluate Expression (Control+plus) oddawal tekst wiersza do
// Script.run, czyli do interpretera JScript .NET w osobnej bibliotece.
// Dawalo to pelny jezyk programowania w edytorze tekstu -- za duzo, za drogo
// (dodatkowy plik w paczce, refleksja, klasa bledow niewidoczna dla
// kompilatora) i na jezyku, ktory Microsoft porzucil.
//
// TO JEST KALKULATOR, NIE JEZYK.  Umie dokladnie tyle, ile trzeba przy pisaniu
// tekstu: liczby, cztery dzialania, potege, reszte z dzielenia, nawiasy,
// procent oraz garsc funkcji matematycznych.  Nie umie zmiennych, przypisan,
// petli ani wywolan systemowych -- i ma nie umiec.
//
// DLACZEGO WLASNY PARSER, A NIE DataTable.Compute: Compute nie zna potegi,
// zaokraglen ani funkcji, a bledy zglasza wyjatkiem z komunikatem po
// angielsku, ktorego nie da sie sensownie powiedziec czytnikiem.  Tu kazdy
// blad wraca jako krotkie zdanie, gotowe do powiedzenia.
//
// KROPKA CZY PRZECINEK: w polskim ukladzie klawiatury przecinek jest naturalny
// dla czlowieka, a kropka dla komputera.  Parser przyjmuje OBA jako separator
// dziesietny, bo pisze to czlowiek.  Przecinek rozdzielajacy argumenty funkcji
// (min, max) jest rozpoznawany po tym, ze stoi WEWNATRZ nawiasu funkcji -- to
// jedyne miejsce, gdzie jest dwuznaczny, i tam wygrywa rola separatora
// argumentow.  Wynik zapisywany jest zawsze z KROPKA, kultura niezalezna, bo
// taka postac wklejona do dokumentu czyta sie tak samo na kazdej maszynie.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public class Wyrazenia
{
    // Wynik liczenia: albo wartosc, albo komunikat bledu gotowy do mowienia.
    public class Wynik
    {
        public bool Udane;
        public double Wartosc;
        public string Blad;
    }

    // Policz wyrazenie i zwroc gotowy NAPIS.  Puste, gdy nie ma czego liczyc
    // (wiersz bez liczb) -- wtedy komenda po prostu milczy, tak jak dawniej
    // przy pustym wyniku skryptu.  Komunikat bledu wraca jako tekst zaczynajacy
    // sie od wykrzyknika, zeby dzwonicy nie pomylil go z wynikiem.
    public static string Policz(string sWyrazenie)
    {
        Wynik w = Ocen(sWyrazenie);
        if (!w.Udane) return (w.Blad.Length == 0) ? "" : "!" + w.Blad;
        return Sformatuj(w.Wartosc);
    }

    // Postac wyniku: bez zbednych zer na koncu, z kropka dziesietna, zaokraglona
    // do 12 cyfr znaczacych.  Zaokraglenie jest konieczne, bo arytmetyka
    // zmiennoprzecinkowa daje 0.30000000000000004 na 0.1+0.2, a czytnik
    // przeczytalby to w calosci.
    public static string Sformatuj(double d)
    {
        if (double.IsNaN(d)) return "!Nie jest liczba";
        if (double.IsInfinity(d)) return "!Nieskonczonosc";
        double dR = Math.Round(d, 10);
        if (Math.Abs(dR) < 1e-10) dR = 0;
        string s = dR.ToString("G12", CultureInfo.InvariantCulture);
        if (s.Contains("E"))
        {
            // Postac wykladnicza czyta sie zle, wiec dla umiarkowanych liczb
            // rozpisujemy ja zwyklym zapisem.
            if (Math.Abs(dR) < 1e15 && Math.Abs(dR) > 1e-10)
                s = dR.ToString("0.##########", CultureInfo.InvariantCulture);
        }
        return s;
    }

    public static Wynik Ocen(string sWyrazenie)
    {
        Wynik w = new Wynik();
        w.Udane = false;
        w.Blad = "";
        if (sWyrazenie == null) return w;

        string s = Przygotuj(sWyrazenie);
        if (s.Length == 0) return w;          // nie ma czego liczyc: cisza

        try
        {
            int i = 0;
            double d = Suma(s, ref i);
            Omin(s, ref i);
            if (i < s.Length)
            {
                w.Blad = "Nie rozumiem znaku " + Nazwij(s[i]);
                return w;
            }
            w.Udane = true;
            w.Wartosc = d;
            return w;
        }
        catch (BladWyrazenia bw) { w.Blad = bw.Message; return w; }
        catch (Exception) { w.Blad = "Nie umiem tego policzyc"; return w; }
    }

    // ---------- przygotowanie tekstu ----------
    //
    // Wiersz dokumentu bywa opisem, nie samym wyrazeniem ("razem: 12 + 3 zl").
    // Stary silnik JScript przewrocilby sie na takim wierszu.  Tu wycinamy
    // czesc, ktora wyglada na wyrazenie: od pierwszej liczby, kropki dziesietnej
    // albo nawiasu do konca ciagu znakow, ktore parser rozumie.  Dzieki temu
    // Control+plus dziala na wierszu z ogonkiem tekstu, a nie tylko na czystym
    // dzialaniu.
    private static string Przygotuj(string sText)
    {
        string s = sText.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
        // Znak rownosci na koncu ("2+2=") jest naturalnym zapisem czlowieka.
        s = s.TrimEnd();
        while (s.EndsWith("=")) s = s.Substring(0, s.Length - 1).TrimEnd();
        // Zapis z odstepami tysiecy ("12 000") laczymy, zeby nie wyszly dwie liczby.
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == ' ' && i > 0 && i + 1 < s.Length && char.IsDigit(s[i - 1]) && char.IsDigit(s[i + 1])) continue;
            sb.Append(c);
        }
        s = sb.ToString();

        int iStart = -1;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (char.IsDigit(c) || c == '(' || c == '-' && i + 1 < s.Length && (char.IsDigit(s[i + 1]) || s[i + 1] == '.')) { iStart = i; break; }
            if (char.IsLetter(c) && ZnaneSlowo(s, i)) { iStart = i; break; }
        }
        if (iStart == -1) return "";
        s = s.Substring(iStart);

        int iEnd = s.Length;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            bool bOk = char.IsDigit(c) || char.IsLetter(c) || c == ' ' || c == '.' || c == ','
                || c == '+' || c == '-' || c == '*' || c == '/' || c == '%' || c == '^'
                || c == '(' || c == ')';
            if (!bOk) { iEnd = i; break; }
        }
        return s.Substring(0, iEnd).Trim();
    }

    private static bool ZnaneSlowo(string s, int i)
    {
        string[] a = new string[] { "pi", "e", "sqrt", "abs", "round", "floor", "ceil", "trunc",
            "min", "max", "pow", "log", "log10", "ln", "exp", "sin", "cos", "tan" };
        foreach (string sName in a)
        {
            if (i + sName.Length > s.Length) continue;
            if (string.Compare(s.Substring(i, sName.Length), sName, StringComparison.OrdinalIgnoreCase) != 0) continue;
            int j = i + sName.Length;
            if (j < s.Length && (char.IsLetterOrDigit(s[j]))) continue;
            return true;
        }
        return false;
    }

    // ---------- gramatyka ----------
    // Suma     := Iloczyn (('+'|'-') Iloczyn)*
    // Iloczyn  := Potega (('*'|'/'|'%') Potega)*
    // Potega   := Jednostka ('^' Potega)?        -- prawostronna, jak w matematyce
    // Jednostka:= ('-'|'+')? (liczba | '(' Suma ')' | funkcja | stala) ('%')?

    private class BladWyrazenia : Exception
    {
        public BladWyrazenia(string s) : base(s) { }
    }

    private static void Omin(string s, ref int i)
    {
        while (i < s.Length && s[i] == ' ') i++;
    }

    private static string Nazwij(char c)
    {
        if (c == ' ') return "odstep";
        return "\"" + c + "\"";
    }

    private static double Suma(string s, ref int i)
    {
        double d = Iloczyn(s, ref i);
        while (true)
        {
            Omin(s, ref i);
            if (i >= s.Length) return d;
            char c = s[i];
            if (c == '+') { i++; d += Iloczyn(s, ref i); }
            else if (c == '-') { i++; d -= Iloczyn(s, ref i); }
            else return d;
        }
    }

    private static double Iloczyn(string s, ref int i)
    {
        double d = Potega(s, ref i);
        while (true)
        {
            Omin(s, ref i);
            if (i >= s.Length) return d;
            char c = s[i];
            if (c == '*') { i++; d *= Potega(s, ref i); }
            else if (c == '/')
            {
                i++;
                double dDziel = Potega(s, ref i);
                if (dDziel == 0) throw new BladWyrazenia("Dzielenie przez zero");
                d /= dDziel;
            }
            else if (c == '%')
            {
                // Procent POSTFIKSOWY jest zjadany w Jednostce; tutaj '%' moze
                // znaczyc tylko reszte z dzielenia ("7 % 3").
                i++;
                double dMod = Potega(s, ref i);
                if (dMod == 0) throw new BladWyrazenia("Dzielenie przez zero");
                d = d % dMod;
            }
            else return d;
        }
    }

    private static double Potega(string s, ref int i)
    {
        double d = Jednostka(s, ref i);
        Omin(s, ref i);
        if (i < s.Length && s[i] == '^')
        {
            i++;
            double dWyk = Potega(s, ref i);
            return Math.Pow(d, dWyk);
        }
        return d;
    }

    private static double Jednostka(string s, ref int i)
    {
        Omin(s, ref i);
        if (i >= s.Length) throw new BladWyrazenia("Wyrazenie urwane");

        char c = s[i];
        if (c == '-') { i++; return -Jednostka(s, ref i); }
        if (c == '+') { i++; return Jednostka(s, ref i); }

        double d;
        if (c == '(')
        {
            i++;
            d = Suma(s, ref i);
            Omin(s, ref i);
            if (i >= s.Length || s[i] != ')') throw new BladWyrazenia("Brak nawiasu zamykajacego");
            i++;
        }
        else if (char.IsDigit(c) || c == '.' || c == ',')
        {
            d = Liczba(s, ref i);
        }
        else if (char.IsLetter(c))
        {
            d = Nazwa(s, ref i);
        }
        else throw new BladWyrazenia("Nie rozumiem znaku " + Nazwij(c));

        // Procent postfiksowy: "20%" to 0.2.  Sprawdzamy, czy po '%' NIE stoi
        // kolejna jednostka -- wtedy to reszta z dzielenia, nie procent.
        Omin(s, ref i);
        if (i < s.Length && s[i] == '%')
        {
            int j = i + 1;
            while (j < s.Length && s[j] == ' ') j++;
            bool bNastepnaJednostka = j < s.Length && (char.IsDigit(s[j]) || s[j] == '(' || s[j] == '.' || char.IsLetter(s[j]));
            if (!bNastepnaJednostka) { i++; d = d / 100.0; }
        }
        return d;
    }

    private static double Liczba(string s, ref int i)
    {
        int iStart = i;
        bool bKropka = false;
        StringBuilder sb = new StringBuilder();
        while (i < s.Length)
        {
            char c = s[i];
            if (char.IsDigit(c)) { sb.Append(c); i++; continue; }
            if ((c == '.' || c == ',') && !bKropka)
            {
                // Przecinek jest separatorem dziesietnym tylko wtedy, gdy PO nim
                // stoi cyfra; inaczej rozdziela argumenty funkcji.
                if (i + 1 < s.Length && char.IsDigit(s[i + 1])) { sb.Append('.'); bKropka = true; i++; continue; }
                break;
            }
            break;
        }
        if (sb.Length == 0) throw new BladWyrazenia("Oczekiwalem liczby");
        double d;
        if (!double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out d))
            throw new BladWyrazenia("Zla liczba " + s.Substring(iStart, i - iStart));
        return d;
    }

    private static double Nazwa(string s, ref int i)
    {
        int iStart = i;
        while (i < s.Length && char.IsLetterOrDigit(s[i])) i++;
        string sNazwa = s.Substring(iStart, i - iStart).ToLower();

        if (sNazwa == "pi") return Math.PI;
        if (sNazwa == "e") return Math.E;

        Omin(s, ref i);
        if (i >= s.Length || s[i] != '(') throw new BladWyrazenia("Nie znam nazwy " + sNazwa);
        i++;
        List<double> l = new List<double>();
        Omin(s, ref i);
        if (i < s.Length && s[i] == ')') i++;
        else
        {
            while (true)
            {
                l.Add(Suma(s, ref i));
                Omin(s, ref i);
                if (i < s.Length && (s[i] == ',' || s[i] == ';')) { i++; continue; }
                if (i < s.Length && s[i] == ')') { i++; break; }
                throw new BladWyrazenia("Brak nawiasu zamykajacego w " + sNazwa);
            }
        }

        double a0 = (l.Count > 0) ? l[0] : 0;
        switch (sNazwa)
        {
            case "sqrt":
                if (a0 < 0) throw new BladWyrazenia("Pierwiastek z liczby ujemnej");
                return Math.Sqrt(a0);
            case "abs": return Math.Abs(a0);
            case "round":
                if (l.Count > 1) return Math.Round(a0, (int) l[1], MidpointRounding.AwayFromZero);
                return Math.Round(a0, MidpointRounding.AwayFromZero);
            case "floor": return Math.Floor(a0);
            case "ceil": return Math.Ceiling(a0);
            case "trunc": return Math.Truncate(a0);
            case "min":
                if (l.Count < 2) throw new BladWyrazenia("min potrzebuje dwoch liczb");
                { double d = a0; for (int k = 1; k < l.Count; k++) d = Math.Min(d, l[k]); return d; }
            case "max":
                if (l.Count < 2) throw new BladWyrazenia("max potrzebuje dwoch liczb");
                { double d = a0; for (int k = 1; k < l.Count; k++) d = Math.Max(d, l[k]); return d; }
            case "pow":
                if (l.Count < 2) throw new BladWyrazenia("pow potrzebuje dwoch liczb");
                return Math.Pow(a0, l[1]);
            case "log":
                if (l.Count > 1) return Math.Log(a0, l[1]);
                return Math.Log(a0);
            case "ln": return Math.Log(a0);
            case "log10": return Math.Log10(a0);
            case "exp": return Math.Exp(a0);
            case "sin": return Math.Sin(a0);
            case "cos": return Math.Cos(a0);
            case "tan": return Math.Tan(a0);
        }
        throw new BladWyrazenia("Nie znam funkcji " + sNazwa);
    }

    // ---------- rozwijanie zapisu z odwrotnym ukosnikiem ----------
    //
    // DRUGI NASTEPCA JSCRIPTU.  Util.Literalize oddawala tekst do Script.run
    // owinietego w cudzyslowy, zeby JScript rozwinal \n, \t i \uXXXX.  Silnik
    // odchodzi, wiec robimy to sami.  Regex.Unescape sam nie wystarczy: rzuca
    // wyjatkiem na pojedynczym ukosniku ("C:\dane"), a takie sciezki siedza w
    // pliku ustawien uzytkownika.  Tu nieznana sekwencja zostaje NIETKNIETA,
    // czyli sciezka przechodzi bez zmiany -- to zachowanie bezpieczne dla
    // istniejacych plikow ustawien.
    public static string Rozwin(string sText)
    {
        if (sText == null) return "";
        if (sText.IndexOf('\\') == -1) return sText;
        StringBuilder sb = new StringBuilder(sText.Length);
        for (int i = 0; i < sText.Length; i++)
        {
            char c = sText[i];
            if (c != '\\' || i + 1 >= sText.Length) { sb.Append(c); continue; }
            char n = sText[i + 1];
            switch (n)
            {
                case 'n': sb.Append('\n'); i++; break;
                case 'r': sb.Append('\r'); i++; break;
                case 't': sb.Append('\t'); i++; break;
                case 'f': sb.Append('\f'); i++; break;
                case 'b': sb.Append('\b'); i++; break;
                case 'v': sb.Append('\v'); i++; break;
                case '0': sb.Append('\0'); i++; break;
                case 'a': sb.Append('\a'); i++; break;
                case '\\': sb.Append('\\'); i++; break;
                case '"': sb.Append('"'); i++; break;
                case '\'': sb.Append('\''); i++; break;
                case 'u':
                case 'x':
                    {
                        int iLen = (n == 'u') ? 4 : 2;
                        if (i + 2 + iLen <= sText.Length)
                        {
                            string sHex = sText.Substring(i + 2, iLen);
                            int iKod;
                            if (int.TryParse(sHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out iKod))
                            {
                                sb.Append((char) iKod);
                                i += 1 + iLen;
                                break;
                            }
                        }
                        sb.Append(c);   // nieznany zapis zostaje jak byl
                        break;
                    }
                default:
                    sb.Append(c);       // np. "C:\dane" - ukosnik zostaje
                    break;
            }
        }
        return sb.ToString();
    }
} // Wyrazenia class
