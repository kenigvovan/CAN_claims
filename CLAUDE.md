# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A **Vintage Story mod** implementing a land-claiming system (cities, plots, alliances, conflicts, permissions). It targets `net10.0` and requires the `VINTAGE_STORY` environment variable pointing to the Vintage Story installation directory.

## Build & test commands

```bash
# Build the mod (from repo root)
dotnet build claims/claims/claims.csproj

# Run all tests
dotnet test claims.tests/claims.tests.csproj

# Run a single test class
dotnet test claims.tests/claims.tests.csproj --filter "FullyQualifiedName~AllianceCommandTests"

# Package a release (runs ValidateJson → Build → Package)
cd claims/CakeBuild && dotnet run

# Package with a specific configuration
cd claims/CakeBuild && dotnet run -- --configuration=Debug
```

Build output goes to `claims/claims/bin/<Configuration>/Mods/mod/`. The `Package` task zips the release to `claims/Releases/<name>_<version>.zip`.

## Architecture

### Entry point
`claims/claims/src/claims.cs` — the `ModSystem` subclass. Vintage Story calls `Start()`, `StartServerSide()`, and `StartClientSide()`. Both server and client run in the same process in singleplayer, which is why `claims` has separate static instances (`modInstance`/`clientModInstance`) and separate `DataStorage` objects (`dataStorage`/`clientDataStorage`).

### Central data store
`DataStorage` (`src/DataStorage.cs`) is the in-memory database for the entire mod. It holds:
- `ConcurrentDictionary` maps for `City`, `Alliance`, `Plot`, `PlayerInfo`, `Prison`, `CityPlotsGroup` — keyed by GUID and/or name
- Plot zones indexed by `Vec2i` for spatial lookups
- Client-side mirror data (`ClientSavedPlotsInZones`) that caches server data locally

`DataStorage` is accessed through static references (`claims.dataStorage`, `claims.clientDataStorage`). Tests inject mocks by directly setting these static fields.

### Persistence
`DatabaseHandler` (abstract, `src/database/`) defines the persistence contract. `SQLiteDatabaseHandler` implements it using `Microsoft.Data.Sqlite`. The DB is loaded at server startup via `loadDatabase()` and saved on shutdown via `saveDatabase()`.

### Network
Server and client communicate over a custom channel `"claimsExt"` using protobuf-net packets (in `src/network/packets/`). Handlers are registered in `ServerPacketHandlers` / `ClientPacketHandlers`.

### Domain model
Core entities in `src/part/structure/`:
- `City` — owns plots, citizens, mayor, permissions, optional alliance, plots groups, prisons
- `Alliance` — groups of cities
- `Plot` — a 16×16 block chunk-level claim, belongs to a city
- `PlayerInfo` — player state within the mod
- `Conflict` / `ConflictLetter` — war/conflict system between cities

### Permissions
`PermsHandler` (`src/perms/`) manages per-city rank/group permissions using `PermGroup` and `PermType`.

### Client GUI
- `CANClaimsGui` — main tabbed GUI (opened with Ctrl+Shift+U), with pages for player, alliance, conflicts, plots groups, summons, ranks
- `ClaimsPlayerMovementGUI` — overlay shown when player crosses plot boundaries (opened with K)
- `PlotsMapLayer` — renders claims on the world map

### Harmony patches
`ApplyPatches` (`src/harmony/`) optionally patches game internals (falling blocks, water spread, block access checks) based on config flags.

### External dependencies (not in NuGet)
- `caneconomy.dll` — economy integration, expected at `libs/caneconomy.dll` (tests) or `../../../../Desktop/caneconomy.dll` (main project)
- `VintagestoryAPI.dll`, `VintagestoryLib.dll` — from `$VINTAGE_STORY`
- `ImGui.NET.dll`, `VSImGui.dll`, `RustyShell.dll` — from `libs/`

## Testing approach

Tests use **xUnit + Moq**. Because the mod uses static state, tests set `claims.src.claims.dataStorage`, `claims.src.claims.config`, and `claims.src.claims.economyHandler` directly in the constructor. `DataStorage` is mocked via `Mock<DataStorage>(false)` (passing `false` selects the client-side constructor path to avoid server API calls).
