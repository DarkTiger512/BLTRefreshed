# Copilot Instructions for Bannerlord Twitch Enhanced Edition

<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

## Project Overview
This is a Mount & Blade II: Bannerlord mod project written in C# that integrates Twitch functionality with the game. The project consists of multiple modules:

- **BannerlordTwitch**: Core Twitch integration module
- **BLTAdoptAHero**: Hero adoption system for Twitch viewers
- **BLTBuffet**: Buffet/reward system for Twitch interactions
- **BLTConfigure**: Configuration module for BLT settings

## Development Guidelines
- This project targets .NET Framework and uses the Bannerlord modding API
- Follow Bannerlord modding conventions and patterns
- Use TaleWorlds' modding framework and attributes properly
- Maintain compatibility with the latest Bannerlord version (v1.2.12)
- Consider Twitch API integration patterns when working with streaming features

## Build System
- **Always use JetBrains Rider MSBuild**: `"C:\Program Files\JetBrains\JetBrains Rider 2025.2.2.1\tools\MSBuild\Current\Bin\MSBuild.exe"`
- Build command syntax: `& "C:\Program Files\JetBrains\JetBrains Rider 2025.2.2.1\tools\MSBuild\Current\Bin\MSBuild.exe" "BannerlordTwitch.sln" /p:Configuration=Debug /verbosity:minimal`
- Never use dotnet CLI or other MSBuild installations for this project

## Code Style
- Use C# naming conventions
- Follow existing project structure and patterns
- Ensure proper error handling for Twitch API calls
- Maintain backward compatibility when possible

## Testing
- Test with actual Bannerlord game instances
- Verify Twitch integration functionality
- Ensure mod loading works correctly in the game launcher
