@echo off
setlocal

if "%~1"=="" (
    echo Usage: release.bat ^<version^> [titre] [description]
    echo Exemple: release.bat v1.0.0 "Release WebGL v1.0" "Premiere release"
    exit /b 1
)

set VERSION=%~1
if "%~2"=="" (set TITLE=Release %VERSION%) else (set TITLE=%~2)
if "%~3"=="" (set NOTES=Build WebGL %VERSION%) else (set NOTES=%~3)

set BUILD_DIR=build
set ZIP_FILE=build-%VERSION%.zip

if not exist "%BUILD_DIR%" (
    echo Erreur: le dossier '%BUILD_DIR%' n'existe pas.
    exit /b 1
)

echo Creation du zip...
powershell -Command "Compress-Archive -Path '%BUILD_DIR%\*' -DestinationPath '%ZIP_FILE%' -Force"

echo Creation de la release %VERSION% sur GitHub...
gh release create %VERSION% %ZIP_FILE% --title "%TITLE%" --notes "%NOTES%"

echo Nettoyage...
del %ZIP_FILE%

echo Release %VERSION% creee avec succes !
endlocal
