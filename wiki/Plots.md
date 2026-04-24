# Plots

Plots are 16x16 block chunks that form the territorial basis of cities. Each claimed plot is protected from unauthorized access by non-members.

## Claiming Plots

### City Plots
Claim a plot for your city (must be adjacent to existing city plots):
```
/city claim
```

### Personal Plots
Buy a plot for personal use:
```
/plot claim
```

### Unclaiming
```
/city unclaim    — Unclaim a city plot
/plot unclaim    — Unclaim a personal plot
```

## Plot Types

Each plot type has a different daily maintenance cost and special features:

| Type | Daily Cost | Description |
|------|-----------|-------------|
| **Default** | 1 | Standard plot, no special features |
| **Main City** | 3 | The founding plot of a city (auto-assigned) |
| **Farm** | 6 | Agricultural land |
| **Temple** | 5 | Respawn point for city members; uses special Temple blocks |
| **Summon** | 7 | Teleportation point for city members |
| **Embassy** | 8 | Plot that can be owned by players from other cities |
| **Tavern** | 9 | Supports inner claims (sub-areas with separate permissions) |
| **Tournament** | 3 | PvP always enabled regardless of city settings |
| **Camp** | 4 | Alliance camp plots (limited by alliance level) |
| **Prison** | 3 | Contains prison cells for holding captured players |

Set a plot's type:
```
/plot set type <type>
```

## Plot Permissions

Each plot has three permission groups:
- **Citizen** — Members of the city that owns the plot
- **Stranger** — Players not in the city
- **Ally** — Members of allied cities (through alliance)

Each group can be granted or denied:
- **Use** — Interact with containers, doors, and blocks
- **Build** — Place and break blocks
- **Attack** — Attack animals on the plot

### Setting Permissions
```
/plot set permissions <group> <use|build|attack> <on|off>
```

**Shorthand:**
```
/plot set p citizen build on
/plot set p stranger use off
/plot set p ally use on
```

## Plot Flags

| Flag | Command | Description |
|------|---------|-------------|
| **PvP** | `/plot set pvp on/off` | Allow player-vs-player combat |
| **Fire** | `/plot set fire on/off` | Allow fire to spread |
| **Blast** | `/plot set blast on/off` | Allow explosion damage |

## Plot Info and Display

```
/plot here              — Show info about the current plot
/plot borders on/off    — Highlight plot boundaries visually
/plot plotmsgs <0-3>    — Message display: 0=nothing, 1=messages only, 2=HUD only, 3=both
```

## Selling Plots

Plot owners can list plots for sale:
```
/plot fs <price>        — Set plot for sale (alias: /plot forsale)
/plot nfs               — Remove from sale (alias: /plot notforsale)
```

## Plot Settings

```
/plot set name <name>   — Set a custom name for the plot
/plot set fee <amount>  — Set a rental/maintenance fee for the plot
```

## Tavern Inner Claims

Tavern plots support up to **3 inner claims** — sub-areas within the plot with independent access control:
- Each inner claim defines a rectangular area (two 3D positions)
- Inner claims have their own permission flags (use, build, attack)
- Members can be added individually to each inner claim

```
/plot innerclaim        — Manage inner claims
```

Max taverns per city: **3** (configurable).

## Protected Animals

On claimed city plots, certain animals are protected from being killed by strangers:
- Bighorn (lamb, ewe, ram)
- Chickens (rooster, chick, hen)
- Pigs (sow, boar, piglet)

## Special Protections

Claimed plots also protect against:
- **Falling blocks** — Blocks from outside won't fall into claimed plots (configurable)
- **Water flow** — Water from outside won't flow into claimed plots (configurable)
- **Always-accessible blocks** — Certain blocks (mailboxes, market stalls) remain usable regardless of permissions
