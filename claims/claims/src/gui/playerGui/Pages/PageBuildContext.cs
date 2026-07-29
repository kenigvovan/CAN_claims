using System;
using System.Collections.Generic;
using Vintagestory.API.Client;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// Everything a page needs for one rebuild, and nothing that outlives it. Pages are singletons,
    /// so anything scoped to a single build lives here rather than in a field.
    /// </summary>
    public sealed class PageBuildContext
    {
        /// <summary>The composer being built. Pages add elements to it but never compose it.</summary>
        public GuiComposer Compo;

        /// <summary>Cursor the page starts drawing from, below the tab bar.</summary>
        public ElementBounds Current;

        /// <summary>The separator line under the tab bar, used for full-width sizing.</summary>
        public ElementBounds Line;

        /// <summary>The dialog body, used for height calculations.</summary>
        public ElementBounds Main;

        private readonly List<Action> after = new List<Action>();

        /// <summary>
        /// Queues work that can only run once the composer has been composed - in practice setting
        /// scrollbar heights, which need the final laid-out sizes.
        /// </summary>
        public void AfterCompose(Action action) => after.Add(action);

        internal void RunAfterCompose()
        {
            foreach (var action in after) action();
            after.Clear();
        }
    }
}
