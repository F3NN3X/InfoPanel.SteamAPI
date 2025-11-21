# Pull Request

## Description
Removed the unused "Token Management" system and associated configuration settings.
This system was intended for advanced token management (Community/Store tokens) but was never fully implemented or used in the current architecture.

## Changes
- Deleted `InfoPanel.SteamAPI/Services/SteamTokenService.cs` (dead code).
- Removed `[Token Management]` section and keys from `InfoPanel.SteamAPI.dll.ini`.
- Removed `Token Management` constants, properties, and initialization logic from `ConfigurationService.cs`.

## Verification
- Verified that `SteamTokenService` is not used in the codebase.
- Built the project successfully.
