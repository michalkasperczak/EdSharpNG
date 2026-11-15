"""Source contract supplemented by the live NVDA release acceptance."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]


class SynonymsUiContractTests(unittest.TestCase):
    def test_language_choice_has_command_help_without_stealing_a_shortcut(self):
        help_text = (ROOT / 'Hotkeys.ini').read_text(encoding='utf8')
        self.assertIn('Thesaurus Dictionary=, Open Settings at the Polski, Angielski or Oba', help_text)
        summary = next(line for line in help_text.splitlines() if line.startswith('Thesaurus='))
        for key in ('English', 'Right', 'F4', 'Shift+Enter', 'F1'):
            self.assertIn(key, summary)

    def test_runtime_dictionary_router_is_in_real_build_and_installer(self):
        build = (ROOT / 'BuildEdSharp.cmd').read_text(encoding='utf8')
        compile_line = next(line for line in build.splitlines() if '/out:EdSharpNG.exe' in line)
        self.assertTrue(' SlownikiSynonimow.cs ' in compile_line, 'real compiler must include dictionary router')
        installer = (ROOT / 'EdSharp_Setup.iss').read_text(encoding='utf8')
        self.assertTrue('Source: "SlownikiSynonimow.cs";' in installer, 'installed source rebuild requires router')
        packaging = (ROOT / 'build_installer_garfield.sh').read_text(encoding='utf8')
        self.assertTrue('for source in EdSharp.cs Synonimy.cs SynonimyOdmiany.cs SlownikiSynonimow.cs FormySynonimow.cs Ustawienia.cs;' in packaging, 'stale dictionary runtime/settings cannot pass freshness gate')
        self.assertIn(' FormySynonimow.cs ', compile_line)
        self.assertIn('Source: "FormySynonimow.cs";', installer)

    def test_morphology_error_is_announced_before_no_results(self):
        source = (ROOT / 'EdSharp.cs').read_text(encoding='utf8')
        handler = source.split('public void Thesaurus() {', 1)[1].split('} // Thesaurus method', 1)[0]
        find = handler.index('lGroups = SlownikiSynonimow.Find(')
        no_results = handler.index('if (lGroups.Count == 0)')
        error_branch = handler[find:no_results]
        self.assertIn('SlownikiSynonimow.Error', error_branch)
        self.assertIn('AddMessage(SlownikiSynonimow.Error)', error_branch)
        self.assertNotIn('return;', error_branch)  # one broken language cannot hide the other


    def test_shift_enter_uses_attested_forms_and_shared_actions(self):
        source = (ROOT / 'EdSharp.cs').read_text(encoding='utf8')
        dialog = source.split('public static string PickSynonyms(', 1)[1].split('} // PickSynonyms method', 1)[0]
        self.assertIn('(Keys.Shift | Keys.Enter)', dialog)
        self.assertIn('ev.IsInputKey = true', dialog, 'Shift+Enter must not fall through to the default insert button')
        self.assertIn('PickSynonymForm(', dialog)
        self.assertIn('aParts[i], aSenseIds[i]', dialog)
        self.assertIn('AddSynonymAction(dlg, lst, "Znaczenie (w prawo)", showDetails)', dialog)
        self.assertIn('AddSynonymAction(dlg, lst, "Odmiany (Shift+Enter)", chooseForms)', dialog)
        self.assertIn('sFormChoice.Length > 0', dialog)
        self.assertIn('SelectionMode.One', dialog)
        forms = source.split('public static string PickSynonymForm(', 1)[1].split('} // PickSynonymForm method', 1)[0]
        self.assertIn('FormySynonimow.GetForms(sFolder, sWord, sPart, sSenseId)', forms)
        self.assertIn('FormySynonimow.Error', forms)
        self.assertIn('Brak dostępnych odmian', forms)
        self.assertIn('return Synonimy.ApplyCapitalization(sWord, forms[index].Tekst)', forms)
        self.assertIn('setHelpDetail', forms)

    def test_right_arrow_opens_details_for_the_selected_row(self):
        source = (ROOT / 'EdSharp.cs').read_text(encoding='utf8')
        dialog = source.split('public static string PickSynonyms(', 1)[1].split('} // PickSynonyms method', 1)[0]
        self.assertIn('Keys.Right', dialog, 'Right must open details without replacing the document')

    def test_details_are_optional_and_bound_to_row_meaning(self):
        source = (ROOT / 'EdSharp.cs').read_text(encoding='utf8')
        handler = source.split('public void Thesaurus() {', 1)[1].split('} // Thesaurus method', 1)[0]
        self.assertIn('lSenseIds.Add(group.SenseId);', handler)
        self.assertIn('Dialog.PickSynonyms(', handler)
        dialog = source.split('public static string PickSynonyms(', 1)[1].split('} // PickSynonyms method', 1)[0]
        self.assertIn('Keys.F4', dialog)
        self.assertIn('SlownikiSynonimow.GetDetails(aSenseIds[i])', dialog)
        self.assertIn('ReadOnly = true', dialog)
        self.assertIn('lst.Focus()', dialog)
        self.assertNotIn('GetDetails(aValue[i])', dialog)

    def test_dictionary_choice_is_explicit_persistent_and_connected(self):
        source = (ROOT / 'EdSharp.cs').read_text(encoding='utf8')
        self.assertIn('menuMiscSynonymDictionary = CreateMenuItem(', source)
        self.assertIn('menuMiscThesaurus, menuMiscSynonymDictionary,', source)
        self.assertIn('if (menuItem == menuMiscSynonymDictionary)', source)
        chooser = source.split('public void ChooseSynonymDictionary() {', 1)[1].split('} // ChooseSynonymDictionary method', 1)[0]
        self.assertIn('PokazUstawienia("SynonymDictionary")', chooser, 'menu entry must open the same Settings field, not a second chooser')
        settings = (ROOT / 'Ustawienia.cs').read_text(encoding='utf8')
        self.assertIn('new Opcja("SynonymDictionary", "Słownik synonimów", "lista"', settings)
        self.assertIn('new string[] { "pl", "en", "both" }', settings)
        self.assertIn('dlg.setInitialFocus(dPola[sInitialOption])', source)
        self.assertNotIn('CurrentCulture', chooser)
        handler = source.split('public void Thesaurus() {', 1)[1].split('} // Thesaurus method', 1)[0]
        self.assertIn('App.ReadOption("SynonymDictionary", "pl")', handler)
        self.assertIn('SlownikiSynonimow.FormatEntry(', handler)


if __name__ == '__main__':
    unittest.main(verbosity=2)
