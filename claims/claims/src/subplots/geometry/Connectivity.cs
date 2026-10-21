using System;
using System.Collections.Generic;

namespace claims.src.subplots.geometry
{
    public static class Connectivity
    {
        /// <summary>
        /// Groups of nodes reachable from each other through neighbours. The neighbours function
        /// may return nodes outside the set; those are skipped.
        /// </summary>
        public static int CountPieces<T>(ICollection<T> nodes, Func<T, IEnumerable<T>> neighbours)
        {
            var seen = new HashSet<T>();
            int pieces = 0;
            foreach (T start in nodes)
            {
                if (!seen.Add(start)) continue;
                pieces++;
                var stack = new Stack<T>();
                stack.Push(start);
                while (stack.Count > 0)
                {
                    foreach (T next in neighbours(stack.Pop()))
                    {
                        if (nodes.Contains(next) && seen.Add(next))
                        {
                            stack.Push(next);
                        }
                    }
                }
            }
            return pieces;
        }
    }
}
