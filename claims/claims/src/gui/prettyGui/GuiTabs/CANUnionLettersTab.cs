using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.part.structure.conflict;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using static System.Net.Mime.MediaTypeNames;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANUnionLettersTab : CANGuiTab
    {
        List<ClientUnionLetterCellElement> toRemove = new();
        public CANUnionLettersTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }
        public override void DrawTab()
        {
            string text = Lang.Get("claims:union_letters_list");
            float windowWidth = ImGui.GetWindowSize().X;
            float textWidth = ImGui.CalcTextSize(text).X;

            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            var clientInfo = claims.clientDataStorage.clientPlayerInfo;
            ImGui.Text(text);


            if (claims.clientDataStorage.clientPlayerInfo.CityInfo == null) return;
            ImGui.BeginChild("UnionLettersScroll", new Vector2(0, 300), true);
            int i = 0;

            foreach (var letter in claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientUnionLetterCellElements)
            {
                ImGui.PushID(i++);

                Vector2 start = ImGui.GetCursorScreenPos();
                float width = ImGui.GetContentRegionAvail().X;

                ImGui.BeginGroup();

                string cellName = string.Format("{0} x {1}", letter.From, letter.To);
                ImGui.Text(cellName);
                
                string expDate = TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(letter.TimeStampExpire, true);
                ImGui.Text(expDate);
                
                if(letter.FromGuid != clientInfo.AllianceInfo?.Guid)
                {
                    if (ImGui.ImageButton("acceptunion", this.iconHandler.GetOrLoadIcon("check-mark"), new Vector2(16)))
                    {
                        ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                       
                        clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/a union accept " + letter.From, EnumChatType.Macro, "");
                            
                        var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientUnionLetterCellElements.FirstOrDefault(c => c.From == letter.From);
                        if (cell != null)
                        {
                            toRemove.Add(cell);
                        }
                    }
                    ImGui.SameLine();
                }

                if (ImGui.ImageButton("denyunion", this.iconHandler.GetOrLoadIcon("convergence-target"), new Vector2(16)))
                {
                    string targetName = letter.From;
                    if (letter.FromGuid == clientInfo.AllianceInfo?.Guid)
                    {
                        targetName = letter.To;
                    }
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/a union decline " + letter.To, EnumChatType.Macro, "");
                    var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientUnionLetterCellElements.FirstOrDefault(c => c.Guid == letter.Guid);
                    if (cell != null)
                    {
                        toRemove.Add(cell);
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
            if(toRemove.Count() > 0)
            {
                foreach(var it in toRemove)
                claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientUnionLetterCellElements.Remove(it);
            }
            ImGui.SetCursorPosX(10);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);
            if (ImGui.ImageButton("newunion", this.iconHandler.GetOrLoadIcon("tower-flag"), new Vector2(32)))
            {
                GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.ALLIANCE_SEND_NEW_UNION_LETTER_NEED_NAME;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Lang.Get("claims:gui-send-new-union-letter"));
            }
            ImGui.SameLine();
            if (ImGui.ImageButton("leaveunion", this.iconHandler.GetOrLoadIcon("exit-door"), new Vector2(32)))
            {
                GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.ALLIANCE_CANCEL_UNION_SELECT;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Lang.Get("claims:gui-send-leave-union"));
            }
            /*==============================================================================================*/
            /*=====================================UNDER 2 LINE=============================================*/
            /*==============================================================================================*/

            float availY = ImGui.GetContentRegionAvail().Y;
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + availY - 80);


            if (ImGui.ImageButton("allianceinfo", this.iconHandler.GetOrLoadIcon("vertical-banner"), new Vector2(60)))
            {
                GuiSys.selectedTab = EnumSelectedTab.AllianceInfoPage;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Lang.Get("claims:gui-alliance"));
            }
            ImGui.SameLine();
            if (ImGui.ImageButton("conflictletters", this.iconHandler.GetOrLoadIcon("envelope"), new Vector2(60)))
            {
                GuiSys.selectedTab = EnumSelectedTab.ConflictLettersPage;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Lang.Get("claims:gui-conflict-letters"));
            }
            ImGui.SameLine();
            if (ImGui.ImageButton("conflictspage", this.iconHandler.GetOrLoadIcon("frog-mouth-helm"), new Vector2(60)))
            {
                GuiSys.selectedTab = EnumSelectedTab.ConflictsPage;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Lang.Get("claims:gui-conflicts-page"));
            }         
        }
    }
}
