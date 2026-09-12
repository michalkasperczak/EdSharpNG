@echo off
rem Pomiar ciaglosci pracy (Sesja.cs). Uruchamiac na Windows.
rem Kompiluje CALY program razem z pomiarem, bo Sesja.cs uzywa Util.Equiv z
rem EdSharp.cs - a to ciagnie za soba odwolania UIA, tak jak zwykly build.
setlocal enabledelayedexpansion
pushd "%~dp0"
set "csc=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe" set "csc=C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe"
set "refDir=C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
set "uiaProv=!refDir!\UIAutomationProvider.dll"
set "uiaTypes=!refDir!\UIAutomationTypes.dll"
if not exist "!uiaProv!" set "uiaProv=%SystemRoot%\Microsoft.NET\assembly\GAC_MSIL\UIAutomationProvider\v4.0_4.0.0.0__31bf3856ad364e35\UIAutomationProvider.dll"
if not exist "!uiaTypes!" set "uiaTypes=%SystemRoot%\Microsoft.NET\assembly\GAC_MSIL\UIAutomationTypes\v4.0_4.0.0.0__31bf3856ad364e35\UIAutomationTypes.dll"
set "udeRef="
if exist Ude.dll set "udeRef=/reference:Ude.dll /define:HAVEUDE"
if exist pomiar_sesja.exe del /f /q pomiar_sesja.exe
rem /main rozstrzyga dwa Main naraz: nasz pomiar i EdSharp.
"%csc%" /nologo /target:exe /platform:anycpu /main:PomiarSesja !udeRef! /out:pomiar_sesja.exe pomiar_sesja.cs Sesja.cs EdSharp.cs Lbc.cs Say.cs Inix.cs KeyMap.cs Web.cs Pisownia.cs Skladniki.cs Csv.cs /reference:"Tektosyne.dll" /reference:"Microsoft.VisualBasic.dll" /reference:"Microsoft.CSharp.dll" /reference:"System.IO.Compression.dll" /reference:"System.IO.Compression.FileSystem.dll" /reference:"!uiaProv!" /reference:"!uiaTypes!" > pomiar_sesja_build.log 2>&1
if errorlevel 1 (echo BUILD FAILED & findstr /C:"error CS" pomiar_sesja_build.log & popd & exit /b 1)
pomiar_sesja.exe
popd
