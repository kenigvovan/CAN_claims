using claims.src.auxialiry;
using System.Collections.Generic;
using System.Linq;

namespace claims.src.delayed
{
    public class LetterRegistry<T> where T : IExpirable
    {
        readonly HashSet<T> items = new();

        public bool Add(T item) => items.Add(item);
        public bool Remove(T item) => items.Remove(item);
        public void Clear() => items.Clear();
        public IEnumerable<T> All => items;

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

        public void ExpireOverdue()
        {
            long now = TimeFunctions.getEpochSeconds();
            foreach (var it in items.ToArray())
            {
                if (it.TimeStampExpire < now)
                {
                    items.Remove(it);
                    it.OnExpire?.Invoke();
                }
            }
        }
    }
}
