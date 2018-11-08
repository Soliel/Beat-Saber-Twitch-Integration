using AsyncTwitch;

namespace TwitchIntegrationPlugin.Commands
{
    public abstract class IrcCommand
    {
        public abstract string[] CommandAlias { get; }
        public abstract void Run(TwitchMessage msg);
    }
}
