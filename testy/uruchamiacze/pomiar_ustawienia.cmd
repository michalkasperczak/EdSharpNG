@echo off
rem Kompiluje i uruchamia pomiar okna ustawien (zadanie 7).
rem Okno powstaje w procesie sondy, wiec potrzebne sa referencje WinForms.
setlocal enabledelayedexpansion
set "csc="
for %%p in (
 "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
 "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) do (if not defined csc if exist %%p set "csc=%%~p")
if not defined csc (echo BLAD: nie znalazlem csc.exe & exit /b 2)
"!csc!" /nologo /target:exe /reference:"System.Windows.Forms.dll" /reference:"System.Drawing.dll" /out:pomiar_ustawienia.exe pomiar_ustawienia_596.cs
if errorlevel 1 (echo BLAD KOMPILACJI & exit /b 3)
pomiar_ustawienia.exe %*
exit /b %errorlevel%
