using System;
using System.Collections.Generic;
using System.Linq;
using claims.src.auxialiry;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using claims.src.network.packets;
using claims.src.part.structure.conflict;
using claims.src.part.structure.war;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    public sealed class ConflictLettersPage : CANGuiPage
    {
        /// <summary>Height of the anchor the list hangs its own heading off.</summary>
        private const double HeadingHeight = 24;

        /// <summary>The list is never squeezed below this, however little room the window leaves.</summary>
        private const double MinListHeight = 70;

        protected override void BuildContent(PageBuildContext ctx)
        {
            var lineBounds = ctx.Line;
            var compo = ctx.Compo;
            var clientInfo = Player;

            var anchor = ctx.Line.BelowCopy(0, 14);
            anchor.Alignment = EnumDialogArea.LeftTop;
            anchor.fixedWidth = lineBounds.fixedWidth;
            anchor.fixedHeight = HeadingHeight;

            bool inAlliance = Player.AllianceInfo != null;

            // The card of buttons goes above the list, its height known before the list is sized -
            // hanging them off the list's inset put them wherever that list's clip happened to end.
            double y = anchor.fixedY;
            y = Card.Actions(compo, anchor, y, Lang.Get("claims:gui-conflict-letters-actions"), slot =>
            {
                var actions = new ActionRow(compo, slot);

                // Alliances declare war as a bloc, lone cities on their own behalf.
                actions.Add("claims:sword-brandish", "sendConflictLetter",
                    on =>
                    {
                        if (on)
                        {
                            OpenDialog(inAlliance
                                ? EnumUpperWindowSelectedState.ALLIANCE_SEND_NEW_CONFLICT_LETTER_NEED_NAME
                                : EnumUpperWindowSelectedState.CITY_SEND_NEW_CONFLICT_LETTER_NEED_NAME);
                        }
                    },
                    Lang.Get("claims:gui-send-new-conflict-letter"));

                // Non-aggression pacts are a peacetime tool for any party - alliance leader or
                // independent city mayor.
                if (claims.config.WAR_NAP_ENABLED)
                {
                    actions.Add("claims:peace-dove", "sendNapOffer",
                        on =>
                        {
                            if (on)
                            {
                                OpenDialog(inAlliance
                                    ? EnumUpperWindowSelectedState.ALLIANCE_SEND_NAP_OFFER_NEED_NAME
                                    : EnumUpperWindowSelectedState.CITY_SEND_NAP_OFFER_NEED_NAME);
                            }
                        },
                        Lang.Get("claims:gui-send-new-nap-letter"));
                }

                // An ultimatum is also a peacetime move: comply, or hand the sender a free war.
                if (claims.config.WAR_ULTIMATUM_ENABLED)
                {
                    actions.Add("claims:price-tag", "sendUltimatum",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.SEND_ULTIMATUM); },
                        Lang.Get("claims:gui-send-new-ultimatum"));
                }
            });

            var listAnchor = anchor.FlatCopy();
            listAnchor.fixedY = y;

            var listOpts = new ScrollableListOptions { Key = "conflict-letters", TitleHeightShrink = 0 };
            listOpts.HeightReserve = ScrollableList.ReserveFor(Gui,
                Math.Max(MinListHeight,
                    Gui.mainBounds.fixedHeight * NavRow.LineHeightFraction
                        - y - Card.Gap - ScrollableList.Overhead(listAnchor, listOpts)));

            var list = ScrollableList.Add(Gui, listAnchor,
                Lang.Get("claims:conflict_letters_list"),
                clientInfo.CityInfo.ClientConflictLetterCellElements,
                (ClientConflictLetterCellElement cell, ElementBounds bounds) => new GuiElementConflictLetterCell(compo.Api, cell, bounds) { On = true },
                listOpts);

            /*==============================================================================================*/
            /*=====================================UNDER 2 LINE=============================================*/
            /*==============================================================================================*/
            NavRow.Build(Gui, anchor, lineBounds, 15,
                new NavButton("claims:fast-backward-button", () => GoTo(State.ConflictSourceTab), Lang.Get("claims:gui-nav-back")),
                new NavButton("claims:frog-mouth-helm", () => GoTo(EnumSelectedTab.ConflictsPage), Lang.Get("claims:gui-nav-conflicts")));

            ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
        }
    }

    public sealed class ConflictsPage : CANGuiPage
    {
        /// <summary>Height of the anchor each list hangs its own heading off.</summary>
        private const double HeadingHeight = 24;

        /// <summary>No list is squeezed below this, however little room the window leaves.</summary>
        private const double MinListHeight = 70;

        protected override void BuildContent(PageBuildContext ctx)
        {
            var lineBounds = ctx.Line;
            var compo = ctx.Compo;
            var clientInfo = Player;

            var anchor = ctx.Line.BelowCopy(0, 14);
            anchor.Alignment = EnumDialogArea.LeftTop;
            anchor.fixedWidth = lineBounds.fixedWidth;
            anchor.fixedHeight = HeadingHeight;

            // Two lists share the page, so the space between the tab bar and the navigation row is
            // split between them. The casus belli list used to be anchored under the first list's
            // inset - bounds living inside that list's own clip - which put it past the bottom edge.
            var conflictOpts = new ScrollableListOptions { Key = "conflicts", TitleHeightShrink = 0 };
            var casusOpts = new ScrollableListOptions { Key = "casus-belli", TitleHeightShrink = 0 };

            double overhead = ScrollableList.Overhead(anchor, conflictOpts);
            double usable = Gui.mainBounds.fixedHeight * NavRow.LineHeightFraction;
            double listHeight = Math.Max(MinListHeight,
                (usable - anchor.fixedY - Card.Gap - overhead * 2) / 2);

            conflictOpts.HeightReserve = ScrollableList.ReserveFor(Gui, listHeight);
            casusOpts.HeightReserve = conflictOpts.HeightReserve;

            var list = ScrollableList.Add(Gui, anchor,
                Lang.Get("claims:conflict_list"),
                clientInfo.CityInfo.ClientConflictCellElements,
                (ClientConflictCellElement cell, ElementBounds bounds) => new GuiElementConflictCell(compo.Api, cell, bounds) { On = true },
                conflictOpts);

            // Who we may go to war with, and what stands in the way. The server owns the answer, and
            // an ally's war can start without any event addressed to us, so ask on every rebuild.
            claims.clientChannel.SendPacket(new SavedPlotsPacket { type = PacketsContentEnum.CLIENT_REQUEST_CASUS_BELLI });

            long now = TimeFunctions.getEpochSeconds();
            // Rows with nothing left to say are dropped: lapsed reason, no cooldown, no pact.
            var casusBelli = clientInfo.CityInfo.ClientCasusBelliCellElements
                .Where(cb => cb.Kind == CasusBelliKind.AllyAtWar || cb.ExpiresAt > now
                          || cb.CooldownUntil > now || cb.PactUntil > now || cb.UnionBreakUntil > now)
                .ToList();

            var cbAnchor = anchor.FlatCopy();
            cbAnchor.fixedY = anchor.fixedY + overhead + listHeight + Card.Gap;

            var cbList = ScrollableList.Add(Gui, cbAnchor,
                Lang.Get("claims:gui_casus_belli_list"),
                casusBelli,
                (ClientCasusBelliCellElement cell, ElementBounds bounds) => new GuiElementCasusBelliCell(compo.Api, cell, bounds) { On = true },
                casusOpts);

            Tooltip.Add(compo, Lang.Get("claims:gui_casus_belli_hint"), cbList.Title, "tip-casusbelli");

            /*==============================================================================================*/
            /*=====================================UNDER 2 LINE=============================================*/
            /*==============================================================================================*/
            NavRow.Build(Gui, anchor, lineBounds, 15,
                new NavButton("claims:fast-backward-button", () => GoTo(State.ConflictSourceTab), Lang.Get("claims:gui-nav-back")),
                new NavButton("claims:envelope", () => GoTo(EnumSelectedTab.ConflictLettersPage), Lang.Get("claims:gui-nav-conflict-letters")));

            ctx.AfterCompose(() =>
            {
                list.ApplyScrollbarHeights(compo);
                cbList.ApplyScrollbarHeights(compo);
            });
        }
    }

    public sealed class ConflictInfoPage : CANGuiPage
    {
        /// <summary>Height of the anchor the schedule list is measured from.</summary>
        private const double HeadingHeight = 24;

        /// <summary>The schedule is never squeezed below this, however little room is left.</summary>
        private const double MinListHeight = 70;

        /// <summary>Height of the approved/suggested tab strip.</summary>
        private const double TabRowHeight = 30;

        private ClientConflictCellElement SelectedConflict()
            => Player.CityInfo?.ClientConflictCellElements.FirstOrDefault(c => c.Guid == State.DialogArgs.Selected);

        /// <summary>
        /// The name our side fights under. An independent city has no alliance, so reading
        /// AllianceInfo.Name outright threw as soon as a lone mayor opened a conflict.
        /// </summary>
        public static string OurPartyName()
        {
            var info = claims.clientDataStorage.clientPlayerInfo;
            return info.AllianceInfo?.Name ?? info.CityInfo?.Name ?? "";
        }

        /// <summary>The guid war schedules are submitted under, alliance first, city otherwise.</summary>
        private static string OurPartyGuid()
        {
            var info = claims.clientDataStorage.clientPlayerInfo;
            return info.AllianceInfo?.Guid ?? info.CityInfo?.Guid;
        }

        protected override bool IsAvailable(out string reasonLangKey)
        {
            reasonLangKey = null;
            return Player.CityInfo != null && SelectedConflict() != null;
        }

        protected override void BuildContent(PageBuildContext ctx)
        {
            var gui = Gui;
            var lineBounds = ctx.Line;
            var compo = ctx.Compo;
            var clientInfo = Player;

            var cell = SelectedConflict();

            // The grid held whatever the last visit left in it: opening a conflict showed neither
            // the agreed windows nor the enemy's proposal until the tabs were clicked, and sending
            // from that state posted a stale schedule into the conflict just opened.
            string gridKey = cell.Guid + ":" + State.SelectedTabGroup;
            if (State.WarGridLoadedFor != gridKey)
            {
                State.WarGridLoadedFor = gridKey;
                if (State.SelectedTabGroup == (int)EnumSelectedWarRangesTab.APPROVED)
                {
                    WarRangeMath.FillWarRangeArrays(cell.WarRanges);
                }
                else if (OurPartyName().Equals(cell.FirstPartyName))
                {
                    WarRangeMath.FillTwoWarRangesArrays(cell.FirstWarRanges, cell.SecondWarRanges);
                }
                else
                {
                    WarRangeMath.FillTwoWarRangesArrays(cell.SecondWarRanges, cell.FirstWarRanges);
                }
            }

            var anchor = ctx.Line.BelowCopy(0, 14);
            anchor.Alignment = EnumDialogArea.LeftTop;
            anchor.fixedWidth = lineBounds.fixedWidth;
            anchor.fixedHeight = HeadingHeight;

            double columnWidth = (lineBounds.fixedWidth - Card.ColumnGap) / 2;

            // --- left: who is fighting and how it stands ---
            var leftColumn = anchor.FlatCopy();
            leftColumn.fixedWidth = columnWidth;

            var conflictRows = new List<CardRow>
            {
                // Party types belong here: a war against a city is not a war against its alliance.
                new CardRow
                {
                    Label = WarTargetTypeHelper.LangLabel(cell.FirstPartyType),
                    Value = cell.FirstPartyName,
                    Key = "firstParty"
                },
                new CardRow
                {
                    Label = WarTargetTypeHelper.LangLabel(cell.SecondPartyType),
                    Value = cell.SecondPartyName,
                    Key = "secondParty"
                },
                new CardRow
                {
                    Label = Lang.Get("claims:gui_conflict_info_war_score"),
                    Value = cell.FirstScore + " : " + cell.SecondScore,
                    Tooltip = Lang.Get("claims:gui_conflict_info_to_win", claims.config.WAR_SCORE_TO_WIN),
                    Key = "warScore"
                },
                new CardRow
                {
                    Label = Lang.Get("claims:gui_conflict_info_started_by"),
                    Value = cell.StartedByPartyName,
                    Key = "startedBy"
                },
                new CardRow
                {
                    Label = Lang.Get("claims:gui_conflict_info_created"),
                    Value = TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(cell.TimeStampCreated, true),
                    Key = "conflictCreated"
                }
            };

            if (cell.ActiveWarTime)
            {
                conflictRows.Insert(0, new CardRow
                {
                    Label = Lang.Get("claims:gui_battle_active"),
                    Value = "",
                    ValueColor = ClaimsColors.Danger,
                    Key = "battleActive"
                });
            }

            double leftY = Card.Rows(compo, leftColumn, leftColumn.fixedY,
                Lang.Get("claims:gui-conflict-section-conflict"), conflictRows);

            // --- right: when the fighting happens ---
            var rightColumn = anchor.FlatCopy();
            rightColumn.fixedWidth = columnWidth;
            rightColumn.fixedX += columnWidth + Card.ColumnGap;

            double rightY = Card.Rows(compo, rightColumn, rightColumn.fixedY,
                Lang.Get("claims:gui-conflict-section-battles"), new List<CardRow>
            {
                new CardRow
                {
                    Label = Lang.Get("claims:gui_conflict_info_pause_days"),
                    Value = cell.MinimumDaysBetweenBattles.ToString(),
                    Key = "pauseDays"
                },
                new CardRow
                {
                    // FormatBattleDate: a (DateTimeOffset) cast on the epoch/MinValue placeholder
                    // dates crashed the client for every player in a zone east of UTC.
                    Label = Lang.Get("claims:gui-conflict-label-last-battle"),
                    Value = TimeFunctions.FormatBattleDate(cell.LastBattleDateStart),
                    Tooltip = Lang.Get("claims:gui_last_start_end_battle",
                        TimeFunctions.FormatBattleDate(cell.LastBattleDateStart),
                        TimeFunctions.FormatBattleDate(cell.LastBattleDateEnd)),
                    Key = "lastBattle"
                },
                new CardRow
                {
                    Label = Lang.Get("claims:gui-conflict-label-next-battle"),
                    Value = TimeFunctions.FormatBattleDate(cell.NextBattleDateStart),
                    Tooltip = Lang.Get("claims:gui_next_start_end_battle",
                        TimeFunctions.FormatBattleDate(cell.NextBattleDateStart),
                        TimeFunctions.FormatBattleDate(cell.NextBattleDateEnd)),
                    Key = "nextBattle"
                }
            });

            double headerBottom = Math.Max(leftY, rightY);

            // The tab strip keeps the row to itself except for the submit button on its right: the
            // button used to sit alone under the list, a bare icon with a whole row to itself.
            var p = anchor.FlatCopy()
                .WithFixedSize(lineBounds.fixedWidth - Card.ActionSize - Card.ActionGap, TabRowHeight);
            p.fixedY = headerBottom;

            GuiTab[] horizontalTabs = new GuiTab[2];

            horizontalTabs[0] = new GuiTab();
            horizontalTabs[0].Name = Lang.Get("claims:gui_conflict_warrange_tab_selected");
            horizontalTabs[0].DataInt = 0;

            horizontalTabs[1] = new GuiTab();
            horizontalTabs[1].Name = Lang.Get("claims:gui_conflict_warrange_tab_suggested");
            horizontalTabs[1].DataInt = 1;
            compo.AddHorizontalTabs(horizontalTabs, p, (int value) =>
            {
                var tabs = compo.GetHorizontalTabs("groupTabs");
                if (tabs != null)
                {
                    if (tabs.activeElement != value)
                    {
                        tabs.activeElement = value;
                        State.SelectedTabGroup = value;
                        var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientConflictCellElements.FirstOrDefault(c => c.Guid == State.DialogArgs.Selected);
                        if (cell == null)
                        {
                            return;
                        }
                        if (value == (int)EnumSelectedWarRangesTab.APPROVED)
                        {
                            WarRangeMath.FillWarRangeArrays(cell.WarRanges);
                        }
                        else
                        {
                            // An independent city fights under its own name, so an alliance must not be assumed.
                            if (OurPartyName().Equals(cell.FirstPartyName))
                            {
                                WarRangeMath.FillTwoWarRangesArrays(cell.FirstWarRanges, cell.SecondWarRanges);
                            }
                            else
                            {
                                WarRangeMath.FillTwoWarRangesArrays(cell.SecondWarRanges, cell.FirstWarRanges);
                            }
                        }
                        gui.BuildMainWindow();
                    }
                }
            }, CairoFont.WhiteSmallText(), CairoFont.WhiteSmallText(), "groupTabs");
            compo.GetHorizontalTabs("groupTabs").activeElement = State.SelectedTabGroup;
            // Both branches share the list key: only one of them ever runs per compose.
            var listOpts = new ScrollableListOptions
            {
                Key = "war-ranges",
                TitleHeightShrink = 0
            };

            // Which clock the grid is in. A slot is a weekday plus a time read on the server's
            // schedule clock, and a player in another zone reading "Saturday 20:00" as their own
            // Saturday evening turns up for a battle that is already over.
            const double clockRowHeight = 18;
            var clockBounds = anchor.FlatCopy().WithFixedHeight(clockRowHeight);
            clockBounds.fixedY = headerBottom + TabRowHeight + 4;
            compo.AddStaticText(WarScheduleDisplay.GridHeading(),
                CairoFont.WhiteDetailText().WithColor(ClaimsColors.Label), clockBounds, "warrange-clock");

            var listAnchor = anchor.FlatCopy();
            listAnchor.fixedY = clockBounds.fixedY + clockRowHeight + 4;
            listAnchor.fixedHeight = 0;

            // The schedule is parented on the cursor itself rather than on a title row it does not
            // have, so it starts where it is put instead of a heading's height further down.
            listOpts.Container = listAnchor;

            // The schedule fills everything left down to the navigation row, rather than a fixed
            // reserve that assumed a header of one particular height. It carries no heading of its
            // own - the tabs above already name it - so no room is left for one.
            double listHeight = Math.Max(MinListHeight,
                Gui.mainBounds.fixedHeight * NavRow.LineHeightFraction
                    - listAnchor.fixedY - Card.Gap
                    - ScrollableList.Overhead(listAnchor, listOpts, hasTitle: false));
            listOpts.HeightReserve = ScrollableList.ReserveFor(Gui, listHeight);

            ScrollableListLayout list;
            if (State.SelectedTabGroup == (int)EnumSelectedWarRangesTab.APPROVED)
            {
                list = ScrollableList.Add(gui, listAnchor, null,
                    clientInfo.CityInfo.ClientWarRangeCellElements
                             .Where(c => WarScheduleHelper.IsDayAllowed(c.DayOfWeek)),
                    (ClientWarRangeCellElement c, ElementBounds bounds) => new GuiElementWarRangeCell(compo.Api, c, bounds, State.SelectedTabGroup != 0) { On = true },
                    listOpts);
            }
            else
            {
                // Days the server forbids are left out rather than drawn dead: a weekend-only server
                // shows two rows instead of seven, five of them unusable. Which days those are is in
                // the heading above the grid, so their absence is not a mystery.
                list = ScrollableList.Add(gui, listAnchor, null,
                    clientInfo.CityInfo.ClientTwoWarRangesCellElement
                             .Where(c => WarScheduleHelper.IsDayAllowed(c.DayOfWeek)),
                    (ClientTwoWarRangesCellElement c, ElementBounds bounds) => new GuiElementTwoWarRangesCell(compo.Api, c, bounds) { On = true },
                    listOpts);
            }

            var submitBounds = anchor.FlatCopy().WithFixedSize(Card.ActionSize, Card.ActionSize);
            submitBounds.fixedX += lineBounds.fixedWidth - Card.ActionSize;
            submitBounds.fixedY = headerBottom + (TabRowHeight - Card.ActionSize) / 2;
            compo.AddIconButton("claims:check-mark", (bool t) =>
            {
                if (t)
                {
                    var cell = SelectedConflict();
                    if (cell == null) return;

                    // Which grid the player just edited depends on the tab: the approved tab renders
                    // ClientWarRangeCellElements, the suggestions tab the two-sided cells. Reading
                    // the two-sided ones unconditionally sent an empty schedule from the first tab.
                    var city = claims.clientDataStorage.clientPlayerInfo.CityInfo;
                    bool approvedTab = State.SelectedTabGroup == (int)EnumSelectedWarRangesTab.APPROVED;
                    Func<int, DayOfWeek> dayOf = index => approvedTab
                        ? city.ClientWarRangeCellElements[index].DayOfWeek
                        : city.ClientTwoWarRangesCellElement[index].DayOfWeek;
                    Func<int, bool[]> slotsOf = index => approvedTab
                        ? city.ClientWarRangeCellElements[index].WarRangeArray
                        : city.ClientTwoWarRangesCellElement[index].OurWarRangeArray;

                    // The whole week as one flat strip of half-hour slots, indexed by the day the cell
                    // says it is rather than by its position in the list. Reading it as a strip is
                    // what makes a window that runs past midnight - or past Saturday into Sunday -
                    // one range instead of a special case.
                    const int slotsPerDay = 48;
                    const int totalSlots = 7 * slotsPerDay;
                    const int cellMinutes = 24 * 60 / slotsPerDay;
                    bool[] week = new bool[totalSlots];
                    for (int index = 0; index < 7; index++)
                    {
                        bool[] daySlots = slotsOf(index);
                        int dayBase = (int)dayOf(index) * slotsPerDay;
                        for (int i = 0; i < slotsPerDay; i++) week[dayBase + i] = daySlots[i];
                    }

                    List<SelectedWarRange> selectedWarRanges = new List<SelectedWarRange>();

                    // Start scanning at a slot whose predecessor is empty, so a window straddling the
                    // week boundary is not cut in two. There is no such slot when the week is either
                    // fully marked or fully empty.
                    int firstSlot = -1;
                    for (int s = 0; s < totalSlots; s++)
                    {
                        if (week[s] && !week[(s - 1 + totalSlots) % totalSlots]) { firstSlot = s; break; }
                    }

                    if (firstSlot < 0)
                    {
                        // Marking the entire week used to send nothing at all.
                        if (week[0])
                        {
                            selectedWarRanges.Add(new SelectedWarRange(DayOfWeek.Sunday, DayOfWeek.Sunday,
                                TimeSpan.Zero, TimeSpan.FromMinutes(totalSlots * cellMinutes), OurPartyGuid()));
                        }
                    }
                    else
                    {
                        int cursor = 0;
                        while (cursor < totalSlots)
                        {
                            int slot = (firstSlot + cursor) % totalSlots;
                            if (!week[slot]) { cursor++; continue; }

                            int length = 0;
                            while (cursor + length < totalSlots && week[(firstSlot + cursor + length) % totalSlots]) length++;

                            int startMinutes = slot * cellMinutes;
                            int durationMinutes = length * cellMinutes;
                            int endMinutes = (startMinutes + durationMinutes) % (7 * 24 * 60);
                            selectedWarRanges.Add(new SelectedWarRange(
                                (DayOfWeek)(startMinutes / (24 * 60)),
                                (DayOfWeek)(endMinutes / (24 * 60)),
                                TimeSpan.FromMinutes(startMinutes % (24 * 60)),
                                TimeSpan.FromMinutes(durationMinutes),
                                OurPartyGuid()));
                            cursor += length;
                        }
                    }

                    if (cell.FirstPartyName.Equals(OurPartyName()))
                    {
                        cell.FirstWarRanges = selectedWarRanges;
                    }
                    else
                    {
                        cell.SecondWarRanges = selectedWarRanges;
                    }
                    Dictionary<EnumPlayerRelatedInfo, string> collector = new Dictionary<EnumPlayerRelatedInfo, string>
                    {
                        { EnumPlayerRelatedInfo.CLIENT_CONFLICT_SUGGESTED_WARRANGE, JsonConvert.SerializeObject(cell) }
                    };
                    claims.clientChannel.SendPacket(new PlayerGuiRelatedInfoPacket()
                    {
                        playerGuiRelatedInfoDictionary = collector
                    });
                }
            }, submitBounds);

            Tooltip.Add(compo, Lang.Get("claims:gui-send-new-conflict-time"), submitBounds, "tip-sendwarrange");

            NavRow.Build(Gui, anchor, lineBounds, 15,
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.ConflictsPage), Lang.Get("claims:gui-nav-back")));

            ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
        }
    }

    /// <summary>
    /// War range bookkeeping. Not GUI code - it maps conflict schedules onto the 48 half-hour slots
    /// per day that the cells render, and CANClaimsGui.SelectRangeAndFill calls into it when a city
    /// update packet arrives.
    /// </summary>
    public static class WarRangeMath
    {
        public static void FillListValues(List<SelectedWarRange> ranges, bool forEnemy = false)
        {
            foreach (var range in ranges)
            {
                int slotCount = (int)(range.Duration.TotalMinutes / claims.config.MIN_RANGE_CELL_DURATION_MINUTES);
                int startSlot = (int)(range.StartTime.TotalMinutes / claims.config.MIN_RANGE_CELL_DURATION_MINUTES);

                for (int i = (int)range.StartDay, k = 0; ; i++, k++)
                {
                    if (k > 6)
                    {
                        break;
                    }
                    int dayIndex = i % 7;
                    ClientTwoWarRangesCellElement cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientTwoWarRangesCellElement.FirstOrDefault(c => c.DayOfWeek == (DayOfWeek)dayIndex);
                    if (cell == null)
                    {
                        continue;
                    }
                    int startPoint = dayIndex == (int)range.StartDay ? startSlot : 0;
                    for (int j = startPoint; j < 48; j++)
                    {
                        if (forEnemy)
                        {
                            cell.EnemyWarRangeArray[j] = true;
                        }
                        else
                        {
                            cell.OurWarRangeArray[j] = true;
                        }
                        slotCount--;
                        if (slotCount <= 0)
                        {
                            goto finshedRange;
                        }
                    }
                }
            finshedRange:
                ;
            }
        }
        public static void FillTwoWarRangesArrays(List<SelectedWarRange> ourRanges, List<SelectedWarRange> enemyRanges)
        {
            foreach (var it in claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientTwoWarRangesCellElement)
            {
                for (int i = 0; i < it.OurWarRangeArray.Length; i++)
                {
                    it.OurWarRangeArray[i] = false;
                    it.EnemyWarRangeArray[i] = false;
                }
            }
            FillListValues(ourRanges);
            FillListValues(enemyRanges, true);
        }
        public static void FillWarRangeArrays(List<SelectedWarRange> ranges)
        {
            foreach (var it in claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientWarRangeCellElements)
            {
                for (int i = 0; i < it.WarRangeArray.Length; i++)
                {
                    it.WarRangeArray[i] = false;
                }
            }
            foreach (var range in ranges)
            {
                int slotCount = (int)(range.Duration.TotalMinutes / claims.config.MIN_RANGE_CELL_DURATION_MINUTES);
                int startSlot = (int)(range.StartTime.TotalMinutes / claims.config.MIN_RANGE_CELL_DURATION_MINUTES);

                for (int i = (int)range.StartDay, k = 0; ; i++, k++)
                {
                    if (k > 6)
                    {
                        break;
                    }
                    int dayIndex = i % 7;
                    ClientWarRangeCellElement cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientWarRangeCellElements.FirstOrDefault(c => c.DayOfWeek == (DayOfWeek)dayIndex);
                    if (cell == null)
                    {
                        continue;
                    }
                    int startPoint = dayIndex == (int)range.StartDay ? startSlot : 0;
                    for (int j = startPoint; j < 48; j++)
                    {
                        cell.WarRangeArray[j] = true;
                        slotCount--;
                        if (slotCount <= 0)
                        {
                            goto finshedRange;
                        }
                    }
                }
            finshedRange:
                ;
            }
        }
    }
}
