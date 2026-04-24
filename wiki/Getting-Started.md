# Getting Started

This guide walks you through your first steps with CAN Claims.

## Creating Your First City

1. Find a location where you want to establish your city
2. Make sure you have enough money (default: **150** coins)
3. Run the command:
   ```
   /city create <cityName>
   ```
4. The plot (16x16 block chunk) you are standing on becomes your **Main City Plot**

> **Note:** Your city must be at least 3 plots away from any other city (configurable by server admin).

## Claiming More Land

Once your city is created, you can expand by claiming adjacent plots:

```
/city claim
```

Stand on the plot you want to claim. The number of plots you can claim depends on your [city level](Cities#city-levels), which grows as more citizens join.

## Inviting Players

To grow your city, invite other players:

```
/city invite <playerName>
```

The invited player can accept with:
```
/accept <cityName>
```

Or deny with:
```
/deny <cityName>
```

## Basic Plot Management

### View Plot Info
```
/plot here
```

### Show Plot Borders
```
/plot borders on
```

### Set Plot Permissions
```
/plot set permissions <group> <use|build|attack> <on|off>
```

Groups: `citizen`, `stranger`, `ally`

**Example:** Allow allies to use containers on your plot:
```
/plot set p ally use on
```

## Chat Channels

Switch between chat channels:
- `/gc` — Global chat (everyone)
- `/cc` — City chat (city members only)
- `/lc` — Local chat (nearby players within 100 blocks)

## Checking Prices

See all current costs and prices:
```
/citizen prices
```

## Checking Time Until Next Day

The mod has its own day cycle for processing fees and maintenance:
```
/citizen nextdaytimer
```

## Using the GUI

Most actions also have a point-and-click interface. Press **U** to open the main window — it has tabs for your city, plot, prices, prison, summons, and plot groups. Press **K** to see info about the plot you are standing on. See [GUI](GUI) for details.

## Next Steps

- [GUI](GUI) — Hotkeys, tabs, HUD, and world map
- [Cities](Cities) — Learn about city levels, settings, and advanced management
- [Plots](Plots) — Understand different plot types and permissions
- [Commands](Commands) — Full command reference
