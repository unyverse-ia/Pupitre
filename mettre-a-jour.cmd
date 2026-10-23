@echo off
setlocal
title Pupitre - mise a jour
set "EXE=%~dp0Pupitre.exe"
set "NEW=%~dp0Pupitre.telecharge.exe"
set "URL=https://github.com/unyverse-ia/Pupitre/raw/main/Pupitre.exe"
set "DATA=%APPDATA%\Pupitre"
echo.
echo  Pupitre - mise a jour depuis GitHub
echo  ---------------------------------------------------------------------------
echo  1. Vos donnees (%DATA%) sont copiees dans un dossier de sauvegarde date.
echo  2. La derniere version de Pupitre.exe est telechargee.
echo  3. Pupitre est ferme (automatiquement, ou vous serez invite a le quitter),
echo     Pupitre.exe est remplace, puis Pupitre est relance.
echo  ---------------------------------------------------------------------------
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
  echo  Echec du telechargement : rien n'a ete modifie. Verifiez la connexion et relancez.
  goto :fin
)
for %%s in ("%NEW%") do if %%~zs LSS 500000 (
  echo  Fichier telecharge incomplet : rien n'a ete modifie.
  del "%NEW%"
  goto :fin
)
powershell -NoProfile -ExecutionPolicy Bypass -Command "Unblock-File -LiteralPath '%NEW%'" >nul 2>&1
rem ---- 3) fermeture de Pupitre, remplacement, relance
tasklist /fi "imagename eq Pupitre.exe" 2>nul | find /i "Pupitre.exe" >nul
if errorlevel 1 goto :remplacer
if not exist "%EXE%" goto :manuel
echo  Fermeture de Pupitre (il enregistre d'abord ses donnees)...
"%EXE%" --quitter
set /a N=0
:attente
timeout /t 1 /nobreak >nul
tasklist /fi "imagename eq Pupitre.exe" 2>nul | find /i "Pupitre.exe" >nul
if errorlevel 1 goto :remplacer
set /a N+=1
if %N% LSS 8 goto :attente
:manuel
echo.
echo  Pupitre est toujours ouvert : cette version ne sait pas se fermer a distance.
echo  >> Quittez Pupitre maintenant : clic droit sur son icone dans la zone de
echo     notification (en bas a droite, pres de l'horloge), puis "Quitter Pupitre".
echo  >> Ensuite, appuyez sur une touche ici pour continuer la mise a jour.
pause >nul
set /a N=0
:attente2
tasklist /fi "imagename eq Pupitre.exe" 2>nul | find /i "Pupitre.exe" >nul
if errorlevel 1 goto :remplacer
timeout /t 1 /nobreak >nul
set /a N+=1
if %N% LSS 15 goto :attente2
echo  Pupitre est encore ouvert : relancez cette mise a jour une fois Pupitre quitte.
del "%NEW%"
goto :fin
:remplacer
if exist "%EXE%" del /f "%EXE%"
move /y "%NEW%" "%EXE%" >nul
echo  Pupitre mis a jour. Relance...
start "" "%EXE%"
:fin
echo.
pause
