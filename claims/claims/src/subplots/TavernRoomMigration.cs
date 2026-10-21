using System;
using System.Collections.Generic;
using System.Linq;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using claims.src.perms;
using claims.src.perms.type;
using claims.src.subplots.geometry;

namespace claims.src.subplots
{
    /// <summary>
    /// Turns the rooms of the old tavern inner claims into sub-plots, once, on load. A room was
    /// "x,y,z:x,y,z:use,build,attack:members" joined by '#', and it only ever added rights: where the
    /// chunk refused, the room's flag decided, for everyone - the member list was never checked. So
    /// each group of the new sub-plot may do what the room allowed or what the chunk already let it.
    /// The far corner counted inside on X and Y and outside on Z, as the server tested it.
    /// </summary>
    public static class TavernRoomMigration
    {
        public static bool Run()
        {
            int made = 0, skipped = 0;
            foreach (Plot plot in claims.dataStorage.getClaimedPlots().Values.ToArray())
            {
                if (plot.PlotDesc is not PlotDescTavern tavern || string.IsNullOrEmpty(tavern.LegacyRooms)) continue;
                int number = 0;
                foreach (var room in ParseRooms(tavern.LegacyRooms))
                {
                    number++;
                    if (plot.hasCity() && TryMake(plot, room.Box, room.Flags, number)) made++;
                    else skipped++;
                }
                tavern.LegacyRooms = "";
                plot.saveToDatabase();
            }
            if (made + skipped > 0)
            {
                claims.sapi?.Logger.Notification("[claims] tavern rooms turned into sub-plots: {0}, skipped: {1}", made, skipped);
            }
            return true;
        }

        /// <summary>The rooms of one tavern; a room that does not read is left out.</summary>
        public static List<(BlockBox Box, bool[] Flags)> ParseRooms(string data)
        {
            var rooms = new List<(BlockBox, bool[])>();
            foreach (string room in (data ?? "").Split('#'))
            {
                string[] parts = room.Split(':');
                if (parts.Length < 3) continue;
                if (!TryVec(parts[0], out int[] a) || !TryVec(parts[1], out int[] b)) continue;
                string[] f = parts[2].Split(',');
                if (f.Length < 3) continue;
                var box = new BlockBox(Math.Min(a[0], b[0]), Math.Min(a[1], b[1]), Math.Min(a[2], b[2]),
                                       Math.Max(a[0], b[0]) + 1, Math.Max(a[1], b[1]) + 1, Math.Max(a[2], b[2]));
                if (box.IsEmpty) continue;
                rooms.Add((box, new[] { f[0] == "1", f[1] == "1", f[2] == "1" }));
            }
            return rooms;
        }

        private static bool TryVec(string text, out int[] v)
        {
            v = new int[3];
            string[] n = text.Split(',');
            if (n.Length != 3) return false;
            for (int i = 0; i < 3; i++)
            {
                if (!int.TryParse(n[i], out v[i])) return false;
            }
            return true;
        }

        private static bool TryMake(Plot plot, BlockBox box, bool[] flags, int number)
        {
            City city = plot.getCity();
            var shape = new BoxShape(box);
            // A room drawn past its tavern, or into another one's sub-plot, cannot be kept as it was.
            if (SubPlot.ChunksOf(shape, auxialiry.PlotPosition.plotSize).Count != 1
                || claims.dataStorage.AllSubPlots().Any(s => s.Shape.Intersects(shape)))
            {
                claims.sapi?.Logger.Warning("[claims] tavern room {0} at {1} skipped: {2} reaches outside it or overlaps a sub-plot",
                    number, plot.getPos(), box);
                return false;
            }

            string name = "room" + number;
            for (int n = 2; SubPlotEditor.FindByName(city, name) != null; n++) name = "room" + number + "_" + n;

            var subPlot = new SubPlot(name, Guid.NewGuid().ToString())
            {
                City = city,
                // A bought tavern stays its owner's inside the rooms too.
                Owner = plot.getPlotOwner(),
                Shape = shape
            };
            PermsHandler chunk = plot.getPermsHandler();
            foreach (PermGroup group in new[] { PermGroup.CITIZEN, PermGroup.STRANGER, PermGroup.ALLY, PermGroup.COMRADE })
            {
                bool[] rights = Rights(subPlot.PermsHandler, group);
                for (int t = 0; t < 3; t++) rights[t] = flags[t] || chunk.getPerm(group, (PermType)t);
            }
            claims.dataStorage.AddSubPlot(subPlot);
            subPlot.saveToDatabase(false);
            return true;
        }

        private static bool[] Rights(PermsHandler perms, PermGroup group) => group switch
        {
            PermGroup.CITIZEN => perms.CitizenPerms,
            PermGroup.STRANGER => perms.StrangerPerms,
            PermGroup.ALLY => perms.AlliancePerms,
            _ => perms.ComradePerms,
        };
    }
}
