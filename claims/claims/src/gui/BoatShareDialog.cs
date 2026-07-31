using claims.src.gui.playerGui.Widgets;
using claims.src.network.packets;
using claims.src.part.structure;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace claims.src.gui
{
    /// <summary>
    /// Asks the owner of the boat under the crosshair who else may use it.
    ///
    /// The boat is remembered on open: the mouse is freed afterwards, so the player may look away.
    /// Ownership and distance are re-checked server-side.
    /// </summary>
    public class BoatShareDialog : GuiDialog
    {
        private const double ButtonWidth = 150;
        private const double ButtonHeight = 28;
        private const double Gap = 6;

        private long boatEntityId;
        private BoatShareMode current;

        public BoatShareDialog(ICoreClientAPI capi) : base(capi)
        {
        }

        public override string ToggleKeyCombinationCode => "claimsboatshare";

        /// <summary>The boat under the crosshair if this player owns it, null otherwise.</summary>
        public static Entity OwnedBoatInSight(ICoreClientAPI capi)
        {
            Entity entity = capi.World.Player?.CurrentEntitySelection?.Entity;
            if (entity?.GetBehavior<EntityBehaviorOwnable>() == null) return null;

            var ownedby = entity.WatchedAttributes.GetTreeAttribute("ownedby");
            if (ownedby == null) return null;
            if (ownedby.GetString("uid", "") != capi.World.Player.PlayerUID) return null;

            return entity;
        }

        /// <summary>
        /// Picks up the boat in sight and composes for it, refusing to open when there is none.
        ///
        /// Overridden rather than handled in the hotkey: GuiDialog binds its own handler to
        /// ToggleKeyCombinationCode and calls TryOpen directly, so both routes have to end up here.
        /// </summary>
        public override bool TryOpen()
        {
            if (!claims.config.BOAT_SHARE_WITH_CITY) return false;

            Entity boat = OwnedBoatInSight(capi);
            if (boat == null)
            {
                capi.TriggerIngameError(this, "noboatinsight", Lang.Get("claims:boat-look-at-one"));
                return false;
            }

            boatEntityId = boat.EntityId;
            current = BoatShareModeHelper.Of(boat);
            Compose(boat.GetName());

            return base.TryOpen();
        }

        private void Compose(string boatName)
        {
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            var composer = capi.Gui.CreateCompo("claims-boat-share", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(Lang.Get("claims:boat-share-title"), () => TryClose())
                .BeginChildElements(bgBounds);

            ElementBounds row = ElementBounds.Fixed(0, 30, ButtonWidth, 20);
            composer.AddStaticText(boatName ?? "", CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label),
                row, "boat-name");

            // The current mode in words; the buttons below only say what it can become.
            row = row.BelowCopy(0, 2);
            composer.AddStaticText(Lang.Get("claims:boat-share-current",
                    Lang.Get(BoatShareModeHelper.LangKeyOf(current))),
                CairoFont.WhiteSmallishText().WithColor(ClaimsColors.Value), row, "boat-current");

            row = row.BelowCopy(0, Gap).WithFixedHeight(ButtonHeight);
            AddModeButton(composer, ref row, BoatShareMode.PERSONAL);
            AddModeButton(composer, ref row, BoatShareMode.CITY);

            // Hidden where the host disallows it - the button would do nothing.
            if (claims.config.BOAT_SHARE_WITH_ALLIANCE)
            {
                AddModeButton(composer, ref row, BoatShareMode.ALLIANCE);
            }

            SingleComposer = composer.EndChildElements().Compose();
        }

        private void AddModeButton(GuiComposer composer, ref ElementBounds row, BoatShareMode mode)
        {
            string code = BoatShareModeHelper.CodeOf(mode);
            composer.AddButton(Lang.Get(BoatShareModeHelper.LangKeyOf(mode)), new ActionConsumable(() =>
            {
                Apply(mode);
                return true;
            }), row.FlatCopy(), mode == current ? EnumButtonStyle.Small : EnumButtonStyle.Normal,
                "boat-mode-" + code);

            row = row.BelowCopy(0, Gap);
        }

        private void Apply(BoatShareMode mode)
        {
            claims.clientChannel.SendPacket(new SavedPlotsPacket
            {
                type = PacketsContentEnum.CLIENT_SET_BOAT_SHARE,
                data = boatEntityId + ";" + (int)mode
            });
            TryClose();
        }
    }
}
