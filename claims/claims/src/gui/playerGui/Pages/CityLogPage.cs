using claims.src.auxialiry;
using claims.src.citylog;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>The city's event history, newest first.</summary>
    public sealed class CityLogPage : CANGuiPage
    {
        protected override bool IsAvailable(out string reasonLangKey)
        {
            reasonLangKey = "claims:you_dont_have_city";
            return Player.CityInfo != null;
        }

        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            var currentBounds = ctx.Current.BelowCopy(0, 20);
            currentBounds.fixedWidth = ctx.Line.fixedWidth;
            currentBounds.WithAlignment(EnumDialogArea.CenterTop);

            compo.AddStaticText(Lang.Get("claims:gui-city-log-title"), ClaimsFonts.ListHeader, currentBounds);

            var log = Player.CityInfo.EventLog;
            if (log == null || log.Count == 0)
            {
                compo.AddStaticText(Lang.Get("claims:gui-city-log-empty"),
                    ClaimsFonts.ListHeader, currentBounds.BelowCopy(0, 20));
                NavRow.Build(Gui, currentBounds, ctx.Line, 15,
                    new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.City), Lang.Get("claims:gui-nav-back")));
                return;
            }

            // A scrolling container of rich text rows rather than a cell list: entries are plain
            // lines with no controls of their own.
            int rowHeight = 25;
            var areaBounds = ElementBounds.Fixed(15, 0, ctx.Line.fixedWidth - 40, Gui.mainBounds.fixedHeight - 260)
                .FixedUnder(currentBounds, 15);

            ElementBounds insetBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, areaBounds.fixedWidth, areaBounds.fixedHeight);
            // 7px clear of the inset, the same gap the shared list widget uses.
            ElementBounds scrollbarBounds = insetBounds.RightCopy(7).WithFixedWidth(20);
            ElementBounds clipBounds = insetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
            ElementBounds containerBounds = clipBounds.FlatCopy();
            ElementBounds rowBounds = ElementBounds.Fixed(0, 0, areaBounds.fixedWidth - 20, rowHeight);

            compo.BeginChildElements(areaBounds)
                .AddInset(insetBounds, 3)
                    .BeginClip(clipBounds)
                        .AddContainer(containerBounds, "citylog-content")
                    .EndClip()
                    .AddVerticalScrollbar((value) =>
                    {
                        ElementBounds bounds = compo.GetContainer("citylog-content").Bounds;
                        bounds.fixedY = 5 - value;
                        bounds.CalcWorldBounds();
                    }, scrollbarBounds, "citylog-scrollbar")
                .EndChildElements();

            GuiElementContainer scrollArea = compo.GetContainer("citylog-content");
            for (int i = log.Count - 1; i >= 0; i--)
            {
                var entry = log[i];
                string line = string.Format("[{0}] {1}",
                    TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(entry.Timestamp),
                    FormatEvent(entry));

                scrollArea.Add(new GuiElementRichtext(compo.Api,
                    VtmlUtil.Richtextify(compo.Api, line, CairoFont.WhiteDetailText()), rowBounds));
                rowBounds = rowBounds.BelowCopy();
            }

            int entryCount = log.Count;
            ctx.AfterCompose(() =>
                compo.GetScrollbar("citylog-scrollbar").SetHeights((float)clipBounds.fixedHeight, rowHeight * entryCount));

            NavRow.Build(Gui, currentBounds, ctx.Line, 15,
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.City), Lang.Get("claims:gui-nav-back")));
        }

        private static string FormatEvent(CityLogEntry entry)
        {
            string args0 = entry.Args.Count > 0 ? entry.Args[0] : "";
            string args1 = entry.Args.Count > 1 ? entry.Args[1] : "";
            string args2 = entry.Args.Count > 2 ? entry.Args[2] : "";
            string args3 = entry.Args.Count > 3 ? entry.Args[3] : "";

            switch (entry.EventType)
            {
                case EnumCityLogEvent.CityCreated: return Lang.Get("claims:log-city-created", args0);
                case EnumCityLogEvent.CitizenJoined: return Lang.Get("claims:log-citizen-joined", args0);
                case EnumCityLogEvent.CitizenLeft: return Lang.Get("claims:log-citizen-left", args0);
                case EnumCityLogEvent.CitizenKicked: return Lang.Get("claims:log-citizen-kicked", args0);
                case EnumCityLogEvent.MayorChanged: return Lang.Get("claims:log-mayor-changed", args0);
                case EnumCityLogEvent.ConflictDeclared: return Lang.Get("claims:log-conflict-declared", args0);
                case EnumCityLogEvent.FlagCaptured: return Lang.Get("claims:log-flag-captured", args0, args1);
                case EnumCityLogEvent.AllianceJoined: return Lang.Get("claims:log-alliance-joined", args0);
                case EnumCityLogEvent.AllianceLeft: return Lang.Get("claims:log-alliance-left", args0);
                case EnumCityLogEvent.TreasuryPillaged: return Lang.Get("claims:log-treasury-pillaged", args0, args1);
                case EnumCityLogEvent.WarEnded: return Lang.Get("claims:log-war-ended", args0, args1, args2, args3);
                case EnumCityLogEvent.UnionFormed: return Lang.Get("claims:log-union-formed", args0);
                case EnumCityLogEvent.UnionBreakAnnounced: return Lang.Get("claims:log-union-break-announced", args0, args1);
                case EnumCityLogEvent.UnionBroken: return Lang.Get("claims:log-union-broken", args0);
                default: return entry.EventType.ToString();
            }
        }
    }
}
