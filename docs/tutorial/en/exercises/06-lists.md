# Exercise 06 — lists

**Goal.** Check selection-only operations and distinguish a two-list comparison.

## Steps

In a new file type:

```text
Anna
anna
Olivia
Anna
```

Select only these four lines and use `Alt+Shift+K`. Undo with `Ctrl+Z`, then use `Alt+Shift+O`, undo, and use `Alt+Shift+Z`.

**Check:** deduplication keeps the first `Anna`, `Olivia`; it does not distinguish `Anna` and `anna`. Sorting also ignores case but retains the items' original spelling. Each test is on a selection.

Replace the text with these two lists and put the caret at the beginning of `Anna` in the second part:

```text
Anna
anna
Olivia
Anna
Anna
olivia
```

`Alt+Shift+G` creates a new window with `anna`, `Olivia`; `Alt+Shift+Q` creates a new window with `Anna`, `Anna`. The first list is above the current row, the second from the current row to the end. First-list duplicates remain and comparison is case-sensitive.

**Safe return:** output is in a new window; close it without saving so no file is created. Comparison itself leaves the exercise file unchanged.

[Return to handbook](../handbook.md).