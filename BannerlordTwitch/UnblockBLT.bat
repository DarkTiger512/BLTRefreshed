@echo off
echo ====================================
echo BLT Enhanced Edition - File Unblock
echo ====================================
echo.
echo This script will unblock BLT mod files to prevent loading issues.
echo.
pause

echo Unblocking BLT mod files...

REM Find Bannerlord installation directory
set "BANNERLORD_DIR="
if exist "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord" (
    set "BANNERLORD_DIR=C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord"
) else if exist "C:\Steam\steamapps\common\Mount & Blade II Bannerlord" (
    set "BANNERLORD_DIR=C:\Steam\steamapps\common\Mount & Blade II Bannerlord"
) else if exist "D:\Steam\steamapps\common\Mount & Blade II Bannerlord" (
    set "BANNERLORD_DIR=D:\Steam\steamapps\common\Mount & Blade II Bannerlord"
) else if exist "E:\Steam\steamapps\common\Mount & Blade II Bannerlord" (
    set "BANNERLORD_DIR=E:\Steam\steamapps\common\Mount & Blade II Bannerlord"
)

if "%BANNERLORD_DIR%"=="" (
    echo ERROR: Could not find Bannerlord installation directory.
    echo Please run this script from the Bannerlord Modules directory.
    pause
    exit /b 1
)

echo Found Bannerlord at: %BANNERLORD_DIR%
echo.

REM Unblock BLT module files
powershell -Command "Get-ChildItem '%BANNERLORD_DIR%\Modules\BannerlordTwitch' -Recurse -File | Unblock-File -ErrorAction SilentlyContinue"
powershell -Command "Get-ChildItem '%BANNERLORD_DIR%\Modules\BLTAdoptAHero' -Recurse -File | Unblock-File -ErrorAction SilentlyContinue"
powershell -Command "Get-ChildItem '%BANNERLORD_DIR%\Modules\BLTBuffet' -Recurse -File | Unblock-File -ErrorAction SilentlyContinue"
powershell -Command "Get-ChildItem '%BANNERLORD_DIR%\Modules\BLTConfigure' -Recurse -File | Unblock-File -ErrorAction SilentlyContinue"

echo.
echo ✅ BLT mod files have been unblocked!
echo.
echo You can now launch Bannerlord normally.
echo.
pause