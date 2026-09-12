@echo off
rem Kompiluje i uruchamia pomiar logiki sprawdzania pisowni.
setlocal enabledelayedexpansion
set "csc="
for %%p in (
 "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
 "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) do (if not defined csc if exist %%p set "csc=%%~p")
if not defined csc (echo BLAD: nie znalazlem csc.exe & exit /b 2)
"!csc!" /nologo /target:exe /reference:"System.Windows.Forms.dll" /reference:"System.Drawing.dll" /out:pomiar_pisownia.exe pomiar_pisownia_logika.cs
if errorlevel 1 (echo BLAD KOMPILACJI & exit /b 3)
pomiar_pisownia.exe EdSharpNG.exe
exit /b %errorlevel%
