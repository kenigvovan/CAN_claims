# Configuration

All server settings are stored in `claims.json`, automatically generated in the mod config directory on first launch.

## Economy

| Setting | Default | Description |
|---------|---------|-------------|
| `NEW_CITY_COST` | 150 | Cost to create a city |
| `CITY_NAME_CHANGE_COST` | 20 | Cost to rename a city |
| `CITY_BASE_CARE` | 2 | Daily city maintenance |
| `CITY_MAX_DEBT` | 1000 | Max debt before consequences |
| `PLOT_CLAIM_PRICE` | 5 | Cost to claim a plot |
| `MAX_CITY_FEE` | 50 | Maximum citizen fee |
| `SUMMON_PAYMENT` | 5 | Cost per summon use |
| `NEW_ALLIANCE_COST` | 300 | Cost to create an alliance |
| `ALLIANCE_RENAME_COST` | 50 | Cost to rename an alliance |
| `ALLIANCE_MAX_FEE` | 50 | Maximum alliance fee |
| `ALLIANCE_BASE_CARE` | 50 | Daily alliance maintenance |
| `NEUTRAL_ALLANCE_PAYMENT` | 50 | Neutral alliance payment |
| `EXTRA_PLOT_COST` | 30 | Cost for extra plots |
| `DELETE_CITIZEN_FROM_CITY_IF_DOESN_PAY_FEE` | true | Remove citizens who can't pay |
| `DELETE_CITY_IF_DOESN_PAY_FEE` | false | Delete city if can't pay |
| `ADDITIONAL_COST_OF_NO_PVP_PLOT` | true | Charge extra for no-PvP plots |

## Plot Costs (Daily Maintenance)

| Setting | Default | Description |
|---------|---------|-------------|
| `DEFAULT_PLOT_COST` | 1 | Default plot |
| `MAIN_CITYPLOT_COST` | 3 | Main city plot |
| `TOURNAMENT_PLOT_COST` | 3 | Tournament plot |
| `PRISON_PLOT_COST` | 3 | Prison plot |
| `CAMP_PLOT_COST` | 4 | Camp plot |
| `TEMPLE_PLOT_COST` | 5 | Temple plot |
| `FARM_PLOT_COST` | 6 | Farm plot |
| `SUMMON_PLOT_COST` | 7 | Summon plot |
| `EMBASSY_PLOT_COST` | 8 | Embassy plot |
| `TAVERN_PLOT_COST` | 9 | Tavern plot |
| `OUTPOST_PLOT_COST` | 150 | Outpost (one-time) |
| `PLOT_NO_PVP_FLAG_COST` | 3 | No-PvP flag (daily) |

## Chat

| Setting | Default | Description |
|---------|---------|-------------|
| `USE_MOD_CHAT_WINDOW` | true | Enable custom chat window |
| `CHAT_WINDOW_NAME` | "claims" | Chat window name |
| `PREFIX_COLOR_PLAYER` | #00FFFF | Player prefix color |
| `NAME_COLOR_PLAYER` | #FFFFFF | Player name color |
| `POSTFIX_COLOR_PLAYER` | #1F920E | Player postfix color |
| `CITY_COLOR_NAME` | #755985 | City name color |
| `ALLIANCE_COLOR_NAME` | #218fdc | Alliance name color |
| `MAX_CITIZEN_TITLE_LENGTH` | 16 | Max title length |
| `LOCAL_CHAT_DISTANCE` | 100 | Local chat range (blocks) |
| `ALLIANCE_PREFIX_LENGTH` | 3 | Alliance prefix length |
| `SHOW_CITY_NAME_IN_CHAT` | true | Show city name in chat |
| `SHOW_ALLIANCE_PREFIX_IN_CHAT` | true | Show alliance prefix |

## War & Conflicts

