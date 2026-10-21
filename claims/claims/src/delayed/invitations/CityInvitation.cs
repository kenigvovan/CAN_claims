using claims.src.auxialiry;
using claims.src.citylog;
using claims.src.gui.playerGui.structures;
using claims.src.messages;
using claims.src.part;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace claims.src.delayed.invitations
{
    /// <summary>
    /// An invitation into a city. Built here rather than in /city invite so that one loaded from the
    /// database after a restart does what a fresh one does: invitations used to live only in memory,
    /// and a restart between invite and accept dropped them without a word to either side.
    /// </summary>
    public static class CityInvitation
    {
        public const string Kind = "city";

        public static Invitation Create(City city, PlayerInfo targetPlayer, long expiresAt)
        {
            return new Invitation(city, targetPlayer, expiresAt,
                () =>
                {
                    // The has-city check at invite time is not enough: the target can found
                    // their own city (becoming its mayor) between invite and accept. Joining
                    // here would overwrite their City via setCity and leave their old city's
                    // mayor pointer dangling, desyncing MAYOR_NAME from their permissions.
                    if (targetPlayer.hasCity())
                    {
                        MessageHandler.sendMsgToPlayerInfo(targetPlayer, Lang.Get("claims:you_already_have_city"));
                        // To the city, not to whoever invited: after a restart nobody is left to tell.
                        MessageHandler.sendMsgInCity(city, Lang.Get("claims:player_has_city_already"));
                        return;
                    }
                    city.AddLogEntry(EnumCityLogEvent.CitizenJoined, targetPlayer.GetPartName());
                    city.FireCitizenJoined(targetPlayer);
                    city.getCityCitizens().Add(targetPlayer);
                    targetPlayer.setCity(city);
                    city.saveToDatabase();
                    targetPlayer.saveToDatabase();
                    targetPlayer.PlayerCache.Reset();
                    TreeAttribute tree = new TreeAttribute();
                    tree.SetString("cityname", city.GetPartName());
                    claims.sapi.World.Api.Event.PushEvent("updatecityinfo", tree);
                    UsefullPacketsSend.SendPlayerRelatedInfoOnCityJoined(targetPlayer);
                    UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, EnumPlayerRelatedInfo.CITY_MEMBERS, EnumPlayerRelatedInfo.MAX_COUNT_PLOTS, EnumPlayerRelatedInfo.CITY_LOG);
                },
                () =>
                {
                    MessageHandler.sendMsgInCity(city, Lang.Get("claims:player_disagreed_with_invitation_to_city", targetPlayer.GetPartName(), city.GetPartName()));
                })
            {
                Kind = Kind
            };
        }
    }
}
