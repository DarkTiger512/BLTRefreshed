﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using BannerlordTwitch.Localization;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Achievements;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero.Powers
{
    public class PowerGroupItemBase : ICloneable, ILoaded, INotifyPropertyChanged
    {
        [LocDisplayName("{=WLtUn4t7}Requirements"),
         LocDescription("{=sl5ZoJdv}Optional unlock criteria"),
         Editor(typeof(DerivedClassCollectionEditor<IAchievementRequirement>),
             typeof(DerivedClassCollectionEditor<IAchievementRequirement>)),
         PropertyOrder(1), UsedImplicitly]
        public ObservableCollection<IAchievementRequirement> Requirements { get; set; } = new();

        public PowerGroupItemBase()
        {
            // For when these are created via the configure tool
            PowerConfig = ConfigureContext.CurrentlyEditedSettings == null
                ? null : GlobalHeroPowerConfig.Get(ConfigureContext.CurrentlyEditedSettings);
        }

        public bool IsUnlocked(Hero hero) => Requirements.All(r => r.IsMet(hero));

        public override string ToString()
            => "{=hPcS0MIw}requires {Requirements}"
                .Translate(("Requirements", string.Join("+", Requirements.Select(r => r.ToString()))));

        #region Implementation Detail
        [YamlIgnore, Browsable(false)]
        protected GlobalHeroPowerConfig PowerConfig { get; set; }
        #endregion

        #region ICloneable
        public object Clone()
        {
            var clone = CloneHelpers.CloneProperties(this);
            clone.Requirements = new(CloneHelpers.CloneCollection(Requirements));
            clone.PowerConfig = PowerConfig;
            return clone;
        }
        #endregion

        #region ILoaded
        public void OnLoaded(BannerlordTwitch.Settings settings)
        {
            PowerConfig = GlobalHeroPowerConfig.Get(settings);
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}