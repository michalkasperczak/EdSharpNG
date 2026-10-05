@echo off
rem Kompiluje narzedzie zywego pomiaru schowka (5.0.115).
setlocal
cd /d "%~dp0"
set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
"%CSC%" /nologo /target:exe /out:SchowekZywy.exe schowek_zywy_115.cs
exit /b %errorlevel%
