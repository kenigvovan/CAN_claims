using claims.src.auxialiry;
using claims.src.citylog;
using claims.src.gui.playerGui.structures;
using claims.src.messages;
using claims.src.part.structure.conflict;
using Vintagestory.API.Config;

namespace claims.src.part.structure.war
{
    /// <summary>
    /// Builds and delivers the after-action war report when a conflict ends: final score, plots
    /// captured, kills and coins pillaged per side. Sent to both sides in chat and logged in each
    /// participating city's event log. Call at the START of PartDemolition.DemolishConflict, while
    /// the sides and stat counters are still valid.
    /// </summary>
    public static class WarReportHelper
    {
        public static void Generate(Conflict conflict)
        {
            if (!claims.config.WAR_REPORT_ENABLED) return;
            if (conflict == null || conflict.First == null || conflict.Second == null) return;

            string firstName = conflict.First.GetPartName();
            string secondName = conflict.Second.GetPartName();
            string report = Lang.Get("claims:war_report",
                firstName, secondName,
                conflict.FirstScore, conflict.SecondScore,
                conflict.FirstPlotsCaptured, conflict.SecondPlotsCaptured,
                conflict.FirstKills, conflict.SecondKills,
                conflict.FirstPillaged, conflict.SecondPillaged);

            MessageHandler.SendMsgInAlliance(conflict.First, report);
            MessageHandler.SendMsgInAlliance(conflict.Second, report);

            string[] args = { firstName, secondName, conflict.FirstScore.ToString(), conflict.SecondScore.ToString() };
            foreach (City c in conflict.First.GetCities())
            {
                c.AddLogEntry(EnumCityLogEvent.WarEnded, args);
                UsefullPacketsSend.AddToQueueCityInfoUpdate(c.Guid, EnumPlayerRelatedInfo.CITY_LOG);
            }
            foreach (City c in conflict.Second.GetCities())
            {
                c.AddLogEntry(EnumCityLogEvent.WarEnded, args);
                UsefullPacketsSend.AddToQueueCityInfoUpdate(c.Guid, EnumPlayerRelatedInfo.CITY_LOG);
            }
        }
    }
}
