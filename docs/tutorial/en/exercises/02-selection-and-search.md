# Exercise 02 — selection and search

**Goal.** Select a range without a mouse and search without changing the document.

## Steps

In a new file type:

```text
alpha
beta
alpha
gamma
```

Stand before `beta`, press `F8`, move Down to the end of the second `alpha`, then press `Shift+F8`. Enter `alpha` after `Ctrl+F`, then use `F3` and `Shift+F3`.

**Check:** the selection includes `beta` and the second `alpha`; search moves between two occurrences. After the last occurrence, another `F3` reports `Not found!` and leaves text unchanged.

**Safe return:** `Ctrl+Shift+F8` restores the last range and `Alt+Shift+F8` returns to the original `F8` point; after selecting upward, that point can be the lower end of the range. No text was changed.

[Return to handbook](../handbook.md).