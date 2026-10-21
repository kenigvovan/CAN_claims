using System.Collections.Generic;
using System.Linq;
using claims.src.auxialiry;
using claims.src.auxialiry.innerclaims;
using claims.src.part;
using claims.src.part.structure;
using claims.src.perms.type;
using claims.src.subplots.geometry;
using Vintagestory.API.MathTools;

namespace claims.src.subplots
{
    /// <summary>
    /// What the client is told about sub-plots, so that it refuses a block it may not touch before
    /// the server has to: the parts of each sub-plot inside one chunk, with the flags already worked
    /// out for that one player. They travel in SavedPlotInfo.clientInnerClaims, the list the client's
    /// access check (DataStorage.getFlagValue) reads before the chunk's own flags - the tavern rooms'
    /// list, now for every chunk.
    /// </summary>
    public static class SubPlotSync
    {
        /// <summary>
        /// The pieces of every sub-plot in this chunk as <paramref name="viewer"/> may use them.
        /// Where the sub-plot is not the one to decide (an enemy at war) the chunk's own flags are
        /// sent for its piece, so the client still judges that block as the server will.
        /// </summary>
        public static List<ClientInnerClaim> PiecesFor(Plot plot, PlayerInfo viewer, bool creative,
                                                       bool chunkUse, bool chunkBuild, bool chunkAttack)
        {
            var pieces = new List<ClientInnerClaim>();
            Vec2i chunk = plot.getPos();
            var subPlots = claims.dataStorage.SubPlotsInChunk(chunk);
            if (subPlots.Count == 0) return pieces;

            BlockBox column = SubPlot.ChunkColumn(chunk, PlotPosition.plotSize, SubPlot.WorldHeight);
            // As OnBlockAction.SubPlotDecision: a raid opens the ground, the chunk decides.
            bool raid = !creative && VillageBlockRules.IsUnderRaidFor(viewer, plot);
            foreach (SubPlot subPlot in subPlots)
            {
                bool[] flags = creative
                    ? new[] { true, true, true }
                    : new[]
                    {
                        raid ? chunkUse : SubPlotAccess.Decide(viewer, subPlot, PermType.USE_PERM) ?? chunkUse,
                        raid ? chunkBuild : SubPlotAccess.Decide(viewer, subPlot, PermType.BUILD_AND_DESTROY_PERM) ?? chunkBuild,
                        SubPlotAccess.Decide(viewer, subPlot, PermType.ATTACK_ANIMALS_PERM) ?? chunkAttack,
                    };
                foreach (BlockBox part in subPlot.Shape.ClipTo(column))
                {
                    // Min and Max as ClientInnerClaim reads them: Max outside, like BlockBox.
                    pieces.Add(new ClientInnerClaim(new Vec3i(part.MinX, part.MinY, part.MinZ),
                                                    new Vec3i(part.MaxX, part.MaxY, part.MaxZ), flags)
                    {
                        Name = subPlot.GetPartName(),
                        Owner = subPlot.Owner?.GetPartName() ?? "",
                        Price = subPlot.IsForSale ? (int)subPlot.Price : -1,
                        Tax = subPlot.Tax
                    });
                }
            }
            return pieces;
        }

        /// <summary>
        /// Queues an update of every chunk the sub-plot reaches, for whoever watches them. Call it
        /// after any change to its shape, owner, members, price or rules - and with the old shape
        /// as well when the shape moved, so the chunks it left are told too.
        /// </summary>
        public static void MarkChanged(SubPlot subPlot)
        {
            MarkChanged(subPlot?.Shape);
            NotifyCity(subPlot?.City);
        }

        /// <summary>
        /// After the members or rules of a plots group changed: its sub-plots are judged differently
        /// for some players now, so their chunks are sent again.
        /// </summary>
        public static void MarkGroupChanged(CityPlotsGroup group)
        {
            if (group == null || claims.dataStorage == null) return;
            foreach (SubPlot subPlot in claims.dataStorage.AllSubPlots().Where(s => s.Group == group).ToArray())
            {
                MarkChanged(subPlot);
            }
        }

