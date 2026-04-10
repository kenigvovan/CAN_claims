using System;
using System.Collections.Generic;
using System.Linq;
using claims.src.auxialiry;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.network.packets;
using claims.src.part.structure.conflict;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using static claims.src.gui.playerGui.CANClaimsGui;

namespace claims.src.gui.playerGui.GuiPages
{
    public static class ConflictPage
    {
        public static void BuildConflictLettersPage(CANClaimsGui gui, ElementBounds currentBounds, ElementBounds lineBounds)
        {
            var compo = gui.SingleComposer;
            var criminalsTabFont = CairoFont.ButtonText().WithFontSize(20).WithOrientation(EnumTextOrientation.Left);
            currentBounds.fixedWidth /= 2;
            currentBounds.WithAlignment(EnumDialogArea.LeftTop);

            var clientInfo = claims.clientDataStorage.clientPlayerInfo;
            currentBounds.fixedWidth = lineBounds.fixedWidth;

            currentBounds = currentBounds.BelowCopy(0, 0);
            ElementBounds createCityBounds = currentBounds.FlatCopy();
            int numClaimsToSkip = gui.selectedClaimsPage * gui.claimsPerPage;
            ElementBounds topTextBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding, 40, createCityBounds.fixedWidth - 30, 30);

            ElementBounds logtextBounds = ElementBounds.Fixed(0, 0, createCityBounds.fixedWidth - 30, gui.mainBounds.fixedHeight - 350).FixedUnder(topTextBounds, 5);
            ElementBounds invitationTextBounds = createCityBounds.BelowCopy();

            invitationTextBounds.fixedHeight -= 50;
            invitationTextBounds.WithAlignment(EnumDialogArea.CenterTop);
            ElementBounds clippingBounds = logtextBounds.ForkBoundingParent();

            ElementBounds insetBounds = logtextBounds.FlatCopy().FixedGrow(6).WithFixedOffset(-3, -3);

            ElementBounds scrollbarBounds = insetBounds.CopyOffsetedSibling(logtextBounds.fixedWidth + 7).WithFixedWidth(20);

            compo.AddStaticText(Lang.Get("claims:conflict_letters_list"),
                CairoFont.WhiteMediumText().WithOrientation(EnumTextOrientation.Center),
                invitationTextBounds);

            gui.clippingRansksBounds = insetBounds.ForkContainingChild(3.0, 3.0, 3.0, 3.0);

            compo.BeginChildElements(invitationTextBounds)
                .BeginClip(clippingBounds)
                .AddInset(insetBounds, 3)
                .AddCellList(gui.listRanksBounds = gui.clippingRansksBounds.
                ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(5.0),
                (ClientConflictLetterCellElement cell, ElementBounds bounds) =>
                {
                    return new GuiElementConflictLetterCell(compo.Api, cell, bounds)
                    {
                        On = true
                    };
                },
                claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientConflictLetterCellElements, "conflictletterscells")
                .EndClip()
                .AddVerticalScrollbar((float value) =>
                {
                    ElementBounds bounds = compo.GetCellList<ClientConflictLetterCellElement>("conflictletterscells").Bounds;
                    bounds.fixedY = (double)(0f - value);
                    bounds.CalcWorldBounds();
                }, scrollbarBounds, "scrollbar")
                .EndChildElements();
            compo.GetCellList<ClientConflictLetterCellElement>("conflictletterscells").BeforeCalcBounds();

            ElementBounds addPlotsGroupBounds = insetBounds.BelowCopy(15, 15);
            addPlotsGroupBounds.WithFixedWidth(25).WithFixedHeight(25);
            compo.AddInset(addPlotsGroupBounds);
            compo.AddIconButton("claims:sword-brandish", (bool t) =>
            {
                if (t)
                {
                    gui.CreateNewCityState = EnumUpperWindowSelectedState.ALLIANCE_SEND_NEW_CONFLICT_LETTER_NEED_NAME;
                    gui.BuildUpperWindow();
                }
            }, addPlotsGroupBounds);
            compo.AddHoverText("Send a new conflict letter",
                                            CairoFont.SmallButtonText(),
                                            (int)currentBounds.fixedWidth / 2, addPlotsGroupBounds);

