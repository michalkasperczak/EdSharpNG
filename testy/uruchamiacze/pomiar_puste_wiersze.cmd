@echo off
rem Kompiluje i uruchamia pomiar komunikatu o pustym wierszu (zgloszenie 15.09.2026).
rem
rem SCIEZKI DO UIA TRZEBA PODAWAC WPROST.  Same nazwy
rem /reference:"UIAutomationClient.dll" NIE dzialaja - csc.exe nie szuka w GAC
rem i konczy sie CS0006 "nie mozna odnalezc pliku metadanych".  Zmierzone
rem 16.09.2026; ten sam wzorzec co w pomiar_sesja.cmd.
setlocal enabledelayedexpansion
pushd "%~dp0"
set "csc="
for %%p in (
 "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
 "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) do (if not defined csc if exist %%p set "csc=%%~p")
if not defined csc (echo BLAD: nie znalazlem csc.exe & popd & exit /b 2)

set "refDir=C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
set "uiaClient=!refDir!\UIAutomationClient.dll"
set "uiaTypes=!refDir!\UIAutomationTypes.dll"
if not exist "!uiaClient!" set "uiaClient=%SystemRoot%\Microsoft.NET\assembly\GAC_MSIL\UIAutomationClient\v4.0_4.0.0.0__31bf3856ad364e35\UIAutomationClient.dll"
if not exist "!uiaTypes!" set "uiaTypes=%SystemRoot%\Microsoft.NET\assembly\GAC_MSIL\UIAutomationTypes\v4.0_4.0.0.0__31bf3856ad364e35\UIAutomationTypes.dll"
if not exist "!uiaClient!" (echo BLAD: nie znalazlem UIAutomationClient.dll & popd & exit /b 4)

"!csc!" /nologo /target:exe /reference:"System.Windows.Forms.dll" /reference:"!uiaClient!" /reference:"!uiaTypes!" /out:pomiar_puste_wiersze.exe pomiar_pustego_wiersza_608.cs
if errorlevel 1 (echo BLAD KOMPILACJI & popd & exit /b 3)
pomiar_puste_wiersze.exe %*
set "kod=%errorlevel%"
popd
exit /b %kod%
