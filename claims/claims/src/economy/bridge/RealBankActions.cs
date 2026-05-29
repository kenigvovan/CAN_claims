using System.Collections.Generic;
using caneconomy.src.implementations.RealMoney;
using caneconomy.src.interfaces;
using claims.src.auxialiry;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace claims.src.economy.bridge
{
    public static class RealBankActions
    {
        private static RealMoneyEconomyHandler RealHandler => caneconomy.caneconomy.getHandler() as RealMoneyEconomyHandler;

        public static void OnBlockRemoved(BlockEntityOpenableContainer be)
        {
            if (!(be is BlockEntityGenericTypedContainer)) return;

            var handler = RealHandler;
            if (handler == null) return;

            Vec3i tmp = new Vec3i(be.Pos);
            if (!handler.TryGetRealBankInfo(tmp, out RealBankInfo rbi)) return;

            (handler as EconomyHandler).deleteAccount(rbi.AccountName);

            string accName = rbi.AccountName;
            if (accName.StartsWith(claims.config.CITY_ACCOUNT_STRING_PREFIX))
            {
                claims.dataStorage.GetCityByName(accName.Substring(claims.config.CITY_ACCOUNT_STRING_PREFIX.Length), out City city);
                if (city != null)
                {
                    MessageHandler.sendMsgToPlayerInfo(city.getMayor(), Lang.Get("claims:city_bank_was_destroyed"));
                }
            }
            else
            {
                claims.dataStorage.GetPlayerByUid(accName, out PlayerInfo playerInfo);
                if (playerInfo != null)
                {
                    MessageHandler.sendMsgToPlayerInfo(playerInfo, Lang.Get("claims:your_bank_was_destroyed"));
                }
            }
        }

        public static void OnButtonSave(BlockEntitySign __instance, IPlayer player, int packetid)
        {
            if (packetid != 1002) return;

            var handler = RealHandler;
            if (handler == null) return;

            string signCodePath = __instance.Block.Code.Path;
            BlockPos chestPos = new(__instance.Pos.X, __instance.Pos.Y, __instance.Pos.Z);
            if (signCodePath.EndsWith("east")) chestPos.X += 1;
            else if (signCodePath.EndsWith("west")) chestPos.X -= 1;
            else if (signCodePath.EndsWith("north")) chestPos.Z -= 1;
            else if (signCodePath.EndsWith("south")) chestPos.Z += 1;

            claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo);

            string signText = __instance.text.Replace("\n", "");

            if (signText.StartsWith(Lang.Get("claims:economy_chest_bank_city_sign_prefix")))
            {
                claims.dataStorage.GetPlot(PlotPosition.fromXZ(chestPos.X, chestPos.Z), out Plot plot);
                if (plot == null) return;

                City city = plot.getCity();
                if (city == null)
                {
                    MessageHandler.sendMsgToPlayer(player as IServerPlayer, Lang.Get("claims:economy_there_is_no_city_in_this_plot"));
                    return;
                }
                if (signText.Substring(Lang.Get("claims:economy_chest_bank_city_sign_prefix").Length) != city.GetPartName().Replace("_", " "))
                {
                    MessageHandler.sendMsgToPlayer(player as IServerPlayer, Lang.Get("claims:economy_different_name_city_plot_sign"));
                    return;
                }
                if (playerInfo == null || !city.isMayor(playerInfo))
                {
                    MessageHandler.sendMsgToPlayer(player as IServerPlayer, Lang.Get("claims:economy_you_need_to_be_mayor"));
                    return;
                }

                EconomyHandler eh = handler;
                if (handler.TryGetRealBankInfo(city.MoneyAccountName, out RealBankInfo tmpVecCity))
                {
                    Vec3i RBIchestCoords = tmpVecCity.getChestCoors();
                    if (RBIchestCoords.X == chestPos.X && RBIchestCoords.Z == chestPos.Z && RBIchestCoords.Y == chestPos.Y)
                    {
                        MessageHandler.sendMsgToPlayer(player as IServerPlayer, Lang.Get("claims:economy_chest_bank_already_set_here"));
                        return;
                    }
                    if (eh.accountExist(city.MoneyAccountName))
                    {
                        eh.deleteAccount(city.MoneyAccountName);
                        MessageHandler.sendMsgToPlayer(player as IServerPlayer, Lang.Get("claims:economy_city_chest_bank_removed"));
                    }
                    eh.newAccount(city.MoneyAccountName, new Dictionary<string, object> { { "chestPos", new Vec3i(chestPos.X, chestPos.Y, chestPos.Z) } });
                    MessageHandler.sendMsgToPlayer(player as IServerPlayer, Lang.Get("claims:economy_city_chest_bank_created"));
                    return;
                }
                else
                {
                    if (eh.accountExist(city.MoneyAccountName))
                    {
                        eh.deleteAccount(city.MoneyAccountName);
                        MessageHandler.sendMsgToPlayer(player as IServerPlayer, Lang.Get("claims:economy_city_chest_bank_removed"));
                    }
                    eh.newAccount(city.MoneyAccountName, new Dictionary<string, object> { { "chestPos", new Vec3i(chestPos.X, chestPos.Y, chestPos.Z) } });
                    MessageHandler.sendMsgToPlayer(player as IServerPlayer, Lang.Get("claims:economy_city_chest_bank_created"));
                    return;
                }
            }
            else if (signText.StartsWith(Lang.Get("claims:economy_chest_bank_player_sign_prefix")))
            {
                if (!player.PlayerName.Equals(signText.Substring(Lang.Get("claims:economy_chest_bank_player_sign_prefix").Length)))
                {
                    MessageHandler.sendMsgToPlayer(player as IServerPlayer, Lang.Get("claims:economy_make_only_own_chest"));
                    return;
                }
                EconomyHandler eh = handler;
                if (handler.TryGetRealBankInfo(player.PlayerUID, out RealBankInfo tmpVecPlayer))
                {
                    Vec3i RBIchestCoords = tmpVecPlayer.getChestCoors();
                    if (tmpVecPlayer != null && RBIchestCoords.X == chestPos.X && RBIchestCoords.Z == chestPos.Z && RBIchestCoords.Y == chestPos.Y)
                    {
                        MessageHandler.sendMsgToPlayer(player as IServerPlayer, Lang.Get("claims:economy_chest_bank_already_set_here"));
                        return;
                    }
                    MessageHandler.sendMsgToPlayer(player as IServerPlayer, Lang.Get("claims:economy_chest_bank_already_set_there", RBIchestCoords.X, RBIchestCoords.Y, RBIchestCoords.Z));
                    return;
                }
                else
                {
                    if (eh.accountExist(player.PlayerUID))
                    {
                        eh.deleteAccount(player.PlayerUID);
                        MessageHandler.sendMsgToPlayer(player as IServerPlayer, Lang.Get("claims:economy_player_chest_bank_removed"));
                    }
                    eh.newAccount(player.PlayerUID, new Dictionary<string, object> { { "chestPos", new Vec3i(chestPos.X, chestPos.Y, chestPos.Z) } });
                    MessageHandler.sendMsgToPlayer(player as IServerPlayer, Lang.Get("claims:economy_player_chest_bank_created"));
                    return;
                }
            }
            else if (signText.StartsWith(Lang.Get("claims:economy_chest_bank_alliance_sign_prefix")))
            {
                claims.dataStorage.GetPlot(PlotPosition.fromXZ(chestPos.X, chestPos.Z), out Plot plot);
                if (plot == null) return;

                City city = plot.getCity();
                if (city == null)
                {
                    MessageHandler.sendMsgToPlayer(player as IServerPlayer, Lang.Get("claims:economy_there_is_no_city_in_this_plot"));
                    return;
                }
                if (signText.Substring(Lang.Get("claims:economy_chest_bank_alliance_sign_prefix").Length) != city.Alliance.GetPartName().Replace("_", " "))
                {
                    MessageHandler.sendMsgToPlayer(player as IServerPlayer, Lang.Get("claims:economy_different_alliance"));
                    return;
                }
                Alliance alliance = playerInfo.Alliance;
                if (alliance == null || !city.Alliance.Equals(alliance) || !playerInfo.Alliance.IsLeader(playerInfo)) return;

                EconomyHandler eh = handler;
                if (handler.TryGetRealBankInfo(alliance.MoneyAccountName, out RealBankInfo tmpVecAlliance))
                {
                    Vec3i RBIchestCoords = tmpVecAlliance.getChestCoors();
                    if (tmpVecAlliance != null && RBIchestCoords.X == chestPos.X && RBIchestCoords.Z == chestPos.Z && RBIchestCoords.Y == chestPos.Y)
                    {
                        MessageHandler.sendMsgToPlayer(player as IServerPlayer, Lang.Get("claims:economy_chest_bank_already_set_here"));
                        return;
                    }
                }
                else
                {
                    if (eh.accountExist(alliance.MoneyAccountName))
                    {
                        eh.deleteAccount(alliance.MoneyAccountName);
                        MessageHandler.sendMsgToPlayer(player as IServerPlayer, Lang.Get("claims:economy_alliance_chest_bank_removed"));
                    }
                    eh.newAccount(alliance.MoneyAccountName, new Dictionary<string, object> { { "chestPos", new Vec3i(chestPos.X, chestPos.Y, chestPos.Z) } });
                    MessageHandler.sendMsgToPlayer(player as IServerPlayer, Lang.Get("claims:economy_alliance_chest_bank_created"));
                }
            }
            else
            {
                MessageHandler.sendMsgToPlayer(player as IServerPlayer, Lang.Get("claims:economy_help_creation"));
            }
        }
    }
}
