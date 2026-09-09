// Lbc.cs -- portable Layout-By-Code dialog classes (LbcTextBox, LbcForm,
// HelpDialog, LbcDialog), shared source between EdSharp and DbDuo. Build it
// alongside the main source (csc ... EdSharp.cs Lbc.cs). Enhancements made in
// either project (e.g. addCheckListBox, the multi-select CheckedListBox adder)
// belong here so they port by copying this one file.
//
// PORTABILITY: the only external dependency is a Say class exposing
// say(...) / sayForced(...), which both EdSharp and DbDuo already provide.
// To move this file to DbDuo, change the single namespace line below to
// DbDuo's namespace; nothing else here is project-specific.

using Microsoft.VisualBasic.ApplicationServices;
using System.Windows.Automation.Provider;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows.Forms;
using Tektosyne.NetMail ;
using Tektosyne.Win32Api;

namespace Homer {

public class LbcTextBox : TextBox
{
    // Tip: focus tip spoken by Shift+F1; set by the Lbc adders.
    public string Tip = "";

    // lsLookupValues: opt-in F4 / Alt+DownArrow pick list. When an
    // editor assigns the valid values for this control's table+field
    // pair (from the builtin lookups table, e.g. maps.kind or
    // projects.kind), F4 opens an alphabetical pick list and replaces
    // the text with the chosen value -- the same pattern the multi-
    // field edit dialog offers, now available in any single Lbc text
    // line. Null or empty means no pick list for this control.
    public List<string> lsLookupValues = null;

    // ----- Text-control extra keys (HomerLbc_40 heritage) -----
    // Every Lbc text field gets this family of screen-reader
    // conveniences by being an LbcTextBox -- nothing is wired per
    // control, so a convenience added here reaches them all at once
    // (the modern replacement for HomerLbc's shared-handler + flag).
    // Copy takes the line WITHOUT its break (text payload); Cut and
    // Delete Line take the line WITH its break (removing the row),
    // then speak the line the caret lands on.
    //   Control+A        Select All          (ProcessCmdKey)
    //   Control+Shift+A  Unselect All        (ProcessCmdKey)
    //   Control+C        Copy: selection, else current line
    //   Alt+C            Copy Append: clipboard + selection-or-line
    //   Control+X        Cut: selection, else current line
    //   Alt+X            Cut Append: clipboard + what Cut removed
    //   Control+D        Delete Line (no clipboard)
    //   F8 / Shift+F8    Start / Complete Selection
    //   Control+F8       Copy All
    //   Alt+F8           Read All
    //   Alt+Y            Say Yield: line and character counts
    //   Alt+Apostrophe   Say Clipboard
    //   Shift+F1         Focus Tip
    // showLookupPick: open an alphabetical pick list of lsLookupValues
    // and, on OK, replace the text with the chosen value. Mirrors the
    // edit dialog's pickLookupValue so the F4 behavior is identical,
    // but is self-contained on the control so any single Lbc text line
    // can offer it just by assigning lsLookupValues.
    private void showLookupPick()
    {
        if (lsLookupValues == null || lsLookupValues.Count == 0) return;
        try
        {
            List<string> lsAlpha = new List<string>(lsLookupValues);
            lsAlpha.Sort(StringComparer.OrdinalIgnoreCase);   // pick list in alpha order
            using (LbcDialog dlgPick = new LbcDialog("Pick value", FindForm()))
            {
                ListBox lbPick = dlgPick.addListBox("Choose a value:", lsAlpha, Text);
                if (!string.Equals(dlgPick.runWithButtons(new string[] { "OK", "Cancel" }),
                        "OK", StringComparison.OrdinalIgnoreCase)) return;
                if (lbPick.SelectedItem == null) return;
                if (ReadOnly) { Say.say("Read-only"); return; }
                Text = lbPick.SelectedItem.ToString();
                SelectionStart = TextLength;
                SelectionLength = 0;
                Say.say(Text);
            }
        }
        catch (Exception exPick)
        { try { System.Diagnostics.Debug.WriteLine("showLookupPick failed: " + exPick.Message); } catch { } }
    }

    // Control+A / Control+Shift+A use ProcessCmdKey so they fire even
    // in multi-line and read-only boxes; the rest use OnKeyDown.
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.A))
        { SelectAll(); Say.say("Selected all"); return true; }
        if (keyData == (Keys.Control | Keys.Shift | Keys.A))
        { SelectionLength = 0; Say.say("Selection cleared"); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnKeyDown(KeyEventArgs evArgs)
    {
        base.OnKeyDown(evArgs);
        if (evArgs.Handled) return;
        int iLength;
        int iStart;
        string sLine;
        Keys key = evArgs.KeyData;

        if ((key == Keys.F4 || key == (Keys.Alt | Keys.Down))
            && lsLookupValues != null && lsLookupValues.Count > 0)
        {
            evArgs.Handled = true;
            evArgs.SuppressKeyPress = true;
            showLookupPick();
        }
        else if (key == (Keys.Control | Keys.C))
        {
            if (SelectionLength > 0) return;   // normal copy proceeds
            sLine = currentLineText();
            setClipboard(sLine);
            Say.say(sLine.Length == 0 ? "Copied blank line" : "Copied line");
        }
        else if (key == (Keys.Alt | Keys.C))
        {
            sLine = (SelectionLength > 0) ? SelectedText : currentLineText();
            setClipboard(clipboardJoin(sLine));
            Say.say("Appended to clipboard");
        }
        else if (key == (Keys.Control | Keys.X))
        {
            if (ReadOnly) { Say.say("Read-only"); return; }
            if (SelectionLength > 0) return;   // normal cut proceeds
            lineBounds(true, out iStart, out iLength);
            Select(iStart, iLength);
            Cut();
            SelectionStart = iStart;
            SelectionLength = 0;
            sayLanding();
        }
        else if (key == (Keys.Alt | Keys.X))
        {
            if (ReadOnly) { Say.say("Read-only"); return; }
            string sPrior = clipboardJoin("");
            if (SelectionLength == 0)
            {
                lineBounds(true, out iStart, out iLength);
                Select(iStart, iLength);
            }
            else iStart = SelectionStart;
            Cut();
            // Odczyt PO Cut() tez przez getClipboard: Cut sam dopiero co
            // pisal do schowka, wiec menedzer schowka jest w tej chwili
            // najbardziej zajety, a bez ponowien wycieta linia by zniknela.
            string sCut = getClipboard();
            setClipboard(sPrior + sCut);
            SelectionStart = iStart;
            SelectionLength = 0;
            sayLanding();
        }
        else if (key == (Keys.Control | Keys.D))
        {
            if (ReadOnly) { Say.say("Read-only"); return; }
            lineBounds(true, out iStart, out iLength);
            Text = Text.Remove(iStart, iLength);
            SelectionStart = Math.Min(iStart, TextLength);
            SelectionLength = 0;
            sayLanding();
        }
        else if (key == Keys.F8)
        {
            int iAnchor = SelectionStart + SelectionLength;
            Tag = iAnchor;
            Say.say("Start selection" + (iAnchor < TextLength ? " at " + Text[iAnchor] : ""));
        }
        else if (key == (Keys.Shift | Keys.F8))
        {
            if (!(Tag is int)) { Say.say("No selection start; press F8 first"); return; }
            int iAnchor2 = (int)Tag;
            int iCaret = SelectionStart + SelectionLength;
            Select(Math.Min(iAnchor2, iCaret), Math.Abs(iCaret - iAnchor2));
            Say.say(SelectionLength + (SelectionLength == 1 ? " character" : " characters"));
        }
        else if (key == (Keys.Control | Keys.F8))
        {
            setClipboard(Text);
            Say.say("Copied all");
        }
        else if (key == (Keys.Alt | Keys.F8))
        {
            Say.say(TextLength == 0 ? "Blank" : Text);
        }
        else if (key == (Keys.Alt | Keys.Y))
        {
            int iLines = Multiline ? Lines.Length : 1;
            Say.say(iLines + (iLines == 1 ? " line, " : " lines, ")
                + TextLength + (TextLength == 1 ? " character" : " characters"));
        }
        else if (key == (Keys.Alt | Keys.OemQuotes))
        {
            string sClip = getClipboard();
            Say.say(sClip.Length == 0 ? "Clipboard empty" : sClip);
        }
        else if (key == (Keys.Shift | Keys.F1))
        {
            Say.say(string.IsNullOrEmpty(Tip) ? "No tip for this field" : Tip);
        }
        // Homer structured-text line operations, active only in
        // multiline fields (notes, tags). Each acts on the selected
        // lines, or all lines when there is no selection. Keys are
        // the original Homer editor bindings. These mutate text, so
        // they no-op (with a spoken note) on read-only fields.
        else if (Multiline && key == (Keys.Alt | Keys.Shift | Keys.O))   applyLineOp(sortLines,    "Sorted");
        else if (Multiline && key == (Keys.Alt | Keys.Shift | Keys.Z))   applyLineOp(reverseLines, "Reversed");
        else if (Multiline && key == (Keys.Alt | Keys.Shift | Keys.K))   applyLineOp(uniqueLines,  "Kept unique,");
        else if (Multiline && key == (Keys.Alt | Keys.Shift | Keys.N))   applyLineOp(numberLines,  "Numbered");
        else if (Multiline && key == (Keys.Control | Keys.Shift | Keys.Enter)) applyLineOp(trimBlanks, "Trimmed");
        else return;

        evArgs.Handled = true;
        evArgs.SuppressKeyPress = true;
    }

    // applyLineOp: run a line transform over the selected lines, or
    // all lines when nothing is selected, replacing the text in
    // place (undoable) and speaking the resulting line count.
    private void applyLineOp(Func<string, string> op, string sVerb)
    {
        if (ReadOnly) { Say.say("Read-only"); return; }
        if (SelectionLength == 0) SelectAll();
        string sOut = op(SelectedText);
        SelectedText = sOut;
        int iLines = (sOut.Length == 0) ? 0 : sOut.Split('\n').Length;
        Say.say(sVerb + " " + iLines + (iLines == 1 ? " line" : " lines"));
    }

    // Line-transform helpers (pure). splitTextLines normalizes any
    // line-break flavor to a list; joinTextLines re-emits CRLF.
    private static List<string> splitTextLines(string sText)
    {
        List<string> lsLines = new List<string>();
        foreach (string sLine in sText.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n'))
            lsLines.Add(sLine);
        return lsLines;
    }
    private static string joinTextLines(List<string> lsLines)
    {
        return string.Join("\r\n", lsLines.ToArray());
    }
    private static string sortLines(string sText)
    {
        List<string> lsLines = splitTextLines(sText);
        lsLines.Sort(StringComparer.OrdinalIgnoreCase);
        return joinTextLines(lsLines);
    }
    private static string reverseLines(string sText)
    {
        List<string> lsLines = splitTextLines(sText);
        lsLines.Reverse();
        return joinTextLines(lsLines);
    }
    private static string uniqueLines(string sText)
    {
        List<string> lsOut = new List<string>();
        HashSet<string> hSeen = new HashSet<string>();
        foreach (string sLine in splitTextLines(sText))
            if (hSeen.Add(sLine)) lsOut.Add(sLine);
        return joinTextLines(lsOut);
    }
    private static string trimBlanks(string sText)
    {
        List<string> lsOut = new List<string>();
        int iBlankRun = 0;
        foreach (string sLine in splitTextLines(sText))
        {
            string sTrim = sLine.Trim();
            if (sTrim.Length == 0)
            {
                iBlankRun++;
                if (iBlankRun <= 2) lsOut.Add("");
            }
            else { iBlankRun = 0; lsOut.Add(sTrim); }
        }
        return joinTextLines(lsOut);
    }
    private static string numberLines(string sText)
    {
        List<string> lsOut = new List<string>();
        int iNum = 0;
        foreach (string sLine in splitTextLines(sText))
        {
            iNum++;
            lsOut.Add(iNum + ". " + sLine);
        }
        return joinTextLines(lsOut);
    }

    // lineBounds: start index and length of the caret's line.
    // bIncludeBreak picks the Cut/Delete flavor (line plus its
    // terminator) versus the Copy flavor (text only).
    private bool lineBounds(bool bIncludeBreak, out int iStart, out int iLength)
    {
        int iEnd;
        int iIndex = SelectionStart + SelectionLength;
        int iRow = GetLineFromCharIndex(iIndex);
        iStart = GetFirstCharIndexFromLine(iRow);
        iEnd = GetFirstCharIndexFromLine(iRow + 1);
        if (iEnd <= 0) iEnd = TextLength;
        else if (!bIncludeBreak) iEnd--;
        if (iStart < 0) iStart = 0;
        iLength = iEnd - iStart;
        if (iLength < 0) iLength = 0;
        return true;
    }

    private string currentLineText()
    {
        int iLength;
        int iStart;
        lineBounds(false, out iStart, out iLength);
        return Text.Substring(iStart, iLength).TrimEnd('\r', '\n');
    }

    private void sayLanding()
    {
        string sLine = currentLineText();
        Say.say(sLine.Length == 0 ? "Blank" : sLine);
    }

    // clipboardJoin / setClipboard: shared by the text conveniences
    // here and by the Lbc list-box copy keys; static so both call
    // them without duplicating the bodies.
    internal static string clipboardJoin(string sAddition)
    {
        string sText = getClipboard();
        if (sText.Length > 0 && !sText.EndsWith("\n")) sText += "\r\n";
        return sText + sAddition;
    }

    // ZAPIS DO SCHOWKA Z PONOWIENIAMI.  Kasperczak trzyma na maszynie menedzer
    // schowka (Ditto), a taki program otwiera schowek na chwile po KAZDEJ
    // zmianie - jednorazowy Clipboard.SetText trafia wtedy na zajety schowek,
    // rzuca wyjatek i kopiowanie po cichu nie dochodzi.  Ta sama petla, ktora
    // ma juz Util.SetClipboardData w EdSharp.cs: dziesiec prob po 40 ms, czyli
    // do 400 ms - typowa blokada menedzera schowka (mierzone 250 ms) mija w
    // tym czasie.  Wszystkie wygody tekstowe i klawisze kopiowania na listach
    // chodza TYLKO tedy, zeby zadna droga nie zostala bez ponowien.
    internal static bool setClipboard(string sText)
    {
        string sPut = (sText == null || sText.Length == 0) ? " " : sText;
        for (int iTry = 0; iTry < 10; iTry++)
        {
            try { Clipboard.SetText(sPut); return true; }
            catch { System.Threading.Thread.Sleep(40); }
        }
        return false;
    }

    // getClipboard: odczyt z tymi samymi ponowieniami.  Dopisywanie do schowka
    // (Alt+C, Alt+X) najpierw CZYTA to, co tam jest - a odczyt blokuje sie
    // dokladnie tak samo jak zapis.  Bez ponowien dopisanie do zajetego
    // schowka gubilo poprzednia zawartosc, bo odczyt oddawal pusty napis.
    internal static string getClipboard()
    {
        for (int iTry = 0; iTry < 10; iTry++)
        {
            try { return Clipboard.GetText() ?? ""; }
            catch { System.Threading.Thread.Sleep(40); }
        }
        return "";
    }
}

// =====================================================================
// LbcForm: shared base for every Lbc dialog. Adds Control+Enter as
// a universal "press OK" accelerator so the user can accept a dialog
// from anywhere in it without tabbing to the OK button -- notably in
// the sort-order dialog, where Enter on the Fields list does Add (not
// OK). The OK button is located by its DialogResult (preferred) or an
// "OK" label, deliberately NOT via the form's AcceptButton: some Lbc
// dialogs point AcceptButton at a different default (e.g. Add) while
// focus sits on a listbox, so AcceptButton is not a reliable handle
// for "OK". If no OK button is found, Control+Enter falls through
// untouched.
public class LbcForm : Form
{
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Enter))
        {
            Button btnOk = findButton(this, true);   // by DialogResult.OK
            if (btnOk == null) btnOk = findButton(this, false); // by "OK" label
            if (btnOk != null) { btnOk.PerformClick(); return true; }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private static Button findButton(Control parent, bool bByDialogResult)
    {
        foreach (Control c in parent.Controls)
        {
            Button btn = c as Button;
            if (btn != null)
            {
                if (bByDialogResult)
                {
                    if (btn.DialogResult == DialogResult.OK) return btn;
                }
                else if (btn.Text != null
                    && btn.Text.Replace("&", "").Trim().Equals("OK", StringComparison.OrdinalIgnoreCase))
                    return btn;
            }
            Button nested = findButton(c, bByDialogResult);
            if (nested != null) return nested;
        }
        return null;
    }
}

