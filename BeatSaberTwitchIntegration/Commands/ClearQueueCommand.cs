using System.Collections.Generic;
using AsyncTwitch;
using TwitchIntegrationPlugin.Serializables;

namespace TwitchIntegrationPlugin.Commands
{
    public class ClearQueueCommand : IrcCommand
    {
        public override string[] CommandAlias => new[] {"clear", "annhilate"};
        public override void Run(TwitchMessage msg)
        {
            StaticData.SongQueue.SongQueueList = new List<QueuedSong>();
        }
    }
}
