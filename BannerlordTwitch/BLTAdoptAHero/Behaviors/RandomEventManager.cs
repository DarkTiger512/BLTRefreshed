using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordTwitch.Util;
using BLTAdoptAHero.Events;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace BLTAdoptAHero.Behaviors
{
    /// <summary>
    /// Campaign behavior that manages random world events
    /// </summary>
    public class RandomEventManager : CampaignBehaviorBase
    {
        private List<RandomEventBase> registeredEvents = new List<RandomEventBase>();
        private CampaignTime lastCheckTime = CampaignTime.Zero;
        
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }
        
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Register dialogs for all events
            foreach (var evt in registeredEvents)
            {
                evt.RegisterDialogs(starter);
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Sync event cooldown data
            if (dataStore.IsLoading)
            {
                // Load event states
                foreach (var evt in registeredEvents)
                {
                    string key = $"BLT_Event_{evt.EventId}_LastTrigger";
                    long ticks = 0;
                    dataStore.SyncData(key, ref ticks);
                    if (ticks > 0)
                    {
                        evt.LastTriggeredTime = CampaignTime.Years((float)ticks / 365f);
                    }
                }
            }
            else if (dataStore.IsSaving)
            {
                // Save event states
                foreach (var evt in registeredEvents)
                {
                    string key = $"BLT_Event_{evt.EventId}_LastTrigger";
                    long ticks = (long)(evt.LastTriggeredTime.ElapsedDaysUntilNow);
                    dataStore.SyncData(key, ref ticks);
                }
            }
        }

        /// <summary>
        /// Register an event to be randomly triggered
        /// </summary>
        public void RegisterEvent(RandomEventBase eventInstance)
        {
            if (eventInstance == null)
            {
                Log.Error("Attempted to register null event");
                return;
            }

            if (registeredEvents.Any(e => e.EventId == eventInstance.EventId))
            {
                Log.Error($"Event with ID {eventInstance.EventId} is already registered");
                return;
            }

            registeredEvents.Add(eventInstance);
            Log.Info($"[Event Manager] Registered event: {eventInstance.EventName} (ID: {eventInstance.EventId})");
        }

        /// <summary>
        /// Unregister an event
        /// </summary>
        public void UnregisterEvent(string eventId)
        {
            var evt = registeredEvents.FirstOrDefault(e => e.EventId == eventId);
            if (evt != null)
            {
                registeredEvents.Remove(evt);
                Log.Info($"[Event Manager] Unregistered event: {evt.EventName}");
            }
        }

        /// <summary>
        /// Get all registered events
        /// </summary>
        public IReadOnlyList<RandomEventBase> GetRegisteredEvents()
        {
            return registeredEvents.AsReadOnly();
        }

        private void OnDailyTick()
        {
            // Check if we should evaluate events today
            Log.Info($"[Event Manager] Daily tick - checking {registeredEvents.Count} events");
            CheckAndTriggerEvents();
        }

        private void OnHourlyTick()
        {
            // Additional hourly checks if needed
            // For now, we only check daily
        }

        private void CheckAndTriggerEvents()
        {
            try
            {
                // Don't check if in a mission or menu
                if (Campaign.Current == null || Hero.MainHero == null)
                {
                    return;
                }

                foreach (var evt in registeredEvents.ToList())
                {
                    Log.Info($"[Event Manager] Checking event: {evt.EventName} (ID: {evt.EventId})");
                    
                    if (!evt.IsEnabled)
                    {
                        Log.Info($"[Event Manager] Event {evt.EventName} is disabled");
                        continue;
                    }

                    // Check if event can trigger based on conditions and cooldown
                    if (!evt.CanTrigger())
                    {
                        Log.Info($"[Event Manager] Event {evt.EventName} cannot trigger (conditions or cooldown not met)");
                        continue;
                    }

                    // Roll for chance
                    float roll = MBRandom.RandomFloat;
                    Log.Info($"[Event Manager] Event {evt.EventName} rolled {roll:F4} vs chance {evt.TriggerChancePerDay:F4}");
                    if (roll <= evt.TriggerChancePerDay)
                    {
                        Log.Info($"[Event Manager] Triggering event: {evt.EventName} (roll: {roll:F4} <= {evt.TriggerChancePerDay:F4})");
                        
                        try
                        {
                            evt.Trigger();
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"[Event Manager] Error triggering event {evt.EventName}: {ex.Message}\n{ex.StackTrace}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[Event Manager] Error in CheckAndTriggerEvents: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Manually trigger an event (for testing or admin commands)
        /// </summary>
        public bool TriggerEventManually(string eventId)
        {
            var evt = registeredEvents.FirstOrDefault(e => e.EventId == eventId);
            if (evt == null)
            {
                Log.Error($"[Event Manager] Event with ID {eventId} not found");
                return false;
            }

            if (!evt.CanTrigger())
            {
                Log.Error($"[Event Manager] Event {evt.EventName} cannot be triggered (conditions not met)");
                return false;
            }

            try
            {
                Log.Info($"[Event Manager] Manually triggering event: {evt.EventName}");
                evt.Trigger();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[Event Manager] Error manually triggering event {evt.EventName}: {ex.Message}");
                return false;
            }
        }
    }
}
