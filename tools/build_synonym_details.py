#!/usr/bin/env python3
"""Build source-derived semantic details; SQL is parsed, never executed."""
from __future__ import annotations
import argparse
import gzip
import hashlib
import json
import re
import sys
from pathlib import Path
TOOLS_DIRECTORY = str(Path(__file__).resolve().parent)
if TOOLS_DIRECTORY not in sys.path:
    sys.path.insert(0, TOOLS_DIRECTORY)
from build_synonyms_plwordnet import _text, _uuid, iter_table_rows
_TABLES = {'tbl_synset_attributes', 'tbl_synset_examples', 'tbl_sense_attributes', 'tbl_sense_examples'}
_HEX_ID = re.compile(r'^[0-9a-fA-F]{32}$')
_WHITESPACE = re.compile(r'\s+')

def _file_metadata(path: Path) -> dict:
    digest = hashlib.sha256()
    with path.open('rb') as handle:
        for block in iter(lambda: handle.read(1024*1024), b''):
            digest.update(block)
    return {'path': str(path), 'bytes': path.stat().st_size, 'sha256': digest.hexdigest()}

def _normalise(value: str) -> str:
    value = _WHITESPACE.sub(' ', value).strip()
    if value.lower() in {'brak danych', 'brak definicji', '-', '--', '???', 'bd', 'b.d.'}:
        return ''
    if any(ord(c) < 32 for c in value):
        raise ValueError('Control character in source detail')
    return value

def _current_synsets(synonyms: Path) -> set[bytes]:
    selected = set()
    with synonyms.open('r', encoding='utf8', errors='strict') as handle:
        for number, line in enumerate(handle, 1):
            line = line.rstrip('\r\n')
            if not line or line.startswith('#'): continue
            fields = line.split('\t')
            if len(fields) != 4 or fields[1] not in {'n','v','a','r'}:
                raise ValueError('Invalid v1 synonym index at line '+str(number))
            synset = fields[2]
            if synset.startswith('omw-2.0:'): continue
            if not _HEX_ID.fullmatch(synset):
                raise ValueError('Invalid current synset identifier at line '+str(number))
            selected.add(bytes.fromhex(synset))
    if not selected: raise ValueError('Synonym index has no current plWordNet synset identifiers')
    return selected

