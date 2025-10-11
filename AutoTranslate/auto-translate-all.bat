@echo off
chcp 65001
cd /d "%~dp0"

REM Activate virtual environment
if exist venv\Scripts\activate.bat (
    call venv\Scripts\activate.bat
    echo Using virtual environment...
) else (
    echo ERROR: Virtual environment not found!
    echo Please run setup-venv.bat first to create the virtual environment.
    pause
    exit /b 1
)

python auto-translate-v2.py --lang "Português (BR)" --lang-code pt --subdir-override BR --update-changed --account ".key\bannerlordtwitch-d69ae5f7983a.json" "..\BannerlordTwitch\*\_Module\ModuleData\Languages\*.xml"
python auto-translate-v2.py --lang 简体中文 --lang-code zh --subdir-override CNs --update-changed --account ".key\bannerlordtwitch-d69ae5f7983a.json" "..\BannerlordTwitch\*\_Module\ModuleData\Languages\*.xml"
python auto-translate-v2.py --lang 繁體中文 --lang-code zh-TW --subdir-override CNt --update-changed --account ".key\bannerlordtwitch-d69ae5f7983a.json" "..\BannerlordTwitch\*\_Module\ModuleData\Languages\*.xml"
python auto-translate-v2.py --lang Deutsch --lang-code de --update-changed --account ".key\bannerlordtwitch-d69ae5f7983a.json" "..\BannerlordTwitch\*\_Module\ModuleData\Languages\*.xml"
python auto-translate-v2.py --lang Français --lang-code fr --update-changed --account ".key\bannerlordtwitch-d69ae5f7983a.json" "..\BannerlordTwitch\*\_Module\ModuleData\Languages\*.xml"
python auto-translate-v2.py --lang Italiano --lang-code it --update-changed --account ".key\bannerlordtwitch-d69ae5f7983a.json" "..\BannerlordTwitch\*\_Module\ModuleData\Languages\*.xml"
python auto-translate-v2.py --lang 日本語 --lang-code ja --subdir-override JP --update-changed --account ".key\bannerlordtwitch-d69ae5f7983a.json" "..\BannerlordTwitch\*\_Module\ModuleData\Languages\*.xml"
python auto-translate-v2.py --lang 한국어 --lang-code ko --update-changed --account ".key\bannerlordtwitch-d69ae5f7983a.json" "..\BannerlordTwitch\*\_Module\ModuleData\Languages\*.xml"
python auto-translate-v2.py --lang Polski --lang-code pl --update-changed --account ".key\bannerlordtwitch-d69ae5f7983a.json" "..\BannerlordTwitch\*\_Module\ModuleData\Languages\*.xml"
python auto-translate-v2.py --lang Русский --lang-code ru --update-changed --account ".key\bannerlordtwitch-d69ae5f7983a.json" "..\BannerlordTwitch\*\_Module\ModuleData\Languages\*.xml"
python auto-translate-v2.py --lang "Español (LA)" --lang-code es --subdir-override SP --update-changed --account ".key\bannerlordtwitch-d69ae5f7983a.json" "..\BannerlordTwitch\*\_Module\ModuleData\Languages\*.xml"
python auto-translate-v2.py --lang Türkçe --lang-code tr --update-changed --account ".key\bannerlordtwitch-d69ae5f7983a.json" "..\BannerlordTwitch\*\_Module\ModuleData\Languages\*.xml"
