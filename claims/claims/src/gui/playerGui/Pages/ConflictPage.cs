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
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    public sealed class ConflictLettersPage : CANGuiPage
    {
        protected override void BuildContent(PageBuildContext ctx)
        {
            var gui = Gui;
            var lineBounds = ctx.Line;
            var compo = ctx.Compo;
            var clientInfo = Player;

            var currentBounds = ctx.Current;
            currentBounds.WithAlignment(EnumDialogArea.LeftTop);
            currentBounds.fixedWidth = lineBounds.fixedWidth;
            currentBounds = currentBounds.BelowCopy(0, 0);

            var list = ScrollableList.Add(Gui, currentBounds,
                Lang.Get("claims:conflict_letters_list"),
                clientInfo.CityInfo.ClientConflictLetterCellElements,
                (ClientConflictLetterCellElement cell, ElementBounds bounds) => new GuiElementConflictLetterCell(compo.Api, cell, bounds) { On = true },
                new ScrollableListOptions { Key = "conflict-letters", HeightReserve = 350 });

            bool inAlliance = Player.AllianceInfo != null;

            ElementBounds actionBounds = list.Inset.BelowCopy(15, 15);
            actionBounds.WithFixedWidth(25).WithFixedHeight(25);
            compo.AddInset(actionBounds);
            compo.AddIconButton("claims:sword-brandish", (bool t) =>
            {
                if (t)
                {
                    // Alliances declare war as a bloc, lone cities on their own behalf.
                    OpenDialog(inAlliance
                        ? EnumUpperWindowSelectedState.ALLIANCE_SEND_NEW_CONFLICT_LETTER_NEED_NAME
                        : EnumUpperWindowSelectedState.CITY_SEND_NEW_CONFLICT_LETTER_NEED_NAME);
                }
            }, actionBounds);
            compo.AddHoverText(Lang.Get("claims:gui-send-new-conflict-letter"),
                                            CairoFont.SmallButtonText(),
                                            (int)currentBounds.fixedWidth / 2, actionBounds);

            // Non-aggression pacts are a peacetime tool for any party - alliance leader or
            // independent city mayor.
            if (claims.config.WAR_NAP_ENABLED)
            {
                actionBounds = actionBounds.RightCopy(15);
                compo.AddInset(actionBounds);
                var napBounds = actionBounds;
                compo.AddIconButton("claims:peace-dove", (bool t) =>
                {
                    if (t)
                    {
                        OpenDialog(inAlliance
                            ? EnumUpperWindowSelectedState.ALLIANCE_SEND_NAP_OFFER_NEED_NAME
                            : EnumUpperWindowSelectedState.CITY_SEND_NAP_OFFER_NEED_NAME);
                    }
                }, napBounds);
                compo.AddHoverText(Lang.Get("claims:gui-send-new-nap-letter"),
                    CairoFont.SmallButtonText(), (int)currentBounds.fixedWidth / 2, napBounds);
            }

            // An ultimatum is also a peacetime move: comply, or hand the sender a free war.
            if (claims.config.WAR_ULTIMATUM_ENABLED)
            {
                actionBounds = actionBounds.RightCopy(15);
                compo.AddInset(actionBounds);
                var ultimatumBounds = actionBounds;
                compo.AddIconButton("claims:price-tag", (bool t) =>
                {
                    if (t) OpenDialog(EnumUpperWindowSelectedState.SEND_ULTIMATUM);
                }, ultimatumBounds);
                compo.AddHoverText(Lang.Get("claims:gui-send-new-ultimatum"),
                    CairoFont.SmallButtonText(), (int)currentBounds.fixedWidth / 2, ultimatumBounds);
            }

            /*==============================================================================================*/
            /*=====================================UNDER 2 LINE=============================================*/
            /*==============================================================================================*/
            NavRow.Build(Gui, currentBounds, lineBounds, 15,
                new NavButton("claims:fast-backward-button", () => GoTo(State.ConflictSourceTab), Lang.Get("claims:gui-nav-back")),
                new NavButton("claims:frog-mouth-helm", () => GoTo(EnumSelectedTab.ConflictsPage), Lang.Get("claims:gui-nav-conflicts")));

            ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
        }
    }

    public sealed class ConflictsPage : CANGuiPage
    {
        protected override void BuildContent(PageBuildContext ctx)
        {
            var lineBounds = ctx.Line;
            var compo = ctx.Compo;
            var clientInfo = Player;

            var currentBounds = ctx.Current;
            currentBounds.WithAlignment(EnumDialogArea.LeftTop);
            currentBounds.fixedWidth = lineBounds.fixedWidth;
            currentBounds = currentBounds.BelowCopy(0, 0);

            var list = ScrollableList.Add(Gui, currentBounds,
                Lang.Get("claims:conflict_list"),
                clientInfo.CityInfo.ClientConflictCellElements,
                (ClientConflictCellElement cell, ElementBounds bounds) => new GuiElementConflictCell(compo.Api, cell, bounds) { On = true },
                new ScrollableListOptions { Key = "conflicts", HeightReserve = 480 });

            // Who we may go to war with, and what stands in the way. The server owns the answer, and
            // an ally's war can start without any event addressed to us, so ask on every rebuild.
            claims.clientChannel.SendPacket(new SavedPlotsPacket { type = PacketsContentEnum.CLIENT_REQUEST_CASUS_BELLI });

            long now = TimeFunctions.getEpochSeconds();
            // Rows with nothing left to say are dropped: lapsed reason, no cooldown, no pact.
            var casusBelli = clientInfo.CityInfo.ClientCasusBelliCellElements
                .Where(cb => cb.Kind == CasusBelliKind.AllyAtWar || cb.ExpiresAt > now
                          || cb.CooldownUntil > now || cb.PactUntil > now || cb.UnionBreakUntil > now)
                .ToList();

            var cbAnchor = list.Inset.BelowCopy(0, 10);
            cbAnchor.fixedWidth = lineBounds.fixedWidth;

            var cbList = ScrollableList.Add(Gui, cbAnchor,
                Lang.Get("claims:gui_casus_belli_list"),
                casusBelli,
                (ClientCasusBelliCellElement cell, ElementBounds bounds) => new GuiElementCasusBelliCell(compo.Api, cell, bounds) { On = true },
                new ScrollableListOptions { Key = "casus-belli", HeightReserve = 620, TitleHeightShrink = 0 });

            Tooltip.Add(compo, Lang.Get("claims:gui_casus_belli_hint"), cbList.Title, "tip-casusbelli");

            /*==============================================================================================*/
            /*=====================================UNDER 2 LINE=============================================*/
            /*==============================================================================================*/
            NavRow.Build(Gui, currentBounds, lineBounds, 15,
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
        private ClientConflictCellElement SelectedConflict()
            => Player.CityInfo?.ClientConflictCellElements.FirstOrDefault(c => c.Guid == State.DialogArgs.Selected);

        /// <summary>
        /// The name our side fights under. An independent city has no alliance, so reading
        /// AllianceInfo.Name outright threw as soon as a lone mayor opened a conflict.
        /// </summary>
        private static string OurPartyName()
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

            var currentBounds = ctx.Current;
            currentBounds.WithAlignment(EnumDialogArea.LeftTop);
            currentBounds.fixedWidth = lineBounds.fixedWidth;
            currentBounds = currentBounds.BelowCopy(0, 0);

            ElementBounds createCityBounds = currentBounds.FlatCopy();
            ElementBounds invitationTextBounds = createCityBounds.BelowCopy();
            invitationTextBounds.WithAlignment(EnumDialogArea.CenterTop);

            var cell = SelectedConflict();

            // Party types belong in the heading: a war against a city is not a war against its alliance.
            compo.AddStaticText(string.Format("{0} ({1}) x {2} ({3})",
                                                cell.FirstPartyName, WarTargetTypeHelper.LangLabel(cell.FirstPartyType),
                                                cell.SecondPartyName, WarTargetTypeHelper.LangLabel(cell.SecondPartyType)),
                                            CairoFont.WhiteMediumText().WithOrientation(EnumTextOrientation.Center),
                                            invitationTextBounds);

            currentBounds = invitationTextBounds.BelowCopy();
            if (cell.ActiveWarTime)
            {
                compo.AddStaticText(Lang.Get("claims:gui_battle_active"),
                    CairoFont.WhiteSmallText().WithColor(new double[] { 0.95, 0.25, 0.25, 1.0 }),
                    currentBounds, "battleActive");
                currentBounds = currentBounds.BelowCopy();
            }

            compo.AddStaticText(Lang.Get("claims:gui_conflict_info_war_score") + " "
                    + cell.FirstScore + " : " + cell.SecondScore
                    + "   (" + Lang.Get("claims:gui_conflict_info_to_win", claims.config.WAR_SCORE_TO_WIN) + ")",
                CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Left),
                currentBounds, "warScore");

            currentBounds = currentBounds.BelowCopy();
            compo.AddStaticText(Lang.Get("claims:gui_conflict_info_started_by") + " " + cell.StartedByPartyName,
                CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Left),
                currentBounds, "startedBy");

            currentBounds = currentBounds.BelowCopy();
            compo.AddStaticText(Lang.Get("claims:gui_conflict_info_created") + " "
                    + TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(cell.TimeStampCreated, true),
                CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Left),
                currentBounds, "conflictCreated");

            currentBounds = currentBounds.BelowCopy();
            compo.AddStaticText(Lang.Get("claims:gui_conflict_info_pause_days") + " " + cell.MinimumDaysBetweenBattles,
                CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Left),
                currentBounds, "pauseDays");

            currentBounds = currentBounds.BelowCopy();
            compo.AddStaticText(Lang.Get("claims:gui_last_start_end_battle",
                TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(((DateTimeOffset)cell.LastBattleDateStart).ToUnixTimeSeconds()),
                TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(((DateTimeOffset)cell.LastBattleDateEnd).ToUnixTimeSeconds())),
                CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Left),
                currentBounds, "MinimumDaysBetweenBattles");

            currentBounds = currentBounds.BelowCopy();
            compo.AddStaticText(Lang.Get("claims:gui_next_start_end_battle",
                TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(((DateTimeOffset)cell.NextBattleDateStart).ToUnixTimeSeconds()),
                TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(((DateTimeOffset)cell.NextBattleDateEnd).ToUnixTimeSeconds())),
                CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Left),
                currentBounds, "TimeStampCreated");
            var p = currentBounds.BelowCopy().WithFixedSize(500, 30);

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
                HeightReserve = 300,
                Container = currentBounds,
                TitleHeightShrink = 0
            };
            ScrollableListLayout list;
            if (State.SelectedTabGroup == (int)EnumSelectedWarRangesTab.APPROVED)
            {
                list = ScrollableList.Add(gui, createCityBounds, null,
                    clientInfo.CityInfo.ClientWarRangeCellElements,
                    (ClientWarRangeCellElement c, ElementBounds bounds) => new GuiElementWarRangeCell(compo.Api, c, bounds, State.SelectedTabGroup != 0) { On = true },
                    listOpts);
            }
            else
            {
                list = ScrollableList.Add(gui, createCityBounds, null,
                    clientInfo.CityInfo.ClientTwoWarRangesCellElement,
                    (ClientTwoWarRangesCellElement c, ElementBounds bounds) => new GuiElementTwoWarRangesCell(compo.Api, c, bounds) { On = true },
                    listOpts);
            }

            currentBounds = list.Inset.BelowCopy(0, 10).WithFixedSize(25, 25);
            compo.AddIconButton("line", (bool t) =>
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

                    List<SelectedWarRange> selectedWarRanges = new List<SelectedWarRange>();
                    int? startIndex = null;
                    int? savedStartIndex = null;
                    DayOfWeek? startDay = null;
                    DayOfWeek? savedStartDay = null;
                    bool? lastCellState = null;
                    //try find start of range

                    for (int day = 0; day < 8; day++)
                    {
                        var warRange = slotsOf(day % 7);
                        for (int i = 0; i < 48; i++)
                        {
                            //find start of the range
                            if (warRange[i] && lastCellState.HasValue && !lastCellState.Value)
                            {
                                startIndex = i;
                                savedStartIndex = i;
                                startDay = (DayOfWeek)(day % 7);
                                savedStartDay = (DayOfWeek)(day % 7);
                                goto foundStart;
                            }
                            lastCellState = warRange[i];
                        }
                    }

                foundStart:
                    if (startIndex == null)
                    {
                        startIndex = 0;
                        startDay = DayOfWeek.Sunday;
                    }
                    bool firstStart = true;
                    for (int day = 0; day < 8; day++)
                    {
                        int dayIndex = ((int)startDay + day) % 7;
                        DayOfWeek itDay = dayOf(dayIndex);
                        bool[] itSlots = slotsOf(dayIndex);

                        for (int i = (startIndex.HasValue && firstStart) ? startIndex.Value : 0; i < 48; i++)
                        {
                            if (itDay == savedStartDay)
                            {
                                if (savedStartIndex != null && i == savedStartIndex - 1)
                                {
                                    if (startIndex != null)
                                    {
                                        int startDayNum = (int)startDay;
                                        int startMinutes = startDayNum * 24 * 60 + (startIndex ?? 0) * 30;
                                        int endMinutes = ((int)itDay) * 24 * 60 + i * 30;
                                        int diff = endMinutes - startMinutes;
                                        if (diff < 0)
                                        {
                                            diff += 7 * 24 * 60;
                                        }
                                        selectedWarRanges.Add(new SelectedWarRange((startDay ?? DayOfWeek.Sunday), itDay,
                                            new TimeSpan(hours: (i * 30) / 60, minutes: (i * 30) % 60, seconds: 0),
                                            TimeSpan.FromMinutes(diff), OurPartyGuid()));
                                    }
                                    goto searchedAll;
                                }
                            }
                            if (itSlots[i])
                            {
                                if (startIndex == null)
                                {
                                    startDay = itDay;
                                    startIndex = i;
                                }
                            }
                            else
                            {
                                if (startIndex != null)
                                {
                                    int startDayNum = (int)startDay;
                                    int startMinutes = startDayNum * 24 * 60 + (startIndex ?? 0) * 30;
                                    int endMinutes = ((int)itDay) * 24 * 60 + i * 30;
                                    int diff = endMinutes - startMinutes;
                                    if (diff < 0)
                                    {
                                        diff += 7 * 24 * 60;
                                    }
                                    selectedWarRanges.Add(new SelectedWarRange((startDay ?? DayOfWeek.Sunday), itDay,
                                        new TimeSpan(hours: ((startIndex ?? 0) * 30) / 60, minutes: ((startIndex ?? 0) * 30) % 60, seconds: 0),
                                        TimeSpan.FromMinutes(diff), OurPartyGuid()));
                                    startIndex = null;
                                    //startDay = null;
                                }
                            }
                            firstStart = false;
                        }
                    }

                searchedAll:
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
            }, currentBounds);

            compo.AddHoverText(Lang.Get("claims:gui-send-new-conflict-time"), CairoFont.WhiteDetailText(), 160, currentBounds);

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
