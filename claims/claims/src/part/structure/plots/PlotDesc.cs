using claims.src.part.structure;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace claims.src.part.structure.plots
{
    public class PlotDesc
    {
        public static PlotDesc Create(PlotType type, IServerPlayer player) => type switch
        {
            PlotType.SUMMON  => new PlotDescSummon(player.Entity.Pos.XYZ),
            PlotType.TAVERN  => new PlotDescTavern(),
            PlotType.PRISON  => new PlotDescPrison(),
            PlotType.TEMPLE  => new PlotDescTemple(),
            PlotType.EMBASSY => new PlotDescEmbassy(),
            _                => new PlotDesc()
        };

        public static PlotDesc Load(PlotType type) => type switch
        {
            PlotType.SUMMON  => new PlotDescSummon(),
            PlotType.TAVERN  => new PlotDescTavern(),
            PlotType.PRISON  => new PlotDescPrison(),
            PlotType.TEMPLE  => new PlotDescTemple(),
            PlotType.EMBASSY => new PlotDescEmbassy(),
            _                => null
        };

        public virtual string Serialize(Plot plot) => "";
        public virtual void Deserialize(string data, Plot plot) { }
        public virtual bool Validate(Plot plot, IServerPlayer player, ref TextCommandResult tcr) => true;
        public virtual void OnActivated(Plot plot, IServerPlayer player, string newTypeName, ref TextCommandResult tcr)
        {
            plot.saveToDatabase();
            tcr.StatusMessage = "claims:plot_set_type";
            tcr.MessageParams = new object[] { newTypeName };
        }
        public virtual void OnDeactivated(Plot plot) { }
    }
}
