#!/usr/bin/env python3
"""Buduje kompaktowy indeks EdSharpNG z eksportu OMW/plWordNet XML.

To narzedzie jest wylacznie etapem budowania danych. EdSharpNG odczytuje wynik
bez Pythona, SQL, sieci ani Microsoft Worda.
"""
import argparse
from collections import defaultdict
from pathlib import Path
import xml.etree.ElementTree as ET


def local_name(tag):
    return tag.rsplit("}", 1)[-1]


def normalise(word):
    return word.strip().lower()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("input_xml", type=Path)
    parser.add_argument("output_index", type=Path)
    args = parser.parse_args()

    synsets = defaultdict(list)
    root = ET.parse(args.input_xml).getroot()
    for entry in root.iter():
        if local_name(entry.tag) != "LexicalEntry":
            continue
        lemma = None
        pos = ""
        sense_ids = []
        for child in entry:
            name = local_name(child.tag)
            if name == "Lemma":
                lemma = normalise(child.attrib.get("writtenForm", ""))
                pos = child.attrib.get("partOfSpeech", "").strip()
            elif name == "Sense" and child.attrib.get("synset"):
                sense_ids.append(child.attrib["synset"].strip())
        if not lemma or not pos:
            continue
        for sense_id in sense_ids:
            synsets[(sense_id, pos)].append(lemma)

    words = defaultdict(list)
    for (sense_id, pos), lemmas in synsets.items():
        unique_lemmas = []
        seen = set()
        for lemma in lemmas:
            # Shift+F7 currently replaces one selected word.  Skip multiword
            # units until the UI has an explicit phrase-selection contract.
            if any(char.isspace() for char in lemma):
                continue
            if lemma not in seen:
                seen.add(lemma)
                unique_lemmas.append(lemma)
        if len(unique_lemmas) < 2:
            continue
        for lemma in unique_lemmas:
            synonyms = [other for other in unique_lemmas if other != lemma]
            if synonyms:
                words[lemma].append((pos, sense_id, synonyms))

    lines = [
        "# EdSharpNG Polish thesaurus index v1",
        "# Source: Open Multilingual Wordnet plWordNet export 2.0; transformed for local read-only lookup.",
        "# Each line: lemma<TAB>part of speech<TAB>synset id<TAB>synonym|synonym",
    ]
    groups = 0
    for lemma in sorted(words):
        entries = sorted(words[lemma], key=lambda item: (item[0], item[1]))
        for pos, sense_id, synonyms in entries:
            lines.append("\t".join([lemma, pos, sense_id, "|".join(synonyms)]))
            groups += 1
    args.output_index.parent.mkdir(parents=True, exist_ok=True)
    args.output_index.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("lemmas=" + str(len(words)))
    print("groups=" + str(groups))
    print("output=" + str(args.output_index))


if __name__ == "__main__":
    main()
