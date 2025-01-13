﻿using System.Linq;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BannerlordTwitch.Helpers
{
    public struct OneShotEffect
    {
        [LocDisplayName("{=cv0hxm25}ParticleEffect"), LocDescription("{=N1WsBndO}Particle Effect to play"),
         ItemsSource(typeof(OneShotParticleEffectItemSource)),
         PropertyOrder(1), UsedImplicitly]
        public string ParticleEffect { get; set; }

        [LocDisplayName("{=FyXztxDH}Sound"), LocDescription("{=2KrpVrR8}Sound to play"),
         ItemsSource(typeof(SoundEffectItemSource)),
         PropertyOrder(2), UsedImplicitly]
        public string Sound { get; set; }

        public void Trigger(Hero hero)
        {
            Trigger(Mission.Current?.Agents?.FirstOrDefault(a => a.GetHero() == hero));
        }

        public void Trigger(Agent agent)
        {
            if (agent?.AgentVisuals != null)
            {
                Trigger(agent.AgentVisuals.GetGlobalFrame(), agent.Index);
            }
        }

        public readonly void Trigger(MatrixFrame location, int relatedAgentIndex = -1)
        {
            Trigger(ParticleEffect, Sound, location, relatedAgentIndex);
        }

        public static void Trigger(string particleEffect, string sound, MatrixFrame location, int relatedAgentIndex = -1)
        {
            if (!string.IsNullOrEmpty(particleEffect))
            {
                Mission.Current.Scene.CreateBurstParticle(
                    ParticleSystemManager.GetRuntimeIdByName(particleEffect),
                    location);
            }

            if (!string.IsNullOrEmpty(sound))
            {
                Mission.Current.MakeSound(SoundEvent.GetEventIdFromString(sound),
                    location.origin, false, true, relatedAgentIndex, -1);
            }
        }

        public override readonly string ToString() => $"{OneShotParticleEffectItemSource.GetFriendlyName(ParticleEffect)} {Sound}";
    }
}