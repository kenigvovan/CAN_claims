using claims.src.part.structure;

namespace claims.src.network.packets
{
    /// <summary>The world-wide overrides. Sent to every player, not only to admins: plots show pvp through them.</summary>
    public class AdminWorldFlags
    {
        public bool PvpEverywhere { get; set; }
        public bool PvpForbidden { get; set; }
        public bool FireEverywhere { get; set; }
        public bool FireForbidden { get; set; }
        public bool BlastEverywhere { get; set; }
        public bool BlastForbidden { get; set; }

        public static AdminWorldFlags From(WorldInfo wi) => new AdminWorldFlags
        {
            PvpEverywhere = wi.pvpEverywhere,
            PvpForbidden = wi.pvpForbidden,
            FireEverywhere = wi.fireEverywhere,
            FireForbidden = wi.fireForbidden,
            BlastEverywhere = wi.blastEverywhere,
            BlastForbidden = wi.blastForbidden
        };

        /// <summary>
        /// Whether pvp is on for a plot whose own rule (plot, city or tournament) is
        /// <paramref name="plotPvp"/>. Same order as OnPVP: "everywhere" wins over "forbidden".
        /// A null means no flags arrived yet - the plot's rule alone.
        /// </summary>
        public static bool EffectivePvp(AdminWorldFlags world, bool plotPvp)
        {
            if (world == null) return plotPvp;
            if (world.PvpEverywhere) return true;
            if (world.PvpForbidden) return false;
            return plotPvp;
        }
    }
}
