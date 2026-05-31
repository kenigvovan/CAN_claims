using claims.src.auxialiry;
using claims.src.part;
using claims.src.part.structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace claims.src
{
    public interface IDataStorage
    {
        bool GetPlayerByUid(string uid, out PlayerInfo player);

        bool GetPlot(PlotPosition pos, out Plot plot);

        bool GetAllianceByName(string name, out Alliance alliance);

        bool GetCityByName(string name, out City city);

        void ClearCacheForPlayersInPlot(Plot plot);
        bool Has3DPlotAt(int plotX, int plotZ);
    }
}
