using claims.src.agreement;
using claims.src.auxialiry;
using claims.src.economy;
using claims.src.messages;
using claims.src.network.packets;
using claims.src.part;
using claims.src.part.structure;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace claims.src.commands
{
    /// <summary>
    /// Villages: the cheap entry-level settlement. A village is a <see cref="City"/> with
    /// <see cref="CityTier.VILLAGE"/>, so everything about plots, permissions and the map is shared
    /// with cities - only the founding, the supply upkeep and the feature gates live here.
    /// </summary>
    public partial class VillageCommand : BaseCommand
    {
        public static TextCommandResult CreateNewVillage(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            if (!claims.config.VILLAGE_ENABLED)
            {
                return TextCommandResult.Error("claims:villages_disabled");
            }
            if (playerInfo.hasCity())
            {
                return TextCommandResult.Error("claims:you_already_have_city");
            }
            PlotPosition currentPlotPosition = PlotPosition.fromXZ((int)player.Entity.Pos.X, (int)player.Entity.Pos.Z);
            // Villages have a distance rule of their own, usually laxer than the one for cities.
            if (!SettlementFounding.TryValidate(playerInfo, currentPlotPosition,
                    claims.config.VILLAGE_MIN_DISTANCE_FROM_CITY, (string)args.LastArg,
                    out string newName, out TextCommandResult error))
            {
                return error;
            }
            if (claims.config.VILLAGE_CREATE_COST > 0
                && claims.economyProvider.GetBalance(playerInfo.Guid) < (decimal)claims.config.VILLAGE_CREATE_COST)
            {
                return TextCommandResult.Error("claims:not_enough_for_new_city");
            }

            AgreementHandler.addNewAgreementOrReplace(new Agreement(
                () =>
                {
                    // Re-checked on confirm: both may have changed while the player was reading.
                    if (playerInfo.hasCity() || claims.dataStorage.GetPlot(currentPlotPosition, out _))
                    {
                        return;
                    }
                    if (claims.config.VILLAGE_CREATE_COST > 0
                        && claims.economyProvider.Withdraw(playerInfo.Guid, (decimal)claims.config.VILLAGE_CREATE_COST) != MoneyOperationResult.Success)
                    {
                        return;
                    }
                    PartInits.initNewCity(playerInfo, currentPlotPosition, newName, CityTier.VILLAGE);
                }, player.PlayerUID));

            claims.serverChannel.SendPacket(new SavedPlotsPacket()
            {
                type = PacketsContentEnum.AGREE_NEEDED_ON_NEW_CITY_CREATION,
                data = newName
            }, player);

            return SuccessWithParams("claims:help_agreement_new_village", new object[] { claims.config.AGREEMENT_COMMAND });
        }

        public static TextCommandResult VillageInfo(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out _, out var playerInfo, out var callerErr)) return callerErr;
            if (!playerInfo.hasCity())
            {
                return TextCommandResult.Error("claims:you_dont_have_city");
            }
            City village = playerInfo.City;
            if (!village.IsVillage())
            {
                return TextCommandResult.Error("claims:not_a_village");
            }
            return TextCommandResult.Success(string.Join("", village.getStatus(playerInfo)));
        }

        public static TextCommandResult UpgradeVillage(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            City village = playerInfo.City;
            if (village == null)
            {
                return TextCommandResult.Error("claims:you_dont_have_city");
            }
            if (!village.IsVillage())
            {
                return TextCommandResult.Error("claims:not_a_village");
            }
            if (!village.isMayor(playerInfo))
            {
                return TextCommandResult.Error("claims:you_dont_have_right_for_that_command");
            }
            if (village.getCityCitizens().Count < claims.config.VILLAGE_UPGRADE_MIN_CITIZENS)
            {
                return ErrorWithParams("claims:village_upgrade_needs_citizens",
                    new object[] { claims.config.VILLAGE_UPGRADE_MIN_CITIZENS });
            }
            // Paid by the head personally: the village has no treasury of its own, and the city
            // account only starts being used by this very upgrade.
            if (claims.config.VILLAGE_UPGRADE_COST > 0
                && claims.economyProvider.GetBalance(playerInfo.Guid) < (decimal)claims.config.VILLAGE_UPGRADE_COST)
            {
                return ErrorWithParams("claims:village_upgrade_not_enough_money",
                    new object[] { claims.config.VILLAGE_UPGRADE_COST });
            }
            bool paid = claims.config.VILLAGE_UPGRADE_COST > 0;
            if (paid && claims.economyProvider.Withdraw(playerInfo.Guid, (decimal)claims.config.VILLAGE_UPGRADE_COST) != MoneyOperationResult.Success)
            {
                return TextCommandResult.Error("claims:economy_money_transaction_error");
            }

            try
            {
                VillageUpgradeHelper.UpgradeToCity(village);
            }
            catch (System.Exception ex)
            {
                // Money is already gone at this point; hand it back rather than charge for nothing.
                if (paid) claims.economyProvider.Deposit(playerInfo.Guid, (decimal)claims.config.VILLAGE_UPGRADE_COST);
                claims.sapi.Logger.Error("[claims] Village upgrade of '{0}' failed: {1}", village.GetPartName(), ex);
                return TextCommandResult.Error("claims:economy_money_transaction_error");
            }
            MessageHandler.sendGlobalMsg(Lang.Get("claims:village_became_city",
                village.getPartNameReplaceUnder(), playerInfo.getPartNameReplaceUnder()));
            return TextCommandResult.Success("claims:village_upgrade_done");
        }

        public static TextCommandResult AbandonVillage(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var callerErr)) return callerErr;
            City village = playerInfo.City;
            if (village == null)
            {
                return TextCommandResult.Error("claims:you_dont_have_city");
            }
            if (!village.IsVillage())
            {
                return TextCommandResult.Error("claims:not_a_village");
            }
            if (!village.isMayor(playerInfo))
            {
                return TextCommandResult.Error("claims:you_dont_have_right_for_that_command");
            }
            // Otherwise the head of a village simply dissolves it when the raid window opens and
            // puts it back an hour later, untouched.
            if (VillageRaidHelper.IsRaidWindowOpen(village))
            {
                return TextCommandResult.Error("claims:village_abandon_during_raid");
            }

            AgreementHandler.addNewAgreementOrReplace(new Agreement(
                () =>
                {
                    // The window may have opened while the player was reading the confirmation.
                    if (VillageRaidHelper.IsRaidWindowOpen(village))
                    {
                        MessageHandler.sendMsgToPlayer(player, Lang.Get("claims:village_abandon_during_raid"));
                        return;
                    }
                    MessageHandler.sendGlobalMsg(Lang.Get("claims:village_has_been_abandoned",
                        village.getPartNameReplaceUnder(), playerInfo.getPartNameReplaceUnder()));
                    // Registered before the demolition, which would hand out the longer
                    // "your village fell" cooldown instead.
                    VillageCooldownHelper.RegisterAbandonedVillage(village, playerInfo);
                    PartDemolition.demolishCity(village, string.Format("Abandoned by player {0}", player.PlayerName),
                        applyVillagePenalty: false);
                }, player.PlayerUID));
            return SuccessWithParams("claims:help_agreement_abandon_village", new object[] { claims.config.AGREEMENT_COMMAND });
        }
    }
}
