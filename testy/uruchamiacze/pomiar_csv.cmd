@echo off
rem Kompiluje i uruchamia pomiar CSV (testy\pomiar_csv.cs + Csv.cs).
rem Osobno od calego programu, bo Csv.cs nie zalezy od Windows Forms.
setlocal enabledelayedexpansion
set "csc="
for %%p in (
 "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
 "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) do (if not defined csc if exist %%p set "csc=%%~p")
if not defined csc (echo BLAD: nie znalazlem csc.exe & exit /b 2)
"!csc!" /nologo /target:exe /out:pomiar_csv.exe pomiar_csv.cs Csv.cs
if errorlevel 1 (echo BLAD KOMPILACJI & exit /b 3)
pomiar_csv.exe
exit /b %errorlevel%
