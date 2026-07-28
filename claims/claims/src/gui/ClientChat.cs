using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace claims.src.gui
{
    /// <summary>
    /// Runs a chat command as if the player had typed it - the way every GUI button acts on the world.
    /// Used to be open-coded (cast the world to ClientMain, grab its eventManager, trigger a Macro line)
    /// at well over a hundred call sites, each of them casting without a null check.
    /// </summary>
    public static class ClientChat
    {
        private static ClientMain cachedWorld;
        private static ClientEventManager cachedEvents;

        public static void Send(string command)
        {
            if (string.IsNullOrEmpty(command)) return;
            ResolveEvents()?.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, command, EnumChatType.Macro, "");
        }

        /// <summary>
        /// The world's event manager, resolved once and kept. The cache is keyed on the world instance:
        /// after a disconnect and reconnect the client builds a new ClientMain, and a blindly kept
        /// reference would send commands into the dead one.
        /// </summary>
        private static ClientEventManager ResolveEvents()
        {
            // A hard cast would crash on a world that is not a ClientMain (dedicated server loading the
            // assembly, shutdown race), so this stays a pattern match.
            if (claims.capi?.World is not ClientMain world) return null;

            if (!ReferenceEquals(world, cachedWorld))
            {
                cachedWorld = world;
                cachedEvents = world.eventManager;
            }
            return cachedEvents;
        }

        /// <summary>Drops the cached world on client shutdown so nothing keeps it alive.</summary>
        public static void Reset()
        {
            cachedWorld = null;
            cachedEvents = null;
        }
    }
}
