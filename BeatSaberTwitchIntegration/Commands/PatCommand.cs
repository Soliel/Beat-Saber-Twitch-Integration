using AsyncTwitch;

namespace TwitchIntegrationPlugin.Commands
{
    public class PatCommand : IrcCommand
    {
        public override string[] CommandAlias => new[] {"pat"};
        public override void Run(TwitchMessage msg)
        {
            string pattie = msg.Content.Remove(0, msg.Content.IndexOf(' ') + 1);
            TwitchConnection.Instance.SendChatMessage($"{msg.Author.DisplayName} pats {pattie}");
        }
    }
}
