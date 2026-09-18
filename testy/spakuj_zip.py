#!/usr/bin/env python3
"""Build the fallback ZIP from the SAME staged inputs as Inno Setup.

Run after zbuduj.sh: python3 testy/spakuj_zip.py 5.0.114
Never copy a user's installation/profile; exclude temp/build/test outputs.
"""
from pathlib import Path, PurePosixPath
import fnmatch
import hashlib
import re
import sys
import zipfile

version = sys.argv[1]
if not re.fullmatch(r"\d+\.\d+\.\d+", version):
    raise SystemExit("Expected a three-part release version")
root = Path(__file__).resolve().parents[1]
stage = Path('/mnt/c/EdSharp')
manifest = (stage / 'EdSharp_Setup.iss').read_text(encoding='utf-8')
if f'AppVersion={version}' not in manifest:
    raise SystemExit('Staging belongs to another version')
files = {}
active = False
for raw in manifest.splitlines():
    line = raw.strip()
    if line.startswith('['):
        active = line == '[Files]'
        continue
    if not active or not line or line.startswith(';'):
        continue
    source = re.search(r'Source:\s*"([^"]+)"', line)
    dest = re.search(r'DestDir:\s*"([^"]+)"', line)
    if not source or not dest or not dest[1].startswith('{app}'):
        raise SystemExit('Unsupported installer file entry: ' + line)
    src = source[1].replace('\\', '/')
    dest_dir = dest[1].replace('{app}', '').replace('\\', '/').strip('/')
    excludes = re.search(r'Excludes:\s*"([^"]+)"', line)
    patterns = excludes[1].split(',') if excludes else []
    if '*' in src:
        if not src.endswith('/*'):
            raise SystemExit('Unsupported wildcard: ' + src)
        base = stage / src[:-2]
        sources = sorted(base.rglob('*')) if base.exists() else []
    else:
        base = stage
        sources = [stage / src]
    present = False
    for path in sources:
        if not path.is_file():
            continue
        if any(fnmatch.fnmatch(path.name.lower(), p.lower()) for p in patterns):
            continue
        present = True
        rel = path.relative_to(base).as_posix() if '*' in src else path.name
        name = str(PurePosixPath('EdSharpNG', dest_dir, rel))
        if name in files:
            raise SystemExit('Duplicate archive entry: ' + name)
        files[name] = path
    if not present and 'skipifsourcedoesntexist' not in line:
        raise SystemExit('Missing required file: ' + src)
for required in ['EdSharpNG.exe', 'EdSharpNG.exe.config', 'EdSharp.ini', 'Tektosyne.dll', 'OriginalFormatSave.cs']:
    assert 'EdSharpNG/' + required in files, required
zip_path = root / 'dist' / f'EdSharpNG_{version}.zip'
with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    for name, path in sorted(files.items()):
        z.write(path, name)
with zipfile.ZipFile(zip_path) as z:
    assert z.testzip() is None
    assert z.read('EdSharpNG/EdSharpNG.exe') == (root / 'EdSharpNG.exe').read_bytes()
print(f'{zip_path}: {len(files)} files, {zip_path.stat().st_size} bytes')
print('SHA256 ' + hashlib.sha256(zip_path.read_bytes()).hexdigest().upper())