            /*==============================================================================================*/
            /*=====================================UNDER 2 LINE=============================================*/
            /*==============================================================================================*/
            var line2Bounds = currentBounds.BelowCopy(0, 20).WithFixedHeight(5).WithFixedWidth(lineBounds.fixedWidth);
            line2Bounds.fixedX = 0;
            line2Bounds.fixedY = gui.mainBounds.fixedHeight * 0.85;
            compo.AddInset(line2Bounds);

            ElementBounds nextIconBounds = line2Bounds.BelowCopy().WithFixedSize(48, 48).WithAlignment(EnumDialogArea.LeftTop);
            nextIconBounds.fixedX = 15;
            nextIconBounds.fixedY = gui.mainBounds.fixedHeight * 0.90;

            compo.AddIconButton("claims:vertical-banner", new Action<bool>((b) =>
            {
                gui.SelectedTab = EnumSelectedTab.AllianceInfoPage;
                gui.BuildMainWindow();
                return;
            }), nextIconBounds);
            nextIconBounds = nextIconBounds.RightCopy(20);

            compo.AddIconButton("claims:frog-mouth-helm", (bool t) =>
            {
                if (t)
                {
                    gui.SelectedTab = EnumSelectedTab.ConflictsPage;
                    gui.BuildMainWindow();
                }
            }, nextIconBounds);

            compo.Compose();

            compo.GetScrollbar("scrollbar").SetHeights((float)gui.clippingRansksBounds.fixedHeight, (float)gui.listRanksBounds.fixedHeight);
        }
        public static void BuildConflictsPage(CANClaimsGui gui, ElementBounds currentBounds, ElementBounds lineBounds)
        {
            var compo = gui.SingleComposer;
            var criminalsTabFont = CairoFont.ButtonText().WithFontSize(20).WithOrientation(EnumTextOrientation.Left);
            currentBounds.fixedWidth /= 2;
            currentBounds.WithAlignment(EnumDialogArea.LeftTop);

            var clientInfo = claims.clientDataStorage.clientPlayerInfo;
            currentBounds.fixedWidth = lineBounds.fixedWidth;

            currentBounds = currentBounds.BelowCopy(0, 0);
            ElementBounds createCityBounds = currentBounds.FlatCopy();
            int numClaimsToSkip = gui.selectedClaimsPage * gui.claimsPerPage;
            ElementBounds topTextBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding, 40, createCityBounds.fixedWidth - 30, 30);

            ElementBounds logtextBounds = ElementBounds.Fixed(0, 0, createCityBounds.fixedWidth - 30, gui.mainBounds.fixedHeight - 350).FixedUnder(topTextBounds, 5);
            ElementBounds invitationTextBounds = createCityBounds.BelowCopy();

            invitationTextBounds.fixedHeight -= 50;
            invitationTextBounds.WithAlignment(EnumDialogArea.CenterTop);
            ElementBounds clippingBounds = logtextBounds.ForkBoundingParent();

            ElementBounds insetBounds = logtextBounds.FlatCopy().FixedGrow(6).WithFixedOffset(-3, -3);

            ElementBounds scrollbarBounds = insetBounds.CopyOffsetedSibling(logtextBounds.fixedWidth + 7).WithFixedWidth(20);

            compo.AddStaticText(Lang.Get("claims:conflict_list"),
                CairoFont.WhiteMediumText().WithOrientation(EnumTextOrientation.Center),
                invitationTextBounds);

            gui.clippingRansksBounds = insetBounds.ForkContainingChild(3.0, 3.0, 3.0, 3.0);

