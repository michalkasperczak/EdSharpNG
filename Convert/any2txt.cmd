@echo off
rem any2txt.cmd -- convert a supported document to plain text for EdSharp's Import,
rem using 2htm.exe in plain-text mode (-p).  Replaces the old GetText.exe and the
rem Office COM converters (WdVert/XlVert/PpVert) for text extraction.
rem   %1 = EdSharp program directory   %2 = source document   %3 = target text file
rem
rem 2htm writes <outputdir>\<sourcebasename>.txt, so we point it at the target's
rem directory and then rename its output to the exact target EdSharp expects.
rem
rem TRAILING BACKSLASH BUG, fixed 13.09.2026 (measured on two machines).
rem %~dp3 always ends in a backslash, so "%outdir%" expanded to
rem "C:\Users\x\Temp\" -- the backslash escaped the closing quote and 2htm read
rem the output directory as 'C:\Users\x\Temp"', answering
rem "Output directory does not exist and cannot be created: illegal characters
rem in path" and writing nothing.  Every Import entry routed through this script
rem (doc2txt, ppt2txt, pptx2txt, xls2txt, xlsx2txt, hlp2txt, html2txt, wpd2txt)
rem therefore produced NO text at all.  Stripping the trailing backslash is the
rem whole fix; keep it, and keep the paths quoted for names with spaces.
setlocal
set "prog=%~1"
set "src=%~2"
set "dst=%~3"
if exist "%dst%" del /f /q "%dst%"
set "outdir=%~dp3"
if "%outdir:~-1%"=="\" set "outdir=%outdir:~0,-1%"
"%prog%\Convert\2htm\2htm.exe" "%src%" -p -f -o "%outdir%"
set "made=%outdir%\%~n2.txt"
if exist "%made%" if /i not "%made%"=="%dst%" move /y "%made%" "%dst%" >nul
endlocal
