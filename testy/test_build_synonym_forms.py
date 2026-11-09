#!/usr/bin/env python3
"""TDD fixture tests for the build-time plWordNet morphology index."""
import gzip
import importlib.util
import inspect
import struct
import sys
import tempfile
import unittest
from unittest.mock import patch
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TOOL = ROOT / "tools" / "build_synonym_forms.py"


def load_tool():
    spec = importlib.util.spec_from_file_location("build_synonym_forms", TOOL)
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def fnv1a64(data):
    value = 14695981039346656037
    for byte in data:
        value = (value ^ byte) * 1099511628211 & 0xFFFFFFFFFFFFFFFF
    return value


def independent_read(path, key):
    """Protocol reader deliberately independent from the generator helpers."""
    data = Path(path).read_bytes()
    assert data[:8] in (b"EDSYNF01", b"EDSYNF02")
    count, directory_offset, total_length = struct.unpack_from("<IQQ", data, 8)
    assert total_length == len(data)
    assert directory_offset + 16 * count == len(data)
    wanted = key.encode("utf8")
    wanted_hash = fnv1a64(wanted)
    for index in range(count):
        item_hash, record_offset = struct.unpack_from("<QQ", data, directory_offset + 16 * index)
        if item_hash != wanted_hash:
            continue
        key_length = struct.unpack_from("<i", data, record_offset)[0]
        item_key = data[record_offset + 4:record_offset + 4 + key_length]
        value_offset = record_offset + 4 + key_length
        value_length = struct.unpack_from("<i", data, value_offset)[0]
        if data[:8] == b"EDSYNF02":
            expanded_length = struct.unpack_from("<i", data, value_offset + 4)[0]
            value = gzip.decompress(data[value_offset + 8:value_offset + 8 + value_length])
            assert len(value) == expanded_length
        else:
            value = data[value_offset + 4:value_offset + 4 + value_length]
        if item_key == wanted:
            return value.decode("utf8").splitlines()
    return []


def uuid_hex(byte):
    return ("%02x" % (65 + byte % 26)) * 16


def sql_fixture(corrupt_morphology_id=False):
    uuid = lambda byte: "_binary '" + chr(65 + byte % 26) * 16 + "'"
    bad = "_binary 'bad'" if corrupt_morphology_id else uuid(0x11)
    return "\n".join((
        "CREATE TABLE `tbl_application_localised_string` (",
        "  `id` bigint NOT NULL,", "  `value` text,", "  `language` varchar(255) NOT NULL,", ") ENGINE=InnoDB;",
        "CREATE TABLE `tbl_lexicon` (", "  `id` bigint NOT NULL,", "  `identifier` varchar(255) NOT NULL,", "  `language_name` varchar(255) NOT NULL,", "  `language_shortcut` varchar(5) DEFAULT NULL,", "  `name` varchar(255) NOT NULL,", "  `lexicon_version` varchar(255) NOT NULL,", ") ENGINE=InnoDB;",
        "CREATE TABLE `tbl_morphology` (", "  `id` bigint NOT NULL,", "  `sense_id` binary(16) DEFAULT NULL,", "  `word_form` varchar(255) DEFAULT NULL,", "  `morphological_tag` varchar(255) DEFAULT NULL,", ") ENGINE=InnoDB;",
        "CREATE TABLE `tbl_part_of_speech` (", "  `id` bigint NOT NULL,", "  `name_id` bigint DEFAULT NULL,", "  `color` varchar(255) DEFAULT NULL,", ") ENGINE=InnoDB;",
        "CREATE TABLE `tbl_sense` (", "  `legacy_id` bigint DEFAULT NULL,", "  `synset_position` int DEFAULT NULL,", "  `variant` int NOT NULL,", "  `domain_id` bigint NOT NULL,", "  `lexicon_id` bigint NOT NULL,", "  `part_of_speech_id` bigint NOT NULL,", "  `status_id` bigint DEFAULT NULL,", "  `id` binary(16) NOT NULL,", "  `synset_id` binary(16) DEFAULT NULL,", "  `word_id` binary(16) DEFAULT NULL,", ") ENGINE=InnoDB;",
        "CREATE TABLE `tbl_word` (", "  `legacy_id` bigint DEFAULT NULL,", "  `word` varchar(255) NOT NULL,", "  `id` binary(16) NOT NULL,", ") ENGINE=InnoDB;",
        "INSERT INTO `tbl_application_localised_string` VALUES (101,'noun','en'),(102,'verb','en');",
        "INSERT INTO `tbl_lexicon` VALUES (1,'plWN','Polish','pl','Polish WordNet','5.0'),(2,'engWN','English','en','English','5.0');",
        "INSERT INTO `tbl_morphology` VALUES (1," + bad + ",'domami','subst:pl:inst:m3'),(2," + uuid(0x12) + ",'zamkiem','subst:sg:inst:m3'),(3," + uuid(0x13) + ",'zamkiem','subst:sg:inst:m3'),(4," + uuid(0x14) + ",'ignored','subst:sg:nom:m3');",
        "INSERT INTO `tbl_part_of_speech` VALUES (1,101,NULL),(2,102,NULL);",
        "INSERT INTO `tbl_sense` VALUES (NULL,1,1,1,1,1,NULL," + uuid(0x11) + "," + uuid(0xa1) + "," + uuid(0x21) + "),(NULL,1,1,1,1,1,NULL," + uuid(0x12) + "," + uuid(0xa2) + "," + uuid(0x22) + "),(NULL,2,1,1,1,1,NULL," + uuid(0x13) + "," + uuid(0xa3) + "," + uuid(0x22) + "),(NULL,1,1,1,2,1,NULL," + uuid(0x14) + "," + uuid(0xa4) + "," + uuid(0x23) + ");",
        "INSERT INTO `tbl_word` VALUES (NULL,'dom'," + uuid(0x21) + "),(NULL,'zamek'," + uuid(0x22) + "),(NULL,'ignored'," + uuid(0x23) + ");",
        "",
    )).encode("utf8")


