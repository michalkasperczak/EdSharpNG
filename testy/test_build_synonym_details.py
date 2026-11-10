"""TDD tests for the optional source-derived synonym-detail TSV builder."""
import gzip
import importlib.util
import io
import json
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
TOOL = ROOT / "tools" / "build_synonym_details.py"


def load_tool():
    if not TOOL.exists():
        raise AssertionError("The synonym-details builder is not implemented yet")
    spec = importlib.util.spec_from_file_location("build_synonym_details", TOOL)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def sql_table(name, columns, rows):
    def quote(value):
        if value is None:
            return b"NULL"
        if isinstance(value, int):
            return str(value).encode("ascii")
        binary = isinstance(value, bytes)
        raw = value if binary else value.encode("utf8")
        raw = (raw.replace(b"\\", b"\\\\").replace(b"'", b"\\'").replace(b"\0", b"\\0")
                  .replace(b"\n", b"\\n").replace(b"\r", b"\\r").replace(b"\t", b"\\t"))
        return (b"_binary " if binary else b"") + b"'" + raw + b"'"

    create = ("CREATE TABLE `" + name + "` (\n" +
              ",\n".join("  `" + column + "` text" for column in columns) +
              "\n) ENGINE=InnoDB;\n").encode("ascii")
    insert = ("INSERT INTO `" + name + "` VALUES ").encode("ascii")
    values = b",".join(b"(" + b",".join(quote(value) for value in row) + b")" for row in rows)
    return create + insert + values + b";\n"


def fixture_sql(bad_uuid=False, bad_utf8=False):
    dom = bytes.fromhex("01" * 16)
    zamek = bytes.fromhex("02" * 16)
    nonindex = bytes.fromhex("03" * 16)
    attribute_synset = b"bad" if bad_uuid else dom
    definition = b"Dom\r\n mieszkalny\t dla ludzi" if not bad_utf8 else b"\xff"
    source = sql_table("tbl_synset_attributes", ["comment", "definition", "princeton_id", "user_name", "error_comment", "ili_id", "synset_id"], [
        ("source comment must not leak", definition, None, "operator", "audit", None, attribute_synset),
        ("source comment must not leak", "Budowla\n obronna", None, "operator", "audit", None, zamek),
        ("non-index source", "NIE WOLNO", None, "operator", "audit", None, nonindex),
    ])
    source += sql_table("tbl_synset_examples", ["example", "id", "synset_attribute_id", "type_id"], [
        ("późniejszy przykład", bytes.fromhex("ff" * 16), dom, 1),
        ("  pierwszy\r\n przykład\t", bytes.fromhex("00" * 16), dom, 1),
        ("\t\r\n", bytes.fromhex("01" * 16), dom, 1),
        ("zamek przykład", bytes.fromhex("10" * 16), zamek, 1),
        ("NIE WOLNO", bytes.fromhex("02" * 16), nonindex, 1),
    ])
    # This deliberately malformed audit table must not be parsed as live source data.
    source += b"CREATE TABLE `tbl_synset_attributes_aud` (\n  `broken` text\n) ENGINE=InnoDB;\n"
    source += b"INSERT INTO `tbl_synset_attributes_aud` VALUES (_binary 'bad');\n"
    return source