// =====================================================================
// LbcGrid: a DataGridView that lets the CALLER see every key FIRST.
//
// A grid has to hand keys back to its owner for two reasons that no
// event on a plain DataGridView answers reliably: the owner decides
// when the table GROWS (an arrow key at the last column or row), and
// the owner decides what CLOSES the dialog. ProcessCmdKey is the one
// place that sees a key before cell navigation, before the dialog
// keys (Escape, Enter) and before the form -- so an owner that hooks
// PreviewCmdKey never has to guess at WinForms key routing.
//
// Handled=true swallows the key completely; leaving it false keeps
// the grid's normal behaviour, so the hook is additive.
public class LbcGrid : DataGridView
{
    public event KeyEventHandler PreviewCmdKey;

    // Which way the user last moved. It decides the ORDER in which a cell
    // reports its position, because the useful coordinate is the one that
    // just CHANGED: moving sideways you want the column first, moving up
    // and down you want the row first. Sideways is the starting state, so
    // the very first cell announced reads like the rest of a row.
    public bool LastMoveWasVertical = false;

    // Was the current edit opened with F2, or started by typing? The two
    // behave differently on purpose, and this is the Excel rule the user
    // named himself: while TYPING, an arrow key confirms the cell and
    // moves on, whereas after F2 the same key moves the CARET inside the
    // text being corrected. Without the distinction, typing a value and
    // pressing Left Arrow buries the next word inside the current cell.
    public bool EditStartedByF2 = false;
    private bool bF2Pending = false;

    public LbcGrid()
    {
        this.CellBeginEdit += delegate(object o, DataGridViewCellCancelEventArgs e)
        {
            EditStartedByF2 = bF2Pending;
            bF2Pending = false;
        };
        this.CellEndEdit += delegate(object o, DataGridViewCellEventArgs e)
        {
            EditStartedByF2 = false;
        };
        // MOWA PO F2 - zgloszenie Kasperczaka 29.08.2026 01:59, doslownie:
        // "jak nacisne F2, to on mi wystarczy jak powie pole edycyjne i
        // ewentualnie wspolrzedne komorki wiersza.  Nie musi mowic jakiegos
        // edit, edytowano albo cos takiego."
        //
        // ZMIERZONE NA ZYWYM NVDA PRZED ZMIANA (zmierz_mowe_f2_w_siatce.py):
        // F2 dawalo doslownie "Formant edytowania | pole edycji | abc".
        // Slowo "Formant edytowania" to NAZWA kontrolki edycyjnej, ktora
        // WinForms tworzy w komorce - czytnik wymawia ja jako pierwsza, przed
        // rodzajem pola.  Nie da sie tego zobaczyc w kodzie: nazwa nie jest
        // nigdzie u nas ustawiana, bierze sie z domyslnego typu kontrolki.
        //
        // NAPRAWA: przy kazdym wejsciu w edycje podstawiamy WLASNA nazwe -
        // wspolrzedne komorki.  Wtedy czytnik mowi wspolrzedne i rodzaj pola
        // ("pole edycji"), czyli dokladnie to, o co prosil, a slowo o
        // edytowaniu znika, bo przestaje byc nazwa czegokolwiek.
        // Rodzaju pola NIE ruszamy: to jedyna informacja, ktora mowi
        // niewidomemu, ze moze teraz pisac.
        this.EditingControlShowing += delegate(object o, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control == null) return;
            DataGridViewCell cell = this.CurrentCell;
            string sWhere = "";
            if (cell != null)
            {
                int iRow = cell.RowIndex;
                int iCol = cell.ColumnIndex + 1;
                sWhere = ((iRow == 0) ? "header row" : ("row " + iRow))
                    + ", column " + iCol;
            }
            e.Control.AccessibleName = sWhere;
            // Opis musi byc PUSTY, nie niedotkniety: czytnik czyta go po
            // nazwie, a domyslny opis powtarzalby to samo innymi slowami.
            e.Control.AccessibleDescription = "";
        };
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        Keys keyCode = keyData & Keys.KeyCode;
        if (keyCode == Keys.F2) bF2Pending = true;
        if (keyCode == Keys.Up || keyCode == Keys.Down
            || keyCode == Keys.PageUp || keyCode == Keys.PageDown) LastMoveWasVertical = true;
        else if (keyCode == Keys.Left || keyCode == Keys.Right
            || keyCode == Keys.Home || keyCode == Keys.End
            || keyCode == Keys.Tab) LastMoveWasVertical = false;

        KeyEventHandler handler = PreviewCmdKey;
        if (handler != null)
        {
            KeyEventArgs ev = new KeyEventArgs(keyData);
            handler(this, ev);
            if (ev.Handled) return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }
}

// =====================================================================
// LbcGridRow: wiersz siatki, ktory MILCZY, zeby glos mial tylko komorka.
//
// ZGLOSZENIE, ktore to wymusilo (Kasperczak 28.08.2026): "czy on dalej to liczy
// od wiersza zero? Bo to tak chyba nie powinno byc, ze wiersz zero."
// Mial racje.  Domyslny wiersz DataGridView oglasza sie czytnikowi PRZED
// komorka, a mowi doslownie "Wiersz 0" - numer liczony od zera, ktory nie
// odpowiada zadnej tabeli, jaka czlowiek widzial.  Do tego komorka podaje numer
// wiersza po raz drugi (i poprawnie, od jedynki), wiec jeden ruch strzalka
// oznaczal dwa glosy i dwie sprzeczne numeracje.
//
// PIERWSZA PROBA (5.0.37) BYLA ZLA I ZOSTAWILA TU OSTRZEZENIE, ze tego "NIE
// UZYWAC": wlasne Clone() budowalo wiersz OD ZERA, czyli BEZ KOMOREK, i okno
// kreatora wywalalo sie na "Unexpected Event".  Zmierzone teraz w harnessie:
// ArgumentOutOfRangeException z kolekcji komorek, i to od razu przy wypelnianiu.
// POPRAWKA JEST JEDNOLINIJKOWA: Clone MUSI wolac base.Clone().
// DataGridViewRow.Clone tworzy instancje NASZEGO typu przez Activator i sam
// kopiuje komorki, wiec nadpisywanie go recznie bylo bledem od poczatku.
//
// Wyciszamy Name, Value, Description ORAZ podmieniamy Role: przy samym Name
// czytnik nadal mowil slowo "wiersz" (czyli role obiektu).  Zmierzone na zywym
// NVDA: bez tego 9 wypowiedzi na cztery ruchy, z tym 5 - dokladnie po jednej na
// ruch, kazda z trescia komorki.
//
// DOWOD: testy/narzedzia_nvda/zmierz_warianty_naglowka_wiersza.py.  Cztery
// warianty na zywym czytniku, z KONTROLA POZYTYWNA (wariant bez zmiany MUSI
// odtworzyc "Wiersz 0" - i odtworzyl).  Ukrycie naglowkow wierszy
// (RowHeadersVisible = false) NIE POMAGA: obiekt wiersza mowi dalej.
// Build tu nic nie dowodzi - poprzednia, wadliwa wersja tez sie kompilowala.
public class LbcGridRow : DataGridViewRow
{
    // MUSI byc base.Clone() - patrz komentarz wyzej.
    public override object Clone()
    {
        return base.Clone();
    }

    protected override AccessibleObject CreateAccessibilityInstance()
    {
        return new LbcGridRowAccessibleObject(this);
    }

    private class LbcGridRowAccessibleObject : DataGridViewRowAccessibleObject
    {
        public LbcGridRowAccessibleObject(LbcGridRow row) : base(row) {}

        public override string Name {get {return "";}}
        public override string Value {get {return "";} set {}}
        public override string Description {get {return "";}}
        // Pane, nie Row: przy roli wiersza czytnik oglasza samo slowo "wiersz",
        // choc nazwa jest pusta.  Zmierzone na zywym NVDA.
        public override AccessibleRole Role {get {return AccessibleRole.Pane;}}
    }
}

// =====================================================================
// LbcGridCell: a grid cell that tells the screen reader its CONTENT
// FIRST and its position after it.
//
// WHY THIS CLASS EXISTS AT ALL. A plain DataGridView cell reports
// itself to a reader as "Column 1 Row 0" -- measured on live NVDA, that
// is literally what a blind user hears while walking the grid: the
// coordinates, and no content. Two things are wrong with it. The user
// moves around a table to read what is IN it, so the content is the
// answer and the position is only context; and the row count starts at
// zero, which matches no table anyone has ever read.
//
// A reader speaks the accessible NAME of the focused cell, so putting
// the content at the front of that name fixes the order with ONE voice.
// Saying it ourselves as well would give two voices for one movement.
//
// The position order follows the DIRECTION of the last move, because the
// coordinate that just changed is the one worth hearing first: sideways
// gives "column 2, row 1", vertically "row 1, column 2".
//
// An EMPTY cell says "blank" rather than falling silent, since silence
// is indistinguishable from a key that did nothing.
//
// JEDEN RUCH, JEDEN GLOS - i to NIE jest kosmetyka.  Uzytkownik zglosil
// (29.08.2026), ze na jedno nacisniecie strzalki slyszy trzy rzeczy:
//   "gh, column 1, row 2 | gh | zaznaczony"
// czyli nasza nazwe, potem tresc komorki DRUGI RAZ, a na koncu stan.  Na
// pustej komorce zamiast powtorki leci "(zerowy)".  Przy chodzeniu po tabeli
// to potraja czas kazdego ruchu i topi tresc w szumie.
// ZMIERZONE NA ZYWYM NVDA, warianty obok siebie (h2.cs, cztery uruchomienia):
//   - powtorka tresci to WARTOSC obiektu dostepnosciowego (Value).  Nazwa i
//     wartosc sa czytane obie, a u nas tresc juz jest w nazwie.  "(zerowy)"
//     to ta sama wartosc dla pustej komorki.
//   - "zaznaczony" NIE bierze sie z samego zaznaczenia: zdjecie stanu
//     Selected dalo GORSZY wynik, bo czytnik zaczal mowic "niezaznaczony".
//     Znika dopiero, gdy komorka przestaje deklarowac ZAZNACZALNOSC
//     (Selectable) - a w siatce jednokomorkowego wyboru zaznaczenie i tak
//     znaczy dokladnie to samo, co "tu jest kursor", wiec nie niesie
//     zadnej informacji.
// LEKCJA OGOLNA: nadpisujac nazwe obiektu dostepnosciowego, sprawdz TAKZE
// jego wartosc, opis i stan - czytnik sklada wypowiedz z nich wszystkich.
public class LbcGridCell : DataGridViewTextBoxCell
{
    protected override AccessibleObject CreateAccessibilityInstance()
    {
        return new LbcGridCellAccessibleObject(this);
    }

    public override object Clone()
    {
        return new LbcGridCell();
    }

    // Header row and 1-based rows: the first row of the grid IS the
    // table header (Markdown requires one), and body rows are numbered
    // from one so the spoken number matches the finished table.
    public static string DescribeCell(DataGridViewCell cell, bool bVertical)
    {
        if (cell == null) return "";
        string sText = (cell.Value == null) ? "" : cell.Value.ToString();
        sText = sText.Trim();
        if (sText.Length == 0) sText = "blank";
        int iRow = cell.RowIndex;
        int iCol = cell.ColumnIndex + 1;
        string sRow = (iRow == 0) ? "header row" : ("row " + iRow);
        string sCol = "column " + iCol;
        if (bVertical) return sText + ", " + sRow + ", " + sCol;
        return sText + ", " + sCol + ", " + sRow;
    }

    private class LbcGridCellAccessibleObject : DataGridViewCellAccessibleObject
    {
        private LbcGridCell cell;

        // KRYTYCZNE: dziedziczymy po DOMYSLNYM obiekcie komorki i nadpisujemy
        // TYLKO nazwe.  Pierwsza wersja budowala AccessibleObject od zera i
        // czytnik przestal ogloszac komorke w ogole (zmierzone na zywym NVDA:
        // rola fokusu spadla z 29 na 28, a mowa podawala nazwe calej siatki).
        // Granice, rodzic, stan i relacje w tabeli musza zostac te, ktore daje
        // WinForms - my zmieniamy wylacznie KOLEJNOSC tego, co slychac.
        public LbcGridCellAccessibleObject(LbcGridCell c) : base(c) {cell = c;}

        public override string Name
        {
            get
            {
                LbcGrid grid = (cell == null) ? null : cell.DataGridView as LbcGrid;
                bool bVertical = (grid != null) && grid.LastMoveWasVertical;
                return DescribeCell(cell, bVertical);
            }
        }

        // Tresc siedzi JUZ w nazwie, wiec wartosc musi zamilknac - inaczej
        // czytnik mowi ja drugi raz, a przy pustej komorce dorzuca "(zerowy)".
        public override string Value
        {
            get {return "";}
            set {}
        }

        public override string Description {get {return "";}}

        // Bez zdjecia ZAZNACZALNOSCI kazdy ruch konczy sie slowem "zaznaczony"
        // albo "niezaznaczony".  W siatce, w ktorej biezaca jest zawsze
        // dokladnie jedna komorka, ta informacja powtarza to, co uzytkownik i
        // tak wie: ze wlasnie tu stoi.
        public override AccessibleStates State
        {
            get
            {
                return base.State & ~(AccessibleStates.Selected | AccessibleStates.Selectable);
            }
        }
    }
}

// =====================================================================
public static class HelpDialog
{
    public static void show(IWin32Window owner, string sTitle, string sText)
    {
        using (Form dlg = new LbcForm())
        {
            dlg.Text = sTitle;
            dlg.AccessibleName = sTitle;
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.ClientSize = new Size(720, 540);
            dlg.MinimumSize = new Size(400, 300);
            dlg.FormBorderStyle = FormBorderStyle.Sizable;
            dlg.MaximizeBox = true;
            dlg.MinimizeBox = false;
            dlg.ShowInTaskbar = false;
            dlg.KeyPreview = true;

            LbcTextBox tb = new LbcTextBox();
            tb.Multiline = true;
            tb.ReadOnly = true;
            tb.ScrollBars = ScrollBars.Vertical;
            tb.WordWrap = true;
            tb.Font = new Font(FontFamily.GenericMonospace, 10f);
            tb.Text = sText;
            tb.AccessibleName = "Help text";
            tb.TabIndex = 0;
            tb.SelectionStart = 0;
            tb.SelectionLength = 0;
            tb.Size = new Size(dlg.ClientSize.Width - 20, dlg.ClientSize.Height - 50);
            tb.Location = new Point(10, 10);
            tb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            Button btnClose = new Button();
            btnClose.Text = "&OK";
            btnClose.AccessibleName = "OK";
            btnClose.DialogResult = DialogResult.OK;
            btnClose.Size = new Size(90, 28);
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClose.Location = new Point(dlg.ClientSize.Width - 100, dlg.ClientSize.Height - 38);
            btnClose.UseVisualStyleBackColor = true;
            btnClose.TabIndex = 1;

            dlg.Controls.Add(tb);
            dlg.Controls.Add(btnClose);
            dlg.AcceptButton = btnClose;
            dlg.CancelButton = btnClose;
            dlg.ActiveControl = tb;

            dlg.ShowDialog(owner);
        }
    }

