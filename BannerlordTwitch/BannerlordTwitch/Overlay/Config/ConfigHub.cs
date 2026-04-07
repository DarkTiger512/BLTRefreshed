using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BannerlordTwitch;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BLTOverlay
{
    public class ConfigHub : Hub
    {
        // ── DTOs sent to the web UI ──────────────────────────

        [UsedImplicitly]
        public class HandlerInfo
        {
            [UsedImplicitly] public string id;
            [UsedImplicitly] public string displayName;
        }

        [UsedImplicitly]
        public class CommandDto
        {
            [UsedImplicitly] public string id;
            [UsedImplicitly] public bool enabled;
            [UsedImplicitly] public string name;
            [UsedImplicitly] public string handler;
            [UsedImplicitly] public string help;
            [UsedImplicitly] public bool moderatorOnly;
            [UsedImplicitly] public bool respondInTwitch;
            [UsedImplicitly] public bool respondInOverlay;
        }

        [UsedImplicitly]
        public class RewardDto
        {
            [UsedImplicitly] public string id;
            [UsedImplicitly] public bool enabled;
            [UsedImplicitly] public string title;
            [UsedImplicitly] public string handler;
            [UsedImplicitly] public string prompt;
            [UsedImplicitly] public int cost;
            [UsedImplicitly] public bool isEnabled;
            [UsedImplicitly] public bool isUserInputRequired;
            [UsedImplicitly] public int? maxPerStream;
            [UsedImplicitly] public int? maxPerUserPerStream;
            [UsedImplicitly] public int? globalCooldownSeconds;
            [UsedImplicitly] public bool respondInTwitch;
            [UsedImplicitly] public bool respondInOverlay;
        }

        [UsedImplicitly]
        public class ConfigPayload
        {
            [UsedImplicitly] public List<CommandDto> commands;
            [UsedImplicitly] public List<RewardDto> rewards;
            [UsedImplicitly] public List<HandlerInfo> commandHandlers;
            [UsedImplicitly] public List<HandlerInfo> rewardHandlers;
            [UsedImplicitly] public int activeProfile;
            [UsedImplicitly] public bool disableAutomaticFulfillment;
            [UsedImplicitly] public SimTestingDto simTesting;
            [UsedImplicitly] public List<GlobalConfigDto> globalConfigs;
        }

        [UsedImplicitly]
        public class SimTestingItemDto
        {
            [UsedImplicitly] public string id;
            [UsedImplicitly] public bool enabled;
            [UsedImplicitly] public string type;   // "Reward" or "Command"
            [UsedImplicitly] public string actionId;
            [UsedImplicitly] public string args;
            [UsedImplicitly] public float weight;
        }

        [UsedImplicitly]
        public class SimTestingDto
        {
            [UsedImplicitly] public int userCount;
            [UsedImplicitly] public int userStayTime;
            [UsedImplicitly] public int intervalMinMS;
            [UsedImplicitly] public int intervalMaxMS;
            [UsedImplicitly] public List<SimTestingItemDto> init;
            [UsedImplicitly] public List<SimTestingItemDto> use;
            [UsedImplicitly] public bool isRunning;
        }

        [UsedImplicitly]
        public class GlobalConfigDto
        {
            [UsedImplicitly] public string id;
            [UsedImplicitly] public string name;
            [UsedImplicitly] public List<ConfigFieldDto> fields;
        }

        [UsedImplicitly]
        public class ConfigFieldDto
        {
            [UsedImplicitly] public string key;
            [UsedImplicitly] public string label;
            [UsedImplicitly] public string type;      // "bool","int","float","string","enum"
            [UsedImplicitly] public object value;
            [UsedImplicitly] public string category;
            [UsedImplicitly] public int order;
            [UsedImplicitly] public double? min;
            [UsedImplicitly] public double? max;
            [UsedImplicitly] public string description;
            [UsedImplicitly] public List<string> enumValues;
        }

        // ── Hub methods ──────────────────────────────────────

        private static SimTestingDto MapSimTesting(Settings settings)
        {
            var sim = settings.SimTesting ?? new SimTestingConfig();
            return new SimTestingDto
            {
                userCount = sim.UserCount,
                userStayTime = sim.UserStayTime,
                intervalMinMS = sim.IntervalMinMS,
                intervalMaxMS = sim.IntervalMaxMS,
                isRunning = BLTModule.TwitchService?.IsSimTesting ?? false,
                init = (sim.Init ?? new System.Collections.ObjectModel.ObservableCollection<SimTestingItem>())
                    .Select(i => new SimTestingItemDto
                    {
                        id = i.ID.ToString(),
                        enabled = i.Enabled,
                        type = i.Type.ToString(),
                        actionId = i.Id?.ToString() ?? "",
                        args = i.Args?.ToString() ?? "",
                        weight = i.Weight,
                    }).ToList(),
                use = (sim.Use ?? new System.Collections.ObjectModel.ObservableCollection<SimTestingItem>())
                    .Select(i => new SimTestingItemDto
                    {
                        id = i.ID.ToString(),
                        enabled = i.Enabled,
                        type = i.Type.ToString(),
                        actionId = i.Id?.ToString() ?? "",
                        args = i.Args?.ToString() ?? "",
                        weight = i.Weight,
                    }).ToList(),
            };
        }

        private static List<GlobalConfigDto> MapGlobalConfigs(Settings settings)
        {
            var result = new List<GlobalConfigDto>();
            if (settings.GlobalConfigs == null) return result;

            foreach (var gc in settings.GlobalConfigs)
            {
                if (gc?.Config == null) continue;
                var dto = new GlobalConfigDto
                {
                    id = gc.Id ?? "",
                    name = gc.Config.ToString() ?? gc.Id ?? "Unknown",
                    fields = new List<ConfigFieldDto>(),
                };

                var configType = gc.Config.GetType();
                foreach (var prop in configType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!prop.CanRead || !prop.CanWrite) continue;

                    var browsable = prop.GetCustomAttribute<BrowsableAttribute>();
                    if (browsable != null && !browsable.Browsable) continue;

                    var propType = prop.PropertyType;
                    var underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;

                    string fieldType = null;
                    List<string> enumVals = null;
                    if (underlyingType == typeof(bool)) fieldType = "bool";
                    else if (underlyingType == typeof(int)) fieldType = "int";
                    else if (underlyingType == typeof(float) || underlyingType == typeof(double)) fieldType = "float";
                    else if (underlyingType == typeof(string)) fieldType = "string";
                    else if (underlyingType.IsEnum)
                    {
                        fieldType = "enum";
                        enumVals = Enum.GetNames(underlyingType).ToList();
                    }
                    else continue; // Skip complex types

                    object val;
                    try { val = prop.GetValue(gc.Config); }
                    catch { continue; }

                    var orderAttr = prop.GetCustomAttribute<Xceed.Wpf.Toolkit.PropertyGrid.Attributes.PropertyOrderAttribute>();
                    var descAttr = prop.GetCustomAttribute<DescriptionAttribute>();
                    var catAttr = prop.GetCustomAttribute<CategoryAttribute>();

                    double? min = null, max = null;

                    dto.fields.Add(new ConfigFieldDto
                    {
                        key = prop.Name,
                        label = prop.Name,
                        type = fieldType,
                        value = val,
                        category = catAttr?.Category,
                        order = orderAttr?.Order ?? 999,
                        min = min,
                        max = max,
                        description = descAttr?.Description,
                        enumValues = enumVals,
                    });
                }

                dto.fields.Sort((a, b) => a.order.CompareTo(b.order));
                result.Add(dto);
            }
            return result;
        }

        /// <summary>
        /// Returns the full configuration to the web UI.
        /// </summary>
        [UsedImplicitly]
        public ConfigPayload GetConfig()
        {
            return MainThreadSync.Run(() =>
            {
                var settings = BLTModule.TwitchService?.GetSettings();
                if (settings == null)
                    return new ConfigPayload
                    {
                        commands = new List<CommandDto>(),
                        rewards = new List<RewardDto>(),
                        commandHandlers = ActionManager.CommandHandlerIDsAndDisplayNames
                            .Select(h => new HandlerInfo { id = h.id, displayName = h.displayName }).ToList(),
                        rewardHandlers = ActionManager.RewardHandlerIDsAndDisplayNames
                            .Select(h => new HandlerInfo { id = h.id, displayName = h.displayName }).ToList(),
                        activeProfile = Settings.ActiveProfile,
                        disableAutomaticFulfillment = false,
                        simTesting = new SimTestingDto
                        {
                            init = new List<SimTestingItemDto>(),
                            use = new List<SimTestingItemDto>(),
                        },
                        globalConfigs = new List<GlobalConfigDto>(),
                    };

                return new ConfigPayload
                {
                    commands = settings.Commands.Select(c => new CommandDto
                    {
                        id = c.ID.ToString(),
                        enabled = c.Enabled,
                        name = c.Name?.ToString() ?? "",
                        handler = c.Handler ?? "",
                        help = c.Help?.ToString() ?? "",
                        moderatorOnly = c.ModeratorOnly,
                        respondInTwitch = c.RespondInTwitch,
                        respondInOverlay = c.RespondInOverlay,
                    }).ToList(),
                    rewards = settings.Rewards.Select(r => new RewardDto
                    {
                        id = r.ID.ToString(),
                        enabled = r.Enabled,
                        title = r.RewardSpec?.Title?.ToString() ?? "",
                        handler = r.Handler ?? "",
                        prompt = r.RewardSpec?.Prompt?.ToString() ?? "",
                        cost = r.RewardSpec?.Cost ?? 100,
                        isEnabled = r.RewardSpec?.IsEnabled ?? true,
                        isUserInputRequired = r.RewardSpec?.IsUserInputRequired ?? false,
                        maxPerStream = r.RewardSpec?.MaxPerStream,
                        maxPerUserPerStream = r.RewardSpec?.MaxPerUserPerStream,
                        globalCooldownSeconds = r.RewardSpec?.GlobalCooldownSeconds,
                        respondInTwitch = r.RespondInTwitch,
                        respondInOverlay = r.RespondInOverlay,
                    }).ToList(),
                    commandHandlers = ActionManager.CommandHandlerIDsAndDisplayNames
                        .Select(h => new HandlerInfo { id = h.id, displayName = h.displayName }).ToList(),
                    rewardHandlers = ActionManager.RewardHandlerIDsAndDisplayNames
                        .Select(h => new HandlerInfo { id = h.id, displayName = h.displayName }).ToList(),
                    activeProfile = Settings.ActiveProfile,
                    disableAutomaticFulfillment = settings.DisableAutomaticFulfillment,
                    simTesting = MapSimTesting(settings),
                    globalConfigs = MapGlobalConfigs(settings),
                };
            });
        }

        /// <summary>
        /// Updates a single command's mutable fields from the web UI.
        /// </summary>
        [UsedImplicitly]
        public bool UpdateCommand(string commandId, JObject updates)
        {
            if (string.IsNullOrWhiteSpace(commandId) || updates == null)
                return false;

            return MainThreadSync.Run(() =>
            {
                var settings = BLTModule.TwitchService?.GetSettings();
                if (settings == null) return false;

                var cmd = settings.Commands.FirstOrDefault(c => c.ID.ToString() == commandId);
                if (cmd == null) return false;

                if (updates.TryGetValue("enabled", out var e))
                    cmd.Enabled = e.Value<bool>();
                if (updates.TryGetValue("name", out var n))
                    cmd.Name = new LocString(n.Value<string>());
                if (updates.TryGetValue("help", out var h))
                    cmd.Help = new LocString(h.Value<string>());
                if (updates.TryGetValue("moderatorOnly", out var m))
                    cmd.ModeratorOnly = m.Value<bool>();
                if (updates.TryGetValue("respondInTwitch", out var rt))
                    cmd.RespondInTwitch = rt.Value<bool>();
                if (updates.TryGetValue("respondInOverlay", out var ro))
                    cmd.RespondInOverlay = ro.Value<bool>();

                Settings.Save(settings);
                return true;
            });
        }

        /// <summary>
        /// Updates a single reward's mutable fields from the web UI.
        /// </summary>
        [UsedImplicitly]
        public bool UpdateReward(string rewardId, JObject updates)
        {
            if (string.IsNullOrWhiteSpace(rewardId) || updates == null)
                return false;

            return MainThreadSync.Run(() =>
            {
                var settings = BLTModule.TwitchService?.GetSettings();
                if (settings == null) return false;

                var reward = settings.Rewards.FirstOrDefault(r => r.ID.ToString() == rewardId);
                if (reward == null) return false;

                if (updates.TryGetValue("enabled", out var e))
                    reward.Enabled = e.Value<bool>();
                if (updates.TryGetValue("title", out var t))
                    reward.RewardSpec.Title = new LocString(t.Value<string>());
                if (updates.TryGetValue("prompt", out var p))
                    reward.RewardSpec.Prompt = new LocString(p.Value<string>());
                if (updates.TryGetValue("cost", out var c))
                    reward.RewardSpec.Cost = c.Value<int>();
                if (updates.TryGetValue("isEnabled", out var ie))
                    reward.RewardSpec.IsEnabled = ie.Value<bool>();
                if (updates.TryGetValue("isUserInputRequired", out var ir))
                    reward.RewardSpec.IsUserInputRequired = ir.Value<bool>();
                if (updates.TryGetValue("maxPerStream", out var mps))
                    reward.RewardSpec.MaxPerStream = mps.Type == JTokenType.Null ? null : mps.Value<int?>();
                if (updates.TryGetValue("maxPerUserPerStream", out var mpups))
                    reward.RewardSpec.MaxPerUserPerStream = mpups.Type == JTokenType.Null ? null : mpups.Value<int?>();
                if (updates.TryGetValue("globalCooldownSeconds", out var gcs))
                    reward.RewardSpec.GlobalCooldownSeconds = gcs.Type == JTokenType.Null ? null : gcs.Value<int?>();
                if (updates.TryGetValue("respondInTwitch", out var rt))
                    reward.RespondInTwitch = rt.Value<bool>();
                if (updates.TryGetValue("respondInOverlay", out var ro))
                    reward.RespondInOverlay = ro.Value<bool>();

                Settings.Save(settings);
                return true;
            });
        }

        /// <summary>
        /// Toggles the enabled state of a command or reward.
        /// </summary>
        [UsedImplicitly]
        public bool ToggleAction(string actionId, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(actionId))
                return false;

            return MainThreadSync.Run(() =>
            {
                var settings = BLTModule.TwitchService?.GetSettings();
                if (settings == null) return false;

                var action = settings.AllActions.FirstOrDefault(a => a.ID.ToString() == actionId);
                if (action == null) return false;

                action.Enabled = enabled;
                Settings.Save(settings);
                return true;
            });
        }

        /// <summary>
        /// Switches the active settings profile (1, 2, or 3).
        /// </summary>
        [UsedImplicitly]
        public bool SwitchProfile(int profile)
        {
            if (profile < 1 || profile > 3)
                return false;

            return MainThreadSync.Run(() =>
            {
                Settings.ChangeProfile(profile);
                return true;
            });
        }

        // ── Sim Testing methods ──────────────────────────────

        /// <summary>
        /// Updates the simulation testing config fields.
        /// </summary>
        [UsedImplicitly]
        public bool UpdateSimTesting(JObject updates)
        {
            if (updates == null) return false;

            return MainThreadSync.Run(() =>
            {
                var settings = BLTModule.TwitchService?.GetSettings();
                if (settings == null) return false;

                var sim = settings.SimTesting;
                if (sim == null) { sim = new SimTestingConfig(); settings.SimTesting = sim; }

                if (updates.TryGetValue("userCount", out var uc))
                    sim.UserCount = Math.Max(1, uc.Value<int>());
                if (updates.TryGetValue("userStayTime", out var ust))
                    sim.UserStayTime = Math.Max(1, ust.Value<int>());
                if (updates.TryGetValue("intervalMinMS", out var imin))
                    sim.IntervalMinMS = Math.Max(100, imin.Value<int>());
                if (updates.TryGetValue("intervalMaxMS", out var imax))
                    sim.IntervalMaxMS = Math.Max(100, imax.Value<int>());

                Settings.Save(settings);
                return true;
            });
        }

        /// <summary>
        /// Starts the simulation test.
        /// </summary>
        [UsedImplicitly]
        public bool StartSimulation()
        {
            return MainThreadSync.Run(() =>
            {
                BLTModule.TwitchService?.StartSim();
                return BLTModule.TwitchService?.IsSimTesting ?? false;
            });
        }

        /// <summary>
        /// Stops the simulation test.
        /// </summary>
        [UsedImplicitly]
        public bool StopSimulation()
        {
            return MainThreadSync.Run(() =>
            {
                BLTModule.TwitchService?.StopSim();
                return !(BLTModule.TwitchService?.IsSimTesting ?? true);
            });
        }

        // ── Global Config methods ────────────────────────────

        /// <summary>
        /// Updates a single field in a global config section.
        /// </summary>
        [UsedImplicitly]
        public bool UpdateGlobalConfig(string configId, string fieldKey, JToken value)
        {
            if (string.IsNullOrWhiteSpace(configId) || string.IsNullOrWhiteSpace(fieldKey) || value == null)
                return false;

            return MainThreadSync.Run(() =>
            {
                var settings = BLTModule.TwitchService?.GetSettings();
                if (settings == null) return false;

                var gc = settings.GlobalConfigs?.FirstOrDefault(g => g.Id == configId);
                if (gc?.Config == null) return false;

                var prop = gc.Config.GetType().GetProperty(fieldKey, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null || !prop.CanWrite) return false;

                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                try
                {
                    object converted;
                    if (value.Type == JTokenType.Null)
                        converted = null;
                    else if (targetType == typeof(bool))
                        converted = value.Value<bool>();
                    else if (targetType == typeof(int))
                        converted = value.Value<int>();
                    else if (targetType == typeof(float))
                        converted = value.Value<float>();
                    else if (targetType == typeof(double))
                        converted = value.Value<double>();
                    else if (targetType == typeof(string))
                        converted = value.Value<string>();
                    else if (targetType.IsEnum)
                        converted = Enum.Parse(targetType, value.Value<string>());
                    else
                        return false;

                    prop.SetValue(gc.Config, converted);
                    Settings.Save(settings);
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }
    }
}
