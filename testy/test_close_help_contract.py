"""Prevent F1 from teaching a destructive gesture as a formatting command."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[1]


class CloseHelpTest(unittest.TestCase):
    def test_manual_points_to_live_keyboard_help(self):
        html = (ROOT / "EdSharp.htm").read_text(encoding="utf-8-sig")
        section = html.split('id="hotkey-summary">', 1)[1].split("<h3", 1)[0]
        self.assertIn("Control+Shift+F1", section)
        self.assertIn("Control+F1", section)
        self.assertNotRegex(section, r"(?m)^[^<\n]+=[^\n]+")

    def test_close_and_folder_gestures_in_manual_match_commands(self):
        html = (ROOT / "EdSharp.htm").read_text(encoding="utf-8-sig")
        code = (ROOT / "EdSharp.cs").read_text(encoding="utf-8-sig")
        for menu, key in (
            ("menuFileCurrentWindows", "F4"), ("menuFileClose", "Control+F4"),
            ("menuFileCloseAllButCurrentWindow", "Control+Shift+W"),
            ("menuMiscGoToFolder", "Shift+F4"), ("menuMiscGoToSpecialFolder", "Control+Shift+F4"),
        ):
            self.assertRegex(code, rf'{menu} = CreateMenuItem\("[^"]+", "{re.escape(key)}"')
        self.assertIn("Control+W or Control+F4 closes the current document", html)
        self.assertIn("Control+Shift+W closes the other documents", html)
        self.assertNotIn("Control+W to turn word wrap", html)
        self.assertNotIn("Control+0 lets you change the current folder", html)
        self.assertNotIn("Shift+F4 to hear the titles", html)


if __name__ == "__main__":
    unittest.main()