    // showRecordView: the Show view's read-only record display
    // with view-cycle and record-navigation keys. Returns a
    // verdict the caller's loop acts on:
    //   0  closed (Escape / OK)            -> list
    //   1  F6 (forward in the cycle)       -> list (show is last)
    //   2  Shift+F6 (back in the cycle)    -> form
    //   3  Control+PageDown                -> next record
    //   4  Control+PageUp                  -> previous record
    // Separate from show() so help text and query results keep
    // their plain read-only dialog without these keys.
    public static int showRecordView(IWin32Window owner, string sTitle, string sText)
    {
        int iVerdict = 0;
        using (Form dlg = new LbcForm())
        {
            dlg.Text = sTitle;
            dlg.AccessibleName = sTitle;
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.ClientSize = new Size(720, 540);
            dlg.MinimumSize = new Size(400, 300);
            dlg.FormBorderStyle = FormBorderStyle.Sizable;
            dlg.MaximizeBox = true; dlg.MinimizeBox = false;
            dlg.ShowInTaskbar = false; dlg.KeyPreview = true;

            TextBox tb = new TextBox();
            tb.Multiline = true; tb.ReadOnly = true;
            tb.ScrollBars = ScrollBars.Vertical; tb.WordWrap = true;
            tb.Font = new Font(FontFamily.GenericMonospace, 10f);
            tb.Text = sText; tb.AccessibleName = "Record";
            tb.TabIndex = 0; tb.SelectionStart = 0; tb.SelectionLength = 0;
            tb.Size = new Size(dlg.ClientSize.Width - 20, dlg.ClientSize.Height - 50);
            tb.Location = new Point(10, 10);
            tb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            Button btnClose = new Button();
            btnClose.Text = "&OK"; btnClose.AccessibleName = "OK";
            btnClose.DialogResult = DialogResult.OK;
            btnClose.Size = new Size(90, 28);
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClose.Location = new Point(dlg.ClientSize.Width - 100, dlg.ClientSize.Height - 38);
            btnClose.UseVisualStyleBackColor = true; btnClose.TabIndex = 1;

            dlg.KeyDown += delegate(object o, KeyEventArgs ev)
            {
                int v = 0;
                if (ev.KeyData == Keys.F6) v = 1;
                else if (ev.KeyData == (Keys.Shift | Keys.F6)) v = 2;
                else if (ev.KeyData == (Keys.Control | Keys.PageDown)) v = 3;
                else if (ev.KeyData == (Keys.Control | Keys.PageUp)) v = 4;
                else return;
                iVerdict = v;
                ev.Handled = true; ev.SuppressKeyPress = true;
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };

            dlg.Controls.Add(tb);
            dlg.Controls.Add(btnClose);
            dlg.AcceptButton = btnClose;
            dlg.CancelButton = btnClose;
            dlg.ActiveControl = tb;
            dlg.ShowDialog(owner);
        }
        return iVerdict;
    }
}

// =====================================================================
// LbcDialog: "Layout by Code" dialog builder.
//
// A reusable WinForms dialog that callers build by adding one
// control at a time. Each add* method appends a labeled row
// to a vertical stack, returns the inner editing control, and
// wires its accessible name from the label so screen readers
// announce the field correctly when the user tabs into it.
//
// After adding all controls, the caller invokes runOkCancel()
// which appends an OK/Cancel button band, shows the dialog
// modally, and returns true on OK. The caller then reads the
// final values directly from the control references it kept.
//
// Borrowed pattern from Jamal Mazrui's Layout by Code system
// (LbC), which exists in C#, Python, AutoIt, and JScript .NET
// versions and codifies the practice of laying out screen-
// reader-friendly dialogs by composing simple "add this
// control" calls in sequence rather than using a designer file.
//
// Features carried over from HomerLbc (the mature JScript .NET
// implementation):
//   - Focus tips: each add* method takes an optional tip string;
//     when the user tabs into the control, the tip appears in
//     a status bar at the bottom of the dialog. Screen readers
//     announce status-bar changes, so the tip is read aloud
//     without forcing a popup. Tips replace tooltip popups,
//     which JAWS often suppresses.
//   - Name-based widget lookup: every control is registered in
//     a Widgets dictionary keyed by an auto-generated name
//     (<Kind>_<CleanedLabel>) so callers can retrieve controls
//     by string name as well as by reference. Useful for
//     handlers that get the sender but not the original ref,
//     and for generic walkers (import/export, validation).
//   - Memo-vs-AcceptButton coordination: while focus is on a
//     multi-line TextBox (added via addTextMemo / addMemoBox),
//     the form's AcceptButton is temporarily cleared so Enter
//     inserts a newline instead of submitting; on LostFocus
//     the AcceptButton is restored. This is the Homer LbC
//     convention -- it lets memo editing feel natural while
//     still letting Enter submit from single-line fields.
//
// Conventions:
//   - Each labeled control gets a Label above it (not beside).
//     Uniform vertical rhythm, label-then-control reading order.
//   - The label text passes through unchanged (caller may
//     include '&' for mnemonic letters); the control's
//     AccessibleName strips '&' and trailing ':'.
//   - TabIndex is assigned in add order. WinForms Labels are
//     non-focusable and naturally skipped during tab traversal.
//   - The dialog auto-sizes its height to its contents up to
//     a cap; AutoScroll engages above the cap.
//   - Two naming patterns coexist: bare-control adders (addLabel,
//     addTextBox, addCheckBox) and labeled-control adders that
//     mirror Homer LbC (addInputBox, addMemoBox, addPickBox,
//     addComboPickBox). The labeled variants are aliases that
//     emit a Label first and then call the bare adder.
//
// Typical usage:
//
//   LbcDialog dlg = new LbcDialog("Configuration", this);
//   TextBox   tbMode = dlg.addInputBox("UI mode", "both", "How EdSharp launches");
//   CheckBox  cbBeep = dlg.addCheckBox("Beep on errors", true, "Audible cue on failure");
//   TextBox   tbNote = dlg.addMemoBox("Startup note", "", "Free-form text shown at launch");
//   if (dlg.runOkCancel())
//   {
//       string sMode = tbMode.Text;
//       bool   bBeep = cbBeep.Checked;
//       string sNote = tbNote.Text;
//       // ... persist or apply
//   }
//
//   // Or look up by name later:
//   TextBox tbAgain = dlg.getTextBox("TextBox_UI_mode");
// =====================================================================
public class LbcDialog : IDisposable
{
    // Layout constants, all alphabetical. Sized for screen-reader
    // users who often run at 125-150% display scaling -- generous
    // padding makes the dialog readable without crowding labels.
    private const int DefaultButtonHeight = 28;
    private const int DefaultButtonWidth  = 90;
    private const int DefaultDialogWidth  = 520;
    private const int DefaultLabelHeight  = 18;
    private const int DefaultLineHeight   = 24;
    private const int DefaultListHeight   = 100;
    private const int DefaultMaxHeight    = 600;
    private const int DefaultMemoHeight   = 96;
    private const int DefaultNumericWidth = 100;
    private const int DefaultPadding      = 12;
    private const int DefaultRowGap       = 6;
    private const int DefaultStatusHeight = 22;
    private const int DefaultTreeHeight   = 260;
    private const int DefaultGridHeight   = 260;

    // Layout state. Built up by add* calls, consumed by runX.
    // Alphabetical declarations.
    private Dictionary<Control, string> dFocusTips;
    // Long per-control key lists. The status-bar tip must stay SHORT
    // because a screen reader speaks the status bar while announcing
    // the freshly opened dialog -- a long tip is then read out before
    // the focused list item, which is exactly backwards for the user
    // who only wants to hear the file name. So a control may carry a
    // short tip here and register its full key list as a help detail,
    // surfaced on demand by the Help button / F1.
    private Dictionary<Control, string> dHelpDetails;
    private Dictionary<string, int>     dNameCounts;
    private Dictionary<string, Control> dWidgets;
    private Control                     ctlFirstFocusable;
    private Form                        frm;
    private IWin32Window                owner;
    private Button                      btnSavedAccept;
    private FlowLayoutPanel             pnlStack;
    private Label                       lblStatusBar;
    private int                         iTabIndex;
    // Most-recent case-insensitive substring searched via Ctrl+J
    // inside a pick-list. F3 / Shift+F3 advance / retreat through
    // matches. Shared across all list boxes in this dialog so the
    // user can chain searches across multiple lists.
    private string                      sListSearchTerm = "";
    // Access-key letters already claimed in THIS dialog, upper-cased.
    // Two controls answering one Alt+letter is worse than a control
    // with none: the measurement (harness of our own panel layout)
    // showed the focus landing on whichever control Windows reaches
    // first, and it was NOT the one whose label was pressed. So every
    // letter handed out is recorded here, and a clash moves to the
    // label's next unused word initial. Buttons and field labels draw
    // from the SAME set, because Alt+letter does not care which of
    // the two a letter came from.
    private string                      sAccessKeysTaken = "";
    // Controls still waiting for a trigger letter, with the label text
    // to put it in. The letters are handed out only when the dialog is
    // about to be shown, NOT when a field is added, because the button
    // row is built last and its letters must come FIRST: a button
    // whose letter the caller chose deliberately ("&Replace" in the
    // spelling dialog) cannot lose it to a field that happened to be
    // added earlier and took R automatically.
    private List<Control>               lsAccessKeyControls;
    private List<string>                lsAccessKeyLabels;

    public LbcDialog(string sTitle, IWin32Window ownerWindow)
    {
        owner = ownerWindow;
        frm = new LbcForm();
        frm.Text = sTitle ?? "";
        frm.AccessibleName = sTitle ?? "";
        frm.StartPosition = FormStartPosition.CenterParent;
        frm.FormBorderStyle = FormBorderStyle.Sizable;
        frm.MaximizeBox = false;
        frm.MinimizeBox = false;
        frm.ShowInTaskbar = false;
        frm.KeyPreview = true;
        // EdSharp-style text-edit hotkeys: route every keystroke
        // through onFormKeyDown so we can intercept Ctrl+C/X,
        // Alt+C/X/F8, F8/Shift+F8, Ctrl+F8, and Ctrl+D when the
        // focused control is a TextBox or memo. The handler is
        // a no-op for other controls and for keystrokes that
        // don't match the EdSharp hotkey set. Master enable flag
        // [Lbc] extraKeys in DbDo.inix, defaults Y.
        frm.KeyDown += new KeyEventHandler(onFormKeyDown);
        frm.MinimumSize = new Size(360, 200);
        frm.ClientSize = new Size(DefaultDialogWidth, 200);

        // Status bar at the bottom of the form. Updates as the
        // user tabs through controls -- each control's focus
        // tip (set via the optional sTip argument on add*
        // methods) appears here when the control receives focus.
        // JAWS, NVDA, and Narrator pick up status-bar text via
        // UIA live-region semantics; we set AccessibleRole to
        // StatusBar so the announcement is appropriately routed.
        //
        // Added BEFORE the stack so its Dock=Bottom claims the
        // bottom area. The button row (added later in run*) is
        // also Dock=Bottom and pushes the status bar up by its
        // own height.
        lblStatusBar = new Label();
        lblStatusBar.Text = "";
        lblStatusBar.AccessibleRole = AccessibleRole.StatusBar;
        lblStatusBar.AccessibleName = "Status";
        lblStatusBar.Dock = DockStyle.Bottom;
        lblStatusBar.Height = DefaultStatusHeight;
        lblStatusBar.TextAlign = ContentAlignment.MiddleLeft;
        lblStatusBar.Padding = new Padding(DefaultPadding, 2, DefaultPadding, 2);
        lblStatusBar.BorderStyle = BorderStyle.Fixed3D;
        frm.Controls.Add(lblStatusBar);

        // Vertical FlowLayoutPanel as the form's main container.
        // AutoScroll on so dialogs with many fields scroll instead
        // of overflowing the screen.
        pnlStack = new FlowLayoutPanel();
        pnlStack.FlowDirection = FlowDirection.TopDown;
        pnlStack.WrapContents = false;
        pnlStack.AutoScroll = true;
        pnlStack.Dock = DockStyle.Fill;
        pnlStack.Padding = new Padding(DefaultPadding);
        frm.Controls.Add(pnlStack);

        dFocusTips = new Dictionary<Control, string>();
        dHelpDetails = new Dictionary<Control, string>();
        dNameCounts = new Dictionary<string, int>();
        dWidgets = new Dictionary<string, Control>(StringComparer.OrdinalIgnoreCase);
        iTabIndex = 0;
        ctlFirstFocusable = null;
        btnSavedAccept = null;
    }

    // ------- Add helpers -------
    //
    // Each adds one control (with optional label and tip) to the
    // vertical stack and returns the control so the caller can
    // stash a reference, attach event handlers, or query the
    // final value after dismissal.
    //
    // Naming pattern (mirrors Homer LbC):
    //   addX        -- bare control, no separate label above it
    //                  (X carries its own label, like CheckBox,
    //                  or no label is wanted)
    //   addXBox     -- labeled control: a Label is added first,
    //                  then the bare X (the convention from
    //                  Homer Lbc where "Box" suffix means
    //                  "with a label above")
    //
    // The Tip parameter, when given, is shown in the status bar
    // when the control receives focus.

    // addLabel: standalone explanatory text. Not focusable, so
    // tip is not applicable.
    public Label addLabel(string sText)
    {
        Label lbl = new Label();
        lbl.Text = sText ?? "";
        lbl.AutoSize = false;
        lbl.Size = new Size(innerWidth(), DefaultLabelHeight);
        lbl.Margin = new Padding(0, 0, 0, DefaultRowGap);
        lbl.TextAlign = ContentAlignment.MiddleLeft;
        registerWidget(lbl, "Label", sText);
        pnlStack.Controls.Add(lbl);
        return lbl;
    }

    // addTextBox: bare single-line text input (no separate
    // Label above). Returns the TextBox.

    public TextBox addTextBox(string sValue, string sTip)
    {
        LbcTextBox tb = new LbcTextBox();
        tb.Text = sValue ?? "";
        tb.Size = new Size(innerWidth(), DefaultLineHeight);
        tb.TabIndex = iTabIndex++;
        tb.Margin = new Padding(0, 0, 0, DefaultRowGap);
        // Inherit the AccessibleName from the most recent Label,
        // if one was just added. Mirrors Homer LbC.
        Label lblLast = currentLabelOrNull();
        if (lblLast != null) tb.AccessibleName = lblLast.AccessibleName;
        tb.GotFocus += handleGotFocus;
        registerWidget(tb, "TextBox", tb.AccessibleName);
        if (!string.IsNullOrEmpty(sTip)) { dFocusTips[tb] = sTip; tb.Tip = sTip; }
        pnlStack.Controls.Add(tb);
        if (ctlFirstFocusable == null) ctlFirstFocusable = tb;
        return tb;
    }

