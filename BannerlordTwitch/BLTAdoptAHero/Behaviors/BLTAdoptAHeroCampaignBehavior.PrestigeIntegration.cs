using System.Linq;
using BannerlordTwitch.Integration;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Util;
using TaleWorlds.CampaignSystem;

namespace BLTAdoptAHero
{
    public partial class BLTAdoptAHeroCampaignBehavior
    {
        public IntegrationPrestigeSnapshot GetPrestigeSnapshot(Hero hero)
        {
            var c = PrestigeConfig;
            var p = GetPrestige(hero);
            string blocked = PrestigeBlock(hero);
            string[] names = { "{=BLTPrestigeMight}Might".Translate(), "{=BLTPrestigeResilience}Resilience".Translate(),
                "{=BLTPrestigeVitality}Vitality".Translate(), "{=BLTPrestigeFortune}Fortune".Translate(), "{=BLTPrestigeInsight}Insight".Translate() };
            string[] descriptions = {
                "{=BLTPrestigeMightEffect}+{VALUE}% outgoing damage per rank".Translate(("VALUE", c.MightPerRank * 100)),
                "{=BLTPrestigeResilienceEffect}{VALUE}% incoming damage reduction per rank".Translate(("VALUE", c.ResiliencePerRank * 100)),
                "{=BLTPrestigeVitalityEffect}+{VALUE} maximum battle HP per rank".Translate(("VALUE", c.VitalityPerRank)),
                "{=BLTPrestigeFortuneEffect}+{VALUE}% battle gold per rank".Translate(("VALUE", c.FortunePerRank * 100)),
                "{=BLTPrestigeInsightEffect}+{VALUE}% BLT skill XP per rank".Translate(("VALUE", c.InsightPerRank * 100)) };
            return new IntegrationPrestigeSnapshot {
                Count = p.Count, Maximum = PrestigePolicy.Maximum(c), RunKills = GetPrestigeKills(hero),
                RequiredKills = PrestigePolicy.RequiredKills(c, p.Count), RequiredGold = PrestigePolicy.RequiredGold(c, p.Count),
                Eligible = blocked == null, BlockingReason = blocked, ResetSummary = PrestigeResetSummary(),
                Perks = PrestigePolicy.Perks.Select((id, i) => new IntegrationPrestigePerk {
                    Id = id, Name = names[i], Description = descriptions[i], Rank = p.Rank(id), Cap = c.RankCap
                }).ToArray()
            };
        }
    }
}
