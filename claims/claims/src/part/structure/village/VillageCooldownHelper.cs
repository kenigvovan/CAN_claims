using System;
using System.Collections.Generic;
using claims.src.auxialiry;
using claims.src.messages;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace claims.src.part.structure
{
    /// <summary>
    /// What stops a fallen village from being rebuilt the same evening: its former citizens have to
    /// wait before founding anything again, and the site itself stays off limits for a while - to
    /// anyone, so a friend who was never on the citizen list cannot put the village back either.
    ///
    /// Joining an existing settlement is deliberately never blocked: losing a village should cost
    /// the land, not leave its people with nowhere to live.
    /// </summary>
    public static class VillageCooldownHelper
    {
        /// <summary>Puts everyone on cooldown, marks the ground and tells them how long it lasts.</summary>
        public static void RegisterFallenVillage(City village)
        {
            if (village == null) return;

            foreach (PlayerInfo citizen in village.getCityCitizens())
            {
                SetPlayerCooldown(citizen, abandoned: false);
                MessageHandler.sendMsgToPlayerInfo(citizen, Lang.Get("claims:village_lost_cooldown",
                    HoursLeft(RefoundUntil(citizen)), claims.config.VILLAGE_SITE_COOLDOWN_HOURS));
            }
            MarkRuins(village, abandoned: false);
        }

        /// <summary>Cooldown for giving a village up voluntarily - shorter than losing one.</summary>
        public static void RegisterAbandonedVillage(City village, PlayerInfo mayor)
        {
            if (village == null) return;

            if (mayor != null)
            {
                SetPlayerCooldown(mayor, abandoned: true);
                MessageHandler.sendMsgToPlayerInfo(mayor, Lang.Get("claims:village_lost_cooldown",
                    HoursLeft(RefoundUntil(mayor)), claims.config.VILLAGE_ABANDON_COOLDOWN_HOURS));
            }
            MarkRuins(village, abandoned: true);
        }

        /// <summary>When this player may found a settlement again, by the current config; 0 if never blocked.</summary>
        public static long RefoundUntil(PlayerInfo player)
        {
            if (player == null || player.VillageLostAt <= 0) return 0;
            int hours = player.VillageAbandoned
                ? claims.config.VILLAGE_ABANDON_COOLDOWN_HOURS
                : claims.config.VILLAGE_REFOUND_COOLDOWN_HOURS;
            return player.VillageLostAt + (long)hours * 3600;
        }

        private static void SetPlayerCooldown(PlayerInfo player, bool abandoned)
        {
            long now = TimeFunctions.getEpochSeconds();
            long previousUntil = RefoundUntil(player);
            long previousLostAt = player.VillageLostAt;
            bool previousAbandoned = player.VillageAbandoned;

            player.VillageLostAt = now;
            player.VillageAbandoned = abandoned;
            // Never shorten a penalty the player already carries.
            if (RefoundUntil(player) < previousUntil)
            {
                player.VillageLostAt = previousLostAt;
                player.VillageAbandoned = previousAbandoned;
            }
            player.saveToDatabase();
        }

        private static void MarkRuins(City village, bool abandoned)
        {
            VillageRuin fresh = new VillageRuin(TimeFunctions.getEpochSeconds(), abandoned);
            if (fresh.Until <= fresh.Since) return;

            foreach (Plot plot in village.getCityPlots())
            {
                Vec2i pos = plot.getPos();
                bool existed = claims.dataStorage.VillageRuins.TryGetValue(pos, out VillageRuin old);
                if (existed && old.Until > fresh.Until) continue;
                claims.dataStorage.VillageRuins[pos] = new VillageRuin(fresh.Since, fresh.Abandoned);
                claims.getModInstance().getDatabaseHandler().saveVillageRuin(pos.X, pos.Y, fresh.Since, fresh.Abandoned, existed);
            }
        }

        /// <summary>
        /// Whether this player may found a settlement here. Returns false and fills the lang key
        /// plus the hours still to wait, so the refusal can say how long that is.
        /// </summary>
        public static bool CanFoundHere(PlayerInfo founder, Vec2i plotPos, out string langKey, out int hoursLeft)
        {
            long now = TimeFunctions.getEpochSeconds();

            long refoundUntil = RefoundUntil(founder);
            if (refoundUntil > now)
            {
                langKey = "claims:village_refound_cooldown";
                hoursLeft = HoursLeft(refoundUntil);
                return false;
            }

            long ruinUntil = RuinCooldownAt(plotPos, now);
            if (ruinUntil > 0)
            {
                langKey = "claims:village_site_cooldown";
                hoursLeft = HoursLeft(ruinUntil);
                return false;
            }

            langKey = null;
            hoursLeft = 0;
            return true;
        }

        /// <summary>Hours still to wait, rounded up - "0 h left" would read as "go ahead".</summary>
        private static int HoursLeft(long until)
        {
            long secondsLeft = until - TimeFunctions.getEpochSeconds();
            if (secondsLeft <= 0) return 0;
            return (int)Math.Ceiling(secondsLeft / 3600.0);
        }

        /// <summary>When the ruins nearest to this spot stop blocking it; 0 if nothing does.</summary>
        private static long RuinCooldownAt(Vec2i plotPos, long now)
        {
            if (plotPos == null) return 0;
            int radius = claims.config.VILLAGE_RUIN_RADIUS_PLOTS;

            long latest = 0;
            foreach (KeyValuePair<Vec2i, VillageRuin> ruin in claims.dataStorage.VillageRuins)
            {
                long until = ruin.Value.Until;
                if (until <= now) continue;
                if (MathClaims.distanceBetween(ruin.Key, plotPos) > radius) continue;
                if (until > latest) latest = until;
            }
            return latest;
        }

        /// <summary>Drops ruins whose ban has run out. Called from the hour timer.</summary>
        public static void PurgeExpired()
        {
            long now = TimeFunctions.getEpochSeconds();
            List<Vec2i> expired = new List<Vec2i>();
            foreach (KeyValuePair<Vec2i, VillageRuin> ruin in claims.dataStorage.VillageRuins)
            {
                if (ruin.Value.Until <= now) expired.Add(ruin.Key);
            }
            foreach (Vec2i pos in expired)
            {
                claims.dataStorage.VillageRuins.Remove(pos);
                claims.getModInstance().getDatabaseHandler().deleteVillageRuin(pos.X, pos.Y);
            }
        }
    }
}
