using claims.src;
using Vintagestory.API.Client;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public abstract class CANGuiTab
    {
        public ICoreClientAPI capi;
        public IconHandler iconHandler;
        private claimsGui _guiSys;
        protected claimsGui GuiSys => _guiSys ??= capi.ModLoader.GetModSystem<claimsGui>();
        public abstract void DrawTab();
    }
}
