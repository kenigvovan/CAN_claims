using claims.src;
using Vintagestory.API.Client;

namespace claims.src.gui.prettyGui.GuiSecondaryTabs
{
    public abstract class CANGuiSecondaryTab
    {
        public ICoreClientAPI capi;
        public IconHandler iconHandler;
        private claimsGui _guiSys;
        protected claimsGui GuiSys => _guiSys ??= capi.ModLoader.GetModSystem<claimsGui>();
        public abstract void DrawTab();

        /// <summary>Runs a chat command on behalf of the player - how every button here acts on the world.</summary>
        protected static void SendCommand(string command) => ClientChat.Send(command);
    }
}
