@echo off
rem ====================================================================
rem BuildEdSharp.cmd - x64 build for EdSharp.exe (v5 modernization).
rem
rem   EdSharpNG.exe -- the WinForms app, from EdSharp.cs by csc.exe,
rem   x64, with the manifest and (if present) icon.
rem
rem EdSharp.dll (JScript .NET host built from EdSharp.js by jsc.exe) is NO
rem LONGER BUILT: the whole scripting layer was removed on 16.09.2026
rem (docs/CO-USUWAMY.md 2.2).  With it went the jsc.exe step, the reflection
rem load (Assembly.LoadFrom) and one file from the installer.  Expression
rem evaluation and backslash-escape expansion now live in Wyrazenia.cs.
rem
rem Bare compiler, no MSBuild/NuGet: csc.exe ships with the .NET Framework
rem and is also available, newer, from VS Build Tools.
rem ====================================================================
setlocal enableextensions enabledelayedexpansion
pushd "%~dp0"

set "log=BuildEdSharp.log"
echo EdSharp build log > "!log!"
echo Started %DATE% %TIME% >> "!log!"

if not exist "EdSharp.cs" echo ERROR: EdSharp.cs not found.& popd & exit /b 1
if not exist "Lbc.cs" echo ERROR: Lbc.cs not found.& popd & exit /b 1
if not exist "Say.cs" echo ERROR: Say.cs not found.& popd & exit /b 1
if not exist "Inix.cs" echo ERROR: Inix.cs not found.& popd & exit /b 1
if not exist "KeyMap.cs" echo ERROR: KeyMap.cs not found.& popd & exit /b 1
if not exist "Wyrazenia.cs" echo ERROR: Wyrazenia.cs not found.& popd & exit /b 1
if not exist "EdSharp.manifest" echo ERROR: EdSharp.manifest not found.& popd & exit /b 1

