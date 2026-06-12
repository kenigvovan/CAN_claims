namespace claims.src.network.packets
{
    public class AdminCityFlagsItem
    {
        public string Guid { get; set; }
        public string Name { get; set; }
        public bool Pvp { get; set; }
        public bool Fire { get; set; }
        public bool Blast { get; set; }
        public bool Technical { get; set; }
        public bool Open { get; set; }
        public int CitizenCount { get; set; }
        public int PlotCount { get; set; }
        public double Balance { get; set; }
        public bool HasBalance { get; set; }
    }
}
