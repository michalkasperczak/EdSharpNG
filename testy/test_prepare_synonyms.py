"""Exercise the actual Windows build-time unpacker using real gzip resources."""
import base64
import gzip
import hashlib
import json
from pathlib import Path
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "PrepareSynonyms.ps1"
POWERSHELL = "/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe"


class PrepareSynonymsTests(unittest.TestCase):
    def run_fixture(self, corrupt=False, preexisting=None):
        self.assertTrue(SCRIPT.is_file(), "Build-time synonym resource unpacker must exist")
        with tempfile.TemporaryDirectory() as directory:
            folder = Path(directory)
            data = folder / "Synonyms"
            data.mkdir()
            original = b"EDSYNF02" + ("poświadczone formy\n" * 100).encode("utf8")
            packed = gzip.compress(original, mtime=0)
            (data / "plwordnet-forms.bin.gz").write_bytes(packed)
            meta = {"schema": "edsharp-synonym-forms-v1", "binary_bytes": len(original), "binary_sha256": hashlib.sha256(original).hexdigest(), "packed_bytes": len(packed), "packed_sha256": hashlib.sha256(packed).hexdigest()}
            if corrupt:
                (data / "plwordnet-forms.bin.gz").write_bytes(b"X" + packed[1:])
            (data / "forms-resource.json").write_text(json.dumps(meta), encoding="utf8")
            if preexisting is not None:
                (data / "plwordnet-forms.bin").write_bytes(preexisting)
            win_folder = subprocess.check_output(["wslpath", "-w", str(folder)], text=True).strip()
            win_script = subprocess.check_output(["wslpath", "-w", str(SCRIPT)], text=True).strip()
            ps = "& '" + win_script.replace("'", "''") + "' -Root '" + win_folder.replace("'", "''") + "'; if (!$?) { exit 1 }"
            process = subprocess.run([POWERSHELL, "-NoProfile", "-ExecutionPolicy", "Bypass", "-EncodedCommand", base64.b64encode(ps.encode("utf-16le")).decode()], capture_output=True, timeout=30)
            self.last_output = (process.stdout + process.stderr).decode("utf8", errors="replace")
            output = (data / "plwordnet-forms.bin").read_bytes() if (data / "plwordnet-forms.bin").exists() else None
            leftovers = list(data.glob("*.building-*"))
            return process.returncode, output, original, leftovers

    def test_prepares_byte_identical_runtime_without_python_dependency(self):
        status, output, original, leftovers = self.run_fixture()
        self.assertEqual(status, 0, self.last_output)
        self.assertEqual(output, original)
        self.assertEqual(leftovers, [])

    def test_bad_resource_preserves_previous_runtime(self):
        previous = b"previous runtime bytes"
        status, output, original, leftovers = self.run_fixture(corrupt=True, preexisting=previous)
        self.assertNotEqual(status, 0)
        self.assertEqual(output, previous)
        self.assertEqual(leftovers, [])

    def test_replaces_old_runtime_from_verified_resource(self):
        status, output, original, leftovers = self.run_fixture(preexisting=b"old bytes")
        self.assertEqual(status, 0)
        self.assertEqual(output, original)
        self.assertEqual(leftovers, [])


if __name__ == "__main__":
    unittest.main(verbosity=2)
