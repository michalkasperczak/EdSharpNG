#!/usr/bin/env python3
"""Read the selected fields of a plWordNet MySQL dump without executing SQL.

This is build-time tooling only. SQL binary UUIDs are bytes, not UTF-8 text.
"""
import re

_VALUE = re.compile(
    rb"\s*(?:(?:_binary\s+|[bB])?'((?:[^'\\]|\\.|'')*)'|(NULL)|(-?\d+))\s*([,)])",
    re.DOTALL,
)
_ESCAPE = re.compile(rb"\\(.)|''", re.DOTALL)
_ESCAPES = {b'0': b'\0', b'b': b'\b', b'n': b'\n', b'r': b'\r',
            b't': b'\t', b'Z': b'\x1a'}


def _unescape(match: re.Match[bytes]) -> bytes:
    value = match.group(1)
    if value is None:
        return b"'"
    return _ESCAPES.get(value, value)


def parse_values(payload):
    """Yield tuples from the VALUES part of one extended INSERT statement."""
    pos = 0
    length = len(payload)
    while pos < length:
        while pos < length and payload[pos] in b' \t\r\n':
            pos += 1
        if pos >= length or payload[pos] != ord('('):
            raise ValueError('Expected a SQL row at byte ' + str(pos))
        pos += 1
        row = []
        while True:
            match = _VALUE.match(payload, pos)
            if match is None:
                raise ValueError('Unsupported or incomplete SQL value at byte ' + str(pos))
            if match.group(1) is not None:
                value = _ESCAPE.sub(_unescape, match.group(1))
            elif match.group(2) is not None:
                value = None
            else:
                value = int(match.group(3))
            row.append(value)
            pos = match.end()
            if match.group(4) == b')':
                break
        yield tuple(row)
        while pos < length and payload[pos] in b' \t\r\n':
            pos += 1
        if pos < length and payload[pos] == ord(','):
            pos += 1
            continue
        if pos < length and payload[pos] == ord(';') and not payload[pos+1:].strip():
            return
        raise ValueError('Expected row separator or statement end at byte ' + str(pos))
    raise ValueError('Incomplete SQL VALUES statement')


_CREATE = re.compile(rb'^CREATE TABLE `([^`]+)` \(')
_COLUMN = re.compile(rb'^\s+`([^`]+)`\s')
_INSERT = re.compile(rb'^INSERT INTO `([^`]+)` VALUES ')


def iter_table_rows(source, tables):
    """Inspect only selected live tables, excluding audit/history and SQL code."""
    schemas = {}
    current = None
    for line in source:
        match = _CREATE.match(line)
        if match:
            name = match.group(1).decode('ascii')
            current = name if name in tables else None
            if current is not None:
                schemas[current] = []
            continue
        if current is not None:
            column = _COLUMN.match(line)
            if column:
                schemas[current].append(column.group(1).decode('ascii'))
            if line.startswith(b') ENGINE='):
                current = None
            continue
        match = _INSERT.match(line)
        if match is None:
            continue
        name = match.group(1).decode('ascii')
        if name not in tables:
            continue
        if name not in schemas:
            raise ValueError('Missing table schema: ' + name)
        columns = schemas[name]
        for values in parse_values(line[match.end():]):
            if len(values) != len(columns):
                raise ValueError('Unexpected column count in ' + name)
            yield name, dict(zip(columns, values))


def _text(value):
    if not isinstance(value, bytes):
        raise ValueError('Expected UTF-8 text field')
    return value.decode('utf8', errors='strict')


def _uuid(value):
    if not isinstance(value, bytes) or len(value) != 16:
        raise ValueError('Expected a 16-byte binary identifier')
    return value


def _single_word(value):
    word = _text(value).strip().lower()
    if not word or '|' in word or any(char.isspace() for char in word):
        return None
    return word


