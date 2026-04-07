using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Annotations;
using BLTAdoptAHero.Achievements;
using BLTAdoptAHero.Powers;
using Microsoft.AspNet.SignalR;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;

namespace BLTAdoptAHero.UI
{
    [UsedImplicitly]
    public class InventoryHub : Hub
    {
        // ── DTOs ─────────────────────────────────────────────

        [UsedImplicitly]
        public class EquipmentItemDto
        {
            [UsedImplicitly] public string name;
            [UsedImplicitly] public string icon;
            [UsedImplicitly] public string type;
            [UsedImplicitly] public int tier;
        }

        [UsedImplicitly]
        public class RetinueUnitDto
        {
            [UsedImplicitly] public string name;
            [UsedImplicitly] public int count;
            [UsedImplicitly] public int tier;
        }

        [UsedImplicitly]
        public class StatDto
        {
            [UsedImplicitly] public string label;
            [UsedImplicitly] public int total;
            [UsedImplicitly] public int classSpecific;
        }

        [UsedImplicitly]
        public class SkillDto
        {
            [UsedImplicitly] public string name;
            [UsedImplicitly] public int value;
            [UsedImplicitly] public int focus;
        }

        [UsedImplicitly]
        public class HeroInventoryPayload
        {
            [UsedImplicitly] public bool found;
            [UsedImplicitly] public string errorMessage;

            // Identity
            [UsedImplicitly] public string heroName;
            [UsedImplicitly] public string className;
            [UsedImplicitly] public string clanName;
            [UsedImplicitly] public string cultureName;
            [UsedImplicitly] public int age;
            [UsedImplicitly] public int hp;
            [UsedImplicitly] public int maxHp;
            [UsedImplicitly] public string location;
            [UsedImplicitly] public int level;

            // Economy
            [UsedImplicitly] public int gold;
            [UsedImplicitly] public int equipmentTier;

            // Equipment
            [UsedImplicitly] public List<EquipmentItemDto> battleEquipment;
            [UsedImplicitly] public List<EquipmentItemDto> civilianEquipment;
            [UsedImplicitly] public List<EquipmentItemDto> customItems;

            // Retinue
            [UsedImplicitly] public List<RetinueUnitDto> retinue;

            // Skills & attributes
            [UsedImplicitly] public List<SkillDto> skills;
            [UsedImplicitly] public Dictionary<string, int> attributes;

            // Stats
            [UsedImplicitly] public List<StatDto> trackedStats;

            // Powers
            [UsedImplicitly] public List<string> activePowers;
            [UsedImplicitly] public List<string> passivePowers;

            // Achievements
            [UsedImplicitly] public List<string> achievements;
        }

        // ── Hub method ───────────────────────────────────────

