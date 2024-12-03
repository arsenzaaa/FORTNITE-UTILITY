@echo off & setlocal EnableDelayedExpansion

set "CURRENT_VERSION=0.0.1"
set "GITHUB_URL=https://raw.githubusercontent.com/arsenzaaa/FORTNITE-UTILITY/refs/heads/main/version.txt"
set "RELEASE_URL=https://github.com/arsenzaaa/FORTNITE-UTILITY/releases"
set "VERSION_DIR=C:\Users\Administrator\AppData\Local\FortniteUtility"
set "VERSION_FILE=%VERSION_DIR%\version.txt"

if not exist "%VERSION_DIR%" (
    mkdir "%VERSION_DIR%"
)

for /f "delims=" %%A in ('powershell -command "[datetime]::Now.ToString('yyyy-MM-dd HH:mm:ss')"') do set CURRENT_TIMESTAMP=%%A

if not exist "%VERSION_FILE%" (
    echo time: %CURRENT_TIMESTAMP%> "%VERSION_FILE%"
    echo ver: %CURRENT_VERSION%>> "%VERSION_FILE%"
    echo skip: >> "%VERSION_FILE%"
)

for /f "tokens=1,* delims=: " %%A in (%VERSION_FILE%) do (
    if "%%A"=="time" set "LAST_CHECK=%%B"
    if "%%A"=="ver" set "INSTALLED_VERSION=%%B"
    if "%%A"=="skip" set "SKIP_VERSION=%%B"
)

if "%~1"=="soft" (
    for /f "tokens=1-3 delims=-: " %%A in ("%CURRENT_TIMESTAMP% %LAST_CHECK%") do set /a "time_diff_in_minutes = (%%B-%%D)*43200 + (%%C-%%E)*1440 + (%%D-%%F)*60"
    if !time_diff_in_minutes! LEQ 360 goto :mainMenu
)

for /f "delims=" %%A in ('powershell -command "(Invoke-WebRequest -Uri %GITHUB_URL% -Headers @{\"Cache-Control\"=\"no-cache\"} -TimeoutSec 5).Content" 2^>nul') do set "NEW_VERSION=%%A"
if not defined NEW_VERSION goto :mainMenu

echo time: %CURRENT_TIMESTAMP%> "%VERSION_FILE%"
echo ver: %NEW_VERSION%>> "%VERSION_FILE%"
echo skip: %SKIP_VERSION%>> "%VERSION_FILE%"

if "%NEW_VERSION%"=="%INSTALLED_VERSION%" goto :mainMenu
if "%NEW_VERSION%"=="%SKIP_VERSION%" goto :mainMenu

echo.
echo         NEW VERSION FOUND: %NEW_VERSION%.
echo.
echo        Visit %RELEASE_URL% to download the new version.
echo.

set /p "CHOICE=Skip this update? (Y / N, default: N): " || set "CHOICE=N"
if /i "!CHOICE!"=="Y" (
    echo skip: %NEW_VERSION%>> "%VERSION_FILE%"
) else (
    start %RELEASE_URL%
)

cls
:mainMenu
cls
echo.
echo              FORTNITE UTILITY BY ARSENZA
echo.
echo                    BUY HYPE TWEAKS
echo               https://dsc.gg/hypetweaks
echo.
echo        1. Stop Epic Games Processes (wait for the game to fully load before closing)
echo        2. Clear Fortnite Cache (the game will have to recompile the shaders)
echo        3. Install Fortnite Graphics Settings (GameUserSettings.ini)
echo        4. Exit Fortnite Utility
echo.
set /p choice="Select an option (1-4): "

if "%choice%"=="1" (
    call :stopProcesses
    goto :mainMenu
) else if "%choice%"=="2" (
    call :clearCacheWarning
) else if "%choice%"=="3" (
    call :installConfig
) else if "%choice%"=="4" (
    echo. & echo. & echo        Thank you for using Fortnite Utility.
    timeout 2 > nul 2>&1
    exit
) else (
    echo.
    echo        Invalid choice. Please try again.
    timeout 2 > nul 2>&1
    goto :mainMenu
)

