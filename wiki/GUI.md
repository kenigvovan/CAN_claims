# GUI Overview

CAN Claims provides three client-side interfaces: a main tabbed **Pretty GUI** (the primary window used by players), a **Plot Info HUD** that shows the plot you are standing on, and a **legacy** dialog window. The mod also renders claim data onto the in-game world map.

## Hotkeys

| Hotkey | Interface |
|--------|-----------|
| **U** | Pretty GUI (main tabbed window) |
| **Ctrl+Shift+U** | Legacy CAN Claims dialog |
| **K** | Plot Info HUD |

All three hotkeys are registered under `HotkeyType.GUIOrOtherControls` and can be rebound in the standard Vintage Story controls menu.

## Pretty GUI (Main Window)

Opened with **U**. This is the primary interface for managing cities, plots, alliances, prisons, and conflicts. It is an ImGui overlay provided through the VSImGui framework.

Closing:
- Press **U** again
- Press **Escape** while the window is focused

### Main Tab Bar

The top row displays seven icon buttons (hover any icon for a tooltip):

| Icon | Tab | Purpose |
|------|-----|---------|
| Citadel | **CITY** | City management — name, mayor, creation date, plots used/available, claim buttons |
| Magnifying glass | **PLAYER** | Your personal info — friends list, next payment, access to the city and alliance browsers |
| Price tag | **PRICES** | All current costs and prices, rendered as items on a simulated inventory grid |
| Flat platform | **PLOT** | Plot at your current position — owner, name, sale price, permissions, claim/unclaim |
| Prisoner | **PRISON** | Prison management — criminal list, prison cells list, add/remove cells and criminals |
| Magic portal | **SUMMON** | Summon points — list, use, add, remove |
| Huts village | **PLOTSGROUP** | Your plot groups — create, manage, invitations |

### Secondary Tabs (Pages Reached from Main Tabs)

Certain actions open additional tabs in the same window:

| Tab | Reached from | Shows |
|-----|--------------|-------|
| Ranks | CITY → Ranks | List of all city ranks |
| Rank Info | Ranks → [rank] | Permissions and members for a single rank |
| Cities list | PLAYER → Cities | Server-wide list of cities |
| Alliance list | PLAYER → Alliances | Server-wide list of alliances |
| Alliance info | any city | Your alliance — members, leader, capital, fees |
| Plots group info | PLOTSGROUP → [group] | Details of a single plot group |
| Plots group invites | PLOTSGROUP → Invites | Pending invitations to plot groups |
| Conflicts | Alliance | Active wars and conflicts |
| Conflict info | Conflicts → [conflict] | Battle ranges, parties, flags, state |
| Conflict letters | Conflicts | Declarations, peace offers, replies |
| Union letters | Alliance | Diplomatic letters between alliances |
| City plots color | CITY settings | Color picker for your city's plots on the map |

### Popup / Input Dialogs (Secondary Window)

Many actions (renaming, confirming, entering values, picking targets) open a smaller floating window on top of the main one. These windows are **modal-like** — they stay until you confirm or cancel. Common categories:

- **Text input** — name a city, plot, alliance, rank, group, summon point, or criminal
- **Integer / double input** — set plot sale price, fee, or tax rate
- **Yes / No confirmation** — unclaim, kick, delete, leave, accept peace offer
- **Selection pickers** — pick a friend to remove, a criminal to release, a plot to add to a group, a player for a rank
- **Permission editors** — toggle `use` / `build` / `attack` flags for Citizen / Stranger / Ally / Comrade on a plot, city, or plot group
- **Conflict declaration** — pick target, range, and terms when declaring war

These popups are rendered by the same system, so their look and feel match the main window.

## Plot Info HUD

Opened with **K**, and also shown automatically when you cross into a new plot (if enabled).

| Line | Content |
|------|---------|
| 1 | City name (or "Wild Lands" if the plot is unclaimed) |
| 2 | Plot name, if the owner set one |
| 3 | Plot group name, if the plot belongs to a group |
| 4 | Alliance affiliation, if the city is in one |
| 5 | Flags — PvP, use, build, attack |

The HUD automatically closes after **15 seconds** of inactivity. You can configure when it appears with:

```
/plot plotmsgs <0-3>
```

| Mode | Behavior |
|------|----------|
| 0 | Nothing shown on plot change |
| 1 | Chat message only |
| 2 | HUD only |
| 3 | Both chat message and HUD |

## World Map Integration

CAN Claims registers its own map layer called **Plots** (index 2) with the world map. When the layer is enabled, the map colors each chunk according to claim ownership.

Default colors (from `claims.json`, all RGBA):

| Setting | Default | Meaning |
|---------|---------|---------|
| `PLOT_BORDERS_COLOR_WILD_PLOT` | `[64, 255, 255, 0]` | Unclaimed land |
| `PLOT_BORDERS_COLOR_OUR_CITY_PLOT` | `[143, 5, 146, 0]` | Your city's plots |
| `PLOT_BORDERS_COLOR_OTHER_PLOT` | `[16, 49, 158, 0]` | Plots of other cities |

Each city can override its color using `/city set color <color>` or `/city set colorint <int>`, which also affects the color picker in the CITY plots color tab.

The server can choose how plot boundaries are drawn using:

```
CITY_AREA_VISIBILITY_STATE = ALL | WITHOUT_BORDER | WITHOUT_INNER
```

Clicking on a colored chunk while the Plots layer is active opens a small tooltip with the plot's city, name, group, sale price, and permission flags.

### In-World Plot Borders

In addition to the map layer, you can visualize plot borders directly in the world:

```
/plot borders on
/plot borders off
```

This draws the 16×16 chunk outlines around plots in your view.

## Map Command

```
/cmap
```

Opens the standalone city map window. This is separate from the main Pretty GUI.

## Legacy Dialog (`Ctrl+Shift+U`)

The mod also ships a second, older dialog based on the standard Vintage Story `GuiDialog` / `GuiComposer` system. It has its own set of pages (city, plots, alliance, conflicts, prison, ranks, plot groups, prices, summon, player info) but is being gradually replaced by the Pretty GUI.

You can still open it with **Ctrl+Shift+U** if you prefer the classic look, or if a specific flow is not yet fully ported. Functionally, both dialogs call the same server commands.

## Display Settings Reference

| Setting | Default | Description |
|---------|---------|-------------|
| `PLOT_BORDERS_COLOR_WILD_PLOT` | `[64, 255, 255, 0]` | Map color for unclaimed plots |
| `PLOT_BORDERS_COLOR_OUR_CITY_PLOT` | `[143, 5, 146, 0]` | Map color for your city plots |
| `PLOT_BORDERS_COLOR_OTHER_PLOT` | `[16, 49, 158, 0]` | Map color for other cities' plots |
| `CITY_AREA_VISIBILITY_STATE` | `ALL` | Border rendering mode (ALL / WITHOUT_BORDER / WITHOUT_INNER) |
| `GUI_SHOW_DEBT` | `true` | Show city debt in the CITY tab |
| `USE_MOD_CHAT_WINDOW` | `true` | Use the dedicated chat window named `CHAT_WINDOW_NAME` |
| `CHAT_WINDOW_NAME` | `"claims"` | Name of the chat window created by the mod |

See [Configuration](Configuration) for the complete settings list.
