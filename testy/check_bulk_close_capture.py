"""Check a real Ctrl+Shift+W cancellation capture from the isolated WinForms host."""
import sys
from pathlib import Path


def read_state(directory, name):
    return dict(line.split("=", 1) for line in (directory / (name + ".state")).read_text(encoding="utf-8").splitlines())


def check(directory, before, after, disk_name="dirty.txt"):
    start, end = (read_state(directory, x) for x in (before, after))
    assert start["pid"] == end["pid"] and start["product"] == end["product"]
    assert start["foregroundPid"] == start["pid"] and end["foregroundPid"] == end["pid"]
    assert not start["modals"] and not end["modals"]
    dirty = next(x for x in start["documents"].split("|") if x.startswith(disk_name + ":True:"))
    assert start["active"] != disk_name, "fixture must use a nonactive modified document"
    assert dirty in end["documents"].split("|"), "Ctrl+Shift+W lost the modified buffer instead of preserving it on cancel"
    assert start["active"] == end["active"], "cancel must keep the original active document"
    assert (directory / disk_name).read_bytes() == ("On disk " + disk_name).encode("utf-8"), "cancel must not write the file"


if __name__ == "__main__":
    check(Path(sys.argv[1]), *sys.argv[2:])
    print("PASS: cancelling bulk close preserves the nonactive buffer and disk bytes")
