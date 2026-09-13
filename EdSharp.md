# EdSharp User Guide

EdSharp\
Version 5.0 beta\
June 2026\
Copyright 2007 - 2026 by Jamal Mazrui\
GNU Lesser General Public License (LGPL)\

## Contents

- [Description](#description)
- [Installation](#installation)
- [Editing](#editing)
- [Navigating](#navigating)
- [Querying](#querying)
- [Managing Files](#managing-files)
- [Invoking Snippets](#invoking-snippets)
- [Working with Structured Text](#working-with-structured-text)
- [Word Processing](#word-processing)
- [Doing Math](#doing-math)
- [Programming](#programming)
- [Scripting Add-Ins](#scripting-add-ins)
- [Miscellaneous](#miscellaneous)
- [Hotkey Summary](#hotkey-summary)
- [Development Notes](#development-notes)

## Description
EdSharp is a full featured text editor that is friendly, powerful, and open source.  It uses a standard Windows interface for an application that supports multiple document windows.  Though intended for sighted users as well, it seeks to enhance productivity for users of the JAWS, NVDA, Window-Eyes, or System Access screen readers by automatically verbalizing relevant information.  These speech messages supplement default speech heuristics, providing confirmation or results of commands without the need for manually interrogating the screen.  If a screen reader is not detected in memory, EdSharp uses the default SAPI voice, if available, which may be configured via the Speech applet in Control Panel.

Written in the C# (pronounced C Sharp) language, EdSharp implements the "Homer editor interface," which originally evolved with an editor called TextPal.  The same interface was also implemented in the package of JAWS scripts and tools called HomerKit.  EdSharp 5.0 is built AnyCPU on the .NET Framework 4.8, so it runs as a native 64-bit application on both Intel/AMD (x64) and ARM (ARM64) editions of Windows.  The .NET Framework 4.8 is built into Windows 10 and 11 and is a free download from Microsoft for older systems.

Almost every EdSharp command may be done through a mnemonic keystroke, as well as a menu or mouse operation.  These commands begin with the standard keys available in Notepad or most Windows-based editors.  EdSharp then adds many beneficial features.  Optional scripts for some screen readers provide further fine tuning of the speech interface for those users.

## Installation
The installation program for EdSharp is called EdSharp_Setup.exe.  When executed, it prompts for a program folder, the default being\
`C:\Program Files (x86)\EdSharp`
The installer also creates a program group for EdSharp on the Windows start menu, containing choices to launch EdSharp, read Documentation, or uninstall.  Additional choices either set or clear an association between EdSharp and files with a particular extension, such as .txt or ini.  Binary formats such as `pdf or .pptx may also be associated with EdSharp, thus permitting automatic conversion to text when opened via Windows Explorer.

After installing EdSharp, the setup program presents a list of several checkboxes.  One checkbox offers an optional set of JAWS scripts to fine tune the EdSharp speech interface in a few ways that could not be accomplished otherwise.  If the scripts were installed and you would later prefer default JAWS behavior instead, you can disable the scripts via the "Manage Application Settings" dialog from the "Options menu of JAWS.  

Another checkbox sets Alt+Control+E as a system-wide key associated with the EdSharp shortcut placed on the Windows desktop.  If this hot key is found to conflict with an existing shortcut, navigate to either the EdSharp or other shortcut on the desktop, press Alt+Enter for properties, and then change the hot key to something else (or leave it blank).  

An additional checkbox opens this manual in the default web browser.

EdSharp may be safely installed over previous versions.  The About option from its Help menu, or Alt+F1 key, indicates the current version number and release date.  The History of Changes command, Shift+F1, summarizes fixes and improvements over time.

EdSharp features may be explained in the following categories of activity:  editing, navigating, querying, managing files, invoking snippets, working with structured text, word processing, programming, doing math, and miscellaneous.

## Editing
### Selecting, Copying, and Pasting
As usual, you may press Control+C or Control+X to copy or cut selected text to the clipboard.  EdSharp tries to make an an intelligent guess when no text is selected.  In this case, the current line is assumed .  Press Alt+C or Alt+X to perform a copy or cut operation that appends rather than replaces text on the clipboard.  If the previous clipboard text did not end with a line break, EdSharp inserts one before the appended text.

Every copy in EdSharp retries when the clipboard is busy.  A clipboard manager -- Ditto, or the clipboard history built into Windows -- opens the clipboard for a moment after each change, and a program that writes to it only once fails silently in that moment.  EdSharp tries ten times, forty milliseconds apart, which covers the quarter of a second such a manager typically holds it.  If all ten attempts fail the program SAYS so instead of claiming a copy that did not happen: silence would be worse than a message, because the key sounds identical either way and the loss only shows up on pasting.  This applies to text fields in dialog boxes and to the copy keys on lists as well, not only to the document.

An alternative way of selecting text uses F8 to mark the start of a selection.  Navigate to the end point by whatever means (arrow keys, find command, etc.) without having to hold down the Shift key.  Note that the caret should be placed one position past the last character to be selected.  Press Shift+F8 to select text from the start position.  EdSharp says the number of characters selected.  You may subsequently select between the same positions again with Control+Shift+F8, or return to the start position with Alt+Shift+F8.

As usual, Control+A selects all text.  Control+Shift+A clears any selection.  Press Control+F8 to copy all text to the clipboard with a single command.  Control+V pastes clipboard text at the cursor position.  Control+Shift+V inserts the content of a file instead.

Press Control+Space to select a chunk of text at the cursor position.  A chunk is defined as a contiguous sequence of non-space characters.  It may be more than what the hotkey Control+Shift+RightArrow selects, since such word movement commands stop at punctuation marks.  Press Control+Space again to extend the selection to the next chunk.

The Append From Clipboard command, Alt+Shift+C, is like the "paste board" feature of the NoteTab editor.  When this mode is toggled on for a document, each snippet of text copied to the Windows clipboard -- whether from another application or from a different document open in EdSharp -- will also be pasted into the collecting document, which will then automatically be saved to disk.  EdSharp beeps to confirm this is happening.  Text copied within the collecting document itself is not appended, so copying part of that document does not copy it back into itself.  Snippets are separated by a section break sequence, which makes it possible to navigate among them with the Control+PageDown and Control+PageUp commands.  Thus the feature may be used to conveniently collect and save information from applications that do not have a built-in appending mechanism.  When done, toggle off the mode with the same command, Alt+Shift+C.

Press Control+Z to undo the last editing operation, or Control+Shift+Z to redo it.

Press Control+N to start a new document, or Control+Shift+N to initialize one with text on the clipboard.  A new document is saved with the .md extension by default, so the Markdown commands (contents, footnotes, comments, lists, links) work in it right away.  The default extension is the ExtensionDefault setting; if you had set it to rtf, EdSharpNG changes it to md once, and any other value you chose is left alone.

### Replacing
Press Control+R to replace text and hear the number of matches.  Press Control+Shift+R to replace with a "regular expression" -- a complex but powerful syntax that permits almost any transformation of text.  EdSharp replaces within selected text, or all text if there is no selection.  Press Control+Shift+E to extract parts of text based on a regular expression.  The matching parts are placed in a new editing window separated by a section break sequence of characters.  EdSharp uses regular expressions of the .NET Framework, explained in [Microsoft's .NET regular expression reference](<https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference>).

Press Alt+Shift+P to copy the full path of the current file to the clipboard, permitting it to be easily pasted into dialogs of other aplications.  Use the Path List command, Control+Shift+P, to generate a list of files in a new editing window.  You are prompted for the directory to open, and then the extensions to include based on what files EdSharp finds in the directory.  The new window contains the full path of the first file, and then just file names on subsequent lines -- since they are in the same directory.

### Changing Case
Press Control+U to convert the current character or selected text to upper case, or Control+Shift+U to lower case.  Alt+U converts to proper case, putting the first letter of each word in upper case and the rest in lower case.  Alt+Shift+U inverts case, converting lower case characters to upper case and vice versa.

The Yield Encoding command, Alt+Shift+Y, may be used to convert all or selected text according to a particular character encoding.  If text from a file or the clipboard appears to be rendered improperly in EdSharp, you can tell it to base its interpretation on a different encoding:  ASCII, Latin 1, UTF-7, UTF-8, UTF-16, UTF-32, or another encoding that you pick from a list of over 100 available.  You can also choose a conversion where the Unicode number of each character is put on a separate line.  This may be used to identify non-printing characters in the document.  The command replaces all or selected text, so put a copy in a new document window if you want to retain the original.

Use the Quote command, Control+Q to add an email quote sequence (> ) at the start of the current or selected lines.  Control+Shift+Q removes this sequence, as well as any other leading space or tab characters.  

F2 prompts for a character to insert based on its numeric value in the Unicode character set.  The number should have four hex digits (base 16).  This command is useful for inputting a character that does not have a corresponding keystroke.  You can specify a decimal (base 10) number instead by preceding it with the letter d.  For example, the ellipses symbol (...) may be specified either in hex as 2026 or in decimal as d8230.

Press Control+Shift+J to join lines of all or selected text.  A sequence of any number of spaces followed by a hard line break is replaced by a single space.  However, consecutive line breaks denoting a paragraph break are not effected.  Generally, the purpose is to word wrap paragraphs that contain unnecessary line breaks, e.g., text received in an email message.

The reverse command is Hard Line Break, on the Edit menu without a shortcut since 5.0.65, because Kasperczak judged the key more valuable than the command.  It lets you specify the maximum width of lines in all or selected text.  EdSharp prompts for the number of characters allowed before a line break, defaulting to the number currently found as the width of text.  Generally, the purpose is to format text for a display that does not automatically word wrap.

### Comparing and Sorting
Several commands help you compare and sort textual items.  The LimitItem setting in the Configuration Options dialog specifies the divider between items, which is a hard line break by default (\n) -- lines are unwrapped for this purpose.  Use the Trim Blanks command, Control+Shift+Enter, to eliminate space or tab characters at the start and end of a line in the current line or selected text.  Consecutive blank lines are also reduced to a maximum of two.

Use the Order Items command, Alt+Shift+O, to alphebetically sort lines in all or selected text.  Press Alt+Shift+Z to reverse the order of all or selected lines (you may think of a reverse order from Z to A).  Use the Keep Unique Items command, Alt+Shift+K, to eliminate duplicate lines from all or selected text.  EdSharp ignores case when comparing lines in these commands.  

Press Alt+Shift+N to number lines in all or selected text.  EdSharp prompts for the starting number, defaulting to 1.  Each line is then prefixed by a consecutive number, period, and space.  Blank lines are ignored.

Two commands produce text in a new window after comparing sets of lines.  Lines above the cursor are considered the first set, and the rest are considered the second.  Use the List Different Items command, Alt+Shift+G, to get lines that are in the first set but not the second.  Use the Query Common Items command, Alt+Shift+Q, to get lines that are in both.  These commands ignore blank lines but are case sensitive.

### Deleting
As usual, Delete (without a selection) deletes a character in the forward direction, and Backspace deletes backward.  Control+Delete deletes forward by a word, and Control+Backspace deletes backward.  Control+Shift+Delete deletes from the cursor to the end of the line, and Control+Shift+Backspace deletes from the cursor to the start of the line.  Alt+Shift+Delete deletes from the cursor to the end of the document, and Alt+Shift+Backspace deletes from the cursor to the top of the document.  Alt+Backspace deletes the current line.  Control+D deletes the current hard line (past wrapping to the next hard line break).  Control+Shift+D deletes the current paragraph (past one or more blank lines).  After deleting, EdSharp reads the new character, word, or line at the cursor.

Press F7 to spell check all or selected text.  With no selection, checking starts at the cursor rather than at the top of the document, so you can resume where you left off.  EdSharpNG uses the spell checker built into Windows -- the same one other Windows programs use -- so Microsoft Word is no longer required and the check happens without leaving the editor.  Polish is supported, as is any other language whose dictionary is installed in Windows.  Use the Thesaurus command, Shift+F7, to look up synonyms for the word at the cursor position; that one still relies on Microsoft Word.

## Navigating
Press Home or End to go to the start or end of the line, and automatically hear the character there.  Press Alt+Home or Alt+End to go to the first or last non-blank character of the line.  Press Control+Home or Control+End to go to the top or bottom of the document, and automatically hear the line there.

As usual, Control+F finds text in a forward direction.  Control+Shift+F reverses the search.  Alt+F3 or Alt+Shift+F3 search forward or backward for either the chunk at the cursor or selected text.  Control+F3 or Control+Shift+F3 prompt for a regular expression for searching forward or backward.  F3 or Shift+F3 searches forward or backward for the last target, which may be either standard text or a regular expression.  If a search is successful, EdSharp reads the matching line automatically.

The Text you enter in Find or Replace dialogs may include tokens that represent nonprinting characters.  This syntax is available for strings in the C programming language and its variations.  Common tokens are a pair of characters consisting of a backslash and letter, such as the following:  \r for carriage return (ASCII 13), \n for line feed (ASCII 10), \t for tab (ASCII 9), and \f for form feed (ASCII 12).  Such tokens allow you to search for text, say, at the beginning or end of a line (use \n for a line break in EdSharp).  

The trade off for this flexibility is that backslash and quote characters must be preceded by a backslash when intended literally (not part of a token), i.e., \\ for backslash and \" for quote.  Since this doubling of characters may be cumbersome with search terms such as a file path, however, EdSharp supports use of an initial @ character to indicate that the following characters should be interpreted literally rather than as possible tokens.  For example, if searching for a file in the document, you could enter the term\
`@c:\temp\temp.txt`
rather than\
`c:\\temp\\temp.txt`
If you need to search for a string that begins with the @ character, precede it with a backslash, e.g., `\@domain.com` follows the user name of an email address.

Press Control+G to go to a percentage point in the document, or Alt+G to repeat the command with the previous value.  Similarly, press Control+J to jump to a line number, or Alt+J to repeat.  A column number may be specified after the line number and a comma.  If no line number is specified before the comma, EdSharp jumps to the column on the current line.

Press Control+RightArrow or Control+LeftArrow to read by word -- stopping at embedded symbols.  Words containing accented letters, such as Polish diacritics, are treated as single words: the editor decides where a word begins and ends itself, because the underlying edit control would otherwise break a word at every accented letter.  Holding Shift as well selects by word.  Press Alt+RightArrow or Alt+LeftArrow to read by chunk -- text delimited only by white space characters.  Press Alt+DownArrow or Alt+UpArrow to move by sentence -- your screen reader announces the sentence itself, because these two keys are its own sentence commands, so EdSharp only moves the cursor and puts the sentence on the status bar.  Press Control+DownArrow or Control+UpArrow to read by paragraph, which announces the paragraph once.

## Querying
Use the Address command, Alt+A, to hear the line, column, and percent position of the cursor in the document.  Press Alt+P to hear the full path of the file on disk.  Use the Yield command, Alt+Y, to hear the number of characters, words, and lines contained in all or selected text.  Press Alt+Z to hear whether the document has been modified from the version on disk, or press it again to check its character encoding and the kind of line breaks the file had on disk (Windows, Unix, Macintosh, or mixed).

Press Alt+F8 to hear the whole document without moving the cursor.  Use the Quote Clipboard command, Alt+Apostrophe, to hear the textual content of the clipboard, or its spelling if this key is pressed again.  Press Alt+Semicolon to hear the current time and date.

Press Shift+Space to hear selected text.  Press Shift+Backspace to hear the chunk of text at the cursor.  If the same key is pressed again without moving the cursor, the text is spelled instead.

Press Control+Shift+Y for the "yield," or number of results matching a regular expression, which you specify.  This may be useful before using Control+Shift+R to replace text or Control+Shift+E to extract it.

## Managing Files
Press Control+O to open a file.  It is the only Open command, and it decides for itself how the file should be shown.  Text, Markdown, reStructuredText, source code, and anything unrecognized open directly.  Documents whose raw bytes are unreadable -- Word, Excel, PowerPoint, PDF, EPUB, WordPerfect, WinHelp -- are converted to text.  A web page (.htm, .html, .xhtml) offers a choice, because more than one result makes sense: Markdown, plain text, or Tidy.  A rich text file (.rtf) offers its own list, described in the Rich Text section.  Before 5.0.99 a second command, Open Other Format on Control+Shift+O, was needed for conversion; it was removed and its behaviour folded into Control+O, so there is no longer a way to open a document as raw bytes by accident.  The ViewLevels option overrides the decision for any extension.

Opening a file also makes its folder the starting point for the next Control+O.  Until 27.08.2026 only SAVING changed that folder, so after saving something elsewhere, reaching a file next to the one just opened meant navigating the whole way again.

File converters may be configured through the Manual Options command, Alt+Shift+M.  For example, an open source PDF converter is distributed with EdSharp and configured, by default, with the following line in the Import section:\
`pdf=%ProgDir%\pdftotext.exe %Source% %Target%`

To configure a converter, specify the command line for converting an extension from a source, non-text format to a target, text format.  The following variables may be used:\

EdSharp bundles the `2htm` utility (in `Convert\2htm`) and uses it, through the `Convert\any2txt.cmd` wrapper, to extract plain text from many document formats -- Word (`.docx`, `.doc`, `.rtf`, `.odt`), PDF, Excel (`.xlsx`, `.xls`), PowerPoint (`.pptx`, `.ppt`), HTML, CSV, JSON, and more.  This replaces the older `GetText.exe`, which hung on modern Windows, along with the separate Office and HTML text converters.  Markdown and other lightweight markup are still converted with Pandoc.

- %ProgDir% = Full path of the directory containing the EdSharp.exe program
- %Source% = Full path of the source file
- %SourceDir% = Full path of the directory containing the source file
- %SourceName% = Name of the source file
- %SourceRoot% = Root name without extension of the source file
- %SourceExt% = Extension of the source file

The short path of a file or directory is used unless a variable includes a Long suffix, e.g., %ProgDirLong% or %SourceLong%.  Most utilities require long file names to be surrounded by quote marks, e.g., "%SourceLong%" syntax.  For technical reasons, if quotes are used within the command line, then a pair of quotes should also be added around it.  Variables for the Target file are like that of the source.

External converters distributed with EdSharp are stored in the Convert subfolder of the EdSharp program folder, e.g., in (default installation)\
`C:\Program Files (x86)\EdSharp\Convert`

A text format called Markdown is useful for various conversions, explained in the [Markdown article on Wikipedia](<http://en.wikipedia.org/wiki/Markdown>).

If EdSharp finds more than one converter available for a file extension, you are prompted which one to use.  If a converter entry does not contain the digit 2 and another extension, it is assumed to be .txt.

Use the Open Again command, Alt+O, to reload the current file from disk.  Press Alt+R to open a file from the list of those recently used.  Use the FileFind command, Alt+Shift+F, to pick a file from a list of those containing text and matching wildcards that you specify.  Multiple wildcard patterns are possible, separated by a vertical bar (|) character, e.g.,\
`*.txt|catalog*.htm`

### Handling Favorite Files and Bookmark Positions
Press Alt+Shift+L to add the current file to the list of favorites; pressing it again in a file that is already there takes it off the list, so one key both adds and removes.  The key says what it did and nothing else: "Added to favorites" or "Removed from favorites".  It used to read out the name of the command first, "Toggle Favorite", and only then what had happened, which is two things spoken for one keypress; the name of a key you have just pressed carries no information, while on a toggle the direction of the change is the whole point.  The name of the command still goes to the status bar, so nothing is lost for anyone reading the screen.  Press Alt+L to list favorites and open one, and use Delete on that list to take a file off it without opening the file at all.  The Clear Favorite command is still in the menu, with no shortcut of its own, for taking the current file off the list by name; it now says "Removed from favorites" when it takes the file off the list and "Not in favorites" when the file was never on it, where before it did its work in silence and sounded exactly the same either way.  Bookmarks are kept apart from favorites, in their own section of the configuration file, so taking a file off the list of favorites leaves its bookmarks alone and setting a bookmark does not quietly make the file a favorite; the two lists are separate things.  If a bookmark is set, EdSharp automatically goes to it and says the percent position in the document.  Also, if the word wrap or guard setting is different than when the file was designated as a favorite, EdSharp restores that setting and says so.  This command opens a file verbatim, assuming that you had set it as a favorite to edit it literally, e.g., a .htm file you are developing.  When a file is opened from Windows Explorer or the recent files list, on the other hand, EdSharp automatically converts it to plain text if an import converter is configured for its extension.

The Recent Files list (Alt+R) and the Favorites list (Alt+L) each offer extra keys on the highlighted file: RightArrow opens it with another program, LeftArrow reads its full path, Control+Enter shows it in Explorer, Control+C copies the file NAME on its own, Control+Shift+C copies the full PATH, Alt+C appends the path to the clipboard as text, Delete removes entries from the list, and Shift+Delete deletes the file from disk after confirming.  The two copy keys hand you two different pieces of text, which is why both exist: the bare name is what usually goes into a sentence or a search box, and you cannot get it out of a full path without editing by hand.  Control+Shift+C also puts the FILE itself on the clipboard alongside the path, which is what lets you paste it into File Explorer, into an e-mail as an attachment or into any program that takes files; a text box still receives the path, so the destination decides which form it takes.  All the copy keys work across everything you have selected, so Shift with the arrow keys and then Control+C hands you a whole block of files at once; the program says how many it took, and says how many entries no longer exist on disk, since those can only travel as text.  Delete works across the whole selection as well: it takes every selected entry off the list and says how many it took, since the entry is only a line in the settings file and the file on disk is left alone.  Deleting from disk stays deliberately single-file: on several selected items Shift+Delete refuses and says how many are selected, because on a key that erases a file a guess is the difference between a mistake and a lost file.  The list itself announces nothing but the file name, so a screen reader gets to it immediately; these keys are documented here rather than spoken in the list.

Lists in EdSharp take more than one item at a time.  Hold Shift with the arrow keys to extend the selection, or Control+Space to add a single item to what is already selected, exactly as in Windows Explorer; a plain arrow key moves the selection to one item, so nothing changes for anyone who does not use Shift.  Control+C then copies every selected item, one per line and in list order regardless of the direction you selected in, and Alt+C appends them to the clipboard instead of replacing it.  The program says how many items it took, because a coordinate on the screen tells a screen-reader user nothing.  This holds on the Recent Files and Favorites lists as well, where Control+C copies the file names and Control+Shift+C the full paths (with the files themselves, so they paste into File Explorer, not only as text), and Delete takes every selected entry off the list; only Shift+Delete stays single-file there, because it erases a file from disk and a guess would cost the wrong file.

Numbered files (pliki numerowane) give ten fixed shortcuts to the files you return to most often.  Press Alt+Shift and a digit to assign the current file to that digit; EdSharp confirms, for example, "Numbered file 3 is notes.md".  Press Alt and the same digit to go back to it later: if the file is already open EdSharp moves to its window and says just the file name, and if it is not open it says "Opening notes.md" and opens it.  The digits 1 through 9 are numbers 1 through 9, and 0 is the tenth, matching the order of the keys on the keyboard.  The numeric keypad works too.  An unused number says "Numbered file 3 is empty!", and a file that has since been moved or deleted says "Numbered file 3 not found!".  Assignments are stored on disk, so they survive restarting EdSharp.  A document that has never been saved cannot be assigned, since there is no file to remember.  To review what you have assigned, use the Numbered Files command, Alt+Shift+F2, which lists each one with its key and file name.  Numbered files are deliberately separate from the Control+digit window navigation described with the window commands: these are fixed assignments that you choose, those are simply the windows that happen to be open.

Press Control+B to set a bookmark at the cursor position.  If the current file has a single bookmark, Alt+B goes to it.  If more than one, a list of bookmarked lines is presented, with focus on the next one ahead of the cursor position.  Thus you can sequentially visit bookmarks by pressing Alt+B and Enter.  On that list, Delete or Backspace removes the highlighted bookmark, which is the everyday way to get rid of one; removing the last one leaves the list open and empty, saying "No bookmarks, press Escape to close the list", so that a run of Delete presses cannot end with the focus back in the text of the document, where the next press would delete a character instead of a bookmark.  Leaving the list is always Escape.  The Clear Bookmark command, on the Navigate menu, clears a bookmark at the cursor position and has no shortcut of its own.  Bookmarks live in their own section of the configuration file, apart from the list of favorites, so removing a file from favorites no longer clears them and bookmarking a file no longer adds it to favorites; to clear bookmarks, delete them on the Alt+B list.  EdSharp tracks and restores the bookmark, word wrap, and guard settings of each file opened.

A bookmark with a name of your own is a separate thing, on Control+Shift+B, and it has its own list on Alt+Shift+B.  Two lists rather than one box asking which kind you want, because in daily use you either want the place you were reading or the place you gave a name to, and never both at once.  The box offers the word under the cursor as a starting name, which is shorter and more precise than a whole line of a paragraph would be; the name is required, because a nameless entry on that list would tell a screen reader nothing.  Standing on a line that already carries a named bookmark, the same key renames it, and clearing the field removes it -- the title of the box says which of the two is happening.  The list shows the name first and the line number after it, content before coordinates as everywhere else here, and Delete takes an entry off the list without leaving the window; removing the last named bookmark leaves the list open and empty in exactly the same way as the ordinary bookmark list, so a run of Delete presses cannot drop you back into the text.  Named bookmarks are kept apart from ordinary ones in the configuration file, in their own section, so removing a file from favorites does not touch them and they are counted per line rather than per character position: adding a word higher up in a paragraph leaves them where they were.

### Saving
As usual, press Control+S to save.  Press Control+Shift+S to Save As, giving the document a new name in EdSharp and on disk.  Press Alt+Shift+S to save a copy of the document under a different name while keeping the original name in EdSharp.  

When saving text to a file, EdSharp checks whether any character has a Unicode number greater than 255, which means that more than one byte is needed to represent it.  If so, the file is saved with a UTF-8 encoding, the most common form of Unicode for storing files on disk.  Otherwise, the default encoding of the computer is used, e.g., Latin 1.

Alt+Shift+E exports a file to another format.  Built-in options include ASCII format (characters with ANSI codes above 127 are removed), Mac format (line break is \r), and Unix format (line break is \n).  The Mac and Unix options write the file in UTF-8 without a byte order mark, so accented letters survive on the system the copy is meant for; before version 5.0.39 they were written in the single-byte code page of Windows, which made a Unix copy unreadable anywhere but a Polish Windows.  The ASCII option keeps the single-byte encoding, since it strips accents anyway.  Via Microsoft Word converters, additional formats include .doc, .htm, .rtf, and .xml.  Other converters may be configured by editing the Export section through the Manual Options command, Alt+Shift+M.  The syntax is like that in the Import section (explained elsewhere).  The Other option lets you pick a character encoding for the target file from a list of over 100 available.

Use the Run command, F5, to execute the current file as if its name had been entered in the Windows Start/Run dialog.  The effect is also like pressing Enter on its name in Windows Explorer, opening it with the program associated with that extension.  For example, pressing F5 when the current document has a .htm extension will open it in the default web browser.

Press Shift+F5 to execute a file path, email address, or web URL at the cursor position.  If text is selected, it will be used instead -- after removing any line break characters.  You also have a chance to adjust it before execution.

Use the Mail command, Control+M, to send the current file as the body of a message, or Control+Shift+M to send it as an attachment.  These commands invoke a Windows feature similar to the "Send To" feature of Microsoft Word.

  Press Alt+Shift+R to rename the current file, both in EdSharp and on disk.  Press Alt+Shift+D to delete it.

Press Alt+Backslash to open Windows Explorer in the directory containing the current file, or Control+Backslash to open a command prompt there.  Besides the current folder as the default to open, the intervening dialog also lets you open the EdSharp program folder, data folder, or snippet folder.  It also lets you create a new folder on disk.  

Press Control+F9 to verify the current compiler and folder of EdSharp.  Control+0 lets you change the current folder to one containing recent or favorite files, which are put in a list.  Press Control+Alt+0 to change to a special folder of Windows, e.g., My Documents.  These commands may be more efficient than navigating the standard Windows open file dialog, invoked with Control+O.

## Invoking Snippets
Press Alt+S to save all or selected text to a file that may be conveniently pasted into other documents.  You may give the file a descriptive name and an extension appropriate for its content.  It is saved in a subfolder of the EdSharp data folder.  Each programming compiler or interpreter may have its own set of snippets.  The subfolder name is the same as the current value of the Pick Compiler command, Control+Shift+F5.  If no compiler has been chosen, the "Default" subfolder is used.

Press Alt+V to pick one of the available snippets and paste it into the current document.  This command lists snippets in the Default snippet folder, as well as those in the folder associated with a Compiler being used.  This lets you have a set of snippets that are available regardless of the programming language in use.  

Use the View Snippet command, Alt+Shift+V, to load a snippet file into EdSharp for viewing or editing rather than execution.  You can also manage snippet files with the Explorer Folder command, Alt+Backslash, which lets you open Windows Explorer in the subfolder containing snippet files.

EdSharp processes a snippet with a .js extension as JScript .NET code to be evaluated.  Such a file can do almost anything in EdSharp, as explained in the section about EdSharp's scripting capability.  For example, the "ul from selected.js" file generates an HTML unordered list from selected lines of text.

A Non .js snippet may be either literal or an interpreted type.  A literal snippet is pasted completely.  An interpreted type is separated into an initial header line and remaining lines as its body.  The header line contains keywords, in lower case, that control how EdSharp processes the snippet.  At present, two interpreted types are defined:  html and text.  The type keyword must be the first word on the header line, and thus the first word of the snippet file.

The first body line of an HTML snippet is the name of an HTML tag.  Subsequent body lines are attributes of the tag.  An optional default value can follow the attribute name, separated by an equals sign (=).  Here is an example for the anchor tag:

html phrase
a
 href=
 name=
 target=
 class=
 title=
 src=

An attribute may contain \n or other nonprinting tokens.  It may be commented out with a semicolon (;) as the first character of the line.  

The "phrase" keyword tells EdSharp that the tag may be embedded in a paragraph, rather than creating a block with line breaks before and after.  Another keyword, "empty," would tell EdSharp not to add a closing tag like </a> when pasting (e.g., for the <br> tag).  

EdSharp pastes only those attributes that have values greater then zero in length as part of the opening tag.  To include an attribute with essentially no value, enter a space character for it in the dialog.  If text is selected when pressing Alt+V, it is surrounded by the opening and closing tags, and the cursor is placed afterward.  If there is no selection, the cursor is placed between the opening and closing tags.

Over 100 HTML snippets are distributed with EdSharp:  a collection of tags and attributes to serve common needs in developing web sites.  HTML and PHP page templates are also included.  You can modify or add to these, and are encouraged to submit ones you think would be useful to others.  

New .txt and .js snippet files will be installed when upgrading EdSharp.  Since the installer does not replace snippets with the same names, however, you need to manually clear the appropriate folder if you want to ensure a fresh set of snippets.  You can do this by pressing Control+Shift+F5, picking the HTML Tidy compiler, then pressing Alt+Backslash and choosing the snippet folder to open in Windows Explorer.  From that window, press Control+A and Delete to remove all files in the folder.

With the text type of snippet, EdSharp pastes the whole body after making possible substitutions controlled by a keyword called "form."  This lets you embed variable or constant tokens in the body.  A variable has surrounding percent signs and an equals sign between the name and default value.  For example, the variable %City=%Silver Spring% means a variable named City with a default value of Silver Spring.  EdSharp creates a dialog that prompts for the value of each variable it finds in the snippet body.  It then replaces the variable references with the values entered.  You may repeat the same variable reference in a snippet so the user is prompted once for a value that is then used for multiple text insertions.  Subsequent references should omit the default value after the equals sign, e.g., a %City=% token.

Certain constant tokens are also defined:  %Date% for the current date, %Time% for the time, %UserName% for the Windows user name, %UserFirstName% for the first part of that name, and %UserLastName% for the second part, if any.  Date and Time formats may be customized as EdSharp configuration options, using the DateTime formatting syntax of the .NET Framework, explained in [Microsoft's DateTimeFormatInfo reference](<https://learn.microsoft.com/en-us/dotnet/api/system.globalization.datetimeformatinfo>).

If a snippet header contains the "caret" keyword, EdSharp looks for a double caret sequence (^^) in the body, and positions the cursor (think of blinking caret) in that location after pasting.  An example text type snippet is called Letter.txt, located in the Default snippet folder.  Its content is as follows:

```
text form caret
%Date%

Dear %Customer=%:

Thank you for your purchase of %Product=Super Widget%.  ^^

Sincerely,
%UserName%
```

EdSharp notices that this is a text type snippet because of the first word of the file.  It finds two other keywords on the header line:  form and caret.  It creates a dialog with two edit boxes, prompting for the Customer and Product -- defaulting to Super Widget.  It substitutes the values entered, as well as date and user name constants.  After pasting, the cursor is positioned after the first sentence.

A section of the configuration file that supports snippets is called Tokens.  Each of these user-defined tokens is an expression in Microsoft JScript .NET:  a version of JavaScript explained in [Microsoft's JScript .NET reference](<https://learn.microsoft.com/en-us/previous-versions/visualstudio/visual-studio-2010/72bd815a(v=vs.100)>).

Three token examples are currently provided in the EdSharp configuration file.  The CurrentDirectory token illustrates a call to a static method in the .NET Framework Class Library (FCL) -- in this case, returning the current directory of the EdSharp process.  The Signature token shows syntax for a literal string -- in this case, a signature block with multiple lines.  The UnorderedList token refers to a JScript file called ul.js that is provided in the HTML snippet folder.  

When EdSharp finds that a token refers to a file in the current snippet folder, it interprets the content of that file as JScript.  The ul.js example creates an unordered list element in HTML after prompting for the number of items to generate in the list.  Its content is as follows:

```
[Begin Content of ul.js]
var iCount = Interaction.InputBox("Number of Items:", "Input", "0")
var sTag = "<ul>\n"
var i = 1
while (i <= iCount) {
sTag += "<li>Item" + i + "</li>\n"
i++
}
sTag += "</ul>\n"
[End Content of ul.js]
```

User-defined tokens may be included in a snippet that has the "form" keyword in its header.  They may also be typed in a document being edited.  A token expression may be tested with the Evaluate Expression command, Control+Equals, which evaluates the current line or selected text and places the result on the line below.  The Replace Tokens command, Control+Shift+Equals, swaps token names with their computed results in all or selected text.  

For example, you might press Alt+Shift+M for Manual Options and define a signature token as follows:

```
[Tokens]
Signature=("Sincerely,\nJohn Doe\nJohn.Doe@NiftyHomePage.com\n")
```

Then type %Signature% in your document where you want that to appear, and use the Replace Tokens command to do it.  Alternatively, put %Signature% in the body of a snippet containing the form keyword in its header, and paste that snippet with Alt+V.

## Working with Structured Text
Several commands work with a structured text document consisting of a table of contents that lists topics at the beginning of the document, followed by a section for each topic in the body.  Each topic in the table of contents is a line with the same text as the heading of its corresponding section.  A divider sequence of characters -- a line of 10 dashes followed by a form feed and line break, separates each section.  The next line after such a section break is the topic heading of the next section.  For an example of this structure, examine the EdSharp.txt file in the EdSharp program folder.  The default location is\
`C:\Program Files (x86)\EdSharp\EdSharp.txt`

Press Control+PageDown to go to the next section and read its heading, or Control+PageUp to go to the previous one.  The topic search that used to be on Control+F6 and Alt+F6 was removed in EdSharpNG: it looked for the form feed of a structured text file, of which a Markdown document has none, so it could only ever answer "Not found!".  Control+F6 now opens the Link List, described further down with the other Markdown commands.  Since 5.0.51 EdSharpNG also takes Control+F6 and Control+Shift+F6 away from the built-in MDI window cycle, so Windows can no longer use them to switch between open documents: that cycle used to fire before our own shortcut table saw the key, which would have made the list of links unreachable.  Use Control+Tab and Control+Shift+Tab to change window, and F4 for the list of open windows.  The way to reach a chapter is the heading tree on F6.  Footnotes left the F6 family entirely: Control+Alt+K travels between a footnote marker and its text.

In EdSharpNG a section is a Markdown heading, so press Control+Enter to start a new section: it inserts a heading prefix at the same level as the heading you are currently in, and you then type the heading text.  Before the first heading of a file it inserts a level 1 heading, the title.  Control+PageDown and Control+PageUp move to the next and previous heading of any level.  Hold Shift as well, Control+Shift+PageDown and Control+Shift+PageUp, to move only between headings of the same level as the section you are in, skipping subsections.  On arrival EdSharp speaks the text of the heading and then its level, for example "Installation, heading 3" -- the word "level" is not spoken, and the text under the heading is not read.  At the last or first matching heading the cursor stays where it is and EdSharp says "Last heading!", "First heading!", or, for the same-level commands, "Last heading at this level!" and "First heading at this level!".  The original dashes-and-form-feed section break of EdSharp is no longer inserted by this command.  Press F6 for Document Navigation: a tree of every heading in the document, with sub-headings hanging under their parent, the way a book reader or an old MHT help file presents a table of contents.  Up and Down Arrow move through it, Right Arrow expands a heading and Left Arrow collapses it or moves to its parent, typing letters jumps to a heading beginning with them, and Enter moves the cursor to the selected heading in the text and speaks it the same way the heading navigation commands do.  Escape closes the tree without moving the cursor.  Reopening the tree returns to the heading you were last on.  A document with no heading is not opened at all: EdSharp only says "No headings!".  Alt+T verbally confirms the topic of the current section.

Press Alt+Shift+T to write a table of contents at the top of a Markdown document.  It lists every heading, at every level, as an indented bullet list, and each entry is an internal link to its heading.  Those links keep working after export: pandoc turns them into anchors in HTML and into Word bookmarks in a .docx file, so a reader of the exported document can click an entry and land on the chapter.  Pressing Alt+Shift+T again rewrites the existing contents instead of adding a second one, so it is also the way to refresh the list after you rename or add a chapter.  The command works only on .md files, never writes to disk, and its change can be taken back with Control+Z.

Shift+F6 travels between the two, and which way it goes depends on where the cursor is.  From anywhere in the body it jumps to the entry of the section you are in, or to the top of the contents when you are before the first heading.  From an entry of the contents it jumps to that chapter and speaks it as the heading navigation commands do.  With no headings at all it says "No headings!"; when the document has headings but no contents yet, it says "No contents yet, press Alt+Shift+T to create it!".  The same internal links work in the Markdown preview under Escape: Enter on an entry moves to the chapter instead of opening a web browser.

### Links

Press Control+K to insert a link.  The box asks for the text the reader will see, for the address, and for the kind of link: an ordinary web or file link, an image, or a place in this document.  Pick a place in this document and you do not type an address at all -- instead you choose a heading from a list, and EdSharpNG works out the anchor for you.  It works it out exactly the way the table of contents does, including the suffix that tells two chapters of the same name apart, so a link made this way still lands on its chapter after export to HTML or Word.  A heading that pandoc gives no anchor to, such as one made only of punctuation, is left off the list rather than offered as a link that would go nowhere; if the document has no usable heading at all, the internal kind is not offered.

Whatever is selected when you press Control+K becomes the suggested link text, so the everyday way is to select a word and turn it into a link.  An image is inserted with the picture syntax, and its text is the description a screen reader will read, so EdSharpNG refuses to insert an image without one.  An ordinary link with no text of its own gets the address as its text, because a link that says nothing cannot be followed by ear.  The command works only on .md files, does nothing inside a code block, where the markup would be printed rather than followed, and its change can be taken back with Control+Z.

Control+F6 opens the Link List: every link of the document in the order it appears, each announced as its text and nothing else.  Since 5.0.65 the address is NOT in the row: Kasperczak chose to have Left Arrow speak it on demand, the same way other lists in this program fill in what the row does not carry, because a long address made every item take half a minute to read.  Since 5.0.67 the line number is not in the row either, and it went the same way, under Left Arrow next to the address.  It first stood at the front of the row, so every item began with the word "Line": the reader announced a coordinate instead of the link, and jumping through the list by first letter was dead because every entry started with the same letter.  Moving it to the end of the row was not enough -- the reader still said "line" and a number on every single item, twenty times over in a document with twenty links, which is what Kasperczak reported twice.  Content before coordinates is the rule everywhere in this program, in table cells and in named bookmarks alike.  A bare e-mail address counts as a link too, also in plain text files, so the list finds it and Control+Shift+C copies it with a mailto scheme so that it opens a mail program when clicked.  Enter moves the cursor to the link and speaks it the same way; the cursor lands on the TEXT of the link, not on the bracket, so the screen reader reads words rather than punctuation.  Escape closes the list without moving anywhere, and a document with no link at all is reported as "No links!" without opening an empty window.

Three keys work inside the list.  Control+C copies the link as Markdown, exactly as it stands in the file, for pasting into another document.  Control+Shift+C copies it as a formatted link, the kind Word and LibreOffice paste as a real clickable hyperlink; a link that has no web address, an internal jump to a heading for instance, cannot be formatted that way and says so instead of copying something misleading.  F2 opens a small box with the text and the address of the link, and typing new ones rewrites the link in the document; the list then reopens with the new wording, since every link after the edited one has moved.  Editing obeys the same rules as inserting: an image still needs a description, clearing the text of an ordinary link leaves the bare address, and the change can be taken back with Control+Z.  On a guarded document F2 refuses and says so.

Alt+PageDown goes to the next link and Alt+PageUp to the previous one, without opening any window: the list shows the whole document at once, these two keys walk it link by link.  They speak what the list shows, text then address then kind, since content comes before everything else here, and the cursor lands on the TEXT of the link rather than on the bracket.  Neither wraps around at the end of the document; at the last or first link the cursor stays where it is and the program says so, because a silent jump back to the beginning would send a reader who cannot see it searching again through links already read.  Both keys read the same parser as the list, so the two commands can never disagree about what counts as a link, and both work in plain text files as well -- a bare address in a .txt file is a real link.  Neither works while the preview is open, where the cursor moves through converted text and offsets from the source file have nothing to point at.  The whole PageUp and PageDown family is now consistent: the modifier picks what kind of element you travel between -- nothing for pages, Shift for bookmarks, Control for headings, Control+Alt for footnotes, Alt for links and Alt+Shift for comments -- and Shift alone reverses the direction.

The list finds both kinds of link Markdown knows: the bracket-and-parenthesis form, including image links, and plain addresses written straight into the text, whether angle-bracketed, beginning with http, or beginning with www.  Addresses inside a fenced code block are left out, because there an address is an example rather than a link -- the same rule the preview, the footnotes and the comments follow.  Unlike inserting a link, the list is not restricted to .md files: it only reads, and a plain address in a .txt file is a real link.  It is not available while the preview is open, where the element list on F7 already offers links with its own filter.

### Footnotes

A footnote keeps a remark out of the sentence that needed it.  EdSharpNG writes footnotes the Markdown way: a marker such as [^1] stands in the sentence and the text of the note stands at the end of the document, on a line that begins with the same label and a colon.  That is the syntax the converters shipped in the Convert folder already understand, so a footnote written here becomes a real footnote everywhere the document goes: an anchor with a return link in HTML, and a genuine Word footnote, the kind that appears at the bottom of the page, in a .docx file.

Insert Footnote opens a box with two fields: the text of the note, and where that text should land -- the end of the document, or the end of the chapter the cursor is in.  The second choice exists because a file with several chapters may later be split into separate files, and a note has to stay with its own chapter; when the document has no headings at all, only the end of the document is offered, because the other option would do nothing.  The marker appears where you were writing and the cursor stays in the sentence, so nothing interrupts the thought you were in the middle of.  The number is given automatically, one higher than the highest already used, and nothing has to be renumbered when you insert a note between two existing ones: Markdown numbers footnotes in the order their markers appear in the text, whatever the labels are.

Go to Footnote travels between the two ends, and which way it goes depends on where the cursor is, the same way Shift+F6 works for the table of contents.  From a sentence that carries a marker it jumps to the text of that note; from the text of a note it jumps back to the marker in the sentence.  It is enough to stand anywhere in the line that holds the marker, including at its very end.  Away from any footnote it says so and leaves the cursor where it is, naming the key that finds one: travelling between footnotes is a separate pair of keys, Control+Alt+PageDown for the next marker and Control+Alt+PageUp for the previous one.  Both of them were tried on Control+Alt+K first, together with the jump to the text of the note, and Kasperczak asked for them to be separated after using it: one key that sometimes moved to the text of a note and sometimes to the next note meant too much pressing to get anywhere.  Neither wraps around at the end of the document; they say "Last footnote!" or "First footnote!" and leave the cursor where it is, because a reader who cannot see the wrap would search again through text already read.  Footnote List, on Alt+K, shows every footnote of the document with its text, and Enter goes to the marker in the sentence.  Left Arrow on the list reads the line of text that carries the marker, without the marker itself, so a note can be placed in the document before choosing it -- the list shows the note, the arrow shows what it is about.  A footnote whose text is missing, or whose marker is missing, is reported as such rather than silently skipped.  With no footnote at all the commands say "No footnotes!".  All of them work only on .md files, and inserting requires an unguarded document; the change can be taken back with Control+Z, and nothing is ever written to disk.  Two places refuse a new footnote outright, because a marker put there would be a dead one: inside a fenced code block, where a marker is only ever example text and the converters ignore it, and inside the text of another footnote, where the note you wrote would vanish altogether from an exported document.  A heading is not refused: a footnote there survives export like any other, and a heading is where the cursor lands after every jump through the table of contents.  In the Markdown preview under Escape a footnote reads as "footnote" and its number rather than as the raw bracket and caret, both at the marker in the sentence and at the text at the end of the document, and Enter travels between the two exactly as Go to Footnote does in the editor: from the marker it moves the cursor to the text of the note, and from the text it moves back to the sentence.  A marker inside a fenced code block is left as written and Enter does not follow it, since there it is example text and not a footnote.

Export Footnotes, in the menu with no shortcut of its own, collects the footnotes of the document into a new editing window, and asks first in which of two shapes.  Footnotes only gives a numbered list of the notes themselves, which is what you want when the notes are sources or references and the sentences would only be in the way.  Footnotes with their sentences puts, above each note, the sentence in the document that carries its marker, which is what makes the export readable on its own: a remark without the thing it remarks on is rarely worth reading.  The sentence is quoted without the marker, since the number stands beside it anyway, and its boundaries are the ones the editor already uses when you move by sentence.  The order follows the markers in the text rather than the labels, which is also the order in which the exported document will number them.  A note whose marker is missing is not dropped: it lands at the end, under a heading that says so.  The result goes to a new window rather than to a file or the clipboard, so it can be read straight away with the reader's arrows, saved under any name, or copied whole -- and the document it came from is not touched at all.

### Internal comments

An internal comment is a working remark for you alone.  It stays in the Markdown file and it is dropped on the way out: converted to Word, to a web page or to plain text, the remark simply is not there.  That is the whole point of it, and it is what makes it different from a Word comment in the margin, which EdSharpNG does not write.  Measured with the very converters that ship in this package, so this is a property of the program rather than a promise: the remark survives as a page comment in the preview, and its text is absent from a .docx document and from plain text output.

Insert Comment, in the Miscellaneous menu, either inserts a new comment or edits the one the cursor is in, and which of the two happens depends on where the cursor stands -- the same principle the table wizard follows on Control+Shift+T.  The title of the box says which it is, Insert Comment or Edit Comment, because that is where a screen reader hears it without having to ask.  Standing at the very end of a comment counts as standing in it, since that is where the cursor is left right after one is inserted.  Clearing the field of an existing comment removes that comment, and the program says so; a comment cannot be left empty by accident, because an empty box on a new comment inserts nothing at all.  A closing sequence typed inside the text is made harmless before it is written, otherwise it would end the comment early and spill the rest of the remark into the visible document.

Next Comment goes to the next comment and Prior Comment to the previous one, reading the remark and then the word comment -- content first, as everywhere else in this program.  Neither wraps around: at the last or first comment the cursor stays where it is and the program says so, because a silent jump back to the beginning would send you looking again for a remark you had already read.  Comment List shows every comment with the line it sits on, and Enter goes to the chosen one; the line number is given rather than a count, since what you are looking for is a place in the document.  With no comment at all the commands say "No comments!".  A comment inside a fenced code block is deliberately not treated as a comment: there it is example text, the preview shows it as visible text, and jumping through it would mislead.  Next Comment is on Alt+Shift+PageDown and Prior Comment on Alt+Shift+PageUp, the same pair that walks the other kinds of marker, so moving between remarks needs no menu.  Insert Comment and Comment List have no shortcut: the keys they used to occupy -- Alt+F9 and Control+Alt+F9, as well as the earlier Control+Shift+F9, Alt+Shift+F9, bare F9 and Shift+F9 -- were given up on your decision, since inserting a remark or listing them all is not done often enough to hold a chord.  Control+Alt+F9 was the worse of the two: on a Polish keyboard Control+Alt is what the right Alt sends, so the chord swallowed accented letters.  Both commands are still in the Navigate menu and in the command palette, so the keys can come back whenever you want them. All four commands work only on .md files and not while the preview is open, inserting requires an unguarded document, and every change can be taken back with Control+Z, with nothing written to disk.

A Markdown document can also be read in a rendered preview.  Press Escape to open the preview of the current .md file, and Escape again to return to editing -- Escape is always the way back, whichever way you entered.  In the ordinary preview the two views follow each other, so moving the cursor in one moves it in the other.  Press Shift+Escape instead to open the preview detached, meaning the editing cursor stays where you left it while you browse; on returning to editing you land back where you were.  While the preview is open, Shift+Escape never leaves it: it switches the synchronization on and off in place, saying "Synced" or "Detached", so the two keys cannot get in each other's way.

You can adjust the LimitItem configuration setting to perform comparison operations on sections rather than lines of text.  For example, press Control+Comma for Configuration Options, and Alt+S for the SectionBreak setting.  Since the text is initially selected, press Control+C to copy it to the clipboard.  Then press Alt+L for LimitItem, Control+V to paste, and Enter to save settings.  Now you can sort sections alphabetically with the Order Items command (Alt+Shift+O), reverse them with the Reverse Items command (Alt+Shift+Z), or eliminate duplicates with the Keep Unique Items command (Alt+Shift+U).

## Word Processing
EdSharp supports several aspects of Rich Text Format (.rtf) as well as plain text (with optional structure).  In certain situations, EdSharp behaves differently if a file has a .rtf extension rather than any other one.  Specifically, opening a .rtf file with Control+O offers a list of ways to show it, including keeping its formatting.  The Save, Save As, and Save Copy commands, Control+S, Control+Shift+S, and Alt+S, save a .rtf file with formatting preserved.  The Print command, Control+P, prints a .rtf file using the associated program for this operation in the Windows registry (typically Microsoft Word or WordPad).  Use the Copy Rich Text command, Control+Shift+C, to copy selected text with formatting to the clipboard.

In a Markdown or plain text file that command does more than copy: it converts what you copied into real formatting, so that pasting into Word gives a formatted document rather than text full of asterisks.  Select several lines and every heading becomes a Word heading, every run of list items becomes a real Word list, `**bold**` and `*italic*` become bold and italic, `~~struck~~` becomes struck through, and a Markdown link becomes a clickable Word hyperlink whose visible text is the link title.  A fenced code block is copied verbatim, because an asterisk in program text is an asterisk and not an emphasis marker.  With the cursor on a single line the command behaves as it always did, copying the link, the list or the formatted line at the cursor, which is more precise than a whole-selection conversion could be.  Text with no Markdown markup at all is left to the ordinary copy, so nothing changes for plain notes.  The clipboard always carries plain text as well, so a program that cannot read formatting still receives the words.

The commands that SET rich-text formatting were removed in 5.0.107 at the request of Michal Kasperczak, who observed that they "are for rich formatting, so in ordinary text files they make no sense".  Justify (Alt+Shift+J) set left, bullet, centre or right alignment; Style (Alt+Slash) set bold, italic or underline; Baseline (Alt+Shift+F6) made a subscript or superscript.  All three wrote properties of an RTF file, and in Markdown -- the format this editor works in -- emphasis is written as asterisks IN THE TEXT, so the setting had nothing to save and was lost when the file was written.  To make text bold in Markdown, type the asterisks: `**bold**`.  The Selection Font command, formerly Alt+Shift+Dash, was removed in 5.0.99, and Default Font in the same release.

EdSharpNG no longer walks changes in RICH-TEXT formatting.  Stock EdSharp jumped between changes in justification, baseline, style and font on Control with the bracket, slash, dash and function keys; those eight commands were removed at the request of Michal Kasperczak on 29 August 2026, because EdSharpNG is a Markdown editor and in Markdown emphasis is written as asterisks in the text itself, so there is no font or style change to land on.  The dialogs that SET formatting followed them out in 5.0.107, for the same reason.

Two of those keys came back on 30 August 2026 with a different job: walking the asterisks THEMSELVES.  Press Control+Slash to go to the next piece of bold or italic text, or Control+Shift+Slash for the previous one.  One command finds both kinds, because Michal Kasperczak asked for "bold and underline together" and did not want a separate command for italics alone.  The cursor lands on the TEXT, not on the asterisk, and speech reads the emphasised words followed by their kind: bold, italic, or bold italic (one marker means italic, two mean bold, three mean both).  Underscores count as well as asterisks, which is how Markdown works, but an underscore inside a word is left alone so that a file_name_like_this is not mistaken for emphasis.  Asterisks inside a fenced code block are skipped, and so is the asterisk that starts a bullet, because it opens a list rather than emphasis.

Press Control+Dash to go to the START of the next list, or Control+Shift+Dash for the previous one.  These commands move BETWEEN lists rather than between items: within a list the Down Arrow already does the job, so stopping on every bullet would only slow you down.  Speech reads the first item of the list and how many items it has.  A blank line ends a list, the same rule the preview uses, so both agree on where one list stops and the next begins.

To move to other pieces of Markdown, use the structural commands: Control with PageUp or PageDown for headings, F6 for the document tree, F7 in preview for the element list, Control+Shift+K to write a footnote, Control+Alt+K and Control+Alt+Shift+K to move between footnotes, and Alt+K for the list of them.

To query styles, baseline, and justification, press Alt+Slash; it reports what is under the cursor.  The Font query, formerly Alt+Dash, was removed in 5.0.99: a screen reader answers the same question better and everywhere, with NVDA+F in NVDA.

## Doing Math
You can press Control+Equals to evaluate mathematical expressions in JScript code, either on the current line or in selected text.  Control+Shift+G goes to another, more interactive environment, such as the interactive console of Python or iPython.  The latter is particularly useful for learning the library of the .NET Framework and testing expressions that may be incorporated in programming code, including EdSharp snippets.  It may also be used as a simple, speech-friendly calculator.

Alternatively, the GoToEnvironment setting could be configured for a computer algebra system such as [Maxima](<http://Maxima.SourceForge.net>),
or [Axiom](<http://Axiom.SourceForge.net>).

### LaTex
EdSharp includes support for the LaTeX language (pronounced La Tech).  This is a common language used for typesetting, especially for scientific publications.  About 30 sample LaTeX snippets, ending in a .tex extension, are distributed with EdSharp.  You can convert from LaTeX to RTF and vice versa.  To fully work with LaTeX, install the open source [MiKTeX](<http://www.miktex.org>) package for Windows.

With that installation, EdSharp's LaTeX compiler option lets you check and correct syntax.  You can then export to PDF or XML -- in this case, XHTML containing embedded MathML (math markup language for the web).  If the resulting .xml file is opened in Internet Explorer with a screen reader, sophisticated mathematical statements will be intelligible when the free MathPlayer add-in has been installed from the [MathPlayer download page](<http://www.dessci.com/en/products/mathplayer/download.htm>).

## Programming
Press Tab to indent the current line of text, or Shift+Tab to outdent it.  If multiple lines of text are selected, these commands are applied to all of them.  The Trim Blanks command, Control+Shift+Enter, removes all indentation and trailing spaces at once, as well as removing more than two consecutive blank lines (when multiple lines are selected).

Press Alt+I to hear the number of indentation levels of the current line.  Alt+Shift+I toggles a mode in which you are alerted to changes in indentation level, such as when using the up and down arrow keys.  EdSharp will say how many levels in or out the indentation has changed.  This mode also reverses the rols of the Enter and Shift+Enter keys.

When Indent Mode is off, you can start a new line of text with the same indentation as the current one by pressing Shift+Enter.  By default, an indentation unit is one tab character.  This may be changed with the Configuration Options command, Control+Comma.

To go to the first character of the current line after any indentation, press Alt+Home.  To go to the last non-white space character, press Alt+End.

Use the Infer Indent command, Alt+RightBracket, to hear what indent unit the current document seems to be using.  EdSharp looks at the first line that starts with a space or tab character.  If this key is pressed again without moving the cursor, that sequence of space or tab characters is configured as EdSharp's IndentUnit setting.  This makes it easy to use the same indentation style as a file you have opened.

Press Control+I to go to the next change in indentation, or Control+Shift+I to go to the previous one.  EdSharp skips blank or commented lines with these commands.  The older block commands, which walked whole blocks of code by nesting level, have been removed from EdSharpNG: indentation in a Markdown document marks a list item or a quotation, not the structure of a program, so there was nothing to walk.  Control+B now sets a bookmark.

Control+I stops wherever the indentation changes, so inside a loop body it takes you to the line where a lower level of indentation resumes.  In Ruby, this would be the line with the word "end".  In Python, it would be the first line of code following the loop, since the change in indentation, itself, indicates the end of the loop.  

The related query command, Alt+I, helps you understand indentation without moving the cursor.  It provides additional information when pressed a second time in a row.  Alt+I says the indentation level of the current line.  When toggled, it reads the text of the preceding line with less indentation, which is typically the statement that introduced the current block, e.g., an if, for, or while statement.  The matching Alt+B query, which said the rest of the current block of code, is gone: Alt+B now shows the list of bookmarks.

The Quote and Unquote commands, Control+Q and Control+Shift+Q, may be used to add or remove comment symbols at the start of lines.  The default quote prefix may be changed from > to a comment sequence appropriate for the language in use, e.g., ' for Visual Basic, * for Xbase, ; for AutoIt, or # for Ruby.  

Curly brace characters delimit code structures in a number of languages.  Press Control+Shift+RightBracket to find the matching right brace (}) character from the current location.  Press Control+Shift+LeftBracket for the matching left brace ({) instead.  Press Alt+Shift+RightBracket to hear the number of unmatched left braces before the cursor and right braces after.  Different brace characters may be configured, e.g., angle brackets (<>) for editing HTML or XML.  If the cursor is on a brace-type character when issuing one of these commands, i.e., one of {}<>[]() , then EdSharp uses that character and its opposite when searching, regardless of the current setting.  In addition, Control+Shift+. (think of the > symbol) goes to the matching end tag of an HTML element, and Control+Shift+, goes to the start tag.

Two commands specifically aid programming in the Python language with speech.  This language requires indentation for subordinate code blocks, and the indentation, itself, rather than words or punctuation, is how such structure is specified.  The indentation is helpful to sighted programmers, and often to users of large print or braille, but generally inefficient for speech users, since such access tends to be serial rather than two dimensional in nature.  The PyBrace command (Alt+Shift+LeftBracket) converts indented structure to a form explicitly indicated by opening and closing braces, similar to C-like languages.  The PyDent command (Alt+LeftBracket) does the reverse, converting from PyBrace to indented format that is understood by the Python interpreter (using the current IndentUnit setting).

The PyBrace and PyDent formats include comments that indicate when a block has closed , e.g., "# end for" on the line after a "for" block.  If text is selected, these commands replace it with the alternate format;  otherwise, they create a new editing window so the original file is still available.

A scripting language allows a program to be run as a text file associated by extension with its interpreter, e.g., .py for Python, .au3 for AutoIt, or .rb for Ruby.  Press F5 to run the current file with its associated interpreter.  If the current file name has a complete path, EdSharp saves to disk before running the file to ensure the latest version is being used.  Otherwise, EdSharp saves to a file in a temporary folder and runs that file.

The Alt+F5 command prompts for a command to run and speaks its standard output.  The path of the current file may be passed via the syntax described for EdSharp's Import and Export capability.  The command remembers its previous value, and may be adjusted each time it is run.  Use the Review Output command, Alt+Shift+F5, to open a new editing window containing the output produced by the last command.

Use the Compile command, Control+F5, for a programming language that involves compiling source code to binary form.  For example, a C# program in a .cs file may be compiled to a .exe file.  This command may also be used for interpreters that report syntax errors via the standard output or standard error streams.

These tool commands typically begin with the file name of the compiler or interpreter.  Any parameters may be specified thereafter.  If the token %SourceDir% is included, EdSharp temporarily changes to the directory containing the source file before running the tool.

The first line and column position mentioned in the output, if any, is assumed to be the position of a compilation error in the source code.  EdSharp uses the JumpPosition setting to find the position in the output based on a regular expression.  The regular expression should be defined so that the first number of a matching string is the line number and the second number, if any, is the column number.  EdSharp automatically jumps to that position.  It is also saved so that the Jump Again command, Alt+J, returns there.

An indentation error is treated differently on purpose.  Python reports IndentationError and TabError with its marker at the end of the offending line, but the fault is the whitespace at the START of it, so EdSharpNG puts the cursor in column one, where the correction belongs.

With no compiler configured, Control+F5 still works on two kinds of file out of the box.  A C# (.cs) file is compiled with the newest C# compiler found on the machine.  A Python (.py or .pyw) file is run with a real Python interpreter: Windows keeps a stub named python.exe of its own, which runs nothing and opens a Microsoft Store advertisement instead, so EdSharpNG skips that folder and looks for a genuine installation.  For Python, the output is also shortened before it is spoken: the path of the file, repeated in every traceback frame, and the "Traceback (most recent call last)" banner are removed, so speech begins at the line number and reaches the error message itself immediately.  Choosing a compiler with Control+Shift+F5, or setting AbbreviateOutput yourself, overrides both defaults.

The NavigatePart setting remains in the compiler configuration for compatibility with the original EdSharp, but no command in EdSharpNG reads it any more.  The commands that used it (Next Part on Alt+PageDown, Prior Part on Alt+PageUp and Go to Part on Alt+Shift+G) were removed on 27.08.2026, because they only ever matched a pattern configured per programming language and had nothing to find in a Markdown document.  The two freed chords did not stay free: Alt+PageDown and Alt+PageUp now travel between links.  Document structure is described by Markdown headings instead: Control+PageDown and Control+PageUp move between them, F6 opens the heading tree, Alt+Shift+T writes a table of contents and Shift+F6 jumps between the contents and the chapter.

Thus, the Compile command, Control+F5, combines debugging steps efficiently by compiling, saying output without a modal message box, and automatically jumping to the first error position, if found in the output.  The output spoken may be abbreviated by means of a regular expression setting that specifies the pattern of text to remove.  The Pick Compiler command, Control+Shift+F5, lets you conveniently configure the CompileCommand , AbbreviateOutput, JumpPosition, NavigatePart, and QuotePrefix ssettings for a particular compiler or interpreter.  EdSharp offers settings for the following languages:  Boo, C#, HTML, Java, JAWS Script, JScript .NET, LaTeX, Perl, PHP, PowerBASIC, PowerShell, Python, Ruby, and Visual Basic .NET.

The name of a tool to be run should either include its directory location or be available on the Windows search path.  This may be adjusted by editing the Path environment variable in the Advanced tab page of the System applet in Control Panel.  If the tool is a long file name enclosed in quotes then either prefix the command line with the @ symbol or enclose the whole thing in quotes.  This is necessary to prevent .ini file manipulation functions of Windows from losing the opening quote before the tool.

For HTML, the HTML Tidy utility is configured by default and distributed with EdSharp.  After eliminating coding errors found with Control+F5, use Alt+Shift+E to export to a target file containing clean HTML.  More information is available at the [HTML Tidy project page](<http://tidy.sourceforge.net>).

For PowerBASIC, a batch file is needed (in the EdSharp program folder), which refers to the default location of PowerBASIC for Windows version 10.0.  The path to the JAWS script compiler is also hard coded for the latest version.  JAWS scripting is additionally supported by EdSharp's own scripts:  Control+I is a hot key for inserting the path to the user script folder, and Control+Shift+I is for the All Users script folder, when focus is in the Open or Save Dialog of EdSharp.

Compiler settings are stored in the [Compilers] section of the EdSharp.ini file.  Only current compiler settings appear in the configuration options dialog, Control+Comma.  Other settings may be edited, however, using the Manual Options command, Alt+Shift+M.  You can adjust command line parameters of configured compilers, or add others.  Installing a new version of EdSharp does not change existing compiler settings.

## Scripting Add-Ins
Almost the complete object model of the EdSharp application has been exposed to add-in code in the JScript .NET language, explained in [Microsoft's JScript .NET reference](<https://learn.microsoft.com/en-us/previous-versions/visualstudio/visual-studio-2010/72bd815a(v=vs.100)>).

JScript.NET is a version of JavaScript with access to the huge library of the .NET Framework.  In EdSharp, JScript code may be used in the Evaluate Expression command (Control+Equals), Replace Tokens command (Control+Shift+Equals), and Paste Snippet command (Alt+V).  Stand-alone JScript executables may also be created with the JScript .NET compiler (jsc.exe), which is included with free .NET developer tools.

Using the Compile command (Control+F5) is the best way to debug JScript code even if you want to use it as an add-in rather than stand-alone executable.  This is because the JScript compiler provides error information that is not available when add-in code fails to execute due to syntax errors.  When writing code in this way, you will probably want to import one or more namespaces to abbreviate .NET references, e.g., by copying statements at the top of the Eval.js file in the EdSharp program folder.  Comment out such import statements in the debugged, add-in version of the code, since EdSharp already calls them internally.

The EdSharp object model includes a hierarchy of classes corresponding to the overall application, multiple document interface (MDI) frame, MDI child windows, and RichTextBox (RTB) within each window.  Typically, a script will manipulate text in the current RTB control.  The Frame property of the App class refers to the single MDI frame.  The Child property of that frame object refers to the active MDI child.  The RTB property of that child object refers to the current editing control.

  Thus, a JScript routine might start by creating one or more object variables as follows:

```
var frame = App.Frame
var child = frame.Child
var rtb = child.RTB
```

By convention, .NET properties are initially capitalized, whereas field and local variables are not.  Methods of the frame object can invoke menu items, e.g., a new editing window could be created with the following statement:\
`frame.menuFileNew.PerformClick()`
Methods and properties of an RTB (RichTextBox) object are explained in [Microsoft's RichTextBox members reference](<https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.richtextbox>).

EdSharp also adds some methods and properties in its inherited version of the RichTextBox class, e.g., the ReplaceRange method for replacing text between two points in the current document.  Other EdSharp classes provide convenient scripting methods, e.g., Dialog.Pick gets a user choice from a listbox and Util.String2File saves a string of text to a file on disk.

These classes will be further documented based on questions received.  At present, the best way to learn them is to examine code in sample .js snippet files and the main EdSharp.cs program file, which implement behavior you experience when running the application.  Although the .cs code is in the C# language, its syntax is similar to JScript, and the names of classes, methods, and properties are the same.

## Miscellaneous
EdSharp never assigns a keyboard shortcut to a chord that types a character on your keyboard layout.  This matters on a Polish keyboard, where the right Alt key is the same thing as Control and Alt held together: nine Polish letters are typed that way, a with an ogonek on the A key, c with an acute on C, e with an ogonek on E, l with a stroke on L, n with an acute on N, o with an acute on O, s with an acute on S, z with an acute on X, and z with a dot on Z.  A command placed on one of those chords would quietly take the letter away, and you would find out only when a word could no longer be written.  The program therefore asks the keyboard layout itself which chords type a character, and refuses such an assignment with an alert at startup rather than shadowing your typing.  This is why the commands that do use Control and Alt sit on keys that type nothing: K for the footnote jumps, in both directions, F9 for the comment list, and the arrow keys for moving a section.  The check covers Control and Alt held with Shift as well, since that is how a capital accented letter is typed, so Control+Alt+Shift+K was measured against the layout in the same way as the plain chord.  On a keyboard with no such letters the check simply never fires, since the layout, not a fixed list, decides.

The Extra Speech Toggle command is gone as of 5.0.65: Kasperczak asked for its removal, saying it was mainly a JAWS matter.  Because the setting used to be remembered on disk, removing the toggle alone would have left anyone whose extra speech was off with no way back, so the upgrade also drops the ExtraSpeech key from the settings file and extra speech returns to its default.  The one part of that setting which is NOT about speech, the dash that silences indent change announcements, is preserved.  Messages that used to be redirected when extra speech was off still go to a text file in the EdSharp data directory called Speech.log, which may be examined in an editing window with Alt+Shift+X.  This file is initialized when EdSharp starts.

With the optional JAWS scripts, you can toggle a speech setting of reading all or no punctuation using JAWSKey plus the grave accent at the top left of the main keypad (U.S. keyboard).  All punctuation is useful when reading carefully for details whereas no punctuation is useful when reading quickly for concepts.

Word wrap is on the Miscellaneous menu, as the Word Wrap and Unwrap commands; neither has a keyboard shortcut, since wrapping is rarely toggled while writing.  Use the Guard Document command, Control+F7, to turn read-only protection on or off, preventing accidental modifications.  One key does both directions, so the command announces the RESULT rather than its own name: it says "Guard on" when the protection goes on and "Guard off" when it comes off.  Guarding is not available while the Markdown preview is open, since the preview guards the document itself; the command says "Close the preview first!" there.  Wrap and guard settings are restored the next time a file is opened.

### Finding a Command Without the Menus

Press Control+Shift+X for the Command Palette.  Type any part of a command name and the list narrows as you type; Enter runs what is selected.  Each entry shows its keyboard shortcut, so the palette doubles as a way to learn the shortcuts for what you actually use.  This is faster than walking a menu tree with several hundred commands in it, particularly when you know the name but not the menu.

### Opening a CSV File as a Table

A file of comma-separated values reads badly as plain text: one long line per record, with the columns separated by commas you have to count.  When you open a `.csv` or `.tsv` file that really looks like a table -- several rows, at least two columns -- EdSharpNG offers to show it in the table grid instead, the same grid used by Insert Table.  There your screen reader names the column heading at every cell.  Answer No and the file opens as ordinary text, because a CSV file is sometimes just text you want to edit.

In the grid, Right Arrow from the last column adds a column and Down Arrow from the last row adds a row.  F2 edits a cell, Delete clears it, Control+Enter saves the table back to the file, and Escape asks before discarding changes.  You can also reach this deliberately with the Edit CSV as Table command on the Miscellaneous menu.

### Polish and Other Legacy Encodings

Older Polish text files use encodings that predate Unicode: Mazovia (the Polish DOS layout), Latin II (code page 852) and Windows-1250.  All three are on the encoding lists used by Yield Encoding, opening and saving, so text from those files reads correctly instead of showing damaged letters.  Mazovia is handled by EdSharpNG's own code, since Windows itself no longer knows that code page.

### Work Continuity

EdSharpNG can bring your work back after a restart, a crash, or a power cut.  Both halves of that are OFF until you ask for them, because not everyone wants a program keeping copies of their text.  Open the Work Continuity command on the Miscellaneous menu; it has no keyboard shortcut, since it is something you set once rather than a command you repeat.  The window holds two check boxes and one number, so your screen reader states the current setting as you move through it -- there is nothing to look up in a settings file.

The first check box restores your open files on startup, along with the cursor position in each one and any bookmarks you had set.  The state is written while you work, not only when you exit, so it survives the program being killed or the machine losing power.

The second check box turns on auto save, and the number below it sets how many seconds pass between saves, from five to an hour, thirty by default.  Auto save does NOT write to your document.  It keeps a recovery copy in EdSharp's own data directory, so declining to save on exit still discards your changes exactly as it always did; nothing is decided behind your back.

When EdSharpNG starts and finds unsaved changes from a session that ended badly, it asks whether to bring them back, and the question says how many files are waiting and when they were last saved, so you are not choosing blind.  If every file was saved normally, they simply reopen without a question.  Recovery copies older than a fortnight are cleared out on their own, and turning the feature off deletes the stored session together with every copy.

One more consequence: the empty NoName document is created only when there is nothing to restore.  Previously it appeared first, every time, so restoring a session left you with your files AND a blank window to close.

EdSharp checks whether the file in the current editing window has been modified by another program since being loaded from disk.  If so, you are prompted whether to open it again (like what Alt+O does manually).  If you answer No, version checking on the current file stops until you save or reload it.

Use the Repeat Line command, in the menu with no shortcut of its own, to make a copy of the current line directly below it.  Control+Y used to carry it and now redoes an undone change instead, the way most editors use that key.  The cursor is placed at the start of the new line.  This is useful for creating a new line of text by editing a previous line that is similar.

Press Control+Equals to evaluate either the current line or selected text as an expression in the JScript.NET language.  This command is useful for mathematical calculations.  For example,  the following algebra calculates the cumulative total of an initial 100 deposit compounded for 10 years at an annual rate of 5% interest:\

```
var interest = 1.15
var deposit = 100
var years = 10
Math.Pow(interest, years) * deposit
162.889462677744
```

The result, about $163, was placed on the line below the previously selected text, and the cursor was placed at the start of that line.

The Transform Files command, Alt+Equals, applies a saved job of search and replace tasks to one or more files -- typically to massage data or formatting in predictable ways.  The current document holds the list of files to process, one path per line, and EdSharp prompts for the job file.  A job is an .inix file (it may carry a .ini or .inix extension) with one bracketed section per task.  Each section has a Find key (the regular expression to match) and then either replaces or extracts.  A Replace key gives the replacement text, in which \n, \t, and similar escapes are honored, the usual .NET group references such as $1 work, and $# inserts the running match count; an empty Replace deletes the matched text.  Alternatively, an Extract key set to true collects each match to the clipboard without changing the file, separated by the optional Divider key (a form feed and newline by default).  An optional Options key takes a comma-separated list of .NET regular expression option names such as multiline, ignorecase, or singleline.  Because the .inix format supports multi-line values, a Find or Replace expression may span several lines.  This is the same job format processed by the companion Regexer tool.  When you run a job, EdSharp offers Test (count matches only), Run (apply the changes), and Verbose (apply with per-task detail) modes.

Before invoking this command, the current editing window should contain the list of files to process, one per line.  Such a list could be typed manually or generated via the Path List command (Control+Shift+P).  If a file does not include a leading path, the prior one is assumed.  

An intervening dialog lets you test what changes would occur without actually performing them.  In either case, you can subsequently use the Review Output command, Alt+Shift+F5, to examine the change log.

Here is a sample job with three tasks -- a deletion, a replacement, and an extraction:\

```
[Trim trailing whitespace from each line]
Options=multiline
Find=[ \t]+$
Replace=

[Collapse runs of blank lines to a single blank line]
Find=\n{3,}
Replace=\n\n

[Collect every Markdown heading to the clipboard]
Options=multiline
Find=^#{1,6} .+$
Extract=true
```

The first task has an empty Replace, so it deletes each match; the second supplies replacement text; the third collects matches to the clipboard instead of changing the file.  A copy of this job ships as Transform_Example.inix in the EdSharp program folder.

Press Alt+Shift+Semicolon to insert the current time and date at the cursor position.  The configurable format defaults to text like the following:\
`5:43 AM Sunday, May 13, 2007`

Configuration settings let you adjust the date and time formats according to templates explained in [Microsoft's custom date and time format reference](<https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings>).
Specify a setting of 0 for the date or time component to be excluded.

EdSharpNG has no Calculate Date command.  Stock EdSharp prompted for a year, month, week and day on Control+Shift+Semicolon and inserted the resulting date; it was removed at the request of Michal Kasperczak on 29 August 2026, as a calendar calculator rather than work on text.  Inserting the current date and time on Alt+Shift+Semicolon, described above, is unchanged.

In a structured text file, the first line is assumed to be the title of the resulting web page.  Lines beginning with a web or email address are converted to links.  Typically, you would use this command after creating sections with Control+Enter and generating a table of contents with Alt+Shift+T.

Use the Text Combine command, on the Miscellaneous menu, to convert a list of files in the current editing window and append them as text in a new document.  It has no keyboard shortcut, because Control+Shift+T now opens the table wizard.  The Text Convert command, which wrote a .txt file next to each source file on disk, has been removed from EdSharpNG altogether: it was the one command here that wrote to disk without asking, and on a document that is not a list of file paths it reported an unexpected event.  Control+T is free.

Two commands write Markdown lists.  Press Control+L for a bulleted list and Control+Shift+L for a numbered one; they work on the current line, or on every line of the selection.  Both are toggles, and both toggle back to PLAIN TEXT rather than to the other kind of list: pressing Control+L on a list that is already bulleted strips the bullets, which is also how you leave a list you did not mean to start.  Changing a bulleted list into a numbered one is therefore two presses, through plain text, and that is deliberate -- one key that cycled through three states would leave you guessing which of them you are in.  Neither command works inside a fenced code block, where a line beginning with a dash is program text rather than a list item, and EdSharp says so instead of quietly changing your code.  In a rich text file the same two keys set real bulleted and numbered formatting instead of writing Markdown.  These lists used to live on Control+Shift+7 and Control+Shift+8; both of those are now free.

To work on a table in a Markdown document, press Control+Shift+T.  What happens depends on where the cursor is: on a line of an existing table the wizard opens THAT table, with your cells already in it, and the window is titled Edit Table; anywhere else it starts a new table with a single cell, titled Insert Table.  The wizard grows as you type, so you do not have to know the size in advance: the arrow keys move between cells, Right Arrow from the last column adds a column, and Down Arrow from the last row adds a row.  Typing on a cell replaces what is in it; F2 opens what is already there for correction, and Enter confirms it.  Delete clears a cell.  The first row is the table header, which Markdown requires; each cell says its own position as you move, header row for that first row and row 1, row 2 and so on for the rows of data, so no number counts from zero.  A cell announces its CONTENT first and its position after it, in ONE utterance per key press: sideways you hear the text, the column and then the row, and vertically the text, the row and then the column, because the coordinate that just changed is the one worth hearing first.  An empty cell says blank, since silence cannot be told apart from a key that did nothing.  Press Control+Enter to put the table into the document, or Escape to close the wizard without changing anything; Escape asks first if you have changed something, and closes at once if you have not.  Editing an existing table replaces exactly the lines that table occupies, so nothing around it moves and Control+Z undoes the whole change in one step.  Column alignments written in the dashes row are kept, and a vertical bar you type inside a cell stays part of the cell text.  Empty columns and rows at the end are left out, so an extra press of an arrow key costs nothing.  The table is written in Markdown pipe syntax, which the Convert tools turn into a real table with proper header cells in HTML and in Word.  A table with a single column counts as a table too.  The command works on Markdown files, on a document that is not guarded, and outside the preview; a table inside a fenced code block is left alone, because there it is text to display rather than a table.

To make navigation more flexible and efficient in a listbox with many items, EdSharp adds the following features to its list-based dialogs.

Control+J prompts for text within an item, going to the first match if a new search, or the next match if the previous value is accepted.  Alt+J goes to the next match without prompting for a value.  The item with focus when the dialog is closed -- but not canceled -- becomes the current item the next time that the same list dialog is invoked (you are notified when it is not the first item).  The Jump value of that dialog is also remembered.

Control+F sets a filter to restrict what items are shown via wildcards (* to match any sequence of characters or ? to match a single one).  For example, you could browse replace-related commands in the Alternate Menu, Alt+F10, by pressing Control+F after invoking that list and then entering *replace* as the filter expression.  Control+Shift+F clears the filter so all items are shown again.  The order of items may also be changed:  Alt+A for alpha order, Alt+Shift+A for reverse alpha order, Alt+D for default order, or Alt+Shift+D for reverse default order.

Multiple commands support flexible checking or unchecking in a checked listbox, such as the dialog that picks files to convert.  Press Space to toggle the checked state of the current item, Control+A to check all items, or Control+Shift+A to uncheck all.  Press Shift+DownArrow for check and Next, or Shift+UpArrow for check and Previous.  Press Shift+End for check to Bottom, or Shift+Home for check to Top.  Shift+NumPad5 checks the current item.  F8 marks the start of a checking operation, completed with Shift+F8.

Adding the Alt modifier key performs the same action except for uncheckging rather than checkging.  Thus, Alt+Shift+NumPad5 unchecks the current item, Alt+Shift+Home unchecks to the top of the list, Alt+Shift+End unchecks to the bottom, Alt+Shift+DownArrow unchecks en route to the next item, and Alt+Shift+UpArrow unchecks en route to the previous.  F8 then Alt+Shift+F8 unchecks items in that range.

Other arrow keypad actions navigate among checkged items.  Control+Home goes to the top checkged item, and Control+End goes to the bottom one.  Control+DownArrow goes to the Next , and Control+UpArrow goes to the previous.

Shift+Space tells you what items are currently checked.  Alt+A says the address of the current item in the list, e.g., 11 of 42.

Press Control+Comma to adjust configuration options of EdSharp through a dialog.  Each option has a unique access key in its label, so you can jump directly to it with an Alt plus letter combination.

Access keys work this way in every dialog of EdSharp, not only in configuration options.  Each field, checkbox and action button carries its own Alt plus letter, so you can reach any of them directly instead of tabbing past the fields before it.  The letter is the first letter of a word in the label, which is what a screen reader announces along with the field name.  Where two labels would want the same letter, the second one takes the next free word initial, and if every initial is taken it goes without a letter rather than answering the same key as another field.  The OK and Cancel buttons deliberately have no access key, because Enter presses OK and Escape presses Cancel from anywhere in the dialog.

You can configure whether EdSharp's application window is maximized at startup, and whether an editing window is word wrapped when created -- the default is Yes for these options.  When a file is saved without giving it an extension, .rtf is added as a configurable default.  If a file would be overwritten, the original may be optionally saved with .bak added (default is No).  The OpenPrevious option determines whether files open at the end of the previous session are automatically opened at the start of the next one (default is No).  Another option limits the number of files shown with the Recent Files command, Alt+R (default is 100).

The HardPageAddress option determines whether the Address command, Alt+A, gives a page number instead of document percentage (default is No).  A form feed character specifies a hard page break.  In EdSharpNG the Section Break command, Control+Enter, inserts a Markdown heading instead of that sequence, and Control+PageDown and Control+PageUp navigate by heading rather than by page.  Pressing Alt+A a second time in a row gives the alternate type of address information, so you can still get a page number without changing the HardPageAddress setting.

The ViewLevels option controls whether EdSharp converts a file when it is opened from outside the editor -- through Windows Explorer, the "Open with" menu, the command line, or the Recent Files command.  (Since 5.0.99 the Open command, Control+O, uses this same option, so a file behaves identically however it is opened.)  By default EdSharp opens files raw, with one exception: binary and document formats whose raw bytes are not readable -- Word (.doc, .docx), Excel (.xls, .xlsx), PowerPoint (.ppt, .pptx), PDF (.pdf), EPUB (.epub, .epub3), WordPerfect (.wpd), WinHelp (.hlp), and rich text (.rtf) -- are converted to text (or, for .rtf, shown as rich text).  From version 5.0.61 a rich text file opened this way also offers "md" on the import list, which converts it to Markdown through the bundled Pandoc; the older route to plain text went through Microsoft Word and therefore needed Word installed.  Every text, markup, data, source-code, or otherwise unrecognized format opens raw, so no text format is ever auto-converted.  Braille files (.brl, .brf) are treated as text and therefore open raw, showing the braille content as it is stored rather than back-translating it.  Use ViewLevels to override any extension: a value of 0 opens that type raw and 1 converts it.  For example, "docx:0" would open Word files raw, "rst:1" would auto-convert reStructuredText, and "brl:1 brf:1" would back-translate braille on open.  The option is a single space-separated list, as in ViewLevels="docx:0 rst:1".

Braille back-translation itself was repaired on 27.08.2026.  Both .brl and .brf now go through liblouis, which is distributed with EdSharpNG: a braille file opened with Control+O is converted to ordinary text (set "brl:1 brf:1" in ViewLevels, since braille counts as text by default).  Previously the .brl converter named in EdSharp.ini did not exist as a file at all, and the .brf converter (NFBTrans) reported success while writing nothing, so either kind of braille file produced an empty document.  Unified English Braille grade 2 is used by default.  For another code, including Polish, set the BrailleTable environment variable to the name of a liblouis table -- Pl-Pl-g1.utb for Polish -- with Environment Variables, Alt+Shift+V.  Polish back-translation was measured and returns the diacritics correctly when the braille file is stored as UTF-8.

The UseIndentModeDefault setting determines the state of indent mode when a new document window is opened.  This mode may be toggled on a per-window basis with Alt+Shift+I.  The configuration setting determines whether it is initially on or off when a file is opened (default is No).

The YieldEncoding setting determines the character encoding EdSharp will use when opeing a file from disk.  EdSharp ignores this setting if the file has a .rtf extension indicating rich text format, or has an initial byte order mark (BOM) indicating Unicode format (e.g., UTF-8 or UTF-16).  An encoding may be indicated by either its name or number.  A list of those available may be found by choosing the Other option in the Yield Encoding command, Alt+Shift+Y, or Export Format command, Alt+Shift+E.  If no YieldEncoding setting is configured, EdSharp examines the content of the file. A byte order mark is definitive. Without one, a file whose bytes form valid UTF-8 is read as UTF-8, and a file whose bytes cannot be UTF-8 is read with the default encoding configured in the regional settings applet of Windows Control Panel. This matters for text saved in a legacy single-byte code page without a byte order mark, as older versions of Notepad and many DOS-era programs produced: reading such a file as UTF-8 replaces every accented letter with a replacement character, and the loss is irreversible once the document is saved. Polish text in windows-1250 was measured losing every diacritic this way before version 5.0.36.  Typically, the setting is Western European.  Use the Status command, Alt+Z, pressed twice in order to check the encoding that EdSharp used to open the current document, together with the kind of line breaks it had on disk.  From version 5.0.37, saving CONVERTS a document read in a legacy single-byte code page to UTF-8, so a file opened here stops being a trap for accented letters; UTF-16 and UTF-32 are left alone, because those were a deliberate choice and converting them would change the size of a file other programs may read.  A configured YieldEncoding setting still wins, and is saved unchanged.  From version 5.0.61 a file whose name ends in .rtf is written as rich text only when the DOCUMENT is rich text, that is when it was opened as rich text or converted into it.  A document that is plain text -- a .rtf file opened with No at the "Treat as rich text?" prompt, or a new document saved under that name -- is written as the plain text it is, and EdSharp says "Saving as plain text, not rich text" so the difference is heard rather than seen.  Deciding this by the file name alone destroyed such a document: the rich text writer escaped every control word in it, so the file reopened showing its own markup instead of the document, and the formatting was gone for good.  Save Copy, Alt+Shift+S, follows the same rule.  Line breaks are written in Windows form on save regardless of what the file had, which is what the Export Format command, Alt+Shift+E, is for when a copy with Unix or Macintosh line breaks is wanted; from version 5.0.39 that copy is written in UTF-8, so it stays readable on the system it is meant for.

A separate dialog invoked with Alt+Shift+Equals is available for setting the font and color of text in an editing window.  The default font was chosen for friendliness to low vision users, and you can adjust this subjective choice.  Use the Manual Options command, Alt+Shift+M, to directly edit the main configuration file, EdSharp.ini, located in the EdSharp data folder.  This is a subfolder of the Windows path to Application Data, typically named something like\
\C:\users\UserName\appdata\roaming\EdSharp\\`

To change key assignments, edit the Keys section of the configuration file, e.g., the line\
`Replace=Control+R`
could be changed to\
`Replace=Control+H`

A command without a hot key may still be invoked via the regular menu system or Alternate Menu (Alt+F10).  The terms used to identify available keys are listed in [Microsoft's Keys enumeration reference](<https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.keys>).

Certain configuration options are associated with the current compiler rather than being global .  Specifically, favorites, bookmarks, and user-defined tokens apply to the current compiler (picked via Control+Shift+F5), so you can work with items more relevant to each coding project.  The Reset Configuration command, Alt+Shift+F10, lets you easily remove custom settings and restore defaults of EdSharp.  This command lets you choose whether to reset the main configuration, current compiler configuration, or create a new compiler configuration.  The New choice prompts for the compiler name, command line, AbbreviateOutput, NavigatePart, QuotePrefix, and ExtensionDefault settings.  

A compiler configuration file is stored in the EdSharp data folder in a file having the compiler name and a .ini extension.  For example, if you created settings for the "Delphi" compiler, EdSharp would create a Delphi entry in the Compilers section of EdSharp.ini, and then store related favorites, bookmarks, and user-defined tokens in Delphi.ini.  Press Control+F9 to query the current compiler and directory.

Press F4 to activate an editing window from a list of those currently open.  Press Control and a digit, Control+1 through Control+9, to go straight to an open window by the order in which it was opened: Control+1 is the first file you opened, Control+2 the second, and so on.  EdSharp says the file name of the window it moves to, or the window title for a document that has never been saved.  If you ask for a window that is not there, it says how many windows are open, and the cursor stays where it is.  This is window navigation, like Control+Tab and Control+Shift+Tab, and has nothing to do with the numbered files under Alt and a digit.  The whole range Control+1 through Control+9 is available for this: the Next Baseline command, which used to hold Control+6, and the Format Code command, which held Control+4, have both been removed from EdSharpNG altogether.  Press Control+F4, or Control+W, to close the current window, or Control+Shift+W to close all windows except the current one.  The F4 family is now consistent: F4 lists the open windows, Shift+F4 lists the folders of your recent and favorite files, and Control+Shift+F4 lists the special folders of Windows.  Shift+F4 used to read out the window titles, which the list under F4 does better, and the folder commands used to sit on Control+0 and Control+Alt+0, which are now free.  EdSharp windows may be visually organized according to common MDI (multiple document interface) patterns.  The Window menu includes the following commands:  Arrange Icons, Alt+F11; Cascade, Control+F11; Tile Horizontal, Alt+Shift+F11; and Tile Vertical, Control+Shift+F11.

Leaving the menu puts the cursor back in the text.  When a menu closes -- with Escape, or after running a command -- Windows hands keyboard focus back to whatever held it before the menu opened, and in a multiple-document window that is often the document frame or the MDI client rather than the edit control itself.  A screen reader then loses the caret and has to be woken up, which used to take pressing Escape twice.  EdSharp now moves focus back to the real edit control whenever a menu closes, the same way it already did when the program regains activation through Alt+Tab.  With the Markdown preview open, focus returns to the preview rather than to the source, since the preview is left by hand and nothing else should throw you out of it.

Use the Alternate Menu command, Alt+F10, to execute a command from a single, alphebetized list.

The Context Menu command, Shift+F10, lets you choose an action to perform on the current file based on those available for its type/extension (in the Windows registry).  Also included is the OpenWith action, by which a default program may be associated with files of this type.  The Send To Menu, Control+F10, lets you choose among SendTo shortcuts (installed by various applications) to perform on the current file.

### Online Help
Press Alt+Shift+H for a summary of EdSharp hot keys.  Press F1 to load this documentation, or Alt+F1 to simply confirm the version.  Control+F1 toggles a Key Describer mode in which pressing a key describes its action.  If you switch to another application window, the mode is automatically turned off.

Use the Environment Variables command, in the menu with no shortcut of its own, to review or change such settings of Windows.  It had Control+E until 5.0.58: changing environment variables of Windows is a programmer's errand rather than part of writing a document, and the letter E is worth more than that.  Choose those of the current process, user, or system as a whole.  Jump quickly to a particular variable based on its initial letter, e.g., Alt+P for the PATH setting that determines where Windows searches for an executable file that is not found in the current directory.  Changes to process settings affect the current session of EdSharp, but not the next time it is run.  User settings take effect when you log in again.  System settings take affect when you restart the computer.

The Burn to CD command was removed in EdSharpNG.  The Path List command, Control+Shift+P, still gathers the paths of a folder into the document, which remains useful on its own.

Use the Elevate Version command, F11, to update EdSharpNG to the latest version published on the [EdSharpNG releases page](<https://github.com/michalkasperczak/EdSharpNG/releases/latest>).  EdSharpNG checks that page and, if a newer release is available, offers to download and run its installer.  The command looks at the EdSharpNG fork, not at the original EdSharp by Jamal Mazrui: installing the original would silently replace EdSharpNG and every feature added to it.  While EdSharpNG is in testing no release is published there, so the command says that there is nothing to update to and names the version you are running; test builds reach you directly instead.
### Hotkey Summary
The following are EdSharp commands listed in related groups.

```
Launch EdSharp=Alt+Control+E, Launch or activate the EdSharp application via a Windows desktop shortcut
Documentation=F1, Open Documentation in web browser
About=Alt+F1, Display version and release date
History of Changes=Shift+F1, Display list of fixes and improvements
Key Describer=Control+F1, Toggle a mode in which pressing a key describes its action
Alternate Menu=Alt+F10, Present all commands in a single, alphabetized list
Context Menu=Shift+F10, Pick a command from those available to Windows Explorer for the current file extension
SendTo Menu=Control+F10, Pick a command from those available as Windows "Send To" options

Select All=Control+A, Select all text
Unselect All=Control+Shift+A, Clear text selection

Select Chunk=Control+Space, Select contiguous sequence of non-blank characters at cursor, or select the next chunk if a selection already exists
Say Selected=Shift+Space or JAWSKey+Shift+DownArrow, Say selected text, or spell if repeated
Say Chunk=Shift+BackSpace, Say chunk at cursor

Start Selection=F8, Mark starting point of text to be selected
Complete Selection=Shift+F8, Select text from starting point to cursor
Reselect=Control+Shift+F8, Reselect between previous start and end positions
Go to Start of Selection=Alt+Shift+F8, Return to start position of selection
Copy All=Control+F8, Copy all text to clipboard
Read All=Alt+F8, Say all text (without moving cursor)

Say Address=Alt+A, Say line, column, and percent position of cursor
Say Indent=Alt+I, Say the indentation level of the current line, or the preceding line with less indentation if repeated
Say Yield=Alt+Y, Say number of characters, words, and lines in all or selected text
Say Status=Alt+Z, Say whether current file has been modified since last save to disk, or say its character encoding and line break kind if repeated
Say Clipboard=Alt+Apostrophe, Say clipboard text, or spell if repeated
Say Time=Alt+Semi-colon, Say current time and date
Insert Time=Alt+Shift+Semicolon, Insert current time and date

Configuration Options=Control+Comma, Adjust configuration options through a dialog
Manual Options=Alt+Shift+M, Adjust options by directly editing the main configuration file
Reset Configuration=Alt+Shift+F10, Revert to default options, or define a new compiler configuration

Copy=Control+C, Copy selected text to clipboard, or copy current line if no selection
Copy Append=Alt+C, Append selected text to clipboard, or append current line if no selection
Copy Rich Text=Control+Shift+C, Copy with formatting to clipboard: a selection of several lines keeps its headings, lists, bold and italic when pasted into Word; on a single line it copies the link, list or formatted line at the cursor
Cut=Control+X, Cut selected text to clipboard, or cut current line if no selection
Cut Append=Alt+X, Cut and append selected text to clipboard, or cut and append current line if no selection

Paste=Control+V, Paste text from clipboard; text copied with formatting inside EdSharpNG comes back as Markdown, so a copied link keeps its address
Paste File=Control+Shift+V, Insert another file at cursor position
Append from Clipboard=Alt+Shift+C, Toggle a mode in which text copied to the clipboard is also saved to a file
Undo=Control+Z, Undo the last editing action
Redo=Control+Shift+Z, Redo the last action that was undone; Control+Y does the same

Save Snippet=Alt+S, Save all or selected text to a snippet file
Invoke Snippet=Alt+V, Pick snippet file to paste or execute
View Snippet=Alt+Shift+V, Pick snippet file to view or edit

Regular Expression Tool=Control+Shift+Y, Count matches of a regular expression, or extract them to a new window; the dialog asks which
Replace with Regular Expression=Control+Shift+R, Search and replace regular expression in all or selected text
Replace=Control+R, Search and replace string in all or selected text

File Find=Alt+Shift+F, Open file from list of files containing a search string
Forward Find=Control+F, Search forward for string in all or selected text
Reverse Find=Control+Shift+F, Search backward for string
Forward Find with Regular Expression=Control+F3, Search forward for regular expression in all or selected text
Reverse Find with Regular Expression=Control+Shift+F3, Search forward for regular expression in all or selected text
Forward Find at Cursor=Alt+F3, Search forward for chunk or selected text
Reverse Find at Cursor=Alt+Shift+F3, Search backward for chunk or selected text
Forward Find Again=F3, Search forward for next match
Reverse Find Again=Shift+F3, Search backward for previous match

Word Wrap=, Word wrap lines (menu only, no shortcut)
Unwrap=, Unwrap lines (menu only, no shortcut)
Guard Document=Control+F7, Turn read-only protection on or off

Extra Speech Log=Alt+Shift+X Open speech.log file in a new window

Go to Percent=Control+G, Go to percentage point in document
Go to Percent Again=Alt+G, Repeat Go command
Jump to Line=Control+J, Jump to line number or to line, column position
Jump to Line Again=Alt+J, Repeat Jump command

Set Bookmark=Control+B, Set bookmark at cursor position
Clear Bookmark=, Clear bookmark at cursor position; Delete on the bookmark list does the same (menu only, no shortcut)
Go to Bookmark=Alt+B, Go to bookmark in current file
Next Bookmark=Shift+PageDown, Go to next bookmark and read its line
Prior Bookmark=Shift+PageUp, Go to prior bookmark and read its line
Set Named Bookmark=Control+Shift+B, Set a bookmark with a name of your own at the cursor line, or rename the one that is already there; an empty name removes it
Named Bookmark List=Alt+Shift+B, Show the list of named bookmarks and go to the one you choose; Delete removes one

Toggle Favorite=Alt+Shift+L, Add the current file to the list of favorites, or take it off that list if it is already there; says only what happened, "Added to favorites" or "Removed from favorites", not the name of the command
Clear Favorite=, Clear current file from the list of favorites and say so, or say "Not in favorites" if it was never on the list (menu only, no shortcut)
List Favorites=Alt+L, Open a file from the list of favorites; on the list Control+C copies the files themselves, so they paste into File Explorer as well as into text, Alt+C appends the paths as text, Delete takes every selected entry off the list
Recent Files=Alt+R, Open a file from the list of those recently used; on the list Control+C copies the files themselves, so they paste into File Explorer as well as into text, Alt+C appends the paths as text, Delete takes every selected entry off the list
Numbered Files=Alt+Shift+F2, Open a file from the list of numbered files
Open Numbered File=Alt+digit, Open the file assigned to that digit, Alt+1 to Alt+9 and Alt+0 for the tenth
Assign Numbered File=Alt+Shift+digit, Assign the current file to that digit

New=Control+N, Open a new editing window
New from Clipboard=Control+Shift+N, Open a new editing window containing clipboard text

Open=Control+O, Open file
Open Again=Alt+O, Reload the current file from disk

Properties=Alt+Enter, display Windows properties dialog for current file
Save=Control+S, Save
Save As=Control+Shift+S, Save As
Save Copy=Alt+Shift+S, Save copy of document using a different name
Export Format=Alt+Shift+E, Export document to another format

Print=Control+P, Print current file
Mail Body=Control+M, Mail text of current window as body of an email message
Mail Attachment=Control+Shift+M, Mail current file as an email attachment, offering to save unsaved changes first

Run=, Execute current file, based on its extension (menu only, F5 now opens the Markdown preview in a web browser)
Preview Markdown in Web Browser=F5, Show the current Markdown document as a page in the default web browser
Run at Cursor=Shift+F5, Execute a web URL or email address at cursor position or in selected text
Prompt Command=Alt+F5, Prompt for a command line to execute and say its standard output
Review Output=Alt+Shift+F5, Open standard output of last prompt or compile command in a new editing window
Compile=Control+F5, Compile source code, say output, and jump to error position
Pick Compiler=Control+Shift+F5, Pick a compiler or interpreter from the list of those configured
Say Compiler=Control+F9, Say current compiler and folder
Go to Folder=Shift+F4, Go to folder containing recent or favorite files
Go to Special Folder=Control+Shift+F4, Go to special folder of Windows
Go to Environment=, Go to interactive environment of current compiler (menu only, no shortcut)

Spell Check=F7, Spell check all or selected text
Thesaurus=Shift+F7, Look up synonyms for word at cursor

Say Path=Alt+P, Say full path of current file
Path to Clipboard=Alt+Shift+P, Copy full path of current file to clipboard
Path List=Control+Shift+P, Generate a list of files in a new editing window

Special Character=F2, Insert character indirectly by specifying its Unicode value
Quote=Control+Q, Add prefix sequence to current or selected lines
Unquote=Control+Shift+Q, Remove prefix sequence from current or selected lines
Join Lines=Control+Shift+J, Word wrap lines in all or selected paragraphs
Hard Line Break=, Set the maximum width of lines in all or selected text (menu only, no shortcut)

Upper Case=Control+U, Convert current or selected characters to upper case
Lower Case=Control+Shift+U, Convert current or selected characters to lower case
Proper Case=Alt+U, Convert current or selected characters to proper case
Swap Case=Alt+Shift+U, Convert lower case characters to upper case, and vice versa
Yield Encoding=Alt+Shift+Y, Render all or selected text based on a character encoding

Repeat Line=, Copy current line below it (menu only, no shortcut; Control+Y now redoes)
Evaluate Expression=Control+Equals, Evaluate current line or selected text as a JScript.NET expression and copy the result below
Replace Tokens=Control+Shift+Equals, Swap user-defined tokens with their computed results in all or selected text
Transform Files=Alt+Equals, Apply a set of search and replace tasks to a list of files in the current window
Trim Blanks=Control+Shift+Enter, Trim leading and trailing blanks from the current or selected lines, and remove more than two consecutive blank lines

End Character=Alt+End, Go to last non-blank character of line and read it
Home Character=Alt+Home, Go to first non-blank character of line and read it
Next Word=Control+RightArrow, Go to next word (word boundaries follow Unicode letters, so accented and Polish letters do not split a word); your screen reader reads the word it lands on
Prior Word=Control+LeftArrow, Go to previous word (word boundaries follow Unicode letters, so accented and Polish letters do not split a word); your screen reader reads the word it lands on
Next Chunk=Alt+RightArrow, Go to next chunk and read it
Prior Chunk=Alt+LeftArrow, Go to previous chunk and read it
Next Sentence=Alt+DownArrow, Go to next sentence; your screen reader reads it
Prior Sentence=Alt+UpArrow, Go to previous sentence; your screen reader reads it
Next Paragraph=Control+DownArrow, Go to next paragraph; your screen reader reads the paragraph it recognises, EdSharp adds any further lines of the EdSharp paragraph, and the whole paragraph is also placed on the status bar
Prior Paragraph=Control+UpArrow, Go to previous paragraph; your screen reader reads the paragraph it recognises, EdSharp adds any further lines of the EdSharp paragraph, and the whole paragraph is also placed on the status bar

Delete Right=Control+Shift+Delete, Delete from cursor to end of line
Delete Left=Control+Shift+Backspace, Delete from cursor to start of line
Delete Down=Alt+Shift+Delete, Delete from cursor to bottom of file
Delete Up=Alt+Shift+Backspace, Delete from cursor to top of file
Delete Line=Alt+Backspace, Delete current line
Delete Hard Line=Control+D, Delete line ending in hard line break
Delete Paragraph=Control+Shift+D, Delete past one or more blank lines
Delete File=Alt+Shift+D Delete current file on disk
Rename=Alt+Shift+R Rename current file on disk

Next Section=Control+PageDown, Go to the next Markdown heading of any level
Prior Section=Control+PageUp, Go to the previous Markdown heading of any level
Next Section at Same Level=Control+Shift+PageDown, Go to the next Markdown heading of the same level, skipping subsections
Prior Section at Same Level=Control+Shift+PageUp, Go to the previous Markdown heading of the same level, skipping subsections
Document Navigation=F6, Open a tree of the document headings and go to the one you choose
Go to Contents=Shift+F6, Jump between the table of contents and the chapter, whichever way the cursor is
Next Emphasis=Control+Slash, Go to the next bold or italic text and say it
Prior Emphasis=Control+Shift+Slash, Go to the previous bold or italic text and say it
Next List=Control+Dash, Go to the start of the next list and say its first item
Prior List=Control+Shift+Dash, Go to the start of the previous list and say its first item
Link List=Control+F6, Show the list of links in the document and go to the one you choose; Left Arrow reads the address and the line number, Control+C copies the link as Markdown, Control+Shift+C as a formatted link, F2 edits its text and address
Next Link=Alt+PageDown, Go to the next link in the document and say its text, address and kind
Prior Link=Alt+PageUp, Go to the previous link in the document and say its text, address and kind

Topic=Alt+T, Say topic of current section
Section Break=Control+Enter, Start a new section by inserting a Markdown heading at the level of the heading above

Text Combine=, Convert other formats to text and combine them in a new editing window (menu only, no shortcut)
Insert Link=Control+K, Insert a link: type the text and the address, or pick an image or a place in this document
Insert Table=Control+Shift+T, Open the table wizard: on a table it opens that table for editing, elsewhere it starts a new one, and Control+Enter puts it into the document
Bulleted List=Control+L, Turn the current line or the selected lines into a bulleted list; on a list that is already bulleted it turns the lines back into plain text
Numbered List=Control+Shift+L, Turn the current line or the selected lines into a numbered list; on a list that is already numbered it turns the lines back into plain text
Table of Contents=Alt+Shift+T, Write or refresh a Markdown table of contents with links at the top of the document
Insert Footnote=Control+Shift+K, Insert a Markdown footnote: type its text and choose whether it lands at the end of the document or the end of the current chapter
Go to Footnote=Control+Alt+K, Jump between the footnote marker in the sentence and its text at the end of the document
Next Footnote=Control+Alt+PageDown, Go to the next footnote marker in the text
Prior Footnote=Control+Alt+PageUp, Go to the previous footnote marker in the text
Footnote List=Alt+K, Show the list of footnotes and go to the one you choose
Export Footnotes=, Put all footnotes of the document in a new window, either as a numbered list or each one preceded by the sentence that carries it (menu only, no shortcut)
Insert Comment=, Insert an internal comment at the cursor, or edit the comment the cursor is in; internal comments stay in the Markdown file and are dropped when converting to Word, HTML or plain text
Next Comment=, Go to the next internal comment and read it
Prior Comment=, Go to the prior internal comment and read it
Comment List=, Show the list of internal comments and go to the one you choose

Say Styles=Alt+Slash, Say current justification and styles

Infer Indent=Alt+RightBracket, Infer the indent unit of the current document, or configure EdSharp accordingly if repeated
Toggle Indentation=Windows+Grave, Toggle announcement of indentation by JAWS
Indent Mode=Alt+Shift+I, Toggle auto indent with Enter, and announcement of indentation changes
Enter New Line=Enter, Start new line at left margin
Indent New Line=Shift+Enter, Start new line with same indentation as current one
Indent New Line Prior=Alt+Shift+Enter, insert prior line with same indentation as current one
Indent=Tab, Indent current line or selected text by one unit
Outdent=Shift+Tab, Reduce indentation of current or selected lines by one unit
Align=Alt+Shift+A, Adjust indentation of current or selected lines according to prior line
Next Indent=Control+I, Go to the next change in indentation
Prior Indent=Control+Shift+I, Go to the previous change in indentation
Right Brace=Control+Shift+RightBracket, Search forward for matching right brace character
Left Brace=Control+Shift+LeftBracket, Search backward for matching left brace character
End Tag=Control+Shift+Period, go to closing tag of HTML element
Start Tag=Control+Shift+Comma, Go to opening tag of HTML element

Order Items=Alt+Shift+O, Sort items alphabetically in all or selected text
Reverse Items=Alt+Shift+Z, Reverse order of all or selected items of text
Keep Unique Items=Alt+Shift+K, Discard repetitive items in all or selected text
Number Items=Alt+Shift+N, Insert numbers at the start of items in all or selected text
List Different Items=Alt+Shift+G, Compare two lists and put non-overlapping items in a new window
Query Common Items=Alt+Shift+Q, Compare two lists and put overlapping items in a new window

PyDent=Alt+LeftBracket, Convert from PyBrace format, or reformat typical Python code, using the IndentUnit setting and adding comments at ends of blocks
PyBrace=Alt+Shift+LeftBracket, Convert from PyDent format, or reformat typical Python code, using braces instead of indentation and adding comments at ends of blocks

Explorer Folder=Alt+Backslash, Open Windows Explorer in the EdSharp program folder, data folder, or current folder
Command Prompt=Control+Backslash, Open a command prompt in the EdSharp program folder, data folder, or current folder
Environment Variables=, Change Windows environment variables for the current process, user, or system (menu only, no shortcut)

Next Window=Control+Tab, Cycle to next editing window
Prior Window=Control+Shift+Tab, Cycle to previous editing window
Go to Window=Control+digit, Go to an open editing window by the order it was opened and say its file name, Control+1 to Control+9
Preview=Escape, Toggle the Markdown preview of the current .md file, and return to editing from it
Detached Preview=Shift+Escape, Open the Markdown preview without following the editing cursor, or switch synchronization while previewing
Current Windows=F4, Activate an editing window from a list of those currently open
Close Window=Control+F4, Close current editing window, also on Control+W
Close All but Current Window=Control+Shift+W, Close all editing windows except the current one
Exit EdSharp=Alt+F4, Exit the EdSharp application

Arrange Icons=Alt+F11, Arrange open windows
Cascade=Control+F11, Cascade open windows
Tile Horizontal=Alt+Shift+F11, Tile open windows horizontally
Tile Vertical=Control+Shift+F11, Tile open windows vertically

Elevate Version=F11, Download latest EdSharpNG version and run installer (after confirming)

With JAWS scripts:
Toggle Punctuation=JAWSKey+Grave Accent, Toggle JAWS voice between all and no punctuation
Voice Louder=Alt+Grave, Increase JAWS voice volume by 5%
Voice Softer=Alt+Shift+Grave, Decrease JAWS voice volume by 5%
Voice Faster=Control+Grave, Increase JAWS voice rate by 5%
Voice Slower=Control+Shift+Grave, Decrease JAWS voice rate by 5%
Insert Script Path=Control+I, Insert JAWS script path in Open or Save Dialog
Insert All Users Path=Control+Shift+I, Insert JAWS All Users path in Open or Save Dialog
```

### Development Notes
For the technically curious, I developed EdSharp with the [C# programming language](<https://learn.microsoft.com/en-us/dotnet/csharp/>).

EdSharp 5.0 targets the .NET Framework 4.8 and is compiled AnyCPU, so the same build runs as native 64-bit on x64 and ARM64 Windows.  To rebuild it from source, open a command prompt in the program folder and run `BuildEdSharp.cmd`.  That script locates the C# compiler (preferring the newest Roslyn `csc.exe`, otherwise the framework `csc.exe` that ships with .NET), compiles `EdSharp.exe` and `EdSharp.dll`, and -- on a best-effort basis -- fetches the few files it needs:  the `Ude.dll` encoding-detection library and current portable builds of the Convert tools (Pandoc, HTML Tidy, liblouis, Xpdf, and Artistic Style).  A clean clone therefore needs nothing but `BuildEdSharp.cmd`; everything generated or downloaded is reproduced on demand.

To build the Windows installer, `EdSharp_Setup.exe`, install [Inno Setup](<https://jrsoftware.org/isinfo.php>) and compile `EdSharp_Setup.iss` with it (after a successful `BuildEdSharp.cmd`, so the compiled files exist).

Because the built and fetched files are reproducible, they are kept out of source control by `.gitignore`:  `EdSharp.exe`, `EdSharp.dll`, `EdSharp_Setup.exe`, `BuildEdSharp.log`, the fetched `Ude.dll`, and the downloaded tool folders under `Convert`.  Committed binaries that are not produced by the build, such as `Tektosyne.dll`, `nvdaControllerClient.dll`, and `EdSharp.ico`, remain in the repository.  A `.gitattributes` file keeps text files in Windows (CRLF) form and marks those binaries.  This lets the whole project be pushed to GitHub with `git add -A`.

### Contributors
Thanks go to Jim Homme for contributing an improved set of HTML snippets.  He also contributed the Sounds4Stuff scheme for JAWS, which may be installed via Settings Packager.

I thank Jaffer for contributing C++ and PHP snippets.

### Third Party Utilities
PDF conversions use the open source [Xpdf](<http://www.foolabs.com/xpdf/home.html>) software.

The GetText utility is from [Kryltech](<http://www.kryltech.com>)
with a license in the file GetText.txt (in the EdSharp\Convert\GetText folder).

I welcome feedback, which helps EdSharp improve over time.  When reporting a problem, the more specifics the better, including steps to reproduce it, if possible.

The latest version of EdSharpNG is available on the [EdSharpNG releases page](<https://github.com/michalkasperczak/EdSharpNG/releases/latest>).
This may be downloaded and installed with the Elevate Version command, F11.  The original EdSharp by Jamal Mazrui lives at its own [releases page](<https://github.com/JamalMazrui/EdSharp/releases/latest>) and is a separate program: installing it would replace EdSharpNG.

Jamal Mazrui
jamal at empowermentzone dot com