        /// <summary>A line in the city's log about one of its sub-plots, saved and sent to the log page.</summary>
        public static void Log(SubPlot subPlot, citylog.EnumCityLogEvent eventType, params string[] args)
        {
            City city = subPlot?.City;
            if (city == null) return;
            var withName = new string[args.Length + 1];
            withName[0] = subPlot.GetPartName();
            args.CopyTo(withName, 1);
            city.AddLogEntry(eventType, withName);
            city.saveToDatabase();
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid, gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_LOG);
        }

        /// <summary>Queues the city's sub-plot list for its citizens' GUI.</summary>
        public static void NotifyCity(City city)
        {
            if (city == null) return;
            UsefullPacketsSend.AddToQueueCityInfoUpdate(city.Guid,
                gui.playerGui.structures.EnumPlayerRelatedInfo.CITY_SUBPLOTS_ALL);
        }

        /// <summary>Every sub-plot of the city as the GUI shows it; empty, never null.</summary>
        public static List<gui.playerGui.structures.cellElements.SubPlotCellElement> Cells(City city)
        {
            var cells = new List<gui.playerGui.structures.cellElements.SubPlotCellElement>();
            if (city == null || claims.dataStorage == null) return cells;
            foreach (SubPlot subPlot in SubPlotEditor.OfCity(city).OrderBy(s => s.GetPartName()))
            {
                cells.Add(ToCell(subPlot));
            }
            return cells;
        }

        public static gui.playerGui.structures.cellElements.SubPlotCellElement ToCell(SubPlot subPlot)
        {
            BlockBox bounds = subPlot.Shape.Bounds;
            return new gui.playerGui.structures.cellElements.SubPlotCellElement
            {
                Guid = subPlot.Guid,
                Name = subPlot.GetPartName(),
                OwnerName = subPlot.Owner?.GetPartName() ?? "",
                MembersNames = subPlot.Members.Select(m => m.GetPartName()).OrderBy(n => n).ToList(),
                GroupName = subPlot.Group?.GetPartName() ?? "",
                PermsHandler = subPlot.PermsHandler,
                Price = subPlot.Price,
                Tax = subPlot.Tax,
                PendingTax = subPlot.PendingTax,
                PendingTaxAt = subPlot.PendingTaxAt,
                PendingTaxAccepted = subPlot.PendingTaxAccepted,
                Boxes = subPlot.Shape.Boxes.Count,
                Volume = subPlot.Shape.Volume,
                Chunks = string.Join(" ", subPlot.Chunks(PlotPosition.plotSize).Select(c => c.X + ":" + c.Y)),
                // Boxes stacked on each other look the same from above: one rectangle for them.
                Rects = subPlot.Shape.Boxes
                    .Select(b => (b.MinX, b.MinZ, b.MaxX, b.MaxZ)).Distinct()
                    .Select(r => new[] { r.MinX, r.MinZ, r.MaxX, r.MaxZ }).ToList(),
                X = (bounds.MinX + bounds.MaxX) / 2,
                Y = bounds.MinY,
                Z = (bounds.MinZ + bounds.MaxZ) / 2
            };
        }

        /// <summary>Refreshes the "next payment" the player sees, after their sub-plot tax bill changed.</summary>
        public static void NotifyPayment(PlayerInfo player)
        {
            if (player == null) return;
            UsefullPacketsSend.AddToQueuePlayerInfoUpdate(player.Guid,
                gui.playerGui.structures.EnumPlayerRelatedInfo.PLAYER_NEXT_PAYMENT);
        }

        public static void MarkChanged(BoxShape shape)
        {
            if (shape == null || claims.serverPlayerMovementListener == null) return;
            foreach (Vec2i chunk in SubPlot.ChunksOf(shape, PlotPosition.plotSize))
            {
                claims.serverPlayerMovementListener.markPlotToWasReUpdated(chunk);
            }
        }
    }
}
