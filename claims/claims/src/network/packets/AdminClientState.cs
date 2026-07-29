using System.Collections.Generic;

namespace claims.src.network.packets
{
    /// <summary>
    /// World and per-city flags the server sends to admins, kept here rather than inside a GUI so
    /// both the native dialog and the ImGui one read the same copy - and so it survives either of
    /// them being removed.
    /// </summary>
    public static class AdminClientState
    {
        public static Dictionary<string, AdminCityFlagsItem> CityFlags { get; } = new Dictionary<string, AdminCityFlagsItem>();

        /// <summary>Null until the first ADMIN_REQUEST_CITY_FLAGS reply arrives.</summary>
        public static AdminWorldFlags World { get; set; }

        public static void Accept(AdminDataPacket data)
        {
            if (data == null) return;

            CityFlags.Clear();
            if (data.Cities != null)
            {
                foreach (var item in data.Cities) CityFlags[item.Name] = item;
            }
            World = data.World;
        }
    }
}
