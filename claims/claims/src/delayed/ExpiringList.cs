using claims.src.auxialiry;
using System;
using System.Collections.Generic;

namespace claims.src.delayed
{
    public class ExpiringList<T>
    {
        protected readonly List<T> items = new();

        public virtual bool Add(T item)
        {
            items.Add(item);
            return true;
        }
        public bool Remove(T item) => items.Remove(item);
        public void Clear() => items.Clear();
        public IEnumerable<T> All => items;

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
                    // A throwing callback must not abort the rest of the sweep or escape into the timer
                    try { onExpire?.Invoke(it); }
                    catch (Exception ex) { claims.sapi?.Logger.Error("[claims] ExpiringList onExpire callback failed: " + ex); }
                }
            }
        }
    }
}
