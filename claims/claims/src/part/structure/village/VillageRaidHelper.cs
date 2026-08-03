using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures;
using claims.src.messages;
using claims.src.part.structure.plots;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace claims.src.part.structure
{
    /// <summary>
    /// The raid window of a village. A village is never a party to a war, so it cannot be besieged
    /// the way a city is. Instead it is vulnerable for one hour every day, at the same time of day
    /// it was founded, starting a week after it was put down.
    ///
    /// While the window is open the village loses its block protection entirely (see
    /// OnBlockAction.EvalPermission) and its anchor can be broken; enough breaks end the village.
    /// Outside the window nothing of it can be touched.
    ///
    /// Whether the window is open is pure arithmetic on the founding time, so nothing has to be
    /// polled: a callback fires exactly when a window opens and again when it closes, and that is
    /// the only moment anything needs doing. Players who log in later are re-evaluated anyway,
    /// when their permission cache is built on join and on every plot they walk into.
    /// </summary>
    public static class VillageRaidHelper
    {
        // Guids with a callback already pending, so a village never gets two of them.
        private static readonly HashSet<string> scheduled = new HashSet<string>();

        // Longest a single callback may sleep. Keeps the millisecond delay inside an int and gives
        // the schedule a chance to catch up after a config change.
        private const int MaxDelaySeconds = 3600;

        private static long DayLength => claims.config.MOD_DAY_DURATION_IN_SECONDS > 0
            ? claims.config.MOD_DAY_DURATION_IN_SECONDS
            : 86400;

        // Counted in the same days as the window itself, so both stay in step on servers that
        // shorten MOD_DAY_DURATION_IN_SECONDS.
        private static long GraceSeconds => claims.config.VILLAGE_RAID_GRACE_DAYS * DayLength;

        /// <summary>
        /// Seconds elapsed since today's founding hour, which is where the window starts.
        /// -1 while the grace period is still running and there is no window at all yet.
        /// Every question about the schedule reduces to this one number.
        /// </summary>
        private static long SecondsIntoDay(City village, long now)
        {
            long age = now - village.TimeStampCreated;
            if (age < GraceSeconds) return -1;
            return age % DayLength;
        }

        /// <summary>Whether the village is vulnerable right now.</summary>
        public static bool IsRaidWindowOpen(City village)
        {
            if (!claims.config.VILLAGE_RAIDABLE) return false;
            if (village == null || !village.IsVillage() || village.isTechnicalCity()) return false;
            if (village.TimeStampCreated <= 0) return false;

            long into = SecondsIntoDay(village, TimeFunctions.getEpochSeconds());
            if (into < 0 || into >= claims.config.VILLAGE_RAID_DURATION_SECONDS) return false;

            // No anchor, no window: a settlement an admin turned into a village has nothing to
            // break, and stripping its protection daily would be pure loss. Checked last - it walks
            // the plots, and by now we already know we are inside the hour.
            return village.TryGetVillageMain(out _, out _);
        }

        /// <summary>Seconds left of the open window, 0 when it is closed. For the HUD and messages.</summary>
        public static int SecondsLeft(City village)
        {
            if (!IsRaidWindowOpen(village)) return 0;
            return (int)(claims.config.VILLAGE_RAID_DURATION_SECONDS
                         - SecondsIntoDay(village, TimeFunctions.getEpochSeconds()));
        }

        /// <summary>Unix seconds of the next opening; the start of the current one while it is open.</summary>
        public static long NextWindowStart(City village)
        {
            long now = TimeFunctions.getEpochSeconds();
            long into = SecondsIntoDay(village, now);
            if (into < 0) return village.TimeStampCreated + GraceSeconds;

            return into < claims.config.VILLAGE_RAID_DURATION_SECONDS
                ? now - into                    // already open: it started that many seconds ago
                : now + (DayLength - into);
        }

        /// <summary>
        /// One line saying when this village is open to attack. Deliberately only handed out on the
        /// spot - to its own citizens, or to whoever walks up and hits the anchor - so a raider has
        /// to scout the place instead of reading the schedule off a list.
        /// </summary>
        public static string DescribeSchedule(City village)
        {
            if (!claims.config.VILLAGE_RAIDABLE) return Lang.Get("claims:village_raid_never");
            if (IsRaidWindowOpen(village)) return Lang.Get("claims:village_raid_open_now", SecondsLeft(village) / 60);

            return Lang.Get("claims:village_raid_next",
                TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(NextWindowStart(village)),
                claims.config.VILLAGE_RAID_DURATION_SECONDS / 60);
        }

        // Last time each player was told the schedule, so hitting the anchor repeatedly does not
        // flood their chat.
        private static readonly Dictionary<string, long> scheduleHintAt = new Dictionary<string, long>();
        private const int HintCooldownSeconds = 30;

        /// <summary>
        /// Tells a would-be raider when the village can actually be attacked. Throttled per player;
        /// this fires on every swing at a protected anchor otherwise.
        /// </summary>
        public static void HintScheduleTo(IServerPlayer player, City village)
        {
            if (player == null || village == null) return;

            long now = TimeFunctions.getEpochSeconds();
            if (scheduleHintAt.TryGetValue(player.PlayerUID, out long last) && now - last < HintCooldownSeconds) return;

            // Drop stale entries now and then so the dictionary does not grow with every player
            // who ever poked an anchor.
            if (scheduleHintAt.Count > 64)
            {
                List<string> stale = new List<string>();
                foreach (KeyValuePair<string, long> entry in scheduleHintAt)
                {
                    if (now - entry.Value >= HintCooldownSeconds) stale.Add(entry.Key);
                }
                foreach (string uid in stale) scheduleHintAt.Remove(uid);
            }
            scheduleHintAt[player.PlayerUID] = now;

            MessageHandler.sendMsgToPlayer(player, DescribeSchedule(village));
        }

        /// <summary>
        /// Restores the state of every village and arms its next callback. Called once after the
        /// world has loaded - a server that was down through a window simply picks up where it is.
        /// </summary>
        public static void ScheduleAll()
        {
            foreach (City village in claims.dataStorage.getCitiesList())
            {
                if (!village.IsVillage()) continue;
                ApplyWindowState(village, announce: false);
                Schedule(village);
            }
        }

        /// <summary>Arms the callback for the next opening or closing of this village's window.</summary>
        public static void Schedule(City village)
        {
            if (village == null || !village.IsVillage()) return;
            if (!claims.config.VILLAGE_RAIDABLE) return;
            if (!scheduled.Add(village.Guid)) return;

            // Capped at an hour: a week's grace period in milliseconds overflows an int, and the
            // callback would fire with a negative delay. Waking up early costs nothing - the state
            // simply has not changed yet, and the next callback is armed again.
            int delaySeconds = System.Math.Min(SecondsToNextEvent(village), MaxDelaySeconds);
            string guid = village.Guid;
            claims.sapi.Event.RegisterCallback(_ => OnWindowEvent(guid), delaySeconds * 1000);
        }

        private static void OnWindowEvent(string villageGuid)
        {
            scheduled.Remove(villageGuid);
            // The village may have fallen or been upgraded while the callback was pending.
            if (!claims.dataStorage.getCityByGUID(villageGuid, out City village) || !village.IsVillage()) return;

            ApplyWindowState(village, announce: true);
            Schedule(village);
        }

        /// <summary>Brings the stored state in line with the clock, announcing the change if asked.</summary>
        private static void ApplyWindowState(City village, bool announce)
        {
            if (!village.TryGetVillageMain(out Plot mainPlot, out PlotDescVillage desc)) return;

            bool open = IsRaidWindowOpen(village);
            if (open == desc.RaidActive) return;

            desc.RaidActive = open;
            if (open)
            {
                // A fresh window means a whole anchor: a raid is a single push, not a counter an
                // attacker chips away at over days.
                desc.BreaksLeft = claims.config.VILLAGE_ANCHOR_BREAKS;
                if (announce)
                {
                    string coords = claims.config.SEND_COORDS_OF_PLOT_IN_UNDER_ATTACK && desc.AnchorPos != null
                        ? " (" + desc.AnchorPos.X + ", " + desc.AnchorPos.Y + ", " + desc.AnchorPos.Z + ")"
                        : "";
                    MessageHandler.sendMsgInCity(village, Lang.Get("claims:village_raid_window_open",
                        claims.config.VILLAGE_RAID_DURATION_SECONDS / 60) + coords);
                }
            }
            else
            {
                desc.BreaksLeft = 0;
                if (announce) MessageHandler.sendMsgInCity(village, Lang.Get("claims:village_raid_window_closed"));
            }

            mainPlot.saveToDatabase();
            VillageSupplyHelper.SyncBlockEntities(desc);
            RefreshPlotRights(village);
            // Citizens see the window in their city card; it just moved to the next day.
            UsefullPacketsSend.AddToQueueCityInfoUpdate(village.Guid, EnumPlayerRelatedInfo.CITY_RAID_WINDOW);
        }

        private static int SecondsToNextEvent(City village)
        {
            long now = TimeFunctions.getEpochSeconds();
            long into = SecondsIntoDay(village, now);
            if (into < 0) return (int)(village.TimeStampCreated + GraceSeconds - now);

            long duration = claims.config.VILLAGE_RAID_DURATION_SECONDS;
            // Inside the window the next event is its end, otherwise the next opening.
            long secondsLeft = into < duration ? duration - into : DayLength - into;
            // Never zero: a callback with no delay would fire in the same tick and loop.
            return (int)(secondsLeft > 0 ? secondsLeft : 1);
        }

        /// <summary>
        /// The window switches block protection for the whole village on and off, so the cached
        /// permissions of everyone standing there - and the copy the clients hold - must be
        /// dropped, the same treatment a war window gets in RightsHandler.
        /// </summary>
        private static void RefreshPlotRights(City village)
        {
            foreach (Plot plot in village.getCityPlots())
            {
                claims.dataStorage.ClearCacheForPlayersInPlot(plot);
                claims.serverPlayerMovementListener.markPlotToWasReUpdated(plot.getPos());
            }
        }
    }
}
