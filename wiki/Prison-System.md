# Prison System

Cities can build prisons to hold captured enemy players, creating a ransom-based mechanic for conflict resolution.

## Setting Up a Prison

1. Claim a plot and set its type to **Prison**:
   ```
   /plot set type prison
   ```

2. Add prison cells (specify coordinates within the plot):
   ```
   /city prison addcell <x> <y> <z>
   ```

3. Each prison plot can hold up to **4 cells** (`MAX_CELLS_PER_PRISON`)

## Managing Prison Cells

```
/city prison list                    — List all prison cells
/city prison addcell <x> <y> <z>    — Add a cell at coordinates
/city prison removecell <number>     — Remove cell by number
/city prison cremovecell <x> <y> <z> — Remove cell by coordinates
```

## How Imprisonment Works

When a player is killed by a city citizen on a city plot, they may be imprisoned:
- The defeated player's respawn point is set to a random prison cell
- Imprisoned players have restricted command access
- The **summon** command is blocked while in prison (`BLOCKED_COMMANDS_PRISON`)

## Criminal System

Cities can mark players as criminals, allowing citizens to attack them:

```
/city criminal add <player>     — Mark player as criminal
/city criminal remove <player>  — Remove criminal status
/city criminal list             — List all marked criminals
```

Criminals can be attacked by city citizens even outside of war, including on plots where PvP is normally disabled.

## Ransom Costs

Ransoming a prisoner costs different amounts based on their role:

| Role | Ransom Cost |
|------|-------------|
| No city affiliation | 2 |
| City citizen | 10 |
| Chief | 5 |
| City mayor | 20 |
| Alliance leader | 30 |

## Temple Respawn

Cities with **Temple** plots can set up respawn points using special Temple blocks. When a city member dies, they respawn at their city's temple rather than the world spawn.

## Permissions

Prison-related permissions for city ranks:
- `CITY_PRISON_ALL` — Full prison management
- `CITY_CRIMINAL_ALL` — Full criminal list management
- `CITY_ADD_CRIMINAL` — Mark players as criminals
- `CITY_REMOVE_CRIMINAL` — Remove criminal marks
- `CITY_PRISON_ADD_CELL` — Add prison cells
- `CITY_PRISON_REMOVE_CELL` — Remove prison cells
- `CITY_PRISON_LIST` — View prison info
