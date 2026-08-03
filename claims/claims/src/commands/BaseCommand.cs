using claims.src.part;
using claims.src.part.structure;
using claims.src.rights;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace claims.src.commands
{
    public class BaseCommand
    {
        /// <summary>
        /// Precondition body: allows the command if the calling player has ANY of the given permissions.
        /// Use inside .WithPreCondition: <c>(args) => BaseCommand.RequirePermission(args, EnumPlayerPermissions.X)</c>
        /// </summary>
        public static TextCommandResult RequirePermission(TextCommandCallingArgs args, params EnumPlayerPermissions[] anyPermission)
        {
            if (args.Caller.Player is IServerPlayer player)
            {
                if (CheckForPlayerPermissions(player, anyPermission))
                {
                    return TextCommandResult.Success();
                }
                return TextCommandResult.Error(Lang.Get("claims:you_dont_have_right_for_that_command"));
            }
            return TextCommandResult.Error("");
        }

        /// <summary>
        /// Precondition body: allows the command only if the calling player's role is in ROLE_CODES_WITH_ADMIN_RIGHTS.
        /// Use inside .WithPreCondition: <c>(args) => BaseCommand.RequireAdminRole(args)</c>
        /// </summary>
        public static TextCommandResult RequireAdminRole(TextCommandCallingArgs args)
        {
            if (args.Caller.Player is IServerPlayer player)
            {
                if (!claims.config.ROLE_CODES_WITH_ADMIN_RIGHTS.Contains(player.Role.Code))
                {
                    return TextCommandResult.Error(Lang.Get("claims:you_dont_have_right_for_that_command"));
                }
                return TextCommandResult.Success();
            }
            return TextCommandResult.Error("");
        }

        public static bool TryResolveCaller(TextCommandCallingArgs args, out IServerPlayer player, out PlayerInfo playerInfo, out TextCommandResult err)
        {
            player = args.Caller.Player as IServerPlayer;
            if (player == null)
            {
                playerInfo = null;
                err = TextCommandResult.Error("");
                return false;
            }
            if (!claims.dataStorage.GetPlayerByUid(player.PlayerUID, out playerInfo))
            {
                err = TextCommandResult.Error(Lang.Get("claims:no_such_player_info"));
                return false;
            }
            err = null;
            return true;
        }

        public static bool getBoolFromString(string str)
        {
            if (str.Equals("on"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool isOwnerOfPlotMayorAdmin(Plot plot, PlayerInfo playerInfo, IServerPlayer player)
        {
            if(claims.config.ROLE_CODES_WITH_ADMIN_RIGHTS.Contains(player.Role.Code))
            {
                return true;
            }
            if (plot.hasPlotOwner() && plot.getPlotOwner().Equals(playerInfo))
            {
                return true;
            }
            else if (playerInfo.hasCity() && plot.hasCity() && playerInfo.City.Equals(plot.getCity()) && plot.getCity().isMayor(playerInfo))
            {
                return true;
            }
            return false;
        }
        public static bool isMayorAdmin(Plot plot, PlayerInfo playerInfo, IServerPlayer player)
        {
            if (claims.config.ROLE_CODES_WITH_ADMIN_RIGHTS.Contains(player.Role.Code))
            {
                return true;
            }
            else if (playerInfo.hasCity() && plot.hasCity() && playerInfo.City.Equals(plot.getCity()) && plot.getCity().isMayor(playerInfo))
            {
                return true;
            }
            return false;
        }
        public static bool CheckForPlayerPermissions(IServerPlayer player, EnumPlayerPermissions[] anyPermission)
        {
            if (claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                foreach (var permission in anyPermission)
                {
                    if (playerInfo.PlayerPermissionsHandler.HasPermission(permission))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static TextCommandResult SuccessWithParams(string msg, object[] msgParams)
        {
            return new TextCommandResult
            {
                Status = EnumCommandStatus.Success,
                MessageParams = msgParams,
                StatusMessage = msg
            };
        }

        public static TextCommandResult ErrorWithParams(string msg, object[] msgParams)
        {
            return new TextCommandResult
            {
                Status = EnumCommandStatus.Error,
                MessageParams = msgParams,
                StatusMessage = msg
            };
        }
    }
}
