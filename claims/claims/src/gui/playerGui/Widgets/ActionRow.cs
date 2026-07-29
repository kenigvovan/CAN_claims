using System;
using Vintagestory.API.Client;

namespace claims.src.gui.playerGui.Widgets
{
    /// <summary>
    /// The strip of icon buttons a card ends with. Walks left to right on its own, so a page only
    /// says which buttons the player is allowed to see - every page used to repeat the same
    /// "bounds, RightCopy, tooltip" three lines per button.
    /// </summary>
    public sealed class ActionRow
    {
        private readonly GuiComposer compo;
        private ElementBounds slot;

        public ActionRow(GuiComposer compo, ElementBounds firstSlot)
        {
            this.compo = compo;
            this.slot = firstSlot;
        }

        /// <summary>An icon button that runs <paramref name="onClick"/>, named by its tooltip.</summary>
        public ElementBounds Add(string icon, string key, Action<bool> onClick, string tooltip, bool toggleable = false)
        {
            ElementBounds bounds = slot;

            compo.AddIconButton(icon, onClick, bounds, key);
            if (toggleable) compo.GetToggleButton(key).Toggleable = true;
            if (tooltip != null) Tooltip.Add(compo, tooltip, bounds, "tip-" + key);

            slot = Card.NextAction(bounds);
            return bounds;
        }
    }
}
