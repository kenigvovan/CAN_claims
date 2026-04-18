# War & Conflicts

CAN Claims features a structured warfare system that allows cities and alliances to engage in territorial battles with scheduled combat windows and capture mechanics.

## Overview

Wars follow a formal process:
1. **Declaration** — One side proposes war
2. **Acceptance** — The other side agrees (if required by config)
3. **Scheduling** — Battle times are set
4. **Battle** — Combat occurs during scheduled windows
5. **Resolution** — Territory changes hands, peace is negotiated

## City Wars

Cities without an alliance can wage war independently:

### Declaring War
```
/city war declare <cityOrAllianceName>
```

### Responding to War Declarations
```
/city war accept <cityOrAllianceName>     — Accept the declaration
/city war deny <cityOrAllianceName>       — Deny the declaration
```

### Revoking a Declaration
```
/city war revoke <cityOrAllianceName>
```

### Peace Negotiations
```
/city war offerstop <cityOrAllianceName>   — Offer to end the war
/city war acceptstop <cityOrAllianceName>  — Accept peace
/city war denystop <cityOrAllianceName>    — Deny peace offer
```

## Alliance Conflicts

Alliances can declare large-scale conflicts:

```
/alliance conflict declare <allianceName>    — Declare conflict
/alliance conflict accept <allianceName>     — Accept conflict
/alliance conflict deny <allianceName>       — Deny conflict
/alliance conflict revoke <allianceName>     — Revoke declaration
/alliance conflict offerstop <allianceName>  — Offer peace
/alliance conflict acceptstop <allianceName> — Accept peace
/alliance conflict denystop <allianceName>   — Deny peace
```

## Conflict States

| State | Description |
|-------|-------------|
| **Proposal** | War has been declared, awaiting response |
| **Created** | Both sides agreed, waiting for battle to start |
| **Active** | Battle is in progress |
| **First Won** | First party has won |
| **Second Won** | Second party has won |

## Battle Scheduling

Wars don't happen immediately — battles are scheduled during specific time windows:

- **War ranges** define when battles can occur (day of week + time window)
- Each alliance can have up to **2** active war ranges (`WARRANGE_PER_ALLIANCE`)
- Minimum battle duration: **60 minutes** (`MIN_WARRANGE_DURATION_MINUTES`)
- Minimum days between battles: **3** (`MINIMUM_DAYS_BETWEEN_BATTLES`)
- Activation delay: **300 seconds** after conflict is accepted (`DELAY_FOR_CONFLICT_ACTIVATED`)

## Capture Flags

During active battles, attackers use **Capture Flag** blocks to seize enemy plots:

### Mechanics
1. Place a Capture Flag block on an enemy plot during a battle window
2. Defend the flag for **60 seconds** (`FLAG_CAPTURE_DURATION_SECONDS`)
3. The flag has **10** reinforcement points (`FLAG_REINFORCEMENT_AMOUNT`) — defenders must destroy it
4. Maximum **2** capture flags can be active at once (`MAX_AMOUNT_OF_CAPTURE_FLAGS_ACTIVE`)
5. If the flag survives, the plot is captured

### Notifications
When plots are under attack or captured, announcements are sent to chat (configurable):
- Plot under attack alerts (with optional coordinates)
- Plot captured alerts (with optional coordinates)

## PvP During War

- Players from warring factions can attack each other on contested territory
- PvP is automatically enabled between enemies during active battles
- **Tournament plots** always have PvP enabled regardless of war status
- Criminals can be attacked by city citizens at any time

## Time-Based PvP

The server can restrict PvP to certain hours (independent of wars):
- **PvP window:** 19:00–6:00 by default
- Configurable via `PVP_TIME_START` and `PVP_TIME_END`
- Toggle: `PVP_DURING_PART_OF_THE_DAY`

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| `NEED_AGREE_FOR_CONFLICT` | true | Require both sides to agree |
| `DELAY_FOR_CONFLICT_ACTIVATED` | 300 | Seconds before conflict goes active |
| `MIN_WARRANGE_DURATION_MINUTES` | 60 | Minimum battle window length |
| `WARRANGE_PER_ALLIANCE` | 2 | Max war ranges per alliance |
| `MINIMUM_DAYS_BETWEEN_BATTLES` | 3 | Cooldown between battles |
| `MAX_AMOUNT_OF_CAPTURE_FLAGS_ACTIVE` | 2 | Max simultaneous capture flags |
| `FLAG_REINFORCEMENT_AMOUNT` | 10 | Flag hit points |
| `FLAG_CAPTURE_DURATION_SECONDS` | 60 | Time to capture a plot |
| `CAPTURED_PLOTS_DO_NOT_BLOCK_CLAIMS` | true | Captured plots don't prevent new claims |
