using Cairo;
using claims.src.gui.playerGui.structures.cellElements;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>
    /// A summon point: its name, its coordinates, and buttons to teleport to it or rename it.
    /// </summary>
    public class GuiElementCitySummonCell : CANGuiElementCellBase
    {
        private const double ButtonSize = 32;
        private const double EdgePadding = 12;

        private readonly SummonCellElement summonCell;

        protected override double MinCellHeight => 74.0;

        /// <summary>
        /// Nothing happens when the row itself is clicked - everything is on the two buttons - so
        /// the three lit columns only promised interaction the cell does not have.
        /// </summary>
        protected override bool UseHoverHighlights => false;

        public GuiElementCitySummonCell(ICoreClientAPI capi, SummonCellElement summonCell, ElementBounds bounds)
            : base(capi, bounds)
        {
            this.summonCell = summonCell;

            var font = CairoFont.WhiteDetailText();

            // Buttons sit against the right edge rather than after the name, so they line up down
            // the list instead of shifting with the length of each point's name.
            bool canRename = claims.clientDataStorage.clientPlayerInfo.PlayerPermissions
                    .HasPermission(rights.EnumPlayerPermissions.CITY_SET_SUMMON);

            double buttonsWidth = canRename ? ButtonSize * 2 + 8 : ButtonSize;
            double textWidth = Bounds.fixedWidth - buttonsWidth - EdgePadding * 2 - 10;
            if (textWidth < 60) textWidth = 60;

            ElementBounds nameBounds = ElementBounds.Fixed(EdgePadding, 9, textWidth, 26).WithParent(Bounds);
            AddTitle(summonCell.Name.Length > 0 ? summonCell.Name : Lang.Get("claims:gui-summon-unnamed"), nameBounds);

            // Coordinates relative to world spawn, same as the prison cells show.
            this.text = Lang.Get("claims:gui-prison-cell-coords",
                (summonCell.SpawnPosition.X - capi.World.DefaultSpawnPosition.AsBlockPos.X).ToString(),
                (summonCell.SpawnPosition.Y - capi.World.DefaultSpawnPosition.AsBlockPos.Y).ToString(),
                (summonCell.SpawnPosition.Z - capi.World.DefaultSpawnPosition.AsBlockPos.Z).ToString());

            var coordsBounds = ElementBounds.Fixed(EdgePadding + 2, 38, textWidth, 24).WithParent(Bounds);
            AddSubtitle(this.text, coordsBounds);

            // Placed by an explicit X off the cell width: EnumDialogArea alignment is resolved at
            // CalcWorldBounds against the parent, which is not what the cell draws its children
            // against, so the button would land outside the composed surface.
            double cellWidth = Bounds.fixedWidth > 0 ? Bounds.fixedWidth : 430;
            double rightOffset = cellWidth - EdgePadding - ButtonSize;

            if (canRename)
            {
                var renameBounds = ElementBounds
                    .Fixed(rightOffset, 21, ButtonSize, ButtonSize)
                    .WithParent(Bounds);
                children.Add(new GuiElementToggleButton(capi, "claims:pencil", "", font, (bool t) =>
                {
                    if (t)
                    {
                        claims.CANCityGui.OpenDialog(EnumUpperWindowSelectedState.CITY_SUMMON_NEED_NAME,
                            args => args.Pos = this.summonCell.SpawnPosition);
                    }
                }, renameBounds));
                AddTooltip(renameBounds, Lang.Get("claims:gui-summon-rename-tooltip"));

                rightOffset -= ButtonSize + 8;
            }

            var useBounds = ElementBounds
                .Fixed(rightOffset, 21, ButtonSize, ButtonSize)
                .WithParent(Bounds);
            children.Add(new GuiElementToggleButton(capi, "claims:magic-portal", "", font, (bool t) =>
            {
                if (t) ClientChat.Send("/c summon use " + this.summonCell.Name);
            }, useBounds));
            AddTooltip(useBounds, Lang.Get("claims:gui-summon-use-tooltip"));

            Bounds.fixedHeight = MinCellHeight;
        }

        /// <summary>Everything visible is drawn by the base from richTexts and children.</summary>
        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
        }
    }
}
