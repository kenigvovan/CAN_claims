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
            Vector4 titleColor = new Vector4(0.4f, 0.7f, 1.0f, 1.0f);
            Vector4 groupNameColor = new Vector4(1.0f, 0.85f, 0.3f, 1.0f);
            Vector4 labelColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);

            if (ImGui.Button(Lang.Get("claims:gui-back")))
            {
                GuiSys.selectedTab = EnumSelectedTab.PlotsGroup;
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (claims.clientDataStorage.clientPlayerInfo.ReceivedPlotsGroupInvitations.Count > 0)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, titleColor);
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

                    ImGui.PushStyleColor(ImGuiCol.Text, groupNameColor);
                    ImGui.Text(invite.PlotsGroupName);
                    ImGui.PopStyleColor();

                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                    ImGui.Text("  " + invite.CityName);
                    ImGui.PopStyleColor();

                    if (ImGui.ImageButton("acceptplotsgroup", this.iconHandler.GetOrLoadIcon("expander"), new Vector2(16)))
                    {
                        ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                        clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plotsgroupaccept "
                            + invite.CityName + " " + invite.PlotsGroupName, EnumChatType.Macro, "");
                        var cell = claims.clientDataStorage.clientPlayerInfo.ReceivedPlotsGroupInvitations.FirstOrDefault(c => c.CityName == invite.CityName && c.PlotsGroupName == invite.PlotsGroupName);
                        if (cell != null)
                        {
                            toRemove.Add(cell);
                        }
                    }
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-plotsgroup-accept-tooltip"));

                    ImGui.SameLine();
                    if (ImGui.ImageButton("declineplotsgroup", this.iconHandler.GetOrLoadIcon("contract"), new Vector2(16)))
                    {
                        ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                        clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plotsgroupdeny "
                            + invite.CityName + " " + invite.PlotsGroupName, EnumChatType.Macro, "");
                        var cell = claims.clientDataStorage.clientPlayerInfo.ReceivedPlotsGroupInvitations.FirstOrDefault(c => c.CityName == invite.CityName && c.PlotsGroupName == invite.PlotsGroupName);
                        if (cell != null)
                        {
                            toRemove.Add(cell);
                        }
                    }
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-plotsgroup-decline-tooltip"));

                    ImGui.EndGroup();
                    ImGui.PopID();

                    ImGui.Dummy(new Vector2(0, 4));
                    ImGui.Separator();
                    ImGui.Dummy(new Vector2(0, 4));
                    i++;
                }
                ImGui.EndChild();
                if (toRemove.Count() > 0)
                {
                    foreach (var it in toRemove)
                        claims.clientDataStorage.clientPlayerInfo.ReceivedPlotsGroupInvitations.Remove(it);
                    toRemove.Clear();
                }
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                ImGui.Text(Lang.Get("claims:gui-no-plotsgroup-invites"));
                ImGui.PopStyleColor();
            }
        }
    }
}
