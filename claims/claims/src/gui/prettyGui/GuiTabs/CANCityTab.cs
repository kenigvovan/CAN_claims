using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANCityTab: CANGuiTab
    {
        List<ClientToCityInvitation> toRemove = new();
        public CANCityTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }
        public override void DrawTab()
        {
            Vector4 labelColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
            Vector4 nameColor = new Vector4(1.0f, 0.85f, 0.3f, 1.0f);

            if (claims.clientDataStorage.clientPlayerInfo?.CityInfo != null && claims.clientDataStorage.clientPlayerInfo?.CityInfo.Name != "")
            {
                var clientInfo = claims.clientDataStorage.clientPlayerInfo;

                // --- City name (centered, clickable if has permission) ---
                var perms = clientInfo.PlayerPermissions;
                string text = clientInfo.CityInfo.Name;
                ImGui.SetWindowFontScale(1.3f);
                float textWidth = ImGui.CalcTextSize(text).X;
                ImGui.SetWindowFontScale(1.0f);
                float windowWidth = ImGui.GetWindowSize().X;
                ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_NAME) || perms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_ALL))
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1, 1, 1, 0.1f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(1, 1, 1, 0.05f));
                    ImGui.PushStyleColor(ImGuiCol.Text, nameColor);
                    ImGui.SetWindowFontScale(1.3f);
                    if (ImGui.Button(text))
                    {
                        capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.SELECT_NEW_CITY_NAME;
                    }
                    ImGui.SetWindowFontScale(1.0f);
                    ImGui.PopStyleColor(4);
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, nameColor);
                    ImGui.SetWindowFontScale(1.3f);
                    ImGui.Text(text);
                    ImGui.SetWindowFontScale(1.0f);
                    ImGui.PopStyleColor();
                }

                ImGui.Separator();
                ImGui.Spacing();

                // --- Info table ---
                if (ImGui.BeginTable("CityInfoTable", 2, ImGuiTableFlags.None))
                {
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 130);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                    ImGui.Text(Lang.Get("claims:gui-city-tab-mayor"));
                    ImGui.PopStyleColor();
                    ImGui.TableNextColumn();
                    ImGui.Text(clientInfo.CityInfo.MayorName ?? "");

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                    ImGui.Text(Lang.Get("claims:gui-city-tab-created"));
                    ImGui.PopStyleColor();
                    ImGui.TableNextColumn();
                    ImGui.Text(TimeFunctions.getDateFromEpochSeconds(clientInfo.CityInfo.TimeStampCreated));

                    ImGui.EndTable();
                }

                ImGui.Spacing();

                // --- Plots ---
                clientInfo.CityInfo.MaxCountPlots.TryGetValue("base", out int baseAmount);
                clientInfo.CityInfo.MaxCountPlots.TryGetValue("bonus", out int bonus);
                clientInfo.CityInfo.MaxCountPlots.TryGetValue("alliance", out int alliance);
                string langVal;
                if (bonus > 0 && alliance > 0)
                {
                    langVal = Lang.Get("claims:gui-claimed-max-plots-with-bonus-alliance",
                        clientInfo.CityInfo.CountPlots, baseAmount + bonus + alliance, bonus, alliance);
                }
                else if (bonus > 0)
                {
                    langVal = Lang.Get("claims:gui-claimed-max-plots-with-bonus",
                        clientInfo.CityInfo.CountPlots, baseAmount + bonus + alliance, bonus);
                }
                else if (alliance > 0)
                {
                    langVal = Lang.Get("claims:gui-claimed-max-plots-with-alliance",
                        clientInfo.CityInfo.CountPlots, baseAmount + bonus + alliance, alliance);
                }
                else
                {
                    langVal = Lang.Get("claims:gui-claimed-max-plots", clientInfo.CityInfo.CountPlots, baseAmount);
                }

                ImGui.Text(langVal);

                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_CLAIM_PLOT))
                {
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.55f, 0.3f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.65f, 0.4f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.45f, 0.25f, 1.0f));
                    if (ImGui.ImageButton("claimplot", this.iconHandler.GetOrLoadIcon("expander"), new Vector2(16)))
                    {
                        capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.CLAIM_CITY_PLOT_CONFIRM;
                    }
                    ImGui.PopStyleColor(3);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(Lang.Get("claims:gui-buy-plot"));
                    }
                }
                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_UNCLAIM_PLOT))
                {
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.7f, 0.25f, 0.2f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.8f, 0.35f, 0.3f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.2f, 0.15f, 1.0f));
                    if (ImGui.ImageButton("unclaimplot", this.iconHandler.GetOrLoadIcon("contract"), new Vector2(16)))
                    {
                        capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.UNCLAIM_CITY_PLOT_CONFIRM;
                    }
                    ImGui.PopStyleColor(3);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(Lang.Get("claims:gui-unclaim-plot"));
                    }
                }
                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_PLOT_ACCESS_PERMISSIONS))
                {
                    ImGui.SameLine();
                    if (ImGui.ImageButton("setplotpermissions", this.iconHandler.GetOrLoadIcon("medal"), new Vector2(16)))
                    {
                        capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.CITY_PLOTS_PERMISSIONS;
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(Lang.Get("claims:gui-city-plots-permissions"));
                    }
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // --- Population ---
                ImGui.Text(Lang.Get("claims:gui-city-population", clientInfo.CityInfo.PlayersNames.Count));
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(StringFunctions.concatStringsWithDelim(clientInfo.CityInfo.PlayersNames, ','));
                }
                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_INVITE))
                {
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.55f, 0.3f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.65f, 0.4f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.45f, 0.25f, 1.0f));
                    if (ImGui.ImageButton("inviteplayer", this.iconHandler.GetOrLoadIcon("expander"), new Vector2(16)))
                    {
                        capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.INVITE_TO_CITY_NEED_NAME;
                    }
                    ImGui.PopStyleColor(3);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(Lang.Get("claims:gui-invite-player"));
                    }
                }
                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_KICK))
                {
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.7f, 0.25f, 0.2f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.8f, 0.35f, 0.3f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.2f, 0.15f, 1.0f));
                    if (ImGui.ImageButton("kickplayer", this.iconHandler.GetOrLoadIcon("contract"), new Vector2(16)))
                    {
                        capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.KICK_FROM_CITY_NEED_NAME;
                    }
                    ImGui.PopStyleColor(3);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(Lang.Get("claims:gui-kick-player"));
                    }
                }
                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_UNINVITE))
                {
                    ImGui.SameLine();
                    if (ImGui.ImageButton("uninviteplayer", this.iconHandler.GetOrLoadIcon("anticlockwise-rotation"), new Vector2(16)))
                    {
                        capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.UNINVITE_TO_CITY;
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(Lang.Get("claims:gui-uninvite-player"));
                    }
                }

                // --- Economy ---
                if (claims.config.SELECTED_ECONOMY_HANDLER == "VIRTUAL_MONEY")
                {
                    if (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_SEE_BALANCE))
                    {
                        ImGui.Spacing();
                        ImGui.Separator();
                        ImGui.Spacing();
                        ImGui.Text(Lang.Get("claims:gui-city-balance", clientInfo.CityInfo.CityBalance));
                    }
                }
                if (claims.config.GUI_SHOW_DEBT && clientInfo.CityInfo.CityDebt > 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
                    ImGui.Text(Lang.Get("claims:gui-city-debt", clientInfo.CityInfo.CityDebt));
                    ImGui.PopStyleColor();
                }
                if (clientInfo.CityInfo.CityDayPayment > 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                    ImGui.Text(Lang.Get("claims:gui-city-payment", clientInfo.CityInfo.CityDayPayment));
                    ImGui.PopStyleColor();
                }

                // --- Bottom navigation ---
                float availY = ImGui.GetContentRegionAvail().Y;
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + availY - 80);

                if (ImGui.ImageButton("leavecity", this.iconHandler.GetOrLoadIcon("exit-door"), new Vector2(60)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.LEAVE_CITY_CONFIRM;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-leave-city"));
                }
                ImGui.SameLine();
                if (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_REMOVE_RANK) ||
                    claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_SET_RANK))
                {
                    if (ImGui.ImageButton("ranks", this.iconHandler.GetOrLoadIcon("achievement"), new Vector2(60)))
                    {
                        capi.ModLoader.GetModSystem<claimsGui>().selectedTab = EnumSelectedTab.RANKS;
                    }
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-city-ranks"));
                }
                ImGui.SameLine();
                if (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_SET_PLOTS_COLOR))
                {
                    if (ImGui.ImageButton("plotscolors", this.iconHandler.GetOrLoadIcon("large-paint-brush"), new Vector2(60)))
                    {
                        capi.ModLoader.GetModSystem<claimsGui>().selectedTab = EnumSelectedTab.CityPlotsColorSelector;
                    }
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-city-plots-color"));
                }
                ImGui.SameLine();
                if (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_SET_PLOTS_COLOR))
                {
                    if (ImGui.ImageButton("alliance", this.iconHandler.GetOrLoadIcon("vertical-banner"), new Vector2(60)))
                    {
                        capi.ModLoader.GetModSystem<claimsGui>().selectedTab = EnumSelectedTab.AllianceInfoPage;
                    }
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-alliance"));
                }
                ImGui.SameLine();
                if (ImGui.ImageButton("conflictletterscity", this.iconHandler.GetOrLoadIcon("envelope"), new Vector2(60)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().conflictSourceTab = EnumSelectedTab.CITY;
                    capi.ModLoader.GetModSystem<claimsGui>().selectedTab = EnumSelectedTab.ConflictLettersPage;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-conflict-letters"));
                }
                ImGui.SameLine();
                if (ImGui.ImageButton("conflictspagecity", this.iconHandler.GetOrLoadIcon("frog-mouth-helm"), new Vector2(60)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().conflictSourceTab = EnumSelectedTab.CITY;
                    capi.ModLoader.GetModSystem<claimsGui>().selectedTab = EnumSelectedTab.ConflictsPage;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-conflicts-page"));
                }
            }
            else
            {
                // --- No city: create button ---
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.55f, 0.3f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.65f, 0.4f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.45f, 0.25f, 1.0f));
                if (ImGui.ImageButton("createcity", this.iconHandler.GetOrLoadIcon("queen-crown"), new Vector2(60)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.NEED_NAME;
                }
                ImGui.PopStyleColor(3);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-new-city-button"));
                }

                // --- Invitations ---
                if (claims.clientDataStorage.clientPlayerInfo.ReceivedInvitations.Count > 0)
                {
                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    ImGui.PushStyleColor(ImGuiCol.Text, nameColor);
                    ImGui.SetWindowFontScale(1.15f);
                    ImGui.Text(Lang.Get("claims:gui-city-tab-invitations",
                        claims.clientDataStorage.clientPlayerInfo.ReceivedInvitations.Count));
                    ImGui.SetWindowFontScale(1.0f);
                    ImGui.PopStyleColor();

                    ImGui.BeginChild("InvitesScroll", new Vector2(0, 300), true);
                    int i = 0;
                    foreach (var invite in claims.clientDataStorage.clientPlayerInfo.ReceivedInvitations)
                    {
                        ImGui.PushID(i++);
                        ImGui.BeginGroup();

                        ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                        ImGui.Text(Lang.Get("claims:gui-city-tab-invite-city"));
                        ImGui.PopStyleColor();
                        ImGui.SameLine(0, 0);
                        ImGui.Text(invite.CityName ?? "");

                        ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
                        ImGui.Text(Lang.Get("claims:gui-city-tab-invite-expires"));
                        ImGui.PopStyleColor();
                        ImGui.SameLine(0, 0);
                        ImGui.Text(TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(invite.TimeoutStamp, true));

                        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.55f, 0.3f, 1.0f));
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.65f, 0.4f, 1.0f));
                        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.45f, 0.25f, 1.0f));
                        if (ImGui.Button(Lang.Get("claims:gui-city-tab-accept")))
                        {
                            ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                            clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/accept " + invite.CityName, EnumChatType.Macro, "");
                            toRemove.Add(invite);
                        }
                        ImGui.PopStyleColor(3);

                        ImGui.SameLine();

                        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.7f, 0.25f, 0.2f, 1.0f));
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.8f, 0.35f, 0.3f, 1.0f));
                        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.2f, 0.15f, 1.0f));
                        if (ImGui.Button(Lang.Get("claims:gui-city-tab-decline")))
                        {
                            ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                            clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/deny " + invite.CityName, EnumChatType.Macro, "");
                            toRemove.Add(invite);
                        }
                        ImGui.PopStyleColor(3);

                        ImGui.EndGroup();
                        ImGui.PopID();

                        ImGui.Spacing();
                        ImGui.Separator();
                        ImGui.Spacing();
                    }
                    ImGui.EndChild();
                    if (toRemove.Count > 0)
                    {
                        foreach (var inv in toRemove)
                            claims.clientDataStorage.clientPlayerInfo.ReceivedInvitations.Remove(inv);
                        toRemove.Clear();
                    }
                }
            }
        }
    }
}
