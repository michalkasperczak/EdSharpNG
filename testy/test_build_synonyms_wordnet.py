#!/usr/bin/env python3
import hashlib
import json
import pathlib
import subprocess
import sys
import tempfile
import unittest
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))


class WordNetBuildTests(unittest.TestCase):
    def make_zip(self, root, members):
        source = pathlib.Path(root) / "wordnet.zip"
        with zipfile.ZipFile(source, "w", zipfile.ZIP_DEFLATED) as archive:
            archive.writestr("wordnet/LICENSE", "fixture license\n")
            for name, content in members.items():
                archive.writestr("wordnet/" + name, content)
        return source

    def test_build_writes_single_word_index_and_manifest(self):
        from build_synonyms_wordnet import build

        with tempfile.TemporaryDirectory() as temporary:
            source = self.make_zip(temporary, {
                "data.noun": "00000000 03 n 02 bank 0 depository_financial_institution 0 000 | financial institution\n",
                "data.verb": "",
                "data.adj": "",
                "data.adv": "",
                "noun.exc": "",
                "verb.exc": "",
                "adj.exc": "",
                "adv.exc": "",
            })
            output = pathlib.Path(temporary) / "out"
            manifest = build(source, output)

            self.assertEqual(
                (output / "wordnet-en-synonyms.txt").read_text(encoding="utf-8"),
                "bank\tn\twn30:n:00000000\tdepository financial institution\n",
            )
            self.assertEqual(manifest["counts"]["index_rows"], 1)
            self.assertEqual(manifest["archive"]["sha256"], hashlib.sha256(source.read_bytes()).hexdigest())

    def test_build_emits_source_only_details_exceptions_license_and_notice(self):
        from build_synonyms_wordnet import build

        with tempfile.TemporaryDirectory() as temporary:
            source = self.make_zip(temporary, {
                "data.noun": (
                    "00000000 03 n 02 bank 0 riverside 0 000 | sloping land beside a body of water\n"
                    "00000001 03 n 02 bank 0 depository_financial_institution 0 000 | a financial institution; \"the bank approved the loan\"\n"
                    "00000002 03 n 02 Canis 0 lupus 0 000 | a genus; \"Canis is capitalized\"\n"
                    "00000003 03 n 01 orphan 0 000 | should not appear\n"
                ),
                "data.verb": "00000000 03 v 02 go 0 travel 0 000 00 | move from one place to another; \"went home\"\n",
                "data.adj": (
                    "00000000 03 s 02 happy 0 glad 0 000 | feeling pleasure; \"a happy child\"\n"
                    "00000001 03 a 02 good 0 fine 0 000 | having desired qualities\n"
                ),
                "data.adv": "",
                "noun.exc": "banks bank\nchildren child\n",
                "verb.exc": "went go\n",
                "adj.exc": "",
                "adv.exc": "",
            })
            output = pathlib.Path(temporary) / "out"
            manifest = build(source, output)

            index = (output / "wordnet-en-synonyms.txt").read_text(encoding="utf-8").splitlines()
            self.assertEqual(sum(line.startswith("bank\tn\t") for line in index), 2)
            self.assertIn("Canis\tn\twn30:n:00000002\tlupus", index)
            self.assertIn("happy\ta\twn30:a:00000000\tglad", index)
            self.assertNotIn("depository financial institution", [line.split("\t")[0] for line in index])
            self.assertNotIn("orphan", [line.split("\t")[0] for line in index])
            self.assertEqual(
                (output / "wordnet-en-details.txt").read_text(encoding="utf-8").splitlines(),
                [
                    "wn30:n:00000000\tsloping land beside a body of water\t",
                    "wn30:n:00000001\ta financial institution\tthe bank approved the loan",
                    "wn30:n:00000002\ta genus\tCanis is capitalized",
                    "wn30:v:00000000\tmove from one place to another\twent home",
                    "wn30:a:00000000\tfeeling pleasure\ta happy child",
                    "wn30:a:00000001\thaving desired qualities\t",
                ],
            )
            self.assertEqual(
                (output / "wordnet-en-exceptions.txt").read_text(encoding="utf-8"),
                "banks\tn\tbank\nwent\tv\tgo\n",
            )
            self.assertEqual((output / "LICENSE-WordNet.txt").read_text(encoding="utf-8"), "fixture license\n")
            notice = (output / "NOTICE-WordNet.txt").read_text(encoding="utf-8")
            self.assertIn("https://raw.githubusercontent.com/nltk/nltk_data/gh-pages/packages/corpora/wordnet.zip", notice)
            self.assertIn("Princeton WordNet 3.0", notice)
            self.assertEqual(manifest["counts"]["usable_synsets"], 6)
            self.assertEqual(manifest["counts"]["examples"], 4)
            self.assertEqual(manifest["counts"]["exceptions"], 2)

    def test_build_strips_adjective_syntactic_markers_and_counts_unique_lemmas(self):
        from build_synonyms_wordnet import build

        with tempfile.TemporaryDirectory() as temporary:
            source = self.make_zip(temporary, {
                "data.noun": "00000000 03 n 02 top 0 apex 0 000 | the highest point\n",
                "data.verb": "",
                "data.adj": "00000000 03 s 03 ablaze(p) 0 on_fire(p) 0 tops(p) 0 000 | burning brightly\n"
                            "00000001 03 a 02 top(a) 0 upper(a) 0 000 | situated above\n",
                "data.adv": "",
                "noun.exc": "",
                "verb.exc": "",
                "adj.exc": "ablazes ablaze(p)\n",
                "adv.exc": "",
            })
            output = pathlib.Path(temporary) / "out"
            manifest = build(source, output)

            index = (output / "wordnet-en-synonyms.txt").read_text(encoding="utf-8")
            self.assertIn("ablaze\ta\twn30:a:00000000\ton fire|tops\n", index)
            self.assertIn("tops\ta\twn30:a:00000000\tablaze|on fire\n", index)
            self.assertIn("top\ta\twn30:a:00000001\tupper\n", index)
            self.assertNotIn("(p)", index)
            self.assertNotIn("(a)", index)
            self.assertEqual(
                (output / "wordnet-en-exceptions.txt").read_text(encoding="utf-8"),
                "ablazes\ta\tablaze\n",
            )
            self.assertEqual(manifest["counts"]["usable_lemmas"], 5)
            self.assertEqual(manifest["counts"]["lemma_pos_pairs"], 6)

    def test_build_preserves_later_definitions_and_attributed_first_real_example(self):
        from build_synonyms_wordnet import build

        with tempfile.TemporaryDirectory() as temporary:
            source = self.make_zip(temporary, {
                "data.noun": (
                    "00000000 03 n 02 quote 0 saying 0 000 | a \"quoted phrase\" in the definition; \"\"; "
                    "a later definition; \"a real example\"- Henry Miller; a final definition\n"
                ),
                "data.verb": "",
                "data.adj": "",
                "data.adv": "",
                "noun.exc": "",
                "verb.exc": "",
                "adj.exc": "",
                "adv.exc": "",
            })
            output = pathlib.Path(temporary) / "out"
            build(source, output)

            self.assertEqual(
                (output / "wordnet-en-details.txt").read_text(encoding="utf-8"),
                'wn30:n:00000000\ta "quoted phrase" in the definition; a later definition; a final definition\ta real example- Henry Miller\n',
            )

    def test_committed_resources_preserve_generator_hashes_on_checkout(self):
        from hashlib import sha256

        manifest = json.loads((ROOT / 'testy/wyniki/english-synonyms/build.json').read_text(encoding='utf-8'))
        names = {
            'synonyms': 'wordnet-en-synonyms.txt', 'details': 'wordnet-en-details.txt',
            'exceptions': 'wordnet-en-exceptions.txt', 'license': 'LICENSE-WordNet.txt',
            'notice': 'NOTICE-WordNet.txt',
        }
        for key, name in names.items():
            path = ROOT / 'Synonyms' / name
            raw = path.read_bytes()
            self.assertEqual(sha256(raw).hexdigest(), manifest['outputs'][key]['sha256'], name)
            self.assertEqual(len(raw), manifest['outputs'][key]['bytes'], name)
            attributes = subprocess.run(
                ['git', 'check-attr', 'eol', '--', 'Synonyms/' + name],
                cwd=ROOT, check=True, capture_output=True, text=True,
            ).stdout.strip()
            self.assertTrue(attributes.endswith(': eol: lf'), attributes)

    def test_later_examples_are_not_mislabelled_as_definitions(self):
        from build_synonyms_wordnet import _split_gloss

        self.assertEqual(
            _split_gloss('a vessel; "a first example"; "a second example"; another definition'),
            ('a vessel; another definition', 'a first example'),
        )

    def test_build_counts_distinct_lowercased_lemmas_not_casefolded_lemmas(self):
        from build_synonyms_wordnet import build

        with tempfile.TemporaryDirectory() as temporary:
            source = self.make_zip(temporary, {
                "data.noun": (
                    "00000000 03 n 02 Straße 0 road 0 000 | a street\n"
                    "00000001 03 n 02 STRASSE 0 avenue 0 000 | a broad street\n"
                    "00000002 03 n 02 ſtage 0 platform 0 000 | a raised floor\n"
                ),
                "data.verb": "",
                "data.adj": "",
                "data.adv": "",
                "noun.exc": "longs ſtage\nstreets Straße\n",
                "verb.exc": "",
                "adj.exc": "",
                "adv.exc": "",
            })
            manifest = build(source, pathlib.Path(temporary) / "out")

            self.assertEqual(manifest["counts"]["usable_lemmas"], 6)
            self.assertEqual(manifest["counts"]["lemma_pos_pairs"], 6)
            self.assertEqual(
                (pathlib.Path(temporary) / "out" / "wordnet-en-exceptions.txt").read_text(encoding="utf-8"),
                "longs\tn\tſtage\nstreets\tn\tStraße\n",
            )

    def test_build_rejects_control_in_source_detail(self):
        from build_synonyms_wordnet import build

        with tempfile.TemporaryDirectory() as temporary:
            source = self.make_zip(temporary, {
                "data.noun": "00000000 03 n 02 safe 0 sound 0 000 | unsafe\x01detail\n",
                "data.verb": "",
                "data.adj": "",
                "data.adv": "",
                "noun.exc": "",
                "verb.exc": "",
                "adj.exc": "",
                "adv.exc": "",
            })
            with self.assertRaisesRegex(ValueError, "unsafe detail"):
                build(source, pathlib.Path(temporary) / "out")

    def test_build_rejects_indented_malformed_data_record(self):
        from build_synonyms_wordnet import build

        with tempfile.TemporaryDirectory() as temporary:
            source = self.make_zip(temporary, {
                "data.noun": "  00000000 03 n 02 safe 0 sound 0 000 | malformed offset\n",
                "data.verb": "",
                "data.adj": "",
                "data.adv": "",
                "noun.exc": "",
                "verb.exc": "",
                "adj.exc": "",
                "adv.exc": "",
            })
            with self.assertRaisesRegex(ValueError, "malformed data.noun:1"):
                build(source, pathlib.Path(temporary) / "out")

    def test_build_validates_archive_and_is_repeatable_through_cli(self):
        from build_synonyms_wordnet import build

        members = {
            "data.noun": "00000000 03 n 02 quote 0 saying 0 000 | first \"definition\" segment; more detail; \"first real example\"\n",
            "data.verb": "00000000 03 v 02 go 0 travel 0 000 00 | move\n",
            "data.adj": "",
            "data.adv": "",
            "noun.exc": "went go\n",
            "verb.exc": "went go\n",
            "adj.exc": "",
            "adv.exc": "",
        }
        with tempfile.TemporaryDirectory() as temporary:
            source = self.make_zip(temporary, members)
            first = pathlib.Path(temporary) / "first"
            second = pathlib.Path(temporary) / "second"
            self.assertEqual(build(source, first), build(source, second))
            for name in ("wordnet-en-synonyms.txt", "wordnet-en-details.txt", "wordnet-en-exceptions.txt"):
                self.assertEqual((first / name).read_bytes(), (second / name).read_bytes())
            self.assertEqual(
                (first / "wordnet-en-details.txt").read_text(encoding="utf-8").splitlines()[0],
                'wn30:n:00000000\tfirst "definition" segment; more detail\tfirst real example',
            )
            self.assertEqual((first / "wordnet-en-exceptions.txt").read_text(encoding="utf-8"), "went\tv\tgo\n")
            manifest_path = pathlib.Path(temporary) / "build.json"
            completed = subprocess.run(
                [sys.executable, str(ROOT / "tools" / "build_synonyms_wordnet.py"),
                 "--source", str(source), "--output-dir", str(second), "--manifest", str(manifest_path)],
                check=False, capture_output=True, text=True,
            )
            self.assertEqual(completed.returncode, 0, completed.stderr)
            self.assertEqual(json.loads(manifest_path.read_text(encoding="utf-8")), build(source, first))

            malformed = self.make_zip(temporary, {**members, "data.noun": "00000000 03 n 01 broken 0 000\n"})
            with self.assertRaisesRegex(ValueError, "malformed data.noun:1"):
                build(malformed, pathlib.Path(temporary) / "bad")
            duplicate = self.make_zip(temporary, {**members, "data.noun": (
                "00000000 03 n 02 quote 0 saying 0 000 | first\n"
                "00000000 03 n 02 second 0 third 0 000 | duplicate\n"
            )})
            with self.assertRaisesRegex(ValueError, "duplicate sense ID"):
                build(duplicate, pathlib.Path(temporary) / "duplicate")
            with zipfile.ZipFile(source, "a") as archive:
                archive.writestr("../not-extracted", "x")
            with self.assertRaisesRegex(ValueError, "unsafe archive member"):
                build(source, pathlib.Path(temporary) / "unsafe")

            invalid_utf8 = pathlib.Path(temporary) / "invalid.zip"
            with zipfile.ZipFile(invalid_utf8, "w") as archive:
                archive.writestr("wordnet/LICENSE", b"license\n")
                archive.writestr("wordnet/data.noun", b"\xff")
                for name in ("data.verb", "data.adj", "data.adv", "noun.exc", "verb.exc", "adj.exc", "adv.exc"):
                    archive.writestr("wordnet/" + name, b"")
            with self.assertRaisesRegex(ValueError, "invalid UTF-8"):
                build(invalid_utf8, pathlib.Path(temporary) / "utf8")


if __name__ == "__main__":
    unittest.main()
