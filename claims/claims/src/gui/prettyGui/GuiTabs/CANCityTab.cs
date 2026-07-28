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
            if (claims.clientDataStorage.clientPlayerInfo?.CityInfo != null && claims.clientDataStorage.clientPlayerInfo?.CityInfo.Name != "")
            {
                var clientInfo = claims.clientDataStorage.clientPlayerInfo;

                // --- City name (centered, clickable if has permission) ---
                var perms = clientInfo.PlayerPermissions;
                string text = clientInfo.CityInfo.Name;
                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_NAME) || perms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_ALL))
                {
                    if (CenteredTitleButton(text, ColValue))
                        GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.SELECT_NEW_CITY_NAME;
                }
                else
                {
                    CenteredTitle(text, ColValue);
                }

                ImGui.Separator();
                ImGui.Spacing();

                // --- Info table ---
                if (ImGui.BeginTable("CityInfoTable", 2, TableFlags))
                {
                    ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 130);
                    ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    Label(Lang.Get("claims:gui-city-tab-mayor"));
                    ImGui.TableNextColumn();
                    ImGui.Text(clientInfo.CityInfo.MayorName ?? "");

                    if (clientInfo.CityInfo.CityTitles != null && clientInfo.CityInfo.CityTitles.Count > 0)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        Label(Lang.Get("claims:gui-city-your-rank-label"));
                        ImGui.TableNextColumn();
                        ImGui.Text(string.Join(", ", clientInfo.CityInfo.CityTitles));
                    }

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    Label(Lang.Get("claims:gui-city-tab-created"));
                    ImGui.TableNextColumn();
                    ImGui.Text(TimeFunctions.getDateFromEpochSeconds(clientInfo.CityInfo.TimeStampCreated));

                    ImGui.EndTable();
                }

                ImGui.Spacing();

                // --- Plots ---
                clientInfo.CityInfo.MaxCountPlots.TryGetValue("base", out int baseAmount);
                clientInfo.CityInfo.MaxCountPlots.TryGetValue("bonus", out int bonus);
                clientInfo.CityInfo.MaxCountPlots.TryGetValue("alliance", out int alliance);
                int maxPlots = baseAmount + bonus + alliance;
                string langVal;
                if (bonus > 0 && alliance > 0)
                {
                    langVal = Lang.Get("claims:gui-claimed-max-plots-with-bonus-alliance",
                        clientInfo.CityInfo.CountPlots, maxPlots, bonus, alliance);
                }
                else if (bonus > 0)
                {
                    langVal = Lang.Get("claims:gui-claimed-max-plots-with-bonus",
                        clientInfo.CityInfo.CountPlots, maxPlots, bonus);
                }
                else if (alliance > 0)
                {
                    langVal = Lang.Get("claims:gui-claimed-max-plots-with-alliance",
                        clientInfo.CityInfo.CountPlots, maxPlots, alliance);
                }
                else
                {
                    langVal = Lang.Get("claims:gui-claimed-max-plots", clientInfo.CityInfo.CountPlots, baseAmount);
                }

                var plotColor = StateColor(maxPlots > 0 ? (float)clientInfo.CityInfo.CountPlots / maxPlots : 0f);
                if (plotColor.HasValue) ImGui.PushStyleColor(ImGuiCol.Text, plotColor.Value);
                ImGui.Text(langVal);
                if (plotColor.HasValue) ImGui.PopStyleColor();

                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_CLAIM_PLOT))
                {
                    ImGui.SameLine();
                    if (GreenIconButton("claimplot", "expander", 16, Lang.Get("claims:gui-buy-plot")))
                        GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CLAIM_CITY_PLOT_CONFIRM;
                }
                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_UNCLAIM_PLOT))
                {
                    ImGui.SameLine();
                    if (RedIconButton("unclaimplot", "contract", 16, Lang.Get("claims:gui-unclaim-plot")))
                        GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.UNCLAIM_CITY_PLOT_CONFIRM;
                }
                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_PLOT_ACCESS_PERMISSIONS))
                {
                    ImGui.SameLine();
                    if (IconButton("setplotpermissions", "medal", 16, Lang.Get("claims:gui-city-plots-permissions")))
                        GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_PLOTS_PERMISSIONS;
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
                    if (GreenIconButton("inviteplayer", "expander", 16, Lang.Get("claims:gui-invite-player")))
                        GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.INVITE_TO_CITY_NEED_NAME;
                }
                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_KICK))
                {
                    ImGui.SameLine();
                    if (RedIconButton("kickplayer", "contract", 16, Lang.Get("claims:gui-kick-player")))
                        GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.KICK_FROM_CITY_NEED_NAME;
                }
                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_UNINVITE))
                {
                    ImGui.SameLine();
                    if (IconButton("uninviteplayer", "anticlockwise-rotation", 16, Lang.Get("claims:gui-uninvite-player")))
                        GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.UNINVITE_TO_CITY;
                }

                // --- Economy ---
                if (claims.config.SELECTED_ECONOMY_HANDLER == "VIRTUAL_MONEY")
                {
                    if (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_SEE_BALANCE))
                    {
                        ImGui.Spacing();
                        ImGui.Separator();
                        ImGui.Spacing();
                        double bal = clientInfo.CityInfo.CityBalance;
                        double fee = clientInfo.CityInfo.CityDayPayment;
                        var balColor = bal < 0 ? ColDanger : (fee > 0 && bal < fee * 3 ? ColWarning : (Vector4?)null);
                        if (balColor.HasValue) ImGui.PushStyleColor(ImGuiCol.Text, balColor.Value);
                        ImGui.Text(Lang.Get("claims:gui-city-balance", bal));
                        if (balColor.HasValue) ImGui.PopStyleColor();
                        ImGui.SameLine();
                        if (ImGui.SmallButton("+##citydeposit"))
                        {
                            GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_DEPOSIT_CONFIRM;
                        }
                        if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-city-deposit-tooltip"));
                        if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_WITHDRAW_MONEY))
                        {
                            ImGui.SameLine();
                            if (ImGui.SmallButton("-##citywithdraw"))
                            {
                                GuiSys.intInput = 0;
                                GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_WITHDRAW;
                            }
                            if (ImGui.IsItemHovered()) ImGui.SetTooltip(Lang.Get("claims:gui-city-withdraw-tooltip"));
                        }
                    }
                }
                if (claims.config.GUI_SHOW_DEBT && clientInfo.CityInfo.CityDebt > 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, ColDanger);
                    ImGui.Text(Lang.Get("claims:gui-city-debt", clientInfo.CityInfo.CityDebt));
                    ImGui.PopStyleColor();
                }
                if (clientInfo.CityInfo.CityDayPayment > 0)
                {
                    Label(Lang.Get("claims:gui-city-payment", clientInfo.CityInfo.CityDayPayment));
                }
                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_GLOBAL_FEE) || perms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_ALL))
                {
                    Label(Lang.Get("claims:gui-city-fee"));
                    ImGui.SameLine(0, 0);
                    ImGui.Text(clientInfo.CityInfo.CityFee.ToString());
                    ImGui.SameLine();
                    if (IconButton("setcityfee", "medal", 16, Lang.Get("claims:gui-city-set-fee-tooltip")))
                        GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.CITY_SET_FEE;
                }

                // --- Bottom navigation ---
                AlignBottom();

                if (IconButton("leavecity", "exit-door", 60, Lang.Get("claims:gui-leave-city")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.LEAVE_CITY_CONFIRM;
                ImGui.SameLine();
                if (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_REMOVE_RANK) ||
                    claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_SET_RANK))
                {
                    if (IconButton("ranks", "achievement", 60, Lang.Get("claims:gui-city-ranks")))
                        GuiSys.selectedTab = EnumSelectedTab.RANKS;
                    ImGui.SameLine();
                }
                if (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_SET_PLOTS_COLOR))
                {
                    if (IconButton("plotscolors", "large-paint-brush", 60, Lang.Get("claims:gui-city-plots-color")))
                        GuiSys.selectedTab = EnumSelectedTab.CityPlotsColorSelector;
                    ImGui.SameLine();
                    if (IconButton("alliance", "vertical-banner", 60, Lang.Get("claims:gui-alliance")))
                        GuiSys.selectedTab = EnumSelectedTab.AllianceInfoPage;
                    ImGui.SameLine();
                }
                if (IconButton("conflictletterscity", "envelope", 60, Lang.Get("claims:gui-conflict-letters")))
                {
                    GuiSys.conflictSourceTab = EnumSelectedTab.CITY;
                    GuiSys.selectedTab = EnumSelectedTab.ConflictLettersPage;
                }
                ImGui.SameLine();
                if (IconButton("conflictspagecity", "frog-mouth-helm", 60, Lang.Get("claims:gui-conflicts-page")))
                {
                    GuiSys.conflictSourceTab = EnumSelectedTab.CITY;
                    GuiSys.selectedTab = EnumSelectedTab.ConflictsPage;
                }
                ImGui.SameLine();
                if (IconButton("citylog", "files", 60, Lang.Get("claims:gui-city-log-tooltip")))
                    GuiSys.selectedTab = EnumSelectedTab.CityLog;
                ImGui.SameLine();
                if (IconButton("citymap", "huts-village", 60, Lang.Get("claims:gui-city-map-tooltip")))
                    GuiSys.selectedTab = EnumSelectedTab.CityMap;
            }
            else
            {
                // --- No city: create button ---
                if (GreenIconButton("createcity", "queen-crown", 60, Lang.Get("claims:gui-new-city-button")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.NEED_NAME;

                // --- Invitations ---
                if (claims.clientDataStorage.clientPlayerInfo.ReceivedInvitations.Count > 0)
                {
                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    ImGui.PushStyleColor(ImGuiCol.Text, ColValue);
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

                        LabelValue(Lang.Get("claims:gui-city-tab-invite-city"), invite.CityName ?? "");
                        LabelValue(Lang.Get("claims:gui-city-tab-invite-expires"),
                            TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(invite.TimeoutStamp, true));

                        if (GreenButton(Lang.Get("claims:gui-city-tab-accept")))
                        {
                            SendCommand("/accept " + invite.CityName);
                            toRemove.Add(invite);
                        }

                        ImGui.SameLine();

                        if (RedButton(Lang.Get("claims:gui-city-tab-decline")))
                        {
                            SendCommand("/deny " + invite.CityName);
                            toRemove.Add(invite);
                        }

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
