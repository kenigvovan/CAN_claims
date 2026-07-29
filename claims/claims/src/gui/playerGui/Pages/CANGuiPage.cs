using Vintagestory.API.Client;
using System;
using claims.src.gui.playerGui.structures;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// One page of the claims dialog.
    ///
    /// Pages are singletons created once by <see cref="PageRegistry"/>, so they must not keep
    /// composer elements, ElementBounds or textures between builds - the composer owns those and
    /// throws them away on every rebuild. Anything that has to survive belongs in ClaimsGuiState.
    ///
    /// Composing is not a page's job: CANClaimsGui.BuildMainWindow composes exactly once after the
    /// page has run. Pages used to call Compose() themselves, which is how one page ended up
    /// composing three times and another not at all.
    /// </summary>
    public abstract class CANGuiPage
    {
        protected CANClaimsGui Gui { get; private set; }
        protected ClaimsGuiState State => Gui.State;
        protected static ClientPlayerInfo Player => claims.clientDataStorage.clientPlayerInfo;

        internal void Build(CANClaimsGui gui, PageBuildContext ctx)
        {
            Gui = gui;
            if (!IsAvailable(out string reasonLangKey))
            {
                BuildUnavailable(ctx, reasonLangKey);
                return;
            }
            BuildContent(ctx);
        }

        protected abstract void BuildContent(PageBuildContext ctx);

        /// <summary>
        /// Whether the page has anything to show. Replaces the seven slightly different copies of
        /// "if (CityInfo == null) { Compose(); return; }" that pages open-coded.
        /// </summary>
        protected virtual bool IsAvailable(out string reasonLangKey)
        {
            reasonLangKey = null;
            return true;
        }

        /// <summary>
        /// Asked before the dialog opens on this page. A page that cannot show anything and has
        /// nothing to say about it would be a blank tab, so the dialog falls back to the city page.
        /// </summary>
        internal bool CanBeShown()
        {
            if (IsAvailable(out string reason)) return true;
            return reason != null;
        }

        /// <summary>Drawn instead of the page when IsAvailable said no.</summary>
        protected virtual void BuildUnavailable(PageBuildContext ctx, string reasonLangKey)
        {
            if (reasonLangKey == null) return;

            var bounds = ctx.Current.FlatCopy().BelowCopy(0, 40);
            bounds.WithAlignment(Vintagestory.API.Client.EnumDialogArea.CenterTop);
            ctx.Compo.AddStaticText(Lang.Get(reasonLangKey),
                Vintagestory.API.Client.CairoFont.WhiteMediumText()
                    .WithOrientation(Vintagestory.API.Client.EnumTextOrientation.Center),
                bounds);
        }

        protected void OpenDialog(EnumUpperWindowSelectedState dialog, Action<DialogArgs> fill = null)
            => Gui.OpenDialog(dialog, fill);

        protected void GoTo(EnumSelectedTab tab)
        {
            State.SelectedTab = tab;
            Gui.BuildMainWindow();
        }

        protected static void Send(string command) => ClientChat.Send(command);
    }
}
