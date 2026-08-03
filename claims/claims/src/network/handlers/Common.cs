using claims.src.network.packets;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace claims.src.network.handlers
{
    public static class Common
    {
        public static void RegisterMessageTypes(INetworkChannel channel, ICoreAPI api)
        {
            channel.RegisterMessageType(typeof(SavedPlotsPacket));
            channel.RegisterMessageType(typeof(PlayerGuiRelatedInfoPacket));
            channel.RegisterMessageType(typeof(ConfigUpdateValuesPacket));
            channel.RegisterMessageType(typeof(BountyBoardPacket));
            if (claims.config.VERBOSE_LOGGING)
            {
                api.Logger.VerboseDebug("[claims] RegisterMessageType(SavedPlotsPacket)");               
                api.Logger.VerboseDebug("[claims] RegisterMessageType(PlayerGuiRelatedInfoPacket)");              
                api.Logger.VerboseDebug("[claims] RegisterMessageType(ConfigUpdateValuesPacket)");
                
            }
        }
    }
}