class BuildSynonymFormsTests(unittest.TestCase):
    def test_compressed_records_preserve_exact_source_rows(self):
        tool = load_tool()
        self.assertIn("compressed", inspect.signature(tool.build_index).parameters)
        result, data = self.build_fixture(compressed=True)
        self.assertEqual(result["format"], "EDSYNF02")
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "forms.bin"
            path.write_bytes(data)
            self.assertEqual(independent_read(path, "f:domami"), ["dom\tn\t" + uuid_hex(0xa1) + "\tsubst:pl:inst:m3"])

    def build_fixture(self, corrupt_morphology_id=False, compressed=False):
        with tempfile.TemporaryDirectory() as directory:
            directory = Path(directory)
            source = directory / "fixture.sql.gz"
            with gzip.open(source, "wb") as handle:
                handle.write(sql_fixture(corrupt_morphology_id))
            synonyms = directory / "synonyms.txt"
            synonyms.write_text(
                "# v1\n"
                "dom\tn\t11111111111111111111111111111111\tbudynek\n"
                "zamek\tn\t22222222222222222222222222222222\tforteca\n"
                "twierdza\tn\t33333333333333333333333333333333\tzamek\n",
                encoding="utf8",
            )
            output = directory / "forms.bin"
            tool = load_tool()
            result = tool.build_index(source, synonyms, output, **({"compressed": True} if compressed else {}))
            return result, output.read_bytes()

    def test_keeps_only_main_polish_lemmas_present_in_combined_synonyms(self):
        result, data = self.build_fixture()
        self.assertEqual(result["source_lexicon_identifier"], "plWN")
        self.assertEqual(result["source_lexicon_version"], "5.0")
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "forms.bin"
            path.write_bytes(data)
            self.assertEqual(independent_read(path, "f:domami"), ["dom\tn\t" + uuid_hex(0xa1) + "\tsubst:pl:inst:m3"])
            self.assertEqual(independent_read(path, "f:ignored"), [])

    def test_preserves_synset_per_meaning_instead_of_merging_zamek(self):
        _, data = self.build_fixture()
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "forms.bin"
            path.write_bytes(data)
            self.assertEqual(independent_read(path, "f:zamkiem"), [
                "zamek\tn\t" + uuid_hex(0xa2) + "\tsubst:sg:inst:m3",
                "zamek\tn\t" + uuid_hex(0xa3) + "\tsubst:sg:inst:m3",
            ])
            self.assertEqual(independent_read(path, "l:zamek"), [
                "zamkiem\tn\t" + uuid_hex(0xa2) + "\tsubst:sg:inst:m3",
                "zamkiem\tn\t" + uuid_hex(0xa3) + "\tsubst:sg:inst:m3",
            ])

    def test_writes_hash_sorted_directory_with_exact_record_data(self):
        _, data = self.build_fixture()
        count, directory_offset, total_length = struct.unpack_from("<IQQ", data, 8)
        self.assertEqual(data[:8], b"EDSYNF01")
        self.assertEqual(total_length, len(data))
        self.assertEqual(directory_offset + 16 * count, len(data))
        entries = [struct.unpack_from("<QQ", data, directory_offset + 16 * index) for index in range(count)]
        self.assertEqual(entries, sorted(entries))
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "forms.bin"
            path.write_bytes(data)
            self.assertEqual(independent_read(path, "l:dom"), ["domami\tn\t" + uuid_hex(0xa1) + "\tsubst:pl:inst:m3"])

    def test_reads_dump_twice_because_morphology_precedes_senses(self):
        with tempfile.TemporaryDirectory() as directory:
            directory = Path(directory)
            source = directory / "fixture.sql.gz"
            with gzip.open(source, "wb") as handle:
                handle.write(sql_fixture())
            synonyms = directory / "synonyms.txt"
            synonyms.write_text("dom\tn\tx\tbudynek\nzamek\tn\ty\tforteca\n", encoding="utf8")
            tool = load_tool()
            original_open = tool.gzip.open
            opens = []

            def tracked_open(path, *args, **kwargs):
                if Path(path) == source:
                    opens.append(path)
                return original_open(path, *args, **kwargs)

            with patch.object(tool.gzip, "open", side_effect=tracked_open):
                tool.build_index(source, synonyms, directory / "forms.bin")
            self.assertEqual(len(opens), 2)

    def test_rejects_corrupt_binary_uuid_in_morphology(self):
        with self.assertRaisesRegex(ValueError, "16-byte binary identifier"):
            self.build_fixture(corrupt_morphology_id=True)


if __name__ == "__main__":
    unittest.main(verbosity=2)