class BuildSynonymDetailsTests(unittest.TestCase):
    def build_fixture(self, **source_options):
        directory = tempfile.TemporaryDirectory()
        root = Path(directory.name)
        source = root / "fixture.sql.gz"
        with gzip.open(source, "wb") as handle:
            handle.write(fixture_sql(**source_options))
        synonyms = root / "synonyms.txt"
        synonyms.write_text(
            "# EdSharpNG synonyms v1\n"
            "dom\tn\t" + "01" * 16 + "\tchata\n"
            "zamek\tn\t" + "02" * 16 + "\tforteca\n"
            "legacy\tn\tomw-2.0:old-zamek\tstary\n"
            "dom\tv\t" + "01" * 16 + "\tzamieszkiwać\n",
            encoding="utf8",
        )
        return directory, source, synonyms, root / "details.txt", root / "manifest.json"

    def test_builds_source_exact_details_for_current_index_synsets(self):
        tool = load_tool()
        directory, source, synonyms, output, manifest = self.build_fixture()
        with directory:
            result = tool.build_details(source, synonyms, output, manifest)
            self.assertEqual(output.read_text("utf8"),
                             "01" * 16 + "\tDom mieszkalny dla ludzi\tpierwszy przykład\n" +
                             "02" * 16 + "\tBudowla obronna\tzamek przykład\n")
            self.assertEqual(result["synsets_in_current_index"], 2)
            self.assertEqual(result["details_rows"], 2)
            self.assertEqual(result["definitions"], 2)
            self.assertEqual(result["examples"], 2)
            self.assertEqual(result["coverage_percent"], 100.0)
            recorded = json.loads(manifest.read_text("utf8"))
            self.assertEqual(recorded["output"]["sha256"], result["output"]["sha256"])
            self.assertEqual(recorded["source"]["bytes"], source.stat().st_size)
            self.assertEqual(recorded["input"]["bytes"], synonyms.stat().st_size)
            self.assertEqual(recorded["output"]["bytes"], output.stat().st_size)

    def test_ignores_nonindex_and_audit_rows(self):
        tool = load_tool()
        directory, source, synonyms, output, manifest = self.build_fixture()
        with directory:
            tool.build_details(source, synonyms, output, manifest)
            data = output.read_text("utf8")
        self.assertNotIn("NIE WOLNO", data)
        self.assertNotIn("source comment", data)
        self.assertNotIn("operator", data)
        self.assertNotIn("audit", data)

    def test_sense_definition_fallback_is_polish_and_meaning_bound(self):
        tool = load_tool()
        directory, source, synonyms, output, manifest = self.build_fixture()
        a, b, foreign = [bytes([n]) * 16 for n in (1, 2, 3)]
        sa, sb, sf = [bytes([n]) * 16 for n in (11, 12, 13)]
        wa, wb = bytes([21])*16, bytes([22])*16
        sql = sql_table('tbl_lexicon', ['id', 'identifier', 'language_shortcut', 'lexicon_version'], [(1,'plWN','pl','5.0'), (2,'PWN','en','3.1')])
        sql += sql_table('tbl_sense', ['id','synset_id','word_id','lexicon_id'], [(sa,a,wa,1),(sb,b,wb,1),(sf,a,wa,2)])
        sql += sql_table('tbl_word', ['id','word'], [(wa,'dom'),(wb,'zamek')])
        sql += sql_table('tbl_sense_attributes', ['sense_id','definition'], [(sa,'budynek mieszkalny'),(sb,'budowla obronna'),(sf,'NEVER FOREIGN')])
        sql += sql_table('tbl_sense_examples', ['id','sense_attribute_id','example'], [(bytes([31])*16,sa,'Dom stoi.'),(bytes([32])*16,sb,'Zamek stoi.'),(bytes([33])*16,sf,'NEVER FOREIGN')])
        sql += sql_table('tbl_synset_attributes', ['synset_id','definition'], [(a,'brak danych'),(b,'  -  ')])
        with directory:
            with gzip.open(source,'wb') as f: f.write(sql)
            tool.build_details(source, synonyms, output, manifest)
            data = output.read_text('utf8')
            self.assertIn('budynek mieszkalny',data)
            self.assertIn('budowla obronna',data)
            self.assertIn('Dom stoi.',data)
            self.assertNotIn('NEVER FOREIGN',data)
            self.assertNotIn('brak danych',data)
            rows={r.split('\t')[0]:r.split('\t')[1:] for r in data.splitlines()}
            self.assertNotIn('budowla obronna',rows[a.hex()][0])
            self.assertIn('„dom”',rows[a.hex()][0])
            self.assertIn('„dom”',rows[a.hex()][1])

    def test_placeholder_is_not_a_definition(self):
        tool = load_tool()
        self.assertEqual(tool._normalise(' brak danych '), '')
        self.assertEqual(tool._normalise(' - '), '')

    def test_rejects_invalid_live_source_uuid(self):
        tool = load_tool()
        directory, source, synonyms, output, manifest = self.build_fixture(bad_uuid=True)
        with directory:
            with self.assertRaisesRegex(ValueError, "16-byte binary identifier"):
                tool.build_details(source, synonyms, output, manifest)

    def test_rejects_invalid_live_source_utf8(self):
        tool = load_tool()
        directory, source, synonyms, output, manifest = self.build_fixture(bad_utf8=True)
        with directory:
            with self.assertRaisesRegex(UnicodeDecodeError, "utf-8"):
                tool.build_details(source, synonyms, output, manifest)


if __name__ == "__main__":
    unittest.main(verbosity=2)
