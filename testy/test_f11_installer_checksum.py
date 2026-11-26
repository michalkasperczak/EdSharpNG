"""Compile the F11 checksum decision and run it against a synthetic payload.

Run with Python 3 under WSL on the Windows build host. No network or Setup.
The live F11 test separately covers routing, download and the real update UI.
"""
from pathlib import Path
import re
import subprocess
from tempfile import TemporaryDirectory


ROOT = Path(__file__).resolve().parent.parent
CSC = '/mnt/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe'


def main():
    source = (ROOT / 'EdSharp.cs').read_text(encoding='utf-8-sig')
    handler = source.split('public void ElevateVersion(bool bForce) {', 1)[1]
    block = handler.split('Util.Say(JezykInterfejsu.Wybierz("Preparing the verified update",', 1)[0]
    expected = re.findall(r'^if \((sExpected[^\n]*)\) \{$', block, re.M)
    actual = re.findall(r'^if \(((?:sActual|!String\.Equals\(sActual)[^\n]*)\) \{$', block, re.M)
    assert len(expected) == 1 and len(actual) == 2, (expected, actual)
    # Compile the actual predicates from the handler, not an equivalent in Python.
    decisions = ' || '.join('(' + condition + ')' for condition in expected + actual)
    program = r'''
using System;
using System.Security.Cryptography;
using System.Text;
class F11ChecksumTest {
    static bool Accept(string sActual, string sExpected) { return !(DECISIONS); }
    static int Main() {
        string actual;
        using (var sha = SHA256.Create())
            actual = BitConverter.ToString(sha.ComputeHash(Encoding.ASCII.GetBytes("abc"))).Replace("-", "");
        const string expected = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
        if (Accept(actual, "")) { Console.WriteLine("FAIL: missing published checksum accepted"); return 11; }
        if (Accept("", expected)) { Console.WriteLine("FAIL: unavailable file checksum accepted"); return 12; }
        if (Accept(actual, "0" + expected.Substring(1))) { Console.WriteLine("FAIL: different checksum accepted"); return 13; }
        if (!Accept(actual, expected)) { Console.WriteLine("FAIL: same checksum in different letter case rejected"); return 10; }
        Console.WriteLine("PASS: same SHA-256 accepted; different and missing values rejected");
        return 0;
    }
}
'''.replace('DECISIONS', decisions)
    # Windows cannot run this probe in a WSL-only temporary directory.
    with TemporaryDirectory(prefix='EdSharpF11Checksum.', dir='/mnt/c') as name:
        directory = Path(name)
        (directory / 'test.cs').write_text(program, encoding='ascii')
        subprocess.run([CSC, '/nologo', '/out:test.exe', 'test.cs'], cwd=directory, check=True)
        return subprocess.run([str(directory / 'test.exe')], cwd=directory).returncode


if __name__ == '__main__':
    raise SystemExit(main())
