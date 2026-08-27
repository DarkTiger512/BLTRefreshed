using System;
using System.Collections.Generic;
using System.Linq;

namespace BLTAdoptAHero.Util
{
    public static class SmartTroopPolicy
    {
        public readonly struct Selection<T>
        {
            public Selection(T value, int fallbackTier)
            {
                Value = value;
                FallbackTier = fallbackTier;
            }

            public T Value { get; }
            public int FallbackTier { get; }
        }

        public static Selection<T> Select<T>(
            IEnumerable<T> candidates,
            Func<T, bool> sameCulture,
            Func<T, bool> classCompatible,
            IEnumerable<T> safeFallback,
            Func<T, string> stableKey)
        {
            var available = candidates.Where(value => value != null).Distinct().ToList();
            T First(IEnumerable<T> source) => source.OrderBy(stableKey, StringComparer.Ordinal).FirstOrDefault();

            var selected = First(available.Where(value => sameCulture(value) && classCompatible(value)));
            if (selected != null) return new Selection<T>(selected, 1);

            selected = First(available.Where(classCompatible));
            if (selected != null) return new Selection<T>(selected, 2);

            selected = First(available.Where(sameCulture));
            if (selected != null) return new Selection<T>(selected, 3);

            selected = First(safeFallback.Where(value => value != null));
            return new Selection<T>(selected, 4);
        }
    }
}
