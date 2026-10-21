using System;
using System.Collections.Generic;
using claims.src.part;
using claims.src.part.structure;
using claims.src.subplots.geometry;
using Vintagestory.API.MathTools;

namespace claims.src.subplots
{
    /// <summary>
    /// Whether a shape may be a sub-plot of a city, as a lang key or null when it may. Pure, so the
    /// commands, the GUI and the tests ask the same thing:
    /// - at most <c>maxBoxes</c> boxes, one connected piece, inside the world's height;
    /// - every chunk it reaches belongs to the city and is held by the city itself - not bought by a
    ///   citizen, not in a plots group, not for sale - so each block has one set of rules;
    /// - no other sub-plot of the city shares a block with it.
    /// </summary>
    public static class SubPlotRules
    {
        public static string Check(BoxShape shape, City city, Func<Vec2i, Plot> plotAt,
                                   IEnumerable<SubPlot> others, int maxBoxes, int plotSize, int worldHeight)
        {
            if (shape == null) return "claims:subplot_no_shape";
            if (shape.Boxes.Count > maxBoxes) return "claims:subplot_too_many_boxes";
            if (!shape.IsConnected()) return "claims:subplot_not_connected";
            if (shape.Bounds.MinY < 0 || shape.Bounds.MaxY > worldHeight) return "claims:subplot_outside_world";

            foreach (Vec2i chunk in SubPlot.ChunksOf(shape, plotSize))
            {
                // Only the chunks a box really reaches: the bounds of an L-shape cover corners it does not.
                if (shape.ClipTo(SubPlot.ChunkColumn(chunk, plotSize, worldHeight)).Count == 0) continue;
                string chunkProblem = ChunkProblem(plotAt(chunk), city);
                if (chunkProblem != null) return chunkProblem;
            }

            foreach (SubPlot other in others ?? Array.Empty<SubPlot>())
            {
                if (other?.Shape != null && other.Shape.Intersects(shape)) return "claims:subplot_overlaps";
            }
            return null;
        }

        /// <summary>Why a sub-plot cannot reach into this chunk; null when it can.</summary>
        public static string ChunkProblem(Plot plot, City city)
        {
            if (plot == null || !plot.hasCity() || !plot.getCity().Equals(city)) return "claims:subplot_outside_city";
            if (plot.hasPlotOwner()) return "claims:subplot_on_owned_plot";
            if (plot.hasCityPlotsGroup()) return "claims:subplot_on_plotsgroup";
            // On sale to citizens, the chunk is promised whole.
            if (plot.IsForSale) return "claims:subplot_on_plot_for_sale";
            return null;
        }
    }
}
