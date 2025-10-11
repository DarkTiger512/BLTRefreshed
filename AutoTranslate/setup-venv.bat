@echo off
echo Setting up Python virtual environment for AutoTranslate...
cd /d "%~dp0"

REM Create virtual environment
echo Creating virtual environment...
python -m venv venv

REM Activate and install dependencies
echo Installing dependencies...
call venv\Scripts\activate.bat
pip install --upgrade pip
pip install -r requirements.txt

echo.
echo ========================================
echo Virtual environment setup complete!
echo ========================================
echo.
echo To activate the environment, run:
echo   AutoTranslate\venv\Scripts\activate.bat
echo.
echo To run translations, run:
echo   auto-translate-all.bat
echo.
pause
