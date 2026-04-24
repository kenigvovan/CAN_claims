# Commands Reference

Complete list of all commands in CAN Claims. Commands use `/` prefix.

## Chat Commands

| Command | Description |
|---------|-------------|
| `/gc` | Switch to Global chat |
| `/cc` | Switch to City chat |
| `/lc` | Switch to Local chat (100 block range) |

## Citizen Commands (`/citizen` or `/ci`)

| Command | Arguments | Description |
|---------|-----------|-------------|
| `/ci info` | `<player>` | View player information |
| `/ci prices` | — | List all costs and prices |
| `/ci nextdaytimer` | — | Time until next mod day (alias: `ndt`) |
| `/ci invitelist` | — | List your pending city invitations |
| `/ci friend info` | — | Show your friends list |
| `/ci friend add` | `<player>` | Add a friend |
| `/ci friend remove` | `<player>` | Remove a friend |

## Agreement Commands

| Command | Arguments | Description |
|---------|-----------|-------------|
| `/agree` | — | Accept all pending agreements |
| `/accept` | `[cityName]` | Accept a city invitation |
| `/deny` | `[cityName]` | Deny a city invitation |
| `/plotsgroupaccept` | `<city> <group>` | Accept plots group invitation |
| `/plotsgroupleave` | `<city> <group>` | Leave a plots group |

## City Commands (`/city` or `/c`)

### General

| Command | Arguments | Description |
|---------|-----------|-------------|
| `/c create` | `<name>` | Create a new city (alias: `new`) |
| `/c delete` | — | Delete your city |
| `/c list` | — | List all cities |
| `/c here` | — | City info at current plot |
| `/c info` | `<city>` | Detailed city info |
| `/c join` | — | Join an open city |
| `/c leave` | — | Leave your city |
| `/c claim` | — | Claim current plot for city |
| `/c unclaim` | — | Unclaim current city plot |
| `/c extraplot` | — | Buy an extra plot |
| `/c outpost` | — | Buy an outpost plot |
| `/c invite` | `<player>` | Invite player to city |
| `/c uninvite` | `<player>` | Cancel invitation |
| `/c kick` | `<player>` | Kick player from city |
| `/c inviteaccept` | `<alliance>` | Accept alliance invitation |
| `/c invitesent` | `[page]` | List sent invitations |

### City Settings (`/c set`)

| Command | Arguments | Description |
|---------|-----------|-------------|
| `/c set name` | `<name>` | Rename city |
| `/c set open` | `on/off` | Toggle open/closed |
| `/c set mayor` | `<player>` | Transfer mayor role |
| `/c set fee` | `<amount>` | Set citizen fee |
| `/c set pvp` | `on/off` | Toggle PvP |
| `/c set fire` | `on/off` | Toggle fire spread |
| `/c set blast` | `on/off` | Toggle explosions |
| `/c set color` | `<color>` | Set city color |
| `/c set colorint` | `<int>` | Set city color (integer) |
| `/c set title` | `<player> <title>` | Set citizen title |
| `/c set invmsg` | `[message]` | Set invitation message |
| `/c set permissions` | `<group> <perm> <on/off>` | Set default permissions (alias: `p`) |
| `/c set info` | — | View city settings |

### Ranks (`/c rank`)

| Command | Arguments | Description |
|---------|-----------|-------------|
| `/c rank create` | `<name>` | Create custom rank |
| `/c rank delete` | `<name>` | Delete rank |
| `/c rank add` | `<rank> <player>` | Assign rank to player |
| `/c rank remove` | `[rank] [player]` | Remove rank from player |
| `/c rank addperm` | `<rank> <perms>` | Add permission to rank |
| `/c rank removeperm` | `<rank> <perms>` | Remove permission from rank |
| `/c rank list` | `[player]` | List ranks |

### Prison (`/c prison`)

| Command | Arguments | Description |
|---------|-----------|-------------|
| `/c prison list` | — | List prison cells |
| `/c prison addcell` | `<x> <y> <z>` | Add a prison cell |
| `/c prison removecell` | `<number>` | Remove cell by number |
| `/c prison cremovecell` | `<x> <y> <z>` | Remove cell by coordinates |

### Criminals (`/c criminal`)

| Command | Arguments | Description |
|---------|-----------|-------------|
| `/c criminal list` | — | List criminals |
| `/c criminal add` | `<player>` | Mark as criminal |
| `/c criminal remove` | `<player>` | Remove criminal status |

### Summon (`/c summon`)

| Command | Arguments | Description |
|---------|-----------|-------------|
| `/c summon list` | — | List summon points |
| `/c summon set point` | — | Set summon at current location |
| `/c summon set name` | `<name>` | Name the summon point |
| `/c summon set cname` | `[x y z] <name>` | Name summon by coordinates |
| `/c summon use` | `<name>` | Teleport to summon point |