def build_lemma_index(source, output):
    """Export main Polish synsets to the existing four-column runtime format."""
    from collections import defaultdict
    from pathlib import Path

    tables = {'tbl_lexicon', 'tbl_application_localised_string',
              'tbl_part_of_speech', 'tbl_word', 'tbl_sense'}
    lexicons, names, parts, words, senses = {}, {}, {}, {}, []
    for table, row in iter_table_rows(source, tables):
        if table == 'tbl_lexicon':
            lexicons[row['id']] = (_text(row['identifier']),
                                   _text(row['language_shortcut']) if row['language_shortcut'] else '',
                                   _text(row['lexicon_version']))
        elif table == 'tbl_application_localised_string':
            if row['language'] == b'en' and row['value'] in (b'noun', b'verb', b'adjective', b'adverb'):
                names[row['id']] = {b'noun': 'n', b'verb': 'v', b'adjective': 'a', b'adverb': 'r'}[row['value']]
        elif table == 'tbl_part_of_speech':
            parts[row['id']] = row['name_id']
        elif table == 'tbl_word':
            words[_uuid(row['id'])] = _single_word(row['word'])
        elif table == 'tbl_sense':
            if row['synset_id'] is not None and row['word_id'] is not None:
                senses.append((row['lexicon_id'], row['part_of_speech_id'],
                               row['synset_position'] if row['synset_position'] is not None else 0,
                               _uuid(row['synset_id']), _uuid(row['word_id'])))

    polish = [key for key, value in lexicons.items() if value[:2] == ('plWN', 'pl')]
    if len(polish) != 1 or lexicons[polish[0]][2] != '5.0':
        raise ValueError('Expected exactly one main Polish plWordNet 5.0 lexicon')
    lexicon = polish[0]
    by_synset = defaultdict(list)
    for lex, part_id, position, synset, word_id in senses:
        if lex != lexicon:
            continue
        if part_id not in parts or parts[part_id] not in names:
            raise ValueError('Unknown Polish part of speech')
        if word_id not in words:
            raise ValueError('Missing referenced Polish word')
        word = words[word_id]
        if word is not None:
            by_synset[(synset, names[parts[part_id]])].append((position, word))

    by_lemma = defaultdict(list)
    synonym_groups = 0
    for (synset, part), members in sorted(by_synset.items()):
        ordered = list(dict.fromkeys(word for position, word in sorted(members)))
        if len(ordered) < 2:
            continue
        synonym_groups += 1
        for word in ordered:
            synonyms = [other for other in ordered if other != word]
            by_lemma[word].append((part, synset.hex(), synonyms))

    output = Path(output)
    output.parent.mkdir(parents=True, exist_ok=True)
    temporary = output.with_name(output.name + '.building')
    records = 0
    with temporary.open('w', encoding='utf8', newline='\n') as dest:
        dest.write('# EdSharpNG synonyms v1; source plWordNet 5.0; Polish plWN only\n')
        for word, groups in sorted(by_lemma.items()):
            for part, sense, synonyms in sorted(groups):
                dest.write('\t'.join((word, part, sense, '|'.join(synonyms))) + '\n')
                records += 1
    temporary.replace(output)
    return {'source_version': lexicons[lexicon][2], 'polish_lexicon_id': lexicon,
            'lemmas_with_synonyms': len(by_lemma), 'records': records,
            'synsets_with_synonyms': synonym_groups,
            'polish_single_word_synsets': len(by_synset)}


def _runtime_rows(path):
    """Validate a v1 input before using it as the older-source fallback."""
    with open(path, encoding='utf8', errors='strict') as source:
        for number, line in enumerate(source, 1):
            line = line.rstrip('\r\n')
            if not line or line.startswith('#'):
                continue
            fields = line.split('\t')
            if len(fields) != 4 or fields[1] not in ('n', 'v', 'a', 'r') or not fields[2]:
                raise ValueError('Invalid v1 synonym index at line ' + str(number))
            word = _single_word(fields[0].encode('utf8'))
            synonyms = list(dict.fromkeys(_single_word(value.encode('utf8'))
                                         for value in fields[3].split('|')))
            synonyms = [value for value in synonyms if value and value != word]
            if not word or not synonyms:
                raise ValueError('Empty v1 synonym entry at line ' + str(number))
            yield word, fields[1], fields[2], '|'.join(synonyms)


def add_legacy_fallback(primary, legacy):
    """Keep older entries only where 5.0 has no synonyms; never relabel as 5.0."""
    from pathlib import Path

    primary = Path(primary)
    rows = list(_runtime_rows(primary))
    current_words = {row[0] for row in rows}
    fallback = [row for row in _runtime_rows(legacy) if row[0] not in current_words]
    fallback_words = {row[0] for row in fallback}
    for word, part, sense, synonyms in fallback:
        rows.append((word, part, 'omw-2.0:' + sense, synonyms))
    temporary = primary.with_name(primary.name + '.building')
    with temporary.open('w', encoding='utf8', newline='\n') as output:
        output.write('# EdSharpNG synonyms v1; plWordNet 5.0; missing-lemma fallback OMW/plWordNet 2.0\n')
        for row in sorted(rows):
            output.write('\t'.join(row) + '\n')
    temporary.replace(primary)
    return {'legacy_fallback_lemmas': len(fallback_words),
            'legacy_fallback_records': len(fallback),
            'lemmas_with_synonyms': len(current_words | fallback_words),
            'records': len(rows)}