        /// <summary>
        /// Returns full hero inventory data for a given Twitch username.
        /// </summary>
        [UsedImplicitly]
        public HeroInventoryPayload GetInventory(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return new HeroInventoryPayload { found = false, errorMessage = "No username provided." };

            userName = userName.Trim();

            return MainThreadSync.Run(() =>
            {
                var behavior = BLTAdoptAHeroCampaignBehavior.Current;
                if (behavior == null)
                    return new HeroInventoryPayload
                        { found = false, errorMessage = "Game campaign not active." };

                var hero = behavior.GetAdoptedHero(userName);
                if (hero == null)
                    return new HeroInventoryPayload
                        { found = false, errorMessage = "No adopted hero found for " + userName + "." };

                var heroClass = hero.GetClass();

                // Equipment helper
                List<EquipmentItemDto> MapEquipment(Equipment eq)
                {
                    var list = new List<EquipmentItemDto>();
                    foreach (var (element, _) in eq.YieldFilledEquipmentSlots())
                    {
                        list.Add(new EquipmentItemDto
                        {
                            name = element.GetModifiedItemName().ToString(),
                            icon = GetItemIcon(element.Item),
                            type = element.Item?.ItemType.ToString() ?? "Unknown",
                            tier = (int)(element.Item?.Tier ?? 0),
                        });
                    }
                    return list;
                }

                // Custom items
                var customItems = behavior.GetCustomItems(hero)
                    .Select(e => new EquipmentItemDto
                    {
                        name = e.GetModifiedItemName().ToString(),
                        icon = GetItemIcon(e.Item),
                        type = e.Item?.ItemType.ToString() ?? "Unknown",
                        tier = (int)(e.Item?.Tier ?? 0),
                    }).ToList();

                // Retinue
                var retinue = behavior.GetRetinue(hero)
                    .GroupBy(r => r)
                    .OrderBy(g => g.Key.Tier)
                    .Select(g => new RetinueUnitDto
                    {
                        name = g.Key.Name.ToString(),
                        count = g.Count(),
                        tier = g.Key.Tier,
                    }).ToList();

                // Skills
                var skills = CampaignHelpers.AllSkillObjects
                    .Select(s => new SkillDto
                    {
                        name = s.Name.ToString(),
                        value = hero.GetSkillValue(s),
                        focus = hero.HeroDeveloper.GetFocus(s),
                    })
                    .Where(s => s.value > 0)
                    .OrderByDescending(s => s.value)
                    .ToList();

                // Attributes
                var attributes = new Dictionary<string, int>();
                foreach (var attr in CampaignHelpers.AllAttributes)
                {
                    attributes[CampaignHelpers.GetShortAttributeName(attr)] = hero.GetAttributeValue(attr);
                }

                // Tracked stats
                var statIds = new[]
                {
                    ("Kills", AchievementStatsData.Statistic.TotalKills),
                    ("Deaths", AchievementStatsData.Statistic.TotalDeaths),
                    ("Viewer Kills", AchievementStatsData.Statistic.TotalViewerKills),
                    ("Streamer Kills", AchievementStatsData.Statistic.TotalStreamerKills),
                    ("Battles", AchievementStatsData.Statistic.Battles),
                    ("Summons", AchievementStatsData.Statistic.Summons),
                    ("Attacks", AchievementStatsData.Statistic.Attacks),
                    ("Tournament Wins", AchievementStatsData.Statistic.TotalTournamentFinalWins),
                    ("Tournament Round Wins", AchievementStatsData.Statistic.TotalTournamentRoundWins),
                };
                var trackedStats = statIds.Select(s => new StatDto
                {
                    label = s.Item1,
                    total = behavior.GetAchievementTotalStat(hero, s.Item2),
                    classSpecific = behavior.GetAchievementClassStat(hero, s.Item2),
                }).ToList();

                // Powers
                var activePowers = new List<string>();
                var passivePowers = new List<string>();
                if (heroClass != null)
                {
                    activePowers = heroClass.ActivePower
                        .GetUnlockedPowers(hero)
                        .OfType<HeroPowerDefBase>()
                        .Select(p => p.Name.ToString())
                        .ToList();
                    passivePowers = heroClass.PassivePower
                        .GetUnlockedPowers(hero)
                        .OfType<HeroPowerDefBase>()
                        .Select(p => p.Name.ToString())
                        .ToList();
                }

                // Achievements
                var achievements = behavior.GetAchievements(hero)
                    .Select(a => a.Name.ToString())
                    .ToList();

                return new HeroInventoryPayload
                {
                    found = true,
                    heroName = hero.Name.ToString(),
                    className = heroClass?.Name.ToString() ?? "No Class",
                    clanName = hero.Clan?.Name.ToString(),
                    cultureName = hero.Culture?.Name.ToString(),
                    age = (int)Math.Truncate((double)hero.Age),
                    hp = hero.HitPoints,
                    maxHp = hero.CharacterObject.MaxHitPoints(),
                    location = hero.LastKnownClosestSettlement?.Name.ToString(),
                    level = hero.Level,
                    gold = behavior.GetHeroGold(hero),
                    equipmentTier = behavior.GetEquipmentTier(hero) + 1,
                    battleEquipment = MapEquipment(hero.BattleEquipment),
                    civilianEquipment = MapEquipment(hero.CivilianEquipment),
                    customItems = customItems,
                    retinue = retinue,
                    skills = skills,
                    attributes = attributes,
                    trackedStats = trackedStats,
                    activePowers = activePowers,
                    passivePowers = passivePowers,
                    achievements = achievements,
                };
            });
        }

        private static string GetItemIcon(ItemObject item)
        {
            if (item == null) return "gear";
            switch (item.ItemType)
            {
                case ItemObject.ItemTypeEnum.OneHandedWeapon:
                case ItemObject.ItemTypeEnum.TwoHandedWeapon:
                case ItemObject.ItemTypeEnum.Polearm:
                    return "sword";
                case ItemObject.ItemTypeEnum.Bow:
                case ItemObject.ItemTypeEnum.Crossbow:
                case ItemObject.ItemTypeEnum.Thrown:
                    return "ranged";
                case ItemObject.ItemTypeEnum.Shield:
                    return "shield";
                case ItemObject.ItemTypeEnum.Arrows:
                case ItemObject.ItemTypeEnum.Bolts:
                    return "ammo";
                case ItemObject.ItemTypeEnum.Horse:
                case ItemObject.ItemTypeEnum.HorseHarness:
                    return "horse";
                case ItemObject.ItemTypeEnum.HeadArmor:
                    return "helmet";
                case ItemObject.ItemTypeEnum.BodyArmor:
                case ItemObject.ItemTypeEnum.ChestArmor:
                    return "armor";
                case ItemObject.ItemTypeEnum.LegArmor:
                    return "boots";
                case ItemObject.ItemTypeEnum.HandArmor:
                    return "gloves";
                case ItemObject.ItemTypeEnum.Cape:
                    return "cape";
                case ItemObject.ItemTypeEnum.Banner:
                    return "banner";
                default:
                    return "gear";
            }
        }
    }
}
