using claims.src.part;
using claims.src.part.structure;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace claims.src.harmony
{
    /// <summary>
    /// Makes a fellow citizen of a boat's owner count as its owner.
    ///
    /// Every vanilla ownership check - the helm (EntityRideableSeat.CanMount), attached storage,
    /// roping, damage tolerance - goes through EntityBehaviorOwnable.IsOwner, so one postfix covers
    /// all of them. The server decides from live city data; the client knows no city but its own and
    /// goes by the stamp the server writes onto the boat (see StampSharedParty).
    /// </summary>
    public class BoatSharePatches
    {
        private const string SharedCityGuidAttr = "claimsSharedCityGuid";
        private const string SharedCityNameAttr = "claimsSharedCityName";
        private const string SharedAllianceGuidAttr = "claimsSharedAllianceGuid";

        public static void Postfix_Ownable_IsOwner(EntityBehaviorOwnable __instance, EntityAgent byEntity, ref bool __result)
        {
            // Vanilla already said yes: unowned boat, or the caller is the owner.
            if (__result) return;
            if (claims.config?.BOAT_SHARE_WITH_CITY != true) return;
            if (byEntity is not EntityPlayer player) return;

            var wa = __instance.entity.WatchedAttributes;
            if (wa.GetTreeAttribute("ownedby") == null) return;

            // What the owner set on this particular boat, narrowed by what the host allows.
            BoatShareMode mode = BoatShareModeHelper.Effective(BoatShareModeHelper.Of(__instance.entity));
            if (mode == BoatShareMode.PERSONAL) return;

            if (__instance.entity.World.Side == EnumAppSide.Server)
            {
                string ownerUid = wa.GetTreeAttribute("ownedby").GetString("uid", "");
                if (ownerUid.Length == 0 || claims.dataStorage == null) return;

                claims.dataStorage.GetPlayerByUid(ownerUid, out PlayerInfo owner);
                claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo caller);
                if (owner?.City == null || caller?.City == null) return;

                if (owner.City.Guid == caller.City.Guid)
                {
                    __result = true;
                }
                else if (mode == BoatShareMode.ALLIANCE
                    && owner.City.Alliance != null && caller.City.Alliance != null
                    && owner.City.Alliance.Guid == caller.City.Alliance.Guid)
                {
                    __result = true;
                }

                // Refreshed here too, so an owner who switched cities does not leave a stale stamp.
                StampSharedParty(__instance);
            }
            else
            {
                // CanMount checks client-side first. Only our own player is judged here; other
                // players' cities are unknown to the client.
                var info = claims.clientDataStorage?.clientPlayerInfo;
                if (info == null || claims.capi == null) return;
                if (player.PlayerUID != claims.capi.World.Player.PlayerUID) return;

                string cityGuid = wa.GetString(SharedCityGuidAttr, "");
                string allianceGuid = wa.GetString(SharedAllianceGuidAttr, "");

                if (cityGuid.Length > 0 && cityGuid == info.CityInfo?.Guid)
                {
                    __result = true;
                }
                else if (allianceGuid.Length > 0 && allianceGuid == info.AllianceInfo?.Guid)
                {
                    __result = true;
                }
            }
        }

        /// <summary>Runs on entity load, so a boat carries its stamp before any client asks.</summary>
        public static void Postfix_Ownable_VerifyOwnership(EntityBehaviorOwnable __instance)
        {
            if (__instance.entity.World.Side != EnumAppSide.Server) return;
            StampSharedParty(__instance);
        }

        /// <summary>
        /// Writes the owner's city onto the boat, or clears it when sharing is off, the boat is
        /// personal or the owner has no city - so clients can read "is this shared" from the stamp
        /// alone. Written only on change; every write is a network sync.
        /// </summary>
        private static void StampSharedParty(EntityBehaviorOwnable beh)
        {
            var wa = beh.entity.WatchedAttributes;

            string cityGuid = "", cityName = "", allianceGuid = "";
            var ownedby = wa.GetTreeAttribute("ownedby");
            BoatShareMode mode = BoatShareModeHelper.Effective(BoatShareModeHelper.Of(beh.entity));

            if (ownedby != null && mode != BoatShareMode.PERSONAL && claims.dataStorage != null)
            {
                claims.dataStorage.GetPlayerByUid(ownedby.GetString("uid", ""), out PlayerInfo owner);
                if (owner?.City != null)
                {
                    cityGuid = owner.City.Guid;
                    cityName = owner.City.GetPartName();
                    if (mode == BoatShareMode.ALLIANCE && owner.City.Alliance != null)
                    {
                        allianceGuid = owner.City.Alliance.Guid;
                    }
                }
            }

            if (wa.GetString(SharedCityGuidAttr, "") != cityGuid) wa.SetString(SharedCityGuidAttr, cityGuid);
            if (wa.GetString(SharedCityNameAttr, "") != cityName) wa.SetString(SharedCityNameAttr, cityName);
            if (wa.GetString(SharedAllianceGuidAttr, "") != allianceGuid) wa.SetString(SharedAllianceGuidAttr, allianceGuid);
        }

        /// <summary>Adds the sharing line to the boat's tooltip, under vanilla's "Owned by".</summary>
        public static void Postfix_Ownable_GetInfoText(EntityBehaviorOwnable __instance, StringBuilder infotext)
        {
            var wa = __instance.entity.WatchedAttributes;
            if (wa.GetTreeAttribute("ownedby") == null) return;

            string cityName = wa.GetString(SharedCityNameAttr, "");
            if (cityName.Length == 0)
            {
                // Owned and set to personal: say so, to distinguish it from an owner with no city.
                if (BoatShareModeHelper.Of(__instance.entity) == BoatShareMode.PERSONAL
                    && claims.config?.BOAT_SHARE_WITH_CITY == true)
                {
                    infotext.AppendLine(Lang.Get("claims:boat-shared-personal"));
                }
                return;
            }

            infotext.AppendLine(Lang.Get(
                wa.GetString(SharedAllianceGuidAttr, "").Length > 0
                    ? "claims:boat-shared-alliance"
                    : "claims:boat-shared-city",
                cityName));
        }
    }
}
