@echo off
setlocal
cd /d "%~dp0"
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /out:RichCopyProbe.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll pomiar_bogaty_schowek_5114.cs
if errorlevel 1 exit /b 1
for /L %%I in (1,1,3) do (
  RichCopyProbe.exe "%CD%\EdSharpNG.exe" "%CD%\rich-copy-fixture.md"
  if errorlevel 1 exit /b 1
)
