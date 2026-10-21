using claims.src.part;
using claims.src.perms;
using claims.src.perms.type;

namespace claims.src.subplots
{
    /// <summary>
    /// What a player may do inside a sub-plot. Asked before the chunk's rules and before the
    /// per-chunk cache: inside a flat its rules win both ways - a citizen who may build in the chunk
    /// may not in someone's flat, and a stranger the flat lets in may build there.
    /// </summary>
    public static class SubPlotAccess
    {
        /// <summary>
        /// True or false when the sub-plot decides; null when it is not its call and the chunk's rules
        /// apply - an enemy at war, whose reach is the war's destruction scope, not the flat's door.
        /// </summary>
        public static bool? Decide(PlayerInfo player, SubPlot subPlot, PermType type)
        {
            if (player == null || subPlot == null) return null;
            City city = subPlot.City;

            // The mayor keeps every right on the city's ground, as over a citizen's own plot.
            if (city != null && city.isMayor(player)) return true;
            if (subPlot.Owner != null && subPlot.Owner.Equals(player)) return true;
            if (subPlot.Members.Contains(player)) return true;

            // War overrides peacetime ties here as in getPlotRelationForPlayerInfo: a besieger is not
            // judged by the flat's guest list.
            if (city != null && player.hasCity() && !city.Equals(player.City) && city.HostileCities.Contains(player.City))
            {
                return null;
            }

            PermsHandler perms = subPlot.PermsHandler;
            bool citizen = city != null && city.getCityCitizens().Contains(player);
            bool ally = !citizen && city != null && city.HasAlliance() && player.HasAlliance()
                        && city.Alliance.ComradAlliancies.Contains(player.Alliance);
            bool right = citizen ? perms.getPerm(PermGroup.CITIZEN, type)
                       : ally ? perms.getPerm(PermGroup.ALLY, type)
                       : perms.getPerm(PermGroup.STRANGER, type);

            // A plots group the sub-plot is in gives its members the group's rights, as on the group's
            // plots - on top of what they have anyway, like friendship below.
            if (subPlot.Group != null && subPlot.Group.PlayersList.Contains(player)
                && subPlot.Group.PermsHandler.getPerm(PermGroup.CITIZEN, type))
            {
                return true;
            }

            // Friendship adds the friend's rights to what the player has anyway, never takes any away.
            if (subPlot.Owner != null && subPlot.Owner.Friends.Contains(player))
            {
                return right || perms.getPerm(PermGroup.COMRADE, type);
            }
            return right;
        }
    }
}
