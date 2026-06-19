using System.IO;
using ProtoBuf;
using Vintagestory.API.Datastructures;

namespace claims.src.economy
{
    // Network/serialization-friendly form of CoinDisplayInfo. Carries the full
    // coin denomination list (value + item code + optional attributes) so the
    // client can render every configured coin, including several coins that
    // share the same value but differ by attributes (those would collapse in an
    // OrderedDictionary keyed by value).
    [ProtoContract]
    public class CoinDenominationData
    {
        [ProtoMember(1)]
        public decimal Value;
        [ProtoMember(2)]
        public string CollectibleCode;
        [ProtoMember(3)]
        public byte[] Attributes;

        public CoinDenominationData() { }

        public static CoinDenominationData FromDisplayInfo(CoinDisplayInfo info)
        {
            return new CoinDenominationData
            {
                Value = info.Value,
                CollectibleCode = info.CollectibleCode,
                Attributes = SerializeAttributes(info.Attributes)
            };
        }

        // Rebuilds the TreeAttribute from the serialized bytes, or null when the
        // coin has no attributes.
        public TreeAttribute ToTreeAttribute()
        {
            if (Attributes == null || Attributes.Length == 0) return null;
            var tree = new TreeAttribute();
            using var ms = new MemoryStream(Attributes);
            using var reader = new BinaryReader(ms);
            tree.FromBytes(reader);
            return tree;
        }

        private static byte[] SerializeAttributes(TreeAttribute attributes)
        {
            if (attributes == null) return null;
            using var ms = new MemoryStream();
            using (var writer = new BinaryWriter(ms))
            {
                attributes.ToBytes(writer);
            }
            return ms.ToArray();
        }
    }
}
