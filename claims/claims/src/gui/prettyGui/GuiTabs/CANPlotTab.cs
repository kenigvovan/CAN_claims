using System.Numerics;
using claims.src.network.packets;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANPlotTab: CANGuiTab
    {
        public CANPlotTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }
        public override void DrawTab()
        {
            var clientInfo = claims.clientDataStorage.clientPlayerInfo;
            Vector4 labelColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
            Vector4 valueColor = new Vector4(1.0f, 0.85f, 0.3f, 1.0f);
            Vector4 sectionColor = new Vector4(0.4f, 0.7f, 1.0f, 1.0f);

            // --- Header: position + refresh ---
            ImGui.PushStyleColor(ImGuiCol.Text, valueColor);
            ImGui.SetWindowFontScale(1.15f);
            ImGui.Text($"[{clientInfo.CurrentPlotInfo.PlotPosition.X} / {clientInfo.CurrentPlotInfo.PlotPosition.Y}]");
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();
            ImGui.SameLine();
            if (ImGui.ImageButton("plotinfoget", this.iconHandler.GetOrLoadIcon("info"), new Vector2(15)))
            {
                claims.clientChannel.SendPacket(new SavedPlotsPacket()
                {
                    type = PacketsContentEnum.CURRENT_PLOT_CLIENT_REQUEST,
                    data = ""
                });
            }

            ImGui.Separator();
            ImGui.Spacing();

            var perms = clientInfo.PlayerPermissions;
            bool canEditPlot = perms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_ALL_CITY_PLOTS)
                || perms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_ALL_OWN_PLOT);

            // --- Plot name ---
            ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
            ImGui.Text(Lang.Get("claims:gui-plot-name", ""));
            ImGui.PopStyleColor();
            ImGui.SameLine(0, 0);
            ImGui.Text(clientInfo.CurrentPlotInfo.PlotName ?? "");
            if (canEditPlot || perms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_NAME))
            {
                ImGui.SameLine();
                if (ImGui.ImageButton("setplotname", this.iconHandler.GetOrLoadIcon("info"), new Vector2(15)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.PLOT_SET_NAME;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-plot-set-name-tooltip"));
                }
            }

            // --- Owner ---
            ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
            ImGui.Text(Lang.Get("claims:gui-owner-name", ""));
            ImGui.PopStyleColor();
            ImGui.SameLine(0, 0);
            ImGui.Text(clientInfo.CurrentPlotInfo.OwnerName ?? "");

            if (clientInfo.CurrentPlotInfo.Price > -1)
            {
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.55f, 0.3f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.65f, 0.4f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.45f, 0.25f, 1.0f));
                if (ImGui.ImageButton("plotclaim", this.iconHandler.GetOrLoadIcon("id-card"), new Vector2(15)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.PLOT_CLAIM;
                }
                ImGui.PopStyleColor(3);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-plot-buy-tooltip"));
                }
            }

            if (clientInfo.CurrentPlotInfo.OwnerName?.Length > 0)
            {
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.7f, 0.25f, 0.2f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.8f, 0.35f, 0.3f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.2f, 0.15f, 1.0f));
                if (ImGui.ImageButton("plotunclaim", this.iconHandler.GetOrLoadIcon("id-card"), new Vector2(15)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.PLOT_UNCLAIM;
                }
                ImGui.PopStyleColor(3);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-plot-unclaim-tooltip"));
                }
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- Plot type ---
            ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
            ImGui.Text(Lang.Get("claims:gui-plot-type", ""));
            ImGui.PopStyleColor();
            ImGui.SameLine(0, 0);
            ImGui.Text(PlotInfo.dictPlotTypes.TryGetValue(clientInfo.CurrentPlotInfo.PlotType, out PlotInfo plotInfo) ? plotInfo.getFullName() : "-");
            if (canEditPlot || perms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_TYPE))
            {
                ImGui.SameLine();
                if (ImGui.ImageButton("plotsettype", this.iconHandler.GetOrLoadIcon("files"), new Vector2(15)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.PLOT_SET_TYPE;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-plot-set-type-tooltip"));
                }
            }

            if (clientInfo.CurrentPlotInfo.PlotType == PlotType.PRISON && perms.HasPermission(rights.EnumPlayerPermissions.CITY_PRISON_ADD_CELL))
            {
                ImGui.SameLine();
                if (ImGui.ImageButton("addprisoncell", this.iconHandler.GetOrLoadIcon("pencil"), new Vector2(15)))
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/c prison addcell", EnumChatType.Macro, "");
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-plot-add-prison-cell-tooltip"));
                }
            }
            else if (clientInfo.CurrentPlotInfo.PlotType == PlotType.SUMMON && perms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_SUMMON))
            {
                ImGui.SameLine();
                if (ImGui.ImageButton("setsummonpoint", this.iconHandler.GetOrLoadIcon("pencil"), new Vector2(15)))
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/c summon set point", EnumChatType.Macro, "");
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-plot-set-summon-tooltip"));
                }
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- Tax ---
            ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
            ImGui.Text(Lang.Get("claims:gui-plot-custom-tax", ""));
            ImGui.PopStyleColor();
            ImGui.SameLine(0, 0);
            ImGui.Text(clientInfo.CurrentPlotInfo.CustomTax.ToString());
            if (canEditPlot || perms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_FEE))
            {
                ImGui.SameLine();
                if (ImGui.ImageButton("settax", this.iconHandler.GetOrLoadIcon("medal"), new Vector2(15)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.PLOT_SET_TAX;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-plot-set-tax-tooltip"));
                }
            }

            // --- Price ---
            ImGui.PushStyleColor(ImGuiCol.Text, labelColor);
            ImGui.Text(Lang.Get("claims:gui-plot-price", ""));
            ImGui.PopStyleColor();
            ImGui.SameLine(0, 0);
            ImGui.Text(clientInfo.CurrentPlotInfo.Price > -1
                ? clientInfo.CurrentPlotInfo.Price.ToString()
                : Lang.Get("claims:gui-not-for-sale"));
            if (canEditPlot || perms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_FS))
            {
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.55f, 0.3f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.65f, 0.4f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.45f, 0.25f, 1.0f));
                if (ImGui.ImageButton("setplotprice", this.iconHandler.GetOrLoadIcon("medal"), new Vector2(15)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.PLOT_SET_PRICE_NEED_NUMBER;
                }
                ImGui.PopStyleColor(3);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-plot-set-price-tooltip"));
                }
            }
            if (canEditPlot || perms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_NFS))
            {
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.7f, 0.25f, 0.2f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.8f, 0.35f, 0.3f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.2f, 0.15f, 1.0f));
                if (ImGui.ImageButton("plotnfs", this.iconHandler.GetOrLoadIcon("contract"), new Vector2(15)))
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot nfs", EnumChatType.Macro, "");
                    // Optimistic local update
                    clientInfo.CurrentPlotInfo.Price = -1;
                }
                ImGui.PopStyleColor(3);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-plot-not-for-sale-tooltip"));
                }
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- Permissions ---
            if (canEditPlot || perms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_PLOT_ACCESS_PERMISSIONS))
            {
                ImGui.Text(Lang.Get("claims:gui-plot-permissions"));
                ImGui.SameLine();
                if (ImGui.ImageButton("plotpermissions", this.iconHandler.GetOrLoadIcon("medal"), new Vector2(15)))
                {
                    capi.ModLoader.GetModSystem<claimsGui>().secondaryWindowTab = EnumSecondaryWindowTab.PLOT_PERMISSIONS;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Lang.Get("claims:gui-plot-set-permissions-tooltip"));
                }
            }

            // --- Borders ---
            ImGui.Text(Lang.Get("claims:gui-plot-borders"));
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.55f, 0.3f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.65f, 0.4f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.45f, 0.25f, 1.0f));
            if (ImGui.ImageButton("showplotborders", this.iconHandler.GetOrLoadIcon("medal"), new Vector2(15)))
            {
                ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot borders on", EnumChatType.Macro, "");
            }
            ImGui.PopStyleColor(3);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Lang.Get("claims:gui-plot-show-borders-tooltip"));
            }
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.7f, 0.25f, 0.2f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.8f, 0.35f, 0.3f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0.2f, 0.15f, 1.0f));
            if (ImGui.ImageButton("hideplotborders", this.iconHandler.GetOrLoadIcon("medal"), new Vector2(15)))
            {
                ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot borders off", EnumChatType.Macro, "");
            }
            ImGui.PopStyleColor(3);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Lang.Get("claims:gui-plot-hide-borders-tooltip"));
            }
        }
    }
}