| Setting | Default | Description |
|---------|---------|-------------|
| `NEED_AGREE_FOR_CONFLICT` | true | Require agreement for conflict |
| `DELAY_FOR_CONFLICT_ACTIVATED` | 300 | Activation delay (seconds) |
| `NEED_AGREE_FOR_WAR_RANGES` | true | Require agreement for war ranges |
| `MIN_WARRANGE_DURATION_MINUTES` | 60 | Minimum battle duration |
| `WARRANGE_PER_ALLIANCE` | 2 | War ranges per alliance |
| `MIN_RANGE_CELL_DURATION_MINUTES` | 30 | Minimum range cell duration |
| `MINIMUM_DAYS_BETWEEN_BATTLES` | 3 | Cooldown between battles |
| `MAX_AMOUNT_OF_CAPTURE_FLAGS_ACTIVE` | 2 | Max active capture flags |
| `FLAG_REINFORCEMENT_AMOUNT` | 10 | Capture flag hit points |
| `FLAG_CAPTURE_DURATION_SECONDS` | 60 | Time to capture a plot |

## Time & PvP

| Setting | Default | Description |
|---------|---------|-------------|
| `HOUR_NEW_DAY_START` | 43200 | New day tick count |
| `MOD_DAY_DURATION_IN_SECONDS` | 86400 | Mod day length (seconds) |
| `PVP_DURING_PART_OF_THE_DAY` | true | Enable time-based PvP |
| `PVP_TIME_START` | 19 | PvP window start hour |
| `PVP_TIME_END` | 6 | PvP window end hour |
| `SECONDS_SUMMON_TIME` | 10 | Summon cast time |
| `SECONDS_SUMMON_COOLDOWN` | 10 | Summon cooldown |
| `AGREEMENT_TIMEOUT_SECONDS` | 120 | Agreement timeout |

## Invitations

| Setting | Default | Description |
|---------|---------|-------------|
| `MAX_SENT_INVITATIONS_CITY` | 20 | Max sent city invites |
| `MAX_SENT_INVITATIONS_VILLAGE` | 10 | Max sent village invites |
| `MAX_RECEIVED_INVITATIONS_CITY` | 10 | Max received city invites |
| `MAX_RECEIVED_INVITATIONS_PLAYER` | 10 | Max received invites per player |
| `MAX_SENT_INVITATIONS_ALLIANCE` | 10 | Max sent alliance invites |
| `HOUR_TIMEOUT_INVITATION_TO_ALLIANCE` | 2 | Alliance invite timeout (hours) |
| `HOUR_TIMEOUT_INVITATION_CITY` | 2 | City invite timeout (hours) |

## Prison

| Setting | Default | Description |
|---------|---------|-------------|
| `MAX_CELLS_PER_PRISON` | 4 | Max cells per prison |
| `BLOCKED_COMMANDS_PRISON` | ["summon"] | Commands blocked in prison |
| `RANSOM_FOR_NO_CITIZEN` | 2 | Ransom for non-citizens |
| `RANSOM_FOR_CITIZEN` | 10 | Ransom for citizens |
| `RANSOM_FOR_MAYOR` | 20 | Ransom for mayors |
| `RANSOM_FOR_LEADER` | 30 | Ransom for alliance leaders |
| `RANSOM_FOR_CHIEF` | 5 | Ransom for chiefs |

## Distance & Territory

| Setting | Default | Description |
|---------|---------|-------------|
| `PLOT_SIZE` | 16 | Plot size in blocks |
| `MAP_ZONE_SIZE` | 512 | Map zone size |
| `MIN_DISTANCE_FROM_OTHER_CITY_NEW_CITY` | 3 | Min distance between cities (plots) |
| `MAX_OUTPOST_DISTANCE_FROM_CITY` | 1250 | Max outpost distance (blocks) |
| `MIN_OUTPOST_DISTANCE_FROM_CITY` | 0 | Min outpost distance (blocks) |
| `CAPTURED_PLOTS_DO_NOT_BLOCK_CLAIMS` | true | Captured plots don't block claims |

## Summon

| Setting | Default | Description |
|---------|---------|-------------|
| `SUMMON_ALLOWED` | true | Enable summon feature |
| `SUMMON_MIN_PLAYERS` | 0 | Min players required for summon |
| `SUMMON_HOR_RANGE` | 10 | Horizontal summon range |
| `SUMMON_VER_RANGE` | 10 | Vertical summon range |

## Plot Groups

