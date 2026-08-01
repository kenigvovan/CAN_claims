using System.Linq;
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

        /// <summary>Height of one log line, and the width the scrollbar needs beside the entries.</summary>
        private const int RowHeight = 25;
        private const double ScrollbarReserve = 27;

        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            var anchor = ctx.Line.BelowCopy(0, 14);
            anchor.Alignment = EnumDialogArea.LeftTop;

            var column = anchor.FlatCopy();
            column.fixedWidth = ctx.Line.fixedWidth;

            // The log is the whole page, so its card runs down to just above the navigation row
            // rather than to a height picked to look about right.
            double cardHeight = Gui.mainBounds.fixedHeight * NavRow.LineHeightFraction
                              - column.fixedY - Card.Gap;

            var log = Player.CityInfo.EventLog;

            ElementBounds inner = Card.Frame(compo, column, column.fixedY, cardHeight,
                Lang.Get("claims:gui-city-log-title"));

            if (log == null || log.Count == 0)
            {
                compo.AddStaticText(Lang.Get("claims:gui-city-log-empty"),
                    CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label),
                    inner.FlatCopy().WithFixedHeight(24), "citylog-empty");

                BuildNav(ctx);
                return;
            }

            // A scrolling container of rich text rows rather than a cell list: entries are plain
            // lines with no controls of their own. The card's own frame is the border, so this no
            // longer draws an inset of its own inside it.
            // The clip is the outermost of these and is added to the composer itself, so it is what
            // everything else forks from: a bounds that is only forked from and never added has no
            // parent, and the renderer dereferences that parent while pushing the scissor.
            ElementBounds clipBounds = ElementBounds.Fixed(0, 0,
                inner.fixedWidth - ScrollbarReserve, inner.fixedHeight);
            // 7px clear of the entries, the same gap the shared list widget uses.
            ElementBounds scrollbarBounds = clipBounds.RightCopy(7).WithFixedWidth(20);
            ElementBounds containerBounds = clipBounds.FlatCopy();
            ElementBounds rowBounds = ElementBounds.Fixed(0, 0, clipBounds.fixedWidth - 10, RowHeight);

            compo.BeginChildElements(inner)
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

            var stampFont = CairoFont.WhiteDetailText().WithColor(ClaimsColors.Label);

            GuiElementContainer scrollArea = compo.GetContainer("citylog-content");
            for (int i = log.Count - 1; i >= 0; i--)
            {
                var entry = log[i];

                // Timestamp and text are two runs rather than one string: the time is what the eye
                // skips over, the event is what it looks for, so they are not the same colour.
                var stamp = VtmlUtil.Richtextify(compo.Api,
                    TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(entry.Timestamp) + "  ", stampFont);
                var text = VtmlUtil.Richtextify(compo.Api, FormatEvent(entry),
                    CairoFont.WhiteDetailText().WithColor(ColorFor(entry.EventType)));

                scrollArea.Add(new GuiElementRichtext(compo.Api, stamp.Concat(text).ToArray(), rowBounds));
                rowBounds = rowBounds.BelowCopy();
            }

            int entryCount = log.Count;
            ctx.AfterCompose(() =>
                compo.GetScrollbar("citylog-scrollbar").SetHeights((float)clipBounds.fixedHeight, RowHeight * entryCount));

            BuildNav(ctx);
        }

        private void BuildNav(PageBuildContext ctx)
        {
            NavRow.Build(Gui, ctx.Current, ctx.Line, 0,
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.City), Lang.Get("claims:gui-nav-back")));
        }

        /// <summary>
        /// What an entry means at a glance: losses and hostilities in the danger colour, pacts and
        /// arrivals in the friendly one, notice given of something ending in the warning colour.
        /// </summary>
        private static double[] ColorFor(EnumCityLogEvent eventType)
        {
            switch (eventType)
            {
                case EnumCityLogEvent.ConflictDeclared:
                case EnumCityLogEvent.FlagCaptured:
                case EnumCityLogEvent.TreasuryPillaged:
                case EnumCityLogEvent.CitizenKicked:
                case EnumCityLogEvent.UnionBroken:
                    return ClaimsColors.Danger;

                case EnumCityLogEvent.CitizenJoined:
                case EnumCityLogEvent.AllianceJoined:
                case EnumCityLogEvent.UnionFormed:
                    return ClaimsColors.Success;

                case EnumCityLogEvent.CitizenLeft:
                case EnumCityLogEvent.AllianceLeft:
                case EnumCityLogEvent.UnionBreakAnnounced:
                    return ClaimsColors.Warning;

                default:
                    return ClaimsColors.Value;
            }
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
                case EnumCityLogEvent.VillageFounded: return Lang.Get("claims:log-village-founded", args0);
                case EnumCityLogEvent.VillageUpgraded: return Lang.Get("claims:log-village-upgraded", args0);
                default: return entry.EventType.ToString();
            }
        }
    }
}