### Plot Groups (`/c plotsgroup` or `/c pg`)

| Command | Arguments | Description |
|---------|-----------|-------------|
| `/c pg create` | `<name>` | Create plot group |
| `/c pg delete` | `<name>` | Delete plot group |
| `/c pg list` | — | List plot groups |
| `/c pg listplayers` | `<name>` | List group members |
| `/c pg add` | `<group> <player>` | Add player to group |
| `/c pg unadd` | `<group> <player>` | Remove player from group |
| `/c pg kick` | `<group> <player>` | Kick player from group |
| `/c pg plotadd` | `<group>` | Add current plot to group |
| `/c pg plotremove` | `<group>` | Remove current plot from group |
| `/c pg set pvp` | `on/off` | Set group PvP |
| `/c pg set fire` | `on/off` | Set group fire spread |
| `/c pg set blast` | `on/off` | Set group blast |
| `/c pg set permissions` | `...` | Set group permissions (alias: `p`) |

### War (`/c war`)

| Command | Arguments | Description |
|---------|-----------|-------------|
| `/c war declare` | `<target>` | Declare war (alias: `d`) |
| `/c war revoke` | `<target>` | Revoke declaration (alias: `r`) |
| `/c war accept` | `<target>` | Accept war (alias: `a`) |
| `/c war deny` | `<target>` | Deny war |
| `/c war offerstop` | `<target>` | Offer peace |
| `/c war acceptstop` | `<target>` | Accept peace |
| `/c war denystop` | `<target>` | Deny peace offer |

## Plot Commands (`/plot`)

| Command | Arguments | Description |
|---------|-----------|-------------|
| `/plot here` | — | View current plot info |
| `/plot borders` | `on/off` | Toggle plot border display |
| `/plot claim` | — | Claim plot (personal) |
| `/plot unclaim` | — | Unclaim plot |
| `/plot fs` | `[price]` | Set plot for sale (alias: `forsale`) |
| `/plot nfs` | — | Remove from sale (alias: `notforsale`) |
| `/plot innerclaim` | — | Manage inner claims (taverns) |
| `/plot plotmsgs` | `0-3` | Message display mode |
| `/plot set permissions` | `<group> <perm> <on/off>` | Set permissions (alias: `p`) |
| `/plot set pvp` | `on/off` | Toggle PvP |
| `/plot set fire` | `on/off` | Toggle fire spread |
| `/plot set blast` | `on/off` | Toggle explosions |
| `/plot set name` | `<name>` | Set plot name |
| `/plot set fee` | `<amount>` | Set plot fee |
| `/plot set type` | `<type>` | Set plot type |

**Plot types:** `default`, `farm`, `temple`, `summon`, `embassy`, `tavern`, `tournament`, `camp`, `prison`

## Alliance Commands (`/alliance` or `/a`)

### General

| Command | Arguments | Description |
|---------|-----------|-------------|
| `/a create` | `<name>` | Create alliance (alias: `new`) |
| `/a delete` | — | Delete alliance (alias: `remove`) |
| `/a leave` | — | Leave alliance (alias: `l`) |
| `/a kick` | `<city>` | Kick city from alliance |
| `/a invite` | `<city>` | Invite city |
| `/a listinvites` | — | List pending invitations |

### Alliance Settings (`/a set`)

| Command | Arguments | Description |
|---------|-----------|-------------|
| `/a set name` | `<name>` | Rename alliance |
| `/a set fee` | `<amount>` | Set alliance fee |
| `/a set capital` | `<city>` | Change capital city |
| `/a set prefix` | `<prefix>` | Set chat prefix (max 3 chars) |

### Conflicts (`/a conflict`)

| Command | Arguments | Description |
|---------|-----------|-------------|
| `/a conflict declare` | `<alliance>` | Declare conflict (alias: `d`) |
| `/a conflict revoke` | `<alliance>` | Revoke conflict (alias: `r`) |
| `/a conflict accept` | `<alliance>` | Accept conflict (alias: `a`) |
| `/a conflict deny` | `<alliance>` | Deny conflict |
| `/a conflict offerstop` | `<alliance>` | Offer peace |
| `/a conflict acceptstop` | `<alliance>` | Accept peace |
| `/a conflict denystop` | `<alliance>` | Deny peace |

### Unions (`/a union`)

| Command | Arguments | Description |
|---------|-----------|-------------|
| `/a union declare` | `<alliance>` | Propose union (alias: `d`) |
| `/a union accept` | `<alliance>` | Accept union (alias: `ac`) |
| `/a union decline` | `<alliance>` | Decline union (alias: `dn`) |
| `/a union revoke` | `<alliance>` | Revoke union (alias: `r`) |

## Map Command

| Command | Description |
|---------|-------------|
| `/cmap` | Open city map display |
