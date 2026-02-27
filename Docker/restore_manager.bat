@echo off
:menu
cls
echo ================================
echo   Restore from dump manager
echo ================================
echo.
echo 1. Restore Keycloak db
echo 2. Exit
echo.
set /p choice="Enter your choice (1-2): "

if "%choice%"=="1" goto Keycloak
if "%choice%"=="2" goto end
goto menu

:Keycloak
echo Try to restore Keycloak database...
echo .
echo Stopping Keycloak container...
docker stop keycloak_app

echo Restoring Keycloak database from dump...
docker run --rm --network=host -v "%cd%\keycloak.dump":/backup/keycloak.dump -e PGPASSWORD="keycloak" postgres:17 pg_restore --host=host.docker.internal --port=5432 --username=keycloak --dbname=keycloak --clean --if-exists /backup/keycloak.dump

@REM if %ERRORLEVEL% NEQ 0 (
@REM      echo.
@REM      echo ERROR: Database restore failed!
@REM      pause
@REM      goto menu
@REM  )

echo.
echo Done!
pause
goto menu

:end
echo Goodbye!