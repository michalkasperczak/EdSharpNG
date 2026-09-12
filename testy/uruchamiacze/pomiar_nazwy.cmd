@echo off
setlocal
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set WORK=C:\EdSharpBuild\nazwytest
if not exist "%WORK%" mkdir "%WORK%"
copy /y "\\wsl.localhost\Ubuntu\home\michal\projekty\edsharp\testy\pomiar_nazwy_skrotow.cs" "%WORK%\pomiar_nazwy_skrotow.cs" >nul
cd /d "%WORK%"
"%CSC%" /nologo /target:exe /out:pomiar_nazwy.exe /r:System.Windows.Forms.dll pomiar_nazwy_skrotow.cs
if errorlevel 1 (echo KOMPILACJA NIE PRZESZLA & exit /b 1)
pomiar_nazwy.exe
exit /b %errorlevel%
