@echo off
:menu
cls
echo ================================
echo   Keycloak Docker Manager
echo ================================
echo.
echo 1. Start services
echo 2. Stop services
echo 3. Restart services
echo 4. View logs
echo 5. Check status
echo 6. Remove all (with volumes)
echo 7. Exit
echo.
set /p choice="Enter your choice (1-7): "

if "%choice%"=="1" goto start
if "%choice%"=="2" goto stop
if "%choice%"=="3" goto restart
if "%choice%"=="4" goto logs
if "%choice%"=="5" goto status
if "%choice%"=="6" goto remove
if "%choice%"=="7" goto end
goto menu

:start
echo Starting services...
docker-compose up -d
echo.
echo Services started!
echo Keycloak: http://localhost:8080
pause
goto menu

:stop
echo Stopping services...
docker-compose down
echo Services stopped!
pause
goto menu

:restart
echo Restarting services...
docker-compose restart
echo Services restarted!
pause
goto menu

:logs
echo Showing logs (Ctrl+C to exit)...
docker-compose logs -f
goto menu

:status
echo Checking status...
docker-compose ps
echo.
pause
goto menu

:remove
echo WARNING: This will remove all containers and volumes!
set /p confirm="Are you sure? (yes/no): "
if /i "%confirm%"=="yes" (
    docker-compose down -v
    echo All removed!
) else (
    echo Cancelled.
)
pause
goto menu

:end
echo Goodbye!