            compo.BeginChildElements(invitationTextBounds)
                .BeginClip(clippingBounds)
                .AddInset(insetBounds, 3)
                .AddCellList(gui.listRanksBounds = gui.clippingRansksBounds.
                ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(5.0),
                (ClientConflictCellElement cell, ElementBounds bounds) =>
                {
                    return new GuiElementConflictCell(compo.Api, cell, bounds)
                    {
                        On = true
                    };
                },
                claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientConflictCellElements, "conflicscells")
                .EndClip()
                .AddVerticalScrollbar((float value) =>
                {
                    ElementBounds bounds = compo.GetCellList<ClientConflictCellElement>("conflicscells").Bounds;
                    bounds.fixedY = (double)(0f - value);
                    bounds.CalcWorldBounds();
                }, scrollbarBounds, "scrollbar")
                .EndChildElements();
            compo.GetCellList<ClientConflictCellElement>("conflicscells").BeforeCalcBounds();

            /*==============================================================================================*/
            /*=====================================UNDER 2 LINE=============================================*/
            /*==============================================================================================*/
            var line2Bounds = currentBounds.BelowCopy(0, 20).WithFixedHeight(5).WithFixedWidth(lineBounds.fixedWidth);
            line2Bounds.fixedX = 0;
            line2Bounds.fixedY = gui.mainBounds.fixedHeight * 0.85;
            compo.AddInset(line2Bounds);

            ElementBounds nextIconBounds = line2Bounds.BelowCopy().WithFixedSize(48, 48).WithAlignment(EnumDialogArea.LeftTop);
            nextIconBounds.fixedX = 15;
            nextIconBounds.fixedY = gui.mainBounds.fixedHeight * 0.90;

            compo.AddIconButton("claims:vertical-banner", new Action<bool>((b) =>
            {
                gui.SelectedTab = EnumSelectedTab.AllianceInfoPage;
                gui.BuildMainWindow();
                return;
            }), nextIconBounds);
            nextIconBounds = nextIconBounds.RightCopy(20);

            compo.AddIconButton("claims:envelope", (bool t) =>
            {
                if (t)
                {
                    gui.SelectedTab = EnumSelectedTab.ConflictLettersPage;
                    gui.BuildMainWindow();
                }
            }, nextIconBounds);

            compo.Compose();

