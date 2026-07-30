using System.Collections.Generic;
using System.Linq;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>Every alliance in the world, orderable by age, size or name.</summary>
    public sealed class AllianceListPage : CANGuiPage
    {
        /// <summary>Height of the anchor the list is measured from.</summary>
        private const double HeadingHeight = 24;

        /// <summary>The list is never squeezed below this, however little room is left.</summary>
        private const double MinListHeight = 70;

        /// <summary>Height of the sort tab strip inside its card.</summary>
        private const double TabsHeight = 30;

        private static readonly EnumAllianceSort[] SortOrder =
        {
            EnumAllianceSort.Default, EnumAllianceSort.Oldest, EnumAllianceSort.Cities, EnumAllianceSort.Name
        };

        private static readonly string[] SortLangKeys =
        {
            "claims:gui-alliancelist-sort-default",
            "claims:gui-alliancelist-sort-oldest",
            "claims:gui-alliancelist-sort-cities",
            "claims:gui-alliancelist-sort-name"
        };

        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            // The sort control gets the same framed heading the city list has, instead of floating
            // above the list with nothing to name it.
            var column = ctx.Line.BelowCopy(0, 14);
            column.Alignment = EnumDialogArea.LeftTop;
            column.fixedWidth = ctx.Line.fixedWidth;
            column.fixedHeight = HeadingHeight;

            double sortHeight = Card.HeaderHeight + TabsHeight + Card.Padding * 2;
            ElementBounds sortInner = Card.Frame(compo, column, column.fixedY, sortHeight,
                Lang.Get("claims:gui-alliancelist-sort-label"));

            // The ImGui version used a row of highlightable buttons; horizontal tabs are the native
            // control that already carries "which one is active".
            GuiTab[] sortTabs = new GuiTab[SortOrder.Length];
            for (int i = 0; i < SortOrder.Length; i++)
            {
                sortTabs[i] = new GuiTab { Name = Lang.Get(SortLangKeys[i]), DataInt = i };
            }

            var tabsBounds = sortInner.FlatCopy().WithFixedHeight(TabsHeight);
            compo.AddHorizontalTabs(sortTabs, tabsBounds, (int value) =>
            {
                if (value < 0 || value >= SortOrder.Length) return;
                if (State.AllianceSort == SortOrder[value]) return;

                State.AllianceSort = SortOrder[value];
                Gui.BuildMainWindow();
            }, CairoFont.WhiteSmallText(), CairoFont.WhiteSmallText(), "allianceSortTabs");

            compo.GetHorizontalTabs("allianceSortTabs").activeElement = System.Array.IndexOf(SortOrder, State.AllianceSort);

            var listAnchor = column.FlatCopy();
            listAnchor.fixedY = column.fixedY + sortHeight + Card.Gap;

            // The list fills what is left down to the navigation row, rather than a fixed reserve
            // that had to be re-tuned whenever anything above it changed height.
            var listOpts = new ScrollableListOptions { Key = "alliance-stats", TitleHeightShrink = 0 };
            listOpts.HeightReserve = ScrollableList.ReserveFor(Gui,
                System.Math.Max(MinListHeight,
                    Gui.mainBounds.fixedHeight * NavRow.LineHeightFraction
                        - listAnchor.fixedY - Card.Gap - ScrollableList.Overhead(listAnchor, listOpts)));

            var list = ScrollableList.Add(Gui, listAnchor,
                Lang.Get("claims:gui_alliance_list_title"),
                Sorted().ToList(),
                (ClientAllianceInfoCellElement cell, ElementBounds bounds) => new GuiElementAllianceStatCell(compo.Api, cell, bounds) { On = true },
                listOpts);

            NavRow.Build(Gui, column, ctx.Line, 15,
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.AllianceInfoPage), Lang.Get("claims:gui-nav-back")));

            ctx.AfterCompose(() => list.ApplyScrollbarHeights(compo));
        }

        private IEnumerable<ClientAllianceInfoCellElement> Sorted()
        {
            var list = Player.AllAlliancesList ?? new List<ClientAllianceInfoCellElement>();
            switch (State.AllianceSort)
            {
                case EnumAllianceSort.Oldest: return list.OrderBy(a => a.TimeStampCreated);
                case EnumAllianceSort.Cities: return list.OrderByDescending(a => a.CitiesCount);
                case EnumAllianceSort.Name: return list.OrderBy(a => a.Name);
                default: return list;
            }
        }
    }
}
