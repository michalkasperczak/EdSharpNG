"""Source contract supplemented by the live NVDA release acceptance."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]


class SynonymsUiContractTests(unittest.TestCase):
    def test_morphology_error_is_announced_before_no_results(self):
        source = (ROOT / 'EdSharp.cs').read_text(encoding='utf8')
        handler = source.split('public void Thesaurus() {', 1)[1].split('} // Thesaurus method', 1)[0]
        find = handler.index('lGroups = Synonimy.Find(sText);')
        no_results = handler.index('if (lGroups.Count == 0)')
        error_branch = handler[find:no_results]
        self.assertIn('Synonimy.FormsError', error_branch)
        self.assertIn('AddMessage(Synonimy.FormsError)', error_branch)
        self.assertIn('return;', error_branch)


if __name__ == '__main__':
    unittest.main(verbosity=2)
