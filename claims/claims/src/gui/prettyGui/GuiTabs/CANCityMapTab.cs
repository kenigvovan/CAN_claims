using claims.src.gui.playerGui.structures.cellElements;
using claims.src.part.structure;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.prettyGui.GuiTabs
{
    public class CANCityMapTab : CANGuiTab
    {
        private static readonly Dictionary<PlotType, Vector4> PlotColors = new()
        {
            { PlotType.DEFAULT,         new Vector4(0.35f, 0.60f, 0.35f, 1.0f) },
            { PlotType.MAIN_CITY_PLOT,  new Vector4(1.0f,  0.85f, 0.2f,  1.0f) },
            { PlotType.TAVERN,          new Vector4(0.8f,  0.5f,  0.2f,  1.0f) },
            { PlotType.PRISON,          new Vector4(0.55f, 0.55f, 0.55f, 1.0f) },
            { PlotType.EMBASSY,         new Vector4(0.3f,  0.6f,  0.9f,  1.0f) },
            { PlotType.SUMMON,          new Vector4(0.7f,  0.3f,  0.9f,  1.0f) },
            { PlotType.FARM,            new Vector4(0.6f,  0.85f, 0.3f,  1.0f) },
            { PlotType.CAMP,            new Vector4(0.75f, 0.55f, 0.35f, 1.0f) },
            { PlotType.TOURNAMENT,      new Vector4(0.9f,  0.3f,  0.3f,  1.0f) },
            { PlotType.TEMPLE,          new Vector4(0.9f,  0.85f, 0.6f,  1.0f) },
        };

        public CANCityMapTab(ICoreClientAPI capi, IconHandler iconHandler)
        {
            this.capi = capi;
            this.iconHandler = iconHandler;
        }

        private static List<(int coord, bool isGap)> BuildAxis(SortedSet<int> values)
        {
            var axis = new List<(int coord, bool isGap)>();
            int prev = int.MinValue;
            foreach (int v in values)
            {
                if (prev != int.MinValue)
                {
                    int diff = v - prev;
                    if (diff > 5)
                        axis.Add((-1, true));
                    else
                        for (int i = prev + 1; i < v; i++)
                            axis.Add((i, false));
                }
                axis.Add((v, false));
                prev = v;
            }
            return axis;
        }

        public override void DrawTab()
        {
            var clientInfo = claims.clientDataStorage?.clientPlayerInfo;
            if (clientInfo?.CityInfo == null)
            {
                ImGui.TextDisabled(Lang.Get("claims:you_dont_have_city"));
                return;
            }

            if (ImGui.Button(Lang.Get("claims:gui-back")))
            {
                GuiSys.selectedTab = EnumSelectedTab.CITY;
            }

            ImGui.Separator();
            ImGui.Spacing();

            Vector4 headerColor = new Vector4(1.0f, 0.85f, 0.3f, 1.0f);
            ImGui.PushStyleColor(ImGuiCol.Text, headerColor);
            ImGui.Text(Lang.Get("claims:gui-city-map-title"));
            ImGui.PopStyleColor();

            ImGui.Spacing();

            var plots = clientInfo.CityInfo.PlotsMap;
            if (plots == null || plots.Count == 0)
            {
                ImGui.TextDisabled(Lang.Get("claims:gui-city-map-empty"));
                return;
            }

            const float cellSize = 14f;
            const float gap = 1f;
            const float step = cellSize + gap;

            var typeLookup = new Dictionary<(int, int), PlotType>();
            var xSet = new SortedSet<int>();
            var zSet = new SortedSet<int>();
            foreach (var p in plots)
            {
                typeLookup[(p.X, p.Z)] = p.Type;
                xSet.Add(p.X);
                zSet.Add(p.Z);
            }

            var colAxis = BuildAxis(xSet);
            var rowAxis = BuildAxis(zSet);
            int vcols = colAxis.Count;
            int vrows = rowAxis.Count;

            float mapWidth  = vcols * step;
            float mapHeight = vrows * step;

            float scrollHeight = Math.Min(mapHeight + 20, 520f);
            ImGui.BeginChild("citymapscroll", new Vector2(0, scrollHeight), false,
                ImGuiWindowFlags.HorizontalScrollbar);

            var drawList = ImGui.GetWindowDrawList();
            Vector2 origin = ImGui.GetCursorScreenPos();

            float scrollX = ImGui.GetScrollX();
            float scrollY = ImGui.GetScrollY();
            float viewW   = ImGui.GetWindowWidth();
            float viewH   = ImGui.GetWindowHeight();

            int firstCol = Math.Max(0, (int)(scrollX / step));
            int lastCol  = Math.Min(vcols - 1, (int)((scrollX + viewW) / step) + 1);
            int firstRow = Math.Max(0, (int)(scrollY / step));
            int lastRow  = Math.Min(vrows - 1, (int)((scrollY + viewH) / step) + 1);

            uint emptyColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.25f, 0.25f, 0.25f, 0.4f));
            uint gapColor   = ImGui.ColorConvertFloat4ToU32(new Vector4(0.12f, 0.12f, 0.25f, 0.7f));

            for (int vc = firstCol; vc <= lastCol; vc++)
            {
                for (int vr = firstRow; vr <= lastRow; vr++)
                {
                    float sx = origin.X + vc * step;
                    float sy = origin.Y + vr * step;
                    var p1 = new Vector2(sx, sy);
                    var p2 = new Vector2(sx + cellSize, sy + cellSize);

                    if (colAxis[vc].isGap || rowAxis[vr].isGap)
                    {
                        drawList.AddRectFilled(p1, p2, gapColor);
                    }
                    else if (typeLookup.TryGetValue((colAxis[vc].coord, rowAxis[vr].coord), out PlotType type))
                    {
                        Vector4 c = PlotColors.TryGetValue(type, out var col4) ? col4 : new Vector4(0.5f, 0.5f, 0.5f, 1f);
                        drawList.AddRectFilled(p1, p2, ImGui.ColorConvertFloat4ToU32(c));
                    }
                    else
                    {
                        drawList.AddRect(p1, p2, emptyColor);
                    }
                }
            }

            ImGui.Dummy(new Vector2(mapWidth, mapHeight));

            Vector2 mousePos = ImGui.GetMousePos();
            float relX = mousePos.X - origin.X;
            float relY = mousePos.Y - origin.Y;
            if (relX >= 0 && relY >= 0 && relX < mapWidth && relY < mapHeight)
            {
                int hovVC = Math.Min((int)(relX / step), vcols - 1);
                int hovVR = Math.Min((int)(relY / step), vrows - 1);
                if (!colAxis[hovVC].isGap && !rowAxis[hovVR].isGap &&
                    typeLookup.TryGetValue((colAxis[hovVC].coord, rowAxis[hovVR].coord), out PlotType hovType))
                {
                    string typeName = Lang.Get($"claims:gui-plot-type-{hovType.ToString().ToLowerInvariant()}");
                    ImGui.SetTooltip($"{typeName} ({colAxis[hovVC].coord}, {rowAxis[hovVR].coord})");
                }
            }

            ImGui.EndChild();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.Text(Lang.Get("claims:gui-city-map-legend"));
            ImGui.Spacing();

            var typeCounts = new Dictionary<PlotType, int>();
            foreach (var p in plots)
            {
                typeCounts.TryGetValue(p.Type, out int cnt);
                typeCounts[p.Type] = cnt + 1;
            }

            int drawn = 0;
            foreach (var kv in PlotColors)
            {
                if (!typeCounts.TryGetValue(kv.Key, out int count)) continue;

                if (drawn > 0 && drawn % 4 != 0) ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, kv.Value);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, kv.Value);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, kv.Value);
                ImGui.Button($"##legend_{kv.Key}", new Vector2(14, 14));
                ImGui.PopStyleColor(3);
                ImGui.SameLine();
                ImGui.Text($"{Lang.Get($"claims:gui-plot-type-{kv.Key.ToString().ToLowerInvariant()}")} ({count})");
                drawn++;
            }

            var gapLegendColor = new Vector4(0.12f, 0.12f, 0.25f, 0.7f);
            if (drawn > 0 && drawn % 4 != 0) ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button, gapLegendColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, gapLegendColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, gapLegendColor);
            ImGui.Button("##legend_gap", new Vector2(14, 14));
            ImGui.PopStyleColor(3);
            ImGui.SameLine();
            ImGui.Text(Lang.Get("claims:gui-city-map-gap"));
        }
    }
}
