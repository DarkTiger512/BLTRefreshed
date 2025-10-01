# BLT Enhanced Edition v4.9.1 - Installation Guide

## 📦 Installation Steps

### Simple Installation
1. Extract `BLT-v4.9.1-For-Game-Version-v1.2.12.rar` to your Bannerlord Modules folder
2. Launch Bannerlord and enable the modules
3. **Start streaming!**

### Installation Paths
- **Steam**: `C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\`
- **Epic**: `C:\Program Files\Epic Games\Mount & Blade II Bannerlord\Modules\`
- **GOG**: `C:\Program Files (x86)\GOG Galaxy\Games\Mount & Blade II Bannerlord\Modules\`

## ⚠️ Troubleshooting

**If you get "Cannot load DLL" errors:**
This happens when Windows blocks downloaded files. To fix:

1. **Right-click the downloaded .rar file → Properties → Unblock → OK** (before extracting)
2. **Or after extracting, unblock all DLL files:**
   - Open PowerShell as Administrator  
   - Run: `Get-ChildItem "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules" -Recurse -Filter "*.dll" | Unblock-File`
   - Restart Bannerlord

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