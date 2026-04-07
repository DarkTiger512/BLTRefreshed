using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem.TournamentGames;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    internal partial class GlobalTournamentConfig
    {
        public class Round1Def
        {
            [LocDisplayName("{=WUQRg4yA}Vanilla"),
             LocDescription("{=nfYBftnO}Allow the vanilla round setup"),
             PropertyOrder(1), UsedImplicitly, Document]
            public bool EnableVanilla { get; set; } = true;
            [LocDisplayName("{=ErBd0iuP}1 Match 4 Teams"),
             LocDescription("{=5E1lXDYh}Allow 1 match with 4 teams of 4"),
             PropertyOrder(3), UsedImplicitly, Document]
            public bool Enable1Match4Teams { get; set; }
            [LocDisplayName("{=SsArdTT9}2 Matches 2 Teams"),
             LocDescription("{=jEghMKUV}Allow 2 matches with 2 teams of 4"),
             PropertyOrder(4), UsedImplicitly, Document]
            public bool Enable2Match2Teams { get; set; }
            [LocDisplayName("{=qb1yo7DZ}2 Matches 4 Teams"),
             LocDescription("{=f8rXSGwN}Allow 2 matches with 4 teams of 2"),
             PropertyOrder(5), UsedImplicitly, Document]
            public bool Enable2Match4Teams { get; set; }
            [LocDisplayName("{=WAzvdvAJ}4 Matches 2 Teams"),
             LocDescription("{=GPCYzshA}Allow 4 matches with 2 teams of 2"),
             PropertyOrder(6), UsedImplicitly, Document]
            public bool Enable4Match2Teams { get; set; }
            [LocDisplayName("{=sMCb8Xeu}4 Matches 4 Teams"),
             LocDescription("{=MjsTFwUv}Allow 4 matches with 4 teams of 1"),
             PropertyOrder(7), UsedImplicitly, Document]
            public bool Enable4Match4Teams { get; set; }
            [LocDisplayName("{=URNM9UqS}8 Matches 2 Teams"),
             LocDescription("{=otwzXPTQ}Allow 8 matches with 2 teams of 1"),
             PropertyOrder(8), UsedImplicitly, Document]
            public bool Enable8Match2Teams { get; set; }

            public override string ToString()
            {
                var enabled = new List<string>();
                if (EnableVanilla) enabled.Add("{=WUQRg4yA}Vanilla".Translate());
                if (Enable1Match4Teams) enabled.Add("{=ErBd0iuP}1 Match 4 Teams".Translate());
                if (Enable2Match2Teams) enabled.Add("{=SsArdTT9}2 Matches 2 Teams".Translate());
                if (Enable2Match4Teams) enabled.Add("{=qb1yo7DZ}2 Matches 4 Teams".Translate());
                if (Enable4Match2Teams) enabled.Add("{=WAzvdvAJ}4 Matches 2 Teams".Translate());
                if (Enable4Match4Teams) enabled.Add("{=sMCb8Xeu}4 Matches 4 Teams".Translate());
                if (Enable8Match2Teams) enabled.Add("{=URNM9UqS}8 Matches 2 Teams".Translate());
                return string.Join(", ", enabled);
            }

            public const int ParticipantCount = 16;
            public const int WinnerCount = ParticipantCount / 2;

            public TournamentRound GetRandomRound(TournamentRound vanilla, TournamentGame.QualificationMode qualificationMode)
            {
                var matches = new List<TournamentRound>();
                if (Enable1Match4Teams)
                    matches.Add(new(ParticipantCount, 1, 4, WinnerCount, qualificationMode));
                if (Enable2Match2Teams)
                    matches.Add(new(ParticipantCount, 2, 2, WinnerCount, qualificationMode));
                if (Enable2Match4Teams)
                    matches.Add(new(ParticipantCount, 2, 4, WinnerCount, qualificationMode));
                if (Enable4Match2Teams)
                    matches.Add(new(ParticipantCount, 4, 2, WinnerCount, qualificationMode));
                if (Enable4Match4Teams)
                    matches.Add(new(ParticipantCount, 4, 4, WinnerCount, qualificationMode));
                if (Enable8Match2Teams)
                    matches.Add(new(ParticipantCount, 8, 2, WinnerCount, qualificationMode));
                if (EnableVanilla || !matches.Any())
                    matches.Add(vanilla);
                return matches.SelectRandom();
            }
        }

        public class Round2Def
        {
            [LocDisplayName("{=WUQRg4yA}Vanilla"),
             LocDescription("{=nfYBftnO}Allow the vanilla round setup"),
             PropertyOrder(1), UsedImplicitly, Document]
            public bool EnableVanilla { get; set; } = true;
            [LocDisplayName("{=uuBzCFgM}1 Match 2 Teams"),
             LocDescription("{=5hLSHeuv}Allow 1 match with 2 teams of 4"),
             PropertyOrder(2), UsedImplicitly, Document]
            public bool Enable1Match2Teams { get; set; }
            [LocDisplayName("{=ErBd0iuP}1 Match 4 Teams"),
             LocDescription("{=CLbRKpZp}Allow 1 match with 4 teams of 2"),
             PropertyOrder(3), UsedImplicitly, Document]
            public bool Enable1Match4Teams { get; set; }
            [LocDisplayName("{=SsArdTT9}2 Matches 2 Teams"),
             LocDescription("{=Dr1Js5fB}Allow 2 matches with 2 teams of 2"),
             PropertyOrder(4), UsedImplicitly, Document]
            public bool Enable2Match2Teams { get; set; }
            [LocDisplayName("{=qb1yo7DZ}2 Matches 4 Teams"),
             LocDescription("{=nmMzoqeC}Allow 2 matches with 4 teams of 1"),
             PropertyOrder(5), UsedImplicitly, Document]
            public bool Enable2Match4Teams { get; set; }
            [LocDisplayName("{=WAzvdvAJ}4 Matches 2 Teams"),
             LocDescription("{=xMaA76xu}Allow 4 matches with 2 teams of 1"),
             PropertyOrder(6), UsedImplicitly, Document]
            public bool Enable4Match2Teams { get; set; }

            public override string ToString()
            {
                var enabled = new List<string>();
                if (EnableVanilla) enabled.Add("{=WUQRg4yA}Vanilla".Translate());
                if (Enable1Match2Teams) enabled.Add("{=uuBzCFgM}1 Match 2 Teams".Translate());
                if (Enable1Match4Teams) enabled.Add("{=ErBd0iuP}1 Match 4 Teams".Translate());
                if (Enable2Match2Teams) enabled.Add("{=SsArdTT9}2 Matches 2 Teams".Translate());
                if (Enable2Match4Teams) enabled.Add("{=qb1yo7DZ}2 Matches 4 Teams".Translate());
                if (Enable4Match2Teams) enabled.Add("{=WAzvdvAJ}4 Matches 2 Teams".Translate());
                return string.Join(", ", enabled);
            }

            public const int ParticipantCount = 8;
            public const int WinnerCount = ParticipantCount / 2;

            public TournamentRound GetRandomRound(TournamentRound vanilla, TournamentGame.QualificationMode qualificationMode)
            {
                var matches = new List<TournamentRound>();
                if (Enable1Match2Teams)
                    matches.Add(new(ParticipantCount, 1, 2, WinnerCount, qualificationMode));
                if (Enable1Match4Teams)
                    matches.Add(new(ParticipantCount, 1, 4, WinnerCount, qualificationMode));
                if (Enable2Match2Teams)
                    matches.Add(new(ParticipantCount, 2, 2, WinnerCount, qualificationMode));
                if (Enable2Match4Teams)
                    matches.Add(new(ParticipantCount, 2, 4, WinnerCount, qualificationMode));
                if (Enable4Match2Teams)
                    matches.Add(new(ParticipantCount, 4, 2, WinnerCount, qualificationMode));
                if (EnableVanilla || !matches.Any())
                    matches.Add(vanilla);
                return matches.SelectRandom();
            }
        }

        public class Round3Def
        {
            [LocDisplayName("{=WUQRg4yA}Vanilla"),
             LocDescription("{=nfYBftnO}Allow the vanilla round setup"),
             PropertyOrder(1), UsedImplicitly, Document]
            public bool EnableVanilla { get; set; } = true;
            [LocDisplayName("{=uuBzCFgM}1 Match 2 Teams"),
             LocDescription("{=t9yLB8gB}Allow 1 match with 2 teams of 2"),
             PropertyOrder(2), UsedImplicitly, Document]
            public bool Enable1Match2Teams { get; set; }
            [LocDisplayName("{=ErBd0iuP}1 Match 4 Teams"),
             LocDescription("{=YfOnzOH6}Allow 1 match with 4 teams of 1"),
             PropertyOrder(3), UsedImplicitly, Document]
            public bool Enable1Match4Teams { get; set; }
            [LocDisplayName("{=SsArdTT9}2 Matches 2 Teams"),
             LocDescription("{=OrZaS458}Allow 2 matches with 2 teams of 1"),
             PropertyOrder(4), UsedImplicitly, Document]
            public bool Enable2Match2Teams { get; set; }

            public override string ToString()
            {
                var enabled = new List<string>();
                if (EnableVanilla) enabled.Add("{=WUQRg4yA}Vanilla".Translate());
                if (Enable1Match2Teams) enabled.Add("{=uuBzCFgM}1 Match 2 Teams".Translate());
                if (Enable1Match4Teams) enabled.Add("{=ErBd0iuP}1 Match 4 Teams".Translate());
                if (Enable2Match2Teams) enabled.Add("{=SsArdTT9}2 Matches 2 Teams".Translate());
                return string.Join(", ", enabled);
            }

            public const int ParticipantCount = 4;
            public const int WinnerCount = ParticipantCount / 2;

            public TournamentRound GetRandomRound(TournamentRound vanilla, TournamentGame.QualificationMode qualificationMode)
            {
                var matches = new List<TournamentRound>();
                if (Enable1Match2Teams)
                    matches.Add(new(ParticipantCount, 1, 2, WinnerCount, qualificationMode));
                if (Enable1Match4Teams)
                    matches.Add(new(ParticipantCount, 1, 4, WinnerCount, qualificationMode));
                if (Enable2Match2Teams)
                    matches.Add(new(ParticipantCount, 2, 2, WinnerCount, qualificationMode));
                if (EnableVanilla || !matches.Any())
                    matches.Add(vanilla);
                return matches.SelectRandom();
            }
        }

        [LocDisplayName("{=eivyBgAa}Round 1 Type"),
         LocCategory("Round Type", "{=KOIu6Q7d}Round Type"),
         LocDescription("{=eivyBgAa}Round 1 Type"),
         PropertyOrder(1), ExpandableObject, UsedImplicitly, Document]
        public Round1Def Round1Type { get; set; } = new();
        [LocDisplayName("{=pefX6BdQ}Round 2 Type"),
         LocCategory("Round Type", "{=KOIu6Q7d}Round Type"),
         LocDescription("{=pefX6BdQ}Round 2 Type"),
         PropertyOrder(2), ExpandableObject, UsedImplicitly, Document]
        public Round2Def Round2Type { get; set; } = new();
        [LocDisplayName("{=GpV7pKPY}Round 3 Type"),
         LocCategory("Round Type", "{=KOIu6Q7d}Round Type"),
         LocDescription("{=GpV7pKPY}Round 3 Type"),
         PropertyOrder(3), ExpandableObject, UsedImplicitly, Document]
        public Round3Def Round3Type { get; set; } = new();
    }
}
