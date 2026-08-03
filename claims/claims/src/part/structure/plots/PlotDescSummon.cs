using System;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.part.structure;
using Newtonsoft.Json;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.part.structure.plots
{
    public class PlotDescSummon : PlotDesc
    {
        public Vec3d SummonPoint { get; set; }
        public string Name { get; set; } = "";

        public PlotDescSummon(Vec3d summonPoint) { SummonPoint = summonPoint; }
        public PlotDescSummon() { }

        public override string Serialize(Plot plot) => JsonConvert.SerializeObject(this);

        public override void Deserialize(string data, Plot plot)
        {
            try
            {
                JsonConvert.PopulateObject(data, this);
                if (plot.hasCity())
                    plot.getCity().summonPlots.Add(plot);
            }
            catch (Exception ex) { claims.sapi.Logger.Warning("[claims] PlotDescSummon.Init failed: " + ex.Message); }
        }

        public override bool Validate(Plot plot, IServerPlayer player, ref TextCommandResult tcr)
        {
            CityLevelInfo cli = Settings.getCityLevelInfo(plot.getCity().getCityCitizens().Count);
            if (plot.getCity().summonPlots.Count >= cli.SummonPlots)
            {
                tcr.StatusMessage = "claims:limit_summon_plots";
                return false;
            }
            return true;
        }

        public override void OnActivated(Plot plot, IServerPlayer player, string newTypeName, ref TextCommandResult tcr)
        {
            Name = "Point" + ((int)SummonPoint.X % 10) + ((int)SummonPoint.Z % 10);
            plot.getCity().summonPlots.Add(plot);
            plot.saveToDatabase();
            plot.getCity().saveToDatabase();
            UsefullPacketsSend.AddToQueueCityInfoUpdate(plot.getCity().Guid,
                new Dictionary<string, object> { { "value", new SummonCellElement(SummonPoint.AsVec3i.Clone(), Name) } },
                EnumPlayerRelatedInfo.CITY_SUMMON_POINT_ADD);
            tcr.StatusMessage = "claims:plot_set_type";
            tcr.MessageParams = new object[] { newTypeName };
        }

        public override void OnDeactivated(Plot plot)
        {
            UsefullPacketsSend.AddToQueueCityInfoUpdate(plot.getCity().Guid,
                new Dictionary<string, object> { { "value", new SummonCellElement(SummonPoint.AsVec3i.Clone(), Name) } },
                EnumPlayerRelatedInfo.CITY_SUMMON_POINT_REMOVE);
            plot.getCity().summonPlots.Remove(plot);
        }
    }
}
