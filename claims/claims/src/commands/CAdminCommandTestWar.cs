using System;
using System.Collections.Generic;
using System.Globalization;
using claims.src.auxialiry;
using claims.src.events;
using claims.src.gui.playerGui.structures;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using claims.src.part.structure.war;
using claims.src.rights;
using Vintagestory.API.Common;

namespace claims.src.commands
{
    /// <summary>
    /// Scaffolding for testing wars on an empty world: two throwaway cities and a conflict between
    /// them, without a second player, a mayor, money or the declaration gates. Everything it makes is
    /// a normal City/Conflict, so the war code under test sees nothing special about it.
    ///
    /// Kept apart from CAdminCommand because none of it belongs on a live server beyond a look at
    /// what a feature does.
    /// </summary>
    public static class CAdminCommandTestWar
    {
        /// <summary>Prefix every city this command makes, so `testwar remove` knows what to clear.</summary>
        private const string NamePrefix = "TestWar_";

        /// <summary>Plots left between the two test cities, so neither sits on the other's border.</summary>
        private const int PlotGap = 3;

        /// <summary>
        /// /cadmin testwar create [nameA] [nameB] - two mayorless cities and a CREATED conflict.
        /// </summary>
        public static TextCommandResult Create(TextCommandCallingArgs args)
        {
            string rawFirst = args.Parsers[0].GetValue() as string;
            string rawSecond = args.Parsers[1].GetValue() as string;

            string firstName = NameFor(rawFirst, "A");
            string secondName = NameFor(rawSecond, "B");
            if (firstName.Equals(secondName, StringComparison.OrdinalIgnoreCase))
                return TextCommandResult.Error("both cities would be called " + firstName);

            if (claims.dataStorage.GetCityByName(firstName, out _) || claims.dataStorage.GetCityByName(secondName, out _))
                return TextCommandResult.Error("a city called " + firstName + " or " + secondName + " already exists");

            // Near the caller when there is one, so the plots are somewhere reachable; at the origin
            // when the command comes from the server console.
            PlotPosition origin = args.Caller.Player?.Entity != null
                ? PlotPosition.fromEntityyPos(args.Caller.Player.Entity.Pos)
                : new PlotPosition(0, 0);

            if (!TryFindFreePlot(origin, 0, out PlotPosition firstPos)
                || !TryFindFreePlot(origin, PlotGap, out PlotPosition secondPos))
            {
                return TextCommandResult.Error("no free plots near " + origin.X + "," + origin.Z);
            }

            // No creator: initNewCity marks a mayorless city as technical, which is what a test city
            // is - nobody lives there and nothing should try to charge it rent.
            PartInits.initNewCity(null, firstPos, firstName);
            PartInits.initNewCity(null, secondPos, secondName);

            if (!claims.dataStorage.GetCityByName(firstName, out City first)
                || !claims.dataStorage.GetCityByName(secondName, out City second))
            {
                return TextCommandResult.Error("cities were created but could not be read back");
            }

            // The write queue turns an UPDATE that matched no row into an INSERT, so a brand new city
            // is only in the database after the queue has run. Saving it a second time before that -
            // which declaring the war does - would queue a second INSERT of the same guid and trip
            // the UNIQUE constraint. Flushing here puts the rows in before anything touches them.
            claims.getModInstance().getDatabaseHandler().saveEveryThing();

            Conflict conflict = StartConflict(first, second);

            return TextCommandResult.Success(
                "created " + firstName + " (" + firstPos.X + "," + firstPos.Z + ") and "
                + secondName + " (" + secondPos.X + "," + secondPos.Z + "), conflict " + conflict.State
                + ". Next: /cadmin testwar ranges " + firstName + " " + secondName + " saturday 20:00 120");
        }

        /// <summary>
        /// /cadmin testwar ranges &lt;A&gt; &lt;B&gt; &lt;days&gt; &lt;HH:mm&gt; &lt;minutes&gt; - sets the agreed
        /// schedule directly and activates the conflict, so the day filter and the next-battle
        /// arithmetic can be looked at without two players negotiating a grid.
        /// </summary>
        public static TextCommandResult Ranges(TextCommandCallingArgs args)
        {
            if (!TryGetConflict(args, out Conflict conflict, out TextCommandResult error)) return error;

            string rawDays = ((string)args.Parsers[2].GetValue()) ?? "";
            string rawTime = ((string)args.Parsers[3].GetValue()) ?? "";
            int duration = (int)args.Parsers[4].GetValue();
            if (duration <= 0) duration = claims.config.MIN_WARRANGE_DURATION_MINUTES;

            var days = new List<DayOfWeek>();
            foreach (string part in rawDays.Split(','))
            {
                string token = part.Trim();
                if (token.Length == 0) continue;
                if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
                {
                    if (number < 0 || number > 6) return TextCommandResult.Error("day number out of range: " + number);
                    days.Add((DayOfWeek)number);
                }
                else if (Enum.TryParse(token, true, out DayOfWeek parsed))
                {
                    days.Add(parsed);
                }
                else
                {
                    return TextCommandResult.Error("not a weekday: " + token);
                }
            }
            if (days.Count == 0) return TextCommandResult.Error("no days given");

            if (!TimeSpan.TryParse(rawTime, CultureInfo.InvariantCulture, out TimeSpan start)
                || start < TimeSpan.Zero || start >= TimeSpan.FromDays(1))
            {
                return TextCommandResult.Error("expected a start time as HH:mm, got '" + rawTime + "'");
            }

            var requested = new List<SelectedWarRange>();
            foreach (DayOfWeek day in days)
            {
                requested.Add(new SelectedWarRange(day, day, start, TimeSpan.FromMinutes(duration), conflict.First.Guid));
            }

            // Through the same filter the packet handler uses, so what the schedule ends up as here
            // is what a player negotiating the same slots would have got.
            var allowed = WarScheduleHelper.FilterToAllowedDays(requested);
            int dropped = requested.Count - allowed.Count;

            conflict.WarRanges = allowed;
            conflict.FirstWarRanges.Clear();
            conflict.SecondWarRanges.Clear();
            conflict.State = allowed.Count > 0 ? ConflictState.ACTIVE : ConflictState.CREATED;
            conflict.CalculateNextBattleDate();
            conflict.saveToDatabase();
            ModConfigReady.CheckForWarToStart();
            PushConflictUpdate(conflict, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_WARRANGES_UPDATED);

            string next = conflict.NextBattleDateStart == DateTime.UnixEpoch
                ? "none"
                : conflict.NextBattleDateStart.ToString("yyyy-MM-dd HH:mm:ss");
            return TextCommandResult.Success(
                "state " + conflict.State + ", " + allowed.Count + " window(s)"
                + (dropped > 0 ? " (" + dropped + " dropped by war_allowed_battle_days)" : "")
                + ", next battle " + next);
        }