:stopProcesses
cls
echo.
echo        Stopping Epic Games Processes...
echo.
for %%p in (
    EpicGamesLauncher.exe
    EpicWebHelper.exe
    Heroic.exe
    steam.exe
    CrashReportClient.exe
) do (
    taskkill /F /IM %%p >nul 2>&1 && (
        echo [SUCCESS] %%p Process stopped.
    ) || (
        echo [INFO] %%p Process not found.
    )
)
echo.
echo.
echo        All Processes Checked...
echo.
call :showReturnMessage
goto :mainMenu

:clearCacheWarning
cls
echo.
echo        WARNING: Clearing the Fortnite cache will trigger shader recompilation.
echo        The first time you launch the game after this, there will be framerate drops and stutters.
echo        Afterward, everything will stabilize.
echo.
set /p confirm="Are you sure you want to proceed? (Y / N): "

if /i "%confirm%"=="Y" (
    call :moveAndClean
) else (
    echo. & echo     Returning to main menu...
    timeout 2 > nul 2>&1
    goto :mainMenu
)

:moveAndClean
cls
echo.
echo        Clearing Fortnite Cache...
echo.

for %%p in (
    FortniteLauncher.exe
    FortniteClient-Win64-Shipping_EAC_EOS.exe
    FortniteClient-Win64-Shipping.exe
    CrashReportClient.exe
) do (
    taskkill /F /IM %%p >nul 2>&1
    echo %%p Process stopped... || echo %%p Not Found...
)

if exist "%userprofile%\AppData\Local\FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini" (
    move "%userprofile%\AppData\Local\FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini" "%userprofile%\Desktop" > nul 2>&1
)

if not exist "%userprofile%\AppData\Local\FortniteGame\Saved\Config\WindowsClient" (
    mkdir "%userprofile%\AppData\Local\FortniteGame\Saved\Config\WindowsClient"
)

move "%userprofile%\Desktop\GameUserSettings.ini" "%userprofile%\AppData\Local\FortniteGame\Saved\Config\WindowsClient" > nul 2>&1

del /q /f "%userprofile%\AppData\Local\D3DSCache\*" > nul 2>&1
for /d %%p in ("%userprofile%\AppData\Local\D3DSCache\*") do (
    rmdir /s /q "%%p" > nul 2>&1
)

del /q /f "%userprofile%\AppData\LocalLow\NVIDIA\PerDriverVersion\DXCache\*" > nul 2>&1
for /d %%p in ("%userprofile%\AppData\LocalLow\NVIDIA\PerDriverVersion\DXCache\*") do (
    rmdir /s /q "%%p" > nul 2>&1
)

rd "%userprofile%\AppData\Local\CrashReportClient" /s /q > nul 2>&1
rd "%userprofile%\AppData\Local\EpicOnlineServicesUIHelper" /s /q > nul 2>&1
rd "%userprofile%\AppData\Local\EpicGamesLauncher\Saved\Config\CrashReportClient" /s /q > nul 2>&1
rd "%userprofile%\AppData\Local\EpicGamesLauncher\Saved\Logs" /s /q > nul 2>&1
rd "%userprofile%\AppData\Local\EpicGamesLauncher\Saved\webcache*" /s /q > nul 2>&1

for /r "%userprofile%\AppData\Roaming\EasyAntiCheat" %%f in (*.log) do (
    del /f /q "%%f" > nul 2>&1
)

timeout 2 > nul 2>&1

echo.
echo        Cleanup complete.
echo.
call :showReturnMessage
goto :mainMenu

