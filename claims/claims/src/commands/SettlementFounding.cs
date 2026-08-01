using claims.src.auxialiry;
using claims.src.part;
using claims.src.part.structure;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace claims.src.commands
{
    /// <summary>
    /// The checks a new settlement has to pass, shared by cities and villages: both are founded on
    /// free land, by someone who has nowhere else to live, under a name nobody took, far enough
    /// from the neighbours and clear of the ruins of a fallen village.
    ///
    /// Only the distance is a parameter - a village settles by a rule of its own.
    /// </summary>
    public static class SettlementFounding
    {
        public static bool TryValidate(PlayerInfo founder, PlotPosition where, int minDistance, string rawName,
            out string name, out TextCommandResult error)
        {
            name = null;
            error = null;

            if (founder.hasCity())
            {
                error = TextCommandResult.Error("claims:you_already_have_city");
                return false;
            }
            if (claims.dataStorage.GetPlot(where, out _))
            {
                error = TextCommandResult.Error("claims:plot_already_claimed");
                return false;
            }
            if (!claims.dataStorage.CheckClaimLimiters(founder, where))
            {
                error = TextCommandResult.Error("claims:too_close_to_forbidden_area");
                return false;
            }

            Vec2i plotPos = where.getPos();
            if (!VillageCooldownHelper.CanFoundHere(founder, plotPos, out string cooldownKey, out int hoursLeft))
            {
                error = BaseCommand.ErrorWithParams(cooldownKey, new object[] { hoursLeft });
                return false;
            }

            name = Filter.filterName(rawName);
            if (claims.dataStorage.cityExistsByName(name))
            {
                error = TextCommandResult.Error("claims:city_name_is_already_taken");
                return false;
            }
            if (name.Length == 0 || !Filter.checkForBlockedNames(name))
            {
                error = TextCommandResult.Error("claims:invalid_new_city_name");
                return false;
            }
            if (name.Length > claims.config.MAX_LENGTH_CITY_NAME)
            {
                error = TextCommandResult.Error("claims:city_name_is_too_long");
                return false;
            }
            if (!claims.dataStorage.plotHasDistantEnoughFromOtherForNewCity(plotPos, minDistance))
            {
                error = TextCommandResult.Error("claims:too_close_to_another_city_new_city");
                return false;
            }
            return true;
        }
    }
}
