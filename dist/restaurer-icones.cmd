@echo off
rem Secours : réaffiche les icônes du bureau Windows si Pupitre les a masquées et ne tourne plus.
rem (Équivalent du clic droit sur le bureau > Affichage > Afficher les éléments du bureau.)
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v HideIcons /t REG_DWORD /d 0 /f >nul
taskkill /f /im explorer.exe >nul 2>&1
start explorer.exe
echo Icones du bureau reaffichees.
