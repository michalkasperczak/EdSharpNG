@echo off
rem Kompiluje i uruchamia pomiar CSV na GOTOWEJ binarce EdSharpNG.exe.
setlocal enabledelayedexpansion
set "csc="
for %%p in (
 "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
 "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) do (if not defined csc if exist %%p set "csc=%%~p")
if not defined csc (echo BLAD: nie znalazlem csc.exe & exit /b 2)
"!csc!" /nologo /target:exe /out:pomiar_csv_w_programie.exe pomiar_csv_w_programie.cs
if errorlevel 1 (echo BLAD KOMPILACJI & exit /b 3)
pomiar_csv_w_programie.exe EdSharpNG.exe
exit /b %errorlevel%
