#!/usr/bin/env python3
"""Build offline English synonym resources from a Princeton WordNet 3.0 zip."""

import argparse
import hashlib
import json
import pathlib
import re
import zipfile


DATA_MEMBERS = {"n": "data.noun", "v": "data.verb", "a": "data.adj", "r": "data.adv"}
EXCEPTION_MEMBERS = {"n": "noun.exc", "v": "verb.exc", "a": "adj.exc", "r": "adv.exc"}
RECORD = re.compile(r"^(\d{8})\s+\d{2}\s+([nvars])\s+([0-9a-fA-F]{2})\s+(.*)$")
ADJECTIVE_MARKER = re.compile(r"\((?:a|p|ip)\)$")
EXAMPLE_SEGMENT = re.compile(r'^"([^\"]+)"(\s*-\s*.+)?$')
SOURCE_URL = "https://raw.githubusercontent.com/nltk/nltk_data/gh-pages/packages/corpora/wordnet.zip"
MAX_MEMBER_BYTES = 32 * 1024 * 1024


def _invalid_text(value):
    return any(character == "|" or ord(character) < 32 for character in value)


def _normalize_word(word, pos):
    return ADJECTIVE_MARKER.sub("", word) if pos == "a" else word


def _safe_archive(archive):
    total_size = 0
    for info in archive.infolist():
        if info.filename.startswith(("/", "\\")) or ".." in pathlib.PurePosixPath(info.filename).parts:
            raise ValueError("unsafe archive member: " + info.filename)
        if info.file_size > MAX_MEMBER_BYTES:
            raise ValueError("archive member exceeds size bound: " + info.filename)
        total_size += info.file_size
    if total_size > MAX_MEMBER_BYTES * 4:
        raise ValueError("archive exceeds size bound")


def _read_member(archive, name):
    try:
        raw = archive.read("wordnet/" + name)
    except KeyError as error:
        raise ValueError("missing required archive member: " + name) from error
    if len(raw) > MAX_MEMBER_BYTES:
        raise ValueError("archive member exceeds size bound: " + name)
    try:
        return raw.decode("utf-8", "strict")
    except UnicodeDecodeError as error:
        raise ValueError("invalid UTF-8 in " + name) from error


def _parse_record(member, line_number, line, requested_pos):
    match = RECORD.match(line)
    if not match:
        raise ValueError(f"malformed {member}:{line_number}")
    offset, source_pos, word_count_hex, tail = match.groups()
    pos = "a" if source_pos == "s" else source_pos
    if pos != requested_pos:
        raise ValueError(f"unexpected POS in {member}:{line_number}")
    syntax, separator, gloss = tail.partition("|")
    if not separator:
        raise ValueError(f"malformed {member}:{line_number}")
    if _invalid_text(gloss):
        raise ValueError(f"unsafe detail in {member}:{line_number}")
    fields = syntax.split()
    word_count = int(word_count_hex, 16)
    cursor = word_count * 2
    if len(fields) <= cursor:
        raise ValueError(f"malformed {member}:{line_number}")
    words = []
    for index in range(0, cursor, 2):
        word = fields[index]
        if _invalid_text(word):
            raise ValueError(f"unsafe lemma in {member}:{line_number}")
        words.append(_normalize_word(word, pos))
    try:
        pointer_count = int(fields[cursor])
    except ValueError as error:
        raise ValueError(f"malformed {member}:{line_number}") from error
    cursor += 1 + pointer_count * 4
    if len(fields) < cursor:
        raise ValueError(f"malformed {member}:{line_number}")
    if source_pos == "v":
        if len(fields) <= cursor:
            raise ValueError(f"malformed {member}:{line_number}")
        try:
            frame_count = int(fields[cursor])
        except ValueError as error:
            raise ValueError(f"malformed {member}:{line_number}") from error
        cursor += 1 + frame_count * 3
    if len(fields) != cursor:
        raise ValueError(f"malformed {member}:{line_number}")
    definition, example = _split_gloss(gloss)
    return {"id": f"wn30:{pos}:{offset}", "pos": pos, "words": words,
            "definition": definition, "example": example}


def _split_gloss(gloss):
    segments = []
    current = []
    in_quote = False
    for character in gloss:
        if character == '"':
            in_quote = not in_quote
        if character == ";" and not in_quote:
            segments.append("".join(current))
            current = []
        else:
            current.append(character)
    if in_quote:
        return (" ".join(gloss.split()), "")
    segments.append("".join(current))

    definition_segments = []
    example = ""
    for segment in segments:
        text = segment.strip()
        if not text or text == '""':
            continue
        match = EXAMPLE_SEGMENT.fullmatch(text)
        if match:
            if not example:
                example = " ".join((match.group(1) + (match.group(2) or "")).split())
        else:
            definition_segments.append(text)
    return ("; ".join(" ".join(segment.split()) for segment in definition_segments), example)


