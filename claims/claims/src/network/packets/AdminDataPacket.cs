using System.Collections.Generic;

namespace claims.src.network.packets
{
    public class AdminDataPacket
    {
        public List<AdminCityFlagsItem> Cities { get; set; }
        public AdminWorldFlags World { get; set; }
    }
}
