using System.Collections.Generic;
using System.Linq;
using System.Text;
using claims.src.auxialiry;
using claims.src.delayed.invitations;
using claims.src.messages;
using claims.src.part;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace claims.src.commands
{
    public class CitizenCommand:BaseCommand
    {
        /*==============================================================================================*/
        /*=====================================GENERAL==================================================*/
        /*==============================================================================================*/
        public static TextCommandResult CitizenInfo(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            //claims.sapi.SendIngameDiscovery(player as IServerPlayer, "ingamediscovery-battle-start", Lang.Get("claims:ingamediscovery-battle-start"), new object[] { 1, 2 });
            //claims.sapi.World.PlaySoundAt(new AssetLocation("game:sounds/effect/deepbell"), player.Entity, null, false, 5f, 0.5f);
            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
          
            string name = Filter.filterName((string)args.LastArg);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Success("claims:invalid_player_name");
            }
            claims.dataStorage.getPlayerByName(name, out PlayerInfo targetPlayer);
            if (targetPlayer == null)
            {
                return TextCommandResult.Success("claims:invalid_player_name");
            }
            return TextCommandResult.Success(string.Join("", targetPlayer.getStatus()));
        }
        public static TextCommandResult CitizenPrices(TextCommandCallingArgs args)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Lang.Get("claims:new_city_price", claims.config.NEW_CITY_COST));
            sb.Append("\n");
            sb.Append(Lang.Get("claims:new_plot_claim_price", claims.config.PLOT_CLAIM_PRICE));

            sb.Append("\n");
            sb.Append(Lang.Get("claims:plot_type_taxes", claims.config.PLOT_CLAIM_PRICE));
            sb.Append("\n");
            sb.Append(Lang.Get("claims:summon_plot_cost", claims.config.SUMMON_PLOT_COST));
            sb.Append(", ");
            sb.Append(Lang.Get("claims:embassy_plot_cost", claims.config.EMBASSY_PLOT_COST));
            sb.Append(", ");
            sb.Append(Lang.Get("claims:default_plot_cost", claims.config.DEFAULT_PLOT_COST));
            sb.Append(", ");
            sb.Append(Lang.Get("claims:tavern_plot_cost", claims.config.TAVERN_PLOT_COST));
            sb.Append(", ");
            sb.Append(Lang.Get("claims:prison_plot_cost", claims.config.PRISON_PLOT_COST));
            sb.Append("\n");
            sb.Append(Lang.Get("claims:no_pvp_for_plot", claims.config.PLOT_NO_PVP_FLAG_COST));
            sb.Append("\n");
            sb.Append(Lang.Get("claims:extra_plot_cost", claims.config.EXTRA_PLOT_COST));
            sb.Append("\n");
            sb.Append(Lang.Get("claims:outpost_plot_cost", claims.config.OUTPOST_PLOT_COST));
            sb.Append("\n");
            return TextCommandResult.Success(sb.ToString());
        }
        public static TextCommandResult NextDayTimer(TextCommandCallingArgs args)
        {
            long secondsBeforeNextDay = TimeFunctions.getSecondsBeforeNextDayStart();
            StringBuilder sb = new StringBuilder();
            StringBuilder itervalToAppend = new StringBuilder();
            if ((secondsBeforeNextDay / TimeFunctions.secondsInAnHour) > 0)
            {
                itervalToAppend.Append((secondsBeforeNextDay / TimeFunctions.secondsInAnHour) + " hours ");
            }
            secondsBeforeNextDay %= TimeFunctions.secondsInAnHour;
            if ((secondsBeforeNextDay / 60) > 0)
            {
                itervalToAppend.Append((secondsBeforeNextDay / 60) + " minutes ");
            }
            if ((secondsBeforeNextDay % 60) > 0)
            {
                itervalToAppend.Append((secondsBeforeNextDay % 60) + " seconds");
            }
            return SuccessWithParams("claims:next_day_timer", new object[] { itervalToAppend.ToString() });
        }
        /*==============================================================================================*/
        /*=====================================INVITES==================================================*/
        /*==============================================================================================*/
        public static TextCommandResult InvitesList(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            List<Invitation> listInvitations = playerInfo.getReceivedInvitations();
            if (listInvitations.Count == 0)
            {
                return TextCommandResult.Success("claims:no_invitations");
            }
            if (args.LastArg == null)
            {
                return TextCommandResult.Success(StringFunctions.getNthPageOf(listInvitations, 1));
            }
            else
            {
                return TextCommandResult.Success(StringFunctions.getNthPageOf(listInvitations, (int)args.LastArg));
            }
        }
        /*==============================================================================================*/
        /*=====================================FRIEND===================================================*/
        /*==============================================================================================*/
        public static TextCommandResult FriendsList(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);

            return TextCommandResult.Success(string.Join("", StringFunctions.makeStringPlayersName(playerInfo.Friends.ToList(), ',')));
        }
        public static TextCommandResult FriendAdd(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);

            string name = Filter.filterName((string)args.LastArg);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Success("claims:invalid_player_name");
            }
            claims.dataStorage.getPlayerByName(name, out PlayerInfo targetPlayer);
            if (targetPlayer == null)
            {
                return TextCommandResult.Success("claims:invalid_player_name");
            }
            if (targetPlayer.Guid.Equals(playerInfo.Guid))
            {
                return TextCommandResult.Success("claims:can_not_add_yourself_as_friend");
            }
            if (playerInfo.addComrade(targetPlayer))
            {
                UsefullPacketsSend.AddToQueuePlayerInfoUpdate(playerInfo.Guid, gui.playerGui.structures.EnumPlayerRelatedInfo.FRIENDS);
                MessageHandler.sendMsgToPlayerInfo(targetPlayer, Lang.Get("claims:player_added_you_as_comrade", playerInfo.getPartNameReplaceUnder()));
                playerInfo.saveToDatabase();
                return SuccessWithParams("claims:was_added_as_comrade", new object[] { targetPlayer.getPartNameReplaceUnder() });
            }
            else
            {
                return SuccessWithParams("claims:already_added_as_comrade", new object[] { targetPlayer.getPartNameReplaceUnder() });
            }
        }
        public static TextCommandResult FriendRemove(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;

            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);

            string name = Filter.filterName((string)args.LastArg);
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                return TextCommandResult.Success("claims:invalid_player_name");
            }
            claims.dataStorage.getPlayerByName(name, out PlayerInfo targetPlayer);
            if (targetPlayer == null)
            {
                return TextCommandResult.Success("claims:invalid_player_name");
            }
            if (playerInfo.removeComrade(targetPlayer))
            {
                UsefullPacketsSend.AddToQueuePlayerInfoUpdate(playerInfo.Guid, gui.playerGui.structures.EnumPlayerRelatedInfo.FRIENDS);
                MessageHandler.sendMsgToPlayerInfo(targetPlayer, Lang.Get("claims:you_were_remove_from_comrades_by", playerInfo.getPartNameReplaceUnder()));
                playerInfo.saveToDatabase();
                return SuccessWithParams("claims:was_removed_from_comrades", new object[] { targetPlayer.getPartNameReplaceUnder() });
            }
            else
            {
                return SuccessWithParams("claims:was_removed_from_comrades", new object[] { targetPlayer.getPartNameReplaceUnder() });
            }
        }
        /*
        
        public static TextCommandResult citizenSetInfo(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo ourPlayer);
            if (ourPlayer != null)
            {
                tcr.StatusMessage = ourPlayer.PermsHandler.getStringForChat() + "\n";
            }
            return tcr;
        }
        public static TextCommandResult SetPerm(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            if(args.RawArgs.Length < 3)
            {
                return tcr;
            }
            
            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            playerInfo.PermsHandler.setAccessPerm(args.RawArgs, tcr);
            playerInfo.saveToDatabase();
            return tcr;
        }
        public static TextCommandResult processFee(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            double futurePayment = 0;
            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            //But it is only plots, where he is an owner
            foreach (Plot plot in playerInfo.PlayerPlots)
            {
                //PlotInfo.dictPlotTypes.TryGetValue(plot.getType(), out PlotInfo plotInfo);
                futurePayment += plot.getCustomTax();
            }

            // Same conditions the day timer charges by, so the answer here is what will be taken:
            // once per group, nothing from the mayor, nothing for a group holding no land.
            double plotsGroupFee = 0;
            foreach(CityPlotsGroup group in claims.dataStorage.getCityPlotsGroupsDict().Values)
            {
                if (!group.HasFee() || !group.PlayersList.Contains(playerInfo)) continue;
                if (group.City?.isMayor(playerInfo) ?? false) continue;
                if (!(group.City?.getCityPlots().Any(p => p.hasPlotGroup() && group.Equals(p.getPlotGroup())) ?? false))
                {
                    continue;
                }
                plotsGroupFee += group.PlotsGroupFee;
            }

            tcr.StatusMessage = "claims:fees_city_for_player";
            tcr.MessageParams = new object[] { playerInfo.PlayerPlots.Count, plotsGroupFee, (plotsGroupFee + futurePayment).ToString() };
            return tcr;
        }
        public static TextCommandResult prisonInfo(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();

            StringBuilder sb = new StringBuilder();
            sb.Append(Lang.Get("claims:ransom_prices") + "\n");
            sb.Append(Lang.Get("claims:stranger") + claims.config.RANSOM_FOR_NO_CITIZEN.ToString() + "\n");
            sb.Append(Lang.Get("claims:citizen") + claims.config.RANSOM_FOR_NO_CITIZEN.ToString() + "\n");
            sb.Append(Lang.Get("claims:chief") + claims.config.RANSOM_FOR_CHIEF.ToString() + "\n");
            sb.Append(Lang.Get("claims:mayor") + claims.config.RANSOM_FOR_MAYOR.ToString() + "\n");
            sb.Append(Lang.Get("claims:leader") + claims.config.RANSOM_FOR_LEADER.ToString() + "\n");
            tcr.StatusMessage = sb.ToString();
            tcr.Status = EnumCommandStatus.Success;
            return tcr;
        }
        public static TextCommandResult processPrisonPayout(TextCommandCallingArgs args)
        {
            IServerPlayer player = args.Caller.Player as IServerPlayer;
            TextCommandResult tcr = new TextCommandResult();
            tcr.Status = EnumCommandStatus.Success;

            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);
            if(!playerInfo.isPrisoned())
            {
                tcr.StatusMessage = "claims:you_are_not_prisoned";
                return tcr;
            }
            if(playerInfo.Account.getBalance() < playerInfo.getRansomPrice())
            {
                tcr.StatusMessage = "claims:not_enough_money";
                return tcr;
            }
            else
            {
                tcr.StatusMessage = "claims:you_paid_ransom";
                EntityPos ep = claims.sapi.World.DefaultSpawnPosition;
                (claims.sapi.World.PlayerByUid(playerInfo.Guid) as IServerPlayer).SetSpawnPosition(new PlayerSpawnPos((int)ep.X, (int)ep.Y, (int)ep.Z));
                (claims.sapi.World.PlayerByUid(playerInfo.Guid) as IServerPlayer).
                    Entity.TeleportToDouble(ep.X, ep.Y, ep.Z);
                return tcr;                
            }           
        }
       
      
        
        
        */
    }
}
