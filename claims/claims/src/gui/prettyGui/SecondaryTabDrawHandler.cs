using System.Collections.Generic;
using claims.src.gui.prettyGui.GuiSecondaryTabs;
using Vintagestory.API.Client;

namespace claims.src.gui.prettyGui
{
    public class SecondaryTabDrawHandler
    {
        private Dictionary<EnumSecondaryWindowTab, CANGuiSecondaryTab> TabDictionary;
        public ICoreClientAPI capi;
        public IconHandler iconHandler;
        public SecondaryTabDrawHandler(ICoreClientAPI capi, IconHandler iconHandler)
        {
            TabDictionary = new();
            this.capi = capi;
            this.TabDictionary.Add(EnumSecondaryWindowTab.NEED_NAME, new CANNeedNameTab(capi, iconHandler, "claims:gui-enter-new-city-name", "/city new ", "-->"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.NEED_AGREE, new CANNeedAgreeTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSecondaryWindowTab.INVITE_TO_CITY_NEED_NAME, new CANNeedNameTab(capi, iconHandler, "claims:gui-enter-player-name", "/city invite ", "Invite"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.KICK_FROM_CITY_NEED_NAME, new CANKickFromCityTab(capi, iconHandler));
            this.TabDictionary.Add(EnumSecondaryWindowTab.CLAIM_CITY_PLOT_CONFIRM, new CANYesNoTab(capi, iconHandler, "claims:gui-confirm-city-plot-claim", "/city claim"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.UNCLAIM_CITY_PLOT_CONFIRM, new CANYesNoTab(capi, iconHandler, "claims:gui-confirm-city-plot-unclaim", "/city unclaim"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.ADD_FRIEND_NEED_NAME, new CANNeedNameTab(capi, iconHandler, "claims:gui-enter-player-name", "/citizen friend add ", "claims:gui-add-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.PLOT_SET_NAME, new CANNeedNameTab(capi, iconHandler, "claims:gui-enter-name-for-plot", "/plot set name ", "claims:gui-set-name"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.PLOT_CLAIM, new CANYesNoTab(capi, iconHandler, "claims:gui-claim-current-plot", "/plot claim", "claims:gui-confirm-button", "claims:gui-decline-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.PLOT_UNCLAIM, new CANYesNoTab(capi, iconHandler, "claims:gui-unclaim-current-plot", "/plot unclaim", "claims:gui-confirm-button", "claims:gui-decline-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.LEAVE_CITY_CONFIRM, new CANYesNoTab(capi, iconHandler, "claims:gui-leave-city-confirm-button", "/city leave", "claims:gui-confirm-button", "claims:gui-decline-button"));
            //this.TabDictionary.Add(EnumSecondaryWindowTab.CITY_RANK_REMOVE_CONFIRM, new CANYesNoTab(capi, iconHandler, ("claims:gui-strip-rank-from-player", "/city leave", "claims:gui-confirm-button", "claims:gui-decline-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.CITY_PLOTSGROUP_ADD_NEW_NEED_NAME, new CANNeedNameTab(capi, iconHandler, "claims:gui-enter-plotsgroup-name", "/c plotsgroup create ", "claims:gui-add-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.NEW_ALLIANCE_NEED_NAME, new CANNeedNameTab(capi, iconHandler, "claims:gui-enter-alliance-name", "/a create ", "claims:gui-create-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.INVITE_TO_ALLIANCE_NEED_NAME, new CANNeedNameTab(capi, iconHandler, "claims:gui-enter-alliance-name", "/a invite ", "claims:gui-add-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.KICK_FROM_ALLIANCE_NEED_NAME, new CANNeedNameTab(capi, iconHandler, "claims:gui-enter-alliance-name", "/a kick ", "claims:gui-remove-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.SELECT_NEW_ALLIANCE_NAME, new CANNeedNameTab(capi, iconHandler, "claims:gui-enter-alliance-name", "/a set name ", "claims:gui-set-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.ALLIANCE_PREFIX_NEED_NAME, new CANNeedNameTab(capi, iconHandler, "claims:gui-enter-alliance-name", "/a set prefix ", "claims:gui-set-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.LEAVE_ALLIANCE_CONFIRM, new CANYesNoTab(capi, iconHandler, "claims:gui-leave-alliance-confirm-button", "/alliance leave", "claims:gui-confirm-button", "claims:gui-decline-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.ALLIANCE_SEND_NEW_CONFLICT_LETTER_NEED_NAME, new CANDeclareConflictTab(capi, iconHandler, "/a conflict declare "));
            //this.TabDictionary.Add(EnumSecondaryWindowTab.ALLIANCE_SEND_PEACE_OFFER_CONFIRM, new CANYesNoTab(capi, iconHandler, "claims:gui_send_peace_offer", "/alliance conflict offerstop ", "claims:gui-confirm-button", "claims:gui-decline-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.CITY_RANK_CREATION_NEED_NAME, new CANNeedNameTab(capi, iconHandler, "claims:gui-enter-rank-name", "/c rank create ", "claims:gui-add-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.UNINVITE_TO_CITY, new CANNeedNameTab(capi, iconHandler, "claims:gui-enter-player-name", "/city uninvite ", "claims:gui-uninvite-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.PLOT_SET_PRICE_NEED_NUMBER, new CANNeedIntInputTab(capi, iconHandler, "claims:gui-enter-plot-price", "/plot fs ", "claims:gui-set-plot-price"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.PLOT_SET_TAX, new CANNeedDoubleInputTab(capi, iconHandler, "claims:gui-enter-plot-tax", "/plot set fee ", "claims:gui-set-plot-tax"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.SELECT_NEW_CITY_NAME, new CANNeedNameTab(capi, iconHandler, "claims:gui-enter-new-city-name", "/city set name ", "claims:gui-set-name-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.ADD_CRIMINAL_NEED_NAME, new CANNeedNameTab(capi, iconHandler, "claims:gui-enter-player-name", "/city criminal add ", "claims:gui-add-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.REMOVE_FRIEND, new CANRemoveFriendTab(capi, iconHandler, "claims:gui-enter-player-name", "/citizen friend remove ", "claims:gui-remove-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.PLOT_SET_TYPE, new CANChangePlotTypeTab(capi, iconHandler, "claims:gui-select-plot-type", "/plot set type ", "claims:gui-set-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.REMOVE_CRIMINAL, new CANRemoveCriminalTab(capi, iconHandler, "claims:gui-enter-player-name", "/city criminal remove ", "claims:gui-remove-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.CITY_RANK_REMOVE_CONFIRM, new CANYesNoWithTwoValuesTab(capi, iconHandler, "claims:gui-strip-rank-from-player", "/city rank remove", "claims:gui-confirm-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.CITY_RANK_ADD, new CANSelectPlayerForRankTab(capi, iconHandler, "claims:gui-select-player-to-add-rank", "/city rank add ", "claims:gui-add-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.PLOT_PERMISSIONS, new CANPermissionsTab(capi, iconHandler, "claims:gui-plot-permissions-title", "/city rank add ", "claims:gui-add-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.CITY_SUMMON_NEED_NAME, new CANYesNoSummonNameTab(capi, iconHandler, "claims:gui-enter-summon-point-name", "claims:gui-set-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.CITY_PLOTSGROUP_REMOVE_SELECT, new CANSelectPlotsGroupToRemoveTab(capi, iconHandler, "claims:gui-select-plotsgroup-to-remove", "/plot set type ", "claims:gui-set-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.CITY_PLOTSGROUP_REMOVE_CONFIRM, new CANYesNoWithTwoValuesTab(capi, iconHandler, "claims:gui-remove-plotsgroup-name", "/c plotsgroup delete ", "claims:gui-remove-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.CITY_PRISON_REMOVE_CELL_CONFIRM, new CANYesNoRemovePrisonCellTab(capi, iconHandler, "claims:gui-prison-cell-remove-confirm", "/c prison cremovecell ", "claims:gui-remove-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.ADD_PLOTSGROUP_MEMBER_NEED_NAME, new CANSelectPlotsGroupAddMemberTab(capi, iconHandler, "claims:gui-enter-player-name", "/c prison cremovecell ", "claims:gui-add-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.REMOVE_PLOTSGROUP_MEMBER_SELECT, new CANKickFromPlotsGroupTab(capi, iconHandler, "claims:gui-kick-from-plotsgroup", "", "claims:gui-kick-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.REMOVE_PLOTSGROUP_MEMBER_CONFIRM, new CANKickFromPlotsGroupConfirmTab(capi, iconHandler, "claims:gui-kick-player-from-plotsgroup", "", "claims:gui-kick-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.CITY_PLOTSGROUP_PERMISSIONS, new CANPlotsGroupPermissionsTab(capi, iconHandler, "claims:gui-plotsgroup-permissions", "", "claims:gui-add-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.CITY_PLOTSGROUP_PLOT_CLAIM_CONFIRM, new CANSelectPlotsGroupClaimTab(capi, iconHandler, "claims:gui-claim-plot-plotsgroup", "/c plotsgroup plotadd ", "claims:gui-confirm-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.CITY_PLOTSGROUP_PLOT_UNCLAIM_CONFIRM, new CANSelectPlotsGroupClaimTab(capi, iconHandler, "claims:gui-claim-plot-plotsgroup", "/c plotsgroup plotremove ", "claims:gui-confirm-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.ALLIANCE_SEND_NEW_UNION_LETTER_NEED_NAME, new CANNeedNameTab(capi, iconHandler, "claims:gui-enter-alliance-name", "/a union declare ", "claims:gui-confirm-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.CITY_PLOTS_PERMISSIONS, new CANCityPermissionsTab(capi, iconHandler, "claims:gui-city-permissions-title", "", "claims:gui-add-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.ALLIANCE_CANCEL_UNION_SELECT, new CANSelectUnionToLeaveTab(capi, iconHandler, "claims:gui-select-union-to-leave-title", "/a union revoke", "claims:gui-confirm-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.ALLIANCE_SEND_PEACE_OFFER_CONFIRM, new CANYesNoPieceOfferSendTab(capi, iconHandler, "claims:gui_send_peace_offer", "/alliance conflict offerstop ", "claims:gui-confirm-button", "claims:gui-decline-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.CITY_SEND_NEW_CONFLICT_LETTER_NEED_NAME, new CANDeclareConflictTab(capi, iconHandler, "/c war declare "));
            this.TabDictionary.Add(EnumSecondaryWindowTab.CITY_SET_FEE, new CANNeedIntInputTab(capi, iconHandler, "claims:gui-enter-city-fee", "/c set fee ", "claims:gui-set-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.CITY_WITHDRAW, new CANNeedIntInputTab(capi, iconHandler, "claims:gui-enter-city-withdraw-amount", "/city withdraw ", "claims:gui-city-withdraw-button"));
            this.TabDictionary.Add(EnumSecondaryWindowTab.CITY_DEPOSIT_CONFIRM, new CANYesNoTab(capi, iconHandler, "claims:gui-city-deposit-confirm", "/city deposit", "claims:gui-confirm-button", "claims:gui-decline-button"));
            this.iconHandler = iconHandler;
        }
        public void DrawTab(EnumSecondaryWindowTab selectedTab)
        {
            if (this.TabDictionary.TryGetValue(selectedTab, out var guiTab))
            {
                guiTab.DrawTab();
            }
        }
    }
}