        /// <summary>/cadmin testwar remove - deletes every city this command made, wars included.</summary>
        public static TextCommandResult Remove(TextCommandCallingArgs args)
        {
            var doomed = new List<City>();
            foreach (City city in claims.dataStorage.getCitiesList())
            {
                if (city.GetPartName().StartsWith(NamePrefix, StringComparison.OrdinalIgnoreCase)) doomed.Add(city);
            }
            // demolishCity tears down the conflicts along with the city, so the list is taken first.
            foreach (City city in doomed)
            {
                PartDemolition.demolishCity(city, "test war teardown");
            }
            return TextCommandResult.Success("removed " + doomed.Count + " test city/cities");
        }

        /// <summary>Puts two cities at war straight away - no letter, no cost, no cooldown.</summary>
        private static Conflict StartConflict(City first, City second)
        {
            Conflict conflict = new Conflict("", Alliance.GetUnusedGuid());
            conflict.First = first;
            conflict.StartedBy = first;
            conflict.Second = second;
            conflict.State = ConflictState.CREATED;
            conflict.TimeStampStarted = TimeFunctions.getEpochSeconds();
            conflict.MinimumDaysBetweenBattles = claims.config.MINIMUM_DAYS_BETWEEN_BATTLES;
            RightsHandler.SetPartiesHostile(first, second, conflict);
            claims.dataStorage.TryAddConflict(conflict);
            first.saveToDatabase();
            second.saveToDatabase();
            conflict.saveToDatabase(false);
            PushConflictUpdate(conflict, EnumPlayerRelatedInfo.ALLIANCE_CONFLICT_ADD);
            return conflict;
        }

        private static void PushConflictUpdate(Conflict conflict, EnumPlayerRelatedInfo kind)
        {
            var cell = ClientConflictCellElement.FromConflict(conflict);
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(conflict.First,
                new Dictionary<string, object> { { "value", cell } }, kind);
            UsefullPacketsSend.AddToQueueConflictPartyInfoUpdate(conflict.Second,
                new Dictionary<string, object> { { "value", cell } }, kind);
        }

        /// <summary>Resolves the two city names in parsers 0 and 1 to the conflict between them.</summary>
        private static bool TryGetConflict(TextCommandCallingArgs args, out Conflict conflict, out TextCommandResult error)
        {
            conflict = null;
            error = null;

            string firstName = Filter.filterName(args.Parsers[0].GetValue().ToString());
            string secondName = Filter.filterName(args.Parsers[1].GetValue().ToString());

            if (!claims.dataStorage.GetCityByName(firstName, out City first))
            {
                error = TextCommandResult.Error("no city called " + firstName);
                return false;
            }
            if (!claims.dataStorage.GetCityByName(secondName, out City second))
            {
                error = TextCommandResult.Error("no city called " + secondName);
                return false;
            }
            if (!ConflictHandler.TryGetConflictWithSides(first, second, out conflict))
            {
                error = TextCommandResult.Error("no conflict between " + firstName + " and " + secondName);
                return false;
            }
            return true;
        }

        /// <summary>Default names are numbered, so the command can be run twice without a clash.</summary>
        private static string NameFor(string given, string suffix)
        {
            if (!string.IsNullOrWhiteSpace(given)) return Filter.filterName(given);

            for (int i = 1; i < 100; i++)
            {
                string candidate = NamePrefix + suffix + i.ToString(CultureInfo.InvariantCulture);
                if (!claims.dataStorage.GetCityByName(candidate, out _)) return candidate;
            }
            return NamePrefix + suffix;
        }

        /// <summary>First unclaimed plot at or east of the origin, searched outward in a square ring.</summary>
        private static bool TryFindFreePlot(PlotPosition origin, int offset, out PlotPosition found)
        {
            for (int radius = 0; radius < 32; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        // Only the ring itself: the inside was covered by a smaller radius.
                        if (radius > 0 && Math.Abs(dx) != radius && Math.Abs(dz) != radius) continue;

                        var candidate = new PlotPosition(origin.X + offset + dx, origin.Z + dz);
                        if (!claims.dataStorage.GetPlot(candidate, out _))
                        {
                            found = candidate;
                            return true;
                        }
                    }
                }
            }
            found = null;
            return false;
        }
    }
}