def _rows(archive):
    rows = []
    seen_ids = set()
    for requested_pos, member in DATA_MEMBERS.items():
        for line_number, line in enumerate(_read_member(archive, member).splitlines(), 1):
            if not line:
                continue
            if line.startswith("  "):
                if re.match(r"\d{8}\s", line.lstrip()):
                    raise ValueError(f"malformed {member}:{line_number}")
                continue
            row = _parse_record(member, line_number, line, requested_pos)
            if row["id"] in seen_ids:
                raise ValueError("duplicate sense ID: " + row["id"])
            seen_ids.add(row["id"])
            rows.append(row)
    return rows


def _index(rows):
    entries = []
    usable_ids = set()
    lemma_pos_pairs = set()
    lemma_names = set()
    for row in rows:
        replacements = []
        seen_replacements = set()
        for word in row["words"]:
            replacement = word.replace("_", " ")
            key = replacement.casefold()
            if key not in seen_replacements:
                seen_replacements.add(key)
                replacements.append(replacement)
        for word in row["words"]:
            lemma = word.replace("_", " ")
            if " " in lemma:
                continue
            synonyms = []
            seen_synonyms = set()
            for replacement in replacements:
                key = replacement.casefold()
                if key != lemma.casefold() and key not in seen_synonyms:
                    seen_synonyms.add(key)
                    synonyms.append(replacement)
            if synonyms:
                entries.append((lemma, row["pos"], row["id"], "|".join(synonyms)))
                usable_ids.add(row["id"])
                lemma_pos_pairs.add((lemma.lower(), row["pos"]))
                lemma_names.add(lemma.lower())
    entries.sort(key=lambda entry: (entry[0].casefold(), entry[1], entry[2]))
    return entries, usable_ids, lemma_pos_pairs, lemma_names


def _exceptions(archive, lemma_keys):
    entries = set()
    for pos, member in EXCEPTION_MEMBERS.items():
        for line_number, line in enumerate(_read_member(archive, member).splitlines(), 1):
            if not line:
                continue
            fields = line.split()
            if len(fields) < 2 or any(_invalid_text(field) for field in fields):
                raise ValueError(f"malformed {member}:{line_number}")
            observed = _normalize_word(fields[0].replace("_", " "), pos)
            for base in fields[1:]:
                base = _normalize_word(base.replace("_", " "), pos)
                if " " not in base and (base.lower(), pos) in lemma_keys:
                    entries.add((observed, pos, base))
    return sorted(entries, key=lambda entry: (entry[0].casefold(), entry[1], entry[2].casefold()))


def _write_text(path, rows):
    path.write_text("".join("\t".join(row) + "\n" for row in rows), encoding="utf-8", newline="\n")


def _digest(path):
    data = path.read_bytes()
    return {"sha256": hashlib.sha256(data).hexdigest(), "bytes": len(data)}


def build(source_zip, output_dir):
    """Build resources and return a manifest dictionary."""
    source = pathlib.Path(source_zip)
    destination = pathlib.Path(output_dir)
    with zipfile.ZipFile(source) as archive:
        _safe_archive(archive)
        rows = _rows(archive)
        index, usable_ids, lemma_pos_pairs, lemma_names = _index(rows)
        exceptions = _exceptions(archive, lemma_pos_pairs)
        license_bytes = archive.read("wordnet/LICENSE")
    destination.mkdir(parents=True, exist_ok=True)
    outputs = {
        "synonyms": destination / "wordnet-en-synonyms.txt",
        "details": destination / "wordnet-en-details.txt",
        "exceptions": destination / "wordnet-en-exceptions.txt",
        "license": destination / "LICENSE-WordNet.txt",
        "notice": destination / "NOTICE-WordNet.txt",
    }
    _write_text(outputs["synonyms"], index)
    detail_rows = [(row["id"], row["definition"], row["example"])
                   for row in rows if row["id"] in usable_ids]
    _write_text(outputs["details"], detail_rows)
    _write_text(outputs["exceptions"], exceptions)
    outputs["license"].write_bytes(license_bytes)
    outputs["notice"].write_text(
        "English WordNet data source: " + SOURCE_URL + "\n"
        "Original data: Princeton WordNet 3.0 (copyright 2006).\n"
        "This is the English source used upstream by EdSharp through NLTK; it is not a newer WordNet release and requires no runtime Python or NLTK.\n",
        encoding="utf-8", newline="\n")
    manifest = {
        "archive": {"path": str(source), "sha256": _digest(source)["sha256"], "bytes": source.stat().st_size},
        "outputs": {name: _digest(path) for name, path in outputs.items()},
        "counts": {
            "source_synsets": len(rows),
            "usable_synsets": len(usable_ids),
            "usable_lemmas": len(lemma_names),
            "lemma_pos_pairs": len(lemma_pos_pairs),
            "index_rows": len(index),
            "definitions": len(detail_rows),
            "examples": sum(bool(row[2]) for row in detail_rows),
            "exceptions": len(exceptions),
        },
    }
    return manifest


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", required=True)
    parser.add_argument("--output-dir", required=True)
    parser.add_argument("--manifest", required=True)
    arguments = parser.parse_args()
    manifest = build(arguments.source, arguments.output_dir)
    manifest_path = pathlib.Path(arguments.manifest)
    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
