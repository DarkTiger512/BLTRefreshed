using System;
using System.Collections.Generic;
using System.Linq;

namespace BLTAdoptAHero.Util
{
    internal enum AmmoReportKind
    {
        Available,
        Depleted,
        MissingAmmo,
        NoRangedWeapon
    }

    internal sealed class AmmoStackSnapshot
    {
        public int Slot { get; set; }
        public string Name { get; set; }
        public int Current { get; set; }
        public int Maximum { get; set; }
    }

    internal sealed class AmmoReportResult
    {
        public AmmoReportKind Kind { get; set; }
        public IReadOnlyList<AmmoStackSnapshot> Stacks { get; set; }
        public int TotalCurrent { get; set; }
        public int TotalMaximum { get; set; }

        public string Details => string.Join(", ", Stacks.Select(stack =>
            $"{stack.Name}: {stack.Current}/{stack.Maximum}"));
    }

    internal static class AmmoReport
    {
        public static AmmoReportResult Create(IEnumerable<AmmoStackSnapshot> stacks, bool hasRangedWeapon)
        {
            var ordered = (stacks ?? Enumerable.Empty<AmmoStackSnapshot>())
                .Where(stack => stack != null)
                .OrderBy(stack => stack.Slot)
                .ThenBy(stack => stack.Name ?? string.Empty, StringComparer.Ordinal)
                .ToList();

            if (ordered.Count == 0)
            {
                return new AmmoReportResult
                {
                    Kind = hasRangedWeapon ? AmmoReportKind.MissingAmmo : AmmoReportKind.NoRangedWeapon,
                    Stacks = ordered
                };
            }

            int current = ordered.Sum(stack => Math.Max(0, stack.Current));
            int maximum = ordered.Sum(stack => Math.Max(0, stack.Maximum));
            return new AmmoReportResult
            {
                Kind = current > 0 ? AmmoReportKind.Available : AmmoReportKind.Depleted,
                Stacks = ordered,
                TotalCurrent = current,
                TotalMaximum = maximum
            };
        }
    }
}
