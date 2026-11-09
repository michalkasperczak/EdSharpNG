#!/usr/bin/env python3
"""Build the offline EdSharpNG plWordNet morphology index.

This tool runs only while preparing application data.  The generated binary has
no Python, SQL, or database dependency at application runtime.
"""
from __future__ import annotations

import argparse
import gzip
import json
import os
import sqlite3
import struct
import sys
import time
from pathlib import Path
from typing import Any

# Support both ``python tools/build_synonym_forms.py`` and direct test loading.
TOOLS_DIRECTORY = str(Path(__file__).resolve().parent)
if TOOLS_DIRECTORY not in sys.path:
    sys.path.insert(0, TOOLS_DIRECTORY)
from build_synonyms_plwordnet import _single_word, _text, _uuid, iter_table_rows

MAGIC = b"EDSYNF01"
HEADER_SIZE = 28
FNV_OFFSET = 14695981039346656037
FNV_PRIME = 1099511628211
MAX_KEY_BYTES = 4096
MAX_VALUE_BYTES = 4 * 1024 * 1024
PART_NAMES = {"noun": "n", "verb": "v", "adjective": "a", "adverb": "r"}
TABLES = {
    "tbl_lexicon", "tbl_application_localised_string", "tbl_part_of_speech",
    "tbl_word", "tbl_sense", "tbl_morphology",
}


def fnv1a64(data: bytes) -> int:
    value = FNV_OFFSET
    for byte in data:
        value = ((value ^ byte) * FNV_PRIME) & 0xFFFFFFFFFFFFFFFF
    return value


def _safe_field(value: bytes | None, field: str) -> str | None:
    if value is None:
        return None
    text = _text(value).strip()
    if not text:
        return None
    if "|" in text or "\t" in text or "\r" in text or "\n" in text:
        raise ValueError("Invalid " + field + " field")
    return text


def _combined_lemmas(path: Path) -> set[str]:
    """Return every single-word token available in the combined v1 synonym data."""
    lemmas: set[str] = set()
    with path.open("r", encoding="utf8", newline=None) as source:
        for number, line in enumerate(source, 1):
            line = line.rstrip("\r\n")
            if not line or line.startswith("#"):
                continue
            fields = line.split("\t")
            if len(fields) != 4 or fields[1] not in ("n", "v", "a", "r") or not fields[2]:
                raise ValueError("Invalid combined synonym index at line " + str(number))
            for item in (fields[0], *fields[3].split("|")):
                word = _single_word(item.encode("utf8"))
                if word is not None:
                    lemmas.add(word)
    if not lemmas:
        raise ValueError("Combined synonym index has no usable lemmas")
    return lemmas


def _open_database() -> sqlite3.Connection:
    scratch = Path(os.environ["TMPDIR"])
    scratch.mkdir(parents=True, exist_ok=True)
    database = scratch / ("edsharp-synonym-forms-" + str(os.getpid()) + ".sqlite3")
    if database.exists():
        database.unlink()
    connection = sqlite3.connect(str(database))
    connection.execute("PRAGMA journal_mode=OFF")
    connection.execute("PRAGMA synchronous=OFF")
    connection.execute("PRAGMA temp_store=FILE")
    connection.execute("PRAGMA cache_size=-32768")
    connection.execute("CREATE TABLE morphology (sense_id BLOB NOT NULL, form TEXT NOT NULL, tag TEXT NOT NULL)")
    connection.execute("CREATE TABLE senses (id BLOB PRIMARY KEY, synset BLOB NOT NULL, word_id BLOB NOT NULL, part TEXT NOT NULL) WITHOUT ROWID")
    connection.execute("CREATE TABLE words (id BLOB PRIMARY KEY, word TEXT NOT NULL) WITHOUT ROWID")
    connection.execute("CREATE TABLE records (key TEXT NOT NULL, value TEXT NOT NULL, PRIMARY KEY (key, value)) WITHOUT ROWID")
    return connection