def build_details(source, synonyms, output, manifest=None):
    source, synonyms, output = Path(source), Path(synonyms), Path(output)
    if not source.is_file() or not synonyms.is_file():
        raise ValueError('Source SQL gzip and synonym v1 index must exist')
    selected = _current_synsets(synonyms)
    # Pass 1: exact live sense->synset join. Export tables are in name order.
    lexicons, senses, words = {}, {}, {}
    wanted_words = set()
    with gzip.open(source, 'rb') as handle:
        for table, row in iter_table_rows(handle, {'tbl_lexicon','tbl_sense','tbl_word'}):
            if table == 'tbl_lexicon':
                lexicons[row['id']] = tuple(_text(row[k]) if row[k] is not None else '' for k in ('identifier','language_shortcut','lexicon_version'))
            elif table == 'tbl_sense':
                if row['synset_id'] is None or row['word_id'] is None: continue
                synset = _uuid(row['synset_id'])
                if synset in selected:
                    word = _uuid(row['word_id'])
                    senses[_uuid(row['id'])] = (row['lexicon_id'], synset, word)
                    wanted_words.add(word)
            else:
                word = _uuid(row['id'])
                if word in wanted_words:
                    words[word] = _normalise(_text(row['word']))
    polish = [k for k,v in lexicons.items() if v == ('plWN','pl','5.0')]
    if senses and len(polish) != 1:
        raise ValueError('Expected exactly one main Polish plWordNet 5.0 lexicon')
    senses = {k:v for k,v in senses.items() if polish and v[0] == polish[0]}
    if any(v[2] not in words for v in senses.values()):
        raise ValueError('Missing source word for Polish sense')
    definitions, examples, sense_definitions, sense_examples = {}, {}, {}, {}
    # Pass 2: only live definition/example columns; no comments, users, AUD.
    with gzip.open(source, 'rb') as handle:
        for table, row in iter_table_rows(handle, _TABLES):
            if table == 'tbl_synset_attributes':
                synset = _uuid(row['synset_id'])
                text = _normalise(_text(row['definition'])) if row['definition'] is not None else ''
                if synset in selected and text: definitions[synset] = text
            elif table == 'tbl_synset_examples':
                eid = _uuid(row['id'])
                synset = _uuid(row['synset_attribute_id']) if row['synset_attribute_id'] is not None else None
                text = _normalise(_text(row['example'])) if row['example'] is not None else ''
                if synset in selected and text:
                    if synset not in examples or eid < examples[synset][0]: examples[synset] = (eid,text)
            elif table == 'tbl_sense_attributes':
                sense = _uuid(row['sense_id'])
                text = _normalise(_text(row['definition'])) if row['definition'] is not None else ''
                if sense in senses and text: sense_definitions[sense] = text
            else:
                eid = _uuid(row['id'])
                sense = _uuid(row['sense_attribute_id']) if row['sense_attribute_id'] is not None else None
                text = _normalise(_text(row['example'])) if row['example'] is not None else ''
                if sense in senses and text:
                    if sense not in sense_examples or eid < sense_examples[sense][0]: sense_examples[sense] = (eid,text)
    fallback = {}
    for sense, (_,synset,word) in senses.items():
        if sense not in sense_definitions and sense not in sense_examples: continue
        candidate = (sense not in sense_definitions, words[word], sense)
        if synset not in fallback or candidate < fallback[synset]: fallback[synset] = candidate
    output.parent.mkdir(parents=True, exist_ok=True)
    temporary = output.with_name(output.name+'.building')
    rows = definition_rows = example_rows = unit_rows = 0
    with temporary.open('w',encoding='utf8',newline='\n') as handle:
        for synset in sorted(selected):
            definition = definitions.get(synset,'')
            example = examples.get(synset,(b'',''))[1]
            if synset in fallback:
                _,lemma,sense = fallback[synset]
                label = 'Jednostka „'+lemma+'”: '
                if not definition and sense in sense_definitions:
                    definition = label+sense_definitions[sense]
                    unit_rows += 1
                if not example and sense in sense_examples: example = label+sense_examples[sense][1]
            if not definition and not example: continue
            if max(len(definition),len(example)) > 30000: raise ValueError('Source detail exceeds runtime limit')
            handle.write(synset.hex()+'\t'+definition+'\t'+example+'\n')
            rows += 1; definition_rows += bool(definition); example_rows += bool(example)
    temporary.replace(output)
    result = {'source':_file_metadata(source),'input':_file_metadata(synonyms),'output':_file_metadata(output),
              'synsets_in_current_index':len(selected),'details_rows':rows,'definitions':definition_rows,
              'examples':example_rows,'unit_definition_rows':unit_rows,'coverage_percent':round(100*rows/len(selected),3)}
    manifest_path = Path(manifest) if manifest is not None else output.with_suffix('.manifest.json')
    manifest_path.parent.mkdir(parents=True,exist_ok=True)
    temporary_manifest = manifest_path.with_name(manifest_path.name+'.building')
    temporary_manifest.write_text(json.dumps(result,ensure_ascii=False,indent=2,sort_keys=True)+'\n',encoding='utf8')
    temporary_manifest.replace(manifest_path)
    result['manifest']=str(manifest_path)
    return result

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source',required=True)
    parser.add_argument('--synonyms',required=True)
    parser.add_argument('--output',required=True)
    parser.add_argument('--manifest')
    args=parser.parse_args()
    print(json.dumps(build_details(args.source,args.synonyms,args.output,args.manifest),ensure_ascii=False,sort_keys=True))
    return 0
if __name__=='__main__': raise SystemExit(main())
