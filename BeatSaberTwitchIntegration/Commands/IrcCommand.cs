using AsyncTwitch;

namespace TwitchIntegrationPlugin.Commands
{
    public abstract class IrcCommand
    {
        public abstract string CommandName { get; }
        public abstract string[] CommandAlias { get; }
        public abstract void Run(TwitchMessage msg);
    }
}
