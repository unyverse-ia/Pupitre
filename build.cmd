@echo off
setlocal
cd /d "%~dp0"
rem Compilation 100 %% locale avec le compilateur C# fourni par le .NET Framework 4.8 (inclus dans Windows 10/11).
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "WPF=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" set "WPF=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\WPF"
if not exist dist mkdir dist
if not exist assets mkdir assets

echo [1/3] Icone...
if exist assets\pupitre.ico if "%~1"=="" goto :skipicon
"%CSC%" /nologo /target:exe /optimize+ /codepage:65001 /out:tools\MakeIcon.exe /r:System.Drawing.dll tools\MakeIcon.cs || goto :err
tools\MakeIcon.exe assets\pupitre-icone.png assets\pupitre.ico || goto :err
:skipicon
echo        assets\pupitre.ico (passer "icon" en argument pour la regenerer)

echo [2/3] Compilation de Pupitre.exe...
set "RES="
if exist assets\templates\*.png for %%f in (assets\templates\*.png) do call set "RES=%%RES%% /resource:assets\templates\%%~nxf,templates.%%~nxf"
"%CSC%" /nologo /target:winexe /optimize+ /codepage:65001 /platform:anycpu /out:dist\Pupitre.exe %RES% ^
  /win32manifest:app.manifest /win32icon:assets\pupitre.ico ^
  /r:"%WPF%\PresentationCore.dll" /r:"%WPF%\PresentationFramework.dll" /r:"%WPF%\WindowsBase.dll" ^
  /r:System.Xaml.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Core.dll /r:System.Runtime.Serialization.dll ^
  src\*.cs || goto :err

copy /y tools\restaurer-icones.cmd dist\ >nul

echo [3/3] OK : dist\Pupitre.exe
exit /b 0

:err
echo.
echo ECHEC de la compilation.
exit /b 1
