# Alliances

Alliances allow multiple cities to band together for mutual benefits, shared permissions, and collective warfare.

## Creating an Alliance

```
/alliance create <allianceName>
```

**Requirements:**
- Your city must not already be in an alliance
- You must be the city mayor
- Cost: **300** coins (`NEW_ALLIANCE_COST`)

The creating city becomes the **capital** of the alliance, and its mayor becomes the **alliance leader**.

## Alliance Levels

Alliances level up based on the number of member cities:

| Member Cities | Level | Additional Plots per City | Max Camps | Daily Payment |
|---------------|-------|--------------------------|-----------|---------------|
| 1 | 1 | 12 | 1 | 4 |
| 2 | 2 | 24 | 1 | 5 |

> Server admins can customize levels in `alliance_level_info.json`.

## Managing an Alliance

### Inviting Cities
```
/alliance invite <cityName>
```

The invited city's mayor accepts with:
```
/city inviteaccept <allianceName>
```

Invitations expire after **2 hours** (`HOUR_TIMEOUT_INVITATION_TO_ALLIANCE`).

### Other Management Commands
```
/alliance kick <cityName>       — Remove a city from the alliance
/alliance leave                 — Leave the alliance (as a city)
/alliance delete                — Dissolve the alliance (leader only)
/alliance listinvites           — View pending invitations
```

### Alliance Settings
```
/alliance set name <newName>    — Rename (costs 50 coins)
/alliance set fee <amount>      — Set daily fee for member cities (max 50)
/alliance set capital <city>    — Change the capital city
/alliance set prefix <prefix>   — Set a chat prefix (max 3 characters)
```

## Alliance Benefits

- **Shared plot access** — Alliance members can be granted permissions on allied city plots via the "ally" permission group
- **Additional plots** — Each city in the alliance gains extra claimable plots based on alliance level
- **Camp plots** — Alliance unlocks camp plot types for member cities
- **Collective warfare** — Alliances can declare conflicts against other alliances
- **Chat prefix** — Alliance prefix shown in chat messages

## Unions (Diplomacy)

Alliances can form diplomatic unions with other alliances:

```
/alliance union declare <allianceName>     — Propose a union
/alliance union accept <allianceName>      — Accept a union proposal
/alliance union decline <allianceName>     — Decline a union proposal
/alliance union revoke <allianceName>      — Revoke a union
```

Unions establish comrade status between alliances, affecting plot permissions for the "comrade" group.

## Alliance Conflicts

See [War & Conflicts](War-and-Conflicts) for details on alliance-level warfare.

## Daily Maintenance

Alliances pay daily maintenance from their treasury:
- **Base care:** 50 coins (`ALLIANCE_BASE_CARE`)
- **Level-based payment:** Varies by alliance level
