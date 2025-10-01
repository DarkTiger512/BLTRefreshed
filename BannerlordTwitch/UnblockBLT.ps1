# BLT Enhanced Edition - File Unblock Script
# This script automatically unblocks BLT mod files to prevent loading issues

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "BLT Enhanced Edition - File Unblock" -ForegroundColor Cyan  
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Function to find Bannerlord directory
function Find-BannerlordDirectory {
    $commonPaths = @(
        "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord",
        "C:\Steam\steamapps\common\Mount & Blade II Bannerlord", 
        "D:\Steam\steamapps\common\Mount & Blade II Bannerlord",
        "E:\Steam\steamapps\common\Mount & Blade II Bannerlord",
        "C:\Program Files\Steam\steamapps\common\Mount & Blade II Bannerlord"
    )
    
    foreach ($path in $commonPaths) {
        if (Test-Path $path) {
            return $path
        }
    }
    return $null
}

# Find Bannerlord installation
$bannerlordDir = Find-BannerlordDirectory

if (-not $bannerlordDir) {
    Write-Host "❌ ERROR: Could not find Bannerlord installation directory." -ForegroundColor Red
    Write-Host "Please make sure Bannerlord is installed via Steam." -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "✅ Found Bannerlord at: $bannerlordDir" -ForegroundColor Green
Write-Host ""

# Unblock BLT module files
$modules = @("BannerlordTwitch", "BLTAdoptAHero", "BLTBuffet", "BLTConfigure")
$totalUnblocked = 0

foreach ($module in $modules) {
    $modulePath = Join-Path $bannerlordDir "Modules\$module"
    if (Test-Path $modulePath) {
        Write-Host "Unblocking $module files..." -ForegroundColor Yellow
        try {
            $files = Get-ChildItem $modulePath -Recurse -File
            $files | Unblock-File -ErrorAction SilentlyContinue
            $count = $files.Count
            $totalUnblocked += $count
            Write-Host "  ✅ Unblocked $count files in $module" -ForegroundColor Green
        } catch {
            Write-Host "  ⚠️  Warning: Could not unblock some files in $module" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  ℹ️  Module $module not found (may not be installed)" -ForegroundColor Blue
    }
}

Write-Host ""
Write-Host "🎉 Successfully unblocked $totalUnblocked BLT mod files!" -ForegroundColor Green
Write-Host ""
Write-Host "You can now launch Bannerlord normally." -ForegroundColor White
Write-Host "If you still have issues, try running Bannerlord as Administrator." -ForegroundColor Yellow
Write-Host ""
Read-Host "Press Enter to exit"