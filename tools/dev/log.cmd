@echo off
type "%APPDATA%\Pupitre\erreurs.log" > "%~dp0log.txt" 2>&1
echo FIN >> "%~dp0log.txt"
