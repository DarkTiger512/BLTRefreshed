using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BannerlordTwitch.Util;
using BannerlordTwitch.Integration;
using BannerlordTwitch.Helpers;
using BLTAdoptAHero.Annotations;
using Microsoft.AspNet.SignalR;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero.UI
{
    [UsedImplicitly]
    public class MissionInfoHub : Hub
    {
        [UsedImplicitly]
        public class HeroState
        {
            [UsedImplicitly] public string Id;
            public string Name;

            [UsedImplicitly] public float HP;

            [UsedImplicitly] public float CooldownFractionRemaining;
            [UsedImplicitly] public float CooldownSecondsRemaining;

            [UsedImplicitly] public float ActivePowerFractionRemaining;
            [UsedImplicitly] public float HealFractionRemaining;

            [UsedImplicitly] public bool IsPlayerSide;

            [UsedImplicitly] public int TournamentTeam;

            [UsedImplicitly] public string State;

            [UsedImplicitly] public float MaxHP;

            [UsedImplicitly] public int Kills;

            [UsedImplicitly] public int Retinue;
            [UsedImplicitly] public int DeadRetinue;

            [UsedImplicitly] public int Retinue2;
            [UsedImplicitly] public int DeadRetinue2;

            [UsedImplicitly] public int RetinueKills;

            [UsedImplicitly] public int GoldEarned;
            [UsedImplicitly] public int XPEarned;
            [UsedImplicitly] public int AmmoCurrent;
            [UsedImplicitly] public int AmmoMaximum;
        }

        private static readonly List<HeroState> heroState = new();

        public override Task OnConnected()
        {
            Clients.Caller.setKeyLabels(new
            {
                Kills = "{=AM2zlkem}Kills".Translate(),
                RetinueKills = "{=79JXI4JL}+Retinue Kills".Translate(),
                Gold = "{=o0Q8Y1Qg}Gold".Translate(),
                XP = "{=VtEJiMWy}XP".Translate(),
            });
            Update();
            return base.OnConnected();
        }

        public static void Update()
        {
            lock (heroState)
            {
                GlobalHost.ConnectionManager.GetHubContext<MissionInfoHub>()
                    .Clients.All.update(heroState);
                var supported = MissionHelpers.InTournament() || MissionHelpers.InFieldBattleMission() || MissionHelpers.InSiegeMission();
                if (supported)
                {
                    IntegrationBattleProvider.Update(MissionHelpers.InTournament() ? "tournament" : "battle", Mission.Current?.IsDeploymentFinished == true,
                        heroState.ConvertAll(state => new IntegrationBattleCombatant
                        {
                            Id = state.Id ?? state.Name, Name = state.Name, HP = state.HP, MaxHP = state.MaxHP, State = state.State,
                            IsPlayerSide = state.IsPlayerSide, TournamentTeam = state.TournamentTeam,
                            CooldownFractionRemaining = state.CooldownFractionRemaining, CooldownSecondsRemaining = state.CooldownSecondsRemaining,
                            ActivePowerFractionRemaining = state.ActivePowerFractionRemaining, Kills = state.Kills,
                            Retinue = state.Retinue, DeadRetinue = state.DeadRetinue, EliteRetinue = state.Retinue2,
                            DeadEliteRetinue = state.DeadRetinue2, RetinueKills = state.RetinueKills,
                            GoldEarned = state.GoldEarned, XPEarned = state.XPEarned,
                            AmmoCurrent = state.AmmoCurrent, AmmoMaximum = state.AmmoMaximum,
                        }));
                }
            }
        }

        public static void Remove(string name)
        {
            lock (heroState)
            {
                heroState.RemoveAll(h
                    => string.Equals(h.Name, name, StringComparison.CurrentCultureIgnoreCase));
            }
        }

        public static void Clear()
        {
            lock (heroState)
            {
                heroState.Clear();
            }

            IntegrationBattleProvider.Clear();
            // Update the legacy overlay immediately without reactivating managed mission state.
            GlobalHost.ConnectionManager.GetHubContext<MissionInfoHub>().Clients.All.update(Array.Empty<HeroState>());
        }

        public static void UpdateHero(HeroState state)
        {
            lock (heroState)
            {
                heroState.RemoveAll(h
                    => string.Equals(h.Name, state.Name, StringComparison.CurrentCultureIgnoreCase));
                heroState.Add(state);
            }
        }

        private static string GetContentPath(string fileName) => Path.Combine(
            Path.GetDirectoryName(typeof(MissionInfoHub).Assembly.Location) ?? ".",
            "Overlay", "MissionInfo", fileName);
        private static string GetContent(string fileName) => File.ReadAllText(GetContentPath(fileName));

        public static void Register()
        {
            BLTOverlay.BLTOverlay.Register("mission", 200,
                GetContent("MissionInfo.css"),
                GetContent("MissionInfo.html"),
                GetContent("MissionInfo.js"));
        }
    }
}
