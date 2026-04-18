# Cities

Cities are the core organizational unit in CAN Claims. They provide territory protection, governance, and a community structure for players.

## Creating a City

```
/city create <cityName>
```

**Requirements:**
- Cost: **150** coins (configurable: `NEW_CITY_COST`)
- Must be at least 3 plots from another city (`MIN_DISTANCE_FROM_OTHER_CITY_NEW_CITY`)
- City name max length: 40 characters (`MAX_LENGTH_CITY_NAME`)

The plot you stand on becomes the **Main City Plot**.

## City Levels

Cities level up based on the number of citizens. Higher levels grant more plots and features.

| Citizens | Level | Max Plots | Extra Plots Buyable | Summon Plots |
|----------|-------|-----------|---------------------|--------------|
| 1 | 1 | 2 | 2 | 0 |
| 2 | 2 | 4 | 4 | 0 |
| 3 | 3 | 8 | 8 | 0 |
| 4 | 4 | 16 | 16 | 0 |
| 8 | 8 | 24 | 24 | 1 |
| 16 | 16 | 30 | 30 | 1 |
| 20 | 20 | 38 | 38 | 1 |
| 24 | 24 | 44 | 44 | 1 |
| 30 | 30 | 50 | 50 | 2 |
| 36 | 36 | 56 | 56 | 2 |

> Server admins can customize levels in `city_level_info.json`.

## City Management

### Viewing City Info
```
/city info <cityName>    — Info about a specific city
/city here               — Info about the city on your current plot
/city list               — List all cities on the server
```

### City Settings

| Command | Description |
|---------|-------------|
| `/city set name <newName>` | Rename your city (costs 20 coins) |
| `/city set open on/off` | Allow anyone to join (open) or require invitations (closed) |
| `/city set mayor <player>` | Transfer mayor role to another citizen |
| `/city set fee <amount>` | Set daily fee for citizens (max 50) |
| `/city set pvp on/off` | Enable/disable PvP on all city plots |
| `/city set fire on/off` | Enable/disable fire spread on city plots |
| `/city set blast on/off` | Enable/disable explosion damage on city plots |
| `/city set color <color>` | Set city display color |
| `/city set title <player> <title>` | Set a custom title for a citizen |
| `/city set invmsg <message>` | Set city invitation message (max 100 chars) |
| `/city set permissions <group> <perm> <on/off>` | Set default plot permissions |

### Joining and Leaving

**Open cities:**
```
/city join
```

**Closed cities (invitation required):**
```
/city invite <player>     — Mayor/authorized citizen sends invite
/accept <cityName>        — Player accepts
/deny <cityName>          — Player denies
```

**Leaving a city:**
```
/city leave
```

**Kicking a citizen:**
```
/city kick <player>
```

## Extra Plots and Outposts

### Extra Plots
Buy additional plots beyond your city level limit:
```
/city extraplot
```
Cost: **30** coins per extra plot. Limited by your city level.

### Outposts
Claim plots at a distance from your city:
```
/city outpost
```
Cost: **150** coins. Distance range: 0–1250 blocks from city.

## Summon Points

Cities with summon plots can set up teleportation points:

```
/city summon set point            — Set summon at current location
/city summon set name <name>      — Name the summon point
/city summon use <name>           — Teleport to a summon point
/city summon list                 — List all summon points
```

Cost per summon: **5** coins. Cooldown: 10 seconds.

## Plot Groups

Organize plots into groups with shared members and permissions:

```
/city plotsgroup create <name>    — Create a group (alias: /city pg create)
/city pg delete <name>            — Delete a group
/city pg plotadd <name>           — Add current plot to group
/city pg plotremove <name>        — Remove current plot from group
/city pg add <name> <player>      — Add player to group
/city pg kick <name> <player>     — Remove player from group
/city pg list                     — List all groups
/city pg listplayers <name>       — List group members
/city pg set pvp on/off           — Set group PvP state
/city pg set fire on/off          — Set group fire spread
/city pg set blast on/off         — Set group blast state
/city pg set permissions ...      — Set group permissions
```

Max groups per city: **5** (configurable).

## Daily Maintenance

Each mod day, cities pay maintenance from their treasury:
- **Base care:** 2 coins
- **Plot costs:** Varies by plot type (see [Economy](Economy))
- **No-PVP plot surcharge:** 3 coins per protected plot (if enabled)
- **City level payment:** Based on level config

If a city cannot pay, it accumulates debt up to **1000** coins. Depending on server config, citizens may be removed or the city deleted when debt is too high.
