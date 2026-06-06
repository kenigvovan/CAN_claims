using claims.src.auxialiry;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.part;
using claims.src.part.structure;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.part.structure.plots
{
    public class PlotDescPrison : PlotDesc
    {
        public string prisonGuid { get; set; }

        public PlotDescPrison(string guid) { prisonGuid = guid; }
        public PlotDescPrison() { }

        public override string Serialize(Plot plot) => prisonGuid;

        public override void Deserialize(string data, Plot plot)
        {
            prisonGuid = data;
            claims.dataStorage.getPrison(data, out Prison prison);
            plot.Prison = prison;
        }

        public override void OnActivated(Plot plot, IServerPlayer player, string newTypeName, ref TextCommandResult tcr)
        {
            PartInits.initPrison(plot, plot.getCity(), player);
            UsefullPacketsSend.AddToQueueCityInfoUpdate(plot.getCity().Guid,
                new Dictionary<string, object> { { "value", new PrisonCellElement(player.Entity.Pos.AsBlockPos.AsVec3i.Clone(), new HashSet<string>()) } },
                EnumPlayerRelatedInfo.CITY_ADD_PRISON_CELL);
            tcr.StatusMessage = "claims:plot_set_type";
            tcr.MessageParams = new object[] { newTypeName };
        }

        public override void OnDeactivated(Plot plot)
        {
            EntityPos ep = claims.sapi.World.DefaultSpawnPosition;
            foreach (PrisonCellInfo cell in plot.Prison.getPrisonCells())
            {
                foreach (PlayerInfo player in cell.getPlayerInfos())
                {
                    IServerPlayer onlinePlayer = claims.sapi.World.PlayerByUid(player.Guid) as IServerPlayer;
                    if (onlinePlayer != null)
                    {
                        onlinePlayer.SetSpawnPosition(new PlayerSpawnPos((int)ep.X, (int)ep.Y, (int)ep.Z));
                        onlinePlayer.Entity.TeleportToDouble(ep.X, ep.Y, ep.Z);
                        player.PrisonHoursLeft = 0;
                    }
                    else
                    {
                        player.PrisonHoursLeft = -1;
                    }
                    player.PrisonedIn = null;
                    player.saveToDatabase();
                }
            }
            claims.dataStorage.removePrison(plot.Prison.Guid);
            if (plot.Prison.City != null)
            {
                plot.Prison.City.getPrisons().Remove(plot.Prison);
                plot.Prison.City.saveToDatabase();
            }
            plot.Prison.Plot.Type = PlotType.DEFAULT;
            claims.getModInstance().getDatabaseHandler().deleteFromDatabasePrison(plot.Prison);
            plot.Prison = null;
            plot.saveToDatabase();
        }
    }
}
