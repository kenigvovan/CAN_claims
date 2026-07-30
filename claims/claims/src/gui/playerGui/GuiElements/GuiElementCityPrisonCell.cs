using System.Collections.Generic;
using System.Linq;
using Cairo;
using claims.src.auxialiry;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.GuiElements
{
    /// <summary>A prison cell: which one it is, where it stands, and who is being held in it.</summary>
    public class GuiElementCityPrisonCell : CANGuiElementCellBase
    {
        /// <summary>How many inmates are named in the row before the rest become "and N more".</summary>
        private const int NamesShown = 2;

        /// <summary>Width of the occupancy stripe down the left edge.</summary>
        private const double MarkerWidth = 4.0;

        private readonly PrisonCellElement prisonCell;
        private readonly bool occupied;

        /// <summary>
        /// The cell's only action is its own remove button, so the row has nothing to click. Without
        /// this the base lit the three vanilla columns down the right edge, which read as if the
        /// plate were split into three parts.
        /// </summary>
        protected override bool UseHoverHighlights => false;

        /// <param name="number">Position in the city's cell list, so a cell can be named rather than
        /// identified only by its coordinates.</param>
        public GuiElementCityPrisonCell(ICoreClientAPI capi, PrisonCellElement prisonCell, ElementBounds bounds, int number)
            : base(capi, bounds)
        {
            this.prisonCell = prisonCell;
            this.occupied = prisonCell.Players.Count > 0;

            // Coordinates are shown relative to world spawn, which is what players read off the map.
            this.text = Lang.Get("claims:gui-prison-cell-coords",
                (prisonCell.SpawnPosition.X - capi.World.DefaultSpawnPosition.AsBlockPos.X).ToString(),
                (prisonCell.SpawnPosition.Y - capi.World.DefaultSpawnPosition.AsBlockPos.Y).ToString(),
                (prisonCell.SpawnPosition.Z - capi.World.DefaultSpawnPosition.AsBlockPos.Z).ToString());

            var font = CairoFont.WhiteDetailText();
            double textWidth = Bounds.fixedWidth - 60;
            if (textWidth < 60) textWidth = 60;

            // The cell's number heads the row: coordinates identify it to the server, but a player
            // reading the list wants to tell one cell from another at a glance. They follow, muted.
            var titleBounds = ElementBounds.Fixed(16, 4, textWidth, 22).WithParent(Bounds);
            AddTitle(Lang.Get("claims:gui-prison-cell-number", number), titleBounds);

            var coordsBounds = ElementBounds.Fixed(16, 26, textWidth, 18).WithParent(Bounds);
            AddSubtitle(this.text, coordsBounds, 14);

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

            // Who is inside, by name. A bare count meant hovering every row to find a given player;
            // the names are what the list is consulted for. Laid out one box per prisoner, they used
            // to stretch the cell to whatever height the list happened to need.
            var occupantsBounds = ElementBounds.Fixed(16, 46, textWidth, 20).WithParent(Bounds);

            if (!occupied)
            {
                AddLabelValue(Lang.Get("claims:gui-prison-cell-occupants"),
                    Lang.Get("claims:gui-prison-cell-empty"), occupantsBounds, 110, ClaimsColors.Label);
            }
            else
            {
                AddLabelValue(Lang.Get("claims:gui-prison-cell-occupants"),
                    NameList(prisonCell.Players), occupantsBounds, 110, ClaimsColors.Danger);

                // The full roster stays on hover, for when the row only had room for the first few.
                AddTooltip(ElementBounds.Fixed(0, 0, textWidth, MinCellHeight).WithParent(Bounds),
                    StringFunctions.concatStringsWithDelim(prisonCell.Players, ','));
            }

            Bounds.fixedHeight = MinCellHeight;
        }

        /// <summary>The first few inmates by name, with a count standing in for the rest.</summary>
        private static string NameList(ICollection<string> players)
        {
            string names = string.Join(", ", players.Take(NamesShown).Select(StringFunctions.replaceUnderscore));

            int rest = players.Count - NamesShown;
            return rest > 0 ? names + " " + Lang.Get("claims:gui-prison-cell-more", rest) : names;
        }

        /// <summary>
        /// Occupancy as a stripe down the left edge - the one thing worth spotting without reading
        /// the row. Everything else is drawn by the base from richTexts and children.
        /// </summary>
        protected override void ComposeContent(Context ctx, ImageSurface surface)
        {
            double[] color = occupied ? ClaimsColors.Danger : ClaimsColors.Label;

            ctx.SetSourceRGBA(color[0], color[1], color[2], occupied ? 0.9 : 0.35);
            GuiElement.RoundRectangle(ctx, GuiElement.scaled(3.0), GuiElement.scaled(6.0),
                GuiElement.scaled(MarkerWidth), Bounds.OuterHeight - GuiElement.scaled(12.0), 1.0);
            ctx.Fill();
        }
    }
}
