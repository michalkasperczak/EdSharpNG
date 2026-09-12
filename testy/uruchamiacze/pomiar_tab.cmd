@echo off
setlocal
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set SRC=\\wsl.localhost\Ubuntu\home\michal\projekty\edsharp\testy\pomiar_tab_przyciskow.cs
set WORK=C:\EdSharpBuild\tabtest
if not exist "%WORK%" mkdir "%WORK%"
copy /y "%SRC%" "%WORK%\pomiar_tab_przyciskow.cs" >nul
copy /y "\\wsl.localhost\Ubuntu\home\michal\projekty\edsharp\EdSharpNG.exe" "%WORK%\EdSharpNG.exe" >nul
cd /d "%WORK%"
"%CSC%" /nologo /target:exe /out:pomiar_tab.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll pomiar_tab_przyciskow.cs
if errorlevel 1 (echo KOMPILACJA NIE PRZESZLA & exit /b 1)
pomiar_tab.exe EdSharpNG.exe
exit /b %errorlevel%
