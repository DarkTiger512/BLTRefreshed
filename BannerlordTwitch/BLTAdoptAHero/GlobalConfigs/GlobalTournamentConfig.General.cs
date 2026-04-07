using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.Library;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    internal partial class GlobalTournamentConfig
    {
        [LocDisplayName("{=P1ZCMZbp}Start Health Multiplier"),
         LocCategory("General", "{=C5T5nnix}General"),
         LocDescription("{=n6Bc5M3s}Amount to multiply normal starting health by"),
         PropertyOrder(1), Range(0.5, 10),
         Editor(typeof(SliderFloatEditor), typeof(SliderFloatEditor)), UsedImplicitly, Document]
        public float StartHealthMultiplier { get; set; } = 2;

        [LocDisplayName("{=x3wiU1LY}Disable Kill Rewards In Tournament"),
         LocCategory("General", "{=C5T5nnix}General"),
         LocDescription("{=j63d8DuH}Heroes won't get any kill rewards in tournaments"),
         PropertyOrder(2), Document, UsedImplicitly]
        public bool DisableKillRewardsInTournament { get; set; } = true;

        [LocDisplayName("{=ksB1FmZV}Disable Tracking Kills Tournament"),
         LocCategory("General", "{=C5T5nnix}General"),
         LocDescription("{=o5aH9rTO}Tournament kills / deaths won't be counted towards achievements or kill streaks"),
         PropertyOrder(3), Document, UsedImplicitly]
        public bool DisableTrackingKillsTournament { get; set; } = true;

        [LocDisplayName("{=bBipANlh}No Horses"),
         LocCategory("Equipment", "{=i7ZDVTaw}Equipment"),
         LocDescription("{=sJ68yZs1}Remove horses completely from the BLT tournaments (the horse AI is terrible)"),
         PropertyOrder(2), UsedImplicitly, Document]
        public bool NoHorses { get; set; } = true;

        [LocDisplayName("{=CYVy4Hhx}No Spears"),
         LocCategory("Equipment", "{=i7ZDVTaw}Equipment"),
         LocDescription("{=BjMC8Htn}Replaces all lances and spears with swords, because lance and spear combat is terrible"),
         PropertyOrder(3), UsedImplicitly, Document]
        public bool NoSpears { get; set; } = true;

        [LocDisplayName("{=Ed55OyZ5}Normalize Armor"),
         LocCategory("Equipment", "{=i7ZDVTaw}Equipment"),
         LocDescription("{=kYFdZ8Yx}Replaces all armor with fixed tier armor, based on Culture if possible (tier specified by Normalized Armor Tier below)"),
         PropertyOrder(4), UsedImplicitly, Document]
        public bool NormalizeArmor { get; set; }

        [LocDisplayName("{=xHJbcOaV}Normalize Armor Tier"),
         LocCategory("Equipment", "{=i7ZDVTaw}Equipment"),
         LocDescription("{=HnqCyrDD}Armor tier to set all contenstants to (1 to 6), if Normalize Armor is enabled"),
         PropertyOrder(5), Range(1, 6), UsedImplicitly, Document]
        public int NormalizeArmorTier { get; set; } = 3;

        [LocDisplayName("{=5Y08IsDl}Randomize Weapon Types"),
         LocCategory("Equipment", "{=i7ZDVTaw}Equipment"),
         LocDescription("{=oSCPOoqi}Randomizes the weapons used in each round, weighted based on the classes of the participants"),
         PropertyOrder(6), UsedImplicitly, Document]
        public bool RandomizeWeaponTypes { get; set; } = true;

        [LocDisplayName("{=UCAbZYqU}Previous Winner Debuffs"),
         LocCategory("Balancing", "{=Zwh9GYUE}Balancing"),
         LocDescription("{=FrloGGew}Applies skill debuffers to previous tournament winners"),
         Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)), PropertyOrder(1), UsedImplicitly, Document]
        public ObservableCollection<SkillDebuffDef> PreviousWinnerDebuffs { get; set; } = new() { new() };
    }
}
