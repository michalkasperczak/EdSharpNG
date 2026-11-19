# Exercise 05 — durable snippet library and ZIP

> **Target PL/EN menu DRAFT.** Use a new file plus a test folder and ZIP, not your production library.

**Goal.** Set a durable folder, save and expand a snippet, and learn explicit ZIP export/import.

## Steps

1. Press `Ctrl+,`. In **Settings > Configuration Options…**, find **Snippet library folder**, use **Browse…**, choose an existing test folder, and confirm.
2. In a new file type `Regards,`, select it, use `Alt+S` (**Save Snippet…**), and name it `signature`.
3. At a new position use `Alt+V` (**Insert Snippet…**). `Alt+Shift+V` (**Edit Snippet…**) opens the file for reading or editing.
4. Delete the insertion, type `signature` before the caret, and press `Ctrl+Shift+Space` (**Expand Snippet**).
5. In **Tools > Snippets**, use **Export Snippet Library…** to save a test ZIP outside the library. For a test import use **Import Snippet Library…** and deliberately select a conflict policy.

## Expected result

One folder is used for saving, inserting, editing, tokens, and manual expansion. An exact name wins, then a prefix, then a fragment; several matches require a choice. ZIP import asks to skip, keep both, or replace with backup. An empty folder setting means the existing `Snippets\Default`; an explicit unavailable folder reports an error without silent fallback.

## Safe return

`Ctrl+Z` reverses insertion in the current document. Keep your own folder copy before import and cancel rather than guessing a conflict policy. `Ctrl+Space` only selects a non-blank chunk; it does not expand a snippet.

[Return to handbook](../handbook.md).