def _flush(connection: sqlite3.Connection, sql: str, rows: list[tuple]) -> None:
    if rows:
        connection.executemany(sql, rows)
        rows.clear()


def _first_pass(source: Path, connection: sqlite3.Connection) -> tuple[dict[str, Any], int, dict[int, str], dict[int, int]]:
    """Persist forms and read metadata before the later sense/word pass."""
    localised: dict[int, str] = {}
    parts: dict[int, int] = {}
    source_lexicon_id: int | None = None
    source_lexicon: tuple[str, str, str] | None = None
    morphology_batch: list[tuple] = []
    counts: dict[str, Any] = {"morphology_rows_read": 0, "morphology_rows_usable": 0,
              "sense_rows_main_polish": 0, "word_rows_usable": 0}
    tables = {"tbl_lexicon", "tbl_application_localised_string", "tbl_part_of_speech", "tbl_morphology"}
    with gzip.open(source, "rb") as handle:
        for table, row in iter_table_rows(handle, tables):
            if table == "tbl_application_localised_string":
                value = row["value"]
                if row["language"] == b"en" and value is not None and _text(value) in PART_NAMES:
                    localised[row["id"]] = _text(value)
            elif table == "tbl_lexicon":
                item = (_text(row["identifier"]), _text(row["language_shortcut"]) if row["language_shortcut"] else "", _text(row["lexicon_version"]))
                if item == ("plWN", "pl", "5.0"):
                    if source_lexicon_id is not None:
                        raise ValueError("More than one main Polish plWN 5.0 lexicon")
                    source_lexicon_id, source_lexicon = row["id"], item
            elif table == "tbl_part_of_speech" and row["name_id"] is not None:
                parts[row["id"]] = row["name_id"]
            elif table == "tbl_morphology":
                counts["morphology_rows_read"] += 1
                sense_id, form, tag = row["sense_id"], row["word_form"], row["morphological_tag"]
                if sense_id is not None and form is not None and tag is not None:
                    morphology_batch.append((_uuid(sense_id), _single_word(form), _safe_field(tag, "morphological tag")))
                if len(morphology_batch) >= 10000:
                    usable = [item for item in morphology_batch if item[1] is not None and item[2] is not None]
                    counts["morphology_rows_usable"] += len(usable)
                    _flush(connection, "INSERT INTO morphology VALUES (?, ?, ?)", usable)
                    morphology_batch.clear()
    usable = [item for item in morphology_batch if item[1] is not None and item[2] is not None]
    counts["morphology_rows_usable"] += len(usable)
    _flush(connection, "INSERT INTO morphology VALUES (?, ?, ?)", usable)
    if source_lexicon_id is None or source_lexicon is None:
        raise ValueError("Expected exactly one main Polish plWN 5.0 lexicon")
    actual_parts = {part_id: PART_NAMES[localised[name_id]] for part_id, name_id in parts.items() if name_id in localised}
    counts.update({"source_lexicon_id": source_lexicon_id, "source_lexicon_identifier": source_lexicon[0],
                   "source_lexicon_language": source_lexicon[1], "source_lexicon_version": source_lexicon[2],
                   "part_of_speech_ids": {str(key): value for key, value in sorted(actual_parts.items())}})
    return counts, source_lexicon_id, localised, parts


