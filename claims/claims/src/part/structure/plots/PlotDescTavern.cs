using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace claims.src.part.structure.plots
{
    public class PlotDescTavern : PlotDesc
    {
        /// <summary>
        /// Rooms saved by the old tavern inner claims, as they were stored, until
        /// subplots.TavernRoomMigration turns them into sub-plots on load. Nothing writes new ones.
        /// </summary>
        public string LegacyRooms = "";

        public override string Serialize(Plot plot) => LegacyRooms;
        public override void Deserialize(string data, Plot plot) => LegacyRooms = data ?? "";
        public override bool Validate(Plot plot, IServerPlayer player, ref TextCommandResult tcr)
        {
            int count = plot.getCity().getCityPlots().Count(p => p.Type == PlotType.TAVERN);
            if (count >= claims.config.MAX_NUMBER_TAVERN_PER_CITY)
            {
                tcr.StatusMessage = "claims:too_much_taverns";
                tcr.MessageParams = new object[] { claims.config.MAX_NUMBER_TAVERN_PER_CITY };
                return false;
            }
            return true;
        }
    }
}
