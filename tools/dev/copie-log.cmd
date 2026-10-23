@echo off
if exist "%APPDATA%\Pupitre\erreurs.log" copy /Y "%APPDATA%\Pupitre\erreurs.log" "%~dp0real\erreurs.log" >nul
if exist "%APPDATA%\Pupitre\demarrage.log" copy /Y "%APPDATA%\Pupitre\demarrage.log" "%~dp0real\demarrage.log" >nul
dir /b "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup" > "%~dp0real\startup.txt" 2>&1
reg query "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v Pupitre > "%~dp0real\run.txt" 2>&1
