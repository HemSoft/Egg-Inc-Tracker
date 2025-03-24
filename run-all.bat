@echo off
echo Starting Egg Inc Tracker components...
echo.

REM Set up color coding for console output
set "apiColor=0A"    REM Green text on black
set "funcColor=0B"   REM Cyan text on black
set "webColor=0E"    REM Yellow text on black

REM Check if Azurite is running (checking port 10000)
echo Checking if Azurite is running...
netstat -ano | findstr :10000 > nul
if %ERRORLEVEL% NEQ 0 (
    echo Azurite not detected. Starting Azurite...
    call azur
    timeout /t 3 > nul
) else (
    echo Azurite is already running.
)

echo Starting API (https://localhost:5000)...
start "Egg Inc API" cmd.exe /k "color %apiColor% && cd /d %~dp0EggIncTrackerApi && dotnet run --urls=https://localhost:5000"

echo Starting Azure Function...
start "Egg Inc Function" cmd.exe /k "color %funcColor% && cd /d %~dp0UpdatePlayerFunction && func start"

echo Starting EggDash Web App...
start "Egg Inc WebApp" cmd.exe /k "color %webColor% && cd /d %~dp0EggDash && dotnet run"

echo.
echo All components started in separate windows.
echo - Green console: API
echo - Cyan console: Azure Function
echo - Yellow console: Web App
echo.
echo Press any key to close this window...
pause > nul
