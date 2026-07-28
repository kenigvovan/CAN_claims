using claims.src.gui.playerGui.structures.cellElements;
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
    public class CANPlotsGroupInvitesTab : CANGuiTab
    {
        List<ClientToPlotsGroupInvitation> toRemove = new();
        public CANPlotsGroupInvitesTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }
        public override void DrawTab()
        {
            if (BackButton())
                GuiSys.selectedTab = EnumSelectedTab.PlotsGroup;

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (claims.clientDataStorage.clientPlayerInfo.ReceivedPlotsGroupInvitations.Count > 0)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ColSection);
                ImGui.Text(Lang.Get("claims:gui-invites-list-title"));
                ImGui.PopStyleColor();

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.BeginChild("InvitesScroll", new Vector2(0, 300), true);
                int i = 0;
                foreach (var invite in claims.clientDataStorage.clientPlayerInfo.ReceivedPlotsGroupInvitations)
                {
                    ImGui.PushID(i);
                    ImGui.BeginGroup();

                    ImGui.PushStyleColor(ImGuiCol.Text, ColValue);
                    ImGui.Text(invite.PlotsGroupName);
                    ImGui.PopStyleColor();

                    ImGui.SameLine();
                    Label("  " + invite.CityName);

                    if (GreenIconButton("acceptplotsgroup", "expander", 16, Lang.Get("claims:gui-plotsgroup-accept-tooltip")))
                    {
                        SendCommand("/plotsgroupaccept " + invite.CityName + " " + invite.PlotsGroupName);
                        var cell = claims.clientDataStorage.clientPlayerInfo.ReceivedPlotsGroupInvitations.FirstOrDefault(c => c.CityName == invite.CityName && c.PlotsGroupName == invite.PlotsGroupName);
                        if (cell != null)
                        {
                            toRemove.Add(cell);
                        }
                    }

                    ImGui.SameLine();
                    if (RedIconButton("declineplotsgroup", "contract", 16, Lang.Get("claims:gui-plotsgroup-decline-tooltip")))
                    {
                        SendCommand("/plotsgroupdeny " + invite.CityName + " " + invite.PlotsGroupName);
                        var cell = claims.clientDataStorage.clientPlayerInfo.ReceivedPlotsGroupInvitations.FirstOrDefault(c => c.CityName == invite.CityName && c.PlotsGroupName == invite.PlotsGroupName);
                        if (cell != null)
                        {
                            toRemove.Add(cell);
                        }
                    }

                    ImGui.EndGroup();
                    ImGui.PopID();

                    ImGui.Dummy(new Vector2(0, 4));
                    ImGui.Separator();
                    ImGui.Dummy(new Vector2(0, 4));
                    i++;
                }
                ImGui.EndChild();
                if (toRemove.Count > 0)
                {
                    foreach (var it in toRemove)
                        claims.clientDataStorage.clientPlayerInfo.ReceivedPlotsGroupInvitations.Remove(it);
                    toRemove.Clear();
                }
            }
            else
            {
                Label(Lang.Get("claims:gui-no-plotsgroup-invites"));
            }
        }
    }
}
