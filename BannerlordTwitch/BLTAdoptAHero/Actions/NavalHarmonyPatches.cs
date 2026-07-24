using HarmonyLib;
using NavalDLC.CampaignBehaviors;
using NavalDLC.Missions.MissionLogics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLTAdoptAHero.Actions
{
    internal class NavalHarmonyPatches
    {
        [HarmonyPatch(typeof(ShipAgentSpawnLogic), "IsAnyTeamsUnfilled")]
        public static class Patch_IsAnyTeamsUnfilled
        {
            static bool Prefix(ref bool __result)
            {
                // Always return true, ignoring original logic
                __result = true;
                return false; // skip original method
            }
        }

        [HarmonyPatch(typeof(ShipTradeCampaignBehavior), "OnShipOwnerChanged")]
        static class BLT_Suppress_OnShipOwnerChanged_Exception
        {
            static Exception Finalizer(Exception __exception)
            {
                // If the method threw, swallow it completely
                if (__exception != null)
                {
                    return null;
                }

                return null;
            }
        }

    }
}
