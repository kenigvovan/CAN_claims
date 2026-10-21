using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace claims.src.timers
{
    public class BackupScheduler
    {
        /// <summary>Scheduled copies only: pruning never touches the manual one or anything else in the folder.</summary>
        public const string ScheduledPrefix = "claims_";
        public const string ScheduledSuffix = ".db";
        // Sorts by time as text, which is what pruning relies on.
        private const string TimeFormat = "yyyy-MM-dd_HH-mm-ss";

        private readonly ICoreServerAPI sapi;
        private DateTime? lastBackupTime = null;
        private readonly HashSet<string> reportedBadTimes = new HashSet<string>();

        public BackupScheduler(ICoreServerAPI sapi)
        {
            this.sapi = sapi;
        }

        public void Tick()
        {
            DateTime now = DateTime.Now;
            var slots = new List<(int Hour, int Minute)>();
            foreach (string it in claims.config.DAYTIME_MAKE_BACKUP)
            {
                if (TryParseTime(it, out int hour, out int minute))
                {
                    slots.Add((hour, minute));
                }
                else if (reportedBadTimes.Add(it))
                {
                    // Once per value: the tick runs every few seconds.
                    sapi?.Logger.Warning("[claims] DAYTIME_MAKE_BACKUP: '{0}' is not a time of day (H:MM), skipped", it);
                }
            }

            DateTime? due = LatestSlot(now, slots);
            if (due == null) return;
            if (lastBackupTime != null && lastBackupTime >= due) return;

            lastBackupTime = now;
            MakeBackup(now);
        }

        private void MakeBackup(DateTime now)
        {
            string name = ScheduledBackupName(now);
            if (!claims.getModInstance().getDatabaseHandler().makeBackup(name))
            {
                // makeBackup has logged why.
                return;
            }
            sapi?.Logger.Notification("[claims] Backup created: " + name);

            int keep = claims.config.BACKUP_KEEP_COUNT;
            if (keep <= 0) return;
            try
            {
                string folder = BackupFolder();
                foreach (string old in FilesToPrune(Directory.GetFiles(folder).Select(Path.GetFileName), keep))
                {
                    File.Delete(Path.Combine(folder, old));
                }
            }
            catch (Exception e)
            {
                sapi?.Logger.Warning("[claims] Old backups could not be removed: {0}", e);
            }
        }

        /// <summary>Where backups go: FULL_BACKUP_FOLDER if set, otherwise a folder in the game's data path.</summary>
        public static string BackupFolder()
        {
            return claims.config.FULL_BACKUP_FOLDER.Length > 0
                ? claims.config.FULL_BACKUP_FOLDER
                : Path.Combine(GamePaths.DataPath, claims.config.BACKUP_FOLDER_NAME_IN_DATA_FOLDER);
        }

        /// <summary>"H:MM" or "HH:MM" within a day. "25:00" used to throw out of the tick.</summary>
        public static bool TryParseTime(string value, out int hour, out int minute)
        {
            hour = minute = 0;
            string[] parts = (value ?? "").Split(':');
            return parts.Length == 2
                && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out hour)
                && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out minute)
                && hour >= 0 && hour <= 23 && minute >= 0 && minute <= 59;
        }

        /// <summary>
        /// The most recent scheduled moment at or before <paramref name="now"/>, yesterday's slots
        /// included: a slot at 23:59 checked a few seconds after midnight used to be skipped, as only
        /// today's times were compared.
        /// </summary>
        public static DateTime? LatestSlot(DateTime now, IEnumerable<(int Hour, int Minute)> slots)
        {
            DateTime? latest = null;
            foreach (var (hour, minute) in slots)
            {
                DateTime today = now.Date.AddHours(hour).AddMinutes(minute);
                DateTime slot = today <= now ? today : today.AddDays(-1);
                if (latest == null || slot > latest) latest = slot;
            }
            return latest;
        }

        public static string ScheduledBackupName(DateTime at) =>
            ScheduledPrefix + at.ToString(TimeFormat, CultureInfo.InvariantCulture) + ScheduledSuffix;

        /// <summary>
        /// Scheduled backups beyond the newest <paramref name="keep"/>, oldest first. They piled up
        /// without limit - four a day by default.
        /// </summary>
        public static List<string> FilesToPrune(IEnumerable<string> fileNames, int keep)
        {
            var scheduled = fileNames
                .Where(IsScheduledBackupName)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();
            if (keep <= 0 || scheduled.Count <= keep) return new List<string>();
            return scheduled.Take(scheduled.Count - keep).ToList();
        }

        private static bool IsScheduledBackupName(string name)
        {
            if (name == null || !name.StartsWith(ScheduledPrefix, StringComparison.Ordinal)
                || !name.EndsWith(ScheduledSuffix, StringComparison.Ordinal)) return false;
            string stamp = name.Substring(ScheduledPrefix.Length, name.Length - ScheduledPrefix.Length - ScheduledSuffix.Length);
            return DateTime.TryParseExact(stamp, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }
    }
}