    // addInlineInputBox: labeled single-line text input where
    // the label and the text box share one row. Layout:
    //
    //   [Label:        ] [____textbox____________________]
    //
    // Used by the record edit dialog (New-Record / Set-Record)
    // for single-line fields, per the user-guide layout rule:
    // single-line fields put "Label: " inline with the textbox,
    // while multi-line fields put the label on the row above
    // the (taller) memo box. Inline placement gives the screen
    // reader a tight, scannable layout: Tab moves to the field
    // and JAWS announces "Label: <value>" without the user
    // hearing the bare label as a separate Tab stop.
    //
    // Implementation: a TableLayoutPanel with one row, two
    // columns. The label column is auto-sized to fit its text;
    // the textbox column fills the remaining width. The whole
    // panel is added as a single row to the outer FlowLayoutPanel.
    public TextBox addInlineInputBox(string sLabel, string sValue, string sTip)
    {
        TableLayoutPanel pnlRow = new TableLayoutPanel();
        pnlRow.ColumnCount = 2;
        pnlRow.RowCount = 1;
        pnlRow.AutoSize = true;
        pnlRow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        pnlRow.Margin = new Padding(0, 0, 0, DefaultRowGap);
        pnlRow.ColumnStyles.Clear();
        pnlRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pnlRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        pnlRow.Width = innerWidth();

        Label lbl = new Label();
        lbl.Text = (sLabel ?? "").TrimEnd();
        if (!lbl.Text.EndsWith(":")) lbl.Text = lbl.Text + ":";
        lbl.UseMnemonic = true;
        // Same trigger-letter rule as addFieldLabel. Measured
        // separately for this layout, because here the label and the
        // field sit in a TableLayoutPanel of their own rather than in
        // the dialog's main stack: the mnemonic reaches the field in
        // the same row and does NOT leak to the field after it.
        queueAccessKey(lbl, (sLabel ?? "").TrimEnd());
        lbl.AccessibleName = cleanLabel(sLabel);
        lbl.AutoSize = true;
        lbl.TextAlign = ContentAlignment.MiddleLeft;
        lbl.Margin = new Padding(0, 4, DefaultPadding, 0);
        pnlRow.Controls.Add(lbl, 0, 0);

        LbcTextBox tb = new LbcTextBox();
        tb.Text = sValue ?? "";
        tb.Dock = DockStyle.Fill;
        tb.TabIndex = iTabIndex++;
        tb.Margin = new Padding(0, 0, 0, 0);
        tb.AccessibleName = cleanLabel(sLabel);
        tb.GotFocus += handleGotFocus;
        registerWidget(tb, "TextBox", tb.AccessibleName);
        if (!string.IsNullOrEmpty(sTip)) { dFocusTips[tb] = sTip; tb.Tip = sTip; }
        pnlRow.Controls.Add(tb, 1, 0);

        pnlStack.Controls.Add(pnlRow);
        if (ctlFirstFocusable == null) ctlFirstFocusable = tb;
        return tb;
    }

    // addInputBox: labeled single-line text input. Adds a Label
    // first, then a TextBox whose AccessibleName comes from the
    // Label. Equivalent to AddInputBox in Homer LbC.
    //
    // Note: for the record edit dialog, callers should prefer
    // addInlineInputBox (label and textbox on one row). This
    // older addInputBox method (label above textbox) is still
    // used by some dialogs where vertical rhythm matters.
    public TextBox addInputBox(string sLabel, string sValue, string sTip)
    {
        addFieldLabel(sLabel);
        TextBox tb = addTextBox(sValue, sTip);
        // The label inheritance happened in addTextBox via
        // currentLabelOrNull. Set AccessibleName explicitly
        // here too as belt-and-suspenders.
        tb.AccessibleName = cleanLabel(sLabel);
        return tb;
    }

    // 2-arg overload (no tip). Convenient for dialogs where the
    // label is self-explanatory and no extra tooltip is needed.
    public TextBox addInputBox(string sLabel, string sValue)
    {
        return addInputBox(sLabel, sValue, null);
    }

    // addTextLine: alias for addInputBox without a tip. Kept
    // for backward compatibility with the earlier API.
    public TextBox addTextLine(string sLabel, string sValue)
    {
        return addInputBox(sLabel, sValue, null);
    }

    // addMemo: bare multi-line text input. Coordinates with
    // AcceptButton so Enter inside the memo inserts a newline
    // instead of submitting the dialog.
    public TextBox addMemo(string sValue, string sTip)
    {
        LbcTextBox tb = new LbcTextBox();
        tb.Text = sValue ?? "";
        tb.Multiline = true;
        tb.AcceptsReturn = true;
        tb.AcceptsTab = false;
        tb.ScrollBars = ScrollBars.Vertical;
        tb.WordWrap = true;
        tb.Size = new Size(innerWidth(), DefaultMemoHeight);
        tb.TabIndex = iTabIndex++;
        tb.Margin = new Padding(0, 0, 0, DefaultRowGap);
        Label lblLast = currentLabelOrNull();
        if (lblLast != null) tb.AccessibleName = lblLast.AccessibleName;
        tb.GotFocus += handleMemoGotFocus;
        tb.LostFocus += handleMemoLostFocus;
        registerWidget(tb, "Memo", tb.AccessibleName);
        if (!string.IsNullOrEmpty(sTip)) { dFocusTips[tb] = sTip; tb.Tip = sTip; }
        pnlStack.Controls.Add(tb);
        if (ctlFirstFocusable == null) ctlFirstFocusable = tb;
        return tb;
    }

    // addMemoBox: labeled multi-line text input. Equivalent to
    // AddMemoBox in Homer LbC.
    public TextBox addMemoBox(string sLabel, string sValue, string sTip)
    {
        addFieldLabel(sLabel);
        TextBox tb = addMemo(sValue, sTip);
        tb.AccessibleName = cleanLabel(sLabel);
        return tb;
    }

    // addTextMemo: alias for addMemoBox without a tip. Kept for
    // backward compatibility.
    public TextBox addTextMemo(string sLabel, string sValue)
    {
        return addMemoBox(sLabel, sValue, null);
    }

    // addCheckBox: boolean toggle. The label is part of the
    // checkbox itself (WinForms convention).
    public CheckBox addCheckBox(string sLabel, bool bValue, string sTip)
    {
        CheckBox cb = new CheckBox();
        cb.Text = sLabel ?? "";
        cb.UseMnemonic = true;
        // A check box carries its own caption, so the trigger letter
        // goes straight onto the control: Alt+letter both focuses and
        // TOGGLES it, which is the Windows behaviour users expect.
        queueAccessKey(cb, sLabel ?? "");
        cb.AccessibleName = cleanLabel(sLabel);
        cb.Checked = bValue;
        cb.AutoSize = false;
        cb.Size = new Size(innerWidth(), DefaultLineHeight);
        cb.TabIndex = iTabIndex++;
        cb.Margin = new Padding(0, 0, 0, DefaultRowGap);
        cb.GotFocus += handleGotFocus;
        registerWidget(cb, "CheckBox", sLabel);
        if (!string.IsNullOrEmpty(sTip)) dFocusTips[cb] = sTip;
        pnlStack.Controls.Add(cb);
        if (ctlFirstFocusable == null) ctlFirstFocusable = cb;
        return cb;
    }

    // addCheckBox without tip: backward compat.
    public CheckBox addCheckBox(string sLabel, bool bValue)
    {
        return addCheckBox(sLabel, bValue, null);
    }

    // addListBox: bare pick-one list (no Label).
    //
    // WIELOKROTNY WYBOR SHIFTEM (jego zyczenie 02.09.2026: "chce na listach
    // wielokrotny wybor Shiftem, potem Control+C kopiuje kilka pozycji naraz, a
    // Alt+C dodaje do schowka").  SelectionMode.MultiExtended daje dokladnie
    // zachowanie znane z Eksploratora: Shift ze strzalka rozszerza zaznaczenie,
    // Control ze spacja dorzuca pojedyncza pozycje, sama strzalka przestawia
    // zaznaczenie na jedna - wiec dla kogos, kto Shiftu nie uzywa, NIC sie nie
    // zmienia.  To takze wybor pod czytnik: MultiExtended jest zwyklym
    // wielokrotnym wyborem listy Windows, ktory NVDA i JAWS oglaszaja same
    // ("zaznaczone"), inaczej niz lista z polami wyboru, gdzie stan trzeba
    // przelaczac spacja.
    //
    // Lista PLIKOW jest z tego wylaczona (jego decyzja) i cofa tryb do
    // SelectionMode.One u siebie - tam Enter otwiera plik, a zaznaczenie kilku
    // pozycji nie ma sensu.
    public ListBox addListBox(IList<string> lsNames, string sSelected, string sTip)
    {
        ListBox lb = new ListBox();
        lb.Size = new Size(innerWidth(), DefaultListHeight);
        lb.TabIndex = iTabIndex++;
        lb.Margin = new Padding(0, 0, 0, DefaultRowGap);
        lb.SelectionMode = SelectionMode.MultiExtended;
        populateListBox(lb, lsNames, sSelected);
        Label lblLast = currentLabelOrNull();
        if (lblLast != null) lb.AccessibleName = lblLast.AccessibleName;
        lb.GotFocus += handleGotFocus;
        // Find-in-list keys. Ctrl+J prompts for a case-insensitive
        // substring; F3 advances to the next match; Shift+F3 the
        // previous. Mirrors the data list's Jump-to and Find-Next
        // chords. Useful when picking from long lists (Alternate
        // Menu, Choose Table on a large schema).
        lb.KeyDown += new KeyEventHandler(handleListBoxFindKeys);
        lb.KeyDown += new KeyEventHandler(handleListBoxCopyKeys);
        registerWidget(lb, "ListBox", lb.AccessibleName);
        if (!string.IsNullOrEmpty(sTip)) dFocusTips[lb] = sTip;
        pnlStack.Controls.Add(lb);
        if (ctlFirstFocusable == null) ctlFirstFocusable = lb;
        return lb;
    }

    // addCheckListBox: a multi-select list as a CheckedListBox. Each
    // item toggles with Space (CheckOnClick also toggles on click), and
    // screen readers announce the checked state -- the accessible multi-
    // selection primitive the single-select addListBox does not provide.
    // lsChecked holds indices to pre-check; pass null for none.
    public CheckedListBox addCheckListBox(IList<string> lsNames, IList<int> lsChecked, string sTip)
    {
        CheckedListBox clb = new CheckedListBox();
        clb.Size = new Size(innerWidth(), DefaultListHeight);
        clb.TabIndex = iTabIndex++;
        clb.Margin = new Padding(0, 0, 0, DefaultRowGap);
        clb.CheckOnClick = true;
        foreach (string sName in lsNames) clb.Items.Add(sName);
        if (lsChecked != null) foreach (int iCheck in lsChecked) if (iCheck >= 0 && iCheck < clb.Items.Count) clb.SetItemChecked(iCheck, true);
        if (clb.Items.Count > 0) clb.SelectedIndex = 0;
        Label lblLast = currentLabelOrNull();
        if (lblLast != null) clb.AccessibleName = lblLast.AccessibleName;
        clb.GotFocus += handleGotFocus;
        registerWidget(clb, "CheckedListBox", clb.AccessibleName);
        if (!string.IsNullOrEmpty(sTip)) dFocusTips[clb] = sTip;
        pnlStack.Controls.Add(clb);
        if (ctlFirstFocusable == null) ctlFirstFocusable = clb;
        return clb;
    }

    // selectOnly: ustaw zaznaczenie na DOKLADNIE jedna pozycje.
    //
    // Odkad listy chodza w SelectionMode.MultiExtended, samo "lb.SelectedIndex = i"
    // NIE przestawia zaznaczenia, tylko DOKLADA pozycje do juz zaznaczonych - tak
    // dziala ListBox Windows w trybie wielokrotnym.  Bez tego skok F3 po kolejnych
    // trafieniach zostawialby za soba rosnacy slad zaznaczonych wierszy, a
    // Control+C kopiowalby wszystkie odwiedzone pozycje zamiast biezacej.
    // Kazde miejsce, ktore PRZESTAWIA kursor listy (szukanie, usuniecie pozycji),
    // idzie wiec tedy.
    internal static void selectOnly(ListBox lb, int iIndex)
    {
        if (lb == null) return;
        if (iIndex < 0 || iIndex >= lb.Items.Count) return;
        if (lb.SelectionMode != SelectionMode.One) lb.ClearSelected();
        lb.SelectedIndex = iIndex;
    }

    // handleListBoxFindKeys: KeyDown handler attached to every
    // ListBox added via addListBox / addPickBox. Intercepts the
    // three find-in-list chords; defers everything else to the
    // ListBox's normal handling.
    // handleListBoxCopyKeys: Control+C copies the current list
    // item's text to the clipboard; Alt+C appends it (clipboard,
    // CRLF, item), mirroring the text-control family so every
    // Lbc control answers the same chords.
    private void handleListBoxCopyKeys(object sender, KeyEventArgs evArgs)
    {
        ListBox lb = sender as ListBox;
        if (lb == null) return;
        // File-path lists (EdSharp Recent / Favorites) handle the whole copy
        // family themselves: Ctrl+C copies full PATHS, Ctrl+Shift+C the file
        // NAMES and Alt+C appends the paths, all across every selected item.
        // Defer on all three - a KeyDown handler added later in the chain runs
        // even after this one sets Handled, so leaving any of them here would
        // write to the clipboard twice and speak two different messages.
        // The Link List does the same for Ctrl+C and Ctrl+Shift+C: it copies
        // the link itself, not the "Line 12. text, address, link" row shown.
        if (lb.Tag is string && (string)lb.Tag == "edsharp-filelist"
            && (evArgs.KeyData == (Keys.Control | Keys.C)
                || evArgs.KeyData == (Keys.Control | Keys.Shift | Keys.C)
                || evArgs.KeyData == (Keys.Alt | Keys.C))) return;
        if (lb.Tag is string && (string)lb.Tag == "edsharp-linklist"
            && (evArgs.KeyData == (Keys.Control | Keys.C)
                || evArgs.KeyData == (Keys.Control | Keys.Shift | Keys.C))) return;
        if (evArgs.KeyData != (Keys.Control | Keys.C)
            && evArgs.KeyData != (Keys.Alt | Keys.C)) return;
        // WSZYSTKIE ZAZNACZONE POZYCJE, w kolejnosci z listy - nie sama pozycja
        // pod kursorem.  SelectedIndices jest juz posortowane rosnaco, wiec
        // skopiowany blok czyta sie w tej samej kolejnosci, w ktorej stoi na
        // liscie, niezaleznie od tego, czy zaznaczano z gory w dol czy odwrotnie.
        List<string> lsPicked = new List<string>();
        foreach (int iSel in lb.SelectedIndices)
            if (iSel >= 0 && iSel < lb.Items.Count && lb.Items[iSel] != null)
                lsPicked.Add(lb.Items[iSel].ToString());
        if (lsPicked.Count == 0) { Say.say("No item"); evArgs.Handled = true; evArgs.SuppressKeyPress = true; return; }
        string sItem = string.Join("\r\n", lsPicked.ToArray());
        // Liczba w komunikacie MA znaczenie: niewidomy nie widzi podswietlenia,
        // wiec inaczej nie wie, ile pozycji wlasnie zabral.
        string sHowMany = (lsPicked.Count == 1) ? "" : " " + lsPicked.Count + " items";
        bool bDone;
        if (evArgs.KeyData == (Keys.Control | Keys.C))
        {
            bDone = LbcTextBox.setClipboard(sItem);
            Say.say(bDone ? ((lsPicked.Count == 1) ? "Copied item" : "Copied" + sHowMany) : "Clipboard is busy, nothing copied");
        }
        else
        {
            bDone = LbcTextBox.setClipboard(LbcTextBox.clipboardJoin(sItem));
            Say.say(bDone ? "Appended to clipboard" + sHowMany : "Clipboard is busy, nothing appended");
        }
        evArgs.Handled = true;
        evArgs.SuppressKeyPress = true;
    }

