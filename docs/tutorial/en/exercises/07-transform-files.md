# Exercise 07 — Transform Files

**Goal.** Run a safe Test on copies and recognize writing by Run and Verbose.

## Steps

Create a folder named `transform-copies`. Put copies of two small `.md` files in it; do not point to your own documents. In a new document, type the full paths of both copies, one path per line.

Prepare `trim-spaces.inix`:

```ini
[Trim trailing whitespace]
Options=multiline
Find=[ \t]+$
Replace=
```

Press `Alt+=`, choose that file, then choose **Test**.

**Check:** you hear copy names, the section name, and match counts; both copies remain identical to their pre-test contents. Choose **Run** only after comparing your copies. When you need more messages, choose **Verbose**: it also applies changes, it is not a test.

Add a separate section with `Extract=true` if you want to collect matches. That section writes to the clipboard, not files.

**Safe return:** `Run` may save every copy with a match, including when `Replace` is empty. There is no bulk Undo or promised rollback. Delete the copy folder and recreate it from your own backup instead of running a job on user work.

[Return to handbook](../handbook.md).