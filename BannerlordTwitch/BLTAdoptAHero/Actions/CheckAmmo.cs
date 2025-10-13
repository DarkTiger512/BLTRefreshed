using System;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero
{
    [LocDisplayName("{=BLT_CheckAmmo}Check Ammo"),
     LocDescription("{=BLT_CheckAmmoDesc}Shows your hero's remaining ammunition in the current mission"),
     UsedImplicitly]
    public class CheckAmmo : HeroCommandHandlerBase
    {
        protected override void ExecuteInternal(Hero adoptedHero, ReplyContext context, object config,
            Action<string> onSuccess, Action<string> onFailure)
        {
            // Check if there's an active mission
            if (Mission.Current == null)
            {
                onFailure("{=BLT_NoActiveMission}No active mission - you must be in battle to check ammo!".Translate());
                return;
            }

            // Get the agent for this hero
            var agent = adoptedHero.GetAgent();
            if (agent == null)
            {
                onFailure("{=BLT_HeroNotInMission}Your hero is not currently in this mission! Use !summon or !attack first.".Translate());
                return;
            }

            // Check if hero is alive
            if (!agent.IsActive())
            {
                onFailure("{=BLT_HeroDead}Your hero is dead!".Translate());
                return;
            }

            // Get hero's class to check if they're a ranged class
            var heroClass = adoptedHero.GetClass();
            bool isRangedClass = heroClass?.Formation == "Ranged" || heroClass?.Formation == "HorseArcher";

            // Count ammunition
            int totalAmmo = 0;
            var ammoTypes = new System.Collections.Generic.List<string>();
            var debugInfo = new System.Collections.Generic.List<string>();

            for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
            {
                var equipmentElement = agent.Equipment[i];
                if (!equipmentElement.IsEmpty)
                {
                    var weapon = equipmentElement.Item;
                    debugInfo.Add($"Slot {i}: {weapon.Name} (Type: {weapon.Type}, Amount: {equipmentElement.Amount})");
                    
                    // Check if it's ammunition (arrows, bolts, throwing weapons)
                    if (weapon.Type == ItemObject.ItemTypeEnum.Arrows || 
                        weapon.Type == ItemObject.ItemTypeEnum.Bolts ||
                        weapon.Type == ItemObject.ItemTypeEnum.Thrown)
                    {
                        // Use equipmentElement.Amount instead of GetAmmoAmount
                        short ammoCount = equipmentElement.Amount;
                        debugInfo.Add($"  -> Is ammo! Count: {ammoCount}");
                        totalAmmo += ammoCount;
                        
                        string ammoName = weapon.Name?.ToString() ?? "Unknown";
                        ammoTypes.Add($"{ammoName}: {ammoCount}");
                    }
                }
            }

            // Debug output
            if (debugInfo.Any())
            {
                BannerlordTwitch.Util.Log.Info($"[AMMO DEBUG] Equipment for {adoptedHero.Name}:");
                foreach (var info in debugInfo)
                {
                    BannerlordTwitch.Util.Log.Info($"[AMMO DEBUG] {info}");
                }
            }

            // Build response message
            if (totalAmmo > 0)
            {
                string ammoDetails = string.Join(", ", ammoTypes);
                string classInfo = isRangedClass ? $" ({heroClass?.Name})" : "";
                
                onSuccess($"💥 Ammunition Status{classInfo}: {ammoDetails} | Total: {totalAmmo}");
            }
            else
            {
                // Show debug info in response if no ammo found
                string debugOutput = debugInfo.Any() 
                    ? $" | DEBUG: Found {debugInfo.Count} items: " + string.Join("; ", debugInfo.Take(3))
                    : " | DEBUG: No equipment found";
                
                if (isRangedClass)
                {
                    onFailure($"Out of ammo! You're running on empty! 🏹💨{debugOutput}");
                }
                else
                {
                    onFailure($"You don't have any ranged weapons with ammunition.{debugOutput}");
                }
            }
        }
    }
}
