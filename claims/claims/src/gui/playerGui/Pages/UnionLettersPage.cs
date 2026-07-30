using System;
using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// Pending union offers and dissolution requests, over a card saying who the alliance already
    /// stands with and which of those unions are running out their notice.
    /// </summary>
    public sealed class UnionLettersPage : CANGuiPage
    {
        /// <summary>Height of the anchor the list is measured from.</summary>
        private const double HeadingHeight = 24;

        /// <summary>The list is never squeezed below this, however little room is left.</summary>
        private const double MinListHeight = 70;

        protected override bool IsAvailable(out string reasonLangKey)
        {
            reasonLangKey = null;
            return Player.CityInfo != null;
        }

        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;
            var alliance = Player.AllianceInfo;

            var anchor = ctx.Line.BelowCopy(0, 14);
            anchor.Alignment = EnumDialogArea.LeftTop;
            anchor.fixedWidth = ctx.Line.fixedWidth;
            anchor.fixedHeight = HeadingHeight;

            var rows = new List<CardRow>();

            if (alliance == null)
            {
                // Unions are made between alliances, so a city outside one has nothing to offer.
                rows.Add(new CardRow
                {
                    Label = Lang.Get("claims:gui-union-no-alliance"),
                    Value = "",
                    Key = "union-no-alliance"
                });
            }
            else
            {
                rows.Add(new CardRow
                {
                    Label = Lang.Get("claims:gui-alliance-label-allies"),
                    Value = alliance.Allies.Count.ToString(),
                    Tooltip = alliance.Allies.Count > 0 ? string.Join(", ", alliance.Allies) : null,
                    ValueColor = alliance.Allies.Count > 0 ? ClaimsColors.Success : null,
                    Key = "unionAllies"
                });

                rows.Add(new CardRow
                {
                    Label = Lang.Get("claims:gui-union-label-offers"),
                    Value = Player.CityInfo.ClientUnionLetterCellElements.Count.ToString(),
                    Key = "unionOffers"
                });

                // A union given notice still holds until its timer runs out, so it is named here
                // rather than quietly disappearing from the ally count.
                if (alliance.PendingUnionBreaks != null)
                {
                    long nowSeconds = TimeFunctions.getEpochSeconds();
                    int index = 0;
                    foreach (var pending in alliance.PendingUnionBreaks)
                    {
                        if (pending.Value <= nowSeconds) continue;

                        string allyName = StringFunctions.replaceUnderscore(pending.Key);
                        string left = StringFunctions.FormatDuration(pending.Value - nowSeconds);

                        rows.Add(new CardRow
                        {
                            Label = allyName,
                            Value = left,
                            ValueColor = ClaimsColors.Danger,
                            Tooltip = Lang.Get("claims:gui-union-break-pending", allyName, left),
                            Key = "unionBreak" + index++
                        });
                    }
                }
            }

            double y = Card.RowsWithActions(compo, anchor, anchor.fixedY,
                Lang.Get("claims:gui-union-section-unions"), rows, slot =>
            {
                var actions = new ActionRow(compo, slot);

                actions.Add("claims:tower-flag", "proposeUnion",
                    on => { if (on) OpenDialog(EnumUpperWindowSelectedState.ALLIANCE_SEND_NEW_UNION_LETTER_NEED_NAME); },
                    Lang.Get("claims:gui-send-new-union-letter"));

                actions.Add("claims:exit-door", "cancelUnion",
                    on => { if (on) OpenDialog(EnumUpperWindowSelectedState.ALLIANCE_CANCEL_UNION_SELECT); },
                    Lang.Get("claims:gui-send-leave-union"));
            });

            var listAnchor = anchor.FlatCopy();
            listAnchor.fixedY = y;

            // The list fills what is left down to the navigation row, rather than a fixed reserve
            // that assumed a card of one particular height.
            var listOpts = new ScrollableListOptions { Key = "union-letters", TitleHeightShrink = 0 };
            listOpts.HeightReserve = ScrollableList.ReserveFor(Gui,
                Math.Max(MinListHeight,
                    Gui.mainBounds.fixedHeight * NavRow.LineHeightFraction
                        - y - Card.Gap - ScrollableList.Overhead(listAnchor, listOpts)));

            var list = ScrollableList.Add(Gui, listAnchor,
                Lang.Get("claims:union_letters_list"),
                Player.CityInfo.ClientUnionLetterCellElements,
                (ClientUnionLetterCellElement cell, ElementBounds bounds) => new GuiElementUnionLetterCell(compo.Api, cell, bounds) { On = true },
                listOpts);

            NavRow.Build(Gui, anchor, ctx.Line, 15,
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.AllianceInfoPage), Lang.Get("claims:gui-nav-back")));

            ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
        }
    }
}
