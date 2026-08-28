using System;
using System.Collections.Generic;
using Vintagestory.API.Client;

namespace claims.src.gui.hud
{
    /// <summary>What the layout editor needs to draw and move a ghost of a HUD panel.</summary>
    public interface IMovableHudPanel
    {
        /// <summary>Key the position is stored under; the panel's composer key.</summary>
        string LayoutKey { get; }
        string EditorLabel { get; }
        double EditorWidth { get; }
        double EditorHeight { get; }
        /// <summary>Unscaled top-left corner the panel's anchor would place it at.</summary>
        (double X, double Y) DefaultTopLeft(double screenW, double screenH, double height);
        /// <summary>Recompose at the (possibly changed) stored position.</summary>
        void OnLayoutChanged();
    }

    /// <summary>
    /// Where each HUD panel sits on this player's screen; client-side and cosmetic only. Positions
    /// are normalised (0..1 of the viewport) so a layout survives resolution changes; a panel never
    /// moved has no entry and keeps its hardcoded anchor.
    /// </summary>
    public class ClaimsHudLayout
    {
        /// <summary>The layout of the current client session; null server-side.</summary>
        public static ClaimsHudLayout Current;

        public class PanelPos
        {
            public float X;
            public float Y;
        }

        /// <summary>Keyed by the panel's composer key, the one name each panel already owns.</summary>
        public Dictionary<string, PanelPos> Panels { get; set; } = new Dictionary<string, PanelPos>();

        public PanelPos Get(string key)
        {
            Panels.TryGetValue(key, out PanelPos pos);
            return pos;
        }

        public void Set(string key, double normX, double normY)
        {
            Panels[key] = new PanelPos { X = (float)normX, Y = (float)normY };
        }

        public void ResetAll() => Panels.Clear();

        private const string FileName = "claims-hudlayout.json";

        public static void Load(ICoreClientAPI capi)
        {
            ClaimsHudLayout loaded = null;
            try { loaded = capi.LoadModConfig<ClaimsHudLayout>(FileName); }
            catch (Exception e)
            {
                // A corrupt layout file must not cost the player their HUD.
                capi.Logger.Warning("[claims] Could not read {0}, using default HUD layout: {1}", FileName, e.Message);
            }
            loaded = loaded ?? new ClaimsHudLayout();
            loaded.Panels = loaded.Panels ?? new Dictionary<string, PanelPos>();
            Current = loaded;
        }

        public void Save(ICoreClientAPI capi)
        {
            try { capi.StoreModConfig(this, FileName); }
            catch (Exception e)
            {
                capi.Logger.Warning("[claims] Could not save {0}: {1}", FileName, e.Message);
            }
        }
    }
}