def _second_pass(source: Path, allowed_lemmas: set[str], connection: sqlite3.Connection,
                 source_lexicon_id: int, localised: dict[int, str], parts: dict[int, int], counts: dict[str, Any]) -> None:
    """Resolve stored morphology IDs to the main Polish senses and words."""
    sense_batch: list[tuple] = []
    word_batch: list[tuple] = []
    with gzip.open(source, "rb") as handle:
        for table, row in iter_table_rows(handle, {"tbl_sense", "tbl_word"}):
            if table == "tbl_sense":
                if row["lexicon_id"] != source_lexicon_id:
                    continue
                counts["sense_rows_main_polish"] += 1
                name_id = parts.get(row["part_of_speech_id"])
                part = PART_NAMES.get(localised.get(name_id, "") if name_id is not None else "")
                if part is None:
                    raise ValueError("Unknown main Polish part of speech")
                if row["synset_id"] is not None and row["word_id"] is not None:
                    sense_batch.append((_uuid(row["id"]), _uuid(row["synset_id"]), _uuid(row["word_id"]), part))
                    if len(sense_batch) >= 10000:
                        _flush(connection, "INSERT INTO senses VALUES (?, ?, ?, ?)", sense_batch)
            else:
                word = _single_word(row["word"])
                if word is not None and word in allowed_lemmas:
                    word_batch.append((_uuid(row["id"]), word))
                    if len(word_batch) >= 10000:
                        counts["word_rows_usable"] += len(word_batch)
                        _flush(connection, "INSERT INTO words VALUES (?, ?)", word_batch)
    counts["word_rows_usable"] += len(word_batch)
    _flush(connection, "INSERT INTO senses VALUES (?, ?, ?, ?)", sense_batch)
    _flush(connection, "INSERT INTO words VALUES (?, ?)", word_batch)
    connection.commit()


def _parse_source(source: Path, allowed_lemmas: set[str], connection: sqlite3.Connection) -> dict:
    """Use two dump passes because tbl_morphology precedes tbl_sense."""
    counts, source_lexicon_id, localised, parts = _first_pass(source, connection)
    _second_pass(source, allowed_lemmas, connection, source_lexicon_id, localised, parts, counts)
    return counts


def _make_records(connection: sqlite3.Connection) -> tuple[int, int]:
    """Join raw forms to main-plWN senses and create both lookup directions."""
    cursor = connection.execute(
        "SELECT m.form, w.word, s.part, hex(s.synset), m.tag "
        "FROM morphology AS m JOIN senses AS s ON s.id = m.sense_id "
        "JOIN words AS w ON w.id = s.word_id ORDER BY m.form, w.word, s.part, s.synset, m.tag"
    )
    batch: list[tuple[str, str]] = []
    source_lemmas: set[str] = set()
    source_rows = 0
    for form, lemma, part, synset, tag in cursor:
        synset = synset.lower()
        value_for_form = "\t".join((lemma, part, synset, tag))
        value_for_lemma = "\t".join((form, part, synset, tag))
        batch.append(("f:" + form, value_for_form))
        batch.append(("l:" + lemma, value_for_lemma))
        source_lemmas.add(lemma)
        source_rows += 1
        if len(batch) >= 10000:
            _flush(connection, "INSERT OR IGNORE INTO records VALUES (?, ?)", batch)
    _flush(connection, "INSERT OR IGNORE INTO records VALUES (?, ?)", batch)
    connection.commit()
    record_rows = connection.execute("SELECT COUNT(*) FROM records").fetchone()[0]
    return len(source_lemmas), record_rows


