using System.Collections.Generic;
using claims.src.part;
using Newtonsoft.Json;
using Vintagestory.API.MathTools;
using static OpenTK.Graphics.OpenGL.GL;

namespace claims.src.auxialiry.ClaimLimiter
{
    public class NearClaimLimiter : ClaimLimiter
    {
        public List<(Vec2i, int)> Zones;
        public NearClaimLimiter(Dictionary<string, object> args) 
        {
            if(args.TryGetValue("Zones", out var val))
            {
                Zones = new();
                var li =JsonConvert.DeserializeObject<List<(Vec2i, int)>>(JsonConvert.SerializeObject(val));
                foreach (var it in li)
                {
                    Zones.Add((new Vec2i(it.Item1.X, it.Item1.Y), it.Item2));
                }
            }
        }
        public override bool CanClaimHere(PlayerInfo playerInfo, PlotPosition plotPosition)
        {
            foreach (var zone in Zones)
            {
                Vec2i center = new Vec2i(plotPosition.X * PlotPosition.plotSize + PlotPosition.plotSize / 2 - (int)claims.sapi.World.DefaultSpawnPosition.X, plotPosition.Z * PlotPosition.plotSize + PlotPosition.plotSize / 2 - (int)claims.sapi.World.DefaultSpawnPosition.Z);
                if (MathClaims.distanceBetween(center, zone.Item1) < zone.Item2)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
