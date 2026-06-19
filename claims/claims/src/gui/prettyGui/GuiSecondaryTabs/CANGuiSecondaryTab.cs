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
    }
}
