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
            if (clientInfo?.CurrentPlotInfo == null) return;

            // --- Header: position + refresh ---
            ImGui.PushStyleColor(ImGuiCol.Text, ColValue);
            ImGui.SetWindowFontScale(1.15f);
            ImGui.Text($"[{clientInfo.CurrentPlotInfo.PlotPosition.X} / {clientInfo.CurrentPlotInfo.PlotPosition.Y}]");
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();
            ImGui.SameLine();
            if (IconButton("plotinfoget", "info", 15))
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
            LabelValue(Lang.Get("claims:gui-plot-name", ""), clientInfo.CurrentPlotInfo.PlotName ?? "");
            if (canEditPlot || perms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_NAME))
            {
                ImGui.SameLine();
                if (IconButton("setplotname", "info", 15, Lang.Get("claims:gui-plot-set-name-tooltip")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.PLOT_SET_NAME;
            }

            // --- Owner ---
            LabelValue(Lang.Get("claims:gui-owner-name", ""), clientInfo.CurrentPlotInfo.OwnerName ?? "");

            if (clientInfo.CurrentPlotInfo.Price > -1)
            {
                ImGui.SameLine();
                if (GreenIconButton("plotclaim", "id-card", 15, Lang.Get("claims:gui-plot-buy-tooltip")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.PLOT_CLAIM;
            }

            if (clientInfo.CurrentPlotInfo.OwnerName?.Length > 0)
            {
                ImGui.SameLine();
                if (RedIconButton("plotunclaim", "id-card", 15, Lang.Get("claims:gui-plot-unclaim-tooltip")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.PLOT_UNCLAIM;
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- Plot type ---
            LabelValue(Lang.Get("claims:gui-plot-type", ""),
                PlotInfo.dictPlotTypes.TryGetValue(clientInfo.CurrentPlotInfo.PlotType, out PlotInfo plotInfo) ? plotInfo.getFullName() : "-");
            if (canEditPlot || perms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_TYPE))
            {
                ImGui.SameLine();
                if (IconButton("plotsettype", "files", 15, Lang.Get("claims:gui-plot-set-type-tooltip")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.PLOT_SET_TYPE;
            }

            if (clientInfo.CurrentPlotInfo.PlotType == PlotType.PRISON && perms.HasPermission(rights.EnumPlayerPermissions.CITY_PRISON_ADD_CELL))
            {
                ImGui.SameLine();
                if (IconButton("addprisoncell", "pencil", 15, Lang.Get("claims:gui-plot-add-prison-cell-tooltip")))
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/c prison addcell", EnumChatType.Macro, "");
                }
            }
            else if (clientInfo.CurrentPlotInfo.PlotType == PlotType.SUMMON && perms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_SUMMON))
            {
                ImGui.SameLine();
                if (IconButton("setsummonpoint", "pencil", 15, Lang.Get("claims:gui-plot-set-summon-tooltip")))
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/c summon set point", EnumChatType.Macro, "");
                }
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // --- Tax ---
            LabelValue(Lang.Get("claims:gui-plot-custom-tax", ""), clientInfo.CurrentPlotInfo.CustomTax.ToString());
            if (canEditPlot || perms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_FEE))
            {
                ImGui.SameLine();
                if (IconButton("settax", "medal", 15, Lang.Get("claims:gui-plot-set-tax-tooltip")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.PLOT_SET_TAX;
            }

            // --- Price ---
            LabelValue(Lang.Get("claims:gui-plot-price", ""), clientInfo.CurrentPlotInfo.Price > -1
                ? clientInfo.CurrentPlotInfo.Price.ToString()
                : Lang.Get("claims:gui-not-for-sale"));
            if (canEditPlot || perms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_FS))
            {
                ImGui.SameLine();
                if (GreenIconButton("setplotprice", "medal", 15, Lang.Get("claims:gui-plot-set-price-tooltip")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.PLOT_SET_PRICE_NEED_NUMBER;
            }
            if (canEditPlot || perms.HasPermission(rights.EnumPlayerPermissions.PLOT_SET_NFS))
            {
                ImGui.SameLine();
                if (RedIconButton("plotnfs", "contract", 15, Lang.Get("claims:gui-plot-not-for-sale-tooltip")))
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot nfs", EnumChatType.Macro, "");
                    // Optimistic local update
                    clientInfo.CurrentPlotInfo.Price = -1;
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
                if (IconButton("plotpermissions", "medal", 15, Lang.Get("claims:gui-plot-set-permissions-tooltip")))
                    GuiSys.secondaryWindowTab = EnumSecondaryWindowTab.PLOT_PERMISSIONS;
            }

            // --- Borders ---
            ImGui.Text(Lang.Get("claims:gui-plot-borders"));
            ImGui.SameLine();
            if (GreenIconButton("showplotborders", "medal", 15, Lang.Get("claims:gui-plot-show-borders-tooltip")))
            {
                ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot borders on", EnumChatType.Macro, "");
            }
            ImGui.SameLine();
            if (RedIconButton("hideplotborders", "medal", 15, Lang.Get("claims:gui-plot-hide-borders-tooltip")))
            {
                ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot borders off", EnumChatType.Macro, "");
            }
        }
    }
}
