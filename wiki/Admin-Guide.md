# Admin Guide

Server administrators can use the `/cadmin` command to manage cities, plots, and world settings. All admin commands require the `controlserver` privilege and membership in a role listed in `ROLE_CODES_WITH_ADMIN_RIGHTS` (default: `admin`).

## Event Triggers

| Command | Description |
|---------|-------------|
| `/cadmin nday` | Trigger new day processing (fees, maintenance, etc.) |
| `/cadmin nhour` | Trigger new hour processing |
| `/cadmin backup` | Create a manual database backup |

## City Administration (`/cadmin city`)

### Creating and Deleting
| Command | Arguments | Description |
|---------|-----------|-------------|
| `/cadmin city new` | `<player> <cityName>` | Create a city for a player |
| `/cadmin city delete` | `<cityName>` | Delete a city |

### Claiming Land
| Command | Arguments | Description |
|---------|-----------|-------------|
| `/cadmin city claim` | `<player> <cityName>` | Claim current plot for a city |
| `/cadmin city radiusclaim` | `<player> <cityName> <radius>` | Claim plots in a radius |
| `/cadmin city unclaim` | — | Unclaim current plot |

### Player Management
| Command | Arguments | Description |
|---------|-----------|-------------|
| `/cadmin city add` | `<player> <cityName>` | Add player to a city |
| `/cadmin city kick` | `<player> <cityName>` | Remove player from a city |

### City Settings
| Command | Arguments | Description |
|---------|-----------|-------------|
| `/cadmin city set name` | `<cityName>` | Rename a city |
| `/cadmin city set mayor` | `<player>` | Set city mayor |
| `/cadmin city set pvp` | `on/off` | Toggle city PvP |
| `/cadmin city set fire` | `on/off` | Toggle city fire spread |
| `/cadmin city set blast` | `on/off` | Toggle city explosions |
| `/cadmin city set open` | `on/off` | Toggle open/closed |
| `/cadmin city set fee` | `<amount>` | Set city fee |
| `/cadmin city set technical` | `on/off` | Toggle technical city status |
| `/cadmin city set bonusclaims` | `<amount>` | Set bonus claim plots |

## Plot Administration (`/cadmin plot`)

| Command | Arguments | Description |
|---------|-----------|-------------|
| `/cadmin plot set permissions` | `...` | Set plot permissions (alias: `p`) |
| `/cadmin plot set pvp` | `on/off` | Toggle plot PvP |
| `/cadmin plot set fire` | `on/off` | Toggle plot fire spread |
| `/cadmin plot set blast` | `on/off` | Toggle plot explosions |
| `/cadmin plot type` | `<plotType>` | Set plot type |
| `/cadmin plot fee` | `<amount>` | Set plot fee |
| `/cadmin plot fs` | `<price>` | Force set plot for sale |

## World Settings (`/cadmin world`)

| Command | Arguments | Description |
|---------|-----------|-------------|
| `/cadmin world info` | — | Display world statistics |
| `/cadmin world set blastew` | `on/off` | Blast everywhere |
| `/cadmin world set pvpew` | `on/off` | PvP everywhere |
| `/cadmin world set fireew` | `on/off` | Fire everywhere |
| `/cadmin world set pvpfb` | `on/off` | PvP fallback |
| `/cadmin world set firefb` | `on/off` | Fire fallback |
| `/cadmin world set blastfb` | `on/off` | Blast fallback |

## War Administration

| Command | Arguments | Description |
|---------|-----------|-------------|
| `/cadmin startwar` | `<party1> <party2>` | Force start a war between two parties |
| `/cadmin setbattledate` | `<party1> <party2> [minutesDelay] [durationMinutes]` | Schedule a battle |

## Backup System

The mod automatically creates backups at configured times (default: 6:00, 12:00, 18:00, 0:00). Files:
- `backup_daily_claims.db` — Daily automatic backup
- `backup_hourly_claims.db` — Hourly automatic backup
- `backup_manually_claims.db` — Manual backup via `/cadmin backup`

## Tips for Admins

- Use `/cadmin nday` to test daily processing without waiting
- Use `/cadmin city radiusclaim` for quick city expansion
- Set `VERBOSE_LOGGING` to `true` in config for debugging
- Use `ROLE_CODES_WITH_ADMIN_RIGHTS` to grant admin access to specific roles
- The `technical` flag on cities can be used for special administrative/NPC cities
- Use `bonusclaims` to grant extra plots without changing the level system
