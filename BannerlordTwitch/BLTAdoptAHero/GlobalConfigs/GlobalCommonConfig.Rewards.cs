using System;
using System.Collections.ObjectModel;
using BannerlordTwitch.Annotations;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Achievements;
using BLTAdoptAHero.Actions.Util;
using TaleWorlds.Library;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BLTAdoptAHero
{
    internal partial class GlobalCommonConfig
    {
        #region User Editable
        #region Kill Streak Rewards
        [LocDisplayName("{=3DZYc6hN}Kill Streaks"),
         LocCategory("Kill Streak Rewards", "{=lnz7d1BI}Kill Streak Rewards"),
         LocDescription("{=3DZYc6hN}Kill Streaks"),
         Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)),
         PropertyOrder(1), UsedImplicitly]
        public ObservableCollection<KillStreakDef> KillStreaks { get; set; } = new();

        [LocDisplayName("{=wQ7lXgLA}Show Kill Streak Popup"),
         LocCategory("Kill Streak Rewards", "{=lnz7d1BI}Kill Streak Rewards"),
         LocDescription("{=wDW3143d}Whether to use the popup banner to announce kill streaks. Will only print in the overlay instead if disabled."),
         PropertyOrder(2), UsedImplicitly]
        public bool ShowKillStreakPopup { get; set; } = true;

        [LocDisplayName("{=rhwujKvf}Kill Streak Popup Alert Sound"),
         LocCategory("Kill Streak Rewards", "{=lnz7d1BI}Kill Streak Rewards"),
         LocDescription("{=1GVV1fjY}Sound to play when killstreak popup is disabled."),
         PropertyOrder(3), UsedImplicitly]
        public Log.Sound KillStreakPopupAlertSound { get; set; } = Log.Sound.Horns2;

        [LocDisplayName("{=dP9AoB9o}Reference Level Reward"),
         LocCategory("Kill Streak Rewards", "{=lnz7d1BI}Kill Streak Rewards"),
         LocDescription("{=y7AZjeSK}The level at which the rewards normalize and start to reduce (if relative level scaling is enabled)."),
         PropertyOrder(4), UsedImplicitly]
        public int ReferenceLevelReward { get; set; } = 15;
        #endregion

        #region Achievements
        [LocDisplayName("{=zTLei6dQ}Achievements"),
         LocCategory("Achievements", "{=EPr2clqT}Achievements"),
         LocDescription("{=zTLei6dQ}Achievements"),
         Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)),
         PropertyOrder(1), UsedImplicitly]
        public ObservableCollection<AchievementDef> Achievements { get; set; } = new();
        #endregion

        #region Shouts
        [LocDisplayName("{=HkD6326j}Shouts"),
         LocCategory("Shouts", "{=UhUpH8C8}Shouts"),
         LocDescription("{=ufqtH5QV}Custom shouts"),
         Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)),
         PropertyOrder(1), UsedImplicitly]
        public ObservableCollection<Shout> Shouts { get; set; } = new();

        [LocDisplayName("{=wehigXCC}Include Default Shouts"),
         LocCategory("Shouts", "{=UhUpH8C8}Shouts"),
         LocDescription("{=m6Vv2LBt}Whether to include default shouts"),
         PropertyOrder(2), UsedImplicitly]
        public bool IncludeDefaultShouts { get; set; } = true;
        #endregion
        #endregion
    }
}
