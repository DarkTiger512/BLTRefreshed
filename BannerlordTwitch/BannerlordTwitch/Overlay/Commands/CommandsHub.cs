using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