| Setting | Default | Description |
|---------|---------|-------------|
| `MAX_PLOTS_GROUP_PER_CITY` | 5 | Max plot groups per city |
| `PLOT_GROUP_INVITATION_TIMEOUT` | 2 | Group invite timeout (hours) |

## Display

| Setting | Default | Description |
|---------|---------|-------------|
| `PLOT_BORDERS_COLOR_WILD_PLOT` | [64,255,255,0] | Wild plot border color (RGBA) |
| `PLOT_BORDERS_COLOR_OUR_CITY_PLOT` | [143,5,146,0] | Own city plot color (RGBA) |
| `PLOT_BORDERS_COLOR_OTHER_PLOT` | [16,49,158,0] | Other city plot color (RGBA) |
| `CITY_AREA_VISIBILITY_STATE` | ALL | Map visibility (ALL, WITHOUT_BORDER, WITHOUT_INNER) |
| `GUI_SHOW_DEBT` | true | Show debt in GUI |

## Database & Backups

| Setting | Default | Description |
|---------|---------|-------------|
| `PATH_TO_DB_AND_JSON_FILES` | "" | Custom path (empty = default) |
| `DB_NAME` | "claims.db" | Database file name |
| `MANUALLY_BACKUP_FILE_NAME` | "backup_manually_claims.db" | Manual backup name |
| `DAILY_BACKUP_FILE_NAME` | "backup_daily_claims.db" | Daily backup name |
| `HOURLY_BACKUP_FILE_NAME` | "backup_hourly_claims.db" | Hourly backup name |
| `DAYTIME_MAKE_BACKUP` | ["6:00","12:00","18:00","0:00"] | Backup schedule |
| `BACKUP_CHECK_TIMER_SECONDS` | 10 | Backup check interval |

## Patches

| Setting | Default | Description |
|---------|---------|-------------|
| `FALLING_BLOCKS_TO_CITY_PLOTS_PATCH` | true | Block falling blocks from outside |
| `WATER_FLOW_CITY_PLOTS_PATCH` | true | Block water flow from outside |

## Protection

| Setting | Default | Description |
|---------|---------|-------------|
| `PROTECTED_MOB_TYPES` | See below | Animals protected in claims |

Default protected animals: Bighorn lamb, Bighorn ewe, Bighorn ram, Rooster, Chick, Hen, Sow, Boar, Piglet

## Advanced

| Setting | Default | Description |
|---------|---------|-------------|
| `CLAIM_LIMITERS_ENABLED` | false | Enable claim zone limiters |
| `NEW_CITY_ONLY_BY_ITEM` | false | Require special item for city creation |
| `MAX_NUMBER_TAVERN_PER_CITY` | 3 | Max taverns per city |
| `MAX_NUMBER_INNER_CLAIM_PER_TAVERN` | 3 | Max inner claims per tavern |
| `SELECTED_ECONOMY_HANDLER` | "" | Economy mod integration |
| `ROLE_CODES_WITH_ADMIN_RIGHTS` | ["admin"] | Roles with admin access |
| `VERBOSE_LOGGING` | true | Debug logging |
| `PLAYER_MOVEMENT_CANCEL_TELEPORTATION` | true | Cancel teleport on move |
| `BLOCKED_NAMES` | [] | Reserved city names |

## City Level Configuration

Edit `city_level_info.json` to customize city levels:

```json
{
  "1": {
    "AmountOfPlots": 2,
    "UnconditionalPayment": 0,
    "SummonPlots": 0,
    "Maxextrachunksbought": 2
  },
  "2": {
    "AmountOfPlots": 4,
    "UnconditionalPayment": 0,
    "SummonPlots": 0,
    "Maxextrachunksbought": 4
  }
}
```

## Alliance Level Configuration

Edit `alliance_level_info.json` to customize alliance levels:

```json
{
  "1": {
    "AdditionalAmountOfPlots": 12,
    "MaxCampsAmount": 1,
    "UnconditionalPayment": 4
  },
  "2": {
    "AdditionalAmountOfPlots": 24,
    "MaxCampsAmount": 1,
    "UnconditionalPayment": 5
  }
}
```
