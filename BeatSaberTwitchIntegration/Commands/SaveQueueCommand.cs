using AsyncTwitch;

namespace TwitchIntegrationPlugin.Commands
{
    public class SaveQueueCommand : IrcCommand
    {
        public override string[] CommandAlias => new[] {"saveq", "save"};

        public override void Run(TwitchMessage msg)
        {
            StaticData.SongQueue.SaveSongQueue();
            TwitchConnection.Instance.SendChatMessage("Saving Queue.");
        }
    }
}
