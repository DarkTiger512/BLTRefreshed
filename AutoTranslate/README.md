# AutoTranslate - BannerlordTwitch Localization

Automatic translation system for translating mod text into 13 languages using Google Cloud Translation API.

## Setup

### First Time Setup

1. **Create Virtual Environment**:
   ```powershell
   cd AutoTranslate
   .\setup-venv.bat
   ```

   This will:
   - Create a local Python virtual environment in `AutoTranslate/venv/`
   - Install all required dependencies
   - Keep everything isolated from your system Python

2. **Add Google Cloud Credentials**:
   - Place your Google Cloud service account JSON file in `AutoTranslate/.key/`
   - File should be named: `bannerlordtwitch-d69ae5f7983a.json`

## Usage

### Run All Translations

```powershell
cd AutoTranslate
.\auto-translate-all.bat
```

This will translate all localization files into:
- 🇧🇷 Portuguese (Brazilian)
- 🇨🇳 Chinese (Simplified)
- 🇹🇼 Chinese (Traditional)
- 🇩🇪 German
- 🇫🇷 French
- 🇮🇹 Italian
- 🇯🇵 Japanese
- 🇰🇷 Korean
- 🇵🇱 Polish
- 🇷🇺 Russian
- 🇪🇸 Spanish (Latin America)
- 🇹🇷 Turkish

### Run Single Language

```powershell
cd AutoTranslate
venv\Scripts\activate.bat
python auto-translate-v2.py --lang "Deutsch" --lang-code de --update-changed --account ".key\bannerlordtwitch-d69ae5f7983a.json" "..\BannerlordTwitch\*\_Module\ModuleData\Languages\*.xml"
```

## How It Works

1. Reads English XML localization files
2. Extracts text strings while preserving placeholders like `{HERO_NAME}`
3. Sends to Google Cloud Translate API
4. Creates translated XML files in language-specific subdirectories
5. Updates existing translations if source text changed (`--update-changed` flag)

## Files

- **setup-venv.bat** - Creates virtual environment and installs dependencies
- **auto-translate-all.bat** - Translates all languages at once
- **auto-translate-v2.py** - Main translation script
- **requirements.txt** - Python package dependencies
- **venv/** - Virtual environment (not tracked in git)

## Notes

- The virtual environment is **NOT** committed to git (see `.gitignore`)
- Each developer needs to run `setup-venv.bat` once
- Google Cloud API key is required and should be kept private
- Translations preserve game variable placeholders automatically
