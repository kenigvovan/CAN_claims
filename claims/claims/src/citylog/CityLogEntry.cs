using System.Collections.Generic;

namespace claims.src.citylog
{
    public class CityLogEntry
    {
        public long Timestamp { get; set; }
        public EnumCityLogEvent EventType { get; set; }
        public List<string> Args { get; set; } = new List<string>();

        public CityLogEntry() { }

        public CityLogEntry(long timestamp, EnumCityLogEvent eventType, params string[] args)
        {
            Timestamp = timestamp;
            EventType = eventType;
            Args = new List<string>(args);
        }
    }
}
