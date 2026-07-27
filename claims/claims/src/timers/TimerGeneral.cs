using claims.src.auxialiry;
using claims.src.delayed.cooldowns;
using claims.src.delayed.teleportation;
using claims.src.part.structure.conflict;
using claims.src.part.structure.union;
using Vintagestory.API.Server;

namespace claims.src.timers
{
    public class TimerGeneral
    {
        public static void StartServerTimers(ICoreServerAPI sapi)
        {
            //Start new day sequence. The callback runs on the main server thread —
            //day/hour processing touches shared game state, so it must NOT be offloaded
            //to a background thread (that caused data races with the server tick).
            var returnId = sapi.Event.RegisterCallback((dt =>
            {
                new DayTimer().Run(true);
            }), (int)TimeFunctions.getSecondsBeforeNextDayStart() * 1000);

            sapi.Logger.Debug("StartTimers:newdayTimer, handlerID " + returnId);

            //Start new hour sequence
            returnId = sapi.Event.RegisterCallback((dt =>
            {
                new HourTimer().Run();
            }), (int)TimeFunctions.getSecondsBeforeNextHourStart() * 1000);

            // === BACKUP SCHEDULER ===
            var backupScheduler = new BackupScheduler(sapi);

            // Проверка раз в 10 секунд (можно раз в 30 или 60)
            sapi.Event.RegisterGameTickListener(dt =>
            {
                backupScheduler.Tick();
            }, claims.config.BACKUP_CHECK_TIMER_SECONDS);

            //Start timer for cooldown processing
            sapi.Event.Timer((() =>
            {
                CooldownHandler.processCooldowns();
            }
            ), 1);
            sapi.Event.Timer((() =>
            {
                TeleportationHandler.UpdateTeleportations();
            }
            ), 1);

            sapi.Event.Timer((() =>
            {
                UsefullPacketsSend.SendAllCollectedCityUpdatesToCitizens();
            }
            ), claims.config.CHECK_FOR_PACKETS_TO_SEND_EVERY_N_SECONDS);

            //Start timer for conflicts handler
            claims.sapi.Event.Timer((() =>
            {
                ConflictHandler.updateConflictLetters();
                UnionHander.updateUnionLetters();
                UnionBreakHelper.ApplyDueBreaks();
            }
            ), 300);
        }
    }
}
