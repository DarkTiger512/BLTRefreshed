using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace BLTAdoptAHero.Util
{
    // Preserve the engine's exact XP dictionaries and property owners, including DLC skills.
    // Fail before mutation if a supported engine field has changed.
    internal sealed class PrestigeHeroSnapshot
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly List<Action> restore = new();
        private readonly List<Action> traits = new();
        public PrestigeHeroSnapshot(Hero hero)
        {
            foreach (string name in new[] { "_heroPerks", "_heroSkills", "_characterAttributes", "Level" })
            {
                var field = typeof(Hero).GetField(name, Fields) ?? throw new NotSupportedException("Missing hero field: " + name);
                CaptureField(hero, field);
            }
            foreach (var field in hero.HeroDeveloper.GetType().GetFields(Fields))
                if (!field.Name.Contains("Hero")) CaptureField(hero.HeroDeveloper, field);
            int start = restore.Count;
            CaptureField(hero, typeof(Hero).GetField("_heroTraits", Fields) ?? throw new NotSupportedException("Missing hero traits"));
            traits.AddRange(restore.Skip(start));
            var battle = hero.BattleEquipment.Clone();
            var civilian = hero.CivilianEquipment.Clone();
            var nameBefore = hero.Name;
            var firstName = hero.FirstName;
            int health = hero.HitPoints;
            restore.Add(() => { hero.BattleEquipment.FillFrom(battle); hero.CivilianEquipment.FillFrom(civilian); hero.SetName(nameBefore, firstName); hero.HitPoints = health; });
        }
        private void CaptureField(object owner, FieldInfo field)
        {
            object value = field.GetValue(owner);
            restore.Add(() => field.SetValue(owner, value));
            if (value is IDictionary dictionary)
            {
                var entries = new List<DictionaryEntry>();
                var iterator = dictionary.GetEnumerator();
                while (iterator.MoveNext()) entries.Add(iterator.Entry);
                restore.Add(() => { dictionary.Clear(); foreach (var entry in entries) dictionary.Add(entry.Key, entry.Value); });
            }
            else if (value != null && value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(PropertyOwner<>))
                foreach (var child in value.GetType().GetFields(Fields)) CaptureField(value, child);
        }
        public void Restore() { foreach (var action in restore) action(); }
        public void RestoreTraits() { foreach (var action in traits) action(); }
    }
}
