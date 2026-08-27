using System;
using System.Collections.Generic;
using System.Linq;

namespace BLTAdoptAHero.Util
{
    public static class CycleSafeGraph
    {
        public static IReadOnlyList<T> FindTerminals<T>(
            T root,
            Func<T, IEnumerable<T>> getChildren,
            IEqualityComparer<T> comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;
            return Find(root, getChildren, new HashSet<T>(comparer), comparer)
                .Distinct(comparer)
                .ToList();
        }

        private static IEnumerable<T> Find<T>(
            T node,
            Func<T, IEnumerable<T>> getChildren,
            HashSet<T> path,
            IEqualityComparer<T> comparer)
        {
            if (node == null || !path.Add(node)) return Array.Empty<T>();

            var children = (getChildren(node) ?? Array.Empty<T>())
                .Where(child => child != null)
                .ToList();
            if (children.Count == 0) return new[] { node };

            var terminals = new List<T>();
            foreach (var child in children)
            {
                terminals.AddRange(Find(child, getChildren, new HashSet<T>(path, comparer), comparer));
            }

            return terminals.Count == 0 ? new[] { node } : terminals;
        }
    }
}
