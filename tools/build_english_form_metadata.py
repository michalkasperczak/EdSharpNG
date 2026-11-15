#!/usr/bin/env python3
"""Generate optional English form-order metadata from pinned AGID infl.txt.

The result enriches only existing Princeton WordNet exception triples.  It is
build-time tooling: EdSharp does not load Python or AGID at runtime.
"""
import argparse
import hashlib
import json
import pathlib
import re

AGID_VERSION = "2016.01.19"
AGID_COMMIT = "b22230cc5250887737fdefe9ca4c9d9d01230eaa"
AGID_SOURCE_URL = "https://raw.githubusercontent.com/en-wl/wordlist/" + AGID_COMMIT + "/agid/infl.txt"
SOURCE_ID = "AGID" + AGID_VERSION
WORD = r"[A-Za-z']+"
EXCEPTION_WORD = r"[A-Za-z][A-Za-z'-]*"
AGID_LINE = re.compile(r"^(%s) ([VNA])(\?)?: (.*)$" % WORD)
INDIVIDUAL = re.compile(r"^(%s)([~<!?]*)(?: ([0-9]+(?:\.[0-9]+)?))?(?: \{([^{}]+)\})?$" % WORD)
KNOWN = {
    "n": {"NNS"},
    "v": {"VB", "VBP", "VBZ", "VBD", "VBN", "VBG"},
    "a": {"JJR", "JJS"},
    "r": {"RBR", "RBS"},
}


def digest(path):
    data = pathlib.Path(path).read_bytes()
    return {"sha256": hashlib.sha256(data).hexdigest(), "bytes": len(data)}


def exception_rows(path):
    rows = set()
    for line_number, raw in enumerate(pathlib.Path(path).read_text(encoding="utf-8").splitlines(), 1):
        if not raw or raw.startswith("#"):
            continue
        fields = raw.split("\t")
        if len(fields) != 3 or not all(fields):
            raise ValueError("malformed exception row at line %d" % line_number)
        form, pos, lemma = fields
        if pos not in KNOWN or not re.fullmatch(EXCEPTION_WORD, form) or not re.fullmatch(EXCEPTION_WORD, lemma):
            raise ValueError("unsafe exception row at line %d" % line_number)
        rows.add((form, pos, lemma))
    return rows


def parse_individual(value):
    match = INDIVIDUAL.fullmatch(value)
    if not match:
        raise ValueError("malformed AGID individual entry: " + value)
    word, marker, variant, qualifier = match.groups()
    if marker or qualifier or (variant is not None and float(variant) >= 2):
        return None
    return word


def parse_groups(payload):
    groups = []
    for group in payload.split(" | "):
        if not group:
            raise ValueError("empty AGID inflection group")
        words = []
        for value in group.split(", "):
            word = parse_individual(value)
            if word is not None:
                words.append(word)
        groups.append(words)
    return groups


def slot_tags(pos, group_count):
    if pos == "N":
        return ["NNS"] * group_count
    if pos == "A":
        if group_count > 2:
            return None
        return ["JJR", "JJS"][:group_count]
    # The README assigns eight explicit slots to be.  Other verb layouts are
    # reliable only for the documented three- or four-group patterns.
    if group_count == 3:
        return ["VBD|VBN", "VBG", "VBZ"]
    if group_count == 4:
        return ["VBD", "VBN", "VBG", "VBZ"]
    return None


def add_record(candidates, exception_set, lemma, agid_pos, line_number, payload):
    groups = parse_groups(payload)
    if agid_pos == "V" and lemma == "be":
        if len(groups) != 8:
            return False
        tags = ["VBD", "VBD", "VBN", "VBG", "VBP", "VBP", "VBZ", "VBP"]
    else:
        tags = slot_tags(agid_pos, len(groups))
        if tags is None:
            return False
    for words, tagset in zip(groups, tags):
        for word in words:
            candidate_tags = tagset.split("|")
            target_pos = ("v" if agid_pos == "V" else "n" if agid_pos == "N" else None)
            target_positions = [target_pos] if target_pos else ["a", "r"]
            for pos in target_positions:
                key = (word, pos, lemma)
                if key not in exception_set:
                    continue
                for tag in candidate_tags:
                    # AGID merges adjective and adverb under A.  Its slot is
                    # still source-attested, but Penn uses a separate tag pair
                    # for an existing WordNet adverb exception.
                    target_tag = tag
                    if pos == "r" and tag == "JJR":
                        target_tag = "RBR"
                    elif pos == "r" and tag == "JJS":
                        target_tag = "RBS"
                    candidates.setdefault(key, {}).setdefault(target_tag, set()).add(line_number)
    return True


def build(agid_path, exceptions_path):
    exceptions = exception_rows(exceptions_path)
    candidates = {}
    parsed_records = 0
    skipped_uncertain_pos = 0
    skipped_layout = 0
    for line_number, raw in enumerate(pathlib.Path(agid_path).read_text(encoding="utf-8").splitlines(), 1):
        if not raw:
            continue
        match = AGID_LINE.fullmatch(raw)
        if not match:
            raise ValueError("malformed AGID line %d" % line_number)
        lemma, agid_pos, uncertain, payload = match.groups()
        if uncertain:
            skipped_uncertain_pos += 1
            continue
        parsed_records += 1
        if not add_record(candidates, exceptions, lemma, agid_pos, line_number, payload):
            skipped_layout += 1
    rows = []
    for key in sorted(candidates):
        tag_map = candidates[key]
        tags = sorted(tag_map)
        lines = sorted({line for line_set in tag_map.values() for line in line_set})
        rows.append(key + ("|".join(tags), SOURCE_ID + ":" + ",".join(str(line) for line in lines)))
    matched = {row[:3] for row in rows}
    return rows, {
        "exception_rows": len(exceptions),
        "metadata_rows": len(rows),
        "unmatched_exception_rows": len(exceptions - matched),
        "ambiguous_candidate_rows": sum("|" in row[3] for row in rows),
        "parsed_agid_records": parsed_records,
        "skipped_uncertain_pos_records": skipped_uncertain_pos,
        "skipped_unknown_layout_records": skipped_layout,
    }


def write_rows(path, rows):
    output = pathlib.Path(path)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text("".join("\t".join(row) + "\n" for row in rows), encoding="utf-8", newline="\n")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--agid", required=True)
    parser.add_argument("--exceptions", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--manifest", required=True)
    parser.add_argument("--additional-resource", action="append", default=[])
    arguments = parser.parse_args()
    rows, counts = build(arguments.agid, arguments.exceptions)
    write_rows(arguments.output, rows)
    resources = {"metadata": digest(arguments.output)}
    for resource in arguments.additional_resource:
        name = pathlib.Path(resource).name
        if not name or name in resources:
            raise ValueError("duplicate manifest resource: " + resource)
        resources[name] = digest(resource)
    manifest = {
        "generator": "tools/build_english_form_metadata.py",
        "agid": {"version": AGID_VERSION, "source_id": SOURCE_ID, "commit": AGID_COMMIT, "source_url": AGID_SOURCE_URL, **digest(arguments.agid)},
        "wordnet_exceptions": digest(arguments.exceptions),
        "outputs": resources,
        "counts": counts,
    }
    destination = pathlib.Path(arguments.manifest)
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(manifest, ensure_ascii=False, indent=2, sort_keys=True) + "\n", encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
