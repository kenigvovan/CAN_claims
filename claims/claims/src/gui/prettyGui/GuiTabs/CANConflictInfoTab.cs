using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.network.packets;
using claims.src.part.structure.conflict;
using ImGuiNET;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANConflictInfoTab : CANGuiTab
    {
        public CANConflictInfoTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
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
        public override void DrawTab()
        {          
            var clientInfo = claims.clientDataStorage.clientPlayerInfo;
           
            var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientConflictCellElements.FirstOrDefault(c => c.Guid == capi.ModLoader.GetModSystem<claimsGui>().textInput);
            if (cell == null)
            {
                return;
            }
            string firstTypeLabel = cell.FirstPartyType == WarTargetType.Alliance
                ? Lang.Get("claims:conflict_target_alliance") : Lang.Get("claims:conflict_target_city");
            string secondTypeLabel = cell.SecondPartyType == WarTargetType.Alliance
                ? Lang.Get("claims:conflict_target_alliance") : Lang.Get("claims:conflict_target_city");
            ImGui.Text(string.Format("{0} ({1}) x {2} ({3})", cell.FirstPartyName, firstTypeLabel, cell.SecondPartyName, secondTypeLabel));

            if (cell.ActiveWarTime)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.2f, 0.2f, 1.0f));
                ImGui.Text(Lang.Get("claims:gui_battle_active"));
                ImGui.PopStyleColor();
            }

            ImGui.Text(Lang.Get("claims:gui_last_start_end_battle",
                TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(((DateTimeOffset)cell.LastBattleDateStart).ToUnixTimeSeconds()),
                TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(((DateTimeOffset)cell.LastBattleDateEnd).ToUnixTimeSeconds())));

            ImGui.Text(Lang.Get("claims:gui_next_start_end_battle",
                TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(((DateTimeOffset)cell.NextBattleDateStart).ToUnixTimeSeconds()),
                TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(((DateTimeOffset)cell.NextBattleDateEnd).ToUnixTimeSeconds())));

            if (ImGui.BeginTabBar("MyTabs"))
            {
                if (ImGui.BeginTabItem("Selected"))
                {
                    if (capi.ModLoader.GetModSystem<claimsGui>().selectedWarrangeTab != 0)
                    {
                        FillWarRangeArrays(cell.WarRanges);
                        capi.ModLoader.GetModSystem<claimsGui>().selectedWarrangeTab = 0;
                    }

                    ImGui.BeginChild("InvitesScroll", new Vector2(0, 300), true);
                    int ip = 0;
                    foreach (var warRange in claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientWarRangeCellElements)
                    {
                        ImGui.PushID(ip);

                        Vector2 start = ImGui.GetCursorScreenPos();
                        float width = ImGui.GetContentRegionAvail().X;

                        ImGui.BeginGroup();
                        ImGui.Text(((DayOfWeek)(ip)).ToString());
                        ip++;
                        for (int j = 0; j < 3; j++)
                        {
                            for (int i = 0; i < 16; i++)
                            {
                                int p = i + j * 16;

                                
                                bool value = warRange.WarRangeArray[p];

                                // меняем цвет если включено
                                if (value)
                                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.7f, 0.3f, 1.0f));

                                if (ImGui.Button("##btn" + p.ToString(), new Vector2(16, 16)))
                                {
                                    warRange.WarRangeArray[p] = !warRange.WarRangeArray[p];
                                }

                                if (value)
                                    ImGui.PopStyleColor();

                                // hover tooltip (аналог GuiElementHoverText)
                                if (ImGui.IsItemHovered())
                                {
                                    int h1 = (int)Math.Floor(p * 0.5f);
                                    int m1 = (int)((p * 0.5f - h1) * 60);
                                    int h2 = (int)Math.Floor((p + 1) * 0.5f);
                                    int m2 = (int)(((p + 1) * 0.5f - h2) * 60);

                                    ImGui.BeginTooltip();
                                    ImGui.Text(string.Format("{0:00}:{1:00} - {2:00}:{3:00}", h1, m1, h2, m2));
                                    ImGui.EndTooltip();
                                }

                                if (i < 15)
                                    ImGui.SameLine();
                            }
                        }

                        ImGui.EndGroup();

                        Vector2 end = ImGui.GetItemRectMax();
                        var draw = ImGui.GetWindowDrawList();

                        ImGui.PopID();

                        ImGui.Dummy(new Vector2(0, 8));
                        ImGui.Separator();
                    }
                    ImGui.EndChild();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Suggested"))
                {
                    if (capi.ModLoader.GetModSystem<claimsGui>().selectedWarrangeTab != 1)
                    {
                        string ourName = claims.clientDataStorage.clientPlayerInfo.AllianceInfo?.Name
                            ?? claims.clientDataStorage.clientPlayerInfo.CityInfo?.Name ?? "";
                        if (ourName.Equals(cell.FirstPartyName))
                        {
                            FillTwoWarRangesArrays(cell.FirstWarRanges, cell.SecondWarRanges);
                        }
                        else
                        {
                            FillTwoWarRangesArrays(cell.SecondWarRanges, cell.FirstWarRanges);
                        }
                        capi.ModLoader.GetModSystem<claimsGui>().selectedWarrangeTab = 1;
                    }




                    ImGui.BeginChild("InvitesScroll", new Vector2(0, 300), true);
                    int ip = 0;
                    // claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientTwoWarRangesCellElement
                    for (int day = 0; day < 7; day++)
                    {
                        ImGui.PushID(ip);

                        Vector2 start = ImGui.GetCursorScreenPos();
                        float width = ImGui.GetContentRegionAvail().X;

                        ImGui.BeginGroup();
                        ImGui.Text(((DayOfWeek)(ip)).ToString());
                        ip++;
                        ImGui.Text("Enemy's:");
                        var warRange = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientTwoWarRangesCellElement[day].EnemyWarRangeArray;
                        for (int j = 0; j < 3; j++)
                        {
                            for (int i = 0; i < 16; i++)
                            {
                                int p = i + j * 16;


                                bool value = warRange[p];

                                // меняем цвет если включено
                                if (value)
                                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.7f, 0.3f, 1.0f));

                                if (ImGui.Button("##btn" + p.ToString(), new Vector2(16, 16)))
                                {
                                    //warRange[p] = !warRange[p];
                                }

                                if (value)
                                    ImGui.PopStyleColor();

                                // hover tooltip (аналог GuiElementHoverText)
                                if (ImGui.IsItemHovered())
                                {
                                    int h1 = (int)Math.Floor(p * 0.5f);
                                    int m1 = (int)((p * 0.5f - h1) * 60);
                                    int h2 = (int)Math.Floor((p + 1) * 0.5f);
                                    int m2 = (int)(((p + 1) * 0.5f - h2) * 60);

                                    ImGui.BeginTooltip();
                                    ImGui.Text(string.Format("{0:00}:{1:00} - {2:00}:{3:00}", h1, m1, h2, m2));
                                    ImGui.EndTooltip();
                                }

                                if (i < 15)
                                    ImGui.SameLine();
                            }
                        }
                        ImGui.Separator();
                        ImGui.Text("Our:");
                        var warRangeOur = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientTwoWarRangesCellElement[day].OurWarRangeArray;
                        for (int j = 0; j < 3; j++)
                        {
                            for (int i = 0; i < 16; i++)
                            {
                                int p = i + j * 16;


                                bool value = warRangeOur[p];

                                // меняем цвет если включено
                                if (value)
                                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.7f, 0.3f, 1.0f));

                                if (ImGui.Button("##btnour" + p.ToString(), new Vector2(16, 16)))
                                {
                                    claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientTwoWarRangesCellElement[day].OurWarRangeArray[p] = !warRangeOur[p];
                                }

                                if (value)
                                    ImGui.PopStyleColor();

                                // hover tooltip (аналог GuiElementHoverText)
                                if (ImGui.IsItemHovered())
                                {
                                    int h1 = (int)Math.Floor(p * 0.5f);
                                    int m1 = (int)((p * 0.5f - h1) * 60);
                                    int h2 = (int)Math.Floor((p + 1) * 0.5f);
                                    int m2 = (int)(((p + 1) * 0.5f - h2) * 60);

                                    ImGui.BeginTooltip();
                                    ImGui.Text(string.Format("{0:00}:{1:00} - {2:00}:{3:00}", h1, m1, h2, m2));
                                    ImGui.EndTooltip();
                                }

                                if (i < 15)
                                    ImGui.SameLine();
                            }
                        }
                        ImGui.EndGroup();

                        Vector2 end = ImGui.GetItemRectMax();
                        var draw = ImGui.GetWindowDrawList();

                        ImGui.PopID();

                        ImGui.Dummy(new Vector2(0, 8));
                        ImGui.Separator();
                        ImGui.Separator();
                    }
                    ImGui.EndChild();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            if (ImGui.ImageButton("sendinfo", this.iconHandler.GetOrLoadIcon("info"), new Vector2(20)))
            {
                if (cell == null)
                {
                    return;
                }

                string ourName = claims.clientDataStorage.clientPlayerInfo.AllianceInfo?.Name
                    ?? claims.clientDataStorage.clientPlayerInfo.CityInfo?.Name ?? "";
                string ourPartyGuid = ourName.Equals(cell.FirstPartyName) ? cell.FirstPartyGuid : cell.SecondPartyGuid;

                int activeTab = capi.ModLoader.GetModSystem<claimsGui>().selectedWarrangeTab;
                bool[][] warRangesByDay = new bool[7][];
                for (int d = 0; d < 7; d++)
                {
                    if (activeTab == 0)
                    {
                        warRangesByDay[d] = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientWarRangeCellElements[d].WarRangeArray;
                    }
                    else
                    {
                        warRangesByDay[d] = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientTwoWarRangesCellElement[d].OurWarRangeArray;
                    }
                }

                List<SelectedWarRange> selectedWarRanges = new List<SelectedWarRange>();
                int? startIndex = null;
                int? savedStartIndex = null;
                DayOfWeek? startDay = null;
                DayOfWeek? savedStartDay = null;
                bool? lastCellState = null;
                //try find start of range

                for (int day = 0; day < 8; day++)
                {
                    var warRange = warRangesByDay[day % 7];
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
                    return;
                }
                bool firstStart = true;
                for (int day = 0; day < 8; day++)
                {
                    int dayIndex = ((int)startDay + day) % 7;
                    var currentWarRange = warRangesByDay[dayIndex];
                    DayOfWeek currentDayOfWeek = (DayOfWeek)dayIndex;

                    for (int i = (startIndex.HasValue && firstStart) ? startIndex.Value : 0; i < 48; i++)
                    {
                        if (currentDayOfWeek == savedStartDay)
                        {
                            if (savedStartIndex != null && i == savedStartIndex - 1)
                            {
                                if (startIndex != null)
                                {
                                    int startDayNum = (int)startDay;
                                    int startMinutes = startDayNum * 24 * 60 + (startIndex ?? 0) * 30;
                                    int endMinutes = (int)currentDayOfWeek * 24 * 60 + i * 30;
                                    int diff = endMinutes - startMinutes;
                                    if (diff < 0)
                                    {
                                        diff += 7 * 24 * 60;
                                    }
                                    selectedWarRanges.Add(new SelectedWarRange((startDay ?? DayOfWeek.Sunday), currentDayOfWeek,
                                        new TimeSpan(hours: ((startIndex ?? 0) * 30) / 60, minutes: ((startIndex ?? 0) * 30) % 60, seconds: 0),
                                        TimeSpan.FromMinutes(diff), ourPartyGuid));
                                }
                                goto searchedAll;
                            }
                        }
                        if (currentWarRange[i])
                        {
                            if (startIndex == null)
                            {
                                startDay = currentDayOfWeek;
                                startIndex = i;
                            }
                        }
                        else
                        {
                            if (startIndex != null)
                            {
                                int startDayNum = (int)startDay;
                                int startMinutes = startDayNum * 24 * 60 + (startIndex ?? 0) * 30;
                                int endMinutes = (int)currentDayOfWeek * 24 * 60 + i * 30;
                                int diff = endMinutes - startMinutes;
                                if (diff < 0)
                                {
                                    diff += 7 * 24 * 60;
                                }
                                selectedWarRanges.Add(new SelectedWarRange((startDay ?? DayOfWeek.Sunday), currentDayOfWeek,
                                    new TimeSpan(hours: ((startIndex ?? 0) * 30) / 60, minutes: ((startIndex ?? 0) * 30) % 60, seconds: 0),
                                    TimeSpan.FromMinutes(diff), ourPartyGuid));
                                startIndex = null;
                            }
                        }
                        firstStart = false;
                    }
                }

            searchedAll:
                if (cell.FirstPartyName.Equals(ourName))
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
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Lang.Get("claims:gui-send-new-conflict-time"));
            }


            /*==============================================================================================*/
            /*=====================================UNDER 2 LINE=============================================*/
            /*==============================================================================================*/
            float availY = ImGui.GetContentRegionAvail().Y;
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + availY - 80);


            if (ImGui.ImageButton("allianceinfo", this.iconHandler.GetOrLoadIcon("vertical-banner"), new Vector2(60)))
            {
                capi.ModLoader.GetModSystem<claimsGui>().selectedTab = capi.ModLoader.GetModSystem<claimsGui>().conflictSourceTab;
            }
            ImGui.SameLine();
            if (ImGui.ImageButton("conflictletters", this.iconHandler.GetOrLoadIcon("envelope"), new Vector2(60)))
            {
                capi.ModLoader.GetModSystem<claimsGui>().selectedTab = EnumSelectedTab.ConflictLettersPage;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Lang.Get("claims:gui-conflict-letters"));
            }

        }
    }
}
