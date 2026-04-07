using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BannerlordTwitch;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using Microsoft.AspNet.SignalR;

namespace BLTOverlay
{
    public class CommandsHub : Hub
    {
        [UsedImplicitly]
        public class CommandInfo
        {
            /// <summary>"command" or "reward"</summary>
            [UsedImplicitly] public string type;
            [UsedImplicitly] public string name;
            [UsedImplicitly] public string description;
            [UsedImplicitly] public bool moderatorOnly;
            /// <summary>Channel point cost (rewards only)</summary>
            [UsedImplicitly] public int? cost;
            /// <summary>Global cooldown in seconds (rewards only)</summary>
            [UsedImplicitly] public int? cooldownSeconds;
        }

        private static readonly List<CommandInfo> commandList = new();

        public override Task OnConnected()
        {
            lock (commandList)
            {
                Clients.Caller.updateCommands(commandList);
            }
            return base.OnConnected();
        }

        [UsedImplicitly]
        public void Refresh()
        {
            lock (commandList)
            {
                Clients.Caller.updateCommands(commandList);
            }
        }

        /// <summary>
        /// Called from the overlay when a viewer clicks a command to execute it.
        /// </summary>
        [UsedImplicitly]
        public bool ExecuteCommand(string commandName, string userName)
        {
            if (string.IsNullOrWhiteSpace(commandName) || string.IsNullOrWhiteSpace(userName))
                return false;

            // Sanitize inputs
            commandName = commandName.TrimStart('!').Trim();
            userName = userName.Trim();

            if (commandName.Length > 100 || userName.Length > 100)
                return false;

            return MainThreadSync.Run(() =>
                BLTModule.TwitchService?.TestCommand(commandName, userName, null) ?? false);
        }

        /// <summary>
        /// Called from TwitchService when the command/reward list is known.
        /// Broadcasts the updated list to all connected clients.
        /// </summary>
        public static void UpdateCommandList(IEnumerable<CommandInfo> commands)
        {
            lock (commandList)
            {
                commandList.Clear();
                commandList.AddRange(commands);
            }
            GlobalHost.ConnectionManager.GetHubContext<CommandsHub>()
                .Clients.All.updateCommands(commandList);
        }
    }
}
