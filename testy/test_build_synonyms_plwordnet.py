"""Tests for the build-time plWordNet SQL reader (no SQL execution)."""
import importlib.util
import io
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]


def load_builder():
    path = ROOT / 'tools' / 'build_synonyms_plwordnet.py'
    if not path.exists():
        raise AssertionError('The safe plWordNet SQL reader is not implemented yet')
    spec = importlib.util.spec_from_file_location('build_synonyms_plwordnet', path)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class SqlReaderTests(unittest.TestCase):
    def test_binary_ids_stay_bytes_and_polish_text_stays_utf8(self):
        reader = load_builder()
        payload = (b"(1,'" + 'ważny'.encode('utf8') +
                   b"',_binary '\\0\\'\\\\\xff0123456789ab'),(2,'cha\\'ta',NULL);")
        self.assertEqual(list(reader.parse_values(payload)), [
            (1, 'ważny'.encode('utf8'), b"\0'\\\xff0123456789ab"),
            (2, b"cha'ta", None),
        ])


def sql_table(name, columns, rows):
    def quote(value):
        if value is None:
            return b'NULL'
        if isinstance(value, int):
            return str(value).encode('ascii')
        binary = isinstance(value, bytes)
        value = value if binary else value.encode('utf8')
        value = value.replace(b'\\', b'\\\\').replace(b"'", b"\\'").replace(b'\0', b'\\0')
        return (b'_binary ' if binary else b'') + b"'" + value + b"'"
    create = ('CREATE TABLE `' + name + '` (\n' +
              ',\n'.join('  `' + col + '` text' for col in columns) +
              '\n) ENGINE=InnoDB;\n').encode('ascii')
    insert = ('INSERT INTO `' + name + '` VALUES ').encode('ascii')
    return create + insert + b','.join(b'(' + b','.join(map(quote, row)) + b')' for row in rows) + b';\n'


class LemmaIndexTests(unittest.TestCase):
    def test_only_same_synset_and_pos_in_main_polish_lexicon_are_synonyms(self):
        builder = load_builder()
        self.assertTrue(callable(getattr(builder, 'build_lemma_index', None)),
                        'The Polish synset index builder is not implemented yet')
        group = b'0123456789abcdef'
        word_ids = [bytes([x]) * 16 for x in range(1, 7)]
        source = sql_table('tbl_lexicon', ['id', 'identifier', 'language_shortcut', 'lexicon_version'],
                           [(1, 'plWN', 'pl', '5.0'), (2, 'PWN', 'en', '3.1'), (6, 's_muz', 'pl', '1.0')])
        source += sql_table('tbl_application_localised_string', ['id', 'value', 'language'], [(2, 'noun', 'en'), (4, 'adjective', 'en')])
        source += sql_table('tbl_part_of_speech', ['id', 'name_id'], [(20, 2), (40, 4)])
        columns = ['lexicon_id', 'part_of_speech_id', 'synset_position', 'id', 'synset_id', 'word_id']
        source += sql_table('tbl_sense', columns, [
            (1, 20, 0, b'a' * 16, group, word_ids[0]),
            (1, 20, 1, b'b' * 16, group, word_ids[1]),
            (2, 20, 2, b'c' * 16, group, word_ids[2]),
            (6, 20, 3, b'd' * 16, group, word_ids[3]),
            (1, 40, 4, b'e' * 16, group, word_ids[4]),
            (1, 20, 5, b'f' * 16, b'x' * 16, word_ids[5]),
        ])
        source += sql_table('tbl_word', ['word', 'id'], list(zip(
            ['dom', 'chałupa', 'home', 'tonika', 'domowy', 'budynek'], word_ids)))
        # A relation is not synonymy; the singleton "budynek" must not be merged.
        source += sql_table('tbl_synset_relation', ['child_synset_id', 'parent_synset_id'], [(group, b'x' * 16)])
        with tempfile.TemporaryDirectory() as temp:
            output = Path(temp) / 'synonyms.txt'
            stats = builder.build_lemma_index(io.BytesIO(source), output)
            lines = [line.split('\t') for line in output.read_text('utf8').splitlines() if not line.startswith('#')]
        self.assertEqual([(line[0], line[1], line[3]) for line in lines],
                         [('chałupa', 'n', 'dom'), ('dom', 'n', 'chałupa')])
        self.assertEqual(stats['source_version'], '5.0')
        self.assertEqual(stats['lemmas_with_synonyms'], 2)


class LegacyFallbackTests(unittest.TestCase):
    def test_older_data_only_fills_missing_lemmas_and_keeps_source_label(self):
        builder = load_builder()
        self.assertTrue(callable(getattr(builder, 'add_legacy_fallback', None)),
                        'The explicitly labelled legacy fallback is not implemented yet')
        with tempfile.TemporaryDirectory() as temp:
            main = Path(temp) / 'main.txt'
            legacy = Path(temp) / 'legacy.txt'
            main.write_text('# plWordNet 5.0\ndom\tn\tnew-id\tchata\n', encoding='utf8')
            legacy.write_text('dom\tn\told-dom\tchałupa\nradość\tn\told-joy\tuciecha\n', encoding='utf8')
            stats = builder.add_legacy_fallback(main, legacy)
            rows = [line.split('\t') for line in main.read_text('utf8').splitlines() if not line.startswith('#')]
        self.assertEqual(rows, [['dom', 'n', 'new-id', 'chata'],
                                ['radość', 'n', 'omw-2.0:old-joy', 'uciecha']])
        self.assertEqual(stats['legacy_fallback_lemmas'], 1)


if __name__ == '__main__':
    unittest.main()
