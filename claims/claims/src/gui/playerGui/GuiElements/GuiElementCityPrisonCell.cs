using Cairo;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>A prison cell: its coordinates plus the players currently held in it.</summary>
    public class GuiElementCityPrisonCell : CANGuiElementCellBase
    {
        private readonly PrisonCellElement prisonCell;

        public GuiElementCityPrisonCell(ICoreClientAPI capi, PrisonCellElement prisonCell, ElementBounds bounds)
            : base(capi, bounds)
        {
            this.prisonCell = prisonCell;

            // Coordinates are shown relative to world spawn, which is what players read off the map.
            this.text = Lang.Get("claims:gui-prison-cell-coords",
                (prisonCell.SpawnPosition.X - capi.World.DefaultSpawnPosition.AsBlockPos.X).ToString(),
                (prisonCell.SpawnPosition.Y - capi.World.DefaultSpawnPosition.AsBlockPos.Y).ToString(),
                (prisonCell.SpawnPosition.Z - capi.World.DefaultSpawnPosition.AsBlockPos.Z).ToString());

            var font = CairoFont.WhiteDetailText();
            double textWidth = Bounds.fixedWidth - 60;
            if (textWidth < 60) textWidth = 60;

            // Heading is the cell's position - that is what identifies it - with its occupants under
            // it, the same shape the other list cells use.
            var titleBounds = ElementBounds.Fixed(12, 6, textWidth, 22).WithParent(Bounds);
            AddTitle(this.text, titleBounds);

            // Removing a cell is a permission, and it needs the cell's position to know which one.
            var perms = claims.clientDataStorage.clientPlayerInfo.PlayerPermissions;
            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_PRISON_REMOVE_CELL)
             || perms.HasPermission(rights.EnumPlayerPermissions.CITY_PRISON_ALL))
            {
                double cellWidth = Bounds.fixedWidth > 0 ? Bounds.fixedWidth : 430;
                var removeBounds = ElementBounds.Fixed(cellWidth - 44, 20, 32, 32).WithParent(Bounds);
                children.Add(new GuiElementToggleButton(capi, "claims:cancel", "", font, (bool t) =>
                {
                    if (t)
                    {
                        claims.CANCityGui.OpenDialog(EnumUpperWindowSelectedState.CITY_PRISON_REMOVE_CELL_CONFIRM,
                            args => args.Pos = this.prisonCell.SpawnPosition);
                    }
                }, removeBounds));
                AddTooltip(removeBounds, Lang.Get("claims:gui-prison-remove-cell-tooltip"));
            }

            // Occupants as a count with the names on hover. Laid out one box per prisoner, they used
            // to stretch the cell to whatever height the list happened to need.
            var occupantsBounds = titleBounds.BelowCopy(0, 4).WithFixedHeight(20);

            if (prisonCell.Players.Count == 0)
            {
                AddLabelValue(Lang.Get("claims:gui-prison-cell-occupants"),
                    Lang.Get("claims:gui-prison-cell-empty"), occupantsBounds, 110, ClaimsColors.Label);
            }
            else
            {
                AddLabelValue(Lang.Get("claims:gui-prison-cell-occupants"),
                    prisonCell.Players.Count.ToString(), occupantsBounds, 110, ClaimsColors.Danger);

                AddTooltip(ElementBounds.Fixed(0, 0, textWidth, 56).WithParent(Bounds),
                    StringFunctions.concatStringsWithDelim(prisonCell.Players, ','));
            }

            Bounds.fixedHeight = MinCellHeight;
        }

        /// <summary>Everything visible is drawn by the base from richTexts and children.</summary>
        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
        }
    }
}
