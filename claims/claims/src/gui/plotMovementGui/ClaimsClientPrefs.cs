using System;
using Vintagestory.API.Client;

namespace claims.src.gui.plotMovementGui
{
    /// <summary>
    /// The player's own display choices, kept on their machine. The server-side ShowPlotMovement
    /// setting had no command and no button left, and the panel never read it: nobody could turn
    /// anything off. A separate file, as claims.json is the server's config in singleplayer.
    /// </summary>
    public class ClaimsClientPrefs
    {
        private const string FileName = "claims_client.json";

        /// <summary>The city title and the short place line, see <see cref="LocationBanner"/>.</summary>
        public bool ShowBanner = true;
        /// <summary>Open the plot info panel on every plot change. Off: it opens on its hotkey only.</summary>
        public bool AutoShowPlotPanel = false;
        /// <summary>The flag and right squares over nearby plots, see <see cref="PlotMarkerRenderer"/>.</summary>
        public bool ShowPlotMarkers = false;
        /// <summary>The border grid around the player, see <see cref="PlotBorderRenderer"/>.</summary>
        public bool ShowPlotBorders = false;

        public static ClaimsClientPrefs Load(ICoreClientAPI capi)
        {
            try
            {
                return capi.LoadModConfig<ClaimsClientPrefs>(FileName) ?? new ClaimsClientPrefs();
            }
            catch (Exception e)
            {
                capi.Logger.Error("[claims] {0} could not be read, defaults used: {1}", FileName, e.Message);
                return new ClaimsClientPrefs();
            }
        }

        public void Save(ICoreClientAPI capi)
        {
            try
            {
                capi.StoreModConfig(this, FileName);
            }
            catch (Exception e)
            {
                capi.Logger.Warning("[claims] {0} could not be written: {1}", FileName, e.Message);
            }
        }
    }
}