def _write_binary(connection: sqlite3.Connection, output: Path, compressed: bool = False) -> dict:
    output.parent.mkdir(parents=True, exist_ok=True)
    temporary = output.with_name(output.name + ".building")
    directory: list[tuple[int, bytes, int]] = []
    key_count = 0
    value_rows = 0
    with temporary.open("wb") as dest:
        dest.write(b"EDSYNF02" if compressed else MAGIC)
        dest.write(b"\0" * 20)
        current_key: str | None = None
        values: list[str] = []

        def write_record(key: str, rows: list[str]) -> None:
            nonlocal key_count, value_rows
            key_bytes = key.encode("utf8")
            value_bytes = ("\n".join(rows) + "\n").encode("utf8")
            if len(key_bytes) > MAX_KEY_BYTES:
                raise ValueError("Key exceeds " + str(MAX_KEY_BYTES) + " bytes: " + key)
            if len(value_bytes) > MAX_VALUE_BYTES:
                raise ValueError("Value exceeds " + str(MAX_VALUE_BYTES) + " bytes: " + key)
            offset = dest.tell()
            dest.write(struct.pack("<i", len(key_bytes)))
            dest.write(key_bytes)
            if compressed:
                packed = gzip.compress(value_bytes, compresslevel=6, mtime=0)
                dest.write(struct.pack("<ii", len(packed), len(value_bytes)))
                dest.write(packed)
            else:
                dest.write(struct.pack("<i", len(value_bytes)))
                dest.write(value_bytes)
            directory.append((fnv1a64(key_bytes), key_bytes, offset))
            key_count += 1
            value_rows += len(rows)

        for key, value in connection.execute("SELECT key, value FROM records ORDER BY key, value"):
            if current_key is not None and key != current_key:
                write_record(current_key, values)
                values = []
            current_key = key
            values.append(value)
        if current_key is not None:
            write_record(current_key, values)
        directory_offset = dest.tell()
        for hashed, key_bytes, offset in sorted(directory, key=lambda item: (item[0], item[1])):
            dest.write(struct.pack("<QQ", hashed, offset))
        total_length = dest.tell()
        if directory_offset + 16 * key_count != total_length:
            raise AssertionError("Invalid directory length")
        dest.seek(8)
        dest.write(struct.pack("<IQQ", key_count, directory_offset, total_length))
    temporary.replace(output)
    return {"keys": key_count, "value_rows": value_rows,
            "directory_offset": directory_offset, "total_file_length": total_length}


def build_index(source: str | Path, synonyms: str | Path, output: str | Path, manifest: str | Path | None = None, compressed: bool = False) -> dict:
    """Build index and a reproducible JSON manifest; return its complete data."""
    started = time.monotonic()
    source, synonyms, output = Path(source), Path(synonyms), Path(output)
    if not source.is_file() or not synonyms.is_file():
        raise ValueError("Source SQL gzip and combined synonym index must exist")
    allowed_lemmas = _combined_lemmas(synonyms)
    connection = _open_database()
    database_path = Path(connection.execute("PRAGMA database_list").fetchone()[2])
    try:
        result = _parse_source(source, allowed_lemmas, connection)
        source_lemmas, record_rows = _make_records(connection)
        result.update(_write_binary(connection, output, compressed))
        result.update({"format": "EDSYNF02" if compressed else "EDSYNF01", "source_sql_gz": str(source),
                       "synonyms_v1": str(synonyms), "output": str(output),
                       "combined_lemma_tokens": len(allowed_lemmas),
                       "source_lemmas_with_forms": source_lemmas,
                       "deduplicated_record_rows": record_rows,
                       "elapsed_seconds": round(time.monotonic() - started, 3)})
    finally:
        connection.close()
        database_path.unlink(missing_ok=True)
    manifest_path = Path(manifest) if manifest is not None else output.with_suffix(".manifest.json")
    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    temporary_manifest = manifest_path.with_name(manifest_path.name + ".building")
    temporary_manifest.write_text(json.dumps(result, ensure_ascii=False, indent=2, sort_keys=True) + "\n", encoding="utf8")
    temporary_manifest.replace(manifest_path)
    result["manifest"] = str(manifest_path)
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", required=True, help="verified plWordNet SQL gzip")
    parser.add_argument("--synonyms", required=True, help="combined synonym v1 text index")
    parser.add_argument("--output", required=True, help="EDSYNF01 output binary")
    parser.add_argument("--manifest", help="JSON manifest path (default: output .manifest.json)")
    parser.add_argument("--compressed", action="store_true", help="compress each value block (EDSYNF02), retaining random access")
    arguments = parser.parse_args()
    print(json.dumps(build_index(arguments.source, arguments.synonyms, arguments.output, arguments.manifest, arguments.compressed), ensure_ascii=False, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
