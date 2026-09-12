// pomiar_nazwy_skrotow.cs -- jak brzmi chord po zamianie na tekst.
// Chodzi o to, co USLYSZY czytnik ekranu w palecie polecen: "Alt+Back" i
// "Alt+OemQuotes" to nazwy z wnetrza .NET, nie nazwy klawiszy, ktore czlowiek
// ma pod palcami.
using System;
using System.ComponentModel;
using System.Windows.Forms;

class PomiarNazwySkrotow {

// Surowa zamiana - to, co robil Util.Key2String.
static string Surowo(Keys k) {
return TypeDescriptor.GetConverter(typeof(Keys)).ConvertToString(k);
}

// Zamiana CZYTELNA - odwzorowanie Util.KeyToSpoken z EdSharp.cs.
static string Czytelnie(Keys keyData) {
if (keyData == Keys.None) return "";
Keys keyCode = keyData & Keys.KeyCode;
string sName;
switch (keyCode) {
case Keys.Back: sName = "Backspace"; break;
case Keys.Return: sName = "Enter"; break;
case Keys.Escape: sName = "Escape"; break;
case Keys.Space: sName = "Space"; break;
case Keys.Left: sName = "Left Arrow"; break;
case Keys.Right: sName = "Right Arrow"; break;
case Keys.Up: sName = "Up Arrow"; break;
case Keys.Down: sName = "Down Arrow"; break;
case Keys.PageUp: sName = "Page Up"; break;
case Keys.PageDown: sName = "Page Down"; break;
case Keys.Delete: sName = "Delete"; break;
case Keys.Insert: sName = "Insert"; break;
case Keys.Apps: sName = "Applications"; break;
case Keys.Oemcomma: sName = "Comma"; break;
case Keys.OemPeriod: sName = "Period"; break;
case Keys.OemQuestion: sName = "Slash"; break;
case Keys.OemMinus: sName = "Minus"; break;
case Keys.Oemplus: sName = "Equals"; break;
case Keys.OemQuotes: sName = "Apostrophe"; break;
case Keys.OemSemicolon: sName = "Semicolon"; break;
case Keys.OemOpenBrackets: sName = "Left Bracket"; break;
case Keys.OemCloseBrackets: sName = "Right Bracket"; break;
case Keys.OemPipe: sName = "Backslash"; break;
case Keys.Oemtilde: sName = "Grave Accent"; break;
default:
sName = keyCode.ToString();
if (sName.Length == 2 && sName[0] == 'D' && Char.IsDigit(sName[1])) sName = sName.Substring(1);
break;
}
string sMods = "";
if ((keyData & Keys.Control) == Keys.Control) sMods += "Control+";
if ((keyData & Keys.Alt) == Keys.Alt) sMods += "Alt+";
if ((keyData & Keys.Shift) == Keys.Shift) sMods += "Shift+";
return sMods + sName;
}

static int iZgodne = 0, iNiezgodne = 0;
static void Sprawdz(Keys k, string sOczek) {
string sJest = Czytelnie(k);
bool bOk = (sJest == sOczek);
if (bOk) iZgodne++; else iNiezgodne++;
Console.WriteLine("{0} {1,-34} czytelnie: [{2}]  surowo: [{3}]",
bOk ? "ZGODNE  " : "NIEZGODNE", k.ToString(), sJest, Surowo(k));
}

static void Main() {
Console.WriteLine("== JAK BRZMI SKROT W PALECIE POLECEN ==\n");

// Te przypadki brzmialy zle: nazwy z wnetrza .NET.
Sprawdz(Keys.Alt | Keys.Back, "Alt+Backspace");
Sprawdz(Keys.Control | Keys.Shift | Keys.Back, "Control+Shift+Backspace");
Sprawdz(Keys.Alt | Keys.OemQuotes, "Alt+Apostrophe");
Sprawdz(Keys.Alt | Keys.OemSemicolon, "Alt+Semicolon");
Sprawdz(Keys.Alt | Keys.OemQuestion, "Alt+Slash");
Sprawdz(Keys.Alt | Keys.OemMinus, "Alt+Minus");
Sprawdz(Keys.Control | Keys.Shift | Keys.Oemcomma, "Control+Shift+Comma");
Sprawdz(Keys.Control | Keys.Shift | Keys.OemPeriod, "Control+Shift+Period");
Sprawdz(Keys.Control | Keys.Shift | Keys.OemCloseBrackets, "Control+Shift+Right Bracket");
Sprawdz(Keys.Control | Keys.Shift | Keys.OemOpenBrackets, "Control+Shift+Left Bracket");
Sprawdz(Keys.Alt | Keys.Shift | Keys.OemCloseBrackets, "Alt+Shift+Right Bracket");

// Strzalki - "Alt+Left" mowione bez slowa "arrow" jest dwuznaczne.
Sprawdz(Keys.Alt | Keys.Left, "Alt+Left Arrow");
Sprawdz(Keys.Alt | Keys.Right, "Alt+Right Arrow");
Sprawdz(Keys.Alt | Keys.Up, "Alt+Up Arrow");
Sprawdz(Keys.Alt | Keys.Down, "Alt+Down Arrow");
Sprawdz(Keys.Control | Keys.Down, "Control+Down Arrow");

// Zwykle przypadki - musza zostac bez zmian.
Sprawdz(Keys.F7, "F7");
Sprawdz(Keys.Shift | Keys.F7, "Shift+F7");
Sprawdz(Keys.Control | Keys.Shift | Keys.F1, "Control+Shift+F1");
Sprawdz(Keys.Control | Keys.O, "Control+O");
Sprawdz(Keys.Control | Keys.Shift | Keys.X, "Control+Shift+X");
Sprawdz(Keys.Alt | Keys.F10, "Alt+F10");
Sprawdz(Keys.Apps, "Applications");
Sprawdz(Keys.Shift | Keys.Space, "Shift+Space");
Sprawdz(Keys.Control | Keys.Tab, "Control+Tab");
Sprawdz(Keys.Alt | Keys.Shift | Keys.Space, "Alt+Shift+Space");

// Cyfry: Keys.D1 nie moze zostac "D1".
Sprawdz(Keys.Alt | Keys.D1, "Alt+1");
Sprawdz(Keys.Alt | Keys.D0, "Alt+0");
Sprawdz(Keys.Alt | Keys.Shift | Keys.D6, "Alt+Shift+6");

// Kolejnosc modyfikatorow zawsze ta sama: Control, Alt, Shift.
Sprawdz(Keys.Control | Keys.Alt | Keys.Shift | Keys.K, "Control+Alt+Shift+K");

Sprawdz(Keys.None, "");

Console.WriteLine("\n== WYNIK: {0} zgodnych, {1} niezgodnych ==", iZgodne, iNiezgodne);
Console.WriteLine(iNiezgodne == 0 ? "OK" : "SA NIEZGODNOSCI");
}
}
