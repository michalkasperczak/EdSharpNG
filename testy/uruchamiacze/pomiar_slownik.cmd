@echo off
setlocal
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set SRC=\\wsl.localhost\Ubuntu\home\michal\projekty\edsharp\testy\pomiar_slownik_menu.cs
set WORK=C:\EdSharpBuild\slowniktest
if not exist "%WORK%" mkdir "%WORK%"
copy /y "%SRC%" "%WORK%\pomiar_slownik_menu.cs" >nul
cd /d "%WORK%"
"%CSC%" /nologo /target:exe /out:pomiar_slownik.exe pomiar_slownik_menu.cs
if errorlevel 1 (echo KOMPILACJA NIE PRZESZLA & exit /b 1)
pomiar_slownik.exe
exit /b %errorlevel%
