# EdSharp offline handbook — DRAFT of the new PL/EN menu

> **Unpublished DRAFT.** This handbook describes the frozen target PL/EN menu model, not Michał's installed release. It does not claim that the exercises were run in the GUI or measured with live NVDA. Work in a new file or an exercise copy, never in your own document.

<!-- lesson: document-save -->
## 1. Document and save

**Goal.** Create a small Markdown document and keep it under your own name.

1. Press `Ctrl+N`, type a title and two sentences.
2. Press `Ctrl+Shift+S`, choose an exercise folder and name it `note.md`.
3. Add a third sentence and press `Ctrl+S`.

**Expected result.** A separate `note.md` contains three sentences. `Ctrl+S` saves the current document and `Ctrl+Shift+S` asks for a new name.

**Safe return.** `Ctrl+Z` reverses the last edit; closing without saving can discard unsaved changes. This is not a replacement for your own file copy.

Exercise: [01 — document and save](exercises/01-document-and-save.md).

<!-- lesson: selection-search -->
## 2. Selection and search

**Goal.** Select two lines without a mouse and find a repeated word.

1. Put the caret before the first line and press `F8`.
2. Move to the end of the range and press `Shift+F8`.
3. Use `Ctrl+F`, then `F3` or `Shift+F3`.

**Expected result.** The selection runs from the `F8` point to the caret; search does not alter text. `Alt+F3` searches text at the caret or selection.

**Safe return.** `Ctrl+Shift+F8` restores the previous range and `Alt+Shift+F8` returns to the original `F8` point; after selecting upward, that point can be the lower end of the range.

Exercise: [02 — selection and search](exercises/02-selection-and-search.md).

<!-- lesson: markdown-preview -->
## 3. Markdown and preview

**Goal.** Write a heading, list, and link, then check preview.

1. In an `.md` file type `# Meeting`, a blank line, and `- coffee`.
2. Use `Escape` for the text Markdown preview; a second `Escape` returns to editing.
3. After saving, use `F5` or target-menu **View > Preview Markdown in Web Browser**.

**Expected result.** `Escape` opens a text `MarkdownReviewTextBox`, not a WebView. `F5` opens the saved document in the default browser. **View** also starts with **Word Wrap** and **Unwrap**.

**Safe return.** `Escape` leaves preview; `Ctrl+Z` after returning reverses only a text edit.

Exercise: [03 — Markdown and preview](exercises/03-markdown-and-preview.md).

<!-- lesson: tasks-tables -->
## 4. Tasks and tables

**Goal.** Create a checklist and a small table in a new file.

1. Use `Ctrl+Shift+F2` to create a task list.
2. Press `Ctrl+Shift+X` to change a task; `Alt+Shift+F2` reports progress.
3. Use `Ctrl+Shift+T`, supply wizard data, and confirm with `Ctrl+Enter`.

**Expected result.** In target **Review > Tasks**, the dynamic command says **Mark as Done** or **Mark as Not Done**. Help and the command palette use the neutral name **Toggle Task Completion**. After toggling, the application announces **Done** or **Not Done**. The table is in the current document; target menu location is **Insert > Insert Table…**.

**Safe return.** `Ctrl+Z` reverses the latest operation only in the exercise file.

Exercise: [04 — tasks and table](exercises/04-tasks-and-table.md).

<!-- lesson: snippets -->
## 5. Snippets: durable folder, ZIP, and expansion

**Goal.** Choose one durable library folder, save and insert a snippet, then safely export or import a ZIP.

1. Press `Ctrl+,`. In target **Settings > Configuration Options…**, choose **Snippet library folder** and use **Browse…** to select an existing durable folder, such as a folder in your documents. Confirm the change.
2. Select test text and use `Alt+S` (**Save Snippet…**). Insert with `Alt+V` (**Insert Snippet…**) and open for editing with `Alt+Shift+V` (**Edit Snippet…**).
3. Type a name fragment immediately before the caret and use `Ctrl+Shift+Space` (**Expand Snippet**).
4. In **Tools > Snippets**, choose **Export Snippet Library…**, save a ZIP outside the library, then use **Import Snippet Library…** with a test ZIP.

**Expected result.** The same folder serves saving, inserting, editing, tokens, and manual expansion. An empty setting uses the existing `Snippets\Default`; an explicit unavailable folder reports an error and does not silently fall back. Expansion prefers an exact name, then a prefix, then a middle/end fragment; several results require a choice. ZIP import asks how to handle conflicts: skip, keep both, or replace with backup. Export creates a ZIP, not another library tree.

**Safe return.** `Ctrl+Z` reverses insertion in the current document. Keep your own folder copy before import; canceling a dialog should not modify files. `Ctrl+Space` still selects a non-blank chunk; it does not expand a snippet.

Exercise: [05 — snippets](exercises/05-snippets.md).

<!-- lesson: lists -->
## 6. List operations

**Goal.** Work on selected lines and compare two lists.

1. Select a range and use `Alt+Shift+O`, `Alt+Shift+Z`, or `Alt+Shift+K`.
2. For a comparison, put the caret so the first list is above the current row and the second begins at it.
3. Use `Alt+Shift+G` or `Alt+Shift+Q`.

**Expected result.** Sorting and deduplication ignore case and retain the first spelling. Two-list comparisons retain first-list order and duplicates but are case-sensitive. The target location is **Tools > Text Transformations**.

**Safe return.** Work in a copy; `Ctrl+Z` reverses the latest text change.

Exercise: [06 — lists](exercises/06-lists.md).

<!-- lesson: transform-files -->
## 7. Transform Files: copies and Test first

**Goal.** Run a find-and-replace job only on copied files.

1. Prepare copies in a separate folder and a document containing their full paths, one per line.
2. Use `Alt+=` or **Tools > Text Transformations > Transform Files**, then choose an `.inix` job.
3. Choose **Test** first, and only then **Run** or **Verbose**.

**Expected result.** **Test** counts matches without writing. **Run** writes matching files. **Verbose** also writes files and reports sections and match counts. `Extract=true` collects matches to the clipboard without modifying files.

**Safe return.** No complete Undo or multi-file rollback is promised. Keep your own copy before `Run` or `Verbose`; if needed, delete only the copy folder and restore it from backup.

Exercise: [07 — Transform Files](exercises/07-transform-files.md).

## Boundaries of this DRAFT

This content comes from the frozen 210-command menu model. It does not prove final integration, the installer, live GUI shortcut routing, or NVDA speech. Removed commands — Styles, Copy All, Alternate Menu, Clear Favorite, Topic/Alt+T, Read All/Alt+F8, Read Clipboard/Alt+apostrophe, Env Vars editor, and Speech Log — are neither restored nor documented.
