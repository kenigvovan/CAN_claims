using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using claims.src.auxialiry;
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

        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            var anchor = ctx.Line.BelowCopy(0, 14);
            anchor.Alignment = EnumDialogArea.LeftTop;
            double columnWidth = (ctx.Line.fixedWidth - Card.ColumnGap) / 2;

            var left = anchor.FlatCopy();
            left.fixedWidth = columnWidth;
            double y = left.fixedY;

            // --- friends ---
            var friendRows = new List<CardRow>
            {
                new CardRow
                {
                    Label = Lang.Get("claims:gui-player-label-friends"),
                    Value = Player.Friends.Count.ToString(),
                    Tooltip = Player.Friends.Count > 0
                        ? StringFunctions.concatStringsWithDelim(Player.Friends, ',')
                        : null,
                    Key = "friends"
                }
            };

            y = Card.RowsWithActions(compo, left, y, Lang.Get("claims:gui-player-section-friends"), friendRows, slot =>
            {
                var actions = new ActionRow(compo, slot);

                actions.Add("plus", "addFriend",
                    on => { if (on) OpenDialog(EnumUpperWindowSelectedState.ADD_FRIEND_NEED_NAME); },
                    Lang.Get("claims:gui-player-add-friend-tooltip"));

                actions.Add("line", "removeFriend",
                    on => { if (on) OpenDialog(EnumUpperWindowSelectedState.REMOVE_FRIEND); },
                    Lang.Get("claims:gui-player-remove-friend-tooltip"));
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
                var right = anchor.FlatCopy();
                right.fixedWidth = columnWidth;
                right.fixedX += columnWidth + Card.ColumnGap;

                const double controlHeight = 28;
                bool campTeleport = claims.config.WAR_CAMP_ENABLED && claims.config.WAR_CAMP_TP_ENABLED;

                double height = Card.HeaderHeight + controlHeight + Card.Padding * 2
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

                if (campTeleport)
                {
                    var campBounds = respawnBounds.BelowCopy(0, Card.ActionGap).WithFixedHeight(controlHeight);
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
                    Lang.Get("claims:gui_alliance_list_title")));
        }

        /// <summary>Balances are doubles; whole values should not read "1500.0".</summary>
        private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
