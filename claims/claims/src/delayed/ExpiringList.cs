using claims.src.auxialiry;
using System;
using System.Collections.Generic;

namespace claims.src.delayed
{
    public class ExpiringList<T>
    {
        readonly List<T> items = new();

        public void Add(T item) => items.Add(item);
        public bool Remove(T item) => items.Remove(item);

        // Safe snapshot for caller-side iteration (avoids modification-during-foreach).
        public T[] Snapshot() => items.ToArray();

        public void ExpireOverdue(Func<T, long> getExpiry, Action<T> onExpire = null)
        {
            long now = TimeFunctions.getEpochSeconds();
            foreach (var it in items.ToArray())
            {
                if (getExpiry(it) < now)
                {
                    items.Remove(it);
                    onExpire?.Invoke(it);
                }
            }
        }
    }
}