    private void handleListBoxFindKeys(object sender, KeyEventArgs evArgs)
    {
        ListBox lb = sender as ListBox;
        if (lb == null) return;
        Keys k = evArgs.KeyData;
        if (k == (Keys.Control | Keys.J))
        { promptAndFindInListBox(lb); evArgs.Handled = true; evArgs.SuppressKeyPress = true; }
        else if (k == Keys.F3)
        { findNextInListBox(lb, true); evArgs.Handled = true; evArgs.SuppressKeyPress = true; }
        else if (k == (Keys.Shift | Keys.F3))
        { findNextInListBox(lb, false); evArgs.Handled = true; evArgs.SuppressKeyPress = true; }
    }

    // Ctrl+J: prompt for a case-insensitive substring, then jump
    // to the first item containing it. The substring is stored
    // in sListSearchTerm so subsequent F3 / Shift+F3 can advance.
    private void promptAndFindInListBox(ListBox lb)
    {
        string sPrompt = "Find substring (case-insensitive):";
        string sInitial = sListSearchTerm ?? "";
        // Simple inline input: a tiny modal dialog with one input
        // box. We don't recurse into LbcDialog because nesting
        // would complicate event routing; a vanilla Form is fine.
        using (Form prompt = new LbcForm())
        {
            prompt.Text = "Find in list";
            prompt.AccessibleName = "Find in list";
            prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
            prompt.StartPosition = FormStartPosition.CenterParent;
            prompt.MinimizeBox = false;
            prompt.MaximizeBox = false;
            prompt.ClientSize = new Size(400, 110);
            prompt.KeyPreview = true;
            Label lbl = new Label();
            lbl.Text = sPrompt;
            lbl.AutoSize = true;
            lbl.Location = new Point(12, 12);
            TextBox tb = new TextBox();
            tb.Text = sInitial;
            tb.SelectAll();
            tb.Location = new Point(12, 36);
            tb.Size = new Size(376, 20);
            tb.AccessibleName = "Find substring";
            Button btnOk = new Button();
            btnOk.Text = "&OK";
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new Point(232, 70);
            Button btnCancel = new Button();
            btnCancel.Text = "&Cancel";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(313, 70);
            prompt.Controls.Add(lbl);
            prompt.Controls.Add(tb);
            prompt.Controls.Add(btnOk);
            prompt.Controls.Add(btnCancel);
            prompt.AcceptButton = btnOk;
            prompt.CancelButton = btnCancel;
            if (prompt.ShowDialog(frm) != DialogResult.OK) return;
            sListSearchTerm = tb.Text ?? "";
        }
        if (string.IsNullOrEmpty(sListSearchTerm)) return;
        // Start search from item 0 (Ctrl+J is "find first match
        // from the top," not "find next from current position").
        int iFound = findInListBoxFrom(lb, 0, true, sListSearchTerm);
        if (iFound < 0)
            Say.say("No match for \"" + sListSearchTerm + "\"");
        else
        { selectOnly(lb, iFound); lb.Focus(); }
    }

    // F3 / Shift+F3: advance / retreat through matches of the
    // stored search term. If no search term is set, silently say
    // so (don't pop up a dialog -- F3 is a navigation key, not a
    // configuration key).
    private void findNextInListBox(ListBox lb, bool bForward)
    {
        if (string.IsNullOrEmpty(sListSearchTerm))
        { Say.say("No find substring set; press Control+J first"); return; }
        int iFrom = lb.SelectedIndex + (bForward ? 1 : -1);
        if (iFrom < 0) iFrom = lb.Items.Count - 1;
        if (iFrom >= lb.Items.Count) iFrom = 0;
        int iFound = findInListBoxFrom(lb, iFrom, bForward, sListSearchTerm);
        if (iFound < 0)
            Say.say("No more matches for \"" + sListSearchTerm + "\"");
        else
            selectOnly(lb, iFound);
    }

    // findInListBoxFrom: case-insensitive substring search through
    // the ListBox starting at iFrom, wrapping at the boundary.
    // Returns the index of the match or -1 if none.
    private static int findInListBoxFrom(ListBox lb, int iFrom, bool bForward, string sNeedle)
    {
        int n = lb.Items.Count;
        if (n == 0 || string.IsNullOrEmpty(sNeedle)) return -1;
        if (iFrom < 0) iFrom = 0;
        if (iFrom >= n) iFrom = n - 1;
        string sLowerNeedle = sNeedle.ToLowerInvariant();
        for (int i = 0; i < n; i++)
        {
            int j = bForward
                ? (iFrom + i) % n
                : ((iFrom - i) % n + n) % n;
            string sItem = (lb.Items[j] ?? "").ToString().ToLowerInvariant();
            if (sItem.Contains(sLowerNeedle)) return j;
        }
        return -1;
    }

    // addTreeView: a hierarchical pick control. The screen-reader
    // counterpart of addListBox for content that is a TREE rather
    // than a flat list -- a document's headings, an outline, a
    // folder hierarchy. Screen readers announce a TreeView item's
    // level and its expanded/collapsed state natively, which is
    // exactly the information a flat list cannot carry, so the user
    // hears "Installation, level 2, collapsed" and can skip a whole
    // branch instead of arrowing through every child.
    //
    // Accessibility notes (learned from the list boxes above):
    //   - HideSelection=false keeps the highlighted node visible and
    //     reported while focus is elsewhere in the dialog.
    //   - Letter navigation is built into TreeView (type-ahead on the
    //     node labels), so no extra key handling is needed for it.
    //   - LabelEdit stays OFF: F2 inside the tree must not start
    //     renaming a node in a read-only picker.
    //   - The tip goes to the status bar like every other control, so
    //     it must stay SHORT; register long key lists via
    //     setHelpDetail instead.
    public TreeView addTreeView(string sTip)
    {
        TreeView tv = new TreeView();
        tv.Size = new Size(innerWidth(), DefaultTreeHeight);
        tv.TabIndex = iTabIndex++;
        tv.Margin = new Padding(0, 0, 0, DefaultRowGap);
        tv.HideSelection = false;
        tv.LabelEdit = false;
        tv.ShowLines = true;
        tv.ShowPlusMinus = true;
        tv.ShowRootLines = true;
        tv.PathSeparator = "\\";
        Label lblLast = currentLabelOrNull();
        if (lblLast != null) tv.AccessibleName = lblLast.AccessibleName;
        tv.GotFocus += handleGotFocus;
        registerWidget(tv, "TreeView", tv.AccessibleName);
        if (!string.IsNullOrEmpty(sTip)) dFocusTips[tv] = sTip;
        pnlStack.Controls.Add(tv);
        if (ctlFirstFocusable == null) ctlFirstFocusable = tv;
        return tv;
    }

    // addPickTree: labeled variant, mirroring addPickBox.
    public TreeView addPickTree(string sLabel, string sTip)
    {
        addFieldLabel(sLabel);
        TreeView tv = addTreeView(sTip);
        tv.AccessibleName = cleanLabel(sLabel);
        return tv;
    }

    // addTableGrid: a spreadsheet-like grid of editable cells. The
    // screen-reader counterpart of addTreeView for content that is a
    // TABLE rather than a list or a tree: a reader announces a cell's
    // ROW and COLUMN and, crucially, the COLUMN HEADER as focus moves
    // across, which is exactly the information a stack of text boxes
    // cannot carry. The caller owns the columns, the rows and the
    // keys; this adder only builds an accessible, keyboard-only grid.
    //
    // Accessibility notes (same family as the list and tree adders):
    //   - EditOnKeystrokeOrF2 gives the spreadsheet feel a blind user
    //     expects: typing on a cell replaces its content at once,
    //     and F2 opens what is already there for correction.
    //   - CellSelect plus MultiSelect=false keeps exactly one cell
    //     current, so "where am I" always has one answer.
    //   - Row and column headers stay VISIBLE because their text is
    //     what the reader announces for orientation; they are not
    //     decoration.
    //   - The grid must never grow or shrink behind the user's back,
    //     so the built-in new-row line and drag-reorder are off; the
    //     caller adds rows and columns on explicit key presses.
    //   - The tip goes to the status bar like every other control, so
    //     it must stay SHORT; register long key lists via
    //     setHelpDetail instead.
    //   - RowTemplate is LbcGridRow, ktory MILCZY.  Bez tego czytnik przed
    //     kazda komorka oglasza obiekt wiersza slowami "Wiersz 0" - numer od
    //     zera, ktory potem komorka podaje drugi raz i od jedynki.  To bylo
    //     zgloszenie uzytkownika, nie kosmetyka; szczegoly przy klasie.
    public LbcGrid addTableGrid(string sTip)
    {
        LbcGrid dgv = new LbcGrid();
        dgv.RowTemplate = new LbcGridRow();
        dgv.Size = new Size(innerWidth(), DefaultGridHeight);
        dgv.TabIndex = iTabIndex++;
        dgv.Margin = new Padding(0, 0, 0, DefaultRowGap);
        dgv.AllowUserToAddRows = false;
        dgv.AllowUserToDeleteRows = false;
        dgv.AllowUserToOrderColumns = false;
        dgv.AllowUserToResizeRows = false;
        dgv.RowHeadersVisible = true;
        dgv.ColumnHeadersVisible = true;
        dgv.MultiSelect = false;
        dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;
        dgv.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgv.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToDisplayedHeaders;
        dgv.ScrollBars = ScrollBars.Both;
        dgv.StandardTab = false;
        Label lblLast = currentLabelOrNull();
        if (lblLast != null) dgv.AccessibleName = lblLast.AccessibleName;
        dgv.GotFocus += handleGotFocus;
        registerWidget(dgv, "TableGrid", dgv.AccessibleName);
        if (!string.IsNullOrEmpty(sTip)) dFocusTips[dgv] = sTip;
        pnlStack.Controls.Add(dgv);
        if (ctlFirstFocusable == null) ctlFirstFocusable = dgv;
        return dgv;
    }

    // addPickGrid: labeled variant, mirroring addPickTree.
    public LbcGrid addPickGrid(string sLabel, string sTip)
    {
        addFieldLabel(sLabel);
        LbcGrid dgv = addTableGrid(sTip);
        dgv.AccessibleName = cleanLabel(sLabel);
        return dgv;
    }

    // addGridColumn: the ONLY way callers should add a column to an
    // LbcGrid. It installs LbcGridCell as the column's cell template, so
    // every cell in the grid reports its CONTENT before its position to
    // the screen reader. A column added with the plain Columns.Add(name,
    // header) overload gets the default cell and silently loses that --
    // which is exactly the bug this method exists to prevent.
    public static int addGridColumn(LbcGrid dgv, string sName, string sHeader)
    {
        if (dgv == null) return -1;
        DataGridViewColumn col = new DataGridViewColumn(new LbcGridCell());
        col.Name = sName;
        col.HeaderText = sHeader;
        col.SortMode = DataGridViewColumnSortMode.NotSortable;
        return dgv.Columns.Add(col);
    }

    // addPickBox: labeled pick-one list. Equivalent to AddPickBox
    // in Homer LbC.
    public ListBox addPickBox(string sLabel, IList<string> lsNames, string sSelected, string sTip)
    {
        addFieldLabel(sLabel);
        ListBox lb = addListBox(lsNames, sSelected, sTip);
        lb.AccessibleName = cleanLabel(sLabel);
        return lb;
    }

    // addListBox(label, items, selected): backward-compat
    // 3-arg form that maps to addPickBox without a tip.
    public ListBox addListBox(string sLabel, IList<string> lsNames, string sSelected)
    {
        return addPickBox(sLabel, lsNames, sSelected, null);
    }

    // addComboBox: bare drop-down pick-one. DropDownList style
    // so the user can only pick from the list.
    public ComboBox addComboBox(IList<string> lsNames, string sSelected, string sTip)
    {
        ComboBox cb = new ComboBox();
        cb.DropDownStyle = ComboBoxStyle.DropDownList;
        cb.Size = new Size(innerWidth(), DefaultLineHeight);
        cb.TabIndex = iTabIndex++;
        cb.Margin = new Padding(0, 0, 0, DefaultRowGap);
        populateComboBox(cb, lsNames, sSelected);
        Label lblLast = currentLabelOrNull();
        if (lblLast != null) cb.AccessibleName = lblLast.AccessibleName;
        cb.GotFocus += handleGotFocus;
        registerWidget(cb, "ComboBox", cb.AccessibleName);
        if (!string.IsNullOrEmpty(sTip)) dFocusTips[cb] = sTip;
        pnlStack.Controls.Add(cb);
        if (ctlFirstFocusable == null) ctlFirstFocusable = cb;
        return cb;
    }

    // addComboPickBox: labeled drop-down pick-one. Equivalent
    // to AddComboPickBox in Homer LbC.
    public ComboBox addComboPickBox(string sLabel, IList<string> lsNames, string sSelected, string sTip)
    {
        addFieldLabel(sLabel);
        ComboBox cb = addComboBox(lsNames, sSelected, sTip);
        cb.AccessibleName = cleanLabel(sLabel);
        return cb;
    }

    // addComboBox(label, items, selected): backward-compat
    // 3-arg form that maps to addComboPickBox without a tip.
    public ComboBox addComboBox(string sLabel, IList<string> lsNames, string sSelected)
    {
        return addComboPickBox(sLabel, lsNames, sSelected, null);
    }

    // addRadioButton: one option in a radio-button group. The
    // first call after a non-RadioButton control starts a new
    // group automatically (WinForms convention).
    public RadioButton addRadioButton(string sLabel, bool bChecked, string sTip)
    {
        RadioButton rb = new RadioButton();
        rb.Text = sLabel ?? "";
        rb.UseMnemonic = true;
        // Same as the check box: the caption belongs to the control,
        // so Alt+letter selects this option directly.
        queueAccessKey(rb, sLabel ?? "");
        rb.AccessibleName = cleanLabel(sLabel);
        rb.Checked = bChecked;
        rb.AutoSize = false;
        rb.Size = new Size(innerWidth(), DefaultLineHeight);
        rb.TabIndex = iTabIndex++;
        rb.Margin = new Padding(0, 0, 0, DefaultRowGap);
        rb.GotFocus += handleGotFocus;
        registerWidget(rb, "RadioButton", sLabel);
        if (!string.IsNullOrEmpty(sTip)) dFocusTips[rb] = sTip;
        pnlStack.Controls.Add(rb);
        if (ctlFirstFocusable == null) ctlFirstFocusable = rb;
        return rb;
    }

    // addRadioButton without tip: backward compat.
    public RadioButton addRadioButton(string sLabel, bool bChecked)
    {
        return addRadioButton(sLabel, bChecked, null);
    }

