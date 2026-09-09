@echo off
rem Back-translate a braille file to ordinary text, for EdSharpNG's Import table.
rem
rem Parameters:
rem   %1  the liblouis folder (EdSharpNG passes %ProgDir%\Convert\liblouis)
rem   %2  the source braille file, quoted
rem   %3  the target text file
rem
rem Why this file exists: EdSharp.ini has always named
rem Convert\liblouis\brl2txt.bat as the brl2txt converter, but the file itself
rem was never present, so opening a .brl file produced an empty conversion and
rem an error box. Measured 27 August 2026.
rem
rem The table is chosen by the BrailleTable environment variable when it is set,
rem so a user reading Polish braille can put Pl-Pl-g1.utb there; otherwise
rem Unified English Braille grade 2 is used, which is what the original
rem EdSharp assumed.

setlocal

set "louDir=%~1"

if "%BrailleTable%"=="" set "BrailleTable=en-ueb-g2.ctb"

rem liblouis finds its tables through this variable; without it lou_translate
rem reports that the table cannot be found even though it is right there.
set "LOUIS_TABLEPATH=%louDir%\share\liblouis\tables"

if not exist "%louDir%\bin\lou_translate.exe" (
  echo The braille translator was not found at "%louDir%\bin\lou_translate.exe".
  exit /b 1
)

rem The source and target are used straight from the parameters with %~2 and
rem %~3, not through variables: EdSharpNG passes the source already quoted, and
rem storing it in a variable first leaves the quotes inside the value, so the
rem redirection then looks for a file whose name begins with a quotation mark
rem and cmd answers that the syntax is incorrect. Measured 27 August 2026.
"%louDir%\bin\lou_translate.exe" -b "%BrailleTable%" < "%~2" > "%~3"

endlocal
