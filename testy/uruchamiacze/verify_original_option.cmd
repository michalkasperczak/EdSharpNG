@echo off
setlocal
cd /d "%~dp0"
set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
"%CSC%" /nologo /langversion:5 /out:OriginalTransactionProbe.exe /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll harness_zapis_oryginalu.cs OriginalFormatSave.cs ZapisFormatow.cs
if errorlevel 1 exit /b 1
OriginalTransactionProbe.exe
if errorlevel 1 exit /b 1
"%CSC%" /nologo /out:OriginOptionProbe.exe pomiar_opcji_formatu_zrodlowego.cs
if errorlevel 1 exit /b 1
OriginOptionProbe.exe "%CD%\EdSharpNG.exe"
if errorlevel 1 exit /b 1
"%CSC%" /nologo /out:OriginIntegration2.exe harness_integracja_oryginalu.cs
if errorlevel 1 exit /b 1
OriginIntegration2.exe "%CD%\EdSharpNG.exe" "%CD%\EdSharp.ini"
if errorlevel 1 exit /b 1
"%CSC%" /nologo /reference:EdSharpNG.exe /out:SessionProbe.exe pomiar_sesja.cs
if errorlevel 1 exit /b 1
SessionProbe.exe
if errorlevel 1 exit /b 1
exit /b 0
