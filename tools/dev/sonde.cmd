@echo off
set OUT=%~dp0sonde.txt
echo APPDATA=%APPDATA% > "%OUT%"
echo USERPROFILE=%USERPROFILE% >> "%OUT%"
echo LOCALAPPDATA=%LOCALAPPDATA% >> "%OUT%"
echo --- Roaming\Pupitre >> "%OUT%"
dir /a "%USERPROFILE%\AppData\Roaming\Pupitre" >> "%OUT%" 2>&1
echo --- C:\Users >> "%OUT%"
dir /ad C:\Users >> "%OUT%" 2>&1
echo --- Packages cache >> "%OUT%"
dir /a "C:\Users\conta\AppData\Local\Packages\Claude_pzs8sxrjxfjjc\LocalCache\Roaming\Pupitre" >> "%OUT%" 2>&1
echo --- ProfileList >> "%OUT%"
reg query "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList" >> "%OUT%" 2>&1
if not exist "%~dp0real" mkdir "%~dp0real"
copy /y "%USERPROFILE%\AppData\Roaming\Pupitre\*.json" "%~dp0real\" >> "%OUT%" 2>&1
echo FIN >> "%OUT%"
