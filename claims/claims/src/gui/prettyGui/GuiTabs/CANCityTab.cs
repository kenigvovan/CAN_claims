using claims.src.auxialiry;
using ImGuiNET;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANCityTab: CANGuiTab
    {
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
                var cityTabFont = CairoFont.ButtonText().WithFontSize(20).WithOrientation(EnumTextOrientation.Left);

                string text = clientInfo.CityInfo.Name;

                float windowWidth = ImGui.GetWindowSize().X;
                float textWidth = ImGui.CalcTextSize(text).X;

                ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
                if (ImGui.Button(text))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.SELECT_NEW_CITY_NAME;
                }
                ImGui.Text(Lang.Get("claims:gui-mayor-name", clientInfo.CityInfo.MayorName));
                ImGui.Text(Lang.Get("claims:gui-date-created", TimeFunctions.getDateFromEpochSeconds(clientInfo.CityInfo.TimeStampCreated)));
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
                ImGui.SameLine();

                if (ImGui.ImageButton("claimplot", this.iconHandler.GetOrLoadIcon("expander"), new Vector2(16)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.CLAIM_CITY_PLOT_CONFIRM;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-buy-plot"));
                }
                ImGui.SameLine();
                if (ImGui.ImageButton("unclaimplot", this.iconHandler.GetOrLoadIcon("contract"), new Vector2(16)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.UNCLAIM_CITY_PLOT_CONFIRM;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-unclaim-plot"));
                }
                ImGui.SameLine();
                if (ImGui.ImageButton("setplotpermissions", this.iconHandler.GetOrLoadIcon("medal"), new Vector2(16)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.CITY_PLOTS_PERMISSIONS;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-city-plots-permissions"));
                }
                ImGui.Text(Lang.Get("claims:gui-city-population", clientInfo.CityInfo.PlayersNames.Count));
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(StringFunctions.concatStringsWithDelim(clientInfo.CityInfo.PlayersNames, ','));
                }
                ImGui.SameLine();
                if (ImGui.ImageButton("inviteplayer", this.iconHandler.GetOrLoadIcon("expander"), new Vector2(16)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.INVITE_TO_CITY_NEED_NAME;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-invite-player"));
                }
                ImGui.SameLine();
                if (ImGui.ImageButton("kickplayer", this.iconHandler.GetOrLoadIcon("contract"), new Vector2(16)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.KICK_FROM_CITY_NEED_NAME;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-kick-player"));
                }
                ImGui.SameLine();
                if (ImGui.ImageButton("uninviteplayer", this.iconHandler.GetOrLoadIcon("anticlockwise-rotation"), new Vector2(16)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.UNINVITE_TO_CITY;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-uninvite-player"));
                }
                if (claims.config.SELECTED_ECONOMY_HANDLER == "VIRTUAL_MONEY")
                {
                    if (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_SEE_BALANCE))
                    {
                        ImGui.Text(Lang.Get("claims:gui-city-balance", clientInfo.CityInfo.CityBalance));
                    }
                }
                if (claims.config.GUI_SHOW_DEBT && clientInfo.CityInfo.CityDebt > 0)
                {
                    ImGui.Text(Lang.Get("claims:gui-city-debt", clientInfo.CityInfo.CityDebt));
                }
                if (clientInfo.CityInfo.CityDayPayment > 0)
                {
                    ImGui.Text(Lang.Get("claims:gui-city-payment", clientInfo.CityInfo.CityDayPayment));
                }
                if (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_SET_GLOBAL_FEE)
                    || claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_SET_ALL))
                {
                    ImGui.Text(Lang.Get("claims:gui-city-fee"));
                    ImGui.SameLine(0, 0);
                    ImGui.Text(clientInfo.CityInfo.CityFee.ToString());
                    ImGui.SameLine();
                    if (ImGui.ImageButton("setcityfee", this.iconHandler.GetOrLoadIcon("medal"), new Vector2(16)))
                    {
                        capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.CITY_SET_FEE;
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(Lang.Get("claims:gui-city-set-fee-tooltip"));
                    }
                }


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

            }
            else
            {
                if (ImGui.ImageButton("createcity", this.iconHandler.GetOrLoadIcon("queen-crown"), new Vector2(60)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.NEED_NAME;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-new-city-button"));
                }

                if (claims.clientDataStorage.clientPlayerInfo.ReceivedInvitations.Count > 0)
                {
                    ImGui.Text("Invitations" + " [" + claims.clientDataStorage.clientPlayerInfo.ReceivedInvitations.Count + "]");

                    ImGui.BeginChild("InvitesScroll", new Vector2(0, 300), true);
                    int i = 0;
                    foreach(var invite in claims.clientDataStorage.clientPlayerInfo.ReceivedInvitations)
                    {
                        ImGui.PushID(i);

                        Vector2 start = ImGui.GetCursorScreenPos();
                        float width = ImGui.GetContentRegionAvail().X;

                        ImGui.BeginGroup();

                        ImGui.Text($"City: {claims.clientDataStorage.clientPlayerInfo.ReceivedInvitations[i].CityName}");
                        ImGui.Text($"Time: {TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(claims.clientDataStorage.clientPlayerInfo.ReceivedInvitations[i].TimeoutStamp, true).ToString()}");

                        if (ImGui.Button("Accept"))
                        {                          
                            ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                            clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/accept " + claims.clientDataStorage.clientPlayerInfo?.ReceivedInvitations[i].CityName, EnumChatType.Macro, "");
                        }

                        ImGui.SameLine();

                        if (ImGui.Button("Decline"))
                        {
                            ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                            clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/deny " + claims.clientDataStorage.clientPlayerInfo?.ReceivedInvitations[i].CityName, EnumChatType.Macro, "");
                        }

                        ImGui.EndGroup();

                        Vector2 end = ImGui.GetItemRectMax();
                        var draw = ImGui.GetWindowDrawList();

                        ImGui.PopID();

                        ImGui.Dummy(new Vector2(0, 8));
                        ImGui.Separator();
                    }
                    ImGui.EndChild();
                }
            }
        }
    }
}
