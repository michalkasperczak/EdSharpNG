@echo off
rem Canonical offline-capable source build for accepted EdSharp 130 inputs.
rem Default behavior preserves base fetch behavior; /offline forbids network helpers.
setlocal enableextensions enabledelayedexpansion
pushd "%~dp0"
set "log=BuildEdSharp.log"
set "offline=0"
set "synonymsOutputRoot=%~dp0."
if /i "%~1"=="/offline" set "offline=1"
if /i "%~1"=="/offline" if not "%~2"=="" set "synonymsOutputRoot=%~2"
echo EdSharp canonical build log > "!log!"
echo Started %DATE% %TIME% >> "!log!"
echo offline=!offline! >> "!log!"
echo synonymsOutputRoot=!synonymsOutputRoot! >> "!log!"
for %%F in (EdSharp.cs OpisyPolecen.cs AgreedMenuLayout.cs ControlledCommand.cs PromptCommandUi.cs ProblemReport.cs ProblemReportDialog.cs DialogRozwijaniaSnippetow.cs RozwijanieSnippetow.cs ArchiwumSnippetow.cs Aktualizacja.cs AktualizacjaEdytora.cs AktualizacjaRunner.cs Lbc.cs Say.cs Inix.cs KeyMap.cs Web.cs Pisownia.cs Skladniki.cs Csv.cs Sesja.cs Ustawienia.cs Localizacja\JezykInterfejsu.cs Wyrazenia.cs Synonimy.cs SynonimyOdmiany.cs SlownikiSynonimow.cs FormySynonimow.cs Zadania.cs Zmiany.cs ZapisFormatow.cs PdfExport.cs OriginalFormatSave.cs EdSharp.manifest Tektosyne.dll Ude.dll Synonyms\forms-resource.json Synonyms\plwordnet-forms.bin.gz) do if not exist "%%F" (echo ERROR: %%F not found. >> "!log!" & popd & exit /b 1)
set "csc="
for %%p in ("C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe" "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe" "%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" "%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe") do (if not defined csc if exist %%p set "csc=%%~p")
if not defined csc echo ERROR: No csc.exe found. >> "!log!" & popd & exit /b 1
set "powershellExe=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
if not exist "!powershellExe!" set "powershellExe=powershell"
set "uiaProv="
set "uiaTypes="
set "refDir=C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
if exist "!refDir!\UIAutomationProvider.dll" set "uiaProv=!refDir!\UIAutomationProvider.dll"
if exist "!refDir!\UIAutomationTypes.dll" set "uiaTypes=!refDir!\UIAutomationTypes.dll"
if not defined uiaProv if exist "%SystemRoot%\Microsoft.NET\assembly\GAC_MSIL\UIAutomationProvider\v4.0_4.0.0.0__31bf3856ad364e35\UIAutomationProvider.dll" set "uiaProv=%SystemRoot%\Microsoft.NET\assembly\GAC_MSIL\UIAutomationProvider\v4.0_4.0.0.0__31bf3856ad364e35\UIAutomationProvider.dll"
if not defined uiaTypes if exist "%SystemRoot%\Microsoft.NET\assembly\GAC_MSIL\UIAutomationTypes\v4.0_4.0.0.0__31bf3856ad364e35\UIAutomationTypes.dll" set "uiaTypes=%SystemRoot%\Microsoft.NET\assembly\GAC_MSIL\UIAutomationTypes\v4.0_4.0.0.0__31bf3856ad364e35\UIAutomationTypes.dll"
if not defined uiaProv echo ERROR: UIAutomationProvider.dll not found. >> "!log!" & popd & exit /b 1
if not defined uiaTypes echo ERROR: UIAutomationTypes.dll not found. >> "!log!" & popd & exit /b 1
if "!offline!"=="0" if exist "FetchUde.ps1" "!powershellExe!" -NoProfile -ExecutionPolicy Bypass -File "FetchUde.ps1" >> "!log!" 2>&1
if not exist "Ude.dll" echo ERROR: Ude.dll absent; HAVEUDE is mandatory for this canonical build. >> "!log!" & popd & exit /b 1
echo Preparing local synonym forms... >> "!log!"
"!powershellExe!" -NoProfile -ExecutionPolicy Bypass -File "PrepareSynonyms.ps1" -Root "%~dp0." -OutputRoot "!synonymsOutputRoot!" >> "!log!" 2>&1
if errorlevel 1 goto failed
if exist EdSharpNG.exe del /f /q EdSharpNG.exe
set "icon="
if exist EdSharp.ico set "icon=/win32icon:EdSharp.ico"
> BuildEdSharp.csc.args.rsp (
 echo /nologo
 echo /target:winexe
 echo /platform:anycpu
 echo /optimize+
 echo /define:HAVEUDE
 if defined icon echo !icon!
 echo /win32manifest:EdSharp.manifest
 echo /reference:Tektosyne.dll
 echo /reference:Microsoft.VisualBasic.dll
 echo /reference:Microsoft.CSharp.dll
 echo /reference:System.IO.Compression.dll
 echo /reference:System.IO.Compression.FileSystem.dll
 echo /reference:System.Runtime.Serialization.dll
 echo /reference:"!uiaProv!"
 echo /reference:"!uiaTypes!"
 echo /reference:Ude.dll
 echo /out:EdSharpNG.exe
 echo EdSharp.cs
 echo Aktualizacja.cs
 echo AktualizacjaEdytora.cs
 echo OpisyPolecen.cs
 echo AgreedMenuLayout.cs
 echo ControlledCommand.cs
 echo PromptCommandUi.cs
 echo ProblemReport.cs
 echo ProblemReportDialog.cs
 echo DialogRozwijaniaSnippetow.cs
 echo RozwijanieSnippetow.cs
 echo ArchiwumSnippetow.cs
 echo Lbc.cs
 echo Say.cs
 echo Inix.cs
 echo KeyMap.cs
 echo Web.cs
 echo Pisownia.cs
 echo Skladniki.cs
 echo Csv.cs
 echo Sesja.cs
 echo Ustawienia.cs
 echo Localizacja\JezykInterfejsu.cs
 echo Wyrazenia.cs
 echo Synonimy.cs
 echo SynonimyOdmiany.cs
 echo SlownikiSynonimow.cs
 echo FormySynonimow.cs
 echo Zadania.cs
 echo Zmiany.cs
 echo ZapisFormatow.cs
 echo PdfExport.cs
 echo OriginalFormatSave.cs
)
echo C# compiler: !csc! >> "!log!"
echo C# response: BuildEdSharp.csc.args.rsp >> "!log!"
"!csc!" @BuildEdSharp.csc.args.rsp >> "!log!" 2>&1
if errorlevel 1 goto failed
rem The helper deliberately has its own entry point and must not be linked into EdSharpNG.exe.
echo Building EdSharpUpdate.exe... >> "!log!"
"!csc!" /nologo /target:winexe /platform:anycpu /optimize+ /reference:System.Runtime.Serialization.dll /out:EdSharpUpdate.exe Aktualizacja.cs AktualizacjaRunner.cs >> "!log!" 2>&1
if errorlevel 1 goto failed
if not exist EdSharpUpdate.exe (
 echo ERROR: EdSharpUpdate.exe was not created. >> "!log!"
 goto failed
)
if "!offline!"=="0" if exist "FetchConvertTools.ps1" "!powershellExe!" -NoProfile -ExecutionPolicy Bypass -File "FetchConvertTools.ps1" >> "!log!" 2>&1
rem Keep our wrapper outside fetched Convert\liblouis; install it after the optional fetch.
if exist "ConvertHelpers\brl2txt.bat" (
  if exist "Convert\liblouis\" (
    copy /y "ConvertHelpers\brl2txt.bat" "Convert\liblouis\brl2txt.bat" >nul 2>&1
    if errorlevel 1 (
      echo ERROR: Could not copy braille back-translation wrapper into Convert\liblouis. >> "!log!"
      goto failed
    )
    echo Braille back-translation wrapper copied into Convert\liblouis. >> "!log!"
  ) else (
    echo Convert\liblouis absent - braille back-translation wrapper not installed. >> "!log!"
  )
) else (
  echo ConvertHelpers\brl2txt.bat absent - braille back-translation wrapper not installed. >> "!log!"
)
echo BUILD COMPLETE: EdSharpNG.exe built successfully. >> "!log!"
echo Finished %DATE% %TIME% >> "!log!"
popd & endlocal & exit /b 0
:failed
echo BUILD FAILED - compile errors are listed above in this log. >> "!log!"
popd & endlocal & exit /b 1
