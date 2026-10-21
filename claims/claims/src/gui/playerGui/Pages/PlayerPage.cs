using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using claims.src.network.packets;
using claims.src.part;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// The player themselves: friends, money held and owed, where they respawn, and the way back to
    /// their war camp - in the same cards the other tabs are built from.
    /// </summary>
    public sealed class PlayerPage : CANGuiPage
    {
        private const string RespawnDropKey = "player-respawn";
        private const string TempleDropKey = "player-respawn-temple";

        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            Columns(CardAnchor(ctx), out ElementBounds left, out ElementBounds right);
            double y = left.fixedY;

            // --- friends ---
            // Each friend by name with its own ✕, instead of a count with the names in a tooltip.
            var friendItems = Player.Friends
                .OrderBy(n => n, System.StringComparer.OrdinalIgnoreCase)
                .Select(name => new ItemRow
                {
                    Label = StringFunctions.replaceUnderscore(name),
                    Key = "friend-" + name,
                    Buttons = new List<RowButton>
                    {
                        new RowButton("claims:cancel",
                            () => OpenDialog(EnumUpperWindowSelectedState.REMOVE_FRIEND_CONFIRM, a => a.Text = name),
                            Lang.Get("claims:gui-player-remove-friend-tooltip"))
                    }
                })
                .ToList();
            const int MaxFriendsShown = 5;
            bool friendsCut = friendItems.Count > MaxFriendsShown;

            y = Card.Items(compo, left, y,
                Lang.Get("claims:gui-player-section-friends") + " (" + Player.Friends.Count + ")",
                Card.Limit(friendItems, MaxFriendsShown), Lang.Get("claims:gui-player-no-friends"), slot =>
            {
                var actions = new ActionRow(compo, slot);

                actions.Add("plus", "addFriend",
                    on => { if (on) OpenDialog(EnumUpperWindowSelectedState.ADD_FRIEND_NEED_NAME); },
                    Lang.Get("claims:gui-player-add-friend-tooltip"));

                // For the friends a cut list leaves out; the rows carry their own ✕.
                if (friendsCut)
                {
                    actions.Add("line", "removeFriend",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.REMOVE_FRIEND); },
                        Lang.Get("claims:gui-player-remove-friend-tooltip"));
                }
            });

            // --- money, where there is any ---
            var walletRows = new List<CardRow>();
            if (claims.config.SELECTED_ECONOMY_HANDLER == "VIRTUAL_MONEY" && Player.PlayerBalance > 0)
            {
                walletRows.Add(new CardRow
                {
                    Label = Lang.Get("claims:gui-player-label-balance"),
                    Value = Number(Player.PlayerBalance),
                    Key = "playerBalance"
                });
            }
            if (Player.PlayerNextPayments != null && Player.PlayerNextPayments.Count > 0)
            {
                walletRows.Add(new CardRow
                {
                    Label = Lang.Get("claims:gui-player-label-next-payment"),
                    Value = Player.PlayerNextPayments.Values.Sum().ToString(),
                    // One number tells the player what leaves their pocket but not why; the parts
                    // are what they can act on - leave a group, drop a plot.
                    Tooltip = PaymentBreakdown(),
                    Key = "playerNextPayment"
                });
            }

            if (walletRows.Count > 0)
            {
                Card.Rows(compo, left, y, Lang.Get("claims:gui-player-section-wallet"), walletRows);
            }

            // --- respawn and the camp, both only meaningful for citizens ---
            if (Player.CityInfo != null)
            {
                const double controlHeight = 28;
                bool campTeleport = claims.config.WAR_CAMP_ENABLED && claims.config.WAR_CAMP_TP_ENABLED;
                // The temple list is only offered when the server lets it be picked here; with the
                // setting off the only way is right clicking the temple block itself.
                var temples = Player.CityInfo.TempleRespawnPoints ?? new List<TempleRespawnPointInfo>();
                bool templeChoice = claims.config.TEMPLE_RESPAWN_ENABLED
                                 && claims.config.TEMPLE_RESPAWN_CHOICE_ENABLED
                                 && claims.config.TEMPLE_RESPAWN_CHOICE_FROM_GUI
                                 && temples.Count > 1;

                double height = Card.HeaderHeight + controlHeight + Card.Padding * 2
                              + (templeChoice ? Card.ActionGap + controlHeight : 0)
                              + (campTeleport ? Card.ActionGap + controlHeight : 0);

                ElementBounds inner = Card.Frame(compo, right, right.fixedY, height,
                    Lang.Get("claims:gui-player-section-respawn"));

                string[] values = { "0", "1", "2" };
                string[] names =
                {
                    Lang.Get("claims:respawn_pref_nearest"),
                    Lang.Get("claims:respawn_pref_home"),
                    Lang.Get("claims:respawn_pref_camp")
                };

                int selected = (int)RespawnPreference.Read(compo.Api.World.Player);

                var respawnBounds = inner.FlatCopy().WithFixedHeight(controlHeight);
                compo.AddDropDown(values, names, selected, (code, on) =>
                {
                    if (!int.TryParse(code, out int picked)) return;

                    // Written locally at once so the dropdown keeps the picked entry; the server
                    // holds the authoritative value in the player's watched attributes.
                    RespawnPreference.Write(compo.Api.World.Player, (EnumRespawnPreference)picked);
                    claims.clientChannel.SendPacket(new SavedPlotsPacket
                    {
                        type = PacketsContentEnum.CLIENT_SET_RESPAWN_PREFERENCE,
                        data = picked.ToString()
                    });
                }, respawnBounds, RespawnDropKey);

                var lastBounds = respawnBounds;
                if (templeChoice)
                {
                    // An empty code means "no particular temple" - the nearest one, as before.
                    var templeValues = new List<string> { "" };
                    var templeNames = new List<string> { Lang.Get("claims:gui-respawn-temple-nearest") };
                    foreach (var temple in temples)
                    {
                        templeValues.Add(temple.PlotX + ";" + temple.PlotZ);
                        templeNames.Add(string.IsNullOrEmpty(temple.Name)
                            ? Lang.Get("claims:gui-respawn-temple-at", temple.X, temple.Y, temple.Z)
                            : temple.Name);
                    }

                    int templeSelected = 0;
                    if (RespawnPreference.TryReadTemple(compo.Api.World.Player, Player.CityInfo.Guid, out var chosen))
                    {
                        int idx = templeValues.IndexOf(chosen.X + ";" + chosen.Y);
                        if (idx > 0) templeSelected = idx;
                    }

                    var templeBounds = lastBounds.BelowCopy(0, Card.ActionGap).WithFixedHeight(controlHeight);
                    compo.AddDropDown(templeValues.ToArray(), templeNames.ToArray(), templeSelected, (code, on) =>
                    {
                        claims.clientChannel.SendPacket(new SavedPlotsPacket
                        {
                            type = PacketsContentEnum.CLIENT_SET_RESPAWN_TEMPLE,
                            data = code ?? ""
                        });
                    }, templeBounds, TempleDropKey);
                    Tooltip.Add(compo, Lang.Get("claims:gui-respawn-temple-tooltip"), templeBounds, "tip-respawntemple");
                    lastBounds = templeBounds;
                }

                if (campTeleport)
                {
                    var campBounds = lastBounds.BelowCopy(0, Card.ActionGap).WithFixedHeight(controlHeight);
                    compo.AddButton(Lang.Get("claims:gui-camp-teleport"), new ActionConsumable(() =>
                    {
                        claims.clientChannel.SendPacket(new SavedPlotsPacket
                        {
                            type = PacketsContentEnum.CLIENT_CAMP_TELEPORT
                        });
                        return true;
                    }), campBounds, EnumButtonStyle.Normal);
                    Tooltip.Add(compo,
                        Lang.Get("claims:gui-camp-teleport-tooltip", claims.config.WAR_CAMP_TP_CAST_SECONDS),
                        campBounds, "tip-campteleport");
                }
            }

            NavRow.Build(Gui, ctx.Current, ctx.Line, 0,
                new NavButton("claims:village", () => GoTo(EnumSelectedTab.CitiesListPage), Lang.Get("claims:gui-nav-cities-list")),
                new NavButton("claims:vertical-banner", () => GoTo(EnumSelectedTab.AllianceListPage),
                    Lang.Get("claims:gui_alliance_list_title")),
                // The editor needs the screen to itself - the ghosts sit where this window is.
                new NavButton("claims:expander", () => { Gui.TryClose(); claims.OpenHudLayoutEditor(); },
                    Lang.Get("claims:gui-hud-edit-title")),
                // The same switches as .claimsbanner and .claimsplotpanel; the tooltip says what is on now.
                new NavButton("claims:tower-flag", () => { claims.ToggleLocationBanner(); Gui.BuildMainWindow(); },
                    Lang.Get((claims.clientPrefs?.ShowBanner ?? false) ? "claims:gui-banner-on-tooltip" : "claims:gui-banner-off-tooltip")),
                new NavButton("claims:info", () => { claims.ToggleAutoPlotPanel(); Gui.BuildMainWindow(); },
                    Lang.Get((claims.clientPrefs?.AutoShowPlotPanel ?? false) ? "claims:gui-plotpanel-auto-on-tooltip" : "claims:gui-plotpanel-auto-off-tooltip")),
                new NavButton("claims:magnifying-glass", () => { claims.TogglePlotMarkers(); Gui.BuildMainWindow(); },
                    Lang.Get((claims.clientPrefs?.ShowPlotMarkers ?? false) ? "claims:gui-markers-on-tooltip" : "claims:gui-markers-off-tooltip")));
        }

        /// <summary>Balances are doubles; whole values should not read "1500.0".</summary>
        private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        /// <summary>
        /// What the daily payment is made of, a line per source. The keys are the ones the server
        /// puts in the payments dictionary; an unknown one is printed as it comes rather than
        /// dropped, so a source added later is visible before anyone writes a label for it.
        /// </summary>
        private string PaymentBreakdown()
        {
            var lines = new List<string>();
            foreach (var pair in Player.PlayerNextPayments)
            {
                string label;
                switch (pair.Key)
                {
                    case "city": label = Lang.Get("claims:gui-player-payment-city"); break;
                    case "plots": label = Lang.Get("claims:gui-player-payment-plots"); break;
                    case "plotsgroups": label = Lang.Get("claims:gui-player-payment-plotsgroups"); break;
                    case "subplots": label = Lang.Get("claims:gui-player-payment-subplots"); break;
                    default: label = pair.Key; break;
                }
                lines.Add(label + ": " + pair.Value);
            }
            return string.Join("\n", lines);
        }
    }
}