    // addNumericUpDown: typed integer input with min/max bounds
    // and spinner. Equivalent to AddNumericUpDown in Homer LbC.
    public NumericUpDown addNumericUpDown(string sLabel, int iValue,
                                          int iMin, int iMax, string sTip)
    {
        if (!string.IsNullOrEmpty(sLabel)) addFieldLabel(sLabel);
        NumericUpDown nud = new NumericUpDown();
        nud.Minimum = iMin;
        nud.Maximum = iMax;
        nud.Value = Math.Max(iMin, Math.Min(iMax, iValue));
        nud.Size = new Size(DefaultNumericWidth, DefaultLineHeight);
        nud.TabIndex = iTabIndex++;
        nud.Margin = new Padding(0, 0, 0, DefaultRowGap);
        nud.AccessibleName = cleanLabel(sLabel);
        nud.GotFocus += handleGotFocus;
        registerWidget(nud, "NumericUpDown", sLabel);
        if (!string.IsNullOrEmpty(sTip)) dFocusTips[nud] = sTip;
        pnlStack.Controls.Add(nud);
        if (ctlFirstFocusable == null) ctlFirstFocusable = nud;
        return nud;
    }

    // addSeparator: a thin horizontal divider, for visually
    // grouping related fields. Not focusable.
    public void addSeparator()
    {
        Label sep = new Label();
        sep.Size = new Size(innerWidth(), 2);
        sep.BorderStyle = BorderStyle.Fixed3D;
        sep.Margin = new Padding(0, DefaultRowGap, 0, DefaultRowGap);
        pnlStack.Controls.Add(sep);
    }

    // ------- Widget lookup helpers -------
    //
    // Every widget added via add* methods is registered in
    // dWidgets under an auto-generated name of the form
    // <Kind>_<CleanedLabel>. These helpers let callers fetch
    // widgets by name without having to keep references.
    //
    // The name generator strips non-alphanumeric chars from
    // the label, replaces spaces with underscore, and appends
    // a 2/3/... suffix on collisions.

    public Control findControl(string sName)
    {
        if (string.IsNullOrEmpty(sName)) return null;
        Control ctl;
        if (dWidgets.TryGetValue(sName, out ctl)) return ctl;
        return null;
    }

    public TextBox       getTextBox(string sName)       { return findControl(sName) as TextBox; }
    public CheckBox      getCheckBox(string sName)      { return findControl(sName) as CheckBox; }
    public ComboBox      getComboBox(string sName)      { return findControl(sName) as ComboBox; }
    public ListBox       getListBox(string sName)       { return findControl(sName) as ListBox; }
    public RadioButton   getRadioButton(string sName)   { return findControl(sName) as RadioButton; }
    public NumericUpDown getNumericUpDown(string sName) { return findControl(sName) as NumericUpDown; }
    public Label         getLabel(string sName)         { return findControl(sName) as Label; }

    // Snapshot of every widget added so far, keyed by name.
    // Useful for callers that want to walk the whole dialog.
    public IDictionary<string, Control> widgets
    { get { return new Dictionary<string, Control>(dWidgets); } }

    // ------- Finish and show -------

    // runOkCancel: convenience wrapper. Returns true on OK.
    public bool runOkCancel()
    {
        return string.Equals(runWithButtons(new string[] { "OK", "Cancel" }),
                             "OK", StringComparison.OrdinalIgnoreCase);
    }

    // runWithButtons: add a button band at the bottom, show
    // modally, return the label of the button the user pressed
    // (or "" on Escape/close-box). The first label is the
    // AcceptButton (Enter); any "Cancel" or "Close" label is
    // the CancelButton (Escape).
    // Initial-focus override: when set, this control receives
    // focus once the dialog is shown (Focus() before Shown is a
    // no-op). Form-view record navigation uses it to land on
    // the same field across reopen cycles.
    private Control ctlInitialFocus;
    public void setInitialFocus(Control ctl) { ctlInitialFocus = ctl; }

    public string runWithButtons(string[] aButtonLabels)
    {
        return runWithButtons(aButtonLabels, true);
    }

