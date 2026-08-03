namespace claims.src.delayed
{
    public class LetterRegistry<T> : ExpiringList<T> where T : IExpirable
    {
        public override bool Add(T item)
        {
            if (items.Contains(item)) return false;
            return base.Add(item);
        }

        public bool TryGetByGuid(string guid, out T item)
        {
            foreach (var it in items)
            {
                if (it.Guid == guid) { item = it; return true; }
            }
            item = default;
            return false;
        }

        public bool GuidIsFree(string guid) => !TryGetByGuid(guid, out _);

        public void ExpireOverdue() => ExpireOverdue(it => it.TimeStampExpire, it => it.OnExpire?.Invoke());
    }
}
