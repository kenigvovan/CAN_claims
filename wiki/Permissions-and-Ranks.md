# Permissions & Ranks

CAN Claims has a detailed permission system for controlling what players can do within cities and on plots.

## Plot Access Permissions

Every plot has three access groups, each with three permission types:

### Groups
| Group | Who |
|-------|-----|
| **Citizen** | Members of the city that owns the plot |
| **Stranger** | Players not in the city |
| **Ally** | Players from allied cities (through alliance) |
| **Comrade** | Players marked as friends |

### Permission Types
| Permission | Description |
|------------|-------------|
| **Use** | Interact with containers, doors, mechanisms |
| **Build** | Place and break blocks |
| **Attack** | Attack animals on the plot |

### Setting Plot Permissions
```
/plot set p <group> <use|build|attack> <on|off>
```

### Setting City-Wide Default Permissions
```
/city set p <group> <use|build|attack> <on|off>
```

## City Roles

### Built-in Roles

**Default** (all players):
- `PLOT_CLAIM`, `PLOT_UNCLAIM`
- `CITY_HERE`, `CITY_INFO`

**Mayor** (city creator/designated mayor):
- All city permissions (full list below)

**Leader** (alliance leader — mayor of capital city):
- All alliance-specific permissions

### Custom Ranks

Cities can create custom ranks with selected permissions:

```
/city rank create <rankName>                      — Create a new rank
/city rank delete <rankName>                      — Delete a rank
/city rank addperm <rankName> <permission>        — Add permission to rank
/city rank removeperm <rankName> <permission>     — Remove permission from rank
/city rank add <rankName> <playerName>            — Assign rank to player
/city rank remove <rankName> <playerName>         — Remove rank from player
/city rank list [playerName]                      — List ranks (or player's ranks)
```

## Full Permission List

### Plot Actions (Personal)
These permissions control what a player can do to plots in general and to their own plots. Most are granted automatically to citizens of the owning city.

| Permission | Description |
|------------|-------------|
| `PLOT_CLAIM` | Claim a personal plot |
| `PLOT_UNCLAIM` | Unclaim a personal plot |
| `PLOT_SET_FS` | Mark a plot for sale |
| `PLOT_SET_NFS` | Remove a plot from sale |
| `PLOT_SET_ALL_OWN_PLOT` | All modify permissions on plots you own |
| `PLOT_SET_ALL_CITY_PLOTS` | All modify permissions on every city plot |
| `PLOT_SET_NAME` | Rename a plot |
| `PLOT_SET_FEE` | Set a plot fee |
| `PLOT_SET_TYPE` | Change a plot's type |
| `PLOT_SET_PVP` | Toggle plot PvP |
| `PLOT_SET_FIRE` | Toggle plot fire spread |
| `PLOT_SET_BLAST` | Toggle plot explosion damage |
| `PLOT_SET_PLOT_ACCESS_PERMISSIONS` | Set a plot's access permissions |
| `PLOT_INNER_PLOT` | Manage inner claims (tavern sub-areas) |

### Player Info

| Permission | Description |
|------------|-------------|
| `PLAYER_INFO_OTHER` | View other players' info via `/citizen info` |

### City Management
| Permission | Description |
|------------|-------------|
| `CITY_CLAIM_PLOT` | Claim plots for the city |
| `CITY_UNCLAIM_PLOT` | Unclaim city plots |
| `CITY_BUY_EXTRA_PLOT` | Purchase extra plots |
| `CITY_BUY_OUTPOST` | Establish outposts |
| `CITY_INVITE` | Invite players to the city |
| `CITY_KICK` | Remove players from the city |
| `CITY_UNINVITE` | Cancel invitations |
| `SHOW_INVITES_SENT` | View sent invitations |

