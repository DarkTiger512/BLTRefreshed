using System;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BLTAdoptAHero.Behaviors;
using BLTAdoptAHero.Util;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace BLTAdoptAHero.Actions
{
    [LocDisplayName("Stream Objective Admin"),
     LocDescription("Moderator controls for starting, inspecting, and stopping stream objectives"), UsedImplicitly]
    internal sealed class StreamObjectiveAdminCommand : ICommandHandler
    {
        public Type HandlerConfigType => null;

        public void Execute(ReplyContext context, object config)
        {
            var behavior = StreamObjectivesBehavior.Current;
            if (behavior == null) { Reply(context, "Stream objectives require an active campaign."); return; }
            var args = (context.Args ?? "").Trim();
            var verb = args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.ToLowerInvariant();
            switch (verb)
            {
                case "list":
                    Reply(context, "Objectives: kills, cavalry, battles, tournaments, captures, survive.");
                    return;
                case "status":
                    Reply(context, behavior.Status(context.UserName));
                    return;
                case "stop":
                    behavior.Cancel(out string stopped);
                    Reply(context, stopped);
                    return;
                case "start":
                    bool CultureExists(string id) => MBObjectManager.Instance.GetObject<CultureObject>(id) != null;
                    if (!StreamObjectivePolicy.TryParseStart(args, CultureExists, out var start, out string error))
                    { Reply(context, error); return; }
                    behavior.Start(start, context.UserName, out string started);
                    Reply(context, started);
                    return;
                default:
                    Reply(context, "Usage: !objective list | start <type> ... gold=<amount> xp=<amount> | status | stop");
                    return;
            }
        }

        private static void Reply(ReplyContext context, string text) => ActionManager.SendReply(context, text);
    }

    [LocDisplayName("Stream Objectives Status"),
     LocDescription("Shows the active community objective and your contribution"), UsedImplicitly]
    internal sealed class StreamObjectivesStatusCommand : ICommandHandler
    {
        public Type HandlerConfigType => null;
        public void Execute(ReplyContext context, object config) => ActionManager.SendReply(context,
            StreamObjectivesBehavior.Current?.Status(context.UserName) ?? "Stream objectives require an active campaign.");
    }
}
