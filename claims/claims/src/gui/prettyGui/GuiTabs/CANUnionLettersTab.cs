using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.part.structure.conflict;
using claims.src.part.structure.union;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

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
            CenteredTitle(Lang.Get("claims:union_letters_list"), ColSection);
            var clientInfo = claims.clientDataStorage.clientPlayerInfo;


            if (claims.clientDataStorage.clientPlayerInfo.CityInfo == null) return;
            ImGui.BeginChild("UnionLettersScroll", new Vector2(0, 300), true);
            int i = 0;

            foreach (var letter in claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientUnionLetterCellElements)
            {
                ImGui.PushID(i++);
                ImGui.BeginGroup();

                string cellName = string.Format("{0} x {1}", letter.From, letter.To);
                ImGui.Text(cellName);

                // Accepting a Dissolve letter ends the union - never let it read like a union offer.
                bool dissolve = letter.Purpose == UnionLetterPurpose.Dissolve;
                ImGui.PushStyleColor(ImGuiCol.Text, dissolve ? ColWarning : ColValue);
                ImGui.Text(Lang.Get(dissolve ? "claims:gui_union_letter_dissolve" : "claims:gui_union_letter_form"));
                ImGui.PopStyleColor();

                string expDate = TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(letter.TimeStampExpire, true);
                ImGui.Text(expDate);
                
                if(letter.FromGuid != clientInfo.AllianceInfo?.Guid)
                {
                    if (GreenIconButton("acceptunion", "check-mark", 16))
                    {
                        SendCommand("/a union accept " + letter.From);

                        var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientUnionLetterCellElements.FirstOrDefault(c => c.From == letter.From);
                        if (cell != null)
                        {
                            toRemove.Add(cell);
                        }
                    }
                    ImGui.SameLine();
                }

                if (RedIconButton("denyunion", "convergence-target", 16))
                {
                    string targetName = letter.From;
                    if (letter.FromGuid == clientInfo.AllianceInfo?.Guid)
                    {
                        targetName = letter.To;
                    }
                    SendCommand("/a union decline " + letter.To);
                    var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientUnionLetterCellElements.FirstOrDefault(c => c.Guid == letter.Guid);
                    if (cell != null)
                    {
                        toRemove.Add(cell);
                    }
                }

                ImGui.EndGroup();
                ImGui.PopID();

                ImGui.Dummy(new Vector2(0, 8));
                ImGui.Separator();
            }
            ImGui.EndChild();
            if (toRemove.Count > 0)
            {
                foreach (var it in toRemove)
                    claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientUnionLetterCellElements.Remove(it);
                toRemove.Clear();
            }
            ImGui.SetCursorPosX(10);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);
            if (IconButton("newunion", "tower-flag", 32, Lang.Get("claims:gui-send-new-union-letter")))
                GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.ALLIANCE_SEND_NEW_UNION_LETTER_NEED_NAME;
            ImGui.SameLine();
            if (IconButton("leaveunion", "exit-door", 32, Lang.Get("claims:gui-send-leave-union")))
                GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.ALLIANCE_CANCEL_UNION_SELECT;
            /*==============================================================================================*/
            /*=====================================UNDER 2 LINE=============================================*/
            /*==============================================================================================*/

            AlignBottom();


            if (IconButton("allianceinfo", "vertical-banner", 60, Lang.Get("claims:gui-alliance")))
                GuiSys.selectedTab = EnumSelectedTab.AllianceInfoPage;
            ImGui.SameLine();
            if (IconButton("conflictletters", "envelope", 60, Lang.Get("claims:gui-conflict-letters")))
                GuiSys.selectedTab = EnumSelectedTab.ConflictLettersPage;
            ImGui.SameLine();
            if (IconButton("conflictspage", "frog-mouth-helm", 60, Lang.Get("claims:gui-conflicts-page")))
                GuiSys.selectedTab = EnumSelectedTab.ConflictsPage;
        }
    }
}
