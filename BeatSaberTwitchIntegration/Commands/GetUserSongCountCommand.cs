using AsyncTwitch;

namespace TwitchIntegrationPlugin.Commands
{
    public class GetUserSongCountCommand : IrcCommand
    {
        public override string[] CommandAlias => new[] {"qcount", "queued"};
        public override void Run(TwitchMessage msg)
        {
            if (!StaticData.UserRequestCount.ContainsKey(msg.Author.DisplayName))
            {
                TwitchConnection.Instance.SendChatMessage($"{msg.Author.DisplayName}, you have no songs in the queue.");
                return;
            }
            TwitchConnection.Instance.SendChatMessage($"{msg.Author.DisplayName}, you have {StaticData.UserRequestCount[msg.Author.DisplayName]} songs in queue.");
        }
    }
}
