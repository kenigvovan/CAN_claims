using System.Collections.Generic;
using ProtoBuf;

namespace claims.src.network.packets
{
    // Public bounty board, broadcast to all clients whenever a bounty changes.
    [ProtoContract]
    public class BountyBoardPacket
    {
        [ProtoMember(1)]
        public List<BountyBoardEntry> Entries = new List<BountyBoardEntry>();
    }

    [ProtoContract]
    public class BountyBoardEntry
    {
        [ProtoMember(1)]
        public string Name;
        [ProtoMember(2)]
        public long Amount;
    }
}
