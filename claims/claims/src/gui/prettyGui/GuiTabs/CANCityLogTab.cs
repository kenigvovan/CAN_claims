using claims.src.auxialiry;
using claims.src.citylog;
using ImGuiNET;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANCityLogTab : CANGuiTab
    {
        public CANCityLogTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }

        public override void DrawTab()
        {
            var clientInfo = claims.clientDataStorage?.clientPlayerInfo;
            if (clientInfo?.CityInfo == null)
            {
                ImGui.TextDisabled(Lang.Get("claims:you_dont_have_city"));
                return;
            }

            if (BackButton())
                GuiSys.selectedTab = EnumSelectedTab.CITY;

            ImGui.Separator();
            ImGui.Spacing();

            ImGui.PushStyleColor(ImGuiCol.Text, ColValue);
            ImGui.Text(Lang.Get("claims:gui-city-log-title"));
            ImGui.PopStyleColor();

            ImGui.Spacing();

            var log = clientInfo.CityInfo.EventLog;
            if (log == null || log.Count == 0)
            {
                ImGui.TextDisabled(Lang.Get("claims:gui-city-log-empty"));
                return;
            }

            ImGui.BeginChild("citylogscroll", new Vector2(0, 0), true);

            for (int i = log.Count - 1; i >= 0; i--)
            {
                var entry = log[i];
                string dateStr = TimeFunctions.getDateFromEpochSecondsWithHoursMinutes(entry.Timestamp);
                string eventText = FormatEvent(entry);
                Vector4 entryColor = GetEventColor(entry.EventType);

                ImGui.PushStyleColor(ImGuiCol.Text, ColHint);
                ImGui.Text($"[{dateStr}]");
                ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, entryColor);
                ImGui.TextWrapped(eventText);
                ImGui.PopStyleColor();
            }

            ImGui.EndChild();
        }

        private static string FormatEvent(CityLogEntry entry)
        {
            string args0 = entry.Args.Count > 0 ? entry.Args[0] : "";
            string args1 = entry.Args.Count > 1 ? entry.Args[1] : "";

            return entry.EventType switch
            {
                EnumCityLogEvent.CityCreated      => Lang.Get("claims:log-city-created", args0),
                EnumCityLogEvent.CitizenJoined    => Lang.Get("claims:log-citizen-joined", args0),
                EnumCityLogEvent.CitizenLeft      => Lang.Get("claims:log-citizen-left", args0),
                EnumCityLogEvent.CitizenKicked    => Lang.Get("claims:log-citizen-kicked", args0),
                EnumCityLogEvent.MayorChanged     => Lang.Get("claims:log-mayor-changed", args0),
                EnumCityLogEvent.ConflictDeclared => Lang.Get("claims:log-conflict-declared", args0),
                EnumCityLogEvent.FlagCaptured     => Lang.Get("claims:log-flag-captured", args0, args1),
                EnumCityLogEvent.AllianceJoined   => Lang.Get("claims:log-alliance-joined", args0),
                EnumCityLogEvent.AllianceLeft     => Lang.Get("claims:log-alliance-left", args0),
                _                                 => entry.EventType.ToString()
            };
        }

        private static readonly Vector4 ColEventDefault       = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        private static readonly Vector4 ColEventCityCreated   = new Vector4(0.4f, 0.9f, 0.4f, 1.0f);
        private static readonly Vector4 ColEventJoined        = new Vector4(0.5f, 0.85f, 0.5f, 1.0f);
        private static readonly Vector4 ColEventLeft          = new Vector4(0.75f, 0.75f, 0.75f, 1.0f);
        private static readonly Vector4 ColEventKicked        = new Vector4(0.9f, 0.5f, 0.3f, 1.0f);
        private static readonly Vector4 ColEventMayor         = new Vector4(1.0f, 0.85f, 0.3f, 1.0f);
        private static readonly Vector4 ColEventConflict      = new Vector4(0.9f, 0.25f, 0.25f, 1.0f);
        private static readonly Vector4 ColEventFlagCaptured  = new Vector4(0.9f, 0.4f, 0.1f, 1.0f);
        private static readonly Vector4 ColEventAllianceJoin  = new Vector4(0.4f, 0.7f, 1.0f, 1.0f);
        private static readonly Vector4 ColEventAllianceLeft  = new Vector4(0.6f, 0.6f, 0.85f, 1.0f);

        private static Vector4 GetEventColor(EnumCityLogEvent eventType) => eventType switch
        {
            EnumCityLogEvent.CityCreated      => ColEventCityCreated,
            EnumCityLogEvent.CitizenJoined    => ColEventJoined,
            EnumCityLogEvent.CitizenLeft      => ColEventLeft,
            EnumCityLogEvent.CitizenKicked    => ColEventKicked,
            EnumCityLogEvent.MayorChanged     => ColEventMayor,
            EnumCityLogEvent.ConflictDeclared => ColEventConflict,
            EnumCityLogEvent.FlagCaptured     => ColEventFlagCaptured,
            EnumCityLogEvent.AllianceJoined   => ColEventAllianceJoin,
            EnumCityLogEvent.AllianceLeft     => ColEventAllianceLeft,
            _                                 => ColEventDefault
        };
    }
}
