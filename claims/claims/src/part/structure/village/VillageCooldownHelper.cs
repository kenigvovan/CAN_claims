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

            int hours = claims.config.VILLAGE_REFOUND_COOLDOWN_HOURS;
            long until = TimeFunctions.getEpochSeconds() + (long)hours * 3600;

            foreach (PlayerInfo citizen in village.getCityCitizens())
            {
                // Never shorten a penalty the player already carries.
                if (citizen.VillageCooldownUntil < until) citizen.VillageCooldownUntil = until;
                citizen.saveToDatabase();
                MessageHandler.sendMsgToPlayerInfo(citizen, Lang.Get("claims:village_lost_cooldown",
                    HoursLeft(citizen.VillageCooldownUntil), claims.config.VILLAGE_SITE_COOLDOWN_HOURS));
            }
            MarkRuins(village, claims.config.VILLAGE_SITE_COOLDOWN_HOURS);
        }

        /// <summary>Cooldown for giving a village up voluntarily - shorter than losing one.</summary>
        public static void RegisterAbandonedVillage(City village, PlayerInfo mayor)
        {
            if (village == null) return;

            int hours = claims.config.VILLAGE_ABANDON_COOLDOWN_HOURS;
            long until = TimeFunctions.getEpochSeconds() + (long)hours * 3600;
            if (mayor != null)
            {
                if (mayor.VillageCooldownUntil < until) mayor.VillageCooldownUntil = until;
                mayor.saveToDatabase();
                MessageHandler.sendMsgToPlayerInfo(mayor, Lang.Get("claims:village_lost_cooldown",
                    HoursLeft(mayor.VillageCooldownUntil), hours));
            }
            MarkRuins(village, hours);
        }

        private static void MarkRuins(City village, int hours)
        {
            if (hours <= 0) return;
            long until = TimeFunctions.getEpochSeconds() + (long)hours * 3600;

            foreach (Plot plot in village.getCityPlots())
            {
                Vec2i pos = plot.getPos();
                bool existed = claims.dataStorage.VillageRuins.ContainsKey(pos);
                claims.dataStorage.VillageRuins[pos] = until;
                claims.getModInstance().getDatabaseHandler().saveVillageRuin(pos.X, pos.Y, until, existed);
            }
        }

        /// <summary>
        /// Whether this player may found a settlement here. Returns false and fills the lang key
        /// plus the hours still to wait, so the refusal can say how long that is.
        /// </summary>
        public static bool CanFoundHere(PlayerInfo founder, Vec2i plotPos, out string langKey, out int hoursLeft)
        {
            long now = TimeFunctions.getEpochSeconds();

            if (founder != null && founder.VillageCooldownUntil > now)
            {
                langKey = "claims:village_refound_cooldown";
                hoursLeft = HoursLeft(founder.VillageCooldownUntil);
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
            foreach (KeyValuePair<Vec2i, long> ruin in claims.dataStorage.VillageRuins)
            {
                if (ruin.Value <= now) continue;
                if (MathClaims.distanceBetween(ruin.Key, plotPos) > radius) continue;
                if (ruin.Value > latest) latest = ruin.Value;
            }
            return latest;
        }

        /// <summary>Drops ruins whose ban has run out. Called from the hour timer.</summary>
        public static void PurgeExpired()
        {
            long now = TimeFunctions.getEpochSeconds();
            List<Vec2i> expired = new List<Vec2i>();
            foreach (KeyValuePair<Vec2i, long> ruin in claims.dataStorage.VillageRuins)
            {
                if (ruin.Value <= now) expired.Add(ruin.Key);
            }
            foreach (Vec2i pos in expired)
            {
                claims.dataStorage.VillageRuins.Remove(pos);
                claims.getModInstance().getDatabaseHandler().deleteVillageRuin(pos.X, pos.Y);
            }
        }
    }
}
