# Installation

## Requirements

- Vintage Story 1.20+
- .NET 10.0 runtime (included with Vintage Story)

## Server-Side Installation

1. Download the latest release from the [Releases page](https://github.com/kenigvovan/CAN_claims/releases)
2. Place the mod `.zip` file into your server's `Mods` folder
3. Start the server — the mod will generate a default `claims.json` configuration file
4. Edit `claims.json` to customize settings (see [Configuration](Configuration))
5. Restart the server to apply changes

## Client-Side

Players connecting to a server running CAN Claims do not need to install the mod separately — the server handles synchronization.

## Optional Dependencies

- **Economy mods** — CAN Claims can integrate with economy mods via the `SELECTED_ECONOMY_HANDLER` config option. Compatible mods include [VinConomy](https://github.com/kenigvovan) and others that provide currency systems.

## First Launch

On first launch, the mod will create:
- `claims.json` — Main configuration file
- `claims.db` — SQLite database for persistent data
- `claims_permissions.json` — Permissions data
- `city_level_info.json` — City leveling configuration
- `alliance_level_info.json` — Alliance leveling configuration

All files are stored in the mod config directory by default, or in a custom path if `PATH_TO_DB_AND_JSON_FILES` is configured.