### City Settings
| Permission | Description |
|------------|-------------|
| `CITY_SET_ALL` | Full settings access |
| `CITY_SET_NAME` | Rename the city |
| `CITY_SET_OPEN_STATE` | Toggle open/closed |
| `CITY_SET_PVP` | Toggle city PvP |
| `CITY_SET_FIRE` | Toggle fire spread |
| `CITY_SET_BLAST` | Toggle explosion damage |
| `CITY_SET_GLOBAL_FEE` | Set citizen fees |
| `CITY_SET_DAILY_MSG` | Set daily message |
| `CITY_SET_PLOT_ACCESS_PERMISSIONS` | Set plot permissions |
| `CITY_SET_INV_MSG` | Set invitation message |
| `CITY_SET_PLOTS_COLOR` | Change city plots color |
| `CITY_SET_SUMMON` | Manage summon points |
| `CITY_SET_OTHERS_PREFIX` | Set other players' prefixes |

### City Information
| Permission | Description |
|------------|-------------|
| `CITY_INFO` | View detailed city info |
| `CITY_HERE` | View city at current location |
| `CITY_SEE_BALANCE` | View city treasury |

### Rank Management
| Permission | Description |
|------------|-------------|
| `CITY_CREATE_CITY_RANK` | Create custom ranks |
| `CITY_DELETE_CITY_RANK` | Delete custom ranks |
| `CITY_SEE_CITY_RANKS` | View rank list |
| `CITY_ADD_PERMISSION_TO_RANK` | Add permissions to ranks |
| `CITY_REMOVE_PERMISSION_FROM_RANK` | Remove permissions from ranks |
| `CITY_SHOW_RANK_OTHERS` | View other players' ranks |
| `CITY_SET_RANK` | Assign ranks to players |
| `CITY_REMOVE_RANK` | Remove ranks from players |

### Prison & Criminal
| Permission | Description |
|------------|-------------|
| `CITY_PRISON_ALL` | Full prison management |
| `CITY_CRIMINAL_ALL` | Full criminal management |
| `CITY_ADD_CRIMINAL` | Mark criminals |
| `CITY_REMOVE_CRIMINAL` | Remove criminal marks |
| `CITY_PRISON_ADD_CELL` | Add prison cells |
| `CITY_PRISON_REMOVE_CELL` | Remove prison cells |
| `CITY_PRISON_LIST` | View prison info |

### Plot Groups
| Permission | Description |
|------------|-------------|
| `CITY_PLOTSGROUP_CREATE` | Create plot groups |
| `CITY_PLOTSGROUP_REMOVE` | Delete plot groups |
| `CITY_PLOTSGROUP_ADD_PLAYER` | Add players to groups |
| `CITY_PLOTSGROUP_KICK_PLAYER` | Remove players from groups |
| `CITY_PLOTSGROUP_ADD_PLOT` | Add plots to groups |
| `CITY_PLOTSGROUP_REMOVE_PLOT` | Remove plots from groups |
| `CITY_PLOTSGROUP_LIST` | View group list |
| `CITY_PLOTSGROUP_SET` | Modify group settings |
| `CITY_PLOTSGROUP_SET_PVP` | Set group PvP |
| `CITY_PLOTSGROUP_SET_FIRE` | Set group fire spread |
| `CITY_PLOTSGROUP_SET_BLAST` | Set group blast |

### Finance
| Permission | Description |
|------------|-------------|
| `CITY_WITHDRAW_MONEY` | Withdraw from city treasury |

### Alliance Permissions (Leader only)
| Permission | Description |
|------------|-------------|
| `ALLIANCE_DECLARE_CONFLICT` | Declare conflicts |
| `ALLIANCE_REVOKE_CONFLICT` | Revoke conflict declarations |
| `ALLIANCE_ACCEPT_CONFLICT` | Accept conflicts |
| `ALLIANCE_DENY_CONFLICT` | Deny conflicts |
| `ALLIANCE_OFFER_STOP_CONFLICT` | Propose peace |
| `ALLIANCE_ACCEPT_STOP_CONFLICT` | Accept peace |
| `ALLIANCE_DENY_STOP_CONFLICT` | Deny peace |
| `ALLIANCE_WITHDRAW_MONEY` | Withdraw from alliance treasury |
| `ALLIANCE_DECLARE_UNION` | Propose unions |
| `ALLIANCE_REVOKE_UNION` | Revoke unions |
| `ALLIANCE_ACCEPT_UNION` | Accept unions |
| `ALLIANCE_DENY_UNION` | Deny unions |
