# Economy

CAN Claims includes a comprehensive economic system with city treasuries, daily maintenance, fees, and integration with economy mods.

## Currency

The mod integrates with external economy mods via the `SELECTED_ECONOMY_HANDLER` configuration option. Currency values are mapped through `COINS_VALUES_TO_CODE` and `ID_TO_COINS_VALUES` settings.

## City Treasury

Each city has its own bank account (prefixed with `#city_`). The treasury is used for:
- Paying daily maintenance
- Purchasing plots, outposts, and extra plots
- Summon costs
- City renaming

### City Banks
City banks are created by placing a chest with a sign:
- Only the mayor can create city banks
- The sign text must match the city name
- Banks can be relocated (the old one is destroyed)

## Alliance Treasury

Alliances also have their own accounts (prefixed with `#alliance_`), used for alliance maintenance and fees.

## Cost Reference

### One-Time Costs

| Action | Cost |
|--------|------|
| Create a city | 150 |
| Rename a city | 20 |
| Create an alliance | 300 |
| Rename an alliance | 50 |
| Buy extra plot | 30 |
| Buy outpost | 150 |
| Use summon | 5 |

### Daily Plot Maintenance

| Plot Type | Daily Cost |
|-----------|-----------|
| Default | 1 |
| Main City | 3 |
| Tournament | 3 |
| Prison | 3 |
| Camp | 4 |
| Temple | 5 |
| Farm | 6 |
| Summon | 7 |
| Embassy | 8 |
| Tavern | 9 |

### Daily City Costs

| Cost | Default |
|------|---------|
| Base care | 2 |
| No-PvP plot surcharge | 3 per plot |
| Max city fee | 50 |

### Daily Alliance Costs

| Cost | Default |
|------|---------|
| Base care | 50 |
| Level-based payment | Varies |
| Max alliance fee | 50 |

## Fees

### City Fees
Mayors can set a daily fee for citizens:
```
/city set fee <amount>
```
Max fee: **50** coins. Citizens who cannot pay may be removed from the city (configurable).

### Plot Fees
Plot owners can set individual fees:
```
/plot set fee <amount>
```

### Alliance Fees
Alliance leaders can charge member cities:
```
/alliance set fee <amount>
```

## Debt System

- Cities accumulate debt when they can't pay maintenance
- Max city debt: **1000** coins (`CITY_MAX_DEBT`)
- When max debt is reached:
  - Citizens may be removed if `DELETE_CITIZEN_FROM_CITY_IF_DOESN_PAY_FEE` is true
  - City may be deleted if `DELETE_CITY_IF_DOESN_PAY_FEE` is true (disabled by default)

## Permissions

Economy-related city permissions:
- `CITY_SEE_BALANCE` — View city treasury balance
- `CITY_WITHDRAW_MONEY` — Withdraw from city treasury
- `ALLIANCE_WITHDRAW_MONEY` — Withdraw from alliance treasury (leader only)