    // Every Lbc dialog carries a Help button, placed rightmost in
    // the button row per Windows layout convention (and reachable
    // as Alt+H or F1). It describes the dialog's fields with
    // their tips plus the universal dialog keys. bAddHelp lets
    // the help dialog itself opt out, preventing recursion.
    public string runWithButtons(string[] aButtonLabels, bool bAddHelp)
    {
        string[] aGivenLabels = aButtonLabels;
        if (bAddHelp && Array.IndexOf(aButtonLabels, "Help") < 0)
        {
            List<string> lsAll = new List<string>(aButtonLabels);
            lsAll.Add("Help");
            aButtonLabels = lsAll.ToArray();
        }
        FlowLayoutPanel pnlButtonRow = new FlowLayoutPanel();
        pnlButtonRow.FlowDirection = FlowDirection.RightToLeft;
        pnlButtonRow.AutoSize = false;
        pnlButtonRow.Dock = DockStyle.Bottom;
        pnlButtonRow.Height = DefaultButtonHeight + DefaultPadding * 2;
        pnlButtonRow.Padding = new Padding(DefaultPadding);

        string sResult = "";
        Button btnAccept = null;
        Button btnCancel = null;

        // Add buttons right-to-left so the visual order reads
        // left-to-right as given. RightToLeft FlowDirection
        // puts the first-added at the right; we want first-
        // given at the left, so iterate in reverse.
        for (int i = aButtonLabels.Length - 1; i >= 0; i--)
        {
            string sLabel = aButtonLabels[i] ?? "";
            Button btn = new Button();
            string sPlain = sLabel.Replace("&", "");
            // OK and Cancel get NO trigger letter, on Kasperczak's own
            // decision (29.08.2026: "Enter zatwierdza, Esc anuluuje,
            // czyli jak u niego") and for a plain reason: Enter and
            // Escape already press them, so Alt+O and Alt+C would only
            // eat two letters that a real action button may need.
            bool bNoAccessKey = string.Equals(sPlain, "OK", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(sPlain, "Cancel", StringComparison.OrdinalIgnoreCase);
            // Buttons are added in REVERSE (see the comment above), so
            // the letters are claimed here in reverse order too. That
            // is harmless: the set only has to be free of duplicates,
            // and the caller's own ampersands are honoured either way.
            btn.Text = bNoAccessKey ? sPlain : claimAccessKey(sLabel);
            btn.UseMnemonic = true;
            btn.AccessibleName = sPlain;
            btn.Size = new Size(DefaultButtonWidth, DefaultButtonHeight);
            btn.TabIndex = iTabIndex++;
            btn.Margin = new Padding(DefaultRowGap, 0, 0, 0);
            btn.UseVisualStyleBackColor = true;

            string sCaptured = sLabel.Replace("&", "");
            if (bAddHelp && string.Equals(sCaptured, "Help", StringComparison.OrdinalIgnoreCase))
            {
                btn.Click += delegate(object o, EventArgs e) { showHelp(); };
            }
            else
            {
                btn.Click += delegate(object o, EventArgs e) {
                    sResult = sCaptured;
                    frm.DialogResult = DialogResult.OK;
                    frm.Close();
                };
            }
            registerWidget(btn, "Button", sCaptured);
            pnlButtonRow.Controls.Add(btn);
            if (i == 0) btnAccept = btn;
            if (string.Equals(sCaptured, "Cancel", StringComparison.OrdinalIgnoreCase)
                || string.Equals(sCaptured, "Close", StringComparison.OrdinalIgnoreCase))
                btnCancel = btn;
        }
        frm.Controls.Add(pnlButtonRow);
        // The buttons have claimed their letters; the fields get theirs
        // now, from whatever is left. Order matters: a button letter is
        // usually a deliberate choice by the calling dialog, a field
        // letter is automatic, and the deliberate one must win.
        assignQueuedAccessKeys();
        if (btnAccept != null) frm.AcceptButton = btnAccept;
        if (btnCancel != null) frm.CancelButton = btnCancel;
        // Control+Enter presses the accept button regardless of
        // keyboard focus -- including inside a multi-line memo,
        // where plain Enter inserts a newline (AcceptsReturn) and
        // never reaches the AcceptButton. This is the classic
        // windowing-guidelines rule (Enter and Control+Return both
        // invoke the default action, Control+Return covering the
        // multiline case), and it gives every Lbc dialog one
        // universal "submit from anywhere" chord.
        // Universal dialog keys, handled at form level:
        //   Control+Enter  press the accept button from anywhere
        //                  (covers memos, where plain Enter
        //                  inserts a newline)
        //   F1             show the dialog's Help
        //   Control+Home   focus the FIRST field (excluding the
        //   Control+End    button row) / the LAST field -- except
        //                  inside a multi-line memo, where these
        //                  chords keep their caret meaning
        //                  (start / end of text), which a text
        //                  editor's muscle memory expects.
        //   F7             list every focusable control in
        //                  navigation order; OK moves focus to
        //                  the chosen one.
        {
            Button btnDefault = btnAccept;
            bool bHelpHere = bAddHelp;
            frm.KeyPreview = true;
            frm.KeyDown += delegate(object sender, KeyEventArgs evArgs)
            {
                TextBox tbActive = frm.ActiveControl as TextBox;
                bool bInMemo = (tbActive != null) && tbActive.Multiline;
                if (evArgs.KeyData == (Keys.Control | Keys.Enter) && btnDefault != null)
                {
                    evArgs.Handled = true;
                    evArgs.SuppressKeyPress = true;
                    btnDefault.PerformClick();
                }
                else if (evArgs.KeyData == Keys.F1 && bHelpHere)
                {
                    evArgs.Handled = true;
                    evArgs.SuppressKeyPress = true;
                    showHelp();
                }
                else if (evArgs.KeyData == (Keys.Control | Keys.Home) && !bInMemo)
                {
                    evArgs.Handled = true;
                    evArgs.SuppressKeyPress = true;
                    focusFieldEdge(true);
                }
                else if (evArgs.KeyData == (Keys.Control | Keys.End) && !bInMemo)
                {
                    evArgs.Handled = true;
                    evArgs.SuppressKeyPress = true;
                    focusFieldEdge(false);
                }
                else if (evArgs.KeyData == Keys.F7)
                {
                    evArgs.Handled = true;
                    evArgs.SuppressKeyPress = true;
                    pickFocusControl();
                }
                // Shift+F1 speaks the focused control's tip and, when
                // registered, its full key list. LbcTextBox already
                // implements Shift+F1 for itself, so only lists are
                // handled here -- otherwise the form would swallow the
                // text box's own tip.
                else if (evArgs.KeyData == (Keys.Shift | Keys.F1)
                         && (frm.ActiveControl is ListBox))
                {
                    evArgs.Handled = true;
                    evArgs.SuppressKeyPress = true;
                    sayFocusHelp(frm.ActiveControl);
                }
            };
        }
        // Single-button confirmation dialogs (e.g., the read-only
        // memo dialog used by Invoke-Script and the speech-only
        // double-press) typically only carry an "OK" button. With
        // no explicit Cancel button, the user would otherwise
        // have to Tab to OK and press Enter or Space to dismiss
        // -- Escape would do nothing. Wire that one button as
        // BOTH AcceptButton and CancelButton so Escape, Enter,
        // and the click all close the dialog with the same
        // result. This matches the LbcDialog principle "minimum
        // keystrokes to dismiss" and the user expectation that
        // Escape always works.
        if (btnCancel == null && btnAccept != null
            && aGivenLabels.Length == 1)
            frm.CancelButton = btnAccept;

        int iContentHeight = computeStackHeight();
        int iTotalHeight = iContentHeight + pnlButtonRow.Height
                         + lblStatusBar.Height + 8;
        if (iTotalHeight > DefaultMaxHeight) iTotalHeight = DefaultMaxHeight;
        if (iTotalHeight < 200) iTotalHeight = 200;
        frm.ClientSize = new Size(DefaultDialogWidth, iTotalHeight);

        if (ctlFirstFocusable != null) frm.ActiveControl = ctlFirstFocusable;
        if (ctlInitialFocus != null)
        {
            Control ctlFocusLater = ctlInitialFocus;
            frm.Shown += delegate(object o, EventArgs e)
            { try { ctlFocusLater.Focus(); } catch { } };
        }
        frm.ShowDialog(owner);
        return sResult;
    }

    // form: outer access for callers who need to tweak something
    // the high-level API doesn't expose (e.g., add an Icon).
    public Form form { get { return frm; } }

    public void Dispose()
    {
        if (frm != null) { frm.Dispose(); frm = null; }
    }

    // ------- Internal helpers (kept private) -------

    // currentLabelOrNull: the most-recently-added Label, if the
    // last control added was a Label. Returns null if the last
    // control was something else (or the stack is empty). Used
    // by bare-control adders to inherit the accessible name
    // from an immediately-preceding Label, mirroring the Homer
    // LbC convention where AddTextBox after AddLabel automatically
    // gets the label's AccessibleName.
    private Label currentLabelOrNull()
    {
        int iCount = pnlStack.Controls.Count;
        if (iCount == 0) return null;
        return pnlStack.Controls[iCount - 1] as Label;
    }

    // handleGotFocus: status-bar tip update on focus.
    // showHelp: the Help button / F1 target. Lists the dialog's
    // fields in layout order with their focus tips, then the
    // universal dialog keys. Shown in a read-only memo so the
    // user can arrow through it line by line; the help dialog
    // suppresses its own Help button (bAddHelp false) to avoid
    // recursion.
    public void showHelp()
    {
        string sTip;
        StringBuilder sbHelp = new StringBuilder();
        sbHelp.AppendLine("Fields in this dialog:");
        foreach (Control ctl in pnlStack.Controls)
        {
            if (ctl is Label) continue;
            string sName = string.IsNullOrEmpty(ctl.AccessibleName)
                ? ctl.GetType().Name : ctl.AccessibleName;
            sbHelp.Append("  ").Append(sName);
            if (dFocusTips.TryGetValue(ctl, out sTip) && !string.IsNullOrEmpty(sTip))
                sbHelp.Append(" -- ").Append(sTip);
            sbHelp.AppendLine();
            // Long key lists live here rather than on the status bar,
            // so they never delay the dialog's opening announcement.
            string sDetail;
            if (dHelpDetails.TryGetValue(ctl, out sDetail) && !string.IsNullOrEmpty(sDetail))
                sbHelp.Append("    Keys: ").AppendLine(sDetail);
        }
        sbHelp.AppendLine();
        sbHelp.AppendLine("Dialog keys:");
        sbHelp.AppendLine("  Control+Enter presses the accept button from anywhere.");
        sbHelp.AppendLine("  Control+Home and Control+End move to the first and last field (outside multi-line text, where they move the caret).");
        sbHelp.AppendLine("  Escape cancels. F1 shows this help.");
        sbHelp.AppendLine("  F7 lists the dialog's controls in navigation order; choose one and press OK to focus it (type a letter to jump in the list).");
        sbHelp.AppendLine("  In text fields: Control+C copies the current line when nothing is selected, Alt+C appends, Control+X cuts the line, Control+D deletes it, F8 and Shift+F8 start and complete a selection, Control+F8 copies all, Alt+F8 reads all, Alt+Y says counts, Shift+F1 speaks the field tip.");
        sbHelp.AppendLine("  In lists: Control+C copies the current item, Alt+C appends it; Control+J then F3 search the list.");
        using (LbcDialog dlgHelp = new LbcDialog("Help: " + frm.Text, frm))
        {
            TextBox tbHelp = dlgHelp.addMemo(sbHelp.ToString(), null);
            tbHelp.ReadOnly = true;
            tbHelp.AccessibleName = "Help text";
            dlgHelp.runWithButtons(new string[] { "OK" }, false);
        }
    }

    // pickFocusControl: the F7 control list. Presents every
    // focusable control in the dialog -- content fields first in
    // navigation (tab) order, then the button row in visual
    // left-to-right order -- in a list box. The ListBox's native
    // type-a-letter behavior gives navigation by initial letter;
    // invoking OK moves focus to the chosen control. The idiom
    // mirrors a screen reader's elements list (JAWS Insert+F7),
    // scoped to the dialog. The chooser suppresses its own Help
    // button to stay lightweight; F7 inside it simply lists the
    // chooser's controls, which is harmless.
    private void pickFocusControl()
    {
        Dictionary<string, Control> dByName = new Dictionary<string, Control>();
        List<string> lsNames = new List<string>();
        string sCurrent = null;
        foreach (Control ctl in pnlStack.Controls)
            addFocusEntry(ctl, lsNames, dByName);
        // The button row is a sibling FlowLayoutPanel whose
        // Controls collection is stored right-to-left (see
        // runWithButtons); reverse it for navigation order.
        foreach (Control ctlTop in frm.Controls)
        {
            FlowLayoutPanel pnlRow = ctlTop as FlowLayoutPanel;
            if (pnlRow == null || pnlRow == pnlStack) continue;
            List<Control> lsRow = new List<Control>();
            foreach (Control ctl in pnlRow.Controls) lsRow.Add(ctl);
            lsRow.Reverse();
            foreach (Control ctl in lsRow) addFocusEntry(ctl, lsNames, dByName);
        }
        if (lsNames.Count == 0) { Say.say("No focusable controls"); return; }
        Control ctlActive = frm.ActiveControl;
        foreach (KeyValuePair<string, Control> pair in dByName)
        {
            if (pair.Value == ctlActive) { sCurrent = pair.Key; break; }
        }
        using (LbcDialog dlgPick = new LbcDialog("Controls: " + frm.Text, frm))
        {
            ListBox lb = dlgPick.addListBox("Choose a control to focus:",
                lsNames, sCurrent != null ? sCurrent : lsNames[0]);
            if (!string.Equals(dlgPick.runWithButtons(new string[] { "OK", "Cancel" }, false),
                    "OK", StringComparison.OrdinalIgnoreCase)) return;
            string sChosen = (lb.SelectedItem != null) ? lb.SelectedItem.ToString() : null;
            Control ctlTarget;
            if (sChosen != null && dByName.TryGetValue(sChosen, out ctlTarget))
                ctlTarget.Focus();
        }
    }

    // pressButton: programmatically invoke a button-row button
    // by its label (mnemonics ignored) -- the hook form-view
    // keys use to close-with-save (OK) or close-discarding
    // (Cancel) from inside a field handler, reusing the
    // button's own wiring.
    public bool pressButton(string sLabel)
    {
        foreach (Control ctlTop in frm.Controls)
        {
            FlowLayoutPanel pnlRow = ctlTop as FlowLayoutPanel;
            if (pnlRow == null || pnlRow == pnlStack) continue;
            foreach (Control ctl in pnlRow.Controls)
            {
                Button btn = ctl as Button;
                if (btn != null && string.Equals(btn.Text.Replace("&", ""), sLabel,
                        StringComparison.OrdinalIgnoreCase))
                { btn.PerformClick(); return true; }
            }
        }
        return false;
    }

    // addFocusEntry: register one focusable control under a
    // unique display name (AccessibleName, else button text, else
    // type name; duplicates get a numeric suffix).
    private void addFocusEntry(Control ctl, List<string> lsNames, Dictionary<string, Control> dByName)
    {
        if (ctl is Label || !ctl.CanSelect || !ctl.Visible) return;
        string sBase = !string.IsNullOrEmpty(ctl.AccessibleName) ? ctl.AccessibleName
            : (!string.IsNullOrEmpty(ctl.Text) ? ctl.Text.Replace("&", "") : ctl.GetType().Name);
        string sName = sBase;
        int iSuffix = 2;
        while (dByName.ContainsKey(sName)) { sName = sBase + " " + iSuffix; iSuffix++; }
        lsNames.Add(sName);
        dByName[sName] = ctl;
    }

    // focusFieldEdge: Control+Home/End target -- the first or
    // last selectable field in the content stack. The button row
    // lives outside pnlStack, so OK, Cancel, and Help are
    // excluded by construction.
    private void focusFieldEdge(bool bFirst)
    {
        Control ctlTarget = null;
        foreach (Control ctl in pnlStack.Controls)
        {
            if (ctl is Label || !ctl.CanSelect) continue;
            ctlTarget = ctl;
            if (bFirst) break;
        }
        if (ctlTarget != null) ctlTarget.Focus();
    }

    // setStatusText: put a message on the dialog's status line --
    // the same line that shows focus tips, so the user already
    // knows where to look (and JAWS users can reread it with the
    // say-status-bar hotkey).
    public void setStatusText(string sText)
    {
        if (lblStatusBar != null) lblStatusBar.Text = sText ?? "";
    }

    // setHelpDetail: attach a long key list to a control WITHOUT
    // putting it on the status bar. The status bar is spoken by screen
    // readers as part of the dialog's opening announcement, so a long
    // tip there delays the one thing the user actually wants to hear
    // (the focused item). Register the long text here instead: it is
    // listed under the control in the Help dialog (Help button / F1)
    // and spoken on demand by Shift+F1 on that control.
    public void setHelpDetail(Control ctl, string sDetail)
    {
        if (ctl == null || string.IsNullOrEmpty(sDetail)) return;
        dHelpDetails[ctl] = sDetail;
    }

    // sayFocusHelp: speak a control's short tip plus its registered
    // long key list, on explicit request (Shift+F1). Forced speech, so
    // it is not lost behind whatever the reader is currently saying.
    private void sayFocusHelp(Control ctl)
    {
        if (ctl == null) return;
        string sTip, sDetail;
        StringBuilder sb = new StringBuilder();
        if (dFocusTips.TryGetValue(ctl, out sTip) && !string.IsNullOrEmpty(sTip))
            sb.Append(sTip);
        if (dHelpDetails.TryGetValue(ctl, out sDetail) && !string.IsNullOrEmpty(sDetail))
        {
            if (sb.Length > 0) sb.Append(". ");
            sb.Append(sDetail);
        }
        Say.sayForced(sb.Length == 0 ? "No tip for this field" : sb.ToString());
    }

    private void handleGotFocus(object sender, EventArgs evArgs)
    {
        Control ctl = sender as Control;
        if (ctl == null) { lblStatusBar.Text = ""; return; }
        string sTip;
        lblStatusBar.Text = dFocusTips.TryGetValue(ctl, out sTip) ? sTip : "";
    }

    // handleMemoGotFocus: while a memo has focus, Enter must
    // insert a newline instead of submitting. Clear the form's
    // AcceptButton (remembering it for restore on LostFocus).
    // Also update the status bar.
    private void handleMemoGotFocus(object sender, EventArgs evArgs)
    {
        if (frm.AcceptButton != null)
            btnSavedAccept = frm.AcceptButton as Button;
        frm.AcceptButton = null;
        handleGotFocus(sender, evArgs);
    }

    // handleMemoLostFocus: restore the AcceptButton when the
    // memo loses focus, so Enter from a subsequent single-line
    // field submits as expected.
    private void handleMemoLostFocus(object sender, EventArgs evArgs)
    {
        if (btnSavedAccept != null && frm.AcceptButton == null)
            frm.AcceptButton = btnSavedAccept;
    }

    // ------- EdSharp-style text-edit hotkeys -------
    //
    // When the focused control inside the LbcDialog is a single-
    // line TextBox (Name starts with "TextBox_") or a multi-line
    // memo (Name starts with "Memo_"), the following hotkeys are
    // recognized in addition to standard text-edit behavior:
    //
    //   Ctrl+C    Copy. If text is selected, copy selection
    //             (default behavior). If no selection, copy the
    //             current line to the clipboard.
    //   Alt+C     Append to clipboard. If selected, append the
    //             selection; if no selection, append the current
    //             line. Preserves the existing clipboard content;
    //             the new piece is added on a fresh line.
    //   Ctrl+X    Cut. If selected, cut the selection (default).
    //             If no selection, cut the current line and
    //             speak the next line as feedback.
    //   Alt+X     Cut + append. Like Alt+C but removes the cut
    //             text from the buffer after appending.
    //   F8        Mark the start of a selection at the current
    //             caret position. The position is stashed in
    //             tb.Tag. The character at the start is spoken
    //             as a confirmation.
    //   Shift+F8  Complete the selection from the saved start to
    //             the current caret. Highlights the range; the
    //             length is announced.
    //   Ctrl+F8   Copy ALL text in the field to the clipboard.
    //             Status bar reports "Copy All".
    //   Alt+F8    Speak ALL text in the field via the live
    //             region. Status bar reports "Read All".
    //   Ctrl+D    Delete the current line. The next line is
    //             spoken as feedback so the user knows where the
    //             caret ended up.
    //
    // None of the above conflicts with standard control behavior:
    // F8, Shift+F8, Ctrl+F8, Alt+F8 are unbound in standard
    // WinForms TextBoxes; Ctrl+D is unbound; the Ctrl+C / Ctrl+X
    // overrides only act when there is no selection (the standard
    // Copy and Cut don't do anything without a selection); the
    // Alt+ variants are unbound. Pattern adapted from HomerLbc's
    // EdSharp-style hotkeys; see HomerLbc_40.js lines 995-1162.
    //
    // Master enable flag: [Lbc] extraKeys in DbDo.inix, default Y.
    // When N, all of the above hotkeys are passed through to
    // standard control handling.

    private static bool? bExtraKeysCached = null;
    private static bool isExtraKeysOn()
    {
        if (bExtraKeysCached.HasValue) return bExtraKeysCached.Value;
        bool bDefault = true;
        try
        {
            string sIniPath = Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath) ?? ".",
                "EdSharp.inix");
            if (!File.Exists(sIniPath)) { bExtraKeysCached = bDefault; return bDefault; }
            bool bInLbcSection = false;
            foreach (string sLine in File.ReadAllLines(sIniPath))
            {
                string sTrim = sLine.Trim();
                if (sTrim.Length == 0 || sTrim.StartsWith(";") || sTrim.StartsWith("#")) continue;
                if (sTrim.StartsWith("["))
                {
                    bInLbcSection = sTrim.Equals("[Lbc]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (!bInLbcSection) continue;
                int iEq = sTrim.IndexOf('=');
                if (iEq <= 0) continue;
                string sName = sTrim.Substring(0, iEq).Trim();
                if (!sName.Equals("extraKeys", StringComparison.OrdinalIgnoreCase)) continue;
                string sVal = sTrim.Substring(iEq + 1).Trim();
                if (sVal.Length == 0) { bExtraKeysCached = bDefault; return bDefault; }
                char c = sVal[0];
                bExtraKeysCached = !(c == 'N' || c == 'n' || c == '0' || c == 'F' || c == 'f');
                return bExtraKeysCached.Value;
            }
        }
        catch { /* tolerate; fall through to default */ }
        bExtraKeysCached = bDefault;
        return bDefault;
    }

    // textEditEligible: is the focused control a single-line text
    // box or a multi-line memo that DbDo registered via the
    // standard add* methods? Pattern matches the Name prefix that
    // registerWidget assigns (TextBox_ or Memo_).
    private static TextBox textEditEligible(Control ctl)
    {
        TextBox tb = ctl as TextBox;
        if (tb == null) return null;
        string sName = tb.Name ?? "";
        if (!sName.StartsWith("TextBox_") && !sName.StartsWith("Memo_")) return null;
        if (tb.ReadOnly) return null;
        return tb;
    }

    // onFormKeyDown: form-level KeyDown handler for the LbcDialog.
    // Frm.KeyPreview is on, so this fires before the focused
    // control sees the key. We intercept the nine EdSharp-style
    // chords listed in the section header and pass everything
    // else through.
    private void onFormKeyDown(object sender, KeyEventArgs evArgs)
    {
        if (!isExtraKeysOn()) return;
        // GetFocus via active control on the form.
        Control ctl = getActiveControl();
        TextBox tb = textEditEligible(ctl);
        if (tb == null) return;

        Keys k = evArgs.KeyData;
        bool bHandled = true;

        if      (k == Keys.F8)                          textEditStartSelection(tb);
        else if (k == (Keys.Shift   | Keys.F8))         textEditCompleteSelection(tb);
        else if (k == (Keys.Control | Keys.F8))         textEditCopyAll(tb);
        else if (k == (Keys.Alt     | Keys.F8))         textEditReadAll(tb);
        else if (k == (Keys.Control | Keys.C))          { if (tb.SelectionLength > 0) bHandled = false; else textEditCopyLine(tb); }
        else if (k == (Keys.Alt     | Keys.C))          textEditAppendLineToClipboard(tb);
        else if (k == (Keys.Control | Keys.X))          { if (tb.SelectionLength > 0) bHandled = false; else textEditCutLine(tb); }
        else if (k == (Keys.Alt     | Keys.X))          textEditCutAppend(tb);
        else if (k == (Keys.Control | Keys.D))          textEditDeleteLine(tb);
        else                                            bHandled = false;

        if (bHandled)
        {
            evArgs.Handled = true;
            evArgs.SuppressKeyPress = true;
        }
    }

    // getActiveControl: walk the active-control chain to find the
    // deepest focused control. Form.ActiveControl can return a
    // container; we descend through ContainerControls to the leaf.
    private Control getActiveControl()
    {
        Control ctl = frm.ActiveControl;
        while (ctl is ContainerControl)
        {
            Control child = ((ContainerControl)ctl).ActiveControl;
            if (child == null || child == ctl) break;
            ctl = child;
        }
        return ctl;
    }

    // Compute the (start, length) of the line containing the
    // current caret in tb. Used by every line-oriented hotkey.
    // The line END is computed as the start of the NEXT line
    // minus 1 (to exclude the trailing newline), or the end of
    // the text when on the last line.
    private static void textEditCurrentLineRange(TextBox tb, out int iStart, out int iLength)
    {
        int iCaret = tb.SelectionStart + tb.SelectionLength;
        int iRow = tb.GetLineFromCharIndex(iCaret);
        iStart = tb.GetFirstCharIndexFromLine(iRow);
        int iEnd = tb.GetFirstCharIndexFromLine(iRow + 1);
        if (iEnd <= 0) iEnd = tb.TextLength;
        else iEnd--;
        iLength = iEnd - iStart;
        if (iLength < 0) iLength = 0;
    }

    // F8 -- mark the start of a selection at the current caret.
    // The position is stored in tb.Tag as a boxed int. The char
    // at the start is spoken as a confirmation.
    private void textEditStartSelection(TextBox tb)
    {
        int iCaret = tb.SelectionStart + tb.SelectionLength;
        tb.Tag = iCaret;
        lblStatusBar.Text = "Start Selection";
        string sCh = iCaret < tb.TextLength ? tb.Text.Substring(iCaret, 1) : "end";
        Say.say("Start selection " + sCh);
    }

    // Shift+F8 -- complete a selection from the saved start to
    // the current caret. If no start was saved, fall back to
    // marking start at the current caret (so the user can press
    // F8 then Shift+F8 after another navigation step in either
    // order). Length is announced.
    private void textEditCompleteSelection(TextBox tb)
    {
        if (tb.Tag == null)
        {
            textEditStartSelection(tb);
            return;
        }
        int iStart;
        try { iStart = (int)tb.Tag; }
        catch { textEditStartSelection(tb); return; }
        int iEnd = tb.SelectionStart + tb.SelectionLength;
        if (iEnd < iStart) { int t = iStart; iStart = iEnd; iEnd = t; }
        int iLength = iEnd - iStart;
        tb.Select(iStart, iLength);
        lblStatusBar.Text = "Complete Selection, " + iLength + " character" + (iLength == 1 ? "" : "s");
        Say.say("Selected " + iLength + " character" + (iLength == 1 ? "" : "s"));
    }

    // Ctrl+F8 -- copy ALL text in the field to the clipboard.
    private void textEditCopyAll(TextBox tb)
    {
        try
        {
            if (!LbcTextBox.setClipboard(tb.Text)) { Say.say("Could not copy"); return; }
            lblStatusBar.Text = "Copy All";
            Say.say("Copied all, " + tb.TextLength + " character" + (tb.TextLength == 1 ? "" : "s"));
        }
        catch { Say.say("Could not copy"); }
    }

    // Alt+F8 -- speak ALL text in the field via the live region.
    private void textEditReadAll(TextBox tb)
    {
        lblStatusBar.Text = "Read All";
        Say.say(tb.Text);
    }

    // Ctrl+C with no selection -- copy the current line.
    private void textEditCopyLine(TextBox tb)
    {
        int iStart, iLength;
        textEditCurrentLineRange(tb, out iStart, out iLength);
        string sLine = tb.Text.Substring(iStart, iLength);
        if (!LbcTextBox.setClipboard(sLine)) { Say.say("Could not copy"); return; }
        lblStatusBar.Text = "Copy Line";
        Say.say("Copied line");
    }

    // Alt+C -- append the current line (or selection) to the
    // existing clipboard contents, separated by a newline.
    private void textEditAppendLineToClipboard(TextBox tb)
    {
        string sExisting = LbcTextBox.getClipboard();
        if (sExisting.Length > 0 && !sExisting.EndsWith("\n")) sExisting += "\r\n";
        string sAdd;
        if (tb.SelectionLength > 0)
        {
            sAdd = tb.SelectedText;
        }
        else
        {
            int iStart, iLength;
            textEditCurrentLineRange(tb, out iStart, out iLength);
            sAdd = tb.Text.Substring(iStart, iLength);
        }
        if (!LbcTextBox.setClipboard(sExisting + sAdd)) { Say.say("Could not append"); return; }
        lblStatusBar.Text = "Append to Clipboard";
        Say.say("Appended");
    }

    // Ctrl+X with no selection -- cut the current line.
    private void textEditCutLine(TextBox tb)
    {
        int iStart, iLength;
        textEditCurrentLineRange(tb, out iStart, out iLength);
        // Include the trailing newline if there is a next line
        // so the cut doesn't leave a blank line behind.
        int iCutLen = iLength;
        if (iStart + iLength < tb.TextLength)
            iCutLen = iLength + 1; // include "\n" or "\r"
        // Account for possible "\r\n" sequence -- TextBox uses
        // "\r\n" line endings on Windows; the row index treats
        // a single "\r\n" as one line break, so iEnd-1 already
        // excludes the "\n"; we also strip the "\r" by extending
        // by 1.
        if (iCutLen > 0 && iStart + iCutLen < tb.TextLength
            && tb.Text[iStart + iCutLen] == '\n')
            iCutLen++;
        string sLine = tb.Text.Substring(iStart, iCutLen);
        if (!LbcTextBox.setClipboard(sLine)) { Say.say("Could not cut"); return; }
        tb.Text = tb.Text.Remove(iStart, iCutLen);
        tb.SelectionStart = Math.Min(iStart, tb.TextLength);
        tb.SelectionLength = 0;
        lblStatusBar.Text = "Cut Line";
        // Speak the next line as feedback (the line that's now
        // at the caret).
        int iNextStart, iNextLen;
        textEditCurrentLineRange(tb, out iNextStart, out iNextLen);
        string sNext = tb.Text.Substring(iNextStart, iNextLen);
        Say.say(sNext.Length > 0 ? "Cut, now: " + sNext : "Cut, end of text");
    }

    // Alt+X -- cut current line / selection AND append to
    // clipboard. After the cut, the clipboard holds existing
    // contents plus the cut text (separator is a newline).
    private void textEditCutAppend(TextBox tb)
    {
        string sExisting = LbcTextBox.getClipboard();
        if (sExisting.Length > 0 && !sExisting.EndsWith("\n")) sExisting += "\r\n";

        string sAdd;
        int iStart, iLength;
        if (tb.SelectionLength > 0)
        {
            iStart = tb.SelectionStart;
            iLength = tb.SelectionLength;
            sAdd = tb.SelectedText;
        }
        else
        {
            textEditCurrentLineRange(tb, out iStart, out iLength);
            sAdd = tb.Text.Substring(iStart, iLength);
            // Include trailing "\n" so cut doesn't leave blank line
            if (iStart + iLength < tb.TextLength) iLength++;
            if (iLength > 0 && iStart + iLength < tb.TextLength
                && tb.Text[iStart + iLength] == '\n') iLength++;
        }
        if (!LbcTextBox.setClipboard(sExisting + sAdd)) { Say.say("Could not append"); return; }
        tb.Text = tb.Text.Remove(iStart, iLength);
        tb.SelectionStart = Math.Min(iStart, tb.TextLength);
        tb.SelectionLength = 0;
        lblStatusBar.Text = "Cut and Append";
        Say.say("Cut, appended");
    }

    // Ctrl+D -- delete the current line; speak the next line as
    // feedback. Does not touch the clipboard.
    private void textEditDeleteLine(TextBox tb)
    {
        int iStart, iLength;
        textEditCurrentLineRange(tb, out iStart, out iLength);
        int iCutLen = iLength;
        if (iStart + iLength < tb.TextLength) iCutLen++;
        if (iCutLen > 0 && iStart + iCutLen < tb.TextLength
            && tb.Text[iStart + iCutLen] == '\n') iCutLen++;
        tb.Text = tb.Text.Remove(iStart, iCutLen);
        tb.SelectionStart = Math.Min(iStart, tb.TextLength);
        tb.SelectionLength = 0;
        lblStatusBar.Text = "Delete Line";
        int iNextStart, iNextLen;
        textEditCurrentLineRange(tb, out iNextStart, out iNextLen);
        string sNext = tb.Text.Substring(iNextStart, iNextLen);
        Say.say(sNext.Length > 0 ? "Deleted, now: " + sNext : "Deleted, end of text");
    }

    // registerWidget: store the control under an auto-generated
    // name <Kind>_<CleanedLabel> in dWidgets. On collisions a
    // numeric suffix is appended (TextBox_Name, TextBox_Name_2).
    private void registerWidget(Control ctl, string sKind, string sLabel)
    {
        string sClean = makeIdentifier(sLabel);
        string sBase = sKind + "_" + sClean;
        string sName = sBase;
        int iCount;
        if (dNameCounts.TryGetValue(sBase, out iCount) && iCount > 0)
            sName = sBase + "_" + (iCount + 1);
        dNameCounts[sBase] = (dNameCounts.ContainsKey(sBase) ? dNameCounts[sBase] : 0) + 1;
        ctl.Name = sName;
        dWidgets[sName] = ctl;
    }

    // makeIdentifier: turn a label into a programmer-friendly
    // identifier suffix. Strips '&' and ':', maps any run of
    // non-alphanumeric to a single underscore, trims leading
    // and trailing underscores, falls back to "field" on empty.
    private static string makeIdentifier(string sLabel)
    {
        if (string.IsNullOrEmpty(sLabel)) return "field";
        StringBuilder sb = new StringBuilder();
        bool bLastWasUnderscore = true; // suppress leading
        foreach (char c in sLabel)
        {
            if (char.IsLetterOrDigit(c)) { sb.Append(c); bLastWasUnderscore = false; }
            else if (!bLastWasUnderscore) { sb.Append('_'); bLastWasUnderscore = true; }
        }
        string s = sb.ToString();
        if (s.EndsWith("_")) s = s.Substring(0, s.Length - 1);
        return s.Length > 0 ? s : "field";
    }

    // The horizontal space inside the stack panel for a control,
    // accounting for padding and a scrollbar reservation.
    private int innerWidth()
    {
        return DefaultDialogWidth - DefaultPadding * 2 - 24;
    }

    // cleanLabel: strip '&' mnemonic markers and trailing ':'
    // before using a label as an AccessibleName.
    private string cleanLabel(string sLabel)
    {
        if (string.IsNullOrEmpty(sLabel)) return "";
        string s = sLabel.Replace("&", "");
        if (s.EndsWith(":")) s = s.Substring(0, s.Length - 1);
        return s.Trim();
    }

    // claimAccessKey: give a label its Alt+letter, avoiding clashes.
    //
    // Every dialog field gets a trigger letter, because Alt+letter is
    // how a keyboard user reaches a field without tabbing past the
    // ones before it. Rules, in order:
    //
    //   1. An ampersand written by the CALLER wins -- BUT ONLY IF that
    //      letter is still free. Measured on our own dialogs: the
    //      compiler-settings window asks for "&Name" and
    //      "&NavigatePart", so honouring both ampersands blindly left
    //      two fields on Alt+N and one of them unreachable. A written
    //      ampersand is a preference, not a licence to duplicate.
    //   2. Otherwise the letter is the first WORD INITIAL not already
    //      claimed in this dialog. Never a letter from inside a word:
    //      screen-reader users and sighted users alike look for the
    //      underlined first letter of a word, and "S&entence" reads
    //      as a typo.
    //   3. With every initial taken, the label goes WITHOUT a letter.
    //      A field with no trigger is honest; two fields answering
    //      one key is a bug that shows up as focus landing somewhere
    //      the user did not ask for (measured, not assumed).
    //
    // Digits count as initials too: a dialog with "1st line" and
    // "2nd line" is reachable, and a digit is as pressable as a
    // letter.
    private string claimAccessKey(string sLabel)
    {
        if (string.IsNullOrEmpty(sLabel)) return sLabel;
        // The caller's own ampersand is honoured when its letter is
        // still free; a clash falls through to the automatic rule
        // below rather than producing a duplicate.
        int iGiven = sLabel.IndexOf('&');
        if (iGiven >= 0 && iGiven + 1 < sLabel.Length)
        {
            char cGiven = Char.ToUpper(sLabel[iGiven + 1]);
            if (Char.IsLetterOrDigit(cGiven) && sAccessKeysTaken.IndexOf(cGiven) < 0)
            {
                sAccessKeysTaken += cGiven;
                return sLabel;
            }
            // Letter taken: strip the request and pick automatically.
            sLabel = sLabel.Replace("&", "");
        }
        bool bAtWordStart = true;
        for (int i = 0; i < sLabel.Length; i++)
        {
            char cHere = sLabel[i];
            if (!Char.IsLetterOrDigit(cHere)) { bAtWordStart = true; continue; }
            if (bAtWordStart && sAccessKeysTaken.IndexOf(Char.ToUpper(cHere)) < 0)
            {
                sAccessKeysTaken += Char.ToUpper(cHere);
                return sLabel.Substring(0, i) + "&" + sLabel.Substring(i);
            }
            bAtWordStart = false;
        }
        // Nothing free: no trigger letter rather than a duplicate one.
        return sLabel;
    }

    // reserveAccessKey: record a letter as taken without touching any
    // label. Used for keys that belong to the dialog itself rather
    // than to a field, so no field can quietly steal them.
    private void reserveAccessKey(char cLetter)
    {
        char cUp = Char.ToUpper(cLetter);
        if (Char.IsLetterOrDigit(cUp) && sAccessKeysTaken.IndexOf(cUp) < 0)
            sAccessKeysTaken += cUp;
    }

    // queueAccessKey: remember that this control's caption needs a
    // trigger letter, to be assigned when the dialog is shown. The
    // caption is written back into the SAME control, so a label keeps
    // its trailing colon and a check box keeps its own text.
    private void queueAccessKey(Control ctl, string sLabel)
    {
        if (ctl == null) return;
        if (lsAccessKeyControls == null)
        {
            lsAccessKeyControls = new List<Control>();
            lsAccessKeyLabels = new List<string>();
        }
        lsAccessKeyControls.Add(ctl);
        lsAccessKeyLabels.Add(sLabel ?? "");
    }

    // assignQueuedAccessKeys: hand out the field letters. Called from
    // runWithButtons AFTER the buttons have claimed theirs, so a
    // deliberate button letter always wins over an automatic field one.
    private void assignQueuedAccessKeys()
    {
        if (lsAccessKeyControls == null) return;
        for (int i = 0; i < lsAccessKeyControls.Count; i++)
        {
            Control ctl = lsAccessKeyControls[i];
            if (ctl == null) continue;
            string sWith = claimAccessKey(lsAccessKeyLabels[i]);
            // A field label ends with a colon; the letter goes before
            // it, never on the punctuation.
            Label lbl = ctl as Label;
            if (lbl != null && !sWith.EndsWith(":")) sWith = sWith + ":";
            ctl.Text = sWith;
        }
        lsAccessKeyControls.Clear();
        lsAccessKeyLabels.Clear();
    }

    // addFieldLabel: emit a Label above a field control. Reused
    // by every labeled-control adder (addInputBox, addMemoBox,
    // addPickBox, addComboPickBox, addNumericUpDown).
    private void addFieldLabel(string sText)
    {
        if (string.IsNullOrEmpty(sText)) return;
        Label lbl = new Label();
        lbl.Text = sText;
        lbl.UseMnemonic = true;
        // The trigger letter goes on the LABEL, not on the field: in
        // WinForms a label's mnemonic hands focus to the next control
        // that can take it, which is exactly the field below. Measured
        // in our own panel layout, for a text box, a list and a label
        // sharing a row -- assuming it would have been guesswork.
        // Queued rather than assigned now, so the buttons pick their
        // letters first (see assignQueuedAccessKeys).
        queueAccessKey(lbl, sText);
        lbl.AccessibleName = cleanLabel(sText);
        lbl.AutoSize = false;
        lbl.Size = new Size(innerWidth(), DefaultLabelHeight);
        lbl.Margin = new Padding(0, 0, 0, 0);
        lbl.TextAlign = ContentAlignment.MiddleLeft;
        pnlStack.Controls.Add(lbl);
    }

    // populateListBox: shared logic for filling a ListBox with
    // strings and pre-selecting one.
    private void populateListBox(ListBox lb, IList<string> lsNames, string sSelected)
    {
        if (lsNames == null) return;
        foreach (string sN in lsNames) lb.Items.Add(sN);
        int iSel = -1;
        if (!string.IsNullOrEmpty(sSelected))
        {
            for (int i = 0; i < lb.Items.Count; i++)
            {
                if (string.Equals(lb.Items[i].ToString(), sSelected,
                        StringComparison.OrdinalIgnoreCase))
                { iSel = i; break; }
            }
        }
        if (iSel < 0 && lb.Items.Count > 0) iSel = 0;
        if (iSel >= 0) lb.SelectedIndex = iSel;
    }

    // populateComboBox: shared logic for filling a ComboBox.
    private void populateComboBox(ComboBox cb, IList<string> lsNames, string sSelected)
    {
        if (lsNames == null) return;
        foreach (string sN in lsNames) cb.Items.Add(sN);
        int iSel = -1;
        if (!string.IsNullOrEmpty(sSelected))
        {
            for (int i = 0; i < cb.Items.Count; i++)
            {
                if (string.Equals(cb.Items[i].ToString(), sSelected,
                        StringComparison.OrdinalIgnoreCase))
                { iSel = i; break; }
            }
        }
        if (iSel < 0 && cb.Items.Count > 0) iSel = 0;
        if (iSel >= 0) cb.SelectedIndex = iSel;
    }

    // computeStackHeight: sum heights of stack children plus
    // margins. Used to choose an initial dialog height.
    private int computeStackHeight()
    {
        int iTotal = pnlStack.Padding.Vertical;
        foreach (Control c in pnlStack.Controls)
            iTotal += c.Height + c.Margin.Vertical;
        return iTotal + 8;
    }
}

} // namespace Homer (Lbc.cs)
