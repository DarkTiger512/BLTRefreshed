using System;
using System.Collections.Generic;

// Compatibility metadata retained for settings reflection without loading WPF/Xceed.
namespace Xceed.Wpf.Toolkit.PropertyGrid.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PropertyOrderAttribute : Attribute { public PropertyOrderAttribute(int order) { Order = order; } public int Order { get; } }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ItemsSourceAttribute : Attribute { public ItemsSourceAttribute(Type type) { Type = type; } public Type Type { get; } }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ExpandableObjectAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class InstanceNameAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class CategoryOrderAttribute : Attribute { public CategoryOrderAttribute(string category, int order) { Category = category; Order = order; } public string Category { get; } public int Order { get; } }
    public interface IItemsSource { ItemCollection GetValues(); }
    public sealed class ItemCollection : List<Item> { public void Add(object value, string displayName) => Add(new Item(value, displayName)); public void Add(object value) => Add(new Item(value, value?.ToString())); }
    public sealed class Item { public Item(object value, string displayName) { Value = value; DisplayName = displayName; } public object Value { get; } public string DisplayName { get; } }
}

namespace BannerlordTwitch.UI
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class UIRangeAttribute : Attribute
    {
        public UIRangeAttribute(double min, double max, double step = 1) { Min = min; Max = max; Step = step; }
        public double Min { get; }
        public double Max { get; }
        public double Step { get; }
    }
    public sealed class SliderFloatEditor { }
    public sealed class RangeFloatEditor { }
    public sealed class RangeIntEditor { }
    public sealed class DefaultCollectionEditor { }
    public sealed class DerivedClassCollectionEditor { }
    public sealed class DerivedClassCollectionEditor<T> { }
}
