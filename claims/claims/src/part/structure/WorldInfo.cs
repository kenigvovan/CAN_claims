using claims.src.auxialiry;
using claims.src.part.interfaces;
using claims.src.perms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Config;

namespace claims.src.part.structure
{
    public class WorldInfo : Part, IGetStatus
    {
        //Forced everywhere, but if false still can be turned on
        public bool pvpEverywhere;
        public bool fireEverywhere;
        public bool blastEverywhere;
        //Hostile mobs spawn everywhere, the nomobspawn flag of a plot is ignored
        public bool mobSpawnEverywhere;

        //If true can NOT be turned on at all
        public bool fireForbidden;
        public bool pvpForbidden;
        public bool blastForbidden;
        //Hostile mobs never spawn, claimed land or not
        public bool mobSpawnForbidden;

        public WorldInfo(string val, string guid) : base(val, guid)
        {
            
        }

        public List<string> getStatus(PlayerInfo forPlayer = null)
        {
            List<string> status = new List<string>
            {
                Lang.Get("claims:world_pvp_everywhere", this.pvpEverywhere) + "\n",
                Lang.Get("claims:world_fire_everywhere", this.fireEverywhere) + "\n",
                Lang.Get("claims:world_blast_everywhere", this.blastEverywhere) + "\n",
                Lang.Get("claims:world_pvp_forbidden", this.pvpForbidden) + "\n",
                Lang.Get("claims:world_fire_forbidden", this.fireForbidden) + "\n",
                Lang.Get("claims:world_blast_forbidden", this.blastForbidden) + "\n",
                Lang.Get("claims:world_mobspawn_everywhere", this.mobSpawnEverywhere) + "\n",
                Lang.Get("claims:world_mobspawn_forbidden", this.mobSpawnForbidden) + "\n"
            };
            return status;
        }

        public override bool saveToDatabase(bool update = true)
        {
            return claims.getModInstance().getDatabaseHandler().saveWorldInfo(this, update);
        }
    }
}
