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

            var currentBounds = ctx.Current;
            currentBounds.WithAlignment(EnumDialogArea.LeftTop);
            currentBounds.fixedWidth = ctx.Line.fixedWidth;
            currentBounds = currentBounds.BelowCopy(0, 10);

            // The ImGui version used a row of highlightable buttons; horizontal tabs are the native
            // control that already carries "which one is active".
            GuiTab[] sortTabs = new GuiTab[SortOrder.Length];
            for (int i = 0; i < SortOrder.Length; i++)
            {
                sortTabs[i] = new GuiTab { Name = Lang.Get(SortLangKeys[i]), DataInt = i };
            }

            var tabsBounds = currentBounds.FlatCopy().WithFixedSize(ctx.Line.fixedWidth, 30);
            compo.AddHorizontalTabs(sortTabs, tabsBounds, (int value) =>
            {
                if (value < 0 || value >= SortOrder.Length) return;
                if (State.AllianceSort == SortOrder[value]) return;

                State.AllianceSort = SortOrder[value];
                Gui.BuildMainWindow();
            }, CairoFont.WhiteSmallText(), CairoFont.WhiteSmallText(), "allianceSortTabs");

            compo.GetHorizontalTabs("allianceSortTabs").activeElement = System.Array.IndexOf(SortOrder, State.AllianceSort);

            var list = ScrollableList.Add(Gui, currentBounds.BelowCopy(0, 30),
                Lang.Get("claims:gui_alliance_list_title"),
                Sorted().ToList(),
                (ClientAllianceInfoCellElement cell, ElementBounds bounds) => new GuiElementAllianceStatCell(compo.Api, cell, bounds) { On = true },
                new ScrollableListOptions { Key = "alliance-stats", HeightReserve = 300 });

            NavRow.Build(Gui, currentBounds, ctx.Line, 15,
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
