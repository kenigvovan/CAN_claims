using claims.src.auxialiry;
using claims.src.cityplotsgroups;
using claims.src.delayed.invitations;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.part;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace claims.src.timers
{
    public class HourTimer
    {
        public void Run()
        {
            //process invites
            InvitationHandler.findAndDeleteOverdueInvitations();
            CityPlotsGroupInvitationsHandler.updateCityPlotsGroupInvitations();

            //announced plots group fee raises whose waiting period is over
            PlotsGroupFeeHelper.SettleDueRaises();

            //villages live off their granary, not off money, so their upkeep is hourly
            part.structure.VillageSupplyHelper.ProcessVillages();
            part.structure.VillageCooldownHelper.PurgeExpired();

            //prison hours decrease and tp freed players
            foreach(PlayerInfo player in claims.dataStorage.getPlayersDict().Values.ToArray())
            {
                if(player.PrisonHoursLeft == 1)
                {
                    EntityPos ep = claims.sapi.World.DefaultSpawnPosition;
                    IServerPlayer onlinePlayer = claims.sapi.World.PlayerByUid(player.Guid) as IServerPlayer;
                    if (onlinePlayer != null)
                    {
                        onlinePlayer.Entity.TeleportToDouble(ep.X, ep.Y, ep.Z);
                        onlinePlayer.SetSpawnPosition(new PlayerSpawnPos((int)ep.X, (int)ep.Y, (int)ep.Z));
                        player.PrisonHoursLeft = 0;
                    }
                    else
                    {
                        player.PrisonHoursLeft = -1; // released while offline, teleport on next login
                    }
                    // Out of the cell as well, not just out of prison: the roster is persisted and
                    // shown in the city window, so a served sentence left the player sitting there.
                    if (player.PrisonedIn != null
                        && player.PrisonedIn.TryGetCellInWhichPlayer(player, out var servedCell))
                    {
                        servedCell.RemovePlayer(player);
                        if (player.PrisonedIn.City != null)
                        {
                            UsefullPacketsSend.AddToQueueCityInfoUpdate(player.PrisonedIn.City.Guid,
                                new Dictionary<string, object> { { "value", new PrisonCellElement(servedCell.spawnPostion, servedCell.playerNames) } },
                                EnumPlayerRelatedInfo.CITY_CELL_PRISON_UPDATE);
                        }
                    }
                    player.PrisonedIn = null;
                }
                else if(player.PrisonHoursLeft > 1)
                {
                    player.PrisonHoursLeft = player.PrisonHoursLeft - 1;
                }
                player.saveToDatabase();
            }

            claims.sapi.Event.RegisterCallback((dt =>
            {
                new HourTimer().Run();
            }), (int)TimeFunctions.getSecondsBeforeNextHourStart() * 1000);
        }
    }
}
