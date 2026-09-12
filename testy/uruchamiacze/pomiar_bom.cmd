@echo off
setlocal
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set WORK=C:\EdSharpBuild\bomtest
if not exist "%WORK%" mkdir "%WORK%"
copy /y "\\wsl.localhost\Ubuntu\home\michal\projekty\edsharp\testy\pomiar_ini_bom.cs" "%WORK%\pomiar_ini_bom.cs" >nul
cd /d "%WORK%"
"%CSC%" /nologo /target:exe /out:pomiar_bom.exe pomiar_ini_bom.cs
if errorlevel 1 (echo KOMPILACJA NIE PRZESZLA & exit /b 1)
pomiar_bom.exe C:\EdSharp\Hotkeys.ini
exit /b %errorlevel%