            compo.GetScrollbar("scrollbar").SetHeights((float)gui.clippingRansksBounds.fixedHeight, (float)gui.listRanksBounds.fixedHeight);
        }
        public static void BuildConflictInfoPage(CANClaimsGui gui, ElementBounds currentBounds, ElementBounds lineBounds)
        {
            var compo = gui.SingleComposer;
            var criminalsTabFont = CairoFont.ButtonText().WithFontSize(20).WithOrientation(EnumTextOrientation.Left);
            currentBounds.fixedWidth /= 2;
            currentBounds.WithAlignment(EnumDialogArea.LeftTop);

            var clientInfo = claims.clientDataStorage.clientPlayerInfo;
            currentBounds.fixedWidth = lineBounds.fixedWidth;

            currentBounds = currentBounds.BelowCopy(0, 0);
            ElementBounds createCityBounds = currentBounds.FlatCopy();
            int numClaimsToSkip = gui.selectedClaimsPage * gui.claimsPerPage;
            ElementBounds topTextBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding, 40, createCityBounds.fixedWidth - 30, 30);

            ElementBounds logtextBounds = ElementBounds.Fixed(0, 0, createCityBounds.fixedWidth - 30, gui.mainBounds.fixedHeight - 300).FixedUnder(topTextBounds, 5);
            ElementBounds invitationTextBounds = createCityBounds.BelowCopy();

            invitationTextBounds.WithAlignment(EnumDialogArea.CenterTop);
            ElementBounds clippingBounds = logtextBounds.ForkBoundingParent();
            //clippingBounds.fixedY += 20;
            ElementBounds insetBounds = logtextBounds.FlatCopy().FixedGrow(6).WithFixedOffset(-3, -3);
            //insetBounds.fixedY += 40;
            ElementBounds scrollbarBounds = insetBounds.CopyOffsetedSibling(logtextBounds.fixedWidth + 7).WithFixedWidth(20);
            //compo.AddInset(createCityBounds);
            //compo.AddInset(logtextBounds);
            var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientConflictCellElements.FirstOrDefault(c => c.Guid == gui.selectedString);
            if (cell == null)
            {
                compo.Compose();
                return;
            }

            compo.AddStaticText(string.Format("{0} x {1}", cell.FirstPartyName, cell.SecondPartyName),
                                            CairoFont.WhiteMediumText().WithOrientation(EnumTextOrientation.Center),
                                            invitationTextBounds);
            currentBounds = invitationTextBounds.BelowCopy();
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
            horizontalTabs[0].Name = "Selected";
            horizontalTabs[0].DataInt = 0;

            horizontalTabs[1] = new GuiTab();
            horizontalTabs[1].Name = "Suggested";
            horizontalTabs[1].DataInt = 1;
            compo.AddHorizontalTabs(horizontalTabs, p, (int value) =>
            {
                var tabs = compo.GetHorizontalTabs("groupTabs");
                if (tabs != null)
                {
                    if (tabs.activeElement != value)
                    {
                        tabs.activeElement = value;
                        gui.SelectedTabGroup = value;
                        var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientConflictCellElements.FirstOrDefault(c => c.Guid == gui.selectedString);
                        if (cell == null)
                        {
                            return;
                        }
                        if (value == (int)EnumSelectedWarRangesTab.APPROVED)
                        {
                            FillWarRangeArrays(cell.WarRanges);
                        }
                        else
                        {
                            if (claims.clientDataStorage.clientPlayerInfo.AllianceInfo.Name.Equals(cell.FirstPartyName))
                            {
                                FillTwoWarRangesArrays(cell.FirstWarRanges, cell.SecondWarRanges);
                            }
                            else
                            {
                                FillTwoWarRangesArrays(cell.SecondWarRanges, cell.FirstWarRanges);
                            }
                        }
                        gui.BuildMainWindow();
                    }
                }
            }, CairoFont.WhiteSmallText(), CairoFont.WhiteSmallText(), "groupTabs");
            compo.GetHorizontalTabs("groupTabs").activeElement = gui.SelectedTabGroup;
            gui.clippingRansksBounds = insetBounds.ForkContainingChild(3.0, 3.0, 3.0, 3.0);
            if (gui.SelectedTabGroup == (int)EnumSelectedWarRangesTab.APPROVED)
            {
                compo.BeginChildElements(currentBounds)
                    .BeginClip(clippingBounds)
                    .AddInset(insetBounds, 3)
                    .AddCellList(gui.listRanksBounds = gui.clippingRansksBounds.
                    ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(5.0),
                    (ClientWarRangeCellElement cell, ElementBounds bounds) =>
                    {
                        return new GuiElementWarRangeCell(compo.Api, cell, bounds, gui.SelectedTabGroup != 0)
                        {
                            On = true
                        };
                    },
                    claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientWarRangeCellElements, "conflicscells")
                    .EndClip()
                    .AddVerticalScrollbar((float value) =>
                    {
                        ElementBounds bounds = compo.GetCellList<ClientWarRangeCellElement>("conflicscells").Bounds;
                        bounds.fixedY = (double)(0f - value);
                        bounds.CalcWorldBounds();
                    }, scrollbarBounds, "scrollbar")
                    .EndChildElements();
                compo.GetCellList<ClientWarRangeCellElement>("conflicscells").BeforeCalcBounds();
            }
            else
            {
                compo.BeginChildElements(currentBounds)
                   .BeginClip(clippingBounds)
                   .AddInset(insetBounds, 3)
                   .AddCellList(gui.listRanksBounds = gui.clippingRansksBounds.
                   ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(5.0),
                   (ClientTwoWarRangesCellElement cell, ElementBounds bounds) =>
                   {
                       return new GuiElementTwoWarRangesCell(compo.Api, cell, bounds)
                       {
                           On = true
                       };
                   },
                   claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientTwoWarRangesCellElement, "conflicscells")
                   .EndClip()
                   .AddVerticalScrollbar((float value) =>
                   {
                       ElementBounds bounds = compo.GetCellList<ClientTwoWarRangesCellElement>("conflicscells").Bounds;
                       bounds.fixedY = (double)(0f - value);
                       bounds.CalcWorldBounds();
                   }, scrollbarBounds, "scrollbar")
                   .EndChildElements();
                compo.GetCellList<ClientTwoWarRangesCellElement>("conflicscells").BeforeCalcBounds();
            }

            currentBounds = insetBounds.BelowCopy(0, 10).WithFixedSize(25, 25);
            compo.AddIconButton("line", (bool t) =>
            {
                if (t)
                {
                    var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientConflictCellElements.FirstOrDefault(c => c.Guid == gui.selectedString);
                    if (cell == null)
                    {
                        compo.Compose();
                        return;
                    }

                    List<SelectedWarRange> selectedWarRanges = new List<SelectedWarRange>();
                    int? startIndex = null;
                    int? savedStartIndex = null;
                    DayOfWeek? startDay = null;
                    DayOfWeek? savedStartDay = null;
                    bool? lastCellState = null;
                    bool firstGo = true;
                    //try find start of range

                    for (int day = 0; day < 8; day++)
                    {
                        var currDay = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientTwoWarRangesCellElement[day % 7];
                        var warRange = currDay.OurWarRangeArray;
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
                        ClientTwoWarRangesCellElement it = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientTwoWarRangesCellElement[((int)startDay + day) % 7];

                        for (int i = (startIndex.HasValue && firstStart) ? startIndex.Value : 0; i < 48; i++)
                        {
                            if (it.DayOfWeek == savedStartDay)
                            {
                                if (savedStartIndex != null && i == savedStartIndex - 1)
                                {
                                    if (startIndex != null)
                                    {
                                        int startDayNum = (int)startDay;
                                        int startMinutes = startDayNum * 24 * 60 + (startIndex ?? 0) * 30;
                                        int endMinutes = ((int)it.DayOfWeek) * 24 * 60 + i * 30;
                                        int diff = endMinutes - startMinutes;
                                        if (diff < 0)
                                        {
                                            diff += 7 * 24 * 60;
                                        }
                                        selectedWarRanges.Add(new SelectedWarRange((startDay ?? DayOfWeek.Sunday), it.DayOfWeek,
                                            new TimeSpan(hours: (i * 30) / 60, minutes: (i * 30) % 60, seconds: 0),
                                            TimeSpan.FromMinutes(diff), claims.clientDataStorage.clientPlayerInfo.AllianceInfo.Guid));
                                    }
                                    goto searchedAll;
                                }
                            }
                            if (it.OurWarRangeArray[i])
                            {
                                if (startIndex == null)
                                {
                                    startDay = it.DayOfWeek;
                                    startIndex = i;
                                }
                            }
                            else
                            {
                                if (startIndex != null)
                                {
                                    int startDayNum = (int)startDay;
                                    int startMinutes = startDayNum * 24 * 60 + (startIndex ?? 0) * 30;
                                    int endMinutes = ((int)it.DayOfWeek) * 24 * 60 + i * 30;
                                    int diff = endMinutes - startMinutes;
                                    if (diff < 0)
                                    {
                                        diff += 7 * 24 * 60;
                                    }
                                    selectedWarRanges.Add(new SelectedWarRange((startDay ?? DayOfWeek.Sunday), it.DayOfWeek,
                                        new TimeSpan(hours: ((startIndex ?? 0) * 30) / 60, minutes: ((startIndex ?? 0) * 30) % 60, seconds: 0),
                                        TimeSpan.FromMinutes(diff), claims.clientDataStorage.clientPlayerInfo.AllianceInfo.Guid));
                                    startIndex = null;
                                    //startDay = null;
                                }
                            }
                            firstStart = false;
                        }
                    }

                searchedAll:
                    if (cell.FirstPartyName.Equals(claims.clientDataStorage.clientPlayerInfo.AllianceInfo.Name))
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

            compo.AddHoverText("Send updated times.", CairoFont.WhiteDetailText(), 60, currentBounds);

            compo.Compose();

            compo.GetScrollbar("scrollbar").SetHeights((float)gui.clippingRansksBounds.fixedHeight, (float)gui.listRanksBounds.fixedHeight);

            compo.Compose();

        }
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
