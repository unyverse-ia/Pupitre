@echo off
if exist "%APPDATA%\Pupitre\erreurs.log" copy /Y "%APPDATA%\Pupitre\erreurs.log" "%~dp0real\erreurs.log" >nul
