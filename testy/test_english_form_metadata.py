#!/usr/bin/env python3
"""TDD for the build-time-only AGID metadata generator."""
import hashlib
import json
import pathlib
import subprocess
import sys
import tempfile
import unittest

REPO = pathlib.Path(__file__).resolve().parents[1]
GENERATOR = REPO / "tools" / "build_english_form_metadata.py"


class EnglishFormMetadataGeneratorTests(unittest.TestCase):
    def run_generator(self, infl, exceptions):
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            agid = root / "infl.txt"
            source = root / "wordnet-en-exceptions.txt"
            output = root / "wordnet-en-form-metadata.txt"
            manifest = root / "manifest.json"
            agid.write_text(infl, encoding="utf-8", newline="\n")
            source.write_text(exceptions, encoding="utf-8", newline="\n")
            result = subprocess.run(
                [sys.executable, str(GENERATOR), "--agid", str(agid),
                 "--exceptions", str(source), "--output", str(output),
                 "--manifest", str(manifest)],
                text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                check=False)
            self.assertEqual(0, result.returncode, result.stderr)
            return output.read_text(encoding="utf-8"), json.loads(manifest.read_text(encoding="utf-8"))

    def test_maps_standard_and_be_slots_without_guessing_pos(self):
        infl = "\n".join((
            "go V: went | gone | going | goes",
            "be V: was, wast 2 | were | been | being | am | are, art 2 | is | are",
            "good A: better | best",
            "well A: better | best",
            "child N: children",
            "mystery V?: mystified | mystifying | mystifies",
            "bad A: worse, badder {slang} | worst, baddest {slang}",
            ""))
        exceptions = "\n".join((
            "went\tv\tgo", "gone\tv\tgo", "going\tv\tgo", "goes\tv\tgo",
            "was\tv\tbe", "were\tv\tbe", "been\tv\tbe", "being\tv\tbe", "am\tv\tbe", "are\tv\tbe", "is\tv\tbe",
            "better\ta\tgood", "best\ta\tgood", "better\tr\twell", "best\tr\twell",
            "children\tn\tchild", "mystified\tv\tmystery", "worse\ta\tbad", "worst\ta\tbad", "badder\ta\tbad", "baddest\ta\tbad", ""))
        text, manifest = self.run_generator(infl, exceptions)
        rows = [line.split("\t") for line in text.splitlines()]
        tags = {(form, pos, lemma): tagset for form, pos, lemma, tagset, source in rows}
        self.assertEqual("VBD", tags[("went", "v", "go")])
        self.assertEqual("VBN", tags[("gone", "v", "go")])
        self.assertEqual("VBG", tags[("going", "v", "go")])
        self.assertEqual("VBZ", tags[("goes", "v", "go")])
        self.assertEqual("JJR", tags[("better", "a", "good")])
        self.assertEqual("JJS", tags[("best", "a", "good")])
        self.assertEqual("RBR", tags[("better", "r", "well")])
        self.assertEqual("RBS", tags[("best", "r", "well")])
        self.assertEqual("NNS", tags[("children", "n", "child")])
        self.assertEqual("VBP", tags[("am", "v", "be")])
        self.assertEqual("VBZ", tags[("is", "v", "be")])
        self.assertEqual("VBD", tags[("was", "v", "be")])
        self.assertEqual("VBN", tags[("been", "v", "be")])
        self.assertEqual("VBG", tags[("being", "v", "be")])
        self.assertNotIn(("mystified", "v", "mystery"), tags)
        self.assertEqual("JJR", tags[("worse", "a", "bad")])
        self.assertNotIn(("badder", "a", "bad"), tags)
        self.assertEqual(18, manifest["counts"]["metadata_rows"])

    def test_preserves_all_candidate_tags_and_never_casefolds_keys(self):
        infl = "\n".join((
            "glim V: glommed | glomming | gloms",
            "glim V: glimmed | glommed | glimming | glims",
            "Cafe N: Cafes",
            ""))
        exceptions = "\n".join(("glommed\tv\tglim", "cafe\tn\tCafe", ""))
        text, manifest = self.run_generator(infl, exceptions)
        rows = [line.split("\t") for line in text.splitlines()]
        self.assertEqual(1, len(rows))
        self.assertEqual(["glommed", "v", "glim", "VBD|VBN"], rows[0][:4])
        self.assertRegex(rows[0][4], r"^AGID2016\.01\.19:[0-9]+,[0-9]+$")
        self.assertEqual(1, manifest["counts"]["ambiguous_candidate_rows"])
        self.assertEqual(1, manifest["counts"]["unmatched_exception_rows"])

    def test_is_reproducible_and_manifest_covers_actual_source_and_output(self):
        infl = "go V: went | gone | going | goes\n"
        exceptions = "went\tv\tgo\ngone\tv\tgo\ngoing\tv\tgo\ngoes\tv\tgo\n"
        first, first_manifest = self.run_generator(infl, exceptions)
        second, second_manifest = self.run_generator(infl, exceptions)
        self.assertEqual(first, second)
        self.assertEqual(first_manifest, second_manifest)
        self.assertEqual(hashlib.sha256(first.encode("utf-8")).hexdigest(), first_manifest["outputs"]["metadata"]["sha256"])
        self.assertEqual("b22230cc5250887737fdefe9ca4c9d9d01230eaa", first_manifest["agid"]["commit"])
        self.assertEqual("https://raw.githubusercontent.com/en-wl/wordlist/b22230cc5250887737fdefe9ca4c9d9d01230eaa/agid/infl.txt", first_manifest["agid"]["source_url"])
        self.assertEqual(4, first_manifest["counts"]["metadata_rows"])


if __name__ == "__main__":
    unittest.main()
