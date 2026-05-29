using Vintagestory.API.Datastructures;

namespace claims.src.economy
{
    public sealed class CoinDisplayInfo
    {
        public decimal Value { get; }
        public string CollectibleCode { get; }
        public TreeAttribute Attributes { get; }

        public CoinDisplayInfo(decimal value, string collectibleCode, TreeAttribute attributes)
        {
            Value = value;
            CollectibleCode = collectibleCode;
            Attributes = attributes;
        }
    }
}
