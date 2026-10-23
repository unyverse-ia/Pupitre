@echo off
setlocal
title Pupitre - mise a jour
set "EXE=%~dp0Pupitre.exe"
set "NEW=%~dp0Pupitre.telecharge.exe"
set "OLD=%~dp0Pupitre.precedent.exe"
set "URL=https://github.com/unyverse-ia/Pupitre/raw/main/dist/Pupitre.exe"
set "DATA=%APPDATA%\Pupitre"
echo.
echo  Pupitre - mise a jour depuis GitHub
echo  Les donnees (%DATA%) ne sont pas touchees ; une copie de sauvegarde est faite avant.
echo.
rem ---- 1) sauvegarde des donnees
for /f "tokens=1-3 delims=/ " %%a in ("%DATE%") do set "D=%%c-%%b-%%a"
set "T=%TIME:~0,2%%TIME:~3,2%"
set "T=%T: =0%"
set "BAK=%DATA%\sauvegardes\%D%_%T%"
if not exist "%DATA%\*.json" (
  echo  Aucune donnee existante a sauvegarder.
) else (
  mkdir "%BAK%" >nul 2>&1
  copy /y "%DATA%\*.json" "%BAK%\" >nul
  echo  Sauvegarde des donnees : %BAK%
)
rem ---- 2) telechargement
echo  Telechargement de la derniere version...
if exist "%NEW%" del "%NEW%"
curl.exe -L -f -s -S -o "%NEW%" "%URL%"
if errorlevel 1 (
  echo  Echec du telechargement : rien n'a ete modifie.
  goto :fin
)
for %%s in ("%NEW%") do if %%~zs LSS 500000 (
  echo  Fichier telecharge incomplet : rien n'a ete modifie.
  del "%NEW%"
  goto :fin
)
powershell -NoProfile -ExecutionPolicy Bypass -Command "Unblock-File -LiteralPath '%NEW%'" >nul 2>&1
rem ---- 3) arret propre de Pupitre (il enregistre tout), remplacement, relance
if not exist "%EXE%" goto :remplacer
tasklist /fi "imagename eq Pupitre.exe" 2>nul | find /i "Pupitre.exe" >nul
if errorlevel 1 goto :ferme
"%EXE%" --quitter
set /a N=0
:attente
timeout /t 1 /nobreak >nul
tasklist /fi "imagename eq Pupitre.exe" 2>nul | find /i "Pupitre.exe" >nul
if errorlevel 1 goto :ferme
set /a N+=1
if %N% LSS 8 goto :attente
echo.
echo  Pupitre est encore ouvert (version installee plus ancienne, sans fermeture a distance).
echo  Quittez-le a la main : icone Pupitre de la zone de notification, clic droit, Quitter.
echo  Puis appuyez sur une touche pour continuer.
pause >nul
set /a N=0
:attente2
tasklist /fi "imagename eq Pupitre.exe" 2>nul | find /i "Pupitre.exe" >nul
if errorlevel 1 goto :ferme
timeout /t 1 /nobreak >nul
set /a N+=1
if %N% LSS 15 goto :attente2
echo  Pupitre ne s'est pas ferme : relancez cette mise a jour une fois Pupitre quitte.
del "%NEW%"
goto :fin
:ferme
if exist "%OLD%" del "%OLD%"
if exist "%EXE%" move /y "%EXE%" "%OLD%" >nul
:remplacer
move /y "%NEW%" "%EXE%" >nul
echo  Pupitre mis a jour. Relance...
start "" "%EXE%"
:fin
echo.
pause
