using System.Collections.Generic;
using claims.src.perms;

namespace claims.src.gui.playerGui.structures.cellElements
{
    /// <summary>One sub-plot of our city as the GUI shows it; built by subplots.SubPlotSync.ToCell.</summary>
    public class SubPlotCellElement
    {
        public string Guid { get; set; }
        public string Name { get; set; }
        /// <summary>Empty while the city holds it.</summary>
        public string OwnerName { get; set; } = "";
        public List<string> MembersNames { get; set; } = new List<string>();
        /// <summary>The plots group it is in; empty when none.</summary>
        public string GroupName { get; set; } = "";
        public PermsHandler PermsHandler { get; set; }
        /// <summary>-1 when not for sale.</summary>
        public double Price { get; set; } = -1;
        public double Tax { get; set; }
        /// <summary>An announced raise of the tax, -1 when none.</summary>
        public double PendingTax { get; set; } = -1;
        public long PendingTaxAt { get; set; }
        public bool PendingTaxAccepted { get; set; }
        public int Boxes { get; set; }
        public long Volume { get; set; }
        /// <summary>The plots it reaches into, "x:z" each.</summary>
        public string Chunks { get; set; } = "";
        /// <summary>Its boxes seen from above, minX, minZ, maxX, maxZ each with the max outside - for the city map.</summary>
        public List<int[]> Rects { get; set; } = new List<int[]>();
        /// <summary>The middle of its bounds, for "where is it".</summary>
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public bool IsForSale => Price >= 0;
        public bool HasPendingTax => PendingTax >= 0;
    }
}