rem ---- locate csc.exe: prefer Roslyn (latest C#), fall back to Framework ----
set "csc="
for %%p in (
  "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe"
  "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe"
  "C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
  "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
  "C:\Program Files (x86)\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe"
  "C:\Program Files (x86)\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe"
  "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe"
  "%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
  "%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) do (if not defined csc if exist %%p set "csc=%%~p")
if not defined csc echo ERROR: No csc.exe found. Install VS Build Tools or repair .NET Framework.& popd & exit /b 1
echo C# compiler: !csc! >> "!log!"

rem ---- locate UIA reference assemblies (for the UIA-notification work) ----
rem Not referenced by this minimal baseline yet, but probed here so the
rem command line is ready when the speech subsystem moves to UIA. Probe
rem the .NET 4.8 Developer Pack first, then the GAC runtime images.
set "uiaProv="
set "uiaTypes="
set "refDir=C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
if exist "!refDir!\UIAutomationProvider.dll" set "uiaProv=!refDir!\UIAutomationProvider.dll"
if exist "!refDir!\UIAutomationTypes.dll"    set "uiaTypes=!refDir!\UIAutomationTypes.dll"
if not defined uiaProv if exist "%SystemRoot%\Microsoft.NET\assembly\GAC_MSIL\UIAutomationProvider\v4.0_4.0.0.0__31bf3856ad364e35\UIAutomationProvider.dll" set "uiaProv=%SystemRoot%\Microsoft.NET\assembly\GAC_MSIL\UIAutomationProvider\v4.0_4.0.0.0__31bf3856ad364e35\UIAutomationProvider.dll"
if not defined uiaTypes if exist "%SystemRoot%\Microsoft.NET\assembly\GAC_MSIL\UIAutomationTypes\v4.0_4.0.0.0__31bf3856ad364e35\UIAutomationTypes.dll" set "uiaTypes=%SystemRoot%\Microsoft.NET\assembly\GAC_MSIL\UIAutomationTypes\v4.0_4.0.0.0__31bf3856ad364e35\UIAutomationTypes.dll"
if not defined uiaProv echo ERROR: UIAutomationProvider.dll not found; install the .NET 4.8 Developer Pack.& popd & exit /b 1
if not defined uiaTypes echo ERROR: UIAutomationTypes.dll not found; install the .NET 4.8 Developer Pack.& popd & exit /b 1
echo UIAutomationProvider: !uiaProv! >> "!log!"
echo UIAutomationTypes: !uiaTypes! >> "!log!"

rem ---- x64 NVDA controller client (runtime-only, optional) ----
rem nvdaControllerClient.dll drives NVDA's extra-speech announcements. It is
rem NOT needed to build or to run the editor (normal screen-reader access
rem works without it); only the optional spoken status messages use it. The
rem 32-bit nvdaControllerClient32.dll from older EdSharp will not load in an
rem x64 process. To enable it, drop the x64 nvdaControllerClient.dll beside
rem EdSharp.exe; get it from the NVDA controllerClient package at
rem https://www.nvaccess.org/ (Developer downloads).
if exist "nvdaControllerClient.dll" (echo NVDA controller client present.) else (echo NVDA controller client absent ^(optional, runtime only^).)

rem ---- EdSharp.dll (JScript .NET) NIE JEST JUZ BUDOWANA ----
rem Stara kopia zostawiona w katalogu budowania mylila: instalator dostawal
rem plik, ktorego program juz nie wola.  Kasujemy ja, jesli lezy.
if exist EdSharp.dll del /f /q EdSharp.dll

rem ---- best-effort: fetch the encoding-detection library (Ude.dll) ----
rem Referenced by csc at build time and loaded beside EdSharp.exe at run time.
rem Never fails the build: if Ude.dll is absent, the compile drops the HAVEUDE
rem symbol and content detection degrades to byte-order-mark detection with a
rem UTF-8-with-BOM default (BOM-less non-UTF-8 files are not auto-detected).
if exist "FetchUde.ps1" (
  echo Checking encoding-detection library ^(Ude.dll, best-effort^)... >> "!log!"
  powershell -NoProfile -ExecutionPolicy Bypass -File "FetchUde.ps1" >> "!log!" 2>&1
)
set "udeRef="
set "udeDef="
if exist "Ude.dll" set "udeRef=/reference:Ude.dll"
if exist "Ude.dll" set "udeDef=/define:HAVEUDE"
if exist "Ude.dll" (echo Ude.dll present - content encoding detection enabled. >> "!log!") else (echo Ude.dll absent - BOM detection + UTF-8-BOM default only. >> "!log!")

rem ---- compile EdSharp.cs -> EdSharpNG.exe (x64) ----
echo Compiling EdSharp.cs -^> EdSharpNG.exe ...
if exist EdSharpNG.exe del /f /q EdSharpNG.exe
set "icon="
if exist EdSharp.ico set "icon=/win32icon:EdSharp.ico"
"!csc!" /nologo /target:winexe /platform:anycpu /optimize+ !udeDef! %icon% /win32manifest:EdSharp.manifest /reference:"Tektosyne.dll" /reference:"Microsoft.VisualBasic.dll" /reference:"Microsoft.CSharp.dll" /reference:"System.IO.Compression.dll" /reference:"System.IO.Compression.FileSystem.dll" /reference:"!uiaProv!" /reference:"!uiaTypes!" !udeRef! /out:EdSharpNG.exe EdSharp.cs Lbc.cs Say.cs Inix.cs KeyMap.cs Web.cs Pisownia.cs Skladniki.cs Csv.cs Sesja.cs Ustawienia.cs Wyrazenia.cs >> "!log!" 2>&1
if errorlevel 1 goto failed

rem ---- native pre-JIT (ngen) is handled by the INSTALLER, not here ----
rem EdSharp.exe is a managed .NET assembly; the CLR JIT-compiles it at launch.
rem EdSharp_Setup.iss runs "ngen install" against the INSTALLED copy in Program
rem Files (elevated, where it actually takes effect) to give a precompiled native
rem image for faster startup.  Running ngen here, in the build folder and usually
rem without admin, did nothing useful for the installed program, so it was removed.
)

rem ---- best-effort: fetch any MISSING third-party Convert tools ----
rem Only downloads tools that are absent; never fails the build. Edit the
rem $tools table in FetchConvertTools.ps1 to adjust URLs/versions, or delete
rem that file to disable this step.
if exist "FetchConvertTools.ps1" (
  echo Checking third-party Convert tools ^(best-effort, automatic^).  "present" = already in this folder, nothing to download; only MISSING tools are fetched, and any that fail to download are retried on the next build. >> "!log!"
  powershell -NoProfile -ExecutionPolicy Bypass -File "FetchConvertTools.ps1" >> "!log!" 2>&1
)

rem ---- our own helper scripts INTO the fetched tool folders --------------
rem Convert\liblouis is downloaded, so it is in .gitignore and FetchConvertTools
rem empties it before each (re)install -- anything we put there by hand would
rem vanish on the next tool update. Our braille back-translation wrapper is
rem therefore kept in ConvertHelpers\ (which IS committed) and copied in after
rem the download, on every build.
if exist "ConvertHelpers\brl2txt.bat" (
  if exist "Convert\liblouis\" (
    copy /y "ConvertHelpers\brl2txt.bat" "Convert\liblouis\brl2txt.bat" >nul 2>&1
    echo Braille back-translation wrapper copied into Convert\liblouis. >> "!log!"
  ) else (
    echo Convert\liblouis absent - braille back-translation will not work until the tools are fetched. >> "!log!"
  )
)

echo.
echo Build complete:
echo   EdSharpNG.exe  -- the application (x64)
echo. >> "!log!"
echo BUILD COMPLETE: EdSharpNG.exe built successfully. >> "!log!"
echo Finished %DATE% %TIME% >> "!log!"
popd & endlocal & exit /b 0

:failed
echo. >> "!log!"
echo BUILD FAILED - compile errors are listed above in this log. >> "!log!"
echo.
echo BUILD FAILED. Errors from %log%:
type "!log!" | findstr /C:": error" /C:"error CS" /C:"error JS"
popd & endlocal & exit /b 1
