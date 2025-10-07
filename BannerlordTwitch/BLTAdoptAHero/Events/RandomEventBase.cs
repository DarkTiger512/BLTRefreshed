using System;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BLTAdoptAHero.Annotations;
using TaleWorlds.CampaignSystem;

namespace BLTAdoptAHero.Events
{
    /// <summary>
    /// Base class for random world events that can trigger during gameplay
    /// </summary>
    public abstract class RandomEventBase
    {
        /// <summary>
        /// Unique identifier for this event type
        /// </summary>
        public abstract string EventId { get; }

        /// <summary>
        /// Display name shown in the UI
        /// </summary>
        public abstract string EventName { get; }

        /// <summary>
        /// Description of what this event does
        /// </summary>
        public abstract string EventDescription { get; }

        /// <summary>
        /// Whether this event is enabled
        /// </summary>
        public virtual bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Base chance per day for this event to trigger (0.0 to 1.0)
        /// </summary>
        public virtual float TriggerChancePerDay { get; set; } = 0.01f;

        /// <summary>
        /// Register any dialogs needed for this event. Called during campaign initialization.
        /// </summary>
        public virtual void RegisterDialogs(CampaignGameStarter starter) { }

        /// <summary>
        /// Minimum number of days between this event triggering
        /// </summary>
        public virtual int CooldownDays { get; set; } = 30;

        /// <summary>
        /// Last campaign day this event was triggered
        /// </summary>
        public CampaignTime LastTriggeredTime { get; set; } = CampaignTime.Zero;

        /// <summary>
        /// Check if this event can trigger right now
        /// </summary>
        public virtual bool CanTrigger()
        {
            if (!IsEnabled)
                return false;

            // Check cooldown
            float daysSinceLastTrigger = (float)(CampaignTime.Now - LastTriggeredTime).ToDays;
            if (daysSinceLastTrigger < CooldownDays)
                return false;

            return CheckSpecificConditions();
        }

        /// <summary>
        /// Override to add event-specific trigger conditions
        /// </summary>
        protected virtual bool CheckSpecificConditions()
        {
            return true;
        }

        /// <summary>
        /// Execute the event
        /// </summary>
        public void Trigger()
        {
            LastTriggeredTime = CampaignTime.Now;
            ExecuteEvent();
        }

        /// <summary>
        /// Override to implement the event logic
        /// </summary>
        protected abstract void ExecuteEvent();

        /// <summary>
        /// Generate documentation for this event
        /// </summary>
        public virtual void GenerateDocumentation(IDocumentationGenerator generator)
        {
            generator.PropertyValuePair("Event", EventName);
            generator.PropertyValuePair("Description", EventDescription);
            generator.PropertyValuePair("Trigger Chance", $"{TriggerChancePerDay * 100:F2}% per day");
            generator.PropertyValuePair("Cooldown", $"{CooldownDays} days");
        }
    }
}
