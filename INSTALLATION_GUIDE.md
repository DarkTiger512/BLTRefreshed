# BLT Enhanced Edition v4.9.1 - Installation Guide

## 📦 Installation Steps

### Method 1: Automatic (Recommended)
1. Extract `BLT-v4.9.1-For-Game-Version-v1.2.12.rar` to your Bannerlord Modules folder
2. **Run `UnblockBLT.bat` as Administrator** (fixes Windows file blocking)
3. Launch Bannerlord and enable the modules

### Method 2: Manual  
1. Extract the mod files to: `Mount & Blade II Bannerlord\Modules\`
2. **If you get "Cannot load DLL" errors:**
   - Open PowerShell as Administrator
   - Run: `Get-ChildItem "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules" -Recurse -Filter "*.dll" | Unblock-File`
   - Restart Bannerlord

## ⚠️ Troubleshooting "Cannot load DLL" Error

This error occurs because Windows blocks files downloaded from the internet. 

**Quick Fix:**
1. Right-click `UnblockBLT.bat` → "Run as administrator"
2. Or manually unblock files in Windows Explorer (right-click DLL → Properties → Unblock)

## 🎯 Module Load Order
Make sure modules load in this order:
1. Bannerlord.Harmony
2. Native
3. SandBoxCore
4. Sandbox  
5. StoryMode
6. CustomBattle
7. **BannerlordTwitch** ← Main module
8. BLTAdoptAHero
9. BLTBuffet
10. BLTConfigure

## 🆘 Still Having Issues?

1. **Add Windows Defender exclusion:** Add the entire Bannerlord folder to Windows Defender exclusions
2. **Run as Administrator:** Launch Bannerlord as Administrator
3. **Clear mod cache:** Delete `Documents\Mount and Blade II Bannerlord\Configs\ModuleData` folder
4. **Verify game files:** Steam → Bannerlord → Properties → Local Files → Verify integrity

## ✨ New in v4.9.1
- **Update Notification System**: Automatic GitHub version checking with bottom-left notifications
- **Enhanced User Experience**: Clear status messages when updates are available or mod is current
- **Improved Compatibility**: Better error handling and loading stability