:installConfig
cls
echo.
echo        Installing Fortnite Graphics Settings...
echo.
echo  Do you want to enable performance mode (best performance and minimal latency), or switch to DirectX 12, which will have worse performance but will eliminate most bugs, freezes, crashes, etc.?
echo.
echo        1. Performance Mode (Best performance and minimal latency, bugs, freezes, and crashes may occur)
echo.
echo        2. DirectX 12 (Worse performance, but fewer bugs)
echo. & echo.
set /p choice="Select an option (1-2): "

if "%choice%"=="1" (
    call :installPerformanceMode
) else if "%choice%"=="2" (
    call :switchToDX12
) else (
    echo Invalid choice. Returning to main menu...
    timeout 2 > nul 2>&1
    goto :mainMenu
)

:installPerformanceMode
cls
echo.
echo        Installing Performance Mode...
echo.

for %%p in (
    FortniteLauncher.exe
    FortniteClient-Win64-Shipping_EAC_EOS.exe
    FortniteClient-Win64-Shipping.exe
    CrashReportClient.exe
) do (
    taskkill /F /IM %%p >nul 2>&1
    echo %%p Process stopped... || echo %%p Not Found...
)

powershell -Command "Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/arsenzaaa/FORTNITE-UTILITY/refs/heads/main/GameUserSettings.ini' -OutFile '%temp%\GameUserSettings.ini'"

if not exist "%temp%\GameUserSettings.ini" (
    echo Failed to download the settings file. Please try again later...
    timeout 2 > nul 2>&1
    goto :mainMenu
)

if not exist "%userprofile%\AppData\Local\FortniteGame\Saved\Config\WindowsClient" (
    mkdir "%userprofile%\AppData\Local\FortniteGame\Saved\Config\WindowsClient"
)

move /Y "%temp%\GameUserSettings.ini" "%userprofile%\AppData\Local\FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini" >nul 2>&1

timeout 2 > nul 2>&1

echo.
echo        Performance Mode Applied.
echo.
call :showReturnMessage
goto :mainMenu

:switchToDX12
cls
echo.
echo        Switching to DirectX 12...
echo.

for %%p in (
    FortniteLauncher.exe
    FortniteClient-Win64-Shipping_EAC_EOS.exe
    FortniteClient-Win64-Shipping.exe
    CrashReportClient.exe
) do (
    taskkill /F /IM %%p >nul 2>&1
    echo %%p Process stopped... || echo %%p Not Found...
)

powershell -Command "Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/arsenzaaa/FORTNITE-UTILITY/refs/heads/main/GameUserSettings.ini' -OutFile '%temp%\GameUserSettings.ini'"

if not exist "%temp%\GameUserSettings.ini" (
    echo Failed to download the settings file. Please try again later...
    timeout 2 > nul 2>&1
    goto :mainMenu
)

if not exist "%userprofile%\AppData\Local\FortniteGame\Saved\Config\WindowsClient" (
    mkdir "%userprofile%\AppData\Local\FortniteGame\Saved\Config\WindowsClient"
)

move /Y "%temp%\GameUserSettings.ini" "%userprofile%\AppData\Local\FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini" >nul 2>&1

set "settingsFile=%userprofile%\AppData\Local\FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini"

if exist "%settingsFile%" (
    echo        Modifying DirectX settings...
    powershell -Command "(Get-Content '%settingsFile%') -replace 'PreferredRHI=dx11', 'PreferredRHI=dx12' | Set-Content '%settingsFile%'"
    powershell -Command "(Get-Content '%settingsFile%') -replace 'PreferredFeatureLevel=es31', 'PreferredFeatureLevel=sm6' | Set-Content '%settingsFile%'"
    echo. & echo        DirectX 12 has been applied successfully.
) else (
    echo        Could not find the GameUserSettings.ini file at the expected location.
)

timeout 2 > nul 2>&1
call :showReturnMessage
goto :mainMenu

:showReturnMessage
echo. & echo     Press any key to return to the menu... & timeout 10 > nul 2>&1 & goto :mainMenu
