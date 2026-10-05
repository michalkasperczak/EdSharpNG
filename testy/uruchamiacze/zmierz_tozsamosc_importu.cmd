@echo off
rem Kompiluje i uruchamia pomiar tozsamosci zaimportowanego dokumentu.
rem Wolane przez testy/zmierz_tozsamosc_importu.sh z WSL.
rem
rem Sonda laduje EdSharpNG.exe przez refleksje, wiec NIE kompiluje sie z
rem /reference do niego: stara binarka nie ma wlasnosci IdentityPath i
rem kompilacja z referencja nie przeszlaby dla pomiaru odniesienia.  Wlasnie o
rem to chodzi - ta sama sonda musi dac sie uruchomic na obu wersjach.
setlocal
cd /d "%~dp0"
set "CSC="
for %%p in (
  "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe"
  "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe"
  "%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
  "%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) do (if not defined CSC if exist %%p set "CSC=%%~p")
if not defined CSC echo BLAD: nie znalazlem csc.exe & exit /b 1

"%CSC%" /nologo /warn:0 /out:ImportIdentityProbe.exe harness_tozsamosc_importu.cs
if errorlevel 1 echo BLAD: kompilacja sondy nie udala sie & exit /b 2

ImportIdentityProbe.exe "%CD%\EdSharpNG.exe" "%CD%\EdSharp.ini"
exit /b %errorlevel%
